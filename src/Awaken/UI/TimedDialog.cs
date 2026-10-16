namespace Awaken.UI;

/// <summary>
/// 自定义定时时长输入对话框（分钟）。
/// </summary>
internal sealed class TimedDialog : Form
{
    private readonly NumericUpDown _minutes = new();

    public int Minutes => (int)_minutes.Value;

    public TimedDialog(int defaultMinutes)
    {
        Text = "定时保持唤醒";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Dpi;
        ShowInTaskbar = false;

        var label = new Label
        {
            Text = "持续时间（分钟）：",
            AutoSize = true,
            Location = new Point(12, 17),
        };

        _minutes.Minimum = 1;
        _minutes.Maximum = 24 * 60;
        _minutes.Value = Math.Clamp(defaultMinutes, 1, 24 * 60);
        _minutes.Location = new Point(135, 14);
        _minutes.Width = 72;

        var ok = new Button { Text = "确定", DialogResult = DialogResult.OK };
        var cancel = new Button { Text = "取消", DialogResult = DialogResult.Cancel };
        ok.SetBounds(45, 48, 80, 28);
        cancel.SetBounds(135, 48, 80, 28);

        AcceptButton = ok;
        CancelButton = cancel;

        Controls.AddRange([label, _minutes, ok, cancel]);
        ClientSize = new Size(260, 88);
    }
}
