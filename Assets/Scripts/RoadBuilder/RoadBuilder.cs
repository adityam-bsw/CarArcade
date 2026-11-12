using System.Collections.Generic;
using UnityEngine;

namespace RoadBuilderGame
{
    public class RoadBuilder : MonoBehaviour
    {
        [Header("Road Styling & Geometry")]
        public float roadThickness = 0.38f;
        public float supportDepth = 1.35f;
        public float minPointDistance = 0.38f;
        public int splineSamplesPerSpan = 10;
        public float uniformSampleSpacing = 0.16f;
        public RoadStyle CurrentStyle = RoadStyle.AsphaltHighway;

        [Header("Forward-Only & Anti-Overlap Rules")]
        [Tooltip("Minimum forward X step required for every knot so the road can only advance to the right")]
        public float minForwardDeltaX = 0.14f;
        [Tooltip("Automatically snaps new strokes to the frontier road tip for seamless highways")]
        public float snapRadius = 1.65f;
        [Tooltip("Maximum turn angle per knot to prevent kinks")]
        public float maxTurnAngleDegrees = 32f;
        [Tooltip("Real-time preview tip smoothing factor")]
        public float liveTipLerp = 0.45f;

        public class Segment
        {
            public List<Vector2> rawKnots = new List<Vector2>();
            public List<Vector2> sampledPoints = new List<Vector2>();
            public RoadChunk chunk;
            public float minX;
            public float maxX;
            public Vector2 liveTip;
            public bool hasLiveTip;
        }

        private readonly List<Segment> _segments = new List<Segment>();
        private Segment _activeSegment;
        private PhysicsMaterial2D _physicsMaterial;

        public int SegmentCount => _segments.Count;

        /// <summary>
        /// The furthest right X coordinate of any road built so far.
        /// New road points must always be >= FrontierMaxX to prevent overlapping!
        /// </summary>
        public float FrontierMaxX { get; private set; } = -1000f;

        public Vector2 LastPoint => (_activeSegment != null && _activeSegment.rawKnots.Count > 0)
            ? _activeSegment.rawKnots[_activeSegment.rawKnots.Count - 1]
            : Vector2.zero;

        public void Init(PhysicsMaterial2D mat)
        {
            _physicsMaterial = mat;
        }

        public void SetRoadStyle(RoadStyle style)
        {
            CurrentStyle = style;
            foreach (var seg in _segments)
            {
                if (seg.chunk != null && seg.sampledPoints.Count >= 2)
                {
                    seg.chunk.Build(seg.sampledPoints, 0, seg.sampledPoints.Count - 1, roadThickness, supportDepth, CurrentStyle);
                }
            }
        }

        public void ResetRoad(float startX, float endX, float y)
        {
            foreach (var s in _segments)
            {
                if (s.chunk != null) Destroy(s.chunk.gameObject);
            }
            _segments.Clear();
            _activeSegment = null;
            FrontierMaxX = startX;

            // Create initial smooth starting highway
            BeginNewSegment(new Vector2(startX, y), allowSnap: false);
            for (float x = startX + 2f; x <= endX; x += 2f)
            {
                AddPointToCurrent(new Vector2(x, y));
            }
            EndSegment();
        }

        public bool BeginNewSegment(Vector2 p)
        {
            return BeginNewSegment(p, allowSnap: true);
        }

