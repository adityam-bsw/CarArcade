using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoadBuilderGame.UI
{
    /// <summary>In-run HUD: distance, speed, coins, lives, ink and the boost button. Never covers the drawing area.</summary>
    public class HudScreen : AnimatedScreen
    {
        [SerializeField] private TMP_Text distanceText;
        [SerializeField] private TMP_Text speedText;
        [SerializeField] private TMP_Text coinsText;
        [SerializeField] private Image[] hearts;
        [SerializeField] private RectTransform inkBar;
        [SerializeField] private RectTransform inkFill;
        [SerializeField] private TMP_Text inkLabel;
        [SerializeField] private Button boostButton;

        private const float LowInk = 0.2f;

        private int _distance = -1, _speed = -1, _coins = -1, _lives = -1, _inkPercent = -1;
        private bool _lowPulse;

        public override bool BackCloses => false;

        private void Awake()
        {
            boostButton.onClick.AddListener(() => GameServices.Actions?.Boost());
        }

        public override void OnShow(object args)
        {
            _distance = _speed = _coins = _lives = _inkPercent = -1;
            if (_lowPulse) { UIAnim.StopPulse(inkBar); _lowPulse = false; }
            GameServices.Model.HudChanged += Refresh;
            Refresh();
        }

        public override void OnHide()
        {
            GameServices.Model.HudChanged -= Refresh;
            if (_lowPulse) { UIAnim.StopPulse(inkBar); _lowPulse = false; }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            GameServices.Model.HudChanged -= Refresh;
        }

        // Runs every frame while playing, so it only touches a label when its value actually changed.
        private void Refresh()
        {
            var m = GameServices.Model;
            var theme = UIThemeManager.Current;

            int d = Mathf.FloorToInt(m.Distance);
            if (d != _distance) { _distance = d; distanceText.SetText("{0} m", d); }

            int s = Mathf.RoundToInt(m.SpeedKmh);
            if (s != _speed) { _speed = s; speedText.SetText("{0} km/h", s); }

            int c = m.Coins + m.SessionCoins;
            if (c != _coins)
            {
                bool gained = _coins >= 0 && c > _coins;
                _coins = c;
                coinsText.SetText("{0}", c);
                if (gained) UIAnim.Punch(coinsText.transform.parent, 0.12f);
            }

            if (m.Lives != _lives)
            {
                int previous = _lives;
                _lives = m.Lives;
                for (int i = 0; i < hearts.Length; i++)
                {
                    bool full = i < m.Lives;
                    hearts[i].color = full ? Color.white : new Color(0.2f, 0.2f, 0.25f, 0.35f);
                    if (previous >= 0 && full != (i < previous)) UIAnim.Punch(hearts[i].transform, 0.4f);
                }
            }

            int ink = Mathf.RoundToInt(m.Ink01 * 100f);
            if (ink != _inkPercent)
            {
                _inkPercent = ink;
                inkFill.anchorMax = new Vector2(Mathf.Max(0.02f, m.Ink01), 1f);
                inkLabel.SetText("ROAD INK {0}%", ink);

                bool low = m.Ink01 < LowInk;
                if (low && !_lowPulse) { _lowPulse = true; UIAnim.StartPulse(inkBar, 1.05f); }
                else if (!low && _lowPulse) { _lowPulse = false; UIAnim.StopPulse(inkBar); }
            }
        }
    }
}
