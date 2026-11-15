using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoadBuilderGame
{
    /// <summary>One way to defeat the Road-Eater (A3). <see cref="Tick"/> returns true when it is defeated; every effect is a prop or a banner, the Eater model is not touched here.</summary>
    public abstract class EaterCounterplay
    {
        protected EaterVariantSO V; protected EaterHost H; protected float Clock;
        protected readonly List<SpriteRenderer> Props = new List<SpriteRenderer>();
        public virtual string Hint { get { return ""; } }
        /// <summary>0..1 progress shown by the HUD (phase or gate countdown), -1 when not applicable.</summary>
        public virtual float Progress01 { get { return -1f; } }
        public virtual int PursuitPhase { get { return 1; } }

        public void Begin(EaterVariantSO v, EaterHost h) { V = v; H = h; Clock = 0f; OnBegin(); }
        public virtual void Stop() { for (int i = 0; i < Props.Count; i++) if (Props[i] != null) Props[i].gameObject.SetActive(false); }
        public void Destroy() { for (int i = 0; i < Props.Count; i++) if (Props[i] != null) UnityEngine.Object.Destroy(Props[i].gameObject); Props.Clear(); }
        protected virtual void OnBegin() { }
        public abstract bool Tick(float dt);
        public virtual void OnLifeLost() { }

        protected SpriteRenderer Prop(int i, Sprite sprite, Color c, int order)
        {
            while (Props.Count <= i) Props.Add(null);
            if (Props[i] == null) { Props[i] = SpriteFactory.CreateSprite("EaterCounter" + i, null, sprite != null ? sprite : SpriteFactory.Circle, c, order); Props[i].gameObject.SetActive(false); }
            return Props[i];
        }
        protected float RoadY(float x) { return H.Road.TryGetRoadYAtX(x, out float y) ? y : H.CarPos.y - 0.5f; }
    }

    /// <summary>Boost ring lure: a cyan ring floats ahead; taking it while the Eater is close (gap under lureMaxGap) sends the Eater into the burst and defeats it.</summary>
    public sealed class LureRingCounter : EaterCounterplay
    {
        private float _next, _x, _y; private bool _live;
        public override string Hint { get { return "LURE IT INTO THE RING"; } }
        protected override void OnBegin() { _next = 6f; _live = false; }
        public override bool Tick(float dt)
        {
            Clock += dt; var art = HazardArtSet.Load(); var r = Prop(0, art != null ? art.tellRing : null, new Color(0.3f, 0.9f, 1f, 0.95f), 20);
            if (!_live)
            {
                if (Clock >= _next && H.Eating) { _x = H.CarPos.x + 14f + Mathf.Max(0f, H.CarVel.x); if (!H.Road.HasRoadAt(_x)) { _next = Clock + 2f; return false; } _y = RoadY(_x) + 1.6f; _live = true; r.gameObject.SetActive(true); H.Banner("LURE RING!"); }
                return false;
            }
            r.transform.position = new Vector3(_x, _y, 0f); r.transform.localScale = Vector3.one * (1.7f + 0.2f * Mathf.Sin(Clock * 8f));
            if (Vector2.Distance(H.CarPos, new Vector2(_x, _y)) <= 1.6f)
            {
                _live = false; r.gameObject.SetActive(false); _next = Clock + V.lureEverySeconds;
                if (ParticleManager.Instance) ParticleManager.Instance.SpawnBurst(new Vector2(_x, _y), new Color(0.3f, 0.9f, 1f), 18);
                if (H.Gap <= V.lureMaxGap) return true;
                H.Banner("TOO FAR! LET IT CLOSE IN");
            }
            else if (H.CarPos.x > _x + 4f) { _live = false; r.gameObject.SetActive(false); _next = Clock + 3f; }
            return false;
        }
    }

    /// <summary>Weak road span (collapse trap or heavy platform): placed ahead, armed when the car crosses it boosted or airborne (trap) or heavy (platform); collapses when the Eater front reaches it.</summary>
    public sealed class WeakSpanCounter : EaterCounterplay
    {
        private readonly bool _heavy; private float _next, _x; private bool _live, _armed, _passed;
        public WeakSpanCounter(bool heavy) { _heavy = heavy; }
        public override string Hint { get { return _heavy ? "HEAVY VEHICLES BREAK THE PLATFORM" : "BOOST OR JUMP OVER THE WEAK SPAN"; } }
        protected override void OnBegin() { _next = 8f; _live = false; }
        public override bool Tick(float dt)
        {
            Clock += dt; var art = HazardArtSet.Load(); var r = Prop(0, art != null ? art.trapStrip : SpriteFactory.Box(Color.white), new Color(0.75f, 0.55f, 0.3f, 0.95f), 12);
            if (!_live)
            {
                if (Clock >= _next && H.Eating) { _x = H.CarPos.x + V.spanAheadM + Mathf.Max(0f, H.CarVel.x); if (!H.Road.HasRoadAt(_x)) { _next = Clock + 2f; return false; } _live = true; _armed = _passed = false; r.gameObject.SetActive(true); H.Banner(_heavy ? "WEAK PLATFORM" : "WEAK SPAN"); }
                return false;
            }
            r.transform.position = new Vector3(_x, RoadY(_x) + 0.15f, 0f); r.transform.localScale = new Vector3(V.spanLength, 0.4f, 1f);
            if (!_passed && H.CarPos.x > _x + V.spanLength * 0.5f)
            {
                _passed = true;
                _armed = _heavy ? H.CarMass >= V.heavyMass : (H.Boosting || !H.Grounded);
                r.color = _armed ? new Color(1f, 0.45f, 0.15f, 0.95f) : new Color(0.75f, 0.55f, 0.3f, 0.6f);
                if (!_armed) { H.Banner(_heavy ? "TOO LIGHT" : "NOT SHAKEN LOOSE"); }
            }
            if (_passed && H.EaterX >= _x - V.spanLength * 0.5f)
            {
                r.gameObject.SetActive(false); _live = false; _next = Clock + V.spanEverySeconds;
                if (_armed) { if (ParticleManager.Instance) ParticleManager.Instance.SpawnBurst(new Vector2(_x, RoadY(_x)), V.tint, 24); return true; }
            }
            return false;
        }
    }

    /// <summary>Three pursuit phases: it gets faster to attack each phase; surviving all three defeats it.</summary>
    public sealed class ThreePhaseCounter : EaterCounterplay
    {
        private int _phase = 1;
        public override string Hint { get { return "SURVIVE 3 PHASES"; } }
        public override float Progress01 { get { return Mathf.Clamp01(Clock / (V.phaseSeconds * 3f)); } }
        public override int PursuitPhase { get { return _phase; } }
        protected override void OnBegin() { _phase = 1; }
        public override bool Tick(float dt)
        {
            if (!H.Eating) return false;
            Clock += dt; int p = Mathf.Clamp(1 + (int)(Clock / V.phaseSeconds), 1, 3);
            if (p != _phase) { _phase = p; H.Banner("EATER PHASE " + p); H.Cue("sfx.boss.phaseFlare"); H.Shake(0.3f); }
            return Clock >= V.phaseSeconds * 3f;
        }
    }

    /// <summary>Final gate: a gate stands ahead; 8 s after crossing it the Eater gives up. A bite (lost heart) restarts the 8 s.</summary>
    public sealed class FinalGateCounter : EaterCounterplay
    {
        private float _gateX, _after; private bool _crossed;
        public override string Hint { get { return "REACH THE GATE, THEN HOLD 8 S"; } }
        public override float Progress01 { get { return _crossed ? Mathf.Clamp01(_after / V.gateEscapeSeconds) : 0f; } }
        protected override void OnBegin() { _gateX = H.ChaseStartX + V.gateAfterChaseM; _after = 0f; _crossed = false; }
        public override void OnLifeLost() { _after = 0f; }
        public override bool Tick(float dt)
        {
            var art = HazardArtSet.Load(); var g = Prop(0, art != null ? art.finishGate : null, new Color(1f, 0.85f, 0.3f, 0.95f), 11);
            if (!_crossed && H.CarPos.x > _gateX - 60f && H.Road.HasRoadAt(_gateX)) { g.gameObject.SetActive(true); g.transform.position = new Vector3(_gateX, RoadY(_gateX) + 2.2f, 0f); g.transform.localScale = Vector3.one; }
            if (!_crossed && H.CarPos.x >= _gateX) { _crossed = true; H.Banner("GATE! HOLD ON 8 S"); H.Cue("vo.rob0.s1.rings"); }
            if (_crossed) { _after += dt; if (_after >= V.gateEscapeSeconds) return true; }
            return false;
        }
    }

    public static class EaterCounterplays
    {
        private static readonly Dictionary<EaterDefeatOption, Func<EaterCounterplay>> Map = new Dictionary<EaterDefeatOption, Func<EaterCounterplay>>
        {
            { EaterDefeatOption.BoostRingLure, () => new LureRingCounter() },
            { EaterDefeatOption.CollapseTrap, () => new WeakSpanCounter(false) },
            { EaterDefeatOption.HeavyPlatform, () => new WeakSpanCounter(true) },
            { EaterDefeatOption.ThreePhases, () => new ThreePhaseCounter() },
            { EaterDefeatOption.FinalGateEscape, () => new FinalGateCounter() },
        };
        public static EaterCounterplay Create(EaterDefeatOption o) { return Map.TryGetValue(o, out var f) ? f() : null; }
    }
}
