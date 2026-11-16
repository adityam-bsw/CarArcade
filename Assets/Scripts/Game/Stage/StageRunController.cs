using System;
using System.Collections.Generic;

namespace RoadBuilderGame
{
    /// <summary>What an event does to the world while it is Active. One small class per kind, registered by event id; the director never knows them.</summary>
    public interface IStageEventBehaviour
    {
        void OnStart(StageEventState state);
        void OnEnd(StageEventState state);
        /// <summary>The car respawned at <paramref name="distanceM"/> while this event was Active.</summary>
        void OnRespawn(StageEventState state, float distanceM);
    }

    /// <summary>An event behaviour that also needs a per-frame tick while its event is Active (wind gusts, the road-tax checkpoint). Zero allocation.</summary>
    public interface IStageTickBehaviour
    {
        void Tick(float scaledDt, float distanceM);
    }

    /// <summary>
    /// Owns the <see cref="StageDirector"/> for a real run and connects it to the game: ticks it, routes Started and Ended to the registered behaviour modules,
    /// forwards the three warning signals to the presenter, and applies the pause and resume rule. Plain class created by the manager, no scene access.
    /// Behind <see cref="GameplayTuning.stageEvents"/>: when the switch says no, <see cref="Active"/> is false and nothing else runs.
    /// </summary>
    public sealed class StageRunController
    {
        /// <summary>Stable id of the Countryside slice mode in the run record (never reuse or renumber). Used until a ModeDefinition exists.</summary>
        public const int ModeIdCountryside = 1;
        /// <summary>Stable id of the endless weighted ladder mode (other worlds, and the slice after its finish gate).</summary>
        public const int ModeIdEndless = 2;
        /// <summary>After the app resumes from a pause, hazards stay out this long (brief F0).</summary>
        public const float ResumeGraceS = 0.5f;

        private StageLadderDefinition _ladder;
        private readonly StageDirector _director;
        private readonly List<IStageTickBehaviour> _ticking = new List<IStageTickBehaviour>(8);
        private readonly Dictionary<string, IStageEventBehaviour> _behaviours = new Dictionary<string, IStageEventBehaviour>();
        private float _resumeGraceLeftS;
        private bool _running;

        /// <summary>S1, S2 and S3 of an event that has a registered behaviour (events without one are schedule-only and stay silent).</summary>
        public event Action<StageEventState, StageSignal> Signal;
        public event Action<StageEventState> EventStarted, EventEnded;

        public StageRunController(StageLadderDefinition ladder)
        {
            _ladder = ladder;
            _director = new StageDirector(ladder);
            _director.SignalFired += OnSignal;
            _director.Started += OnStarted;
            _director.Ended += OnEnded;
        }

        public StageDirector Director => _director;
        public StageLadderDefinition Ladder => _ladder;
        /// <summary>A run is being driven by the director.</summary>
        public bool Active => _running && _director.Enabled;
        public bool HasData => _ladder != null && (IsEndless(_ladder) ? (_ladder.eventPool != null && _ladder.eventPool.Length > 0) : (_ladder.slots != null && _ladder.slots.Length > 0));
        public static bool IsEndless(StageLadderDefinition l) { return l != null && l.ladderMode == StageLadderMode.Endless; }
        /// <summary>The run is on the endless ladder (a non-slice world, or the slice after its finish gate).</summary>
        public bool OnEndless => IsEndless(_ladder);
        public float DistanceM { get; private set; }

        public void Register(string eventId, IStageEventBehaviour behaviour) { if (!string.IsNullOrEmpty(eventId) && behaviour != null) _behaviours[eventId] = behaviour; }
        public bool HasBehaviour(string eventId) { return eventId != null && _behaviours.ContainsKey(eventId); }

        /// <summary>The switch from <see cref="GameplayTuning"/>, resolved for the world on screen.</summary>
        public static bool ShouldRun(StageEventsSwitch mode, bool isCountryside)
        {
            return mode == StageEventsSwitch.Always || (mode == StageEventsSwitch.CountrysideOnly && isCountryside);
        }

        /// <summary>Raw seed for a new run from a caller-supplied entropy value (the clock lives with the caller; this stays deterministic). Never returns a zero seed.</summary>
        public static StageRunRecord NewRecord(ulong entropy, int layoutVersion, int modeId = ModeIdCountryside)
        {
            uint raw = unchecked((uint)(entropy ^ (entropy >> 32)));
            raw &= 0x01FFFFFFu;
            if (raw == 0u) raw = 1u;
            return new StageRunRecord { runSeed = StageSeed.MakeRunSeed(raw), modeId = modeId, layoutVersion = layoutVersion, debugJumped = false };
        }

        /// <summary>Makes <paramref name="ladder"/> the ladder of the next Begin (slice or endless). Does not touch a run in progress.</summary>
        public void UseLadder(StageLadderDefinition ladder)
        {
            if (_running) End(StageRunOutcome.Quit);
            _ladder = ladder;
            _director.SetLadder(ladder);
        }

