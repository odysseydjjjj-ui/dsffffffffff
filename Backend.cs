using System.Text.Json;
using Postgrest.Attributes;
using Postgrest.Models;
using Supabase.Realtime.PostgresChanges;
using Trackify.Models;

namespace Trackify.Services;

public class Profile
{
    public Guid Id { get; set; }
    public long NumberId { get; set; }
    public string Username { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string? AvatarUrl { get; set; }
    public string BannerColor { get; set; } = "#7C3AED";
    public string? BannerEmoji { get; set; }
    public Guid? PinnedGift { get; set; }
    public int Ring { get; set; }
    public string? RingCustom { get; set; }
    public string Plan { get; set; } = "free";
    public DateTime? PlanExpires { get; set; }
    public int Tokens { get; set; }
    public DateTime? MutedUntil { get; set; }
    public bool IsAdmin { get; set; }
    public string? Role { get; set; }
    public bool Verified { get; set; }

    public string EffectivePlan => Plan != "free" && PlanExpires > DateTime.UtcNow ? Plan : "free";
    public int PlanRank => EffectivePlan switch { "pro" => 1, "max" => 2, "ultra" => 3, _ => 0 };
    public bool IsMuted => MutedUntil > DateTime.UtcNow;
}

public record SendResult(bool Ok, string? Error, DateTime? MutedUntil)
{
    public string Message => Error switch
    {
        "muted" => $"You are muted until {MutedUntil?.ToLocalTime():HH:mm}.",
        "filtered" => $"Links and slurs are not allowed. Muted until {MutedUntil?.ToLocalTime():HH:mm}.",
        null => "",
        _ => Error
    };
}

public class CatalogGift { public Guid Id { get; set; } public string Name { get; set; } = ""; public string VideoPath { get; set; } = ""; public int Price { get; set; } public bool IsUnique { get; set; } }
public class OwnedGift { public Guid Id { get; set; } public Guid GiftId { get; set; } public string Name { get; set; } = ""; public string VideoPath { get; set; } = ""; public bool IsUnique { get; set; } public int? SalePrice { get; set; } }
public class MarketListing { public Guid Id { get; set; } public string Name { get; set; } = ""; public string VideoPath { get; set; } = ""; public bool IsUnique { get; set; } public int Price { get; set; } public string Seller { get; set; } = ""; }
public class ForumPostDto { public Guid Id { get; set; } public string Title { get; set; } = ""; public string Author { get; set; } = ""; public int Replies { get; set; } public bool Pinned { get; set; } }
class ChatDto { public string Text { get; set; } = ""; public DateTime CreatedAt { get; set; } public string Author { get; set; } = ""; public string? Plan { get; set; } public string? Role { get; set; } public bool Verified { get; set; } }

[Table("chat_messages")]
public class ChatRow : BaseModel
{
    [PrimaryKey("id")] public long Id { get; set; }
    [Column("room")] public string Room { get; set; } = "";
    [Column("author")] public Guid Author { get; set; }
    [Column("text")] public string Text { get; set; } = "";
    [Column("created_at")] public DateTime CreatedAt { get; set; }
}

public static class Backend
{
    public static Supabase.Client Client { get; private set; } = null!;
    public static Profile? Me { get; private set; }
    public static event Action<Profile?>? MeChanged;

    static bool inited;

    public static async Task InitAsync()
    {
        if (inited) return;
        inited = true;
        Client = new Supabase.Client(Config.SupabaseUrl, Config.SupabaseAnonKey,
            new Supabase.SupabaseOptions { AutoRefreshToken = true, AutoConnectRealtime = true });
        await Client.InitializeAsync();
    }

    // ---------- RPC plumbing
    public static async Task<JsonElement> RpcAsync(string fn, Dictionary<string, object?>? args = null)
    {
        try
        {
            var r = await Client.Rpc(fn, args ?? new Dictionary<string, object?>());
            var s = r?.Content;
            if (string.IsNullOrWhiteSpace(s)) return default;
            using var doc = JsonDocument.Parse(s);
            return doc.RootElement.Clone();
        }
        catch (Postgrest.Exceptions.PostgrestException ex) { throw new ServiceException(ExtractMessage(ex.Message)); }
    }

