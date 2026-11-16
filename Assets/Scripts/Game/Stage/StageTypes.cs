using System;
using UnityEngine;

namespace RoadBuilderGame
{
    public enum StageEventRole { Primary, Modifier, Monster, Boss, Reward }
    public enum StagePhase { Planned, Locked, Warning, Active, Recovery, Ended }
    public enum StageLadderMode { FixedSpine, Weighted, Endless }
    public enum StageTier { Easy, Medium, Hard, Extreme }
    public enum StageCompatRule { Forbid, Allow, Conditional }
    public enum StageCoinShape { Line, Lanes, LowRoute, AboveNextPiece, Rings }
    public enum StageSignal { Announcer, Sign, Cue }          // S1, S2, S3 (stage-events.md section 4)
    public enum StageRunOutcome { Finished, Died, Quit }

    /// <summary>What a mutation event changes about the run. One entry per effect in <see cref="StageEventDefinition.effects"/>; the numbers live in the spec, not in code.</summary>
    public enum StageEffectKind
    {
        None,
        GravityMul,       // a: multiplier on the vehicle's gravity
        SpeedMul,         // a: multiplier on the forward target speed
        InkRegenMul,      // a: multiplier on ink regeneration
        InkCapacityMul,   // a: multiplier on the tank size
        BoostDrainMul,    // a: how many times faster a boost burns
        CoinVacuum,       // a: radius m, b: drift speed m/s
        WindSteady,       // a: acceleration m/s^2 (sign picked from the seed), b: modulation period s
        WindGust,         // a: peak acceleration m/s^2, b: mean seconds between gusts (each gust lasts 1.2 s)
        RoadTax,          // a: coins per checkpoint, b: checkpoint spacing m
        Fragile,          // a: fraction of the reference impact that already cracks the car, b: reference impact m/s
        VehicleScale,     // a: uniform scale
        // ---- batch A2 (appended: serialized ints of the kinds above never change)
        GravityPulse,     // a: gravity multiplier during a window, b: window seconds (calm gap is 2x the window)
        Whiteout,         // a: visibility m, b: peak fog alpha
        MotorReverse,     // a: reverse motor factor (negative), b: burst seconds (gap 1.8x)
        MirrorInput,      // drawing input mirrors horizontally about the screen centre
        FlappyTunnel,     // a: gate spacing m, b: gap height m (short hop tap)
        RoadCollapse,     // a: seconds until the road behind the car disappears
        IceAge,           // a: friction multiplier on every road
        MagneticPylons,   // a: peak pull m/s^2, b: pull radius m
        ElasticRoad,      // a: bounciness added to the road material
        RoadFade,         // a: seconds a drawn road stays visible, b: ghost marker spacing m (0 = none)
        GhostDouble,      // a: seconds the ghost lags behind
        FallingPlatforms, // a: seconds until a touched platform drops
        GravityGates,     // a: gravity multiplier in the reversed zone, b: gate spacing m
        TrafficConvoy,    // a: traffic speed m/s, b: seconds between vehicles
        RollingBoulder,   // a: roll speed m/s, b: seconds between boulders
        ElectricFences,   // a: pulse cycle s, b: safe window s
        FloodSurge,       // a: metres per step, b: seconds per step
        LavaChase,        // a: rise m/s, b: start depth below the car m
        WindTunnels,      // a: wind m/s^2, b: gate spacing m
        StickyRoad,       // a: friction multiplier, b: speed multiplier
        RoadLaser,        // a: strip width m, b: seconds between scans
        FallingStars,     // a: platform life s, b: seconds between stars
        VehicleSwap,      // a: metres between swaps
        RhythmGates,      // a: beats per minute, b: gate spacing m
        ChaosFinale       // a: seconds between elite strike fans; the other effects of the event are the modifier pool (two chosen by variant)
    }

    [Serializable]
    public struct StageEffectSpec
    {
        public StageEffectKind kind;
        public float a;
        public float b;
        public StageEffectSpec(StageEffectKind kind, float a, float b = 0f) { this.kind = kind; this.a = a; this.b = b; }
    }

    /// <summary>World tags of one catalogue world id (for the suitable-worlds filter of the endless ladder).</summary>
    [Serializable]
    public struct StageWorldTagEntry { public string worldId; public string[] tags; }

    /// <summary>One distance-table row of the endless ladder (blueprint A2). Stage k covers [k x stageLengthM, (k+1) x stageLengthM).</summary>
    [Serializable]
    public struct EndlessStageRow
    {
        public int stage;
        public string label;
        [Tooltip("Required ladderTag of the primary event (empty = any).")] public string tag;
        [Tooltip("A fixed event (the Road-Eater at stage 1, RO-B0 at stage 8). Empty = drawn.")] public StageEventDefinition fixedEvent;
        [Tooltip("Lowest and highest tier the primary may be drawn from.")] public StageTier minTier, maxTier;
        [Tooltip("1 = one event, 2 = a stacked second compatible mutation (the blueprint 'two compatible modifiers').")] [Range(1, 2)] public int concurrent;
    }

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

        /// <summary>A state for a single event outside a plan (behaviour tests, the QA probe).</summary>
        public static StageEventState For(StageEventDefinition d, float startM = 0f, float endM = 0f, uint terrainSeed = 1u)
        {
            return new StageEventState { Definition = d, StartM = startM, EndM = endM > startM ? endM : startM + (d != null ? d.lengthM : 0f), TerrainSeed = terrainSeed, Phase = StagePhase.Active };
        }

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