        public bool BeginNewSegment(Vector2 p, bool allowSnap)
        {
            if (allowSnap && _segments.Count > 0)
            {
                var lastSeg = _segments[_segments.Count - 1];
                if (lastSeg.rawKnots.Count >= 2)
                {
                    Vector2 tip = lastSeg.rawKnots[lastSeg.rawKnots.Count - 1];
                    float distToTip = Vector2.Distance(p, tip);

                    // 1. If starting near the frontier tip, weld directly onto the last segment
                    if (distToTip <= snapRadius)
                    {
                        _activeSegment = lastSeg;
                        if (p.x > tip.x + minForwardDeltaX && distToTip >= minPointDistance * 0.5f)
                        {
                            Vector2 constrained = ConstrainForwardAndTurn(_activeSegment, p);
                            _activeSegment.rawKnots.Add(constrained);
                            _activeSegment.maxX = Mathf.Max(_activeSegment.maxX, constrained.x);
                            FrontierMaxX = Mathf.Max(FrontierMaxX, _activeSegment.maxX);
                        }
                        _activeSegment.liveTip = new Vector2(Mathf.Max(tip.x + minForwardDeltaX, p.x), p.y);
                        _activeSegment.hasLiveTip = true;
                        RebuildActive();
                        return true;
                    }

                    // 2. Strictly forbid starting a new overlapping road behind the existing road's frontier X!
                    // If the user taps slightly behind the tip X, clamp X forward to tip.x + gap or weld if close in Y.
                    if (p.x < FrontierMaxX + 0.15f)
                    {
                        if (Mathf.Abs(p.y - tip.y) <= snapRadius * 1.2f && Mathf.Abs(p.x - tip.x) <= snapRadius * 1.5f)
                        {
                            _activeSegment = lastSeg;
                            _activeSegment.liveTip = tip;
                            _activeSegment.hasLiveTip = false;
                            return true;
                        }
                        // Otherwise reject starting a duplicate road underneath/above existing road
                        _activeSegment = null;
                        return false;
                    }
                }
            }

            _activeSegment = new Segment();
            _activeSegment.rawKnots.Add(p);
            _activeSegment.liveTip = p;
            _activeSegment.hasLiveTip = false;
            _activeSegment.minX = p.x;
            _activeSegment.maxX = p.x;
            FrontierMaxX = Mathf.Max(FrontierMaxX, p.x);

            var go = new GameObject("RoadSegment_" + _segments.Count);
            go.transform.SetParent(transform, false);
            _activeSegment.chunk = go.AddComponent<RoadChunk>();
            _activeSegment.chunk.Init(SpriteFactory.MeshMaterial, _physicsMaterial);

            _segments.Add(_activeSegment);
            return true;
        }

        /// <summary>
        /// Updates the live elastic tip of the road while the player drags, strictly enforcing forward-only X.
        /// </summary>
        public void UpdateLiveStrokeTip(Vector2 worldPos)
        {
            if (_activeSegment == null || _activeSegment.rawKnots.Count == 0) return;

            Vector2 lastKnot = _activeSegment.rawKnots[_activeSegment.rawKnots.Count - 1];
            // Only stretch forward (to the right)
            if (worldPos.x <= lastKnot.x + minForwardDeltaX)
            {
                _activeSegment.hasLiveTip = false;
                return;
            }

            float dist = Vector2.Distance(lastKnot, worldPos);
            if (dist > 0.06f)
            {
                Vector2 constrained = ConstrainForwardAndTurn(_activeSegment, worldPos);
                if (constrained.x > lastKnot.x + minForwardDeltaX * 0.5f)
                {
                    _activeSegment.liveTip = Vector2.Lerp(_activeSegment.liveTip, constrained, liveTipLerp);
                    _activeSegment.liveTip.x = Mathf.Max(lastKnot.x + minForwardDeltaX * 0.5f, _activeSegment.liveTip.x);
                    _activeSegment.hasLiveTip = true;
                    RebuildActive();
                }
            }
        }

        public bool AddPointToCurrent(Vector2 p)
        {
            if (_activeSegment == null || _activeSegment.rawKnots.Count == 0)
            {
                return BeginNewSegment(p);
            }

            Vector2 last = _activeSegment.rawKnots[_activeSegment.rawKnots.Count - 1];

            // STRICT FORWARD-ONLY CHECK: Ignore any pointer movement that goes backward (left) of the last knot!
            if (p.x <= last.x + minForwardDeltaX)
            {
                return false;
            }

            float dist = Vector2.Distance(last, p);
            if (dist < minPointDistance)
            {
                UpdateLiveStrokeTip(p);
                return false;
            }

            Vector2 smoothPt = ConstrainForwardAndTurn(_activeSegment, p);
            if (smoothPt.x <= last.x + minForwardDeltaX * 0.5f)
            {
                return false;
            }

            // Subdivide if the mouse jumped a long forward distance in a single frame
            float constrainedDist = Vector2.Distance(last, smoothPt);
            if (constrainedDist > minPointDistance * 2.4f)
            {
                int subdivs = Mathf.Clamp(Mathf.FloorToInt(constrainedDist / minPointDistance), 2, 6);
                for (int s = 1; s <= subdivs; s++)
                {
                    Vector2 interp = Vector2.Lerp(last, smoothPt, (float)s / subdivs);
                    _activeSegment.rawKnots.Add(interp);
                }
            }
            else
            {
                _activeSegment.rawKnots.Add(smoothPt);
            }

            _activeSegment.liveTip = smoothPt;
            _activeSegment.hasLiveTip = false;
            _activeSegment.maxX = Mathf.Max(_activeSegment.maxX, smoothPt.x);
            FrontierMaxX = Mathf.Max(FrontierMaxX, _activeSegment.maxX);
            RebuildActive();
            return true;
        }

