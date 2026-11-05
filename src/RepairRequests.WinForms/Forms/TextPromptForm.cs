using RepairRequests.WinForms.Infrastructure;

namespace RepairRequests.WinForms.Forms;

/// <summary>Небольшой диалог ввода одной строки (причина отмены, комментарий).</summary>
public class TextPromptForm : Form
{
    private readonly TextBox _text;

    public string Value => _text.Text.Trim();

    public TextPromptForm(string title, string prompt, bool multiline = true)
    {
        Text = title;
        ClientSize = new Size(480, multiline ? 260 : 190);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = UiTheme.Background;
        ForeColor = UiTheme.Text;
        Font = UiTheme.BaseFont;

        var label = UiTheme.Label(prompt, 20, 18, 440);
        label.Height = 44;
        Controls.Add(label);

        _text = UiTheme.TextBox(20, 66, 440, multiline, multiline ? 110 : 30);
        Controls.Add(_text);

        var ok = UiTheme.Button("ОК", UiTheme.Green, 20, ClientSize.Height - 66, 200, 46);
        UiTheme.Flatten(ok);
        ok.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(_text.Text))
            {
                UiTheme.Error("Введите значение.");
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        };
        Controls.Add(ok);

        var cancel = UiTheme.Button("Отмена", UiTheme.Red, 240, ClientSize.Height - 66, 200, 46);
        UiTheme.Flatten(cancel);
        cancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        Controls.Add(cancel);

        AcceptButton = ok;
        CancelButton = cancel;
        Shown += (_, _) => _text.Focus();
    }
}
