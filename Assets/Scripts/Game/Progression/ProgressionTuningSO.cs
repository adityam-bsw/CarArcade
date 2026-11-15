using System;
using UnityEngine;

namespace RoadBuilderGame
{
    public enum MasteryReward { Sticker, Coins, Colour, Gem, Cosmetic }
    [Serializable] public struct MasteryLevel { public int metres; public MasteryReward reward; public int amount; }
    [Serializable] public struct FirstHourStep { public string id; public int minute; public int coins; public int gems; public string note; }

    /// <summary>Scrap, vehicle mastery, daily challenge, rewarded-ad limits and the first-hour reward schedule as data (blueprint D1, D3b, D5 to D7).</summary>
    [CreateAssetMenu(menuName = "Road Builder/Progression/Progression Tuning", fileName = "ProgressionTuning")]
    public class ProgressionTuningSO : ScriptableObject
    {
        [Header("Scrap (earned in play only)")]
        public int scrapPerBossFirst = 20, scrapPerBossRepeat = 10, scrapPerDuplicate = 5;

        [Header("Vehicle mastery (lifetime metres on that vehicle)")]
        public MasteryLevel[] mastery =
        {
            new MasteryLevel { metres = 500, reward = MasteryReward.Sticker, amount = 1 },
            new MasteryLevel { metres = 2000, reward = MasteryReward.Coins, amount = 500 },
            new MasteryLevel { metres = 5000, reward = MasteryReward.Colour, amount = 1 },
            new MasteryLevel { metres = 10000, reward = MasteryReward.Gem, amount = 1 },
            new MasteryLevel { metres = 20000, reward = MasteryReward.Cosmetic, amount = 1 },
        };
        [Tooltip("Per-vehicle goal stub: counter id and target, e.g. bike clean landings.")] public string[] goalIds = { "vehicle-bicycle:clean-landings", "vehicle-tank:fragile-breaks", "vehicle-race-car:near-misses", "vehicle-moon-rover:low-gravity-stages" };
        public int[] goalTargets = { 10, 20, 15, 5 };

        [Header("Daily challenge")]
        public string[] dailyVehicleIds = { "vehicle-car", "vehicle-sports-car", "vehicle-monster-truck" };
        public string[] dailyWorldIds = { "world-rolling-hills", "world-desert", "world-night-city" };
        public string[] dailyEventIds = { "EVT-ICE", "EVT-METEOR", "EVT-MUD", "EVT-GLASS", "EVT-RINGS" };   // stage ladder pool ids; the run start filters picks through the compatibility matrix
        public int dailyCoinsMin = 100, dailyCoinsMax = 250, dailyMasteryPoints = 1, dailyWeeklyTokens = 1;
        public int dailyEventCount = 3;
        [Header("Weekly challenge (stub)")] public int weeklyTokensNeeded = 5, weeklyCoins = 500, weeklyGems = 2;

        [Header("Rewarded ads (no SDK; kids-safe)")]
        public int doubleCoinsPerDay = 2, revivePerRun = 1, extraDailyRewardPerDay = 1;

        [Header("First hour schedule (data only, never forced UI)")]
        public FirstHourStep[] firstHour =
        {
            new FirstHourStep { id = "first-drive", minute = 0, coins = 0, note = "First drive" },
            new FirstHourStep { id = "first-500m", minute = 4, coins = 500, note = "500 m milestone" },
            new FirstHourStep { id = "first-upgrade", minute = 8, coins = 0, note = "First upgrade" },
            new FirstHourStep { id = "first-monster", minute = 12, coins = 150, note = "Escape the Road-Eater" },
            new FirstHourStep { id = "second-vehicle", minute = 18, coins = 0, note = "Second vehicle" },
            new FirstHourStep { id = "first-surface", minute = 24, coins = 100, note = "First new surface" },
            new FirstHourStep { id = "first-boss", minute = 32, coins = 200, gems = 1, note = "Defeat RO-B0" },
            new FirstHourStep { id = "first-world", minute = 42, coins = 0, note = "First new world" },
            new FirstHourStep { id = "first-mastery", minute = 50, coins = 0, note = "First mastery reward" },
            new FirstHourStep { id = "daily-challenge", minute = 58, coins = 100, note = "Daily challenge" },
        };

        private static ProgressionTuningSO _cached;
        public static ProgressionTuningSO Load()
        {
            if (_cached != null) return _cached;
            _cached = Resources.Load<ProgressionTuningSO>("Progression/ProgressionTuning");
            if (_cached == null) _cached = CreateInstance<ProgressionTuningSO>();
            return _cached;
        }
    }
}
