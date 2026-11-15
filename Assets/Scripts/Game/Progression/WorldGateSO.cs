using System;
using UnityEngine;

namespace RoadBuilderGame
{
    /// <summary>World unlock table (blueprint D4). Distance is cumulative lifetime metres. Gates only guard NEW unlocks; owned content stays unlocked.</summary>
    [CreateAssetMenu(menuName = "Road Builder/Progression/World Gates", fileName = "WorldGates")]
    public class WorldGateSO : ScriptableObject
    {
        public WorldGate[] gates = Defaults();
        private static WorldGateSO _cached;
        public static WorldGateSO Load() { if (_cached == null) { _cached = Resources.Load<WorldGateSO>("Progression/WorldGates"); if (_cached == null) _cached = CreateInstance<WorldGateSO>(); } return _cached; }
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
}
