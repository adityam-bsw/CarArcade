namespace RoadBuilderGame
{
    /// <summary>
    /// How a piece of art is meant to be drawn. Pixel is the original look (point filtered, snapped to the art-pixel
    /// grid). Smooth is the clean high-resolution look (bilinear, no snapping). Pixel is always the default (value 0),
    /// so every existing asset keeps rendering exactly as before until it is converted and switched.
    /// </summary>
    public enum ArtStyle { Pixel = 0, Smooth = 1 }

    /// <summary>
    /// Import conventions for the smooth art style (Docs/design/ART_BIBLE.md section 7.1). Shared by the editor
    /// importer rule and the EditMode checks so both read the same numbers. Applies only under <see cref="Root"/>;
    /// pixel assets elsewhere are never touched.
    /// </summary>
    public static class SmoothArt
    {
        public const string Root = "Assets/Art/Smooth/";
        public const string UiFolder = "Assets/Art/Smooth/UI/";
        public const string BackgroundFolder = "Assets/Art/Smooth/Backgrounds/";
        public const string RoadFolder = "Assets/Art/Smooth/Roads/";

        /// <summary>Road strips tile in X: they import with Repeat wrap and a Full Rect sprite mesh.</summary>
        public static bool IsRoadFolder(string assetPath)
        {
            return assetPath != null && assetPath.Replace('\\', '/').StartsWith(RoadFolder, System.StringComparison.Ordinal);
        }

        public const float GameplayPpu = 128f;
        public const float UiPpu = 100f;
        /// <summary>576 px layer height over 14 world units.</summary>
        public const float BackgroundPpu = 576f / 14f;
        /// <summary>World height of one 576 px background layer; it equals the default camera view height, so smooth layers are shown at scale 1.</summary>
        public const float BackgroundHeightUnits = 14f;

        public const int MaxTextureSize = 2048;
        /// <summary>Sprite atlas padding to author and set on SpriteAtlas assets (they are not changed by the importer rule).</summary>
        public const int AtlasPaddingPx = 4;

        /// <summary>Vehicle art folders (Vehicles, and the catalogue restyle Vehicles2): ASTC 5x5; Vehicles2 files keep a per-file ppu of 16 x S.</summary>
        public static bool IsVehicleFolder(string assetPath)
        {
            string p = assetPath.Replace('\\', '/');
            return p.StartsWith(Root + "Vehicles/", System.StringComparison.Ordinal) || p.StartsWith(Root + "Vehicles2/", System.StringComparison.Ordinal);
        }

        public static bool IsSmoothPath(string assetPath)
        {
            return assetPath != null && assetPath.Replace('\\', '/').StartsWith(Root, System.StringComparison.Ordinal);
        }

        /// <summary>Default pixels per unit for a new texture at this path (a per-file override after import is allowed).</summary>
        public static float DefaultPpu(string assetPath)
        {
            string p = assetPath.Replace('\\', '/');
            if (p.StartsWith(UiFolder, System.StringComparison.Ordinal)) return UiPpu;
            if (p.StartsWith(BackgroundFolder, System.StringComparison.Ordinal)) return BackgroundPpu;
            return GameplayPpu;
        }
    }
}
