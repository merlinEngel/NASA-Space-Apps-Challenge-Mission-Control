using Newtonsoft.Json;

namespace MissionCore
{
    public class InstrumentSpec
    {
        [JsonProperty("id")] public string id;
        [JsonProperty("name")] public string name;
        [JsonProperty("source")] public string source;
        [JsonProperty("mass_kg")] public double massKg;
        [JsonProperty("power_W")] public double powerW;
        [JsonProperty("data_rate_class")] public string dataRateClass;
        [JsonProperty("price_USD")] public double priceUSD;
        // Data rate while the instrument is measuring.
        [JsonProperty("data_rate_bps")] public double dataRateBpS;
        // Share of the time the instrument is measuring (0..1).
        [JsonProperty("duty_cycle")] public double dutyCycle;
    }
}