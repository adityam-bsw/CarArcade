using System.Collections.Generic;
using NUnit.Framework;
using RoadBuilderGame.Editor;
using UnityEngine;

namespace RoadBuilderGame.Tests
{
    /// <summary>Section C gap work: the new behaviour modules, the per-piece runtime on a fake road, the real road cut, the projectile and monster tables, compatibility pairs and the 28-surface map.</summary>
    public class SurfaceRuntimeTests
    {
        private sealed class FakeRoad : ISurfaceRoad
        {
            public readonly List<Vector2> Cuts = new List<Vector2>();
            public readonly List<Vector2> Protected = new List<Vector2>();
            public int Tints; public float Offset;
            public bool CutRange(float x0, float x1) { Cuts.Add(new Vector2(x0, x1)); return true; }
            public bool IsRangeProtected(float x0, float x1) { foreach (var p in Protected) if (x1 >= p.x && x0 <= p.y) return true; return false; }
            public void TintRange(float x0, float x1, Color c) { Tints++; }
            public void SetOffsetY(float dy) { Offset = dy; }
        }

        private static SurfaceBehaviourSetSO Set(params SurfaceModuleSpec[] specs)
        {
            var so = ScriptableObject.CreateInstance<SurfaceBehaviourSetSO>(); so.surfaceId = "surface-test"; so.modules = specs; so.pieceLengthM = 4f; so.safeEntryM = 6f; return so;
        }
        private static SurfaceModuleSpec S(SurfaceModuleKind k, float a = 0, float b = 0, float c = 0, float d = 0) { return new SurfaceModuleSpec(k, a, b, c, d); }
        private static IRoadBehaviour Mod(SurfaceModuleKind k, float a = 0, float b = 0, float c = 0, float d = 0)
        {
            var m = SurfaceModuleFactory.Create(k); m.Configure(S(k, a, b, c, d), null); return m;
        }
        private static void Run(IRoadBehaviour m, SurfaceFrame f, float seconds)
        {
            float t = 0f; while (t < seconds) { f.TimeS += 0.05f; m.Tick(0.05f, f); t += 0.05f; }
        }
        private static SurfaceFrame Frame() { return new SurfaceFrame { Dt = 0.05f, BaseTarget = 8f, Speed = 8f }; }
        private static SurfaceCarSample Car(float x, float speed = 8f) { return new SurfaceCarSample { X = x, Speed = speed, MudDragMul = 1f }; }

        [Test]
        public void R01_EveryKindBuildsConfiguresAndResets()
        {
            Assert.AreEqual(System.Enum.GetValues(typeof(SurfaceModuleKind)).Length, SurfaceModuleFactory.KindCount);
            foreach (SurfaceModuleKind k in System.Enum.GetValues(typeof(SurfaceModuleKind)))
            {
                var m = SurfaceModuleFactory.Create(k);
                m.Configure(S(k), null); m.OnVehicleEnter(); m.Tick(0.05f, Frame()); m.OnVehicleExit(); m.Reset();
                Assert.AreEqual(SurfaceModuleFactory.IsPieceLocal(k), m.PieceLocal, k.ToString());
            }
        }

        [Test]
        public void R02_Magnetic_OnlyMetallicIsPulled()
        {
            var m = Mod(SurfaceModuleKind.Magnetic, 7f, 0.8f); m.OnVehicleEnter();
            var f = Frame(); f.Metallic = false; Run(m, f, 0.2f);
            Assert.AreEqual(0f, m.VerticalAccel, 0f); Assert.AreEqual(0f, m.SpeedDelta, 0f);
            f.Metallic = true; Run(m, f, 0.2f);
            Assert.AreEqual(-7f, m.VerticalAccel, 0.001f); Assert.AreEqual(0.8f, m.SpeedDelta, 0.001f);
        }

        [Test]
        public void R03_Acid_DrainsRaisesOneHitAndRecovers()
        {
            var m = Mod(SurfaceModuleKind.Decay, 6f, 0.4f); m.OnVehicleEnter(); var f = Frame();
            Run(m, f, 3f); Assert.AreEqual(0, m.TakeDamage()); Assert.Less(m.ApplyTarget(8f, f), 8f);
            Run(m, f, 3.3f); Assert.AreEqual(1, m.TakeDamage()); Assert.AreEqual(0, m.TakeDamage());
            m.OnVehicleExit(); Run(m, f, 4f); Assert.AreEqual(0, m.Stage);
        }

