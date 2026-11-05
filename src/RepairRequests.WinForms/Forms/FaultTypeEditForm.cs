using RepairRequests.Core.Models;
using RepairRequests.WinForms.Infrastructure;

namespace RepairRequests.WinForms.Forms;

/// <summary>Добавление и редактирование записи справочника неисправностей.</summary>
public class FaultTypeEditForm : Form
{
    private readonly int? _faultId;

    private readonly TextBox _name = UiTheme.TextBox(30, 94, 540);
    private readonly ComboBox _category = UiTheme.Combo(30, 164, 540);
    private readonly NumericUpDown _norm = UiTheme.Number(30, 234, 250, 200, 1);
    private readonly NumericUpDown _price = UiTheme.Number(310, 234, 260, 1_000_000, 2);
    private readonly TextBox _description = UiTheme.TextBox(30, 304, 540, true, 120);
    private readonly CheckBox _active = new();
    private readonly Button _save;

    private FaultType? _fault;

    public FaultTypeEditForm(int? faultId)
    {
        _faultId = faultId;
        Text = faultId.HasValue ? "Редактирование неисправности" : "Новая неисправность";
        ClientSize = new Size(600, 590);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = UiTheme.Background;
        ForeColor = UiTheme.Text;
        Font = UiTheme.BaseFont;

        Controls.Add(UiTheme.Title(faultId.HasValue ? "Неисправность" : "Новая запись", 30, 16));

        Controls.Add(UiTheme.Label("Наименование *", 30, 68, bold: true, size: 10F));
        Controls.Add(_name);
        _name.MaxLength = 120;

        Controls.Add(UiTheme.Label("Категория *", 30, 138, bold: true, size: 10F));
        _category.DropDownStyle = ComboBoxStyle.DropDown;
        Controls.Add(_category);

        Controls.Add(UiTheme.Label("Норматив, ч", 30, 208, bold: true, size: 10F));
        Controls.Add(_norm);
        _norm.DecimalPlaces = 1;
        _norm.Minimum = 0.1m;
        _norm.Maximum = 200m;
        _norm.Value = 1m;

        Controls.Add(UiTheme.Label("Стоимость работ, ₽", 310, 208, bold: true, size: 10F));
        Controls.Add(_price);

        Controls.Add(UiTheme.Label("Описание", 30, 278, bold: true, size: 10F));
        Controls.Add(_description);
        _description.MaxLength = 500;

        _active.Text = "Запись активна и доступна при оформлении заявок";
        _active.Location = new Point(30, 442);
        _active.AutoSize = true;
        _active.ForeColor = UiTheme.Text;
        _active.Checked = true;
        Controls.Add(_active);

        _save = UiTheme.Button("Сохранить", UiTheme.Green, 30, 500, 250, 50, 12F);
        UiTheme.Flatten(_save);
        _save.Click += Save_Click;
        Controls.Add(_save);

        var cancel = UiTheme.Button("Отмена", UiTheme.Red, 320, 500, 250, 50, 12F);
        UiTheme.Flatten(cancel);
        cancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        Controls.Add(cancel);

        Shown += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            var categories = await AppServices.FaultTypes.GetCategoriesAsync();
            _category.Items.Clear();
            foreach (var category in categories)
                _category.Items.Add(category);

            if (!_faultId.HasValue)
            {
                if (_category.Items.Count > 0)
                    _category.SelectedIndex = 0;
                return;
            }

            _fault = await AppServices.FaultTypes.GetByIdAsync(_faultId.Value);
            if (_fault is null)
            {
                UiTheme.Error("Запись справочника не найдена.");
                Close();
                return;
            }

            _name.Text = _fault.Name;
            _category.Text = _fault.Category;
            _norm.Value = Math.Clamp(_fault.NormHours, 0.1m, 200m);
            _price.Value = Math.Min(_fault.BasePrice, _price.Maximum);
            _description.Text = _fault.Description ?? string.Empty;
            _active.Checked = _fault.IsActive;
        }
        catch (Exception ex)
        {
            UiTheme.Error("Не удалось загрузить запись:\n" + ex.Message);
            Close();
        }
    }

    private async void Save_Click(object? sender, EventArgs e)
    {
        _save.Enabled = false;
        try
        {
            var name = _name.Text.Trim();
            var category = _category.Text.Trim();

            if (await AppServices.FaultTypes.NameExistsAsync(name, _fault?.Id))
            {
                UiTheme.Error("Неисправность с таким наименованием уже есть в справочнике.");
                return;
            }

            var fault = _fault ?? new FaultType();
            fault.Name = name;
            fault.Category = category;
            fault.NormHours = _norm.Value;
            fault.BasePrice = _price.Value;
            fault.Description = string.IsNullOrWhiteSpace(_description.Text) ? null : _description.Text.Trim();
            fault.IsActive = _active.Checked;

            var errors = DataValidator.Validate(fault);
            if (errors.Count > 0)
            {
                UiTheme.Error(string.Join("\n", errors));
                return;
            }

            if (_fault is null)
                await AppServices.FaultTypes.AddAsync(fault);
            else
                await AppServices.FaultTypes.UpdateAsync(fault);

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            UiTheme.Error("Не удалось сохранить запись:\n" + ex.Message);
        }
        finally
        {
            _save.Enabled = true;
        }
    }
}
