namespace Awaken.UI;

/// <summary>
/// 直至时刻选择对话框（仅 HH:mm，无日期；当天该时刻已过则视为次日）。
/// </summary>
internal sealed class TimeDialog : Form
{
    private readonly DateTimePicker _picker = new();

    public TimeSpan TimeOfDay => _picker.Value.TimeOfDay;

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
            Text = "时刻（当天已过则顺延至次日）：",
            AutoSize = true,
            Location = new Point(14, 16),
        };

        _picker.Format = DateTimePickerFormat.Custom;
        _picker.CustomFormat = "HH:mm";
        _picker.ShowUpDown = true;
        _picker.Value = DateTime.Now.AddHours(1);
        _picker.SetBounds(14, 48, 130, 30);

        var ok = new Button { Text = "确定", DialogResult = DialogResult.OK };
        var cancel = new Button { Text = "取消", DialogResult = DialogResult.Cancel };
        ok.SetBounds(120, 96, 78, 32);
        cancel.SetBounds(206, 96, 78, 32);
        AcceptButton = ok;
        CancelButton = cancel;

        Controls.AddRange([label, _picker, ok, cancel]);
        ClientSize = new Size(296, 140);
    }
}