        [Test]
        public void R04_Oil_TorqueGrowsWithDriveAndFireBurnsOnce()
        {
            var m = Mod(SurfaceModuleKind.Torque, 70f, 0.55f); m.OnVehicleEnter();
            var f = Frame(); f.Speed = 8f; f.BaseTarget = 8f; Run(m, f, 0.5f); float idle = Mathf.Abs(m.Torque);
            f.Speed = 3f; f.BaseTarget = 9f; Run(m, f, 0.5f);
            Assert.Greater(Mathf.Abs(m.Torque) + 0.001f, idle); Assert.AreEqual(0.55f, m.GripMul, 0.001f);
            m.OnProjectileImpact(1f, true); Run(m, f, 1.7f);
            Assert.AreEqual(1, m.TakeDamage());
        }

        [Test]
        public void R05_Sticky_AttachesThenReleasesThenRearms()
        {
            var m = Mod(SurfaceModuleKind.Sticky, 1f, 2f, 2f, 2f); m.OnVehicleEnter(); var f = Frame();
            Run(m, f, 0.2f); Assert.AreEqual(-2f, m.SpeedDelta, 0.001f); Assert.Greater(m.GripMul, 1.5f);
            Run(m, f, 1.0f); Assert.AreEqual(2f, m.SpeedDelta, 0.001f);                       // released: burst
            Run(m, f, 0.5f); Assert.AreEqual(0f, m.SpeedDelta, 0.001f);
        }

        [Test]
        public void R06_Electric_BlinkThenChargeCutsDriveOncePerWindow()
        {
            var m = Mod(SurfaceModuleKind.Charge, 2.5f, 1.2f, 0.6f, 0.5f); m.OnVehicleEnter(); var f = Frame();
            Run(m, f, 2.1f); Assert.AreEqual(1, m.Stage); Assert.IsFalse(m.DriveCut);          // warning blink
            Run(m, f, 0.5f); Assert.AreEqual(2, m.Stage); Assert.IsTrue(m.DriveCut);           // charged, shock
            Run(m, f, 0.7f); Assert.IsFalse(m.DriveCut);                                       // shock over inside the same window
            Run(m, f, 2.0f); Assert.AreEqual(0, m.Stage);                                      // safe again
        }

        [Test]
        public void R07_Paper_OnlyHeavyTears()
        {
            var light = Mod(SurfaceModuleKind.Tear, 1.6f); light.OnVehicleEnter(); var f = Frame(); f.Heavy = false; Run(light, f, 5f); Assert.IsFalse(light.Gone);
            var heavy = Mod(SurfaceModuleKind.Tear, 1.6f); heavy.OnVehicleEnter(); f.Heavy = true; Run(heavy, f, 1.0f); Assert.IsFalse(heavy.Gone); Run(heavy, f, 1.0f); Assert.IsTrue(heavy.Gone);
        }

        [Test]
        public void R08_Fade_AlphaSwingsBetweenMinAndOne()
        {
            var m = Mod(SurfaceModuleKind.Fade, 2f, 0.2f); var f = Frame(); float lo = 9f, hi = -9f;
            for (int i = 0; i < 100; i++) { f.TimeS += 0.05f; m.Tick(0.05f, f); lo = Mathf.Min(lo, m.Alpha); hi = Mathf.Max(hi, m.Alpha); }
            Assert.AreEqual(0.2f, lo, 0.03f); Assert.AreEqual(1f, hi, 0.03f);
        }

        [Test]
        public void R09_Spikes_BoostBreaksThemOtherwiseSnag()
        {
            var snag = Mod(SurfaceModuleKind.Spikes, 5f, 5f, -1.3f); snag.OnVehicleEnter(); var f = Frame(); Run(snag, f, 0.2f);
            Assert.AreEqual(-1.3f, snag.SpeedDelta, 0.001f);
            var boost = Mod(SurfaceModuleKind.Spikes, 5f, 5f, -1.3f); boost.OnVehicleEnter(); f = Frame(); f.Boosting = true; Run(boost, f, 0.2f);
            Assert.AreEqual(2, boost.Stage); Assert.AreEqual(0f, boost.SpeedDelta, 0.001f);
        }

        [Test]
        public void R10_LavaRiverPlatforms_CycleAndNeverRemoveRoad()
        {
            var m = Mod(SurfaceModuleKind.Cycle, 4f, 0.5f, 0.3f, 1.5f); m.OnVehicleEnter(); var f = Frame(); int sunk = 0, risen = 0;
            for (int i = 0; i < 100; i++) { f.TimeS += 0.05f; m.Tick(0.05f, f); if (m.Stage == 2) sunk++; if (m.Stage == 0) risen++; Assert.IsFalse(m.Gone); }
            Assert.Greater(sunk, 10); Assert.Greater(risen, 10);
        }

