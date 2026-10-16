namespace Awaken.UI;

/// <summary>
/// 直至时刻选择对话框（仅当天，时/分两个数字框；小时输满 2 位自动跳到分钟，
/// 已过时刻由调用方拒绝）。
/// </summary>
internal sealed class TimeDialog : Form
{
    private readonly NumericUpDown _hour = new();
    private readonly NumericUpDown _minute = new();

    public TimeSpan TimeOfDay => new((int)_hour.Value, (int)_minute.Value, 0);

    public TimeDialog()
    {
        Text = "保持唤醒直至";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Dpi;
        ShowInTaskbar = false;
        Font = new Font(SystemFonts.MessageBoxFont.FontFamily, 11f);

        var label = new Label
        {
            Text = "时刻（仅限今天）：",
            AutoSize = true,
            Location = new Point(14, 16),
        };

        DateTime next = DateTime.Now.AddHours(1);
        _hour.Minimum = 0;
        _hour.Maximum = 23;
        _hour.Value = next.Hour;
        _hour.SetBounds(14, 48, 62, 30);
        _hour.TextAlign = HorizontalAlignment.Center;

        _minute.Minimum = 0;
        _minute.Maximum = 59;
        _minute.Value = next.Minute;
        _minute.SetBounds(120, 48, 62, 30);
        _minute.TextAlign = HorizontalAlignment.Center;

        var hourLabel = new Label { Text = "时", AutoSize = true, Location = new Point(82, 52) };
        var minuteLabel = new Label { Text = "分", AutoSize = true, Location = new Point(188, 52) };

        _hour.Enter += (_, _) => _hour.Select(0, _hour.Text.Length);
        _minute.Enter += (_, _) => _minute.Select(0, _minute.Text.Length);
        _hour.TextChanged += (_, _) =>
        {
            if (_hour.Focused && _hour.Text.Length == 2)
            {
                _minute.Focus();
            }
        };

        var ok = new Button { Text = "确定", DialogResult = DialogResult.OK };
        var cancel = new Button { Text = "取消", DialogResult = DialogResult.Cancel };
        ok.SetBounds(120, 100, 78, 32);
        cancel.SetBounds(206, 100, 78, 32);
        AcceptButton = ok;
        CancelButton = cancel;

        Controls.AddRange([label, _hour, hourLabel, _minute, minuteLabel, ok, cancel]);
        ClientSize = new Size(296, 144);
    }
}
