using System.Collections.Generic;
using UnityEngine;

namespace RoadBuilderGame
{
    public enum RoadStyle
    {
        AsphaltHighway = 0,
        DirtTrack = 1,
        NeonGrid = 2
    }

    /// <summary>
    /// One road segment as game logic sees it: its geometry and a top-surface EdgeCollider2D aligned with the driving
    /// surface. The collider is computed from the geometry first and never reads the skin; the look is drawn by the
    /// RoadRenderer on the same object. So changing a skin can never move a collider point.
    /// </summary>
    [RequireComponent(typeof(EdgeCollider2D))]
    public class RoadChunk : MonoBehaviour
    {
        private EdgeCollider2D _edge;
        private EdgeCollider2D _tail;                 // live end of a chunk that is rebuilt while the car drives on its settled part
        private PhysicsMaterial2D _mat;
        private readonly List<EdgeCollider2D> _blocks = new List<EdgeCollider2D>(16);   // _blocks[0] is _edge; each frozen block is never touched again
        private readonly List<int> _blockEnd = new List<int>(16);                       // last point index of each block
        private int _nFrozen;                                                           // blocks in use; the rest are disabled spares
        private int _colRebuilds;
        private readonly List<Vector2> _part = new List<Vector2>(64);
        private readonly List<Vector2> _prevPoints = new List<Vector2>(64);
        /// <summary>Collider SetPoints calls so far (diagnostics and tests: a settled stretch must not be recreated while the end grows).</summary>
        public int ColliderRebuilds => _colRebuilds;
        private const int SettleBlock = 8;
        private RoadRenderer _renderer;
        private readonly List<Vector2> _edgePoints = new List<Vector2>();

        public float MaxX { get; private set; }
        public RoadGeometry Geometry { get; private set; }

        /// <summary>The collider's points in local space, exposed so tests can prove skins do not affect physics.</summary>
        public IReadOnlyList<Vector2> ColliderPoints => _edgePoints;

        /// <summary>Tints the part of this chunk between world x0 and x1 (looks only).</summary>
        public void TintRange(float x0, float x1, Color c) { if (_renderer != null) _renderer.TintRange(x0, x1, c); }
        public void ClearTint() { if (_renderer != null) _renderer.ClearTint(); }
        public Color FirstTint { get { return _renderer != null ? _renderer.FirstTint : Color.white; } }
        public Color LastTint { get { return _renderer != null ? _renderer.LastTint : Color.white; } }
        public void SetNeighbourTints(Color left, Color right) { if (_renderer != null) _renderer.SetNeighbourTints(left, right); }
        /// <summary>Lifts or sinks the whole chunk (rope sway, floating rocks). Collider and mesh move together as they share the transform.</summary>
        public void SetOffsetY(float dy) { var t = transform.localPosition; if (Mathf.Abs(t.y - dy) > 0.0005f) { t.y = dy; transform.localPosition = t; } }

        private bool _hasStartGhost, _hasEndGhost;
        private RoadSkin _skin;
        private Vector2 _startGhost, _endGhost;     // local space

        /// <summary>World-space collider ends: first, second, second-to-last and last point. False when the chunk has fewer than 2 points.</summary>
        public bool TryGetEnds(out Vector2 first, out Vector2 second, out Vector2 beforeLast, out Vector2 last)
        {
            int n = _edgePoints.Count;
            first = second = beforeLast = last = default;
            if (n < 2) return false;
            var t = transform;
            first = t.TransformPoint(_edgePoints[0]); second = t.TransformPoint(_edgePoints[1]);
            beforeLast = t.TransformPoint(_edgePoints[n - 2]); last = t.TransformPoint(_edgePoints[n - 1]);
            return true;
        }

        /// <summary>
        /// Ghost vertices (the neighbouring chunks' next points, world space) make the chunks behave as one continuous chain for the wheels:
        /// a circle rolling over a join no longer snags on the end cap of the edge it just left. Only touches the collider when a value changed.
        /// </summary>
        public void SetGhosts(bool hasStart, Vector2 startWorld, bool hasEnd, Vector2 endWorld)
        {
            if (_edge == null) return;
            var t = transform;
            Vector2 ls = hasStart ? (Vector2)t.InverseTransformPoint(startWorld) : default;
            Vector2 le = hasEnd ? (Vector2)t.InverseTransformPoint(endWorld) : default;
            bool changed = hasStart != _hasStartGhost || hasEnd != _hasEndGhost
                || (hasStart && (ls - _startGhost).sqrMagnitude > 0.0004f) || (hasEnd && (le - _endGhost).sqrMagnitude > 0.0004f);
            if (!changed) return;
            bool joinChanged = hasStart != _hasStartGhost || hasEnd != _hasEndGhost;
            _hasStartGhost = hasStart; _hasEndGhost = hasEnd; _startGhost = ls; _endGhost = le;
            ApplyGhosts();
            if (joinChanged && _renderer != null && Geometry != null && _skin != null) _renderer.Rebuild(Geometry, _skin, !hasStart, !hasEnd);   // free ends fray, joined ends stay flush
        }

