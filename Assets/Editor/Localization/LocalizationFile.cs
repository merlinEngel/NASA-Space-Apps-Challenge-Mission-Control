using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace MissionGame.EditorTools
{
    // Editable in-memory copy of texts.json. Keeps the key order of the file,
    // so a save produces a small, readable git diff.
    public class LocalizationFile
    {
        public List<string> Keys { get; } = new();
        readonly Dictionary<string, Dictionary<string, string>> values = new();

        public static LocalizationFile Load(string path)
        {
            var file = new LocalizationFile();
            if (!File.Exists(path)) return file;

            var dict = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(File.ReadAllText(path))
                       ?? new Dictionary<string, Dictionary<string, string>>();
            foreach (var pair in dict)
            {
                file.Keys.Add(pair.Key);
                file.values[pair.Key] = new Dictionary<string, string>(pair.Value ?? new Dictionary<string, string>());
            }
            return file;
        }

        public bool Contains(string key) => values.ContainsKey(key);

        public IEnumerable<string> LanguagesInFile => values.Values.SelectMany(v => v.Keys).Distinct();

        // null if the key or the language does not exist
        public string Get(string key, string language) =>
            values.TryGetValue(key, out var byLanguage) && byLanguage.TryGetValue(language, out string text) ? text : null;

        public static bool IsMissing(string value) => string.IsNullOrWhiteSpace(value);

        public void Set(string key, string language, string value)
        {
            AddKey(key);
            values[key][language] = value;
        }

        // New keys go behind the last key of the same group ("ui.", "status." ...), otherwise to the end.
        public void AddKey(string key)
        {
            if (values.ContainsKey(key)) return;
            values[key] = new Dictionary<string, string>();

            string group = GroupOf(key);
            int last = Keys.FindLastIndex(k => GroupOf(k) == group);
            if (last < 0) Keys.Add(key);
            else Keys.Insert(last + 1, key);
        }

        public void Remove(string key)
        {
            Keys.Remove(key);
            values.Remove(key);
        }

        // Writes one line per key, aligned like the hand-written file, with a blank line between groups.
        // Empty values are not written, because TextTable would show them as an empty string.
        public void Save(string path, IReadOnlyList<string> languageOrder)
        {
            int width = Keys.Count == 0 ? 0 : Keys.Max(k => JsonConvert.ToString(k).Length) + 2;
            var sb = new StringBuilder("{\n");
            string lastGroup = null;

            for (int i = 0; i < Keys.Count; i++)
            {
                string key = Keys[i];
                string group = GroupOf(key);
                if (lastGroup != null && group != lastGroup) sb.Append('\n');
                lastGroup = group;

                var entries = values[key]
                    .Where(p => !IsMissing(p.Value))
                    .OrderBy(p => LanguageRank(p.Key, languageOrder))
                    .Select(p => $"{JsonConvert.ToString(p.Key)}: {JsonConvert.ToString(p.Value)}")
                    .ToList();
                string body = entries.Count == 0 ? "{}" : "{ " + string.Join(", ", entries) + " }";

                sb.Append("  ").Append((JsonConvert.ToString(key) + ":").PadRight(width)).Append(body);
                if (i < Keys.Count - 1) sb.Append(',');
                sb.Append('\n');
            }

            sb.Append("}\n");
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
        }

        static int LanguageRank(string language, IReadOnlyList<string> order)
        {
            for (int i = 0; i < order.Count; i++)
                if (order[i] == language) return i;
            return int.MaxValue;
        }

        static string GroupOf(string key)
        {
            int dot = key.IndexOf('.');
            return dot < 0 ? key : key.Substring(0, dot);
        }
    }
}
