using Newtonsoft.Json;

namespace MissionCore
{
    public class SolarCellSpec
    {
        [JsonProperty("id")] public string id;
        [JsonProperty("name")] public string name;
        [JsonProperty("source")] public string source;
        [JsonProperty("efficiency")] public double efficiency;
        [JsonProperty("packing_factor")] public double packingFactor;
        [JsonProperty("mass_per_area_kg_m2")] public double massPerAreaKgM2;
        [JsonProperty("degradation_per_year")] public double degradationPerYear;
        [JsonProperty("price_per_area_USD_m2")] public double pricePerAreaUSDM2;
    }
}