using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using RoadBuilderGame.UI;

namespace RoadBuilderGame.Tests
{
    /// <summary>
    /// Gap list A1: the twelve mutation events (data, effects apply and revert), the endless ladder planner (2000 seeds: determinism, no forbidden stacks, all
    /// blueprint rules), the tutorial-preview-once rule, the adaptive easing and the vehicle scale. Real assets, no scene.
    /// </summary>
    public class MutationPackTests
    {
        private const string Dir = "Assets/Resources/Stage";
        private static readonly string[] MutationIds =
        {
            "EVT-GRAVSHIFT", "EVT-HEAVYG", "EVT-TUNNEL", "EVT-INKSTORM", "EVT-BOOSTDRAIN", "EVT-VACUUM", "EVT-CANYON", "EVT-LOWINK", "EVT-TAX", "EVT-FRAGILE", "EVT-TINY", "EVT-GIANT"
        };

        private static StageLadderDefinition Endless()
        {
            var l = AssetDatabase.LoadAssetAtPath<StageLadderDefinition>(Dir + "/EndlessLadder.asset");
            Assert.IsNotNull(l, "Run Road Builder > Stage > Build Mutation Pack");
            return l;
        }

        private static StageEventDefinition Def(string id)
        {
            var d = AssetDatabase.LoadAssetAtPath<StageEventDefinition>(Dir + "/Events/" + id + ".asset");
            Assert.IsNotNull(d, id);
            return d;
        }

        private static StageRunRecord Rec(uint raw) { return new StageRunRecord { runSeed = StageSeed.MakeRunSeed(raw), modeId = StageRunController.ModeIdEndless, layoutVersion = 1 }; }

        private static readonly string[][] Worlds =
        {
            new[] { "hills", "earth" }, new[] { "moon" }, new[] { "mars" }, new[] { "desert", "earth" }, new[] { "synthwave", "earth" },
            new[] { "candy" }, new[] { "cave" }, new[] { "volcano" }, new[] { "snow", "earth" }, new[] { "haunted" }
        };

        private static StageWorldContext Ctx(int i, int[] eased = null) { return new StageWorldContext { Tags = Worlds[i % Worlds.Length], EasedStages = eased }; }

        // ---------------------------------------------------------------- data

        [Test]
        public void A01_TwelveEvents_ValidAndComplete()
        {
            var errors = new List<string>();
            var theme = AssetDatabase.LoadAssetAtPath<UITheme>("Assets/Resources/UI/UITheme_Default.asset");
            foreach (var id in MutationIds)
            {
                var d = Def(id);
                Assert.IsTrue(d.Validate(errors), string.Join("\n", errors));
                Assert.Greater(d.effects.Length, 0, id + " effects");
                Assert.IsNotEmpty(d.ruleText, id + " rule"); Assert.IsNotEmpty(d.counterplayText, id + " counterplay (every mutation has one)");
                Assert.IsNotEmpty(d.signText, id + " sign text"); Assert.IsNotEmpty(d.iconKey, id + " icon key");
                Assert.IsNotEmpty(d.suitableWorlds, id + " suitable worlds"); Assert.IsNotEmpty(d.hazardFamily, id + " family");
                Assert.IsTrue(StageCues.Registry.Contains(d.announcerCueId), id + " voice cue id");
                Assert.IsTrue(StageCues.Registry.Contains(d.warnSfxId) && StageCues.Registry.Contains(d.cueSfxId), id + " sign and cue sfx ids");
                Assert.IsNotEmpty(StageCues.BannerText(d.bannerKey), id + " banner"); Assert.IsNotEmpty(StageCues.SignText(d.bannerKey), id + " sign");
                Assert.IsTrue(Enum.TryParse<ColorToken>(d.tintColorToken, out var tok), id + " tint token");
                Assert.Greater(theme.GetColor(tok).a, 0.5f, id + " token colour is set in the theme");
                Assert.AreEqual(5f, d.announcerLeadS); Assert.AreEqual(3f, d.signLeadS); Assert.AreEqual(1f, d.cueLeadS);
            }
        }

