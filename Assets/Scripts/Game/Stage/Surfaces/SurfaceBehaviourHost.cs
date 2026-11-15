using UnityEngine;

namespace RoadBuilderGame
{
    /// <summary>
    /// Runs the module list of the surface the vehicle is on. Module instances are pooled one per kind (built once, reconfigured on Enter), so a run allocates nothing after the first use.
    /// Per-frame modules (ice, mud) tick every call; the rest tick at 20 Hz with the accumulated time. Pure C#.
    /// </summary>
    public sealed class SurfaceBehaviourHost
    {
        public const float SlowStepS = 0.05f;
        private readonly IRoadBehaviour[] _pool = new IRoadBehaviour[SurfaceModuleFactory.KindCount];
        private readonly IRoadBehaviour[] _active = new IRoadBehaviour[SurfaceModuleFactory.KindCount];
        private int _count;
        private float _slowAcc;
        private readonly SurfaceFrame _slowFrame = new SurfaceFrame();

        public int Count => _count;
        public IRoadBehaviour this[int i] => _active[i];

        private bool _vehicleOn;

        /// <summary>Binds the module list. <paramref name="vehicleOn"/> false builds the host for a road piece the vehicle is not on yet (neighbour pieces): call <see cref="SetVehicleOn"/> when it arrives.</summary>
        public void Enter(SurfaceRuleSO rule, SurfaceModuleSpec[] specs, bool vehicleOn = true)
        {
            _vehicleOn = vehicleOn;
            _count = 0; _slowAcc = 0f;
            if (specs == null) return;
            for (int i = 0; i < specs.Length && _count < _active.Length; i++)
            {
                int k = (int)specs[i].kind;
                var m = _pool[k] ?? (_pool[k] = SurfaceModuleFactory.Create(specs[i].kind));
                bool dup = false;
                for (int j = 0; j < _count; j++) if (_active[j] == m) dup = true;
                if (dup) continue;                       // one instance per kind in a list
                m.Configure(specs[i], rule);
                if (vehicleOn) m.OnVehicleEnter();
                _active[_count++] = m;
            }
        }

        public void Exit()
        {
            for (int i = 0; i < _count; i++) { _active[i].OnVehicleExit(); _active[i].Reset(); _active[i] = null; }
            _count = 0; _slowAcc = 0f; _vehicleOn = false;
        }

        public bool VehicleOn => _vehicleOn;

        /// <summary>Back to the intact state, modules stay bound (a piece that could not be cut because it is protected road).</summary>
        public void ResetState()
        {
            for (int i = 0; i < _count; i++) { _active[i].Reset(); if (_vehicleOn) _active[i].OnVehicleEnter(); }
        }

        /// <summary>The vehicle arrives on or leaves this road piece. Modules keep their state either way (dissolve and burn run on after the vehicle left).</summary>
        public void SetVehicleOn(bool on)
        {
            if (on == _vehicleOn) return;
            _vehicleOn = on;
            for (int i = 0; i < _count; i++) { if (on) _active[i].OnVehicleEnter(); else _active[i].OnVehicleExit(); }
        }

        public void Tick(SurfaceFrame f)
        {
            if (_count == 0) return;
            _slowAcc += f.Dt;
            bool slow = _slowAcc >= SlowStepS;
            float slowDt = _slowAcc;
            if (slow) _slowAcc = 0f;
            for (int i = 0; i < _count; i++)
            {
                var m = _active[i];
                if (m.TickEveryFrame) m.Tick(f.Dt, f);
                else if (slow) m.Tick(slowDt, f);
            }
        }

        /// <summary>The target speed after every module had its say, plus the additive deltas.</summary>
        public float TargetSpeed(SurfaceFrame f)
        {
            float t = f.BaseTarget;
            for (int i = 0; i < _count; i++) t = _active[i].ApplyTarget(t, f);
            for (int i = 0; i < _count; i++) t += _active[i].SpeedDelta;
            return t;
        }

        public bool DriveCut { get { for (int i = 0; i < _count; i++) if (_active[i].DriveCut) return true; return false; } }
        public bool AnyGone { get { for (int i = 0; i < _count; i++) if (_active[i].Gone) return true; return false; } }
        public int MaxStage { get { int s = 0; for (int i = 0; i < _count; i++) if (_active[i].Stage > s) s = _active[i].Stage; return s; } }
        public float Sway { get { float s = 0f; for (int i = 0; i < _count; i++) s += _active[i].Sway; return s; } }
        public float TakeImpulse() { float p = 0f; for (int i = 0; i < _count; i++) p += _active[i].TakeImpulse(); return p; }
        public void ProjectileImpact(float energy, bool fire) { for (int i = 0; i < _count; i++) _active[i].OnProjectileImpact(energy, fire); }
        public float Torque { get { float s = 0f; for (int i = 0; i < _count; i++) s += _active[i].Torque; return s; } }
        public float VerticalAccel { get { float s = 0f; for (int i = 0; i < _count; i++) s += _active[i].VerticalAccel; return s; } }
        public float GripMul { get { float s = 1f; for (int i = 0; i < _count; i++) s *= _active[i].GripMul; return s; } }
        public float Alpha { get { float s = 1f; for (int i = 0; i < _count; i++) s = Mathf.Min(s, _active[i].Alpha); return s; } }
        public int TakeDamage() { int h = 0; for (int i = 0; i < _count; i++) h += _active[i].TakeDamage(); return h; }
        public void HazardEffect(SurfaceHazardKind kind, float strength) { for (int i = 0; i < _count; i++) _active[i].OnHazardEffect(kind, strength); }
        public T Find<T>() where T : class { for (int i = 0; i < _count; i++) if (_active[i] is T t) return t; return null; }
    }
}
