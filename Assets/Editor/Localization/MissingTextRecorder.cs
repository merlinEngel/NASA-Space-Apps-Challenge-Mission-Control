using MissionCore;
using UnityEditor;
using UnityEngine;

namespace MissionGame.EditorTools
{
    // When Play Mode ends, copies the keys TextTable could not find into the localization settings.
    // ExitingPlayMode fires while the scene objects still exist, so CatalogManager is still reachable.
    [InitializeOnLoad]
    static class MissingTextRecorder
    {
        static MissingTextRecorder()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingPlayMode) return;

            CatalogManager manager = CatalogManager.Instance;
            if (manager == null || manager.Catalog == null) return;
            TextTable texts = manager.Texts;
            if (texts == null || texts.MissingKeys.Count == 0) return;

            LocalizationSettings settings = LocalizationSettings.Load();
            int added = 0;
            foreach (string key in texts.MissingKeys)
            {
                if (settings.missingKeys.Contains(key)) continue;
                settings.missingKeys.Add(key);
                added++;
            }
            if (added == 0) return;

            settings.Save();
            Debug.Log($"Localization: recorded {added} missing text key(s). See Tools > Localization.");
            LocalizationWindow.RefreshOpenWindows();
        }
    }
}
