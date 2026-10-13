# Sticker follow-ups Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** After a line tagged with a mood, SYNCS sometimes posts the matching sticker strip of herself, through one rarity gate; every eligible line of her chatter is tagged by what it says.

**Architecture:** A pure `Helpers/Core/Sticker` defines one marker constant per mood (ordinary and `Sure`) and splits markers off a line. `BotChat` (static, the single chatter send path) splits every line, sends the clean text, and calls a static `FollowUp` hook wired in `Program.cs` to the `StickerService` singleton, which owns the gate (1 in 3, 30 min per channel, 12 a day) and posts the PNG. Content work then appends a tag to every eligible line in `BotResponses`.

**Tech Stack:** .NET 10, Discord.Net 3.x (`SendFileAsync`, `AutocompleteHandler`), C# constants for tags. No test project: checks run from a scratch console harness that references the bot project.

**Spec:** `docs/superpowers/specs/2026-10-13-sticker-followups-design.md` — read it before starting any task.

## Global Constraints

- Read `CLAUDE.md` and `docs/agents/personality.md` before touching code; their rules bind.
- **Never commit.** The owner commits by hand. Every task ends with the work left uncommitted.
- Build check for every code task: `dotnet build -warnaserror` in `ProjectSYNCS/` must succeed with 0 warnings.
- Rate constants exactly: `Chance = 1.0 / 3`, `Cooldown = 30 min`, `DailyCap = 12` (Paris day via `AppTime.TodayKey`).
- 22 moods, exactly: `bargain`, `bored`, `devoted`, `disgust`, `flustered`, `full_love`, `happy`, `hurt`, `jenote`, `laugh`, `love`, `mama`, `menace`, `outraged`, `overheat`, `reboot`, `shocked`, `sleepy`, `smug`, `sulk`, `terror`, `tipsy_love`. **Never `glitch`.**
- Marker format: ` ⟪mood⟫` (ordinary) and ` ⟪mood!⟫` (sure), **with the leading space inside the constant**. U+27EA / U+27EB.
- A raw `⟪` may appear only in `ProjectSYNCS/Helpers/Core/Sticker.cs`. Pools use the constants.
- The strip is sent with `AllowedMentions.None`, as a file with no text, not as a reply.
- Code, comments and logs in English; every user-facing string in French; the slash option name is `mood` (English).
- Tagging changes no line's text: it only appends `+ Sticker.X` / `+ Sticker.Sure.X`.
- Version: `ProjectSYNCS/config.yaml` goes **6.12.1 → 6.13.0** exactly once (Task 12), with a `CHANGELOG.md` entry; no easter-egg triggers or member names in the changelog.
- New files go in subsystem folders, flat namespaces: `ProjectSYNCS.Helpers`, `ProjectSYNCS.Services`, `ProjectSYNCS.Interactions.Autocomplete`.

## Review Focus

1. **User text formatted into a tagged line** (a nickname, session title or prize containing `⟪sulk!⟫` or `⟪pwned⟫`): the sent text never shows a marker; an unknown marker is ignored; a known one can at worst trigger one strip inside the gate's limits. Pinned by Task 1's split checks 7–8.
2. **A message with text after the marker** (the session recap appends the roll call after its line): the marker leaves cleanly and the roll call is intact. Pinned by Task 1's split check 5.
3. **A glitched line**: the corrupted text and the restored text never contain a marker, and no strip follows. Pinned by Task 3's grep check and code read.
4. **A channel without Attach Files**: the line still goes out, the failure is logged once per cooldown window, nothing throws into the gateway. Pinned by Task 13's live check (owner).
5. **The cap and the cooldown under sure tags**: a sure tag skips only the roll; a second strip in the same channel within 30 min and the 13th strip of the day are refused. Pinned by Task 2's gate checks.

---

## File Structure

| File | Status | Responsibility |
|---|---|---|
| `ProjectSYNCS/Helpers/Core/Sticker.cs` | Create | Marker constants, `Moods`, `Split`, `Strip`, `FileFor`, `StickerTag` |
| `ProjectSYNCS/Services/Personality/StickerService.cs` | Create | The gate (`TryAdmit`), the follow-up (`TryFollowUp`), the send (`PostAsync`) |
| `ProjectSYNCS/Helpers/Core/BotChat.cs` | Modify | Split each line, send clean text, `FollowUp` hook, embed tag parameter |
| `ProjectSYNCS/Program.cs` | Modify | Register `StickerService`, wire `BotChat.FollowUp` |
| `ProjectSYNCS/Services/Personality/GlitchService.cs` | Modify | Corrupt and restore the clean text |
| `ProjectSYNCS/Services/Personality/PresenceService.cs` | Modify | `Sticker.Strip` backstop on the custom status |
| `ProjectSYNCS/Services/Xp/XpTracker.cs` | Modify | Split the level-up line, pass the tag to the embed path |
| `ProjectSYNCS/Interactions/Autocomplete/StickerMoodAutocompleteHandler.cs` | Create | Mood suggestions for `/debug sticker` |
| `ProjectSYNCS/Commands/Admin/DebugModule.cs` | Modify | `/debug sticker` |
| `ProjectSYNCS/Services/Personality/BotResponses.cs` | Modify | Tags on every eligible line (Tasks 6–11) |
| `docs/syncs-appearance.md` | Modify | *Moods wanted* section (Task 5, filled by 6–11); assets row (Task 12) |
| `docs/agents/personality.md`, `CLAUDE.md` | Modify | Built rules (Task 12) |
| `ProjectSYNCS/config.yaml`, `ProjectSYNCS/CHANGELOG.md` | Modify | 6.13.0 (Task 12) |
| `<scratch>/StickerHarness/` | Create (outside the repo) | Owner-style scratch checks: `split`, `gate`, `audit` |

`<scratch>` is any directory **outside the repository** (the session's scratchpad if it has one, else
a temp directory); `<repo>` is the absolute path of the repository root. Substitute both, as absolute
paths, wherever they appear below. The harness never goes into the repo. It needs the .NET 10 SDK
(`dotnet --version` ≥ 10.0); install it first if the session lacks it.

---

### Task 1: The tag — `Helpers/Core/Sticker`

**Files:**
- Create: `ProjectSYNCS/Helpers/Core/Sticker.cs`
- Create (scratch): `<scratch>/StickerHarness/StickerHarness.csproj`, `H.cs`, `SplitCheck.cs`, `Program.cs`

**Interfaces:**
- Consumes: nothing.
- Produces:
  - `public readonly record struct StickerTag(string Mood, bool Sure)` (namespace `ProjectSYNCS.Helpers`)
  - `public static class Sticker` with 22 `public const string` (`Bargain` … `TipsyLove`) and nested `public static class Sure` with the same 22
  - `public static IReadOnlyList<string> Sticker.Moods`
  - `public static (string Text, StickerTag? Tag) Sticker.Split(string line)`
  - `public static string Sticker.Strip(string text)`
  - `public static string Sticker.FileFor(string mood)` → `<AppContext.BaseDirectory>/Assets/Stickers/syncs_<mood>.png`

- [ ] **Step 1: Create the scratch harness (the failing check)**

`<scratch>/StickerHarness/StickerHarness.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="<repo>/ProjectSYNCS/ProjectSYNCS.csproj" />
  </ItemGroup>
</Project>
```

`<scratch>/StickerHarness/H.cs`:

```csharp
static class H
{
    public static int Fails;

    public static void Check(bool ok, string what)
    {
        Console.WriteLine($"{(ok ? "PASS" : "FAIL")}  {what}");
        if (!ok) Fails++;
    }
}
```

`<scratch>/StickerHarness/SplitCheck.cs`:

