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
        public int version = 1;
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

        /// <summary>Clamp values to the ranges the game supports (same limits the old PlayerPrefs loader used).</summary>
        public void Sanitize()
        {
            engineLevel = Mathf.Clamp(engineLevel, 1, 5);
            suspensionLevel = Mathf.Clamp(suspensionLevel, 1, 5);
            inkTankLevel = Mathf.Clamp(inkTankLevel, 1, 5);
            if (coins < 0) coins = 0;
            if (bestDistance < 0f) bestDistance = 0f;
        }
    }

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
