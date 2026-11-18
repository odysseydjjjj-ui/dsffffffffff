using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Trackify.Models;

namespace Trackify.Views;

public partial class ForumPage : UserControl
{
    private readonly ObservableCollection<ForumPost> posts = new(MockData.Posts);

    public ForumPage()
    {
        InitializeComponent();
        Posts.ItemsSource = posts;
    }

    private void OnPost(object? sender, RoutedEventArgs e)
    {
        var text = Draft.Text?.Trim() ?? "";
        if (text.Length == 0) return;
        var lowered = text.ToLowerInvariant();
        if (lowered.Contains("http") || lowered.Contains("www.") || lowered.Contains(".com") || lowered.Contains(".ru"))
        {
            Warning.IsVisible = true;
            return;
        }
        Warning.IsVisible = false;
        posts.Insert(1, new ForumPost { Title = text, Author = "you", Replies = 0 });
        Draft.Text = "";
    }
}
