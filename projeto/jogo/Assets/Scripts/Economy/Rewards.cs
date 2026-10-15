using System;
using System.Collections.Generic;

namespace ZombieStreamer.Economy
{
    /// <summary>Tipos de recompensa que qualquer fonte (compra, anúncio, quest, rank) pode entregar.</summary>
    public enum RewardType
    {
        Coins,
        Tickets,
        NoAds,
        Booster,
        Outfit,
        OutfitNext,
        AfkMultiplier,
        ClaimMultiplier,
        FreeUpgrade,
        PkRetry,
    }

    public enum BoosterType
    {
        TapX4,
        PassiveX4,
        IncomeX10,
        AutoClick,
        DoubleCoin,
    }

    /// <summary>Recompensa declarativa, lida dos JSON de design (iap_catalogo / ad_placements).</summary>
    [Serializable]
    public sealed class RewardDefinition
    {
        public RewardType type;
        public double amount;
        public bool coinsScaledToIncome;
        public double minutes;
        public BoosterType booster;
        public int durationSeconds;
        public int outfitIndex = -1;
        public double multiplier = 1;
        /// <summary>Opções da roleta AFK; a UI sorteia uma e cria a recompensa com <see cref="WithMultiplier"/>.</summary>
        public double[] multipliers;
        public string target;
        public int levels = 1;

        public RewardDefinition WithMultiplier(double value)
        {
            var copy = (RewardDefinition)MemberwiseClone();
            copy.multiplier = value;
            return copy;
        }

        public static RewardDefinition Coins(double amount) => new RewardDefinition { type = RewardType.Coins, amount = amount };
        public static RewardDefinition Tickets(int amount) => new RewardDefinition { type = RewardType.Tickets, amount = amount };
        public static RewardDefinition NoAds() => new RewardDefinition { type = RewardType.NoAds };

        public static RewardDefinition Booster(BoosterType booster, int seconds) =>
            new RewardDefinition { type = RewardType.Booster, booster = booster, durationSeconds = seconds };
    }

    /// <summary>De onde veio a recompensa; usado em analytics e para regras (ex.: compra é sempre entregue).</summary>
    public enum RewardSource
    {
        Purchase,
        RewardedAd,
        Ticket,
        Quest,
        RankUp,
        Gift,
    }

    /// <summary>Resultado imutável do que foi efetivamente creditado.</summary>
    public sealed class GrantResult
    {
        public GrantResult(RewardSource source, IReadOnlyList<string> applied)
        {
            Source = source;
            Applied = applied;
        }

        public RewardSource Source { get; }
        public IReadOnlyList<string> Applied { get; }
    }
}
