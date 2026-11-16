using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using RoadBuilderGame.UI;

namespace RoadBuilderGame.Tests
{
    /// <summary>Gap list A, batch A2: the 26 remaining stage events (data, behaviours that always revert, never-stack matrix), endless boss tiers every 5000 m, forced daily events, Extreme tier.</summary>
    public class StageEvents2Tests
    {
        private const string Dir = "Assets/Resources/Stage";
        private static readonly string[] A2 =
        {
            "EVT-GRAVINV", "EVT-WHITEOUT", "EVT-REVERSE", "EVT-MIRROR", "EVT-FLAPPY", "EVT-COLLAPSE", "EVT-ICEAGE", "EVT-MAGNET", "EVT-ELASTIC", "EVT-SHADOW", "EVT-DOUBLE", "EVT-PLATFORMS",
            "EVT-GRAVZONES", "EVT-TRAFFIC", "EVT-BOULDER", "EVT-FENCES", "EVT-FLOOD", "EVT-LAVA", "EVT-WINDTUNNEL", "EVT-STICKY", "EVT-INVINK", "EVT-LASER", "EVT-STARS", "EVT-SWAP", "EVT-RHYTHM", "EVT-CHAOS"
        };

        private static StageLadderDefinition Endless() { var l = AssetDatabase.LoadAssetAtPath<StageLadderDefinition>(Dir + "/EndlessLadder.asset"); Assert.IsNotNull(l); return l; }
        private static StageEventDefinition Def(string id) { var d = AssetDatabase.LoadAssetAtPath<StageEventDefinition>(Dir + "/Events/" + id + ".asset"); Assert.IsNotNull(d, id + " (run Road Builder > Stage > Build Event Pack 2)"); return d; }
        private static StageRunRecord Rec(uint raw) { return new StageRunRecord { runSeed = StageSeed.MakeRunSeed(raw), modeId = StageRunController.ModeIdEndless, layoutVersion = 1 }; }
        private static readonly string[][] Worlds =
        {
            new[] { "hills", "earth" }, new[] { "moon" }, new[] { "mars" }, new[] { "desert", "earth" }, new[] { "synthwave", "earth" },
            new[] { "candy" }, new[] { "cave" }, new[] { "volcano" }, new[] { "snow", "earth" }, new[] { "haunted" }, new[] { "underwater" }
        };
        private static StageWorldContext Ctx(int i) { return new StageWorldContext { Tags = Worlds[i % Worlds.Length] }; }

        [Test]
        public void B01_TwentySixEvents_ValidWithCuesTokensAndCounterplay()
        {
            Assert.AreEqual(26, A2.Length);
            var errors = new List<string>(); var theme = AssetDatabase.LoadAssetAtPath<UITheme>("Assets/Resources/UI/UITheme_Default.asset");
            foreach (var id in A2)
            {
                var d = Def(id);
                Assert.IsTrue(d.Validate(errors), string.Join("\n", errors));
                Assert.Greater(d.effects.Length, 0, id); Assert.IsNotEmpty(d.ruleText, id); Assert.IsNotEmpty(d.counterplayText, id); Assert.IsNotEmpty(d.signText, id);
                Assert.IsNotEmpty(d.suitableWorlds, id); Assert.IsNotEmpty(d.hazardFamily, id);
                Assert.IsTrue(StageCues.Registry.Contains(d.announcerCueId) && StageCues.Registry.Contains(d.warnSfxId) && StageCues.Registry.Contains(d.cueSfxId), id + " cue ids");
                Assert.IsNotEmpty(StageCues.BannerText(d.bannerKey), id + " banner");
                Assert.IsTrue(Enum.TryParse<ColorToken>(d.tintColorToken, out var tok), id + " token"); Assert.Greater(theme.GetColor(tok).a, 0.5f, id + " token colour set");
                Assert.AreEqual(5f, d.announcerLeadS); Assert.AreEqual(3f, d.signLeadS); Assert.AreEqual(1f, d.cueLeadS);
                Assert.IsTrue(Array.IndexOf(Endless().eventPool, d) >= 0, id + " is in the endless pool");
            }
        }

