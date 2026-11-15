using UnityEngine;

namespace RoadBuilderGame
{
    /// <summary>Road-Eater numbers (Docs/design/hazards.md 2.7). Pure data; defaults are the shipped values. Lives in Resources/Stage/Hazards/MonsterDef-RoadEater.asset (a missing asset falls back to these defaults).</summary>
    [CreateAssetMenu(menuName = "Road Builder/Hazards/Monster Definition", fileName = "MON-EATER")]
    public class MonsterDefinitionSO : ScriptableObject
    {
        public string id = "MON-EATER";
        [Header("Distances (m of path)")]
        public float triggerDistance = 1000f;
        public float warnMetres = 60f;
        public float warnSeconds = 6f;
        public float startGap = 32f;
        public float targetGap = 18f;
        public float timeoutDistance = 1750f;
        [Header("Speed controller")]
        public float vFloor = 3f;
        [Range(1f, 1.25f)] public float maxSpeedRatio = 1.25f;
        public float minSpeedRatio = 0.85f;
        public float catchUpEndGap = 36f;
        public float lostTrackRatio = 0.9f;
        public float accelMax = 4f, decelMax = 5f;
        [Header("Snap")]
        public float snapGap = 6f, snapCancelGap = 8f;
        [Range(0.8f, 1.5f)] public float snapSeconds = 1f;
        public float snapAirborneMaxS = 1.5f;
        [Header("Deletion")]
        public float protectedZone = 6f;
        public float trimTickSeconds = 0.1f;
        public float trimQuantum = 1f;
        public float wholeChunkMargin = 1.5f;
        public float biteWidth = 3f;
        [Header("Pushback")]
        public float pushbackGap = 32f;
        public float pushbackFreeze = 1.5f, pushbackGrace = 1.5f;
        [Header("Surge")]
        public float surgeIntervalMin = 12f, surgeIntervalMax = 16f;
        [Range(0.8f, 2f)] public float surgeTelegraph = 1.2f;
        public float surgeDuration = 2.5f;
        public float surgeMinGap = 21f, surgeAbortGap = 12f;
        [Header("Jump delay (chew)")]
        public float jumpMinGapLength = 6f, jumpMaxGapLength = 14f;
        public float jumpDelaySeconds = 2f, jumpSlowRatio = 0.4f, jumpCooldown = 8f;
        public int jumpMaxPerChase = 4;
        [Header("Escape and timeout")]
        public float escapeGap = 45f, escapeHoldSeconds = 8f, escapeResetGap = 43f, escapeResetSeconds = 0.5f;
        public float exitSeconds = 3f;
        public int escapeCoinsFirst = 150, escapeCoinsRepeat = 75;
        public float escapeFrenzy = 0.5f;
        public int timeoutCoinsFirst = 75, timeoutCoinsRepeat = 38;
        public float timeoutFrenzy = 0.25f;
        [Header("Bands (m)")]
        public float bandCalm = 24f, bandEdge = 12f, bandDanger = 6f;
        [Header("Near miss")]
        public float nearMissLowGap = 4f, nearMissRecoverGap = 12f, nearMissWindowS = 4f, nearMissCooldownS = 10f;
        [Header("Rescue")]
        public float inkRescueThreshold = 0.15f;

        /// <summary>Gap to speed ratio (hazards.md 2.3).</summary>
        public float Ratio(float gap)
        {
            if (gap >= catchUpEndGap) return lostTrackRatio;
            if (gap <= snapGap) return minSpeedRatio;
            if (gap <= targetGap) return Mathf.Lerp(minSpeedRatio, 1f, (gap - snapGap) / (targetGap - snapGap));
            return Mathf.Lerp(1f, maxSpeedRatio, (gap - targetGap) / (catchUpEndGap - targetGap));
        }
    }
}
