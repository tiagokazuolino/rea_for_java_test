using System;

namespace ZombieStreamer.Monetization
{
    /// <summary>
    /// Decide se um intersticial pode aparecer agora. O original tem "Interstitial pacing";
    /// aqui as regras vêm de ad_placements.json e podem ser ajustadas via Remote Config.
    /// </summary>
    public sealed class InterstitialPacer
    {
        private readonly InterstitialPacingConfig _config;
        private readonly IClock _clock;
        private readonly double _installTime;
        private double _lastInterstitial = double.NegativeInfinity;
        private double _lastRewarded = double.NegativeInfinity;
        private int _shownThisSession;

        public InterstitialPacer(InterstitialPacingConfig config, IClock clock, double installTimeSeconds)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _installTime = installTimeSeconds;
        }

        public bool CanShow(bool hasNoAds, bool isNaturalBreak)
        {
            if (hasNoAds)
                return false;
            if (_config.onlyOnNaturalBreaks && !isNaturalBreak)
                return false;
            if (_shownThisSession >= _config.maxPerSession)
                return false;
            var now = _clock.NowSeconds;
            return now - _installTime >= _config.graceSecondsAfterInstall
                && now - _lastInterstitial >= _config.minSecondsBetween
                && now - _lastRewarded >= _config.minSecondsAfterRewarded;
        }

        public void NotifyInterstitialShown()
        {
            _lastInterstitial = _clock.NowSeconds;
            _shownThisSession++;
        }

        /// <summary>Quem acabou de ver um rewarded não deve levar intersticial logo em seguida.</summary>
        public void NotifyRewardedShown() => _lastRewarded = _clock.NowSeconds;
    }
}
