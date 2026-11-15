namespace RoadBuilderGame
{
    /// <summary>The two vehicle facts surfaces care about: heavy (tears paper, smashes spikes, steadies a rope bridge) and metallic (feels the magnet rail). From mass and a short list of ids, no per-vehicle code in the modules.</summary>
    public static class SurfaceVehicleClass
    {
        public const float HeavyMass = 2.0f;
        private static readonly string[] NonMetal = { "vehicle-bicycle", "vehicle-skateboard", "vehicle-sled", "vehicle-toboggan", "vehicle-hovercraft", "vehicle-jet-ski" };
        public static bool IsHeavy(float bodyMass) { return bodyMass >= HeavyMass; }
        public static bool IsMetallic(string vehicleKey)
        {
            if (string.IsNullOrEmpty(vehicleKey)) return true;
            for (int i = 0; i < NonMetal.Length; i++) if (NonMetal[i] == vehicleKey) return false;
            return true;
        }
    }
}
