using System;
using UnityEngine;

namespace RoadBuilderGame
{
    public enum CosmeticKind { Sticker, Colour, Trim }
    public enum WeeklyKind { Distance, CleanJumps, NearMisses, Flips, CoinsEarned, SpecialSurfaceMetres, BossDefeats }
    public enum AchievementMetric { LifetimeMetres, BossDefeats, MissionsDone, CleanJumps, VehiclesOwned, WorldsOwned, UpgradeLevels, MasteryLevels, BestRunMetres, DailyClaims }

    /// <summary>A cosmetic from the gem shop or the album. Mastery cosmetics are generated from the vehicle id and are not listed here.</summary>
    [Serializable] public struct CosmeticDef { public string id, label; public CosmeticKind kind; public Color color; [Tooltip("Gems to buy in the shop; 0 = not sold (album reward only).")] public int gems; }
    [Serializable] public struct WeeklyChallengeDef { public string id, label; public WeeklyKind kind; public int target, coins; }
    [Serializable] public struct AlbumDay { public int day; public int coins, gems; [Tooltip("Cosmetic id (optional cosmetic reward). Empty = a coin or gem reward.")] public string cosmeticId; }
    [Serializable] public struct AchievementDef { public string id, label; public AchievementMetric metric; public int target, gems; }
    [Serializable] public struct GemPack { public string id; public int gems; public string priceLabel; }
    [Serializable] public struct StageEventRule { [Tooltip("Matches when the stage event id contains this text (case-insensitive).")] public string idContains; public ProgressionEvent raises; }
    [Serializable] public struct RunBand { public int metres, minCoins, maxCoins; }

    /// <summary>What a finished run reports to the progression layer (one call per run end).</summary>
    public struct RunSummary
    {
        public string vehicleKey, worldId, surfaceId;
        public float distance;
        public int cleanJumps, nearMisses, flips, coins;
        public bool finished, daily;
        public DateTime utc;
    }

    /// <summary>One thing a hub row shows.</summary>
    public sealed class HubRow
    {
        public string title = "", detail = "", button = "", action = "";
        public bool enabled, done;
    }

    public enum HubPage { Mastery = 0, Journey = 1, Daily = 2, Weekly = 3, Album = 4, Shop = 5, Unlocks = 6, Workshop = 7 }

    /// <summary>What a hub page shows: a heading, a short footer line and rows. The screen pages through the rows.</summary>
    public sealed class HubView
    {
        public HubPage page;
        public string title = "", subtitle = "", footer = "";
        public readonly System.Collections.Generic.List<HubRow> rows = new System.Collections.Generic.List<HubRow>();
        public HubRow Add(string title, string detail, string button, string action, bool enabled, bool done = false)
        {
            var r = new HubRow { title = title, detail = detail, button = button, action = action, enabled = enabled, done = done };
            rows.Add(r);
            return r;
        }
    }
}
