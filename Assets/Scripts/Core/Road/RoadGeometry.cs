using System.Collections.Generic;
using UnityEngine;

namespace RoadBuilderGame
{
    /// <summary>
    /// The shape of one road segment: its centre line, smoothed surface normals and distance along the line.
    /// This is all game logic and physics ever need; it knows nothing about colours or textures.
    /// The maths is moved unchanged from the old RoadChunk.
    /// </summary>
    public sealed class RoadGeometry
    {
        public int Count;
        public Vector2[] Spine;
        public Vector2[] Normals;
        public float[] Arc;               // distance along the centre line at each point
        public float Thickness;
        public float HalfThick;
        public float SupportDepth;
        public Vector2 StartTangent;
        public Vector2 EndTangent;

        // Scratch for the two normal passes (main thread only). Output arrays are reused via <paramref name="reuse"/>; they may be longer than Count, so always index by Count.
        private static Vector2[] _nA = new Vector2[256];
        private static Vector2[] _nB = new Vector2[256];

        public static RoadGeometry Compute(List<Vector2> pts, int start, int end, float thickness, float supportDepth, Vector2 startNormalHint = default, RoadGeometry reuse = null)
        {
            int n = end - start + 1;
            var g = reuse ?? new RoadGeometry();
            g.Count = n;
            if (g.Spine == null || g.Spine.Length < n)
            {
                int cap = Mathf.Max(n, 64, g.Spine != null ? g.Spine.Length * 2 : 0);
                g.Spine = new Vector2[cap]; g.Normals = new Vector2[cap]; g.Arc = new float[cap];
            }
            g.Thickness = thickness; g.HalfThick = thickness * 0.5f; g.SupportDepth = supportDepth;
            if (_nA.Length < n) { _nA = new Vector2[n * 2]; _nB = new Vector2[n * 2]; }

            // 1. Smooth continuous normals along the spine
            var normals = _nA;
            for (int i = 0; i < n; i++)
            {
                Vector2 dir;
                if (i == 0)
                    dir = pts[start + 1] - pts[start];
                else if (i == n - 1)
                    dir = pts[end] - pts[end - 1];
                else
                {
                    Vector2 d0 = (pts[start + i] - pts[start + i - 1]).normalized;
                    Vector2 d1 = (pts[start + i + 1] - pts[start + i]).normalized;
                    dir = d0 + d1;
                }

                if (dir.sqrMagnitude < 1e-6f) dir = Vector2.right;
                dir.Normalize();

                // Always keep the road top surface facing generally upward in 2D
                Vector2 norm = new Vector2(-dir.y, dir.x);
                if (norm.y < 0f) norm = -norm;
                normals[i] = norm;
            }

            // Chunk join: reuse the previous chunk's end normal so both colliders share the exact same boundary vertex.
            if (startNormalHint.sqrMagnitude > 0.25f) normals[0] = startNormalHint.normalized;

            // Smooth normals with a 3-tap filter so ribbon thickness never pinches on tight curves
            if (n >= 3)
            {
                var smoothed = _nB;
                smoothed[0] = normals[0];
                smoothed[n - 1] = normals[n - 1];
                for (int i = 1; i < n - 1; i++)
                {
                    Vector2 sn = (normals[i - 1] + normals[i] * 2f + normals[i + 1]) * 0.25f;
                    smoothed[i] = sn.sqrMagnitude > 1e-5f ? sn.normalized : normals[i];
                }
                normals = smoothed;
            }

            float run = 0f;
            for (int i = 0; i < n; i++)
            {
                g.Spine[i] = pts[start + i];
                g.Normals[i] = normals[i];
                if (i > 0) run += Vector2.Distance(pts[start + i - 1], pts[start + i]);
                g.Arc[i] = run;
            }

            g.StartTangent = (pts[start + 1] - pts[start]).normalized;
            g.EndTangent = (pts[end] - pts[end - 1]).normalized;
            return g;
        }
    }
}
