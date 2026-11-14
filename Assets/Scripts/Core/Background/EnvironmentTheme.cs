namespace RoadBuilderGame
{
    /// <summary>The worlds the player can pick. Each maps to a mood the background remixer draws scenes from.</summary>
    public enum EnvironmentTheme
    {
        Country = 0,
        Desert = 1,
        CyberNight = 2
    }

    /// <summary>
    /// Scenes of one mood share a colour feel, so their layers can be mixed without clashing.
    /// </summary>
    public enum Mood
    {
        Green = 0,   // rolling hills, rainforest, coast
        Warm = 1,    // desert, volcanic
        Cool = 2,    // alpine, mist, tundra
        Dark = 3     // night city, industrial
    }

    public enum TimeOfDay
    {
        Day = 0,
        Dawn = 1,
        Dusk = 2
    }
}
