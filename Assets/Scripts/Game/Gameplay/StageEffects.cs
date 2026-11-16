using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoadBuilderGame
{
    /// <summary>
    /// The live result of every active mutation event: one multiplier or flag per channel. Behaviours add and remove their entries by owner, the manager reads the
    /// folded values once per frame and applies them to the car, the ink tank and the run. Reset() puts every channel back to neutral, so an effect can never
    /// outlive its event, a death, a restart or a quit. Fixed arrays only: no allocation after construction.
    /// </summary>
    public sealed class StageEffectState
    {
        private struct Entry { public object Owner; public StageEffectKind Kind; public float A, B, Cur, Cur2; }
        private readonly Entry[] _e = new Entry[16];
        private int _n;

        public float GravityMul = 1f, SpeedMul = 1f, InkRegenMul = 1f, InkCapMul = 1f, BoostDrainMul = 1f, CarScale = 1f;
        public bool CoinVacuum; public float VacuumRadius, VacuumSpeed;
        public float WindAccelX;
        /// <summary>0 = off. Fraction of <see cref="FragileRefMps"/> that already cracks the car.</summary>
        public float FragileThreshold01, FragileRefMps = 12f;
        public float DistanceM;
        // ---- batch A2 channels (neutral values: 1 / 0 / false)
        public float MotorDir = 1f, FrictionMul = 1f, BounceAdd, AccelY;
        public bool Mirror;
        /// <summary>Coin gates crossed since the last Road Tax checkpoint (the manager adds, the tax behaviour spends).</summary>
        public int TaxGatesCrossed;

        public bool Any => _n > 0;
        public int Count => _n;

        public void Add(object owner, StageEffectKind kind, float a, float b)
        {
            if (kind == StageEffectKind.None || _n >= _e.Length) return;
            _e[_n++] = new Entry { Owner = owner, Kind = kind, A = a, B = b, Cur = (kind == StageEffectKind.GravityPulse || kind == StageEffectKind.GravityGates || kind == StageEffectKind.MotorReverse) ? 1f : (kind == StageEffectKind.VehicleSwap ? 0f : 0f) };
            Recompute();
        }

        public void Remove(object owner)
        {
            int w = 0;
            for (int i = 0; i < _n; i++) if (!ReferenceEquals(_e[i].Owner, owner)) _e[w++] = _e[i];
            for (int i = w; i < _n; i++) _e[i] = default;
            _n = w;
            Recompute();
        }

        /// <summary>Everything back to neutral (run start, run end, restart, quit).</summary>
        public void Reset()
        {
            for (int i = 0; i < _n; i++) _e[i] = default;
            _n = 0; TaxGatesCrossed = 0; DistanceM = 0f;
            Recompute();
        }

        /// <summary>Sets the current wind acceleration of one owner's wind entry (m/s^2, signed).</summary>
        public void SetWind(object owner, float accel)
        {
            for (int i = 0; i < _n; i++) if (ReferenceEquals(_e[i].Owner, owner) && (_e[i].Kind == StageEffectKind.WindSteady || _e[i].Kind == StageEffectKind.WindGust || _e[i].Kind == StageEffectKind.WindTunnels)) _e[i].Cur = accel;
            Recompute();
        }

        /// <summary>Sets the live value of one owner's dynamic entry (gravity window multiplier, motor factor, swap profile index, pull x/y) and refolds. Zero allocation.</summary>
        public void SetCur(object owner, StageEffectKind kind, float cur, float cur2 = 0f)
        {
            for (int i = 0; i < _n; i++) if (ReferenceEquals(_e[i].Owner, owner) && _e[i].Kind == kind) { _e[i].Cur = cur; _e[i].Cur2 = cur2; }
            Recompute();
        }

        // Vehicle swap classes: speed, gravity, scale (index 0 = the player's own vehicle)
        public static readonly string[] SwapNames = { "OWN", "LIGHT", "HEAVY", "FLOATY", "ROCKET" };
        private static readonly float[] SwapSpeed = { 1f, 1.12f, 0.9f, 1f, 1.22f };
        private static readonly float[] SwapGrav = { 1f, 0.92f, 1.2f, 0.7f, 1.05f };
        private static readonly float[] SwapScale = { 1f, 0.85f, 1.2f, 1f, 0.9f };

        private void Recompute()
        {
            GravityMul = SpeedMul = InkRegenMul = InkCapMul = BoostDrainMul = CarScale = 1f;
            MotorDir = FrictionMul = 1f; BounceAdd = AccelY = 0f; Mirror = false;
            CoinVacuum = false; VacuumRadius = VacuumSpeed = 0f; FragileThreshold01 = 0f; FragileRefMps = 12f;
            float wind = 0f;
            for (int i = 0; i < _n; i++)
            {
                var x = _e[i];
                switch (x.Kind)
                {
                    case StageEffectKind.GravityMul: GravityMul *= x.A; break;
                    case StageEffectKind.SpeedMul: SpeedMul *= x.A; break;
                    case StageEffectKind.InkRegenMul: InkRegenMul *= x.A; break;
                    case StageEffectKind.InkCapacityMul: InkCapMul *= x.A; break;
                    case StageEffectKind.BoostDrainMul: BoostDrainMul *= x.A; break;
                    case StageEffectKind.VehicleScale: CarScale *= x.A; break;
                    case StageEffectKind.CoinVacuum: CoinVacuum = true; VacuumRadius = Mathf.Max(VacuumRadius, x.A); VacuumSpeed = Mathf.Max(VacuumSpeed, x.B); break;
                    case StageEffectKind.Fragile: FragileThreshold01 = FragileThreshold01 <= 0f ? x.A : Mathf.Min(FragileThreshold01, x.A); FragileRefMps = x.B > 0f ? x.B : FragileRefMps; break;
                    case StageEffectKind.WindSteady:
                    case StageEffectKind.WindGust: wind += x.Cur; break;
                    case StageEffectKind.WindTunnels: wind += x.Cur; AccelY += x.Cur2; break;
                    case StageEffectKind.MagneticPylons: wind += x.Cur; AccelY += x.Cur2; break;
                    case StageEffectKind.GravityPulse:
                    case StageEffectKind.GravityGates: GravityMul *= x.Cur; break;
                    case StageEffectKind.MotorReverse: MotorDir *= x.Cur; break;
                    case StageEffectKind.MirrorInput: Mirror = true; break;
                    case StageEffectKind.IceAge: FrictionMul *= x.A; break;
                    case StageEffectKind.StickyRoad: FrictionMul *= x.A; SpeedMul *= x.B; break;
                    case StageEffectKind.ElasticRoad: BounceAdd += x.A; break;
                    case StageEffectKind.FloodSurge: SpeedMul *= x.Cur > 0f ? x.Cur : 1f; break;
                    case StageEffectKind.VehicleSwap:
                        { int k = Mathf.Clamp((int)x.Cur, 0, SwapSpeed.Length - 1); SpeedMul *= SwapSpeed[k]; GravityMul *= SwapGrav[k]; CarScale *= SwapScale[k]; break; }
                }
            }
            WindAccelX = wind;
        }

        public string Snapshot()
        {
            return "grav=" + GravityMul.ToString("0.00") + " speed=" + SpeedMul.ToString("0.00") + " inkRegen=" + InkRegenMul.ToString("0.00") + " inkCap=" + InkCapMul.ToString("0.00")
                + " boostDrain=" + BoostDrainMul.ToString("0.00") + " scale=" + CarScale.ToString("0.00") + " vacuum=" + CoinVacuum + " wind=" + WindAccelX.ToString("0.00")
                + " fragile=" + FragileThreshold01.ToString("0.00") + " motorDir=" + MotorDir.ToString("0.00") + " friction=" + FrictionMul.ToString("0.00") + " bounce=" + BounceAdd.ToString("0.00") + " accY=" + AccelY.ToString("0.00") + " mirror=" + Mirror + " entries=" + _n;
        }
    }

    /// <summary>Optional: a behaviour that placed pickups and must put them back when a jump or revive cleared them.</summary>
    public interface IStagePickupsAware { void OnPickupsCleared(float distanceM); }

    /// <summary>What the economy-style events need from the game: place a coin gate, take coins, show a line. The manager supplies them; this file holds no scene reference.</summary>
    public sealed class StageEffectHooks
    {
        public Action<float, float> PlaceCoinGate;      // run distance m, seeded height 0..1
        public Func<int, int> ChargeCoins;              // wants N coins, returns what was really taken (session coins only, never below 0)
        public Action<string> Banner;
        /// <summary>World objects and engine hooks of the batch A2 events (null in tests that only need the data channels).</summary>
        public StageWorld2 World;
    }

    /// <summary>
    /// One class for every data-driven mutation event: on start it adds the event's effect specs to the <see cref="StageEffectState"/>, on end (and on death,
    /// restart, quit, through the controller) it removes them. Wind kinds are ticked from the seeded terrain seed: a steady side wind with a slow swell,
    /// or short gusts at random intervals. Zero allocation per tick.
    /// </summary>
    public class StageEffectBehaviour : IStageEventBehaviour, IStageTickBehaviour
    {
        public const float GustSeconds = 1.2f;
        protected readonly StageEventDefinition Def;
        protected readonly StageEffectState Fx;
        private Pcg32 _rng;
        private float _t, _gustWait, _gustLeft, _dir = 1f;
        private bool _hasWind;

        public StageEffectBehaviour(StageEventDefinition def, StageEffectState fx) { Def = def; Fx = fx; }

        public virtual void OnStart(StageEventState s)
        {
            _rng = new Pcg32(unchecked(s.TerrainSeed ^ 0x57D1A5u));
            _dir = _rng.NextFloat01() < 0.5f ? -1f : 1f;
            _t = 0f; _gustLeft = 0f; _gustWait = 1.5f; _hasWind = false;
            var fx = Def.effects;
            for (int i = 0; i < fx.Length; i++)
            {
                Fx.Add(this, fx[i].kind, fx[i].a, fx[i].b);
                if (fx[i].kind == StageEffectKind.WindSteady || fx[i].kind == StageEffectKind.WindGust) _hasWind = true;
            }
        }

        public virtual void OnEnd(StageEventState s) { Fx.Remove(this); _hasWind = false; }

        public virtual void OnRespawn(StageEventState s, float distanceM) { _gustLeft = 0f; _gustWait = Mathf.Max(_gustWait, 2f); if (_hasWind) Fx.SetWind(this, 0f); }

        public virtual void Tick(float dt, float distanceM)
        {
            if (!_hasWind || dt <= 0f) return;
            var fx = Def.effects;
            for (int i = 0; i < fx.Length; i++)
            {
                if (fx[i].kind == StageEffectKind.WindSteady)
                {
                    _t += dt;
                    float period = Mathf.Max(2f, fx[i].b);
                    Fx.SetWind(this, fx[i].a * _dir * (0.7f + 0.3f * Mathf.Sin(_t * Mathf.PI * 2f / period)));
                }
                else if (fx[i].kind == StageEffectKind.WindGust)
                {
                    if (_gustLeft > 0f)
                    {
                        _gustLeft -= dt;
                        float k = Mathf.Clamp01(1f - _gustLeft / GustSeconds);
                        Fx.SetWind(this, _gustLeft > 0f ? fx[i].a * _dir * Mathf.Sin(k * Mathf.PI) : 0f);
                    }
                    else
                    {
                        _gustWait -= dt;
                        if (_gustWait <= 0f)
                        {
                            _gustLeft = GustSeconds; _dir = _rng.NextFloat01() < 0.5f ? -1f : 1f;
                            _gustWait = Mathf.Max(2f, fx[i].b * (0.6f + 0.8f * _rng.NextFloat01()));
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Road Tax: every <c>b</c> metres of the window costs <c>a</c> coins unless the car crossed a coin gate since the last checkpoint. The gates are placed once when
    /// the event starts, 40 m before each checkpoint at a seeded height. Only coins earned this run can be taken (never below zero, never the saved balance).
    /// </summary>
    public sealed class RoadTaxBehaviour : StageEffectBehaviour, IStagePickupsAware
    {
        private readonly StageEffectHooks _hooks;
        private float _next, _cost, _spacing, _endM, _startM;
        private uint _gateSeed;
        private bool _justStarted;

        public RoadTaxBehaviour(StageEventDefinition def, StageEffectState fx, StageEffectHooks hooks) : base(def, fx) { _hooks = hooks; }

        public override void OnStart(StageEventState s)
        {
            base.OnStart(s);
            Fx.TaxGatesCrossed = 0;
            _cost = 10f; _spacing = 100f;
            foreach (var e in Def.effects) if (e.kind == StageEffectKind.RoadTax) { _cost = e.a; _spacing = Mathf.Max(20f, e.b); }
            _endM = s.EndM; _startM = s.StartM; _gateSeed = unchecked(s.TerrainSeed ^ 0x7A8u);
            _next = s.StartM + _spacing;
            while (_next <= Fx.DistanceM && _next <= _endM) _next += _spacing;                 // a jump into the middle skips the checkpoints already behind
            PlaceGates();
            _justStarted = true;
        }

        private void PlaceGates()
        {
            var rng = new Pcg32(_gateSeed);
            int k = 0;
            for (float c = _startM + _spacing; c <= _endM + 0.01f; c += _spacing, k++)
            {
                float h = rng.NextFloat01();                                                   // one draw per checkpoint, so a re-placed gate keeps its height
                if (c >= _next) _hooks?.PlaceCoinGate?.Invoke(c - 40f, h);
            }
        }

        /// <summary>A jump or revive cleared every pickup: put the gates of the checkpoints still ahead back (unless this very start just placed them).</summary>
        public void OnPickupsCleared(float distanceM)
        {
            if (_justStarted) { _justStarted = false; return; }
            while (_next < distanceM && _next <= _endM) _next += _spacing;
            PlaceGates();
        }

        public override void OnEnd(StageEventState s) { base.OnEnd(s); Fx.TaxGatesCrossed = 0; }

        public override void Tick(float dt, float distanceM)
        {
            base.Tick(dt, distanceM);
            _justStarted = false;
            while (distanceM >= _next + _spacing && _next <= _endM) _next += _spacing;        // checkpoints far behind (a jump) are skipped, never charged
            while (distanceM >= _next && _next <= _endM + 0.01f)
            {
                if (Fx.TaxGatesCrossed > 0) { Fx.TaxGatesCrossed = 0; _hooks?.Banner?.Invoke("GATE PASSED"); }
                else
                {
                    int paid = _hooks?.ChargeCoins != null ? _hooks.ChargeCoins((int)_cost) : 0;
                    _hooks?.Banner?.Invoke(paid > 0 ? "ROAD TAX -" + paid : "ROAD TAX");
                }
                _next += _spacing;
            }
        }
    }

    /// <summary>Registers one behaviour per mutation event id from the event data. Kinds that need extra hooks have their own class in <see cref="Special"/>; no switch on ids.</summary>
    public static class StageEffectRegistry
    {
        private static readonly Dictionary<StageEffectKind, Func<StageEventDefinition, StageEffectState, StageEffectHooks, IStageEventBehaviour>> Special =
            new Dictionary<StageEffectKind, Func<StageEventDefinition, StageEffectState, StageEffectHooks, IStageEventBehaviour>>
            {
                { StageEffectKind.RoadTax, (d, fx, h) => new RoadTaxBehaviour(d, fx, h) },
            };

        public static IStageEventBehaviour Create(StageEventDefinition def, StageEffectState fx, StageEffectHooks hooks)
        {
            var b2 = StageEffects2.TryCreate(def, fx, hooks);
            if (b2 != null) return b2;
            foreach (var e in def.effects) if (Special.TryGetValue(e.kind, out var make)) return make(def, fx, hooks);
            return new StageEffectBehaviour(def, fx);
        }

        /// <summary>Registers every definition that has effects and no behaviour yet. Returns the number registered.</summary>
        public static int RegisterAll(StageRunController run, StageEffectState fx, IEnumerable<StageEventDefinition> defs, StageEffectHooks hooks)
        {
            int n = 0;
            foreach (var d in defs)
            {
                if (d == null || d.effects == null || d.effects.Length == 0 || run.HasBehaviour(d.id)) continue;
                run.Register(d.id, Create(d, fx, hooks)); n++;
            }
            return n;
        }
    }
}
