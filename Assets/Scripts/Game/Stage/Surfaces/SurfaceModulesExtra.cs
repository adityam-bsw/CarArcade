using UnityEngine;

namespace RoadBuilderGame
{
    // Section C gap work: the behaviour kinds added after the first nine. Same rules as SurfaceModules.cs: pure C#, no scene, no allocation after the first Configure,
    // numbers come from the SurfaceModuleSpec a..d (documented per class). Cyclic modules read the shared run clock (SurfaceFrame.TimeS) so a row of pieces stays in step.

    /// <summary>Magnetic rail: a metallic vehicle is pulled down onto the rail and gets a small speed assist. a = pull m/s^2 (7), b = speed assist m/s (0.8). Non-metallic vehicles feel nothing.</summary>
    public sealed class MagneticModule : RoadBehaviourBase
    {
        private float _pull = 7f, _assist = 0.8f; private bool _on, _metal;
        public override float VerticalAccel => _on && _metal ? -_pull : 0f;
        public override float SpeedDelta => _on && _metal ? _assist : 0f;
        public override int Stage => _on && _metal ? 1 : 0;
        public override void Configure(in SurfaceModuleSpec s, SurfaceRuleSO r) { _pull = s.a > 0f ? s.a : 7f; _assist = s.b > 0f ? s.b : 0.8f; Reset(); }
        public override void OnVehicleEnter() { _on = true; }
        public override void OnVehicleExit() { _on = false; }
        public override void Tick(float dt, SurfaceFrame f) { _metal = f.Metallic; }
        public override void Reset() { _on = false; _metal = false; }
    }

    /// <summary>Acid: durability drains while the vehicle is on it and recovers slowly off it. At zero one durability hit is raised and durability resets to half. a = seconds to drain (6), b = recovery per second (0.4). Speed shrinks down to 75 percent with durability.</summary>
    public sealed class DecayModule : RoadBehaviourBase
    {
        private float _drainS = 6f, _recover = 0.4f, _dur = 1f; private bool _on; private int _hits;
        public float Durability => _dur;
        public override int Stage => _dur < 0.35f ? 2 : _dur < 0.7f ? 1 : 0;
        public override int TakeDamage() { int h = _hits; _hits = 0; return h; }
        public override void Configure(in SurfaceModuleSpec s, SurfaceRuleSO r) { _drainS = s.a > 0f ? s.a : 6f; _recover = s.b > 0f ? s.b : 0.4f; Reset(); }
        public override void OnVehicleEnter() { _on = true; }
        public override void OnVehicleExit() { _on = false; }
        public override void OnHazardEffect(SurfaceHazardKind kind, float strength) { if (kind == SurfaceHazardKind.Acid) _dur = Mathf.Max(0f, _dur - 0.3f * Mathf.Max(0.5f, strength)); }
        public override void Tick(float dt, SurfaceFrame f)
        {
            if (_on) _dur -= dt * (f.Heavy ? 1.4f : 1f) / _drainS;
            else _dur = Mathf.Min(1f, _dur + dt * _recover);
            if (_dur <= 0f) { _hits++; _dur = 0.5f; }
        }
        public override float ApplyTarget(float target, SurfaceFrame f) { return target * (0.75f + 0.25f * _dur); }
        public override void Reset() { _dur = 1f; _on = false; _hits = 0; }
    }

