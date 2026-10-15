#if ZS_APPLOVIN_MAX
using System;
using UnityEngine;

namespace ZombieStreamer.Monetization.Adapters
{
    /// <summary>
    /// Adaptador AppLovin MAX: SOMENTE vídeo recompensado, aberto pelo jogador.
    /// Não cria intersticial nem banner (no painel do MAX, crie apenas ad units do tipo Rewarded).
    /// Ative com o define ZS_APPLOVIN_MAX depois de importar o plugin MAX.
    /// Não compilado neste repositório; conferir contra a versão do plugin instalada.
    /// </summary>
    public sealed class MaxAdService : IAdService
    {
        private readonly string _sdkKey;
        private readonly string _rewardedUnit;
        private Action<AdShowResult> _pending;
        private bool _rewardEarned;
        private int _retry;

        public MaxAdService(string sdkKey, string rewardedUnit)
        {
            _sdkKey = sdkKey;
            _rewardedUnit = rewardedUnit;
        }

        public bool IsRewardedReady => MaxSdk.IsRewardedAdReady(_rewardedUnit);

        public void Initialize(Action onReady)
        {
            MaxSdkCallbacks.OnSdkInitializedEvent += _ =>
            {
                Hook();
                MaxSdk.LoadRewardedAd(_rewardedUnit);
                onReady?.Invoke();
            };
            MaxSdk.SetSdkKey(_sdkKey);
            MaxSdk.InitializeSdk();
        }

        public void ShowRewarded(string placementId, Action<AdShowResult> onFinished)
        {
            if (_pending != null)
            {
                onFinished?.Invoke(AdShowResult.Failed); // já há um vídeo em exibição
                return;
            }
            if (!IsRewardedReady)
            {
                onFinished?.Invoke(AdShowResult.NotLoaded);
                return;
            }
            _rewardEarned = false;
            _pending = onFinished;
            MaxSdk.ShowRewardedAd(_rewardedUnit, placementId);
        }

        private void Hook()
        {
            MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += (_, __) => _retry = 0;
            MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += (_, __) => RetryLoad();
            MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += (_, __, ___) => _rewardEarned = true;
            MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += (_, __) =>
            {
                Complete(_rewardEarned ? AdShowResult.Completed : AdShowResult.Skipped);
                MaxSdk.LoadRewardedAd(_rewardedUnit);
            };
            MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += (_, __, ___) =>
            {
                Complete(AdShowResult.Failed);
                MaxSdk.LoadRewardedAd(_rewardedUnit);
            };
        }

        private void Complete(AdShowResult result)
        {
            var callback = _pending;
            _pending = null;
            callback?.Invoke(result);
        }

        private void RetryLoad()
        {
            _retry = Math.Min(_retry + 1, 6);
            var delay = (float)Math.Pow(2, _retry); // backoff exponencial: 2s..64s
            AdRetryRunner.Schedule(delay, () => MaxSdk.LoadRewardedAd(_rewardedUnit));
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