        private Vector2 ConstrainForwardAndTurn(Segment seg, Vector2 candidate)
        {
            int count = seg.rawKnots.Count;
            if (count == 0) return candidate;

            Vector2 last = seg.rawKnots[count - 1];

            // Force candidate X to be strictly ahead of last.x
            if (candidate.x < last.x + minForwardDeltaX)
            {
                candidate.x = last.x + minForwardDeltaX;
            }

            Vector2 delta = candidate - last;
            float len = delta.magnitude;
            if (len < 1e-4f) return last + new Vector2(minForwardDeltaX, 0f);

            Vector2 dir = delta / len;

            // 1. Clamp slope angle to [-45, +45] degrees from +X (strictly forward and climbable, never vertical)
            float maxSlopeRad = 45f * Mathf.Deg2Rad;
            float minCos = Mathf.Cos(maxSlopeRad); // ~0.707
            if (dir.x < minCos)
            {
                float signY = Mathf.Sign(dir.y);
                dir = new Vector2(minCos, signY * Mathf.Sin(maxSlopeRad)).normalized;
            }

            // 2. Limit sharp bend angle relative to the previous segment vector
            if (count >= 2)
            {
                Vector2 prevDir = (last - seg.rawKnots[count - 2]).normalized;
                if (prevDir.sqrMagnitude > 1e-4f)
                {
                    float angle = Vector2.SignedAngle(prevDir, dir);
                    if (Mathf.Abs(angle) > maxTurnAngleDegrees)
                    {
                        float clampedAngle = Mathf.Sign(angle) * maxTurnAngleDegrees;
                        float rad = clampedAngle * Mathf.Deg2Rad;
                        float cos = Mathf.Cos(rad);
                        float sin = Mathf.Sin(rad);
                        Vector2 turned = new Vector2(prevDir.x * cos - prevDir.y * sin, prevDir.x * sin + prevDir.y * cos).normalized;

                        // Re-verify forward X after turn limit
                        if (turned.x >= minCos)
                        {
                            dir = turned;
                        }
                    }
                }
            }

            return last + dir * len;
        }

        public void EndSegment()
        {
            if (_activeSegment != null)
            {
                _activeSegment.hasLiveTip = false;
                // If segment only had 1 knot (just a tap with no drag), remove it so no orphan dot remains
                if (_activeSegment.rawKnots.Count < 2)
                {
                    if (_activeSegment.chunk != null) Destroy(_activeSegment.chunk.gameObject);
                    _segments.Remove(_activeSegment);
                    RecalculateFrontierMaxX();
                }
                else
                {
                    RebuildActive();
                }
                _activeSegment = null;
            }
        }

        private void RecalculateFrontierMaxX()
        {
            float max = -1000f;
            foreach (var seg in _segments)
            {
                if (seg.maxX > max) max = seg.maxX;
            }
            FrontierMaxX = max;
        }

