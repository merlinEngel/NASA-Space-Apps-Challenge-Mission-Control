using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace MissionCore
{
    public static class CatalogLoader
    {
        static readonly JsonSerializerSettings settings = new()
        {
            ContractResolver = new DefaultContractResolver { NamingStrategy = new SnakeCaseNamingStrategy() },
            MissingMemberHandling = MissingMemberHandling.Ignore,
            Converters = { new StringEnumConverter { NamingStrategy = new SnakeCaseNamingStrategy(), AllowIntegerValues = false } }
        };

        public static List<T> LoadList<T>(string json, string fileName)
        {
            List<T> list;
            try { list = JsonConvert.DeserializeObject<List<T>>(json, settings); }
            catch (JsonException e) { throw new FormatException($"{fileName}: {e.Message}"); }
            if (list == null) throw new FormatException($"{fileName}: file is empty");
            return list;
        }

        // For files that are a single object: { ... } (balance_rules)
        public static T LoadObject<T>(string json, string fileName) where T : class
        {
            T obj;
            try { obj = JsonConvert.DeserializeObject<T>(json, settings); }
            catch (JsonException e) { throw new FormatException($"{fileName}: {e.Message}"); }
            if (obj == null) throw new FormatException($"{fileName}: file is empty");
            return obj;
        }
    }
}