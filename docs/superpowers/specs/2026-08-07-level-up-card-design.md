# Level-up card

## Context

`XpTracker.AnnounceAsync` currently posts a plain-text line, picked from
`BotResponses.XpLevelUpLines`, when someone crosses a level in SYNCS's own XP
system. The request is to frame that announcement as a small embed ("card")
instead, carrying the person's avatar, and to show the level transition (e.g.
`3 → 4`) rather than just the new number. Two content additions ride along:
a fixed easter-egg line at levels 7 and 67, and a one-time reset of everyone's
accumulated XP now that the announcement is changing shape.

## Card design

- **Title:** `Niveau {old} → {new} !` — no emoji. On a multi-level jump within
  a single grant, this still announces only once, showing the full span
  (e.g. `Niveau 5 → 8 !`), matching the existing "final level only, never one
  message per level" rule.
- **Description:** the line picked from `XpLevelUpLines` — **except** when
  `newLevel` is exactly `7` or `67`, where the description is the literal
  string `"SIX SEVEEEEN"` instead. `ResponsePicker` is not consulted in that
  case: it's the same fixed line every time, deliberately, not a pool entry.
- **Thumbnail:** the person's avatar — `user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl()`,
  the same fallback-chain shape `BotResponses.DisplayNameFor` already uses for
  name resolution.
- **Color:** `Color.Purple`, matching `/level`'s leaderboard embed — one
  visual identity across every level-related surface.
- No footer, no extra fields (XP total, progress bar, rank) — out of scope
  for this pass.
- Still an unprompted **post**, not a reply; still paced behind the typing
  indicator before sending, same as every other personality line.

## Technical changes

### `XpTracker.cs`

- `AnnounceAsync` gains the old level as a parameter (`AnnounceAsync(channel,
  userId, oldLevel, newLevel, knownUser)`) so the title can show the span —
  `GrantAsync` already computes both and currently only forwards `newLevel`.
- `ResolveName(channel, userId, knownUser)` becomes `ResolveUser(channel,
  userId, knownUser)`, returning the resolved `IUser?` instead of a `string?`.
  Both the display name and the avatar URL are then derived from that one
  resolved user, instead of resolving just a name. The existing "member
  couldn't be resolved → skip the announcement silently" rule is unchanged,
  just now gates on the `IUser?` being null instead of the `string?`.
- Description selection: `newLevel is 7 or 67 ? "SIX SEVEEEEN" :
  string.Format(_picker.Pick(channel.Id, BotResponses.XpLevelUpLines), name, newLevel)`.
- Builds an `Embed` (title, description, thumbnail, color) and sends it via
  the new `BotChat.PostEmbedWithTypingAsync` (below) instead of
  `PostWithTypingAsync`.

### `Helpers/BotChat.cs`

- New sibling method:
  `PostEmbedWithTypingAsync(IMessageChannel channel, Embed embed, string delayText, ILogger logger, string what)`.
  Same shape as `PostWithTypingAsync` — pause behind the typing indicator,
  send, swallow-and-log on failure — but sends `embed:` instead of message
  text. `delayText` is the flavor line (or `"SIX SEVEEEEN"`), reused only to
  size the typing pause via the existing private `TypingDelayFor`; it is not
  sent as text.
- `PostWithTypingAsync` itself is untouched — every other personality pool
  still goes through it unchanged.

### Migration — one-time XP reset

- A new EF Core migration, schema-empty, whose `Up()` runs
  `migrationBuilder.Sql("DELETE FROM MemberXps;");` and whose `Down()` is a
  no-op (a delete cannot be meaningfully reversed). No precedent in this repo
  for a data-only migration — every existing one is schema-only — so this is
  a new pattern, used deliberately once rather than adding a standing
  admin/reset command.
- Applies automatically on the next startup, in both dev and prod, via the
  existing "migrations are applied automatically on startup" mechanism —
  no manual database access needed.

## Out of scope

- XP total, progress-to-next-level, or leaderboard rank on the card (asked
  and explicitly deferred during design).
- Any standing "reset XP" command — this migration is a one-time data fix,
  not a reusable feature.

## Testing

No test project exists in this repo (build + manual/scratch verification is
the established pattern here). Verify:

1. `dotnet build -warnaserror` — clean.
2. Scratch/reflection check (mirroring this session's existing `cuecheck`/
   `xpcheck` harnesses) that:
   - a normal level-up picks a non-empty `XpLevelUpLines` line and formats it
     with name/new-level correctly;
   - `newLevel == 7` and `newLevel == 67` both produce exactly
     `"SIX SEVEEEEN"`, bypassing `ResponsePicker`;
   - a multi-level jump (e.g. old=5, new=8) produces the title `Niveau 5 → 8 !`.
3. Apply the new migration against a throwaway copy of a populated SQLite
   file and confirm `MemberXps` is empty afterward and no schema changed.
4. Dev guild, manual: trigger a real level-up and confirm the card renders
   with title, description, avatar thumbnail, and purple color as designed.
