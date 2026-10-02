using Newtonsoft.Json;

namespace MissionCore
{
    public class BatterySpec
    {
        [JsonProperty("id")] public string id;
        [JsonProperty("name")] public string name;
        [JsonProperty("source")] public string source;
        [JsonProperty("energy_Wh")] public double energyWh;
        [JsonProperty("mass_kg")] public double massKg;
        [JsonProperty("max_depth_of_discharge")] public double maxDepthOfDischarge;
        [JsonProperty("price_USD")] public double priceUSD;
    }
}