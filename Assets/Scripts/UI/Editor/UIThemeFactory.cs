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

        private static void SetZoneColors(UITheme theme)
        {
            theme.SetColor(ColorToken.ZoneIce, Hex("5BB8F0"));      // blue
            theme.SetColor(ColorToken.ZoneMud, Hex("B07A4F"));      // brown
            theme.SetColor(ColorToken.ZoneGlass, Hex("BFF3F5"));    // cyan-white
            theme.SetColor(ColorToken.ZoneMeteor, Hex("E8553F"));   // red (damage)
            theme.SetColor(ColorToken.ZoneRings, Hex("F6C445"));    // gold (reward)
            theme.SetColor(ColorToken.ZoneGravity, Hex("9B7BE8"));  // violet
            theme.SetColor(ColorToken.ZoneSpeed, Hex("F28C38"));    // orange
            theme.SetColor(ColorToken.ZoneInk, Hex("5560D6"));      // indigo
            theme.SetColor(ColorToken.ZoneWind, Hex("3FC4B5"));     // teal
            theme.SetColor(ColorToken.ZoneEconomy, Hex("57C26B"));  // green
            theme.SetColor(ColorToken.ZoneBody, Hex("EE7FB0"));     // pink
            theme.SetColor(ColorToken.ZoneScale, Hex("A8D63F"));    // lime
            theme.SetColor(ColorToken.ZoneVision, Hex("D9E2EC"));   // pale
            theme.SetColor(ColorToken.ZoneControl, Hex("F2B134"));  // amber
            theme.SetColor(ColorToken.ZoneHazard, Hex("D6384F"));   // crimson
            theme.SetColor(ColorToken.ZoneLiquid, Hex("E8651F"));   // deep orange
            theme.SetColor(ColorToken.ZoneTerrain, Hex("C9A66B"));  // tan
            theme.SetColor(ColorToken.ZoneRhythm, Hex("D45BD0"));   // magenta
        }

        /// <summary>Adds only the stage-zone colour tokens to the existing theme asset and leaves everything else untouched.</summary>
        [MenuItem("Road Builder/UI/Add Zone Colour Tokens")]
        public static void AddZoneColors()
        {
            var theme = AssetDatabase.LoadAssetAtPath<UITheme>(ThemePath);
            if (theme == null) { Debug.LogError("[UIThemeFactory] No theme at " + ThemePath); return; }
            SetZoneColors(theme);
            EditorUtility.SetDirty(theme);
            AssetDatabase.SaveAssets();
            Debug.Log("[UIThemeFactory] Zone colour tokens set on " + ThemePath);
        }

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

            // Colours: Retro Arcade Workshop (see Docs/UI/UI_Design_Bible.md).
            theme.SetColor(ColorToken.Background, Hex("18202A"));   // DeepInk
            theme.SetColor(ColorToken.Surface, Hex("252B36"));      // WorkshopCharcoal
            theme.SetColor(ColorToken.SurfaceAlt, Hex("3B434D"));   // dark steel
            theme.SetColor(ColorToken.Primary, Hex("F4A340"));      // RacingOrange
            theme.SetColor(ColorToken.Secondary, Hex("59636D"));    // steel
            theme.SetColor(ColorToken.Accent, Hex("F9C77D"));       // light orange
            theme.SetColor(ColorToken.Success, Hex("6B9B87"));      // WorkshopTeal
            theme.SetColor(ColorToken.Danger, Hex("D94B40"));       // SignalRed
            theme.SetColor(ColorToken.TextPrimary, Hex("E7E0CF"));  // WarmCream
            theme.SetColor(ColorToken.TextSecondary, Hex("BDB59F"));
            theme.SetColor(ColorToken.TextOnAccent, Hex("18202A"));
            theme.SetColor(ColorToken.Coin, Hex("F6B64E"));
            theme.SetColor(ColorToken.Ink, Hex("6B9B87"));
            theme.SetColor(ColorToken.Outline, Hex("18202A"));
            theme.SetColor(ColorToken.Tint, Color.white);
            SetZoneColors(theme);

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

            // Workshop artwork (generated by tools/ui/workshop_home.py, logged in Docs/UI/UI_Asset_Ledger.csv).
            const string art = "Assets/Art/UI/Workshop/";
            theme.SetSprite(SpriteSlot.ButtonArcade, ImportArt(art + "UI_Button_Arcade_Primary_Normal.png", false));
            theme.SetSprite(SpriteSlot.ButtonArcadePressed, ImportArt(art + "UI_Button_Arcade_Primary_Pressed.png", false));
            theme.SetSprite(SpriteSlot.ButtonArcadeDisabled, ImportArt(art + "UI_Button_Arcade_Primary_Disabled.png", false));
            theme.SetSprite(SpriteSlot.ButtonPlate, ImportArt(art + "UI_Button_Plate_Secondary_Normal.png", false));
            theme.SetSprite(SpriteSlot.ButtonPlatePressed, ImportArt(art + "UI_Button_Plate_Secondary_Pressed.png", false));
            theme.SetSprite(SpriteSlot.BadgeHex, ImportArt(art + "UI_Badge_Hex_Gear.png", false));
            theme.SetSprite(SpriteSlot.PlateTitle, ImportArt(art + "UI_Plate_Title_Main.png", false));
            theme.SetSprite(SpriteSlot.PlateCounter, ImportArt(art + "UI_Plate_Counter_Coin.png", false));
            theme.SetSprite(SpriteSlot.PlateLicense, ImportArt(art + "UI_Plate_License_Best.png", false));
            theme.SetSprite(SpriteSlot.DecoTape, ImportArt(art + "UI_Deco_Tape_01.png", false));
            theme.SetSprite(SpriteSlot.DecoSticker, ImportArt(art + "UI_Deco_Sticker_Star.png", false));

            theme.SetSprite(SpriteSlot.PlateTitleSmall, ImportArt(art + "UI_Plate_Title_Small.png", false));
            theme.SetSprite(SpriteSlot.BadgeHexBack, ImportArt(art + "UI_Badge_Hex_Back.png", false));
            theme.SetSprite(SpriteSlot.PlateUpgrade, ImportArt(art + "UI_Plate_Upgrade.png", false));
            theme.SetSprite(SpriteSlot.PipOn, ImportArt(art + "UI_Pip_On.png", false));
            theme.SetSprite(SpriteSlot.PipOff, ImportArt(art + "UI_Pip_Off.png", false));
            theme.SetSprite(SpriteSlot.PlateName, ImportArt(art + "UI_Plate_Name.png", false));
            theme.SetSprite(SpriteSlot.CardVehicle, ImportArt(art + "UI_Card_Vehicle_Plate.png", false));
            theme.SetSprite(SpriteSlot.CardWorld, ImportArt(art + "UI_Card_World_Postcard.png", false));
            theme.SetSprite(SpriteSlot.CardRoad, ImportArt(art + "UI_Card_Road_Ticket.png", false));
            theme.SetSprite(SpriteSlot.FrameVehicle, ImportArt(art + "UI_Card_Frame_Vehicle.png", false));
            theme.SetSprite(SpriteSlot.FrameWorld, ImportArt(art + "UI_Card_Frame_World.png", false));
            theme.SetSprite(SpriteSlot.FrameRoad, ImportArt(art + "UI_Card_Frame_Road.png", false));
            theme.SetSprite(SpriteSlot.LockTag, ImportArt(art + "UI_Icon_Lock_Tag.png", false));
            theme.SetSprite(SpriteSlot.CertPaper, ImportArt(art + "UI_Cert_Paper.png", false));
            theme.SetSprite(SpriteSlot.StampRecord, ImportArt(art + "UI_Stamp_Record.png", false));
            theme.SetSprite(SpriteSlot.PanelBoard, ImportArt(art + "UI_Panel_Board.png", false, 70f));
            theme.SetSprite(SpriteSlot.ToggleOn, ImportArt(art + "UI_Toggle_On.png", false));
            theme.SetSprite(SpriteSlot.ToggleOff, ImportArt(art + "UI_Toggle_Off.png", false));
            theme.SetSprite(SpriteSlot.HudOdometer, ImportArt(art + "UI_HUD_Odometer.png", false));
            theme.SetSprite(SpriteSlot.HudSpeedo, ImportArt(art + "UI_HUD_Speedo.png", false));
            theme.SetSprite(SpriteSlot.HudNeedle, ImportArt(art + "UI_HUD_Needle.png", false));
            theme.SetSprite(SpriteSlot.HudHub, ImportArt(art + "UI_HUD_Hub.png", false));
            theme.SetSprite(SpriteSlot.HeartOn, ImportArt(art + "UI_HUD_Heart_On.png", false));
            theme.SetSprite(SpriteSlot.HeartOff, ImportArt(art + "UI_HUD_Heart_Off.png", false));
            theme.SetSprite(SpriteSlot.InkFrame, ImportArt(art + "UI_HUD_Ink_Frame.png", false));
            theme.SetSprite(SpriteSlot.InkFill, ImportArt(art + "UI_HUD_Ink_Fill.png", false));
            theme.SetSprite(SpriteSlot.TabNormal, ImportArt(art + "UI_Tab_Metal_Normal.png", false));
            theme.SetSprite(SpriteSlot.TabSelected, ImportArt(art + "UI_Tab_Metal_Selected.png", false));

            theme.SetSprite(SpriteSlot.BgWallTile, ImportArt(art + "UI_Bg_Wall_Tile.png", true, 0f, 20f, true));
            theme.SetSprite(SpriteSlot.BgFloorTile, ImportArt(art + "UI_Bg_Floor_Tile.png", true, 0f, 20f, true));
            theme.SetSprite(SpriteSlot.BgHazardTile, ImportArt(art + "UI_Bg_Hazard_Tile.png", true, 0f, 20f, true));
            theme.SetSprite(SpriteSlot.BgBeamTile, ImportArt(art + "UI_Bg_Beam_Tile.png", true, 0f, 20f, true));
            theme.SetSprite(SpriteSlot.BgDoor, ImportArt(art + "UI_Bg_Door.png", true, 0f, 20f, false));
            BuildPropSet();

            EditorUtility.SetDirty(theme);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[UIThemeFactory] Default theme ready at " + ThemePath);
        }

        /// <summary>Every stand-alone workshop prop (pixel art, Point filter) in one asset the builder and screens read by id.</summary>
        private static void BuildPropSet()
        {
            const string dir = "Assets/Art/UI/Workshop/Props";
            const string path = "Assets/Resources/UI/WorkshopProps.asset";
            var set = AssetDatabase.LoadAssetAtPath<WorkshopPropSet>(path);
            if (set == null) { set = ScriptableObject.CreateInstance<WorkshopPropSet>(); AssetDatabase.CreateAsset(set, path); }
            set.props.Clear();
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { dir }))
            {
                string file = AssetDatabase.GUIDToAssetPath(guid);
                var sprite = ImportArt(file, true);
                string id = Path.GetFileNameWithoutExtension(file).Replace("UI_Prop_", "");
                if (sprite != null) set.props.Add(new WorkshopPropSet.Entry { id = id, sprite = sprite });
            }
            EditorUtility.SetDirty(set);
        }

        /// <summary>Illustrated UI art: smooth art is bilinear, pixel art is point; both uncompressed with no mipmaps.</summary>
        private static Sprite ImportArt(string path, bool pixel, float border = 0f, float ppu = 100f, bool tile = false)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) { Debug.LogError("[UIThemeFactory] Missing art " + path); return null; }
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = ppu;
            imp.mipmapEnabled = false;
            imp.isReadable = false;
            if (tile) imp.wrapMode = TextureWrapMode.Repeat;
            imp.alphaIsTransparency = true;
            imp.filterMode = pixel ? FilterMode.Point : FilterMode.Bilinear;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.spriteBorder = new Vector4(border, border, border, border);
            if (tile)
            {
                var ts = new TextureImporterSettings();
                imp.ReadTextureSettings(ts);
                ts.spriteMeshType = SpriteMeshType.FullRect;
                imp.SetTextureSettings(ts);
            }
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
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
