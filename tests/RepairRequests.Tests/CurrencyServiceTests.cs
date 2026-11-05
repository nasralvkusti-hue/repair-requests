using System.Text;
using RepairRequests.Core.Services;

namespace RepairRequests.Tests;

public class CurrencyServiceTests
{
    [Fact]
    public void Fallback_HasPositiveRatesAndFlag()
    {
        var fallback = CurrencyService.Fallback;

        Assert.True(fallback.IsFallback);
        Assert.True(fallback.Usd > 0);
        Assert.True(fallback.Eur > 0);
        Assert.False(string.IsNullOrWhiteSpace(fallback.Error));
    }

    [Theory]
    [InlineData(100, 92.50, 9250.00)]
    [InlineData(10, 100.20, 1002.00)]
    [InlineData(3.33, 92.50, 308.02)]
    [InlineData(0, 92.50, 0)]
    public void ConvertToRub_RoundsToKopecks(decimal amount, decimal rate, decimal expected)
    {
        Assert.Equal(expected, CurrencyService.ConvertToRub(amount, rate));
    }

    [Fact]
    public async Task GetRatesAsync_ReturnsUsableRatesEvenWithoutNetwork()
    {
        var service = new CurrencyService();

        var rate = await service.GetRatesAsync();

        Assert.True(rate.Usd > 0);
        Assert.True(rate.Eur > 0);
        Assert.True(rate.Date <= DateTime.Today.AddDays(1));
    }
}
