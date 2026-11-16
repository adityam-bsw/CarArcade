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
        [Header("Road-Eater (imported dragon, Assets/Art/Smooth/Hazards/RoadEater, 40 ppu, faces left)")]
        [Tooltip("Five whole-head chomp frames.")] public Sprite[] eaterHeads = new Sprite[0];
        [Tooltip("Trailing body segment.")] public Sprite eaterBodySegment;
        [Tooltip("Four flame frames (white with yellow outline, tinted in code).")] public Sprite[] eaterFlames = new Sprite[0];

        private static HazardArtSet _cached; private static bool _tried;
        public static HazardArtSet Load() { if (!_tried) { _tried = true; _cached = Resources.Load<HazardArtSet>(ResourcePath); } return _cached; }
    }
}
