using System;
using System.Collections.Generic;

namespace RoadBuilderGame
{
    /// <summary>
    /// Pure C# stage director core. Knows distance and scaled time only: no scene, no Mode, no vehicle or road class, no Unity random.
    /// The plan is built once in <see cref="Begin"/>; <see cref="Tick"/> only compares the car distance with precomputed thresholds
    /// (O(events), a handful, no allocation). Stack rules come from the EventCompatibilityMatrix asset, never from code ifs.
    /// Spec: Docs/design/stage-events.md section 2.
    /// </summary>
    public sealed class StageDirector : IStageDirector
    {
        /// <summary>FR-9: the respawn platform is never shorter than this (metres).</summary>
        public const float MinRespawnPlatformM = 6f;

        private StageLadderDefinition _ladder;
        private StagePlan _plan;
        private bool _enabled;
        private float _distanceM;
        private float _graceLeftS, _holdLeftS;
        private float _respawnM;
        private float _respawnClearM, _respawnPlatformM;

        public event Action<StageEventState> Warned, Started, Ended;
        public event Action<StageEventState, StageSignal> SignalFired;

        /// <param name="ladder">Null or empty means the director is disabled and CanSpawnHazard answers false for every id (safe default).</param>
        public StageDirector(StageLadderDefinition ladder) { _ladder = ladder; }

        public bool Enabled => _enabled;
        public StageLadderDefinition Ladder => _ladder;
        public void SetLadder(StageLadderDefinition l) { _ladder = l; }
        public StagePlan Plan => _plan;
        public IReadOnlyList<StageLogEntry> Log => _plan != null ? _plan.Log : null;
        public StageRunRecord Record => _plan != null ? _plan.Record : default;
        public float FinishM => _plan != null ? _plan.FinishM : 0f;
        public float DistanceM => _distanceM;
        public float RespawnClearM => _respawnClearM;
        public float RespawnPlatformM => _respawnPlatformM;
        public float RespawnDistanceM => _respawnM;
        public float HazardGraceLeftS => _graceLeftS;

        public StageEventState NextEvent
        {
            get
            {
                if (_plan == null || _plan.Events.Count == 0) return StageEventState.None;
                StageEventState last = StageEventState.None;
                var ev = _plan.Events;
                for (int i = 0; i < ev.Count; i++)
                {
                    var e = ev[i];
                    if (e.Definition.IsModifier) continue;
                    last = e;
                    if (e.Phase < StagePhase.Active) return e;
                }
                return last;
            }
        }

        public StageEventState ActiveEvent
        {
            get
            {
                if (_plan == null) return null;
                var ev = _plan.Events;
                for (int i = 0; i < ev.Count; i++) if (ev[i].Phase == StagePhase.Active && !ev[i].Definition.IsModifier) return ev[i];
                return null;
            }
        }

        public int ActiveModifierCount
        {
            get
            {
                if (_plan == null) return 0;
                int n = 0; var ev = _plan.Events;
                for (int i = 0; i < ev.Count; i++) if (ev[i].Phase == StagePhase.Active && ev[i].Definition.IsModifier) n += ev[i].Definition.modifierSlots;
                return n;
            }
        }

        /// <summary>No primary and no modifier is Active: coins and ink only.</summary>
        public bool Breather
        {
            get
            {
                if (_plan == null) return true;
                var ev = _plan.Events;
                for (int i = 0; i < ev.Count; i++) if (ev[i].Phase == StagePhase.Active) return false;
                return true;
            }
        }

        public bool CanSpawnHazard(string hazardId)
        {
            if (!_enabled || string.IsNullOrEmpty(hazardId)) return false;
            if (_graceLeftS > 0f) return false;
            if (_distanceM < _ladder.noHazardUntilM) return false;
            var ev = _plan.Events;
            StageEventState owner = null;
            for (int i = 0; i < ev.Count; i++)
                if (ev[i].Phase == StagePhase.Active && ev[i].Definition.spawnerId == hazardId) { owner = ev[i]; break; }
            if (owner == null) return false;
            for (int i = 0; i < ev.Count; i++)
            {
                var e = ev[i];
                if (e == owner || e.Phase != StagePhase.Warning || e.Definition.role == StageEventRole.Reward) continue;
                if (_ladder.matrix == null || !_ladder.matrix.CanOverlap(owner.Id, e.Id)) return false;     // an incompatible event is warning: no new hazards
            }
            return true;
        }

