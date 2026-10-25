using Newtonsoft.Json;

namespace MissionCore.Rules
{
    // Thresholds of the flight readiness review (the "review" group in balance_rules.json).
    public class ReviewRules
    {
        public double MinPowerReserve { get; set; }   // 0.10 = average generation at least 10 % above the need

        [JsonProperty("avoidance_delta_v_per_year_m_s")]
        public double AvoidanceDeltaVPerYearMPerS { get; set; }   // delta-v kept for collision avoidance

        public double DisposalPerigeeM { get; set; }  // perigee altitude the end-of-mission burn lowers the orbit to
    }
}
