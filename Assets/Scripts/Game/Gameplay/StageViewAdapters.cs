using UnityEngine;

namespace RoadBuilderGame
{
    /// <summary>Wraps the live RoadCar for the stage layer. One instance per car; Target is swapped, no per-frame allocation.</summary>
    public sealed class CarStageAdapter : IStageCarView
    {
        public RoadCar Target;
        public Rigidbody2D Body { get { return Target != null ? Target.Body : null; } }
        public bool IsGrounded { get { return Target != null && Target.IsGrounded; } }
        public SpriteRenderer[] Renderers { get { return Target != null ? Target.GetComponentsInChildren<SpriteRenderer>() : null; } }
        public float Scale { get { return Target != null ? Target.Scale : 1f; } }
    }

    /// <summary>Wraps the live RoadBuilder for the stage layer.</summary>
    public sealed class RoadStageAdapter : IStageRoadView
    {
        public RoadBuilder Target;
        public float FrontierMaxX { get { return Target != null ? Target.FrontierMaxX : -1000f; } }
        public bool TryGetRoadYAtX(float x, out float roadY) { if (Target == null) { roadY = 0f; return false; } return Target.TryGetRoadYAtX(x, out roadY); }
        public bool HasRoadAt(float x) { return Target != null && Target.HasRoadAt(x); }
        public bool IsRangeProtected(float x0, float x1) { return Target != null && Target.IsRangeProtected(x0, x1); }
        public bool CutRange(float x0, float x1) { return Target != null && Target.CutRange(x0, x1); }
        public void TintRange(float x0, float x1, Color c) { if (Target != null) Target.TintRange(x0, x1, c); }
        public int EatBefore(float frontX, float wholeChunkMargin, float quantum, int maxTrims = 1) { return Target != null ? Target.EatBefore(frontX, wholeChunkMargin, quantum, maxTrims) : 0; }
    }
}