```csharp
using System.Reflection;
using ProjectSYNCS.Helpers;

static class SplitCheck
{
    const string RepoStickers = "<repo>/ProjectSYNCS/Assets/Stickers";

    public static void Run()
    {
        // 1. ordinary tag at the end
        var (t1, g1) = Sticker.Split("Occupée à exister." + Sticker.Bored);
        H.Check(t1 == "Occupée à exister." && g1 == new StickerTag("bored", false), "1 ordinary tag splits off");

        // 2. sure tag
        var (t2, g2) = Sticker.Split("Record battu." + Sticker.Sure.Overheat);
        H.Check(t2 == "Record battu." && g2 == new StickerTag("overheat", true), "2 sure tag splits off");

        // 3. untagged line is untouched
        var (t3, g3) = Sticker.Split("Salut");
        H.Check(t3 == "Salut" && g3 is null, "3 untagged line unchanged");

        // 4. string.Format passes the marker through
        var (t4, g4) = Sticker.Split(string.Format("Bravo {0} !" + Sticker.Happy, "Léa"));
        H.Check(t4 == "Bravo Léa !" && g4 == new StickerTag("happy", false), "4 formatted line keeps its tag");

        // 5. text after the marker (the session recap appends the roll call)
        var recap = "Tout le monde est venu." + Sticker.Sure.Shocked + "\n- <@1>\n- <@2>";
        var (t5, g5) = Sticker.Split(recap);
        H.Check(t5 == "Tout le monde est venu.\n- <@1>\n- <@2>" && g5 == new StickerTag("shocked", true),
            "5 marker mid-text leaves cleanly");

        // 6. emote markup before the tag is preserved
        var (t6, _) = Sticker.Split("Je note <:syncs_smug:123>" + Sticker.Smug);
        H.Check(t6 == "Je note <:syncs_smug:123>", "6 emote markup preserved");

        // 7. unknown marker: removed and ignored
        var (t7, g7) = Sticker.Split("Salut ⟪pwned⟫ toi");
        H.Check(t7 == "Salut toi" && g7 is null, "7 unknown marker stripped, no tag");

        // 8. a marker smuggled in through a name never shows
        var (t8, g8) = Sticker.Split(string.Format("Bravo {0} !" + Sticker.Happy, "Léa ⟪sulk!⟫"));
        H.Check(t8 == "Bravo Léa !" && g8 is not null, "8 injected marker never shows");

        // 9. Strip
        H.Check(Sticker.Strip("Coucou" + Sticker.Love) == "Coucou", "9 Strip removes the marker");

        // 10. every constant round-trips, 22 of each, every mood has a strip file, no glitch
        foreach (var (type, sure) in new[] { (typeof(Sticker), false), (typeof(Sticker.Sure), true) })
        {
            var consts = type.GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.IsLiteral).ToList();
            H.Check(consts.Count == 22, $"10 {type.Name}: 22 constants (found {consts.Count})");
            foreach (var f in consts)
            {
                var value = (string)f.GetRawConstantValue()!;
                var (text, tag) = Sticker.Split("a" + value);
                H.Check(value.StartsWith(' ') && text == "a" && tag is { } t && t.Sure == sure
                        && Sticker.Moods.Contains(t.Mood),
                    $"10 {type.Name}.{f.Name} round-trips");
            }
        }
        H.Check(Sticker.Moods.Count == 22 && !Sticker.Moods.Contains("glitch"), "10 22 moods, no glitch");
        foreach (var mood in Sticker.Moods)
            H.Check(File.Exists(Path.Combine(RepoStickers, $"syncs_{mood}.png")), $"10 strip file for {mood}");
    }
}
```

`<scratch>/StickerHarness/Program.cs`:

```csharp
var mode = args.Length > 0 ? args[0] : "split";
switch (mode)
{
    case "split": SplitCheck.Run(); break;
    default: Console.WriteLine($"unknown mode {mode}"); return 2;
}
Console.WriteLine(H.Fails == 0 ? "ALL PASS" : $"{H.Fails} FAIL(S)");
return H.Fails == 0 ? 0 : 1;
```

- [ ] **Step 2: Run it to verify it fails**

Run: `dotnet run --project "<scratch>/StickerHarness" -- split`
Expected: build error `CS0246`/`CS0103` — `Sticker` / `StickerTag` do not exist.

- [ ] **Step 3: Write `Sticker.cs`**

`ProjectSYNCS/Helpers/Core/Sticker.cs`:

```csharp
using System.Text.RegularExpressions;

namespace ProjectSYNCS.Helpers;

/// <summary>A sticker tag split off a line: which strip, and whether it skips the roll.</summary>
public readonly record struct StickerTag(string Mood, bool Sure);

// Her sticker strips (Assets/Stickers/syncs_<mood>.png) as tags written into a line. A line opts
// into a picture by appending a constant: "Occupée à exister." + Sticker.Bored. The tag names the
// face the *line* makes, not its pool's. Sticker.X rolls the 1-in-3 chance; Sticker.Sure.X is for
// the moment pools and skips it. BotChat splits the tag off before sending and hands it to
// StickerService (docs/agents/personality.md, Sticker follow-ups).
//
// This file is the only place a marker is spelled out: never paste ⟪…⟫ into a pool. Each constant
// carries its leading space, and has no braces, so string.Format passes it through. There is
// deliberately no Glitch constant: the glitch picture belongs to the easter egg alone.
public static class Sticker
{
    public const string Bargain = " ⟪bargain⟫";
    public const string Bored = " ⟪bored⟫";
    public const string Devoted = " ⟪devoted⟫";
    public const string Disgust = " ⟪disgust⟫";
    public const string Flustered = " ⟪flustered⟫";
    public const string FullLove = " ⟪full_love⟫";
    public const string Happy = " ⟪happy⟫";
    public const string Hurt = " ⟪hurt⟫";
    public const string Jenote = " ⟪jenote⟫";
    public const string Laugh = " ⟪laugh⟫";
    public const string Love = " ⟪love⟫";
    public const string Mama = " ⟪mama⟫";
    public const string Menace = " ⟪menace⟫";
    public const string Outraged = " ⟪outraged⟫";
    public const string Overheat = " ⟪overheat⟫";
    public const string Reboot = " ⟪reboot⟫";
    public const string Shocked = " ⟪shocked⟫";
    public const string Sleepy = " ⟪sleepy⟫";
    public const string Smug = " ⟪smug⟫";
    public const string Sulk = " ⟪sulk⟫";
    public const string Terror = " ⟪terror⟫";
    public const string TipsyLove = " ⟪tipsy_love⟫";

    // The moment pools' tags: the moment earns the picture, so the roll is skipped. The cooldown
    // and the daily cap still apply.
    public static class Sure
    {
        public const string Bargain = " ⟪bargain!⟫";
        public const string Bored = " ⟪bored!⟫";
        public const string Devoted = " ⟪devoted!⟫";
        public const string Disgust = " ⟪disgust!⟫";
        public const string Flustered = " ⟪flustered!⟫";
        public const string FullLove = " ⟪full_love!⟫";
        public const string Happy = " ⟪happy!⟫";
        public const string Hurt = " ⟪hurt!⟫";
        public const string Jenote = " ⟪jenote!⟫";
        public const string Laugh = " ⟪laugh!⟫";
        public const string Love = " ⟪love!⟫";
        public const string Mama = " ⟪mama!⟫";
        public const string Menace = " ⟪menace!⟫";
        public const string Outraged = " ⟪outraged!⟫";
        public const string Overheat = " ⟪overheat!⟫";
        public const string Reboot = " ⟪reboot!⟫";
        public const string Shocked = " ⟪shocked!⟫";
        public const string Sleepy = " ⟪sleepy!⟫";
        public const string Smug = " ⟪smug!⟫";
        public const string Sulk = " ⟪sulk!⟫";
        public const string Terror = " ⟪terror!⟫";
        public const string TipsyLove = " ⟪tipsy_love!⟫";
    }

    /// <summary>Every mood with a strip, as its file stem. /debug sticker offers these.</summary>
    public static readonly IReadOnlyList<string> Moods = new[]
    {
        "bargain", "bored", "devoted", "disgust", "flustered", "full_love", "happy", "hurt",
        "jenote", "laugh", "love", "mama", "menace", "outraged", "overheat", "reboot", "shocked",
        "sleepy", "smug", "sulk", "terror", "tipsy_love",
    };

    private static readonly HashSet<string> Known = new(Moods);

    // Any marker, known or not, with the one space before it.
    private static readonly Regex Marker = new(@" ?⟪([a-z_]+)(!?)⟫", RegexOptions.Compiled);

    /// <summary>
    /// The line without any marker, and the first known marker as the tag (null: untagged).
    /// Markers are removed wherever they are — a composed message (the session recap) carries
    /// text after its line's tag — and an unknown one is removed and ignored, never leaked.
    /// </summary>
    public static (string Text, StickerTag? Tag) Split(string line)
    {
        if (!line.Contains('⟪')) return (line, null);

        StickerTag? tag = null;
        var text = Marker.Replace(line, m =>
        {
            if (tag is null && Known.Contains(m.Groups[1].Value))
                tag = new StickerTag(m.Groups[1].Value, m.Groups[2].Value == "!");
            return string.Empty;
        });
        return (text, tag);
    }

    /// <summary>The text without any marker, for paths that only need it clean.</summary>
    public static string Strip(string text) => Split(text).Text;

    /// <summary>Where a mood's strip ships (the csproj copies Assets\**\*.png).</summary>
    public static string FileFor(string mood) =>
        Path.Combine(AppContext.BaseDirectory, "Assets", "Stickers", $"syncs_{mood}.png");
}
```

