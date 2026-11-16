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
            /// <summary>Never deleted by <see cref="RoadBuilder.EatBefore"/> (safety platforms).</summary>
            public bool noEat;
            /// <summary>Surface normal shared with the previous chunk's last point (zero = none), so chunk joins are watertight.</summary>
            public Vector2 startNormal;
            /// <summary>Tangent shared with the previous chunk's last point (zero = none), so the joint has no slope kink.</summary>
            public Vector2 startTangent;
        }
        private float _offsetY;

        private readonly List<Segment> _segments = new List<Segment>();
        private Segment _activeSegment;
        private PhysicsMaterial2D _physicsMaterial;
        private RoadSkin _skin;

        /// <summary>How the road is drawn. Looks only: changing it never moves the road or changes its physics.</summary>
        public RoadSkin CurrentSkin => _skin != null ? _skin : (_skin = RoadSkins.Get(CurrentStyle));

        public int SegmentCount => _segments.Count;

        [Header("Performance")]
        [Tooltip("A stroke is cut into chunks of at most this many knots, so a long road never means rebuilding a long mesh and collider.")]
        public int maxKnotsPerChunk = 36;
        [Tooltip("Minimum seconds between rebuilds caused only by the stretching live tip.")]
        public float liveTipRebuildInterval = 0.04f;
        private float _lastLiveRebuild;
        private readonly List<Vector2> _working = new List<Vector2>(64);
        /// <summary>Total sampled road points across all segments (diagnostics).</summary>
        public int SampledPointCount { get { int n = 0; foreach (var s in _segments) n += s.sampledPoints.Count; return n; } }

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
            SetSkin(RoadSkins.Get(style));
        }

        /// <summary>Draw the road with any skin (a catalogue surface builds its own). Looks only.</summary>
        public void SetSkin(RoadSkin skin)
        {
            if (skin == null) return;
            _skin = skin;
            foreach (var seg in _segments)
            {
                if (seg.chunk != null && seg.sampledPoints.Count >= 2)
                {
                    seg.chunk.Build(seg.sampledPoints, 0, seg.sampledPoints.Count - 1, roadThickness, supportDepth, _skin);
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
            _noEat.Clear();
            _offsetY = 0f;
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
            // A stroke that starts on the end of an earlier chunk (runway, ink pickup, platform) shares its exact point, normal and tangent.
            for (int si = _segments.Count - 1; si >= 0; si--)
            {
                var prev = _segments[si];
                if (prev.chunk == null || prev.chunk.Geometry == null || prev.sampledPoints.Count < 2) continue;
                if (Mathf.Abs(prev.maxX - p.x) > 0.1f) continue;
                Vector2 end = prev.sampledPoints[prev.sampledPoints.Count - 1];
                if (Vector2.Distance(end, p) > 0.1f) continue;
                p = end;
                var pg = prev.chunk.Geometry;
                _activeSegment.startNormal = pg.Normals[pg.Count - 1];
                _activeSegment.startTangent = pg.EndTangent;
                break;
            }
            _activeSegment.rawKnots.Add(p);
            _activeSegment.liveTip = p;
            _activeSegment.hasLiveTip = false;
            _activeSegment.minX = p.x;
            _activeSegment.maxX = p.x;
            FrontierMaxX = Mathf.Max(FrontierMaxX, p.x);

            var go = new GameObject("RoadSegment_" + _segments.Count);
            go.transform.SetParent(transform, false);
            _activeSegment.chunk = go.AddComponent<RoadChunk>();
            _activeSegment.chunk.Init(_physicsMaterial);
            _activeSegment.chunk.SetOffsetY(_offsetY);

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
                    if (Time.unscaledTime - _lastLiveRebuild >= liveTipRebuildInterval) RebuildActive();
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
            if (_activeSegment.rawKnots.Count >= maxKnotsPerChunk) StartNextChunk();
            return true;
        }

        /// <summary>
        /// The active chunk is full: it is finished (never rebuilt again) and the stroke goes on in a new chunk that starts at
        /// the same point, so the road is continuous but each rebuild stays small.
        /// </summary>
        private void StartNextChunk()
        {
            var full = _activeSegment;
            full.hasLiveTip = false;
            Vector2 joint = full.rawKnots[full.rawKnots.Count - 1];

            var next = new Segment();
            if (full.chunk != null && full.chunk.Geometry != null) { next.startNormal = full.chunk.Geometry.Normals[full.chunk.Geometry.Count - 1]; next.startTangent = full.chunk.Geometry.EndTangent; }
            next.rawKnots.Add(joint);
            next.liveTip = joint;
            next.hasLiveTip = false;
            next.minX = joint.x;
            next.maxX = joint.x;
            var go = new GameObject("RoadSegment_" + _segments.Count);
            go.transform.SetParent(transform, false);
            next.chunk = go.AddComponent<RoadChunk>();
            next.chunk.Init(_physicsMaterial);
            _segments.Add(next);
            _activeSegment = next;
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

            _lastLiveRebuild = Time.unscaledTime;
            var workingKnots = _working;
            workingKnots.Clear();
            workingKnots.AddRange(_activeSegment.rawKnots);
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
                _activeSegment.sampledPoints = BezierSpline.SampleSpline(relaxedKnots, splineSamplesPerSpan, uniformSampleSpacing, _activeSegment.startTangent);

                if (_activeSegment.sampledPoints.Count >= 2)
                {
                    _activeSegment.chunk.Build(
                        _activeSegment.sampledPoints,
                        0,
                        _activeSegment.sampledPoints.Count - 1,
                        roadThickness,
                        supportDepth,
                        CurrentSkin,
                        _activeSegment.startNormal
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
                if (_activeSegment != null) _activeSegment.noEat = true;      // the respawn platform is never eaten (FR-10)
                EndSegment();
                return landingSpot;
            }

            return anchor;
        }

        public bool TryGetRoadYAtX(float x, out float roadY)
        {
            roadY = 0f;
            for (int s = 0; s < _segments.Count; s++)
            {
                var pts = _segments[s].sampledPoints;
                int n = pts.Count;
                if (n < 2 || x < pts[0].x || x > pts[n - 1].x) continue;

                // binary search for the span that contains x (points are strictly forward in x)
                int lo = 0, hi = n - 1;
                while (hi - lo > 1)
                {
                    int mid = (lo + hi) >> 1;
                    if (pts[mid].x <= x) lo = mid; else hi = mid;
                }
                Vector2 a = pts[lo], b = pts[hi];
                float t = (b.x - a.x) > 1e-4f ? (x - a.x) / (b.x - a.x) : 0f;
                roadY = Mathf.Lerp(a.y, b.y, Mathf.Clamp01(t));
                return true;
            }
            return false;
        }

        public float LowestY(float x0, float x1)
        {
            float min = float.MaxValue;
            for (int s = 0; s < _segments.Count; s++)
            {
                var seg = _segments[s];
                if (seg.maxX < x0 || seg.minX > x1) continue;           // chunk entirely outside the range
                var pts = seg.sampledPoints.Count > 0 ? seg.sampledPoints : seg.rawKnots;
                for (int i = 0; i < pts.Count; i++)
                {
                    var pt = pts[i];
                    if (pt.x >= x0 && pt.x <= x1 && pt.y < min) min = pt.y;
                }
            }
            return (min == float.MaxValue) ? -10f : min;
        }

        // ------------------------------------------------------------------ Surface break / tint / sway API (section C: Break, Dissolve, Burn)
        private static void DestroyNow(GameObject go)
        {
            if (go == null) return;
            go.SetActive(false);                                   // collider and mesh are gone this very frame
            if (Application.isPlaying) Destroy(go); else DestroyImmediate(go);
        }

        /// <summary>True when any no-eat range or no-eat segment overlaps [x0, x1] (Road-Eater and finish-gate road, owned by section B). Break must not touch it.</summary>
        public bool IsRangeProtected(float x0, float x1)
        {
            for (int i = 0; i < _noEat.Count; i++) if (x1 >= _noEat[i].x0 && x0 <= _noEat[i].x1) return true;
            for (int i = 0; i < _segments.Count; i++) { var s = _segments[i]; if (s.noEat && s.maxX >= x0 && s.minX <= x1) return true; }
            return false;
        }

        /// <summary>
        /// Removes the road between world x0 and x1 (a broken, dissolved or burnt piece). A chunk that straddles the cut is split: the left part is rebuilt in place,
        /// the right part becomes a new chunk, and both mesh and collider are rebuilt in this same call. The active (being drawn) chunk is never cut.
        /// Returns true when road was removed. Allocates only when it actually cuts (a one-off event, never per frame).
        /// </summary>
        public bool CutRange(float x0, float x1)
        {
            if (x1 <= x0) return false;
            bool any = false;
            for (int i = _segments.Count - 1; i >= 0; i--)
            {
                var seg = _segments[i];
                if (seg == _activeSegment || seg.chunk == null || seg.maxX <= x0 || seg.minX >= x1) continue;
                var pts = seg.sampledPoints; int n = pts.Count;
                if (n < 2) continue;
                int a = 0; while (a < n && pts[a].x < x0) a++;
                int b = a; while (b < n && pts[b].x <= x1) b++;
                if (a == b) continue;
                Segment right = null;
                if (n - b >= 2)
                {
                    right = new Segment { noEat = seg.noEat };
                    right.sampledPoints.AddRange(pts.GetRange(b, n - b));
                    right.rawKnots.AddRange(right.sampledPoints);
                    right.minX = right.sampledPoints[0].x; right.maxX = right.sampledPoints[right.sampledPoints.Count - 1].x;
                    var go = new GameObject("RoadSegment_cut");
                    go.transform.SetParent(transform, false);
                    right.chunk = go.AddComponent<RoadChunk>();
                    right.chunk.Init(_physicsMaterial);
                    right.chunk.Build(right.sampledPoints, 0, right.sampledPoints.Count - 1, roadThickness, supportDepth, CurrentSkin);
                    right.chunk.SetOffsetY(_offsetY);
                }
                int insertAt = i + 1;
                if (a >= 2)
                {
                    pts.RemoveRange(a, n - a);
                    seg.maxX = pts[pts.Count - 1].x;
                    seg.chunk.Build(pts, 0, pts.Count - 1, roadThickness, supportDepth, CurrentSkin, seg.startNormal);
                }
                else { DestroyNow(seg.chunk.gameObject); _segments.RemoveAt(i); insertAt = i; }
                if (right != null) _segments.Insert(insertAt, right);
                any = true;
            }
            return any;           // FrontierMaxX is left alone: nothing may be drawn behind the old frontier
        }

        /// <summary>Tints the road between x0 and x1 (crack stage, burn, fade). Looks only.</summary>
        public void TintRange(float x0, float x1, Color c)
        {
            for (int i = 0; i < _segments.Count; i++) { var s = _segments[i]; if (s.chunk != null && s.maxX >= x0 && s.minX <= x1) s.chunk.TintRange(x0, x1, c); }
        }

        /// <summary>Lifts or sinks every road chunk by dy metres (rope sway, floating rocks, lava river). One value for all chunks so joins never step.</summary>
        public void SetOffsetY(float dy)
        {
            if (Mathf.Abs(dy - _offsetY) < 0.004f) return;
            _offsetY = dy;
            for (int i = 0; i < _segments.Count; i++) if (_segments[i].chunk != null) _segments[i].chunk.SetOffsetY(dy);
        }

        // ------------------------------------------------------------------ Road-Eater trim API (hazards.md 2.5)
        private struct NoEatRange { public float x0, x1; }
        private readonly List<NoEatRange> _noEat = new List<NoEatRange>(4);

        /// <summary>Road between <paramref name="x0"/> and <paramref name="x1"/> (world x) is never deleted, including road drawn there later (glass entry, finish gate).</summary>
        public void AddNoEatRange(float x0, float x1) { _noEat.Add(new NoEatRange { x0 = x0, x1 = x1 }); }
        public void ClearNoEatRanges() { _noEat.Clear(); }

        private bool IsNoEat(Segment s)
        {
            if (s.noEat) return true;
            for (int i = 0; i < _noEat.Count; i++) if (s.maxX >= _noEat[i].x0 && s.minX <= _noEat[i].x1) return true;
            return false;
        }

        /// <summary>
        /// Deletes road behind <paramref name="frontX"/>: whole chunks that end before frontX - margin go at once; the chunk that straddles the front is cut back to
        /// the front (quantised) and its mesh and collider are rebuilt in this same call. At most <paramref name="maxTrims"/> straddling chunks are rebuilt per call.
        /// The active (being drawn) chunk and noEat chunks are never touched. Returns how many chunks changed. No allocation.
        /// </summary>
        public int EatBefore(float frontX, float wholeChunkMargin, float quantum, int maxTrims = 1)
        {
            int changed = 0, trims = 0;
            float q = quantum > 0f ? Mathf.Floor(frontX / quantum) * quantum : frontX;
            for (int i = _segments.Count - 1; i >= 0; i--)
            {
                var seg = _segments[i];
                if (seg == _activeSegment || seg.chunk == null || IsNoEat(seg)) continue;
                if (seg.maxX <= frontX - wholeChunkMargin)
                {
                    Destroy(seg.chunk.gameObject); _segments.RemoveAt(i); changed++;
                }
                else if (seg.minX < q && seg.maxX > q && trims < maxTrims)
                {
                    var pts = seg.sampledPoints;
                    int cut = 0;
                    while (cut < pts.Count && pts[cut].x < q) cut++;
                    if (cut <= 0) continue;
                    if (pts.Count - cut < 2) { Destroy(seg.chunk.gameObject); _segments.RemoveAt(i); changed++; continue; }
                    pts.RemoveRange(0, cut);
                    seg.startNormal = Vector2.zero; seg.startTangent = Vector2.zero;
                    seg.minX = pts[0].x;
                    seg.chunk.Build(pts, 0, pts.Count - 1, roadThickness, supportDepth, CurrentSkin);     // collider and mesh in the same frame
                    trims++; changed++;
                }
            }
            if (changed > 0) RecalculateFrontierMaxX();
            return changed;
        }

        /// <summary>Start x of the nearest road that begins after <paramref name="x"/>, or NaN when none exists.</summary>
        public float NextRoadStartAfter(float x)
        {
            float best = float.NaN;
            for (int i = 0; i < _segments.Count; i++)
            {
                float m = _segments[i].minX;
                if (m > x && (float.IsNaN(best) || m < best)) best = m;
            }
            return best;
        }

        /// <summary>Smallest start x of any drawn road (the oldest road still alive), or NaN when none.</summary>
        public float OldestRoadX()
        {
            float best = float.NaN;
            for (int i = 0; i < _segments.Count; i++) { float m = _segments[i].minX; if (float.IsNaN(best) || m < best) best = m; }
            return best;
        }

        /// <summary>True when drawn road covers world x (a cheap road lookup that allocates nothing).</summary>
        public bool HasRoadAt(float x) { return TryGetRoadYAtX(x, out _); }

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
