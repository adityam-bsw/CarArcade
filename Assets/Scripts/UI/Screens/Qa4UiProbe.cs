#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using UnityEngine;

namespace RoadBuilderGame.UI
{
    /// <summary>QA4 UI probe (Editor and development builds only): opens each hub tab, settings, pause, result (long run) and the boss HUD, saving a screenshot of each at the current Game view size.</summary>
    public sealed class Qa4UiProbe : MonoBehaviour
    {
        public static bool Done; public static string Log = "";

        private RenderTexture _rt; private int _w, _h; private Camera _cam; private readonly System.Collections.Generic.List<Canvas> _canvases = new System.Collections.Generic.List<Canvas>();

        /// <summary>Renders the world camera and every overlay canvas into a w x h texture (the Game view cannot be resized from script), so the screenshots are exactly that resolution.</summary>
        public static void Run(string dir, int w, int h)
        {
            Done = false; Log = "";
            var go = new GameObject("Qa4UiProbe");
            var p = go.AddComponent<Qa4UiProbe>(); p._w = w; p._h = h;
            p.StartCoroutine(p.Routine(dir, "_" + w + "x" + h));
        }

        /// <summary>A real RO-B0 fight (injected at 400 m on the QA runway): screenshot the edge meter at this resolution once the HUD reports it.</summary>
        public static void RunBoss(string dir, int w, int h)
        {
            Done = false; Log = "";
            var go = new GameObject("Qa4UiProbe"); var p = go.AddComponent<Qa4UiProbe>(); p._w = w; p._h = h;
            p.StartCoroutine(p.BossRoutine(dir, "_" + w + "x" + h));
        }

        private IEnumerator BossRoutine(string dir, string sfx)
        {
            var m = RoadGameManager.Instance; var model = GameServices.Model;
            if (m.DebugState != ScreenState.HomeDashboard) { m.GoHome(); yield return Real(0.6f); }
            m.StartRace(); yield return Real(1.5f);
            var def = Resources.Load<StageEventDefinition>("Stage/BOS-ROB0");
            m.DebugInjectEvent(def, 400f); m.DebugJumpToStage(400f - 60f);
            float t = 0f; while (!model.BossOn && t < 40f) { t += Time.unscaledDeltaTime; yield return null; }
            Log += "bossOn=" + model.BossOn + " after " + t.ToString("0.0") + "s tier=" + model.BossTier + " phase=" + model.BossPhase + " hits=" + model.BossHits + "/" + model.BossMaxHits + "\n";
            BeginRt(); yield return Real(1.0f);
            yield return Save(System.IO.Path.Combine(dir, "bossreal" + sfx + ".png"));
            EndRt(); m.GoHome(); Done = true;
        }

        private void BeginRt()
        {
            _cam = Camera.main; _rt = new RenderTexture(_w, _h, 24, RenderTextureFormat.ARGB32);
            _cam.targetTexture = _rt;
            foreach (var c in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay) { _canvases.Add(c); c.renderMode = RenderMode.ScreenSpaceCamera; c.worldCamera = _cam; c.planeDistance = 1f; }
        }

        private void EndRt()
        {
            foreach (var c in _canvases) if (c != null) { c.renderMode = RenderMode.ScreenSpaceOverlay; c.worldCamera = null; }
            if (_cam != null) _cam.targetTexture = null;
            if (_rt != null) _rt.Release();
        }

        private IEnumerator Save(string path)
        {
            yield return new WaitForEndOfFrame();
            var prev = RenderTexture.active; RenderTexture.active = _rt;
            var t = new Texture2D(_w, _h, TextureFormat.RGB24, false); t.ReadPixels(new Rect(0, 0, _w, _h), 0, 0); t.Apply();
            RenderTexture.active = prev;
            System.IO.File.WriteAllBytes(path, t.EncodeToPNG()); Destroy(t);
        }

        private IEnumerator Real(float s) { float t = 0f; while (t < s) { t += Time.unscaledDeltaTime; yield return null; } }
        private IEnumerator Shot(string dir, string name) { yield return Real(0.8f); yield return Save(System.IO.Path.Combine(dir, name + ".png")); }

        private IEnumerator Routine(string dir, string sfx)
        {
            var m = RoadGameManager.Instance; var stack = FindFirstObjectByType<ScreenStack>();
            BeginRt();
            if (m.DebugState != ScreenState.HomeDashboard) { m.GoHome(); yield return Real(0.6f); }
            yield return Shot(dir, "home" + sfx);
            string[] names = { "mastery", "journey", "daily", "weekly", "album", "shop", "roads", "scrap" };
            for (int i = 0; i < 8; i++)
            {
                stack.Push<ProgressHubScreen>((HubPage)i); yield return Shot(dir, "hub_" + names[i] + sfx);
                stack.Pop(); yield return Real(0.4f);
            }
            stack.Push<SettingsScreen>(); yield return Shot(dir, "settings" + sfx); stack.Pop(); yield return Real(0.4f);
            m.SwitchScreen(ScreenState.StageSelect); yield return Shot(dir, "worlds" + sfx);
            m.SwitchScreen(ScreenState.GarageMenu); yield return Shot(dir, "garage" + sfx);
            m.GoHome(); yield return Real(0.5f);
            // run: pause screen and boss HUD
            m.StartRace(); yield return Real(2.5f);
            m.PauseRun(); yield return Shot(dir, "pause" + sfx); m.ResumeRun(); yield return Real(0.4f);
            var model = GameServices.Model;
            model.BossOn = true; model.BossTier = "Scout"; model.BossPhase = 2; model.BossHits = 2; model.BossMaxHits = 3; model.BossOverload01 = 0.6f; model.RaiseHud();
            for (int i = 0; i < 40; i++) { model.BossOn = true; model.BossTier = "Scout"; model.BossPhase = 2; model.BossHits = 2; model.BossMaxHits = 3; model.BossOverload01 = 0.6f; model.RaiseHud(); yield return null; }
            yield return Save(System.IO.Path.Combine(dir, "bosshud" + sfx + ".png"));
            model.BossOn = false; model.RaiseHud();
            // result with a very long bonus list
            var lines = new System.Text.StringBuilder();
            for (int i = 0; i < 12; i++) lines.Append("CHEST ").Append(250 * (i + 1)).Append(" m  +12\nAIR TIME ").Append(i % 3 + 1).Append(".").Append(i).Append(" s  +").Append(3 + i % 4).Append("\n");
            lines.Append("RING  +5\nRING  +5\nESCAPED  +75\nPERFECT CLOSE!  +18");
            m.DebugKill(); yield return Real(0.6f); m.GoHome(); yield return Real(0.6f);
            var r = model.LastRun; r.bonusLines = lines.ToString(); r.gems = 9; r.coinsWon = 543; r.distance = 2310f; r.reason = "BOULDER BALL!"; model.LastRun = r;
            m.SwitchScreen(ScreenState.GameOver); yield return Real(2.0f); yield return Shot(dir, "result" + sfx);
            m.GoHome(); yield return Real(0.5f);
            EndRt(); Done = true;
        }
    }
}
#endif