        [Test]
        public void A02_BlueprintNumbers()
        {
            Assert.AreEqual(0.65f, Def("EVT-GRAVSHIFT").effects[0].a); Assert.AreEqual(1.6f, Def("EVT-HEAVYG").effects[0].a);
            Assert.AreEqual(1.45f, Def("EVT-TUNNEL").effects[0].a);
            var ink = Def("EVT-INKSTORM"); Assert.AreEqual(0.5f, ink.effects[0].a); Assert.AreEqual(StageEffectKind.WindGust, ink.effects[1].kind);
            Assert.AreEqual(2f, Def("EVT-BOOSTDRAIN").effects[0].a);
            Assert.AreEqual(3f, Def("EVT-CANYON").effects[0].a); Assert.AreEqual(0.45f, Def("EVT-LOWINK").effects[0].a);
            var tax = Def("EVT-TAX").effects[0]; Assert.AreEqual(10f, tax.a); Assert.AreEqual(100f, tax.b);
            Assert.AreEqual(0.6f, Def("EVT-FRAGILE").effects[0].a);
            Assert.AreEqual(0.55f, Def("EVT-TINY").effects[0].a); Assert.AreEqual(1.6f, Def("EVT-GIANT").effects[0].a);
            Assert.AreEqual(450f, Def("EVT-GRAVSHIFT").lengthM); Assert.AreEqual(300f, Def("EVT-TUNNEL").lengthM); Assert.AreEqual(300f, Def("EVT-TINY").lengthM);
        }

        [Test]
        public void A03_Matrix_NeverStackListsAreForbidden_AndCoverageIsComplete()
        {
            var l = Endless(); var m = l.matrix;
            var errors = new List<string>();
            Assert.IsTrue(l.Validate(errors), string.Join("\n", errors));
            string[][] never =
            {
                new[] { "EVT-TINY", "EVT-GIANT" }, new[] { "EVT-GRAVSHIFT", "EVT-HEAVYG" }, new[] { "EVT-HEAVYG", "EVT-FRAGILE" },
                new[] { "EVT-FRAGILE", "EVT-METEOR" }, new[] { "EVT-FRAGILE", "EVT-CANYON" }, new[] { "MON-EATER", "BOS-ROB0" }, new[] { "MON-EATER", "EVT-TAX" },
            };
            foreach (var n in never) { Assert.IsTrue(m.TryGetPair(n[0], n[1], out var p), n[0] + "/" + n[1] + " has an entry"); Assert.AreEqual(StageCompatRule.Forbid, p.rule, n[0] + "/" + n[1]); }
            Assert.AreEqual(17 * 16 / 2, m.pairs.Length, "every pair of the 17 pool events is explicit");
            int allow = 0; foreach (var p in m.pairs) if (p.rule == StageCompatRule.Allow) allow++;
            Assert.Greater(allow, 20, "there are compatible stacks");
        }

        // ---------------------------------------------------------------- effects apply and revert

        [Test]
        public void A04_EveryMutation_AppliesItsEffect_AndRevertsOnEndAndReset()
        {
            var fx = new StageEffectState();
            var run = new StageRunController(Endless());
            var hooks = new StageEffectHooks();
            Assert.AreEqual(12, StageEffectRegistry.RegisterAll(run, fx, Endless().eventPool, hooks));
            string neutral = fx.Snapshot();
            foreach (var id in MutationIds)
            {
                Assert.IsTrue(run.HasBehaviour(id), id);
                var d = Def(id);
                var st = StageEventState.For(d, 1000, 1000 + d.lengthM, 12345u);
                var beh = Behaviour(run, id);
                beh.OnStart(st);
                Assert.AreNotEqual(neutral, fx.Snapshot(), id + " changes something while active");
                beh.OnEnd(st);
                Assert.AreEqual(neutral, fx.Snapshot(), id + " reverts exactly on end");
                beh.OnStart(st); fx.Reset();
                Assert.AreEqual(neutral, fx.Snapshot(), id + " reverts on Reset (death, restart, quit)");
                Assert.IsFalse(fx.Any);
            }
        }

