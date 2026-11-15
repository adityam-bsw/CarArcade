using UnityEngine;

namespace RoadBuilderGame
{
    /// <summary>RO-B0 numbers (hazards.md 5.7). Lives in Resources/Stage/Hazards/BossDef-RoB0.asset; a missing asset falls back to <see cref="CreateDefault"/>.</summary>
    [CreateAssetMenu(menuName = "Road Builder/Hazards/Boss Definition", fileName = "BOS-ROB0")]
    public class BossDefinitionSO : ScriptableObject
    {
        public string id = "BOS-ROB0";
        public float introSeconds = 2f;
        [Header("Hover")]
        public float hoverAhead = 6.5f, hoverAheadMin = 5.5f, hoverAheadMax = 8f;
        public float hoverHeight = 7.5f, hoverHeightMin = 6f, hoverHeightMax = 10f;
        public float hoverAx = 1.2f, hoverFx = 0.3f, hoverAy = 0.8f, hoverFy = 0.5f;
        public float followSmoothTime = 0.6f;
        public float minAboveRoof = 4.5f;
        [Header("Overload meter")]
        public float meterMax = 100f;
        public float meterPassivePerSec = 0.2f;
        public float meterDodge = 3f, meterNear = 5f, meterPerfect = 6f, meterRing = 8f, meterHitPenalty = -8f;
        public float[] phaseThresholds = { 33f, 66f };
        public float phaseTelegraph = 2f, phaseQuiet = 2.5f;
        public float lullSeconds = 3f;
        public float hitLullSeconds = 3f, hitGraceSeconds = 0.5f;
        [Header("Fairness")]
        public int maxAlive = 2;
        public float sameTypeMinSeparation = 1.5f;
        public float contactMinSeparation = 0.8f;
        public float timeoutSeconds = 105f;
        public float defeatSeconds = 4f;
        public float stationarySpeed = 1f;
        public float counterInk01 = 0.15f;
        public int counterCoins = 5;
        [Header("Rewards")]
        public int defeatCoinsFirst = 200, defeatCoinsRepeat = 100, defeatGemFirst = 1;
        public int timeoutCoinsFirst = 100, timeoutCoinsRepeat = 50;
        public int ringsPerPattern = 1;
        public string catalogUnlockId = "boss-robo-b0";
        [Header("Patterns and projectiles")]
        public ProjectileDefinitionSO bolt, bomb, roller;
        public AttackPatternSO[] pool1, pool2, pool3;

        public ProjectileDefinitionSO Projectile(ProjectileKind k) { return k == ProjectileKind.Bolt ? bolt : k == ProjectileKind.Bomb ? bomb : roller; }
        public AttackPatternSO[] Pool(int phase) { return phase <= 1 ? pool1 : phase == 2 ? pool2 : pool3; }

        /// <summary>The shipped boss: five reusable patterns in the three phase pools (hazards.md 5.5). One source for the editor builder and the tests.</summary>
        public static BossDefinitionSO CreateDefault()
        {
            var b = CreateInstance<BossDefinitionSO>(); b.name = "BOS-ROB0";
            b.bolt = ProjectileDefinitionSO.Make(ProjectileKind.Bolt); b.bomb = ProjectileDefinitionSO.Make(ProjectileKind.Bomb); b.roller = ProjectileDefinitionSO.Make(ProjectileKind.Roller);
            var B = (System.Func<float, ProjectileKind, BeatMode, int, BeatMode?, AttackBeat>)AttackPatternSO.B;
            var ap1 = AttackPatternSO.Make("AP-1", 2.4f, 1, B(0f, ProjectileKind.Bolt, BeatMode.Aimed, 1, null));
            var ap2 = AttackPatternSO.Make("AP-2", 5f, 1, B(0f, ProjectileKind.Bolt, BeatMode.Aimed, 1, null), B(2f, ProjectileKind.Bolt, BeatMode.Aimed, 1, null));
            var ap3 = AttackPatternSO.Make("AP-3", 6f, 2, B(0f, ProjectileKind.Bomb, BeatMode.Lead, 1, null), B(2.6f, ProjectileKind.Bomb, BeatMode.Direct, 2, BeatMode.Lead));
            var ap4 = AttackPatternSO.Make("AP-4", 6f, 2, B(0f, ProjectileKind.Roller, BeatMode.Aimed, 1, null), B(3.2f, ProjectileKind.Bolt, BeatMode.Aimed, 1, null));
            var ap5 = AttackPatternSO.Make("AP-5", 7.5f, 2, B(0f, ProjectileKind.Bomb, BeatMode.Direct, 2, BeatMode.Lead), B(1.4f, ProjectileKind.Roller, BeatMode.Aimed, 1, null), B(4.4f, ProjectileKind.Bolt, BeatMode.Aimed, 3, BeatMode.Aimed));
            b.pool1 = new[] { ap1, ap2, ap3 }; b.pool2 = new[] { ap2, ap3, ap4 }; b.pool3 = new[] { ap4, ap5, ap2 };
            return b;
        }
    }
}
