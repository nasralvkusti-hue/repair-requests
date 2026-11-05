using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using RepairRequests.Core.Models;

namespace RepairRequests.Core.Services;

/// <summary>Формирование печатной формы отчёта в PDF (QuestPDF).</summary>
public static class PdfReportService
{
    private static readonly string Accent = "#1F3A5F";
    private static readonly string Light = "#EEF2F7";

    public static string Export(ReportData data, string filePath)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        // Печатная форма использует Times New Roman (кириллица), поэтому подключаем системные шрифты Windows.
        QuestPDF.Settings.UseSystemFonts = true;

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.8f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Times New Roman").FontColor("#1A1A1A"));

                page.Header().Element(c => Header(c, data));
                page.Content().PaddingVertical(10).Element(c => Content(c, data));
                page.Footer().Row(row =>
                {
                    row.RelativeItem().Text($"Сформировано {data.GeneratedAt:dd.MM.yyyy HH:mm}")
                        .FontSize(8).FontColor(Colors.Grey.Darken1);
                    row.ConstantItem(120).AlignRight().Text(text =>
                    {
                        text.Span("Страница ").FontSize(8).FontColor(Colors.Grey.Darken1);
                        text.CurrentPageNumber().FontSize(8);
                        text.Span(" из ").FontSize(8).FontColor(Colors.Grey.Darken1);
                        text.TotalPages().FontSize(8);
                    });
                });
            });
        }).GeneratePdf(filePath);

        return filePath;
    }

    private static void Header(IContainer container, ReportData data)
    {
        container.Column(column =>
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Column(text =>
                {
                    text.Item().Text("СИСТЕМА ЗАЯВОК НА РЕМОНТ").FontSize(9).FontColor(Accent).Bold();
                    text.Item().Text(data.Title).FontSize(16).Bold().FontColor(Accent);
                    text.Item().Text($"Период: {data.PeriodFrom:dd.MM.yyyy} — {data.PeriodTo:dd.MM.yyyy}")
                        .FontSize(10).FontColor(Colors.Grey.Darken2);
                });
                row.ConstantItem(90).AlignRight().AlignMiddle()
                    .Background(Accent).Padding(8)
                    .Text("ОТЧЁТ").FontColor(Colors.White).Bold().FontSize(11);
            });
            column.Item().PaddingTop(6).LineHorizontal(1).LineColor(Accent);
        });
    }

    private static void Content(IContainer container, ReportData data)
    {
        container.Column(column =>
        {
            column.Spacing(10);

            column.Item().Text("1. Сводные показатели").FontSize(13).Bold().FontColor(Accent);
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(2);
                });

                var cells = new (string Label, string Value)[]
                {
                    ("Всего заявок", data.TotalRequests.ToString()),
                    ("Выполнено и закрыто", data.CompletedRequests.ToString()),
                    ("В работе / ожидают", data.OpenRequests.ToString()),
                    ("Отменено", data.CancelledRequests.ToString()),
                    ("Просрочено", data.OverdueRequests.ToString()),
                    ("Доля выполнения", $"{data.CompletionRate:0.0} %"),
                    ("Среднее время ремонта", $"{data.Statistics.MeanHours:0.00} ч"),
                    ("Средний простой", $"{data.AverageDowntimeHours:0.00} ч"),
                    ("Среднее ожидание", $"{data.AverageWaitingHours:0.00} ч"),
                    ("Суммарная стоимость", $"{data.TotalCost:N2} ₽")
                };

                for (var i = 0; i < cells.Length; i += 2)
                {
                    Pair(table, cells[i].Label, cells[i].Value);
                    Pair(table, cells[i + 1].Label, cells[i + 1].Value);
                }
            });

            column.Item().Text("2. Распределение заявок").FontSize(13).Bold().FontColor(Accent);
            column.Item().Row(row =>
            {
                row.RelativeItem().Element(c => DistributionTable(c, "По статусам", data.ByStatus));
                row.ConstantItem(12);
                row.RelativeItem().Element(c => DistributionTable(c, "По приоритетам", data.ByPriority));
            });

            column.Item().Text("3. Математическая модель времени ремонта").FontSize(13).Bold().FontColor(Accent);
            column.Item().Text(
                    "Длительность ремонта описана экспоненциальным распределением с интенсивностью λ = 1 / x̄. " +
                    "Функция распределения F(t) = 1 − e^(−λt) показывает вероятность завершения ремонта за t часов.")
                .FontSize(9).Italic().FontColor(Colors.Grey.Darken2);

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(4);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(4);
                });

                table.Header(header =>
                {
                    header.Cell().Element(HeaderCell).Text("Показатель");
                    header.Cell().Element(HeaderCell).Text("Значение");
                    header.Cell().Element(HeaderCell).Text("Формула");
                });

                var s = data.Statistics;
                var rows = new (string Name, string Value, string Formula)[]
                {
                    ("Объём выборки n", s.Count.ToString(), "завершённые ремонты"),
                    ("Среднее x̄, ч", s.MeanHours.ToString("0.000"), "Σxᵢ / n"),
                    ("Медиана Me, ч", s.MedianHours.ToString("0.000"), "середина вариационного ряда"),
                    ("Мода Mo, ч", s.ModeHours.ToString("0.000"), "частый часовой интервал"),
                    ("Дисперсия s²", s.Variance.ToString("0.000"), "Σ(xᵢ − x̄)² / (n − 1)"),
                    ("Отклонение s, ч", s.StandardDeviation.ToString("0.000"), "√s²"),
                    ("Коэффициент вариации CV", s.CoefficientOfVariation.ToString("0.000"), "s / x̄"),
                    ("Интенсивность λ, 1/ч", s.LambdaPerHour.ToString("0.0000"), "1 / x̄"),
                    ("Медиана экспоненциального закона, ч", s.ExponentialMedianHours.ToString("0.000"), "ln 2 / λ"),
                    ("90-й процентиль, ч", s.P90Hours.ToString("0.000"), "−ln(0,1) / λ"),
                    ("Доверительный интервал 95 %, ч", $"{s.ConfidenceLowHours:0.000} — {s.ConfidenceHighHours:0.000}", "x̄ ± 1,96·s/√n")
                };

                foreach (var (name, value, formula) in rows)
                {
                    table.Cell().Element(BodyCell).Text(name);
                    table.Cell().Element(BodyCell).AlignRight().Text(value);
                    table.Cell().Element(BodyCell).Text(formula).FontSize(8).FontColor(Colors.Grey.Darken2);
                }
            });

            column.Item().Row(row =>
            {
                row.RelativeItem().Border(1).BorderColor("#D6DEE8").Background(Light).Padding(8).Column(c =>
                {
                    c.Item().Text("Вероятность завершить ремонт за 8 ч").FontSize(8).FontColor(Colors.Grey.Darken2);
                    c.Item().Text(data.Statistics.ProbabilityLessThan(8).ToString("P1")).FontSize(14).Bold().FontColor(Accent);
                });
                row.ConstantItem(10);
                row.RelativeItem().Border(1).BorderColor("#D6DEE8").Background(Light).Padding(8).Column(c =>
                {
                    c.Item().Text("Вероятность, что ремонт займёт более 24 ч").FontSize(8).FontColor(Colors.Grey.Darken2);
                    c.Item().Text(data.Statistics.ProbabilityGreaterThan(24).ToString("P1")).FontSize(14).Bold().FontColor(Accent);
                });
            });

            column.Item().Text("4. Простои по категориям неисправностей").FontSize(13).Bold().FontColor(Accent);
            column.Item().Element(c => DowntimeTable(c, data.ByCategory, "Категория"));

            column.Item().Text("5. Простои по транспортным средствам").FontSize(13).Bold().FontColor(Accent);
            column.Item().Element(c => DowntimeTable(c, data.ByVehicle, "Транспортное средство"));

            column.Item().Text("6. Динамика по месяцам").FontSize(13).Bold().FontColor(Accent);
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                table.Header(header =>
                {
                    header.Cell().Element(HeaderCell).Text("Месяц");
                    header.Cell().Element(HeaderCell).Text("Создано");
                    header.Cell().Element(HeaderCell).Text("Завершено");
                    header.Cell().Element(HeaderCell).Text("Средний ремонт, ч");
                    header.Cell().Element(HeaderCell).Text("Сумма, ₽");
                });

                foreach (var point in data.ByMonth)
                {
                    table.Cell().Element(BodyCell).Text(point.Label);
                    table.Cell().Element(BodyCell).AlignRight().Text(point.Created.ToString());
                    table.Cell().Element(BodyCell).AlignRight().Text(point.Completed.ToString());
                    table.Cell().Element(BodyCell).AlignRight().Text(point.AverageRepairHours.ToString("0.00"));
                    table.Cell().Element(BodyCell).AlignRight().Text(point.TotalCost.ToString("N0"));
                }
            });

            column.Item().Text("7. Реестр заявок").FontSize(13).Bold().FontColor(Accent);
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(4);
                    columns.RelativeColumn(4);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(2);
                });

                table.Header(header =>
                {
                    header.Cell().Element(HeaderCell).Text("Номер");
                    header.Cell().Element(HeaderCell).Text("Создана");
                    header.Cell().Element(HeaderCell).Text("ТС / клиент");
                    header.Cell().Element(HeaderCell).Text("Неисправность");
                    header.Cell().Element(HeaderCell).Text("Статус");
                    header.Cell().Element(HeaderCell).Text("Сумма, ₽");
                });

                foreach (var request in data.Requests.OrderBy(r => r.CreatedAt))
                {
                    table.Cell().Element(BodyCell).Text(request.Number).FontSize(8);
                    table.Cell().Element(BodyCell).Text(request.CreatedAt.ToString("dd.MM.yyyy")).FontSize(8);
                    table.Cell().Element(BodyCell).Column(c =>
                    {
                        c.Item().Text(request.Vehicle?.DisplayName ?? "—").FontSize(8);
                        c.Item().Text(request.Client?.FullName ?? "—").FontSize(7).FontColor(Colors.Grey.Darken2);
                    });
                    table.Cell().Element(BodyCell).Text(request.FaultType?.Name ?? "—").FontSize(8);
                    table.Cell().Element(BodyCell).Text(request.Status.Ru()).FontSize(8);
                    table.Cell().Element(BodyCell).AlignRight().Text(request.TotalCost.ToString("N0")).FontSize(8);
                }
            });

            if (!string.IsNullOrWhiteSpace(data.GeneratedBy))
            {
                column.Item().PaddingTop(6).AlignRight()
                    .Text($"Ответственный: {data.GeneratedBy}").FontSize(9).Italic();
            }
        });
    }

    private static void Pair(TableDescriptor table, string label, string value)
    {
        table.Cell().Element(BodyCell).Text(label).FontSize(9);
        table.Cell().Element(BodyCell).AlignRight().Text(value).Bold().FontSize(9);
    }

    private static void DistributionTable(IContainer container, string title, IReadOnlyList<DistributionItem> items)
    {
        container.Column(column =>
        {
            column.Item().PaddingBottom(2).Text(title).FontSize(9).Bold().FontColor(Accent);
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3);
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                table.Header(header =>
                {
                    header.Cell().Element(HeaderCell).Text("Значение");
                    header.Cell().Element(HeaderCell).Text("Кол-во");
                    header.Cell().Element(HeaderCell).Text("Доля");
                });

                foreach (var item in items)
                {
                    table.Cell().Element(BodyCell).Text(item.Key).FontSize(8);
                    table.Cell().Element(BodyCell).AlignRight().Text(item.Count.ToString()).FontSize(8);
                    table.Cell().Element(BodyCell).AlignRight().Text($"{item.Share:0.0} %").FontSize(8);
                }
            });
        });
    }

    private static void DowntimeTable(IContainer container, IReadOnlyList<DowntimeGroup> groups, string firstColumn)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(4);
                columns.RelativeColumn();
                columns.RelativeColumn();
                columns.RelativeColumn();
                columns.RelativeColumn();
                columns.RelativeColumn();
            });

            table.Header(header =>
            {
                header.Cell().Element(HeaderCell).Text(firstColumn);
                header.Cell().Element(HeaderCell).Text("Заявок");
                header.Cell().Element(HeaderCell).Text("Простой, ч");
                header.Cell().Element(HeaderCell).Text("Ремонт, ч");
                header.Cell().Element(HeaderCell).Text("Ожидание, ч");
                header.Cell().Element(HeaderCell).Text("Макс., ч");
            });

            foreach (var group in groups)
            {
                table.Cell().Element(BodyCell).Text(group.Key).FontSize(8);
                table.Cell().Element(BodyCell).AlignRight().Text(group.Count.ToString()).FontSize(8);
                table.Cell().Element(BodyCell).AlignRight().Text(group.AverageTotalHours.ToString("0.00")).FontSize(8);
                table.Cell().Element(BodyCell).AlignRight().Text(group.AverageRepairHours.ToString("0.00")).FontSize(8);
                table.Cell().Element(BodyCell).AlignRight().Text(group.AverageWaitingHours.ToString("0.00")).FontSize(8);
                table.Cell().Element(BodyCell).AlignRight().Text(group.MaxTotalHours.ToString("0.00")).FontSize(8);
            }
        });
    }

    private static IContainer HeaderCell(IContainer container) =>
        container.Background(Accent).Padding(4).DefaultTextStyle(x => x.FontColor(Colors.White).Bold().FontSize(8));

    private static IContainer BodyCell(IContainer container) =>
        container.BorderBottom(0.5f).BorderColor("#C9D4E2").Padding(4).DefaultTextStyle(x => x.FontSize(9));
}
