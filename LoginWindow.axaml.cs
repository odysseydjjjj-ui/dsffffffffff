using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Trackify.Services;

namespace Trackify.Views;

public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();
        Opened += async (_, _) =>
        {
            try
            {
                await Backend.InitAsync();
                if (await AuthService.TryRestoreAsync()) OpenMain();
            }
            catch { }
        };
    }

    void OpenMain()
    {
        var main = new MainWindow();
        App.Main = main;
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = main;
        main.Show();
        Close();
    }

    void ShowError(AuthResult r)
    {
        if (r == AuthResult.Ok) { ErrorText.IsVisible = false; return; }
        ErrorText.Text = r switch
        {
            AuthResult.Cancelled => "Sign-in was cancelled.",
            AuthResult.Banned => "This account is banned.",
            AuthResult.AltNotSupported => "This device already has a Trackify account. Alt accounts are not supported.",
            _ => AuthService.LastError.Length > 0 ? AuthService.LastError : "Sign-in failed. Check your email and password.",
        };
        ErrorText.IsVisible = true;
    }

    private async void OnSignIn(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(Email.Text) || string.IsNullOrWhiteSpace(Pass.Text)) return;
        var r = await AuthService.SignInEmailAsync(Email.Text.Trim(), Pass.Text);
        ShowError(r);
        if (r == AuthResult.Ok) OpenMain();
    }

    private async void OnSignUp(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(Email.Text) || string.IsNullOrWhiteSpace(Pass.Text)) return;
        var r = await AuthService.SignUpAsync(Email.Text.Trim(), Pass.Text);
        ShowError(r);
        if (r == AuthResult.Ok) OpenMain();
    }

    private async void OnGoogle(object? sender, RoutedEventArgs e)
    {
        var r = await AuthService.SignInWithGoogleAsync();
        ShowError(r);
        if (r == AuthResult.Ok) OpenMain();
    }
}
