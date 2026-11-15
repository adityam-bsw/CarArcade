using System;
using UnityEngine;

namespace RoadBuilderGame
{
    public enum ProfileAxis { Ink = 0, Flip = 1, Boost = 2, Impact = 3, Speed = 4, Torque = 5, Accel = 6 }

    /// <summary>One vehicle's strength and weakness as multipliers on the run (1 = the balanced starter). Data only; RoadGameManager applies it at run start.</summary>
    [Serializable]
    public class VehicleProfile
    {
        public string vehicleId;
        [Tooltip("Ink cost per metre (below 1 = cheaper road).")] public float ink = 1f;
        [Tooltip("Time upside down before the flip loses a life (above 1 = forgiving).")] public float flip = 1f;
        [Tooltip("Boost duration.")] public float boost = 1f;
        [Tooltip("Impact resistance: the no-damage window after a hit (above 1 = tougher).")] public float impact = 1f;
        [Tooltip("Target speed.")] public float speed = 1f;
        [Tooltip("Wheel torque (climbing).")] public float torque = 1f;
        [Tooltip("How fast the run speed ramps up with distance.")] public float accel = 1f;
        [Tooltip("Crushes fragile roads (monster truck, tank). Read by the surface modules.")] public bool crushesFragile;
        [Tooltip("Mastery goal counter id and target for this vehicle.")] public string goalId = "clean-landings";
        public int goalTarget = 10;

        public float Get(ProfileAxis a)
        {
            switch (a) { case ProfileAxis.Ink: return ink; case ProfileAxis.Flip: return flip; case ProfileAxis.Boost: return boost; case ProfileAxis.Impact: return impact; case ProfileAxis.Speed: return speed; case ProfileAxis.Torque: return torque; default: return accel; }
        }

        /// <summary>Value where higher is always better (ink is inverted).</summary>
        public float Goodness(ProfileAxis a) { float v = Get(a); return a == ProfileAxis.Ink ? 1f / Mathf.Max(0.01f, v) : v; }
    }

    /// <summary>The resolved multipliers for one run (profile plus owned scrap upgrades). Plain struct, no allocation.</summary>
    public struct VehicleStats
    {
        public float ink, flip, boost, impact, speed, torque, accel;
        public bool crushesFragile;
        public static VehicleStats Identity => new VehicleStats { ink = 1f, flip = 1f, boost = 1f, impact = 1f, speed = 1f, torque = 1f, accel = 1f };
    }

    /// <summary>Per-vehicle strength and weakness profiles (blueprint D2): the next vehicle is never strictly better than the last, each wants a different drawing strategy.</summary>
    [CreateAssetMenu(menuName = "Road Builder/Progression/Vehicle Profiles", fileName = "VehicleProfiles")]
    public class VehicleProfileSO : ScriptableObject
    {
        public VehicleProfile[] profiles = Defaults();
        private static VehicleProfileSO _cached;
        public static VehicleProfileSO Load() { if (_cached == null) { _cached = Resources.Load<VehicleProfileSO>("Progression/VehicleProfiles"); if (_cached == null) _cached = CreateInstance<VehicleProfileSO>(); } return _cached; }

        public VehicleProfile Find(string id)
        {
            if (profiles != null && !string.IsNullOrEmpty(id)) for (int i = 0; i < profiles.Length; i++) if (profiles[i].vehicleId == id) return profiles[i];
            return null;
        }

        /// <summary>The axis where this vehicle is weakest (what a scrap upgrade reinforces).</summary>
        public static ProfileAxis WeakAxis(VehicleProfile p)
        {
            var worst = ProfileAxis.Ink; float w = float.MaxValue;
            for (int i = 0; i < 7; i++) { float g = p.Goodness((ProfileAxis)i); if (g < w) { w = g; worst = (ProfileAxis)i; } }
            return worst;
        }