        [Test]
        public void B02_BlueprintNumbers()
        {
            Assert.AreEqual(-0.3f, Def("EVT-GRAVINV").effects[0].a); Assert.AreEqual(3f, Def("EVT-GRAVINV").effects[0].b); Assert.AreEqual(350f, Def("EVT-GRAVINV").lengthM); Assert.AreEqual(9, Def("EVT-GRAVINV").difficultyCost);
            Assert.AreEqual(30f, Def("EVT-WHITEOUT").effects[0].a); Assert.AreEqual(5f, Def("EVT-REVERSE").effects[0].b); Assert.AreEqual(250f, Def("EVT-FLAPPY").lengthM);
            Assert.AreEqual(1.5f, Def("EVT-COLLAPSE").effects[0].a); Assert.AreEqual(2f, Def("EVT-SHADOW").effects[0].a); Assert.AreEqual(1f, Def("EVT-INVINK").effects[0].a);
            Assert.AreEqual(0.5f, Def("EVT-DOUBLE").effects[0].a); Assert.AreEqual(1f, Def("EVT-PLATFORMS").effects[0].a);
            Assert.AreEqual(2f, Def("EVT-FENCES").effects[0].a); Assert.AreEqual(0.8f, Def("EVT-FENCES").effects[0].b);
            Assert.AreEqual(1f, Def("EVT-FLOOD").effects[0].a); Assert.AreEqual(5f, Def("EVT-FLOOD").effects[0].b); Assert.AreEqual(0.8f, Def("EVT-LAVA").effects[0].a);
            Assert.AreEqual(10, Def("EVT-CHAOS").difficultyCost); Assert.AreEqual(StageTier.Extreme, Def("EVT-CHAOS").Tier); Assert.AreEqual(500f, Def("EVT-CHAOS").lengthM);
            Assert.AreEqual(100f, Def("EVT-SWAP").effects[0].a);
        }

        [Test]
        public void B03_NeverStackList_IsForbidden_ForEveryBlueprintPair_AndChaosStacksWithNothing()
        {
            var m = Endless().matrix;
            string[][] never =
            {
                new[] { "EVT-GRAVINV", "EVT-SHADOW" }, new[] { "EVT-GRAVINV", "EVT-INVINK" }, new[] { "EVT-REVERSE", "EVT-MIRROR" }, new[] { "EVT-LOWINK", "EVT-INVINK" }, new[] { "EVT-LOWINK", "EVT-COLLAPSE" },
                new[] { "EVT-INVINK", "EVT-COLLAPSE" }, new[] { "EVT-TINY", "EVT-GIANT" }, new[] { "EVT-LAVA", "EVT-FLOOD" }, new[] { "EVT-FLAPPY", "EVT-REVERSE" }, new[] { "EVT-FRAGILE", "EVT-METEOR" },
            };
            foreach (var n in never) Assert.IsFalse(m.CanOverlap(n[0], n[1]), n[0] + " + " + n[1]);
            foreach (var d in Endless().eventPool) if (d.id != "EVT-CHAOS") Assert.IsFalse(m.CanOverlap("EVT-CHAOS", d.id), "chaos + " + d.id);
            int allow = 0; foreach (var p in m.pairs) if (p.rule == StageCompatRule.Allow && (p.idA.StartsWith("EVT-") && IsA2(p.idA) || IsA2(p.idB))) allow++;
            Assert.Greater(allow, 40, "A2 events have compatible partners");
        }

        private static void SetVariant(StageEventState st, int v) { typeof(StageEventState).GetProperty("Variant").GetSetMethod(true).Invoke(st, new object[] { v }); }

        private static bool IsA2(string id) { return Array.IndexOf(A2, id) >= 0; }

        private static StageWorld2 World(StageEffectState fx, RoadCar car, out int[] damage)
        {
            var dmg = new int[1]; damage = dmg; var view = new CarStageAdapter { Target = car };
            return new StageWorld2(fx, new StageWorld2Host { Car = () => car != null ? view : null, Road = () => null, Cam = () => null, Damage = r => dmg[0]++, CanDamage = () => true, DrainInk01 = f => { }, AddCoins = n => { }, ComboPenalty = n => { }, Cue = id => { } });
        }

