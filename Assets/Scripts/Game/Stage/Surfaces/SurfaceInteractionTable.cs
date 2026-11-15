using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoadBuilderGame
{
    /// <summary>What a persistent projectile effect does to the road under it, as a small set of reusable patch kinds (no switch on surface type).</summary>
    public enum SurfacePatchKind { None = 0, Impact = 1, Mound = 2, Slippery = 3, Crater = 4, Burn = 5, Sticky = 6, Spike = 7, Charge = 8, Fade = 9, Snag = 10, Acid = 11, Lift = 12, GravityWarp = 13 }

    /// <summary>One projectile-on-surface rule. surfaceId "*" matches any surface; a row for the exact surface wins over "*". Patch None cancels the effect on that surface (water puts out fire).</summary>
    [Serializable]
    public struct ProjectileSurfaceRow
    {
        public string surfaceId;
        public ProjectileSurfaceEffect effect;
        public SurfacePatchKind patch;
        [Tooltip("Impact: energy (3 or more is heavy). Other kinds: 1 = the default strength, scales the numbers.")] public float strength;
        public string note;
    }

    /// <summary>How the Road-Eater and other monsters behave on a surface. Data only; <see cref="IMonsterSurfaceQuery"/> hands it to section B.</summary>
    [Serializable]
    public struct MonsterSurfaceRow
    {
        public string surfaceId;
        [Range(0.3f, 1.5f), Tooltip("Multiplier on the monster's speed while on this surface (below 1 = slowed).")] public float slowMul;
        [Tooltip("Seconds the monster is stunned when it crosses the piece (once per piece).")] public float stunSeconds;
        [Range(0.5f, 2f), Tooltip("Multiplier on how fast the Road-Eater eats this road (above 1 = eats faster).")] public float eatMul;
        [Tooltip("The stun only applies when the piece is at this stage or higher (0 = always; electric rail charged = 2).")] public int stunStage;
        public string note;
    }

    /// <summary>The numbers a monster reads for one spot of road. No allocation.</summary>
    public struct MonsterSurfaceEffect
    {
        public float SlowMul, StunS, EatMul;
        public static MonsterSurfaceEffect None { get { return new MonsterSurfaceEffect { SlowMul = 1f, EatMul = 1f }; } }
    }

    /// <summary>Section B calls this to let surfaces slow, stun or speed up the Road-Eater. Implemented by <see cref="SurfaceRuntime"/>.</summary>
    public interface IMonsterSurfaceQuery
    {
        MonsterSurfaceEffect MonsterAt(float worldX);
    }

    /// <summary>Projectile and monster interaction tables for all surfaces (blueprint C2 and B2). Pure data, lookups scan small arrays and allocate nothing.</summary>
    [CreateAssetMenu(menuName = "Road Builder/Stage/Surface Interactions", fileName = "SurfaceInteractions")]
    public class SurfaceInteractionTable : ScriptableObject
    {
        public ProjectileSurfaceRow[] projectileRows = new ProjectileSurfaceRow[0];
        public MonsterSurfaceRow[] monsterRows = new MonsterSurfaceRow[0];

        public bool TryPatch(string surfaceId, ProjectileSurfaceEffect effect, out SurfacePatchKind kind, out float strength)
        {
            kind = SurfacePatchKind.None; strength = 1f;
            if (projectileRows == null) return false;
            int any = -1;
            for (int i = 0; i < projectileRows.Length; i++)
            {
                var r = projectileRows[i];
                if (r.effect != effect) continue;
                if (r.surfaceId == surfaceId && !string.IsNullOrEmpty(surfaceId)) { kind = r.patch; strength = r.strength > 0f ? r.strength : 1f; return true; }
                if (r.surfaceId == "*") any = i;
            }
            if (any >= 0) { kind = projectileRows[any].patch; strength = projectileRows[any].strength > 0f ? projectileRows[any].strength : 1f; return true; }
            return false;
        }

        public bool TryMonster(string surfaceId, out MonsterSurfaceRow row)
        {
            row = default;
            if (monsterRows == null || string.IsNullOrEmpty(surfaceId)) return false;
            for (int i = 0; i < monsterRows.Length; i++) if (monsterRows[i].surfaceId == surfaceId) { row = monsterRows[i]; return true; }
            return false;
        }

        public bool Validate(List<string> errors)
        {
            bool ok = true;
            void Fail(string m) { ok = false; if (errors != null) errors.Add(name + ": " + m); }
            var seen = new HashSet<string>();
            if (projectileRows != null)
                foreach (var r in projectileRows)
                {
                    if (string.IsNullOrEmpty(r.surfaceId)) Fail("projectile row without surfaceId");
                    if (!seen.Add(r.surfaceId + "|" + r.effect)) Fail("duplicate projectile row " + r.surfaceId + "|" + r.effect);
                }
            var seenM = new HashSet<string>();
            if (monsterRows != null)
                foreach (var m in monsterRows)
                {
                    if (string.IsNullOrEmpty(m.surfaceId)) Fail("monster row without surfaceId");
                    else if (!seenM.Add(m.surfaceId)) Fail("duplicate monster row " + m.surfaceId);
                    if (m.slowMul < 0.3f || m.slowMul > 1.5f) Fail(m.surfaceId + ": slowMul out of 0.3..1.5");
                    if (m.stunSeconds < 0f || m.stunSeconds > 3f) Fail(m.surfaceId + ": stunSeconds out of 0..3");
                }
            return ok;
        }
    }

    /// <summary>Finds the behaviour set of a surface id. Loads every set under Resources/Stage/Surfaces once.</summary>
    public static class SurfaceBehaviourLibrary
    {
        private static Dictionary<string, SurfaceBehaviourSetSO> _byId;
        private static SurfaceInteractionTable _table;

        public static SurfaceBehaviourSetSO Find(string surfaceId)
        {
            if (string.IsNullOrEmpty(surfaceId)) return null;
            if (_byId == null)
            {
                _byId = new Dictionary<string, SurfaceBehaviourSetSO>();
                foreach (var so in Resources.LoadAll<SurfaceBehaviourSetSO>("Stage/Surfaces")) if (so != null && !string.IsNullOrEmpty(so.surfaceId)) _byId[so.surfaceId] = so;
            }
            _byId.TryGetValue(surfaceId, out var set);
            return set;
        }

        public static SurfaceInteractionTable Interactions { get { return _table != null ? _table : (_table = Resources.Load<SurfaceInteractionTable>("Stage/Surfaces/SurfaceInteractions")); } }

        public static Sprite Icon(string iconId) { return string.IsNullOrEmpty(iconId) ? null : Resources.Load<Sprite>("Stage/SurfaceIcons/" + iconId); }

        /// <summary>For tests and the editor after a rebuild.</summary>
        public static void Invalidate() { _byId = null; _table = null; }
    }
}
