using Awaken.Config;
using Awaken.Core;
using Awaken.Integration;

namespace Awaken.UI;

/// <summary>
/// 托盘宿主：右键菜单切换唤醒模式（含定时模式），图标与提示随状态变化，
/// 选择持久化到便携配置。
/// </summary>
internal sealed class TrayApplicationContext : ApplicationContext
{
    private const int TickMs = 30_000;

    private readonly AwakeManager _manager = new();
    private readonly NotifyIcon _trayIcon;
    private readonly ToolStripMenuItem _offItem = new("关闭");
    private readonly ToolStripMenuItem _keepAwakeItem = new("保持唤醒");
    private readonly ToolStripMenuItem _keepDisplayItem = new("保持唤醒，屏幕常亮");
    private readonly ToolStripMenuItem _timedItem = new("定时保持唤醒");
    private readonly ToolStripMenuItem _timed30 = new("30 分钟");
    private readonly ToolStripMenuItem _timed60 = new("1 小时");
    private readonly ToolStripMenuItem _timed120 = new("2 小时");
    private readonly ToolStripMenuItem _timedCustom = new("自定义...");
    private readonly ToolStripMenuItem _autoStartItem = new("开机自启");
    private readonly System.Windows.Forms.Timer _tickTimer = new() { Interval = TickMs };
    private readonly AppConfig _config;

    private AwakeMode _mode;
    private ToolStripMenuItem? _timedChosen;
    private DateTime _timedExpiry;

    public TrayApplicationContext()
    {
        _config = ConfigStore.Load();

        ToolStripMenuItem[] timedMenu = [_timed30, _timed60, _timed120, _timedCustom];
        _timedItem.DropDown.Items.AddRange(timedMenu);
        _timed30.Click += (_, _) => StartTimed(TimeSpan.FromMinutes(30), _timed30);
        _timed60.Click += (_, _) => StartTimed(TimeSpan.FromMinutes(60), _timed60);
        _timed120.Click += (_, _) => StartTimed(TimeSpan.FromMinutes(120), _timed120);
        _timedCustom.Click += (_, _) => StartTimedCustom();

        var menu = new ContextMenuStrip();
        menu.Items.AddRange([_offItem, _keepAwakeItem, _keepDisplayItem]);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_timedItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_autoStartItem);
        menu.Items.Add(new ToolStripMenuItem("退出", null, (_, _) => Exit()));

        _offItem.Click += (_, _) => SetMode(AwakeMode.Off);
        _keepAwakeItem.Click += (_, _) => SetMode(AwakeMode.KeepAwake);
        _keepDisplayItem.Click += (_, _) => SetMode(AwakeMode.KeepDisplayOn);

        _tickTimer.Tick += OnTick;

        _autoStartItem.Click += (_, _) =>
        {
            if (AutoStart.IsEnabled())
            {
                AutoStart.Disable();
            }
            else
            {
                AutoStart.Enable();
            }

            _autoStartItem.Checked = AutoStart.IsEnabled();
        };

        AutoStart.SelfHeal();
        _autoStartItem.Checked = AutoStart.IsEnabled();

        _trayIcon = new NotifyIcon
        {
            ContextMenuStrip = menu,
            Visible = true,
        };
        SetMode(_config.Mode);
    }

    private void SetMode(AwakeMode mode)
    {
        _timedChosen = null;
        _timedItem.Checked = false;
        _tickTimer.Stop();
        ApplyMode(mode);
        UpdateChecks();
    }

    /// <summary>只更新引擎、图标、配置与基础提示，不触碰定时状态。</summary>
    private void ApplyMode(AwakeMode mode)
    {
        _mode = mode;
        _manager.SetMode(mode);
        _config.Mode = mode;
        ConfigStore.Save(_config);
        _trayIcon.Icon = StateIcons.For(mode);
        _trayIcon.Text = TooltipFor(mode);
    }

    private void StartTimed(TimeSpan duration, ToolStripMenuItem chosen)
    {
        _timedExpiry = DateTime.UtcNow + duration;
        _timedChosen = chosen;
        _manager.SetMode(AwakeMode.KeepAwake);
        _trayIcon.Icon = StateIcons.Timed();
        UpdateTimedTooltip();
        _tickTimer.Start();
        UpdateChecks();
    }

    private void StartTimedCustom()
    {
        using var dialog = new TimedDialog(_config.LastTimedMinutes);
        if (dialog.ShowDialog() != DialogResult.OK)
        {
            return;
        }

        _config.LastTimedMinutes = dialog.Minutes;
        ConfigStore.Save(_config);
        StartTimed(TimeSpan.FromMinutes(dialog.Minutes), _timedCustom);
    }

    private void OnTick(object? sender, EventArgs e)
    {
        if (_timedChosen is null)
        {
            return;
        }

        TimeSpan remaining = _timedExpiry - DateTime.UtcNow;
        if (remaining <= TimeSpan.Zero)
        {
            SetMode(AwakeMode.Off);
            return;
        }

        UpdateTimedTooltip();
    }

    private void UpdateTimedTooltip()
    {
        TimeSpan remaining = _timedExpiry - DateTime.UtcNow;
        string text = $"Awaken - 定时保持唤醒（剩 {FormatRemaining(remaining)}）";
        if (text.Length > 63)
        {
            text = text[..63];
        }

        _trayIcon.Text = text;
    }

    private static string FormatRemaining(TimeSpan remaining) => remaining.TotalHours >= 1
        ? $"{(int)remaining.TotalHours} 小时 {remaining.Minutes} 分"
        : $"{(int)Math.Ceiling(remaining.TotalMinutes)} 分钟";

    private void UpdateChecks()
    {
        bool timed = _timedChosen is not null;
        _offItem.Checked = !timed && _mode == AwakeMode.Off;
        _keepAwakeItem.Checked = !timed && _mode == AwakeMode.KeepAwake;
        _keepDisplayItem.Checked = !timed && _mode == AwakeMode.KeepDisplayOn;
        _timedItem.Checked = timed;
        _timed30.Checked = _timedChosen == _timed30;
        _timed60.Checked = _timedChosen == _timed60;
        _timed120.Checked = _timedChosen == _timed120;
        _timedCustom.Checked = _timedChosen == _timedCustom;
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
            _tickTimer.Stop();
            _tickTimer.Dispose();
            _trayIcon.Dispose();
            _manager.Dispose();
            StateIcons.DestroyAll();
        }

        base.Dispose(disposing);
    }
}
