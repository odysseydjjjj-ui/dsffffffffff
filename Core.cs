using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Win32;

namespace Trackify.Services;

public static class Config
{
    // All of these are public by design. Secrets (SoundCloud client_secret, service_role) live ONLY in the edge function.
    public const string SupabaseUrl = "https://YOUR-PROJECT.supabase.co";
    public const string SupabaseAnonKey = "YOUR-ANON-KEY";
    public const string SoundCloudClientId = "YOUR-SC-CLIENT-ID";
    public const string DiscordAppId = "YOUR-DISCORD-APP-ID";
    public const string UpdateRepoUrl = "https://github.com/YOUR-NAME/trackify";
    public const int LoopbackPort = 53682;                        // register http://localhost:53682/callback in the SoundCloud app
    public const string RedirectUri = "http://localhost:53682/callback";

    public static readonly string DataDir = Directory.CreateDirectory(
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Trackify")).FullName;

    public static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };
}

public class ServiceException(string message) : Exception(message);

public static class Device
{
    /// Heuristic anti-alt: one hashed machine id per account. A VM or reinstalled Windows can bypass it.
    public static string Hash()
    {
        var guid = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography")?.GetValue("MachineGuid") as string
                   ?? Environment.MachineName;
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("trackify:" + guid))).ToLowerInvariant();
    }
}

public static class Secure
{
    static string PathOf(string name) => Path.Combine(Config.DataDir, name + ".bin");

    public static void Save(string name, string value) =>
        File.WriteAllBytes(PathOf(name), ProtectedData.Protect(Encoding.UTF8.GetBytes(value), null, DataProtectionScope.CurrentUser));

    public static string? Load(string name)
    {
        try { return File.Exists(PathOf(name)) ? Encoding.UTF8.GetString(ProtectedData.Unprotect(File.ReadAllBytes(PathOf(name)), null, DataProtectionScope.CurrentUser)) : null; }
        catch { return null; }
    }

    public static void Delete(string name) { try { File.Delete(PathOf(name)); } catch { } }
}
