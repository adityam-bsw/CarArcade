using System.Collections.Generic;
using UnityEngine;

namespace RoadBuilderGame
{
    /// <summary>
    /// Spawns coins, ink canisters and hearts ahead of the car along a world-space wave, and removes
    /// them behind it. Extracted from RoadGameManager unchanged.
    /// </summary>
    public sealed class PickupSpawner
    {
        private readonly RoadBuilder _road;
        private readonly List<Collectible> _active = new List<Collectible>();
        private float _nextSpawnX = 14f;

        public PickupSpawner(RoadBuilder road)
        {
            _road = road;
        }

        /// <summary>Destroy every live pickup and restart spawning at <paramref name="firstSpawnX"/>.</summary>
        public void Reset(float firstSpawnX)
        {
            foreach (var col in _active)
                if (col != null) Object.Destroy(col.gameObject);
            _active.Clear();
            _nextSpawnX = firstSpawnX;
        }

        /// <summary>Set by the manager: how full the ink tank is (0 to 1) and whether a heart would help.</summary>
        public System.Func<float> Ink01 = () => 1f;
        public System.Func<bool> NeedsHeart = () => true;
        public System.Func<float> InkUnits = () => 100f;         // ink left in the tank (units)
        public System.Func<float> InkPerMeter = () => 1.2f;      // ink one metre of road costs
        public GameplayTuning Tuning = GameplayTuning.Load();

        private enum Shape { Arc, Line, Zigzag, Ring, Pile }
        private float _lastInkX = -100f;
        private static readonly System.Collections.Generic.Dictionary<string, Sprite> Sprites = new System.Collections.Generic.Dictionary<string, Sprite>();

        /// <summary>Art style of the world on screen. Smooth uses the smooth pickup sprites; pickups without one keep the original.</summary>
        public ArtStyle Art = ArtStyle.Pixel;

        private Sprite Icon(string name)
        {
            if (Art == ArtStyle.Smooth)
            {
                var smooth = SmoothSliceArt.Load();
                var ss = smooth != null ? smooth.Pickup(name) : null;
                if (ss != null) return ss;
            }
            if (!Sprites.TryGetValue(name, out var s) || s == null) Sprites[name] = s = Resources.Load<Sprite>("RoadBuilder/" + name);
            return s;
        }

        private float GroundY(float x)
        {
            // Smooth world-space wave for natural arcs, kept above any road already drawn at x.
            float waveY = 1.4f + Mathf.Sin(x * 0.14f) * 1.35f + Mathf.Cos(x * 0.06f) * 0.65f;
            if (_road.TryGetRoadYAtX(x, out float roadY)) waveY = Mathf.Max(waveY, roadY + 1.2f);
            return waveY;
        }

        private Collectible Place(Collectible.Type t, string sprite, float x, float y)
        {
            var go = new GameObject("Pickup_" + t);
            go.transform.position = new Vector3(x, y, 0f);
            var pickup = go.AddComponent<Collectible>();
            pickup.Init(t, Icon(sprite));
            _active.Add(pickup);
            return pickup;
        }

        private void CoinShape(Shape shape, float x)
        {
            switch (shape)
            {
                case Shape.Arc:      // a jump: coins rise and fall over a hump
                    for (int i = 0; i < 5; i++) { float cx = x + i * 1.5f; Place(Collectible.Type.Coin, "Coin", cx, GroundY(cx) + Mathf.Sin(i / 4f * Mathf.PI) * 1.3f); }
                    if (Random.value < Tuning.ringChancePerArc) { float rx = x + 3f; Place(Collectible.Type.Ring, "Pickup_Ring", rx, GroundY(rx) + 1.6f); }   // a boost ring above the apex
                    break;
                case Shape.Line:     // a safe path to follow
                    for (int i = 0; i < 7; i++) { float cx = x + i * 1.3f; Place(Collectible.Type.Coin, "Coin", cx, GroundY(cx) + 0.2f); }
                    break;
                case Shape.Zigzag:   // a skill test: steer up and down
                    for (int i = 0; i < 6; i++) { float cx = x + i * 1.4f; Place(Collectible.Type.Coin, "Coin", cx, GroundY(cx) + (i % 2 == 0 ? 0f : 1.3f)); }
                    break;
                case Shape.Ring:     // a ring around one point
                    { float cx = x + 2f, cy = GroundY(cx) + 1.4f; for (int i = 0; i < 8; i++) { float a = i * Mathf.PI * 2f / 8f; Place(Collectible.Type.Coin, "Coin", cx + Mathf.Cos(a) * 1.3f, cy + Mathf.Sin(a) * 1.1f); } }
                    break;
                default:             // a pile: two rows of three
                    for (int r = 0; r < 2; r++) for (int i = 0; i < 3; i++) { float cx = x + i * 0.9f; Place(Collectible.Type.Coin, "Coin", cx, GroundY(cx) + 0.2f + r * 0.8f); }
                    break;
            }
        }

