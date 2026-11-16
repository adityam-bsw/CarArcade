using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoadBuilderGame
{
    /// <summary>Counter values the unlock gates read. Most are plain counters; a few are derived from other saved data.</summary>
    public static class GateCounters
    {
        public static int Value(PlayerProfile p, ExtraGate g)
        {
            var t = ProgressionTuningSO.Load();
            int raw = p.Counter(GateService.ExtraCounterId(g));
            switch (g)
            {
                case ExtraGate.WaterMastery: return raw + p.Counter("surf-metres:water") / Mathf.Max(1, t.waterMasteryMetres);
                // Lava has real raisers (a lava surface driven lavaSurviveMetres, or the EVT-LAVACHASE event ending) so it never needs the fallback.
                case ExtraGate.SurviveLava: return raw;
                // Rhythm is raised by the EVT-RHYTHM event ending; the long-run fallback only applies while that event asset does not exist.
                case ExtraGate.RhythmChallenge: return raw + (t.fallbackForMissingEvents && !RhythmEventExists() ? p.Counter("gate:LongRuns") : 0);
                case ExtraGate.SurfaceTutorial:
                    int learned = 0;
                    if (p.surfaceSeenCount != null) for (int i = 0; i < p.surfaceSeenCount.Count; i++) if (p.surfaceSeenCount[i].count >= 3) learned++;
                    return raw + learned;
                case ExtraGate.WorldMastery: return raw + MasteredWorlds(p, t);
                default: return raw;
            }
        }

        private static int _rhythmEvent = -1;
        /// <summary>True once the Rhythm Gates stage event (EVT-RHYTHM) ships; cached, editor reload clears it.</summary>
        public static bool RhythmEventExists()
        {
            if (_rhythmEvent < 0) _rhythmEvent = Resources.Load<StageEventDefinition>("Stage/Events/EVT-RHYTHM") != null ? 1 : 0;
            return _rhythmEvent == 1;
        }

        public static int MasteredWorlds(PlayerProfile p, ProgressionTuningSO t)
        {
            int n = 0;
            if (p.counters != null) for (int i = 0; i < p.counters.Count; i++) if (p.counters[i].id.StartsWith("world-metres:") && p.counters[i].count >= t.worldMasteryMetres) n++;
            return n;
        }
    }

    /// <summary>Vehicle prices and the resolved per-run multipliers of a vehicle.</summary>
    public static class VehicleEconomy
    {
        /// <summary>Coins to unlock a catalogue vehicle: the gate table wins over the catalogue's placeholder cost.</summary>
        public static int Price(CatalogEntry e, VehicleGateSO gates)
        {
            if (e == null) return 0;
            return gates != null && gates.TryGet(e.id, out var g) ? g.coins : e.cost;
        }

        /// <summary>Writes the gate prices into the catalogue entries (idempotent). Everything that reads <c>CatalogEntry.cost</c> then shows and charges the blueprint price.</summary>
        public static int SyncCatalog(AssetCatalog cat, VehicleGateSO gates)
        {
            if (cat == null || gates == null) return 0;
            int n = 0;
            foreach (var e in cat.entries)
                if (e != null && e.kind == CatalogKind.Vehicle && gates.TryGet(e.id, out var g) && e.cost != g.coins) { e.cost = g.coins; n++; }
            return n;
        }

        public static int Tier(VehicleGateSO gates, string id) { return gates != null && gates.TryGet(id, out var g) ? g.tier : 1; }

        /// <summary>The multipliers for a run: the vehicle's profile, with the weakest axis moved back toward 1 when the scrap reinforce upgrade is owned.</summary>
        public static VehicleStats Effective(PlayerProfile p, ProgressionTuningSO t, VehicleProfileSO so, string vehicleId)
        {
            var s = VehicleStats.Identity;
            var vp = so != null ? so.Find(vehicleId) : null;
            if (vp == null) return s;
            float ink = vp.ink, flip = vp.flip, boost = vp.boost, impact = vp.impact, speed = vp.speed, torque = vp.torque, accel = vp.accel;
            if (p != null && t != null && p.scrapUpgrades != null && p.scrapUpgrades.Contains("scrap:" + vehicleId))
            {
                var worst = VehicleProfileSO.WeakAxis(vp);
                if (vp.Goodness(worst) < 1f)
                {
                    float f = t.reinforceFraction;
                    switch (worst)
                    {
                        case ProfileAxis.Ink: ink -= (ink - 1f) * f; break;
                        case ProfileAxis.Flip: flip += (1f - flip) * f; break;
                        case ProfileAxis.Boost: boost += (1f - boost) * f; break;
                        case ProfileAxis.Impact: impact += (1f - impact) * f; break;
                        case ProfileAxis.Speed: speed += (1f - speed) * f; break;
                        case ProfileAxis.Torque: torque += (1f - torque) * f; break;
                        default: accel += (1f - accel) * f; break;
                    }
                }
            }
            s.ink = ink; s.flip = flip; s.boost = boost; s.impact = impact; s.speed = speed; s.torque = torque; s.accel = accel; s.crushesFragile = vp.crushesFragile;
            return s;
        }

        /// <summary>The resolved stats of the run in progress. Surface and stage sections may read <c>crushesFragile</c>. Set by the manager at run start.</summary>
        public static VehicleStats Active = VehicleStats.Identity;

        /// <summary>One line for the garage: tier, strength and weakness.</summary>
        public static string ProfileLine(VehicleProfileSO so, VehicleGateSO gates, string vehicleId)
        {
            var vp = so != null ? so.Find(vehicleId) : null;
            if (vp == null) return "";
            var strong = VehicleProfileSO.StrongAxis(vp); var weak = VehicleProfileSO.WeakAxis(vp);
            string s = "TIER " + Tier(gates, vehicleId) + "   ";
            if (vp.crushesFragile) s += "CRUSHES FRAGILE ROAD   ";
            s += "+ " + VehicleProfileSO.StrengthText(strong);
            if (vp.Goodness(weak) < 0.999f) s += "   - " + VehicleProfileSO.WeaknessText(weak);
            else s += "   BALANCED";
            return s;
        }
    }

    /// <summary>Weekly challenge (blueprint D6): three themed challenges, a boss challenge and a special surface challenge, plus a cumulative weekly token reward. Progress counters are keyed by the week (Monday UTC), so a new week starts at zero by itself.</summary>
    public static class WeeklyService
    {
        public static string Key(DateTime utc) { return DailyChallenge.WeekKey(utc); }

        /// <summary>A new week clears the token count (the daily claim adds to it).</summary>
        public static void Roll(PlayerProfile p, DateTime utc)
        {
            string k = Key(utc);
            if (p.weeklyKey != k) { p.weeklyKey = k; p.weeklyTokens = 0; }
        }

        public static string CounterId(string weekKey, WeeklyKind kind) { return "w:" + weekKey + ":" + kind; }

        public static void AddProgress(PlayerProfile p, DateTime utc, WeeklyKind kind, int n)
        {
            if (p == null || n <= 0) return;
            p.AddCounter(CounterId(Key(utc), kind), n);
        }

        public static int Progress(PlayerProfile p, DateTime utc, WeeklyChallengeDef d) { return p.Counter(CounterId(Key(utc), d.kind)); }

        /// <summary>This week's five challenges: three themed (chosen from the pool by the week's seed), the boss challenge and the special surface challenge.</summary>
        public static List<WeeklyChallengeDef> Challenges(ProgressionTuningSO t, DateTime utc)
        {
            var list = new List<WeeklyChallengeDef>(5);
            var pool = new List<WeeklyChallengeDef>(t.weeklyThemed ?? new WeeklyChallengeDef[0]);
            var rng = new Pcg32(DailyChallenge.SeedFor(DateTime.Parse(Key(utc), System.Globalization.CultureInfo.InvariantCulture)));
            for (int i = 0; i < 3 && pool.Count > 0; i++) { int k = rng.NextInt(pool.Count); list.Add(pool[k]); pool.RemoveAt(k); }
            list.Add(t.weeklyBoss); list.Add(t.weeklySurface);
            return list;
        }

        public static string ClaimId(DateTime utc, string id) { return "weekly:" + Key(utc) + ":" + id; }

        public static bool Claim(PlayerProfile p, ProgressionTuningSO t, DateTime utc, string id)
        {
            Roll(p, utc);
            foreach (var d in Challenges(t, utc))
            {
                if (d.id != id) continue;
                if (Progress(p, utc, d) < d.target || p.HasClaim(ClaimId(utc, id))) return false;
                p.AddClaim(ClaimId(utc, id));
                p.coins += d.coins; p.weeklyTokens += 1;
                return true;
            }
            return false;
        }

        public static bool CanClaimTokens(PlayerProfile p, ProgressionTuningSO t, DateTime utc) { Roll(p, utc); return p.weeklyTokens >= t.weeklyTokensNeeded && !p.HasClaim("weeklytokens:" + Key(utc)); }

        public static bool ClaimTokens(PlayerProfile p, ProgressionTuningSO t, DateTime utc)
        {
            if (!CanClaimTokens(p, t, utc)) return false;
            p.AddClaim("weeklytokens:" + Key(utc));
            p.coins += t.weeklyCoins; p.gems += t.weeklyGems;
            return true;
        }
    }

    /// <summary>Road Trip Album (blueprint D6): 30 days, 20 free coin or gem days and 10 optional cosmetic days, all free, none expiring, no pressure.</summary>
    public static class AlbumService
    {
        public static void Start(PlayerProfile p, DateTime utc) { if (string.IsNullOrEmpty(p.albumStart)) p.albumStart = DailyChallenge.DateKey(utc); }

        /// <summary>Highest day that has been reached (1..30). The album opens on day 1 the first time it is looked at.</summary>
        public static int DayReached(PlayerProfile p, DateTime utc, int total)
        {
            if (string.IsNullOrEmpty(p.albumStart)) return 1;
            var s = DateTime.Parse(p.albumStart, System.Globalization.CultureInfo.InvariantCulture);
            return Mathf.Clamp((int)(utc.Date - s.Date).TotalDays + 1, 1, total);
        }

        public static bool Claimed(PlayerProfile p, int day) { return p.HasClaim("album:" + day); }

        public static bool Claim(PlayerProfile p, ProgressionTuningSO t, DateTime utc, int day)
        {
            Start(p, utc);
            if (t.album == null || day < 1 || day > t.album.Length) return false;
            if (day > DayReached(p, utc, t.album.Length) || Claimed(p, day)) return false;
            var d = t.album[day - 1];
            p.AddClaim("album:" + day);
            p.coins += d.coins; p.gems += d.gems;
            if (!string.IsNullOrEmpty(d.cosmeticId)) Cosmetics.Grant(p, t, d.cosmeticId);
            return true;
        }

        public static int ClaimableCount(PlayerProfile p, ProgressionTuningSO t, DateTime utc)
        {
            if (t.album == null) return 0;
            int reached = DayReached(p, utc, t.album.Length), n = 0;
            for (int d = 1; d <= reached; d++) if (!Claimed(p, d)) n++;
            return n;
        }
    }

    /// <summary>Gem sources (blueprint D1): achievements and milestones. Gems also come from mastery level 4, the day-7 daily reward, the first boss, the weekly token reward and the album.</summary>
    public static class AchievementService
    {
        public static float Value(PlayerProfile p, ProgressionTuningSO t, AchievementMetric m)
        {
            switch (m)
            {
                case AchievementMetric.LifetimeMetres: return p.lifetimeDistance;
                case AchievementMetric.BestRunMetres: return p.bestDistance;
                case AchievementMetric.BossDefeats: return p.bossDefeats;
                case AchievementMetric.MissionsDone: return p.Counter(GateService.ExtraCounterId(ExtraGate.Missions));
                case AchievementMetric.CleanJumps: return p.Counter(GateService.ExtraCounterId(ExtraGate.CleanJumps));
                case AchievementMetric.VehiclesOwned: return p.unlockedVehicleIds != null ? p.unlockedVehicleIds.Count : 0;
                case AchievementMetric.WorldsOwned: return p.unlockedWorldIds != null ? p.unlockedWorldIds.Count : 0;
                case AchievementMetric.UpgradeLevels:
                    int sum = 0;
                    if (p.upgrades != null) foreach (var u in p.upgrades) if (u.levels != null) foreach (var l in u.levels) sum += l;
                    return sum;
                case AchievementMetric.MasteryLevels:
                    int best = 0;
                    if (p.vehicleProgress != null) foreach (var v in p.vehicleProgress) best = Mathf.Max(best, MasteryService.LevelFor(t, v.metres));
                    return best;
                default: return p.Counter("daily-claims");
            }
        }

        public static bool Done(PlayerProfile p, ProgressionTuningSO t, AchievementDef a) { return Value(p, t, a.metric) >= a.target; }
        public static bool Claimed(PlayerProfile p, AchievementDef a) { return p.HasClaim("ach:" + a.id); }

        public static bool Claim(PlayerProfile p, ProgressionTuningSO t, string id)
        {
            if (t.achievements == null) return false;
            foreach (var a in t.achievements)
            {
                if (a.id != id) continue;
                if (!Done(p, t, a) || Claimed(p, a)) return false;
                p.AddClaim("ach:" + a.id); p.gems += a.gems;
                return true;
            }
            return false;
        }

        public static int ClaimableCount(PlayerProfile p, ProgressionTuningSO t)
        {
            int n = 0;
            if (t.achievements != null) foreach (var a in t.achievements) if (Done(p, t, a) && !Claimed(p, a)) n++;
            return n;
        }
    }

    /// <summary>Local daily leaderboard (blueprint D6): the best scores per UTC day, top N, saved in the profile.</summary>
    public static class LeaderboardService
    {
        public const int KeepDays = 14;

        public static void Submit(PlayerProfile p, string dateKey, int score, string vehicleId, int topN)
        {
            if (p.dailyBoard == null) p.dailyBoard = new List<BoardEntry>();
            if (score <= 0) return;
            p.dailyBoard.Add(new BoardEntry { date = dateKey, vehicleId = vehicleId ?? "", score = score });
            var today = Top(p, dateKey, int.MaxValue);
            for (int i = Mathf.Max(1, topN); i < today.Count; i++) p.dailyBoard.Remove(today[i]);
            DateTime d;
            if (DateTime.TryParse(dateKey, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out d))
            {
                string cutoff = DailyChallenge.DateKey(d.AddDays(-KeepDays));
                p.dailyBoard.RemoveAll(e => string.CompareOrdinal(e.date, cutoff) < 0);
            }
        }

        /// <summary>Entries of a day, best first.</summary>
        public static List<BoardEntry> Top(PlayerProfile p, string dateKey, int n)
        {
            var l = new List<BoardEntry>();
            if (p.dailyBoard != null) foreach (var e in p.dailyBoard) if (e.date == dateKey) l.Add(e);
            l.Sort((a, b) => b.score.CompareTo(a.score));
            if (l.Count > n) l.RemoveRange(n, l.Count - n);
            return l;
        }
    }

    /// <summary>Scrap sources and sinks (blueprint D1): dismantling a vehicle, duplicate cosmetics (see Cosmetics.Grant), the boss reward (ProgressionEvents.BossDefeated), and the scrap-priced vehicle-specific reinforce upgrade.</summary>
    public static class ScrapService
    {
        /// <summary>Vehicles that cannot be dismantled: the starter, and the slice pair that the profile flags re-own on load.</summary>
        public static bool CanDismantle(string id) { return !string.IsNullOrEmpty(id) && id != "vehicle-car" && id != "vehicle-sports-car" && id != "vehicle-monster-truck"; }

        public static int DismantleValue(ProgressionTuningSO t, VehicleGateSO gates, string id)
        {
            int price = gates != null && gates.TryGet(id, out var g) ? g.coins : 0;
            return Mathf.Clamp(price / Mathf.Max(1, t.dismantleCoinsPerScrap), t.dismantleMinScrap, t.dismantleMaxScrap);
        }

        /// <summary>Removes an owned vehicle from the garage for scrap. Upgrades and mastery stay in the save, so buying it again restores them.</summary>
        public static int Dismantle(PlayerProfile p, ProgressionTuningSO t, VehicleGateSO gates, string id)
        {
            if (!CanDismantle(id) || p.unlockedVehicleIds == null || !p.unlockedVehicleIds.Contains(id)) return 0;
            int scrap = DismantleValue(t, gates, id);
            p.unlockedVehicleIds.Remove(id);
            MasteryService.AddScrap(p, scrap);
            return scrap;
        }

        public static int ReinforceCost(ProgressionTuningSO t, VehicleGateSO gates, string vehicleId) { return t.reinforceScrapBase + t.reinforceScrapPerTier * VehicleEconomy.Tier(gates, vehicleId); }

        public static bool HasReinforce(PlayerProfile p, string vehicleId) { return p.scrapUpgrades != null && p.scrapUpgrades.Contains("scrap:" + vehicleId); }

        public static bool CanReinforce(VehicleProfileSO so, string vehicleId) { var vp = so.Find(vehicleId); return vp != null && vp.Goodness(VehicleProfileSO.WeakAxis(vp)) < 0.999f; }

        public static bool BuyReinforce(PlayerProfile p, ProgressionTuningSO t, VehicleGateSO gates, VehicleProfileSO so, string vehicleId)
        {
            if (HasReinforce(p, vehicleId) || !CanReinforce(so, vehicleId)) return false;
            if (!MasteryService.SpendScrap(p, ReinforceCost(t, gates, vehicleId))) return false;
            p.scrapUpgrades.Add("scrap:" + vehicleId);
            return true;
        }
    }

    /// <summary>Which first-hour steps the profile has already done (the checklist ticks itself; the reward is claimed with a tap). Never forced.</summary>
    public static class FirstHourTracker
    {
        public static bool IsDone(PlayerProfile p, string stepId)
        {
            switch (stepId)
            {
                case "first-drive": return p.lifetimeDistance > 0f || p.bestDistance > 0f;
                case "first-500m": return Mathf.Max(p.lifetimeDistance, p.bestDistance) >= 500f || p.bestDistance >= 500f;
                case "first-upgrade":
                    if (p.upgrades != null) foreach (var u in p.upgrades) if (u.levels != null) foreach (var l in u.levels) if (l > 0) return true;
                    return false;
                case "first-monster": return p.Counter(GateService.ExtraCounterId(ExtraGate.FirstMonster)) > 0 || p.eaterEscapes + p.eaterTimeouts > 0;
                case "second-vehicle": return p.unlockedVehicleIds != null && p.unlockedVehicleIds.Count > 0;
                case "first-surface": return p.unlockedSurfaceIds != null && p.unlockedSurfaceIds.Count > 2;
                case "first-boss": return p.bossDefeats > 0;
                case "first-world": return p.unlockedWorldIds != null && p.unlockedWorldIds.Count > 1;
                case "first-mastery":
                    if (p.vehicleProgress != null) foreach (var v in p.vehicleProgress) if (v.masteryClaimedMask != 0) return true;
                    return false;
                case "daily-challenge": return p.Counter("daily-claims") > 0;
                default: return false;
            }
        }

        public static int ClaimableCount(PlayerProfile p, ProgressionTuningSO t)
        {
            int n = 0;
            for (int i = 0; i < t.firstHour.Length; i++) if (IsDone(p, t.firstHour[i].id) && !p.firstHourClaimed.Contains(t.firstHour[i].id) && (t.firstHour[i].coins > 0 || t.firstHour[i].gems > 0)) n++;
            return n;
        }
    }

    /// <summary>One call at the end of a run: the counters that gates, mastery goals and the weekly challenge read, and the daily leaderboard.</summary>
    public static class RunProgress
    {
        public static void Record(PlayerProfile p, ProgressionTuningSO t, SurfaceGateSO surfaces, RunSummary r)
        {
            if (p == null || t == null) return;
            var utc = r.utc == default(DateTime) ? DateTime.UtcNow : r.utc;
            WeeklyService.Roll(p, utc);
            int metres = Mathf.FloorToInt(Mathf.Max(0f, r.distance));
            WeeklyService.AddProgress(p, utc, WeeklyKind.Distance, metres);
            WeeklyService.AddProgress(p, utc, WeeklyKind.CleanJumps, r.cleanJumps);
            WeeklyService.AddProgress(p, utc, WeeklyKind.NearMisses, r.nearMisses);
            WeeklyService.AddProgress(p, utc, WeeklyKind.Flips, r.flips);
            WeeklyService.AddProgress(p, utc, WeeklyKind.CoinsEarned, r.coins);
            if (surfaces != null && !string.IsNullOrEmpty(r.surfaceId) && surfaces.TryGet(r.surfaceId, out var sg) && sg.category >= SurfaceCategory.Advanced)
                WeeklyService.AddProgress(p, utc, WeeklyKind.SpecialSurfaceMetres, metres);
            if (!string.IsNullOrEmpty(r.worldId)) p.AddCounter("world-metres:" + r.worldId, metres);
            if (Array.IndexOf(t.waterSurfaceIds, r.surfaceId) >= 0) p.AddCounter("surf-metres:water", metres);
            if (Array.IndexOf(t.lavaSurfaceIds, r.surfaceId) >= 0 && r.distance >= t.lavaSurviveMetres) ProgressionEvents.Record(p, ProgressionEvent.LavaStageSurvived);
            if (r.distance >= t.fallbackLongRunMetres) p.AddCounter("gate:LongRuns", 1);
            if (!string.IsNullOrEmpty(r.vehicleKey))
            {
                if (Array.IndexOf(t.lowGravityWorldIds, r.worldId) >= 0 && t.lowGravityStageMetres > 0)
                    MasteryService.AddGoal(p, r.vehicleKey, "low-gravity-stages", metres / t.lowGravityStageMetres);
                Cosmetics.CheckGoal(p, t, r.vehicleKey);
            }
            if (r.daily) LeaderboardService.Submit(p, DailyChallenge.DateKey(utc), metres, r.vehicleKey, t.leaderboardSize);
        }
    }

    /// <summary>Daily events the stage ladder must run in a daily challenge run, in order. Section A's ladder reads <see cref="ForcedEvents.Current"/> when it plans a run.</summary>
    public interface IForcedEventList
    {
        /// <summary>Stage event ids (StageEventDefinition.id) to place in the ladder, in the order they should appear. Empty = no forcing.</summary>
        IReadOnlyList<string> EventIds { get; }
    }

    public sealed class FixedForcedEventList : IForcedEventList
    {
        private readonly string[] _ids;
        public FixedForcedEventList(string[] ids) { _ids = ids ?? new string[0]; }
        public IReadOnlyList<string> EventIds { get { return _ids; } }
    }

    /// <summary>Holder for the forced events of the run being started. The manager sets it for a daily challenge and clears it afterwards; the ladder planner reads it.</summary>
    public static class ForcedEvents
    {
        public static IForcedEventList Current;
        public static bool Active { get { return Current != null && Current.EventIds != null && Current.EventIds.Count > 0; } }
    }

    /// <summary>Optional purchase seam (gems). No billing SDK exists in the project; the user picks one later (Google Play Families review and a parent gate first).</summary>
    public interface IPurchaseService
    {
        bool IsAvailable { get; }
        /// <summary>Starts a purchase of a product id and calls back true when it went through.</summary>
        void Purchase(string productId, Action<bool> onResult);
    }

    public sealed class NoOpPurchaseService : IPurchaseService
    {
        public bool IsAvailable { get { return false; } }
        public void Purchase(string productId, Action<bool> onResult) { if (onResult != null) onResult(false); }
    }

    /// <summary>Editor and development builds only: pretends the purchase went through. Never a real charge.</summary>
    public sealed class FakePurchaseService : IPurchaseService
    {
        public bool IsAvailable { get { return true; } }
        public void Purchase(string productId, Action<bool> onResult) { if (onResult != null) onResult(true); }
        public static IPurchaseService CreateDefault()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            return new FakePurchaseService();
#else
            return new NoOpPurchaseService();
#endif
        }
    }

    /// <summary>Expected coins of a run, from the real earning formulas in <see cref="GameplayTuning"/> and <see cref="RunDirector"/>, for a declared player profile. EditMode tests check it against the D5 bands.</summary>
    public static class EarningsModel
    {
        public struct Profile
        {
            public float coinsPickedPer100m, avgMultiplier, jumpsPer100m, avgAirSeconds, flipsPer100m, nearMissesPer100m;
            /// <summary>A median player: invented, not measured (see Docs/PROGRESSION.md). Coins picked is per 100 m at full coin density; the model scales it by coinShapeKeepChance.</summary>
            public static Profile Median => new Profile { coinsPickedPer100m = 4f, avgMultiplier = 1.5f, jumpsPer100m = 0.6f, avgAirSeconds = 1.2f, flipsPer100m = 0.1f, nearMissesPer100m = 0f };
        }

        /// <summary>Coins from 250 m chests, pickups, air time, flips and near misses for a run of <paramref name="metres"/> metres (record bonus and event payouts excluded).</summary>
        public static float Expected(GameplayTuning g, Profile p, float metres)
        {
            float total = 0f;
            if (g.milestoneEvery > 0f)
            {
                int chests = Mathf.FloorToInt(metres / g.milestoneEvery);
                for (int k = 1; k <= chests; k++) total += g.milestoneCoins * (g.milestoneIndexCap > 0 ? Mathf.Min(k, g.milestoneIndexCap) : k);
            }
            float per100 = metres / 100f;
            total += per100 * p.coinsPickedPer100m * g.coinShapeKeepChance * g.coinValue * p.avgMultiplier;
            float airPay = Mathf.Max(0f, Mathf.RoundToInt((Mathf.Min(p.avgAirSeconds, g.airMaxPaidSeconds) - g.airMinSeconds + 0.5f) * g.airCoinsPerSecond));
            total += per100 * p.jumpsPer100m * airPay;
            total += per100 * p.flipsPer100m * g.flipCoins;
            total += per100 * p.nearMissesPer100m * g.nearMissCoins;
            return total;
        }

        /// <summary>Coins the old formula (chest pay = milestoneCoins x index with no cap) would pay from chests alone.</summary>
        public static float UncappedChests(GameplayTuning g, float metres)
        {
            float total = 0f; int chests = g.milestoneEvery > 0f ? Mathf.FloorToInt(metres / g.milestoneEvery) : 0;
            for (int k = 1; k <= chests; k++) total += g.milestoneCoins * k;
            return total;
        }
    }
}
