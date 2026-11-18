using System.Text.Json;

namespace Trackify.Models;

public static class AppSettings
{
    static readonly string FilePath = Path.Combine(Config.DataDir, "settings.json");

    public static bool DynamicBackground { get; set; } = true;
    public static bool MinimizeToTray { get; set; } = true;
    public static bool DiscordActivity { get; set; } = true;
    public static bool AutoUpdate { get; set; } = true;
    public static bool OfflineCache { get; set; } = true;

    public static event Action? Changed;
    public static void Raise() => Changed?.Invoke();

    static AppSettings() { try { Load(); } catch { } }

    static void Load()
    {
        if (!File.Exists(FilePath)) return;
        var d = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(File.ReadAllText(FilePath));
        if (d == null) return;
        DynamicBackground = d.GetValueOrDefault("bg").GetBoolean();
        MinimizeToTray = d.GetValueOrDefault("tray").GetBoolean();
        DiscordActivity = d.GetValueOrDefault("discord").GetBoolean();
        AutoUpdate = d.GetValueOrDefault("update").GetBoolean();
        OfflineCache = d.GetValueOrDefault("offline").GetBoolean();
    }

    public static void Save()
    {
        var d = new Dictionary<string, object>
        {
            ["bg"] = DynamicBackground, ["tray"] = MinimizeToTray, ["discord"] = DiscordActivity,
            ["update"] = AutoUpdate, ["offline"] = OfflineCache,
        };
        File.WriteAllText(FilePath, JsonSerializer.Serialize(d));
    }
}
