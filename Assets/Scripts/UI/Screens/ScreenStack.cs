using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RoadBuilderGame.UI
{
    /// <summary>
    /// Navigation for screen prefabs: Push opens a screen on top, Pop closes it, Replace swaps the top.
    /// Screens are registered in the Inspector (prefabs) and instantiated on first use. Handles Android Back.
    /// </summary>
    public class ScreenStack : MonoBehaviour
    {
        [Tooltip("Screen prefabs this stack can open. Each prefab needs a UIScreen component.")]
        [SerializeField] private List<UIScreen> screenPrefabs = new List<UIScreen>();

        [Tooltip("Parent for instantiated screens. Defaults to this transform.")]
        [SerializeField] private RectTransform root;

        private readonly Dictionary<Type, UIScreen> _instances = new Dictionary<Type, UIScreen>();
        private readonly List<UIScreen> _stack = new List<UIScreen>();
        private bool _transitioning;
        // Screens that were covered by a full screen above them (OnHide was called). Restored properly when uncovered,
        // even if the user pops again before the exit animation finished.
        private readonly HashSet<UIScreen> _covered = new HashSet<UIScreen>();

        private void Awake()
        {
            // Screens that already sit in the scene under the root are used as they are (nothing is spawned for them).
            var parent = root != null ? root : (RectTransform)transform;
            foreach (Transform child in parent)
            {
                var screen = child.GetComponent<UIScreen>();
                if (screen == null || _instances.ContainsKey(screen.GetType())) continue;
                screen.Stack = this;
                screen.gameObject.SetActive(false);
                _instances[screen.GetType()] = screen;
            }
        }

        public UIScreen Top => _stack.Count > 0 ? _stack[_stack.Count - 1] : null;
        public int Depth => _stack.Count;

        /// <summary>Raised after the visible top screen changes (null when the stack is empty).</summary>
        public event Action<UIScreen> TopChanged;

        public T Push<T>(object args = null) where T : UIScreen
        {
            var screen = GetOrCreate<T>();
            if (screen == null) return null;
            if (_stack.Contains(screen)) return screen;

            var below = Top;
            _stack.Add(screen);
            screen.transform.SetAsLastSibling();
            screen.gameObject.SetActive(true);
            screen.OnShow(args);
            screen.PlayIn(null);

            // Full screens cover what is below; popups leave it visible.
            if (below != null && !screen.IsPopup) HideBelow(below);
            TopChanged?.Invoke(screen);
            return screen;
        }

        public void Pop()
        {
            if (_stack.Count == 0) return;
            var top = _stack[_stack.Count - 1];
            _stack.RemoveAt(_stack.Count - 1);
            _covered.Remove(top);
            top.OnHide();
            // Only switch it off if it was not pushed again while the exit animation ran (e.g. Reset to the same screen).
            top.PlayOut(() => { if (top != null && !_stack.Contains(top)) top.gameObject.SetActive(false); });

            // Re-reveal screens uncovered by this pop.
            for (int i = _stack.Count - 1; i >= 0; i--)
            {
                var s = _stack[i];
                // A covered screen unsubscribed in OnHide and faded out, so show it again properly: active, OnShow, PlayIn.
                if (_covered.Remove(s)) { s.gameObject.SetActive(true); s.OnShow(null); s.PlayIn(null); }
                else if (!s.gameObject.activeSelf) { s.gameObject.SetActive(true); s.PlayIn(null); }
                if (!s.IsPopup) break;
            }
            TopChanged?.Invoke(Top);
        }

        public T Replace<T>(object args = null) where T : UIScreen
        {
            if (_stack.Count > 0) Pop();
            return Push<T>(args);
        }

        /// <summary>Close everything, then open T. Use when switching root screens (e.g. Menu to Gameplay HUD).</summary>
        public T Reset<T>(object args = null) where T : UIScreen
        {
            while (_stack.Count > 0) Pop();
            return Push<T>(args);
        }

        private void HideBelow(UIScreen below)
        {
            _covered.Add(below);
            below.OnHide();
            below.PlayOut(() => { if (below != null && Top != below) below.gameObject.SetActive(false); });
        }

        private T GetOrCreate<T>() where T : UIScreen
        {
            if (_instances.TryGetValue(typeof(T), out var existing) && existing != null) return (T)existing;

            foreach (var prefab in screenPrefabs)
            {
                if (prefab is T)
                {
                    var parent = root != null ? root : (RectTransform)transform;
                    var inst = Instantiate(prefab, parent);
                    inst.name = prefab.name;
                    inst.Stack = this;
                    inst.gameObject.SetActive(false);
                    _instances[typeof(T)] = inst;
                    return (T)inst;
                }
            }
            Debug.LogError("[ScreenStack] No registered prefab for " + typeof(T).Name);
            return null;
        }

        private void Update()
        {
            // Android Back button arrives as Escape.
            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame)
            {
                var top = Top;
                if (top != null && top.BackCloses) top.OnBackPressed();
            }
        }
    }
}
