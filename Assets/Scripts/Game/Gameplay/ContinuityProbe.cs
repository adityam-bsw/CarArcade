#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RoadBuilderGame
{
    /// <summary>
    /// Track-continuity probe (Editor and development builds only). Drives real runs with DebugAutoDraw and measures, per physics step,
    /// wheel contact, body vertical acceleration, per-frame y-velocity change, wheel lift-offs on gentle road and falls below the road. Logs "[Cont]" lines and writes a CSV.
    /// </summary>
    public sealed class ContinuityProbe : MonoBehaviour
    {
        public static readonly List<string> Lines = new List<string>();
        public static bool Done;
        private RoadGameManager _m;
        private string _csvPath;
        private readonly StringBuilder _csv = new StringBuilder();

        private sealed class Acc
        {
            public string label; public float startX;
            public int steps, grounded; public int[] wheelContact = new int[2];
            public float maxAy, maxAyX, maxDvyStep, maxDvyFrame, maxDvyFrameX;
            public int liftEvents, offRoad; public float firstLiftX = -1f, firstOffX = -1f;
            public float[] airT = new float[2];
            public bool[] liftFlag = new bool[2];
            // per-metre bucket
            public int bucket = -1, bSteps, bContact; public float bMaxAy, bMaxDvyFrame;
        }

        private Acc _a;
        private Rigidbody2D[] _wheels = new Rigidbody2D[2];
        private float[] _wheelR = new float[2];
        private float _prevVy, _prevFrameVy; private bool _havePrev;
        private readonly ContactPoint2D[] _buf = new ContactPoint2D[8];
        private RoadCar _car;

        public static float Hill = 1f;
        public static void Run(string vehicleIds, float metres)
        {
            Lines.Clear(); Done = false;
            var go = new GameObject("ContinuityProbe");
            var p = go.AddComponent<ContinuityProbe>();
            p.StartCoroutine(p.Routine(vehicleIds.Split(','), metres));
        }

        private void Log(string s) { Lines.Add(s); Debug.Log("[Cont] " + s); }

        private void Begin(string label)
        {
            _car = _m.DebugCar; _havePrev = false;
            _wheels[0] = _wheels[1] = null;
            var js = _car.Body.GetComponents<WheelJoint2D>();
            for (int i = 0; i < 2 && i < js.Length; i++)
            {
                _wheels[i] = js[i].connectedBody;
                var cc = _wheels[i].GetComponent<CircleCollider2D>();
                _wheelR[i] = cc.radius * _wheels[i].transform.lossyScale.x;
            }
            _a = new Acc { label = label, startX = _car.Body.position.x };
        }

        private string Summary()
        {
            var a = _a; if (a == null) return "";
            float n = Mathf.Max(1, a.steps);
            return a.label + " dist=" + (_car.Body.position.x - a.startX).ToString("0") + "m steps=" + a.steps
                + " contact(w0/w1)=" + (a.wheelContact[0] / n).ToString("0.000") + "/" + (a.wheelContact[1] / n).ToString("0.000")
                + " bothGrounded=" + (a.grounded / n).ToString("0.000")
                + " maxAy=" + a.maxAy.ToString("0.0") + "@" + a.maxAyX.ToString("0")
                + " maxDvyStep=" + a.maxDvyStep.ToString("0.00") + " maxDvyFrame=" + a.maxDvyFrame.ToString("0.00") + "@" + a.maxDvyFrameX.ToString("0")
                + " liftEvents(>0.1s gentle)=" + a.liftEvents + (a.firstLiftX >= 0 ? "@" + a.firstLiftX.ToString("0") : "")
                + " offRoad(>0.3m)=" + a.offRoad + (a.firstOffX >= 0 ? "@" + a.firstOffX.ToString("0") : "");
        }

        private void FlushBucket(string veh)
        {
            var a = _a; if (a == null || a.bucket < 0 || a.bSteps == 0) return;
            _csv.Append(veh).Append(',').Append(a.label).Append(',').Append(a.bucket).Append(',')
                .Append((a.bContact / (2f * a.bSteps)).ToString("0.000")).Append(',').Append(a.bMaxAy.ToString("0.00")).Append(',').Append(a.bMaxDvyFrame.ToString("0.000")).Append('\n');
            a.bSteps = 0; a.bContact = 0; a.bMaxAy = 0f; a.bMaxDvyFrame = 0f;
        }

        private string _veh = "";

        private void LogLift(Acc a, int wi, float wx, RoadBuilder road, float bx)
        {
            if (a.label != "drive" || a.liftEvents > 40) return;
            road.TryGetRoadYAtX(wx - 1f, out float ya); road.TryGetRoadYAtX(wx, out float yb); road.TryGetRoadYAtX(wx + 1f, out float yc);
            float best = 999f, bx0 = 0f;
            foreach (var c in FindObjectsByType<RoadChunk>(FindObjectsSortMode.None))
            {
                var cols = c.GetComponentsInChildren<Collider2D>();
                foreach (var col in cols) { float lo = col.bounds.min.x, hi = col.bounds.max.x; float d1 = Mathf.Abs(wx - lo), d2 = Mathf.Abs(wx - hi); if (d1 < best) { best = d1; bx0 = lo; } if (d2 < best) { best = d2; bx0 = hi; } }
            }
            Debug.Log("[Cont] lift w" + wi + " x=" + wx.ToString("0.0") + " slope=" + ((yc - ya) / 2f).ToString("0.00") + " curv=" + (ya - 2f * yb + yc).ToString("0.000") + " v=" + _car.Body.linearVelocity.ToString("0.0") + " nearestJoin=" + bx0.ToString("0.0") + " dist=" + best.ToString("0.0"));
        }

        private readonly string[] _ring = new string[40]; private int _ringI; private int _dumps;
        private void Ring(float x, int cnt, float vy, RoadBuilder road)
        {
            var sb = new StringBuilder();
            sb.Append("x=").Append(x.ToString("0.00")).Append(" c=").Append(cnt).Append(" vy=").Append(vy.ToString("0.000")).Append(" ang=").Append(_car.Body.rotation.ToString("0.0"));
            for (int i = 0; i < 2; i++) if (_wheels[i] != null) { road.TryGetRoadYAtX(_wheels[i].position.x, out float ry); sb.Append(" w").Append(i).Append("h=").Append((_wheels[i].position.y - ry - _wheelR[i]).ToString("0.000")).Append(" wvy=").Append(_wheels[i].linearVelocity.y.ToString("0.000")); }
            _ring[_ringI] = sb.ToString(); _ringI = (_ringI + 1) % _ring.Length;
        }
        private void DumpRing()
        {
            if (_dumps++ > 2) return;
            var sb = new StringBuilder("[Cont] RING (oldest first)\n");
            for (int k = 0; k < _ring.Length; k++) { var l = _ring[(_ringI + k) % _ring.Length]; if (l != null) sb.Append("  ").Append(l).Append('\n'); }
            Debug.Log(sb.ToString());
        }

        private bool Touching(Rigidbody2D w)
        {
            int n = w.GetContacts(_buf);
            for (int i = 0; i < n; i++) if (_buf[i].collider != null && !_buf[i].collider.isTrigger) return true;
            return false;
        }

        private void FixedUpdate()
        {
            if (_a == null || _car == null || _car.Body == null || _m == null || !_m.DebugPlaying) return;
            var body = _car.Body; var road = _m.DebugRoad;
            float vy = body.linearVelocity.y; float x = body.position.x;
            float dt = Time.fixedDeltaTime;
            if (_havePrev)
            {
                float dv = vy - _prevVy; float ay = Mathf.Abs(dv) / dt;
                var a = _a;
                a.steps++;
                int cnt = 0; bool all = true;
                for (int i = 0; i < 2; i++)
                {
                    if (_wheels[i] == null) continue;
                    bool t = Touching(_wheels[i]);
                    if (t) { a.wheelContact[i]++; cnt++; a.airT[i] = 0f; a.liftFlag[i] = false; }
                    else
                    {
                        all = false; a.airT[i] += dt;
                        if (a.airT[i] > 0.1f && !a.liftFlag[i])
                        {
                            // only count on flat / gentle road under the wheel (slope < 0.35)
                            float wx = _wheels[i].position.x;
                            if (road.TryGetRoadYAtX(wx - 0.5f, out float y0) && road.TryGetRoadYAtX(wx + 0.5f, out float y1) && Mathf.Abs(y1 - y0) < 0.35f)
                            { a.liftEvents++; a.liftFlag[i] = true; if (a.firstLiftX < 0) a.firstLiftX = wx; LogLift(a, i, wx, road, x); DumpRing(); }
                        }
                    }
                }
                if (all) a.grounded++;
                Ring(x, cnt, vy, road);
                if (ay > a.maxAy) { a.maxAy = ay; a.maxAyX = x; }
                if (Mathf.Abs(dv) > a.maxDvyStep) a.maxDvyStep = Mathf.Abs(dv);
                if (road.TryGetRoadYAtX(x, out float ry) && body.position.y < ry - 0.3f)
                { a.offRoad++; if (a.firstOffX < 0) a.firstOffX = x; }
                int b = Mathf.FloorToInt(x - a.startX);
                if (b != a.bucket) { FlushBucket(_veh); a.bucket = b; }
                a.bSteps++; a.bContact += cnt; if (ay > a.bMaxAy) a.bMaxAy = ay;
            }
            _prevVy = vy; _havePrev = true;
        }

        private void Update()
        {
            if (_a == null || _car == null || _car.Body == null || _m == null || !_m.DebugPlaying) return;
            float vy = _car.Body.linearVelocity.y;
            if (_prevFrameVy != float.MinValue)
            {
                float d = Mathf.Abs(vy - _prevFrameVy);
                if (d > _a.maxDvyFrame) { _a.maxDvyFrame = d; _a.maxDvyFrameX = _car.Body.position.x; }
                if (d > _a.bMaxDvyFrame) _a.bMaxDvyFrame = d;
            }
            _prevFrameVy = vy;
        }

        private IEnumerator Seconds(float s) { float t = 0f; while (t < s) { t += Time.deltaTime; yield return null; } }

        private IEnumerator Routine(string[] vehicles, float metres)
        {
            Application.runInBackground = true;
            _m = RoadGameManager.Instance;
            _csv.Append("vehicle,phase,metre,contactFrac,maxAy,maxDvyFrame\n");
            _csvPath = System.IO.Path.Combine(Application.temporaryCachePath, "continuity.csv");
            _prevFrameVy = float.MinValue;
            _m.CheatUnlockAll();
            _m.CheatSelect(CatalogKind.Surface, "surface-asphalt"); _m.CheatSelect(CatalogKind.World, "world-rolling-hills");
            foreach (var id in vehicles)
            {
                _veh = id;
                _m.CheatSelect(CatalogKind.Vehicle, id);
                if (_m.DebugState != ScreenState.HomeDashboard) { _m.GoHome(); yield return Seconds(0.4f); }
                _m.StartRace();
                float tw = 0f; while (!_m.DebugPlaying && tw < 6f) { tw += Time.unscaledDeltaTime; yield return null; }
                yield return Seconds(0.5f);
                if (!_m.DebugPlaying) { Log(id + " did not start"); continue; }
                _m.CheatGod = true; _m.CheatInfInk = true;
                var drawer = gameObject.AddComponent<DebugAutoDraw>(); drawer.manager = _m; drawer.chaseInk = false; drawer.hillScale = Hill;
                var js = _m.DebugCar.Body.GetComponents<WheelJoint2D>();
                var jd = new StringBuilder();
                foreach (var j in js) jd.Append(" [freq=" + j.suspension.frequency.ToString("0.0") + " damp=" + j.suspension.dampingRatio.ToString("0.00") + " motor=" + j.useMotor + "]");
                Log(id + " selected=" + _m.CheatVehicleId + " world=" + _m.CheatWorldId + " surface=" + _m.CheatSurfaceId + " joints" + jd + " fixedDt=" + Time.fixedDeltaTime + " interp=" + _m.DebugCar.Body.interpolation);

                // phase 1: drive `metres` on the drawn road
                Begin("drive");
                float t0 = Time.realtimeSinceStartup; float startX = _a.startX;
                while (_m.DebugPlaying && _car.Body.position.x - startX < metres && Time.realtimeSinceStartup - t0 < 400f) yield return null;
                FlushBucket(id); Log(id + " " + Summary()); _a = null;
                if (!_m.DebugPlaying) { Log(id + " run ended early"); Destroy(drawer); continue; }

                // phase 2: cheat jump 2000 m
                _m.CheatJump(2000f); _car = _m.DebugCar; Destroy(drawer); drawer = gameObject.AddComponent<DebugAutoDraw>(); drawer.manager = _m; drawer.chaseInk = false; drawer.hillScale = Hill;
                yield return Seconds(1.0f); Begin("jump2000"); yield return Seconds(12f);
                FlushBucket(id); Log(id + " " + Summary()); _a = null;

                // phase 3: respawn on a safety platform
                if (_m.DebugPlaying)
                {
                    var pos = _m.DebugRoad.CreateSafetyPlatformAndFindRespawn(_m.DebugCar.Body.position.x, 0f);
                    _m.DebugCar.RespawnSafely(pos); Destroy(drawer); drawer = gameObject.AddComponent<DebugAutoDraw>(); drawer.manager = _m; drawer.chaseInk = false; drawer.hillScale = Hill; Begin("respawn"); yield return Seconds(8f);
                    FlushBucket(id); Log(id + " " + Summary()); _a = null;
                }

                // phase 4: road eater trimming behind
                if (_m.DebugPlaying)
                {
                    _m.CheatEater(_m.CheatWorldId); Begin("eater"); yield return Seconds(14f);
                    FlushBucket(id); Log(id + " " + Summary()); _a = null;
                    _m.CheatStopHazards();
                }
                Destroy(drawer);
                _m.GoHome(); yield return Seconds(0.5f);
            }
            try { System.IO.File.WriteAllText("/private/tmp/claude-503/-Users-Aditya-unity-projects-CarArcade/b3ea3f91-1251-4ca5-991a-9457d21419c5/scratchpad/continuity.csv", _csv.ToString()); } catch { }
            Done = true; Log("DONE csv written");
        }

    }
}
#endif
