using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using MissionGame.UI;
using MissionCore.Catalogs;
using MissionCore.Mission;
using MissionCore.Rules;
using MissionCore.Text;

namespace MissionGame
{
    // Runs before every other script, so the catalog exists in all other Awake/Start calls.
    [DefaultExecutionOrder(-100)]
    public class CatalogManager : Singleton<CatalogManager>
    {
        public Catalog Catalog { get; private set; }
        public TextTable Texts => Catalog.Texts;
        public DisplayFormatter Formatter { get; private set; }
        public event Action DisplayModeChanged;

        public void SetDisplayMode(DisplayMode mode)
        {
            if (Formatter.Mode == mode) return;
            Formatter.Mode = mode;
            DisplayModeChanged?.Invoke();
        }

        string dir;

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this) return;
            DontDestroyOnLoad(gameObject);

            dir = Path.Combine(Application.streamingAssetsPath, "Data");

            Catalog = CatalogLoader.LoadAll(file => Exists(file) ? Read(file) : null);

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
