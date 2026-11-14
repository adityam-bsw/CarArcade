using System;
using UnityEngine;

namespace RoadBuilderGame.UI
{
    /// <summary>UIScreen with DOTween transitions. Pick a style per prefab; popups default to Pop.</summary>
    public abstract class AnimatedScreen : UIScreen
    {
        [SerializeField] private ShowStyle showStyle = ShowStyle.SlideUp;

        [Tooltip("The part that moves/scales. Leave empty to animate the whole screen.")]
        [SerializeField] private RectTransform content;

        private Vector2 _restPos;
        private bool _restCaptured;

        private RectTransform Target => content != null ? content : (RectTransform)transform;

        private void CaptureRest()
        {
            if (_restCaptured) return;
            _restPos = Target.anchoredPosition;
            _restCaptured = true;
        }

        public override void PlayIn(Action done)
        {
            CaptureRest();
            UIAnim.Show(Group, Target, _restPos, showStyle, done);
        }

        public override void PlayOut(Action done)
        {
            CaptureRest();
            UIAnim.Hide(Group, done);
        }

        protected virtual void OnDisable()
        {
            // A screen deactivated mid-transition must not keep a half-faded state for its next show.
            if (_restCaptured) Target.anchoredPosition = _restPos;
        }
    }
}
