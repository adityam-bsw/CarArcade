using System.Collections.Generic;

namespace RoadBuilderGame
{
    /// <summary>
    /// The thematic default road of every world: what a player who never picked a road explicitly sees. Display only: the gameplay numbers stay those of the
    /// starter road, and an explicit road choice always wins. Ids are surfaces that have a smooth strip under Resources/Roads/Smooth.
    /// Unknown worlds fall back to the three-theme defaults in <see cref="ThemeArtStyles"/>.
    /// </summary>
    public static class WorldRoads
    {
        private static readonly Dictionary<string, string> Map = new Dictionary<string, string>
        {
            { "world-summer", "surface-asphalt" }, { "world-rolling-hills", "surface-asphalt" }, { "world-spring-blossom", "surface-asphalt" },
            { "world-autumn-leaves", "surface-dirt" }, { "world-farm", "surface-dirt" }, { "world-wild-west", "surface-dirt" }, { "world-canyon", "surface-dirt" },
            { "world-jungle", "surface-grass-trail" },
            { "world-desert", "surface-sand" }, { "world-coast", "surface-beach" },
            { "world-winter", "surface-snow" }, { "world-christmas", "surface-snow" }, { "world-arctic", "surface-snow" },
            { "world-moon", "surface-moon-dust" }, { "world-mars", "surface-moon-dust" },
            { "world-industrial", "surface-metal-plates" }, { "world-night-city", "surface-neon-grid" }, { "world-synthwave", "surface-neon-grid" },
            { "world-candy-land", "surface-candy" }, { "world-cave", "surface-gravel" }, { "world-cloud-kingdom", "surface-cloud" },
            { "world-haunted-halloween", "surface-haunted-shadow" }, { "world-underwater", "surface-coral" }, { "world-volcano", "surface-lava-crust" },
        };

        /// <summary>Surface id of the world's default road.</summary>
        public static string DefaultFor(string worldId, EnvironmentTheme theme)
        {
            if (!string.IsNullOrEmpty(worldId) && Map.TryGetValue(worldId, out var id)) return id;
            return ThemeArtStyles.DefaultSmoothRoad(theme);
        }
    }
}
