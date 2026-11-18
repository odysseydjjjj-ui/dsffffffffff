using System.Text.RegularExpressions;

namespace Trackify.Services;

/// Client-side mirror of the server filter (sql/schema.sql). It only gives instant feedback;
/// the server is authoritative and is the one that applies the 5-10 minute mute.
public static class ContentFilter
{
    static readonly Regex Link = new(
        @"(https?://|www\.|discord\.gg|[a-z0-9-]+\.(com|net|org|ru|io|gg|me|ly|tv|xyz|co|to|app|dev|link|ua|by|lt|su)\b)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    static readonly Regex DotWord = new(@"\s*(\[dot\]|\(dot\)|\{dot\}|\sdot\s)\s*", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    static readonly Regex DotSpaces = new(@"\s*\.\s*", RegexOptions.Compiled);
    static readonly Regex Repeats = new(@"(.)\1+", RegexOptions.Compiled);

    static HashSet<string> blocked = new();

    /// Normalised words, loaded from the server (rpc get_blocked_words). The client ships no word list.
    public static void SetBlocked(IEnumerable<string> normalisedWords) => blocked = new HashSet<string>(normalisedWords);

    public static string Normalize(string t)
    {
        var s = t.ToLowerInvariant();
        var map = "0134578@$";
        var to = "oieastbas";
        var chars = s.Select(c => { var i = map.IndexOf(c); return i >= 0 ? to[i] : c; });
        s = new string(chars.Where(char.IsLetterOrDigit).ToArray());
        return Repeats.Replace(s, "$1");
    }

    public static bool HasLink(string t)
    {
        var s = DotSpaces.Replace(DotWord.Replace(t, "."), ".");
        return Link.IsMatch(s);
    }

    public static string? Check(string text)
    {
        if (HasLink(text)) return "Links are not allowed.";
        var n = Normalize(text);
        foreach (var w in blocked) if (n.Contains(w)) return "That word is not allowed.";
        return null;
    }
}
