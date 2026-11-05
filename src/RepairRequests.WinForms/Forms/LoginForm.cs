using RepairRequests.WinForms.Infrastructure;

namespace RepairRequests.WinForms.Forms;

/// <summary>Форма авторизации (аналог Form1 в PcClab).</summary>
public class LoginForm : Form
{
    private readonly TextBox _email;
    private readonly TextBox _password;
    private readonly Button _loginButton;

    public LoginForm()
    {
        Text = "Система заявок на ремонт — вход";
        ClientSize = new Size(520, 610);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = UiTheme.Background;
        ForeColor = UiTheme.Text;
        Font = UiTheme.BaseFont;

        var logo = UiTheme.Picture("logo.png", 110, 12, 300, 230);

        var subtitle = new Label
        {
            Text = "ВХОД В СИСТЕМУ",
            Font = new Font("Microsoft Sans Serif", 16F, FontStyle.Bold),
            ForeColor = UiTheme.Green,
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(40, 246),
            Size = new Size(440, 34)
        };

        var emailLabel = UiTheme.Label("Логин (e-mail)", 56, 300, bold: true);
        _email = UiTheme.TextBox(56, 328, 190);
        _email.Text = "admin@remont.ru";

        var passwordLabel = UiTheme.Label("Пароль", 272, 300, bold: true);
        _password = UiTheme.TextBox(272, 328, 190);
        _password.UseSystemPasswordChar = true;
        _password.Text = "Admin123!";

        var showPassword = new CheckBox
        {
            Text = "показать пароль",
            Location = new Point(272, 362),
            AutoSize = true,
            ForeColor = UiTheme.Muted,
            Font = UiTheme.SmallFont
        };
        showPassword.CheckedChanged += (_, _) => _password.UseSystemPasswordChar = !showPassword.Checked;

        _loginButton = UiTheme.Button("Войти", UiTheme.Green, 56, 400, 150, 60, 12F);
        UiTheme.Flatten(_loginButton);
        _loginButton.Click += LoginButton_Click;

        var exitButton = UiTheme.Button("Выход", UiTheme.Red, 330, 400, 150, 60, 12F);
        UiTheme.Flatten(exitButton);
        exitButton.Click += (_, _) => Close();

        var hint = UiTheme.MutedLabel(
            "Демонстрационные учётные записи:\n" +
            "администратор — admin@remont.ru / Admin123!\n" +
            "менеджер — manager@remont.ru / Manager123!\n" +
            "клиент — client@remont.ru / Client123!",
            56, 476, 410);
        hint.Height = 64;

        var register = new Button
        {
            Text = "Регистрация клиента",
            Location = new Point(56, 552),
            Size = new Size(424, 34),
            FlatStyle = FlatStyle.Flat,
            BackColor = UiTheme.Background,
            ForeColor = Color.FromArgb(144, 202, 249),
            Font = new Font("Microsoft Sans Serif", 10F, FontStyle.Underline),
            Cursor = Cursors.Hand
        };
        register.FlatAppearance.BorderSize = 0;
        register.Click += (_, _) =>
        {
            using var form = new RegisterForm();
            if (form.ShowDialog(this) == DialogResult.OK)
            {
                _email.Text = form.RegisteredEmail;
                _password.Clear();
                _password.Focus();
            }
        };

        Controls.AddRange(new Control[]
        {
            logo, subtitle, emailLabel, _email, passwordLabel, _password, showPassword,
            _loginButton, exitButton, hint, register
        });

        AcceptButton = _loginButton;
        ActiveControl = _password;
    }

    private async void LoginButton_Click(object? sender, EventArgs e)
    {
        _loginButton.Enabled = false;
        try
        {
            var result = await AppServices.Auth.LoginAsync(_email.Text.Trim(), _password.Text);
            if (!result.Success || result.User is null)
            {
                UiTheme.Error(result.Error ?? "Ошибка авторизации.");
                _password.SelectAll();
                _password.Focus();
                return;
            }

            AppServices.CurrentUser = result.User;

            var menu = new MainMenuForm();
            Hide();

            menu.FormClosed += (_, _) =>
            {
                if (menu.LogoutRequested)
                {
                    AppServices.Logout();
                    _password.Clear();
                    Show();
                    _password.Focus();
                }
                else
                {
                    Close();
                }
            };

            menu.Show();
        }
        catch (Exception ex)
        {
            UiTheme.Error("Не удалось выполнить вход:\n" + ex.Message);
        }
        finally
        {
            _loginButton.Enabled = true;
        }
    }
}