    /// <summary>Oil: the grip is low and the vehicle rotates more under drive torque. a = max nose-up angular acceleration deg/s^2 (70), b = grip multiplier (0.55). An incendiary hit sets it alight for 3 s: a burning oil piece raises one durability hit after 1.5 s under the vehicle.</summary>
    public sealed class TorqueModule : RoadBehaviourBase
    {
        private float _maxTorque = 70f, _grip = 0.55f, _t, _torque, _fire, _burnOn; private bool _on; private int _hits;
        public override float Torque => _torque;
        public override float GripMul => _on ? _grip : 1f;
        public override int Stage => _fire > 0f ? 2 : 0;
        public override int TakeDamage() { int h = _hits; _hits = 0; return h; }
        public override void Configure(in SurfaceModuleSpec s, SurfaceRuleSO r) { _maxTorque = s.a > 0f ? s.a : 70f; _grip = s.b > 0f ? s.b : 0.55f; Reset(); }
        public override void OnVehicleEnter() { _on = true; }
        public override void OnVehicleExit() { _on = false; _torque = 0f; }
        public override void OnProjectileImpact(float energy, bool fire) { if (fire) _fire = 3f; }
        public override void OnHazardEffect(SurfaceHazardKind kind, float strength) { if (kind == SurfaceHazardKind.Fire) _fire = 3f; }
        public override void Tick(float dt, SurfaceFrame f)
        {
            _t += dt;
            float drive = Mathf.Clamp01((f.BaseTarget - f.Speed) / 3f) * (f.Boosting ? 1.6f : 1f);                 // more motor torque, more rotation
            _torque = _on ? _maxTorque * drive * (0.6f + 0.4f * Mathf.Sin(_t * 2.3f)) : 0f;
            if (_fire > 0f)
            {
                _fire -= dt;
                if (_on) { _burnOn += dt; if (_burnOn >= 1.5f) { _burnOn = 0f; _hits++; } } else _burnOn = 0f;
            }
        }
        public override void Reset() { _t = 0f; _torque = 0f; _fire = 0f; _burnOn = 0f; _on = false; _hits = 0; }
    }

    /// <summary>Gravel: loose stones. Small random rocking that grows with speed, and a little less grip. a = max rocking deg/s^2 (45), b = grip multiplier (0.85).</summary>
    public sealed class InstabilityModule : RoadBehaviourBase
    {
        private float _max = 45f, _grip = 0.85f, _t, _torque; private bool _on;
        public override float Torque => _torque;
        public override float GripMul => _on ? _grip : 1f;
        public override void Configure(in SurfaceModuleSpec s, SurfaceRuleSO r) { _max = s.a > 0f ? s.a : 45f; _grip = s.b > 0f ? s.b : 0.85f; Reset(); }
        public override void OnVehicleEnter() { _on = true; }
        public override void OnVehicleExit() { _on = false; _torque = 0f; }
        public override void Tick(float dt, SurfaceFrame f)
        {
            _t += dt;
            _torque = _on ? _max * Mathf.Clamp01(f.Speed / 10f) * Mathf.Sin(_t * 7.3f) * Mathf.Sin(_t * 3.1f + 1f) : 0f;
        }
        public override void Reset() { _t = 0f; _torque = 0f; _on = false; }
    }

    /// <summary>Sticky candy: the wheels attach (slower, very grippy) then release with a small burst, then a pause before it can grab again. a = attach s (1.2), b = release burst m/s (2), c = attached slow m/s (2), d = rearm s (2.5).</summary>
    public sealed class StickyModule : RoadBehaviourBase
    {
        private enum Phase { Idle, Attached, Release }
        private float _attachS = 1.2f, _burst = 2f, _slow = 2f, _rearmS = 2.5f, _left; private Phase _p; private bool _on;
        public override float SpeedDelta => _p == Phase.Attached ? -_slow : _p == Phase.Release ? _burst : 0f;
        public override float GripMul => _p == Phase.Attached ? 1.8f : 1f;
        public override int Stage => _p == Phase.Attached ? 1 : 0;
        public override void Configure(in SurfaceModuleSpec s, SurfaceRuleSO r) { _attachS = s.a > 0f ? s.a : 1.2f; _burst = s.b > 0f ? s.b : 2f; _slow = s.c > 0f ? s.c : 2f; _rearmS = s.d > 0f ? s.d : 2.5f; Reset(); }
        public override void OnVehicleEnter() { _on = true; if (_p == Phase.Idle && _left <= 0f) { _p = Phase.Attached; _left = _attachS; } }
        public override void OnVehicleExit() { _on = false; }
        public override void OnHazardEffect(SurfaceHazardKind kind, float strength) { }
        public override void Tick(float dt, SurfaceFrame f)
        {
            if (_left > 0f) _left -= dt;
            if (_p == Phase.Attached && _left <= 0f) { _p = Phase.Release; _left = 0.4f; }
            else if (_p == Phase.Release && _left <= 0f) { _p = Phase.Idle; _left = _rearmS; }
            else if (_p == Phase.Idle && _left <= 0f && _on) { _p = Phase.Attached; _left = _attachS; }
        }
        public override void Reset() { _p = Phase.Idle; _left = 0f; _on = false; }
    }

