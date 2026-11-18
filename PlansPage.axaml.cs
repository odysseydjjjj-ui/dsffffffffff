using Avalonia.Controls;
using Trackify.Models;

namespace Trackify.Views;

public partial class PlansPage : UserControl
{
    public PlansPage()
    {
        InitializeComponent();
        Items.ItemsSource = MockData.Plans;
    }
}
