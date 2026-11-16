#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Unity.Profiling;
using UnityEngine;

namespace RoadBuilderGame
{
    /// <summary>
    /// Development probe: per-frame managed allocation (ProfilerRecorder "GC Allocated In Frame", exact bytes) in two phases of a real run.
    /// A = the test driver is drawing road ahead (road append + rebuild cost). B = the driver is gone and the car rolls on the prebuilt road
    /// with the stage director running, so only the game's own per-frame cost is left. Add to any object in Play mode; read <see cref="Result"/>.
    /// </summary>
    [DefaultExecutionOrder(-32000)]
    public sealed class AllocProbeEarly : MonoBehaviour
    {
        public static long T0; public static bool Collect; public static readonly List<long> Window = new List<long>(2048);
        public static ProfilerRecorder Rec;
        private void OnEnable() { Rec = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame"); }
        private void OnDisable() { Rec.Dispose(); }
        private void Update() { T0 = Rec.CurrentValue; }
    }

    /// <summary>Closes the window opened by <see cref="AllocProbeEarly"/>: bytes allocated by every Update and LateUpdate of the game in one frame (editor and rendering excluded).</summary>
    [DefaultExecutionOrder(32000)]
    public sealed class AllocProbeLate : MonoBehaviour
    {
        private void LateUpdate() { if (AllocProbeEarly.Collect) AllocProbeEarly.Window.Add(AllocProbeEarly.Rec.CurrentValue - AllocProbeEarly.T0); }
    }

    public sealed class AllocProbe : MonoBehaviour
    {
        public static string Result = "";
        public float phaseASeconds = 12f, phaseBSeconds = 6f, prebuildAhead = 140f;

        private IEnumerator Start()
        {
            gameObject.AddComponent<AllocProbeEarly>(); gameObject.AddComponent<AllocProbeLate>();
            var m = FindFirstObjectByType<RoadGameManager>();
            if (m == null) { Result = "no manager"; yield break; }
            if (m.DebugPlaying || m.DebugState != ScreenState.HomeDashboard) { m.GoHome(); yield return new WaitForSecondsRealtime(0.5f); }
            m.StartRace();
            float w = 0f; while (!m.DebugPlaying && w < 6f) { w += Time.unscaledDeltaTime; yield return null; }
            yield return new WaitForSecondsRealtime(1f);
            var drawer = gameObject.AddComponent<DebugAutoDraw>(); drawer.manager = m; drawer.chaseInk = false; drawer.lookAhead = prebuildAhead;
            using (var rec = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame"))
            {
                var a = new List<long>(1024);
                yield return Sample(rec, a, phaseASeconds); var wa = WindowOut;
                Destroy(drawer); yield return null;
                var b = new List<long>(1024);
                yield return Sample(rec, b, phaseBSeconds); var wb = WindowOut;
                m.PauseRun(); yield return null;
                var c = new List<long>(256);
                yield return Sample(rec, c, 3f); var wc = WindowOut;
                m.ResumeRun(); m.GoHome(); yield return new WaitForSecondsRealtime(1.5f);
                var d = new List<long>(256);
                yield return Sample(rec, d, 3f); var wd = WindowOut;
                var e = new List<long>(256);
                var canv = FindObjectsByType<Canvas>(FindObjectsSortMode.None); foreach (var cv in canv) cv.enabled = false;
                yield return null; yield return Sample(rec, e, 2f);
                var f = new List<long>(256);
                var mbs = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None); var was = new List<MonoBehaviour>();
                foreach (var mb in mbs) if (mb != null && mb != this && mb.enabled) { was.Add(mb); mb.enabled = false; }
                yield return null; yield return Sample(rec, f, 2f);
                foreach (var mb in was) if (mb != null) mb.enabled = true;
                foreach (var cv in canv) if (cv != null) cv.enabled = true;
                var sb = new StringBuilder();
                sb.Append("SCRIPTS-ONLY Update->LateUpdate window: A(driver drawing) ").Append(Stats(wa)).Append(" | B(prebuilt road, no driver) ").Append(Stats(wb)).Append(" | C(paused) ").Append(Stats(wc)).Append(" | D(home) ").Append(Stats(wd)).Append(" || WHOLE FRAME incl. editor: A ").Append(Stats(a)).Append(" | B ").Append(Stats(b)).Append(" | D ").Append(Stats(d)).Append(" | E(home, canvases off): ").Append(Stats(e)).Append(" | F(home, all behaviours off): ").Append(Stats(f));
                sb.Append(" | dist=").Append(m.DebugDistance.ToString("0")).Append(" playing=").Append(m.DebugPlaying);
                Result = sb.ToString(); Debug.Log("[AllocProbe] " + Result);
            }
        }

        private static IEnumerator Sample(ProfilerRecorder rec, List<long> into, float seconds)
        {
            float end = Time.realtimeSinceStartup + seconds;
            AllocProbeEarly.Window.Clear(); AllocProbeEarly.Collect = true;
            while (Time.realtimeSinceStartup < end) { yield return null; if (rec.Valid) into.Add(rec.LastValue); }
            AllocProbeEarly.Collect = false; WindowOut = new List<long>(AllocProbeEarly.Window);
        }

        public static List<long> WindowOut;
        private static string Stats(List<long> l)
        {
            if (l.Count == 0) return "no samples";
            var s = new List<long>(l); s.Sort(); long sum = 0; foreach (var v in l) sum += v;
            return "frames=" + l.Count + " mean=" + (sum / (double)l.Count / 1024.0).ToString("F2") + "KB median=" + (s[s.Count / 2] / 1024.0).ToString("F2") + "KB p95=" + (s[(int)(s.Count * 0.95)] / 1024.0).ToString("F2") + "KB max=" + (s[s.Count - 1] / 1024.0).ToString("F1") + "KB";
        }
    }
}
#endif
