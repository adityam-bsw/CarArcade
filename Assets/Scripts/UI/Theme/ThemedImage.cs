using UnityEngine;
using UnityEngine.UI;

namespace RoadBuilderGame.UI
{
    /// <summary>Styles an Image from the active theme: a colour token and an optional 9-sliced sprite slot.</summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Image))]
    public class ThemedImage : MonoBehaviour
    {
        public SpriteSlot slot = SpriteSlot.None;
        public ColorToken colorToken = ColorToken.Surface;

        [Tooltip("Tint the image with the colour token. Turn off for artwork that must keep its own colours.")]
        public bool tint = true;

        [Range(0f, 1f)] public float alpha = 1f;

        private Image _image;

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
            if (_image == null) _image = GetComponent<Image>();
            if (_image == null) return;

            if (slot != SpriteSlot.None)
            {
                var sprite = theme.GetSprite(slot);
                if (sprite != null)
                {
                    _image.sprite = sprite;
                    _image.type = sprite.border.sqrMagnitude > 0f ? Image.Type.Sliced : Image.Type.Simple;
                }
            }
            var c = tint ? theme.GetColor(colorToken) : Color.white;
            c.a *= alpha;
            _image.color = c;
        }
    }
}
