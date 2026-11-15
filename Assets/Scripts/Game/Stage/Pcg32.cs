namespace RoadBuilderGame
{
    /// <summary>
    /// PCG32 (XSH-RR) generator as a small struct: no dependence on time, frame count or Unity's random class, so a seed always gives the same sequence.
    /// Fixed stream constant; the slot seed selects the sequence.
    /// </summary>
    public struct Pcg32
    {
        private const ulong Multiplier = 6364136223846793005UL;
        private const ulong Increment = (54UL << 1) | 1UL;
        private ulong _state;

        public Pcg32(ulong seed)
        {
            _state = 0UL;
            NextUInt();
            unchecked { _state += seed; }
            NextUInt();
        }

        public uint NextUInt()
        {
            unchecked
            {
                ulong old = _state;
                _state = old * Multiplier + Increment;
                uint xorShifted = (uint)(((old >> 18) ^ old) >> 27);
                int rot = (int)(old >> 59);
                return (xorShifted >> rot) | (xorShifted << ((-rot) & 31));
            }
        }

        /// <summary>Unbiased integer in [0, maxExclusive). Returns 0 when maxExclusive is 1 or less.</summary>
        public int NextInt(int maxExclusive)
        {
            if (maxExclusive <= 1) return 0;
            uint bound = (uint)maxExclusive;
            uint threshold = unchecked((uint)(-(int)bound)) % bound;
            while (true)
            {
                uint r = NextUInt();
                if (r >= threshold) return (int)(r % bound);
            }
        }

        /// <summary>Integer in [minInclusive, maxInclusive].</summary>
        public int NextRange(int minInclusive, int maxInclusive)
        {
            if (maxInclusive <= minInclusive) return minInclusive;
            return minInclusive + NextInt(maxInclusive - minInclusive + 1);
        }

        /// <summary>Float in [0, 1).</summary>
        public float NextFloat01() { return (NextUInt() >> 8) * (1f / 16777216f); }
    }
}
