using System;

namespace RoadBuilderGame
{
    public enum StageEventRole { Primary, Modifier, Monster, Boss, Reward }
    public enum StagePhase { Planned, Locked, Warning, Active, Recovery, Ended }
    public enum StageLadderMode { FixedSpine, Weighted }
    public enum StageCompatRule { Forbid, Allow, Conditional }
    public enum StageCoinShape { Line, Lanes, LowRoute, AboveNextPiece, Rings }
    public enum StageSignal { Announcer, Sign, Cue }          // S1, S2, S3 (stage-events.md section 4)
    public enum StageRunOutcome { Finished, Died, Quit }

    /// <summary>What the director needs from a run. Stored by SaveService at run start (the director itself never saves).</summary>
    [Serializable]
    public struct StageRunRecord
    {
        /// <summary>Generate with <see cref="StageSeed.MakeRunSeed"/> (raw shifted left 7).</summary>
        public uint runSeed;
        public int modeId;
        public int layoutVersion;
        public bool debugJumped;
    }

    public static class StageSeed
    {
        public const uint FallbackRunSeed = 128u;

        /// <summary>raw << 7 leaves the low 7 bits free, so with stageIndex + modeId &lt; 128 no two (run, slot) seeds collide.</summary>
        public static uint MakeRunSeed(uint raw) { return unchecked(raw << 7); }

        /// <summary>Per-slot seed exactly as the brief requires: unchecked(runSeed + stageIndex + modeId).</summary>
        public static uint SlotSeed(uint runSeed, int stageIndex, int modeId)
        {
            return unchecked(runSeed + (uint)stageIndex + (uint)modeId);
        }
    }

    /// <summary>One planned (or live) event. Created once in Begin(); Tick only changes <see cref="Phase"/>.</summary>
    public sealed class StageEventState
    {
        public StageEventDefinition Definition { get; internal set; }
        public int SlotIndex { get; internal set; }
        public float StartM { get; internal set; }
        public float EndM { get; internal set; }
        public int Variant { get; internal set; }
        /// <summary>Draw 4 of the slot RNG, handed to the event spawner.</summary>
        public uint TerrainSeed { get; internal set; }
        public StagePhase Phase { get; internal set; }
        /// <summary>Distance at which the choice is committed (geometry pre-build, spawn block).</summary>
        public float LockM { get; internal set; }
        /// <summary>Distances of the three time-based signals S1 (announcer), S2 (sign), S3 (cue); the warning phase starts at S1.</summary>
        public float WarnM { get; internal set; }
        public float SignM { get; internal set; }
        public float CueM { get; internal set; }
        public float RecoveryEndM { get; internal set; }
        public bool SignalS2Fired, SignalS3Fired;
        public bool IsNone => Definition == null;
        public string Id => Definition != null ? Definition.id : string.Empty;

        public static readonly StageEventState None = new StageEventState { Phase = StagePhase.Ended };
    }

    /// <summary>Log entry such as event_rejected or event_dropped_breather. Kept on the plan, never thrown.</summary>
    public readonly struct StageLogEntry
    {
        public readonly string Code;       // event_rejected, event_dropped_breather, ...
        public readonly int Slot;
        public readonly string EventId;
        public readonly string Reason;
        public StageLogEntry(string code, int slot, string eventId, string reason) { Code = code; Slot = slot; EventId = eventId; Reason = reason; }
        public override string ToString() { return Code + " {slot=" + Slot + ", id=" + EventId + ", reason=" + Reason + "}"; }
    }
}