        [Test]
        public void B04_EveryEvent_AppliesAndAlwaysReverts_OnEnd_OnReset_AndAfterARespawn()
        {
            var car = RoadCar.Create(new Vector2(0f, 1f), VehicleType.ClassicRed, ArtStyle.Pixel);
            var created = new List<GameObject>();
            try
            {
                foreach (var id in A2)
                {
                    var fx = new StageEffectState(); string neutral = fx.Snapshot();
                    var world = World(fx, car, out var dmg);
                    var hooks = new StageEffectHooks { World = world, Banner = s => { } };
                    var d = Def(id);
                    var beh = StageEffectRegistry.Create(d, fx, hooks);
                    Assert.IsInstanceOf<Fx2Behaviour>(beh, id);
                    var st = StageEventState.For(d, 1000f, 1000f + d.lengthM, 777u); SetVariant(st, 1);
                    car.Body.position = new Vector2(1000f, 1f);
                    beh.OnStart(st);
                    var tick = (IStageTickBehaviour)beh;
                    for (int i = 0; i < 400; i++) { car.Body.position += new Vector2(0.12f, 0f); world.Tick(0.05f); tick.Tick(0.05f, 1000f + i * 0.12f); if (i == 200) beh.OnRespawn(st, 1024f); }
                    beh.OnEnd(st);
                    Assert.AreEqual(neutral, fx.Snapshot(), id + " channels back to neutral on end");
                    Assert.AreEqual(0, world.LiveCount(beh), id + " frees every prop on end");
                    Assert.IsFalse(world.HopEnabled && id == "EVT-FLAPPY", "hop tap off after end");
                    beh.OnStart(st); world.Reset(); fx.Reset();
                    Assert.AreEqual(neutral, fx.Snapshot(), id + " neutral after Reset");
                    Assert.AreEqual(0, world.LiveCount(null), id + " no props after Reset");
                    var root = GameObject.Find("StageWorld2"); if (root != null) created.Add(root);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(car.gameObject);
                foreach (var g in created) if (g != null) UnityEngine.Object.DestroyImmediate(g);
                foreach (var g in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None)) if (g != null && (g.name == "StageWorld2" || g.name == "StageOverlay" || g.name == "StageFog" || g.name == "MirrorCursor")) UnityEngine.Object.DestroyImmediate(g);
            }
        }

