#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;

namespace RoadBuilderGame
{
    /// <summary>
    /// Development test driver: draws the road ahead of the car through the real brush (same smoothing, spacing and ink cost
    /// as a finger), rolling hills, and optionally bends the road up or down to reach the nearest ink pickup.
    /// Add it to any object in Play mode to play a run without a pointer. Not shipped in release builds.
    /// </summary>
    public sealed class DebugAutoDraw : MonoBehaviour
    {
        public RoadGameManager manager;
        public float lookAhead = 11f;
        public float step = 1.0f;
        public float maxSlope = 0.55f;
        public float hillScale = 1f;                      // 0 = dead flat road (continuity measurements)
        public bool chaseInk = true;
        public bool drawing;
        public float inkSpent, metresDrawn;

        // Frame-time probe: average and worst frame (ms) over the last 120 frames, and managed memory growth.
        private readonly float[] _dts = new float[120];
        private int _dtIndex, _dtCount;
        private long _memStart = -1, _allocLast;
        private readonly long[] _allocs = new long[120];
        /// <summary>Average managed allocation per frame (KB) over the last 120 frames.</summary>
        public float AllocKb { get { long t = 0, top = 0, top2 = 0, top3 = 0; for (int i = 0; i < _dtCount; i++) { long a = _allocs[i]; t += a; if (a > top) { top3 = top2; top2 = top; top = a; } else if (a > top2) { top3 = top2; top2 = a; } else if (a > top3) top3 = a; } t -= top + top2 + top3; return _dtCount > 3 ? t / (float)(_dtCount - 3) / 1024f : 0f; } }
        public float AvgMs { get { float t = 0; for (int i = 0; i < _dtCount; i++) t += _dts[i]; return _dtCount > 0 ? t / _dtCount * 1000f : 0f; } }
        public float MaxMs { get { float m = 0; for (int i = 0; i < _dtCount; i++) m = Mathf.Max(m, _dts[i]); return m * 1000f; } }
        public float MemMb => _memStart < 0 ? 0f : (System.GC.GetTotalMemory(false) - _memStart) / 1048576f;

        private Collectible[] _cache;
        private Vector2 _tip;
        private bool _hasTip;

        private void Update()
        {
            if (_memStart < 0) _memStart = System.GC.GetTotalMemory(false);
            long allocNow = System.GC.GetTotalMemory(false); _allocs[_dtIndex] = allocNow > _allocLast ? allocNow - _allocLast : 0; _allocLast = allocNow;
            _dts[_dtIndex] = Time.unscaledDeltaTime; _dtIndex = (_dtIndex + 1) % _dts.Length; _dtCount = Mathf.Min(_dtCount + 1, _dts.Length);
            if (Time.frameCount % 300 == 0 && manager != null && manager.DebugRoad != null)
                if (Time.frameCount % 300 == 0 && manager != null && manager.DebugRoad != null)
                Debug.Log("[Perf] avg " + AvgMs.ToString("F1") + " ms, worst " + MaxMs.ToString("F0") + " ms, alloc " + AllocKb.ToString("F1") + " KB/frame, segs " + manager.DebugRoad.SegmentCount + ", pts " + manager.DebugRoad.SampledPointCount + ", drawn " + metresDrawn.ToString("F0") + " m");
            if (manager == null || manager.DebugState != ScreenState.Playing) { drawing = false; _hasTip = false; return; }
            var brush = manager.DebugBrush; var car = manager.DebugCar; var road = manager.DebugRoad;
            if (brush == null || car == null || car.Body == null) return;

            if (!_hasTip)
            {
                float fx = road.FrontierMaxX;
                if (!road.TryGetRoadYAtX(fx - 0.2f, out float fy)) fy = 0f;
                _tip = new Vector2(fx, fy); _hasTip = true;
            }

            float limit = car.Body.position.x + lookAhead;
            if (_tip.x >= limit) return;                      // far enough ahead, wait for the car

            if (!brush.IsDrawing)
            {
                if (manager.currentInk < 4f) return;           // the brush refuses below this
                brush.Inject(_tip + new Vector2(0.05f, 0f), true, true, false);
                drawing = brush.IsDrawing;
                if (!drawing) return;
            }

            // next point: a step forward, y follows gentle hills, or climbs/dips towards the nearest ink pickup
            float x = _tip.x + step;
            float y = hillScale * (0.8f * Mathf.Sin(x * 0.18f) + 0.4f * Mathf.Sin(x * 0.07f));
            if (chaseInk)
            {
                Collectible best = null;
                if (Time.frameCount % 15 == 0 || _cache == null) _cache = FindObjectsByType<Collectible>(FindObjectsSortMode.None);
                foreach (var c in _cache)
                    if (c != null)
                    if (c.itemType == Collectible.Type.Ink && c.transform.position.x > _tip.x - 0.5f && (best == null || c.transform.position.x < best.transform.position.x)) best = c;
                if (best != null && best.transform.position.x - x < 14f) y = best.transform.position.y - 0.4f;
            }
            y = Mathf.Clamp(y, _tip.y - maxSlope * step, _tip.y + maxSlope * step);

            float before = manager.currentInk;
            var p = new Vector2(x, y);
            brush.Inject(p, false, true, false);                         // one pointer event per frame, like a finger
            float spent = before - manager.currentInk;
            if (spent > 0f) { inkSpent += spent; metresDrawn += spent / Mathf.Max(0.01f, manager.inkCostPerMeter); }
            drawing = brush.IsDrawing;
            _tip = drawing ? road.LastPoint : new Vector2(x, y);
            if (!brush.IsDrawing) _hasTip = false;
        }
    }

