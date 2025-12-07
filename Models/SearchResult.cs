namespace GestioneCespiti.Models
{
    public class SearchResult
    {
        public AssetSheet Sheet { get; set; } = null!;
        public int RowIndex { get; set; }
        public string ColumnName { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
}
