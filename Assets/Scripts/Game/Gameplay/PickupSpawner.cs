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

        public void Tick(float carX)
        {
            // Spawn pickups along a smooth, predictable world wave completely decoupled from road creation
            while (_nextSpawnX < carX + 32f)
            {
                float x = _nextSpawnX;
                // Smooth world-space sine wave for natural collectible arcs (independent of player road strokes)
                float waveY = 1.4f + Mathf.Sin(x * 0.14f) * 1.35f + Mathf.Cos(x * 0.06f) * 0.65f;

                // If a road already exists at X, keep the item above it, never buried inside
                if (_road.TryGetRoadYAtX(x, out float existingRoadY))
                {
                    waveY = Mathf.Max(waveY, existingRoadY + 1.35f);
                }

                int roll = Random.Range(0, 100);
                Collectible.Type t;
                Sprite s;
                if (roll < 65)
                {
                    t = Collectible.Type.Coin;
                    s = Resources.Load<Sprite>("RoadBuilder/Coin");
                }
                else if (roll < 88)
                {
                    t = Collectible.Type.Ink;
                    s = Resources.Load<Sprite>("RoadBuilder/InkCanister");
                }
                else
                {
                    t = Collectible.Type.Heart;
                    s = Resources.Load<Sprite>("RoadBuilder/Heart");
                }

                // Coins come in neat 3-coin arcs so the player has clear visual targets to build towards
                if (t == Collectible.Type.Coin)
                {
                    for (int c = 0; c < 3; c++)
                    {
                        float cx = x + c * 1.6f;
                        float cy = waveY + Mathf.Sin((c / 2f) * Mathf.PI) * 0.55f;
                        if (_road.TryGetRoadYAtX(cx, out float ry)) cy = Mathf.Max(cy, ry + 1.35f);

                        var coinGo = new GameObject("Pickup_Coin");
                        coinGo.transform.position = new Vector3(cx, cy, 0f);
                        var coinPickup = coinGo.AddComponent<Collectible>();
                        coinPickup.Init(t, s);
                        _active.Add(coinPickup);
                    }
                    _nextSpawnX += Random.Range(11f, 16f);
                }
                else
                {
                    var itemGo = new GameObject("Pickup_" + t);
                    itemGo.transform.position = new Vector3(x, waveY, 0f);
                    var pickup = itemGo.AddComponent<Collectible>();
                    pickup.Init(t, s);
                    _active.Add(pickup);
                    _nextSpawnX += Random.Range(9f, 14f);
                }
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
