using System;
using UnityEngine;

namespace RoadBuilderGame
{
    /// <summary>
    /// Scene side of RO-B0: hover controller, the projectile pools (3 bolts, 2 bombs, 2 rollers, rings), tells, flight, hit and near-miss tests, and the procedural shapes.
    /// All decisions (what to fire, phases, meter) come from <see cref="BossModel"/>; all numbers from <see cref="BossDefinitionSO"/> and the projectile assets.
    /// Everything is allocated once in <see cref="Begin"/>; <see cref="Tick"/> allocates nothing. Plain class owned by the manager; the manager supplies every outside effect as a delegate.
    /// </summary>
    public sealed class BossController
    {
        private sealed class Proj
        {
            public bool Active, Warning, Dud, Resolved;
            public ProjectileKind Kind; public BeatMode Mode; public ProjectileDefinitionSO Def;
            public int Stage;                      // 0 tell, 1 flight or roll, 2 blast
            public float T, Life, MinDist = 999f, TravelM, SpawnCarX, NoRoadFor, Aim;
            public Vector2 Pos, Dir, Start, Target;
            public bool Locked;
            public Transform Body, Tell; public SpriteRenderer BodySr, TellSr;
        }

        private sealed class Ring { public bool Active; public Vector2 Pos; public float Age; public Transform T; public SpriteRenderer Sr; }

        private readonly RoadBuilder _road;
        private readonly BossDefinitionSO _d;
        private readonly BossModel _model;
        private readonly Proj[] _pool;
        private readonly int[] _poolStart = new int[3], _poolCount = new int[3];
        private readonly Ring[] _rings = new Ring[2];
        private readonly float[] _lastFire = new float[3] { -10f, -10f, -10f };
        private readonly ProjectileSkinSO[] _skins = new ProjectileSkinSO[3];
        private Transform _boss; private SpriteRenderer _bossBody, _bossTrim;
        private Vector2 _pos, _vel;
        private float _clock, _charge = 1f, _phx, _phy;
        private Pcg32 _rng;
        private bool _built, _gone;

        public BossModel Model => _model;
        public Func<bool> CanDamage = () => true;
        /// <summary>The car is hit: the manager takes a heart (a shield absorbs it) and returns true when a shield did.</summary>
        public Func<string, bool> Hit = r => false;
        public Func<bool> CounterAvailable = () => true, InkAvailable = () => true;
        /// <summary>source index, perfect, metres travelled during the tell.</summary>
        public Action<int, bool, float> NearMissed;
        public Action<string> Cue, Banner;
        public Action<float> Shake;
        public Action RingTaken;
        /// <summary>The fight is decided: true = timeout retreat, false = Overload defeat.</summary>
        public Action<bool> Decided;

        public BossController(RoadBuilder road, BossDefinitionSO data)
        {
            _road = road; _d = data; _model = new BossModel(data);
            _pool = new Proj[Count(data.bolt) + Count(data.bomb) + Count(data.roller)];
            _model.Beat += OnBeat;
            _model.Raised += OnEvent;
        }

        private static int Count(ProjectileDefinitionSO p) { return p != null ? Mathf.Max(1, p.poolSize) : 0; }

        public ProjectileSkinSO Skin(ProjectileKind k) { return _skins[(int)k]; }
        public void SetSkin(ProjectileKind k, ProjectileSkinSO skin) { if (skin != null) _skins[(int)k] = skin; }

        public static ProjectileSkinSO DefaultSkin(ProjectileKind k)
        {
            var s = ScriptableObject.CreateInstance<ProjectileSkinSO>(); s.worldId = "country";
            switch (k)
            {
                case ProjectileKind.Bolt: s.archetypeId = "PRJ-BOLT"; s.displayName = "Corn Shot"; s.tint = new Color(1f, 0.85f, 0.2f); s.scale = 0.8f; break;
                case ProjectileKind.Bomb: s.archetypeId = "PRJ-BOMB"; s.displayName = "Pumpkin Bomb"; s.tint = new Color(0.95f, 0.5f, 0.1f); s.scale = 1.3f; break;
                default: s.archetypeId = "PRJ-SAW"; s.displayName = "Hay Roller"; s.tint = new Color(0.86f, 0.75f, 0.35f); s.scale = 1.2f; break;
            }
            return s;
        }

