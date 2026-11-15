using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace RoadBuilderGame.Tests
{
    public class SurfaceModuleTests
    {
        private static SurfaceRuleSO Rule(string id)
        {
            var r = ScriptableObject.CreateInstance<SurfaceRuleSO>(); r.surfaceId = id; return r;
        }

        [Test]
        public void M01_Ice_SlipMatchesLegacyFormula()
        {
            var r = Rule("surface-ice"); var z = new SurfaceZoneModel(); z.Enter(r);
            Assert.IsTrue(z.IsIce);
            z.Tick(0.016f, 8f, 1f, 0f, 20f, false); Assert.IsFalse(z.DriveCut);          // under the 25 deg threshold
            z.Tick(0.016f, 8f, 1f, 0f, 45f, false); Assert.IsTrue(z.DriveCut);
            Assert.AreEqual(SurfaceZoneModel.SlipSeconds(r, 45f), z.SlipLeftS, 0.02f);
        }

        [Test]
        public void M02_Mud_SpeedMatchesLegacyFormula()
        {
            var r = Rule("surface-mud"); var z = new SurfaceZoneModel(); z.Enter(r);
            float t = 0f, v = 0f;
            for (int i = 0; i < 125; i++) { t += 0.016f; v = z.Tick(0.016f, 9.8f, 1f, 0f, 0f, false); }
            Assert.AreEqual(SurfaceZoneModel.MudSpeed(r, 9.8f, t, 1f, 0f, false), v, 0.001f);
            Assert.AreEqual(9.8f - 2f, v, 0.01f);                                          // cap reached after 2 s
            Assert.AreEqual(9.8f, z.Tick(0.016f, 9.8f, 1f, 0f, 0f, true), 0.001f);         // boost ignores drag
        }

        [Test]
        public void M03_NoModules_KeepsTargetSpeed()
        {
            var r = Rule("surface-asphalt"); var z = new SurfaceZoneModel(); z.Enter(r);
            Assert.AreEqual(7.5f, z.Tick(0.016f, 7.5f, 1f, 30f, 90f, false), 0f);
            Assert.IsFalse(z.DriveCut);
        }

        [Test]
        public void M04_Breakable_EntryPlatformThenCracksThenDrops()
        {
            var r = Rule("surface-glass"); var h = new SurfaceBehaviourHost(); h.Enter(r, SurfaceModuleFactory.LegacySpecs(r));
            var f = new SurfaceFrame { Dt = 0.05f, BaseTarget = 8f, ZoneMetres = 3f };
            for (int i = 0; i < 100; i++) h.Tick(f);
            Assert.AreEqual(0, h.MaxStage);                                                // inside the 6 m platform: never cracks
            f.ZoneMetres = 10f;
            for (int i = 0; i < 17; i++) h.Tick(f);                                        // 0.85 s
            Assert.AreEqual(1, h.MaxStage);
            for (int i = 0; i < 40; i++) h.Tick(f);                                        // 2.85 s
            Assert.IsTrue(h.AnyGone);
        }

        [Test]
        public void M05_Breakable_HeavyImpactStepsUp_LightDoesNot()
        {
            var r = Rule("surface-glass"); var h = new SurfaceBehaviourHost(); h.Enter(r, SurfaceModuleFactory.LegacySpecs(r));
            h.ProjectileImpact(1f, false); Assert.AreEqual(0, h.MaxStage);
            h.ProjectileImpact(3f, false); Assert.AreEqual(1, h.MaxStage);
            h.ProjectileImpact(3f, false); Assert.AreEqual(2, h.MaxStage);
        }

        [Test]
        public void M06_BounceSpeedPadConveyorDissolveHeatOscillation()
        {
            var h = new SurfaceBehaviourHost(); var f = new SurfaceFrame { Dt = 0.05f, BaseTarget = 8f };
            h.Enter(null, new[] { new SurfaceModuleSpec(SurfaceModuleKind.Bounce, 5f, 1f), new SurfaceModuleSpec(SurfaceModuleKind.SpeedPad, 2f, 1f), new SurfaceModuleSpec(SurfaceModuleKind.Conveyor, 1f) });
            Assert.AreEqual(5f, h.TakeImpulse(), 0f); Assert.AreEqual(0f, h.TakeImpulse(), 0f);
            Assert.AreEqual(11f, h.TargetSpeed(f), 0.001f);                                // 8 + pad 2 + conveyor 1
            for (int i = 0; i < 30; i++) h.Tick(f);
            Assert.AreEqual(9f, h.TargetSpeed(f), 0.001f);                                 // pad over
            h.Exit();
            h.Enter(null, new[] { new SurfaceModuleSpec(SurfaceModuleKind.Dissolve, 1f, 0f), new SurfaceModuleSpec(SurfaceModuleKind.Heat, 1f, 2f), new SurfaceModuleSpec(SurfaceModuleKind.Oscillation, 0.2f, 1f) });
            for (int i = 0; i < 25; i++) h.Tick(f);
            Assert.IsTrue(h.Find<DissolveModule>().Gone);
            Assert.AreNotEqual(0f, h.Sway);
            h.ProjectileImpact(1f, true);
            for (int i = 0; i < 50; i++) h.Tick(f);
            Assert.IsTrue(h.Find<HeatModule>().Gone);
            h.Exit(); Assert.AreEqual(0, h.Count);
        }

        [Test]
        public void M07_Learning_FullThenIconThenNone_SavedInProfile()
        {
            var p = new PlayerProfile();
            Assert.AreEqual(SurfaceHintLevel.Full, SurfaceLearning.Level(p.MarkSurfaceSeen("surface-ice")));
            Assert.AreEqual(SurfaceHintLevel.Icon, SurfaceLearning.Level(p.MarkSurfaceSeen("surface-ice")));
            Assert.AreEqual(SurfaceHintLevel.None, SurfaceLearning.Level(p.MarkSurfaceSeen("surface-ice")));
            Assert.AreEqual(3, p.SurfaceSeen("surface-ice"));
            Assert.AreEqual(0, p.SurfaceSeen("surface-mud"));
            var back = JsonUtility.FromJson<PlayerProfile>(JsonUtility.ToJson(p));
            Assert.AreEqual(3, back.SurfaceSeen("surface-ice"));
        }

        [Test]
        public void M08_Compatibility_Validation()
        {
            var db = ScriptableObject.CreateInstance<SurfaceCompatibilityDatabase>();
            db.pairs = new[]
            {
                new SurfacePairRule { a = "surface-sea-water", b = "surface-lava-crust", combo = SurfaceCombo.Merge, mergeId = "surface-obsidian" },
                new SurfacePairRule { a = "surface-ice", b = "surface-mud", combo = SurfaceCombo.Forbidden },
            };
            var errs = new List<string>();
            Assert.IsTrue(db.Validate(errs), string.Join("\n", errs));
            Assert.AreEqual(SurfaceCombo.Merge, db.Query("surface-lava-crust", "surface-sea-water", out var m)); Assert.AreEqual("surface-obsidian", m);
            Assert.AreEqual(SurfaceCombo.Forbidden, db.Query("surface-mud", "surface-ice", out _));
            Assert.AreEqual(SurfaceCombo.Allowed, db.Query("surface-dirt", "surface-ice", out _));
            db.pairs = new[]
            {
                new SurfacePairRule { a = "x", b = "x", combo = SurfaceCombo.Allowed },
                new SurfacePairRule { a = "a", b = "b", combo = SurfaceCombo.Merge },
                new SurfacePairRule { a = "a", b = "b", combo = SurfaceCombo.Forbidden },
            };
            errs.Clear();
            Assert.IsFalse(db.Validate(errs)); Assert.GreaterOrEqual(errs.Count, 3);
        }

        [Test]
        public void M09_BuiltAssets_MapAtLeast12Surfaces_AndValidate()
        {
            var sets = AssetDatabase.FindAssets("t:SurfaceBehaviourSetSO", new[] { "Assets/Resources/Stage/Surfaces" });
            Assert.GreaterOrEqual(sets.Length, 12, "run Road Builder > Stage > Build Surface Modules");
            var errs = new List<string>();
            foreach (var g in sets) Assert.IsTrue(AssetDatabase.LoadAssetAtPath<SurfaceBehaviourSetSO>(AssetDatabase.GUIDToAssetPath(g)).Validate(errs), string.Join("\n", errs));
            var db = AssetDatabase.LoadAssetAtPath<SurfaceCompatibilityDatabase>("Assets/Resources/Stage/Surfaces/SurfaceCompatibility.asset");
            Assert.IsNotNull(db); Assert.IsTrue(db.Validate(errs), string.Join("\n", errs));
        }
    }
}
