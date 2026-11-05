using RepairRequests.Core.Models;
using RepairRequests.Core.Services;
using RepairRequests.WinForms.Infrastructure;

namespace RepairRequests.WinForms.Forms;

/// <summary>
/// Создание и редактирование заявки: данные автомобиля, неисправность, стоимость,
/// назначение механика и изменение статуса по жизненному циклу.
/// </summary>
public class RequestEditForm : Form
{
    private readonly int? _requestId;
    private readonly bool _manager;
    private readonly bool _client;

    private readonly Label _title;
    private readonly Label _info;
    private readonly Label _statusBadge;

    private readonly ComboBox _vehicleCombo = UiTheme.Combo(20, 104, 520);
    private readonly ComboBox _faultCombo = UiTheme.Combo(20, 170, 520);
    private readonly ComboBox _priorityCombo = UiTheme.Combo(20, 236, 245);
    private readonly DateTimePicker _planned = UiTheme.Date(285, 236, 255);
    private readonly ComboBox _mechanicCombo = UiTheme.Combo(20, 302, 245);
    private readonly NumericUpDown _labor = UiTheme.Number(285, 302, 255);
    private readonly NumericUpDown _parts = UiTheme.Number(20, 368, 245);
    private readonly Label _total;
    private readonly TextBox _description = UiTheme.TextBox(20, 434, 520, true, 105);
    private readonly TextBox _comment = UiTheme.TextBox(20, 572, 520, true, 55);

    private readonly DataGridView _history = UiTheme.Grid(585, 104, 395, 210);
    private readonly Label _actionHint;
    private readonly Button _saveButton;

    private readonly List<Button> _actionButtons = new();
    private readonly List<(Button Button, RequestStatus Target)> _actions = new();

    private List<Vehicle> _vehicles = new();
    private List<FaultType> _faults = new();
    private List<MechanicItem> _mechanics = new();

    private RepairRequest? _request;
    private bool _loading;

