namespace Trackify.Models;

public static class MockData
{
    public static List<Track> Tracks { get; } = new()
    {
        new Track { Title = "Neon Tide",      Artist = "Kaito",        Duration = "3:12", Color = "#8B5CF6",
            Lyrics = "[00:02.00]City lights on the water\n[00:08.50]<00:08.50>We <00:09.10>run <00:09.70>through <00:10.40>the <00:11.00>neon <00:11.90>tide\n[00:16.00]Never look back\n[00:24.00]<00:24.00>Hold <00:24.60>on <00:25.20>to <00:25.80>the <00:26.40>night" },
        new Track { Title = "Midnight Run",   Artist = "Vera Lune",    Duration = "2:47", Color = "#EC4899",
            Lyrics = "[00:01.00]Engine hums at midnight\n[00:09.00]<00:09.00>Every <00:09.50>mile <00:10.20>a <00:10.80>new <00:11.40>beginning\n[00:20.00]We don't stop" },
        new Track { Title = "Golden Hour",    Artist = "Sundown Club", Duration = "3:58", Color = "#F59E0B",
            Lyrics = "[00:03.00]Slow down, breathe in\n[00:11.00]<00:11.00>Everything <00:11.80>is <00:12.30>golden <00:13.10>now" },
        new Track { Title = "Glass City",     Artist = "Mono",         Duration = "4:05", Color = "#10B981" },
        new Track { Title = "Paper Planes",   Artist = "June Park",    Duration = "2:31", Color = "#3B82F6" },
        new Track { Title = "Afterglow",      Artist = "Nokta",        Duration = "3:44", Color = "#EF4444" },
    };

    public static List<Track> Popular { get; } = new()
    {
        Tracks[1], Tracks[4],
        new Track { Title = "Ultraviolet",  Artist = "Hexa",    Duration = "3:02", Color = "#8B5CF6" },
        new Track { Title = "Dust & Stars", Artist = "Lowlight", Duration = "5:10", Color = "#F59E0B" },
        new Track { Title = "Cold Brew",    Artist = "Tokyo FM", Duration = "2:56", Color = "#10B981" },
    };

    public static List<Gift> Gifts { get; } = new()
    {
        new Gift { Name = "Rose Loop",      Price = 60,  Unique = false, VideoPath = "gifts/rose.mp4" },
        new Gift { Name = "Neon Cat",       Price = 150, Unique = false, VideoPath = "gifts/cat.mp4" },
        new Gift { Name = "Golden Crown",   Price = 500, Unique = true,  VideoPath = "gifts/crown.mp4" },
        new Gift { Name = "Pixel Dragon",   Price = 900, Unique = true,  VideoPath = "gifts/dragon.mp4" },
        new Gift { Name = "Ghost Flame",    Price = 120, Unique = false, VideoPath = "games/ghost.mp4" },
    };

    public static List<Game> Games { get; } = new()
    {
        new Game { Name = "Reaction", Glyph = "R", Description = "Wait for green, then click as fast as you can.", Kind = "reaction" },
        new Game { Name = "Clicker",  Glyph = "C", Description = "Tap 15 times. Beat your best time.", Kind = "clicker" },
        new Game { Name = "Rhythm",   Glyph = "~", Description = "Tap to the beat of the track. Coming in stage 3.", Kind = "soon" },
        new Game { Name = "Guessr",   Glyph = "?", Description = "Guess the track from a 3 second clip. Coming in stage 3.", Kind = "soon" },
    };

    public static List<ForumPost> Posts { get; } = new()
    {
        new ForumPost { Title = "Welcome to Trackify", Author = "ryan", Replies = 42, Pinned = true },
        new ForumPost { Title = "Post your upload here", Author = "devv", Replies = 17, Pinned = true },
        new ForumPost { Title = "Best tracks of the week?", Author = "beef", Replies = 9 },
        new ForumPost { Title = "How do rings work?", Author = "you", Replies = 3 },
    };

    public static List<FriendCard> Chats { get; } = new()
    {
        new FriendCard { Name = "ryan",  RoleIcon = "avares://Trackify/Assets/Icons/owner.png",   VerifiedIcon = "avares://Trackify/Assets/Icons/verified.png", PlanIcon = "avares://Trackify/Assets/Icons/token.png" },
        new FriendCard { Name = "devv",  VerifiedIcon = "avares://Trackify/Assets/Icons/verified.png" },
        new FriendCard { Name = "beef",  StatusIcon = "avares://Trackify/Assets/Icons/idle.png" },
        new FriendCard { Name = "mira" },
        new FriendCard { Name = "nokta", StatusIcon = "avares://Trackify/Assets/Icons/offline.png" },
    };

    public static List<ChatMessage> Messages { get; } = new()
    {
        new ChatMessage { Author = "ryan", Text = "yo, new build is smooth", Time = "18:02", PlanIcon = "avares://Trackify/Assets/Icons/token.png", RoleIcon = "avares://Trackify/Assets/Icons/owner.png", VerifiedIcon = "avares://Trackify/Assets/Icons/verified.png" },
        new ChatMessage { Author = "you",  Text = "the offline cache is a game changer", Time = "18:04" },
        new ChatMessage { Author = "devv", Text = "uploaded two new tracks to TrackCloud", Time = "18:10", VerifiedIcon = "avares://Trackify/Assets/Icons/verified.png" },
    };

    public static string[] BannerColors { get; } =
        { "#7C3AED", "#8B5CF6", "#EC4899", "#F59E0B", "#10B981", "#3B82F6", "#EF4444", "#0EA5E9" };

    public static List<PlanItem> Plans { get; } = new()
    {
        new PlanItem { Sprite = "free",  Name = "Free",  PriceNumber = "0",   Active = true,  Button = "Current",
            Features = "Listen to everything on TrackCloud\n3 playlists\nBuilt-in avatar rings" },
        new PlanItem { Sprite = "pro",   Name = "Pro",   PriceNumber = "120", Button = "Get Pro",
            Features = "Upload music to TrackCloud\nDownloads cost listeners 50 §\n3 playlists" },
        new PlanItem { Sprite = "max",   Name = "Max",   PriceNumber = "300", Button = "Get Max",
            Features = "Uploads free to download\nSell your gifts on the market\n3 playlists" },
        new PlanItem { Sprite = "ultra", Name = "Ultra", PriceNumber = "700", Button = "Get Ultra",
            Features = "Custom animated avatar ring\nUp to 5 usernames, sell them\nBanner emoji\nSell gifts" },
    };
}
