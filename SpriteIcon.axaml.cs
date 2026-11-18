using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Trackify.Controls;

/// Plan rings around avatars: free = gray, pro = violet, max = gold, ultra = animated gradient, custom = two user colors.
public partial class SpriteIcon : UserControl
{
    public static readonly StyledProperty<string> SourceProperty =
        AvaloniaProperty.Register<SpriteIcon, string>(nameof(Source), "free");

    public static readonly StyledProperty<string?> CustomColorsProperty =
        AvaloniaProperty.Register<SpriteIcon, string?>(nameof(CustomColors));

    public string Source
    {
        get => GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public string? CustomColors
    {
        get => GetValue(CustomColorsProperty);
        set => SetValue(CustomColorsProperty, value);
    }

    public SpriteIcon()
    {
        InitializeComponent();
        SourceProperty.Changed.AddClassHandler<SpriteIcon>((s, _) => s.Apply());
        CustomColorsProperty.Changed.AddClassHandler<SpriteIcon>((s, _) => s.Apply());
        Apply();
    }

    void Apply()
    {
        if (Ring == null) return;
        var custom = (Source == "custom" ? CustomColors : null);
        if (custom != null)
        {
            var parts = custom.Split(',');
            if (parts.Length == 2)
            {
                Ring.Stroke = new LinearGradientBrush
                {
                    StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                    EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
                    GradientStops = { new GradientStop(Color.Parse(parts[0]), 0), new GradientStop(Color.Parse(parts[1]), 1) }
                };
                return;
            }
        }
        Ring.Stroke = Source switch
        {
            "pro" => new SolidColorBrush(Color.Parse("#8B5CF6")),
            "max" => new SolidColorBrush(Color.Parse("#F59E0B")),
            "ultra" => new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
                GradientStops = { new GradientStop(Color.Parse("#8B5CF6"), 0), new GradientStop(Color.Parse("#EC4899"), 0.5), new GradientStop(Color.Parse("#F59E0B"), 1) }
            },
            _ => new SolidColorBrush(Color.Parse("#8A8A94")),
        };
    }
}
