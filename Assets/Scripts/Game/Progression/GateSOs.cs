using System;
using UnityEngine;

namespace RoadBuilderGame
{
    public enum ExtraGate { None, FirstMonster, Missions, CleanJumps, BossEncounter, SurviveLava, WaterMastery, StageEvents, SurvivalRuns, RhythmChallenge }

    [Serializable] public struct WorldGate { public string worldId; public int unlockMetres; public int coins; public ExtraGate extra; public int extraCount; }
    [Serializable] public struct VehicleGate { public string vehicleId; public int unlockMetres; public int coins; }

}
