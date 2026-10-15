using System;
using System.Collections.Generic;

namespace ZombieStreamer.Economy
{
    /// <summary>Partes do jogo que guardam níveis reais e outros sistemas (o RewardGranter não conhece as salas).</summary>
    public interface IGameplayRewardSink
    {
        /// <summary>Renda atual por minuto, usada em recompensas "coinsScaledToIncome".</summary>
        double IncomePerMinute { get; }

        /// <summary>Níveis permanentes (só chega aqui vindo de compra ou conquista).</summary>
        void GrantItemLevels(string itemKey, int levels);

        void GrantSkillLevels(string skillId, int levels);
    }

    /// <summary>
    /// Único ponto que credita recompensas. Aplica a regra do jogo:
    /// anúncio/ticket só dá efeito TEMPORÁRIO; posse permanente vem de conquista (Gameplay) ou compra (Purchase).
    /// Valida o lote inteiro antes de aplicar qualquer coisa, para nunca entregar pela metade.
    /// </summary>
    public sealed class RewardGranter
    {
        private readonly PlayerWallet _wallet;
        private readonly TemporaryEffects _effects;
        private readonly IGameplayRewardSink _sink;

        public RewardGranter(PlayerWallet wallet, TemporaryEffects effects, IGameplayRewardSink sink)
        {
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _effects = effects ?? throw new ArgumentNullException(nameof(effects));
            _sink = sink ?? throw new ArgumentNullException(nameof(sink));
        }

        public event Action<GrantResult> Granted;

        public GrantResult Grant(IReadOnlyList<RewardDefinition> rewards, RewardSource source)
        {
            if (rewards == null)
                throw new ArgumentNullException(nameof(rewards));
            foreach (var reward in rewards)
                Validate(reward, source);
            // Moedas escaladas pela renda dependem do gameplay: checa antes de aplicar qualquer item do lote.
            foreach (var reward in rewards)
                if (reward.type == RewardType.Coins && reward.coinsScaledToIncome
                    && !IsFiniteNonNegative(_sink.IncomePerMinute * reward.minutes))
                    throw new RewardPolicyException("Renda atual inválida para calcular moedas.");
            var applied = new List<string>();
            foreach (var reward in rewards)
                applied.Add(Apply(reward));
            var result = new GrantResult(source, applied);
            Granted?.Invoke(result);
            return result;
        }

        public static void Validate(RewardDefinition r, RewardSource source)
        {
            if (r == null || r.type == RewardType.None)
                throw new RewardPolicyException("Recompensa nula ou sem tipo.");
            if (!RewardRules.IsAllowed(r.type, source))
                throw new RewardPolicyException(
                    $"{r.type} é permanente e não pode vir de {source}: só por conquista no jogo ou compra.");
            if (RewardRules.RequiresTarget(r.type) && !r.HasTarget)
                throw new RewardPolicyException($"{r.type} precisa de alvo (roupa, item ou skill escolhido).");
            if (RewardRules.IsTemporary(r.type) && r.durationSeconds <= 0)
                throw new RewardPolicyException($"{r.type} é temporário e precisa de durationSeconds > 0.");
            if (!IsFiniteNonNegative(r.amount) || !IsFiniteNonNegative(r.minutes) || r.levels < 0
                || double.IsNaN(r.multiplier) || double.IsInfinity(r.multiplier) || r.multiplier < 1)
                throw new RewardPolicyException($"{r.type} tem valores numéricos inválidos.");
        }

        private static bool IsFiniteNonNegative(double v) => !double.IsNaN(v) && !double.IsInfinity(v) && v >= 0;

        private string Apply(RewardDefinition r)
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
                case RewardType.Outfit:
                    _wallet.GrantOutfit(r.outfitIndex);
                    return $"outfit:{r.outfitIndex}";
                case RewardType.ItemLevels:
                    _sink.GrantItemLevels(r.target, Math.Max(1, r.levels));
                    return $"item:{r.target}:+{r.levels}";
                case RewardType.SkillLevels:
                    _sink.GrantSkillLevels(r.target, Math.Max(1, r.levels));
                    return $"skill:{r.target}:+{r.levels}";
                case RewardType.PermanentIncomeMultiplier:
                    _wallet.SetPermanentIncomeMultiplier(r.multiplier);
                    return $"income_x{r.multiplier}";
                case RewardType.Booster:
                    _effects.Add(EffectKind.Booster, r.booster.ToString(), 1, r.durationSeconds);
                    return $"booster:{r.booster}:{r.durationSeconds}s";
                case RewardType.OutfitTrial:
                    _effects.Add(EffectKind.OutfitTrial, r.outfitIndex.ToString(), 1, r.durationSeconds);
                    return $"outfit_trial:{r.outfitIndex}:{r.durationSeconds}s";
                case RewardType.ItemTrial:
                    _effects.Add(EffectKind.ItemTrial, r.target, Math.Max(1, r.levels), r.durationSeconds);
                    return $"item_trial:{r.target}:+{r.levels}:{r.durationSeconds}s";
                case RewardType.SkillTrial:
                    _effects.Add(EffectKind.SkillTrial, r.target, Math.Max(1, r.levels), r.durationSeconds);
                    return $"skill_trial:{r.target}:+{r.levels}:{r.durationSeconds}s";
                case RewardType.PkRetry:
                    _effects.Add(EffectKind.PkRetry, "pk", 1, r.durationSeconds);
                    return $"pk_retry:{r.durationSeconds}s";
                default:
                    throw new RewardPolicyException($"Tipo de recompensa desconhecido: {r.type}");
            }
        }
    }
}
