using UnityEngine;

namespace RoadBuilderGame
{
    /// <summary>
    /// Smooth sprites for the three slice vehicles (body and own wheel each) and the pickups. Data only: the asset
    /// Resources/Smooth/SmoothSliceArt references the files in Assets/Art/Smooth, so nothing is duplicated. Callers use it
    /// only when the on-screen theme is ArtStyle.Smooth and fall back to the original Resources/RoadBuilder sprites for any
    /// entry that is missing. Sprites are always fitted to the same world sizes as the pixel ones, so size and colliders never change.
    /// </summary>
    [CreateAssetMenu(menuName = "Road Builder/Smooth Slice Art", fileName = "SmoothSliceArt")]
    public class SmoothSliceArt : ScriptableObject
    {
        public Sprite bodyClassic, bodySport, bodyMonster;
        public Sprite wheelClassic, wheelSport, wheelMonster;
        public Sprite coin, heart, ink, ring;
        [Tooltip("Not used by any pickup yet (no Overload state in code); kept so it is ready when one exists.")]
        public Sprite ringOverload;
        [Tooltip("Smooth star. NOT wired: ParticleManager tints its star per burst, which would muddy a coloured sprite.")]
        public Sprite star;

        private static SmoothSliceArt _loaded;
        private static bool _tried;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { _loaded = null; _tried = false; }

        public static SmoothSliceArt Load()
        {
            if (!_tried || _loaded == null) { _tried = true; _loaded = Resources.Load<SmoothSliceArt>("Smooth/SmoothSliceArt"); }
            return _loaded;
        }

        public Sprite Body(VehicleType t)
        {
            switch (t) { case VehicleType.SportCyan: return bodySport; case VehicleType.MonsterTruck: return bodyMonster; default: return bodyClassic; }
        }

        public Sprite Wheel(VehicleType t)
        {
            switch (t) { case VehicleType.SportCyan: return wheelSport; case VehicleType.MonsterTruck: return wheelMonster; default: return wheelClassic; }
        }

        /// <summary>Smooth pickup by its Resources/RoadBuilder file name (Coin, Heart, InkCanister, Pickup_Ring); null if it has none.</summary>
        public Sprite Pickup(string name)
        {
            switch (name) { case "Coin": return coin; case "Heart": return heart; case "InkCanister": return ink; case "Pickup_Ring": return ring; default: return null; }
        }
    }
}
