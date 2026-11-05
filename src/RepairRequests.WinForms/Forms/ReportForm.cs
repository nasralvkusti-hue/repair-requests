using System.Diagnostics;
using RepairRequests.Core.Services;
using RepairRequests.WinForms.Infrastructure;

namespace RepairRequests.WinForms.Forms;

/// <summary>Отчёт по простоям и времени ремонта (аналог Form4 в PcClab).</summary>
public class ReportForm : Form
{
    private readonly DateTimePicker _from;
    private readonly DateTimePicker _to;
    private readonly Label _kpiTotal;
    private readonly Label _kpiDone;
    private readonly Label _kpiOpen;
    private readonly Label _kpiAvg;
    private readonly Label _kpiDowntime;
    private readonly Label _kpiMedian;
    private readonly Label _kpiStd;
    private readonly Label _kpiSum;
    private readonly Label _kpiModel;
    private readonly DataGridView _categoryGrid;
    private readonly DataGridView _vehicleGrid;
    private readonly DataGridView _monthGrid;
    private readonly Label _status;

    private ReportData? _report;

    public ReportForm()
    {
        Text = "Отчёт по заявкам на ремонт";
        ClientSize = new Size(1150, 740);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = UiTheme.Background;
        ForeColor = UiTheme.Text;
        Font = UiTheme.BaseFont;

        Controls.Add(UiTheme.Title(AppServices.IsManager ? "Отчёт по заявкам" : "Мой отчёт по заявкам", 20, 12));
        Controls.Add(UiTheme.MutedLabel(
            "Простои по категориям, автомобилям и месяцам; статистика времени ремонта. " +
            "Отчёт можно выгрузить в Excel или PDF.",
            20, 52, 1020));

        Controls.Add(UiTheme.Label("Период с", 20, 86, bold: true, size: 10F));
        _from = UiTheme.Date(20, 110, 150);
        _from.Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-5);
        Controls.Add(_from);

        Controls.Add(UiTheme.Label("по", 184, 113, bold: true, size: 10F));
        _to = UiTheme.Date(212, 110, 150);
        _to.Value = DateTime.Today;
        Controls.Add(_to);

        var build = UiTheme.Button("Сформировать", UiTheme.Green, 386, 104, 160, 38, 10F);
        UiTheme.Flatten(build);
        build.Click += async (_, _) => await BuildReportAsync();
        Controls.Add(build);

        _status = UiTheme.MutedLabel("Отчёт ещё не сформирован", 566, 111, 550);
        _status.TextAlign = ContentAlignment.MiddleRight;
        Controls.Add(_status);

        _kpiTotal = Kpi("Всего заявок: —", 20, 160);
        _kpiDone = Kpi("Выполнено: —", 300, 160);
        _kpiOpen = Kpi("Активных: —", 580, 160);
        _kpiAvg = Kpi("Среднее время ремонта: —", 840, 160);

        _kpiDowntime = Kpi("Средний простой: —", 20, 196);
        _kpiMedian = Kpi("Медиана ремонта: —", 300, 196);
        _kpiStd = Kpi("Отклонение: —", 580, 196);
        _kpiSum = Kpi("Сумма: —", 840, 196);

        Controls.AddRange(new Control[]
        {
            _kpiTotal, _kpiDone, _kpiOpen, _kpiAvg,
            _kpiDowntime, _kpiMedian, _kpiStd, _kpiSum
        });

        _kpiModel = new Label
        {
            Location = new Point(20, 230),
            Size = new Size(1100, 46),
            Font = new Font("Microsoft Sans Serif", 9F),
            ForeColor = UiTheme.Muted,
            Text = "Математическая модель: λ = 1/x̄; F(t) = 1 − e^(−λt)."
        };
        Controls.Add(_kpiModel);

        var tabs = new TabControl
        {
            Location = new Point(20, 280),
            Size = new Size(1100, 340),
            Font = new Font("Microsoft Sans Serif", 10F)
        };

        _categoryGrid = CreateGrid();
        _vehicleGrid = CreateGrid();
        _monthGrid = CreateGrid();

        tabs.TabPages.Add(MakeTab("По категориям", _categoryGrid));
        tabs.TabPages.Add(MakeTab("По автомобилям", _vehicleGrid));
        tabs.TabPages.Add(MakeTab("По месяцам", _monthGrid));
        Controls.Add(tabs);

        var excel = UiTheme.Button("Экспорт в Excel", UiTheme.Green, 20, 650, 210, 50);
        UiTheme.Flatten(excel);
        excel.Click += (_, _) => ExportExcel();
        Controls.Add(excel);

