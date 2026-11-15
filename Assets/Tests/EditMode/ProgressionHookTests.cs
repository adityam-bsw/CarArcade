using System;
using NUnit.Framework;
using UnityEngine;

namespace RoadBuilderGame.Tests
{
    /// <summary>Section D part 2: gate counter hooks, the Shield upgrade, per-vehicle costs, gate text, daily matrix filter, revive limit.</summary>
    public class ProgressionHookTests
    {
        private static UpgradeTreeSO Tree() => ScriptableObject.CreateInstance<UpgradeTreeSO>();
        private static ProgressionTuningSO Tun() => ScriptableObject.CreateInstance<ProgressionTuningSO>();

        [Test] public void Hooks_IncrementTheGateCounters_AndOpenTheGate()
        {
            var p = new PlayerProfile { lifetimeDistance = 3000f }; var g = ScriptableObject.CreateInstance<WorldGateSO>();
            Assert.IsFalse(GateService.World(p, g, "world-desert").ExtraMet);
            ProgressionEvents.Record(p, ProgressionEvent.MonsterReached);
            Assert.IsTrue(GateService.World(p, g, "world-desert").ExtraMet);
            for (int i = 0; i < 3; i++) ProgressionEvents.Record(p, ProgressionEvent.MissionCompleted);
            ProgressionEvents.Record(p, ProgressionEvent.BossReached);
            ProgressionEvents.Record(p, ProgressionEvent.StageEventCompleted, 10);
            Assert.AreEqual(3, p.Counter("gate:Missions")); Assert.AreEqual(1, p.Counter("gate:BossEncounter")); Assert.AreEqual(10, p.Counter("gate:StageEvents"));
            ProgressionEvents.Record(p, ProgressionEvent.CleanJump, 0); ProgressionEvents.Record(null, ProgressionEvent.CleanJump);
            Assert.AreEqual(0, p.Counter("gate:CleanJumps"));
        }

        [Test] public void RunEnd_CountsCleanJumps_SurvivalRun_AndGoals()
        {
            var p = new PlayerProfile(); var t = Tun();
            ProgressionEvents.RecordRunEnd(p, t, "vehicle-bicycle", 400f, false, 3, 2);
            Assert.AreEqual(3, p.Counter("gate:CleanJumps")); Assert.AreEqual(0, p.Counter("gate:SurvivalRuns"));
            Assert.AreEqual(3, p.Counter("vehicle-bicycle:clean-landings")); Assert.AreEqual(2, p.Counter("vehicle-bicycle:near-misses"));
            ProgressionEvents.RecordRunEnd(p, t, "vehicle-bicycle", t.survivalRunMetres, false, 0, 0);
            ProgressionEvents.RecordRunEnd(p, t, "vehicle-bicycle", 10f, true, 0, 0);
            Assert.AreEqual(2, p.Counter("gate:SurvivalRuns"));
        }

        [Test] public void Shield_AbsorbsOne_ThenRechargesFasterPerLevel()
        {
            var s = new UpgradeShield(); var t = Tree();
            s.Begin(0, t.ShieldRecharge(0)); Assert.IsFalse(s.Ready); Assert.IsFalse(s.TryAbsorb());
            s.Begin(1, t.ShieldRecharge(1)); Assert.IsTrue(s.TryAbsorb()); Assert.IsFalse(s.TryAbsorb());
            s.Tick(t.ShieldRecharge(1) - 1f); Assert.IsFalse(s.Ready);
            s.Tick(1.01f); Assert.IsTrue(s.Ready);
            Assert.Less(t.ShieldRecharge(5), t.ShieldRecharge(1));
        }

        [Test] public void Shield_InRunDirector_AbsorbsBeforeLifeLoss_PickupFirst()
        {
            var run = new RunDirector(ScriptableObject.CreateInstance<GameplayTuning>());
            run.Begin(1, 0f); run.UpShield.Begin(2, 30f);
            Assert.IsTrue(run.ShieldUp);
            run.Shield = true;
            Assert.IsTrue(run.AbsorbHit()); Assert.IsFalse(run.Shield); Assert.IsTrue(run.UpShield.Ready);   // the pickup goes first
            Assert.IsTrue(run.AbsorbHit()); Assert.IsFalse(run.UpShield.Ready);
            Assert.IsFalse(run.AbsorbHit());
        }

