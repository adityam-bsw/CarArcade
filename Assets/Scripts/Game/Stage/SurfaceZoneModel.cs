using UnityEngine;

namespace RoadBuilderGame
{
    /// <summary>
    /// The runtime state of one surface zone (ice, mud, glass) and its formulas from Docs/design/hazards.md section 3. Pure C#, no scene, no allocation.
    /// The manager feeds it the car's slope and bend and applies the answers through the existing hooks (target speed, motor, friction, ink, coin factor).
    /// </summary>
    public sealed class SurfaceZoneModel
    {
        public SurfaceRuleSO Rule { get; private set; }
        public bool Active => Rule != null;
        /// <summary>Seconds spent in the zone (the mud drag clock).</summary>
        public float DwellS { get; private set; }
        public float SlipLeftS { get; private set; }
        private float _rearmLeftS;
        public bool IsMud => Rule != null && Rule.dragPerSec > 0f && Rule.surfaceId == "surface-mud";
        public bool IsIce => Rule != null && Rule.surfaceId == "surface-ice";

        public void Enter(SurfaceRuleSO rule) { Rule = rule; DwellS = 0f; SlipLeftS = 0f; _rearmLeftS = 0f; }
        public void Exit() { Rule = null; DwellS = 0f; SlipLeftS = 0f; _rearmLeftS = 0f; }

        /// <summary>Extra reduction of the mud drag after <paramref name="dwellS"/> seconds: mudDragMul * min(cap, dragPerSec * t).</summary>
        public static float MudDrag(SurfaceRuleSO r, float dwellS, float mudDragMul)
        {
            return mudDragMul * Mathf.Min(r.mudDragCapMps, r.dragPerSec * Mathf.Max(0f, dwellS));
        }

        /// <summary>
        /// Mud target speed (hazards.md 3.2). <paramref name="baseTarget"/> is the unboosted target already multiplied by the surface speed factor.
        /// Result = max(floor, base - drag), and a slope above the stall angle caps it at the floor. Boost ignores drag and stall.
        /// </summary>
        public static float MudSpeed(SurfaceRuleSO r, float baseTarget, float dwellS, float mudDragMul, float uphillDeg, bool boosting)
        {
            if (boosting && r.mudBoostIgnoresDrag) return baseTarget;
            float v = Mathf.Max(r.dragFloorSpeed, baseTarget - MudDrag(r, dwellS, mudDragMul));
            if (uphillDeg > r.stallAngleDeg) v = Mathf.Min(v, r.dragFloorSpeed);
            return v;
        }

        /// <summary>Drive-off time for a bend of <paramref name="turnDeg"/> over the span (hazards.md 3.1): min(max, max * (dtheta - slip) / ramp), 0 at or under the threshold.</summary>
        public static float SlipSeconds(SurfaceRuleSO r, float turnDeg)
        {
            if (turnDeg <= r.slipAngleDeg) return 0f;
            return Mathf.Min(r.iceSlipMaxSeconds, r.iceSlipMaxSeconds * (turnDeg - r.slipAngleDeg) / Mathf.Max(0.01f, r.iceSlipRampDegrees));
        }

        /// <summary>Per frame. Advances the dwell and slip clocks (scaled <paramref name="dt"/>) and returns the target speed for this frame.</summary>
        public float Tick(float dt, float baseTarget, float mudDragMul, float uphillDeg, float turnDeg, bool boosting)
        {
            if (Rule == null) return baseTarget;
            DwellS += dt;
            if (SlipLeftS > 0f) { SlipLeftS -= dt; if (SlipLeftS <= 0f) _rearmLeftS = Rule.iceSlipRearmSeconds; }
            else if (_rearmLeftS > 0f) _rearmLeftS -= dt;
            if (IsIce && SlipLeftS <= 0f && _rearmLeftS <= 0f)
            {
                float s = SlipSeconds(Rule, turnDeg);
                if (s > 0f) SlipLeftS = s;
            }
            if (IsMud) return MudSpeed(Rule, baseTarget, DwellS, mudDragMul, uphillDeg, boosting);
            return baseTarget;
        }

        /// <summary>True while the ice slip has the drive cut (the car coasts; no damage).</summary>
        public bool DriveCut => SlipLeftS > 0f;
    }
}
