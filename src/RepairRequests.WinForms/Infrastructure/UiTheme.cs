using System.Drawing.Drawing2D;

namespace RepairRequests.WinForms.Infrastructure;

/// <summary>Единое оформление форм в стиле учебного приложения PcClab.</summary>
public static class UiTheme
{
    public static readonly Color Background = Color.FromArgb(64, 64, 64);
    public static readonly Color Panel = Color.FromArgb(76, 76, 76);
    public static readonly Color Text = Color.FromArgb(240, 240, 240);
    public static readonly Color Muted = Color.FromArgb(190, 195, 200);
    public static readonly Color Green = Color.FromArgb(76, 175, 80);
    public static readonly Color Red = Color.FromArgb(205, 92, 92);
    public static readonly Color Neutral = Color.FromArgb(112, 128, 144);
    public static readonly Color InputBack = Color.FromArgb(232, 232, 232);
    public static readonly Color InputText = Color.FromArgb(30, 30, 30);
    public static readonly Color Header = Color.FromArgb(31, 58, 95);

    public static readonly Font BaseFont = new("Microsoft Sans Serif", 11F);
    public static readonly Font SmallFont = new("Microsoft Sans Serif", 9F);
    public static readonly Font TitleFont = new("Microsoft Sans Serif", 22F, FontStyle.Bold);
    public static readonly Font SubtitleFont = new("Microsoft Sans Serif", 14F, FontStyle.Bold);

    public static Label Title(string text, int x, int y, int width = 0) => new()
    {
        Text = text,
        Font = TitleFont,
        ForeColor = Text,
        AutoSize = width <= 0,
        Location = new Point(x, y),
        Width = width,
        Height = 42
    };

    public static Label Label(string text, int x, int y, int width = 0, bool bold = false, float size = 11F) => new()
    {
        Text = text,
        Font = bold ? new Font("Microsoft Sans Serif", size, FontStyle.Bold) : new Font("Microsoft Sans Serif", size),
        ForeColor = Text,
        AutoSize = width <= 0,
        Location = new Point(x, y),
        Width = width,
        Height = 24
    };

    public static Label MutedLabel(string text, int x, int y, int width = 0) => new()
    {
        Text = text,
        Font = SmallFont,
        ForeColor = Muted,
        AutoSize = width <= 0,
        Location = new Point(x, y),
        Width = width,
        Height = 20
    };

    public static TextBox TextBox(int x, int y, int width, bool multiline = false, int height = 30) => new()
    {
        Location = new Point(x, y),
        Width = width,
        Height = multiline ? height : 30,
        Multiline = multiline,
        Font = BaseFont,
        BackColor = InputBack,
        ForeColor = InputText,
        BorderStyle = BorderStyle.FixedSingle,
        ScrollBars = multiline ? ScrollBars.Vertical : ScrollBars.None
    };

    public static ComboBox Combo(int x, int y, int width) => new()
    {
        Location = new Point(x, y),
        Width = width,
        Font = BaseFont,
        DropDownStyle = ComboBoxStyle.DropDownList,
        BackColor = InputBack,
        ForeColor = InputText,
        FlatStyle = FlatStyle.Flat
    };

    public static NumericUpDown Number(int x, int y, int width, decimal max = 10_000_000, int decimals = 2,
        bool thousandsSeparator = true) => new()
    {
        Location = new Point(x, y),
        Width = width,
        Font = BaseFont,
        Maximum = max,
        Minimum = 0,
        DecimalPlaces = decimals,
        ThousandsSeparator = thousandsSeparator,
        BackColor = InputBack,
        ForeColor = InputText,
        BorderStyle = BorderStyle.FixedSingle,
        TextAlign = HorizontalAlignment.Right
    };

    public static DateTimePicker Date(int x, int y, int width) => new()
    {
        Location = new Point(x, y),
        Width = width,
        Font = BaseFont,
        Format = DateTimePickerFormat.Short,
        CalendarForeColor = Color.Black,
        CalendarMonthBackground = Color.White
    };

