using System.Collections.Generic;
using UnityEngine;

namespace RoadBuilderGame.Tests
{
    /// <summary>In-memory slice data built from Docs/design/stage-events.md (sections 3.1, 5, 9). No assets, no scene.</summary>
    public static class StageTestData
    {
        public const int ModeId = 5;

        public static StageEventDefinition Def(string id, StageEventRole role, int cost, float min, float max, float len, float cooldown,
            string spawner = null, string[] requires = null, int modSlots = 0, float factor = 1f, float jitter = 40f, int variants = 2, float weight = 1f)
        {
            var d = ScriptableObject.CreateInstance<StageEventDefinition>();
            d.name = id; d.id = id; d.role = role; d.difficultyCost = cost; d.minDistanceM = min; d.maxDistanceM = max; d.lengthM = len;
            d.cooldownAfterM = cooldown; d.spawnerId = spawner; d.requires = requires ?? new string[0]; d.modifierSlots = modSlots;
            d.paceSpeedFactor = factor; d.startJitterM = jitter; d.variantCount = variants; d.weight = weight; d.telemetryId = id;
            return d;
        }

        public static StageEventDefinition Ice() { return Def("EVT-ICE", StageEventRole.Primary, 4, 190, 390, 140, 160, "hz-ice", factor: 1.15f); }
        public static StageEventDefinition Meteor() { return Def("EVT-METEOR", StageEventRole.Primary, 2, 490, 640, 100, 130, "hz-meteor", variants: 1); }
        public static StageEventDefinition Mud() { return Def("EVT-MUD", StageEventRole.Primary, 3, 720, 870, 120, 130, "hz-mud"); }
        public static StageEventDefinition Eater() { return Def("MON-EATER", StageEventRole.Monster, 7, 1000, 1750, 750, 100, "hz-eater", jitter: 0); }
        public static StageEventDefinition Glass() { var g = Def("EVT-GLASS", StageEventRole.Modifier, 6, 1250, 1600, 160, 0, "hz-glass", new[] { "MON-EATER" }, 1, variants: 1); g.safePlatformM = 6f; return g; }
        public static StageEventDefinition Rings() { return Def("EVT-RINGS", StageEventRole.Reward, 3, 1790, 1990, 180, 0); }
        public static StageEventDefinition Boss() { return Def("BOS-ROB0", StageEventRole.Boss, 8, 2000, 0, 600, 150, "hz-boss", jitter: 0); }

        static StageCompatPair P(string a, string b, StageCompatRule r = StageCompatRule.Forbid, float gap = 0f, string cond = null)
        {
            return new StageCompatPair { idA = a, idB = b, rule = r, adjacentMinGapM = gap, conditionKey = cond, reasonKey = "test" };
        }

        /// <summary>The 21 pairs of the section 5 table.</summary>
        public static EventCompatibilityMatrix Matrix()
        {
            var m = ScriptableObject.CreateInstance<EventCompatibilityMatrix>();
            m.name = "TestMatrix";
            m.pairs = new[]
            {
                P("EVT-ICE", "EVT-MUD"), P("EVT-ICE", "EVT-METEOR"), P("EVT-ICE", "EVT-GLASS"), P("EVT-ICE", "EVT-RINGS"), P("EVT-ICE", "MON-EATER"), P("EVT-ICE", "BOS-ROB0"),
                P("EVT-MUD", "EVT-METEOR"), P("EVT-MUD", "EVT-GLASS", StageCompatRule.Forbid, 300f), P("EVT-MUD", "EVT-RINGS"),
                P("EVT-MUD", "MON-EATER", StageCompatRule.Conditional, 0f, "C1:disabled"), P("EVT-MUD", "BOS-ROB0"),
                P("EVT-METEOR", "EVT-GLASS"), P("EVT-METEOR", "EVT-RINGS"), P("EVT-METEOR", "MON-EATER"), P("EVT-METEOR", "BOS-ROB0"),
                P("EVT-GLASS", "EVT-RINGS"), P("EVT-GLASS", "MON-EATER", StageCompatRule.Allow), P("EVT-GLASS", "BOS-ROB0"),
                P("EVT-RINGS", "MON-EATER"), P("EVT-RINGS", "BOS-ROB0"), P("MON-EATER", "BOS-ROB0"),
            };
            return m;
        }

