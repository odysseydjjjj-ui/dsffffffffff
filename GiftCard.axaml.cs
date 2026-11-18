using Avalonia.Controls;
using Avalonia.Interactivity;
using Trackify.Models;
using Trackify.Services;

namespace Trackify.Controls;

public partial class GiftCard : UserControl
{
    public GiftCard() { InitializeComponent(); }

    private async void OnBuy(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not Gift g || Backend.Me == null) return;
        try { await Backend.BuyGiftAsync(g.Id); } catch (ServiceException) { }
    }
}
