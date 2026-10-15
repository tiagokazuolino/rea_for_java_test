using System;
using System.Collections.Generic;

namespace ZombieStreamer.Economy
{
    /// <summary>Tipos de recompensa. Veja <see cref="RewardRules"/> para o que é temporário e o que é permanente.</summary>
    public enum RewardType
    {
        /// <summary>Valor padrão quando o JSON omite "type": sempre rejeitado.</summary>
        None,

        // Permanentes: só por conquista no jogo ou compra.
        Coins,
        Tickets,
        Outfit,
        ItemLevels,
        SkillLevels,
        PermanentIncomeMultiplier,

        // Temporários: os únicos que um anúncio (ou ticket) pode dar.
        Booster,
        OutfitTrial,
        ItemTrial,
        SkillTrial,
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

    /// <summary>De onde veio a recompensa.</summary>
    public enum RewardSource
    {
        /// <summary>Conquistado jogando: quests, rank, compras com moedas do jogo, vitória no PK.</summary>
        Gameplay,
        Purchase,
        RewardedAd,
        /// <summary>Ticket gasto no lugar de assistir o vídeo (mesma regra do anúncio).</summary>
        Ticket,
    }

    /// <summary>Regra central do jogo: anúncio só dá vantagem temporária; posse permanente se conquista ou se compra.</summary>
    public static class RewardRules
    {
        public static bool IsTemporary(RewardType type) =>
            type == RewardType.Booster || type == RewardType.OutfitTrial || type == RewardType.ItemTrial
            || type == RewardType.SkillTrial || type == RewardType.PkRetry;

        public static bool IsAllowed(RewardType type, RewardSource source) =>
            IsTemporary(type) || source == RewardSource.Gameplay || source == RewardSource.Purchase;

        /// <summary>Tipos em que a UI precisa dizer o alvo (qual roupa, item ou skill).</summary>
        public static bool RequiresTarget(RewardType type) =>
            type == RewardType.OutfitTrial || type == RewardType.ItemTrial || type == RewardType.SkillTrial
            || type == RewardType.Outfit || type == RewardType.ItemLevels || type == RewardType.SkillLevels;
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
        /// <summary>Item ("sala:indice", ex. "bunker:4") ou skill ("talk").</summary>
        public string target;
        public int levels = 1;
        public double multiplier = 1;

        public bool HasTarget => type == RewardType.Outfit || type == RewardType.OutfitTrial
            ? outfitIndex >= 0
            : !string.IsNullOrEmpty(target);

        /// <summary>Cópia com o alvo escolhido pelo jogador (roupa, item ou skill).</summary>
        public RewardDefinition WithTarget(string newTarget = null, int newOutfitIndex = -1)
        {
            var copy = (RewardDefinition)MemberwiseClone();
            if (newTarget != null)
                copy.target = newTarget;
            if (newOutfitIndex >= 0)
                copy.outfitIndex = newOutfitIndex;
            return copy;
        }

        public static RewardDefinition Coins(double amount) => new RewardDefinition { type = RewardType.Coins, amount = amount };
        public static RewardDefinition Tickets(int amount) => new RewardDefinition { type = RewardType.Tickets, amount = amount };

        public static RewardDefinition Booster(BoosterType booster, int seconds) =>
            new RewardDefinition { type = RewardType.Booster, booster = booster, durationSeconds = seconds };
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

    /// <summary>Tentativa de dar algo permanente por anúncio/ticket, ou recompensa malformada.</summary>
    public sealed class RewardPolicyException : InvalidOperationException
    {
        public RewardPolicyException(string message) : base(message) { }
    }
}