        [Test] public void UpgradeCost_PerVehicle_Scales_AndBuyUsesIt()
        {
            var t = Tree();
            Assert.AreEqual(500, t.Cost(0, "vehicle-car")); Assert.AreEqual(400, t.Cost(0, "vehicle-bicycle")); Assert.AreEqual(750, t.Cost(0, "vehicle-tank"));
            Assert.AreEqual(-1, t.Cost(5, "vehicle-tank"));
            var p = new PlayerProfile { coins = 749 };
            Assert.IsFalse(UpgradeService.TryBuy(p, "vehicle-tank", UpgradeBranch.Shield, t));
            p.coins = 750; Assert.IsTrue(UpgradeService.TryBuy(p, "vehicle-tank", UpgradeBranch.Shield, t));
            Assert.AreEqual(1, UpgradeService.Level(p, "vehicle-tank", UpgradeBranch.Shield)); Assert.AreEqual(0, p.coins);
        }

        [Test] public void GateText_ShowsRequirement_OwnedAndUngatedStayOpen()
        {
            var g = ScriptableObject.CreateInstance<WorldGateSO>(); var p = new PlayerProfile();
            Assert.AreEqual("REACH 1000 m", GateText.World(p, g, "world-desert"));
            p.lifetimeDistance = 1500f; Assert.AreEqual("MEET THE ROAD-EATER", GateText.World(p, g, "world-desert"));
            ProgressionEvents.Record(p, ProgressionEvent.MonsterReached); Assert.AreEqual("2000 COINS", GateText.World(p, g, "world-desert"));
            Assert.IsTrue(GateText.WorldLocked(p, g, "world-desert"));
            Assert.IsFalse(GateText.WorldLocked(p, g, "world-rolling-hills"));          // free and met
            Assert.IsFalse(GateText.WorldLocked(p, g, "world-not-in-table"));             // not in the table (every catalogue world is gated now)
            p.unlockedWorldIds.Add("world-moon"); Assert.AreEqual("", GateText.World(p, g, "world-moon"));   // owned stays unlocked
            Assert.IsFalse(GateText.WorldLocked(p, g, "world-moon"));
            var v = ScriptableObject.CreateInstance<VehicleGateSO>();
            Assert.AreEqual("REACH 8000 m", GateText.Vehicle(p, v, "vehicle-tank")); Assert.AreEqual("", GateText.Vehicle(p, v, "vehicle-car"));
        }

        [Test] public void Daily_MatrixFilter_SkipsForbiddenPairs_RewardUnchanged()
        {
            var t = Tun(); t.dailyEventIds = new[] { "A", "B", "C", "D" }; t.dailyEventCount = 3;
            var m = ScriptableObject.CreateInstance<EventCompatibilityMatrix>();
            m.pairs = new[] { new StageCompatPair { idA = "A", idB = "B", rule = StageCompatRule.Forbid }, new StageCompatPair { idA = "C", idB = "D", rule = StageCompatRule.Forbid } };
            for (int day = 1; day <= 20; day++)
            {
                var utc = new DateTime(2026, 3, day, 0, 0, 0, DateTimeKind.Utc);
                var d = DailyChallenge.Build(utc, t, m);
                Assert.AreEqual(DailyChallenge.Build(utc, t).rewardCoins, d.rewardCoins);
                Assert.LessOrEqual(d.eventIds.Length, 2);   // two forbidden pairs leave at most one of each
                for (int i = 0; i < d.eventIds.Length; i++) for (int j = i + 1; j < d.eventIds.Length; j++)
                    Assert.IsFalse(m.TryGetPair(d.eventIds[i], d.eventIds[j], out var pr) && pr.rule == StageCompatRule.Forbid);
                CollectionAssert.AreEqual(d.eventIds, DailyChallenge.Build(utc, t, m).eventIds);
            }
        }

        [Test] public void Revive_OncePerRun_Continuation_KeepsTheCounter()
        {
            var p = new PlayerProfile(); var t = Tun(); var offers = new AdOffers(p, t, new EditorFakeAdService(), () => new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            offers.BeginRun(); offers.EndRun();
            Assert.IsTrue(offers.TryOffer(AdPlacement.Revive, null));
            offers.BeginRun(true); offers.EndRun();                 // the revived run dies again
            Assert.IsFalse(offers.CanOffer(AdPlacement.Revive));
            offers.BeginRun(); offers.EndRun(); Assert.IsTrue(offers.CanOffer(AdPlacement.Revive));   // a fresh run resets it
            offers.BeginRun(); Assert.IsFalse(offers.CanOffer(AdPlacement.Revive));                    // never mid-run
        }
    }
}
