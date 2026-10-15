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
        public readonly List<string> Boosters = new List<string>();
        public void ActivateBooster(BoosterType booster, int durationSeconds) => Boosters.Add($"{booster}:{durationSeconds}");
        public void GrantFreeUpgrade(string target, int levels) { }
        public void GrantPkRetry() { }
        public int NextLockedOutfitIndex() => 3;
    }

    internal sealed class StubAds : IAdService
    {
        public bool IsRewardedReady { get; set; } = true;
        public bool IsInterstitialReady => true;
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
        public void ShowInterstitial(string placementId, Action<AdShowResult> onFinished) => onFinished(AdShowResult.Completed);
        public void SetBannerVisible(bool visible) { }
    }

    internal sealed class StubIap : IIapService
    {
        public readonly List<string> Confirmed = new List<string>();
        public event Action<PurchaseReceipt> PurchaseSucceeded;
        public event Action<string, PurchaseFailure> PurchaseFailed;
        public void Initialize(IEnumerable<IapProductConfig> products, Action onReady) => onReady?.Invoke();
        public string GetLocalizedPrice(string productId) => null;
        public void Purchase(string productId) => PurchaseSucceeded?.Invoke(new PurchaseReceipt(productId, "tx-" + productId));
        public void Deliver(string productId, string tx) => PurchaseSucceeded?.Invoke(new PurchaseReceipt(productId, tx));
        public void Fail(string productId) => PurchaseFailed?.Invoke(productId, PurchaseFailure.Cancelled);
        public void RestorePurchases() { }
        public void ConfirmDelivered(PurchaseReceipt receipt) => Confirmed.Add(receipt.TransactionId);
    }

    public sealed class RewardedOfferServiceTests
    {
        private FakeClock _clock;
        private StubAds _ads;
        private StubSink _sink;
        private PlayerWallet _wallet;
        private RewardedOfferService _offers;

        [SetUp]
        public void SetUp()
        {
            _clock = new FakeClock();
            _ads = new StubAds();
            _sink = new StubSink();
            _wallet = new PlayerWallet(new WalletState());
            var placements = new List<AdPlacementConfig>
            {
                new AdPlacementConfig { id = "rv_tap", kind = AdKind.Rewarded, ticketSkippable = true,
                    reward = RewardDefinition.Booster(BoosterType.TapX4, 180) },
                new AdPlacementConfig { id = "rv_gift", kind = AdKind.Rewarded, cooldownSeconds = 600,
                    reward = RewardDefinition.Coins(50) },
                new AdPlacementConfig { id = "rv_x2", kind = AdKind.Rewarded,
                    reward = new RewardDefinition { type = RewardType.ClaimMultiplier, multiplier = 2 } },
                new AdPlacementConfig { id = "rv_afk", kind = AdKind.Rewarded, ticketSkippable = true,
                    reward = new RewardDefinition { type = RewardType.AfkMultiplier, multipliers = new double[] { 2, 3, 5 } } },
            };
            var pacer = new InterstitialPacer(new InterstitialPacingConfig(), _clock, 0);
            _offers = new RewardedOfferService(placements, _ads, _wallet,
                new RewardGranter(_wallet, _sink), pacer, _clock, new NullAnalytics());
        }

        [Test]
        public void Claim_GrantsBooster_WhenAdCompletes()
        {
            OfferOutcome outcome = default;
            _offers.Claim("rv_tap", o => outcome = o);
            Assert.AreEqual(OfferOutcome.GrantedByAd, outcome);
            CollectionAssert.AreEqual(new[] { "TapX4:180" }, _sink.Boosters);
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
            _offers.Claim("rv_gift", o => outcome = o);
            Assert.AreEqual(OfferOutcome.AdNotCompleted, outcome);
            Assert.AreEqual(0, _wallet.Coins);
        }

        [Test]
        public void Claim_RespectsCooldown()
        {
            _offers.Claim("rv_gift", _ => { });
            OfferOutcome second = default;
            _offers.Claim("rv_gift", o => second = o);
            Assert.AreEqual(OfferOutcome.OnCooldown, second);
            _clock.NowSeconds += 601;
            _offers.Claim("rv_gift", o => second = o);
            Assert.AreEqual(OfferOutcome.GrantedByAd, second);
            Assert.AreEqual(100, _wallet.Coins);
        }

        [Test]
        public void Claim_MultiplierAddsOnlyTheExtraPart()
        {
            _offers.Claim("rv_x2", _ => { }, baseAmount: 500);
            Assert.AreEqual(500, _wallet.Coins);
        }

        [Test]
        public void Claim_AfkWithoutDrawnMultiplier_DoesNotSpendTicket()
        {
            _wallet.AddTickets(1);
            OfferOutcome outcome = default;
            _offers.Claim("rv_afk", o => outcome = o, baseAmount: 100);
            Assert.AreEqual(OfferOutcome.MissingMultiplier, outcome);
            Assert.AreEqual(1, _wallet.Tickets);
        }

        [Test]
        public void Claim_AfkWithDrawnMultiplier_AddsExtra()
        {
            var afk = new RewardDefinition { type = RewardType.AfkMultiplier }.WithMultiplier(3);
            _offers.Claim("rv_afk", _ => { }, afk, baseAmount: 100);
            Assert.AreEqual(200, _wallet.Coins);
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
            Assert.AreEqual(1, _sink.Boosters.Count);
        }

        [Test]
        public void Cooldown_IsStoredInWalletState()
        {
            _offers.Claim("rv_gift", _ => { });
            Assert.IsTrue(_wallet.TryGetLastClaim("rv_gift", out _));
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
            var catalog = new List<IapProductConfig>
            {
                new IapProductConfig { productId = "zs_noads", type = ProductKind.NonConsumable,
                    rewards = { RewardDefinition.NoAds() } },
                new IapProductConfig { productId = "zs_tickets_15", type = ProductKind.Consumable,
                    rewards = { RewardDefinition.Tickets(15) } },
                new IapProductConfig { productId = "zs_noads_tickets", type = ProductKind.NonConsumable,
                    rewards = { RewardDefinition.NoAds(), RewardDefinition.Tickets(50) } },
                new IapProductConfig { productId = "zs_coins_small", type = ProductKind.Consumable,
                    rewards = { new RewardDefinition { type = RewardType.Coins, coinsScaledToIncome = true, minutes = 120 } } },
            };
            _handler = new PurchaseRewardHandler(catalog, _iap, _wallet, new RewardGranter(_wallet, _sink), new NullAnalytics(),
                () => _saveOk);
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
        public void NoAds_CannotBeBoughtTwice()
        {
            _handler.Buy("zs_noads");
            Assert.IsTrue(_wallet.HasNoAds);
            Assert.IsFalse(_handler.CanBuy("zs_noads"));
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
            CollectionAssert.DoesNotContain(_wallet.Snapshot.processedTransactions, "tx-nan");
        }

        [Test]
        public void Restore_OfNonConsumableBundle_DoesNotRegrantTickets()
        {
            _iap.Deliver("zs_noads_tickets", "order-1");
            Assert.AreEqual(50, _wallet.Tickets);
            _wallet.Snapshot.processedTransactions.Clear(); // reinstalação com save em nuvem restaurado: a loja reenvia com outro id
            _iap.Deliver("zs_noads_tickets", "order-1-restore");
            Assert.AreEqual(50, _wallet.Tickets);
            Assert.IsTrue(_wallet.HasNoAds);
        }

        [Test]
        public void UnknownProduct_IsNotConfirmed()
        {
            _iap.Deliver("produto_removido", "tx-x");
            CollectionAssert.DoesNotContain(_iap.Confirmed, "tx-x");
        }
    }

    public sealed class InterstitialPacerTests
    {
        private FakeClock _clock;
        private InterstitialPacer _pacer;

        [SetUp]
        public void SetUp()
        {
            _clock = new FakeClock { NowSeconds = 1000 };
            _pacer = new InterstitialPacer(new InterstitialPacingConfig
            {
                minSecondsBetween = 90, minSecondsAfterRewarded = 60, graceSecondsAfterInstall = 300, maxPerSession = 2,
            }, _clock, installTimeSeconds: 0);
        }

        [Test]
        public void BlockedForNoAdsBuyers() => Assert.IsFalse(_pacer.CanShow(hasNoAds: true, isNaturalBreak: true));

        [Test]
        public void BlockedOutsideNaturalBreaks() => Assert.IsFalse(_pacer.CanShow(false, isNaturalBreak: false));

        [Test]
        public void BlockedDuringInstallGrace()
        {
            var fresh = new InterstitialPacer(new InterstitialPacingConfig(), _clock, installTimeSeconds: 900);
            Assert.IsFalse(fresh.CanShow(false, true));
        }

        [Test]
        public void EnforcesSpacingAndSessionCap()
        {
            Assert.IsTrue(_pacer.CanShow(false, true));
            _pacer.NotifyInterstitialShown();
            Assert.IsFalse(_pacer.CanShow(false, true));
            _clock.NowSeconds += 91;
            Assert.IsTrue(_pacer.CanShow(false, true));
            _pacer.NotifyInterstitialShown();
            _clock.NowSeconds += 91;
            Assert.IsFalse(_pacer.CanShow(false, true), "limite de 2 por sessão");
        }

        [Test]
        public void WaitsAfterRewarded()
        {
            _pacer.NotifyRewardedShown();
            Assert.IsFalse(_pacer.CanShow(false, true));
            _clock.NowSeconds += 61;
            Assert.IsTrue(_pacer.CanShow(false, true));
        }
    }
}
