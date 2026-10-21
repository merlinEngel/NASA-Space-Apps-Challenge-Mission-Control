using MissionCore.Earth;
using Newtonsoft.Json;

namespace MissionCore.Catalogs
{
    public class OrbitPresetSpec
    {
        [JsonProperty("id")] public string id;
        [JsonProperty("source")] public string source;
        [JsonProperty("name")] public string name;
        [JsonProperty("altitude_m")] public double altitudeM;
        [JsonProperty("inclination_deg")] private double inclinationDeg;
        [JsonProperty("sun_synchronous")] public bool sunSynchronous;

        public double EffectiveInclinationDeg => sunSynchronous ? J2Drift.SunSynchronousInclinationDeg(altitudeM) ?? inclinationDeg : inclinationDeg;
    }
}