        public static ProfileAxis StrongAxis(VehicleProfile p)
        {
            var best = ProfileAxis.Ink; float w = float.MinValue;
            for (int i = 0; i < 7; i++) { float g = p.Goodness((ProfileAxis)i); if (g > w) { w = g; best = (ProfileAxis)i; } }
            return best;
        }

        public static string StrengthText(ProfileAxis a)
        {
            switch (a) { case ProfileAxis.Ink: return "CHEAP INK"; case ProfileAxis.Flip: return "STAYS UPRIGHT"; case ProfileAxis.Boost: return "LONG BOOST"; case ProfileAxis.Impact: return "TOUGH"; case ProfileAxis.Speed: return "FAST"; case ProfileAxis.Torque: return "STRONG CLIMB"; default: return "QUICK START"; }
        }

        public static string WeaknessText(ProfileAxis a)
        {
            switch (a) { case ProfileAxis.Ink: return "COSTLY INK"; case ProfileAxis.Flip: return "TIPS EASILY"; case ProfileAxis.Boost: return "SHORT BOOST"; case ProfileAxis.Impact: return "FRAGILE"; case ProfileAxis.Speed: return "SLOW"; case ProfileAxis.Torque: return "WEAK CLIMB"; default: return "SLOW START"; }
        }

        /// <summary>True when a is at least as good as b on every axis and better on one (the situation the design forbids).</summary>
        public static bool StrictlyBetter(VehicleProfile a, VehicleProfile b)
        {
            bool better = false;
            for (int i = 0; i < 7; i++)
            {
                float ga = a.Goodness((ProfileAxis)i), gb = b.Goodness((ProfileAxis)i);
                if (ga < gb - 1e-4f) return false;
                if (ga > gb + 1e-4f) better = true;
            }
            return better;
        }

        // Goal ids: L clean-landings, F fragile-breaks, N near-misses, G low-gravity-stages.
        private static VehicleProfile P(string id, float ink, float flip, float boost, float impact, float speed, float torque, float accel, char goal, bool crush = false)
        {
            string g = goal == 'F' ? "fragile-breaks" : goal == 'N' ? "near-misses" : goal == 'G' ? "low-gravity-stages" : "clean-landings";
            int t = goal == 'F' ? 20 : goal == 'N' ? 15 : goal == 'G' ? 5 : 10;
            return new VehicleProfile { vehicleId = id, ink = ink, flip = flip, boost = boost, impact = impact, speed = speed, torque = torque, accel = accel, goalId = g, goalTarget = t, crushesFragile = crush };
        }

