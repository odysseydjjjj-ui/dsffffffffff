using LibVLCSharp.Shared;
using Trackify.Models;

namespace Trackify.Services;

public static class PlayerService
{
    static readonly LibVLC libvlc;
    static readonly MediaPlayer mp;
    static readonly List<Track> queue = new();
    static int index = -1;

    public static Track? Current { get; private set; }
    public static bool IsPlaying => mp.IsPlaying;
    public static event Action<Track?>? TrackChanged;

    static PlayerService()
    {
        try
        {
            Core.Initialize();
            libvlc = new LibVLC("--no-video", "--network-caching=400");
            mp = new MediaPlayer(libvlc);
            mp.EndReached += (_, _) => { _ = NextAsync(); };
        }
        catch { libvlc = null!; mp = null!; }
    }

    public static void SetQueue(IEnumerable<Track> tracks)
    {
        queue.Clear();
        queue.AddRange(tracks);
    }

    public static async Task PlayAsync(Track t)
    {
        Current = t;
        TrackChanged?.Invoke(t);
        if (mp == null) return;
        try
        {
            string? loc = t.LocalPath;
            if (loc == null && t.Id is Guid id) loc = OfflineCache.Find(id);
            if (loc == null && t.StoragePath != null) loc = await Backend.SignedUrlAsync("tracks", t.StoragePath);
            if (string.IsNullOrEmpty(loc)) return;
            var media = new Media(libvlc, loc, FromLocation);
            mp.Media = media;
            mp.Play();
            media.Dispose();
        }
        catch { }
    }

    /// Downloads the current track into the offline cache in the background.
    public static async Task CacheCurrentAsync()
    {
        var t = Current;
        if (t?.Id is not Guid id || t.StoragePath == null || OfflineCache.Find(id) != null) return;
        try
        {
            var url = await Backend.SignedUrlAsync("tracks", t.StoragePath);
            using var http = new HttpClient();
            var bytes = await http.GetByteArrayAsync(url);
            OfflineCache.Save(id, Path.GetExtension(t.StoragePath), bytes);
        }
        catch { }
    }

    public static async Task NextAsync()
    {
        if (queue.Count == 0) return;
        index = (index + 1) % queue.Count;
        await PlayAsync(queue[index]);
    }

    public static async Task PrevAsync()
    {
        if (queue.Count == 0) return;
        index = (index - 1 + queue.Count) % queue.Count;
        await PlayAsync(queue[index]);
    }

    public static void Toggle() { if (mp == null) return; if (mp.IsPlaying) mp.Pause(); else mp.Play(); }
    public static void SetVolume(int v) { if (mp != null) mp.Volume = v; }

    public static async Task SeekAsync(double fraction)
    {
        if (mp == null || mp.Media == null) return;
        var len = mp.Media.Duration;
        if (len > 0) await Task.Run(() => mp.SeekTo(TimeSpan.FromMilliseconds(len * Math.Clamp(fraction, 0, 1))));
    }

    public static (TimeSpan pos, TimeSpan len) Position()
    {
        if (mp?.Media == null) return (TimeSpan.Zero, TimeSpan.Zero);
        var len = TimeSpan.FromMilliseconds(Math.Max(0, mp.Media.Duration));
        var pos = mp.Time >= 0 ? TimeSpan.FromMilliseconds(mp.Time) : TimeSpan.Zero;
        return (pos, len);
    }
}
