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
/// 唤醒模式。
/// </summary>
internal enum AwakeMode
{
    /// <summary>不干预，恢复正常电源策略。</summary>
    Off,

    /// <summary>保持系统唤醒（不休眠），屏幕可熄灭。</summary>
    KeepAwake,

    /// <summary>保持系统唤醒且屏幕常亮。</summary>
    KeepDisplayOn,
}
