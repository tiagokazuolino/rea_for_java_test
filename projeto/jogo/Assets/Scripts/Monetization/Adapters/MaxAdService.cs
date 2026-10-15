#if ZS_APPLOVIN_MAX
using System;
using UnityEngine;

namespace ZombieStreamer.Monetization.Adapters
{
    /// <summary>
    /// Adaptador AppLovin MAX (o original usa MAX com mediação AdMob, Meta, Pangle, Mintegral,
    /// Unity Ads, ironSource, Vungle, InMobi, Moloco, BidMachine, Bigo, Yandex).
    /// Ative com o define ZS_APPLOVIN_MAX depois de importar o plugin MAX.
    /// Os IDs de ad unit NÃO ficam no código: vêm do MonetizationBootstrap (ScriptableObject/Remote Config).
    /// Não compilado neste repositório; conferir contra a versão do plugin instalada.
    /// </summary>
    public sealed class MaxAdService : IAdService
    {
        private readonly string _sdkKey;
        private readonly string _rewardedUnit;
        private readonly string _interstitialUnit;
        private readonly string _bannerUnit;
        private Action<AdShowResult> _pendingRewarded;
        private Action<AdShowResult> _pendingInterstitial;
        private bool _rewardEarned;
        private int _rewardedRetry;
        private int _interstitialRetry;

        public MaxAdService(string sdkKey, string rewardedUnit, string interstitialUnit, string bannerUnit)
        {
            _sdkKey = sdkKey;
            _rewardedUnit = rewardedUnit;
            _interstitialUnit = interstitialUnit;
            _bannerUnit = bannerUnit;
        }

        public bool IsRewardedReady => MaxSdk.IsRewardedAdReady(_rewardedUnit);
        public bool IsInterstitialReady => MaxSdk.IsInterstitialReady(_interstitialUnit);

        public void Initialize(Action onReady)
        {
            MaxSdkCallbacks.OnSdkInitializedEvent += _ =>
            {
                HookRewarded();
                HookInterstitial();
                MaxSdk.LoadRewardedAd(_rewardedUnit);
                MaxSdk.LoadInterstitial(_interstitialUnit);
                if (!string.IsNullOrEmpty(_bannerUnit))
                    MaxSdk.CreateBanner(_bannerUnit, MaxSdkBase.BannerPosition.BottomCenter);
                onReady?.Invoke();
            };
            MaxSdk.SetSdkKey(_sdkKey);
            MaxSdk.InitializeSdk();
        }

        public void ShowRewarded(string placementId, Action<AdShowResult> onFinished)
        {
            if (!IsRewardedReady)
            {
                onFinished?.Invoke(AdShowResult.NotLoaded);
                return;
            }
            _rewardEarned = false;
            _pendingRewarded = onFinished;
            MaxSdk.ShowRewardedAd(_rewardedUnit, placementId);
        }

        public void ShowInterstitial(string placementId, Action<AdShowResult> onFinished)
        {
            if (!IsInterstitialReady)
            {
                onFinished?.Invoke(AdShowResult.NotLoaded);
                return;
            }
            _pendingInterstitial = onFinished;
            MaxSdk.ShowInterstitial(_interstitialUnit, placementId);
        }

        public void SetBannerVisible(bool visible)
        {
            if (string.IsNullOrEmpty(_bannerUnit))
                return;
            if (visible)
                MaxSdk.ShowBanner(_bannerUnit);
            else
                MaxSdk.HideBanner(_bannerUnit);
        }

        private void HookRewarded()
        {
            MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += (_, __) => _rewardedRetry = 0;
            MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += (_, __) => RetryLoad(ref _rewardedRetry, () => MaxSdk.LoadRewardedAd(_rewardedUnit));
            MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += (_, __, ___) => _rewardEarned = true;
            MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += (_, __) =>
            {
                Complete(ref _pendingRewarded, _rewardEarned ? AdShowResult.Completed : AdShowResult.Skipped);
                MaxSdk.LoadRewardedAd(_rewardedUnit);
            };
            MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += (_, __, ___) =>
            {
                Complete(ref _pendingRewarded, AdShowResult.Failed);
                MaxSdk.LoadRewardedAd(_rewardedUnit);
            };
        }

        private void HookInterstitial()
        {
            MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += (_, __) => _interstitialRetry = 0;
            MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += (_, __) => RetryLoad(ref _interstitialRetry, () => MaxSdk.LoadInterstitial(_interstitialUnit));
            MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += (_, __) =>
            {
                Complete(ref _pendingInterstitial, AdShowResult.Completed);
                MaxSdk.LoadInterstitial(_interstitialUnit);
            };
            MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += (_, __, ___) =>
            {
                Complete(ref _pendingInterstitial, AdShowResult.Failed);
                MaxSdk.LoadInterstitial(_interstitialUnit);
            };
        }

        private static void Complete(ref Action<AdShowResult> pending, AdShowResult result)
        {
            var callback = pending;
            pending = null;
            callback?.Invoke(result);
        }

        private static void RetryLoad(ref int attempt, Action load)
        {
            attempt = Math.Min(attempt + 1, 6);
            var delay = (float)Math.Pow(2, attempt); // backoff exponencial: 2s..64s
            AdRetryRunner.Schedule(delay, load);
        }
    }

    /// <summary>Executa recargas com atraso sem depender de MonoBehaviour do jogo.</summary>
    internal sealed class AdRetryRunner : MonoBehaviour
    {
        private static AdRetryRunner _instance;

        public static void Schedule(float seconds, Action action)
        {
            if (_instance == null)
            {
                var go = new GameObject("[AdRetryRunner]");
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<AdRetryRunner>();
            }
            _instance.StartCoroutine(_instance.Run(seconds, action));
        }

        private System.Collections.IEnumerator Run(float seconds, Action action)
        {
            yield return new WaitForSecondsRealtime(seconds);
            action();
        }
    }
}
#endif
