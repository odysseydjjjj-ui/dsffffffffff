-- Trackify schema, stage 1-2 (TrackCloud). Run in the Supabase SQL editor.
-- Clients have SELECT only via RLS; all writes go through SECURITY DEFINER functions.
create extension if not exists pgcrypto;

---------------------------------------------------------------- tables
create table profiles (
  id uuid primary key references auth.users(id) on delete cascade,
  number_id bigint generated always as identity unique,            -- public ID
  device_hash text unique,                                          -- heuristic anti-alt
  username text unique not null,                                    -- @username (primary)
  display_name text not null,
  avatar_url text,
  banner_color text not null default '#7C3AED',
  banner_emoji text,                                                -- Ultra
  pinned_gift uuid,                                                 -- gifts.id, must be "unique" gift
  ring int not null default 0,                                      -- built-in ring index
  ring_custom text,                                                 -- Ultra: "#hex1,#hex2"
  plan text not null default 'free' check (plan in ('free','pro','max','ultra')),
  plan_expires timestamptz,
  tokens int not null default 0 check (tokens >= 0),
  muted_until timestamptz,
  banned boolean not null default false,
  is_admin boolean not null default false,
  role text check (role in ('owner','mod')),
  verified boolean not null default false,
  created_at timestamptz not null default now()
);

-- console access for @ryan @devv @beef @trackify: add their Google/email addresses here
create table admin_whitelist (email text primary key, username text unique not null, role text not null default 'mod');
create table blocked_words (word_norm text primary key);
create table listen_state (user_id uuid primary key references profiles(id) on delete cascade, last_beat timestamptz, carry int not null default 0, day date not null default current_date, earned_today int not null default 0);

create table usernames (name text primary key, owner uuid not null references profiles(id) on delete cascade, price int, is_primary boolean not null default false);
create table reserved_usernames (name text primary key);
insert into reserved_usernames values ('ryan'),('devv'),('beef'),('trackify'),('admin'),('support'),('mod');

-- TrackCloud: all music lives here
create table tracks (id uuid primary key default gen_random_uuid(), owner uuid not null references profiles(id) on delete cascade,
  title text not null, artist text not null, path text not null, duration int, lyrics text,
  download_price int not null default 50, created_at timestamptz default now());
create table track_unlocks (user_id uuid references profiles(id) on delete cascade, track_id uuid references tracks(id) on delete cascade, primary key (user_id, track_id));

create table gifts (id uuid primary key default gen_random_uuid(), name text not null, video_path text not null, price int not null check (price > 0), is_unique boolean not null default false, created_by uuid references profiles(id));
create table user_gifts (id uuid primary key default gen_random_uuid(), owner uuid not null references profiles(id) on delete cascade, gift_id uuid not null references gifts(id) on delete cascade, sale_price int, created_at timestamptz default now());

create table chat_messages (id bigint generated always as identity primary key, room text not null, author uuid not null references profiles(id) on delete cascade, text text not null check (char_length(text) between 1 and 500), created_at timestamptz not null default now());
create index on chat_messages (room, id desc);
create table forum_posts (id uuid primary key default gen_random_uuid(), author uuid not null references profiles(id) on delete cascade, title text not null check (char_length(title) between 3 and 120), body text not null check (char_length(body) <= 4000), pinned boolean not null default false, created_at timestamptz default now());
create table forum_replies (id uuid primary key default gen_random_uuid(), post_id uuid not null references forum_posts(id) on delete cascade, author uuid not null references profiles(id) on delete cascade, body text not null check (char_length(body) <= 2000), created_at timestamptz default now());

---------------------------------------------------------------- RLS: read-only for clients
do $$ declare t text; begin
  foreach t in array array['profiles','usernames','tracks','gifts','user_gifts','chat_messages','forum_posts','forum_replies'] loop
    execute format('alter table %I enable row level security', t);
    execute format('create policy "read" on %I for select to authenticated using (true)', t);
  end loop;
