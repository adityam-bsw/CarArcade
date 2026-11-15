using System;
using System.IO;
using UnityEngine;

namespace RoadBuilderGame
{
    /// <summary>
    /// Everything the game remembers about the player. Plain data, versioned, no Unity object references.
    /// Enums are stored as ints so this file depends on nothing else in the project.
    /// </summary>
    [Serializable]
    public class PlayerProfile
    {
        public int version = CurrentVersion;
        public const int CurrentVersion = 2;
        public int coins = 150;
        public float bestDistance;
        public int engineLevel = 1;
        public int suspensionLevel = 1;
        public int inkTankLevel = 1;
        public int vehicle;
        public int theme;
        public int roadStyle;
        public bool sportUnlocked;
        public bool truckUnlocked;
        public bool soundEnabled = true;

        // Catalogue choices (ids from the asset manifest). Empty = none picked yet. Stored only: the game applies them later.
        public string selectedVehicleId = "";
        public System.Collections.Generic.List<string> unlockedVehicleIds = new System.Collections.Generic.List<string>();
        public string selectedSurfaceId = "";
        public string selectedWorldId = "";

        /// <summary>The stage-event run record: seed, mode and layout of the last run, so a retry replays the same ladder. Written by the run start only.</summary>
        public StageRunRecord lastStageRun;
        /// <summary>Times the Road-Eater was escaped or timed out, and RO-B0 defeated or timed out. Repeat clears pay half (brief section 14). Saved with the profile, no PlayerPrefs.</summary>
        public int eaterEscapes, eaterTimeouts, bossDefeats, bossTimeouts;

        // Rewards and meta progress
        public int gems;
        public int boostLevel = 1;
        public string lastDailyClaim = "";           // yyyy-MM-dd of the last claimed daily reward
        public int dailyStreak;                       // days in a row, 1 to 7
        public string missionDate = "";               // the day the missions below belong to
        public System.Collections.Generic.List<MissionState> missions = new System.Collections.Generic.List<MissionState>();
        public System.Collections.Generic.List<VehicleXp> vehicleXp = new System.Collections.Generic.List<VehicleXp>();
        public System.Collections.Generic.List<SurfaceSeen> surfaceSeenCount = new System.Collections.Generic.List<SurfaceSeen>();   // how often each surface was met, for the 3-step hint rule

        // ---- Section D progression (version 2). All additive: old saves load with these at their defaults and are migrated in Sanitize.
        public int scrap;
        public float lifetimeDistance;                                              // cumulative metres, gates new unlocks only
        public System.Collections.Generic.List<UpgradeEntry> upgrades = new System.Collections.Generic.List<UpgradeEntry>();            // per vehicle, levels owned 0..5 per branch
        public System.Collections.Generic.List<VehicleProgress> vehicleProgress = new System.Collections.Generic.List<VehicleProgress>();   // per vehicle mastery
        public System.Collections.Generic.List<string> unlockedWorldIds = new System.Collections.Generic.List<string> { "world-rolling-hills" };
        public System.Collections.Generic.List<CounterEntry> counters = new System.Collections.Generic.List<CounterEntry>();            // gate and goal counters
        public string dailyDate = ""; public int dailyBest; public bool dailyClaimed;   // UTC yyyy-MM-dd of the daily challenge below
        public int masteryPoints, weeklyTokens;
        public string adDate = ""; public int adDoubleUsed, adExtraDailyUsed;           // rewarded-ad offers used on adDate (UTC)
        public System.Collections.Generic.List<string> firstHourClaimed = new System.Collections.Generic.List<string>();

        /// <summary>Upgrade levels for a vehicle key. A vehicle never seen before inherits the old global levels (legacy 1..5 becomes 0..4), so no purchase is lost.</summary>
        public UpgradeEntry UpgradesFor(string key)
        {
            if (upgrades == null) upgrades = new System.Collections.Generic.List<UpgradeEntry>();
            for (int i = 0; i < upgrades.Count; i++) if (upgrades[i].id == key) return upgrades[i];
            var e = new UpgradeEntry { id = key, levels = new int[UpgradeTreeSO.Branches] };
            e.levels[0] = Mathf.Clamp(engineLevel - 1, 0, 4); e.levels[1] = Mathf.Clamp(suspensionLevel - 1, 0, 4);
            e.levels[2] = Mathf.Clamp(inkTankLevel - 1, 0, 4); e.levels[3] = Mathf.Clamp(boostLevel - 1, 0, 4);
            upgrades.Add(e);
            return e;
        }