        [Test]
        public void B05_ChannelValues_MirrorMotorGravityFrictionBounceSwapChaos()
        {
            var fx = new StageEffectState(); var run = new StageRunController(Endless());
            var h = new StageEffectHooks();
            Fx2Behaviour Make(string id) { var b = (Fx2Behaviour)StageEffectRegistry.Create(Def(id), fx, h); return b; }
            var mirror = Make("EVT-MIRROR"); mirror.OnStart(StageEventState.For(Def("EVT-MIRROR"), 0, 300)); Assert.IsTrue(fx.Mirror); mirror.OnEnd(null); Assert.IsFalse(fx.Mirror);
            var ice = Make("EVT-ICEAGE"); ice.OnStart(StageEventState.For(Def("EVT-ICEAGE"), 0, 300)); Assert.AreEqual(0.15f, fx.FrictionMul, 1e-5f); ice.OnEnd(null); Assert.AreEqual(1f, fx.FrictionMul);
            var el = Make("EVT-ELASTIC"); el.OnStart(StageEventState.For(Def("EVT-ELASTIC"), 0, 300)); Assert.AreEqual(0.55f, fx.BounceAdd, 1e-5f); el.OnEnd(null); Assert.AreEqual(0f, fx.BounceAdd);
            var sticky = Make("EVT-STICKY"); sticky.OnStart(StageEventState.For(Def("EVT-STICKY"), 0, 300)); Assert.AreEqual(2.2f, fx.FrictionMul, 1e-5f); Assert.AreEqual(0.92f, fx.SpeedMul, 1e-5f); sticky.OnEnd(null);
            var o = new object(); fx.Add(o, StageEffectKind.MotorReverse, -0.5f, 5f); Assert.AreEqual(1f, fx.MotorDir); fx.SetCur(o, StageEffectKind.MotorReverse, -0.5f); Assert.AreEqual(-0.5f, fx.MotorDir); fx.Remove(o); Assert.AreEqual(1f, fx.MotorDir);
            fx.Add(o, StageEffectKind.GravityPulse, -0.3f, 3f); Assert.AreEqual(1f, fx.GravityMul); fx.SetCur(o, StageEffectKind.GravityPulse, -0.3f); Assert.AreEqual(-0.3f, fx.GravityMul, 1e-5f); fx.Remove(o); Assert.AreEqual(1f, fx.GravityMul);
            fx.Add(o, StageEffectKind.VehicleSwap, 100f, 0f); fx.SetCur(o, StageEffectKind.VehicleSwap, 2f); Assert.AreEqual(1.2f, fx.GravityMul, 1e-5f); Assert.AreEqual(1.2f, fx.CarScale, 1e-5f); fx.Remove(o); Assert.AreEqual(1f, fx.CarScale);
            // chaos: two of the four modifier effects, chosen by variant, never all four
            var chaos = Make("EVT-CHAOS"); var st = StageEventState.For(Def("EVT-CHAOS"), 0, 500); var world = new StageWorld2(fx, new StageWorld2Host());
            h.World = world; chaos = Make("EVT-CHAOS");
            for (int v = 0; v < 3; v++)
            {
                SetVariant(st, v); chaos.OnStart(st);
                int changed = 0; if (!Mathf.Approximately(fx.GravityMul, 1f)) changed++; if (!Mathf.Approximately(fx.SpeedMul, 1f)) changed++; if (!Mathf.Approximately(fx.InkRegenMul, 1f)) changed++; if (!Mathf.Approximately(fx.BoostDrainMul, 1f)) changed++;
                Assert.AreEqual(2, changed, "variant " + v + " applies exactly two modifiers"); chaos.OnEnd(st); Assert.IsFalse(fx.Any);
            }
            var go = GameObject.Find("StageWorld2"); if (go != null) UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void B06_World_PoolIsBounded_FreesCleanly_AndMirrorMapsAboutTheCameraCentre()
        {
            var fx = new StageEffectState(); var camGo = new GameObject("cam"); var cam = camGo.AddComponent<Camera>(); camGo.transform.position = new Vector3(10f, 0f, -10f);
            var w = new StageWorld2(fx, new StageWorld2Host { Cam = () => cam });
            try
            {
                var owner = new object(); int got = 0;
                for (int i = 0; i < 60; i++) if (w.Spawn(owner, i, 0f, 1f, 1f, Color.white) != null) got++;
                Assert.AreEqual(40, got, "pool of 40, a full pool simply spawns less");
                w.FreeOwned(owner); Assert.AreEqual(0, w.LiveCount(null));
                Assert.AreEqual(new Vector2(3f, 2f), w.MapPointer(new Vector2(3f, 2f)), "identity while Mirror is off");
                fx.Add(owner, StageEffectKind.MirrorInput, 1f, 0f);
                Assert.AreEqual(17f, w.MapPointer(new Vector2(3f, 2f)).x, 1e-4f, "x mirrors about the camera centre (10)");
                fx.Remove(owner); w.Reset();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(camGo);
                foreach (var g in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None)) if (g != null && (g.name == "StageWorld2" || g.name == "MirrorCursor")) UnityEngine.Object.DestroyImmediate(g);
            }
        }

        // ---------------------------------------------------------------- planner

