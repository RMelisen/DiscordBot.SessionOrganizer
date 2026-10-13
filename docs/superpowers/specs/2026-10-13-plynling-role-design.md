# Plynling role — design

**Status:** approved in brainstorming, 2026-10-13. Target version **6.11.0** (minor: new subcommand
group + migration).

## Goal

SYNCS manages one Discord role per guild that **every member with a living Plynling holds, and
nobody else does**. It serves two purposes: flair (visible on the name and in the member list) and a
ping group (any member can `@` the Plynling owners). It gates nothing — no channel, no permission —
so a role briefly out of date is harmless, and an hourly sync is precise enough.

"Living" means `DiedAt == null` after `PlynlingLife.Settle`. **Frozen Plynlings count as living**
(self-freeze and staff freeze alike): a freeze is not a death.

## Decisions taken

| Question | Decision |
|---|---|
| Which roles | One role: "has a living Plynling". No species roles, no XP roles (later, maybe). |
| How it comes to exist | Both: `create` (SYNCS makes it) and `set` (adopt an existing one); `clear` stops managing. |
| Ownership | **Strict mirror**: SYNCS adds it where missing and removes it from anyone without a living Plynling, including hand-given copies. |
| Mentionability | A role made by `create` is mentionable by everyone. A role picked with `set` keeps staff's setting. |
| When it changes | Immediately on adopt / abandon / staff resurrect, **plus** a full sync at the end of every `PlynlingSweepService` pass. |
| SYNCS pinging it | Not in this version. |

## Data

- `GuildSettings.PlynlingRoleId` (`ulong`, `.HasConversion<long>()`), **zero = unset**, the
  convention every other id in that table follows. One migration (`AddPlynlingRole`), schema-only.
