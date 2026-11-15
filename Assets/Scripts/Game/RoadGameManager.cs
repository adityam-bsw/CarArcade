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
        public int maxLives = 3;
        public int currentLives = 3;
        public int totalCoins = 150;
        public int sessionCoins = 0;

        [Header("Player Upgrades")]
        public int engineLevel = 1;
        public int suspensionLevel = 1;
        public int inkTankLevel = 1;
        public int boostLevel = 1;

        [Header("Vehicle Unlocks")]
        public bool sportUnlocked = false;
        public bool truckUnlocked = false;

        private GameplayTuning _tuning;
        private RunDirector _run;
        private MetaProgress _meta;
        private int _powerKey = -1;
        private string _powerText = "";
        private PlayerProfile _profile;
        private bool _soundEnabled = true;
        private AudioDirector _audio;
        private MeteorSpawner _meteors;
        // Stage events (vertical slice). Everything below is inert unless GameplayTuning.stageEvents allows the world on screen.
        private StageRunController _stage;
        private StageWarningPresenter _stagePresenter;
        private readonly SurfaceZoneModel _zone = new SurfaceZoneModel();
        // Section C: the player-selected surface runs its behaviour set on the real road (per-piece pooled state, outputs applied to the car), and projectile patches act through it.
        private readonly SurfaceRuntime _surf = new SurfaceRuntime();
        private float _surfBaseFriction = 1.25f; private bool _surfCut;
        private bool _stageOn;
        // Mutation events (gap list A1): folded effect values, the two ladders, the keep-going switch and the adaptive eased stages of this run.
        private readonly StageEffectState _fx = new StageEffectState();
        private StageLadderDefinition _sliceLadder, _endlessLadder;
        private bool _sliceCleared;
        private float _baseMaxInk = 100f;
        private StageWorldContext _worldCtx;
        // Road-Eater, RO-B0, near-miss economy (phase B2). Inert unless the stage director runs.
        private RoadEaterController _eater;
        private BossController _boss;
        private MonsterDefinitionSO _eaterData;
        private Announcer _announcer; private readonly HitStop _hitStop = new HitStop(); private readonly FinishGateProp _gateProp = new FinishGateProp(); private string _hazardWorldId = "";
        private BossDefinitionSO _bossData;
        private readonly NearMissLedger _nearLedger = new NearMissLedger();
        private float _hitGraceLeft, _recentSpeed, _bossEndS = -1f, _meteorWarnDist;
        private bool _finishing, _runEnding;
        private ScreenState _previousState = ScreenState.HomeDashboard;
        private float _frenzyTrailTimer;
        // Track traits of the current surface (CatalogEntry.speedFactor / inkFactor / coinFactor); 1 = plain asphalt.
        private float _trackSpeed = 1f, _trackInk = 1f;
        private string _selVehicleId = "", _selSurfaceId = "", _selWorldId = "";
        private readonly HashSet<string> _unlockedVehicleIds = new HashSet<string>();
        private readonly Dictionary<string, BackgroundSkin> _worldSkins = new Dictionary<string, BackgroundSkin>();
        // Art style of the world on screen. Surfaces switch to their smooth strip only when this is Smooth, so a road never
        // changes style on its own: a pixel world keeps pixel roads.
        private ArtStyle _worldArt = ArtStyle.Pixel;
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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // Read access for DebugAutoDraw (test driver). Not part of the game.
        public BrushController DebugBrush => _brush;
        public RoadCar DebugCar => _car;
        public RoadBuilder DebugRoad => _road;
        public ScreenState DebugState => _state;
#endif

        public VehicleType SelectedVehicle { get; private set; } = VehicleType.ClassicRed;
        public EnvironmentTheme SelectedTheme { get; private set; } = EnvironmentTheme.Country;
        public RoadStyle SelectedRoadStyle { get; private set; } = RoadStyle.AsphaltHighway;

        private int _lastScreenW, _lastScreenH;
        private Rect _lastSafeArea;

        private void OnEnable() { Collectible.Collected += OnCollectItem; }
        private void OnDisable() { Collectible.Collected -= OnCollectItem; AudioListener.pause = false; Time.timeScale = 1f; Time.fixedDeltaTime = 0.02f; Collectible.MagnetTarget = null; Collectible.RepelTarget = null; }

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
            _tuning = GameplayTuning.Load();
            _run = new RunDirector(_tuning);
            _run.Bonus += OnRunBonus;
            _audio = new AudioDirector();
            _audio.Bind(_run);
            _pickups = new PickupSpawner(_road)
            {
                Tuning = _tuning,
                Ink01 = () => maxInk > 0f ? currentInk / maxInk : 1f,
                InkUnits = () => currentInk,
                InkPerMeter = () => inkCostPerMeter * _trackInk * _vp.ink,
                NeedsHeart = () => currentLives < maxLives
            };
            _cameraRig = new GameCameraRig(_cam, _background);
            _meteors = new MeteorSpawner(_road, _tuning);
            _meteors.Warned += () => _audio.MeteorWarning();
            _meteors.NearMissed += () => { if (!_stageOn) _run.NearMiss(); };      // stage mode pays through the near-miss ledger instead
            _meteors.NearMissedAt += OnMeteorNearMiss;
            _meteors.Warned += () => _meteorWarnDist = _distance;
            _meteors.Impacted += OnMeteorImpact;
            _run.CueRaised += OnRunCue;
            SetupStageEvents();
            _surf.Bind(new RoadBuilderSurfaceRoad(_road), SurfaceBehaviourLibrary.Interactions);
            _surf.Debris = (p, c, n) => { if (ParticleManager.Instance == null) return; if (_road.TryGetRoadYAtX(p.x, out float ry)) p.y = ry + 0.3f; ParticleManager.Instance.SpawnBurst(p, c, n); };
            ProjectileSurfaceHooks.Current = new SurfaceProjectileHook(_surf);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            gameObject.AddComponent<QaJumpOverlay>().Init(this);
