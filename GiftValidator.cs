namespace Trackify.Services;

/// Gifts are silent MP4 loops, max 25 MB, admins only (@ryan @devv @beef @trackify).
public static class GiftValidator
{
    public static void Validate(string mp4Path)
    {
        var fi = new FileInfo(mp4Path);
        if (!fi.Exists) throw new ServiceException("File not found.");
        if (!fi.Extension.Equals(".mp4", StringComparison.OrdinalIgnoreCase)) throw new ServiceException("Gifts must be MP4.");
        if (fi.Length > 25 * 1024 * 1024) throw new ServiceException("Max gift size is 25 MB.");
        try
        {
            using var f = TagLib.File.Create(mp4Path);
            if (f.Properties.AudioTracks > 0) throw new ServiceException("Gifts must have no audio track.");
        }
        catch (ServiceException) { throw; }
        catch { /* unreadable metadata: let the server re-check */ }
    }
}
