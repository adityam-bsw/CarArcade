using System.Collections.Generic;
using UnityEngine;

namespace RoadBuilderGame
{
    /// <summary>The module list and hint text of one catalogue surface. Pure data; the runtime state lives in the modules of <see cref="SurfaceBehaviourHost"/>.</summary>
    [CreateAssetMenu(menuName = "Road Builder/Stage/Surface Behaviour Set", fileName = "BHV-NEW")]
    public class SurfaceBehaviourSetSO : ScriptableObject
    {
        public string surfaceId = "";
        public SurfaceModuleSpec[] modules = new SurfaceModuleSpec[0];
        [Tooltip("Full first-time hint, 3 seconds at most when read.")] public string hintFull = "";
        [Tooltip("Second-time hint, a few words next to the icon.")] public string hintShort = "";
        [Tooltip("Icon id for the second sighting (a sprite name in the catalogue), empty for none.")] public string hintIcon = "";

        [Header("Runtime on the road (data, never hardcoded)")]
        [Range(2f, 8f), Tooltip("Length of one road piece: the unit that cracks, dissolves, burns or fades on its own.")] public float pieceLengthM = 4f;
        [Tooltip("The first metres of the run on this surface never break (entry platform).")] public float safeEntryM = 6f;
        public Color warnTint = new Color(0.9f, 0.82f, 0.7f, 1f);
        public Color dangerTint = new Color(0.7f, 0.45f, 0.4f, 1f);
        [Tooltip("Short verb of the 3-second tutorial (the action the player does).")] public string verb = "";
        [Tooltip("Consequence of the 3-second tutorial.")] public string consequence = "";

        public bool Validate(List<string> errors)
        {
            bool ok = true;
            void Fail(string m) { ok = false; if (errors != null) errors.Add(name + ": " + m); }
            if (string.IsNullOrEmpty(surfaceId)) Fail("surfaceId is empty");
            if (modules == null || modules.Length == 0) Fail("no modules");
            else
            {
                var seen = new HashSet<int>();
                foreach (var m in modules) if (!seen.Add((int)m.kind)) Fail("module " + m.kind + " listed twice");
            }
            if (!string.IsNullOrEmpty(hintFull) && hintFull.Length > 60) Fail("hintFull longer than 60 characters (3 s read)");
            if (string.IsNullOrEmpty(hintFull) || string.IsNullOrEmpty(hintShort) || string.IsNullOrEmpty(hintIcon)) Fail("tutorial needs hintFull, hintShort and hintIcon");
            return ok;
        }
    }
}