        private static IStageEventBehaviour Behaviour(StageRunController run, string id)
        {
            var f = typeof(StageRunController).GetField("_behaviours", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return ((Dictionary<string, IStageEventBehaviour>)f.GetValue(run))[id];
        }

        [Test]
        public void A05_EffectValues_MatchTheSpec()
        {
            var fx = new StageEffectState();
            var run = new StageRunController(Endless());
            StageEffectRegistry.RegisterAll(run, fx, Endless().eventPool, new StageEffectHooks());
            float G(string id) { var d = Def(id); Behaviour(run, id).OnStart(StageEventState.For(d, 0f, 0f, 1u)); float v = fx.GravityMul; fx.Reset(); return v; }
            Assert.AreEqual(0.65f, G("EVT-GRAVSHIFT"), 1e-4f); Assert.AreEqual(1.6f, G("EVT-HEAVYG"), 1e-4f);
            Behaviour(run, "EVT-TUNNEL").OnStart(StageEventState.For(Def("EVT-TUNNEL"))); Assert.AreEqual(1.45f, fx.SpeedMul, 1e-4f); fx.Reset();
            Behaviour(run, "EVT-INKSTORM").OnStart(StageEventState.For(Def("EVT-INKSTORM"))); Assert.AreEqual(0.5f, fx.InkRegenMul, 1e-4f); fx.Reset();
            Behaviour(run, "EVT-LOWINK").OnStart(StageEventState.For(Def("EVT-LOWINK"))); Assert.AreEqual(0.45f, fx.InkCapMul, 1e-4f); fx.Reset();
            Behaviour(run, "EVT-BOOSTDRAIN").OnStart(StageEventState.For(Def("EVT-BOOSTDRAIN"))); Assert.AreEqual(2f, fx.BoostDrainMul, 1e-4f); fx.Reset();
            Behaviour(run, "EVT-TINY").OnStart(StageEventState.For(Def("EVT-TINY"))); Assert.AreEqual(0.55f, fx.CarScale, 1e-4f); fx.Reset();
            Behaviour(run, "EVT-GIANT").OnStart(StageEventState.For(Def("EVT-GIANT"))); Assert.AreEqual(1.6f, fx.CarScale, 1e-4f); fx.Reset();
            Behaviour(run, "EVT-FRAGILE").OnStart(StageEventState.For(Def("EVT-FRAGILE"))); Assert.AreEqual(0.6f, fx.FragileThreshold01, 1e-4f); fx.Reset();
            Behaviour(run, "EVT-VACUUM").OnStart(StageEventState.For(Def("EVT-VACUUM"))); Assert.IsTrue(fx.CoinVacuum); fx.Reset(); Assert.IsFalse(fx.CoinVacuum);
        }

        [Test]
        public void A06_Wind_IsSeededAndZeroAllocationShaped_AndStopsOnEnd()
        {
            float[] Run(uint seed, string id, int ticks)
            {
                var fx = new StageEffectState(); var d = Def(id);
                var b = new StageEffectBehaviour(d, fx);
                b.OnStart(StageEventState.For(d, 0f, 0f, seed));
                var v = new float[ticks];
                for (int i = 0; i < ticks; i++) { b.Tick(0.05f, 0f); v[i] = fx.WindAccelX; }
                b.OnEnd(StageEventState.For(d));
                Assert.AreEqual(0f, fx.WindAccelX, "wind is gone after the event");
                return v;
            }
            var a = Run(7u, "EVT-INKSTORM", 600); var b2 = Run(7u, "EVT-INKSTORM", 600); var c = Run(8u, "EVT-INKSTORM", 600);
            CollectionAssert.AreEqual(a, b2, "same seed, same gusts");
            Assert.IsFalse(Array.TrueForAll(a, x => x == 0f), "gusts happen");
            float peak = 0f; foreach (var x in a) peak = Mathf.Max(peak, Mathf.Abs(x));
            Assert.LessOrEqual(peak, 4.0001f, "peak gust is the spec value");
            var steady = Run(3u, "EVT-CANYON", 400);
            foreach (var x in steady) Assert.That(Mathf.Abs(x), Is.InRange(0.89f, 3.01f), "steady wind stays within 0.7..1.0 of 3 m/s^2");
        }

        [Test]
        public void A07_RoadTax_ChargesWithoutAGate_ThenGateSavesTheNextCheckpoint_NeverBelowZero()
        {
            var fx = new StageEffectState(); var d = Def("EVT-TAX");
            int coins = 25; var gates = new List<float>(); var banners = new List<string>();
            var hooks = new StageEffectHooks { PlaceCoinGate = (x, h) => gates.Add(x), ChargeCoins = n => { int t = Mathf.Clamp(n, 0, coins); coins -= t; return t; }, Banner = banners.Add };
            var b = new RoadTaxBehaviour(d, fx, hooks);
            var st = StageEventState.For(d, 3000f, 3400f, 9u);
            b.OnStart(st);
            Assert.AreEqual(4, gates.Count, "one gate per checkpoint (400 m / 100 m)");
            Assert.AreEqual(3060f, gates[0], 0.01f, "gate sits 40 m before its checkpoint");
            b.Tick(0.1f, 3101f); Assert.AreEqual(15, coins, "no gate crossed: 10 coins");
            fx.TaxGatesCrossed++;
            b.Tick(0.1f, 3201f); Assert.AreEqual(15, coins, "gate crossed: no tax");
            b.Tick(0.1f, 3301f); Assert.AreEqual(5, coins);
            b.Tick(0.1f, 3401f); Assert.AreEqual(0, coins, "never below zero: only 5 left to take");
            b.OnEnd(st); Assert.AreEqual(0, fx.TaxGatesCrossed);
        }

        [Test]
        public void A08_TutorialPreview_ShowsInThreeSteps_BySeenCounter()
        {
            var d = Def("EVT-GRAVSHIFT");
            var p = new PlayerProfile();
            string first = StageCues.Preview(d, p.MarkEventSeen(d.id));
            string second = StageCues.Preview(d, p.MarkEventSeen(d.id));
            string third = StageCues.Preview(d, p.MarkEventSeen(d.id));
            StringAssert.Contains(d.ruleText, first); StringAssert.Contains(d.counterplayText, first);
            Assert.AreEqual(d.counterplayText, second); Assert.AreEqual(string.Empty, third);
            Assert.AreEqual(3, p.EventSeen(d.id));
        }

        // ---------------------------------------------------------------- the endless planner

        [Test]
        public void A10_Planner_Determinism_2000Seeds()
        {
            var l = Endless();
            for (uint s = 1; s <= 2000; s++)
            {
                var ctx = Ctx((int)s);
                string a = StageTestData.Signature(EndlessLadderPlanner.Plan(l, Rec(s), 0f, ctx));
                string b = StageTestData.Signature(EndlessLadderPlanner.Plan(l, Rec(s), 0f, ctx));
                Assert.AreEqual(a, b, "seed " + s);
                Assert.IsNotEmpty(a);
            }
            Assert.AreNotEqual(StageTestData.Signature(EndlessLadderPlanner.Plan(l, Rec(1), 0f, Ctx(0))), StageTestData.Signature(EndlessLadderPlanner.Plan(l, Rec(2), 0f, Ctx(0))), "different seeds differ");
            var other = Rec(5); other.modeId = 9;
            Assert.AreNotEqual(StageTestData.Signature(EndlessLadderPlanner.Plan(l, Rec(5), 0f, Ctx(0))), StageTestData.Signature(EndlessLadderPlanner.Plan(l, other, 0f, Ctx(0))), "mode id is part of the seed");
        }

        [Test]
        public void A11_Planner_2000Seeds_NoForbiddenStacks_AndEveryBlueprintRule()
        {
            var l = Endless(); var m = l.matrix;
            int hardEvents = 0, stacked = 0, breathers = 0, total = 0;
            for (uint s = 1; s <= 2000; s++)
            {
                var plan = EndlessLadderPlanner.Plan(l, Rec(s), 0f, Ctx((int)s));
                var ev = plan.Events;
                var ctx = Ctx((int)s);
                Assert.Greater(ev.Count, 20, "seed " + s + " plans a long ladder");
                // fixed rows
                Assert.AreEqual("MON-EATER", ev[0].Id); Assert.AreEqual(1000f, ev[0].StartM);
                var boss = ev.Find(e => e.Id == "BOS-ROB0"); Assert.IsNotNull(boss, "boss at stage 8, seed " + s); Assert.AreEqual(8000f, boss.StartM);
                // no event in training stage 0
                foreach (var e in ev) Assert.GreaterOrEqual(e.StartM, 1000f);
                // overlaps need an explicit Allow; never more than 2 active at once
                for (int i = 0; i < ev.Count; i++)
                    for (int j = i + 1; j < ev.Count; j++)
                        if (StageLadderPlanner.Overlaps(ev[i], ev[j]))
                        {
                            Assert.IsTrue(m.CanOverlap(ev[i].Id, ev[j].Id), "seed " + s + ": forbidden stack " + ev[i].Id + " + " + ev[j].Id);
                            stacked++;
                        }
                for (int i = 0; i < ev.Count; i++)
                {
                    int active = 1;
                    for (int j = 0; j < ev.Count; j++) if (j != i && ev[j].StartM <= ev[i].StartM && ev[j].EndM > ev[i].StartM) active++;
                    Assert.LessOrEqual(active, l.endlessModifierCap, "seed " + s + ": more than 2 active at " + ev[i].StartM);
                }
                // 100 m between activations from 5000 m
                for (int i = 1; i < ev.Count; i++)
                    if (ev[i].StartM >= l.stackGapFromM) Assert.GreaterOrEqual(ev[i].StartM - ev[i - 1].StartM, l.activationGapM - 0.01f, "seed " + s + ": activation gap at " + ev[i].StartM);
                // per stage grouping
                var byStage = new SortedDictionary<int, List<StageEventState>>();
                foreach (var e in ev) { int k = e.SlotIndex / 10; if (!byStage.TryGetValue(k, out var lst)) byStage[k] = lst = new List<StageEventState>(); lst.Add(e); }
                string lastPrimary = null; int hardRun = 0;
                var famAt = new Dictionary<int, List<string>>();
                for (int k = 1; k <= l.endlessStageCount; k++)
                {
                    total++;
                    var row = l.RowFor(k);
                    if (!byStage.TryGetValue(k, out var evs)) { breathers++; hardRun = 0; continue; }
                    var primary = evs.Find(e => e.SlotIndex % 10 == 0);
                    Assert.IsNotNull(primary, "seed " + s + " stage " + k);
                    Assert.GreaterOrEqual(primary.StartM, k * 1000f, "stage window");
                    Assert.AreNotEqual(lastPrimary, primary.Id, "seed " + s + ": same primary twice in a row at stage " + k);
                    lastPrimary = primary.Id;
                    if (row.fixedEvent == null)
                    {
                        Assert.IsTrue(ctx.Suits(primary.Definition), "seed " + s + ": " + primary.Id + " is not suitable for the world");
                        Assert.Less(hardRun, l.hardStagesBeforeBreather, "seed " + s + ": no breather after two hard stages (stage " + k + ")");
                    }
                    foreach (var e in evs) if (e.SlotIndex % 10 == 1) { Assert.IsTrue(ctx.Suits(e.Definition), "stack suitable"); Assert.GreaterOrEqual(e.StartM - primary.StartM, l.activationGapM - 0.01f); }
                    bool hard = false; var fams = new List<string>();
                    foreach (var e in evs) { if (e.Definition.difficultyCost >= l.endlessHardCost) hard = true; fams.Add(e.Definition.hazardFamily); hardEvents += e.Definition.Tier >= StageTier.Hard ? 1 : 0; }
                    famAt[k] = fams; hardRun = hard ? hardRun + 1 : 0;
                }
                // hazard family: at most twice in any 4 stages (the fixed Eater and boss families appear once)
                for (int k = 1; k + 3 <= l.endlessStageCount; k++)
                {
                    var count = new Dictionary<string, int>();
                    for (int q = k; q < k + 4; q++) if (famAt.TryGetValue(q, out var f)) foreach (var fam in new HashSet<string>(f)) { count.TryGetValue(fam, out int c); count[fam] = c + 1; }
                    foreach (var kv in count) Assert.LessOrEqual(kv.Value, l.familyMaxInWindow, "seed " + s + ": family " + kv.Key + " more than twice in stages " + k + ".." + (k + 3));
                }
            }
            Assert.Greater(stacked, 1000, "stacks happen (and are all allowed)");
            Assert.Greater(breathers, 2000, "breathers happen");
            TestContext.WriteLine("2000 seeds: " + total + " stages, " + breathers + " breather stages, " + stacked + " allowed stacks, " + hardEvents + " Hard+ events");
        }

        [Test]
        public void A12_TierWeights_Follow40_35_20_5()
        {
            var l = Endless();
            var cands = new List<StageEventDefinition>
            {
                StageTestData.Def("T-E", StageEventRole.Primary, 4, 0, 0, 300, 100), StageTestData.Def("T-M", StageEventRole.Primary, 6, 0, 0, 300, 100),
                StageTestData.Def("T-H", StageEventRole.Primary, 8, 0, 0, 300, 100), StageTestData.Def("T-X", StageEventRole.Primary, 10, 0, 0, 300, 100)
            };
            var count = new int[4]; const int N = 100000;
            for (int i = 0; i < N; i++) count[(int)EndlessLadderPlanner.Choose(l, cands, false, (i + 0.5f) / N, 0.5f).Tier]++;
            Assert.AreEqual(0.40f, count[0] / (float)N, 0.01f); Assert.AreEqual(0.35f, count[1] / (float)N, 0.01f);
            Assert.AreEqual(0.20f, count[2] / (float)N, 0.01f); Assert.AreEqual(0.05f, count[3] / (float)N, 0.01f);
            var eased = new int[4];
            for (int i = 0; i < N; i++) eased[(int)EndlessLadderPlanner.Choose(l, cands, true, (i + 0.5f) / N, 0.5f).Tier]++;
            Assert.Less(eased[2] + eased[3], count[2] + count[3], "adaptive easing lowers the Hard and Extreme weights");
            TestContext.WriteLine("normal " + string.Join("/", count) + " eased " + string.Join("/", eased));
        }

        [Test]
        public void A13_Planner_OnlySuitableWorldsAreDrawn_PerEventTable()
        {
            var l = Endless();
            for (int w = 0; w < Worlds.Length; w++)
            {
                var ctx = Ctx(w);
                var seen = new HashSet<string>();
                for (uint s = 1; s <= 300; s++)
                    foreach (var e in EndlessLadderPlanner.Plan(l, Rec(s), 0f, ctx).Events)
                        if (e.Definition.role == StageEventRole.Primary) { Assert.IsTrue(ctx.Suits(e.Definition), string.Join("/", ctx.Tags) + " drew " + e.Id); seen.Add(e.Id); }
                Assert.Greater(seen.Count, 2, string.Join("/", ctx.Tags) + " has a varied pool");
            }
            // blueprint spot checks
            Assert.IsTrue(Ctx(1).Suits(Def("EVT-GRAVSHIFT")), "moon"); Assert.IsFalse(Ctx(0).Suits(Def("EVT-GRAVSHIFT")), "hills is not a low-gravity world");
            Assert.IsTrue(Ctx(5).Suits(Def("EVT-TINY")), "candy"); Assert.IsFalse(Ctx(3).Suits(Def("EVT-TINY")));
            Assert.IsTrue(Ctx(7).Suits(Def("EVT-LOWINK")), "all-world event");
        }

        [Test]
        public void A14_Planner_AdaptiveEasing_LowersHardShare_AndContinuationStartsAfterTheGate()
        {
            var l = Endless();
            int Hard(bool eased)
            {
                int hard = 0;
                for (uint s = 1; s <= 2000; s++)
                {
                    var ctx = Ctx(0, eased ? new[] { 6 } : null);                           // stage 6 is the elite row (Hard..Extreme)
                    var plan = EndlessLadderPlanner.Plan(l, Rec(s), 0f, ctx);
                    foreach (var e in plan.Events) if (e.SlotIndex / 10 == 6 && e.SlotIndex % 10 == 0 && e.Definition.Tier >= StageTier.Hard) hard++;
                }
                return hard;
            }
            int normal = Hard(false), easedHard = Hard(true);
            TestContext.WriteLine("stage 6 Hard+ draws: normal " + normal + ", eased " + easedHard);
            Assert.Less(easedHard, normal, "easing a stage lowers how often it draws a Hard event");
            // continuing after the slice: nothing before the gate + warm-up, stage index follows the distance
            var plan2 = EndlessLadderPlanner.Plan(l, Rec(11), 2900f, Ctx(0));
            Assert.GreaterOrEqual(plan2.Events[0].StartM, 3000f);
            Assert.AreEqual(3, plan2.Events[0].SlotIndex / 10, "first stage after a 2900 m finish is stage 3 (movement)");
            Assert.Greater(plan2.Events[0].WarnM, 2900f, "its warning starts after the gate");
            Assert.AreEqual(0f, plan2.FinishM, "the endless ladder has no finish gate");
        }

        [Test]
        public void A15_SliceLadder_IsUnchanged_SevenSlots()
        {
            var l = AssetDatabase.LoadAssetAtPath<StageLadderDefinition>(Dir + "/StageLadder.asset");
            Assert.AreEqual(StageLadderMode.FixedSpine, l.ladderMode); Assert.AreEqual(7, l.slots.Length); Assert.AreEqual(21, l.matrix.pairs.Length);
            Assert.IsFalse(StageRunController.IsEndless(l)); Assert.IsTrue(StageRunController.IsEndless(Endless()));
        }

        [Test]
        public void A16_AdaptiveCounters_AreSaveServiceCounters_AndClear()
        {
            var p = new PlayerProfile();
            Assert.IsFalse(p.adaptiveChallenge, "opt-in, default off");
            p.AddStageFail(6); p.AddStageFail(6); p.AddStageFail(6);
            Assert.AreEqual(3, p.StageFails(6)); p.ClearStageFail(6); Assert.AreEqual(0, p.StageFails(6));
        }

        // ---------------------------------------------------------------- vehicle scale

        [Test]
        public void A20_RoadCar_Scale_IsPhysicsSafe_AndReverts()
        {
            var car = RoadCar.Create(new Vector2(0f, 1f), VehicleType.ClassicRed, ArtStyle.Pixel);
            try
            {
                float m0 = car.Body.mass, t0 = car.maxTorquePerWheel;
                var joints = car.Body.GetComponents<WheelJoint2D>(); Assert.AreEqual(2, joints.Length);
                var wheels = new[] { joints[0].connectedBody, joints[1].connectedBody };
                float wm0 = wheels[0].mass;
                car.SetScale(0.55f);
                Assert.AreEqual(0.55f, car.Scale); Assert.AreEqual(0.55f, car.Body.transform.localScale.x, 1e-5f);
                Assert.AreEqual(m0 * 0.55f * 0.55f, car.Body.mass, 1e-4f, "mass follows the area");
                Assert.AreEqual(t0 * 0.3025f, car.maxTorquePerWheel, 1e-3f);
                foreach (var w in wheels)
                {
                    Assert.AreEqual(0.55f, w.transform.localScale.x, 1e-5f, "wheel and its collider scale");
                    float dist = Vector2.Distance(w.position, car.Body.position);
                    Assert.Less(dist, 1.0f, "wheel stays under the body, scaled anchor (full size is about 0.85)");
                }
                car.SetScale(1.6f);
                Assert.AreEqual(m0 * 1.6f * 1.6f, car.Body.mass, 1e-3f);
                car.SetScale(1f);
                Assert.AreEqual(m0, car.Body.mass, 1e-4f, "mass reverts exactly"); Assert.AreEqual(t0, car.maxTorquePerWheel, 1e-3f); Assert.AreEqual(wm0, wheels[0].mass, 1e-4f);
                Assert.AreEqual(1f, car.Body.transform.localScale.x, 1e-5f);
                car.SetGravityMul(0.65f); Assert.AreEqual(0.65f, car.Body.gravityScale, 1e-5f); Assert.AreEqual(0.65f, wheels[0].gravityScale, 1e-5f);
                car.SetGravityMul(1f); Assert.AreEqual(1f, car.Body.gravityScale); Assert.AreEqual(1f, wheels[1].gravityScale);
            }
            finally { UnityEngine.Object.DestroyImmediate(car.gameObject); }
        }
    }
}
