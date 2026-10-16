using System.Drawing.Drawing2D;
using Awaken.Core;

namespace Awaken.UI;

/// <summary>
/// 按模式程序化生成托盘图标（免 ico 资源文件），缓存复用，退出时统一销毁 GDI 句柄。
/// </summary>
internal static class StateIcons
{
    private static readonly Dictionary<AwakeMode, Icon> Cache = [];
    private static Icon? _timedIcon;

    public static Icon For(AwakeMode mode)
    {
        if (Cache.TryGetValue(mode, out Icon? cached))
        {
            return cached;
        }

        Icon icon = Draw(mode);
        Cache[mode] = icon;
        return icon;
    }

    /// <summary>定时模式图标：绿色圆 + 白色表盘。</summary>
    public static Icon Timed()
    {
        if (_timedIcon is not null)
        {
            return _timedIcon;
        }

        using var bmp = new Bitmap(32, 32);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        DrawCircle(g, filled: true, Color.FromArgb(76, 175, 80));
        DrawClock(g);
        _timedIcon = Icon.FromHandle(bmp.GetHicon());
        return _timedIcon;
    }

    public static void DestroyAll()
    {
        foreach (Icon icon in Cache.Values)
        {
            _ = NativeMethods.DestroyIcon(icon.Handle);
        }

        Cache.Clear();
        if (_timedIcon is not null)
        {
            _ = NativeMethods.DestroyIcon(_timedIcon.Handle);
            _timedIcon = null;
        }
    }

    private static Icon Draw(AwakeMode mode)
    {
        using var bmp = new Bitmap(32, 32);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;

        switch (mode)
        {
            case AwakeMode.KeepAwake:
                DrawCircle(g, filled: true, Color.FromArgb(76, 175, 80));
                break;
            case AwakeMode.KeepDisplayOn:
                DrawCircle(g, filled: true, Color.FromArgb(33, 150, 243));
                DrawScreen(g);
                break;
            default:
                DrawCircle(g, filled: false, Color.Gray);
                break;
        }

        return Icon.FromHandle(bmp.GetHicon());
    }

    private static void DrawClock(Graphics g)
    {
        using var ring = new Pen(Color.White, 2f);
        g.DrawEllipse(ring, 9, 9, 14, 14);
        using var hands = new Pen(Color.White, 2.5f);
        g.DrawLine(hands, 16, 16, 16, 11);
        g.DrawLine(hands, 16, 16, 20, 16);
    }

    private static void DrawCircle(Graphics g, bool filled, Color color)
    {
        var rect = new Rectangle(4, 4, 24, 24);
        if (filled)
        {
            using var brush = new SolidBrush(color);
            g.FillEllipse(brush, rect);
        }
        else
        {
            using var pen = new Pen(color, 3.5f);
            g.DrawEllipse(pen, rect);
        }
    }

    private static void DrawScreen(Graphics g)
    {
        using var brush = new SolidBrush(Color.White);
        g.FillRectangle(brush, 9, 14, 14, 7);
    }
}
