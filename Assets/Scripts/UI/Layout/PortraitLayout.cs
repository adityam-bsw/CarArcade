using System;
using UnityEngine;

namespace RoadBuilderGame.UI
{
    /// <summary>
    /// Portrait variant of a screen layout. The screen is authored for landscape; this lists the rects whose anchors, pivot, position, size and scale
    /// differ when the screen is portrait (DesignFrame decides). The landscape values are captured the first time it applies, so the prefab itself
    /// stays the landscape layout. No allocation after the first call; applied only when the screen shape flips.
    /// </summary>
    [DisallowMultipleComponent]
    public class PortraitLayout : MonoBehaviour
    {
        [Serializable]
        public struct Entry
        {
            public RectTransform target;
            public Vector2 anchorMin, anchorMax, pivot, position, size;
            public float scale;
        }

        [SerializeField] private Entry[] entries = Array.Empty<Entry>();
        private Entry[] _landscape;
        private bool _portrait, _known;

        public Entry[] Entries { get => entries; set => entries = value; }

        public void Apply(bool portrait)
        {
            if (_known && portrait == _portrait) return;
            if (_landscape == null)
            {
                _landscape = new Entry[entries.Length];
                for (int i = 0; i < entries.Length; i++)
                {
                    var t = entries[i].target; if (t == null) continue;
                    _landscape[i] = new Entry { target = t, anchorMin = t.anchorMin, anchorMax = t.anchorMax, pivot = t.pivot, position = t.anchoredPosition, size = t.sizeDelta, scale = t.localScale.x };
                }
            }
            _known = true; _portrait = portrait;
            var src = portrait ? entries : _landscape;
            for (int i = 0; i < src.Length; i++)
            {
                var e = src[i]; var t = e.target; if (t == null) continue;
                t.anchorMin = e.anchorMin; t.anchorMax = e.anchorMax; t.pivot = e.pivot;
                t.sizeDelta = e.size; t.anchoredPosition = e.position;
                t.localScale = new Vector3(e.scale, e.scale, 1f);
            }
        }
    }
}