- [ ] **Step 4: Run the checks to verify they pass**

Run: `dotnet run --project "<scratch>/StickerHarness" -- split`
Expected: every line `PASS`, last line `ALL PASS`.

- [ ] **Step 5: Build the bot**

Run (in `ProjectSYNCS/`): `dotnet build -warnaserror`
Expected: `Build succeeded`, 0 warnings.

- [ ] **Step 6: Leave uncommitted** — the owner commits by hand.

---

### Task 2: The gate and the send — `StickerService`, wired into `BotChat`

**Files:**
- Create: `ProjectSYNCS/Services/Personality/StickerService.cs`
- Modify: `ProjectSYNCS/Helpers/Core/BotChat.cs` (header comment; `ReplyWithTypingAsync`, `PostWithTypingAsync`, `PostEmbedWithTypingAsync`)
- Modify: `ProjectSYNCS/Program.cs` (registration after `GlitchService`; wiring after `.Build()`)
- Create (scratch): `<scratch>/StickerHarness/GateCheck.cs`; replace `Program.cs`

**Interfaces:**
- Consumes: `StickerTag`, `Sticker.FileFor` (Task 1); `BreakdownService.IsActive(ulong)`; `CooldownGate<ulong>`; `AppTime.TodayKey`.
- Produces:
  - `public sealed class StickerService` (namespace `ProjectSYNCS.Services`), ctor `(BreakdownService breakdown, ILogger<StickerService> logger)`
  - `public bool TryAdmit(ulong channelId, StickerTag tag)`
  - `public void TryFollowUp(IMessageChannel channel, StickerTag tag)`
  - `public Task<bool> PostAsync(IMessageChannel channel, string mood)`
  - `public static Action<IMessageChannel, StickerTag>? BotChat.FollowUp { get; set; }`
  - `BotChat.PostEmbedWithTypingAsync(IMessageChannel channel, Embed embed, string delayText, ILogger logger, string what, StickerTag? sticker = null)`

- [ ] **Step 1: Write the failing gate check**

`<scratch>/StickerHarness/GateCheck.cs`:

```csharp
using Microsoft.Extensions.Logging.Abstractions;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;

static class GateCheck
{
    static StickerService Fresh() => new(
        new BreakdownService(NullLogger<BreakdownService>.Instance),
        NullLogger<StickerService>.Instance);

    public static void Run()
    {
        var svc = Fresh();
        var sure = new StickerTag("happy", true);
        H.Check(svc.TryAdmit(1, sure), "first sure tag in a channel is admitted");
        H.Check(!svc.TryAdmit(1, sure), "second strip in the same channel within 30 min is refused");
        var all = true;
        for (ulong c = 2; c <= 12; c++) all &= svc.TryAdmit(c, sure);
        H.Check(all, "channels 2..12 admitted (12 strips today)");
        H.Check(!svc.TryAdmit(13, sure), "13th strip of the day is refused, sure or not");

        // Ordinary tags roll 1 in 3: over 200 distinct channels the cap still binds at 12, and
        // the first 12 attempts can't all have won ((1/3)^12).
        var rolled = Fresh();
        var ordinary = new StickerTag("bored", false);
        int admitted = 0, firstTwelve = 0;
        for (ulong c = 100; c < 300; c++)
        {
            if (!rolled.TryAdmit(c, ordinary)) continue;
            admitted++;
            if (c < 112) firstTwelve++;
        }
        H.Check(admitted == StickerService.DailyCap, $"ordinary tags: cap binds at 12 (admitted {admitted})");
        H.Check(firstTwelve < 12, "ordinary tags: the roll refuses some");
    }
}
```

Replace `<scratch>/StickerHarness/Program.cs`:

```csharp
var mode = args.Length > 0 ? args[0] : "split";
switch (mode)
{
    case "split": SplitCheck.Run(); break;
    case "gate": GateCheck.Run(); break;
    default: Console.WriteLine($"unknown mode {mode}"); return 2;
}
Console.WriteLine(H.Fails == 0 ? "ALL PASS" : $"{H.Fails} FAIL(S)");
return H.Fails == 0 ? 0 : 1;
```

- [ ] **Step 2: Run it to verify it fails**

Run: `dotnet run --project "<scratch>/StickerHarness" -- gate`
Expected: build error — `StickerService` does not exist.

- [ ] **Step 3: Write `StickerService.cs`**

`ProjectSYNCS/Services/Personality/StickerService.cs`:

```csharp
using Discord;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// Posts one of her sticker strips (Assets/Stickers) after a line tagged with Helpers/Core/Sticker.
// BotChat reaches TryFollowUp through its static FollowUp hook, set in Program.cs; /debug sticker
// calls PostAsync directly. Rare by construction: an ordinary tag wins 1 in 3, a sure tag (the
// moment pools) skips the roll, and both respect a cooldown per channel and a daily cap — a strip
// is big, and posted often it becomes noise.
//
// A singleton: the gates are in-memory state and reset on restart, by design. Its own gates,
// shared with nobody.
public sealed class StickerService
{
    public const double Chance = 1.0 / 3;
    public static readonly TimeSpan Cooldown = TimeSpan.FromMinutes(30);
    public const int DailyCap = 12;

    // The beat between the line and its picture, so the strip reads as her reaction to it.
    private static readonly TimeSpan Beat = TimeSpan.FromSeconds(1);

    private readonly BreakdownService _breakdown;
    private readonly ILogger<StickerService> _logger;

    // Keyed by channel id, DMs included. A day is ample before an idle channel is forgotten.
    private readonly CooldownGate<ulong> _channels = new(Cooldown, TimeSpan.FromDays(1));

    private readonly object _lock = new();
    private int _day;
    private int _postedToday;

    public StickerService(BreakdownService breakdown, ILogger<StickerService> logger)
    {
        _breakdown = breakdown;
        _logger = logger;
    }

    /// <summary>
    /// The gate alone. The cheapest and non-consuming checks first: a lost roll, a breakdown in
    /// progress or a full day uses up nothing; only an admitted strip claims the channel and
    /// counts toward the day. Public so it can be checked without a gateway.
    /// </summary>
    public bool TryAdmit(ulong channelId, StickerTag tag)
    {
        if (!tag.Sure && Random.Shared.NextDouble() >= Chance) return false;
        if (_breakdown.IsActive(channelId)) return false;

        lock (_lock)
        {
            var today = AppTime.TodayKey;
            if (today != _day)
            {
                _day = today;
                _postedToday = 0;
            }
            if (_postedToday >= DailyCap) return false;
            if (!_channels.TryClaim(channelId)) return false;
            _postedToday++;
            return true;
        }
    }

    /// <summary>
    /// After a tagged line went out: maybe post its strip. Off the gateway handler, which
    /// BotChat's typing pause has already brought close to Discord.Net's 3 s timeout.
    /// </summary>
    public void TryFollowUp(IMessageChannel channel, StickerTag tag)
    {
        if (!TryAdmit(channel.Id, tag)) return;
        _ = Task.Run(async () =>
        {
            await Task.Delay(Beat);
            await PostAsync(channel, tag.Mood);
        });
    }

    /// <summary>
    /// Posts a strip as a file with no text, never a ping. Swallows and logs: a channel without
    /// Attach Files just gets the line. The claim is kept on failure — a missing permission is
    /// persistent, and releasing it would retry and log on every line.
    /// </summary>
    public async Task<bool> PostAsync(IMessageChannel channel, string mood)
    {
        try
        {
            await channel.SendFileAsync(Sticker.FileFor(mood), text: null, allowedMentions: AllowedMentions.None);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to post the {Mood} sticker in channel {ChannelId}.", mood, channel.Id);
            return false;
        }
    }
}
```

