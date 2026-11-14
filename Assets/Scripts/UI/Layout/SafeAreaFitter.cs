using UnityEngine;

namespace RoadBuilderGame.UI
{
    /// <summary>
    /// Fits this RectTransform to Screen.safeArea so notches, camera cut-outs and gesture bars never cover UI.
    /// Put it on a full-stretch child of the Canvas and parent your screen content under it. Replaces the
    /// per-panel safe-area code that lived in RoadGameManager.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaFitter : MonoBehaviour
    {
        [SerializeField] private bool fitLeft = true, fitRight = true, fitTop = true, fitBottom = true;

        private RectTransform _rt;
        private Rect _lastSafe;
        private Vector2Int _lastScreen;

        private void OnEnable() { Apply(true); }

        private void Update() { Apply(false); }

        private void Apply(bool force)
        {
            if (_rt == null) _rt = (RectTransform)transform;
            if (Screen.width <= 0 || Screen.height <= 0) return;

            var safe = Screen.safeArea;
            var size = new Vector2Int(Screen.width, Screen.height);
            if (!force && safe == _lastSafe && size == _lastScreen) return;
            _lastSafe = safe;
            _lastScreen = size;

            Vector2 min = safe.position;
            Vector2 max = safe.position + safe.size;
            min.x = fitLeft ? min.x / size.x : 0f;
            max.x = fitRight ? max.x / size.x : 1f;
            min.y = fitBottom ? min.y / size.y : 0f;
            max.y = fitTop ? max.y / size.y : 1f;

            _rt.anchorMin = min;
            _rt.anchorMax = max;
            _rt.offsetMin = Vector2.zero;
            _rt.offsetMax = Vector2.zero;
        }
    }
}
