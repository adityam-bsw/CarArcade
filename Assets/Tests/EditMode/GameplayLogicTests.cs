using System;
using NUnit.Framework;
using UnityEngine;

namespace RoadBuilderGame.Tests
{
    /// <summary>The run rules (boost, combo, power-ups, milestones) and the meta rules (daily reward, missions, mastery).</summary>
    public class GameplayLogicTests
    {
        private static GameplayTuning Tuning() => ScriptableObject.CreateInstance<GameplayTuning>();

        [Test]
        public void Boost_CostsCoins_UntilTheFreeMeterIsFull()
        {
            var t = Tuning(); var run = new RunDirector(t); run.Begin(1, 100f);
            Assert.AreEqual(RunDirector.BoostResult.Refused, run.TryBoost(t.boostCoinCost - 1, out int paid));
            Assert.AreEqual(0, paid);
            Assert.AreEqual(RunDirector.BoostResult.Paid, run.TryBoost(t.boostCoinCost, out paid));
            Assert.AreEqual(t.boostCoinCost, paid);
            Assert.IsTrue(run.Boosting);
            run.Tick(10f, null, 0f);                       // boost and cooldown run out
            run.Boost01 = 1f;
            Assert.AreEqual(0, run.NextBoostCost);
            Assert.AreEqual(RunDirector.BoostResult.Free, run.TryBoost(0, out paid));
            Assert.AreEqual(0, paid);
            Assert.AreEqual(0f, run.Boost01);
        }

        [Test]
        public void Boost_HasACooldown_AndUpgradesLastLonger()
        {
            var t = Tuning(); var a = new RunDirector(t); a.Begin(1, 100f);
            a.TryBoost(100, out _);
            Assert.AreEqual(RunDirector.BoostResult.None, a.TryBoost(100, out _), "no second boost during the first");
            var b = new RunDirector(t); b.Begin(5, 100f);
            b.TryBoost(100, out _);
            Assert.Greater(b.BoostLeft, a.BoostLeft);
        }

        [Test]
        public void Coins_BuildACombo_ThatRaisesTheMultiplier_AndExpires()
        {
            var t = Tuning(); var run = new RunDirector(t); run.Begin(1, 100f);
            int first = run.CollectCoin();
            Assert.AreEqual(t.coinValue, first);
            for (int i = 0; i < t.comboPerStep * 2; i++) run.CollectCoin();
            Assert.Greater(run.Multiplier, 1);
            Assert.LessOrEqual(run.Multiplier, t.maxMultiplier);
            run.Tick(t.comboWindow + 0.1f, null, 0f);
            Assert.AreEqual(1, run.Multiplier);
            Assert.AreEqual(0, run.Combo);
        }

        [Test]
        public void DoubleCoins_DoublesTheValue()
        {
            var t = Tuning(); var run = new RunDirector(t); run.Begin(1, 100f);
            run.CollectPower(Collectible.Type.Double);
            Assert.AreEqual(t.coinValue * 2, run.CollectCoin());
        }

        [Test]
        public void Shield_AbsorbsOneHit()
        {
            var run = new RunDirector(Tuning()); run.Begin(1, 100f);
            Assert.IsFalse(run.AbsorbHit());
            run.CollectPower(Collectible.Type.Shield);
            Assert.IsTrue(run.AbsorbHit());
            Assert.IsFalse(run.AbsorbHit());
        }

        [Test]
        public void PowerUps_RunOut_AndSlowMoSlowsTime()
        {
            var t = Tuning(); var run = new RunDirector(t); run.Begin(1, 100f);
            run.CollectPower(Collectible.Type.SlowMo); run.CollectPower(Collectible.Type.Ghost); run.CollectPower(Collectible.Type.Magnet);
            Assert.Less(run.TimeScale, 1f);
            Assert.IsTrue(run.Ghost); Assert.IsTrue(run.MagnetOn);
            run.Tick(60f, null, 0f);
            Assert.AreEqual(1f, run.TimeScale);
            Assert.IsFalse(run.Ghost); Assert.IsFalse(run.MagnetOn);
            Assert.AreEqual("", run.PowerText());
        }

        [Test]
        public void Milestones_PayChests_AndTheRecordOnce()
        {
            var t = Tuning(); var run = new RunDirector(t); run.Begin(1, 300f);
            int paid = 0; run.Bonus += (label, coins) => paid += coins;
            run.Tick(0.01f, null, t.milestoneEvery + 1f);                   // first chest
            Assert.AreEqual(1, run.Stats.milestones);
            Assert.AreEqual(t.milestoneCoins, paid);
            run.Tick(0.01f, null, 301f);                                    // passes the old record of 300
            Assert.AreEqual(t.milestoneCoins + t.recordBonus, paid);
            run.Tick(0.01f, null, 302f);                                    // the record pays only once
            Assert.AreEqual(t.milestoneCoins + t.recordBonus, paid);
        }

