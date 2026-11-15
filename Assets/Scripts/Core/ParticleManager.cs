using System.Collections.Generic;
using UnityEngine;

namespace RoadBuilderGame
{
    /// <summary>
    /// Small star bursts for pickups, crashes and drawing. Particles are pooled (no GameObject is created or destroyed while
    /// playing once the pool has warmed up) and the number alive is capped, so effects never cost frames or memory.
    /// </summary>
    public class ParticleManager : MonoBehaviour
    {
        public static ParticleManager Instance { get; private set; }

        private const int MaxAlive = 90;

        private Sprite _starSprite;
        private readonly Stack<SimpleParticle> _free = new Stack<SimpleParticle>(MaxAlive);
        private int _alive;

        public void Init()
        {
            Instance = this;
            _starSprite = Resources.Load<Sprite>("RoadBuilder/Star");
        }

        public void SpawnBurst(Vector2 position, Color color, int count = 10)
        {
            for (int i = 0; i < count && _alive < MaxAlive; i++)
            {
                var p = _free.Count > 0 ? _free.Pop() : CreateParticle();
                p.Launch(position, color, Random.Range(0.2f, 0.45f));
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float speed = Random.Range(2.5f, 7f);
                p.velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;
                p.gravity = -9.8f;
                p.life = Random.Range(0.4f, 0.8f);
                p.rotSpeed = Random.Range(-300f, 300f);
                _alive++;
            }
        }

        private SimpleParticle CreateParticle()
        {
            var go = new GameObject("BurstParticle");
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _starSprite != null ? _starSprite : SpriteFactory.Circle;
            sr.sortingOrder = 20;
            var p = go.AddComponent<SimpleParticle>();
            p.Bind(this, sr);
            return p;
        }

        internal void Recycle(SimpleParticle p)
        {
            _alive = Mathf.Max(0, _alive - 1);
            _free.Push(p);
        }
    }

    public class SimpleParticle : MonoBehaviour
    {
        public Vector2 velocity;
        public float gravity = -9.8f;
        public float life = 0.6f;
        public float rotSpeed = 0f;

        private float _age;
        private SpriteRenderer _sr;
        private ParticleManager _owner;
        private float _size;
        private Color _color;

        internal void Bind(ParticleManager owner, SpriteRenderer sr) { _owner = owner; _sr = sr; gameObject.SetActive(false); }

        internal void Launch(Vector2 position, Color color, float size)
        {
            transform.position = position;
            transform.rotation = Quaternion.identity;
            _size = size; _color = color; _age = 0f;
            transform.localScale = Vector3.one * size;
            _sr.color = color;
            gameObject.SetActive(true);
        }

        private void Update()
        {
            _age += Time.deltaTime;
            if (_age >= life)
            {
                gameObject.SetActive(false);
                _owner.Recycle(this);
                return;
            }

            velocity.y += gravity * Time.deltaTime;
            transform.position += (Vector3)velocity * Time.deltaTime;
            transform.Rotate(0, 0, rotSpeed * Time.deltaTime);

            float t = 1f - (_age / life);
            transform.localScale = Vector3.one * (_size * t);
            _color.a = t;
            _sr.color = _color;
        }
    }
}
