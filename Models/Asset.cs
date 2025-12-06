using System.Collections.Generic;

namespace GestioneCespiti.Models
{
    public class Asset
    {
        public Dictionary<string, string> Values { get; set; }

        public Asset()
        {
            Values = new Dictionary<string, string>();
        }

        public string this[string columnName]
        {
            get => Values.ContainsKey(columnName) ? Values[columnName] : string.Empty;
            set => Values[columnName] = value ?? string.Empty;
        }

        public bool TryGetValue(string columnName, out string value)
        {
            if (Values.ContainsKey(columnName))
            {
                value = Values[columnName];
                return true;
            }
            value = string.Empty;
            return false;
        }

        public string GetValueOrDefault(string columnName, string defaultValue = "")
        {
            return Values.ContainsKey(columnName) ? Values[columnName] : defaultValue;
        }
    }
}
