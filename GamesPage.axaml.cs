using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Trackify.Models;

namespace Trackify.Views;

public partial class GamesPage : UserControl
{
    Game? current;
    int state;                  // reaction: 0 idle, 1 waiting, 2 go
    DispatcherTimer? delay;
    DateTime goAt;
    int taps;
    DateTime tapStart;
    readonly Random rng = new();

    public GamesPage()
    {
        InitializeComponent();
        Items.ItemsSource = MockData.Games;
        delay = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
        delay.Tick += DelayTick;
    }

    private void OnPlay(object? sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.DataContext is not Game g) return;
        if (g.Kind == "soon") { GameZone.IsVisible = true; GameTitle.Text = g.Name; GameHint.Text = g.Description; GameButton.IsVisible = false; GameResult.Text = "Coming in stage 3."; return; }
        current = g; state = 0; taps = 0;
        GameZone.IsVisible = true;
        GameButton.IsVisible = true;
        GameTitle.Text = g.Name;
        GameHint.Text = g.Description;
        GameResult.Text = "";
        if (g.Kind == "reaction") GameButton.Content = "Start";
        else GameButton.Content = "Tap!";
    }

    private void OnGameClick(object? sender, RoutedEventArgs e)
    {
        if (current?.Kind == "reaction") ReactionClick();
        else if (current?.Kind == "clicker") ClickerClick();
    }

    void ReactionClick()
    {
        if (state == 0)
        {
            state = 1;
            GameButton.Content = "Wait for it...";
            delay!.Interval = TimeSpan.FromMilliseconds(rng.Next(1200, 4000));
            delay.Start();
        }
        else if (state == 1)
        {
            delay!.Stop(); state = 0;
            GameResult.Text = "Too early! Click Start to try again.";
            GameButton.Content = "Start";
        }
        else
        {
            state = 0;
            var ms = (DateTime.Now - goAt).TotalMilliseconds;
            GameResult.Text = ms < 250 ? $"{ms:F0} ms — insane!" : $"{ms:F0} ms";
            GameButton.Content = "Start";
        }
    }

    void DelayTick(object? s, EventArgs e)
    {
        if (current?.Kind != "reaction" || state != 1) { delay!.Stop(); return; }
        delay!.Stop();
        state = 2;
        goAt = DateTime.Now;
        GameButton.Content = "CLICK!";
    }

    void ClickerClick()
    {
        if (taps == 0) { tapStart = DateTime.Now; taps = 1; GameResult.Text = "1 / 15"; return; }
        taps++;
        GameResult.Text = $"{taps} / 15";
        if (taps >= 15)
        {
            var ms = (DateTime.Now - tapStart).TotalMilliseconds;
            GameResult.Text = ms < 4000 ? $"{ms / 1000:F2}s — new record!" : $"{ms / 1000:F2}s";
            taps = 0;
        }
    }
}
