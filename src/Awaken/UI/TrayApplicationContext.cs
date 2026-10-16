using Awaken.Config;
using Awaken.Core;
using Awaken.Integration;

namespace Awaken.UI;

/// <summary>
/// 托盘宿主：唤醒模式（关闭/保持唤醒）+ 快捷定时项（一级菜单）+ 直至时刻 +
/// 独立的"屏幕常亮"叠加开关，图标与提示随状态变化，选择持久化到便携配置。
/// </summary>
internal sealed class TrayApplicationContext : ApplicationContext
{
    private const int TickMs = 30_000;

    private readonly AwakeManager _manager = new();
    private readonly NotifyIcon _trayIcon;
    private readonly System.Windows.Forms.Timer _tickTimer = new() { Interval = TickMs };
    private readonly AppConfig _config;

    private ContextMenuStrip _menu = null!;
    private ToolStripMenuItem _offItem = null!;
    private ToolStripMenuItem _keepAwakeItem = null!;
    private ToolStripMenuItem _timedUntilItem = null!;
    private ToolStripMenuItem _displayItem = null!;
    private ToolStripMenuItem _autoStartItem = null!;
    private readonly List<ToolStripMenuItem> _rootTimedItems = [];

    private enum TimedKind
    {
        None,
        Preset,
        Until,
    }

    private AwakeMode _mode;
    private bool _timedActive;
    private TimedKind _timedKind = TimedKind.None;
    private int? _presetMinutes;
    private DateTime _timedExpiry;

    public TrayApplicationContext()
    {
        _config = ConfigStore.Load();
        MigrateLegacyMode();

        BuildMenu();

        _tickTimer.Tick += OnTick;

        _trayIcon = new NotifyIcon
        {
            ContextMenuStrip = _menu,
            Visible = true,
        };
        SetMode(_config.Mode);
    }

    /// <summary>旧配置 Mode == KeepDisplayOn 迁移为 KeepAwake + KeepDisplayOn 开关。</summary>
    private void MigrateLegacyMode()
    {
        if (_config.Mode == AwakeMode.KeepDisplayOn)
        {
            _config.Mode = AwakeMode.KeepAwake;
            _config.KeepDisplayOn = true;
        }
    }

    private void BuildMenu()
    {
        _rootTimedItems.Clear();

        _offItem = new ToolStripMenuItem("关闭");
        _keepAwakeItem = new ToolStripMenuItem("保持唤醒");
        _timedUntilItem = new ToolStripMenuItem("保持唤醒直至...");
        _displayItem = new ToolStripMenuItem("屏幕常亮");
        _autoStartItem = new ToolStripMenuItem("开机自启");

        _offItem.Click += (_, _) => SetMode(AwakeMode.Off);
        _keepAwakeItem.Click += (_, _) => SetMode(AwakeMode.KeepAwake);
        _timedUntilItem.Click += (_, _) => StartUntilFromDialog();
        _displayItem.Click += (_, _) => ToggleDisplay();
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

        var menu = new ContextMenuStrip();
        menu.Items.AddRange([_offItem, _keepAwakeItem]);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_timedUntilItem);
        menu.Items.Add(new ToolStripSeparator());

