using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Trackify.Models;

namespace Trackify.Views;

public partial class ProfilePage : UserControl
{
    public ProfilePage()
    {
        InitializeComponent();
        Pinned.ItemsSource = MockData.Gifts.Where(g => g.Unique).Take(2).ToList();

        foreach (var hex in MockData.BannerColors)
        {
            var brush = new SolidColorBrush(Color.Parse(hex));
            var b = new Button
            {
                Width = 34,
                Height = 34,
                CornerRadius = new Avalonia.CornerRadius(17),
                Background = brush,
                BorderBrush = new SolidColorBrush(Color.Parse("#44FFFFFF")),
                BorderThickness = new Avalonia.Thickness(1)
            };
            b.Click += (_, _) => Banner.Background = brush;
            ColorRow.Children.Add(b);
        }

        foreach (var name in new[] { "free", "pro", "max", "ultra", "custom" })
        {
            var sprite = new Trackify.Controls.SpriteIcon { Source = name, Width = 40, Height = 40 };
            var b = new Button
            {
                Classes = { "icon" },
                Padding = new Avalonia.Thickness(4),
                Content = sprite
            };
            b.Click += (_, _) => RingSprite.Source = name;
            RingRow.Children.Add(b);
        }
    }

    private void OnCrop(object? sender, RoutedEventArgs e)
    {
    }
}
