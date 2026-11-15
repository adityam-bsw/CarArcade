using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using RoadBuilderGame.UI;

namespace RoadBuilderGame.Tests
{
    /// <summary>
    /// Phase B1 tests: the real slice assets, the surface-zone formulas, the run controller and a simulated run in which the events fire in order,
    /// the warnings reach the model in order, and the old game is untouched when the switch is off. No scene.
    /// </summary>
    public class StageWiringTests
    {
        private const string Dir = "Assets/Resources/Stage";

        private static StageLadderDefinition RealLadder()
        {
            var l = AssetDatabase.LoadAssetAtPath<StageLadderDefinition>(Dir + "/StageLadder.asset");
            Assert.IsNotNull(l, "Run Road Builder > Stage > Build Slice Assets");
            return l;
        }

        private static SurfaceRuleSO Rule(string name) { var r = AssetDatabase.LoadAssetAtPath<SurfaceRuleSO>(Dir + "/" + name + ".asset"); Assert.IsNotNull(r, name); return r; }

        private static StageRunRecord Rec(uint raw) { return new StageRunRecord { runSeed = StageSeed.MakeRunSeed(raw), modeId = StageRunController.ModeIdCountryside, layoutVersion = 1 }; }

        // ---------------------------------------------------------------- the real assets

        [Test]
        public void W01_SliceAssets_ExistAndValidate()
        {
            var errors = new List<string>();
            var ladder = RealLadder();
            Assert.IsTrue(ladder.Validate(errors), string.Join("\n", errors));
            Assert.AreEqual(7, ladder.slots.Length);
            Assert.AreEqual(21, ladder.matrix.pairs.Length, "the 21 pairs of stage-events.md section 5");
            foreach (var g in AssetDatabase.FindAssets("t:StageEventDefinition", new[] { Dir }))
            {
                var e = AssetDatabase.LoadAssetAtPath<StageEventDefinition>(AssetDatabase.GUIDToAssetPath(g));
                Assert.IsTrue(e.Validate(errors), string.Join("\n", errors));
            }
            foreach (var g in AssetDatabase.FindAssets("t:SurfaceRuleSO", new[] { Dir }))
                Assert.IsTrue(AssetDatabase.LoadAssetAtPath<SurfaceRuleSO>(AssetDatabase.GUIDToAssetPath(g)).Validate(errors), string.Join("\n", errors));
        }

        [Test]
        public void W02_RealLadder_2000Seeds_AllSevenPlaced_NoRejections_InSpecWindows()
        {
            var l = RealLadder();
            for (uint raw = 1; raw <= 2000; raw++)
            {
                var p = StageLadderPlanner.Build(l, Rec(raw));
                Assert.AreEqual(0, p.Log.Count, "seed " + raw + ": " + string.Join(";", p.Log));
                Assert.AreEqual(7, p.Events.Count);
                string[] order = { "EVT-ICE", "EVT-METEOR", "EVT-MUD", "MON-EATER", "EVT-GLASS", "EVT-RINGS", "BOS-ROB0" };
                for (int i = 0; i < order.Length; i++) Assert.AreEqual(order[i], p.Events[i].Id);
                Assert.That(p.Events[0].StartM, Is.InRange(190f, 220f));
                Assert.That(p.Events[1].StartM, Is.InRange(490f, 520f));
                Assert.That(p.Events[2].StartM, Is.InRange(720f, 750f));
                Assert.AreEqual(1000f, p.Events[3].StartM);
                Assert.That(p.Events[4].StartM, Is.InRange(1310f, 1390f));
                Assert.That(p.Events[5].StartM, Is.InRange(1790f, 1810f));
                Assert.AreEqual(2000f, p.Events[6].StartM);
            }
        }

