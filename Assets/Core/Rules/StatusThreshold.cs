using Newtonsoft.Json;

namespace MissionCore
{
    public class StatusThreshold
    {
        [JsonProperty("green_min_reserve")]
        public double GreenMinReserve { get; set; }

        [JsonProperty("yellow_min_reserve")]
        public double YellowMinReserve { get; set; }
    }
}