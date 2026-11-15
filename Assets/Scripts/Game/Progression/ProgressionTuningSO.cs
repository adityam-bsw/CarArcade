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

        [Header("Scrap sources")]
        [Tooltip("Dismantling an owned vehicle pays price / this many scrap (clamped).")] public int dismantleCoinsPerScrap = 200;
        public int dismantleMinScrap = 5, dismantleMaxScrap = 250;
        [Tooltip("Scrap price of a vehicle's reinforce upgrade = base + tier x perTier.")] public int reinforceScrapBase = 20, reinforceScrapPerTier = 15;
        [Tooltip("The reinforce upgrade moves the vehicle's weakest axis this fraction of the way back to 1.")] [Range(0f, 1f)] public float reinforceFraction = 0.5f;

        [Header("Counters raised by run results")]
        public string[] lowGravityWorldIds = { "world-moon", "world-mars" };
        [Tooltip("Each full block of this many metres driven in a low-gravity world counts as one low-gravity stage (rover mastery goal).")] public int lowGravityStageMetres = 1000;
        public string[] waterSurfaceIds = { "surface-river", "surface-sea-water", "surface-coral" };
        public string[] lavaSurfaceIds = { "surface-lava-crust", "surface-magma-bridge" };
        [Tooltip("Metres on water surfaces per point of water mastery.")] public int waterMasteryMetres = 1000;
        [Tooltip("Metres driven in one world before it counts as mastered (feeds the Exotic surface gates).")] public int worldMasteryMetres = 3000;
        [Tooltip("Metres on a lava surface in one run that count as surviving a lava stage.")] public int lavaSurviveMetres = 500;
        [Tooltip("Until the stage events exist, lava survival and the rhythm challenge also count one per long run, so no world is locked forever. Switch off when the events raise their counters.")] public bool fallbackForMissingEvents = true;
        public int fallbackLongRunMetres = 3000;
        [Tooltip("Stage event ids that raise a counter when they end. Fragile-road breaking is raised by the surface sections themselves (ProgressionEvents.Record FragileBroken), not by a rule.")] public StageEventRule[] eventRules =
        {
            new StageEventRule { idContains = "LOWGRAV", raises = ProgressionEvent.LowGravityStage },
            new StageEventRule { idContains = "LAVA", raises = ProgressionEvent.LavaStageSurvived },
            new StageEventRule { idContains = "RHYTHM", raises = ProgressionEvent.RhythmChallenge },
        };

        [Header("Gate counters")]
        [Tooltip("A run counts as a survival run when it reaches this distance (or finishes the course).")] public int survivalRunMetres = 1000;

        [Header("Daily challenge")]
        public string[] dailyVehicleIds = { "vehicle-car", "vehicle-sports-car", "vehicle-monster-truck" };
        public string[] dailyWorldIds = { "world-rolling-hills", "world-desert", "world-night-city" };
        public string[] dailyEventIds = { "EVT-ICE", "EVT-METEOR", "EVT-MUD", "EVT-GLASS", "EVT-RINGS" };   // stage ladder pool ids; the run start filters picks through the compatibility matrix
        public int dailyCoinsMin = 100, dailyCoinsMax = 250, dailyMasteryPoints = 1, dailyWeeklyTokens = 1;
        public int dailyEventCount = 3;
        public int leaderboardSize = 10;
        [Header("Weekly challenge")] public int weeklyTokensNeeded = 5, weeklyCoins = 500, weeklyGems = 2;
        [Tooltip("Themed pool: three are chosen per week from the week's seed.")]
        public WeeklyChallengeDef[] weeklyThemed =
        {
            new WeeklyChallengeDef { id = "w-distance", label = "DRIVE FAR", kind = WeeklyKind.Distance, target = 6000, coins = 250 },
            new WeeklyChallengeDef { id = "w-air", label = "AIR WEEK: CLEAN JUMPS", kind = WeeklyKind.CleanJumps, target = 40, coins = 250 },
            new WeeklyChallengeDef { id = "w-near", label = "NEAR-MISS WEEK", kind = WeeklyKind.NearMisses, target = 25, coins = 250 },
            new WeeklyChallengeDef { id = "w-flips", label = "FLIP WEEK", kind = WeeklyKind.Flips, target = 20, coins = 250 },
            new WeeklyChallengeDef { id = "w-coins", label = "COIN WEEK", kind = WeeklyKind.CoinsEarned, target = 1500, coins = 250 },
        };
        public WeeklyChallengeDef weeklyBoss = new WeeklyChallengeDef { id = "w-boss", label = "BOSS CHALLENGE: DEFEAT RO-B0", kind = WeeklyKind.BossDefeats, target = 1, coins = 400 };
        public WeeklyChallengeDef weeklySurface = new WeeklyChallengeDef { id = "w-surface", label = "SPECIAL SURFACE: METRES ON ADVANCED ROADS", kind = WeeklyKind.SpecialSurfaceMetres, target = 1500, coins = 400 };

        [Header("Road Trip Album (30 days, 20 free + 10 optional cosmetic; always claimable, no pressure)")]
        public AlbumDay[] album = DefaultAlbum();

        [Header("Cosmetics (gem shop and album). Vehicle tints are gameplay-side data, not UI colour tokens.")]
        public CosmeticDef[] cosmetics =
        {
            new CosmeticDef { id = "paint-sunset", label = "SUNSET PAINT", kind = CosmeticKind.Colour, color = new Color(1f, 0.55f, 0.25f), gems = 5 },
            new CosmeticDef { id = "paint-ocean", label = "OCEAN PAINT", kind = CosmeticKind.Colour, color = new Color(0.3f, 0.75f, 0.95f), gems = 5 },
            new CosmeticDef { id = "paint-lime", label = "LIME PAINT", kind = CosmeticKind.Colour, color = new Color(0.65f, 0.95f, 0.3f), gems = 5 },
            new CosmeticDef { id = "paint-midnight", label = "MIDNIGHT PAINT", kind = CosmeticKind.Colour, color = new Color(0.35f, 0.35f, 0.7f), gems = 8 },
            new CosmeticDef { id = "paint-gold", label = "GOLD PAINT", kind = CosmeticKind.Colour, color = new Color(1f, 0.82f, 0.25f), gems = 15 },
            new CosmeticDef { id = "sticker-flame", label = "FLAME STICKER", kind = CosmeticKind.Sticker, color = new Color(1f, 0.4f, 0.2f), gems = 4 },
            new CosmeticDef { id = "sticker-bolt", label = "BOLT STICKER", kind = CosmeticKind.Sticker, color = new Color(1f, 0.9f, 0.2f), gems = 4 },
            new CosmeticDef { id = "sticker-star", label = "STAR STICKER", kind = CosmeticKind.Sticker, color = new Color(0.95f, 0.95f, 1f), gems = 4 },
            new CosmeticDef { id = "trim-chrome", label = "CHROME TRIM", kind = CosmeticKind.Trim, color = new Color(0.85f, 0.9f, 0.95f), gems = 10 },
            new CosmeticDef { id = "paint-trip-1", label = "ROAD TRIP GREEN", kind = CosmeticKind.Colour, color = new Color(0.4f, 0.85f, 0.5f) },
            new CosmeticDef { id = "paint-trip-2", label = "ROAD TRIP PLUM", kind = CosmeticKind.Colour, color = new Color(0.7f, 0.4f, 0.85f) },
            new CosmeticDef { id = "paint-trip-3", label = "ROAD TRIP CORAL", kind = CosmeticKind.Colour, color = new Color(1f, 0.5f, 0.5f) },
            new CosmeticDef { id = "sticker-trip-1", label = "SUNNY STICKER", kind = CosmeticKind.Sticker, color = new Color(1f, 0.85f, 0.3f) },
            new CosmeticDef { id = "sticker-trip-2", label = "WAVE STICKER", kind = CosmeticKind.Sticker, color = new Color(0.3f, 0.8f, 1f) },
            new CosmeticDef { id = "trim-trip", label = "ROAD TRIP TRIM", kind = CosmeticKind.Trim, color = new Color(1f, 0.8f, 0.3f) },
        };

        [Header("Gems: earned from achievements, milestones and the daily; buying is optional and only offered when a purchase service exists")]
        public AchievementDef[] achievements =
        {
            new AchievementDef { id = "ach-dist-1k", label = "DRIVE 1,000 M", metric = AchievementMetric.LifetimeMetres, target = 1000, gems = 1 },
            new AchievementDef { id = "ach-dist-5k", label = "DRIVE 5,000 M", metric = AchievementMetric.LifetimeMetres, target = 5000, gems = 2 },
            new AchievementDef { id = "ach-dist-20k", label = "DRIVE 20,000 M", metric = AchievementMetric.LifetimeMetres, target = 20000, gems = 3 },
            new AchievementDef { id = "ach-run-1k", label = "ONE RUN OF 1,000 M", metric = AchievementMetric.BestRunMetres, target = 1000, gems = 1 },
            new AchievementDef { id = "ach-run-3k", label = "ONE RUN OF 3,000 M", metric = AchievementMetric.BestRunMetres, target = 3000, gems = 2 },
            new AchievementDef { id = "ach-boss", label = "DEFEAT RO-B0", metric = AchievementMetric.BossDefeats, target = 1, gems = 2 },
            new AchievementDef { id = "ach-missions", label = "FINISH 10 MISSIONS", metric = AchievementMetric.MissionsDone, target = 10, gems = 2 },
            new AchievementDef { id = "ach-jumps", label = "50 CLEAN JUMPS", metric = AchievementMetric.CleanJumps, target = 50, gems = 1 },
            new AchievementDef { id = "ach-garage", label = "OWN 3 VEHICLES", metric = AchievementMetric.VehiclesOwned, target = 3, gems = 1 },
            new AchievementDef { id = "ach-worlds", label = "OWN 3 WORLDS", metric = AchievementMetric.WorldsOwned, target = 3, gems = 2 },
            new AchievementDef { id = "ach-upgrades", label = "BUY 10 UPGRADES", metric = AchievementMetric.UpgradeLevels, target = 10, gems = 2 },
            new AchievementDef { id = "ach-mastery", label = "REACH MASTERY LEVEL 3", metric = AchievementMetric.MasteryLevels, target = 3, gems = 2 },
            new AchievementDef { id = "ach-daily", label = "CLAIM 5 DAILY CHALLENGES", metric = AchievementMetric.DailyClaims, target = 5, gems = 2 },
        };
        public GemPack[] gemPacks =
        {
            new GemPack { id = "gems-small", gems = 20, priceLabel = "SMALL PACK" },
            new GemPack { id = "gems-medium", gems = 60, priceLabel = "MEDIUM PACK" },
            new GemPack { id = "gems-large", gems = 140, priceLabel = "LARGE PACK" },
        };
        public bool shopEnabled = true;

        [Header("Run earnings targets (blueprint D5): coins of a whole run for a median player")]
        public RunBand[] runBands =
        {
            new RunBand { metres = 500, minCoins = 80, maxCoins = 120 }, new RunBand { metres = 1500, minCoins = 180, maxCoins = 300 },
            new RunBand { metres = 3000, minCoins = 350, maxCoins = 500 }, new RunBand { metres = 5000, minCoins = 600, maxCoins = 900 },
            new RunBand { metres = 8000, minCoins = 1000, maxCoins = 1400 },
        };

        private static AlbumDay[] DefaultAlbum()
        {
            // Days 5, 8, 11, 14, 17, 20, 23, 26, 29 and 30 are the 10 optional cosmetics; the other 20 pay coins (and a gem every 7th day). All free and always claimable.
            var cos = new System.Collections.Generic.Dictionary<int, string> { { 5, "paint-trip-1" }, { 8, "sticker-trip-1" }, { 11, "paint-trip-2" }, { 14, "trim-trip" }, { 17, "sticker-trip-2" },
                { 20, "paint-trip-3" }, { 23, "sticker-star" }, { 26, "paint-sunset" }, { 29, "paint-ocean" }, { 30, "paint-gold" } };
            var a = new AlbumDay[30];
            for (int d = 1; d <= 30; d++)
            {
                a[d - 1] = new AlbumDay { day = d, cosmeticId = "" };
                if (cos.TryGetValue(d, out var id)) a[d - 1].cosmeticId = id;
                else { a[d - 1].coins = 50 + (d / 5) * 25; if (d % 7 == 0) a[d - 1].gems = 1; }
            }
            return a;
        }

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
