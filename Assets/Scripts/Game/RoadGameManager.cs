using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace RoadBuilderGame
{
    public class RoadGameManager : MonoBehaviour, IGameActions
    {
        public static RoadGameManager Instance { get; private set; }

        [Header("Scene Object References (Inspectable in Editor)")]
        [SerializeField] private Camera _cam;
        [SerializeField] private RoadBuilder _road;
        [SerializeField] private BackgroundDirector _background;
        [SerializeField] private SoundManager _soundManager;
        [SerializeField] private ParticleManager _particleManager;
        [SerializeField] private EventSystem _eventSystem;

        [Header("Gameplay Dynamics")]
        public float baseStartSpeed = 4.2f;
        public float baseMaxSpeed = 11.5f;
        public float speedGainPerMetre = 0.02f;
        public float cameraSize = 7.0f;
        public float dragSpacing = 0.28f;
        public float fallDistance = 7.0f;
        public float flipLoseTime = 1.3f;

        [Header("Ink & Economy")]
        public float maxInk = 100f;
        public float currentInk = 100f;
        public float inkCostPerMeter = 1.2f;
        public float inkRegenPerSec = 5.0f;
        public int maxLives = 3;
        public int currentLives = 3;
        public int totalCoins = 150;
        public int sessionCoins = 0;

        [Header("Player Upgrades")]
        public int engineLevel = 1;
        public int suspensionLevel = 1;
        public int inkTankLevel = 1;

        [Header("Vehicle Unlocks")]
        public bool sportUnlocked = false;
        public bool truckUnlocked = false;

        private PlayerProfile _profile;
        private bool _soundEnabled = true;
        private string _selVehicleId = "", _selSurfaceId = "", _selWorldId = "";
        private readonly HashSet<string> _unlockedVehicleIds = new HashSet<string>();
        private readonly Dictionary<string, RoadSkin> _surfaceSkins = new Dictionary<string, RoadSkin>();

        private RoadCar _car;
        private PhysicsMaterial2D _roadMaterial;

        private ScreenState _state = ScreenState.HomeDashboard;
        private float _startX, _distance, _bestScore, _stateTime, _upsideDownTime;
        // Extracted collaborators (plain classes, created in Awake)
        private BrushController _brush;
        private PickupSpawner _pickups;
        private GameCameraRig _cameraRig;

        [Header("Smooth Brush Settings")]
        [Tooltip("Higher values follow finger faster; lower values create silkier macro curves")]
        public float brushSmoothingSpeed = 22f;

        public VehicleType SelectedVehicle { get; private set; } = VehicleType.ClassicRed;
        public EnvironmentTheme SelectedTheme { get; private set; } = EnvironmentTheme.Country;
        public RoadStyle SelectedRoadStyle { get; private set; } = RoadStyle.AsphaltHighway;

        private int _lastScreenW, _lastScreenH;
        private Rect _lastSafeArea;

        private void OnEnable() { Collectible.Collected += OnCollectItem; }
        private void OnDisable() { Collectible.Collected -= OnCollectItem; }

        private void Awake()
        {
            Instance = this;
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            // 1. Ensure Camera is assigned or found
            if (_cam == null) _cam = Camera.main;
            if (_cam == null)
            {
                var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
                _cam = camGo.AddComponent<Camera>();
            }
            _cam.orthographic = true;
            _cam.orthographicSize = cameraSize;
            _cam.clearFlags = CameraClearFlags.SolidColor;

            // 2. Ensure EventSystem exists with both modern and legacy input modules for clicks!
            if (_eventSystem == null) _eventSystem = FindAnyObjectByType<EventSystem>();
            if (_eventSystem == null)
            {
                var esGo = new GameObject("EventSystem");
                _eventSystem = esGo.AddComponent<EventSystem>();
                esGo.AddComponent<InputSystemUIInputModule>();
                esGo.AddComponent<StandaloneInputModule>();
            }
            else
            {
                if (_eventSystem.GetComponent<InputSystemUIInputModule>() == null)
                {
                    _eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
                }
                if (_eventSystem.GetComponent<StandaloneInputModule>() == null)
                {
                    _eventSystem.gameObject.AddComponent<StandaloneInputModule>();
                }
            }

            // 3. Ensure Managers exist
            if (_soundManager == null) _soundManager = FindAnyObjectByType<SoundManager>();
            if (_soundManager == null)
            {
                var smGo = new GameObject("SoundManager");
                _soundManager = smGo.AddComponent<SoundManager>();
            }
            _soundManager.Init();

            if (_particleManager == null) _particleManager = FindAnyObjectByType<ParticleManager>();
            if (_particleManager == null)
            {
                var pmGo = new GameObject("ParticleManager");
                _particleManager = pmGo.AddComponent<ParticleManager>();
            }
            _particleManager.Init();

            // 4. Ensure RoadBuilder exists
            if (_road == null) _road = FindAnyObjectByType<RoadBuilder>();
            if (_road == null)
            {
                var rGo = new GameObject("Road");
                _road = rGo.AddComponent<RoadBuilder>();
            }
            _roadMaterial = new PhysicsMaterial2D("Road") { friction = 1.25f, bounciness = 0f };
            _road.Init(_roadMaterial);

            // 5. Ensure the background director exists
            if (_background == null) _background = FindAnyObjectByType<BackgroundDirector>();
            if (_background == null)
            {
                var bgGo = new GameObject("Background");
                _background = bgGo.AddComponent<BackgroundDirector>();
            }
            _background.Init();

            _brush = new BrushController(_cam, _road)
            {
                InkAvailable = () => currentInk,
                SpendInk = amount =>
                {
                    currentInk = Mathf.Max(0f, currentInk - amount);
                    UpdateInkUI();
                }
            };
            _pickups = new PickupSpawner(_road);
            _cameraRig = new GameCameraRig(_cam, _background);

            LoadPlayerData();
            _background.ApplyTheme(SelectedTheme);
            ApplySurface();

            GameServices.Actions = this;

            UpdateResponsiveLayout(force: true);
            SwitchScreen(ScreenState.HomeDashboard);
        }

        /// <summary>
        /// Dynamically adapts CanvasScaler, SafeArea insets, and Camera orthographic size
        /// so the UI and gameplay work on ANY aspect ratio (21:9 ultrawide, 16:9, 4:3 iPad, 9:16 portrait).
        /// </summary>
        public void UpdateResponsiveLayout(bool force = false)
        {
            int w = Screen.width;
            int h = Screen.height;
            Rect safe = Screen.safeArea;

            if (!force && w == _lastScreenW && h == _lastScreenH && safe == _lastSafeArea)
                return;

            _lastScreenW = w;
            _lastScreenH = h;
            _lastSafeArea = safe;

            float aspect = (h > 0) ? (float)w / h : 1.777f;

            // 3. Adapt Camera orthographic size so narrow/portrait screens still see the same horizontal look-ahead distance!
            if (_cam != null)
            {
                // Reference aspect is 16:9 (1.777). If narrower than 1.5, expand orthographicSize proportionally.
                if (aspect < 1.5f)
                {
                    _cam.orthographicSize = Mathf.Clamp(cameraSize * (1.5f / Mathf.Max(0.45f, aspect)), cameraSize, 13.5f);
                }
                else
                {
                    _cam.orthographicSize = cameraSize;
                }
            }
        }

        private void LoadPlayerData()
        {
            _profile = SaveService.Load();
            totalCoins = _profile.coins;
            _bestScore = _profile.bestDistance;
            engineLevel = _profile.engineLevel;
            suspensionLevel = _profile.suspensionLevel;
            inkTankLevel = _profile.inkTankLevel;
            SelectedVehicle = (VehicleType)_profile.vehicle;
            SelectedTheme = (EnvironmentTheme)_profile.theme;
            SelectedRoadStyle = (RoadStyle)_profile.roadStyle;
            sportUnlocked = _profile.sportUnlocked;
            truckUnlocked = _profile.truckUnlocked;
            _soundEnabled = _profile.soundEnabled;
            _selVehicleId = _profile.selectedVehicleId ?? "";
            _selSurfaceId = _profile.selectedSurfaceId ?? "";
            _selWorldId = _profile.selectedWorldId ?? "";
            _unlockedVehicleIds.Clear();
            if (_profile.unlockedVehicleIds != null) foreach (var id in _profile.unlockedVehicleIds) _unlockedVehicleIds.Add(id);
            AudioListener.volume = _soundEnabled ? 1f : 0f;
        }

        private void SavePlayerData()
        {
            if (_profile == null) _profile = new PlayerProfile();
            _profile.coins = totalCoins;
            _profile.bestDistance = _bestScore;
            _profile.engineLevel = engineLevel;
            _profile.suspensionLevel = suspensionLevel;
            _profile.inkTankLevel = inkTankLevel;
            _profile.vehicle = (int)SelectedVehicle;
            _profile.theme = (int)SelectedTheme;
            _profile.roadStyle = (int)SelectedRoadStyle;
            _profile.sportUnlocked = sportUnlocked;
            _profile.truckUnlocked = truckUnlocked;
            _profile.soundEnabled = _soundEnabled;
            _profile.selectedVehicleId = _selVehicleId;
            _profile.selectedSurfaceId = _selSurfaceId;
            _profile.selectedWorldId = _selWorldId;
            _profile.unlockedVehicleIds = new List<string>(_unlockedVehicleIds);
            SaveService.Save(_profile);
        }

        public void SwitchScreen(ScreenState newState)
        {
            _state = newState;
            _stateTime = 0f;

            switch (newState)
            {
                case ScreenState.HomeDashboard:
                    SetupShowcaseCar();
                    if (SoundManager.Instance) SoundManager.Instance.SetEngineRunning(false, 0f);
                    SyncModel();
                    break;

                case ScreenState.StageSelect:
                    SyncModel();
                    break;

                case ScreenState.GarageMenu:
                    SetupShowcaseCar();
                    SyncModel();
                    break;

                case ScreenState.Playing:
                    StartGameplaySession();
                    SyncModel();
                    break;

                case ScreenState.GameOver:
                    if (SoundManager.Instance)
                    {
                        SoundManager.Instance.SetEngineRunning(false, 0f);
                        SoundManager.Instance.PlayCrash();
                    }
                    SyncModel();
                    break;
            }

            Model.SetScreen(newState);
        }

        private void CleanupAllCars()
        {
            if (_car != null)
            {
                Destroy(_car.gameObject);
                _car = null;
            }
            var existingCars = FindObjectsByType<RoadCar>(FindObjectsSortMode.None);
            foreach (var c in existingCars)
            {
                if (c != null) Destroy(c.gameObject);
            }
        }

        private void SetupShowcaseCar()
        {
            CleanupAllCars();
            ApplySurface();
            _road.ResetRoad(-15f, 25f, 0f);
            _car = CreateSelectedCar(new Vector2(0f, 1.2f));
            _car.MotorOn = false;

            _cam.transform.position = new Vector3(0f, 1.8f, -10f);
            _background.Follow(_cam);
        }

        private void StartGameplaySession()
        {
            CleanupAllCars();

            _background.ApplyTheme(SelectedTheme);
            ApplySurface();
            _road.ResetRoad(-20f, 12f, 0f);

            _car = CreateSelectedCar(new Vector2(-2f, 1.2f));
            _car.MotorOn = true;

            // Apply Upgrades:
            float engineBonus = (engineLevel - 1) * 0.8f;
            _car.TargetSpeed = (baseStartSpeed + engineBonus) * _car.SpeedMultiplier;
            _car.maxTorquePerWheel += (suspensionLevel - 1) * 3f;

            maxInk = 100f + (inkTankLevel - 1) * 30f;
            currentInk = maxInk;

            _startX = _car.Body.position.x;
            _distance = 0f;
            _upsideDownTime = 0f;
            _pickups.Reset(16f);
            _brush.Cancel();
            currentLives = maxLives;
            sessionCoins = 0;

            UpdateHeartsUI();
            UpdateInkUI();
            if (SoundManager.Instance) SoundManager.Instance.SetEngineRunning(true, 0.2f);
            _cameraRig.Snap(_car);
        }

        private void Update()
        {
            _stateTime += Time.deltaTime;
            UpdateResponsiveLayout(force: false);

            if (_state == ScreenState.Playing)
            {
                _brush.SmoothingSpeed = brushSmoothingSpeed;
                _brush.DragSpacing = dragSpacing;
                _brush.InkCostPerMeter = inkCostPerMeter;
                _brush.Tick();

                currentInk = Mathf.Min(maxInk, currentInk + inkRegenPerSec * Time.deltaTime);

                if (_car == null) return;

                Vector2 carPos = _car.Body.position;
                _distance = Mathf.Max(_distance, carPos.x - _startX);
                float speedRatio = Mathf.InverseLerp(baseStartSpeed, baseMaxSpeed + (engineLevel * 0.8f), _car.TargetSpeed);
                _car.TargetSpeed = Mathf.Min(baseMaxSpeed + (engineLevel * 0.8f), baseStartSpeed + (engineLevel * 0.8f) + _distance * speedGainPerMetre);
                if (SoundManager.Instance) SoundManager.Instance.SetEngineRunning(true, speedRatio);

                _pickups.Tick(carPos.x);

                // Fail conditions:
                // Only check chasm drop against a real road under or behind the car, or an absolute abyss floor (-12f)
                float referenceFloorY = -4f;
                if (_road.TryGetRoadYAtX(carPos.x, out float roadUnderCarY))
                {
                    referenceFloorY = roadUnderCarY;
                }
                else
                {
                    float nearbyLowest = _road.LowestY(carPos.x - 10f, carPos.x + 4f);
                    if (nearbyLowest > -9.5f) referenceFloorY = nearbyLowest;
                }

                if (carPos.y < referenceFloorY - fallDistance || carPos.y < -12f)
                {
                    HandleLifeLoss("FELL INTO THE CHASM!");
                }
                else if (_car.HeadHit)
                {
                    HandleLifeLoss("DRIVER CRASHED!");
                }
                else
                {
                    _upsideDownTime = _car.IsUpsideDown ? _upsideDownTime + Time.deltaTime : 0f;
                    if (_upsideDownTime > flipLoseTime)
                    {
                        HandleLifeLoss("VEHICLE FLIPPED!");
                    }
                }

                _road.CullBefore(_cam.transform.position.x - cameraSize * _cam.aspect - 15f);

                SyncHud();
            }
        }

        private void HandleLifeLoss(string reason)
        {
            _brush.Cancel();
            currentLives--;
            UpdateHeartsUI();
            if (SoundManager.Instance) SoundManager.Instance.PlayCrash();
            if (ParticleManager.Instance && _car != null)
                ParticleManager.Instance.SpawnBurst(_car.Body.position, new Color(1f, 0.3f, 0.2f), 22);

            if (currentLives <= 0)
            {
                TriggerGameOver(reason);
            }
            else
            {
                // Build a guaranteed flat safety runway at the frontier so the car never drops into nothingness!
                Vector2 safeSurfacePt = _road.CreateSafetyPlatformAndFindRespawn(_car.Body.position.x, 0f);
                _car.RespawnSafely(safeSurfacePt);
                _upsideDownTime = 0f;
            }
        }

        private void TriggerGameOver(string reason)
        {
            bool newBest = _distance > _bestScore;
            if (newBest) _bestScore = _distance;
            totalCoins += sessionCoins;
            SavePlayerData();

            Model.LastRun = new RunResult
            {
                reason = reason,
                distance = _distance,
                coinsWon = sessionCoins,
                totalCoins = totalCoins,
                newBest = newBest
            };

            SwitchScreen(ScreenState.GameOver);
        }

        private void LateUpdate()
        {
            _cameraRig.Tick(_car, _state == ScreenState.Playing);
        }

        public void OnCollectItem(Collectible item)
        {
            switch (item.itemType)
            {
                case Collectible.Type.Coin:
                    sessionCoins += 10;
                    if (SoundManager.Instance) SoundManager.Instance.PlayCoin();
                    if (ParticleManager.Instance) ParticleManager.Instance.SpawnBurst(item.transform.position, new Color(1f, 0.85f, 0.2f), 12);
                    break;
                case Collectible.Type.Heart:
                    if (currentLives < maxLives) currentLives++;
                    UpdateHeartsUI();
                    if (SoundManager.Instance) SoundManager.Instance.PlayHeart();
                    if (ParticleManager.Instance) ParticleManager.Instance.SpawnBurst(item.transform.position, new Color(1f, 0.25f, 0.45f), 16);
                    break;
                case Collectible.Type.Ink:
                    currentInk = Mathf.Min(maxInk, currentInk + 45f);
                    UpdateInkUI();
                    if (SoundManager.Instance) SoundManager.Instance.PlayCoin();
                    if (ParticleManager.Instance) ParticleManager.Instance.SpawnBurst(item.transform.position, new Color(0.6f, 0.2f, 0.9f), 14);
                    break;
            }
        }

        // ==================================================================== MODEL SYNC (UI reads this)
        private static GameModel Model => GameServices.Model;

        private void SyncModel()
        {
            var m = Model;
            m.Coins = totalCoins;
            m.BestDistance = _bestScore;
            m.EngineLevel = engineLevel;
            m.SuspensionLevel = suspensionLevel;
            m.InkTankLevel = inkTankLevel;
            m.Vehicle = SelectedVehicle;
            m.World = SelectedTheme;
            m.RoadStyle = SelectedRoadStyle;
            m.SportUnlocked = sportUnlocked;
            m.TruckUnlocked = truckUnlocked;
            m.SoundEnabled = _soundEnabled;
            m.SelectedVehicleId = _selVehicleId;
            m.SelectedSurfaceId = _selSurfaceId;
            m.SelectedWorldId = _selWorldId;
            m.UnlockedVehicleIds.Clear();
            foreach (var id in _unlockedVehicleIds) m.UnlockedVehicleIds.Add(id);
            m.RaiseChanged();
        }

        private void SyncHud()
        {
            var m = Model;
            m.Distance = _distance;
            m.SpeedKmh = _car != null ? _car.TargetSpeed * 3.6f : 0f;
            m.SessionCoins = sessionCoins;
            m.Lives = currentLives;
            m.MaxLives = maxLives;
            m.Ink01 = maxInk > 0f ? Mathf.Clamp01(currentInk / maxInk) : 0f;
            m.RaiseHud();
        }

        private void UpdateHeartsUI() { SyncHud(); }
        private void UpdateInkUI() { SyncHud(); }

        private void OnDestroy()
        {
            if (GameServices.Actions == (IGameActions)this) GameServices.Actions = null;
        }

        // ==================================================================== ACTIONS (IGameActions)
        public void StartRace()
        {
            // Never start a run in a car the player has not unlocked.
            if (!IsCurrentVehicleUnlocked()) SelectedVehicle = VehicleType.ClassicRed;
            if (CatalogVehicle() == null) _selVehicleId = "";
            SwitchScreen(ScreenState.Playing);
        }

        public void GoHome() { SwitchScreen(ScreenState.HomeDashboard); }
        public void OpenGarage() { SwitchScreen(ScreenState.GarageMenu); }
        public void OpenWorlds() { SwitchScreen(ScreenState.StageSelect); }

        public void Boost()
        {
            if (_car == null) return;
            if (SoundManager.Instance) SoundManager.Instance.PlayUpgrade();
            if (ParticleManager.Instance)
                ParticleManager.Instance.SpawnBurst(_car.Body.position, new Color(0.2f, 0.95f, 1f), 14);
            _car.NudgeUnstick();
        }

        public void CycleWorld(int dir)
        {
            int total = System.Enum.GetValues(typeof(EnvironmentTheme)).Length;
            SelectedTheme = (EnvironmentTheme)(((int)SelectedTheme + dir + total) % total);
            _background.ApplyTheme(SelectedTheme);
            SavePlayerData();
            SyncModel();
        }

        public void CycleRoadStyle()
        {
            int total = System.Enum.GetValues(typeof(RoadStyle)).Length;
            SelectedRoadStyle = (RoadStyle)(((int)SelectedRoadStyle + 1) % total);
            _selSurfaceId = "";
            ApplySurface();
            if (SoundManager.Instance) SoundManager.Instance.PlayUpgrade();
            SavePlayerData();
            SyncModel();
        }

        public void CycleVehicle(int dir)
        {
            int total = System.Enum.GetValues(typeof(VehicleType)).Length;
            SelectedVehicle = (VehicleType)(((int)SelectedVehicle + dir + total) % total);
            _selVehicleId = "";
            SavePlayerData();
            SetupShowcaseCar();
            SyncModel();
        }

        public void SelectOrUnlockVehicle()
        {
            if (IsCurrentVehicleUnlocked())
            {
                if (SoundManager.Instance) SoundManager.Instance.PlayUpgrade();
                SavePlayerData();
            }
            else
            {
                int cost = GameCatalog.VehicleCost(SelectedVehicle);
                if (totalCoins >= cost)
                {
                    totalCoins -= cost;
                    if (SelectedVehicle == VehicleType.SportCyan) sportUnlocked = true;
                    if (SelectedVehicle == VehicleType.MonsterTruck) truckUnlocked = true;
                    if (SoundManager.Instance) SoundManager.Instance.PlayUpgrade();
                    SavePlayerData();
                }
                else
                {
                    if (SoundManager.Instance) SoundManager.Instance.PlayCrash();
                }
            }
            SyncModel();
        }

        public void UpgradeStat(int statIndex)
        {
            int currentLevel = (statIndex == 0) ? engineLevel : ((statIndex == 1) ? suspensionLevel : inkTankLevel);
            if (currentLevel >= GameCatalog.MaxUpgradeLevel) return;

            int cost = GameCatalog.UpgradeCost(currentLevel);
            if (totalCoins >= cost)
            {
                totalCoins -= cost;
                if (statIndex == 0) engineLevel++;
                else if (statIndex == 1) suspensionLevel++;
                else if (statIndex == 2) inkTankLevel++;

                if (SoundManager.Instance) SoundManager.Instance.PlayUpgrade();
                SavePlayerData();
                SyncModel();
            }
            else
            {
                if (SoundManager.Instance) SoundManager.Instance.PlayCrash();
            }
        }

        /// <summary>
        /// A catalogue pick. Vehicles: buy when locked (coins), then drive it. Surfaces: the next road is drawn with it and
        /// uses its grip. Worlds: remembered only (the background remixer does not read it yet). Unplayable items are refused.
        /// </summary>
        public void SelectCatalogItem(CatalogKind kind, string id)
        {
            id = id ?? "";
            var entry = AssetCatalog.Load()?.Find(id);
            if (entry != null && kind != CatalogKind.World && !entry.playable)
            {
                if (SoundManager.Instance) SoundManager.Instance.PlayCrash();
                return;
            }

            if (kind == CatalogKind.Vehicle)
            {
                if (entry != null && entry.cost > 0 && !_unlockedVehicleIds.Contains(id))
                {
                    if (totalCoins < entry.cost)
                    {
                        if (SoundManager.Instance) SoundManager.Instance.PlayCrash();
                        return;
                    }
                    totalCoins -= entry.cost;
                    _unlockedVehicleIds.Add(id);
                }
                _selVehicleId = id;
                if (SoundManager.Instance) SoundManager.Instance.PlayUpgrade();
                SavePlayerData();
                SyncModel();
                if (_state == ScreenState.GarageMenu || _state == ScreenState.HomeDashboard) SetupShowcaseCar();
                return;
            }
            if (kind == CatalogKind.Surface)
            {
                _selSurfaceId = id;
                ApplySurface();
            }
            else _selWorldId = id;
            if (SoundManager.Instance) SoundManager.Instance.PlayUpgrade();
            SavePlayerData();
            SyncModel();
        }

        // ---------------------------------------------------------------- catalogue vehicles and surfaces
        private CatalogEntry CatalogVehicle()
        {
            if (string.IsNullOrEmpty(_selVehicleId)) return null;
            var e = AssetCatalog.Load()?.Find(_selVehicleId);
            if (e == null || e.kind != CatalogKind.Vehicle || !e.playable || e.preview == null) return null;
            if (e.cost > 0 && !_unlockedVehicleIds.Contains(e.id)) return null;
            return e;
        }

        private RoadCar CreateSelectedCar(Vector2 pos)
        {
            var e = CatalogVehicle();
            if (e == null) return RoadCar.Create(pos, SelectedVehicle);
            return RoadCar.Create(pos, new VehicleRig
            {
                id = e.id, body = e.preview, width = e.widthUnits, mass = e.mass, torque = e.torque, speedMultiplier = e.speed
            });
        }

        /// <summary>Road skin and grip for the current surface pick (or the legacy road style). Looks and friction only.</summary>
        private void ApplySurface()
        {
            var e = string.IsNullOrEmpty(_selSurfaceId) ? null : AssetCatalog.Load()?.Find(_selSurfaceId);
            if (e != null && e.kind == CatalogKind.Surface && e.playable)
            {
                _roadMaterial.friction = e.friction;
                _roadMaterial.bounciness = e.bounce;
                if (e.strip != null)
                {
                    if (!_surfaceSkins.TryGetValue(e.id, out var skin) || skin == null)
                    {
                        skin = ScriptableObject.CreateInstance<RoadSkin>();
                        skin.displayName = e.displayName;
                        skin.atlas = e.strip;
                        _surfaceSkins[e.id] = skin;
                    }
                    _road.SetSkin(skin);
                    return;
                }
                if (e.roadStyle >= 0) { SelectedRoadStyle = (RoadStyle)e.roadStyle; _road.SetRoadStyle(SelectedRoadStyle); return; }
            }
            _roadMaterial.friction = 1.25f;
            _roadMaterial.bounciness = 0f;
            _road.SetRoadStyle(SelectedRoadStyle);
        }

        public void SetSoundEnabled(bool enabled)
        {
            _soundEnabled = enabled;
            AudioListener.volume = enabled ? 1f : 0f;
            SavePlayerData();
            SyncModel();
        }

        private bool IsCurrentVehicleUnlocked()
        {
            if (SelectedVehicle == VehicleType.ClassicRed) return true;
            if (SelectedVehicle == VehicleType.SportCyan) return sportUnlocked;
            if (SelectedVehicle == VehicleType.MonsterTruck) return truckUnlocked;
            return false;
        }
    }
}
