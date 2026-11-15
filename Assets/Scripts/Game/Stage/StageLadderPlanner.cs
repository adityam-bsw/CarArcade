using System.Collections.Generic;
using UnityEngine;

namespace RoadBuilderGame
{
    /// <summary>The planned ladder of one run: a pure function of (ladder asset, run seed, mode id). Built once in Begin(), never during Tick.</summary>
    public sealed class StagePlan
    {
        public readonly List<StageEventState> Events = new List<StageEventState>();
        public readonly List<StageLogEntry> Log = new List<StageLogEntry>();
        public StageRunRecord Record;
        /// <summary>Finish gate distance, 0 when the ladder has none (Weighted).</summary>
        public float FinishM;
    }

    /// <summary>
    /// Builds the plan. Draw order per slot is fixed and never reordered: 1 jitter, 2 variant, 3 replacement choice (only after a rejection),
    /// 4 terrain seed. Slot RNGs are independent (seed = unchecked(runSeed + stageIndex + modeId)). No Unity random, no time.
    /// </summary>
    public static class StageLadderPlanner
    {
        private const float Eps = 0.01f;

        public static StagePlan Build(StageLadderDefinition ladder, StageRunRecord record)
        {
            var plan = new StagePlan();
            if (record.runSeed == 0u) { record.runSeed = StageSeed.FallbackRunSeed; plan.Log.Add(new StageLogEntry("run_seed_fallback", -1, string.Empty, "runSeed was 0")); }
            plan.Record = record;
            if (ladder == null) return plan;
            if (ladder.ladderMode == StageLadderMode.Endless) EndlessLadderPlanner.Fill(ladder, plan, 0f, default);
            else if (ladder.ladderMode == StageLadderMode.Weighted) BuildWeighted(ladder, plan);
            else BuildSpine(ladder, plan);
            plan.Events.Sort(CompareByStart);
            plan.FinishM = ComputeFinish(ladder, plan);
            return plan;
        }

        private static int CompareByStart(StageEventState a, StageEventState b)
        {
            int c = a.StartM.CompareTo(b.StartM);
            return c != 0 ? c : a.SlotIndex.CompareTo(b.SlotIndex);
        }

        private static float ComputeFinish(StageLadderDefinition ladder, StagePlan plan)
        {
            if (ladder.ladderMode != StageLadderMode.FixedSpine) return 0f;
            float after = 0f;
            foreach (var e in plan.Events) if (e.Definition.role == StageEventRole.Boss && e.EndM + ladder.finishAfterBossM > after) after = e.EndM + ladder.finishAfterBossM;
            return Mathf.Max(ladder.finishMinM, after);
        }

        // ------------------------------------------------------------------ shared helpers

        public static float SlotLength(StageLadderSlot s) { return s.lengthM > 0f ? s.lengthM : s.eventDefinition.lengthM; }

        /// <summary>Minimum clear road between the end of <paramref name="prev"/> (at <paramref name="atM"/>) and the start of <paramref name="next"/>: the stricter of metres and seconds.</summary>
        public static float GapM(StageLadderDefinition ladder, StageEventDefinition prev, StageEventDefinition next, float atM)
        {
            if (prev == null) return 0f;
            if (prev.role == StageEventRole.Reward) return prev.cooldownAfterM;       // the gauntlet is itself the breather
            if (next != null && next.role == StageEventRole.Reward) return ladder.rewardMinGapM;
            float seconds = ladder.Pace.MetresForSeconds(atM, prev.minBreatherS, prev.paceSpeedFactor);
            return Mathf.Max(prev.cooldownAfterM, seconds);
        }

        /// <summary>Fills lock, signal and recovery distances from the event data. Signal distances use the speed floor so a boosting car keeps the full lead.</summary>
        public static void FillDistances(StageLadderDefinition ladder, StageEventState st)
        {
            var d = st.Definition;
            float floor = ladder.signalSpeedFloorMps;
            st.LockM = st.StartM - d.lockDistanceM;
            float lead = d.announcerLeadS * floor;
            if (d.role == StageEventRole.Monster) lead = ladder.eaterWarnLeadM;      // the Eater warns at eaterWarnM (940 m): S1 is the start of the 6 s Warn state, not 5 s x speed floor
            st.WarnM = st.StartM - lead;
            st.SignM = st.StartM - d.signLeadS * floor;
            st.CueM = st.StartM - d.cueLeadS * floor;
            st.RecoveryEndM = st.EndM + d.cooldownAfterM;
        }

