using System;

namespace XOPlayable.Core
{
    /// <summary>Instance-owned pseudo-random stream eliminating hidden global RNG state.</summary>
    public sealed class XORandomStream
    {
        private Random _rng;

        public int Seed { get; private set; }

        public XORandomStream()
        {
            Seed = Environment.TickCount;
            _rng = new Random(Seed);
        }

        public XORandomStream(int seed)
        {
            Seed = seed;
            _rng = new Random(seed);
        }

        public void Reseed(int seed)
        {
            Seed = seed;
            _rng = new Random(seed);
        }

        public int Range(int minInclusive, int maxExclusive)
        {
            return (minInclusive >= maxExclusive) ? minInclusive : _rng.Next(minInclusive, maxExclusive);
        }

        public float Range(float minInclusive, float maxInclusive)
        {
            return (minInclusive >= maxInclusive)
                ? minInclusive
                : (float)(minInclusive + _rng.NextDouble() * (maxInclusive - minInclusive));
        }

        public float Value => (float)_rng.NextDouble();
    }
}

