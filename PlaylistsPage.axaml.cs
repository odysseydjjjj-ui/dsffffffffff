using Avalonia.Controls;
using Trackify.Models;

namespace Trackify.Views;

public partial class PlaylistsPage : UserControl
{
    public PlaylistsPage()
    {
        InitializeComponent();
        Lists.ItemsSource = new List<Track>
        {
            new Track { Title = "Favorites", Artist = "24 tracks", Duration = "1:12:40", Source = "Playlist", Color = MockData.Tracks[0].Color },
            new Track { Title = "Night Drive", Artist = "18 tracks", Duration = "58:02", Source = "Playlist", Color = MockData.Tracks[2].Color },
            new Track { Title = "Focus", Artist = "31 tracks", Duration = "1:44:15", Source = "Playlist", Color = MockData.Tracks[3].Color }
        };
    }
}
