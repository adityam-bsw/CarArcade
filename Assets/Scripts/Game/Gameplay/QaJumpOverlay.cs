#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;
using UnityEngine.InputSystem;

namespace RoadBuilderGame
{
    /// <summary>
    /// QA only (Editor and development builds; compiled out of release): a small panel of the debug jump points of stage-events.md section 8.
    /// F9 or a tap on the DBG corner toggles it. IMGUI on purpose: it must exist without any UI prefab and never ships.
    /// </summary>
    public sealed class QaJumpOverlay : MonoBehaviour
    {
        private RoadGameManager _game;
        private bool _open;
        private static readonly float[] Points = { 0f, 220f, 480f, 640f, 710f, 1000f, 1350f, 1800f, 2000f };

        public void Init(RoadGameManager game) { _game = game; }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.f9Key.wasPressedThisFrame) _open = !_open;
        }

        private void OnGUI()
        {
            if (_game == null || !_game.DebugStageOn || _game.DebugState != ScreenState.Playing) return;
            if (GUI.Button(new Rect(Screen.width - 70, Screen.height * 0.45f, 64, 40), "DBG")) _open = !_open;
            if (!_open) return;
            float y = Screen.height * 0.45f + 46f;
            GUI.Label(new Rect(Screen.width - 200, y - 22, 190, 22), "Stage jump (" + Mathf.RoundToInt(_game.DebugDistance) + " m)");
            for (int i = 0; i < Points.Length; i++)
                if (GUI.Button(new Rect(Screen.width - 130, y + i * 38f, 124, 34), Points[i] + " m")) _game.DebugJumpToStage(Points[i]);
        }
    }
}
#endif
