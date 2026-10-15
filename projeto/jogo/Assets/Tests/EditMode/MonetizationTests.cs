using System;
using System.Collections.Generic;
using NUnit.Framework;
using ZombieStreamer.Economy;
using ZombieStreamer.Monetization;

namespace ZombieStreamer.Tests.EditMode
{
    internal sealed class FakeClock : IClock
    {
        public double NowSeconds { get; set; } = 10_000;
    }

    internal sealed class NullAnalytics : IAnalytics
    {
        public readonly List<string> Events = new List<string>();
        public void Log(string eventName, IReadOnlyDictionary<string, object> parameters) => Events.Add(eventName);
    }

    internal sealed class StubSink : IGameplayRewardSink
    {
        public double IncomePerMinute { get; set; } = 100;
        public readonly Dictionary<string, int> ItemLevels = new Dictionary<string, int>();
        public readonly Dictionary<string, int> SkillLevels = new Dictionary<string, int>();

        public void GrantItemLevels(string itemKey, int levels) =>
            ItemLevels[itemKey] = (ItemLevels.TryGetValue(itemKey, out var l) ? l : 0) + levels;

        public void GrantSkillLevels(string skillId, int levels) =>
            SkillLevels[skillId] = (SkillLevels.TryGetValue(skillId, out var l) ? l : 0) + levels;
    }

    internal sealed class StubAds : IAdService
    {
        public bool IsRewardedReady { get; set; } = true;
        public AdShowResult Result { get; set; } = AdShowResult.Completed;
        public bool Deferred { get; set; }
        public Action<AdShowResult> Pending;
        public int RewardedShown;
        public void Initialize(Action onReady) => onReady?.Invoke();

        public void ShowRewarded(string placementId, Action<AdShowResult> onFinished)
        {
            RewardedShown++;
            if (Deferred)
                Pending = onFinished;
            else
                onFinished(Result);
        }
    }

    internal sealed class StubIap : IIapService
    {
        public readonly List<string> Confirmed = new List<string>();
        public readonly List<string> Started = new List<string>();
        public bool AutoComplete { get; set; } = true;
        public event Action<PurchaseReceipt> PurchaseSucceeded;
        public event Action<string, PurchaseFailure> PurchaseFailed;
        public void Initialize(IEnumerable<IapProductConfig> products, Action onReady) => onReady?.Invoke();
        public string GetLocalizedPrice(string productId) => null;

        public void Purchase(string productId)
        {
            Started.Add(productId);
            if (AutoComplete)
                PurchaseSucceeded?.Invoke(new PurchaseReceipt(productId, "tx-" + productId));
        }

        public void Deliver(string productId, string tx) => PurchaseSucceeded?.Invoke(new PurchaseReceipt(productId, tx));
        public void Fail(string productId) => PurchaseFailed?.Invoke(productId, PurchaseFailure.Cancelled);
        public void RestorePurchases() { }
        public void ConfirmDelivered(PurchaseReceipt receipt) => Confirmed.Add(receipt.TransactionId);
    }

    /// <summary>A regra do jogo: anúncio só dá efeito temporário; posse permanente só por conquista ou compra.</summary>
    public sealed class RewardPolicyTests
    {
        private FakeClock _clock;
        private PlayerWallet _wallet;
        private StubSink _sink;
        private TemporaryEffects _effects;
        private RewardGranter _granter;

        [SetUp]
        public void SetUp()
        {
            _clock = new FakeClock();
            _wallet = new PlayerWallet(new WalletState());
            _sink = new StubSink();
            _effects = new TemporaryEffects(_wallet.Snapshot.temporaryEffects, _clock);
            _granter = new RewardGranter(_wallet, _effects, _sink);
        }

