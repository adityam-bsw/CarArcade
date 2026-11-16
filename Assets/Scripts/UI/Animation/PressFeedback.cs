using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RoadBuilderGame.UI
{
    /// <summary>
    /// Squash on press, spring back on release, and a global Pressed event (the click sound hooks into it).
    /// Add to any button. Responds on pointer-down so feedback lands inside 100 ms.
    /// </summary>
    public class PressFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        /// <summary>Raised when any feedback-enabled button is pressed down.</summary>
        public static event Action Pressed;

        private Selectable _selectable;
        private bool _down;
        private Vector3 _base = Vector3.one;      // the scale at rest (a portrait layout may rest above 1): press squashes from it and release returns to it

        private bool CanPress()
        {
            if (_selectable == null) _selectable = GetComponent<Selectable>();
            return _selectable == null || _selectable.IsInteractable();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!CanPress()) return;
            if (!_down) _base = transform.localScale;
            _down = true;
            Pressed?.Invoke();
            var theme = UIThemeManager.Current;
            UIAnim.Press(transform, _base * (theme != null ? theme.pressedScale : 0.92f));
        }

        public void OnPointerUp(PointerEventData eventData) { Release(); }
        public void OnPointerExit(PointerEventData eventData) { Release(); }

        private void Release()
        {
            if (!_down) return;
            _down = false;
            UIAnim.Release(transform, _base);
        }

        private void OnDisable()
        {
            if (_down) transform.localScale = _base;
            _down = false;
        }
    }
}
