using System;
using UnityEngine;

namespace RoadBuilderGame
{
    public enum ExtraGate { None, FirstMonster, Missions, CleanJumps, BossEncounter, SurviveLava, WaterMastery, StageEvents, SurvivalRuns, RhythmChallenge }

    [Serializable] public struct WorldGate { public string worldId; public int unlockMetres; public int coins; public ExtraGate extra; public int extraCount; }
    [Serializable] public struct VehicleGate { public string vehicleId; public int unlockMetres; public int coins; }

    /// <summary>World unlock table (blueprint D4). Distance is cumulative lifetime metres. Gates only guard NEW unlocks; owned content stays unlocked.</summary>
    [CreateAssetMenu(menuName = "Road Builder/Progression/World Gates", fileName = "WorldGates")]
    public class WorldGateSO : ScriptableObject
    {
        public WorldGate[] gates = Defaults();
        public bool TryGet(string id, out WorldGate g) { if (gates != null) for (int i = 0; i < gates.Length; i++) if (gates[i].worldId == id) { g = gates[i]; return true; } g = default; return false; }
        public static WorldGate[] Defaults()
        {
            return new[]
            {
                W("world-rolling-hills", 0, 0, ExtraGate.None, 0), W("world-desert", 1000, 2000, ExtraGate.FirstMonster, 1),
                W("world-winter", 2500, 4000, ExtraGate.Missions, 3), W("world-moon", 4000, 6000, ExtraGate.CleanJumps, 5),
                W("world-mars", 5500, 8000, ExtraGate.BossEncounter, 1), W("world-volcano", 7000, 12000, ExtraGate.SurviveLava, 1),
                W("world-underwater", 8500, 15000, ExtraGate.WaterMastery, 1), W("world-candy-land", 10000, 18000, ExtraGate.StageEvents, 10),
                W("world-haunted-halloween", 12000, 22000, ExtraGate.SurvivalRuns, 5), W("world-synthwave", 15000, 30000, ExtraGate.RhythmChallenge, 1),
            };
        }
        private static WorldGate W(string id, int m, int c, ExtraGate e, int n) { return new WorldGate { worldId = id, unlockMetres = m, coins = c, extra = e, extraCount = n }; }
    }

    /// <summary>Vehicle unlock table (blueprint D2; the two slice prices 600 and 2000 are approved separately and have no distance gate yet).</summary>
    [CreateAssetMenu(menuName = "Road Builder/Progression/Vehicle Gates", fileName = "VehicleGates")]
    public class VehicleGateSO : ScriptableObject
    {
        public VehicleGate[] gates = Defaults();
        public bool TryGet(string id, out VehicleGate g) { if (gates != null) for (int i = 0; i < gates.Length; i++) if (gates[i].vehicleId == id) { g = gates[i]; return true; } g = default; return false; }
        public static VehicleGate[] Defaults()
        {
            return new[]
            {
                V("vehicle-car", 0, 0), V("vehicle-sports-car", 0, 600), V("vehicle-monster-truck", 0, 2000), V("vehicle-bicycle", 500, 1500),
                V("vehicle-dune-buggy", 1500, 4000), V("vehicle-race-car", 3000, 8000), V("vehicle-tank", 8000, 28000), V("vehicle-moon-rover", 12000, 50000),
            };
        }
        private static VehicleGate V(string id, int m, int c) { return new VehicleGate { vehicleId = id, unlockMetres = m, coins = c }; }
    }
}
