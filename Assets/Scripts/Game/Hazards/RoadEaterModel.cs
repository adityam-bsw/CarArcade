using System;
using UnityEngine;

namespace RoadBuilderGame
{
    public enum EaterState { Dormant, Warn, Track, Surge, Snap, Pushback, Escape, Retreat, Done }
    public enum EaterBand { Calm, Edge, Danger, Snap }
    public enum EaterEvent { WarnStart, ChaseStart, SurgeTelegraph, SurgeStart, SurgeEnd, SnapStart, SnapCancel, Bite, PushbackEnd, Escaped, TimedOut, Gone, ChewStart, NearMiss, BandChanged }

    /// <summary>
    /// Road-Eater as a scalar behind the car (hazards.md section 2): spawn gap, gap-based speed controller, surge, snap, pushback, escape, timeout.
    /// Pure C#, scaled time and distance in, events out. No scene access and no allocation in <see cref="Tick"/>.
    /// <see cref="EatFront"/> is the Eater front clamped to <c>protectedZone</c> behind the car, so the scene side can delete road up to it and never under the vehicle (FR-10).
    /// </summary>
    public sealed class RoadEaterModel
    {
        private readonly MonsterDefinitionSO _d;
        private Pcg32 _rng;
        private float _chewLeft, _chewCooldown, _surgeIn, _surgeTelegraphLeft, _surgeLeft, _snapLeft, _snapAirborne, _freezeLeft, _exitLeft;
        private float _escapeHold, _belowResetFor, _warnClock, _lowGapAgo = -1f, _nearCooldown, _graceLeft;
        private int _jumps;

        public EaterState State { get; private set; }
        public EaterBand Band { get; private set; }
        /// <summary>Distance of the Eater front along the road, m.</summary>
        public float S { get; private set; }
        public float Gap { get; private set; }
        public float Speed { get; private set; }
        public float EscapeProgress01 => _d.escapeHoldSeconds > 0f ? Mathf.Clamp01(_escapeHold / _d.escapeHoldSeconds) : 0f;
        public bool Telegraphing => _surgeTelegraphLeft > 0f;
        public float SnapLeft => _snapLeft;
        public float GraceLeft => _graceLeft;
        /// <summary>The Eater is deleting road right now (chase states only; escape and retreat stop it).</summary>
        public bool Eating => State == EaterState.Track || State == EaterState.Surge || State == EaterState.Snap;
        public bool Alive => State != EaterState.Dormant && State != EaterState.Done;
        public bool Chasing => Eating || State == EaterState.Pushback;
        /// <summary>Highest distance road may be deleted up to: the front, never closer than protectedZone to the car.</summary>
        public float EatFront { get; private set; }
        /// <summary>Surface numbers under the Eater (section C): speed multiplier, a stun in seconds (0 = none) and eat-speed multiplier. Set by the controller before each tick.</summary>
        public float SurfaceSlowMul = 1f, SurfaceEatMul = 1f, StunLeft, StunCooldown;
        /// <summary>The Eater touches a stunning spot: it stops for <paramref name="seconds"/>, then cannot be stunned again for 4 s (no stun-lock).</summary>
        public void ApplySurface(float slowMul, float eatMul, float stunSeconds)
        {
            SurfaceSlowMul = slowMul <= 0f ? 1f : slowMul; SurfaceEatMul = eatMul <= 0f ? 1f : eatMul;
            if (stunSeconds > 0f && StunLeft <= 0f && StunCooldown <= 0f) { StunLeft = stunSeconds; StunCooldown = stunSeconds + 4f; }
        }
        /// <summary>Cheat spawn: own timeout in metres of Eater-relative distance (0 = the data's timeoutDistance). Cleared by Reset.</summary>
        public float TimeoutOverrideS;
        public MonsterDefinitionSO Data => _d;

        public event Action<EaterEvent> Raised;

        public RoadEaterModel(MonsterDefinitionSO data) { _d = data; }

        public void Reset(uint seed)
        {
            SurfaceSlowMul = SurfaceEatMul = 1f; StunLeft = StunCooldown = 0f; TimeoutOverrideS = 0f;
            State = EaterState.Dormant; Band = EaterBand.Calm; S = Gap = Speed = EatFront = 0f;
            _chewLeft = _chewCooldown = _surgeTelegraphLeft = _surgeLeft = _snapLeft = _snapAirborne = _freezeLeft = _exitLeft = 0f;
            _escapeHold = _belowResetFor = _warnClock = _nearCooldown = _graceLeft = 0f; _lowGapAgo = -1f; _jumps = 0;
            _rng = new Pcg32(seed == 0u ? 1u : seed);
        }

        public void BeginWarn() { if (State != EaterState.Dormant) return; State = EaterState.Warn; _warnClock = 0f; Raised?.Invoke(EaterEvent.WarnStart); }

