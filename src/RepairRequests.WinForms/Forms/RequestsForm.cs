using RepairRequests.Core.Models;
using RepairRequests.Core.Repositories;
using RepairRequests.WinForms.Infrastructure;

namespace RepairRequests.WinForms.Forms;

/// <summary>Реестр заявок с фильтрацией и операциями CRUD (аналог Form3 в PcClab).</summary>
public class RequestsForm : Form
{
    private readonly DataGridView _grid;
    private readonly TextBox _search;
    private readonly ComboBox _status;
    private readonly Label _count;
    private readonly Button _editButton;
    private readonly Button _deleteButton;
    private List<RequestRow> _rows = new();

    public RequestsForm()
    {
        Text = AppServices.IsManager ? "Заявки на ремонт" : "Мои заявки на ремонт";
        ClientSize = new Size(1140, 690);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = UiTheme.Background;
        ForeColor = UiTheme.Text;
        Font = UiTheme.BaseFont;

        Controls.Add(UiTheme.Title(AppServices.IsManager ? "Заявки на ремонт" : "Мои заявки на ремонт", 20, 12));
        Controls.Add(UiTheme.MutedLabel(
            AppServices.IsManager
                ? "Реестр заявок: поиск, фильтрация, изменение статуса и стоимости работ."
                : "Здесь отображаются только ваши заявки. Новую заявку можно создать кнопкой «Добавить».",
            20, 52, 900));

        Controls.Add(UiTheme.Label("Поиск", 20, 82, bold: true, size: 10F));
        _search = UiTheme.TextBox(20, 106, 300);
        _search.PlaceholderText = "номер, автомобиль, клиент, неисправность";
        _search.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                _ = LoadDataAsync();
            }
        };
        Controls.Add(_search);

        Controls.Add(UiTheme.Label("Статус", 336, 82, bold: true, size: 10F));
        _status = UiTheme.Combo(336, 106, 180);
        _status.Items.Add("Все статусы");
        foreach (var status in Enum.GetValues<RequestStatus>())
            _status.Items.Add(status.Ru());
        _status.SelectedIndex = 0;
        Controls.Add(_status);

        var find = UiTheme.Button("Найти", UiTheme.Green, 532, 101, 110, 36, 10F);
        UiTheme.Flatten(find);
        find.Click += async (_, _) => await LoadDataAsync();
        Controls.Add(find);

        var reset = UiTheme.Button("Сброс", UiTheme.Neutral, 650, 101, 110, 36, 10F);
        UiTheme.Flatten(reset);
        reset.Click += async (_, _) =>
        {
            _search.Clear();
            _status.SelectedIndex = 0;
            await LoadDataAsync();
        };
        Controls.Add(reset);

        _count = UiTheme.MutedLabel("Загрузка…", 780, 108, 340);
        _count.TextAlign = ContentAlignment.MiddleRight;
        Controls.Add(_count);

        _grid = UiTheme.Grid(20, 150, 1100, 450);
        _grid.SelectionChanged += (_, _) => UpdateButtons();
        _grid.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0)
                EditSelected();
        };
        _grid.CellFormatting += Grid_CellFormatting;
        Controls.Add(_grid);

        var add = UiTheme.Button("Добавить", UiTheme.Green, 20, 620, 140, 46);
        UiTheme.Flatten(add);
        add.Click += (_, _) => AddRequest();
        Controls.Add(add);

        _editButton = UiTheme.Button("Изменить", UiTheme.Neutral, 172, 620, 140, 46);
        UiTheme.Flatten(_editButton);
        _editButton.Click += (_, _) => EditSelected();
        Controls.Add(_editButton);

        _deleteButton = UiTheme.Button("Удалить", UiTheme.Red, 324, 620, 140, 46);
        UiTheme.Flatten(_deleteButton);
        _deleteButton.Click += async (_, _) => await DeleteSelectedAsync();
        _deleteButton.Visible = AppServices.IsManager;
        Controls.Add(_deleteButton);

        var refresh = UiTheme.Button("Обновить", UiTheme.Neutral, 476, 620, 140, 46);
        UiTheme.Flatten(refresh);
        refresh.Click += async (_, _) => await LoadDataAsync();
        Controls.Add(refresh);

        var back = UiTheme.Button("Вернуться", UiTheme.Red, 930, 620, 190, 46);
        UiTheme.Flatten(back);
        back.Click += (_, _) => Close();
        Controls.Add(back);

        Shown += async (_, _) => await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        try
        {
            UseWaitCursor = true;
            _count.Text = "Загрузка…";

            var filter = new RequestFilter
            {
                Search = string.IsNullOrWhiteSpace(_search.Text) ? null : _search.Text.Trim(),
                Status = _status.SelectedIndex > 0 ? (RequestStatus)_status.SelectedIndex : null,
                ClientId = AppServices.ClientScope,
                Page = 1,
                PageSize = 500,
                SortBy = "date_desc"
            };

            var page = await AppServices.Requests.GetPagedAsync(filter);

            _rows = page.Items.Select(r => new RequestRow
            {
                Id = r.Id,
                Number = r.Number,
                CreatedAt = r.CreatedAt,
                Vehicle = r.Vehicle?.DisplayName ?? "—",
                Client = r.Client?.FullName ?? "—",
                Fault = r.FaultType?.Name ?? "—",
                Mechanic = r.AssignedMechanic?.FullName ?? "не назначен",
                Priority = r.Priority.Ru(),
                Status = r.Status.Ru(),
                RepairHours = r.RepairHours.HasValue ? r.RepairHours.Value.ToString("0.0") : "—",
                TotalCost = r.TotalCost.ToString("N2")
            }).ToList();

            _grid.DataSource = _rows;
            ConfigureColumns();
            _count.Text = $"Показано {_rows.Count} из {page.TotalCount}";

            UiTheme.SelectFirstRow(_grid);
        }
        catch (Exception ex)
        {
            UiTheme.Error("Не удалось загрузить заявки:\n" + ex.Message);
        }
        finally
        {
            UseWaitCursor = false;
            UpdateButtons();
        }
    }

    private void ConfigureColumns()
    {
        if (_grid.Columns.Count == 0)
            return;

        _grid.Columns["Id"]!.Visible = false;
        _grid.Columns["Number"]!.HeaderText = "Номер";
        _grid.Columns["Number"]!.FillWeight = 70;
        _grid.Columns["CreatedAt"]!.HeaderText = "Создана";
        _grid.Columns["CreatedAt"]!.DefaultCellStyle.Format = "dd.MM.yyyy";
        _grid.Columns["CreatedAt"]!.FillWeight = 70;
        _grid.Columns["Vehicle"]!.HeaderText = "Автомобиль";
        _grid.Columns["Vehicle"]!.FillWeight = 140;
        _grid.Columns["Client"]!.HeaderText = "Клиент";
        _grid.Columns["Client"]!.FillWeight = 120;
        _grid.Columns["Fault"]!.HeaderText = "Неисправность";
        _grid.Columns["Fault"]!.FillWeight = 150;
        _grid.Columns["Mechanic"]!.HeaderText = "Механик";
        _grid.Columns["Mechanic"]!.FillWeight = 110;
        _grid.Columns["Priority"]!.HeaderText = "Приоритет";
        _grid.Columns["Priority"]!.FillWeight = 80;
        _grid.Columns["Status"]!.HeaderText = "Статус";
        _grid.Columns["Status"]!.FillWeight = 80;
        _grid.Columns["RepairHours"]!.HeaderText = "Ремонт, ч";
        _grid.Columns["RepairHours"]!.FillWeight = 65;
        _grid.Columns["TotalCost"]!.HeaderText = "Стоимость, ₽";
        _grid.Columns["TotalCost"]!.FillWeight = 85;
        _grid.Columns["TotalCost"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        _grid.Columns["RepairHours"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
    }

    private void Grid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || _grid.Columns[e.ColumnIndex].Name != "Status")
            return;

        var status = e.Value?.ToString() ?? string.Empty;
        e.CellStyle!.ForeColor = UiTheme.StatusColor(status);
        e.CellStyle.Font = new Font("Microsoft Sans Serif", 9.5F, FontStyle.Bold);
    }

    private RequestRow? SelectedRow =>
        _grid.CurrentRow?.DataBoundItem as RequestRow;

    private void UpdateButtons()
    {
        var hasSelection = SelectedRow is not null;
        _editButton.Enabled = hasSelection;
        _deleteButton.Enabled = hasSelection && AppServices.IsManager;
    }

    private void AddRequest()
    {
        using var form = new RequestEditForm(null);
        if (form.ShowDialog(this) == DialogResult.OK)
            _ = LoadDataAsync();
    }

    private void EditSelected()
    {
        var row = SelectedRow;
        if (row is null)
            return;

        using var form = new RequestEditForm(row.Id);
        if (form.ShowDialog(this) == DialogResult.OK)
            _ = LoadDataAsync();
    }

    private async Task DeleteSelectedAsync()
    {
        var row = SelectedRow;
        if (row is null)
            return;

        if (!AppServices.IsManager)
        {
            UiTheme.Error("Удалять заявки может только менеджер или администратор.");
            return;
        }

        if (!UiTheme.Confirm($"Удалить заявку {row.Number}?\nДействие нельзя отменить."))
            return;

        try
        {
            var request = await AppServices.Requests.GetByIdAsync(row.Id);
            if (request is null)
                return;

            await AppServices.Requests.DeleteAsync(request);
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            UiTheme.Error("Не удалось удалить заявку:\n" + ex.Message);
        }
    }

    private sealed class RequestRow
    {
        public int Id { get; init; }
        public string Number { get; init; } = string.Empty;
        public DateTime CreatedAt { get; init; }
        public string Vehicle { get; init; } = string.Empty;
        public string Client { get; init; } = string.Empty;
        public string Fault { get; init; } = string.Empty;
        public string Mechanic { get; init; } = string.Empty;
        public string Priority { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public string RepairHours { get; init; } = string.Empty;
        public string TotalCost { get; init; } = string.Empty;
    }
}