        /// <summary>Overlap = shared Active metres, or one Active window inside the other's Warning phase. A Reward (ring gauntlet) is safe, so it never counts in the warning term: its signals, and the boss signals, may begin inside it.</summary>
        public static bool Overlaps(StageEventState a, StageEventState b)
        {
            if (a.StartM < b.EndM && b.StartM < a.EndM) return true;
            if (a.Definition.role == StageEventRole.Reward || b.Definition.role == StageEventRole.Reward) return false;
            if (a.StartM < b.StartM && a.EndM > b.WarnM) return true;   // a is Active inside b's Warning
            if (b.StartM < a.StartM && b.EndM > a.WarnM) return true;   // b is Active inside a's Warning
            return false;
        }

        private static bool MatrixAllows(StageLadderDefinition l, string a, string b) { return l.matrix != null && l.matrix.CanOverlap(a, b); }

        /// <summary>Null when the candidate is legal at <paramref name="start"/>, otherwise the rejection reason.</summary>
        internal static string Evaluate(StagePlan plan, StageLadderDefinition L, StageEventDefinition def, float start, float len, bool endless = false)
        {
            if (!endless && start < def.minDistanceM - Eps) return "window_min";
            if (!endless && def.maxDistanceM > 0f && start + len > def.maxDistanceM + Eps) return "window_max";
            if (def.role != StageEventRole.Reward && def.role != StageEventRole.Modifier && start < L.noHazardUntilM - Eps) return "warmup";
            var t = new StageEventState { Definition = def, StartM = start, EndM = start + len };
            FillDistances(L, t);
            for (int i = 0; i < plan.Events.Count; i++)
            {
                var e = plan.Events[i];
                bool overlap = Overlaps(t, e);
                if (overlap)
                {
                    if (!MatrixAllows(L, def.id, e.Id)) return "matrix:" + def.id + "/" + e.Id;
                    continue;
                }
                float need = L.matrix != null ? L.matrix.AdjacentMinGapM(def.id, e.Id) : 0f;
                if (need > 0f)
                {
                    float gap = t.StartM >= e.EndM ? t.StartM - e.EndM : e.StartM - t.EndM;
                    if (gap < need - Eps) return "adjacent:" + def.id + "/" + e.Id;
                }
            }
            if (!endless && def.role != StageEventRole.Modifier && def.requires != null)
                foreach (var r in def.requires)
                {
                    bool met = false;
                    for (int i = 0; i < plan.Events.Count && !met; i++) met = plan.Events[i].Id == r && plan.Events[i].StartM <= start && start <= plan.Events[i].EndM;
                    if (!met) return "requires:" + r;
                }
            return null;
        }

        internal static StageEventState MakeState(StageLadderDefinition L, StageEventDefinition def, int slot, float start, float len, int variant, uint terrain)
        {
            var st = new StageEventState { Definition = def, SlotIndex = slot, StartM = start, EndM = start + len, Variant = variant, TerrainSeed = terrain, Phase = StagePhase.Planned };
            FillDistances(L, st);
            return st;
        }

        internal static int VariantOf(uint draw, StageEventDefinition def) { return (int)(draw % (uint)Mathf.Max(1, def.variantCount)); }

        // ------------------------------------------------------------------ fixed spine (the slice)