        [Test]
        public void R11_RopeHeavyHalvesSway_BuoyancyOnlyLightFloats_BumpKicksGrounded()
        {
            var rope = Mod(SurfaceModuleKind.Oscillation, 0.2f, 1f); rope.OnVehicleEnter(); var f = Frame(); f.Heavy = false; Run(rope, f, 0.25f); float light = Mathf.Abs(rope.Sway);
            f.Heavy = true; Run(rope, f, 0.05f); Assert.LessOrEqual(Mathf.Abs(rope.Sway), 0.1001f); Assert.Greater(light, 0f);
            var w = Mod(SurfaceModuleKind.Buoyancy, 0.3f, 0.6f, 0.94f); w.OnVehicleEnter(); f = Frame(); f.Heavy = false; Run(w, f, 0.1f); Assert.Greater(w.VerticalAccel, 0f);
            f.Heavy = true; Run(w, f, 0.1f); Assert.AreEqual(0f, w.VerticalAccel, 0f); Assert.AreEqual(0.6f, w.GripMul, 0.001f);
            var b = Mod(SurfaceModuleKind.Bump, 0.9f, 0.2f); b.OnVehicleEnter(); f = Frame(); f.Speed = 6f; Run(b, f, 0.3f); Assert.Greater(b.TakeImpulse(), 0f);
            f.Airborne = true; Run(b, f, 0.5f); Assert.AreEqual(0f, b.TakeImpulse(), 0f);
        }

        [Test]
        public void R12_LavaCrustBreaksAfterThreeSecondsOfStanding_AndSpringStrideLaunchesEveryThird()
        {
            var crust = Mod(SurfaceModuleKind.Breakable, 3f, 3f, 4f); crust.OnVehicleEnter(); var f = Frame(); f.ZoneMetres = 20f;
            Run(crust, f, 2.8f); Assert.IsFalse(crust.Gone); Run(crust, f, 0.4f); Assert.IsTrue(crust.Gone);
            var spring = Mod(SurfaceModuleKind.Bounce, 7f, 0.1f, 0f, 3f); f = Frame(); int launched = 0;
            for (int p = 0; p < 9; p++) { f.PieceIndex = p; spring.Tick(0.2f, f); spring.OnVehicleEnter(); if (spring.TakeImpulse() > 0f) launched++; spring.OnVehicleExit(); }
            Assert.AreEqual(3, launched);
        }

        // ------------------------------------------------------------------ runtime on a fake road
        private static SurfaceRuntime Rt(FakeRoad road, SurfaceBehaviourSetSO set, SurfaceInteractionTable table = null)
        {
            var rt = new SurfaceRuntime(); rt.Bind(road, table); rt.SetSurface(set.surfaceId, set, null, 0f); return rt;
        }
        private static void Drive(SurfaceRuntime rt, float x0, float x1, float speed, float dt = 0.05f)
        {
            for (float x = x0; x < x1; x += speed * dt) rt.Tick(dt, Car(x, speed), 8f);
        }
        private static void Stand(SurfaceRuntime rt, float x, float seconds, float dt = 0.05f)
        {
            for (float t = 0f; t < seconds; t += dt) rt.Tick(dt, Car(x, 0f), 8f);
        }

        [Test]
        public void R20_Glass_MovingCarNeverBreaksIt_StandingStillAfterEntryPlatformDoes()
        {
            var road = new FakeRoad(); var rt = Rt(road, Set(S(SurfaceModuleKind.Breakable)));
            Drive(rt, 0f, 60f, 8f); Assert.AreEqual(0, road.Cuts.Count, "a moving car leaves every piece before it drops");
            Stand(rt, 60.5f, 3.5f); Assert.AreEqual(1, road.Cuts.Count);
            Assert.AreEqual(60f, road.Cuts[0].x, 0.001f); Assert.AreEqual(64f, road.Cuts[0].y, 0.001f);
        }

