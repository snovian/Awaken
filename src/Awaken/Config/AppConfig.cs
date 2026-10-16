using Awaken.Core;

namespace Awaken.Config;

/// <summary>
/// 应用配置，JSON 序列化。
/// </summary>
internal sealed class AppConfig
{
    /// <summary>上次使用的唤醒模式。</summary>
    public AwakeMode Mode { get; set; } = AwakeMode.KeepAwake;
}