        public void Begin(uint seed)
        {
            Build();
            _model.Reset(seed); _rng = new Pcg32(seed ^ 0x9E3779B9u); _clock = 0f; _gone = false; _charge = 1f;
            _phx = _rng.NextFloat01() * 6.283f; _phy = _rng.NextFloat01() * 6.283f;
            for (int i = 0; i < _lastFire.Length; i++) _lastFire[i] = -10f;
            ClearAll(); _boss.gameObject.SetActive(false);
        }

        public void Stop() { if (!_built) return; _model.Reset(1u); ClearAll(); _boss.gameObject.SetActive(false); }

        /// <summary>The boss event started: hover in, 2 s intro, then phase 1.</summary>
        public void Start(Vector2 carPos)
        {
            Build();
            _pos = carPos + new Vector2(16f, 13f); _vel = Vector2.zero; _gone = false;
            _boss.gameObject.SetActive(true);
            _model.Begin();
        }

        public void OnRespawn() { _model.Interrupt(); }

        private void Build()
        {
            if (_built) return;
            for (int k = 0; k < 3; k++) if (_skins[k] == null) _skins[k] = DefaultSkin((ProjectileKind)k);
            int n = 0;
            for (int k = 0; k < 3; k++)
            {
                var def = _d.Projectile((ProjectileKind)k);
                _poolStart[k] = n; _poolCount[k] = Count(def);
                for (int i = 0; i < _poolCount[k]; i++)
                {
                    var p = new Proj { Kind = (ProjectileKind)k, Def = def };
                    p.Body = SpriteFactory.CreateSprite("Prj" + k + "_" + i, null, _skins[k].sprite != null ? _skins[k].sprite : SpriteFactory.Circle, _skins[k].tint, 22).transform;
                    p.BodySr = p.Body.GetComponent<SpriteRenderer>();
                    p.Tell = SpriteFactory.CreateSprite("Tell" + k + "_" + i, null, _skins[k].tellSprite != null ? _skins[k].tellSprite : (k == 0 ? SpriteFactory.Box(Color.white) : SpriteFactory.Circle), _skins[k].tellTint, 21).transform;
                    p.TellSr = p.Tell.GetComponent<SpriteRenderer>();
                    p.Body.gameObject.SetActive(false); p.Tell.gameObject.SetActive(false);
                    _pool[n++] = p;
                }
            }
            for (int i = 0; i < _rings.Length; i++)
            {
                var r = new Ring(); r.Sr = SpriteFactory.CreateSprite("OverloadRing" + i, null, SpriteFactory.Circle, new Color(1f, 0.84f, 0.2f, 0.9f), 20); r.T = r.Sr.transform;
                r.T.localScale = new Vector3(1.6f, 1.6f, 1f); r.T.gameObject.SetActive(false); _rings[i] = r;
            }
            _boss = new GameObject("RO-B0").transform;
            _bossTrim = SpriteFactory.CreateSprite("Trim", _boss, SpriteFactory.Circle, new Color(0.3f, 0.6f, 1f), 24); _bossTrim.transform.localScale = new Vector3(2.2f, 1.7f, 1f);
            _bossBody = SpriteFactory.CreateSprite("Body", _boss, SpriteFactory.Circle, new Color(0.55f, 0.6f, 0.68f), 25); _bossBody.transform.localScale = new Vector3(1.9f, 1.4f, 1f);
            var eye = SpriteFactory.CreateSprite("Eye", _boss, SpriteFactory.Circle, Color.white, 26); eye.transform.localPosition = new Vector3(-0.35f, 0.1f, 0f); eye.transform.localScale = new Vector3(0.6f, 0.6f, 1f);
            var pupil = SpriteFactory.CreateSprite("Pupil", _boss, SpriteFactory.Circle, new Color(0.1f, 0.12f, 0.2f), 27); pupil.transform.localPosition = new Vector3(-0.45f, 0.1f, 0f); pupil.transform.localScale = new Vector3(0.28f, 0.28f, 1f);
            var ant = SpriteFactory.CreateSprite("Antenna", _boss, SpriteFactory.Circle, new Color(0.9f, 0.9f, 0.95f), 26); ant.transform.localPosition = new Vector3(0.2f, 0.95f, 0f); ant.transform.localScale = new Vector3(0.22f, 0.5f, 1f);
            _boss.gameObject.SetActive(false);
            _built = true;
        }

