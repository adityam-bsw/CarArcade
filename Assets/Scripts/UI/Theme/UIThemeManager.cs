using System;
using UnityEngine;

namespace RoadBuilderGame.UI
{
    /// <summary>
    /// Holds the active <see cref="UITheme"/>. Themed components listen to <see cref="Changed"/> and restyle
    /// themselves. Falls back to Resources/UI/UITheme_Default so edit mode and a bare scene still work.
    /// </summary>
    public static class UIThemeManager
    {
        public const string DefaultThemeResource = "UI/UITheme_Default";

        private static UITheme _current;

        public static event Action Changed;

        public static UITheme Current
        {
            get
            {
                if (_current == null) _current = Resources.Load<UITheme>(DefaultThemeResource);
                return _current;
            }
        }

        public static void Set(UITheme theme)
        {
            if (theme == null || theme == _current) return;
            _current = theme;
            Changed?.Invoke();
        }

        /// <summary>Back to the default theme (e.g. leaving a mode that ships its own look).</summary>
        public static void ResetToDefault()
        {
            _current = Resources.Load<UITheme>(DefaultThemeResource);
            Changed?.Invoke();
        }

        internal static void NotifyThemeEdited(UITheme edited)
        {
            // Live preview while tweaking the asset in the Inspector.
            if (edited == _current) Changed?.Invoke();
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        private static void ClearOnReload() { _current = null; }
#endif
    }
}
