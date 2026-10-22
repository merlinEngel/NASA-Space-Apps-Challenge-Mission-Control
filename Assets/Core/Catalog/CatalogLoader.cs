using System;
using System.Collections.Generic;
using MissionCore.Mission;
using MissionCore.Rules;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System.IO;
using MissionCore.Text;

namespace MissionCore.Catalogs
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

        // Loads all catalog files. readFile returns the file content, or null if the file does not exist.
        // The caller decides how files are read (File.ReadAllText, UnityWebRequest on WebGL, ...).
        public static Catalog LoadAll(Func<string, string> readFile)
        {
            string Required(string file) => readFile(file) ?? throw new FileNotFoundException($"{file} not found", file);

            List<T> List<T>(string file, Action<List<T>, string> validate)
            {
                var list = LoadList<T>(Required(file), file);
                validate(list, file);
                return list;
            }

            T Optional<T>(string file, Action<T, string> validate) where T : class
            {
                string json = readFile(file);
                if (json == null) return null;
                var obj = LoadObject<T>(json, file);
                validate(obj, file);
                return obj;
            }

            var balance = LoadObject<BalanceRules>(Required("Rules/balance_rules.json"), "Rules/balance_rules.json");
            CatalogValidator.Validate(balance, "Rules/balance_rules.json");

            TextTable texts = null;
            string textJson = readFile("texts.json");
            if (textJson != null)
            {
                var dict = TextTableLoader.LoadTextTable(textJson, "texts.json");
                CatalogValidator.Validate(dict, "texts.json");
                texts = new TextTable(dict);
            }

            return new Catalog(
                List<PropulsionSpec>("propulsion.json", CatalogValidator.Validate),
                List<InstrumentSpec>("instruments.json", CatalogValidator.Validate),
                List<LauncherSpec>("launchers.json", CatalogValidator.Validate),
                List<CommsSpec>("comms.json", CatalogValidator.Validate),
                List<BatterySpec>("batteries.json", CatalogValidator.Validate),
                List<PlatformSpec>("platforms.json", CatalogValidator.Validate),
                List<GroundStationSpec>("groundstations.json", CatalogValidator.Validate),
                List<SolarCellSpec>("solar.json", CatalogValidator.Validate),
                List<MissionTemplate>("missions.json", CatalogValidator.Validate),
                balance,
                Optional<ScoringRules>("Rules/scoring.json", CatalogValidator.Validate),
                Optional<SimRules>("Rules/sim_rules.json", CatalogValidator.Validate),
                texts,
                List<OrbitPresetSpec>("orbit_presets.json", CatalogValidator.Validate));
        }
    }
}