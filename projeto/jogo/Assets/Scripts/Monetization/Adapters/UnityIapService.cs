#if ZS_UNITY_IAP
using System;
using System.Collections.Generic;

namespace ZombieStreamer.Monetization.Adapters
{
    /// <summary>
    /// Adaptador para Unity IAP (o original usa Unity Purchasing v5 com Google Play).
    /// A API do Unity IAP mudou bastante entre v4 (IStoreListener) e v5 (StoreController);
    /// implemente os TODOs com a versão instalada no projeto e ative o define ZS_UNITY_IAP.
    /// Regras que o adaptador PRECISA cumprir para o PurchaseRewardHandler funcionar:
    ///  1. Disparar PurchaseSucceeded só para compras validadas (recibo do Google Play).
    ///  2. Disparar PurchaseSucceeded também para compras pendentes encontradas no startup.
    ///  3. Não confirmar (consume/acknowledge) a compra até ConfirmDelivered ser chamado.
    ///  4. Usar o orderId/transactionId da loja como TransactionId (idempotência).
    /// </summary>
    public sealed class UnityIapService : IIapService
    {
        private readonly Dictionary<string, string> _localizedPrices = new Dictionary<string, string>();

        public event Action<PurchaseReceipt> PurchaseSucceeded;
        public event Action<string, PurchaseFailure> PurchaseFailed;

        public void Initialize(IEnumerable<IapProductConfig> products, Action onReady)
        {
            // TODO: registrar produtos (Consumable/NonConsumable) e conectar à loja.
            // TODO: ao receber a lista de produtos, preencher _localizedPrices[productId] = metadata.localizedPriceString.
            // TODO: para cada compra pendente/não confirmada: PurchaseSucceeded?.Invoke(new PurchaseReceipt(id, orderId)).
            onReady?.Invoke();
        }

        public string GetLocalizedPrice(string productId) =>
            _localizedPrices.TryGetValue(productId, out var price) ? price : null;

        public void Purchase(string productId)
        {
            // TODO: iniciar compra. No callback de sucesso validado:
            //   PurchaseSucceeded?.Invoke(new PurchaseReceipt(productId, orderId));
            // No callback de falha: PurchaseFailed?.Invoke(productId, MapFailure(reason));
            PurchaseFailed?.Invoke(productId, PurchaseFailure.NotAvailable);
        }

        public void RestorePurchases()
        {
            // TODO: no Google Play as compras não-consumíveis voltam no startup; aqui basta re-sincronizar.
        }

        public void ConfirmDelivered(PurchaseReceipt receipt)
        {
            // TODO: confirmar a compra pendente (ConfirmPurchase / ConfirmPendingPurchase, conforme a versão).
        }
    }
}
#endif
