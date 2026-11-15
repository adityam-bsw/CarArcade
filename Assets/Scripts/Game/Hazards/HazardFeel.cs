using UnityEngine;

namespace RoadBuilderGame
{
    /// <summary>Hit stop on a perfect near miss (gap list B): scaled time drops to a crawl for a few real-time hundredths, then returns to what it was. Unscaled time only; never touches pause.</summary>
    public sealed class HitStop
    {
        public const float Scale = 0.06f;
        private float _left; private float _prev = 1f; private bool _on;
        public bool Active => _on;
        public void Request(float seconds)
        {
            if (_on || Time.timeScale <= 0.001f) return;          // paused or already stopped: nothing to do
            _prev = Time.timeScale; _left = Mathf.Clamp(seconds, 0.02f, 0.12f); _on = true;
            Time.timeScale = Scale; Time.fixedDeltaTime = 0.02f * Scale;
        }
        public void Tick(float unscaledDt)
        {
            if (!_on) return;
            _left -= unscaledDt;
            if (_left <= 0f) Release();
        }
        /// <summary>Restore the scale (also on pause, run end or disable) so the stop can never stick.</summary>
        public void Release()
        {
            if (!_on) return; _on = false;
            if (Mathf.Approximately(Time.timeScale, Scale)) { Time.timeScale = _prev; Time.fixedDeltaTime = 0.02f * _prev; }
        }
    }

    /// <summary>The finish gate prop (gap list B): two posts and a banner at the finish distance, created once, moved by the manager. Own smooth art when the art set has it.</summary>
    public sealed class FinishGateProp
    {
        private Transform _root; private bool _built;
        private void Build()
        {
            if (_built) return; _built = true;
            _root = new GameObject("FinishGate").transform;
            var art = HazardArtSet.Load();
            if (art != null && art.finishGate != null) { var g = SpriteFactory.CreateSprite("Gate", _root, art.finishGate, Color.white, 11); g.transform.localPosition = new Vector3(0f, 2.4f, 0f); }
            else
            {
                var l = SpriteFactory.CreateSprite("PostL", _root, SpriteFactory.Box(Color.white), new Color(0.95f, 0.95f, 0.95f), 11); l.transform.localPosition = new Vector3(-2.2f, 2f, 0f); l.transform.localScale = new Vector3(0.3f, 4f, 1f);
                var r = SpriteFactory.CreateSprite("PostR", _root, SpriteFactory.Box(Color.white), new Color(0.95f, 0.95f, 0.95f), 11); r.transform.localPosition = new Vector3(2.2f, 2f, 0f); r.transform.localScale = new Vector3(0.3f, 4f, 1f);
                var b = SpriteFactory.CreateSprite("Banner", _root, SpriteFactory.Box(Color.white), new Color(0.2f, 0.2f, 0.25f), 12); b.transform.localPosition = new Vector3(0f, 4.1f, 0f); b.transform.localScale = new Vector3(4.7f, 0.7f, 1f);
                var s = SpriteFactory.CreateSprite("Check", _root, SpriteFactory.Box(Color.white), new Color(1f, 0.85f, 0.2f), 13); s.transform.localPosition = new Vector3(0f, 4.1f, 0f); s.transform.localScale = new Vector3(1.4f, 0.4f, 1f);
            }
            _root.gameObject.SetActive(false);
        }

        /// <summary>Show the gate at world x when the car is near enough to see it and the road exists there; hide it otherwise.</summary>
        public void Place(RoadBuilder road, float gateX, float carX, float visibleRange)
        {
            Build();
            bool near = gateX - carX < visibleRange && gateX - carX > -6f && road != null && road.TryGetRoadYAtX(gateX, out _);
            if (_root.gameObject.activeSelf != near) _root.gameObject.SetActive(near);
            if (near && road.TryGetRoadYAtX(gateX, out float y)) _root.position = new Vector3(gateX, y, 0f);
        }
        public void Hide() { if (_built) _root.gameObject.SetActive(false); }
        public void Destroy() { if (_built && _root != null) Object.Destroy(_root.gameObject); _built = false; }
    }
}
