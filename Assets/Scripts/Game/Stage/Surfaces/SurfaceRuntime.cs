using System;
using UnityEngine;

namespace RoadBuilderGame
{
    /// <summary>The road operations the surface runtime needs. <see cref="RoadBuilderSurfaceRoad"/> adapts <see cref="RoadBuilder"/>; tests use a fake.</summary>
    public interface ISurfaceRoad
    {
        /// <summary>Removes road between x0 and x1 (mesh and collider in the same call). True when something was removed.</summary>
        bool CutRange(float x0, float x1);
        /// <summary>True when the range holds road the Road-Eater or a gate owns (noEat); break, dissolve and burn must not touch it.</summary>
        bool IsRangeProtected(float x0, float x1);
        void TintRange(float x0, float x1, Color c);
        void SetOffsetY(float dy);
    }

    public sealed class RoadBuilderSurfaceRoad : ISurfaceRoad
    {
        private readonly RoadBuilder _r;
        public RoadBuilderSurfaceRoad(RoadBuilder r) { _r = r; }
        public bool CutRange(float x0, float x1) { return _r != null && _r.CutRange(x0, x1); }
        public bool IsRangeProtected(float x0, float x1) { return _r != null && _r.IsRangeProtected(x0, x1); }
        public void TintRange(float x0, float x1, Color c) { if (_r != null) _r.TintRange(x0, x1, c); }
        public void SetOffsetY(float dy) { if (_r != null) _r.SetOffsetY(dy); }
    }

    /// <summary>The car facts the runtime needs each frame. Filled by the manager once per frame, no allocation.</summary>
    public struct SurfaceCarSample
    {
        public float X, Speed, UphillDeg, TurnDeg, MudDragMul;
        public bool Boosting, Airborne, Heavy, Metallic;
    }

    /// <summary>What the car must do this frame because of the surface. The manager applies it to the rigid body and the road material.</summary>
    public struct SurfaceOutputs
    {
        public float TargetSpeed;       // m/s, replaces the manager's target
        public bool DriveCut;           // motor off this frame
        public float Impulse;           // m/s upward, apply once
        public float Torque;            // deg/s^2 angular acceleration (positive = nose up)
        public float VerticalAccel;     // m/s^2 along world up
        public float GripMul;           // multiplier on the road friction
        public int Damage;              // durability hits to raise this frame
    }

    /// <summary>
    /// Runs the behaviour set of the player-selected (or event) surface on the real road. The road is divided into pieces of <see cref="SurfaceBehaviourSetSO.pieceLengthM"/> along world x;
    /// the car's piece and its neighbours get a pooled host of the piece-local modules (cracks, dissolve, burn, spikes, fade, pads, springs), and one host of the vehicle-continuous modules
    /// (mud dwell, acid durability, sticky, oil, magnetic...) follows the car. Modules are ticked at 20 Hz by the host; the runtime reads their outputs, hands them to the car through
    /// <see cref="SurfaceOutputs"/> and changes the road: Break, Dissolve and Burn cut the piece out (never inside the entry platform, never inside no-eat road, never right after a respawn),
    /// stages and fades tint the piece. Projectile effects add timed patches. Pure C# apart from <see cref="Color"/>; zero allocation per frame after the pools warm up.
    /// </summary>
    public sealed class SurfaceRuntime : IMonsterSurfaceQuery
    {
        public const int MaxCells = 14;
        public const int MaxPatches = 6;
        public const float RespawnGraceS = 1.5f;
        private const int Behind = 6, Ahead = 5;

        private sealed class Cell
        {
            public readonly SurfaceBehaviourHost host = new SurfaceBehaviourHost();
            public int piece; public bool used, cut; public int tintKey = -1;
        }

        private struct Patch { public bool used; public SurfacePatchKind kind; public float x0, x1, left, strength; public bool fired; public ProjectileSurfaceEffect effect; }

