using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoadBuilderGame
{
    public enum SurfaceCombo { Allowed = 0, Forbidden = 1, Merge = 2 }

    [Serializable]
    public struct SurfacePairRule
    {
        public string a, b;
        public SurfaceCombo combo;
        [Tooltip("Surface id the pair becomes when combo is Merge (water + lava = obsidian).")] public string mergeId;
        public string note;
    }

    /// <summary>
    /// Which surfaces may touch or stack, and what a Merge makes (blueprint C3, skill surface-behaviour-modules step 5). Pairs are unordered.
    /// Lookup scans a tiny list and allocates nothing.
    /// </summary>
    [CreateAssetMenu(menuName = "Road Builder/Stage/Surface Compatibility", fileName = "SurfaceCompatibility")]
    public class SurfaceCompatibilityDatabase : ScriptableObject
    {
        public SurfacePairRule[] pairs = new SurfacePairRule[0];

        public SurfaceCombo Query(string a, string b, out string mergeId)
        {
            mergeId = null;
            if (pairs == null) return SurfaceCombo.Allowed;
            for (int i = 0; i < pairs.Length; i++)
            {
                var p = pairs[i];
                if ((p.a == a && p.b == b) || (p.a == b && p.b == a)) { mergeId = p.mergeId; return p.combo; }
            }
            return SurfaceCombo.Allowed;
        }

        /// <summary>Fails on empty ids, self pairs, duplicate unordered pairs, a Merge without result, a result that is one of its own inputs, or an id outside <paramref name="known"/> (when given).</summary>
        public bool Validate(List<string> errors, ICollection<string> known = null)
        {
            bool ok = true;
            void Fail(string m) { ok = false; if (errors != null) errors.Add(name + ": " + m); }
            var seen = new HashSet<string>();
            if (pairs == null) return true;
            for (int i = 0; i < pairs.Length; i++)
            {
                var p = pairs[i];
                if (string.IsNullOrEmpty(p.a) || string.IsNullOrEmpty(p.b)) { Fail("pair " + i + " has an empty id"); continue; }
                if (p.a == p.b) Fail("pair " + i + " pairs " + p.a + " with itself");
                string key = string.CompareOrdinal(p.a, p.b) < 0 ? p.a + "|" + p.b : p.b + "|" + p.a;
                if (!seen.Add(key)) Fail("duplicate pair " + key);
                if (p.combo == SurfaceCombo.Merge)
                {
                    if (string.IsNullOrEmpty(p.mergeId)) Fail(key + " is a Merge without mergeId");
                    else if (p.mergeId == p.a || p.mergeId == p.b) Fail(key + " merges into one of its inputs");
                }
                if (known != null)
                {
                    if (!known.Contains(p.a)) Fail("unknown surface " + p.a);
                    if (!known.Contains(p.b)) Fail("unknown surface " + p.b);
                    if (p.combo == SurfaceCombo.Merge && !string.IsNullOrEmpty(p.mergeId) && !known.Contains(p.mergeId)) Fail("unknown merge result " + p.mergeId);
                }
            }
            return ok;
        }
    }
}
