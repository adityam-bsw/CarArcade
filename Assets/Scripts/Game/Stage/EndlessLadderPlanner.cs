using System.Collections.Generic;
using UnityEngine;

namespace RoadBuilderGame
{
    /// <summary>What the endless planner needs to know about the run besides the seed: the world's tags and the stages the adaptive challenge eases.</summary>
    public struct StageWorldContext
    {
        public string[] Tags;
        public int[] EasedStages;
        public bool IsEased(int stage) { if (EasedStages != null) for (int i = 0; i < EasedStages.Length; i++) if (EasedStages[i] == stage) return true; return false; }
        public bool Suits(StageEventDefinition d)
        {
            var w = d.suitableWorlds;
            if (w == null || w.Length == 0) return false;
            for (int i = 0; i < w.Length; i++)
            {
                if (w[i] == "all") return true;
                if (Tags != null) for (int t = 0; t < Tags.Length; t++) if (Tags[t] == w[i]) return true;
            }
            return false;
        }
    }

    /// <summary>
    /// The endless weighted ladder (blueprint A2 distance table): a pure function of (ladder asset, run seed, mode id, world tags, eased stages).
    /// Stage k covers [k x 1000, (k+1) x 1000): training 0, Road-Eater 1, physics mutation 2, movement 3, environment 4, two compatible modifiers 5,
    /// elite 6, surface combo 7, boss 8, extreme 9, survival gauntlet 10, then controlled random. Per stage one primary (tier weights 40/35/20/5 among the
    /// suitable events) and, where the row says so, one stacked compatible mutation at least 100 m later. Rules: no same primary twice in a row, a hazard
    /// family at most twice in four stages, a breather stage after two hard stages, the matrix decides every overlap. Seed per stage is
    /// unchecked(runSeed + stageIndex + modeId). Every stage consumes the same draws, so a later stage never shifts an earlier one.
    /// </summary>
    public static class EndlessLadderPlanner
    {
        private const int TierCount = 4;

        public static StagePlan Plan(StageLadderDefinition L, StageRunRecord record, float fromM, StageWorldContext ctx)
        {
            var plan = new StagePlan();
            if (record.runSeed == 0u) { record.runSeed = StageSeed.FallbackRunSeed; plan.Log.Add(new StageLogEntry("run_seed_fallback", -1, string.Empty, "runSeed was 0")); }
            plan.Record = record;
            if (L == null) return plan;
            Fill(L, plan, fromM, ctx);
            plan.Events.Sort((a, b) => { int c = a.StartM.CompareTo(b.StartM); return c != 0 ? c : a.SlotIndex.CompareTo(b.SlotIndex); });
            plan.FinishM = 0f;
            return plan;
        }

