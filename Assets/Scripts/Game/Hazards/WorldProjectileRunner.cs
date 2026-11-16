using System;
using UnityEngine;

namespace RoadBuilderGame
{
    /// <summary>
    /// Scene side of the world projectile (B2): one definition per run (chosen by world id), a small fixed pool, the six flight patterns (arc, straight, splitting, homing,
    /// bouncing, zigzag), telegraph visuals and the single persistent surface effect, which is reported to <see cref="ProjectileSurfaceHooks"/> (section C).
    /// Allocation happens in <see cref="Build"/> only. Outcomes go back through delegates so the boss model keeps owning the rules.
    /// </summary>
    public sealed class WorldProjectileRunner
    {
        private const int PoolSize = 6, GroupCount = 4;

        private sealed class P
        {
            public bool Active, Warning, Dud; public int Stage, Group, Bounces; public bool Locked, Child, Split;
            public float T, Life, Seed, Aim, TurnLeft, TravelM;
            public Vector2 Pos, Vel, Start, Target, Dir;
            public Transform Body, Tell; public SpriteRenderer BodySr, TellSr;
        }

        private readonly RoadBuilder _road;
        private readonly P[] _pool = new P[PoolSize];
        private readonly float[] _gMin = new float[GroupCount], _gTravel = new float[GroupCount];
        private readonly int[] _gAlive = new int[GroupCount];
        private readonly bool[] _gDone = new bool[GroupCount];
        private ProjectileDefinitionSO _d;
        private Transform _patch; private SpriteRenderer _patchSr; private float _patchLeft; private bool _patchLive;
        private bool _built; private float _clock;

        public ProjectileDefinitionSO Definition => _d;
        public bool Has => _d != null && _d.IsWorld;

        public Func<bool> CanDamage = () => true;
        /// <summary>The car was struck; returns true when a shield absorbed it.</summary>
        public Func<string, bool> Hit = r => false;
        /// <summary>Group resolved without a hit: minimum distance, metres travelled during the tell.</summary>
        public Action<float, float> Passed;
        public Action<bool> Struck;
        public Action<string> Cue;
        public Action<float> Shake;
        /// <summary>A projectile left the cannon (drives the robot's recoil and pose).</summary>
        public Action Fired;
        /// <summary>Trail puff request for RO-B0 art families: slot, position, family (1 rock, 2 bolt, 3 bubble).</summary>
        public Action<int, Vector2, int> Trail;
        private int _family;
        public int AliveCount { get { int n = 0; for (int i = 0; i < PoolSize; i++) if (_pool[i] != null && _pool[i].Active && !_pool[i].Warning && !_pool[i].Dud) n++; return n; } }
        public bool PatchLive => _patchLive;
        /// <summary>True while any projectile is in its tell (the robot shows its angry pose).</summary>
        public bool Telling { get { for (int i = 0; i < PoolSize; i++) { var p = _pool[i]; if (p != null && p.Active && p.Stage == 0) return true; } return false; } }

        public WorldProjectileRunner(RoadBuilder road) { _road = road; }

        public void Configure(ProjectileDefinitionSO def)
        {
            if (_d == def) return;
            ClearAll(); _d = def;
            if (_built) Skin();
        }

