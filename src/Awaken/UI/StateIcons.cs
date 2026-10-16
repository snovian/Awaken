using System.Drawing.Drawing2D;
using Awaken.Core;

namespace Awaken.UI;

/// <summary>
/// 按模式程序化生成托盘图标（免 ico 资源文件），缓存复用，退出时统一销毁 GDI 句柄。
/// </summary>
internal static class StateIcons
{
    private static readonly Dictionary<AwakeMode, Icon> Cache = [];

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

    public static void DestroyAll()
    {
        foreach (Icon icon in Cache.Values)
        {
            _ = NativeMethods.DestroyIcon(icon.Handle);
        }

        Cache.Clear();
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