        // ----------------------------------------------------------------------------------------- meta
        private static MetaProgress Meta(PlayerProfile p, DateTime day)
        {
            var m = new MetaProgress(p, Tuning());
            m.SetClock(() => day);
            return m;
        }

        [Test]
        public void Daily_CanBeClaimedOncePerDay_AndStreakContinuesOnlyDayToDay()
        {
            var p = new PlayerProfile { coins = 0 };
            var d1 = new DateTime(2026, 11, 10);
            Assert.IsTrue(Meta(p, d1).ClaimDaily(out int c1, out _));
            Assert.AreEqual(50, c1); Assert.AreEqual(1, p.dailyStreak);
            Assert.IsFalse(Meta(p, d1).ClaimDaily(out _, out _), "second claim the same day");
            Assert.IsTrue(Meta(p, d1.AddDays(1)).ClaimDaily(out int c2, out _));
            Assert.AreEqual(75, c2); Assert.AreEqual(2, p.dailyStreak);
            Assert.IsTrue(Meta(p, d1.AddDays(5)).ClaimDaily(out int c3, out _));
            Assert.AreEqual(50, c3, "a missed day restarts the streak"); Assert.AreEqual(1, p.dailyStreak);
            Assert.AreEqual(50 + 75 + 50, p.coins);
        }

        [Test]
        public void Daily_DaySeven_PaysGems()
        {
            var p = new PlayerProfile { coins = 0, dailyStreak = 6, lastDailyClaim = new DateTime(2026, 11, 9).ToString("yyyy-MM-dd") };
            Assert.IsTrue(Meta(p, new DateTime(2026, 11, 10)).ClaimDaily(out int coins, out int gems));
            Assert.AreEqual(300, coins); Assert.AreEqual(3, gems);
        }

        [Test]
        public void Missions_AreCreatedPerDay_Progress_AndPayOnce()
        {
            var p = new PlayerProfile { coins = 0 };
            var day = new DateTime(2026, 11, 10);
            var meta = Meta(p, day);
            meta.EnsureMissions();
            Assert.AreEqual(3, p.missions.Count);
            var again = p.missions[0];
            meta.EnsureMissions();
            Assert.AreSame(again, p.missions[0], "the same day keeps the same missions");

            // A huge run completes everything that a single run can complete.
            var stats = new RunStats { coins = 9999, flips = 99, boosts = 99, gems = 9, milestones = 9, airTime = 999f };
            meta.RecordRun(stats, 5000f, "vehicle-car");
            for (int i = 0; i < p.missions.Count; i++) Assert.GreaterOrEqual(p.missions[i].progress, p.missions[i].target, MetaProgress.Describe(p.missions[i]));
            Assert.IsTrue(meta.ClaimMission(0, out int coins, out _));
            Assert.Greater(coins, 0);
            Assert.IsFalse(meta.ClaimMission(0, out _, out _), "no double claim");
            Assert.AreEqual(9, p.gems - (p.missions[0].rewardGems), "gems found in the run are saved");

            Meta(p, day.AddDays(1)).EnsureMissions();
            Assert.AreEqual(0, p.missions[0].progress, "a new day gives fresh missions");
        }

        [Test]
        public void Mastery_GainsXp_AndLevelsUp()
        {
            var p = new PlayerProfile();
            var meta = Meta(p, new DateTime(2026, 11, 10));
            Assert.AreEqual(1, MetaProgress.LevelFromXp(0));
            Assert.AreEqual(2, MetaProgress.LevelFromXp(100));
            Assert.AreEqual(3, MetaProgress.LevelFromXp(300));
            meta.RecordRun(new RunStats { flips = 2 }, 1000f, "vehicle-taxi");
            Assert.AreEqual(110, meta.Xp("vehicle-taxi"));
        }

        [Test]
        public void Tuning_Defaults_AreSane()
        {
            var t = Tuning();
            Assert.Greater(t.boostCoinCost, 0); Assert.Greater(t.inkRegenPerSec, 0f); Assert.Less(t.inkRegenPerSec, 5f);
            Assert.AreEqual(7, t.dailyCoins.Length);
            Assert.LessOrEqual(t.inkChance + t.heartChance + t.gemChance + t.powerUpChance, 60, "coins stay the most common pickup");
        }
    }
}
