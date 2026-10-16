namespace Awaken.UI;

/// <summary>
/// 设置对话框：勾选常用时长（30 分钟～12 小时），勾选项显示在托盘一级菜单。
/// </summary>
internal sealed class SettingsDialog : Form
{
    private static readonly (int Minutes, string Label)[] Presets =
    [
        (30, "30 分钟"),
        .. Enumerable.Range(1, 12).Select(h => (h * 60, DurationFormat.For(h * 60))),
    ];

    private readonly CheckedListBox _list = new();

    /// <summary>勾选的快捷定时项（分钟），按预设顺序。</summary>
    public IReadOnlyList<int> RootTimedMinutes =>
        Presets.Where((_, i) => _list.GetItemChecked(i)).Select(p => p.Minutes).ToList();

    public SettingsDialog(IEnumerable<int> current)
    {
        Text = "设置";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Dpi;
        ShowInTaskbar = false;
        Font = new Font(SystemFonts.MessageBoxFont.FontFamily, 11f);

        var label = new Label
        {
            Text = "勾选显示在一级菜单的快捷定时项：",
            AutoSize = true,
            Location = new Point(14, 16),
        };

        _list.CheckOnClick = true;
        _list.SetBounds(14, 48, 330, 340);
        HashSet<int> currentSet = [.. current];
        foreach ((int minutes, string text) in Presets)
        {
            _list.Items.Add(text, currentSet.Contains(minutes));
        }

        var ok = new Button { Text = "确定", DialogResult = DialogResult.OK };
        var cancel = new Button { Text = "取消", DialogResult = DialogResult.Cancel };
        ok.SetBounds(180, 400, 78, 32);
        cancel.SetBounds(266, 400, 78, 32);
        AcceptButton = ok;
        CancelButton = cancel;

        Controls.AddRange([label, _list, ok, cancel]);
        ClientSize = new Size(356, 444);
    }
}
