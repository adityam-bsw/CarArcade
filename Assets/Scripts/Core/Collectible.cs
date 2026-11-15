using UnityEngine;

namespace RoadBuilderGame
{
    public class Collectible : MonoBehaviour
    {
        public enum Type { Coin, Heart, Ink, Gem, Shield, Magnet, Double, SlowMo, Ghost, Ring, CoinGate }

        /// <summary>Raised when the car touches this pickup, just before it is destroyed. Game code subscribes; this class knows nothing about it.</summary>
        public static event System.Action<Collectible> Collected;

        /// <summary>While set, coins fly towards this point when inside MagnetRadius (set by the game while a magnet is active).</summary>
        public static Transform MagnetTarget;
        /// <summary>Road-Eater: pickups left of this world x lie over eaten road and vanish (gap list B). Reset to negative infinity at run start.</summary>
        public static float EatenBeforeX = float.NegativeInfinity;
        public static float MagnetRadius = 5f;
        /// <summary>While set (Coin Vacuum), coins inside <see cref="RepelRadius"/> drift away from this point, at most <see cref="RepelMaxDriftM"/> from where they were placed.</summary>
        public static Transform RepelTarget;
        public static float RepelRadius = 7f, RepelSpeed = 4.5f, RepelMaxDriftM = 6f;
        private Vector3 _origin;

        public Type itemType;
        public int value = 1;

        private Vector3 _startPos;
        private SpriteRenderer _sr;

        /// <summary>Moves the pickup (its bobbing is relative to a base position, so move that).</summary>
        public void MoveTo(Vector3 position) { _startPos = position; _origin = position; transform.position = position; }

        public void Init(Type type, Sprite sprite)
        {
            itemType = type;
            _startPos = transform.position; _origin = _startPos;
            _sr = gameObject.AddComponent<SpriteRenderer>();
            _sr.sprite = sprite;
            _sr.sortingOrder = 15;

            var col = gameObject.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.5f;

            float targetScale = (type == Type.Heart) ? 0.75f : ((type == Type.Ink) ? 0.85f : ((type == Type.Coin) ? 0.65f : (type == Type.Ring ? 1.6f : (type == Type.CoinGate ? 2.4f : 0.9f))));
            if (sprite != null)
            {
                Vector2 sz = sprite.bounds.size;
                transform.localScale = new Vector3(targetScale / sz.x, targetScale / sz.y, 1f);
            }
        }

        private void Update()
        {
            if (_startPos.x < EatenBeforeX) { Destroy(gameObject); return; }
            // Floating bounce + gentle spin/pulse
            if (MagnetTarget != null && (itemType == Type.Coin || itemType == Type.Gem))
            {
                Vector3 to = MagnetTarget.position - _startPos;
                if (to.sqrMagnitude < MagnetRadius * MagnetRadius)
                    _startPos += to.normalized * Mathf.Min(to.magnitude, (6f + 18f / Mathf.Max(0.5f, to.magnitude)) * Time.deltaTime);
            }
            if (RepelTarget != null && itemType == Type.Coin)
            {
                Vector3 away = _startPos - RepelTarget.position; away.z = 0f;
                float d = away.magnitude;
                if (d < RepelRadius && (_startPos - _origin).sqrMagnitude < RepelMaxDriftM * RepelMaxDriftM)
                    _startPos += (d > 0.05f ? away / d : Vector3.up) * (RepelSpeed * Time.deltaTime);
            }
            float bounce = Mathf.Sin(Time.time * 4f + _startPos.x) * 0.15f;
            transform.position = _startPos + new Vector3(0, bounce, 0);

            if (itemType == Type.Coin)
            {
                // Pulsing shimmer
                float s = 1f + Mathf.Sin(Time.time * 6f) * 0.08f;
                transform.rotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.time * 3f) * 12f);
            }
            else if (itemType == Type.Ring || itemType == Type.CoinGate)
            {
                transform.rotation = Quaternion.Euler(0, 0, Time.time * 90f);
            }
            else if (itemType == Type.Heart)
            {
                // Heartbeat pulse
                float beat = 1f + Mathf.PingPong(Time.time * 2f, 0.2f);
                // preserve scale aspect
                transform.rotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.time * 2.5f) * 8f);
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            var car = collision.GetComponentInParent<RoadCar>();
            if (car != null)
            {
                Collected?.Invoke(this);
                Destroy(gameObject);
            }
        }
    }
}

