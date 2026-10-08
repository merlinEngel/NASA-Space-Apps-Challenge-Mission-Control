using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MissionGame.EditorTools
{
    // Tools > Localization: view, edit and complete texts.json.
    // Layout: toolbar (file, search, save) on top, navigation on the left, content on the right.
    public class LocalizationWindow : EditorWindow
    {
        enum View { All, Incomplete, Missing, FillIn, Add }
        const string FillControl = "LocalizationFillField";
        const float SidebarWidth = 190;
        const float ChipWidth = 34;

        // Colors match the game UI palette (accent 4FC3F7, status green/yellow/red).
        static readonly Color Accent = new(0.31f, 0.76f, 0.97f);
        static readonly Color Ok = new(0.24f, 0.86f, 0.59f);
        static readonly Color Warn = new(1f, 0.78f, 0.34f);
        static readonly Color Error = new(1f, 0.36f, 0.42f);
        static readonly Color Neutral = new(0.55f, 0.61f, 0.72f);
        static bool Pro => EditorGUIUtility.isProSkin;
        static Color RowA => Pro ? new Color(1, 1, 1, 0.025f) : new Color(0, 0, 0, 0.025f);
        static Color RowB => Pro ? new Color(1, 1, 1, 0.055f) : new Color(0, 0, 0, 0.055f);
        static Color GroupBackground => Pro ? new Color(1, 1, 1, 0.09f) : new Color(0, 0, 0, 0.09f);
        static Color Separator => Pro ? new Color(0, 0, 0, 0.45f) : new Color(0, 0, 0, 0.2f);

        LocalizationSettings settings;
        LocalizationFile file;
        bool dirty;
        string loadError;   // set when texts.json cannot be parsed; saving is blocked so the file is not overwritten
        View view;
        string search = "";
        Vector2 scroll;
        readonly HashSet<string> expanded = new();
        readonly HashSet<string> collapsedGroups = new();
        Action afterGui;    // changes to lists run after drawing, never while a list is being drawn

        // Missing view: values typed in before the key is added to the file
        readonly Dictionary<string, Dictionary<string, string>> pending = new();

        // Add view
        string newKey = "";
        readonly Dictionary<string, string> newValues = new();
        string newLanguage = "";

        // Fill-in view
        string fillValue = "";
        readonly HashSet<(string key, string language)> skipped = new();
        bool focusFill = true;
        int filledThisSession;

        // NonSerialized: Unity keeps private window fields across domain reloads (e.g. entering Play Mode),
        // but GUIStyles come back broken (no padding, no font size). Rebuilding them is cheap.
        [NonSerialized] GUIStyle wrapStyle, chipStyle, sidebarStyle, countStyle, titleStyle, bigKeyStyle,
                 cardStyle, contentStyle, fillFieldStyle, hintStyle, foldoutStyle;

        [MenuItem("Tools/Localization")]
        static void Open()
        {
            var window = GetWindow<LocalizationWindow>("Localization");
            window.minSize = new Vector2(560, 360);
        }

        // Called by MissingTextRecorder after Play Mode, so the Missing view updates without reopening.
        public static void RefreshOpenWindows()
        {
            foreach (LocalizationWindow window in Resources.FindObjectsOfTypeAll<LocalizationWindow>())
            {
                window.settings = LocalizationSettings.Load();
                window.Repaint();
            }
        }

        void OnEnable()
        {
            wantsMouseMove = true;   // sidebar hover highlight
            settings = LocalizationSettings.Load();
            LoadFile();
        }

        void OnDestroy()
        {
            if (dirty && loadError == null && EditorUtility.DisplayDialog("Localization", "texts.json has unsaved changes.", "Save", "Discard"))
                SaveFile();
        }

        // ---------- file handling ----------

        void LoadFile()
        {
            try
            {
                file = LocalizationFile.Load(settings.textsPath);
                loadError = null;
            }
            catch (Exception e)
            {
                file = new LocalizationFile();
                loadError = e.Message;
                Debug.LogError($"Localization: cannot read {settings.textsPath}: {e.Message}");
            }
            dirty = false;
            skipped.Clear();
            filledThisSession = 0;

            // Languages that exist in the file but not yet in the settings are taken over.
            bool changed = false;
            foreach (string language in file.LanguagesInFile)
            {
                if (settings.languages.Contains(language)) continue;
                settings.languages.Add(language);
                changed = true;
            }
            changed |= settings.missingKeys.RemoveAll(file.Contains) > 0;
            if (changed) settings.Save();
        }

        void SaveFile()
        {
            if (loadError != null) return;
            file.Save(settings.textsPath, settings.languages);
            dirty = false;
            settings.missingKeys.RemoveAll(file.Contains);
            settings.Save();
            if (settings.textsPath.StartsWith("Assets/")) AssetDatabase.ImportAsset(settings.textsPath);
        }

        bool ConfirmDiscard() =>
            !dirty || EditorUtility.DisplayDialog("Localization", "Discard unsaved changes?", "Discard", "Cancel");

        static string ToProjectPath(string absolute)
        {
            string root = Directory.GetCurrentDirectory().Replace('\\', '/') + "/";
            absolute = absolute.Replace('\\', '/');
            return absolute.StartsWith(root) ? absolute.Substring(root.Length) : absolute;
        }

        // ---------- main layout ----------

        void OnGUI()
        {
            if (settings == null || file == null) OnEnable();
            if (Event.current.type == EventType.MouseMove) Repaint();
            EnsureStyles();

            DrawToolbar();
            DrawBanners();

            EditorGUILayout.BeginHorizontal();
            DrawSidebar();
            Rect line = GUILayoutUtility.GetRect(1, 1, GUILayout.Width(1), GUILayout.ExpandHeight(true));
            EditorGUI.DrawRect(line, Separator);

            EditorGUILayout.BeginVertical();
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.BeginVertical(contentStyle);
            switch (view)
            {
                case View.All: DrawTextList(file.Keys, "All texts", "No texts yet. Add one in “Add”."); break;
                case View.Incomplete: DrawTextList(file.Keys.Where(HasMissingValue), "Incomplete texts", "Every text has a value in every language."); break;
                case View.Missing: DrawMissing(); break;
                case View.FillIn: DrawFillIn(); break;
                case View.Add: DrawAdd(); break;
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            if (afterGui != null)
            {
                Action action = afterGui;
                afterGui = null;
                action();
                Repaint();
            }
        }

        void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("File", EditorStyles.miniLabel, GUILayout.Width(26));

            EditorGUI.BeginChangeCheck();
            string path = EditorGUILayout.TextField(settings.textsPath, EditorStyles.toolbarTextField, GUILayout.MinWidth(140));
            if (EditorGUI.EndChangeCheck())
            {
                settings.textsPath = path;
                settings.Save();
            }

            if (GUILayout.Button(new GUIContent("…", "Select texts.json"), EditorStyles.toolbarButton, GUILayout.Width(24)))
            {
                string folder = string.IsNullOrWhiteSpace(settings.textsPath)
                    ? Application.dataPath
                    : Path.GetDirectoryName(Path.GetFullPath(settings.textsPath));
                string picked = EditorUtility.OpenFilePanel("Select texts.json", folder, "json");
                if (!string.IsNullOrEmpty(picked) && ConfirmDiscard())
                {
                    settings.textsPath = ToProjectPath(picked);
                    settings.Save();
                    LoadFile();
                }
            }
            if (GUILayout.Button(new GUIContent("Reload", "Read the file again"), EditorStyles.toolbarButton, GUILayout.Width(52)) && ConfirmDiscard())
                LoadFile();

            GUILayout.Space(8);
            search = GUILayout.TextField(search, EditorStyles.toolbarSearchField, GUILayout.MinWidth(100), GUILayout.MaxWidth(220));

            Color previous = GUI.backgroundColor;
            if (dirty) GUI.backgroundColor = Accent;
            GUI.enabled = dirty && loadError == null;
            if (GUILayout.Button(dirty ? "Save ●" : "Save", EditorStyles.toolbarButton, GUILayout.Width(58))) SaveFile();
            GUI.enabled = true;
            GUI.backgroundColor = previous;
            EditorGUILayout.EndHorizontal();
        }

        void DrawBanners()
        {
            if (loadError != null)
                EditorGUILayout.HelpBox("texts.json is not valid JSON, saving is disabled:\n" + loadError, MessageType.Error);
            else if (!File.Exists(settings.textsPath))
                EditorGUILayout.HelpBox("File not found. Save creates it.", MessageType.Warning);
        }

        void DrawSidebar()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(SidebarWidth));
            GUILayout.Space(8);

            int incomplete = file.Keys.Count(HasMissingValue);
            int missing = MissingKeys().Count();
            int open = OpenSlots().Count(s => !skipped.Contains(s));

            SidebarSection("TEXTS");
            SidebarItem(View.All, "All texts", file.Keys.Count.ToString(), Neutral);
            SidebarItem(View.Incomplete, "Incomplete", incomplete.ToString(), incomplete > 0 ? Warn : Ok);
            SidebarItem(View.Missing, "Missing keys", missing.ToString(), missing > 0 ? Error : Ok);

            GUILayout.Space(8);
            SidebarSection("EDIT");
            SidebarItem(View.FillIn, "Fill in", open.ToString(), open > 0 ? Accent : Ok);
            SidebarItem(View.Add, "Add text / language", null, Neutral);

            GUILayout.Space(14);
            SidebarSection("LANGUAGES");
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(10);
            foreach (string language in settings.languages) Chip(language, Accent);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();
            if (dirty)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(10);
                Chip("unsaved changes", Warn, false);
                EditorGUILayout.EndHorizontal();
            }
            GUILayout.Space(8);
            EditorGUILayout.EndVertical();
        }

        void SidebarSection(string title)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(10);
            GUILayout.Label(title, EditorStyles.miniBoldLabel);
            EditorGUILayout.EndHorizontal();
        }

        void SidebarItem(View target, string label, string count, Color countColor)
        {
            Rect r = GUILayoutUtility.GetRect(SidebarWidth, 24, GUILayout.ExpandWidth(true));
            bool selected = view == target;
            if (Event.current.type == EventType.Repaint)
            {
                if (selected)
                {
                    EditorGUI.DrawRect(r, new Color(Accent.r, Accent.g, Accent.b, 0.18f));
                    EditorGUI.DrawRect(new Rect(r.x, r.y, 3, r.height), Accent);
                }
                else if (r.Contains(Event.current.mousePosition))
                    EditorGUI.DrawRect(r, RowB);
            }

            GUI.Label(r, label, sidebarStyle);
            if (count != null)
            {
                var content = new GUIContent(count);
                Vector2 size = chipStyle.CalcSize(content);
                var chip = new Rect(r.xMax - size.x - 10, r.y + (r.height - 16) / 2, Mathf.Max(size.x, 22), 16);
                DrawChip(chip, content, countColor);
            }

            if (GUI.Button(r, GUIContent.none, GUIStyle.none) && view != target)
            {
                view = target;
                scroll = Vector2.zero;
                focusFill = true;
                GUIUtility.keyboardControl = 0;
            }
            EditorGUIUtility.AddCursorRect(r, MouseCursor.Link);
        }

        // ---------- views ----------

        // Texts grouped by prefix ("ui", "part" ...), each group collapsible, each text a foldout row.
        void DrawTextList(IEnumerable<string> source, string title, string emptyText)
        {
            List<string> keys = source.Where(Matches).ToList();
            Header(title, $"{keys.Count} shown");
            if (keys.Count == 0)
            {
                Empty(string.IsNullOrEmpty(search) ? emptyText : $"Nothing matches “{search}”.");
                return;
            }

            foreach (IGrouping<string, string> group in keys.GroupBy(GroupOf))
            {
                int gaps = group.Count(HasMissingValue);
                if (!GroupHeader(group.Key, group.Count(), gaps)) continue;

                int row = 0;
                foreach (string key in group) DrawTextRow(key, group.Key, row++ % 2 == 0);
                GUILayout.Space(6);
            }
        }

        void DrawTextRow(string key, string group, bool even)
        {
            Rect area = EditorGUILayout.BeginVertical();
            if (Event.current.type == EventType.Repaint) EditorGUI.DrawRect(area, even ? RowA : RowB);

            var chips = settings.languages
                .Select(l => (l, LocalizationFile.IsMissing(file.Get(key, l)) ? Warn : Ok))
                .ToList();
            bool open = RowHeader(key, ShortName(key, group), chips, expanded.Contains(key));

            if (open)
            {
                BeginIndent();
                foreach (string language in settings.languages)
                {
                    string value = file.Get(key, language) ?? "";
                    EditorGUI.BeginChangeCheck();
                    string edited = LanguageField(language, value);
                    if (EditorGUI.EndChangeCheck())
                    {
                        file.Set(key, language, edited);
                        dirty = true;
                    }
                }

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(key, EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Copy key", EditorStyles.miniButtonLeft, GUILayout.Width(70)))
                    EditorGUIUtility.systemCopyBuffer = key;
                if (GUILayout.Button("Delete", EditorStyles.miniButtonRight, GUILayout.Width(60)) &&
                    EditorUtility.DisplayDialog("Localization", $"Delete '{key}'?", "Delete", "Cancel"))
                {
                    afterGui += () => { file.Remove(key); expanded.Remove(key); dirty = true; };
                }
                EditorGUILayout.EndHorizontal();
                EndIndent();
            }

            EditorGUILayout.EndVertical();
        }

        // Keys requested in Play Mode that are not in the file yet. Same rows, all values empty.
        void DrawMissing()
        {
            List<string> missing = MissingKeys().Where(Matches).ToList();
            Header("Missing keys", "requested in Play Mode, not in texts.json");
            if (missing.Count == 0)
            {
                Empty(string.IsNullOrEmpty(search)
                    ? "No missing keys. Keys that the game asks for but cannot find are collected when you leave Play Mode."
                    : $"Nothing matches “{search}”.");
                return;
            }

            int row = 0;
            foreach (string key in missing)
            {
                Rect area = EditorGUILayout.BeginVertical();
                if (Event.current.type == EventType.Repaint) EditorGUI.DrawRect(area, row++ % 2 == 0 ? RowA : RowB);

                bool open = RowHeader(key, key, new List<(string, Color)> { ("not in file", Error) }, expanded.Contains(key));
                if (open)
                {
                    if (!pending.TryGetValue(key, out var values)) pending[key] = values = new Dictionary<string, string>();
                    BeginIndent();
                    foreach (string language in settings.languages)
                    {
                        values.TryGetValue(language, out string value);
                        values[language] = LanguageField(language, value ?? "");
                    }

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Add to texts", EditorStyles.miniButtonLeft, GUILayout.Width(90)))
                    {
                        afterGui += () =>
                        {
                            file.AddKey(key);
                            foreach (var pair in values) file.Set(key, pair.Key, pair.Value);
                            pending.Remove(key);
                            dirty = true;
                        };
                    }
                    if (GUILayout.Button("Ignore", EditorStyles.miniButtonRight, GUILayout.Width(60)))
                    {
                        afterGui += () =>
                        {
                            settings.missingKeys.Remove(key);
                            settings.Save();
                            pending.Remove(key);
                        };
                    }
                    EditorGUILayout.EndHorizontal();
                    EndIndent();
                }
                EditorGUILayout.EndVertical();
            }
        }

        // One empty slot at a time: type the value, press Enter, the next slot appears.
        void DrawFillIn()
        {
            List<(string key, string language)> open = OpenSlots().ToList();
            List<(string key, string language)> todo = open.Where(s => !skipped.Contains(s)).ToList();

            Header("Fill in", "missing values, one after another");

            int total = todo.Count + filledThisSession;
            if (total > 0)
            {
                Rect bar = GUILayoutUtility.GetRect(0, 18, GUILayout.ExpandWidth(true));
                EditorGUI.ProgressBar(bar, filledThisSession / (float)total, $"{filledThisSession} done  ·  {todo.Count} left  ·  {skipped.Count} skipped");
                GUILayout.Space(10);
            }

            if (todo.Count == 0)
            {
                Empty(open.Count == 0 ? "All texts are complete." : "Only skipped values are left.");
                if (skipped.Count > 0 && GUILayout.Button("Show skipped values again", GUILayout.Width(200))) skipped.Clear();
                return;
            }

            var (key, language) = todo[0];
            EditorGUILayout.BeginVertical(cardStyle);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(GroupOf(key), EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            if (!file.Contains(key)) Chip("new key", Error, false);
            EditorGUILayout.EndHorizontal();
            GUILayout.Label(key, bigKeyStyle);
            GUILayout.Space(8);

            // Existing values in other languages help with the translation.
            foreach (string other in settings.languages.Where(l => l != language))
            {
                string reference = file.Get(key, other);
                if (LocalizationFile.IsMissing(reference)) continue;
                EditorGUILayout.BeginHorizontal();
                Chip(other, Neutral);
                GUILayout.Label(reference, EditorStyles.wordWrappedLabel);
                EditorGUILayout.EndHorizontal();
            }

            GUILayout.Space(6);

            // Read Enter before the text field handles the event.
            Event e = Event.current;
            bool enter = e.type == EventType.KeyDown
                         && (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
                         && GUI.GetNameOfFocusedControl() == FillControl;

            EditorGUILayout.BeginHorizontal();
            Chip(language, Accent);
            GUI.SetNextControlName(FillControl);
            fillValue = EditorGUILayout.TextField(fillValue, fillFieldStyle, GUILayout.Height(24));
            EditorGUILayout.EndHorizontal();

            if (focusFill && e.type == EventType.Repaint)
            {
                EditorGUI.FocusTextInControl(FillControl);
                focusFill = false;
            }

            GUILayout.Space(6);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Enter saves and shows the next value.", hintStyle);
            GUILayout.FlexibleSpace();
            bool skip = GUILayout.Button("Skip", GUILayout.Width(70));
            GUI.enabled = !LocalizationFile.IsMissing(fillValue);
            bool commit = GUILayout.Button("Save  ↵", GUILayout.Width(90));
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            if ((enter || commit) && !LocalizationFile.IsMissing(fillValue))
            {
                if (enter) e.Use();
                string value = fillValue.Trim();
                afterGui += () =>
                {
                    file.Set(key, language, value);
                    dirty = true;
                    filledThisSession++;
                };
                NextFillSlot();
            }
            else if (skip)
            {
                skipped.Add((key, language));
                NextFillSlot();
            }
        }

        void NextFillSlot()
        {
            // The text field keeps its own buffer while focused; drop focus so the cleared value shows.
            GUIUtility.keyboardControl = 0;
            fillValue = "";
            focusFill = true;
            Repaint();
        }

        void DrawAdd()
        {
            Header("Add", "new text or new language");

            EditorGUILayout.BeginVertical(cardStyle);
            GUILayout.Label("New text", EditorStyles.boldLabel);
            GUILayout.Space(4);
            newKey = EditorGUILayout.TextField("Key", newKey).Trim();
            GUILayout.Label("Use group.name, e.g. ui.save or part.x_band. The group decides where the key is placed.", hintStyle);
            GUILayout.Space(4);
            foreach (string language in settings.languages)
            {
                newValues.TryGetValue(language, out string value);
                newValues[language] = LanguageField(language, value ?? "");
            }

            bool exists = file.Contains(newKey);
            if (exists) EditorGUILayout.HelpBox($"'{newKey}' already exists.", MessageType.Error);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUI.enabled = newKey.Length > 0 && !exists;
            if (GUILayout.Button("Add text", GUILayout.Width(100)))
            {
                string key = newKey;
                var values = new Dictionary<string, string>(newValues);
                afterGui += () =>
                {
                    file.AddKey(key);
                    foreach (var pair in values) file.Set(key, pair.Key, pair.Value);
                    dirty = true;
                    expanded.Add(key);
                    if (settings.missingKeys.Remove(key)) settings.Save();
                };
                newKey = "";
                newValues.Clear();
                GUIUtility.keyboardControl = 0;
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            GUILayout.Space(10);

            EditorGUILayout.BeginVertical(cardStyle);
            GUILayout.Label("New language", EditorStyles.boldLabel);
            GUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Current", GUILayout.Width(EditorGUIUtility.labelWidth - 4));
            foreach (string language in settings.languages) Chip(language, Accent);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            newLanguage = EditorGUILayout.TextField("Code", newLanguage).Trim().ToLowerInvariant();
            GUILayout.Label("Two-letter code, e.g. fr. Every text then shows a warning until it has a value in the new language.", hintStyle);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUI.enabled = newLanguage.Length > 0 && !settings.languages.Contains(newLanguage);
            if (GUILayout.Button("Add language", GUILayout.Width(100)))
            {
                settings.languages.Add(newLanguage);
                settings.Save();
                newLanguage = "";
                GUIUtility.keyboardControl = 0;
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        // ---------- building blocks ----------

        void EnsureStyles()
        {
            if (chipStyle != null) return;

            wrapStyle = new GUIStyle(EditorStyles.textArea) { wordWrap = true };
            chipStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(6, 6, 1, 1),
                margin = new RectOffset(2, 4, 3, 3)
            };
            chipStyle.normal.textColor = new Color(0.07f, 0.09f, 0.13f);
            sidebarStyle = new GUIStyle(EditorStyles.label) { padding = new RectOffset(14, 8, 0, 0), alignment = TextAnchor.MiddleLeft };
            countStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleRight };
            titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 16 };
            bigKeyStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 15, wordWrap = true };
            cardStyle = new GUIStyle(EditorStyles.helpBox) { padding = new RectOffset(14, 14, 10, 12) };
            contentStyle = new GUIStyle { padding = new RectOffset(14, 14, 10, 10) };
            fillFieldStyle = new GUIStyle(EditorStyles.textField) { fontSize = 13, alignment = TextAnchor.MiddleLeft };
            hintStyle = new GUIStyle(EditorStyles.wordWrappedMiniLabel);
            foldoutStyle = new GUIStyle(EditorStyles.foldout) { fontStyle = FontStyle.Bold, fontSize = 12 };
        }

        void Header(string title, string subtitle)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(title, titleStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Label(subtitle, countStyle);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(8);
        }

        void Empty(string text)
        {
            GUILayout.Space(30);
            var style = new GUIStyle(EditorStyles.centeredGreyMiniLabel) { wordWrap = true, fontSize = 12 };
            GUILayout.Label(text, style);
        }

        // Group bar with count; returns true while the group is open.
        bool GroupHeader(string group, int count, int gaps)
        {
            Rect r = GUILayoutUtility.GetRect(0, 24, GUILayout.ExpandWidth(true));
            if (Event.current.type == EventType.Repaint) EditorGUI.DrawRect(r, GroupBackground);

            bool open = !collapsedGroups.Contains(group);
            var foldoutRect = new Rect(r.x + 4, r.y, r.width - 140, r.height);
            bool nowOpen = EditorGUI.Foldout(foldoutRect, open, group, true, foldoutStyle);

            var countContent = new GUIContent(count.ToString());
            float x = r.xMax - 6;
            if (gaps > 0)
            {
                var gapContent = new GUIContent($"{gaps} incomplete");
                float w = chipStyle.CalcSize(gapContent).x;
                x -= w;
                DrawChip(new Rect(x, r.y + 4, w, 16), gapContent, Warn);
                x -= 4;
            }
            float cw = Mathf.Max(chipStyle.CalcSize(countContent).x, 22);
            x -= cw;
            DrawChip(new Rect(x, r.y + 4, cw, 16), countContent, Neutral);

            if (nowOpen != open)
            {
                if (nowOpen) collapsedGroups.Remove(group);
                else collapsedGroups.Add(group);
            }
            GUILayout.Space(2);
            return nowOpen;
        }

        // Foldout row with status chips on the right; returns true while open.
        bool RowHeader(string key, string label, List<(string text, Color color)> chips, bool open)
        {
            Rect r = GUILayoutUtility.GetRect(0, 22, GUILayout.ExpandWidth(true));

            float x = r.xMax - 6;
            for (int i = chips.Count - 1; i >= 0; i--)
            {
                var content = new GUIContent(chips[i].text);
                float w = Mathf.Max(chipStyle.CalcSize(content).x, ChipWidth);
                x -= w;
                DrawChip(new Rect(x, r.y + 3, w, 16), content, chips[i].color);
                x -= 3;
            }

            var foldoutRect = new Rect(r.x + 18, r.y, x - r.x - 22, r.height);
            bool nowOpen = EditorGUI.Foldout(foldoutRect, open, new GUIContent(label, key), true);
            if (nowOpen) expanded.Add(key);
            else expanded.Remove(key);
            return nowOpen;
        }

        string LanguageField(string language, string value)
        {
            EditorGUILayout.BeginHorizontal();
            Chip(language, LocalizationFile.IsMissing(value) ? Warn : Ok);
            string result = EditorGUILayout.TextArea(value, wrapStyle);
            EditorGUILayout.EndHorizontal();
            return result;
        }

        // Layout version of a chip (fixed width for language codes, auto width otherwise).
        void Chip(string text, Color color, bool fixedWidth = true)
        {
            var content = new GUIContent(text);
            float width = fixedWidth ? Mathf.Max(chipStyle.CalcSize(content).x, ChipWidth) : chipStyle.CalcSize(content).x;
            Rect r = GUILayoutUtility.GetRect(width, 16, chipStyle, GUILayout.Width(width), GUILayout.Height(16));
            DrawChip(r, content, color);
        }

        void DrawChip(Rect r, GUIContent content, Color color)
        {
            if (Event.current.type != EventType.Repaint) return;
            EditorGUI.DrawRect(r, color);
            chipStyle.Draw(r, content, false, false, false, false);
        }

        static void BeginIndent()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(22);
            EditorGUILayout.BeginVertical();
            GUILayout.Space(2);
        }

        static void EndIndent()
        {
            GUILayout.Space(6);
            EditorGUILayout.EndVertical();
            GUILayout.Space(8);
            EditorGUILayout.EndHorizontal();
        }

        // ---------- data helpers ----------

        static string GroupOf(string key)
        {
            int dot = key.IndexOf('.');
            return dot < 0 ? key : key.Substring(0, dot);
        }

        static string ShortName(string key, string group) =>
            key.Length > group.Length + 1 && key.StartsWith(group + ".") ? key.Substring(group.Length + 1) : key;

        bool Matches(string key) =>
            string.IsNullOrEmpty(search) || key.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;

        bool HasMissingValue(string key) =>
            settings.languages.Any(language => LocalizationFile.IsMissing(file.Get(key, language)));

        IEnumerable<string> MissingKeys() => settings.missingKeys.Where(k => !file.Contains(k));

        IEnumerable<(string key, string language)> OpenSlots()
        {
            foreach (string key in file.Keys)
                foreach (string language in settings.languages)
                    if (LocalizationFile.IsMissing(file.Get(key, language))) yield return (key, language);

            foreach (string key in MissingKeys())
                foreach (string language in settings.languages)
                    if (LocalizationFile.IsMissing(file.Get(key, language))) yield return (key, language);
        }
    }
}
