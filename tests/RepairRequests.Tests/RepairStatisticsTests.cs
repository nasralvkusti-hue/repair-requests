using RepairRequests.Core.Models;
using RepairRequests.Core.Services;

namespace RepairRequests.Tests;

public class RepairStatisticsTests
{
    [Fact]
    public void Calculate_EmptySet_ReturnsEmptyStatistics()
    {
        var stats = RepairStatisticsService.Calculate(Array.Empty<double>());

        Assert.False(stats.HasData);
        Assert.Equal(0, stats.Count);
        Assert.Equal(0, stats.MeanHours);
        Assert.Equal(0, stats.LambdaPerHour);
        Assert.Equal(0, stats.ProbabilityLessThan(8));
        Assert.Equal(0, stats.ProbabilityGreaterThan(8));
    }

    [Fact]
    public void Calculate_IgnoresNegativeAndNonFiniteValues()
    {
        var stats = RepairStatisticsService.Calculate(new[] { -1.0, double.NaN, double.PositiveInfinity, 2.0, 4.0 });

        Assert.Equal(2, stats.Count);
        Assert.Equal(3.0, stats.MeanHours);
        Assert.Equal(2.0, stats.MinHours);
        Assert.Equal(4.0, stats.MaxHours);
    }

    [Fact]
    public void Calculate_ComputesDescriptiveStatistics()
    {
        var stats = RepairStatisticsService.Calculate(new[] { 1.0, 2.0, 3.0, 4.0 });

        Assert.True(stats.HasData);
        Assert.Equal(4, stats.Count);
        Assert.Equal(2.5, stats.MeanHours);
        Assert.Equal(2.5, stats.MedianHours);
        Assert.Equal(1.67, stats.Variance);
        Assert.Equal(1.29, stats.StandardDeviation);
        Assert.Equal(0.52, stats.CoefficientOfVariation);
        Assert.Equal(1.0, stats.MinHours);
        Assert.Equal(4.0, stats.MaxHours);
    }

    [Fact]
    public void Calculate_UsesAverageOfTwoMiddleValues_ForEvenCount()
    {
        var stats = RepairStatisticsService.Calculate(new[] { 10.0, 2.0, 8.0, 4.0 });

        Assert.Equal(6.0, stats.MedianHours);
    }

    [Fact]
    public void Calculate_ReturnsMiddleValue_ForOddCount()
    {
        var stats = RepairStatisticsService.Calculate(new[] { 5.0, 1.0, 3.0 });

        Assert.Equal(3.0, stats.MedianHours);
    }

    [Fact]
    public void Calculate_ModeIsMiddleOfMostFrequentHourBin()
    {
        var stats = RepairStatisticsService.Calculate(new[] { 1.1, 1.4, 1.9, 5.2, 5.8 });

        Assert.Equal(1.5, stats.ModeHours);
    }

    [Fact]
    public void Calculate_BuildsExponentialModel()
    {
        var stats = RepairStatisticsService.Calculate(new[] { 1.0, 2.0, 3.0, 4.0 });

        // λ = 1 / x̄ = 0,4; медиана = ln 2 / λ; P90 = −ln 0,1 / λ.
        Assert.Equal(0.4, stats.LambdaPerHour, 3);
        Assert.Equal(1.73, stats.ExponentialMedianHours);
        Assert.Equal(5.76, stats.P90Hours);
    }

    [Fact]
    public void Calculate_SingleValue_HasZeroDispersion()
    {
        var stats = RepairStatisticsService.Calculate(new[] { 4.0 });

        Assert.Equal(1, stats.Count);
        Assert.Equal(4.0, stats.MeanHours);
        Assert.Equal(0.0, stats.Variance);
        Assert.Equal(0.0, stats.StandardDeviation);
        Assert.Equal(4.0, stats.ConfidenceLowHours);
        Assert.Equal(4.0, stats.ConfidenceHighHours);
    }

    [Fact]
    public void ConfidenceInterval_ContainsMean_AndIsNonNegative()
    {
        var stats = RepairStatisticsService.Calculate(new[] { 1.0, 3.0, 9.0, 12.0, 20.0 });

        Assert.True(stats.ConfidenceLowHours >= 0);
        Assert.True(stats.ConfidenceLowHours <= stats.MeanHours);
        Assert.True(stats.ConfidenceHighHours >= stats.MeanHours);
    }

    [Fact]
    public void Probabilities_AreComplementaryAndMonotonic()
    {
        var stats = RepairStatisticsService.Calculate(new[] { 2.0, 4.0, 6.0, 8.0 });

        Assert.Equal(0, stats.ProbabilityLessThan(0), 6);
        Assert.Equal(1, stats.ProbabilityGreaterThan(0), 6);

        var less = stats.ProbabilityLessThan(5);
        var greater = stats.ProbabilityGreaterThan(5);
        Assert.Equal(1.0, less + greater, 6);
        Assert.True(stats.ProbabilityLessThan(10) > less);
        Assert.True(stats.ProbabilityGreaterThan(10) < greater);
    }

    [Fact]
    public void Calculate_OverRequests_UsesOnlyFinishedRepairs()
    {
        var requests = new[]
        {
            Sample.Request(1, RequestStatus.Completed,
                new DateTime(2026, 1, 1, 8, 0, 0),
                new DateTime(2026, 1, 1, 10, 0, 0),
                new DateTime(2026, 1, 1, 12, 0, 0)),
            Sample.Request(2, RequestStatus.New, new DateTime(2026, 1, 2, 8, 0, 0)),
            Sample.Request(3, RequestStatus.InProgress,
                new DateTime(2026, 1, 3, 8, 0, 0),
                new DateTime(2026, 1, 3, 9, 0, 0))
        };

        var stats = RepairStatisticsService.Calculate(requests);

        Assert.Equal(1, stats.Count);
        Assert.Equal(2.0, stats.MeanHours);
    }
}
