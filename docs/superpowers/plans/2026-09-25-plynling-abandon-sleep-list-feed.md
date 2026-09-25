# Plynlings: abandon, sleep, list, feeding others — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add `/plynling abandon`, a nightly sleep with deferred deaths, `/plynling list`, and double-price feeding of others' Plynlings.

**Architecture:** Each feature is an independent task on the existing Plynling layers: pure rules in `Helpers/PlynlingLife` (checked by a scratch C# harness), persistence in `PlynlingService`/`ShameService`, commands in `PlynlingModule`, buttons in `PlynlingComponentHandler`, art in `tools/plynling-art`.

**Tech Stack:** .NET 10, Discord.Net 3.20, EF Core (SQLite, `dotnet ef` 10.0.9), Python 3.12 + Pillow.

Spec: `docs/superpowers/specs/2026-09-25-plynling-abandon-sleep-list-feed-design.md`.

## Global Constraints

- Sleep window **[01:00, 05:00) Europe/Paris** via `AppTime.Zone`; deaths due in it happen at **05:00**; the warning moves to **23:00** the evening before when it would fall in **[23:00, 05:00)**.
- Abandon: **slash command + modal typing the name**; the row is **deleted**; public announcement; `/shame` « 💔 L'Indigne »; **30-minute** in-memory adoption cooldown.
- Non-owners pay **double** to feed. List: living, oldest first, **10 per page**, embed.
- French user-facing text, gendered through `PlynlingGrammar.Agree` / `GenderedLines`. Nothing in the art moves side to side; no existing art file changes.
- Build `-warnaserror`. Commit per task (never push); version bump is a separate final "Updated version" commit.

`$SCRATCH` = the session scratchpad; the checks go in a new harness `$SCRATCH/extrascheck` (copy of `stagecheck`).

---

### Task 1: `/plynling abandon`

**Files:** `Models/ShameRecord.cs`, `Models/ShameDailyStat.cs` (+ migration `AddShameAbandonHits`), `Services/ShameService.cs` (`AddAbandonHitAsync`, `ShameWall.Indignes`), `Commands/ShameModule.cs` (5th title), `Services/BotResponses.cs` (`PlynlingAbandonLines`, `ShameEmptyIndigne`), `Services/PlynlingCooldowns.cs` (`AbandonedAt`), `Services/PlynlingService.cs` (`AbandonAsync`, adopt checks cooldown in the module), `Services/PlynlingAnnouncer.cs` (`AnnounceAbandonAsync`), `Commands/PlynlingModule.cs` (`abandon` command + `plyn:abandon:*` modal handler, `ignoreGroupNames: true`), `Helpers/PlynlingText.cs`.

- [ ] Failing checks in `extrascheck`: `PlynlingCardUi.NamesMatch("  pOuf ", "Pouf")`, `NamesMatch("Pou f", "Pouf") == false`, `NamesMatch("Mon  petit", "mon petit")`; `/shame` wall component count ≤ 40 (build it with five titles through `ShameModule`'s static builder).
- [ ] `NamesMatch(string typed, string name)`: trim, collapse whitespace runs, `string.Equals(..., StringComparison.OrdinalIgnoreCase)`.
- [ ] `AbandonHits` on both shame models (comment: counted by `/plynling abandon`, one per abandonment, uncapped — it is rare and deliberate); `dotnet ef migrations add AddShameAbandonHits`; check the migration is two `AddColumn`s only.
- [ ] `ShameService.AddAbandonHitAsync(guildId, userId)` (same shape as `AddShoutHitAsync`), `ShameWall` gains `Indignes`, both branches of `GetWallAsync` rank `AbandonHits`.
- [ ] `ShameModule`: `AddTitle(container, "💔 L'Indigne", wall.Indignes, BotResponses.ShameEmptyIndigne, "abandon", "abandons")`; update the budget comment/CLAUDE.md to 31/40.
- [ ] `PlynlingCooldowns.Abandon`: `ConcurrentDictionary<(ulong Guild, ulong User), DateTimeOffset>` with `Mark(key, now)` and `ReadyAt(key)`; `AbandonCooldown = 30 min` in `PlynlingLife`.
- [ ] `PlynlingService.AbandonAsync(int plynlingId, ulong ownerId)`: loads, refuses if missing/not the owner's/dead, `Remove` + save, returns the removed row.
- [ ] `/plynling abandon`: no living Plynling → `NoPlynling`; else `RespondWithModalAsync` with custom id `plyn:abandon:{id}` and one short text input « Son nom ». Modal handler: `NamesMatch` or refuse (`PlynlingText.AbandonMismatch`); abandon; mark the cooldown; `AddAbandonHitAsync` (own try, logged); `AnnounceAbandonAsync` (sprite: `Sprite(species, stage, Sad)`); ephemeral confirmation. `/plynling adopt` refuses while `ReadyAt > now` (`PlynlingText.AdoptCooldown(readyAt)`).
- [ ] Build, run `extrascheck`, commit « Added /plynling abandon ».

### Task 2: Sleep

**Files:** `Helpers/PlynlingCatalog.cs` (`PlynlingMood.Sleeping`), `Helpers/PlynlingLife.cs`, `Helpers/PlynlingCardUi.cs`, `Commands/PlynlingModule.cs` (hide « Caresser »), `Services/PlynlingService.cs` (`CareOutcome.Asleep` on pet), `Services/PlynlingCareService.cs`, `Helpers/PlynlingText.cs`; art: `sprites.py` (sleeping face, zzz), `motion.py`, `species.py` (Coprin ink keys), `export.py` (`STATES += ["sleeping"]`).

```csharp
public static readonly TimeSpan NightStart = TimeSpan.FromHours(1), NightEnd = TimeSpan.FromHours(5);

public static bool IsAsleep(DateTimeOffset t) { var h = AppTime.ToZoned(t).TimeOfDay; return h >= NightStart && h < NightEnd; }

// 05:00 Paris on the morning of t (t asleep); built from the wall clock so DST days are right.
public static DateTimeOffset WakeAfter(DateTimeOffset t) { var z = AppTime.ToZoned(t); var wall = z.Date + NightEnd; return new DateTimeOffset(wall, AppTime.Zone.GetUtcOffset(wall)); }

public static DateTimeOffset? EffectiveDeathAt(Plynling p) => DeathAt(p) is { } d ? (IsAsleep(d) ? WakeAfter(d) : d) : null;

// 3 h before the effective death, pulled back to 23:00 when that lands in [23:00, 05:00).
public static DateTimeOffset? WarnAt(Plynling p) { ... }
```

- [ ] Failing checks: `IsAsleep` at 00:59/01:00/04:59/05:00 Paris; `WakeAfter` on 2026-03-29 and 2026-10-25 is 05:00 local; a Plynling due to starve at 03:00 dies at 05:00 (`Settle`); due at 00:30 dies at 00:30; `WarnAt` for death 08:00 → 05:00, for 07:30 → 23:00 the day before, for 03:00 (→ 05:00) → 23:00 the day before, for 18:00 → 15:00; `Mood` at 02:00 is `Sleeping`, at 02:00 frozen is `Frozen`; pet at 02:00 refused as asleep.
- [ ] Implement; `Settle`/`ShouldWarn`/`Feed` re-arm use `EffectiveDeathAt`/`WarnAt`; `MoodLabel` « endormi/endormie »; status « 💤 {Endormi/Endormie} jusqu'à 5 h »; card hides « Caresser » when `Mood == Sleeping`.
- [ ] Art prototype: sleeping face (closed eyes, small « o » mouth), rising « z » glyphs in `extras`, species touches off; preview all 7 species adult+baby; **owner review**; export (137 files); artcheck 98 sprite URLs; animcheck 0 failed with 123 old files byte-identical.
- [ ] Commit « Put Plynlings to sleep at night ».

### Task 3: `/plynling list`

- [ ] `PlynlingService.GetLivingAsync(guildId, now)` (settles, saves if changed); `PlynlingModule.BuildListEmbed(IReadOnlyList<Plynling>, int page, DateTimeOffset now)` static + `BuildListButtons(page, pages)` (`plyn:lprev:{page-1}`, `plyn:lnext:{page+1}`, disabled at the ends); handlers in `PlynlingComponentHandler` re-render in place. Check: 25 Plynlings → 3 pages, page clamped, ids distinct, embed ≤ 6000.
- [ ] Commit « Added /plynling list ».

### Task 4: Feeding others at double price

- [ ] `PlynlingService.FeedAsync`: `price = actor == owner ? info.Price : info.Price * 2`; drop the `NotOwner` refusal (and `CareOutcome.NotOwner`, `PlynlingText.NotYours`). `PlynlingLife.FeedPrice(FoodInfo, bool isOwner)` pure, checked. Card select option description gains « · {2×} pour un autre »; `/plynling feed` gets `[Summary("user")] IUser? user = null`. Care line credits `<@actor>` and the price.
- [ ] Commit « Let anyone feed a Plynling, at double the price ».

### Task 5: Docs and version

- [ ] `/plynling help` (abandon, list, sleep, feeding others; recheck caps), `README.md`, `CLAUDE.md`, `tools/plynling-art/README.md`; commit « Documented … »; bump `config.yaml` +0.0.1; commit « Updated version ».
