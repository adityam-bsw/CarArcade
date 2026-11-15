using System;
using NUnit.Framework;
using UnityEngine;

namespace RoadBuilderGame.Tests
{
    /// <summary>Blueprint section D: upgrades, gates, mastery, daily seed, save migration, ad limits.</summary>
    public class ProgressionTests
    {
        private static UpgradeTreeSO Tree() => ScriptableObject.CreateInstance<UpgradeTreeSO>();
        private static ProgressionTuningSO Tun() => ScriptableObject.CreateInstance<ProgressionTuningSO>();

        [Test] public void Upgrade_Costs_AndMax()
        {
            var t = Tree();
            Assert.AreEqual(new[] { 500, 900, 1500, 2500, 4000 }, new[] { t.Cost(0), t.Cost(1), t.Cost(2), t.Cost(3), t.Cost(4) });
            Assert.AreEqual(-1, t.Cost(5));
        }

        [Test] public void Upgrade_Effect_IsSixPercentPerLevel_AndOverridable()
        {
            var t = Tree();
            Assert.AreEqual(0f, t.Bonus(UpgradeBranch.Engine, "x", 0), 1e-5f);
            Assert.AreEqual(11.5f * 0.06f, t.Bonus(UpgradeBranch.Engine, "x", 1), 1e-4f);
            Assert.AreEqual(6f * 5 / 5 * 1f, t.Bonus(UpgradeBranch.InkTank, "x", 1), 1e-4f);   // 100 x 6 percent
            Assert.AreEqual(30f, t.Bonus(UpgradeBranch.InkTank, "x", 5), 1e-3f);
            t.overrides = new[] { new BranchBaseOverride { vehicleId = "tank", branch = UpgradeBranch.InkTank, baseValue = 50f } };
            Assert.AreEqual(3f, t.Bonus(UpgradeBranch.InkTank, "tank", 1), 1e-4f);
            Assert.AreEqual(6f, t.Bonus(UpgradeBranch.InkTank, "car", 1), 1e-4f);
        }

        [Test] public void Upgrade_Buy_DeductsAndStopsAtMax()
        {
            var p = new PlayerProfile { coins = 100000 }; var t = Tree();
            for (int i = 0; i < 5; i++) Assert.IsTrue(UpgradeService.TryBuy(p, "v", UpgradeBranch.Boost, t));
            Assert.IsFalse(UpgradeService.TryBuy(p, "v", UpgradeBranch.Boost, t));
            Assert.AreEqual(100000 - 9400, p.coins);
            var poor = new PlayerProfile { coins = 499 };
            Assert.IsFalse(UpgradeService.TryBuy(poor, "v", UpgradeBranch.Engine, t));
        }

        [Test] public void Save_Migration_V1_KeepsUpgradesWorldsAndDistance()
        {
            var json = "{\"version\":1,\"coins\":10,\"bestDistance\":1234.5,\"engineLevel\":3,\"suspensionLevel\":1,\"inkTankLevel\":5,\"boostLevel\":2,\"selectedWorldId\":\"world-farm\"}";
            var p = JsonUtility.FromJson<PlayerProfile>(json);
            p.Sanitize();
            Assert.AreEqual(2, p.version);
            Assert.AreEqual(1234.5f, p.lifetimeDistance, 1e-3f);
            Assert.IsTrue(p.unlockedWorldIds.Contains("world-desert") && p.unlockedWorldIds.Contains("world-night-city") && p.unlockedWorldIds.Contains("world-farm"));
            var e = p.UpgradesFor("vehicle-car");
            Assert.AreEqual(new[] { 2, 0, 4, 1, 0 }, e.levels);
            Assert.IsTrue(GateService.World(p, ScriptableObject.CreateInstance<WorldGateSO>(), "world-desert").AlreadyUnlocked);
            // round trip
            var q = JsonUtility.FromJson<PlayerProfile>(JsonUtility.ToJson(p)); q.Sanitize();
            Assert.AreEqual(new[] { 2, 0, 4, 1, 0 }, q.UpgradesFor("vehicle-car").levels);
        }

        [Test] public void Gates_NewContentOnly()
        {
            var w = ScriptableObject.CreateInstance<WorldGateSO>(); var v = ScriptableObject.CreateInstance<VehicleGateSO>();
            var p = new PlayerProfile { coins = 100000, lifetimeDistance = 999f };
            Assert.IsFalse(GateService.World(p, w, "world-desert").CanBuy);
            p.lifetimeDistance = 1000f;
            Assert.IsFalse(GateService.World(p, w, "world-desert").CanBuy, "extra gate still open");
            p.AddCounter(GateService.ExtraCounterId(ExtraGate.FirstMonster), 1);
            Assert.IsTrue(GateService.TryUnlockWorld(p, w, "world-desert"));
            Assert.AreEqual(98000, p.coins);
            Assert.IsTrue(GateService.TryUnlockWorld(p, w, "world-desert"), "already owned is free");
            Assert.AreEqual(98000, p.coins);
            Assert.IsFalse(GateService.TryUnlockVehicle(p, v, "vehicle-tank"));
            p.lifetimeDistance = 8000f;
            Assert.IsTrue(GateService.TryUnlockVehicle(p, v, "vehicle-tank"));
            var owned = new PlayerProfile(); owned.unlockedVehicleIds.Add("vehicle-moon-rover");
            Assert.IsTrue(GateService.Vehicle(owned, v, "vehicle-moon-rover").CanBuy, "owned stays unlocked at 0 m");
        }