        [TestCase(RewardSource.RewardedAd)]
        [TestCase(RewardSource.Ticket)]
        public void AdOrTicket_CannotGrantPermanentThings(RewardSource source)
        {
            var permanent = new[]
            {
                RewardDefinition.Coins(10),
                RewardDefinition.Tickets(1),
                new RewardDefinition { type = RewardType.Outfit, outfitIndex = 2 },
                new RewardDefinition { type = RewardType.ItemLevels, target = "bunker:0", levels = 1 },
                new RewardDefinition { type = RewardType.SkillLevels, target = "talk", levels = 1 },
                new RewardDefinition { type = RewardType.PermanentIncomeMultiplier, multiplier = 2 },
            };
            foreach (var r in permanent)
                Assert.Throws<RewardPolicyException>(() => _granter.Grant(new[] { r }, source), r.type.ToString());
            Assert.AreEqual(0, _wallet.Coins);
            Assert.AreEqual(0, _wallet.Tickets);
            CollectionAssert.IsEmpty(_wallet.Snapshot.ownedOutfits);
            CollectionAssert.IsEmpty(_sink.ItemLevels);
        }

        [Test]
        public void MixedBatch_IsRejectedWhole_NothingApplied()
        {
            var batch = new[] { RewardDefinition.Booster(BoosterType.TapX4, 60), RewardDefinition.Coins(10) };
            Assert.Throws<RewardPolicyException>(() => _granter.Grant(batch, RewardSource.RewardedAd));
            Assert.IsFalse(_effects.IsBoosterActive(BoosterType.TapX4));
        }

        [TestCase(RewardSource.Gameplay)]
        [TestCase(RewardSource.Purchase)]
        public void EarnedOrBought_CanGrantPermanentThings(RewardSource source)
        {
            _granter.Grant(new[]
            {
                RewardDefinition.Coins(10),
                new RewardDefinition { type = RewardType.Outfit, outfitIndex = 2 },
                new RewardDefinition { type = RewardType.ItemLevels, target = "bunker:0", levels = 3 },
            }, source);
            Assert.AreEqual(10, _wallet.Coins);
            Assert.IsTrue(_wallet.OwnsOutfit(2));
            Assert.AreEqual(3, _sink.ItemLevels["bunker:0"]);
        }

        [Test]
        public void OutfitTrial_ExpiresAndNeverBecomesOwned()
        {
            _granter.Grant(new[] { new RewardDefinition { type = RewardType.OutfitTrial, outfitIndex = 5, durationSeconds = 1800 } },
                RewardSource.RewardedAd);
            Assert.AreEqual(5, _effects.ActiveOutfitTrial());
            Assert.IsFalse(_wallet.OwnsOutfit(5));

            TemporaryEffectState expired = null;
            _effects.Expired += e => expired = e;
            _clock.NowSeconds += 1801;
            _effects.Prune();
            Assert.AreEqual(-1, _effects.ActiveOutfitTrial());
            Assert.AreEqual(EffectKind.OutfitTrial, expired.kind);
            Assert.IsFalse(_wallet.OwnsOutfit(5));
        }

        [Test]
        public void ItemTrial_GivesBonusLevelsOnlyWhileActive()
        {
            _granter.Grant(new[] { new RewardDefinition { type = RewardType.ItemTrial, target = "bunker:4", levels = 10, durationSeconds = 900 } },
                RewardSource.RewardedAd);
            Assert.AreEqual(10, _effects.BonusLevels(EffectKind.ItemTrial, "bunker:4"));
            CollectionAssert.IsEmpty(_sink.ItemLevels, "nível real não muda");
            _clock.NowSeconds += 901;
            Assert.AreEqual(0, _effects.BonusLevels(EffectKind.ItemTrial, "bunker:4"));
        }

        [Test]
        public void SameBooster_StacksDuration()
        {
            _granter.Grant(new[] { RewardDefinition.Booster(BoosterType.TapX4, 180) }, RewardSource.RewardedAd);
            _granter.Grant(new[] { RewardDefinition.Booster(BoosterType.TapX4, 180) }, RewardSource.Ticket);
            Assert.AreEqual(360, _effects.Remaining(EffectKind.Booster, "TapX4"), 0.001);
        }

        [Test]
        public void PkRetries_StackAndAreConsumedOneByOne()
        {
            var retry = new RewardDefinition { type = RewardType.PkRetry, durationSeconds = 120 };
            _granter.Grant(new[] { retry }, RewardSource.RewardedAd);
            _granter.Grant(new[] { retry }, RewardSource.Ticket);
            Assert.IsTrue(_effects.TryConsumePkRetry());
            Assert.IsTrue(_effects.TryConsumePkRetry());
            Assert.IsFalse(_effects.TryConsumePkRetry());
        }

