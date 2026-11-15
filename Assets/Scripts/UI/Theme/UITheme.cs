using System;
using TMPro;
using UnityEngine;

namespace RoadBuilderGame.UI
{
    /// <summary>Named colour slots. UI never hardcodes a colour; it asks the theme for a token.</summary>
    public enum ColorToken
    {
        Background, Surface, SurfaceAlt, Primary, Secondary, Accent, Success, Danger,
        TextPrimary, TextSecondary, TextOnAccent, Coin, Ink, Outline,
        /// <summary>White: for illustrated artwork that keeps its own colours.</summary>
        Tint,
        // Stage event zone colours (brief section 4: red damage, blue ice, brown mud, cyan-white glass, gold reward). Appended so saved theme indices stay valid.
        ZoneIce, ZoneMud, ZoneGlass, ZoneMeteor, ZoneRings,
        // Mutation event zone colours (gap list A1): gravity violet, speed orange, ink indigo, wind teal, economy green, fragile pink, scale lime.
        ZoneGravity, ZoneSpeed, ZoneInk, ZoneWind, ZoneEconomy, ZoneBody, ZoneScale
    }

    public enum FontRole { Display, Body }

    /// <summary>Semantic text styles. Screens pick a style, the theme decides font, size and colour.</summary>
    public enum TextStyle { Title, Heading, Body, Caption, Number, Button }

    /// <summary>Named sprite slots (9-sliced). A theme can swap the whole look by swapping these.</summary>
    public enum SpriteSlot
    {
        None, Button, Panel, Popup, Pill, BarBackground, BarFill, Card,
        // Retro Arcade Workshop artwork (illustrated, keeps its own colours: use ColorToken.Tint)
        ButtonArcade, ButtonArcadePressed, ButtonArcadeDisabled, ButtonPlate, ButtonPlatePressed, BadgeHex,
        PlateTitle, PlateCounter, PlateLicense, DecoTape, DecoSticker, BackdropHome,
        PlateTitleSmall, BadgeHexBack, PlateUpgrade, PipOn, PipOff, PlateName,
        CardVehicle, CardWorld, CardRoad, FrameVehicle, FrameWorld, FrameRoad, LockTag, CertPaper, StampRecord, PanelBoard, ToggleOn, ToggleOff,
        HudOdometer, HudSpeedo, HudNeedle, HudHub, HeartOn, HeartOff, InkFrame, InkFill, TabNormal, TabSelected, BackdropGarage, BackdropWall,
        // Garage background as separate parts: tiles repeat sideways to fill any screen, the door is one centred sprite
        BgWallTile, BgFloorTile, BgHazardTile, BgBeamTile, BgDoor
    }

    [Serializable]
    public class TextStyleDef
    {
        public FontRole font = FontRole.Body;
        public float size = 32f;
        public FontStyles fontStyle = FontStyles.Normal;
        public ColorToken color = ColorToken.TextPrimary;
    }

    /// <summary>
    /// One look for the whole UI: colours, fonts, text styles, sprites. Mode packs can ship their own
    /// (e.g. a pencil-notebook theme) and swap it through <see cref="UIThemeManager"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "Road Builder/UI Theme", fileName = "UITheme")]
    public class UITheme : ScriptableObject
    {
        [Header("Colours (indexed by ColorToken)")]
        [SerializeField] private Color[] colors = new Color[Enum.GetValues(typeof(ColorToken)).Length];

        [Header("Art")]
        [Tooltip("Informational: which art style this theme's sprites follow. Pixel until the Workshop plates are converted.")]
        public ArtStyle filterStyle = ArtStyle.Pixel;

        [Header("Fonts")]
        public TMP_FontAsset displayFont;
        public TMP_FontAsset bodyFont;

        [Header("Text styles (indexed by TextStyle)")]
        [SerializeField] private TextStyleDef[] textStyles = new TextStyleDef[Enum.GetValues(typeof(TextStyle)).Length];

        [Header("Sprites (indexed by SpriteSlot, None is unused)")]
        [SerializeField] private Sprite[] sprites = new Sprite[Enum.GetValues(typeof(SpriteSlot)).Length];

        [Header("Press feedback")]
        [Range(0.8f, 1f)] public float pressedScale = 0.92f;

        public Color GetColor(ColorToken token)
        {
            int i = (int)token;
            return (colors != null && i < colors.Length) ? colors[i] : Color.magenta;
        }

        public void SetColor(ColorToken token, Color c)
        {
            EnsureSizes();
            colors[(int)token] = c;
        }

        public Sprite GetSprite(SpriteSlot slot)
        {
            int i = (int)slot;
            return (sprites != null && i < sprites.Length) ? sprites[i] : null;
        }

        public void SetSprite(SpriteSlot slot, Sprite s)
        {
            EnsureSizes();
            sprites[(int)slot] = s;
        }

        public TextStyleDef GetTextStyle(TextStyle style)
        {
            int i = (int)style;
            if (textStyles == null || i >= textStyles.Length || textStyles[i] == null) return new TextStyleDef();
            return textStyles[i];
        }

        public void SetTextStyle(TextStyle style, TextStyleDef def)
        {
            EnsureSizes();
            textStyles[(int)style] = def;
        }

        public TMP_FontAsset GetFont(FontRole role) => role == FontRole.Display ? displayFont : bodyFont;

        private void EnsureSizes()
        {
            Resize(ref colors, Enum.GetValues(typeof(ColorToken)).Length);
            Resize(ref textStyles, Enum.GetValues(typeof(TextStyle)).Length);
            Resize(ref sprites, Enum.GetValues(typeof(SpriteSlot)).Length);
        }

        private static void Resize<T>(ref T[] arr, int n)
        {
            if (arr == null) arr = new T[n];
            else if (arr.Length != n) Array.Resize(ref arr, n);
        }

        private void OnValidate()
        {
            EnsureSizes();
            UIThemeManager.NotifyThemeEdited(this);
        }
    }
}