end $$;
create policy "own unlocks" on track_unlocks for select to authenticated using (user_id = auth.uid());
alter table track_unlocks enable row level security;
alter table admin_whitelist enable row level security;  -- no policies = service role only
alter table blocked_words enable row level security;
alter table listen_state enable row level security;
alter table reserved_usernames enable row level security;
-- profiles hold private columns (tokens, device_hash): clients read them only via get_me / get_profile
revoke select on profiles from authenticated, anon;

---------------------------------------------------------------- helpers
create function eff_plan(p_uid uuid) returns text language sql stable security definer as $$
  select case when plan_expires > now() then plan else 'free' end from profiles where id = p_uid $$;
create function plan_rank(p text) returns int language sql immutable as $$
  select case p when 'pro' then 1 when 'max' then 2 when 'ultra' then 3 else 0 end $$;
create function is_admin_uid(p_uid uuid) returns boolean language sql stable security definer as $$
  select coalesce((select is_admin from profiles where id = p_uid), false) $$;

create function collapse(t text) returns text language sql immutable as $$ select regexp_replace(t, '(.)\1+', '\1', 'g') $$;
create function norm_text(t text) returns text language sql immutable as $$
  select collapse(regexp_replace(translate(lower(t), '0134578@$', 'oieastbas'), '[^a-zа-яё0-9]', '', 'g')) $$;
create function has_link(t text) returns boolean language sql immutable as $$
  select regexp_replace(regexp_replace(lower(t), '\s*(\[dot\]|\(dot\)|\{dot\}|\sdot\s)\s*', '.', 'g'), '\s*\.\s*', '.', 'g')
    ~ '(https?://|www\.|discord\.gg|[a-z0-9-]+\.(com|net|org|ru|io|gg|me|ly|tv|xyz|co|to|app|dev|link|ua|by|lt|su)\y)' $$;
-- add words with:  select add_blocked('word');   (service role / SQL editor only)
create function add_blocked(w text) returns void language sql as $$ insert into blocked_words values (norm_text(w)) on conflict do nothing $$;
revoke execute on function add_blocked(text) from public, anon, authenticated;
create function get_blocked_words() returns setof text language sql stable security definer as $$ select word_norm from blocked_words $$;

-- returns NULL if text is fine, else the reason; applies a 5-10 min mute on violation
create function enforce_filter(p_uid uuid, p_text text) returns text language plpgsql security definer as $$
declare m timestamptz; n text;
begin
  select muted_until into m from profiles where id = p_uid;
  if m is not null and m > now() then return 'muted'; end if;
  n := norm_text(p_text);
  if has_link(p_text) or exists (select 1 from blocked_words w where position(w.word_norm in n) > 0) then
    update profiles set muted_until = now() + make_interval(mins => 5 + floor(random() * 6)::int) where id = p_uid;
    return 'filtered';
  end if;
  return null;
end $$;
revoke execute on function enforce_filter(uuid, text) from public, anon, authenticated;

---------------------------------------------------------------- profile / account
-- called right after any sign-in (email or Google). Creates the profile once,
-- re-attaches the device hash, and refuses alt accounts on the same device.
create function ensure_profile(p_device text, p_display text) returns void language plpgsql security definer as $$
declare v_email text; w admin_whitelist%rowtype; uname text; n bigint; dev profiles%rowtype;
begin
  if exists (select 1 from profiles where id = auth.uid()) then
    select * into dev from profiles where device_hash = p_device and id <> auth.uid();
    if found then raise exception 'alt_not_supported'; end if;
    update profiles set device_hash = coalesce(device_hash, p_device) where id = auth.uid();
    return;
  end if;
  select lower(email) into v_email from auth.users where id = auth.uid();
  select * into w from admin_whitelist where lower(email) = v_email;
  if found then uname := w.username;
  else
    n := (select coalesce(max(number_id), 0) + 1 from profiles);
    uname := 'user' || n;
  end if;
  insert into profiles (id, device_hash, username, display_name, is_admin, role, verified)
    values (auth.uid(), p_device, uname,
            left(coalesce(nullif(trim(p_display), ''), split_part(v_email, '@', 1), 'user'), 32),
            found, w.role, found);
  insert into usernames (name, owner, is_primary) values (uname, auth.uid(), true);
