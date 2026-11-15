namespace RoadBuilderGame
{
    /// <summary>Which world's hazard rows a run uses. Countryside keeps the shipped bolt, bomb and roller and the plain Road-Eater so the slice is unchanged; every other world uses its B2 projectile and A3 variant.</summary>
    public static class HazardWorlds
    {
        /// <summary>Testing and preview only: forces a world's hazards (short or catalogue id) in any run. Never saved.</summary>
        public static string Override = "";

        public static string Resolve(string selectedWorldId, bool countryside)
        {
            if (!string.IsNullOrEmpty(Override)) return Override;
            return countryside ? "" : (selectedWorldId ?? "");
        }
    }
}
