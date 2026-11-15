using System;
using UnityEngine;

namespace RoadBuilderGame
{
    [Serializable]
    public struct AttackBeat
    {
        [Tooltip("Seconds after the pattern start at which the tell begins.")] public float tOffset;
        public ProjectileKind projectile;
        public BeatMode mode;
        [Tooltip("Earliest boss phase (1..3) in which this beat is used; lower phases skip it.")] public int phaseMin;
        [Tooltip("Mode used when the beat's phase is not reached or the counter gate fails.")] public BeatMode fallbackMode;
    }

    /// <summary>An ordered list of beats (hazards.md 5.5). Reusable across phases.</summary>
    [CreateAssetMenu(menuName = "Road Builder/Hazards/Attack Pattern", fileName = "AP-NEW")]
    public class AttackPatternSO : ScriptableObject
    {
        public string id = "AP-1";
        public AttackBeat[] beats = new AttackBeat[0];
        public float duration = 2.4f;
        public int maxAlive = 1;

        public static AttackPatternSO Make(string id, float duration, int maxAlive, params AttackBeat[] beats)
        {
            var p = CreateInstance<AttackPatternSO>(); p.name = p.id = id; p.duration = duration; p.maxAlive = maxAlive; p.beats = beats; return p;
        }
        public static AttackBeat B(float t, ProjectileKind k, BeatMode m, int phaseMin = 1, BeatMode? fallback = null)
        {
            return new AttackBeat { tOffset = t, projectile = k, mode = m, phaseMin = phaseMin, fallbackMode = fallback ?? m };
        }
    }
}