        void RebuildActive()
        {
            if (_activeSegment == null) return;

            var workingKnots = new List<Vector2>(_activeSegment.rawKnots);
            if (_activeSegment.hasLiveTip && workingKnots.Count >= 1)
            {
                Vector2 last = workingKnots[workingKnots.Count - 1];
                if (_activeSegment.liveTip.x > last.x + minForwardDeltaX * 0.5f &&
                    Vector2.Distance(last, _activeSegment.liveTip) > 0.08f)
                {
                    workingKnots.Add(_activeSegment.liveTip);
                }
            }

            if (workingKnots.Count >= 2)
            {
                // Stronger 4-pass Laplacian relaxation so valleys have a wide, gentle curvature radius larger than the car's wheelbase
                var relaxedKnots = BezierSpline.SmoothKnots(workingKnots, 3, 0.48f);
                _activeSegment.sampledPoints = BezierSpline.SampleSpline(relaxedKnots, splineSamplesPerSpan, uniformSampleSpacing);

                if (_activeSegment.sampledPoints.Count >= 2)
                {
                    _activeSegment.chunk.Build(
                        _activeSegment.sampledPoints,
                        0,
                        _activeSegment.sampledPoints.Count - 1,
                        roadThickness,
                        supportDepth,
                        CurrentStyle
                    );
                    _activeSegment.maxX = Mathf.Max(_activeSegment.maxX, _activeSegment.chunk.MaxX);
                    FrontierMaxX = Mathf.Max(FrontierMaxX, _activeSegment.maxX);
                }
            }
        }

        /// <summary>
        /// Builds a guaranteed flat safety recovery platform at safePos and clears any overlapping segments
        /// so the car always lands cleanly on solid ground upon respawning.
        /// </summary>
        public Vector2 CreateSafetyPlatformAndFindRespawn(float crashX, float fallbackY)
        {
            EndSegment();

            // Find a valid road point slightly behind crashX, or use fallback
            Vector2 anchor = new Vector2(Mathf.Max(0f, crashX - 2f), fallbackY);
            bool foundRoad = false;

            for (int s = _segments.Count - 1; s >= 0; s--)
            {
                var pts = _segments[s].sampledPoints;
                for (int i = pts.Count - 1; i >= 0; i--)
                {
                    if (pts[i].x <= crashX + 1f)
                    {
                        anchor = pts[i];
                        foundRoad = true;
                        break;
                    }
                }
                if (foundRoad) break;
            }

            anchor.y = Mathf.Clamp(anchor.y, -4f, 6f);

            // If the anchor is near the frontier tip, extend a flat 8-metre safety runway forward from FrontierMaxX
            float runwayStart = Mathf.Max(anchor.x, FrontierMaxX);
            if (runwayStart - anchor.x < 4f)
            {
                Vector2 landingSpot = new Vector2(runwayStart + 2.5f, anchor.y);
                BeginNewSegment(new Vector2(runwayStart, anchor.y), allowSnap: true);
                AddPointToCurrent(new Vector2(runwayStart + 2f, anchor.y));
                AddPointToCurrent(new Vector2(runwayStart + 4f, anchor.y));
                AddPointToCurrent(new Vector2(runwayStart + 6f, anchor.y));
                EndSegment();
                return landingSpot;
            }

            return anchor;
        }

        public bool TryGetRoadYAtX(float x, out float roadY)
        {
            roadY = 0f;
            foreach (var seg in _segments)
            {
                var pts = seg.sampledPoints;
                if (pts.Count < 2) continue;
                if (x < pts[0].x || x > pts[pts.Count - 1].x) continue;

                for (int i = 0; i < pts.Count - 1; i++)
                {
                    Vector2 a = pts[i];
                    Vector2 b = pts[i + 1];
                    if (x >= a.x && x <= b.x)
                    {
                        float t = (b.x - a.x) > 1e-4f ? (x - a.x) / (b.x - a.x) : 0f;
                        roadY = Mathf.Lerp(a.y, b.y, t);
                        return true;
                    }
                }
            }
            return false;
        }

        public float LowestY(float x0, float x1)
        {
            float min = float.MaxValue;
            foreach (var seg in _segments)
            {
                var pts = seg.sampledPoints.Count > 0 ? seg.sampledPoints : seg.rawKnots;
                foreach (var pt in pts)
                {
                    if (pt.x >= x0 && pt.x <= x1)
                    {
                        if (pt.y < min) min = pt.y;
                    }
                }
            }
            return (min == float.MaxValue) ? -10f : min;
        }

        public void CullBefore(float x)
        {
            for (int i = _segments.Count - 1; i >= 0; i--)
            {
                var seg = _segments[i];
                if (seg == _activeSegment) continue;
                if (seg.chunk != null && seg.maxX < x)
                {
                    Destroy(seg.chunk.gameObject);
                    _segments.RemoveAt(i);
                }
            }
        }
    }
}