        private readonly Cell[] _cells = new Cell[MaxCells];
        private readonly Patch[] _patches = new Patch[MaxPatches];
        private readonly SurfaceBehaviourHost _car = new SurfaceBehaviourHost();
        private readonly SurfaceFrame _frame = new SurfaceFrame();
        private ISurfaceRoad _road;
        private SurfaceBehaviourSetSO _set;
        private SurfaceRuleSO _rule;
        private SurfaceModuleSpec[] _pieceSpecs = Array.Empty<SurfaceModuleSpec>(), _contSpecs = Array.Empty<SurfaceModuleSpec>();
        private float _pieceLen = 4f, _zoneStartX, _protectUntilX, _time, _graceLeft, _airT, _swayApplied;
        private int _carPiece = int.MinValue;
        private bool _carOn;
        private SurfaceInteractionTable _table;
        private string _surfaceId = "";
        private SurfaceOutputs _out;

        /// <summary>Particle burst for debris and dust (position, colour, count). Set by the manager to the shared particle pool.</summary>
        public Action<Vector2, Color, int> Debris;
        /// <summary>Raised once per piece that was cut out of the road: x0, x1.</summary>
        public Action<float, float> PieceRemoved;
        /// <summary>A piece of a fragile surface (a set with a Breakable or Tear module: glass, fragile paper, lava crust) was cut away. Raised once per piece; the manager credits <c>ProgressionEvent.FragileBroken</c>.</summary>
        public Action FragileBroken;
        private bool _fragileSet;

        public SurfaceOutputs Out { get { return _out; } }
        public bool Active { get { return _set != null && !Paused && (_pieceSpecs.Length + _contSpecs.Length) > 0; } }
        /// <summary>True while a stage event zone owns the road behaviour: the player-surface cells do not tick, projectile patches still do.</summary>
        public bool Paused { get; set; }
        public string SurfaceId { get { return _surfaceId; } set { _surfaceId = value ?? ""; } }
        public SurfaceBehaviourSetSO Set { get { return _set; } }
        public SurfaceBehaviourHost CarHost { get { return _car; } }
        public int PieceOf(float x) { return Mathf.FloorToInt((x - _zoneStartX) / _pieceLen); }
        public int UsedCells { get { int n = 0; for (int i = 0; i < MaxCells; i++) if (_cells[i] != null && _cells[i].used) n++; return n; } }
        /// <summary>The piece-local host bound to the piece at world x, or null (tests and the HUD).</summary>
        public SurfaceBehaviourHost PieceHostAt(float x) { var c = Find(PieceOf(x)); return c != null ? c.host : null; }

        public SurfaceRuntime() { for (int i = 0; i < MaxCells; i++) _cells[i] = new Cell(); }

        public void Bind(ISurfaceRoad road, SurfaceInteractionTable table) { _road = road; _table = table; }

        /// <summary>A new run starts at world x <paramref name="startX"/> on the surface <paramref name="surfaceId"/> (set may be null: plain road, only projectile patches act).</summary>
        public void SetSurface(string surfaceId, SurfaceBehaviourSetSO set, SurfaceRuleSO rule, float startX)
        {
            Clear();
            _surfaceId = surfaceId ?? ""; _set = set; _rule = rule; _zoneStartX = startX;
            _fragileSet = HasFragileModule(set); _frameKind = FrameKindOf(set);
            if (set == null || set.modules == null || set.modules.Length == 0) { _pieceSpecs = Array.Empty<SurfaceModuleSpec>(); _contSpecs = Array.Empty<SurfaceModuleSpec>(); _set = null; return; }
            _pieceLen = Mathf.Max(2f, set.pieceLengthM);
            _protectUntilX = startX + (rule != null ? rule.safeEntryPlatformMetres : set.safeEntryM);
            int pc = 0, cc = 0;
            for (int i = 0; i < set.modules.Length; i++) if (SurfaceModuleFactory.IsPieceLocal(set.modules[i].kind)) pc++; else cc++;
            _pieceSpecs = new SurfaceModuleSpec[pc]; _contSpecs = new SurfaceModuleSpec[cc];
            pc = 0; cc = 0;
            for (int i = 0; i < set.modules.Length; i++) { if (SurfaceModuleFactory.IsPieceLocal(set.modules[i].kind)) _pieceSpecs[pc++] = set.modules[i]; else _contSpecs[cc++] = set.modules[i]; }
            _car.Enter(rule, _contSpecs, true);
            _carOn = true;
        }

