using RepairRequests.WinForms.Infrastructure;

namespace RepairRequests.WinForms.Forms;

/// <summary>
/// Экран загрузки. Показывается сразу после старта, пока создаются папки,
/// открывается база данных и применяются миграции. Нужен, чтобы при медленном
/// первом запуске (облачная папка, антивирус) пользователь видел, что программа
/// работает, а не «не запускается».
/// </summary>
public class SplashForm : Form
{
    private readonly Label _status;
    private readonly Label _hint;

    public SplashForm()
    {
        Text = "Система заявок на ремонт";
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(560, 330);
        BackColor = UiTheme.Background;
        ForeColor = UiTheme.Text;
        Font = UiTheme.BaseFont;
        ShowInTaskbar = true;
        ControlBox = false;

        var logo = UiTheme.Picture("logo.png", 180, 18, 200, 150);

        var title = new Label
        {
            Text = "СИСТЕМА ЗАЯВОК НА РЕМОНТ",
            Font = new Font("Microsoft Sans Serif", 16F, FontStyle.Bold),
            ForeColor = UiTheme.Green,
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(20, 178),
            Size = new Size(520, 34)
        };

        var subtitle = new Label
        {
            Text = "учёт заявок и статистика времени ремонта",
            Font = UiTheme.SmallFont,
            ForeColor = UiTheme.Muted,
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(20, 212),
            Size = new Size(520, 22)
        };

        _status = new Label
        {
            Text = "Подготовка к запуску…",
            Font = new Font("Microsoft Sans Serif", 11F),
            ForeColor = UiTheme.Text,
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(20, 252),
            Size = new Size(520, 26)
        };

        _hint = new Label
        {
            Text = "Первый запуск может занять до 30 секунд — не закрывайте окно",
            Font = UiTheme.SmallFont,
            ForeColor = UiTheme.Muted,
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(20, 292),
            Size = new Size(520, 22)
        };

        Controls.AddRange(new Control[] { logo, title, subtitle, _status, _hint });

        Paint += (_, e) =>
        {
            using var pen = new Pen(Color.FromArgb(110, 110, 110));
            e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        };
    }

    /// <summary>Обновляет подпись и сразу перерисовывает окно.</summary>
    public void SetStatus(string text)
    {
        if (IsDisposed)
            return;

        _status.Text = text;
        _status.Refresh();
        Application.DoEvents();
    }

    /// <summary>Показывает подсказку о длительном первом запуске только при необходимости.</summary>
    public void ShowSlowHint(bool visible) => _hint.Visible = visible;
}