end $$;
revoke execute on function ensure_profile(text, text) from public, anon, authenticated;

create function get_me() returns jsonb language sql stable security definer as $$
  select to_jsonb(p) - 'device_hash' from profiles p where id = auth.uid() $$;

create function get_profile(p_username text) returns jsonb language sql stable security definer as $$
  select to_jsonb(p) - 'device_hash' - 'tokens' - 'muted_until' from profiles p where username = lower(p_username) $$;

create function set_display_name(p_name text) returns void language plpgsql security definer as $$
begin
  if char_length(trim(p_name)) not between 2 and 32 then raise exception 'bad name'; end if;
  if enforce_filter(auth.uid(), p_name) is not null then raise exception 'name not allowed'; end if;
  update profiles set display_name = trim(p_name) where id = auth.uid();
end $$;

create function set_avatar(p_url text) returns void language sql security definer as $$ update profiles set avatar_url = p_url where id = auth.uid() $$;

create function set_banner(p_color text, p_emoji text) returns void language plpgsql security definer as $$
begin
  if p_color !~ '^#[0-9a-fA-F]{6}$' then raise exception 'bad color'; end if;
  if p_emoji is not null and p_emoji <> '' then
    if plan_rank(eff_plan(auth.uid())) < 3 then raise exception 'Ultra required for banner emoji'; end if;
    if char_length(p_emoji) > 8 then raise exception 'bad emoji'; end if;
  end if;
  update profiles set banner_color = p_color, banner_emoji = nullif(p_emoji, '') where id = auth.uid();
end $$;

create function set_ring(p_index int, p_custom text) returns void language plpgsql security definer as $$
begin
  if p_index not between 0 and 5 then raise exception 'bad ring'; end if;
  if p_custom is not null and p_custom <> '' then
    if plan_rank(eff_plan(auth.uid())) < 3 then raise exception 'Ultra required for custom ring'; end if;
    if p_custom !~ '^#[0-9a-fA-F]{6},#[0-9a-fA-F]{6}$' then raise exception 'bad ring colors'; end if;
  end if;
  update profiles set ring = p_index, ring_custom = nullif(p_custom, '') where id = auth.uid();
end $$;

---------------------------------------------------------------- tokens & plans (3 days each)
create function earn_listen() returns jsonb language plpgsql security definer as $$
declare s listen_state%rowtype; gap numeric; earned int := 0;
begin
  insert into listen_state (user_id) values (auth.uid()) on conflict do nothing;
  select * into s from listen_state where user_id = auth.uid() for update;
  if s.day <> current_date then s.day := current_date; s.earned_today := 0; end if;
  gap := extract(epoch from now() - coalesce(s.last_beat, now() - interval '1 hour'));
  if gap >= 25 then
    s.carry := s.carry + least(gap, 35)::int;
    earned := least(s.carry / 120, 300 - s.earned_today);          -- 1 § per 2 min listened, max 300 § / day
    s.carry := case when s.earned_today + earned >= 300 then 0 else s.carry % 120 end;
    s.earned_today := s.earned_today + earned;
    update listen_state set last_beat = now(), carry = s.carry, day = s.day, earned_today = s.earned_today where user_id = auth.uid();
    if earned > 0 then update profiles set tokens = tokens + earned where id = auth.uid(); end if;
  end if;
  return jsonb_build_object('earned', earned, 'tokens', (select tokens from profiles where id = auth.uid()));
end $$;