        /// <summary>CanSpawnHazard plus the respawn clear zone (hazards within clearOnRespawnM of the respawn stay out for the hold time).</summary>
        public bool CanSpawnHazardAt(string hazardId, float hazardDistanceM)
        {
            if (!CanSpawnHazard(hazardId)) return false;
            if (_holdLeftS > 0f && Math.Abs(hazardDistanceM - _respawnM) < _respawnClearM) return false;
            return true;
        }

        public void Begin(StageRunRecord run)
        {
            _plan = StageLadderPlanner.Build(_ladder, run);
            _enabled = _ladder != null && _plan.Events.Count > 0;
            _distanceM = 0f; _graceLeftS = 0f; _holdLeftS = 0f; _respawnM = 0f; _respawnClearM = 0f; _respawnPlatformM = 0f;
            // Spawn at distance 0: everything starts Planned or Locked (an event inside its lock distance of 0 would be Locked).
            for (int i = 0; i < _plan.Events.Count; i++)
            {
                var e = _plan.Events[i];
                e.Phase = PhaseAt(e, 0f); e.SignalS2Fired = e.SignalS3Fired = false;
            }
        }

        /// <summary>
        /// Plans the endless ladder from <paramref name="fromM"/> on (0 = a fresh run, or the finish gate of the slice) and makes it the current plan. Events of the old
        /// plan that are Active end first. Pure planning: the same seed, world tags and eased stages always give the same plan.
        /// </summary>
        public void BeginEndless(StageLadderDefinition endless, StageRunRecord run, float fromM, StageWorldContext ctx)
        {
            if (_plan != null)
            {
                var old = _plan.Events;
                for (int i = 0; i < old.Count; i++) if (old[i].Phase == StagePhase.Active) { old[i].Phase = StagePhase.Ended; if (Ended != null) Ended(old[i]); }
            }
            if (endless != null) _ladder = endless;
            _plan = EndlessLadderPlanner.Plan(_ladder, run, fromM, ctx);
            _enabled = _ladder != null && _plan.Events.Count > 0;
            if (fromM <= 0f) { _distanceM = 0f; _graceLeftS = 0f; _holdLeftS = 0f; _respawnM = 0f; _respawnClearM = 0f; _respawnPlatformM = 0f; }
            for (int i = 0; i < _plan.Events.Count; i++)
            {
                var e = _plan.Events[i];
                e.Phase = PhaseAt(e, _distanceM); e.SignalS2Fired = e.SignalS3Fired = false;
            }
        }

        public void Tick(float carDistanceM, float carSpeedMps, float scaledDt)
        {
            if (!_enabled) return;
            if (scaledDt > 0f)
            {
                if (_graceLeftS > 0f) _graceLeftS -= scaledDt;
                if (_holdLeftS > 0f) _holdLeftS -= scaledDt;
            }
            _distanceM = carDistanceM;
            Refresh(carDistanceM, true);
        }

        public void OnRespawn(float carDistanceM)
        {
            if (!_enabled) return;
            _respawnM = carDistanceM;
            _graceLeftS = _ladder.respawnGraceS;
            _holdLeftS = _ladder.respawnEffectHoldS;
            _respawnClearM = 0f; _respawnPlatformM = MinRespawnPlatformM;
            var ev = _plan.Events;
            for (int i = 0; i < ev.Count; i++)
            {
                if (ev[i].Phase != StagePhase.Active) continue;
                if (ev[i].Definition.clearOnRespawnM > _respawnClearM) _respawnClearM = ev[i].Definition.clearOnRespawnM;
                if (ev[i].Definition.safePlatformM > _respawnPlatformM) _respawnPlatformM = ev[i].Definition.safePlatformM;
            }
            if (_respawnClearM <= 0f) _respawnClearM = 15f;
            _distanceM = carDistanceM;
            Refresh(carDistanceM, true);
        }

        public void OnRunEnd(StageRunOutcome outcome)
        {
            if (!_enabled) return;
            var ev = _plan.Events;
            for (int i = 0; i < ev.Count; i++)
            {
                var e = ev[i];
                bool wasActive = e.Phase == StagePhase.Active;
                e.Phase = StagePhase.Ended;
                if (wasActive && Ended != null) Ended(e);
            }
            _enabled = false;       // a finished run publishes nothing; Begin() starts the next one
        }

