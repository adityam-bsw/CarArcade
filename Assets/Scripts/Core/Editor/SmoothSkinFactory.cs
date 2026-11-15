using System.IO;
using UnityEditor;
using UnityEngine;

namespace RoadBuilderGame.Editor
{
    /// <summary>
    /// Builds the smooth skins as NEW assets (the pixel skins are never touched): one RoadSkin per smooth road strip under
    /// Resources/Roads/Smooth, the Countryside BackgroundSkin under Resources/Backgrounds, and the ThemeArtStyles switch.
    /// Menu: Road Builder > Smooth > Build Countryside Skins. Safe to re-run (assets are updated in place, GUIDs kept).
    /// </summary>
    public static class SmoothSkinFactory
    {
        private const string RoadArt = "Assets/Art/Smooth/Roads/";
        private const string BgRoot = "Assets/Art/Smooth/Backgrounds/";
        private const string BgArt = BgRoot + "Countryside/";
        private const string RoadDir = "Assets/Resources/Roads/Smooth";
        private const string BgDir = "Assets/Resources/Backgrounds";
        private const string ThemeDir = "Assets/Resources/Themes";

        // surface id, strip file, lip row (px from the strip top where the driving surface sits), legacy style
        // Measured from the alpha of each strip (median first-opaque row per column): asphalt 4, dirt 7, ice 6, glass 8,
        // grass soil line 14 (blades reach row 4 and rise over the collider), mud 8 (bumps reach row 0).
        private static readonly (string id, string file, float lip, RoadStyle style, string name)[] Roads =
        {
            ("surface-asphalt",    "road_asphalt_1024x192.png", 4f,  RoadStyle.AsphaltHighway, "Asphalt (smooth)"),
            ("surface-dirt",       "road_dirt_1024x192.png",    7f,  RoadStyle.DirtTrack,      "Dirt (smooth)"),
            ("surface-grass-trail","road_grass_1024x192.png",   14f, RoadStyle.AsphaltHighway, "Grass trail (smooth)"),
            ("surface-ice",        "road_ice_1024x192.png",     6f,  RoadStyle.AsphaltHighway, "Ice (smooth)"),
            ("surface-mud",        "road_mud_1024x192.png",     8f,  RoadStyle.AsphaltHighway, "Mud (smooth)"),
            ("surface-glass",      "road_glass_1024x192.png",   8f,  RoadStyle.AsphaltHighway, "Glass (smooth)"),
            // Batch 2 (lip rows measured by the forge, Assets/Art/Smooth/Roads/FORGE_NOTES_batch2.txt)
            ("surface-sand",        "road_sand_1024x192.png",         7f,  RoadStyle.DirtTrack,      "Sand (smooth)"),
            ("surface-snow",        "road_snow_1024x192.png",         8f,  RoadStyle.AsphaltHighway, "Snow (smooth)"),
            ("surface-gravel",      "road_gravel_1024x192.png",       4f,  RoadStyle.DirtTrack,      "Gravel (smooth)"),
            ("surface-cobblestone", "road_cobblestone_1024x192.png",  4f,  RoadStyle.AsphaltHighway, "Cobblestone (smooth)"),
            ("surface-moon-dust",   "road_moon_dust_1024x192.png",    10f, RoadStyle.AsphaltHighway, "Moon dust (smooth)"),
            ("surface-neon-grid",   "road_neon_grid_1024x192.png",    4f,  RoadStyle.NeonGrid,       "Neon grid (smooth)"),
            ("surface-wooden-bridge","road_wooden_bridge_1024x192.png",5f, RoadStyle.AsphaltHighway, "Wooden bridge (smooth)"),
            ("surface-rail-track",  "road_rail_track_1024x192.png",   2f,  RoadStyle.AsphaltHighway, "Rail track (smooth)"),
        };

        /// <summary>Builds every smooth skin: Countryside, Desert, Night City backgrounds, all 14 road strips, the theme switch.</summary>
        [MenuItem("Road Builder/Smooth/Build Desert and Night City Skins")]
        public static void BuildAll() { Build(); }

        [MenuItem("Road Builder/Smooth/Build Countryside Skins")]
        public static void Build()
        {
            BuildRoads();
            BuildBackground();
            BuildDesertBackground();
            BuildNightCityBackground();
            BuildThemeSwitch();
            BuildSliceArt();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[SmoothSkinFactory] Smooth skins built (Countryside, Desert, Night City, " + Roads.Length + " roads).");
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (a == null) { a = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(a, path); }
            return a;
        }

        private static void BuildRoads()
        {
            Directory.CreateDirectory(RoadDir);
            foreach (var r in Roads)
            {
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(RoadArt + r.file);
                if (tex == null) { Debug.LogError("[SmoothSkinFactory] missing " + RoadArt + r.file); continue; }
                var skin = LoadOrCreate<RoadSkin>($"{RoadDir}/{r.id}.asset");
                skin.style = r.style;
                skin.displayName = r.name;
                skin.surfaceId = r.id;
                skin.artStyle = ArtStyle.Smooth;
                skin.atlas = tex;
                skin.texelWorld = 1f / SmoothArt.GameplayPpu;
                skin.lipPx = r.lip;
                skin.tint = Color.white;
                EditorUtility.SetDirty(skin);
            }
        }

        private static Sprite S(string file) { return AssetDatabase.LoadAssetAtPath<Sprite>(BgArt + file); }

        private static Sprite Sp(string path) { return AssetDatabase.LoadAssetAtPath<Sprite>(path); }

        // Ground colour under the near layer: the bottom-left pixel of the art (the texture is not readable at runtime).
        private static Color SampleGround(string nearPath)
        {
            var tmp = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tmp.LoadImage(File.ReadAllBytes(nearPath));
            var c = tmp.GetPixel(0, 0); c.a = 1f;
            Object.DestroyImmediate(tmp);
            return c;
        }