        [Test]
        public void R21_EntryPlatformAndNoEatRoadNeverBreak_StateResetsInstead()
        {
            var road = new FakeRoad(); var rt = Rt(road, Set(S(SurfaceModuleKind.Dissolve, 0.5f, 0f)));
            Stand(rt, 2f, 2f); Assert.AreEqual(0, road.Cuts.Count, "inside the first 6 m");
            road.Protected.Add(new Vector2(30f, 40f)); Stand(rt, 33f, 3f);
            Assert.AreEqual(0, road.Cuts.Count, "noEat road (section B) is never cut");
        }

        [Test]
        public void R22_Cloud_DissolvesTwoSecondsAfterContactEvenWhenTheCarMovedOn()
        {
            var road = new FakeRoad(); var rt = Rt(road, Set(S(SurfaceModuleKind.Dissolve, 2f, 0f)));
            Drive(rt, 0f, 12f, 8f);
            Assert.AreEqual(0, road.Cuts.Count);
            Stand(rt, 14f, 3f);
            Assert.GreaterOrEqual(road.Cuts.Count, 1);
        }

        [Test]
        public void R23_RespawnGrace_DefersACutUnderTheCar()
        {
            var road = new FakeRoad(); var rt = Rt(road, Set(S(SurfaceModuleKind.Dissolve, 0.5f, 0f)));
            Stand(rt, 20f, 0.3f); rt.OnRespawn(); Stand(rt, 20f, 0.3f);
            Assert.AreEqual(0, road.Cuts.Count);
            Stand(rt, 20f, 2.5f); Assert.GreaterOrEqual(road.Cuts.Count, 1);
        }

        [Test]
        public void R24_RopeBridgeSwayReachesTheRoad_PoolIsBounded()
        {
            var road = new FakeRoad(); var rt = Rt(road, Set(S(SurfaceModuleKind.Oscillation, 0.2f, 1f)));
            Drive(rt, 0f, 200f, 10f);
            Assert.AreNotEqual(0f, road.Offset);
            Assert.LessOrEqual(rt.UsedCells, SurfaceRuntime.MaxCells);
        }

        [Test]
        public void R25_ContinuousModulesFollowTheCarAcrossPieces_MudKeepsItsDrag()
        {
            var road = new FakeRoad(); var rt = Rt(road, Set(S(SurfaceModuleKind.Sinking, 1f, 2f, 3f, 18f)));
            Drive(rt, 0f, 40f, 8f);
            Assert.Less(rt.Out.TargetSpeed, 8f - 1.5f);
        }

        [Test]
        public void R26_ElectricRailDriveCutAndMagnetPullReachTheOutputs()
        {
            var road = new FakeRoad(); var rt = Rt(road, Set(S(SurfaceModuleKind.Charge, 1f, 1f, 0.3f, 0.5f), S(SurfaceModuleKind.Magnetic, 7f, 0.8f)));
            bool cut = false; float pull = 0f;
            for (float t = 0f; t < 4f; t += 0.05f) { var c = Car(2f, 8f); c.Metallic = true; rt.Tick(0.05f, c, 8f); cut |= rt.Out.DriveCut; pull = Mathf.Min(pull, rt.Out.VerticalAccel); }
            Assert.IsTrue(cut); Assert.Less(pull, -6f);
        }

        // ------------------------------------------------------------------ projectile and monster tables
        private static SurfaceInteractionTable Table()
        {
            var t = ScriptableObject.CreateInstance<SurfaceInteractionTable>(); t.projectileRows = SurfaceModuleBuilder.ProjectileRows(); t.monsterRows = SurfaceModuleBuilder.MonsterRows(); return t;
        }

        [Test]
        public void R30_ProjectileTable_ValidatesAndCoversEveryEffect()
        {
            var t = Table(); var errs = new List<string>(); Assert.IsTrue(t.Validate(errs), string.Join("\n", errs));
            foreach (ProjectileSurfaceEffect e in System.Enum.GetValues(typeof(ProjectileSurfaceEffect)))
            {
                if (e == ProjectileSurfaceEffect.None) continue;
                Assert.IsTrue(t.TryPatch("surface-asphalt", e, out var kind, out _), e + " has no default row");
                Assert.AreNotEqual(SurfacePatchKind.None, kind, e.ToString());
            }
            t.TryPatch("surface-sea-water", ProjectileSurfaceEffect.BurnWood, out var k2, out _); Assert.AreEqual(SurfacePatchKind.None, k2);
        }