        [Test]
        public void InvalidNumbers_AreRejectedBeforeAnythingIsApplied()
        {
            var batch = new[] { RewardDefinition.Tickets(5), RewardDefinition.Coins(double.NaN) };
            Assert.Throws<RewardPolicyException>(() => _granter.Grant(batch, RewardSource.Purchase));
            Assert.AreEqual(0, _wallet.Tickets);
        }

        [Test]
        public void TemporaryWithoutDuration_IsRejected() =>
            Assert.Throws<RewardPolicyException>(() =>
                _granter.Grant(new[] { RewardDefinition.Booster(BoosterType.TapX4, 0) }, RewardSource.RewardedAd));

        [Test]
        public void ConfigValidator_RejectsAdThatWouldGivePermanentReward()
        {
            var ads = new AdPlacementsFile
            {
                placements = { new AdPlacementConfig { id = "rv_coins", reward = RewardDefinition.Coins(100) } },
            };
            Assert.Throws<RewardPolicyException>(() => MonetizationConfigValidator.Validate(ads, new IapCatalogFile()));
        }
    }

    public sealed class RewardedOfferServiceTests
    {
        private FakeClock _clock;
        private StubAds _ads;
        private PlayerWallet _wallet;
        private TemporaryEffects _effects;
        private RewardedOfferService _offers;

        [SetUp]
        public void SetUp()
        {
            _clock = new FakeClock();
            _ads = new StubAds();
            _wallet = new PlayerWallet(new WalletState());
            _effects = new TemporaryEffects(_wallet.Snapshot.temporaryEffects, _clock);
            var placements = new List<AdPlacementConfig>
            {
                new AdPlacementConfig { id = "rv_tap", ticketSkippable = true,
                    reward = RewardDefinition.Booster(BoosterType.TapX4, 180) },
                new AdPlacementConfig { id = "rv_income", cooldownSeconds = 300,
                    reward = RewardDefinition.Booster(BoosterType.IncomeX10, 60) },
                new AdPlacementConfig { id = "rv_outfit", ticketSkippable = true, requiresChoice = true,
                    reward = new RewardDefinition { type = RewardType.OutfitTrial, durationSeconds = 1800 } },
            };
            _offers = new RewardedOfferService(placements, _ads, _wallet,
                new RewardGranter(_wallet, _effects, new StubSink()), _clock, new NullAnalytics());
        }

        [Test]
        public void Constructor_RejectsPlacementWithPermanentReward()
        {
            var bad = new List<AdPlacementConfig> { new AdPlacementConfig { id = "bad", reward = RewardDefinition.Coins(1) } };
            Assert.Throws<RewardPolicyException>(() => new RewardedOfferService(bad, _ads, _wallet,
                new RewardGranter(_wallet, _effects, new StubSink()), _clock, new NullAnalytics()));
        }

        [Test]
        public void Claim_GrantsTemporaryBooster_WhenAdCompletes()
        {
            OfferOutcome outcome = default;
            _offers.Claim("rv_tap", o => outcome = o);
            Assert.AreEqual(OfferOutcome.GrantedByAd, outcome);
            Assert.IsTrue(_effects.IsBoosterActive(BoosterType.TapX4));
            _clock.NowSeconds += 181;
            Assert.IsFalse(_effects.IsBoosterActive(BoosterType.TapX4));
        }

        [Test]
        public void Claim_SpendsTicketInsteadOfAd_WhenPlayerHasTickets()
        {
            _wallet.AddTickets(2);
            OfferOutcome outcome = default;
            _offers.Claim("rv_tap", o => outcome = o);
            Assert.AreEqual(OfferOutcome.GrantedByTicket, outcome);
            Assert.AreEqual(1, _wallet.Tickets);
            Assert.AreEqual(0, _ads.RewardedShown);
        }

        [Test]
        public void Claim_GrantsNothing_WhenAdSkipped()
        {
            _ads.Result = AdShowResult.Skipped;
            OfferOutcome outcome = default;
            _offers.Claim("rv_tap", o => outcome = o);
            Assert.AreEqual(OfferOutcome.AdNotCompleted, outcome);
            Assert.IsFalse(_effects.IsBoosterActive(BoosterType.TapX4));
        }