    static string ExtractMessage(string raw)
    {
        try { using var d = JsonDocument.Parse(raw); if (d.RootElement.TryGetProperty("message", out var m)) return m.GetString() ?? raw; } catch { }
        return raw;
    }

    static async Task<T> RpcAsync<T>(string fn, Dictionary<string, object?>? args = null) where T : new()
    {
        var e = await RpcAsync(fn, args);
        return e.ValueKind == JsonValueKind.Undefined || e.ValueKind == JsonValueKind.Null ? new T() : e.Deserialize<T>(Config.Json)!;
    }

    static Dictionary<string, object?> A(params (string, object?)[] kv) => kv.ToDictionary(x => x.Item1, x => x.Item2);

    static SendResult ToSend(JsonElement e)
    {
        if (e.TryGetProperty("error", out var er))
            return new SendResult(false, er.GetString(), e.TryGetProperty("muted_until", out var m) && m.ValueKind == JsonValueKind.String ? m.GetDateTime().ToUniversalTime() : null);
        return new SendResult(true, null, null);
    }

    // ---------- profile
    public static async Task<Profile?> LoadMeAsync()
    {
        var e = await RpcAsync("get_me");
        Me = e.ValueKind == JsonValueKind.Object ? e.Deserialize<Profile>(Config.Json) : null;
        MeChanged?.Invoke(Me);
        return Me;
    }

    public static async Task<Profile?> GetProfileAsync(string username)
    {
        var e = await RpcAsync("get_profile", A(("p_username", username)));
        return e.ValueKind == JsonValueKind.Object ? e.Deserialize<Profile>(Config.Json) : null;
    }

    public static async Task SetBannerAsync(string hex, string? emoji) { await RpcAsync("set_banner", A(("p_color", hex), ("p_emoji", emoji))); await LoadMeAsync(); }
    public static async Task SetRingAsync(int index, string? customHex) { await RpcAsync("set_ring", A(("p_index", index), ("p_custom", customHex))); await LoadMeAsync(); }
    public static async Task SetDisplayNameAsync(string name) { await RpcAsync("set_display_name", A(("p_name", name))); await LoadMeAsync(); }
    public static async Task PinGiftAsync(Guid? giftId) { await RpcAsync("pin_gift", A(("p_gift", giftId))); await LoadMeAsync(); }

    public static async Task UploadAvatarAsync(byte[] png)
    {
        var path = $"{Me!.Id}/avatar.png";
        await Client.Storage.From("avatars").Upload(png, path, new Supabase.Storage.FileOptions { Upsert = true, ContentType = "image/png" });
        var url = Client.Storage.From("avatars").GetPublicUrl(path) + "?v=" + DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        await RpcAsync("set_avatar", A(("p_url", url)));
        await LoadMeAsync();
    }

    // ---------- tokens / plans
    public static async Task<int> EarnListenAsync()
    {
        var e = await RpcAsync("earn_listen");
        if (Me != null && e.TryGetProperty("tokens", out var t)) { Me.Tokens = t.GetInt32(); MeChanged?.Invoke(Me); }
        return e.TryGetProperty("earned", out var g) ? g.GetInt32() : 0;
    }

    public static async Task BuyPlanAsync(string plan) { await RpcAsync("buy_plan", A(("p_plan", plan))); await LoadMeAsync(); }

    // ---------- chat (Supabase realtime)
    static readonly Dictionary<Guid, ChatMessage> cards = new();

