using Awaken.Core;

namespace Awaken.Config;

/// <summary>
/// 应用配置，JSON 序列化。
/// </summary>
internal sealed class AppConfig
{
    /// <summary>上次使用的唤醒模式。</summary>
    public AwakeMode Mode { get; set; } = AwakeMode.KeepAwake;

    /// <summary>屏幕常亮叠加开关：任何唤醒状态（含定时）下均附加 ES_DISPLAY_REQUIRED。</summary>
    public bool KeepDisplayOn { get; set; }

    /// <summary>显示在托盘一级菜单的快捷定时项（分钟数），点击即开始对应时长的定时唤醒。</summary>
    public List<int> RootTimedMinutes { get; set; } = [30, 60, 120];
}
