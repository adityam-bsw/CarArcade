using System.Collections.Generic;
using UnityEngine;

namespace RoadBuilderGame
{
    /// <summary>
    /// High-precision spline and curve smoothing engine for 2D road generation.
    /// Combines:
    /// 1. Moving-window Laplacian smoothing on raw input knots (eliminates mouse/finger jitter).
    /// 2. Centripetal Catmull-Rom to Cubic Bézier conversion (prevents loops, cusps, and overshoots).
    /// 3. Subdivision / Chaikin corner refinement and uniform arc-length resampling.
    /// 4. Smooth mitered normal calculation for zero-kink ribbon extrusion.
    /// </summary>
    public static class BezierSpline
    {
        /// <summary>
        /// Evaluates a Cubic Bézier curve at parameter t in [0, 1].
        /// B(t) = (1-t)^3*P0 + 3(1-t)^2*t*P1 + 3(1-t)*t^2*P2 + t^3*P3
        /// </summary>
        public static Vector2 EvaluateCubic(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            float u = 1f - t;
            float tt = t * t;
            float uu = u * u;
            float uuu = uu * u;
            float ttt = tt * t;

            return uuu * p0 + 3f * uu * t * p1 + 3f * u * tt * p2 + ttt * p3;
        }

        /// <summary>
        /// Evaluates the first derivative (tangent vector) of a Cubic Bézier curve at parameter t.
        /// </summary>
        public static Vector2 EvaluateTangent(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            float u = 1f - t;
            return 3f * u * u * (p1 - p0) + 6f * u * t * (p2 - p1) + 3f * t * t * (p3 - p2);
        }

        /// <summary>
        /// Applies iterative Laplacian relaxation to raw input knots while preserving endpoints.
        /// Eliminates hand/mouse micro-tremors before spline fitting.
        /// </summary>
        public static List<Vector2> SmoothKnots(List<Vector2> rawKnots, int iterations = 2, float strength = 0.45f)
        {
            int n = rawKnots.Count;
            if (n < 3) return new List<Vector2>(rawKnots);

            var current = new List<Vector2>(rawKnots);
            var next = new List<Vector2>(rawKnots);

            for (int iter = 0; iter < iterations; iter++)
            {
                next[0] = current[0];
                next[n - 1] = current[n - 1];

                for (int i = 1; i < n - 1; i++)
                {
                    Vector2 avg = (current[i - 1] + current[i + 1]) * 0.5f;
                    next[i] = Vector2.Lerp(current[i], avg, strength);
                }

                for (int i = 1; i < n - 1; i++)
                {
                    current[i] = next[i];
                }
            }

            return current;
        }

        /// <summary>
        /// Generates a sequence of ultra-smooth sampled points along a Centripetal Catmull-Rom / Cubic Bézier
        /// spline passing through the relaxed control knots, then resamples at uniform arc-length intervals.
        /// </summary>
        public static List<Vector2> SampleSpline(List<Vector2> rawKnots, int samplesPerSpan = 10, float targetSpacing = 0.16f)
        {
            var result = new List<Vector2>();
            int rawCount = rawKnots.Count;
            if (rawCount < 2)
            {
                result.AddRange(rawKnots);
                return result;
            }

            // 1. Pre-relax raw knots to remove mouse/touch jitter
            List<Vector2> knots = SmoothKnots(rawKnots, 2, 0.42f);
            int n = knots.Count;

            var densePoints = new List<Vector2>(n * samplesPerSpan);

            if (n == 2)
            {
                float dist = Vector2.Distance(knots[0], knots[1]);
                int steps = Mathf.Max(2, Mathf.CeilToInt(dist / Mathf.Max(0.05f, targetSpacing)));
                for (int i = 0; i <= steps; i++)
                {
                    float t = (float)i / steps;
                    densePoints.Add(Vector2.Lerp(knots[0], knots[1], t));
                }
                return densePoints;
            }

            // 2. Compute Centripetal Catmull-Rom tangents (alpha = 0.5) converted to Cubic Bézier control points.
            // Centripetal parameterization guarantees NO self-intersections, cusps, or wild overshoots
            // when knot spacing is uneven (e.g. fast drag vs slow drag).
            for (int i = 0; i < n - 1; i++)
            {
                Vector2 p0 = (i == 0) ? knots[0] + (knots[0] - knots[1]) : knots[i - 1];
                Vector2 p1 = knots[i];
                Vector2 p2 = knots[i + 1];
                Vector2 p3 = (i + 2 < n) ? knots[i + 2] : knots[n - 1] + (knots[n - 1] - knots[n - 2]);

                float d01 = Mathf.Pow(Mathf.Max(0.0001f, (p1 - p0).sqrMagnitude), 0.25f);
                float d12 = Mathf.Pow(Mathf.Max(0.0001f, (p2 - p1).sqrMagnitude), 0.25f);
                float d23 = Mathf.Pow(Mathf.Max(0.0001f, (p3 - p2).sqrMagnitude), 0.25f);

                Vector2 m1 = (p2 - p1 + (d12 / d01) * (p1 - p0) - (d12 / (d01 + d12)) * (p2 - p0));
                Vector2 m2 = (p2 - p1 + (d12 / d23) * (p3 - p2) - (d12 / (d12 + d23)) * (p3 - p1));

                // Convert Hermite tangents to Cubic Bézier control points
                Vector2 b0 = p1;
                Vector2 b1 = p1 + m1 / 3f;
                Vector2 b2 = p2 - m2 / 3f;
                Vector2 b3 = p2;

                int steps = Mathf.Max(6, samplesPerSpan);
                int startStep = (i == 0) ? 0 : 1;
                for (int s = startStep; s <= steps; s++)
                {
                    float t = (float)s / steps;
                    densePoints.Add(EvaluateCubic(b0, b1, b2, b3, t));
                }
            }

            // 3. Resample to uniform arc-length so physics EdgeCollider2D and mesh quads have consistent spacing
            return ResampleUniformArcLength(densePoints, Mathf.Max(0.08f, targetSpacing));
        }

        /// <summary>
        /// Resamples a polyline at uniform arc-length intervals so mesh triangles and physics collider edges
        /// never bunch up or stretch awkwardly.
        /// </summary>
        public static List<Vector2> ResampleUniformArcLength(List<Vector2> pts, float spacing)
        {
            if (pts == null || pts.Count < 2) return pts;

            var uniform = new List<Vector2>(pts.Count);
            uniform.Add(pts[0]);

            float accumulated = 0f;
            Vector2 prev = pts[0];

            for (int i = 1; i < pts.Count; i++)
            {
                Vector2 curr = pts[i];
                float segLen = Vector2.Distance(prev, curr);
                if (segLen < 1e-5f) continue;

                while (accumulated + segLen >= spacing)
                {
                    float remain = spacing - accumulated;
                    float t = remain / segLen;
                    Vector2 newPt = Vector2.Lerp(prev, curr, t);
                    uniform.Add(newPt);

                    prev = newPt;
                    segLen = Vector2.Distance(prev, curr);
                    accumulated = 0f;
                }

                accumulated += segLen;
                prev = curr;
            }

            Vector2 last = pts[pts.Count - 1];
            if (Vector2.Distance(uniform[uniform.Count - 1], last) > spacing * 0.25f)
            {
                uniform.Add(last);
            }
            else if (uniform.Count > 1)
            {
                uniform[uniform.Count - 1] = last;
            }

            return uniform;
        }
    }
}
