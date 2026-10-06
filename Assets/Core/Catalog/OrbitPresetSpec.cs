using Newtonsoft.Json;

namespace MissionCore
{
    public class OrbitPresetSpec
    {
        [JsonProperty("id")] public string id;
        [JsonProperty("source")] public string source;
        [JsonProperty("name")] public string name;
        [JsonProperty("altitude_m")] public double altitudeM;
        [JsonProperty("inclination_deg")] public double inclinationDeg;
        [JsonProperty("sun_synchronous")] public bool sunSynchronous;
    }
}