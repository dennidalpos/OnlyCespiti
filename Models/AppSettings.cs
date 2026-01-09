using System.Collections.Generic;
using System.Linq;

namespace GestioneCespiti.Models
{
    public class AppSettings
    {
        private static readonly List<string> DefaultCauseDismissioneOptions = new List<string>
        {
            "Obsolescenza",
            "Guasto",
            "Non conforme"
        };

        public List<string> CauseDismissioneOptions { get; set; }
        public List<string> TipoAssetOptions { get; set; }

        public AppSettings()
        {
            CauseDismissioneOptions = new List<string>(DefaultCauseDismissioneOptions);
            TipoAssetOptions = new List<string>();
        }

        public static bool IsDefaultOption(string option)
        {
            return DefaultCauseDismissioneOptions.Any(o => o.Equals(option, System.StringComparison.OrdinalIgnoreCase));
        }

        public static List<string> GetDefaultOptions()
        {
            return new List<string>(DefaultCauseDismissioneOptions);
        }
    }
}
