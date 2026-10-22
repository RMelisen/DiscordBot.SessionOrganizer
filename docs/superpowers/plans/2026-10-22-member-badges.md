# Member badges (« Succès ») Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Give members tiered badges (paliers), one-off moments and hidden secrets, shown as icons on
`/level` with a « Succès » button that opens an ephemeral detail view.

**Architecture:** A pure catalog + evaluator (`Helpers/Xp/MemberBadges`) turns a `MemberBadgeFacts`
snapshot into standings; a transient `MemberBadgeService` reads the facts and writes rows to one new
table `MemberBadge`; one singleton `BadgeAwarder` is the only write path used by every hook,
fire-and-forget and swallow-and-log; static Components V2 builders (`MemberBadgeCards`) render the
card line and the four-tab view.

**Tech Stack:** .NET 10, Discord.Net 3.20 (Components V2), EF Core + SQLite, the
`tests/ProjectSYNCS.Checks` console.

**Spec:** `docs/superpowers/specs/2026-10-22-member-badges-design.md` (approved 2026-10-22). Read it
first: it holds the full catalog (keys, emoji, names, hints, thresholds) — this plan does not repeat
those values.

## Global Constraints

- Root `CLAUDE.md` and `docs/agents/xp.md` rules apply; read both before starting.
- UI strings French, code/comments/logs English. Badge names, hints and thresholds: **verbatim from
  the spec tables**.
