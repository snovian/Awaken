using System.Drawing.Drawing2D;
using Awaken.Core;

namespace Awaken.UI;

/// <summary>
/// 按状态程序化生成托盘图标（免 ico 资源文件），缓存复用，退出时统一销毁 GDI 句柄。
/// </summary>
internal static class StateIcons
{
    private static readonly Dictionary<(bool Active, bool Display, bool Timed), Icon> Cache = [];

    /// <summary>状态 = 是否唤醒 × 是否屏幕常亮 × 是否定时。</summary>
    public static Icon For(bool active, bool display, bool timed)
    {
        var key = (active, display, timed);
        if (Cache.TryGetValue(key, out Icon? cached))
        {
            return cached;
        }

        Icon icon = Draw(active, display, timed);
        Cache[key] = icon;
        return icon;
    }

    public static void DestroyAll()
    {
        foreach (Icon icon in Cache.Values)
        {
            _ = NativeMethods.DestroyIcon(icon.Handle);
        }

        Cache.Clear();
    }

    private static Icon Draw(bool active, bool display, bool timed)
    {
        using var bmp = new Bitmap(32, 32);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;

        if (!active)
        {
            DrawCircle(g, filled: false, Color.Gray);
        }
        else
        {
            DrawCircle(g, filled: true, display ? Color.FromArgb(33, 150, 243) : Color.FromArgb(76, 175, 80));
            if (display)
            {
                DrawScreen(g);
            }

            if (timed)
            {
                DrawClock(g);
            }
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
