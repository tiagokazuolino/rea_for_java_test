using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZombieStreamer.Monetization.Adapters
{
    /// <summary>Anúncios simulados para Editor/QA: sempre prontos, completam na hora.</summary>
    public sealed class FakeAdService : IAdService
    {
        public bool IsRewardedReady => true;
        public bool IsInterstitialReady => true;
        public AdShowResult NextResult { get; set; } = AdShowResult.Completed;

        public void Initialize(Action onReady) => onReady?.Invoke();

        public void ShowRewarded(string placementId, Action<AdShowResult> onFinished)
        {
            Debug.Log($"[FakeAds] rewarded '{placementId}' -> {NextResult}");
            onFinished?.Invoke(NextResult);
        }

        public void ShowInterstitial(string placementId, Action<AdShowResult> onFinished)
        {
            Debug.Log($"[FakeAds] interstitial '{placementId}'");
            onFinished?.Invoke(AdShowResult.Completed);
        }

        public void SetBannerVisible(bool visible) => Debug.Log($"[FakeAds] banner visible={visible}");
    }

    /// <summary>Loja simulada: toda compra é aprovada com um transactionId novo.</summary>
    public sealed class FakeIapService : IIapService
    {
        private readonly Dictionary<string, string> _prices = new Dictionary<string, string>();

        public event Action<PurchaseReceipt> PurchaseSucceeded;
        public event Action<string, PurchaseFailure> PurchaseFailed;

        public void Initialize(IEnumerable<IapProductConfig> products, Action onReady)
        {
            foreach (var p in products)
                _prices[p.productId] = p.priceTier;
            onReady?.Invoke();
        }

        public string GetLocalizedPrice(string productId) =>
            _prices.TryGetValue(productId, out var price) ? price : null;

        public void Purchase(string productId)
        {
            if (!_prices.ContainsKey(productId))
            {
                PurchaseFailed?.Invoke(productId, PurchaseFailure.NotAvailable);
                return;
            }
            PurchaseSucceeded?.Invoke(new PurchaseReceipt(productId, Guid.NewGuid().ToString("N")));
        }

        public void RestorePurchases() => Debug.Log("[FakeIap] restore (nada a restaurar)");

        public void ConfirmDelivered(PurchaseReceipt receipt) =>
            Debug.Log($"[FakeIap] confirmado {receipt.ProductId} ({receipt.TransactionId})");
    }

    /// <summary>Fallback de build sem SDK de anúncios: nada está pronto, nada é entregue.</summary>
    public sealed class UnavailableAdService : IAdService
    {
        public bool IsRewardedReady => false;
        public bool IsInterstitialReady => false;

        public void Initialize(Action onReady)
        {
            Debug.LogError("[Ads] Nenhum SDK de anúncios configurado (defina ZS_APPLOVIN_MAX).");
            onReady?.Invoke();
        }

        public void ShowRewarded(string placementId, Action<AdShowResult> onFinished) => onFinished?.Invoke(AdShowResult.NotLoaded);
        public void ShowInterstitial(string placementId, Action<AdShowResult> onFinished) => onFinished?.Invoke(AdShowResult.NotLoaded);
        public void SetBannerVisible(bool visible) { }
    }

    /// <summary>Fallback de build sem loja configurada: toda compra falha.</summary>
    public sealed class UnavailableIapService : IIapService
    {
        public event Action<PurchaseReceipt> PurchaseSucceeded { add { } remove { } }
        public event Action<string, PurchaseFailure> PurchaseFailed;

        public void Initialize(IEnumerable<IapProductConfig> products, Action onReady)
        {
            Debug.LogError("[IAP] Nenhuma loja configurada (defina ZS_UNITY_IAP).");
            onReady?.Invoke();
        }

        public string GetLocalizedPrice(string productId) => null;
        public void Purchase(string productId) => PurchaseFailed?.Invoke(productId, PurchaseFailure.NotAvailable);
        public void RestorePurchases() { }
        public void ConfirmDelivered(PurchaseReceipt receipt) { }
    }

    public sealed class UnityClock : IClock
    {
        public double NowSeconds => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;
    }

    public sealed class DebugAnalytics : IAnalytics
    {
        public void Log(string eventName, IReadOnlyDictionary<string, object> parameters)
        {
            var parts = new List<string>();
            foreach (var kv in parameters)
                parts.Add($"{kv.Key}={kv.Value}");
            Debug.Log($"[Analytics] {eventName} {string.Join(" ", parts)}");
        }
    }
}
