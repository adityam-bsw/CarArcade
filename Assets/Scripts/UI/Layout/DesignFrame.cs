using UnityEngine;

namespace RoadBuilderGame.UI
{
    /// <summary>
    /// The 1920x1080 design frame. Controls are authored at 1920x1080 and anchored to the edges/corners/centre of this frame, which
    /// fills the safe area: on wider screens edge items hug the real edges and centre items stay centred. If the safe area is
    /// smaller than 1920x1080 the frame scales down (and grows its rect) so nothing is cut off.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public class DesignFrame : MonoBehaviour
    {
        public static readonly Vector2 DesignSize = new Vector2(1920f, 1080f);

        private RectTransform _rt;
        private PortraitLayout _layout;
        private Vector2 _lastParent;

        /// <summary>Screens taller than this width-to-height ratio use their portrait layout (1920 wide, 3413 high minimum) instead of the 1920x1080 landscape one.</summary>
        public const float PortraitAspect = 0.85f;
        public static readonly Vector2 PortraitSize = new Vector2(1920f, 3413f);
        public bool IsPortrait { get; private set; }

        private void OnEnable() { Apply(true); }
        private void Update() { Apply(false); }

        private void Apply(bool force)
        {
            if (_rt == null) _rt = (RectTransform)transform;
            var parent = _rt.parent as RectTransform;
            if (parent == null) return;
            var size = parent.rect.size;
            if (size.x < 1f || size.y < 1f) return;          // not laid out yet (inactive or zero-size parent): keep the last good values
            if (!force && size == _lastParent) return;
            _lastParent = size;

            // Fill the whole parent (safe area) so edge anchors reach the real screen edges. If the parent is smaller than the
            // 1920x1080 design, scale down and enlarge the rect by 1/k so it still fills the parent exactly.
            IsPortrait = size.x < size.y * PortraitAspect;
            float k = IsPortrait ? Mathf.Min(size.x / PortraitSize.x, size.y / PortraitSize.y) : Mathf.Min(1f, size.x / DesignSize.x, size.y / DesignSize.y);
            _rt.anchorMin = _rt.anchorMax = new Vector2(0.5f, 0.5f);
            _rt.pivot = new Vector2(0.5f, 0.5f);
            _rt.sizeDelta = size / k;
            _rt.anchoredPosition = Vector2.zero;
            _rt.localScale = new Vector3(k, k, 1f);
            if (_layout == null) _layout = GetComponent<PortraitLayout>();
            if (_layout != null) _layout.Apply(IsPortrait);
        }
    }
}
