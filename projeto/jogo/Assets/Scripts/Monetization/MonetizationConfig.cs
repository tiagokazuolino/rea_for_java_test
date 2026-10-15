using System;
using System.Collections.Generic;
using ZombieStreamer.Economy;

namespace ZombieStreamer.Monetization
{
    // DTOs espelhando design/monetizacao/*.json (copiar para Assets/Resources/Config/ no projeto Unity).

    public enum AdKind
    {
        Rewarded,
        Interstitial,
        Banner,
    }

    [Serializable]
    public sealed class AdPlacementConfig
    {
        public string id;
        public AdKind kind;
        public bool ticketSkippable;
        public RewardDefinition reward;
        public int cooldownSeconds;
        public List<string> triggers = new List<string>();
    }

    [Serializable]
    public sealed class InterstitialPacingConfig
    {
        public int minSecondsBetween = 90;
        public int minSecondsAfterRewarded = 60;
        public int graceSecondsAfterInstall = 300;
        public int maxPerSession = 8;
        public bool onlyOnNaturalBreaks = true;
    }

    [Serializable]
    public sealed class AdPlacementsFile
    {
        public InterstitialPacingConfig interstitialPacing = new InterstitialPacingConfig();
        public List<AdPlacementConfig> placements = new List<AdPlacementConfig>();
    }

    public enum ProductKind
    {
        Consumable,
        NonConsumable,
        Subscription,
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
        public List<RewardDefinition> rewards = new List<RewardDefinition>();
    }

    [Serializable]
    public sealed class IapCatalogFile
    {
        public List<IapProductConfig> products = new List<IapProductConfig>();
    }
}
