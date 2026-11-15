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
        private bool _stageOn;
        // Road-Eater, RO-B0, near-miss economy (phase B2). Inert unless the stage director runs.
        private RoadEaterController _eater;
        private BossController _boss;
        private MonsterDefinitionSO _eaterData;
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
        private void OnDisable() { Collectible.Collected -= OnCollectItem; Time.timeScale = 1f; Time.fixedDeltaTime = 0.02f; Collectible.MagnetTarget = null; }

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
                InkPerMeter = () => inkCostPerMeter * _trackInk,
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
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            gameObject.AddComponent<QaJumpOverlay>().Init(this);
#endif

            LoadPlayerData();
            _meta = new MetaProgress(_profile, _tuning);
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
            engineLevel = _profile.engineLevel;
            suspensionLevel = _profile.suspensionLevel;
            inkTankLevel = _profile.inkTankLevel;
            boostLevel = _profile.boostLevel;
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

        private void SavePlayerData()
        {
            if (_profile == null) _profile = new PlayerProfile();
            _profile.coins = totalCoins;
            _profile.bestDistance = _bestScore;
            _profile.engineLevel = engineLevel;
            _profile.suspensionLevel = suspensionLevel;
            _profile.inkTankLevel = inkTankLevel;
            _profile.boostLevel = boostLevel;
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
            _previousState = _state;
            _state = newState;
            _stateTime = 0f;
            if (newState != ScreenState.Playing)
            {
                ApplyTimeScale(1f); Collectible.MagnetTarget = null;
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
            float engineBonus = (engineLevel - 1) * 0.8f;
            _car.TargetSpeed = (baseStartSpeed + engineBonus) * _car.SpeedMultiplier * _trackSpeed;
            _car.maxTorquePerWheel += (suspensionLevel - 1) * 3f;

            maxInk = 100f + (inkTankLevel - 1) * 30f;
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
            _meteors.Reset();
            _powerKey = -1;
            BeginStageRun(_previousState == ScreenState.GameOver);

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
                _brush.SmoothingSpeed = brushSmoothingSpeed;
                _brush.DragSpacing = dragSpacing;
                _brush.InkCostPerMeter = _run.Ghost ? 0f : inkCostPerMeter * _trackInk;
                _brush.Tick();

                currentInk = Mathf.Min(maxInk, currentInk + _tuning.inkRegenPerSec * Time.deltaTime);

                if (_car == null) return;

                Vector2 carPos = _car.Body.position;
                _distance = Mathf.Max(_distance, carPos.x - _startX);
                float speedRatio = Mathf.InverseLerp(baseStartSpeed, baseMaxSpeed + (engineLevel * 0.8f), _car.TargetSpeed);
                float baseTarget = Mathf.Min(baseMaxSpeed + (engineLevel * 0.8f), baseStartSpeed + (engineLevel * 0.8f) + _distance * speedGainPerMetre) * _trackSpeed;
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
                _car.TargetSpeed = baseTarget + _run.BoostSpeedBonus;
                _run.Tick(Time.unscaledDeltaTime, _car, _distance);
                Collectible.MagnetTarget = _run.MagnetOn ? _car.Body.transform : null;
                Collectible.MagnetRadius = _tuning.magnetRadius;
                ApplyTimeScale(_run.TimeScale);
                if (SoundManager.Instance) SoundManager.Instance.SetEngineRunning(true, speedRatio);

                _pickups.Tick(carPos.x);
                _meteors.Tick(Time.deltaTime, _car, _distance);
                _audio.InkLevel(maxInk > 0f ? currentInk / maxInk : 1f);
                if (_run.Frenzying) FrenzyTrail(carPos);

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
                _upsideDownTime = 0f;
                if (_stage.Active)
                {
                    _stage.OnRespawn(_distance);
                    _hitGraceLeft = 1.5f;                       // FR-7 and FR-9: no further hazard damage for 1.5 s after any hit, shielded or not
                    _nearLedger.NoteDamage(); _nearLedger.NoteRespawn();
                    _eater.OnLifeLost(_car.Body.position.x);    // push-back (no-op unless the Eater is chasing)
                    _boss.OnRespawn();                          // clear projectiles, 3 s lull (no-op unless RO-B0 is fighting)
                }
            }
            return shielded;
        }

        private void TriggerGameOver(string reason, bool finished = false)
        {
            EndStageRun(finished ? StageRunOutcome.Finished : StageRunOutcome.Died);
            bool newBest = _distance > _bestScore;
            if (newBest) _bestScore = _distance;
            totalCoins += sessionCoins;
            _meta.RecordRun(_run.Stats, _distance, _selVehicleId);
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

            SwitchScreen(ScreenState.GameOver);
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
            if (Mathf.Approximately(Time.timeScale, scale)) return;
            Time.timeScale = scale;
            Time.fixedDeltaTime = 0.02f * scale;
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
            m.Gems = _profile != null ? _profile.gems : 0;
            m.BoostLevel = boostLevel;
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
                int key = (_run.Shield ? 1 : 0) + Mathf.CeilToInt(_run.MagnetLeft) * 2 + Mathf.CeilToInt(_run.DoubleLeft) * 128 + Mathf.CeilToInt(_run.SlowLeft) * 8192 + Mathf.CeilToInt(_run.GhostLeft) * 524288;
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
            if (_car == null || _state != ScreenState.Playing) return;
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
                totalCoins = _profile.coins;
                SavePlayerData();
                if (SoundManager.Instance) SoundManager.Instance.PlayUpgrade();
                SyncModel();
            }
        }

        public void CycleWorld(int dir)
        {
            int total = System.Enum.GetValues(typeof(EnvironmentTheme)).Length;
            SelectedTheme = (EnvironmentTheme)(((int)SelectedTheme + dir + total) % total);
            _selWorldId = "";
            ApplyWorld();
            ApplySurface();
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
            int currentLevel = (statIndex == 0) ? engineLevel : ((statIndex == 1) ? suspensionLevel : ((statIndex == 2) ? inkTankLevel : boostLevel));
            if (currentLevel >= GameCatalog.MaxUpgradeLevel) return;

            int cost = GameCatalog.UpgradeCost(currentLevel);
            if (totalCoins >= cost)
            {
                totalCoins -= cost;
                if (statIndex == 0) engineLevel++;
                else if (statIndex == 1) suspensionLevel++;
                else if (statIndex == 2) inkTankLevel++;
                else if (statIndex == 3) boostLevel++;

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
            if (e.cost > 0 && !_unlockedVehicleIds.Contains(e.id)) return null;
            return e;
        }

        private RoadCar CreateSelectedCar(Vector2 pos)
        {
            var e = CatalogVehicle();
            if (e == null) return RoadCar.Create(pos, SelectedVehicle, _worldArt);   // slice vehicles follow the world's art style; catalogue vehicles keep their own art
            return RoadCar.Create(pos, e.MakeRig(_worldArt));   // Smooth worlds: smooth body and wheels for vehicles whose smooth art is ready; same size, mass, torque
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
                _roadMaterial.friction = e.friction;
                _roadMaterial.bounciness = e.bounce;
                SetSurfaceTuning(e);   // gameplay numbers always come from the selected surface, whatever strip is drawn
                if (ApplyEntrySkin(e)) return;
            }
            _roadMaterial.friction = 1.25f;
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
            _stage = new StageRunController(ladder);
            _stage.Register("EVT-ICE", new SurfaceZoneBehaviour(EnterZone, ExitZone));
            _stage.Register("EVT-MUD", new SurfaceZoneBehaviour(EnterZone, ExitZone));
            _stage.Register("EVT-GLASS", new SurfaceZoneBehaviour(EnterZone, ExitZone));
            _stage.Register("EVT-METEOR", new MeteorWindowBehaviour(_meteors));
            _stage.Register("EVT-RINGS", new RingWindowBehaviour(PlaceRingGauntlet));
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

            _eater = new RoadEaterController(_road, _eaterData, _cam)
            {
                Cue = id => _audio?.StageCue(id),
                Banner = ShowStageBanner,
                Shake = a => _cameraRig?.Shake(a, 0.4f),
                Bite = () => HandleLifeLoss("EATEN BY THE ROAD-EATER!"),
                Cleared = OnEaterCleared,
                NearMissed = OnEaterNearMiss
            };
            _boss = new BossController(_road, _bossData)
            {
                Cue = id => _audio?.StageCue(id),
                Banner = ShowStageBanner,
                Shake = a => _cameraRig?.Shake(a, 0.4f),
                CanDamage = () => _hitGraceLeft <= 0f && !_stage.HazardGraceActive,
                Hit = r => HandleLifeLoss(r),
                CounterAvailable = () => _run.Boost01 >= 1f || sessionCoins + totalCoins >= _bossData.counterCoins || (maxInk > 0f && currentInk / maxInk >= _bossData.counterInk01),
                InkAvailable = () => maxInk > 0f && currentInk / maxInk >= _bossData.counterInk01,
                NearMissed = (src, perfect, travel) => ResolveNearMiss(src, perfect, travel),
                RingTaken = () => _run.CollectPower(Collectible.Type.Ring),
                Decided = OnBossDecided
            };
            _boss.Model.Raised += e => { if (e == BossEvent.Gone) _bossEndS = _distance; };

            // Behaviour modules registered by event id on the existing run controller (no switch): the director only knows ids and distances.
            _stage.Register("MON-EATER", new DelegateBehaviour(
                s => _eater.StartChase(_car.Body.position.x),
                s => { if (!_runEnding && _eater.Model.Chasing) _eater.Model.Retreat(true); }));      // the window ended at 1750 m: timeout retreat
            _stage.Register("BOS-ROB0", new DelegateBehaviour(
                s => _boss.Start(_car.Body.position),
                s => { },                                                                              // the fight outlives its planned window: it ends by Overload or the 105 s timeout
                (s, d) => _boss.OnRespawn()));
            _stage.Signal += (e, sig) => { if (sig == StageSignal.Announcer && e.Definition.role == StageEventRole.Monster) _eater.Warn(); };
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
            _eater.Tick(dt, carPos.x, carPos.y, vel.x, !_car.IsGrounded, _run.Boosting);
            if (!_stage.Active) return;                                                       // the bite or a hit may have ended the run
            _boss.Tick(dt, carPos, vel);
            var m = Model; var em = _eater.Model; var bm = _boss.Model;
            m.EaterOn = em.Chasing; m.EaterGapM = em.Gap; m.EaterBand = (int)em.Band; m.EaterEscape01 = em.EscapeProgress01;
            m.BossOn = bm.Fighting; m.BossOverload01 = bm.Meter01; m.BossPhase = bm.Phase;
            if (!_finishing && _distance >= FinishGateM() && !bm.Fighting && bm.State != BossState.Intro && bm.State != BossState.Defeat)
            {
                _finishing = true;
                _audio?.StageCue(StageCues.Finish);
                TriggerGameOver("COURSE COMPLETE!", true);
            }
        }

        /// <summary>Finish gate: max(2750, boss end + 150) (brief X7); before the boss has ended it is the planned finish.</summary>
        private float FinishGateM()
        {
            float planned = _stage.Director.FinishM;
            return _bossEndS >= 0f ? Mathf.Max(planned, _bossEndS + _stage.Ladder.finishAfterBossM) : planned;
        }

        private void ResolveNearMiss(int source, bool perfect, float travelM)
        {
            int non = _run.Stats.coins + _run.Stats.bonusCoins - _nearLedger.Paid;
            var r = _nearLedger.Resolve(source, perfect, travelM, _recentSpeed, non);
            if (r.Valid) _run.NearMissPaid(r.Coins, (r.Perfect ? "PERFECT CLOSE!" : "CLOSE!") + (r.Chain > 1 ? " x" + r.Chain : ""));
        }

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
            var pay = HazardRewards.Eater(_eaterData, _profile, timeout);
            HazardRewards.RecordEater(_profile, timeout);
            SavePlayerData();
            ShowStageBanner(StageCues.BannerText(timeout ? "banner.eater.retreat" : "banner.eater.escape"));
            _run.AwardEvent(pay.Label, pay.Coins, pay.Gems, pay.Frenzy);
        }

        private void OnBossDecided(bool timeout)
        {
            var pay = HazardRewards.Boss(_bossData, _profile, timeout);
            HazardRewards.RecordBoss(_profile, timeout);
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
            _stagePresenter.Reset();
            _stageOn = StageRunController.ShouldRun(_tuning.stageEvents, IsCountrysideWorld()) && _stage.HasData;
            _meteors.StageDriven = _stageOn;
            _eater.Stop(); _boss.Stop();
            _road.ClearNoEatRanges();
            _nearLedger.Reset(); _hitGraceLeft = 0f; _recentSpeed = 0f; _bossEndS = -1f; _finishing = false; _runEnding = false;
            Model.EaterOn = false; Model.BossOn = false;
            if (!_stageOn) return;
            // Retry reuses the saved seed (same ladder); a new run draws a new one. The record is saved through SaveService only.
            var saved = _profile.lastStageRun;
            StageRunRecord rec;
            if (retry && saved.runSeed != 0u && saved.layoutVersion == _stage.Ladder.layoutVersion) { rec = saved; rec.debugJumped = false; }
            else
            {
                rec = StageRunController.NewRecord((ulong)System.DateTime.UtcNow.Ticks, _stage.Ladder.layoutVersion);
                _profile.lastStageRun = rec;
                SavePlayerData();
            }
            _stage.Begin(rec);
            _eater.Begin(_startX, rec.runSeed ^ 0x00E47E8u);
            _boss.Begin(rec.runSeed ^ 0x00B055u);
        }

        private void EndStageRun(StageRunOutcome outcome)
        {
            if (_stage == null || !_stage.Active) return;
            _runEnding = true;
            _stage.End(outcome);                 // Ended is raised for the Active zone or window, which clears them
            _runEnding = false;
            _eater.Stop(); _boss.Stop();
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

        private void OnApplicationPause(bool paused) { _stage?.OnPause(paused); }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public StageRunController DebugStage => _stage;
        public bool DebugStageOn => _stageOn;
        public float DebugDistance => _distance;
        /// <summary>Flat runway laid ahead of a QA jump (metres): long enough to ride through a whole zone without drawing.</summary>
        public const float QaRunwayAheadM = 170f;

        /// <summary>QA jump (Editor and development builds only): moves the run to <paramref name="distanceM"/> on a fresh flat runway, full ink and hearts, and lets the director replay the warning state.</summary>
        public void DebugJumpToStage(float distanceM)
        {
            if (_state != ScreenState.Playing || _car == null || !_stage.Active) return;
            _brush.Cancel();
            float worldX = _startX + distanceM;
            _zone.Exit();
            ApplySurface();
            _stagePresenter.Reset();
            _eater.Stop(); _boss.Stop(); _finishing = false; _bossEndS = -1f;
            _road.ResetRoad(worldX - 14f, worldX + QaRunwayAheadM, 0f);
            _car.RespawnSafely(new Vector2(worldX, 0f));
            _distance = distanceM;
            _pickups.Reset(worldX + 16f);
            _meteors.Reset();
            currentInk = maxInk; currentLives = maxLives;
            UpdateHeartsUI(); UpdateInkUI();
            _stage.JumpTo(distanceM);
            var rec = _profile.lastStageRun; rec.debugJumped = true; _profile.lastStageRun = rec;
            _cameraRig.Snap(_car);
            Debug.Log("[Stage] debug_jump " + distanceM + " m");
        }
#endif

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
