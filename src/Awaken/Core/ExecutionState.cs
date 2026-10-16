namespace Awaken.Core;

/// <summary>
/// Win32 SetThreadExecutionState 标志。
/// </summary>
[Flags]
internal enum ExecutionState : uint
{
    ES_CONTINUOUS = 0x80000000,
    ES_SYSTEM_REQUIRED = 0x00000001,
    ES_DISPLAY_REQUIRED = 0x00000002,
}

/// <summary>
/// 唤醒模式。屏幕常亮不再属于模式，而是独立的叠加开关（见 <see cref="AwakeManager.SetState"/>）。
/// </summary>
internal enum AwakeMode
{
    /// <summary>不干预，恢复正常电源策略。</summary>
    Off,

    /// <summary>保持系统唤醒（不休眠）。</summary>
    KeepAwake,

    /// <summary>旧配置值，仅作反序列化兼容，加载时迁移为 KeepAwake + 屏幕常亮开关。</summary>
    KeepDisplayOn,
}
