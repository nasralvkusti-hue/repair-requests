using ClosedXML.Excel;
using RepairRequests.Core.Models;

namespace RepairRequests.Core.Services;

/// <summary>Экспорт отчёта в книгу Excel (ClosedXML): сводка, реестр заявок, простои, статистика.</summary>
public static class ExcelExportService
{
    public static string Export(ReportData data, string filePath)
    {
        using var workbook = new XLWorkbook();

        BuildSummarySheet(workbook, data);
        BuildRequestsSheet(workbook, data);
        BuildDowntimeSheet(workbook, data);
        BuildStatisticsSheet(workbook, data);

        Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? ".");
        workbook.SaveAs(filePath);
        return filePath;
    }

    private static void BuildSummarySheet(XLWorkbook workbook, ReportData data)
    {
        var sheet = workbook.Worksheets.Add("Сводка");
        sheet.Cell(1, 1).Value = data.Title;
        sheet.Cell(1, 1).Style.Font.Bold = true;
        sheet.Cell(1, 1).Style.Font.FontSize = 16;
        sheet.Range(1, 1, 1, 4).Merge();

        sheet.Cell(2, 1).Value = $"Период: {data.PeriodFrom:dd.MM.yyyy} — {data.PeriodTo:dd.MM.yyyy}";
        sheet.Cell(3, 1).Value = $"Сформирован: {data.GeneratedAt:dd.MM.yyyy HH:mm}";
        if (!string.IsNullOrWhiteSpace(data.GeneratedBy))
            sheet.Cell(4, 1).Value = $"Пользователь: {data.GeneratedBy}";

        var rows = new (string Metric, string Value)[]
        {
            ("Всего заявок", data.TotalRequests.ToString()),
            ("Выполнено и закрыто", data.CompletedRequests.ToString()),
            ("В работе / ожидают", data.OpenRequests.ToString()),
            ("Отменено", data.CancelledRequests.ToString()),
            ("Просрочено", data.OverdueRequests.ToString()),
            ("Доля выполнения, %", data.CompletionRate.ToString("0.0")),
            ("Среднее время ремонта, ч", data.Statistics.MeanHours.ToString("0.00")),
            ("Медиана времени ремонта, ч", data.Statistics.MedianHours.ToString("0.00")),
            ("Средний простой, ч", data.AverageDowntimeHours.ToString("0.00")),
            ("Среднее ожидание, ч", data.AverageWaitingHours.ToString("0.00")),
            ("Суммарная стоимость, ₽", data.TotalCost.ToString("N2")),
            ("Средняя стоимость заявки, ₽", data.AverageCost.ToString("N2"))
        };

        sheet.Cell(6, 1).Value = "Показатель";
        sheet.Cell(6, 2).Value = "Значение";
        Header(sheet.Range(6, 1, 6, 2));

        var row = 7;
        foreach (var (metric, value) in rows)
        {
            sheet.Cell(row, 1).Value = metric;
            sheet.Cell(row, 2).Value = value;
            sheet.Cell(row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            row++;
        }

        sheet.Cell(row + 1, 1).Value = "Распределение по статусам";
        sheet.Cell(row + 1, 1).Style.Font.Bold = true;
        sheet.Cell(row + 2, 1).Value = "Статус";
        sheet.Cell(row + 2, 2).Value = "Количество";
        sheet.Cell(row + 2, 3).Value = "Доля, %";
        Header(sheet.Range(row + 2, 1, row + 2, 3));

        var statusRow = row + 3;
        foreach (var item in data.ByStatus)
        {
            sheet.Cell(statusRow, 1).Value = item.Key;
            sheet.Cell(statusRow, 2).Value = item.Count;
            sheet.Cell(statusRow, 3).Value = item.Share;
            statusRow++;
        }

        sheet.Columns().AdjustToContents();
    }

    private static void BuildRequestsSheet(XLWorkbook workbook, ReportData data)
    {
        var sheet = workbook.Worksheets.Add("Заявки");
        var headers = new[]
        {
            "Номер", "Создана", "ТС", "Госномер", "Клиент", "Неисправность", "Категория",
            "Механик", "Приоритет", "Статус", "Начало работ", "Завершение",
            "Ремонт, ч", "Простой, ч", "Работы, ₽", "Запчасти, ₽", "Итого, ₽"
        };

        for (var i = 0; i < headers.Length; i++)
            sheet.Cell(1, i + 1).Value = headers[i];

        Header(sheet.Range(1, 1, 1, headers.Length));

        var row = 2;
        foreach (var request in data.Requests.OrderBy(r => r.CreatedAt))
        {
            sheet.Cell(row, 1).Value = request.Number;
            sheet.Cell(row, 2).Value = request.CreatedAt;
            sheet.Cell(row, 2).Style.DateFormat.Format = "dd.MM.yyyy HH:mm";
            sheet.Cell(row, 3).Value = request.Vehicle is null ? string.Empty : $"{request.Vehicle.Brand} {request.Vehicle.Model}";
            sheet.Cell(row, 4).Value = request.Vehicle?.LicensePlate ?? string.Empty;
            sheet.Cell(row, 5).Value = request.Client?.FullName ?? string.Empty;
            sheet.Cell(row, 6).Value = request.FaultType?.Name ?? string.Empty;
            sheet.Cell(row, 7).Value = request.FaultType?.Category ?? string.Empty;
            sheet.Cell(row, 8).Value = request.AssignedMechanic?.FullName ?? "Не назначен";
            sheet.Cell(row, 9).Value = request.Priority.Ru();
            sheet.Cell(row, 10).Value = request.Status.Ru();
            sheet.Cell(row, 11).Value = request.StartedAt;
            sheet.Cell(row, 11).Style.DateFormat.Format = "dd.MM.yyyy HH:mm";
            sheet.Cell(row, 12).Value = request.CompletedAt;
            sheet.Cell(row, 12).Style.DateFormat.Format = "dd.MM.yyyy HH:mm";
            if (request.RepairHours.HasValue)
                sheet.Cell(row, 13).Value = Math.Round(request.RepairHours.Value, 2);
            sheet.Cell(row, 14).Value = Math.Round(request.TotalDowntimeHours, 2);
            sheet.Cell(row, 15).Value = request.LaborCost;
            sheet.Cell(row, 16).Value = request.PartsCost;
            sheet.Cell(row, 17).Value = request.TotalCost;
            sheet.Range(row, 15, row, 17).Style.NumberFormat.Format = "#,##0.00";
            row++;
        }

        if (data.Requests.Count > 0)
        {
            var table = sheet.Range(1, 1, row - 1, headers.Length);
            table.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            table.Style.Border.SetInsideBorder(XLBorderStyleValues.Hair);
            sheet.SheetView.FreezeRows(1);
            sheet.Range(1, 1, row - 1, headers.Length).SetAutoFilter();
        }

        sheet.Columns().AdjustToContents();
    }

    private static void BuildDowntimeSheet(XLWorkbook workbook, ReportData data)
    {
        var sheet = workbook.Worksheets.Add("Простои");
        sheet.Cell(1, 1).Value = "Простои по категориям неисправностей";
        sheet.Cell(1, 1).Style.Font.Bold = true;
        sheet.Cell(1, 1).Style.Font.FontSize = 13;

        var headers = new[] { "Категория", "Заявок", "Средний простой, ч", "Средний ремонт, ч", "Среднее ожидание, ч", "Макс. простой, ч" };
        for (var i = 0; i < headers.Length; i++)
            sheet.Cell(3, i + 1).Value = headers[i];
        Header(sheet.Range(3, 1, 3, headers.Length));

        var row = 4;
        foreach (var group in data.ByCategory)
        {
            sheet.Cell(row, 1).Value = group.Key;
            sheet.Cell(row, 2).Value = group.Count;
            sheet.Cell(row, 3).Value = group.AverageTotalHours;
            sheet.Cell(row, 4).Value = group.AverageRepairHours;
            sheet.Cell(row, 5).Value = group.AverageWaitingHours;
            sheet.Cell(row, 6).Value = group.MaxTotalHours;
            row++;
        }

        row += 2;
        sheet.Cell(row, 1).Value = "Простои по транспортным средствам";
        sheet.Cell(row, 1).Style.Font.Bold = true;
        sheet.Cell(row, 1).Style.Font.FontSize = 13;

        for (var i = 0; i < headers.Length; i++)
            sheet.Cell(row + 1, i + 1).Value = i == 0 ? "Транспортное средство" : headers[i];
        Header(sheet.Range(row + 1, 1, row + 1, headers.Length));

        row += 2;
        foreach (var group in data.ByVehicle)
        {
            sheet.Cell(row, 1).Value = group.Key;
            sheet.Cell(row, 2).Value = group.Count;
            sheet.Cell(row, 3).Value = group.AverageTotalHours;
            sheet.Cell(row, 4).Value = group.AverageRepairHours;
            sheet.Cell(row, 5).Value = group.AverageWaitingHours;
            sheet.Cell(row, 6).Value = group.MaxTotalHours;
            row++;
        }

        row += 2;
        sheet.Cell(row, 1).Value = "Динамика по месяцам";
        sheet.Cell(row, 1).Style.Font.Bold = true;
        sheet.Cell(row, 1).Style.Font.FontSize = 13;

        var monthHeaders = new[] { "Месяц", "Создано", "Завершено", "Средний ремонт, ч", "Сумма, ₽" };
        for (var i = 0; i < monthHeaders.Length; i++)
            sheet.Cell(row + 1, i + 1).Value = monthHeaders[i];
        Header(sheet.Range(row + 1, 1, row + 1, monthHeaders.Length));

        row += 2;
        foreach (var point in data.ByMonth)
        {
            sheet.Cell(row, 1).Value = point.Label;
            sheet.Cell(row, 2).Value = point.Created;
            sheet.Cell(row, 3).Value = point.Completed;
            sheet.Cell(row, 4).Value = point.AverageRepairHours;
            sheet.Cell(row, 5).Value = point.TotalCost;
            sheet.Cell(row, 5).Style.NumberFormat.Format = "#,##0.00";
            row++;
        }

        sheet.Columns().AdjustToContents();
    }

    private static void BuildStatisticsSheet(XLWorkbook workbook, ReportData data)
    {
        var sheet = workbook.Worksheets.Add("Статистика");
        var stats = data.Statistics;

        sheet.Cell(1, 1).Value = "Математическая модель времени ремонта";
        sheet.Cell(1, 1).Style.Font.Bold = true;
        sheet.Cell(1, 1).Style.Font.FontSize = 13;

        var rows = new (string Name, string Value, string Formula)[]
        {
            ("Объём выборки n", stats.Count.ToString(), "число завершённых ремонтов"),
            ("Среднее арифметическое x̄, ч", stats.MeanHours.ToString("0.0000"), "x̄ = Σxᵢ / n"),
            ("Медиана Me, ч", stats.MedianHours.ToString("0.0000"), "Me = x(n/2) при чётном n"),
            ("Мода Mo, ч", stats.ModeHours.ToString("0.0000"), "середина самого частого интервала"),
            ("Выборочная дисперсия s²", stats.Variance.ToString("0.0000"), "s² = Σ(xᵢ − x̄)² / (n − 1)"),
            ("Среднеквадратичное отклонение s, ч", stats.StandardDeviation.ToString("0.0000"), "s = √s²"),
            ("Коэффициент вариации CV", stats.CoefficientOfVariation.ToString("0.0000"), "CV = s / x̄"),
            ("Минимум, ч", stats.MinHours.ToString("0.0000"), "min xᵢ"),
            ("Максимум, ч", stats.MaxHours.ToString("0.0000"), "max xᵢ"),
            ("Интенсивность λ, 1/ч", stats.LambdaPerHour.ToString("0.0000"), "λ = 1 / x̄"),
            ("Медиана экспоненциального закона, ч", stats.ExponentialMedianHours.ToString("0.0000"), "Me = ln(2) / λ"),
            ("90-й процентиль, ч", stats.P90Hours.ToString("0.0000"), "P90 = −ln(0,1) / λ"),
            ("Доверительный интервал, низ, ч", stats.ConfidenceLowHours.ToString("0.0000"), "x̄ − 1,96·s/√n"),
            ("Доверительный интервал, верх, ч", stats.ConfidenceHighHours.ToString("0.0000"), "x̄ + 1,96·s/√n")
        };

        sheet.Cell(3, 1).Value = "Показатель";
        sheet.Cell(3, 2).Value = "Значение";
        sheet.Cell(3, 3).Value = "Формула / пояснение";
        Header(sheet.Range(3, 1, 3, 3));

        var row = 4;
        foreach (var (name, value, formula) in rows)
        {
            sheet.Cell(row, 1).Value = name;
            sheet.Cell(row, 2).Value = value;
            sheet.Cell(row, 3).Value = formula;
            row++;
        }

        sheet.Cell(row + 1, 1).Value = "Экспоненциальная модель: F(t) = 1 − e^(−λt).";
        sheet.Cell(row + 2, 1).Value = "Вероятность завершить ремонт за 8 ч:";
        sheet.Cell(row + 2, 2).Value = stats.ProbabilityLessThan(8).ToString("P1");
        sheet.Cell(row + 3, 1).Value = "Вероятность, что ремонт займёт более 24 ч:";
        sheet.Cell(row + 3, 2).Value = stats.ProbabilityGreaterThan(24).ToString("P1");

        sheet.Columns().AdjustToContents();
    }

    private static void Header(IXLRange range)
    {
        range.Style.Font.Bold = true;
        range.Style.Fill.BackgroundColor = XLColor.FromHtml("#1F3A5F");
        range.Style.Font.FontColor = XLColor.White;
        range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
    }
}