        private static void BuildSpine(StageLadderDefinition L, StagePlan plan)
        {
            var slots = L.slots ?? new StageLadderSlot[0];
            int n = slots.Length;
            // Backward pass: the latest legal start of each primary so that the next primary can still start at its latest.
            var latest = new float[n];
            StageEventDefinition nextDef = null; float nextLatest = 0f;
            for (int i = n - 1; i >= 0; i--)
            {
                var s = slots[i];
                if (s.eventDefinition == null || s.eventDefinition.IsModifier) continue;
                float len = SlotLength(s);
                float lat = s.startRangeMaxM;
                if (nextDef != null && !s.IsFixedAnchor)
                {
                    float x = Mathf.Min(lat, nextLatest - len - s.eventDefinition.cooldownAfterM);
                    for (int it = 0; it < 4; it++) x = Mathf.Min(lat, nextLatest - len - GapM(L, s.eventDefinition, nextDef, x + len));
                    lat = x;
                }
                latest[i] = lat; nextDef = s.eventDefinition; nextLatest = lat;
            }

            StageEventState prev = null;
            for (int i = 0; i < n; i++)
            {
                var s = slots[i];
                var def = s.eventDefinition;
                if (def == null) continue;
                var rng = new Pcg32(StageSeed.SlotSeed(plan.Record.runSeed, i, plan.Record.modeId));
                int jit = (int)def.startJitterM;
                int jitter = rng.NextRange(-jit, jit);                                  // draw 1
                uint variantDraw = rng.NextUInt();                                      // draw 2
                float len = SlotLength(s);
                float want = Mathf.Clamp(s.nominalStartM + jitter, s.startRangeMinM, s.startRangeMaxM);

                StageEventState placed = def.IsModifier
                    ? TryPlaceModifier(plan, L, s, i, want, variantDraw, ref rng)
                    : TryPlacePrimary(plan, L, s, i, want, latest[i], prev, variantDraw, ref rng);
                if (placed == null) continue;
                plan.Events.Add(placed);
                if (!def.IsModifier) prev = placed;
            }
        }

        private static StageEventState TryPlacePrimary(StagePlan plan, StageLadderDefinition L, StageLadderSlot s, int slotIndex, float want, float latest,
            StageEventState prev, uint variantDraw, ref Pcg32 rng)
        {
            var def = s.eventDefinition;
            float len = SlotLength(s);
            float start;
            float earliest = s.startRangeMinM;
            if (prev != null) earliest = Mathf.Max(earliest, prev.EndM + GapM(L, prev.Definition, def, prev.EndM));
            if (s.IsFixedAnchor)
            {
                start = s.startRangeMinM;
                if (earliest > start + Eps) plan.Log.Add(new StageLogEntry("event_breather_violation", slotIndex, def.id, "fixed anchor closer than the breather rule"));
            }
            else
            {
                if (earliest > latest + Eps) { plan.Log.Add(new StageLogEntry("event_dropped_breather", slotIndex, def.id, "no start left inside the window")); return null; }
                start = Mathf.Clamp(want, earliest, latest);
            }

            string why = Evaluate(plan, L, def, start, len);
            if (why == null) return MakeState(L, def, slotIndex, start, len, VariantOf(variantDraw, def), rng.NextUInt());      // draw 4

            plan.Log.Add(new StageLogEntry("event_rejected", slotIndex, def.id, why));
            uint choice = rng.NextUInt();                                                                                       // draw 3
            var repl = PickReplacement(plan, L, def, choice, start, len, out float rStart, out float rLen);
            if (repl == null) { plan.Log.Add(new StageLogEntry("event_dropped_no_candidate", slotIndex, def.id, why)); return null; }
            return MakeState(L, repl, slotIndex, rStart, rLen, VariantOf(variantDraw, repl), rng.NextUInt());
        }

