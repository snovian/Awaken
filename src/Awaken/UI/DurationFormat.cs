namespace Awaken.UI;

/// <summary>
/// 分钟数与剩余时长的中文展示格式。
/// </summary>
internal static class DurationFormat
{
    public static string For(int minutes) => minutes switch
    {
        < 60 => $"{minutes} 分钟",
        int h when minutes % 60 == 0 => $"{h / 60} 小时",
        _ => $"{minutes / 60} 小时 {minutes % 60} 分",
    };

    public static string For(TimeSpan remaining) => For((int)Math.Ceiling(remaining.TotalMinutes));
}
