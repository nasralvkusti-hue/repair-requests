using RepairRequests.Core.Repositories;
using RepairRequests.WinForms.Infrastructure;

namespace RepairRequests.WinForms.Forms;

/// <summary>Справочник неисправностей с нормативами трудоёмкости (модуль А).</summary>
public class FaultsForm : Form
{
    private readonly DataGridView _grid;
    private readonly TextBox _search;
    private readonly Label _count;
    private readonly Button _editButton;
    private readonly Button _deleteButton;
    private List<FaultRow> _rows = new();

    public FaultsForm()
    {
        Text = "Справочник неисправностей";
        ClientSize = new Size(1050, 600);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = UiTheme.Background;
        ForeColor = UiTheme.Text;
        Font = UiTheme.BaseFont;

        Controls.Add(UiTheme.Title("Неисправности", 20, 12));
        Controls.Add(UiTheme.MutedLabel(
            "Справочник неисправностей: категория, нормативная трудоёмкость и базовая стоимость работ.",
            20, 52, 950));

        Controls.Add(UiTheme.Label("Поиск", 20, 82, bold: true, size: 10F));
        _search = UiTheme.TextBox(20, 106, 340);
        _search.PlaceholderText = "наименование или категория";
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

        _count = UiTheme.MutedLabel("Загрузка…", 690, 108, 340);
        _count.TextAlign = ContentAlignment.MiddleRight;
        Controls.Add(_count);

        _grid = UiTheme.Grid(20, 150, 1010, 360);
        _grid.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0)
                EditSelected();
        };
        _grid.SelectionChanged += (_, _) => UpdateButtons();
        Controls.Add(_grid);

        var add = UiTheme.Button("Добавить", UiTheme.Green, 20, 530, 140, 46);
        UiTheme.Flatten(add);
        add.Click += (_, _) => AddFault();
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

        var back = UiTheme.Button("Вернуться", UiTheme.Red, 880, 530, 150, 46);
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
            var filter = new FaultTypeFilter
            {
                Search = string.IsNullOrWhiteSpace(_search.Text) ? null : _search.Text.Trim(),
                IncludeInactive = true,
                Page = 1,
                PageSize = 500
            };

            var page = await AppServices.FaultTypes.GetPagedAsync(filter);
            _rows = page.Items.Select(f => new FaultRow
            {
                Id = f.Id,
                Name = f.Name,
                Category = f.Category,
                NormHours = f.NormHours,
                BasePrice = f.BasePrice,
                Active = f.IsActive ? "да" : "нет",
                Description = f.Description ?? string.Empty
            }).ToList();

            _grid.DataSource = _rows;
            ConfigureColumns();
            _count.Text = $"Показано {_rows.Count} из {page.TotalCount}";

            UiTheme.SelectFirstRow(_grid);
        }
        catch (Exception ex)
        {
            UiTheme.Error("Не удалось загрузить справочник:\n" + ex.Message);
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
        _grid.Columns["Name"]!.HeaderText = "Неисправность";
        _grid.Columns["Name"]!.FillWeight = 170;
        _grid.Columns["Category"]!.HeaderText = "Категория";
        _grid.Columns["Category"]!.FillWeight = 120;
        _grid.Columns["NormHours"]!.HeaderText = "Норматив, ч";
        _grid.Columns["NormHours"]!.FillWeight = 75;
        _grid.Columns["NormHours"]!.DefaultCellStyle.Format = "0.0";
        _grid.Columns["BasePrice"]!.HeaderText = "Работы, ₽";
        _grid.Columns["BasePrice"]!.FillWeight = 80;
        _grid.Columns["BasePrice"]!.DefaultCellStyle.Format = "N2";
        _grid.Columns["BasePrice"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        _grid.Columns["Active"]!.HeaderText = "Активна";
        _grid.Columns["Active"]!.FillWeight = 50;
        _grid.Columns["Description"]!.HeaderText = "Описание";
        _grid.Columns["Description"]!.FillWeight = 230;
    }

    private FaultRow? SelectedRow => _grid.CurrentRow?.DataBoundItem as FaultRow;

    private void UpdateButtons()
    {
        var has = SelectedRow is not null;
        _editButton.Enabled = has;
        _deleteButton.Enabled = has;
    }

    private void AddFault()
    {
        using var form = new FaultTypeEditForm(null);
        if (form.ShowDialog(this) == DialogResult.OK)
            _ = LoadDataAsync();
    }

    private void EditSelected()
    {
        var row = SelectedRow;
        if (row is null)
            return;

        using var form = new FaultTypeEditForm(row.Id);
        if (form.ShowDialog(this) == DialogResult.OK)
            _ = LoadDataAsync();
    }

    private async Task DeleteSelectedAsync()
    {
        var row = SelectedRow;
        if (row is null)
            return;

        if (!UiTheme.Confirm($"Удалить неисправность «{row.Name}»?"))
            return;

        try
        {
            if (await AppServices.FaultTypes.HasRequestsAsync(row.Id))
            {
                UiTheme.Error("Нельзя удалить неисправность: по ней уже есть заявки.\n" +
                              "Можно отметить запись как неактивную.");
                return;
            }

            var entity = await AppServices.FaultTypes.GetByIdAsync(row.Id);
            if (entity is null)
                return;

            await AppServices.FaultTypes.DeleteAsync(entity);
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            UiTheme.Error("Не удалось удалить запись справочника:\n" + ex.Message);
        }
    }

    private sealed class FaultRow
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Category { get; init; } = string.Empty;
        public decimal NormHours { get; init; }
        public decimal BasePrice { get; init; }
        public string Active { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
    }
}
