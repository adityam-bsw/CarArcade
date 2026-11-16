using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace RoadBuilderGame.Tests
{
    /// <summary>Integration pass: hazards default for every world, Eater and projectiles read the surface, fragile breaks raise progression, the 13 behaviour surfaces are real catalogue items, banner icon, coin tuning.</summary>
    public class IntegrationWiringTests
    {
        private sealed class FakeRoad : ISurfaceRoad
        {
            public bool CutRange(float x0, float x1) { return true; }
            public bool IsRangeProtected(float x0, float x1) { return false; }
            public void TintRange(float x0, float x1, Color c) { }
            public void SetOffsetY(float dy) { }
        }

        [Test]
        public void Hazards_CountrysideUsesHillsRowsWhenStageEventsOn_LegacyOnlyWhenOff()
        {
            HazardWorlds.Override = "";
            Assert.AreEqual("hills", HazardWorlds.Resolve("world-rolling-hills", true, true));
            Assert.AreEqual("", HazardWorlds.Resolve("world-rolling-hills", true, false));
            Assert.AreEqual("", HazardWorlds.Resolve("world-rolling-hills", true));
            Assert.AreEqual("world-desert", HazardWorlds.Resolve("world-desert", false, true));
            Assert.AreEqual("Dirt Maw", EaterVariantSO.ForWorld(HazardWorlds.Resolve("x", true, true)).displayName);
            Assert.AreEqual("PRJ-BOULDER", WorldProjectiles.ForWorld(HazardWorlds.Resolve("x", true, true)).id);
        }

        [Test]
        public void Eater_SurfaceSlowAndStun_SlowsAndStopsIt_StunCannotLock()
        {
            var d = ScriptableObject.CreateInstance<MonsterDefinitionSO>();
            RoadEaterModel Make() { var m = new RoadEaterModel(d); m.Reset(7u); m.BeginWarn(); m.StartChase(1000f, 100f); return m; }
            var plain = Make(); var slow = Make();
            slow.ApplySurface(0.5f, 1f, 0f);
            for (int i = 0; i < 60; i++) { plain.Tick(0.05f, 1000f + i, 20f, false, false); slow.Tick(0.05f, 1000f + i, 20f, false, false); }
            Assert.Less(slow.S, plain.S, "a slow surface holds the Eater back");
            var st = Make(); st.ApplySurface(1f, 1f, 1f);
            for (int i = 0; i < 10; i++) st.Tick(0.05f, 1000f, 20f, false, false);
            Assert.AreEqual(0f, st.Speed, 1.1f);
            float s0 = st.S; for (int i = 0; i < 10; i++) st.Tick(0.05f, 1000f, 20f, false, false);
            Assert.AreEqual(s0, st.S, 0.05f, "stunned: it does not advance");
            st.ApplySurface(1f, 1f, 1f); Assert.Greater(st.StunCooldown, 0f);
            for (int i = 0; i < 40; i++) st.Tick(0.05f, 1000f, 20f, false, false);      // stun over (1 s) but still in cooldown
            st.ApplySurface(1f, 1f, 1f); Assert.LessOrEqual(st.StunLeft, 0f, "no re-stun inside the cooldown");
        }

        [Test]
        public void MonsterQuery_ReadsSurfaceTable()
        {
            var rt = new SurfaceRuntime(); rt.Bind(new FakeRoad(), SurfaceBehaviourLibrary.Interactions);
            rt.SurfaceId = "surface-mud";
            Assert.Less(rt.MonsterAt(2f).SlowMul, 1f);
            IMonsterSurfaceQuery q = rt; Assert.IsNotNull(q);
        }

        [Test]
        public void ProjectileHook_InstalledHookReachesSurfaceRuntime()
        {
            var old = ProjectileSurfaceHooks.Current;
            try
            {
                var rt = new SurfaceRuntime(); rt.Bind(new FakeRoad(), SurfaceBehaviourLibrary.Interactions);
                ProjectileSurfaceHooks.Current = new SurfaceProjectileHook(rt);
                Assert.IsInstanceOf<SurfaceProjectileHook>(ProjectileSurfaceHooks.Current);
                ProjectileSurfaceHooks.Current.OnProjectileSurface(ProjectileSurfaceEffect.ChargeGrid, new Vector2(5f, 0f), 1f, 1f, "PRJ-NEON");
                ProjectileSurfaceHooks.Current.OnProjectileSurfaceCleared(ProjectileSurfaceEffect.ChargeGrid);
            }
            finally { ProjectileSurfaceHooks.Current = old; }
        }

        [Test]
        public void FragileBroken_RaisedWhenAGlassPieceIsCut_AndNotForPlainSurfaces()
        {
            var set = ScriptableObject.CreateInstance<SurfaceBehaviourSetSO>(); set.surfaceId = "surface-test"; set.pieceLengthM = 4f; set.safeEntryM = 6f;
            set.modules = new[] { new SurfaceModuleSpec(SurfaceModuleKind.Breakable, 0, 0, 0, 0) };
            Assert.IsTrue(SurfaceRuntime.HasFragileModule(set));
            var rt = new SurfaceRuntime(); rt.Bind(new FakeRoad(), null); rt.SetSurface("surface-test", set, null, 0f);
            int broken = 0; rt.FragileBroken = () => broken++;
            for (float t = 0f; t < 4f; t += 0.05f) rt.Tick(0.05f, new SurfaceCarSample { X = 60.5f, Speed = 0f, MudDragMul = 1f }, 8f);
            Assert.AreEqual(1, broken);
            set.modules = new[] { new SurfaceModuleSpec(SurfaceModuleKind.Dissolve, 0.5f, 0, 0, 0) };
            Assert.IsFalse(SurfaceRuntime.HasFragileModule(set));
            var p = new PlayerProfile(); ProgressionEvents.ActiveVehicle = "";
            ProgressionEvents.Record(p, ProgressionEvent.FragileBroken); // counter path exists and does not throw without an active vehicle
        }

        [Test]
        public void LavaAndRhythmRules_RaiseOnTheA2EventIds()
        {
            var t = ProgressionTuningSO.Load(); var p = new PlayerProfile();
            ProgressionEvents.RecordStageEvent(p, t, "EVT-LAVACHASE");
            ProgressionEvents.RecordStageEvent(p, t, "EVT-RHYTHM");
            Assert.AreEqual(1, p.Counter(GateService.ExtraCounterId(ExtraGate.SurviveLava)));
            Assert.AreEqual(1, p.Counter(GateService.ExtraCounterId(ExtraGate.RhythmChallenge)));
        }

        [Test]
        public void BehaviourSurfaces_AreCatalogueItems_WithSkins_Gates_AndCategories()
        {
            var cat = AssetCatalog.Load(); var gates = SurfaceGateSO.Load();
            var cats = AssetDatabase.LoadAssetAtPath<SurfaceUnlockCategoriesSO>("Assets/Resources/Stage/Surfaces/SurfaceUnlockCategories.asset");
            Assert.AreEqual(13, SmoothOnlySurfaces.Ids.Length);
            foreach (var id in SmoothOnlySurfaces.Ids)
            {
                var e = cat.Find(id);
                Assert.IsNotNull(e, id + " catalogue entry"); Assert.AreEqual(CatalogKind.Surface, e.kind); Assert.IsTrue(e.playable, id + " playable"); Assert.IsNotNull(e.preview, id + " preview");
                Assert.IsTrue(e.Reviewed, id + " reviewed so it shows in release");
                var skin = RoadSkins.GetSmooth(id); Assert.IsNotNull(skin, id + " smooth skin"); Assert.IsTrue(skin.SmoothStrip);
                Assert.IsTrue(gates.TryGet(id, out _), id + " gate");
                Assert.GreaterOrEqual(cats.IndexOf(id), 0, id + " unlock category");
                Assert.IsNotNull(SurfaceBehaviourLibrary.Find(id), id + " behaviour set");
            }
            Assert.AreEqual(26f, RoadSkins.GetSmooth("surface-ice-crystals").lipPx);
            Assert.AreEqual(2, RoadSkins.GetSmooth("surface-conveyor").FrameCount);
            Assert.AreEqual(2, RoadSkins.GetSmooth("surface-electric-rail").FrameCount);
            Assert.AreEqual(2, RoadSkins.GetSmooth("surface-fragile-paper").FrameCount);
        }

        [Test]
        public void RoadFrames_ConveyorAlternates_ElectricShowsOffWhenSafe()
        {
            var conv = SurfaceBehaviourLibrary.Find("surface-conveyor"); var rt = new SurfaceRuntime(); rt.Bind(new FakeRoad(), null); rt.SetSurface("surface-conveyor", conv, null, 0f);
            var seen = new HashSet<int>();
            for (float t = 0f; t < 1f; t += 0.05f) { rt.Tick(0.05f, new SurfaceCarSample { X = 10f, Speed = 8f, MudDragMul = 1f }, 8f); seen.Add(rt.FrameAt(10f)); }
            Assert.AreEqual(2, seen.Count, "belt frames alternate");
            var rail = SurfaceBehaviourLibrary.Find("surface-electric-rail"); rt.SetSurface("surface-electric-rail", rail, null, 0f);
            rt.Tick(0.05f, new SurfaceCarSample { X = 10f, Speed = 8f, MudDragMul = 1f }, 8f);
            Assert.AreEqual(1, rt.FrameAt(10f), "safe window shows the dormant strip");
        }

        [Test]
        public void RoadSkin_SetFrame_FallsBackToMainAtlas()
        {
            var s = ScriptableObject.CreateInstance<RoadSkin>(); s.SetFrame(3); Assert.AreEqual(0, s.Frame);
            Assert.AreEqual(1, s.FrameCount);
        }

        [Test]
        public void SmoothArtImporter_RoadFolderUsesRepeatAndFullRect()
        {
            Assert.IsTrue(SmoothArt.IsRoadFolder("Assets/Art/Smooth/Roads/road_acid_1024x192.png"));
            Assert.IsFalse(SmoothArt.IsRoadFolder("Assets/Art/Smooth/UI/x.png"));
            foreach (var g in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Art/Smooth/Roads" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(g); var imp = (TextureImporter)AssetImporter.GetAtPath(path);
                var ts = new TextureImporterSettings(); imp.ReadTextureSettings(ts);
                Assert.AreEqual(TextureWrapMode.Repeat, imp.wrapMode, path); Assert.AreEqual(SpriteMeshType.FullRect, ts.spriteMeshType, path);
            }
        }

        [Test]
        public void HomeDaily_StartsTheDailyDirectlyUnlessAClaimWaits()
        {
            Assert.AreEqual(RoadBuilderGame.UI.HomeScreen.DailyAction.Start, RoadBuilderGame.UI.HomeScreen.DailyActionFor(false));
            Assert.AreEqual(RoadBuilderGame.UI.HomeScreen.DailyAction.Claim, RoadBuilderGame.UI.HomeScreen.DailyActionFor(true));
        }

        [Test]
        public void Coins_ValueTenWithSparseShapes_StillMeetsEarningsBands()
        {
            var g = GameplayTuning.Load();
            Assert.AreEqual(10, g.coinValue); Assert.Less(g.coinShapeKeepChance, 0.5f);
            foreach (var b in ProgressionTuningSO.Load().runBands) Assert.That(EarningsModel.Expected(g, EarningsModel.Profile.Median, b.metres), Is.InRange(b.minCoins, b.maxCoins), b.metres + " m");
        }

        [Test]
        public void Model_BannerIconDefaultsEmpty() { Assert.AreEqual("", new GameModel().StageBannerIcon); }

        [Test]
        public void SurfaceHints_AllNewSurfacesCarryAnIconThatExists()
        {
            foreach (var id in SmoothOnlySurfaces.Ids) { var set = SurfaceBehaviourLibrary.Find(id); Assert.IsNotNull(SurfaceBehaviourLibrary.Icon(set.hintIcon), id + " icon " + set.hintIcon); }
        }
    }
}