        [Test]
        public void R31_SandMoundSlowsThenExpires_WaterIgnoresIt_BurnWoodBurnsTheBridge()
        {
            var road = new FakeRoad(); var rt = Rt(road, Set(S(SurfaceModuleKind.Heat, 1f, 1.5f)), Table()); rt.SurfaceId = "surface-asphalt";
            rt.AddProjectileEffect(ProjectileSurfaceEffect.SandMound, 30f, 1.5f, 2f);
            rt.Tick(0.05f, Car(30f), 8f); Assert.AreEqual(6f, rt.Out.TargetSpeed, 0.01f);
            Stand(rt, 50f, 2.5f); rt.Tick(0.05f, Car(30f), 8f); Assert.AreEqual(8f, rt.Out.TargetSpeed, 0.01f);
            rt.SurfaceId = "surface-sea-water"; rt.AddProjectileEffect(ProjectileSurfaceEffect.SandMound, 30f, 1.5f, 2f);
            rt.Tick(0.05f, Car(30f), 8f); Assert.AreEqual(8f, rt.Out.TargetSpeed, 0.01f);
            rt.SurfaceId = "surface-wooden-bridge"; Stand(rt, 40f, 0.2f);
            rt.AddProjectileEffect(ProjectileSurfaceEffect.BurnWood, 40f, 1.5f, 2f);
            Stand(rt, 40f, 3f); Assert.GreaterOrEqual(road.Cuts.Count, 1, "the burnt piece was removed");
        }

        [Test]
        public void R32_HookInstallsThroughTheSharedSlot_ChargePatchCutsTheDrive()
        {
            var road = new FakeRoad(); var rt = Rt(road, Set(S(SurfaceModuleKind.Fade)), Table());
            ProjectileSurfaceHooks.Current = new SurfaceProjectileHook(rt);
            ProjectileSurfaceHooks.Current.OnProjectileSurface(ProjectileSurfaceEffect.ChargeGrid, new Vector2(10f, 0f), 1.5f, 2f, "PRJ-NEON");
            rt.Tick(0.05f, Car(10f), 8f); Assert.IsTrue(rt.Out.DriveCut);
            ProjectileSurfaceHooks.Current = null; Assert.IsNotNull(ProjectileSurfaceHooks.Current);
        }

        [Test]
        public void R33_MonsterTable_SlowsStunsAndEatsFaster()
        {
            var road = new FakeRoad(); var rt = Rt(road, Set(S(SurfaceModuleKind.Charge, 2.5f, 1.2f, 0.6f, 0.5f)), Table()); rt.SurfaceId = "surface-electric-rail";
            Assert.AreEqual(0f, rt.MonsterAt(30f).StunS, 0f, "safe window: no stun");
            for (float t = 0f; t < 3f; t += 0.05f) rt.Tick(0.05f, Car(2f), 8f);
            Assert.Greater(rt.MonsterAt(2f).StunS, 0f);
            rt.SurfaceId = "surface-mud"; Assert.Less(rt.MonsterAt(2f).SlowMul, 1f);
            rt.SurfaceId = "surface-glass"; Assert.Greater(rt.MonsterAt(2f).EatMul, 1f);
            rt.SurfaceId = "surface-asphalt"; Assert.AreEqual(1f, rt.MonsterAt(2f).SlowMul, 0f);
        }

        // ------------------------------------------------------------------ compatibility, map, unlock
        [Test]
        public void R40_CompatibilityPairsFollowBlueprintC3()
        {
            var db = ScriptableObject.CreateInstance<SurfaceCompatibilityDatabase>(); db.pairs = SurfaceModuleBuilder.Pairs();
            var known = SurfaceModuleBuilder.KnownIds(); foreach (var t in SurfaceModuleBuilder.ModifierTokens) known.Add(t);
            var errs = new List<string>(); Assert.IsTrue(db.Validate(errs, known), string.Join("\n", errs));
            Assert.AreEqual(SurfaceCombo.AllowedWarn, db.Query("surface-oil", "mod-fire", out _)); Assert.IsTrue(db.TryRule("mod-fire", "surface-oil", out var r)); Assert.IsNotEmpty(r.warning);
            Assert.AreEqual(SurfaceCombo.Reduced, db.Query("surface-ice", "mod-wind", out _)); db.TryRule("surface-ice", "mod-wind", out r); Assert.Less(r.factor, 1f);
            Assert.AreEqual(SurfaceCombo.Merge, db.Query("surface-sea-water", "surface-lava-crust", out var m)); Assert.AreEqual("surface-obsidian", m);
            Assert.AreEqual(SurfaceCombo.AllowedWarn, db.Query("surface-glass", "mod-heavy", out _));
            Assert.AreEqual(SurfaceCombo.Forbidden, db.Query("surface-cloud", "mod-gravity-inversion", out _));
            Assert.AreEqual(SurfaceCombo.Merge, db.Query("surface-magnetic-rail", "surface-electric-rail", out m)); Assert.AreEqual("surface-electromagnetic", m);
            Assert.AreEqual(SurfaceCombo.Merge, db.Query("surface-rope-bridge", "mod-heavy", out m)); Assert.AreEqual("surface-rope-bridge-strong", m);
            Assert.AreEqual(SurfaceCombo.Forbidden, db.Query("surface-acid", "surface-lava-crust", out _));
            Assert.IsFalse(db.MayCombine("surface-acid", "surface-lava-river")); Assert.IsTrue(db.MayCombine("surface-oil", "mod-fire"));
        }

