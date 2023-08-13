using System.Globalization;

namespace Berger.Extensions.Localization
{
    public static class CultureHelper
    {
        public static NumberFormatInfo GetNumberFormatByCurrencySymbol(string iso)
        {
            var culture = CultureInfo
                .GetCultures(CultureTypes.SpecificCultures)
                .FirstOrDefault(x => new RegionInfo(x.Name).ISOCurrencySymbol == iso);

            if (culture is not null)
            {
                var clone = (CultureInfo)culture.Clone();

                clone.NumberFormat.CurrencySymbol = iso;

                return clone.NumberFormat;
            }

            return Thread.CurrentThread.CurrentUICulture.NumberFormat;
        }
    }
}