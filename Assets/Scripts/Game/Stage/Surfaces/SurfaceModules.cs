using System;
using UnityEngine;

namespace RoadBuilderGame
{
    /// <summary>The composable surface behaviours. A surface lists kinds; a kind never knows which surface uses it.</summary>
    public enum SurfaceModuleKind { Sliding = 0, Sinking = 1, Breakable = 2, Bounce = 3, Dissolve = 4, Oscillation = 5, SpeedPad = 6, Heat = 7, Conveyor = 8 }

    /// <summary>
    /// One entry of a surface's module list. Shared data, never mutated at runtime. a..d are documented per kind in <see cref="SurfaceModuleFactory"/>.
    /// Sliding, Sinking and Breakable read their numbers from the surface's <see cref="SurfaceRuleSO"/> when one is given (so the ice, mud and glass numbers stay in one place).
    /// </summary>
    [Serializable]
    public struct SurfaceModuleSpec
    {
        public SurfaceModuleKind kind;
        public float a, b, c, d;
        public SurfaceModuleSpec(SurfaceModuleKind kind, float a = 0f, float b = 0f, float c = 0f, float d = 0f) { this.kind = kind; this.a = a; this.b = b; this.c = c; this.d = d; }
    }

    /// <summary>Reused input block the manager fills once per frame. No allocation.</summary>
    public sealed class SurfaceFrame
    {
        public float Dt;               // scaled seconds since the last host call
        public float BaseTarget;       // unboosted target speed already multiplied by the surface speed factor
        public float MudDragMul = 1f;
        public float UphillDeg, TurnDeg;
        public bool Boosting;
        public float ZoneMetres;       // how far into the surface the front wheel is (entry platform rule)
        public bool Airborne;
        public float Aux;              // module-specific one-shot input, unused by the host
    }

    /// <summary>Lifecycle of one surface behaviour. Instances own all runtime state; <see cref="SurfaceModuleSpec"/> and the rule stay read-only.</summary>
    public interface IRoadBehaviour
    {
        /// <summary>True for modules whose formula is per frame (ice, mud, as before the refactor); others tick at 20 Hz with the accumulated dt.</summary>
        bool TickEveryFrame { get; }
        bool DriveCut { get; }                       // the motor is off (ice slip)
        float SpeedDelta { get; }                    // m/s added to the target (conveyor, speed pad)
        int Stage { get; }                           // visible state: crack stage, dissolve stage, 0 = intact
        bool Gone { get; }                           // the piece is broken or dissolved
        float Sway { get; }                          // road sway offset in metres (rope bridge)
        float TakeImpulse();                         // one-shot upward impulse in m/s (bounce); reading clears it
        void Configure(in SurfaceModuleSpec spec, SurfaceRuleSO rule);
        void OnVehicleEnter();
        void OnVehicleExit();
        /// <summary>energy is the impact strength in the projectile table's units (1 = light, 3 and above = heavy); fire marks an incendiary hit.</summary>
        void OnProjectileImpact(float energy, bool fire);
        void Tick(float dt, SurfaceFrame f);
        float ApplyTarget(float target, SurfaceFrame f);
        void Reset();
    }

    /// <summary>Default no-op behaviour so each module overrides only what it uses.</summary>
    public abstract class RoadBehaviourBase : IRoadBehaviour
    {
        public virtual bool TickEveryFrame => false;
        public virtual bool DriveCut => false;
        public virtual float SpeedDelta => 0f;
        public virtual int Stage => 0;
        public virtual bool Gone => false;
        public virtual float Sway => 0f;
        public virtual float TakeImpulse() => 0f;
        public abstract void Configure(in SurfaceModuleSpec spec, SurfaceRuleSO rule);
        public virtual void OnVehicleEnter() { }
        public virtual void OnVehicleExit() { }
        public virtual void OnProjectileImpact(float energy, bool fire) { }
        public virtual void Tick(float dt, SurfaceFrame f) { }
        public virtual float ApplyTarget(float target, SurfaceFrame f) => target;
        public abstract void Reset();
    }