        [Test]
        public void W03_CueIdsAndTokens_ResolveToTheRegistryAndTheTheme()
        {
            foreach (var g in AssetDatabase.FindAssets("t:StageEventDefinition", new[] { Dir }))
            {
                var e = AssetDatabase.LoadAssetAtPath<StageEventDefinition>(AssetDatabase.GUIDToAssetPath(g));
                Assert.IsTrue(StageCues.Registry.Contains(e.announcerCueId), e.id + " announcer " + e.announcerCueId);
                Assert.IsTrue(StageCues.Registry.Contains(e.warnSfxId), e.id + " sign " + e.warnSfxId);
                Assert.IsTrue(StageCues.Registry.Contains(e.cueSfxId), e.id + " cue " + e.cueSfxId);
                Assert.IsTrue(Enum.TryParse<ColorToken>(e.tintColorToken, out _), e.id + " token " + e.tintColorToken);
                Assert.IsNotEmpty(StageCues.BannerText(e.bannerKey), e.id + " banner");
                Assert.AreEqual(5f, e.announcerLeadS); Assert.AreEqual(3f, e.signLeadS); Assert.AreEqual(1f, e.cueLeadS);
            }
            var theme = AssetDatabase.LoadAssetAtPath<UITheme>("Assets/Resources/UI/UITheme_Default.asset");
            foreach (var t in new[] { ColorToken.ZoneIce, ColorToken.ZoneMud, ColorToken.ZoneGlass, ColorToken.ZoneMeteor, ColorToken.ZoneRings })
                Assert.Greater(theme.GetColor(t).a, 0.5f, t + " is not set in the theme");
        }

        [Test]
        public void W04_SurfaceRules_MirrorTheCatalogueNumbers()
        {
            var cat = AssetDatabase.LoadAssetAtPath<AssetCatalog>("Assets/Resources/Catalog/AssetCatalog.asset");
            foreach (var n in new[] { "SRF-ICE", "SRF-MUD", "SRF-GLS" })
            {
                var r = Rule(n);
                var e = cat.Find(r.surfaceId);
                Assert.IsNotNull(e, r.surfaceId);
                Assert.AreEqual(e.friction, r.friction, 0.001f, n + " friction");
                Assert.AreEqual(e.speedFactor, r.speedFactor, 0.001f, n + " speed");
                Assert.AreEqual(e.inkFactor, r.inkFactor, 0.001f, n + " ink");
                Assert.AreEqual(e.coinFactor, r.coinFactor, 0.001f, n + " coin");
            }
        }

        [Test]
        public void W05_GameplayTuning_StageSwitch_DefaultsAndGate()
        {
            var t = ScriptableObject.CreateInstance<GameplayTuning>();
            Assert.AreEqual(StageEventsSwitch.CountrysideOnly, t.stageEvents);
            Assert.IsFalse(t.stageHaptics, "haptics default off");
            Assert.AreEqual(5, t.boostCoinCost, "boost price unchanged");
            Assert.IsTrue(StageRunController.ShouldRun(StageEventsSwitch.CountrysideOnly, true));
            Assert.IsFalse(StageRunController.ShouldRun(StageEventsSwitch.CountrysideOnly, false));
            Assert.IsTrue(StageRunController.ShouldRun(StageEventsSwitch.Always, false));
            Assert.IsFalse(StageRunController.ShouldRun(StageEventsSwitch.Off, true));
            // mud drag cap = 2.0 x multiplier: Red 2.0, Cyan 2.5, Monster 1.2
            var mud = Rule("SRF-MUD");
            Assert.AreEqual(2.0f, SurfaceZoneModel.MudDrag(mud, 99f, t.MudDragMul((int)VehicleType.ClassicRed)), 0.001f);
            Assert.AreEqual(2.5f, SurfaceZoneModel.MudDrag(mud, 99f, t.MudDragMul((int)VehicleType.SportCyan)), 0.001f);
            Assert.AreEqual(1.2f, SurfaceZoneModel.MudDrag(mud, 99f, t.MudDragMul((int)VehicleType.MonsterTruck)), 0.001f);
            Assert.AreEqual(1f, t.MudDragMul(-1), "catalogue vehicles use 1");
        }

        [Test]
        public void W06_RunRecord_SurvivesTheSaveFormat_AndSeedIsNeverZero()
        {
            var p = new PlayerProfile { lastStageRun = StageRunController.NewRecord(0UL, 1) };
            Assert.AreNotEqual(0u, p.lastStageRun.runSeed);
            Assert.AreEqual(0u, p.lastStageRun.runSeed & 127u, "low 7 bits free (raw << 7)");
            p.lastStageRun = StageRunController.NewRecord(0x1234567890ABCDEFUL, 1);
            var back = JsonUtility.FromJson<PlayerProfile>(JsonUtility.ToJson(p));
            Assert.AreEqual(p.lastStageRun.runSeed, back.lastStageRun.runSeed);
            Assert.AreEqual(StageRunController.ModeIdCountryside, back.lastStageRun.modeId);
            Assert.AreEqual(1, back.lastStageRun.layoutVersion);
            var old = JsonUtility.FromJson<PlayerProfile>("{\"version\":1,\"coins\":10}");
            Assert.AreEqual(0u, old.lastStageRun.runSeed, "an old save loads with an empty record");
        }