        /// <summary>Drops all state (run end, retry). The road keeps what was already cut; the caller resets the road itself.</summary>
        public void Clear()
        {
            for (int i = 0; i < MaxCells; i++) FreeCell(_cells[i]);
            _car.Exit();
            for (int i = 0; i < MaxPatches; i++) _patches[i].used = false;
            _carPiece = int.MinValue; _time = 0f; _graceLeft = 0f; _airT = 0f; _carOn = false;
            _out = new SurfaceOutputs { GripMul = 1f };
            if (_road != null && _swayApplied != 0f) { _road.SetOffsetY(0f); _swayApplied = 0f; }
        }

        /// <summary>The car respawned: states restart, nothing may break under it for a moment (the safety runway is never cut either).</summary>
        /// <summary>True when the set holds a Breakable or Tear module (the surfaces whose broken pieces count as fragile breaks).</summary>
        public static bool HasFragileModule(SurfaceBehaviourSetSO set)
        {
            if (set == null || set.modules == null) return false;
            for (int i = 0; i < set.modules.Length; i++) { var k = set.modules[i].kind; if (k == SurfaceModuleKind.Breakable || k == SurfaceModuleKind.Tear) return true; }
            return false;
        }

        // ------------------------------------------------------------------ road art frame (conveyor belt, electric rail, fragile paper)
        /// <summary>Seconds per conveyor belt frame.</summary>
        public const float ConveyorFrameS = 0.15f;
        private int _frameKind;      // 0 none, 1 conveyor, 2 charge, 3 tear

        /// <summary>
        /// The animation frame of the road strip (RoadSkin.frames): conveyor alternates belt frames; the electric rail shows its lit strip while the piece under the car is warning or charged
        /// and the dormant strip in its safe window; fragile paper shows the torn strip while the piece under the car is tearing (stage 1, at most a moment before it is cut).
        /// </summary>
        public int FrameAt(float carX)
        {
            if (_frameKind == 0 || _set == null) return 0;
            if (_frameKind == 1) return ((int)(_time / ConveyorFrameS)) & 1;
            var h = PieceHostAt(carX); if (h == null) return _frameKind == 2 ? 1 : 0;
            int st = h.MaxStage;
            return _frameKind == 2 ? (st >= 1 ? 0 : 1) : (st >= 1 ? 1 : 0);
        }

        private static int FrameKindOf(SurfaceBehaviourSetSO set)
        {
            if (set == null || set.modules == null) return 0;
            for (int i = 0; i < set.modules.Length; i++)
            {
                var k = set.modules[i].kind;
                if (k == SurfaceModuleKind.Conveyor) return 1;
                if (k == SurfaceModuleKind.Charge) return 2;
                if (k == SurfaceModuleKind.Tear) return 3;
            }
            return 0;
        }

        public void OnRespawn()
        {
            for (int i = 0; i < MaxCells; i++) FreeCell(_cells[i]);
            if (_set != null) { _car.Exit(); _car.Enter(_rule, _contSpecs, true); _carOn = true; }
            _carPiece = int.MinValue; _graceLeft = RespawnGraceS;
            for (int i = 0; i < MaxPatches; i++) _patches[i].used = false;
        }

        // ------------------------------------------------------------------ per-frame tick
        /// <summary>Call once per gameplay frame with scaled dt. Read <see cref="Out"/> afterwards.</summary>
        public void Tick(float dt, in SurfaceCarSample car, float baseTarget)
        {
            _time += dt;
            if (_graceLeft > 0f) _graceLeft -= dt;
            var o = new SurfaceOutputs { TargetSpeed = baseTarget, GripMul = 1f };
            if (Active) TickSurface(dt, car, baseTarget, ref o);
            TickPatches(dt, car, ref o);
            _out = o;
        }

        private void FillFrame(float dt, in SurfaceCarSample c, float baseTarget)
        {
            var f = _frame;
            f.Dt = dt; f.BaseTarget = baseTarget; f.MudDragMul = c.MudDragMul <= 0f ? 1f : c.MudDragMul; f.UphillDeg = c.UphillDeg; f.TurnDeg = c.TurnDeg; f.Boosting = c.Boosting;
            f.ZoneMetres = c.X - _zoneStartX; f.Airborne = c.Airborne; f.TimeS = _time; f.Speed = c.Speed; f.Heavy = c.Heavy; f.Metallic = c.Metallic;
        }

