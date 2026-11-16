using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoadBuilderGame
{
    [Serializable]
    public struct StageLadderSlot
    {
        public StageEventDefinition eventDefinition;
        public float nominalStartM;
        public float startRangeMinM;
        public float startRangeMaxM;
        [Tooltip("0 uses the event's own lengthM.")] public float lengthM;

        public bool IsFixedAnchor => startRangeMaxM <= startRangeMinM;
    }

    /// <summary>
    /// The ordered slots and gates of a stage. FixedSpine plays <see cref="slots"/>; Weighted builds repeating event units from <see cref="eventPool"/>.
    /// Spec: Docs/design/stage-events.md sections 3 and 9.2. Distances are metres of path, speeds m/s, times seconds of scaled time.
    /// </summary>
    [CreateAssetMenu(menuName = "Road Builder/Stage/Ladder Definition", fileName = "StageLadder")]
    public class StageLadderDefinition : ScriptableObject
    {
        public StageLadderMode ladderMode = StageLadderMode.FixedSpine;
        public int layoutVersion = 1;
        public EventCompatibilityMatrix matrix;
        public StageLadderSlot[] slots = new StageLadderSlot[0];
        [Tooltip("Events the Weighted ladder and the rejection fallback may pick from.")] public StageEventDefinition[] eventPool = new StageEventDefinition[0];

        [Header("Gates (metres)")]
        public float startPlatformM = 30f;
        public float noHazardUntilM = 190f;
        public float eaterWarnM = 940f;
        public float eaterStartM = 1000f;
        public float eaterTimeoutM = 1750f;
        public float finishMinM = 2750f;
        public float finishAfterBossM = 150f;
        [Tooltip("Smallest clear road before a Reward (ring gauntlet) that follows a threat.")] public float rewardMinGapM = 50f;
        [Tooltip("A modifier must end at least this far before its parent event's planned end.")] public float modifierEndMarginM = 200f;
        public float finishSlowmoScale = 0.65f;
        public float finishSlowmoS = 1.5f;

        [Header("Rules")]
        public int maxModifiers = 2;
        public int hardStagesBeforeBreather = 2;
        [Tooltip("Hazards are blocked this long (scaled seconds) after a respawn.")] public float respawnGraceS = 1.5f;
        [Tooltip("No new event effect starts this long (scaled seconds) after a respawn.")] public float respawnEffectHoldS = 3f;

        [Header("Signal distance model")]
        [Tooltip("Signal distances use this speed so a boosting car still gets the full lead: cap speed plus boost.")] public float signalSpeedFloorMps = 16f;
        [Tooltip("Eater warning window length: eaterStartM minus eaterWarnM.")] public float eaterWarnLeadM = 60f;

        [Header("Pace model (breather seconds to metres; brief speed curve)")]
        public float paceStartSpeedMps = 4.2f;
        public float paceGainMpsPerM = 0.02f;
        public float paceCapSpeedMps = 11.5f;

        [Header("Weighted ladder")]
        public float weightedLengthM = 6000f;
        public int weightedMaxEvents = 64;
        [Tooltip("Recovery after an event is drawn in whole 10 m steps between these.")] public float weightedRecoveryMinM = 100f;
        public float weightedRecoveryMaxM = 200f;
        public float weightedBreatherM = 200f;

        [Header("Endless ladder (StageLadderMode.Endless; blueprint A2 distance table)")]
        public float stageLengthM = 1000f;
        [Tooltip("A boss stage every this many metres (5000: stages 5, 10, 15 ...); 0 = only the row table. The boss tier is picked by distance (Hunter 3000+, Interceptor 5000+, Commander 8000+, Overlord 10000+).")] public float bossEveryM = 5000f;
        [Tooltip("One extra early boss stage (3 = the Hunter at 3000 m); 0 = none.")] public int bossFirstStage = 3;
        [Tooltip("Stages planned ahead (stage 0 is the training stage, no events).")] public int endlessStageCount = 40;
        [Tooltip("Rows for stages 1..10; any stage above uses the random row.")] public EndlessStageRow[] endlessStages = new EndlessStageRow[0];
        public EndlessStageRow endlessRandomRow;
        [Tooltip("Selection weights Easy, Medium, Hard, Extreme (40/35/20/5).")] public float[] tierWeights = { 40f, 35f, 20f, 5f };
        [Tooltip("At most this many mutations are active together (blueprint: max 2 modifiers).")] public int endlessModifierCap = 2;
        [Tooltip("From this distance a stacked second event is allowed, and activations are at least activationGapM apart.")] public float stackGapFromM = 5000f;
        public float activationGapM = 100f;
        [Tooltip("A stage whose primary costs at least this much counts as hard (two in a row are followed by a breather).")] public int endlessHardCost = 7;
        [Tooltip("A hazard family may appear at most familyMaxInWindow times in familyWindowStages stages.")] public int familyWindowStages = 4, familyMaxInWindow = 2;
        [Tooltip("Largest start offset into a stage (whole 10 m steps).")] public float endlessStartJitterM = 60f;
        [Header("Adaptive challenge (opt-in, Settings)")]
        public int adaptiveFailsToEase = 3;
        [Tooltip("Hard and Extreme tier weights are multiplied by this for a stage the player keeps failing.")] [Range(0f, 1f)] public float adaptiveHardMul = 0.35f;
        [Header("World tags")]
        public StageWorldTagEntry[] worldTags = new StageWorldTagEntry[0];
        [Tooltip("Tags of a built-in theme world (Country, Desert, CyberNight) that has no catalogue id.")] public string[] themeTagsCountry = new string[0], themeTagsDesert = new string[0], themeTagsCyber = new string[0];

        [Header("Debug (debug builds only)")]
        public float[] debugJumpPointsM = { 0f, 220f, 480f, 640f, 710f, 1000f, 1350f, 1800f, 2000f };

        public StagePace Pace => new StagePace(paceStartSpeedMps, paceGainMpsPerM, paceCapSpeedMps);

        /// <summary>Row for a stage number of the endless ladder (stages above the table use the random row).</summary>
        public EndlessStageRow RowFor(int stage)
        {
            if (endlessStages != null) for (int i = 0; i < endlessStages.Length; i++) if (endlessStages[i].stage == stage) return endlessStages[i];
            return endlessRandomRow;
        }

        /// <summary>The tags of a catalogue world (empty when the id is unknown). Allocation free.</summary>
        public string[] TagsForWorld(string worldId)
        {
            if (worldTags != null && !string.IsNullOrEmpty(worldId)) for (int i = 0; i < worldTags.Length; i++) if (worldTags[i].worldId == worldId) return worldTags[i].tags;
            return null;
        }

        public bool Validate(List<string> errors)
        {
            void Fail(string msg) { if (errors != null) errors.Add(name + ": " + msg); }
            bool bad = false;
            if (layoutVersion < 1) { Fail("layoutVersion below 1"); bad = true; }
            if (matrix == null) { Fail("matrix is missing (default deny would block everything)"); bad = true; }
            if (maxModifiers < 0 || maxModifiers > 2) { Fail("maxModifiers outside 0..2"); bad = true; }
            if (signalSpeedFloorMps < paceCapSpeedMps) { Fail("signalSpeedFloorMps below the pace cap speed"); bad = true; }
            if (paceStartSpeedMps <= 0f || paceCapSpeedMps < paceStartSpeedMps || paceGainMpsPerM < 0f) { Fail("pace model invalid"); bad = true; }
            if (eaterWarnM >= eaterStartM) { Fail("eaterWarnM must be below eaterStartM"); bad = true; }
            if (slots == null) { Fail("slots is null"); bad = true; }
            if (ladderMode == StageLadderMode.Endless)
            {
                if (tierWeights == null || tierWeights.Length != 4) { Fail("tierWeights needs 4 entries (Easy, Medium, Hard, Extreme)"); bad = true; }
                if (stageLengthM < 500f) { Fail("stageLengthM below 500"); bad = true; }
                if (endlessModifierCap < 1 || endlessModifierCap > 2) { Fail("endlessModifierCap outside 1..2"); bad = true; }
                if (eventPool == null || eventPool.Length == 0) { Fail("an endless ladder needs an event pool"); bad = true; }
            }
            var ids = new HashSet<string>();
            if (slots != null)
                for (int i = 0; i < slots.Length; i++)
                {
                    var s = slots[i];
                    if (s.eventDefinition == null) { Fail("slot " + i + " has no event"); bad = true; continue; }
                    if (!ids.Add(s.eventDefinition.id)) { Fail("slot " + i + " repeats " + s.eventDefinition.id + " (slice has no repeats)"); bad = true; }
                    if (s.startRangeMaxM < s.startRangeMinM) { Fail("slot " + i + " start range inverted"); bad = true; }
                    if (s.nominalStartM < s.startRangeMinM - 0.01f || s.nominalStartM > s.startRangeMaxM + 0.01f) { Fail("slot " + i + " nominal start outside its range"); bad = true; }
                    if (s.nominalStartM < noHazardUntilM && !s.eventDefinition.IsModifier && s.eventDefinition.role != StageEventRole.Reward) { Fail("slot " + i + " starts inside the no-hazard warm-up"); bad = true; }
                    var d = s.eventDefinition;
                    float len = s.lengthM > 0f ? s.lengthM : d.lengthM;
                    if (d.maxDistanceM > 0f && s.startRangeMaxM + len > d.maxDistanceM + 0.01f) { Fail("slot " + i + " can end after the event window max"); bad = true; }
                    if (s.startRangeMinM < d.minDistanceM - 0.01f) { Fail("slot " + i + " can start before the event window min"); bad = true; }
                }
            if (matrix != null && slots != null)
            {
                var all = new List<StageEventDefinition>();
                foreach (var s in slots) if (s.eventDefinition != null) all.Add(s.eventDefinition);
                if (eventPool != null) foreach (var e in eventPool) if (e != null && !all.Contains(e)) all.Add(e);
                var local = new List<string>();
                matrix.Validate(all, local);
                foreach (var m in local) { Fail(m); bad = true; }
            }
            return !bad;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            var errs = new List<string>();
            if (!Validate(errs)) foreach (var e in errs) Debug.LogWarning("[StageLadderDefinition] " + e, this);
        }
#endif
    }
}
