using System;
using UnityEngine;

namespace RoadBuilderGame
{
    /// <summary>Vehicle unlock table (blueprint D2; the two slice prices 600 and 2000 are approved separately and have no distance gate yet).</summary>
    [CreateAssetMenu(menuName = "Road Builder/Progression/Vehicle Gates", fileName = "VehicleGates")]
    public class VehicleGateSO : ScriptableObject
    {
        public VehicleGate[] gates = Defaults();
        private static VehicleGateSO _cached;
        public static VehicleGateSO Load() { if (_cached == null) { _cached = Resources.Load<VehicleGateSO>("Progression/VehicleGates"); if (_cached == null) _cached = CreateInstance<VehicleGateSO>(); } return _cached; }
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
