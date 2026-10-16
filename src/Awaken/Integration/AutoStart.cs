using Microsoft.Win32;

namespace Awaken.Integration;

/// <summary>
/// 开机自启：HKCU Run 键，免管理员权限。绿色版场景下 exe 可能被移动，
/// 启动时调用 <see cref="SelfHeal"/> 修正注册路径。
/// </summary>
internal static class AutoStart
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "Awaken";

    public static bool IsEnabled()
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
        return key?.GetValue(ValueName) is not null;
    }

    public static void Enable()
    {
        using RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKeyPath);
        key.SetValue(ValueName, Quote(CurrentExePath));
    }

    public static void Disable()
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        key?.DeleteValue(ValueName, throwOnMissingValue: false);
    }

    /// <summary>已启用但注册路径与当前 exe 不一致（exe 被移动）时，更新注册路径。</summary>
    public static void SelfHeal()
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
        if (key?.GetValue(ValueName) is not string cmd)
        {
            return;
        }

        if (!string.Equals(cmd, Quote(CurrentExePath), StringComparison.OrdinalIgnoreCase))
        {
            Enable();
        }
    }

    private static string CurrentExePath =>
        Environment.ProcessPath ?? Application.ExecutablePath;

    private static string Quote(string path) => $"\"{path}\"";
}