        internal static void Fill(StageLadderDefinition L, StagePlan plan, float fromM, StageWorldContext ctx)
        {
            var pool = new List<StageEventDefinition>();
            if (L.eventPool != null)
                foreach (var d in L.eventPool)
                    if (d != null && d.weight > 0f && d.role != StageEventRole.Reward && !d.IsModifier && (d.requires == null || d.requires.Length == 0)) pool.Add(d);

            float len = L.stageLengthM;
            int firstStage = Mathf.Max(1, Mathf.CeilToInt((fromM + (fromM > 0f ? 100f : 0f)) / len));
            string lastPrimary = null;
            int hardRun = 0;
            float prevEnd = fromM; StageEventDefinition prevDef = null;
            var famHistory = new List<string[]>();                 // families of the last stages, index by planned order
            var cands = new List<StageEventDefinition>();
            int jitterSteps = Mathf.Max(0, (int)(L.endlessStartJitterM / 10f));

            for (int k = firstStage; k <= L.endlessStageCount; k++)
            {
                var rng = new Pcg32(StageSeed.SlotSeed(plan.Record.runSeed, k, plan.Record.modeId));
                int jitter = rng.NextInt(jitterSteps + 1);          // draw 1
                uint variantDraw = rng.NextUInt();                   // draw 2
                float tierRoll = rng.NextFloat01();                  // draw 3
                float pickRoll = rng.NextFloat01();                  // draw 4
                uint terrainDraw = rng.NextUInt();                   // draw 5
                float stackTierRoll = rng.NextFloat01();             // draw 6
                float stackPickRoll = rng.NextFloat01();             // draw 7
                int stackOffset = rng.NextInt(11);                   // draw 8
                uint stackVariant = rng.NextUInt();                  // draw 9
                uint stackTerrain = rng.NextUInt();                  // draw 10

                var row = L.RowFor(k);
                float stageStart = k * len;
                bool eased = ctx.IsEased(k);
                StageEventDefinition primary = null;

                if (row.fixedEvent != null) primary = row.fixedEvent;
                else
                {
                    if (hardRun >= L.hardStagesBeforeBreather)
                    {
                        hardRun = 0; famHistory.Add(null);
                        plan.Log.Add(new StageLogEntry("breather_inserted", k, string.Empty, "two hard stages"));
                        continue;
                    }
                    primary = Pick(L, pool, cands, ctx, row, row.tag, lastPrimary, famHistory, null, eased, tierRoll, pickRoll, plan, k, true);
                    if (primary == null)
                    {
                        hardRun = 0; famHistory.Add(null);
                        plan.Log.Add(new StageLogEntry("breather_inserted", k, string.Empty, "no suitable event"));
                        continue;
                    }
                }

                float plen = primary.lengthM;
                float start;
                if (row.fixedEvent != null) start = Mathf.Max(stageStart, prevEnd > 0f ? prevEnd + StageLadderPlanner.GapM(L, prevDef, primary, prevEnd) : 0f);
                else
                {
                    start = stageStart + 10f * jitter;
                    if (prevDef != null) start = Mathf.Max(start, prevEnd + StageLadderPlanner.GapM(L, prevDef, primary, prevEnd));
                }
                string why = StageLadderPlanner.Evaluate(plan, L, primary, start, plen, true);
                if (why != null)
                {
                    plan.Log.Add(new StageLogEntry("event_rejected", k, primary.id, why));
                    hardRun = 0; famHistory.Add(null);
                    continue;
                }

                var st = StageLadderPlanner.MakeState(L, primary, k * 10, start, plen, StageLadderPlanner.VariantOf(variantDraw, primary), terrainDraw);
                plan.Events.Add(st);
                prevEnd = st.EndM; prevDef = primary; lastPrimary = primary.id;
                bool hard = primary.difficultyCost >= L.endlessHardCost;
                var fams = new List<string> { primary.hazardFamily };

                if (row.concurrent >= 2 && L.endlessModifierCap >= 2 && stageStart >= L.stackGapFromM && primary.role == StageEventRole.Primary)
                {
                    var second = Pick(L, pool, cands, ctx, row, null, primary.id, famHistory, primary, eased, stackTierRoll, stackPickRoll, plan, k, false);
                    if (second != null)
                    {
                        float sStart = start + L.activationGapM + 10f * stackOffset;
                        string why2 = StageLadderPlanner.Evaluate(plan, L, second, sStart, second.lengthM, true);
                        if (why2 == null)
                        {
                            var s2 = StageLadderPlanner.MakeState(L, second, k * 10 + 1, sStart, second.lengthM, StageLadderPlanner.VariantOf(stackVariant, second), stackTerrain);
                            plan.Events.Add(s2);
                            if (s2.EndM > prevEnd) { prevEnd = s2.EndM; prevDef = second; }
                            fams.Add(second.hazardFamily);
                            if (second.difficultyCost >= L.endlessHardCost) hard = true;
                        }
                        else plan.Log.Add(new StageLogEntry("event_rejected", k, second.id, why2));
                    }
                }
                famHistory.Add(fams.ToArray());
                hardRun = hard ? hardRun + 1 : 0;
            }
        }

