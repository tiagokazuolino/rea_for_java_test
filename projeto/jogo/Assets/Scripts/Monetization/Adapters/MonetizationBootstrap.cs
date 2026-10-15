using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;
using ZombieStreamer.Economy;

namespace ZombieStreamer.Monetization.Adapters
{
    /// <summary>
    /// Monta a monetização a partir dos JSON de design (Assets/Resources/Config/, como TextAsset).
    /// Regras: anúncio só quando o jogador toca no botão e só dá efeito temporário;
    /// posse permanente só por conquista ou compra. Não há intersticial nem banner.
    /// Requer o pacote com.unity.nuget.newtonsoft-json.
    /// </summary>
    public sealed class MonetizationBootstrap : MonoBehaviour
    {
        [SerializeField] private TextAsset adPlacementsJson;
        [SerializeField] private TextAsset iapCatalogJson;
        [SerializeField] private bool useFakesInEditor = true;

        [Header("AppLovin MAX - só ad unit Rewarded (preencher no Inspector; não versionar chaves reais em texto)")]
        [SerializeField] private string maxSdkKey;
        [SerializeField] private string rewardedAdUnit;

        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            Converters = { new StringEnumConverter() },
            MissingMemberHandling = MissingMemberHandling.Ignore,
        };

        public RewardedOfferService Offers { get; private set; }
        public PurchaseRewardHandler Purchases { get; private set; }
        public TemporaryEffects Effects { get; private set; }
        public IAdService Ads { get; private set; }
        public IIapService Iap { get; private set; }

        /// <summary>Chamado pelo GameBootstrap depois de carregar o save.</summary>
        /// <param name="persist">Grava o save de forma síncrona (retorna false se falhar); usado nas compras.</param>
        public void Build(PlayerWallet wallet, IGameplayRewardSink sink, Func<bool> persist)
        {
            if (wallet == null)
                throw new ArgumentNullException(nameof(wallet));
            var adsFile = Parse<AdPlacementsFile>(adPlacementsJson, "ad_placements");
            var catalog = Parse<IapCatalogFile>(iapCatalogJson, "iap_catalogo");
            MonetizationConfigValidator.Validate(adsFile, catalog); // falha cedo se um anúncio daria algo permanente

            var clock = new UnityClock();
            var analytics = new DebugAnalytics();
            Effects = new TemporaryEffects(wallet.Snapshot.temporaryEffects, clock);
            Effects.Prune();
            var granter = new RewardGranter(wallet, Effects, sink);

            (Ads, Iap) = CreateServices();
            Offers = new RewardedOfferService(adsFile.placements, Ads, wallet, granter, clock, analytics);
            Purchases = new PurchaseRewardHandler(catalog.products, Iap, wallet, granter, analytics, persist);

            Ads.Initialize(null);
            Iap.Initialize(catalog.products, null);
        }

        private void Update() => Effects?.Prune(); // dispara Expired → UI oferece "conquistar ou comprar"

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
            ads = new MaxAdService(maxSdkKey, rewardedAdUnit);
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
