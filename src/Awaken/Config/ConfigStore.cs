using System.Text.Json;
using System.Text.Json.Serialization;

namespace Awaken.Config;

/// <summary>
/// 便携配置存取：优先 exe 旁 config.json（绿色模式）；目录不可写时降级到
/// %LocalAppData%\Awaken\config.json。解析失败回退默认值，写入走临时文件原子替换。
/// </summary>
internal static class ConfigStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private static string? _configPath;

    public static string ConfigPath => _configPath ??= ResolvePath();

    private static string ResolvePath()
    {
        string portablePath = Path.Combine(AppContext.BaseDirectory, "config.json");

        // 已存在便携配置 → 保持绿色模式，无论目录当前是否可写。
        if (File.Exists(portablePath))
        {
            return portablePath;
        }

        try
        {
            // 试探 exe 目录可写性。
            File.WriteAllText(portablePath, string.Empty);
            File.Delete(portablePath);
            return portablePath;
        }
        catch (UnauthorizedAccessException)
        {
        }
        catch (IOException)
        {
        }

        string appDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Awaken");
        Directory.CreateDirectory(appDataDir);
        return Path.Combine(appDataDir, "config.json");
    }

    public static AppConfig Load()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                return JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(ConfigPath), JsonOptions) ?? new AppConfig();
            }
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            // 配置损坏不致命：回退默认值，下次保存覆盖。
        }

        return new AppConfig();
    }

    public static void Save(AppConfig config)
    {
        string path = ConfigPath;
        string tmp = path + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(config, JsonOptions));
        if (File.Exists(path))
        {
            File.Replace(tmp, path, null);
        }
        else
        {
            File.Move(tmp, path);
        }
    }
}
