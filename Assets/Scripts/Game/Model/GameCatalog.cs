namespace RoadBuilderGame
{
    /// <summary>
    /// Display names, prices and rules for vehicles, worlds and upgrades. Plain data for now; the Mode packs
    /// in Phase 2 replace the world entries with ModeDefinition assets.
    /// </summary>
    public static class GameCatalog
    {
        public const int MaxUpgradeLevel = 5;
        

        public static string VehicleName(VehicleType t)
        {
            switch (t)
            {
                case VehicleType.SportCyan: return "CYAN HYPERCAR";
                case VehicleType.MonsterTruck: return "MONSTER TRUCK";
                default: return "CLASSIC RUNNER";
            }
        }

        public static int VehicleCost(VehicleType t)
        {
            if (t == VehicleType.SportCyan) return 600;
            if (t == VehicleType.MonsterTruck) return 2000;
            return 0;
        }

        /// <summary>Coins for the next level when currentLevel (0..5) are owned; costs come from the UpgradeTreeSO (500/900/1500/2500/4000).</summary>
        public static int UpgradeCost(int currentLevel) { return UpgradeTreeSO.Load().Cost(currentLevel); }

        public static int UpgradeCost(int currentLevel, string vehicleKey) { return UpgradeTreeSO.Load().Cost(currentLevel, vehicleKey); }

        public static string UpgradeName(int statIndex)
        {
            return statIndex == 0 ? "ENGINE" : (statIndex == 1 ? "SUSPENSION" : (statIndex == 2 ? "INK TANK" : (statIndex == 3 ? "BOOST" : "SHIELD")));
        }

        public static string UpgradeHint(int statIndex)
        {
            return statIndex == 0 ? "More speed" : (statIndex == 1 ? "More grip" : (statIndex == 2 ? "More road" : (statIndex == 3 ? "Longer boost" : "Blocks a hit")));
        }

        public static void World(EnvironmentTheme t, out string title, out string description, out string previewResource)
        {
            switch (t)
            {
                case EnvironmentTheme.Desert:
                    title = "DESERT DUNES";
                    description = "Blazing hot sand dunes with rustic dirt and sand tracks.";
                    previewResource = "RoadBuilder/Art/Stage_Desert";
                    break;
                case EnvironmentTheme.CyberNight:
                    title = "CYBER NIGHT 2099";
                    description = "Synthwave neon landscape with glowing energy tracks.";
                    previewResource = "RoadBuilder/Art/Stage_Cyber";
                    break;
                default:
                    title = "COUNTRYSIDE HILLS";
                    description = "Lush green rolling hills with smooth tarmac highway.";
                    previewResource = "RoadBuilder/Art/Stage_Country";
                    break;
            }
        }

        public static string RoadStyleName(RoadStyle s)
        {
            switch (s)
            {
                case RoadStyle.DirtTrack: return "DIRT TRACK";
                case RoadStyle.NeonGrid: return "NEON GRID";
                default: return "ASPHALT HIGHWAY";
            }
        }
    }
}