        private void ApplyGhosts()
        {
            _edge.useAdjacentStartPoint = _hasStartGhost; if (_hasStartGhost) _edge.adjacentStartPoint = _startGhost;
            if (_nFrozen == 0) { _edge.useAdjacentEndPoint = _hasEndGhost; if (_hasEndGhost) _edge.adjacentEndPoint = _endGhost; }
            else if (_tail != null) { _tail.useAdjacentEndPoint = _hasEndGhost; if (_hasEndGhost) _tail.adjacentEndPoint = _endGhost; }
        }

        public void Init(PhysicsMaterial2D physicsMaterial)
        {
            _renderer = GetComponent<RoadRenderer>();
            if (_renderer == null) _renderer = gameObject.AddComponent<RoadRenderer>();
            _renderer.Init();

            _edge = GetComponent<EdgeCollider2D>();
            _edge.sharedMaterial = physicsMaterial;
            _edge.edgeRadius = 0.04f;
            _mat = physicsMaterial;
            _blocks.Clear(); _blockEnd.Clear(); _blocks.Add(_edge); _blockEnd.Add(0); _nFrozen = 0;
        }

        /// <summary>
        /// Writes the collider. Recreating an EdgeCollider2D destroys its contacts, so while a chunk grows under the car the settled points are
        /// frozen into small blocks (one EdgeCollider2D per SettleBlock points, never rebuilt again, joined by adjacent-point ghosts) and only the
        /// live end is recreated. A frozen block is dropped only if one of its points or its neighbours actually changed (spline resample).
        /// </summary>
        private void ApplyColliders()
        {
            int n = _edgePoints.Count;
            int common = 0, pn = _prevPoints.Count;
            while (common < n && common < pn && (_edgePoints[common] - _prevPoints[common]).sqrMagnitude < 1e-10f) common++;
            int maxM = Mathf.Min(common - 2, n - 3);       // last unchanged point whose neighbours are unchanged; the live end keeps >= 3 points
            while (_nFrozen > 0 && _blockEnd[_nFrozen - 1] > maxM) { _nFrozen--; if (_nFrozen > 0) _blocks[_nFrozen].enabled = false; }
            int s = _nFrozen > 0 ? _blockEnd[_nFrozen - 1] : 0;
            while (maxM - s >= SettleBlock)
            {
                int e = s + SettleBlock;
                EdgeCollider2D col;
                if (_nFrozen == 0) col = _edge;
                else
                {
                    if (_blocks.Count <= _nFrozen) { col = gameObject.AddComponent<EdgeCollider2D>(); col.sharedMaterial = _mat; col.edgeRadius = 0.04f; _blocks.Add(col); _blockEnd.Add(0); }
                    col = _blocks[_nFrozen]; col.enabled = true;
                }
                _part.Clear(); for (int i = s; i <= e; i++) _part.Add(_edgePoints[i]);
                col.SetPoints(_part); _colRebuilds++;
                col.useAdjacentStartPoint = s == 0 ? _hasStartGhost : true; col.adjacentStartPoint = s == 0 ? _startGhost : _edgePoints[s - 1];
                col.useAdjacentEndPoint = true; col.adjacentEndPoint = _edgePoints[e + 1];
                _blockEnd[_nFrozen] = e;
                _nFrozen++; s = e;
            }
            if (_nFrozen == 0)
            {
                _edge.SetPoints(_edgePoints); _colRebuilds++;
                if (_tail != null) _tail.enabled = false;
            }
            else
            {
                if (_tail == null) { _tail = gameObject.AddComponent<EdgeCollider2D>(); _tail.sharedMaterial = _mat; _tail.edgeRadius = 0.04f; }
                _tail.enabled = true;
                _part.Clear(); for (int i = s; i < n; i++) _part.Add(_edgePoints[i]);
                _tail.SetPoints(_part); _colRebuilds++;
                _tail.useAdjacentStartPoint = true; _tail.adjacentStartPoint = _edgePoints[s - 1];
            }
            ApplyGhosts();
            _prevPoints.Clear(); _prevPoints.AddRange(_edgePoints);
        }

        public void Build(List<Vector2> pts, int start, int end, float roadThickness, float supportDepth, RoadSkin skin, Vector2 startNormalHint = default)
        {
            int n = end - start + 1;
            if (n < 2) return;

            Geometry = RoadGeometry.Compute(pts, start, end, roadThickness, supportDepth, startNormalHint, Geometry);   // reuses its arrays

            // Physics first: align the collider with the driving surface (slightly inset by the edge radius).
            _edgePoints.Clear();
            float maxX = float.MinValue;
            for (int i = 0; i < Geometry.Count; i++)
            {
                Vector2 p = Geometry.Spine[i];
                if (p.x > maxX) maxX = p.x;
                _edgePoints.Add(p + Geometry.Normals[i] * (Geometry.HalfThick - 0.03f));
            }
            ApplyColliders();
            MaxX = maxX;

            // Then the look, from the same geometry.
            _skin = skin;
            if (_renderer != null) _renderer.Rebuild(Geometry, skin, !_hasStartGhost, !_hasEndGhost);
        }
    }
}
