using System;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace RoadBuilderGame.UI
{
    public enum ShowStyle { Fade, SlideUp, Pop }

    /// <summary>
    /// The one place UI tweens are written (rb-dotween rules): every tween is linked to its GameObject, uses
    /// unscaled time, and kills what it replaces. Durations and eases follow rb-motion-design.
    /// </summary>
    public static class UIAnim
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Boot()
        {
            DOTween.Init(true, true, LogBehaviour.ErrorsOnly).SetCapacity(200, 50);
            DOTween.defaultEaseType = Ease.OutCubic;
        }

        /// <summary>Screen entrance. Interactable immediately so taps are never swallowed by the animation.</summary>
        public static void Show(CanvasGroup cg, RectTransform rt, Vector2 restPos, ShowStyle style, Action done)
        {
            cg.DOKill();
            rt.DOKill();
            cg.interactable = true;
            cg.blocksRaycasts = true;
            rt.anchoredPosition = restPos;
            rt.localScale = Vector3.one;

            var seq = DOTween.Sequence().SetUpdate(true).SetLink(cg.gameObject);
            switch (style)
            {
                case ShowStyle.SlideUp:
                    cg.alpha = 0f;
                    rt.anchoredPosition = restPos + new Vector2(0f, -48f);
                    seq.Join(cg.DOFade(1f, 0.22f));
                    seq.Join(rt.DOAnchorPos(restPos, 0.3f).SetEase(Ease.OutCubic));
                    break;
                case ShowStyle.Pop:
                    cg.alpha = 0f;
                    rt.localScale = Vector3.one * 0.85f;
                    seq.Join(cg.DOFade(1f, 0.16f));
                    seq.Join(rt.DOScale(1f, 0.28f).SetEase(Ease.OutBack));
                    break;
                default:
                    cg.alpha = 0f;
                    seq.Append(cg.DOFade(1f, 0.2f));
                    break;
            }
            if (done != null) seq.OnComplete(() => done());
        }

        /// <summary>Screen exit: faster than the entrance and non-interactive straight away.</summary>
        public static void Hide(CanvasGroup cg, Action done)
        {
            cg.DOKill();
            cg.interactable = false;
            cg.blocksRaycasts = false;
            cg.DOFade(0f, 0.15f).SetEase(Ease.InCubic).SetUpdate(true).SetLink(cg.gameObject)
                .OnComplete(() => done?.Invoke());
        }

        public static void Press(Transform tr, float scale)
        {
            tr.DOKill();
            tr.DOScale(scale, 0.08f).SetEase(Ease.OutQuad).SetUpdate(true).SetLink(tr.gameObject);
        }

        public static void Release(Transform tr)
        {
            tr.DOKill();
            tr.DOScale(1f, 0.25f).SetEase(Ease.OutBack).SetUpdate(true).SetLink(tr.gameObject);
        }

        /// <summary>Quick scale punch for "this just changed" (a heart lost, a level gained).</summary>
        public static void Punch(Transform tr, float strength = 0.18f)
        {
            tr.DOKill(true);
            tr.DOPunchScale(Vector3.one * strength, 0.32f, 6, 0.6f).SetUpdate(true).SetLink(tr.gameObject);
        }

        /// <summary>Counts a number up in a TMP label. The format must contain {0}, for example "{0} m".</summary>
        public static Tween CountUp(TMP_Text label, int from, int to, float duration, string format = "{0}")
        {
            label.DOKill();
            int shown = from;
            label.SetText(format, from);
            return DOTween.To(() => shown, v =>
                {
                    shown = v;
                    label.SetText(format, v);
                }, to, duration)
                .SetEase(Ease.OutQuad).SetUpdate(true).SetLink(label.gameObject).SetTarget(label);
        }

        /// <summary>Endless gentle pulse. The caller owns it and must kill it (StopPulse) when done.</summary>
        public static void StartPulse(Transform tr, float scale = 1.08f)
        {
            tr.DOKill();
            tr.localScale = Vector3.one;
            tr.DOScale(scale, 0.35f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true).SetLink(tr.gameObject);
        }

        public static void StopPulse(Transform tr)
        {
            tr.DOKill();
            tr.localScale = Vector3.one;
        }
    }
}
