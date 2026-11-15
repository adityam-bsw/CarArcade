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
                W("world-rolling-hills", 0, 0, ExtraGate.None, 0), W("world-summer", 500, 1000, ExtraGate.None, 0), W("world-spring-blossom", 700, 1500, ExtraGate.Missions, 1),
                W("world-farm", 900, 1800, ExtraGate.None, 0), W("world-desert", 1000, 2000, ExtraGate.FirstMonster, 1), W("world-coast", 1500, 2500, ExtraGate.Missions, 2),
                W("world-autumn-leaves", 1800, 3000, ExtraGate.StageEvents, 3), W("world-winter", 2500, 4000, ExtraGate.Missions, 3), W("world-canyon", 3000, 5000, ExtraGate.CleanJumps, 3),
                W("world-night-city", 3500, 5500, ExtraGate.Missions, 4), W("world-moon", 4000, 6000, ExtraGate.CleanJumps, 5), W("world-wild-west", 4500, 7000, ExtraGate.StageEvents, 5),
                W("world-jungle", 5000, 7500, ExtraGate.SurvivalRuns, 2), W("world-mars", 5500, 8000, ExtraGate.BossEncounter, 1), W("world-arctic", 6000, 9000, ExtraGate.SurvivalRuns, 3),
                W("world-volcano", 7000, 12000, ExtraGate.SurviveLava, 1), W("world-cave", 7500, 12000, ExtraGate.StageEvents, 6), W("world-industrial", 8000, 13000, ExtraGate.BossDefeated, 1),
                W("world-underwater", 8500, 15000, ExtraGate.WaterMastery, 1), W("world-christmas", 9000, 16000, ExtraGate.Missions, 8), W("world-candy-land", 10000, 18000, ExtraGate.StageEvents, 10),
                W("world-cloud-kingdom", 11000, 20000, ExtraGate.CleanJumps, 15), W("world-haunted-halloween", 12000, 22000, ExtraGate.SurvivalRuns, 5), W("world-synthwave", 15000, 30000, ExtraGate.RhythmChallenge, 1),
            };
        }
        private static WorldGate W(string id, int m, int c, ExtraGate e, int n) { return new WorldGate { worldId = id, unlockMetres = m, coins = c, extra = e, extraCount = n }; }
    }
}