        public VehicleProgress ProgressFor(string id)
        {
            if (vehicleProgress == null) vehicleProgress = new System.Collections.Generic.List<VehicleProgress>();
            for (int i = 0; i < vehicleProgress.Count; i++) if (vehicleProgress[i].id == id) return vehicleProgress[i];
            var v = new VehicleProgress { id = id };
            vehicleProgress.Add(v);
            return v;
        }

        public int Counter(string id) { if (counters != null) for (int i = 0; i < counters.Count; i++) if (counters[i].id == id) return counters[i].count; return 0; }
        public void AddCounter(string id, int n)
        {
            if (counters == null) counters = new System.Collections.Generic.List<CounterEntry>();
            for (int i = 0; i < counters.Count; i++) if (counters[i].id == id) { counters[i].count += n; return; }
            counters.Add(new CounterEntry { id = id, count = n });
        }

        public int SurfaceSeen(string id)
        {
            if (surfaceSeenCount == null) return 0;
            for (int i = 0; i < surfaceSeenCount.Count; i++) if (surfaceSeenCount[i].id == id) return surfaceSeenCount[i].count;
            return 0;
        }

        /// <summary>Counts one sighting and returns the count before it (0 = first time).</summary>
        public int MarkSurfaceSeen(string id)
        {
            if (string.IsNullOrEmpty(id)) return 0;
            if (surfaceSeenCount == null) surfaceSeenCount = new System.Collections.Generic.List<SurfaceSeen>();
            for (int i = 0; i < surfaceSeenCount.Count; i++)
                if (surfaceSeenCount[i].id == id) { int before = surfaceSeenCount[i].count; surfaceSeenCount[i] = new SurfaceSeen { id = id, count = Mathf.Min(before + 1, 99) }; return before; }
            surfaceSeenCount.Add(new SurfaceSeen { id = id, count = 1 });
            return 0;
        }

        /// <summary>Clamp values to the ranges the game supports (same limits the old PlayerPrefs loader used).</summary>
        public void Sanitize()
        {
            if (version < 2)
            {
                // Migration 1 -> 2: nothing owned may become locked. Lifetime distance starts at the best run, the three old worlds stay unlocked,
                // upgrade levels move to per-vehicle entries lazily (UpgradesFor reads the old global levels).
                version = 2;
                lifetimeDistance = Mathf.Max(lifetimeDistance, bestDistance);
                if (unlockedWorldIds == null) unlockedWorldIds = new System.Collections.Generic.List<string>();
                foreach (var w in new[] { "world-rolling-hills", "world-desert", "world-night-city", selectedWorldId })
                    if (!string.IsNullOrEmpty(w) && !unlockedWorldIds.Contains(w)) unlockedWorldIds.Add(w);
            }
            if (upgrades == null) upgrades = new System.Collections.Generic.List<UpgradeEntry>();
            foreach (var u in upgrades)
            {
                if (u.levels == null || u.levels.Length != UpgradeTreeSO.Branches)
                {
                    var n = new int[UpgradeTreeSO.Branches];
                    if (u.levels != null) System.Array.Copy(u.levels, n, Mathf.Min(u.levels.Length, n.Length));
                    u.levels = n;
                }
                for (int i = 0; i < u.levels.Length; i++) u.levels[i] = Mathf.Clamp(u.levels[i], 0, UpgradeTreeSO.Levels);
            }
            if (vehicleProgress == null) vehicleProgress = new System.Collections.Generic.List<VehicleProgress>();
            if (unlockedWorldIds == null) unlockedWorldIds = new System.Collections.Generic.List<string>();
            if (counters == null) counters = new System.Collections.Generic.List<CounterEntry>();
            if (firstHourClaimed == null) firstHourClaimed = new System.Collections.Generic.List<string>();
            if (scrap < 0) scrap = 0;
            if (lifetimeDistance < 0f) lifetimeDistance = 0f;
            if (dailyDate == null) dailyDate = "";
            if (adDate == null) adDate = "";
            engineLevel = Mathf.Clamp(engineLevel, 1, 5);
            suspensionLevel = Mathf.Clamp(suspensionLevel, 1, 5);
            inkTankLevel = Mathf.Clamp(inkTankLevel, 1, 5);
            boostLevel = Mathf.Clamp(boostLevel, 1, 5);
            if (gems < 0) gems = 0;
            if (missions == null) missions = new System.Collections.Generic.List<MissionState>();
            if (surfaceSeenCount == null) surfaceSeenCount = new System.Collections.Generic.List<SurfaceSeen>();
            if (vehicleXp == null) vehicleXp = new System.Collections.Generic.List<VehicleXp>();
            if (coins < 0) coins = 0;
            if (bestDistance < 0f) bestDistance = 0f;
        }
    }

