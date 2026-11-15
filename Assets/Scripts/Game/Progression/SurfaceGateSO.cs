using System;
using UnityEngine;

namespace RoadBuilderGame
{
    /// <summary>Surface unlock table (blueprint D4): Basic 1,000-2,500 m, Unstable 3,000-5,000 m (surface tutorial), Advanced 6,000-10,000 m (5 missions), Special 12,000-18,000 m (boss reward), Exotic 20,000-30,000 m (world mastery). Coin costs are our assumption (the blueprint lists none).</summary>
    [CreateAssetMenu(menuName = "Road Builder/Progression/Surface Gates", fileName = "SurfaceGates")]
    public class SurfaceGateSO : ScriptableObject
    {
        public SurfaceGate[] gates = Defaults();
        private static SurfaceGateSO _cached;
        public static SurfaceGateSO Load() { if (_cached == null) { _cached = Resources.Load<SurfaceGateSO>("Progression/SurfaceGates"); if (_cached == null) _cached = CreateInstance<SurfaceGateSO>(); } return _cached; }
        public bool TryGet(string id, out SurfaceGate g) { if (gates != null) for (int i = 0; i < gates.Length; i++) if (gates[i].surfaceId == id) { g = gates[i]; return true; } g = default; return false; }

        public static SurfaceGate[] Defaults()
        {
            const SurfaceCategory B = SurfaceCategory.Basic, U = SurfaceCategory.Unstable, A = SurfaceCategory.Advanced, S = SurfaceCategory.Special, X = SurfaceCategory.Exotic;
            return new[]
            {
                G("surface-asphalt", B, 0, 0, ExtraGate.None, 0), G("surface-dirt", B, 0, 0, ExtraGate.None, 0), G("surface-neon-grid", B, 2500, 600, ExtraGate.None, 0),
                G("surface-gravel", B, 1000, 300, ExtraGate.None, 0), G("surface-cobblestone", B, 1200, 350, ExtraGate.None, 0), G("surface-grass-trail", B, 1400, 400, ExtraGate.None, 0),
                G("surface-sand", B, 1600, 450, ExtraGate.None, 0), G("surface-beach", B, 1800, 500, ExtraGate.None, 0), G("surface-wooden-bridge", B, 2000, 500, ExtraGate.None, 0),
                G("surface-rail-track", B, 2200, 550, ExtraGate.None, 0), G("surface-snow", B, 2500, 600, ExtraGate.None, 0),
                G("surface-mud", U, 3000, 900, ExtraGate.SurfaceTutorial, 1), G("surface-salt-flat", U, 3500, 1000, ExtraGate.SurfaceTutorial, 1), G("surface-river", U, 4000, 1200, ExtraGate.SurfaceTutorial, 1),
                G("surface-moon-dust", U, 4500, 1400, ExtraGate.SurfaceTutorial, 1), G("surface-rope-bridge", U, 5000, 1500, ExtraGate.SurfaceTutorial, 1),
                G("surface-ice", A, 6000, 2200, ExtraGate.Missions, 5), G("surface-glass", A, 7000, 2800, ExtraGate.Missions, 5), G("surface-coral", A, 8000, 3200, ExtraGate.Missions, 5),
                G("surface-magma-bridge", A, 10000, 3800, ExtraGate.Missions, 5),
                G("surface-ice-bridge", S, 12000, 5500, ExtraGate.BossDefeated, 1), G("surface-sea-water", S, 15000, 7000, ExtraGate.BossDefeated, 1), G("surface-lava-crust", S, 18000, 8500, ExtraGate.BossDefeated, 1),
                G("surface-cloud", X, 20000, 10000, ExtraGate.WorldMastery, 2), G("surface-candy", X, 25000, 12500, ExtraGate.WorldMastery, 3), G("surface-floating-islands", X, 30000, 15000, ExtraGate.WorldMastery, 4),
            };
        }
        private static SurfaceGate G(string id, SurfaceCategory c, int m, int coins, ExtraGate e, int n) { return new SurfaceGate { surfaceId = id, category = c, unlockMetres = m, coins = coins, extra = e, extraCount = n }; }
    }
}