        [Test] public void Mastery_Levels_PayOnce()
        {
            var t = Tun(); var p = new PlayerProfile { coins = 0 };
            Assert.AreEqual(0, MasteryService.LevelFor(t, 499f));
            Assert.AreEqual(1, MasteryService.RecordDistance(p, t, "car", 600f));
            Assert.AreEqual(1, MasteryService.Level(p, t, "car"));
            Assert.AreEqual(1, MasteryService.RecordDistance(p, t, "car", 1500f));   // 2100 m: level 2, +500 coins
            Assert.AreEqual(500, p.coins);
            Assert.AreEqual(0, MasteryService.RecordDistance(p, t, "car", 10f));
            MasteryService.RecordDistance(p, t, "car", 8000f);                         // 10110: levels 3 and 4
            Assert.AreEqual(1, p.gems); Assert.AreEqual(4, MasteryService.Level(p, t, "car"));
            Assert.AreEqual(10110f, p.lifetimeDistance, 1e-2f);
            MasteryService.AddGoal(p, "vehicle-bicycle", "clean-landings", 10);
            Assert.IsTrue(MasteryService.GoalDone(p, t, "vehicle-bicycle", "clean-landings"));
        }

        [Test] public void Daily_Seed_IsDeterministicByUtcDate()
        {
            var t = Tun(); var d = new DateTime(2026, 11, 16, 3, 0, 0, DateTimeKind.Utc);
            Assert.AreEqual(DailyChallenge.SeedFor(d), DailyChallenge.SeedFor(d.AddHours(15)));
            Assert.AreNotEqual(DailyChallenge.SeedFor(d), DailyChallenge.SeedFor(d.AddDays(1)));
            var a = DailyChallenge.Build(d, t); var b = DailyChallenge.Build(d.AddHours(5), t);
            Assert.AreEqual(a.vehicleId, b.vehicleId); Assert.AreEqual(a.worldId, b.worldId);
            Assert.AreEqual(a.eventIds, b.eventIds); Assert.AreEqual(3, a.eventIds.Length);
            Assert.AreEqual(3, new System.Collections.Generic.HashSet<string>(a.eventIds).Count);
            Assert.That(a.rewardCoins, Is.InRange(100, 250));
        }

        [Test] public void Daily_OncePerDay_LocalBest()
        {
            var t = Tun(); var p = new PlayerProfile(); var d = new DateTime(2026, 11, 16, 8, 0, 0, DateTimeKind.Utc);
            Assert.IsFalse(DailyChallenge.CanClaim(p, d));
            Assert.IsTrue(DailyChallenge.SubmitScore(p, d, 500)); Assert.IsFalse(DailyChallenge.SubmitScore(p, d, 300));
            Assert.IsTrue(DailyChallenge.Claim(p, d, t, out int c)); Assert.Greater(c, 99);
            Assert.IsFalse(DailyChallenge.Claim(p, d, t, out _));
            Assert.AreEqual(1, p.masteryPoints); Assert.AreEqual(1, p.weeklyTokens);
            var next = d.AddDays(1); DailyChallenge.SubmitScore(p, next, 10);
            Assert.IsTrue(DailyChallenge.Claim(p, next, t, out _));
            Assert.AreEqual("2026-11-16", DailyChallenge.WeekKey(d));   // a Monday
        }

        [Test] public void Ads_Limits_AndNeverMidRun()
        {
            var t = Tun(); var p = new PlayerProfile(); var now = new DateTime(2026, 11, 16, 12, 0, 0, DateTimeKind.Utc);
            var o = new AdOffers(p, t, new EditorFakeAdService(), () => now);
            o.BeginRun();
            Assert.IsFalse(o.TryOffer(AdPlacement.DoubleCoins, null), "never mid-run");
            o.EndRun();
            Assert.IsTrue(o.TryOffer(AdPlacement.DoubleCoins, null)); Assert.IsTrue(o.TryOffer(AdPlacement.DoubleCoins, null));
            Assert.IsFalse(o.TryOffer(AdPlacement.DoubleCoins, null), "max 2 per day");
            Assert.IsTrue(o.TryOffer(AdPlacement.ExtraDailyReward, null)); Assert.IsFalse(o.TryOffer(AdPlacement.ExtraDailyReward, null));
            Assert.IsTrue(o.TryOffer(AdPlacement.Revive, null)); Assert.IsFalse(o.TryOffer(AdPlacement.Revive, null), "one revive per run");
            o.BeginRun(); o.EndRun(); Assert.IsTrue(o.CanOffer(AdPlacement.Revive));
            now = now.AddDays(1); Assert.IsTrue(o.CanOffer(AdPlacement.DoubleCoins), "counters reset next UTC day");
            Assert.IsFalse(new AdOffers(p, t, new NoOpAdService(), () => now).CanOffer(AdPlacement.DoubleCoins), "no SDK means no offers");
        }

        [Test] public void Ads_NoRewardNoCount()
        {
            var t = Tun(); var p = new PlayerProfile(); var o = new AdOffers(p, t, new NoOpAdService(), () => DateTime.UtcNow);
            Assert.IsFalse(o.TryOffer(AdPlacement.DoubleCoins, null)); Assert.AreEqual(0, p.adDoubleUsed);
        }

        [Test] public void FirstHour_ClaimOnce()
        {
            var t = Tun(); var p = new PlayerProfile { coins = 0 };
            Assert.IsTrue(FirstHour.Claim(p, t, "first-500m")); Assert.IsFalse(FirstHour.Claim(p, t, "first-500m"));
            Assert.AreEqual(500, p.coins); Assert.IsFalse(FirstHour.Claim(p, t, "nope"));
        }
    }
}
