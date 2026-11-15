using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoadBuilderGame.UI
{
    /// <summary>
    /// One item in a card rail or grid. Its look depends on what it shows: vehicles are license plates, worlds are
    /// postcards, roads are material tickets. The selected card lifts and gets a lit frame; locked vehicles carry a padlock tag.
    /// Views only: it displays an entry and reports taps.
    /// </summary>
    public class CollectionCard : MonoBehaviour
    {
        [SerializeField] private RectTransform visual;
        [SerializeField] private Image background;
        [SerializeField] private Image preview;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text badgeText;
        [SerializeField] private Image frame;
        [SerializeField] private Image lockTag;
        [SerializeField] private Button button;

        private const float Lift = 14f;
        private Tween _lift;

        public CatalogEntry Entry { get; private set; }
        /// <summary>The player's current pick. Shown as EQUIPPED.</summary>
        public bool Equipped { get; set; }

        // Card size and the boxes inside it, in card pixels from the top-left: picture, name, badge.
        private struct Layout { public Vector2 size; public Vector4 picture, name, badge; public SpriteSlot card, frameSlot; }

        private static Layout LayoutFor(CatalogKind kind)
        {
            switch (kind)
            {
                case CatalogKind.World:
                    return new Layout { size = new Vector2(340, 252), picture = new Vector4(22, 22, 318, 170), name = new Vector4(24, 176, 316, 212),
                        badge = new Vector4(24, 214, 316, 240), card = SpriteSlot.CardWorld, frameSlot = SpriteSlot.FrameWorld };
                case CatalogKind.Surface:
                    return new Layout { size = new Vector2(300, 208), picture = new Vector4(46, 20, 276, 126), name = new Vector4(46, 132, 280, 164),
                        badge = new Vector4(46, 166, 280, 192), card = SpriteSlot.CardRoad, frameSlot = SpriteSlot.FrameRoad };
                default:
                    return new Layout { size = new Vector2(300, 224), picture = new Vector4(26, 32, 274, 138), name = new Vector4(30, 150, 270, 184),
                        badge = new Vector4(30, 186, 270, 212), card = SpriteSlot.CardVehicle, frameSlot = SpriteSlot.FrameVehicle };
            }
        }

        /// <summary>Card size for a kind (the grid and rails use it for cell sizes).</summary>
        public static Vector2 SizeFor(CatalogKind kind) { return LayoutFor(kind).size; }

        private static void Box(RectTransform rt, Vector4 b)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(b.x, -b.y);
            rt.sizeDelta = new Vector2(b.z - b.x, b.w - b.y);
        }

        public void Bind(CatalogEntry entry, bool selected, Action<CollectionCard> onClick)
        {
            Entry = entry;
            var theme = UIThemeManager.Current;
            var l = LayoutFor(entry.kind);

            ((RectTransform)transform).sizeDelta = l.size;
            visual.sizeDelta = l.size;
            background.sprite = theme.GetSprite(l.card);
            background.color = Color.white;
            Box(preview.rectTransform, l.picture);
            Box(nameText.rectTransform, l.name);
            Box(badgeText.rectTransform, l.badge);

            var fs = theme.GetSprite(l.frameSlot);
            frame.sprite = fs;
            frame.rectTransform.anchorMin = frame.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            frame.rectTransform.sizeDelta = fs != null ? fs.rect.size : l.size;
            frame.rectTransform.anchoredPosition = Vector2.zero;

            preview.sprite = entry.PreviewFor(ThemeArtStyles.For(GameServices.Model.World));
            preview.preserveAspect = true;
            nameText.SetText(entry.displayName);
            nameText.color = theme.GetColor(ColorToken.TextOnAccent);

            Refresh();
            SetSelected(selected, false);
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick?.Invoke(this));
        }

        /// <summary>Badge, padlock tag and dimming from the entry's state.</summary>
        public void Refresh()
        {
            var entry = Entry;
            if (entry == null) return;
            var theme = UIThemeManager.Current;
            bool usable = entry.playable || entry.kind == CatalogKind.World;
            preview.color = usable ? Color.white : new Color(1f, 1f, 1f, 0.4f);

            bool locked = entry.kind == CatalogKind.Vehicle && usable && !GameServices.Model.IsVehicleUnlocked(entry);
            lockTag.gameObject.SetActive(locked);
            if (locked)
            {
                var sp = theme.GetSprite(SpriteSlot.LockTag);
                lockTag.sprite = sp;
                var rt = lockTag.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(1, 1);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(54, 70);
                rt.anchoredPosition = new Vector2(-18, -22);
                rt.localEulerAngles = new Vector3(0, 0, 9f);
            }

            if (!usable) Badge("COMING SOON", ColorToken.Danger);
            else if (Equipped) Badge("EQUIPPED", ColorToken.Success);
            else if (locked) { badgeText.SetText("{0} COINS", entry.cost); badgeText.color = theme.GetColor(ColorToken.TextOnAccent); }
            else if (entry.Placeholder) Badge("PLACEHOLDER", ColorToken.Danger);
            else if (!entry.Reviewed) Badge("UNREVIEWED", ColorToken.Secondary);
            else Badge(string.Empty, ColorToken.Secondary);
        }

        private void Badge(string text, ColorToken token)
        {
            badgeText.SetText(text);
            badgeText.color = UIThemeManager.Current.GetColor(token);
        }

        public void SetSelected(bool selected) { SetSelected(selected, true); }

        private void SetSelected(bool selected, bool animate)
        {
            frame.gameObject.SetActive(selected);
            _lift?.Kill();
            float y = selected ? Lift : 0f;
            if (animate && isActiveAndEnabled)
                _lift = visual.DOAnchorPosY(y, 0.22f).SetEase(Ease.OutBack).SetUpdate(true);
            else
                visual.anchoredPosition = new Vector2(0f, y);
        }

        private void OnDisable() { _lift?.Kill(); }
    }
}
