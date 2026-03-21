using System.Globalization;

namespace EMenu.Web.Extensions
{
    public static class CurrencyExtensions
    {
        private static readonly CultureInfo ViVnCulture = CultureInfo.GetCultureInfo("vi-VN");

        public static string ToVnd(this decimal amount)
        {
            return string.Format(ViVnCulture, "{0:N0} đ", amount);
        }

        public static string ToVnd(this decimal? amount)
        {
            return amount.HasValue
                ? amount.Value.ToVnd()
                : "0 đ";
        }
    }
}
