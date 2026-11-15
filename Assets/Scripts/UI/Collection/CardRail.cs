using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RoadBuilderGame.UI
{
    /// <summary>
    /// A horizontal, swipeable strip of catalogue cards (vehicles, worlds or roads). Views only: it shows entries, marks
    /// the equipped and focused ones, and reports taps. Playable items come first, items not playable yet last.
    /// </summary>
    public class CardRail : MonoBehaviour
    {
        [SerializeField] private ScrollRect scroll;
        [SerializeField] private RectTransform content;
        [SerializeField] private CollectionCard cardPrefab;

        private readonly List<CollectionCard> _cards = new List<CollectionCard>();

        public void Populate(CatalogKind kind, Action<CatalogEntry> onTap)
        {
            for (int i = content.childCount - 1; i >= 0; i--) Destroy(content.GetChild(i).gameObject);
            _cards.Clear();

            var catalog = AssetCatalog.Load();
            if (catalog == null) return;
            var items = catalog.Query(kind);
            var ordered = new List<CatalogEntry>();
            foreach (var e in items) if (e.playable) ordered.Add(e);
            foreach (var e in items) if (!e.playable) ordered.Add(e);

            foreach (var e in ordered)
            {
                var card = Instantiate(cardPrefab, content);
                card.Bind(e, false, c => onTap?.Invoke(c.Entry));
                _cards.Add(card);
            }
        }

        /// <summary>Mark the equipped card and the focused one (the one being looked at, which may not be owned yet).</summary>
        public void Refresh(string equippedId, CatalogEntry focus)
        {
            foreach (var c in _cards)
            {
                c.Equipped = c.Entry.id == equippedId;
                c.Refresh();
                c.SetSelected(c.Equipped || c.Entry == focus);
            }
        }

        /// <summary>Shake the card with this id (a locked item was tapped).</summary>
        public void ShakeLocked(string id)
        {
            foreach (var c in _cards) if (c.Entry.id == id) { c.ShakeLocked(); return; }
        }

        /// <summary>Scroll so the card with this id is in view (centred when there is room).</summary>
        public void Reveal(string id)
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            var viewport = (RectTransform)scroll.viewport;
            float overflow = content.rect.width - viewport.rect.width;
            if (overflow <= 0f) { scroll.horizontalNormalizedPosition = 0f; return; }
            foreach (var c in _cards)
            {
                if (c.Entry.id != id) continue;
                var rt = (RectTransform)c.transform;
                scroll.horizontalNormalizedPosition = Mathf.Clamp01((rt.anchoredPosition.x + rt.rect.width * 0.5f - viewport.rect.width * 0.5f) / overflow);
                return;
            }
        }
    }
}