        private static void BuildBackground()
        {
            Directory.CreateDirectory(BgDir);
            var skin = LoadOrCreate<BackgroundSkin>(BgDir + "/countryside_smooth.asset");
            skin.displayName = "Countryside (Smooth)";
            skin.mood = Mood.Green;
            skin.artStyle = ArtStyle.Smooth;
            skin.sky = S("bg_countryside_sky_4x576.png");
            skin.far = S("bg_countryside_far_2048x576.png");
            skin.mid = S("bg_countryside_mid_2048x576.png");
            // near_v2 (fences, boulders, flowers) is the default; v1 and the light alternate stay selectable (BackgroundRemixer.SetNearVariant 0 / 1).
            skin.near = S("bg_countryside_near_v2_2048x864.png");
            skin.nearAlternates = new[] { S("bg_countryside_near_2048x864.png"), S("bg_countryside_near_v2_light_2048x864.png") };
            skin.scroll = new[] { 0f, 0.2f, 0.4f, 0.8f };   // ART_BIBLE section 5
            skin.sun = S("bg_countryside_sun_256.png");
            skin.clouds = new[] { S("bg_countryside_cloud_a.png"), S("bg_countryside_cloud_b.png"), S("bg_countryside_cloud_c.png"), S("bg_countryside_cloud_d.png") };
            skin.cloudScroll = 0.1f;
            skin.groundFill = SampleGround(BgArt + "bg_countryside_near_v2_2048x864.png");
            EditorUtility.SetDirty(skin);
        }

        private static void BuildDesertBackground()
        {
            Directory.CreateDirectory(BgDir);
            string d = BgRoot + "Desert/";
            var skin = LoadOrCreate<BackgroundSkin>(BgDir + "/desert_smooth.asset");
            skin.displayName = "Desert (Smooth)";
            skin.mood = Mood.Warm;
            skin.artStyle = ArtStyle.Smooth;
            skin.sky = Sp(d + "bg_desert_sky_4x576.png");
            skin.far = Sp(d + "bg_desert_far_2048x576.png");
            skin.mid = Sp(d + "bg_desert_mid_2048x576.png");
            skin.near = Sp(d + "bg_desert_near_2048x864.png");
            skin.nearAlternates = new Sprite[0];
            skin.scroll = new[] { 0f, 0.2f, 0.4f, 0.8f };
            skin.sun = Sp(d + "bg_desert_sun_256.png");
            skin.overlay = null;
            skin.clouds = new Sprite[0];
            skin.groundFill = SampleGround(d + "bg_desert_near_2048x864.png");
            EditorUtility.SetDirty(skin);
        }

        private static void BuildNightCityBackground()
        {
            Directory.CreateDirectory(BgDir);
            string d = BgRoot + "NightCity/";
            var skin = LoadOrCreate<BackgroundSkin>(BgDir + "/nightcity_smooth.asset");
            skin.displayName = "Night City (Smooth)";
            skin.mood = Mood.Dark;
            skin.artStyle = ArtStyle.Smooth;
            skin.sky = Sp(d + "bg_nightcity_sky_4x576.png");
            skin.far = Sp(d + "bg_nightcity_far_2048x576.png");
            skin.mid = Sp(d + "bg_nightcity_mid_2048x576.png");
            skin.near = Sp(d + "bg_nightcity_near_2048x864.png");
            skin.nearAlternates = new Sprite[0];
            skin.scroll = new[] { 0f, 0.2f, 0.4f, 0.8f };
            skin.sun = Sp(d + "bg_nightcity_moon_320.png");        // the moon uses the sun slot
            skin.overlay = Sp(d + "bg_nightcity_stars_2048x576.png");
            skin.clouds = new Sprite[0];
            skin.groundFill = SampleGround(d + "bg_nightcity_near_2048x864.png");
            EditorUtility.SetDirty(skin);
        }

        /// <summary>Resources/Smooth/SmoothSliceArt: references to the smooth slice vehicle and pickup sprites (no copies).</summary>
        public static void BuildSliceArt()
        {
            Directory.CreateDirectory("Assets/Resources/Smooth");
            var a = LoadOrCreate<SmoothSliceArt>("Assets/Resources/Smooth/SmoothSliceArt.asset");
            Sprite V(string f) { return AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Smooth/Vehicles/" + f); }
            Sprite P(string f) { return AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Smooth/Pickups/" + f); }
            a.bodyClassic = V("body_classic.png"); a.bodySport = V("body_sport.png"); a.bodyMonster = V("body_monster.png");
            a.wheelClassic = V("wheel_classic.png"); a.wheelSport = V("wheel_sport.png"); a.wheelMonster = V("wheel_monster.png");
            a.coin = P("Coin.png"); a.heart = P("Heart.png"); a.ink = P("InkCanister.png");
            a.ring = P("Pickup_Ring.png"); a.ringOverload = P("Pickup_Ring_Overload.png"); a.star = P("Star.png");
            EditorUtility.SetDirty(a);
        }

        private static void BuildThemeSwitch()
        {
            Directory.CreateDirectory(ThemeDir);
            var t = LoadOrCreate<ThemeArtStyles>(ThemeDir + "/ThemeArtStyles.asset");
            t.country = ArtStyle.Smooth;
            t.desert = ArtStyle.Smooth;
            t.cyberNight = ArtStyle.Smooth;
            t.countryRoad = "surface-asphalt";
            t.desertRoad = "surface-sand";
            t.cyberNightRoad = "surface-neon-grid";
            EditorUtility.SetDirty(t);
        }
    }
}