        // ---------------------------------------------------------------- formulas

        [Test]
        public void W10_Mud_Formulas()
        {
            var r = Rule("SRF-MUD");
            float baseT = 11.5f * 0.85f;                                         // 9.775
            Assert.AreEqual(baseT, SurfaceZoneModel.MudSpeed(r, baseT, 0f, 1f, 0f, false), 0.001f, "no drag at the zone entry");
            Assert.AreEqual(baseT - 1f, SurfaceZoneModel.MudSpeed(r, baseT, 1f, 1f, 0f, false), 0.001f, "1.0 m/s per second");
            Assert.AreEqual(baseT - 2f, SurfaceZoneModel.MudSpeed(r, baseT, 2f, 1f, 0f, false), 0.001f, "capped at 2.0 after 2 s");
            Assert.AreEqual(baseT - 2f, SurfaceZoneModel.MudSpeed(r, baseT, 60f, 1f, 0f, false), 0.001f, "never more than the cap");
            Assert.AreEqual(baseT - 1.2f, SurfaceZoneModel.MudSpeed(r, baseT, 60f, 0.6f, 0f, false), 0.001f, "Monster cap 1.2");
            Assert.AreEqual(3f, SurfaceZoneModel.MudSpeed(r, 4f, 60f, 1.25f, 0f, false), 0.001f, "the floor: mud alone never stops the car");
            Assert.AreEqual(3f, SurfaceZoneModel.MudSpeed(r, baseT, 0f, 1f, 19f, false), 0.001f, "a slope over 18 degrees stalls to the floor");
            Assert.AreEqual(baseT, SurfaceZoneModel.MudSpeed(r, baseT, 60f, 1f, 25f, true), 0.001f, "boost ignores drag and stall");
            Assert.AreEqual(baseT - 2f, SurfaceZoneModel.MudSpeed(r, baseT, 60f, 1f, 18f, false), 0.001f, "18 degrees exactly does not stall");
        }

        [Test]
        public void W11_Ice_Slip_ThresholdRampAndRearm()
        {
            var r = Rule("SRF-ICE");
            Assert.AreEqual(0f, SurfaceZoneModel.SlipSeconds(r, 25f), "at the threshold no slip");
            Assert.AreEqual(0f, SurfaceZoneModel.SlipSeconds(r, 10f));
            Assert.AreEqual(0.3f, SurfaceZoneModel.SlipSeconds(r, 35f), 0.001f, "0.6 * (35 - 25) / 20");
            Assert.AreEqual(0.6f, SurfaceZoneModel.SlipSeconds(r, 90f), 0.001f, "never more than 0.6 s");
            var z = new SurfaceZoneModel(); z.Enter(r);
            z.Tick(0.02f, 10f, 1f, 0f, 45f, false);
            Assert.IsTrue(z.DriveCut, "a 45 degree bend cuts the drive");
            for (int i = 0; i < 40; i++) z.Tick(0.02f, 10f, 1f, 0f, 45f, false);      // 0.8 s later the slip is over, and the re-arm holds a second slip off
            Assert.IsFalse(z.DriveCut, "one long bend is one slip, then the car drives again");
            Assert.AreEqual(10f, z.Tick(0.02f, 10f, 1f, 0f, 0f, false), "ice leaves the target speed alone (the 1.15 factor is applied by the manager)");
        }

        [Test]
        public void W12_Zones_AllocateNothing()
        {
            var z = new SurfaceZoneModel(); z.Enter(Rule("SRF-MUD"));
            z.Tick(0.02f, 9f, 1f, 5f, 3f, false);
            long before = GC.GetAllocatedBytesForCurrentThread();
            float sum = 0f;
            for (int i = 0; i < 20000; i++) sum += z.Tick(0.02f, 9f, 1f, 5f, 3f, i % 7 == 0);
            Assert.AreEqual(0L, GC.GetAllocatedBytesForCurrentThread() - before);
            Assert.Greater(sum, 0f);
        }

        // ---------------------------------------------------------------- a simulated run