create function buy_plan(p_plan text) returns jsonb language plpgsql security definer as $$
declare price int; cur text;
begin
  price := case p_plan when 'pro' then 120 when 'max' then 300 when 'ultra' then 700 end;
  if price is null then raise exception 'bad plan'; end if;
  cur := eff_plan(auth.uid());
  if plan_rank(p_plan) < plan_rank(cur) then raise exception 'You already have a higher plan active'; end if;
  update profiles set tokens = tokens - price, plan = p_plan,
    plan_expires = (case when cur = p_plan then plan_expires else now() end) + interval '3 days'   -- renewing stacks time
    where id = auth.uid() and tokens >= price;
  if not found then raise exception 'Not enough §'; end if;
  return get_me();
end $$;

---------------------------------------------------------------- chat & forum
create function get_chat(p_room text, p_limit int default 50) returns jsonb language sql stable security definer as $$
  select coalesce(jsonb_agg(x order by x.id), '[]') from (
    select m.id, m.text, m.created_at, p.username as author, p.display_name, eff_plan(p.id) as plan, p.role, p.verified
    from chat_messages m join profiles p on p.id = m.author where m.room = p_room order by m.id desc limit least(p_limit, 200)) x $$;

create function get_card(p_uid uuid) returns jsonb language sql stable security definer as $$
  select jsonb_build_object('username', username, 'display_name', display_name, 'plan', eff_plan(id), 'role', role, 'verified', verified) from profiles where id = p_uid $$;

create function send_message(p_room text, p_text text) returns jsonb language plpgsql security definer as $$
declare r text; mu timestamptz;
begin
  p_text := trim(p_text);
  if p_text = '' or char_length(p_text) > 500 then raise exception 'bad message'; end if;
  r := enforce_filter(auth.uid(), p_text);
  select muted_until into mu from profiles where id = auth.uid();
  if r is not null then return jsonb_build_object('error', r, 'muted_until', mu); end if;
  insert into chat_messages (room, author, text) values (p_room, auth.uid(), p_text);
  return jsonb_build_object('ok', true);
end $$;

create function get_forum() returns jsonb language sql stable security definer as $$
  select coalesce(jsonb_agg(x order by x.pinned desc, x.created_at desc), '[]') from (
    select f.id, f.title, f.pinned, f.created_at, p.username as author,
      (select count(*) from forum_replies r where r.post_id = f.id) as replies
    from forum_posts f join profiles p on p.id = f.author limit 100) x $$;

create function create_post(p_title text, p_body text) returns jsonb language plpgsql security definer as $$
declare r text; mu timestamptz;
begin
  r := coalesce(enforce_filter(auth.uid(), p_title), enforce_filter(auth.uid(), p_body));
  select muted_until into mu from profiles where id = auth.uid();
  if r is not null then return jsonb_build_object('error', r, 'muted_until', mu); end if;
  insert into forum_posts (author, title, body) values (auth.uid(), trim(p_title), p_body);
  return jsonb_build_object('ok', true);
end $$;

create function reply_post(p_post uuid, p_body text) returns jsonb language plpgsql security definer as $$
declare r text; mu timestamptz;
begin
  r := enforce_filter(auth.uid(), p_body);
  select muted_until into mu from profiles where id = auth.uid();
  if r is not null then return jsonb_build_object('error', r, 'muted_until', mu); end if;
  insert into forum_replies (post_id, author, body) values (p_post, auth.uid(), p_body);
  return jsonb_build_object('ok', true);
end $$;

---------------------------------------------------------------- TrackCloud: uploads, streaming, offline
insert into storage.buckets (id, name, public) values ('tracks', 'tracks', false), ('gifts', 'gifts', false), ('avatars', 'avatars', true) on conflict do nothing;

