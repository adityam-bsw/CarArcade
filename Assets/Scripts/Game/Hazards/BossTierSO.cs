using System.Collections.Generic;
using UnityEngine;

namespace RoadBuilderGame
{
    public enum BossTierId { Scout, Hunter, Interceptor, Commander, Overlord }

    /// <summary>
    /// One RO-B0 tier (blueprint B2 boss tiers). Pure data: which distance it appears at, which patterns each phase draws from (ids resolved against the boss pools),
    /// the counter meter (10 points disables), hit limit, trap attack and the rewards. Lives in Resources/Stage/Hazards/Tiers; a missing asset falls back to <see cref="Defaults"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "Road Builder/Hazards/Boss Tier", fileName = "TIER-NEW")]
    public class BossTierSO : ScriptableObject
    {
        public BossTierId tier = BossTierId.Scout;
        public string id = "TIER-SCOUT";
        public string displayName = "Scout";
        [Tooltip("The tier is used when the encounter starts at or beyond this distance (m); the highest matching tier wins.")] public float minDistanceM = 0f;
        [Header("Fight")]
        public float encounterSeconds = 40f;
        public float counterPointsToDisable = 10f;
        public int maxHits = 3;
        [Tooltip("Phases that attack (1 or 3). Overlord is three-phase.")] public int phases = 3;
        [Tooltip("Attack windows (patterns) that must finish in a phase before the next phase may start.")] public int windowsPerPhase = 2;
        public bool alternatePatterns;
        public bool trapAttack;
        public float trapEverySeconds = 9f, trapTellSeconds = 1.4f, trapActiveSeconds = 2.5f, trapWidth = 3.2f;
        [Header("Counter points (disable at counterPointsToDisable)")]
        public float dodge = 0.8f, near = 1.2f, perfect = 1.6f, ring = 2f, hitPenalty = -1f, passivePerSec = 0.04f;
        [Header("Pattern ids per phase (AP-1..AP-5); empty uses the boss definition pools")]
        public string[] pattern1 = new string[0], pattern2 = new string[0], pattern3 = new string[0];
        [Header("Rewards")]
        public int coinsFirst = 500, gemsFirst = 5, firstBonusCoins = 250, coinsRepeat = 250, masteryPoints = 1;
        public string cosmeticId = "robot-cos-scout";
        public string announcerCueId = "vo.rob0.boss.defeat";

        public string[] Patterns(int phase) { return phase <= 1 ? pattern1 : phase == 2 ? pattern2 : pattern3; }

        public static BossTierSO Make(BossTierId t, string name, float minM, int phases, bool trap, bool alternate, string[] p1, string[] p2, string[] p3, float seconds, float passive)
        {
            var s = CreateInstance<BossTierSO>();
            s.tier = t; s.id = s.name = "TIER-" + t.ToString().ToUpperInvariant(); s.displayName = name; s.minDistanceM = minM; s.phases = phases; s.trapAttack = trap; s.alternatePatterns = alternate;
            s.pattern1 = p1; s.pattern2 = p2; s.pattern3 = p3; s.encounterSeconds = seconds; s.passivePerSec = passive; s.cosmeticId = "robot-cos-" + t.ToString().ToLowerInvariant();
            return s;
        }

        private static List<BossTierSO> _defaults;
        /// <summary>The five shipped tiers: Scout (first appearance, one projectile), Hunter 3000 m+ (two bursts), Interceptor 5000 m+ (projectile and road trap), Commander 8000 m+ (alternating patterns), Overlord 10000 m+ (three phases).</summary>
        public static IReadOnlyList<BossTierSO> Defaults()
        {
            if (_defaults != null) return _defaults;
            _defaults = new List<BossTierSO>
            {
                Make(BossTierId.Scout, "Scout", 0f, 3, false, false, new[] { "AP-1" }, new[] { "AP-1" }, new[] { "AP-1" }, 34f, 0.05f),
                Make(BossTierId.Hunter, "Hunter", 3000f, 3, false, false, new[] { "AP-2" }, new[] { "AP-2" }, new[] { "AP-2" }, 38f, 0.05f),
                Make(BossTierId.Interceptor, "Interceptor", 5000f, 3, true, false, new[] { "AP-2" }, new[] { "AP-2", "AP-3" }, new[] { "AP-3" }, 40f, 0.04f),
                Make(BossTierId.Commander, "Commander", 8000f, 3, true, true, new[] { "AP-2", "AP-3" }, new[] { "AP-3", "AP-4" }, new[] { "AP-4", "AP-5" }, 42f, 0.04f),
                Make(BossTierId.Overlord, "Overlord", 10000f, 3, true, false, new string[0], new string[0], new string[0], 45f, 0.03f),
            };
            return _defaults;
        }

        /// <summary>Highest tier whose minimum distance is at or below <paramref name="distanceM"/>.</summary>
        public static BossTierSO ForDistance(IReadOnlyList<BossTierSO> tiers, float distanceM)
        {
            BossTierSO best = null;
            for (int i = 0; i < tiers.Count; i++) { var t = tiers[i]; if (t != null && distanceM >= t.minDistanceM && (best == null || t.minDistanceM >= best.minDistanceM)) best = t; }
            return best ?? (tiers.Count > 0 ? tiers[0] : null);
        }

        public static BossTierSO[] LoadAll()
        {
            var res = Resources.LoadAll<BossTierSO>("Stage/Hazards/Tiers");
            if (res != null && res.Length >= 5) { System.Array.Sort(res, (a, b) => a.minDistanceM.CompareTo(b.minDistanceM)); return res; }
            var d = Defaults(); var arr = new BossTierSO[d.Count]; for (int i = 0; i < arr.Length; i++) arr[i] = d[i]; return arr;
        }
    }
}
