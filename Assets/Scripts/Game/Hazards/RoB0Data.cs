using UnityEngine;

namespace RoadBuilderGame
{
    /// <summary>RO-B0 rig constants: motion, timings, per-pose registration and anchor points (metres in the art's local space, +x = behind the robot because the art faces LEFT). Art spec: Docs/asset-manifest/reviews/enemy-ro-b0.md.</summary>
    public static class RoB0Data
    {
        public enum Pose { Idle, Blink, Angry, Firing, Hurt, Damaged }
        public const int PoseCount = 6;

        // hover and feel
        public const float BobAmplitude = 0.7f, BobHz = 0.55f, SwayDeg = 3f, LeanDegPerMps = 0.6f, LeanMaxDeg = 6f, BreatheAmount = 0.025f, BreatheHz = 1.1f;
        public const float FaceDeadZone = 1.5f, FlipSpeed = 7f;
        public const float BlinkMin = 2f, BlinkMax = 4f, BlinkHold = 0.14f;
        public const float FireSeconds = 0.24f, HurtSeconds = 0.55f, PhaseAngrySeconds = 1.2f, HurtMinGain01 = 0.04f, HurtCooldown = 0.8f;
        public const float DamagedAt01 = 0.66f, SparkMin = 1f, SparkMax = 2.4f;
        /// <summary>Highest point of the art above the pivot (cone tip) plus the bob: the hover clamp keeps this under the HUD band.</summary>
        public const float TopClearance = 1.6f + BobAmplitude + 0.15f;

        // screen anchor: the robot hovers in camera viewport space (0..1) at the top-right, facing left toward the car; right edge kept left of the boss HUD panel (x > 0.86)
        public const float AnchorX = 0.72f, AnchorY = 0.6f, PortraitAnchorY = 0.78f, AnchorWanderScale = 0.3f, RightEdgeMaxViewport = 0.8f, RightExtentM = 1.6f, RigScale = 0.9f;
        public const float RetreatViewportDx = 0.5f, RetreatViewportDy = 0.15f;

        // shield bubble
        public const float ShieldAlpha = 0.35f, ShieldPulse = 0.06f, ShieldScale = 0.8f, ShieldPopScale = 1.4f, ShieldPopSeconds = 0.3f;
        public static readonly Vector2 ShieldCentre = new Vector2(-0.35f, -0.1f);
        public static readonly Color ShieldPhase1 = new Color(0.55f, 0.8f, 1f), ShieldPhase2 = new Color(1f, 0.88f, 0.4f);

        // tweens
        public const float EnterSeconds = 1.5f, DefeatSeconds = 2.6f, RecoilSeconds = 0.28f, ShakeSeconds = 0.4f;
        public static readonly Vector3 EnterFrom = new Vector3(11f, 1.5f, 0f), Recoil = new Vector3(0.45f, 0.12f, 0f);

        // anchors (centroid-pivot local, metres)
        public static readonly Vector2 JetL = new Vector2(-0.41f, -1.5f), JetR = new Vector2(0.22f, -1.5f), SparkPoint = new Vector2(1.05f, 0.6f), FlashPoint = new Vector2(-1.45f, -0.25f);
        public const float FlameW = 0.3f, FlameH = 0.45f, FlameBoost = 0.9f;
        public static readonly Color FlameColor = new Color(0.7f, 0.95f, 1f, 0.6f);

        // per pose: art offset and scale (pose 06 is drawn slightly larger and lower than the rest), cannon muzzle
        public static readonly Vector2[] PoseOffset = { Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(-0.17f, -0.1f) };
        public static readonly float[] PoseScale = { 1f, 1f, 1f, 1f, 1f, 1f };
        public static readonly Vector2[] PoseMuzzle =
        {
            new Vector2(-1.87f, -0.65f), new Vector2(-1.87f, -0.65f), new Vector2(-1.87f, -0.65f), new Vector2(-2.0f, -0.25f), new Vector2(-1.8f, -0.6f), new Vector2(-1.95f, -0.7f)
        };

        // sorting (the road is below 20, car and props under it)
        public const int OrderFlame = 24, OrderBody = 25, OrderFlash = 26, OrderFlameTop = 26, OrderShield = 27, OrderFx = 28, OrderTrail = 20;

        // projectiles: family 1 rock, 2 bolt, 3 bubble
        public static readonly Color TrailRock = new Color(0.6f, 0.52f, 0.45f, 0.5f), TrailBolt = new Color(0.5f, 0.9f, 1f, 0.6f), TrailBubble = new Color(0.75f, 0.95f, 1f, 0.45f);

        /// <summary>World projectile id to RO-B0 art family (0 = keep its own skin): rock for boulder and meteor, bolt for the neon bolt, bubble for the bubble and homing orb.</summary>
        public static int FamilyForWorldProjectile(string id)
        {
            switch (id)
            {
                case "PRJ-BOULDER": case "PRJ-METEORCORE": return 1;
                case "PRJ-NEONBOLT": return 2;
                case "PRJ-BUBBLE": case "PRJ-GRAVORB": return 3;
                default: return 0;
            }
        }

        public static Color TrailColor(int family) { return family == 2 ? TrailBolt : family == 3 ? TrailBubble : TrailRock; }
    }
}
