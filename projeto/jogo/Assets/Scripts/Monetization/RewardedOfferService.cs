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
        /// <summary>Erro ao aplicar o prêmio; o ticket (se usado) foi devolvido.</summary>
        DeliveryFailed,
        /// <summary>Placement de teste (roupa/item/skill) chamado sem dizer o alvo.</summary>
        MissingChoice,
    }

    /// <summary>
    /// "Assista e ganhe temporariamente": o jogador ESCOLHE tocar num botão e recebe um efeito com prazo
    /// (booster, roupa em teste, item/skill com níveis extras). Pode gastar 1 ticket comprado para pular o vídeo.
    /// Nunca concede posse permanente: o RewardGranter recusa e o config é validado no startup.
    /// </summary>
    public sealed class RewardedOfferService
    {
        private readonly Dictionary<string, AdPlacementConfig> _placements = new Dictionary<string, AdPlacementConfig>();
        private readonly HashSet<string> _inFlight = new HashSet<string>();
        private bool _adShowing; // o SDK só exibe um vídeo por vez, para qualquer placement
        private readonly IAdService _ads;
        private readonly PlayerWallet _wallet;
        private readonly RewardGranter _granter;
        private readonly IClock _clock;
        private readonly IAnalytics _analytics;

        public RewardedOfferService(IEnumerable<AdPlacementConfig> placements, IAdService ads, PlayerWallet wallet,
            RewardGranter granter, IClock clock, IAnalytics analytics)
        {
            foreach (var p in placements ?? throw new ArgumentNullException(nameof(placements)))
            {
                if (p.reward == null || !RewardRules.IsTemporary(p.reward.type))
                    throw new RewardPolicyException($"Placement '{p.id}' precisa dar um efeito temporário.");
                if (p.reward.durationSeconds <= 0)
                    throw new RewardPolicyException($"Placement '{p.id}' precisa de durationSeconds > 0.");
                if (RewardRules.RequiresTarget(p.reward.type) && !p.requiresChoice)
                    throw new RewardPolicyException($"Placement '{p.id}' precisa de requiresChoice=true.");
                _placements[p.id] = p;
            }
            _ads = ads ?? throw new ArgumentNullException(nameof(ads));
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _granter = granter ?? throw new ArgumentNullException(nameof(granter));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _analytics = analytics ?? throw new ArgumentNullException(nameof(analytics));
        }

        /// <summary>Para a UI decidir se o botão fica ativo.</summary>
        public bool IsAvailable(string placementId) =>
            _placements.TryGetValue(placementId, out var p) && !IsOnCooldown(p) && !_inFlight.Contains(placementId)
            && ((p.ticketSkippable && _wallet.Tickets > 0) || _ads.IsRewardedReady);

        /// <summary>Para a UI mostrar ícone de ticket em vez de ícone de vídeo.</summary>
        public bool WillUseTicket(string placementId) =>
            _placements.TryGetValue(placementId, out var p) && p.ticketSkippable && _wallet.Tickets > 0;

        public double CooldownRemaining(string placementId) =>
            _placements.TryGetValue(placementId, out var p) && p.cooldownSeconds > 0 && _wallet.TryGetLastClaim(p.id, out var last)
                ? Math.Max(0, p.cooldownSeconds - Math.Abs(_clock.NowSeconds - last))
                : 0;

        /// <param name="target">Item ("bunker:4") ou skill ("talk") a testar, quando o placement exige escolha.</param>
        /// <param name="outfitIndex">Roupa a testar, quando o placement exige escolha.</param>
        public void Claim(string placementId, Action<OfferOutcome> onDone, string target = null, int outfitIndex = -1)
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
            var reward = placement.requiresChoice ? placement.reward.WithTarget(target, outfitIndex) : placement.reward;
            if (RewardRules.RequiresTarget(reward.type) && !reward.HasTarget)
            {
                Finish(placementId, OfferOutcome.MissingChoice, onDone, release: false);
                return;
            }
            try
            {
                RewardGranter.Validate(reward, RewardSource.RewardedAd); // ANTES de gastar ticket ou mostrar vídeo
            }
            catch (RewardPolicyException)
            {
                Finish(placementId, OfferOutcome.DeliveryFailed, onDone, release: false);
                return;
            }
            _inFlight.Add(placementId);

            if (placement.ticketSkippable && _wallet.TrySpendTicket())
            {
                var byTicket = Deliver(placement, reward, RewardSource.Ticket);
                if (!byTicket)
                    _wallet.AddTickets(1); // devolve o ticket
                Finish(placementId, byTicket ? OfferOutcome.GrantedByTicket : OfferOutcome.DeliveryFailed, onDone);
                return;
            }
            if (_adShowing)
            {
                Finish(placementId, OfferOutcome.AlreadyInProgress, onDone);
                return;
            }
            if (!_ads.IsRewardedReady)
            {
                Finish(placementId, OfferOutcome.AdNotReady, onDone);
                return;
            }
            _adShowing = true;
            _ads.ShowRewarded(placementId, result =>
            {
                _adShowing = false;
                if (result != AdShowResult.Completed)
                {
                    Finish(placementId, OfferOutcome.AdNotCompleted, onDone);
                    return;
                }
                var byAd = Deliver(placement, reward, RewardSource.RewardedAd);
                Finish(placementId, byAd ? OfferOutcome.GrantedByAd : OfferOutcome.DeliveryFailed, onDone);
            });
        }

        private bool IsOnCooldown(AdPlacementConfig p) =>
            p.cooldownSeconds > 0 && _wallet.TryGetLastClaim(p.id, out var last)
            && Math.Abs(_clock.NowSeconds - last) < p.cooldownSeconds; // Abs: relógio atrasado não zera o cooldown

        /// <summary>Aplica o prêmio; nunca deixa exceção escapar para o callback do SDK nem travar o placement.</summary>
        private bool Deliver(AdPlacementConfig p, RewardDefinition reward, RewardSource source)
        {
            try
            {
                _granter.Grant(new[] { reward }, source);
            }
            catch (Exception ex)
            {
                _analytics.Log("rewarded_delivery_error", new Dictionary<string, object>
                {
                    { "placement", p.id },
                    { "error", ex.GetType().Name },
                });
                return false;
            }
            _wallet.SetLastClaim(p.id, _clock.NowSeconds); // cooldown só conta se o prêmio foi entregue
            return true;
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
