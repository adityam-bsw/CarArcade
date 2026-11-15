using UnityEngine;

namespace RoadBuilderGame
{
    /// <summary>Events from the run flow that feed the extra unlock-gate counters and vehicle goals (blueprint D2, D3b, D4).</summary>
    public enum ProgressionEvent { MonsterReached, MissionCompleted, CleanJump, BossReached, StageEventCompleted, SurvivalRun }

    /// <summary>The single hook the run flow calls. Counters live in the profile (saved by SaveService); nothing is stored elsewhere.</summary>
    public static class ProgressionEvents
    {
        public static ExtraGate GateFor(ProgressionEvent e)
        {
            switch (e)
            {
                case ProgressionEvent.MonsterReached: return ExtraGate.FirstMonster;
                case ProgressionEvent.MissionCompleted: return ExtraGate.Missions;
                case ProgressionEvent.CleanJump: return ExtraGate.CleanJumps;
                case ProgressionEvent.BossReached: return ExtraGate.BossEncounter;
                case ProgressionEvent.StageEventCompleted: return ExtraGate.StageEvents;
                default: return ExtraGate.SurvivalRuns;
            }
        }

        public static void Record(PlayerProfile p, ProgressionEvent e, int n = 1)
        {
            if (p == null || n <= 0) return;
            p.AddCounter(GateService.ExtraCounterId(GateFor(e)), n);
        }

        /// <summary>Run end: clean jumps, a survival run when the run reached the survival distance or finished, and the vehicle goal counters.</summary>
        public static void RecordRunEnd(PlayerProfile p, ProgressionTuningSO t, string vehicleKey, float distance, bool finished, int cleanJumps, int nearMisses)
        {
            if (p == null) return;
            Record(p, ProgressionEvent.CleanJump, cleanJumps);
            if (finished || (t != null && distance >= t.survivalRunMetres)) Record(p, ProgressionEvent.SurvivalRun);
            if (!string.IsNullOrEmpty(vehicleKey))
            {
                MasteryService.AddGoal(p, vehicleKey, "clean-landings", cleanJumps);
                MasteryService.AddGoal(p, vehicleKey, "near-misses", nearMisses);
            }
        }
    }

    /// <summary>The Shield upgrade: from level 1 the run starts with one charge that absorbs a hit, then recharges (faster per level). Plain logic, ticked with unscaled time.</summary>
    public sealed class UpgradeShield
    {
        public int Level { get; private set; }
        public float RechargeSeconds { get; private set; }
        public bool Ready { get; private set; }
        public float RechargeLeft { get; private set; }

        public void Begin(int level, float rechargeSeconds)
        {
            Level = Mathf.Max(0, level); RechargeSeconds = rechargeSeconds;
            Ready = Level > 0; RechargeLeft = 0f;
        }

        public void Tick(float dt)
        {
            if (Level <= 0 || Ready) return;
            RechargeLeft -= dt;
            if (RechargeLeft <= 0f) { Ready = true; RechargeLeft = 0f; }
        }

        public bool TryAbsorb()
        {
            if (!Ready) return false;
            Ready = false; RechargeLeft = RechargeSeconds;
            return true;
        }
    }

    /// <summary>Text for what still blocks a world or vehicle unlock. Empty when nothing blocks it (owned, or all gates met).</summary>
    public static class GateText
    {
        public static string ExtraName(ExtraGate g, int n)
        {
            switch (g)
            {
                case ExtraGate.FirstMonster: return "MEET THE ROAD-EATER";
                case ExtraGate.Missions: return n + " MISSIONS";
                case ExtraGate.CleanJumps: return n + " CLEAN JUMPS";
                case ExtraGate.BossEncounter: return "FACE RO-B0";
                case ExtraGate.SurviveLava: return "SURVIVE LAVA";
                case ExtraGate.WaterMastery: return "MASTER WATER";
                case ExtraGate.StageEvents: return n + " STAGE EVENTS";
                case ExtraGate.SurvivalRuns: return n + " SURVIVAL RUNS";
                case ExtraGate.RhythmChallenge: return "RHYTHM CHALLENGE";
                default: return "";
            }
        }

        public static string World(PlayerProfile p, WorldGateSO t, string worldId)
        {
            var s = GateService.World(p, t, worldId);
            if (s.AlreadyUnlocked || t == null || !t.TryGet(worldId, out var g)) return "";
            if (!s.DistanceMet) return "REACH " + g.unlockMetres + " m";
            if (!s.ExtraMet) return ExtraName(g.extra, g.extraCount);
            return g.coins > 0 ? g.coins + " COINS" : "";
        }

        /// <summary>True while a gated world still needs unlocking (gates unmet, or met but not paid). Worlds not in the table, owned worlds and free met worlds are never locked.</summary>
        public static bool WorldLocked(PlayerProfile p, WorldGateSO t, string worldId)
        {
            var s = GateService.World(p, t, worldId);
            if (s.AlreadyUnlocked || t == null || !t.TryGet(worldId, out _)) return false;
            return !(s.CanBuy && s.CoinCost <= 0);
        }

        public static string Vehicle(PlayerProfile p, VehicleGateSO t, string vehicleId)
        {
            var s = GateService.Vehicle(p, t, vehicleId);
            if (s.AlreadyUnlocked || t == null || !t.TryGet(vehicleId, out var g)) return "";
            return s.DistanceMet ? "" : "REACH " + g.unlockMetres + " m";
        }
    }
}