    public RequestEditForm(int? requestId)
    {
        _requestId = requestId;
        _manager = AppServices.IsManager;
        _client = !_manager;

        Text = requestId.HasValue ? "Редактирование заявки" : "Новая заявка на ремонт";
        ClientSize = new Size(1000, 750);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = UiTheme.Background;
        ForeColor = UiTheme.Text;
        Font = UiTheme.BaseFont;

        _title = UiTheme.Title(requestId.HasValue ? "Заявка" : "Новая заявка", 20, 12);
        Controls.Add(_title);

        _statusBadge = new Label
        {
            Location = new Point(700, 14),
            Size = new Size(280, 40),
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Microsoft Sans Serif", 13F, FontStyle.Bold),
            BackColor = UiTheme.Neutral,
            ForeColor = Color.White,
            Text = "Новая"
        };
        Controls.Add(_statusBadge);

        _info = UiTheme.MutedLabel("Заполните данные заявки", 20, 54, 660);
        _info.Height = 40;
        Controls.Add(_info);

        AddField("Автомобиль *", _vehicleCombo, 20, 80);
        AddField("Неисправность *", _faultCombo, 20, 146);

        AddField("Приоритет", _priorityCombo, 20, 212);
        AddField("Плановое завершение", _planned, 285, 212);

        var mechanicLabel = AddField("Механик", _mechanicCombo, 20, 278);
        AddField("Стоимость работ, ₽", _labor, 285, 278);

        AddField("Стоимость запчастей, ₽", _parts, 20, 344);
        Controls.Add(UiTheme.Label("Итого, ₽", 285, 344, bold: true, size: 11F));
        _total = new Label
        {
            Location = new Point(285, 368),
            Size = new Size(255, 30),
            Font = new Font("Microsoft Sans Serif", 13F, FontStyle.Bold),
            ForeColor = UiTheme.Green,
            Text = "0,00",
            TextAlign = ContentAlignment.MiddleLeft
        };
        Controls.Add(_total);

        AddField("Описание неисправности * (не менее 10 символов)", _description, 20, 410);
        _description.Height = 105;

        var commentLabel = AddField("Комментарий механика", _comment, 20, 548);
        _comment.Height = 55;

        var hint = UiTheme.MutedLabel(
            "Поля со звёздочкой обязательны. Номер заявки присваивается автоматически.",
            20, 642, 520);
        hint.Height = 36;
        Controls.Add(hint);

        if (_client)
        {
            mechanicLabel.Visible = false;
            _mechanicCombo.Visible = false;
            commentLabel.Visible = false;
            _comment.Visible = false;
        }

        Controls.Add(UiTheme.Label("История заявки", 585, 80, bold: true, size: 11F));
        Controls.Add(_history);
        _history.Font = new Font("Microsoft Sans Serif", 8.5F);
        _history.ColumnHeadersHeight = 30;
        _history.RowTemplate.Height = 26;

        Controls.Add(UiTheme.Label("Действия", 585, 320, bold: true, size: 11F));

        AddAction("Принять заявку", RequestStatus.Accepted);
        AddAction("Начать работы", RequestStatus.InProgress);
        AddAction("Завершить ремонт", RequestStatus.Completed);
        AddAction("Закрыть заявку", RequestStatus.Closed);
        AddAction("Отменить заявку", RequestStatus.Cancelled);

        _actionHint = UiTheme.MutedLabel(
            "Действия доступны в соответствии с текущим статусом заявки.",
            585, 570, 395);
        _actionHint.Height = 60;
        Controls.Add(_actionHint);

        _saveButton = UiTheme.Button("Сохранить", UiTheme.Green, 20, 680, 190, 50, 12F);
        UiTheme.Flatten(_saveButton);
        _saveButton.Click += SaveButton_Click;
        Controls.Add(_saveButton);

        var close = UiTheme.Button("Закрыть", UiTheme.Red, 790, 680, 190, 50, 12F);
        UiTheme.Flatten(close);
        close.Click += (_, _) => Close();
        Controls.Add(close);

        _priorityCombo.Items.AddRange(new object[] { "Низкий", "Обычный", "Высокий", "Критический" });
        _priorityCombo.SelectedIndex = 1;

        _planned.Value = DateTime.Today.AddDays(3);
        _description.MaxLength = 1000;
        _comment.MaxLength = 1000;

        _labor.ValueChanged += (_, _) => UpdateTotal();
        _parts.ValueChanged += (_, _) => UpdateTotal();
        _faultCombo.SelectedIndexChanged += FaultChanged;

        Shown += async (_, _) => await LoadAsync();
    }

    private Label AddField(string caption, Control control, int x, int y)
    {
        var label = UiTheme.Label(caption, x, y, bold: true, size: 10F);
        Controls.Add(label);
        control.Location = new Point(x, y + 24);
        Controls.Add(control);
        return label;
    }

    private void AddAction(string text, RequestStatus target)
    {
        var button = UiTheme.Button(text, target == RequestStatus.Cancelled ? UiTheme.Red : UiTheme.Neutral,
            585, 344, 395, 38, 10F);
        UiTheme.Flatten(button);
        button.Visible = false;
        button.Click += async (_, _) => await ChangeStatusAsync(target);
        Controls.Add(button);
        _actionButtons.Add(button);
        _actions.Add((button, target));
    }