        /// <summary>The chase begins: the Eater appears <c>startGap</c> behind the car, or at the oldest drawn road if less road than that exists (never closer than the target gap).</summary>
        public void StartChase(float carS, float roadBehindM)
        {
            if (State != EaterState.Dormant && State != EaterState.Warn) return;
            float gap = Mathf.Clamp(roadBehindM, _d.targetGap, _d.startGap);
            S = carS - gap; Gap = gap; Speed = 0f; EatFront = Mathf.Min(S, carS - _d.protectedZone);
            State = EaterState.Track; Band = BandOf(gap);
            _surgeIn = NextSurge(); _jumps = 0; _chewCooldown = 0f; _escapeHold = 0f;
            Raised?.Invoke(EaterEvent.ChaseStart);
        }

        /// <summary>The car lost a heart or used a shield (any source) while chasing: gap resets to pushbackGap, the Eater freezes 1.5 s, then a 1.5 s grace.</summary>
        public void OnLifeLost(float carS)
        {
            if (!Chasing) return;
            State = EaterState.Pushback; S = carS - _d.pushbackGap; Gap = _d.pushbackGap; Speed = 0f; EatFront = Mathf.Min(EatFront, carS - _d.protectedZone);
            _freezeLeft = _d.pushbackFreeze; _graceLeft = _d.pushbackFreeze + _d.pushbackGrace;
            _snapLeft = 0f; _surgeTelegraphLeft = 0f; _surgeLeft = 0f; _lowGapAgo = -1f;
        }

        /// <summary>The eat front reached a road gap of <paramref name="lengthM"/> that the car already crossed: the Eater chews (slowed) for a while. Bounded: cooldown and a per-chase cap.</summary>
        public bool OnGapReached(float lengthM)
        {
            if (!Eating || _chewCooldown > 0f || _jumps >= _d.jumpMaxPerChase) return false;
            if (lengthM < _d.jumpMinGapLength || lengthM > _d.jumpMaxGapLength) return false;
            _jumps++; _chewLeft = _d.jumpDelaySeconds; _chewCooldown = _d.jumpCooldown;
            Raised?.Invoke(EaterEvent.ChewStart);
            return true;
        }

        public void Retreat(bool timeout)
        {
            if (!Chasing) return;
            State = EaterState.Retreat; _exitLeft = _d.exitSeconds; _snapLeft = 0f; _surgeLeft = _surgeTelegraphLeft = 0f;
            Raised?.Invoke(timeout ? EaterEvent.TimedOut : EaterEvent.Escaped);
        }

        public static EaterBand BandOf(MonsterDefinitionSO d, float gap)
        {
            return gap > d.bandCalm ? EaterBand.Calm : gap > d.bandEdge ? EaterBand.Edge : gap > d.bandDanger ? EaterBand.Danger : EaterBand.Snap;
        }
        private EaterBand BandOf(float gap) { return BandOf(_d, gap); }
        private float NextSurge() { return _d.surgeIntervalMin + (_d.surgeIntervalMax - _d.surgeIntervalMin) * _rng.NextFloat01(); }

