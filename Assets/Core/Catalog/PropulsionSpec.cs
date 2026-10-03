using Newtonsoft.Json;

namespace MissionCore
{
    public class PropulsionSpec
    {
        [JsonProperty("id")] public string id;
        [JsonProperty("name")] public string name;
        [JsonProperty("source")] public string source;
        [JsonProperty("isp_s")] public double ispS;
        [JsonProperty("dry_mass_kg")] public double dryMassKg;
        [JsonProperty("price_USD")] public double priceUsd;
        [JsonProperty("thrust_N")] public double thrustN;
        [JsonProperty("power_W")] public double? powerW;
    }
}