#endif

            VehicleEconomy.SyncCatalog(AssetCatalog.Load(), VehicleGateSO.Load());   // blueprint D2 prices into the catalogue (idempotent)
            LoadPlayerData();
            _meta = new MetaProgress(_profile, _tuning);
            _tree = UpgradeTreeSO.Load(); _prog = ProgressionTuningSO.Load(); _worldGates = WorldGateSO.Load(); _vehicleGates = VehicleGateSO.Load();
            _surfaceGates = SurfaceGateSO.Load(); _vehicleProfiles = VehicleProfileSO.Load();
            Cosmetics.Reconcile(_profile, _prog);
            _hub = new ProgressionController(_profile) { Purchases = FakePurchaseService.CreateDefault() };
            _offers = new AdOffers(_profile, _prog, AdOffers.CreateDefaultService(), null);
            _meta.EnsureMissions();
            ApplyWorld();
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
            // Starter cars bought before the collection existed carry over.
            if (sportUnlocked) _unlockedVehicleIds.Add("vehicle-sports-car");
            if (truckUnlocked) _unlockedVehicleIds.Add("vehicle-monster-truck");
            // Always have a pick, so the rails show something equipped. Valid only if it exists in the catalogue.
            // A saved pick that is locked or no longer playable falls back to the free starter car.
            var pickedVehicle = AssetCatalog.Load()?.Find(_selVehicleId);
            if (string.IsNullOrEmpty(_selVehicleId) || pickedVehicle == null || !pickedVehicle.playable
                || (pickedVehicle.cost > 0 && !_unlockedVehicleIds.Contains(_selVehicleId)))
                _selVehicleId = "vehicle-car";
            if (string.IsNullOrEmpty(_selSurfaceId)) _selSurfaceId = "surface-asphalt";
            if (string.IsNullOrEmpty(_selWorldId)) _selWorldId = SelectedTheme == EnvironmentTheme.Desert ? "world-desert" : (SelectedTheme == EnvironmentTheme.CyberNight ? "world-night-city" : "world-rolling-hills");
            AudioListener.volume = _soundEnabled ? 1f : 0f;
        }

        // ---- Section D: upgrade levels (0..5) are stored per vehicle in the profile; these fields mirror the selected vehicle.
        private UpgradeTreeSO _tree;
        private ProgressionTuningSO _prog;
        private string _upgradeKey = "";
        private float _engineBonus;
        private int _shieldLevel;
        private WorldGateSO _worldGates;
        private VehicleGateSO _vehicleGates;
        private SurfaceGateSO _surfaceGates;
        private VehicleProfileSO _vehicleProfiles;
        private ProgressionController _hub;
        private VehicleStats _vp = VehicleStats.Identity;   // strength and weakness of the vehicle in the run (VehicleProfile + scrap reinforce)
        private AdOffers _offers;
        // Daily challenge run and post-run offers (section D part 2)
        private bool _dailyActive, _reviving, _canRevive, _runDoubled, _finishedRun;
        private DailyChallengeDef _daily;
        private string _dailySavedVehicle = "", _dailySavedWorld = "";
        private float _deathDistance;

        private string VehicleKey() { return string.IsNullOrEmpty(_selVehicleId) ? "vehicle-enum-" + (int)SelectedVehicle : _selVehicleId; }

        private void RefreshUpgradeLevels()
        {
            if (_tree == null) _tree = UpgradeTreeSO.Load();
            if (_prog == null) _prog = ProgressionTuningSO.Load();
            if (_profile == null) return;
            string key = VehicleKey();
            if (key == _upgradeKey) return;
            _upgradeKey = key;
            var e = _profile.UpgradesFor(key);
            engineLevel = e.levels[0]; suspensionLevel = e.levels[1]; inkTankLevel = e.levels[2]; boostLevel = e.levels[3]; _shieldLevel = e.levels[4];
        }

        private void StoreUpgradeLevels()
        {
            if (string.IsNullOrEmpty(_upgradeKey) || _profile == null) return;
            var e = _profile.UpgradesFor(_upgradeKey);
            e.levels[0] = engineLevel; e.levels[1] = suspensionLevel; e.levels[2] = inkTankLevel; e.levels[3] = boostLevel; e.levels[4] = _shieldLevel;
        }

        private void SavePlayerData()
        {
            if (_profile == null) _profile = new PlayerProfile();
            _profile.coins = totalCoins;
            _profile.bestDistance = _bestScore;
            StoreUpgradeLevels();
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
            ClearPause();
            _previousState = _state;
            _state = newState;
            _stateTime = 0f;
            if (newState != ScreenState.Playing)
            {
                ApplyTimeScale(1f); Collectible.MagnetTarget = null; Collectible.RepelTarget = null;
                if (newState != ScreenState.GameOver) EndDaily();
                EndStageRun(StageRunOutcome.Quit);       // no-op when the run already ended as Died
            }

            switch (newState)
            {
                case ScreenState.HomeDashboard:
                    SetupShowcaseCar();
                    if (SoundManager.Instance) SoundManager.Instance.SetEngineRunning(false, 0f);
                    SyncModel();
                    break;

                case ScreenState.StageSelect:
                    SetupShowcaseCar();   // the door shows the live world and road with the showcase car
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

                case ScreenState.Rewards:
                    SyncModel();
                    break;

                case ScreenState.GameOver:
                    _audio?.GameOver(Model.LastRun.newBest);
                    SyncModel();
                    break;
            }

            _audio?.OnScreen(newState, _selWorldId);
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

            _cam.transform.position = new Vector3(0f, 2.35f, -10f);
            _background.Follow(_cam);
        }

        private void StartGameplaySession()
        {
            CleanupAllCars();

            ApplyWorld();
            ApplySurface();
            _road.ResetRoad(-20f, 12f, 0f);

            _car = CreateSelectedCar(new Vector2(-2f, 1.2f));
            _car.MotorOn = true;

            // Apply Upgrades:
            RefreshUpgradeLevels();
            float engineBonus = _engineBonus = _tree.Bonus(UpgradeBranch.Engine, _upgradeKey, engineLevel);
            _car.TargetSpeed = (baseStartSpeed + engineBonus) * _car.SpeedMultiplier * _trackSpeed;
            _car.maxTorquePerWheel += _tree.Bonus(UpgradeBranch.Suspension, _upgradeKey, suspensionLevel);
            _vp = VehicleEconomy.Effective(_profile, _prog, _vehicleProfiles, _upgradeKey);
            VehicleEconomy.Active = _vp; ProgressionEvents.ActiveVehicle = _upgradeKey;
            _car.maxTorquePerWheel *= _vp.torque;
            _car.TargetSpeed *= _vp.speed;

            maxInk = _tree.Base(UpgradeBranch.InkTank, _upgradeKey) + _tree.Bonus(UpgradeBranch.InkTank, _upgradeKey, inkTankLevel);
            _baseMaxInk = maxInk;
            currentInk = maxInk;

            _startX = _car.Body.position.x;
            _distance = 0f;
            _upsideDownTime = 0f;
            _pickups.Art = _worldArt;
            _pickups.Reset(16f);
            _brush.Cancel();
            currentLives = maxLives;
            sessionCoins = 0;
            _run.Begin(boostLevel, _bestScore);
            _run.BoostMult = _vp.boost;
            _run.UpShield.Begin(_shieldLevel, _tree.ShieldRecharge(_shieldLevel));
            _offers.BeginRun(_reviving);
            _runDoubled = false;
            _meteors.Reset();
            _powerKey = -1;
            BeginStageRun(_previousState == ScreenState.GameOver);
            if (_reviving) { _reviving = false; if (_stage.Active) { JumpRun(_deathDistance); currentLives = 1; } }

            UpdateHeartsUI();
            UpdateInkUI();
            if (SoundManager.Instance) SoundManager.Instance.SetEngineRunning(true, 0.2f);
            _audio?.RunStart();
            _cameraRig.Snap(_car);
        }

        private void Update()
        {
            _stateTime += Time.deltaTime;
            UpdateResponsiveLayout(force: false);

            if (_state == ScreenState.Playing)
            {
                if (_paused) return;                   // paused: no gameplay system ticks (scaled time is 0 as well; this also stops the unscaled run director)
                _brush.SmoothingSpeed = brushSmoothingSpeed;
                _brush.DragSpacing = dragSpacing;
                _brush.InkCostPerMeter = _run.Ghost ? 0f : inkCostPerMeter * _trackInk * _vp.ink;
                _brush.Tick();

                currentInk = Mathf.Min(maxInk, currentInk + _tuning.inkRegenPerSec * _fx.InkRegenMul * Time.deltaTime);

                if (_car == null) return;

                Vector2 carPos = _car.Body.position;
                _distance = Mathf.Max(_distance, carPos.x - _startX);
                float speedRatio = Mathf.InverseLerp(baseStartSpeed, baseMaxSpeed + _engineBonus, _car.TargetSpeed);
                float baseTarget = Mathf.Min(baseMaxSpeed + _engineBonus, baseStartSpeed + _engineBonus + _distance * speedGainPerMetre * _vp.accel) * _trackSpeed * _vp.speed;
                _fx.DistanceM = _distance;
                if (_stage.Active)
                {
                    _stage.Tick(_distance, _car.Body.linearVelocity.x, Time.deltaTime);     // scaled time: slow-mo stretches a warning, never shortens it
                    _stagePresenter.Tick(_distance, Time.deltaTime);
                    TickHazards(carPos, Time.deltaTime);
                    if (_state != ScreenState.Playing) return;            // a bite or a hit ended the run this frame
                    if (_zone.Active)
                    {
                        SampleRoadShape(carPos.x, _zone.Rule.iceSlipSpanMetres, out float uphillDeg, out float turnDeg);
                        baseTarget = _zone.Tick(Time.deltaTime, baseTarget, _tuning.MudDragMul(MudVehicleIndex()), uphillDeg, turnDeg, _run.Boosting);
                        _car.MotorOn = !_zone.DriveCut;                                     // ice slip: the car coasts, no damage
                    }
                }
                _run.BoostDrainMul = _fx.BoostDrainMul;
                ApplyStageEffects();
                baseTarget = TickSurfaceRuntime(carPos, baseTarget);
                if (_state != ScreenState.Playing) return;                  // a durability hit ended the run
                _car.TargetSpeed = baseTarget * _fx.SpeedMul + _run.BoostSpeedBonus;
                _run.Tick(Time.unscaledDeltaTime, _car, _distance);
                Collectible.MagnetTarget = _run.MagnetOn ? _car.Body.transform : null;
                Collectible.MagnetRadius = _tuning.magnetRadius;
                ApplyTimeScale(_run.TimeScale);
                if (SoundManager.Instance) SoundManager.Instance.SetEngineRunning(true, speedRatio);

                _pickups.Tick(carPos.x);
                _meteors.Tick(Time.deltaTime, _car, _distance);
                _audio.InkLevel(maxInk > 0f ? currentInk / maxInk : 1f);
                if (_run.Frenzying) FrenzyTrail(carPos);

                if (CheckFragileImpact()) return;                       // a hard hit while the Fragile Vehicle event runs

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
                    if (_upsideDownTime > flipLoseTime * _vp.flip)
                    {
                        HandleLifeLoss("VEHICLE FLIPPED!");
                    }
                }

                _road.CullBefore(_cam.transform.position.x - cameraSize * _cam.aspect - 15f);

                SyncHud();
            }
        }

        // ==================================================================== PAUSE
        private bool _paused;

        /// <summary>
        /// Pause: scaled time 0 (physics, stage timers, Eater, RO-B0, projectiles, near-miss slow-mo all stand still), the run director and the brush
        /// are not ticked (Update returns early), audio effects freeze (music keeps playing). UI tweens use unscaled time.
        /// </summary>
        public void PauseRun()
        {
            if (_state != ScreenState.Playing || _paused || _car == null) return;
            _paused = true;
            _brush.Cancel();
            _hitStop.Release();
            Time.timeScale = 0f;                       // not ApplyTimeScale: fixedDeltaTime must never become 0
            AudioListener.pause = true;
            _stage?.OnPause(true);
            Model.SetPaused(true);
        }

        public void ResumeRun()
        {
            if (!_paused) return;
            _paused = false;
            AudioListener.pause = false;
            _stage?.OnPause(false);                    // 0.5 s hazard-free grace, counted in scaled time
            ApplyTimeScale(_run.TimeScale);            // back to 1, or the slow-mo that was running
            Model.SetPaused(false);
        }

        /// <summary>Leaves the paused run for the menu. The run still counts: coins, best distance and progress are banked, no revive offer.</summary>
        public void QuitRun()
        {
            if (_state != ScreenState.Playing) return;
            TriggerGameOver("RUN ENDED", false, true);
        }

        public void QuitApp()
        {
            SavePlayerData();
            Application.Quit();
        }

        private void ClearPause()
        {
            if (!_paused && !AudioListener.pause) return;
            _paused = false;
            AudioListener.pause = false;
            Model.SetPaused(false);
        }

        private bool HandleLifeLoss(string reason)
        {
            _brush.Cancel();
            bool shielded = _run.AbsorbHit();       // a shield soaks up one crash
            _run.Interrupt();
            _cameraRig?.Shake(shielded ? 0.2f : 0.45f, 0.4f);
            _audio?.Crash(shielded);
            if (shielded)
            {
                if (ParticleManager.Instance && _car != null)
                    ParticleManager.Instance.SpawnBurst(_car.Body.position, new Color(0.4f, 0.65f, 1f), 18);
            }
            else
            {
                currentLives--;
                UpdateHeartsUI();
                if (ParticleManager.Instance && _car != null)
                    ParticleManager.Instance.SpawnBurst(_car.Body.position, new Color(1f, 0.3f, 0.2f), 22);
            }

            if (currentLives <= 0)
            {
                TriggerGameOver(reason);
            }
            else
            {
                // Build a guaranteed flat safety runway at the frontier so the car never drops into nothingness!
                Vector2 safeSurfacePt = _road.CreateSafetyPlatformAndFindRespawn(_car.Body.position.x, 0f);
                _car.RespawnSafely(safeSurfacePt);
                _surf.OnRespawn();
                _upsideDownTime = 0f;
                if (_stage.Active)
                {
                    _stage.OnRespawn(_distance);
                    _hitGraceLeft = 1.5f * _vp.impact;                       // FR-7 and FR-9: no further hazard damage for 1.5 s after any hit, shielded or not
                    _nearLedger.NoteDamage(); _nearLedger.NoteRespawn();
                    _eater.OnLifeLost(_car.Body.position.x);    // push-back (no-op unless the Eater is chasing)
                    _boss.OnRespawn();                          // clear projectiles, 3 s lull (no-op unless RO-B0 is fighting)
                }
            }
            return shielded;
        }

        private void TriggerGameOver(string reason, bool finished = false, bool toMenu = false)
        {
            RecordStageFail(finished, toMenu);
            bool cleared = finished || _sliceCleared;
            EndStageRun(finished ? StageRunOutcome.Finished : StageRunOutcome.Died);
            bool newBest = _distance > _bestScore;
            if (newBest) _bestScore = _distance;
            totalCoins += sessionCoins;
            _meta.RecordRun(_run.Stats, _distance, _selVehicleId);
            RefreshUpgradeLevels();
            _profile.coins = totalCoins;   // mastery pays coins into the profile; keep the manager's balance authoritative
            MasteryService.RecordDistance(_profile, _prog, VehicleKey(), _distance);
            ProgressionEvents.RecordRunEnd(_profile, _prog, VehicleKey(), _distance, cleared, _run.Stats.cleanJumps, _run.Stats.nearMisses);
            _offers.EndRun();
            _deathDistance = _distance; _finishedRun = finished;
            _canRevive = _stageOn && !finished && !toMenu;
            if (_dailyActive) DailyChallenge.SubmitScore(_profile, System.DateTime.UtcNow, Mathf.FloorToInt(_distance));
            RunProgress.Record(_profile, _prog, _surfaceGates, new RunSummary { vehicleKey = VehicleKey(), worldId = _selWorldId, surfaceId = _selSurfaceId, distance = _distance, cleanJumps = _run.Stats.cleanJumps, nearMisses = _run.Stats.nearMisses, flips = _run.Stats.flips, coins = sessionCoins, finished = finished, daily = _dailyActive, utc = System.DateTime.UtcNow });
            totalCoins = _profile.coins;
            SavePlayerData();

            Model.LastRun = new RunResult
            {
                reason = reason,
                distance = _distance,
                coinsWon = sessionCoins,
                totalCoins = totalCoins,
                newBest = newBest,
                gems = _run.Stats.gems,
                bonusCoins = _run.Stats.bonusCoins,
                bonusLines = string.Join("\n", _run.Stats.lines)
            };

            SwitchScreen(toMenu ? ScreenState.HomeDashboard : ScreenState.GameOver);
        }

        private void LateUpdate()
        {
            _cameraRig?.Tick(_car, _state == ScreenState.Playing);
        }

        public void OnCollectItem(Collectible item)
        {
            switch (item.itemType)
            {
                case Collectible.Type.Coin:
                    sessionCoins += _run.CollectCoin();
                    if (SoundManager.Instance) SoundManager.Instance.PlayCoin();
                    if (ParticleManager.Instance) ParticleManager.Instance.SpawnBurst(item.transform.position, new Color(1f, 0.85f, 0.2f), 12);
                    break;
                case Collectible.Type.Heart:
                    if (currentLives < maxLives) currentLives++;
                    UpdateHeartsUI();
                    if (SoundManager.Instance) SoundManager.Instance.PlayHeart();
                    if (ParticleManager.Instance) ParticleManager.Instance.SpawnBurst(item.transform.position, new Color(1f, 0.25f, 0.45f), 16);
                    break;
                case Collectible.Type.CoinGate:
                    _fx.TaxGatesCrossed++;                                  // Road Tax: this gate pays the next checkpoint
                    if (SoundManager.Instance) SoundManager.Instance.PlaySfx("ring", 0.8f);
                    if (ParticleManager.Instance) ParticleManager.Instance.SpawnBurst(item.transform.position, new Color(1f, 0.85f, 0.3f), 16);
                    break;
                case Collectible.Type.Ink:
                    currentInk = Mathf.Min(maxInk, currentInk + _tuning.inkPickup);
                    UpdateInkUI();
                    if (SoundManager.Instance) SoundManager.Instance.PlaySfx("ink", 0.8f);
                    if (ParticleManager.Instance) ParticleManager.Instance.SpawnBurst(item.transform.position, new Color(0.6f, 0.2f, 0.9f), 14);
                    break;
                default:      // gem and the power-ups
                    _run.CollectPower(item.itemType);           // the run cue plays the sound
                    if (ParticleManager.Instance) ParticleManager.Instance.SpawnBurst(item.transform.position, new Color(0.5f, 0.9f, 1f), 18);
                    break;
            }
        }

        private void OnMeteorImpact(Vector2 point, bool hit)
        {
            _audio.MeteorImpact();
            _cameraRig?.Shake(hit ? 0.5f : 0.3f, 0.35f);
            if (ParticleManager.Instance) ParticleManager.Instance.SpawnBurst(point + Vector2.up * 0.3f, new Color(1f, 0.55f, 0.1f), 26);
            if (hit) HandleLifeLoss("METEOR STRIKE!");
        }

        /// <summary>Run cues that need an animation or camera reaction (sound lives in AudioDirector).</summary>
        private void OnRunCue(RunDirector.Cue cue)
        {
            if (_car == null) return;
            Vector2 p = _car.Body.position;
            switch (cue)
            {
                case RunDirector.Cue.FrenzyOn:
                    _cameraRig?.Shake(0.35f, 0.5f);
                    if (ParticleManager.Instance) ParticleManager.Instance.SpawnBurst(p, new Color(1f, 0.3f, 0.9f), 30);
                    break;
                case RunDirector.Cue.Ring:
                    if (ParticleManager.Instance) ParticleManager.Instance.SpawnBurst(p, new Color(1f, 0.85f, 0.3f), 20);
                    _cameraRig?.Shake(0.12f, 0.2f);
                    break;
                case RunDirector.Cue.Boost:
                    _cameraRig?.Shake(0.15f, 0.3f);
                    break;
                case RunDirector.Cue.NearMiss:
                case RunDirector.Cue.Flip:
                    if (ParticleManager.Instance) ParticleManager.Instance.SpawnBurst(p + Vector2.up, new Color(0.4f, 1f, 0.9f), 14);
                    break;
            }
        }

        /// <summary>Rainbow sparks behind the car while frenzy is on (about 12 a second).</summary>
        private void FrenzyTrail(Vector2 carPos)
        {
            _frenzyTrailTimer -= Time.deltaTime;
            if (_frenzyTrailTimer > 0f || ParticleManager.Instance == null) return;
            _frenzyTrailTimer = 0.08f;
            ParticleManager.Instance.SpawnBurst(carPos + Vector2.left * 0.8f, Color.HSVToRGB(Mathf.Repeat(Time.time * 1.5f, 1f), 0.75f, 1f), 2);
        }

        /// <summary>The run director paid out a bonus or announced an event: add the coins and show a toast.</summary>
        private void OnRunBonus(string label, int coins)
        {
            sessionCoins += coins;
            var m = Model;
            m.Toast = coins > 0 ? label + "  +" + coins : label;
            m.ToastId++;
            if (coins > 0 && SoundManager.Instance) SoundManager.Instance.PlayCoin();
            if (coins > 0 && ParticleManager.Instance && _car != null)
                ParticleManager.Instance.SpawnBurst(_car.Body.position + Vector2.up, new Color(1f, 0.85f, 0.2f), 10);
        }

        private void ApplyTimeScale(float scale)
        {
            if (_hitStop.Active) return;               // a perfect near miss holds the frame for a few hundredths of a second
            if (Mathf.Approximately(Time.timeScale, scale)) return;
            Time.timeScale = scale;
            Time.fixedDeltaTime = 0.02f * scale;
        }

        // ==================================================================== MODEL SYNC (UI reads this)
        private static GameModel Model => GameServices.Model;

        private void SyncModel()
        {
            RefreshUpgradeLevels();
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
            m.AdaptiveChallenge = _profile != null && _profile.adaptiveChallenge;
            m.SelectedVehicleId = _selVehicleId;
            m.SelectedSurfaceId = _selSurfaceId;
            m.SelectedWorldId = _selWorldId;
            m.Gems = _profile != null ? _profile.gems : 0;
            m.BoostLevel = boostLevel;
            m.Scrap = _profile != null ? _profile.scrap : 0;
            m.ShieldLevel = _shieldLevel;
            for (int i = 0; i < 5; i++) m.UpgradeCosts[i] = GameCatalog.UpgradeCost(m.LevelOf(i), _upgradeKey);
            SyncProgressionModel(m);
            if (_meta != null)
            {
                _meta.EnsureMissions();
                m.DailyAvailable = _meta.DailyAvailable;
                m.DailyDay = _meta.NextDailyDay;
                m.DailyCoins = _tuning.dailyCoins[Mathf.Clamp(m.DailyDay - 1, 0, _tuning.dailyCoins.Length - 1)];
                m.DailyGems = m.DailyDay == 7 ? _tuning.dailyGemsOnLastDay : 0;
                m.RewardsAvailable = _meta.AnythingToClaim();
                var views = new MissionView[_profile.missions.Count];
                for (int i = 0; i < views.Length; i++)
                {
                    var ms = _profile.missions[i];
                    views[i] = new MissionView { text = MetaProgress.Describe(ms), progress = ms.progress, target = ms.target, claimed = ms.claimed, rewardCoins = ms.rewardCoins, rewardGems = ms.rewardGems };
                }
                m.Missions = views;
                int xp = _meta.Xp(string.IsNullOrEmpty(_selVehicleId) ? "vehicle-start" : _selVehicleId);
                m.MasteryXp = xp; m.MasteryLevel = MetaProgress.LevelFromXp(xp);
            }
            m.UnlockedVehicleIds.Clear();
            foreach (var id in _unlockedVehicleIds) m.UnlockedVehicleIds.Add(id);
            m.RaiseChanged();
        }

        private void SyncProgressionModel(GameModel m)
        {
            if (_profile == null) return;
            m.LockText.Clear();
            var cat = AssetCatalog.Load();
            if (cat != null)
                foreach (var e in cat.entries)
                {
                    if (e == null) continue;
                    string t = "";
                    if (e.kind == CatalogKind.World) t = GateText.World(_profile, _worldGates, e.id);
                    else if (e.kind == CatalogKind.Vehicle && !_unlockedVehicleIds.Contains(e.id)) t = GateText.Vehicle(_profile, _vehicleGates, e.id);
                    else if (e.kind == CatalogKind.Surface) t = GateText.Surface(_profile, _surfaceGates, e.id);
                    if (e.kind == CatalogKind.Vehicle) m.VehicleLines[e.id] = VehicleEconomy.ProfileLine(_vehicleProfiles, _vehicleGates, e.id);
                    if (t.Length > 0) m.LockText[e.id] = t;
                }
            if (_hub != null) { _hub.SelectedVehicle = _selVehicleId; m.HubWaiting = _hub.Waiting(); }
            var now = System.DateTime.UtcNow;
            var d = DailyChallenge.Build(now, _prog, _sliceLadder != null ? _sliceLadder.matrix : null);
            string key = DailyChallenge.DateKey(now);
            bool today = _profile.dailyDate == key;
            m.DailyChallengeOn = !string.IsNullOrEmpty(d.vehicleId) && !string.IsNullOrEmpty(d.worldId);
            m.DailyChallengeBest = today ? _profile.dailyBest : 0;
            m.DailyChallengeClaimed = today && _profile.dailyClaimed;
            m.DailyChallengeClaimable = DailyChallenge.CanClaim(_profile, now);
            m.DailyChallengeCoins = d.rewardCoins;
            m.DailyChallengeInfo = m.DailyChallengeClaimed ? "DONE  BEST " + m.DailyChallengeBest + " m" : (m.DailyChallengeClaimable ? "CLAIM +" + d.rewardCoins : (m.DailyChallengeBest > 0 ? "BEST " + m.DailyChallengeBest + " m" : "+" + d.rewardCoins + " COINS"));
            bool over = _state == ScreenState.GameOver;
            m.OfferDouble = over && !_runDoubled && m.LastRun.coinsWon > 0 && _offers.CanOffer(AdPlacement.DoubleCoins);
            m.OfferRevive = over && _canRevive && _offers.CanOffer(AdPlacement.Revive);
            m.OfferExtraDaily = over && _profile.dailyClaimed && today && _offers.CanOffer(AdPlacement.ExtraDailyReward);
            m.OfferDoubleLeft = _offers.Remaining(AdPlacement.DoubleCoins);
            m.OfferExtraDailyLeft = _offers.Remaining(AdPlacement.ExtraDailyReward);
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
            if (_run != null)
            {
                m.Boost01 = _run.Boost01;
                m.BoostCost = _run.NextBoostCost;
                m.BoostReady = _run.BoostReady(totalCoins + sessionCoins);
                m.Combo = _run.Combo; m.Multiplier = _run.Multiplier;
                m.SessionGems = _run.Stats.gems;
                int key = (_run.ShieldUp ? 1 : 0) + Mathf.CeilToInt(_run.MagnetLeft) * 2 + Mathf.CeilToInt(_run.DoubleLeft) * 128 + Mathf.CeilToInt(_run.SlowLeft) * 8192 + Mathf.CeilToInt(_run.GhostLeft) * 524288;
                if (key != _powerKey) { _powerKey = key; _powerText = _run.PowerText(); }
                m.PowerText = _powerText;
            }
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
            if (_dailyActive && !_reviving) EndDaily();
            // Never start a run in a car the player has not unlocked.
            if (!IsCurrentVehicleUnlocked()) SelectedVehicle = VehicleType.ClassicRed;
            if (CatalogVehicle() == null) _selVehicleId = "";
            SwitchScreen(ScreenState.Playing);
        }

        /// <summary>Today's daily challenge: fixed seed (the ladder of BeginStageRun), vehicle and world. The player's own picks are restored when the run flow leaves it.</summary>
        public void StartDaily()
        {
            if (_state == ScreenState.Playing) return;
            var cat = AssetCatalog.Load();
            _daily = DailyChallenge.Build(System.DateTime.UtcNow, _prog, _sliceLadder != null ? _sliceLadder.matrix : null);
            _dailySavedVehicle = _selVehicleId; _dailySavedWorld = _selWorldId;
            var v = cat?.Find(_daily.vehicleId); if (v != null && v.kind == CatalogKind.Vehicle && v.playable) _selVehicleId = v.id;
            var w = cat?.Find(_daily.worldId); if (w != null && w.kind == CatalogKind.World && w.playable) _selWorldId = w.id;
            _dailyActive = true;
            ForcedEvents.Current = new FixedForcedEventList(_daily.eventIds);   // section A's ladder reads this for the three daily events
            SwitchScreen(ScreenState.Playing);
        }

        private void EndDaily()
        {
            if (!_dailyActive) return;
            _dailyActive = false;
            ForcedEvents.Current = null;
            _selVehicleId = _dailySavedVehicle; _selWorldId = _dailySavedWorld;
            ApplyWorld(); ApplySurface();
        }

        public void ClaimDailyChallenge()
        {
            SavePlayerData();
            if (DailyChallenge.Claim(_profile, System.DateTime.UtcNow, _prog, out _))
            {
                totalCoins = _profile.coins;
                SavePlayerData();
                if (SoundManager.Instance) SoundManager.Instance.PlayUpgrade();
            }
            SyncModel();
        }

        /// <summary>Optional rewarded offer from the result screen. The ad service decides if an ad exists; limits live in AdOffers; never while a run is active.</summary>
        public void WatchOffer(AdPlacement placement)
        {
            if (_state != ScreenState.GameOver || _offers == null) return;
            if (placement == AdPlacement.DoubleCoins && (_runDoubled || Model.LastRun.coinsWon <= 0)) return;
            if (placement == AdPlacement.Revive && !_canRevive) return;
            if (placement == AdPlacement.ExtraDailyReward && !_profile.dailyClaimed) return;
            _offers.TryOffer(placement, ok =>
            {
                if (!ok) return;
                if (placement == AdPlacement.DoubleCoins)
                {
                    _runDoubled = true; totalCoins += Model.LastRun.coinsWon;
                    var r = Model.LastRun; r.coinsWon *= 2; r.totalCoins = totalCoins; Model.LastRun = r;
                    SavePlayerData();
                }
                else if (placement == AdPlacement.ExtraDailyReward)
                {
                    totalCoins += DailyChallenge.Build(System.DateTime.UtcNow, _prog).rewardCoins;
                    var r = Model.LastRun; r.totalCoins = totalCoins; Model.LastRun = r;
                    SavePlayerData();
                }
                else { _canRevive = false; _reviving = true; SwitchScreen(ScreenState.Playing); return; }
                SyncModel();
            });
        }

        // ---- Progression hub (mastery, journey, daily, weekly, album, shop, unlocks, workshop): rows come from ProgressionController, actions change the profile.
        public HubView BuildHub(HubPage page)
        {
            if (_hub == null) return new HubView { page = page };
            _profile.coins = totalCoins; _hub.P = _profile; _hub.SelectedVehicle = _selVehicleId; _hub.Matrix = _stage != null && _stage.Ladder != null ? _stage.Ladder.matrix : null;
            return _hub.Build(page);
        }

        public string HubAction(string action)
        {
            if (_hub == null || _state == ScreenState.Playing) return "";
            SavePlayerData();                                   // pushes the manager's wallet and unlocked vehicles into the profile
            _hub.P = _profile; _hub.SelectedVehicle = _selVehicleId;
            string msg = _hub.Act(action, out var effect);
            totalCoins = _profile.coins;
            _unlockedVehicleIds.Clear();
            if (_profile.unlockedVehicleIds != null) foreach (var id in _profile.unlockedVehicleIds) _unlockedVehicleIds.Add(id);
            if (sportUnlocked) _unlockedVehicleIds.Add("vehicle-sports-car");
            if (truckUnlocked) _unlockedVehicleIds.Add("vehicle-monster-truck");
            if (msg.Length > 0 && SoundManager.Instance) SoundManager.Instance.PlayUpgrade();
            SavePlayerData();
            SyncModel();
            if (effect == HubEffect.StartDaily) StartDaily();
            return msg;
        }

        public void GoHome() { SwitchScreen(ScreenState.HomeDashboard); }
        public void OpenGarage() { SwitchScreen(ScreenState.GarageMenu); }
        public void OpenWorlds() { SwitchScreen(ScreenState.StageSelect); }

        public void Boost()
        {
            if (_car == null || _state != ScreenState.Playing || _paused) return;
            var result = _run.TryBoost(totalCoins + sessionCoins, out int paid);
            if (result == RunDirector.BoostResult.None) return;
            if (result == RunDirector.BoostResult.Refused)
            {
                OnRunBonus("NEED " + _tuning.boostCoinCost + " COINS", 0);
                return;
            }
            if (paid > 0)
            {
                int fromRun = Mathf.Min(sessionCoins, paid);      // spend this run's coins first
                sessionCoins -= fromRun;
                totalCoins -= paid - fromRun;
            }
            _car.BoostKick(_car.TargetSpeed + _tuning.boostSpeedBonus);
            if (SoundManager.Instance) SoundManager.Instance.PlayUpgrade();
            if (ParticleManager.Instance)
                ParticleManager.Instance.SpawnBurst(_car.Body.position, new Color(0.2f, 0.95f, 1f), 14);
            OnRunBonus(result == RunDirector.BoostResult.Free ? "FREE BOOST" : "BOOST", 0);
            SyncHud();
        }

        public void OpenRewards() { SwitchScreen(ScreenState.Rewards); }

        public void ClaimDaily()
        {
            SavePlayerData();
            if (_meta.ClaimDaily(out int coins, out int gems))
            {
                totalCoins = _profile.coins;
                SavePlayerData();
                if (SoundManager.Instance) SoundManager.Instance.PlayUpgrade();
                SyncModel();
            }
        }

        public void ClaimMission(int index)
        {
            SavePlayerData();
            if (_meta.ClaimMission(index, out int coins, out int gems))
            {
                ProgressionEvents.Record(_profile, ProgressionEvent.MissionCompleted);
                totalCoins = _profile.coins;
                SavePlayerData();
                if (SoundManager.Instance) SoundManager.Instance.PlayUpgrade();
                SyncModel();
            }
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
            if (statIndex < 0 || statIndex > 4) return;
            int currentLevel = (statIndex == 0) ? engineLevel : ((statIndex == 1) ? suspensionLevel : ((statIndex == 2) ? inkTankLevel : (statIndex == 3 ? boostLevel : _shieldLevel)));
            if (currentLevel >= GameCatalog.MaxUpgradeLevel) return;

            int cost = GameCatalog.UpgradeCost(currentLevel, _upgradeKey);
            if (totalCoins >= cost)
            {
                totalCoins -= cost;
                if (statIndex == 0) engineLevel++;
                else if (statIndex == 1) suspensionLevel++;
                else if (statIndex == 2) inkTankLevel++;
                else if (statIndex == 3) boostLevel++;
                else _shieldLevel++;

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
            if (entry != null && !entry.playable)
            {
                if (SoundManager.Instance) SoundManager.Instance.PlayCrash();
                return;
            }

            if (kind == CatalogKind.World && entry != null && GateText.WorldLocked(_profile, _worldGates, id))
            {
                _profile.coins = totalCoins;
                if (!GateService.TryUnlockWorld(_profile, _worldGates, id)) { if (SoundManager.Instance) SoundManager.Instance.PlayCrash(); return; }
                totalCoins = _profile.coins;
            }
            if (kind == CatalogKind.Surface && entry != null && GateText.SurfaceLocked(_profile, _surfaceGates, id))
            {
                _profile.coins = totalCoins;
                if (!GateService.TryUnlockSurface(_profile, _surfaceGates, id)) { if (SoundManager.Instance) SoundManager.Instance.PlayCrash(); return; }
                totalCoins = _profile.coins;
            }
            if (kind == CatalogKind.Vehicle)
            {
                if (entry != null && !_unlockedVehicleIds.Contains(id) && GateText.Vehicle(_profile, _vehicleGates, id).Length > 0)
                {
                    if (SoundManager.Instance) SoundManager.Instance.PlayCrash();      // distance gate not reached yet (the card says how far)
                    return;
                }
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
            else
            {
                _selWorldId = id;
                ApplyWorld();
                ApplySurface();   // the road follows the world's art style
                _audio?.OnWorldChosen(id);
            }
            if (SoundManager.Instance) SoundManager.Instance.PlayUpgrade();
            SavePlayerData();
            SyncModel();
        }

        // ---------------------------------------------------------------- catalogue vehicles and surfaces
        private CatalogEntry CatalogVehicle()
        {
            var e = AssetCatalog.Load()?.Find(_selVehicleId);
            if (e == null || e.kind != CatalogKind.Vehicle || !e.playable || e.preview == null) return null;
            if (e.cost > 0 && !_unlockedVehicleIds.Contains(e.id) && !(_dailyActive && e.id == _selVehicleId)) return null;
            return e;
        }

        private RoadCar CreateSelectedCar(Vector2 pos)
        {
            var e = CatalogVehicle();
            if (e == null) return RoadCar.Create(pos, SelectedVehicle, _worldArt);   // slice vehicles follow the world's art style; catalogue vehicles keep their own art
            var made = RoadCar.Create(pos, e.MakeRig(_worldArt));
            CosmeticApplier.Apply(made.gameObject, _profile, _prog, e.id);
            return made;   // Smooth worlds: smooth body and wheels for vehicles whose smooth art is ready; same size, mass, torque
        }

        /// <summary>The scenery for the current world pick: a remix of its mood, or one scene shown as drawn.</summary>
        private void ApplyWorld()
        {
            var e = AssetCatalog.Load()?.Find(_selWorldId);
            if (e != null && e.kind == CatalogKind.World && e.playable)
            {
                if (e.themeIndex >= 0)
                {
                    SelectedTheme = (EnvironmentTheme)e.themeIndex;
                    _worldArt = ThemeArtStyles.For(SelectedTheme);
                    _background.ApplyTheme(SelectedTheme);
                    return;
                }
                if (e.layers != null && e.layers.Length == BackgroundSkin.LayerCount)
                {
                    if (!_worldSkins.TryGetValue(e.id, out var skin) || skin == null)
                    {
                        skin = ScriptableObject.CreateInstance<BackgroundSkin>();
                        skin.displayName = e.displayName;
                        skin.sky = e.layers[0]; skin.far = e.layers[1]; skin.mid = e.layers[2]; skin.near = e.layers[3];
                        _worldSkins[e.id] = skin;
                    }
                    _worldArt = skin.artStyle;
                    _background.ApplySkin(skin);
                    return;
                }
            }
            _worldArt = ThemeArtStyles.For(SelectedTheme);
            _background.ApplyTheme(SelectedTheme);
        }

        /// <summary>Road skin and grip for the current surface pick (or the legacy road style). Looks and friction only.</summary>
        private void ApplySurface()
        {
            var e = string.IsNullOrEmpty(_selSurfaceId) ? null : AssetCatalog.Load()?.Find(_selSurfaceId);
            if (e != null && e.kind == CatalogKind.Surface && e.playable)
            {
                _roadMaterial.friction = e.friction; _surfBaseFriction = e.friction;
                _roadMaterial.bounciness = e.bounce;
                SetSurfaceTuning(e);   // gameplay numbers always come from the selected surface, whatever strip is drawn
                if (ApplyEntrySkin(e)) return;
            }
            _roadMaterial.friction = 1.25f; _surfBaseFriction = 1.25f;
            _roadMaterial.bounciness = 0f;
            _trackSpeed = 1f; _trackInk = 1f; _run.CoinFactor = 1f;
            var legacySmooth = SmoothDisplaySkin(RoadSkins.SurfaceIdOf(SelectedRoadStyle));
            if (legacySmooth != null) { _road.SetSkin(legacySmooth); return; }
            _road.SetRoadStyle(SelectedRoadStyle);
        }

        /// <summary>Draws the road with the look of a catalogue surface. False when the entry has no look of its own (the caller falls back to the legacy style).</summary>
        private bool ApplyEntrySkin(CatalogEntry e)
        {
            var smoothSkin = SmoothDisplaySkin(e.id);
            if (smoothSkin != null) { _road.SetSkin(smoothSkin); return true; }
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
                return true;
            }
            if (e.roadStyle >= 0) { SelectedRoadStyle = (RoadStyle)e.roadStyle; _road.SetRoadStyle(SelectedRoadStyle); return true; }
            return false;
        }

        private readonly HashSet<string> _smoothFallbackLogged = new HashSet<string>();

        /// <summary>
        /// Display skin for a surface in a Smooth world: its own smooth strip, else the world's default smooth road (display only;
        /// the selected surface id, friction, bounce and tuning are untouched). Null in a pixel world or when no default exists.
        /// The fallback is logged once per surface.
        /// </summary>
        private RoadSkin SmoothDisplaySkin(string surfaceId)
        {
            if (_worldArt != ArtStyle.Smooth) return null;
            var own = RoadSkins.GetSmooth(surfaceId);
            if (own != null) return own;
            string defId = ThemeArtStyles.DefaultSmoothRoad(SelectedTheme);
            var fallback = RoadSkins.GetSmooth(defId);
            if (fallback != null && _smoothFallbackLogged.Add(string.IsNullOrEmpty(surfaceId) ? "(legacy)" : surfaceId))
                Debug.Log("[Road] No smooth strip for '" + surfaceId + "' in a Smooth world: drawing the default '" + defId + "' (display only, gameplay numbers unchanged).");
            return fallback;
        }

        private void SetSurfaceTuning(CatalogEntry e)
        {
            _trackSpeed = e.speedFactor; _trackInk = e.inkFactor; _run.CoinFactor = e.coinFactor;
        }

        // ==================================================================== STAGE EVENTS (Docs/design/stage-events.md; behind GameplayTuning.stageEvents)
        private void SetupStageEvents()
        {
            var ladder = Resources.Load<StageLadderDefinition>("Stage/StageLadder");
            _sliceLadder = ladder;
            _endlessLadder = Resources.Load<StageLadderDefinition>("Stage/EndlessLadder");
            _stage = new StageRunController(ladder);
            _stage.Register("EVT-ICE", new SurfaceZoneBehaviour(EnterZone, ExitZone));
            _stage.Register("EVT-MUD", new SurfaceZoneBehaviour(EnterZone, ExitZone));
            _stage.Register("EVT-GLASS", new SurfaceZoneBehaviour(EnterZone, ExitZone));
            _stage.Register("EVT-METEOR", new MeteorWindowBehaviour(_meteors));
            _stage.Register("EVT-RINGS", new RingWindowBehaviour(PlaceRingGauntlet));
            // Mutation events: one registered behaviour per event id, built from the event data (effects, rule, counterplay).
            var hooks = new StageEffectHooks
            {
                PlaceCoinGate = (dist, h) => _pickups?.PlaceCoinGate(_startX + dist, h),
                ChargeCoins = n => { int take = Mathf.Clamp(n, 0, sessionCoins); sessionCoins -= take; return take; },
                Banner = ShowStageBanner
            };
            if (_endlessLadder != null) StageEffectRegistry.RegisterAll(_stage, _fx, _endlessLadder.eventPool, hooks);
            _stagePresenter = new StageWarningPresenter(_stage, _audio, _tuning);
            _meteors.CanStrike = () => _stage.CanSpawnHazard("hz-meteor");
            SetupHazards();
        }

        private void SetupHazards()
        {
            _eaterData = Resources.Load<MonsterDefinitionSO>("Stage/Hazards/MonsterDef-RoadEater");
            if (_eaterData == null) _eaterData = ScriptableObject.CreateInstance<MonsterDefinitionSO>();
            _bossData = Resources.Load<BossDefinitionSO>("Stage/Hazards/BossDef-RoB0");
            if (_bossData == null || _bossData.bolt == null || _bossData.pool1 == null || _bossData.pool1.Length == 0) _bossData = BossDefinitionSO.CreateDefault();
            _nearLedger.Steps = _tuning.nearMissCoinsStep != null && _tuning.nearMissCoinsStep.Length > 0 ? _tuning.nearMissCoinsStep : _nearLedger.Steps;
            _nearLedger.PerfectMultiplier = _tuning.nearMissPerfectMultiplier; _nearLedger.ChainWindowS = _tuning.nearMissChainWindow;
            _nearLedger.RunCapFraction = _tuning.nearMissRunCapFraction; _nearLedger.EaterPay = _tuning.nearMissEaterPay;

            _announcer = new Announcer
            {
                PlaySfx = id => _audio?.StageCue(id),
                Subtitle = (t, secs, urgent) => { var mm = Model; mm.Subtitle = t; mm.SubtitleSeconds = secs; mm.SubtitleId++; },
                ThreatActive = () => (_eater != null && _eater.Model.Chasing && _eater.Model.Band != EaterBand.Calm) || (_boss != null && (_boss.Model.State == BossState.Pattern || _boss.World.AliveCount > 0))
            };
            _eater = new RoadEaterController(_road, _eaterData, _cam)
            {
                Cue = id => _announcer.Say(id),
                Banner = ShowStageBanner,
                Shake = a => _cameraRig?.Shake(a, 0.4f),
                Bite = () => HandleLifeLoss("EATEN BY THE ROAD-EATER!"),
                Cleared = OnEaterCleared,
                NearMissed = OnEaterNearMiss,
                Ink01 = () => maxInk > 0f ? currentInk / maxInk : 1f,
                RescueInk = () => { if (_pickups != null && _car != null) _pickups.PlaceInkAt(_car.Body.position.x + 7f + Mathf.Max(0f, _car.Body.linearVelocity.x)); }
            };
            _eater.Host.Damage = r => HandleLifeLoss(r); _eater.Host.CanDamage = () => _hitGraceLeft <= 0f && !_stage.HazardGraceActive;
            _eater.Host.CarForce = f => { if (_car != null) _car.Body.AddForce(f); };
            _eater.Host.Shake = a => _cameraRig?.Shake(a, 0.4f); _eater.Host.Cue = id => _announcer.Say(id); _eater.Host.Banner = ShowStageBanner;
            _boss = new BossController(_road, _bossData)
            {
                Cue = id => _announcer.Say(id),
                Banner = ShowStageBanner,
                Shake = a => _cameraRig?.Shake(a, 0.4f),
                CanDamage = () => _hitGraceLeft <= 0f && !_stage.HazardGraceActive,
                Hit = r => HandleLifeLoss(r),
                CounterAvailable = () => _run.Boost01 >= 1f || sessionCoins + totalCoins >= _bossData.counterCoins || (maxInk > 0f && currentInk / maxInk >= _bossData.counterInk01),
                InkAvailable = () => maxInk > 0f && currentInk / maxInk >= _bossData.counterInk01,
                NearMissed = (src, perfect, travel) => ResolveNearMiss(src, perfect, travel),
                RingTaken = () => _run.CollectPower(Collectible.Type.Ring),
                Decided = OnBossDecided,
                Overpowered = () => { _announcer.Say(StageCues.BossRetreat); }
            };
            _boss.Model.Raised += e => { if (e == BossEvent.Gone) _bossEndS = _distance; };

            // Behaviour modules registered by event id on the existing run controller (no switch): the director only knows ids and distances.
            _stage.Register("MON-EATER", new DelegateBehaviour(
                s => { ProgressionEvents.Record(_profile, ProgressionEvent.MonsterReached); _eater.StartChase(_car.Body.position.x); },
                s => { if (!_runEnding && _eater.Model.Chasing) _eater.Model.Retreat(true); }));      // the window ended at 1750 m: timeout retreat
            _stage.Register("BOS-ROB0", new DelegateBehaviour(
                s => { ProgressionEvents.Record(_profile, ProgressionEvent.BossReached); _boss.Start(_car.Body.position, _distance, _hazardWorldId); },
                s => { },                                                                              // the fight outlives its planned window: it ends by Overload or the 105 s timeout
                (s, d) => _boss.OnRespawn()));
            _stage.Signal += (e, sig) => { if (sig == StageSignal.Announcer && e.Definition.role == StageEventRole.Monster) _eater.Warn(); };
            _stage.EventEnded += e => { if (!_runEnding) ProgressionEvents.RecordStageEvent(_profile, _prog, e.Definition != null ? e.Definition.id : ""); };
            _stage.EventStarted += OnMutationStarted;
            _stage.EventStarted += e => { if (e.Definition.noEat) _road.AddNoEatRange(_startX + e.StartM, _startX + e.EndM); };
        }

        private void ShowStageBanner(string text)
        {
            var m = Model; m.StageBanner = text; m.StageBannerTint = ""; m.StageBannerId++;
        }

        // ------------------------------------------------------------------ hazards per frame (Road-Eater, RO-B0, near-miss economy, finish gate)
        private void TickHazards(Vector2 carPos, float dt)
        {
            if (_hitGraceLeft > 0f) _hitGraceLeft -= dt;
            _nearLedger.Tick(dt);
            Vector2 vel = _car.Body.linearVelocity;
            _recentSpeed += (vel.x - _recentSpeed) * Mathf.Min(1f, dt);                      // about a 1 s average
            _hitStop.Tick(Time.unscaledDeltaTime); _announcer.Tick(Time.unscaledDeltaTime);
            _eater.Host.CarVel = vel; _eater.Host.CarMass = _car.Body.mass;
            _eater.Tick(dt, carPos.x, carPos.y, vel.x, !_car.IsGrounded, _run.Boosting);
            if (!_stage.Active) return;                                                       // the bite or a hit may have ended the run
            _boss.Tick(dt, carPos, vel);
            var m = Model; var em = _eater.Model; var bm = _boss.Model;
            m.EaterOn = em.Chasing; m.EaterGapM = em.Gap; m.EaterBand = (int)em.Band; m.EaterEscape01 = em.EscapeProgress01;
            m.BossOn = bm.Fighting; m.BossOverload01 = bm.Meter01; m.BossPhase = bm.Phase;
            m.BossTier = _boss.Tier != null ? _boss.Tier.displayName : ""; m.BossHits = bm.Hits; m.BossMaxHits = _boss.Tier != null ? _boss.Tier.maxHits : 3;
            m.EaterVariant = _eaterVariant_Name(); m.EaterPhase = _eater.PursuitPhase;
            if (_gateProp != null) { float gx = FinishGateM(); if (gx > 0f) _gateProp.Place(_road, _startX + gx, carPos.x, 60f); else _gateProp.Hide(); }
            float gate = FinishGateM();
            if (!_finishing && gate > 0f && _distance >= gate && !bm.Fighting && bm.State != BossState.Intro && bm.State != BossState.Defeat)
            {
                if (KeepGoingAvailable()) { KeepGoing(); return; }
                _finishing = true;
                _audio?.StageCue(StageCues.Finish);
                TriggerGameOver("COURSE COMPLETE!", true);
            }
        }

        /// <summary>Finish gate: max(2750, boss end + 150) (brief X7); before the boss has ended it is the planned finish.</summary>
        private float FinishGateM()
        {
            if (_stage.OnEndless) return 0f;                       // the endless ladder has no finish gate
            float planned = _stage.Director.FinishM;
            return _bossEndS >= 0f ? Mathf.Max(planned, _bossEndS + _stage.Ladder.finishAfterBossM) : planned;
        }

        private void ResolveNearMiss(int source, bool perfect, float travelM)
        {
            int non = _run.Stats.coins + _run.Stats.bonusCoins - _nearLedger.Paid;
            var r = _nearLedger.Resolve(source, perfect, travelM, _recentSpeed, non);
            if (r.Valid) { _run.NearMissPaid(r.Coins, (r.Perfect ? "PERFECT CLOSE!" : "CLOSE!") + (r.Chain > 1 ? " x" + r.Chain : "")); if (r.Perfect) _hitStop.Request(0.07f); _announcer.Say("vo.rob0.nearmiss"); }
        }

        private string _eaterVariant_Name() { var v = _eater.Variant; return v != null ? v.displayName : ""; }

        private void OnMeteorNearMiss(float d)
        {
            if (_stageOn) ResolveNearMiss(0, d <= _tuning.meteorBlastRadius + 1.5f, _distance - _meteorWarnDist);
        }

        private void OnEaterNearMiss()
        {
            int non = _run.Stats.coins + _run.Stats.bonusCoins - _nearLedger.Paid;
            var r = _nearLedger.ResolveEater(non);
            if (r.Valid) _run.NearMissPaid(r.Coins, "CLOSE CALL!");
        }

        private void OnEaterCleared(bool timeout)
        {
            var dv = timeout ? null : _eater.DefeatedVariant;
            var pay = dv != null ? HazardRewards.EaterDefeat(dv, _profile) : HazardRewards.Eater(_eaterData, _profile, timeout);
            if (dv != null) HazardRewards.RecordEaterDefeat(_profile, dv); else HazardRewards.RecordEater(_profile, timeout);
            if (pay.TrophyId != null) ShowStageBanner("TROPHY!");
            SavePlayerData();
            ShowStageBanner(StageCues.BannerText(timeout ? "banner.eater.retreat" : "banner.eater.escape"));
            _run.AwardEvent(pay.Label, pay.Coins, pay.Gems, pay.Frenzy);
        }

        private void OnBossDecided(bool timeout)
        {
            var tier = _boss.Tier;
            var pay = !timeout && tier != null ? HazardRewards.BossTier(tier, _profile) : HazardRewards.Boss(_bossData, _profile, timeout);
            if (!timeout && tier != null) HazardRewards.RecordBossTier(_profile, tier); else HazardRewards.RecordBoss(_profile, timeout);
            if (!timeout) { RefreshUpgradeLevels(); ProgressionEvents.Record(_profile, ProgressionEvent.BossDefeated); }
            if (pay.CosmeticId != null) ShowStageBanner("NEW ROBOT COSMETIC!");
            SavePlayerData();
            _run.AwardEvent(pay.Label, pay.Coins, pay.Gems, pay.Frenzy);
        }

        /// <summary>Countryside means the Country theme, and not a custom layered world from the catalogue.</summary>
        private bool IsCountrysideWorld()
        {
            var e = string.IsNullOrEmpty(_selWorldId) ? null : AssetCatalog.Load()?.Find(_selWorldId);
            if (e != null && e.kind == CatalogKind.World && e.playable && e.themeIndex < 0) return false;
            return SelectedTheme == EnvironmentTheme.Country;
        }

        private void BeginStageRun(bool retry)
        {
            _zone.Exit();
            BeginSurfaceRun();
            _stagePresenter.Reset();
            _fx.Reset(); ApplyStageEffects(); _sliceCleared = false;
            bool country = IsCountrysideWorld();
            // The 7-slot slice ladder runs in Countryside; every other world (stageEvents = Always) runs the endless ladder from the start.
            var ladder = country ? (_sliceLadder != null ? _sliceLadder : _endlessLadder) : _endlessLadder;
            if (ladder != null) _stage.UseLadder(ladder);
            _stageOn = ladder != null && StageRunController.ShouldRun(_tuning.stageEvents, country) && _stage.HasData;
            _meteors.StageDriven = _stageOn;
            _eater.Stop(); _boss.Stop(); _hitStop.Release(); _announcer?.Clear(); _gateProp.Hide();
            _road.ClearNoEatRanges();
            _nearLedger.Reset(); _hitGraceLeft = 0f; _recentSpeed = 0f; _bossEndS = -1f; _finishing = false; _runEnding = false;
            Model.EaterOn = false; Model.BossOn = false;
            if (!_stageOn) return;
            bool endless = _stage.OnEndless;
            int modeId = endless ? StageRunController.ModeIdEndless : StageRunController.ModeIdCountryside;
            // Retry reuses the saved seed (same ladder); a new run draws a new one. The record is saved through SaveService only.
            var saved = _profile.lastStageRun;
            StageRunRecord rec;
            if (_dailyActive) { rec = StageRunController.NewRecord(_daily.seed, ladder.layoutVersion, modeId); _profile.lastStageRun = rec; }
            else if (retry && saved.runSeed != 0u && saved.layoutVersion == ladder.layoutVersion) { rec = saved; rec.debugJumped = false; rec.modeId = modeId; }
            else
            {
                rec = StageRunController.NewRecord((ulong)System.DateTime.UtcNow.Ticks, ladder.layoutVersion, modeId);
                _profile.lastStageRun = rec;
                SavePlayerData();
            }
            if (endless) { _worldCtx = BuildWorldContext(ladder); _stage.BeginEndless(rec, _worldCtx); }
            else { _worldCtx = default; _stage.Begin(rec); }
            _hazardWorldId = HazardWorlds.Resolve(_selWorldId, IsCountrysideWorld()); _announcer.Reset(); _hitStop.Release();
            _eater.Configure(EaterVariantSO.ForWorld(_hazardWorldId));
            _eater.Begin(_startX, rec.runSeed ^ 0x00E47E8u);
            _boss.Begin(rec.runSeed ^ 0x00B055u);
        }

        /// <summary>World tags (suitable-worlds filter) and, with the opt-in on, the stages the player keeps failing (adaptive easing).</summary>
        private StageWorldContext BuildWorldContext(StageLadderDefinition ladder)
        {
            var ctx = new StageWorldContext();
            string[] tags = ladder.TagsForWorld(_selWorldId);
            if (tags == null) tags = SelectedTheme == EnvironmentTheme.Desert ? ladder.themeTagsDesert : (SelectedTheme == EnvironmentTheme.CyberNight ? ladder.themeTagsCyber : ladder.themeTagsCountry);
            ctx.Tags = tags;
            if (_profile != null && _profile.adaptiveChallenge)
            {
                var eased = new List<int>();
                for (int k = 1; k <= ladder.endlessStageCount; k++) if (_profile.StageFails(k) >= ladder.adaptiveFailsToEase) eased.Add(k);
                ctx.EasedStages = eased.ToArray();
            }
            return ctx;
        }

        // ------------------------------------------------------------------ mutation events (gap list A1)

        /// <summary>Pushes the folded effect values into the car, the ink tank and the pickups. Cheap compares only; runs every frame while playing and on every reset.</summary>
        private void ApplyStageEffects()
        {
            float cap = _baseMaxInk * _fx.InkCapMul;
            if (_baseMaxInk > 0f && !Mathf.Approximately(maxInk, cap))
            {
                maxInk = cap;
                if (currentInk > maxInk) currentInk = maxInk;
                UpdateInkUI();
            }
            if (_car != null)
            {
                _car.SetGravityMul(_fx.GravityMul);
                if (!Mathf.Approximately(_car.Scale, _fx.CarScale))
                {
                    _car.SetScale(_fx.CarScale);
                    if (ParticleManager.Instance && _car.Body != null) ParticleManager.Instance.SpawnBurst(_car.Body.position, new Color(0.65f, 0.85f, 0.3f), 14);
                }
                _car.WindAccel = new Vector2(_fx.WindAccelX, 0f);
            }
            Collectible.RepelTarget = _fx.CoinVacuum && _car != null && _car.Body != null ? _car.Body.transform : null;
            if (_fx.CoinVacuum) { Collectible.RepelRadius = _fx.VacuumRadius; Collectible.RepelSpeed = _fx.VacuumSpeed; }
        }

        /// <summary>Fragile Vehicle: a collision harder than the threshold costs a heart. Returns true when that ended the run.</summary>
        private bool CheckFragileImpact()
        {
            float imp = _car.ConsumeImpact();
            if (_fx.FragileThreshold01 <= 0f || imp < _fx.FragileThreshold01 * _fx.FragileRefMps) return false;
            if (_car.IsInvincible || _hitGraceLeft > 0f || _stage.HazardGraceActive) return false;
            HandleLifeLoss("VEHICLE CRACKED!");
            return _state != ScreenState.Playing;
        }

        /// <summary>The 3-step learning rule for a mutation (counted through SaveService) and the adaptive indicator.</summary>
        private void OnMutationStarted(StageEventState e)
        {
            var d = e.Definition;
            bool eased = _stage.OnEndless && _profile.adaptiveChallenge && e.SlotIndex % 10 == 0 && _worldCtx.IsEased(e.SlotIndex / 10);
            string text = string.Empty;
            if (d.effects != null && d.effects.Length > 0)
            {
                int before = _profile.MarkEventSeen(d.id);
                SavePlayerData();
                text = StageCues.Preview(d, before);
            }
            if (eased) text = string.IsNullOrEmpty(text) ? "EASIER STAGE (ADAPTIVE)" : text + " (EASIER)";
            if (!string.IsNullOrEmpty(text)) ShowStageBanner(text);
        }

        private bool KeepGoingAvailable() { return _stage.Active && !_stage.OnEndless && _endlessLadder != null && !_sliceCleared; }

        /// <summary>The slice finish gate no longer ends the run: the course counts as complete and the endless ladder takes over from the gate (same run, same seed).</summary>
        private void KeepGoing()
        {
            _sliceCleared = true;
            _audio?.StageCue(StageCues.Finish);
            ShowStageBanner("COURSE COMPLETE! KEEP GOING");
            _worldCtx = BuildWorldContext(_endlessLadder);
            _stage.ContinueEndless(_endlessLadder, _distance, _worldCtx);
            Debug.Log("[Stage] slice cleared at " + _distance.ToString("0") + " m, endless ladder continues (" + _stage.Director.Plan.Events.Count + " events planned)");
        }

        /// <summary>Adaptive Challenge: a death in an endless stage counts as a fail of that stage; passing a stage clears its fails. Only while the opt-in is on.</summary>
        private void RecordStageFail(bool finished, bool toMenu)
        {
            if (!_stageOn || finished || toMenu || !_profile.adaptiveChallenge || !_stage.OnEndless) return;
            int stage = Mathf.FloorToInt(_distance / Mathf.Max(1f, _stage.Ladder.stageLengthM));
            if (stage >= 1) _profile.AddStageFail(stage);
            for (int k = 1; k < stage; k++) _profile.ClearStageFail(k);
        }

        private void EndStageRun(StageRunOutcome outcome)
        {
            if (_stage == null) return;
            if (!_stage.Active) { _fx.Reset(); ApplyStageEffects(); return; }
            _runEnding = true;
            _stage.End(outcome);                 // Ended is raised for the Active zone or window, which clears them
            _runEnding = false;
            _fx.Reset(); ApplyStageEffects();    // nothing a mutation changed outlives the run (death, restart, quit)
            _eater.Stop(); _boss.Stop(); _hitStop.Release(); _announcer?.Clear(); _gateProp.Hide();
            Model.EaterOn = false; Model.BossOn = false;
            _stagePresenter.Reset();
            _zone.Exit();
        }

        private void EnterZone(SurfaceRuleSO rule, float startM)
        {
            _zone.Enter(rule);
            _roadMaterial.friction = rule.friction;
            _trackSpeed = rule.speedFactor; _trackInk = rule.inkFactor; _run.CoinFactor = rule.coinFactor;
            var e = AssetCatalog.Load()?.Find(rule.surfaceId);
            if (e != null && e.kind == CatalogKind.Surface) ApplyEntrySkin(e);       // the road drawn in the zone looks like the zone
            if (_pickups != null) _pickups.PlaceInkAt(_startX + startM + 6f);         // ink at zone entry (stage-events.md section 6)
            ShowSurfaceHint(rule);
        }

        /// <summary>Section C: the road of this run is the player's selected surface. Binds its behaviour set to the runtime and shows the 3-step hint.</summary>
        private void BeginSurfaceRun()
        {
            _surf.Paused = false;
            var e = string.IsNullOrEmpty(_selSurfaceId) ? null : AssetCatalog.Load()?.Find(_selSurfaceId);
            if (e == null || e.kind != CatalogKind.Surface || !e.playable || _car == null) { _surf.SetSurface("", null, null, _car != null ? _startX : 0f); return; }   // unmapped or no surface: the old friction and speed stay
            var set = SurfaceBehaviourLibrary.Find(e.id);
            _surfBaseFriction = e.friction;
            _surf.SetSurface(e.id, set, null, _startX);
            if (set == null) return;
            int before = _profile.MarkSurfaceSeen(e.id);
            SavePlayerData();
            string hint = SurfaceLearning.Hint(set, SurfaceLearning.Level(before));
            if (!string.IsNullOrEmpty(hint)) ShowStageBanner(hint);
        }

        /// <summary>Per frame: feeds the car to the surface runtime and applies what the modules ask for (speed, drive cut, bounce impulse, torque, buoyancy or magnet pull, grip, durability hits). Returns the target speed.</summary>
        private float TickSurfaceRuntime(Vector2 carPos, float baseTarget)
        {
            _surf.Paused = _zone.Active;
            _surf.SurfaceId = _zone.Active ? _zone.Rule.surfaceId : (_selSurfaceId ?? "");
            var s = new SurfaceCarSample { X = carPos.x, Speed = _car.Body.linearVelocity.x, Boosting = _run.Boosting, Airborne = !_car.IsGrounded, MudDragMul = _tuning.MudDragMul(MudVehicleIndex()) };
            s.Heavy = SurfaceVehicleClass.IsHeavy(_car.Body.mass); s.Metallic = SurfaceVehicleClass.IsMetallic(VehicleKey());
            if (_surf.Active) SampleRoadShape(carPos.x, 4f, out s.UphillDeg, out s.TurnDeg);
            float dt = Time.deltaTime;
            _surf.Tick(dt, in s, baseTarget);
            var o = _surf.Out;
            if (_zone.Active) return baseTarget;
            if (o.DriveCut != _surfCut) { _surfCut = o.DriveCut; _car.MotorOn = !o.DriveCut; }
            var body = _car.Body;
            if (o.Impulse > 0f) { var v = body.linearVelocity; v.y = Mathf.Max(v.y, 0f) + o.Impulse; body.linearVelocity = v; }
            if (o.VerticalAccel != 0f) { var v = body.linearVelocity; v.y += o.VerticalAccel * dt; body.linearVelocity = v; }
            if (o.Torque != 0f) body.angularVelocity += o.Torque * dt;
            float fr = _surfBaseFriction * o.GripMul;
            if (Mathf.Abs(_roadMaterial.friction - fr) > 0.02f) _roadMaterial.friction = fr;
            for (int i = 0; i < o.Damage; i++)
                if (_hitGraceLeft <= 0f && (_stage == null || !_stage.Active || !_stage.HazardGraceActive)) { HandleLifeLoss("DURABILITY LOST!"); break; }
            return o.TargetSpeed;
        }

        /// <summary>Section B calls this to let the surface under the Road-Eater slow, stun or speed it up.</summary>
        public IMonsterSurfaceQuery MonsterSurfaces { get { return _surf; } }

        /// <summary>The 3-step learning rule on the existing banner: first sighting full preview, second the short line, then nothing. Counted through SaveService.</summary>
        private void ShowSurfaceHint(SurfaceRuleSO rule)
        {
            if (rule == null) return;
            int before = _profile.MarkSurfaceSeen(rule.surfaceId);
            SavePlayerData();
            string hint = SurfaceLearning.Hint(rule.behaviours, SurfaceLearning.Level(before));
            if (!string.IsNullOrEmpty(hint)) ShowStageBanner(hint);
        }

        private void ExitZone()
        {
            _zone.Exit();
            if (_car != null) _car.MotorOn = true;
            ApplySurface();                                                            // the player's own surface returns
        }

        private void PlaceRingGauntlet(StageEventState s)
        {
            var d = s.Definition;
            _pickups.PlaceGauntlet(_startX + s.StartM, d.ringCount, d.ringSpacingM, d.ringMaxHeightM, s.TerrainSeed);
        }

        private int MudVehicleIndex() { return _selVehicleId.Length > 0 && CatalogVehicle() != null ? -1 : (int)SelectedVehicle; }

        /// <summary>Slope ahead of the car (uphill only) and the bend over <paramref name="spanM"/> metres of road. Reads the road lookup, allocates nothing.</summary>
        private void SampleRoadShape(float x, float spanM, out float uphillDeg, out float turnDeg)
        {
            uphillDeg = 0f; turnDeg = 0f;
            float h = Mathf.Max(0.5f, spanM * 0.5f);
            if (!_road.TryGetRoadYAtX(x - h, out float y0) || !_road.TryGetRoadYAtX(x, out float y1) || !_road.TryGetRoadYAtX(x + h, out float y2)) return;
            float a0 = Mathf.Atan2(y1 - y0, h) * Mathf.Rad2Deg, a1 = Mathf.Atan2(y2 - y1, h) * Mathf.Rad2Deg;
            uphillDeg = Mathf.Max(0f, a1);
            turnDeg = Mathf.Abs(a1 - a0);
        }

        // App in the background counts as a pause (brief F0): the run freezes and waits on the pause popup.
        private void OnApplicationPause(bool paused)
        {
            if (paused) PauseRun();
            _stage?.OnPause(paused);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public StageRunController DebugStage => _stage;
        // ---- QA probe accessors (gap list A1): they read and drive the real run, nothing is faked.
        public StageEffectState DebugFx => _fx;
        public bool DebugPlaying => _state == ScreenState.Playing && _car != null;
        public int DebugLives => currentLives;
        public int DebugSessionCoins { get => sessionCoins; set => sessionCoins = value; }
        public float DebugInk { get => currentInk; set => currentInk = value; }
        public float DebugMaxInk => maxInk;
        public float DebugBaseMaxInk => _baseMaxInk;
        public float DebugInkRegenPerSec => _tuning.inkRegenPerSec * _fx.InkRegenMul;
        public float DebugBoostDrainMul => _run.BoostDrainMul;
        public bool DebugBoosting => _run.Boosting;
        public float DebugBoostLeft => _run.BoostLeft;
        public void DebugStartBoost() { _run.BoostLeft = 1.4f; }
        public float DebugTargetSpeed => _car != null ? _car.TargetSpeed : 0f;
        public StageLadderDefinition DebugEndlessLadder => _endlessLadder;
        public bool DebugSliceCleared => _sliceCleared;
        public string DebugBanner => Model.StageBanner;
        public int DebugEventSeen(string id) { return _profile.EventSeen(id); }
        public void DebugResetEventSeen(string id) { _profile.AddCounter("evtseen:" + id, -_profile.EventSeen(id)); }
        /// <summary>Replaces the live plan with one event at <paramref name="startM"/> and returns it (the director, signals and behaviours then run for real).</summary>
        public StageEventState DebugInjectEvent(StageEventDefinition d, float startM) { return _stage.Active ? _stage.Director.DebugInject(d, startM, true) : null; }
        /// <summary>Takes hearts until the run ends (the real death path: game over, effects must revert).</summary>
        public void DebugKill() { for (int i = 0; i < 6 && _state == ScreenState.Playing; i++) HandleLifeLoss("QA PROBE"); }
        public void DebugLoseLife() { HandleLifeLoss("QA PROBE (one life)"); }
        public bool DebugKeepGoingNow() { if (!KeepGoingAvailable()) return false; KeepGoing(); return true; }
        public float DebugFinishGate() { return FinishGateM(); }
        public int DebugStageFails(int stage) { return _profile.StageFails(stage); }
        public void DebugClearStageFail(int stage) { _profile.ClearStageFail(stage); }
        public int[] DebugEasedStages() { return _worldCtx.EasedStages ?? new int[0]; }
        public void DebugSetDistanceAtLeast(float d) { if (_car != null) _distance = Mathf.Max(_distance, d); }
        /// <summary>Teleports the car to the end of the slice (QA) so the finish gate logic runs in the real tick.</summary>
        public void DebugJumpToGate() { DebugJumpToStage(FinishGateM() - 12f); }
        public bool DebugStageOn => _stageOn;
        public float DebugDistance => _distance;
        /// <summary>Flat runway laid ahead of a QA jump (metres): long enough to ride through a whole zone without drawing.</summary>
        public const float QaRunwayAheadM = 170f;

        /// <summary>QA jump (Editor and development builds only): moves the run to <paramref name="distanceM"/> on a fresh flat runway, full ink and hearts, and lets the director replay the warning state.</summary>
        public void DebugJumpToStage(float distanceM)
        {
            if (_state != ScreenState.Playing || _car == null || !_stage.Active) return;
            JumpRun(distanceM);
            var rec = _profile.lastStageRun; rec.debugJumped = true; _profile.lastStageRun = rec;
            Debug.Log("[Stage] debug_jump " + distanceM + " m");
        }
#endif

        /// <summary>Moves the run to <paramref name="distanceM"/> on a fresh flat runway with full ink and hearts and replays the director state. Used by the post-run revive and the QA jump.</summary>
        private void JumpRun(float distanceM)
        {
            if (_car == null || !_stage.Active) return;
            _brush.Cancel();
            float worldX = _startX + distanceM;
            _zone.Exit();
            ApplySurface();
            _stagePresenter.Reset();
            _eater.Stop(); _boss.Stop(); _hitStop.Release(); _announcer?.Clear(); _gateProp.Hide(); _finishing = false; _bossEndS = -1f;
            _road.ResetRoad(worldX - 14f, worldX + QaRunwayAheadM, 0f);
            _car.RespawnSafely(new Vector2(worldX, 0f));
            _surf.OnRespawn();
            _distance = distanceM;
            _pickups.Reset(worldX + 16f);
            _meteors.Reset();
            currentInk = maxInk; currentLives = maxLives;
            UpdateHeartsUI(); UpdateInkUI();
            _fx.DistanceM = distanceM;
            _runEnding = true; _stage.JumpTo(distanceM); _runEnding = false;
            _stage.NotifyPickupsCleared(distanceM);                              // Road Tax gates were cleared with the other pickups
            _cameraRig.Snap(_car);
        }

        public void SetAdaptiveChallenge(bool enabled)
        {
            if (_profile == null) return;
            _profile.adaptiveChallenge = enabled;
            SavePlayerData();
            SyncModel();
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
