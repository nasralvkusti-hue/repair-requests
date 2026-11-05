using RepairRequests.WinForms.Infrastructure;

namespace RepairRequests.WinForms.Forms;

/// <summary>Регистрация нового клиента сервиса.</summary>
public class RegisterForm : Form
{
    private readonly TextBox _fullName;
    private readonly TextBox _email;
    private readonly TextBox _phone;
    private readonly TextBox _password;
    private readonly TextBox _confirm;
    private readonly Button _submit;

    public string RegisteredEmail { get; private set; } = string.Empty;

    public RegisterForm()
    {
        Text = "Регистрация клиента";
        ClientSize = new Size(540, 640);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = UiTheme.Background;
        ForeColor = UiTheme.Text;
        Font = UiTheme.BaseFont;

        Controls.Add(UiTheme.Title("Регистрация", 30, 18));

        var y = 70;
        Controls.Add(UiTheme.Label("ФИО", 30, y, bold: true));
        _fullName = UiTheme.TextBox(30, y + 26, 460);
        Controls.Add(_fullName);

        y += 70;
        Controls.Add(UiTheme.Label("Электронная почта", 30, y, bold: true));
        _email = UiTheme.TextBox(30, y + 26, 460);
        Controls.Add(_email);

        y += 70;
        Controls.Add(UiTheme.Label("Телефон", 30, y, bold: true));
        _phone = UiTheme.TextBox(30, y + 26, 460);
        Controls.Add(_phone);

        y += 70;
        Controls.Add(UiTheme.Label("Пароль (не менее 8 символов, буквы и цифры)", 30, y, bold: true));
        _password = UiTheme.TextBox(30, y + 26, 220);
        _password.UseSystemPasswordChar = true;
        Controls.Add(_password);

        Controls.Add(UiTheme.Label("Повторите пароль", 270, y, bold: true));
        _confirm = UiTheme.TextBox(270, y + 26, 220);
        _confirm.UseSystemPasswordChar = true;
        Controls.Add(_confirm);

        _submit = UiTheme.Button("Зарегистрироваться", UiTheme.Green, 30, 540, 280, 52);
        UiTheme.Flatten(_submit);
        _submit.Click += Submit_Click;
        Controls.Add(_submit);

        var cancel = UiTheme.Button("Отмена", UiTheme.Red, 330, 540, 160, 52);
        UiTheme.Flatten(cancel);
        cancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        Controls.Add(cancel);

        var hint = UiTheme.MutedLabel(
            "После регистрации вы сможете войти в систему\nи подать заявку на ремонт своего автомобиля.",
            30, 480, 460);
        hint.Height = 40;
        Controls.Add(hint);

        AcceptButton = _submit;
    }

    private async void Submit_Click(object? sender, EventArgs e)
    {
        _submit.Enabled = false;
        try
        {
            var result = await AppServices.Auth.RegisterAsync(
                _fullName.Text, _email.Text, _phone.Text, _password.Text, _confirm.Text);

            if (!result.Success || result.User is null)
            {
                UiTheme.Error(result.Error ?? "Не удалось зарегистрироваться.");
                return;
            }

            RegisteredEmail = result.User.Email;
            UiTheme.Info("Регистрация завершена. Теперь можно войти в систему.");
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            UiTheme.Error("Не удалось зарегистрироваться:\n" + ex.Message);
        }
        finally
        {
            _submit.Enabled = true;
        }
    }
}