        private sealed class Probe : IStageEventBehaviour
        {
            public readonly List<string> Log = new List<string>();
            public readonly string Id;
            public Probe(string id) { Id = id; }
            public void OnStart(StageEventState s) { Log.Add("start " + s.Id); }
            public void OnEnd(StageEventState s) { Log.Add("end " + s.Id); }
            public void OnRespawn(StageEventState s, float d) { Log.Add("respawn " + s.Id); }
        }

        private static float Speed(float d, StagePace pace) { return pace.SpeedAt(d); }

        private sealed class Sim
        {
            public StageRunController Run;
            public readonly List<string> Events = new List<string>();
            public readonly List<float> At = new List<float>();
            public readonly List<float> Time = new List<float>();
            public float T;
            public float Distance;
            public Probe P;
            public Sim(StageLadderDefinition l, uint raw, bool register = true)
            {
                Run = new StageRunController(l);
                P = new Probe("all");
                if (register) foreach (var id in new[] { "EVT-ICE", "EVT-METEOR", "EVT-MUD", "EVT-GLASS", "EVT-RINGS" }) Run.Register(id, P);
                Run.Signal += (e, s) => { Events.Add(e.Id + ":" + s); At.Add(Distance); Time.Add(T); };
                Run.EventStarted += e => { Events.Add(e.Id + ":Start"); At.Add(Distance); Time.Add(T); };
                Run.EventEnded += e => { Events.Add(e.Id + ":End"); At.Add(Distance); Time.Add(T); };
                Run.Begin(Rec(raw));
            }
            public void Drive(float toM, float dt = 0.02f, float boostBonus = 0f)
            {
                var pace = new StagePace(4.2f, 0.02f, 11.5f);
                while (Distance < toM)
                {
                    float v = Speed(Distance, pace) + boostBonus;
                    Distance += v * dt; T += dt;
                    Run.Tick(Distance, v, dt);
                }
            }
        }

        [Test]
        public void W20_SimulatedRun_EventsFireInOrder_S1S2S3BeforeStart_ThenEnd()
        {
            var sim = new Sim(RealLadder(), 77u);
            sim.Drive(2900f);
            var plan = sim.Run.Director.Plan;
            string[] zone = { "EVT-ICE", "EVT-METEOR", "EVT-MUD", "EVT-GLASS", "EVT-RINGS" };
            // global order of the registered events: each one as Announcer, Sign, Cue, Start, End
            var expected = new List<string>();
            foreach (var id in zone) expected.AddRange(new[] { id + ":Announcer", id + ":Sign", id + ":Cue", id + ":Start", id + ":End" });
            // glass starts inside the chase but the chase (MON-EATER) has no behaviour, so the order is the plan order of the five
            var seen = new List<string>(sim.Events);
            Assert.AreEqual(string.Join(",", expected), string.Join(",", seen));
            // each start lands on the planned distance, and the three signals come at least 5 / 3 / 1 s of scaled time earlier
            for (int i = 0; i < zone.Length; i++)
            {
                var st = plan.Events.Find(e => e.Id == zone[i]);
                float tStart = sim.Time[i * 5 + 3], tS1 = sim.Time[i * 5], tS2 = sim.Time[i * 5 + 1], tS3 = sim.Time[i * 5 + 2];
                Assert.GreaterOrEqual(tStart - tS1, 5f - 0.05f, zone[i] + " S1 lead");
                Assert.GreaterOrEqual(tStart - tS2, 3f - 0.05f, zone[i] + " S2 lead");
                Assert.GreaterOrEqual(tStart - tS3, 1f - 0.05f, zone[i] + " S3 lead");
                Assert.AreEqual(st.StartM, sim.At[i * 5 + 3], 0.6f, zone[i] + " starts at its planned distance");
                Assert.AreEqual(st.EndM, sim.At[i * 5 + 4], 0.6f, zone[i] + " ends at its planned distance");
            }
            // the schedule-only anchors never speak
            foreach (var s in seen) { Assert.IsFalse(s.StartsWith("MON-EATER") || s.StartsWith("BOS-ROB0"), s); }
        }