    public static Button Button(string text, Color back, int x, int y, int width = 128, int height = 45, float size = 11F) => new()
    {
        Text = text,
        Location = new Point(x, y),
        Size = new Size(width, height),
        BackColor = back,
        ForeColor = Color.White,
        Font = new Font("Microsoft Sans Serif", size, FontStyle.Bold),
        FlatStyle = FlatStyle.Flat,
        Cursor = Cursors.Hand,
        UseVisualStyleBackColor = false
    };

    public static void Flatten(Button button)
    {
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = ControlPaint.Light(button.BackColor, 0.1f);
        button.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(button.BackColor, 0.1f);
    }

    public static Button MenuButton(string text, Color back, int x, int y, int width = 150)
    {
        var button = Button(text, back, x, y, width, 44, 12F);
        Flatten(button);
        return button;
    }

    public static PictureBox Picture(string fileName, int x, int y, int width = 150, int height = 110)
    {
        var box = new PictureBox
        {
            Location = new Point(x, y),
            Size = new Size(width, height),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Transparent
        };

        try
        {
            var path = AppPaths.Resource(fileName);
            if (File.Exists(path))
            {
                // Загружаем через копию в памяти: Image.FromFile удерживает файл
                // открытым, из-за чего повторная загрузка того же рисунка
                // (например, на экране загрузки и форме входа) может не сработать.
                using var stream = new MemoryStream(File.ReadAllBytes(path));
                using var loaded = Image.FromStream(stream);
                box.Image = new Bitmap(loaded);
            }
        }
        catch
        {
            // Если ресурс недоступен, картинка просто не отображается.
        }

        return box;
    }

    public static DataGridView Grid(int x, int y, int width, int height)
    {
        var grid = new DataGridView
        {
            Location = new Point(x, y),
            Size = new Size(width, height),
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            ReadOnly = true,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            Font = new Font("Microsoft Sans Serif", 10F),
            GridColor = Color.FromArgb(220, 225, 230),
            EnableHeadersVisualStyles = false,
            ColumnHeadersHeight = 36,
            RowTemplate = { Height = 30 }
        };

        grid.ColumnHeadersDefaultCellStyle.BackColor = Header;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        grid.ColumnHeadersDefaultCellStyle.Font = new Font("Microsoft Sans Serif", 10F, FontStyle.Bold);
        grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
        grid.DefaultCellStyle.BackColor = Color.White;
        grid.DefaultCellStyle.ForeColor = Color.FromArgb(30, 30, 30);
        grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(197, 225, 202);
        grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(20, 20, 20);
        grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(247, 249, 251);

        return grid;
    }

    public static Panel Separator(int x, int y, int width) => new()
    {
        Location = new Point(x, y),
        Size = new Size(width, 1),
        BackColor = Color.FromArgb(110, 110, 110)
    };

    /// <summary>Активирует первую строку таблицы, чтобы кнопки работы с записью стали доступны.</summary>
    public static void SelectFirstRow(DataGridView grid)
    {
        if (grid.Rows.Count == 0)
            return;

        var cell = grid.Rows[0].Cells.Cast<DataGridViewCell>().FirstOrDefault(c => c.Visible);
        if (cell is not null)
            grid.CurrentCell = cell;
    }

    public static void Error(string message) =>
        MessageBox.Show(message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);

    public static void Info(string message) =>
        MessageBox.Show(message, "Сообщение", MessageBoxButtons.OK, MessageBoxIcon.Information);

    public static bool Confirm(string message) =>
        MessageBox.Show(message, "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
        == DialogResult.Yes;

    public static Color StatusColor(string status) => status switch
    {
        "Новая" => Color.FromArgb(33, 150, 243),
        "Принята" => Color.FromArgb(255, 152, 0),
        "В работе" => Color.FromArgb(156, 39, 176),
        "Выполнена" => Color.FromArgb(76, 175, 80),
        "Закрыта" => Color.FromArgb(96, 125, 139),
        "Отменена" => Color.FromArgb(211, 47, 47),
        _ => Color.Gray
    };
}
