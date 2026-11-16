using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RoadBuilderGame.Editor
{
    /// <summary>
    /// Batch A2: creates (or refreshes in place) the 26 remaining blueprint stage events as StageEventDefinition assets in Resources/Stage/Events, then rebuilds the endless
    /// compatibility matrix over ALL events (A1 12 + slice 3 + monster + boss + A2 26) and adds them to the endless ladder pool. Runs the A1 pack first so one menu keeps
    /// everything consistent. Safe to re-run. Menu: Road Builder / Stage / Build Event Pack 2.
    /// </summary>
    public static class MutationPack2Builder
    {
        // never stack (blueprint) plus the channel and family clashes this batch adds
        private static readonly string[][] NeverStack2 =
        {
            new[] { "EVT-GRAVINV", "EVT-SHADOW", "blueprint: gravity inversion + invisible road" },
            new[] { "EVT-GRAVINV", "EVT-INVINK", "blueprint: gravity inversion + invisible road" },
            new[] { "EVT-REVERSE", "EVT-MIRROR", "blueprint: reverse driving + mirror controls" },
            new[] { "EVT-LOWINK", "EVT-INVINK", "blueprint: low ink + invisible ink + road collapse" },
            new[] { "EVT-LOWINK", "EVT-COLLAPSE", "blueprint: low ink + invisible ink + road collapse" },
            new[] { "EVT-INVINK", "EVT-COLLAPSE", "blueprint: low ink + invisible ink + road collapse" },
            new[] { "EVT-LAVA", "EVT-FLOOD", "blueprint: lava chase + flood surge" },
            new[] { "EVT-FLAPPY", "EVT-REVERSE", "blueprint: flappy tunnel + reverse driving" },
            new[] { "EVT-GRAVINV", "EVT-GRAVSHIFT", "same gravity channel" }, new[] { "EVT-GRAVINV", "EVT-HEAVYG", "same gravity channel" },
            new[] { "EVT-GRAVZONES", "EVT-GRAVSHIFT", "same gravity channel" }, new[] { "EVT-GRAVZONES", "EVT-HEAVYG", "same gravity channel" },
            new[] { "EVT-GRAVINV", "EVT-GRAVZONES", "same gravity channel" },
            new[] { "EVT-SWAP", "EVT-TINY", "vehicle swap changes the scale channel" }, new[] { "EVT-SWAP", "EVT-GIANT", "vehicle swap changes the scale channel" },
            new[] { "EVT-SWAP", "EVT-GRAVSHIFT", "vehicle swap changes gravity" }, new[] { "EVT-SWAP", "EVT-HEAVYG", "vehicle swap changes gravity" },
            new[] { "EVT-SWAP", "EVT-GRAVINV", "vehicle swap changes gravity" }, new[] { "EVT-SWAP", "EVT-GRAVZONES", "vehicle swap changes gravity" },
            new[] { "EVT-SWAP", "EVT-TUNNEL", "vehicle swap changes speed" },
            new[] { "EVT-TINY", "EVT-FLAPPY", "tiny car in a tight tunnel is trivial and unreadable" },
            new[] { "EVT-GIANT", "EVT-FLAPPY", "giant car cannot fit the gaps" }, new[] { "EVT-GIANT", "EVT-MAGNET", "giant car and pylons" },
            new[] { "EVT-WHITEOUT", "EVT-SHADOW", "two vision losses" }, new[] { "EVT-WHITEOUT", "EVT-INVINK", "two vision losses" }, new[] { "EVT-SHADOW", "EVT-INVINK", "two vision losses" },
            new[] { "EVT-WINDTUNNEL", "EVT-CANYON", "two winds" }, new[] { "EVT-WINDTUNNEL", "EVT-INKSTORM", "wind + wind gusts" }, new[] { "EVT-WINDTUNNEL", "EVT-GRAVSHIFT", "wind + low gravity" },
            new[] { "EVT-WINDTUNNEL", "EVT-TUNNEL", "speed + wind" }, new[] { "EVT-WINDTUNNEL", "EVT-FRAGILE", "wind + fragile" },
            new[] { "EVT-ICEAGE", "EVT-STICKY", "two friction events" }, new[] { "EVT-ICEAGE", "EVT-ELASTIC", "two road-material events" }, new[] { "EVT-STICKY", "EVT-ELASTIC", "two road-material events" },
            new[] { "EVT-ELASTIC", "EVT-FRAGILE", "bouncing cracks a fragile car" }, new[] { "EVT-ELASTIC", "EVT-HEAVYG", "bounce + heavy gravity" },
            new[] { "EVT-FLOOD", "EVT-COLLAPSE", "rising water + vanishing road" }, new[] { "EVT-LAVA", "EVT-COLLAPSE", "rising lava + vanishing road" },
            new[] { "EVT-LAVA", "EVT-SHADOW", "lava + invisible road" }, new[] { "EVT-LAVA", "EVT-INVINK", "lava + invisible road" },
            new[] { "EVT-FRAGILE", "EVT-FENCES", "fences + fragile" }, new[] { "EVT-FRAGILE", "EVT-BOULDER", "unavoidable boulder + fragile" }, new[] { "EVT-FRAGILE", "EVT-FLAPPY", "gates + fragile" },
            new[] { "EVT-FRAGILE", "EVT-TRAFFIC", "traffic + fragile" }, new[] { "EVT-FRAGILE", "EVT-MAGNET", "pull + fragile" },
            new[] { "EVT-VACUUM", "EVT-MIRROR", "drifting coins + mirrored drawing" },
            new[] { "EVT-TUNNEL", "EVT-REVERSE", "speed + reverse bursts" }, new[] { "EVT-TUNNEL", "EVT-BOULDER", "speed + rolling boulder" }, new[] { "EVT-TUNNEL", "EVT-FENCES", "speed + fences" },
            new[] { "EVT-TUNNEL", "EVT-LASER", "speed + laser" }, new[] { "EVT-TUNNEL", "EVT-FLAPPY", "speed + gates" }, new[] { "EVT-TUNNEL", "EVT-RHYTHM", "speed + beat gates" },
            new[] { "EVT-MIRROR", "EVT-INKSTORM", "mirror + ink starvation" }, new[] { "EVT-MIRROR", "EVT-LOWINK", "mirror + ink starvation" },
            new[] { "EVT-DOUBLE", "EVT-TINY", "ghost copy and tiny scale" },
            new[] { "EVT-RHYTHM", "EVT-LOWINK", "mistimed gates drain ink: low ink is unfair" }, new[] { "EVT-RHYTHM", "EVT-INKSTORM", "mistimed gates drain ink: ink starvation" },
            new[] { "EVT-REVERSE", "EVT-LAVA", "reverse into rising lava" }, new[] { "EVT-REVERSE", "EVT-FLOOD", "reverse into rising water" }, new[] { "EVT-REVERSE", "EVT-FENCES", "reverse into charged fences" },
            new[] { "EVT-REVERSE", "EVT-BOULDER", "reverse into a boulder" },
        };

        private static StageEventDefinition Mk(string key, string tint, int cost, float len, string family, string[] tags, string[] worlds, string rule, string counter, float pace, params StageEffectSpec[] fx)
        {
            var e = MutationPackBuilder.Ev("EVT-" + key.ToUpperInvariant(), key, tint, cost, len, family, tags[0], worlds, rule, counter, pace, fx);
            e.ladderTags = tags;
            EditorUtility.SetDirty(e);
            return e;
        }

        private static StageEffectSpec S(StageEffectKind k, float a, float b = 0f) { return new StageEffectSpec(k, a, b); }

        [MenuItem("Road Builder/Stage/Build Event Pack 2")]
        public static void BuildMenu()
        {
            var errors = Build();
            if (errors.Count == 0) Debug.Log("[MutationPack2Builder] Event pack 2 built and valid in " + MutationPackBuilder.Dir);
            else foreach (var e in errors) Debug.LogError("[MutationPack2Builder] " + e);
        }

        public static List<string> Build()
        {
            MutationPackBuilder.Build();                                   // A1 + slice first (their matrix and ladder are overwritten below with the full set)
            var ev = new List<StageEventDefinition>
            {
                Mk("gravinv", "ZoneGravity", 9, 350, "gravity", new[] { "physics" }, new[] { "moon", "synthwave" }, "Gravity flips for 3 s windows", "Overhead paths and a shield", 1f, S(StageEffectKind.GravityPulse, -0.3f, 3f)),
                Mk("whiteout", "ZoneVision", 7, 400, "vision", new[] { "environment" }, new[] { "snow", "haunted" }, "Fog hides the road ahead (30 m)", "Follow markers, conservative slopes", 1f, S(StageEffectKind.Whiteout, 30f, 0.9f)),
                Mk("reverse", "ZoneControl", 8, 350, "control", new[] { "movement" }, new[] { "haunted", "cave" }, "Motor reverses in 5 s bursts", "Adapt drawing and input", 1f, S(StageEffectKind.MotorReverse, -0.5f, 5f)),
                Mk("mirror", "ZoneControl", 8, 400, "control", new[] { "movement" }, new[] { "synthwave", "moon" }, "Drawing input mirrors", "Draw using the ring preview", 1f, S(StageEffectKind.MirrorInput, 1f)),
                Mk("flappy", "ZoneControl", 8, 250, "obstacle", new[] { "movement", "environment" }, new[] { "candy", "cave" }, "Ceiling and floor blocks form gaps", "Draw through the gaps, tap to hop", 1f, S(StageEffectKind.FlappyTunnel, 26f, 2.6f)),
                Mk("collapse", "ZoneTerrain", 8, 400, "terrain", new[] { "environment", "surface" }, new[] { "cave", "volcano" }, "Road behind you vanishes after 1.5 s", "Keep your momentum", 1f, S(StageEffectKind.RoadCollapse, 1.5f)),
                Mk("iceage", "ZoneIce", 7, 350, "terrain", new[] { "surface", "environment" }, new[] { "snow", "moon" }, "Every road is slippery", "Gradual curves", 1f, S(StageEffectKind.IceAge, 0.15f)),
                Mk("magnet", "ZoneHazard", 8, 350, "obstacle", new[] { "environment" }, new[] { "synthwave", "cave" }, "Steel pylons pull the car", "Counter-curves and a shield", 1f, S(StageEffectKind.MagneticPylons, 14f, 9f)),
                Mk("elastic", "ZoneTerrain", 6, 350, "terrain", new[] { "surface" }, new[] { "candy", "hills" }, "The road bounces on impact", "Keep ramps low", 1f, S(StageEffectKind.ElasticRoad, 0.55f)),
                Mk("shadow", "ZoneVision", 9, 300, "vision", new[] { "environment" }, new[] { "haunted", "cave" }, "Road turns invisible after 2 s", "Follow the ghost markers", 1f, S(StageEffectKind.RoadFade, 2f, 8f)),
                Mk("double", "ZoneControl", 7, 300, "control", new[] { "movement" }, new[] { "synthwave" }, "A ghost copy trails 0.5 s behind", "Leave room for both", 1f, S(StageEffectKind.GhostDouble, 0.5f)),
                Mk("platforms", "ZoneTerrain", 8, 300, "terrain", new[] { "environment" }, new[] { "cave", "moon" }, "Platforms drop 1 s after you land", "Connect roads, keep moving", 1f, S(StageEffectKind.FallingPlatforms, 1f)),
                Mk("gravzones", "ZoneGravity", 9, 350, "gravity", new[] { "physics" }, new[] { "moon", "synthwave" }, "Gravity wells flip at gates", "Low vertical speed at gates", 1f, S(StageEffectKind.GravityGates, -0.35f, 58f)),
                Mk("traffic", "ZoneHazard", 6, 400, "obstacle", new[] { "environment" }, new[] { "hills", "desert" }, "Slow vehicles block the road", "Draw over or around", 1f, S(StageEffectKind.TrafficConvoy, 3.5f, 6f)),
                Mk("boulder", "ZoneHazard", 7, 350, "obstacle", new[] { "environment" }, new[] { "hills", "cave" }, "A boulder drops, breaks the road, rolls", "Jump it or guide it into the pit", 1f, S(StageEffectKind.RollingBoulder, 6f, 9f)),
                Mk("fences", "ZoneHazard", 8, 300, "obstacle", new[] { "environment" }, new[] { "synthwave" }, "Fences pulse every 2 s", "Cross in the 0.8 s safe window", 1f, S(StageEffectKind.ElectricFences, 2f, 0.8f)),
                Mk("flood", "ZoneLiquid", 8, 350, "liquid", new[] { "environment" }, new[] { "underwater", "hills" }, "Water rises 1 m every 5 s", "Stay above the waterline", 1f, S(StageEffectKind.FloodSurge, 1f, 5f)),
                Mk("lava", "ZoneLiquid", 9, 400, "liquid", new[] { "environment" }, new[] { "volcano" }, "Lava rises 0.8 m/s", "Keep altitude and speed", 1f, S(StageEffectKind.LavaChase, 0.8f, 9f)),
                Mk("windtunnel", "ZoneWind", 7, 350, "wind", new[] { "environment" }, new[] { "desert", "snow" }, "Wind turns at every gate", "Change the road angle before entry", 1f, S(StageEffectKind.WindTunnels, 3.5f, 64f)),
                Mk("sticky", "ZoneTerrain", 5, 400, "terrain", new[] { "surface" }, new[] { "mud", "candy" }, "The road grips and drags", "Long curves", 1f, S(StageEffectKind.StickyRoad, 2.2f, 0.92f)),
                Mk("invink", "ZoneVision", 9, 300, "vision", new[] { "environment" }, new[] { "haunted" }, "Drawn road is visible for 1 s", "Trust the preview line", 1f, S(StageEffectKind.RoadFade, 1f, 0f)),
                Mk("laser", "ZoneHazard", 8, 350, "obstacle", new[] { "environment" }, new[] { "synthwave" }, "A laser cuts one road strip", "Bridge it or hop it", 1f, S(StageEffectKind.RoadLaser, 2.4f, 7f)),
                Mk("stars", "ZoneTerrain", 6, 350, "terrain", new[] { "environment" }, new[] { "moon" }, "Stars fall and become platforms", "Catch them to build a route", 1f, S(StageEffectKind.FallingStars, 4.5f, 3.2f)),
                Mk("swap", "ZoneControl", 8, 300, "control", new[] { "physics" }, new[] { "all" }, "Vehicle class swaps every 100 m", "Adapt to the new feel", 1f, S(StageEffectKind.VehicleSwap, 100f)),
                Mk("rhythm", "ZoneRhythm", 8, 400, "rhythm", new[] { "movement" }, new[] { "synthwave", "candy" }, "Gates pulse; off-beat crossings drain ink", "Match the beat", 1f, S(StageEffectKind.RhythmGates, 96f, 48f)),
                Mk("chaos", "ZoneHazard", 10, 500, "chaos", new[] { "environment", "physics", "movement" }, new[] { "all" }, "Two modifiers plus elite strikes", "Manage your resources", 1f,
                    S(StageEffectKind.ChaosFinale, 3.4f), S(StageEffectKind.GravityMul, 0.8f), S(StageEffectKind.SpeedMul, 1.2f), S(StageEffectKind.InkRegenMul, 0.6f), S(StageEffectKind.BoostDrainMul, 1.8f)),
            };
            foreach (var e in ev) if (e.id == "EVT-CHAOS") { e.variantCount = 3; EditorUtility.SetDirty(e); }

            var L = AssetDatabase.LoadAssetAtPath<StageLadderDefinition>(MutationPackBuilder.EndlessLadderPath);
            var oldPool = new List<StageEventDefinition>(L.eventPool);
            var all = new List<StageEventDefinition>(oldPool);
            foreach (var e in ev) if (!all.Contains(e)) all.Add(e);

            var fam = new Dictionary<string, string>();
            foreach (var d in all) fam[d.id] = d.hazardFamily;
            var matrix = MutationPackBuilder.LoadOrCreate<EventCompatibilityMatrix>(MutationPackBuilder.EndlessMatrixPath);
            var pairs = new List<StageCompatPair>();
            for (int i = 0; i < all.Count; i++)
                for (int j = i + 1; j < all.Count; j++)
                {
                    string a = all[i].id, b = all[j].id, reason; var rule = Rule(a, b, fam, out reason);
                    pairs.Add(new StageCompatPair { idA = a, idB = b, rule = rule, adjacentMinGapM = 0f, conditionKey = "", reasonKey = reason });
                }
            matrix.pairs = pairs.ToArray();
            EditorUtility.SetDirty(matrix);
            foreach (var e in all)
            {
                if (e.role != StageEventRole.Primary || e.effects == null || e.effects.Length == 0) continue;
                var f = new List<string>();
                foreach (var o in all) if (o != e && !matrix.CanOverlap(e.id, o.id)) f.Add(o.id);
                e.forbiddenWith = f.ToArray(); EditorUtility.SetDirty(e);
            }

            L.matrix = matrix; L.eventPool = all.ToArray(); L.bossEveryM = 5000f; L.bossFirstStage = 3;
            EditorUtility.SetDirty(L);
            AssetDatabase.SaveAssets();
            return StageSliceBuilder.ValidateAll();
        }

        private static StageCompatRule Rule(string a, string b, Dictionary<string, string> fam, out string reason)
        {
            foreach (var n in NeverStack2) if ((n[0] == a && n[1] == b) || (n[0] == b && n[1] == a)) { reason = n[2]; return StageCompatRule.Forbid; }
            if (a == "EVT-CHAOS" || b == "EVT-CHAOS") { reason = "A2:chaos finale already stacks two modifiers"; return StageCompatRule.Forbid; }
            string fa = fam.ContainsKey(a) ? fam[a] : "", fb = fam.ContainsKey(b) ? fam[b] : "";
            bool a2 = IsA2(a), b2 = IsA2(b);
            if ((a2 || b2) && !string.IsNullOrEmpty(fa) && fa == fb) { reason = "A2:same hazard family never stacks"; return StageCompatRule.Forbid; }
            string r; var rule = MutationPackBuilder.Rule(a, b, out r); reason = r; return rule;
        }

        private static bool IsA2(string id)
        {
            switch (id)
            {
                case "EVT-GRAVINV": case "EVT-WHITEOUT": case "EVT-REVERSE": case "EVT-MIRROR": case "EVT-FLAPPY": case "EVT-COLLAPSE": case "EVT-ICEAGE": case "EVT-MAGNET": case "EVT-ELASTIC": case "EVT-SHADOW":
                case "EVT-DOUBLE": case "EVT-PLATFORMS": case "EVT-GRAVZONES": case "EVT-TRAFFIC": case "EVT-BOULDER": case "EVT-FENCES": case "EVT-FLOOD": case "EVT-LAVA": case "EVT-WINDTUNNEL":
                case "EVT-STICKY": case "EVT-INVINK": case "EVT-LASER": case "EVT-STARS": case "EVT-SWAP": case "EVT-RHYTHM": case "EVT-CHAOS": return true;
            }
            return false;
        }
    }
}
