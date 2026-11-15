using System;
using UnityEngine;

namespace RoadBuilderGame
{
    public enum BossState { Dormant, Intro, Pattern, Lull, PhaseChange, Defeat, Retreat, Done }
    public enum BossOutcome { None, Dodged, Near, Perfect, Hit, HitShielded }
    public enum BossEvent { IntroStart, PatternStart, RingDue, PhaseTelegraph, PhaseChanged, Cleared, Defeated, TimedOut, Gone }

    /// <summary>One beat the controller must show and fire (the model has already applied the phase, stationary and counter-gate rules).</summary>
    public struct BeatRequest
    {
        public ProjectileKind Kind;
        public BeatMode Mode;
        /// <summary>Harmless variant: aimed 8 m ahead of the car (stationary car or failed counter gate).</summary>
        public bool WarningOnly;
        public bool Downgraded;
    }

    /// <summary>What the scene tells the boss every tick. A struct so Tick allocates nothing.</summary>
    public struct BossContext
    {
        public float CarSpeed;
        public bool CounterAvailable;      // free boost full, 5 coins, or ink at 15 percent (FR-4)
        public bool InkAvailable;          // a gap can still be drawn (roller counter)
        public bool RoadAheadForRoller;
        public int Alive;
        public float SinceBolt, SinceBomb, SinceRoller;
    }

    /// <summary>
    /// RO-B0 brain (hazards.md section 5): the Overload counter meter (instead of HP), three phases by meter, seeded pattern selection,
    /// lulls with exactly one ring, phase-change telegraph, timeout and defeat. Pure C#, scaled time, zero allocation in <see cref="Tick"/>.
    /// Projectile flight, hit tests and visuals live in the scene controller, which reports outcomes back through <see cref="Resolve"/>.
    /// </summary>
    public sealed class BossModel
    {
        private readonly BossDefinitionSO _d;
        private Pcg32 _rng;
        private AttackPatternSO _pattern, _previous;
        private float _t, _stateLeft, _fightClock, _patternClock, _quietLeft;
        private int _nextBeat;

        public BossState State { get; private set; }
        public float Meter { get; private set; }
        public int Phase { get; private set; } = 1;
        public float Meter01 => _d.meterMax > 0f ? Mathf.Clamp01(Meter / _d.meterMax) : 0f;
        public float FightClock => _fightClock;
        /// <summary>No projectile may start while this is above zero (lull after a hit, phase quiet).</summary>
        public float QuietLeft => _quietLeft;
        public bool Fighting => State == BossState.Pattern || State == BossState.Lull || State == BossState.PhaseChange;
        public AttackPatternSO CurrentPattern => _pattern;
        public int PatternsStarted { get; private set; }
        public BossDefinitionSO Data => _d;

        public event Action<BossEvent> Raised;
        public event Action<BeatRequest> Beat;

        public BossModel(BossDefinitionSO d) { _d = d; }

        public void Reset(uint seed)
        {
            State = BossState.Dormant; Meter = 0f; Phase = 1; _pattern = _previous = null; _fightClock = _patternClock = _quietLeft = _stateLeft = 0f; _nextBeat = 0;
            PatternsStarted = 0; _rng = new Pcg32(seed == 0u ? 1u : seed);
        }

        public void Begin() { if (State != BossState.Dormant) return; State = BossState.Intro; _stateLeft = _d.introSeconds; Raised?.Invoke(BossEvent.IntroStart); }

        public int PhaseFor(float meter)
        {
            int p = 1;
            if (_d.phaseThresholds != null) for (int i = 0; i < _d.phaseThresholds.Length; i++) if (meter >= _d.phaseThresholds[i]) p = i + 2;
            return Mathf.Min(p, 3);
        }

        /// <summary>A projectile aimed at the car resolved. Only dodges, near misses and hits change the meter; a hit also clears the field and starts a lull.</summary>
        public void Resolve(BossOutcome o)
        {
            if (!Fighting && State != BossState.Intro) return;
            switch (o)
            {
                case BossOutcome.Dodged: AddMeter(_d.meterDodge); break;
                case BossOutcome.Near: AddMeter(_d.meterNear); break;
                case BossOutcome.Perfect: AddMeter(_d.meterPerfect); break;
                case BossOutcome.Hit: Meter = Mathf.Max(0f, Meter + _d.meterHitPenalty); Interrupt(); break;
                case BossOutcome.HitShielded: Interrupt(); break;        // a shield-absorbed hit costs no meter
            }
        }

        public void RingPassed() { if (Fighting) AddMeter(_d.meterRing); }

        /// <summary>Hit, shield hit or respawn: projectiles cleared, 3 s lull plus 0.5 s grace, no ring for the aborted pattern.</summary>
        public void Interrupt()
        {
            if (!Fighting) return;
            _pattern = null; _nextBeat = 0;
            State = BossState.Lull; _stateLeft = _d.hitLullSeconds + _d.hitGraceSeconds; _quietLeft = _stateLeft;
            Raised?.Invoke(BossEvent.Cleared);
        }

