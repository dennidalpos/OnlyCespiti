using System.Collections.Generic;
using System.Linq;

namespace GestioneCespiti.Models
{
    public class AppSettings
    {
        private static readonly List<string> DefaultOptions = new List<string>
        {
            "Obsolescenza",
            "Guasto",
            "Non conforme"
        };

        public List<string> CauseDismissioneOptions { get; set; }

        public AppSettings()
        {
            CauseDismissioneOptions = new List<string>(DefaultOptions);
        }

        public static bool IsDefaultOption(string option)
        {
            return DefaultOptions.Any(o => o.Equals(option, System.StringComparison.OrdinalIgnoreCase));
        }

        public static List<string> GetDefaultOptions()
        {
            return new List<string>(DefaultOptions);
        }
    }
}