        /// <summary>Tier roll, then a weighted pick inside the tier. Filters relax in a fixed order (tag, tier range, family window) so a stage is never empty without a reason.</summary>
        private static StageEventDefinition Pick(StageLadderDefinition L, List<StageEventDefinition> pool, List<StageEventDefinition> cands, StageWorldContext ctx,
            EndlessStageRow row, string tag, string notId, List<string[]> famHistory, StageEventDefinition stackedOn, bool eased,
            float tierRoll, float pickRoll, StagePlan plan, int stage, bool primaryPick)
        {
            // relax levels: 0 all filters, 1 without tag, 2 without tier range. The family window is a hard rule: nothing relaxes it (an empty pool becomes a breather).
            StageTier minTier = eased && row.minTier > StageTier.Easy ? row.minTier - 1 : row.minTier;     // adaptive easing also lets one easier tier in
            for (int level = 0; level < 3; level++)
            {
                cands.Clear();
                for (int i = 0; i < pool.Count; i++)
                {
                    var d = pool[i];
                    if (d.role != StageEventRole.Primary) continue;                          // Eater and boss only through fixed rows
                    if (d.id == notId || (stackedOn != null && d.id == stackedOn.id)) continue;
                    if (!ctx.Suits(d)) continue;
                    if (stackedOn != null && !(L.matrix != null && L.matrix.CanOverlap(stackedOn.id, d.id))) continue;
                    if (level < 1 && !string.IsNullOrEmpty(tag) && !d.HasTag(tag)) continue;
                    if (level < 2 && (d.Tier < minTier || d.Tier > row.maxTier)) continue;
                    if (FamilyBlocked(L, famHistory, d.hazardFamily)) continue;
                    if (stackedOn != null && !string.IsNullOrEmpty(d.hazardFamily) && d.hazardFamily == stackedOn.hazardFamily) continue;
                    cands.Add(d);
                }
                if (cands.Count == 0) continue;
                if (level > 0 && primaryPick) plan.Log.Add(new StageLogEntry("filter_relaxed", stage, string.Empty, "level " + level));
                return Choose(L, cands, eased, tierRoll, pickRoll);
            }
            return null;
        }

        private static bool FamilyBlocked(StageLadderDefinition L, List<string[]> hist, string family)
        {
            if (string.IsNullOrEmpty(family)) return false;
            int window = Mathf.Max(1, L.familyWindowStages - 1), count = 0;
            for (int i = hist.Count - 1, seen = 0; i >= 0 && seen < window; i--, seen++)
                if (hist[i] != null) for (int f = 0; f < hist[i].Length; f++) if (hist[i][f] == family) { count++; break; }
            return count + 1 > L.familyMaxInWindow;
        }

        public static StageEventDefinition Choose(StageLadderDefinition L, List<StageEventDefinition> cands, bool eased, float tierRoll, float pickRoll)
        {
            var w = new float[TierCount]; var any = new bool[TierCount];
            for (int i = 0; i < cands.Count; i++) any[(int)cands[i].Tier] = true;
            float total = 0f;
            for (int t = 0; t < TierCount; t++)
            {
                if (!any[t]) continue;
                float tw = L.tierWeights != null && t < L.tierWeights.Length ? L.tierWeights[t] : 1f;
                if (eased && t >= (int)StageTier.Hard) tw *= L.adaptiveHardMul;
                w[t] = tw; total += tw;
            }
            int tier = 0;
            float r = tierRoll * total, acc = 0f;
            for (int t = 0; t < TierCount; t++) { if (w[t] <= 0f) continue; tier = t; acc += w[t]; if (r < acc) break; }
            float sum = 0f;
            for (int i = 0; i < cands.Count; i++) if ((int)cands[i].Tier == tier) sum += cands[i].weight;
            float pr = pickRoll * sum; acc = 0f;
            StageEventDefinition last = null;
            for (int i = 0; i < cands.Count; i++)
            {
                if ((int)cands[i].Tier != tier) continue;
                last = cands[i]; acc += cands[i].weight;
                if (pr < acc) return cands[i];
            }
            return last;
        }
    }
}
