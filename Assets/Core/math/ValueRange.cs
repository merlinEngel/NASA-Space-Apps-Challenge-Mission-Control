using System;

namespace MissionCore.Numerics
{
    [System.Serializable]
    public class ValueRange
    {
        public ValueRange(double min, double max)
        {
            Min = min;
            Max = max;
        }

        public double Min {get; set;}
        public double Max {get; set;}

        public bool Contains(double value) => Min <= value && value <= Max;

        // Uniform random value in [Min, Max) from the given generator (reproducible with a seed).
        public double Sample(Random random) => Min + random.NextDouble() * (Max - Min);
    }
}