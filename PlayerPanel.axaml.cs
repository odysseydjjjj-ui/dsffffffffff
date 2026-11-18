using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Trackify.Models;
using Trackify.Services;

namespace Trackify.Controls;

public partial class PlayerPanel : UserControl
{
    public static Color CurrentColor { get; private set; } = Color.Parse("#8B5CF6");
    public event Action<Color>? AccentChanged;

    readonly DispatcherTimer ticker;
    bool dragging;
    DateTime lastBeat = DateTime.MinValue;

    public PlayerPanel()
    {
        InitializeComponent();
        ticker = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        ticker.Tick += (_, _) => Tick();
        ticker.Start();

        Seek.AddHandler(Slider.PointerPressedEvent, (_, _) => dragging = true);
        Seek.AddHandler(Slider.PointerReleasedEvent, (_, _) => { dragging = false; _ = PlayerService.SeekAsync(Seek.Value / 1000.0); });
        Volume.ValueChanged += (_, e) => PlayerService.SetVolume((int)e.NewValue);

        PlayerService.TrackChanged += OnTrack;
        SetAccent(Color.Parse("#8B5CF6"));
    }

    void OnTrack(Track? t)
    {
        if (t == null) return;
        TitleText.Text = t.Title;
        ArtistText.Text = $"{t.Artist} · {t.Source}";
        CoverGlyph.Text = t.Initial;
        LyricLine.Text = ""; LyricWords.Text = ""; LyricNext.Text = "";
        try { SetAccent(Color.Parse(t.Color)); } catch { }
        if (AppSettings.OfflineCache) _ = PlayerService.CacheCurrentAsync();
    }

    void SetAccent(Color c)
    {
        CurrentColor = c;
        Cover.Background = new SolidColorBrush(c);
        AccentChanged?.Invoke(c);
    }

    void Tick()
    {
        var (pos, len) = PlayerService.Position();
        if (!dragging) Seek.Value = len > 0 ? 1000.0 * pos.TotalSeconds / len.TotalSeconds : 0;
        Elapsed.Text = $"{(int)pos.TotalMinutes}:{pos.Seconds:D2}";
        Total.Text = len > TimeSpan.Zero ? $"{(int)len.TotalMinutes}:{len.Seconds:D2}" : "--:--";
        PlayGlyph.Text = PlayerService.IsPlaying ? "❚❚" : "▶";

        var t = PlayerService.Current;
        if (t?.Lyrics != null)
        {
            var (line, idx) = LyricsService.At(LyricsService.Parse(t.Lyrics), pos);
            if (line != null)
            {
                LyricLine.Text = line.Text;
                LyricWords.Text = line.Words.Count > 0
                    ? string.Join(" ", line.Words.Select((w, i) => i == idx ? $"[{w.Text}]" : w.Text))
                    : "";
            }
        }

        if (PlayerService.IsPlaying && Backend.Me != null && DateTime.UtcNow - lastBeat > TimeSpan.FromSeconds(28))
        {
            lastBeat = DateTime.UtcNow;
            _ = Backend.EarnListenAsync();
        }
    }

    private void OnPlay(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => PlayerService.Toggle();
    private void OnNext(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => _ = PlayerService.NextAsync();
    private void OnPrev(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => _ = PlayerService.PrevAsync();
}
