using Newtonsoft.Json;

namespace MissionCore
{
    public class BalanceRules
    {
        // Short C# name for the long JSON key
        [JsonProperty("dry_mass_fractions_earth_orbit_with_propulsion")]
        public MassFractions DryMassFractions { get; set; }

        public double MassMarginEarlyPhase { get; set; }    // 0.25 = +25 %
        public double PowerMarginEarlyPhase { get; set; }
        public string Source { get; set; }
    }

    // Share of each subsystem in the dry mass (all together = 1.0)
    public class MassFractions
    {
        public double Payload { get; set; }
        public double Structure { get; set; }
        public double Thermal { get; set; }
        public double Power { get; set; }
        public double Communications { get; set; }
        public double AttitudeControl { get; set; }
        public double Computer { get; set; }
        public double Propulsion { get; set; }
        public double Other { get; set; }
    }
}