    /// <summary>Metal plates: standing on them heats them; hot plates shift (a small sway) and slow the vehicle, and they cool off when it leaves. a = seconds to heat (3), b = shift amplitude m (0.08), c = hot slow m/s (0.8).</summary>
    public sealed class MetalHeatModule : RoadBehaviourBase
    {
        private float _heatS = 3f, _amp = 0.08f, _slow = 0.8f, _heat, _phase; private bool _on;
        public override float Sway => _heat >= 1f ? _amp * Mathf.Sin(_phase) : 0f;
        public override float SpeedDelta => _heat >= 1f ? -_slow : 0f;
        public override int Stage => _heat >= 1f ? 2 : _heat >= 0.5f ? 1 : 0;
        public override void Configure(in SurfaceModuleSpec s, SurfaceRuleSO r) { _heatS = s.a > 0f ? s.a : 3f; _amp = s.b > 0f ? s.b : 0.08f; _slow = s.c > 0f ? s.c : 0.8f; Reset(); }
        public override void OnVehicleEnter() { _on = true; }
        public override void OnVehicleExit() { _on = false; }
        public override void OnHazardEffect(SurfaceHazardKind kind, float strength) { if (kind == SurfaceHazardKind.Charge || kind == SurfaceHazardKind.Fire) _heat = Mathf.Min(1f, _heat + 0.5f); }
        public override void Tick(float dt, SurfaceFrame f)
        {
            _heat = _on ? Mathf.Min(1f, _heat + dt / _heatS) : Mathf.Max(0f, _heat - dt / (_heatS * 2f));
            _phase += dt * 5f;
            if (_phase > 6.2831853f) _phase -= 6.2831853f;
        }
        public override void Reset() { _heat = 0f; _phase = 0f; _on = false; }
    }

    /// <summary>Ice crystals: spikes grow in windows. A vehicle that boosts or is heavy smashes them (they stay broken); any other vehicle snags and slows. a = period s (5), b = spikes-up window s (2), c = snag m/s (-1.3, negative slows), d unused.</summary>
    public sealed class SpikesModule : RoadBehaviourBase
    {
        public override bool PieceLocal => true;
        private float _period = 5f, _up = 2f, _snag = -1.3f, _snagLeft, _phaseOff; private bool _on, _broken, _isUp;
        public override int Stage => _broken ? 2 : _isUp ? 1 : 0;
        public override float SpeedDelta => _snagLeft > 0f ? _snag : 0f;
        public bool Broken => _broken;
        public override void Configure(in SurfaceModuleSpec s, SurfaceRuleSO r) { _period = s.a > 0f ? s.a : 5f; _up = s.b > 0f ? s.b : 2f; _snag = s.c != 0f ? s.c : -1.3f; Reset(); }
        public override void OnVehicleEnter() { _on = true; }
        public override void OnVehicleExit() { _on = false; }
        public override void OnProjectileImpact(float energy, bool fire) { if (energy >= 3f || fire) _broken = true; }
        public override void OnHazardEffect(SurfaceHazardKind kind, float strength) { if (kind == SurfaceHazardKind.Fire) _broken = true; }      // fire melts crystals
        public override void Tick(float dt, SurfaceFrame f)
        {
            _isUp = !_broken && Mathf.Repeat(f.TimeS + f.PieceIndex * 1.3f + _phaseOff, _period) < _up;
            if (_snagLeft > 0f) _snagLeft -= dt;
            if (_on && _isUp)
            {
                if (f.Boosting || f.Heavy) { _broken = true; _isUp = false; }
                else if (_snagLeft <= 0f) _snagLeft = 0.6f;
            }
        }
        public override void Reset() { _snagLeft = 0f; _on = false; _broken = false; _isUp = false; }
    }

