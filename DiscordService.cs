using DiscordRPC;
using Trackify.Models;

namespace Trackify.Services;

/// Discord Rich Presence: shows the current track. No-op until a real DiscordAppId is set in Core.cs.
public static class DiscordService
{
    static DiscordRpcClient? client;
    static RichPresence? last;

    static bool Enabled => AppSettings.DiscordActivity
        && !Config.DiscordAppId.StartsWith("YOUR-", StringComparison.OrdinalIgnoreCase);

    static void Ensure()
    {
        if (client != null || !Enabled) return;
        try
        {
            client = new DiscordRpcClient(Config.DiscordAppId);
            client.Initialize();
            if (last != null) client.SetPresence(last);
        }
        catch { client = null; }
    }

    public static void UpdateTrack(Track? t)
    {
        last = t == null ? null : new RichPresence
        {
            Details = t.Title,
            State = t.Artist,
            Assets = new Assets { LargeImageKey = "trackify", LargeImageText = "Trackify · TrackCloud" },
            Timestamps = Timestamps.Now,
        };
        if (last == null) { client?.ClearPresence(); return; }
        Ensure();
        if (client?.IsInitialized == true) client.SetPresence(last);
    }

    public static void Shutdown() => client?.Dispose();
}
