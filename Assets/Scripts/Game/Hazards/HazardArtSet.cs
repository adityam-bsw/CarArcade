using UnityEngine;

namespace RoadBuilderGame
{
    /// <summary>Own smooth-style hazard sprites (Assets/Art/Smooth/Hazards, tools/art/make_hazard_art.py) referenced as data. Any null sprite falls back to the code shapes.</summary>
    [CreateAssetMenu(menuName = "Road Builder/Hazards/Art Set", fileName = "HazardArt")]
    public class HazardArtSet : ScriptableObject
    {
        public const string ResourcePath = "Stage/Hazards/HazardArt";
        [Header("RO-B0")] public Sprite robotBody, robotTrim, robotEye, robotAntenna, robotJet;
        [Header("Shared")] public Sprite tellRing, tellMarker, tellGlow, trapStrip, finishGate, vignette;
        [Tooltip("Road-Eater body per variant id (aligned with EaterVariantSO.id).")] public EaterSprite[] eaters = new EaterSprite[0];

        [System.Serializable] public struct EaterSprite { public string variantId; public Sprite body; }

        private static HazardArtSet _cached; private static bool _tried;
        public static HazardArtSet Load() { if (!_tried) { _tried = true; _cached = Resources.Load<HazardArtSet>(ResourcePath); } return _cached; }
        public Sprite Eater(string variantId) { for (int i = 0; i < eaters.Length; i++) if (eaters[i].variantId == variantId) return eaters[i].body; return null; }
    }
}
