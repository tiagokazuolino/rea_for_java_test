using System;
using System.Collections.Generic;
using ZombieStreamer.Economy;

namespace ZombieStreamer.Monetization
{
    public enum OfferOutcome
    {
        GrantedByTicket,
        GrantedByAd,
        AdNotReady,
        AdNotCompleted,
        OnCooldown,
        UnknownPlacement,
        AlreadyInProgress,
        /// <summary>Placement de multiplicador chamado sem o multiplicador sorteado (bug da UI).</summary>
        MissingMultiplier,
    }

    /// <summary>
    /// "Ganhar item com ads": o jogador toca num botão (booster, x2, presente...) e recebe a
    /// recompensa ao assistir um vídeo, ou gastando 1 ticket (comprado na loja) para pular o vídeo.
    /// </summary>
    public sealed class RewardedOfferService
    {
        private readonly Dictionary<string, AdPlacementConfig> _placements = new Dictionary<string, AdPlacementConfig>();
        private readonly HashSet<string> _inFlight = new HashSet<string>();
        private readonly IAdService _ads;
        private readonly PlayerWallet _wallet;
        private readonly RewardGranter _granter;
        private readonly InterstitialPacer _pacer;
        private readonly IClock _clock;
        private readonly IAnalytics _analytics;

        public RewardedOfferService(IEnumerable<AdPlacementConfig> placements, IAdService ads, PlayerWallet wallet,
            RewardGranter granter, InterstitialPacer pacer, IClock clock, IAnalytics analytics)
        {
            foreach (var p in placements ?? throw new ArgumentNullException(nameof(placements)))
                if (p.kind == AdKind.Rewarded)
                    _placements[p.id] = p;
            _ads = ads ?? throw new ArgumentNullException(nameof(ads));
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _granter = granter ?? throw new ArgumentNullException(nameof(granter));
            _pacer = pacer ?? throw new ArgumentNullException(nameof(pacer));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _analytics = analytics ?? throw new ArgumentNullException(nameof(analytics));
        }

        /// <summary>Para a UI decidir se mostra o ícone de vídeo, de ticket, ou o botão desabilitado.</summary>
        public bool IsAvailable(string placementId) =>
            _placements.TryGetValue(placementId, out var p) && !IsOnCooldown(p)
            && ((p.ticketSkippable && _wallet.Tickets > 0) || _ads.IsRewardedReady);

        public bool WillUseTicket(string placementId) =>
            _placements.TryGetValue(placementId, out var p) && p.ticketSkippable && _wallet.Tickets > 0;

        /// <param name="rewardOverride">Ex.: multiplicador sorteado na roleta AFK.</param>
        /// <param name="baseAmount">Valor que será multiplicado (doação, ganho AFK).</param>
        public void Claim(string placementId, Action<OfferOutcome> onDone,
            RewardDefinition rewardOverride = null, double baseAmount = 0)
        {
            // Saídas antes do _inFlight.Add não liberam a trava: pode haver outro resgate em andamento.
            if (!_placements.TryGetValue(placementId, out var placement))
            {
                Finish(placementId, OfferOutcome.UnknownPlacement, onDone, release: false);
                return;
            }
            if (_inFlight.Contains(placementId))
            {
                Finish(placementId, OfferOutcome.AlreadyInProgress, onDone, release: false);
                return;
            }
            if (IsOnCooldown(placement))
            {
                Finish(placementId, OfferOutcome.OnCooldown, onDone, release: false);
                return;
            }
            var reward = rewardOverride ?? placement.reward;
            if (reward.type == RewardType.AfkMultiplier && reward.multiplier <= 1)
            {
                Finish(placementId, OfferOutcome.MissingMultiplier, onDone, release: false);
                return;
            }
            _inFlight.Add(placementId);

            if (placement.ticketSkippable && _wallet.TrySpendTicket())
            {
                Deliver(placement, reward, RewardSource.Ticket, baseAmount);
                Finish(placementId, OfferOutcome.GrantedByTicket, onDone);
                return;
            }
            if (!_ads.IsRewardedReady)
            {
                Finish(placementId, OfferOutcome.AdNotReady, onDone);
                return;
            }
            _ads.ShowRewarded(placementId, result =>
            {
                if (result == AdShowResult.Completed || result == AdShowResult.Skipped)
                    _pacer.NotifyRewardedShown();
                if (result != AdShowResult.Completed)
                {
                    Finish(placementId, OfferOutcome.AdNotCompleted, onDone);
                    return;
                }
                Deliver(placement, reward, RewardSource.RewardedAd, baseAmount);
                Finish(placementId, OfferOutcome.GrantedByAd, onDone);
            });
        }

        private bool IsOnCooldown(AdPlacementConfig p) =>
            p.cooldownSeconds > 0 && _wallet.TryGetLastClaim(p.id, out var last)
            && Math.Abs(_clock.NowSeconds - last) < p.cooldownSeconds; // Abs: relógio atrasado não zera o cooldown

        private void Deliver(AdPlacementConfig p, RewardDefinition reward, RewardSource source, double baseAmount)
        {
            _wallet.SetLastClaim(p.id, _clock.NowSeconds);
            _granter.Grant(new[] { reward }, source, baseAmount);
        }

        private void Finish(string placementId, OfferOutcome outcome, Action<OfferOutcome> onDone, bool release = true)
        {
            if (release)
                _inFlight.Remove(placementId);
            _analytics.Log("rewarded_offer", new Dictionary<string, object>
            {
                { "placement", placementId },
                { "outcome", outcome.ToString() },
            });
            onDone?.Invoke(outcome);
        }
    }
}