    private async Task LoadAsync()
    {
        _loading = true;
        UseWaitCursor = true;
        try
        {
            _vehicles = (await AppServices.Vehicles.GetAllAsync(AppServices.ClientScope)).ToList();
            _faults = (await AppServices.FaultTypes.GetActiveAsync()).ToList();

            _vehicleCombo.DataSource = null;
            _vehicleCombo.DisplayMember = nameof(Vehicle.DisplayName);
            _vehicleCombo.ValueMember = nameof(Vehicle.Id);
            _vehicleCombo.DataSource = _vehicles;

            _faultCombo.DataSource = null;
            _faultCombo.DisplayMember = nameof(FaultType.Name);
            _faultCombo.ValueMember = nameof(FaultType.Id);
            _faultCombo.DataSource = _faults;

            _mechanics = new List<MechanicItem> { new(null, "не назначен") };
            if (_manager)
            {
                var users = await AppServices.Users.GetMechanicsAsync();
                _mechanics.AddRange(users.Select(u => new MechanicItem(u.Id, u.FullName)));
            }

            _mechanicCombo.DataSource = null;
            _mechanicCombo.DisplayMember = nameof(MechanicItem.Name);
            _mechanicCombo.DataSource = _mechanics;

            if (_requestId.HasValue)
            {
                _request = await AppServices.Requests.GetByIdAsync(_requestId.Value);
                if (_request is null)
                {
                    UiTheme.Error("Заявка не найдена.");
                    Close();
                    return;
                }

                _title.Text = $"Заявка {_request.Number}";
                PopulateFromRequest();
            }
            else
            {
                _title.Text = "Новая заявка";
                _statusBadge.Text = RequestStatus.New.Ru();
                _statusBadge.BackColor = UiTheme.StatusColor(_statusBadge.Text);
                _labor.Value = _faults.Count > 0 ? Math.Min(_faults[0].BasePrice, _labor.Maximum) : 0;
                UpdateTotal();
                ShowHistory(null);
            }

            ApplyAccessRules();
            RefreshActions();
        }
        catch (Exception ex)
        {
            UiTheme.Error("Не удалось открыть заявку:\n" + ex.Message);
            Close();
        }
        finally
        {
            _loading = false;
            UseWaitCursor = false;
        }
    }

    private void PopulateFromRequest()
    {
        if (_request is null)
            return;

        _loading = true;

        _vehicleCombo.SelectedValue = _request.VehicleId;
        _faultCombo.SelectedValue = _request.FaultTypeId;
        _priorityCombo.SelectedIndex = Math.Clamp((int)_request.Priority - 1, 0, 3);
        _planned.Value = _request.PlannedCompletion ?? DateTime.Today.AddDays(3);
        _mechanicCombo.SelectedIndex = Math.Max(0,
            _mechanics.FindIndex(m => m.Id == _request.AssignedMechanicId));
        _labor.Value = Math.Min(_request.LaborCost, _labor.Maximum);
        _parts.Value = Math.Min(_request.PartsCost, _parts.Maximum);
        _description.Text = _request.Description;
        _comment.Text = _request.Comment ?? string.Empty;

        _statusBadge.Text = _request.Status.Ru();
        _statusBadge.BackColor = UiTheme.StatusColor(_statusBadge.Text);

        _info.Text =
            $"Создана: {_request.CreatedAt:dd.MM.yyyy HH:mm}    " +
            $"Принята: {FormatDate(_request.AcceptedAt)}    " +
            $"Начата: {FormatDate(_request.StartedAt)}\n" +
            $"Завершена: {FormatDate(_request.CompletedAt)}    " +
            $"Плановое завершение: {_request.PlannedCompletion:dd.MM.yyyy}    " +
            $"Клиент: {_request.Client?.FullName ?? "—"}";

        ShowHistory(_request);
        UpdateTotal();
        _loading = false;
    }

    private static string FormatDate(DateTime? value) => value?.ToString("dd.MM.yyyy HH:mm") ?? "—";