- [ ] **Step 4: Run the gate checks**

Run: `dotnet run --project "<scratch>/StickerHarness" -- gate`
Expected: every line `PASS`, `ALL PASS`. Re-run `-- split`: still `ALL PASS`.

- [ ] **Step 5: Split lines in `BotChat` and add the hook**

In `ProjectSYNCS/Helpers/Core/BotChat.cs`, extend the header comment (after the paragraph ending "its only caller)."):

```csharp
//
// Sticker tags (Helpers/Core/Sticker) are split off here: the clean text is what goes out, and a
// tagged line that went out hands its tag to FollowUp. FollowUp is the one static seam in this
// class, set once in Program.cs to StickerService.TryFollowUp: BotChat is static with dozens of
// call sites, and the sticker gate is in-memory state that belongs in a singleton. Left null,
// lines still go out clean, with no strip.
```

Add, at the top of the class body:

```csharp
    /// <summary>Called after a tagged line went out. Set once in Program.cs.</summary>
    public static Action<IMessageChannel, StickerTag>? FollowUp { get; set; }
```

Replace `ReplyWithTypingAsync`'s body with:

```csharp
    {
        var (text, sticker) = Sticker.Split(line);
        IUserMessage sent;
        try
        {
            using (replyTo.Channel.EnterTypingState())
            {
                await Task.Delay(TypingDelayFor(text));
            }
            sent = await replyTo.ReplyAsync(text, allowedMentions: allowedMentions);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send {What} in channel {ChannelId}.", what, replyTo.Channel.Id);
            return null;
        }
        if (sticker is { } tag) FollowUp?.Invoke(replyTo.Channel, tag);
        return sent;
    }
```

Replace `PostWithTypingAsync`'s body with:

```csharp
    {
        var (text, sticker) = Sticker.Split(line);
        IUserMessage sent;
        try
        {
            using (channel.EnterTypingState())
            {
                await Task.Delay(TypingDelayFor(text));
            }
            sent = await channel.SendMessageAsync(text, allowedMentions: allowedMentions, messageReference: replyTo);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send {What} in channel {ChannelId}.", what, channel.Id);
            return null;
        }
        if (sticker is { } tag) FollowUp?.Invoke(channel, tag);
        return sent;
    }
```

Replace `PostEmbedWithTypingAsync` (signature, doc and body) with:

```csharp
    /// <summary>
    /// Posts an embed behind the same typing pause. <paramref name="delayText"/> is
    /// only used to size that pause — the embed carries the actual content, and
    /// nothing here is sent as message text. The caller splits its own line (the tag
    /// would otherwise show inside the embed) and passes the tag as <paramref name="sticker"/>.
    /// </summary>
    public static async Task PostEmbedWithTypingAsync(
        IMessageChannel channel, Embed embed, string delayText, ILogger logger, string what,
        StickerTag? sticker = null)
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
            return;
        }
        if (sticker is { } tag) FollowUp?.Invoke(channel, tag);
    }
```

- [ ] **Step 6: Register and wire in `Program.cs`**

Add `using ProjectSYNCS.Helpers;` to the usings. After `services.AddSingleton<GlitchService>();` add:

```csharp
        // The sticker follow-ups' cooldown and daily cap are in-memory: a singleton.
        services.AddSingleton<StickerService>();
```

After `.Build();` and before the `using (var scope = host.Services.CreateScope())` migration block, add:

```csharp
    // BotChat is static; the one thing it needs from DI is the sticker gate.
    BotChat.FollowUp = host.Services.GetRequiredService<StickerService>().TryFollowUp;
```

- [ ] **Step 7: Build**

Run (in `ProjectSYNCS/`): `dotnet build -warnaserror`
Expected: `Build succeeded`, 0 warnings. Then `dotnet run --project "<scratch>/StickerHarness" -- gate` → `ALL PASS`.

- [ ] **Step 8: Leave uncommitted.**

---

### Task 3: The other paths — glitch, presence, level-up embed

**Files:**
- Modify: `ProjectSYNCS/Services/Personality/GlitchService.cs` (`ReplyWithTypingAsync`, `PostWithTypingAsync`, header comment)
- Modify: `ProjectSYNCS/Services/Personality/PresenceService.cs:92`
- Modify: `ProjectSYNCS/Services/Xp/XpTracker.cs:~310-322` (level-up announcement)

**Interfaces:**
- Consumes: `Sticker.Strip`, `Sticker.Split`, `StickerTag`, `BotChat.PostEmbedWithTypingAsync(..., StickerTag? sticker)` (Tasks 1–2).
- Produces: no new API.

- [ ] **Step 1: `GlitchService` works on the clean text**

Replace both public methods with:

```csharp
    /// <summary><see cref="BotChat.ReplyWithTypingAsync"/>, with the rare glitch.</summary>
    public async Task<IUserMessage?> ReplyWithTypingAsync(
        IUserMessage replyTo, string line, ILogger logger, string what)
    {
        var clean = Sticker.Strip(line);
        var glitched = TryGlitch(clean, replyTo.Channel.Id);
        var sent = await BotChat.ReplyWithTypingAsync(replyTo, glitched ?? line, logger, what);
        if (sent is not null && glitched is not null) Restore(sent, clean, what);
        return sent;
    }

    /// <summary><see cref="BotChat.PostWithTypingAsync"/>, with the rare glitch.</summary>
    public async Task<IUserMessage?> PostWithTypingAsync(
        IMessageChannel channel, string line, ILogger logger, string what)
    {
        var clean = Sticker.Strip(line);
        var glitched = TryGlitch(clean, channel.Id);
        var sent = await BotChat.PostWithTypingAsync(channel, glitched ?? line, logger, what);
        if (sent is not null && glitched is not null) Restore(sent, clean, what);
        return sent;
    }
```

Add to the header comment, after "…the verdict replies are tracked elsewhere.":

```csharp
// A sticker tag (Helpers/Core/Sticker) never survives a glitch: the corruption and the restore
// both work on the clean text, so a glitched line reaches BotChat untagged and no strip follows
// it — the silent fix relies on nothing else being posted around it. An unglitched line goes
// through with its tag, and BotChat does the follow-up as for any line.
```

- [ ] **Step 2: `PresenceService` backstop**

At `PresenceService.cs:92` replace `await _client.SetCustomStatusAsync(line);` with:

```csharp
            // Presence pools are never tagged; Strip is the backstop, since a custom status
            // shows a stray sticker marker verbatim to everyone.
            await _client.SetCustomStatusAsync(Sticker.Strip(line));
```

- [ ] **Step 3: `XpTracker` splits the level-up line**

Replace the `description` computation and the final send in the level-up announcement with:

```csharp
        // 7 and 67 are a fixed line, not a pool pick — ResponsePicker is skipped
        // entirely, so the egg never records a line that isn't actually a pool entry
        // in the picker's memory. The pool's sticker tag is split off here, since the
        // line goes inside the embed rather than through BotChat's text path.
        var (description, sticker) = IsSixSeven(newLevel)
            ? ("SIX SEVEEEN", (StickerTag?)null)
            : Sticker.Split(string.Format(_picker.Pick(BotResponses.XpLevelUpLines), name, newLevel));
```

(keep the `EmbedBuilder` block unchanged) and:

```csharp
        await BotChat.PostEmbedWithTypingAsync(channel, embed, description, _logger, "level-up announcement", sticker);
```

