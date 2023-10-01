using System.Globalization;

namespace Berger.Extensions.Localization
{
    public static class CultureConfiguration
    {
        public static void ConfigureCultureName(string name)
        {
            var culture = new CultureInfo(name);

            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
        }
        public static void ConfigureCultureLanguage(string language)
        {
            var culture = CultureInfo.GetCultureInfo(language);

            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
        }
    }
}