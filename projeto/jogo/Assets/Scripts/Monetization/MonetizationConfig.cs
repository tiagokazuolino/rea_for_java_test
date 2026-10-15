using System;
using System.Collections.Generic;
using ZombieStreamer.Economy;

namespace ZombieStreamer.Monetization
{
    // DTOs espelhando design/monetizacao/*.json (cópia em Assets/Resources/Config/).

    /// <summary>Um botão "assistir para ganhar temporariamente". Não existem intersticiais nem banners.</summary>
    [Serializable]
    public sealed class AdPlacementConfig
    {
        public string id;
        public bool ticketSkippable;
        /// <summary>A UI precisa informar o alvo (qual roupa/item/skill testar).</summary>
        public bool requiresChoice;
        public RewardDefinition reward;
        public int cooldownSeconds;
    }

    [Serializable]
    public sealed class AdPlacementsFile
    {
        public List<AdPlacementConfig> placements = new List<AdPlacementConfig>();
    }

    public enum ProductKind
    {
        Consumable,
        NonConsumable,
    }

    [Serializable]
    public sealed class IapProductConfig
    {
        public string productId;
        public ProductKind type;
        public string name;
        public string priceTier;
        public string badge;
        public bool oncePerAccount;
        /// <summary>A UI informa o alvo antes da compra (ex.: a roupa que o jogador acabou de testar).</summary>
        public bool requiresChoice;
        public List<RewardDefinition> rewards = new List<RewardDefinition>();
    }

    [Serializable]
    public sealed class IapCatalogFile
    {
        public List<IapProductConfig> products = new List<IapProductConfig>();
    }

    /// <summary>Checa no startup se o JSON respeita a regra "anúncio = só temporário".</summary>
    public static class MonetizationConfigValidator
    {
        public static void Validate(AdPlacementsFile ads, IapCatalogFile iap)
        {
            foreach (var p in ads.placements)
            {
                if (p.reward == null)
                    throw new RewardPolicyException($"Placement '{p.id}' sem recompensa.");
                if (!RewardRules.IsTemporary(p.reward.type))
                    throw new RewardPolicyException(
                        $"Placement '{p.id}' daria {p.reward.type} (permanente). Anúncio só pode dar efeito temporário.");
                if (p.reward.durationSeconds <= 0)
                    throw new RewardPolicyException($"Placement '{p.id}' precisa de durationSeconds > 0.");
                if (RewardRules.RequiresTarget(p.reward.type) && !p.requiresChoice)
                    throw new RewardPolicyException($"Placement '{p.id}' precisa de requiresChoice=true.");
            }
            foreach (var product in iap.products)
            {
                if (product.rewards.Count == 0)
                    throw new RewardPolicyException($"Produto '{product.productId}' sem recompensa.");
                foreach (var r in product.rewards)
                    if (RewardRules.RequiresTarget(r.type) && !r.HasTarget && !product.requiresChoice)
                        throw new RewardPolicyException($"Produto '{product.productId}' precisa de alvo ou requiresChoice.");
            }
        }
    }
}
