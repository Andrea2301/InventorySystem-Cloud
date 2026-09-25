using System;
using System.Globalization;

namespace InventorySystemCloud.Shared
{
    public static class CurrencyHelper
    {
        public static CultureInfo GetCultureInfo(string? currencyCode)
        {
            var code = (currencyCode ?? "USD").Trim().ToUpperInvariant();
            return code switch
            {
                "USD" => new CultureInfo("en-US"),
                "EUR" => new CultureInfo("es-ES"),
                "COP" => new CultureInfo("es-CO"),
                "MXN" => new CultureInfo("es-MX"),
                "GBP" => new CultureInfo("en-GB"),
                "BRL" => new CultureInfo("pt-BR"),
                "CLP" => new CultureInfo("es-CL"),
                "PEN" => new CultureInfo("es-PE"),
                "ARS" => new CultureInfo("es-AR"),
                _ => CultureInfo.InvariantCulture
            };
        }

        public static string GetCurrencySymbol(string? currencyCode)
        {
            var code = (currencyCode ?? "USD").Trim().ToUpperInvariant();
            return code switch
            {
                "USD" => "$",
                "EUR" => "€",
                "COP" => "$",
                "MXN" => "$",
                "GBP" => "£",
                "BRL" => "R$",
                "CLP" => "$",
                "PEN" => "S/",
                "ARS" => "$",
                _ => "$"
            };
        }

        public static string FormatAmount(decimal amount, string? currencyCode)
        {
            var code = (currencyCode ?? "USD").Trim().ToUpperInvariant();
            var culture = GetCultureInfo(code);
            var symbol = GetCurrencySymbol(code);

            // Format depending on currency rules (e.g. COP/CLP often don't need decimal cents if 0, USD/EUR standard 2 decimals)
            return code switch
            {
                "COP" or "CLP" => amount % 1 == 0 
                    ? $"{symbol} {amount:N0} {code}" 
                    : $"{symbol} {amount.ToString("N2", culture)} {code}",
                "EUR" => $"{amount.ToString("N2", culture)} {symbol}",
                "USD" => $"{symbol}{amount.ToString("N2", culture)} USD",
                "GBP" => $"{symbol}{amount.ToString("N2", culture)} GBP",
                _ => $"{symbol} {amount.ToString("N2", culture)} {code}"
            };
        }
    }
}
