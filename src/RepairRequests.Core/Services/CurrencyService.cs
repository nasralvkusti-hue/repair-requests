using System.Globalization;
using System.Text.Json;

namespace RepairRequests.Core.Services;

/// <summary>
/// Получение курсов USD/EUR с официального API ЦБ РФ с резервным значением
/// на случай отсутствия сети (модуль расчёта стоимости запчастей в валюте).
/// </summary>
public class CurrencyService
{
    private const string Endpoint = "https://www.cbr-xml-daily.ru/daily_json.js";

    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromSeconds(6)
    };

    /// <summary>Резервный курс, используемый при недоступности сети.</summary>
    public static CurrencyRate Fallback { get; } = new(
        Date: DateTime.Today,
        Usd: 92.50m,
        Eur: 100.20m,
        IsFallback: true,
        Error: "Использован резервный курс: сервис ЦБ РФ недоступен.");

    public async Task<CurrencyRate> GetRatesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var json = await Http.GetStringAsync(Endpoint, cancellationToken);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var date = root.TryGetProperty("Date", out var dateElement)
                       && DateTime.TryParse(dateElement.GetString(), CultureInfo.InvariantCulture,
                           DateTimeStyles.AdjustToUniversal, out var parsed)
                ? parsed.ToLocalTime()
                : DateTime.Today;

            var usd = ReadValue(root, "USD");
            var eur = ReadValue(root, "EUR");

            if (usd <= 0 || eur <= 0)
                return Fallback with { Date = date };

            return new CurrencyRate(date, usd, eur, IsFallback: false);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return Fallback with { Error = $"Не удалось получить курс ЦБ РФ: {ex.Message}. Использован резервный курс." };
        }
    }

    private static decimal ReadValue(JsonElement root, string code)
    {
        if (!root.TryGetProperty("Valute", out var valute)
            || !valute.TryGetProperty(code, out var item)
            || !item.TryGetProperty("Value", out var value))
            return 0m;

        return value.TryGetDecimal(out var result) ? result : 0m;
    }

    /// <summary>Пересчёт суммы в валюте в рубли по курсу.</summary>
    public static decimal ConvertToRub(decimal amount, decimal rate) =>
        Math.Round(amount * rate, 2);
}
