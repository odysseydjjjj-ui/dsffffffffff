using System.Text.Json;
using Trackify.Models;

namespace Trackify.Services;

/// Local mirror of TrackCloud tracks. Lets the app play downloaded tracks with no internet.
/// JSON index + files on disk, LRU cleanup at 2 GB.
public static class OfflineCache
{
    static string Dir => Directory.CreateDirectory(Path.Combine(Config.DataDir, "cache")).FullName;
    static string IndexPath => Path.Combine(Dir, "index.json");
    const long MaxBytes = 2L * 1024 * 1024 * 1024;

    record Entry(string File, string Title, string Artist);

    static Dictionary<Guid, Entry> index = new();

    static OfflineCache()
    {
        try
        {
            var raw = File.Exists(IndexPath) ? File.ReadAllText(IndexPath) : "{}";
            index = JsonSerializer.Deserialize<Dictionary<Guid, Entry>>(raw) ?? new();
        }
        catch { index = new(); }
    }

    static void SaveIndex() =>
        File.WriteAllText(IndexPath, JsonSerializer.Serialize(index));

    public static string? Find(Guid id) =>
        index.TryGetValue(id, out var e) && File.Exists(Path.Combine(Dir, e.File)) ? Path.Combine(Dir, e.File) : null;

    public static void Save(Guid id, string ext, byte[] bytes)
    {
        var name = id.ToString("N") + ext;
        File.WriteAllBytes(Path.Combine(Dir, name), bytes);
        index[id] = new Entry(name, "", "");
        SaveIndex();
        Cleanup();
    }

    public static List<Track> OfflineTracks() => index.Select(kv => new Track
    {
        Id = kv.Key, Title = kv.Value.Title, Artist = kv.Value.Artist,
        Duration = "offline", LocalPath = Find(kv.Key),
    }).Where(t => t.LocalPath != null).ToList();

    public static void SetMeta(Guid id, string title, string artist)
    {
        if (index.TryGetValue(id, out var e)) { index[id] = e with { Title = title, Artist = artist }; SaveIndex(); }
    }

    public static void Remove(Guid id)
    {
        if (index.TryGetValue(id, out var e)) { try { File.Delete(Path.Combine(Dir, e.File)); } catch { } index.Remove(id); SaveIndex(); }
    }

    static void Cleanup()
    {
        try
        {
            long total = Directory.EnumerateFiles(Dir).Where(f => f != IndexPath).Sum(f => new FileInfo(f).Length);
            foreach (var f in Directory.EnumerateFiles(Dir).Where(f => f != IndexPath)
                         .Select(f => new FileInfo(f)).OrderBy(f => f.LastWriteTimeUtc))
            {
                if (total <= MaxBytes) break;
                total -= f.Length;
                try { f.Delete(); } catch { }
            }
            var gone = index.Where(kv => !File.Exists(Path.Combine(Dir, kv.Value.File))).Select(kv => kv.Key).ToList();
            foreach (var k in gone) index.Remove(k);
            if (gone.Count > 0) SaveIndex();
        }
        catch { }
    }
}
