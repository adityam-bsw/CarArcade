using System;
using UnityEngine;

namespace RoadBuilderGame
{
    public enum UpgradeBranch { Engine = 0, Suspension = 1, InkTank = 2, Boost = 3, Shield = 4 }

    [Serializable] public struct VehicleCostScale { public string vehicleId; public float scale; }

    [Serializable] public struct BranchBaseOverride { public string vehicleId; public UpgradeBranch branch; public float baseValue; }

    /// <summary>
    /// Shared upgrade tree (blueprint D3): five branches, five levels each. A level owned is 0..5 (0 = stock). Bonus = base x summed percent
    /// of the owned levels. Base values are per branch and can be overridden per vehicle. Replaces the hard-coded 0.8 / 3 / 30 per level formulas.
    /// </summary>
    [CreateAssetMenu(menuName = "Road Builder/Progression/Upgrade Tree", fileName = "UpgradeTree")]
    public class UpgradeTreeSO : ScriptableObject
    {
        public const int Levels = 5, Branches = 5;
        [Tooltip("Coins to buy level 1..5 of any branch.")] public int[] costs = { 500, 900, 1500, 2500, 4000 };
        [Tooltip("Fraction of the base value added by level 1..5 (economy.md section 15: about 6 percent per level).")] public float[] pctPerLevel = { 0.06f, 0.06f, 0.06f, 0.06f, 0.06f };
        [Tooltip("Base value per branch: engine max speed m/s, suspension torque, ink tank, boost seconds, shield strength.")] public float[] baseValues = { 11.5f, 34f, 100f, 1.4f, 1f };
        public BranchBaseOverride[] overrides = new BranchBaseOverride[0];
        [Tooltip("Seconds the Shield upgrade takes to recharge after it absorbed a hit, for level 1..5 (higher level recharges faster).")] public float[] shieldRechargeSeconds = { 45f, 38f, 31f, 24f, 18f };
        [Tooltip("Per-vehicle multiplier on the upgrade costs (starting values, economy-balancer owns them). Missing vehicle = 1.")]
        public VehicleCostScale[] costScales = { new VehicleCostScale { vehicleId = "vehicle-bicycle", scale = 0.8f }, new VehicleCostScale { vehicleId = "vehicle-race-car", scale = 1.25f }, new VehicleCostScale { vehicleId = "vehicle-tank", scale = 1.5f }, new VehicleCostScale { vehicleId = "vehicle-moon-rover", scale = 1.75f } };

        /// <summary>Coins to buy the next level when <paramref name="levelOwned"/> are owned; -1 when maxed.</summary>
        public int Cost(int levelOwned) { return levelOwned < 0 || levelOwned >= Levels || costs == null || levelOwned >= costs.Length ? -1 : costs[levelOwned]; }

        /// <summary>Coins for the next level of <paramref name="vehicleId"/> (per-vehicle scale, rounded to 10); -1 when maxed.</summary>
        public int Cost(int levelOwned, string vehicleId)
        {
            int c = Cost(levelOwned);
            if (c < 0 || string.IsNullOrEmpty(vehicleId) || costScales == null) return c;
            for (int i = 0; i < costScales.Length; i++)
                if (costScales[i].vehicleId == vehicleId && costScales[i].scale > 0f) return Mathf.Max(10, Mathf.RoundToInt(c * costScales[i].scale / 10f) * 10);
            return c;
        }

        /// <summary>Recharge time of the Shield upgrade at a level (1..5); 0 when the level is 0 (no shield).</summary>
        public float ShieldRecharge(int level)
        {
            if (level <= 0 || shieldRechargeSeconds == null || shieldRechargeSeconds.Length == 0) return 0f;
            return shieldRechargeSeconds[Mathf.Clamp(level, 1, shieldRechargeSeconds.Length) - 1];
        }

        public float Base(UpgradeBranch b, string vehicleId)
        {
            if (overrides != null && !string.IsNullOrEmpty(vehicleId))
                for (int i = 0; i < overrides.Length; i++) if (overrides[i].branch == b && overrides[i].vehicleId == vehicleId) return overrides[i].baseValue;
            int k = (int)b;
            return baseValues != null && k < baseValues.Length ? baseValues[k] : 0f;
        }

        public float Fraction(int level)
        {
            float f = 0f;
            int n = Mathf.Min(Mathf.Clamp(level, 0, Levels), pctPerLevel == null ? 0 : pctPerLevel.Length);
            for (int i = 0; i < n; i++) f += pctPerLevel[i];
            return f;
        }

        /// <summary>Extra value the owned levels add on top of the stock value.</summary>
        public float Bonus(UpgradeBranch b, string vehicleId, int level) { return Base(b, vehicleId) * Fraction(level); }

        private static UpgradeTreeSO _cached;
        public static UpgradeTreeSO Load()
        {
            if (_cached != null) return _cached;
            _cached = Resources.Load<UpgradeTreeSO>("Progression/UpgradeTree");
            if (_cached == null) _cached = CreateInstance<UpgradeTreeSO>();
            return _cached;
        }
    }
}
