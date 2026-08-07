# Level-Up Card Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Turn SYNCS's plain-text level-up announcement into a small embed card (avatar thumbnail, "old → new" level title, purple color), add a fixed "SIX SEVEEEEN" line at levels 7/67, and ship a one-time migration that resets everyone's accumulated XP.

**Architecture:** All behavioral logic lives in `XpTracker` (already the single place every XP signal funnels through, per `ProjectSYNCS/Services/XpTracker.cs`). Two small pure `private static` helpers make the title/easter-egg logic independently testable without a live Discord connection; the embed assembly itself, like every other Discord-facing piece in this codebase, is build-verified only (no test project exists — see `CLAUDE.md`). The reset ships as a new, schema-empty EF Core migration, following the repo's existing "migrations apply automatically on startup" convention rather than a one-off manual DB edit.

**Tech Stack:** .NET 10, Discord.Net 3.20 (`EmbedBuilder`, `IUser.GetAvatarUrl()`), EF Core 10 / SQLite.

## Global Constraints

- All user-facing strings stay in French, except the literal `"SIX SEVEEEEN"` line, which is intentionally the English meme phrase verbatim — do not translate or alter its spelling/casing.
- `dotnet build` must stay clean under `-warnaserror` (the project's standing build command).
- No test project exists in this repo. Verification is: (a) reflection-based scratch console programs for pure logic, referencing `ProjectSYNCS.csproj` via `ProjectReference`, mirroring this session's established `cuecheck`/`xpcheck` pattern; (b) `dotnet build`; (c) for the migration, a real SQLite-backed throwaway database.
- Every place in this codebase that resolves a display name over a fallback chain uses `BotResponses.DisplayNameFor`. Do not introduce a second copy of that chain.
- Wherever this plan says `$SCRATCH`, substitute your own scratch/temp working directory. Each scratch console project references the real project via:
  ```xml
  <ProjectReference Include="C:\Users\c235773\Desktop\Sources - RME\Discord Bots\SessionOrganizer\ProjectSYNCS\ProjectSYNCS.csproj" />
  ```

---

### Task 1: Level-up card — title/easter-egg logic + embed assembly

**Files:**
- Modify: `ProjectSYNCS/Services/XpTracker.cs` (`GrantAsync`, `AnnounceAsync`, `ResolveName` → `ResolveUser`; two new private static helpers)
- Modify: `ProjectSYNCS/Helpers/BotChat.cs` (new `PostEmbedWithTypingAsync` method)
- Test: `$SCRATCH/levelupcheck/Program.cs` (scratch, not committed)

**Interfaces:**
- Consumes: `BotResponses.XpLevelUpLines` (`string[]`, unchanged), `BotResponses.DisplayNameFor(ulong, string)` (unchanged), `ResponsePicker.Pick(ulong, string[])` (unchanged), `XpService.AddXpAsync` returning `(int OldLevel, int NewLevel)` (unchanged, already exists).
- Produces: `BotChat.PostEmbedWithTypingAsync(IMessageChannel channel, Embed embed, string delayText, ILogger logger, string what)` — used only by `XpTracker` for now, but public/static like its siblings so any future embed-shaped announcement can reuse it.

- [ ] **Step 1: Write the failing scratch test**

Create the scratch project:

```bash
mkdir -p "$SCRATCH/levelupcheck"
cd "$SCRATCH/levelupcheck"
dotnet new console -o . --force
```

Overwrite `$SCRATCH/levelupcheck/levelupcheck.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="C:\Users\c235773\Desktop\Sources - RME\Discord Bots\SessionOrganizer\ProjectSYNCS\ProjectSYNCS.csproj" />
  </ItemGroup>
</Project>
```

Overwrite `$SCRATCH/levelupcheck/Program.cs`:

```csharp
using System.Reflection;

var asm = typeof(ProjectSYNCS.Services.XpService).Assembly;
var trackerType = asm.GetType("ProjectSYNCS.Services.XpTracker")
    ?? throw new Exception("XpTracker type not found");

var buildTitle = trackerType.GetMethod("BuildLevelUpTitle", BindingFlags.NonPublic | BindingFlags.Static)
    ?? throw new Exception("BuildLevelUpTitle not found");
var isSixSeven = trackerType.GetMethod("IsSixSeven", BindingFlags.NonPublic | BindingFlags.Static)
    ?? throw new Exception("IsSixSeven not found");

int pass = 0, fail = 0;
void Check(string label, bool condition)
{
    if (condition) pass++;
    else { fail++; Console.WriteLine($"FAIL: {label}"); }
}

string Title(int oldLevel, int newLevel) => (string)buildTitle.Invoke(null, new object[] { oldLevel, newLevel })!;
bool SixSeven(int level) => (bool)isSixSeven.Invoke(null, new object[] { level })!;

Check("title shows a single-level jump", Title(3, 4) == "Niveau 3 \u2192 4 !");
Check("title shows a multi-level jump", Title(5, 8) == "Niveau 5 \u2192 8 !");
Check("title handles level 0 to 1", Title(0, 1) == "Niveau 0 \u2192 1 !");
Check("7 is six-seven", SixSeven(7));
Check("67 is six-seven", SixSeven(67));
Check("6 is not six-seven", !SixSeven(6));
Check("17 is not six-seven (contains a 7, must not match)", !SixSeven(17));
Check("70 is not six-seven", !SixSeven(70));
Check("0 is not six-seven", !SixSeven(0));

Console.WriteLine($"{pass} passed, {fail} failed");
if (fail > 0) Environment.Exit(1);
```

- [ ] **Step 2: Run it, confirm it fails**

```bash
dotnet run --project "$SCRATCH/levelupcheck"
```

Expected: throws `Exception: BuildLevelUpTitle not found` (or `XpTracker type not found` if the assembly hasn't been rebuilt since your last pull) — the methods don't exist yet.

- [ ] **Step 3: Implement in `ProjectSYNCS/Helpers/BotChat.cs`**

Add this method to the `BotChat` class, directly after the existing `PostWithTypingAsync` method (before the `TypingDelayFor` comment):

```csharp
    public static async Task PostEmbedWithTypingAsync(
        IMessageChannel channel, Embed embed, string delayText, ILogger logger, string what)
    {
        try
        {
            using (channel.EnterTypingState())
            {
                await Task.Delay(TypingDelayFor(delayText));
            }
            await channel.SendMessageAsync(embed: embed);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send {What} in channel {ChannelId}.", what, channel.Id);
        }
    }
```

Also update the class's doc comment (the block starting `// The single send path for the bot's *own* chatter.`) to add one sentence: `// Two entry points share that pause: PostWithTypingAsync/ReplyWithTypingAsync for text, PostEmbedWithTypingAsync for an embed (the level-up card, so far its only caller).`

- [ ] **Step 4: Implement in `ProjectSYNCS/Services/XpTracker.cs`**

Replace the existing `AnnounceAsync` and `ResolveName` methods (and the one call site in `GrantAsync`) as follows.

In `GrantAsync`, change:

```csharp
            if (newLevel > oldLevel && channel is not null)
                await AnnounceAsync(channel, userId, newLevel, knownUser);
```

to:

```csharp
            if (newLevel > oldLevel && channel is not null)
                await AnnounceAsync(channel, userId, oldLevel, newLevel, knownUser);
```

Replace the whole `AnnounceAsync` method with:

```csharp
    private async Task AnnounceAsync(IMessageChannel channel, ulong userId, int oldLevel, int newLevel, IUser? knownUser)
    {
        var user = ResolveUser(channel, userId, knownUser);
        // Couldn't resolve the member (rare cache miss) — the level is still
        // recorded, only the celebration is skipped, matching "Discord side effects
        // must never break the flow."
        if (user is null) return;

        var name = BotResponses.DisplayNameFor(userId,
            (user as SocketGuildUser)?.Nickname ?? user.GlobalName ?? user.Username);
        var avatarUrl = user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl();

        // 7 and 67 are a fixed line, not a pool pick — ResponsePicker is skipped
        // entirely so it never burns one of that channel's "recently used" slots on
        // a line that isn't actually a pool entry.
        var description = IsSixSeven(newLevel)
            ? "SIX SEVEEEEN"
            : string.Format(_picker.Pick(channel.Id, BotResponses.XpLevelUpLines), name, newLevel);

        var embed = new EmbedBuilder()
            .WithTitle(BuildLevelUpTitle(oldLevel, newLevel))
            .WithDescription(description)
            .WithThumbnailUrl(avatarUrl)
            .WithColor(Color.Purple)
            .Build();

        await BotChat.PostEmbedWithTypingAsync(channel, embed, description, _logger, "level-up announcement");
    }

    // "Niveau {old} → {new} !" — shows the span on a multi-level jump (one grant can
    // cross more than one threshold), never just the final number.
    private static string BuildLevelUpTitle(int oldLevel, int newLevel) =>
        $"Niveau {oldLevel} \u2192 {newLevel} !";

    // The level-67/level-7 meme easter egg. A literal level check, not a vocabulary
    // cue — nothing to do with MessageCues.
    private static bool IsSixSeven(int level) => level is 7 or 67;

    // Where the caller already has an IUser (the message path), skip the lookup.
    // Otherwise resolve through the guild the channel belongs to. Name and avatar are
    // both derived from whichever IUser this returns, so the fallback chain in
    // AnnounceAsync applies uniformly regardless of which branch resolved it.
    private static IUser? ResolveUser(IMessageChannel channel, ulong userId, IUser? knownUser) =>
        knownUser ?? (channel as SocketGuildChannel)?.Guild.GetUser(userId);
```

Delete the old `ResolveName` method entirely (it's fully replaced by `ResolveUser` plus the name-building line now inlined in `AnnounceAsync`).

- [ ] **Step 5: Run the scratch test again, confirm it passes**

```bash
dotnet run --project "$SCRATCH/levelupcheck"
```

Expected: `9 passed, 0 failed`.

- [ ] **Step 6: Full solution build**

```bash
cd "ProjectSYNCS"
dotnet build -warnaserror
```

Expected: build succeeds, 0 errors, 0 warnings. This is what actually verifies the embed-assembly code (`EmbedBuilder`, `IUser.GetAvatarUrl()`, `BotChat.PostEmbedWithTypingAsync`) compiles correctly — those pieces touch live Discord.Net gateway types and can't be exercised outside a real connection, consistent with how every other Discord-facing method in this codebase is verified (build + manual dev-guild check, per `CLAUDE.md`).

- [ ] **Step 7: Commit**

```bash
git add ProjectSYNCS/Services/XpTracker.cs ProjectSYNCS/Helpers/BotChat.cs
git commit -m "$(cat <<'EOF'
Turn the level-up announcement into an embed card

Avatar thumbnail, a title showing the level span (old -> new) instead
of just the new number, and a fixed "SIX SEVEEEEN" line at levels 7
and 67 in place of the usual pool pick.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

### Task 2: One-time XP reset migration

**Files:**
- Create: `ProjectSYNCS/Migrations/<timestamp>_ResetMemberXp.cs` (name generated by the EF CLI)
- Create: `ProjectSYNCS/Migrations/<timestamp>_ResetMemberXp.Designer.cs` (generated)
- Modify: `ProjectSYNCS/Migrations/AppDbContextModelSnapshot.cs` (regenerated by the CLI; no actual model change, so this should come out byte-identical or near-identical)
- Test: `$SCRATCH/xpresetcheck/Program.cs` (scratch, not committed)

**Interfaces:**
- Consumes: `ProjectSYNCS.Data.AppDbContext`, `ProjectSYNCS.Models.MemberXp` (both unchanged, used read/write from the scratch verification program).
- Produces: nothing consumed by later tasks — this is a standalone data fix.

- [ ] **Step 1: Generate the migration**

```bash
cd "ProjectSYNCS"
dotnet ef migrations add ResetMemberXp
```

This creates an empty migration (no `CreateTable`/`AddColumn` calls in `Up()`/`Down()`) since there is no model change — that's expected for a data-only migration.

- [ ] **Step 2: Edit the generated migration**

Open the newly created `ProjectSYNCS/Migrations/<timestamp>_ResetMemberXp.cs`. Replace its empty `Up`/`Down` bodies with:

```csharp
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // One-time reset alongside the level-up card rework — every existing
            // total predates the new card format. Data-only, no schema change.
            migrationBuilder.Sql("DELETE FROM MemberXps;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // A delete can't be meaningfully reversed — the original totals are gone.
        }
```

- [ ] **Step 3: Build**

```bash
dotnet build -warnaserror
```

Expected: 0 errors, 0 warnings.

- [ ] **Step 4: Write the throwaway-database verification program**

```bash
mkdir -p "$SCRATCH/xpresetcheck"
cd "$SCRATCH/xpresetcheck"
dotnet new console -o . --force
```

Overwrite `$SCRATCH/xpresetcheck/xpresetcheck.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="C:\Users\c235773\Desktop\Sources - RME\Discord Bots\SessionOrganizer\ProjectSYNCS\ProjectSYNCS.csproj" />
  </ItemGroup>
</Project>
```

Overwrite `$SCRATCH/xpresetcheck/Program.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using ProjectSYNCS.Data;
using ProjectSYNCS.Models;

if (args.Length < 2)
{
    Console.WriteLine("Usage: xpresetcheck <db-path> <seed|count>");
    Environment.Exit(1);
}

var dbPath = args[0];
var mode = args[1];

var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseSqlite($"Data Source={dbPath}")
    .Options;

using var db = new AppDbContext(options);

if (mode == "seed")
{
    db.MemberXps.Add(new MemberXp { GuildId = 111, UserId = 222, TotalXp = 999 });
    db.SaveChanges();
    Console.WriteLine("Seeded 1 row.");
}
else
{
    Console.WriteLine($"MemberXps count: {db.MemberXps.Count()}");
}
```

- [ ] **Step 5: Build the schema up to (not including) the reset migration, seed a row, confirm it's there**

```bash
rm -f "$SCRATCH/xpresetcheck/reset-test.db"
export Database__Path="$SCRATCH/xpresetcheck/reset-test.db"
(cd "ProjectSYNCS" && dotnet ef database update AddMemberXp)
dotnet run --project "$SCRATCH/xpresetcheck" -- "$SCRATCH/xpresetcheck/reset-test.db" seed
dotnet run --project "$SCRATCH/xpresetcheck" -- "$SCRATCH/xpresetcheck/reset-test.db" count
```

Expected final line: `MemberXps count: 1`.

- [ ] **Step 6: Apply the reset migration, confirm the row is gone**

```bash
(cd "ProjectSYNCS" && dotnet ef database update)
dotnet run --project "$SCRATCH/xpresetcheck" -- "$SCRATCH/xpresetcheck/reset-test.db" count
```

Expected: `MemberXps count: 0`.

```bash
unset Database__Path
rm -f "$SCRATCH/xpresetcheck/reset-test.db"
```

- [ ] **Step 7: Commit**

```bash
git add ProjectSYNCS/Migrations/
git commit -m "$(cat <<'EOF'
Add one-time migration to reset MemberXp totals

Data-only migration (no schema change) that clears everyone's
accumulated XP, timed to ship with the level-up card rework so
totals and the new card format start clean together.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

### Task 3: Docs

**Files:**
- Modify: `CLAUDE.md`
- Modify: `README.md`
- Modify: `ProjectSYNCS/Services/BotResponses.cs:597-601` (the doc comment above `XpLevelUpLines`)

**Interfaces:**
- Consumes: nothing new — this task only updates prose to match Tasks 1–2.
- Produces: nothing consumed by other tasks.

- [ ] **Step 1: Update the `XpLevelUpLines` doc comment**

In `ProjectSYNCS/Services/BotResponses.cs`, the comment directly above `public static readonly string[] XpLevelUpLines =` currently reads:

```csharp
    // Posted (not as a reply) when someone crosses a level in SYNCS's own XP system —
    // distinct from LevelUpCheers, which is her reacting to the *other* leveling bot's
    // announcements. This one she owns: her system, her tally, her voice. Deliberately
    // named to not read close to Helpers.LevelUpAnnouncement (the other bot's
    // detector). {0} = the person's name, {1} = their new level.
```

Replace it with:

```csharp
    // Posted (not as a reply) when someone crosses a level in SYNCS's own XP system —
    // distinct from LevelUpCheers, which is her reacting to the *other* leveling bot's
    // announcements. This one she owns: her system, her tally, her voice. Deliberately
    // named to not read close to Helpers.LevelUpAnnouncement (the other bot's
    // detector). {0} = the person's name, {1} = their new level.
    //
    // Rendered as an embed's description, not a plain message — see
    // XpTracker.AnnounceAsync. At level 7 or 67 this pool isn't consulted at all;
    // the description is the literal string "SIX SEVEEEEN" instead.
```

- [ ] **Step 2: Update `CLAUDE.md`'s `BotChat` paragraph**

Find this paragraph (currently around line 431):

```
**`Helpers/BotChat` is the single send path for the bot's own chatter**, and
`Helpers/EmoteMarkup.Parse` the single reaction parser. Both were private members of
`ChatterService` / `ReactionService` until `BotFeedbackTracker` needed them too. The
typing-delay clamp in `BotChat` has to stay inside Discord.Net's 3 s `HandlerTimeout`,
and the parser carries the id-less-markup trap above — neither is a constant worth
having two copies of. `BreakdownService` still keeps its own much slower pacing.
```

Replace it with:

```
**`Helpers/BotChat` is the single send path for the bot's own chatter**, and
`Helpers/EmoteMarkup.Parse` the single reaction parser. Both were private members of
`ChatterService` / `ReactionService` until `BotFeedbackTracker` needed them too. The
typing-delay clamp in `BotChat` has to stay inside Discord.Net's 3 s `HandlerTimeout`,
and the parser carries the id-less-markup trap above — neither is a constant worth
having two copies of. `BreakdownService` still keeps its own much slower pacing.
`BotChat` has two send methods sharing that same pause — `PostWithTypingAsync` /
`ReplyWithTypingAsync` for plain text, `PostEmbedWithTypingAsync` for an embed (the
level-up card is its only caller so far).
```

- [ ] **Step 3: Add a `CLAUDE.md` entry for the level-up card**

Find this line (currently around line 545):

```
must never push the acknowledgement itself later.
```

It ends a paragraph, followed by a blank line and then `**`VoiceXpService` sweeps instead of tracking join/leave/mute events.**`. Insert a new paragraph between them:

```
must never push the acknowledgement itself later.

**The level-up announcement is a card, not a plain message.** `XpTracker.AnnounceAsync`
posts an embed — avatar thumbnail, `Color.Purple`, and a title showing the span
crossed (`Niveau {old} → {new} !`, not just the final number, so a multi-level jump
in one grant still reads correctly). At level **7 or 67** the description is the
literal string `"SIX SEVEEEEN"` instead of a picked `XpLevelUpLines` line — a fixed
easter egg, not a pool entry, so `ResponsePicker` is never consulted for it and it
never burns that channel's "recently used" exclusion slot.

**`VoiceXpService` sweeps instead of tracking join/leave/mute events.** Voice XP is
```

(The last line above is the existing text shown only so the insertion point is unambiguous — don't duplicate it.)

- [ ] **Step 4: Update `README.md`**

Find this line (currently around line 54):

```
  bonus for replying to or mentioning her, and for a genuinely-recorded good/bad-bot
  verdict. Crossing a level gets an unprompted announcement in her own voice. A single
```

Replace `Crossing a level gets an unprompted announcement in her own voice.` with:
`Crossing a level gets an unprompted card announcement in her own voice, avatar and all.`

So the surrounding text reads:

```
  bonus for replying to or mentioning her, and for a genuinely-recorded good/bad-bot
  verdict. Crossing a level gets an unprompted card announcement in her own voice,
  avatar and all. A single
```

(Rewrap the paragraph to the file's existing line width as you edit it — don't leave it as one long line.)

- [ ] **Step 5: Build**

```bash
cd "ProjectSYNCS"
dotnet build -warnaserror
```

Expected: 0 errors, 0 warnings (docs-only changes, but this confirms nothing else broke since Task 2).

- [ ] **Step 6: Commit**

```bash
git add CLAUDE.md README.md ProjectSYNCS/Services/BotResponses.cs
git commit -m "$(cat <<'EOF'
Document the level-up card and SIX SEVEEEEN easter egg

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

## Final verification

After all three tasks:

1. `cd "ProjectSYNCS" && dotnet build -warnaserror` — clean.
2. `git log --oneline -5` — three new commits (card logic, migration, docs), each with a clean `git status` in between.
3. Manual, dev guild (when credentials are available): trigger a real level-up and confirm the card renders — title shows the span, description shows either a pool line or `"SIX SEVEEEEN"` at 7/67, avatar thumbnail is present, color is purple. This is the one piece nothing above can verify without a live connection.
