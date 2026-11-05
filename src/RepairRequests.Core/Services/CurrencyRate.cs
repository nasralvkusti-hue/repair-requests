namespace RepairRequests.Core.Services;

/// <summary>Курс валют, полученный от внешнего API (ЦБ РФ) или из резервного источника.</summary>
public sealed record CurrencyRate(
    DateTime Date,
    decimal Usd,
    decimal Eur,
    bool IsFallback,
    string? Error = null)
{
    public decimal GetRate(string code) => code.ToUpperInvariant() switch
    {
        "USD" => Usd,
        "EUR" => Eur,
        _ => 1m
    };
}