    /// <summary>
    /// Development test: plays every playable road for a few seconds each with DebugAutoDraw and logs "[Tour]" lines
    /// (speed, distance, coins, ink spent, lives, errors). Add next to DebugAutoDraw in Play mode.
    /// </summary>
    public sealed class DebugSurfaceTour : MonoBehaviour
    {
        public RoadGameManager manager;
        public DebugAutoDraw driver;
        public float secondsPerRoad = 12f;
        public string only = "";
        public string result = "";
        public bool finished;

        private System.Collections.IEnumerator Start()
        {
            yield return new WaitForSecondsRealtime(1f);
            var cat = AssetCatalog.Load();
            foreach (var e in cat.entries)
            {
                if (e.kind != CatalogKind.Surface || !e.playable) continue;
                if (only.Length > 0 && !only.Contains(e.id)) continue;
                manager.SelectCatalogItem(CatalogKind.Surface, e.id);
                manager.GoHome();
                yield return new WaitForSecondsRealtime(0.4f);
                manager.StartRace();
                yield return new WaitForSecondsRealtime(1.5f);
                float t0 = Time.realtimeSinceStartup; float speedSum = 0; int n = 0; float inkStart = driver.inkSpent, m0 = driver.metresDrawn;
                while (Time.realtimeSinceStartup - t0 < secondsPerRoad)
                {
                    if (manager.DebugCar != null && manager.DebugCar.Body != null) { speedSum += manager.DebugCar.Body.linearVelocity.x; n++; }
                    yield return null;
                }
                var car = manager.DebugCar;
                string line = "[Tour] " + e.id + " grip " + e.friction.ToString("0.00") + " | dist " + (car != null ? car.Body.position.x.ToString("F0") : "?") + " m | avg speed " + (n > 0 ? (speedSum / n).ToString("F1") : "?")
                    + " | target " + (car != null ? car.TargetSpeed.ToString("F1") : "?") + " | drawn " + (driver.metresDrawn - m0).ToString("F0") + " m, ink " + (driver.inkSpent - inkStart).ToString("F0")
                    + " | lives " + manager.currentLives + " | state " + manager.DebugState;
                result += line + "\n"; Debug.Log(line);
            }
            manager.GoHome();
            finished = true;
        }
    }
}
#endif
