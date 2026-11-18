using System.Text.RegularExpressions;

namespace Trackify.Services;

public class LyricLine
{
    public TimeSpan Time;
    public string Text = "";
    public List<(TimeSpan Time, string Text)> Words = new();
}

/// Parses standard LRC and enhanced (word-tagged) LRC: <mm:ss.xx>word
public static class LyricsService
{
    static readonly Regex LineTag = new(@"\[(\d+):(\d+(?:\.\d+)?)\]", RegexOptions.Compiled);
    static readonly Regex WordTag = new(@"<(\d+):(\d+(?:\.\d+)?)>([^<]*)", RegexOptions.Compiled);

    static TimeSpan Ts(int m, double s) => TimeSpan.FromSeconds(m * 60 + s);

    public static List<LyricLine> Parse(string lrc)
    {
        var lines = new List<LyricLine>();
        foreach (var raw in lrc.Split('\n'))
        {
            var tags = LineTag.Matches(raw);
            if (tags.Count == 0) continue;
            var body = LineTag.Replace(raw, "").TrimEnd();
            var words = WordTag.Matches(body)
                .Select(m => (Ts(int.Parse(m.Groups[1].Value), double.Parse(m.Groups[2].Value)), m.Groups[3].Value))
                .ToList();
            var text = words.Count > 0 ? string.Concat(words.Select(w => w.Item2)) : body;
            foreach (Match tag in tags)
            {
                var line = new LyricLine { Time = Ts(int.Parse(tag.Groups[1].Value), double.Parse(tag.Groups[2].Value)), Text = text, Words = words };
                int pos = lines.FindIndex(l => l.Time > line.Time);
                if (pos < 0) lines.Add(line); else lines.Insert(pos, line);
            }
        }
        return lines;
    }

    /// Current line + index of the active word (karaoke highlight).
    public static (LyricLine? line, int wordIndex) At(List<LyricLine> lines, TimeSpan pos)
    {
        if (lines.Count == 0) return (null, -1);
        int i = lines.FindLastIndex(l => l.Time <= pos);
        if (i < 0) return (null, -1);
        var line = lines[i];
        int w = line.Words.FindLastIndex(x => x.Time <= pos);
        return (line, w);
    }
}
