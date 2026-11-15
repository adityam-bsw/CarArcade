using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace RoadBuilderGame.Editor
{
    /// <summary>
    /// Creates (or refreshes in place) the vertical-slice stage assets from the spec tables: three SurfaceRuleSO, seven StageEventDefinition
    /// (the five slice events plus the two schedule anchors MON-EATER and BOS-ROB0 that phase B2 gives behaviour), the EventCompatibilityMatrix
    /// (21 pairs of stage-events.md section 5) and the StageLadder (section 3.1), then runs every Validate. Safe to re-run: existing assets keep their GUIDs.
    /// Menu: Road Builder / Stage / Build Slice Assets. Spec: Docs/design/stage-events.md sections 3 and 9, hazards.md section 3.
    /// </summary>
    public static class StageSliceBuilder
    {
        public const string Dir = "Assets/Resources/Stage";
        public const string LadderPath = Dir + "/StageLadder.asset";
        public const string MatrixPath = Dir + "/EventCompatibilityMatrix.asset";

        [MenuItem("Road Builder/Stage/Build Slice Assets")]
        public static void BuildMenu()
        {
            var errors = Build();
            AssetDatabase.SaveAssets();
            if (errors.Count == 0) Debug.Log("[StageSliceBuilder] Slice assets built and valid in " + Dir);
            else foreach (var e in errors) Debug.LogError("[StageSliceBuilder] " + e);
        }

        [MenuItem("Road Builder/Stage/Validate Slice Assets")]
        public static void ValidateMenu()
        {
            var errors = ValidateAll();
            if (errors.Count == 0) Debug.Log("[StageSliceBuilder] Slice assets are valid");
            else foreach (var e in errors) Debug.LogError("[StageSliceBuilder] " + e);
        }

        /// <summary>Builds everything and returns the validation errors (empty when valid).</summary>
        public static List<string> Build()
        {
            Directory.CreateDirectory(Dir);
            var ice = Surface("SRF-ICE", "surface-ice", 0.35f, 1.15f, 1.30f, 1.50f, "ZoneIce", s => { });
            var mud = Surface("SRF-MUD", "surface-mud", 1.60f, 0.85f, 0.75f, 1.30f, "ZoneMud", s => { });
            var glass = Surface("SRF-GLS", "surface-glass", 0.50f, 1.12f, 1.30f, 1.40f, "ZoneGlass", s => { });

            var evIce = Event("EVT-ICE", e =>
            {
                e.role = StageEventRole.Primary; e.difficultyCost = 4; e.minDistanceM = 190; e.maxDistanceM = 390; e.lengthM = 140; e.cooldownAfterM = 160;
                e.paceSpeedFactor = 1.15f; e.spawnerId = "hz-ice"; e.surfaceRule = ice;
                e.terrainSlopeMinDeg = -12; e.terrainSlopeMaxDeg = 15; e.terrainLongestGapM = 6; e.coinShape = StageCoinShape.Line; e.ringCount = 1; e.ringMaxHeightM = 6; e.variantCount = 2;
                Cues(e, "ice", StageCues.S1Ice, "ZoneIce");
                e.forbiddenWith = new[] { "EVT-MUD", "EVT-METEOR", "EVT-GLASS", "EVT-RINGS", "MON-EATER", "BOS-ROB0" };
            });
            var evMeteor = Event("EVT-METEOR", e =>
            {
                e.role = StageEventRole.Primary; e.difficultyCost = 2; e.minDistanceM = 490; e.maxDistanceM = 640; e.lengthM = 100; e.cooldownAfterM = 130;
                e.spawnerId = "hz-meteor";
                e.terrainSlopeMinDeg = -10; e.terrainSlopeMaxDeg = 10; e.terrainLongestGapM = 6; e.coinShape = StageCoinShape.Lanes; e.ringCount = 0; e.variantCount = 1;
                e.strikeIntervalSecMin = 3.5f; e.strikeIntervalSecMax = 5.5f; e.strikeMaxCount = 3;
                Cues(e, "meteor", StageCues.S1Meteor, "ZoneMeteor");
                e.forbiddenWith = new[] { "EVT-ICE", "EVT-MUD", "EVT-GLASS", "EVT-RINGS", "MON-EATER", "BOS-ROB0" };
            });
            var evMud = Event("EVT-MUD", e =>
            {
                e.role = StageEventRole.Primary; e.difficultyCost = 3; e.minDistanceM = 720; e.maxDistanceM = 870; e.lengthM = 120; e.cooldownAfterM = 130;
                e.spawnerId = "hz-mud"; e.surfaceRule = mud;
                e.terrainSlopeMinDeg = 0; e.terrainSlopeMaxDeg = 14; e.terrainLongestGapM = 0; e.coinShape = StageCoinShape.LowRoute; e.ringCount = 1; e.ringMaxHeightM = 4; e.variantCount = 2;
                Cues(e, "mud", StageCues.S1Mud, "ZoneMud");
                e.forbiddenWith = new[] { "EVT-ICE", "EVT-METEOR", "EVT-GLASS", "EVT-RINGS", "MON-EATER", "BOS-ROB0" };
                e.adjacentForbiddenWith = new[] { "EVT-GLASS" }; e.adjacentMinGapM = 300;
            });
            var evEater = Event("MON-EATER", e =>
            {   // schedule anchor only in B1: the director plans and signals it, no behaviour is registered, so nothing is shown or done until phase B2
                e.role = StageEventRole.Monster; e.difficultyCost = 7; e.minDistanceM = 1000; e.maxDistanceM = 1750; e.lengthM = 750; e.startJitterM = 0; e.cooldownAfterM = 100;
                e.spawnerId = "hz-eater"; e.terrainSlopeMinDeg = -12; e.terrainSlopeMaxDeg = 12; e.terrainLongestGapM = 6; e.coinShape = StageCoinShape.Line; e.variantCount = 1;
                Cues(e, "eater", StageCues.S1Eater, "ZoneMeteor");
                e.forbiddenWith = new[] { "EVT-ICE", "EVT-MUD", "EVT-METEOR", "EVT-RINGS", "BOS-ROB0" };
            });
            var evGlass = Event("EVT-GLASS", e =>
            {
                e.role = StageEventRole.Modifier; e.difficultyCost = 6; e.minDistanceM = 1250; e.maxDistanceM = 1600; e.lengthM = 160; e.cooldownAfterM = 0;
                e.requires = new[] { "MON-EATER" }; e.modifierSlots = 1; e.safePlatformM = 6; e.noEat = true; e.surfaceRule = glass; e.spawnerId = "hz-glass";
                e.terrainSlopeMinDeg = -12; e.terrainSlopeMaxDeg = 12; e.terrainLongestGapM = 0; e.coinShape = StageCoinShape.AboveNextPiece; e.ringCount = 1; e.ringMaxHeightM = 5;
                e.inkPickupSpacingM = 90; e.variantCount = 1;
                Cues(e, "glass", StageCues.S1Glass, "ZoneGlass");
                e.forbiddenWith = new[] { "EVT-ICE", "EVT-MUD", "EVT-METEOR", "EVT-RINGS", "BOS-ROB0" };
                e.adjacentForbiddenWith = new[] { "EVT-MUD" }; e.adjacentMinGapM = 300;
            });
            var evRings = Event("EVT-RINGS", e =>
            {
                e.role = StageEventRole.Reward; e.difficultyCost = 3; e.minDistanceM = 1790; e.maxDistanceM = 1990; e.lengthM = 180; e.cooldownAfterM = 0;
                e.terrainSlopeMinDeg = -8; e.terrainSlopeMaxDeg = 12; e.terrainLongestGapM = 6; e.coinShape = StageCoinShape.Rings; e.ringCount = 10; e.ringSpacingM = 16; e.ringMaxHeightM = 6; e.variantCount = 2;
                Cues(e, "rings", StageCues.S1Rings, "ZoneRings");
                e.forbiddenWith = new[] { "EVT-ICE", "EVT-MUD", "EVT-METEOR", "EVT-GLASS", "MON-EATER", "BOS-ROB0" };
            });
            var evBoss = Event("BOS-ROB0", e =>
            {   // schedule anchor only in B1 (RO-B0 is phase B2)
                e.role = StageEventRole.Boss; e.difficultyCost = 8; e.minDistanceM = 2000; e.maxDistanceM = 0; e.lengthM = 600; e.startJitterM = 0; e.cooldownAfterM = 150;
                e.spawnerId = "hz-boss"; e.terrainSlopeMinDeg = -8; e.terrainSlopeMaxDeg = 8; e.terrainLongestGapM = 6; e.variantCount = 1;
                Cues(e, "boss", StageCues.S1Boss, "ZoneMeteor");
                e.forbiddenWith = new[] { "EVT-ICE", "EVT-MUD", "EVT-METEOR", "EVT-GLASS", "EVT-RINGS", "MON-EATER" };
            });

            var matrix = LoadOrCreate<EventCompatibilityMatrix>(MatrixPath);
            matrix.pairs = new[]
            {
                P("EVT-ICE", "EVT-MUD", "X1"), P("EVT-ICE", "EVT-METEOR", "X2"), P("EVT-ICE", "EVT-GLASS", "X3"), P("EVT-ICE", "EVT-RINGS", "X7"),
                P("EVT-ICE", "MON-EATER", "X4", StageCompatRule.Forbid, 0f, "phase:Warn"), P("EVT-ICE", "BOS-ROB0", "X6"),
                P("EVT-MUD", "EVT-METEOR", "X2"), P("EVT-MUD", "EVT-GLASS", "X8", StageCompatRule.Forbid, 300f), P("EVT-MUD", "EVT-RINGS", "X7"),
                P("EVT-MUD", "MON-EATER", "C1", StageCompatRule.Conditional, 0f, "C1:disabled"), P("EVT-MUD", "BOS-ROB0", "X6"),
                P("EVT-METEOR", "EVT-GLASS", "X2"), P("EVT-METEOR", "EVT-RINGS", "X7"), P("EVT-METEOR", "MON-EATER", "X5"), P("EVT-METEOR", "BOS-ROB0", "X5"),
                P("EVT-GLASS", "EVT-RINGS", "X7"), P("EVT-GLASS", "MON-EATER", "OK", StageCompatRule.Allow, 0f, "phase:Track"), P("EVT-GLASS", "BOS-ROB0", "X6"),
                P("EVT-RINGS", "MON-EATER", "X9"), P("EVT-RINGS", "BOS-ROB0", "X9"), P("MON-EATER", "BOS-ROB0", "X10"),
            };
            EditorUtility.SetDirty(matrix);

            var ladder = LoadOrCreate<StageLadderDefinition>(LadderPath);
            ladder.ladderMode = StageLadderMode.FixedSpine;
            ladder.layoutVersion = 1;
            ladder.matrix = matrix;
            ladder.slots = new[]
            {
                Slot(evIce, 205, 190, 220), Slot(evMeteor, 505, 490, 520), Slot(evMud, 735, 720, 750), Slot(evEater, 1000, 1000, 1000),
                Slot(evGlass, 1350, 1310, 1390), Slot(evRings, 1800, 1790, 1810), Slot(evBoss, 2000, 2000, 2000),
            };
            ladder.eventPool = new[] { evIce, evMeteor, evMud, evEater, evGlass, evRings, evBoss };
            ladder.startPlatformM = 30; ladder.noHazardUntilM = 190; ladder.eaterWarnM = 940; ladder.eaterStartM = 1000; ladder.eaterTimeoutM = 1750;
            ladder.finishMinM = 2750; ladder.finishAfterBossM = 150;
            ladder.debugJumpPointsM = new[] { 0f, 220f, 480f, 640f, 710f, 1000f, 1350f, 1800f, 2000f };
            EditorUtility.SetDirty(ladder);

            AssetDatabase.SaveAssets();
            return ValidateAll();
        }

        /// <summary>Runs every Validate on the saved slice assets.</summary>
        public static List<string> ValidateAll()
        {
            var errors = new List<string>();
            var ladder = AssetDatabase.LoadAssetAtPath<StageLadderDefinition>(LadderPath);
            if (ladder == null) { errors.Add("No ladder at " + LadderPath + " (run Road Builder / Stage / Build Slice Assets)"); return errors; }
            ladder.Validate(errors);
            foreach (var g in AssetDatabase.FindAssets("t:StageEventDefinition", new[] { Dir }))
                AssetDatabase.LoadAssetAtPath<StageEventDefinition>(AssetDatabase.GUIDToAssetPath(g)).Validate(errors);
            foreach (var g in AssetDatabase.FindAssets("t:SurfaceRuleSO", new[] { Dir }))
                AssetDatabase.LoadAssetAtPath<SurfaceRuleSO>(AssetDatabase.GUIDToAssetPath(g)).Validate(errors);
            return errors;
        }

        // ------------------------------------------------------------------ helpers

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (a != null) return a;
            a = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(a, path);
            return a;
        }

        private static SurfaceRuleSO Surface(string name, string surfaceId, float friction, float speed, float ink, float coin, string tint, System.Action<SurfaceRuleSO> extra)
        {
            var s = LoadOrCreate<SurfaceRuleSO>(Dir + "/" + name + ".asset");
            s.surfaceId = surfaceId; s.friction = friction; s.speedFactor = speed; s.inkFactor = ink; s.coinFactor = coin; s.tintColorToken = tint;
            extra(s);
            EditorUtility.SetDirty(s);
            return s;
        }

        private static StageEventDefinition Event(string id, System.Action<StageEventDefinition> fill)
        {
            var e = LoadOrCreate<StageEventDefinition>(Dir + "/" + id + ".asset");
            // reset to the spec defaults of the fields this builder does not name, so a re-run is a clean rebuild
            var fresh = ScriptableObject.CreateInstance<StageEventDefinition>();
            EditorUtility.CopySerialized(fresh, e);
            Object.DestroyImmediate(fresh);
            e.name = id; e.id = id; e.telemetryId = id; e.startJitterM = 40f; e.lockDistanceM = 150f; e.minBreatherS = 12f; e.weight = 1f;
            fill(e);
            EditorUtility.SetDirty(e);
            return e;
        }

        private static void Cues(StageEventDefinition e, string key, string s1, string tint)
        {
            e.displayNameKey = "event." + key; e.bannerKey = "banner." + key; e.announcerCueId = s1; e.tintColorToken = tint;
            e.warnSfxId = "sfx." + key + ".sign"; e.cueSfxId = "sfx." + key + ".cue";
            e.announcerLeadS = 5f; e.signLeadS = 3f; e.cueLeadS = 1f; e.signalMinGapS = 0.6f; e.bannerSeconds = 1.5f;
        }

        private static StageCompatPair P(string a, string b, string reason, StageCompatRule rule = StageCompatRule.Forbid, float gap = 0f, string cond = "")
        {
            return new StageCompatPair { idA = a, idB = b, rule = rule, adjacentMinGapM = gap, conditionKey = cond, reasonKey = reason };
        }

        private static StageLadderSlot Slot(StageEventDefinition d, float nominal, float min, float max)
        {
            return new StageLadderSlot { eventDefinition = d, nominalStartM = nominal, startRangeMinM = min, startRangeMaxM = max, lengthM = 0f };
        }
    }

    /// <summary>QA debug jump points as menu items (Play mode only; Editor and development builds). Same list as the ladder's debugJumpPointsM.</summary>
    public static class StageDebugMenu
    {
        private static void Jump(float d)
        {
            if (!Application.isPlaying || RoadGameManager.Instance == null) { Debug.LogWarning("[Stage] Debug jumps work in Play mode, on a run."); return; }
            RoadGameManager.Instance.DebugJumpToStage(d);
        }

        [MenuItem("Road Builder/Stage/Debug Jump/0 start gate")] private static void J0() { Jump(0f); }
        [MenuItem("Road Builder/Stage/Debug Jump/220 ice")] private static void J220() { Jump(220f); }
        [MenuItem("Road Builder/Stage/Debug Jump/480 meteor warning")] private static void J480() { Jump(480f); }
        [MenuItem("Road Builder/Stage/Debug Jump/640 breather")] private static void J640() { Jump(640f); }
        [MenuItem("Road Builder/Stage/Debug Jump/710 mud warning")] private static void J710() { Jump(710f); }
        [MenuItem("Road Builder/Stage/Debug Jump/1000 eater start")] private static void J1000() { Jump(1000f); }
        [MenuItem("Road Builder/Stage/Debug Jump/1350 glass")] private static void J1350() { Jump(1350f); }
        [MenuItem("Road Builder/Stage/Debug Jump/1800 rings")] private static void J1800() { Jump(1800f); }
        [MenuItem("Road Builder/Stage/Debug Jump/2000 boss")] private static void J2000() { Jump(2000f); }
        [MenuItem("Road Builder/Stage/Debug Jump/Start a run")] private static void StartRun()
        {
            if (Application.isPlaying && RoadGameManager.Instance != null) RoadGameManager.Instance.StartRace();
        }
    }
}
