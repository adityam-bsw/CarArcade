using UnityEngine;

namespace RoadBuilderGame
{
    /// <summary>
    /// Draws the endless background. Four looping pixel-art layers scroll at different speeds; every few hundred
    /// metres the scenery cross-fades to a new remix (see BackgroundRemixer), so a long run keeps changing.
    /// Looks only: it reads the camera and nothing else, and knows nothing about the road or the game rules.
    /// </summary>
    public class BackgroundDirector : MonoBehaviour
    {
        private const int Layers = BackgroundSkin.LayerCount;

        // World height the camera looked at when the layers were placed. Layers lag this by their stick factor
        // so a fall or a climb does not drag the whole backdrop with it.
        private const float ReferenceY = 2.3f;
        private static readonly float[] StickY = { 1f, 0.95f, 0.9f, 0.85f };
        private static readonly int[] Orders = { -50, -46, -42, -38 };
        private const int FillOrder = -39;

        private const float RegionMin = 450f, RegionRange = 350f; // metres before the next remix
        private const float FadeLength = 160f;                      // metres a cross-fade takes

        private sealed class Slot
        {
            public readonly SpriteRenderer[] layer = new SpriteRenderer[Layers];
            public SpriteRenderer fill;
            public BackgroundRecipe recipe;
            public float alpha;
        }

        private Slot _a, _b;
        private System.Random _rng;
        private bool _anchorPending = true;
        private float _anchorX, _nextAt, _fadeStart;
        private bool _fading;
        private static Sprite _whiteSprite;

        public EnvironmentTheme CurrentTheme { get; private set; } = EnvironmentTheme.Country;

        public void Init()
        {
            _a = CreateSlot("A", 0);
            _b = CreateSlot("B", 1);
            ApplyTheme(EnvironmentTheme.Country);
        }

        /// <summary>Start a fresh remix for this world. Called when the world is picked and at the start of every run.</summary>
        public void ApplyTheme(EnvironmentTheme theme)
        {
            CurrentTheme = theme;
            _rng = new System.Random(unchecked(System.Environment.TickCount * 397 + (int)theme));

            var recipe = BackgroundRemixer.Make(theme, _rng, null);
            if (recipe == null) return;

            Assign(_a, recipe);
            _a.alpha = 1f;
            _b.alpha = 0f;
            _b.recipe = null;
            _fading = false;
            _anchorPending = true;

            var cam = Camera.main;
            if (cam != null) cam.backgroundColor = recipe.fill;
        }

        public void Follow(Camera cam)
        {
            if (cam == null || _a == null || _a.recipe == null) return;

            float camX = cam.transform.position.x;
            if (_anchorPending)
            {
                _anchorX = camX;
                _nextAt = NextRegionLength();
                _anchorPending = false;
            }

            Advance(camX - _anchorX);
            Layout(_a, cam);
            if (_b.alpha > 0f && _b.recipe != null) Layout(_b, cam);
        }

        // ------------------------------------------------------------------ regions and fading
        private void Advance(float progress)
        {
            if (!_fading && progress >= _nextAt)
            {
                var next = BackgroundRemixer.Make(CurrentTheme, _rng, _a.recipe);
                if (next != null)
                {
                    Assign(_b, next);
                    _b.alpha = 0f;
                    _fading = true;
                    _fadeStart = progress;
                }
                else _nextAt = progress + NextRegionLength();
            }

            if (_fading)
            {
                float t = Mathf.Clamp01((progress - _fadeStart) / FadeLength);
                t = t * t * (3f - 2f * t);
                _b.alpha = t;
                _a.alpha = 1f - t;

                if (progress - _fadeStart >= FadeLength)
                {
                    var done = _a; _a = _b; _b = done;
                    _a.alpha = 1f;
                    _b.alpha = 0f;
                    _b.recipe = null;
                    _fading = false;
                    _nextAt = progress + NextRegionLength();
                }
            }
        }

        private float NextRegionLength() { return RegionMin + (float)_rng.NextDouble() * RegionRange; }

