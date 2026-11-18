using Avalonia.Controls;
using Trackify.Models;

namespace Trackify.Views;

public partial class ChatsPage : UserControl
{
    public ChatsPage()
    {
        InitializeComponent();
        Friends.ItemsSource = MockData.Chats;
        Msgs.ItemsSource = MockData.Messages;
    }
}
