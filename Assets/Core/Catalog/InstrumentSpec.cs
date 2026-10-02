using Newtonsoft.Json;

namespace MissionCore
{
    public class InstrumentSpec
    {
        [JsonProperty("id")] public string id;
        [JsonProperty("name")] public string name;
        [JsonProperty("source")] public string source;
        [JsonProperty("mass_kg")] public double massKG;
        [JsonProperty("power_W")] public double powerW;
        [JsonProperty("data_rate_class")] public string dataRateClass;
    }
}