        [Test]
        public void Claim_RespectsPersistedCooldown()
        {
            _offers.Claim("rv_income", _ => { });
            OfferOutcome second = default;
            _offers.Claim("rv_income", o => second = o);
            Assert.AreEqual(OfferOutcome.OnCooldown, second);
            Assert.IsTrue(_wallet.TryGetLastClaim("rv_income", out _));
            _clock.NowSeconds += 301;
            _offers.Claim("rv_income", o => second = o);
            Assert.AreEqual(OfferOutcome.GrantedByAd, second);
        }

        [Test]
        public void OutfitTrial_RequiresChoice_AndDoesNotSpendTicketWithoutIt()
        {
            _wallet.AddTickets(1);
            OfferOutcome outcome = default;
            _offers.Claim("rv_outfit", o => outcome = o);
            Assert.AreEqual(OfferOutcome.MissingChoice, outcome);
            Assert.AreEqual(1, _wallet.Tickets);

            _offers.Claim("rv_outfit", o => outcome = o, outfitIndex: 7);
            Assert.AreEqual(OfferOutcome.GrantedByTicket, outcome);
            Assert.AreEqual(7, _effects.ActiveOutfitTrial());
            Assert.IsFalse(_wallet.OwnsOutfit(7));
        }

        [Test]
        public void Claim_IgnoresDoubleTapWhileAdIsShowing()
        {
            _ads.Deferred = true;
            OfferOutcome second = default;
            _offers.Claim("rv_tap", _ => { });
            _offers.Claim("rv_tap", o => second = o);
            Assert.AreEqual(OfferOutcome.AlreadyInProgress, second);
            Assert.AreEqual(1, _ads.RewardedShown);
            _ads.Pending(AdShowResult.Completed);
            Assert.IsTrue(_effects.IsBoosterActive(BoosterType.TapX4));
        }

        [Test]
        public void Claim_ReportsAdNotReady()
        {
            _ads.IsRewardedReady = false;
            OfferOutcome outcome = default;
            _offers.Claim("rv_tap", o => outcome = o);
            Assert.AreEqual(OfferOutcome.AdNotReady, outcome);
        }
    }

    public sealed class PurchaseRewardHandlerTests
    {
        private StubIap _iap;
        private StubSink _sink;
        private PlayerWallet _wallet;
        private PurchaseRewardHandler _handler;
        private bool _saveOk;

        [SetUp]
        public void SetUp()
        {
            _saveOk = true;
            _iap = new StubIap();
            _sink = new StubSink { IncomePerMinute = 10 };
            _wallet = new PlayerWallet(new WalletState());
            var effects = new TemporaryEffects(_wallet.Snapshot.temporaryEffects, new FakeClock());
            var catalog = new List<IapProductConfig>
            {
                new IapProductConfig { productId = "zs_tickets_15", type = ProductKind.Consumable,
                    rewards = { RewardDefinition.Tickets(15) } },
                new IapProductConfig { productId = "zs_coins_small", type = ProductKind.Consumable,
                    rewards = { new RewardDefinition { type = RewardType.Coins, coinsScaledToIncome = true, minutes = 120 } } },
                new IapProductConfig { productId = "zs_income_x2", type = ProductKind.NonConsumable,
                    rewards = { new RewardDefinition { type = RewardType.PermanentIncomeMultiplier, multiplier = 2 } } },
                new IapProductConfig { productId = "zs_golden_pack", type = ProductKind.Consumable, oncePerAccount = true,
                    rewards = { RewardDefinition.Tickets(30), new RewardDefinition { type = RewardType.Outfit, outfitIndex = 9 } } },
                new IapProductConfig { productId = "zs_outfit_unlock", type = ProductKind.Consumable, requiresChoice = true,
                    rewards = { new RewardDefinition { type = RewardType.Outfit } } },
            };
            _handler = new PurchaseRewardHandler(catalog, _iap, _wallet, new RewardGranter(_wallet, effects, _sink),
                new NullAnalytics(), () => _saveOk);
        }

        [Test]
        public void Purchase_DeliversRewardsAndConfirms()
        {
            _handler.Buy("zs_tickets_15");
            Assert.AreEqual(15, _wallet.Tickets);
            CollectionAssert.Contains(_iap.Confirmed, "tx-zs_tickets_15");
        }

