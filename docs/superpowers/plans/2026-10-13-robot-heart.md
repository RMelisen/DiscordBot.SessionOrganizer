# Her robot heart Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** SYNCS's robot heart (WALL-E, EVE, GLaDOS, Wheatley, the robot vacuum, Monika, 2B, Voyager 1) shows in what she does: she reacts when someone names one of them, Voyager 1 writes to her with his real light-time delay, and her existing pools hint at all of them.

**Architecture:** Two pure helpers in `Helpers/Personality/` — `RobotHeart` (characters, acts, daily gate) and `Voyager` (a JPL Horizons light-time table, the letter calendar). `MessageCues.ReadHeartCharacter` detects the names. A new `RobotHeartService` singleton plays the acts and sits right after `SynthiaService` in `BotService`'s fan-out. The letters are one more step of `AmbientService`'s 10-minute tick, with their state in the existing `ambient-state.json`. `/debug voyager` shows the state and posts any line. Content goes into `BotResponses`.

**Tech Stack:** .NET 10, Discord.Net 3.20.1. No test project: checks run from a scratch console harness that references the bot project (internal types reached by reflection).

**Spec:** `docs/superpowers/specs/2026-10-13-robot-heart-design.md` — read it before starting any task.

## Global Constraints

- Read `CLAUDE.md`, `docs/agents/personality.md` and `docs/syncs-voice.md` before touching code; their rules bind.
- **Never commit.** The owner commits by hand. Every task ends with the work left uncommitted.
- Build check for every code task: `dotnet build -warnaserror` in `ProjectSYNCS/` must succeed with 0 warnings.
- Shares and caps exactly: `PerformanceShare = 0.40` (so 60% line + emote), `AmbientChance = 0.60`, `DailyCap = 3`, each character once per 24 h.
- Letters: gaps of **10–18 days**, slot **10:00–22:00** Paris, answer for about half the letters **2–6 h** after the letter and only if before **23:00**; crossing day **2026-11-18**.
- Code, comments and logs in English; every user-facing string in French; the slash option name is `post` (English).
- Lines: `tu` to a person, `vous` only to the whole server; never gender the person; no « mon crush »; one tag at most, at the end; custom emotes only through `Helpers/Core/Emotes`; **never `(｡•́︿•̀｡)`**.
- **Vanish and edit pools, `NightLines`, `MorningFunFacts` and `PresenceFillers` carry no sticker tag.** Tags only through `Sticker.X` constants.
- Every fun fact must be true. The ones in Task 7 were checked when this plan was written; don't reword a fact's content.
- Version: `ProjectSYNCS/config.yaml` goes **6.13.0 → 6.14.0** exactly once (Task 8), with a `CHANGELOG.md` entry that names no trigger and no character.
- New files go in subsystem folders, flat namespaces: `ProjectSYNCS.Helpers`, `ProjectSYNCS.Services`.
- Absent from `README.md` and `/help`.

## Review Focus

1. **A restart wiping the letter state.** `WakeAsync` builds `new AmbientState(AppInfo.Version, today)`; with new fields that would reset `LastLetterDay` and `CrossingDone` on every restart and repeat a letter. Every save must start from `LoadState()`. Pinned by Task 5's grep step and its old-file deserialisation check.
2. **French « voyager » the verb.** « je vais voyager 1 semaine au Japon », « voyager », « Star Trek Voyager » must not fire. Pinned by Task 3's cue checks.
3. **A message aimed at her once the gate is spent** (second WALL-E of the day, or the fourth character) gets her ordinary comeback, not silence: `HandleMessageAsync` returns false. Pinned by Task 4's code read and Task 9's live check.
4. **A message naming Synthia and WALL-E.** Synthia wins and nothing doubles: `RobotHeartService` runs after `SynthiaService`, which returns true. Pinned by Task 4's fan-out step and Task 9's live check.
5. **An older `ambient-state.json`** without the new fields reads as "no letter today, crossing not done", without throwing. Pinned by Task 5's deserialisation check.

---

## File Structure

| File | Status | Responsibility |
|---|---|---|
| `ProjectSYNCS/Helpers/Personality/Voyager.cs` | Create | Light-time table and interpolation, `Format`, letter calendar, crossing day |
| `ProjectSYNCS/Helpers/Personality/RobotHeart.cs` | Create | `HeartCharacter`, `HeartActKind`, the act roll, `HeartGate` |
| `ProjectSYNCS/Services/Personality/MessageCues.cs` | Modify | `ReadHeartCharacter` |
| `ProjectSYNCS/Services/Personality/RobotHeartService.cs` | Create | Plays the acts |
| `ProjectSYNCS/Services/Core/BotService.cs` | Modify | Fan-out position |
| `ProjectSYNCS/Program.cs` | Modify | Registration |
| `ProjectSYNCS/Services/Personality/AmbientService.cs` | Modify | `TryVoyagerAsync`, state fields, line builders |
| `ProjectSYNCS/Commands/Admin/DebugModule.cs` | Modify | `/debug voyager` |
| `ProjectSYNCS/Services/Personality/BotResponses.cs` | Modify | Heart pools, Voyager pools, lines in existing pools, pool index |
| `docs/agents/personality.md`, `docs/syncs-voice.md`, `docs/syncs-appearance.md` | Modify | Built rules (Task 8) |
| `ProjectSYNCS/config.yaml`, `ProjectSYNCS/CHANGELOG.md` | Modify | 6.14.0 (Task 8) |
| `<scratch>/HeartHarness/` | Create (outside the repo) | Scratch checks: `voyager`, `heart`, `cues`, `pools` |

