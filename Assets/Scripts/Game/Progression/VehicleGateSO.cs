using System;
using UnityEngine;

namespace RoadBuilderGame
{
    /// <summary>
    /// Vehicle unlock table (blueprint D2) for all 32 catalogue vehicles. The two slice prices 600 (sports car) and 2000 (monster truck) are approved
    /// separately and keep no distance gate. Mapping of the blueprint tiers onto the catalogue is documented in Docs/PROGRESSION.md.
    /// The price shown and charged is <see cref="VehicleEconomy.Price"/>: this table wins over the catalogue's old placeholder cost.
    /// </summary>
    [CreateAssetMenu(menuName = "Road Builder/Progression/Vehicle Gates", fileName = "VehicleGates")]
    public class VehicleGateSO : ScriptableObject
    {
        public VehicleGate[] gates = Defaults();
        private static VehicleGateSO _cached;
        public static VehicleGateSO Load() { if (_cached == null) { _cached = Resources.Load<VehicleGateSO>("Progression/VehicleGates"); if (_cached == null) _cached = CreateInstance<VehicleGateSO>(); } return _cached; }
        public bool TryGet(string id, out VehicleGate g) { if (gates != null) for (int i = 0; i < gates.Length; i++) if (gates[i].vehicleId == id) { g = gates[i]; return true; } g = default; return false; }

        // id, tier (blueprint T1..T7), lifetime metres, coins. Anchors from the blueprint: bicycle T2 1500/500, dune buggy (jeep) T3 4000/1500, race car (rally) T4 8000/3000,
        // tank T6 28000/8000, moon rover T7 50000/12000. The others are interpolated by class and by the old catalogue cost order.
        public static VehicleGate[] Defaults()
        {
            return new[]
            {
                V("vehicle-car", 1, 0, 0), V("vehicle-sports-car", 1, 0, 600), V("vehicle-monster-truck", 5, 0, 2000),
                V("vehicle-scooter", 2, 200, 800), V("vehicle-skateboard", 2, 250, 900), V("vehicle-golf-cart", 2, 300, 1000), V("vehicle-sled", 2, 350, 1100),
                V("vehicle-shopping-cart", 2, 400, 1200), V("vehicle-toboggan", 2, 450, 1300), V("vehicle-bicycle", 2, 500, 1500), V("vehicle-snowboard", 2, 700, 1800),
                V("vehicle-handcar", 3, 800, 2200), V("vehicle-taxi", 3, 1000, 2800), V("vehicle-tuk-tuk", 3, 1100, 3000), V("vehicle-motorcycle", 3, 1200, 3400),
                V("vehicle-dune-buggy", 3, 1500, 4000),
                V("vehicle-police-car", 4, 2000, 5000), V("vehicle-quad-atv", 4, 2200, 5500), V("vehicle-tractor", 4, 2400, 6000), V("vehicle-pickup", 4, 2600, 6500),
                V("vehicle-camper", 4, 2800, 7000), V("vehicle-snowmobile", 4, 2900, 7500), V("vehicle-race-car", 4, 3000, 8000),
                V("vehicle-bus", 5, 4000, 10000), V("vehicle-fire-truck", 5, 4500, 11000), V("vehicle-semi-truck", 5, 5500, 14000), V("vehicle-hovercraft", 5, 6000, 16000),
                V("vehicle-jet-ski", 5, 6500, 18000), V("vehicle-speedboat", 6, 7000, 20000), V("vehicle-tank", 6, 8000, 28000), V("vehicle-rocket-sled", 6, 10000, 40000),
                V("vehicle-moon-rover", 7, 12000, 50000),
            };
        }
        private static VehicleGate V(string id, int tier, int m, int c) { return new VehicleGate { vehicleId = id, tier = tier, unlockMetres = m, coins = c }; }
    }
}
