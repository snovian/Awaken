using Awaken.Config;
using Awaken.Core;
using Awaken.Integration;

namespace Awaken.UI;

/// <summary>
/// 托盘宿主：唤醒模式（关闭/保持唤醒/定时）+ 独立的"屏幕常亮"叠加开关，
/// 图标与提示随状态变化，选择持久化到便携配置。
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
    private ToolStripMenuItem _timedItem = null!;
    private ToolStripMenuItem _timed30 = null!;
    private ToolStripMenuItem _timed60 = null!;
    private ToolStripMenuItem _timed120 = null!;
    private ToolStripMenuItem _timedCustom = null!;
    private ToolStripMenuItem _displayItem = null!;
    private ToolStripMenuItem _autoStartItem = null!;

    private AwakeMode _mode;
    private bool _timedActive;
    private int? _timedChosenMinutes;
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
        _offItem = new ToolStripMenuItem("关闭");
        _keepAwakeItem = new ToolStripMenuItem("保持唤醒");
        _timedItem = new ToolStripMenuItem("定时保持唤醒");
        _timed30 = new ToolStripMenuItem("30 分钟") { Tag = 30 };
        _timed60 = new ToolStripMenuItem("1 小时") { Tag = 60 };
        _timed120 = new ToolStripMenuItem("2 小时") { Tag = 120 };
        _timedCustom = new ToolStripMenuItem("自定义...");
        _displayItem = new ToolStripMenuItem("屏幕常亮");
        _autoStartItem = new ToolStripMenuItem("开机自启");

        _offItem.Click += (_, _) => SetMode(AwakeMode.Off);
        _keepAwakeItem.Click += (_, _) => SetMode(AwakeMode.KeepAwake);
        _timed30.Click += (_, _) => StartTimed(30);
        _timed60.Click += (_, _) => StartTimed(60);
        _timed120.Click += (_, _) => StartTimed(120);
        _timedCustom.Click += (_, _) => StartTimedCustom();
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

        _timedItem.DropDown.Items.AddRange([_timed30, _timed60, _timed120, _timedCustom]);

        var menu = new ContextMenuStrip();
        menu.Items.AddRange([_offItem, _keepAwakeItem]);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_timedItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_displayItem);
        menu.Items.Add(_autoStartItem);
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
        _timedChosenMinutes = null;
        _tickTimer.Stop();
        _mode = mode;
        _config.Mode = mode;
        ConfigStore.Save(_config);
        UpdateEngine();
        UpdateVisuals();
    }

    private void StartTimed(int minutes, bool isCustom = false)
    {
        _timedActive = true;
        _timedChosenMinutes = isCustom ? null : minutes;
        _timedExpiry = DateTime.UtcNow + TimeSpan.FromMinutes(minutes);
        // 定时属于临时状态，不回写配置：异常退出后重启按上次稳定模式恢复。
        _mode = AwakeMode.KeepAwake;
        UpdateEngine();
        _tickTimer.Start();
        UpdateVisuals();
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
        StartTimed(dialog.Minutes, isCustom: true);
    }

    private void ToggleDisplay()
    {
        _config.KeepDisplayOn = !_config.KeepDisplayOn;
        ConfigStore.Save(_config);
        UpdateEngine();
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
        _timedItem.Checked = _timedActive;
        _timed30.Checked = _timedActive && _timedChosenMinutes == 30;
        _timed60.Checked = _timedActive && _timedChosenMinutes == 60;
        _timed120.Checked = _timedActive && _timedChosenMinutes == 120;
        _timedCustom.Checked = _timedActive && _timedChosenMinutes is null;
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
            core = $"定时保持唤醒（剩 {DurationFormat.For(_timedExpiry - DateTime.UtcNow)}）";
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
