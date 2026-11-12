using System.Collections.Generic;
using UnityEngine;

namespace RoadBuilderGame
{
    public class ParticleManager : MonoBehaviour
    {
        public static ParticleManager Instance { get; private set; }

        private Sprite _starSprite;

        public void Init()
        {
            Instance = this;
            _starSprite = Resources.Load<Sprite>("RoadBuilder/Star");
        }

        public void SpawnBurst(Vector2 position, Color color, int count = 10)
        {
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("BurstParticle");
                go.transform.position = position;
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = _starSprite != null ? _starSprite : SpriteFactory.Circle;
                sr.color = color;
                sr.sortingOrder = 20;

                float size = Random.Range(0.2f, 0.45f);
                go.transform.localScale = Vector3.one * size;

                var p = go.AddComponent<SimpleParticle>();
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float speed = Random.Range(2.5f, 7f);
                p.velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;
                p.gravity = -9.8f;
                p.life = Random.Range(0.4f, 0.8f);
                p.rotSpeed = Random.Range(-300f, 300f);
            }
        }
    }

    public class SimpleParticle : MonoBehaviour
    {
        public Vector2 velocity;
        public float gravity = -9.8f;
        public float life = 0.6f;
        public float rotSpeed = 0f;

        private float _age = 0f;
        private SpriteRenderer _sr;
        private Vector3 _initScale;

        private void Start()
        {
            _sr = GetComponent<SpriteRenderer>();
            _initScale = transform.localScale;
        }

        private void Update()
        {
            _age += Time.deltaTime;
            if (_age >= life)
            {
                Destroy(gameObject);
                return;
            }

            velocity.y += gravity * Time.deltaTime;
            transform.position += (Vector3)velocity * Time.deltaTime;
            transform.Rotate(0, 0, rotSpeed * Time.deltaTime);

            float t = 1f - (_age / life);
            transform.localScale = _initScale * t;
            if (_sr != null)
            {
                Color c = _sr.color;
                c.a = t;
                _sr.color = c;
            }
        }
    }
}