        [Test]
        public void B10_Planner_2000Seeds_UsesNewEvents_ExtremeTier_AndNeverStacksChaosOrNeverStackPairs()
        {
            var l = Endless(); var m = l.matrix;
            var drawn = new Dictionary<string, int>(); int extreme = 0, stacks = 0;
            for (uint s = 1; s <= 2000; s++)
            {
                var plan = EndlessLadderPlanner.Plan(l, Rec(s), 0f, Ctx((int)s));
                var ev = plan.Events;
                foreach (var e in ev) { drawn.TryGetValue(e.Id, out int c); drawn[e.Id] = c + 1; if (e.Definition.Tier == StageTier.Extreme) extreme++; }
                for (int i = 0; i < ev.Count; i++)
                    for (int j = i + 1; j < ev.Count; j++)
                        if (StageLadderPlanner.Overlaps(ev[i], ev[j])) { Assert.IsTrue(m.CanOverlap(ev[i].Id, ev[j].Id), "seed " + s + " " + ev[i].Id + "+" + ev[j].Id); stacks++; Assert.AreNotEqual("EVT-CHAOS", ev[i].Id); Assert.AreNotEqual("EVT-CHAOS", ev[j].Id); }
            }
            foreach (var id in A2) Assert.IsTrue(drawn.ContainsKey(id) && drawn[id] > 0, id + " is drawn by some world in 2000 seeds");
            Assert.Greater(extreme, 100, "the 5% Extreme weight is used (Chaos Finale)");
            TestContext.WriteLine("A2 draws over 2000 seeds: " + string.Join(", ", drawn) + " | extreme " + extreme + " | stacks " + stacks);
        }

        [Test]
        public void B11_Boss_EveryFiveThousand_WithTierByDistance()
        {
            var l = Endless(); var tiers = BossTierSO.Defaults();
            for (uint s = 1; s <= 200; s++)
            {
                var plan = EndlessLadderPlanner.Plan(l, Rec(s), 0f, Ctx((int)s));
                var bosses = plan.Events.FindAll(e => e.Id == "BOS-ROB0");
                var at = new List<int>(); foreach (var b in bosses) at.Add(b.SlotIndex / 10);
                foreach (int k in new[] { 3, 5, 8, 10, 15, 20, 25, 30, 35, 40 }) Assert.Contains(k, at, "seed " + s + " boss at stage " + k);
                foreach (var b in bosses)
                {
                    string name = BossTierSO.ForDistance(tiers, b.StartM).displayName; int k = b.SlotIndex / 10;
                    string want = k >= 10 ? "Overlord" : (k >= 8 ? "Commander" : (k >= 5 ? "Interceptor" : "Hunter"));
                    Assert.AreEqual(want, name, "seed " + s + " stage " + k);
                }
            }
        }

        [Test]
        public void B12_ForcedDailyEvents_AreUsedInOrder_AndUnknownIdsAreLogged()
        {
            var l = Endless(); var ctx = Ctx(0);
            ctx.ForcedIds = new[] { "EVT-FENCES", "EVT-LASER", "EVT-LOWINK" };
            var plan = EndlessLadderPlanner.Plan(l, Rec(42), 0f, ctx);
            // stage 1 is the Road-Eater, stages 3 and 5 are boss stages: the forced events take the free stages 2, 4, 6 in order
            Assert.AreEqual("EVT-FENCES", plan.Events.Find(e => e.SlotIndex == 20).Id); Assert.AreEqual("EVT-LASER", plan.Events.Find(e => e.SlotIndex == 40).Id);
            Assert.AreEqual("EVT-LOWINK", plan.Events.Find(e => e.SlotIndex == 60).Id);
            // through the progression hook
            ForcedEvents.Current = new FixedForcedEventList(new[] { "EVT-TAX", "EVT-GRAVSHIFT", "EVT-NOPE" });
            try
            {
                var p2 = EndlessLadderPlanner.Plan(l, Rec(43), 0f, Ctx(1));
                Assert.AreEqual("EVT-TAX", p2.Events.Find(e => e.SlotIndex == 20).Id); Assert.AreEqual("EVT-GRAVSHIFT", p2.Events.Find(e => e.SlotIndex == 40).Id);
                Assert.IsTrue(p2.Log.Exists(x => x.Code == "forced_event_unknown"));
            }
            finally { ForcedEvents.Current = null; }
            var free = EndlessLadderPlanner.Plan(l, Rec(42), 0f, Ctx(0));
            Assert.AreNotEqual("EVT-FENCES", free.Events.Find(e => e.SlotIndex == 20)?.Id, "no forcing without the hook");
        }
    }
}
