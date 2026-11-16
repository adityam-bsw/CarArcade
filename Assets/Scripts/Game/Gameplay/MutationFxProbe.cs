#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RoadBuilderGame
{
    /// <summary>
    /// QA probe (Editor and development builds only): drives a REAL run in Play mode through each mutation event and logs the values before, during and after
    /// (gravity scale, target speed, ink regen, scale and joint geometry, coin drift, wind, boost drain, tax) so the report has measurements, not claims.
    /// Everything goes through the real director, behaviours and manager tick; the probe only starts the run, jumps the car, waits and reads.
    /// Start from the Editor with MutationFxProbe.Run(); lines go to Debug.Log with the prefix [FxProbe] and to <see cref="Lines"/>.
    /// </summary>
    public sealed class MutationFxProbe : MonoBehaviour
    {
        public static readonly List<string> Lines = new List<string>();
        public static bool Done;
        private RoadGameManager _m;
        private StageLadderDefinition _ladder;

        public static MutationFxProbe Run(string only = null)
        {
            Lines.Clear(); Done = false;
            var go = new GameObject("MutationFxProbe");
            var p = go.AddComponent<MutationFxProbe>();
            p.StartCoroutine(p.Routine(only));
            return p;
        }

        private void Log(string s) { Lines.Add(s); Debug.Log("[FxProbe] " + s); }

        private StageEventDefinition Def(string id)
        {
            foreach (var d in _ladder.eventPool) if (d != null && d.id == id) return d;
            return null;
        }

        private string Snap()
        {
            var car = _m.DebugCar; var sb = new StringBuilder();
            if (car != null)
            {
                var back = car.Body.GetComponent<Rigidbody2D>();
                var joints = car.Body.GetComponents<WheelJoint2D>();
                float wd = joints.Length == 2 ? Vector2.Distance(joints[0].connectedBody.position, car.Body.position) : -1f;
                sb.Append("gravScale=").Append(car.Body.gravityScale.ToString("0.00"))
                  .Append(" scale=").Append(car.Scale.ToString("0.00")).Append(" mass=").Append(car.Body.mass.ToString("0.000"))
                  .Append(" wheelDist=").Append(wd.ToString("0.000")).Append(" targetSpeed=").Append(car.TargetSpeed.ToString("0.00"))
                  .Append(" vx=").Append(car.Body.linearVelocity.x.ToString("0.00")).Append(" windAccel=").Append(car.WindAccel.x.ToString("0.00"));
            }
            sb.Append(" | inkMax=").Append(_m.DebugMaxInk.ToString("0.0")).Append("/").Append(_m.DebugBaseMaxInk.ToString("0.0"))
              .Append(" inkRegen/s=").Append(_m.DebugInkRegenPerSec.ToString("0.00")).Append(" boostDrain=").Append(_m.DebugBoostDrainMul.ToString("0.00"))
              .Append(" repel=").Append(Collectible.RepelTarget != null).Append(" lives=").Append(_m.DebugLives).Append(" coins=").Append(_m.DebugSessionCoins);
            sb.Append(" | fx{").Append(_m.DebugFx.Snapshot()).Append("} ").Append(_m.DebugWorld2 != null ? _m.DebugWorld2.Snapshot() : "");
            return sb.ToString();
        }

        private IEnumerator WaitPlaying()
        {
            float t = 0f;
            while (!_m.DebugPlaying && t < 6f) { t += Time.unscaledDeltaTime; yield return null; }
        }

        private IEnumerator WaitActive(string id, float max)
        {
            float t = 0f;
            while (t < max)
            {
                var a = _m.DebugStage.Director.Plan.Events;
                bool on = false;
                for (int i = 0; i < a.Count; i++) if (a[i].Id == id && a[i].Phase == StagePhase.Active) on = true;
                if (on) yield break;
                t += Time.unscaledDeltaTime; yield return null;
            }
            Log("TIMEOUT waiting for " + id + " to become Active");
        }

        private IEnumerator Seconds(float s) { float t = 0f; while (t < s) { t += Time.deltaTime; yield return null; } }

        private IEnumerator NewRun()
        {
            if (_m.DebugPlaying) { _m.GoHome(); yield return Seconds(0.3f); }
            _m.StartRace();
            yield return WaitPlaying();
            yield return Seconds(0.5f);
        }

        /// <summary>Batch A2 probe: the same real-run before/during/after/death/quit sequence for the 26 new events, with timed samples while active.</summary>
        public static MutationFxProbe RunA2(string only = null)
        {
            Lines.Clear(); Done = false;
            var go = new GameObject("MutationFxProbe");
            var p = go.AddComponent<MutationFxProbe>();
            p.StartCoroutine(p.Routine(only, new[] { "EVT-GRAVINV", "EVT-WHITEOUT", "EVT-REVERSE", "EVT-MIRROR", "EVT-FLAPPY", "EVT-COLLAPSE", "EVT-ICEAGE", "EVT-MAGNET", "EVT-ELASTIC", "EVT-SHADOW", "EVT-DOUBLE", "EVT-PLATFORMS",
                "EVT-GRAVZONES", "EVT-TRAFFIC", "EVT-BOULDER", "EVT-FENCES", "EVT-FLOOD", "EVT-LAVA", "EVT-WINDTUNNEL", "EVT-STICKY", "EVT-INVINK", "EVT-LASER", "EVT-STARS", "EVT-SWAP", "EVT-RHYTHM", "EVT-CHAOS" }));
            return p;
        }

        private IEnumerator Routine(string only, string[] idsOverride = null)
        {
            _m = RoadGameManager.Instance;
            _ladder = _m.DebugEndlessLadder;
            if (_m == null || _ladder == null) { Log("no manager or endless ladder"); Done = true; yield break; }
            string[] ids = idsOverride ?? new[] { "EVT-GRAVSHIFT", "EVT-HEAVYG", "EVT-TUNNEL", "EVT-INKSTORM", "EVT-BOOSTDRAIN", "EVT-VACUUM", "EVT-CANYON", "EVT-LOWINK", "EVT-TAX", "EVT-FRAGILE", "EVT-TINY", "EVT-GIANT" };
            foreach (var id in ids)
            {
                if (only != null && only.IndexOf(id, System.StringComparison.Ordinal) < 0) continue;     // 'only' may list several ids
                Log("==== " + id);
                yield return NewRun();
                var d = Def(id);
                float start = 400f;
                _m.DebugInjectEvent(d, start);
                _m.DebugJumpToStage(start - 95f);                 // flat runway of 170 m ahead; the warning phase begins 80 m before the start
                yield return Seconds(0.25f);
                Log("BEFORE  (dist " + _m.DebugDistance.ToString("0") + "): " + Snap());
                yield return WaitActive(id, 25f);
                yield return Seconds(1.2f);
                Log("DURING  (dist " + _m.DebugDistance.ToString("0") + "): " + Snap());
                yield return Specific(id, d, start);
                // end of the event: jump past it; the real director raises Ended
                _m.DebugJumpToStage(start + d.lengthM + 40f);
                yield return Seconds(0.8f);
                Log("AFTER   (dist " + _m.DebugDistance.ToString("0") + "): " + Snap());
                // death path: restart the event, then lose every heart (game over) and check the revert
                _m.DebugInjectEvent(d, _m.DebugDistance + 120f);
                _m.DebugJumpToStage(_m.DebugDistance + 120f - 95f + 1f);
                yield return WaitActive(id, 25f);
                yield return Seconds(0.4f);
                bool active = _m.DebugFx.Any;
                _m.DebugKill();
                yield return Seconds(0.3f);
                Log("DEATH   active-before=" + active + " fx-after{" + _m.DebugFx.Snapshot() + "} gameoverScreen=" + (!_m.DebugPlaying));
                // quit path
                yield return NewRun();
                _m.DebugInjectEvent(d, 400f); _m.DebugJumpToStage(400f - 95f + 1f);
                yield return WaitActive(id, 25f);
                yield return Seconds(0.3f);
                bool a2 = _m.DebugFx.Any;
                _m.GoHome();
                yield return Seconds(0.4f);
                Log("QUIT    active-before=" + a2 + " fx-after{" + _m.DebugFx.Snapshot() + "} repel=" + (Collectible.RepelTarget != null));
            }
            if (_m.DebugPlaying) _m.GoHome();
            Done = true;
            Log("PROBE DONE");
        }

        public static MutationFxProbe RunLadders()
        {
            Lines.Clear(); Done = false;
            var go = new GameObject("MutationFxProbe");
            var p = go.AddComponent<MutationFxProbe>();
            p.StartCoroutine(p.LadderRoutine());
            return p;
        }

        private string PlanLine(int max)
        {
            var sb = new StringBuilder(); var ev = _m.DebugStage.Director.Plan.Events;
            for (int i = 0; i < ev.Count && i < max; i++) sb.Append(ev[i].Id).Append('@').Append(ev[i].StartM.ToString("0")).Append(' ');
            return sb.ToString();
        }

        private IEnumerator LadderRoutine()
        {
            _m = RoadGameManager.Instance; _ladder = _m.DebugEndlessLadder;
            // ---- A: Countryside slice, then keep going
            Log("==== A: Countryside slice finish gate -> endless");
            GameServices.Actions.SelectCatalogItem(CatalogKind.World, "world-rolling-hills");
            yield return NewRun();
            Log("slice run: onEndless=" + _m.DebugStage.OnEndless + " events=" + _m.DebugStage.Director.Plan.Events.Count + " plan: " + PlanLine(8) + " finishM=" + _m.DebugFinishGate().ToString("0"));
            _m.DebugJumpToGate();
            float t = 0f; while (!_m.DebugSliceCleared && t < 20f) { t += Time.deltaTime; yield return null; }
            yield return Seconds(0.5f);
            Log("after the gate: sliceCleared=" + _m.DebugSliceCleared + " stillPlaying=" + _m.DebugPlaying + " onEndless=" + _m.DebugStage.OnEndless + " finishGate=" + _m.DebugFinishGate().ToString("0") + " dist=" + _m.DebugDistance.ToString("0") + " banner='" + _m.DebugBanner + "' plan: " + PlanLine(6));
            var ev = _m.DebugStage.Director.Plan.Events;
            if (ev.Count > 0)
            {
                var first = ev[0];
                _m.DebugJumpToStage(first.StartM - 95f);
                yield return WaitActive(first.Id, 25f); yield return Seconds(1f);
                Log("endless event " + first.Id + " active after the slice: " + Snap() + " banner='" + _m.DebugBanner + "'");
            }
            yield return Seconds(8f);
            Log("kept driving 8 s past the gate: still playing=" + _m.DebugPlaying + " dist=" + _m.DebugDistance.ToString("0"));
            _m.GoHome(); yield return Seconds(0.3f);
            // ---- B: other worlds run the endless ladder from 0
            foreach (var w in new[] { "world-moon", "world-candy-land", "world-desert" })
            {
                Log("==== B: " + w);
                GameServices.Actions.SelectCatalogItem(CatalogKind.World, w);
                yield return NewRun();
                Log("stageOn=" + _m.DebugStageOn + " onEndless=" + _m.DebugStage.OnEndless + " events=" + _m.DebugStage.Director.Plan.Events.Count + " plan: " + PlanLine(10));
                var e2 = _m.DebugStage.Director.Plan.Events;
                for (int i = 0; i < e2.Count && i < 3; i++)
                {
                    var sx = e2[i];
                    _m.DebugJumpToStage(sx.StartM - 95f);
                    yield return WaitActive(sx.Id, 30f); yield return Seconds(0.8f);
                    Log("stage event " + sx.Id + " (stage " + (sx.SlotIndex / 10) + ") active: " + Snap());
                }
                _m.GoHome(); yield return Seconds(0.3f);
            }
            // ---- C: adaptive challenge, three fails at stage 2 (real death path), then the stage is eased
            Log("==== C: adaptive challenge");
            GameServices.Actions.SelectCatalogItem(CatalogKind.World, "world-desert");
            GameServices.Actions.SetAdaptiveChallenge(true);
            for (int n = 1; n <= 3; n++)
            {
                yield return NewRun();
                _m.DebugJumpToStage(2300f); yield return Seconds(0.5f);
                _m.DebugKill(); yield return Seconds(0.4f);
                Log("fail " + n + " at stage 2: counter=" + _m.DebugStageFails(2));
            }
            yield return NewRun();
            Log("new run, adaptive on, eased stages from the plan context: [" + string.Join(",", _m.DebugEasedStages()) + "]");
            var e3 = _m.DebugStage.Director.Plan.Events;
            StageEventState s2 = null; foreach (var x in e3) if (x.SlotIndex / 10 == 2 && x.SlotIndex % 10 == 0) s2 = x;
            if (s2 != null)
            {
                _m.DebugJumpToStage(s2.StartM - 95f); yield return WaitActive(s2.Id, 30f); yield return Seconds(0.5f);
                Log("eased stage 2 draws " + s2.Id + " (tier " + s2.Definition.Tier + "), HUD banner='" + _m.DebugBanner + "'");
            }
            GameServices.Actions.SetAdaptiveChallenge(false);
            for (int k = 1; k <= 12; k++) _m.DebugClearStageFail(k);
            _m.GoHome();
            Done = true; Log("LADDER PROBE DONE");
        }

        private static bool idsA2Contains(string id) { return System.Array.IndexOf(new[] { "EVT-GRAVINV", "EVT-WHITEOUT", "EVT-REVERSE", "EVT-MIRROR", "EVT-FLAPPY", "EVT-COLLAPSE", "EVT-ICEAGE", "EVT-MAGNET", "EVT-ELASTIC", "EVT-SHADOW", "EVT-DOUBLE", "EVT-PLATFORMS", "EVT-GRAVZONES", "EVT-TRAFFIC", "EVT-BOULDER", "EVT-FENCES", "EVT-FLOOD", "EVT-LAVA", "EVT-WINDTUNNEL", "EVT-STICKY", "EVT-INVINK", "EVT-LASER", "EVT-STARS", "EVT-SWAP", "EVT-RHYTHM", "EVT-CHAOS" }, id) >= 0; }

        private IEnumerator Specific(string id, StageEventDefinition d, float start)
        {
            var car = _m.DebugCar;
            if (idsA2Contains(id))
            {
                // timed samples (every 1 s for 12 s): the phases of the windowed events (gravity flips, reverse bursts, gates, fences) show up as changes
                var sb2 = new StringBuilder("samples: ");
                for (int i = 0; i < 12; i++) { sb2.Append("[").Append(_m.DebugFx.Snapshot()).Append(" ").Append(_m.DebugWorld2 != null ? _m.DebugWorld2.Snapshot() : "").Append(" lives=").Append(_m.DebugLives).Append("] "); yield return Seconds(1f); }
                Log("DURING  " + sb2);
            }
            if (id == "EVT-INKSTORM" || id == "EVT-CANYON")
            {
                var sb = new StringBuilder("wind samples (m/s^2): ");
                for (int i = 0; i < 24; i++) { sb.Append(car.WindAccel.x.ToString("0.0")).Append(' '); yield return Seconds(0.25f); }
                Log("DURING  " + sb);
            }
            if (id == "EVT-CANYON")
            {
                float w0 = _m.DebugFx.WindAccelX; float d0 = _m.DebugDistance;
                _m.PauseRun(); yield return new WaitForSecondsRealtime(1.0f);
                Log("DURING  paused 1 s real time: wind " + w0.ToString("0.00") + " -> " + _m.DebugFx.WindAccelX.ToString("0.00") + ", distance " + d0.ToString("0.0") + " -> " + _m.DebugDistance.ToString("0.0") + ", timeScale " + Time.timeScale);
                _m.ResumeRun(); yield return Seconds(0.3f);
            }
            if (id == "EVT-INKSTORM" || id == "EVT-LOWINK")
            {
                _m.DebugInk = _m.DebugMaxInk * 0.2f;
                float i0 = _m.DebugInk; yield return Seconds(3f);
                Log("DURING  ink regen measured: " + ((_m.DebugInk - i0) / 3f).ToString("0.00") + " per second (tuning base 1.00, ink pickups may add)");
            }
            if (id == "EVT-BOOSTDRAIN")
            {
                _m.DebugStartBoost();
                float t = 0f; while (_m.DebugBoosting && t < 5f) { t += Time.deltaTime; yield return null; }
                Log("DURING  a 1.40 s boost lasted " + t.ToString("0.00") + " s real time (drain multiplier " + _m.DebugBoostDrainMul.ToString("0.00") + ")");
            }
            if (id == "EVT-VACUUM")
            {
                var coins = new List<Collectible>();
                for (int i = 0; i < 4; i++)
                {
                    var go = new GameObject("ProbeCoin"); go.transform.position = car.Body.position + new Vector2(5f + i, 1.0f);
                    var c = go.AddComponent<Collectible>(); c.Init(Collectible.Type.Coin, null); coins.Add(c);
                }
                var p0 = new Vector3[coins.Count]; for (int i = 0; i < coins.Count; i++) p0[i] = coins[i].transform.position;
                float gap0 = coins[0].transform.position.x - car.Body.position.x;
                yield return Seconds(0.8f);
                var sb = new StringBuilder("coin drift after 0.8 s (m from spawn): ");
                for (int i = 0; i < coins.Count; i++) sb.Append(Vector3.Distance(p0[i], coins[i].transform.position).ToString("0.00")).Append(' ');
                Log("DURING  " + sb + "(car moved " + (car.Body.position.x - (p0[0].x - gap0)).ToString("0.0") + " m)");
                foreach (var c in coins) if (c != null) Destroy(c.gameObject);
            }
            if (id == "EVT-TAX")
            {
                // jump inside the window so the runway (170 m) covers the checkpoints at 500 and 600; the jump clears the pickups and the tax puts its gates back
                _m.DebugSessionCoins = 100;
                _m.DebugJumpToStage(start + 55f);
                yield return Seconds(0.3f);
                var gates = new List<Collectible>();
                foreach (var c in FindObjectsByType<Collectible>(FindObjectsSortMode.None)) if (c.itemType == Collectible.Type.CoinGate) gates.Add(c);
                gates.Sort((x, y) => x.transform.position.x.CompareTo(y.transform.position.x));
                var sbg = new StringBuilder("coin gates re-placed after the jump at world x: ");
                foreach (var g in gates) sbg.Append(g.transform.position.x.ToString("0")).Append("(y").Append(g.transform.position.y.ToString("0.0")).Append(") ");
                Log("DURING  " + sbg);
                // make the first gate reachable on the flat runway (it is drawn on the ground wave); remove the second so checkpoint 600 has no gate
                if (gates.Count > 0) gates[0].MoveTo(new Vector3(gates[0].transform.position.x, 0.9f, 0f));
                if (gates.Count > 1) Destroy(gates[1].gameObject);
                var lines = new StringBuilder(); int lastCoins = _m.DebugSessionCoins; float t = 0f; string lastBanner = "";
                while (t < 14f)
                {
                    yield return null; t += Time.deltaTime;
                    string b = _m.DebugBanner;
                    if (b != lastBanner) { lines.Append("dist ").Append(_m.DebugDistance.ToString("0")).Append(" banner '").Append(b).Append("'; "); lastBanner = b; }
                    if (_m.DebugSessionCoins < lastCoins) { lines.Append("dist ").Append(_m.DebugDistance.ToString("0")).Append(": coins ").Append(lastCoins).Append(" -> ").Append(_m.DebugSessionCoins).Append("; "); }
                    lastCoins = _m.DebugSessionCoins;
                }
                Log("DURING  tax checkpoints 500 (gate crossed?) and 600 (no gate): " + lines + " coins now " + _m.DebugSessionCoins);
            }
            if (id == "EVT-FRAGILE")
            {
                int l0 = _m.DebugLives;
                car.Body.position += Vector2.up * 0.6f; yield return Seconds(1.2f);
                Log("DURING  small drop (0.6 m, impact ~3.5 m/s): lives " + l0 + " -> " + _m.DebugLives);
                l0 = _m.DebugLives;
                car.Body.position += Vector2.up * 5.5f; car.Body.linearVelocity = new Vector2(car.Body.linearVelocity.x, 0f);
                yield return Seconds(1.6f);
                Log("DURING  big drop (5.5 m, impact ~10 m/s, threshold " + (0.6f * 12f).ToString("0.0") + "): lives " + l0 + " -> " + _m.DebugLives + " banner path ok=" + (_m.DebugLives < l0));
            }
            if (id == "EVT-TINY" || id == "EVT-GIANT")
            {
                float x0 = car.Body.position.x;
                var vs = new StringBuilder("vx every 0.25 s: ");
                for (int i = 0; i < 6; i++) { vs.Append(car.Body.linearVelocity.x.ToString("0.0")).Append(' '); yield return Seconds(0.25f); }
                Log("DURING  " + vs);
                var car2 = _m.DebugCar;
                bool grounded = car2.IsGrounded;
                var b = car2.Body.GetComponentInChildren<PolygonCollider2D>().bounds.size;
                Log("DURING  drove " + (car2.Body.position.x - x0).ToString("0.0") + " m in 1.5 s, grounded=" + grounded + ", body collider world size=" + b.x.ToString("0.00") + " x " + b.y.ToString("0.00")
                    + ", wheel radius world=" + car2.Body.GetComponents<WheelJoint2D>()[0].connectedBody.GetComponent<CircleCollider2D>().bounds.extents.x.ToString("0.000"));
                // respawn while scaled: the scale must persist and the wheels re-seat
                _m.DebugLoseLife(); yield return Seconds(0.8f);
                var j = _m.DebugCar.Body.GetComponents<WheelJoint2D>();
                Log("DURING  after a respawn: " + Snap() + " wheelDist(back,front)=" + Vector2.Distance(j[0].connectedBody.position, _m.DebugCar.Body.position).ToString("0.000") + "," + Vector2.Distance(j[1].connectedBody.position, _m.DebugCar.Body.position).ToString("0.000"));
            }
        }
    }
}
#endif
