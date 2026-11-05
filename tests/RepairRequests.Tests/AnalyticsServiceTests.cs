using RepairRequests.Core.Models;
using RepairRequests.Core.Services;

namespace RepairRequests.Tests;

public class AnalyticsServiceTests
{
    private static readonly DateTime From = new(2026, 3, 1);
    private static readonly DateTime To = new(2026, 3, 31);

    private static IReadOnlyList<RepairRequest> PeriodRequests()
    {
        var completed = Sample.Request(
            1, RequestStatus.Completed,
            new DateTime(2026, 3, 2, 8, 0, 0),
            new DateTime(2026, 3, 2, 10, 0, 0),
            new DateTime(2026, 3, 2, 14, 0, 0),
            labor: 1000m, parts: 500m,
            category: "Двигатель",
            priority: RequestPriority.High);

        var closed = Sample.Request(
            2, RequestStatus.Closed,
            new DateTime(2026, 3, 10, 8, 0, 0),
            new DateTime(2026, 3, 10, 9, 0, 0),
            new DateTime(2026, 3, 10, 12, 0, 0),
            labor: 2000m, parts: 0m,
            category: "Подвеска");

        var cancelled = Sample.Request(
            3, RequestStatus.Cancelled,
            new DateTime(2026, 3, 20, 10, 0, 0),
            labor: 0m, parts: 0m,
            category: "Кузов");

        var aprilNew = Sample.Request(
            4, RequestStatus.New,
            new DateTime(2026, 4, 5, 9, 0, 0),
            category: "Двигатель");

        var aprilInProgress = Sample.Request(
            5, RequestStatus.InProgress,
            new DateTime(2026, 4, 6, 9, 0, 0),
            new DateTime(2026, 4, 6, 11, 0, 0),
            category: "Подвеска");

        return new[] { completed, closed, cancelled, aprilNew, aprilInProgress };
    }

    [Fact]
    public void Build_FiltersRequestsByCreatedDate()
    {
        var report = AnalyticsService.Build(PeriodRequests().ToList(), From, To);

        Assert.Equal(3, report.TotalRequests);
        Assert.All(report.Requests, r => Assert.InRange(r.CreatedAt.Date, From, To));
        Assert.Equal(From, report.PeriodFrom);
        Assert.Equal(To, report.PeriodTo);
    }

    [Fact]
    public void Build_ComputesCountersAndCosts()
    {
        var report = AnalyticsService.Build(PeriodRequests().ToList(), From, To);

        Assert.Equal(2, report.CompletedRequests);
        Assert.Equal(0, report.OpenRequests);
        Assert.Equal(1, report.CancelledRequests);
        Assert.Equal(3500m, report.TotalCost);
        Assert.Equal(1166.67m, report.AverageCost);
        Assert.Equal(66.7, report.CompletionRate);
    }

    [Fact]
    public void Build_ComputesAverageDowntimeAndRepairTime()
    {
        var report = AnalyticsService.Build(PeriodRequests().ToList(), From, To);

        // Простой: 6 ч и 4 ч; средний ремонт: 4 ч и 3 ч.
        Assert.Equal(5.0, report.AverageDowntimeHours);
        Assert.Equal(3.5, report.Statistics.MeanHours);
        Assert.Equal(2, report.Statistics.Count);
    }

    [Fact]
    public void Build_BuildsStatusAndPriorityDistribution()
    {
        var report = AnalyticsService.Build(PeriodRequests().ToList(), From, To);

        Assert.Equal(3, report.ByStatus.Count);
        Assert.Equal(1, report.ByStatus.Single(s => s.Key == "Выполнена").Count);
        Assert.Equal(1, report.ByStatus.Single(s => s.Key == "Закрыта").Count);
        Assert.Equal(1, report.ByStatus.Single(s => s.Key == "Отменена").Count);

        Assert.Equal(1, report.ByPriority.Single(p => p.Key == "Высокий").Count);
        Assert.Equal(2, report.ByPriority.Single(p => p.Key == "Обычный").Count);
        Assert.All(report.ByStatus, item => Assert.True(item.Share > 0 && item.Share <= 100));
    }

    [Fact]
    public void Build_GroupsDowntimeByCategory()
    {
        var report = AnalyticsService.Build(PeriodRequests().ToList(), From, To);

        var engine = report.ByCategory.Single(g => g.Key == "Двигатель");
        var suspension = report.ByCategory.Single(g => g.Key == "Подвеска");
        var body = report.ByCategory.Single(g => g.Key == "Кузов");

        Assert.Equal(1, engine.Count);
        Assert.Equal(4.0, engine.AverageRepairHours);
        Assert.Equal(3.0, suspension.AverageRepairHours);
        Assert.Equal(1, body.Count);
    }