    [Serializable] public class MissionState { public int kind; public int target; public int progress; public bool claimed; public int rewardCoins; public int rewardGems; }
    [Serializable] public struct SurfaceSeen { public string id; public int count; }
    [Serializable] public class UpgradeEntry { public string id; public int[] levels; }
    [Serializable] public class VehicleProgress { public string id; public float metres; public int masteryClaimedMask; }
    [Serializable] public class CounterEntry { public string id; public int count; }
    [Serializable] public class VehicleXp { public string id; public int xp; }

    /// <summary>
    /// The only place that reads or writes player progress. Replaces the loose "HCD_*" PlayerPrefs keys.
    /// Load order: save.json -> save.json.bak -> legacy PlayerPrefs (one-time migration) -> defaults.
    /// </summary>
    public static class SaveService
    {
        private const string FileName = "save.json";

        // Legacy PlayerPrefs keys, read once for migration only. Do not add new keys here.
        private const string LegacyCoins = "HCD_Coins";
        private const string LegacyBest = "HCD_Best";
        private const string LegacyEngine = "HCD_Engine";
        private const string LegacySusp = "HCD_Susp";
        private const string LegacyInkTank = "HCD_Ink";
        private const string LegacyVehicle = "HCD_Vehicle";
        private const string LegacyTheme = "HCD_Theme";
        private const string LegacyRoadStyle = "HCD_RoadStyle";
        private const string LegacySportUnlock = "HCD_Unlock_Sport";
        private const string LegacyTruckUnlock = "HCD_Unlock_Truck";

        private static string FilePath => System.IO.Path.Combine(Application.persistentDataPath, FileName);

        public static PlayerProfile Load()
        {
            var profile = TryRead(FilePath) ?? TryRead(FilePath + ".bak");
            if (profile == null)
            {
                profile = ReadLegacyPrefs();
                Save(profile); // persist immediately so migration happens once
            }
            profile.Sanitize();
            return profile;
        }

        public static void Save(PlayerProfile profile)
        {
            if (profile == null) return;
            try
            {
                string path = FilePath;
                string tmp = path + ".tmp";
                File.WriteAllText(tmp, JsonUtility.ToJson(profile));
                if (File.Exists(path)) File.Copy(path, path + ".bak", true);
                if (File.Exists(path)) File.Delete(path);
                File.Move(tmp, path);
            }
            catch (Exception e)
            {
                Debug.LogError("[SaveService] Could not save progress: " + e.Message);
            }
        }

        private static PlayerProfile TryRead(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;
                return JsonUtility.FromJson<PlayerProfile>(File.ReadAllText(path));
            }
            catch (Exception e)
            {
                Debug.LogWarning("[SaveService] Ignoring unreadable save '" + path + "': " + e.Message);
                return null;
            }
        }

        private static PlayerProfile ReadLegacyPrefs()
        {
            return new PlayerProfile
            {
                coins = PlayerPrefs.GetInt(LegacyCoins, 150),
                bestDistance = PlayerPrefs.GetFloat(LegacyBest, 0f),
                engineLevel = PlayerPrefs.GetInt(LegacyEngine, 1),
                suspensionLevel = PlayerPrefs.GetInt(LegacySusp, 1),
                inkTankLevel = PlayerPrefs.GetInt(LegacyInkTank, 1),
                vehicle = PlayerPrefs.GetInt(LegacyVehicle, 0),
                theme = PlayerPrefs.GetInt(LegacyTheme, 0),
                roadStyle = PlayerPrefs.GetInt(LegacyRoadStyle, 0),
                sportUnlocked = PlayerPrefs.GetInt(LegacySportUnlock, 0) == 1,
                truckUnlocked = PlayerPrefs.GetInt(LegacyTruckUnlock, 0) == 1,
            };
        }
    }
}
