using UnityEngine;

namespace RoadBuilderGame
{
    public enum ProjectileKind { Bolt, Bomb, Roller, World }
    /// <summary>Flight pattern of a world projectile (blueprint B2).</summary>
    public enum ProjectilePattern { Arc, Straight, Splitting, Homing, Bouncing, Zigzag }
    /// <summary>Persistent surface effect a world projectile may leave behind (at most one per projectile). C implements <see cref="IProjectileSurfaceHook"/>.</summary>
    public enum ProjectileSurfaceEffect { None, BreakWeak, SandMound, SlipperyPatch, GravityDistort, Crater, BurnWood, LiftLight, Sticky, TempSpike, ChargeGrid, Invisible, Snag, ElectricBarrier, ChargeMetal, WeakenRoad }
    public enum TelegraphShape { Line, Ring, Marker, Glow, Flash }
    public enum BeatMode { Aimed, Lead, Direct }

    /// <summary>One projectile archetype (hazards.md 4.5). Behaviour numbers never change per world; the skin only changes the look.</summary>
    [CreateAssetMenu(menuName = "Road Builder/Hazards/Projectile Definition", fileName = "PRJ-NEW")]
    public class ProjectileDefinitionSO : ScriptableObject
    {
        public string id = "PRJ-BOLT";
        public ProjectileKind kind = ProjectileKind.Bolt;
        [Range(0.8f, 2f)] public float tellSeconds = 1.2f;
        public float lockLeadSeconds = 0.4f;
        public float speed = 14f;
        public float hitRadius = 0.4f;
        [Tooltip("Extra reach of the car hurtbox added to the hit radius (centre-distance hit threshold = hitRadius + carReach).")] public float carReach = 0.45f;
        public float blastHeight = 2.2f;
        public float activeSeconds = 2.5f;
        public float flightSeconds = 1.1f;
        public float launchDelay = 0.4f;
        public float minMuzzleDistance = 9f;
        public float spawnClearance = 2.3f;
        public float nearMissRadius = 1.6f;
        public int damage = 1;
        public int poolSize = 3;
        public float[] aimTurnRate = { 15f, 20f, 25f };
        [Tooltip("Cue id of the tell voice line (voice-lines.md 3.3).")] public string tellCueId = "vo.rob0.proj.bolt";
        public string fireSfxId = "sfx.proj.fire.bolt";
        [Tooltip("True when a drawn gap is the only counter (roller): the FR-4 gate needs ink then.")] public bool inkOnlyCounter;

        [Header("World projectile (B2). Pattern None-equivalent defaults leave the Countryside archetypes untouched.")]
        public string worldId = "";
        public string displayName = "";
        public ProjectilePattern pattern = ProjectilePattern.Straight;
        public TelegraphShape telegraph = TelegraphShape.Line;
        public string telegraphLabel = "";
        public ProjectileSurfaceEffect surfaceEffect = ProjectileSurfaceEffect.None;
        public float surfaceSeconds = 2f, surfaceRadius = 1.6f;
        [Tooltip("Homing turn rate deg/s per boss phase. Slower than a player can out-turn: never faster than evadable.")] public float homingTurnRate = 28f;
        public float homingSeconds = 1.6f;
        public int splitCount = 2; public float splitAfterSeconds = 0.5f, splitSpreadDeg = 22f;
        public int maxBounces = 2; public float bounceDamping = 0.7f;
        public float zigzagAmplitude = 1.6f, zigzagHz = 1.4f;
        public float arcHeight = 3.5f;
        public Color tint = Color.white;
        public float scale = 0.9f;
        [Tooltip("Own procedural sprite (Assets/Art/Smooth/Hazards); null falls back to a code circle.")] public Sprite sprite;
        public Sprite tellSprite;
        public bool IsWorld => !string.IsNullOrEmpty(worldId);

        public float HitCentre => hitRadius + carReach;
        public float PerfectEdge => HitCentre + (nearMissRadius - HitCentre) * 0.5f;
        public int NearMissSource => kind == ProjectileKind.Bolt || kind == ProjectileKind.World ? 1 : kind == ProjectileKind.Bomb ? 2 : 3;

        public static ProjectileDefinitionSO Make(ProjectileKind k)
        {
            var p = CreateInstance<ProjectileDefinitionSO>(); p.kind = k;
            switch (k)
            {
                case ProjectileKind.Bolt: p.name = p.id = "PRJ-BOLT"; break;
                case ProjectileKind.Bomb:
                    p.name = p.id = "PRJ-BOMB"; p.tellSeconds = 1.5f; p.lockLeadSeconds = 0f; p.speed = 0f; p.hitRadius = 2f; p.carReach = 0.7f; p.activeSeconds = 0.25f; p.flightSeconds = 1.1f;
                    p.minMuzzleDistance = 0f; p.spawnClearance = 4.5f; p.nearMissRadius = 4.5f; p.poolSize = 2; p.tellCueId = "vo.rob0.proj.bomb"; p.fireSfxId = "sfx.proj.fire.bomb"; break;
                case ProjectileKind.Roller:
                    p.name = p.id = "PRJ-SAW"; p.tellSeconds = 1.4f; p.lockLeadSeconds = 0f; p.speed = 8f; p.hitRadius = 0.6f; p.carReach = 0.45f; p.activeSeconds = 3.5f; p.minMuzzleDistance = 0f;
                    p.spawnClearance = 2.3f; p.nearMissRadius = 1.6f; p.poolSize = 2; p.tellCueId = "vo.rob0.proj.saw"; p.fireSfxId = "sfx.proj.fire.saw"; p.inkOnlyCounter = true; break;
            }
            return p;
        }
    }
}