        // ------------------------------------------------------------------ drawing
        private void Layout(Slot slot, Camera cam)
        {
            var recipe = slot.recipe;
            Vector3 c = cam.transform.position;
            float viewH = cam.orthographicSize * 2f;
            float viewW = viewH * cam.aspect;

            // The whole layer height always equals the view height, so one art pixel is viewH/216 world units.
            bool anyFading = _fading;

            for (int l = 0; l < Layers; l++)
            {
                var sr = slot.layer[l];
                var sprite = recipe.sprites[l];
                if (sr == null || sprite == null) continue;

                float nativeW = sprite.rect.width / sprite.pixelsPerUnit;
                float nativeH = sprite.rect.height / sprite.pixelsPerUnit;
                float s = viewH / nativeH;                 // uniform scale: layer height == view height
                float pxWorld = viewH / sprite.rect.height; // one art pixel in world units
                float tileW = nativeW * s;

                int tiles = 2 * Mathf.CeilToInt(viewW / (2f * tileW)) + 3; // odd count, always wider than the view
                float scroll = recipe.scroll[l];
                float origin = c.x * (1f - scroll) + recipe.offset01[l] * tileW;
                float centreK = Mathf.Round((c.x - origin) / tileW);
                float x = origin + centreK * tileW;
                float y = ReferenceY + (c.y - ReferenceY) * StickY[l];

                // Snap to the art-pixel grid so nothing shimmers while the camera moves.
                x = Mathf.Round(x / pxWorld) * pxWorld;
                y = Mathf.Round(y / pxWorld) * pxWorld;

                sr.transform.position = new Vector3(x, y, 0f);
                sr.transform.localScale = new Vector3(s, s, 1f);
                sr.size = new Vector2(tiles * nativeW, nativeH);

                // The sky of the outgoing slot stays opaque while the new one fades in, so no gap ever shows.
                float a = (l == 0 && slot == _a && anyFading) ? 1f : slot.alpha;
                var tint = recipe.tint[l];
                sr.color = new Color(tint.r, tint.g, tint.b, a);

                if (l == Layers - 1 && slot.fill != null)
                {
                    float bottom = y - viewH * 0.5f;
                    slot.fill.transform.position = new Vector3(c.x, bottom - 20f, 0f);
                    slot.fill.transform.localScale = new Vector3(viewW + 40f, 40f, 1f);
                    var f = recipe.fill;
                    slot.fill.color = new Color(f.r * tint.r, f.g * tint.g, f.b * tint.b, slot.alpha);
                }
            }
        }

        private void Assign(Slot slot, BackgroundRecipe recipe)
        {
            slot.recipe = recipe;
            for (int l = 0; l < Layers; l++)
            {
                var sr = slot.layer[l];
                sr.sprite = recipe.sprites[l];
                sr.drawMode = SpriteDrawMode.Tiled;
                sr.tileMode = SpriteTileMode.Continuous;
                sr.flipX = recipe.flip[l];
            }
        }

        private Slot CreateSlot(string name, int slotIndex)
        {
            var slot = new Slot();
            var root = new GameObject("Background_" + name).transform;
            root.SetParent(transform, false);

            for (int l = 0; l < Layers; l++)
            {
                var go = new GameObject("Layer" + l);
                go.transform.SetParent(root, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sharedMaterial = SpriteFactory.SpriteMaterial;
                sr.sortingOrder = Orders[l];
                slot.layer[l] = sr;
            }

            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(root, false);
            slot.fill = fillGo.AddComponent<SpriteRenderer>();
            slot.fill.sharedMaterial = SpriteFactory.SpriteMaterial;
            slot.fill.sprite = WhiteSprite();
            slot.fill.sortingOrder = FillOrder;
            return slot;
        }

        private static Sprite WhiteSprite()
        {
            if (_whiteSprite != null) return _whiteSprite;
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var px = new Color32[16];
            for (int i = 0; i < px.Length; i++) px[i] = new Color32(255, 255, 255, 255);
            tex.SetPixels32(px);
            tex.Apply();
            _whiteSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
            return _whiteSprite;
        }
    }
}
