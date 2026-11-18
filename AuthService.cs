using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using Supabase.Gotrue;

namespace Trackify.Services;

public enum AuthResult { Ok, Cancelled, Banned, AltNotSupported, Failed }

/// TrackCloud accounts live in Supabase Auth. Google sign-in runs through Supabase OAuth
/// with a PKCE loopback (add http://localhost:53682/callback to Supabase redirect URLs).
public static class AuthService
{
    public static string LastError { get; private set; } = "";

    public static async Task<AuthResult> SignInEmailAsync(string email, string password)
    {
        try
        {
            var s = await Backend.Client.Auth.SignIn(email, password);
            return await FinishAsync(s);
        }
        catch (Exception ex) { LastError = ex.Message; return AuthResult.Failed; }
    }

    public static async Task<AuthResult> SignUpAsync(string email, string password)
    {
        try
        {
            var s = await Backend.Client.Auth.SignUp(email, password);
            if (s?.User == null || Backend.Client.Auth.CurrentSession == null)
            { LastError = "Confirm your email first, then sign in."; return AuthResult.Failed; }
            return await FinishAsync(s);
        }
        catch (Exception ex) { LastError = ex.Message; return AuthResult.Failed; }
    }

    public static async Task<AuthResult> SignInWithGoogleAsync(CancellationToken ct = default)
    {
        try
        {
            var res = await Backend.Client.Auth.SignInWithOAuth(Provider.Google,
                new SignInWithOAuthOptions { RedirectTo = Config.RedirectUri, FlowType = OAuthFlowType.PKCE });
            if (res?.Uri == null) { LastError = "Google login is not configured."; return AuthResult.Failed; }

            using var listener = new HttpListener();
            listener.Prefixes.Add($"http://localhost:{Config.LoopbackPort}/");
            listener.Start();
            Process.Start(new ProcessStartInfo(res.Uri.ToString()) { UseShellExecute = true });

            var ctxTask = listener.GetContextAsync();
            if (await Task.WhenAny(ctxTask, Task.Delay(TimeSpan.FromMinutes(3), ct)) != ctxTask)
                return AuthResult.Cancelled;

            var ctx = ctxTask.Result;
            var code = ctx.Request.QueryString["code"];
            var html = Encoding.UTF8.GetBytes("<html><body style='background:#0E0E10;color:#fff;font-family:sans-serif;text-align:center;padding-top:20vh'><h2>You can close this tab and return to Trackify.</h2></body></html>");
            ctx.Response.ContentType = "text/html; charset=utf-8";
            await ctx.Response.OutputStream.WriteAsync(html, ct);
            ctx.Response.Close();
            if (string.IsNullOrEmpty(code)) return AuthResult.Failed;

            await Backend.Client.Auth.ExchangeCodeForSession(code);
            return await FinishAsync(Backend.Client.Auth.CurrentSession);
        }
        catch (Exception ex) { LastError = ex.Message; return AuthResult.Failed; }
    }

    static async Task<AuthResult> FinishAsync(Session? s)
    {
        if (s?.User == null) { LastError = "Sign-in failed."; return AuthResult.Failed; }
        try
        {
            var display = s.User.UserMetadata?.GetProperty("full_name").GetString()
                          ?? s.User.UserMetadata?.GetProperty("name").GetString() ?? "";
            await Backend.RpcAsync("ensure_profile", new Dictionary<string, object?>
                { ["p_device"] = Device.Hash(), ["p_display"] = display });
        }
        catch (ServiceException ex) when (ex.Message.Contains("alt_not_supported"))
        {
            await LocalWipeAsync();
            return AuthResult.AltNotSupported;
        }

        await Backend.LoadMeAsync();
        if (Backend.Me == null) { LastError = "Could not load your profile."; return AuthResult.Failed; }
        if (Backend.Me.Banned) { await LocalWipeAsync(); return AuthResult.Banned; }

        Persist();
        await Backend.LoadBlockedWordsAsync();
        return AuthResult.Ok;
    }

    public static async Task<bool> TryRestoreAsync()
    {
        var raw = Secure.Load("session");
        if (raw == null) return false;
        try
        {
            var s = JsonSerializer.Deserialize<Dictionary<string, string>>(raw)!;
            await Backend.Client.Auth.SetSession(s["access"], s["refresh"]);
            if (Backend.Client.Auth.CurrentSession == null) return false;
            await Backend.LoadMeAsync();
            if (Backend.Me == null || Backend.Me.Banned) { await LocalWipeAsync(); return false; }
            await Backend.LoadBlockedWordsAsync();
            return true;
        }
        catch { return false; }
    }

    public static async Task SignOutAsync() => await LocalWipeAsync();

    static async Task LocalWipeAsync()
    {
        try { await Backend.Client.Auth.SignOut(); } catch { }
        Secure.Delete("session");
    }

    static void Persist()
    {
        var s = Backend.Client.Auth.CurrentSession;
        if (s == null) return;
        Secure.Save("session", JsonSerializer.Serialize(new Dictionary<string, string>
            { ["access"] = s.AccessToken!, ["refresh"] = s.RefreshToken! }));
    }
}
