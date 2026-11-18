using Velopack;
using Velopack.Sources;

namespace Trackify.Services;

/// Velopack auto-update from GitHub Releases. Configured by setting UpdateRepoUrl in Core.cs.
public static class UpdateService
{
    public static async Task<string> CheckAndApplyAsync()
    {
        if (Config.UpdateRepoUrl.Contains("YOUR-NAME")) return "Updates are not configured yet.";
        try
        {
            using var mgr = new UpdateManager(new GithubSource(Config.UpdateRepoUrl, null, false));
            var update = await mgr.CheckForUpdatesAsync();
            if (update == null) return "is up to date";
            await mgr.DownloadUpdatesAsync(update);
            if (AppSettings.AutoUpdate) mgr.ApplyUpdatesAndRestart(update);
            return $"v{update.TargetFullRelease.Version} downloaded — restart to apply";
        }
        catch (Exception ex) { return "update check failed: " + ex.Message; }
    }
}
