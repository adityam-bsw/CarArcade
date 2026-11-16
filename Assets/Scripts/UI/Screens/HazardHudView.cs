using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoadBuilderGame.UI
{
    /// <summary>
    /// Boss and Road-Eater HUD layer (gap list B): the counter meter on the screen edge with phase and hits, a vignette that warms with danger, and the announcer subtitle.
    /// Reads <see cref="GameModel"/> only and holds no rules. Instantiated under the HUD from the Resources prefab UI/HazardHud; every colour is a UITheme token.
    /// </summary>
    public class HazardHudView : MonoBehaviour
    {
        [SerializeField] private Image vignette;
        [SerializeField] private RectTransform meterRoot;
        [SerializeField] private Image meterFill;
        [SerializeField] private TMP_Text tierText, phaseText, hitsText, subtitleText;
        [SerializeField] private CanvasGroup subtitleGroup;

        private int _subId = -1; private float _subLeft;
        private float _vig;

        private void OnEnable()
        {
            GameServices.Model.HudChanged += Refresh;
            _subId = GameServices.Model.SubtitleId; _subLeft = 0f; Hide();
        }

        private void OnDisable() { if (GameServices.Model != null) GameServices.Model.HudChanged -= Refresh; }

        private void Hide()
        {
            if (meterRoot != null) meterRoot.gameObject.SetActive(false);
            if (subtitleGroup != null) subtitleGroup.alpha = 0f;
            SetVig(0f, Color.clear);
        }

        private void SetVig(float a, Color c) { if (vignette == null) return; c.a = a; vignette.color = c; }

        private void Update()
        {
            if (Time.timeScale <= 0f) return;                     // paused: the subtitle clock stands still with the run
            if (_subLeft > 0f) { _subLeft -= Time.unscaledDeltaTime; if (_subLeft <= 0f && subtitleGroup != null) subtitleGroup.alpha = 0f; }
        }

        private void Refresh()
        {
            var m = GameServices.Model; var theme = UIThemeManager.Current; if (theme == null) return;
            bool boss = m.BossOn;
            if (meterRoot != null && meterRoot.gameObject.activeSelf != boss) meterRoot.gameObject.SetActive(boss);
            if (boss)
            {
                if (meterFill != null) { meterFill.fillAmount = m.BossOverload01; meterFill.color = theme.GetColor(m.BossPhase >= 3 ? ColorToken.Danger : m.BossPhase == 2 ? ColorToken.Coin : ColorToken.Ink); }
                if (tierText != null) tierText.SetText(m.BossTier);
                if (phaseText != null) phaseText.SetText("PHASE {0}", m.BossPhase);
                if (hitsText != null) hitsText.SetText("HITS {0}/{1}", m.BossHits, m.BossMaxHits);
            }
            // vignette: boss phase 3 or a close Road-Eater (danger or snap band) warms the screen edge; calm is clear
            float target = m.EaterOn ? (m.EaterBand >= 3 ? 0.5f : m.EaterBand == 2 ? 0.3f : 0f) : boss ? (m.BossPhase >= 3 ? 0.22f : 0f) : 0f;
            _vig = Mathf.MoveTowards(_vig, target, 2f * (Time.timeScale <= 0f ? 0f : Time.unscaledDeltaTime));
            SetVig(_vig, theme.GetColor(ColorToken.Danger));
            if (subtitleText != null && m.SubtitleId != _subId)
            {
                _subId = m.SubtitleId; subtitleText.SetText(m.Subtitle); _subLeft = Mathf.Max(1f, m.SubtitleSeconds);
                if (subtitleGroup != null) subtitleGroup.alpha = 1f;
            }
        }
    }
}