- Badge keys are stable and append-only (stored, and part of icon file names).
- **No announcement** of any kind when a badge is earned (no message, DM, reaction).
- Every award is **per guild**; a DM (guild id 0) never earns anything; her own id never earns anything.
- **A badge write never breaks the flow it rides on**: always after the flow's own save, through
  `BadgeAwarder`, swallowed and logged. *(Deviation from the spec's « same unit of work »: a
  unique-index race inside the main save would fail a feed or a give — the spec's own anti-goal.)*
- SQLite: no `DateTimeOffset` comparison in SQL; filter ids/booleans in SQL, dates in memory.
  Snowflakes `.HasConversion<long>()`. No `HasMaxLength`.
- V2 messages: no content/embeds, `AllowedMentions.None`, flag re-asserted on every edit, ≤ 40
  components, ≤ 4000 text characters; every button its own custom-id.
- Secret triggers never appear in `/help`, `README.md` or `CHANGELOG.md`.
- **The owner commits by hand**: no task runs `git commit`. Each task ends with the build/checks green
  and stops.
- Version **7.34.0** (once, in Task 7).

## Review Focus

1. **A member who left the server** (`Guild.GetUser` null): `/level` and « Succès » still render;
   `seniority` is simply unearned — Task 1 check `seniority null`.
2. **A member with nothing at all**: no badge line on the card; every « Succès » tab renders with only
   `???` lines — Task 3 checks `empty card line` / `empty view`.
3. **A member with everything at Diamant, every secret, real emoji markup on every icon**: every tab
   fits 4000 characters and 40 components — Task 3 check `worst view`.
4. **DMs and her own id**: `/ship moi syncs` or `/rate syncs` in a DM, or an arcade game she wins,
   award nothing and throw nothing — Task 1 check `IsAwardable`, Task 2 guard in `BadgeAwarder`.
5. **A row whose key is no longer in the catalog**: ignored by the display, not counted — Task 1
   check `unknown stored key`.

---

### Task 1: Catalog and evaluation (pure)

**Files:**
- Create: `ProjectSYNCS/Helpers/Xp/MemberBadges.cs`
- Modify: `ProjectSYNCS/Helpers/Economy/ItemEmojis.cs` (`IsIconKey` accepts member badge keys)
- Test: `tests/ProjectSYNCS.Checks/Program.cs` (new section `// 34. Member badges`, before the final summary)

**Interfaces:**
- Produces (all in namespace `ProjectSYNCS.Helpers`):
  - `enum MemberBadgeKind { Palier, Moment, Secret }`
  - `enum BadgeUnit { Count, Hours, Days }` — how a palier's raw value prints (`Hours`: the value is minutes).
  - `sealed record MemberBadgeInfo(string Key, MemberBadgeKind Kind, string Emoji, string Name, string Hint, IReadOnlyList<long> Thresholds, BadgeUnit Unit = BadgeUnit.Count)` — `Thresholds` empty for moments and secrets.
  - `sealed record StoredBadge(long Count, DateTimeOffset EarnedAt)`
  - `sealed record MemberBadgeFacts(long VoiceMinutes, long CompanionMinutes, long QuizWins, long QuizBestMs, long Reactions, long SessionsAttended, long SessionsOrganised, long GiveawaysWon, long GoodBot, long PlynlingBadges, bool Punctual, DateTimeOffset? JoinedAt, IReadOnlyDictionary<string, StoredBadge> Stored)`
  - `sealed record BadgeStanding(MemberBadgeInfo Info, CollectionTier Tier, long Value, bool Earned, DateTimeOffset? EarnedAt)` — reuses `CollectionTier` (and `CollectionTiers.Label` / `Medal`).
  - `static class MemberBadges` with:
    - nested `static class Keys` — one `const string` per spec key, named in PascalCase of the key (`Voice = "voice"`, `NightOwl = "night_owl"`, `FirstWake = "first_wake"`, …, `Critic = "critic"`; 31 constants). Every hook in Tasks 4–6 uses these, never a literal.
    - `IReadOnlyList<MemberBadgeInfo> All` — spec order: 12 paliers, 6 moments, 13 secrets.
    - `MemberBadgeInfo? ByKey(string key)`
    - `const long LightningMs = 3000;`
    - `CollectionTier TierFor(long value, IReadOnlyList<long> thresholds)`
    - `IReadOnlyList<BadgeStanding> Evaluate(MemberBadgeFacts facts, DateTimeOffset now)` — one standing per `All` entry, same order.
    - `string IconKey(MemberBadgeInfo info, CollectionTier tier)` — palier: `badge.<key>_<tier lowercase>`; others: `badge.<key>`.
    - `bool IsIconKey(string key)` — true for every `IconKey` of every badge (paliers: tiers Bronze..Diamond only).
    - `string Icon(BadgeStanding s)` — `ItemEmojis.For(IconKey(s.Info, s.Tier)) ?? placeholder`; placeholder = `CollectionTiers.Medal(tier) + Emoji` for an earned palier, else `Emoji`.
    - `bool IsAwardable(ulong guildId, string key)` — `guildId != 0` and the key is a moment, a secret, or one of the two counter paliers (`arcade`, `nanny`).

Raw palier units (thresholds in the same unit as the value): `voice` and `companion` in **minutes**
(`Unit = Hours`: voice `600, 3000, 9000, 24000, 60000`, companion `300, 1500, 4500, 12000, 30000`);
`seniority` in **whole days** since `JoinedAt` (`Unit = Days`: `90, 180, 365, 730, 1095`); all others
plain counts. A palier is earned at Bronze. Values: `arcade`/`nanny` = `Stored[key].Count` (0 if absent),
`seniority` = 0 when `JoinedAt` is null. Moments/secrets: `lightning` earned when
`0 < QuizBestMs < LightningMs`, `punctual` when `facts.Punctual` (both `EarnedAt` null); every other
moment/secret earned when its key is in `Stored` (`EarnedAt` from the row). Rows with keys not in
`All` are ignored.

- [ ] **Step 1: Write the failing checks** (section 34, in the file's style: `Fail(...)` on mismatch)

```csharp
// 34. Member badges: the catalog, tiers at their edges, derived moments, icon keys.
{
    var mb = MemberBadges.All;
    if (mb.Count(b => b.Kind == MemberBadgeKind.Palier) != 12 || mb.Count(b => b.Kind == MemberBadgeKind.Moment) != 6
        || mb.Count(b => b.Kind == MemberBadgeKind.Secret) != 13) Fail("member badges: catalog counts");
    if (mb.Select(b => b.Key).Distinct().Count() != mb.Count) Fail("member badges: duplicate key");
    foreach (var b in mb)
    {
        if (b.Kind == MemberBadgeKind.Palier != (b.Thresholds.Count == 5)) Fail($"{b.Key}: thresholds");
        if (b.Thresholds.Zip(b.Thresholds.Skip(1)).Any(p => p.First >= p.Second)) Fail($"{b.Key}: thresholds not ascending");
        if (string.IsNullOrWhiteSpace(b.Name) || string.IsNullOrWhiteSpace(b.Hint) || (b.Name + b.Hint).IndexOfAny(['{', '}']) >= 0) Fail($"{b.Key}: name/hint");
        foreach (var t in b.Kind == MemberBadgeKind.Palier ? Enum.GetValues<CollectionTier>().Skip(1) : [CollectionTier.None])
        {
            var key = MemberBadges.IconKey(b, t);
            var name = ItemEmojis.EmojiName(key);
            if (!MemberBadges.IsIconKey(key) || name is null || name.Length > 32 || !name.All(c => char.IsAsciiLetterOrDigit(c) || c == '_')) Fail($"{key}: icon key");
            if (ItemCatalog.Sets.Any(s => Enum.GetValues<CollectionTier>().Any(ct => ItemEmojis.BadgeKey(s.Key, ct) == key))) Fail($"{key}: clashes with a collection badge");
        }
    }
    var mbNow = new DateTimeOffset(2026, 10, 22, 12, 0, 0, TimeSpan.Zero);
    MemberBadgeFacts MbFacts(long voice = 0, long bestMs = 0, DateTimeOffset? joined = null, Dictionary<string, StoredBadge>? stored = null) =>
        new(voice, 0, 0, bestMs, 0, 0, 0, 0, 0, 0, false, joined, stored ?? new());
    BadgeStanding MbOf(MemberBadgeFacts f, string key) => MemberBadges.Evaluate(f, mbNow).Single(s => s.Info.Key == key);
    if (MbOf(MbFacts(voice: 599), "voice").Earned || MbOf(MbFacts(voice: 600), "voice").Tier != CollectionTier.Bronze
        || MbOf(MbFacts(voice: 60000), "voice").Tier != CollectionTier.Diamond) Fail("voice tier edges");
    if (MbOf(MbFacts(), "seniority").Earned) Fail("seniority null");
    if (MbOf(MbFacts(joined: mbNow.AddDays(-90)), "seniority").Tier != CollectionTier.Bronze) Fail("seniority 90 days");
    if (!MbOf(MbFacts(bestMs: 2999), "lightning").Earned || MbOf(MbFacts(bestMs: 3000), "lightning").Earned
        || MbOf(MbFacts(bestMs: 0), "lightning").Earned) Fail("lightning edges");
    var mbStored = new Dictionary<string, StoredBadge> { ["arcade"] = new(5, mbNow), ["hush"] = new(1, mbNow), ["gone_key"] = new(1, mbNow) };
    var mbAll = MemberBadges.Evaluate(MbFacts(stored: mbStored), mbNow);
    if (MbOf(MbFacts(stored: mbStored), "arcade").Tier != CollectionTier.Bronze || !MbOf(MbFacts(stored: mbStored), "hush").Earned) Fail("stored badges");
    if (mbAll.Count != mb.Count || mbAll.Count(s => s.Earned) != 2) Fail("unknown stored key");
    if (MemberBadges.IsAwardable(0, "hush") || MemberBadges.IsAwardable(1, "nope") || MemberBadges.IsAwardable(1, "voice")
        || !MemberBadges.IsAwardable(1, "nanny") || !MemberBadges.IsAwardable(1, "crowned")) Fail("IsAwardable");
}
```

- [ ] **Step 2: Run the checks to see them fail** — `dotnet run --project tests/ProjectSYNCS.Checks`; expected: build error, `MemberBadges` not found.

- [ ] **Step 3: Implement `MemberBadges.cs`** with the interfaces above and the spec's tables; header comment in the style of `CollectionTiers` (derived, keys stable). Make `ItemEmojis.IsIconKey` also return `MemberBadges.IsIconKey(key)`.

- [ ] **Step 4: Run the checks** — same command; expected: no `FAIL` line from section 34, `ALL CHECKS PASSED`.

---

### Task 2: Storage, reads and the award path

**Files:**
- Create: `ProjectSYNCS/Models/Xp/MemberBadge.cs`, `ProjectSYNCS/Services/Xp/MemberBadgeService.cs`, `ProjectSYNCS/Services/Xp/BadgeAwarder.cs`
- Modify: `ProjectSYNCS/Data/AppDbContext.cs` (DbSet + config), `ProjectSYNCS/Program.cs` (DI)
- Create (generated): migration `AddMemberBadges`

**Interfaces:**
- Consumes: Task 1 (`MemberBadgeFacts`, `StoredBadge`, `MemberBadges.IsAwardable`).
- Produces:
  - `class MemberBadge { int Id; ulong GuildId; ulong UserId; string Key; long Count; DateTimeOffset EarnedAt; }` — unique index (`GuildId`, `UserId`, `Key`).
  - `MemberBadgeService` (transient, takes `AppDbContext`):
    - `Task<MemberBadgeFacts> GetFactsAsync(ulong guildId, ulong userId, DateTimeOffset? joinedAt, DateTimeOffset now)`
    - `Task<bool> AwardAsync(ulong guildId, ulong userId, string key, DateTimeOffset now)` — insert `Count = 1` if absent; false if present.
    - `Task IncrementAsync(ulong guildId, ulong userId, string key, DateTimeOffset now)` — `ExecuteUpdateAsync(Count = Count + 1)`; on 0 rows, insert `Count = 1`; on a unique-index `DbUpdateException`, clear the tracker and run the update once more.
  - `BadgeAwarder` (singleton; `IServiceProvider`, `DiscordSocketClient`, `ILogger<BadgeAwarder>`):
    - `void Award(ulong guildId, ulong userId, string key)`
    - `void Increment(ulong guildId, ulong userId, string key)`

`GetFactsAsync` sources (spec *Definitions and known caveats*): `MemberXp` (VoiceMinutes, ReactionsUsed),
`VoiceCompanionStat` totals, `QuizStat` (Wins, BestMs), `BotFeedback.GoodCount`, giveaway entries with
`IsWinner` in this guild, Plynling badges joined to Plynlings with this `OwnerId` and guild, sessions
organised (`OrganizerId`, not cancelled, then `ScheduledAt < now` **in memory**), participants with
`FirstSeenInVoiceAt != null` on non-cancelled sessions of this guild (count = attended; `Punctual` =
any `FirstSeenInVoiceAt < ScheduledAt`, **in memory**), and every `MemberBadge` row as `Stored`.

`BadgeAwarder` rules: return at once unless `MemberBadges.IsAwardable(guildId, key)` and
`userId != _client.CurrentUser?.Id`. `Award` keeps a `HashSet<(ulong, ulong, string)>` of claims under
its own lock (bounded by members × badges, so no `BoundedSet` — it must be able to release); a claim
already held returns at once; otherwise `Task.Run` → `CreateAsyncScope` → `AwardAsync`; on exception
log a warning **and release the claim** (root `CLAUDE.md`, *Reliability*). `Increment` has no claim set.
Both use `DateTimeOffset.UtcNow`.

- [ ] **Step 1: Add the model, `DbSet<MemberBadge> MemberBadges`, and its config** (`GuildId`/`UserId` `.HasConversion<long>()`, the unique index).
- [ ] **Step 2: Implement `MemberBadgeService` and `BadgeAwarder`; register** `services.AddTransient<MemberBadgeService>()` beside `XpService` and `services.AddSingleton<BadgeAwarder>()` before `ChatterService`.
- [ ] **Step 3: Generate the migration** — `cd ProjectSYNCS && dotnet ef migrations add AddMemberBadges`; expected: one `CreateTable("MemberBadges")` plus the unique index, nothing else in `Up`.
- [ ] **Step 4: Build** — `dotnet build ProjectSYNCS -warnaserror`; expected: `0 Error(s)`.

---

### Task 3: The `/level` card line and the « Succès » view

**Files:**
- Create: `ProjectSYNCS/Commands/Xp/MemberBadgeCards.cs`
- Modify: `ProjectSYNCS/Commands/Xp/LevelModule.cs` (inject `MemberBadgeService`; card line; button; two handlers; delete `CardBadges`)
- Test: `tests/ProjectSYNCS.Checks/Program.cs` (section 34, plus replace the two `LevelModule.CardBadges` checks near line 1427 with `MemberBadgeCards.CardLine`)

**Interfaces:**
- Consumes: Task 1 (`BadgeStanding`, `MemberBadges.Evaluate/Icon`), Task 2 (`MemberBadgeService.GetFactsAsync`).
- Produces:
  - `enum BadgeSection { Collections, Paliers, Moments, Secrets }`
  - `static class MemberBadgeCards` (namespace `ProjectSYNCS.Commands`):
    - `string? CardLine(IReadOnlyCollection<CollectionCompletion> completions, IReadOnlyList<BadgeStanding> standings)` — `null` when nothing is earned; else `**Succès** · <icons>` then `-# N succès` (each completed set counts 1, each earned standing 1). Icon order: collection badges (`CollectionTiers.Badge`, catalog order), earned paliers at tier, earned moments, found secrets.
    - `MessageComponent BuildView(ulong userId, string displayName, BadgeSection section, IReadOnlyCollection<CollectionCompletion> completions, IReadOnlyList<BadgeStanding> standings)`
    - `string OpenId(ulong userId)` → `level:badges:{userId}`; `string TabId(ulong userId, BadgeSection s)` → `level:badgetab:{userId}:{s}`.

View shape: one purple container — heading `## 🏅 Succès de {Format.Sanitize(displayName)}`, separator,
one TextDisplay with the section's lines (formats per the spec's *« Succès » view*; paliers progress
`37 h / 50 h pour Argent` using `CollectionTiers.Label`; moment and secret dates `<t:unix:D>`; the
two derived moments without a date; Secrets heading `X / 13 trouvés`) — then one action row of four
tab buttons (Collections, Paliers, Moments, Secrets), the current one disabled. **The view opens on
Paliers** (collections already have `/inventory`). One page per section from the start: the spec
allows paging when one message doesn't fit, and all four sections together come to roughly 4600
characters at worst, over the 4000 V2 text cap.

`LevelModule`:
- `LevelAsync`: `joinedAt = Context.Guild.GetUser(target.Id)?.JoinedAt`; facts → `Evaluate` → `CardLine` replaces `CardBadges`. The action row gains `« Succès »` (secondary, 🏅, `OpenId(target.Id)`). Card stays ≤ 11 components.
- `[ComponentInteraction("level:badges:*", ignoreGroupNames: true)] OnBadgesAsync(string userId)`: `DeferAsync(ephemeral: true)`, gather, `FollowupAsync(components: BuildView(..., Paliers, ...), flags: ComponentsV2, ephemeral: true, allowedMentions: None)`.
- `[ComponentInteraction("level:badgetab:*:*", ignoreGroupNames: true)] OnBadgeTabAsync(string userId, string section)`: `DeferAsync()`, gather, `ModifyOriginalResponseAsync` with `Components`, `Flags = ComponentsV2`, `AllowedMentions.None`.
- Display name: `Context.Guild.GetUser(id)` → `BotResponses.DisplayNameFor`, else `await Context.Client.GetUserAsync(id)`, else « ce membre ».

- [ ] **Step 1: Write the failing checks** (append to section 34):

```csharp
    var mbEmpty = MemberBadges.Evaluate(MbFacts(), mbNow);
    // MemberBadgeCards and BadgeSection live in ProjectSYNCS.Commands, which the file doesn't import:
    // add `using ProjectSYNCS.Commands;` at the top, or qualify them as the file does for LevelModule.
    if (MemberBadgeCards.CardLine(new List<ProjectSYNCS.Models.CollectionCompletion>(), mbEmpty) is not null) Fail("empty card line");
    // Worst case: everything at Diamant, every secret found, every icon a real emoji markup.
    var mbMax = new MemberBadgeFacts(99_999_999, 99_999_999, 99_999, 1, 99_999_999, 99_999, 99_999, 99_999, 99_999_999, 99_999, true,
        mbNow.AddYears(-20), mb.ToDictionary(b => b.Key, _ => new StoredBadge(99_999_999, mbNow)));
    var mbDone = ItemCatalog.Sets.Select(s => new ProjectSYNCS.Models.CollectionCompletion { SetKey = s.Key, Count = 999 }).ToList();
    foreach (var b in mb) foreach (var t in Enum.GetValues<CollectionTier>())
        ItemEmojis.Set(MemberBadges.IconKey(b, t), ItemEmojis.Markup("b_" + new string('x', 30), ulong.MaxValue));
    foreach (var (facts, done, what) in new[] { (MbFacts(), new List<ProjectSYNCS.Models.CollectionCompletion>(), "empty view"), (mbMax, mbDone, "worst view") })
    foreach (var section in Enum.GetValues<BadgeSection>())
    {
        var view = MemberBadgeCards.BuildView(ulong.MaxValue, new string('*', 32), section, done, MemberBadges.Evaluate(facts, mbNow));
        // Sum every TextDisplay's content in the tree; count every component; collect every custom-id.
        var (text, count, ids) = V2Measure(view);
        if (text > 4000 || count > 40 || ids.Count != ids.Distinct().Count()) Fail($"{what} {section}: {text} chars, {count} components");
    }
    if (MemberBadgeCards.CardLine(mbDone, MemberBadges.Evaluate(mbMax, mbNow)) is not { } mbLine || mbLine.Length > 4000) Fail("full card line");
    ItemEmojis.Clear();
```

Add a local `(int Text, int Count, List<string> Ids) V2Measure(Discord.MessageComponent c)` that walks
`ContainerComponent`, `SectionComponent`, `ActionRowComponent`, `TextDisplayComponent`, `ButtonComponent`
recursively.

- [ ] **Step 2: Run the checks to see them fail** — expected: build error, `MemberBadgeCards` not found.
- [ ] **Step 3: Implement `MemberBadgeCards` and the `LevelModule` changes above;** update the two old `CardBadges` checks to call `MemberBadgeCards.CardLine(allDone, MemberBadges.Evaluate(<empty facts>, now))`.
- [ ] **Step 4: Run the checks and build** — `dotnet run --project tests/ProjectSYNCS.Checks` → `ALL CHECKS PASSED`; `dotnet build ProjectSYNCS -warnaserror` → `0 Error(s)`.

---

### Task 4: Hooks on the command and game paths

**Files (modify):** `Services/Plynling/PlynlingService.cs`, `Services/Economy/InventoryService.cs`,
`Interactions/Components/TonightComponentHandler.cs`, `Services/Quiz/QuizCrownService.cs`,
`Interactions/Components/PartyComponentHandler.cs`, `Services/Arcade/ArcadeRunner.cs`,
`Services/Arcade/PartyRunner.cs` (all under `ProjectSYNCS/`).

**Interfaces:** Consumes `BadgeAwarder.Award/Increment` and `MemberBadges.Keys` (Tasks 1–2); inject `BadgeAwarder` into each class.

| Badge | Where, exactly | Call |
|---|---|---|
| `nanny` | `PlynlingService.FeedAsync`, after `SaveChangesAsync`, when `plynling.OwnerId != actorId` | `Increment(plynling.GuildId, actorId, Keys.Nanny)` |
| `generous` | `InventoryService.GiveAsync` (the public wrapper, **not** the retried `GiveOnceAsync`), when the outcome is `Given` | `Award(guildId, fromId, Keys.Generous)` |
| `rallier` | `TonightComponentHandler.AnswerAsync`, inside the `ClaimThresholdAsync` branch | `Award(guild, updated.OrganizerId, Keys.Rallier)` |
| `crowned` | `QuizCrownService.CrownAsync`, only when `pay` is true, each champion | `Award(guildId, id, Keys.Crowned)` |
| `quokka` | `PartyComponentHandler.OnQuokkaHitAsync`, on the winning click only | `Award(guild, Context.User.Id, Keys.Quokka)` |
| `arcade` | every game ending, once per human winner (rule below) | `Increment(guild, id, Keys.Arcade)` |

Arcade wins (spec *Awarding*): Duel `winner` in `ArcadeRunner.FinishDuelAsync`; Heist success → each
of `settled.Payers` in `LaunchHeistAsync`; Race (`PartyRunner.RunRaceAsync`) and Crash (`FlyAsync`) →
each player whose net is positive; Blackjack `Natural`/`Win` → `g.PlayerId` in `FinishBlackjackAsync`;
Bomb → every player except `loser` in `LightAsync`; Temperature → `winners`; boards → the winning
human in `FinishBoardAsync`; Quote → `Context.User.Id` on a correct guess in
`PartyComponentHandler.OnQuoteGuessAsync`. Her own id is filtered by `BadgeAwarder`.

- [ ] **Step 1: Add each call** at the place named, after the flow's own save.
- [ ] **Step 2: Build** — `dotnet build ProjectSYNCS -warnaserror` → `0 Error(s)`; `dotnet run --project tests/ProjectSYNCS.Checks` → `ALL CHECKS PASSED` (constructor changes can break checks that build services).

---

### Task 5: Secrets in her chat (ChatterService, /ship, /rate)

**Files:**
- Modify: `ProjectSYNCS/Helpers/Personality/Shipping.cs`, `ProjectSYNCS/Services/Personality/ChatterService.cs`, `ProjectSYNCS/Commands/Personality/ShipModule.cs`, `ProjectSYNCS/Commands/Personality/RateModule.cs`
- Test: section 34

**Interfaces:**
- Produces: `public static bool Shipping.IsSelfWithHer(string a, string b, ulong askerId, ulong selfId, ulong ownerId, ulong tataId)` — true when one side's `Rating.Rate(...).Kind == RatingKind.Herself` and the other side's `Rating.Rate(...).AboutAsker` is true.
- Consumes: `BadgeAwarder.Award`, `MemberBadges.Keys`. Guild id everywhere: `(message.Channel as SocketGuildChannel)?.Guild.Id ?? 0` or `Context.Guild?.Id ?? 0` (0 → no award).

| Badge | Where |
|---|---|
| `breakdown` | `ChatterService`, both breakdown sites (mention path and reply path, random roll and passphrase), when the breakdown actually starts |
| `quoicoubeh` / `tron` | `TryQuoicoubehAsync` / `TryTronAsync`, after a successful send |
| `threat` | `TryHandleShutdownThreatAsync`, once it answers |
| `hush` | `TryIntent`, where `_hush.Hush(...)` is called |
| `heart` | `ChatterService.TryShip` and `ShipModule.ShipAsync`, after the `@random` draws, when `IsSelfWithHer(a, b, asker, self, AvailabilityService.OwnerId, BotResponses.TataId)` |
| `critic` | `RateModule.RateAsync`, when `result.Kind == RatingKind.Herself` |

- [ ] **Step 1: Write the failing checks** (append to section 34; `selfId`, `askerId`, `otherId` are distinct test ids):

```csharp
    const ulong mbSelf = 111, mbAsker = 222, mbOther = 333;
    var mbTata = (ulong)br.GetField("TataId")!.GetValue(null)!; // BotResponses is internal: reflection, like the rest of the file
    bool Heart(string a, string b, ulong asker = mbAsker) => Shipping.IsSelfWithHer(a, b, asker, mbSelf, AvailabilityService.OwnerId, mbTata);
    if (!Heart("syncs", "moi") || !Heart("moi", "toi") || !Heart($"<@{mbAsker}>", "syncs")
        || !Heart("moi", "syncs", AvailabilityService.OwnerId)) Fail("heart: self with her");
    if (Heart("syncs", "pizza") || Heart("syncs", $"<@{mbOther}>") || Heart("moi", "pizza") || Heart("syncs", "toi")) Fail("heart: not self with her");
```

- [ ] **Step 2: Run the checks to see them fail** — expected: build error, `IsSelfWithHer` not found.
- [ ] **Step 3: Implement `IsSelfWithHer` and the six hooks** (fire-and-forget calls; nothing awaited on the gateway path).
- [ ] **Step 4: Run the checks and build** — `ALL CHECKS PASSED`; `0 Error(s)`.

---

### Task 6: Secrets in the ambient and feedback paths

**Files:**
- Create: `ProjectSYNCS/Helpers/Feedback/FeedbackFlip.cs`
- Modify: `ProjectSYNCS/Services/Personality/AmbientService.cs`, `ProjectSYNCS/Helpers/Personality/Ambient.cs`, `ProjectSYNCS/Services/Personality/SynthiaService.cs`, `ProjectSYNCS/Services/Personality/GlitchService.cs`, `ProjectSYNCS/Services/Personality/RivalryService.cs`, `ProjectSYNCS/Services/Feedback/BotFeedbackTracker.cs`
- Test: section 34

**Interfaces:**
- Produces:
  - `sealed class FeedbackFlip` (owns its lock): `static readonly TimeSpan Window = TimeSpan.FromSeconds(60)`; `bool Record(ulong userId, FeedbackKind verdict, DateTimeOffset now)` — true when this user's previous recorded verdict was the opposite one and less than `Window` ago; prunes entries older than `Window` on each call.
  - `Ambient.FirstWakeWindow = TimeSpan.FromHours(2)`.
- Consumes: `BadgeAwarder.Award`, `MemberBadges.Keys`.

| Badge | Where |
|---|---|
| `night_owl` | `AmbientService.HandleMessageAsync`, when `scold` was set (after the lock) |
| `first_wake` | New `DateTimeOffset? _firstWakeUntil` under `_gate`: set to `now + Ambient.FirstWakeWindow` in `WakeAsync` when `abrupt` and `sent is not null`, and in `GlitchWakeAsync` when its line was sent. In `HandleMessageAsync`, inside the existing lock: take and clear it; award if `now <` it. Its own field — never `_pendingEdit`. |
| `synthia` | `SynthiaService.HandleMessageAsync`, after `_daily.TryClaim(0)` succeeds |
| `glitch` | `GlitchService.ReplyWithTypingAsync`, when the glitch fired and `replyTo.Author` is not a bot |
| `traitor` | `RivalryService.OnPraiseStolenAsync`, praiser `praise.Author` |
| `weathervane` | `BotFeedbackTracker.HandleMessageAsync`, right after `var claim = TryClaim(...)`: when `claim is Claim.Granted or Claim.AlreadyJudged` and `_flip.Record(author, verdict, now)` |

- [ ] **Step 1: Write the failing checks** (append to section 34):

```csharp
    var flip = new FeedbackFlip();
    if (flip.Record(1, FeedbackKind.Good, mbNow) || !flip.Record(1, FeedbackKind.Bad, mbNow.AddSeconds(30))) Fail("flip within a minute");
    if (flip.Record(2, FeedbackKind.Good, mbNow) || flip.Record(2, FeedbackKind.Bad, mbNow.AddSeconds(61))) Fail("flip after a minute");
    if (flip.Record(3, FeedbackKind.Good, mbNow) || flip.Record(3, FeedbackKind.Good, mbNow.AddSeconds(5))) Fail("same verdict twice");
    if (flip.Record(4, FeedbackKind.Good, mbNow) || flip.Record(5, FeedbackKind.Bad, mbNow.AddSeconds(5))) Fail("two people");
```

- [ ] **Step 2: Run the checks to see them fail** — expected: build error, `FeedbackFlip` not found.
- [ ] **Step 3: Implement `FeedbackFlip`, the constant, and the six hooks;** inject `BadgeAwarder` into `AmbientService`, `SynthiaService`, `GlitchService`, `RivalryService`, `BotFeedbackTracker` (check `GlitchService`/`StickerService`/`BreakdownService` for a DI cycle: `BadgeAwarder` depends on none of them, so none should appear).
- [ ] **Step 4: Run the checks and build** — `ALL CHECKS PASSED`; `0 Error(s)`.

---

### Task 7: Docs, help and version

**Files (modify):** `ProjectSYNCS/config.yaml`, `ProjectSYNCS/CHANGELOG.md`, `README.md`,
`ProjectSYNCS/Commands/Core/HelpModule.cs`, `docs/agents/xp.md`, `CLAUDE.md`.

- [ ] **Step 1: Version** — `config.yaml` `version: "7.34.0"` (check `git diff` first: bump once).
- [ ] **Step 2: Changelog** — `## 7.34.0 — <date>` on top, English: « Succès » button on `/level`; tiered badges and moments; « some badges are secret ». No secret names or triggers.
- [ ] **Step 3: README and `/help`** — README line 22 and the `/level` paragraph (~273) mention the « Succès » button and the visible badges; `HelpModule` line 88 likewise (« badges de collection » → « succès »). `/help` is near the 6000 cap: the checks measure it — keep the change short. No secrets in either.
- [ ] **Step 4: `docs/agents/xp.md`** — replace the `CardBadges` paragraph (line ~84) with a *Member badges* section: catalog in `Helpers/Xp/MemberBadges`, one table `MemberBadge`, paliers derived and never stored, the single write path `BadgeAwarder` (after the flow's save, claim released on failure, guild 0 and her id ignored), no announcements, secrets out of help/README/changelog. In root `CLAUDE.md`, add « member badges, « Succès » » to the XP row of the subsystem table.
- [ ] **Step 5: Final verification** — `dotnet build ProjectSYNCS -warnaserror` → `0 Error(s)`; `dotnet run --project tests/ProjectSYNCS.Checks` → `ALL CHECKS PASSED`. Then run against the dev guild: `/level` on yourself and on someone with nothing; open « Succès », click all four tabs; `/rate syncs` and `/ship moi syncs` then reopen « Succès » → Critique and Cœur de silicium found.