    private void ShowHistory(RepairRequest? request)
    {
        var rows = request?.History
            .OrderByDescending(h => h.ChangedAt)
            .Select(h => new HistoryRow
            {
                Date = h.ChangedAt.ToString("dd.MM.yyyy HH:mm"),
                Status = h.Status.Ru(),
                User = h.ChangedBy?.FullName ?? "система",
                Comment = h.Comment ?? string.Empty
            })
            .ToList() ?? new List<HistoryRow>();

        _history.DataSource = rows;

        if (_history.Columns.Count == 0)
            return;

        _history.Columns["Date"]!.HeaderText = "Дата";
        _history.Columns["Date"]!.FillWeight = 85;
        _history.Columns["Status"]!.HeaderText = "Статус";
        _history.Columns["Status"]!.FillWeight = 70;
        _history.Columns["User"]!.HeaderText = "Пользователь";
        _history.Columns["User"]!.FillWeight = 100;
        _history.Columns["Comment"]!.HeaderText = "Комментарий";
        _history.Columns["Comment"]!.FillWeight = 120;
    }

    private bool CanEdit =>
        _request is null ||
        (_manager
            ? _request.Status is not (RequestStatus.Closed or RequestStatus.Cancelled)
            : _request.Status == RequestStatus.New);

    private void ApplyAccessRules()
    {
        var canEdit = CanEdit;

        _vehicleCombo.Enabled = canEdit;
        _faultCombo.Enabled = canEdit;
        _priorityCombo.Enabled = canEdit;
        _planned.Enabled = canEdit;
        _description.ReadOnly = !canEdit;
        _description.BackColor = canEdit ? UiTheme.InputBack : Color.FromArgb(210, 210, 210);

        _mechanicCombo.Enabled = canEdit && _manager;
        _labor.Enabled = canEdit && _manager;
        _parts.Enabled = canEdit && _manager;
        _comment.ReadOnly = !canEdit || _client;

        _saveButton.Visible = canEdit;
        foreach (var (button, _) in _actions)
            button.Enabled = canEdit;

        if (!canEdit)
        {
            _actionHint.Text = "Заявка доступна только для просмотра.";
            return;
        }

        _actionHint.Text = _request is null
            ? "Сохраните заявку, после чего станут доступны действия по смене статуса."
            : "Доступные действия соответствуют текущему статусу заявки.";
    }

    private void RefreshActions()
    {
        if (!CanEdit)
        {
            foreach (var (button, _) in _actions)
                button.Visible = false;
            return;
        }

        var allowed = _request is null
            ? Array.Empty<RequestStatus>()
            : _request.Status.NextStatuses();

        var visibleIndex = 0;
        foreach (var (button, target) in _actions)
        {
            var visible = allowed.Contains(target) && (_manager || target == RequestStatus.Cancelled);
            button.Visible = visible;
            if (!visible)
                continue;

            button.Location = new Point(585, 344 + visibleIndex * 46);
            visibleIndex++;
        }

        if (visibleIndex == 0)
        {
            _actionHint.Text = _request is null
                ? "Сохраните заявку, после чего станут доступны действия по смене статуса."
                : "Для текущего статуса действий нет.";
        }
    }

    private void FaultChanged(object? sender, EventArgs e)
    {
        if (_loading || _request is not null)
            return;

        if (_faultCombo.SelectedItem is FaultType fault)
            _labor.Value = Math.Min(fault.BasePrice, _labor.Maximum);
    }

    private void UpdateTotal()
    {
        _total.Text = (_labor.Value + _parts.Value).ToString("N2") + " ₽";
    }

    private Vehicle? SelectedVehicle => _vehicleCombo.SelectedItem as Vehicle;
    private FaultType? SelectedFault => _faultCombo.SelectedItem as FaultType;
    private int? SelectedMechanicId => (_mechanicCombo.SelectedItem as MechanicItem)?.Id;

    private bool ValidateFields()
    {
        if (SelectedVehicle is null)
        {
            UiTheme.Error("Выберите автомобиль. Если автомобиля нет в списке, добавьте его в разделе «Мой транспорт».");
            return false;
        }

        if (SelectedFault is null)
        {
            UiTheme.Error("Выберите неисправность из справочника.");
            return false;
        }

        if (_description.Text.Trim().Length < 10)
        {
            UiTheme.Error("Описание неисправности должно содержать не менее 10 символов.");
            _description.Focus();
            return false;
        }

        return true;
    }

