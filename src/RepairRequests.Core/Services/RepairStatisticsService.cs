namespace RepairRequests.Core.Services;

/// <summary>
/// Описательная статистика времени ремонта и экспоненциальная модель распределения
/// (теория вероятностей, модуль В).
/// </summary>
public sealed record RepairTimeStatistics(
    int Count,
    double MeanHours,
    double MedianHours,
    double ModeHours,
    double Variance,
    double StandardDeviation,
    double MinHours,
    double MaxHours,
    double CoefficientOfVariation,
    double LambdaPerHour,
    double ExponentialMedianHours,
    double P90Hours,
    double ConfidenceLowHours,
    double ConfidenceHighHours)
{
    public static RepairTimeStatistics Empty { get; } = new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

    public bool HasData => Count > 0;

    /// <summary>Вероятность того, что ремонт завершится не дольше t часов: F(t) = 1 − e^(−λt).</summary>
    public double ProbabilityLessThan(double hours) =>
        LambdaPerHour <= 0 ? 0 : 1 - Math.Exp(-LambdaPerHour * Math.Max(0, hours));

    /// <summary>Вероятность того, что ремонт продлится дольше t часов: P(T &gt; t) = e^(−λt).</summary>
    public double ProbabilityGreaterThan(double hours) =>
        LambdaPerHour <= 0 ? 0 : Math.Exp(-LambdaPerHour * Math.Max(0, hours));
}

public static class RepairStatisticsService
{
    /// <summary>Считает показатели по массиву длительностей ремонта (в часах).</summary>
    public static RepairTimeStatistics Calculate(IEnumerable<double> repairHours)
    {
        var data = repairHours.Where(h => !double.IsNaN(h) && !double.IsInfinity(h) && h >= 0)
            .OrderBy(h => h)
            .ToArray();

        if (data.Length == 0)
            return RepairTimeStatistics.Empty;

        var n = data.Length;
        var mean = data.Average();

        double median;
        var middle = n / 2;
        median = n % 2 == 1
            ? data[middle]
            : (data[middle - 1] + data[middle]) / 2.0;

        // Мода — середина наиболее частого часового интервала.
        var modeBin = data
            .GroupBy(h => Math.Floor(h))
            .Select(g => new { Bin = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Bin)
            .First();
        var mode = modeBin.Bin + 0.5;

        var variance = n > 1
            ? data.Sum(h => Math.Pow(h - mean, 2)) / (n - 1)
            : 0;
        var std = Math.Sqrt(variance);
        var cv = mean > 0 ? std / mean : 0;
        var lambda = mean > 0 ? 1 / mean : 0;
        var expMedian = lambda > 0 ? Math.Log(2) / lambda : 0;
        var p90 = lambda > 0 ? -Math.Log(0.1) / lambda : 0;
        var margin = n > 1 ? 1.96 * std / Math.Sqrt(n) : 0;

        return new RepairTimeStatistics(
            Count: n,
            MeanHours: Round(mean),
            MedianHours: Round(median),
            ModeHours: Round(mode),
            Variance: Round(variance),
            StandardDeviation: Round(std),
            MinHours: Round(data[0]),
            MaxHours: Round(data[^1]),
            CoefficientOfVariation: Round(cv),
            LambdaPerHour: Round(lambda, 4),
            ExponentialMedianHours: Round(expMedian),
            P90Hours: Round(p90),
            ConfidenceLowHours: Round(Math.Max(0, mean - margin)),
            ConfidenceHighHours: Round(mean + margin));
    }

    public static RepairTimeStatistics Calculate(IEnumerable<Models.RepairRequest> requests) =>
        Calculate(requests
            .Where(r => r.RepairHours.HasValue)
            .Select(r => r.RepairHours!.Value));

    private static double Round(double value, int digits = 2) => Math.Round(value, digits);
}
