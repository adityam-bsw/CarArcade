using UnityEngine;

namespace RoadBuilderGame
{
    /// <summary>Own smooth-style hazard sprites (Assets/Art/Smooth/Hazards, tools/art/make_hazard_art.py) referenced as data. Any null sprite falls back to the code shapes.</summary>
    [CreateAssetMenu(menuName = "Road Builder/Hazards/Art Set", fileName = "HazardArt")]
    public class HazardArtSet : ScriptableObject
    {
        public const string ResourcePath = "Stage/Hazards/HazardArt";
        [Header("RO-B0 rig (Assets/Art/Smooth/Hazards/RoB0; poses 956x958 ppu 240 share one pivot, faces left)")]
        public Sprite rob0Idle, rob0EyeOpen, rob0EyeAngry, rob0Hurt, rob0Firing, rob0Damaged;
        [Tooltip("771x779, ppu 170, about 90 percent opaque: drawn at alpha 0.35 in code.")] public Sprite rob0Shield;
        [Tooltip("Projectile art, ppu 128, about 0.85 u.")] public Sprite rob0Rock, rob0Bolt, rob0Bubble;
        [Tooltip("RoadBuilder/SpriteWhiteFlash: white silhouette for the hurt flash. Referenced here so it is never stripped.")] public Shader flashShader;
        [Header("Shared")] public Sprite tellRing, tellMarker, tellGlow, trapStrip, finishGate, vignette;
        [Header("Road-Eater (imported dragon, Assets/Art/Smooth/Hazards/RoadEater, 40 ppu, faces left)")]
        [Tooltip("Five whole-head chomp frames.")] public Sprite[] eaterHeads = new Sprite[0];
        [Tooltip("Trailing body segment.")] public Sprite eaterBodySegment;
        [Tooltip("RoadBuilder/SpriteHueShift: per-variant hue shift of the dragon art. Referenced here so it is never stripped.")] public Shader hueShader;
        [Tooltip("Four flame frames (white with yellow outline, tinted in code).")] public Sprite[] eaterFlames = new Sprite[0];

        private static HazardArtSet _cached; private static bool _tried;
        public static HazardArtSet Load() { if (!_tried) { _tried = true; _cached = Resources.Load<HazardArtSet>(ResourcePath); } return _cached; }
    }
}
