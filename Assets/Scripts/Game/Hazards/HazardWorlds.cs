namespace RoadBuilderGame
{
    /// <summary>Which world's hazard rows a run uses. Every world uses its B2 projectile and A3 variant while stage events are on (Countryside = hills: Boulder Ball and Dirt Maw); the legacy bolt, bomb, roller and plain Eater stay only for stage events off.</summary>
    public static class HazardWorlds
    {
        /// <summary>Testing and preview only: forces a world's hazards (short or catalogue id) in any run. Never saved.</summary>
        public static string Override = "";

        /// <summary>Countryside's world row (rolling hills: Boulder Ball, Dirt Maw).</summary>
        public const string CountrysideWorld = "hills";

        /// <summary>Legacy resolve (stage events assumed off for Countryside): the old bolt, bomb, roller and plain Eater.</summary>
        public static string Resolve(string selectedWorldId, bool countryside) { return Resolve(selectedWorldId, countryside, false); }

        /// <summary>
        /// With stage events on, every world (Countryside included) uses its own projectile row, boss tier set and Eater variant. Only with stage events off does
        /// Countryside keep the legacy archetypes.
        /// </summary>
        public static string Resolve(string selectedWorldId, bool countryside, bool stageEventsOn)
        {
            if (!string.IsNullOrEmpty(Override)) return Override;
            if (countryside) return stageEventsOn ? CountrysideWorld : "";
            return selectedWorldId ?? "";
        }
    }
}