        /// <summary>Scaled time only (pause means not calling it). Zero allocation.</summary>
        public void Tick(float dt, float carS, float carSpeed, bool airborne, bool boosting)
        {
            if (State == EaterState.Dormant || State == EaterState.Done || dt <= 0f) return;
            if (_graceLeft > 0f) _graceLeft -= dt;
            if (_chewCooldown > 0f) _chewCooldown -= dt;
            if (_nearCooldown > 0f) _nearCooldown -= dt;
            if (State == EaterState.Warn) { _warnClock += dt; return; }

            float vRef = Mathf.Max(carSpeed, _d.vFloor);

            if (State == EaterState.Pushback)
            {
                _freezeLeft -= dt; Gap = carS - S; EatFront = Mathf.Min(EatFront, carS - _d.protectedZone);
                if (_freezeLeft <= 0f) { State = EaterState.Track; _surgeIn = NextSurge(); Raised?.Invoke(EaterEvent.PushbackEnd); }
                return;
            }
            if (State == EaterState.Escape || State == EaterState.Retreat)
            {
                Speed = Mathf.MoveTowards(Speed, 0.5f * vRef, _d.decelMax * dt);
                S = Mathf.Min(S + Speed * dt, carS - _d.protectedZone); Gap = carS - S;
                _exitLeft -= dt;
                if (_exitLeft <= 0f) { State = EaterState.Done; Raised?.Invoke(EaterEvent.Gone); }
                return;
            }

            Gap = carS - S;
            if (carS >= (TimeoutOverrideS > 0f ? TimeoutOverrideS : _d.timeoutDistance)) { Retreat(true); return; }

            // ---- snap
            if (State == EaterState.Snap)
            {
                if (Gap >= _d.snapCancelGap) { State = EaterState.Track; _snapLeft = 0f; Raised?.Invoke(EaterEvent.SnapCancel); }
                else if (airborne && _snapAirborne < _d.snapAirborneMaxS) _snapAirborne += dt;   // the timer waits while the car is in the air, at most 1.5 s
                else
                {
                    _snapLeft -= dt;
                    if (_snapLeft <= 0f)
                    {
                        _snapLeft = 0f; State = EaterState.Pushback; _freezeLeft = _d.pushbackFreeze; _graceLeft = _freezeLeft + _d.pushbackGrace; Speed = 0f;
                        Raised?.Invoke(EaterEvent.Bite);                 // the scene costs a heart (or a shield); OnLifeLost then places the Eater at the push-back gap
                        return;
                    }
                }
                if (State == EaterState.Snap) { UpdateBand(); NearMissTrack(dt); EatFront = Mathf.Min(S, carS - _d.protectedZone); return; }   // closing frozen
            }
            else if (Gap < _d.snapGap)
            {
                State = EaterState.Snap; _snapLeft = _d.snapSeconds; _snapAirborne = 0f; _surgeLeft = _surgeTelegraphLeft = 0f;
                Raised?.Invoke(EaterEvent.SnapStart);
                UpdateBand(); NearMissTrack(dt); EatFront = Mathf.Min(S, carS - _d.protectedZone);
                return;
            }

            // ---- surge scheduling (seeded interval, only from a safe gap)
            if (State == EaterState.Track)
            {
                if (_surgeTelegraphLeft > 0f)
                {
                    _surgeTelegraphLeft -= dt;
                    if (_surgeTelegraphLeft <= 0f) { State = EaterState.Surge; _surgeLeft = _d.surgeDuration; Raised?.Invoke(EaterEvent.SurgeStart); }
                }
                else
                {
                    _surgeIn -= dt;
                    if (_surgeIn <= 0f)
                    {
                        if (Gap >= _d.surgeMinGap) { _surgeTelegraphLeft = _d.surgeTelegraph; Raised?.Invoke(EaterEvent.SurgeTelegraph); }
                        else _surgeIn = 1f;      // too close for a surge: ask again in a second
                    }
                }
            }
            else if (State == EaterState.Surge)
            {
                _surgeLeft -= dt;
                if (_surgeLeft <= 0f || Gap < _d.surgeAbortGap) { State = EaterState.Track; _surgeIn = NextSurge(); Raised?.Invoke(EaterEvent.SurgeEnd); }
            }

            // ---- speed controller
            float r = State == EaterState.Surge ? _d.maxSpeedRatio : _d.Ratio(Gap);
            float vt = Mathf.Min(r * vRef, _d.maxSpeedRatio * vRef);
            if (_chewLeft > 0f) { vt *= _d.jumpSlowRatio; _chewLeft -= dt; }
            vt *= SurfaceSlowMul * SurfaceEatMul;
            if (StunCooldown > 0f) StunCooldown -= dt;
            if (StunLeft > 0f) { StunLeft -= dt; vt = 0f; }
            Speed = Speed < vt ? Mathf.Min(vt, Speed + _d.accelMax * dt) : Mathf.Max(vt, Speed - _d.decelMax * dt);
            S += Speed * dt;
            Gap = carS - S;
            EatFront = Mathf.Min(S, carS - _d.protectedZone);      // road is never deleted inside the protected zone; only a resolved Snap bite reaches the car (after a platform exists)

            // ---- escape hold (gap >= 45 m for 8 s; resets after 0.5 s below 43 m)
            if (Gap >= _d.escapeGap) { _escapeHold += dt; _belowResetFor = 0f; }
            else if (Gap < _d.escapeResetGap) { _belowResetFor += dt; if (_belowResetFor >= _d.escapeResetSeconds) _escapeHold = 0f; }
            else _belowResetFor = 0f;
            if (_escapeHold >= _d.escapeHoldSeconds) { State = EaterState.Escape; _exitLeft = _d.exitSeconds; Raised?.Invoke(EaterEvent.Escaped); return; }

            UpdateBand();
            NearMissTrack(dt);
        }

        private void UpdateBand()
        {
            var b = BandOf(Gap);
            if (b != Band) { Band = b; Raised?.Invoke(EaterEvent.BandChanged); }
        }

        /// <summary>Gap under 4 m recovered to over 12 m within 4 s, once per 10 s (hazards.md section 6).</summary>
        private void NearMissTrack(float dt)
        {
            if (_lowGapAgo >= 0f) { _lowGapAgo += dt; if (_lowGapAgo > _d.nearMissWindowS) _lowGapAgo = -1f; }
            if (Gap < _d.nearMissLowGap) _lowGapAgo = 0f;
            else if (_lowGapAgo >= 0f && Gap > _d.nearMissRecoverGap)
            {
                _lowGapAgo = -1f;
                if (_nearCooldown <= 0f) { _nearCooldown = _d.nearMissCooldownS; Raised?.Invoke(EaterEvent.NearMiss); }
            }
        }
    }
}