    /// <summary>Haunted shadow: the road fades out and in. It stays solid (looks only, so it is always fair). a = seconds per half cycle (2), b = lowest alpha (0.2).</summary>
    public sealed class FadeModule : RoadBehaviourBase
    {
        public override bool PieceLocal => true;
        private float _halfS = 2f, _min = 0.2f, _alpha = 1f, _blank;
        public override float Alpha => _blank > 0f ? Mathf.Min(_alpha, 0.05f) : _alpha;
        public override int Stage => Alpha < 0.5f ? 1 : 0;
        public override void Configure(in SurfaceModuleSpec s, SurfaceRuleSO r) { _halfS = s.a > 0f ? s.a : 2f; _min = s.b > 0f ? s.b : 0.2f; Reset(); }
        public override void OnHazardEffect(SurfaceHazardKind kind, float strength) { if (kind == SurfaceHazardKind.Cold) _blank = Mathf.Max(_blank, strength > 0f ? strength : 1f); }   // a shadow orb blanks the road for a second
        public override void Tick(float dt, SurfaceFrame f)
        {
            _alpha = Mathf.Lerp(_min, 1f, 0.5f + 0.5f * Mathf.Cos(Mathf.PI * (f.TimeS + f.PieceIndex * 0.15f) / _halfS));
            if (_blank > 0f) _blank -= dt;
        }
        public override void Reset() { _alpha = 1f; _blank = 0f; }
    }

    /// <summary>Electric rail: alternating safe and charged windows with a blink before the charge. Driving onto a charged window cuts the drive for a moment (no damage). a = safe s (2.5), b = charged s (1.2), c = warning blink s (0.6), d = drive cut s (0.5).</summary>
    public sealed class ChargeModule : RoadBehaviourBase
    {
        public override bool PieceLocal => true;
        private float _safe = 2.5f, _live = 1.2f, _warn = 0.6f, _cutS = 0.5f, _cut, _force; private bool _on, _shocked, _charged, _warning;
        public override bool DriveCut => _cut > 0f;
        public override int Stage => _charged ? 2 : _warning ? 1 : 0;
        public override void Configure(in SurfaceModuleSpec s, SurfaceRuleSO r) { _safe = s.a > 0f ? s.a : 2.5f; _live = s.b > 0f ? s.b : 1.2f; _warn = s.c > 0f ? s.c : 0.6f; _cutS = s.d > 0f ? s.d : 0.5f; Reset(); }
        public override void OnVehicleEnter() { _on = true; }
        public override void OnVehicleExit() { _on = false; }
        public override void OnHazardEffect(SurfaceHazardKind kind, float strength) { if (kind == SurfaceHazardKind.Charge) _force = Mathf.Max(_force, strength > 0f ? strength : 1.5f); }   // a neon bolt charges the rail
        public override void Tick(float dt, SurfaceFrame f)
        {
            float cycle = _safe + _live;
            float ph = Mathf.Repeat(f.TimeS + f.PieceIndex * 0.5f, cycle);
            bool was = _charged;
            _charged = ph >= _safe || _force > 0f;
            _warning = !_charged && ph >= _safe - _warn;
            if (_force > 0f) _force -= dt;
            if (_cut > 0f) _cut -= dt;
            if (!_charged) _shocked = false;
            else if (_on && !_shocked) { _shocked = true; _cut = _cutS; }                 // one shock per charged window
            if (was && !_charged) _cut = Mathf.Min(_cut, 0f);
        }
        public override void Reset() { _cut = 0f; _force = 0f; _on = false; _shocked = false; _charged = false; _warning = false; }
    }

    /// <summary>Fragile paper: a heavy vehicle that stays on it for a seconds tears it (Gone); a light vehicle never does. a = seconds of heavy load (0.8).</summary>
    public sealed class TearModule : RoadBehaviourBase
    {
        public override bool PieceLocal => true;
        private float _tearS = 0.8f, _load; private bool _on;
        public override int Stage => _load >= _tearS ? 2 : _load > _tearS * 0.4f ? 1 : 0;
        public override bool Gone => _load >= _tearS;
        public override void Configure(in SurfaceModuleSpec s, SurfaceRuleSO r) { _tearS = s.a > 0f ? s.a : 0.8f; Reset(); }
        public override void OnVehicleEnter() { _on = true; }
        public override void OnVehicleExit() { _on = false; }
        public override void OnProjectileImpact(float energy, bool fire) { if (fire || energy >= 3f) _load = _tearS; }
        public override void Tick(float dt, SurfaceFrame f) { if (_on && f.Heavy && _load < _tearS) _load += dt; }
        public override void Reset() { _load = 0f; _on = false; }
    }