        private static StageEventState TryPlaceModifier(StagePlan plan, StageLadderDefinition L, StageLadderSlot s, int slotIndex, float want, uint variantDraw, ref Pcg32 rng)
        {
            var def = s.eventDefinition;
            float len = SlotLength(s);
            StageEventState parent = null;
            if (def.requires != null)
                foreach (var r in def.requires)
                {
                    for (int i = 0; i < plan.Events.Count && parent == null; i++) if (plan.Events[i].Id == r) parent = plan.Events[i];
                    if (parent != null) break;
                }
            if (parent == null) { plan.Log.Add(new StageLogEntry("event_rejected", slotIndex, def.id, "requires:parent not placed")); return null; }
            float lo = Mathf.Max(s.startRangeMinM, parent.StartM);
            float hi = Mathf.Min(s.startRangeMaxM, parent.EndM - L.modifierEndMarginM - len);
            if (lo > hi + Eps) { plan.Log.Add(new StageLogEntry("event_rejected", slotIndex, def.id, "window:does not fit inside " + parent.Id)); return null; }
            float start = Mathf.Clamp(want, lo, hi);
            int slotsInUse = def.modifierSlots;
            for (int i = 0; i < plan.Events.Count; i++)
            {
                var e = plan.Events[i];
                if (e.Definition.IsModifier && start < e.EndM && e.StartM < start + len) slotsInUse += e.Definition.modifierSlots;
            }
            if (slotsInUse > L.maxModifiers) { plan.Log.Add(new StageLogEntry("event_rejected", slotIndex, def.id, "modifier_cap")); return null; }
            string why = Evaluate(plan, L, def, start, len);
            if (why != null) { plan.Log.Add(new StageLogEntry("event_rejected", slotIndex, def.id, why)); return null; }
            return MakeState(L, def, slotIndex, start, len, VariantOf(variantDraw, def), rng.NextUInt());
        }

        /// <summary>Next candidate by descending weight, ties broken by the slot RNG (<paramref name="choice"/> rotates the tie order).</summary>
        private static StageEventDefinition PickReplacement(StagePlan plan, StageLadderDefinition L, StageEventDefinition rejected, uint choice,
            float wantStart, float wantLen, out float start, out float len)
        {
            start = wantStart; len = wantLen;
            var pool = L.eventPool;
            if (pool == null || pool.Length == 0) return null;
            int count = pool.Length;
            int rot = (int)(choice % (uint)count);
            var tried = new List<StageEventDefinition>(count);
            for (int attempt = 0; attempt < count; attempt++)
            {
                StageEventDefinition best = null; float bestW = -1f;
                for (int k = 0; k < count; k++)
                {
                    var c = pool[(k + rot) % count];
                    if (c == null || c == rejected || c.IsModifier || tried.Contains(c) || c.weight <= 0f) continue;
                    if (c.role != rejected.role && !(c.role != StageEventRole.Monster && c.role != StageEventRole.Boss && rejected.role != StageEventRole.Monster && rejected.role != StageEventRole.Boss)) continue;
                    bool placedAlready = false;
                    for (int i = 0; i < plan.Events.Count && !placedAlready; i++) placedAlready = plan.Events[i].Definition == c;
                    if (placedAlready) continue;
                    if (c.weight > bestW) { best = c; bestW = c.weight; }
                }
                if (best == null) return null;
                tried.Add(best);
                float cl = best.lengthM;
                float cs = wantStart;
                StageEventState prev = null;
                for (int i = 0; i < plan.Events.Count; i++) if (!plan.Events[i].Definition.IsModifier && plan.Events[i].EndM <= cs + Eps) prev = plan.Events[i];
                if (prev != null) cs = Mathf.Max(cs, prev.EndM + GapM(L, prev.Definition, best, prev.EndM));
                string why = Evaluate(plan, L, best, cs, cl);
                if (why == null) { start = cs; len = cl; return best; }
                plan.Log.Add(new StageLogEntry("event_rejected", -1, best.id, why));
            }
            return null;
        }

        // ------------------------------------------------------------------ weighted ladder (endless, post-slice)

