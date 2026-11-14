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
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log($"[CatalogBuilder] {catalog.entries.Count} entries ({missing} missing sprites) -> {AssetPath}");
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
