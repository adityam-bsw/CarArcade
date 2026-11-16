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
        private RoadRenderer _renderer;
        private readonly List<Vector2> _edgePoints = new List<Vector2>();

        public float MaxX { get; private set; }
        public RoadGeometry Geometry { get; private set; }

        /// <summary>The collider's points in local space, exposed so tests can prove skins do not affect physics.</summary>
        public IReadOnlyList<Vector2> ColliderPoints => _edgePoints;

        /// <summary>Tints the part of this chunk between world x0 and x1 (looks only).</summary>
        public void TintRange(float x0, float x1, Color c) { if (_renderer != null) _renderer.TintRange(x0, x1, c); }
        public void ClearTint() { if (_renderer != null) _renderer.ClearTint(); }
        /// <summary>Lifts or sinks the whole chunk (rope sway, floating rocks). Collider and mesh move together as they share the transform.</summary>
        public void SetOffsetY(float dy) { var t = transform.localPosition; if (Mathf.Abs(t.y - dy) > 0.0005f) { t.y = dy; transform.localPosition = t; } }

        private bool _hasStartGhost, _hasEndGhost;
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
            _hasStartGhost = hasStart; _hasEndGhost = hasEnd; _startGhost = ls; _endGhost = le;
            ApplyGhosts();
        }

        private void ApplyGhosts()
        {
            _edge.useAdjacentStartPoint = _hasStartGhost; if (_hasStartGhost) _edge.adjacentStartPoint = _startGhost;
            _edge.useAdjacentEndPoint = _hasEndGhost; if (_hasEndGhost) _edge.adjacentEndPoint = _endGhost;
        }

        public void Init(PhysicsMaterial2D physicsMaterial)
        {
            _renderer = GetComponent<RoadRenderer>();
            if (_renderer == null) _renderer = gameObject.AddComponent<RoadRenderer>();
            _renderer.Init();

            _edge = GetComponent<EdgeCollider2D>();
            _edge.sharedMaterial = physicsMaterial;
            _edge.edgeRadius = 0.04f;
        }

        public void Build(List<Vector2> pts, int start, int end, float roadThickness, float supportDepth, RoadSkin skin, Vector2 startNormalHint = default)
        {
            int n = end - start + 1;
            if (n < 2) return;

            Geometry = RoadGeometry.Compute(pts, start, end, roadThickness, supportDepth, startNormalHint);

            // Physics first: align the collider with the driving surface (slightly inset by the edge radius).
            _edgePoints.Clear();
            float maxX = float.MinValue;
            for (int i = 0; i < Geometry.Count; i++)
            {
                Vector2 p = Geometry.Spine[i];
                if (p.x > maxX) maxX = p.x;
                _edgePoints.Add(p + Geometry.Normals[i] * (Geometry.HalfThick - 0.03f));
            }
            _edge.SetPoints(_edgePoints);
            ApplyGhosts();
            MaxX = maxX;

            // Then the look, from the same geometry.
            if (_renderer != null) _renderer.Rebuild(Geometry, skin);
        }
    }
}
