using NUnit.Framework;
using UnityEngine;

namespace RoadBuilderGame.Tests
{
    /// <summary>QA13: Flappy gates are passable with a perfectly timed hop at every normal speed, spaced and telegraphed in time, and the whiteout ramp is defined in screen space.</summary>
    public class QA13FixTests
    {
        [Test]
        public void Flappy_PerfectHop_ClearsEveryGate_AtEverySpeed_AndAWindowExists()
        {
            foreach (float gap in new[] { 2.6f, 3.2f })
                foreach (float speed in new[] { 5f, 6f, 8f, 10f, 12f, 14f })
                {
                    float first = float.NaN, last = float.NaN;
                    for (float d = 0f; d <= 12f; d += 0.05f) if (FlappyTunnelGeometry.HopClears(speed, d, gap)) { if (float.IsNaN(first)) first = d; last = d; }
                    Assert.IsFalse(float.IsNaN(first), "a hop clears the gate at " + speed + " m/s gap " + gap);
                    Assert.GreaterOrEqual(last - first, 0.9f, "launch window (m) at " + speed + " m/s: " + first + ".." + last);
                    Assert.GreaterOrEqual((last - first) / speed, 0.1f, "launch window (s) at " + speed + " m/s");
                }
        }

        [Test]
        public void Flappy_NoHop_HitsTheFloorPost_SoTheGateIsReadableAsAHop()
        {
            Assert.IsTrue(FlappyTunnelGeometry.Hits(0f, FlappyTunnelGeometry.RestCentreY, 1f, 0f, FlappyTunnelGeometry.FloorH * 0.5f, FlappyTunnelGeometry.PillarW, FlappyTunnelGeometry.FloorH));
        }

        [Test]
        public void Flappy_Gap_FitsCarPlusHopApex_AndSpacingIsAtLeast2_6SecondsOfTravel()
        {
            float apex = FlappyTunnelGeometry.HopV * FlappyTunnelGeometry.HopV / (2f * FlappyTunnelGeometry.Gravity);
            float room = FlappyTunnelGeometry.Gap(2.6f) - 2f * FlappyTunnelGeometry.CarHalfY;          // free height above a car standing on the floor post
            Assert.GreaterOrEqual(room + FlappyTunnelGeometry.FloorH, apex, "gap >= car height + hop apex margin");
            foreach (float v in new[] { 6f, 9f, 12f, 16f }) Assert.GreaterOrEqual(FlappyTunnelGeometry.Spacing(26f, v) / v, 2.6f - 1e-3f, "spacing in seconds at " + v);
            Assert.GreaterOrEqual(FlappyTunnelGeometry.SpawnAhead(12f, 10f) / 10f, 2f - 1e-3f, "telegraphed 2 s before");
        }
    }
}