    /// <summary>Ice: a sharp bend cuts the drive for a moment (hazards.md 3.1). Same clocks and formula as the old zone model.</summary>
    public sealed class SlidingModule : RoadBehaviourBase
    {
        private float _slipAngle = 25f, _maxS = 0.6f, _rampDeg = 20f, _rearmS = 0.6f;
        private float _slipLeft, _rearmLeft;
        public override bool TickEveryFrame => true;
        public override bool DriveCut => _slipLeft > 0f;
        public float SlipLeftS => _slipLeft;
        public override void Configure(in SurfaceModuleSpec s, SurfaceRuleSO r)
        {
            if (r != null) { _slipAngle = r.slipAngleDeg; _maxS = r.iceSlipMaxSeconds; _rampDeg = r.iceSlipRampDegrees; _rearmS = r.iceSlipRearmSeconds; }
            else { _slipAngle = s.a; _maxS = s.b; _rampDeg = s.c; _rearmS = s.d; }
            Reset();
        }
        public static float SlipSeconds(float slipAngle, float maxS, float rampDeg, float turnDeg)
        {
            if (turnDeg <= slipAngle) return 0f;
            return Mathf.Min(maxS, maxS * (turnDeg - slipAngle) / Mathf.Max(0.01f, rampDeg));
        }
        public override void Tick(float dt, SurfaceFrame f)
        {
            if (_slipLeft > 0f) { _slipLeft -= dt; if (_slipLeft <= 0f) _rearmLeft = _rearmS; }
            else if (_rearmLeft > 0f) _rearmLeft -= dt;
            if (_slipLeft <= 0f && _rearmLeft <= 0f)
            {
                float s = SlipSeconds(_slipAngle, _maxS, _rampDeg, f.TurnDeg);
                if (s > 0f) _slipLeft = s;
            }
        }
        public override void Reset() { _slipLeft = 0f; _rearmLeft = 0f; }
    }

    /// <summary>Mud and honey: suction that grows with time in the zone (hazards.md 3.2). Boost ignores it.</summary>
    public sealed class SinkingModule : RoadBehaviourBase
    {
        private float _dragPerSec = 1f, _cap = 2f, _floor = 3f, _stallDeg = 18f; private bool _boostIgnores = true;
        private float _dwell;
        public override bool TickEveryFrame => true;
        public float DwellS => _dwell;
        public override void Configure(in SurfaceModuleSpec s, SurfaceRuleSO r)
        {
            if (r != null) { _dragPerSec = r.dragPerSec; _cap = r.mudDragCapMps; _floor = r.dragFloorSpeed; _stallDeg = r.stallAngleDeg; _boostIgnores = r.mudBoostIgnoresDrag; }
            else { _dragPerSec = s.a; _cap = Mathf.Min(2f, s.b); _floor = s.c; _stallDeg = s.d; _boostIgnores = true; }
            Reset();
        }
        public static float Drag(float dragPerSec, float cap, float dwellS, float mudDragMul) { return mudDragMul * Mathf.Min(cap, dragPerSec * Mathf.Max(0f, dwellS)); }
        public override void Tick(float dt, SurfaceFrame f) { _dwell += dt; }
        public override float ApplyTarget(float target, SurfaceFrame f)
        {
            if (f.Boosting && _boostIgnores) return target;
            float v = Mathf.Max(_floor, target - Drag(_dragPerSec, _cap, _dwell, f.MudDragMul));
            if (f.UphillDeg > _stallDeg) v = Mathf.Min(v, _floor);
            return v;
        }
        public override void Reset() { _dwell = 0f; }
    }