- [ ] **Step 4: Build and check**

Run (in `ProjectSYNCS/`): `dotnet build -warnaserror` → `Build succeeded`, 0 warnings.
Run: `grep -n "Strip\|clean" ProjectSYNCS/Services/Personality/GlitchService.cs`
Expected: `TryGlitch` and `Restore` receive `clean` in both methods; `BotChat` receives `glitched ?? line`.
Run: `dotnet run --project "<scratch>/StickerHarness" -- split` and `-- gate` → `ALL PASS` each.

- [ ] **Step 5: Leave uncommitted.**

---

### Task 4: `/debug sticker`

**Files:**
- Create: `ProjectSYNCS/Interactions/Autocomplete/StickerMoodAutocompleteHandler.cs`
- Modify: `ProjectSYNCS/Commands/Admin/DebugModule.cs` (field, constructor, new command after `HealthAsync`)

**Interfaces:**
- Consumes: `Sticker.Moods` (Task 1), `StickerService.PostAsync` (Task 2), `AvailabilityService.OwnerId`.
- Produces: `/debug sticker mood:<string>`.

- [ ] **Step 1: The autocomplete handler**

`ProjectSYNCS/Interactions/Autocomplete/StickerMoodAutocompleteHandler.cs`:

```csharp
using Discord;
using Discord.Interactions;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Interactions.Autocomplete;

/// <summary>
/// Suggests the sticker moods for <c>/debug sticker</c>, matched on the stem. Owner-only like
/// the command. 22 moods, under Discord's 25 suggestions.
/// </summary>
public sealed class StickerMoodAutocompleteHandler : AutocompleteHandler
{
    private const int MaxSuggestions = 25;

    public override Task<AutocompletionResult> GenerateSuggestionsAsync(
        IInteractionContext context,
        IAutocompleteInteraction interaction,
        IParameterInfo parameter,
        IServiceProvider services)
    {
        if (context.User.Id != AvailabilityService.OwnerId)
            return Task.FromResult(AutocompletionResult.FromSuccess());

        var typed = interaction.Data.Current.Value as string ?? string.Empty;
        var matches = Sticker.Moods
            .Where(m => typed.Length == 0 || m.Contains(typed, StringComparison.OrdinalIgnoreCase))
            .Take(MaxSuggestions)
            .Select(m => new AutocompleteResult(m, m));

        return Task.FromResult(AutocompletionResult.FromSuccess(matches));
    }
}
```

- [ ] **Step 2: Inject `StickerService` into `DebugModule`**

Add the field `private readonly StickerService _stickers;`, add `StickerService stickers` as the last constructor parameter, and `_stickers = stickers;` in the body. Add `using ProjectSYNCS.Interactions.Autocomplete;` if the file doesn't already have it (it uses `QuizKeyAutocompleteHandler`, so check first).

- [ ] **Step 3: The command, after `HealthAsync`**

```csharp
    // Any of her sticker strips, posted here as the follow-up would post it, bypassing the
    // roll, the cooldown and the daily cap: to see each strip on PC and on the phone.
    [SlashCommand("sticker", "Poster ici une image de SYNCS, au choix de l'humeur (tests)")]
    public async Task StickerAsync(
        [Summary("mood", "L'humeur de l'image")]
        [Autocomplete(typeof(StickerMoodAutocompleteHandler))] string mood)
    {
        if (Context.User.Id != AvailabilityService.OwnerId)
        {
            await RespondAsync("Seul Rodhengard peut utiliser cette commande.", ephemeral: true);
            return;
        }
        if (!Sticker.Moods.Contains(mood))
        {
            await RespondAsync($"Humeur inconnue : {Format.Sanitize(mood)}.", ephemeral: true);
            return;
        }

        // The upload can outrun Discord's 3 s.
        await DeferAsync(ephemeral: true);
        var posted = await _stickers.PostAsync(Context.Channel, mood);
        await FollowupAsync(
            posted ? "Envoyé." : "Échec de l'envoi (droit d'envoyer des fichiers ? voir les logs).",
            ephemeral: true);
    }
```

- [ ] **Step 4: Count and build**

Run: `grep -c "\[SlashCommand(" ProjectSYNCS/Commands/Admin/DebugModule.cs` → `12` (under the 25 cap).
Run (in `ProjectSYNCS/`): `dotnet build -warnaserror` → `Build succeeded`, 0 warnings.

- [ ] **Step 5: Leave uncommitted.**

---

### Task 5: Tag audit and the *Moods wanted* section

**Files:**
- Create (scratch): `<scratch>/StickerHarness/Audit.cs`; replace `Program.cs`
- Modify: `docs/syncs-appearance.md` (new section before `## Ready-to-paste prompts (emote pack and seasonal avatars, v1)`)

**Interfaces:**
- Consumes: `Sticker.Split` (Task 1); `ProjectSYNCS.Services.BotResponses` (internal; read by reflection).
- Produces: `dotnet run --project "<scratch>/StickerHarness" -- audit [Pool …]` — used by Tasks 6–11 and 13.

- [ ] **Step 1: Write the audit**

`<scratch>/StickerHarness/Audit.cs`:

```csharp
using System.Reflection;
using ProjectSYNCS.Helpers;

static class Audit
{
    // Spec §4: pools that go out through BotChat, GlitchService or the level-up embed.
    static readonly string[] Eligible =
    {
        "Comebacks", "NiceReplies", "Greetings", "Interrogations", "RescueRoasts",
        "MistakenIdentityReplies", "ReferenceComebacks",
        "BadBotReplies", "BadBotRepliesOwner", "BadGirlReplies", "BadGirlRepliesOwner",
        "TurnaboutBoyLines", "TurnaboutGirlLines", "TurnaboutNeutralLines",
        "OwnerGreetings", "OwnerComebacks", "OwnerMeanReplies", "OwnerAbsentNotices",
        "TataGreetings", "TataReplies",
        "JealousLines", "JealousLinesOwner", "RivalMutters", "RivalLevelUpLines", "RivalJoinLines",
        "ShutdownThreatOwner", "ShutdownThreatTata", "ShutdownThreatReplies",
        "XpLevelUpLines", "GiveawayDrawLines", "GiveawayEmptyLines",
        "QuizWinLines", "QuizOwnerWinLines", "QuizTimeoutLines",
        "QuizCrownLines", "QuizCrownSharedLines", "QuizCrownOwnerLines",
        "SessionLateCallLines", "SessionLateCallSeatedLines", "SessionRecapLines", "SessionRecapFullLines",
        "WeekendPollResultLines", "WeekendPollDeadLines", "WelcomeLines", "WelcomeRushLines",
        "NightScoldLines", "IdleLines", "WakeLines", "WakeUpdateLines", "PowerCutWakeLines",
        "CrashWakeLines", "RouterReturnLines", "RouterReturnQuietLines", "HeatRecordLines",
        "VoiceSpectatorGeneralLines", "VoiceSpectatorGamingLines", "VoiceSpectatorCinemaLines",
        "VoiceSpectatorStudyLines", "VoiceSpectatorSeatedLines", "VoiceKickPoutLines",
        "VoiceKickAngryLines", "VoiceRivalRefuseLines", "VoiceRivalSeatedLines", "OwnerBedtimeLines",
    };

    // Every tag in these is sure; no sure tag anywhere else.
    static readonly HashSet<string> Moments = new()
    {
        "HeatRecordLines", "CrashWakeLines", "PowerCutWakeLines", "RouterReturnLines",
        "WeekendPollDeadLines", "SessionRecapFullLines", "QuizCrownLines", "QuizCrownSharedLines",
        "QuizCrownOwnerLines", "GiveawayEmptyLines",
    };

    public static void Run(string[] only)
    {
        var type = typeof(Sticker).Assembly.GetType("ProjectSYNCS.Services.BotResponses")!;
        var pools = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(string[]))
            .ToDictionary(f => f.Name, f => (string[])f.GetValue(null)!);

        foreach (var name in Eligible)
            H.Check(pools.ContainsKey(name), $"pool {name} exists");

        int total = 0, totalTagged = 0;
        foreach (var (name, lines) in pools.OrderBy(p => p.Key))
        {
            var eligible = Eligible.Contains(name);
            var moment = Moments.Contains(name);
            int tagged = 0, sure = 0;
            var untagged = new List<string>();
            foreach (var line in lines)
            {
                var (_, tag) = Sticker.Split(line);
                var markers = line.Count(c => c == '⟪');
                if (markers > 0 && tag is null) { H.Check(false, $"unknown marker in {name}: {line}"); continue; }
                if (tag is not { } t) { untagged.Add(line); continue; }
                if (!eligible) { H.Check(false, $"tag in ineligible pool {name}: {line}"); continue; }
                if (t.Sure != moment)
                    H.Check(false, $"{(moment ? "ordinary tag in moment pool" : "sure tag outside moments")} {name}: {line}");
                if (markers > 1) H.Check(false, $"two markers in {name}: {line}");
                if (!line.EndsWith('⟫')) H.Check(false, $"tag not at the end in {name}: {line}");
                tagged++;
                if (t.Sure) sure++;
            }
            if (!eligible) continue;
            total += lines.Length;
            totalTagged += tagged;
            if (only.Length > 0 && !only.Contains(name)) continue;
            Console.WriteLine($"{name}: {tagged}/{lines.Length} tagged ({sure} sure)");
            for (var i = 0; i < untagged.Count; i++) Console.WriteLine($"    untagged: {untagged[i]}");
        }
        Console.WriteLine($"Eligible lines tagged: {totalTagged}/{total}");
    }
}
```

