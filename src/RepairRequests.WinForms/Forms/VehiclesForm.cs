using RepairRequests.Core.Models;
using RepairRequests.Core.Repositories;
using RepairRequests.WinForms.Infrastructure;

namespace RepairRequests.WinForms.Forms;

/// <summary>Справочник транспортных средств клиента (модуль А).</summary>
public class VehiclesForm : Form
{
    private readonly DataGridView _grid;
    private readonly TextBox _search;
    private readonly Label _count;
    private readonly Button _editButton;
    private readonly Button _deleteButton;
    private List<VehicleRow> _rows = new();

    public VehiclesForm()
    {
        Text = AppServices.IsManager ? "Справочник автомобилей" : "Мой транспорт";
        ClientSize = new Size(1000, 600);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = UiTheme.Background;
        ForeColor = UiTheme.Text;
        Font = UiTheme.BaseFont;

        Controls.Add(UiTheme.Title(AppServices.IsManager ? "Автомобили" : "Мой транспорт", 20, 12));
        Controls.Add(UiTheme.MutedLabel(
            AppServices.IsManager
                ? "Все транспортные средства клиентов сервиса."
                : "Автомобили, привязанные к вашей учётной записи.",
            20, 52, 900));

        Controls.Add(UiTheme.Label("Поиск", 20, 82, bold: true, size: 10F));
        _search = UiTheme.TextBox(20, 106, 340);
        _search.PlaceholderText = "марка, модель, номер, владелец";
        _search.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                _ = LoadDataAsync();
            }
        };
        Controls.Add(_search);

        var find = UiTheme.Button("Найти", UiTheme.Green, 376, 101, 110, 36, 10F);
        UiTheme.Flatten(find);
        find.Click += async (_, _) => await LoadDataAsync();
        Controls.Add(find);

        var reset = UiTheme.Button("Сброс", UiTheme.Neutral, 494, 101, 110, 36, 10F);
        UiTheme.Flatten(reset);
        reset.Click += async (_, _) => { _search.Clear(); await LoadDataAsync(); };
        Controls.Add(reset);

        _count = UiTheme.MutedLabel("Загрузка…", 640, 108, 340);
        _count.TextAlign = ContentAlignment.MiddleRight;
        Controls.Add(_count);

        _grid = UiTheme.Grid(20, 150, 960, 360);
        _grid.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0)
                EditSelected();
        };
        _grid.SelectionChanged += (_, _) => UpdateButtons();
        Controls.Add(_grid);

        var add = UiTheme.Button("Добавить", UiTheme.Green, 20, 530, 140, 46);
        UiTheme.Flatten(add);
        add.Click += (_, _) => AddVehicle();
        Controls.Add(add);

        _editButton = UiTheme.Button("Изменить", UiTheme.Neutral, 172, 530, 140, 46);
        UiTheme.Flatten(_editButton);
        _editButton.Click += (_, _) => EditSelected();
        Controls.Add(_editButton);

        _deleteButton = UiTheme.Button("Удалить", UiTheme.Red, 324, 530, 140, 46);
        UiTheme.Flatten(_deleteButton);
        _deleteButton.Click += async (_, _) => await DeleteSelectedAsync();
        Controls.Add(_deleteButton);

        var refresh = UiTheme.Button("Обновить", UiTheme.Neutral, 476, 530, 140, 46);
        UiTheme.Flatten(refresh);
        refresh.Click += async (_, _) => await LoadDataAsync();
        Controls.Add(refresh);

        var back = UiTheme.Button("Вернуться", UiTheme.Red, 830, 530, 150, 46);
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
            var filter = new VehicleFilter
            {
                Search = string.IsNullOrWhiteSpace(_search.Text) ? null : _search.Text.Trim(),
                OwnerId = AppServices.ClientScope,
                Page = 1,
                PageSize = 500
            };

            var page = await AppServices.Vehicles.GetPagedAsync(filter);
            _rows = page.Items.Select(v => new VehicleRow
            {
                Id = v.Id,
                Brand = v.Brand,
                Model = v.Model,
                LicensePlate = v.LicensePlate,
                Year = v.Year,
                Mileage = v.Mileage,
                Owner = v.Owner?.FullName ?? "—"
            }).ToList();

            _grid.DataSource = _rows;
            ConfigureColumns();
            _count.Text = $"Показано {_rows.Count} из {page.TotalCount}";

            UiTheme.SelectFirstRow(_grid);
        }
        catch (Exception ex)
        {
            UiTheme.Error("Не удалось загрузить автомобили:\n" + ex.Message);
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
        _grid.Columns["Brand"]!.HeaderText = "Марка";
        _grid.Columns["Model"]!.HeaderText = "Модель";
        _grid.Columns["LicensePlate"]!.HeaderText = "Госномер";
        _grid.Columns["Year"]!.HeaderText = "Год";
        _grid.Columns["Mileage"]!.HeaderText = "Пробег, км";
        _grid.Columns["Mileage"]!.DefaultCellStyle.Format = "N0";
        _grid.Columns["Owner"]!.HeaderText = "Владелец";
        _grid.Columns["Owner"]!.FillWeight = 160;

        if (!AppServices.IsManager)
            _grid.Columns["Owner"]!.Visible = false;
    }

    private VehicleRow? SelectedRow => _grid.CurrentRow?.DataBoundItem as VehicleRow;

    private void UpdateButtons()
    {
        var has = SelectedRow is not null;
        _editButton.Enabled = has;
        _deleteButton.Enabled = has;
    }

    private void AddVehicle()
    {
        using var form = new VehicleEditForm(null);
        if (form.ShowDialog(this) == DialogResult.OK)
            _ = LoadDataAsync();
    }

    private void EditSelected()
    {
        var row = SelectedRow;
        if (row is null)
            return;

        using var form = new VehicleEditForm(row.Id);
        if (form.ShowDialog(this) == DialogResult.OK)
            _ = LoadDataAsync();
    }

    private async Task DeleteSelectedAsync()
    {
        var row = SelectedRow;
        if (row is null)
            return;

        if (!UiTheme.Confirm($"Удалить автомобиль {row.Brand} {row.Model} ({row.LicensePlate})?"))
            return;

        try
        {
            if (await AppServices.Requests.HasRequestsForVehicleAsync(row.Id))
            {
                UiTheme.Error("Нельзя удалить автомобиль: по нему уже есть заявки на ремонт.\n" +
                              "Сначала удалите или перенесите связанные заявки.");
                return;
            }

            var vehicle = await AppServices.Vehicles.GetByIdAsync(row.Id);
            if (vehicle is null)
                return;

            await AppServices.Vehicles.DeleteAsync(vehicle);
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            UiTheme.Error("Не удалось удалить автомобиль:\n" + ex.Message);
        }
    }

    private sealed class VehicleRow
    {
        public int Id { get; init; }
        public string Brand { get; init; } = string.Empty;
        public string Model { get; init; } = string.Empty;
        public string LicensePlate { get; init; } = string.Empty;
        public int Year { get; init; }
        public int Mileage { get; init; }
        public string Owner { get; init; } = string.Empty;
    }
}
