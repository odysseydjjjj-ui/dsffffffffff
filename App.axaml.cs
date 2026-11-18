using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Trackify.Views;

namespace Trackify;

public partial class App : Application
{
    public static MainWindow? Main;
    public static bool Quitting;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            var login = new LoginWindow();
            desktop.MainWindow = login;
            login.Show();
        }
        base.OnFrameworkInitializationCompleted();
    }

    private void ShowMain()
    {
        if (Main == null) return;
        Main.Show();
        if (Main.WindowState == Avalonia.Controls.WindowState.Minimized) Main.WindowState = Avalonia.Controls.WindowState.Normal;
        Main.Activate();
    }

    private void OnTrayClicked(object? sender, EventArgs e) => ShowMain();

    private void OnOpen(object? sender, EventArgs e) => ShowMain();

    private void OnQuit(object? sender, EventArgs e)
    {
        Quitting = true;
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) desktop.Shutdown();
    }
}