    public static async Task<List<ChatMessage>> GetChatAsync(string room, int limit = 50)
    {
        var list = await RpcAsync<List<ChatDto>>("get_chat", A(("p_room", room), ("p_limit", limit)));
        return list.Select(d => new ChatMessage { Author = d.Author, Text = d.Text, Time = d.CreatedAt.ToLocalTime().ToString("HH:mm"), Plan = d.Plan ?? "free", Role = d.Role ?? "", Verified = d.Verified }).ToList();
    }

    public static async Task<SendResult> SendMessageAsync(string room, string text)
    {
        var pre = ContentFilter.Check(text);
        if (Me?.IsMuted == true) return new SendResult(false, "muted", Me.MutedUntil);
        var r = ToSend(await RpcAsync("send_message", A(("p_room", room), ("p_text", text))));
        if (!r.Ok) await LoadMeAsync();                     // pick up the new muted_until
        else if (pre != null) { /* client filter was stricter than server: ignore */ }
        return r;
    }

    /// Calls onMessage on a background thread for every new message in the room. Dispose the result to unsubscribe.
    public static async Task<IDisposable> SubscribeChatAsync(string room, Action<ChatMessage> onMessage)
    {
        var channel = Client.Realtime.Channel("room-" + room);
        channel.Register(new PostgresChangesOptions("public", "chat_messages", PostgresChangesOptions.ListenType.Inserts, $"room=eq.{room}"));
        channel.AddPostgresChangeHandler(PostgresChangesOptions.ListenType.Inserts, async (_, change) =>
        {
            var row = change.Model<ChatRow>();
            if (row == null) return;
            if (!cards.TryGetValue(row.Author, out var card))
            {
                var e = await RpcAsync("get_card", A(("p_uid", row.Author)));
                card = new ChatMessage
                {
                    Author = e.GetProperty("username").GetString() ?? "?",
                    Plan = e.GetProperty("plan").GetString() ?? "free",
                    Role = e.TryGetProperty("role", out var r) && r.ValueKind == JsonValueKind.String ? r.GetString()! : "",
                    Verified = e.TryGetProperty("verified", out var v) && v.GetBoolean()
                };
                cards[row.Author] = card;
            }
            onMessage(new ChatMessage { Author = card.Author, Plan = card.Plan, Role = card.Role, Verified = card.Verified, Text = row.Text, Time = row.CreatedAt.ToLocalTime().ToString("HH:mm") });
        });
        await channel.Subscribe();
        return new Unsub(() => channel.Unsubscribe());
    }

    sealed class Unsub(Action a) : IDisposable { public void Dispose() => a(); }

    // ---------- forum
    public static Task<List<ForumPostDto>> GetForumAsync() => RpcAsync<List<ForumPostDto>>("get_forum");
    public static async Task<SendResult> CreatePostAsync(string title, string body)
    {
        var r = ToSend(await RpcAsync("create_post", A(("p_title", title), ("p_body", body))));
        if (!r.Ok) await LoadMeAsync();
        return r;
    }
    public static async Task<SendResult> ReplyAsync(Guid post, string body)
    {
        var r = ToSend(await RpcAsync("reply_post", A(("p_post", post), ("p_body", body))));
        if (!r.Ok) await LoadMeAsync();
        return r;
    }

    // ---------- uploads
    public static async Task<Guid> UploadTrackAsync(string filePath, string title, string artist)
    {
        if (Me == null || Me.PlanRank < 1) throw new ServiceException("Pro or higher is required to upload.");
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        if (ext is not (".mp3" or ".ogg" or ".flac" or ".wav" or ".m4a")) throw new ServiceException("Unsupported audio format.");
        var info = new FileInfo(filePath);
        if (info.Length > 50 * 1024 * 1024) throw new ServiceException("Max file size is 50 MB.");
        int dur = 0;
        try { using var f = TagLib.File.Create(filePath); dur = (int)f.Properties.Duration.TotalSeconds; } catch { }
        var path = $"{Me.Id}/{Guid.NewGuid():N}{ext}";
        await Client.Storage.From("tracks").Upload(await File.ReadAllBytesAsync(filePath), path, new Supabase.Storage.FileOptions { ContentType = "audio/mpeg" });
        var e = await RpcAsync("register_track", A(("p_title", title), ("p_artist", artist), ("p_path", path), ("p_duration", dur)));
        return e.GetGuid();
    }

