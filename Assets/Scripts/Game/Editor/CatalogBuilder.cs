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
        };
        // surface id -> friction, bounce, legacy RoadStyle (-1 = strip)
        private static readonly Dictionary<string, float[]> Surfaces = new Dictionary<string, float[]>
        {
            { "surface-asphalt", new[] { 1.25f, 0f, 0f } }, { "surface-dirt", new[] { 1.0f, 0f, 1f } }, { "surface-neon-grid", new[] { 1.25f, 0f, 2f } },
            { "surface-cobblestone", new[] { 1.1f, 0f, -1f } }, { "surface-gravel", new[] { 0.9f, 0f, -1f } },
            { "surface-mud", new[] { 1.6f, 0f, -1f } }, { "surface-rail-track", new[] { 1.3f, 0f, -1f } },
            { "surface-glass", new[] { 0.5f, 0f, -1f } }, { "surface-salt-flat", new[] { 0.9f, 0f, -1f } },
            { "surface-moon-dust", new[] { 0.8f, 0f, -1f } }, { "surface-magma-bridge", new[] { 1.2f, 0f, -1f } },
        };
        private static KeyValuePair<string, int> P(string cls, int cost) { return new KeyValuePair<string, int>(cls, cost); }

        private static void FillPlayData(CatalogEntry e)
        {
            e.playable = false; e.playNote = ""; e.cost = 0; e.strip = null; e.roadStyle = -1;
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
                    e.friction = s[0]; e.bounce = s[1];
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
            else e.playNote = "Worlds are remembered but not shown on the track yet.";
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
