using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Trackify.Models;

namespace Trackify.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        TitleBar.PointerPressed += (_, e) =>
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) BeginMoveDrag(e);
        };
        Player.AccentChanged += SetAccent;
        Services.PlayerHooks.Wire();
        AppSettings.Changed += ApplyBackground;
        SetAccent(Player.CurrentColor);
        Show("Home");
    }

    private void SetAccent(Color color) => BgOverlay.Background = new SolidColorBrush(color);

    private void ApplyBackground()
    {
        BgOverlay.Opacity = AppSettings.DynamicBackground ? 0.3 : 0;
        SetAccent(Player.CurrentColor);
    }

    private void Show(string page)
    {
        Host.Content = page switch
        {
            "Search" => new SearchPage(),
            "Playlists" => new PlaylistsPage(),
            "Forum" => new ForumPage(),
            "Chats" => new ChatsPage(),
            "Catalog" => new CatalogPage(),
            "Plans" => new PlansPage(),
            "Games" => new GamesPage(),
            "Profile" => new ProfilePage(),
            "Settings" => new SettingsPage(),
            _ => new HomePage()
        };
    }

    private void OnNav(object? sender, RoutedEventArgs e)
    {
        if (sender is RadioButton rb && rb.Tag is string tag) Show(tag);
    }

    private void OnMinimize(object? sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void OnMaximize(object? sender, RoutedEventArgs e) =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void OnClose(object? sender, RoutedEventArgs e) => Close();

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (AppSettings.MinimizeToTray && !App.Quitting)
        {
            e.Cancel = true;
            Hide();
        }
        base.OnClosing(e);
    }
}
