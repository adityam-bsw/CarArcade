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

        public void Enter(SurfaceRuleSO rule, SurfaceModuleSpec[] specs)
        {
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
                m.OnVehicleEnter();
                _active[_count++] = m;
            }
        }

        public void Exit()
        {
            for (int i = 0; i < _count; i++) { _active[i].OnVehicleExit(); _active[i].Reset(); _active[i] = null; }
            _count = 0; _slowAcc = 0f;
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
        public T Find<T>() where T : class { for (int i = 0; i < _count; i++) if (_active[i] is T t) return t; return null; }
    }
}