create policy "tracks read" on storage.objects for select to authenticated using (bucket_id = 'tracks');
create policy "tracks write own folder, paid plan" on storage.objects for insert to authenticated
  with check (bucket_id = 'tracks' and (storage.foldername(name))[1] = auth.uid()::text and plan_rank(eff_plan(auth.uid())) >= 1);
create policy "gifts read" on storage.objects for select to authenticated using (bucket_id = 'gifts');
create policy "gifts write admins only" on storage.objects for insert to authenticated
  with check (bucket_id = 'gifts' and is_admin_uid(auth.uid()));
create policy "avatars write own" on storage.objects for insert to authenticated
  with check (bucket_id = 'avatars' and (storage.foldername(name))[1] = auth.uid()::text);
create policy "avatars update own" on storage.objects for update to authenticated
  using (bucket_id = 'avatars' and (storage.foldername(name))[1] = auth.uid()::text);

create function register_track(p_title text, p_artist text, p_path text, p_duration int, p_lyrics text default null) returns uuid language plpgsql security definer as $$
declare pl text := eff_plan(auth.uid()); id uuid;
begin
  if plan_rank(pl) < 1 then raise exception 'Pro or higher required to upload'; end if;
  if split_part(p_path, '/', 1) <> auth.uid()::text then raise exception 'bad path'; end if;
  if (select count(*) from tracks where owner = auth.uid()) >= 200 then raise exception 'upload limit reached'; end if;
  if enforce_filter(auth.uid(), p_title || ' ' || p_artist) is not null then raise exception 'title not allowed'; end if;
  insert into tracks (owner, title, artist, path, duration, lyrics, download_price)
    values (auth.uid(), left(p_title, 100), left(p_artist, 100), p_path, p_duration,
            nullif(left(coalesce(p_lyrics, ''), 20000), ''),
            case when pl = 'pro' then 50 else 0 end)   -- Pro: downloads cost 50 §; Max/Ultra: free
    returning tracks.id into id;
  return id;
end $$;

create function get_my_uploads() returns jsonb language sql stable security definer as $$
  select coalesce(jsonb_agg(to_jsonb(t) order by t.created_at desc), '[]') from tracks t where owner = auth.uid() $$;

create function search_uploads(p_q text) returns jsonb language sql stable security definer as $$
  select coalesce(jsonb_agg(x), '[]') from (
    select t.id, t.title, t.artist, t.path, t.duration, t.lyrics, t.download_price, p.username as uploader
    from tracks t join profiles p on p.id = t.owner
    where p_q = '' or t.title ilike '%' || p_q || '%' or t.artist ilike '%' || p_q || '%' or p.username ilike '%' || p_q || '%'
    order by t.created_at desc limit 30) x $$;

create function request_download(p_track uuid) returns text language plpgsql security definer as $$
declare t tracks%rowtype;
begin
  select * into t from tracks where id = p_track;
  if not found then raise exception 'no such track'; end if;
  if t.download_price > 0 and t.owner <> auth.uid() and not exists (select 1 from track_unlocks where user_id = auth.uid() and track_id = p_track) then
    update profiles set tokens = tokens - t.download_price where id = auth.uid() and tokens >= t.download_price;
    if not found then raise exception 'Not enough §'; end if;
    update profiles set tokens = tokens + t.download_price where id = t.owner;   -- uploader gets paid
  end if;
  insert into track_unlocks values (auth.uid(), p_track) on conflict do nothing;
  return t.path;
end $$;

---------------------------------------------------------------- catalog (gifts) & selling
create function add_gift(p_name text, p_path text, p_price int, p_unique boolean) returns uuid language plpgsql security definer as $$
declare id uuid;
begin
  if not is_admin_uid(auth.uid()) then raise exception 'forbidden'; end if;
  insert into gifts (name, video_path, price, is_unique, created_by) values (p_name, p_path, p_price, p_unique, auth.uid()) returning gifts.id into id;
  return id;
end $$;

