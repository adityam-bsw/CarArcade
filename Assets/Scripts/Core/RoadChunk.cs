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

        public void Init(PhysicsMaterial2D physicsMaterial)
        {
            _renderer = GetComponent<RoadRenderer>();
            if (_renderer == null) _renderer = gameObject.AddComponent<RoadRenderer>();
            _renderer.Init();

            _edge = GetComponent<EdgeCollider2D>();
            _edge.sharedMaterial = physicsMaterial;
            _edge.edgeRadius = 0.04f;
        }

        public void Build(List<Vector2> pts, int start, int end, float roadThickness, float supportDepth, RoadSkin skin)
        {
            int n = end - start + 1;
            if (n < 2) return;

            Geometry = RoadGeometry.Compute(pts, start, end, roadThickness, supportDepth);

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
            MaxX = maxX;

            // Then the look, from the same geometry.
            if (_renderer != null) _renderer.Rebuild(Geometry, skin);
        }
    }
}