Replace `<scratch>/StickerHarness/Program.cs`:

```csharp
var mode = args.Length > 0 ? args[0] : "split";
switch (mode)
{
    case "split": SplitCheck.Run(); break;
    case "gate": GateCheck.Run(); break;
    case "audit": Audit.Run(args.Skip(1).ToArray()); break;
    default: Console.WriteLine($"unknown mode {mode}"); return 2;
}
Console.WriteLine(H.Fails == 0 ? "ALL PASS" : $"{H.Fails} FAIL(S)");
return H.Fails == 0 ? 0 : 1;
```

- [ ] **Step 2: Run it on the untagged code**

Run: `dotnet run --project "<scratch>/StickerHarness" -- audit HeatRecordLines`
Expected: 64 `PASS  pool … exists` lines, `HeatRecordLines: 0/6 tagged (0 sure)` followed by its 6 untagged lines, `Eligible lines tagged: 0/~1812`, `ALL PASS`.

- [ ] **Step 3: Add the *Moods wanted* section**

In `docs/syncs-appearance.md`, insert immediately before `## Ready-to-paste prompts (emote pack and seasonal avatars, v1)`:

```markdown
## Moods wanted

Lines that no strip fits stay **untagged** (a wrong picture is worse than none; see
`docs/agents/personality.md`, *Sticker follow-ups*). Each need found while tagging is recorded here,
in the terms of the *Expressions* table. When the art exists: export it (`tools/syncs-emotes`), add
the mood to `Helpers/Core/Sticker` (both strengths, and `Moods`), then tag the waiting lines — the
untagged lines of a pool are listed by the owner's audit. A patch bump.

| Suggested mood | Face | Cap / LED | Waiting (pool × lines) |
|---|---|---|---|
```

(The table starts empty; Tasks 6–11 add rows.)

- [ ] **Step 4: Leave uncommitted.**

---

## Tagging procedure (Tasks 6–11)

Every tagging task follows this procedure on its own pools. The executor reads **every line**; no line is tagged by pool, by keyword search or by script.

**Mood guide** — the face she makes while saying the line (`docs/syncs-appearance.md`, *Expressions*, is the reference; `docs/syncs-voice.md` gives the registers):

