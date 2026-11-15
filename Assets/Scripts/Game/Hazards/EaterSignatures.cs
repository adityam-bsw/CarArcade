using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoadBuilderGame
{
    /// <summary>What the Road-Eater modules may read and do. The controller fills the inputs each tick; every effect on the car or the run goes out through a delegate, so modules hold no manager reference.</summary>
    public sealed class EaterHost
    {
        public RoadBuilder Road;
        public Vector2 CarPos, CarVel, MawPos; public bool Grounded = true, Boosting; public float CarMass = 1.5f, EaterX, Gap = 30f, ChaseStartX, Clock; public int Phase = 1;
        public Func<string, bool> Damage = r => false;      // heart loss; true when a shield absorbed it
        public Func<bool> CanDamage = () => true;
        public Action<Vector2> CarForce = f => { };
        public Action<float> Shake = a => { }; public Action<string> Cue = id => { }; public Action<string> Banner = t => { };
        // outputs the modules write
        public bool HideMaw, UseMawOverride; public Vector2 MawOverride; public float ForceEatX = float.NegativeInfinity;
        public bool Eating = true;
    }

    /// <summary>One signature attack: Rest -> Tell (1.2 s or more, visible marker) -> Active. Data comes from <see cref="EaterVariantSO"/>; subclasses only say what the tell and the active part do.</summary>
    public abstract class EaterSignatureModule
    {
        protected enum Ph { Rest, Tell, Active }
        protected Ph State; protected float T, RestLeft;
        protected EaterVariantSO V; protected EaterHost H; protected Pcg32 Rng;
        private readonly List<SpriteRenderer> _vis = new List<SpriteRenderer>();
        protected HazardArtSet Art;

        public void Begin(EaterVariantSO v, EaterHost h, uint seed) { V = v; H = h; Rng = new Pcg32(seed == 0u ? 7u : seed); State = Ph.Rest; T = 0f; RestLeft = v.firstAfterSeconds; Art = HazardArtSet.Load(); OnBegin(); }

        public void Stop() { State = Ph.Rest; for (int i = 0; i < _vis.Count; i++) if (_vis[i] != null) _vis[i].gameObject.SetActive(false); H.HideMaw = false; H.UseMawOverride = false; OnStop(); }
        public void Destroy() { for (int i = 0; i < _vis.Count; i++) if (_vis[i] != null) UnityEngine.Object.Destroy(_vis[i].gameObject); _vis.Clear(); }

        /// <param name="canStart">false while the Eater is frozen, snapping or not chasing: no attack starts then (fairness).</param>
        public void Tick(float dt, bool canStart)
        {
            switch (State)
            {
                case Ph.Rest:
                    if (!canStart) return;
                    RestLeft -= dt * (1f + 0.25f * (H.Phase - 1));        // later pursuit phases attack more often
                    if (RestLeft <= 0f) { State = Ph.Tell; T = 0f; if (!StartTell()) { State = Ph.Rest; RestLeft = 2f; } else H.Cue("sfx.eater.chew"); }
                    return;
                case Ph.Tell:
                    T += dt; TickTell(dt);
                    if (T >= V.tellSeconds) { State = Ph.Active; T = 0f; StartActive(); }
                    return;
                default:
                    T += dt;
                    if (TickActive(dt) || T >= V.activeSeconds) { EndActive(); State = Ph.Rest; RestLeft = V.everySeconds; H.HideMaw = false; H.UseMawOverride = false; }
                    return;
            }
        }

        protected virtual void OnBegin() { }
        protected virtual void OnStop() { }
        protected abstract bool StartTell();
        protected virtual void TickTell(float dt) { }
        protected virtual void StartActive() { }
        /// <summary>Return true to end early.</summary>
        protected virtual bool TickActive(float dt) { return false; }
        protected virtual void EndActive() { HideAll(); }

        // ---- helpers
        protected SpriteRenderer Vis(int i, Sprite sprite, Color c, int order)
        {
            while (_vis.Count <= i) _vis.Add(null);
            if (_vis[i] == null) { _vis[i] = SpriteFactory.CreateSprite("EaterSig" + i, null, sprite != null ? sprite : SpriteFactory.Circle, c, order); _vis[i].gameObject.SetActive(false); }
            return _vis[i];
        }
        protected void Show(int i, Vector2 at, Vector2 scale, Color c, Sprite sprite = null, float rotation = 0f, int order = 14)
        {
            var sr = Vis(i, sprite, c, order); sr.sprite = sprite != null ? sprite : sr.sprite; sr.color = c; var t = sr.transform; t.position = new Vector3(at.x, at.y, 0f); t.localScale = new Vector3(scale.x, scale.y, 1f); t.rotation = Quaternion.Euler(0f, 0f, rotation);
            if (!sr.gameObject.activeSelf) sr.gameObject.SetActive(true);
        }
        protected void HideAll() { for (int i = 0; i < _vis.Count; i++) if (_vis[i] != null) _vis[i].gameObject.SetActive(false); }
        protected void Hide(int i) { if (i < _vis.Count && _vis[i] != null) _vis[i].gameObject.SetActive(false); }
        protected Color Tint(float a) { var c = V.tint; c.a = a; return c; }
        protected float Pulse(float rate) { return 0.5f + 0.5f * Mathf.Abs(Mathf.Sin(T * rate)); }

        /// <summary>A road point where the car will be after the tell and half the active time (aimed, so braking, boosting or jumping is the counter).</summary>
        protected bool AimPoint(float extra, out Vector2 p)
        {
            float x = H.CarPos.x + Mathf.Max(0f, H.CarVel.x) * (V.tellSeconds + V.activeSeconds * 0.5f) + extra + 2.6f;
            if (!H.Road.HasRoadAt(x)) { bool ok = false; for (float o = 1f; o <= 10f && !ok; o += 1f) { if (H.Road.HasRoadAt(x - o)) { x -= o; ok = true; } else if (H.Road.HasRoadAt(x + o)) { x += o; ok = true; } } if (!ok) { p = default; return false; } }
            H.Road.TryGetRoadYAtX(x, out float y); p = new Vector2(x, y); return true;
        }
        protected bool InZone(Vector2 c, float r) { return Mathf.Abs(H.CarPos.x - c.x) <= r + 0.45f && H.CarPos.y - c.y <= 1.7f && H.CarPos.y - c.y >= -1f; }
        protected void Hurt(string reason) { if (H.CanDamage()) { H.Damage(reason); H.Shake(0.3f); } }
    }

    /// <summary>Damage zones at aimed road points (dirt maw emerge, leech landing, burning ground, stretch and snap, ceiling rocks, shadow bite).</summary>
    public sealed class ZoneSignature : EaterSignatureModule
    {
        private readonly string _reason; private readonly float[] _offsets; private readonly bool _hide, _jump, _stretch, _drop, _burn, _persistent;
        private readonly Vector2[] _pts; private readonly bool[] _hit; private Vector2 _from;

        public ZoneSignature(string reason, float[] offsets, bool hideMaw, bool jump, bool stretch, bool drop, bool burn, bool persistent)
        { _reason = reason; _offsets = offsets; _hide = hideMaw; _jump = jump; _stretch = stretch; _drop = drop; _burn = burn; _persistent = persistent; _pts = new Vector2[offsets.Length]; _hit = new bool[offsets.Length]; }

        protected override bool StartTell()
        {
            for (int i = 0; i < _offsets.Length; i++) { if (!AimPoint(_offsets[i], out _pts[i])) return false; _hit[i] = false; }
            _from = H.MawPos; if (_hide) H.HideMaw = true; return true;
        }

        protected override void TickTell(float dt)
        {
            float k = Mathf.Clamp01(T / V.tellSeconds);
            for (int i = 0; i < _pts.Length; i++)
            {
                Show(i, _pts[i] + Vector2.up * 0.1f, new Vector2(V.radius * 2f * (0.6f + 0.4f * k), 0.45f), Tint(0.3f + 0.5f * Pulse(12f)), Art != null ? Art.tellMarker : null, 0f, 13);
                if (_drop) Show(8 + i, new Vector2(_pts[i].x, _pts[i].y + 9f * (1f - k) + 0.6f), new Vector2(1.2f, 1.2f), Tint(0.95f), null, 0f, 15);       // the rock falls during the tell, lands at the end
            }
            if (_stretch) { Vector2 tip = Vector2.Lerp(_from, _pts[0], k); Vector2 mid = (_from + tip) * 0.5f; Show(4, mid, new Vector2(Vector2.Distance(_from, tip), 0.5f), Tint(0.8f), SpriteFactory.Box(Color.white), Mathf.Atan2(tip.y - _from.y, tip.x - _from.x) * Mathf.Rad2Deg, 14); }
        }

        protected override void StartActive()
        {
            H.Shake(0.25f); H.Cue("sfx.eater.jumpChew");
            if (_burn) ProjectileSurfaceHooks.Current.OnProjectileSurface(ProjectileSurfaceEffect.BurnWood, _pts[0], V.radius, V.activeSeconds, V.id);
            for (int i = 0; i < _pts.Length; i++) if (ParticleManager.Instance) ParticleManager.Instance.SpawnBurst(_pts[i] + Vector2.up * 0.3f, V.tint, 12);
        }

        protected override bool TickActive(float dt)
        {
            float k = Mathf.Clamp01(T / V.activeSeconds);
            if (_hide) { H.HideMaw = false; H.UseMawOverride = true; H.MawOverride = _pts[0] + Vector2.up * 1.3f; }       // burrow and shadow: it appears at the bite
            if (_jump) { H.UseMawOverride = true; H.MawOverride = Vector2.Lerp(_from, _pts[0], k) + Vector2.up * (4f * k * (1f - k) * 5f); }
            for (int i = 0; i < _pts.Length; i++)
            {
                Show(i, _pts[i] + Vector2.up * 0.2f, new Vector2(V.radius * 2f, _persistent ? 0.5f : 0.9f), Tint(0.9f), Art != null ? Art.tellMarker : null, 0f, 13);
                if (!_hit[i] && InZone(_pts[i], V.radius)) { _hit[i] = true; Hurt(_reason); if (!_persistent) return true; }
            }
            return false;
        }
    }

    /// <summary>Pulls the car back toward the Eater for a while (iron worm on metal, abyss eel current). A boost resists most of it.</summary>
    public sealed class PullSignature : EaterSignatureModule
    {
        private readonly bool _current;
        public PullSignature(bool current) { _current = current; }
        protected override bool StartTell() { return H.CarVel.x > 1f; }
        protected override void TickTell(float dt)
        {
            Vector2 a = H.MawPos, b = H.CarPos; Vector2 mid = (a + b) * 0.5f; float len = Vector2.Distance(a, b);
            Show(0, mid, new Vector2(Mathf.Min(len, V.range), _current ? 1.4f : 0.18f), Tint(0.25f + 0.4f * Pulse(10f)), SpriteFactory.Box(Color.white), Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg, 12);
        }
        protected override bool TickActive(float dt)
        {
            if (Vector2.Distance(H.MawPos, H.CarPos) > V.range) return false;
            float resist = H.Boosting ? 0.35f : 1f;
            H.CarForce(new Vector2(-V.power * resist * H.CarMass, 0f));
            Vector2 a = H.MawPos, b = H.CarPos;
            Show(0, (a + b) * 0.5f, new Vector2(Vector2.Distance(a, b), _current ? 1.4f : 0.18f), Tint(0.5f), SpriteFactory.Box(Color.white), Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg, 12);
            return false;
        }
    }

    /// <summary>A sinkhole that drifts back along the road; inside it the car is dragged down in speed (boost or a jump escapes).</summary>
    public sealed class SinkholeSignature : EaterSignatureModule
    {
        private Vector2 _p;
        protected override bool StartTell() { return AimPoint(8f, out _p); }
        protected override void TickTell(float dt) { Show(0, _p + Vector2.up * 0.1f, new Vector2(V.radius * 2f * (0.5f + 0.5f * T / V.tellSeconds), 0.5f), Tint(0.3f + 0.5f * Pulse(12f)), Art != null ? Art.tellRing : null, 0f, 13); }
        protected override bool TickActive(float dt)
        {
            _p.x -= 3f * dt; if (H.Road.TryGetRoadYAtX(_p.x, out float y)) _p.y = y;
            Show(0, _p + Vector2.up * 0.1f, new Vector2(V.radius * 2f, 0.6f), new Color(0.15f, 0.1f, 0.05f, 0.9f), Art != null ? Art.tellRing : null, 0f, 13);
            if (InZone(_p, V.radius) && H.Grounded) H.CarForce(new Vector2(-Mathf.Sign(H.CarVel.x) * V.power * H.CarMass * (H.Boosting ? 0.4f : 1f), 0f));
            return false;
        }
    }

    /// <summary>A scan line sweeps forward from the Eater and erases the road it crosses (never within the protected zone of the car).</summary>
    public sealed class ScanSignature : EaterSignatureModule
    {
        private float _x;
        protected override bool StartTell() { _x = H.MawPos.x; return true; }
        protected override void TickTell(float dt) { Show(0, new Vector2(_x, H.CarPos.y + 2f), new Vector2(0.12f, 12f), Tint(0.25f + 0.5f * Pulse(18f)), SpriteFactory.Box(Color.white), 0f, 30); }
        protected override bool TickActive(float dt)
        {
            float target = H.CarPos.x - 6.5f; _x = Mathf.MoveTowards(_x, target, Mathf.Max(12f, (target - _x) / Mathf.Max(0.3f, V.activeSeconds - T)) * dt);
            Show(0, new Vector2(_x, H.CarPos.y + 2f), new Vector2(0.25f, 12f), Tint(0.95f), SpriteFactory.Box(Color.white), 0f, 30);
            H.ForceEatX = Mathf.Max(H.ForceEatX, _x);
            return _x >= target - 0.01f;
        }
    }

    /// <summary>Registry: signature id to a configured module. Adding an attack means one row here, never a switch in the controller.</summary>
    public static class EaterSignatures
    {
        private static readonly Dictionary<EaterSignature, Func<EaterSignatureModule>> Map = new Dictionary<EaterSignature, Func<EaterSignatureModule>>
        {
            { EaterSignature.BurrowEmerge, () => new ZoneSignature("DIRT MAW EMERGED!", new[] { 0f }, true, false, false, false, false, false) },
            { EaterSignature.Sinkhole, () => new SinkholeSignature() },
            { EaterSignature.LeechJump, () => new ZoneSignature("LUNAR LEECH LANDED!", new[] { 0f }, false, true, false, false, false, false) },
            { EaterSignature.MetalPull, () => new PullSignature(false) },
            { EaterSignature.BurningGround, () => new ZoneSignature("BURNING GROUND!", new[] { 0f }, false, false, false, false, true, true) },
            { EaterSignature.SuctionCurrent, () => new PullSignature(true) },
            { EaterSignature.StretchSnap, () => new ZoneSignature("GUMMY SNAP!", new[] { 0f }, false, false, true, false, false, false) },
            { EaterSignature.CeilingRocks, () => new ZoneSignature("FALLING ROCKS!", new[] { 0f, 5.5f }, false, false, false, true, false, false) },
            { EaterSignature.ScanLineErase, () => new ScanSignature() },
            { EaterSignature.ShadowTeleport, () => new ZoneSignature("SHADOW BITE!", new[] { 0f }, true, false, false, false, false, false) },
        };
        public static EaterSignatureModule Create(EaterSignature s) { return Map.TryGetValue(s, out var f) ? f() : null; }
        public static bool Has(EaterSignature s) { return Map.ContainsKey(s); }
    }
}