    /// <summary>Water: buoyant sections lift a light vehicle a little and the tyres have less traction; a heavy vehicle sinks into the grip. a = lift as a fraction of gravity (0.3), b = grip multiplier (0.6), c = speed multiplier (0.94).</summary>
    public sealed class BuoyancyModule : RoadBehaviourBase
    {
        private float _lift = 0.3f, _grip = 0.6f, _speedMul = 0.94f, _t, _bob; private bool _on, _heavy;
        public override float VerticalAccel => _on && !_heavy ? _lift * 9.81f * (0.7f + 0.3f * _bob) : 0f;
        public override float GripMul => _on ? _grip : 1f;
        public override void Configure(in SurfaceModuleSpec s, SurfaceRuleSO r) { _lift = s.a > 0f ? s.a : 0.3f; _grip = s.b > 0f ? s.b : 0.6f; _speedMul = s.c > 0f ? s.c : 0.94f; Reset(); }
        public override void OnVehicleEnter() { _on = true; }
        public override void OnVehicleExit() { _on = false; }
        public override void OnHazardEffect(SurfaceHazardKind kind, float strength) { }
        public override void Tick(float dt, SurfaceFrame f) { _t += dt; _bob = Mathf.Sin(_t * 3f); _heavy = f.Heavy; }
        public override float ApplyTarget(float target, SurfaceFrame f) { return _on ? target * _speedMul : target; }
        public override void Reset() { _t = 0f; _bob = 0f; _on = false; _heavy = false; }
    }

    /// <summary>Cobblestone: bumps give the suspension a small repeated kick so it oscillates. a = kick m/s (0.9), b = seconds between kicks (0.35). Needs a grounded, moving vehicle.</summary>
    public sealed class BumpModule : RoadBehaviourBase
    {
        private float _kick = 0.9f, _every = 0.35f, _left, _pending; private bool _on;
        public override float TakeImpulse() { float p = _pending; _pending = 0f; return p; }
        public override void Configure(in SurfaceModuleSpec s, SurfaceRuleSO r) { _kick = s.a > 0f ? s.a : 0.9f; _every = s.b > 0f ? s.b : 0.35f; Reset(); }
        public override void OnVehicleEnter() { _on = true; _left = _every; }
        public override void OnVehicleExit() { _on = false; }
        public override void Tick(float dt, SurfaceFrame f)
        {
            if (!_on) return;
            _left -= dt;
            if (_left <= 0f) { _left = _every; if (!f.Airborne && f.Speed > 1.5f) _pending = _kick * Mathf.Clamp01(f.Speed / 6f); }
        }
        public override void Reset() { _left = 0f; _pending = 0f; _on = false; }
    }

    /// <summary>
    /// Lava river platforms: every piece cycles between risen (safe), sinking (warning, stage 1) and sunk (stage 2). A sunk piece lowers the road a little and slows the vehicle; it never removes road, so it is always fair.
    /// a = period s (4), b = risen fraction of the period (0.55), c = sink depth m (0.25), d = sunk slow m/s (1.5).
    /// </summary>
    public sealed class CycleModule : RoadBehaviourBase
    {
        public override bool PieceLocal => true;
        private float _period = 4f, _risen = 0.55f, _depth = 0.25f, _slow = 1.5f, _dip; private int _stage; private bool _on;
        public override int Stage => _stage;
        public override float Sway => _on ? -_dip : 0f;
        public override float SpeedDelta => _on && _stage == 2 ? -_slow : 0f;
        public override void Configure(in SurfaceModuleSpec s, SurfaceRuleSO r) { _period = s.a > 0f ? s.a : 4f; _risen = s.b > 0f ? s.b : 0.55f; _depth = s.c > 0f ? s.c : 0.25f; _slow = s.d > 0f ? s.d : 1.5f; Reset(); }
        public override void OnVehicleEnter() { _on = true; }
        public override void OnVehicleExit() { _on = false; }
        public override void Tick(float dt, SurfaceFrame f)
        {
            float ph = Mathf.Repeat(f.TimeS + f.PieceIndex * 0.9f, _period) / _period;
            _stage = ph < _risen ? 0 : ph < _risen + 0.12f ? 1 : 2;
            _dip = Mathf.MoveTowards(_dip, _stage == 2 ? _depth : 0f, dt * 0.8f);
        }
        public override void Reset() { _dip = 0f; _stage = 0; _on = false; }
    }
}