        [Test]
        public void Purchase_IsIdempotentPerTransaction()
        {
            _iap.Deliver("zs_tickets_15", "same-tx");
            _iap.Deliver("zs_tickets_15", "same-tx");
            Assert.AreEqual(15, _wallet.Tickets);
            Assert.AreEqual(2, _iap.Confirmed.Count, "confirma de novo para a loja parar de reenviar");
        }

        [Test]
        public void Purchase_ScalesCoinsWithIncome()
        {
            _handler.Buy("zs_coins_small");
            Assert.AreEqual(1200, _wallet.Coins);
        }

        [Test]
        public void PermanentIncome_CannotBeBoughtTwice()
        {
            _handler.Buy("zs_income_x2");
            Assert.AreEqual(2, _wallet.PermanentIncomeMultiplier);
            Assert.IsFalse(_handler.CanBuy("zs_income_x2"));
        }

        [Test]
        public void BuyingTriedOutfit_UsesChoiceAndMakesItOwned()
        {
            _handler.Buy("zs_outfit_unlock", outfitIndex: 7);
            Assert.IsTrue(_wallet.OwnsOutfit(7));
            CollectionAssert.IsEmpty(_wallet.Snapshot.pendingPurchaseChoices);
        }

        [Test]
        public void ChoiceProduct_WithoutChoice_DoesNotOpenStore()
        {
            _handler.Buy("zs_outfit_unlock");
            CollectionAssert.IsEmpty(_iap.Started);
        }

        [Test]
        public void ChoiceSurvivesCrashBetweenStoreAndDelivery()
        {
            _iap.AutoComplete = false;
            _handler.Buy("zs_outfit_unlock", outfitIndex: 4);
            Assert.AreEqual(1, _wallet.Snapshot.pendingPurchaseChoices.Count, "salvo antes de abrir a loja");
            _iap.Deliver("zs_outfit_unlock", "late-tx"); // loja reenvia no próximo startup
            Assert.IsTrue(_wallet.OwnsOutfit(4));
        }

        [Test]
        public void SaveFailure_LeavesPurchasePending()
        {
            _saveOk = false;
            _handler.Buy("zs_tickets_15");
            CollectionAssert.IsEmpty(_iap.Confirmed);
        }

        [Test]
        public void GrantFailure_DoesNotMarkNorConfirm()
        {
            _sink.IncomePerMinute = double.NaN;
            _iap.Deliver("zs_coins_small", "tx-nan");
            CollectionAssert.IsEmpty(_iap.Confirmed);
            Assert.IsFalse(_wallet.IsTransactionProcessed("tx-nan"));
        }

        [Test]
        public void Redelivery_OfOncePerAccountPack_DoesNotRegrantTickets()
        {
            _iap.Deliver("zs_golden_pack", "order-1");
            Assert.AreEqual(30, _wallet.Tickets);
            _iap.Deliver("zs_golden_pack", "order-1-restore"); // reinstalação com save em nuvem restaurado
            Assert.AreEqual(30, _wallet.Tickets);
            Assert.IsTrue(_wallet.OwnsOutfit(9));
        }

        [Test]
        public void Redelivery_AfterFailedSave_SavesBeforeConfirming()
        {
            _saveOk = false;
            _iap.Deliver("zs_tickets_15", "tx-1");
            _iap.Deliver("zs_tickets_15", "tx-1"); // loja reenvia na mesma sessão; save ainda falhando
            CollectionAssert.IsEmpty(_iap.Confirmed);
            _saveOk = true;
            _iap.Deliver("zs_tickets_15", "tx-1");
            CollectionAssert.Contains(_iap.Confirmed, "tx-1");
            Assert.AreEqual(15, _wallet.Tickets);
        }

        [Test]
        public void CancelledPurchase_DiscardsSavedChoice()
        {
            _iap.AutoComplete = false;
            _handler.Buy("zs_outfit_unlock", outfitIndex: 3);
            _iap.Fail("zs_outfit_unlock");
            CollectionAssert.IsEmpty(_wallet.Snapshot.pendingPurchaseChoices);
        }

        [Test]
        public void UnknownProduct_IsNotConfirmed()
        {
            _iap.Deliver("produto_removido", "tx-x");
            CollectionAssert.DoesNotContain(_iap.Confirmed, "tx-x");
        }
    }
}
