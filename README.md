# Trackify

Open-source native music player (Avalonia UI, C# .NET 8). Windows first, Android/iOS later from the same codebase.

Stage 1 (interface) and stage 2 (features) are done. All music now comes from **TrackCloud** — our own
Supabase-backed upload platform. SoundCloud / Yandex Music / VK Music integrations were removed.

## What's inside

- **TrackCloud** — every track is an upload in Supabase Storage (`tracks` bucket). Search via `search_uploads` RPC.
- **Offline mode** — played tracks are cached to `%AppData%/Trackify/cache` (2 GB LRU) and play without internet.
- **Auth** — Supabase Auth: email/password or Google (PKCE loopback). One account per device (anti-alt).
- **Discord Rich Presence** — shows the current track (set `DiscordAppId` in `Services/Core.cs`).
- **Auto-update** — Velopack from GitHub Releases (set `UpdateRepoUrl` in `Services/Core.cs`).
- **Tokens** — § earned by listening (1 § / 2 min, 300 §/day cap), spent on plans/gifts.
- **Plans (3 days each)** — Free · Pro 120 § (uploads, downloads cost 50 §) · Max 300 § (free downloads, sell gifts) · Ultra 700 § (custom ring, banner emoji, 5 usernames, sell them).
- **Catalog** — silent MP4 gift loops; uploads of gifts are admin-only (@ryan @devv @beef @trackify). Unique gifts pin to the profile banner with Glow.
- **Forum + chat** — server-side filter: links/slurs = 5–10 min mute. Chat runs on Supabase Realtime.
- **Lyrics** — LRC and word-tagged (enhanced) LRC, karaoke highlight in the player panel.
- **Games** — Reaction and Clicker are playable; more in stage 3.
- Dynamic background follows track colors (can be disabled), tray on close, avatar crop hook, plan rings.

## Setup

1. Create a Supabase project, run `schema.sql` in the SQL editor.
2. In Supabase Auth settings add redirect URL `http://localhost:53682/callback` (for Google sign-in).
3. Put your keys in `Services/Core.cs`: `SupabaseUrl`, `SupabaseAnonKey`, (optional) `DiscordAppId`, `UpdateRepoUrl`.
4. Add admin emails: `insert into admin_whitelist (email, username, role) values ('you@gmail.com', 'ryan', 'owner');`
5. `dotnet run`

## Build exe

`dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true`

## Stages

1. Interface — done
2. Features — done (this release, v0.3.0)
3. Server conversion — next: stream tracks through an edge function so files are never exposed via signed URLs; move games/render to server-authoritative logic
4. Phone port — Avalonia mobile target, replace DPAPI (`Secure`) with platform keychain, `Device.Hash` per-install GUID
5. GitHub release — Velopack channels, CI build

## Where the music lives

Track files: Supabase Storage bucket `tracks` (private, signed URLs). Fine for launch.
For scale, mirror the bucket to **Cloudflare R2** (S3-compatible, zero egress fees) or **Backblaze B2**,
keep Supabase as the metadata/auth layer, and serve audio from the CDN.
