using System;
using System.Collections.Generic;

namespace ZombieStreamer.Monetization
{
    public enum AdShowResult
    {
        Completed,
        Skipped,
        NotLoaded,
        Failed,
    }

    /// <summary>Fronteira com o SDK de anúncios (AppLovin MAX, AdMob...). Implementações em Adapters/.</summary>
    public interface IAdService
    {
        bool IsRewardedReady { get; }
        bool IsInterstitialReady { get; }

        void Initialize(Action onReady);

        /// <param name="placementId">Usado em analytics/relatórios do SDK.</param>
        /// <param name="onFinished">Completed somente quando o SDK confirmar a recompensa.</param>
        void ShowRewarded(string placementId, Action<AdShowResult> onFinished);

        void ShowInterstitial(string placementId, Action<AdShowResult> onFinished);
        void SetBannerVisible(bool visible);
    }

    public enum PurchaseFailure
    {
        Cancelled,
        AlreadyOwned,
        NotAvailable,
        PaymentDeclined,
        Unknown,
    }

    /// <summary>Compra concluída e validada pela loja.</summary>
    public sealed class PurchaseReceipt
    {
        public PurchaseReceipt(string productId, string transactionId)
        {
            ProductId = productId;
            TransactionId = transactionId;
        }

        public string ProductId { get; }
        public string TransactionId { get; }
    }

    /// <summary>Fronteira com a loja (Unity IAP / Google Play Billing).</summary>
    public interface IIapService
    {
        void Initialize(IEnumerable<IapProductConfig> products, Action onReady);

        /// <summary>Preço localizado vindo da loja (ex.: "R$ 24,90"). Null se ainda não carregou.</summary>
        string GetLocalizedPrice(string productId);

        void Purchase(string productId);

        /// <summary>Recompra/restauração de não-consumíveis (no-ads).</summary>
        void RestorePurchases();

        /// <summary>Disparado para compras novas E pendentes/restauradas no startup.</summary>
        event Action<PurchaseReceipt> PurchaseSucceeded;

        event Action<string, PurchaseFailure> PurchaseFailed;

        /// <summary>Confirma à loja que a entrega foi feita (consome/acknowledge).</summary>
        void ConfirmDelivered(PurchaseReceipt receipt);
    }

    public interface IClock
    {
        double NowSeconds { get; }
    }

    public interface IAnalytics
    {
        void Log(string eventName, IReadOnlyDictionary<string, object> parameters);
    }
}