        //                                          ink   flip  boost impact speed torque accel goal
        public static VehicleProfile[] Defaults()
        {
            return new[]
            {
                P("vehicle-car",           1.00f, 1.00f, 1.00f, 1.00f, 1.00f, 1.00f, 1.00f, 'L'),   // balanced starter
                P("vehicle-sports-car",    1.00f, 0.90f, 1.00f, 0.85f, 1.08f, 1.00f, 1.05f, 'N'),   // fast, fragile
                P("vehicle-police-car",    1.10f, 1.00f, 1.00f, 1.00f, 1.05f, 1.05f, 1.00f, 'N'),
                P("vehicle-taxi",          0.92f, 1.00f, 1.00f, 1.05f, 0.96f, 1.00f, 1.00f, 'L'),
                P("vehicle-tuk-tuk",       0.90f, 0.85f, 1.00f, 1.05f, 1.00f, 0.90f, 1.00f, 'L'),
                P("vehicle-golf-cart",     0.90f, 1.10f, 1.00f, 1.00f, 0.90f, 1.00f, 1.00f, 'L'),
                P("vehicle-dune-buggy",    1.05f, 1.05f, 0.95f, 1.00f, 1.00f, 1.10f, 1.00f, 'L'),   // jeep: balanced offroad
                P("vehicle-race-car",      1.00f, 0.90f, 1.00f, 0.80f, 1.12f, 1.10f, 1.00f, 'N'),   // rally: speed and grip, weak impact
                P("vehicle-pickup",        1.10f, 1.00f, 1.00f, 1.15f, 0.93f, 1.10f, 1.00f, 'F'),
                P("vehicle-tractor",       1.00f, 1.00f, 0.90f, 1.10f, 0.80f, 1.25f, 0.80f, 'F'),
                P("vehicle-camper",        1.10f, 1.10f, 1.00f, 1.10f, 0.88f, 1.00f, 0.85f, 'F'),
                P("vehicle-monster-truck", 1.25f, 1.15f, 1.00f, 1.25f, 0.95f, 1.15f, 1.00f, 'F', true),   // crushes fragile roads, high ink
                P("vehicle-bus",           1.30f, 1.15f, 1.10f, 1.20f, 0.85f, 1.00f, 0.80f, 'F'),
                P("vehicle-fire-truck",    1.25f, 0.95f, 1.15f, 1.20f, 0.88f, 1.00f, 1.00f, 'F'),
                P("vehicle-semi-truck",    1.35f, 0.80f, 1.00f, 1.30f, 0.85f, 1.20f, 0.75f, 'F'),
                P("vehicle-tank",          1.15f, 1.40f, 0.85f, 1.60f, 0.80f, 1.20f, 0.85f, 'F', true),   // heavy, durable, slow
                P("vehicle-bicycle",       0.70f, 0.75f, 0.90f, 0.80f, 0.95f, 1.00f, 1.00f, 'L'),   // low ink, easy flip
                P("vehicle-scooter",       0.80f, 0.85f, 1.00f, 1.00f, 0.92f, 1.00f, 1.10f, 'L'),
                P("vehicle-skateboard",    0.65f, 0.60f, 1.00f, 0.70f, 1.00f, 1.00f, 1.20f, 'L'),
                P("vehicle-motorcycle",    0.90f, 0.70f, 1.00f, 0.75f, 1.10f, 1.00f, 1.15f, 'L'),
                P("vehicle-quad-atv",      1.10f, 1.00f, 1.00f, 0.90f, 0.97f, 1.20f, 1.00f, 'L'),
                P("vehicle-sled",          0.75f, 1.00f, 1.00f, 0.85f, 1.05f, 0.70f, 1.00f, 'L'),
                P("vehicle-toboggan",      0.80f, 1.10f, 1.00f, 1.00f, 1.03f, 0.75f, 1.00f, 'L'),
                P("vehicle-snowboard",     0.70f, 0.70f, 1.00f, 0.80f, 1.00f, 1.00f, 1.15f, 'L'),
                P("vehicle-snowmobile",    1.20f, 1.00f, 1.00f, 0.90f, 1.10f, 1.10f, 1.00f, 'N'),
                P("vehicle-hovercraft",    0.90f, 1.50f, 1.05f, 0.85f, 1.00f, 0.65f, 1.00f, 'L'),   // floats, weak climbs
                P("vehicle-jet-ski",       1.00f, 0.85f, 1.25f, 0.80f, 1.10f, 0.80f, 1.00f, 'N'),
                P("vehicle-speedboat",     1.15f, 0.90f, 1.20f, 0.90f, 1.15f, 0.75f, 1.00f, 'N'),
                P("vehicle-handcar",       0.80f, 1.00f, 0.80f, 1.10f, 0.85f, 1.00f, 0.90f, 'L'),
                P("vehicle-moon-rover",    0.95f, 1.35f, 1.00f, 1.00f, 1.00f, 0.90f, 0.60f, 'G'),   // low gravity floaty, poor accel
                P("vehicle-rocket-sled",   1.30f, 0.70f, 1.30f, 0.70f, 1.20f, 0.80f, 1.20f, 'N'),
                P("vehicle-shopping-cart", 0.75f, 0.80f, 1.10f, 0.85f, 0.90f, 1.00f, 1.00f, 'L'),
            };
        }
    }
}
