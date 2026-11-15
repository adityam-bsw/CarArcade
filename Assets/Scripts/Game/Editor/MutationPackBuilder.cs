using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace RoadBuilderGame.Editor
{
    /// <summary>
    /// Creates (or refreshes in place) the A1 mutation pack from the blueprint A2 table: twelve StageEventDefinition assets (gravity, speed, ink, wind, economy, body,
    /// scale), the endless compatibility matrix (every pair of the 17 events, with the never-stack list as Forbid) and the endless StageLadderDefinition with the
    /// distance table rows, tier weights, world tags and rules. Safe to re-run. Menu: Road Builder / Stage / Build Mutation Pack.
    /// </summary>
    public static class MutationPackBuilder
    {
        public const string Dir = StageSliceBuilder.Dir + "/Events";
        public const string EndlessLadderPath = StageSliceBuilder.Dir + "/EndlessLadder.asset";
        public const string EndlessMatrixPath = StageSliceBuilder.Dir + "/EndlessCompatibilityMatrix.asset";

        // Event keys: gs hg tu is bd cv wc li tx fr tn gn
        private static readonly string[][] NeverStack =
        {
            new[] { "EVT-GRAVSHIFT", "EVT-HEAVYG", "same gravity channel" },
            new[] { "EVT-TINY", "EVT-GIANT", "never stack: tiny + giant" },
            new[] { "EVT-GRAVSHIFT", "EVT-INKSTORM", "low gravity + wind gusts is uncontrollable" },
            new[] { "EVT-GRAVSHIFT", "EVT-CANYON", "low gravity + wind is uncontrollable" },
            new[] { "EVT-HEAVYG", "EVT-FRAGILE", "heavy landings crack a fragile car unfairly" },
            new[] { "EVT-HEAVYG", "EVT-GIANT", "heavy + giant bogs the car" },
            new[] { "EVT-TUNNEL", "EVT-CANYON", "speed + wind" },
            new[] { "EVT-TUNNEL", "EVT-INKSTORM", "speed + wind gusts" },
            new[] { "EVT-TUNNEL", "EVT-FRAGILE", "speed + fragile" },
            new[] { "EVT-INKSTORM", "EVT-LOWINK", "ink starvation" },
            new[] { "EVT-INKSTORM", "EVT-CANYON", "two winds" },
            new[] { "EVT-INKSTORM", "EVT-FRAGILE", "wind + fragile" },
            new[] { "EVT-CANYON", "EVT-FRAGILE", "wind + fragile" },
            new[] { "EVT-FRAGILE", "EVT-GIANT", "giant impacts crack a fragile car" },
            new[] { "EVT-VACUUM", "EVT-TAX", "drifting coins + tax is brutal" },
        };

        // Allowed with the three surface and sky events of the slice (everything else with them is Forbid).
        private static readonly Dictionary<string, string[]> SliceAllow = new Dictionary<string, string[]>
        {
            { "EVT-ICE", new[] { "EVT-GRAVSHIFT", "EVT-BOOSTDRAIN", "EVT-VACUUM", "EVT-TAX", "EVT-TINY" } },
            { "EVT-MUD", new[] { "EVT-GRAVSHIFT", "EVT-TUNNEL", "EVT-VACUUM", "EVT-TAX", "EVT-GIANT", "EVT-BOOSTDRAIN" } },
            { "EVT-METEOR", new[] { "EVT-GRAVSHIFT", "EVT-BOOSTDRAIN", "EVT-VACUUM", "EVT-TAX", "EVT-TINY", "EVT-GIANT", "EVT-LOWINK" } },
        };

        [MenuItem("Road Builder/Stage/Build Mutation Pack")]
        public static void BuildMenu()
        {
            var errors = Build();
            AssetDatabase.SaveAssets();
            if (errors.Count == 0) Debug.Log("[MutationPackBuilder] Mutation pack built and valid in " + Dir);
            else foreach (var e in errors) Debug.LogError("[MutationPackBuilder] " + e);
        }

        public static List<string> Build()
        {
            Directory.CreateDirectory(Dir);
            StageSliceBuilder.Build();                       // slice assets first, so the endless metadata of the slice events is in place

            var gs = Ev("EVT-GRAVSHIFT", "gravshift", "ZoneGravity", 5, 450, "gravity", "physics", new[] { "moon", "mars" }, "Gravity 0.65x", "Shallow ramps, avoid airtime", 1f, new StageEffectSpec(StageEffectKind.GravityMul, 0.65f));
            var hg = Ev("EVT-HEAVYG", "heavyg", "ZoneGravity", 6, 400, "gravity", "physics", new[] { "earth", "mars" }, "Gravity 1.6x", "Smooth descents, no big jumps", 1f, new StageEffectSpec(StageEffectKind.GravityMul, 1.6f));
            var tu = Ev("EVT-TUNNEL", "tunnel", "ZoneSpeed", 6, 300, "speed", "movement", new[] { "synthwave", "hills" }, "Speed +45%", "Draw long smooth roads", 1.45f, new StageEffectSpec(StageEffectKind.SpeedMul, 1.45f));
            var ink = Ev("EVT-INKSTORM", "inkstorm", "ZoneInk", 7, 450, "ink", "environment", new[] { "desert", "mars" }, "Ink regen -50%, wind gusts", "Short efficient segments", 1f,
                new StageEffectSpec(StageEffectKind.InkRegenMul, 0.5f), new StageEffectSpec(StageEffectKind.WindGust, 4f, 4.5f));
            var bd = Ev("EVT-BOOSTDRAIN", "boostdrain", "ZoneSpeed", 6, 400, "boost", "movement", new[] { "volcano", "desert" }, "Boost burns 2x faster", "Save your boost", 1f, new StageEffectSpec(StageEffectKind.BoostDrainMul, 2f));
            var cv = Ev("EVT-VACUUM", "vacuum", "ZoneEconomy", 5, 350, "economy", "economy", new[] { "hills", "candy" }, "Coins drift away", "Draw through their path", 1f, new StageEffectSpec(StageEffectKind.CoinVacuum, 7f, 4.5f));
            var wc = Ev("EVT-CANYON", "canyon", "ZoneWind", 7, 450, "wind", "environment", new[] { "hills", "desert" }, "Side wind 3 m/s", "Counter-slope, stay low", 1f, new StageEffectSpec(StageEffectKind.WindSteady, 3f, 8f));
            wc.ladderTags = new[] { "environment", "movement" };
            var li = Ev("EVT-LOWINK", "lowink", "ZoneInk", 8, 300, "ink", "economy", new[] { "all" }, "Ink tank 45%", "Use terrain, short bridges", 1f, new StageEffectSpec(StageEffectKind.InkCapacityMul, 0.45f));
            var tx = Ev("EVT-TAX", "tax", "ZoneEconomy", 5, 400, "economy", "economy", new[] { "all" }, "-10 coins every 100 m", "Cross the coin gates", 1f, new StageEffectSpec(StageEffectKind.RoadTax, 10f, 100f));
            var fr = Ev("EVT-FRAGILE", "fragile", "ZoneBody", 8, 350, "body", "body", new[] { "all" }, "Cracks at 60% impact", "Smooth roads, no flips", 1f, new StageEffectSpec(StageEffectKind.Fragile, 0.6f, 12f));
            fr.ladderTags = new[] { "body", "physics" };
            var tn = Ev("EVT-TINY", "tiny", "ZoneScale", 7, 300, "scale", "physics", new[] { "candy", "cave" }, "Car 0.55x", "Narrow gaps are open", 1f, new StageEffectSpec(StageEffectKind.VehicleScale, 0.55f));
            var gn = Ev("EVT-GIANT", "giant", "ZoneScale", 7, 300, "scale", "physics", new[] { "hills", "desert" }, "Car 1.6x", "Wide stable roads", 1f, new StageEffectSpec(StageEffectKind.VehicleScale, 1.6f));
            var mutations = new[] { gs, hg, tu, ink, bd, cv, wc, li, tx, fr, tn, gn };

            var ice = Load("EVT-ICE"); var mud = Load("EVT-MUD"); var meteor = Load("EVT-METEOR"); var eater = Load("MON-EATER"); var boss = Load("BOS-ROB0");
            var all = new List<StageEventDefinition>(mutations) { ice, mud, meteor, eater, boss };

            // ---- matrix: every pair of the 17 events, explicit
            var matrix = LoadOrCreate<EventCompatibilityMatrix>(EndlessMatrixPath);
            var pairs = new List<StageCompatPair>();
            for (int i = 0; i < all.Count; i++)
                for (int j = i + 1; j < all.Count; j++)
                {
                    string a = all[i].id, b = all[j].id;
                    string reason; var rule = Rule(a, b, out reason);
                    pairs.Add(new StageCompatPair { idA = a, idB = b, rule = rule, adjacentMinGapM = 0f, conditionKey = "", reasonKey = reason });
                }
            matrix.pairs = pairs.ToArray();
            EditorUtility.SetDirty(matrix);
            // mirror lists on the mutation events (validated against the matrix)
            foreach (var e in mutations)
            {
                var f = new List<string>();
                foreach (var o in all) if (o != e && !matrix.CanOverlap(e.id, o.id)) f.Add(o.id);
                e.forbiddenWith = f.ToArray();
                EditorUtility.SetDirty(e);
            }

            // ---- endless ladder
            var L = LoadOrCreate<StageLadderDefinition>(EndlessLadderPath);
            L.ladderMode = StageLadderMode.Endless;
            L.layoutVersion = 1;
            L.matrix = matrix;
            L.slots = new StageLadderSlot[0];
            L.eventPool = all.ToArray();
            L.stageLengthM = 1000f; L.endlessStageCount = 40;
            L.endlessStages = new[]
            {
                Row(1, "Road-Eater", "", eater, StageTier.Easy, StageTier.Extreme, 1),
                Row(2, "First physics mutation", "physics", null, StageTier.Easy, StageTier.Medium, 1),
                Row(3, "New movement mechanic", "movement", null, StageTier.Easy, StageTier.Medium, 1),
                Row(4, "Environmental hazard", "environment", null, StageTier.Medium, StageTier.Hard, 1),
                Row(5, "Two compatible modifiers", "", null, StageTier.Medium, StageTier.Hard, 2),
                Row(6, "Elite hazard", "", null, StageTier.Hard, StageTier.Extreme, 1),
                Row(7, "Advanced surface combo", "surface", null, StageTier.Easy, StageTier.Hard, 2),
                Row(8, "Boss", "", boss, StageTier.Easy, StageTier.Extreme, 1),
                Row(9, "Extreme", "", null, StageTier.Hard, StageTier.Extreme, 2),
                Row(10, "Survival gauntlet", "", null, StageTier.Medium, StageTier.Extreme, 2),
            };
            L.endlessRandomRow = Row(11, "Endless controlled random", "", null, StageTier.Easy, StageTier.Extreme, 2);
            L.tierWeights = new[] { 40f, 35f, 20f, 5f };
            L.endlessModifierCap = 2; L.stackGapFromM = 5000f; L.activationGapM = 100f; L.endlessHardCost = 7;
            L.familyWindowStages = 4; L.familyMaxInWindow = 2; L.endlessStartJitterM = 60f;
            L.adaptiveFailsToEase = 3; L.adaptiveHardMul = 0.35f;
            L.hardStagesBeforeBreather = 2; L.noHazardUntilM = 190f;
            L.worldTags = WorldTags();
            L.themeTagsCountry = new[] { "hills", "earth" }; L.themeTagsDesert = new[] { "desert", "earth" }; L.themeTagsCyber = new[] { "synthwave", "earth" };
            EditorUtility.SetDirty(L);
            AssetDatabase.SaveAssets();
            return StageSliceBuilder.ValidateAll();
        }

        private static StageCompatRule Rule(string a, string b, out string reason)
        {
            foreach (var n in NeverStack) if ((n[0] == a && n[1] == b) || (n[0] == b && n[1] == a)) { reason = n[2]; return StageCompatRule.Forbid; }
            foreach (var kv in SliceAllow)
            {
                bool aSlice = kv.Key == a, bSlice = kv.Key == b;
                if (aSlice || bSlice)
                {
                    string other = aSlice ? b : a;
                    if (System.Array.IndexOf(kv.Value, other) >= 0) { reason = "A1:compatible"; return StageCompatRule.Allow; }
                    reason = "A1:slice event stacks only with the listed mutations"; return StageCompatRule.Forbid;
                }
            }
            bool special = a == "MON-EATER" || b == "MON-EATER" || a == "BOS-ROB0" || b == "BOS-ROB0" || a == "EVT-ICE" || b == "EVT-ICE" || a == "EVT-MUD" || b == "EVT-MUD" || a == "EVT-METEOR" || b == "EVT-METEOR";
            if (special) { reason = "A1:monster and boss never stack"; return StageCompatRule.Forbid; }
            reason = "A1:compatible"; return StageCompatRule.Allow;
        }

        private static EndlessStageRow Row(int stage, string label, string tag, StageEventDefinition fixedEvent, StageTier min, StageTier max, int concurrent)
        {
            return new EndlessStageRow { stage = stage, label = label, tag = tag, fixedEvent = fixedEvent, minTier = min, maxTier = max, concurrent = concurrent };
        }

        private static StageWorldTagEntry W(string id, params string[] tags) { return new StageWorldTagEntry { worldId = id, tags = tags }; }

        private static StageWorldTagEntry[] WorldTags()
        {
            return new[]
            {
                W("world-moon", "moon"), W("world-mars", "mars"), W("world-rolling-hills", "hills", "earth"), W("world-desert", "desert", "earth"),
                W("world-canyon", "desert", "hills", "earth"), W("world-wild-west", "desert", "earth"), W("world-night-city", "synthwave", "earth"), W("world-synthwave", "synthwave"),
                W("world-industrial", "earth"), W("world-candy-land", "candy"), W("world-cave", "cave"), W("world-volcano", "volcano"),
                W("world-arctic", "snow", "earth"), W("world-winter", "snow", "earth"), W("world-christmas", "snow", "earth"),
                W("world-haunted-halloween", "haunted"), W("world-underwater", "underwater"), W("world-cloud-kingdom", "cloud"),
                W("world-farm", "hills", "earth"), W("world-jungle", "hills", "earth"), W("world-coast", "hills", "earth"), W("world-summer", "hills", "earth"),
                W("world-spring-blossom", "hills", "earth"), W("world-autumn-leaves", "hills", "earth"),
            };
        }

        // ------------------------------------------------------------------ helpers

        private static StageEventDefinition Load(string id)
        {
            var e = AssetDatabase.LoadAssetAtPath<StageEventDefinition>(StageSliceBuilder.Dir + "/" + id + ".asset");
            if (e == null) throw new System.Exception("Slice event " + id + " missing; run Build Slice Assets first");
            return e;
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (a != null) return a;
            a = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(a, path);
            return a;
        }

        private static StageEventDefinition Ev(string id, string key, string tint, int cost, float len, string family, string tag, string[] worlds, string rule, string counter, float pace, params StageEffectSpec[] effects)
        {
            var e = LoadOrCreate<StageEventDefinition>(Dir + "/" + id + ".asset");
            var fresh = ScriptableObject.CreateInstance<StageEventDefinition>();
            EditorUtility.CopySerialized(fresh, e);
            Object.DestroyImmediate(fresh);
            e.name = id; e.id = id; e.telemetryId = id; e.role = StageEventRole.Primary; e.difficultyCost = cost; e.lengthM = len; e.weight = 1f; e.paceSpeedFactor = pace;
            e.minDistanceM = 0f; e.maxDistanceM = 0f; e.startJitterM = 40f; e.lockDistanceM = 150f; e.cooldownAfterM = 130f; e.minBreatherS = 12f;
            e.displayNameKey = "event." + key; e.bannerKey = "banner." + key; e.announcerCueId = "vo.rob0.s1." + key; e.tintColorToken = tint;
            e.warnSfxId = "sfx." + key + ".sign"; e.cueSfxId = "sfx." + key + ".cue";
            e.announcerLeadS = 5f; e.signLeadS = 3f; e.cueLeadS = 1f; e.signalMinGapS = 0.6f; e.bannerSeconds = 1.5f;
            e.effects = effects; e.ruleText = rule; e.counterplayText = counter; e.iconKey = "icon." + key;
            e.signText = StageCues.SignText("banner." + key);
            e.suitableWorlds = worlds; e.hazardFamily = family; e.ladderTags = new[] { tag };
            e.coinShape = StageCoinShape.Line; e.variantCount = 2;
            EditorUtility.SetDirty(e);
            return e;
        }
    }
}
