#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RoadBuilderGame
{
    /// <summary>QA4 verification probe (Editor and development builds only). Drives the real run and logs measurements with the prefix [Qa4].</summary>
    public sealed class Qa4Probe : MonoBehaviour
    {
        public static readonly List<string> Lines = new List<string>();
        public static bool Done;
        private RoadGameManager _m;
        public static string ShotDir = "";

        public static void Run(string what, string shotDir)
        {
            Lines.Clear(); Done = false; ShotDir = shotDir;
            var go = new GameObject("Qa4Probe"); var p = go.AddComponent<Qa4Probe>();
            p.StartCoroutine(p.Routine(what));
        }

        private void Log(string s) { Lines.Add(s); Debug.Log("[Qa4] " + s); }
        private IEnumerator Seconds(float s) { float t = 0f; while (t < s) { t += Time.deltaTime; yield return null; } }
        private IEnumerator Real(float s) { float t = 0f; while (t < s) { t += Time.unscaledDeltaTime; yield return null; } }
        private IEnumerator NewRun()
        {
            if (_m.DebugPlaying || _m.DebugState != ScreenState.HomeDashboard) { _m.GoHome(); yield return Seconds(0.3f); }
            _m.StartRace();
            float t = 0f; while (!_m.DebugPlaying && t < 6f) { t += Time.unscaledDeltaTime; yield return null; }
            yield return Seconds(0.5f);
        }
        private void Shot(string name) { if (!string.IsNullOrEmpty(ShotDir)) ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(ShotDir, name + ".png")); }
        private StageEventDefinition Def(string id) { foreach (var d in _m.DebugEndlessLadder.eventPool) if (d != null && d.id == id) return d; return null; }
        private IEnumerator WaitActive(string id, float max)
        {
            float t = 0f;
            while (t < max)
            {
                var a = _m.DebugStage.Director.Plan.Events;
                for (int i = 0; i < a.Count; i++) if (a[i].Id == id && a[i].Phase == StagePhase.Active) yield break;
                t += Time.unscaledDeltaTime; yield return null;
            }
            Log("TIMEOUT waiting for " + id);
        }

        private IEnumerator Routine(string what)
        {
            _m = RoadGameManager.Instance;
            if (what.Contains("props")) yield return Props();
            if (what.Contains("gameover")) yield return GameOver();
            if (what.Contains("mud")) yield return Mud();
            if (what.Contains("wheels")) yield return Wheels();
            if (what.Contains("alloc")) yield return Alloc();
            if (what.Contains("lava")) yield return Lava();
            if (what.Contains("select")) yield return Select();
            if (_m.DebugState != ScreenState.HomeDashboard) _m.GoHome();
            Done = true; Log("DONE " + what);
        }

        private IEnumerator Props()
        {
            foreach (var id in new[] { "EVT-FLAPPY", "EVT-LAVA", "EVT-LASER", "EVT-TRAFFIC", "EVT-DOUBLE" })
            {
                Log("==== " + id);
                yield return NewRun();
                var d = Def(id); float start = 400f;
                _m.DebugInjectEvent(d, start); _m.DebugJumpToStage(start - 95f);
                yield return WaitActive(id, 25f);
                for (int k = 0; k < 8; k++)
                {
                    yield return Seconds(0.75f);
                    Log(id + " t=" + (k * 0.75f + 0.75f).ToString("0.0") + " carX=" + _m.DebugCar.Body.position.x.ToString("0.0") + " " + (_m.DebugWorld2 != null ? _m.DebugWorld2.PropReport(Camera.main) : ""));
                    if (k == 1 || k == 4) { Shot("b8_" + id + "_" + k); yield return null; yield return null; }
                }
            }
        }

        private IEnumerator GameOver()
        {
            yield return NewRun(); yield return Seconds(3f);
            var car = _m.DebugCar; var body = car.Body;
            Log("before kill: v=" + body.linearVelocity.x.ToString("0.0") + " x=" + body.position.x.ToString("0.0") + " simulated=" + body.simulated);
            _m.DebugKill(); yield return Seconds(0.2f);
            float x0 = body.position.x; var d0 = _m.DebugDistance;
            yield return Seconds(1.5f);
            Log("after kill (1.5 s later): state=" + _m.DebugState + " v=" + body.linearVelocity.x.ToString("0.0") + " dx=" + (body.position.x - x0).ToString("0.00") + " simulated=" + body.simulated + " MotorOn=" + car.MotorOn + " distanceChanged=" + (_m.DebugDistance != d0) + " timeScale=" + Time.timeScale);
            Shot("b2_gameover"); yield return null; yield return null;
            // pause is a no-op at game over; revive via the offer path
            GameServices.Actions.WatchOffer(AdPlacement.Revive); yield return Seconds(1.2f);
            Log("after revive offer: state=" + _m.DebugState + " carNull=" + (_m.DebugCar == null) + (_m.DebugCar != null ? " simulated=" + _m.DebugCar.Body.simulated + " v=" + _m.DebugCar.Body.linearVelocity.x.ToString("0.0") + " lives=" + _m.DebugLives : ""));
            if (_m.DebugPlaying) { yield return Seconds(2f); Log("revived car drives: x=" + _m.DebugCar.Body.position.x.ToString("0.0") + " v=" + _m.DebugCar.Body.linearVelocity.x.ToString("0.0")); }
            // quit from a fresh game over
            _m.DebugKill(); yield return Seconds(0.3f); _m.GoHome(); yield return Seconds(0.5f);
            Log("quit to home: state=" + _m.DebugState);
            yield return NewRun(); yield return Seconds(2f);
            _m.PauseRun(); yield return Real(0.5f); Log("paused: timeScale=" + Time.timeScale); _m.ResumeRun(); yield return Seconds(1f);
            Log("resumed: v=" + _m.DebugCar.Body.linearVelocity.x.ToString("0.0"));
        }

        private IEnumerator Mud()
        {
            int before = GameServices.Model.NoticeId;
            GameServices.Actions.SelectCatalogItem(CatalogKind.Surface, "surface-mud");
            Log("select mud: notice='" + GameServices.Model.Notice + "' (changed=" + (GameServices.Model.NoticeId != before) + ") selected=" + GameServices.Model.SelectedSurfaceId);
            var drawer = gameObject.AddComponent<DebugAutoDraw>(); drawer.manager = _m;
            yield return NewRun();
            float sum = 0f; int n = 0, zero = 0; float minT = 99f; float t = 0f;
            while (t < 25f) { yield return Seconds(0.25f); t += 0.25f; if (!_m.DebugPlaying) break; float tg = _m.DebugTargetSpeed; sum += _m.DebugCar.Body.linearVelocity.x; n++; if (tg < 0.1f) zero++; minT = Mathf.Min(minT, tg); }
            Log("mud run: seconds=" + t.ToString("0") + " avgVx=" + (n > 0 ? sum / n : 0f).ToString("0.00") + " samplesTargetZero=" + zero + "/" + n + " minTarget=" + minT.ToString("0.00") + " dist=" + _m.DebugDistance.ToString("0") + " playing=" + _m.DebugPlaying);
            Destroy(drawer);
            _m.GoHome(); yield return Seconds(0.3f);
            GameServices.Actions.SelectCatalogItem(CatalogKind.Surface, "surface-asphalt");
        }

        private IEnumerator Wheels()
        {
            foreach (var id in new[] { "vehicle-shopping-cart", "vehicle-bicycle", "vehicle-motorcycle", "vehicle-monster-truck" })
            {
                GameServices.Actions.SelectCatalogItem(CatalogKind.Vehicle, id);
                if (GameServices.Model.SelectedId(CatalogKind.Vehicle) != id) { Log(id + " not selectable: " + GameServices.Model.Notice); continue; }
                var drawer = gameObject.AddComponent<DebugAutoDraw>(); drawer.manager = _m;
                yield return NewRun(); yield return Seconds(6f);
                var car = _m.DebugCar; var js = car.Body.GetComponents<WheelJoint2D>(); var sb = new StringBuilder(id + " v=" + car.Body.linearVelocity.x.ToString("0.00") + " target=" + car.TargetSpeed.ToString("0.00"));
                for (int rep = 0; rep < 3; rep++)
                {
                    sb.Append(" |");
                    foreach (var j in js) { var w = j.connectedBody; var cc = w.GetComponent<CircleCollider2D>(); float r = cc.radius * w.transform.lossyScale.x; float om = w.angularVelocity * Mathf.Deg2Rad; sb.Append(" r=").Append(r.ToString("0.00")).Append(" om=").Append(om.ToString("0.0")).Append(" v/r=").Append((-car.Body.linearVelocity.x / r).ToString("0.0")); }
                    yield return Seconds(0.5f);
                }
                Log(sb.ToString());
                Destroy(drawer);
            }
            _m.GoHome();
        }

        private IEnumerator Frames(string label, int n)
        {
            using (var rec = Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Memory, "GC Allocated In Frame"))
            {
                yield return null; yield return null;
                var a = new long[n];
                for (int i = 0; i < n; i++) { yield return null; a[i] = rec.LastValue; }
                System.Array.Sort(a); long sum = 0; for (int i = 0; i < n; i++) sum += a[i];
                Log(label + " GC alloc/frame avg=" + (sum / (float)n).ToString("0") + " B median=" + a[n / 2] + " p90=" + a[(int)(n * 0.9f)] + " max=" + a[n - 1]);
            }
        }

        private IEnumerator Alloc()
        {
            Log("alloc probe: ProfilerRecorder 'GC Allocated In Frame' (includes editor overhead); QA runway, no auto draw");
            if (_m.DebugState != ScreenState.HomeDashboard) { _m.GoHome(); yield return Seconds(0.5f); }
            yield return Frames("idle home (baseline)", 180);
            yield return NewRun(); yield return Seconds(1f);
            yield return Frames("plain run, no event", 240);
            foreach (var id in new[] { "EVT-FLAPPY", "EVT-TRAFFIC", "EVT-LAVA", "EVT-DOUBLE", "EVT-RHYTHM", "EVT-CHAOS", "EVT-ICE" })
            {
                yield return NewRun();
                var d = Def(id); if (d == null) { Log(id + " missing"); continue; }
                _m.DebugInjectEvent(d, 400f); _m.DebugJumpToStage(305f);
                yield return WaitActive(id, 25f); yield return Seconds(1.5f);
                yield return Frames(id, 240);
            }
        }

        private IEnumerator Lava()
        {
            yield return NewRun();
            var d = Def("EVT-LAVA"); float start = 400f;
            int c0 = _m.DebugExtraCounter(ExtraGate.SurviveLava);
            _m.DebugInjectEvent(d, start); _m.DebugJumpToStage(start - 95f);
            yield return WaitActive("EVT-LAVA", 25f); yield return Seconds(4f);
            Shot("b8_lava_glow"); yield return null; yield return null;
            Log("lava gate counter before=" + c0 + " during=" + _m.DebugExtraCounter(ExtraGate.SurviveLava) + " lives=" + _m.DebugLives);
            _m.DebugJumpToStage(start + d.lengthM + 40f); yield return Seconds(1.0f);
            Log("lava gate counter after a QA jump past the event (the jump deliberately credits nothing)=" + _m.DebugExtraCounter(ExtraGate.SurviveLava));
            // natural end: the distance advances through the real director tick (no JumpTo), as when the car drives out of the event
            yield return NewRun();
            _m.DebugInjectEvent(d, start); _m.DebugJumpToStage(start - 95f);
            yield return WaitActive("EVT-LAVA", 25f); yield return Seconds(1f);
            _m.DebugSetDistanceAtLeast(start + d.lengthM + 6f); yield return Seconds(1.0f);
            Log("event length=" + d.lengthM + " lava gate counter after the director ended it on the normal tick=" + _m.DebugExtraCounter(ExtraGate.SurviveLava));
            Log("(unused)" + _m.DebugExtraCounter(ExtraGate.SurviveLava) + " lives=" + _m.DebugLives + " playing=" + _m.DebugPlaying);
        }

        private IEnumerator Select()
        {
            string[] ids = { "surface-ice", "surface-oil", "surface-spring-road", "surface-fragile-paper", "surface-glass", "surface-lava-river" };
            foreach (var id in ids)
            {
                string before = GameServices.Model.SelectedSurfaceId; int nb = GameServices.Model.NoticeId;
                GameServices.Actions.SelectCatalogItem(CatalogKind.Surface, id);
                Log("select " + id + ": selected " + before + " -> " + GameServices.Model.SelectedSurfaceId + " notice(changed=" + (GameServices.Model.NoticeId != nb) + ")='" + GameServices.Model.Notice + "'");
                yield return null;
            }
            foreach (var id in new[] { "vehicle-rocket-sled", "vehicle-hovercraft", "vehicle-motorcycle", "vehicle-semi-truck" })
            {
                string before = GameServices.Model.SelectedId(CatalogKind.Vehicle); int nb = GameServices.Model.NoticeId;
                GameServices.Actions.SelectCatalogItem(CatalogKind.Vehicle, id);
                Log("select " + id + ": selected " + before + " -> " + GameServices.Model.SelectedId(CatalogKind.Vehicle) + " notice(changed=" + (GameServices.Model.NoticeId != nb) + ")='" + GameServices.Model.Notice + "'");
                yield return null;
            }
        }
    }
}
#endif
