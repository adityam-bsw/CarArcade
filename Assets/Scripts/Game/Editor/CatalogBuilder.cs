using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace RoadBuilderGame.Editor
{
    /// <summary>
    /// Builds Assets/Resources/Catalog/AssetCatalog.asset from Assets/Art/Catalog/catalog.json, which
    /// tools/assets/make_previews.py writes from the asset manifest. Order of work after assets change:
    ///   1. python3 tools/assets/make_previews.py     2. Road Builder > Catalog > Build Catalog
    /// </summary>
    public static class CatalogBuilder
    {
        private const string JsonPath = "Assets/Art/Catalog/catalog.json";
        private const string AssetPath = "Assets/Resources/Catalog/AssetCatalog.asset";

        [System.Serializable] private class Row
        {
            public string id, kind, name, group, status, note, licence, preview;
            public int quality;
        }
        [System.Serializable] private class Rows { public List<Row> entries; }
        [System.Serializable] private class WheelRow { public float cx, cy, r; public string image; }
        [System.Serializable] private class WheelCar { public string id; public List<WheelRow> wheels; }
        [System.Serializable] private class WheelFile { public List<WheelCar> vehicles; }
        private const string WheelsJson = "Assets/Art/Catalog/wheels.json";

        [MenuItem("Road Builder/Catalog/Build Catalog")]
        public static void Build()
        {
            if (!File.Exists(JsonPath)) { Debug.LogError("[CatalogBuilder] Missing " + JsonPath + ". Run tools/assets/make_previews.py first."); return; }
            var rows = JsonUtility.FromJson<Rows>(File.ReadAllText(JsonPath));

            Directory.CreateDirectory(Path.GetDirectoryName(AssetPath));
            var catalog = AssetDatabase.LoadAssetAtPath<AssetCatalog>(AssetPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<AssetCatalog>();
                AssetDatabase.CreateAsset(catalog, AssetPath);
            }

            // Previews are pixel art: crisp, uncompressed, no mipmaps.
            foreach (var r in rows.entries) ConfigurePreview(r.preview);
            AssetDatabase.Refresh();

            catalog.entries.Clear();
            int missing = 0;
            foreach (var r in rows.entries)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(r.preview);
                if (sprite == null) { missing++; Debug.LogWarning("[CatalogBuilder] No sprite for " + r.id); continue; }
                catalog.entries.Add(new CatalogEntry
                {
                    id = r.id,
                    kind = r.kind == "vehicle" ? CatalogKind.Vehicle : (r.kind == "surface" ? CatalogKind.Surface : CatalogKind.World),
                    displayName = r.name,
                    group = r.group,
                    status = r.status == "have" ? CatalogStatus.Have : (r.status == "ready" ? CatalogStatus.Ready : CatalogStatus.Forged),
                    quality = r.quality,
                    note = r.note,
                    licence = r.licence,
                    preview = sprite
                });
            }
            foreach (var e in catalog.entries) FillPlayData(e);
            LinkWheels(catalog);
            SmoothVehicleLinker.Link(catalog);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log($"[CatalogBuilder] {catalog.entries.Count} entries ({missing} missing sprites) -> {AssetPath}");
        }

        // ---------------------------------------------------------------- play data (tuning lives in the asset)
        // class -> mass, torque per wheel, speed multiplier
        private static readonly Dictionary<string, float[]> Classes = new Dictionary<string, float[]>
        {
            { "compact", new[] { 1.3f, 36f, 1.05f } }, { "car", new[] { 1.45f, 34f, 1f } }, { "sport", new[] { 1.3f, 38f, 1.15f } },
            { "truck", new[] { 2.0f, 52f, 0.95f } }, { "heavy", new[] { 2.6f, 62f, 0.85f } }, { "bike", new[] { 0.9f, 26f, 1f } },
            { "offroad", new[] { 1.7f, 46f, 1f } },
        };
        // vehicle id -> class, coin cost
        private static readonly Dictionary<string, KeyValuePair<string, int>> Vehicles = new Dictionary<string, KeyValuePair<string, int>>
        {
            { "vehicle-car", P("car", 0) }, { "vehicle-scooter", P("bike", 0) }, { "vehicle-bicycle", P("bike", 0) },
            { "vehicle-golf-cart", P("compact", 0) }, { "vehicle-dune-buggy", P("offroad", 120) },
            { "vehicle-taxi", P("car", 150) }, { "vehicle-police-car", P("sport", 220) }, { "vehicle-tuk-tuk", P("compact", 150) },
            { "vehicle-sports-car", P("sport", 250) }, { "vehicle-race-car", P("sport", 400) },
            { "vehicle-motorcycle", P("bike", 200) }, { "vehicle-quad-atv", P("offroad", 260) },
            { "vehicle-pickup", P("truck", 300) }, { "vehicle-tractor", P("truck", 280) }, { "vehicle-camper", P("truck", 320) },
            { "vehicle-monster-truck", P("truck", 450) }, { "vehicle-moon-rover", P("offroad", 500) },
            { "vehicle-bus", P("heavy", 400) }, { "vehicle-fire-truck", P("heavy", 450) }, { "vehicle-semi-truck", P("heavy", 500) },
            { "vehicle-tank", P("heavy", 700) }, { "vehicle-handcar", P("compact", 180) }, { "vehicle-mine-cart", P("compact", 180) },
            { "vehicle-shopping-cart", P("bike", 100) },
            { "vehicle-skateboard", P("bike", 80) }, { "vehicle-snowmobile", P("offroad", 350) }, { "vehicle-sled", P("compact", 90) },
            { "vehicle-toboggan", P("compact", 110) }, { "vehicle-snowboard", P("bike", 130) },
        };
        // surface id -> friction, bounce, legacy RoadStyle (-1 = strip), speed factor, ink factor, coin factor
        // Every track trades something: grip vs speed vs ink cost vs coin value.
        private static readonly Dictionary<string, float[]> Surfaces = new Dictionary<string, float[]>
        {
            { "surface-asphalt",       new[] { 1.25f, 0f,    0f,  1.00f, 1.00f, 1.00f } },
            { "surface-dirt",          new[] { 1.00f, 0f,    1f,  0.97f, 0.90f, 1.10f } },
            { "surface-neon-grid",     new[] { 1.25f, 0f,    2f,  1.08f, 1.10f, 1.00f } },
            { "surface-cobblestone",   new[] { 1.10f, 0.05f, -1f, 0.96f, 1.00f, 1.10f } },
            { "surface-gravel",        new[] { 0.90f, 0f,    -1f, 0.95f, 0.85f, 1.15f } },
            { "surface-mud",           new[] { 1.60f, 0f,    -1f, 0.85f, 0.75f, 1.30f } },
            { "surface-rail-track",    new[] { 1.30f, 0.05f, -1f, 1.10f, 1.15f, 1.00f } },
            { "surface-glass",         new[] { 0.50f, 0f,    -1f, 1.12f, 1.30f, 1.40f } },
            { "surface-salt-flat",     new[] { 0.90f, 0f,    -1f, 1.06f, 1.00f, 1.10f } },
            { "surface-moon-dust",     new[] { 0.80f, 0f,    -1f, 1.00f, 0.90f, 1.20f } },
            { "surface-magma-bridge",  new[] { 1.20f, 0f,    -1f, 1.05f, 1.25f, 1.50f } },
            { "surface-sand",          new[] { 0.95f, 0f,    -1f, 0.90f, 0.80f, 1.20f } },
            { "surface-beach",         new[] { 1.00f, 0f,    -1f, 0.95f, 0.85f, 1.10f } },
            { "surface-snow",          new[] { 0.70f, 0f,    -1f, 1.00f, 0.90f, 1.20f } },
            { "surface-ice",           new[] { 0.35f, 0f,    -1f, 1.15f, 1.30f, 1.50f } },
            { "surface-ice-bridge",    new[] { 0.40f, 0f,    -1f, 1.12f, 1.30f, 1.50f } },
            { "surface-grass-trail",   new[] { 1.10f, 0f,    -1f, 1.00f, 0.90f, 1.05f } },
            { "surface-wooden-bridge", new[] { 1.15f, 0.05f, -1f, 1.02f, 1.20f, 1.15f } },
            { "surface-rope-bridge",   new[] { 1.00f, 0.12f, -1f, 1.00f, 1.30f, 1.30f } },
            { "surface-river",         new[] { 0.90f, 0f,    -1f, 0.92f, 0.90f, 1.15f } },
            { "surface-coral",         new[] { 1.10f, 0f,    -1f, 0.98f, 1.00f, 1.15f } },
            { "surface-cloud",         new[] { 0.60f, 0.15f, -1f, 1.05f, 1.20f, 1.30f } },
            { "surface-candy",         new[] { 0.80f, 0.10f, -1f, 1.00f, 1.00f, 1.25f } },
            { "surface-lava-crust",    new[] { 1.20f, 0f,    -1f, 1.05f, 1.30f, 1.50f } },
            { "surface-sea-water",     new[] { 0.70f, 0f,    -1f, 0.85f, 1.00f, 1.20f } },
            { "surface-floating-islands", new[] { 1.10f, 0f, -1f, 1.00f, 1.10f, 1.20f } },
        };
        private static KeyValuePair<string, int> P(string cls, int cost) { return new KeyValuePair<string, int>(cls, cost); }

        private static void FillPlayData(CatalogEntry e)
        {
            e.playable = false; e.playNote = ""; e.cost = 0; e.strip = null; e.roadStyle = -1; e.speedFactor = e.inkFactor = e.coinFactor = 1f;
            if (e.kind == CatalogKind.Vehicle)
            {
                if (Vehicles.TryGetValue(e.id, out var v))
                {
                    var c = Classes[v.Key];
                    e.playable = true; e.cost = v.Value;
                    e.mass = c[0]; e.torque = c[1]; e.speed = c[2];
                    e.widthUnits = Mathf.Clamp(e.preview.rect.width / 14f, 1.5f, 3.0f);
                }
                else e.playNote = "Needs " + NeedFor(e.id) + " physics, which the game does not have yet.";
            }
            else if (e.kind == CatalogKind.Surface)
            {
                if (Surfaces.TryGetValue(e.id, out var s))
                {
                    e.friction = s[0]; e.bounce = s[1]; e.speedFactor = s[3]; e.inkFactor = s[4]; e.coinFactor = s[5];
                    if (s[2] >= 0f) { e.roadStyle = (int)s[2]; e.playable = true; }
                    else
                    {
                        string path = "Assets/Art/Surfaces/Forged/" + e.id + "/" + e.id + ".png";
                        ConfigureStrip(path);
                        e.strip = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                        e.playable = e.strip != null;
                    }
                }
                else e.playNote = "Needs a road strip made from this art first.";
            }
            else FillWorld(e);
        }

        private static readonly Dictionary<string, string> VistaScene = new Dictionary<string, string>
        {
            { "world-rolling-hills", "downs" }, { "world-coast", "leeward" }, { "world-desert", "mesa" }, { "world-volcano", "kiln" },
            { "world-winter", "tundra" }, { "world-arctic", "cairn" }, { "world-jungle", "canopy" }, { "world-night-city", "sodium" },
            { "world-industrial", "flue" },
        };
        // Worlds that play with the original remix of their mood: Country, Desert, CyberNight.
        private static readonly Dictionary<string, int> RemixTheme = new Dictionary<string, int>
        {
            { "world-rolling-hills", 0 }, { "world-desert", 1 }, { "world-night-city", 2 },
        };

        private static void FillWorld(CatalogEntry e)
        {
            e.layers = null; e.themeIndex = -1;
            if (RemixTheme.TryGetValue(e.id, out int theme)) { e.themeIndex = theme; e.playable = true; return; }

            var sprites = new Sprite[4];
            string[] names = { "sky", "far", "mid", "near" };
            if (VistaScene.TryGetValue(e.id, out string scene))
            {
                var skin = AssetDatabase.LoadAssetAtPath<BackgroundSkin>("Assets/Resources/Backgrounds/" + scene + ".asset");
                if (skin != null) for (int i = 0; i < 4; i++) sprites[i] = skin.Layer(i);
            }
            else
            {
                string key = e.id.Substring("world-".Length);
                for (int i = 0; i < 4; i++)
                {
                    string path = "Assets/Art/Worlds/Forged/" + e.id + "/" + key + "_" + i + "_" + names[i] + ".png";
                    ConfigureWorldLayer(path);
                    sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                }
            }
            bool ok = true;
            foreach (var s in sprites) if (s == null) ok = false;
            if (ok) { e.layers = sprites; e.playable = true; }
            else e.playNote = "Needs its four scenery layers first.";
        }

        /// <summary>Attaches the wheel cut-outs written by tools/assets/find_wheels.py to their vehicle entries.</summary>
        private static void LinkWheels(AssetCatalog catalog)
        {
            if (!File.Exists(WheelsJson)) { Debug.LogWarning("[CatalogBuilder] No wheels.json: vehicles keep their wheels in the picture. Run tools/assets/find_wheels.py."); return; }
            var file = JsonUtility.FromJson<WheelFile>(File.ReadAllText(WheelsJson));
            foreach (var car in file.vehicles)
                foreach (var w in car.wheels) ConfigureWheel(w.image);
            AssetDatabase.Refresh();
            int linked = 0;
            foreach (var car in file.vehicles)
            {
                var e = catalog.Find(car.id);
                if (e == null) continue;
                var list = new List<WheelArt>();
                foreach (var w in car.wheels)
                {
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(w.image);
                    if (sprite != null) list.Add(new WheelArt { sprite = sprite, cx = w.cx, cy = w.cy, r = w.r });
                }
                e.wheels = list.ToArray();
                linked++;
            }
            Debug.Log("[CatalogBuilder] wheel cut-outs linked for " + linked + " vehicles");
        }

        private static void ConfigureWheel(string path)
        {
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) return;
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = 16f;
            imp.filterMode = FilterMode.Point;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.SaveAndReimport();
        }

        private static void ConfigureWorldLayer(string path)
        {
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) return;
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = 16f;
            imp.filterMode = FilterMode.Point;
            imp.wrapMode = TextureWrapMode.Repeat;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            var settings = new TextureImporterSettings();
            imp.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            imp.SetTextureSettings(settings);
            imp.SaveAndReimport();
        }

        private static string NeedFor(string id)
        {
            if (id.Contains("boat") || id.Contains("ski") || id.Contains("hover")) return "water";
            if (id.Contains("sled") || id.Contains("snow") || id.Contains("toboggan")) return "snow-runner";
            if (id.Contains("skate")) return "board";
            return "special";
        }

        private static void ConfigureStrip(string path)
        {
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) return;
            imp.textureType = TextureImporterType.Default;
            imp.filterMode = FilterMode.Point;
            imp.wrapModeU = TextureWrapMode.Repeat;
            imp.wrapModeV = TextureWrapMode.Clamp;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            imp.npotScale = TextureImporterNPOTScale.None;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.SaveAndReimport();
        }

        private static void ConfigurePreview(string path)
        {
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) return;
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = 16f;
            imp.filterMode = FilterMode.Point;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.SaveAndReimport();
        }
    }
}