        static StageLadderSlot Slot(StageEventDefinition d, float nominal, float min, float max, float len = 0f)
        {
            return new StageLadderSlot { eventDefinition = d, nominalStartM = nominal, startRangeMinM = min, startRangeMaxM = max, lengthM = len };
        }

        public static StageLadderDefinition SliceLadder(out List<StageEventDefinition> defs)
        {
            var l = ScriptableObject.CreateInstance<StageLadderDefinition>();
            l.name = "TestSlice";
            defs = new List<StageEventDefinition> { Ice(), Meteor(), Mud(), Eater(), Glass(), Rings(), Boss() };
            l.matrix = Matrix();
            l.slots = new[]
            {
                Slot(defs[0], 205, 190, 220), Slot(defs[1], 505, 490, 520), Slot(defs[2], 735, 720, 750), Slot(defs[3], 1000, 1000, 1000),
                Slot(defs[4], 1350, 1310, 1390), Slot(defs[5], 1800, 1790, 1810), Slot(defs[6], 2000, 2000, 2000),
            };
            l.eventPool = defs.ToArray();
            return l;
        }

        public static StageLadderDefinition SliceLadder() { return SliceLadder(out _); }

        public static StageRunRecord Record(uint raw, int mode = ModeId) { return new StageRunRecord { runSeed = StageSeed.MakeRunSeed(raw), modeId = mode, layoutVersion = 1 }; }

        /// <summary>Endless ladder pool: two cheap, one mid, two hard events. A/B carry a 300 m adjacency so the fallback path runs.</summary>
        public static StageLadderDefinition WeightedLadder()
        {
            var l = ScriptableObject.CreateInstance<StageLadderDefinition>();
            l.name = "TestWeighted";
            l.ladderMode = StageLadderMode.Weighted;
            var a = Def("W-A", StageEventRole.Primary, 2, 0, 0, 400, 100, "hz-a", weight: 1f);
            var b = Def("W-B", StageEventRole.Primary, 3, 0, 0, 400, 100, "hz-b", weight: 2f);
            var c = Def("W-C", StageEventRole.Primary, 7, 0, 0, 450, 100, "hz-c", weight: 2f);
            var d = Def("W-D", StageEventRole.Primary, 8, 0, 0, 500, 100, "hz-d", weight: 2f);
            var e = Def("W-E", StageEventRole.Primary, 4, 0, 0, 350, 100, "hz-e", weight: 3f);
            var m = ScriptableObject.CreateInstance<EventCompatibilityMatrix>();
            var ids = new[] { a.id, b.id, c.id, d.id, e.id };
            var pairs = new List<StageCompatPair>();
            for (int i = 0; i < ids.Length; i++) for (int j = i + 1; j < ids.Length; j++)
                pairs.Add(P(ids[i], ids[j], StageCompatRule.Forbid, (i == 0 && j == 1) ? 300f : 0f));
            m.pairs = pairs.ToArray();
            l.matrix = m;
            l.eventPool = new[] { a, b, c, d, e };
            l.weightedLengthM = 8000f;
            l.weightedMaxEvents = 40;
            return l;
        }

        public static string Signature(StagePlan plan)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var e in plan.Events) sb.Append(e.SlotIndex).Append('|').Append(e.Id).Append('|').Append(e.StartM.ToString("R")).Append('|').Append(e.EndM.ToString("R")).Append('|').Append(e.Variant).Append('|').Append(e.TerrainSeed).Append(';');
            return sb.ToString();
        }
    }
}