        [Test]
        public void W21_Warnings_FullLeadWhenBoosting_AndWhenCrawling()
        {
            foreach (float boost in new[] { 0f, 4.5f })
            {
                var sim = new Sim(RealLadder(), 5u);
                sim.Drive(2900f, 0.02f, boost);
                for (int i = 0; i < 5; i++)
                {
                    Assert.GreaterOrEqual(sim.Time[i * 5 + 3] - sim.Time[i * 5 + 1], 3f - 0.05f, "boost " + boost + " S2 lead of event " + i);
                    Assert.GreaterOrEqual(sim.Time[i * 5 + 3] - sim.Time[i * 5 + 2], 1f - 0.05f, "boost " + boost + " S3 lead of event " + i);
                }
            }
        }

        private sealed class Null : IStageEventBehaviour
        {
            public int Starts, Ends;
            public void OnStart(StageEventState s) { Starts++; }
            public void OnEnd(StageEventState s) { Ends++; }
            public void OnRespawn(StageEventState s, float d) { }
        }

        [Test]
        public void W22_ZeroAllocation_Tick_WithBehavioursAndPresenterAttached()
        {
            var run = new StageRunController(RealLadder());
            var nb = new Null();
            foreach (var id in new[] { "EVT-ICE", "EVT-METEOR", "EVT-MUD", "EVT-GLASS", "EVT-RINGS" }) run.Register(id, nb);
            var pres = new StageWarningPresenter(run, null, null);
            var pace = new StagePace(4.2f, 0.02f, 11.5f);
            for (int pass = 0; pass < 2; pass++)       // warm-up: first calls compile and cache
            {
                run.Begin(Rec(9u)); pres.Reset();
                float w = 0f; while (w < 2900f) { float v = pace.SpeedAt(w); w += v * 0.02f; run.Tick(w, v, 0.02f); pres.Tick(w, 0.02f); }
            }
            run.Begin(Rec(9u)); pres.Reset();
            long before = GC.GetAllocatedBytesForCurrentThread();
            float d = 0f; int hz = 0;
            while (d < 2900f) { float v = pace.SpeedAt(d); d += v * 0.02f; run.Tick(d, v, 0.02f); pres.Tick(d, 0.02f); if (run.CanSpawnHazard("hz-meteor")) hz++; }
            long used = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.AreEqual(0L, used, "bytes allocated over a whole simulated run (all five events, presenter, hazard queries)");
            Assert.Greater(hz, 0);
            Assert.AreEqual(15, nb.Starts, "five events in each of the three runs");
        }

        [Test]
        public void W23_SwitchOff_NothingRuns_AndHazardsAreRefused()
        {
            var run = new StageRunController(null);
            Assert.IsFalse(run.HasData);
            run.Begin(Rec(1u));
            Assert.IsFalse(run.Active);
            run.Tick(300f, 8f, 0.02f);
            Assert.IsFalse(run.CanSpawnHazard("hz-meteor"));
            run.OnRespawn(100f); run.OnPause(true); run.OnPause(false); run.End(StageRunOutcome.Died);   // all safe no-ops
        }

        [Test]
        public void W24_Retry_SameSeedSameLadder_NewRunNewSeed()
        {
            var a = StageRunController.NewRecord(111UL, 1);
            var b = StageRunController.NewRecord(222UL, 1);
            Assert.AreNotEqual(a.runSeed, b.runSeed);
            var l = RealLadder();
            string s1 = StageTestData.Signature(StageLadderPlanner.Build(l, a));
            Assert.AreEqual(s1, StageTestData.Signature(StageLadderPlanner.Build(l, a)), "retry reuses the record");
            Assert.AreNotEqual(s1, StageTestData.Signature(StageLadderPlanner.Build(l, b)));
            var sim = new Sim(l, 7u);
            sim.Drive(300f);
            sim.Run.End(StageRunOutcome.Died);
            Assert.IsFalse(sim.Run.Active);
            var first = new List<string>(sim.Events);
            sim.Events.Clear(); sim.Distance = 0f; sim.T = 0f;
            sim.Run.Begin(Rec(7u));
            sim.Drive(300f);
            // events before the death point repeat exactly (End is raised on run end only for active events)
            Assert.AreEqual(first.FindAll(x => x.Contains("Announcer") || x.Contains("Start")).Count, sim.Events.FindAll(x => x.Contains("Announcer") || x.Contains("Start")).Count);
        }

