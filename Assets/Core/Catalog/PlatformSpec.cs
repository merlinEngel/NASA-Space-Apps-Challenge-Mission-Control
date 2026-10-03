using Newtonsoft.Json;

namespace MissionCore
{
    public class PlatformSpec
    {
        [JsonProperty("id")] public string id;
        [JsonProperty("name")] public string name;
        [JsonProperty("source")] public string source;
        [JsonProperty("bus_mass_kg")] public double busMassKg;
        [JsonProperty("max_mass_kg")] public double maxMassKg;
        [JsonProperty("payload_volume_U")] public double payloadVolumeU;
        [JsonProperty("bus_power_W")] public double busPowerW;
        [JsonProperty("max_solar_area_m2")] public double maxSolarAreaM2;
        [JsonProperty("lifetime_years")] public double lifetimeYears;
        [JsonProperty("price_USD")] public double priceUSD;
        // On-board data storage of the bus computer.
        [JsonProperty("storage_bits")] public double storageBits;
    }
}