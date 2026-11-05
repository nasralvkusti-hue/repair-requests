using System.Text;
using ClosedXML.Excel;
using RepairRequests.Core.Repositories;
using RepairRequests.Core.Services;

namespace RepairRequests.Tests;

/// <summary>Проверка выгрузки отчёта в Excel и PDF на реальных демонстрационных данных.</summary>
public class ExportTests
{
    private static async Task<ReportData> BuildReportAsync(TestDbFactory factory)
    {
        var repo = new RepairRequestRepository(factory);
        var from = DateTime.Today.AddMonths(-6);
        var to = DateTime.Today;
        var requests = await repo.GetForPeriodAsync(from, to);

        return AnalyticsService.Build(requests, from, to, "Иванов Пётр Андреевич");
    }

    [Fact]
    public async Task ExcelExport_CreatesWorkbookWithFourSheets()
    {
        using var factory = await TestDbFactory.CreateSeededAsync();
        var report = await BuildReportAsync(factory);
        var path = Path.Combine(Path.GetTempPath(), $"report-{Guid.NewGuid():N}.xlsx");

        try
        {
            var result = ExcelExportService.Export(report, path);

            Assert.True(File.Exists(result));
            Assert.True(new FileInfo(result).Length > 5_000);

            using var workbook = new XLWorkbook(result);
            Assert.Equal(4, workbook.Worksheets.Count);
            Assert.Equal("Сводка", workbook.Worksheet(1).Name);
            Assert.Equal("Заявки", workbook.Worksheet(2).Name);
            Assert.Equal("Простои", workbook.Worksheet(3).Name);
            Assert.Equal("Статистика", workbook.Worksheet(4).Name);
            Assert.Contains("Отчёт по заявкам", workbook.Worksheet(1).Cell(1, 1).GetString());
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public async Task ExcelExport_ContainsRequestRows()
    {
        using var factory = await TestDbFactory.CreateSeededAsync();
        var report = await BuildReportAsync(factory);
        var path = Path.Combine(Path.GetTempPath(), $"report-{Guid.NewGuid():N}.xlsx");

        try
        {
            ExcelExportService.Export(report, path);
            using var workbook = new XLWorkbook(path);
            var sheet = workbook.Worksheet("Заявки");

            var lastRow = sheet.LastRowUsed()!.RowNumber();
            Assert.True(lastRow >= report.TotalRequests);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public async Task PdfExport_CreatesValidPdfFile()
    {
        using var factory = await TestDbFactory.CreateSeededAsync();
        var report = await BuildReportAsync(factory);
        var path = Path.Combine(Path.GetTempPath(), $"report-{Guid.NewGuid():N}.pdf");

        try
        {
            var result = PdfReportService.Export(report, path);

            Assert.True(File.Exists(result));
            var bytes = await File.ReadAllBytesAsync(result);
            Assert.True(bytes.Length > 1_000);
            Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));
            Assert.Contains("%%EOF", Encoding.ASCII.GetString(bytes, bytes.Length - 8, 8));
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public async Task ExcelAndPdf_HandleEmptyReport()
    {
        using var factory = await TestDbFactory.CreateSeededAsync();
        var empty = AnalyticsService.Build(
            Array.Empty<Core.Models.RepairRequest>(),
            new DateTime(2030, 1, 1),
            new DateTime(2030, 12, 31));

        var excel = Path.Combine(Path.GetTempPath(), $"report-{Guid.NewGuid():N}.xlsx");
        var pdf = Path.Combine(Path.GetTempPath(), $"report-{Guid.NewGuid():N}.pdf");

        try
        {
            ExcelExportService.Export(empty, excel);
            Assert.True(File.Exists(excel));

            PdfReportService.Export(empty, pdf);
            Assert.True(File.Exists(pdf));
            Assert.True(new FileInfo(pdf).Length > 0);
        }
        finally
        {
            if (File.Exists(excel))
                File.Delete(excel);
            if (File.Exists(pdf))
                File.Delete(pdf);
        }
    }
}
