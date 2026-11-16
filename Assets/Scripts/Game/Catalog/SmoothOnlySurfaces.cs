using System;

namespace RoadBuilderGame
{
    /// <summary>
    /// The 13 behaviour surfaces (blueprint C2) that only exist as smooth road strips. They are drawn with their own strip in every world, pixel worlds included,
    /// because the strip is the cue for what the road does. Other surfaces keep the world's art style.
    /// </summary>
    public static class SmoothOnlySurfaces
    {
        public static readonly string[] Ids =
        {
            "surface-spring-road", "surface-magnetic-rail", "surface-acid", "surface-oil", "surface-conveyor", "surface-lava-river", "surface-sticky-candy", "surface-honey",
            "surface-metal-plates", "surface-ice-crystals", "surface-haunted-shadow", "surface-electric-rail", "surface-fragile-paper",
        };
        public static bool Contains(string id) { return Array.IndexOf(Ids, id) >= 0; }
    }
}