    /// <summary>
    /// Glass (hazards.md 3.3): the first safeEntryMetres never crack; after that a dwell clock arms the piece: hairline (stage 1), web (2), drop (3, Gone).
    /// A heavy impact (energy at or above heavyEnergy) jumps to the next stage. A vehicle that keeps moving leaves before the drop.
    /// </summary>
    public sealed class BreakableModule : RoadBehaviourBase
    {
        private float _hair = 0.8f, _web = 1.6f, _drop = 2.6f, _safeEntryM = 6f, _heavy = 3f;
        private float _dwell; private bool _on;
        public override int Stage => _dwell >= _drop ? 3 : _dwell >= _web ? 2 : _dwell >= _hair ? 1 : 0;
        public override bool Gone => _dwell >= _drop;
        public override void Configure(in SurfaceModuleSpec s, SurfaceRuleSO r)
        {
            if (r != null) { _hair = r.glassDwellHairline; _web = r.glassDwellWeb; _drop = r.glassDwellDrop; _safeEntryM = r.safeEntryPlatformMetres; }
            else { _hair = 0.8f; _web = 1.6f; _drop = 2.6f; _safeEntryM = 6f; }
            if (s.a > 0f) _heavy = s.a;
            Reset();
        }
        public override void OnVehicleEnter() { _on = true; }
        public override void OnVehicleExit() { _on = false; }
        public override void OnProjectileImpact(float energy, bool fire)
        {
            if (energy < _heavy) return;
            _dwell = _dwell < _hair ? _hair : _dwell < _web ? _web : Mathf.Max(_dwell, _drop);   // a heavy hit steps the crack up one stage
        }
        public override void Tick(float dt, SurfaceFrame f)
        {
            if (_on && f.ZoneMetres >= _safeEntryM && _dwell < _drop) _dwell += dt;
        }
        public override void Reset() { _dwell = 0f; _on = false; }
    }

    /// <summary>Spring pad: every entry gives one upward impulse, then a short cooldown. a = impulse m/s, b = cooldown s.</summary>
    public sealed class BounceModule : RoadBehaviourBase
    {
        private float _impulse = 6f, _cooldown = 0.5f, _cdLeft, _pending;
        public override void Configure(in SurfaceModuleSpec s, SurfaceRuleSO r) { _impulse = s.a > 0f ? s.a : 6f; _cooldown = s.b; Reset(); }
        public override void OnVehicleEnter() { if (_cdLeft <= 0f) { _pending = _impulse; _cdLeft = _cooldown; } }
        public override float TakeImpulse() { float p = _pending; _pending = 0f; return p; }
        public override void Tick(float dt, SurfaceFrame f) { if (_cdLeft > 0f) _cdLeft -= dt; }
        public override void Reset() { _cdLeft = 0f; _pending = 0f; }
    }

    /// <summary>Cloud: dissolves after the vehicle has stood on it for a seconds (stage 1 at half, Gone at a). b = seconds to re-form after leaving, 0 = never in this run.</summary>
    public sealed class DissolveModule : RoadBehaviourBase
    {
        private float _after = 1.2f, _reform = 0f, _dwell, _goneFor; private bool _on;
        public override int Stage => _dwell >= _after ? 2 : _dwell >= _after * 0.5f ? 1 : 0;
        public override bool Gone => _dwell >= _after;
        public override void Configure(in SurfaceModuleSpec s, SurfaceRuleSO r) { _after = s.a > 0f ? s.a : 1.2f; _reform = s.b; Reset(); }
        public override void OnVehicleEnter() { _on = true; }
        public override void OnVehicleExit() { _on = false; }
        public override void Tick(float dt, SurfaceFrame f)
        {
            if (_on && _dwell < _after) _dwell += dt;
            else if (_dwell >= _after && _reform > 0f) { _goneFor += dt; if (_goneFor >= _reform) { _dwell = 0f; _goneFor = 0f; } }
        }
        public override void Reset() { _dwell = 0f; _goneFor = 0f; _on = false; }
    }

    /// <summary>Rope bridge sway. a = amplitude metres, b = frequency Hz, c = extra amplitude per m/s of speed is not used; amplitude grows with the vehicle on it.</summary>
    public sealed class OscillationModule : RoadBehaviourBase
    {
        private float _amp = 0.12f, _hz = 0.8f, _phase, _on;
        public override float Sway => _amp * _on * Mathf.Sin(_phase);
        public override void Configure(in SurfaceModuleSpec s, SurfaceRuleSO r) { _amp = s.a > 0f ? s.a : 0.12f; _hz = s.b > 0f ? s.b : 0.8f; Reset(); }
        public override void OnVehicleEnter() { _on = 1f; }
        public override void OnVehicleExit() { _on = 0f; }
        public override void Tick(float dt, SurfaceFrame f) { _phase += dt * _hz * 6.2831853f; if (_phase > 6.2831853f) _phase -= 6.2831853f; }
        public override void Reset() { _phase = 0f; _on = 0f; }
    }

