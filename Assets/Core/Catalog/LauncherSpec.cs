using System;
using Newtonsoft.Json;

namespace MissionCore
{
    // How a launch is bought: a whole rocket for a fixed price, or a slot on a shared flight paid per kg.
    public enum LaunchType
    {
        Dedicated,
        Rideshare
    }

    // Orbit classes a rideshare flight can drop customers into.
    public enum LaunchOrbit
    {
        Leo,
        Sso,
        Gto
    }

    public class LauncherSpec
    {
        [JsonProperty("id")] public string id;
        [JsonProperty("name")] public string name;
        [JsonProperty("source")] public string source;
        // Missing in the file means "dedicated", so older entries stay valid.
        [JsonProperty("type")] public LaunchType type = LaunchType.Dedicated;
        [JsonProperty("success_rate")] public double successRate;

        // ---------- Dedicated ----------
        [JsonProperty("sso_kg")] public double? ssoKg;
        [JsonProperty("gto_kg")] public double? gtoKg;
        [JsonProperty("leo_kg")] public double leoKg;
        [JsonProperty("price_USD")] public double priceUSD;

        // ---------- Rideshare ----------
        [JsonProperty("price_per_kg_USD")] public double pricePerKgUSD;
        [JsonProperty("max_payload_per_customer_kg")] public double maxPayloadPerCustomerKg;
        // Lighter payloads are billed as if they had this mass.
        [JsonProperty("min_billable_mass_kg")] public double minBillableMassKg;
        [JsonProperty("orbits")] public LaunchOrbit[] orbits;
        // Optional altitude window of the shared flight.
        [JsonProperty("altitude_m")] public ValueRange altitudeM;

        public bool IsRideshare => type == LaunchType.Rideshare;

        // Price of one launch for a spacecraft of the given mass.
        // Dedicated: the fixed rocket price. Rideshare: max(mass, min billable mass) * price per kg.
        public double GetLaunchCost(double payloadMassKg)
        {
            if (payloadMassKg < 0) throw new ArgumentOutOfRangeException(nameof(payloadMassKg), "payload mass must be >= 0");
            if (!IsRideshare) return priceUSD;
            return Math.Max(payloadMassKg, minBillableMassKg) * pricePerKgUSD;
        }
    }
}
