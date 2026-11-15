using System.Collections.Generic;
using UnityEngine;

namespace RoadBuilderGame
{
    /// <summary>
    /// One stage event (ice, meteors, mud, glass, rings, the Eater chase, the boss). Pure data, units in field names.
    /// Spec: Docs/design/stage-events.md section 9.1. Nothing here is read by vehicle, road or brush controllers.
    /// Prefab, sprite and audio references (signPrefab, banner icon, warn/cue sfx, SurfaceRuleSO) are added in the wiring phase.
    /// </summary>
    [CreateAssetMenu(menuName = "Road Builder/Stage/Event Definition", fileName = "EVT-NEW")]
    public class StageEventDefinition : ScriptableObject
    {
        public const float JumpHeightCapM = 8f;
        public const int HardCostThreshold = 6;

        [Header("Identity")]
        [Tooltip("Unique id, for example EVT-ICE. Never reused or renamed.")] public string id = "EVT-NEW";
        public StageEventRole role = StageEventRole.Primary;
        [Tooltip("Localisation key, no raw text.")] public string displayNameKey;
        [Range(1, 10)] public int difficultyCost = 3;

        [Header("Distance window (metres of path from the start line)")]
        public float minDistanceM = 0f;
        [Tooltip("End of the window. 0 means no limit (boss).")] public float maxDistanceM = 0f;
        [Range(60f, 750f)] public float lengthM = 120f;
        [Range(0f, 60f)] public float startJitterM = 40f;
        [Range(100f, 250f)] public float lockDistanceM = 150f;
        [Range(0f, 250f)] public float cooldownAfterM = 130f;
        [Range(8f, 20f)] public float minBreatherS = 12f;
        [Tooltip("Pace factor while this event runs (ice 1.15). Only used to convert the breather seconds to metres.")] [Range(0.5f, 2f)] public float paceSpeedFactor = 1f;
        [Range(0f, 10f)] public float weight = 1f;

        [Header("Warning signals (seconds of scaled time before the effect)")]
        [Range(4f, 6f)] public float announcerLeadS = 5f;
        [Range(2.5f, 4f)] public float signLeadS = 3f;
        [Range(1f, 1.5f)] public float cueLeadS = 1f;
        [Range(0.4f, 1f)] public float signalMinGapS = 0.6f;
        public string bannerKey;
        [Range(1f, 2.5f)] public float bannerSeconds = 1.5f;
        public string announcerCueId;
        [Tooltip("UITheme colour token name (a ColorToken), not a hex.")] public string tintColorToken;
        [Tooltip("Sound-only cue ids from voice-lines.md 3.3: S2 sign sting and S3 edge cue (sfx.<event>.sign, sfx.<event>.cue).")] public string warnSfxId;
        public string cueSfxId;
        [Tooltip("Surface numbers for a surface event (ice, mud, glass); null for the others.")] public SurfaceRuleSO surfaceRule;

        [Header("Stacking (mirrors the EventCompatibilityMatrix; validated against it)")]
        public string[] forbiddenWith = new string[0];
        [Tooltip("Events that must be Active when this one starts (glass requires the Eater).")] public string[] requires = new string[0];
        public string[] adjacentForbiddenWith = new string[0];
        [Range(0f, 600f)] public float adjacentMinGapM = 0f;
        [Range(0, 2)] public int modifierSlots = 0;

        [Header("Mutation effects (data-driven; one registered behaviour per event id applies and reverts them)")]
        public StageEffectSpec[] effects = new StageEffectSpec[0];
        [Tooltip("The rule in one line for the first-sighting preview (the 3-step learning rule).")] public string ruleText;
        [Tooltip("What the player can do about it. Every mutation has one.")] public string counterplayText;
        [Tooltip("Short sign word and icon key (S2 sign); the word is the non-colour shape channel.")] public string signText, iconKey;

        [Header("Endless ladder selection")]
        [Tooltip("World tags this event suits (moon, mars, earth, hills, synthwave, desert, volcano, candy, cave, snow ...). 'all' suits every world. Empty = never drawn by the endless ladder.")]
        public string[] suitableWorlds = new string[0];
        [Tooltip("Hazard family: the endless ladder allows a family at most twice in four stages.")] public string hazardFamily;
        [Tooltip("Stage-table tags: physics, movement, environment, economy, body, surface, sky ...")] public string[] ladderTags = new string[0];

        [Header("Spawning and terrain")]
        [Tooltip("Which hazard spawner this event enables (the director answers CanSpawnHazard with it).")] public string spawnerId;
        [Range(-20f, 20f)] public float terrainSlopeMinDeg = -10f;
        [Range(-20f, 20f)] public float terrainSlopeMaxDeg = 10f;
        [Range(0f, 8f)] public float terrainLongestGapM = 6f;
        [Tooltip("Global cap 8 m; the validator rejects more.")] [Range(0f, 8f)] public float terrainJumpHeightCapM = JumpHeightCapM;
        public StageCoinShape coinShape = StageCoinShape.Line;
        [Range(0, 16)] public int ringCount = 0;
        [Range(10f, 30f)] public float ringSpacingM = 16f;
        [Range(0f, 8f)] public float ringMaxHeightM = 6f;
        [Range(50f, 150f)] public float inkPickupSpacingM = 110f;
        [Range(1, 4)] public int variantCount = 1;