    /// <summary>Neon pad: a speed bonus on entry for b seconds. a = bonus m/s.</summary>
    public sealed class SpeedPadModule : RoadBehaviourBase
    {
        private float _bonus = 2.5f, _durS = 1.2f, _left;
        public override float SpeedDelta => _left > 0f ? _bonus : 0f;
        public override void Configure(in SurfaceModuleSpec s, SurfaceRuleSO r) { _bonus = s.a > 0f ? s.a : 2.5f; _durS = s.b > 0f ? s.b : 1.2f; Reset(); }
        public override void OnVehicleEnter() { _left = _durS; }
        public override void Tick(float dt, SurfaceFrame f) { if (_left > 0f) _left -= dt; }
        public override void Reset() { _left = 0f; }
    }

    /// <summary>Wooden bridge: an incendiary hit sets it burning for a seconds; while burning Stage is 1, afterwards the planks are Gone. b = seconds of fire before collapse.</summary>
    public sealed class HeatModule : RoadBehaviourBase
    {
        private float _burnS = 2.5f, _left, _collapseS = 4f, _burnt; private bool _burning;
        public override int Stage => _burning ? 1 : 0;
        public override bool Gone => _burnt >= _collapseS;
        public override void Configure(in SurfaceModuleSpec s, SurfaceRuleSO r) { _burnS = s.a > 0f ? s.a : 2.5f; _collapseS = s.b > 0f ? s.b : 4f; Reset(); }
        public override void OnProjectileImpact(float energy, bool fire) { if (fire) { _burning = true; _left = _burnS; } }
        public override void Tick(float dt, SurfaceFrame f)
        {
            if (!_burning) return;
            _burnt += dt; _left -= dt;
            if (_left <= 0f && _burnt < _collapseS) _left = 0f;
        }
        public override void Reset() { _burning = false; _left = 0f; _burnt = 0f; }
    }

    /// <summary>Conveyor: adds a constant along-road speed, signed (negative runs against the car). a = m/s.</summary>
    public sealed class ConveyorModule : RoadBehaviourBase
    {
        private float _mps = 1.5f; private bool _on;
        public override float SpeedDelta => _on ? _mps : 0f;
        public override void Configure(in SurfaceModuleSpec s, SurfaceRuleSO r) { _mps = Mathf.Abs(s.a) > 0.001f ? s.a : 1.5f; Reset(); }
        public override void OnVehicleEnter() { _on = true; }
        public override void OnVehicleExit() { _on = false; }
        public override void Reset() { _on = false; }
    }

    /// <summary>Kind to module instance. A table of constructors, not a switch on surface type.</summary>
    public static class SurfaceModuleFactory
    {
        public const int KindCount = 9;
        private static readonly Func<IRoadBehaviour>[] Ctors =
        {
            () => new SlidingModule(), () => new SinkingModule(), () => new BreakableModule(), () => new BounceModule(), () => new DissolveModule(),
            () => new OscillationModule(), () => new SpeedPadModule(), () => new HeatModule(), () => new ConveyorModule(),
        };
        public static IRoadBehaviour Create(SurfaceModuleKind kind) { return Ctors[(int)kind](); }

        /// <summary>The modules the three event surfaces had before the refactor, used only when a rule has no behaviour set assigned yet.</summary>
        public static SurfaceModuleSpec[] LegacySpecs(SurfaceRuleSO rule)
        {
            if (rule == null) return Array.Empty<SurfaceModuleSpec>();
            if (rule.surfaceId == "surface-ice") return new[] { new SurfaceModuleSpec(SurfaceModuleKind.Sliding) };
            if (rule.surfaceId == "surface-mud") return new[] { new SurfaceModuleSpec(SurfaceModuleKind.Sinking) };
            if (rule.surfaceId == "surface-glass") return new[] { new SurfaceModuleSpec(SurfaceModuleKind.Breakable) };
            return Array.Empty<SurfaceModuleSpec>();
        }
    }
}