        /// <summary>A fresh run on the endless ladder for a world described by <paramref name="ctx"/>.</summary>
        public void BeginEndless(StageRunRecord record, StageWorldContext ctx)
        {
            if (_running) End(StageRunOutcome.Quit);
            _resumeGraceLeftS = 0f; _ticking.Clear(); DistanceM = 0f;
            if (!HasData) { _running = false; return; }
            _director.BeginEndless(_ladder, record, 0f, ctx);
            _running = _director.Enabled;
        }

        /// <summary>The slice was finished: keep going on <paramref name="endless"/> from <paramref name="fromM"/> (the finish gate). Same run, same seed.</summary>
        public bool ContinueEndless(StageLadderDefinition endless, float fromM, StageWorldContext ctx)
        {
            if (!_running || endless == null) return false;
            var rec = _director.Record; rec.modeId = ModeIdEndless;
            _ladder = endless;
            _director.BeginEndless(endless, rec, fromM, ctx);
            _running = _director.Enabled;
            return _running;
        }

        public void Begin(StageRunRecord record)
        {
            if (_running) End(StageRunOutcome.Quit);
            _resumeGraceLeftS = 0f; _ticking.Clear(); DistanceM = 0f;
            if (!HasData) { _running = false; return; }
            _director.Begin(record);
            _running = _director.Enabled;
        }

        /// <summary>Distance driven and scaled time only. Zero allocation.</summary>
        public void Tick(float distanceM, float speedMps, float scaledDt)
        {
            if (!_running) return;
            if (_resumeGraceLeftS > 0f && scaledDt > 0f) _resumeGraceLeftS -= scaledDt;
            DistanceM = distanceM;
            _director.Tick(distanceM, speedMps, scaledDt);
            for (int i = 0; i < _ticking.Count; i++) _ticking[i].Tick(scaledDt, distanceM);
        }

        /// <summary>Hazard gate for spawners: the director's answer plus the 0.5 s resume grace and the respawn clear zone.</summary>
        public bool CanSpawnHazard(string hazardId)
        {
            return _running && _resumeGraceLeftS <= 0f && _director.CanSpawnHazard(hazardId);
        }

        /// <summary>The 0.5 s resume grace or the director's respawn grace is running: no hazard may deal damage.</summary>
        public bool HazardGraceActive => _resumeGraceLeftS > 0f || _director.HazardGraceLeftS > 0f;

        public bool CanSpawnHazardAt(string hazardId, float hazardDistanceM)
        {
            return _running && _resumeGraceLeftS <= 0f && _director.CanSpawnHazardAt(hazardId, hazardDistanceM);
        }

        /// <summary>Heart lost, still alive: the car is back on the safety platform at <paramref name="distanceM"/>. The ladder does not rewind.</summary>
        public void OnRespawn(float distanceM)
        {
            if (!_running) return;
            _director.OnRespawn(distanceM);
            var ev = _director.Plan.Events;
            for (int i = 0; i < ev.Count; i++)
                if (ev[i].Phase == StagePhase.Active && _behaviours.TryGetValue(ev[i].Id, out var b)) b.OnRespawn(ev[i], distanceM);
        }

        /// <summary>App backgrounded or the game paused: nothing ticks (scaled time stands still); the resume gives a short hazard-free grace.</summary>
        public void OnPause(bool paused)
        {
            if (!_running) return;
            if (!paused) _resumeGraceLeftS = ResumeGraceS;
        }

        /// <summary>The run is over (Died, Finished or Quit). Active events end, so zones and windows are cleared.</summary>
        public void End(StageRunOutcome outcome)
        {
            if (!_running) return;
            _director.OnRunEnd(outcome);   // raises Ended for every Active event
            _running = false; _ticking.Clear();
        }

        /// <summary>The game cleared every pickup (QA jump, revive): active behaviours that had placed pickups put them back.</summary>
        public void NotifyPickupsCleared(float distanceM)
        {
            for (int i = 0; i < _ticking.Count; i++) if (_ticking[i] is IStagePickupsAware a) a.OnPickupsCleared(distanceM);
        }

        public void JumpTo(float distanceM)
        {
            if (!_running) return;
            _director.JumpTo(distanceM);
        }

        // ------------------------------------------------------------------ director events

        private void OnSignal(StageEventState e, StageSignal s)
        {
            if (!_behaviours.ContainsKey(e.Id)) return;
            Signal?.Invoke(e, s);
        }

        private void OnStarted(StageEventState e)
        {
            if (_behaviours.TryGetValue(e.Id, out var b))
            {
                if (b is IStageTickBehaviour t && !_ticking.Contains(t)) _ticking.Add(t);
                b.OnStart(e); EventStarted?.Invoke(e);
            }
        }

        private void OnEnded(StageEventState e)
        {
            if (_behaviours.TryGetValue(e.Id, out var b))
            {
                if (b is IStageTickBehaviour t) _ticking.Remove(t);
                b.OnEnd(e); EventEnded?.Invoke(e);
            }
        }
    }
}
