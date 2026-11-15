using UnityEngine;

namespace RoadBuilderGame
{
    /// <summary>
    /// The single art-style switch per world theme. A theme is Smooth or Pixel as a whole: its background remix pool and its
    /// road surfaces both follow this value, so one world never mixes styles. Data lives in the asset
    /// Resources/Themes/ThemeArtStyles; if the asset is missing the built-in defaults apply (all three worlds Smooth).
    /// Flip a theme to Pixel (here, or at runtime with <see cref="Override"/>) to get the original look back.
    /// </summary>
    [CreateAssetMenu(menuName = "Road Builder/Theme Art Styles", fileName = "ThemeArtStyles")]
    public class ThemeArtStyles : ScriptableObject
    {
        public ArtStyle country = ArtStyle.Smooth;
        public ArtStyle desert = ArtStyle.Smooth;
        public ArtStyle cyberNight = ArtStyle.Smooth;

        [Header("Default smooth road per world (display fallback when the chosen surface has no smooth strip)")]
        public string countryRoad = "surface-asphalt";
        public string desertRoad = "surface-sand";
        public string cyberNightRoad = "surface-neon-grid";

        private static ThemeArtStyles _loaded;
        private static readonly ArtStyle?[] Overrides = new ArtStyle?[3];

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _loaded = null;
            for (int i = 0; i < Overrides.Length; i++) Overrides[i] = null;
        }

        public ArtStyle Get(EnvironmentTheme theme)
        {
            switch (theme)
            {
                case EnvironmentTheme.Desert: return desert;
                case EnvironmentTheme.CyberNight: return cyberNight;
                default: return country;
            }
        }

        /// <summary>The art style the theme is drawn in right now (runtime override first, then the asset, then the defaults).</summary>
        public static ArtStyle For(EnvironmentTheme theme)
        {
            int i = (int)theme;
            if (i >= 0 && i < Overrides.Length && Overrides[i].HasValue) return Overrides[i].Value;
            if (_loaded == null) _loaded = Resources.Load<ThemeArtStyles>("Themes/ThemeArtStyles");
            if (_loaded != null) return _loaded.Get(theme);
            return ArtStyle.Smooth;
        }

        /// <summary>Surface id of the world's default smooth road, drawn when the chosen surface has no smooth strip (display only).</summary>
        public static string DefaultSmoothRoad(EnvironmentTheme theme)
        {
            if (_loaded == null) _loaded = Resources.Load<ThemeArtStyles>("Themes/ThemeArtStyles");
            switch (theme)
            {
                case EnvironmentTheme.Desert: return _loaded != null ? _loaded.desertRoad : "surface-sand";
                case EnvironmentTheme.CyberNight: return _loaded != null ? _loaded.cyberNightRoad : "surface-neon-grid";
                default: return _loaded != null ? _loaded.countryRoad : "surface-asphalt";
            }
        }

        /// <summary>Session-only switch (does not touch the asset or the save). Pass null to clear. Apply it before the next world is applied.</summary>
        public static void Override(EnvironmentTheme theme, ArtStyle? style)
        {
            int i = (int)theme;
            if (i >= 0 && i < Overrides.Length) Overrides[i] = style;
        }
    }
}
