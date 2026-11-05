using RepairRequests.Core.Models;

namespace RepairRequests.Core.Services;

/// <summary>Срез простоя в разрезе категории, автомобиля или месяца (модуль Г).</summary>
public sealed record DowntimeGroup(
    string Key,
    int Count,
    double AverageTotalHours,
    double AverageRepairHours,
    double AverageWaitingHours,
    double MaxTotalHours);

/// <summary>Точка месячного ряда для диаграмм и отчёта.</summary>
public sealed record MonthlyPoint(
    int Year,
    int Month,
    string Label,
    int Created,
    int Completed,
    double AverageRepairHours,
    decimal TotalCost);

/// <summary>Распределение заявок по значению (статус, приоритет, категория).</summary>
public sealed record DistributionItem(string Key, int Count, double Share);

/// <summary>Итоговая аналитика за период: основа для отчёта и диаграмм.</summary>
public sealed class ReportData
{
    public string Title { get; init; } = "Отчёт по заявкам на ремонт";
    public DateTime PeriodFrom { get; init; }
    public DateTime PeriodTo { get; init; }
    public DateTime GeneratedAt { get; init; } = DateTime.Now;
    public string GeneratedBy { get; init; } = string.Empty;

    public IReadOnlyList<RepairRequest> Requests { get; init; } = Array.Empty<RepairRequest>();
    public RepairTimeStatistics Statistics { get; init; } = RepairTimeStatistics.Empty;

    public int TotalRequests { get; init; }
    public int CompletedRequests { get; init; }
    public int OpenRequests { get; init; }
    public int CancelledRequests { get; init; }
    public int OverdueRequests { get; init; }
    public decimal TotalCost { get; init; }
    public decimal AverageCost { get; init; }
    public double AverageDowntimeHours { get; init; }
    public double AverageWaitingHours { get; init; }
    public double CompletionRate { get; init; }

    public IReadOnlyList<DistributionItem> ByStatus { get; init; } = Array.Empty<DistributionItem>();
    public IReadOnlyList<DistributionItem> ByPriority { get; init; } = Array.Empty<DistributionItem>();
    public IReadOnlyList<DowntimeGroup> ByCategory { get; init; } = Array.Empty<DowntimeGroup>();
    public IReadOnlyList<DowntimeGroup> ByVehicle { get; init; } = Array.Empty<DowntimeGroup>();
    public IReadOnlyList<MonthlyPoint> ByMonth { get; init; } = Array.Empty<MonthlyPoint>();
}

/// <summary>
/// Аналитика простоев и сводные показатели (модуль Г): группировка по категориям
/// неисправностей, автомобилям и месяцам, расчёт средних длительностей.
/// </summary>
public static class AnalyticsService
{
    public static ReportData Build(
        IReadOnlyList<RepairRequest> requests,
        DateTime from,
        DateTime to,
        string? generatedBy = null,
        string? title = null)
    {
        var list = requests
            .Where(r => r.CreatedAt.Date >= from.Date && r.CreatedAt.Date <= to.Date)
            .OrderByDescending(r => r.CreatedAt)
            .ToList();

        var completed = list
            .Where(r => r.Status is RequestStatus.Completed or RequestStatus.Closed)
            .ToList();

        var stats = RepairStatisticsService.Calculate(
            list.Where(r => r.RepairHours.HasValue).Select(r => r.RepairHours!.Value));

        var finished = list.Where(r => r.CompletedAt.HasValue || r.ClosedAt.HasValue).ToList();
        var avgDowntime = finished.Count == 0 ? 0 : finished.Average(r => r.TotalDowntimeHours);
        var waiting = list.Where(r => r.WaitingHours.HasValue).Select(r => r.WaitingHours!.Value).ToList();
        var costs = list.Select(r => r.TotalCost).ToList();

        return new ReportData
        {
            Title = title ?? "Отчёт по заявкам на ремонт",
            PeriodFrom = from.Date,
            PeriodTo = to.Date,
            GeneratedAt = DateTime.Now,
            GeneratedBy = generatedBy ?? string.Empty,
            Requests = list,
            Statistics = stats,
            TotalRequests = list.Count,
            CompletedRequests = completed.Count,
            OpenRequests = list.Count(r => r.Status.IsOpen()),
            CancelledRequests = list.Count(r => r.Status == RequestStatus.Cancelled),
            OverdueRequests = list.Count(r => r.IsOverdue),
            TotalCost = costs.Sum(),
            AverageCost = costs.Count == 0 ? 0 : Math.Round(costs.Average(), 2),
            AverageDowntimeHours = Math.Round(avgDowntime, 2),
            AverageWaitingHours = Math.Round(waiting.Count == 0 ? 0 : waiting.Average(), 2),
            CompletionRate = list.Count == 0 ? 0 : Math.Round(completed.Count * 100.0 / list.Count, 1),
            ByStatus = Distribution(list, r => r.Status.Ru()),
            ByPriority = Distribution(list, r => r.Priority.Ru()),
            ByCategory = GroupDowntime(list, r => r.FaultType?.Category ?? "Без категории"),
            ByVehicle = GroupDowntime(list, r => r.Vehicle?.DisplayName ?? $"ТС №{r.VehicleId}"),
            ByMonth = MonthlySeries(list, from, to)
        };
    }