        private void AddMeter(float v)
        {
            Meter = Mathf.Clamp(Meter + v, 0f, _d.meterMax);
            if (Meter >= _d.meterMax && State != BossState.Defeat && State != BossState.Done)
            {
                State = BossState.Defeat; _stateLeft = _d.defeatSeconds; _pattern = null; _quietLeft = _stateLeft;
                Raised?.Invoke(BossEvent.Cleared); Raised?.Invoke(BossEvent.Defeated);
            }
        }

        public void Tick(float dt, in BossContext ctx)
        {
            if (dt <= 0f || State == BossState.Dormant || State == BossState.Done) return;
            if (_quietLeft > 0f) _quietLeft -= dt;
            switch (State)
            {
                case BossState.Intro:
                    _stateLeft -= dt; if (_stateLeft <= 0f) StartLull(0.5f, false);
                    return;
                case BossState.Defeat:
                    _stateLeft -= dt; if (_stateLeft <= 0f) { State = BossState.Done; Raised?.Invoke(BossEvent.Gone); }
                    return;
                case BossState.Retreat:
                    _stateLeft -= dt; if (_stateLeft <= 0f) { State = BossState.Done; Raised?.Invoke(BossEvent.Gone); }
                    return;
            }
            _fightClock += dt;
            AddMeter(_d.meterPassivePerSec * dt);
            if (State == BossState.Defeat) return;
            if (_fightClock >= _d.timeoutSeconds) { State = BossState.Retreat; _stateLeft = 1.5f; _pattern = null; Raised?.Invoke(BossEvent.Cleared); Raised?.Invoke(BossEvent.TimedOut); return; }

            if (State == BossState.Lull)
            {
                _stateLeft -= dt;
                if (_stateLeft <= 0f)
                {
                    if (PhaseFor(Meter) > Phase)
                    {
                        State = BossState.PhaseChange; _stateLeft = Mathf.Max(_d.phaseQuiet, _d.phaseTelegraph); _quietLeft = _stateLeft; Phase = PhaseFor(Meter);
                        Raised?.Invoke(BossEvent.PhaseTelegraph);
                    }
                    else StartPattern();
                }
            }
            else if (State == BossState.PhaseChange)
            {
                _stateLeft -= dt;
                if (_stateLeft <= 0f) { Raised?.Invoke(BossEvent.PhaseChanged); StartPattern(); }
            }
            else if (State == BossState.Pattern)
            {
                _patternClock += dt;
                var beats = _pattern.beats;
                while (_nextBeat < beats.Length && _patternClock >= beats[_nextBeat].tOffset)
                {
                    if (!TryFire(in beats[_nextBeat], in ctx)) { if (_patternClock - beats[_nextBeat].tOffset < 0.6f) break; }   // wait up to 0.6 s for a free slot, then skip the beat
                    _nextBeat++;
                }
                if (_patternClock >= _pattern.duration && _nextBeat >= beats.Length) StartLull(_d.lullSeconds, true);
            }
        }

        private void StartLull(float seconds, bool ring)
        {
            State = BossState.Lull; _stateLeft = seconds; _pattern = null;
            if (ring) { Raised?.Invoke(BossEvent.RingDue); }
        }

        private void StartPattern()
        {
            var pool = _d.Pool(Phase);
            AttackPatternSO pick = null;
            if (pool != null && pool.Length > 0)
            {
                int i = _rng.NextInt(pool.Length);
                pick = pool[i];
                if (pool.Length > 1 && pick == _previous) pick = pool[(i + 1) % pool.Length];       // never repeat the previous pattern
            }
            _pattern = pick; _previous = pick; _patternClock = 0f; _nextBeat = 0; PatternsStarted++;
            if (_pattern == null) { StartLull(_d.lullSeconds, false); return; }
            State = BossState.Pattern;
            Raised?.Invoke(BossEvent.PatternStart);
        }

        private bool TryFire(in AttackBeat b, in BossContext ctx)
        {
            var mode = b.mode;
            if (Phase < b.phaseMin) { if (b.fallbackMode != b.mode) mode = b.fallbackMode; else return true; }   // beat belongs to a later phase: skip it
            int cap = Mathf.Min(_pattern.maxAlive, _d.maxAlive);
            if (ctx.Alive >= cap) return false;
            float since = b.projectile == ProjectileKind.Bolt ? ctx.SinceBolt : b.projectile == ProjectileKind.Bomb ? ctx.SinceBomb : ctx.SinceRoller;
            if (since < _d.sameTypeMinSeparation) return false;

            var req = new BeatRequest { Kind = b.projectile, Mode = mode };
            bool stationary = ctx.CarSpeed < _d.stationarySpeed;
            if (b.projectile == ProjectileKind.Roller)
            {
                if (stationary || !ctx.InkAvailable || !ctx.RoadAheadForRoller) return true;          // skipped (harmless)
            }
            else if (b.projectile == ProjectileKind.Bomb)
            {
                if (mode == BeatMode.Direct && (stationary || !ctx.CounterAvailable)) { req.Mode = BeatMode.Lead; req.Downgraded = true; }
                if (stationary) req.Mode = BeatMode.Lead;
            }
            else
            {
                if (stationary || !ctx.CounterAvailable) { req.WarningOnly = true; req.Downgraded = !stationary; }
            }
            Beat?.Invoke(req);
            return true;
        }
    }
}
