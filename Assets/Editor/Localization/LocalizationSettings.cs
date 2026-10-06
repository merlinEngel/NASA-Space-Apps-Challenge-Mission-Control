using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MissionGame.EditorTools
{
    // Editor-only settings, stored next to the scripts so they are in git and shared with the team.
    // Languages live here (not only in texts.json), so a new language exists before it has any text.
    [Serializable]
    public class LocalizationSettings
    {
        public const string SettingsPath = "Assets/Editor/Localization/localization_settings.json";

        public string textsPath = "Assets/StreamingAssets/Data/texts.json";
        public List<string> languages = new() { "de", "en" };
        public List<string> missingKeys = new();   // keys requested in Play Mode that texts.json did not contain

        public static LocalizationSettings Load()
        {
            if (!File.Exists(SettingsPath)) return new LocalizationSettings();
            return JsonUtility.FromJson<LocalizationSettings>(File.ReadAllText(SettingsPath)) ?? new LocalizationSettings();
        }

        public void Save()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath));
            File.WriteAllText(SettingsPath, JsonUtility.ToJson(this, true));
        }
    }
}
