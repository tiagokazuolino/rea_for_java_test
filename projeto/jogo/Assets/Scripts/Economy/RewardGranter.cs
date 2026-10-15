using System;
using System.Collections.Generic;

namespace ZombieStreamer.Economy
{
    /// <summary>Partes do jogo que a recompensa afeta além da carteira (boosters, upgrades, PK...).</summary>
    public interface IGameplayRewardSink
    {
        /// <summary>Renda atual por minuto, usada em recompensas "coinsScaledToIncome".</summary>
        double IncomePerMinute { get; }

        void ActivateBooster(BoosterType booster, int durationSeconds);
        void GrantFreeUpgrade(string target, int levels);
        void GrantPkRetry();
        int NextLockedOutfitIndex();
    }

    /// <summary>
    /// Único ponto que credita recompensas. Compras, anúncios recompensados, tickets e quests
    /// passam por aqui, então a regra de "ganhar item comprando ou com ads" fica centralizada.
    /// </summary>
    public sealed class RewardGranter
    {
        private readonly PlayerWallet _wallet;
        private readonly IGameplayRewardSink _sink;

        public RewardGranter(PlayerWallet wallet, IGameplayRewardSink sink)
        {
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _sink = sink ?? throw new ArgumentNullException(nameof(sink));
        }

        public event Action<GrantResult> Granted;

        /// <param name="baseAmount">Valor-base para multiplicadores (ex.: doação ou ganho AFK que será multiplicado).</param>
        public GrantResult Grant(IEnumerable<RewardDefinition> rewards, RewardSource source, double baseAmount = 0)
        {
            if (rewards == null)
                throw new ArgumentNullException(nameof(rewards));
            var applied = new List<string>();
            foreach (var reward in rewards)
                applied.Add(Apply(reward, baseAmount));
            var result = new GrantResult(source, applied);
            Granted?.Invoke(result);
            return result;
        }

        private string Apply(RewardDefinition r, double baseAmount)
        {
            switch (r.type)
            {
                case RewardType.Coins:
                    var coins = r.coinsScaledToIncome ? _sink.IncomePerMinute * r.minutes : r.amount;
                    _wallet.AddCoins(coins);
                    return $"coins:{coins}";
                case RewardType.Tickets:
                    _wallet.AddTickets((int)r.amount);
                    return $"tickets:{(int)r.amount}";
                case RewardType.NoAds:
                    _wallet.GrantNoAds();
                    return "noads";
                case RewardType.Booster:
                    _sink.ActivateBooster(r.booster, r.durationSeconds);
                    return $"booster:{r.booster}:{r.durationSeconds}s";
                case RewardType.Outfit:
                    _wallet.GrantOutfit(r.outfitIndex);
                    return $"outfit:{r.outfitIndex}";
                case RewardType.OutfitNext:
                    var next = _sink.NextLockedOutfitIndex();
                    if (next >= 0)
                        _wallet.GrantOutfit(next);
                    return $"outfit:{next}";
                case RewardType.ClaimMultiplier:
                case RewardType.AfkMultiplier:
                    // baseAmount já foi creditado 1x pelo fluxo normal; aqui entra só o extra.
                    var extra = baseAmount * Math.Max(0, r.multiplier - 1);
                    _wallet.AddCoins(extra);
                    return $"multiplier:x{r.multiplier}:+{extra}";
                case RewardType.FreeUpgrade:
                    _sink.GrantFreeUpgrade(string.IsNullOrEmpty(r.target) ? "Skill" : r.target, Math.Max(1, r.levels));
                    return "free_upgrade";
                case RewardType.PkRetry:
                    _sink.GrantPkRetry();
                    return "pk_retry";
                default:
                    throw new ArgumentOutOfRangeException(nameof(r), r.type, "Tipo de recompensa desconhecido");
            }
        }
    }
}