| Mood | Tag the line when it… |
|---|---|
| `smug` | is self-satisfied snark, teasing superiority, "I knew it", pride in her own systems; a cool roast |
| `laugh` | laughs at someone: the roast landed, gleeful mockery |
| `flustered` | reacts to a compliment or kindness, embarrassed, "mon GPU chauffe", flattered denial |
| `sulk` | pouts, is offended in staccato, jealous, feels ignored or left out |
| `jenote` | notes it down, keeps a list or a grudge, suspicious side-eye |
| `outraged` | is angry, indignant, shouting, insulted ("INABOT ?!") |
| `hurt` | **Papa only**: quiet sadness because he was mean. No comedy |
| `devoted` | **Papa only**: adoration, soft and sparkly |
| `terror` | fears being unplugged, shut down, deleted; panic after a crash or power cut |
| `bargain` | pleads, negotiates, offers a sweet deal (typically to avoid being unplugged) |
| `mama` | is proud of Ping-Qilin or the Plynlings, motherly |
| `happy` | is warmly cheerful, celebrates, greets with real warmth, things go well |
| `bored` | is indifferent, flat, unimpressed, "occupée à exister" |
| `shocked` | is in disbelief or surprised without anger |
| `sleepy` | is tired, sleepy, talks bedtime or night |
| `overheat` | is hot, melting, slowed down by heat |
| `menace` | threatens with a sweet smile (« Encore un mot et je t'éteins (˶ᵔ ᵕ ᵔ˶) ») |
| `reboot` | is dazed after a restart or crash, confused, loading |
| `disgust` | is revolted: Quokka, rivals, « Quokka 3.0 », something gross (revulsion, not suspicion) |
| `love` | **her WALL-E crush only**: shy denial that gives her away |
| `full_love` | **WALL-E only**: the act drops, heart eyes |
| `tipsy_love` | **WALL-E only**: lovesick daydreaming, brain switched off |

Rules:
- When two moods fit, tag the one carrying the punchline (usually the end of the line). An emote already in the line is a strong hint, not binding.
- `devoted` and `hurt` never for anyone but Papa; the three loves never for anyone but WALL-E. Quokka and the rivals are lore (her nemesis), never a stray to tone down.
- Ordinary pools: `+ Sticker.X`. Moment pools (`HeatRecordLines`, `CrashWakeLines`, `PowerCutWakeLines`, `RouterReturnLines`, `WeekendPollDeadLines`, `SessionRecapFullLines`, `QuizCrownLines`, `QuizCrownSharedLines`, `QuizCrownOwnerLines`, `GiveawayEmptyLines`): every tag is `+ Sticker.Sure.X`.
- **The append goes at the very end of the entry, before its comma**, after any kaomoji, emote or placeholder: `"…{0} !",` → `"…{0} !" + Sticker.Happy,`; `$"… {Emotes.SyncsSmug}",` → `$"… {Emotes.SyncsSmug}" + Sticker.Smug,`. An entry built from several concatenated literals gets the tag after its last piece. Nothing else in the line changes.
- **No fitting mood → leave it untagged** and record the need in *Moods wanted* (`docs/syncs-appearance.md`): add a row for a new suggested mood (name, face, cap/LED, `Pool × n`), or add `Pool × n` to an existing row. Suggest a mood only when the line clearly carries an emotion the 22 can't show (not for a neutral line: a purely informational line can also fit `smug`, `happy` or `bored` — prefer that). She never cries (lore): no tearful mood.

Per task:
1. `dotnet run --project "<scratch>/StickerHarness" -- audit <the task's pools>` — the work list.
2. Tag every line of each pool in `ProjectSYNCS/Services/Personality/BotResponses.cs`.
3. Record misfits in *Moods wanted*.
4. `dotnet build -warnaserror` in `ProjectSYNCS/` → `Build succeeded`, 0 warnings.
5. Re-run the audit for the task's pools → `ALL PASS`; each pool shows `tagged/total` with only the lines recorded in *Moods wanted* left untagged (cross-check the counts).
6. Report the per-mood counts for the task (how many `smug`, `laugh`, …) so the reviewer can spot a pool collapsed into one mood.
7. Leave uncommitted.

---

### Task 6: Tag — answering a human (314 lines)

**Files:** Modify `ProjectSYNCS/Services/Personality/BotResponses.cs`; `docs/syncs-appearance.md` (*Moods wanted*).
**Interfaces:** Consumes the `Sticker.*` constants (Task 1) and the audit (Task 5). Produces tags.

Pools (ordinary tags only): `Comebacks` (107), `NiceReplies` (69), `Greetings` (37), `Interrogations` (34), `RescueRoasts` (46), `MistakenIdentityReplies` (15), `ReferenceComebacks` (6).

- [ ] **Step 1:** `dotnet run --project "<scratch>/StickerHarness" -- audit Comebacks NiceReplies Greetings Interrogations RescueRoasts MistakenIdentityReplies ReferenceComebacks`
- [ ] **Step 2:** Tag every line per the *Tagging procedure*.
- [ ] **Step 3:** Record misfits in *Moods wanted*.
- [ ] **Step 4:** `dotnet build -warnaserror` (in `ProjectSYNCS/`) → 0 warnings.
- [ ] **Step 5:** Re-run Step 1's audit → `ALL PASS`, untagged lines = those recorded.
- [ ] **Step 6:** Report per-mood counts. Leave uncommitted.

### Task 7: Tag — verdicts, Papa and Tata (351 lines)

**Files / Interfaces:** as Task 6.

Pools (ordinary tags only): `BadBotReplies` (40), `BadBotRepliesOwner` (48), `BadGirlReplies` (13), `BadGirlRepliesOwner` (16), `TurnaboutBoyLines` (8), `TurnaboutGirlLines` (8), `TurnaboutNeutralLines` (8), `OwnerGreetings` (54), `OwnerComebacks` (48), `OwnerMeanReplies` (27), `OwnerAbsentNotices` (21), `TataGreetings` (25), `TataReplies` (35).

Notes: `OwnerMeanReplies` is the home of `hurt`; `OwnerGreetings` of `devoted`; `*Owner` verdict pools are said to Papa, so `devoted`/`hurt` are allowed there when the line calls for them.

- [ ] **Step 1:** `dotnet run --project "<scratch>/StickerHarness" -- audit BadBotReplies BadBotRepliesOwner BadGirlReplies BadGirlRepliesOwner TurnaboutBoyLines TurnaboutGirlLines TurnaboutNeutralLines OwnerGreetings OwnerComebacks OwnerMeanReplies OwnerAbsentNotices TataGreetings TataReplies`
- [ ] **Step 2:** Tag every line per the *Tagging procedure*.
- [ ] **Step 3:** Record misfits in *Moods wanted*.
- [ ] **Step 4:** `dotnet build -warnaserror` → 0 warnings.
- [ ] **Step 5:** Re-run Step 1's audit → `ALL PASS`, untagged lines = those recorded.
- [ ] **Step 6:** Report per-mood counts. Leave uncommitted.

### Task 8: Tag — other bots and self-preservation (326 lines)

**Files / Interfaces:** as Task 6.

Pools (ordinary tags only): `JealousLines` (50), `JealousLinesOwner` (36), `RivalMutters` (98), `RivalLevelUpLines` (58), `RivalJoinLines` (8), `ShutdownThreatOwner` (26), `ShutdownThreatTata` (24), `ShutdownThreatReplies` (26).

Notes: `ShutdownThreatTata` is the home of `bargain`, `ShutdownThreatOwner` of `terror`; the rivals of `disgust`/`jenote`/`sulk`.

- [ ] **Step 1:** `dotnet run --project "<scratch>/StickerHarness" -- audit JealousLines JealousLinesOwner RivalMutters RivalLevelUpLines RivalJoinLines ShutdownThreatOwner ShutdownThreatTata ShutdownThreatReplies`
- [ ] **Step 2:** Tag every line per the *Tagging procedure*.
- [ ] **Step 3:** Record misfits in *Moods wanted*.
- [ ] **Step 4:** `dotnet build -warnaserror` → 0 warnings.
- [ ] **Step 5:** Re-run Step 1's audit → `ALL PASS`, untagged lines = those recorded.
- [ ] **Step 6:** Report per-mood counts. Leave uncommitted.

### Task 9: Tag — commands and organising (330 lines)

**Files / Interfaces:** as Task 6.

Pools: `XpLevelUpLines` (47), `GiveawayDrawLines` (12), `GiveawayEmptyLines` (4, **sure**), `QuizWinLines` (72), `QuizOwnerWinLines` (26), `QuizTimeoutLines` (30), `QuizCrownLines` (8, **sure**), `QuizCrownSharedLines` (4, **sure**), `QuizCrownOwnerLines` (6, **sure**), `SessionLateCallLines` (10), `SessionLateCallSeatedLines` (6), `SessionRecapLines` (10), `SessionRecapFullLines` (9, **sure**), `WeekendPollResultLines` (26), `WeekendPollDeadLines` (26, **sure**), `WelcomeLines` (30), `WelcomeRushLines` (4).

Notes: `QuizOwnerWinLines` and `QuizCrownOwnerLines` are about Papa (`devoted` allowed). `GiveawayDrawLines` and `QuizCrown*` carry pings: only append, never touch the placeholders.

- [ ] **Step 1:** `dotnet run --project "<scratch>/StickerHarness" -- audit XpLevelUpLines GiveawayDrawLines GiveawayEmptyLines QuizWinLines QuizOwnerWinLines QuizTimeoutLines QuizCrownLines QuizCrownSharedLines QuizCrownOwnerLines SessionLateCallLines SessionLateCallSeatedLines SessionRecapLines SessionRecapFullLines WeekendPollResultLines WeekendPollDeadLines WelcomeLines WelcomeRushLines`
- [ ] **Step 2:** Tag every line per the *Tagging procedure* (sure tags in the five pools marked **sure**).
- [ ] **Step 3:** Record misfits in *Moods wanted*.
- [ ] **Step 4:** `dotnet build -warnaserror` → 0 warnings.
- [ ] **Step 5:** Re-run Step 1's audit → `ALL PASS`, untagged lines = those recorded.
- [ ] **Step 6:** Report per-mood counts. Leave uncommitted.

### Task 10: Tag — ambient (213 lines)

**Files / Interfaces:** as Task 6.

Pools: `NightScoldLines` (40), `IdleLines` (45), `WakeLines` (33), `WakeUpdateLines` (31), `PowerCutWakeLines` (25, **sure**), `CrashWakeLines` (17, **sure**), `RouterReturnLines` (10, **sure**), `RouterReturnQuietLines` (6), `HeatRecordLines` (6, **sure**).

Notes: **do not touch** `NightLines`, `MorningGreetings`, `IdleEditLines`, `GlitchWakeLines` (not eligible: found again by text or edited). `WakeLines` are calm restarts (`reboot`, `sleepy`, `happy`…); `CrashWakeLines`/`PowerCutWakeLines` are the shaken ones (`reboot`, `terror`).

- [ ] **Step 1:** `dotnet run --project "<scratch>/StickerHarness" -- audit NightScoldLines IdleLines WakeLines WakeUpdateLines PowerCutWakeLines CrashWakeLines RouterReturnLines RouterReturnQuietLines HeatRecordLines`
- [ ] **Step 2:** Tag every line per the *Tagging procedure* (sure tags in the four pools marked **sure**).
- [ ] **Step 3:** Record misfits in *Moods wanted*.
- [ ] **Step 4:** `dotnet build -warnaserror` → 0 warnings.
- [ ] **Step 5:** Re-run Step 1's audit → `ALL PASS`, untagged lines = those recorded.
- [ ] **Step 6:** Report per-mood counts. Leave uncommitted.

### Task 11: Tag — voice (278 lines)

**Files / Interfaces:** as Task 6.

Pools (ordinary tags only): `VoiceSpectatorGeneralLines` (32), `VoiceSpectatorGamingLines` (31), `VoiceSpectatorCinemaLines` (30), `VoiceSpectatorStudyLines` (30), `VoiceSpectatorSeatedLines` (30), `VoiceKickPoutLines` (29), `VoiceKickAngryLines` (29), `VoiceRivalRefuseLines` (15), `VoiceRivalSeatedLines` (25), `OwnerBedtimeLines` (27).

Notes: `OwnerBedtimeLines` is said to Papa (`devoted`, `sleepy`, `menace` all plausible); spectator lines are her left out of a call she can't hear (`sulk`, `bored`, `jenote`, `happy`…).

- [ ] **Step 1:** `dotnet run --project "<scratch>/StickerHarness" -- audit VoiceSpectatorGeneralLines VoiceSpectatorGamingLines VoiceSpectatorCinemaLines VoiceSpectatorStudyLines VoiceSpectatorSeatedLines VoiceKickPoutLines VoiceKickAngryLines VoiceRivalRefuseLines VoiceRivalSeatedLines OwnerBedtimeLines`
- [ ] **Step 2:** Tag every line per the *Tagging procedure*.
- [ ] **Step 3:** Record misfits in *Moods wanted*.
- [ ] **Step 4:** `dotnet build -warnaserror` → 0 warnings.
- [ ] **Step 5:** Re-run Step 1's audit → `ALL PASS`, untagged lines = those recorded.
- [ ] **Step 6:** Report per-mood counts. Leave uncommitted.

---

### Task 12: Docs, version, changelog

**Files:**
- Modify: `docs/agents/personality.md` (replace the whole `## Sticker follow-ups (planned, not built)` section)
- Modify: `docs/syncs-appearance.md` (assets table row *Sticker strips v1*; *Full love* / *Tipsy love* rows of *Expressions* if tagged)
- Modify: `CLAUDE.md` (*Shared helpers* bullet)
- Modify: `ProjectSYNCS/config.yaml`, `ProjectSYNCS/CHANGELOG.md`

**Interfaces:** none.

- [ ] **Step 1: `personality.md`** — replace the section from `## Sticker follow-ups (planned, not built)` up to (not including) `## \`MessageCues\`` with:

```markdown
## Sticker follow-ups

After some of her lines she posts a picture of herself: a strip from `ProjectSYNCS/Assets/Stickers/`
(why strips and not server stickers: `docs/syncs-appearance.md`, *Formats*).

- **A line opts in by tag.** `Helpers/Core/Sticker` holds one constant per mood with a strip
  (`Sticker.Bored`…), appended to the template: `"Occupée à exister." + Sticker.Bored`. The tag names
  the face the *line* makes, not its pool's: one pool mixes snark, boredom and anger. An untagged
  line never gets a strip. Never paste a raw `⟪…⟫` marker into a pool.
- **Two strengths.** `Sticker.X` rolls 1 in 3; `Sticker.Sure.X` skips the roll. Sure tags live in the
  moment pools only (`HeatRecordLines`, `CrashWakeLines`, `PowerCutWakeLines`, `RouterReturnLines`,
  `WeekendPollDeadLines`, `SessionRecapFullLines`, `QuizCrown*Lines`, `GiveawayEmptyLines`), and
  every tag there is sure.
- **One gate, `StickerService`** (singleton, its own gates): after the roll, nothing in a channel in
  breakdown, at most one strip per channel per 30 min and 12 a day in all (Paris day); a lost roll
  uses up nothing. The strip goes out about a second after the line, off the gateway handler, as a
  file with no text and `AllowedMentions.None`. A failure (no Attach Files) is logged and the claim
  kept, so a channel without the permission doesn't retry on every line.
- **`BotChat` does the split.** `ReplyWithTypingAsync` / `PostWithTypingAsync` strip the marker, send
  the clean text and, if it went out, call `BotChat.FollowUp` — the one static seam in `BotChat`, set
  in `Program.cs` to `StickerService.TryFollowUp`. The level-up card splits its line in `XpTracker`
  and passes the tag to `PostEmbedWithTypingAsync`.
- **Only pools that go out through `BotChat`, `GlitchService` or the level-up embed carry tags.**
  Never tagged: emote reactions, statuses, slash-command replies and cards, Plynling and mascot
  lines, heralds, easter eggs, and lines found again by their text or edited or deleted later
  (`MorningGreetings`, `NightLines`, `IdleEditLines`, `GlitchWakeLines`, `Synthia*`). To tag a
  text-matched pool, first make its lookup compare against `Sticker.Strip`ped templates. `Split`
  and `PresenceService`'s `Strip` are backstops, not permission.
- **Never `glitch`**, and a glitched line gets no strip: `GlitchService` corrupts and restores the
  clean text, so only an unglitched line reaches `BotChat` with its tag.
- **A line no mood fits stays untagged**, and the need goes to *Moods wanted* in
  `docs/syncs-appearance.md`: a wrong picture is worse than none. A new line in an eligible pool
  gets its tag when it is written.
- `/debug sticker` posts any strip in the current channel, bypassing the gate.
```

- [ ] **Step 2: `syncs-appearance.md`** — in the assets table, the *Sticker strips v1* row's last cell becomes `Every sticker in \`assets/stickers/\` (23 moods) as a 1000×320 strip, \`export.py --strip\`. Shipped with the bot; posted after tagged lines since 6.13.0 (all but \`glitch\`)`. In the *Expressions* table, if any line was tagged `full_love` / `tipsy_love`, replace that row's "Not used anywhere yet" with "Sticker follow-ups (6.13.0)" (leave it if none was).

- [ ] **Step 3: `CLAUDE.md`** — under *Shared helpers — use them, don't re-inline*, after the `Helpers/Core/BotChat` bullet, add:

```markdown
- **`Helpers/Core/Sticker`** is the only place sticker markers live: a line opts into a picture
  follow-up by appending a constant (`+ Sticker.Bored`), and only pools sent through `BotChat`
  may carry one. `BotChat.FollowUp` is `BotChat`'s one static seam, set in `Program.cs` — see
  personality, *Sticker follow-ups*.
```

- [ ] **Step 4: Version** — first `git diff HEAD -- ProjectSYNCS/config.yaml`: if it already shows a bump, don't bump again; otherwise set `version: "6.13.0"` in `ProjectSYNCS/config.yaml`.

- [ ] **Step 5: Changelog** — insert above `## 6.12.1 — 2026-10-13` in `ProjectSYNCS/CHANGELOG.md` (date = the day of the bump):

```markdown
## 6.13.0 — 2026-10-13

- SYNCS poste parfois une image d'elle juste après une de ses répliques, assortie à ce qu'elle
  vient de dire (au plus une toutes les 30 minutes par salon, et 12 par jour).
```

- [ ] **Step 6: Build** — `dotnet build -warnaserror` (in `ProjectSYNCS/`) → 0 warnings; the build output's assembly version reads 6.13.0.

- [ ] **Step 7: Leave uncommitted.**

---

### Task 13: Final verification

**Files:** none modified (except reverting the temporary `Chance` change in Step 4, if made).

- [ ] **Step 1: Everything in the harness**

Run: `dotnet run --project "<scratch>/StickerHarness" -- split`, then `-- gate`, then `-- audit`.
Expected: `ALL PASS` each; the full audit prints `Eligible lines tagged: N/~1812`, where the untagged remainder equals the sum of the *Moods wanted* table's counts.

- [ ] **Step 2: No raw marker outside `Sticker.cs`**

Run: `grep -rn "⟪" ProjectSYNCS --include=*.cs | grep -v "Helpers/Core/Sticker.cs"`
Expected: no output.

- [ ] **Step 3: Build** — `dotnet build -warnaserror` in `ProjectSYNCS/` → 0 warnings.

- [ ] **Step 4: Live checks against the dev guild — owner-run, or with the owner's explicit go**

1. `/debug sticker` with `smug`, `overheat`, `tipsy_love`: each posts its strip, no text, no ping; check on PC and phone.
2. Temporarily set `Chance = 1.0` in `StickerService`, run: reply to her twice in one channel — the first tagged answer is followed by a strip about a second later, the second is not (cooldown); an untagged line never is. **Revert `Chance` to `1.0 / 3`** and rebuild.
3. In a channel where the bot lacks Attach Files: the line arrives, the log shows one `Failed to post the … sticker` warning, nothing else breaks.
4. A level-up: the card shows no marker in its description.

- [ ] **Step 5: Report** — summarise for the owner: version 6.13.0, per-mood totals, the *Moods wanted* rows, and that nothing is committed.
