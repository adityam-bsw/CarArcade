using System;
using UnityEngine;

namespace RoadBuilderGame
{
    /// <summary>
    /// Ice, mud and glass: while the event is Active the zone's numbers replace the surface tuning through the existing hooks
    /// (friction, speed factor, ink factor, coin factor, road look). The manager supplies the two delegates; this class holds no scene reference.
    /// </summary>
    public sealed class SurfaceZoneBehaviour : IStageEventBehaviour
    {
        private readonly Action<SurfaceRuleSO, float> _enter;     // rule, world distance of the zone start (for the entry ink)
        private readonly Action _exit;
        private readonly Action<float> _respawn;

        public SurfaceZoneBehaviour(Action<SurfaceRuleSO, float> enter, Action exit, Action<float> respawn = null) { _enter = enter; _exit = exit; _respawn = respawn; }

        public void OnStart(StageEventState s) { if (s.Definition.surfaceRule != null) _enter(s.Definition.surfaceRule, s.StartM); }
        public void OnEnd(StageEventState s) { _exit(); }
        public void OnRespawn(StageEventState s, float distanceM) { _respawn?.Invoke(distanceM); }   // surface zones remain after a respawn
    }

    /// <summary>EVT-METEOR: opens the existing meteor spawner for the window, with the event's interval, strike count and terrain seed.</summary>
    public sealed class MeteorWindowBehaviour : IStageEventBehaviour
    {
        private readonly MeteorSpawner _meteors;
        public MeteorWindowBehaviour(MeteorSpawner meteors) { _meteors = meteors; }

        public void OnStart(StageEventState s)
        {
            var d = s.Definition;
            _meteors.BeginWindow(d.strikeIntervalSecMin, d.strikeIntervalSecMax, d.strikeMaxCount, s.TerrainSeed);
        }
        public void OnEnd(StageEventState s) { _meteors.EndWindow(); }
        public void OnRespawn(StageEventState s, float distanceM) { _meteors.OnRespawn(); }
    }

    /// <summary>EVT-RINGS: places the ring gauntlet ahead of the car when the reward starts. No hazard, no surface rule.</summary>
    public sealed class RingWindowBehaviour : IStageEventBehaviour
    {
        private readonly Action<StageEventState> _place;
        public RingWindowBehaviour(Action<StageEventState> place) { _place = place; }
        public void OnStart(StageEventState s) { _place(s); }
        public void OnEnd(StageEventState s) { }
        public void OnRespawn(StageEventState s, float distanceM) { }
    }
}

namespace RoadBuilderGame
{
    /// <summary>
    /// Event behaviour built from three delegates, registered by event id (MON-EATER, BOS-ROB0). The manager supplies the delegates that wire the Road-Eater and RO-B0
    /// controllers; the director and the run controller never know them (no switch on ids anywhere).
    /// </summary>
    public sealed class DelegateBehaviour : IStageEventBehaviour
    {
        private readonly Action<StageEventState> _start, _end;
        private readonly Action<StageEventState, float> _respawn;
        public DelegateBehaviour(Action<StageEventState> start, Action<StageEventState> end, Action<StageEventState, float> respawn = null) { _start = start; _end = end; _respawn = respawn; }
        public void OnStart(StageEventState s) { _start?.Invoke(s); }
        public void OnEnd(StageEventState s) { _end?.Invoke(s); }
        public void OnRespawn(StageEventState s, float distanceM) { _respawn?.Invoke(s, distanceM); }
    }
}