        public void JumpTo(float distanceM)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (_plan == null || _plan.Events.Count == 0) return;
            var rec = _plan.Record; rec.debugJumped = true; _plan.Record = rec;
            _enabled = true;
            _distanceM = distanceM; _holdLeftS = 0f; _graceLeftS = 0f;
            var ev = _plan.Events;
            for (int i = 0; i < ev.Count; i++)
            {
                var e = ev[i];
                bool wasActive = e.Phase == StagePhase.Active;
                var old = e.Phase;
                var p = PhaseAt(e, distanceM);
                e.Phase = p;
                // Landing inside a warning window replays S1 now and S2 and S3 on the next Tick (catch-up); later phases have played them.
                e.SignalS2Fired = e.SignalS3Fired = p > StagePhase.Warning;
                if (p == StagePhase.Warning && old < StagePhase.Warning)
                {
                    if (Warned != null) Warned(e);
                    if (SignalFired != null) SignalFired(e, StageSignal.Announcer);
                }
                if (p == StagePhase.Active && !wasActive && Started != null) Started(e);
                else if (wasActive && p != StagePhase.Active && Ended != null) Ended(e);
            }
#endif
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>QA only: puts <paramref name="d"/> into the live plan at <paramref name="startM"/> (optionally replacing every other event) so a real run can be driven through exactly that event.</summary>
        public StageEventState DebugInject(StageEventDefinition d, float startM, bool clearOthers)
        {
            if (_plan == null || d == null) return null;
            if (clearOthers)
            {
                for (int i = 0; i < _plan.Events.Count; i++)
                    if (_plan.Events[i].Phase == StagePhase.Active) { _plan.Events[i].Phase = StagePhase.Ended; if (Ended != null) Ended(_plan.Events[i]); }
                _plan.Events.Clear();
            }
            var st = StageLadderPlanner.MakeState(_ladder, d, 9000 + _plan.Events.Count, startM, d.lengthM, 0, 777u);
            _plan.Events.Add(st);
            _plan.Events.Sort((x, y) => x.StartM.CompareTo(y.StartM));
            st.Phase = PhaseAt(st, _distanceM);
            _enabled = true;
            return st;
        }
#endif

        // ------------------------------------------------------------------ internals

        private static StagePhase PhaseAt(StageEventState e, float d)
        {
            if (d < e.LockM) return StagePhase.Planned;
            if (d < e.WarnM) return StagePhase.Locked;
            if (d < e.StartM) return StagePhase.Warning;
            if (d < e.EndM) return StagePhase.Active;
            if (d < e.RecoveryEndM) return StagePhase.Recovery;
            return StagePhase.Ended;
        }

        private void Refresh(float d, bool raise)
        {
            var ev = _plan.Events;
            for (int i = 0; i < ev.Count; i++)
            {
                var e = ev[i];
                var old = e.Phase;
                var now = PhaseAt(e, d);
                if (now == StagePhase.Active && old != StagePhase.Active && _holdLeftS > 0f) now = StagePhase.Warning;   // no effect starts right after a respawn
                if (now == old && now != StagePhase.Warning && now != StagePhase.Locked) continue;

                if (now < StagePhase.Warning) { e.SignalS2Fired = false; e.SignalS3Fired = false; }
                if (now != old)
                {
                    e.Phase = now;
                    if (raise && old < StagePhase.Warning && now >= StagePhase.Warning && now != StagePhase.Ended)
                    {
                        if (Warned != null) Warned(e);
                        if (SignalFired != null) SignalFired(e, StageSignal.Announcer);
                    }
                    if (now == StagePhase.Active && old != StagePhase.Active && raise && Started != null) Started(e);
                    if (old == StagePhase.Active && now != StagePhase.Active && raise && Ended != null) Ended(e);
                }
                if (raise && e.Phase == StagePhase.Warning)
                {
                    if (!e.SignalS2Fired && d >= e.SignM) { e.SignalS2Fired = true; if (SignalFired != null) SignalFired(e, StageSignal.Sign); }
                    if (!e.SignalS3Fired && d >= e.CueM) { e.SignalS3Fired = true; if (SignalFired != null) SignalFired(e, StageSignal.Cue); }
                }
            }
        }
    }
}
