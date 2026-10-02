using Newtonsoft.Json;

namespace MissionCore
{
    public class GroundStationSpec
    {
        [JsonProperty("id")] public string id;
        [JsonProperty("name")] public string name;
        [JsonProperty("source")] public string source;
        [JsonProperty("lat_deg")] public double latDeg;
        [JsonProperty("lon_deg")] public double lonDeg;
        [JsonProperty("bands")] public string[] bands;
        [JsonProperty("min_elevation_deg")] public double minElevationDeg;
        [JsonProperty("cost_per_contact_USD")] public double costPerContactUSD;
    }
}