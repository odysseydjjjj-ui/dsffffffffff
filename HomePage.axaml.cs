using Avalonia.Controls;
using Trackify.Models;

namespace Trackify.Views;

public partial class HomePage : UserControl
{
    public HomePage()
    {
        InitializeComponent();
        Recent.ItemsSource = MockData.Tracks.Take(3).ToList();
        Popular.ItemsSource = MockData.Popular;
        Services.PlayerService.SetQueue(MockData.Tracks.Concat(MockData.Popular));
    }
}
