#if UNITY_EDITOR
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace RoadBuilderGame.UI
{
    /// <summary>TEMPORARY QA12 probe (deleted after verification).</summary>
    public sealed class Qa12Tmp : MonoBehaviour
    {
        public static bool Done; public static string Log = "";
        private RenderTexture _rt; private int _w, _h; private Camera _cam; private readonly System.Collections.Generic.List<Canvas> _canvases = new System.Collections.Generic.List<Canvas>();
        public static void Run(string dir, string tag, string what, int w, int h) { Done = false; Log = ""; var go = new GameObject("Qa12Tmp"); var p = go.AddComponent<Qa12Tmp>(); p._w = w; p._h = h; p.StartCoroutine(p.Routine(dir, tag, what)); }
        private void BeginRt()
        {
            _cam = Camera.main; _rt = new RenderTexture(_w, _h, 24, RenderTextureFormat.ARGB32); _cam.targetTexture = _rt;
            foreach (var c in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay) { _canvases.Add(c); c.renderMode = RenderMode.ScreenSpaceCamera; c.worldCamera = _cam; c.planeDistance = 1f; }
        }
        private void EndRt()
        {
            foreach (var c in _canvases) if (c != null) { c.renderMode = RenderMode.ScreenSpaceOverlay; c.worldCamera = null; }
            if (_cam != null) _cam.targetTexture = null; if (_rt != null) _rt.Release();
        }
        private IEnumerator Real(float s) { float t = 0f; while (t < s) { t += Time.unscaledDeltaTime; yield return null; } }
        private IEnumerator Save(string path)
        {
            yield return new WaitForEndOfFrame();
            var prev = RenderTexture.active; RenderTexture.active = _rt;
            var t = new Texture2D(_w, _h, TextureFormat.RGB24, false); t.ReadPixels(new Rect(0, 0, _w, _h), 0, 0); t.Apply();
            RenderTexture.active = prev; System.IO.File.WriteAllBytes(path, t.EncodeToPNG()); Destroy(t);
        }
        private IEnumerator Shot(string dir, string name) { yield return Real(0.9f); yield return Save(System.IO.Path.Combine(dir, name + ".png")); }

        private IEnumerator Routine(string dir, string tag, string what)
        {
            BeginRt(); var m = RoadGameManager.Instance; var stack = FindFirstObjectByType<ScreenStack>(); var model = GameServices.Model;
            if (m.DebugState != ScreenState.HomeDashboard) { m.GoHome(); yield return Real(0.6f); }
            if (what.Contains("home")) yield return Shot(dir, "home" + tag);
            if (what.Contains("hub"))
            {
                for (int i = 0; i < 8; i++) { stack.Push<ProgressHubScreen>((HubPage)i); yield return Shot(dir, "hub" + i + tag); stack.Pop(); yield return Real(0.4f); }
            }
            if (what.Contains("rewards")) { m.OpenRewards(); yield return Shot(dir, "rewards" + tag); m.GoHome(); yield return Real(0.5f); }
            if (what.Contains("settings")) { stack.Push<SettingsScreen>(); yield return Shot(dir, "settings" + tag); stack.Pop(); yield return Real(0.4f); }
            if (what.Contains("coll")) { stack.Push<CollectionScreen>(CatalogKind.Vehicle); yield return Shot(dir, "coll" + tag); stack.Pop(); yield return Real(0.4f); }
            if (what.Contains("worlds")) { m.SwitchScreen(ScreenState.StageSelect); yield return Shot(dir, "worlds" + tag); m.GoHome(); yield return Real(0.5f); }
            if (what.Contains("garage"))
            {
                m.SwitchScreen(ScreenState.GarageMenu); yield return Shot(dir, "garage" + tag);
                var sc = FindFirstObjectByType<GarageScreen>(); var sr = sc.GetComponentInChildren<ScrollRect>();
                sr.verticalNormalizedPosition = 0f; yield return Real(0.3f); yield return Shot(dir, "garage_scrolled" + tag);
                m.GoHome(); yield return Real(0.5f);
            }
            if (what.Contains("pause")) { m.StartRace(); yield return Real(2.0f); m.PauseRun(); yield return Shot(dir, "pause" + tag); m.ResumeRun(); yield return Real(0.3f); yield return Shot(dir, "run" + tag); model.Toast = "NEW BEST!  +100 COINS"; model.ToastId++; yield return Real(0.6f); yield return Save(System.IO.Path.Combine(dir, "toast" + tag + ".png")); m.CheatJump(1000f); yield return Real(0.8f); yield return Save(System.IO.Path.Combine(dir, "banner" + tag + ".png")); m.DebugKill(); yield return Real(1f); m.GoHome(); yield return Real(0.5f); }
            if (what.Contains("result"))
            {
                m.StartRace(); yield return Real(1.5f); m.CheatJump(3000f); yield return Real(0.5f); m.DebugKill(); yield return Real(2.5f);
                yield return Shot(dir, "result" + tag); m.GoHome(); yield return Real(0.5f);
            }
            EndRt(); Done = true;
        }
    }
}
#endif