        private static void BuildWeighted(StageLadderDefinition L, StagePlan plan)
        {
            var pool = L.eventPool ?? new StageEventDefinition[0];
            StageEventState prev = null;
            string lastId = null;                      // last primary id, kept across breathers (never the same primary twice)
            int hardRun = 0;
            float pos = L.noHazardUntilM;
            var cands = new List<StageEventDefinition>(pool.Length);
            var tried = new List<StageEventDefinition>(pool.Length);
            for (int n = 0; n < L.weightedMaxEvents * 4 && plan.Events.Count < L.weightedMaxEvents && pos < L.weightedLengthM; n++)
            {
                var rng = new Pcg32(StageSeed.SlotSeed(plan.Record.runSeed, n, plan.Record.modeId));
                int steps = Mathf.Max(0, (int)((L.weightedRecoveryMaxM - L.weightedRecoveryMinM) / 10f));
                float recovery = L.weightedRecoveryMinM + 10f * rng.NextInt(steps + 1);       // draw 1
                uint variantDraw = rng.NextUInt();                                              // draw 2

                if (prev != null && hardRun >= L.hardStagesBeforeBreather)
                {
                    pos = prev.EndM + Mathf.Max(L.weightedBreatherM, recovery);                   // a breather slot: no event
                    hardRun = 0; prev = null; plan.Log.Add(new StageLogEntry("breather_inserted", n, string.Empty, "hard-stage rule")); continue;
                }

                StageEventDefinition prevDef = prev != null ? prev.Definition : null;
                cands.Clear(); float total = 0f;
                foreach (var c in pool)
                {
                    if (c == null || c.IsModifier || c.weight <= 0f) continue;
                    if (c.difficultyCost > 3 + n) continue;                                      // difficulty ramp
                    if (lastId != null && lastId == c.id) continue;                         // never the same primary twice
                    float start = StartFor(L, prev, c, pos, recovery);
                    if (start < c.minDistanceM || (c.maxDistanceM > 0f && start + c.lengthM > c.maxDistanceM)) continue;
                    cands.Add(c); total += c.weight;
                }
                if (cands.Count == 0) { pos += Mathf.Max(L.weightedBreatherM, recovery); prev = null; hardRun = 0; plan.Log.Add(new StageLogEntry("breather_inserted", n, string.Empty, "empty pool")); continue; }

                float r = rng.NextFloat01() * total;
                StageEventDefinition pick = cands[cands.Count - 1];
                float acc = 0f;
                foreach (var c in cands) { acc += c.weight; if (r < acc) { pick = c; break; } }

                tried.Clear();
                StageEventDefinition chosen = null; float chosenStart = 0f;
                int rot = 0; bool rotDrawn = false;
                var attempt = pick;
                for (int guard = 0; guard < cands.Count + 1 && attempt != null; guard++)
                {
                    tried.Add(attempt);
                    float start = StartFor(L, prev, attempt, pos, recovery);
                    string why = Evaluate(plan, L, attempt, start, attempt.lengthM);
                    if (why == null) { chosen = attempt; chosenStart = start; break; }
                    plan.Log.Add(new StageLogEntry("event_rejected", n, attempt.id, why));
                    if (!rotDrawn) { rot = (int)(rng.NextUInt() % (uint)cands.Count); rotDrawn = true; }   // draw 3
                    attempt = null; float bestW = -1f;
                    for (int k = 0; k < cands.Count; k++)
                    {
                        var c = cands[(k + rot) % cands.Count];
                        if (tried.Contains(c)) continue;
                        if (c.weight > bestW) { attempt = c; bestW = c.weight; }
                    }
                }
                if (chosen == null) { pos += Mathf.Max(L.weightedBreatherM, recovery); prev = null; hardRun = 0; plan.Log.Add(new StageLogEntry("breather_inserted", n, string.Empty, "all candidates rejected")); continue; }

                var st = MakeState(L, chosen, n, chosenStart, chosen.lengthM, VariantOf(variantDraw, chosen), rng.NextUInt());   // draw 4
                plan.Events.Add(st);
                prev = st; pos = st.EndM; lastId = chosen.id;
                hardRun = chosen.IsHard ? hardRun + 1 : 0;
            }
        }

        private static float StartFor(StageLadderDefinition L, StageEventState prev, StageEventDefinition c, float pos, float recovery)
        {
            if (prev == null) return Mathf.Max(pos, c.minDistanceM);
            return Mathf.Max(pos + recovery, prev.EndM + GapM(L, prev.Definition, c, prev.EndM), c.minDistanceM);
        }
    }
}
