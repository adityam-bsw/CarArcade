using System;
using UnityEngine;

namespace RoadBuilderGame
{
    /// <summary>Events from the run flow that feed the extra unlock-gate counters and vehicle goals (blueprint D2, D3b, D4). Append new members at the end only.</summary>
    public enum ProgressionEvent
    {
        MonsterReached, MissionCompleted, CleanJump, BossReached, StageEventCompleted, SurvivalRun,
        /// <summary>A fragile road section was broken (glass bridge, fragile paper...). Raised by the surface and stage sections; feeds the tank and monster truck mastery goal.</summary>
        FragileBroken,
        /// <summary>A low-gravity stage was survived (rover mastery goal).</summary>
        LowGravityStage,
        LavaStageSurvived,
        RhythmChallenge,
        /// <summary>RO-B0 defeated: pays scrap (first or repeat) and raises the boss counters. Raised once per defeat by the manager's boss payout; section B does not need to raise it again.</summary>
        BossDefeated,
    }

    /// <summary>The single hook the run flow calls. Counters live in the profile (saved by SaveService); nothing is stored elsewhere.</summary>
    public static class ProgressionEvents
    {
        /// <summary>The vehicle key of the run in progress (set by the manager at run start); vehicle goal events are credited to it.</summary>
        public static string ActiveVehicle = "";

        public static ExtraGate GateFor(ProgressionEvent e)
        {
            switch (e)
            {
                case ProgressionEvent.MonsterReached: return ExtraGate.FirstMonster;
                case ProgressionEvent.MissionCompleted: return ExtraGate.Missions;
                case ProgressionEvent.CleanJump: return ExtraGate.CleanJumps;
                case ProgressionEvent.BossReached: return ExtraGate.BossEncounter;
                case ProgressionEvent.StageEventCompleted: return ExtraGate.StageEvents;
                case ProgressionEvent.SurvivalRun: return ExtraGate.SurvivalRuns;
                case ProgressionEvent.LavaStageSurvived: return ExtraGate.SurviveLava;
                case ProgressionEvent.RhythmChallenge: return ExtraGate.RhythmChallenge;
                case ProgressionEvent.BossDefeated: return ExtraGate.BossDefeated;
                default: return ExtraGate.None;
            }
        }

        /// <summary>The per-vehicle goal counter an event feeds ("" = none).</summary>
        public static string GoalFor(ProgressionEvent e)
        {
            switch (e)
            {
                case ProgressionEvent.FragileBroken: return "fragile-breaks";
                case ProgressionEvent.LowGravityStage: return "low-gravity-stages";
                case ProgressionEvent.CleanJump: return "clean-landings";
                default: return "";
            }
        }

        public static void Record(PlayerProfile p, ProgressionEvent e, int n = 1)
        {
            if (p == null || n <= 0) return;
            var g = GateFor(e);
            if (e == ProgressionEvent.BossDefeated)
            {
                var t = ProgressionTuningSO.Load();
                bool first = p.Counter(GateService.ExtraCounterId(ExtraGate.BossDefeated)) == 0 && p.bossDefeats <= 1;   // the hazard payout already counted this defeat
                MasteryService.AddScrap(p, first ? t.scrapPerBossFirst : t.scrapPerBossRepeat);
            }
            if (g != ExtraGate.None) p.AddCounter(GateService.ExtraCounterId(g), n);
            string goal = GoalFor(e);
            if (e != ProgressionEvent.CleanJump && goal.Length > 0 && !string.IsNullOrEmpty(ActiveVehicle)) MasteryService.AddGoal(p, ActiveVehicle, goal, n);   // clean landings are credited at run end
            if (e == ProgressionEvent.BossDefeated) WeeklyService.AddProgress(p, DateTime.UtcNow, WeeklyKind.BossDefeats, n);
        }

        /// <summary>A stage event ended: counts it, then raises any counter the data rules attach to its id (lava, rhythm, low gravity, fragile road).</summary>
        public static void RecordStageEvent(PlayerProfile p, ProgressionTuningSO t, string eventId)
        {
            if (p == null) return;
            Record(p, ProgressionEvent.StageEventCompleted);
            if (t == null || t.eventRules == null || string.IsNullOrEmpty(eventId)) return;
            for (int i = 0; i < t.eventRules.Length; i++)
                if (!string.IsNullOrEmpty(t.eventRules[i].idContains) && eventId.IndexOf(t.eventRules[i].idContains, StringComparison.OrdinalIgnoreCase) >= 0) Record(p, t.eventRules[i].raises);
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
                case ExtraGate.BossDefeated: return "DEFEAT RO-B0";
                case ExtraGate.SurfaceTutorial: return "LEARN A SURFACE";
                case ExtraGate.WorldMastery: return n + " WORLDS MASTERED";
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

        /// <summary>What still blocks a surface: distance, then the extra condition, then the coin price. Empty for owned and free surfaces.</summary>
        public static string Surface(PlayerProfile p, SurfaceGateSO t, string surfaceId)
        {
            var s = GateService.Surface(p, t, surfaceId);
            if (s.AlreadyUnlocked || t == null || !t.TryGet(surfaceId, out var g)) return "";
            if (!s.DistanceMet) return "REACH " + g.unlockMetres + " m";
            if (!s.ExtraMet) return ExtraName(g.extra, g.extraCount);
            return g.coins > 0 ? g.coins + " COINS" : "";
        }

        public static bool SurfaceLocked(PlayerProfile p, SurfaceGateSO t, string surfaceId)
        {
            var s = GateService.Surface(p, t, surfaceId);
            if (s.AlreadyUnlocked || t == null || !t.TryGet(surfaceId, out var g)) return false;
            return !(s.CanBuy && g.coins <= 0);
        }

        public static string Vehicle(PlayerProfile p, VehicleGateSO t, string vehicleId)
        {
            var s = GateService.Vehicle(p, t, vehicleId);
            if (s.AlreadyUnlocked || t == null || !t.TryGet(vehicleId, out var g)) return "";
            return s.DistanceMet ? "" : "REACH " + g.unlockMetres + " m";
        }
    }
}
