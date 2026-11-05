using RepairRequests.Core.Models;
using RepairRequests.WinForms.Infrastructure;

namespace RepairRequests.WinForms.Forms;

/// <summary>Добавление и редактирование автомобиля.</summary>
public class VehicleEditForm : Form
{
    private readonly int? _vehicleId;
    private readonly bool _manager;

    private readonly TextBox _brand = UiTheme.TextBox(30, 94, 230);
    private readonly TextBox _model = UiTheme.TextBox(290, 94, 230);
    private readonly TextBox _plate = UiTheme.TextBox(30, 164, 230);
    private readonly TextBox _vin = UiTheme.TextBox(290, 164, 230);
    private readonly NumericUpDown _year = UiTheme.Number(30, 234, 230, 2100, 0, thousandsSeparator: false);
    private readonly NumericUpDown _mileage = UiTheme.Number(290, 234, 230, 5_000_000, 0);
    private readonly ComboBox _ownerCombo = UiTheme.Combo(30, 304, 490);
    private readonly Label _ownerLabel;
    private readonly Button _save;

    private Vehicle? _vehicle;
    private List<User> _clients = new();

    public VehicleEditForm(int? vehicleId)
    {
        _vehicleId = vehicleId;
        _manager = AppServices.IsManager;

        Text = vehicleId.HasValue ? "Редактирование автомобиля" : "Новый автомобиль";
        ClientSize = new Size(560, 520);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = UiTheme.Background;
        ForeColor = UiTheme.Text;
        Font = UiTheme.BaseFont;

        Controls.Add(UiTheme.Title(vehicleId.HasValue ? "Автомобиль" : "Новый автомобиль", 30, 16));

        Controls.Add(UiTheme.Label("Марка *", 30, 68, bold: true, size: 10F));
        Controls.Add(_brand);
        Controls.Add(UiTheme.Label("Модель *", 290, 68, bold: true, size: 10F));
        Controls.Add(_model);

        Controls.Add(UiTheme.Label("Государственный номер *", 30, 138, bold: true, size: 10F));
        Controls.Add(_plate);
        _plate.MaxLength = 12;
        _plate.CharacterCasing = CharacterCasing.Upper;
        Controls.Add(UiTheme.MutedLabel("формат: А123ВС77", 30, 198, 230));

        Controls.Add(UiTheme.Label("VIN (17 символов, необязательно)", 290, 138, bold: true, size: 10F));
        Controls.Add(_vin);
        _vin.MaxLength = 17;
        _vin.CharacterCasing = CharacterCasing.Upper;

        Controls.Add(UiTheme.Label("Год выпуска", 30, 210, bold: true, size: 10F));
        Controls.Add(_year);
        _year.Minimum = 1950;
        _year.Maximum = 2100;
        _year.Value = DateTime.Today.Year;

        Controls.Add(UiTheme.Label("Пробег, км", 290, 210, bold: true, size: 10F));
        Controls.Add(_mileage);

        var ownerLabel = UiTheme.Label("Владелец", 30, 278, bold: true, size: 10F);
        Controls.Add(ownerLabel);
        _ownerCombo.Visible = _manager;
        Controls.Add(_ownerCombo);

        _ownerLabel = UiTheme.Label(AppServices.CurrentUser?.FullName ?? string.Empty, 30, 306, 490);
        _ownerLabel.Visible = !_manager;
        Controls.Add(_ownerLabel);

        var hint = UiTheme.MutedLabel(
            "Поля со звёздочкой обязательны. Госномер вводится без пробелов.",
            30, 360, 490);
        hint.Height = 36;
        Controls.Add(hint);

        _save = UiTheme.Button("Сохранить", UiTheme.Green, 30, 440, 230, 50, 12F);
        UiTheme.Flatten(_save);
        _save.Click += Save_Click;
        Controls.Add(_save);

        var cancel = UiTheme.Button("Отмена", UiTheme.Red, 290, 440, 230, 50, 12F);
        UiTheme.Flatten(cancel);
        cancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        Controls.Add(cancel);

        Shown += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            if (_manager)
            {
                _clients = (await AppServices.Users.GetClientsAsync()).ToList();
                _ownerCombo.DataSource = _clients;
                _ownerCombo.DisplayMember = nameof(User.FullName);
                _ownerCombo.ValueMember = nameof(User.Id);
            }

            if (!_vehicleId.HasValue)
            {
                if (_manager && _clients.Count > 0)
                    _ownerCombo.SelectedIndex = 0;

                _mileage.Value = 0;
                return;
            }

            _vehicle = await AppServices.Vehicles.GetByIdAsync(_vehicleId.Value);
            if (_vehicle is null)
            {
                UiTheme.Error("Автомобиль не найден.");
                Close();
                return;
            }

            _brand.Text = _vehicle.Brand;
            _model.Text = _vehicle.Model;
            _plate.Text = _vehicle.LicensePlate;
            _vin.Text = _vehicle.Vin ?? string.Empty;
            _year.Value = Math.Clamp(_vehicle.Year, 1950, 2100);
            _mileage.Value = Math.Clamp(_vehicle.Mileage, 0, 5_000_000);

            if (_manager)
                _ownerCombo.SelectedValue = _vehicle.OwnerId;
            else
                _ownerLabel.Text = _vehicle.Owner?.FullName ?? AppServices.CurrentUser?.FullName ?? string.Empty;
        }
        catch (Exception ex)
        {
            UiTheme.Error("Не удалось загрузить автомобиль:\n" + ex.Message);
            Close();
        }
    }

    private async void Save_Click(object? sender, EventArgs e)
    {
        _save.Enabled = false;
        try
        {
            var ownerId = _manager
                ? (_ownerCombo.SelectedItem as User)?.Id ?? 0
                : AppServices.CurrentUser!.Id;

            if (ownerId == 0)
            {
                UiTheme.Error("Выберите владельца автомобиля.");
                return;
            }

            var plate = _plate.Text.Trim().ToUpperInvariant();
            if (await AppServices.Vehicles.LicensePlateExistsAsync(plate, _vehicle?.Id))
            {
                UiTheme.Error("Автомобиль с таким государственным номером уже существует.");
                return;
            }

            var vehicle = _vehicle ?? new Vehicle();
            vehicle.OwnerId = ownerId;
            vehicle.Brand = _brand.Text.Trim();
            vehicle.Model = _model.Text.Trim();
            vehicle.LicensePlate = plate;
            vehicle.Vin = string.IsNullOrWhiteSpace(_vin.Text) ? null : _vin.Text.Trim().ToUpperInvariant();
            vehicle.Year = (int)_year.Value;
            vehicle.Mileage = (int)_mileage.Value;

            var errors = DataValidator.Validate(vehicle);
            if (errors.Count > 0)
            {
                UiTheme.Error(string.Join("\n", errors));
                return;
            }

            if (_vehicle is null)
            {
                vehicle.CreatedAt = DateTime.Now;
                await AppServices.Vehicles.AddAsync(vehicle);
            }
            else
            {
                await AppServices.Vehicles.UpdateAsync(vehicle);
            }

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            UiTheme.Error("Не удалось сохранить автомобиль:\n" + ex.Message);
        }
        finally
        {
            _save.Enabled = true;
        }
    }
}
