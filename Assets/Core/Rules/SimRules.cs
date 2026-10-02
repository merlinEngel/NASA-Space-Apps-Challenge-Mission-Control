using System.Collections.Generic;

namespace MissionCore
{
    // Values from sim_rules.json: fixed rules of the simulation loop.
    public class SimRules
    {
        // Length of one simulation tick in seconds
        public double TickS { get; set; }

        // RK4 steps per tick (6 -> 10 s each at a 60 s tick)
        public int IntegratorSubsteps { get; set; }

        public LoadSheddingRules LoadShedding { get; set; }

        // Keys are "low", "medium", "high"; use DensityFactor() instead of reading it directly.
        public Dictionary<string, double> SolarActivityDensityFactor { get; set; }

        public string Source { get; set; }

        public double DensityFactor(SolarActivity activity) =>
            SolarActivityDensityFactor[activity.ToSnakeCase()];
    }

    // Battery state of charge (0..1) at which instruments and safe mode switch.
    public class LoadSheddingRules
    {
        public double InstrumentsOffBelowSoc { get; set; }
        public double SafeModeBelowSoc { get; set; }
        public double InstrumentsOnAboveSoc { get; set; }
    }
}
