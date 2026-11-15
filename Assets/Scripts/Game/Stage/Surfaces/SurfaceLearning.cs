namespace RoadBuilderGame
{
    public enum SurfaceHintLevel { Full = 0, Icon = 1, None = 2 }

    /// <summary>The 3-step learning rule: the first sighting of a surface shows the full preview, the second only the icon, the third and later nothing.</summary>
    public static class SurfaceLearning
    {
        public static SurfaceHintLevel Level(int seenBefore) { return seenBefore <= 0 ? SurfaceHintLevel.Full : seenBefore == 1 ? SurfaceHintLevel.Icon : SurfaceHintLevel.None; }

        /// <summary>Banner text for the level, empty for none.</summary>
        public static string Hint(SurfaceBehaviourSetSO set, SurfaceHintLevel level)
        {
            if (set == null) return "";
            if (level == SurfaceHintLevel.Full) return set.hintFull ?? "";
            if (level == SurfaceHintLevel.Icon) return set.hintShort ?? "";
            return "";
        }
    }
}
