using RepairRequests.Core.Models;
using RepairRequests.WinForms.Infrastructure;

namespace RepairRequests.WinForms.Forms;

/// <summary>Главное меню (аналог Form2 в PcClab): крупные кнопки-разделы.</summary>
public class MainMenuForm : Form
{
    public bool LogoutRequested { get; private set; }

    private sealed record MenuEntry(string Title, string Image, Color Color, Action Open);

    public MainMenuForm()
    {
        Text = "Система заявок на ремонт — главное меню";
        ClientSize = new Size(980, 440);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        BackColor = UiTheme.Background;
        ForeColor = UiTheme.Text;
        Font = UiTheme.BaseFont;

        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 86,
            BackColor = Color.FromArgb(52, 52, 52)
        };

        var title = new Label
        {
            Text = "ГЛАВНОЕ МЕНЮ",
            Font = new Font("Microsoft Sans Serif", 22F, FontStyle.Bold),
            ForeColor = UiTheme.Text,
            AutoSize = true,
            Location = new Point(30, 18)
        };
        header.Controls.Add(title);

        var user = AppServices.CurrentUser!;
        var userLabel = new Label
        {
            Text = $"{user.FullName}\n{user.Role.Ru()}",
            Font = new Font("Microsoft Sans Serif", 10F),
            ForeColor = UiTheme.Muted,
            TextAlign = ContentAlignment.MiddleRight,
            AutoSize = false,
            Location = new Point(650, 16),
            Size = new Size(300, 54)
        };
        header.Controls.Add(userLabel);
        Controls.Add(header);

        var entries = BuildEntries();
        var itemWidth = 150;
        var gap = 34;
        var totalWidth = entries.Count * itemWidth + (entries.Count - 1) * gap;
        var startX = (ClientSize.Width - totalWidth) / 2;

        for (var i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            var x = startX + i * (itemWidth + gap);

            var picture = UiTheme.Picture(entry.Image, x, 120, itemWidth, 112);
            picture.Cursor = Cursors.Hand;
            picture.Click += (_, _) => entry.Open();

            var button = UiTheme.MenuButton(entry.Title, entry.Color, x, 246, itemWidth);
            button.Click += (_, _) => entry.Open();

            Controls.Add(picture);
            Controls.Add(button);
        }

        var footer = UiTheme.MutedLabel(
            "Учебный проект • C# WinForms • Entity Framework Core / SQLite\n" +
            $"База данных: {AppServices.DatabasePath}",
            30, 340, 920);
        footer.Height = 40;
        Controls.Add(footer);

        var about = UiTheme.MutedLabel("Сервисная станция «АвтоМастер» • версия 1.0", 30, 390, 600);
        Controls.Add(about);
    }

    private List<MenuEntry> BuildEntries()
    {
        var entries = new List<MenuEntry>
        {
            new(
                AppServices.IsManager ? "Заявки" : "Мои заявки",
                "menu_requests.png",
                Color.FromArgb(76, 175, 80),
                () => OpenDialog(new RequestsForm())),
            new(
                AppServices.IsManager ? "Автомобили" : "Мой транспорт",
                "menu_vehicles.png",
                Color.FromArgb(84, 110, 122),
                () => OpenDialog(new VehiclesForm()))
        };

        if (AppServices.IsManager)
        {
            entries.Add(new MenuEntry(
                "Неисправности",
                "menu_faults.png",
                Color.FromArgb(84, 110, 122),
                () => OpenDialog(new FaultsForm())));
        }

        entries.Add(new MenuEntry(
            "Отчёт",
            "menu_report.png",
            Color.FromArgb(84, 110, 122),
            () => OpenDialog(new ReportForm())));

        entries.Add(new MenuEntry(
            "Выход",
            "menu_exit.png",
            UiTheme.Red,
            () =>
            {
                LogoutRequested = true;
                Close();
            }));

        return entries;
    }

    private void OpenDialog(Form form)
    {
        using (form)
            form.ShowDialog(this);
    }
}
