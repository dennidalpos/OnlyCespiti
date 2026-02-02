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
            get
            {
                EnsureValues();
                return Values.TryGetValue(columnName, out var value) ? value : string.Empty;
            }
            set
            {
                EnsureValues();
                Values[columnName] = value ?? string.Empty;
            }
        }

        public bool TryGetValue(string columnName, out string value)
        {
            EnsureValues();
            return Values.TryGetValue(columnName, out value!);
        }

        public string GetValueOrDefault(string columnName, string defaultValue = "")
        {
            EnsureValues();
            return Values.TryGetValue(columnName, out var value) ? value : defaultValue;
        }

        private void EnsureValues()
        {
            Values ??= new Dictionary<string, string>();
        }
    }
}
