using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace RoadBuilderGame.Tests
{
    /// <summary>
    /// Smooth-art switch: existing assets must stay on the pixel path (default), and textures placed under
    /// Assets/Art/Smooth/ must follow the smooth import table (ART_BIBLE 7.1): bilinear, no point sampling, not the pixel
    /// 16 ppu, ASTC on Android when overridden. Ledger, licence and no-scripts rules stay in AssetLedgerCoverageTests.
    /// </summary>
    public class SmoothArtImportTests
    {
        [Test]
        public void ArtStyle_DefaultsToPixel()
        {
            Assert.AreEqual(0, (int)ArtStyle.Pixel);
            Assert.AreEqual(ArtStyle.Pixel, ScriptableObject.CreateInstance<RoadSkin>().artStyle);
            Assert.AreEqual(ArtStyle.Pixel, ScriptableObject.CreateInstance<BackgroundSkin>().artStyle);
        }

        [Test]
        public void ExistingRoadAndBackgroundSkins_StayPixel()
        {
            foreach (var g in AssetDatabase.FindAssets("t:RoadSkin"))
            {
                var road = AssetDatabase.LoadAssetAtPath<RoadSkin>(AssetDatabase.GUIDToAssetPath(g));
                // Same rule as backgrounds: Smooth only with a strip from the smooth folder.
                if (road.artStyle == ArtStyle.Smooth)
                    Assert.IsTrue(road.atlas != null && SmoothArt.IsSmoothPath(AssetDatabase.GetAssetPath(road.atlas)), road.name + " is Smooth but its strip is not under " + SmoothArt.Root);
                else Assert.AreEqual(ArtStyle.Pixel, road.artStyle, road.name);
            }
            foreach (var g in AssetDatabase.FindAssets("t:BackgroundSkin"))
            {
                var skin = AssetDatabase.LoadAssetAtPath<BackgroundSkin>(AssetDatabase.GUIDToAssetPath(g));
                // A skin may be switched to Smooth only once its layers live in the smooth folder.
                if (skin.artStyle == ArtStyle.Smooth)
                    Assert.IsTrue(SmoothArt.IsSmoothPath(AssetDatabase.GetAssetPath(skin.sky)), skin.name + " is Smooth but its art is not under " + SmoothArt.Root);
            }
        }

        [Test]
        public void SmoothPathRules()
        {
            Assert.IsTrue(SmoothArt.IsSmoothPath("Assets/Art/Smooth/Vehicles/car.png"));
            Assert.IsFalse(SmoothArt.IsSmoothPath("Assets/Art/Vehicles/Forged/car.png"));
            Assert.AreEqual(128f, SmoothArt.DefaultPpu("Assets/Art/Smooth/Vehicles/car.png"));
            Assert.AreEqual(100f, SmoothArt.DefaultPpu("Assets/Art/Smooth/UI/plate.png"));
            Assert.AreEqual(576f / 14f, SmoothArt.DefaultPpu("Assets/Art/Smooth/Backgrounds/sky.png"), 1e-4f);
        }

        [Test]
        public void TexturesUnderSmoothFolder_FollowSmoothTable()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Art/Smooth")) { Assert.Pass("No smooth art imported yet."); }
            var problems = new List<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Art/Smooth" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var imp = AssetImporter.GetAtPath(path) as TextureImporter;
                if (imp == null) continue;
                if (imp.filterMode == FilterMode.Point) problems.Add(path + ": point filter");
                if (Mathf.Approximately(imp.spritePixelsPerUnit, 16f)) problems.Add(path + ": 16 ppu (pixel value)");
                if (imp.maxTextureSize > 4096) problems.Add(path + ": larger than 4096");
                var android = imp.GetPlatformTextureSettings("Android");
                if (android.overridden && (android.format < TextureImporterFormat.ASTC_4x4 && android.format != TextureImporterFormat.RGBA32 && android.format != TextureImporterFormat.Automatic))
                    problems.Add(path + ": Android format " + android.format);
            }
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        // ---- smooth Countryside wiring -------------------------------------------------------------

        [Test]
        public void AllThreeThemesAreSmooth()
        {
            foreach (EnvironmentTheme t in System.Enum.GetValues(typeof(EnvironmentTheme))) ThemeArtStyles.Override(t, null);
            Assert.AreEqual(ArtStyle.Smooth, ThemeArtStyles.For(EnvironmentTheme.Country));
            Assert.AreEqual(ArtStyle.Smooth, ThemeArtStyles.For(EnvironmentTheme.Desert));
            Assert.AreEqual(ArtStyle.Smooth, ThemeArtStyles.For(EnvironmentTheme.CyberNight));
        }

        [Test]
        public void EveryTheme_CanBeSwitchedBackToPixel_AndKeepsPixelSkins()
        {
            foreach (EnvironmentTheme t in System.Enum.GetValues(typeof(EnvironmentTheme)))
            {
                ThemeArtStyles.Override(t, ArtStyle.Pixel);
                try
                {
                    var r = BackgroundRemixer.Make(t, new System.Random(3), null);
                    Assert.AreEqual(ArtStyle.Pixel, r.artStyle, t.ToString());
                    foreach (var sp in r.sprites) Assert.IsFalse(SmoothArt.IsSmoothPath(AssetDatabase.GetAssetPath(sp)), t + " pixel override must not show smooth art");
                }
                finally { ThemeArtStyles.Override(t, null); }
            }
        }

        [Test]
        public void DefaultSmoothRoadPerWorld_HasAStrip()
        {
            Assert.AreEqual("surface-asphalt", ThemeArtStyles.DefaultSmoothRoad(EnvironmentTheme.Country));
            Assert.AreEqual("surface-sand", ThemeArtStyles.DefaultSmoothRoad(EnvironmentTheme.Desert));
            Assert.AreEqual("surface-neon-grid", ThemeArtStyles.DefaultSmoothRoad(EnvironmentTheme.CyberNight));
            foreach (EnvironmentTheme t in System.Enum.GetValues(typeof(EnvironmentTheme)))
                Assert.IsNotNull(RoadSkins.GetSmooth(ThemeArtStyles.DefaultSmoothRoad(t)), t.ToString());
        }

        [Test]
        public void SurfacesWithoutSmoothStrip_ResolveToNull_SoTheWorldDefaultIsUsed()
        {
            foreach (var id in new[] { "surface-lava-crust", "surface-sea-water", "surface-magma-bridge", "surface-river", "surface-candy", "surface-cloud", "surface-coral", "surface-beach", "surface-floating-islands", "surface-ice-bridge", "surface-rope-bridge", "surface-salt-flat" })
                Assert.IsNotNull(RoadSkins.GetSmooth(id), id + " has its own smooth strip");
            Assert.IsNull(RoadSkins.GetSmooth("surface-no-such-strip"));
            Assert.IsNull(RoadSkins.GetSmooth(null));
        }

        [Test]
        public void SmoothDesertAndNightCitySkins_HaveLayersDecorAndFill()
        {
            var desert = AssetDatabase.LoadAssetAtPath<BackgroundSkin>("Assets/Resources/Backgrounds/desert_smooth.asset");
            var night = AssetDatabase.LoadAssetAtPath<BackgroundSkin>("Assets/Resources/Backgrounds/nightcity_smooth.asset");
            foreach (var skin in new[] { desert, night })
            {
                Assert.IsNotNull(skin);
                Assert.AreEqual(ArtStyle.Smooth, skin.artStyle);
                for (int i = 0; i < BackgroundSkin.LayerCount; i++) Assert.IsTrue(SmoothArt.IsSmoothPath(AssetDatabase.GetAssetPath(skin.Layer(i))), skin.name + " layer " + i);
                CollectionAssert.AreEqual(new[] { 0f, 0.2f, 0.4f, 0.8f }, skin.scroll);
                Assert.IsNotNull(skin.sun, skin.name + " sun/moon");
                Assert.AreEqual(1f, skin.groundFill.a, skin.name + " ground fill sampled");
            }
            Assert.AreEqual(Mood.Warm, desert.mood);
            Assert.AreEqual(Mood.Dark, night.mood);
            Assert.IsNull(desert.overlay);
            Assert.IsNotNull(night.overlay, "stars overlay");
        }

        [Test]
        public void Countryside_DefaultsToNearV2_WithV1AndLightSelectable()
        {
            var skin = AssetDatabase.LoadAssetAtPath<BackgroundSkin>("Assets/Resources/Backgrounds/countryside_smooth.asset");
            StringAssert.Contains("near_v2_2048x864", AssetDatabase.GetAssetPath(skin.near));
            Assert.AreEqual(2, skin.nearAlternates.Length);
            StringAssert.EndsWith("bg_countryside_near_2048x864.png", AssetDatabase.GetAssetPath(skin.nearAlternates[0]));
            StringAssert.Contains("near_v2_light", AssetDatabase.GetAssetPath(skin.nearAlternates[1]));
            ThemeArtStyles.Override(EnvironmentTheme.Country, null);
            try
            {
                Assert.AreSame(skin.near, BackgroundRemixer.FromSkin(skin).sprites[3]);
                BackgroundRemixer.SetNearVariant(0);
                Assert.AreSame(skin.nearAlternates[0], BackgroundRemixer.FromSkin(skin).sprites[3]);
                BackgroundRemixer.SetNearVariant(1);
                Assert.AreSame(skin.nearAlternates[1], BackgroundRemixer.FromSkin(skin).sprites[3]);
            }
            finally { BackgroundRemixer.SetNearVariant(-1); }
            Assert.AreSame(skin.near, BackgroundRemixer.FromSkin(skin).sprites[3]);
        }

        [Test]
        public void NightCityRecipe_CarriesStarsOverlayAndMoon()
        {
            var r = BackgroundRemixer.Make(EnvironmentTheme.CyberNight, new System.Random(5), null);
            Assert.IsNotNull(r.overlay);
            Assert.IsNotNull(r.sun);
            Assert.AreEqual(ArtStyle.Smooth, r.artStyle);
        }

        [Test]
        public void ThemeSwitch_CanSelectPixelAgain()
        {
            ThemeArtStyles.Override(EnvironmentTheme.Country, ArtStyle.Pixel);
            try
            {
                Assert.AreEqual(ArtStyle.Pixel, ThemeArtStyles.For(EnvironmentTheme.Country));
                var r = BackgroundRemixer.Make(EnvironmentTheme.Country, new System.Random(1), null);
                Assert.AreEqual(ArtStyle.Pixel, r.artStyle);
            }
            finally { ThemeArtStyles.Override(EnvironmentTheme.Country, null); }
        }

        [Test]
        public void RemixPools_NeverMixStyles()
        {
            foreach (EnvironmentTheme theme in System.Enum.GetValues(typeof(EnvironmentTheme)))
            {
                ThemeArtStyles.Override(theme, null);
                BackgroundRecipe prev = null;
                for (int seed = 0; seed < 40; seed++)
                {
                    var r = BackgroundRemixer.Make(theme, new System.Random(seed), prev);
                    Assert.IsNotNull(r, theme.ToString());
                    Assert.AreEqual(ThemeArtStyles.For(theme), r.artStyle, theme + " seed " + seed);
                    for (int l = 0; l < BackgroundSkin.LayerCount; l++)
                    {
                        bool smoothArt = SmoothArt.IsSmoothPath(AssetDatabase.GetAssetPath(r.sprites[l]));
                        Assert.AreEqual(r.artStyle == ArtStyle.Smooth, smoothArt, theme + " layer " + l + " seed " + seed);
                    }
                    prev = r;
                }
            }
        }

        [Test]
        public void SmoothCountrysideSkin_HasAllLayersAndDecor()
        {
            var skin = AssetDatabase.LoadAssetAtPath<BackgroundSkin>("Assets/Resources/Backgrounds/countryside_smooth.asset");
            Assert.IsNotNull(skin);
            Assert.AreEqual(ArtStyle.Smooth, skin.artStyle);
            for (int i = 0; i < BackgroundSkin.LayerCount; i++) Assert.IsNotNull(skin.Layer(i), "layer " + i);
            CollectionAssert.AreEqual(new[] { 0f, 0.2f, 0.4f, 0.8f }, skin.scroll);
            Assert.IsNotNull(skin.sun);
            Assert.AreEqual(4, skin.clouds.Length);
            foreach (var c in skin.clouds) Assert.IsNotNull(c);
        }

        [Test]
        public void SmoothRoadSkins_AreStrips_WithLipPerSurface()
        {
            string[] ids = { "surface-asphalt", "surface-dirt", "surface-grass-trail", "surface-ice", "surface-mud", "surface-glass",
                "surface-sand", "surface-snow", "surface-gravel", "surface-cobblestone", "surface-moon-dust", "surface-neon-grid", "surface-wooden-bridge", "surface-rail-track" };
            var lips = new System.Collections.Generic.Dictionary<string, float> { { "surface-sand", 7f }, { "surface-snow", 8f }, { "surface-gravel", 4f }, { "surface-cobblestone", 4f },
                { "surface-moon-dust", 10f }, { "surface-neon-grid", 4f }, { "surface-wooden-bridge", 5f }, { "surface-rail-track", 2f } };
            foreach (var id in ids)
            {
                var skin = AssetDatabase.LoadAssetAtPath<RoadSkin>("Assets/Resources/Roads/Smooth/" + id + ".asset");
                Assert.IsNotNull(skin, id);
                Assert.AreEqual(ArtStyle.Smooth, skin.artStyle);
                Assert.AreEqual(1f / 128f, skin.texelWorld, 1e-6f, id);
                Assert.IsTrue(skin.SmoothStrip, id);
                Assert.Greater(skin.lipPx, 0f, id);
                if (lips.TryGetValue(id, out var lip)) Assert.AreEqual(lip, skin.lipPx, id + " lip row from the forge measurement");
            }
            var asphalt = AssetDatabase.LoadAssetAtPath<RoadSkin>("Assets/Resources/Roads/Smooth/surface-asphalt.asset");
            var grass = AssetDatabase.LoadAssetAtPath<RoadSkin>("Assets/Resources/Roads/Smooth/surface-grass-trail.asset");
            Assert.Greater(grass.lipPx, asphalt.lipPx, "grass sits lower in its strip than asphalt");
        }

        [Test]
        public void SmoothStrip_AlignsLipToColliderTop_AndMapsOneToOne()
        {
            var skin = AssetDatabase.LoadAssetAtPath<RoadSkin>("Assets/Resources/Roads/Smooth/surface-grass-trail.asset");
            var pts = new List<Vector2>();
            for (int i = 0; i <= 20; i++) pts.Add(new Vector2(i * 0.5f, 0f));
            var g = RoadGeometry.Compute(pts, 0, pts.Count - 1, 0.38f, 1.35f);
            var go = new GameObject("r", typeof(MeshFilter), typeof(MeshRenderer));
            try
            {
                var rr = go.AddComponent<RoadRenderer>();
                rr.Rebuild(g, skin);
                var mesh = go.GetComponent<MeshFilter>().sharedMesh;
                var v = mesh.vertices; var uv = mesh.uv;
                float lip = skin.lipPx / 128f;
                Assert.AreEqual(g.HalfThick + lip, v[0].y, 1e-4f, "art top is above the collider top by the lip");
                Assert.AreEqual(g.HalfThick, v[1].y, 1e-4f, "lip row sits on the collider top");
                Assert.AreEqual(1f - skin.lipPx / skin.atlas.height, uv[1].y, 1e-4f);
                Assert.AreEqual(0f, uv[3].y, 1e-6f, "strip bottom reaches the end of the skirt");
                // no row stretching: world drop per v unit equals strip height
                float worldDrop = v[1].y - v[3].y;
                float vDrop = uv[1].y - uv[3].y;
                Assert.AreEqual(skin.atlas.height / 128f * vDrop, worldDrop, 1e-3f);
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
