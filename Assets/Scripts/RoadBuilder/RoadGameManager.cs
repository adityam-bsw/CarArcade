using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace RoadBuilderGame
{
    public class RoadGameManager : MonoBehaviour
    {
        public static RoadGameManager Instance { get; private set; }

        public enum ScreenState
        {
            HomeDashboard,
            StageSelect,
            GarageMenu,
            Playing,
            GameOver
        }

        [Header("Scene Object References (Inspectable in Editor)")]
        [SerializeField] private Camera _cam;
        [SerializeField] private RoadBuilder _road;
        [SerializeField] private ParallaxBackground _background;
        [SerializeField] private SoundManager _soundManager;
        [SerializeField] private ParticleManager _particleManager;
        [SerializeField] private EventSystem _eventSystem;

        [Header("UI Panels & Canvas (Visible and Editable in Scene)")]
        [SerializeField] public GameObject uiCanvas;
        [SerializeField] public GameObject homePanel;
        [SerializeField] public GameObject stagePanel;
        [SerializeField] public GameObject garagePanel;
        [SerializeField] public GameObject hudPanel;
        [SerializeField] public GameObject gameOverPanel;

        [Header("Home View Bindings")]
        [SerializeField] public Text homeCoinsText;
        [SerializeField] public Text homeBestText;

        [Header("Stage View Bindings")]
        [SerializeField] public Image stagePreviewImg;
        [SerializeField] public Text stageTitleText;
        [SerializeField] public Text stageDescText;

        [Header("Garage View Bindings")]
        [SerializeField] public Text garageCarNameText;
        [SerializeField] public Text garageUnlockBtnText;
        [SerializeField] public Text engineValText;
        [SerializeField] public Text suspValText;
        [SerializeField] public Text inkValText;
        [SerializeField] public Text engineCostText;
        [SerializeField] public Text suspCostText;
        [SerializeField] public Text inkCostText;

        [Header("HUD Bindings")]
        [SerializeField] public Text hudDistanceText;
        [SerializeField] public Text hudSpeedText;
        [SerializeField] public Text hudCoinsText;
        [SerializeField] public RectTransform hudInkFill;
        [SerializeField] public Text hudInkText;
        [SerializeField] public GameObject[] hudHeartIcons;

        [Header("Game Over Bindings")]
        [SerializeField] public Text goReasonText;
        [SerializeField] public Text goDistanceText;
        [SerializeField] public Text goCoinsText;

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

        private const string CoinsKey = "HCD_Coins";
        private const string BestScoreKey = "HCD_Best";
        private const string EngineKey = "HCD_Engine";
        private const string SuspKey = "HCD_Susp";
        private const string InkTankKey = "HCD_Ink";
        private const string VehicleKey = "HCD_Vehicle";
        private const string ThemeKey = "HCD_Theme";
        private const string RoadStyleKey = "HCD_RoadStyle";
        private const string SportUnlockKey = "HCD_Unlock_Sport";
        private const string TruckUnlockKey = "HCD_Unlock_Truck";

        private RoadCar _car;
        private PhysicsMaterial2D _roadMaterial;

        private ScreenState _state = ScreenState.HomeDashboard;
        private float _startX, _distance, _bestScore, _stateTime, _upsideDownTime;
        private Vector2 _lastStrokeWorld;
        private Vector2 _smoothedBrushWorld;
        private bool _isDrawingStroke = false;

        [Header("Smooth Brush Settings")]
        [Tooltip("Higher values follow finger faster; lower values create silkier macro curves")]
        public float brushSmoothingSpeed = 22f;

        public VehicleType SelectedVehicle { get; private set; } = VehicleType.ClassicRed;
        public EnvironmentTheme SelectedTheme { get; private set; } = EnvironmentTheme.Country;
        public RoadStyle SelectedRoadStyle { get; private set; } = RoadStyle.AsphaltHighway;

        private readonly List<Collectible> _activeCollectibles = new List<Collectible>();
        private float _nextSpawnX = 14f;
        private int _lastScreenW, _lastScreenH;
        private Rect _lastSafeArea;

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

            // 5. Ensure ParallaxBackground exists
            if (_background == null) _background = FindAnyObjectByType<ParallaxBackground>();
            if (_background == null)
            {
                var bgGo = new GameObject("Background");
                _background = bgGo.AddComponent<ParallaxBackground>();
            }
            _background.Init();

            LoadPlayerData();
            _background.ApplyTheme(SelectedTheme);
            _road.SetRoadStyle(SelectedRoadStyle);

            // If UI Canvas wasn't pre-constructed in scene, build it as fallback
            if (uiCanvas == null)
            {
                BuildComprehensiveUI();
            }
            else
            {
                BindButtonEvents();
            }

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

            // 1. Update CanvasScaler to Expand mode so zero UI elements ever clip off-screen on narrow/tall or ultrawide displays
            if (uiCanvas != null)
            {
                var scaler = uiCanvas.GetComponent<CanvasScaler>();
                if (scaler != null)
                {
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(1920, 1080);
                    scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
                }

                // 2. Apply Screen.safeArea normalized anchors to all main panels (protects against notches & rounded corners)
                if (w > 0 && h > 0)
                {
                    Vector2 anchorMin = new Vector2(safe.xMin / w, safe.yMin / h);
                    Vector2 anchorMax = new Vector2(safe.xMax / w, safe.yMax / h);
                    ApplySafeAreaToPanel(homePanel, anchorMin, anchorMax);
                    ApplySafeAreaToPanel(hudPanel, anchorMin, anchorMax);
                }
            }

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

        private static void ApplySafeAreaToPanel(GameObject panel, Vector2 anchorMin, Vector2 anchorMax)
        {
            if (panel == null) return;
            var rt = panel.GetComponent<RectTransform>();
            if (rt == null) return;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private void LoadPlayerData()
        {
            totalCoins = PlayerPrefs.GetInt(CoinsKey, 150);
            _bestScore = PlayerPrefs.GetFloat(BestScoreKey, 0f);
            engineLevel = Mathf.Clamp(PlayerPrefs.GetInt(EngineKey, 1), 1, 5);
            suspensionLevel = Mathf.Clamp(PlayerPrefs.GetInt(SuspKey, 1), 1, 5);
            inkTankLevel = Mathf.Clamp(PlayerPrefs.GetInt(InkTankKey, 1), 1, 5);
            SelectedVehicle = (VehicleType)PlayerPrefs.GetInt(VehicleKey, 0);
            SelectedTheme = (EnvironmentTheme)PlayerPrefs.GetInt(ThemeKey, 0);
            SelectedRoadStyle = (RoadStyle)PlayerPrefs.GetInt(RoadStyleKey, 0);

            sportUnlocked = PlayerPrefs.GetInt(SportUnlockKey, 0) == 1;
            truckUnlocked = PlayerPrefs.GetInt(TruckUnlockKey, 0) == 1;
        }

        private void SavePlayerData()
        {
            PlayerPrefs.SetInt(CoinsKey, totalCoins);
            PlayerPrefs.SetFloat(BestScoreKey, _bestScore);
            PlayerPrefs.SetInt(EngineKey, engineLevel);
            PlayerPrefs.SetInt(SuspKey, suspensionLevel);
            PlayerPrefs.SetInt(InkTankKey, inkTankLevel);
            PlayerPrefs.SetInt(VehicleKey, (int)SelectedVehicle);
            PlayerPrefs.SetInt(ThemeKey, (int)SelectedTheme);
            PlayerPrefs.SetInt(RoadStyleKey, (int)SelectedRoadStyle);
            PlayerPrefs.SetInt(SportUnlockKey, sportUnlocked ? 1 : 0);
            PlayerPrefs.SetInt(TruckUnlockKey, truckUnlocked ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void SwitchScreen(ScreenState newState)
        {
            _state = newState;
            _stateTime = 0f;

            if (homePanel) homePanel.SetActive(newState == ScreenState.HomeDashboard);
            if (stagePanel) stagePanel.SetActive(newState == ScreenState.StageSelect);
            if (garagePanel) garagePanel.SetActive(newState == ScreenState.GarageMenu);
            if (hudPanel) hudPanel.SetActive(newState == ScreenState.Playing);
            if (gameOverPanel) gameOverPanel.SetActive(newState == ScreenState.GameOver);

            switch (newState)
            {
                case ScreenState.HomeDashboard:
                    SetupShowcaseCar();
                    if (SoundManager.Instance) SoundManager.Instance.SetEngineRunning(false, 0f);
                    UpdateHomeUI();
                    break;

                case ScreenState.StageSelect:
                    UpdateStageUI();
                    break;

                case ScreenState.GarageMenu:
                    SetupShowcaseCar();
                    UpdateGarageUI();
                    break;

                case ScreenState.Playing:
                    StartGameplaySession();
                    break;

                case ScreenState.GameOver:
                    if (SoundManager.Instance)
                    {
                        SoundManager.Instance.SetEngineRunning(false, 0f);
                        SoundManager.Instance.PlayCrash();
                    }
                    UpdateGameOverUI();
                    break;
            }
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
            _road.ResetRoad(-15f, 25f, 0f);
            _car = RoadCar.Create(new Vector2(0f, 1.2f), SelectedVehicle);
            _car.MotorOn = false;

            _cam.transform.position = new Vector3(0f, 1.8f, -10f);
            _background.Follow(_cam);
        }

        private void StartGameplaySession()
        {
            CleanupAllCars();
            foreach (var col in _activeCollectibles)
                if (col != null) Destroy(col.gameObject);
            _activeCollectibles.Clear();

            _background.ApplyTheme(SelectedTheme);
            _road.SetRoadStyle(SelectedRoadStyle);
            _road.ResetRoad(-20f, 12f, 0f);

            _car = RoadCar.Create(new Vector2(-2f, 1.2f), SelectedVehicle);
            _car.MotorOn = true;

            // Apply Upgrades:
            float engineBonus = (engineLevel - 1) * 0.8f;
            _car.TargetSpeed = baseStartSpeed + engineBonus;
            _car.maxTorquePerWheel += (suspensionLevel - 1) * 3f;

            maxInk = 100f + (inkTankLevel - 1) * 30f;
            currentInk = maxInk;

            _startX = _car.Body.position.x;
            _distance = 0f;
            _upsideDownTime = 0f;
            _nextSpawnX = 16f;
            _isDrawingStroke = false;
            currentLives = maxLives;
            sessionCoins = 0;

            UpdateHeartsUI();
            UpdateInkUI();
            if (SoundManager.Instance) SoundManager.Instance.SetEngineRunning(true, 0.2f);
            SnapCamera();
        }

        private void Update()
        {
            _stateTime += Time.deltaTime;
            UpdateResponsiveLayout(force: false);

            if (_state == ScreenState.Playing)
            {
                HandleGameplayInput();

                currentInk = Mathf.Min(maxInk, currentInk + inkRegenPerSec * Time.deltaTime);
                UpdateInkUI();

                if (_car == null) return;

                Vector2 carPos = _car.Body.position;
                _distance = Mathf.Max(_distance, carPos.x - _startX);
                float speedRatio = Mathf.InverseLerp(baseStartSpeed, baseMaxSpeed + (engineLevel * 0.8f), _car.TargetSpeed);
                _car.TargetSpeed = Mathf.Min(baseMaxSpeed + (engineLevel * 0.8f), baseStartSpeed + (engineLevel * 0.8f) + _distance * speedGainPerMetre);
                if (SoundManager.Instance) SoundManager.Instance.SetEngineRunning(true, speedRatio);

                SpawnPickupsAhead(carPos.x);

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

                // Update HUD
                if (hudDistanceText) hudDistanceText.text = $"{_distance:0} m";
                if (hudSpeedText) hudSpeedText.text = $"{_car.TargetSpeed * 3.6f:0} km/h";
                if (hudCoinsText) hudCoinsText.text = $"COINS: {totalCoins + sessionCoins}";
            }
        }

        private void HandleLifeLoss(string reason)
        {
            _isDrawingStroke = false;
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

            if (goReasonText) goReasonText.text = reason;
            if (goDistanceText) goDistanceText.text = $"{_distance:0} METRES{(newBest ? "\n<color=#FFD700>★ NEW PERSONAL BEST! ★</color>" : "")}";
            if (goCoinsText) goCoinsText.text = $"COINS WON: +{sessionCoins} (TOTAL: {totalCoins})";

            SwitchScreen(ScreenState.GameOver);
        }

        private void HandleGameplayInput()
        {
            // Don't draw road strokes if pointer is over any UI button
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            Vector2 screenPos = Vector2.zero;
            bool down = false;
            bool held = false;
            bool up = false;

            // 1. Check New Input System Pointer
            var pointer = Pointer.current;
            if (pointer != null)
            {
                screenPos = pointer.position.ReadValue();
                down = pointer.press.wasPressedThisFrame;
                held = pointer.press.isPressed;
                up = pointer.press.wasReleasedThisFrame;
            }

            // 2. Legacy / Mouse Fallback (Supports legacy editor clicks, mouse, and touches)
            if (!down && !held && !up)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    down = true;
                    held = true;
                    screenPos = Input.mousePosition;
                }
                else if (Input.GetMouseButton(0))
                {
                    held = true;
                    screenPos = Input.mousePosition;
                }
                else if (Input.GetMouseButtonUp(0))
                {
                    up = true;
                    screenPos = Input.mousePosition;
                }
            }

            if (!down && !held && !up) return;

            Vector2 world = _cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -_cam.transform.position.z));

            if (down)
            {
                StartStroke(world);
            }
            else if (held && _isDrawingStroke)
            {
                ContinueStroke(world);
            }
            else if (up && _isDrawingStroke)
            {
                EndStroke();
            }
        }

        private void StartStroke(Vector2 worldPos)
        {
            if (currentInk < 4f) return;

            // Only start a stroke if it is allowed by forward-only / anti-overlap rules
            if (!_road.BeginNewSegment(worldPos))
            {
                _isDrawingStroke = false;
                return;
            }

            _isDrawingStroke = true;
            _smoothedBrushWorld = worldPos;
            _lastStrokeWorld = _road.LastPoint;
            if (SoundManager.Instance) SoundManager.Instance.PlayDraw();
            if (ParticleManager.Instance) ParticleManager.Instance.SpawnBurst(_lastStrokeWorld, new Color(0.9f, 0.95f, 1f), 3);
        }

        private void ContinueStroke(Vector2 worldPos)
        {
            if (currentInk < 2f)
            {
                EndStroke();
                return;
            }

            // Smooth the pointer position using an exponential spring filter so micro-jitters vanish
            float alpha = 1f - Mathf.Exp(-brushSmoothingSpeed * Time.deltaTime);
            _smoothedBrushWorld = Vector2.Lerp(_smoothedBrushWorld, worldPos, alpha);

            float d = Vector2.Distance(_lastStrokeWorld, _smoothedBrushWorld);
            if (d >= dragSpacing)
            {
                if (_road.AddPointToCurrent(_smoothedBrushWorld))
                {
                    Vector2 actualAdded = _road.LastPoint;
                    float committedDist = Vector2.Distance(_lastStrokeWorld, actualAdded);
                    currentInk = Mathf.Max(0f, currentInk - committedDist * inkCostPerMeter);
                    UpdateInkUI();
                    _lastStrokeWorld = actualAdded;
                    if (SoundManager.Instance) SoundManager.Instance.PlayDraw();
                    if (ParticleManager.Instance) ParticleManager.Instance.SpawnBurst(actualAdded, new Color(0.85f, 0.95f, 1f), 2);
                }
            }
            else
            {
                // Continuously stretch the elastic live tip between knot commits for 60fps silkiness
                _road.UpdateLiveStrokeTip(_smoothedBrushWorld);
            }
        }

        private void EndStroke()
        {
            _isDrawingStroke = false;
            _road.EndSegment();
        }

        private void LateUpdate()
        {
            if (_state == ScreenState.Playing && _car != null && _cam != null)
            {
                Vector3 currentCam = _cam.transform.position;
                Vector3 carPos = _car.Body.position;

                // Camera X follows car smoothly with forward look-ahead
                float targetX = carPos.x + 2.5f;
                // Camera Y is stabilized: follows road or car gently, clamped so background never whips wildly during a fall
                float targetY = Mathf.Clamp(carPos.y + 1.1f, -2.5f, 8.5f);

                float newX = Mathf.Lerp(currentCam.x, targetX, 7.5f * Time.deltaTime);
                float newY = Mathf.Lerp(currentCam.y, targetY, 3.5f * Time.deltaTime);

                _cam.transform.position = new Vector3(newX, newY, -10f);
                _background.Follow(_cam);
            }
            else if (_background != null && _cam != null)
            {
                _background.Follow(_cam);
            }
        }

        private void SnapCamera()
        {
            if (_car == null || _cam == null) return;
            Vector3 pos = _car.Body.position;
            pos.z = -10f;
            pos.y = Mathf.Clamp(pos.y + 1.1f, -2.5f, 8.5f);
            pos.x += 2.5f;
            _cam.transform.position = pos;
        }

        private void SpawnPickupsAhead(float carX)
        {
            // Spawn pickups along a smooth, predictable world wave completely decoupled from road creation!
            while (_nextSpawnX < carX + 32f)
            {
                float x = _nextSpawnX;
                // Smooth world-space sine wave for natural collectible arcs (independent of player road strokes)
                float waveY = 1.4f + Mathf.Sin(x * 0.14f) * 1.35f + Mathf.Cos(x * 0.06f) * 0.65f;

                // If a road happens to already exist at X, ensure the item sits nicely above it, never buried inside
                if (_road.TryGetRoadYAtX(x, out float existingRoadY))
                {
                    waveY = Mathf.Max(waveY, existingRoadY + 1.35f);
                }

                int roll = Random.Range(0, 100);
                Collectible.Type t;
                Sprite s;
                if (roll < 65)
                {
                    t = Collectible.Type.Coin;
                    s = Resources.Load<Sprite>("RoadBuilder/Coin");
                }
                else if (roll < 88)
                {
                    t = Collectible.Type.Ink;
                    s = Resources.Load<Sprite>("RoadBuilder/InkCanister");
                }
                else
                {
                    t = Collectible.Type.Heart;
                    s = Resources.Load<Sprite>("RoadBuilder/Heart");
                }

                // Spawn coins in neat 3-coin arcs so the player has clear visual targets to build towards!
                if (t == Collectible.Type.Coin)
                {
                    for (int c = 0; c < 3; c++)
                    {
                        float cx = x + c * 1.6f;
                        float cy = waveY + Mathf.Sin((c / 2f) * Mathf.PI) * 0.55f;
                        if (_road.TryGetRoadYAtX(cx, out float ry)) cy = Mathf.Max(cy, ry + 1.35f);

                        var coinGo = new GameObject("Pickup_Coin");
                        coinGo.transform.position = new Vector3(cx, cy, 0f);
                        var coinPickup = coinGo.AddComponent<Collectible>();
                        coinPickup.Init(t, s);
                        _activeCollectibles.Add(coinPickup);
                    }
                    _nextSpawnX += Random.Range(11f, 16f);
                }
                else
                {
                    var itemGo = new GameObject("Pickup_" + t);
                    itemGo.transform.position = new Vector3(x, waveY, 0f);
                    var pickup = itemGo.AddComponent<Collectible>();
                    pickup.Init(t, s);
                    _activeCollectibles.Add(pickup);
                    _nextSpawnX += Random.Range(9f, 14f);
                }
            }

            for (int i = _activeCollectibles.Count - 1; i >= 0; i--)
            {
                if (_activeCollectibles[i] == null || _activeCollectibles[i].transform.position.x < carX - 18f)
                {
                    if (_activeCollectibles[i] != null) Destroy(_activeCollectibles[i].gameObject);
                    _activeCollectibles.RemoveAt(i);
                }
            }
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

        // ==================================================================== UI SETUP & BINDINGS
        public void BindButtonEvents()
        {
            // Bind Home Buttons
            BindButton(homePanel, "Btn_START RACE!", OnClickStartRace);
            BindButton(homePanel, "Btn_GARAGE", OnClickOpenGarage);
            BindButton(homePanel, "Btn_STAGES", OnClickOpenStages);

            // Bind Stage Select Buttons
            BindButton(stagePanel, "Btn_Prev", OnStagePrev);
            BindButton(stagePanel, "Btn_Next", OnStageNext);
            BindButton(stagePanel, "Btn_CHANGE ROAD MATERIAL", OnClickChangeRoadStyle);
            BindButton(stagePanel, "Btn_Back", OnClickBackToHome);

            // Bind Garage Buttons
            BindButton(garagePanel, "Btn_Prev", OnGarageVehiclePrev);
            BindButton(garagePanel, "Btn_Next", OnGarageVehicleNext);
            BindButton(garagePanel, "Btn_Action", HandleVehicleSelectionAction);
            BindButton(garagePanel, "Btn_Upgrade_0", () => UpgradeStat(0));
            BindButton(garagePanel, "Btn_Upgrade_1", () => UpgradeStat(1));
            BindButton(garagePanel, "Btn_Upgrade_2", () => UpgradeStat(2));
            BindButton(garagePanel, "Btn_Back", OnClickBackToHome);

            // Bind HUD Buttons
            BindButton(hudPanel, "Btn_UNSTUCK", OnClickUnstuckHop);

            // Bind Game Over Buttons
            BindButton(gameOverPanel, "Btn_PLAY AGAIN", OnClickStartRace);
            BindButton(gameOverPanel, "Btn_MENU", OnClickBackToHome);
        }

        public void OnClickUnstuckHop()
        {
            if (_car == null) return;
            if (SoundManager.Instance) SoundManager.Instance.PlayUpgrade();
            if (ParticleManager.Instance)
                ParticleManager.Instance.SpawnBurst(_car.Body.position, new Color(0.2f, 0.95f, 1f), 14);
            _car.NudgeUnstick();
        }

        private void BindButton(GameObject root, string name, UnityEngine.Events.UnityAction action)
        {
            if (root == null) return;
            Transform t = root.transform.Find(name);
            if (t == null)
            {
                // Recursive search
                var btns = root.GetComponentsInChildren<Button>(true);
                foreach (var b in btns)
                {
                    if (b.gameObject.name == name)
                    {
                        b.onClick.RemoveAllListeners();
                        b.onClick.AddListener(action);
                        return;
                    }
                }
                return;
            }
            var btn = t.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(action);
            }
        }

        public void OnClickStartRace()
        {
            if (SoundManager.Instance) SoundManager.Instance.PlayClick();
            SwitchScreen(ScreenState.Playing);
        }

        public void OnClickOpenGarage()
        {
            if (SoundManager.Instance) SoundManager.Instance.PlayClick();
            SwitchScreen(ScreenState.GarageMenu);
        }

        public void OnClickOpenStages()
        {
            if (SoundManager.Instance) SoundManager.Instance.PlayClick();
            SwitchScreen(ScreenState.StageSelect);
        }

        public void OnClickBackToHome()
        {
            if (SoundManager.Instance) SoundManager.Instance.PlayClick();
            SwitchScreen(ScreenState.HomeDashboard);
        }

        public void OnStagePrev() => CycleTheme(-1);
        public void OnStageNext() => CycleTheme(1);
        public void OnClickChangeRoadStyle() => CycleRoadStyle();
        public void OnGarageVehiclePrev() => CycleGarageVehicle(-1);
        public void OnGarageVehicleNext() => CycleGarageVehicle(1);

        private void BuildComprehensiveUI()
        {
            if (uiCanvas != null) Destroy(uiCanvas);

            uiCanvas = new GameObject("FullGameUI");
            var canvas = uiCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = uiCanvas.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            uiCanvas.AddComponent<GraphicRaycaster>();

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            BuildHomePanel(uiCanvas.transform, font);
            BuildStageSelectPanel(uiCanvas.transform, font);
            BuildGaragePanel(uiCanvas.transform, font);
            BuildHUDPanel(uiCanvas.transform, font);
            BuildGameOverPanel(uiCanvas.transform, font);
        }

        // ------------------ 1. HOME DASHBOARD ------------------
        private void BuildHomePanel(Transform parent, Font font)
        {
            homePanel = new GameObject("Panel_Home");
            homePanel.transform.SetParent(parent, false);
            FillRect(homePanel.AddComponent<RectTransform>());

            Sprite bannerSp = Resources.Load<Sprite>("RoadBuilder/Art/GameLogoBanner");
            var logoGo = new GameObject("LogoBanner");
            logoGo.transform.SetParent(homePanel.transform, false);
            var lrt = logoGo.AddComponent<RectTransform>();
            lrt.anchorMin = lrt.anchorMax = new Vector2(0.5f, 1f);
            lrt.anchoredPosition = new Vector2(0, -90);
            lrt.sizeDelta = new Vector2(760, 190);
            var lImg = logoGo.AddComponent<Image>();
            lImg.sprite = bannerSp;
            lImg.raycastTarget = false;

            var titleTxt = MakeText(logoGo.transform, font, 48, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, Color.white);
            titleTxt.text = "ROAD BUILDER RACER";

            homeCoinsText = MakeText(homePanel.transform, font, 44, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(50, -40), new Color(1f, 0.85f, 0.2f));
            homeBestText = MakeText(homePanel.transform, font, 44, TextAnchor.UpperRight, new Vector2(1, 1), new Vector2(-50, -40), Color.white);

            Sprite podSp = Resources.Load<Sprite>("RoadBuilder/Art/Podium");
            var podGo = new GameObject("Podium");
            podGo.transform.SetParent(homePanel.transform, false);
            var prt = podGo.AddComponent<RectTransform>();
            prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0.35f);
            prt.anchoredPosition = new Vector2(0, -20);
            prt.sizeDelta = new Vector2(520, 90);
            var pImg = podGo.AddComponent<Image>();
            pImg.sprite = podSp;
            pImg.raycastTarget = false;

            Sprite greenBtnSp = Resources.Load<Sprite>("RoadBuilder/Art/Btn_RaceGreen");
            Sprite yellowBtnSp = Resources.Load<Sprite>("RoadBuilder/Art/Btn_UpgradeYellow");
            Sprite blueBtnSp = Resources.Load<Sprite>("RoadBuilder/Art/Btn_MenuBlue");

            CreateTexturedButton(homePanel.transform, greenBtnSp, font, "START RACE!", 36, new Vector2(0.5f, 0.15f), new Vector2(0, 0), new Vector2(340, 100), OnClickStartRace);
            CreateTexturedButton(homePanel.transform, yellowBtnSp, font, "GARAGE", 30, new Vector2(0.5f, 0.15f), new Vector2(-360, 0), new Vector2(280, 85), OnClickOpenGarage);
            CreateTexturedButton(homePanel.transform, blueBtnSp, font, "STAGES", 30, new Vector2(0.5f, 0.15f), new Vector2(360, 0), new Vector2(280, 85), OnClickOpenStages);
        }

        private void UpdateHomeUI()
        {
            if (homeCoinsText) homeCoinsText.text = $"COINS: {totalCoins}";
            if (homeBestText) homeBestText.text = $"RECORD: {_bestScore:0} m";
        }

        // ------------------ 2. STAGE / THEME SELECT ------------------
        private void BuildStageSelectPanel(Transform parent, Font font)
        {
            stagePanel = new GameObject("Panel_StageSelect");
            stagePanel.transform.SetParent(parent, false);
            FillRect(stagePanel.AddComponent<RectTransform>());

            var bgCover = stagePanel.AddComponent<Image>();
            bgCover.color = new Color(0.06f, 0.08f, 0.14f, 0.94f);
            bgCover.raycastTarget = false;

            var title = MakeText(stagePanel.transform, font, 52, TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0, -60), Color.white);
            title.text = "SELECT ENVIRONMENT & ROAD TYPE";

            var cardGo = new GameObject("StageCard");
            cardGo.transform.SetParent(stagePanel.transform, false);
            var crt = cardGo.AddComponent<RectTransform>();
            crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.65f);
            crt.anchoredPosition = Vector2.zero;
            crt.sizeDelta = new Vector2(400, 280);
            stagePreviewImg = cardGo.AddComponent<Image>();
            stagePreviewImg.raycastTarget = false;

            stageTitleText = MakeText(stagePanel.transform, font, 40, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.44f), Vector2.zero, new Color(1f, 0.85f, 0.2f));
            stageDescText = MakeText(stagePanel.transform, font, 28, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.36f), Vector2.zero, Color.white);

            Sprite prevSp = Resources.Load<Sprite>("RoadBuilder/UI/Btn_Arrow_Prev");
            Sprite nextSp = Resources.Load<Sprite>("RoadBuilder/UI/Btn_Arrow_Next");

            CreateNamedButton(stagePanel.transform, "Btn_Prev", prevSp, font, "", 0, new Vector2(0.5f, 0.65f), new Vector2(-280, 0), new Vector2(150, 68), OnStagePrev);
            CreateNamedButton(stagePanel.transform, "Btn_Next", nextSp, font, "", 0, new Vector2(0.5f, 0.65f), new Vector2(280, 0), new Vector2(150, 68), OnStageNext);

            Sprite yellowBtnSp = Resources.Load<Sprite>("RoadBuilder/Art/Btn_UpgradeYellow");
            CreateTexturedButton(stagePanel.transform, yellowBtnSp, font, "CHANGE ROAD MATERIAL", 26, new Vector2(0.5f, 0.22f), Vector2.zero, new Vector2(420, 75), OnClickChangeRoadStyle);

            Sprite blueBtnSp = Resources.Load<Sprite>("RoadBuilder/Art/Btn_MenuBlue");
            CreateNamedButton(stagePanel.transform, "Btn_Back", blueBtnSp, font, "< BACK", 28, new Vector2(0.5f, 0.10f), Vector2.zero, new Vector2(240, 70), OnClickBackToHome);
        }

        private void CycleTheme(int dir)
        {
            int total = System.Enum.GetValues(typeof(EnvironmentTheme)).Length;
            SelectedTheme = (EnvironmentTheme)(((int)SelectedTheme + dir + total) % total);
            _background.ApplyTheme(SelectedTheme);
            if (SoundManager.Instance) SoundManager.Instance.PlayClick();
            SavePlayerData();
            UpdateStageUI();
        }

        private void CycleRoadStyle()
        {
            int total = System.Enum.GetValues(typeof(RoadStyle)).Length;
            SelectedRoadStyle = (RoadStyle)(((int)SelectedRoadStyle + 1) % total);
            _road.SetRoadStyle(SelectedRoadStyle);
            if (SoundManager.Instance) SoundManager.Instance.PlayUpgrade();
            SavePlayerData();
            UpdateStageUI();
        }

        private void UpdateStageUI()
        {
            string cardPath = "RoadBuilder/Art/Stage_Country";
            string title = "COUNTRYSIDE HILLS";
            string desc = "Lush green rolling hills with smooth tarmac highway.";

            if (SelectedTheme == EnvironmentTheme.Desert)
            {
                cardPath = "RoadBuilder/Art/Stage_Desert";
                title = "DESERT DUNES";
                desc = "Blazing hot sand dunes with rustic dirt & sand tracks.";
            }
            else if (SelectedTheme == EnvironmentTheme.CyberNight)
            {
                cardPath = "RoadBuilder/Art/Stage_Cyber";
                title = "CYBER NIGHT 2099";
                desc = "Synthwave neon landscape with glowing energy tracks.";
            }

            if (stagePreviewImg) stagePreviewImg.sprite = Resources.Load<Sprite>(cardPath);
            if (stageTitleText) stageTitleText.text = title;
            if (stageDescText) stageDescText.text = $"{desc}\nActive Road: <color=#00FFFF>[{SelectedRoadStyle}]</color>";
        }

        // ------------------ 3. GARAGE & VEHICLE UPGRADES ------------------
        private void BuildGaragePanel(Transform parent, Font font)
        {
            garagePanel = new GameObject("Panel_Garage");
            garagePanel.transform.SetParent(parent, false);
            FillRect(garagePanel.AddComponent<RectTransform>());

            var bgCover = garagePanel.AddComponent<Image>();
            bgCover.color = new Color(0.06f, 0.08f, 0.14f, 0.94f);
            bgCover.raycastTarget = false;

            var title = MakeText(garagePanel.transform, font, 52, TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0, -50), Color.white);
            title.text = "GARAGE & VEHICLE TUNING";

            garageCarNameText = MakeText(garagePanel.transform, font, 40, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.90f), Vector2.zero, new Color(1f, 0.85f, 0.2f));

            Sprite prevSp = Resources.Load<Sprite>("RoadBuilder/UI/Btn_Arrow_Prev");
            Sprite nextSp = Resources.Load<Sprite>("RoadBuilder/UI/Btn_Arrow_Next");

            CreateNamedButton(garagePanel.transform, "Btn_Prev", prevSp, font, "", 0, new Vector2(0.5f, 0.90f), new Vector2(-360, 0), new Vector2(140, 64), OnGarageVehiclePrev);
            CreateNamedButton(garagePanel.transform, "Btn_Next", nextSp, font, "", 0, new Vector2(0.5f, 0.90f), new Vector2(360, 0), new Vector2(140, 64), OnGarageVehicleNext);

            Sprite greenBtnSp = Resources.Load<Sprite>("RoadBuilder/Art/Btn_RaceGreen");
            var unlockBtn = CreateNamedButton(garagePanel.transform, "Btn_Action", greenBtnSp, font, "SELECT VEHICLE", 28, new Vector2(0.5f, 0.81f), Vector2.zero, new Vector2(320, 75), HandleVehicleSelectionAction);
            garageUnlockBtnText = unlockBtn.GetComponentInChildren<Text>();

            BuildUpgradeRow(garagePanel.transform, font, "ENGINE (SPEED)", 0, out engineValText, out engineCostText, () => UpgradeStat(0));
            BuildUpgradeRow(garagePanel.transform, font, "SUSPENSION (GRIP)", 1, out suspValText, out suspCostText, () => UpgradeStat(1));
            BuildUpgradeRow(garagePanel.transform, font, "INK TANK (ROAD)", 2, out inkValText, out inkCostText, () => UpgradeStat(2));

            Sprite blueBtnSp = Resources.Load<Sprite>("RoadBuilder/Art/Btn_MenuBlue");
            CreateNamedButton(garagePanel.transform, "Btn_Back", blueBtnSp, font, "< BACK", 28, new Vector2(0.5f, 0.10f), Vector2.zero, new Vector2(240, 70), OnClickBackToHome);
        }

        private void BuildUpgradeRow(Transform parent, Font font, string label, int index, out Text valTxt, out Text costTxt, UnityEngine.Events.UnityAction onUpgrade)
        {
            float y = -190f - (index * 95f);
            var rowGo = new GameObject("UpgradeRow_" + label);
            rowGo.transform.SetParent(parent, false);
            var rt = rowGo.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0, y);
            rt.sizeDelta = new Vector2(900, 80);

            var bg = rowGo.AddComponent<Image>();
            bg.sprite = Resources.Load<Sprite>("RoadBuilder/Art/StatBarBg");
            bg.type = Image.Type.Sliced;
            bg.color = Color.white;
            bg.raycastTarget = false;

            var title = MakeText(rowGo.transform, font, 28, TextAnchor.MiddleLeft, new Vector2(0, 0.5f), new Vector2(30, 0), Color.white);
            title.text = label;

            valTxt = MakeText(rowGo.transform, font, 26, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(20, 0), new Color(0.2f, 0.95f, 1f));

            Sprite yellowBtnSp = Resources.Load<Sprite>("RoadBuilder/Art/Btn_UpgradeYellow");
            var btn = CreateNamedButton(rowGo.transform, "Btn_Upgrade_" + index, yellowBtnSp, font, "UPGRADE", 22, new Vector2(1, 0.5f), new Vector2(-20, 0), new Vector2(220, 58), onUpgrade);
            costTxt = btn.GetComponentInChildren<Text>();
        }

        private void CycleGarageVehicle(int dir)
        {
            int total = System.Enum.GetValues(typeof(VehicleType)).Length;
            SelectedVehicle = (VehicleType)(((int)SelectedVehicle + dir + total) % total);
            if (SoundManager.Instance) SoundManager.Instance.PlayClick();
            SetupShowcaseCar();
            UpdateGarageUI();
        }

        private void HandleVehicleSelectionAction()
        {
            if (IsCurrentVehicleUnlocked())
            {
                if (SoundManager.Instance) SoundManager.Instance.PlayUpgrade();
                SavePlayerData();
            }
            else
            {
                int cost = GetVehicleCost(SelectedVehicle);
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
            UpdateGarageUI();
        }

        private bool IsCurrentVehicleUnlocked()
        {
            if (SelectedVehicle == VehicleType.ClassicRed) return true;
            if (SelectedVehicle == VehicleType.SportCyan) return sportUnlocked;
            if (SelectedVehicle == VehicleType.MonsterTruck) return truckUnlocked;
            return false;
        }

        private int GetVehicleCost(VehicleType type)
        {
            if (type == VehicleType.SportCyan) return 200;
            if (type == VehicleType.MonsterTruck) return 350;
            return 0;
        }

        private void UpgradeStat(int statIndex)
        {
            int currentLevel = (statIndex == 0) ? engineLevel : ((statIndex == 1) ? suspensionLevel : inkTankLevel);
            if (currentLevel >= 5) return;

            int cost = currentLevel * 50;
            if (totalCoins >= cost)
            {
                totalCoins -= cost;
                if (statIndex == 0) engineLevel++;
                else if (statIndex == 1) suspensionLevel++;
                else if (statIndex == 2) inkTankLevel++;

                if (SoundManager.Instance) SoundManager.Instance.PlayUpgrade();
                SavePlayerData();
                UpdateGarageUI();
            }
            else
            {
                if (SoundManager.Instance) SoundManager.Instance.PlayCrash();
            }
        }

        private void UpdateGarageUI()
        {
            if (garageCarNameText)
            {
                string nameStr = "CLASSIC RUNNER";
                if (SelectedVehicle == VehicleType.SportCyan) nameStr = "CYAN HYPERCAR";
                else if (SelectedVehicle == VehicleType.MonsterTruck) nameStr = "MONSTER TRUCK";
                garageCarNameText.text = nameStr;
            }

            if (garageUnlockBtnText)
            {
                if (IsCurrentVehicleUnlocked())
                {
                    garageUnlockBtnText.text = "EQUIPPED ✓";
                }
                else
                {
                    garageUnlockBtnText.text = $"UNLOCK ({GetVehicleCost(SelectedVehicle)} COINS)";
                }
            }

            if (engineValText) engineValText.text = $"LVL {engineLevel}/5";
            if (suspValText) suspValText.text = $"LVL {suspensionLevel}/5";
            if (inkValText) inkValText.text = $"LVL {inkTankLevel}/5";

            if (engineCostText) engineCostText.text = (engineLevel >= 5) ? "MAX" : $"UPGRADE ({engineLevel * 50}c)";
            if (suspCostText) suspCostText.text = (suspensionLevel >= 5) ? "MAX" : $"UPGRADE ({suspensionLevel * 50}c)";
            if (inkCostText) inkCostText.text = (inkTankLevel >= 5) ? "MAX" : $"UPGRADE ({inkTankLevel * 50}c)";
        }

        // ------------------ 4. IN-GAME HUD ------------------
        private void BuildHUDPanel(Transform parent, Font font)
        {
            hudPanel = new GameObject("Panel_HUD");
            hudPanel.transform.SetParent(parent, false);
            FillRect(hudPanel.AddComponent<RectTransform>());

            hudDistanceText = MakeText(hudPanel.transform, font, 72, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(50, -35), Color.white);
            hudSpeedText = MakeText(hudPanel.transform, font, 42, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(50, -125), new Color(0.9f, 0.95f, 1f));
            hudCoinsText = MakeText(hudPanel.transform, font, 52, TextAnchor.UpperRight, new Vector2(1, 1), new Vector2(-50, -35), new Color(1f, 0.85f, 0.2f));

            var heartContainer = new GameObject("HeartsContainer");
            heartContainer.transform.SetParent(hudPanel.transform, false);
            var rt = heartContainer.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(150, -185);
            rt.sizeDelta = new Vector2(240, 60);

            hudHeartIcons = new GameObject[maxLives];
            Sprite heartSp = Resources.Load<Sprite>("RoadBuilder/Heart");
            for (int i = 0; i < maxLives; i++)
            {
                var h = new GameObject("Heart_" + i);
                h.transform.SetParent(heartContainer.transform, false);
                var hrt = h.AddComponent<RectTransform>();
                hrt.anchoredPosition = new Vector2((i - 1) * 65, 0);
                hrt.sizeDelta = new Vector2(55, 55);

                var img = h.AddComponent<Image>();
                img.sprite = heartSp;
                img.color = Color.white;
                img.raycastTarget = false;
                hudHeartIcons[i] = h;
            }

            Sprite frameSp = Resources.Load<Sprite>("RoadBuilder/UI/InkFrame");
            var barBg = new GameObject("InkBarBg");
            barBg.transform.SetParent(hudPanel.transform, false);
            var brt = barBg.AddComponent<RectTransform>();
            brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0f);
            brt.pivot = new Vector2(0.5f, 0.5f);
            brt.anchoredPosition = new Vector2(0, 50);
            brt.sizeDelta = new Vector2(620, 42);

            var bgImg = barBg.AddComponent<Image>();
            bgImg.sprite = frameSp;
            bgImg.type = Image.Type.Sliced;
            bgImg.color = Color.white;
            bgImg.raycastTarget = false;

            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(barBg.transform, false);
            hudInkFill = fillGo.AddComponent<RectTransform>();
            hudInkFill.anchorMin = new Vector2(0.015f, 0.15f);
            hudInkFill.anchorMax = new Vector2(0.985f, 0.85f);
            hudInkFill.sizeDelta = Vector2.zero;

            var fillImg = fillGo.AddComponent<Image>();
            fillImg.color = new Color(0.65f, 0.25f, 1.0f, 0.95f);
            fillImg.raycastTarget = false;

            hudInkText = MakeText(barBg.transform, font, 24, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, Color.white);
            hudInkText.text = "ROAD INK: 100%";

            Sprite yellowBtnSp = Resources.Load<Sprite>("RoadBuilder/Art/Btn_UpgradeYellow");
            CreateNamedButton(hudPanel.transform, "Btn_UNSTUCK", yellowBtnSp, font, "BOOST / HOP", 24, new Vector2(1f, 0f), new Vector2(-150, 55), new Vector2(230, 62), OnClickUnstuckHop);
        }

        private void UpdateHeartsUI()
        {
            if (hudHeartIcons == null) return;
            for (int i = 0; i < hudHeartIcons.Length; i++)
            {
                if (hudHeartIcons[i] != null)
                {
                    var img = hudHeartIcons[i].GetComponent<Image>();
                    img.color = (i < currentLives) ? Color.white : new Color(0.2f, 0.2f, 0.25f, 0.35f);
                }
            }
        }

        private void UpdateInkUI()
        {
            if (hudInkFill != null)
            {
                float ratio = Mathf.Clamp01(currentInk / maxInk);
                hudInkFill.anchorMax = new Vector2(0.015f + ratio * 0.97f, 0.85f);
            }
            if (hudInkText != null)
            {
                hudInkText.text = $"ROAD INK: {(currentInk / maxInk * 100f):0}%";
            }
        }

        // ------------------ 5. GAME OVER DIALOG ------------------
        private void BuildGameOverPanel(Transform parent, Font font)
        {
            gameOverPanel = new GameObject("Panel_GameOver");
            gameOverPanel.transform.SetParent(parent, false);
            FillRect(gameOverPanel.AddComponent<RectTransform>());

            var bgCover = gameOverPanel.AddComponent<Image>();
            bgCover.color = new Color(0.08f, 0.05f, 0.12f, 0.92f);
            bgCover.raycastTarget = false;

            goReasonText = MakeText(gameOverPanel.transform, font, 64, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.72f), Vector2.zero, new Color(1f, 0.3f, 0.2f));
            goDistanceText = MakeText(gameOverPanel.transform, font, 52, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.56f), Vector2.zero, Color.white);
            goCoinsText = MakeText(gameOverPanel.transform, font, 38, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.42f), Vector2.zero, new Color(1f, 0.85f, 0.2f));

            Sprite greenBtnSp = Resources.Load<Sprite>("RoadBuilder/Art/Btn_RaceGreen");
            CreateTexturedButton(gameOverPanel.transform, greenBtnSp, font, "PLAY AGAIN", 32, new Vector2(0.5f, 0.26f), new Vector2(-160, 0), new Vector2(280, 85), OnClickStartRace);

            Sprite blueBtnSp = Resources.Load<Sprite>("RoadBuilder/Art/Btn_MenuBlue");
            CreateNamedButton(gameOverPanel.transform, "Btn_MENU", blueBtnSp, font, "MENU", 30, new Vector2(0.5f, 0.26f), new Vector2(160, 0), new Vector2(240, 85), OnClickBackToHome);
        }

        private void UpdateGameOverUI() { }

        // ==================================================================== HELPERS
        private static void FillRect(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
        }

        public static GameObject CreateTexturedButton(Transform parent, Sprite sprite, Font font, string label, int fontSize, Vector2 anchor, Vector2 pos, Vector2 size, UnityEngine.Events.UnityAction onClick)
        {
            return CreateNamedButton(parent, "Btn_" + label, sprite, font, label, fontSize, anchor, pos, size, onClick);
        }

        public static GameObject CreateNamedButton(Transform parent, string objectName, Sprite sprite, Font font, string label, int fontSize, Vector2 anchor, Vector2 pos, Vector2 size, UnityEngine.Events.UnityAction onClick)
        {
            var btnGo = new GameObject(objectName);
            btnGo.transform.SetParent(parent, false);
            var rt = btnGo.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var img = btnGo.AddComponent<Image>();
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
            img.color = Color.white;
            img.raycastTarget = true; // Clickable!

            var btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);

            if (!string.IsNullOrEmpty(label))
            {
                var txt = MakeText(btnGo.transform, font, fontSize, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, 3), Color.white);
                txt.text = label;
                txt.raycastTarget = false; // Never block click!
            }

            return btnGo;
        }

        public static Text MakeText(Transform parent, Font font, int size, TextAnchor align, Vector2 anchor, Vector2 offset, Color color)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;

            // Set pivot to match alignment horizontally/vertically so edge-anchored texts never clip off-screen!
            float px = 0.5f;
            if (align == TextAnchor.UpperLeft || align == TextAnchor.MiddleLeft || align == TextAnchor.LowerLeft) px = 0f;
            else if (align == TextAnchor.UpperRight || align == TextAnchor.MiddleRight || align == TextAnchor.LowerRight) px = 1f;

            float py = 0.5f;
            if (align == TextAnchor.UpperLeft || align == TextAnchor.UpperCenter || align == TextAnchor.UpperRight) py = 1f;
            else if (align == TextAnchor.LowerLeft || align == TextAnchor.LowerCenter || align == TextAnchor.LowerRight) py = 0f;

            rt.pivot = new Vector2(px, py);
            rt.anchoredPosition = offset;
            rt.sizeDelta = new Vector2(900, 160);

            var t = go.AddComponent<Text>();
            t.font = font;
            t.fontSize = size;
            t.fontStyle = FontStyle.Bold;
            t.alignment = align;
            t.color = color;
            t.supportRichText = true;
            t.raycastTarget = false; // Never block clicks
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;

            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.8f);
            outline.effectDistance = new Vector2(3, -3);
            return t;
        }
    }
}
