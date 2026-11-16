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
        [Tooltip("Gold smooth star. Not used for particles (ParticleManager tints, so it uses starWhite).")]
        public Sprite star;
        [Tooltip("Neutral white star: tint-ready, used by ParticleManager for every burst colour in a Smooth world.")]
        public Sprite starWhite;
        public Sprite gem, magnet, shield, doublePoints, slowMo, ghost;

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

        /// <summary>Smooth pickup by its Resources/RoadBuilder file name (Coin, Heart, InkCanister, Pickup_Ring, Pickup_Gem/Magnet/Shield/Double/SlowMo/Ghost); null if it has none.</summary>
        public Sprite Pickup(string name)
        {
            switch (name) { case "Coin": return coin; case "Heart": return heart; case "InkCanister": return ink; case "Pickup_Ring": return ring;
                case "Pickup_Gem": return gem; case "Pickup_Magnet": return magnet; case "Pickup_Shield": return shield;
                case "Pickup_Double": return doublePoints; case "Pickup_SlowMo": return slowMo; case "Pickup_Ghost": return ghost;
                default: return null; }
        }
    }
}
