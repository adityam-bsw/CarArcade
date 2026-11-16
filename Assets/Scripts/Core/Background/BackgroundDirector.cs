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
        // The near art is solid only in its bottom ~8% (72 of 864 px). The ground fill reaches this far (in view-scale units)
        // above the bottom of the near layer, then fades out over FeatherUnits, so it never shows a hard edge or hides
        // the near art's own ground line.
        private const float FillSolidUnits = 1.1f, FeatherUnits = 0.6f;
        private const int OverlayOrder = -49, SunOrder = -48, CloudOrder = -47;   // sky < stars < sun/moon < clouds < far layer
        private const int MaxClouds = 4;
        private const float CloudPeriod = 64f;                // world units before a cloud repeats (at scale 1)

        private const float RegionMin = 450f, RegionRange = 350f; // metres before the next remix
        private const float FadeLength = 160f;                      // metres a cross-fade takes

        private sealed class Slot
        {
            public readonly SpriteRenderer[] layer = new SpriteRenderer[Layers];
            public SpriteRenderer fill;
            public SpriteRenderer feather;   // soft top edge of the fill (alpha 1 at the bottom, 0 at the top)
            public SpriteRenderer overlay;
            public SpriteRenderer sun;
            public readonly SpriteRenderer[] clouds = new SpriteRenderer[MaxClouds];
            public BackgroundRecipe recipe;
            public float alpha;
        }

        private Slot _a, _b;
        private System.Random _rng;
        private bool _anchorPending = true;
        private float _anchorX, _nextAt, _fadeStart;
        private bool _fading;
        private bool _locked;   // a single scene shown as drawn: no remix, no cross-fade
        private static Sprite _whiteSprite, _featherSprite;

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
            _locked = false;
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

        /// <summary>Show one scene exactly as drawn for the whole run (worlds that have no remix pool).</summary>
        public void ApplySkin(BackgroundSkin skin)
        {
            if (skin == null) return;
            var recipe = BackgroundRemixer.FromSkin(skin);
            _locked = true;
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
            if (_locked) return;
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

            // The whole layer height always equals the view height (one art pixel is viewH / sprite height world units).
            bool anyFading = _fading;

            for (int l = 0; l < Layers; l++)
            {
                var sr = slot.layer[l];
                var sprite = recipe.sprites[l];
                if (sr == null || sprite == null) continue;

                float nativeW = sprite.rect.width / sprite.pixelsPerUnit;
                float nativeH = sprite.rect.height / sprite.pixelsPerUnit;
                bool smooth = recipe.artStyle == ArtStyle.Smooth;
                // Pixel: layer height == view height. Smooth: layers keep their own height (the 864 px near layer is 1.5x the view)
                // at a scale set by the view, and sit on the bottom edge of the view.
                float s = smooth ? viewH / SmoothArt.BackgroundHeightUnits : viewH / nativeH;
                float pxWorld = viewH / sprite.rect.height; // one art pixel in world units
                float tileW = nativeW * s;

                int tiles = 2 * Mathf.CeilToInt(viewW / (2f * tileW)) + 3; // odd count, always wider than the view
                float scroll = recipe.scroll[l];
                float origin = c.x * (1f - scroll) + recipe.offset01[l] * tileW;
                float centreK = Mathf.Round((c.x - origin) / tileW);
                float x = origin + centreK * tileW;
                float y = ReferenceY + (c.y - ReferenceY) * StickY[l];
                if (smooth) y += (nativeH * s - viewH) * 0.5f;   // bottom-aligned

                // Pixel art snaps to the art-pixel grid so nothing shimmers while the camera moves.
                // Smooth art scrolls in float: snapping would make clean edges stutter.
                if (recipe.artStyle == ArtStyle.Pixel)
                {
                    x = Mathf.Round(x / pxWorld) * pxWorld;
                    y = Mathf.Round(y / pxWorld) * pxWorld;
                }

                sr.transform.position = new Vector3(x, y, 0f);
                sr.transform.localScale = new Vector3(s, s, 1f);
                { var ns = new Vector2(tiles * nativeW, nativeH); if (sr.size != ns) sr.size = ns; }   // a tiled sprite regenerates its mesh when size is written; only write on change

                // The sky of the outgoing slot stays opaque while the new one fades in, so no gap ever shows.
                float a = (l == 0 && slot == _a && anyFading) ? 1f : slot.alpha;
                var tint = recipe.tint[l];
                sr.color = new Color(tint.r, tint.g, tint.b, a);

                if (l == Layers - 1 && slot.fill != null)
                {
                    // Anchored to the true bottom of the near layer (y here already includes the bottom-align shift).
                    float nearBottom = smooth ? y - nativeH * s * 0.5f : y - viewH * 0.5f;
                    float solidTop = nearBottom + (smooth ? FillSolidUnits * s : 0f);
                    float h = solidTop - (nearBottom - 40f);
                    slot.fill.transform.position = new Vector3(c.x, solidTop - h * 0.5f, 0f);
                    slot.fill.transform.localScale = new Vector3(viewW + 40f, h, 1f);
                    var f = recipe.fill;
                    // The outgoing slot keeps an opaque fill (like its sky) so the fade never lets ghost props show through.
                    float fa = (slot == _a && anyFading) ? 1f : slot.alpha;
                    var fc = new Color(f.r * tint.r, f.g * tint.g, f.b * tint.b, fa);
                    slot.fill.color = fc;
                    if (slot.feather != null)
                    {
                        slot.feather.enabled = smooth;
                        float fh = FeatherUnits * s;
                        slot.feather.transform.position = new Vector3(c.x, solidTop, 0f);   // pivot at the bottom edge
                        slot.feather.transform.localScale = new Vector3((viewW + 40f) * 32f, fh, 1f);
                        slot.feather.color = fc;
                    }
                }
            }

            LayoutDecor(slot, c, viewH, viewW);
        }

        // Sun and clouds of a smooth scene. Cheap: at most five renderers, no allocations.
        private void LayoutDecor(Slot slot, Vector3 c, float viewH, float viewW)
        {
            var recipe = slot.recipe;
            float s = viewH / SmoothArt.BackgroundHeightUnits;
            var tint = recipe.tint[0];
            float a = slot.alpha;

            if (slot.overlay != null && recipe.overlay != null)
            {
                // Same geometry as the sky (scroll 0, bottom-aligned), tiled along X.
                var ov = recipe.overlay;
                float nativeW = ov.rect.width / ov.pixelsPerUnit, nativeH = ov.rect.height / ov.pixelsPerUnit;
                float tileW = nativeW * s;
                int tiles = 2 * Mathf.CeilToInt(viewW / (2f * tileW)) + 3;
                float cx = Mathf.Round(c.x / tileW) * tileW;
                float oy = ReferenceY + (c.y - ReferenceY) * StickY[0] + (nativeH * s - viewH) * 0.5f;
                slot.overlay.transform.position = new Vector3(cx, oy, 0f);
                slot.overlay.transform.localScale = new Vector3(s, s, 1f);
                { var os = new Vector2(tiles * nativeW, nativeH); if (slot.overlay.size != os) slot.overlay.size = os; }
                slot.overlay.color = new Color(tint.r, tint.g, tint.b, (slot == _a && _fading) ? 1f : a);
            }

            if (slot.sun != null && recipe.sun != null)
            {
                slot.sun.transform.position = new Vector3(c.x + viewW * 0.30f, c.y + viewH * 0.27f, 0f);
                slot.sun.transform.localScale = new Vector3(s, s, 1f);
                slot.sun.color = new Color(tint.r, tint.g, tint.b, a);
            }

            float o = c.x * (1f - recipe.cloudScroll);
            float period = CloudPeriod * s;
            for (int i = 0; i < MaxClouds; i++)
            {
                var sr = slot.clouds[i];
                if (sr == null || i >= recipe.clouds.Length || recipe.clouds[i] == null) continue;
                float seed = recipe.cloudSeed[i];
                // Spread the clouds evenly along the repeat, nudged by the seed, then take the copy nearest the camera.
                float off = ((i + 0.5f * seed) / MaxClouds) * period;
                float k = Mathf.Round((c.x - o - off) / period);
                float yFrac = 0.10f + 0.30f * (((i * 0.37f + seed * 0.6f) % 1f));
                float scale = s * (0.8f + 0.4f * ((seed * 3.7f) % 1f));
                sr.transform.position = new Vector3(o + off + k * period, c.y + viewH * yFrac, 0f);
                sr.transform.localScale = new Vector3(scale, scale, 1f);
                sr.color = new Color(tint.r, tint.g, tint.b, a * 0.95f);
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

            slot.overlay.sprite = recipe.overlay;
            slot.overlay.drawMode = SpriteDrawMode.Tiled;
            slot.overlay.tileMode = SpriteTileMode.Continuous;
            slot.overlay.enabled = recipe.overlay != null;
            slot.sun.sprite = recipe.sun;
            slot.sun.enabled = recipe.sun != null;
            for (int i = 0; i < MaxClouds; i++)
            {
                var cs = i < recipe.clouds.Length ? recipe.clouds[i] : null;
                slot.clouds[i].sprite = cs;
                slot.clouds[i].enabled = cs != null;
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

            slot.overlay = MakeDecor(root, "Overlay", OverlayOrder);
            slot.sun = MakeDecor(root, "Sun", SunOrder);
            for (int i = 0; i < MaxClouds; i++) slot.clouds[i] = MakeDecor(root, "Cloud" + i, CloudOrder);

            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(root, false);
            slot.fill = fillGo.AddComponent<SpriteRenderer>();
            slot.fill.sharedMaterial = SpriteFactory.SpriteMaterial;
            slot.fill.sprite = WhiteSprite();
            slot.fill.sortingOrder = FillOrder;

            var featherGo = new GameObject("FillFeather");
            featherGo.transform.SetParent(root, false);
            slot.feather = featherGo.AddComponent<SpriteRenderer>();
            slot.feather.sharedMaterial = SpriteFactory.SpriteMaterial;
            slot.feather.sprite = FeatherSprite();
            slot.feather.sortingOrder = FillOrder;
            return slot;
        }

        private static SpriteRenderer MakeDecor(Transform root, string name, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sharedMaterial = SpriteFactory.SpriteMaterial;
            sr.sortingOrder = order;
            sr.enabled = false;
            return sr;
        }

        // 2x64 white strip, alpha 1 at the bottom to 0 at the top; 1 unit tall, 1/32 unit wide, pivot bottom-centre.
        private static Sprite FeatherSprite()
        {
            if (_featherSprite != null) return _featherSprite;
            var tex = new Texture2D(2, 64, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[128];
            for (int y = 0; y < 64; y++)
            {
                float t = 1f - y / 63f;
                byte a = (byte)Mathf.RoundToInt(255f * t * t * (3f - 2f * t));
                px[y * 2] = px[y * 2 + 1] = new Color32(255, 255, 255, a);
            }
            tex.SetPixels32(px);
            tex.Apply();
            _featherSprite = Sprite.Create(tex, new Rect(0, 0, 2, 64), new Vector2(0.5f, 0f), 64f);
            return _featherSprite;
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
