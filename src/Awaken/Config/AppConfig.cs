using Awaken.Core;

namespace Awaken.Config;

/// <summary>
/// 应用配置，JSON 序列化。
/// </summary>
internal sealed class AppConfig
{
    /// <summary>上次使用的唤醒模式。</summary>
    public AwakeMode Mode { get; set; } = AwakeMode.KeepAwake;

    /// <summary>上次自定义定时唤醒的分钟数，作为对话框默认值。</summary>
    public int LastTimedMinutes { get; set; } = 60;

    /// <summary>屏幕常亮叠加开关：任何唤醒状态（含定时）下均附加 ES_DISPLAY_REQUIRED。</summary>
    public bool KeepDisplayOn { get; set; }
}
