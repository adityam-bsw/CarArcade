using TMPro;
using UnityEngine;

namespace RoadBuilderGame.UI
{
    /// <summary>Styles a TMP_Text from the active theme: font, size, weight and colour come from a TextStyle.</summary>
    [ExecuteAlways]
    [RequireComponent(typeof(TMP_Text))]
    public class ThemedText : MonoBehaviour
    {
        public TextStyle style = TextStyle.Body;

        [Tooltip("Use a specific colour token instead of the style's own colour.")]
        public bool overrideColor;
        public ColorToken colorToken = ColorToken.TextPrimary;

        [Tooltip("Keep the size set on the TMP component (e.g. auto-sized or hand-tuned labels).")]
        public bool keepSize;

        private TMP_Text _text;

        private void OnEnable()
        {
            UIThemeManager.Changed += Apply;
            Apply();
        }

        private void OnDisable() { UIThemeManager.Changed -= Apply; }

        private void OnValidate() { Apply(); }

        public void Apply()
        {
            var theme = UIThemeManager.Current;
            if (theme == null) return;
            if (_text == null) _text = GetComponent<TMP_Text>();
            if (_text == null) return;

            var def = theme.GetTextStyle(style);
            var font = theme.GetFont(def.font);
            if (font != null) _text.font = font;
            if (!keepSize) _text.fontSize = def.size;
            _text.fontStyle = def.fontStyle;
            _text.color = theme.GetColor(overrideColor ? colorToken : def.color);
        }
    }
}