    /// <summary>Группировка по произвольному ключу с расчётом средних простоев.</summary>
    public static IReadOnlyList<DowntimeGroup> GroupDowntime(
        IReadOnlyList<RepairRequest> requests,
        Func<RepairRequest, string> keySelector)
    {
        return requests
            .GroupBy(keySelector)
            .Select(g =>
            {
                var repaired = g.Where(r => r.RepairHours.HasValue).Select(r => r.RepairHours!.Value).ToList();
                var waiting = g.Where(r => r.WaitingHours.HasValue).Select(r => r.WaitingHours!.Value).ToList();

                return new DowntimeGroup(
                    Key: g.Key,
                    Count: g.Count(),
                    AverageTotalHours: Math.Round(g.Average(r => r.TotalDowntimeHours), 2),
                    AverageRepairHours: Math.Round(repaired.Count == 0 ? 0 : repaired.Average(), 2),
                    AverageWaitingHours: Math.Round(waiting.Count == 0 ? 0 : waiting.Average(), 2),
                    MaxTotalHours: Math.Round(g.Max(r => r.TotalDowntimeHours), 2));
            })
            .OrderByDescending(g => g.AverageTotalHours)
            .ThenByDescending(g => g.Count)
            .ToList();
    }

    /// <summary>Месячный ряд: сколько создано, сколько завершено, средний ремонт и затраты.</summary>
    public static IReadOnlyList<MonthlyPoint> MonthlySeries(
        IReadOnlyList<RepairRequest> requests,
        DateTime from,
        DateTime to)
    {
        var start = new DateTime(from.Year, from.Month, 1);
        var end = new DateTime(to.Year, to.Month, 1);
        var result = new List<MonthlyPoint>();

        for (var month = start; month <= end; month = month.AddMonths(1))
        {
            var created = requests
                .Where(r => r.CreatedAt.Year == month.Year && r.CreatedAt.Month == month.Month)
                .ToList();

            var completed = requests
                .Where(r => r.CompletedAt.HasValue
                            && r.CompletedAt.Value.Year == month.Year
                            && r.CompletedAt.Value.Month == month.Month
                            && r.Status is RequestStatus.Completed or RequestStatus.Closed)
                .ToList();

            var repairs = completed
                .Where(r => r.RepairHours.HasValue)
                .Select(r => r.RepairHours!.Value)
                .ToList();

            result.Add(new MonthlyPoint(
                Year: month.Year,
                Month: month.Month,
                Label: month.ToString("MM.yyyy"),
                Created: created.Count,
                Completed: completed.Count,
                AverageRepairHours: Math.Round(repairs.Count == 0 ? 0 : repairs.Average(), 2),
                TotalCost: created.Sum(r => r.TotalCost)));
        }

        return result;
    }

    private static IReadOnlyList<DistributionItem> Distribution(
        IReadOnlyList<RepairRequest> requests,
        Func<RepairRequest, string> keySelector)
    {
        if (requests.Count == 0)
            return Array.Empty<DistributionItem>();

        return requests
            .GroupBy(keySelector)
            .Select(g => new DistributionItem(
                Key: g.Key,
                Count: g.Count(),
                Share: Math.Round(g.Count() * 100.0 / requests.Count, 1)))
            .OrderByDescending(x => x.Count)
            .ToList();
    }
}