create function get_catalog() returns jsonb language sql stable security definer as $$
  select coalesce(jsonb_agg(to_jsonb(g) - 'created_by' order by g.price), '[]') from gifts g $$;

create function buy_gift(p_gift uuid) returns uuid language plpgsql security definer as $$
declare g gifts%rowtype; id uuid;
begin
  select * into g from gifts where id = p_gift;
  if not found then raise exception 'no such gift'; end if;
  update profiles set tokens = tokens - g.price where id = auth.uid() and tokens >= g.price;
  if not found then raise exception 'Not enough §'; end if;
  insert into user_gifts (owner, gift_id) values (auth.uid(), p_gift) returning user_gifts.id into id;
  return id;
end $$;

create function get_my_gifts() returns jsonb language sql stable security definer as $$
  select coalesce(jsonb_agg(jsonb_build_object('id', u.id, 'gift_id', g.id, 'name', g.name, 'video_path', g.video_path, 'is_unique', g.is_unique, 'sale_price', u.sale_price)), '[]')
  from user_gifts u join gifts g on g.id = u.gift_id where u.owner = auth.uid() $$;

create function pin_gift(p_gift uuid) returns void language plpgsql security definer as $$
begin   -- only "Unique" gifts can be pinned to the banner (Glow); null unpins
  if p_gift is not null and not exists (select 1 from user_gifts u join gifts g on g.id = u.gift_id where u.owner = auth.uid() and g.id = p_gift and g.is_unique) then
    raise exception 'You do not own that Unique gift'; end if;
  update profiles set pinned_gift = p_gift where id = auth.uid();
end $$;

create function list_gift(p_user_gift uuid, p_price int) returns void language plpgsql security definer as $$
begin
  if plan_rank(eff_plan(auth.uid())) < 2 then raise exception 'Max or higher required to sell gifts'; end if;
  update user_gifts set sale_price = p_price where id = p_user_gift and owner = auth.uid() and (p_price is null or p_price > 0);
  if not found then raise exception 'not your gift'; end if;
end $$;

create function get_market() returns jsonb language sql stable security definer as $$
  select coalesce(jsonb_agg(jsonb_build_object('id', u.id, 'name', g.name, 'video_path', g.video_path, 'is_unique', g.is_unique, 'price', u.sale_price, 'seller', p.username)), '[]')
  from user_gifts u join gifts g on g.id = u.gift_id join profiles p on p.id = u.owner where u.sale_price is not null $$;

create function buy_listing(p_user_gift uuid) returns void language plpgsql security definer as $$
declare u user_gifts%rowtype;
begin
  select * into u from user_gifts where id = p_user_gift and sale_price is not null for update;
  if not found or u.owner = auth.uid() then raise exception 'not available'; end if;
  update profiles set tokens = tokens - u.sale_price where id = auth.uid() and tokens >= u.sale_price;
  if not found then raise exception 'Not enough §'; end if;
  update profiles set tokens = tokens + (u.sale_price * 90 / 100) where id = u.owner;     -- 10% fee
  update profiles set pinned_gift = null where id = u.owner and pinned_gift = u.gift_id
    and not exists (select 1 from user_gifts x where x.owner = u.owner and x.gift_id = u.gift_id and x.id <> u.id);
  update user_gifts set owner = auth.uid(), sale_price = null where id = u.id;
end $$;

