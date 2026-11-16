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
    public sealed class AllocProbe : MonoBehaviour
    {
        public static string Result = "";
        public float phaseASeconds = 12f, phaseBSeconds = 6f, prebuildAhead = 140f;

        private IEnumerator Start()
        {
            var m = RoadGameManager.Instance;
            if (m == null) { Result = "no manager"; yield break; }
            if (m.DebugPlaying || m.DebugState != ScreenState.HomeDashboard) { m.GoHome(); yield return new WaitForSecondsRealtime(0.5f); }
            m.StartRace();
            float w = 0f; while (!m.DebugPlaying && w < 6f) { w += Time.unscaledDeltaTime; yield return null; }
            yield return new WaitForSecondsRealtime(1f);
            var drawer = gameObject.AddComponent<DebugAutoDraw>(); drawer.manager = m; drawer.chaseInk = false; drawer.lookAhead = prebuildAhead;
            using (var rec = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame"))
            {
                var a = new List<long>(1024);
                yield return Sample(rec, a, phaseASeconds);
                Destroy(drawer); yield return null;
                var b = new List<long>(1024);
                yield return Sample(rec, b, phaseBSeconds);
                m.PauseRun(); yield return null;
                var c = new List<long>(256);
                yield return Sample(rec, c, 3f);
                m.ResumeRun(); m.GoHome(); yield return new WaitForSecondsRealtime(1.5f);
                var d = new List<long>(256);
                yield return Sample(rec, d, 3f);
                var sb = new StringBuilder();
                sb.Append("A(driver drawing): ").Append(Stats(a)).Append(" | B(prebuilt road, no driver): ").Append(Stats(b)).Append(" | C(paused): ").Append(Stats(c)).Append(" | D(home): ").Append(Stats(d));
                sb.Append(" | dist=").Append(m.DebugDistance.ToString("0")).Append(" playing=").Append(m.DebugPlaying);
                Result = sb.ToString(); Debug.Log("[AllocProbe] " + Result);
            }
        }

        private static IEnumerator Sample(ProfilerRecorder rec, List<long> into, float seconds)
        {
            float end = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < end) { yield return null; if (rec.Valid) into.Add(rec.LastValue); }
        }

        private static string Stats(List<long> l)
        {
            if (l.Count == 0) return "no samples";
            var s = new List<long>(l); s.Sort(); long sum = 0; foreach (var v in l) sum += v;
            return "frames=" + l.Count + " mean=" + (sum / (double)l.Count / 1024.0).ToString("F2") + "KB median=" + (s[s.Count / 2] / 1024.0).ToString("F2") + "KB p95=" + (s[(int)(s.Count * 0.95)] / 1024.0).ToString("F2") + "KB max=" + (s[s.Count - 1] / 1024.0).ToString("F1") + "KB";
        }
    }
}
#endif
