using Awaken.Core;

namespace Awaken.UI;

/// <summary>
/// 托盘宿主：右键菜单切换唤醒模式，图标与提示文字随模式变化。
/// </summary>
internal sealed class TrayApplicationContext : ApplicationContext
{
    private readonly AwakeManager _manager = new();
    private readonly NotifyIcon _trayIcon;
    private readonly ToolStripMenuItem _offItem = new("关闭");
    private readonly ToolStripMenuItem _keepAwakeItem = new("保持唤醒");
    private readonly ToolStripMenuItem _keepDisplayItem = new("保持唤醒，屏幕常亮");
    private AwakeMode _mode;

    public TrayApplicationContext()
    {
        var menu = new ContextMenuStrip();
        menu.Items.AddRange([_offItem, _keepAwakeItem, _keepDisplayItem]);
        menu.Items.Add(new ToolStripSeparator());
        var exitItem = new ToolStripMenuItem("退出", null, (_, _) => Exit());
        menu.Items.Add(exitItem);

        _offItem.Click += (_, _) => SetMode(AwakeMode.Off);
        _keepAwakeItem.Click += (_, _) => SetMode(AwakeMode.KeepAwake);
        _keepDisplayItem.Click += (_, _) => SetMode(AwakeMode.KeepDisplayOn);

        _trayIcon = new NotifyIcon
        {
            ContextMenuStrip = menu,
            Visible = true,
        };
        SetMode(AwakeMode.KeepAwake);
    }

    private void SetMode(AwakeMode mode)
    {
        _mode = mode;
        _manager.SetMode(mode);
        _trayIcon.Icon = StateIcons.For(mode);
        _trayIcon.Text = TooltipFor(mode);
        _offItem.Checked = mode == AwakeMode.Off;
        _keepAwakeItem.Checked = mode == AwakeMode.KeepAwake;
        _keepDisplayItem.Checked = mode == AwakeMode.KeepDisplayOn;
    }

    private static string TooltipFor(AwakeMode mode) => mode switch
    {
        AwakeMode.KeepAwake => "Awaken - 保持唤醒",
        AwakeMode.KeepDisplayOn => "Awaken - 保持唤醒，屏幕常亮",
        _ => "Awaken - 未激活",
    };

    private void Exit()
    {
        SetMode(AwakeMode.Off);
        _trayIcon.Visible = false;
        ExitThread();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _trayIcon.Dispose();
            _manager.Dispose();
            StateIcons.DestroyAll();
        }

        base.Dispose(disposing);
    }
}
