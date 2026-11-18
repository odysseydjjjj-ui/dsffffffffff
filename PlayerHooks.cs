using Trackify.Models;

namespace Trackify.Services;

static class PlayerHooks
{
    static bool wired;
    public static void Wire()
    {
        if (wired) return;
        wired = true;
        PlayerService.TrackChanged += DiscordService.UpdateTrack;
    }
}
