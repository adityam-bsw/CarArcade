using System;

namespace RoadBuilderGame
{
    public enum ScreenState
    {
        HomeDashboard,
        StageSelect,
        GarageMenu,
        Playing,
        GameOver,
        Rewards
    }

    /// <summary>Outcome of the last run, shown on the result screen.</summary>
    public struct RunResult
    {
        public string reason;
        public float distance;
        public int coinsWon;
        public int totalCoins;
        public bool newBest;
        public int gems;
        public int bonusCoins;
        /// <summary>One line per bonus earned in the run (flips, air time, chests...), for the result screen.</summary>
        public string bonusLines;
    }

    /// <summary>One daily mission as the rewards screen shows it.</summary>
    public sealed class MissionView
    {
        public string text;
        public int progress, target, rewardCoins, rewardGems;
        public bool claimed;
        public bool Complete => progress >= target;
    }

    /// <summary>
    /// Read-only snapshot of game state for the UI. The game (RoadGameManager) writes it and raises events;
    /// screens only read it and react. Keeps UI free of gameplay logic and the game free of UI types.
    /// </summary>
    public sealed class GameModel
    {
        public ScreenState Screen { get; private set; } = ScreenState.HomeDashboard;

        // Progress (persistent)
        public int Coins;
        public float BestDistance;
        public int EngineLevel, SuspensionLevel, InkTankLevel;   // upgrade levels owned 0..5 for the selected vehicle
        public VehicleType Vehicle;
        public EnvironmentTheme World;
        public RoadStyle RoadStyle;
        public bool SportUnlocked, TruckUnlocked;
        public bool SoundEnabled = true;

        // Catalogue picks (manifest ids). Saved with the profile; applying them to the track is a later step.
        public string SelectedVehicleId = "";
        public readonly System.Collections.Generic.HashSet<string> UnlockedVehicleIds = new System.Collections.Generic.HashSet<string>();
        public bool IsVehicleUnlocked(CatalogEntry e) => e != null && (e.cost <= 0 || UnlockedVehicleIds.Contains(e.id));
        public string SelectedSurfaceId = "";
        public string SelectedWorldId = "";

        public string SelectedId(CatalogKind kind)
        {
            return kind == CatalogKind.Vehicle ? SelectedVehicleId : (kind == CatalogKind.Surface ? SelectedSurfaceId : SelectedWorldId);
        }

        // Run (changes every frame while playing)
        public float Distance;
        public float SpeedKmh;
        public int SessionCoins;
        public int Lives, MaxLives = 3;
        public float Ink01 = 1f;
        public float Boost01;                 // free boost meter, 0 to 1
        public int BoostCost;                 // coins the next boost charges (0 = free)
        public bool BoostReady;               // can boost right now
        public int Combo, Multiplier = 1;
        public int SessionGems;
        public string PowerText = "";         // active power-ups, e.g. "SHIELD  MAGNET 8"
        public string Toast = "";             // short event text; ToastId changes for every new one
        public int ToastId;

        // Stage events (written by StageWarningPresenter; the HUD only displays them). Tint fields hold a UITheme colour token name.
        public string StageBanner = "";       // S1 banner text; StageBannerId changes for every new banner
        public int StageBannerId;
        public string StageBannerTint = "";
        public bool StageSignOn;              // S2 sign visible (from S2 until the effect ends)
        public bool StageSignActive;          // the effect is running (the sign counts metres left); false while it counts metres to the start
        public string StageSignName = "";
        public int StageSignMetres;
        public string StageSignTint = "";
        public int StageFlashId;              // S3 edge flash; changes for every flash
        public string StageFlashTint = "";
        // Road-Eater and RO-B0 (written by the manager; a HUD can display them). Gap in metres, band 0 calm 1 edge 2 danger 3 snap, escape and Overload 0..1.
        public bool EaterOn;
        public float EaterGapM, EaterEscape01;
        public int EaterBand;
        public bool BossOn;
        public float BossOverload01;
        public int BossPhase;

        // Rewards and progress
        public int Gems, BoostLevel;
        public int Scrap;                     // earned from boss rewards and duplicates (model only for now)
        public bool RewardsAvailable;         // something to claim
        public bool DailyAvailable;
        public int DailyDay = 1, DailyCoins, DailyGems;
        public MissionView[] Missions = new MissionView[0];
        public int MasteryLevel = 1, MasteryXp, MasteryNext = 100;

        public RunResult LastRun;

        /// <summary>The visible screen should change.</summary>
        public event Action ScreenChanged;
        /// <summary>Persistent or menu-level data changed (coins, levels, selection).</summary>
        public event Action Changed;
        /// <summary>Run data changed (distance, ink, lives). Raised often while playing.</summary>
        public event Action HudChanged;

        public void SetScreen(ScreenState s)
        {
            Screen = s;
            ScreenChanged?.Invoke();
        }

        public void RaiseChanged() { Changed?.Invoke(); }
        public void RaiseHud() { HudChanged?.Invoke(); }

        public bool IsUnlocked(VehicleType v)
        {
            if (v == VehicleType.ClassicRed) return true;
            if (v == VehicleType.SportCyan) return SportUnlocked;
            if (v == VehicleType.MonsterTruck) return TruckUnlocked;
            return false;
        }

        public int LevelOf(int statIndex)
        {
            return statIndex == 0 ? EngineLevel : (statIndex == 1 ? SuspensionLevel : (statIndex == 2 ? InkTankLevel : BoostLevel));
        }
    }

    /// <summary>Everything a screen is allowed to ask the game to do.</summary>
    public interface IGameActions
    {
        void StartRace();
        void GoHome();
        void OpenGarage();
        void OpenWorlds();
        void Boost();
        void OpenRewards();
        void ClaimDaily();
        void ClaimMission(int index);
        void UpgradeStat(int statIndex);
        void SelectOrUnlockVehicle();
        void CycleVehicle(int dir);
        void CycleWorld(int dir);
        void CycleRoadStyle();
        void SetSoundEnabled(bool enabled);
        void SelectCatalogItem(CatalogKind kind, string id);
    }

    /// <summary>Where the UI finds the model and actions. Set up by the game when it starts.</summary>
    public static class GameServices
    {
        private static GameModel _model;

        public static GameModel Model => _model ?? (_model = new GameModel());
        public static IGameActions Actions { get; set; }
    }
}
