using System.Collections.Generic;
using Newtonsoft.Json;

namespace MissionCore
{
    public class MissionDesign
    {
        [JsonProperty("mission_id")] public string MissionId { get; set; }

        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("platform_id")] public string PlatformId { get; set; }
        [JsonProperty("instrument_ids", NullValueHandling = NullValueHandling.Ignore)] public List<string> InstrumentIds { get; set; } = new();
        [JsonProperty("launcher_id")] public string LauncherId { get; set; }
        [JsonProperty("propulsion_id")] public string PropulsionId { get; set; }
        [JsonProperty("comms_id")] public string CommsId { get; set; }
        [JsonProperty("battery_id")] public string BatteryId { get; set; }
        [JsonProperty("solar_cell_id")] public string SolarCellId { get; set; }
        [JsonProperty("ground_station_ids", NullValueHandling = NullValueHandling.Ignore)] public List<string> GroundStationIds { get; set; } = new();

        [JsonProperty("battery_count")]  public int BatteryCount { get; set; } = 1;
        [JsonProperty("solar_area_m2")] public double SolarAreaM2 { get; set; }
        [JsonProperty("propellant_mass_kg")] public double PropellantMassKg { get; set; }
        [JsonProperty("altitude_m")] public double AltitudeM { get; set; }
        [JsonProperty("inclination_deg")] public double InclinationDeg { get; set; }
    }
}