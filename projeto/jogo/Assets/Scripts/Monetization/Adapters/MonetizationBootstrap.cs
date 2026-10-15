using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;
using ZombieStreamer.Economy;

namespace ZombieStreamer.Monetization.Adapters
{
    /// <summary>
    /// Monta o sistema de monetização a partir dos JSON de design.
    /// Coloque ad_placements.json e iap_catalogo.json em Assets/Resources/Config/ (como TextAsset).
    /// Requer o pacote com.unity.nuget.newtonsoft-json (o original também usa Newtonsoft).
    /// </summary>
    public sealed class MonetizationBootstrap : MonoBehaviour
    {
        [SerializeField] private TextAsset adPlacementsJson;
        [SerializeField] private TextAsset iapCatalogJson;
        [SerializeField] private bool useFakesInEditor = true;

        [Header("AppLovin MAX (preencher no Inspector; não versionar chaves reais em texto)")]
        [SerializeField] private string maxSdkKey;
        [SerializeField] private string rewardedAdUnit;
        [SerializeField] private string interstitialAdUnit;
        [SerializeField] private string bannerAdUnit;

        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            Converters = { new StringEnumConverter() },
            MissingMemberHandling = MissingMemberHandling.Ignore,
        };

        public RewardedOfferService Offers { get; private set; }
        public PurchaseRewardHandler Purchases { get; private set; }
        public InterstitialPacer Pacer { get; private set; }
        public IAdService Ads { get; private set; }
        public IIapService Iap { get; private set; }

        private readonly HashSet<string> _interstitialTriggers = new HashSet<string>();
        private bool _bannerHidden;
        private bool _interstitialShowing;

        /// <summary>Chamado pelo GameBootstrap depois de carregar o save.</summary>
        /// <param name="persist">Grava o save de forma síncrona (retorna false se falhar); usado antes de confirmar compras.</param>
        public void Build(PlayerWallet wallet, IGameplayRewardSink sink, double installTimeSeconds, Func<bool> persist)
        {
            if (wallet == null)
                throw new ArgumentNullException(nameof(wallet));
            var adsFile = Parse<AdPlacementsFile>(adPlacementsJson, "ad_placements");
            var catalog = Parse<IapCatalogFile>(iapCatalogJson, "iap_catalogo");
            var clock = new UnityClock();
            var analytics = new DebugAnalytics();
            var granter = new RewardGranter(wallet, sink);

            (Ads, Iap) = CreateServices();
            Pacer = new InterstitialPacer(adsFile.interstitialPacing, clock, installTimeSeconds);
            Offers = new RewardedOfferService(adsFile.placements, Ads, wallet, granter, Pacer, clock, analytics);
            Purchases = new PurchaseRewardHandler(catalog.products, Iap, wallet, granter, analytics, persist);
            foreach (var p in adsFile.placements)
                if (p.kind == AdKind.Interstitial)
                    _interstitialTriggers.UnionWith(p.triggers);

            Ads.Initialize(() =>
            {
                _bannerHidden = wallet.HasNoAds;
                Ads.SetBannerVisible(!_bannerHidden);
                // Esconde o banner uma única vez, na transição para no-ads (não a cada moeda ganha).
                wallet.Changed += () =>
                {
                    if (wallet.HasNoAds && !_bannerHidden)
                    {
                        _bannerHidden = true;
                        Ads.SetBannerVisible(false);
                    }
                };
            });
            Iap.Initialize(catalog.products, null);
        }

        /// <summary>Chamar em pausas naturais (fechar loja, trocar de sala, fim de história/PK).</summary>
        public void TryShowInterstitial(string trigger, bool hasNoAds)
        {
            if (!_interstitialTriggers.Contains(trigger))
            {
                Debug.LogWarning($"[Ads] Gatilho de intersticial '{trigger}' não está em ad_placements.json.");
                return;
            }
            if (_interstitialShowing || !Pacer.CanShow(hasNoAds, isNaturalBreak: true) || !Ads.IsInterstitialReady)
                return;
            _interstitialShowing = true;
            Ads.ShowInterstitial("is_" + trigger, result =>
            {
                _interstitialShowing = false;
                if (result == AdShowResult.Completed)
                    Pacer.NotifyInterstitialShown();
            });
        }

        private (IAdService, IIapService) CreateServices()
        {
#if UNITY_EDITOR
            if (useFakesInEditor)
                return (new FakeAdService(), new FakeIapService());
#endif
            // Sem SDK configurado o build NÃO pode dar recompensas de graça: tudo fica indisponível.
            IAdService ads = new UnavailableAdService();
            IIapService iap = new UnavailableIapService();
#if ZS_APPLOVIN_MAX
            ads = new MaxAdService(maxSdkKey, rewardedAdUnit, interstitialAdUnit, bannerAdUnit);
#endif
#if ZS_UNITY_IAP
            iap = new UnityIapService();
#endif
            return (ads, iap);
        }

        private static T Parse<T>(TextAsset asset, string name) where T : new()
        {
            if (asset == null)
                throw new InvalidOperationException($"Config '{name}' não atribuída no MonetizationBootstrap.");
            return JsonConvert.DeserializeObject<T>(asset.text, JsonSettings) ?? new T();
        }
    }
}
