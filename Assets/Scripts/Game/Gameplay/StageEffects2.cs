using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace RoadBuilderGame
{
    /// <summary>What the batch A2 world objects need from the game. The manager supplies them; this file holds no scene reference of its own.</summary>
    public sealed class StageWorld2Host
    {
        public Func<RoadCar> Car;
        public Func<RoadBuilder> Road;
        public Func<Camera> Cam;
        public Action<string> Damage;          // lose a heart (shield and grace are the manager's business)
        public Func<bool> CanDamage;           // false during hit grace, respawn grace, resume grace
        public Action<float> DrainInk01;       // fraction of the tank
        public Action<int> AddCoins;
        public Action<int> ComboPenalty;
        public Action<string> Cue;             // sound-only cue id (StageCues registry)
    }

    /// <summary>One pooled world object: a coloured rectangle or circle with an optional solid collider (platforms, traffic). Fields are set by the owning behaviour.</summary>
    public sealed class FxProp
    {
        public GameObject Go; public Transform T; public SpriteRenderer Sr; public Rigidbody2D Rb; public BoxCollider2D Col;
        public bool Live, Solid, Circle;
        public object Owner;
        public float X, Y, W = 1f, H = 1f, Rot, A, Life; public int State; public Color Color = Color.white;
    }

    /// <summary>
    /// Everything the A2 events draw or touch in the world: a pool of 40 props, a camera-locked tint overlay and fog ramp, the mirrored-pointer cursor, the short-hop tap,
    /// the music-free beat clock and the road-height helpers. Plain class, created once by the manager; Reset() puts the world back (run start, end, death, quit).
    /// Pools are built on first use; nothing allocates per frame.
    /// </summary>
    public sealed class StageWorld2
    {
        private const int PoolSize = 40;
        private const int OvlSlots = 4;
        private readonly StageEffectState _fx;
        public readonly StageWorld2Host Host;
        private FxProp[] _pool;
        private Transform _root;
        private static Sprite _square, _circle, _ramp;

        private struct Ovl { public object Owner; public Color C; public float Target, Cur; }
        private readonly Ovl[] _ovl = new Ovl[OvlSlots];
        private SpriteRenderer _ovlSr, _fogSr, _cursor;
        private object _fogOwner; private float _fogVis, _fogTarget, _fogCur;

        public float Time { get; private set; }
        public float LastGroundY { get; private set; }
        public bool HopEnabled;
        public float Bpm = 96f;
        public int BeatCount { get; private set; }
        private float _hitCd, _hopCd, _tapT, _cursorLeft; private bool _tapOn; private Vector2 _tapPos;
        private bool _paused;

        public StageWorld2(StageEffectState fx, StageWorld2Host host) { _fx = fx; Host = host; }

        public RoadCar Car { get { return Host.Car != null ? Host.Car() : null; } }
        public RoadBuilder Road { get { return Host.Road != null ? Host.Road() : null; } }
        public StageEffectState Fx { get { return _fx; } }

        // ------------------------------------------------------------------ sprites
        private static Sprite Square()
        {
            if (_square != null) return _square;
            var t = new Texture2D(4, 4, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var px = new Color32[16]; for (int i = 0; i < 16; i++) px[i] = new Color32(255, 255, 255, 255);
            t.SetPixels32(px); t.Apply(false, true);
            return _square = Sprite.Create(t, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        }

        private static Sprite Circle()
        {
            if (_circle != null) return _circle;
            const int n = 32;
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = Mathf.Sqrt((x - 15.5f) * (x - 15.5f) + (y - 15.5f) * (y - 15.5f));
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(255 * Mathf.Clamp01(16f - d)));
                }
            t.SetPixels32(px); t.Apply(false, true);
            return _circle = Sprite.Create(t, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
        }

        private static Sprite Ramp()
        {
            if (_ramp != null) return _ramp;
            var t = new Texture2D(64, 1, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[64];
            for (int x = 0; x < 64; x++) px[x] = new Color32(255, 255, 255, (byte)(255 * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(x / 12f))));
            t.SetPixels32(px); t.Apply(false, true);
            return _ramp = Sprite.Create(t, new Rect(0, 0, 64, 1), new Vector2(0f, 0.5f), 64f);
        }

        private void EnsurePool()
        {
            if (_pool != null) return;
            var rootGo = new GameObject("StageWorld2"); _root = rootGo.transform;
            _pool = new FxProp[PoolSize];
            for (int i = 0; i < PoolSize; i++)
            {
                var go = new GameObject("fx" + i); go.transform.SetParent(_root, false);
                var p = new FxProp { Go = go, T = go.transform };
                p.Sr = go.AddComponent<SpriteRenderer>(); p.Sr.sortingOrder = 30;
                p.Rb = go.AddComponent<Rigidbody2D>(); p.Rb.bodyType = RigidbodyType2D.Kinematic; p.Rb.simulated = false;
                p.Col = go.AddComponent<BoxCollider2D>(); p.Col.size = Vector2.one; p.Col.enabled = false;
                go.SetActive(false);
                _pool[i] = p;
            }
        }

        /// <summary>A pooled prop or null when the pool is full (callers cope: the event simply spawns less).</summary>
        public FxProp Spawn(object owner, float x, float y, float w, float h, Color c, bool circle = false, bool solid = false)
        {
            EnsurePool();
            for (int i = 0; i < PoolSize; i++)
            {
                var p = _pool[i];
                if (p.Live) continue;
                p.Live = true; p.Owner = owner; p.X = x; p.Y = y; p.W = w; p.H = h; p.Rot = 0f; p.A = 0f; p.Life = 0f; p.State = 0; p.Color = c; p.Circle = circle; p.Solid = solid;
                p.Sr.sprite = circle ? Circle() : Square();
                p.Go.SetActive(true);
                p.Rb.simulated = solid; p.Col.enabled = solid;
                Sync(p);
                return p;
            }
            return null;
        }

        public void Free(FxProp p)
        {
            if (p == null || !p.Live) return;
            p.Live = false; p.Owner = null; p.Solid = false;
            p.Col.enabled = false; p.Rb.simulated = false; p.Go.SetActive(false);
        }

        public void SetShape(FxProp p, bool circle) { p.Circle = circle; p.Sr.sprite = circle ? Circle() : Square(); }
        public void SetSolid(FxProp p, bool solid) { p.Solid = solid; p.Col.enabled = solid; p.Rb.simulated = solid; }

        public void FreeOwned(object owner) { if (_pool == null) return; for (int i = 0; i < PoolSize; i++) if (_pool[i].Live && ReferenceEquals(_pool[i].Owner, owner)) Free(_pool[i]); }
        public int LiveCount(object owner) { int n = 0; if (_pool != null) for (int i = 0; i < PoolSize; i++) if (_pool[i].Live && (owner == null || ReferenceEquals(_pool[i].Owner, owner))) n++; return n; }

        private static void Sync(FxProp p)
        {
            p.T.position = new Vector3(p.X, p.Y, 0f);
            p.T.localScale = new Vector3(p.W, p.H, 1f);
            p.T.rotation = Quaternion.Euler(0f, 0f, p.Rot);
            p.Sr.color = p.Color;
            if (p.Solid) p.Rb.position = new Vector2(p.X, p.Y);
        }

        // ------------------------------------------------------------------ queries
        public float GroundY(float x)
        {
            var r = Road;
            if (r != null && r.TryGetRoadYAtX(x, out float y)) return y;
            return LastGroundY;
        }
        public bool HasRoad(float x) { var r = Road; return r != null && r.HasRoadAt(x); }

        /// <summary>The car's body overlaps the prop's rectangle (a slightly forgiving box: half 0.9 x 0.5 m at scale 1).</summary>
        public bool Overlaps(FxProp p)
        {
            var car = Car; if (car == null || car.Body == null) return false;
            Vector2 c = car.Body.position; float s = car.Scale;
            return Mathf.Abs(c.x - p.X) < 0.9f * s + p.W * 0.5f && Mathf.Abs(c.y - p.Y) < 0.5f * s + p.H * 0.5f;
        }

        /// <summary>Deals one heart of damage if the manager allows it (grace respected) and this world is not in its own 1.6 s cooldown. Returns true when it hit.</summary>
        public bool Hit(string reason)
        {
            if (_hitCd > 0f) return false;
            if (Host.CanDamage != null && !Host.CanDamage()) return false;
            _hitCd = 1.6f;
            Host.Damage?.Invoke(reason);
            return true;
        }

        public void ClampRise(float maxUp)
        {
            var car = Car; if (car == null || car.Body == null) return;
            var v = car.Body.linearVelocity; if (v.y > maxUp) { v.y = maxUp; car.Body.linearVelocity = v; }
        }

        public void Hop()
        {
            var car = Car; if (car == null || car.Body == null || _hopCd > 0f || !car.IsGrounded) return;
            var v = car.Body.linearVelocity; v.y = Mathf.Max(v.y, 0f) + 4.6f; car.Body.linearVelocity = v; _hopCd = 0.5f;
        }

        public float BeatPhase { get { float b = Time * Bpm / 60f; return b - Mathf.Floor(b); } }

        // ------------------------------------------------------------------ overlay, fog, mirrored cursor
        public void SetOverlay(object owner, Color c, float alpha)
        {
            for (int i = 0; i < OvlSlots; i++) if (ReferenceEquals(_ovl[i].Owner, owner)) { _ovl[i].C = c; _ovl[i].Target = alpha; return; }
            for (int i = 0; i < OvlSlots; i++) if (_ovl[i].Owner == null) { _ovl[i] = new Ovl { Owner = owner, C = c, Target = alpha, Cur = 0f }; return; }
        }

        public void SetFog(object owner, float visibilityM, float alpha) { _fogOwner = owner; _fogVis = visibilityM; _fogTarget = alpha; }

        /// <summary>Fades this owner's overlay and fog out (they are removed when invisible).</summary>
        public void ClearOverlay(object owner)
        {
            for (int i = 0; i < OvlSlots; i++) if (ReferenceEquals(_ovl[i].Owner, owner)) _ovl[i].Target = 0f;
            if (ReferenceEquals(_fogOwner, owner)) _fogTarget = 0f;
        }

        /// <summary>Brush hook: maps the pointer's world position. While Mirror Road runs the x mirrors about the camera centre and a ring shows where the stroke will land.</summary>
        public Vector2 MapPointer(Vector2 world)
        {
            if (!_fx.Mirror) return world;
            var cam = Host.Cam != null ? Host.Cam() : null; if (cam == null) return world;
            float cx = cam.transform.position.x;
            var m = new Vector2(2f * cx - world.x, world.y);
            EnsureCursor(); _cursor.transform.position = new Vector3(m.x, m.y, 0f); _cursorLeft = 0.2f; _cursor.enabled = true;
            return m;
        }

        private void EnsureCursor()
        {
            if (_cursor != null) return;
            var go = new GameObject("MirrorCursor"); _cursor = go.AddComponent<SpriteRenderer>();
            _cursor.sprite = Circle(); _cursor.sortingOrder = 360; _cursor.color = new Color(0.9f, 0.95f, 1f, 0.8f); go.transform.localScale = new Vector3(0.9f, 0.9f, 1f); _cursor.enabled = false;
        }

        private void EnsureOverlay(Camera cam)
        {
            if (_ovlSr != null) return;
            var a = new GameObject("StageOverlay"); a.transform.SetParent(cam.transform, false); a.transform.localPosition = new Vector3(0f, 0f, 5f);
            _ovlSr = a.AddComponent<SpriteRenderer>(); _ovlSr.sprite = Square(); _ovlSr.sortingOrder = 390; _ovlSr.enabled = false;
            var b = new GameObject("StageFog"); b.transform.SetParent(cam.transform, false); b.transform.localPosition = new Vector3(0f, 0f, 5f);
            _fogSr = b.AddComponent<SpriteRenderer>(); _fogSr.sprite = Ramp(); _fogSr.sortingOrder = 380; _fogSr.enabled = false;
        }

        // ------------------------------------------------------------------ per frame
        public void Tick(float dt)
        {
            if (_paused || dt <= 0f) return;
            Time += dt;
            if (_hitCd > 0f) _hitCd -= dt;
            if (_hopCd > 0f) _hopCd -= dt;
            int beat = Mathf.FloorToInt(Time * Bpm / 60f); BeatCount = beat;
            var car = Car;
            if (car != null && car.Body != null)
            {
                var r = Road;
                if (r != null && r.TryGetRoadYAtX(car.Body.position.x, out float gy)) LastGroundY = gy; else if (LastGroundY == 0f) LastGroundY = car.Body.position.y - 0.5f;
            }
            if (HopEnabled) PollTap(dt);
            if (_cursor != null && _cursorLeft > 0f) { _cursorLeft -= dt; if (_cursorLeft <= 0f) _cursor.enabled = false; }
            var cam = Host.Cam != null ? Host.Cam() : null;
            if (cam != null) UpdateOverlay(cam, car, dt);
            if (_pool != null) for (int i = 0; i < PoolSize; i++) if (_pool[i].Live) Sync(_pool[i]);
        }

        private void PollTap(float dt)
        {
            bool down = false, up = false; Vector2 pos = Vector2.zero;
            var p = Pointer.current;
            if (p != null) { down = p.press.wasPressedThisFrame; up = p.press.wasReleasedThisFrame; pos = p.position.ReadValue(); }
            if (!down && !up)
            {
                if (Input.GetMouseButtonDown(0)) { down = true; pos = Input.mousePosition; }
                else if (Input.GetMouseButtonUp(0)) { up = true; pos = Input.mousePosition; }
            }
            if (down && !(EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())) { _tapOn = true; _tapT = 0f; _tapPos = pos; }
            if (_tapOn) { _tapT += dt; if (_tapT > 0.3f) _tapOn = false; }
            if (up && _tapOn) { if (_tapT < 0.2f && (pos - _tapPos).sqrMagnitude < 1600f) Hop(); _tapOn = false; }
        }

        private void UpdateOverlay(Camera cam, RoadCar car, float dt)
        {
            float best = 0f; Color bc = Color.white; bool any = false;
            float k = Mathf.Min(1f, dt * 4f);
            for (int i = 0; i < OvlSlots; i++)
            {
                if (_ovl[i].Owner == null) continue;
                _ovl[i].Cur += (_ovl[i].Target - _ovl[i].Cur) * k;
                if (_ovl[i].Target <= 0f && _ovl[i].Cur < 0.004f) { _ovl[i] = default; continue; }
                any = true; if (_ovl[i].Cur >= best) { best = _ovl[i].Cur; bc = _ovl[i].C; }
            }
            _fogCur += (_fogTarget - _fogCur) * k;
            bool fogOn = _fogOwner != null && (_fogTarget > 0f || _fogCur > 0.004f);
            if (_fogTarget <= 0f && _fogCur < 0.004f) { _fogOwner = null; _fogCur = 0f; }
            if (!any && !fogOn) { if (_ovlSr != null) { _ovlSr.enabled = false; _fogSr.enabled = false; } return; }
            EnsureOverlay(cam);
            float h = cam.orthographicSize * 2f + 2f, w = h * cam.aspect + 2f;
            if (any) { _ovlSr.enabled = true; bc.a = best; _ovlSr.color = bc; _ovlSr.transform.localScale = new Vector3(w, h, 1f); } else _ovlSr.enabled = false;
            if (fogOn)
            {
                float carLocal = car != null && car.Body != null ? car.Body.position.x - cam.transform.position.x : 0f;
                float pulse = 1f + 0.08f * Mathf.Sin(Time * 1.7f);
                _fogSr.enabled = true; _fogSr.color = new Color(0.95f, 0.97f, 1f, Mathf.Clamp01(_fogCur * pulse));
                _fogSr.transform.localPosition = new Vector3(carLocal + _fogVis * 0.1f, 0f, 5f);
                _fogSr.transform.localScale = new Vector3(60f, h, 1f);
            }
            else _fogSr.enabled = false;
        }

        // ------------------------------------------------------------------ pause and reset
        public void SetPaused(bool paused)
        {
            _paused = paused;
            if (_root != null) _root.gameObject.SetActive(!paused);
            if (_ovlSr != null && paused) { _ovlSr.enabled = false; _fogSr.enabled = false; }
            _tapOn = false;
        }

        /// <summary>Everything this world drew or held goes away at once (run start, end, death, quit, restart).</summary>
        public void Reset()
        {
            if (_pool != null) for (int i = 0; i < PoolSize; i++) Free(_pool[i]);
            for (int i = 0; i < OvlSlots; i++) _ovl[i] = default;
            _fogOwner = null; _fogCur = _fogTarget = 0f;
            if (_ovlSr != null) { _ovlSr.enabled = false; _fogSr.enabled = false; }
            if (_cursor != null) _cursor.enabled = false;
            HopEnabled = false; _hitCd = 0f; _hopCd = 0f; _tapOn = false; _paused = false; Time = 0f; LastGroundY = 0f;
            if (_root != null) _root.gameObject.SetActive(true);
        }

        public string Snapshot() { return "w2{props=" + LiveCount(null) + " hop=" + HopEnabled + " fog=" + _fogCur.ToString("0.00") + " t=" + Time.ToString("0.0") + "}"; }
    }

    // ====================================================================== behaviours

    /// <summary>
    /// Base of every A2 behaviour: applies the event's data channels like the A1 class (so the state always reverts through Fx.Remove), and gives each event a
    /// tick that runs only while it is Active, a seeded RNG, a hold after a respawn, and a cleanup that frees props and overlays on end.
    /// </summary>
    public abstract class Fx2Behaviour : StageEffectBehaviour
    {
        protected readonly StageWorld2 W;
        protected readonly StageEffectHooks Hooks;
        protected readonly StageEffectKind Kind;
        protected Pcg32 R;
        protected StageEventState St;
        protected float T, Hold, SA, SB, Dist;
        protected Color OvlColor; protected float OvlAlpha;

        protected Fx2Behaviour(StageEventDefinition def, StageEffectState fx, StageEffectHooks hooks, StageEffectKind kind, Color ovl = default, float ovlAlpha = 0f) : base(def, fx)
        {
            Hooks = hooks; W = hooks != null ? hooks.World : null; Kind = kind; OvlColor = ovl; OvlAlpha = ovlAlpha;
        }

        protected void Banner(string s) { Hooks?.Banner?.Invoke(s); }
        protected RoadCar Car { get { return W != null ? W.Car : null; } }

        public override void OnStart(StageEventState s)
        {
            base.OnStart(s);
            St = s; T = 0f; Hold = 0f; Dist = s.StartM;
            R = new Pcg32(unchecked(s.TerrainSeed ^ 0xA2C0DEu));
            SA = 0f; SB = 0f;
            foreach (var e in Def.effects) if (e.kind == Kind) { SA = e.a; SB = e.b; }
            if (W != null && OvlAlpha > 0f) W.SetOverlay(this, OvlColor, OvlAlpha);
            if (W != null) Begin();
        }

        public override void OnEnd(StageEventState s)
        {
            base.OnEnd(s);
            if (W != null) { End(); W.FreeOwned(this); W.ClearOverlay(this); }
        }

        public override void OnRespawn(StageEventState s, float distanceM) { base.OnRespawn(s, distanceM); Hold = 2.5f; if (W != null) Respawned(); }

        public override void Tick(float dt, float distanceM)
        {
            base.Tick(dt, distanceM);
            if (W == null || dt <= 0f) return;
            T += dt; Dist = distanceM; if (Hold > 0f) Hold -= dt;
            if (Car == null || Car.Body == null) return;
            Step(dt, distanceM);
        }

        protected virtual void Begin() { }
        protected virtual void End() { }
        protected virtual void Respawned() { }
        protected virtual void Step(float dt, float distanceM) { }
    }

    /// <summary>Pure channel events (Ice Age, Elastic Road, Sticky Road, Mirror Road, Whiteout): the data does the work, this adds the tint, fog and one start line.</summary>
    public sealed class ChannelFxBehaviour : Fx2Behaviour
    {
        private readonly float _fogVis, _fogAlpha; private readonly bool _fog;
        public ChannelFxBehaviour(StageEventDefinition d, StageEffectState fx, StageEffectHooks h, StageEffectKind k, Color c, float a, bool fog = false) : base(d, fx, h, k, c, a) { _fog = fog; }
        protected override void Begin() { if (_fog) W.SetFog(this, SA, Mathf.Clamp(SB > 0f ? SB : 0.9f, 0.3f, 1f)); }
    }

    /// <summary>Gravity Inversion: calm gap, 1 s warning, then the gravity multiplier flips for the window; the rise is capped so it stays survivable.</summary>
    public sealed class GravityInversionBehaviour : Fx2Behaviour
    {
        private int _phase; private float _tm, _window, _calm;
        public GravityInversionBehaviour(StageEventDefinition d, StageEffectState fx, StageEffectHooks h) : base(d, fx, h, StageEffectKind.GravityPulse, new Color(0.6f, 0.45f, 0.95f), 0f) { }
        protected override void Begin() { _window = Mathf.Max(1f, SB); _calm = _window * 2f; _phase = 0; _tm = 4f; }
        protected override void End() { }
        protected override void Respawned() { _phase = 0; _tm = 4f; Fx.SetCur(this, StageEffectKind.GravityPulse, 1f); W.ClearOverlay(this); }
        protected override void Step(float dt, float distanceM)
        {
            if (Hold > 0f) return;
            _tm -= dt;
            if (_phase == 0 && _tm <= 0f) { _phase = 1; _tm = 1f; Banner("GRAVITY FLIPS!"); W.Host.Cue?.Invoke(Def.cueSfxId); W.SetOverlay(this, OvlColor, 0.12f); }
            else if (_phase == 1 && _tm <= 0f) { _phase = 2; _tm = _window; Fx.SetCur(this, StageEffectKind.GravityPulse, SA); W.SetOverlay(this, OvlColor, 0.2f); }
            else if (_phase == 2)
            {
                W.ClampRise(7f);
                if (_tm <= 0f) { _phase = 0; _tm = _calm; Fx.SetCur(this, StageEffectKind.GravityPulse, 1f); W.ClearOverlay(this); }
            }
        }
    }

    /// <summary>Reverse Drive: 1.2 s warning, then the motor factor goes negative for a burst; the factor is read by the manager when it sets the target speed.</summary>
    public sealed class ReverseDriveBehaviour : Fx2Behaviour
    {
        private int _phase; private float _tm, _burst;
        public ReverseDriveBehaviour(StageEventDefinition d, StageEffectState fx, StageEffectHooks h) : base(d, fx, h, StageEffectKind.MotorReverse, new Color(0.9f, 0.4f, 0.35f), 0f) { }
        protected override void Begin() { _burst = Mathf.Max(1f, SB); _phase = 0; _tm = 4f; Fx.SetCur(this, StageEffectKind.MotorReverse, 1f); }
        protected override void Respawned() { _phase = 0; _tm = 4f; Fx.SetCur(this, StageEffectKind.MotorReverse, 1f); W.ClearOverlay(this); }
        protected override void Step(float dt, float distanceM)
        {
            if (Hold > 0f) return;
            _tm -= dt;
            if (_phase == 0 && _tm <= 0f) { _phase = 1; _tm = 1.2f; Banner("REVERSE!"); W.Host.Cue?.Invoke(Def.cueSfxId); W.SetOverlay(this, OvlColor, 0.1f); }
            else if (_phase == 1 && _tm <= 0f) { _phase = 2; _tm = _burst; Fx.SetCur(this, StageEffectKind.MotorReverse, SA < 0f ? SA : -0.5f); }
            else if (_phase == 2 && _tm <= 0f) { _phase = 0; _tm = _burst * 1.8f; Fx.SetCur(this, StageEffectKind.MotorReverse, 1f); W.ClearOverlay(this); }
        }
    }

    /// <summary>Flappy Tunnel: pairs of ceiling and floor blocks form gaps; the short-hop tap (a quick press and release) clears the low floor blocks.</summary>
    public sealed class FlappyTunnelBehaviour : Fx2Behaviour
    {
        private readonly FxProp[] _top = new FxProp[3], _bot = new FxProp[3];
        private float _nextX; private int _n;
        public FlappyTunnelBehaviour(StageEventDefinition d, StageEffectState fx, StageEffectHooks h) : base(d, fx, h, StageEffectKind.FlappyTunnel, new Color(1f, 0.6f, 0.8f), 0.04f) { }
        protected override void Begin() { W.HopEnabled = true; _nextX = Car != null && Car.Body != null ? Car.Body.position.x + 30f : 0f; _n = 0; for (int i = 0; i < 3; i++) { _top[i] = null; _bot[i] = null; } }
        protected override void End() { W.HopEnabled = false; }
        protected override void Respawned() { for (int i = 0; i < 3; i++) { W.Free(_top[i]); W.Free(_bot[i]); _top[i] = _bot[i] = null; } _nextX = Car.Body.position.x + 30f; }
        protected override void Step(float dt, float distanceM)
        {
            float cx = Car.Body.position.x, gap = Mathf.Max(2.2f, SB), spacing = Mathf.Max(18f, SA);
            if (Hold <= 0f && _nextX < cx + 26f && (W.HasRoad(Mathf.Max(_nextX, cx + 22f)) || _n % 2 == 1))
            {
                int slot = -1; for (int i = 0; i < 3; i++) if (_top[i] == null || !_top[i].Live) { slot = i; break; }
                if (slot >= 0)
                {
                    float x = Mathf.Max(_nextX, cx + 22f), g = W.GroundY(x); bool low = R.NextFloat01() < 0.5f;
                    float lowOff = low ? 0.95f : 0f, gapBottom = g + lowOff, gapTop = gapBottom + gap + (low ? 0f : 0.4f);
                    _top[slot] = W.Spawn(this, x, gapTop + 4f, 1.3f, 8f, new Color(0.85f, 0.35f, 0.55f));
                    _bot[slot] = low ? W.Spawn(this, x, g + lowOff * 0.5f - 0.05f, 1.3f, lowOff, new Color(0.95f, 0.55f, 0.7f)) : null;
                    _nextX = x + spacing; _n++;
                }
            }
            for (int i = 0; i < 3; i++)
            {
                var a = _top[i]; if (a == null || !a.Live) continue;
                if (cx - a.X > 10f) { W.Free(a); W.Free(_bot[i]); _top[i] = _bot[i] = null; continue; }
                if (W.Overlaps(a) || (_bot[i] != null && W.Overlaps(_bot[i]))) W.Hit("FLAPPY GATE!");
            }
        }
    }

    /// <summary>Road Collapse: the road behind the car is eaten after a delay (the same trim API as the Road-Eater, which never touches protected road).</summary>
    public sealed class RoadCollapseBehaviour : Fx2Behaviour
    {
        public RoadCollapseBehaviour(StageEventDefinition d, StageEffectState fx, StageEffectHooks h) : base(d, fx, h, StageEffectKind.RoadCollapse, new Color(0.9f, 0.5f, 0.2f), 0.05f) { }
        protected override void Step(float dt, float distanceM)
        {
            if (Hold > 0f || T < 2.5f) return;
            var road = W.Road; if (road == null) return;
            var b = Car.Body; float delay = Mathf.Max(0.5f, SA);
            road.EatBefore(b.position.x - Mathf.Max(5f, b.linearVelocity.x * delay), 0.5f, 1f, 1);
        }
    }

    /// <summary>Magnetic Chaos: steel pylons float ahead and pull the car toward them (acceleration folded into the car's wind channel); touching one costs a heart.</summary>
    public sealed class MagneticChaosBehaviour : Fx2Behaviour
    {
        private readonly FxProp[] _p = new FxProp[3]; private float _nextX;
        public MagneticChaosBehaviour(StageEventDefinition d, StageEffectState fx, StageEffectHooks h) : base(d, fx, h, StageEffectKind.MagneticPylons, new Color(0.6f, 0.7f, 0.9f), 0.04f) { }
        protected override void Begin() { _nextX = Car != null && Car.Body != null ? Car.Body.position.x + 30f : 0f; }
        protected override void End() { Fx.SetCur(this, StageEffectKind.MagneticPylons, 0f, 0f); }
        protected override void Respawned() { for (int i = 0; i < 3; i++) { W.Free(_p[i]); _p[i] = null; } _nextX = Car.Body.position.x + 30f; Fx.SetCur(this, StageEffectKind.MagneticPylons, 0f, 0f); }
        protected override void Step(float dt, float distanceM)
        {
            var b = Car.Body; Vector2 c = b.position; float radius = Mathf.Max(4f, SB), peak = SA;
            if (Hold <= 0f && _nextX < c.x + 26f)
            {
                for (int i = 0; i < 3; i++) if (_p[i] == null || !_p[i].Live)
                {
                    float x = Mathf.Max(_nextX, c.x + 24f);
                    _p[i] = W.Spawn(this, x, W.GroundY(x) + 1.7f + R.NextFloat01() * 1.5f, 1.1f, 1.1f, new Color(0.65f, 0.72f, 0.85f), true);
                    _nextX = x + 36f; break;
                }
            }
            float ax = 0f, ay = 0f;
            for (int i = 0; i < 3; i++)
            {
                var p = _p[i]; if (p == null || !p.Live) continue;
                if (c.x - p.X > 10f) { W.Free(p); _p[i] = null; continue; }
                float dx = p.X - c.x, dy = p.Y - c.y, d = Mathf.Sqrt(dx * dx + dy * dy);
                p.Color = d < radius ? new Color(1f, 0.55f, 0.5f) : new Color(0.65f, 0.72f, 0.85f);
                if (d < radius && d > 0.05f && Hold <= 0f) { float k = peak * (1f - d / radius); ax += dx / d * k; ay += dy / d * k; }
                if (Hold <= 0f && W.Overlaps(p)) { W.Hit("MAGNET SLAM!"); W.Free(p); _p[i] = null; }
            }
            Fx.SetCur(this, StageEffectKind.MagneticPylons, ax, ay);
        }
    }

    /// <summary>Shadow Road and Invisible Ink: drawn road fades out (vertex alpha) a set time after it is first seen; Shadow Road leaves ghost markers, Invisible Ink none.</summary>
    public sealed class RoadFadeBehaviour : Fx2Behaviour
    {
        private const int N = 48; private const float Bucket = 3f;
        private readonly int[] _id = new int[N]; private readonly float[] _seen = new float[N]; private readonly bool[] _faded = new bool[N]; private readonly FxProp[] _mk = new FxProp[N];
        private float _acc; private static readonly Color Clear = new Color(1f, 1f, 1f, 0f);
        public RoadFadeBehaviour(StageEventDefinition d, StageEffectState fx, StageEffectHooks h) : base(d, fx, h, StageEffectKind.RoadFade, new Color(0.25f, 0.2f, 0.4f), 0.06f) { }
        protected override void Begin() { for (int i = 0; i < N; i++) { _id[i] = int.MinValue; _faded[i] = false; _mk[i] = null; } _acc = 0f; }
        protected override void End() { RestoreAll(); }
        protected override void Respawned() { }
        private void RestoreAll()
        {
            var road = W.Road;
            for (int i = 0; i < N; i++)
            {
                if (_faded[i] && road != null) road.TintRange(_id[i] * Bucket, _id[i] * Bucket + Bucket, Color.white);
                _faded[i] = false; W.Free(_mk[i]); _mk[i] = null; _id[i] = int.MinValue;
            }
        }
        protected override void Step(float dt, float distanceM)
        {
            _acc += dt; if (_acc < 0.05f) return; _acc = 0f;                       // 20 Hz
            var road = W.Road; if (road == null) return;
            float cx = Car.Body.position.x, visible = Mathf.Max(0.5f, SA); int every = SB > 0f ? Mathf.Max(1, Mathf.RoundToInt(SB / Bucket)) : 0;
            int b0 = Mathf.FloorToInt((cx - 12f) / Bucket), b1 = Mathf.FloorToInt((cx + 40f) / Bucket);
            for (int b = b0; b <= b1; b++)
            {
                int s = ((b % N) + N) % N;
                float mid = b * Bucket + Bucket * 0.5f;
                bool has = road.HasRoadAt(mid);
                if (_id[s] != b)
                {
                    if (_faded[s]) { _faded[s] = false; W.Free(_mk[s]); _mk[s] = null; }
                    _id[s] = b; _seen[s] = has ? W.Time : -1f; continue;
                }
                if (!has) { _seen[s] = -1f; if (_faded[s]) { _faded[s] = false; W.Free(_mk[s]); _mk[s] = null; } continue; }
                if (_seen[s] < 0f) _seen[s] = W.Time;
                if (!_faded[s] && W.Time - _seen[s] >= visible)
                {
                    road.TintRange(b * Bucket, b * Bucket + Bucket, Clear); _faded[s] = true;
                    if (every > 0 && b % every == 0) _mk[s] = W.Spawn(this, mid, W.GroundY(mid) + 0.45f, 0.4f, 0.4f, new Color(0.85f, 0.8f, 1f, 0.8f), true);
                }
                if (_mk[s] != null && _mk[s].Live) { _mk[s].Y = W.GroundY(mid) + 0.45f; _mk[s].Color = new Color(0.85f, 0.8f, 1f, 0.55f + 0.3f * Mathf.Sin(T * 5f + b)); }
            }
        }
    }

    /// <summary>Double Vehicle: a translucent copy follows the car's recorded path half a second behind; a flipped copy is a "crash" that costs combo.</summary>
    public sealed class DoubleVehicleBehaviour : Fx2Behaviour
    {
        private const int N = 96;
        private readonly float[] _t = new float[N], _x = new float[N], _y = new float[N], _r = new float[N];
        private int _head, _count; private float _lastRec, _crashCd; private FxProp _ghost;
        public DoubleVehicleBehaviour(StageEventDefinition d, StageEffectState fx, StageEffectHooks h) : base(d, fx, h, StageEffectKind.GhostDouble, new Color(0.4f, 0.9f, 1f), 0.03f) { }
        protected override void Begin() { _head = 0; _count = 0; _lastRec = -1f; _crashCd = 0f; _ghost = W.Spawn(this, 0f, -99f, 2.2f, 0.9f, new Color(0.45f, 0.95f, 1f, 0.5f)); if (_ghost != null) _ghost.Sr.sortingOrder = 29; }
        protected override void Respawned() { _count = 0; _head = 0; }
        protected override void Step(float dt, float distanceM)
        {
            var b = Car.Body; float lag = Mathf.Max(0.2f, SA);
            if (W.Time - _lastRec >= 0.033f) { _t[_head] = W.Time; _x[_head] = b.position.x; _y[_head] = b.position.y; _r[_head] = b.rotation; _head = (_head + 1) % N; if (_count < N) _count++; _lastRec = W.Time; }
            if (_crashCd > 0f) _crashCd -= dt;
            if (_ghost == null || !_ghost.Live) return;
            float want = W.Time - lag; int idx = -1;
            for (int k = 0; k < _count; k++) { int i = ((_head - 1 - k) % N + N) % N; if (_t[i] <= want) { idx = i; break; } }
            if (idx < 0) { _ghost.Y = -99f; return; }
            _ghost.X = _x[idx]; _ghost.Y = _y[idx]; _ghost.Rot = _r[idx];
            if (Mathf.Abs(Mathf.DeltaAngle(_r[idx], 0f)) > 100f && _crashCd <= 0f && Hold <= 0f)
            { _crashCd = 2.5f; W.Host.ComboPenalty?.Invoke(3); Banner("GHOST CRASH"); }
        }
    }

    /// <summary>Falling Platforms: solid slabs appear over gaps in the road; a platform the car touches drops one second later.</summary>
    public sealed class FallingPlatformsBehaviour : Fx2Behaviour
    {
        private readonly FxProp[] _p = new FxProp[4]; private float _nextX;
        public FallingPlatformsBehaviour(StageEventDefinition d, StageEffectState fx, StageEffectHooks h) : base(d, fx, h, StageEffectKind.FallingPlatforms, new Color(0.7f, 0.6f, 0.5f), 0.03f) { }
        protected override void Begin() { _nextX = Car != null && Car.Body != null ? Car.Body.position.x + 26f : 0f; }
        protected override void Respawned() { for (int i = 0; i < 4; i++) { W.Free(_p[i]); _p[i] = null; } _nextX = Car.Body.position.x + 26f; }
        protected override void Step(float dt, float distanceM)
        {
            var b = Car.Body; Vector2 c = b.position; float delay = Mathf.Max(0.3f, SA);
            if (Hold <= 0f && _nextX < c.x + 30f)
            {
                float x = Mathf.Max(_nextX, c.x + 26f);
                if (!W.HasRoad(x) && !W.HasRoad(x - 2.8f) && !W.HasRoad(x + 2.8f))
                    for (int i = 0; i < 4; i++) if (_p[i] == null || !_p[i].Live)
                    {
                        _p[i] = W.Spawn(this, x, W.LastGroundY - 0.25f, 4.6f, 0.5f, new Color(0.75f, 0.62f, 0.45f), false, true); _p[i].A = _p[i].Y; _nextX = x + 30f; break;
                    }
                else _nextX = x + 4f;
            }
            for (int i = 0; i < 4; i++)
            {
                var p = _p[i]; if (p == null || !p.Live) continue;
                if (p.State == 0)
                {
                    float top = p.Y + 0.25f;
                    if (Mathf.Abs(c.x - p.X) < p.W * 0.5f + 0.3f && c.y - top > 0.1f && c.y - top < 1.7f && b.linearVelocity.y < 3f) { p.State = 1; p.Life = 0f; }
                    else if (c.x - p.X > 12f) { W.Free(p); _p[i] = null; }
                }
                else if (p.State == 1)
                {
                    p.Life += dt; p.Color = ((int)(p.Life * 10f) & 1) == 0 ? new Color(1f, 0.8f, 0.5f) : new Color(0.75f, 0.62f, 0.45f);
                    if (p.Life >= delay) { p.State = 2; W.SetSolid(p, false); p.Life = 0f; }
                }
                else
                {
                    p.Life += dt; p.Y -= (3f + p.Life * 14f) * dt; p.Color = new Color(0.75f, 0.62f, 0.45f, Mathf.Clamp01(1f - p.Life));
                    if (p.Life > 1.2f) { W.Free(p); _p[i] = null; }
                }
            }
        }
    }

    /// <summary>Shared gate spawner: gates stand across the track every spacing metres; the subclass decides what crossing one does.</summary>
    public abstract class GateBehaviour : Fx2Behaviour
    {
        protected readonly FxProp[] Gates = new FxProp[4]; protected readonly int[] Idx = new int[4]; protected readonly bool[] Passed = new bool[4];
        private float _nextX; protected int Count;
        protected GateBehaviour(StageEventDefinition d, StageEffectState fx, StageEffectHooks h, StageEffectKind k, Color c, float a) : base(d, fx, h, k, c, a) { }
        protected override void Begin() { _nextX = Car != null && Car.Body != null ? Car.Body.position.x + 30f : 0f; Count = 0; for (int i = 0; i < 4; i++) { Gates[i] = null; Passed[i] = false; } GateBegin(); }
        protected virtual void GateBegin() { }
        protected virtual float GateHeight { get { return 9f; } }
        protected abstract Color GateColor(int index);
        protected abstract void OnCross(int index, FxProp gate);
        protected virtual void GateStep(float dt) { }
        protected override void Respawned() { for (int i = 0; i < 4; i++) { W.Free(Gates[i]); Gates[i] = null; } _nextX = Car.Body.position.x + 30f; GateRespawned(); }
        protected virtual void GateRespawned() { }
        protected override void Step(float dt, float distanceM)
        {
            float cx = Car.Body.position.x, sp = Mathf.Max(20f, SB > 0f ? SB : 50f);
            if (Hold <= 0f && _nextX < cx + 34f)
                for (int i = 0; i < 4; i++) if (Gates[i] == null || !Gates[i].Live)
                {
                    float x = Mathf.Max(_nextX, cx + 28f);
                    Gates[i] = W.Spawn(this, x, W.GroundY(x) + GateHeight * 0.35f, 0.45f, GateHeight, GateColor(Count)); Idx[i] = Count; Passed[i] = false; Count++; _nextX = x + sp; break;
                }
            GateStep(dt);
            for (int i = 0; i < 4; i++)
            {
                var g = Gates[i]; if (g == null || !g.Live) continue;
                if (!Passed[i] && cx >= g.X) { Passed[i] = true; OnCross(Idx[i], g); }
                if (cx - g.X > 12f) { W.Free(g); Gates[i] = null; }
            }
        }
    }

    /// <summary>Reverse Gravity Zones: gates alternate between normal gravity and a reversed well; the rise is capped inside the well.</summary>
    public sealed class GravityGatesBehaviour : GateBehaviour
    {
        private bool _inv;
        public GravityGatesBehaviour(StageEventDefinition d, StageEffectState fx, StageEffectHooks h) : base(d, fx, h, StageEffectKind.GravityGates, new Color(0.6f, 0.45f, 0.95f), 0.0f) { }
        protected override void GateBegin() { _inv = false; Fx.SetCur(this, StageEffectKind.GravityGates, 1f); }
        protected override void GateRespawned() { _inv = false; Fx.SetCur(this, StageEffectKind.GravityGates, 1f); W.ClearOverlay(this); }
        protected override Color GateColor(int i) { return (i & 1) == 0 ? new Color(0.7f, 0.5f, 1f, 0.7f) : new Color(0.4f, 1f, 0.6f, 0.7f); }
        protected override void OnCross(int i, FxProp g)
        {
            _inv = !_inv; Fx.SetCur(this, StageEffectKind.GravityGates, _inv ? SA : 1f);
            Banner(_inv ? "ZONE: FLOAT" : "ZONE: NORMAL"); W.Host.Cue?.Invoke(Def.cueSfxId);
            if (_inv) W.SetOverlay(this, OvlColor, 0.14f); else W.ClearOverlay(this);
        }
        protected override void GateStep(float dt) { if (_inv) W.ClampRise(7f); }
    }

    /// <summary>Wind Tunnels: every gate turns the wind (tail, head, up, down).</summary>
    public sealed class WindTunnelsBehaviour : GateBehaviour
    {
        private static readonly string[] Names = { "TAILWIND", "HEADWIND", "UPDRAFT", "DOWNDRAFT" };
        public WindTunnelsBehaviour(StageEventDefinition d, StageEffectState fx, StageEffectHooks h) : base(d, fx, h, StageEffectKind.WindTunnels, new Color(0.3f, 0.8f, 0.75f), 0.03f) { }
        protected override void GateRespawned() { Fx.SetCur(this, StageEffectKind.WindTunnels, 0f, 0f); }
        protected override Color GateColor(int i) { return new Color(0.35f, 0.9f, 0.8f, 0.6f); }
        protected override void OnCross(int i, FxProp g)
        {
            int d = i & 3; float a = SA;
            Fx.SetCur(this, StageEffectKind.WindTunnels, d == 0 ? a : (d == 1 ? -a : 0f), d == 2 ? a * 0.45f : (d == 3 ? -a * 0.45f : 0f));
            Banner(Names[d]); W.Host.Cue?.Invoke(Def.cueSfxId);
        }
        protected override void End() { Fx.SetCur(this, StageEffectKind.WindTunnels, 0f, 0f); }
    }

    /// <summary>Rhythm Gates: a music-free metronome; gates pulse open on the beat, a mistimed crossing drains a fifth of the ink tank.</summary>
    public sealed class RhythmGatesBehaviour : GateBehaviour
    {
        private int _lastBeat;
        public RhythmGatesBehaviour(StageEventDefinition d, StageEffectState fx, StageEffectHooks h) : base(d, fx, h, StageEffectKind.RhythmGates, new Color(0.95f, 0.5f, 0.9f), 0.03f) { }
        protected override void GateBegin() { W.Bpm = Mathf.Clamp(SA, 60f, 140f); _lastBeat = W.BeatCount; }
        protected override Color GateColor(int i) { return Color.white; }
        private bool Open { get { float p = W.BeatPhase; return p < 0.22f || p > 0.9f; } }
        protected override void GateStep(float dt)
        {
            bool open = Open; float pulse = 0.55f + 0.45f * (1f - Mathf.Min(W.BeatPhase, 1f - W.BeatPhase) * 2f);
            bool near = false;
            for (int i = 0; i < 4; i++)
            {
                var g = Gates[i]; if (g == null || !g.Live) continue;
                g.Color = open ? new Color(0.3f, 1f, 0.5f, 0.55f + 0.3f * pulse) : new Color(1f, 0.3f, 0.35f, 0.6f); g.W = open ? 0.35f : 0.7f;
                if (g.X - Car.Body.position.x < 26f) near = true;
            }
            if (W.BeatCount != _lastBeat) { _lastBeat = W.BeatCount; if (near) W.Host.Cue?.Invoke(Def.cueSfxId); }
        }
        protected override void OnCross(int i, FxProp g)
        {
            if (Hold > 0f) return;
            if (Open) Banner("ON BEAT"); else { Banner("OFF BEAT"); W.Host.DrainInk01?.Invoke(0.2f); }
        }
    }

    /// <summary>Electric Fences: tall barriers pulse on a cycle; safe for the window, charged (and hurtful) for the rest, with a flicker before charging.</summary>
    public sealed class ElectricFencesBehaviour : Fx2Behaviour
    {
        private readonly FxProp[] _f = new FxProp[3]; private readonly float[] _off = new float[3]; private float _nextX;
        public ElectricFencesBehaviour(StageEventDefinition d, StageEffectState fx, StageEffectHooks h) : base(d, fx, h, StageEffectKind.ElectricFences, new Color(0.5f, 0.6f, 1f), 0.04f) { }
        protected override void Begin() { _nextX = Car != null && Car.Body != null ? Car.Body.position.x + 30f : 0f; }
        protected override void Respawned() { for (int i = 0; i < 3; i++) { W.Free(_f[i]); _f[i] = null; } _nextX = Car.Body.position.x + 30f; }
        protected override void Step(float dt, float distanceM)
        {
            float cx = Car.Body.position.x, cycle = Mathf.Max(1f, SA), safe = Mathf.Clamp(SB, 0.3f, cycle - 0.3f);
            if (Hold <= 0f && _nextX < cx + 32f)
                for (int i = 0; i < 3; i++) if (_f[i] == null || !_f[i].Live)
                {
                    float x = Mathf.Max(_nextX, cx + 27f);
                    _f[i] = W.Spawn(this, x, W.GroundY(x) + 2.3f, 0.4f, 4.6f, Color.white); _off[i] = R.NextFloat01() * cycle; _nextX = x + 44f; break;
                }
            for (int i = 0; i < 3; i++)
            {
                var f = _f[i]; if (f == null || !f.Live) continue;
                if (cx - f.X > 10f) { W.Free(f); _f[i] = null; continue; }
                float ph = (W.Time + _off[i]) % cycle; bool charged = ph >= safe;
                if (charged) f.Color = new Color(1f, 0.95f, 0.4f, 0.95f);
                else if (safe - ph < 0.35f) f.Color = ((int)(W.Time * 20f) & 1) == 0 ? new Color(1f, 0.95f, 0.4f, 0.4f) : new Color(0.35f, 0.9f, 0.5f, 0.45f);   // flicker before it charges
                else f.Color = new Color(0.35f, 0.9f, 0.5f, 0.45f);
                if (charged && Hold <= 0f && W.Overlaps(f)) W.Hit("ELECTRIC FENCE!");
            }
        }
    }

    /// <summary>Flood Surge (water, steps up) and Lava Chase (lava, rises steadily): a liquid plane below the car rises; Flood slows and finally drowns, Lava burns on contact.</summary>
    public sealed class RisingLiquidBehaviour : Fx2Behaviour
    {
        private readonly bool _lava; private FxProp _liq; private float _level, _remain, _stepT, _sub; private bool _needReset;
        public RisingLiquidBehaviour(StageEventDefinition d, StageEffectState fx, StageEffectHooks h, bool lava) : base(d, fx, h, lava ? StageEffectKind.LavaChase : StageEffectKind.FloodSurge, lava ? new Color(1f, 0.4f, 0.1f) : new Color(0.2f, 0.5f, 0.9f), 0.04f) { _lava = lava; }
        private float Depth { get { return _lava ? Mathf.Max(3f, SB) : 5f; } }
        protected override void Begin()
        {
            _level = Car.Body.position.y - Depth; _remain = 0f; _stepT = _lava ? 0f : Mathf.Max(1f, SB); _sub = 0f; _needReset = false;
            _liq = W.Spawn(this, Car.Body.position.x, _level - 20f, 60f, 40f, _lava ? new Color(1f, 0.35f, 0.05f, 0.85f) : new Color(0.15f, 0.45f, 0.95f, 0.5f));
            if (_liq != null) _liq.Sr.sortingOrder = 31;
        }
        protected override void End() { Fx.SetCur(this, StageEffectKind.FloodSurge, 0f); }
        protected override void Respawned() { _needReset = true; _sub = 0f; Fx.SetCur(this, StageEffectKind.FloodSurge, 0f); }
        protected override void Step(float dt, float distanceM)
        {
            var b = Car.Body;
            if (_needReset && Hold <= 0f) { _level = Mathf.Min(_level, b.position.y - Depth); _remain = 0f; _needReset = false; }
            if (Hold <= 0f)
            {
                if (_lava) _level += Mathf.Max(0f, SA) * dt;
                else
                {
                    _stepT -= dt; if (_stepT <= 0f) { _remain += Mathf.Max(0.2f, SA); _stepT = Mathf.Max(1f, SB); }
                    float d = Mathf.Min(_remain, dt * 1f); _level += d; _remain -= d;
                }
            }
            if (_liq != null && _liq.Live) { _liq.X = b.position.x; _liq.Y = _level - 20f; if (_lava) _liq.Color = new Color(1f, 0.3f + 0.1f * Mathf.Sin(T * 4f), 0.05f, 0.85f); }
            bool under = b.position.y - 0.2f < _level;
            if (Hold > 0f) return;
            if (_lava) { if (under) W.Hit("LAVA!"); }
            else
            {
                Fx.SetCur(this, StageEffectKind.FloodSurge, under ? 0.6f : 0f);
                if (under) { _sub += dt; if (_sub > 1.6f) { _sub = 0f; W.Hit("DROWNED!"); } } else _sub = Mathf.Max(0f, _sub - dt);
            }
        }
    }

    /// <summary>Traffic Convoy: slow solid vehicles drive along the drawn road ahead; the car has to go over or around. A vehicle that blocks the car for 3 s pulls off.</summary>
    public sealed class TrafficConvoyBehaviour : Fx2Behaviour
    {
        private readonly FxProp[] _v = new FxProp[3]; private float _spawnT;
        public TrafficConvoyBehaviour(StageEventDefinition d, StageEffectState fx, StageEffectHooks h) : base(d, fx, h, StageEffectKind.TrafficConvoy, new Color(0.9f, 0.7f, 0.3f), 0.03f) { }
        protected override void Begin() { _spawnT = 3f; }
        protected override void Respawned() { for (int i = 0; i < 3; i++) { W.Free(_v[i]); _v[i] = null; } _spawnT = 3f; }
        protected override void Step(float dt, float distanceM)
        {
            var b = Car.Body; float cx = b.position.x, spd = Mathf.Max(1f, SA);
            _spawnT -= dt;
            if (_spawnT <= 0f && Hold <= 0f)
            {
                float x = cx + 34f;
                if (W.HasRoad(x) && W.HasRoad(x + 3f))
                    for (int i = 0; i < 3; i++) if (_v[i] == null || !_v[i].Live)
                    {
                        _v[i] = W.Spawn(this, x, W.GroundY(x) + 0.75f, 3.2f, 1.2f, i == 1 ? new Color(0.35f, 0.6f, 0.9f) : new Color(0.95f, 0.6f, 0.2f), false, true); _spawnT = Mathf.Max(2f, SB); break;
                    }
                else _spawnT = 0.5f;
            }
            var road = W.Road;
            for (int i = 0; i < 3; i++)
            {
                var v = _v[i]; if (v == null || !v.Live) continue;
                v.X += spd * dt;
                if (cx - v.X > 14f || road == null || !road.TryGetRoadYAtX(v.X, out float gy)) { W.Free(v); _v[i] = null; continue; }
                v.Y = gy + 0.75f;
                if (road.TryGetRoadYAtX(v.X + 1.2f, out float g1) && road.TryGetRoadYAtX(v.X - 1.2f, out float g0)) v.Rot = Mathf.Atan2(g1 - g0, 2.4f) * Mathf.Rad2Deg;
                float gap = v.X - cx;
                if (gap > 0f && gap < 4.8f && b.linearVelocity.x < 0.8f) { v.A += dt; if (v.A > 3f) { W.Free(v); _v[i] = null; } } else v.A = Mathf.Max(0f, v.A - dt);
            }
        }
    }

    /// <summary>Rolling Boulder: a marked drop, a crater where it lands, then it rolls back toward the car along the road (into the crater it just made). Damage on contact.</summary>
    public sealed class RollingBoulderBehaviour : Fx2Behaviour
    {
        private FxProp _b, _mark; private int _st; private float _tm, _vy, _x;
        public RollingBoulderBehaviour(StageEventDefinition d, StageEffectState fx, StageEffectHooks h) : base(d, fx, h, StageEffectKind.RollingBoulder, new Color(0.7f, 0.55f, 0.4f), 0.03f) { }
        protected override void Begin() { _st = 0; _tm = 3f; }
        protected override void Respawned() { W.Free(_b); W.Free(_mark); _b = _mark = null; _st = 0; _tm = 3f; }
        protected override void Step(float dt, float distanceM)
        {
            var body = Car.Body; float cx = body.position.x;
            if (_st == 0)
            {
                _tm -= dt;
                if (_tm <= 0f && Hold <= 0f)
                {
                    _x = cx + 24f + R.NextFloat01() * 6f;
                    if (!W.HasRoad(_x)) { _tm = 0.4f; return; }
                    _mark = W.Spawn(this, _x, W.GroundY(_x) + 0.1f, 2.4f, 0.4f, new Color(1f, 0.3f, 0.2f, 0.7f), true); _st = 1; _tm = 0.9f; Banner("BOULDER!"); W.Host.Cue?.Invoke(Def.cueSfxId);
                }
            }
            else if (_st == 1)
            {
                _tm -= dt; if (_mark != null) _mark.Color = new Color(1f, 0.3f, 0.2f, 0.4f + 0.4f * Mathf.Sin(T * 18f));
                if (_tm <= 0f)
                {
                    W.Free(_mark); _mark = null; _b = W.Spawn(this, _x, W.GroundY(_x) + 10f, 2.2f, 2.2f, new Color(0.55f, 0.45f, 0.35f), true); _vy = 0f; _st = 2;
                }
            }
            else if (_b == null || !_b.Live) { _st = 0; _tm = Mathf.Max(4f, SB); }
            else
            {
                float r = _b.W * 0.5f; var road = W.Road; bool on = road != null && road.TryGetRoadYAtX(_b.X, out float gy) && _b.Y - r <= gy + 0.05f;
                if (_st == 2)
                {
                    _vy -= 20f * dt; _b.Y += _vy * dt;
                    if (road != null && road.TryGetRoadYAtX(_b.X, out float g2) && _b.Y - r <= g2) { _b.Y = g2 + r; if (!road.IsRangeProtected(_b.X - 1.6f, _b.X + 1.6f)) road.CutRange(_b.X - 1.6f, _b.X + 1.6f); _st = 3; }
                    else if (_b.Y < body.position.y - 16f) { W.Free(_b); _b = null; }
                }
                else
                {
                    _b.X -= Mathf.Max(2f, SA) * dt; _b.Rot += 240f * dt;
                    if (road != null && road.TryGetRoadYAtX(_b.X, out float g3)) _b.Y = g3 + r; else { _vy -= 20f * dt; _b.Y += _vy * dt; }
                    if (_b.Y < body.position.y - 16f || cx - _b.X > 12f) { W.Free(_b); _b = null; }
                    else if (Hold <= 0f && W.Overlaps(_b)) { W.Hit("BOULDER!"); W.Free(_b); _b = null; }
                }
            }
        }
    }

    /// <summary>Road Laser: a red beam telegraphs 1.1 s, then one strip of road ahead is cut (protected road is skipped); the car must bridge or hop the gap.</summary>
    public sealed class RoadLaserBehaviour : Fx2Behaviour
    {
        private FxProp _beam; private float _tm, _x; private int _st;
        public RoadLaserBehaviour(StageEventDefinition d, StageEffectState fx, StageEffectHooks h) : base(d, fx, h, StageEffectKind.RoadLaser, new Color(1f, 0.3f, 0.4f), 0.03f) { }
        protected override void Begin() { _st = 0; _tm = 3f; }
        protected override void Respawned() { W.Free(_beam); _beam = null; _st = 0; _tm = 3f; }
        protected override void Step(float dt, float distanceM)
        {
            var b = Car.Body; float width = Mathf.Max(1f, SA);
            if (_st == 0)
            {
                _tm -= dt;
                if (_tm <= 0f && Hold <= 0f)
                {
                    _x = b.position.x + Mathf.Max(14f, b.linearVelocity.x * 2.4f + 4f);
                    if (!W.HasRoad(_x) || Car.Body == null) { _tm = 0.4f; return; }
                    _beam = W.Spawn(this, _x, W.GroundY(_x) + 3f, 0.25f, 14f, new Color(1f, 0.25f, 0.3f, 0.7f)); _st = 1; _tm = 1.1f; Banner("LASER!"); W.Host.Cue?.Invoke(Def.cueSfxId);
                }
            }
            else if (_st == 1)
            {
                _tm -= dt; if (_beam != null) { _beam.Color = new Color(1f, 0.25f, 0.3f, 0.35f + 0.5f * Mathf.Abs(Mathf.Sin(T * 14f))); _beam.W = 0.25f + 0.2f * (1f - _tm); }
                if (_tm <= 0f)
                {
                    var road = W.Road;
                    if (road != null && !road.IsRangeProtected(_x - width * 0.5f, _x + width * 0.5f)) road.CutRange(_x - width * 0.5f, _x + width * 0.5f);
                    if (_beam != null) { _beam.Color = Color.white; _beam.W = width; }
                    _st = 2; _tm = 0.15f;
                }
            }
            else { _tm -= dt; if (_tm <= 0f) { W.Free(_beam); _beam = null; _st = 0; _tm = Mathf.Max(3f, SB); } }
        }
    }

    /// <summary>Falling Stars: stars drop from above; one that lands becomes a temporary platform (blinks before it goes); catching one in flight pays 3 coins.</summary>
    public sealed class FallingStarsBehaviour : Fx2Behaviour
    {
        private readonly FxProp[] _s = new FxProp[6]; private float _tm;
        public FallingStarsBehaviour(StageEventDefinition d, StageEffectState fx, StageEffectHooks h) : base(d, fx, h, StageEffectKind.FallingStars, new Color(0.7f, 0.75f, 1f), 0.04f) { }
        protected override void Begin() { _tm = 2f; }
        protected override void Respawned() { for (int i = 0; i < 6; i++) { W.Free(_s[i]); _s[i] = null; } _tm = 2f; }
        protected override void Step(float dt, float distanceM)
        {
            var b = Car.Body; Vector2 c = b.position; float life = Mathf.Max(1.5f, SA);
            _tm -= dt;
            if (_tm <= 0f && Hold <= 0f)
                for (int i = 0; i < 6; i++) if (_s[i] == null || !_s[i].Live)
                {
                    float x = c.x + 16f + R.NextFloat01() * 14f;
                    float ty = (W.HasRoad(x) ? W.GroundY(x) + 1.8f : W.LastGroundY + 0.3f + R.NextFloat01() * 1.6f);
                    _s[i] = W.Spawn(this, x, c.y + 11f, 0.9f, 0.9f, new Color(1f, 0.92f, 0.35f), true); _s[i].A = ty; _s[i].State = 0; _tm = Mathf.Max(1f, SB); break;
                }
            for (int i = 0; i < 6; i++)
            {
                var s = _s[i]; if (s == null || !s.Live) continue;
                if (c.x - s.X > 12f) { W.Free(s); _s[i] = null; continue; }
                if (s.State == 0)
                {
                    s.Y -= 7f * dt; s.Rot += 200f * dt;
                    if (W.Overlaps(s)) { W.Host.AddCoins?.Invoke(3); W.Free(s); _s[i] = null; continue; }
                    if (s.Y <= s.A) { s.Y = s.A; s.State = 1; s.Life = life; W.SetShape(s, false); s.Rot = 0f; s.W = 3f; s.H = 0.4f; W.SetSolid(s, true); }
                }
                else
                {
                    s.Life -= dt; bool blink = s.Life < 1f && ((int)(s.Life * 10f) & 1) == 0;
                    s.Color = blink ? new Color(1f, 0.92f, 0.35f, 0.3f) : new Color(1f, 0.92f, 0.35f, 0.9f);
                    if (s.Life <= 0f) { W.Free(s); _s[i] = null; }
                }
            }
        }
    }

    /// <summary>Vehicle Swap: every N metres the car switches to a seeded class profile (light, heavy, floaty, rocket) through the speed, gravity and scale channels.</summary>
    public sealed class VehicleSwapBehaviour : Fx2Behaviour
    {
        private static readonly string[] Lines = { "SWAP: OWN", "SWAP: LIGHT", "SWAP: HEAVY", "SWAP: FLOATY", "SWAP: ROCKET" };
        private float _nextM; private int _cur;
        public VehicleSwapBehaviour(StageEventDefinition d, StageEffectState fx, StageEffectHooks h) : base(d, fx, h, StageEffectKind.VehicleSwap, new Color(1f, 0.8f, 0.4f), 0f) { }
        protected override void Begin() { _cur = 0; Fx.SetCur(this, StageEffectKind.VehicleSwap, 0f); _nextM = St.StartM + 20f; }
        protected override void Step(float dt, float distanceM)
        {
            if (distanceM < _nextM || Hold > 0f) return;
            int k = 1 + R.NextInt(4); if (k == _cur) k = 1 + (k % 4); _cur = k;
            Fx.SetCur(this, StageEffectKind.VehicleSwap, k); Banner(Lines[k]); W.Host.Cue?.Invoke(Def.cueSfxId);
            _nextM = distanceM + Mathf.Max(40f, SA);
        }
    }

    /// <summary>Chaos Finale: two of the event's modifier effects (chosen by variant) plus an elite strike fan: three marked impact zones every few seconds.</summary>
    public sealed class ChaosFinaleBehaviour : Fx2Behaviour
    {
        private readonly FxProp[] _m = new FxProp[3]; private readonly float[] _tm = new float[3]; private float _next;
        public ChaosFinaleBehaviour(StageEventDefinition d, StageEffectState fx, StageEffectHooks h) : base(d, fx, h, StageEffectKind.ChaosFinale, new Color(1f, 0.3f, 0.3f), 0.04f) { }
        protected override void Begin()
        {
            Fx.Remove(this);
            var list = new List<StageEffectSpec>();
            foreach (var e in Def.effects) if (e.kind != StageEffectKind.ChaosFinale) list.Add(e);
            if (list.Count >= 2)
            {
                int v = Mathf.Max(0, St.Variant), i0 = v % list.Count, i1 = (i0 + 1 + (v / list.Count) % (list.Count - 1)) % list.Count;
                Fx.Add(this, list[i0].kind, list[i0].a, list[i0].b); Fx.Add(this, list[i1].kind, list[i1].a, list[i1].b);
            }
            _next = 4f;
        }
        protected override void Respawned() { for (int i = 0; i < 3; i++) { W.Free(_m[i]); _m[i] = null; } _next = 4f; }
        protected override void Step(float dt, float distanceM)
        {
            var b = Car.Body;
            _next -= dt;
            if (_next <= 0f && Hold <= 0f)
            {
                _next = Mathf.Max(2.5f, SA); float x0 = b.position.x + Mathf.Max(8f, b.linearVelocity.x * 1.9f);
                for (int i = 0; i < 3; i++)
                {
                    float x = x0 + i * 5.5f; _m[i] = W.Spawn(this, x, W.GroundY(x) + 0.15f, 4.2f, 0.5f, new Color(1f, 0.25f, 0.2f, 0.6f), true); _tm[i] = 1.2f + i * 0.2f;
                }
                Banner("STRIKES!"); W.Host.Cue?.Invoke(Def.cueSfxId);
            }
            for (int i = 0; i < 3; i++)
            {
                var m = _m[i]; if (m == null || !m.Live) continue;
                _tm[i] -= dt; m.Color = new Color(1f, 0.25f, 0.2f, 0.35f + 0.4f * Mathf.Abs(Mathf.Sin(T * 12f)));
                if (_tm[i] > 0f) continue;
                float dx = b.position.x - m.X, dy = b.position.y - m.Y;
                if (dx * dx + dy * dy < 2.4f * 2.4f) W.Hit("ELITE STRIKE!");
                W.Free(m); _m[i] = null;
            }
        }
    }

    /// <summary>Registers the A2 behaviours by effect kind (no switch on event ids). Returns null for a definition that has no A2 kind.</summary>
    public static class StageEffects2
    {
        private delegate Fx2Behaviour Make(StageEventDefinition d, StageEffectState fx, StageEffectHooks h);
        private static readonly Dictionary<StageEffectKind, Make> Table = new Dictionary<StageEffectKind, Make>
        {
            { StageEffectKind.GravityPulse, (d, f, h) => new GravityInversionBehaviour(d, f, h) },
            { StageEffectKind.Whiteout, (d, f, h) => new ChannelFxBehaviour(d, f, h, StageEffectKind.Whiteout, new Color(0.95f, 0.97f, 1f), 0.12f, true) },
            { StageEffectKind.MotorReverse, (d, f, h) => new ReverseDriveBehaviour(d, f, h) },
            { StageEffectKind.MirrorInput, (d, f, h) => new ChannelFxBehaviour(d, f, h, StageEffectKind.MirrorInput, new Color(0.6f, 0.7f, 1f), 0.05f) },
            { StageEffectKind.FlappyTunnel, (d, f, h) => new FlappyTunnelBehaviour(d, f, h) },
            { StageEffectKind.RoadCollapse, (d, f, h) => new RoadCollapseBehaviour(d, f, h) },
            { StageEffectKind.IceAge, (d, f, h) => new ChannelFxBehaviour(d, f, h, StageEffectKind.IceAge, new Color(0.65f, 0.85f, 1f), 0.09f) },
            { StageEffectKind.MagneticPylons, (d, f, h) => new MagneticChaosBehaviour(d, f, h) },
            { StageEffectKind.ElasticRoad, (d, f, h) => new ChannelFxBehaviour(d, f, h, StageEffectKind.ElasticRoad, new Color(1f, 0.6f, 0.85f), 0.06f) },
            { StageEffectKind.RoadFade, (d, f, h) => new RoadFadeBehaviour(d, f, h) },
            { StageEffectKind.GhostDouble, (d, f, h) => new DoubleVehicleBehaviour(d, f, h) },
            { StageEffectKind.FallingPlatforms, (d, f, h) => new FallingPlatformsBehaviour(d, f, h) },
            { StageEffectKind.GravityGates, (d, f, h) => new GravityGatesBehaviour(d, f, h) },
            { StageEffectKind.TrafficConvoy, (d, f, h) => new TrafficConvoyBehaviour(d, f, h) },
            { StageEffectKind.RollingBoulder, (d, f, h) => new RollingBoulderBehaviour(d, f, h) },
            { StageEffectKind.ElectricFences, (d, f, h) => new ElectricFencesBehaviour(d, f, h) },
            { StageEffectKind.FloodSurge, (d, f, h) => new RisingLiquidBehaviour(d, f, h, false) },
            { StageEffectKind.LavaChase, (d, f, h) => new RisingLiquidBehaviour(d, f, h, true) },
            { StageEffectKind.WindTunnels, (d, f, h) => new WindTunnelsBehaviour(d, f, h) },
            { StageEffectKind.StickyRoad, (d, f, h) => new ChannelFxBehaviour(d, f, h, StageEffectKind.StickyRoad, new Color(1f, 0.75f, 0.3f), 0.07f) },
            { StageEffectKind.RoadLaser, (d, f, h) => new RoadLaserBehaviour(d, f, h) },
            { StageEffectKind.FallingStars, (d, f, h) => new FallingStarsBehaviour(d, f, h) },
            { StageEffectKind.VehicleSwap, (d, f, h) => new VehicleSwapBehaviour(d, f, h) },
            { StageEffectKind.RhythmGates, (d, f, h) => new RhythmGatesBehaviour(d, f, h) },
            { StageEffectKind.ChaosFinale, (d, f, h) => new ChaosFinaleBehaviour(d, f, h) },
        };

        public static IStageEventBehaviour TryCreate(StageEventDefinition def, StageEffectState fx, StageEffectHooks hooks)
        {
            if (def == null || def.effects == null) return null;
            foreach (var e in def.effects) if (Table.TryGetValue(e.kind, out var make)) return make(def, fx, hooks);
            return null;
        }
    }
}