    public static async Task<string> SignedUrlAsync(string bucket, string path, int seconds = 3600) =>
        await Client.Storage.From(bucket).CreateSignedUrl(path, seconds);

    /// Charges 50 § for Pro uploads (free for Max/Ultra uploads), then saves the file to destDir.
    /// NOTE: the file is also streamable, so this is a "soft" paywall. Hard enforcement = stage 3 (stream via edge function).
    public static async Task<string> DownloadTrackAsync(Guid trackId, string title, string destDir)
    {
        var path = (await RpcAsync("request_download", A(("p_track", trackId)))).GetString()!;
        var url = await SignedUrlAsync("tracks", path);
        using var http = new HttpClient();
        var bytes = await http.GetByteArrayAsync(url);
        var safe = string.Concat(title.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
        var dest = Path.Combine(destDir, safe + Path.GetExtension(path));
        await File.WriteAllBytesAsync(dest, bytes);
        await LoadMeAsync();
        return dest;
    }

    // ---------- catalog / gifts
    public static Task<List<CatalogGift>> GetCatalogAsync() => RpcAsync<List<CatalogGift>>("get_catalog");
    public static Task<List<OwnedGift>> GetMyGiftsAsync() => RpcAsync<List<OwnedGift>>("get_my_gifts");
    public static Task<List<MarketListing>> GetMarketAsync() => RpcAsync<List<MarketListing>>("get_market");
    public static async Task BuyGiftAsync(Guid gift) { await RpcAsync("buy_gift", A(("p_gift", gift))); await LoadMeAsync(); }
    public static Task ListGiftAsync(Guid userGift, int? price) => RpcAsync("list_gift", A(("p_user_gift", userGift), ("p_price", price)));
    public static async Task BuyListingAsync(Guid userGift) { await RpcAsync("buy_listing", A(("p_user_gift", userGift))); await LoadMeAsync(); }

    /// Admin-only (enforced by RLS on storage + add_gift). MP4 must have NO audio track.
    public static async Task AddGiftAsync(string mp4Path, string name, int price, bool unique)
    {
        GiftValidator.Validate(mp4Path);
        var path = $"{Me!.Id}/{Guid.NewGuid():N}.mp4";
        await Client.Storage.From("gifts").Upload(await File.ReadAllBytesAsync(mp4Path), path, new Supabase.Storage.FileOptions { ContentType = "video/mp4" });
        await RpcAsync("add_gift", A(("p_name", name), ("p_path", path), ("p_price", price), ("p_unique", unique)));
    }

    // ---------- usernames
    public static async Task ClaimUsernameAsync(string name) { await RpcAsync("claim_username", A(("p_name", name))); await LoadMeAsync(); }
    public static async Task SetPrimaryUsernameAsync(string name) { await RpcAsync("set_primary_username", A(("p_name", name))); await LoadMeAsync(); }
    public static Task ListUsernameAsync(string name, int? price) => RpcAsync("list_username", A(("p_name", name), ("p_price", price)));
    public static async Task BuyUsernameAsync(string name) { await RpcAsync("buy_username", A(("p_name", name))); await LoadMeAsync(); }

    public static async Task LoadBlockedWordsAsync()
    {
        var e = await RpcAsync("get_blocked_words");
        if (e.ValueKind == JsonValueKind.Array) ContentFilter.SetBlocked(e.EnumerateArray().Select(x => x.GetString()!).Where(x => x != null));
    }

    public static Task<JsonElement> AdminAsync(string action, string? username = null, int? value = null, string? text = null) =>
        RpcAsync("admin_action", A(("p_action", action), ("p_username", username), ("p_value", value), ("p_text", text)));
}
