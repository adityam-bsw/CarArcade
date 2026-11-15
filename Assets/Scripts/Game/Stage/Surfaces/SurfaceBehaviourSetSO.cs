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
            return ok;
        }
    }
}
