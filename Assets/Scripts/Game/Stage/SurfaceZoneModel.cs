using UnityEngine;

namespace RoadBuilderGame
{
    /// <summary>
    /// The runtime state of one surface zone: a thin facade over <see cref="SurfaceBehaviourHost"/>. The ice, mud and glass logic lives in the modules
    /// (<see cref="SlidingModule"/>, <see cref="SinkingModule"/>, <see cref="BreakableModule"/>); the formulas are those of Docs/design/hazards.md section 3, unchanged.
    /// The manager feeds it the car's slope and bend and applies the answers through the existing hooks. Pure C#, no scene, no allocation per frame.
    /// </summary>
    public sealed class SurfaceZoneModel
    {
        private readonly SurfaceBehaviourHost _host = new SurfaceBehaviourHost();
        private readonly SurfaceFrame _frame = new SurfaceFrame();
        public SurfaceRuleSO Rule { get; private set; }
        public bool Active => Rule != null;
        public SurfaceBehaviourHost Host => _host;
        /// <summary>Seconds spent in the zone (the mud drag clock).</summary>
        public float DwellS { get { var m = _host.Find<SinkingModule>(); return m != null ? m.DwellS : _zoneS; } }
        public float SlipLeftS { get { var m = _host.Find<SlidingModule>(); return m != null ? m.SlipLeftS : 0f; } }
        private float _zoneS;
        public bool IsMud => _host.Find<SinkingModule>() != null;
        public bool IsIce => _host.Find<SlidingModule>() != null;

        public void Enter(SurfaceRuleSO rule)
        {
            Rule = rule; _zoneS = 0f;
            var specs = rule != null && rule.behaviours != null && rule.behaviours.modules != null && rule.behaviours.modules.Length > 0 ? rule.behaviours.modules : SurfaceModuleFactory.LegacySpecs(rule);
            _host.Enter(rule, specs);
        }
        public void Exit() { Rule = null; _zoneS = 0f; _host.Exit(); }

        /// <summary>Extra reduction of the mud drag after <paramref name="dwellS"/> seconds: mudDragMul * min(cap, dragPerSec * t).</summary>
        public static float MudDrag(SurfaceRuleSO r, float dwellS, float mudDragMul) { return SinkingModule.Drag(r.dragPerSec, r.mudDragCapMps, dwellS, mudDragMul); }

        /// <summary>Mud target speed (hazards.md 3.2), as a pure function for tests and tools; the module applies the same numbers.</summary>
        public static float MudSpeed(SurfaceRuleSO r, float baseTarget, float dwellS, float mudDragMul, float uphillDeg, bool boosting)
        {
            if (boosting && r.mudBoostIgnoresDrag) return baseTarget;
            float v = Mathf.Max(r.dragFloorSpeed, baseTarget - MudDrag(r, dwellS, mudDragMul));
            if (uphillDeg > r.stallAngleDeg) v = Mathf.Min(v, r.dragFloorSpeed);
            return v;
        }

        /// <summary>Drive-off time for a bend of <paramref name="turnDeg"/> over the span (hazards.md 3.1).</summary>
        public static float SlipSeconds(SurfaceRuleSO r, float turnDeg) { return SlidingModule.SlipSeconds(r.slipAngleDeg, r.iceSlipMaxSeconds, r.iceSlipRampDegrees, turnDeg); }

        /// <summary>Per frame. Advances the module clocks (scaled <paramref name="dt"/>) and returns the target speed for this frame.</summary>
        public float Tick(float dt, float baseTarget, float mudDragMul, float uphillDeg, float turnDeg, bool boosting)
        {
            if (Rule == null) return baseTarget;
            _zoneS += dt;
            var f = _frame;
            f.Dt = dt; f.BaseTarget = baseTarget; f.MudDragMul = mudDragMul; f.UphillDeg = uphillDeg; f.TurnDeg = turnDeg; f.Boosting = boosting;
            f.ZoneMetres = _zoneS * Mathf.Max(0f, baseTarget);     // distance proxy until the manager passes the real front-wheel position
            _host.Tick(f);
            return _host.TargetSpeed(f);
        }

        /// <summary>A projectile or hazard hit on the surface; modules decide what it means.</summary>
        public void ProjectileImpact(float energy, bool fire) { _host.ProjectileImpact(energy, fire); }

        /// <summary>True while the ice slip has the drive cut (the car coasts; no damage).</summary>
        public bool DriveCut => _host.DriveCut;
    }
}
