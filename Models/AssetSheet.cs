using System;
using System.Collections.Generic;
using System.Linq;

namespace GestioneCespiti.Models
{
    public class AssetSheet
    {
        private string _header = "Foglio Senza Nome";

        public string Header
        {
            get => _header;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    _header = "Foglio Senza Nome";
                    Services.Logger.LogWarning("Header vuoto, impostato a 'Foglio Senza Nome'");
                    return;
                }

                if (value.Length > 100)
                {
                    _header = value.Substring(0, 100).Trim();
                    Services.Logger.LogWarning($"Header troppo lungo, troncato a 100 caratteri: {value}");
                    return;
                }

                _header = value.Trim();
            }
        }

        public List<string> Columns { get; set; }
        public List<Asset> Rows { get; set; }
        public string FileName { get; set; }
        public bool IsArchived { get; set; }

        public AssetSheet()
        {
            Header = "Nuovo Foglio";
            Columns = new List<string>();
            Rows = new List<Asset>();
            FileName = string.Empty;
            IsArchived = false;
        }

        public static AssetSheet CreateNew(string header)
        {
            var sheet = new AssetSheet
            {
                Header = header,
                Columns = new List<string>
                {
                    "Tipo asset",
                    "Marca",
                    "Modello",
                    "Seriale",
                    "Rif inventario",
                    "Descrizione",
                    "Causa dismissione"
                }
            };
            return sheet;
        }

        public void AddColumn(string columnName)
        {
            if (string.IsNullOrWhiteSpace(columnName))
                throw new ArgumentException("Column name cannot be empty");

            columnName = columnName.Trim();

            if (Columns.Any(c => c.Equals(columnName, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException($"Column '{columnName}' already exists");

            Columns.Add(columnName);
        }

        public bool IsValid(out string error)
        {
            if (string.IsNullOrWhiteSpace(_header))
            {
                error = "Header non può essere vuoto";
                return false;
            }

            if (_header.Length > 100)
            {
                error = "Header troppo lungo (max 100 caratteri)";
                return false;
            }

            if (Columns == null || Columns.Count == 0)
            {
                error = "Il foglio deve avere almeno una colonna";
                return false;
            }

            error = string.Empty;
            return true;
        }

        public static int StandardColumnCount => 7;
    }
}
