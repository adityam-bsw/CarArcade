using System.Collections.Generic;
using UnityEngine;

namespace RoadBuilderGame
{
    /// <summary>
    /// The numbers of one surface event (ice, mud, glass): the base profile that mirrors the catalogue surface (friction, speed, ink, coin) plus the special rule.
    /// Spec: Docs/design/hazards.md section 3 and 3.4, brief F1. Pure data. Units in field names.
    /// B1 consumes: base profile, ice slip, mud drag/floor/stall. The glass dwell and piece fields are stored for the break-state work and are not consumed yet.
    /// </summary>
    [CreateAssetMenu(menuName = "Road Builder/Stage/Surface Rule", fileName = "SRF-NEW")]
    public class SurfaceRuleSO : ScriptableObject
    {
        [Header("Identity and base profile (mirror of the catalogue entry; a test keeps them equal)")]
        public string surfaceId = "surface-ice";
        [Range(0.1f, 3f)] public float friction = 1.25f;
        [Range(0.5f, 1.5f)] public float speedFactor = 1f;
        [Range(0.5f, 2f)] public float inkFactor = 1f;
        [Range(0.5f, 2f)] public float coinFactor = 1f;
        [Tooltip("UITheme colour token name for the S2 tint (a ColorToken, never a hex).")] public string tintColorToken = "ZoneIce";
        [Range(10f, 60f)] public float warnMetres = 40f;

        [Header("Ice (hazards.md 3.1)")]
        [Tooltip("Along-slope gain going downhill. Stored; the car is motor-driven so only slip is consumed in B1.")] public float downhillBoost = 1.3f;
        public float uphillBrake = 0.7f;
        [Tooltip("A turn of the road tangent above this over the span cuts the drive.")] [Range(10f, 60f)] public float slipAngleDeg = 25f;
        [Range(2f, 8f)] public float iceSlipSpanMetres = 4f;
        [Range(0.2f, 1f)] public float iceSlipMaxSeconds = 0.6f;
        [Range(5f, 45f)] public float iceSlipRampDegrees = 20f;
        [Tooltip("Coast-free time after a slip before another can start, so one long bend is one slip.")] [Range(0f, 2f)] public float iceSlipRearmSeconds = 0.6f;

        [Header("Mud (hazards.md 3.2)")]
        [Range(0f, 3f)] public float dragPerSec = 1f;
        [Tooltip("Cap on the extra speed reduction before the vehicle multiplier. Never above 2.0.")] [Range(1f, 2f)] public float mudDragCapMps = 2f;
        [Range(0f, 6f)] public float dragFloorSpeed = 3f;
        [Range(5f, 45f)] public float stallAngleDeg = 18f;
        public bool mudBoostIgnoresDrag = true;

        [Header("Glass (hazards.md 3.3; stored, break state not built in B1)")]
        public float glassDwellHairline = 0.8f;
        public float glassDwellWeb = 1.6f;
        public float glassDwellDrop = 2.6f;
        public float glassDropAfterRearLeave = 0.6f;
        public float pieceLength = 4f;
        public float safeEntryPlatformMetres = 6f;

        public bool Validate(List<string> errors)
        {
            bool ok = true;
            void Fail(string m) { ok = false; if (errors != null) errors.Add(name + " (" + surfaceId + "): " + m); }
            if (string.IsNullOrEmpty(surfaceId)) Fail("surfaceId is empty");
            if (mudDragCapMps > 2f) Fail("mudDragCapMps above the 2.0 hard cap");
            if (dragFloorSpeed < 0f) Fail("dragFloorSpeed below 0");
            if (glassDwellDrop < 2f) Fail("glassDwellDrop below 2.0 s");
            if (glassDwellWeb - glassDwellHairline < 0.8f - 0.001f) Fail("glass steps must be at least 0.8 s apart");
            if (glassDwellDrop - glassDwellWeb < 0.8f - 0.001f) Fail("glass steps must be at least 0.8 s apart");
            if (string.IsNullOrEmpty(tintColorToken)) Fail("tintColorToken is empty");
            return ok;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            var errs = new List<string>();
            if (!Validate(errs)) foreach (var e in errs) Debug.LogWarning("[SurfaceRuleSO] " + e, this);
        }
#endif
    }
}
