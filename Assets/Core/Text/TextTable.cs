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
                        culture = new("en-US");
                        break;
                    case "de":
                        culture = new("de-DE");
                        break;
                }
            }
        }
        private string _language = "en";
        private CultureInfo culture = new("en-US");

        public TextTable(Dictionary<string, Dictionary<string, string>> textTable)
        {
            this.textTable = textTable;
        }

        public bool Contains(string key) => textTable.ContainsKey(key);

        public string Get(string key, params object[] args)
        {
            if (textTable.TryGetValue(key, out Dictionary<string, string> value))
            {
                if (value.TryGetValue(Language, out string text))
                {
                    return string.Format(culture, text, args);
                }
                else return $"Language {Language} missing!";
            }
            else return key;
        }
    }
}