using System;
using System.Collections.Generic;
using ZombieStreamer.Economy;

namespace ZombieStreamer.Monetization
{
    /// <summary>
    /// "Ganhar comprando": a única forma, além da conquista no jogo, de obter algo PERMANENTE.
    /// Fluxo: compra validada pela loja → entrega (uma vez por transação) → SALVA → confirma à loja.
    /// Se a entrega ou o save falharem, a compra fica pendente e a loja a reenvia
    /// (o Google Play reembolsa sozinho compras não confirmadas em 3 dias).
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
            var ownedForever = p.type == ProductKind.NonConsumable || p.oncePerAccount;
            return !(ownedForever && _wallet.HasPurchasedOnce(productId));
        }

        /// <param name="target">Item ("bunker:4") ou skill ("talk"), para produtos com requiresChoice.</param>
        /// <param name="outfitIndex">Roupa escolhida, para produtos com requiresChoice.</param>
        public void Buy(string productId, string target = null, int outfitIndex = -1)
        {
            if (!CanBuy(productId))
            {
                OnPurchaseFailed(productId, PurchaseFailure.AlreadyOwned);
                return;
            }
            var product = _catalog[productId];
            if (product.requiresChoice)
            {
                if (string.IsNullOrEmpty(target) && outfitIndex < 0)
                {
                    OnPurchaseFailed(productId, PurchaseFailure.MissingChoice);
                    return;
                }
                // Salva a escolha ANTES de abrir a loja: se o app fechar no meio, a entrega ainda sabe o alvo.
                _wallet.SetPendingChoice(new PendingChoice { productId = productId, target = target, outfitIndex = outfitIndex });
                if (!_persist())
                {
                    OnPurchaseFailed(productId, PurchaseFailure.Unknown);
                    return;
                }
            }
            _iap.Purchase(productId);
        }

        private void OnPurchaseSucceeded(PurchaseReceipt receipt)
        {
            if (!_catalog.TryGetValue(receipt.ProductId, out var product))
            {
                // Produto fora do catálogo: não confirma, para a loja reenviar após atualização do app.
                Log("iap_unknown_product", receipt.ProductId, receipt.TransactionId);
                return;
            }
            if (_wallet.IsTransactionProcessed(receipt.TransactionId))
            {
                // Já entregue antes. Garante que está salvo (o save anterior pode ter falhado) e só então confirma.
                if (!_persist())
                {
                    Log("iap_save_failed", product.productId, receipt.TransactionId);
                    return;
                }
                _iap.ConfirmDelivered(receipt);
                return;
            }

            var alreadyOwned = (product.type == ProductKind.NonConsumable || product.oncePerAccount)
                && _wallet.HasPurchasedOnce(product.productId);
            var rewards = ResolveRewards(product, alreadyOwned);
            if (rewards == null)
            {
                Log("iap_missing_choice", product.productId, receipt.TransactionId);
                return;
            }

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

            if (product.requiresChoice)
                _wallet.TakePendingChoice(product.productId);
            _wallet.MarkTransactionProcessed(receipt.TransactionId);
            if (product.oncePerAccount || product.type == ProductKind.NonConsumable)
                _wallet.MarkPurchasedOnce(product.productId);

            if (!_persist())
            {
                Log("iap_save_failed", product.productId, receipt.TransactionId);
                return;
            }
            Log(alreadyOwned ? "iap_restored" : "iap_delivered", product.productId, receipt.TransactionId);
            _iap.ConfirmDelivered(receipt);
            Delivered?.Invoke(product);
        }

        /// <summary>Null quando o produto exige escolha e ela se perdeu.</summary>
        private IReadOnlyList<RewardDefinition> ResolveRewards(IapProductConfig product, bool alreadyOwned)
        {
            var source = product.rewards;
            if (alreadyOwned)
            {
                // Restore/reenvio de algo já comprado: reaplica só posses permanentes de conta,
                // sem repetir consumíveis (moedas, tickets).
                source = source.FindAll(r => r.type == RewardType.Outfit || r.type == RewardType.PermanentIncomeMultiplier);
            }
            if (!product.requiresChoice)
                return source;

            var choice = _wallet.Snapshot.pendingPurchaseChoices.Find(c => c.productId == product.productId);
            if (choice == null)
                return null;
            return source.ConvertAll(r => r.WithTarget(choice.target, choice.outfitIndex));
        }

        private void OnPurchaseFailed(string productId, PurchaseFailure reason)
        {
            // Compra desistida/recusada: descarta a escolha salva para não ser reaproveitada depois.
            if ((reason == PurchaseFailure.Cancelled || reason == PurchaseFailure.PaymentDeclined
                    || reason == PurchaseFailure.NotAvailable) && _wallet.TakePendingChoice(productId) != null)
                _persist();
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