        var pdf = UiTheme.Button("Экспорт в PDF", UiTheme.Neutral, 246, 650, 190, 50);
        UiTheme.Flatten(pdf);
        pdf.Click += (_, _) => ExportPdf();
        Controls.Add(pdf);

        var back = UiTheme.Button("Вернуться", UiTheme.Red, 940, 650, 180, 50);
        UiTheme.Flatten(back);
        back.Click += (_, _) => Close();
        Controls.Add(back);

        Shown += async (_, _) => await BuildReportAsync();
    }

    private static Label Kpi(string text, int x, int y) => new()
    {
        Text = text,
        Location = new Point(x, y),
        Size = new Size(270, 30),
        Font = new Font("Microsoft Sans Serif", 11F, FontStyle.Bold),
        ForeColor = UiTheme.Text
    };

    private static DataGridView CreateGrid()
    {
        var grid = UiTheme.Grid(0, 0, 100, 100);
        grid.Dock = DockStyle.Fill;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        return grid;
    }

    private static TabPage MakeTab(string title, DataGridView grid)
    {
        var page = new TabPage(title)
        {
            BackColor = Color.White,
            Padding = new Padding(6)
        };
        page.Controls.Add(grid);
        return page;
    }

    private async Task BuildReportAsync()
    {
        if (_from.Value.Date > _to.Value.Date)
        {
            UiTheme.Error("Начало периода не может быть позже окончания.");
            return;
        }

        try
        {
            UseWaitCursor = true;
            _status.Text = "Формирование отчёта…";

            var requests = await AppServices.Requests.GetForPeriodAsync(
                _from.Value.Date, _to.Value.Date, AppServices.ClientScope);

            _report = AnalyticsService.Build(
                requests,
                _from.Value.Date,
                _to.Value.Date,
                AppServices.CurrentUser?.FullName,
                AppServices.IsManager ? "Отчёт по заявкам на ремонт" : "Отчёт по моим заявкам на ремонт");

            var s = _report.Statistics;
            _kpiTotal.Text = $"Всего заявок: {_report.TotalRequests}";
            _kpiDone.Text = $"Выполнено: {_report.CompletedRequests} ({_report.CompletionRate:0.0} %)";
            _kpiOpen.Text = $"Активных: {_report.OpenRequests}";
            _kpiAvg.Text = $"Среднее время ремонта: {s.MeanHours:0.00} ч";
            _kpiDowntime.Text = $"Средний простой: {_report.AverageDowntimeHours:0.00} ч";
            _kpiMedian.Text = $"Медиана ремонта: {s.MedianHours:0.00} ч";
            _kpiStd.Text = $"Отклонение s: {s.StandardDeviation:0.00} ч";
            _kpiSum.Text = $"Сумма: {_report.TotalCost:N2} ₽";

            _kpiModel.Text =
                $"Математическая модель: λ = {s.LambdaPerHour:0.0000} 1/ч; CV = {s.CoefficientOfVariation:0.00}; " +
                $"P90 = {s.P90Hours:0.00} ч\n" +
                $"95 % ДИ среднего = [{s.ConfidenceLowHours:0.00}; {s.ConfidenceHighHours:0.00}] ч; " +
                $"P(T ≤ 8 ч) = {s.ProbabilityLessThan(8):P0}.";

            BindCategories();
            BindVehicles();
            BindMonths();

            _status.Text = $"Период: {_report.PeriodFrom:dd.MM.yyyy} — {_report.PeriodTo:dd.MM.yyyy} • заявок: {_report.TotalRequests}";
        }
        catch (Exception ex)
        {
            _status.Text = "Ошибка формирования отчёта";
            UiTheme.Error("Не удалось сформировать отчёт:\n" + ex.Message);
        }
        finally
        {
            UseWaitCursor = false;
        }
    }

    private void BindCategories()
    {
        var rows = _report!.ByCategory.Select(g => new DowntimeRow
        {
            Name = g.Key,
            Count = g.Count,
            AverageTotal = g.AverageTotalHours,
            AverageRepair = g.AverageRepairHours,
            AverageWaiting = g.AverageWaitingHours,
            MaxTotal = g.MaxTotalHours
        }).ToList();

        _categoryGrid.DataSource = rows;
        ConfigureDowntimeGrid(_categoryGrid, "Категория неисправности");
    }

    private void BindVehicles()
    {
        var rows = _report!.ByVehicle.Select(g => new DowntimeRow
        {
            Name = g.Key,
            Count = g.Count,
            AverageTotal = g.AverageTotalHours,
            AverageRepair = g.AverageRepairHours,
            AverageWaiting = g.AverageWaitingHours,
            MaxTotal = g.MaxTotalHours
        }).ToList();

        _vehicleGrid.DataSource = rows;
        ConfigureDowntimeGrid(_vehicleGrid, "Транспортное средство");
    }

    private void BindMonths()
    {
        var rows = _report!.ByMonth.Select(p => new MonthRow
        {
            Period = p.Label,
            Created = p.Created,
            Completed = p.Completed,
            AverageRepair = p.AverageRepairHours,
            TotalCost = p.TotalCost
        }).ToList();

        _monthGrid.DataSource = rows;
        if (_monthGrid.Columns.Count == 0)
            return;

        _monthGrid.Columns["Period"]!.HeaderText = "Месяц";
        _monthGrid.Columns["Created"]!.HeaderText = "Создано";
        _monthGrid.Columns["Completed"]!.HeaderText = "Завершено";
        _monthGrid.Columns["AverageRepair"]!.HeaderText = "Средний ремонт, ч";
        _monthGrid.Columns["AverageRepair"]!.DefaultCellStyle.Format = "0.00";
        _monthGrid.Columns["TotalCost"]!.HeaderText = "Сумма, ₽";
        _monthGrid.Columns["TotalCost"]!.DefaultCellStyle.Format = "N2";
    }

    private static void ConfigureDowntimeGrid(DataGridView grid, string nameHeader)
    {
        if (grid.Columns.Count == 0)
            return;

        grid.Columns["Name"]!.HeaderText = nameHeader;
        grid.Columns["Name"]!.FillWeight = 220;
        grid.Columns["Count"]!.HeaderText = "Заявок";
        grid.Columns["Count"]!.FillWeight = 70;
        grid.Columns["AverageTotal"]!.HeaderText = "Средний простой, ч";
        grid.Columns["AverageTotal"]!.DefaultCellStyle.Format = "0.00";
        grid.Columns["AverageRepair"]!.HeaderText = "Средний ремонт, ч";
        grid.Columns["AverageRepair"]!.DefaultCellStyle.Format = "0.00";
        grid.Columns["AverageWaiting"]!.HeaderText = "Среднее ожидание, ч";
        grid.Columns["AverageWaiting"]!.DefaultCellStyle.Format = "0.00";
        grid.Columns["MaxTotal"]!.HeaderText = "Макс. простой, ч";
        grid.Columns["MaxTotal"]!.DefaultCellStyle.Format = "0.00";
    }

    private void ExportExcel()
    {
        if (_report is null)
        {
            UiTheme.Error("Сначала сформируйте отчёт.");
            return;
        }

        using var dialog = new SaveFileDialog
        {
            Title = "Сохранить отчёт в Excel",
            Filter = "Книга Excel (*.xlsx)|*.xlsx",
            InitialDirectory = AppPaths.ReportsDirectory,
            FileName = $"Отчёт_по_заявкам_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            ExcelExportService.Export(_report, dialog.FileName);
            OfferToOpen(dialog.FileName, "Отчёт сохранён в Excel.");
        }
        catch (Exception ex)
        {
            UiTheme.Error("Не удалось сохранить файл Excel:\n" + ex.Message);
        }
    }

    private void ExportPdf()
    {
        if (_report is null)
        {
            UiTheme.Error("Сначала сформируйте отчёт.");
            return;
        }

        using var dialog = new SaveFileDialog
        {
            Title = "Сохранить отчёт в PDF",
            Filter = "Документ PDF (*.pdf)|*.pdf",
            InitialDirectory = AppPaths.ReportsDirectory,
            FileName = $"Отчёт_по_заявкам_{DateTime.Now:yyyyMMdd_HHmm}.pdf"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            PdfReportService.Export(_report, dialog.FileName);
            OfferToOpen(dialog.FileName, "Отчёт сохранён в PDF.");
        }
        catch (Exception ex)
        {
            UiTheme.Error("Не удалось сохранить файл PDF:\n" + ex.Message);
        }
    }

    private void OfferToOpen(string path, string message)
    {
        var result = MessageBox.Show(message + "\n\nОткрыть файл?", "Готово",
            MessageBoxButtons.YesNo, MessageBoxIcon.Information);

        if (result != DialogResult.Yes)
            return;

        try
        {
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            UiTheme.Error("Не удалось открыть файл:\n" + ex.Message);
        }
    }

    private sealed class DowntimeRow
    {
        public string Name { get; init; } = string.Empty;
        public int Count { get; init; }
        public double AverageTotal { get; init; }
        public double AverageRepair { get; init; }
        public double AverageWaiting { get; init; }
        public double MaxTotal { get; init; }
    }

    private sealed class MonthRow
    {
        public string Period { get; init; } = string.Empty;
        public int Created { get; init; }
        public int Completed { get; init; }
        public double AverageRepair { get; init; }
        public decimal TotalCost { get; init; }
    }
}