---------------------------------------------------------------- usernames (Ultra: several, sellable)
create function claim_username(p_name text) returns void language plpgsql security definer as $$
declare n int; lim int; pl text := eff_plan(auth.uid());
begin
  p_name := lower(trim(p_name));
  if p_name !~ '^[a-z0-9_]{3,20}$' then raise exception 'Use 3-20 chars: a-z, 0-9, _'; end if;
  if exists (select 1 from reserved_usernames where name = p_name) and not exists (select 1 from usernames where name = p_name and owner = auth.uid()) then raise exception 'reserved'; end if;
  if norm_text(p_name) in (select word_norm from blocked_words) then raise exception 'not allowed'; end if;
  select count(*) into n from usernames where owner = auth.uid();
  lim := case when pl = 'ultra' then 5 else 1 end;
  if n >= lim then
    if lim = 1 then   -- non-Ultra: renaming = replacing the single username
      delete from usernames where owner = auth.uid();
    else raise exception 'Username limit reached'; end if;
  end if;
  insert into usernames (name, owner, is_primary) values (p_name, auth.uid(), n = 0 or lim = 1);
  if n = 0 or lim = 1 then update profiles set username = p_name where id = auth.uid(); end if;
end $$;

create function set_primary_username(p_name text) returns void language plpgsql security definer as $$
begin
  if not exists (select 1 from usernames where name = p_name and owner = auth.uid()) then raise exception 'not yours'; end if;
  update usernames set is_primary = (name = p_name) where owner = auth.uid();
  update profiles set username = p_name where id = auth.uid();
end $$;

create function list_username(p_name text, p_price int) returns void language plpgsql security definer as $$
begin
  if plan_rank(eff_plan(auth.uid())) < 3 then raise exception 'Ultra required to sell usernames'; end if;
  if (select is_primary from usernames where name = p_name and owner = auth.uid()) then raise exception 'Cannot sell your primary username'; end if;
  update usernames set price = p_price where name = p_name and owner = auth.uid() and (p_price is null or p_price > 0);
  if not found then raise exception 'not yours'; end if;
end $$;

create function buy_username(p_name text) returns void language plpgsql security definer as $$
declare u usernames%rowtype;
begin
  if plan_rank(eff_plan(auth.uid())) < 3 then raise exception 'Ultra required'; end if;
  if (select count(*) from usernames where owner = auth.uid()) >= 5 then raise exception 'Username limit reached'; end if;
  select * into u from usernames where name = p_name and price is not null for update;
  if not found or u.owner = auth.uid() then raise exception 'not for sale'; end if;
  update profiles set tokens = tokens - u.price where id = auth.uid() and tokens >= u.price;
  if not found then raise exception 'Not enough §'; end if;
  update profiles set tokens = tokens + (u.price * 90 / 100) where id = u.owner;
  update usernames set owner = auth.uid(), price = null, is_primary = false where name = p_name;
end $$;

---------------------------------------------------------------- console (admins only)
create function admin_action(p_action text, p_username text default null, p_value int default null, p_text text default null) returns jsonb language plpgsql security definer as $$
declare target uuid;
begin
  if not is_admin_uid(auth.uid()) then raise exception 'forbidden'; end if;
  if p_username is not null then
    select id into target from profiles where username = lower(trim(both '@' from p_username));
    if target is null then raise exception 'user not found'; end if;
    if is_admin_uid(target) and p_action in ('mute','ban','setplan') and target <> auth.uid() then raise exception 'cannot target another admin'; end if;
  end if;
  case p_action
    when 'mute'    then update profiles set muted_until = now() + make_interval(mins => greatest(coalesce(p_value, 5), 1)) where id = target;
    when 'unmute'  then update profiles set muted_until = null where id = target;
    when 'grant'   then update profiles set tokens = greatest(tokens + coalesce(p_value, 0), 0) where id = target;
    when 'setplan' then update profiles set plan = lower(p_text), plan_expires = now() + make_interval(days => coalesce(p_value, 3)) where id = target;
    when 'ban'     then update profiles set banned = true where id = target;
    when 'pinpost' then update forum_posts set pinned = not pinned where id = p_text::uuid;
    when 'delpost' then delete from forum_posts where id = p_text::uuid;
    when 'addword' then perform add_blocked(p_text);
    else raise exception 'unknown action';
  end case;
  return jsonb_build_object('ok', true);
end $$;

---------------------------------------------------------------- realtime
alter publication supabase_realtime add table chat_messages;
