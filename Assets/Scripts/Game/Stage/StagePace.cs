using UnityEngine;

namespace RoadBuilderGame
{
    /// <summary>
    /// The brief's speed curve (start speed, linear gain per metre, cap) in closed form, to turn "12 seconds of clear road" into metres.
    /// Speed v(d) = min(cap, start + gain * d); while below the cap dv/dt = gain * v, so v grows exponentially in time.
    /// </summary>
    public readonly struct StagePace
    {
        public readonly float StartMps, GainMpsPerM, CapMps;
        public StagePace(float startMps, float gainMpsPerM, float capMps) { StartMps = startMps; GainMpsPerM = gainMpsPerM; CapMps = capMps; }

        public float SpeedAt(float distanceM)
        {
            return Mathf.Min(CapMps, StartMps + GainMpsPerM * Mathf.Max(0f, distanceM));
        }

        /// <summary>Metres covered in <paramref name="seconds"/> starting at <paramref name="fromM"/>. <paramref name="speedFactor"/> scales every speed (ice 1.15).</summary>
        public float MetresForSeconds(float fromM, float seconds, float speedFactor)
        {
            if (seconds <= 0f) return 0f;
            float t = seconds * Mathf.Max(0.01f, speedFactor);      // base-curve seconds that cover the same metres
            float v = SpeedAt(fromM);
            if (GainMpsPerM <= 0f || v >= CapMps) return v * t;
            float tCap = Mathf.Log(CapMps / v) / GainMpsPerM;
            if (t <= tCap) return (v * Mathf.Exp(GainMpsPerM * t) - v) / GainMpsPerM;
            return (CapMps - v) / GainMpsPerM + CapMps * (t - tCap);
        }

        /// <summary>Seconds to drive from <paramref name="fromM"/> to <paramref name="toM"/>.</summary>
        public float SecondsBetween(float fromM, float toM, float speedFactor)
        {
            if (toM <= fromM) return 0f;
            float f = Mathf.Max(0.01f, speedFactor);
            float v0 = SpeedAt(fromM);
            float baseT;
            if (GainMpsPerM <= 0f || v0 >= CapMps) baseT = (toM - fromM) / v0;
            else
            {
                float dCap = (CapMps - StartMps) / GainMpsPerM;
                float rampEnd = Mathf.Min(toM, dCap);
                baseT = Mathf.Log(SpeedAt(rampEnd) / v0) / GainMpsPerM;
                if (toM > dCap) baseT += (toM - Mathf.Max(fromM, dCap)) / CapMps;
            }
            return baseT / f;
        }
    }
}