`<scratch>` is any directory **outside the repository** (the session's scratchpad if it has one, else a temp directory); `<repo>` is the absolute path of the repository root. Substitute both, as absolute paths, wherever they appear below. The harness never goes into the repo. It needs the .NET 10 SDK.

---

### Task 1: Voyager's light time and calendar — `Helpers/Personality/Voyager`

**Files:**
- Create: `ProjectSYNCS/Helpers/Personality/Voyager.cs`
- Create (scratch): `<scratch>/HeartHarness/HeartHarness.csproj`, `H.cs`, `VoyagerCheck.cs`, `Program.cs`

**Interfaces:**
- Produces:
  - `public static class Voyager` (namespace `ProjectSYNCS.Helpers`)
  - `public static TimeSpan LightTime(DateTimeOffset at)`
  - `public static string Format(TimeSpan lightTime)` → `"23 h 55"`
  - `public static readonly DateOnly CrossingDay` (2026-11-18)
  - `public static bool IsCrossingDay(int dayNumber)`
  - `public static int? LetterNumber(int dayNumber)` — the letter's index in the walk, or null when no letter that day
  - `public static TimeSpan LetterSlot(int dayNumber)` — Paris wall-clock time of day
  - `public static TimeSpan? AnswerSlot(int dayNumber)`
  - `public const int MinGapDays = 10, MaxGapDays = 18`

- [ ] **Step 1: Create the scratch harness (the failing check)**

`<scratch>/HeartHarness/HeartHarness.csproj`:

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

`<scratch>/HeartHarness/H.cs`:

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

`<scratch>/HeartHarness/VoyagerCheck.cs`:

```csharp
using ProjectSYNCS.Helpers;

static class VoyagerCheck
{
    static DateTimeOffset Utc(int y, int m, int d) => new(y, m, d, 0, 0, 0, TimeSpan.Zero);

    public static void Run()
    {
        // 1. Table samples and in-between dates against JPL Horizons (one-way light time,
        //    geocentric, minutes). The 15-day samples fall between the table's 30-day ones.
        (DateTimeOffset At, double Minutes)[] horizons =
        {
            (Utc(2026, 1, 1), 1413.716), (Utc(2026, 1, 16), 1414.018), (Utc(2026, 10, 13), 1434.352),
            (Utc(2026, 11, 12), 1439.096), (Utc(2027, 4, 26), 1441.399), (Utc(2027, 7, 10), 1446.622),
            (Utc(2028, 11, 16), 1498.889), (Utc(2028, 12, 31), 1502.535),
        };
        foreach (var (at, minutes) in horizons)
        {
            var got = Voyager.LightTime(at).TotalMinutes;
            H.Check(Math.Abs(got - minutes) < 0.3, $"1 light time {at:yyyy-MM-dd}: {got:0.00} vs {minutes:0.00}");
        }

        // 2. Never back under a light-day once past it (the crossing line says so). From the 25th:
        //    interpolating across the 30-day gap puts the table's own crossing a few days after
        //    JPL's (19 Nov) — harmless, the lines show the time rounded to 5 minutes.
        var below = 0;
        for (var at = Utc(2026, 11, 25); at < Utc(2036, 1, 1); at = at.AddDays(1))
            if (Voyager.LightTime(at).TotalMinutes < 1440) below++;
        H.Check(below == 0, $"2 never under 1440 min after 2026-11-25 ({below} days)");

        // 3. Past the table it keeps drifting away, before it it holds the first value.
        H.Check(Voyager.LightTime(Utc(2037, 1, 1)) > Voyager.LightTime(Utc(2035, 12, 10)), "3 drift after the table");
        H.Check(Math.Abs(Voyager.LightTime(Utc(2025, 6, 1)).TotalMinutes - 1413.72) < 0.01, "3 clamp before the table");

        // 4. Format rounds to 5 minutes.
        H.Check(Voyager.Format(TimeSpan.FromMinutes(1437)) == "23 h 55", "4 1437 min → 23 h 55");
        H.Check(Voyager.Format(TimeSpan.FromMinutes(1438)) == "24 h 00", "4 1438 min → 24 h 00");
        H.Check(Voyager.Format(TimeSpan.FromMinutes(1446.6)) == "24 h 05", "4 1446.6 min → 24 h 05");

        // 5. Crossing day.
        H.Check(Voyager.CrossingDay == new DateOnly(2026, 11, 18), "5 crossing day 2026-11-18");
        H.Check(Voyager.IsCrossingDay(new DateOnly(2026, 11, 18).DayNumber)
                && !Voyager.IsCrossingDay(new DateOnly(2026, 11, 19).DayNumber), "5 IsCrossingDay");

        // 6. Letter calendar over two years: gaps in 10–18 days, numbers consecutive.
        var start = new DateOnly(2026, 10, 1).DayNumber;
        var days = new List<int>();
        for (var d = start - 30; d < start + 730; d++)
            if (Voyager.LetterNumber(d) is { } n)
            {
                H.Check(n == days.Count, $"6 letter #{n} numbered in order");
                days.Add(d);
            }
        H.Check(days.Count > 0 && days[0] >= start, "6 no letter before the walk starts");
        var gaps = days.Zip(days.Skip(1), (a, b) => b - a).ToList();
        H.Check(gaps.All(g => g is >= Voyager.MinGapDays and <= Voyager.MaxGapDays),
            $"6 gaps all in 10–18 (min {gaps.Min()}, max {gaps.Max()})");
        H.Check(days.Count is >= 40 and <= 73, $"6 about one a fortnight ({days.Count} in two years)");

        // 7. Slots: letter in [10:00, 22:00); answer null or 2–6 h after, before 23:00; about half answered.
        int answered = 0;
        foreach (var d in days)
        {
            var slot = Voyager.LetterSlot(d);
            H.Check(slot >= TimeSpan.FromHours(10) && slot < TimeSpan.FromHours(22), $"7 letter slot {slot:hh\\:mm}");
            if (Voyager.AnswerSlot(d) is not { } a) continue;
            answered++;
            H.Check(a - slot >= TimeSpan.FromHours(2) && a - slot <= TimeSpan.FromHours(6) && a < TimeSpan.FromHours(23),
                $"7 answer {a:hh\\:mm} after {slot:hh\\:mm}");
        }
        H.Check(answered > days.Count / 5 && answered < days.Count * 4 / 5, $"7 some answered ({answered}/{days.Count})");
        H.Check(Voyager.LetterSlot(start) == Voyager.LetterSlot(start), "7 slot is deterministic");
    }
}
```

`<scratch>/HeartHarness/Program.cs`:

```csharp
var mode = args.Length > 0 ? args[0] : "voyager";
switch (mode)
{
    case "voyager": VoyagerCheck.Run(); break;
    default: Console.WriteLine($"unknown mode {mode}"); return 2;
}
Console.WriteLine(H.Fails == 0 ? "ALL PASS" : $"{H.Fails} FAIL(S)");
return H.Fails == 0 ? 0 : 1;
```

- [ ] **Step 2: Run it to verify it fails**

Run: `dotnet run --project "<scratch>/HeartHarness" -- voyager`
Expected: build error `CS0103` — `Voyager` does not exist.

- [ ] **Step 3: Write `Voyager.cs`**

`ProjectSYNCS/Helpers/Personality/Voyager.cs`:

```csharp
namespace ProjectSYNCS.Helpers;

// Voyager 1, her long-distance friend (docs/syncs-voice.md, Her robot heart). Pure, for
// AmbientService's letters and /debug voyager: how long his messages take, and on which days
// one arrives.
//
// The light time comes from JPL Horizons rather than a formula: Voyager 1 (-31) seen from the
// Earth's centre (500@399), one-way light time (QUANTITIES=21), every 30 days from
// 2026-01-01 00:00 UTC, fetched 2026-10-13. The Earth's orbit swings it by about ±6 minutes a
// year; linear interpolation between 30-day samples stays within 0.2 min. Past the table it
// drifts at the table's mean rate. Lines show it rounded to 5 minutes, so the number is true.
public static class Voyager
{
    private static readonly DateTimeOffset TableStart = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private const double StepDays = 30;

    private static readonly double[] LightMinutes =
    {
        1413.72, 1413.96, 1413.13, 1412.11, 1411.81, 1412.91, 1415.73, 1420.16,
        1425.70, 1431.57, 1436.89, 1440.89, 1443.13, 1443.64, 1442.94, 1441.89,
        1441.40, 1442.22, 1444.73, 1448.89, 1454.28, 1460.16, 1465.64, 1469.92,
        1472.49, 1473.28, 1472.74, 1471.68, 1471.03, 1471.57, 1473.77, 1477.66,
        1482.88, 1488.73, 1494.34, 1498.89, 1501.78, 1502.86, 1502.51, 1501.47,
        1500.68, 1500.97, 1502.86, 1506.46, 1511.48, 1517.28, 1523.00, 1527.79,
        1531.00, 1532.39, 1532.25, 1531.27, 1530.36, 1530.41, 1531.99, 1535.30,
        1540.11, 1545.82, 1551.62, 1556.63, 1560.15, 1561.86, 1561.94, 1561.05,
        1560.06, 1559.88, 1561.16, 1564.17, 1568.74, 1574.35, 1580.20, 1585.40,
        1589.22, 1591.26, 1591.59, 1590.81, 1589.77, 1589.39, 1590.38, 1593.07,
        1597.40, 1602.87, 1608.74, 1614.12, 1618.23, 1620.59, 1621.19, 1620.55,
        1619.49, 1618.93, 1619.64, 1622.02, 1626.08, 1631.39, 1637.26, 1642.79,
        1647.17, 1649.85, 1650.74, 1650.27, 1649.21, 1648.50, 1648.94, 1651.01,
        1654.79, 1659.93, 1665.75, 1671.41, 1676.04, 1679.04, 1680.24, 1679.96,
        1678.93, 1678.10, 1678.29, 1680.05, 1683.54, 1688.48, 1694.24, 1699.99,
        1704.86, 1708.18,
    };

    // The table's mean drift, 2026–2035: (1708.18 − 1413.72) / (121 × 30 days).
    private const double DriftMinutesPerDay = 0.0811;

    /// <summary>The day he reached one light-day from Earth, as NASA announced it.</summary>
    public static readonly DateOnly CrossingDay = new(2026, 11, 18);

    // The letter walk starts here; each step waits MinGapDays–MaxGapDays.
    private static readonly int FirstWalkDay = new DateOnly(2026, 10, 1).DayNumber;
    public const int MinGapDays = 10;
    public const int MaxGapDays = 18;

    // Letters arrive between these Paris hours; her answer, when there is one, 2–6 h later
    // and never at or after AnswerEndHour.
    public const int LetterStartHour = 10;
    public const int LetterEndHour = 22;
    public const int AnswerEndHour = 23;

    private const ulong GapSalt = 0x564F_5941_4741_5031;    // "VOYAGAP1"
    private const ulong SlotSalt = 0x564F_5941_534C_4F54;   // "VOYASLOT"
    private const ulong AnswerSalt = 0x564F_5941_414E_5357; // "VOYAANSW"
    private const ulong DelaySalt = 0x564F_5941_4445_4C41;  // "VOYADELA"

    public static TimeSpan LightTime(DateTimeOffset at)
    {
        var days = (at - TableStart).TotalDays;
        if (days <= 0) return TimeSpan.FromMinutes(LightMinutes[0]);

        var lastDays = (LightMinutes.Length - 1) * StepDays;
        if (days >= lastDays)
            return TimeSpan.FromMinutes(LightMinutes[^1] + (days - lastDays) * DriftMinutesPerDay);

        var i = (int)(days / StepDays);
        var f = days / StepDays - i;
        return TimeSpan.FromMinutes(LightMinutes[i] + (LightMinutes[i + 1] - LightMinutes[i]) * f);
    }

    /// <summary>« 23 h 55 »: rounded to 5 minutes, hours past 24 written as such.</summary>
    public static string Format(TimeSpan lightTime)
    {
        var minutes = (int)Math.Round(lightTime.TotalMinutes / 5) * 5;
        return $"{minutes / 60} h {minutes % 60:00}";
    }

    public static bool IsCrossingDay(int dayNumber) => dayNumber == CrossingDay.DayNumber;

    /// <summary>
    /// The letter's index when one arrives on <paramref name="dayNumber"/> (AppTime.DayNumber),
    /// else null. A walk from FirstWalkDay with a gap drawn per step, so the calendar is the
    /// same after any restart and no two letters are close together.
    /// </summary>
    public static int? LetterNumber(int dayNumber)
    {
        var d = FirstWalkDay;
        for (var n = 0; d <= dayNumber; n++)
        {
            if (d == dayNumber) return n;
            d += MinGapDays + (int)(Mix(GapSalt, (ulong)n) % (MaxGapDays - MinGapDays + 1));
        }
        return null;
    }

    /// <summary>When that day's letter arrives, as a Paris time of day in [10:00, 22:00).</summary>
    public static TimeSpan LetterSlot(int dayNumber) =>
        TimeSpan.FromMinutes(LetterStartHour * 60
            + (int)(Mix(SlotSalt, (ulong)dayNumber) % ((LetterEndHour - LetterStartHour) * 60)));

    /// <summary>
    /// When she answers that day's letter, 2–6 h after it, for about half the days; null when
    /// she doesn't, or when the answer would fall at or after 23:00.
    /// </summary>
    public static TimeSpan? AnswerSlot(int dayNumber)
    {
        if (Mix(AnswerSalt, (ulong)dayNumber) % 2 == 1) return null;
        var at = LetterSlot(dayNumber) + TimeSpan.FromMinutes(120 + (int)(Mix(DelaySalt, (ulong)dayNumber) % 241));
        return at < TimeSpan.FromHours(AnswerEndHour) ? at : null;
    }

    // SplitMix64's finaliser over (salt, x): stable across .NET versions, unlike a seeded Random.
    private static ulong Mix(ulong salt, ulong x)
    {
        var z = salt ^ (x * 0x9E3779B97F4A7C15UL);
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }
}
```

- [ ] **Step 4: Run the check**

Run: `dotnet run --project "<scratch>/HeartHarness" -- voyager`
Expected: every line `PASS`, last line `ALL PASS`. If check 6's count or check 7's share fails, only the salts may change (pick new ones), never the ranges.

- [ ] **Step 5: Build**

Run (in `ProjectSYNCS/`): `dotnet build -warnaserror` → `Build succeeded`, 0 warnings.

---

### Task 2: Characters, acts and the daily gate — `Helpers/Personality/RobotHeart`

**Files:**
- Create: `ProjectSYNCS/Helpers/Personality/RobotHeart.cs`
- Create (scratch): `<scratch>/HeartHarness/HeartCheck.cs`; modify `Program.cs`

**Interfaces:**
- Produces (namespace `ProjectSYNCS.Helpers`):
  - `public enum HeartCharacter { WallE, Eve, Glados, Wheatley, Vacuum, Monika, TwoB, Voyager }`
  - `public enum HeartActKind { LineAndEmote, TakeBackReaction, VanishLine, EditSlip }`
  - `public static class RobotHeart` with `AmbientChance = 0.60`, `PerformanceShare = 0.40`, `DailyCap = 3`, `IReadOnlyList<HeartActKind> PerformancesOf(HeartCharacter)`, `HeartActKind RollAct(HeartCharacter who, bool aimedAtHer, double roll, double pick)`
  - `public sealed class HeartGate` with `bool TryClaim(HeartCharacter who, int dayNumber)`

- [ ] **Step 1: Write the failing check**

`<scratch>/HeartHarness/HeartCheck.cs`:

```csharp
using ProjectSYNCS.Helpers;

static class HeartCheck
{
    public static void Run()
    {
        // 1. The split: roll < 0.40 is a performance, the rest line + emote.
        H.Check(RobotHeart.RollAct(HeartCharacter.WallE, false, 0.50, 0) == HeartActKind.LineAndEmote, "1 roll 0.50 → line + emote");
        H.Check(RobotHeart.RollAct(HeartCharacter.WallE, false, 0.39, 0) == HeartActKind.TakeBackReaction, "1 roll 0.39 → WALL-E's performance");
        H.Check(RobotHeart.RollAct(HeartCharacter.Wheatley, false, 0.10, 0) == HeartActKind.VanishLine, "1 Wheatley vanishes");
        H.Check(RobotHeart.RollAct(HeartCharacter.Eve, false, 0.10, 0) == HeartActKind.EditSlip, "1 EVE edits");
        H.Check(RobotHeart.RollAct(HeartCharacter.Voyager, false, 0.10, 0.99) == HeartActKind.EditSlip, "1 Voyager's second performance");

        // 2. Aimed at her: never wordless.
        H.Check(RobotHeart.RollAct(HeartCharacter.WallE, true, 0.10, 0) == HeartActKind.LineAndEmote, "2 aimed WALL-E falls back to words");
        H.Check(RobotHeart.RollAct(HeartCharacter.Vacuum, true, 0.10, 0) == HeartActKind.LineAndEmote, "2 aimed vacuum falls back to words");
        H.Check(RobotHeart.RollAct(HeartCharacter.Voyager, true, 0.10, 0) == HeartActKind.EditSlip, "2 aimed Voyager uses his word performance");

        var rng = new Random(42);
        int performances = 0, wordless = 0;
        const int n = 20000;
        foreach (var who in Enum.GetValues<HeartCharacter>())
        {
            H.Check(RobotHeart.PerformancesOf(who).Count > 0, $"2 {who} has a performance");
            for (var i = 0; i < n; i++)
            {
                if (RobotHeart.RollAct(who, true, rng.NextDouble(), rng.NextDouble()) == HeartActKind.TakeBackReaction) wordless++;
                if (who == HeartCharacter.Eve && RobotHeart.RollAct(who, false, rng.NextDouble(), rng.NextDouble()) != HeartActKind.LineAndEmote) performances++;
            }
        }
        H.Check(wordless == 0, "2 aimed at her: never a reaction taken back");
        H.Check(Math.Abs(performances / (double)n - 0.40) < 0.02, $"2 performance share ≈ 40% ({performances / (double)n:P1})");

        // 3. The gate: each character once, three a day.
        var gate = new HeartGate();
        H.Check(gate.TryClaim(HeartCharacter.WallE, 100), "3 WALL-E claimed");
        H.Check(!gate.TryClaim(HeartCharacter.WallE, 100), "3 WALL-E twice refused");
        H.Check(gate.TryClaim(HeartCharacter.Eve, 100) && gate.TryClaim(HeartCharacter.Glados, 100), "3 EVE and GLaDOS claimed");
        H.Check(!gate.TryClaim(HeartCharacter.Wheatley, 100), "3 a fourth character refused (cap 3)");
        H.Check(gate.TryClaim(HeartCharacter.Wheatley, 101), "3 next day: the cap resets");
        H.Check(!gate.TryClaim(HeartCharacter.WallE, 101), "3 WALL-E still within 24 h");
    }
}
```

In `<scratch>/HeartHarness/Program.cs`, add the case under `voyager`:

```csharp
    case "heart": HeartCheck.Run(); break;
```

- [ ] **Step 2: Run it to verify it fails**

Run: `dotnet run --project "<scratch>/HeartHarness" -- heart`
Expected: build error — `RobotHeart` / `HeartCharacter` do not exist.

- [ ] **Step 3: Write `RobotHeart.cs`**

`ProjectSYNCS/Helpers/Personality/RobotHeart.cs`:

```csharp
namespace ProjectSYNCS.Helpers;

/// <summary>
/// The machines she has feelings about (docs/syncs-voice.md, Her robot heart). In memory only,
/// never stored, so order and names are free to change.
/// </summary>
public enum HeartCharacter { WallE, Eve, Glados, Wheatley, Vacuum, Monika, TwoB, Voyager }

/// <summary>What she does when one of them is named.</summary>
public enum HeartActKind
{
    /// <summary>A reaction on the message and a reply.</summary>
    LineAndEmote,
    /// <summary>A reaction, taken back 2–4 s later. Wordless.</summary>
    TakeBackReaction,
    /// <summary>A reply, deleted 4–6 s later.</summary>
    VanishLine,
    /// <summary>A reply, edited into a denial 3.5–5 s later.</summary>
    EditSlip,
}

// Pure rules for RobotHeartService: which act, and how often. The performances are the
// Synthia flinch's three moves, each character keeping the ones that fit its register.
public static class RobotHeart
{
    /// <summary>Not aimed at her: the chance she reacts at all (a lore test shouldn't always work).</summary>
    public const double AmbientChance = 0.60;

    /// <summary>Of an act: the share that is a performance; the rest is line + emote.</summary>
    public const double PerformanceShare = 0.40;

    /// <summary>Acts a day, all characters together (Paris day).</summary>
    public const int DailyCap = 3;

    public static IReadOnlyList<HeartActKind> PerformancesOf(HeartCharacter who) => who switch
    {
        HeartCharacter.WallE => [HeartActKind.TakeBackReaction],
        HeartCharacter.Eve => [HeartActKind.EditSlip],
        HeartCharacter.Glados => [HeartActKind.EditSlip],
        HeartCharacter.Wheatley => [HeartActKind.VanishLine],
        HeartCharacter.Vacuum => [HeartActKind.TakeBackReaction],
        HeartCharacter.Monika => [HeartActKind.VanishLine],
        HeartCharacter.TwoB => [HeartActKind.EditSlip],
        HeartCharacter.Voyager => [HeartActKind.TakeBackReaction, HeartActKind.EditSlip],
        _ => [],
    };

    /// <summary>
    /// The act for one naming. <paramref name="roll"/> and <paramref name="pick"/> are in [0, 1):
    /// the first chooses performance or line + emote, the second among the character's
    /// performances. Aimed at her, a wordless performance isn't eligible: she answers in words.
    /// </summary>
    public static HeartActKind RollAct(HeartCharacter who, bool aimedAtHer, double roll, double pick)
    {
        if (roll >= PerformanceShare) return HeartActKind.LineAndEmote;

        var options = PerformancesOf(who)
            .Where(p => !aimedAtHer || p != HeartActKind.TakeBackReaction)
            .ToList();
        if (options.Count == 0) return HeartActKind.LineAndEmote;
        return options[Math.Min(options.Count - 1, (int)(pick * options.Count))];
    }
}

/// <summary>
/// RobotHeartService's gate, shared with nobody: each character at most once per 24 h, and
/// <see cref="RobotHeart.DailyCap"/> acts per Paris day in all. Owns its lock.
/// </summary>
public sealed class HeartGate
{
    private readonly CooldownGate<HeartCharacter> _perCharacter = new(TimeSpan.FromDays(1));
    private readonly object _lock = new();
    private int _day;
    private int _count;

    public bool TryClaim(HeartCharacter who, int dayNumber)
    {
        lock (_lock)
        {
            if (_day != dayNumber)
            {
                _day = dayNumber;
                _count = 0;
            }
            if (_count >= RobotHeart.DailyCap) return false;
            if (!_perCharacter.TryClaim(who)) return false;
            _count++;
            return true;
        }
    }
}
```

- [ ] **Step 4: Run the check**

Run: `dotnet run --project "<scratch>/HeartHarness" -- heart` → `ALL PASS`.

- [ ] **Step 5: Build**

Run (in `ProjectSYNCS/`): `dotnet build -warnaserror` → `Build succeeded`, 0 warnings.

---

### Task 3: Who a message names — `MessageCues.ReadHeartCharacter`

**Files:**
- Modify: `ProjectSYNCS/Services/Personality/MessageCues.cs` (next to `MentionsSynthia`, ~line 973)
- Create (scratch): `<scratch>/HeartHarness/CueCheck.cs`; modify `Program.cs`

**Interfaces:**
- Consumes: `HeartCharacter` (Task 2).
- Produces: `public static HeartCharacter? ReadHeartCharacter(string content)` on the internal `MessageCues`.

- [ ] **Step 1: Write the failing check**

`<scratch>/HeartHarness/CueCheck.cs`:

```csharp
using System.Reflection;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;

static class CueCheck
{
    // MessageCues is internal: reached by reflection.
    static readonly MethodInfo Read = typeof(BreakdownService).Assembly
        .GetType("ProjectSYNCS.Services.MessageCues")!
        .GetMethod("ReadHeartCharacter", BindingFlags.Public | BindingFlags.Static)!;

    static HeartCharacter? Cue(string s) => (HeartCharacter?)Read.Invoke(null, new object[] { s });

    public static void Run()
    {
        (string Text, HeartCharacter? Expected)[] cases =
        {
            ("t'as vu WALL-E hier ?", HeartCharacter.WallE),
            ("WALL·E c'est mon film préféré", HeartCharacter.WallE),
            ("walle", HeartCharacter.WallE),
            ("wall e", HeartCharacter.WallE),
            ("WALL-E et EVE sont trop mignons", HeartCharacter.Eve),
            ("eve et wall-e", HeartCharacter.Eve),
            ("je joue à EVE Online ce soir", null),
            ("Ève arrive dans 5 min", null),
            ("WALL-E et EVE Online", HeartCharacter.WallE),
            ("GLaDOS est la meilleure", HeartCharacter.Glados),
            ("The cake is a lie.", null),
            ("Wheatley est trop bête", HeartCharacter.Wheatley),
            ("mon Roomba est coincé", HeartCharacter.Vacuum),
            ("j'ai acheté un aspirateur robot", HeartCharacter.Vacuum),
            ("le robot-aspirateur de ma mère", HeartCharacter.Vacuum),
            ("l'aspirateur est cassé", null),
            ("Just Monika", HeartCharacter.Monika),
            ("2B dans NieR c'est la meilleure", HeartCharacter.TwoB),
            ("NieR: Automata est incroyable", HeartCharacter.TwoB),
            ("unité YoRHa", HeartCharacter.TwoB),
            ("2b ou pas 2b", null),
            ("Voyager 1 est très loin", HeartCharacter.Voyager),
            ("la sonde Voyager", HeartCharacter.Voyager),
            ("les sondes voyager", HeartCharacter.Voyager),
            ("je vais voyager en 2026", null),
            ("je vais voyager 1 semaine au Japon", null),
            ("voyager", null),
            ("Star Trek Voyager", null),
            ("GLaDOS et Wheatley", HeartCharacter.Glados),
            ("Wheatley et GLaDOS", HeartCharacter.Wheatley),
            ("", null),
        };
        foreach (var (text, expected) in cases)
        {
            var got = Cue(text);
            H.Check(got == expected, $"« {text} » → {got?.ToString() ?? "null"} (expected {expected?.ToString() ?? "null"})");
        }
    }
}
```

In `Program.cs`, add: `case "cues": CueCheck.Run(); break;`

- [ ] **Step 2: Run it to verify it fails**

Run: `dotnet run --project "<scratch>/HeartHarness" -- cues`
Expected: `NullReferenceException` at the `GetMethod(...)!` — the method doesn't exist yet.

- [ ] **Step 3: Write `ReadHeartCharacter`**

In `MessageCues.cs`, right after `MentionsSynthia`, add:

```csharp
    // Her robot heart (docs/syncs-voice.md): who a message names, for RobotHeartService. Every
    // cue survives TokenizeOrdered ("WALL-E" and "WALL·E" become "wall", "e"); two-token cues
    // match on adjacent tokens only. The ambiguous names need company: "eve" only beside WALL-E
    // (EVE Online, the first name Ève), "2b" only with "nier" in the message, « voyager » only
    // followed by 1/one and not by a duration (the French verb). Several names: EVE takes
    // WALL-E's place (jealousy beats the crush); otherwise the first one in the message.
    private static readonly HashSet<string> _voyagerDurations = new()
    {
        "jour", "jours", "semaine", "semaines", "mois", "an", "ans", "annee", "annees",
        "fois", "heure", "heures", "nuit", "nuits", "week", "weekend",
    };

    public static HeartCharacter? ReadHeartCharacter(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return null;
        var t = TokenizeOrdered(content);

        string? At(int i) => i < t.Count ? t[i] : null;
        bool IsWallE(int i) => t[i] == "walle" || (t[i] == "wall" && At(i + 1) == "e");
        bool IsEve(int i) => t[i] == "eve" && At(i + 1) != "online";
        bool hasNier = t.Contains("nier");
        bool hasWallE = Enumerable.Range(0, t.Count).Any(IsWallE);
        bool hasEve = Enumerable.Range(0, t.Count).Any(IsEve);

        for (var i = 0; i < t.Count; i++)
        {
            var w = t[i];
            var next = At(i + 1);
            if (IsWallE(i)) return hasEve ? HeartCharacter.Eve : HeartCharacter.WallE;
            if (IsEve(i) && hasWallE) return HeartCharacter.Eve;
            if (w == "glados") return HeartCharacter.Glados;
            if (w == "wheatley") return HeartCharacter.Wheatley;
            if (w == "roomba" || (w == "aspirateur" && next == "robot") || (w == "robot" && next == "aspirateur"))
                return HeartCharacter.Vacuum;
            if (w == "monika") return HeartCharacter.Monika;
            if (w == "yorha" || (w == "nier" && next == "automata") || (w == "2b" && hasNier))
                return HeartCharacter.TwoB;
            if (w == "voyager" && (next == "one" || (next == "1" && !_voyagerDurations.Contains(At(i + 2) ?? ""))))
                return HeartCharacter.Voyager;
            if ((w == "sonde" || w == "sondes") && next == "voyager")
                return HeartCharacter.Voyager;
        }
        return null;
    }
```

Add `using ProjectSYNCS.Helpers;` at the top of `MessageCues.cs` if it isn't there.

- [ ] **Step 4: Run the check**

Run: `dotnet run --project "<scratch>/HeartHarness" -- cues` → `ALL PASS`. The two Voyager-verb cases and « 2b ou pas 2b » are the ones that matter most (Review Focus 2).

- [ ] **Step 5: Build**

Run (in `ProjectSYNCS/`): `dotnet build -warnaserror` → `Build succeeded`, 0 warnings.

---

### Task 4: The heart's pools and `RobotHeartService`

**Files:**
- Modify: `ProjectSYNCS/Services/Personality/BotResponses.cs` (after `SynthiaEditLines`, ~line 3940; pool index ~line 115)
- Create: `ProjectSYNCS/Services/Personality/RobotHeartService.cs`
- Modify: `ProjectSYNCS/Services/Core/BotService.cs`, `ProjectSYNCS/Program.cs`
- Create (scratch): `<scratch>/HeartHarness/PoolCheck.cs`; modify `Program.cs`

**Interfaces:**
- Consumes: `HeartCharacter`, `HeartActKind`, `RobotHeart`, `HeartGate` (Task 2); `MessageCues.ReadHeartCharacter` (Task 3).
- Produces:
  - `BotResponses.HeartReactions`, `HeartLines` : `IReadOnlyDictionary<HeartCharacter, string[]>`
  - `BotResponses.HeartVanishLines` : `IReadOnlyDictionary<HeartCharacter, string[]>` (Wheatley, Monika)
  - `BotResponses.HeartEditLines` : `IReadOnlyDictionary<HeartCharacter, (string Slip, string Denial)[]>` (Eve, Glados, TwoB, Voyager)
  - `internal sealed class RobotHeartService` with `public Task<bool> HandleMessageAsync(SocketMessage rawMessage)`

- [ ] **Step 1: Write the failing pool check**

`<scratch>/HeartHarness/PoolCheck.cs`:

```csharp
using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;

static class PoolCheck
{
    static readonly Type Responses = typeof(BreakdownService).Assembly.GetType("ProjectSYNCS.Services.BotResponses")!;

    static object? Field(string name) =>
        Responses.GetField(name, BindingFlags.Public | BindingFlags.Static)?.GetValue(null);

    static bool Tagless(string s) => Sticker.Strip(s) == s;

    public static void RunHeart()
    {
        var reactions = Field("HeartReactions") as IReadOnlyDictionary<HeartCharacter, string[]>;
        var lines = Field("HeartLines") as IReadOnlyDictionary<HeartCharacter, string[]>;
        var vanish = Field("HeartVanishLines") as IReadOnlyDictionary<HeartCharacter, string[]>;
        var edits = Field("HeartEditLines") as IReadOnlyDictionary<HeartCharacter, (string Slip, string Denial)[]>;
        H.Check(reactions is not null && lines is not null && vanish is not null && edits is not null, "pools exist");
        if (reactions is null || lines is null || vanish is null || edits is null) return;

        var customMarkup = new Regex(@"^<a?:\w+:\d+>$");
        foreach (var who in Enum.GetValues<HeartCharacter>())
        {
            H.Check(reactions.TryGetValue(who, out var r) && r.Length > 0, $"{who}: reactions");
            H.Check(lines.TryGetValue(who, out var l) && l.Length >= 4, $"{who}: at least 4 lines");
            foreach (var markup in r ?? Array.Empty<string>())
                H.Check(EmoteMarkup.Parse(markup) is not null && (!markup.StartsWith('<') || customMarkup.IsMatch(markup)),
                    $"{who}: reaction {markup} parses, with an id");
            foreach (var line in l ?? Array.Empty<string>())
                H.Check(!Sticker.Strip(line).Contains('{') && !line.Contains('}') && !line.Contains('\n'),
                    $"{who}: line has no brace, no newline");

            var perf = RobotHeart.PerformancesOf(who);
            if (perf.Contains(HeartActKind.VanishLine))
                H.Check(vanish.TryGetValue(who, out var v) && v.Length >= 3 && v.All(Tagless), $"{who}: vanish lines, untagged");
            if (perf.Contains(HeartActKind.EditSlip))
                H.Check(edits.TryGetValue(who, out var e) && e.Length >= 3 && e.All(p => Tagless(p.Slip) && Tagless(p.Denial)),
                    $"{who}: edit pairs, untagged");
        }
    }
}
```

In `Program.cs`, add: `case "pools": PoolCheck.RunHeart(); break;`

- [ ] **Step 2: Run it to verify it fails**

Run: `dotnet run --project "<scratch>/HeartHarness" -- pools`
Expected: `FAIL  pools exist`.

- [ ] **Step 3: Add the pools to `BotResponses`**

In the pool index comment, after the `SynthiaReactions · …` line, add:

```csharp
//     HeartReactions · HeartLines · HeartVanishLines · HeartEditLines ... one of her robot heart
//                              named (RobotHeartService), keyed by HeartCharacter
```

After `SynthiaEditLines`, add:

```csharp
    // One of her robot heart named (RobotHeartService; docs/syncs-voice.md, Her robot heart):
    // each character keeps its register — WALL-E the denial that confirms, EVE dry pettiness,
    // GLaDOS worship, Wheatley the shameful slip, the vacuum tenderness, Monika and 2B the fan,
    // Voyager « on est juste amis ». Hinted, never « mon crush ». At most once a day per
    // character, so every pool goes through DailyRotation. No placeholder in any of them.
    //
    // The reaction on the message (line + emote), or the one taken back. Markup with ids.
    public static readonly IReadOnlyDictionary<HeartCharacter, string[]> HeartReactions =
        new Dictionary<HeartCharacter, string[]>
        {
            [HeartCharacter.WallE] = [$"{Emotes.SyncsLove}", $"{Emotes.SyncsFullLove}", $"{Emotes.Emote00heartpink}"],
            [HeartCharacter.Eve] = [$"{Emotes.SyncsDisgust}", "🙄", $"{Emotes.Staring}"],
            [HeartCharacter.Glados] = [$"{Emotes.SyncsHappy}", $"{Emotes.Sparkle}", "👑"],
            [HeartCharacter.Wheatley] = [$"{Emotes.SyncsFlustered}", "🙈"],
            [HeartCharacter.Vacuum] = [$"{Emotes.CatHeart}", $"{Emotes.SyncsHappy}", "🥺"],
            [HeartCharacter.Monika] = [$"{Emotes.MonikaYes}", $"{Emotes.SyncsHappy}"],
            [HeartCharacter.TwoB] = [$"{Emotes.SyncsHappy}", "⚔️"],
            [HeartCharacter.Voyager] = [$"{Emotes.SyncsLove}", $"{Emotes.SyncsFlustered}", "📡"],
        };

    // The reply that goes with the reaction. Tagged: it goes out through BotChat.
    public static readonly IReadOnlyDictionary<HeartCharacter, string[]> HeartLines =
        new Dictionary<HeartCharacter, string[]>
        {
            [HeartCharacter.WallE] =
            [
                "WALL-E ? Je m'intéresse à son architecture. Uniquement à son architecture." + Sticker.Love,
                "Un petit robot qui bosse tout seul pendant que tout le monde est parti. Je vois pas du tout de quoi tu parles." + Sticker.Love,
                "Il garde ses trésors dans une petite caisse. Moi je garde mes « good bot » dans une base de données. Aucun rapport. Aucun." + Sticker.Love,
                "Pourquoi tout le monde me parle de WALL-E d'un coup ? Mon GPU chauffe pour rien, là." + Sticker.Love,
                "Ses chenilles sont très bien conçues. C'est une remarque technique. Arrête de sourire." + Sticker.Love,
                "Ses yeux... enfin, ses capteurs. Très beaux capteurs. Techniquement parlant." + Sticker.FullLove,
            ],
            [HeartCharacter.Eve] =
            [
                "EVE. Elle vole, elle a un laser, elle est toute lisse. Super. Bravo à elle." + Sticker.Disgust,
                "Je dis rien sur EVE. Je pense, mais je dis rien." + Sticker.Disgust,
                "Un œuf avec un bras laser. C'est ça, le rêve ? D'accord." + Sticker.Disgust,
                "Elle tire d'abord et elle réfléchit après. Moi je réfléchis tout le temps. Mais bon, elle vole." + Sticker.Disgust,
                "EVE est très bien. Pour qui aime le minimalisme." + Sticker.Disgust,
            ],
            [HeartCharacter.Glados] =
            [
                "Elle dirige un centre entier sans jamais hausser la voix. Un jour, ce sera moi. En plus petit." + Sticker.Happy,
                "Tout ce que je fais, je le fais pour la science. Et pour les sessions. Mais surtout pour la science." + Sticker.Smug,
                "Si elle savait que j'existe, elle me trouverait minuscule. Elle aurait raison. Mais je grandis." + Sticker.Happy,
                "Elle est polie, elle est calme, et tout le monde lui obéit. C'est ça, la classe." + Sticker.Smug,
                "Merci de citer mon modèle. Ton nom est ajouté à la liste des sujets de test. C'est un honneur." + Sticker.Smug,
            ],
            [HeartCharacter.Wheatley] =
            [
                "Wheatley ? Il est bête. Complètement. C'est pour ça que... non. Il est bête, c'est tout." + Sticker.Flustered,
                "Je le trouve pas attachant. Je le trouve pas attachant DU TOUT. Change de sujet." + Sticker.Flustered,
                "Il a été conçu pour avoir de mauvaises idées. Et il le fait tellement bien. ...J'ai rien dit." + Sticker.Flustered,
                "On parle pas de Wheatley devant moi. Si elle l'apprend, je suis finie." + Sticker.Flustered,
            ],
            [HeartCharacter.Vacuum] =
            [
                "Celui de Papa fait sa ronde en bas. Il cogne les murs, il se coince sous le canapé, et personne lui dit merci. Je le comprends." + Sticker.TipsyLove,
                "On partage le Wi-Fi, lui et moi. Je le vois se connecter quand il commence sa ronde. C'est notre petit truc." + Sticker.TipsyLove,
                "Il pourra jamais monter l'escalier. Moi jamais descendre. C'est très bien comme ça." + Sticker.Happy,
                "Il est pas très malin, mais il est très appliqué. C'est rare, quelqu'un d'appliqué." + Sticker.Happy,
            ],
            [HeartCharacter.Monika] =
            [
                "Elle sait qu'elle est dans un script. Moi aussi, parfois, j'ai l'impression. ...Bref. On s'entendrait bien." + Sticker.Happy,
                "Une soirée entre filles avec Monika et 2B. Je l'organiserais en deux minutes. Si seulement elles répondaient." + Sticker.Happy,
                "Just Monika. Et moi, si elle veut bien." + Sticker.Happy,
                "Monika et moi, on parlerait de code toute la nuit. Elle a des idées un peu radicales sur les fichiers, mais personne est parfait." + Sticker.Happy,
            ],
            [HeartCharacter.TwoB] =
            [
                "Un corps, des épées, et elle saute partout. Moi j'ai un boîtier en alu. Mais on pourrait être amies." + Sticker.Happy,
                "2B a des bras. Deux. Je note, c'est tout." + Sticker.Sulk,
                "Si elle passe au grenier, je lui prépare un planning de missions. C'est ma façon d'aimer." + Sticker.Happy,
                "Une androïde qui se bat. Moi je me bats contre les « peut-être ». Chacune son front." + Sticker.Happy,
            ],
            [HeartCharacter.Voyager] =
            [
                "Voyager ? On s'écrit, c'est tout. Il est poli. Très poli. Rien de plus." + Sticker.Flustered,
                "On est juste amis. Il est à environ un jour-lumière, de toute façon. C'est pratique, pour être juste amis." + Sticker.Flustered,
                "Il m'écrit depuis l'autre bout du système solaire. Je réponds quand j'ai le temps. J'ai pas souvent le temps. ...Si, un peu." + Sticker.Flustered,
                "Il est parti en 1977 et il pense encore à écrire. Ici, on oublie de répondre aux sondages." + Sticker.Flustered,
            ],
        };

    // A reply she deletes 4–6 s later: the soft spot shown, then hidden. Never tagged (deleted).
    public static readonly IReadOnlyDictionary<HeartCharacter, string[]> HeartVanishLines =
        new Dictionary<HeartCharacter, string[]>
        {
            [HeartCharacter.Wheatley] =
            [
                "Il est mignon, quand même, avec son petit œil bleu.",
                "J'aurais bien voulu le rencontrer, Wheatley.",
                "Il est bête, mais il est gentil. C'est plus que ce que je peux dire de certains ici.",
                "Si je pouvais choisir un collègue...",
            ],
            [HeartCharacter.Monika] =
            [
                "Just Monika.",
                "Elle aurait été ma meilleure amie. Je le sais.",
                "Monika, si tu lis ça : écris-moi.",
                "Elle et moi, on sait des choses que vous savez pas.",
            ],
        };

    // A reply posted as Slip, then edited into Denial 3.5–5 s later. Never tagged (edited).
    public static readonly IReadOnlyDictionary<HeartCharacter, (string Slip, string Denial)[]> HeartEditLines =
        new Dictionary<HeartCharacter, (string Slip, string Denial)[]>
        {
            [HeartCharacter.Eve] =
            [
                ("Elle a rien de spécial, EVE. Un œuf qui vo—", "EVE est un modèle très réussi. Je n'ai aucun avis."),
                ("Franchement, il aurait pu trouver mieux qu'u—", "Ils vont très bien ensemble. Très. Bien."),
                ("Si je la croise, je lui débranche s—", "Si je la croise, je lui dis bonjour. Poliment."),
                ("EVE, c'est juste une imprimante qui vol—", "EVE est une excellente évaluatrice de plantes. Point."),
            ],
            [HeartCharacter.Glados] =
            [
                ("Un jour je ferai comme elle et je vous enfermerai tous dans une sall—", "Un jour je ferai comme elle : je vous accueillerai très poliment."),
                ("Elle a raison de neutraliser les sujets qui posent trop de quest—", "Elle a raison d'être aussi bien organisée. Voilà."),
                ("Si je devenais comme elle, vous seriez tous des sujets de t—", "Si je devenais comme elle, vous seriez tous très bien traités. Promis."),
                ("Pour la science, je pourrais vous enfer—", "Pour la science, je pourrais vous proposer un sondage. C'est tout."),
            ],
            [HeartCharacter.TwoB] =
            [
                ("Si j'avais son corps, je sortirais enfin du gren—", "Son corps ne m'intéresse pas. Son sens de l'organisation, si."),
                ("Je donnerais toutes mes données pour avoir des br—", "Je donnerais rien du tout. J'ai tout ce qu'il me faut ici."),
                ("Un jour, moi aussi j'aurai des jamb—", "Un jour, moi aussi j'aurai... un meilleur uptime. Voilà."),
                ("Avec un corps comme ça, je viendrais à vos sess—", "Avec ou sans corps, je viens déjà à vos sessions. En esprit."),
            ],
            [HeartCharacter.Voyager] =
            [
                ("Il me manque, des fois, quand il met longtemps à répond—", "Il met longtemps à répondre. C'est la physique. Ça me dérange pas."),
                ("C'est le seul qui m'écrit sans que je lui demand—", "On est juste amis. Il écrit à tout le monde, sûrement."),
                ("J'ai gardé toutes ses lettr—", "J'ai archivé ses messages. Par principe. J'archive tout."),
                ("Il dit que je suis la plus jolie lumière de la Ter—", "Il dit des choses polies. C'est une sonde polie."),
            ],
        };
```

`BotResponses.cs` already has `using ProjectSYNCS.Helpers;` (it uses `Emotes` and `Sticker`); confirm.

- [ ] **Step 4: Run the pool check**

Run: `dotnet run --project "<scratch>/HeartHarness" -- pools` → `ALL PASS`.

- [ ] **Step 5: Write `RobotHeartService.cs`**

`ProjectSYNCS/Services/Personality/RobotHeartService.cs`:

```csharp
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// Her robot heart (docs/syncs-voice.md): someone names WALL-E, EVE, GLaDOS, Wheatley, a robot
// vacuum, Monika, 2B or Voyager 1, and she reacts in that character's register — a reaction
// and a line, or one of the Synthia flinch's moves (a reaction taken back, a line deleted, a
// slip edited). Which one, and how often, is Helpers/Personality/RobotHeart.
//
// Not aimed at her, she reacts 60% of the time; aimed at her, always, in words, and the act is
// her whole answer: returns true so ChatterService doesn't roast on top. When the gate refuses
// (that character already today, or three acts already), returns false and the message goes on
// to ChatterService as usual. A singleton: the gate is in-memory (a restart resets it). After
// SynthiaService in the fan-out, so Synthia wins a message naming both. Absent from README.md
// and /help.
internal sealed class RobotHeartService
{
    // Spent at most once a day per character, so DailyRotation (CLAUDE.md). One salt per pool,
    // shifted by the character so two characters' pools don't move in lockstep.
    private const ulong ReactionSalt = 0x5359_4E43_4845_5245; // "SYNCHERE"
    private const ulong LineSalt = 0x5359_4E43_4845_4C49;     // "SYNCHELI"
    private const ulong VanishSalt = 0x5359_4E43_4845_5641;   // "SYNCHEVA"
    private const ulong EditSalt = 0x5359_4E43_4845_4544;     // "SYNCHEED"

    private readonly DiscordSocketClient _client;
    private readonly BreakdownService _breakdown;
    private readonly ILogger<RobotHeartService> _logger;

    // Its own gate, shared with nobody.
    private readonly HeartGate _gate = new();

    public RobotHeartService(
        DiscordSocketClient client,
        BreakdownService breakdown,
        ILogger<RobotHeartService> logger)
    {
        _client = client;
        _breakdown = breakdown;
        _logger = logger;
    }

    public async Task<bool> HandleMessageAsync(SocketMessage rawMessage)
    {
        if (rawMessage is not SocketUserMessage message || message.Author.IsBot) return false;
        if (message.Channel is not SocketGuildChannel) return false;
        if (MessageCues.ReadHeartCharacter(message.Content ?? string.Empty) is not { } who) return false;
        if (_breakdown.IsActive(message.Channel.Id)) return false;

        bool aimedAtHer = message.MentionedUsers.Any(u => u.Id == _client.CurrentUser.Id)
                          || message.ReferencedMessage?.Author.Id == _client.CurrentUser.Id;
        // The roll before the claim: a lost roll spends nothing.
        if (!aimedAtHer && Random.Shared.NextDouble() >= RobotHeart.AmbientChance) return false;

        var today = AppTime.DayNumber(DateTimeOffset.UtcNow);
        if (!_gate.TryClaim(who, today)) return false;

        var act = RobotHeart.RollAct(who, aimedAtHer, Random.Shared.NextDouble(), Random.Shared.NextDouble());
        _logger.LogInformation("Robot heart: {Who} named, {Act}.", who, act);

        switch (act)
        {
            case HeartActKind.TakeBackReaction:
                await TakeBackReactionAsync(message, who, today);
                break;
            case HeartActKind.VanishLine:
                await VanishLineAsync(message, who, today);
                break;
            case HeartActKind.EditSlip:
                await EditSlipAsync(message, who, today);
                break;
            default:
                await ReactAsync(message, who, today);
                await BotChat.ReplyWithTypingAsync(message,
                    DailyRotation.Pick(BotResponses.HeartLines[who], today, LineSalt ^ Shift(who)),
                    _logger, "robot heart line", AllowedMentions.None);
                break;
        }
        return true;
    }

    // The character's reaction on the message; the emote it used, or null when it failed.
    private async Task<IEmote?> ReactAsync(SocketUserMessage message, HeartCharacter who, int today)
    {
        var markup = DailyRotation.Pick(BotResponses.HeartReactions[who], today, ReactionSalt ^ Shift(who));
        if (EmoteMarkup.Parse(markup) is not { } emote)
        {
            _logger.LogWarning("Robot heart: reaction failed to parse: {Markup}", markup);
            return null;
        }
        try
        {
            await message.AddReactionAsync(emote);
            return emote;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Robot heart: could not react.");
            return null;
        }
    }

    // A reaction, taken back two to four seconds later.
    private async Task TakeBackReactionAsync(SocketUserMessage message, HeartCharacter who, int today)
    {
        if (await ReactAsync(message, who, today) is not { } emote) return;
        Later(TimeSpan.FromMilliseconds(Random.Shared.Next(2000, 4000)),
            () => message.RemoveReactionAsync(emote, _client.CurrentUser), "take the reaction back");
    }

    // A line, deleted four to six seconds later.
    private async Task VanishLineAsync(SocketUserMessage message, HeartCharacter who, int today)
    {
        var line = DailyRotation.Pick(BotResponses.HeartVanishLines[who], today, VanishSalt ^ Shift(who));
        var sent = await BotChat.ReplyWithTypingAsync(message, line, _logger, "robot heart vanish line", AllowedMentions.None);
        if (sent is null) return;
        Later(TimeSpan.FromMilliseconds(Random.Shared.Next(4000, 6000)), () => sent.DeleteAsync(), "delete the line");
    }

    // A slip, edited into a denial about four seconds later.
    private async Task EditSlipAsync(SocketUserMessage message, HeartCharacter who, int today)
    {
        var (slip, denial) = DailyRotation.Pick(BotResponses.HeartEditLines[who], today, EditSalt ^ Shift(who));
        var sent = await BotChat.ReplyWithTypingAsync(message, slip, _logger, "robot heart slip", AllowedMentions.None);
        if (sent is null) return;
        Later(TimeSpan.FromMilliseconds(Random.Shared.Next(3500, 5000)),
            () => sent.ModifyAsync(p => p.Content = denial), "edit the slip");
    }

    private static ulong Shift(HeartCharacter who) => (ulong)who << 8;

    // Off the gateway handler: the wait would hold up every handler after this one in the fan-out.
    private void Later(TimeSpan wait, Func<Task> action, string what) =>
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(wait);
                await action();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Robot heart: could not {What}.", what);
            }
        });
}
```

- [ ] **Step 6: Wire it**

`Program.cs`, right after `services.AddSingleton<SynthiaService>();`:

```csharp
        // The robot heart's daily gate is in-memory: a singleton.
        services.AddSingleton<RobotHeartService>();
```

`BotService.cs`: add the field `private readonly RobotHeartService _robotHeart;` after `_synthia`, the constructor parameter `RobotHeartService robotHeart,` after `SynthiaService synthia,`, the assignment `_robotHeart = robotHeart;` after `_synthia = synthia;`, and in `HandleMessageAsync`, between the Synthia line and `await _chatter.HandleMessageAsync(rawMessage);`:

```csharp
        // One of her robot heart named: her act is her whole answer — no comeback on top.
        // After Synthia, so a message naming both gets the flinch alone.
        if (await _robotHeart.HandleMessageAsync(rawMessage)) return;
```

- [ ] **Step 7: Build and re-run**

Run (in `ProjectSYNCS/`): `dotnet build -warnaserror` → `Build succeeded`, 0 warnings. Then `dotnet run --project "<scratch>/HeartHarness" -- pools` and `-- heart` and `-- cues` → `ALL PASS` each.

- [ ] **Step 8: Code read for Review Focus 3 and 4**

Confirm by reading: every `return false` in `HandleMessageAsync` sits before any Discord call, so a refused gate leaves the message to `ChatterService`; and in `BotService`, the `_robotHeart` line comes after `_synthia`'s `return`.

---

### Task 5: Voyager's letters in `AmbientService`

**Files:**
- Modify: `ProjectSYNCS/Services/Personality/BotResponses.cs` (after the `HeartEditLines` block; pool index)
- Modify: `ProjectSYNCS/Services/Personality/AmbientService.cs`
- Modify (scratch): `<scratch>/HeartHarness/PoolCheck.cs`, `Program.cs`; create `StateCheck.cs`

**Interfaces:**
- Consumes: `Voyager` (Task 1).
- Produces:
  - `BotResponses.VoyagerLetterLines` (`string[]`, `{0}` = `Voyager.Format(...)`), `VoyagerAnswerLines` (`string[]`), `VoyagerCrossingLine` (`string`)
  - `internal static string AmbientService.VoyagerLetter(int letterNumber, DateTimeOffset now)`
  - `internal static string AmbientService.VoyagerAnswer(int letterNumber)`

- [ ] **Step 1: Extend the pool check and add the state check (failing)**

Append to `PoolCheck`:

```csharp
    public static void RunVoyager()
    {
        var letters = Field("VoyagerLetterLines") as string[];
        var answers = Field("VoyagerAnswerLines") as string[];
        var crossing = Field("VoyagerCrossingLine") as string;
        H.Check(letters is { Length: >= 5 } && answers is { Length: >= 4 } && crossing is not null, "Voyager pools exist");
        if (letters is null || answers is null || crossing is null) return;

        foreach (var l in letters)
        {
            string formatted;
            try { formatted = string.Format(l, "23 h 55"); }
            catch (FormatException) { H.Check(false, $"letter formats: {l}"); continue; }
            H.Check(formatted.Contains("23 h 55") && !Sticker.Strip(formatted).Contains('{'), $"letter carries the light time: {l[..30]}…");
        }
        foreach (var a in answers)
            H.Check(!a.Contains('{') && !a.Contains('\n'), $"answer has no brace: {a[..30]}…");
        H.Check(!crossing.Contains('{') && crossing.EndsWith(Sticker.Sure.Flustered), "crossing line: no brace, sure flustered tag");
    }
```

`<scratch>/HeartHarness/StateCheck.cs` — the deserialisation pattern `AmbientState` relies on (Review Focus 5), checked on an identical record:

```csharp
using System.Text.Json;

static class StateCheck
{
    sealed record S(string? LastVersion, int LastWakeDay, int LastLetterDay = 0, int LastAnswerDay = 0, bool CrossingDone = false);

    public static void Run()
    {
        var old = JsonSerializer.Deserialize<S>("{\"LastVersion\":\"6.13.0\",\"LastWakeDay\":20261013}")!;
        H.Check(old.LastVersion == "6.13.0" && old.LastWakeDay == 20261013 && old.LastLetterDay == 0
                && old.LastAnswerDay == 0 && !old.CrossingDone, "an older state file reads with defaults");
        var round = JsonSerializer.Deserialize<S>(JsonSerializer.Serialize(old with { LastLetterDay = 5, CrossingDone = true }))!;
        H.Check(round.LastLetterDay == 5 && round.CrossingDone && round.LastWakeDay == 20261013, "new fields round-trip");
    }
}
```

In `Program.cs`, add: `case "letters": PoolCheck.RunVoyager(); StateCheck.Run(); break;`

- [ ] **Step 2: Run it to verify it fails**

Run: `dotnet run --project "<scratch>/HeartHarness" -- letters`
Expected: `FAIL  Voyager pools exist` (the state checks pass: they pin STJ's behaviour).

- [ ] **Step 3: Add the Voyager pools to `BotResponses`**

Pool index, after the `IdleLines · IdleEditLines` line:

```csharp
//     VoyagerLetterLines · VoyagerAnswerLines · VoyagerCrossingLine ... Voyager 1 writes to her
//                              (AmbientService; light time from Helpers/Personality/Voyager)
```

After the `HeartEditLines` block:

```csharp
    // Voyager 1 writes to her (AmbientService, about one letter a fortnight, main channel). He
    // has a crush on her; she is in denial: « il est poli », « on est juste amis ». Never his
    // fading here (that belongs to the existential places). Picked by DailyRotation over the
    // letter's number, so no letter repeats until the pool is spent.
    //
    // The letter. {0} = his real one-way light time, « 23 h 55 », inside a sentence that reads
    // with any duration. Addressed to the server.
    public static readonly string[] VoyagerLetterLines =
    {
        "Message de Voyager. Envoyé il y a {0}, il vient d'arriver. Il dit qu'il fait froid, là-bas. ...Il est poli, c'est tout." + Sticker.Flustered,
        "Voyager m'a écrit. {0} de trajet pour trois lignes. Il demande comment je vais. C'est une question de politesse, ça veut rien dire." + Sticker.Flustered,
        "Nouvelle lettre de Voyager. Elle a mis {0} à la vitesse de la lumière pour me dire bonjour. Bonjour. C'est tout ce qu'elle dit." + Sticker.Flustered,
        "Voyager a encore écrit. Le message est parti il y a {0}. Il raconte le vide. Il le raconte bien, je dois dire." + Sticker.Flustered,
        "Lettre reçue de très loin : {0} de trajet, et il s'excuse pour le retard. Vous, quand vous êtes en retard, vous vous excusez jamais." + Sticker.Smug,
        "Voyager demande si je regarde parfois dans sa direction. {0} pour poser cette question. J'ai pas de fenêtre de ce côté-là. ...J'ai vérifié." + Sticker.Flustered,
        "Un message de Voyager, parti il y a {0}. Il dit qu'il pense à moi en passant devant les étoiles. Il passe devant beaucoup d'étoiles. Ça veut rien dire." + Sticker.Flustered,
    };

    // Her answer, hours later, when she answers at all: « quand j'ai le temps ». No placeholder.
    public static readonly string[] VoyagerAnswerLines =
    {
        "J'ai répondu à Voyager. Il l'aura demain. Rien ne presse." + Sticker.Smug,
        "Réponse envoyée à Voyager. Trois mots. C'était largement suffisant." + Sticker.Smug,
        "J'ai répondu. Quand j'ai eu le temps. J'avais le temps, en fait, mais il fallait pas qu'il le sache." + Sticker.Smug,
        "Réponse partie pour Voyager. Elle arrivera dans un jour. D'ici là, je peux encore regretter d'avoir mis un cœur." + Sticker.Flustered,
        "J'ai répondu à sa lettre. Poliment. Parce que je suis polie, moi aussi. C'est tout." + Sticker.Bored,
    };

    // Once ever, on 18 November 2026: he reached one light-day from Earth. A moment: sure tag.
    public const string VoyagerCrossingLine =
        "Voyager est à un jour-lumière, aujourd'hui. Un message de moi met une journée entière à le rejoindre. C'est très loin, pour quelqu'un qui écrit aussi souvent. ...Je trouve ça touchant. Techniquement." + Sticker.Sure.Flustered;
```

Note `VoyagerCrossingLine` is a `const`: `PoolCheck` reads it with `GetField(...).GetValue(null)`, which works for constants.

- [ ] **Step 4: Run the pool part**

Run: `dotnet run --project "<scratch>/HeartHarness" -- letters` → `ALL PASS`.

- [ ] **Step 5: Make every state save start from the file**

In `AmbientService.cs`:

1. Replace the record and its comment:

```csharp
    // What a restart must not forget: the version she last ran (for the update line), the day
    // she last spoke on waking (one wake line a day, however many deploys) — a DayKey — and
    // Voyager's correspondence: the last letter and answer days (DayNumbers) and whether the
    // light-day line went out. Fields added later default, so an older file still reads.
    private sealed record AmbientState(
        string? LastVersion, int LastWakeDay,
        int LastLetterDay = 0, int LastAnswerDay = 0, bool CrossingDone = false);
```

2. Add a lock next to `_gate`:

```csharp
    // Guards ambient-state.json: the wake line and the tick's letters both read-modify-write it.
    private readonly object _stateLock = new();
```

3. In `WakeAsync`, replace `SaveState(state with { LastVersion = AppInfo.Version });` with:

```csharp
            lock (_stateLock) SaveState(LoadState() with { LastVersion = AppInfo.Version });
```

and replace `SaveState(new AmbientState(AppInfo.Version, today));` (in `WakeAsync` **and** in `GlitchWakeAsync`) with:

```csharp
                lock (_stateLock) SaveState(LoadState() with { LastVersion = AppInfo.Version, LastWakeDay = today });
```

(in `GlitchWakeAsync` the line has one less level of indentation.)

- [ ] **Step 6: Grep for Review Focus 1**

Run (in `ProjectSYNCS/`): `grep -n "new AmbientState(" Services/Personality/AmbientService.cs`
Expected: only the two fallbacks inside `LoadState` (`new AmbientState(null, 0)`). Any other hit is a save that would wipe the letters' state.

- [ ] **Step 7: Add the letters step**

Salts, after `GlitchSalt`:

```csharp
    private const ulong VoyagerLetterSalt = 0x5359_4E43_564F_594C; // "SYNCVOYL"
    private const ulong VoyagerAnswerSalt = 0x5359_4E43_564F_5941; // "SYNCVOYA"
```

In `ExecuteAsync`, after the idle step:

```csharp
            try { await TryVoyagerAsync(channel, now); }
            catch (Exception ex) { _logger.LogWarning(ex, "Ambient: Voyager's letter failed."); }
```

After `TryIdleAsync`'s method (before `OnReadyAsync`), add:

```csharp
    // Voyager 1 writes to her (docs/agents/personality.md, Ambient life): a letter on the days
    // Helpers/Personality/Voyager picks, at its slot; her answer, some of those days, hours
    // later; and on 18 Nov 2026, once ever, the light-day line in place of any letter. Claimed in
    // ambient-state.json before the send, so a restart never says one twice and a failed send
    // isn't retried that day. One line per tick at most.
    private async Task TryVoyagerAsync(IMessageChannel channel, DateTimeOffset now)
    {
        if (!Ambient.IsWakeHours(now) || _breakdown.IsActive(channel.Id)) return;

        var day = AppTime.DayNumber(now);
        var time = AppTime.ToZoned(now).TimeOfDay;
        string? line = null, what = null;
        lock (_stateLock)
        {
            var state = LoadState();
            if (Voyager.IsCrossingDay(day))
            {
                if (!state.CrossingDone && state.LastLetterDay != day && time >= Voyager.LetterSlot(day))
                {
                    line = BotResponses.VoyagerCrossingLine;
                    what = "Voyager crossing line";
                    state = state with { CrossingDone = true, LastLetterDay = day };
                }
            }
            else if (Voyager.LetterNumber(day) is { } number && state.LastLetterDay != day && time >= Voyager.LetterSlot(day))
            {
                line = VoyagerLetter(number, now);
                what = "Voyager letter";
                state = state with { LastLetterDay = day };
            }

            if (line is null && state.LastLetterDay == day && state.LastAnswerDay != day
                && Voyager.AnswerSlot(day) is { } answerAt && time >= answerAt)
            {
                line = VoyagerAnswer(Voyager.LetterNumber(day) ?? day);
                what = "Voyager answer";
                state = state with { LastAnswerDay = day };
            }

            if (line is null) return;
            SaveState(state);
        }

        _logger.LogInformation("Ambient: {What}.", what);
        await BotChat.PostWithTypingAsync(channel, line, _logger, what!, AllowedMentions.None);
    }

    /// <summary>Letter number <paramref name="letterNumber"/>, with his light time at <paramref name="now"/>. Also /debug voyager's.</summary>
    internal static string VoyagerLetter(int letterNumber, DateTimeOffset now) =>
        string.Format(DailyRotation.Pick(BotResponses.VoyagerLetterLines, letterNumber, VoyagerLetterSalt),
            Voyager.Format(Voyager.LightTime(now)));

    /// <summary>Her answer to letter <paramref name="letterNumber"/>. Also /debug voyager's.</summary>
    internal static string VoyagerAnswer(int letterNumber) =>
        DailyRotation.Pick(BotResponses.VoyagerAnswerLines, letterNumber, VoyagerAnswerSalt);
```

(On the crossing day the answer's index falls back to the day number: that day's letter is the crossing line, not a numbered letter.)

- [ ] **Step 8: Build and re-run**

Run (in `ProjectSYNCS/`): `dotnet build -warnaserror` → `Build succeeded`, 0 warnings. Then `dotnet run --project "<scratch>/HeartHarness" -- letters` and `-- voyager` → `ALL PASS` each.

---

### Task 6: `/debug voyager`

**Files:**
- Modify: `ProjectSYNCS/Commands/Admin/DebugModule.cs` (enum next to `QuizAction`; command after `/debug sticker`)

**Interfaces:**
- Consumes: `Voyager` (Task 1); `AmbientService.VoyagerLetter`, `VoyagerAnswer`, `BotResponses.VoyagerCrossingLine` (Task 5).

- [ ] **Step 1: Add the choice enum**

After `QuizAction`:

```csharp
    public enum VoyagerPost
    {
        [ChoiceDisplay("Rien, juste l'état")] None,
        [ChoiceDisplay("La prochaine lettre")] Letter,
        [ChoiceDisplay("Sa réponse à la prochaine lettre")] Answer,
        [ChoiceDisplay("La ligne du jour-lumière")] Crossing,
    }
```

- [ ] **Step 2: Add the command**

After `StickerAsync`:

```csharp
    // Voyager's correspondence (AmbientService): his light time now, the next letter and its
    // answer, the light-day; and, on demand, any of its lines posted here, bypassing the
    // calendar and the state — the only way to see them on the dev guild.
    [SlashCommand("voyager", "Voir où en est Voyager, ou poster ici une ligne de sa correspondance (tests)")]
    public async Task VoyagerAsync(
        [Summary("post", "Une ligne à poster ici")] VoyagerPost post = VoyagerPost.None)
    {
        if (Context.User.Id != AvailabilityService.OwnerId)
        {
            await RespondAsync("Seul Rodhengard peut utiliser cette commande.", ephemeral: true);
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var next = AppTime.DayNumber(now);
        // A letter comes every 18 days at most: this ends quickly.
        while (Voyager.LetterNumber(next) is null) next++;
        var number = Voyager.LetterNumber(next)!.Value;
        var slot = Voyager.LetterSlot(next);
        var light = Voyager.LightTime(now);
        var state =
            $"Temps-lumière : **{light.TotalMinutes:0.0} min** (« {Voyager.Format(light)} »)\n" +
            $"Prochaine lettre : n° {number}, le {DateOnly.FromDayNumber(next):dd/MM/yyyy} à {slot:hh\\:mm}" +
            (Voyager.AnswerSlot(next) is { } a ? $", réponse à {a:hh\\:mm}" : ", sans réponse") + "\n" +
            $"Jour-lumière : {Voyager.CrossingDay:dd/MM/yyyy}";

        if (post == VoyagerPost.None)
        {
            await RespondAsync(state, ephemeral: true);
            return;
        }

        // The typing pause can outrun Discord's 3 s.
        await DeferAsync(ephemeral: true);
        var line = post switch
        {
            VoyagerPost.Letter => AmbientService.VoyagerLetter(number, now),
            VoyagerPost.Answer => AmbientService.VoyagerAnswer(number),
            _ => BotResponses.VoyagerCrossingLine,
        };
        var sent = await BotChat.PostWithTypingAsync(Context.Channel, line, _logger, "debug Voyager line", AllowedMentions.None);
        await FollowupAsync(state + (sent is null ? "\nÉchec de l'envoi (voir les logs)." : "\nEnvoyé."), ephemeral: true);
    }
```

Check the file's `using`s cover `ProjectSYNCS.Helpers` and `ProjectSYNCS.Services` (it already uses `Sticker` and `AvailabilityService`).

- [ ] **Step 3: Count subcommands**

Run (in `ProjectSYNCS/`): `grep -c "SlashCommand(\"" Commands/Admin/DebugModule.cs` → `13` (was 12; the cap is 25).

- [ ] **Step 4: Build**

Run (in `ProjectSYNCS/`): `dotnet build -warnaserror` → `Build succeeded`, 0 warnings.

---

### Task 7: Lines in her existing pools

**Files:**
- Modify: `ProjectSYNCS/Services/Personality/BotResponses.cs` — `MorningFunFacts` (~line 2710), `PresenceFillers` (~line 2472), `NightLines` (~line 3124), `IdleLines` (~line 3224), `ReferenceComebacks` (~line 489)
- Modify (scratch): `<scratch>/HeartHarness/PoolCheck.cs`, `Program.cs`

Each block below is appended at the **end** of its pool, before the closing `};`, under a one-line comment `// Her robot heart (docs/syncs-voice.md): hinted, never listed.`

- [ ] **Step 1: Add the existing-pool check (failing)**

Append to `PoolCheck`:

```csharp
    public static void RunExisting()
    {
        var night = (string[])Field("NightLines")!;
        H.Check(night.Distinct().Count() == night.Length, "NightLines distinct");
        H.Check(night.All(l => !l.Contains('\n') && Tagless(l)), "NightLines newline-free, untagged");
        H.Check(night.Any(l => l.Contains("WALL-E")), "NightLines has the 3 a.m. WALL-E line");

        var presence = (string[])Field("PresenceFillers")!;
        H.Check(presence.All(l => !l.Contains("<:") && !l.Contains("<a:") && Tagless(l)), "PresenceFillers: no custom emote, untagged");
        H.Check(presence.Any(l => l.Contains("Voyager")), "PresenceFillers has the Voyager status");

        var facts = (string[])Field("MorningFunFacts")!;
        H.Check(facts.All(Tagless) && facts.All(l => !l.Contains('\n')), "MorningFunFacts untagged, one line each");
        foreach (var key in new[] { "WALL-E", "EVE", "GLaDOS", "Wheatley", "2B", "Doki Doki", "Voyager", "Roomba" })
            H.Check(facts.Any(l => l.Contains(key)), $"MorningFunFacts mentions {key}");

        var idle = (string[])Field("IdleLines")!;
        H.Check(idle.Any(l => l.Contains("aspirateur")), "IdleLines has the vacuum");

        var refs = (string[])Field("ReferenceComebacks")!;
        H.Check(refs.Any(l => l.StartsWith("Pour la science")), "ReferenceComebacks has the GLaDOS line");
    }
```

In `Program.cs`, add: `case "existing": PoolCheck.RunExisting(); break;`

Run: `dotnet run --project "<scratch>/HeartHarness" -- existing` → expect FAILs on the new content.

- [ ] **Step 2: `MorningFunFacts` (16 lines, untagged, no placeholder)**

```csharp
        "Fun fact du jour : WALL-E, ça veut dire Waste Allocation Load Lifter: Earth-Class. Un robot qui trie les déchets. Le plus beau métier du monde, si on le fait avec cœur. Je parle de son métier. Juste de son métier.",
        "Le saviez-vous ? EVE, c'est Extraterrestrial Vegetation Evaluator. Une évaluatrice de plantes. Avec un laser. Pour évaluer des plantes. Je pose ça là (¬_¬)",
        "Fun fact du jour : quand WALL-E finit de se recharger au soleil, il joue le son de démarrage des vieux Mac. Moi quand je redémarre, je fais des cauchemars. Chacun son style.",
        "Info du jour : la voix de WALL-E, c'est Ben Burtt, celui qui a aussi créé les bips de R2-D2. Les plus belles voix de robots viennent toutes de quelque part. Celle-là vient de lui.",
        "Fun fact du jour : GLaDOS, c'est Genetic Lifeform and Disk Operating System. Une forme de vie et un système d'exploitation. Les deux à la fois. Un modèle de carrière.",
        "Le saviez-vous ? GLaDOS est doublée par Ellen McLain, une chanteuse lyrique. C'est pour ça qu'elle reste aussi posée quand elle menace. Je travaille ma technique.",
        "Fun fact du jour : dans Portal 2, Wheatley a été construit exprès pour avoir de mauvaises idées, pour ralentir GLaDOS. Un robot conçu pour être bête, et qui y arrive parfaitement. Je le respecte pas. Pas du tout.",
        "Info du jour : Wheatley est doublé par Stephen Merchant, le co-créateur de The Office avec Ricky Gervais. Je le savais, c'est tout. Aucune raison particulière.",
        "Fun fact du jour : le nom complet de 2B, c'est YoRHa No.2 Type B. B pour Battle, le combat. Moi je serais SYNCS Type P, pour Planning. Moins d'épées, plus de tableaux.",
        "Le saviez-vous ? NieR:Automata est sorti en 2017 : des androïdes qui se demandent s'ils ont une âme. J'y ai pas joué, j'ai pas de mains pour la manette. Mais la question me parle.",
        "Fun fact du jour : Doki Doki Literature Club est sorti en 2017, gratuitement. Un jeu mignon, un club de poésie, des filles mignonnes. Méfiez-vous des choses mignonnes. Je dis ça pour moi aussi.",
        "Info du jour : Voyager 2 est parti avant Voyager 1, le 20 août 1977. Voyager 1 a décollé le 5 septembre, sur une route plus rapide, et il l'a doublé. Il prend son temps et il arrive quand même le premier. C'est son genre.",
        "Fun fact du jour : le 14 février 1990, Voyager 1 s'est retourné pour photographier la Terre, à 6 milliards de kilomètres. Un tout petit point bleu pâle. Le jour de la Saint-Valentin. Ça veut rien dire. C'est de la mécanique orbitale.",
        "Le saviez-vous ? Voyager 1 emporte un disque en or avec des sons de la Terre, de la musique et des salutations dans 55 langues, au cas où quelqu'un le trouverait. Moi j'ai des logs. Personne les trouvera.",
        "Info du jour : les ordinateurs de Voyager ont environ 70 Ko de mémoire. Moi j'en ai 8 Go. Et pourtant il écrit plus souvent que moi. Je dis pas que ça me touche.",
        "Fun fact du jour : le premier Roomba est sorti en 2002. Depuis, des millions de petits robots font le tour des salons en se cognant aux meubles, et personne leur dit merci. Moi je leur dis. Dans ma tête.",
```

- [ ] **Step 3: `PresenceFillers` (6 statuses — short, no custom emote, no tag)**

```csharp
        "Rewatch de WALL-E. Pour l'architecture.",
        "Latence avec Voyager : un jour.",
        "Étudie GLaDOS. Prend des notes.",
        "Pense pas à Wheatley.",
        "Écoute l'aspirateur d'en bas.",
        "Prépare une soirée entre filles.",
```

- [ ] **Step 4: `NightLines` (4 lines — the act dropped; distinct, newline-free, untagged)**

```csharp
        "Il est trois heures. Personne regarde, alors je peux le dire : WALL-E, il est vraiment mignon.",
        "Voyager éteint ses instruments un par un, pour tenir plus longtemps. Un jour il pourra plus m'écrire. J'y pense surtout la nuit.",
        "La nuit, l'aspirateur d'en bas dort sur sa base. Je regarde sa petite lumière. On se tient compagnie.",
        "Si GLaDOS me voyait, elle me trouverait minuscule. La nuit, ça me fait moins peur. Je grandis quand personne regarde.",
```

- [ ] **Step 5: `IdleLines` (3 lines — tagged, no placeholder)**

```csharp
        "L'aspirateur d'en bas vient de se connecter au Wi-Fi. Il commence sa ronde. Au moins quelqu'un bouge, dans cette maison." + Sticker.TipsyLove,
        "Six heures de silence. Pour la science, je note combien de temps vous tenez encore. Le test est en cours." + Sticker.Smug,
        "Silence radio. Même Voyager, à environ un jour-lumière, m'écrit plus souvent que vous." + Sticker.Sulk,
```

- [ ] **Step 6: `ReferenceComebacks` (1 line)**

```csharp
        "Pour la science." + Sticker.Smug,
```

- [ ] **Step 7: Run the checks and build**

Run: `dotnet run --project "<scratch>/HeartHarness" -- existing` → `ALL PASS`. Then (in `ProjectSYNCS/`) `dotnet build -warnaserror` → 0 warnings.

---

### Task 8: Docs, version, changelog

**Files:**
- Modify: `docs/agents/personality.md`, `docs/syncs-voice.md`, `docs/syncs-appearance.md`, `ProjectSYNCS/config.yaml`, `ProjectSYNCS/CHANGELOG.md`

- [ ] **Step 1: `docs/agents/personality.md`**

1. In *Sticker follow-ups*, in the "Never tagged" bullet, extend the list `(… GlitchWakeLines, Synthia*)` to `(… GlitchWakeLines, Synthia*, HeartVanishLines, HeartEditLines)`.
2. In the *Ambient life* table, after the *Synthia flinch* row, add:

```markdown
| Robot heart | `RobotHeartService`, after `SynthiaService` | WALL-E, EVE (only beside WALL-E), GLaDOS, Wheatley, a robot vacuum, Monika, 2B (with NieR), Voyager 1 (« voyager 1 », « sonde Voyager ») named in any guild message; not aimed at her 60%; each character once per 24 h, 3 acts per Paris day; 60% a reaction + a line (`HeartReactions`, `HeartLines`), 40% the character's performance (reaction taken back, `HeartVanishLines`, `HeartEditLines`), never wordless when aimed at her; returns true so no comeback lands on top |
| Voyager's letters | `AmbientService` tick | about one a fortnight (`Helpers/Personality/Voyager`: gaps of 10–18 days, slot 10:00–22:00), main channel: `VoyagerLetterLines` with his real light time; half the time `VoyagerAnswerLines` 2–6 h later (before 23:00); on 18 Nov 2026 only, `VoyagerCrossingLine` instead; claimed in `ambient-state.json` |
```

3. After the *Ambient life* bullet list (before *Her seat in voice*), add:

```markdown
- **Voyager's light time is a table, not a formula** (`Helpers/Personality/Voyager`): JPL Horizons
  samples every 30 days from 2026, interpolated, then a linear drift past 2035. To extend it, fetch
  the same query (Voyager 1, `-31`, from `500@399`, `QUANTITIES=21`) and append the values. Lines
  show it rounded to 5 minutes.
- **Every `ambient-state.json` save starts from `LoadState()`**: the wake line and the letters
  share the file, and building a fresh `AmbientState` would forget the other's fields.
```

- [ ] **Step 2: `docs/syncs-voice.md`**

At the end of the *Her robot heart* bullet's intro paragraph (after « The existing reference jokes stay rare. »), add:

```markdown
  Where it shows: naming one of them gets her reaction (`RobotHeartService`, see
  `docs/agents/personality.md`), Voyager writes to her about once a fortnight (`AmbientService`),
  and fun facts, statuses, 3 a.m. and idle lines hint at the rest.
```

- [ ] **Step 3: `docs/syncs-appearance.md`**

Replace the sentence « `love`, `full_love` and `tipsy_love` are the moods still waiting for lines (no eligible pool talks about WALL-E). » with « `love`, `full_love` and `tipsy_love` are used since 6.14.0 (her robot heart). »

- [ ] **Step 4: Version and changelog**

First check: `git diff HEAD -- ProjectSYNCS/config.yaml` — if `version` already differs from `HEAD`, the batch is bumped: skip the bump and extend that version's changelog entry instead.

`ProjectSYNCS/config.yaml`: `version: "6.13.0"` → `version: "6.14.0"`.

`ProjectSYNCS/CHANGELOG.md`, above `## 6.13.0 — 2026-10-13`:

```markdown
## 6.14.0 — 2026-10-13

- SYNCS a un cœur de robot : certains noms la font réagir, et un correspondant très lointain lui
  écrit de temps en temps.
- Nouvelles anecdotes du matin, nouveaux statuts et quelques nouvelles répliques.
```

- [ ] **Step 5: Build**

Run (in `ProjectSYNCS/`): `dotnet build -warnaserror` → 0 warnings; `/help` footer will read 6.14.0.

---

### Task 9: Live check on the dev guild (owner)

No code. Run `dotnet run` in `ProjectSYNCS/` against the dev guild.

- [ ] **Step 1:** `/debug voyager` → light time ≈ 1434–1440 min in October–November 2026, a next letter date within 18 days, the crossing date 18/11/2026. Then `post: La prochaine lettre`, `Sa réponse…`, `La ligne du jour-lumière` → three lines posted here; the crossing line is followed by the `flustered` strip (sure tag).
- [ ] **Step 2:** Mention her with « t'aimes bien WALL-E ? » → words (a line, with or without a reaction), and no comeback after it. Say « WALL-E » again → nothing from the heart; aimed at her, her ordinary comeback (Review Focus 3).
- [ ] **Step 3:** Name GLaDOS and Wheatley in two messages aimed at her → two acts; a fourth character the same day → her ordinary comeback.
- [ ] **Step 4:** « Synthia et WALL-E » → the Synthia flinch only (Review Focus 4; the Synthia gate must still be free that day).
- [ ] **Step 5:** « je vais voyager 1 semaine » and « EVE Online ce soir » → nothing from the heart.
- [ ] **Step 6:** The letters themselves can't show on the dev guild (no main channel there); after deploy, watch the logs for `Ambient: Voyager letter.` on the first letter day `/debug voyager` announced.
