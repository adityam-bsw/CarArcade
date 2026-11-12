using UnityEngine;

namespace RoadBuilderGame
{
    public class Collectible : MonoBehaviour
    {
        public enum Type { Coin, Heart, Ink }
        public Type itemType;
        public int value = 1;

        private Vector3 _startPos;
        private SpriteRenderer _sr;

        public void Init(Type type, Sprite sprite)
        {
            itemType = type;
            _startPos = transform.position;
            _sr = gameObject.AddComponent<SpriteRenderer>();
            _sr.sprite = sprite;
            _sr.sortingOrder = 15;

            var col = gameObject.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.5f;

            float targetScale = (type == Type.Heart) ? 0.75f : ((type == Type.Ink) ? 0.85f : 0.65f);
            if (sprite != null)
            {
                Vector2 sz = sprite.bounds.size;
                transform.localScale = new Vector3(targetScale / sz.x, targetScale / sz.y, 1f);
            }
        }

        private void Update()
        {
            // Floating bounce + gentle spin/pulse
            float bounce = Mathf.Sin(Time.time * 4f + _startPos.x) * 0.15f;
            transform.position = _startPos + new Vector3(0, bounce, 0);

            if (itemType == Type.Coin)
            {
                // Pulsing shimmer
                float s = 1f + Mathf.Sin(Time.time * 6f) * 0.08f;
                transform.rotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.time * 3f) * 12f);
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
                RoadGameManager.Instance.OnCollectItem(this);
                Destroy(gameObject);
            }
        }
    }
}

