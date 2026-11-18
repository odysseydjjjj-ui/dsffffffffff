using System.Reflection;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Trackify.Models;
using Trackify.Services;

namespace Trackify.Views;

public partial class SettingsPage : UserControl
{
    public SettingsPage()
    {
        InitializeComponent();
        TgBg.IsChecked = AppSettings.DynamicBackground;
        TgOffline.IsChecked = AppSettings.OfflineCache;
        TgTray.IsChecked = AppSettings.MinimizeToTray;
        TgDiscord.IsChecked = AppSettings.DiscordActivity;
        TgUpdate.IsChecked = AppSettings.AutoUpdate;
        UpdateText.Text = "Version " + (Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "?");

        if (Backend.Me != null)
        {
            NameBox.Text = Backend.Me.DisplayName;
            UserText.Text = $"@{Backend.Me.Username} · ID {Backend.Me.NumberId}";
        }

        TgBg.IsCheckedChanged += (_, _) => { AppSettings.DynamicBackground = TgBg.IsChecked == true; AppSettings.Save(); AppSettings.Raise(); };
        TgOffline.IsCheckedChanged += (_, _) => { AppSettings.OfflineCache = TgOffline.IsChecked == true; AppSettings.Save(); };
        TgTray.IsCheckedChanged += (_, _) => { AppSettings.MinimizeToTray = TgTray.IsChecked == true; AppSettings.Save(); };
        TgDiscord.IsCheckedChanged += (_, _) => { AppSettings.DiscordActivity = TgDiscord.IsChecked == true; AppSettings.Save(); };
        TgUpdate.IsCheckedChanged += (_, _) => { AppSettings.AutoUpdate = TgUpdate.IsChecked == true; AppSettings.Save(); };
        NameBox.LostFocus += async (_, _) =>
        {
            if (Backend.Me == null || string.IsNullOrWhiteSpace(NameBox.Text)) return;
            try { await Backend.SetDisplayNameAsync(NameBox.Text.Trim()); } catch (ServiceException) { NameBox.Text = Backend.Me.DisplayName; }
        };
    }

    private async void OnCheckUpdates(object? sender, RoutedEventArgs e) => UpdateText.Text = await UpdateService.CheckAndApplyAsync();

    private async void OnSignOut(object? sender, RoutedEventArgs e)
    {
        await AuthService.SignOutAsync();
        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            var login = new LoginWindow();
            desktop.MainWindow = login;
            login.Show();
            App.Main?.Close();
            App.Main = null;
        }
    }
}
