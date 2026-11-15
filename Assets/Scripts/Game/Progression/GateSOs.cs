using System;
using UnityEngine;

namespace RoadBuilderGame
{
    /// <summary>Extra unlock conditions. Append new members at the end only (the SO assets store the numbers).</summary>
    public enum ExtraGate { None, FirstMonster, Missions, CleanJumps, BossEncounter, SurviveLava, WaterMastery, StageEvents, SurvivalRuns, RhythmChallenge, BossDefeated, SurfaceTutorial, WorldMastery }

    public enum SurfaceCategory { Basic, Unstable, Advanced, Special, Exotic }

    [Serializable] public struct WorldGate { public string worldId; public int unlockMetres; public int coins; public ExtraGate extra; public int extraCount; }
    [Serializable] public struct VehicleGate { public string vehicleId; public int tier; public int unlockMetres; public int coins; }
    [Serializable] public struct SurfaceGate { public string surfaceId; public SurfaceCategory category; public int unlockMetres; public int coins; public ExtraGate extra; public int extraCount; }
}
