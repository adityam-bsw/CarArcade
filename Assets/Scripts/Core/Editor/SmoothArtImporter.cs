using UnityEditor;
using UnityEngine;

namespace RoadBuilderGame.Editor
{
    /// <summary>
    /// Import rule for the smooth art style, limited to Assets/Art/Smooth/. Pixel assets everywhere else are never
    /// touched. Defaults are applied only the first time a texture is imported (importSettingsMissing), so a per-file
    /// tweak (for example a gradient strip kept uncompressed, or a road atlas with mipmaps) survives re-imports.
    /// Binding table: Docs/design/ART_BIBLE.md 7.1. Sprite atlas padding (4 px) is set on the SpriteAtlas asset, not here.
    /// </summary>
    public sealed class SmoothArtImporter : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!SmoothArt.IsSmoothPath(assetPath) || !assetImporter.importSettingsMissing) return;
            Apply((TextureImporter)assetImporter, assetPath);
        }

        public static void Apply(TextureImporter imp, string path)
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            // Vehicles2 files carry a per-file ppu of 16 x S (S = old canvas multiplier, see its MAPPING.md). This rule runs only on
            // first import (importSettingsMissing), so those values are never reset; a brand-new Vehicles2 file needs its ppu set by the inspector.
            imp.spritePixelsPerUnit = SmoothArt.DefaultPpu(path);
            imp.filterMode = FilterMode.Bilinear;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            imp.maxTextureSize = SmoothArt.MaxTextureSize;
            imp.textureCompression = TextureImporterCompression.Compressed;

            var android = imp.GetPlatformTextureSettings("Android");
            android.overridden = true;
            android.maxTextureSize = SmoothArt.MaxTextureSize;
            android.format = AndroidFormat(path);
            imp.SetPlatformTextureSettings(android);
        }

        // ASTC 6x6 for backgrounds, scenery and surfaces; 5x5 for vehicles, hazards and UI plates.
        private static TextureImporterFormat AndroidFormat(string path)
        {
            string p = path.Replace('\\', '/');
            bool detail = p.StartsWith(SmoothArt.UiFolder, System.StringComparison.Ordinal)
                          || SmoothArt.IsVehicleFolder(p)
                          || p.StartsWith(SmoothArt.Root + "Hazards/", System.StringComparison.Ordinal);
            return detail ? TextureImporterFormat.ASTC_5x5 : TextureImporterFormat.ASTC_6x6;
        }
    }
}
