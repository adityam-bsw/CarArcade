using System;
using UnityEngine;

namespace RoadBuilderGame.UI
{
    /// <summary>
    /// Base class for every screen and popup prefab. Screens only display and raise events; they never run
    /// gameplay logic. Transitions are a hook (instant by default) so the Animation layer can supply DOTween ones.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class UIScreen : MonoBehaviour
    {
        private CanvasGroup _group;

        /// <summary>Popups sit on top of the screen below and leave it visible.</summary>
        public virtual bool IsPopup => false;

        /// <summary>Whether Android Back / Escape closes this screen. Return false for the Home screen.</summary>
        public virtual bool BackCloses => true;

        protected CanvasGroup Group => _group != null ? _group : (_group = GetComponent<CanvasGroup>());

        /// <summary>Called when the screen becomes the top of the stack. args is whatever the opener passed.</summary>
        public virtual void OnShow(object args) { }

        /// <summary>Called when the screen is covered or removed.</summary>
        public virtual void OnHide() { }

        /// <summary>Called when the screen is the top and Back / Escape is pressed. Default closes it.</summary>
        public virtual void OnBackPressed() { Stack?.Pop(); }

        public ScreenStack Stack { get; internal set; }

        /// <summary>Show animation. Call done when finished. Default: instant.</summary>
        public virtual void PlayIn(Action done)
        {
            Group.alpha = 1f;
            Group.interactable = true;
            Group.blocksRaycasts = true;
            done?.Invoke();
        }

        /// <summary>Hide animation. Call done when finished. Default: instant.</summary>
        public virtual void PlayOut(Action done)
        {
            Group.interactable = false;
            Group.blocksRaycasts = false;
            done?.Invoke();
        }
    }
}
