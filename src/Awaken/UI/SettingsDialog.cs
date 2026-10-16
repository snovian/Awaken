namespace Awaken.UI;

/// <summary>
/// 设置对话框：管理显示在托盘一级菜单的快捷定时项。
/// </summary>
internal sealed class SettingsDialog : Form
{
    private readonly ListBox _list = new() { Sorted = true };
    private readonly NumericUpDown _minutes = new();

    /// <summary>确认后的快捷定时项（分钟），已排序去重。</summary>
    public IReadOnlyList<int> RootTimedMinutes =>
        _list.Items.Cast<int>().Order().ToList();

    public SettingsDialog(IEnumerable<int> current)
    {
        Text = "设置";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Dpi;
        ShowInTaskbar = false;

        var label = new Label
        {
            Text = "一级菜单快捷定时项（分钟）：",
            AutoSize = true,
            Location = new Point(12, 15),
        };

        _list.SelectionMode = SelectionMode.One;
        _list.SetBounds(12, 38, 250, 110);
        foreach (int minutes in current.Order())
        {
            _list.Items.Add(minutes);
        }

        _minutes.Minimum = 1;
        _minutes.Maximum = 24 * 60;
        _minutes.SetBounds(12, 160, 70, 27);

        var addButton = new Button { Text = "添加" };
        var removeButton = new Button { Text = "删除" };
        addButton.SetBounds(92, 158, 75, 28);
        removeButton.SetBounds(175, 158, 75, 28);
        addButton.Click += (_, _) =>
        {
            int value = (int)_minutes.Value;
            if (!_list.Items.Contains(value))
            {
                _list.Items.Add(value);
            }
        };
        removeButton.Click += (_, _) =>
        {
            if (_list.SelectedIndex >= 0)
            {
                _list.Items.RemoveAt(_list.SelectedIndex);
            }
        };

        var ok = new Button { Text = "确定", DialogResult = DialogResult.OK };
        var cancel = new Button { Text = "取消", DialogResult = DialogResult.Cancel };
        ok.SetBounds(92, 200, 75, 28);
        cancel.SetBounds(175, 200, 75, 28);
        AcceptButton = ok;
        CancelButton = cancel;

        Controls.AddRange([label, _list, _minutes, addButton, removeButton, ok, cancel]);
        ClientSize = new Size(280, 240);
    }
}
