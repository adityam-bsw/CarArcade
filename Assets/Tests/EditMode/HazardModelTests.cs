using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace RoadBuilderGame.Tests
{
    /// <summary>Road-Eater, RO-B0, near-miss ledger, payouts and the road trim API (phase B2). Simulations are pure model runs: design arithmetic confirmed, not play-tested.</summary>
    public class HazardModelTests
    {
        private static MonsterDefinitionSO Monster() { return ScriptableObject.CreateInstance<MonsterDefinitionSO>(); }

        private static RoadEaterModel Chase(MonsterDefinitionSO d, float carS = 1000f, float roadBehind = 100f)
        {
            var m = new RoadEaterModel(d); m.Reset(7u); m.BeginWarn(); m.StartChase(carS, roadBehind); return m;
        }

        [Test]
        public void Eater_RatioTable_MatchesSpec()
        {
            var d = Monster();
            Assert.AreEqual(0.85f, d.Ratio(3f), 1e-4f);
            Assert.AreEqual(0.85f, d.Ratio(6f), 1e-4f);
            Assert.AreEqual(1f, d.Ratio(18f), 1e-4f);
            Assert.AreEqual(1.125f, d.Ratio(27f), 1e-4f);
            Assert.AreEqual(0.9f, d.Ratio(40f), 1e-4f);
            Assert.LessOrEqual(d.maxSpeedRatio, 1.25f);
        }

        [Test]
        public void Eater_SpawnsBehind_AndKeepsMinimumGap()
        {
            var d = Monster();
            Assert.AreEqual(32f, Chase(d).Gap, 1e-3f);
            Assert.AreEqual(18f, Chase(d, 1000f, 10f).Gap, 1e-3f, "less than 18 m of road behind: never closer than the target gap");
            Assert.AreEqual(25f, Chase(d, 1000f, 25f).Gap, 1e-3f, "road shorter than 32 m: spawn at the oldest road");
        }

        [Test]
        public void Eater_MovingCar_HoldsGap_NeverBites_NeverEntersProtectedZone()
        {
            var d = Monster(); var m = Chase(d); int bites = 0; m.Raised += e => { if (e == EaterEvent.Bite) bites++; };
            float carS = 1000f, minGap = 999f; float speed = 5f;
            for (int i = 0; i < 60 * 70; i++)          // 70 s at 60 fps, speed curve 5 -> 11.5 m/s
            {
                speed = Mathf.Min(11.5f, speed + 0.02f / 60f * speed * 60f * (1f / 60f));
                carS += speed / 60f; m.Tick(1f / 60f, carS, speed, false, false);
                Assert.LessOrEqual(m.EatFront, carS - d.protectedZone + 1e-3f);
                Assert.LessOrEqual(m.Speed, d.maxSpeedRatio * Mathf.Max(speed, d.vFloor) + 4f * 2f / 60f + 1e-3f, "cap 1.25 x v_ref (plus one frame of accel lag)");
                minGap = Mathf.Min(minGap, m.Gap);
            }
            Assert.AreEqual(0, bites);
            Assert.GreaterOrEqual(minGap, d.snapGap);
            Assert.That(m.Gap, Is.InRange(14f, 26f), "the controller holds the gap near the 18 m target");
        }

        [Test]
        public void Eater_StoppedCar_SnapsThenBitesOnce_ThenPushback()
        {
            var d = Monster(); var m = Chase(d); int snaps = 0, bites = 0; float tBite = -1f; float t = 0f;
            m.Raised += e => { if (e == EaterEvent.SnapStart) snaps++; if (e == EaterEvent.Bite) { bites++; tBite = t; } };
            for (int i = 0; i < 60 * 12 && bites == 0; i++) { t += 1f / 60f; m.Tick(1f / 60f, 1000f, 0f, false, false); }
            Assert.AreEqual(1, snaps); Assert.AreEqual(1, bites);
            Assert.Greater(tBite, 3f, "hesitation is punished, not a sprint"); Assert.Less(tBite, 12f, "from the 32 m spawn gap a stopped car is bitten in about 10 s");
            m.OnLifeLost(1002f);
            Assert.AreEqual(EaterState.Pushback, m.State); Assert.AreEqual(32f, m.Gap, 1e-3f);
            for (int i = 0; i < 60 * 2; i++) m.Tick(1f / 60f, 1002f, 0f, false, false);
            Assert.AreEqual(EaterState.Track, m.State);
        }

        [Test]
        public void Eater_Snap_NeverEatsWithinSixMetresOfCar()
        {
            var d = Monster(); var m = Chase(d);
            for (int i = 0; i < 60 * 8; i++) { m.Tick(1f / 60f, 1000f, 0f, false, false); Assert.LessOrEqual(m.EatFront, 1000f - d.protectedZone + 1e-3f); }
        }

        [Test]
        public void Eater_FastCar_EscapesAfterEightSecondsAt45m()
        {
            var d = Monster(); var m = Chase(d); bool escaped = false; float t = 0f, gapAt45 = -1f;
            m.Raised += e => { if (e == EaterEvent.Escaped) escaped = true; };
            float carS = 1000f; float v = 3f;
            for (int i = 0; i < 60 * 40 && !escaped; i++)
            {
                v = Mathf.Min(20f, v + 8f / 60f);          // a boost sprint: the Eater's 4 m/s2 acceleration lags the car
                carS += v / 60f; t += 1f / 60f; m.Tick(1f / 60f, carS, v, false, true);
                if (gapAt45 < 0f && m.Gap >= 45f) gapAt45 = t;
            }
            Assert.IsTrue(escaped, "gap 45 m held for 8 s escapes");
            Assert.Greater(t - gapAt45, 7.9f);
        }

        [Test]
        public void Eater_TimesOut_AtTimeoutDistance()
        {
            var d = Monster(); var m = Chase(d, 1700f); bool timeout = false;
            m.Raised += e => { if (e == EaterEvent.TimedOut) timeout = true; };
            float carS = 1700f;
            for (int i = 0; i < 60 * 10 && !timeout; i++) { carS += 10f / 60f; m.Tick(1f / 60f, carS, 10f, false, false); }
            Assert.IsTrue(timeout); Assert.GreaterOrEqual(carS, d.timeoutDistance);
        }

        [Test]
        public void Eater_ChewDelay_BoundedByCooldownAndCap()
        {
            var d = Monster(); var m = Chase(d);
            Assert.IsTrue(m.OnGapReached(10f));
            Assert.IsFalse(m.OnGapReached(10f), "cooldown");
            Assert.IsFalse(m.OnGapReached(20f), "gaps over 14 m are not rewarded");
            Assert.IsFalse(m.OnGapReached(3f), "gaps under 6 m are not rewarded");
        }

        [Test]
        public void Eater_Tick_AllocatesNothing()
        {
            var d = Monster(); var m = Chase(d); float carS = 1000f;
            for (int i = 0; i < 600; i++) { carS += 0.15f; m.Tick(1f / 60f, carS, 9f, false, false); }
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 3000; i++) { carS += 0.15f; m.Tick(1f / 60f, carS, 9f, false, false); }
            Assert.AreEqual(0L, GC.GetAllocatedBytesForCurrentThread() - before);
        }

        // ------------------------------------------------------------------ near-miss ledger
        [Test]
        public void NearMiss_ChainSteps_Perfect_CapAndGuard()
        {
            var l = new NearMissLedger(); l.Reset();
            Assert.AreEqual(8, l.Resolve(1, false, 20f, 8f, 1000).Coins);
            l.Tick(1f); Assert.AreEqual(10, l.Resolve(2, false, 20f, 8f, 1000).Coins);
            l.Tick(1f); Assert.AreEqual(12, l.Resolve(3, false, 20f, 8f, 1000).Coins);
            l.Tick(1f); Assert.AreEqual(15, l.Resolve(1, false, 20f, 8f, 1000).Coins);
            l.Tick(1f); Assert.AreEqual(30, l.Resolve(2, true, 20f, 8f, 1000).Coins, "perfect doubles the 15 step");
            l.Tick(5f); Assert.AreEqual(8, l.Resolve(3, false, 20f, 8f, 1000).Coins, "the chain resets after 4 s");

            var c = new NearMissLedger(); c.Reset();
            Assert.AreEqual(8, c.Resolve(1, false, 20f, 8f, 100).Coins);       // room = floor(0.12 / 0.88 x 100) = 13
            c.Tick(1f); var r = c.Resolve(2, false, 20f, 8f, 100);
            Assert.AreEqual(5, r.Coins); Assert.IsTrue(r.Capped); Assert.IsTrue(r.Valid, "a capped near miss still shows the toast");
            Assert.AreEqual(13, c.Paid);

            var g = new NearMissLedger(); g.Reset();
            Assert.IsFalse(g.Resolve(1, false, 5f, 8f, 1000).Valid, "travelled under 8 m during the telegraph");
            Assert.IsFalse(g.Resolve(1, false, 20f, 2f, 1000).Valid, "averaging under 3 m/s");
            Assert.IsTrue(g.Resolve(1, false, 20f, 8f, 1000).Valid);
            g.Tick(0.5f); Assert.IsFalse(g.Resolve(1, false, 20f, 8f, 1000).Valid, "1.5 s between payments from the same source");
            g.NoteRespawn(); g.Tick(1f); Assert.IsFalse(g.Resolve(2, false, 20f, 8f, 1000).Valid, "grace after a respawn");
        }

        [Test]
        public void NearMiss_Eater_Pays20_ThroughTheSameCap()
        {
            var l = new NearMissLedger(); l.Reset();
            Assert.AreEqual(20, l.ResolveEater(1000).Coins);
            Assert.AreEqual(0, l.ResolveEater(0).Coins);
        }

        [Test]
        public void NearMiss_Resolve_AllocatesNothing()
        {
            var l = new NearMissLedger(); l.Reset(); l.Resolve(1, false, 20f, 8f, 1000);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++) { l.Tick(2f); l.Resolve(i & 7, (i & 1) == 0, 20f, 8f, 5000); }
            Assert.AreEqual(0L, GC.GetAllocatedBytesForCurrentThread() - before);
        }

        // ------------------------------------------------------------------ payouts
        [Test]
        public void Rewards_RepeatClearsPayHalf()
        {
            var p = new PlayerProfile(); var m = Monster(); var b = BossDefinitionSO.CreateDefault();
            Assert.AreEqual(150, HazardRewards.Eater(m, p, false).Coins); Assert.AreEqual(75, HazardRewards.Eater(m, p, true).Coins);
            HazardRewards.RecordEater(p, false);
            Assert.AreEqual(75, HazardRewards.Eater(m, p, false).Coins); Assert.AreEqual(38, HazardRewards.Eater(m, p, true).Coins);
            var f = HazardRewards.Boss(b, p, false); Assert.AreEqual(200, f.Coins); Assert.AreEqual(1, f.Gems);
            Assert.AreEqual(100, HazardRewards.Boss(b, p, true).Coins);
            HazardRewards.RecordBoss(p, false);
            var r = HazardRewards.Boss(b, p, false); Assert.AreEqual(100, r.Coins); Assert.AreEqual(0, r.Gems);
            Assert.AreEqual(50, HazardRewards.Boss(b, p, true).Coins);
        }

        // ------------------------------------------------------------------ RO-B0
        private static BossContext Ctx() { return new BossContext { CarSpeed = 10f, CounterAvailable = true, InkAvailable = true, RoadAheadForRoller = true, SinceBolt = 99f, SinceBomb = 99f, SinceRoller = 99f }; }

        [Test]
        public void Boss_Patterns_ContactTimesAtLeast0_8sApart_And_TellsAboveFloor()
        {
            var d = BossDefinitionSO.CreateDefault();
            Assert.GreaterOrEqual(d.bolt.tellSeconds, 1.2f); Assert.GreaterOrEqual(d.bomb.tellSeconds, 1.2f); Assert.GreaterOrEqual(d.roller.tellSeconds, 1.2f);
            var seen = new HashSet<AttackPatternSO>();
            foreach (var pool in new[] { d.pool1, d.pool2, d.pool3 }) foreach (var p in pool) seen.Add(p);
            Assert.AreEqual(5, seen.Count, "five reusable patterns");
            foreach (var p in seen)
            {
                var contacts = new List<float>();
                foreach (var b in p.beats)
                {
                    var def = d.Projectile(b.projectile);
                    float flight = b.projectile == ProjectileKind.Bolt ? 0.7f : b.projectile == ProjectileKind.Roller ? 1.0f : 0f;
                    contacts.Add(b.tOffset + def.tellSeconds + flight);
                }
                contacts.Sort();
                for (int i = 1; i < contacts.Count; i++) Assert.GreaterOrEqual(contacts[i] - contacts[i - 1], 0.8f, p.id + " contact separation");
            }
        }

        [Test]
        public void Boss_Meter_DodgeHitDefeatAndPhases()
        {
            var d = BossDefinitionSO.CreateDefault(); var m = new BossModel(d); m.Reset(3u); m.Begin();
            Assert.AreEqual(BossState.Intro, m.State);
            m.Resolve(BossOutcome.Dodged); Assert.AreEqual(3f, m.Meter, 1e-4f);
            m.Resolve(BossOutcome.Near); m.Resolve(BossOutcome.Perfect); Assert.AreEqual(14f, m.Meter, 1e-4f);
            m.Tick(2.1f, Ctx());
            m.Resolve(BossOutcome.Hit); Assert.AreEqual(6f, m.Meter, 1e-4f); Assert.AreEqual(BossState.Lull, m.State);
            m.Resolve(BossOutcome.HitShielded); Assert.AreEqual(6f, m.Meter, 1e-4f, "a shield-absorbed hit costs no meter");
            m.Resolve(BossOutcome.Hit); m.Resolve(BossOutcome.Hit); Assert.AreEqual(0f, m.Meter, 1e-4f, "clamped at 0");
            Assert.AreEqual(1, m.PhaseFor(32f)); Assert.AreEqual(2, m.PhaseFor(33f)); Assert.AreEqual(3, m.PhaseFor(66f));
            bool defeated = false; m.Raised += e => { if (e == BossEvent.Defeated) defeated = true; };
            for (int i = 0; i < 40; i++) m.Resolve(BossOutcome.Perfect);
            Assert.IsTrue(defeated); Assert.AreEqual(BossState.Defeat, m.State);
        }

        [Test]
        public void Boss_PerfectPlayer_ReachesOverload_OneRingPerPattern_NoRepeat_FairBeats()
        {
            var d = BossDefinitionSO.CreateDefault(); var m = new BossModel(d); m.Reset(11u);
            int rings = 0, patterns = 0, phaseChanges = 0, beats = 0; bool defeated = false; float defeatT = 0f;
            var ids = new List<string>(); var pending = new List<float>(); var ctx = Ctx(); float t = 0f;
            m.Raised += e =>
            {
                if (e == BossEvent.RingDue) { rings++; m.RingPassed(); }
                if (e == BossEvent.PatternStart) { patterns++; ids.Add(m.CurrentPattern.id); }
                if (e == BossEvent.PhaseChanged) phaseChanges++;
                if (e == BossEvent.Defeated) { defeated = true; defeatT = t; }
            };
            m.Beat += r =>
            {
                beats++;
                Assert.IsFalse(r.WarningOnly); Assert.IsTrue(m.QuietLeft <= 0f, "no projectile starts in a quiet window");
                pending.Add(t + 2.4f);
            };
            m.Begin();
            for (int i = 0; i < 60 * 120 && !defeated; i++)
            {
                t += 1f / 60f;
                for (int k = pending.Count - 1; k >= 0; k--) if (t >= pending[k]) { pending.RemoveAt(k); m.Resolve(BossOutcome.Dodged); }
                m.Tick(1f / 60f, ctx);
            }
            Assert.IsTrue(defeated, "dodging reaches Overload");
            Assert.That(defeatT, Is.InRange(35f, 105f), "a fight lasts under the 105 s timeout");
            Assert.GreaterOrEqual(phaseChanges, 2);
            for (int i = 1; i < ids.Count; i++) Assert.AreNotEqual(ids[i - 1], ids[i], "never the same pattern twice in a row");
            Assert.GreaterOrEqual(rings, patterns - 1, "one ring per finished pattern");
            Assert.LessOrEqual(rings, patterns);
        }

        [Test]
        public void Boss_StationaryCar_OnlyWarningShots_And_FailedGateDowngrades()
        {
            var d = BossDefinitionSO.CreateDefault(); var m = new BossModel(d); m.Reset(5u);
            var got = new List<BeatRequest>(); m.Beat += r => got.Add(r);
            var ctx = Ctx(); ctx.CarSpeed = 0f; m.Begin();
            for (int i = 0; i < 60 * 40; i++) { m.Tick(1f / 60f, ctx); if (i % 180 == 0) m.Resolve(BossOutcome.Dodged); }
            Assert.Greater(got.Count, 0);
            foreach (var r in got) { Assert.AreNotEqual(ProjectileKind.Roller, r.Kind, "rollers skipped"); if (r.Kind == ProjectileKind.Bolt) Assert.IsTrue(r.WarningOnly); else Assert.AreNotEqual(BeatMode.Direct, r.Mode); }

            var m2 = new BossModel(d); m2.Reset(5u); var got2 = new List<BeatRequest>(); m2.Beat += r => got2.Add(r);
            var c2 = Ctx(); c2.CounterAvailable = false; c2.InkAvailable = false; m2.Begin();
            for (int i = 0; i < 60 * 60; i++) { m2.Tick(1f / 60f, c2); if (i % 120 == 0) m2.Resolve(BossOutcome.Dodged); }
            foreach (var r in got2) { Assert.AreNotEqual(ProjectileKind.Roller, r.Kind); if (r.Kind == ProjectileKind.Bolt) Assert.IsTrue(r.WarningOnly); if (r.Kind == ProjectileKind.Bomb) Assert.AreEqual(BeatMode.Lead, r.Mode); }
        }

        [Test]
        public void Boss_TimesOutAt105s()
        {
            var d = BossDefinitionSO.CreateDefault(); d.meterPassivePerSec = 0f; var m = new BossModel(d); m.Reset(9u); bool timeout = false; float t = 0f;
            m.Raised += e => { if (e == BossEvent.TimedOut) timeout = true; };
            m.Begin(); var ctx = Ctx();
            for (int i = 0; i < 60 * 120 && !timeout; i++) { t += 1f / 60f; m.Tick(1f / 60f, ctx); }
            Assert.IsTrue(timeout); Assert.That(t, Is.InRange(105f, 109f));
        }

        [Test]
        public void Boss_Tick_AllocatesNothing()
        {
            var d = BossDefinitionSO.CreateDefault(); var m = new BossModel(d); m.Reset(1u); m.Begin(); var ctx = Ctx();
            m.Beat += r => { }; m.Raised += e => { };
            for (int i = 0; i < 600; i++) m.Tick(1f / 60f, ctx);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 3000; i++) m.Tick(1f / 60f, ctx);
            Assert.AreEqual(0L, GC.GetAllocatedBytesForCurrentThread() - before);
        }

        [Test]
        public void Projectile_Thresholds_MatchSpecTable()
        {
            var d = BossDefinitionSO.CreateDefault();
            Assert.AreEqual(0.85f, d.bolt.HitCentre, 1e-3f); Assert.AreEqual(2.7f, d.bomb.HitCentre, 1e-3f); Assert.AreEqual(1.05f, d.roller.HitCentre, 1e-3f);
            Assert.AreEqual(3.6f, d.bomb.PerfectEdge, 1e-3f); Assert.AreEqual(1.225f, d.bolt.PerfectEdge, 1e-3f);
        }

        // ------------------------------------------------------------------ planner timing and cue registry
        [Test]
        public void Planner_EaterS1_IsAtEaterWarnM()
        {
            var ladder = StageTestData.SliceLadder(); var d = new StageDirector(ladder); d.Begin(StageTestData.Record(21u));
            StageEventState st = null; foreach (var e in d.Plan.Events) if (e.Id == "MON-EATER") st = e;
            Assert.IsNotNull(st);
            Assert.AreEqual(940f, st.WarnM, 1e-3f);
        }

        [Test]
        public void Cues_NewHazardIds_AreRegistered_WithPlaceholders()
        {
            foreach (var id in new[] { StageCues.EaterSurge, StageCues.ProjBolt, StageCues.BossPhase, StageCues.BossRing, StageCues.Finish, StageCues.FireBolt, StageCues.PhaseFlare })
            {
                Assert.IsTrue(StageCues.Registry.Contains(id), id);
                StageCues.Placeholder(id, out string sfx, out _, out _);
                Assert.IsNotNull(sfx, id);
            }
        }

        // ------------------------------------------------------------------ road trim API (partial trim only: whole-chunk removal uses Destroy, a play-mode call)
        [Test]
        public void Road_EatBefore_TrimsChunkCollider_And_RespectsNoEat()
        {
            var go = new GameObject("RoadTest"); var rb = go.AddComponent<RoadBuilder>();
            try
            {
                rb.Init(new PhysicsMaterial2D("t")); rb.ResetRoad(0f, 120f, 0f);
                Assert.IsTrue(rb.HasRoadAt(10f));
                rb.AddNoEatRange(0f, 100f);
                Assert.AreEqual(0, rb.EatBefore(5f, 1.5f, 1f), "noEat range protects the road");
                Assert.IsTrue(rb.HasRoadAt(10f));
                rb.ClearNoEatRanges();
                int n = rb.EatBefore(5.4f, 1.5f, 1f);
                Assert.GreaterOrEqual(n, 1);
                Assert.IsFalse(rb.HasRoadAt(2f), "trimmed road is gone");
                Assert.IsTrue(rb.HasRoadAt(30f), "road ahead of the front stays");
                var chunk = go.GetComponentInChildren<RoadChunk>();
                Assert.GreaterOrEqual(chunk.ColliderPoints[0].x, 4.5f, "collider cut in the same call as the mesh");
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
    }
}
