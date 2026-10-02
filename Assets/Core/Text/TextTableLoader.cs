using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace MissionCore
{
    public class TextTableLoader
    {
        static readonly JsonSerializerSettings settings = new()
        {
            ContractResolver = new DefaultContractResolver { NamingStrategy = new SnakeCaseNamingStrategy() },
            MissingMemberHandling = MissingMemberHandling.Ignore,
            Converters = { new StringEnumConverter { NamingStrategy = new SnakeCaseNamingStrategy(), AllowIntegerValues = false } }
        };
        
        public static Dictionary<string, Dictionary<string, string>> LoadTextTable(string json, string fileName)
        {
            Dictionary<string, Dictionary<string, string>> dict;
            try { dict = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(json, settings); }
            catch (JsonException e) { throw new FormatException($"{fileName}: {e.Message}"); }
            if (dict == null) throw new FormatException($"{fileName}: file is empty");
            return dict;
        }
    }
}