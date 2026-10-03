using Newtonsoft.Json;

namespace MissionCore
{
    public class CommsSpec
    {
        [JsonProperty("id")] public string id;
        [JsonProperty("name")] public string name;
        [JsonProperty("source")] public string source;
        [JsonProperty("band")] public string band;
        [JsonProperty("frequency_Hz")] public double frequencyHz;
        [JsonProperty("data_rate_bps")] public double dataRateBpS;
        [JsonProperty("power_W")] public double powerW;
        [JsonProperty("mass_kg")] public double massKg;
        [JsonProperty("price_USD")] public double priceUSD;
    }
}