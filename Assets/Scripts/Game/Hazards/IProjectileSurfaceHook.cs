using UnityEngine;

namespace RoadBuilderGame
{
    /// <summary>
    /// How a world projectile leaves its one persistent effect on the road (blueprint B2). Section B calls it where a projectile lands (<c>WorldProjectileRunner</c>);
    /// section C implements it (<see cref="SurfaceProjectileHook"/> over <see cref="SurfaceRuntime"/>) and decides what the effect means on the surface under it, from the data table.
    /// </summary>
    public interface IProjectileSurfaceHook
    {
        /// <param name="effect">The projectile's <see cref="ProjectileSurfaceEffect"/>.</param>
        /// <param name="at">World position where it landed.</param>
        /// <param name="radiusM">Half width of the patch in metres.</param>
        /// <param name="seconds">How long the patch lasts; 0 for an instant effect (crater, break).</param>
        /// <param name="projectileId">The projectile definition id, for the interaction table.</param>
        void OnProjectileSurface(ProjectileSurfaceEffect effect, Vector2 at, float radiusM, float seconds, string projectileId);
        /// <summary>The runner's patch ended early (reset, respawn); the hook may drop a patch of this effect.</summary>
        void OnProjectileSurfaceCleared(ProjectileSurfaceEffect effect);
    }

    /// <summary>The default: does nothing.</summary>
    public sealed class NoOpProjectileSurfaceHook : IProjectileSurfaceHook
    {
        public static readonly NoOpProjectileSurfaceHook Instance = new NoOpProjectileSurfaceHook();
        public void OnProjectileSurface(ProjectileSurfaceEffect effect, Vector2 at, float radiusM, float seconds, string projectileId) { }
        public void OnProjectileSurfaceCleared(ProjectileSurfaceEffect effect) { }
    }

    /// <summary>The one place the current hook lives. Defaults to the no-op; the game manager installs the surface runtime hook once at start.</summary>
    public static class ProjectileSurfaceHooks
    {
        private static IProjectileSurfaceHook _current = NoOpProjectileSurfaceHook.Instance;
        public static IProjectileSurfaceHook Current { get { return _current; } set { _current = value ?? NoOpProjectileSurfaceHook.Instance; } }
    }
}