        [Test]
        public void W25_Respawn_GraceBlocksHazards_ZonesRemain_PauseResumeGrace()
        {
            var sim = new Sim(RealLadder(), 3u);
            var start = sim.Run.Director.Plan.Events.Find(e => e.Id == "EVT-METEOR").StartM;
            sim.Drive(start + 10f);
            Assert.IsTrue(sim.Run.CanSpawnHazard("hz-meteor"), "inside the window, past the grace");
            sim.Run.OnRespawn(sim.Distance);
            Assert.IsFalse(sim.Run.CanSpawnHazard("hz-meteor"), "1.5 s after a respawn");
            Assert.IsTrue(sim.P.Log.Contains("respawn EVT-METEOR"), "the active behaviour is told");
            sim.Drive(sim.Distance + 20f, 0.05f);
            Assert.IsTrue(sim.Run.CanSpawnHazard("hz-meteor"));
            sim.Run.OnPause(true); sim.Run.OnPause(false);
            Assert.IsFalse(sim.Run.CanSpawnHazard("hz-meteor"), "0.5 s grace after the app resumes");
            sim.Run.Tick(sim.Distance, 8f, 0.6f);
            Assert.IsTrue(sim.Run.CanSpawnHazard("hz-meteor"));
        }

        [Test]
        public void W26_Hazards_OnlyInsideTheirOwnWindow_NeverInABreather()
        {
            var sim = new Sim(RealLadder(), 11u);
            var plan = sim.Run.Director.Plan;
            var pace = new StagePace(4.2f, 0.02f, 11.5f);
            float d = 0f; int inWindow = 0;
            while (d < 2900f)
            {
                d += pace.SpeedAt(d) * 0.02f; sim.Run.Tick(d, 8f, 0.02f);
                bool m = sim.Run.CanSpawnHazard("hz-meteor");
                var ev = plan.Events.Find(e => e.Id == "EVT-METEOR");
                if (m) { inWindow++; Assert.That(d, Is.InRange(ev.StartM - 0.5f, ev.EndM + 0.5f), "meteor allowed outside its window at " + d); }
                if (d > 1.0f && d < 190f) Assert.IsFalse(sim.Run.CanSpawnHazard("hz-ice") || m, "no hazard in the warm-up");
            }
            Assert.Greater(inWindow, 100);
        }

        [Test]
        public void W27_DebugJump_LandsInWarning_ReplaysSignalsInOrder_AndClearsTheZone()
        {
            var sim = new Sim(RealLadder(), 21u);
            sim.Drive(1f);
            sim.Events.Clear();
            sim.Distance = 480f; sim.Run.JumpTo(480f);
            sim.Run.Tick(sim.Distance, 11.5f, 0.02f);
            Assert.AreEqual("EVT-METEOR:Announcer", sim.Events[0]);
            Assert.IsTrue(sim.Events.Contains("EVT-METEOR:Sign"));
            Assert.IsTrue(sim.Run.Director.Record.debugJumped);
            // into the ice window, then away: the zone behaviour is ended
            sim.Events.Clear(); sim.P.Log.Clear();
            var ice = sim.Run.Director.Plan.Events.Find(e => e.Id == "EVT-ICE");
            sim.Distance = ice.StartM + 5f; sim.Run.JumpTo(sim.Distance);
            Assert.Contains("start EVT-ICE", sim.P.Log);
            sim.Distance = 640f; sim.Run.JumpTo(640f);
            Assert.Contains("end EVT-ICE", sim.P.Log);
            Assert.IsTrue(sim.Run.Director.Breather);
        }

        // ---------------------------------------------------------------- presenter

