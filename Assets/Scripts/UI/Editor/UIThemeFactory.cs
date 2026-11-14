using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace RoadBuilderGame.UI.Editor
{
    /// <summary>
    /// Creates the default UITheme and its procedurally generated 9-slice sprites (white shapes that the theme
    /// tints). Menu: Road Builder > UI > Create Default Theme. Safe to re-run: it updates the existing asset.
    /// </summary>
    public static class UIThemeFactory
    {
        private const string SpriteDir = "Assets/Art/UI/Generated";
        private const string ThemePath = "Assets/Resources/UI/UITheme_Default.asset";
        private const string FallbackFont = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

        [MenuItem("Road Builder/UI/Create Default Theme")]
        public static void CreateDefaultTheme()
        {
            Directory.CreateDirectory(SpriteDir);
            Directory.CreateDirectory(Path.GetDirectoryName(ThemePath));

            var theme = AssetDatabase.LoadAssetAtPath<UITheme>(ThemePath);
            if (theme == null)
            {
                theme = ScriptableObject.CreateInstance<UITheme>();
                AssetDatabase.CreateAsset(theme, ThemePath);
            }

            // Colours: dark navy surfaces with bright, friendly accents.
            theme.SetColor(ColorToken.Background, Hex("1B2033"));
            theme.SetColor(ColorToken.Surface, Hex("262D47"));
            theme.SetColor(ColorToken.SurfaceAlt, Hex("323B5E"));
            theme.SetColor(ColorToken.Primary, Hex("3DDC6C"));
            theme.SetColor(ColorToken.Secondary, Hex("3D8BFF"));
            theme.SetColor(ColorToken.Accent, Hex("FFC83D"));
            theme.SetColor(ColorToken.Success, Hex("3DDC6C"));
            theme.SetColor(ColorToken.Danger, Hex("FF4D5E"));
            theme.SetColor(ColorToken.TextPrimary, Hex("FFFFFF"));
            theme.SetColor(ColorToken.TextSecondary, Hex("B8C0E0"));
            theme.SetColor(ColorToken.TextOnAccent, Hex("1B2033"));
            theme.SetColor(ColorToken.Coin, Hex("FFD23F"));
            theme.SetColor(ColorToken.Ink, Hex("A63DFF"));
            theme.SetColor(ColorToken.Outline, Hex("0E1220"));

            // Fonts: placeholder until the real font family is imported (rb-asset-pipeline).
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FallbackFont);
            if (theme.displayFont == null) theme.displayFont = font;
            if (theme.bodyFont == null) theme.bodyFont = font;

            theme.SetTextStyle(TextStyle.Title, Style(FontRole.Display, 72, FontStyles.Bold, ColorToken.TextPrimary));
            theme.SetTextStyle(TextStyle.Heading, Style(FontRole.Display, 48, FontStyles.Bold, ColorToken.TextPrimary));
            theme.SetTextStyle(TextStyle.Body, Style(FontRole.Body, 32, FontStyles.Normal, ColorToken.TextPrimary));
            theme.SetTextStyle(TextStyle.Caption, Style(FontRole.Body, 24, FontStyles.Normal, ColorToken.TextSecondary));
            theme.SetTextStyle(TextStyle.Number, Style(FontRole.Display, 40, FontStyles.Bold, ColorToken.TextPrimary));
            theme.SetTextStyle(TextStyle.Button, Style(FontRole.Display, 40, FontStyles.Bold, ColorToken.TextPrimary));

            // Sprites: white shapes, tinted by ColorToken at runtime.
            theme.SetSprite(SpriteSlot.Button, MakeSprite("ui_button", 64, 20, lip: 8));
            theme.SetSprite(SpriteSlot.Panel, MakeSprite("ui_panel", 64, 16, lip: 0));
            theme.SetSprite(SpriteSlot.Popup, MakeSprite("ui_popup", 96, 28, lip: 0));
            theme.SetSprite(SpriteSlot.Card, MakeSprite("ui_card", 64, 20, lip: 6));
            theme.SetSprite(SpriteSlot.Pill, MakeSprite("ui_pill", 64, 32, lip: 0));
            theme.SetSprite(SpriteSlot.BarBackground, MakeSprite("ui_bar_bg", 64, 32, lip: 0));
            theme.SetSprite(SpriteSlot.BarFill, MakeSprite("ui_bar_fill", 64, 32, lip: 0));

            EditorUtility.SetDirty(theme);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[UIThemeFactory] Default theme ready at " + ThemePath);
        }

        private static TextStyleDef Style(FontRole role, float size, FontStyles fs, ColorToken color)
            => new TextStyleDef { font = role, size = size, fontStyle = fs, color = color };

        private static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var c);
            return c;
        }

        /// <summary>White rounded rectangle, anti-aliased. A non-zero lip darkens the bottom band to fake button depth.</summary>
        private static Sprite MakeSprite(string name, int size, int radius, int lip)
        {
            string path = SpriteDir + "/" + name + ".png";
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var px = new Color32[size * size];
            float half = size / 2f;
            float inner = half - radius;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Signed distance to a rounded square centred in the texture.
                    float dx = Mathf.Max(Mathf.Abs(x + 0.5f - half) - inner, 0f);
                    float dy = Mathf.Max(Mathf.Abs(y + 0.5f - half) - inner, 0f);
                    float dist = Mathf.Sqrt(dx * dx + dy * dy) - radius;
                    float alpha = Mathf.Clamp01(0.5f - dist);

                    float shade = (lip > 0 && y < lip) ? 0.72f : 1f; // y=0 is the bottom row
                    byte v = (byte)Mathf.RoundToInt(shade * 255f);
                    px[y * size + x] = new Color32(v, v, v, (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.alphaIsTransparency = true;
            importer.spritePixelsPerUnit = 100f;
            importer.spriteBorder = new Vector4(radius + 1, radius + 1, radius + 1, radius + 1);
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