    private void ApplyFields(RepairRequest request)
    {
        var vehicle = SelectedVehicle!;
        request.VehicleId = vehicle.Id;
        request.FaultTypeId = SelectedFault!.Id;
        request.ClientId = vehicle.OwnerId;
        request.AssignedMechanicId = _manager ? SelectedMechanicId : request.AssignedMechanicId;
        request.Description = _description.Text.Trim();
        request.Priority = (RequestPriority)(_priorityCombo.SelectedIndex + 1);
        request.PlannedCompletion = _planned.Value.Date;
        request.LaborCost = _labor.Value;
        request.PartsCost = _parts.Value;

        if (_manager)
            request.Comment = string.IsNullOrWhiteSpace(_comment.Text) ? null : _comment.Text.Trim();
    }

    private async void SaveButton_Click(object? sender, EventArgs e)
    {
        if (!ValidateFields())
            return;

        _saveButton.Enabled = false;
        try
        {
            if (_request is null)
            {
                var vehicle = SelectedVehicle!;
                var request = new RepairRequest
                {
                    Number = await AppServices.Requests.GenerateNumberAsync(),
                    Status = RequestStatus.New,
                    CreatedAt = DateTime.Now,
                    History = new List<RequestStatusHistory>
                    {
                        new()
                        {
                            Status = RequestStatus.New,
                            ChangedAt = DateTime.Now,
                            ChangedById = AppServices.CurrentUser!.Id,
                            Comment = "Заявка создана"
                        }
                    }
                };

                ApplyFields(request);
                await AppServices.Requests.AddAsync(request);
                _request = request;
                UiTheme.Info($"Заявка создана. Присвоен номер {request.Number}.");
            }
            else
            {
                ApplyFields(_request);
                await AppServices.Requests.UpdateAsync(_request);
                UiTheme.Info("Изменения сохранены.");
            }

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            UiTheme.Error("Не удалось сохранить заявку:\n" + ex.Message);
        }
        finally
        {
            _saveButton.Enabled = true;
        }
    }

    private async Task ChangeStatusAsync(RequestStatus target)
    {
        if (_request is null)
            return;

        if (!UiTheme.Confirm($"Изменить статус заявки {_request.Number} на «{target.Ru()}»?"))
            return;

        string? comment = null;
        if (target == RequestStatus.Cancelled)
        {
            using var prompt = new TextPromptForm("Причина отмены", "Укажите причину отмены заявки:");
            if (prompt.ShowDialog(this) != DialogResult.OK)
                return;
            comment = prompt.Value;
        }

        try
        {
            UseWaitCursor = true;

            if (!ValidateFields())
                return;

            ApplyFields(_request);
            await AppServices.Requests.UpdateAsync(_request);

            _request = await AppServices.Requests.GetByIdAsync(_request.Id) ?? _request;

            if (_request.AssignedMechanicId is null
                && target is RequestStatus.Accepted or RequestStatus.InProgress or RequestStatus.Completed)
            {
                _request.AssignedMechanicId = AppServices.CurrentUser!.Id;
            }

            RequestWorkflow.Apply(_request, target, AppServices.CurrentUser!.Id, comment);
            await AppServices.Requests.UpdateAsync(_request);

            _request = await AppServices.Requests.GetByIdAsync(_request.Id) ?? _request;
            PopulateFromRequest();
            ApplyAccessRules();
            RefreshActions();

            UiTheme.Info($"Статус заявки изменён на «{target.Ru()}».");
        }
        catch (Exception ex)
        {
            UiTheme.Error("Не удалось изменить статус:\n" + ex.Message);
        }
        finally
        {
            UseWaitCursor = false;
        }
    }

    private sealed record MechanicItem(int? Id, string Name);

    private sealed class HistoryRow
    {
        public string Date { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public string User { get; init; } = string.Empty;
        public string Comment { get; init; } = string.Empty;
    }
}