        // 一级菜单快捷定时项，默认 30 分钟 / 1 小时 / 2 小时，可在设置中调整。
        foreach (int minutes in _config.RootTimedMinutes.Order())
        {
            var item = new ToolStripMenuItem($"保持唤醒 {DurationFormat.For(minutes)}") { Tag = minutes };
            item.Click += (_, _) => StartTimed(minutes);
            _rootTimedItems.Add(item);
            menu.Items.Add(item);
        }

        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_displayItem);
        menu.Items.Add(_autoStartItem);
        menu.Items.Add(new ToolStripMenuItem("设置...", null, (_, _) => ShowSettings()));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(new ToolStripMenuItem("退出", null, (_, _) => Exit()));

        _autoStartItem.Checked = AutoStart.IsEnabled();

        ContextMenuStrip? old = _menu;
        _menu = menu;
        if (_trayIcon is not null)
        {
            _trayIcon.ContextMenuStrip = menu;
        }

        old?.Dispose();
    }

    private void SetMode(AwakeMode mode)
    {
        _timedActive = false;
        _timedKind = TimedKind.None;
        _presetMinutes = null;
        _timedUntilItem.Text = "保持唤醒直至...";
        _tickTimer.Stop();
        _mode = mode;
        _config.Mode = mode;
        ConfigStore.Save(_config);
        UpdateEngine();
        UpdateVisuals();
    }

    private void StartTimed(int minutes)
    {
        _timedKind = TimedKind.Preset;
        _presetMinutes = minutes;
        _timedExpiry = DateTime.UtcNow + TimeSpan.FromMinutes(minutes);
        StartTimedCore();
    }

    private void StartUntilFromDialog()
    {
        using var dialog = new TimeDialog();
        if (dialog.ShowDialog() != DialogResult.OK)
        {
            return;
        }

        DateTime now = DateTime.Now;
        DateTime target = now.Date + dialog.TimeOfDay;
        if (target <= now)
        {
            MessageBox.Show("所选时刻今天已过，请选择之后的时刻。", "Awaken", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _timedKind = TimedKind.Until;
        _presetMinutes = null;
        _timedExpiry = target.ToUniversalTime();
        StartTimedCore();
    }

    /// <summary>定时类激活的公共收尾：切换引擎、启动倒计时、刷新界面。</summary>
    private void StartTimedCore()
    {
        _timedActive = true;
        // 定时属于临时状态，不回写配置：异常退出后重启按上次稳定模式恢复。
        _mode = AwakeMode.KeepAwake;
        UpdateEngine();
        _tickTimer.Start();
        UpdateVisuals();
    }

    private void ToggleDisplay()
    {
        _config.KeepDisplayOn = !_config.KeepDisplayOn;
        ConfigStore.Save(_config);
        UpdateEngine();
        UpdateVisuals();
    }

    private void ShowSettings()
    {
        using var dialog = new SettingsDialog(_config.RootTimedMinutes);
        if (dialog.ShowDialog() != DialogResult.OK)
        {
            return;
        }

        _config.RootTimedMinutes = [.. dialog.RootTimedMinutes];
        ConfigStore.Save(_config);
        BuildMenu();
        UpdateVisuals();
    }

    private void UpdateEngine() => _manager.SetState(_mode, _config.KeepDisplayOn);

    private void OnTick(object? sender, EventArgs e)
    {
        if (!_timedActive)
        {
            return;
        }

        if (_timedExpiry - DateTime.UtcNow <= TimeSpan.Zero)
        {
            SetMode(AwakeMode.Off);
            return;
        }

        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        bool active = _mode != AwakeMode.Off;
        _trayIcon.Icon = StateIcons.For(active, _config.KeepDisplayOn, _timedActive);
        _trayIcon.Text = ComposeTooltip();
        _offItem.Checked = !_timedActive && _mode == AwakeMode.Off;
        _keepAwakeItem.Checked = !_timedActive && _mode == AwakeMode.KeepAwake;
        _timedUntilItem.Checked = _timedKind == TimedKind.Until;
        _timedUntilItem.Text = _timedKind == TimedKind.Until
            ? $"保持唤醒直至 {_timedExpiry.ToLocalTime():HH:mm}"
            : "保持唤醒直至...";
        foreach (ToolStripMenuItem item in _rootTimedItems)
        {
            item.Checked = _timedKind == TimedKind.Preset && _presetMinutes == (int)item.Tag!;
        }

        _displayItem.Checked = _config.KeepDisplayOn;
    }

    private string ComposeTooltip()
    {
        string core;
        if (_mode == AwakeMode.Off)
        {
            core = "未激活";
        }
        else if (_timedActive)
        {
            string until = _timedKind == TimedKind.Until
                ? $"至 {_timedExpiry.ToLocalTime():HH:mm}"
                : string.Empty;
            core = $"定时保持唤醒{until}（剩 {DurationFormat.For(_timedExpiry - DateTime.UtcNow)}）";
        }
        else
        {
            core = "保持唤醒";
        }

        string display = _config.KeepDisplayOn && _mode != AwakeMode.Off ? "，屏幕常亮" : string.Empty;
        string text = $"Awaken - {core}{display}";
        return text.Length > 63 ? text[..63] : text;
    }

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
