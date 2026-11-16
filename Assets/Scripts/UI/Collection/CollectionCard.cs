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
        [Tooltip("Clips the picture (cover fit) so it never spills over the card art.")]
        [SerializeField] private RectTransform picture;
        [Tooltip("World postcards: the corner stamp is drawn again over the picture, clipped to the stamp, so the stamp sits on the photo.")]
        [SerializeField] private RectTransform stampClip;
        [SerializeField] private Image stampImage;
        [Tooltip("Sits on the blank postage stamp: a green tick for an unlocked world, so the corner never reads as an empty badge.")]
        [SerializeField] private Image stampIcon;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text badgeText;
        [SerializeField] private Image frame;
        [SerializeField] private Image lockTag;
        [SerializeField] private Button button;

        private const float Lift = 14f;
        private Tween _lift, _shake;
        private bool _selected;

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

        // The stamp on the postcard art (340x252): x 259..329, y 3..65 from the top-left.
        private static readonly Vector4 StampBox = new Vector4(258f, 0f, 334f, 66f);

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
            Box(picture != null ? picture : preview.rectTransform, l.picture);
            Box(nameText.rectTransform, l.name);
            Box(badgeText.rectTransform, l.badge);

            var fs = theme.GetSprite(l.frameSlot);
            frame.sprite = fs;
            frame.rectTransform.anchorMin = frame.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            frame.rectTransform.sizeDelta = fs != null ? fs.rect.size : l.size;
            frame.rectTransform.anchoredPosition = Vector2.zero;

            preview.sprite = entry.PreviewFor(ThemeArtStyles.For(GameServices.Model.World));
            FitPicture(entry, l);
            nameText.SetText(entry.displayName);
            nameText.color = theme.GetColor(ColorToken.TextOnAccent);

            Refresh();
            SetSelected(selected, false);
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick?.Invoke(this));
        }

        // Worlds fill the picture box (cover fit, the clip crops the overflow); cars and roads keep their whole shape.
        private void FitPicture(CatalogEntry entry, Layout l)
        {
            bool cover = entry.kind == CatalogKind.World && picture != null && preview.sprite != null;
            var rt = preview.rectTransform;
            if (picture != null)
            {
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
            }
            if (cover)
            {
                var box = new Vector2(l.picture.z - l.picture.x, l.picture.w - l.picture.y);
                var sr = preview.sprite.rect;
                float k = Mathf.Max(box.x / sr.width, box.y / sr.height);
                rt.sizeDelta = new Vector2(sr.width * k, sr.height * k);
                preview.preserveAspect = false;
            }
            else
            {
                if (picture != null) rt.sizeDelta = new Vector2(l.picture.z - l.picture.x, l.picture.w - l.picture.y);
                preview.preserveAspect = true;
            }

            bool stamp = entry.kind == CatalogKind.World && stampClip != null && stampImage != null;
            if (stampClip != null) stampClip.gameObject.SetActive(stamp);
            if (!stamp) return;
            Box(stampClip, StampBox);
            stampImage.sprite = background.sprite;
            var irt = stampImage.rectTransform;
            irt.anchorMin = irt.anchorMax = new Vector2(0, 1); irt.pivot = new Vector2(0, 1);
            irt.sizeDelta = l.size;
            irt.anchoredPosition = new Vector2(-StampBox.x, StampBox.y);
        }

        /// <summary>A short sideways shake with a red flash on the frame: "this one is locked".</summary>
        public void ShakeLocked()
        {
            _shake?.Kill();
            _lift?.Kill();
            float rest = _selected ? Lift : 0f;
            visual.anchoredPosition = new Vector2(0f, rest);
            frame.gameObject.SetActive(true);
            var danger = UIThemeManager.Current.GetColor(ColorToken.Danger);
            frame.color = danger;
            _shake = DOTween.Sequence().SetUpdate(true).SetLink(gameObject)
                .Append(visual.DOShakeAnchorPos(0.35f, new Vector2(16f, 0f), 24, 0f, false, true))
                .Append(frame.DOColor(Color.white, 0.25f))
                .OnComplete(() => { visual.anchoredPosition = new Vector2(0f, rest); frame.color = Color.white; frame.gameObject.SetActive(_selected); })
                .OnKill(() => { if (visual != null) visual.anchoredPosition = new Vector2(0f, _selected ? Lift : 0f); });
        }

        private static Sprite _tick;
        /// <summary>A white check mark drawn once in code (no asset); the card tints it with a theme token.</summary>
        private static Sprite TickSprite()
        {
            if (_tick != null) return _tick;
            const int n = 32; var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            {
                float d1 = SegDist(x, y, 6f, 16f, 13f, 9f), d2 = SegDist(x, y, 13f, 9f, 26f, 24f);
                float a = Mathf.Clamp01(3.2f - Mathf.Min(d1, d2));
                px[y * n + x] = new Color32(255, 255, 255, (byte)(255 * a));
            }
            t.SetPixels32(px); t.Apply(false, true);
            return _tick = Sprite.Create(t, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
        }

        private static float SegDist(float px, float py, float ax, float ay, float bx, float by)
        {
            float dx = bx - ax, dy = by - ay, t = Mathf.Clamp01(((px - ax) * dx + (py - ay) * dy) / (dx * dx + dy * dy));
            float cx = ax + t * dx - px, cy = ay + t * dy - py; return Mathf.Sqrt(cx * cx + cy * cy);
        }

        /// <summary>Badge, padlock tag and dimming from the entry's state.</summary>
        public void Refresh()
        {
            var entry = Entry;
            if (entry == null) return;
            var theme = UIThemeManager.Current;
            bool usable = entry.playable || entry.kind == CatalogKind.World;
            preview.color = usable ? Color.white : new Color(1f, 1f, 1f, 0.4f);

            string gate = GameServices.Model.LockLine(entry.id);
            bool locked = (entry.kind == CatalogKind.Vehicle && usable && !GameServices.Model.IsVehicleUnlocked(entry)) || ((entry.kind == CatalogKind.World || entry.kind == CatalogKind.Surface) && gate.Length > 0);
            lockTag.gameObject.SetActive(locked);
            if (stampIcon != null)
            {
                bool tick = entry.kind == CatalogKind.World && usable && !locked;
                stampIcon.enabled = tick;
                if (tick) { stampIcon.sprite = TickSprite(); stampIcon.color = theme.GetColor(ColorToken.Success); }
            }
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
            else if (locked && gate.Length > 0) { badgeText.SetText(gate); badgeText.color = theme.GetColor(ColorToken.Danger); }
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
            _selected = selected;
            _shake?.Kill();
            frame.color = Color.white;
            frame.gameObject.SetActive(selected);
            _lift?.Kill();
            float y = selected ? Lift : 0f;
            if (animate && isActiveAndEnabled)
                _lift = visual.DOAnchorPosY(y, 0.22f).SetEase(Ease.OutBack).SetUpdate(true);
            else
                visual.anchoredPosition = new Vector2(0f, y);
        }

        private void OnDisable() { _lift?.Kill(); _shake?.Kill(); }
    }
}