        [Test]
        public void W30_Presenter_WritesBannerSignFlash_InOrder_AndSpacesCatchUp()
        {
            var model = GameServices.Model;
            model.StageBannerId = model.StageFlashId = 0;
            var sim = new Sim(RealLadder(), 31u, register: false);
            sim.Run.Register("EVT-METEOR", new Probe("m"));
            var pres = new StageWarningPresenter(sim.Run, null, ScriptableObject.CreateInstance<GameplayTuning>());
            pres.Reset();
            var ev = sim.Run.Director.Plan.Events.Find(e => e.Id == "EVT-METEOR");
            // normal approach
            var pace = new StagePace(4.2f, 0.02f, 11.5f);
            float d = ev.WarnM - 1f; bool bannerSeen = false, signSeen = false, flashSeen = false; int order = 0; int bannerAt = -1, signAt = -1, flashAt = -1;
            sim.Run.Director.JumpTo(d);          // land just before the first signal (debug builds only; the editor has it)
            while (d < ev.StartM + 1f)
            {
                d += 9f * 0.02f; sim.Run.Tick(d, 9f, 0.02f); pres.Tick(d, 0.02f);
                if (!bannerSeen && model.StageBannerId > 0) { bannerSeen = true; bannerAt = order++; }
                if (!signSeen && model.StageSignOn) { signSeen = true; signAt = order++; }
                if (!flashSeen && model.StageFlashId > 0) { flashSeen = true; flashAt = order++; }
            }
            Assert.IsTrue(bannerSeen && signSeen && flashSeen);
            Assert.Less(bannerAt, signAt); Assert.Less(signAt, flashAt);
            Assert.AreEqual("METEORS!", model.StageBanner);
            Assert.AreEqual("ZoneMeteor", model.StageBannerTint);
            Assert.AreEqual("ROCKS", model.StageSignName);
            Assert.IsTrue(model.StageSignActive, "the sign counts metres left once the effect runs");
            // catch-up: land between S1 and S2 with all three due at once; the presenter spaces them by signalMinGapS (0.6 s)
            pres.Reset();
            model.StageBannerId = model.StageFlashId = 0; model.StageSignOn = false;
            sim.Run.Begin(Rec(31u));
            float landing = ev.CueM + 0.5f;
            sim.Run.JumpTo(landing);
            float t = 0f; float signT = -1f, flashT = -1f; d = landing;
            for (int i = 0; i < 200; i++)
            {
                sim.Run.Tick(d, 9f, 0.02f); pres.Tick(d, 0.02f); t += 0.02f;
                if (signT < 0f && model.StageSignOn) signT = t;
                if (flashT < 0f && model.StageFlashId > 0) flashT = t;
            }
            Assert.Greater(signT, 0f); Assert.Greater(flashT, 0f);
            Assert.GreaterOrEqual(flashT - signT, 0.6f - 0.05f, "S3 follows S2 by at least 0.6 s");
            Assert.IsTrue(model.StageBannerId > 0);
        }

        [Test]
        public void W31_MeteorWindowBehaviour_OpensAndClosesTheSpawner_WithEventData()
        {
            var tuning = ScriptableObject.CreateInstance<GameplayTuning>();
            var spawner = new MeteorSpawner(null, tuning) { StageDriven = true };
            var b = new MeteorWindowBehaviour(spawner);
            var st = StageLadderPlanner.Build(RealLadder(), Rec(4u)).Events.Find(e => e.Id == "EVT-METEOR");
            Assert.IsFalse(spawner.WindowOpen);
            b.OnStart(st);
            Assert.IsTrue(spawner.WindowOpen);
            Assert.AreEqual(3, spawner.StrikesLeft, "at most 3 strikes (PROPOSED)");
            b.OnEnd(st);
            Assert.IsFalse(spawner.WindowOpen);
        }

        [Test]
        public void W32_Meteor_RespawnRaisedFromInsideAStrikeImpact_DoesNotBreakTheTick()
        {
            // Regression (found in Play mode): a meteor hit costs a heart, the manager respawns the car and calls OnRespawn from inside Tick's loop over the live strikes.
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;       // the spawner uses Destroy, which edit mode only logs about
            var roadGo = new GameObject("TestRoad"); var road = roadGo.AddComponent<RoadBuilder>();
            var car = RoadCar.Create(new Vector2(0f, 1f), VehicleType.ClassicRed, ArtStyle.Pixel);
            try
            {
                var tuning = ScriptableObject.CreateInstance<GameplayTuning>();
                var spawner = new MeteorSpawner(road, tuning) { StageDriven = true };
                int impacts = 0;
                spawner.Impacted += (p, hit) => { impacts++; spawner.OnRespawn(); };
                spawner.BeginWindow(0.05f, 0.05f, 3, 7u);
                for (int i = 0; i < 120 && impacts < 1; i++) spawner.Tick(0.05f, car, 100f);
                Assert.GreaterOrEqual(impacts, 1, "a strike landed");
                spawner.Tick(0.05f, car, 100f);                     // the clear is applied here
                Assert.LessOrEqual(spawner.StrikesLeft, 2);
            }
            finally { UnityEngine.Object.DestroyImmediate(car.gameObject); UnityEngine.Object.DestroyImmediate(roadGo); UnityEngine.TestTools.LogAssert.ignoreFailingMessages = false; }
        }
    }
}
