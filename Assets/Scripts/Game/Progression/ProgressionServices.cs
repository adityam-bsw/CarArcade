using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoadBuilderGame
{
    /// <summary>Upgrade levels and purchases per vehicle. Pure logic over PlayerProfile and UpgradeTreeSO, no scene objects.</summary>
    public static class UpgradeService
    {
        public static int Level(PlayerProfile p, string key, UpgradeBranch b) { return p.UpgradesFor(key).levels[(int)b]; }

        public static float Bonus(UpgradeTreeSO tree, string key, UpgradeBranch b, int level) { return tree.Bonus(b, key, level); }

        /// <summary>Buys the next level with coins from the profile. False when maxed or too poor.</summary>
        public static bool TryBuy(PlayerProfile p, string key, UpgradeBranch b, UpgradeTreeSO tree)
        {
            var e = p.UpgradesFor(key);
            int cost = tree.Cost(e.levels[(int)b]);
            if (cost < 0 || p.coins < cost) return false;
            p.coins -= cost;
            e.levels[(int)b]++;
            return true;
        }
    }

    public struct GateStatus { public bool AlreadyUnlocked, DistanceMet, ExtraMet, Coins; public int CoinCost; public bool CanBuy => AlreadyUnlocked || (DistanceMet && ExtraMet); }

    /// <summary>Distance and extra gates for NEW unlocks (blueprint D2/D4). Anything already in the profile's unlocked lists stays unlocked whatever the table says.</summary>
    public static class GateService
    {
        public static string ExtraCounterId(ExtraGate g) { return "gate:" + g; }

        public static GateStatus World(PlayerProfile p, WorldGateSO t, string worldId)
        {
            var s = new GateStatus { AlreadyUnlocked = p.unlockedWorldIds != null && p.unlockedWorldIds.Contains(worldId) };
            if (t == null || !t.TryGet(worldId, out var g)) { s.DistanceMet = s.ExtraMet = true; return s; }
            s.CoinCost = g.coins;
            s.DistanceMet = p.lifetimeDistance >= g.unlockMetres;
            s.ExtraMet = g.extra == ExtraGate.None || p.Counter(ExtraCounterId(g.extra)) >= g.extraCount;
            return s;
        }

        public static GateStatus Vehicle(PlayerProfile p, VehicleGateSO t, string vehicleId)
        {
            var s = new GateStatus { AlreadyUnlocked = p.unlockedVehicleIds != null && p.unlockedVehicleIds.Contains(vehicleId), ExtraMet = true };
            if (t == null || !t.TryGet(vehicleId, out var g)) { s.DistanceMet = true; return s; }
            s.CoinCost = g.coins;
            s.DistanceMet = p.lifetimeDistance >= g.unlockMetres;
            return s;
        }

        /// <summary>Buys (unlocks) a world when its gates are met and coins suffice. Already-unlocked worlds return true at no cost.</summary>
        public static bool TryUnlockWorld(PlayerProfile p, WorldGateSO t, string worldId)
        {
            var s = World(p, t, worldId);
            if (s.AlreadyUnlocked) return true;
            if (!s.CanBuy || p.coins < s.CoinCost) return false;
            p.coins -= s.CoinCost;
            p.unlockedWorldIds.Add(worldId);
            return true;
        }

        public static bool TryUnlockVehicle(PlayerProfile p, VehicleGateSO t, string vehicleId)
        {
            var s = Vehicle(p, t, vehicleId);
            if (s.AlreadyUnlocked) return true;
            if (!s.CanBuy || p.coins < s.CoinCost) return false;
            p.coins -= s.CoinCost;
            p.unlockedVehicleIds.Add(vehicleId);
            return true;
        }
    }

    /// <summary>Per-vehicle mastery from lifetime distance on that vehicle (blueprint D3b), plus the per-vehicle goal counter stub, scrap and run recording.</summary>
    public static class MasteryService
    {
        public static int LevelFor(ProgressionTuningSO t, float metres)
        {
            int lvl = 0;
            for (int i = 0; i < t.mastery.Length; i++) if (metres >= t.mastery[i].metres) lvl = i + 1;
            return lvl;
        }

        public static int Level(PlayerProfile p, ProgressionTuningSO t, string vehicleId) { return LevelFor(t, p.ProgressFor(vehicleId).metres); }

        /// <summary>Adds a finished run: lifetime distance, the vehicle's distance, and pays every newly reached mastery level once (coins and gems go to the profile; sticker, colour and cosmetic are recorded in the claimed mask). Returns how many levels were newly claimed.</summary>
        public static int RecordDistance(PlayerProfile p, ProgressionTuningSO t, string vehicleId, float metres)
        {
            if (metres <= 0f) return 0;
            p.lifetimeDistance += metres;
            var v = p.ProgressFor(string.IsNullOrEmpty(vehicleId) ? "vehicle-start" : vehicleId);
            v.metres += metres;
            int claimed = 0;
            for (int i = 0; i < t.mastery.Length && i < 30; i++)
            {
                int bit = 1 << i;
                if (v.metres < t.mastery[i].metres || (v.masteryClaimedMask & bit) != 0) continue;
                v.masteryClaimedMask |= bit;
                claimed++;
                if (t.mastery[i].reward == MasteryReward.Coins) p.coins += t.mastery[i].amount;
                else if (t.mastery[i].reward == MasteryReward.Gem) p.gems += t.mastery[i].amount;
            }
            return claimed;
        }

        public static void AddGoal(PlayerProfile p, string vehicleId, string goal, int n) { p.AddCounter(vehicleId + ":" + goal, n); }
        public static bool GoalDone(PlayerProfile p, ProgressionTuningSO t, string vehicleId, string goal)
        {
            string key = vehicleId + ":" + goal;
            for (int i = 0; i < t.goalIds.Length && i < t.goalTargets.Length; i++) if (t.goalIds[i] == key) return p.Counter(key) >= t.goalTargets[i];
            return false;
        }

        public static void AddScrap(PlayerProfile p, int n) { if (n > 0) p.scrap += n; }
        public static bool SpendScrap(PlayerProfile p, int n) { if (n < 0 || p.scrap < n) return false; p.scrap -= n; return true; }
    }

    public struct DailyChallengeDef { public uint seed; public string date, vehicleId, worldId; public string[] eventIds; public int rewardCoins; }

    /// <summary>Daily challenge (blueprint D6). Same seed for everyone from the UTC date; local best only; one claim per day.</summary>
    public static class DailyChallenge
    {
        public static string DateKey(DateTime utc) { return utc.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture); }

        /// <summary>FNV-1a over the UTC yyyyMMdd number: stable across platforms and runs.</summary>
        public static uint SeedFor(DateTime utc)
        {
            uint h = 2166136261u;
            string k = utc.ToString("yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture);
            for (int i = 0; i < k.Length; i++) { h ^= k[i]; h *= 16777619u; }
            return h;
        }

        /// <summary>Monday (UTC) of the date's week; the weekly challenge key.</summary>
        public static string WeekKey(DateTime utc) { int back = ((int)utc.DayOfWeek + 6) % 7; return DateKey(utc.Date.AddDays(-back)); }

        public static DailyChallengeDef Build(DateTime utc, ProgressionTuningSO t)
        {
            uint seed = SeedFor(utc);
            var rng = new Pcg32(seed);
            var d = new DailyChallengeDef { seed = seed, date = DateKey(utc) };
            d.vehicleId = Pick(t.dailyVehicleIds, ref rng);
            d.worldId = Pick(t.dailyWorldIds, ref rng);
            int n = Mathf.Min(t.dailyEventCount, t.dailyEventIds.Length);
            var pool = new List<string>(t.dailyEventIds);
            d.eventIds = new string[n];
            for (int i = 0; i < n; i++) { int k = rng.NextInt(pool.Count); d.eventIds[i] = pool[k]; pool.RemoveAt(k); }
            d.rewardCoins = rng.NextRange(t.dailyCoinsMin, t.dailyCoinsMax);
            return d;
        }

        private static string Pick(string[] a, ref Pcg32 rng) { return a == null || a.Length == 0 ? "" : a[rng.NextInt(a.Length)]; }

        private static void Roll(PlayerProfile p, DateTime utc) { string k = DateKey(utc); if (p.dailyDate != k) { p.dailyDate = k; p.dailyBest = 0; p.dailyClaimed = false; } }

        /// <summary>Records a score for today; keeps the best. Returns true for a new local best.</summary>
        public static bool SubmitScore(PlayerProfile p, DateTime utc, int score) { Roll(p, utc); if (score <= p.dailyBest) return false; p.dailyBest = score; return true; }

        public static bool CanClaim(PlayerProfile p, DateTime utc, int minScore = 1) { Roll(p, utc); return !p.dailyClaimed && p.dailyBest >= minScore; }

        /// <summary>Pays coins, one mastery point and a weekly token once per UTC day.</summary>
        public static bool Claim(PlayerProfile p, DateTime utc, ProgressionTuningSO t, out int coins, int minScore = 1)
        {
            coins = 0;
            if (!CanClaim(p, utc, minScore)) return false;
            coins = Build(utc, t).rewardCoins;
            p.coins += coins; p.masteryPoints += t.dailyMasteryPoints; p.weeklyTokens += t.dailyWeeklyTokens; p.dailyClaimed = true;
            return true;
        }
    }

    public enum AdPlacement { DoubleCoins, Revive, ExtraDailyReward }

    /// <summary>Rewarded-ad seam. No SDK exists in the project; the user picks one later (Google Play Families review first).</summary>
    public interface IAdService
    {
        bool IsRewardedReady(AdPlacement placement);
        /// <summary>Shows a rewarded ad and calls back true when the reward is earned.</summary>
        void ShowRewarded(AdPlacement placement, Action<bool> onResult);
    }

    public sealed class NoOpAdService : IAdService
    {
        public bool IsRewardedReady(AdPlacement placement) { return false; }
        public void ShowRewarded(AdPlacement placement, Action<bool> onResult) { if (onResult != null) onResult(false); }
    }

    /// <summary>Editor and development builds only: pretends an ad was watched.</summary>
    public sealed class EditorFakeAdService : IAdService
    {
        public bool IsRewardedReady(AdPlacement placement) { return true; }
        public void ShowRewarded(AdPlacement placement, Action<bool> onResult) { if (onResult != null) onResult(true); }
    }

    /// <summary>Offer limits (blueprint D7): 2x coins max 2/day, revive max 1 per run, extra daily reward max 1/day. Never while a run is active. Counters live in the profile.</summary>
    public sealed class AdOffers
    {
        private readonly PlayerProfile _p;
        private readonly ProgressionTuningSO _t;
        private readonly IAdService _ads;
        private readonly Func<DateTime> _utc;
        private int _reviveThisRun;
        public bool RunActive { get; private set; }

        public AdOffers(PlayerProfile p, ProgressionTuningSO t, IAdService ads, Func<DateTime> utcNow) { _p = p; _t = t; _ads = ads ?? new NoOpAdService(); _utc = utcNow ?? (() => DateTime.UtcNow); }

        public static IAdService CreateDefaultService()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            return new EditorFakeAdService();
#else
            return new NoOpAdService();
#endif
        }

        public void BeginRun() { RunActive = true; _reviveThisRun = 0; }
        public void EndRun() { RunActive = false; }

        private void RollDay() { string k = DailyChallenge.DateKey(_utc()); if (_p.adDate != k) { _p.adDate = k; _p.adDoubleUsed = 0; _p.adExtraDailyUsed = 0; } }

        public int Remaining(AdPlacement a)
        {
            RollDay();
            switch (a)
            {
                case AdPlacement.DoubleCoins: return Mathf.Max(0, _t.doubleCoinsPerDay - _p.adDoubleUsed);
                case AdPlacement.ExtraDailyReward: return Mathf.Max(0, _t.extraDailyRewardPerDay - _p.adExtraDailyUsed);
                default: return Mathf.Max(0, _t.revivePerRun - _reviveThisRun);
            }
        }

        public bool CanOffer(AdPlacement a) { return !RunActive && Remaining(a) > 0 && _ads.IsRewardedReady(a); }

        /// <summary>Shows the ad if allowed; the counter is spent only when the reward is earned. Returns false when not offered.</summary>
        public bool TryOffer(AdPlacement a, Action<bool> onRewarded)
        {
            if (!CanOffer(a)) return false;
            _ads.ShowRewarded(a, ok =>
            {
                if (ok) Spend(a);
                if (onRewarded != null) onRewarded(ok);
            });
            return true;
        }

        private void Spend(AdPlacement a)
        {
            RollDay();
            if (a == AdPlacement.DoubleCoins) _p.adDoubleUsed++;
            else if (a == AdPlacement.ExtraDailyReward) _p.adExtraDailyUsed++;
            else _reviveThisRun++;
        }
    }

    /// <summary>First-hour schedule lookups (data in ProgressionTuningSO; never forced UI).</summary>
    public static class FirstHour
    {
        public static bool Claim(PlayerProfile p, ProgressionTuningSO t, string stepId)
        {
            if (p.firstHourClaimed.Contains(stepId)) return false;
            for (int i = 0; i < t.firstHour.Length; i++)
                if (t.firstHour[i].id == stepId) { p.firstHourClaimed.Add(stepId); p.coins += t.firstHour[i].coins; p.gems += t.firstHour[i].gems; return true; }
            return false;
        }
    }
}