        [Header("Meteor strikes")]
        [Range(2f, 12f)] public float strikeIntervalSecMin = 3.5f;
        [Range(2f, 12f)] public float strikeIntervalSecMax = 5.5f;
        [Range(1, 5)] public int strikeMaxCount = 3;

        [Header("Respawn and safety")]
        [Range(10f, 25f)] public float clearOnRespawnM = 15f;
        [Range(0f, 10f)] public float safePlatformM = 0f;
        public bool noEat;
        public string telemetryId;

        public bool IsHard => difficultyCost >= HardCostThreshold;
        /// <summary>Selection tier from the difficulty cost: 1-5 Easy, 6-7 Medium, 8-9 Hard, 10 Extreme (blueprint weights 40/35/20/5).</summary>
        public StageTier Tier => difficultyCost >= 10 ? StageTier.Extreme : (difficultyCost >= 8 ? StageTier.Hard : (difficultyCost >= 6 ? StageTier.Medium : StageTier.Easy));
        public bool HasTag(string tag) { if (ladderTags != null) for (int i = 0; i < ladderTags.Length; i++) if (ladderTags[i] == tag) return true; return false; }
        public bool IsModifier => role == StageEventRole.Modifier;

        /// <summary>Pure-data check. Returns true when valid; messages are appended to <paramref name="errors"/> (may be null).</summary>
        public bool Validate(List<string> errors)
        {
            bool ok = true;
            void Fail(string msg) { ok = false; if (errors != null) errors.Add(name + " (" + id + "): " + msg); }

            if (string.IsNullOrEmpty(id)) Fail("id is empty");
            if (minDistanceM < 0f) Fail("minDistanceM below 0");
            if (maxDistanceM < 0f) Fail("maxDistanceM below 0");
            if (maxDistanceM > 0f && maxDistanceM < minDistanceM + lengthM - 0.001f) Fail("maxDistanceM must be at least minDistanceM + lengthM");
            if (lengthM < 60f || lengthM > 750f) Fail("lengthM outside 60..750");
            if (difficultyCost < 1 || difficultyCost > 10) Fail("difficultyCost outside 1..10");
            if (lockDistanceM < 100f || lockDistanceM > 250f) Fail("lockDistanceM outside 100..250");
            if (cooldownAfterM < 0f || cooldownAfterM > 250f) Fail("cooldownAfterM outside 0..250");
            if (minBreatherS < 8f || minBreatherS > 20f) Fail("minBreatherS outside 8..20");
            if (cueLeadS < 1f) Fail("cueLeadS below 1.0 s (S3 never below 1.0)");
            if (!(announcerLeadS > signLeadS && signLeadS > cueLeadS)) Fail("signal leads must satisfy announcerLeadS > signLeadS > cueLeadS");
            if (terrainJumpHeightCapM > JumpHeightCapM) Fail("terrainJumpHeightCapM above the global 8 m cap");
            if (terrainSlopeMinDeg > terrainSlopeMaxDeg) Fail("terrainSlopeMinDeg above terrainSlopeMaxDeg");
            if (terrainSlopeMinDeg < -20f || terrainSlopeMaxDeg > 20f) Fail("slope outside -20..+20 deg");
            if (terrainLongestGapM > 8f) Fail("terrainLongestGapM above 8 m");
            if (ringMaxHeightM > 8f) Fail("ringMaxHeightM above 8 m");
            if (strikeIntervalSecMax < strikeIntervalSecMin) Fail("strikeIntervalSecMax below strikeIntervalSecMin");
            if (variantCount < 1 || variantCount > 4) Fail("variantCount outside 1..4");
            if (modifierSlots < 0 || modifierSlots > 2) Fail("modifierSlots outside 0..2");
            if (role == StageEventRole.Modifier && modifierSlots < 1) Fail("a Modifier needs modifierSlots of at least 1");
            if (role != StageEventRole.Modifier && modifierSlots != 0) Fail("only a Modifier may use modifierSlots");
            if (role == StageEventRole.Modifier && (requires == null || requires.Length == 0)) Fail("a Modifier needs a parent in requires");
            if (role == StageEventRole.Reward && ringCount > 0 && (ringCount < 8 || ringCount > 16) && id == "EVT-RINGS") Fail("a ring gauntlet needs 8..16 rings");
            if (weight < 0f) Fail("weight below 0");
            if (forbiddenWith != null && id != null) foreach (var f in forbiddenWith) if (f == id) Fail("forbiddenWith lists the event itself");
            return ok;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            var errs = new List<string>();
            if (!Validate(errs)) foreach (var e in errs) Debug.LogWarning("[StageEventDefinition] " + e, this);
        }
#endif
    }
}
