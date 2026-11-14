using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace RoadBuilderGame.UI
{
    /// <summary>
    /// Brings the garage props to life. Every prop is its own child (found by name, missing ones are skipped), so each can
    /// move on its own: the lamps sway, the clock ticks, the robot blinks and its antenna pulses. Tapping a tool, the robot, a
    /// can or the drum plays a short stunt. Views only: nothing here touches the game. All tweens die with the component.
    /// </summary>
    public class WorkshopProps : MonoBehaviour
    {
        private readonly Dictionary<string, RectTransform> _p = new Dictionary<string, RectTransform>();
        private readonly Dictionary<string, Vector3> _restRot = new Dictionary<string, Vector3>();
        private readonly Dictionary<string, Vector3> _restScale = new Dictionary<string, Vector3>();
        private readonly Dictionary<string, Vector2> _restPos = new Dictionary<string, Vector2>();
        private readonly List<Tween> _idle = new List<Tween>();
        private readonly List<Tween> _stunts = new List<Tween>();
        private bool _wired;

        private void Awake()
        {
            foreach (var rt in GetComponentsInChildren<RectTransform>(true))
            {
                if (rt == transform) continue;
                _p[rt.name] = rt;
                _restRot[rt.name] = rt.localEulerAngles;
                _restScale[rt.name] = rt.localScale;
                _restPos[rt.name] = rt.anchoredPosition;
            }
            WireTaps();
        }

        private void OnEnable() { StartIdle(); }

        private void OnDisable()
        {
            KillAll();
            foreach (var kv in _p)
            {
                if (kv.Value == null) continue;
                kv.Value.localEulerAngles = _restRot[kv.Key];
                kv.Value.localScale = _restScale[kv.Key];
                kv.Value.anchoredPosition = _restPos[kv.Key];
            }
        }

        private RectTransform P(string n) { return _p.TryGetValue(n, out var rt) ? rt : null; }

        // ------------------------------------------------------------------ idle life
        private void StartIdle()
        {
            KillAll();
            Sway("Lamp0", 2.2f, 2.8f, 0f);
            Sway("Lamp1", 2.0f, 3.1f, 0.9f);
            Sway("RobotArmL", 2.5f, 2.2f, 0.3f, -1f);
            Sway("RobotArmR", 2.5f, 2.4f, 1.1f);

            var minute = P("ClockMinute"); var hour = P("ClockHour");
            if (minute != null)
            {
                float m = 0f, h = 0f;
                _idle.Add(DOTween.Sequence().AppendCallback(() =>
                {
                    m -= 6f; h -= 0.1f;
                    minute.localEulerAngles = new Vector3(0, 0, m);
                    if (hour != null) hour.localEulerAngles = new Vector3(0, 0, h);
                }).AppendInterval(1f).SetLoops(-1).SetUpdate(true));
            }

            var bulb = P("RobotAntenna");
            if (bulb != null)
                _idle.Add(bulb.DOScale(_restScale["RobotAntenna"] * 1.2f, 0.7f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetUpdate(true));
            if (P("RobotEyes") != null) Blink();
        }

        private void Sway(string name, float degrees, float seconds, float delay, float dir = 1f)
        {
            var rt = P(name);
            if (rt == null) return;
            rt.localEulerAngles = new Vector3(0, 0, -degrees * dir);
            _idle.Add(rt.DOLocalRotate(new Vector3(0, 0, degrees * dir), seconds).SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo).SetDelay(delay).SetUpdate(true));
        }

        private void Blink()
        {
            var eyes = P("RobotEyes");
            if (eyes == null) return;
            var t = DOVirtual.DelayedCall(Random.Range(2.5f, 5f), () =>
            {
                if (eyes == null) return;
                eyes.DOScaleY(0.1f, 0.07f).SetLoops(2, LoopType.Yoyo).SetUpdate(true).OnComplete(Blink);
            }).SetUpdate(true);
            _idle.Add(t);
        }

        // ------------------------------------------------------------------ taps
        private void WireTaps()
        {
            if (_wired) return;
            _wired = true;
            Tap("Wrench", t => Stunt(t, t.DOPunchRotation(new Vector3(0, 0, 28f), 0.6f, 8, 0.6f)));
            Tap("Hammer", t => Swing(t));
            Tap("Screwdriver", t => Stunt(t, t.DOPunchAnchorPos(new Vector2(46f, 0f), 0.5f, 6, 0.5f)));
            Tap("Saw", t => Stunt(t, t.DOShakeAnchorPos(0.5f, new Vector2(14f, 4f), 20, 90f)));
            Tap("Gear", t => Stunt(t, t.DOLocalRotate(new Vector3(0, 0, -360f), 0.9f, RotateMode.LocalAxisAdd).SetEase(Ease.OutCubic)));
            Tap("Toolbox", t => Stunt(t, t.DOPunchAnchorPos(new Vector2(0f, 36f), 0.5f, 4, 0.4f)));
            Tap("Drum", t => Stunt(t, t.DOPunchScale(new Vector3(0.12f, -0.1f, 0f), 0.5f, 6, 0.5f)));
            foreach (var can in new[] { "CanRed", "CanOrange", "CanTeal" }) Tap(can, t => Topple(t));
            Tap("RobotBody", t => RobotCheer());
        }

        private void Tap(string name, System.Action<RectTransform> stunt)
        {
            var rt = P(name);
            if (rt == null) return;
            var img = rt.GetComponent<Image>();
            if (img != null) img.raycastTarget = true;
            var btn = rt.gameObject.GetComponent<Button>() ?? rt.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() => stunt(rt));
        }

        private void Stunt(RectTransform rt, Tween t)
        {
            if (t != null) _stunts.Add(t.SetUpdate(true));
        }

        private void Swing(RectTransform hammer)
        {
            hammer.pivot = new Vector2(0.5f, 0.95f);
            _stunts.Add(hammer.DOLocalRotate(new Vector3(0, 0, -50f), 0.18f).SetEase(Ease.OutQuad).SetUpdate(true)
                .OnComplete(() => _stunts.Add(hammer.DOLocalRotate(_restRot["Hammer"], 0.5f).SetEase(Ease.OutBounce).SetUpdate(true))));
        }

        private void Topple(RectTransform can)
        {
            _stunts.Add(can.DOLocalRotate(new Vector3(0, 0, -80f), 0.25f).SetEase(Ease.OutQuad).SetUpdate(true)
                .OnComplete(() => _stunts.Add(can.DOLocalRotate(Vector3.zero, 0.45f).SetEase(Ease.OutBack).SetDelay(0.5f).SetUpdate(true))));
        }

        private void RobotCheer()
        {
            var l = P("RobotArmL"); var r = P("RobotArmR"); var body = P("RobotBody");
            if (l != null) _stunts.Add(l.DOLocalRotate(new Vector3(0, 0, -150f), 0.25f).SetLoops(6, LoopType.Yoyo).SetEase(Ease.InOutSine).SetUpdate(true));
            if (r != null) _stunts.Add(r.DOLocalRotate(new Vector3(0, 0, 150f), 0.25f).SetLoops(6, LoopType.Yoyo).SetEase(Ease.InOutSine).SetUpdate(true));
            if (body != null) _stunts.Add(body.DOPunchScale(new Vector3(0.06f, 0.1f, 0f), 0.6f, 6, 0.5f).SetUpdate(true));
        }

        private void KillAll()
        {
            foreach (var t in _idle) if (t != null && t.IsActive()) t.Kill();
            foreach (var t in _stunts) if (t != null && t.IsActive()) t.Kill();
            _idle.Clear(); _stunts.Clear();
        }
    }
}