        private static Sprite _orb;
        /// <summary>Glossy orb (shaded rim, soft highlight, antialiased edge) for world projectiles with no own art; the projectile tint multiplies it. Built once.</summary>
        private static Sprite Orb()
        {
            if (_orb != null) return _orb;
            const int n = 96; var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, name = "OrbSoft" };
            var px = new Color32[n * n]; float r = n * 0.5f - 1.5f;
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            {
                float dx = (x - (n - 1) * 0.5f) / r, dy = (y - (n - 1) * 0.5f) / r, d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01((1f - d) * r * 0.5f + 0.5f);
                float shade = Mathf.Lerp(1f, 0.62f, Mathf.Pow(Mathf.Clamp01(d), 2.2f));                       // darker toward the rim
                float hx = dx + 0.35f, hy = dy - 0.38f; float hi = Mathf.Clamp01(1f - Mathf.Sqrt(hx * hx + hy * hy) * 2.6f);   // soft highlight, upper left
                float c = Mathf.Clamp01(shade + hi * 0.45f);
                px[y * n + x] = new Color32((byte)(c * 255f), (byte)(c * 255f), (byte)(c * 255f), (byte)(a * 255f));
            }
            tex.SetPixels32(px); tex.Apply(false, true);
            return _orb = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);                 // 1 unit across, like SpriteFactory.Circle
        }

        private void Skin()
        {
            var art = HazardArtSet.Load(); _family = _d != null ? RoB0Data.FamilyForWorldProjectile(_d.id) : 0;
            Sprite rob = art == null ? null : _family == 1 ? art.rob0Rock : _family == 2 ? art.rob0Bolt : _family == 3 ? art.rob0Bubble : null;
            if (rob == null) _family = 0;
            for (int i = 0; i < PoolSize; i++)
            {
                var p = _pool[i];
                p.BodySr.sprite = rob != null ? rob : _d != null && _d.sprite != null ? _d.sprite : Orb(); p.BodySr.color = rob != null || (_d != null && _d.sprite != null) ? Color.white : (_d != null ? _d.tint : Color.white);
                p.TellSr.sprite = _d != null && _d.tellSprite != null ? _d.tellSprite : (_d != null && _d.telegraph == TelegraphShape.Line ? SpriteFactory.Box(Color.white) : SpriteFactory.Circle);
            }
        }

        private void Build()
        {
            if (_built) return;
            for (int i = 0; i < PoolSize; i++)
            {
                var p = new P();
                p.BodySr = SpriteFactory.CreateSprite("WPrj" + i, null, SpriteFactory.Circle, Color.white, 22); p.Body = p.BodySr.transform;
                p.TellSr = SpriteFactory.CreateSprite("WTell" + i, null, SpriteFactory.Circle, Color.red, 21); p.Tell = p.TellSr.transform;
                p.Body.gameObject.SetActive(false); p.Tell.gameObject.SetActive(false); _pool[i] = p;
            }
            _patchSr = SpriteFactory.CreateSprite("WPatch", null, SpriteFactory.Circle, Color.white, 12); _patch = _patchSr.transform; _patch.gameObject.SetActive(false);
            _built = true; Skin();
        }

        public void Begin() { Build(); _clock = 0f; ClearAll(); }
        public void ClearAll()
        {
            if (!_built) return;
            for (int i = 0; i < PoolSize; i++) Off(_pool[i]);
            for (int i = 0; i < GroupCount; i++) { _gAlive[i] = 0; _gDone[i] = true; }
            EndPatch();
        }

        private static void Off(P p) { p.Active = false; p.Body.gameObject.SetActive(false); p.Tell.gameObject.SetActive(false); }

        private P Free() { for (int i = 0; i < PoolSize; i++) if (!_pool[i].Active) return _pool[i]; return null; }
        private int FreeGroup() { for (int i = 0; i < GroupCount; i++) if (_gAlive[i] <= 0) return i; return -1; }

        // ------------------------------------------------------------------ fire
        /// <summary>Start one projectile from the muzzle. Returns false when it cannot start (pool full or the muzzle is inside the safe distance), harmless in every case.</summary>
        public bool Fire(Vector2 muzzle, Vector2 car, Vector2 vel, bool warningOnly, uint seed)
        {
            if (!Has) return false; Build();
            var d = _d; var p = Free(); int g = FreeGroup(); if (p == null || g < 0) return false;
            bool ground = d.pattern == ProjectilePattern.Arc || d.pattern == ProjectilePattern.Bouncing;
            if (!ground && Vector2.Distance(muzzle, car) < d.minMuzzleDistance) return false;            // FR-2: never from inside the safe distance
            p.Warning = warningOnly; p.Dud = false; p.Stage = 0; p.T = 0f; p.Group = g; p.Bounces = 0; p.Locked = false; p.Child = false; p.Split = false; p.TravelM = car.x;
            p.Seed = (seed & 0xFFFF) * 0.0001f; p.Start = muzzle; p.Pos = muzzle; p.TurnLeft = 0f;
            _gAlive[g] = 1; _gMin[g] = 999f; _gDone[g] = false; _gTravel[g] = 0f;
            float lead = Mathf.Max(0f, vel.x);
            if (ground)
            {
                float total = d.tellSeconds + (d.pattern == ProjectilePattern.Bouncing ? d.flightSeconds : 0f);
                float xm = car.x + lead * total + d.spawnClearance * 0.5f;
                if (warningOnly) xm = car.x + 8f;
                if (!FindRoad(ref xm)) { p.Dud = true; xm = car.x + 8f; }
                p.Target = new Vector2(xm, _road.TryGetRoadYAtX(xm, out float ry) ? ry : car.y - 0.5f);
            }
            else
            {
                Vector2 tgt = warningOnly ? new Vector2(car.x + 8f, car.y) : car + vel * Mathf.Max(0.3f, Vector2.Distance(muzzle, car) / Mathf.Max(1f, d.speed));
                p.Target = tgt; p.Dir = (tgt - muzzle).normalized; p.Aim = Mathf.Atan2(p.Dir.y, p.Dir.x) * Mathf.Rad2Deg;
            }
            p.Active = true; p.Life = d.tellSeconds;
            p.Tell.gameObject.SetActive(!p.Dud); p.Body.gameObject.SetActive(false);
            Cue?.Invoke(d.tellCueId);
            return true;
        }

        private bool FindRoad(ref float x)
        {
            if (_road.HasRoadAt(x)) return true;
            for (float o = 1f; o <= 12f; o += 1f) { if (_road.HasRoadAt(x - o)) { x -= o; return true; } if (_road.HasRoadAt(x + o)) { x += o; return true; } }
            return false;
        }

        // ------------------------------------------------------------------ tick
        public void Tick(float dt, Vector2 car, Vector2 vel, Vector2 muzzle, int phase)
        {
            if (!_built || dt <= 0f) return;
            _clock += dt;
            if (_patchLive) { _patchLeft -= dt; if (_patchLeft <= 0f) EndPatch(); else { var c = _patchSr.color; c.a = Mathf.Clamp01(_patchLeft * 2f) * 0.55f; _patchSr.color = c; } }
            for (int i = 0; i < PoolSize; i++) { var p = _pool[i]; if (p.Active) { Update(p, dt, car, vel, muzzle, phase); if (_family != 0 && p.Active && p.Stage >= 1 && p.Body.gameObject.activeSelf) { if (_family == 3) p.Body.rotation = Quaternion.identity; Trail?.Invoke(i, p.Body.position, _family); } } }
        }

        private void Update(P p, float dt, Vector2 car, Vector2 vel, Vector2 muzzle, int phase)
        {
            var d = _d; p.T += dt;
            if (p.Stage == 0) { Tell(p, d, dt, car, vel, muzzle, phase); return; }
            switch (d.pattern)
            {
                case ProjectilePattern.Arc: Arc(p, d, dt, car); break;
                case ProjectilePattern.Bouncing: Bounce(p, d, dt, car); break;
                default: Fly(p, d, dt, car, phase); break;
            }
        }

        private void Tell(P p, ProjectileDefinitionSO d, float dt, Vector2 car, Vector2 vel, Vector2 muzzle, int phase)
        {
            float k = Mathf.Clamp01(p.T / d.tellSeconds); float pulse = 0.5f + 0.5f * Mathf.Abs(Mathf.Sin(p.T * 14f));
            var tc = d.tint; tc.a = 0.35f + 0.5f * pulse; if (p.Locked) tc.a = 0.9f;
            var tr = p.Tell;
            bool ground = d.pattern == ProjectilePattern.Arc || d.pattern == ProjectilePattern.Bouncing;
            if (ground)
            {
                if (!p.Dud) { tr.position = new Vector3(p.Target.x, p.Target.y + 0.12f, 0f); tr.rotation = Quaternion.identity; tr.localScale = new Vector3(Mathf.Max(1.6f, d.surfaceRadius) * (0.7f + 0.3f * k), 0.45f, 1f); }
                if (d.pattern == ProjectilePattern.Arc && !p.Body.gameObject.activeSelf && p.T >= d.tellSeconds * 0.35f) { p.Body.gameObject.SetActive(true); p.Body.localScale = Vector3.one * d.scale; Cue?.Invoke(d.fireSfxId); Fired?.Invoke(); }
                float f = Mathf.Clamp01((p.T - d.tellSeconds * 0.35f) / Mathf.Max(0.1f, d.tellSeconds * 0.65f + d.flightSeconds * 0f));
                if (d.pattern == ProjectilePattern.Arc && p.Body.gameObject.activeSelf) { Vector2 pos = Vector2.Lerp(p.Start, p.Target, f); pos.y += d.arcHeight * 4f * f * (1f - f); p.Body.position = pos; p.Pos = pos; }
            }
            else
            {
                Vector2 want = BoltTarget(p, muzzle, car, vel, d);
                if (!p.Locked)
                {
                    float w = Mathf.Atan2(want.y - muzzle.y, want.x - muzzle.x) * Mathf.Rad2Deg;
                    p.Aim = Mathf.MoveTowardsAngle(p.Aim, w, (15f + 5f * (Mathf.Clamp(phase, 1, 3) - 1)) * dt);   // slower than a player can out-turn (FR-6)
                    p.Dir = new Vector2(Mathf.Cos(p.Aim * Mathf.Deg2Rad), Mathf.Sin(p.Aim * Mathf.Deg2Rad));
                    if (p.T >= d.tellSeconds - d.lockLeadSeconds) p.Locked = true;
                }
                p.Start = muzzle;
                if (d.telegraph == TelegraphShape.Ring) { tr.position = muzzle; tr.localScale = Vector3.one * (1.2f + 2.2f * (1f - k)); tr.rotation = Quaternion.identity; }
                else if (d.telegraph == TelegraphShape.Flash || d.telegraph == TelegraphShape.Glow) { tr.position = muzzle; tr.localScale = Vector3.one * (1.0f + 1.4f * pulse); tr.rotation = Quaternion.identity; }
                else if (d.telegraph == TelegraphShape.Marker) { tr.position = muzzle + p.Dir * 2.2f; tr.localScale = new Vector3(2.6f, 1.2f, 1f); tr.rotation = Quaternion.Euler(0f, 0f, p.Aim); }
                else { tr.position = (Vector3)(muzzle + p.Dir * 9f); tr.rotation = Quaternion.Euler(0f, 0f, p.Aim); tr.localScale = new Vector3(18f, p.Locked ? 0.14f : 0.08f, 1f); }
            }
            p.TellSr.color = tc;
            if (p.T < d.tellSeconds) return;
            // launch
            p.Stage = 1; p.Life = d.activeSeconds; p.TravelM = car.x - p.TravelM; p.Tell.gameObject.SetActive(false);
            if (!p.Body.gameObject.activeSelf) { p.Body.gameObject.SetActive(true); Cue?.Invoke(d.fireSfxId); Fired?.Invoke(); }
            p.Body.localScale = Vector3.one * d.scale;
            if (ground)
            {
                p.Start = p.Pos; p.T = 0f;
                if (d.pattern == ProjectilePattern.Bouncing)
                {
                    p.Start = muzzle; p.Pos = muzzle; p.Body.gameObject.SetActive(true); Cue?.Invoke(d.fireSfxId); Fired?.Invoke();
                    float fl = Mathf.Max(0.4f, d.flightSeconds);
                    p.Vel = new Vector2((p.Target.x - muzzle.x) / fl, (p.Target.y - muzzle.y + 7f * fl * fl) / fl);       // lands on the marker after flightSeconds (gravity 14)
                }
                else p.Pos = p.Target + Vector2.up * 0.1f;
            }
            else { p.Pos = p.Start; p.Vel = p.Dir * d.speed; p.TurnLeft = d.homingSeconds; p.T = 0f; }
        }

        private Vector2 BoltTarget(P p, Vector2 muzzle, Vector2 car, Vector2 vel, ProjectileDefinitionSO d)
        {
            if (p.Warning) return new Vector2(car.x + 8f, car.y);
            float flight = Mathf.Max(0.3f, Vector2.Distance(muzzle, car) / Mathf.Max(1f, d.speed));
            return car + vel * flight;
        }

        // arc: the tell already carried the body over; this is the landing blast
        private void Arc(P p, ProjectileDefinitionSO d, float dt, Vector2 car)
        {
            if (p.Stage == 1)
            {
                p.Stage = 2; p.Life = Mathf.Max(0.25f, d.flightSeconds * 0.25f); p.Body.position = p.Target; p.Pos = p.Target;
                if (!p.Dud) { Shake?.Invoke(0.25f); Impact(p, d, p.Target); }
                if (ParticleManager.Instance) ParticleManager.Instance.SpawnBurst(p.Target + Vector2.up * 0.3f, d.tint, p.Dud ? 6 : 18);
                p.Body.gameObject.SetActive(false);
            }
            p.Life -= dt;
            if (!p.Dud && p.Stage == 2)
            {
                float dist = Vector2.Distance(car, p.Target); if (dist < _gMin[p.Group]) _gMin[p.Group] = dist;
                if (dist <= d.HitCentre + 0.9f && car.y - p.Target.y <= d.blastHeight + 0.2f && CanDamage() && Strike(p, d.displayName.ToUpperInvariant() + "!")) return;
            }
            if (p.Life <= 0f) Resolve(p, true);
        }

        private void Bounce(P p, ProjectileDefinitionSO d, float dt, Vector2 car)
        {
            if (p.Dud) { Impact(p, d, p.Target); Resolve(p, false); return; }
            p.Vel.y -= 14f * dt; p.Pos += p.Vel * dt; p.Life -= dt; p.Body.position = p.Pos; p.Body.Rotate(0f, 0f, 300f * dt);
            if (_road.TryGetRoadYAtX(p.Pos.x, out float ry) && p.Pos.y <= ry + d.hitRadius && p.Vel.y < 0f)
            {
                p.Pos.y = ry + d.hitRadius; p.Vel.y = Mathf.Abs(p.Vel.y) * d.bounceDamping + 1.5f; p.Bounces++;
                if (ParticleManager.Instance) ParticleManager.Instance.SpawnBurst(p.Pos, d.tint, 5);
                if (p.Bounces >= d.maxBounces) { Impact(p, d, new Vector2(p.Pos.x, ry)); Resolve(p, true); return; }
            }
            Measure(p, d, car, d.displayName.ToUpperInvariant() + "!");
            if (p.Pos.x < car.x - 4f || p.Life <= 0f) Resolve(p, true);
        }

        private void Fly(P p, ProjectileDefinitionSO d, float dt, Vector2 car, int phase)
        {
            Vector2 dir = p.Vel.normalized; p.Life -= dt;
            switch (d.pattern)
            {
                case ProjectilePattern.Homing:
                    if (p.TurnLeft > 0f && !p.Warning)
                    {
                        p.TurnLeft -= dt;
                        float want = Mathf.Atan2(car.y - p.Pos.y, car.x - p.Pos.x) * Mathf.Rad2Deg;
                        float cur = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                        cur = Mathf.MoveTowardsAngle(cur, want, Mathf.Min(d.homingTurnRate, 35f) * dt);     // capped: always evadable
                        dir = new Vector2(Mathf.Cos(cur * Mathf.Deg2Rad), Mathf.Sin(cur * Mathf.Deg2Rad)); p.Vel = dir * d.speed;
                    }
                    p.Pos += p.Vel * dt; break;
                case ProjectilePattern.Zigzag:
                {
                    Vector2 n = new Vector2(-dir.y, dir.x);
                    float before = Mathf.Sin(6.2832f * d.zigzagHz * (p.T - dt) + p.Seed); float now = Mathf.Sin(6.2832f * d.zigzagHz * p.T + p.Seed);
                    p.Pos += p.Vel * dt + n * (now - before) * d.zigzagAmplitude; break;
                }
                case ProjectilePattern.Splitting:
                    p.Pos += p.Vel * dt;
                    if (!p.Child && !p.Split && p.T >= d.splitAfterSeconds) { Split(p, d); return; }
                    break;
                default: p.Pos += p.Vel * dt; break;
            }
            p.Body.position = p.Pos; p.Body.Rotate(0f, 0f, 360f * dt);
            if (Measure(p, d, car, d.displayName.ToUpperInvariant() + "!")) return;
            bool passed = Vector2.Dot(p.Vel, car - p.Pos) < 0f && Vector2.Distance(p.Pos, car) > 2.5f;
            bool grounded = _road.TryGetRoadYAtX(p.Pos.x, out float ry) && p.Pos.y <= ry;
            if (grounded) Impact(p, d, new Vector2(p.Pos.x, ry));
            if (passed || p.Life <= 0f || grounded) Resolve(p, true);
        }

        private void Split(P p, ProjectileDefinitionSO d)
        {
            p.Split = true; int n = Mathf.Clamp(d.splitCount, 2, 3); float baseA = Mathf.Atan2(p.Vel.y, p.Vel.x) * Mathf.Rad2Deg;
            for (int i = 0; i < n; i++)
            {
                P c = i == 0 ? p : Free(); if (c == null) break;
                float a = baseA + (i - (n - 1) * 0.5f) * d.splitSpreadDeg;
                if (i != 0) { c.Active = true; c.Warning = p.Warning; c.Dud = false; c.Stage = 1; c.Group = p.Group; c.Pos = p.Pos; c.T = p.T; c.Life = p.Life; c.Child = true; c.Locked = true; c.Bounces = 0; c.TravelM = p.TravelM; _gAlive[p.Group]++; c.Body.gameObject.SetActive(true); }
                c.Child = true; c.Vel = new Vector2(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad)) * d.speed; c.Body.localScale = Vector3.one * d.scale * 0.8f;
            }
            if (ParticleManager.Instance) ParticleManager.Instance.SpawnBurst(p.Pos, d.tint, 8);
            Cue?.Invoke(d.fireSfxId);
        }

        private bool Measure(P p, ProjectileDefinitionSO d, Vector2 car, string reason)
        {
            float dist = Vector2.Distance(p.Pos, car); if (dist < _gMin[p.Group]) _gMin[p.Group] = dist;
            return !p.Warning && dist <= d.HitCentre && CanDamage() && Strike(p, reason);
        }

        private bool Strike(P p, string reason)
        {
            bool shielded = Hit(reason);
            if (ParticleManager.Instance) ParticleManager.Instance.SpawnBurst(p.Pos, new Color(1f, 0.4f, 0.2f), 14);
            _gDone[p.Group] = true; _gAlive[p.Group] = 0;      // the group is decided: its other members vanish with the model's Cleared event
            Off(p); Struck?.Invoke(shielded); return true;
        }

        private void Resolve(P p, bool counts)
        {
            int g = p.Group; bool wasWarn = p.Warning || p.Dud; float travel = p.TravelM; Off(p);
            if (_gDone[g]) return;
            if (--_gAlive[g] > 0) return;
            _gDone[g] = true;
            if (wasWarn || !counts || _gMin[g] > 900f) return;
            Passed?.Invoke(_gMin[g], travel);
        }

        // ------------------------------------------------------------------ persistent surface effect (one at a time)
        private void Impact(P p, ProjectileDefinitionSO d, Vector2 at)
        {
            if (p.Warning || p.Dud || d.surfaceEffect == ProjectileSurfaceEffect.None) return;
            if (_patchLive) return;                           // at most one persistent hazard per projectile
            if (d.surfaceSeconds <= 0f) { ProjectileSurfaceHooks.Current.OnProjectileSurface(d.surfaceEffect, at, d.surfaceRadius, 0f, d.id); return; }
            _patchLive = true; _patchLeft = d.surfaceSeconds;
            _patch.position = new Vector3(at.x, at.y + 0.05f, 0f); _patch.localScale = new Vector3(d.surfaceRadius * 2f, 0.3f, 1f);
            var c = d.tint; c.a = 0.55f; _patchSr.color = c; _patch.gameObject.SetActive(true);
            ProjectileSurfaceHooks.Current.OnProjectileSurface(d.surfaceEffect, at, d.surfaceRadius, d.surfaceSeconds, d.id);
        }

        private void EndPatch()
        {
            if (!_patchLive) return;
            _patchLive = false; if (_patch != null) _patch.gameObject.SetActive(false);
            if (_d != null) ProjectileSurfaceHooks.Current.OnProjectileSurfaceCleared(_d.surfaceEffect);
        }

        public void Destroy()
        {
            if (!_built) return;
            for (int i = 0; i < PoolSize; i++) { UnityEngine.Object.Destroy(_pool[i].Body.gameObject); UnityEngine.Object.Destroy(_pool[i].Tell.gameObject); }
            UnityEngine.Object.Destroy(_patch.gameObject); _built = false;
        }
    }
}
