using System;
using System.Collections.Generic;
using System.Globalization;

namespace MissionCore
{
    public class TextTable
    {
        private Dictionary<string, Dictionary<string, string>> textTable = new();
        public string Language
        {
            get => _language;
            set
            {
                _language = value;
                switch (value)
                {
                    case "en":
                        Culture = new("en-US");
                        break;
                    case "de":
                        Culture = new("de-DE");
                        break;
                }
            }
        }
        private string _language = "de";
        public CultureInfo Culture { get; private set; } = new("en-US");

        readonly HashSet<string> missingKeys = new();
        public IReadOnlyCollection<string> MissingKeys => missingKeys;

        public TextTable(Dictionary<string, Dictionary<string, string>> textTable)
        {
            this.textTable = textTable;
        }

        public void ReportMissing(string key) => missingKeys.Add(key);

        public bool Contains(string key) => textTable.ContainsKey(key);

        public string Get(string key, params object[] args)
        {
            if (textTable.TryGetValue(key, out Dictionary<string, string> value))
            {
                if (value.TryGetValue(Language, out string text))
                {
                    if (args == null || args.Length == 0) return text;
                    try { return string.Format(Culture, text, args); }
                    catch (FormatException)
                    {
                        return text;
                    }
                }
                else return $"Language {Language} missing!";
            }
            else { missingKeys.Add(key); return key; }
        }
    }
}