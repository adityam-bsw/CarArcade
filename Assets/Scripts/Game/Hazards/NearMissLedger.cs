using UnityEngine;

namespace RoadBuilderGame
{
    /// <summary>Result of one near miss decision. <see cref="Valid"/> false means nothing happens at all (guard failed); <see cref="Coins"/> may be 0 when the run cap blocked the payment (toast, sound and frenzy still fire).</summary>
    public struct NearMissPay
    {
        public bool Valid, Capped, Perfect;
        public int Coins, Chain;
    }

    /// <summary>
    /// Near-miss economy (brief F4, hazards.md 6): chain steps 8/10/12/15, perfect x2, Eater near miss 20, 12 percent run cap,
    /// lingering-farm guard. Pure C#, scaled time in, zero allocation. The numbers are the user-approved economy (brief section 14).
    /// </summary>
    public sealed class NearMissLedger
    {
        public const int SourceCount = 8;
        public int[] Steps = { 8, 10, 12, 15 };
        public int PerfectMultiplier = 2;
        public float ChainWindowS = 4f;
        public float RunCapFraction = 0.12f;
        public int EaterPay = 20;
        public float SourceCooldownS = 1.5f;
        public float MinTelegraphTravelM = 8f;
        public float MinRecentSpeedMps = 3f;
        public float RespawnGraceS = 1.5f;

        private float _clock, _chainLeft, _graceLeft, _damageFrameAt = -10f;
        private readonly float[] _lastBySource = new float[SourceCount];
        private int _chain;

        public int Paid { get; private set; }
        public int Count { get; private set; }

        public void Reset()
        {
            _clock = 0f; _chainLeft = 0f; _graceLeft = 0f; _damageFrameAt = -10f; _chain = 0; Paid = 0; Count = 0;
            for (int i = 0; i < _lastBySource.Length; i++) _lastBySource[i] = -100f;
        }

        public void Tick(float scaledDt)
        {
            _clock += scaledDt;
            if (_chainLeft > 0f) { _chainLeft -= scaledDt; if (_chainLeft <= 0f) _chain = 0; }
            if (_graceLeft > 0f) _graceLeft -= scaledDt;
        }

        /// <summary>A damage hit happened (voids a near miss resolved in the same frame).</summary>
        public void NoteDamage() { _damageFrameAt = _clock; }
        /// <summary>The car respawned: nothing pays for 1.5 s.</summary>
        public void NoteRespawn() { _graceLeft = RespawnGraceS; _chain = 0; _chainLeft = 0f; }

        /// <summary>The run cap formula: min(step, floor(0.12 / 0.88 x nonNearMissCoins) - paid), floored at 0.</summary>
        public int CapRoom(int nonNearMissCoins)
        {
            return Mathf.Max(0, Mathf.FloorToInt(RunCapFraction / (1f - RunCapFraction) * Mathf.Max(0, nonNearMissCoins)) - Paid);
        }

        /// <param name="source">hazard type index 0..7 for the per-source cooldown</param>
        /// <param name="telegraphTravelM">metres the car travelled along the road during that hazard's telegraph</param>
        /// <param name="recentSpeedMps">car speed averaged over the last second</param>
        /// <param name="nonNearMissCoins">coins earned this run that are not near-miss coins</param>
        public NearMissPay Resolve(int source, bool perfect, float telegraphTravelM, float recentSpeedMps, int nonNearMissCoins)
        {
            var r = new NearMissPay();
            if (_graceLeft > 0f || Mathf.Abs(_clock - _damageFrameAt) < 0.0001f) return r;
            if (source >= 0 && source < SourceCount && _clock - _lastBySource[source] < SourceCooldownS) return r;
            if (telegraphTravelM < MinTelegraphTravelM || recentSpeedMps < MinRecentSpeedMps) return r;
            if (source >= 0 && source < SourceCount) _lastBySource[source] = _clock;
            _chain = _chainLeft > 0f ? _chain + 1 : 1;
            _chainLeft = ChainWindowS;
            int step = Steps[Mathf.Min(_chain, Steps.Length) - 1];
            int want = perfect ? step * PerfectMultiplier : step;
            int pay = Mathf.Min(want, CapRoom(nonNearMissCoins));
            Paid += pay; Count++;
            r.Valid = true; r.Perfect = perfect; r.Chain = _chain; r.Coins = pay; r.Capped = pay < want;
            return r;
        }

        /// <summary>Eater near miss: 20 coins, same run cap, same grace; its own once-per-10-s cooldown lives in the model.</summary>
        public NearMissPay ResolveEater(int nonNearMissCoins)
        {
            var r = new NearMissPay();
            if (_graceLeft > 0f) return r;
            int pay = Mathf.Min(EaterPay, CapRoom(nonNearMissCoins));
            Paid += pay; Count++;
            r.Valid = true; r.Coins = pay; r.Capped = pay < EaterPay; r.Chain = 1;
            return r;
        }
    }
}
