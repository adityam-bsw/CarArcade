using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoadBuilderGame
{
    [Serializable]
    public struct StageCompatPair
    {
        public string idA;
        public string idB;
        public StageCompatRule rule;
        [Tooltip("Minimum clear road between the two events when one follows the other. 0 means none.")] public float adjacentMinGapM;
        [Tooltip("For Conditional, for example phase:Warn or minChaseS:20.")] public string conditionKey;
        public string reasonKey;
    }

    /// <summary>
    /// Which events may overlap or sit next to each other. Symmetric (one entry covers both orders). A pair that is not listed is
    /// forbidden (default deny), so a new event cannot stack by accident. Spec: Docs/design/stage-events.md sections 5 and 9.3.
    /// </summary>
    [CreateAssetMenu(menuName = "Road Builder/Stage/Compatibility Matrix", fileName = "EventCompatibilityMatrix")]
    public class EventCompatibilityMatrix : ScriptableObject
    {
        public StageCompatPair[] pairs = new StageCompatPair[0];

        /// <summary>Finds the entry for the two ids in either order. Allocation free.</summary>
        public bool TryGetPair(string a, string b, out StageCompatPair pair)
        {
            if (pairs != null)
                for (int i = 0; i < pairs.Length; i++)
                {
                    var p = pairs[i];
                    if ((p.idA == a && p.idB == b) || (p.idA == b && p.idB == a)) { pair = p; return true; }
                }
            pair = default;
            return false;
        }

        /// <summary>True only for an explicit Allow. Forbid, Conditional (the caller decides) and a missing pair all say no.</summary>
        public bool CanOverlap(string a, string b)
        {
            return TryGetPair(a, b, out var p) && p.rule == StageCompatRule.Allow;
        }

        public float AdjacentMinGapM(string a, string b)
        {
            return TryGetPair(a, b, out var p) ? p.adjacentMinGapM : 0f;
        }

        /// <summary>Symmetry (no contradicting duplicate) and coverage (every event id appears in some pair, otherwise it is forbidden with everything by default).</summary>
        public bool Validate(IList<StageEventDefinition> events, List<string> errors)
        {
            bool ok = true;
            void Fail(string msg) { ok = false; if (errors != null) errors.Add("Matrix " + name + ": " + msg); }
            if (pairs != null)
            {
                for (int i = 0; i < pairs.Length; i++)
                {
                    if (string.IsNullOrEmpty(pairs[i].idA) || string.IsNullOrEmpty(pairs[i].idB)) Fail("pair " + i + " has an empty id");
                    if (pairs[i].idA == pairs[i].idB && !string.IsNullOrEmpty(pairs[i].idA)) Fail("pair " + i + " pairs " + pairs[i].idA + " with itself");
                    for (int j = i + 1; j < pairs.Length; j++)
                    {
                        bool same = (pairs[i].idA == pairs[j].idA && pairs[i].idB == pairs[j].idB) || (pairs[i].idA == pairs[j].idB && pairs[i].idB == pairs[j].idA);
                        if (same && (pairs[i].rule != pairs[j].rule || !Mathf.Approximately(pairs[i].adjacentMinGapM, pairs[j].adjacentMinGapM)))
                            Fail("pairs " + i + " and " + j + " describe " + pairs[i].idA + "/" + pairs[i].idB + " with different rules (matrix must be symmetric)");
                    }
                }
            }
            if (events != null)
                for (int e = 0; e < events.Count; e++)
                {
                    if (events[e] == null) continue;
                    string id = events[e].id;
                    bool found = false;
                    if (pairs != null) for (int i = 0; i < pairs.Length && !found; i++) found = pairs[i].idA == id || pairs[i].idB == id;
                    if (!found && events.Count > 1) Fail("event " + id + " is in no pair");
                    // The event's own mirror lists must agree with the matrix.
                    var f = events[e].forbiddenWith;
                    if (f != null)
                        foreach (var other in f)
                        {
                            if (TryGetPair(id, other, out var p) && p.rule == StageCompatRule.Allow)
                                Fail(id + ".forbiddenWith lists " + other + " but the matrix allows the pair");
                        }
                }
            return ok;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            var errs = new List<string>();
            if (!Validate(null, errs)) foreach (var e in errs) Debug.LogWarning("[EventCompatibilityMatrix] " + e, this);
        }
#endif
    }
}