        private void TickSurface(float dt, in SurfaceCarSample c, float baseTarget, ref SurfaceOutputs o)
        {
            FillFrame(dt, c, baseTarget);
            var f = _frame;

            // grounded debounce: a bump must not toggle the modules on and off every frame
            _airT = c.Airborne ? _airT + dt : 0f;
            bool on = _airT < 0.15f;
            if (on != _carOn) { _car.SetVehicleOn(on); _carOn = on; }

            // piece cells: the car's piece, six behind (24 m: their burn and dissolve run on for the 2 s of a cloud at normal speed) and ahead (their phase is in step before the car arrives)
            int cp = PieceOf(c.X);
            if (cp != _carPiece)
            {
                var old = Find(_carPiece);
                if (old != null) old.host.SetVehicleOn(false);
                _carPiece = cp;
            }
            if (_pieceSpecs.Length > 0)
            {
                for (int k = -1; k <= Ahead; k++) Ensure(cp + k, f);
                for (int i = 0; i < MaxCells; i++) { var cell = _cells[i]; if (cell.used && cell.piece < cp - Behind) FreeCell(cell); }
                var here = Find(cp);
                if (here != null && here.host.VehicleOn != on) here.host.SetVehicleOn(on);
            }

            // vehicle-continuous modules
            _car.Tick(f);
            float target = _car.TargetSpeed(f);
            bool cut = _car.DriveCut;
            float impulse = _car.TakeImpulse(), torque = _car.Torque, vert = _car.VerticalAccel, grip = _car.GripMul, sway = _car.Sway;
            int dmg = _car.TakeDamage();

            // piece-local modules
            for (int i = 0; i < MaxCells; i++)
            {
                var cell = _cells[i];
                if (!cell.used) continue;
                f.PieceIndex = cell.piece;
                cell.host.Tick(f);
                if (cell.piece == cp && cell.host.VehicleOn)
                {
                    f.BaseTarget = target;
                    target = cell.host.TargetSpeed(f);
                    f.BaseTarget = baseTarget;
                    cut |= cell.host.DriveCut;
                    impulse += cell.host.TakeImpulse(); torque += cell.host.Torque; vert += cell.host.VerticalAccel; grip *= cell.host.GripMul; sway += cell.host.Sway;
                    dmg += cell.host.TakeDamage();
                }
                ApplyRoad(cell, c);
            }
            f.PieceIndex = cp;

            if (Mathf.Abs(sway - _swayApplied) > 0.004f && _road != null) { _road.SetOffsetY(sway); _swayApplied = sway; }
            o.TargetSpeed = Mathf.Max(0f, target); o.DriveCut = cut; o.Impulse = impulse; o.Torque = torque; o.VerticalAccel = vert; o.GripMul = Mathf.Clamp(grip, 0.2f, 2f); o.Damage = dmg;
        }

        private Cell Find(int piece)
        {
            for (int i = 0; i < MaxCells; i++) if (_cells[i].used && _cells[i].piece == piece) return _cells[i];
            return null;
        }

        private Cell Ensure(int piece, SurfaceFrame f)
        {
            var c = Find(piece);
            if (c != null) return c;
            Cell slot = null;
            for (int i = 0; i < MaxCells; i++) if (!_cells[i].used) { slot = _cells[i]; break; }
            if (slot == null) { slot = _cells[0]; for (int i = 1; i < MaxCells; i++) if (_cells[i].piece < slot.piece) slot = _cells[i]; FreeCell(slot); }
            slot.used = true; slot.piece = piece; slot.cut = false; slot.tintKey = -1;
            slot.host.Enter(_rule, _pieceSpecs, false);
            f.PieceIndex = piece;
            slot.host.Tick(f);                                       // primes the piece index and the shared-clock phase before the car can touch it
            return slot;
        }

        private void FreeCell(Cell c)
        {
            if (c == null || !c.used) return;
            c.host.Exit(); c.used = false; c.cut = false; c.tintKey = -1;
        }

