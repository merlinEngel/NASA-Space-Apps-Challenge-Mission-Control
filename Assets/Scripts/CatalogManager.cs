using System;
using System.Collections.Generic;
using System.IO;
using MissionCore;
using UnityEngine;

namespace MissionGame
{
    // Runs before every other script, so the catalog exists in all other Awake/Start calls.
    [DefaultExecutionOrder(-100)]
    public class CatalogManager : Singleton<CatalogManager>
    {
        public Catalog Catalog { get; private set; }
        public TextTable Texts => Catalog.Texts;
        public DisplayFormatter Formatter { get; private set; }

        string dir;

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this) return;
            DontDestroyOnLoad(gameObject);

            dir = Path.Combine(Application.streamingAssetsPath, "Data");

            var propulsion = LoadList<PropulsionSpec>("propulsion.json", CatalogValidator.Validate);
            var instruments = LoadList<InstrumentSpec>("instruments.json", CatalogValidator.Validate);
            var launchers = LoadList<LauncherSpec>("launchers.json", CatalogValidator.Validate);
            var comms = LoadList<CommsSpec>("comms.json", CatalogValidator.Validate);
            var batteries = LoadList<BatterySpec>("batteries.json", CatalogValidator.Validate);
            var platforms = LoadList<PlatformSpec>("platforms.json", CatalogValidator.Validate);
            var groundStations = LoadList<GroundStationSpec>("groundstations.json", CatalogValidator.Validate);
            var solar = LoadList<SolarCellSpec>("solar.json", CatalogValidator.Validate);
            var missions = LoadList<MissionTemplate>("missions.json", CatalogValidator.Validate);
            var orbitPresets = LoadList<OrbitPresetSpec>("orbit_presets.json", CatalogValidator.Validate);

            var balance = LoadObject<BalanceRules>("Rules/balance_rules.json", CatalogValidator.Validate);
            // Not written yet: load them as soon as the files exist, stay null until then.
            var scoring = LoadOptionalObject<ScoringRules>("Rules/scoring.json", CatalogValidator.Validate);
            var sim = LoadOptionalObject<SimRules>("Rules/sim_rules.json", CatalogValidator.Validate);
            var texts = LoadOptionalTexts("texts.json");

            Catalog = new Catalog(propulsion, instruments, launchers, comms, batteries, platforms,
                                  groundStations, solar, missions, balance, scoring, sim, texts, orbitPresets);

            Formatter = new DisplayFormatter(DisplayMode.Simple, Texts);

            Debug.Log($"Catalog loaded: {Catalog.Propulsion.Count} propulsion, {Catalog.Instruments.Count} instruments, " +
                      $"{Catalog.Launchers.Count} launchers, {Catalog.Comms.Count} comms, {Catalog.Batteries.Count} batteries, " +
                      $"{Catalog.Platforms.Count} platforms, {Catalog.GroundStations.Count} ground stations, " +
                      $"{Catalog.SolarCells.Count} solar, {Catalog.Missions.Count} missions, {Catalog.Warnings.Count} warnings, " +
                      $"{Catalog.OrbitPresets.Count} orbit presets");
            foreach (string warning in Catalog.Warnings) Debug.LogWarning(warning);
        }

        void Start()
        {
            UpdateTextLocalizers();
        }

        public void UpdateTextLocalizers()
        {
            foreach (TextLocalizer localizer in FindObjectsByType<TextLocalizer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                localizer.UpdateRequested -= UpdateUiText;
                localizer.UpdateRequested += UpdateUiText;
                localizer.RequestUpdate();
            }
        }

        List<T> LoadList<T>(string file, Action<List<T>, string> validate)
        {
            var list = CatalogLoader.LoadList<T>(Read(file), file);
            validate(list, file);
            return list;
        }

        public void UpdateUiText(TextLocalizer localizer, object[] args)
        {
            localizer.SetText(Texts.Get(localizer.key, args));
        }

        T LoadObject<T>(string file, Action<T, string> validate) where T : class
        {
            var obj = CatalogLoader.LoadObject<T>(Read(file), file);
            validate(obj, file);
            return obj;
        }

        T LoadOptionalObject<T>(string file, Action<T, string> validate) where T : class
        {
            if (Exists(file)) return LoadObject(file, validate);
            Debug.LogWarning($"{file} not found, {typeof(T).Name} stays empty");
            return null;
        }

        TextTable LoadOptionalTexts(string file)
        {
            if (!Exists(file)) return null;   // Catalog reports the missing texts as a warning
            var dict = TextTableLoader.LoadTextTable(Read(file), file);
            CatalogValidator.Validate(dict, file);
            return new TextTable(dict);
        }

        bool Exists(string file) => File.Exists(Path.Combine(dir, file));

        // File.ReadAllText does not work on WebGL; swap for UnityWebRequest there later.
        string Read(string file) => File.ReadAllText(Path.Combine(dir, file));
    }
}
