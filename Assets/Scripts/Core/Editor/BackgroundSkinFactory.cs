using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace RoadBuilderGame.Editor
{
    /// <summary>
    /// Turns each VISTA scene folder into a BackgroundSkin asset under Resources/Backgrounds, reading the scroll
    /// speeds from the scene's own JSON, and sets the texture import settings tiled sprites need.
    /// Menu: Road Builder > Backgrounds > Build Skins From VISTA. Safe to re-run.
    /// </summary>
    public static class BackgroundSkinFactory
    {
        private const string SourceDir = "Assets/Art/Backgrounds/Vista";
        private const string SkinDir = "Assets/Resources/Backgrounds";

        // Which mood each scene belongs to (decides what it may be mixed with).
        private static readonly Dictionary<string, (string name, Mood mood)> Scenes = new Dictionary<string, (string, Mood)>
        {
            { "downs",   ("Rolling Hills", Mood.Green) },
            { "canopy",  ("Rainforest",    Mood.Green) },
            { "leeward", ("Coastal Cliffs", Mood.Green) },
            { "mesa",    ("Desert Badlands", Mood.Warm) },
            { "kiln",    ("Volcanic",      Mood.Warm) },
            { "cairn",   ("Alpine Peaks",  Mood.Cool) },
            { "hollow",  ("Misty Valley",  Mood.Cool) },
            { "tundra",  ("Frozen Tundra", Mood.Cool) },
            { "sodium",  ("Night City",    Mood.Dark) },
            { "flue",    ("Industrial Works", Mood.Dark) },
        };

        [System.Serializable] private class SceneJson { public List<LayerJson> layers; }
        [System.Serializable] private class LayerJson { public string name; public float scroll; }

        [MenuItem("Road Builder/Backgrounds/Build Skins From VISTA")]
        public static void Build()
        {
            Directory.CreateDirectory(SkinDir);
            int made = 0;
            foreach (var kv in Scenes)
            {
                string id = kv.Key;
                ConfigureTextures(id);

                var skinPath = SkinDir + "/" + id + ".asset";
                var skin = AssetDatabase.LoadAssetAtPath<BackgroundSkin>(skinPath);
                if (skin == null)
                {
                    skin = ScriptableObject.CreateInstance<BackgroundSkin>();
                    AssetDatabase.CreateAsset(skin, skinPath);
                }

                skin.displayName = kv.Value.name;
                skin.mood = kv.Value.mood;
                skin.sky = Load(id, "0_sky");
                skin.far = Load(id, "1_far");
                skin.mid = Load(id, "2_mid");
                skin.near = Load(id, "3_near");

                var json = JsonUtility.FromJson<SceneJson>(File.ReadAllText($"{SourceDir}/{id}/{id}.json"));
                if (json != null && json.layers != null && json.layers.Count == BackgroundSkin.LayerCount)
                {
                    skin.scroll = new float[BackgroundSkin.LayerCount];
                    for (int i = 0; i < BackgroundSkin.LayerCount; i++) skin.scroll[i] = json.layers[i].scroll;
                }

                EditorUtility.SetDirty(skin);
                made++;
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[BackgroundSkinFactory] Skins ready: " + made);
        }

        // Prefers a cleaned replacement of the sky if one has been generated (see CleanDomeSkies).
        private static Sprite Load(string id, string layer)
        {
            if (layer == "0_sky")
            {
                var clean = AssetDatabase.LoadAssetAtPath<Sprite>($"{SourceDir}/{id}/{id}_0_sky_clean.png");
                if (clean != null) return clean;
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>($"{SourceDir}/{id}/{id}_{layer}.png");
        }

        // Scenes whose sky has a row of identical dome clouds that read as an obvious repeat.
        private static readonly string[] DomeSkyScenes = { "downs", "leeward" }; // order matters: donors first
        private static readonly Dictionary<string, string> SharedSky = new Dictionary<string, string> { { "leeward", "downs" } };

        /// <summary>
        /// Replaces a blemished sky with a clean one made by tiling its most dome-free 16 px vertical strip across the
        /// frame. The dithered gradient and haze bands are kept; the repeating cloud blobs are not. The original file
        /// is left alone and the result is saved next to it as *_0_sky_clean.png (a CC0 derivative).
        /// Menu: Road Builder > Backgrounds > Clean Dome Skies.
        /// </summary>
        [MenuItem("Road Builder/Backgrounds/Clean Dome Skies")]
        public static void CleanDomeSkies()
        {
            foreach (var id in DomeSkyScenes)
            {
                string src = $"{SourceDir}/{id}/{id}_0_sky.png";
                string dst = $"{SourceDir}/{id}/{id}_0_sky_clean.png";

                // Leeward's domes sit on a full-width cloud band, so no strip is clean: it reuses the Downs sky instead.
                if (SharedSky.TryGetValue(id, out var donor))
                {
                    File.Copy($"{SourceDir}/{donor}/{donor}_0_sky_clean.png", dst, true);
                    ImportPixelSprite(dst);
                    Debug.Log($"[BackgroundSkinFactory] {id}: reuses the clean sky of {donor}");
                    continue;
                }

                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                tex.LoadImage(File.ReadAllBytes(src));
                int w = tex.width, h = tex.height;
                var px = tex.GetPixels32();

                // A cloud edge makes the colour jump sharply from one row to the next; a plain sky gradient does not.
                // So the cleanest strip is the one with the least vertical jumping, summed over all its columns.
                const int strip = 8;
                int bestX = 0; long bestScore = long.MaxValue;
                for (int x0 = 0; x0 + strip <= w; x0 += strip)
                {
                    long score = 0;
                    for (int x = x0; x < x0 + strip; x++)
                        for (int y = 1; y < h; y++)
                        {
                            var a = px[(y - 1) * w + x]; var b = px[y * w + x];
                            score += Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b);
                        }
                    if (score < bestScore) { bestScore = score; bestX = x0; }
                }

                var outPx = new Color32[w * h];
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                        outPx[y * w + x] = px[y * w + bestX + (x % strip)];

                var outTex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                outTex.SetPixels32(outPx);
                outTex.Apply();
                File.WriteAllBytes(dst, outTex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                Object.DestroyImmediate(outTex);

                ImportPixelSprite(dst);
                Debug.Log($"[BackgroundSkinFactory] {id}: clean sky from strip at x={bestX}");
            }
            Build(); // re-point the skins at the clean skies
        }

        private static void ImportPixelSprite(string path)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = 16f;
            imp.filterMode = FilterMode.Point;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            imp.wrapModeU = TextureWrapMode.Repeat;
            imp.wrapModeV = TextureWrapMode.Clamp;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.maxTextureSize = 512;
            var settings = new TextureImporterSettings();
            imp.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            imp.SetTextureSettings(settings);
            imp.SaveAndReimport();
        }

        private static byte Median(byte[] v)
        {
            var copy = (byte[])v.Clone();
            System.Array.Sort(copy);
            return copy[copy.Length / 2];
        }

        // Tiled draw mode needs full-rect sprite meshes. The near layer is read once for its ground colour.
        private static void ConfigureTextures(string id)
        {
            foreach (var layer in new[] { "0_sky", "1_far", "2_mid", "3_near" })
            {
                string path = $"{SourceDir}/{id}/{id}_{layer}.png";
                var imp = AssetImporter.GetAtPath(path) as TextureImporter;
                if (imp == null) continue;

                var settings = new TextureImporterSettings();
                imp.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                imp.SetTextureSettings(settings);
                imp.isReadable = layer == "3_near";
                imp.SaveAndReimport();
            }
        }
    }
}
