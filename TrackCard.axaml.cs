using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Trackify.Models;
using Trackify.Services;

namespace Trackify.Controls;

public partial class TrackCard : UserControl
{
    public TrackCard() { InitializeComponent(); }

    private void OnClick(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is Track t) _ = PlayerService.PlayAsync(t);
    }
}
