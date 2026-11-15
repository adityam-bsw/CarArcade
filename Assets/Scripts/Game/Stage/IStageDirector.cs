using System;

namespace RoadBuilderGame
{
    /// <summary>
    /// Publishes the stage ladder as data and answers "may this hazard spawn". No reference to any Mode, vehicle or road class; the caller passes
    /// distance and scaled time in. Spec: Docs/design/stage-events.md section 2.
    /// </summary>
    public interface IStageDirector
    {
        /// <summary>The next event not yet Active (never null after Begin; the none-state when the stage has no events).</summary>
        StageEventState NextEvent { get; }
        /// <summary>The primary event that is Active now, or null in a breather.</summary>
        StageEventState ActiveEvent { get; }
        int ActiveModifierCount { get; }
        bool Breather { get; }
        bool Enabled { get; }

        bool CanSpawnHazard(string hazardId);
        void Begin(StageRunRecord run);
        /// <summary>O(1) per call, no allocation. Distance driven and scaled time only: pausing means not calling it.</summary>
        void Tick(float carDistanceM, float carSpeedMps, float scaledDt);
        void OnRespawn(float carDistanceM);
        void OnRunEnd(StageRunOutcome outcome);
        /// <summary>Debug builds only.</summary>
        void JumpTo(float distanceM);

        /// <summary>Warned = S1 (warning phase begins). Started and Ended bracket the Active phase. SignalFired carries S1, S2 and S3 in order.</summary>
        event Action<StageEventState> Warned, Started, Ended;
        event Action<StageEventState, StageSignal> SignalFired;
    }
}
