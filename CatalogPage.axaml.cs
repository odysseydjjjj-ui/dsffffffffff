using Avalonia.Controls;
using Avalonia.Interactivity;
using Trackify.Models;

namespace Trackify.Views;

public partial class CatalogPage : UserControl
{
    public CatalogPage()
    {
        InitializeComponent();
        Items.ItemsSource = MockData.Gifts;
    }

    private void OnFilter(object? sender, RoutedEventArgs e)
    {
        var unique = sender == UniqueBtn;
        AllBtn.IsChecked = !unique;
        UniqueBtn.IsChecked = unique;
        Items.ItemsSource = unique ? MockData.Gifts.Where(g => g.Unique).ToList() : MockData.Gifts;
    }
}