- `GuildConfig` (the cached record in `GuildConfigService`) gains `PlynlingRoleId`;
  `GuildConfigService.SetPlynlingRoleAsync(guildId, roleId)` writes it through `UpdateSettingsAsync`
  (which already drops the guild's cache entry).

## `/config plynling-role`

A new subgroup in `ConfigModule`, same shape as `moderator-role`: every handler goes through
`ConfigModule.BeginAsync` (`IsStaff`, ephemeral defer). French descriptions and replies.

- **`create [name]`** — `name` optional, `[MaxLength]` from `InputCaps` (Discord's role-name cap is
  100), default « Maîtres de Plynlings ».
  - Refuses if a role is already configured *and still exists* (« `clear` d'abord ») — a second
    click must not leave an orphan role. A configured id whose role was deleted counts as unset.
  - Refuses if the bot lacks `ManageRoles`.
  - Creates the role with **no permissions** (`GuildPermissions.None`), `isMentionable: true`, no
    colour. A new role lands at the bottom of the list, below the bot's own, so it is manageable.
  - Stores the id, replies with the role pill and the counts, then starts the first sync.
- **`set role:`** — refused, with a French reason, when `PlynlingRoleSync.Refusal` returns one:
  - `@everyone` (`role.Id == guild.Id`);
  - a managed / integration role (`role.IsManaged`);
  - a role at or above the bot's top role (`role.Position >= bot.Hierarchy`);
  - any role whose `Permissions.RawValue != 0` — this role is flair; adopting a permission role would
    hand or strip permissions on the sync's say-so;
  - the bot lacking `ManageRoles`.

  On success the reply states what the first sync will do (« 4 l'obtiendront, 2 le perdront »),
  computed with the same `Diff` the sync uses, then the sync runs.
- **`clear`** — stores zero. Touches no member and deletes nothing; the reply says so (« le rôle et
  ses membres restent tels quels — supprime-le dans Discord si tu n'en veux plus »).
- **`/config show`** (`ConfigCards.Build`) gains a « Rôle Plynling » line: the pill, or
  « non configuré », and a warning when `Refusal` reports the role is no longer manageable (deleted,
  moved above hers, `ManageRoles` lost). The card's component count must be re-summed before adding.

The first sync after `create` / `set` runs **in the background** (`_ = Task.Run(...)` with its own
catch-and-log) after the deferred reply, so a large guild cannot outrun the interaction.

## Pure logic — `Helpers/Plynling/PlynlingRoleSync`

Static, no gateway, no database:

- `Diff(IReadOnlySet<ulong> owners, IReadOnlySet<ulong> holders, IReadOnlySet<ulong> members)`
  → `(IReadOnlyList<ulong> ToAdd, IReadOnlyList<ulong> ToRemove)`.
  - `ToAdd` = owners ∩ members − holders. Owners not in the guild are skipped, never retried.
  - `ToRemove` = holders − owners.
  - Both sorted by id, so a capped pass is deterministic.
- `Refusal(...)` — takes plain values (role id, guild id, `IsManaged`, position, raw permissions,
  bot top position, bot has `ManageRoles`, role exists) and returns a French reason or `null`. One
  function backs `set`, `/config show` and the sync's skip check, so they can never disagree on what
  is manageable. `set` adds the permission check; the sync's check omits it (staff may legitimately
  add a permission to the role later — the sync keeps working; only *adopting* such a role is
  refused).
- `MaxRemovalsPerPass = 25`.

## Service — `Services/Plynling/PlynlingRoleService`

**Singleton** (it holds the lock and the logged-once set), takes `IServiceProvider` and opens
`CreateAsyncScope()` per unit of work; never injects a DB service. Registered in `Program.cs`.

- `SyncGuildAsync(SocketGuild guild, CancellationToken ct)`:
  1. Read `PlynlingRoleId` from `GuildConfigService`; zero → return.
  2. Resolve the role and run the sync-flavour `Refusal`; a reason → log it **once per guild per
     process** (`BoundedSet<ulong>`, cleared for that guild when a later pass succeeds) and return.
  3. **If `!guild.HasAllMembers`, skip the pass** (log at debug). With a partial cache an owner
     looks absent and is skipped as "left the guild"; better to do nothing for an hour.
  4. Load owners: living Plynlings of the guild, settled in memory (one query, the same shape the
     sweep uses), projected to `OwnerId`.
  5. `Diff` against `role.Members` and `guild.Users`.
  6. Apply: every add; at most `MaxRemovalsPerPass` removals — over the cap, log a warning with the
     count and do the first 25; the next pass continues. Each call passes
     `RequestOptions { AuditLogReason = "Plynling vivant" / "Plus de Plynling" }` and sits in its
     own `try` (catch, log, next member).
- `SyncMemberAsync(SocketGuild guild, ulong userId)` — the immediate hook. Same rule for one person:
  config + `Refusal` check, does this person have a living Plynling, add or remove if needed.
  Swallow-and-log on any failure; the hourly pass is the backstop.
- A `SemaphoreSlim(1)` serialises `SyncGuildAsync` passes (the sweep and a `set` can overlap). The
  member hook does not take it: it applies the same rule, and adding a held role / removing an
  absent one is a no-op on Discord's side, so a race converges.

## Hooks — the only three life-change call sites

After a **successful** outcome, wrapped so nothing breaks the command (`SyncMemberAsync` swallows
already):

- `PlynlingModule` adoption (`AdoptAsync`, ~line 72) — the role arrives at once.
- `PlynlingModule` abandon (`AbandonAsync`, ~line 117).
- `AdminModule` staff resurrect (`ResurrectAsync`, ~line 380).

Deaths are lazy (`Settle` stamps `DiedAt` on whatever read finds them) and raise nothing, so they
are covered by the sweep. A future fourth path that creates or ends a living Plynling must call
`SyncMemberAsync` — `plynling.md` says so.

## Sweep integration

`PlynlingSweepService` runs `SyncGuildAsync` for each guild **as its last step**, after deaths are
stamped and announced, so a death found this pass removes the role in the same pass. One `try` per
guild, inside the existing loop's per-item discipline — an exception never escapes the hosted loop.
`/debug sweep` runs it too, under the sweep's existing one-pass-at-a-time guard.

## Rate limits

Discord.Net queues per-route. A first sync on a guild with a few dozen owners takes seconds and runs
in the background; the hourly diff is normally empty.

## Docs and version

- `docs/agents/plynling.md`: a *Plynling role* section — strict mirror, hooks + hourly sync, skip on
  incomplete cache, removal cap, the three call sites and the rule for a fourth.
- `docs/agents/admin.md`: `plynling-role set|create|clear` in the `/config` list, the `set` guards,
  `clear` never deletes.
- `HelpModule` `/config` line and `README.md` mention the role (measure `/help` against the 6000
  cap first).
- Root `CLAUDE.md`: mention `PlynlingRoleService` only if the architecture list warrants it.
- `config.yaml` **6.10.1 → 6.11.0**; `CHANGELOG.md` entry: « Rôle Plynling : SYNCS peut créer et
  gérer un rôle pour tous les maîtres d'un Plynling vivant (`/config plynling-role`). »
- The bot needs **Manage Roles** in the server; README's setup notes say so.

## Verification

No test project: `dotnet build -warnaserror`, then on the dev guild:

1. `/config plynling-role create` → role appears, existing owners receive it.
2. Adopt → role at once. Abandon → removed at once.
3. Give the role by hand to someone without a Plynling, `/debug sweep` → removed.
4. Move the role above hers → `/config show` warns, the sweep skips and logs once.
5. `set` on `@everyone`, a bot's managed role, a role with permissions → each refused with its reason.
6. `clear` → nobody's role changes.

## Out of scope

XP / level roles, species roles, SYNCS pinging the role, re-adding on `UserJoined` (the hourly pass
covers a rejoin), and any edit of the role after creation.
