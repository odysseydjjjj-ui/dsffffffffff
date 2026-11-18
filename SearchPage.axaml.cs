using System.Text.Json;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Trackify.Models;
using Trackify.Services;

namespace Trackify.Views;

public partial class SearchPage : UserControl
{
    static readonly string[] Palette = { "#8B5CF6", "#EC4899", "#F59E0B", "#10B981", "#3B82F6", "#EF4444" };

    public SearchPage()
    {
        InitializeComponent();
        Most.ItemsSource = MockData.Popular.Take(3).ToList();
        Query.TextChanged += async (_, _) => await SearchAsync();
    }

    private void OnFilter(object? sender, RoutedEventArgs e) => _ = SearchAsync();

    async Task SearchAsync()
    {
        var q = Query.Text?.Trim() ?? "";
        var results = new List<Track>();
        Empty.IsVisible = true;

        if (OfflineBtn.IsChecked == true)
        {
            results = OfflineCache.OfflineTracks();
            Empty.Text = results.Count == 0 ? "No offline tracks yet — enable caching in Settings and play something" : "";
            Empty.IsVisible = results.Count == 0;
        }
        else if (Backend.Me != null)
        {
            try
            {
                var e = await Backend.RpcAsync("search_uploads", new Dictionary<string, object?> { ["p_q"] = q });
                if (e.ValueKind == JsonValueKind.Array)
                {
                    int i = 0;
                    results = e.EnumerateArray().Select(x => new Track
                    {
                        Id = x.GetProperty("id").GetGuid(),
                        Title = x.GetProperty("title").GetString() ?? "",
                        Artist = x.GetProperty("uploader").GetString() ?? "",
                        Duration = x.TryGetProperty("duration", out var d) && d.ValueKind == JsonValueKind.Number ? Track.MmSs(d.GetInt32()) : "--:--",
                        Color = Palette[(i++) % Palette.Length],
                        StoragePath = x.GetProperty("path").GetString(),
                        Lyrics = x.TryGetProperty("lyrics", out var l) && l.ValueKind == JsonValueKind.String ? l.GetString() : null,
                    }).ToList();
                }
            }
            catch { }
            if (UploadsBtn.IsChecked == true)
                results = results.Where(t => t.Artist == Backend.Me.Username).ToList();
            Empty.Text = results.Count == 0 ? (q.Length == 0 ? "Type something to search TrackCloud" : $"Nothing found for \"{q}\"") : "";
            Empty.IsVisible = results.Count == 0;
        }
        else
        {
            results = MockData.Tracks.Where(t => t.Title.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();
            Empty.Text = "Offline demo — sign in to search TrackCloud";
        }

        Results.ItemsSource = results;
        PlayerService.SetQueue(results.Concat(MockData.Popular));
    }
}
