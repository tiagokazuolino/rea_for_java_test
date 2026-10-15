using System;
using System.Collections.Generic;

namespace ZombieStreamer.Economy
{
    public interface IClock
    {
        double NowSeconds { get; }
    }

    public enum EffectKind
    {
        Booster,
        OutfitTrial,
        ItemTrial,
        SkillTrial,
        PkRetry,
    }

    /// <summary>Efeito com prazo de validade (persistido no save).</summary>
    [Serializable]
    public sealed class TemporaryEffectState
    {
        public EffectKind kind;
        /// <summary>Booster: nome do BoosterType; OutfitTrial: índice da roupa; Item/Skill: alvo.</summary>
        public string key;
        public int value;
        public double expiresAt;
    }

    /// <summary>
    /// Tudo que um anúncio concede vive aqui e expira sozinho. O gameplay CONSULTA estes efeitos
    /// (multiplicadores, roupa em teste, níveis bônus) em vez de alterar posse ou nível real.
    /// </summary>
    public sealed class TemporaryEffects
    {
        private readonly List<TemporaryEffectState> _effects;
        private readonly IClock _clock;

        public TemporaryEffects(List<TemporaryEffectState> persistedEffects, IClock clock)
        {
            _effects = persistedEffects ?? throw new ArgumentNullException(nameof(persistedEffects));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        /// <summary>Disparado quando um efeito acaba (para a UI oferecer "conquistar ou comprar").</summary>
        public event Action<TemporaryEffectState> Expired;

        /// <summary>Concede ou estende. Repetir o mesmo efeito soma o tempo restante.</summary>
        public void Add(EffectKind kind, string key, int value, int durationSeconds)
        {
            if (durationSeconds <= 0)
                throw new RewardPolicyException($"Efeito temporário {kind} sem duração.");
            Prune();
            var now = _clock.NowSeconds;
            if (kind == EffectKind.OutfitTrial)
                _effects.RemoveAll(e => e.kind == EffectKind.OutfitTrial && e.key != key); // uma roupa em teste por vez
            var existing = _effects.Find(e => e.kind == kind && e.key == key);
            if (existing == null)
            {
                _effects.Add(new TemporaryEffectState { kind = kind, key = key, value = value, expiresAt = now + durationSeconds });
                return;
            }
            existing.value = kind == EffectKind.PkRetry ? existing.value + value : Math.Max(existing.value, value);
            existing.expiresAt = Math.Max(existing.expiresAt, now) + durationSeconds;
        }

        public bool IsActive(EffectKind kind, string key) => Remaining(kind, key) > 0;

        public double Remaining(EffectKind kind, string key)
        {
            var effect = _effects.Find(e => e.kind == kind && e.key == key);
            return effect == null ? 0 : Math.Max(0, effect.expiresAt - _clock.NowSeconds);
        }

        public bool IsBoosterActive(BoosterType booster) => IsActive(EffectKind.Booster, booster.ToString());

        /// <summary>Níveis extras temporários de um item/skill (somar ao nível real só no cálculo de renda).</summary>
        public int BonusLevels(EffectKind kind, string target)
        {
            var effect = _effects.Find(e => e.kind == kind && e.key == target);
            return effect != null && effect.expiresAt > _clock.NowSeconds ? effect.value : 0;
        }

        /// <summary>Roupa vestida em teste (-1 se nenhuma). Não entra em "roupas possuídas".</summary>
        public int ActiveOutfitTrial()
        {
            var now = _clock.NowSeconds;
            var effect = _effects.Find(e => e.kind == EffectKind.OutfitTrial && e.expiresAt > now);
            return effect != null && int.TryParse(effect.key, out var index) ? index : -1;
        }

        /// <summary>Consome uma revanche de PK válida.</summary>
        public bool TryConsumePkRetry()
        {
            Prune();
            var effect = _effects.Find(e => e.kind == EffectKind.PkRetry);
            if (effect == null)
                return false;
            effect.value--;
            if (effect.value <= 0)
                _effects.Remove(effect);
            return true;
        }

        /// <summary>Remove vencidos e avisa. Chamar no Update/tick do jogo e ao carregar o save.</summary>
        public void Prune()
        {
            var now = _clock.NowSeconds;
            for (var i = _effects.Count - 1; i >= 0; i--)
            {
                if (_effects[i].expiresAt > now)
                    continue;
                var expired = _effects[i];
                _effects.RemoveAt(i);
                Expired?.Invoke(expired);
            }
        }
    }
}
