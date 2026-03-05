using System.Globalization;
using System.Resources;

namespace Clippy.Localization
{
    public static class L
    {
        private static ResourceManager _rm = new("Clippy.Localization.Strings", typeof(L).Assembly);

        public static string Get(string key)
        {
            return _rm.GetString(key, CultureInfo.CurrentUICulture) ?? key;
        }

        public static string Get(string key, params object[] args)
        {
            var template = Get(key);
            return string.Format(template, args);
        }

        public static void SetLanguage(string cultureCode)
        {
            var culture = new CultureInfo(cultureCode);
            CultureInfo.CurrentUICulture = culture;
            CultureInfo.CurrentCulture = culture;
        }
    }
}