        /// <summary>
        /// When ink runs low there must be a pickup the player can still reach: it sits just past the END OF THE DRAWN ROAD,
        /// at half the distance the remaining ink can still draw (never beyond inkRescueMaxMetres). If one already exists but
        /// lies too far for the ink left, it is pulled in. Runs every frame, so it follows the road as it grows.
        /// </summary>
        private void KeepInkReachable(float carX)
        {
            var t = Tuning;
            if (Ink01() >= t.inkRescueBelow) return;
            float tip = Mathf.Max(_road.FrontierMaxX, carX);
            float reach = Mathf.Max(0f, InkUnits() / Mathf.Max(0.1f, InkPerMeter()));       // metres of road the tank can still draw
            float gap = Mathf.Clamp(reach * 0.5f, 2.5f, t.inkRescueMaxMetres);
            float x = tip + gap;
            float y = _road.TryGetRoadYAtX(tip - 0.3f, out float tipY) ? tipY + 0.9f : GroundY(x);

            Collectible nearest = null;
            foreach (var c in _active)
                if (c != null && c.itemType == Collectible.Type.Ink && c.transform.position.x > carX - 0.5f
                    && (nearest == null || c.transform.position.x < nearest.transform.position.x)) nearest = c;

            if (nearest == null) { Place(Collectible.Type.Ink, "InkCanister", x, y); _lastInkX = x; return; }
            if (nearest.transform.position.x > tip + Mathf.Max(gap, reach * 0.8f) + 1f)
            {
                nearest.MoveTo(new Vector3(x, y, 0f));                                         // too far for the ink left: bring it closer
                _lastInkX = x;
            }
        }

        /// <summary>
        /// The ring gauntlet (EVT-RINGS): <paramref name="count"/> boost rings from <paramref name="startX"/>, <paramref name="spacingM"/> apart, heights 1 to
        /// <paramref name="maxHeightM"/> above the ground wave, chosen by a seeded generator so a retry gets the same run. Coins sit between the rings.
        /// </summary>
        public void PlaceGauntlet(float startX, int count, float spacingM, float maxHeightM, uint seed)
        {
            var rng = new Pcg32(seed);
            for (int i = 0; i < count; i++)
            {
                float x = startX + (i + 1) * spacingM;
                float h = 1f + (maxHeightM - 1f) * rng.NextFloat01();
                Place(Collectible.Type.Ring, "Pickup_Ring", x, GroundY(x) + h);
                float cx = x + spacingM * 0.5f;
                Place(Collectible.Type.Coin, "Coin", cx, GroundY(cx) + 0.6f + h * 0.5f);
            }
            if (_nextSpawnX < startX + count * spacingM + 8f) _nextSpawnX = startX + count * spacingM + 8f;   // the random spawner stays out of the gauntlet
        }

        /// <summary>A Road Tax coin gate at world <paramref name="x"/>; <paramref name="h01"/> picks its height (0..1) above the ground wave.</summary>
        public void PlaceCoinGate(float x, float h01)
        {
            var g = Place(Collectible.Type.CoinGate, "Pickup_Ring", x, GroundY(x) + 0.5f + 1.5f * h01);
            var sr = g.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = new Color(1f, 0.85f, 0.3f, 1f);
            if (_nextSpawnX < x + 4f) _nextSpawnX = x + 4f;
        }

        /// <summary>One ink canister at <paramref name="x"/> (zone entry: ice and mud are the ink-hungry stretches).</summary>
        public void PlaceInkAt(float x) { Place(Collectible.Type.Ink, "InkCanister", x, GroundY(x) + 1.0f); _lastInkX = x; }

        public void Tick(float carX)
        {
            var t = Tuning;
            KeepInkReachable(carX);
            while (_nextSpawnX < carX + 32f)
            {
                float x = _nextSpawnX;
                float y = GroundY(x);
                float ink = Ink01();
                int roll = Random.Range(0, 100);
                float advance = Random.Range(11f, 16f);

                int inkEdge = t.inkChance, heartEdge = inkEdge + t.heartChance, gemEdge = heartEdge + t.gemChance, powerEdge = gemEdge + t.powerUpChance;
                if (roll < inkEdge && ink < t.inkNoSpawnAbove)
                {
                    Place(Collectible.Type.Ink, "InkCanister", x, y + 1.0f);
                    _lastInkX = x; advance = Random.Range(9f, 14f);
                }
                else if (roll < heartEdge && roll >= inkEdge && NeedsHeart()) { Place(Collectible.Type.Heart, "Heart", x, y); advance = Random.Range(9f, 14f); }
                else if (roll >= heartEdge && roll < gemEdge) { Place(Collectible.Type.Gem, "Pickup_Gem", x, y + 0.4f); advance = Random.Range(9f, 14f); }
                else if (roll >= gemEdge && roll < powerEdge)
                {
                    var kinds = new[] { Collectible.Type.Shield, Collectible.Type.Magnet, Collectible.Type.Double, Collectible.Type.SlowMo, Collectible.Type.Ghost };
                    var k = kinds[Random.Range(0, kinds.Length)];
                    Place(k, "Pickup_" + k, x, y + 0.5f); advance = Random.Range(11f, 15f);
                }
                else
                {
                    var shape = (Shape)Random.Range(0, 5);
                    if (Random.value < t.coinShapeKeepChance) CoinShape(shape, x);          // value 10 per coin, so fewer shapes (D5 earnings band)
                    advance = Random.Range(12f, 17f);
                }
                _nextSpawnX += advance;
            }
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                if (_active[i] == null || _active[i].transform.position.x < carX - 18f)
                {
                    if (_active[i] != null) Object.Destroy(_active[i].gameObject);
                    _active.RemoveAt(i);
                }
            }
        }
    }
}
