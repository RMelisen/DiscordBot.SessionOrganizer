# Plynling hygiene and sickness Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Give Plynlings a third need, Hygiène (washed from the card), and a sickness that can start on any morning — likelier when dirty — needs daily medicine, and can kill if neglected.

**Architecture:** Hygiene is stored like hunger and happiness (value at `NeedsAsOf`) and derived from elapsed time in `PlynlingLife`. Each 05:00 Paris « morning » is played once, in time order, inside `PlynlingLife.Settle`, with deterministic rolls from `PlynlingSickness` keyed on (Plynling id, day, purpose) — so a command and the hourly sweep always agree and every morning is checkable. Art is a new `sick` mood and a `_dirty` overlay variant, added as new filenames in `tools/plynling-art`.

**Tech Stack:** C# / .NET 10, EF Core SQLite (migrations applied on startup), Discord.Net 3.20.1 (Components V2 card), Python 3 + Pillow for the art; a scratch console harness.

**Spec:** `docs/superpowers/specs/2026-09-30-plynling-hygiene-sickness-design.md`

## Global Constraints

- **Never commit or push.** The owner commits. Each task ends by listing the files to commit.
- Stored enums are **append-only**: `DeathCause`, `JournalKind`, and item keys (`care.medicine`). `PlynlingMood`, `CareOutcome`, `FreezeOutcome`, `ItemKind` are not stored but new values still go **at the end**.
- No `DateTime.Now`; wall-clock times go through `AppTime` (Paris). Mornings are **05:00 Paris**, built from the wall clock (`AtWallClock`), like `WakeAfter`.
- User-facing text French, code and comments English. Every Plynling line exists in both genders (`GenderedLines`), never genders the player, and follows `docs/plynling-writing-style.md` (SYNCS's voice: bratty, kawaii, petty; never the kaomoji « (｡•́︿•̀｡) », use « (╥﹏╥) » for sad).
- Discord caps: 40 components per V2 message, 5 buttons per row, custom-ids unique per message (one verb per button).
- Numbers (all tunable constants): `HygieneLife` 3 days; `DirtyBelow` 0.33; `DirtyHappinessFactor` 1.5; `BathAmount` 0.60; `BathCooldown` 6 h; `ForageDirt` 0.10; `PlayDirt` 0.05; `OnsetFloor` 0.005; `OnsetPerPoint` 0.012; `IllnessDeathChance` 0.20; death rolls from the **third** sick morning; recovery 5–15 per morning, +15–25 with medicine, cured at 100; `SickMealFactor` 0.5; `MedicinePrice` 30 cailloux.
- Art: 32×32 sprites; **nothing moves side to side**; new filenames only, **no `ART_VERSION` / `PlynlingArt.Version` bump**; every existing file must re-export byte-identical.
- Images are GitHub raw URLs: new art must be **pushed to `main` before** the bot links to it (the owner pushes).
- Build with `dotnet build -warnaserror` (0 warnings). Git Bash here mangles `python -c` / `python -` — write scripts to files.

`$REPO` = `C:\Users\c235773\Desktop\Sources - RME\Discord Bots\SessionOrganizer`; `$SCRATCH` = the session scratchpad; the harness is `$SCRATCH/health/`.

---

## Phase 1 — Hygiène and the bath

### Task 1: The hygiene rules (pure) and the first migration

**Files:**
- Modify: `ProjectSYNCS/Models/Plynling.cs` (add `Hygiene`, `LastMorningDay`)
- Modify: `ProjectSYNCS/Helpers/PlynlingLife.cs`
- Create: `ProjectSYNCS/Migrations/<timestamp>_AddPlynlingHygiene.cs` (via `dotnet ef`)
- Test: `$SCRATCH/health/` (new console project)

**Interfaces:**
- Produces: `Plynling.Hygiene` (double), `Plynling.LastMorningDay` (int); `PlynlingLife.HygieneLife`, `DirtyBelow`, `DirtyHappinessFactor`, `BathAmount`, `BathCooldown`, `ForageDirt`, `PlayDirt`, `ResurrectHygiene`; `PlynlingLife.HygieneAt(Plynling, DateTimeOffset) -> double`; `PlynlingLife.IsDirty(Plynling, DateTimeOffset) -> bool`; `PlynlingLife.Bath(Plynling, DateTimeOffset)`; `PlynlingLife.WouldWasteBath(Plynling, DateTimeOffset) -> bool`; `PlynlingLife.Dirty(Plynling, DateTimeOffset, double amount)`; `PlynlingLife.MorningAt(int dayKey) -> DateTimeOffset`; `PlynlingLife.MorningDayAtOrBefore(DateTimeOffset) -> int`; `PlynlingLife.NextDay(int dayKey) -> int`.

- [ ] **Step 1: Create the harness**

`$SCRATCH/health/health.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <InvariantGlobalization>false</InvariantGlobalization>
    <NoWarn>$(NoWarn);MSB3277</NoWarn>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="C:\Users\c235773\Desktop\Sources - RME\Discord Bots\SessionOrganizer\ProjectSYNCS\ProjectSYNCS.csproj" />
  </ItemGroup>
</Project>
```

`$SCRATCH/health/Program.cs`:

```csharp
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

var failures = new List<string>();
void Check(bool ok, string what) { if (!ok) failures.Add(what); }
bool Near(double a, double b, double eps = 1e-6) => Math.Abs(a - b) < eps;
DateTimeOffset Paris(int y, int mo, int d, int h, int mi = 0) =>
    new DateTimeOffset(new DateTime(y, mo, d, h, mi, 0), AppTime.Zone.GetUtcOffset(new DateTime(y, mo, d, h, mi, 0)));
Plynling Fresh(DateTimeOffset at) => PlynlingLife.Create(1, 1, "Lila", PlynlingSpecies.Cepe, PlynlingGender.Female, at);

// ---- hygiene ---------------------------------------------------------------------------------
var t0 = Paris(2026, 10, 5, 12);
var p = Fresh(t0);
p.Id = 7;
Check(Near(p.Hygiene, 1.0), "a new Plynling starts clean");
Check(Near(PlynlingLife.HygieneAt(p, t0 + TimeSpan.FromDays(1.5)), 0.5), "hygiene halves in 1.5 days");
Check(Near(PlynlingLife.HygieneAt(p, t0 + TimeSpan.FromDays(4)), 0.0), "hygiene floors at 0");
Check(!PlynlingLife.IsDirty(p, t0 + TimeSpan.FromDays(2)) && PlynlingLife.IsDirty(p, t0 + TimeSpan.FromDays(2.02)), "dirty strictly below 33 %");

// Happiness drains 1.5x from the exact moment hygiene crosses 33 %, compared with a minute-by-minute simulation.
var q = Fresh(t0);
q.Happiness = 1.0; q.Hygiene = 0.40;
var sim = 1.0;
for (var m = 1; m <= 60 * 24; m++)
{
    var hyg = 0.40 - (m - 0.5) / (3.0 * 24 * 60);
    sim -= (hyg < 0.33 ? 1.5 : 1.0) / (36.0 * 60);
}
Check(Near(PlynlingLife.HappinessAt(q, t0 + TimeSpan.FromDays(1)), Math.Max(0, sim), 1e-3), "happiness at 1.5x after the crossing");
var clean = Fresh(t0); clean.Happiness = 1.0; clean.Hygiene = 1.0;
Check(Near(PlynlingLife.HappinessAt(clean, t0 + TimeSpan.FromHours(18)), 0.5), "a clean Plynling's happiness is unchanged by hygiene");

// Bath, dirt and waste.
var b = Fresh(t0); b.Hygiene = 0.2;
PlynlingLife.Bath(b, t0);
Check(Near(b.Hygiene, 0.8), "a bath adds 60 %");
PlynlingLife.Bath(b, t0);
Check(Near(b.Hygiene, 1.0), "a bath caps at 100 %");
Check(PlynlingLife.WouldWasteBath(b, t0), "a bath at 100 % is wasted");
PlynlingLife.Dirty(b, t0, PlynlingLife.ForageDirt);
Check(Near(b.Hygiene, 0.9), "forage dirt");
var f = Fresh(t0); f.Hygiene = 0.5; PlynlingLife.Freeze(f, t0, byStaff: true);
Check(Near(PlynlingLife.HygieneAt(f, t0 + TimeSpan.FromDays(5)), 0.5), "frozen hygiene does not move");

// ---- mornings -----------------------------------------------------------------------------------
Check(PlynlingLife.MorningAt(20261005) == Paris(2026, 10, 5, 5), "a morning is 05:00 Paris");
Check(PlynlingLife.MorningAt(20260329) == Paris(2026, 3, 29, 5) && PlynlingLife.MorningAt(20261025) == Paris(2026, 10, 25, 5), "05:00 local on both clock-change days");
Check(PlynlingLife.MorningDayAtOrBefore(Paris(2026, 10, 5, 4, 59)) == 20261004, "before 05:00 the last morning is yesterday's");
Check(PlynlingLife.MorningDayAtOrBefore(Paris(2026, 10, 5, 5)) == 20261005, "at 05:00 it is today's");
Check(PlynlingLife.NextDay(20261031) == 20261101 && PlynlingLife.NextDay(20261231) == 20270101, "next day across months and years");
Check(Fresh(Paris(2026, 10, 5, 3)).LastMorningDay == 20261004, "a Plynling adopted at 03:00 has not had today's morning yet");

Console.WriteLine(failures.Count == 0 ? "OK" : "FAIL\n" + string.Join("\n", failures));
return failures.Count == 0 ? 0 : 1;
```

- [ ] **Step 2: Run it to see it fail**

Run: `cd "$SCRATCH/health" && dotnet run`
Expected: build errors — `Hygiene`, `HygieneAt`, `IsDirty`, `Bath`, `WouldWasteBath`, `Dirty`, `ForageDirt`, `MorningAt`, `MorningDayAtOrBefore`, `NextDay`, `LastMorningDay` do not exist.

- [ ] **Step 3: Add the columns to the model**

In `ProjectSYNCS/Models/Plynling.cs`, after `public double Happiness { get; set; }`:

```csharp
    // 0..1, as of NeedsAsOf, like hunger and happiness. Below PlynlingLife.DirtyBelow it is « sale ».
    public double Hygiene { get; set; } = 1.0;
```

and after `public int LastGiftDay { get; set; }`:

```csharp
    // The last 05:00 Paris morning already played (AppTime.DayKey, yyyymmdd): PlynlingLife.Settle
    // plays every later morning, in order, exactly once.
    public int LastMorningDay { get; set; }
```

- [ ] **Step 4: Add the rules to `PlynlingLife`**

In `ProjectSYNCS/Helpers/PlynlingLife.cs`, next to `HappinessLife`:

```csharp
    public static readonly TimeSpan HygieneLife = TimeSpan.FromDays(3);
    public static readonly TimeSpan BathCooldown = TimeSpan.FromHours(6);
```

next to the other thresholds:

```csharp
    // Below this a Plynling is « sale »: its happiness drains DirtyHappinessFactor times faster, and
    // the card shows the dirt. Strict, like the others.
    public const double DirtyBelow = 0.33;
    public const double DirtyHappinessFactor = 1.5;
    public const double BathAmount = 0.60;
    public const double ForageDirt = 0.10;
    public const double PlayDirt = 0.05;
    public const double ResurrectHygiene = 0.50;
```

In `Create`, add `Hygiene = 1.0,` and `LastMorningDay = MorningDayAtOrBefore(now),`.

Replace `HappinessAt` and add the hygiene functions:

```csharp
    public static double HygieneAt(Plynling p, DateTimeOffset t) =>
        IsFrozen(p) || IsDead(p) ? p.Hygiene : Clamp(p.Hygiene - (t - p.NeedsAsOf) / HygieneLife);

    public static bool IsDirty(Plynling p, DateTimeOffset t) => HygieneAt(p, t) < DirtyBelow;

    // Happiness drains at its own rate while clean and DirtyHappinessFactor times faster once
    // hygiene has fallen below DirtyBelow. Hygiene falls linearly, so the crossing is one exact
    // instant and the drain is two straight pieces — no approximation.
    public static double HappinessAt(Plynling p, DateTimeOffset t)
    {
        if (IsFrozen(p) || IsDead(p)) return p.Happiness;
        var elapsed = Math.Max(0, (t - p.NeedsAsOf).TotalSeconds);
        var cleanFor = p.Hygiene < DirtyBelow ? 0 : (p.Hygiene - DirtyBelow) * HygieneLife.TotalSeconds;
        var clean = Math.Min(elapsed, cleanFor);
        var dirty = elapsed - clean;
        return Clamp(p.Happiness - (clean + dirty * DirtyHappinessFactor) / HappinessLife.TotalSeconds);
    }

    public static bool WouldWasteBath(Plynling p, DateTimeOffset now) => HygieneAt(p, now) >= Full;

    public static void Bath(Plynling p, DateTimeOffset now)
    {
        Rebase(p, now);
        p.Hygiene = Clamp(p.Hygiene + BathAmount);
    }

    // What an outing costs in hygiene (forage, a game).
    public static void Dirty(Plynling p, DateTimeOffset now, double amount)
    {
        Rebase(p, now);
        p.Hygiene = Clamp(p.Hygiene - amount);
    }

    // ---- mornings: 05:00 Paris, when every Plynling wakes -------------------------------------------

    public static DateTimeOffset MorningAt(int dayKey) =>
        AtWallClock(new DateTime(dayKey / 10000, dayKey / 100 % 100, dayKey % 100) + NightEnd);

    public static int NextDay(int dayKey)
    {
        var d = new DateTime(dayKey / 10000, dayKey / 100 % 100, dayKey % 100).AddDays(1);
        return d.Year * 10000 + d.Month * 100 + d.Day;
    }

    // The day of the latest morning at or before t: today's from 05:00, yesterday's before.
    public static int MorningDayAtOrBefore(DateTimeOffset t)
    {
        var zoned = AppTime.ToZoned(t);
        var day = zoned.TimeOfDay >= NightEnd ? zoned.Date : zoned.Date.AddDays(-1);
        return day.Year * 10000 + day.Month * 100 + day.Day;
    }
```

`Rebase` must compute every value before assigning any: `HappinessAt` reads the *stored* hygiene, so overwriting `p.Hygiene` first would bend the happiness slope. Write it as:

```csharp
    private static void Rebase(Plynling p, DateTimeOffset at)
    {
        var hunger = HungerAt(p, at);
        var happiness = HappinessAt(p, at);        // reads the stored hygiene, so compute it first
        var hygiene = HygieneAt(p, at);
        p.Hunger = hunger;
        p.Happiness = happiness;
        p.Hygiene = hygiene;
        p.NeedsAsOf = at;
    }
```

In `Play`, after the happiness line: `p.Hygiene = Clamp(p.Hygiene - PlayDirt);` (Rebase has already run).

In `Resurrect`, add `p.Hygiene = ResurrectHygiene;` and `p.LastMorningDay = MorningDayAtOrBefore(now);`.

- [ ] **Step 5: Run the harness**

Run: `cd "$SCRATCH/health" && dotnet run` → Expected: `OK`.

- [ ] **Step 6: Add the migration**

Run: `cd "$REPO/ProjectSYNCS" && dotnet ef migrations add AddPlynlingHygiene`
Then edit the generated `Up` so existing rows start clean and with no mornings to catch up:

```csharp
            migrationBuilder.AddColumn<double>(
                name: "Hygiene", table: "Plynlings", type: "REAL", nullable: false, defaultValue: 1.0);

            // Existing Plynlings start at the latest morning, so none replays mornings from before
            // the feature. Evaluated when the migration runs, not when it was written.
            migrationBuilder.AddColumn<int>(
                name: "LastMorningDay", table: "Plynlings", type: "INTEGER", nullable: false,
                defaultValue: Helpers.PlynlingLife.MorningDayAtOrBefore(DateTimeOffset.UtcNow));
```

(Keep the generated `Down`. Check the table name the generator used — `Plynlings` — and keep it.)

- [ ] **Step 7: Build**

Run: `cd "$REPO/ProjectSYNCS" && dotnet build -warnaserror` → 0 warnings, 0 errors. Run the harness again → `OK`.

- [ ] **Step 8: Hand over**

Files to commit: `Plynling.cs`, `PlynlingLife.cs`, the migration and its designer file, `AppDbContextModelSnapshot.cs`.

---

### Task 2: « Laver », the card, and dirt's knock-on effects

**Files:**
- Modify: `ProjectSYNCS/Services/PlynlingService.cs` (`BathAsync`, forage dirt)
- Modify: `ProjectSYNCS/Services/PlynlingCareService.cs` (`BathAsync`, refusals)
- Modify: `ProjectSYNCS/Services/PlynlingCooldowns.cs` (the bath gate)
- Modify: `ProjectSYNCS/Interactions/Components/PlynlingComponentHandler.cs` (`plyn:bath:*`)
- Modify: `ProjectSYNCS/Commands/PlynlingModule.cs` (`BuildCard` buttons)
- Modify: `ProjectSYNCS/Helpers/PlynlingCardUi.cs` (the Hygiène bar, « · sale »)
- Modify: `ProjectSYNCS/Helpers/PlynlingText.cs`, `ProjectSYNCS/Services/BotResponses.cs`, `ProjectSYNCS/Helpers/EconomyLog.cs`
- Test: `$SCRATCH/health/Program.cs`

**Interfaces:**
- Consumes: Task 1's `HygieneAt`, `IsDirty`, `Bath`, `WouldWasteBath`, `Dirty`, `BathCooldown`, `ForageDirt`.
- Produces: `CareOutcome.NotOwner` (appended); `PlynlingService.BathAsync(int plynlingId, ulong actorId, DateTimeOffset now) -> (CareOutcome, Plynling?)`; `PlynlingCareService.BathAsync(int plynlingId, ulong actorId, ulong channelId, DateTimeOffset now) -> CareReply`; `PlynlingCooldowns.Bath : CooldownGate<int>`; custom-id `plyn:bath:{id}`; `BotResponses.PlynlingBathLines`; `EconomyLog.ActBath`.

- [ ] **Step 1: Add the failing checks** — in the harness, before the final line:

```csharp
// ---- the card ------------------------------------------------------------------------------
var card = Fresh(t0); card.Id = 3; card.Hygiene = 0.2;
var status = PlynlingCardUi.Status(card, t0);
Check(status.Contains("**Hygiène**") && status.Contains("20 %"), "the card shows the Hygiène bar");
Check(status.Contains("· sale"), "the mood line says « sale » when dirty");
var built = ProjectSYNCS.Commands.PlynlingModule.BuildCard(card, t0.AddHours(8), null);
var ids = built.Components.OfType<Discord.ActionRowComponent>().SelectMany(r => r.Components).OfType<Discord.ButtonComponent>().Select(x => x.CustomId).ToList();
Check(ids.Contains("plyn:bath:3") && ids.Contains("plyn:pet:3"), "Laver sits beside Caresser");
```

Run → build error (`PlynlingModule` has no bath button yet is a runtime FAIL; the `Hygiène` text FAILs).

- [ ] **Step 2: The card text**

In `PlynlingCardUi.Status`, compute `var hygiene = PlynlingLife.HygieneAt(p, now);` and change the return to:

```csharp
        var dirty = p.FrozenAt is null && PlynlingLife.IsDirty(p, now) ? " · sale" : "";
        return $"**Faim** `{Bar(hunger)}` {Percent(hunger)}{clock}\n" +
               $"**Bonheur** `{Bar(happiness)}` {Percent(happiness)}\n" +
               $"**Hygiène** `{Bar(hygiene)}` {Percent(hygiene)}\n" +
               $"**Humeur** · *{MoodLabel(PlynlingLife.Mood(p, now), p.Gender)}{dirty}*";
```

- [ ] **Step 3: The card button**

In `PlynlingModule.BuildCard`, replace the « Caresser » row with:

```csharp
            // No petting or washing a sleeping Plynling — the buttons go, feeding stays. Laver is
            // offered to everyone like the rest of the card and refused in the handler for anyone
            // but the owner.
            if (!PlynlingLife.IsAsleep(now))
                builder.AddComponent(new ActionRowBuilder()
                    .WithButton("🤲 Caresser", $"plyn:pet:{plynling.Id}", ButtonStyle.Primary)
                    .WithButton("🛁 Laver", $"plyn:bath:{plynling.Id}", ButtonStyle.Secondary));
```

- [ ] **Step 4: The service**

Append `NotOwner` to `CareOutcome` in `PlynlingService.cs`:
`public enum CareOutcome { Done, NoPlynling, Dead, Frozen, Wasted, TooPoor, Asleep, Sulking, TooSoon, NotOwner }`

Add to `PlynlingService` (after `PetAsync`):

```csharp
    // Owner only, free. Refused asleep, frozen, dead, or already clean.
    public async Task<(CareOutcome Outcome, Plynling? Plynling)> BathAsync(int plynlingId, ulong actorId, DateTimeOffset now)
    {
        var plynling = await GetByIdAsync(plynlingId, now);
        CareOutcome? refusal =
            plynling is null ? CareOutcome.NoPlynling
            : plynling.DiedAt is not null ? CareOutcome.Dead
            : plynling.OwnerId != actorId ? CareOutcome.NotOwner
            : plynling.FrozenAt is not null ? CareOutcome.Frozen
            : PlynlingLife.IsAsleep(now) ? CareOutcome.Asleep
            : PlynlingLife.WouldWasteBath(plynling, now) ? CareOutcome.Wasted
            : null;
        if (refusal is { } r) return (r, plynling);

        PlynlingLife.Bath(plynling!, now);
        await EconomyLog.AddAsync(_db_context, plynling!.GuildId, EconomyLog.ActBath, 1, now);
        await _db_context.SaveChangesAsync();
        return (CareOutcome.Done, plynling);
    }
```

In `ForageAsync`, after `wallet.LastForageAt = now;`: `PlynlingLife.Dirty(p, now, PlynlingLife.ForageDirt);`

In `EconomyLog`, next to `ActForage`: `public const string ActBath = "act.bath";` and append `ActBath` to `Activities` (keys are stored: append only).

- [ ] **Step 5: The cooldown, the care service and the handler**

`PlynlingCooldowns`:

```csharp
    // One bath every PlynlingLife.BathCooldown per Plynling — only its owner washes it. In memory,
    // like the pet gate; released when the bath is refused.
    public CooldownGate<int> Bath { get; } = new(PlynlingLife.BathCooldown, forget: TimeSpan.FromHours(12));
```

`PlynlingCareService` (after `PetAsync`):

```csharp
    public async Task<CareReply> BathAsync(int plynlingId, ulong actorId, ulong channelId, DateTimeOffset now)
    {
        if (!_cooldowns.Bath.TryClaim(plynlingId, out var readyAt)) return new CareReply(null, PlynlingText.BathCooldown(readyAt));

        var (outcome, plynling) = await _plynlings.BathAsync(plynlingId, actorId, now);
        if (outcome != CareOutcome.Done || plynling is null)
        {
            _cooldowns.Bath.Release(plynlingId);
            return new CareReply(null, outcome == CareOutcome.Wasted
                ? PlynlingText.AlreadyClean(plynling?.Gender ?? PlynlingGender.Male)
                : Refusal(outcome, plynling?.Gender ?? PlynlingGender.Male));
        }

        var line = string.Format(_picker.Pick(channelId, BotResponses.PlynlingBathLines.For(plynling.Gender)),
            PlynlingCardUi.SafeName(plynling.Name));
        var partner = await _plynlings.GetPartnerAsync(plynling);
        return new CareReply(PlynlingModule.BuildCard(plynling, now, line, partnerName: partner?.Name), null);
    }
```

Add to `Refusal`: `CareOutcome.NotOwner => PlynlingText.NotYourPlynling(gender),`.

`PlynlingComponentHandler`, after `OnPetAsync`:

```csharp
    [ComponentInteraction("plyn:bath:*", ignoreGroupNames: true)]
    public async Task OnBathAsync(string idStr)
    {
        if (!int.TryParse(idStr, out var id))
        {
            await RespondAsync(PlynlingText.Unknown, ephemeral: true);
            return;
        }
        await ApplyAsync(await _care.BathAsync(id, Context.User.Id, Context.Channel.Id, DateTimeOffset.UtcNow));
    }
```

- [ ] **Step 6: The text**

`PlynlingText`:

```csharp
    // Refused before the Plynling is loaded, like PetCooldown: worded to need no gender.
    public static string BathCooldown(DateTimeOffset readyAt) =>
        $"Encore tout propre de son dernier bain. Prochain bain <t:{readyAt.ToUnixTimeSeconds()}:R>.";

    public static string AlreadyClean(PlynlingGender g) =>
        $"{g.Agree("Il est déjà tout propre", "Elle est déjà toute propre")} ! Garde l'eau pour plus tard.";

    public static string NotYourPlynling(PlynlingGender g) =>
        $"{g.Agree("Ce", "Cette")} Plynling n'est pas {g.Agree("le tien", "la tienne")} : seul son propriétaire peut faire ça.";
```

`BotResponses` — add `PlynlingBathLines` beside `PlynlingPetLines` ({0} = the name). Write it through a scratch script like the earlier pool scripts (lines in ⟨M|F⟩ form, expanded to the two halves):

```
🛁 Plouf ! **{0}** ressort de l'eau en brillant comme un caillou mouillé ✨
Frotte, frotte… **{0}** est propre comme un sou neuf. Et un peu vexé⟨|e⟩ d'avoir été frotté⟨|e⟩ (¬_¬)
**{0}** a fait des bulles. Beaucoup de bulles. Il y en a partout sauf sur ⟨lui|elle⟩.
Bain terminé : **{0}** sent la mousse et le linge frais (˶ᵔ ᵕ ᵔ˶)
Tu as lavé **{0}**. ⟨Il|Elle⟩ fait semblant de détester ça. ⟨Il|Elle⟩ adore ça.
**{0}** s'ébroue et m'éclabousse. Je note. Je note tout (¬_¬)
Toute la boue est partie. Enfin presque. **{0}** en a gardé un peu derrière l'oreille, par principe.
**{0}** sort du bain, enroulé⟨|e⟩ dans une serviette trop grande. Adorable. Ne le dis à personne ♡
Un bain, un vrai ! **{0}** réclame un canard en plastique la prochaine fois.
**{0}** est ⟨tout propre|toute propre⟩. Et aussi ⟨tout fripé|toute fripée⟩. On ne peut pas tout avoir.
```

- [ ] **Step 7: Build, run the harness, try it**

Run: `dotnet build -warnaserror` → 0/0; harness → `OK`. Also run the existing `plynlingui` gender check if it is in the scratchpad (bans `il`/`-le`/`mort` in F halves); otherwise read the F half of `PlynlingBathLines` for any masculine word.

- [ ] **Step 8: Hand over**

Files: `PlynlingService.cs`, `PlynlingCareService.cs`, `PlynlingCooldowns.cs`, `PlynlingComponentHandler.cs`, `PlynlingModule.cs`, `PlynlingCardUi.cs`, `PlynlingText.cs`, `BotResponses.cs`, `EconomyLog.cs`. The owner can ship Phase 1 here: the card shows the bar and « Laver », with the ordinary face until the art lands.

---

## Phase 2 — The art

### Task 3: The `sick` face and the dirt overlay (preview first)

**Files:**
- Modify: `tools/plynling-art/sprites.py` (`face`: the `sick` state; `finish` is in `species.py`)
- Modify: `tools/plynling-art/species.py` (`finish`: the sick tint)
- Modify: `tools/plynling-art/motion.py` (`sick` motion)
- Create: `tools/plynling-art/dirt.py` (the overlay)
- Modify: `tools/plynling-art/export.py` (`STATES` += `sick`; the `_dirty` files)
- Create (scratch): `$SCRATCH/health/art_preview.py`

**Interfaces:**
- Produces: files `assets/plynlings/plynling_<sp>[_baby]_sick_v4.webp` (14) and `plynling_<sp>[_baby]_<state>_dirty_v4.webp` for `DIRTY_STATES = ["happy", "content", "sad", "hungry", "starving", "sleeping", "sick"]` (98). `dirt.dirty(im) -> Image`.

- [ ] **Step 1: Prove the export is byte-identical on this machine first**

Run: `cd "$REPO/tools/plynling-art" && python export.py` then `cd "$REPO" && git status --short assets/plynlings | head`
Expected: **no output** (nothing modified). If files show as modified, **STOP**: this Pillow encodes differently from the machine that made them; tell the owner, restore with `git checkout -- assets/plynlings`, and do the export on the original machine.

- [ ] **Step 2: The sick face**

In `sprites.py`, add colours at the top with the others: `SICK_BLUSH = (150, 190, 110)`, `THERMO_GLASS = (235, 240, 250)`, `THERMO_RED = (220, 60, 60)`. In `face()`, before the `elif state == "sleeping":` branch:

```python
    elif state == "sick":
        tired_eye(12)                                   # heavy lids, bags under the eyes
        tired_eye(18)
        for x, y in ((14, 24), (15, 23), (16, 24), (17, 23)):          # a queasy wavy mouth
            P(x, y, INK)
        blush(SICK_BLUSH)                               # a greenish flush where the pink would be
        P(18, 24, THERMO_GLASS)                         # a thermometer in the corner of the mouth
        P(19, 24, THERMO_GLASS)
        P(20, 24, THERMO_RED)
```

In `species.py` `finish()`, in the tint loop, after the `starving` tint: `elif state == "sick": c = lerp(c, SICK_TINT, 0.22)` with `SICK_TINT = (150, 200, 120)` defined beside `ICE`.

In `motion.py` `Pose.__init__`, make `sick` move like `hungry` but without drool or sweat: the breath already applies (it is not frozen); add `"sick"` to nothing else. Confirm `self.touch` is True for `sick` (species touches still play — it is ill, not frozen).

- [ ] **Step 3: The dirt overlay**

`tools/plynling-art/dirt.py`:

```python
"""The dirt overlay: a few mud smudges stamped on a finished frame, placed from the body itself
(its bounding box), so they sit on the cap and the flanks of every species and follow the head up
and down with the breath and the hop. Nothing moves side to side. The face zone is left clear."""
from PIL import Image

MUD = (122, 88, 58, 255)
MUD_DARK = (92, 64, 42, 255)
# Relative anchors in the body's bounding box: cap left, cap right, lower flank right, lower flank left.
ANCHORS = [(0.22, 0.28), (0.74, 0.20), (0.78, 0.74), (0.24, 0.80)]
SHAPE = [(0, 0, MUD), (1, 0, MUD_DARK), (0, 1, MUD_DARK)]
FACE = (10, 17, 21, 25)          # x0, y0, x1, y1 on the 32x32 grid, before the head's motion


def dirty(im):
    out = im.copy()
    px = out.load()
    solid = [(x, y) for y in range(out.height) for x in range(out.width) if px[x, y][3] == 255]
    if not solid:
        return out
    x0, x1 = min(x for x, _ in solid), max(x for x, _ in solid)
    y0, y1 = min(y for _, y in solid), max(y for _, y in solid)
    solid_set = set(solid)
    for ax, ay in ANCHORS:
        cx, cy = round(x0 + ax * (x1 - x0)), round(y0 + ay * (y1 - y0))
        for dx, dy, c in SHAPE:
            x, y = cx + dx, cy + dy
            in_face = FACE[0] <= x <= FACE[2] and FACE[1] <= y <= FACE[3]
            if (x, y) in solid_set and not in_face:
                px[x, y] = c
    return out
```

- [ ] **Step 4: Preview sheet (the owner judges before any export)**

`$SCRATCH/health/art_preview.py`:

```python
import os, sys
ART = r"C:\Users\c235773\Desktop\Sources - RME\Discord Bots\SessionOrganizer\tools\plynling-art"
sys.path.insert(0, ART)
from PIL import Image
from common import SPECIES
from export import STAGED
from sprites import build
from dirt import dirty

rows = []
for sp in SPECIES:
    for stage in (["adult", "baby"] if sp in STAGED else ["adult"]):
        rows.append((sp, stage))
cols = [("content", False), ("content", True), ("hungry", True), ("sad", True), ("sick", False), ("sick", True)]
K = 5
sheet = Image.new("RGBA", (len(cols) * 34 * K, len(rows) * 34 * K), (49, 51, 56, 255))
for r, (sp, stage) in enumerate(rows):
    for c, (state, d) in enumerate(cols):
        im = build(state, sp, 0, stage=stage) if stage == "baby" else build(state, sp, 0)
        if d:
            im = dirty(im)
        sheet.alpha_composite(im.resize((32 * K, 32 * K), Image.NEAREST), (c * 34 * K + K, r * 34 * K + K))
out = os.path.join(os.path.dirname(os.path.abspath(__file__)), "art_preview.png")
sheet.save(out)
print(out)
```

Run it, look at the result, adjust (smudge anchors per species via an optional `dirt_nudge` in `common.SPECIES` if one lands badly; thermometer clipped on the narrow Mycena is acceptable only if still readable). **STOP: send `art_preview.png` to the owner** (SendUserFile, render) and wait for approval before Step 5.

- [ ] **Step 5: Export**

In `export.py`: `STATES` gets `"sick"` appended (after `"angry"`). Add `DIRTY_STATES = ["happy", "content", "sad", "hungry", "starving", "sleeping", "sick"]`, `from dirt import dirty`, and inside the state loop, after the card loop's `save_loop(...)` calls:

```python
            if state in DIRTY_STATES:
                save_loop([dirty(build(state, sp, f)) for f in range(FRAMES)], f"plynling_{sp}_{state}_dirty")
                count += 1
                if sp in STAGED:
                    save_loop([dirty(build(state, sp, f, stage="baby")) for f in range(FRAMES)], f"plynling_{sp}_baby_{state}_dirty")
                    count += 1
```

Run: `python export.py` → the count grows by 112 (for the 7 mushroom species; sunflower species also get files if they are in `SPECIES` — count and report the real number). Then `git status --short assets/plynlings`: **only `??` new files**, no `M`. Update `tools/plynling-art/README.md`: a bullet for `sick` (a card face only) and one for `dirt.py` (`_dirty`, card only, not frozen/angry/visit, no version bump).

- [ ] **Step 6: Hand over — the owner commits and pushes the art before Task 4**

Files: `tools/plynling-art/{sprites,species,motion,dirt,export}.py`, `README.md`, the new files in `assets/plynlings/`.

---

### Task 4: The bot shows the dirt

**Files:**
- Modify: `ProjectSYNCS/Helpers/PlynlingArt.cs`
- Modify: `ProjectSYNCS/Commands/PlynlingModule.cs:613`, `PlynlingPlayCards.cs:23,65`, `PlynlingJournalCards.cs:62`, `ProjectSYNCS/Services/PlynlingAnnouncer.cs:44,54`
- Test: `$SCRATCH/health/Program.cs`

**Interfaces:**
- Consumes: the `_dirty` files (pushed); `PlynlingLife.IsDirty`.
- Produces: `PlynlingArt.Sprite(species, stage, mood, bool dirty = false)`; `PlynlingArt.SpriteOf(Plynling p, DateTimeOffset now, PlynlingMood? mood = null) -> string`.

- [ ] **Step 1: Failing checks**

```csharp
var dirtyP = Fresh(t0); dirtyP.Hygiene = 0.1;
Check(PlynlingArt.SpriteOf(dirtyP, t0.AddHours(8)).EndsWith("_content_dirty_v4.webp"), "a dirty Plynling wears the dirt");
Check(!PlynlingArt.SpriteOf(Fresh(t0), t0.AddHours(8)).Contains("_dirty"), "a clean one does not");
var frozenDirty = Fresh(t0); frozenDirty.Hygiene = 0.1; PlynlingLife.Freeze(frozenDirty, t0, true);
Check(!PlynlingArt.SpriteOf(frozenDirty, t0.AddHours(8)).Contains("_dirty"), "no dirt on the frozen face");
Check(!PlynlingArt.VisitSprite(PlynlingSpecies.Cepe, PlynlingStage.Adult, PlynlingMood.Happy).Contains("_dirty"), "visits stay clean");
```

- [ ] **Step 2: Implement**

```csharp
    // Moods that have a « _dirty » picture (tools/plynling-art/export.py DIRTY_STATES). Frozen
    // hides the dirt under the ice; angry is a visit face.
    private static readonly IReadOnlySet<PlynlingMood> DirtyMoods = new HashSet<PlynlingMood>
    {
        PlynlingMood.Happy, PlynlingMood.Content, PlynlingMood.Sad, PlynlingMood.Hungry,
        PlynlingMood.Starving, PlynlingMood.Sleeping,
    };

    public static string Sprite(PlynlingSpecies species, PlynlingStage stage, PlynlingMood mood, bool dirty = false) =>
        $"{BaseUrl}plynling_{Key(species)}{StageSegment(species, stage)}_{mood.ToString().ToLowerInvariant()}" +
        $"{(dirty && DirtyMoods.Contains(mood) ? "_dirty" : "")}_v{Version}.webp";

    /// <summary>The one way to picture a living Plynling: its species, stage, mood (or the one
    /// given) and whether it is dirty.</summary>
    public static string SpriteOf(Plynling p, DateTimeOffset now, PlynlingMood? mood = null) =>
        Sprite(p.Species, PlynlingLife.Stage(p, now), mood ?? PlynlingLife.Mood(p, now),
            p.FrozenAt is null && PlynlingLife.IsDirty(p, now));
```

Replace the six calls: `PlynlingArt.Sprite(x.Species, PlynlingLife.Stage(x, now), PlynlingLife.Mood(x, now))` → `PlynlingArt.SpriteOf(x, now)`; the abandon one → `PlynlingArt.SpriteOf(plynling, now, PlynlingMood.Sad)`.

- [ ] **Step 3: Build, harness, hand over**

`dotnet build -warnaserror` → 0/0; harness → `OK`. Files: `PlynlingArt.cs` and the five call-site files.

---

## Phase 3 — The sickness rules

### Task 5: Mornings, rolls and the second migration (pure)

**Files:**
- Modify: `ProjectSYNCS/Models/Plynling.cs` (sickness columns, `DeathCause`, `PendingMoments`)
- Create: `ProjectSYNCS/Helpers/PlynlingSickness.cs`
- Modify: `ProjectSYNCS/Helpers/PlynlingLife.cs` (`Settle`, `Mood`, `Resurrect`, death helper)
- Modify: `ProjectSYNCS/Helpers/PlynlingCatalog.cs` (`PlynlingMood.Sick`, appended)
- Modify: `ProjectSYNCS/Helpers/PlynlingJournalUi.cs` (`JournalKind.FellSick`, `Recovered` appended)
- Create: migration `AddPlynlingSickness`
- Test: `$SCRATCH/health/Program.cs`

**Interfaces:**
- Produces: `public enum DeathCause { Starvation, Illness }`; `Plynling.SickSince` (DateTimeOffset?), `Recovery` (int), `LastMedicineAt` (DateTimeOffset?), `SickNotified` (bool), `DeathCause` (DeathCause); `[NotMapped] List<(JournalKind Kind, DateTimeOffset At)> Plynling.PendingMoments`; `enum RollPurpose { Onset, Death, Recovery, Medicine }`; `delegate double SicknessRoll(int plynlingId, int dayKey, RollPurpose purpose)`; `PlynlingSickness.Roll`, `OnsetChance(double hygiene) -> double`, constants; `PlynlingLife.Settle(Plynling, DateTimeOffset, SicknessRoll? roll = null) -> bool`; `PlynlingLife.IsSick(Plynling) -> bool`; `PlynlingLife.Cure(Plynling)`; `PlynlingMood.Sick`.

- [ ] **Step 1: Failing checks**

```csharp
// ---- sickness -------------------------------------------------------------------------------
Check(Near(PlynlingSickness.OnsetChance(0.50), 0.005) && Near(PlynlingSickness.OnsetChance(0.33), 0.005), "0.5 % from 33 % up");
Check(Near(PlynlingSickness.OnsetChance(0.30), 0.041) && Near(PlynlingSickness.OnsetChance(0.0), 0.401), "then 1.2 % per point");
Check(PlynlingSickness.Roll(7, 20261005, RollPurpose.Onset) == PlynlingSickness.Roll(7, 20261005, RollPurpose.Onset), "rolls repeat");
Check(PlynlingSickness.Roll(7, 20261005, RollPurpose.Onset) != PlynlingSickness.Roll(7, 20261005, RollPurpose.Death), "purposes differ");
var rs = Enumerable.Range(0, 2000).Select(i => PlynlingSickness.Roll(i, 20261005, RollPurpose.Onset)).ToList();
Check(rs.All(r => r >= 0 && r < 1) && Math.Abs(rs.Average() - 0.5) < 0.03, "rolls are uniform in [0,1)");

// A scripted roll source: onset hits on the first morning, then the death roll is the given value.
SicknessRoll Script(double onset, double death, double rec = 0.5, double med = 0.5) =>
    (id, day, purpose) => purpose switch { RollPurpose.Onset => onset, RollPurpose.Death => death, RollPurpose.Recovery => rec, _ => med };

// Full again at `at`, without going through the rules under test: HungerLife is 2 days, so a check
// spanning several mornings would otherwise starve before its illness death. Happiness is read
// before hygiene changes, as in Rebase. Only call it at the instant of the last Settle.
void Refill(Plynling x, DateTimeOffset at)
{
    var happiness = PlynlingLife.HappinessAt(x, at);
    var hygiene = PlynlingLife.HygieneAt(x, at);
    x.Hunger = 1; x.Happiness = happiness; x.Hygiene = hygiene; x.NeedsAsOf = at;
}

var s0 = Paris(2026, 10, 5, 12);
var sick = Fresh(s0); sick.Id = 11; sick.Hunger = 1; sick.Hygiene = 0;          // 20261005 already played
PlynlingLife.Settle(sick, Paris(2026, 10, 6, 6), Script(0.0, 0.99));
Check(sick.SickSince == PlynlingLife.MorningAt(20261006) && sick.Recovery == 0, "falls sick at 05:00, nothing else that morning");
Check(sick.PendingMoments.Any(m => m.Kind == JournalKind.FellSick), "the onset is journaled");
Check(PlynlingLife.Mood(sick, Paris(2026, 10, 6, 12)) == PlynlingMood.Sick, "the Sick mood");
Refill(sick, Paris(2026, 10, 6, 6));
PlynlingLife.Settle(sick, Paris(2026, 10, 7, 6), Script(0.0, 0.0));             // 2nd sick morning: grace
Check(sick.DiedAt is null && sick.Recovery == 10, "grace morning: recovery only (5 + 0.5*11 = 10)");
Refill(sick, Paris(2026, 10, 7, 6));
PlynlingLife.Settle(sick, Paris(2026, 10, 8, 6), Script(0.0, 0.0));             // 3rd: the death roll
Check(sick.DiedAt == PlynlingLife.MorningAt(20261008) && sick.DeathCause == DeathCause.Illness, "dies of illness at 05:00 on the third morning");

// Stops at the cure: hygiene is still 0 and the onset roll is scripted to hit, so one more
// morning would make it fall sick again.
var treated = Fresh(s0); treated.Id = 12; treated.Hunger = 1; treated.Hygiene = 0;
PlynlingLife.Settle(treated, Paris(2026, 10, 6, 6), Script(0.0, 0.0));
for (var d = 7; d <= 10; d++)
{
    Refill(treated, Paris(2026, 10, d - 1, 6));
    treated.LastMedicineAt = Paris(2026, 10, d - 1, 12);                          // a dose every day
    PlynlingLife.Settle(treated, Paris(2026, 10, d, 6), Script(0.0, 0.0));
}
Check(treated.DiedAt is null && treated.SickSince is null && treated.PendingMoments.Any(m => m.Kind == JournalKind.Recovered),
    "medicine cancels the death roll and heals (+30 a morning → cured on the 4th, 2026-10-10)");

var catchup = Fresh(s0); catchup.Id = 13; catchup.Hunger = 0.3; catchup.Hygiene = 1;     // starves 2026-10-06 02:24 → 05:00
PlynlingLife.Settle(catchup, Paris(2026, 10, 9, 12), Script(0.99, 0.99));
Check(catchup.DiedAt == PlynlingLife.MorningAt(20261006) && catchup.DeathCause == DeathCause.Starvation && catchup.LastMorningDay == 20261005,
    "a starvation before a morning wins, and no later morning is played");

var iced = Fresh(s0); iced.Id = 14; iced.Hygiene = 0; PlynlingLife.Freeze(iced, s0, byStaff: true);
PlynlingLife.Settle(iced, Paris(2026, 10, 9, 6), Script(0.0, 0.0));
Check(iced.SickSince is null && iced.LastMorningDay == 20261009, "frozen mornings are skipped but recorded");

var dst = Fresh(Paris(2026, 3, 28, 12)); dst.Id = 15; dst.Hunger = 1; dst.Hygiene = 0;
PlynlingLife.Settle(dst, Paris(2026, 3, 29, 6), Script(0.0, 0.99));
Check(dst.SickSince == Paris(2026, 3, 29, 5), "the spring-forward morning is still 05:00 local");
```

Run → build errors (the new types do not exist).

- [ ] **Step 2: The model**

In `Plynling.cs`, beside `PlynlingGender`:

```csharp
// Why it died. Stored as an int: **append-only**.
public enum DeathCause { Starvation, Illness }
```

In the class, after `LastMorningDay`:

```csharp
    // Sickness (Helpers/PlynlingSickness): the morning it fell ill (null when healthy), the hidden
    // 0..100 healing bar, the last dose, and whether the owner has been told it fell ill.
    public DateTimeOffset? SickSince { get; set; }
    public int Recovery { get; set; }
    public DateTimeOffset? LastMedicineAt { get; set; }
    public bool SickNotified { get; set; }
```

after `DeathAnnounced`: `public DeathCause DeathCause { get; set; }`

and at the end of the class:

```csharp
    // Moments a Settle produced (fell sick, recovered) that still have to be written to the journal —
    // Settle is pure, so the service that saved the row writes them. Never stored.
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public List<(Helpers.JournalKind Kind, DateTimeOffset At)> PendingMoments { get; } = new();
```

- [ ] **Step 3: `PlynlingSickness`**

`ProjectSYNCS/Helpers/PlynlingSickness.cs`:

```csharp
namespace ProjectSYNCS.Helpers;

public enum RollPurpose { Onset, Death, Recovery, Medicine }

// A roll in [0, 1). Settle uses PlynlingSickness.Roll; the checks pass scripted ones.
public delegate double SicknessRoll(int plynlingId, int dayKey, RollPurpose purpose);

/// <summary>
/// The sickness rules, pure. Every roll is a hash of (Plynling id, morning, purpose) — never a
/// Random — so the command that happens to settle a Plynling and the hourly sweep always reach the
/// same outcome for the same morning, and each outcome is decided once and stored.
/// </summary>
public static class PlynlingSickness
{
    public const double OnsetFloor = 0.005;          // even a clean Plynling can catch something
    public const double OnsetPerPoint = 0.012;       // per point of hygiene below 33 %
    public const double IllnessDeathChance = 0.20;
    public const int GraceMornings = 2;               // sick mornings 0 and 1 carry no death roll
    public const int RecoveryDailyMin = 5, RecoveryDailyMax = 15;
    public const int RecoveryMedicineMin = 15, RecoveryMedicineMax = 25;
    public const int Healed = 100;
    public const double SickMealFactor = 0.5;

    public static double OnsetChance(double hygiene) =>
        OnsetFloor + OnsetPerPoint * Math.Max(0, (PlynlingLife.DirtyBelow - hygiene) * 100);

    public static int Gain(double roll, int min, int max) => min + (int)(roll * (max - min + 1));

    // SplitMix64 over the three inputs: stable across runs and machines (unlike string.GetHashCode).
    public static double Roll(int plynlingId, int dayKey, RollPurpose purpose)
    {
        ulong x = (ulong)(uint)plynlingId * 0x9E3779B97F4A7C15UL
                  ^ (ulong)(uint)dayKey * 0xC2B2AE3D27D4EB4FUL
                  ^ ((ulong)purpose + 1) * 0x165667B19E3779F9UL;
        x += 0x9E3779B97F4A7C15UL;
        x = (x ^ (x >> 30)) * 0xBF58476D1CE4E5B9UL;
        x = (x ^ (x >> 27)) * 0x94D049BB133111EBUL;
        x ^= x >> 31;
        return (x >> 11) * (1.0 / (1UL << 53));
    }
}
```

- [ ] **Step 4: `PlynlingLife`: mood, death, mornings, cure**

`PlynlingCatalog.cs`: append `Sick` to `PlynlingMood` (`..., Sleeping, Angry, Sick`). `PlynlingJournalUi.cs`: append `FellSick, Recovered` at the end of `JournalKind`, and in the wording switch add
`JournalKind.FellSick => $"🤒 {g.Agree("Tombé", "Tombée")} malade.",` / `JournalKind.Recovered => $"💊 {g.Agree("Guéri", "Guérie")} !",`, and change the `Died` line to
`JournalKind.Died => detail == "illness" ? $"{g.Agree("Mort", "Morte")} de maladie." : $"{g.Agree("Mort", "Morte")} de faim.",`.

In `PlynlingLife`:

```csharp
    public static bool IsSick(Plynling p) => p.SickSince is not null;

    public static void Cure(Plynling p)
    {
        p.SickSince = null;
        p.Recovery = 0;
        p.SickNotified = false;
    }
```

`Mood`: after the `IsAsleep` line add `if (IsSick(p)) return PlynlingMood.Sick;`.

Replace `Settle` with:

```csharp
    public static bool Settle(Plynling p, DateTimeOffset now, SicknessRoll? roll = null)
    {
        if (IsDead(p)) return false;
        roll ??= PlynlingSickness.Roll;
        var changed = false;

        // Frozen until when? Mornings before the thaw are skipped, even after the thaw below.
        var frozenUntil = IsFrozen(p) ? (p.FreezeUntil ?? DateTimeOffset.MaxValue) : DateTimeOffset.MinValue;
        if (IsFrozen(p) && p.FreezeUntil is { } until && until <= now)
        {
            EndFreeze(p, until);
            changed = true;
        }

        // Every morning since the last one played, oldest first — unless it starves before it.
        while (true)
        {
            var day = NextDay(p.LastMorningDay);
            var morning = MorningAt(day);
            if (morning > now) break;
            if (EffectiveDeathAt(p) is { } starve && starve <= morning) break;
            p.LastMorningDay = day;
            changed = true;
            if (morning < frozenUntil || IsFrozen(p)) continue;
            PlayMorning(p, morning, day, roll);
            if (IsDead(p)) return true;
        }

        if (EffectiveDeathAt(p) is { } death && death <= now)
        {
            Die(p, death, DeathCause.Starvation);
            changed = true;
        }
        return changed;
    }

    // One morning: the onset roll when healthy; otherwise the death roll (from the third sick
    // morning, unless dosed since the previous morning), then the day's recovery.
    private static void PlayMorning(Plynling p, DateTimeOffset morning, int day, SicknessRoll roll)
    {
        if (!IsSick(p))
        {
            if (roll(p.Id, day, RollPurpose.Onset) < PlynlingSickness.OnsetChance(HygieneAt(p, morning)))
            {
                p.SickSince = morning;
                p.Recovery = 0;
                p.SickNotified = false;
                p.PendingMoments.Add((JournalKind.FellSick, morning));
            }
            return;
        }

        var sickMorning = (int)Math.Round((morning - p.SickSince!.Value).TotalDays);   // 0 = the onset
        var previous = MorningAt(MorningDayAtOrBefore(morning - TimeSpan.FromHours(1)));
        var dosed = p.LastMedicineAt is { } dose && dose >= previous;
        if (sickMorning >= PlynlingSickness.GraceMornings && !dosed
            && roll(p.Id, day, RollPurpose.Death) < PlynlingSickness.IllnessDeathChance)
        {
            Die(p, morning, DeathCause.Illness);
            return;
        }

        p.Recovery += PlynlingSickness.Gain(roll(p.Id, day, RollPurpose.Recovery),
            PlynlingSickness.RecoveryDailyMin, PlynlingSickness.RecoveryDailyMax);
        if (dosed)
            p.Recovery += PlynlingSickness.Gain(roll(p.Id, day, RollPurpose.Medicine),
                PlynlingSickness.RecoveryMedicineMin, PlynlingSickness.RecoveryMedicineMax);
        if (p.Recovery >= PlynlingSickness.Healed)
        {
            Cure(p);
            p.PendingMoments.Add((JournalKind.Recovered, morning));
        }
    }

    private static void Die(Plynling p, DateTimeOffset at, DeathCause cause)
    {
        Rebase(p, at);
        p.AgeBankedSeconds += (long)(at - p.LiveSince).TotalSeconds;
        if (cause == DeathCause.Starvation) p.Hunger = 0;
        p.DiedAt = at;
        p.DeathCause = cause;
    }
```

(`sickMorning` uses rounded days because a DST night is 23 or 25 hours.) In `Resurrect`, add `Cure(p);` and `p.DeathCause = DeathCause.Starvation;`. In `SelfFreezeBlocker`, add `: IsSick(p) ? FreezeOutcome.Sick` before the cooldown check, and append `Sick` to `FreezeOutcome`. In `CanDrawGift`, add `&& !IsSick(p)`.

- [ ] **Step 5: The migration**

Run: `cd "$REPO/ProjectSYNCS" && dotnet ef migrations add AddPlynlingSickness` — check it adds `SickSince`, `Recovery` (default 0), `LastMedicineAt`, `SickNotified` (default false), `DeathCause` (default 0) and nothing for `PendingMoments`.

- [ ] **Step 6: Run**

`dotnet build -warnaserror` → 0/0 (fix every `switch` on `PlynlingMood` / `FreezeOutcome` the compiler flags: `PlynlingCardUi.MoodLabel` gets `PlynlingMood.Sick => gender.Agree("malade", "malade"),`; the freeze command's switch gets `FreezeOutcome.Sick => PlynlingText.SickNoFreeze(g),` — add that text in Task 7). Harness → `OK`.

- [ ] **Step 7: Hand over**

Files: `Plynling.cs`, `PlynlingSickness.cs`, `PlynlingLife.cs`, `PlynlingCatalog.cs`, `PlynlingJournalUi.cs`, `PlynlingCardUi.cs`, the migration files. **Do not ship this alone**: until Task 6–8 land, a Plynling could fall sick with no way to treat it.

---

## Phase 4 — Sickness in the bot

### Task 6: Pending moments, the medicine item and « Soigner »

**Files:**
- Modify: `ProjectSYNCS/Services/PlynlingService.cs` (flush moments; `MedicateAsync`; `CureAsync`; `DebugSetAsync`)
- Modify: `ProjectSYNCS/Services/PlynlingSweepService.cs` (flush moments)
- Modify: `ProjectSYNCS/Helpers/ItemCatalog.cs` (`ItemKind.Care`, `care.medicine`, `BulkPrice`)
- Modify: `ProjectSYNCS/Services/InventoryService.cs` (`BuyMedicineAsync`)
- Modify: `ProjectSYNCS/Commands/InventoryModule.cs` (`/inventory medicine`; the pantry line)
- Modify: `ProjectSYNCS/Services/PlynlingCareService.cs`, `PlynlingComponentHandler.cs`, `PlynlingModule.BuildCard`
- Modify: `ProjectSYNCS/Helpers/EconomyLog.cs`, `PlynlingText.cs`, `BotResponses.cs`
- Test: harness

**Interfaces:**
- Consumes: Task 5's model and `PlynlingLife`.
- Produces: `ItemCatalog.MedicineKey = "care.medicine"`, `ItemCatalog.MedicinePrice = 30`, `ItemCatalog.BulkPrice(long unit, int quantity)`; `InventoryService.BuyMedicineAsync(ulong guildId, ulong userId, int quantity, DateTimeOffset now) -> (bool Bought, long Price, long Balance)`; `CareOutcome.NotSick`, `CareOutcome.AlreadyTreated` (appended); `PlynlingService.MedicateAsync(int plynlingId, ulong actorId, DateTimeOffset now) -> (CareOutcome, Plynling?, bool FromPantry, long Price, long Balance)`; `PlynlingService.FlushMomentsAsync(Plynling)`; custom-id `plyn:heal:{id}`; `BotResponses.PlynlingMedicineLines`; `EconomyLog.ActMedicine`, `SpendMedicine`.

- [ ] **Step 1: Failing checks**

```csharp
Check(ItemCatalog.ByKey(ItemCatalog.MedicineKey) is { Kind: ItemKind.Care, Name: "Médicament" }, "the medicine item exists");
Check(ItemCatalog.BulkPrice(30, 5) == 135 && ItemCatalog.BulkPrice(30, 4) == 120, "the 10 % bulk discount from 5");
var ill = Fresh(t0); ill.Id = 21; ill.SickSince = t0; ill.LastMorningDay = PlynlingLife.MorningDayAtOrBefore(t0);
var illCard = ProjectSYNCS.Commands.PlynlingModule.BuildCard(ill, t0, null);
var illIds = illCard.Components.OfType<Discord.ActionRowComponent>().SelectMany(r => r.Components).OfType<Discord.ButtonComponent>().Select(x => x.CustomId).ToList();
Check(illIds.Contains("plyn:heal:21"), "Soigner appears while sick");
Check(!ids.Contains("plyn:heal:3"), "and not while healthy");
```

- [ ] **Step 2: Flush pending moments wherever a Plynling is settled and saved**

In `PlynlingService`:

```csharp
    // Writes the moments a Settle produced (Plynling.PendingMoments) into the journal. Not saved:
    // the caller's save carries them.
    public async Task FlushMomentsAsync(Plynling p)
    {
        foreach (var (kind, at) in p.PendingMoments) await AddMomentAsync(p, kind, null, at);
        p.PendingMoments.Clear();
    }
```

Call it in `SettledAsync` before `SaveChangesAsync`, in `GetGraveyardAsync` and `GetLivingAsync` for each settled row before their save, and in `PlynlingSweepService.SweepAsync` right after `PlynlingLife.Settle(plynling, now);` (`await plynlings.FlushMomentsAsync(plynling);`). `JournalDeathAsync` passes the cause: `await AddMomentAsync(p, JournalKind.Died, p.DeathCause == DeathCause.Illness ? "illness" : null, died);`.

- [ ] **Step 3: The item and its purchase**

`ItemCatalog.cs`: append `Care` to `ItemKind` (`Food, Collectible, Cosmetic, Care`); add

```csharp
    // The one care item: bought, held in the pantry, given from the card while sick. Its key is
    // stored, so never renamed.
    public const string MedicineKey = "care.medicine";
    public const long MedicinePrice = 30;

    public static long BulkPrice(long unit, int quantity)
    {
        var full = unit * quantity;
        return quantity >= 5 ? (long)Math.Round(full * 0.9, MidpointRounding.AwayFromZero) : full;
    }
```

rewrite `ShopPrice` as `=> BulkPrice(food.Price, quantity);`, and in `BuildAll`, after the foods:
`items.Add(new ItemInfo(MedicineKey, ItemKind.Care, "💊", "Médicament", null, ItemRarity.Common, Season.None, null));`

`InventoryService`:

```csharp
    public async Task<(bool Bought, long Price, long Balance)> BuyMedicineAsync(ulong guildId, ulong userId, int quantity, DateTimeOffset now)
    {
        var price = ItemCatalog.BulkPrice(ItemCatalog.MedicinePrice, quantity);
        var wallet = await PebbleService.GetOrCreateWalletAsync(_db_context, guildId, userId);
        if (wallet.Balance < price) return (false, price, wallet.Balance);
        wallet.Balance -= price;
        await AddAsync(_db_context, guildId, userId, ItemCatalog.MedicineKey, quantity, now);
        await EconomyLog.AddAsync(_db_context, guildId, EconomyLog.SpendShop, price, now);
        await _db_context.SaveChangesAsync();
        return (true, price, wallet.Balance);
    }
```

`InventoryModule`, after `ShopAsync`:

```csharp
    [SlashCommand("medicine", "Acheter des médicaments pour soigner un Plynling malade (−10 % dès 5)")]
    public async Task MedicineAsync([Summary("quantity", "Combien (1 à 20)")] [MinValue(1)] [MaxValue(MaxShopQuantity)] int quantity = 1)
    {
        var (bought, price, balance) = await _inventory.BuyMedicineAsync(Context.Guild.Id, Context.User.Id, quantity, DateTimeOffset.UtcNow);
        await RespondAsync(bought
                ? PlynlingText.Bought(quantity, "Médicament", price, balance, discounted: quantity >= 5)
                : $"{PlynlingText.ShopTooPoor} ({PebbleEconomy.Cailloux(price)}, tu en as {PebbleEconomy.Cailloux(balance)})",
            ephemeral: true);
    }
```

In `BuildInventoryPage`'s pantry, append a line `💊 Médicament : **{Count(ItemCatalog.MedicineKey)}**` after the foods.

- [ ] **Step 4: `MedicateAsync`**

Append `NotSick, AlreadyTreated` to `CareOutcome`. In `PlynlingService`:

```csharp
    // Owner only, once between two mornings. A dose from the pantry, else MedicinePrice from the
    // wallet — the feeding rule — in one save.
    public async Task<(CareOutcome Outcome, Plynling? Plynling, bool FromPantry, long Price, long Balance)> MedicateAsync(
        int plynlingId, ulong actorId, DateTimeOffset now)
    {
        var plynling = await GetByIdAsync(plynlingId, now);
        if (plynling is null) return (CareOutcome.NoPlynling, null, false, 0, 0);
        var wallet = await PebbleService.GetOrCreateWalletAsync(_db_context, plynling.GuildId, actorId);
        var fromPantry = await InventoryService.CountAsync(_db_context, plynling.GuildId, actorId, ItemCatalog.MedicineKey) >= 1;
        var lastMorning = PlynlingLife.MorningAt(plynling.LastMorningDay);
        CareOutcome? refusal =
            plynling.DiedAt is not null ? CareOutcome.Dead
            : plynling.OwnerId != actorId ? CareOutcome.NotOwner
            : plynling.FrozenAt is not null ? CareOutcome.Frozen
            : PlynlingLife.IsAsleep(now) ? CareOutcome.Asleep
            : !PlynlingLife.IsSick(plynling) ? CareOutcome.NotSick
            : plynling.LastMedicineAt is { } dose && dose >= lastMorning ? CareOutcome.AlreadyTreated
            : !fromPantry && wallet.Balance < ItemCatalog.MedicinePrice ? CareOutcome.TooPoor
            : null;
        if (refusal is { } r) return (r, plynling, false, ItemCatalog.MedicinePrice, wallet.Balance);

        if (fromPantry) await InventoryService.TakeAsync(_db_context, plynling.GuildId, actorId, ItemCatalog.MedicineKey, 1);
        else
        {
            wallet.Balance -= ItemCatalog.MedicinePrice;
            await EconomyLog.AddAsync(_db_context, plynling.GuildId, EconomyLog.SpendMedicine, ItemCatalog.MedicinePrice, now);
        }
        plynling.LastMedicineAt = now;
        await EconomyLog.AddAsync(_db_context, plynling.GuildId, EconomyLog.ActMedicine, 1, now);
        await _db_context.SaveChangesAsync();
        return (CareOutcome.Done, plynling, fromPantry, fromPantry ? 0 : ItemCatalog.MedicinePrice, wallet.Balance);
    }
```

`EconomyLog`: `public const string SpendMedicine = "spend.medicine";` beside the spends, `public const string ActMedicine = "act.medicine";` beside the acts, and append `ActMedicine` to `Activities`.

- [ ] **Step 5: The button, the care service, the handler**

`BuildCard` — in the same row as Caresser and Laver:

```csharp
            if (!PlynlingLife.IsAsleep(now))
            {
                var row = new ActionRowBuilder()
                    .WithButton("🤲 Caresser", $"plyn:pet:{plynling.Id}", ButtonStyle.Primary)
                    .WithButton("🛁 Laver", $"plyn:bath:{plynling.Id}", ButtonStyle.Secondary);
                if (PlynlingLife.IsSick(plynling))
                    row.WithButton("💊 Soigner", $"plyn:heal:{plynling.Id}", ButtonStyle.Success);
                builder.AddComponent(row);
            }
```

`PlynlingCareService`:

```csharp
    public async Task<CareReply> MedicateAsync(int plynlingId, ulong actorId, ulong channelId, DateTimeOffset now)
    {
        var (outcome, plynling, fromPantry, price, balance) = await _plynlings.MedicateAsync(plynlingId, actorId, now);
        if (outcome != CareOutcome.Done || plynling is null)
        {
            var g = plynling?.Gender ?? PlynlingGender.Male;
            return new CareReply(null, outcome switch
            {
                CareOutcome.TooPoor => PlynlingText.TooPoor(price, balance),
                CareOutcome.NotSick => PlynlingText.NotSick(g),
                CareOutcome.AlreadyTreated => PlynlingText.AlreadyTreated(g),
                _ => Refusal(outcome, g),
            });
        }
        var line = string.Format(_picker.Pick(channelId, BotResponses.PlynlingMedicineLines.For(plynling.Gender)),
            PlynlingCardUi.SafeName(plynling.Name));
        line += fromPantry ? "\n-# 💊 un médicament de ton garde-manger" : $"\n-# −{PebbleEconomy.Cailloux(price)} · il te reste {PebbleEconomy.Cailloux(balance)}";
        var partner = await _plynlings.GetPartnerAsync(plynling);
        return new CareReply(PlynlingModule.BuildCard(plynling, now, line, partnerName: partner?.Name), null);
    }
```

`PlynlingComponentHandler`: `[ComponentInteraction("plyn:heal:*", ignoreGroupNames: true)] public async Task OnHealAsync(string idStr)` — the same body as `OnBathAsync`, calling `_care.MedicateAsync`.

`PlynlingText`:

```csharp
    public static string NotSick(PlynlingGender g) => $"{g.Agree("Il", "Elle")} n'est pas {g.Agree("malade", "malade")} : pas besoin de médicament.";
    public static string AlreadyTreated(PlynlingGender g) =>
        $"{g.Agree("Il", "Elle")} a déjà eu son médicament aujourd'hui. Le prochain après 5 h.";
```

`BotResponses.PlynlingMedicineLines` ({0} = name):

```
💊 **{0}** avale son médicament en faisant une grimace héroïque. Courage ♡
Une cuillère pour **{0}**… ⟨il|elle⟩ a tout recraché. Deuxième essai : réussi (¬_¬)
**{0}** prend son médicament et se blottit contre toi. Ça va aller (╥﹏╥)
Médicament donné. **{0}** fait semblant d'aller mieux pour te faire plaisir. Ça marche un peu.
**{0}** trouve que le médicament a un goût de caillou mouillé. Moi je trouve que ⟨il|elle⟩ exagère.
Et hop, la dose du jour ! **{0}** a le droit à un bisou sur le front en échange.
**{0}** tend la patte pour son médicament comme ⟨un grand|une grande⟩. Je suis fière de ⟨lui|elle⟩ ✨
Le médicament est pris. **{0}** va dormir un peu mieux ce soir (˶ᵔ ᵕ ᵔ˶)
```

- [ ] **Step 6: Build, harness, hand over**

0/0; `OK`. Check `/inventory` still has ≤ 25 subcommands (now 9). Files: every file listed above.

---

### Task 7: What sickness changes — meals, games, visits, freezing

**Files:**
- Modify: `ProjectSYNCS/Helpers/PlynlingLife.cs` (`Feed`)
- Modify: `ProjectSYNCS/Services/PlynlingService.cs` (`VisitAsync` guard)
- Modify: `ProjectSYNCS/Commands/PlynlingModule.cs` (play, visit, freeze refusals)
- Modify: `ProjectSYNCS/Interactions/Components/PlynlingComponentHandler.cs` (visit accept refusal)
- Modify: `ProjectSYNCS/Services/PlynlingCareService.cs` (the half-meal note)
- Modify: `ProjectSYNCS/Helpers/PlynlingText.cs`

**Interfaces:**
- Consumes: `PlynlingLife.IsSick`, `PlynlingSickness.SickMealFactor`, `FreezeOutcome.Sick`.

- [ ] **Step 1: Failing checks**

```csharp
var eater = Fresh(t0); eater.Hunger = 0.4; eater.Happiness = 0.5; eater.SickSince = t0;
PlynlingLife.Feed(eater, PlynlingCatalog.Info(PlynlingFood.Mushroom), t0);
Check(Near(eater.Hunger, 0.4 + PlynlingCatalog.Info(PlynlingFood.Mushroom).Hunger * 0.5), "a sick Plynling gets half the meal");
Check(PlynlingLife.SelfFreezeBlocker(eater, t0) == FreezeOutcome.Sick || PlynlingLife.SelfFreezeBlocker(eater, t0) == FreezeOutcome.TooHungry,
    "no self-freeze while sick");
var giftee = Fresh(t0); giftee.Happiness = 1; giftee.SickSince = t0;
Check(!PlynlingLife.CanDrawGift(giftee, t0.AddHours(8)), "no gift while sick");
```

- [ ] **Step 2: Implement**

`PlynlingLife.Feed`: `var factor = MealFactor(p, now) * (IsSick(p) ? PlynlingSickness.SickMealFactor : 1);`

`PlynlingModule.PlayAsync` refusal chain, after the asleep line: `: PlynlingLife.IsSick(plynling) ? PlynlingText.SickNoPlay(plynling.Gender)`.
`PlynlingModule.VisitAsync`, after the frozen line: `: PlynlingLife.IsSick(mine) ? PlynlingText.VisitSick(PlynlingCardUi.SafeName(mine.Name)) : PlynlingLife.IsSick(theirs) ? PlynlingText.VisitSick(PlynlingCardUi.SafeName(theirs.Name))`.
`PlynlingComponentHandler.OnVisitAcceptedAsync`, the same two lines after its frozen line.
`PlynlingService.VisitAsync`: `if (PlynlingLife.IsSick(visitor) || PlynlingLife.IsSick(host)) return null;` after the frozen guard.
The freeze command's outcome switch: `FreezeOutcome.Sick => PlynlingText.SickNoFreeze(g),`.
`PlynlingCareService.FeedAsync`: after the meal-mood line, `if (PlynlingLife.IsSick(result.Plynling)) text += $"\n*{PlynlingText.SickMeal(g)}*";`

`PlynlingText`:

```csharp
    public static string SickNoPlay(PlynlingGender g) => $"{g.Agree("Il", "Elle")} est malade : pas de jeux avant d'être {g.Agree("guéri", "guérie")}. Un médicament, peut-être ?";
    public static string VisitSick(string name) => $"**{name}** est malade : pas de visite pour l'instant.";
    public static string SickNoFreeze(PlynlingGender g) => $"{g.Agree("Il", "Elle")} est malade : on ne gèle pas un Plynling malade, on le soigne.";
    public static string SickMeal(PlynlingGender g) => $"Malade, {g.Agree("il", "elle")} n'a mangé que la moitié.";
```

- [ ] **Step 3: Build, harness, hand over** — 0/0; `OK`. Files as listed.

---

### Task 8: Telling the owner — the onset DM, illness deaths, the memorial

**Files:**
- Modify: `ProjectSYNCS/Services/PlynlingSweepService.cs`
- Modify: `ProjectSYNCS/Services/PlynlingAnnouncer.cs`
- Modify: `ProjectSYNCS/Helpers/PlynlingCardUi.cs` (the memorial line)
- Modify: `ProjectSYNCS/Services/BotResponses.cs` (`PlynlingSickWarningLines`, `PlynlingIllnessDeathLines`)

**Interfaces:**
- Produces: `PlynlingAnnouncer.WarnSickAsync(Plynling)`.

- [ ] **Step 1: The sweep**

In `SweepAsync`, between the death branch and the starvation warning:

```csharp
                else if (PlynlingLife.IsSick(plynling) && !plynling.SickNotified)
                {
                    // Flag saved before the DM, like DeathAnnounced: one attempt, never a repeat.
                    plynling.SickNotified = true;
                    await plynlings.SaveAsync();
                    await _announcer.WarnSickAsync(plynling);
                }
```

- [ ] **Step 2: The announcer**

```csharp
    public Task WarnSickAsync(Plynling plynling)
    {
        var line = string.Format(_picker.Pick(plynling.OwnerId, BotResponses.PlynlingSickWarningLines.For(plynling.Gender)),
            PlynlingCardUi.SafeName(plynling.Name));
        return DmOwnerAsync(plynling.OwnerId, line);
    }
```

In `AnnounceDeathAsync`, choose the pool: `var pool = plynling.DeathCause == DeathCause.Illness ? BotResponses.PlynlingIllnessDeathLines : BotResponses.PlynlingDeathLines;` (same four format arguments).

- [ ] **Step 3: The memorial line**

`PlynlingCardUi.Status`, the dead branch: `{p.Gender.Agree("Mort", "Morte")}{(p.DeathCause == DeathCause.Illness ? " de maladie" : "")} <t:…:R>`.

- [ ] **Step 4: The pools**

`PlynlingSickWarningLines` ({0} = name):

```
🤒 **{0}** ne va pas bien du tout. ⟨Il|Elle⟩ est malade. Un médicament par jour, et vite (╥﹏╥)
Alerte : **{0}** est tombé⟨|e⟩ malade. `/inventory medicine`, puis « 💊 Soigner » sur sa carte. Tous les jours.
**{0}** a de la fièvre. Je ne panique pas. Je te préviens, c'est tout. Soigne-⟨le|la⟩ chaque jour.
Petit message pour te dire que **{0}** est malade. Un médicament par jour et ça passera. Sans… je préfère ne pas y penser.
**{0}** tousse, renifle et fait une tête de chou-fleur fané. Malade. Soigne-⟨le|la⟩, s'il te plaît ♡
Mauvaise nouvelle : **{0}** est malade. Bonne nouvelle : ça se soigne. Mais seulement si tu le fais.
```

`PlynlingIllnessDeathLines` ({0} name, {1} owner mention, {2} time lived, {3} memorial):

```
🕯️ **{0}** s'est éteint⟨|e⟩ cette nuit, emporté⟨|e⟩ par la maladie. {1}, ⟨il|elle⟩ a vécu {2}. ⟨Il|Elle⟩ repose sous {3}.
🕯️ La maladie a eu raison de **{0}**. {1}, je suis désolée. {2} de vie, et maintenant {3} (╥﹏╥)
🕯️ **{0}** n'a pas guéri. Un médicament par jour, c'était tout ce qu'il fallait, {1}. ⟨Il|Elle⟩ repose sous {3}.
🕯️ **{0}** est parti⟨|e⟩ au petit matin, malade depuis trop longtemps. {2} de vie. {1}, prends soin du prochain.
🕯️ Silence au village : **{0}** est mort⟨|e⟩ de maladie après {2}. {3} ⟨le|la⟩ garde désormais.
🕯️ **{0}** n'a pas passé la nuit. {1}… la prochaine fois, un bain, un médicament. Je le note pour toi.
```

- [ ] **Step 5: Build, gender check, hand over** — 0/0; F halves carry no masculine agreement. Files as listed.

---

### Task 9: `/admin plynling cure` and `/debug plynling`

**Files:**
- Modify: `ProjectSYNCS/Services/PlynlingService.cs` (`CureAsync`, `DebugSetAsync`)
- Modify: `ProjectSYNCS/Commands/AdminModule.cs` (the plynling group)
- Modify: `ProjectSYNCS/Commands/DebugModule.cs`
- Modify: `ProjectSYNCS/Helpers/PlynlingLife.cs` (`DebugSet`)

**Interfaces:**
- Produces: `enum CureOutcome { Cured, NoPlynling, Dead, NotSick }`; `PlynlingService.CureAsync(ulong guildId, ulong ownerId, DateTimeOffset now) -> (CureOutcome, Plynling?)`; `PlynlingService.DebugSetAsync(ulong guildId, ulong ownerId, int? hygiene, bool? sick, DateTimeOffset now) -> Plynling?`; `PlynlingLife.DebugSet(Plynling, DateTimeOffset, int? hygiene, bool? sick)`.

- [ ] **Step 1: The rules and the service**

`PlynlingLife`:

```csharp
    // /debug plynling: set hygiene (0..100) and/or sickness on the owner's own Plynling, to test.
    public static void DebugSet(Plynling p, DateTimeOffset now, int? hygiene, bool? sick)
    {
        Rebase(p, now);
        if (hygiene is { } h) p.Hygiene = Clamp(h / 100.0);
        if (sick == true && !IsSick(p)) { p.SickSince = MorningAt(MorningDayAtOrBefore(now)); p.Recovery = 0; p.SickNotified = false; }
        if (sick == false) Cure(p);
    }
```

`PlynlingService`:

```csharp
    public enum CureOutcome { Cured, NoPlynling, Dead, NotSick }

    // Staff: cured at once, silently (no DM — the owner sees it on the card).
    public async Task<(CureOutcome Outcome, Plynling? Plynling)> CureAsync(ulong guildId, ulong ownerId, DateTimeOffset now)
    {
        var p = await GetCurrentAsync(guildId, ownerId, now);
        if (p is null) return (CureOutcome.NoPlynling, null);
        if (p.DiedAt is not null) return (CureOutcome.Dead, p);
        if (!PlynlingLife.IsSick(p)) return (CureOutcome.NotSick, p);
        PlynlingLife.Cure(p);
        await AddMomentAsync(p, JournalKind.Recovered, null, now);
        await _db_context.SaveChangesAsync();
        return (CureOutcome.Cured, p);
    }

    public async Task<Plynling?> DebugSetAsync(ulong guildId, ulong ownerId, int? hygiene, bool? sick, DateTimeOffset now)
    {
        var p = await GetCurrentAsync(guildId, ownerId, now);
        if (p is null || p.DiedAt is not null) return null;
        PlynlingLife.DebugSet(p, now, hygiene, sick);
        await _db_context.SaveChangesAsync();
        return p;
    }
```

(Put the `CureOutcome` enum at namespace level beside `CareOutcome`, not inside the class.)

- [ ] **Step 2: `/admin plynling cure`**

In `AdminModule`'s plynling group, after `resurrect`:

```csharp
        [SlashCommand("cure", "Guérir le Plynling malade de quelqu'un")]
        public async Task CureAsync([Summary("user", "À qui est le Plynling")] IUser user)
        {
            if (!SessionPermissions.IsStaff(Context.User))
            {
                await RespondAsync(PlynlingText.StaffOnly, ephemeral: true);
                return;
            }
            var (outcome, plynling) = await _plynlings.CureAsync(Context.Guild.Id, user.Id, DateTimeOffset.UtcNow);
            await RespondAsync(outcome switch
            {
                CureOutcome.Cured => $"💊 **{PlynlingCardUi.SafeName(plynling!.Name)}** est {plynling.Gender.Agree("guéri", "guérie")}.",
                CureOutcome.NotSick => $"**{PlynlingCardUi.SafeName(plynling!.Name)}** n'est pas malade.",
                CureOutcome.Dead => PlynlingText.Dead(plynling!.Gender),
                _ => PlynlingText.NoneFor(user.Id),
            }, ephemeral: true, allowedMentions: AllowedMentions.None);
        }
```

- [ ] **Step 3: `/debug plynling`**

In `DebugModule`, following the owner check pattern of `absent`:

```csharp
    [SlashCommand("plynling", "Régler l'hygiène ou la maladie de ton propre Plynling (tests)")]
    public async Task PlynlingAsync(
        [Summary("hygiene", "Hygiène en %")] [MinValue(0)] [MaxValue(100)] int? hygiene = null,
        [Summary("sick", "Malade ou non")] bool? sick = null)
    {
        if (Context.User.Id != AvailabilityService.OwnerId)
        {
            await RespondAsync("Seul Rodhengard peut utiliser cette commande.", ephemeral: true);
            return;
        }
        if (Context.Guild is null)
        {
            await RespondAsync("Sur un serveur, pas en message privé.", ephemeral: true);
            return;
        }
        var p = await _plynlings.DebugSetAsync(Context.Guild.Id, Context.User.Id, hygiene, sick, DateTimeOffset.UtcNow);
        await RespondAsync(p is null ? PlynlingText.NoPlynling
                : $"🔧 **{PlynlingCardUi.SafeName(p.Name)}** : hygiène {Math.Round(p.Hygiene * 100)} %, {(PlynlingLife.IsSick(p) ? "malade" : "en bonne santé")}.",
            ephemeral: true, allowedMentions: AllowedMentions.None);
    }
```

Inject `PlynlingService` into `DebugModule` (constructor) if it is not already there. The refusal is the literal the module's three other commands already send.

- [ ] **Step 4: Build, hand over** — 0/0. Files as listed.

---

### Task 10: Docs, the dev-guild test, the version

**Files:**
- Modify: `ProjectSYNCS/Commands/PlynlingModule.cs` (`BuildHelpEmbed`), `README.md`, `CLAUDE.md`, `ProjectSYNCS/config.yaml`

- [ ] **Step 1: `/plynling help`**

Add to `BuildHelpEmbed` a field « Propreté et santé »:
« **🛁 Laver** (sur la carte, le propriétaire) — Toutes les 6 h. Sale sous 33 % : il déprime plus vite.
Sale, il peut tomber **malade** un matin (rarement même propre). Malade : il mange moitié moins, ne joue plus, ne rend plus visite.
**💊 Soigner** (sur la carte) — Un médicament par jour (`/inventory medicine`). Sans soins, il peut en mourir. »
Re-measure the embed (≤ 6000, each field ≤ 1024) in the harness: `Check(PlynlingModule.BuildHelpEmbed().Length <= 6000 && PlynlingModule.BuildHelpEmbed().Fields.All(x => x.Value.Length <= 1024), "plynling help caps");`

- [ ] **Step 2: README and CLAUDE.md**

README: the `/inventory` row gains `medicine`; the Plynlings section gains a paragraph on Hygiène, « Laver », sickness and « Soigner »; `/admin plynling` row gains `cure`. CLAUDE.md, after « Happiness changes a meal's worth… »: a note that (1) hygiene is stored at `NeedsAsOf` and happiness drains 1.5× below 33 % from the exact crossing; (2) **mornings are played in `Settle`, in time order, once each, with rolls hashed from (id, day, purpose) so a command and the sweep agree — never a `Random`**; (3) `PendingMoments` is how pure `Settle` hands journal moments to whoever saves; (4) `DeathCause` and the new `JournalKind` values are append-only; (5) the `_dirty` art is card-only and not on frozen; (6) `/admin plynling cure` sends no DM on purpose; `/debug plynling` exists because sickness is rare.

- [ ] **Step 3: Version**

`config.yaml`: bump the version by 0.0.1.

- [ ] **Step 4: The dev-guild test (the owner runs it)**

1. `/plynling view` → the Hygiène bar and « Laver »; press it → +60 %, the line, then the cooldown refusal.
2. `/debug plynling hygiene:10` → « · sale » and the dirt on the face.
3. `/debug plynling sick:true` → the malade face and « 💊 Soigner »; `/inventory medicine`; « Soigner » → pantry used; a second press → « déjà eu son médicament ».
4. `/plynling play`, a visit, a self-freeze → the three refusals; a meal → « n'a mangé que la moitié ».
5. `/admin plynling cure user:@you` → cured, no DM.
6. Next morning after 05:00: the journal shows the moments; the sweep sends the onset DM for a Plynling made sick by the roll (not by `/debug`, which sets `SickNotified` false — so it DMs too: expected).

- [ ] **Step 5: Hand over** — files: `PlynlingModule.cs`, `README.md`, `CLAUDE.md`, `config.yaml`.
