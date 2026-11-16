using UnityEngine;

namespace RoadBuilderGame
{
    /// <summary>The slice of the player car the stage events may read. Implemented on the manager side (CarStageAdapter) so Game/Stage never names the car type.</summary>
    public interface IStageCarView
    {
        Rigidbody2D Body { get; }
        bool IsGrounded { get; }
        float Scale { get; }
        /// <summary>The car's sprite renderers (body, wheels, overlays). Allocates; call once per event start.</summary>
        SpriteRenderer[] Renderers { get; }
    }

    /// <summary>The slice of the road the stage events may query or edit. Implemented on the manager side (RoadStageAdapter).</summary>
    public interface IStageRoadView
    {
        /// <summary>Right end of the drawn road: nothing exists beyond it.</summary>
        float FrontierMaxX { get; }
        bool TryGetRoadYAtX(float x, out float roadY);
        bool HasRoadAt(float x);
        bool IsRangeProtected(float x0, float x1);
        bool CutRange(float x0, float x1);
        void TintRange(float x0, float x1, Color c);
        int EatBefore(float frontX, float wholeChunkMargin, float quantum, int maxTrims = 1);
    }
}
