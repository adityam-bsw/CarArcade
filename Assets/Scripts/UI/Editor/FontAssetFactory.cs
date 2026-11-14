using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace RoadBuilderGame.UI.Editor
{
    /// <summary>
    /// Builds the TMP SDF font assets for the game's two typefaces and assigns them to the default theme.
    /// Dynamic atlas (glyphs added on demand, so Hindi or other scripts can fall back later), ASCII preloaded.
    /// Menu: Road Builder > UI > Build Font Assets. Safe to re-run.
    /// </summary>
    public static class FontAssetFactory
    {
        private const string DisplaySource = "Assets/Fonts/LilitaOne/LilitaOne-Regular.ttf";
        private const string BodySource = "Assets/Fonts/Baloo2/Baloo2-Variable.ttf";
        private const string DisplayAsset = "Assets/Fonts/LilitaOne/LilitaOne SDF.asset";
        private const string BodyAsset = "Assets/Fonts/Baloo2/Baloo2 SDF.asset";
        private const string FallbackAsset = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset";
        private const string ThemePath = "Assets/Resources/UI/UITheme_Default.asset";

        [MenuItem("Road Builder/UI/Build Font Assets")]
        public static void Build()
        {
            var display = BuildOne(DisplaySource, DisplayAsset);
            var body = BuildOne(BodySource, BodyAsset);

            var theme = AssetDatabase.LoadAssetAtPath<UITheme>(ThemePath);
            if (theme != null)
            {
                theme.displayFont = display;
                theme.bodyFont = body;
                EditorUtility.SetDirty(theme);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[FontAssetFactory] Display=" + (display != null) + " Body=" + (body != null) + " theme=" + (theme != null));
        }

        private static TMP_FontAsset BuildOne(string sourcePath, string assetPath)
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>(sourcePath);
            if (font == null) { Debug.LogError("[FontAssetFactory] Missing source font " + sourcePath); return null; }

            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (existing != null) AssetDatabase.DeleteAsset(assetPath);

            var fa = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            if (fa == null) { Debug.LogError("[FontAssetFactory] CreateFontAsset failed for " + sourcePath); return null; }
            fa.name = Path.GetFileNameWithoutExtension(assetPath);

            AssetDatabase.CreateAsset(fa, assetPath);
            if (fa.material != null)
            {
                fa.material.name = fa.name + " Material";
                AssetDatabase.AddObjectToAsset(fa.material, fa);
            }
            if (fa.atlasTextures != null)
            {
                for (int i = 0; i < fa.atlasTextures.Length; i++)
                {
                    fa.atlasTextures[i].name = fa.name + " Atlas" + (i == 0 ? "" : " " + i);
                    AssetDatabase.AddObjectToAsset(fa.atlasTextures[i], fa);
                }
            }

            // Preload printable ASCII so common UI text never triggers runtime atlas generation.
            var sb = new System.Text.StringBuilder();
            for (char c = (char)32; c < 127; c++) sb.Append(c);
            sb.Append("★–—•×");
            fa.TryAddCharacters(sb.ToString(), out string missing);
            if (!string.IsNullOrEmpty(missing)) Debug.Log("[FontAssetFactory] " + fa.name + " lacks: " + missing);

            var fallback = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FallbackAsset);
            if (fallback != null)
            {
                fa.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset> { fallback };
            }

            EditorUtility.SetDirty(fa);
            AssetDatabase.SaveAssets();
            return fa;
        }
    }
}