        // ------------------------------------------------------------------ tick
        public void Tick(float dt, Vector2 carPos, Vector2 carVel)
        {
            if (!_built || dt <= 0f) return;
            _clock += dt; CarPosCache = carPos; CarVelCache = carVel;
            var ctx = new BossContext
            {
                CarSpeed = carVel.x, CounterAvailable = CounterAvailable(), InkAvailable = InkAvailable(),
                RoadAheadForRoller = _road.FrontierMaxX - carPos.x >= carVel.x * 1.4f + carVel.x + 9.7f + 8f,
                Alive = AliveCount(), SinceBolt = _clock - _lastFire[0], SinceBomb = _clock - _lastFire[1], SinceRoller = _clock - _lastFire[2]
            };
            _model.Tick(dt, ctx);
            Hover(dt, carPos);
            for (int i = 0; i < _pool.Length; i++) { var p = _pool[i]; if (p.Active) UpdateProj(p, dt, carPos, carVel); }
            for (int i = 0; i < _rings.Length; i++) { var r = _rings[i]; if (r.Active) UpdateRing(r, dt, carPos); }
        }

        private int AliveCount() { int n = 0; for (int i = 0; i < _pool.Length; i++) if (_pool[i].Active && !_pool[i].Warning && !_pool[i].Dud) n++; return n; }

