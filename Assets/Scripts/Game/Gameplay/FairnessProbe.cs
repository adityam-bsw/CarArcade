#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RoadBuilderGame
{
    /// <summary>
    /// QA13 fairness sim (Editor and development builds only): a passive bot (no input, no god mode) or a hopping bot rides one event on the real manager for a number of
    /// seconds in back-to-back cycles (each cycle re-lays the QA runway and refills hearts), and counts hearts lost and deaths. Lines go to <see cref="Lines"/> and Debug.Log "[Fair]".
    /// </summary>
    public sealed class FairnessProbe : MonoBehaviour
    {
        public static readonly List<string> Lines = new List<string>();
        public static bool Done;
        private RoadGameManager _m;

        public static FairnessProbe Run(string ids, float seconds, bool hopBot)
        {
            Lines.Clear(); Done = false;
            var p = new GameObject("FairnessProbe").AddComponent<FairnessProbe>();
            p.StartCoroutine(p.Routine(ids.Split(','), seconds, hopBot));
            return p;
        }

        /// <summary>QA13: renders the game camera offscreen at landscape and portrait aspects at timed moments of an event (fade in, held, fade out) and writes PNGs into dir.</summary>
        public static FairnessProbe Shots(string id, string dir)
        {
            Lines.Clear(); Done = false;
            var p = new GameObject("FairnessProbe").AddComponent<FairnessProbe>();
            p.StartCoroutine(p.ShotRoutine(id, dir));
            return p;
        }

        private IEnumerator ShotRoutine(string id, string dir)
        {
            _m = RoadGameManager.Instance; var ladder = _m.DebugEndlessLadder;
            StageEventDefinition d = null; foreach (var e in ladder.eventPool) if (e != null && e.id == id) d = e;
            if (_m.DebugPlaying) { _m.GoHome(); yield return Seconds(0.3f); }
            _m.StartRace(); float w = 0f; while (!_m.DebugPlaying && w < 6f) { w += Time.unscaledDeltaTime; yield return null; }
            yield return Seconds(0.5f);
            _m.DebugInjectEvent(d, 400f); _m.DebugJumpToStage(400f - 12f);
            var cam = Camera.main; float[] times = { 0.4f, 1.0f, 2.0f, 4.0f };
            float t = 0f; int k = 0;
            while (k < times.Length) { t += Time.deltaTime; if (t >= times[k]) { yield return Shot(cam, dir, id + "_in" + k); k++; } else yield return null; }
            _m.DebugJumpToStage(400f + d.lengthM + 40f); t = 0f; k = 0; float[] outT = { 0.3f, 0.8f, 1.6f };
            while (k < outT.Length) { t += Time.deltaTime; if (t >= outT[k]) { yield return Shot(cam, dir, id + "_out" + k); k++; } else yield return null; }
            Log("shots done"); Done = true; Destroy(gameObject);
        }

        /// <summary>QA13-02: selects the cloud world (runtime only), starts a run, jumps onto the flat QA runway and captures the road behind the car as the cloud pieces dissolve.</summary>
        public static FairnessProbe CloudShots(string dir)
        {
            Lines.Clear(); Done = false;
            var p = new GameObject("FairnessProbe").AddComponent<FairnessProbe>();
            p.StartCoroutine(p.CloudRoutine(dir));
            return p;
        }

        private IEnumerator CloudRoutine(string dir)
        {
            _m = RoadGameManager.Instance;
            var f = typeof(RoadGameManager).GetField("_selWorldId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance); f.SetValue(_m, "world-cloud-kingdom");
            var f2 = typeof(RoadGameManager).GetField("_selSurfaceId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance); f2.SetValue(_m, "surface-cloud");
            if (_m.DebugPlaying) { _m.GoHome(); yield return Seconds(0.3f); }
            _m.StartRace(); float w = 0f; while (!_m.DebugPlaying && w < 6f) { w += Time.unscaledDeltaTime; yield return null; }
            yield return Seconds(0.5f);
            _m.DebugJumpToStage(60f);
            var cam = Camera.main;
            for (int k = 0; k < 4; k++)
            {
                yield return Seconds(1.2f); yield return Shot(cam, dir, "cloud_t" + k);
                var l = (System.Collections.IList)typeof(RoadBuilder).GetField("_segments", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(_m.DebugRoad);
                var sb = new System.Text.StringBuilder("car=" + _m.DebugCar.Body.position.x.ToString("0.0") + " segs: ");
                foreach (var o in l) { var ch = o.GetType().GetField("chunk").GetValue(o) as RoadChunk; sb.Append(o.GetType().GetField("minX").GetValue(o) + ".." + o.GetType().GetField("maxX").GetValue(o) + (ch != null ? "[a " + ch.FirstTint.a.ToString("0.00") + "/" + ch.LastTint.a.ToString("0.00") + "] " : "[null] ")); }
                Log(sb.ToString());
            }
            Log("cloud done lives=" + _m.DebugLives); Done = true; Destroy(gameObject);
        }

        /// <summary>Forces the camera aspect (and an offscreen target) for two frames so the real per-frame fog code runs at that aspect, then reads the render back.</summary>
        private IEnumerator Shot(Camera cam, string dir, string name)
        {
            var old = cam.targetTexture; int[][] sizes = { new[] { 900, 450 }, new[] { 397, 706 } }; string[] tag = { "land", "port" };
            for (int i = 0; i < 2; i++)
            {
                var rt = new RenderTexture(sizes[i][0], sizes[i][1], 24); cam.targetTexture = rt; cam.aspect = sizes[i][0] / (float)sizes[i][1];
                yield return null; yield return null;
                RenderTexture.active = rt; var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); tex.Apply();
                System.IO.File.WriteAllBytes(dir + "/" + name + "_" + tag[i] + ".png", tex.EncodeToPNG());
                Log(name + "_" + tag[i] + " ortho=" + cam.orthographicSize.ToString("0.00") + " aspect=" + cam.aspect.ToString("0.00"));
                RenderTexture.active = null; Destroy(tex); cam.targetTexture = old; cam.ResetAspect(); rt.Release(); Destroy(rt);
            }
        }

        private void Log(string s) { Lines.Add(s); Debug.Log("[Fair] " + s); }
        private IEnumerator Seconds(float s) { float t = 0f; while (t < s) { t += Time.deltaTime; yield return null; } }

        private IEnumerator Routine(string[] ids, float seconds, bool hop)
        {
            _m = RoadGameManager.Instance; var ladder = _m.DebugEndlessLadder;
            foreach (var id in ids)
            {
                StageEventDefinition d = null; foreach (var e in ladder.eventPool) if (e != null && e.id == id) d = e;
                if (d == null) { Log(id + " not found"); continue; }
                if (_m.DebugPlaying) { _m.GoHome(); yield return Seconds(0.3f); }
                _m.StartRace(); float w = 0f; while (!_m.DebugPlaying && w < 6f) { w += Time.unscaledDeltaTime; yield return null; }
                yield return Seconds(0.5f);
                float t = 0f, cycleT = 0f; int hearts = 0, deaths = 0, cycles = 0; float start = 400f;
                _m.DebugInjectEvent(d, start); _m.DebugJumpToStage(start - 15f); cycleT = 0f; cycles = 1;
                float nextHop = -1f;
                while (t < seconds)
                {
                    float dt = Time.deltaTime; t += dt; cycleT += dt;
                    if (!_m.DebugPlaying)
                    {
                        deaths++; hearts += 0; Log(id + " DEATH t=" + t.ToString("0.0"));
                        _m.GoHome(); yield return Seconds(0.3f); _m.StartRace(); w = 0f; while (!_m.DebugPlaying && w < 6f) { w += Time.unscaledDeltaTime; yield return null; }
                        yield return Seconds(0.3f); _m.DebugInjectEvent(d, start); _m.DebugJumpToStage(start - 15f); cycleT = 0f; cycles++; continue;
                    }
                    if (hop && id == "EVT-FLAPPY") HopBot();
                    if (_m.DebugDistance > start + 120f || cycleT > 40f)
                    {
                        int lost = 3 - _m.DebugLives; hearts += lost; if (lost > 0) Log(id + " cycle " + cycles + " lost " + lost + " t=" + t.ToString("0.0"));
                        _m.DebugInjectEvent(d, start); _m.DebugJumpToStage(start - 15f); cycleT = 0f; cycles++;
                    }
                    yield return null;
                }
                hearts += 3 - _m.DebugLives;
                Log(id + " SUMMARY hearts lost=" + hearts + " deaths=" + deaths + " over " + seconds + "s sim (" + cycles + " cycles, hopBot=" + hop + ")");
            }
            Log("ALLDONE"); Done = true; Destroy(gameObject);
        }

        private readonly HashSet<int> _done = new HashSet<int>();
        private float _lastHopCheckX = -1e9f;
        /// <summary>Hops once per gate at the middle of the launch window (computed from the real car speed and gap with the pure geometry).</summary>
        private void HopBot()
        {
            var car = _m.DebugCar; var w = _m.DebugWorld2; if (car == null || w == null) return;
            var root = GameObject.Find("StageWorld2"); if (root == null) return;
            float cx = car.Body.position.x, vx = Mathf.Max(3f, car.Body.linearVelocity.x);
            for (int i = 0; i < root.transform.childCount; i++)
            {
                var c = root.transform.GetChild(i); if (!c.gameObject.activeSelf || c.localScale.y < 7f) continue;      // ceiling columns are 8 m tall
                float dx = c.position.x - cx; if (dx < 0f || dx > 26f) continue;
                float first = -1f, last = -1f;
                for (float dd = 0.3f; dd < 12f; dd += 0.1f) if (FlappyTunnelGeometry.HopClears(vx, dd, 2.6f)) { if (first < 0f) first = dd; last = dd; }
                if (first < 0f) continue;
                float launch = (first + last) * 0.5f;
                if (dx <= launch) w.Hop();
            }
        }
    }
}
#endif
