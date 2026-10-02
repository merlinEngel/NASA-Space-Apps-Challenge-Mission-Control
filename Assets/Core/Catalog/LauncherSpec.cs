using Newtonsoft.Json;

namespace MissionCore
{
    public class LauncherSpec
    {
        [JsonProperty("id")] public string id;
        [JsonProperty("name")] public string name;
        [JsonProperty("source")] public string source;
        [JsonProperty("sso_kg")] public double? ssoKG;
        [JsonProperty("gto_kg")] public double? gtoKG;
        [JsonProperty("leo_kg")] public double leoKG;
        [JsonProperty("price_USD")] public double priceUSD;
        [JsonProperty("success_rate")] public double successRate;
    }
}