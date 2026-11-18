using System.Text.Json.Serialization;

namespace Trackify.Models;

public class Track
{
    public Guid? Id { get; set; }
    public string Title { get; set; } = "";
    public string Artist { get; set; } = "";
    public string Duration { get; set; } = "0:00";
    public string Source { get; set; } = "TrackCloud";
    public string Color { get; set; } = "#8B5CF6";
    public string? StoragePath { get; set; }
    public string? LocalPath { get; set; }
    public string? Lyrics { get; set; }

    [JsonIgnore] public bool IsOffline => LocalPath != null;
    [JsonIgnore] public string Initial => string.IsNullOrEmpty(Title) ? "?" : Title[..1].ToUpperInvariant();
    public static string MmSs(int sec) => $"{sec / 60}:{sec % 60:D2}";
}

public class Gift
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public int Price { get; set; }
    public bool Unique { get; set; }
    public string VideoPath { get; set; } = "";
}

public class Game
{
    public string Name { get; set; } = "";
    public string Glyph { get; set; } = "";
    public string Description { get; set; } = "";
    public string Kind { get; set; } = "soon";   // reaction | clicker | soon
}

public class ForumPost
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string Author { get; set; } = "";
    public int Replies { get; set; }
    public bool Pinned { get; set; }
    public string Meta => $"{Author} · {Replies} replies";
}

public class FriendCard
{
    public string Name { get; set; } = "";
    public string Initial => string.IsNullOrEmpty(Name) ? "?" : Name[..1].ToUpperInvariant();
    public string StatusIcon { get; set; } = "avares://Trackify/Assets/Icons/online.png";
    public string RoleIcon { get; set; } = "";
    public string VerifiedIcon { get; set; } = "";
    public string PlanIcon { get; set; } = "";
}

public class ChatMessage
{
    public string Author { get; set; } = "";
    public string Text { get; set; } = "";
    public string Time { get; set; } = "";
    public string Plan { get; set; } = "free";
    public string RoleIcon { get; set; } = "";
    public string VerifiedIcon { get; set; } = "";
    public string PlanIcon { get; set; } = "";
}

public class PlanItem
{
    public string Sprite { get; set; } = "free";
    public string Name { get; set; } = "";
    public string PriceNumber { get; set; } = "0";
    public string Features { get; set; } = "";
    public string Button { get; set; } = "Current";
    public bool Active { get; set; }
}