        private void Hover(float dt, Vector2 car)
        {
            if (!_boss.gameObject.activeSelf) return;
            bool telling = false;
            for (int i = 0; i < _pool.Length; i++) if (_pool[i].Active && _pool[i].Stage == 0) { telling = true; break; }
            _charge = Mathf.MoveTowards(_charge, telling ? 0.2f : 1f, 4f * dt);
            float ax = car.x + Mathf.Clamp(_d.hoverAhead, _d.hoverAheadMin, _d.hoverAheadMax);
            float baseY = _road.TryGetRoadYAtX(ax, out float ry) ? Mathf.Max(car.y, ry) : car.y;
            float ay = Mathf.Max(baseY + Mathf.Clamp(_d.hoverHeight, _d.hoverHeightMin, _d.hoverHeightMax), car.y + _d.minAboveRoof + 1f);
            if (_gone || _model.State == BossState.Retreat) { ax += 14f; ay += 8f; }                       // retreat: rises and leaves to the right
            float tx = ax + _d.hoverAx * _charge * Mathf.Sin(6.2832f * _d.hoverFx * _clock + _phx);
            float ty = ay + _d.hoverAy * _charge * Mathf.Sin(6.2832f * _d.hoverFy * _clock + _phy);
            _pos.x = Mathf.SmoothDamp(_pos.x, tx, ref _vel.x, _d.followSmoothTime);
            _pos.y = Mathf.SmoothDamp(_pos.y, ty, ref _vel.y, _d.followSmoothTime);
            if (_model.State == BossState.Defeat) _pos.y -= 1.5f * dt;                                      // the defeated robot sinks
            _boss.position = new Vector3(_pos.x, _pos.y, 0f);
            _bossTrim.color = _model.Phase <= 1 ? new Color(0.3f, 0.6f, 1f) : _model.Phase == 2 ? new Color(1f, 0.85f, 0.2f) : new Color(1f, 0.55f, 0.15f);   // blue, yellow, orange; red is for damage
            if (_gone && Mathf.Abs(_pos.x - ax) < 1f) _boss.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------ events from the model
        private void OnEvent(BossEvent e)
        {
            switch (e)
            {
                case BossEvent.IntroStart: Banner?.Invoke("RO-B0"); Cue?.Invoke("vo.rob0.s1.boss"); break;
                case BossEvent.RingDue: SpawnRing(); break;
                case BossEvent.PhaseTelegraph: Cue?.Invoke("vo.rob0.boss.phase"); Cue?.Invoke("sfx.boss.phaseFlare"); Shake?.Invoke(0.3f); Banner?.Invoke("PHASE " + _model.Phase); break;
                case BossEvent.Cleared: ClearAll(); break;
                case BossEvent.Defeated: Cue?.Invoke("vo.rob0.boss.defeat"); Shake?.Invoke(0.5f); Decided?.Invoke(false); break;
                case BossEvent.TimedOut: Cue?.Invoke("vo.rob0.boss.retreat"); Decided?.Invoke(true); break;
                case BossEvent.Gone: _gone = true; break;
            }
        }

        private Proj Free(ProjectileKind k)
        {
            int s = _poolStart[(int)k], c = _poolCount[(int)k];
            for (int i = 0; i < c; i++) if (!_pool[s + i].Active) return _pool[s + i];
            return null;
        }

        private void OnBeat(BeatRequest req)
        {
            var p = Free(req.Kind); if (p == null) return;
            var def = p.Def; var car = CarPosCache; var vel = CarVelCache;
            p.Warning = req.WarningOnly; p.Mode = req.Mode; p.Dud = false; p.Resolved = false; p.Stage = 0; p.T = 0f; p.MinDist = 999f; p.NoRoadFor = 0f; p.Locked = false; p.SpawnCarX = car.x;
            switch (req.Kind)
            {
                case ProjectileKind.Bolt:
                {
                    Vector2 muzzle = _pos;
                    if (Vector2.Distance(muzzle, car) < def.minMuzzleDistance) return;          // FR-2: never from inside the safe distance
                    p.Start = muzzle; p.Target = BoltTarget(p, muzzle, car, vel);
                    p.Dir = (p.Target - muzzle).normalized; p.Aim = Mathf.Atan2(p.Dir.y, p.Dir.x) * Mathf.Rad2Deg;
                    break;
                }
                case ProjectileKind.Bomb:
                {
                    float vx = Mathf.Max(0f, vel.x);
                    float xm = req.Mode == BeatMode.Direct ? car.x + vx * def.tellSeconds : car.x + vx * (def.tellSeconds + 0.6f) + 3.1f;
                    if (xm - car.x < def.spawnClearance) xm = car.x + def.spawnClearance;           // FR-2
                    if (!FindRoad(ref xm)) { p.Dud = true; xm = car.x + 8f; }
                    p.Target = new Vector2(xm, _road.TryGetRoadYAtX(xm, out float ry) ? ry : car.y - 0.5f);
                    break;
                }
                default:
                {
                    float v = Mathf.Max(0f, vel.x);
                    float d0 = Mathf.Max(20f, (v + 8f) * 1.2f + 2f);
                    float x = car.x + v * def.tellSeconds + d0;
                    x = Mathf.Min(x, _road.FrontierMaxX - 3f);
                    if (x - car.x - v * def.tellSeconds < v + 9.7f || !_road.HasRoadAt(x)) return;      // would give less than 1.0 s to react: skipped (harmless)
                    p.Target = new Vector2(x, _road.TryGetRoadYAtX(x, out float ry) ? ry : car.y);
                    break;
                }
            }
            p.Active = true; p.Life = def.tellSeconds;
            p.Tell.gameObject.SetActive(!p.Dud); p.Body.gameObject.SetActive(false);
            _lastFire[(int)req.Kind] = _clock;
            Cue?.Invoke(def.tellCueId);
        }

        // car state cache for OnBeat (Beat fires from inside model.Tick)
        private Vector2 CarPosCache, CarVelCache;

        private Vector2 BoltTarget(Proj p, Vector2 muzzle, Vector2 car, Vector2 vel)
        {
            if (p.Warning) { float x = car.x + 8f; return new Vector2(x, _road.TryGetRoadYAtX(x, out float ry) ? ry : car.y); }   // 8 m ahead of the car: no threat
            float flight = Mathf.Max(0.3f, Vector2.Distance(muzzle, car) / Mathf.Max(1f, p.Def.speed));
            return car + vel * flight;
        }

        private bool FindRoad(ref float x)
        {
            if (_road.HasRoadAt(x)) return true;
            for (float o = 1f; o <= 12f; o += 1f) { if (_road.HasRoadAt(x - o)) { x -= o; return true; } if (_road.HasRoadAt(x + o)) { x += o; return true; } }
            return false;
        }

        // ------------------------------------------------------------------ projectiles
        private void UpdateProj(Proj p, float dt, Vector2 car, Vector2 vel)
        {
            var def = p.Def; p.T += dt;
            switch (p.Kind)
            {
                case ProjectileKind.Bolt: UpdateBolt(p, def, dt, car, vel); break;
                case ProjectileKind.Bomb: UpdateBomb(p, def, dt, car); break;
                default: UpdateRoller(p, def, dt, car); break;
            }
        }

        private void UpdateBolt(Proj p, ProjectileDefinitionSO def, float dt, Vector2 car, Vector2 vel)
        {
            if (p.Stage == 0)
            {
                if (!p.Locked)
                {
                    Vector2 target = BoltTarget(p, _pos, car, vel);
                    float want = Mathf.Atan2(target.y - _pos.y, target.x - _pos.x) * Mathf.Rad2Deg;
                    float rate = def.aimTurnRate[Mathf.Clamp(_model.Phase, 1, def.aimTurnRate.Length) - 1];
                    p.Aim = Mathf.MoveTowardsAngle(p.Aim, want, rate * dt);                      // aim tracker turns slower than a player can out-turn (FR-6)
                    p.Dir = new Vector2(Mathf.Cos(p.Aim * Mathf.Deg2Rad), Mathf.Sin(p.Aim * Mathf.Deg2Rad));
                    if (p.T >= def.tellSeconds - def.lockLeadSeconds) p.Locked = true;
                }
                p.Start = _pos;
                float pulse = 0.5f + 0.5f * Mathf.Abs(Mathf.Sin(p.T * 14f));
                p.Tell.position = (Vector3)(p.Start + p.Dir * 9f); p.Tell.rotation = Quaternion.Euler(0f, 0f, p.Aim); p.Tell.localScale = new Vector3(18f, p.Locked ? 0.14f : 0.08f, 1f);
                var tc = _skins[0].tellTint; tc.a = p.Locked ? 0.9f : 0.35f + 0.4f * pulse; p.TellSr.color = tc;
                if (p.T >= def.tellSeconds)
                {
                    p.Stage = 1; p.Life = def.activeSeconds; p.Pos = p.Start; p.TravelM = car.x - p.SpawnCarX;
                    p.Tell.gameObject.SetActive(false); p.Body.gameObject.SetActive(true); p.Body.localScale = Vector3.one * _skins[0].scale;
                    Cue?.Invoke(def.fireSfxId);
                }
                return;
            }
            p.Pos += p.Dir * def.speed * dt; p.Life -= dt; p.Body.position = p.Pos; p.Body.Rotate(0f, 0f, 360f * dt);
            float d = Vector2.Distance(p.Pos, car);
            if (d < p.MinDist) p.MinDist = d;
            if (!p.Warning && d <= def.HitCentre && CanDamage()) { Strike(p, "CORN SHOT!"); return; }
            bool passed = Vector2.Dot(p.Dir, car - p.Pos) < 0f;
            bool grounded = _road.TryGetRoadYAtX(p.Pos.x, out float ry) && p.Pos.y <= ry;
            if (passed || p.Life <= 0f || grounded) Finish(p);
        }

        private void UpdateBomb(Proj p, ProjectileDefinitionSO def, float dt, Vector2 car)
        {
            if (p.Stage == 0)
            {
                if (!p.Dud)
                {
                    float pulse = 0.65f + 0.35f * Mathf.Sin(p.T * 12f);
                    p.Tell.position = new Vector3(p.Target.x, p.Target.y + 0.1f, 0f); p.Tell.localScale = new Vector3(def.hitRadius * 2f, 0.7f, 1f);
                    var tc = _skins[1].tellTint; tc.a = 0.35f + 0.45f * pulse * (p.T / def.tellSeconds + 0.3f); p.TellSr.color = tc;
                    if (p.T >= def.launchDelay)
                    {
                        if (!p.Body.gameObject.activeSelf) { p.Start = _pos; p.Body.gameObject.SetActive(true); p.Body.localScale = Vector3.one * _skins[1].scale; Cue?.Invoke(def.fireSfxId); }
                        float k = Mathf.Clamp01((p.T - def.launchDelay) / def.flightSeconds);
                        p.Body.position = (Vector3)Vector2.Lerp(p.Start, p.Target, k) + Vector3.up * (4f * k * (1f - k) * 4f);
                    }
                }
                if (p.T >= def.tellSeconds)
                {
                    p.Stage = 2; p.Life = def.activeSeconds; p.TravelM = car.x - p.SpawnCarX;
                    p.Body.gameObject.SetActive(false);
                    if (ParticleManager.Instance) ParticleManager.Instance.SpawnBurst(p.Target + Vector2.up * 0.3f, new Color(0.95f, 0.5f, 0.1f), p.Dud ? 6 : 20);
                    if (!p.Dud) { Shake?.Invoke(0.25f); p.TellSr.color = new Color(1f, 0.9f, 0.6f, 0.8f); }
                }
                return;
            }
            p.Life -= dt;
            if (!p.Dud)
            {
                float d = Vector2.Distance(car, p.Target);
                if (d < p.MinDist) p.MinDist = d;
                if (d <= def.HitCentre && car.y - p.Target.y <= def.blastHeight + 0.2f && CanDamage()) { Strike(p, "PUMPKIN BOMB!"); return; }
            }
            if (p.Life <= 0f) Finish(p);
        }

        private void UpdateRoller(Proj p, ProjectileDefinitionSO def, float dt, Vector2 car)
        {
            if (p.Stage == 0)
            {
                float pulse = 0.5f + 0.5f * Mathf.Abs(Mathf.Sin(p.T * 16f));
                p.Tell.position = new Vector3(p.Target.x, p.Target.y + 0.25f, 0f); p.Tell.localScale = new Vector3(1.6f, 0.5f, 1f);
                var tc = _skins[2].tellTint; tc.a = 0.4f + 0.5f * pulse; p.TellSr.color = tc;
                if (p.T >= def.tellSeconds)
                {
                    p.Stage = 1; p.Life = def.activeSeconds; p.Pos = p.Target; p.TravelM = car.x - p.SpawnCarX;
                    p.Tell.gameObject.SetActive(false); p.Body.gameObject.SetActive(true); p.Body.localScale = Vector3.one * _skins[2].scale;
                    Cue?.Invoke(def.fireSfxId);
                }
                return;
            }
            p.Pos.x -= def.speed * dt; p.Life -= dt;
            if (_road.TryGetRoadYAtX(p.Pos.x, out float ry)) { p.Pos.y = ry + def.hitRadius; p.NoRoadFor = 0f; }
            else { p.NoRoadFor += dt; p.Pos.y -= 9f * dt; if (p.NoRoadFor > 0.12f) { Finish(p, false); return; } }          // a road gap destroys it
            p.Body.position = p.Pos; p.Body.Rotate(0f, 0f, 400f * dt);
            float d = Vector2.Distance(p.Pos, car);
            if (d < p.MinDist) p.MinDist = d;
            if (d <= def.HitCentre && CanDamage()) { Strike(p, "HAY ROLLER!"); return; }
            if (p.Pos.x < car.x - 1.5f || p.Life <= 0f) Finish(p);
        }

        private void Strike(Proj p, string reason)
        {
            bool shielded = Hit(reason);
            if (ParticleManager.Instance) ParticleManager.Instance.SpawnBurst(p.Kind == ProjectileKind.Bomb ? p.Target : p.Pos, new Color(1f, 0.4f, 0.2f), 14);
            Deactivate(p);
            _model.Resolve(shielded ? BossOutcome.HitShielded : BossOutcome.Hit);
        }

        /// <summary>The projectile resolved without hitting: dodge, near miss or perfect near miss (warning shots and duds count for nothing).</summary>
        private void Finish(Proj p, bool counts = true)
        {
            var def = p.Def; float md = p.MinDist; bool real = !p.Warning && !p.Dud && counts && md < 900f;
            float travel = p.TravelM;
            Deactivate(p);
            if (!real) return;
            if (md <= def.PerfectEdge) { _model.Resolve(BossOutcome.Perfect); NearMissed?.Invoke(def.NearMissSource, true, travel); }
            else if (md <= def.nearMissRadius) { _model.Resolve(BossOutcome.Near); NearMissed?.Invoke(def.NearMissSource, false, travel); }
            else _model.Resolve(BossOutcome.Dodged);
        }

        private void Deactivate(Proj p) { p.Active = false; p.Body.gameObject.SetActive(false); p.Tell.gameObject.SetActive(false); }

        private void ClearAll()
        {
            if (_pool == null) return;
            for (int i = 0; i < _pool.Length; i++) if (_pool[i] != null && _pool[i].Body != null) Deactivate(_pool[i]);
            for (int i = 0; i < _rings.Length; i++) if (_rings[i] != null) { _rings[i].Active = false; _rings[i].T.gameObject.SetActive(false); }
        }

        // ------------------------------------------------------------------ rings
        private void SpawnRing()
        {
            Ring r = null; for (int i = 0; i < _rings.Length; i++) if (!_rings[i].Active) { r = _rings[i]; break; }
            if (r == null) return;
            float x = CarPosCache.x + Mathf.Lerp(4f, 7f, _rng.NextFloat01());
            float baseY = _road.TryGetRoadYAtX(x, out float ry) ? ry : CarPosCache.y;
            r.Pos = new Vector2(x, baseY + Mathf.Lerp(0.8f, 3f, _rng.NextFloat01())); r.Age = 0f; r.Active = true;
            r.T.position = r.Pos; r.T.gameObject.SetActive(true);
            Cue?.Invoke("vo.rob0.boss.ring");
        }

        private void UpdateRing(Ring r, float dt, Vector2 car)
        {
            r.Age += dt;
            r.T.localScale = Vector3.one * (1.6f + 0.15f * Mathf.Sin(r.Age * 8f));
            if (Vector2.Distance(car, r.Pos) <= 1.5f)
            {
                r.Active = false; r.T.gameObject.SetActive(false);
                if (ParticleManager.Instance) ParticleManager.Instance.SpawnBurst(r.Pos, new Color(1f, 0.85f, 0.3f), 16);
                _model.RingPassed(); RingTaken?.Invoke();
            }
            else if (car.x > r.Pos.x + 4f || r.Age > 8f) { r.Active = false; r.T.gameObject.SetActive(false); }
        }

        public void Destroy()
        {
            if (!_built) return;
            for (int i = 0; i < _pool.Length; i++) { UnityEngine.Object.Destroy(_pool[i].Body.gameObject); UnityEngine.Object.Destroy(_pool[i].Tell.gameObject); }
            for (int i = 0; i < _rings.Length; i++) UnityEngine.Object.Destroy(_rings[i].T.gameObject);
            UnityEngine.Object.Destroy(_boss.gameObject); _built = false;
        }
    }
}