        // ------------------------------------------------------------------ road changes: Break, Dissolve, Burn, tint
        private void ApplyRoad(Cell cell, in SurfaceCarSample car)
        {
            if (_road == null) return;
            float x0 = _zoneStartX + cell.piece * _pieceLen, x1 = x0 + _pieceLen;
            var h = cell.host;
            if (h.AnyGone && !cell.cut)
            {
                bool carHere = car.X >= x0 - 1f && car.X <= x1 + 1f;
                if (carHere && _graceLeft > 0f) return;                                                       // just respawned: wait
                cell.cut = true;
                bool protectedRoad = x1 <= _protectUntilX || _road.IsRangeProtected(x0, x1);                  // entry platform and no-eat road never break
                if (!protectedRoad && _road.CutRange(x0, x1))
                {
                    if (Debris != null && _set != null) Debris(new Vector2((x0 + x1) * 0.5f, 0f), _set.dangerTint, 10);
                    if (PieceRemoved != null) PieceRemoved(x0, x1);
                    if (_fragileSet && FragileBroken != null) FragileBroken();
                    for (int i = 0; i < MaxCells; i++) _cells[i].tintKey = -1;                                // a cut rebuilds the neighbours' meshes: tint again
                }
                else { h.ResetState(); cell.cut = false; cell.tintKey = -1; }
                return;
            }
            if (cell.cut) return;
            float a = h.Alpha;
            int stage = h.MaxStage;
            int key = stage * 64 + Mathf.RoundToInt(Mathf.Clamp01(a) * 15f);
            if (key == cell.tintKey) return;
            cell.tintKey = key;
            Color tint = stage >= 2 ? _set.dangerTint : stage == 1 ? _set.warnTint : Color.white;
            if (a < 0.999f) { float v = Mathf.Lerp(0.25f, 1f, a); tint = new Color(tint.r * v, tint.g * v, tint.b * v, Mathf.Max(0.35f, a)); }
            _road.TintRange(x0, x1, tint);
        }

        // ------------------------------------------------------------------ projectile patches (IProjectileSurfaceHook)
        /// <summary>A world projectile landed. The table decides what the effect is on the current surface; at most MaxPatches live at once (the oldest is replaced).</summary>
        public void AddProjectileEffect(ProjectileSurfaceEffect effect, float worldX, float radiusM, float seconds)
        {
            if (effect == ProjectileSurfaceEffect.None) return;
            SurfacePatchKind kind = SurfacePatchKind.None; float strength = 1f;
            if (_table == null || !_table.TryPatch(_surfaceId, effect, out kind, out strength)) { kind = SurfacePatchKind.None; strength = 1f; }
            if (kind == SurfacePatchKind.None) return;
            int slot = 0; float least = float.MaxValue;
            for (int i = 0; i < MaxPatches; i++)
            {
                if (!_patches[i].used) { slot = i; break; }
                if (_patches[i].left < least) { least = _patches[i].left; slot = i; }
            }
            float r = Mathf.Max(0.6f, radiusM);
            _patches[slot] = new Patch { used = true, kind = kind, x0 = worldX - r, x1 = worldX + r, left = Mathf.Max(0.5f, seconds), strength = strength, fired = false, effect = effect };
            var cell = Find(PieceOf(worldX));
            switch (kind)
            {
                case SurfacePatchKind.Impact: if (cell != null) cell.host.ProjectileImpact(strength * 3f, false); _car.ProjectileImpact(strength * 3f, false); break;
                case SurfacePatchKind.Burn: if (cell != null) cell.host.HazardEffect(SurfaceHazardKind.Fire, 1f); _car.HazardEffect(SurfaceHazardKind.Fire, 1f); break;
                case SurfacePatchKind.Charge: if (cell != null) cell.host.HazardEffect(SurfaceHazardKind.Charge, seconds); _car.HazardEffect(SurfaceHazardKind.Charge, seconds); break;
                case SurfacePatchKind.Acid: if (cell != null) cell.host.HazardEffect(SurfaceHazardKind.Acid, strength); _car.HazardEffect(SurfaceHazardKind.Acid, strength); break;
                case SurfacePatchKind.Fade: if (cell != null) cell.host.HazardEffect(SurfaceHazardKind.Cold, seconds); break;
            }
        }