        private static readonly string[] Blueprint28 =
        {
            "surface-asphalt", "surface-ice", "surface-mud", "surface-lava-crust", "surface-glass", "surface-cloud", "surface-rope-bridge", "surface-neon-grid", "surface-sand", "surface-wooden-bridge",
            "surface-spring-road", "surface-magnetic-rail", "surface-gravel", "surface-snow", "surface-sea-water", "surface-acid", "surface-oil", "surface-cobblestone", "surface-conveyor", "surface-lava-river",
            "surface-sticky-candy", "surface-honey", "surface-metal-plates", "surface-ice-crystals", "surface-floating-islands", "surface-haunted-shadow", "surface-electric-rail", "surface-fragile-paper",
        };

        [Test]
        public void R41_All28BlueprintSurfacesAreMapped_ExceptTheStableBaseline_AndEveryMapValidates()
        {
            var maps = SurfaceModuleBuilder.Maps(); var ids = new HashSet<string>(); var errs = new List<string>();
            foreach (var m in maps)
            {
                ids.Add(m.id);
                var so = ScriptableObject.CreateInstance<SurfaceBehaviourSetSO>(); so.surfaceId = m.id; so.modules = m.specs; so.hintFull = m.hintFull; so.hintShort = m.hintShort; so.hintIcon = SurfaceModuleBuilder.IconIdOf(m.id);
                Assert.IsTrue(so.Validate(errs), string.Join("\n", errs));
                Assert.IsNotEmpty(m.verb, m.id); Assert.IsNotEmpty(m.consequence, m.id);
                Assert.IsTrue(System.IO.File.Exists(SurfaceModuleBuilder.IconDir + "/" + so.hintIcon + ".png"), "icon for " + m.id);
            }
            foreach (var id in Blueprint28) if (id != "surface-asphalt") Assert.IsTrue(ids.Contains(id), "unmapped " + id);
        }

        [Test]
        public void R42_UnlockCategories_AreOrderedAndCoverTheCatalogue()
        {
            var u = ScriptableObject.CreateInstance<SurfaceUnlockCategoriesSO>();
            var known = SurfaceModuleBuilder.KnownIds(); var errs = new List<string>();
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<SurfaceUnlockCategoriesSO>(SurfaceModuleBuilder.UnlockPath);
            Assert.IsNotNull(asset, "run Road Builder > Stage > Build Surface Modules");
            Assert.IsTrue(asset.Validate(errs, known), string.Join("\n", errs));
            Assert.AreEqual(5, asset.categories.Length); Assert.GreaterOrEqual(asset.IndexOf("surface-lava-crust"), 3);
        }

        // ------------------------------------------------------------------ the real road
        [Test]
        public void R50_RoadBuilder_CutRangeRemovesRoadAndSplitsTheChunk_ProtectedRangeIsReported()
        {
            var go = new GameObject("RoadTest"); var rb = go.AddComponent<RoadBuilder>(); rb.Init(new PhysicsMaterial2D());
            try
            {
                rb.ResetRoad(0f, 40f, 0f);
                Assert.IsTrue(rb.HasRoadAt(12f));
                Assert.IsTrue(rb.CutRange(10f, 14f));
                Assert.IsFalse(rb.HasRoadAt(12f)); Assert.IsTrue(rb.HasRoadAt(7f)); Assert.IsTrue(rb.HasRoadAt(18f));
                rb.AddNoEatRange(30f, 34f); Assert.IsTrue(rb.IsRangeProtected(31f, 32f)); Assert.IsFalse(rb.IsRangeProtected(20f, 22f));
                rb.SetOffsetY(0.2f); rb.SetOffsetY(0f);
                rb.TintRange(0f, 8f, Color.red);
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
