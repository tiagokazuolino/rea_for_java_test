using System;
using System.Collections.Generic;
using ZombieStreamer.Economy;

namespace ZombieStreamer.Monetization
{
    /// <summary>
    /// "Ganhar item comprando": recebe compras confirmadas pela loja, entrega as recompensas do
    /// catálogo exatamente uma vez, SALVA o progresso e só então confirma à loja (consume/acknowledge).
    /// Se a entrega ou o save falharem, a compra fica pendente e a loja a reenvia no próximo startup.
    /// </summary>
    public sealed class PurchaseRewardHandler : IDisposable
    {
        private readonly Dictionary<string, IapProductConfig> _catalog = new Dictionary<string, IapProductConfig>();
        private readonly IIapService _iap;
        private readonly PlayerWallet _wallet;
        private readonly RewardGranter _granter;
        private readonly IAnalytics _analytics;
        private readonly Func<bool> _persist;

        /// <param name="persist">Grava o save de forma síncrona; retorna false se falhar.</param>
        public PurchaseRewardHandler(IEnumerable<IapProductConfig> catalog, IIapService iap, PlayerWallet wallet,
            RewardGranter granter, IAnalytics analytics, Func<bool> persist)
        {
            foreach (var product in catalog ?? throw new ArgumentNullException(nameof(catalog)))
                _catalog[product.productId] = product;
            _iap = iap ?? throw new ArgumentNullException(nameof(iap));
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _granter = granter ?? throw new ArgumentNullException(nameof(granter));
            _analytics = analytics ?? throw new ArgumentNullException(nameof(analytics));
            _persist = persist ?? throw new ArgumentNullException(nameof(persist));
            _iap.PurchaseSucceeded += OnPurchaseSucceeded;
            _iap.PurchaseFailed += OnPurchaseFailed;
        }

        /// <summary>Disparado depois que as recompensas foram creditadas e salvas (para a UI mostrar "Obrigado!").</summary>
        public event Action<IapProductConfig> Delivered;

        public bool CanBuy(string productId)
        {
            if (!_catalog.TryGetValue(productId, out var p))
                return false;
            if (p.type == ProductKind.NonConsumable && IsOwned(p))
                return false;
            return !(p.oncePerAccount && _wallet.HasPurchasedOnce(productId));
        }

        public void Buy(string productId)
        {
            if (!CanBuy(productId))
            {
                OnPurchaseFailed(productId, PurchaseFailure.AlreadyOwned);
                return;
            }
            _iap.Purchase(productId);
        }

        private bool IsOwned(IapProductConfig p) =>
            _wallet.HasPurchasedOnce(p.productId)
            || (p.rewards.Count == 1 && p.rewards[0].type == RewardType.NoAds && _wallet.HasNoAds);

        private void OnPurchaseSucceeded(PurchaseReceipt receipt)
        {
            if (!_catalog.TryGetValue(receipt.ProductId, out var product))
            {
                // Produto fora do catálogo: não confirma, para a loja reenviar após atualização do app.
                Log("iap_unknown_product", receipt.ProductId, receipt.TransactionId);
                return;
            }
            if (_wallet.Snapshot.processedTransactions.Contains(receipt.TransactionId))
            {
                // Já entregue e salvo antes (ex.: crash depois do save): só confirma.
                _iap.ConfirmDelivered(receipt);
                return;
            }

            // Restore de não-consumível em aparelho novo ou reinstalação: reaplica só os direitos permanentes,
            // sem repetir bônus consumíveis (ex.: os tickets do bundle no-ads).
            var isRestore = product.type == ProductKind.NonConsumable && _wallet.HasPurchasedOnce(product.productId);
            var isRepeatOnce = product.oncePerAccount && _wallet.HasPurchasedOnce(product.productId);
            var rewards = isRestore || isRepeatOnce ? PermanentOnly(product.rewards) : product.rewards;

            try
            {
                _granter.Grant(rewards, RewardSource.Purchase);
            }
            catch (Exception ex)
            {
                // Não marca nem confirma: a loja reenviará a compra.
                _analytics.Log("iap_grant_error", new Dictionary<string, object>
                {
                    { "product", product.productId },
                    { "transaction", receipt.TransactionId },
                    { "error", ex.GetType().Name },
                });
                return;
            }

            _wallet.MarkTransactionProcessed(receipt.TransactionId);
            if (product.oncePerAccount || product.type == ProductKind.NonConsumable)
                _wallet.MarkPurchasedOnce(product.productId);

            if (!_persist())
            {
                Log("iap_save_failed", product.productId, receipt.TransactionId);
                return;
            }
            Log(isRestore ? "iap_restored" : "iap_delivered", product.productId, receipt.TransactionId);
            _iap.ConfirmDelivered(receipt);
            Delivered?.Invoke(product);
        }

        private static List<RewardDefinition> PermanentOnly(List<RewardDefinition> rewards) =>
            rewards.FindAll(r => r.type == RewardType.NoAds || r.type == RewardType.Outfit);

        private void OnPurchaseFailed(string productId, PurchaseFailure reason)
        {
            _analytics.Log("iap_failed", new Dictionary<string, object>
            {
                { "product", productId },
                { "reason", reason.ToString() },
            });
        }

        private void Log(string evt, string productId, string transactionId)
        {
            _analytics.Log(evt, new Dictionary<string, object>
            {
                { "product", productId },
                { "transaction", transactionId },
            });
        }

        public void Dispose()
        {
            _iap.PurchaseSucceeded -= OnPurchaseSucceeded;
            _iap.PurchaseFailed -= OnPurchaseFailed;
        }
    }
}
