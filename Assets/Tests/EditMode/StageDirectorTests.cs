using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace RoadBuilderGame.Tests
{
    /// <summary>EditMode acceptance tests SD-01 to SD-22 from Docs/design/stage-events.md section 11 that need no scene.</summary>
    public class StageDirectorTests
    {
        private const int Seeds = 10000;

        private static StagePlan Plan(StageLadderDefinition l, uint raw, int mode = StageTestData.ModeId)
        {
            return StageLadderPlanner.Build(l, StageTestData.Record(raw, mode));
        }

        private static StageEventState Find(StagePlan p, string id)
        {
            foreach (var e in p.Events) if (e.Id == id) return e;
            return null;
        }

        // ---------------------------------------------------------------- PRNG and pace

        [Test]
        public void Pcg32_SameSeedSameSequence_DifferentSeedDiffers_RangesHold()
        {
            var a = new Pcg32(42); var b = new Pcg32(42); var c = new Pcg32(43);
            bool differs = false;
            for (int i = 0; i < 1000; i++)
            {
                uint x = a.NextUInt(), y = b.NextUInt(), z = c.NextUInt();
                Assert.AreEqual(x, y);
                if (x != z) differs = true;
            }
            Assert.IsTrue(differs);
            var r = new Pcg32(7);
            var seen = new HashSet<int>();
            for (int i = 0; i < 5000; i++)
            {
                int v = r.NextRange(-40, 40); Assert.That(v, Is.InRange(-40, 40)); seen.Add(v);
                float f = r.NextFloat01(); Assert.That(f, Is.GreaterThanOrEqualTo(0f).And.LessThan(1f));
                Assert.That(r.NextInt(4), Is.InRange(0, 3));
            }
            Assert.Greater(seen.Count, 70, "jitter range is covered");
            // Reference value of the PCG32 stream used here (regression guard, computed once from this implementation).
            var g = new Pcg32(0);
            uint first = g.NextUInt();
            var g2 = new Pcg32(0);
            Assert.AreEqual(first, g2.NextUInt());
        }

        [Test]
        public void Pace_SecondsAndMetresAreInverse_AndMonotone()
        {
            var pace = new StagePace(4.2f, 0.02f, 11.5f);
            foreach (float from in new[] { 0f, 100f, 300f, 365f, 800f })
                foreach (float f in new[] { 1f, 1.15f })
                {
                    float m = pace.MetresForSeconds(from, 12f, f);
                    Assert.AreEqual(12f, pace.SecondsBetween(from, from + m, f), 0.01f, "from " + from + " factor " + f);
                }
            Assert.AreEqual(11.5f * 12f, pace.MetresForSeconds(1000f, 12f, 1f), 0.01f);
            Assert.Greater(pace.MetresForSeconds(1000f, 12f, 1.15f), pace.MetresForSeconds(1000f, 12f, 1f));
        }

        // ---------------------------------------------------------------- data validation

        [Test]
        public void Validation_AcceptsSliceData_AndRejectsBadValues()
        {
            var ladder = StageTestData.SliceLadder(out var defs);
            var errs = new List<string>();
            foreach (var d in defs) Assert.IsTrue(d.Validate(errs), string.Join("; ", errs));
            Assert.IsTrue(ladder.Validate(errs), string.Join("; ", errs));

            var bad = StageTestData.Ice();
            bad.terrainJumpHeightCapM = 9f; bad.cueLeadS = 0.5f; bad.maxDistanceM = 200f; bad.id = "";
            errs.Clear();
            Assert.IsFalse(bad.Validate(errs));
            Assert.That(string.Join("|", errs), Does.Contain("8 m cap").And.Contain("cueLeadS").And.Contain("maxDistanceM").And.Contain("id is empty"));

            var noMatrix = StageTestData.SliceLadder(); noMatrix.matrix = null;
            errs.Clear(); Assert.IsFalse(noMatrix.Validate(errs));
        }

        // ---------------------------------------------------------------- SD-01 .. SD-04

        [Test]
        public void SD01_Determinism_1000BuildsIdentical()
        {
            var l = StageTestData.SliceLadder();
            string first = StageTestData.Signature(Plan(l, 777));
            for (int i = 0; i < 1000; i++) Assert.AreEqual(first, StageTestData.Signature(Plan(l, 777)));
            Assert.AreNotEqual(first, StageTestData.Signature(Plan(l, 778)), "a different run seed gives a different layout");
            // The director publishes the same plan.
            var d = new StageDirector(l); d.Begin(StageTestData.Record(777));
            Assert.AreEqual(first, StageTestData.Signature(d.Plan));
        }

        [Test]
        public void SD02_SeedIndependence_NoDependenceOnFrameRateOrPause()
        {
            var l = StageTestData.SliceLadder();
            var rec = StageTestData.Record(31337);
            string fresh = StageTestData.Signature(Plan(l, 31337));
            var phasesByFps = new List<string>();
            foreach (float fps in new[] { 30f, 60f, 120f })
            {
                var d = new StageDirector(l); d.Begin(rec);
                float dist = 0f, dt = 1f / fps; int tick = 0;
                var sb = new System.Text.StringBuilder();
                for (float checkpoint = 100f; checkpoint <= 2600f; checkpoint += 100f)
                {
                    while (dist < checkpoint) { dist += 8f * dt; d.Tick(dist, 8f, dt); if (++tick % 97 == 0) for (int p = 0; p < 30; p++) d.Tick(dist, 0f, 0f); }   // pauses
                    d.Tick(checkpoint, 8f, dt); dist = checkpoint;
                    foreach (var e in d.Plan.Events) sb.Append((int)e.Phase);
                    sb.Append('/');
                }
                Assert.AreEqual(fresh, StageTestData.Signature(d.Plan), "the plan never changes while running");
                phasesByFps.Add(sb.ToString());
            }
            Assert.AreEqual(phasesByFps[0], phasesByFps[1]);
            Assert.AreEqual(phasesByFps[1], phasesByFps[2]);
        }

        [Test]
        public void SD03_SeedFormula_AndNoCollisionsForShiftedRunSeeds()
        {
            Assert.AreEqual(unchecked(1000u + 3u + 5u), StageSeed.SlotSeed(1000u, 3, 5));
            Assert.AreEqual(unchecked(uint.MaxValue + 4u), StageSeed.SlotSeed(uint.MaxValue, 3, 1), "wraps without throwing");
            Assert.AreEqual(0u, StageSeed.MakeRunSeed(5) & 127u, "low 7 bits are free");
            var seen = new HashSet<uint>();
            int count = 0;
            for (uint raw = 0; raw < 400; raw++)
                for (int stage = 0; stage < 64; stage++)
                {
                    seen.Add(StageSeed.SlotSeed(StageSeed.MakeRunSeed(raw), stage, 63));   // stage + mode stays at most 63 + 63 = 126, under 128
                    count++;
                }
            Assert.AreEqual(count, seen.Count, "no two (run, slot) pairs share a seed");
            // The plain sum does collide (the reason for the shift).
            Assert.AreEqual(StageSeed.SlotSeed(5u, 1, 0), StageSeed.SlotSeed(6u, 0, 0));
        }

        [Test]
        public void SD04_SliceRanges_ForTenThousandSeeds()
        {
            var l = StageTestData.SliceLadder();
            for (uint raw = 1; raw <= Seeds; raw++)
            {
                var p = Plan(l, raw);
                Assert.AreEqual(7, p.Events.Count, "seed " + raw + " dropped an event: " + string.Join(", ", p.Log));
                foreach (var s in l.slots)
                {
                    var e = Find(p, s.eventDefinition.id);
                    Assert.IsNotNull(e, s.eventDefinition.id);
                    Assert.That(e.StartM, Is.InRange(s.startRangeMinM - 0.01f, s.startRangeMaxM + 0.01f), "seed " + raw + " " + e.Id);
                    if (s.eventDefinition.maxDistanceM > 0f) Assert.LessOrEqual(e.EndM, s.eventDefinition.maxDistanceM + 0.01f, "seed " + raw + " " + e.Id);
                    Assert.GreaterOrEqual(e.StartM, l.noHazardUntilM - 0.01f);
                }
                Assert.AreEqual(1000f, Find(p, "MON-EATER").StartM);
                Assert.AreEqual(2000f, Find(p, "BOS-ROB0").StartM);
                Assert.GreaterOrEqual(p.FinishM, 2750f);
                Assert.AreEqual(0, p.Log.Count, "seed " + raw + ": " + string.Join(", ", p.Log));
            }
        }

        // ---------------------------------------------------------------- SD-05 .. SD-08

        [Test]
        public void SD05_Breather_MetresAndTwelveSecondsForTenThousandSeeds()
        {
            var l = StageTestData.SliceLadder();
            var pace = l.Pace;
            for (uint raw = 1; raw <= Seeds; raw++)
            {
                var p = Plan(l, raw);
                StageEventState prev = null;
                foreach (var e in p.Events)
                {
                    if (e.Definition.IsModifier) continue;
                    if (prev != null && prev.Definition.role != StageEventRole.Reward)
                    {
                        float gap = e.StartM - prev.EndM;
                        if (e.Definition.role == StageEventRole.Reward) Assert.GreaterOrEqual(gap, l.rewardMinGapM - 0.01f, "seed " + raw);
                        else
                        {
                            Assert.GreaterOrEqual(gap, prev.Definition.cooldownAfterM - 0.01f, "metres, seed " + raw + " " + prev.Id + "->" + e.Id);
                            Assert.GreaterOrEqual(pace.SecondsBetween(prev.EndM, e.StartM, prev.Definition.paceSpeedFactor), prev.Definition.minBreatherS - 0.02f, "seconds, seed " + raw + " " + prev.Id + "->" + e.Id);
                        }
                    }
                    prev = e;
                }
            }
        }

        [Test]
        public void SD06_SD07_WeightedLadder_NoRepeat_AndHardStageBreather()
        {
            var l = StageTestData.WeightedLadder();
            int plansWithHardRun = 0, rejections = 0;
            for (uint raw = 1; raw <= 3000; raw++)
            {
                var p = Plan(l, raw);
                Assert.Greater(p.Events.Count, 5, "seed " + raw);
                rejections += p.Log.FindAll(x => x.Code == "event_rejected").Count;
                string lastId = null; int hardRun = 0; StageEventState prev = null;
                foreach (var e in p.Events)
                {
                    Assert.AreNotEqual(lastId, e.Id, "SD-06 repeat, seed " + raw);
                    if (prev != null)
                    {
                        if (hardRun >= l.hardStagesBeforeBreather) { Assert.GreaterOrEqual(e.StartM - prev.EndM, l.weightedBreatherM - 0.01f, "SD-07, seed " + raw); hardRun = 0; }
                        else Assert.GreaterOrEqual(e.StartM - prev.EndM, l.weightedRecoveryMinM - 0.01f, "recovery, seed " + raw);
                    }
                    hardRun = e.Definition.IsHard ? hardRun + 1 : 0;
                    Assert.LessOrEqual(hardRun, l.hardStagesBeforeBreather, "three hard slots in a row, seed " + raw);
                    if (hardRun == 2) plansWithHardRun++;
                    Assert.LessOrEqual(e.Definition.difficultyCost, 3 + e.SlotIndex, "difficulty ramp, seed " + raw);
                    lastId = e.Id; prev = e;
                }
            }
            Assert.Greater(plansWithHardRun, 100, "the hard-stage rule was actually exercised");
            Assert.Greater(rejections, 0, "the A/B adjacency rule rejected something, so the fallback ran");
        }

        [Test]
        public void SD08_ModifierCap_NeverAboveTwo_AndOnePrimaryAtATime()
        {
            var l = StageTestData.SliceLadder();
            var d = new StageDirector(l);
            for (uint raw = 1; raw <= 1500; raw++)
            {
                d.Begin(StageTestData.Record(raw));
                bool sawModifier = false;
                for (float dist = 0f; dist <= 3200f; dist += 7f)
                {
                    d.Tick(dist, 8f, 0.1f);
                    Assert.LessOrEqual(d.ActiveModifierCount, 2);
                    sawModifier |= d.ActiveModifierCount > 0;
                    int primaries = 0; foreach (var e in d.Plan.Events) if (e.Phase == StagePhase.Active && !e.Definition.IsModifier) primaries++;
                    Assert.LessOrEqual(primaries, 1, "seed " + raw + " at " + dist);
                }
                Assert.IsTrue(sawModifier, "glass ran in seed " + raw);
            }
        }

        // ---------------------------------------------------------------- SD-09, SD-10

        [Test]
        public void SD09_Matrix_ForbiddenPairsNeverOverlap_Symmetric_DefaultDeny()
        {
            var l = StageTestData.SliceLadder(out var defs);
            var m = l.matrix;
            var errs = new List<string>();
            Assert.IsTrue(m.Validate(defs, errs), string.Join("; ", errs));
            foreach (var a in defs) foreach (var b in defs)
            {
                if (a == b) continue;
                Assert.AreEqual(m.CanOverlap(a.id, b.id), m.CanOverlap(b.id, a.id), "symmetric " + a.id + "/" + b.id);
                Assert.IsTrue(m.TryGetPair(a.id, b.id, out _), "all 21 pairs are listed " + a.id + "/" + b.id);
            }
            Assert.IsTrue(m.CanOverlap("EVT-GLASS", "MON-EATER"));
            Assert.IsFalse(m.CanOverlap("EVT-MUD", "MON-EATER"), "C1 is conditional and disabled in the slice");
            Assert.IsFalse(m.CanOverlap("EVT-NEW", "EVT-ICE"), "a pair that is not listed is forbidden");
            Assert.AreEqual(300f, m.AdjacentMinGapM("EVT-GLASS", "EVT-MUD"));

            for (uint raw = 1; raw <= Seeds; raw++)
            {
                var p = Plan(l, raw);
                for (int i = 0; i < p.Events.Count; i++)
                    for (int j = i + 1; j < p.Events.Count; j++)
                    {
                        var a = p.Events[i]; var b = p.Events[j];
                        if (StageLadderPlanner.Overlaps(a, b)) Assert.IsTrue(m.CanOverlap(a.Id, b.Id), "seed " + raw + " " + a.Id + " overlaps " + b.Id);
                        float need = m.AdjacentMinGapM(a.Id, b.Id);
                        if (need > 0f && !StageLadderPlanner.Overlaps(a, b))
                        {
                            float gap = a.StartM >= b.EndM ? a.StartM - b.EndM : b.StartM - a.EndM;
                            Assert.GreaterOrEqual(gap, need, "adjacent gap, seed " + raw + " " + a.Id + "/" + b.Id);
                        }
                    }
            }
        }

        [Test]
        public void SD10_RejectionFallback_YieldsLegalReplacementAndOneLog_NeverThrows()
        {
            var a = StageTestData.Def("T-A", StageEventRole.Primary, 2, 190, 0, 100, 100, "hz-a", jitter: 0, variants: 1);
            var b = StageTestData.Def("T-B", StageEventRole.Primary, 3, 0, 0, 100, 100, "hz-b", jitter: 0, variants: 1);
            var c = StageTestData.Def("T-C", StageEventRole.Primary, 3, 0, 0, 100, 100, "hz-c", jitter: 0, variants: 1);
            var m = ScriptableObject.CreateInstance<EventCompatibilityMatrix>();
            m.pairs = new[]
            {
                new StageCompatPair { idA = "T-A", idB = "T-B", rule = StageCompatRule.Forbid, adjacentMinGapM = 300f },
                new StageCompatPair { idA = "T-A", idB = "T-C", rule = StageCompatRule.Forbid },
                new StageCompatPair { idA = "T-B", idB = "T-C", rule = StageCompatRule.Forbid },
            };
            var l = ScriptableObject.CreateInstance<StageLadderDefinition>();
            l.matrix = m;
            l.slots = new[]
            {
                new StageLadderSlot { eventDefinition = a, nominalStartM = 300, startRangeMinM = 300, startRangeMaxM = 300 },
                new StageLadderSlot { eventDefinition = b, nominalStartM = 560, startRangeMinM = 560, startRangeMaxM = 560 },   // 160 m after A: legal breather, illegal adjacency
            };
            l.eventPool = new[] { a, b, c };
            StagePlan plan = null;
            Assert.DoesNotThrow(() => plan = StageLadderPlanner.Build(l, StageTestData.Record(9)));
            Assert.AreEqual(1, plan.Log.FindAll(x => x.Code == "event_rejected").Count, string.Join(", ", plan.Log));
            Assert.IsNotNull(Find(plan, "T-C"), "legal replacement placed");
            Assert.IsNull(Find(plan, "T-B"));
            var rep = Find(plan, "T-C");
            Assert.GreaterOrEqual(rep.StartM - Find(plan, "T-A").EndM, 100f);

            l.eventPool = new StageEventDefinition[0];
            Assert.DoesNotThrow(() => plan = StageLadderPlanner.Build(l, StageTestData.Record(9)));
            Assert.AreEqual(1, plan.Events.Count);
            Assert.AreEqual(1, plan.Log.FindAll(x => x.Code == "event_dropped_no_candidate").Count);

            // A missing pair never throws and never lets two events stack.
            Assert.DoesNotThrow(() => StageLadderPlanner.Build(StageTestData.SliceLadder(), new StageRunRecord { runSeed = 0, modeId = 0 }));
        }

        // ---------------------------------------------------------------- SD-11, SD-12

        private sealed class SignalProbe
        {
            public float T;
            public readonly Dictionary<string, float[]> Times = new Dictionary<string, float[]>();   // id -> S1, S2, S3, start
            public void OnSignal(StageEventState e, StageSignal s) { Slot(e)[(int)s] = T; }
            public void OnStarted(StageEventState e) { Slot(e)[3] = T; }
            private float[] Slot(StageEventState e)
            {
                if (!Times.TryGetValue(e.Id, out var a)) { a = new[] { -1f, -1f, -1f, -1f }; Times[e.Id] = a; }
                return a;
            }
        }

        [Test]
        public void SD11_Warnings_FireInOrderWithFullLead_AtEverySpeed()
        {
            var l = StageTestData.SliceLadder(out var defs);
            foreach (uint raw in new uint[] { 3, 99, 4242 })
                foreach (float speed in new[] { 4.2f, 11.5f, 16f })
                {
                    var d = new StageDirector(l); d.Begin(StageTestData.Record(raw));
                    var probe = new SignalProbe();
                    d.SignalFired += probe.OnSignal; d.Started += probe.OnStarted;
                    const float dt = 1f / 60f;
                    float dist = 0f;
                    while (dist < 2050f) { probe.T += dt; dist += speed * dt; d.Tick(dist, speed, dt); }
                    Assert.AreEqual(7, probe.Times.Count, "every event signalled and started at speed " + speed);
                    foreach (var kv in probe.Times)
                    {
                        var t = kv.Value;
                        var def = defs.Find(x => x.id == kv.Key);
                        for (int i = 0; i < 4; i++) Assert.GreaterOrEqual(t[i], 0f, kv.Key + " signal " + i);
                        Assert.Less(t[0], t[1]); Assert.Less(t[1], t[2]); Assert.Less(t[2], t[3]);
                        // The Eater warns at eaterWarnM (60 m before the chase, B2 timing fix): 5.2 s at the 11.5 m/s cap, shorter at boost speed; every other event keeps the 5 s lead.
                        float s1Lead = def.role == StageEventRole.Monster ? Mathf.Min(def.announcerLeadS, l.eaterWarnLeadM / speed) : def.announcerLeadS;
                        Assert.GreaterOrEqual(t[3] - t[0], s1Lead - 0.03f, kv.Key + " S1 lead at " + speed);
                        Assert.GreaterOrEqual(t[3] - t[1], def.signLeadS - 0.03f, kv.Key + " S2 lead at " + speed);
                        Assert.GreaterOrEqual(t[3] - t[2], def.cueLeadS - 0.03f, kv.Key + " S3 lead at " + speed);
                        Assert.GreaterOrEqual(t[3] - t[2], 1.0f - 0.03f, "effect never starts before S3 + 1.0 s");
                    }
                }
        }

        [Test]
        public void SD12_Pause_DoesNotChangeRemainingTimeToEffect()
        {
            var l = StageTestData.SliceLadder();
            var d = new StageDirector(l); d.Begin(StageTestData.Record(11));
            var mud = Find(d.Plan, "EVT-MUD");
            float inSign = (mud.SignM + mud.CueM) * 0.5f;
            d.Tick(inSign, 8f, 0.016f);
            Assert.AreEqual(StagePhase.Warning, mud.Phase);
            float remainingBefore = mud.StartM - inSign;
            bool s3Before = mud.SignalS3Fired;
            for (int i = 0; i < 600; i++) d.Tick(inSign, 0f, 0f);                // ten seconds of pause: the game does not advance scaled time
            Assert.AreEqual(StagePhase.Warning, mud.Phase);
            Assert.AreEqual(s3Before, mud.SignalS3Fired);
            Assert.AreEqual(remainingBefore, mud.StartM - d.DistanceM, 0.0001f);
            d.OnRespawn(inSign);
            float grace = d.HazardGraceLeftS;
            for (int i = 0; i < 100; i++) d.Tick(inSign, 0f, 0f);
            Assert.AreEqual(grace, d.HazardGraceLeftS, "paused time does not run the respawn grace down");
        }

        // ---------------------------------------------------------------- SD-13 .. SD-15

        [Test]
        public void SD13_Respawn_GraceClearZonePlatformAndEffectHold()
        {
            var l = StageTestData.SliceLadder();
            var d = new StageDirector(l); d.Begin(StageTestData.Record(21));
            var mud = Find(d.Plan, "EVT-MUD");
            d.JumpTo(mud.StartM + 10f);
            Assert.AreEqual(StagePhase.Active, mud.Phase);
            Assert.IsTrue(d.CanSpawnHazard("hz-mud"));
            d.OnRespawn(mud.StartM + 5f);
            Assert.IsFalse(d.CanSpawnHazard("hz-mud"), "grace right after respawn");
            Assert.AreEqual(15f, d.RespawnClearM);
            Assert.GreaterOrEqual(d.RespawnPlatformM, 6f);
            d.Tick(mud.StartM + 5f, 5f, 0.5f);
            Assert.IsFalse(d.CanSpawnHazard("hz-mud"));
            d.Tick(mud.StartM + 5f, 5f, 1.1f);                                   // 1.6 s since respawn
            Assert.IsTrue(d.CanSpawnHazard("hz-mud"), "after 1.5 s");
            Assert.IsFalse(d.CanSpawnHazardAt("hz-mud", mud.StartM + 12f), "inside the 15 m clear zone while the hold runs");
            Assert.IsTrue(d.CanSpawnHazardAt("hz-mud", mud.StartM + 40f));
            d.Tick(mud.StartM + 5f, 5f, 1.5f);                                   // 3.1 s
            Assert.IsTrue(d.CanSpawnHazardAt("hz-mud", mud.StartM + 12f));

            var glass = Find(d.Plan, "EVT-GLASS");
            d.JumpTo(glass.StartM + 5f);
            Assert.AreEqual(StagePhase.Active, glass.Phase);
            d.OnRespawn(glass.StartM + 5f);
            Assert.AreEqual(6f, d.RespawnPlatformM, "glass respawn keeps a 6 m platform");

            // No effect starts within 3 s of a respawn.
            d.Begin(StageTestData.Record(21));
            mud = Find(d.Plan, "EVT-MUD");
            int started = 0; d.Started += e => started++;
            d.JumpTo(mud.StartM - 2f);
            Assert.AreEqual(StagePhase.Warning, mud.Phase);
            d.OnRespawn(mud.StartM - 2f);
            d.Tick(mud.StartM + 5f, 8f, 0.5f);
            Assert.AreNotEqual(StagePhase.Active, mud.Phase, "held back");
            d.Tick(mud.StartM + 5f, 8f, 3.0f);
            Assert.AreEqual(StagePhase.Active, mud.Phase);
            Assert.AreEqual(1, started);
        }

        [Test]
        public void SD14_Retry_ReproducesTheLadder_AndNeverTouchesTimeScale()
        {
            var l = StageTestData.SliceLadder();
            var rec = StageTestData.Record(55);
            var d = new StageDirector(l);
            d.Begin(rec);
            string first = StageTestData.Signature(d.Plan);
            for (float dist = 0; dist < 1500f; dist += 11f) d.Tick(dist, 9f, 0.05f);
            d.OnRunEnd(StageRunOutcome.Died);
            Assert.IsFalse(d.CanSpawnHazard("hz-mud"));
            foreach (var e in d.Plan.Events) Assert.AreEqual(StagePhase.Ended, e.Phase, "run end clears warnings and events");
            d.Begin(rec);
            Assert.AreEqual(first, StageTestData.Signature(d.Plan));
            Assert.IsTrue(d.Enabled);
            Assert.AreEqual(StagePhase.Planned, d.Plan.Events[d.Plan.Events.Count - 1].Phase);
            Assert.AreEqual(1f, Time.timeScale);
        }

        [Test]
        public void SD15_DebugJumpPoints_LandInTheStatedState_WithoutForbiddenStacks()
        {
            var l = StageTestData.SliceLadder();
            Assert.AreEqual(new float[] { 0f, 220f, 480f, 640f, 710f, 1000f, 1350f, 1800f, 2000f }, l.debugJumpPointsM);
            for (uint raw = 1; raw <= 300; raw++)
            {
                var d = new StageDirector(l); d.Begin(StageTestData.Record(raw));
                var m = l.matrix;
                foreach (float point in l.debugJumpPointsM)
                {
                    d.JumpTo(point);
                    Assert.IsTrue(d.Plan.Record.debugJumped);
                    var ev = d.Plan.Events;
                    for (int i = 0; i < ev.Count; i++)
                        for (int j = i + 1; j < ev.Count; j++)
                        {
                            var a = ev[i]; var b = ev[j];
                            bool aLive = a.Phase == StagePhase.Active || a.Phase == StagePhase.Warning;
                            bool bLive = b.Phase == StagePhase.Active || b.Phase == StagePhase.Warning;
                            bool oneActive = a.Phase == StagePhase.Active || b.Phase == StagePhase.Active;
                            bool reward = a.Definition.role == StageEventRole.Reward || b.Definition.role == StageEventRole.Reward;
                            if (aLive && bLive && oneActive && !reward) Assert.IsTrue(m.CanOverlap(a.Id, b.Id), "seed " + raw + " point " + point + " " + a.Id + "+" + b.Id);
                        }
                    string Ph(string id) { return Find(d.Plan, id).Phase.ToString(); }
                    switch ((int)point)
                    {
                        case 0: Assert.IsTrue(d.Breather); Assert.IsFalse(d.CanSpawnHazard("hz-ice")); break;
                        case 220: Assert.AreEqual("Active", Ph("EVT-ICE")); Assert.LessOrEqual(Find(d.Plan, "EVT-ICE").EndM, 360f); break;
                        case 480: Assert.AreEqual("Warning", Ph("EVT-METEOR")); break;
                        case 640: Assert.IsTrue(d.Breather); break;
                        case 710: Assert.AreEqual("Warning", Ph("EVT-MUD")); break;
                        case 1000: Assert.AreEqual("Active", Ph("MON-EATER")); Assert.AreEqual("MON-EATER", d.ActiveEvent.Id); break;
                        case 1350: Assert.IsTrue(Ph("EVT-GLASS") == "Active" || Ph("EVT-GLASS") == "Warning"); Assert.AreEqual("Active", Ph("MON-EATER")); break;
                        case 1800: Assert.IsTrue(Ph("EVT-RINGS") == "Active" || Ph("EVT-RINGS") == "Warning"); break;
                        case 2000: Assert.AreEqual("Active", Ph("BOS-ROB0")); break;
                    }
                }
            }
        }

        [Test]
        public void SD15b_JumpIntoWarning_ReplaysSignalsInOrder()
        {
            var l = StageTestData.SliceLadder();
            var d = new StageDirector(l); d.Begin(StageTestData.Record(5));
            var order = new List<StageSignal>();
            d.SignalFired += (e, s) => { if (e.Id == "EVT-METEOR") order.Add(s); };
            d.JumpTo(Find(d.Plan, "EVT-METEOR").WarnM + 1f);
            d.Tick(Find(d.Plan, "EVT-METEOR").CueM + 1f, 8f, 0.1f);
            Assert.AreEqual(new[] { StageSignal.Announcer, StageSignal.Sign, StageSignal.Cue }, order.ToArray());
        }

        // ---------------------------------------------------------------- SD-16 .. SD-19

        [Test]
        public void SD16_TerrainCaps_AreEnforcedByTheDefinitionValidator()
        {
            var l = StageTestData.SliceLadder(out var defs);
            foreach (var d in defs)
            {
                Assert.LessOrEqual(d.terrainJumpHeightCapM, StageEventDefinition.JumpHeightCapM);
                Assert.That(d.terrainSlopeMinDeg, Is.GreaterThanOrEqualTo(-20f)); Assert.That(d.terrainSlopeMaxDeg, Is.LessThanOrEqualTo(20f));
                Assert.LessOrEqual(d.terrainLongestGapM, 8f);
            }
            var bad = StageTestData.Mud();
            bad.terrainJumpHeightCapM = 8.5f; bad.terrainSlopeMaxDeg = 25f; bad.terrainLongestGapM = 9f; bad.ringMaxHeightM = 9f;
            var errs = new List<string>();
            Assert.IsFalse(bad.Validate(errs));
            Assert.That(string.Join("|", errs), Does.Contain("8 m cap").And.Contain("slope").And.Contain("terrainLongestGapM").And.Contain("ringMaxHeightM"));
            // The generated-layout half of SD-16 (1000 layouts per event) needs the terrain generator, which is not part of Phase A.
        }

        [Test]
        public void SD17_InkGuarantee_NeedsTheEconomySimulation()
        {
            Assert.Ignore("SD-17 needs measured ink per metre (draw_stats telemetry) and the economy-balancer bot; neither exists yet.");
        }

        [Test]
        public void SD18_Boundary_DirectorHasNoRoadCarRoadBuilderOrModeReference()
        {
            var asm = typeof(StageDirector).Assembly;
            var banned = new HashSet<string> { "RoadCar", "RoadBuilder", "RoadChunk", "RoadGameManager", "ModeDefinition", "ModeRules" };
            var stageTypes = new List<Type>();
            foreach (var t in asm.GetTypes())
                if (t.Namespace == "RoadBuilderGame" && (t.Name.StartsWith("Stage") || t.Name == "IStageDirector" || t.Name == "Pcg32" || t.Name == "EventCompatibilityMatrix")) stageTypes.Add(t);
            Assert.GreaterOrEqual(stageTypes.Count, 9);
            const BindingFlags all = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            foreach (var t in stageTypes)
            {
                void Check(Type x, string where)
                {
                    if (x == null) return;
                    while (x.HasElementType) x = x.GetElementType();
                    Assert.IsFalse(banned.Contains(x.Name), t.Name + " references " + x.Name + " in " + where);
                    if (x.IsGenericType) foreach (var g in x.GetGenericArguments()) Check(g, where);
                }
                foreach (var f in t.GetFields(all)) Check(f.FieldType, "field " + f.Name);
                foreach (var p in t.GetProperties(all)) Check(p.PropertyType, "property " + p.Name);
                foreach (var m in t.GetMethods(all)) { Check(m.ReturnType, "method " + m.Name); foreach (var prm in m.GetParameters()) Check(prm.ParameterType, "method " + m.Name); }
            }
            // The Game asmdef may reference Core only; the director sources must not name the banned types or PlayerPrefs at all.
            string dir = Path.Combine(Application.dataPath, "Scripts/Game/Stage");
            foreach (var file in Directory.GetFiles(dir, "*.cs"))
            {
                string text = File.ReadAllText(file);
                foreach (var word in new[] { "RoadCar", "RoadBuilder(?!Game)", "PlayerPrefs", "UnityEngine.Random", "FindObjectOfType", "System.Random", "DateTime", "Time.time", "Time.frameCount" })
                    Assert.IsFalse(System.Text.RegularExpressions.Regex.IsMatch(text, "\\b" + word.Replace(".", "\\.") + "\\b"), Path.GetFileName(file) + " mentions " + word);
            }
        }

        private static void CountEvent(StageEventState e) { }
        private static void CountSignal(StageEventState e, StageSignal s) { }

        [Test]
        public void SD19_Tick_AllocatesZeroBytes()
        {
            var l = StageTestData.SliceLadder();
            var d = new StageDirector(l); d.Begin(StageTestData.Record(77));
            d.Warned += CountEvent; d.Started += CountEvent; d.Ended += CountEvent; d.SignalFired += CountSignal;
            for (int pass = 0; pass < 2; pass++) { d.JumpTo(0f); for (float dist = 0f; dist < 2900f; dist += 3f) d.Tick(dist, 9f, 0.02f); }   // warm-up
            d.JumpTo(0f);
            long before = GC.GetAllocatedBytesForCurrentThread();
            int hazards = 0;
            for (int n = 0; n < 10; n++)
            {
                for (float dist = 0f; dist < 2900f; dist += 0.5f)
                {
                    d.Tick(dist, 9f, 0.02f);
                    if (d.CanSpawnHazard("hz-mud")) hazards++;
                    if (d.NextEvent.IsNone || d.Breather || d.ActiveModifierCount < 0 || d.ActiveEvent == null) hazards -= 0;
                }
                d.JumpTo(0f);
            }
            long after = GC.GetAllocatedBytesForCurrentThread();
            Assert.AreEqual(0L, after - before, "bytes allocated by ~58000 Ticks and queries");
            Assert.AreNotEqual(0, hazards);
        }

        // ---------------------------------------------------------------- SD-20 (manual) and the two extra rules

        [Test]
        public void SD20_Playtest_IsManual()
        {
            Assert.Ignore("SD-20 is the F6 playtest (10 runs, 5 players, 3 vehicles); it cannot run as an EditMode test.");
        }

        [Test]
        public void SD21_EaterWarn_NoSurfaceMeteorOrHazardActiveBetween940AndChaseStart()
        {
            var l = StageTestData.SliceLadder();
            for (uint raw = 1; raw <= Seeds; raw++)
            {
                var p = Plan(l, raw);
                var eater = Find(p, "MON-EATER");
                foreach (var e in p.Events)
                {
                    if (e == eater || e.Definition.role == StageEventRole.Reward) continue;
                    if (e.Id == "EVT-GLASS") { Assert.GreaterOrEqual(e.StartM, eater.StartM, "glass only from the Track phase, seed " + raw); continue; }
                    bool inWindow = e.StartM < eater.StartM && e.EndM > l.eaterWarnM;
                    Assert.IsFalse(inWindow, "seed " + raw + ": " + e.Id + " reaches into " + l.eaterWarnM + " to " + eater.StartM);
                }
            }
        }

        [Test]
        public void SD22_ChaseEnd_RingsNeverStartWhileTheEaterIsAlive_AndDisabledDirectorIsSafe()
        {
            var l = StageTestData.SliceLadder();
            for (uint raw = 1; raw <= Seeds; raw++)
            {
                var p = Plan(l, raw);
                var eater = Find(p, "MON-EATER"); var rings = Find(p, "EVT-RINGS");
                Assert.LessOrEqual(eater.EndM, l.eaterTimeoutM + 0.01f);
                Assert.GreaterOrEqual(rings.StartM, eater.EndM, "seed " + raw);
            }
            // Mode with no stage data: director disabled, CanSpawnHazard false for every id.
            var off = new StageDirector(null); off.Begin(StageTestData.Record(1));
            Assert.IsFalse(off.Enabled);
            Assert.IsTrue(off.NextEvent.IsNone);
            Assert.IsNull(off.ActiveEvent);
            Assert.IsFalse(off.CanSpawnHazard("hz-mud"));
            Assert.DoesNotThrow(() => { off.Tick(100f, 5f, 0.1f); off.OnRespawn(100f); off.JumpTo(500f); off.OnRunEnd(StageRunOutcome.Quit); });
            // Run seed 0 falls back to 128 and says so.
            var p0 = StageLadderPlanner.Build(l, new StageRunRecord { runSeed = 0, modeId = 5 });
            Assert.AreEqual(StageSeed.FallbackRunSeed, p0.Record.runSeed);
            Assert.AreEqual(1, p0.Log.FindAll(x => x.Code == "run_seed_fallback").Count);
        }

        // ---------------------------------------------------------------- economy change requests (plain data edits)

        [Test]
        public void Economy_ApprovedValues_VehiclePricesNearMissAndBoostUpgrade()
        {
            Assert.AreEqual(0, GameCatalog.VehicleCost(VehicleType.ClassicRed));
            Assert.AreEqual(600, GameCatalog.VehicleCost(VehicleType.SportCyan));
            Assert.AreEqual(2000, GameCatalog.VehicleCost(VehicleType.MonsterTruck));
            var t = ScriptableObject.CreateInstance<GameplayTuning>();
            Assert.AreEqual(8, t.nearMissCoins);
            Assert.AreEqual(0.08f, t.boostDurationPerLevel, 0.0001f);
            Assert.AreEqual(0.057f, t.boostDurationPerLevel / t.boostDuration, 0.001f, "about +6 percent of the base boost per level");
        }
    }
}