    [Fact]
    public void Build_GroupsDowntimeByVehicle()
    {
        var report = AnalyticsService.Build(PeriodRequests().ToList(), From, To);

        Assert.Equal(3, report.ByVehicle.Count);
        Assert.All(report.ByVehicle, g => Assert.Equal(1, g.Count));
    }

    [Fact]
    public void Build_BuildsMonthlySeries()
    {
        var report = AnalyticsService.Build(PeriodRequests().ToList(), From, To);

        var march = Assert.Single(report.ByMonth);
        Assert.Equal("03.2026", march.Label);
        Assert.Equal(3, march.Created);
        Assert.Equal(2, march.Completed);
        Assert.Equal(3.5, march.AverageRepairHours);
        Assert.Equal(3500m, march.TotalCost);
    }

    [Fact]
    public void Build_EmptyPeriod_ReturnsZeroesWithoutExceptions()
    {
        var report = AnalyticsService.Build(Array.Empty<RepairRequest>(), From, To);

        Assert.Equal(0, report.TotalRequests);
        Assert.Equal(0, report.CompletedRequests);
        Assert.Equal(0, report.OpenRequests);
        Assert.Equal(0, report.TotalCost);
        Assert.Equal(0, report.AverageCost);
        Assert.Equal(0, report.CompletionRate);
        Assert.Equal(0, report.AverageDowntimeHours);
        Assert.False(report.Statistics.HasData);
        Assert.Empty(report.ByStatus);
        Assert.Empty(report.ByCategory);
        var month = Assert.Single(report.ByMonth);
        Assert.Equal("03.2026", month.Label);
        Assert.Equal(0, month.Created);
        Assert.Equal(0, month.Completed);
        Assert.Equal(0m, month.TotalCost);
    }

    [Fact]
    public void Build_PeriodBoundariesAreInclusive()
    {
        var requests = new[]
        {
            Sample.Request(1, RequestStatus.Completed, From, From.AddHours(1), From.AddHours(2)),
            Sample.Request(2, RequestStatus.Completed, To, To.AddHours(1), To.AddHours(2)),
            Sample.Request(3, RequestStatus.Completed, From.AddDays(-1), From, From.AddHours(1)),
            Sample.Request(4, RequestStatus.Completed, To.AddDays(1), To.AddDays(1).AddHours(1), To.AddDays(1).AddHours(2))
        };

        var report = AnalyticsService.Build(requests, From, To);

        Assert.Equal(2, report.TotalRequests);
    }

    [Fact]
    public void Build_CountsOverdueOnlyForOpenRequests()
    {
        var overdueOpen = Sample.Request(
            1, RequestStatus.New, DateTime.Today.AddHours(8), planned: DateTime.Today.AddDays(-1));
        var overdueFinished = Sample.Request(
            2, RequestStatus.Completed, DateTime.Today.AddHours(8),
            DateTime.Today.AddHours(9), DateTime.Today.AddHours(10),
            planned: DateTime.Today.AddDays(-2));
        var futureOpen = Sample.Request(
            3, RequestStatus.Accepted, DateTime.Today.AddHours(8), planned: DateTime.Today.AddDays(2));

        var report = AnalyticsService.Build(
            new[] { overdueOpen, overdueFinished, futureOpen }, DateTime.Today, DateTime.Today);

        Assert.Equal(1, report.OverdueRequests);
    }

    [Fact]
    public void Build_UsesCustomTitleAndAuthor()
    {
        var report = AnalyticsService.Build(
            PeriodRequests().ToList(), From, To, generatedBy: "Иванов Пётр Андреевич", title: "Мой отчёт");

        Assert.Equal("Мой отчёт", report.Title);
        Assert.Equal("Иванов Пётр Андреевич", report.GeneratedBy);
    }

    [Fact]
    public void GroupDowntime_OrdersByAverageDowntimeDescending()
    {
        var requests = new[]
        {
            Sample.Request(1, RequestStatus.Completed,
                new DateTime(2026, 5, 1, 8, 0, 0), new DateTime(2026, 5, 1, 9, 0, 0),
                new DateTime(2026, 5, 1, 10, 0, 0), category: "Быстрая"),
            Sample.Request(2, RequestStatus.Completed,
                new DateTime(2026, 5, 1, 8, 0, 0), new DateTime(2026, 5, 2, 8, 0, 0),
                new DateTime(2026, 5, 3, 8, 0, 0), category: "Долгая")
        };

        var groups = AnalyticsService.GroupDowntime(requests, r => r.FaultType!.Category);

        Assert.Equal("Долгая", groups[0].Key);
        Assert.Equal("Быстрая", groups[1].Key);
    }
}