        public void ClearProjectileEffects() { for (int i = 0; i < MaxPatches; i++) _patches[i].used = false; }

        private void TickPatches(float dt, in SurfaceCarSample car, ref SurfaceOutputs o)
        {
            for (int i = 0; i < MaxPatches; i++)
            {
                if (!_patches[i].used) continue;
                _patches[i].left -= dt;
                if (_patches[i].left <= 0f) { _patches[i].used = false; continue; }
                if (car.X < _patches[i].x0 || car.X > _patches[i].x1) continue;
                float s = _patches[i].strength;
                switch (_patches[i].kind)
                {
                    case SurfacePatchKind.Mound: o.TargetSpeed = Mathf.Max(0f, o.TargetSpeed - 2f * s); break;                       // 2 s sand mound (desert spike)
                    case SurfacePatchKind.Slippery: o.GripMul *= 0.35f; break;                                                        // 3 s slippery patch (ice shard)
                    case SurfacePatchKind.Crater:
                        if (!_patches[i].fired) { _patches[i].fired = true; o.Impulse += 2.5f * s; }
                        o.TargetSpeed = Mathf.Max(0f, o.TargetSpeed - 1.5f * s); break;
                    case SurfacePatchKind.Sticky: o.TargetSpeed = Mathf.Max(0f, o.TargetSpeed - 2.5f * s); o.GripMul *= 1.6f; break;
                    case SurfacePatchKind.Spike:
                        if (car.Boosting || car.Heavy) _patches[i].used = false;                                                      // smashed through
                        else o.TargetSpeed = Mathf.Max(0f, o.TargetSpeed - 1.3f * s);
                        break;
                    case SurfacePatchKind.Charge:
                        o.DriveCut = true;
                        if (!_patches[i].fired) { _patches[i].fired = true; _patches[i].left = Mathf.Min(_patches[i].left, 0.5f); }   // one short shock, no damage
                        break;
                    case SurfacePatchKind.Lift: if (!car.Heavy) o.VerticalAccel += 6f * s; break;                                    // pressure bubble lifts light vehicles
                    case SurfacePatchKind.GravityWarp: o.VerticalAccel += 4f * s; break;                                             // gravity orb distorts gravity for 1.5 s
                    case SurfacePatchKind.Snag: o.TargetSpeed = Mathf.Max(0f, o.TargetSpeed - 1.0f * s); break;
                }
            }
        }

        // ------------------------------------------------------------------ monsters (IMonsterSurfaceQuery)
        /// <summary>Slow, stun and eat-speed numbers at world x for the current surface. Section B calls this each tick of the Road-Eater.</summary>
        public MonsterSurfaceEffect MonsterAt(float worldX)
        {
            var e = MonsterSurfaceEffect.None;
            MonsterSurfaceRow row;
            if (_table == null || !_table.TryMonster(_surfaceId, out row)) return e;
            e.SlowMul = row.slowMul <= 0f ? 1f : row.slowMul; e.EatMul = row.eatMul <= 0f ? 1f : row.eatMul;
            if (row.stunSeconds > 0f)
            {
                var cell = Find(PieceOf(worldX));
                if (row.stunStage <= 0 || (cell != null && cell.host.MaxStage >= row.stunStage)) e.StunS = row.stunSeconds;
            }
            return e;
        }
    }

    /// <summary>Adapts the runtime to what section B's projectile runner calls.</summary>
    public sealed class SurfaceProjectileHook : IProjectileSurfaceHook
    {
        private readonly SurfaceRuntime _rt;
        public SurfaceProjectileHook(SurfaceRuntime rt) { _rt = rt; }
        public void OnProjectileSurface(ProjectileSurfaceEffect effect, Vector2 at, float radiusM, float seconds, string projectileId) { _rt.AddProjectileEffect(effect, at.x, radiusM, seconds); }
        public void OnProjectileSurfaceCleared(ProjectileSurfaceEffect effect) { _rt.ClearProjectileEffects(); }
    }
}
