# Plynlings v1 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add Plynlings — a Tamagotchi-style mushroom pet each member can adopt, feed, pet, freeze and lose to starvation — together with a small economy (cailloux, earned by `/work` and passively), a public graveyard, and a dedicated `/plynling help`.

**Architecture:** Nothing ticks. A Plynling stores its needs *as they were at an instant* and a pure helper (`PlynlingLife`) derives every current value from the time elapsed; every transition (feed, pet, freeze, thaw, death, resurrection) *rebases* by computing the current values and storing them with a new instant. `PlynlingLife.Settle` brings a Plynling up to "now" (auto-thaw, then death at the exact instant it starved) and every read goes through it, so a lazily-discovered death is indistinguishable from one the hourly sweep found. The only background job is an hourly `PlynlingSweepService` that settles, announces deaths and sends the single warning DM.

**Tech Stack:** .NET 10, Discord.Net 3.20.1 (Components V2: `ContainerBuilder`, `SectionBuilder`, `ThumbnailBuilder`, `SelectMenuBuilder`), EF Core 10 / SQLite, Python 3 + Pillow (art export only).

## Global Constraints

- **Never commit.** The user commits manually. Every task ends at a hand-off checkpoint, not a commit.
- `dotnet build -warnaserror` from `ProjectSYNCS/` must stay clean after every task.
- No test project exists. Verification is (a) scratch console harnesses in `$SCRATCH` (your own temp dir) that reference the real project, (b) the build, (c) for persistence, a real throwaway SQLite database. Harness csproj, used by every task:
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
  `internal` types (`BotResponses`) are reached by reflection through `typeof(ProjectSYNCS.Services.XpService).Assembly`.
- **Language:** command and option names are English; every user-facing string (descriptions, replies, labels, choice display names) is French.
- **Names are hostile input.** Every rendered Plynling name goes through `PlynlingCardUi.SafeName` (`Discord.Format.Sanitize`), and every message that shows a name or an owner mention is sent with `AllowedMentions.None`.
- **Components V2:** a V2 message has no content and no embeds; `MessageFlags.ComponentsV2` is re-asserted on every `UpdateAsync`; every button/select row gets its own custom-id verb (`COMPONENT_CUSTOM_ID_DUPLICATED` crashed prod before); 40 components per message maximum.
- **SQLite cannot translate `DateTimeOffset` comparisons.** Filter on ids/bools/nulls in SQL, `ToListAsync()`, then compare/order dates in memory.
- Every `ulong` snowflake property gets `.HasConversion<long>()`.
- `AppDbContext` is **transient**: two services = two contexts = two saves. Anything that must be atomic (paying + feeding) happens in one service, one `SaveChangesAsync`.
- Singletons never inject a DB service; they take `IServiceProvider` and `CreateAsyncScope()` per unit of work.
- Every hosted loop catches **per item** (an exception escaping `ExecuteAsync` stops the whole host).
- Every repeated line goes through `ResponsePicker.Pick`; refusals are fixed `const` lines. Every new `BotResponses` pool is added to the file's table of contents.
- **Images** are served from `https://raw.githubusercontent.com/RMelisen/DiscordBot.SessionOrganizer/main/assets/plynlings/`, 256×256, versioned filenames (`…_v1.png`). **They only resolve once `assets/plynlings/` is pushed to `main`** — until then cards show a broken thumbnail. This is expected during dev testing.
- **Agreed numbers (verbatim):**
  - Hunger 100%→0% in **4 days**, happiness in **2 days**, both linear. Hunger 0% = death.
  - New Plynling **70% / 70%**; resurrected **50% / 50%**.
  - Self-freeze only while hunger **≥ 50%**, at most **14 days** (auto-thaw), owner may thaw early, **7-day** cooldown after a self-freeze ends. Staff freeze: no limits, lifted by staff only. Frozen = hunger, happiness and age all stop.
  - Warning DM **≈ 6 hours** before death, once. Deaths and resurrections announced in the game channel **`878305034432045080`**. Sweep **hourly**.
  - Moods, first match wins: **frozen**; **starving** hunger < 25%; **hungry** hunger < 50%; **sad** happiness < 30%; **happy** happiness > 80%; else **content**.
  - Rarity per adoption: Amanite, Cèpe, Rosé des prés **70% together** (70/300 each); Russule **18%**; Mystique **9%**; Doré **3%**.
  - Memorial by time lived (frozen/dead time excluded): **< 7 days** cairn · **< 28 days** small stele · **< 90 days** engraved stele · **< 180 days** urn · **180+ days** statue.
  - Foods: **Champignon** +25% hunger, 15 · **Shiitake** +60% hunger, 30 · **Morille** +40% happiness, 40 · **Truffe** +100% hunger +30% happiness, 80. Refused, free, when its whole effect would be wasted.
  - Pet: **+25% happiness**, **4 h** cooldown per (petter, Plynling), anyone may pet, never touches hunger.
  - `/work`: every **4 h**, pays **40–60**. Passive: **1 caillou per XP grant**, capped at **30% × 3 shifts × 50 = 45/day**.
  - Name required, **≤ 32** characters, staff-only rename.
  - Graveyard: **5 per page**, sort **newest** / **longest life**.

---

### Task 1: Rename the six French slash options

**Files:**
- Modify: `ProjectSYNCS/Commands/GiveawayModule.cs:40,41,53`
- Modify: `ProjectSYNCS/Commands/XpAdminModule.cs:42,43,48,49`
- Modify: `ProjectSYNCS/Commands/ConfigModule.cs:95,127`
- Modify: `CLAUDE.md` (the `## Language` section)
- Test: `$SCRATCH/optioncheck/Program.cs`

**Interfaces:**
- Consumes: nothing.
- Produces: option names `prize`, `duration`, `winners`, `member`, `amount`, `channel`.

- [ ] **Step 1: Write the failing harness**

Create `$SCRATCH/optioncheck/optioncheck.csproj` (harness csproj from Global Constraints) and `$SCRATCH/optioncheck/Program.cs`:

```csharp
using System.Reflection;
using System.Text.RegularExpressions;
using Discord.Interactions;

// Every slash option name must be a legal, English Discord option name.
var asm = typeof(ProjectSYNCS.Services.XpService).Assembly;
var names = asm.GetTypes()
    .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
    .SelectMany(m => m.GetParameters())
    .Select(p => p.GetCustomAttribute<SummaryAttribute>()?.Name)
    .Where(n => n is not null)
    .Select(n => n!)
    .Distinct().OrderBy(n => n).ToList();

var french = new[] { "lot", "duree", "gagnants", "membre", "montant", "salon" };
var fail = 0;
foreach (var n in names)
{
    if (!Regex.IsMatch(n, "^[a-z0-9_-]{1,32}$")) { fail++; Console.WriteLine($"FAIL illegal option name: {n}"); }
    if (french.Contains(n)) { fail++; Console.WriteLine($"FAIL still French: {n}"); }
}
Console.WriteLine($"{names.Count} option names: {string.Join(", ", names)}");
Console.WriteLine(fail == 0 ? "OK" : $"{fail} FAILED");
if (fail > 0) Environment.Exit(1);
```

- [ ] **Step 2: Run it — expect 6 failures**

Run: `dotnet run --project "$SCRATCH/optioncheck"`
Expected: `FAIL still French:` for `duree`, `gagnants`, `lot`, `membre`, `montant`, `salon`; last line `6 FAILED`.

- [ ] **Step 3: Rename the options (names only — descriptions stay French)**

In `GiveawayModule.cs`: `Summary("lot", …)` → `Summary("prize", …)`, `Summary("duree", …)` → `Summary("duration", …)`, `Summary("gagnants", …)` → `Summary("winners", …)`.
In `XpAdminModule.cs` (both commands): `Summary("membre", …)` → `Summary("member", …)`, `Summary("montant", …)` → `Summary("amount", …)`.
In `ConfigModule.cs` (both subcommands): `Summary("salon", …)` → `Summary("channel", …)`.

- [ ] **Step 4: Fix CLAUDE.md's language note**

Replace the `## Language` section body with:

```markdown
**Command and option names are English; every other user-facing string — descriptions,
replies, embeds, button labels, choice display names, error messages — is French.** Code,
comments and logs are in English. Six options (`lot`, `duree`, `gagnants`, `membre`,
`montant`, `salon`) used to be French and were renamed for consistency; renaming an option
changes what people type, so do not rename one casually.
```

- [ ] **Step 5: Verify**

Run: `cd ProjectSYNCS && dotnet build -warnaserror` → `0 Avertissement(s)`, `0 Erreur(s)`.
Run: `dotnet run --project "$SCRATCH/optioncheck"` → last line `OK`.

- [ ] **Step 6: Checkpoint** — stop and hand off to the user for review and commit.

---

### Task 2: Plynling domain core (pure, no Discord, no database)

**Files:**
- Create: `ProjectSYNCS/Models/Plynling.cs`
- Create: `ProjectSYNCS/Helpers/PlynlingCatalog.cs`
- Create: `ProjectSYNCS/Helpers/PlynlingLife.cs`
- Create: `ProjectSYNCS/Helpers/PebbleEconomy.cs`
- Test: `$SCRATCH/plynlingcheck/Program.cs`

**Interfaces:**
- Consumes: `LevelCardUi.Xp(long)` (fr-FR grouping, existing).
- Produces:
  - `enum PlynlingSpecies { Amanite, Cepe, Rose, Russule, Mystique, Dore }` (Models)
  - `class Plynling` (Models) — properties listed in Step 3.
  - `enum PlynlingRarity { Common, Uncommon, Rare, Legendary }`, `enum PlynlingMood { Content, Happy, Sad, Hungry, Starving, Frozen }`, `enum PlynlingFood { Mushroom, Shiitake, Morel, Truffle }`, `enum FreezeOutcome { Frozen, NoPlynling, Dead, AlreadyFrozen, TooHungry, Cooldown }`
  - `record SpeciesInfo(PlynlingSpecies Species, string Name, PlynlingRarity Rarity, int Weight, uint Accent)`, `record FoodInfo(PlynlingFood Food, string Name, string WithArticle, long Price, double Hunger, double Happiness)`
  - `PlynlingCatalog`: `Species`, `Foods`, `Info(PlynlingSpecies)`, `Info(PlynlingFood)`, `RarityLabel(PlynlingRarity)`, `TotalWeight`, `PickSpecies(int roll)`, `RollSpecies()`, `MemorialTier(TimeSpan lived)`, `MemorialName(int tier)`
  - `PlynlingLife`: constants; `Create`, `IsDead`, `IsFrozen`, `HungerAt`, `HappinessAt`, `DeathAt`, `Age`, `Mood`, `Settle`, `SelfFreezeBlocker`, `Freeze`, `Thaw`, `WouldWaste`, `Feed`, `Pet`, `ShouldWarn`, `Resurrect`
  - `PebbleEconomy`: `WorkCooldown`, `WorkMin`, `WorkMax`, `PassivePerGrant`, `PassiveDailyCap`, `RollWorkPay()`, `NextWorkAt(DateTimeOffset?)`, `Passive(long earnedToday, long amount)`, `Cailloux(long n)`

- [ ] **Step 1: Write the failing harness**

Create `$SCRATCH/plynlingcheck/plynlingcheck.csproj` (harness csproj) and `$SCRATCH/plynlingcheck/Program.cs`:

```csharp
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

int pass = 0, fail = 0;
void Check(string label, bool ok) { if (ok) pass++; else { fail++; Console.WriteLine("  FAIL  " + label); } }
bool Near(double a, double b) => Math.Abs(a - b) < 1e-9;
bool NearT(DateTimeOffset a, DateTimeOffset b) => Math.Abs((a - b).TotalSeconds) < 1;
bool NearSpan(TimeSpan a, TimeSpan b) => Math.Abs((a - b).TotalSeconds) < 1;
var t0 = new DateTimeOffset(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);
Plynling New() => PlynlingLife.Create(1, 2, "Rex", PlynlingSpecies.Amanite, t0);
FoodInfo F(PlynlingFood f) => PlynlingCatalog.Info(f);

// --- a newborn: 70/70, starves at 2.8 days ---
var p = New();
Check("newborn hunger 70%", Near(PlynlingLife.HungerAt(p, t0), 0.70));
Check("newborn happiness 70%", Near(PlynlingLife.HappinessAt(p, t0), 0.70));
Check("newborn is content", PlynlingLife.Mood(p, t0) == PlynlingMood.Content);
Check("newborn starves at 2.8 days", NearT(PlynlingLife.DeathAt(p)!.Value, t0 + TimeSpan.FromDays(2.8)));
Check("still content at 19.1h (hunger just above 50%)", PlynlingLife.Mood(p, t0 + TimeSpan.FromHours(19.1)) != PlynlingMood.Hungry);
Check("hungry below 50% (19.3h)", PlynlingLife.Mood(p, t0 + TimeSpan.FromHours(19.3)) == PlynlingMood.Hungry);
Check("starving below 25%", PlynlingLife.Mood(p, t0 + TimeSpan.FromDays(1.8) + TimeSpan.FromMinutes(1)) == PlynlingMood.Starving);

// --- death is settled at the instant it happened, not when someone looked ---
p = New();
var later = t0 + TimeSpan.FromDays(10);
Check("settle reports a change", PlynlingLife.Settle(p, later));
Check("died at 2.8 days", NearT(p.DiedAt!.Value, t0 + TimeSpan.FromDays(2.8)));
Check("age stops at death", NearSpan(PlynlingLife.Age(p, later), TimeSpan.FromDays(2.8)));
Check("second settle is a no-op", !PlynlingLife.Settle(p, later + TimeSpan.FromDays(1)));
Check("a newborn nobody fed gets a cairn", PlynlingCatalog.MemorialTier(PlynlingLife.Age(p, later)) == 1);

// --- feeding and the waste rule ---
p = New();
var t1 = t0 + TimeSpan.FromDays(1);                           // hunger 0.45, happiness 0.20
PlynlingLife.Feed(p, F(PlynlingFood.Truffle), t1);
Check("truffle fills hunger", Near(PlynlingLife.HungerAt(p, t1), 1.0));
Check("truffle adds 30% happiness", Near(PlynlingLife.HappinessAt(p, t1), 0.50));
Check("champignon wasted when hunger full", PlynlingLife.WouldWaste(p, F(PlynlingFood.Mushroom), t1));
Check("morille not wasted while happiness < 100%", !PlynlingLife.WouldWaste(p, F(PlynlingFood.Morel), t1));
Check("truffe not wasted while happiness < 100%", !PlynlingLife.WouldWaste(p, F(PlynlingFood.Truffle), t1));
PlynlingLife.Pet(p, t1); PlynlingLife.Pet(p, t1);
Check("pet adds 25% each, capped", Near(PlynlingLife.HappinessAt(p, t1), 1.0));
Check("truffe wasted when both full", PlynlingLife.WouldWaste(p, F(PlynlingFood.Truffle), t1));
Check("morille wasted when happiness full", PlynlingLife.WouldWaste(p, F(PlynlingFood.Morel), t1));
Check("happy above 80%", PlynlingLife.Mood(p, t1) == PlynlingMood.Happy);
var sad = New();
PlynlingLife.Feed(sad, F(PlynlingFood.Truffle), t0 + TimeSpan.FromDays(1.5)); // hunger full, happiness 0.10+0.30
Check("exactly 30% happiness is not sad (thresholds are strict)", PlynlingLife.Mood(sad, t0 + TimeSpan.FromDays(1.5)) == PlynlingMood.Content);
Check("sad once happiness drops under 30%", PlynlingLife.Mood(sad, t0 + TimeSpan.FromDays(1.75)) == PlynlingMood.Sad);

// --- self-freeze ---
p = New();
Check("can self-freeze at 70%", PlynlingLife.SelfFreezeBlocker(p, t0) is null);
PlynlingLife.Freeze(p, t0, byStaff: false);
var t10 = t0 + TimeSpan.FromDays(10);
Check("frozen: hunger does not move", Near(PlynlingLife.HungerAt(p, t10), 0.70));
Check("frozen: age does not grow", PlynlingLife.Age(p, t10) == TimeSpan.Zero);
Check("frozen mood", PlynlingLife.Mood(p, t10) == PlynlingMood.Frozen);
Check("frozen: no death", !PlynlingLife.Settle(p, t10) && p.DiedAt is null);
Check("frozen: already frozen blocks a second freeze", PlynlingLife.SelfFreezeBlocker(p, t10) == FreezeOutcome.AlreadyFrozen);
var t15 = t0 + TimeSpan.FromDays(15);
Check("a self-freeze thaws itself after 14 days", PlynlingLife.Settle(p, t15) && p.FrozenAt is null);
Check("thawed at day 14, so one day of hunger since", Near(PlynlingLife.HungerAt(p, t15), 0.45));
Check("thaw time recorded for the cooldown", NearT(p.LastSelfThawAt!.Value, t0 + TimeSpan.FromDays(14)));
Check("age resumed at the thaw", NearSpan(PlynlingLife.Age(p, t15), TimeSpan.FromDays(1)));
var q = New(); q.LastSelfThawAt = t0 - TimeSpan.FromDays(6);
Check("cooldown: 6 days after a thaw", PlynlingLife.SelfFreezeBlocker(q, t0) == FreezeOutcome.Cooldown);
q.LastSelfThawAt = t0 - TimeSpan.FromDays(7);
Check("cooldown over after 7 days", PlynlingLife.SelfFreezeBlocker(q, t0) is null);
Check("too hungry below 50%", PlynlingLife.SelfFreezeBlocker(New(), t0 + TimeSpan.FromHours(19.3)) == FreezeOutcome.TooHungry);

// --- staff freeze ---
var s = New();
PlynlingLife.Freeze(s, t0, byStaff: true);
Check("staff freeze never thaws itself", !PlynlingLife.Settle(s, t0 + TimeSpan.FromDays(365)) && s.FrozenAt is not null);
PlynlingLife.Thaw(s, t0 + TimeSpan.FromDays(365));
Check("ending a staff freeze does not start the owner's cooldown", s.LastSelfThawAt is null);
Check("needs resume from where they were", Near(PlynlingLife.HungerAt(s, t0 + TimeSpan.FromDays(365)), 0.70));

// --- the warning DM ---
var w = New(); var death = PlynlingLife.DeathAt(w)!.Value;
Check("no warning 7h before", !PlynlingLife.ShouldWarn(w, death - TimeSpan.FromHours(7)));
Check("warning 5h before", PlynlingLife.ShouldWarn(w, death - TimeSpan.FromHours(5)));
w.WarningSent = true;
Check("warned only once", !PlynlingLife.ShouldWarn(w, death - TimeSpan.FromHours(4)));
PlynlingLife.Feed(w, F(PlynlingFood.Shiitake), death - TimeSpan.FromHours(4));
Check("feeding past the lead re-arms the warning", !w.WarningSent);

// --- resurrection ---
var r = New();
PlynlingLife.Settle(r, t0 + TimeSpan.FromDays(5));              // died at 2.8 days
var back = t0 + TimeSpan.FromDays(20);
PlynlingLife.Resurrect(r, back);
Check("resurrected alive", r.DiedAt is null && !r.DeathAnnounced);
Check("resurrected at 50/50", Near(PlynlingLife.HungerAt(r, back), 0.5) && Near(PlynlingLife.HappinessAt(r, back), 0.5));
Check("a minute later it is hungry and cannot be self-frozen",
    PlynlingLife.Mood(r, back + TimeSpan.FromMinutes(1)) == PlynlingMood.Hungry
    && PlynlingLife.SelfFreezeBlocker(r, back + TimeSpan.FromMinutes(1)) == FreezeOutcome.TooHungry);
Check("age carries on; time dead does not count",
    NearSpan(PlynlingLife.Age(r, back + TimeSpan.FromDays(1)), TimeSpan.FromDays(3.8)));

// --- memorial tiers ---
foreach (var (days, tier) in new[] { (0.1, 1), (6.99, 1), (7.0, 2), (27.9, 2), (28.0, 3), (89.9, 3), (90.0, 4), (179.9, 4), (180.0, 5), (900.0, 5) })
    Check($"{days} days lived -> tier {tier}", PlynlingCatalog.MemorialTier(TimeSpan.FromDays(days)) == tier);

// --- rarity ---
Check("weights total 300", PlynlingCatalog.TotalWeight == 300);
foreach (var (roll, sp) in new[] { (0, PlynlingSpecies.Amanite), (69, PlynlingSpecies.Amanite), (70, PlynlingSpecies.Cepe),
         (139, PlynlingSpecies.Cepe), (140, PlynlingSpecies.Rose), (209, PlynlingSpecies.Rose), (210, PlynlingSpecies.Russule),
         (263, PlynlingSpecies.Russule), (264, PlynlingSpecies.Mystique), (290, PlynlingSpecies.Mystique),
         (291, PlynlingSpecies.Dore), (299, PlynlingSpecies.Dore) })
    Check($"roll {roll} -> {sp}", PlynlingCatalog.PickSpecies(roll) == sp);
Check("commons are 70% together", PlynlingCatalog.Species.Where(x => x.Rarity == PlynlingRarity.Common).Sum(x => x.Weight) == 210);

// --- the food table ---
Check("foods in price order 15 < 30 < 40 < 80",
    PlynlingCatalog.Foods.Select(f => f.Price).SequenceEqual(new long[] { 15, 30, 40, 80 }));

// --- economy ---
Check("passive cap is 45", PebbleEconomy.PassiveDailyCap == 45);
Check("passive grants up to the cap", PebbleEconomy.Passive(40, 10) == (5, 45));
Check("passive grants nothing at the cap", PebbleEconomy.Passive(45, 1) == (0, 45));
Check("no work yet means available now", PebbleEconomy.NextWorkAt(null) is null);
Check("next work 4h later", PebbleEconomy.NextWorkAt(t0) == t0 + TimeSpan.FromHours(4));
var pays = Enumerable.Range(0, 20000).Select(_ => PebbleEconomy.RollWorkPay()).ToList();
Check("work pays 40..60 and hits both ends", pays.Min() == 40 && pays.Max() == 60);
string Plain(string x) => x.Replace('\u202F', ' ').Replace('\u00A0', ' ');
Check("0 caillou", PebbleEconomy.Cailloux(0) == "0 caillou");
Check("1 caillou", PebbleEconomy.Cailloux(1) == "1 caillou");
Check("2 cailloux", PebbleEconomy.Cailloux(2) == "2 cailloux");
Check("1 500 cailloux, grouped", Plain(PebbleEconomy.Cailloux(1500)) == "1 500 cailloux");

Console.WriteLine($"{pass} passed, {fail} failed");
if (fail > 0) Environment.Exit(1);
```

- [ ] **Step 2: Run it — expect a compile failure** (`PlynlingLife`, `PlynlingCatalog`, `PebbleEconomy`, `Plynling` do not exist).

Run: `dotnet run --project "$SCRATCH/plynlingcheck"` → `error CS0246`.

- [ ] **Step 3: Create `ProjectSYNCS/Models/Plynling.cs`**

```csharp
namespace ProjectSYNCS.Models;

public enum PlynlingSpecies { Amanite, Cepe, Rose, Russule, Mystique, Dore }

// One Plynling, alive or dead. A death does not create a second row: DiedAt is set and
// the row is what the graveyard lists; a resurrection clears DiedAt on the same row.
//
// **Nothing here is a live value.** Hunger and happiness are stored as they were at
// NeedsAsOf, and Helpers/PlynlingLife derives the current value from the time elapsed —
// the same idea as the voice-XP taper, which works from minutes already banked. No job
// ticks anything down and a restart loses nothing. Every transition (feeding, freezing,
// thawing, dying) *rebases*: it computes the current values, stores them with a new
// NeedsAsOf, and carries on from there.
public class Plynling
{
    public int Id { get; set; }

    public ulong GuildId { get; set; }
    public ulong OwnerId { get; set; }

    public string Name { get; set; } = string.Empty;
    public PlynlingSpecies Species { get; set; }
    public DateTimeOffset AdoptedAt { get; set; }

    // 0..1, as of NeedsAsOf. While frozen or dead they are simply the stored values.
    public double Hunger { get; set; }
    public double Happiness { get; set; }
    public DateTimeOffset NeedsAsOf { get; set; }

    // Age = AgeBankedSeconds, plus the current stretch since LiveSince while alive and
    // not frozen. Frozen or dead time is never added — the memorial tier is measured on
    // time actually lived.
    public long AgeBankedSeconds { get; set; }
    public DateTimeOffset LiveSince { get; set; }

    public DateTimeOffset? FrozenAt { get; set; }
    // When a self-freeze thaws on its own. Null for a staff freeze, which only staff lift.
    public DateTimeOffset? FreezeUntil { get; set; }
    public bool FrozenByStaff { get; set; }
    // When the owner's last self-freeze ended — the 7-day cooldown counts from here.
    public DateTimeOffset? LastSelfThawAt { get; set; }

    // The single ~6h warning DM. Re-armed when feeding pushes death back past the lead.
    public bool WarningSent { get; set; }

    public DateTimeOffset? DiedAt { get; set; }
    // Set by the sweep once the death has been announced (or attempted), so it is never
    // announced twice. A death found lazily by a command is announced by the next sweep.
    public bool DeathAnnounced { get; set; }
}
```

- [ ] **Step 4: Create `ProjectSYNCS/Helpers/PlynlingCatalog.cs`**

```csharp
using Discord.Interactions;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

public enum PlynlingRarity { Common, Uncommon, Rare, Legendary }

public enum PlynlingMood { Content, Happy, Sad, Hungry, Starving, Frozen }

// The value is English (what /plynling feed sends and what the select option carries);
// ChoiceDisplay is what Discord shows, in French.
public enum PlynlingFood
{
    [ChoiceDisplay("Champignon")] Mushroom,
    [ChoiceDisplay("Shiitake")] Shiitake,
    [ChoiceDisplay("Morille")] Morel,
    [ChoiceDisplay("Truffe")] Truffle,
}

// Accent is the card's colour strip — the species' identifying colour.
public sealed record SpeciesInfo(PlynlingSpecies Species, string Name, PlynlingRarity Rarity, int Weight, uint Accent);

// WithArticle exists because the foods differ in gender ("un champignon", "une morille"):
// lines say "Tu donnes {1} à Rex" rather than guessing an article.
public sealed record FoodInfo(PlynlingFood Food, string Name, string WithArticle, long Price, double Hunger, double Happiness);

// Everything that is data rather than behaviour: species, rarity odds, foods, memorials.
public static class PlynlingCatalog
{
    // Weights out of 300: each common exactly 70/300, so the three commons are 70%
    // together; 18% peu commun, 9% rare, 3% légendaire.
    public static readonly IReadOnlyList<SpeciesInfo> Species = new[]
    {
        new SpeciesInfo(PlynlingSpecies.Amanite,  "Amanite",       PlynlingRarity.Common,    70, 0xCE323A),
        new SpeciesInfo(PlynlingSpecies.Cepe,     "Cèpe",          PlynlingRarity.Common,    70, 0x98623A),
        new SpeciesInfo(PlynlingSpecies.Rose,     "Rosé des prés", PlynlingRarity.Common,    70, 0xEC929E),
        new SpeciesInfo(PlynlingSpecies.Russule,  "Russule verte", PlynlingRarity.Uncommon,  54, 0x62AA58),
        new SpeciesInfo(PlynlingSpecies.Mystique, "Mystique",      PlynlingRarity.Rare,      27, 0x7E52CC),
        new SpeciesInfo(PlynlingSpecies.Dore,     "Doré",          PlynlingRarity.Legendary,  9, 0xE0AA2A),
    };

    // In price order, which is also the order the select menu shows them in.
    public static readonly IReadOnlyList<FoodInfo> Foods = new[]
    {
        new FoodInfo(PlynlingFood.Mushroom, "Champignon", "un champignon", 15, 0.25, 0.00),
        new FoodInfo(PlynlingFood.Shiitake, "Shiitake",   "un shiitake",   30, 0.60, 0.00),
        new FoodInfo(PlynlingFood.Morel,    "Morille",    "une morille",   40, 0.00, 0.40),
        new FoodInfo(PlynlingFood.Truffle,  "Truffe",     "une truffe",    80, 1.00, 0.30),
    };

    public static SpeciesInfo Info(PlynlingSpecies species) => Species.First(s => s.Species == species);
    public static FoodInfo Info(PlynlingFood food) => Foods.First(f => f.Food == food);

    public static string RarityLabel(PlynlingRarity rarity) => rarity switch
    {
        PlynlingRarity.Common => "commun",
        PlynlingRarity.Uncommon => "peu commun",
        PlynlingRarity.Rare => "rare",
        _ => "légendaire",
    };

    public static int TotalWeight => Species.Sum(s => s.Weight);

    // Pure given the roll, so the odds are checkable without randomness.
    public static PlynlingSpecies PickSpecies(int roll)
    {
        foreach (var s in Species)
        {
            if (roll < s.Weight) return s.Species;
            roll -= s.Weight;
        }
        throw new ArgumentOutOfRangeException(nameof(roll));
    }

    public static PlynlingSpecies RollSpecies() => PickSpecies(Random.Shared.Next(TotalWeight));

    // By time actually lived. 4 weeks = 28 days; a month is taken as 30 days.
    public static int MemorialTier(TimeSpan lived) => lived.TotalDays switch
    {
        < 7 => 1,
        < 28 => 2,
        < 90 => 3,
        < 180 => 4,
        _ => 5,
    };

    public static string MemorialName(int tier) => tier switch
    {
        1 => "un cairn",
        2 => "une petite stèle",
        3 => "une stèle gravée",
        4 => "une urne",
        _ => "une statue",
    };
}
```

- [ ] **Step 5: Create `ProjectSYNCS/Helpers/PlynlingLife.cs`**

```csharp
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

public enum FreezeOutcome { Frozen, NoPlynling, Dead, AlreadyFrozen, TooHungry, Cooldown }

// All of a Plynling's time-based behaviour, as pure functions of the row and an instant.
// No Discord, no database: this is the part that is checkable outright, and it is where
// the subtle bugs would live (a frozen day counted as lived, a death at the wrong time).
//
// Thresholds are strict ("under 50%", "above 80%") so that "hungry face" and "can no
// longer self-freeze" are exact complements: both begin just below 50%.
public static class PlynlingLife
{
    public static readonly TimeSpan HungerLife = TimeSpan.FromDays(4);
    public static readonly TimeSpan HappinessLife = TimeSpan.FromDays(2);
    public static readonly TimeSpan SelfFreezeMax = TimeSpan.FromDays(14);
    public static readonly TimeSpan SelfFreezeCooldown = TimeSpan.FromDays(7);
    public static readonly TimeSpan WarningLead = TimeSpan.FromHours(6);
    public static readonly TimeSpan PetCooldown = TimeSpan.FromHours(4);

    public const double StartNeeds = 0.70;
    public const double ResurrectNeeds = 0.50;
    public const double SelfFreezeMinHunger = 0.50;
    public const double PetAmount = 0.25;
    public const double StarvingBelow = 0.25;
    public const double HungryBelow = 0.50;
    public const double SadBelow = 0.30;
    public const double HappyAbove = 0.80;

    // What the card shows as 100% counts as full, so feeding is never refused at a value
    // the owner can see is not full, nor allowed at one they can see is.
    private const double Full = 0.995;

    public static bool IsDead(Plynling p) => p.DiedAt is not null;
    public static bool IsFrozen(Plynling p) => p.FrozenAt is not null;

    public static Plynling Create(ulong guildId, ulong ownerId, string name, PlynlingSpecies species, DateTimeOffset now) => new()
    {
        GuildId = guildId,
        OwnerId = ownerId,
        Name = name,
        Species = species,
        AdoptedAt = now,
        Hunger = StartNeeds,
        Happiness = StartNeeds,
        NeedsAsOf = now,
        LiveSince = now,
    };

    public static double HungerAt(Plynling p, DateTimeOffset t) =>
        IsFrozen(p) || IsDead(p) ? p.Hunger : Clamp(p.Hunger - (t - p.NeedsAsOf) / HungerLife);

    public static double HappinessAt(Plynling p, DateTimeOffset t) =>
        IsFrozen(p) || IsDead(p) ? p.Happiness : Clamp(p.Happiness - (t - p.NeedsAsOf) / HappinessLife);

    // When it will starve if nothing changes. Null when it cannot: frozen, or already dead.
    public static DateTimeOffset? DeathAt(Plynling p) =>
        IsFrozen(p) || IsDead(p) ? null : p.NeedsAsOf + p.Hunger * HungerLife;

    public static TimeSpan Age(Plynling p, DateTimeOffset now) =>
        TimeSpan.FromSeconds(p.AgeBankedSeconds) + (IsFrozen(p) || IsDead(p) ? TimeSpan.Zero : now - p.LiveSince);

    public static PlynlingMood Mood(Plynling p, DateTimeOffset now)
    {
        if (IsFrozen(p)) return PlynlingMood.Frozen;
        var hunger = HungerAt(p, now);
        if (hunger < StarvingBelow) return PlynlingMood.Starving;
        if (hunger < HungryBelow) return PlynlingMood.Hungry;
        var happiness = HappinessAt(p, now);
        if (happiness < SadBelow) return PlynlingMood.Sad;
        if (happiness > HappyAbove) return PlynlingMood.Happy;
        return PlynlingMood.Content;
    }

    /// <summary>
    /// Brings a Plynling up to <paramref name="now"/>: an expired self-freeze thaws at the
    /// moment it was due, then it dies at the moment it starved. Returns whether anything
    /// changed, so the caller knows to save.
    /// </summary>
    /// <remarks>
    /// Every read goes through this first. That is what makes a death discovered by a
    /// command identical to one the hourly sweep found — same instant, same age, same
    /// memorial — and why the sweep is a safety net rather than the source of truth.
    /// Order matters: the thaw is applied first, so the death is computed from the thaw.
    /// </remarks>
    public static bool Settle(Plynling p, DateTimeOffset now)
    {
        if (IsDead(p)) return false;
        var changed = false;

        if (IsFrozen(p) && p.FreezeUntil is { } until && until <= now)
        {
            EndFreeze(p, until);
            changed = true;
        }

        if (DeathAt(p) is { } death && death <= now)
        {
            Rebase(p, death);
            p.AgeBankedSeconds += (long)(death - p.LiveSince).TotalSeconds;
            p.Hunger = 0;
            p.DiedAt = death;
            changed = true;
        }

        return changed;
    }

    // Null when an owner may freeze their own Plynling now; otherwise why not.
    public static FreezeOutcome? SelfFreezeBlocker(Plynling p, DateTimeOffset now) =>
        IsDead(p) ? FreezeOutcome.Dead
        : IsFrozen(p) ? FreezeOutcome.AlreadyFrozen
        : HungerAt(p, now) < SelfFreezeMinHunger ? FreezeOutcome.TooHungry
        : p.LastSelfThawAt is { } thawed && now - thawed < SelfFreezeCooldown ? FreezeOutcome.Cooldown
        : null;

    public static void Freeze(Plynling p, DateTimeOffset now, bool byStaff)
    {
        Rebase(p, now);
        p.AgeBankedSeconds += (long)(now - p.LiveSince).TotalSeconds;
        p.FrozenAt = now;
        p.FreezeUntil = byStaff ? null : now + SelfFreezeMax;
        p.FrozenByStaff = byStaff;
    }

    public static void Thaw(Plynling p, DateTimeOffset now) => EndFreeze(p, now);

    public static bool WouldWaste(Plynling p, FoodInfo food, DateTimeOffset now) =>
        (food.Hunger <= 0 || HungerAt(p, now) >= Full) && (food.Happiness <= 0 || HappinessAt(p, now) >= Full);

    public static void Feed(Plynling p, FoodInfo food, DateTimeOffset now)
    {
        Rebase(p, now);
        p.Hunger = Clamp(p.Hunger + food.Hunger);
        p.Happiness = Clamp(p.Happiness + food.Happiness);
        if (DeathAt(p) is { } death && death - now > WarningLead) p.WarningSent = false;
    }

    public static void Pet(Plynling p, DateTimeOffset now)
    {
        Rebase(p, now);
        p.Happiness = Clamp(p.Happiness + PetAmount);
    }

    public static bool ShouldWarn(Plynling p, DateTimeOffset now) =>
        !p.WarningSent && DeathAt(p) is { } death && death > now && death - now <= WarningLead;

    // The same row comes back: same name, same species, age carrying on. Time spent dead
    // was never added to AgeBankedSeconds, so it does not count.
    public static void Resurrect(Plynling p, DateTimeOffset now)
    {
        p.DiedAt = null;
        p.DeathAnnounced = false;
        p.WarningSent = false;
        p.FrozenAt = null;
        p.FreezeUntil = null;
        p.FrozenByStaff = false;
        p.Hunger = ResurrectNeeds;
        p.Happiness = ResurrectNeeds;
        p.NeedsAsOf = now;
        p.LiveSince = now;
    }

    // Stores the current values as of `at`. Only meaningful while alive and not frozen.
    private static void Rebase(Plynling p, DateTimeOffset at)
    {
        p.Hunger = HungerAt(p, at);
        p.Happiness = HappinessAt(p, at);
        p.NeedsAsOf = at;
    }

    private static void EndFreeze(Plynling p, DateTimeOffset at)
    {
        if (!p.FrozenByStaff) p.LastSelfThawAt = at;
        p.FrozenAt = null;
        p.FreezeUntil = null;
        p.FrozenByStaff = false;
        p.NeedsAsOf = at;
        p.LiveSince = at;
    }

    private static double Clamp(double value) => Math.Clamp(value, 0, 1);
}
```

- [ ] **Step 6: Create `ProjectSYNCS/Helpers/PebbleEconomy.cs`**

```csharp
namespace ProjectSYNCS.Helpers;

// The economy's numbers and arithmetic. Cailloux for users, "pebble" in code.
public static class PebbleEconomy
{
    public static readonly TimeSpan WorkCooldown = TimeSpan.FromHours(4);
    public const int WorkMin = 40;
    public const int WorkMax = 60;

    // "A normal day of /work" for the passive cap: three shifts, not the six that are
    // theoretically possible, because nobody works through the night.
    public const int ShiftsPerDay = 3;
    public const double PassiveShare = 0.30;

    // Passive income rides on XP grants, one caillou each; the cap is what makes it a
    // bonus rather than a second salary.
    public const long PassivePerGrant = 1;

    // 30% x 3 shifts x the average shift (50) = 45. Derived, so tuning /work moves it.
    public static long PassiveDailyCap => (long)Math.Round(PassiveShare * ShiftsPerDay * (WorkMin + WorkMax) / 2.0);

    public static long RollWorkPay() => Random.Shared.Next(WorkMin, WorkMax + 1);

    // Null when they have never worked, i.e. available now.
    public static DateTimeOffset? NextWorkAt(DateTimeOffset? lastWorkAt) => lastWorkAt + WorkCooldown;

    public static (long Granted, long NewTotal) Passive(long earnedToday, long amount)
    {
        var room = Math.Max(0, PassiveDailyCap - earnedToday);
        var granted = Math.Min(room, Math.Max(0, amount));
        return (granted, earnedToday + granted);
    }

    // "0 caillou", "1 caillou", "47 cailloux": the French plural is irregular (-oux), and 0
    // and 1 are singular, so every amount goes through here instead of appending an "s".
    public static string Cailloux(long n) => $"{LevelCardUi.Xp(n)} {(Math.Abs(n) <= 1 ? "caillou" : "cailloux")}";
}
```

- [ ] **Step 7: Verify**

Run: `cd ProjectSYNCS && dotnet build -warnaserror` → clean.
Run: `dotnet run --project "$SCRATCH/plynlingcheck"` → `N passed, 0 failed`.

If "exactly 30% happiness is not sad" fails, re-derive the numbers before touching code: at 1.5 days a newborn's happiness is 0.70 − 0.75 → 0, plus the truffle's 0.30 = 0.30 (not < 0.30, so content); a quarter-day later it is 0.30 − 0.125 = 0.175 → sad.

- [ ] **Step 8: Checkpoint** — stop and hand off to the user for review and commit.

---

### Task 3: Art assets and the URL builder

**Files:**
- Already in the repo (preserved from the design session): `tools/plynling-art/common.py`, `tools/plynling-art/sprites.py`, `tools/plynling-art/memorials.py`, `tools/plynling-art/source/food_{mushroom,shiitake,morel,truffle}.png`
- Create: `tools/plynling-art/export.py`
- Create: `tools/plynling-art/README.md`
- Create (generated): `assets/plynlings/*.png` — 70 files
- Modify: `.gitignore`
- Create: `ProjectSYNCS/Helpers/PlynlingArt.cs`
- Test: `$SCRATCH/artcheck/Program.cs`

**Interfaces:**
- Consumes: `PlynlingSpecies`, `PlynlingMood`, `PlynlingFood` (Task 2).
- Produces: `PlynlingArt.BaseUrl`, `PlynlingArt.Version`, `PlynlingArt.Sprite(PlynlingSpecies, PlynlingMood)`, `PlynlingArt.Memorial(PlynlingSpecies, int tier)`, `PlynlingArt.Food(PlynlingFood)`.

- [ ] **Step 1: Write the failing harness**

Create `$SCRATCH/artcheck/artcheck.csproj` (harness csproj) and `$SCRATCH/artcheck/Program.cs`:

```csharp
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

// Every URL the bot can build must point at a file that exists, at 256x256, and no file
// may exist that nothing links to.
var dir = @"C:\Users\c235773\Desktop\Sources - RME\Discord Bots\SessionOrganizer\assets\plynlings";
int pass = 0, fail = 0;
void Check(string label, bool ok) { if (ok) pass++; else { fail++; Console.WriteLine("  FAIL  " + label); } }

var urls = new List<string>();
foreach (var s in Enum.GetValues<PlynlingSpecies>())
{
    foreach (var m in Enum.GetValues<PlynlingMood>()) urls.Add(PlynlingArt.Sprite(s, m));
    for (var tier = 1; tier <= 5; tier++) urls.Add(PlynlingArt.Memorial(s, tier));
}
foreach (var f in Enum.GetValues<PlynlingFood>()) urls.Add(PlynlingArt.Food(f));

var referenced = new HashSet<string>();
foreach (var url in urls)
{
    Check($"{url} is under BaseUrl", url.StartsWith(PlynlingArt.BaseUrl));
    var file = url[PlynlingArt.BaseUrl.Length..];
    referenced.Add(file);
    var path = Path.Combine(dir, file);
    if (!File.Exists(path)) { Check($"{file} exists", false); continue; }
    var header = File.ReadAllBytes(path).Take(24).ToArray();          // PNG: IHDR width/height at 16..23
    int W(int o) => (header[o] << 24) | (header[o + 1] << 16) | (header[o + 2] << 8) | header[o + 3];
    Check($"{file} is 256x256", W(16) == 256 && W(20) == 256);
}
var onDisk = Directory.Exists(dir) ? Directory.GetFiles(dir, "*.png").Select(Path.GetFileName).ToList() : new();
foreach (var orphan in onDisk.Where(f => !referenced.Contains(f!))) Check($"{orphan} is referenced", false);
Check("70 URLs", urls.Count == 70);

Console.WriteLine($"{pass} passed, {fail} failed");
if (fail > 0) Environment.Exit(1);
```

- [ ] **Step 2: Run it — expect a compile failure** (`PlynlingArt` does not exist).

- [ ] **Step 3: Create `tools/plynling-art/export.py`**

```python
"""Renders every image the bot links to, at 256x256, into assets/plynlings/.

Filenames carry a version (..._v1.png). Discord caches an image by its URL, so changing
the art means bumping ART_VERSION here *and* PlynlingArt.Version in the bot together —
overwriting a file in place can leave the old art showing for a long while.
"""
import os

from PIL import Image

from common import SPECIES
from memorials import memorial
from sprites import build

ART_VERSION = 1
SIZE = 256
HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.normpath(os.path.join(HERE, "..", "..", "assets", "plynlings"))
STATES = ["happy", "content", "sad", "hungry", "starving", "frozen"]
FOODS = ["mushroom", "shiitake", "morel", "truffle"]


def save(im, name):
    # Nearest-neighbour: Discord smooths a small image when it enlarges it, which blurs
    # pixel art, so the files are exported large with hard edges.
    im.resize((SIZE, SIZE), Image.NEAREST).save(os.path.join(OUT, f"{name}_v{ART_VERSION}.png"), optimize=True)


def main():
    os.makedirs(OUT, exist_ok=True)
    for old in os.listdir(OUT):
        if old.endswith(".png"):
            os.remove(os.path.join(OUT, old))
    count = 0
    for sp in SPECIES:
        for state in STATES:
            save(build(state, sp), f"plynling_{sp}_{state}")
            count += 1
        for tier in range(1, 6):
            save(memorial(tier, sp), f"memorial_{sp}_{tier}")
            count += 1
    for food in FOODS:
        save(Image.open(os.path.join(HERE, "source", f"food_{food}.png")).convert("RGBA"), f"food_{food}")
        count += 1
    print(f"{count} files written to {OUT}")


if __name__ == "__main__":
    main()
```

- [ ] **Step 4: Create `tools/plynling-art/README.md`**

```markdown
# Plynling art

Placeholder pixel art for the Plynlings, drawn by code so a tweak is an edit, not a redraw.

- `common.py` — palettes, the six species, the pixel grid, the shared outline pass.
- `sprites.py` — the living Plynling in six moods (happy, content, sad, hungry, starving, frozen).
- `memorials.py` — the five memorial tiers (cairn → statue), each carrying the species' accent colour.
- `source/` — the four food sprites (16×16, hand-drawn), exported as-is.
- `export.py` — renders all 70 files at 256×256 into `assets/plynlings/`.

Requires Python 3 and Pillow. Run from this folder: `python export.py`.

**Changing the art:** bump `ART_VERSION` in `export.py` **and** `PlynlingArt.Version` in the
bot, re-export, commit, push. The bot links to GitHub raw URLs on `main`, and Discord caches
images by URL — a new version must be a new filename, never an overwrite.
```

- [ ] **Step 5: Ignore Python's cache**

Append to `.gitignore`:

```
__pycache__/
```

- [ ] **Step 6: Create `ProjectSYNCS/Helpers/PlynlingArt.cs`**

```csharp
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

// Where every Plynling image lives. The one place these URLs are built, so if the repo
// ever goes private and the images have to be attached instead, this file is the change.
//
// The images are GitHub raw files on main: they resolve only once assets/plynlings/ has
// been pushed. Discord caches by URL, so new art is a new Version (and new filenames from
// tools/plynling-art/export.py), never an overwrite.
public static class PlynlingArt
{
    public const int Version = 1;

    public const string BaseUrl =
        "https://raw.githubusercontent.com/RMelisen/DiscordBot.SessionOrganizer/main/assets/plynlings/";

    public static string Sprite(PlynlingSpecies species, PlynlingMood mood) =>
        $"{BaseUrl}plynling_{Key(species)}_{mood.ToString().ToLowerInvariant()}_v{Version}.png";

    public static string Memorial(PlynlingSpecies species, int tier) =>
        $"{BaseUrl}memorial_{Key(species)}_{tier}_v{Version}.png";

    public static string Food(PlynlingFood food) =>
        $"{BaseUrl}food_{food.ToString().ToLowerInvariant()}_v{Version}.png";

    // Must match the SPECIES keys in tools/plynling-art/common.py.
    public static string Key(PlynlingSpecies species) => species switch
    {
        PlynlingSpecies.Amanite => "amanite",
        PlynlingSpecies.Cepe => "cepe",
        PlynlingSpecies.Rose => "rose",
        PlynlingSpecies.Russule => "russule",
        PlynlingSpecies.Mystique => "mystique",
        _ => "dore",
    };
}
```

- [ ] **Step 7: Export the art**

Run: `cd tools/plynling-art && python export.py`
Expected: `70 files written to …\assets\plynlings`.

- [ ] **Step 8: Verify**

Run: `cd ProjectSYNCS && dotnet build -warnaserror` → clean.
Run: `dotnet run --project "$SCRATCH/artcheck"` → `N passed, 0 failed`.

- [ ] **Step 9: Checkpoint** — stop and hand off to the user for review and commit. **Remind them the images only appear in Discord after this is pushed to `main`.**

---

### Task 4: Persistence — models, mapping, migration, services

**Files:**
- Create: `ProjectSYNCS/Models/PebbleWallet.cs`
- Modify: `ProjectSYNCS/Data/AppDbContext.cs` (two `DbSet`s, two entity blocks)
- Create (generated): `ProjectSYNCS/Migrations/*_AddPlynlingsAndPebbles.cs` (+ Designer, snapshot update)
- Create: `ProjectSYNCS/Services/PebbleService.cs`
- Create: `ProjectSYNCS/Services/PlynlingService.cs`
- Modify: `ProjectSYNCS/Program.cs` (register both transient)
- Test: `$SCRATCH/plynlingdb/Program.cs`

**Interfaces:**
- Consumes: everything from Task 2.
- Produces:
  - `record WorkResult(bool Paid, long Amount, long Balance, DateTimeOffset NextWorkAt)`, `record PebbleBalance(long Balance, DateTimeOffset? NextWorkAt, long PassiveToday)`
  - `PebbleService`: `WorkAsync(ulong guildId, ulong userId, long pay, DateTimeOffset now) → Task<WorkResult>`, `AddPassiveAsync(ulong guildId, ulong userId, long amount) → Task<long>`, `GetBalanceAsync(ulong guildId, ulong userId) → Task<PebbleBalance>`, `static GetOrCreateWalletAsync(AppDbContext db, ulong guildId, ulong userId) → Task<PebbleWallet>`
  - `enum AdoptOutcome { Adopted, AlreadyHasOne }`, `enum CareOutcome { Done, NoPlynling, NotOwner, Dead, Frozen, Wasted, TooPoor }`, `enum ThawOutcome { Thawed, NoPlynling, Dead, NotFrozen, StaffOnly }`, `enum ResurrectOutcome { Resurrected, NoGrave, AlreadyHasOne }`, `record FeedResult(CareOutcome Outcome, Plynling? Plynling, long Price, long Balance)`
  - `PlynlingService`: `GetCurrentAsync`, `GetShownAsync`, `GetByIdAsync`, `GetLatestDeadAsync`, `AdoptAsync`, `FeedAsync(int plynlingId, ulong actorId, PlynlingFood food, DateTimeOffset now)`, `PetAsync(int plynlingId, DateTimeOffset now)`, `FreezeAsync(ulong guildId, ulong ownerId, bool byStaff, DateTimeOffset now)`, `ThawAsync(...)`, `RenameAsync(ulong guildId, ulong ownerId, string name, DateTimeOffset now) → (Plynling?, string OldName)`, `ResurrectAsync(...)`, `GetGraveyardAsync(ulong guildId, ulong? ownerId, DateTimeOffset now)`, `GetSweepBatchAsync()`, `SaveAsync()`

- [ ] **Step 1: Write the failing harness**

Create `$SCRATCH/plynlingdb/plynlingdb.csproj` (harness csproj) and `$SCRATCH/plynlingdb/Program.cs`:

```csharp
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ProjectSYNCS.Data;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;
using ProjectSYNCS.Services;

// Real SQLite, real migrations: the partial unique index and the one-save feed can only
// be proven against the actual database.
var path = Path.Combine(Path.GetTempPath(), $"plyn_{Guid.NewGuid():N}.db");
AppDbContext Ctx() => new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite($"Data Source={path}").Options);
int pass = 0, fail = 0;
void Check(string label, bool ok) { if (ok) pass++; else { fail++; Console.WriteLine("  FAIL  " + label); } }

try
{
    using (var db = Ctx()) db.Database.Migrate();
    var now = new DateTimeOffset(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);

    // --- one living Plynling per person, enforced by the database ---
    var (o1, p1) = await new PlynlingService(Ctx()).AdoptAsync(1, 10, "Rex", PlynlingSpecies.Amanite, now);
    Check("first adoption", o1 == AdoptOutcome.Adopted && p1 is not null);
    var (o2, _) = await new PlynlingService(Ctx()).AdoptAsync(1, 10, "Bis", PlynlingSpecies.Cepe, now);
    Check("second living refused by the service", o2 == AdoptOutcome.AlreadyHasOne);
    using (var db = Ctx())
    {
        db.Plynlings.Add(PlynlingLife.Create(1, 10, "Sneaky", PlynlingSpecies.Rose, now));
        var blocked = false;
        try { db.SaveChanges(); } catch (DbUpdateException) { blocked = true; }
        Check("partial unique index blocks a second living row", blocked);
    }
    var (o3, _) = await new PlynlingService(Ctx()).AdoptAsync(2, 10, "Ailleurs", PlynlingSpecies.Rose, now);
    Check("same person, another server", o3 == AdoptOutcome.Adopted);

    // --- death is settled on read; then adoption is allowed again ---
    var later = now + TimeSpan.FromDays(10);
    var dead = await new PlynlingService(Ctx()).GetCurrentAsync(1, 10, later);
    Check("read settles the death and returns it dead", dead?.DiedAt is not null);
    Check("died at 2.8 days", dead?.DiedAt is { } d && Math.Abs((d - (now + TimeSpan.FromDays(2.8))).TotalSeconds) < 1);
    var (o4, p4) = await new PlynlingService(Ctx()).AdoptAsync(1, 10, "Rex II", PlynlingSpecies.Cepe, later);
    Check("adopt after a death", o4 == AdoptOutcome.Adopted && p4 is not null);

    // --- feeding: refusals change nothing; success saves money and meal together ---
    var t = later + TimeSpan.FromHours(20);                             // Rex II at 0.49 hunger
    var poor = await new PlynlingService(Ctx()).FeedAsync(p4!.Id, 10, PlynlingFood.Mushroom, t);
    Check("too poor", poor.Outcome == CareOutcome.TooPoor && poor.Balance == 0);
    var work = await new PebbleService(Ctx()).WorkAsync(1, 10, 100, t);
    Check("work pays", work.Paid && work.Balance == 100);
    var notOwner = await new PlynlingService(Ctx()).FeedAsync(p4.Id, 99, PlynlingFood.Mushroom, t);
    Check("someone else cannot feed it", notOwner.Outcome == CareOutcome.NotOwner);
    var fed = await new PlynlingService(Ctx()).FeedAsync(p4.Id, 10, PlynlingFood.Truffle, t);
    Check("fed", fed.Outcome == CareOutcome.Done && fed.Balance == 20);
    var wasted = await new PlynlingService(Ctx()).FeedAsync(p4.Id, 10, PlynlingFood.Mushroom, t);
    Check("wasted food refused and free", wasted.Outcome == CareOutcome.Wasted && wasted.Balance == 20);
    using (var db = Ctx())
    {
        var row = db.Plynlings.Single(x => x.Id == p4.Id);
        Check("the meal is stored", Math.Abs(PlynlingLife.HungerAt(row, t) - 1.0) < 1e-6);
        Check("the payment is stored", db.PebbleWallets.Single(x => x.GuildId == 1 && x.UserId == 10).Balance == 20);
    }

    // --- work cooldown ---
    var again = await new PebbleService(Ctx()).WorkAsync(1, 10, 50, t + TimeSpan.FromHours(3));
    Check("work refused within 4h", !again.Paid && again.NextWorkAt == t + TimeSpan.FromHours(4));
    var after = await new PebbleService(Ctx()).WorkAsync(1, 10, 50, t + TimeSpan.FromHours(4));
    Check("work allowed after 4h", after.Paid && after.Balance == 70);

    // --- passive cap ---
    long granted = 0;
    for (var i = 0; i < 100; i++) granted += await new PebbleService(Ctx()).AddPassiveAsync(1, 20, PebbleEconomy.PassivePerGrant);
    Check("passive capped at 45 a day", granted == 45);
    var bal = await new PebbleService(Ctx()).GetBalanceAsync(1, 20);
    Check("balance shows passive progress", bal.Balance == 45 && bal.PassiveToday == 45 && bal.NextWorkAt is null);

    // --- freeze / thaw ---
    var (f1, _) = await new PlynlingService(Ctx()).FreezeAsync(1, 10, byStaff: false, t);
    Check("self-freeze allowed when fed", f1 == FreezeOutcome.Frozen);
    var (th1, _) = await new PlynlingService(Ctx()).ThawAsync(1, 10, byStaff: false, t + TimeSpan.FromDays(1));
    Check("owner ends their self-freeze", th1 == ThawOutcome.Thawed);
    var (f2, _) = await new PlynlingService(Ctx()).FreezeAsync(1, 10, byStaff: false, t + TimeSpan.FromDays(2));
    Check("cooldown after a self-freeze", f2 == FreezeOutcome.Cooldown);
    var (f3, _) = await new PlynlingService(Ctx()).FreezeAsync(1, 10, byStaff: true, t + TimeSpan.FromDays(2));
    Check("staff can freeze regardless", f3 == FreezeOutcome.Frozen);
    var (th2, _) = await new PlynlingService(Ctx()).ThawAsync(1, 10, byStaff: false, t + TimeSpan.FromDays(3));
    Check("owner cannot lift a staff freeze", th2 == ThawOutcome.StaffOnly);
    var (th3, _) = await new PlynlingService(Ctx()).ThawAsync(1, 10, byStaff: true, t + TimeSpan.FromDays(3));
    Check("staff lifts it", th3 == ThawOutcome.Thawed);

    // --- resurrection ---
    var (r1, _) = await new PlynlingService(Ctx()).ResurrectAsync(1, 10, t + TimeSpan.FromDays(3));
    Check("resurrect refused while a living one exists", r1 == ResurrectOutcome.AlreadyHasOne);
    var gone = t + TimeSpan.FromDays(30);
    var (r2, back) = await new PlynlingService(Ctx()).ResurrectAsync(1, 10, gone);   // Rex II has starved by now
    Check("resurrects the latest grave", r2 == ResurrectOutcome.Resurrected && back?.Name == "Rex II");
    Check("back at 50/50", back is not null && Math.Abs(PlynlingLife.HungerAt(back, gone) - 0.5) < 1e-9);
    var (r3, _) = await new PlynlingService(Ctx()).ResurrectAsync(1, 77, gone);
    Check("nobody to resurrect", r3 == ResurrectOutcome.NoGrave);

    // --- rename reaches a grave ---
    var (renamed, oldName) = await new PlynlingService(Ctx()).RenameAsync(1, 10, "Rex Deux", gone);
    Check("rename", renamed?.Name == "Rex Deux" && oldName == "Rex II");

    // --- graveyard and sweep batch ---
    var graves = await new PlynlingService(Ctx()).GetGraveyardAsync(1, null, gone);
    Check("graveyard lists the first Rex", graves.Any(g => g.Name == "Rex"));
    Check("graveyard excludes the resurrected one", graves.All(g => g.Name != "Rex Deux"));
    var batch = await new PlynlingService(Ctx()).GetSweepBatchAsync();
    Check("sweep sees the unannounced death", batch.Any(b => b.Name == "Rex" && !b.DeathAnnounced));
}
finally
{
    SqliteConnection.ClearAllPools();
    File.Delete(path);
}
Console.WriteLine($"{pass} passed, {fail} failed");
if (fail > 0) Environment.Exit(1);
```

- [ ] **Step 2: Run it — expect a compile failure** (`PebbleService`, `PlynlingService`, `db.Plynlings` do not exist).

- [ ] **Step 3: Create `ProjectSYNCS/Models/PebbleWallet.cs`**

```csharp
namespace ProjectSYNCS.Models;

// One person's cailloux in one guild ("pebble" in code). One row per (guild, user).
//
// The passive cap needs to know what was earned *today*, and it is kept here as two
// columns rather than in a daily-bucket table: nothing ranks cailloux by date, so the
// totals+buckets pair the leaderboards use would be a table with no reader. PassiveToday
// counts only while PassiveDay is today; a new day simply starts it from zero.
public class PebbleWallet
{
    public int Id { get; set; }

    public ulong GuildId { get; set; }
    public ulong UserId { get; set; }

    public long Balance { get; set; }

    // Null until the first /work; the 4-hour cooldown counts from here.
    public DateTimeOffset? LastWorkAt { get; set; }

    public int PassiveDay { get; set; }
    public long PassiveToday { get; set; }
}
```

- [ ] **Step 4: Map both in `ProjectSYNCS/Data/AppDbContext.cs`**

Add beside the other `DbSet`s:

```csharp
    public DbSet<Plynling> Plynlings => Set<Plynling>();
    public DbSet<PebbleWallet> PebbleWallets => Set<PebbleWallet>();
```

Add inside `OnModelCreating`, after the last entity block:

```csharp
        modelBuilder.Entity<Plynling>(e =>
        {
            e.Property(x => x.GuildId).HasConversion<long>();
            e.Property(x => x.OwnerId).HasConversion<long>();
            // One *living* Plynling per person per guild, enforced by the database rather
            // than only by the code: a partial unique index over rows that have not died,
            // so two racing adoptions (or a resurrection racing an adoption) cannot both
            // land. Dead rows are excluded, which is what lets the graveyard hold any number.
            e.HasIndex(x => new { x.GuildId, x.OwnerId }).IsUnique().HasFilter("\"DiedAt\" IS NULL");
            e.HasIndex(x => x.GuildId);
        });

        modelBuilder.Entity<PebbleWallet>(e =>
        {
            e.Property(x => x.GuildId).HasConversion<long>();
            e.Property(x => x.UserId).HasConversion<long>();
            e.HasIndex(x => new { x.GuildId, x.UserId }).IsUnique();
        });
```

- [ ] **Step 5: Create `ProjectSYNCS/Services/PebbleService.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using ProjectSYNCS.Data;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Services;

public sealed record WorkResult(bool Paid, long Amount, long Balance, DateTimeOffset NextWorkAt);

public sealed record PebbleBalance(long Balance, DateTimeOffset? NextWorkAt, long PassiveToday);

// EF access for cailloux — transient, like every service wrapping AppDbContext.
public class PebbleService
{
    private readonly AppDbContext _db_context;

    public PebbleService(AppDbContext db_context)
    {
        _db_context = db_context;
    }

    // The pay is a parameter rather than rolled here, so the amount is testable.
    public async Task<WorkResult> WorkAsync(ulong guildId, ulong userId, long pay, DateTimeOffset now)
    {
        var wallet = await GetOrCreateWalletAsync(_db_context, guildId, userId);
        if (PebbleEconomy.NextWorkAt(wallet.LastWorkAt) is { } next && next > now)
            return new WorkResult(false, 0, wallet.Balance, next);

        wallet.Balance += pay;
        wallet.LastWorkAt = now;
        await _db_context.SaveChangesAsync();
        return new WorkResult(true, pay, wallet.Balance, now + PebbleEconomy.WorkCooldown);
    }

    // Returns how much was actually granted — zero once today's cap is reached.
    public async Task<long> AddPassiveAsync(ulong guildId, ulong userId, long amount)
    {
        var wallet = await GetOrCreateWalletAsync(_db_context, guildId, userId);
        var today = AppTime.TodayKey;
        var (granted, total) = PebbleEconomy.Passive(wallet.PassiveDay == today ? wallet.PassiveToday : 0, amount);
        if (granted <= 0) return 0;

        wallet.Balance += granted;
        wallet.PassiveDay = today;
        wallet.PassiveToday = total;
        await _db_context.SaveChangesAsync();
        return granted;
    }

    public async Task<PebbleBalance> GetBalanceAsync(ulong guildId, ulong userId)
    {
        var wallet = await _db_context.PebbleWallets.AsNoTracking()
            .FirstOrDefaultAsync(x => x.GuildId == guildId && x.UserId == userId);
        if (wallet is null) return new PebbleBalance(0, null, 0);

        return new PebbleBalance(
            wallet.Balance,
            PebbleEconomy.NextWorkAt(wallet.LastWorkAt),
            wallet.PassiveDay == AppTime.TodayKey ? wallet.PassiveToday : 0);
    }

    // Static and context-taking so PlynlingService can load the wallet in *its own*
    // context — paying and feeding must save together, and AppDbContext is transient.
    public static async Task<PebbleWallet> GetOrCreateWalletAsync(AppDbContext db, ulong guildId, ulong userId)
    {
        var wallet = await db.PebbleWallets.FirstOrDefaultAsync(x => x.GuildId == guildId && x.UserId == userId);
        if (wallet is null)
        {
            wallet = new PebbleWallet { GuildId = guildId, UserId = userId };
            db.PebbleWallets.Add(wallet);
        }
        return wallet;
    }
}
```

- [ ] **Step 6: Create `ProjectSYNCS/Services/PlynlingService.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using ProjectSYNCS.Data;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Services;

public enum AdoptOutcome { Adopted, AlreadyHasOne }

public enum CareOutcome { Done, NoPlynling, NotOwner, Dead, Frozen, Wasted, TooPoor }

public enum ThawOutcome { Thawed, NoPlynling, Dead, NotFrozen, StaffOnly }

public enum ResurrectOutcome { Resurrected, NoGrave, AlreadyHasOne }

public sealed record FeedResult(CareOutcome Outcome, Plynling? Plynling, long Price, long Balance);

// EF access for Plynlings — transient. Every read goes through PlynlingLife.Settle
// before returning, so a caller always sees a Plynling as it is *now*: dead if it starved
// since anyone looked, thawed if its self-freeze ran out.
//
// **Feeding touches the wallet too, here, on purpose.** AppDbContext is transient, so
// charging through PebbleService would be a second context and a second SaveChanges — a
// crash between the two would take the cailloux without feeding it. One context, one
// save, the same shape as ShameService.TryVoteAsync.
public class PlynlingService
{
    private readonly AppDbContext _db_context;

    public PlynlingService(AppDbContext db_context)
    {
        _db_context = db_context;
    }

    // The owner's living Plynling, brought up to date. If it starved since anyone last
    // looked it comes back dead (and is saved dead), so the caller can say so.
    public async Task<Plynling?> GetCurrentAsync(ulong guildId, ulong ownerId, DateTimeOffset now) =>
        await SettledAsync(await _db_context.Plynlings
            .FirstOrDefaultAsync(x => x.GuildId == guildId && x.OwnerId == ownerId && x.DiedAt == null), now);

    // What /plynling view shows: the living one, or else their most recent grave.
    public async Task<Plynling?> GetShownAsync(ulong guildId, ulong ownerId, DateTimeOffset now) =>
        await GetCurrentAsync(guildId, ownerId, now) ?? await GetLatestDeadAsync(guildId, ownerId);

    public async Task<Plynling?> GetByIdAsync(int id, DateTimeOffset now) =>
        await SettledAsync(await _db_context.Plynlings.FirstOrDefaultAsync(x => x.Id == id), now);

    public async Task<Plynling?> GetLatestDeadAsync(ulong guildId, ulong ownerId)
    {
        // Ordered in memory: SQLite cannot translate DateTimeOffset ordering.
        var dead = await _db_context.Plynlings
            .Where(x => x.GuildId == guildId && x.OwnerId == ownerId && x.DiedAt != null)
            .ToListAsync();
        return dead.OrderByDescending(x => x.DiedAt).FirstOrDefault();
    }

    public async Task<(AdoptOutcome Outcome, Plynling? Plynling)> AdoptAsync(
        ulong guildId, ulong ownerId, string name, PlynlingSpecies species, DateTimeOffset now)
    {
        var current = await GetCurrentAsync(guildId, ownerId, now);
        if (current is { DiedAt: null }) return (AdoptOutcome.AlreadyHasOne, current);

        var plynling = PlynlingLife.Create(guildId, ownerId, name, species, now);
        _db_context.Plynlings.Add(plynling);
        try
        {
            await _db_context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Two adoptions raced; the partial unique index let exactly one through.
            return (AdoptOutcome.AlreadyHasOne, null);
        }
        return (AdoptOutcome.Adopted, plynling);
    }

    public async Task<FeedResult> FeedAsync(int plynlingId, ulong actorId, PlynlingFood food, DateTimeOffset now)
    {
        var info = PlynlingCatalog.Info(food);
        var plynling = await GetByIdAsync(plynlingId, now);
        if (plynling is null) return new FeedResult(CareOutcome.NoPlynling, null, info.Price, 0);

        var wallet = await PebbleService.GetOrCreateWalletAsync(_db_context, plynling.GuildId, actorId);
        CareOutcome? refusal =
            plynling.OwnerId != actorId ? CareOutcome.NotOwner
            : plynling.DiedAt is not null ? CareOutcome.Dead
            : plynling.FrozenAt is not null ? CareOutcome.Frozen
            : PlynlingLife.WouldWaste(plynling, info, now) ? CareOutcome.Wasted
            : wallet.Balance < info.Price ? CareOutcome.TooPoor
            : null;
        if (refusal is { } r) return new FeedResult(r, plynling, info.Price, wallet.Balance);

        wallet.Balance -= info.Price;
        PlynlingLife.Feed(plynling, info, now);
        await _db_context.SaveChangesAsync();          // the money and the meal land together
        return new FeedResult(CareOutcome.Done, plynling, info.Price, wallet.Balance);
    }

    public async Task<(CareOutcome Outcome, Plynling? Plynling)> PetAsync(int plynlingId, DateTimeOffset now)
    {
        var plynling = await GetByIdAsync(plynlingId, now);
        if (plynling is null) return (CareOutcome.NoPlynling, null);
        if (plynling.DiedAt is not null) return (CareOutcome.Dead, plynling);
        if (plynling.FrozenAt is not null) return (CareOutcome.Frozen, plynling);

        PlynlingLife.Pet(plynling, now);
        await _db_context.SaveChangesAsync();
        return (CareOutcome.Done, plynling);
    }

    public async Task<(FreezeOutcome Outcome, Plynling? Plynling)> FreezeAsync(
        ulong guildId, ulong ownerId, bool byStaff, DateTimeOffset now)
    {
        var plynling = await GetCurrentAsync(guildId, ownerId, now);
        if (plynling is null) return (FreezeOutcome.NoPlynling, null);

        // Staff are exempt from the self-freeze rules, but not from reality.
        var blocker = byStaff
            ? (plynling.DiedAt is not null ? FreezeOutcome.Dead
               : plynling.FrozenAt is not null ? FreezeOutcome.AlreadyFrozen
               : (FreezeOutcome?)null)
            : PlynlingLife.SelfFreezeBlocker(plynling, now);
        if (blocker is { } b) return (b, plynling);

        PlynlingLife.Freeze(plynling, now, byStaff);
        await _db_context.SaveChangesAsync();
        return (FreezeOutcome.Frozen, plynling);
    }

    public async Task<(ThawOutcome Outcome, Plynling? Plynling)> ThawAsync(
        ulong guildId, ulong ownerId, bool byStaff, DateTimeOffset now)
    {
        var plynling = await GetCurrentAsync(guildId, ownerId, now);
        if (plynling is null) return (ThawOutcome.NoPlynling, null);
        if (plynling.DiedAt is not null) return (ThawOutcome.Dead, plynling);
        if (plynling.FrozenAt is null) return (ThawOutcome.NotFrozen, plynling);
        // A staff freeze is lifted by staff only; an owner may end their own self-freeze.
        if (plynling.FrozenByStaff && !byStaff) return (ThawOutcome.StaffOnly, plynling);

        PlynlingLife.Thaw(plynling, now);
        await _db_context.SaveChangesAsync();
        return (ThawOutcome.Thawed, plynling);
    }

    // Reaches the shown Plynling — living, or else the latest grave — because a grave
    // shows its name publicly too, and an offensive one needs fixing there as well.
    public async Task<(Plynling? Plynling, string OldName)> RenameAsync(
        ulong guildId, ulong ownerId, string name, DateTimeOffset now)
    {
        var plynling = await GetShownAsync(guildId, ownerId, now);
        if (plynling is null) return (null, string.Empty);

        var oldName = plynling.Name;
        plynling.Name = name;
        await _db_context.SaveChangesAsync();
        return (plynling, oldName);
    }

    public async Task<(ResurrectOutcome Outcome, Plynling? Plynling)> ResurrectAsync(
        ulong guildId, ulong ownerId, DateTimeOffset now)
    {
        var current = await GetCurrentAsync(guildId, ownerId, now);
        if (current is { DiedAt: null }) return (ResurrectOutcome.AlreadyHasOne, current);

        // `current`, if set, is one that just starved on this very read — the latest grave.
        var grave = current ?? await GetLatestDeadAsync(guildId, ownerId);
        if (grave is null) return (ResurrectOutcome.NoGrave, null);

        PlynlingLife.Resurrect(grave, now);
        try
        {
            await _db_context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return (ResurrectOutcome.AlreadyHasOne, null);   // an adoption won the race
        }
        return (ResurrectOutcome.Resurrected, grave);
    }

    public async Task<List<Plynling>> GetGraveyardAsync(ulong guildId, ulong? ownerId, DateTimeOffset now)
    {
        // Settle the living first, so anything that starved since the last sweep is
        // already in the ground when someone opens the graveyard.
        var living = await _db_context.Plynlings.Where(x => x.GuildId == guildId && x.DiedAt == null).ToListAsync();
        var changed = false;
        foreach (var plynling in living)
            changed |= PlynlingLife.Settle(plynling, now);
        if (changed) await _db_context.SaveChangesAsync();

        var query = _db_context.Plynlings.Where(x => x.GuildId == guildId && x.DiedAt != null);
        if (ownerId is { } owner) query = query.Where(x => x.OwnerId == owner);
        return await query.ToListAsync();
    }

    // Every Plynling the hourly sweep has to look at: the living, and deaths not yet announced.
    public async Task<List<Plynling>> GetSweepBatchAsync() =>
        await _db_context.Plynlings.Where(x => x.DiedAt == null || !x.DeathAnnounced).ToListAsync();

    public Task SaveAsync() => _db_context.SaveChangesAsync();

    private async Task<Plynling?> SettledAsync(Plynling? plynling, DateTimeOffset now)
    {
        if (plynling is not null && PlynlingLife.Settle(plynling, now))
            await _db_context.SaveChangesAsync();
        return plynling;
    }
}
```

- [ ] **Step 7: Register both in `ProjectSYNCS/Program.cs`**, beside the other transient services:

```csharp
        services.AddTransient<PebbleService>();
        services.AddTransient<PlynlingService>();
```

- [ ] **Step 8: Generate the migration and check the filter made it in**

Run: `cd ProjectSYNCS && dotnet ef migrations add AddPlynlingsAndPebbles`
Run: `grep -n "DiedAt\\\\\" IS NULL" Migrations/*_AddPlynlingsAndPebbles.cs`
Expected: one line containing `filter: "\"DiedAt\" IS NULL"`. If absent, the partial index was not generated — stop and fix the mapping before continuing.

- [ ] **Step 9: Verify**

Run: `cd ProjectSYNCS && dotnet build -warnaserror` → clean.
Run: `dotnet run --project "$SCRATCH/plynlingdb"` → `N passed, 0 failed`.

- [ ] **Step 10: CLAUDE.md** — add after the `GuildConfigService` note:

```markdown
**Plynlings compute their state; nothing ticks.** `Plynling` stores hunger and happiness
*as they were at* `NeedsAsOf`, and `Helpers/PlynlingLife` derives the current values from
elapsed time — the voice taper's approach. Every transition rebases (computes, stores,
restarts the clock). `PlynlingLife.Settle` brings a row up to now — an expired self-freeze
thaws *at the moment it was due*, then a starved one dies *at the moment it starved* — and
**every read in `PlynlingService` goes through it**, so a death found by a command and one
found by the hourly sweep are identical: same instant, same age, same memorial. Age is
`AgeBankedSeconds` plus the live stretch since `LiveSince`; frozen and dead time is never
banked, which is what the memorial tier is measured on.

**One living Plynling per person is enforced by the database**, with a partial unique index
(`HasFilter("\"DiedAt\" IS NULL")`) — so racing adoptions, or a resurrection racing an
adoption, cannot both land, while the graveyard holds any number of dead rows. The services
still check first; the index is what makes the check safe.

**Feeding pays and feeds in one save.** `AppDbContext` is transient, so charging through
`PebbleService` would be a second context and a second `SaveChanges`; `PlynlingService.FeedAsync`
loads the wallet through `PebbleService.GetOrCreateWalletAsync(its own context, …)` instead.

**The passive-income cap lives on the wallet row** (`PassiveDay` + `PassiveToday`), not in a
daily-bucket table: nothing ranks cailloux by date, so the leaderboards' totals+buckets pair
would be a table with no reader.
```

- [ ] **Step 11: Checkpoint** — stop and hand off to the user for review and commit.

---

### Task 5: Economy commands and passive income

**Files:**
- Create: `ProjectSYNCS/Helpers/PlynlingText.cs`
- Create: `ProjectSYNCS/Commands/EconomyModule.cs`
- Modify: `ProjectSYNCS/Services/BotResponses.cs` (add `WorkLines`; add to the table of contents)
- Modify: `ProjectSYNCS/Services/XpTracker.cs` (`GrantAsync` + new `GrantPassivePebblesAsync`)
- Test: `$SCRATCH/economycheck/Program.cs`

**Interfaces:**
- Consumes: `PebbleService` (Task 4), `PebbleEconomy` (Task 2), `ResponsePicker.Pick(ulong, string[])`.
- Produces: `PlynlingText` (static class of French refusal lines, extended in later tasks), `BotResponses.WorkLines` (`{0}` = the amount, e.g. "+47 cailloux").

- [ ] **Step 1: Write the failing harness**

Create `$SCRATCH/economycheck/economycheck.csproj` (harness csproj) and `$SCRATCH/economycheck/Program.cs`:

```csharp
using System.Reflection;
using ProjectSYNCS.Helpers;

int pass = 0, fail = 0;
void Check(string label, bool ok) { if (ok) pass++; else { fail++; Console.WriteLine("  FAIL  " + label); } }

var asm = typeof(ProjectSYNCS.Services.XpService).Assembly;
var responses = asm.GetType("ProjectSYNCS.Services.BotResponses")!;
string[] Pool(string name) => (string[])responses.GetField(name, BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!;

var work = Pool("WorkLines");
Check("WorkLines has at least 8 lines", work.Length >= 8);
foreach (var line in work)
{
    try { var s = string.Format(line, "+47 cailloux"); Check($"formats: {line[..Math.Min(30, line.Length)]}", s.Contains("+47 cailloux")); }
    catch (FormatException) { Check($"format-safe: {line}", false); }
    Check($"uses no {{1}}: {line[..Math.Min(30, line.Length)]}", !line.Contains("{1}"));
}
Check("WorkLines are listed in the table of contents",
    File.ReadAllText(@"C:\Users\c235773\Desktop\Sources - RME\Discord Bots\SessionOrganizer\ProjectSYNCS\Services\BotResponses.cs")
        .Split("internal static class BotResponses")[0].Contains("WorkLines"));
Check("work cooldown line has a live timestamp", PlynlingText.WorkCooldown(DateTimeOffset.FromUnixTimeSeconds(1_800_000_000)).Contains("<t:1800000000:R>"));

Console.WriteLine($"{pass} passed, {fail} failed");
if (fail > 0) Environment.Exit(1);
```

- [ ] **Step 2: Run it — expect a compile failure** (`PlynlingText` does not exist).

- [ ] **Step 3: Create `ProjectSYNCS/Helpers/PlynlingText.cs`**

```csharp
namespace ProjectSYNCS.Helpers;

// Fixed French lines for refusals and plain notices. Deliberately not ResponsePicker
// pools: the pools exist so repeated *chatter* doesn't repeat, and a refusal is not
// chatter — varied, it would read as scripted.
public static class PlynlingText
{
    public static string WorkCooldown(DateTimeOffset next) =>
        $"Tu as déjà travaillé. Prochain service <t:{next.ToUnixTimeSeconds()}:R>.";
}
```

- [ ] **Step 4: Add `WorkLines` to `ProjectSYNCS/Services/BotResponses.cs`**

Add the pool (anywhere inside the class; next to `YesLines` is natural):

```csharp
    // /work results. {0} = what was earned, already formatted ("+47 cailloux"). The jobs
    // are absurd on purpose: the money is real, the employment is not.
    public static readonly string[] WorkLines =
    {
        "Tu as trié des spores toute la matinée. Passionnant. {0}",
        "Tu as ramassé des cailloux au bord de la rivière. Littéralement. {0}",
        "Tu as aidé un escargot à traverser la route. Il t'a payé, bizarrement. {0}",
        "Tu as nettoyé les chapeaux de trois Plynlings capricieux. {0}",
        "Tu as tenu la caisse du marché aux champignons. {0}",
        "Tu as creusé un tunnel pour une taupe syndiquée. {0}",
        "Service de nuit à la cueillette des morilles. Les mains sales, mais {0}",
        "Tu as livré du terreau dans tout le village. Ton dos s'en souviendra. {0}",
        "Tu as servi de guide à des touristes perdus dans la forêt. {0}",
        "Tu as poli des cailloux. On t'a payé en cailloux. La boucle est bouclée. {0}",
    };
```

And in the table of contents at the top of the file, under `Commands`, add:

```
//     WorkLines ................ /work
```

- [ ] **Step 5: Create `ProjectSYNCS/Commands/EconomyModule.cs`**

```csharp
using Discord;
using Discord.Interactions;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Commands;

// The cailloux commands. Guild-only: a wallet belongs to one server.
[CommandContextType(InteractionContextType.Guild)]
public class EconomyModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly PebbleService _pebbles;
    private readonly ResponsePicker _picker;

    public EconomyModule(PebbleService pebbles, ResponsePicker picker)
    {
        _pebbles = pebbles;
        _picker = picker;
    }

    // Public, in her voice: routine actions are public because they happen in a
    // dedicated channel. Works without owning a Plynling — people save up before
    // adopting, and keep earning after a death.
    [SlashCommand("work", "Travailler pour gagner des cailloux (toutes les 4 h)")]
    public async Task WorkAsync()
    {
        var now = DateTimeOffset.UtcNow;
        var result = await _pebbles.WorkAsync(Context.Guild.Id, Context.User.Id, PebbleEconomy.RollWorkPay(), now);
        if (!result.Paid)
        {
            await RespondAsync(PlynlingText.WorkCooldown(result.NextWorkAt), ephemeral: true);
            return;
        }

        var line = string.Format(_picker.Pick(Context.Channel.Id, BotResponses.WorkLines),
            $"+{PebbleEconomy.Cailloux(result.Amount)}");
        await RespondAsync(
            $"{line}\n-# Solde : {PebbleEconomy.Cailloux(result.Balance)} · prochain service <t:{result.NextWorkAt.ToUnixTimeSeconds()}:R>",
            allowedMentions: AllowedMentions.None);
    }

    // Private: balances are nobody else's business, and nothing in v1 needs a rich list.
    // The passive line makes the cap visible rather than mysterious.
    [SlashCommand("balance", "Voir tes cailloux (visible par toi seul)")]
    public async Task BalanceAsync()
    {
        var balance = await _pebbles.GetBalanceAsync(Context.Guild.Id, Context.User.Id);
        var now = DateTimeOffset.UtcNow;
        var work = balance.NextWorkAt is { } next && next > now
            ? $"<t:{next.ToUnixTimeSeconds()}:R>"
            : "disponible maintenant";

        await RespondAsync(
            $"🪨 **{PebbleEconomy.Cailloux(balance.Balance)}**\n" +
            $"Prochain `/work` : {work}\n" +
            $"Cailloux passifs aujourd'hui : {balance.PassiveToday} / {PebbleEconomy.PassiveDailyCap}",
            ephemeral: true);
    }
}
```

- [ ] **Step 6: Hook passive income into `ProjectSYNCS/Services/XpTracker.cs`**

In `GrantAsync`, immediately after the line `var (oldLevel, newLevel) = await xp.AddXpAsync(guildId, userId, amount);`, add:

```csharp
            await GrantPassivePebblesAsync(scope, guildId, userId);
```

And add this method to the class:

```csharp
    // Passive cailloux ride on every XP grant: the XP cooldowns, excluded channels and
    // voice rules have already decided this grant was earned, so the bonus needs no
    // defences of its own. PebbleService caps it at a share of a day's /work, which is
    // what keeps it a bonus. Its own try: a failure here must not cost the level-up
    // announcement that follows.
    private async Task GrantPassivePebblesAsync(AsyncServiceScope scope, ulong guildId, ulong userId)
    {
        try
        {
            var pebbles = scope.ServiceProvider.GetRequiredService<PebbleService>();
            await pebbles.AddPassiveAsync(guildId, userId, PebbleEconomy.PassivePerGrant);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to grant passive cailloux to user {UserId} in guild {GuildId}.", userId, guildId);
        }
    }
```

- [ ] **Step 7: Verify**

Run: `cd ProjectSYNCS && dotnet build -warnaserror` → clean.
Run: `dotnet run --project "$SCRATCH/economycheck"` → `N passed, 0 failed`.
Run: `dotnet run --project "$SCRATCH/plynlingcheck"` and `"$SCRATCH/plynlingdb"` → still `0 failed`.

- [ ] **Step 8: CLAUDE.md** — add after the Task 4 notes:

```markdown
**Passive cailloux are granted from `XpTracker.GrantAsync`, never detected separately.** Every
XP grant (message, reaction, voice, verdict and bot-interaction bonuses) pays
`PebbleEconomy.PassivePerGrant`, capped per day at 30% of three average `/work` shifts. So
the cap inherits every XP defence for free — cooldowns, excluded channels, voice
eligibility — and can never pay for something XP refused. It runs in its own `try` so a
failure cannot swallow the level-up card. Plynling actions themselves grant **no XP**: the
link runs one way only, or money and levels would feed each other.
```

- [ ] **Step 9: Checkpoint** — stop and hand off to the user for review and commit.

---

### Task 6: The card, adoption, viewing, feeding and petting

**Files:**
- Modify: `ProjectSYNCS/Helpers/InputCaps.cs` (add `PlynlingName`)
- Create: `ProjectSYNCS/Helpers/PlynlingCardUi.cs`
- Modify: `ProjectSYNCS/Helpers/PlynlingText.cs` (care refusals)
- Create: `ProjectSYNCS/Services/PlynlingCooldowns.cs`
- Create: `ProjectSYNCS/Services/PlynlingCareService.cs`
- Create: `ProjectSYNCS/Commands/PlynlingModule.cs`
- Create: `ProjectSYNCS/Interactions/Components/PlynlingComponentHandler.cs`
- Modify: `ProjectSYNCS/Services/BotResponses.cs` (four pools + table of contents)
- Modify: `ProjectSYNCS/Program.cs`
- Test: `$SCRATCH/plynlingui/Program.cs`

**Interfaces:**
- Consumes: `PlynlingService` (Task 4), `PlynlingLife`/`PlynlingCatalog`/`PebbleEconomy` (Task 2), `PlynlingArt` (Task 3), `LevelCardUi.ProgressBar(long, long)`, `LevelCardUi.Duration(long)`.
- Produces:
  - `InputCaps.PlynlingName` (32)
  - `PlynlingCardUi`: `SafeName(string)`, `Percent(double)`, `Bar(double)`, `Heading(Plynling, DateTimeOffset)`, `Status(Plynling, DateTimeOffset)`, `MoodLabel(PlynlingMood)`, `FoodEffect(FoodInfo)`
  - `PlynlingCooldowns.Pet : CooldownGate<(ulong Petter, int PlynlingId)>`
  - `record CareReply(MessageComponent? Card, string? Refusal)`; `PlynlingCareService.PetAsync(int plynlingId, ulong actorId, ulong channelId, DateTimeOffset now)`, `FeedAsync(int plynlingId, ulong actorId, PlynlingFood food, ulong channelId, DateTimeOffset now)`, `static Refusal(CareOutcome)`
  - `PlynlingModule.BuildCard(Plynling, DateTimeOffset, string? lastAction, string? lastActionImage = null) → MessageComponent` — when an image is given it sits beside the line (the food just eaten)
  - Custom ids: `plyn:pet:{id}` (button), `plyn:feed:{id}` (select, values are `PlynlingFood` names)
  - Pools: `PlynlingAdoptLines` ({0} name, {1} species), `PlynlingAdoptRareLines` ({0} name, {1} species, {2} rarity), `PlynlingFeedLines` ({0} name, {1} food with article), `PlynlingPetLines` ({0} name)

- [ ] **Step 1: Write the failing harness**

Create `$SCRATCH/plynlingui/plynlingui.csproj` (harness csproj) and `$SCRATCH/plynlingui/Program.cs`:

```csharp
using System.Collections;
using System.Reflection;
using Discord;
using ProjectSYNCS.Commands;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

int pass = 0, fail = 0;
void Check(string label, bool ok) { if (ok) pass++; else { fail++; Console.WriteLine("  FAIL  " + label); } }

// Walks a built message: every component, and every custom id in it.
static void Walk(object c, List<object> all)
{
    all.Add(c);
    foreach (var name in new[] { "Components", "Accessory" })
    {
        var value = c.GetType().GetProperty(name)?.GetValue(c);
        if (value is IEnumerable list and not string)
        {
            foreach (var x in list) if (x is not null) Walk(x, all);
        }
        else if (value is not null)
        {
            Walk(value, all);
        }
    }
}
(int Count, List<string> Ids) Inspect(MessageComponent m)
{
    var all = new List<object>();
    foreach (var top in m.Components) Walk(top, all);
    var ids = all.Select(x => x.GetType().GetProperty("CustomId")?.GetValue(x) as string).Where(s => s is not null).Select(s => s!).ToList();
    return (all.Count, ids);
}

var t0 = new DateTimeOffset(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);
Plynling Make(string name) { var p = PlynlingLife.Create(1, 42, name, PlynlingSpecies.Amanite, t0); p.Id = 7; return p; }

// --- a living card: two rows, unique ids, well under the cap ---
var alive = Make(new string('R', InputCaps.PlynlingName));
var card = PlynlingModule.BuildCard(alive, t0, "Rex ronronne — caressé par <@1>");
var (count, ids) = Inspect(card);
Check($"living card fits (count {count})", count <= 40);
Check("living card has the pet button and the feed select", ids.Contains("plyn:pet:7") && ids.Contains("plyn:feed:7"));
Check("living card ids are unique", ids.Count == ids.Distinct().Count());

// --- after a meal the food sprite sits beside the line, still under the cap ---
var fedCard = PlynlingModule.BuildCard(alive, t0, "Miam", PlynlingArt.Food(PlynlingFood.Truffle));
var (fedCount, fedIds) = Inspect(fedCard);
Check($"card with the food picture fits (count {fedCount})", fedCount <= 40 && fedIds.Count == fedIds.Distinct().Count());

// --- frozen and dead: no actions offered ---
var frozen = Make("Gelé"); PlynlingLife.Freeze(frozen, t0, byStaff: false);
Check("frozen card has no buttons", Inspect(PlynlingModule.BuildCard(frozen, t0, null)).Ids.Count == 0);
var dead = Make("Mort"); PlynlingLife.Settle(dead, t0 + TimeSpan.FromDays(10));
Check("dead card has no buttons", Inspect(PlynlingModule.BuildCard(dead, t0 + TimeSpan.FromDays(10), null)).Ids.Count == 0);

// --- text ---
Check("status has a live countdown", PlynlingCardUi.Status(alive, t0).Contains("<t:"));
Check("frozen status says frozen", PlynlingCardUi.Status(frozen, t0).Contains("Gelé"));
Check("a relative timestamp is never read as 'jusqu'à …'",
    !System.Text.RegularExpressions.Regex.IsMatch(PlynlingCardUi.Status(alive, t0), @"jusqu'à <t:\d+:R>"));
Check("the mood has its own line", PlynlingCardUi.Status(alive, t0).Contains("**Humeur**"));
Check("dead status names the memorial", PlynlingCardUi.Status(dead, t0 + TimeSpan.FromDays(10)).Contains("cairn"));
var hostile = PlynlingCardUi.SafeName("@everyone **x** <@123>");
Check("names are sanitised", !hostile.Contains("**") && !hostile.Contains("<@123>"));
foreach (var f in PlynlingCatalog.Foods)
    Check($"select label fits: {f.Name}", $"{f.Name} — {PebbleEconomy.Cailloux(f.Price)}".Length <= 100 && PlynlingCardUi.FoodEffect(f).Length <= 100);

// --- pools ---
var responses = typeof(ProjectSYNCS.Services.XpService).Assembly.GetType("ProjectSYNCS.Services.BotResponses")!;
string[] Pool(string n) => (string[])responses.GetField(n, BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!;
var toc = File.ReadAllText(@"C:\Users\c235773\Desktop\Sources - RME\Discord Bots\SessionOrganizer\ProjectSYNCS\Services\BotResponses.cs").Split("internal static class BotResponses")[0];
foreach (var (name, arity) in new[] { ("PlynlingAdoptLines", 2), ("PlynlingAdoptRareLines", 3), ("PlynlingFeedLines", 2), ("PlynlingPetLines", 1) })
{
    var pool = Pool(name);
    Check($"{name} has lines", pool.Length >= 3);
    Check($"{name} is in the table of contents", toc.Contains(name));
    foreach (var line in pool)
    {
        var fmtArgs = Enumerable.Range(0, arity).Select(i => (object)$"ARG{i}").ToArray();   // not "args": top-level programs already have one
        try { string.Format(line, fmtArgs); Check("format-safe", true); } catch (FormatException) { Check($"format-safe: {line}", false); }
        Check($"{name}: no placeholder beyond {{{arity - 1}}}", !line.Contains($"{{{arity}}}"));
    }
}

Console.WriteLine($"{pass} passed, {fail} failed");
if (fail > 0) Environment.Exit(1);
```

- [ ] **Step 2: Run it — expect a compile failure** (`PlynlingModule`, `PlynlingCardUi`, `InputCaps.PlynlingName` do not exist).

- [ ] **Step 3: Add the name cap to `ProjectSYNCS/Helpers/InputCaps.cs`**

```csharp
    /// <summary>
    /// A Plynling's name — Discord's own nickname limit. Shown on the card heading, in her
    /// lines and in public announcements, so it is capped at the option like every title.
    /// </summary>
    public const int PlynlingName = 32;
```

- [ ] **Step 4: Create `ProjectSYNCS/Helpers/PlynlingCardUi.cs`**

```csharp
using Discord;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

// The text on a Plynling card — pure string work, like LevelCardUi, so it is checkable
// without a gateway. The module assembles the components around it.
public static class PlynlingCardUi
{
    // Names are chosen by users and shown publicly: markdown and mention syntax are
    // neutralised here, and every send is AllowedMentions.None on top.
    public static string SafeName(string name) => Format.Sanitize(name);

    public static string Percent(double value) => $"{(int)Math.Round(value * 100)} %";

    public static string Bar(double value) => LevelCardUi.ProgressBar((long)Math.Round(value * 1000), 1000);

    public static string Heading(Plynling p, DateTimeOffset now)
    {
        var info = PlynlingCatalog.Info(p.Species);
        var age = LevelCardUi.Duration((long)PlynlingLife.Age(p, now).TotalMinutes);
        return $"## {SafeName(p.Name)}\n" +
               $"{info.Name} · *{PlynlingCatalog.RarityLabel(info.Rarity)}*\n" +
               $"à <@{p.OwnerId}> · âgé de {age}";
    }

    public static string Status(Plynling p, DateTimeOffset now)
    {
        if (p.DiedAt is { } died)
        {
            var lived = PlynlingLife.Age(p, now);
            var memorial = PlynlingCatalog.MemorialName(PlynlingCatalog.MemorialTier(lived));
            return $"🪦 Mort <t:{died.ToUnixTimeSeconds()}:R>, après {LevelCardUi.Duration((long)lived.TotalMinutes)} de vie. " +
                   $"Il repose sous {memorial}.";
        }

        var hunger = PlynlingLife.HungerAt(p, now);
        var happiness = PlynlingLife.HappinessAt(p, now);
        // A relative timestamp renders as "dans 2 jours", so the words before it must read
        // with that: "mourra de faim dans 2 jours", never "jusqu'à dans 2 jours". The
        // absolute :f form is the one that takes "jusqu'au".
        var clock = p.FrozenAt is not null
            ? p.FreezeUntil is { } until
                ? $"❄️ Gelé jusqu'au <t:{until.ToUnixTimeSeconds()}:f>"
                : "❄️ Gelé par le staff"
            : $"mourra de faim <t:{PlynlingLife.DeathAt(p)!.Value.ToUnixTimeSeconds()}:R>";

        // The mood gets its own line: it covers hunger as well as happiness, so beside the
        // happiness bar a starving Plynling would read "Bonheur 20 % · affamé".
        return $"**Faim** `{Bar(hunger)}` {Percent(hunger)} · {clock}\n" +
               $"**Bonheur** `{Bar(happiness)}` {Percent(happiness)}\n" +
               $"**Humeur** · *{MoodLabel(PlynlingLife.Mood(p, now))}*";
    }

    public static string MoodLabel(PlynlingMood mood) => mood switch
    {
        PlynlingMood.Happy => "heureux",
        PlynlingMood.Sad => "triste",
        PlynlingMood.Hungry => "affamé",
        PlynlingMood.Starving => "mourant de faim",
        PlynlingMood.Frozen => "gelé",
        _ => "content",
    };

    public static string FoodEffect(FoodInfo food)
    {
        var parts = new List<string>();
        if (food.Hunger > 0) parts.Add($"+{(int)Math.Round(food.Hunger * 100)} % de faim");
        if (food.Happiness > 0) parts.Add($"+{(int)Math.Round(food.Happiness * 100)} % de bonheur");
        return string.Join(", ", parts);
    }
}
```

- [ ] **Step 5: Extend `ProjectSYNCS/Helpers/PlynlingText.cs`** — add inside the class:

```csharp
    public const string NoPlynling = "Tu n'as pas de Plynling. `/plynling adopt` pour en adopter un !";
    public const string NotYours = "Ce n'est pas ton Plynling — seul son propriétaire peut le nourrir.";
    public const string Dead = "Ce Plynling n'est plus de ce monde… 🪦";
    public const string Frozen = "Ce Plynling est gelé : rien ne bouge tant qu'il n'est pas dégelé.";
    public const string Wasted = "Il n'a besoin de rien de tout ça pour l'instant — garde tes cailloux.";
    public const string PetCooldown = "Tu l'as caressé il y a peu. Reviens dans quelques heures.";
    public const string AlreadyHasOne = "Tu as déjà un Plynling. Un seul à la fois !";
    public const string EmptyName = "Il lui faut un vrai nom.";
    public const string Unknown = "Ce bouton ne correspond plus à rien.";

    public static string NoneFor(ulong userId) => $"<@{userId}> n'a pas de Plynling.";

    public static string TooPoor(long price, long balance) =>
        $"Il te faut {PebbleEconomy.Cailloux(price)}, tu n'en as que {balance}. `/work` pour en gagner.";
```

- [ ] **Step 6: Create `ProjectSYNCS/Services/PlynlingCooldowns.cs`**

```csharp
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// The in-memory Plynling cooldowns — a singleton, like every gate here. Only petting is
// rationed this way: a restart lets people pet once more early, which costs nothing,
// since petting can never keep a Plynling alive. Anything that touches money or death
// (/work, self-freeze) is stored in the database instead.
public sealed class PlynlingCooldowns
{
    public CooldownGate<(ulong Petter, int PlynlingId)> Pet { get; } =
        new(PlynlingLife.PetCooldown, forget: TimeSpan.FromHours(8));
}
```

- [ ] **Step 7: Add the four pools to `ProjectSYNCS/Services/BotResponses.cs`**

```csharp
    // A new Plynling, announced on the card. {0} = its name (sanitised), {1} = species.
    // Plynling is the grammatical subject throughout, so every line agrees in the
    // masculine whatever the species name's own gender.
    public static readonly string[] PlynlingAdoptLines =
    {
        "Un nouveau Plynling pointe le bout de son chapeau : **{0}**, espèce {1}. Nourris-le bien (˶ᵔ ᵕ ᵔ˶)",
        "**{0}** vient de sortir de terre ! Un Plynling {1}, tout frais tout mignon ✨",
        "Félicitations, c'est un Plynling ! **{0}** ({1}) te regarde déjà avec des yeux affamés.",
        "Un Plynling de plus dans le monde : **{0}**, {1}. Promets-moi de ne pas l'oublier.",
    };

    // The same moment for a rare or legendary pull, which is worth making a fuss about.
    // {0} = name, {1} = species, {2} = rarity label.
    public static readonly string[] PlynlingAdoptRareLines =
    {
        "QUOI ?! Un Plynling **{2}** ! **{0}** est un {1}… tu as une chance insolente ✨✨",
        "Je n'en crois pas mes capteurs : **{0}**, un {1}. C'est **{2}**, ça. Garde-le en vie, par pitié.",
        "Alerte rareté : **{0}** ({1}, *{2}*) vient de naître. Tout le serveur va être jaloux ദ്ദി◝ ⩊ ◜.ᐟ",
    };

    // Shown on the card after a meal. {0} = name, {1} = the food with its article.
    public static readonly string[] PlynlingFeedLines =
    {
        "Tu donnes {1} à **{0}**. Il n'en fait qu'une bouchée (˶˃ ᵕ ˂˶)",
        "**{0}** a dévoré {1}. Il te regarde comme si tu étais la meilleure personne du monde.",
        "Miam ! {1} pour **{0}**, qui fait une petite danse de joie ✨",
        "**{0}** grignote {1} avec une concentration impressionnante.",
    };

    // Shown on the card after a pet. {0} = name.
    public static readonly string[] PlynlingPetLines =
    {
        "**{0}** ronronne. Oui, les Plynlings ronronnent, ne pose pas de questions.",
        "**{0}** ferme les yeux et savoure la caresse (˶ᵔ ᵕ ᵔ˶)",
        "Le chapeau de **{0}** frétille de bonheur ✨",
        "**{0}** se blottit contre ta main. C'est officiel, vous êtes amis.",
        "**{0}** fait un petit bruit satisfait. Encore, encore !",
    };
```

In the table of contents, add a group before `Per-person data and lookups`:

```
//   Plynlings
//     PlynlingAdoptLines · PlynlingAdoptRareLines ... a new Plynling
//     PlynlingFeedLines · PlynlingPetLines .......... shown on the card
```

- [ ] **Step 8: Create `ProjectSYNCS/Services/PlynlingCareService.cs`**

```csharp
using Discord;
using ProjectSYNCS.Commands;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Services;

// Either a re-rendered card (the action happened), or a refusal to send privately.
public sealed record CareReply(MessageComponent? Card, string? Refusal);

// Feeding and petting, shared by the slash commands and the card's buttons so the two
// can never behave differently. Transient: it wraps PlynlingService.
public class PlynlingCareService
{
    private readonly PlynlingService _plynlings;
    private readonly PlynlingCooldowns _cooldowns;
    private readonly ResponsePicker _picker;

    public PlynlingCareService(PlynlingService plynlings, PlynlingCooldowns cooldowns, ResponsePicker picker)
    {
        _plynlings = plynlings;
        _cooldowns = cooldowns;
        _picker = picker;
    }

    // Anyone may pet anyone's Plynling; the 4 h cooldown is per petter per Plynling. The
    // claim is released if the pet does not happen, so a refusal never costs a cooldown.
    public async Task<CareReply> PetAsync(int plynlingId, ulong actorId, ulong channelId, DateTimeOffset now)
    {
        var key = (actorId, plynlingId);
        if (!_cooldowns.Pet.TryClaim(key)) return new CareReply(null, PlynlingText.PetCooldown);

        var (outcome, plynling) = await _plynlings.PetAsync(plynlingId, now);
        if (outcome != CareOutcome.Done || plynling is null)
        {
            _cooldowns.Pet.Release(key);
            return new CareReply(null, Refusal(outcome));
        }

        var line = string.Format(_picker.Pick(channelId, BotResponses.PlynlingPetLines), PlynlingCardUi.SafeName(plynling.Name));
        return new CareReply(PlynlingModule.BuildCard(plynling, now, $"{line} — caressé par <@{actorId}>"), null);
    }

    public async Task<CareReply> FeedAsync(int plynlingId, ulong actorId, PlynlingFood food, ulong channelId, DateTimeOffset now)
    {
        var result = await _plynlings.FeedAsync(plynlingId, actorId, food, now);
        if (result.Outcome != CareOutcome.Done || result.Plynling is null)
        {
            return new CareReply(null, result.Outcome == CareOutcome.TooPoor
                ? PlynlingText.TooPoor(result.Price, result.Balance)
                : Refusal(result.Outcome));
        }

        var info = PlynlingCatalog.Info(food);
        var line = string.Format(_picker.Pick(channelId, BotResponses.PlynlingFeedLines),
            PlynlingCardUi.SafeName(result.Plynling.Name), info.WithArticle);
        return new CareReply(PlynlingModule.BuildCard(result.Plynling, now,
            $"{line}\n-# −{PebbleEconomy.Cailloux(info.Price)} · il te reste {PebbleEconomy.Cailloux(result.Balance)}",
            PlynlingArt.Food(food)), null);
    }

    public static string Refusal(CareOutcome outcome) => outcome switch
    {
        CareOutcome.NoPlynling => PlynlingText.NoPlynling,
        CareOutcome.NotOwner => PlynlingText.NotYours,
        CareOutcome.Dead => PlynlingText.Dead,
        CareOutcome.Frozen => PlynlingText.Frozen,
        CareOutcome.Wasted => PlynlingText.Wasted,
        _ => PlynlingText.Unknown,
    };
}
```

- [ ] **Step 9: Create `ProjectSYNCS/Commands/PlynlingModule.cs`**

```csharp
using Discord;
using Discord.Interactions;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Commands;

// /plynling — adopting, looking after and (for staff) managing Plynlings. Guild-only:
// every query is scoped to Context.Guild.Id.
//
// No DeferAsync anywhere: every action is one or two row reads and one write, well inside
// Discord's 3 s, and not deferring is what lets a success be a *public* V2 card while a
// refusal stays *private* — a public "thinking…" cannot become an ephemeral reply.
[CommandContextType(InteractionContextType.Guild)]
[Group("plynling", "Ton Plynling : l'adopter, t'en occuper, le regarder vivre")]
public class PlynlingModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly PlynlingService _plynlings;
    private readonly PlynlingCareService _care;
    private readonly ResponsePicker _picker;

    public PlynlingModule(PlynlingService plynlings, PlynlingCareService care, ResponsePicker picker)
    {
        _plynlings = plynlings;
        _care = care;
        _picker = picker;
    }

    [SlashCommand("adopt", "Adopter un Plynling — gratuit, mais il faudra s'en occuper")]
    public async Task AdoptAsync(
        [Summary("name", "Son nom")] [MaxLength(InputCaps.PlynlingName)] string name)
    {
        name = name.Trim();
        if (name.Length == 0)
        {
            await RespondAsync(PlynlingText.EmptyName, ephemeral: true);
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var species = PlynlingCatalog.RollSpecies();
        var (outcome, plynling) = await _plynlings.AdoptAsync(Context.Guild.Id, Context.User.Id, name, species, now);
        if (outcome != AdoptOutcome.Adopted || plynling is null)
        {
            await RespondAsync(PlynlingText.AlreadyHasOne, ephemeral: true);
            return;
        }

        var info = PlynlingCatalog.Info(species);
        var pool = info.Rarity >= PlynlingRarity.Rare ? BotResponses.PlynlingAdoptRareLines : BotResponses.PlynlingAdoptLines;
        var line = string.Format(_picker.Pick(Context.Channel.Id, pool),
            PlynlingCardUi.SafeName(plynling.Name), info.Name, PlynlingCatalog.RarityLabel(info.Rarity));
        await RespondCardAsync(plynling, now, line);
    }

    [SlashCommand("view", "Voir un Plynling (le tien par défaut)")]
    public async Task ViewAsync(
        [Summary("user", "À qui est le Plynling (par défaut : toi)")] IUser? user = null)
    {
        var target = user ?? Context.User;
        var now = DateTimeOffset.UtcNow;
        var plynling = await _plynlings.GetShownAsync(Context.Guild.Id, target.Id, now);
        if (plynling is null)
        {
            await RespondAsync(target.Id == Context.User.Id ? PlynlingText.NoPlynling : PlynlingText.NoneFor(target.Id),
                ephemeral: true, allowedMentions: AllowedMentions.None);
            return;
        }
        await RespondCardAsync(plynling, now, null);
    }

    [SlashCommand("feed", "Nourrir ton Plynling")]
    public async Task FeedAsync([Summary("food", "Quoi lui donner")] PlynlingFood food)
    {
        var now = DateTimeOffset.UtcNow;
        var plynling = await _plynlings.GetCurrentAsync(Context.Guild.Id, Context.User.Id, now);
        if (plynling is null)
        {
            await RespondAsync(PlynlingText.NoPlynling, ephemeral: true);
            return;
        }
        await SendAsync(await _care.FeedAsync(plynling.Id, Context.User.Id, food, Context.Channel.Id, now));
    }

    [SlashCommand("pet", "Caresser un Plynling (le tien par défaut)")]
    public async Task PetAsync(
        [Summary("user", "À qui est le Plynling (par défaut : toi)")] IUser? user = null)
    {
        var target = user ?? Context.User;
        var now = DateTimeOffset.UtcNow;
        var plynling = await _plynlings.GetCurrentAsync(Context.Guild.Id, target.Id, now);
        if (plynling is null)
        {
            await RespondAsync(target.Id == Context.User.Id ? PlynlingText.NoPlynling : PlynlingText.NoneFor(target.Id),
                ephemeral: true, allowedMentions: AllowedMentions.None);
            return;
        }
        await SendAsync(await _care.PetAsync(plynling.Id, Context.User.Id, Context.Channel.Id, now));
    }

    // ---- rendering --------------------------------------------------------------

    private Task RespondCardAsync(Plynling plynling, DateTimeOffset now, string? line) =>
        RespondAsync(components: BuildCard(plynling, now, line), flags: MessageFlags.ComponentsV2,
            allowedMentions: AllowedMentions.None);

    private Task SendAsync(CareReply reply) =>
        reply.Card is not null
            ? RespondAsync(components: reply.Card, flags: MessageFlags.ComponentsV2, allowedMentions: AllowedMentions.None)
            : RespondAsync(reply.Refusal, ephemeral: true, allowedMentions: AllowedMentions.None);

    /// <summary>
    /// The card: picture (the sprite for its mood, or its memorial once dead), heading,
    /// bars, an optional line in her voice, and — only while alive and not frozen — a
    /// "Caresser" button and a "Nourrir…" select.
    /// </summary>
    /// <remarks>
    /// Static and Context-free so its component budget is checkable without a gateway.
    /// The two rows use different verbs (<c>plyn:pet</c>, <c>plyn:feed</c>): duplicated
    /// custom ids are rejected outright by Discord, disabled components included.
    /// Nourrir is offered to everyone and refused in the handler for anyone but the
    /// owner — the real check is in code, as with every gate here.
    /// </remarks>
    public static MessageComponent BuildCard(
        Plynling plynling, DateTimeOffset now, string? lastAction, string? lastActionImage = null)
    {
        var info = PlynlingCatalog.Info(plynling.Species);
        var alive = plynling.DiedAt is null;
        var picture = alive
            ? PlynlingArt.Sprite(plynling.Species, PlynlingLife.Mood(plynling, now))
            : PlynlingArt.Memorial(plynling.Species, PlynlingCatalog.MemorialTier(PlynlingLife.Age(plynling, now)));

        var container = new ContainerBuilder()
            .WithAccentColor(new Color(info.Accent))
            .AddComponent(new SectionBuilder()
                .WithAccessory(new ThumbnailBuilder()
                    .WithMedia(new UnfurledMediaItemProperties(picture))
                    .WithDescription(info.Name))
                .AddComponent(new TextDisplayBuilder(PlynlingCardUi.Heading(plynling, now))))
            .AddComponent(new SeparatorBuilder())
            .AddComponent(new TextDisplayBuilder(PlynlingCardUi.Status(plynling, now)));
        if (!string.IsNullOrWhiteSpace(lastAction))
        {
            // After a meal the food's own sprite sits beside her line — the one place the
            // food art appears, since a select menu cannot carry images.
            container.AddComponent(lastActionImage is null
                ? new TextDisplayBuilder(lastAction)
                : new SectionBuilder()
                    .WithAccessory(new ThumbnailBuilder().WithMedia(new UnfurledMediaItemProperties(lastActionImage)))
                    .AddComponent(new TextDisplayBuilder(lastAction)));
        }

        var builder = new ComponentBuilderV2().AddComponent(container);
        if (alive && plynling.FrozenAt is null)
        {
            builder.AddComponent(new ActionRowBuilder()
                .WithButton("🤲 Caresser", $"plyn:pet:{plynling.Id}", ButtonStyle.Primary));

            var menu = new SelectMenuBuilder()
                .WithCustomId($"plyn:feed:{plynling.Id}")
                .WithPlaceholder("🍄 Nourrir…");
            foreach (var food in PlynlingCatalog.Foods)
                menu.AddOption($"{food.Name} — {PebbleEconomy.Cailloux(food.Price)}", food.Food.ToString(),
                    PlynlingCardUi.FoodEffect(food));
            builder.AddComponent(new ActionRowBuilder().WithSelectMenu(menu));
        }
        return builder.Build();
    }
}
```

- [ ] **Step 10: Create `ProjectSYNCS/Interactions/Components/PlynlingComponentHandler.cs`**

```csharp
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Interactions.Components;

// The Plynling card's two controls. A success rewrites the card in place, with her line
// on it, rather than posting a second message; a refusal goes privately to whoever
// pressed, and the card is left untouched.
public class PlynlingComponentHandler : InteractionModuleBase<SocketInteractionContext>
{
    private readonly PlynlingCareService _care;

    public PlynlingComponentHandler(PlynlingCareService care)
    {
        _care = care;
    }

    [ComponentInteraction("plyn:pet:*", ignoreGroupNames: true)]
    public async Task OnPetAsync(string idStr)
    {
        if (!int.TryParse(idStr, out var id))
        {
            await RespondAsync(PlynlingText.Unknown, ephemeral: true);
            return;
        }
        await ApplyAsync(await _care.PetAsync(id, Context.User.Id, Context.Channel.Id, DateTimeOffset.UtcNow));
    }

    [ComponentInteraction("plyn:feed:*", ignoreGroupNames: true)]
    public async Task OnFeedAsync(string idStr, string[] selected)
    {
        if (!int.TryParse(idStr, out var id) || selected.Length == 0 || !Enum.TryParse<PlynlingFood>(selected[0], out var food))
        {
            await RespondAsync(PlynlingText.Unknown, ephemeral: true);
            return;
        }
        await ApplyAsync(await _care.FeedAsync(id, Context.User.Id, food, Context.Channel.Id, DateTimeOffset.UtcNow));
    }

    private async Task ApplyAsync(CareReply reply)
    {
        if (reply.Card is null)
        {
            await RespondAsync(reply.Refusal, ephemeral: true, allowedMentions: AllowedMentions.None);
            return;
        }

        await ((SocketMessageComponent)Context.Interaction).UpdateAsync(m =>
        {
            m.Components = reply.Card;
            // Re-asserted on every edit: an update without it is rejected on a V2 message.
            m.Flags = MessageFlags.ComponentsV2;
            m.AllowedMentions = AllowedMentions.None;
        });
    }
}
```

- [ ] **Step 11: Register in `ProjectSYNCS/Program.cs`**

```csharp
        services.AddTransient<PlynlingCareService>();
        services.AddSingleton<PlynlingCooldowns>();
```

- [ ] **Step 12: Verify**

Run: `cd ProjectSYNCS && dotnet build -warnaserror` → clean.
Run: `dotnet run --project "$SCRATCH/plynlingui"` → `N passed, 0 failed`.
Run the Task 2, 4 and 5 harnesses → still `0 failed`.

- [ ] **Step 13: CLAUDE.md** — add:

```markdown
**The Plynling card is Components V2 and follows every rule `/level` does** — no content or
embeds, the flag re-asserted on each `UpdateAsync`, `AllowedMentions.None` on every send.
Its two controls use two verbs (`plyn:pet:{id}`, `plyn:feed:{id}`), and they are offered
only while the Plynling is alive and not frozen. "Nourrir" is a select *on the card*
rather than a button opening a second message: one fewer round trip, and the handler
refuses it for anyone but the owner — the real check is in code. A button press rewrites
the card in place with her line on it, instead of posting a second message under it.
`PlynlingCareService` is shared by the slash commands and the buttons, so the two can
never behave differently. The pet cooldown is an in-memory `CooldownGate` keyed on
(petter, Plynling), released when the pet is refused; a restart resetting it costs nothing,
since petting cannot keep a Plynling alive.

**Plynling names are hostile input.** They are rendered through `PlynlingCardUi.SafeName`
(`Format.Sanitize` — markdown and mention syntax neutralised) *and* every message carrying
one is sent with `AllowedMentions.None`. The death announcement is public, so a Plynling
named `@everyone` would otherwise ping the server on its way out.
```

- [ ] **Step 14: Checkpoint** — stop and hand off to the user for review and commit.

---

### Task 7: Freeze, thaw, rename, resurrect — and owner notifications

**Files:**
- Create: `ProjectSYNCS/Services/PlynlingAnnouncer.cs`
- Modify: `ProjectSYNCS/Commands/PlynlingModule.cs` (four subcommands; constructor gains `PlynlingAnnouncer`)
- Modify: `ProjectSYNCS/Helpers/PlynlingText.cs` (freeze/thaw/staff lines)
- Modify: `ProjectSYNCS/Services/BotResponses.cs` (five pools + table of contents)
- Modify: `ProjectSYNCS/Program.cs`
- Test: `$SCRATCH/plynlingui/Program.cs` (extend)

**Interfaces:**
- Consumes: `PlynlingService.FreezeAsync/ThawAsync/RenameAsync/ResurrectAsync` (Task 4), `SessionPermissions.IsStaff(IUser)`.
- Produces:
  - `PlynlingAnnouncer.GameChannelId` (`878305034432045080`), `AnnounceDeathAsync(Plynling, DateTimeOffset)`, `AnnounceResurrectionAsync(Plynling, DateTimeOffset)`, `WarnOwnerAsync(Plynling)`, `DmOwnerAsync(ulong ownerId, string line)`
  - Pools: `PlynlingDeathLines` ({0} name, {1} owner mention, {2} lived, {3} memorial), `PlynlingResurrectLines` ({0} name, {1} owner mention), `PlynlingWarningLines` ({0} name, {1} timestamp), `PlynlingStaffFreezeDms` ({0}), `PlynlingStaffThawDms` ({0}), `PlynlingStaffRenameDms` ({0} old, {1} new)

- [ ] **Step 1: Extend the harness** — append to the pool loop in `$SCRATCH/plynlingui/Program.cs` (the `foreach (var (name, arity) in new[] { … })` array):

```csharp
("PlynlingDeathLines", 4), ("PlynlingResurrectLines", 2), ("PlynlingWarningLines", 2),
("PlynlingStaffFreezeDms", 1), ("PlynlingStaffThawDms", 1), ("PlynlingStaffRenameDms", 2)
```

and add before the final summary:

```csharp
Check("game channel id", ProjectSYNCS.Services.PlynlingAnnouncer.GameChannelId == 878305034432045080UL);
Check("warning lines read with a relative timestamp",
    Pool("PlynlingWarningLines").All(l => !l.Contains("jusqu'à {1}") && !l.Contains("que {1}")));
```

- [ ] **Step 2: Run it — expect failures** (the pools and `PlynlingAnnouncer` do not exist).

- [ ] **Step 3: Add the pools to `ProjectSYNCS/Services/BotResponses.cs`**

```csharp
    // Posted publicly in the game channel, with the memorial as the picture.
    // {0} = name, {1} = owner mention (sent with pings off), {2} = time lived, {3} = memorial.
    public static readonly string[] PlynlingDeathLines =
    {
        "🪦 **{0}**, le Plynling de {1}, s'est éteint après {2} de vie. Il repose désormais sous {3}.",
        "🪦 Un chapeau de moins sur cette terre… **{0}** ({1}) nous a quittés après {2}. On lui a dressé {3}.",
        "🪦 Minute de silence pour **{0}**, compagnon de {1} pendant {2}. Il dort sous {3}.",
        "🪦 **{0}** n'a pas survécu à la faim. {2} de vie, et maintenant {3}. {1}, il t'attendait…",
    };

    // Posted publicly when staff bring one back. {0} = name, {1} = owner mention.
    public static readonly string[] PlynlingResurrectLines =
    {
        "✨ **{0}** est revenu d'entre les morts ! {1}, c'est ta deuxième chance. Ne la gâche pas.",
        "✨ La terre tremble… **{0}** ressort du cimetière, un peu poussiéreux mais bien vivant. Bon retour, {1} !",
        "✨ Miracle ! **{0}** respire à nouveau. {1}, nourris-le vite, il a une faim de mort-vivant.",
    };

    // The single DM about six hours before death. {0} = name, {1} = a relative Discord
    // timestamp ("dans 6 heures") — so every line must read with "dans …" in that slot.
    public static readonly string[] PlynlingWarningLines =
    {
        "⚠️ **{0}** a terriblement faim… il mourra {1} si personne ne le nourrit. `/plynling feed`, vite !",
        "⚠️ Ton Plynling **{0}** va mourir de faim {1}. Il compte sur toi.",
        "⚠️ Psst… **{0}** est au bord de l'évanouissement. Il s'effondrera {1}. Ne l'abandonne pas (╥﹏╥)",
    };

    // DMs to an owner when staff act on their Plynling, so it never looks like a bug.
    public static readonly string[] PlynlingStaffFreezeDms =
    {
        "❄️ Le staff a gelé ton Plynling **{0}**. Rien ne bouge tant qu'il n'est pas dégelé — il ne risque rien.",
        "❄️ **{0}** a été mis au frais par le staff. Il t'attendra, bien au froid.",
        "❄️ Pause forcée pour **{0}** : le staff l'a gelé. Pas de faim, pas de soucis, juste une longue sieste.",
    };

    public static readonly string[] PlynlingStaffThawDms =
    {
        "🌱 Le staff a dégelé **{0}**. La faim reprend son cours : pense à le nourrir !",
        "🌱 **{0}** se réveille, dégelé par le staff. Il a déjà un petit creux.",
        "🌱 Fin de la sieste pour **{0}** : le staff l'a dégelé. Son estomac s'en souvient déjà.",
    };

    // {0} = old name, {1} = new name.
    public static readonly string[] PlynlingStaffRenameDms =
    {
        "✏️ Le staff a renommé ton Plynling **{0}** en **{1}**.",
        "✏️ Petit changement d'identité : **{0}** s'appelle désormais **{1}** (décision du staff).",
        "✏️ Ton Plynling répond maintenant au nom de **{1}** — le staff a jugé que **{0}** ne lui allait plus.",
    };
```

In the table of contents' `Plynlings` group, add:

```
//     PlynlingDeathLines · PlynlingResurrectLines ... public, game channel
//     PlynlingWarningLines ..... the ~6h DM before death
//     PlynlingStaffFreezeDms · PlynlingStaffThawDms · PlynlingStaffRenameDms
```

- [ ] **Step 4: Create `ProjectSYNCS/Services/PlynlingAnnouncer.cs`**

```csharp
using Discord;
using Discord.Net;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Services;

// Everything Plynlings say *outside* a command reply: public announcements in the game
// channel and DMs to owners. A singleton with no database access. Every Discord call is
// swallowed and logged — a missing permission or closed DMs must never break a flow.
public sealed class PlynlingAnnouncer
{
    // Where deaths and resurrections are announced. A server-specific id, listed in
    // CLAUDE.md's "Hardcoded ids" beside the others.
    public const ulong GameChannelId = 878305034432045080;

    private readonly DiscordSocketClient _client;
    private readonly ResponsePicker _picker;
    private readonly ILogger<PlynlingAnnouncer> _logger;

    public PlynlingAnnouncer(DiscordSocketClient client, ResponsePicker picker, ILogger<PlynlingAnnouncer> logger)
    {
        _client = client;
        _picker = picker;
        _logger = logger;
    }

    public Task AnnounceDeathAsync(Plynling plynling, DateTimeOffset now)
    {
        var lived = PlynlingLife.Age(plynling, now);
        var tier = PlynlingCatalog.MemorialTier(lived);
        var line = string.Format(_picker.Pick(GameChannelId, BotResponses.PlynlingDeathLines),
            PlynlingCardUi.SafeName(plynling.Name), $"<@{plynling.OwnerId}>",
            LevelCardUi.Duration((long)lived.TotalMinutes), PlynlingCatalog.MemorialName(tier));
        return PostAsync(line, PlynlingArt.Memorial(plynling.Species, tier), "death");
    }

    public Task AnnounceResurrectionAsync(Plynling plynling, DateTimeOffset now)
    {
        var line = string.Format(_picker.Pick(GameChannelId, BotResponses.PlynlingResurrectLines),
            PlynlingCardUi.SafeName(plynling.Name), $"<@{plynling.OwnerId}>");
        return PostAsync(line, PlynlingArt.Sprite(plynling.Species, PlynlingLife.Mood(plynling, now)), "resurrection");
    }

    public Task WarnOwnerAsync(Plynling plynling)
    {
        var death = PlynlingLife.DeathAt(plynling);
        if (death is null) return Task.CompletedTask;
        var line = string.Format(_picker.Pick(plynling.OwnerId, BotResponses.PlynlingWarningLines),
            PlynlingCardUi.SafeName(plynling.Name), $"<t:{death.Value.ToUnixTimeSeconds()}:R>");
        return DmOwnerAsync(plynling.OwnerId, line);
    }

    public async Task DmOwnerAsync(ulong ownerId, string line)
    {
        try
        {
            var user = await _client.GetUserAsync(ownerId);
            if (user is null) return;
            var dm = await user.CreateDMChannelAsync();
            await dm.SendMessageAsync(line, allowedMentions: AllowedMentions.None);
        }
        catch (HttpException ex) when (ex.DiscordCode == DiscordErrorCode.CannotSendMessageToUser)
        {
            // Closed DMs, or no longer sharing a server — the same case reminder DMs handle.
            _logger.LogInformation("User {UserId} does not accept DMs; Plynling notice skipped.", ownerId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to DM user {UserId} about their Plynling.", ownerId);
        }
    }

    private async Task PostAsync(string line, string imageUrl, string what)
    {
        try
        {
            if (_client.GetChannel(GameChannelId) is not IMessageChannel channel)
            {
                _logger.LogWarning("Game channel {ChannelId} not found; Plynling {What} not announced.", GameChannelId, what);
                return;
            }

            var components = new ComponentBuilderV2()
                .AddComponent(new ContainerBuilder()
                    .AddComponent(new SectionBuilder()
                        .WithAccessory(new ThumbnailBuilder().WithMedia(new UnfurledMediaItemProperties(imageUrl)))
                        .AddComponent(new TextDisplayBuilder(line))))
                .Build();
            await channel.SendMessageAsync(components: components, flags: MessageFlags.ComponentsV2,
                allowedMentions: AllowedMentions.None);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to post the Plynling {What} announcement.", what);
        }
    }
}
```

- [ ] **Step 5: Extend `ProjectSYNCS/Helpers/PlynlingText.cs`** — add inside the class:

```csharp
    public const string StaffOnly = "Cette action est réservée au staff.";
    public const string AlreadyFrozen = "Il est déjà gelé.";
    public const string NotFrozen = "Il n'est pas gelé.";
    public const string TooHungryToFreeze = "Trop tard pour le geler : il a déjà trop faim (moins de 50 %). Nourris-le d'abord.";
    public const string ThawStaffOnly = "C'est le staff qui l'a gelé : seul le staff peut le dégeler.";
    public const string NoGrave = "Personne à ressusciter : cette personne n'a aucun Plynling au cimetière.";
    public const string ResurrectBlocked = "Cette personne a déjà un Plynling vivant — un seul à la fois.";

    public static string FreezeCooldown(DateTimeOffset next) =>
        $"Tu l'as dégelé il y a moins de 7 jours. Prochain gel possible <t:{next.ToUnixTimeSeconds()}:R>.";

    // "…Notice", not "Frozen"/"Thawed": those names are already refusal constants.
    public static string FrozenNotice(string name, DateTimeOffset? until) => until is { } u
        ? $"❄️ **{name}** est gelé jusqu'au <t:{u.ToUnixTimeSeconds()}:f>. Rien ne bouge d'ici là."
        : $"❄️ **{name}** est gelé jusqu'à nouvel ordre du staff.";

    public static string ThawedNotice(string name) => $"🌱 **{name}** est dégelé. La faim reprend son cours !";
```

- [ ] **Step 6: Add the four subcommands to `ProjectSYNCS/Commands/PlynlingModule.cs`**

Add a `PlynlingAnnouncer _announcer` field, take it in the constructor (`PlynlingModule(PlynlingService plynlings, PlynlingCareService care, ResponsePicker picker, PlynlingAnnouncer announcer)`), and add:

```csharp
    // Without `user`, the owner freezes their own under the self-freeze rules. With a
    // different `user`, it is a staff freeze: no rules, lifted by staff only, and the
    // owner is told by DM so it never looks like a bug.
    [SlashCommand("freeze", "Geler un Plynling : plus rien ne bouge (vacances)")]
    public async Task FreezeAsync(
        [Summary("user", "Staff : le Plynling de quelqu'un d'autre")] IUser? user = null)
    {
        var target = user ?? Context.User;
        var byStaff = target.Id != Context.User.Id;
        if (byStaff && !SessionPermissions.IsStaff(Context.User))
        {
            await RespondAsync(PlynlingText.StaffOnly, ephemeral: true);
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var (outcome, plynling) = await _plynlings.FreezeAsync(Context.Guild.Id, target.Id, byStaff, now);
        if (outcome != FreezeOutcome.Frozen || plynling is null)
        {
            await RespondAsync(outcome switch
            {
                FreezeOutcome.NoPlynling => byStaff ? PlynlingText.NoneFor(target.Id) : PlynlingText.NoPlynling,
                FreezeOutcome.Dead => PlynlingText.Dead,
                FreezeOutcome.AlreadyFrozen => PlynlingText.AlreadyFrozen,
                FreezeOutcome.TooHungry => PlynlingText.TooHungryToFreeze,
                FreezeOutcome.Cooldown => PlynlingText.FreezeCooldown(plynling!.LastSelfThawAt!.Value + PlynlingLife.SelfFreezeCooldown),
                _ => PlynlingText.Unknown,
            }, ephemeral: true, allowedMentions: AllowedMentions.None);
            return;
        }

        await RespondCardAsync(plynling, now, PlynlingText.FrozenNotice(PlynlingCardUi.SafeName(plynling.Name), plynling.FreezeUntil));
        // After the reply, never before: nothing here defers, and a DM is two or three
        // REST calls against Discord's 3 s deadline for answering the interaction.
        if (byStaff)
            await _announcer.DmOwnerAsync(plynling.OwnerId, string.Format(
                _picker.Pick(plynling.OwnerId, BotResponses.PlynlingStaffFreezeDms), PlynlingCardUi.SafeName(plynling.Name)));
    }

    [SlashCommand("thaw", "Dégeler un Plynling")]
    public async Task ThawAsync(
        [Summary("user", "Staff : le Plynling de quelqu'un d'autre")] IUser? user = null)
    {
        var target = user ?? Context.User;
        var asStaff = SessionPermissions.IsStaff(Context.User);
        if (target.Id != Context.User.Id && !asStaff)
        {
            await RespondAsync(PlynlingText.StaffOnly, ephemeral: true);
            return;
        }

        var now = DateTimeOffset.UtcNow;
        // Staff may lift any freeze, including a staff freeze on their own Plynling.
        var (outcome, plynling) = await _plynlings.ThawAsync(Context.Guild.Id, target.Id, asStaff, now);
        if (outcome != ThawOutcome.Thawed || plynling is null)
        {
            await RespondAsync(outcome switch
            {
                ThawOutcome.NoPlynling => target.Id == Context.User.Id ? PlynlingText.NoPlynling : PlynlingText.NoneFor(target.Id),
                ThawOutcome.Dead => PlynlingText.Dead,
                ThawOutcome.NotFrozen => PlynlingText.NotFrozen,
                ThawOutcome.StaffOnly => PlynlingText.ThawStaffOnly,
                _ => PlynlingText.Unknown,
            }, ephemeral: true, allowedMentions: AllowedMentions.None);
            return;
        }

        await RespondCardAsync(plynling, now, PlynlingText.ThawedNotice(PlynlingCardUi.SafeName(plynling.Name)));
        if (target.Id != Context.User.Id)   // after the reply — see FreezeAsync
            await _announcer.DmOwnerAsync(plynling.OwnerId, string.Format(
                _picker.Pick(plynling.OwnerId, BotResponses.PlynlingStaffThawDms), PlynlingCardUi.SafeName(plynling.Name)));
    }

    // Staff only: the name is shown publicly (card, announcements, graveyard), so fixing
    // an offensive one is moderation. Reaches their latest grave too.
    [SlashCommand("rename", "Staff : renommer le Plynling de quelqu'un")]
    public async Task RenameAsync(
        [Summary("user", "À qui est le Plynling")] IUser user,
        [Summary("name", "Son nouveau nom")] [MaxLength(InputCaps.PlynlingName)] string name)
    {
        if (!SessionPermissions.IsStaff(Context.User))
        {
            await RespondAsync(PlynlingText.StaffOnly, ephemeral: true);
            return;
        }
        name = name.Trim();
        if (name.Length == 0)
        {
            await RespondAsync(PlynlingText.EmptyName, ephemeral: true);
            return;
        }

        var (plynling, oldName) = await _plynlings.RenameAsync(Context.Guild.Id, user.Id, name, DateTimeOffset.UtcNow);
        if (plynling is null)
        {
            await RespondAsync(PlynlingText.NoneFor(user.Id), ephemeral: true, allowedMentions: AllowedMentions.None);
            return;
        }

        await RespondAsync($"✏️ **{PlynlingCardUi.SafeName(oldName)}** s'appelle désormais **{PlynlingCardUi.SafeName(plynling.Name)}**.",
            ephemeral: true, allowedMentions: AllowedMentions.None);
        if (user.Id != Context.User.Id)   // after the reply — see FreezeAsync
            await _announcer.DmOwnerAsync(plynling.OwnerId, string.Format(
                _picker.Pick(plynling.OwnerId, BotResponses.PlynlingStaffRenameDms),
                PlynlingCardUi.SafeName(oldName), PlynlingCardUi.SafeName(plynling.Name)));
    }

    // Staff only in v1 (a rare self-service item comes later, through the same
    // PlynlingService.ResurrectAsync). The comeback is announced publicly, like the death.
    [SlashCommand("resurrect", "Staff : ressusciter le dernier Plynling de quelqu'un")]
    public async Task ResurrectAsync([Summary("user", "À qui est le Plynling")] IUser user)
    {
        if (!SessionPermissions.IsStaff(Context.User))
        {
            await RespondAsync(PlynlingText.StaffOnly, ephemeral: true);
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var (outcome, plynling) = await _plynlings.ResurrectAsync(Context.Guild.Id, user.Id, now);
        if (outcome != ResurrectOutcome.Resurrected || plynling is null)
        {
            await RespondAsync(outcome == ResurrectOutcome.NoGrave ? PlynlingText.NoGrave : PlynlingText.ResurrectBlocked,
                ephemeral: true);
            return;
        }

        await RespondAsync($"✨ **{PlynlingCardUi.SafeName(plynling.Name)}** est de retour (annoncé dans <#{PlynlingAnnouncer.GameChannelId}>).",
            ephemeral: true, allowedMentions: AllowedMentions.None);
        await _announcer.AnnounceResurrectionAsync(plynling, now);   // after the reply — see FreezeAsync
    }
```

- [ ] **Step 7: Register in `ProjectSYNCS/Program.cs`**

```csharp
        services.AddSingleton<PlynlingAnnouncer>();
```

- [ ] **Step 8: Verify**

Run: `cd ProjectSYNCS && dotnet build -warnaserror` → clean.
Run: `dotnet run --project "$SCRATCH/plynlingui"` → `N passed, 0 failed`.
Run the Task 2, 4 and 5 harnesses → still `0 failed`.

- [ ] **Step 9: CLAUDE.md** — in the "**There are three separate authorization models.**" paragraph, replace "which is what `/addxp` and `/removexp` need since nobody owns someone else's XP." with "which is what `/addxp` and `/removexp` need since nobody owns someone else's XP, and what `/plynling freeze|thaw user:`, `/plynling rename` and `/plynling resurrect` check." Then add to `## Hardcoded ids`:

```markdown
`PlynlingAnnouncer.GameChannelId` (`878305034432045080`) is where Plynling deaths and
resurrections are announced. Commands themselves work in any channel.
```

And add:

```markdown
**Freezing has two owners.** A self-freeze (`FrozenByStaff = false`) follows the rules —
hunger ≥ 50%, 14 days at most, thawable early, 7-day cooldown after it ends — and those
rules exist only to stop people escaping death. A staff freeze has none of them and is
lifted by staff only; the owner is told by DM whenever staff freeze, thaw or rename theirs.
`LastSelfThawAt` is written only when a *self*-freeze ends, so a staff thaw never starts
the owner's cooldown.
```

- [ ] **Step 10: Checkpoint** — stop and hand off to the user for review and commit.

---

### Task 8: The hourly sweep

**Files:**
- Create: `ProjectSYNCS/Services/PlynlingSweepService.cs`
- Modify: `ProjectSYNCS/Program.cs`
- Modify: `CLAUDE.md`

**Interfaces:**
- Consumes: `PlynlingService.GetSweepBatchAsync/SaveAsync` (Task 4), `PlynlingLife.Settle/ShouldWarn` (Task 2), `PlynlingAnnouncer.AnnounceDeathAsync/WarnOwnerAsync` (Task 7).
- Produces: nothing consumed later.

This task has no pure logic of its own — every decision it makes is `Settle` and `ShouldWarn`, already covered by the Task 2 harness. Its verification is the build plus the per-item guard review in Step 3.

- [ ] **Step 1: Create `ProjectSYNCS/Services/PlynlingSweepService.cs`**

```csharp
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Services;

// Once an hour: settles every living Plynling (deaths land at the instant they happened,
// self-freezes thaw on schedule), announces deaths nobody has announced yet, and sends
// the single warning DM about six hours before a death.
//
// Its own interval, like every sweep here — an hour is precise enough for a 4-day clock,
// and the other loops' intervals are load-bearing for other things. It is a safety net,
// not the source of truth: every read already settles, so a command can find a death
// first; the sweep then announces it.
public sealed class PlynlingSweepService : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(1);

    private readonly DiscordSocketClient _client;
    private readonly IServiceProvider _services;
    private readonly PlynlingAnnouncer _announcer;
    private readonly ILogger<PlynlingSweepService> _logger;

    public PlynlingSweepService(
        DiscordSocketClient client,
        IServiceProvider services,
        PlynlingAnnouncer announcer,
        ILogger<PlynlingSweepService> logger)
    {
        _client = client;
        _services = services;
        _announcer = announcer;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(CheckInterval, stoppingToken);

            // Announcing into a stale gateway cache would fail every send.
            if (_client.ConnectionState != ConnectionState.Connected) continue;

            await SweepAsync();
        }
    }

    private async Task SweepAsync()
    {
        await using var scope = _services.CreateAsyncScope();
        var plynlings = scope.ServiceProvider.GetRequiredService<PlynlingService>();

        List<Plynling> batch;
        try
        {
            batch = await plynlings.GetSweepBatchAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load Plynlings for the sweep.");
            return;
        }

        var now = DateTimeOffset.UtcNow;
        foreach (var plynling in batch)
        {
            // Per item: one broken Plynling must not stop the others, and an exception
            // escaping ExecuteAsync would stop the whole host.
            try
            {
                PlynlingLife.Settle(plynling, now);

                if (plynling.DiedAt is not null && !plynling.DeathAnnounced)
                {
                    // Marked and saved *before* posting: at most one attempt. A failed post
                    // is logged, never retried every hour into the game channel.
                    plynling.DeathAnnounced = true;
                    await plynlings.SaveAsync();
                    await _announcer.AnnounceDeathAsync(plynling, now);
                }
                else if (PlynlingLife.ShouldWarn(plynling, now))
                {
                    plynling.WarningSent = true;
                    await plynlings.SaveAsync();
                    await _announcer.WarnOwnerAsync(plynling);
                }
                else
                {
                    await plynlings.SaveAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sweep Plynling {PlynlingId}.", plynling.Id);
            }
        }
    }
}
```

- [ ] **Step 2: Register it in `ProjectSYNCS/Program.cs`**, after the other hosted services:

```csharp
        services.AddHostedService<PlynlingSweepService>();
```

- [ ] **Step 3: Verify**

Run: `cd ProjectSYNCS && dotnet build -warnaserror` → clean.
Review: every `await` inside the `foreach` is inside the `try` (grep the file: the only `await` outside it is `GetSweepBatchAsync`, itself in its own `try`, and `Task.Delay`).
Run all harnesses → still `0 failed`.

- [ ] **Step 4: CLAUDE.md** — in `## Architecture`, replace "`VoiceXpService` and `GiveawayDrawService`. The last three each run" with "`VoiceXpService`, `GiveawayDrawService` and `PlynlingSweepService`. The last four each run". In the "**An exception escaping a hosted loop stops the whole bot**" note, replace "None of the five `BackgroundService`s" with "None of the six `BackgroundService`s" and "`GiveawayDrawService`'s per-giveaway draw" with "`GiveawayDrawService`'s per-giveaway draw, `PlynlingSweepService`'s per-Plynling pass". Then add:

```markdown
**`PlynlingSweepService` is the sixth hosted loop, hourly, and a safety net.** Every read in
`PlynlingService` already settles, so a command can discover a death before the sweep does;
the sweep's jobs are the things nobody else triggers — announcing deaths
(`DeathAnnounced`), the single warning DM (`WarningSent`, re-armed by feeding), and
thawing expired self-freezes on schedule. It saves the flag **before** the side effect, so
a failed announcement is logged once rather than retried into the game channel every hour.
```

- [ ] **Step 5: Checkpoint** — stop and hand off to the user for review and commit.

---

### Task 9: The graveyard

**Files:**
- Modify: `ProjectSYNCS/Helpers/PlynlingCardUi.cs` (graveyard text)
- Create: `ProjectSYNCS/Commands/GraveyardModule.cs`
- Test: `$SCRATCH/graveyardcheck/Program.cs`

**Interfaces:**
- Consumes: `PlynlingService.GetGraveyardAsync` (Task 4), `PlynlingLife.Age`, `PlynlingCatalog.MemorialTier/MemorialName`, `PlynlingArt.Memorial`.
- Produces: `enum GraveSort { Recent, Longest }`, `GraveyardModule.PageSize` (5), `GraveyardModule.Order(IEnumerable<Plynling>, GraveSort, DateTimeOffset)`, `GraveyardModule.BuildPage(IReadOnlyList<Plynling>, GraveSort, ulong owner, int page, DateTimeOffset now)`, custom ids `grave:page:{sort}:{owner}:{page}` and `grave:sort:{sort}:{owner}:0`.

- [ ] **Step 1: Write the failing harness**

Create `$SCRATCH/graveyardcheck/graveyardcheck.csproj` (harness csproj) and `$SCRATCH/graveyardcheck/Program.cs`:

```csharp
using System.Collections;
using Discord;
using ProjectSYNCS.Commands;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

int pass = 0, fail = 0;
void Check(string label, bool ok) { if (ok) pass++; else { fail++; Console.WriteLine("  FAIL  " + label); } }
static void Walk(object c, List<object> all)
{
    all.Add(c);
    foreach (var name in new[] { "Components", "Accessory" })
    {
        var value = c.GetType().GetProperty(name)?.GetValue(c);
        if (value is IEnumerable list and not string) { foreach (var x in list) if (x is not null) Walk(x, all); }
        else if (value is not null) Walk(value, all);
    }
}
List<string> Ids(MessageComponent m, out int count)
{
    var all = new List<object>();
    foreach (var top in m.Components) Walk(top, all);
    count = all.Count;
    return all.Select(x => x.GetType().GetProperty("CustomId")?.GetValue(x) as string).Where(s => s is not null).Select(s => s!).ToList();
}

var t0 = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
var now = t0 + TimeSpan.FromDays(400);
// 12 graves: lifetimes rise with i, deaths fall with i — so the two sorts disagree.
var graves = Enumerable.Range(0, 12).Select(i =>
{
    var p = PlynlingLife.Create(1, (ulong)(100 + i), $"P{i}", (PlynlingSpecies)(i % 6), t0 + TimeSpan.FromDays(100 - i * 5));
    p.Id = i + 1;
    p.Hunger = 1;                                             // each lives exactly 4 days…
    p.AgeBankedSeconds = (long)TimeSpan.FromDays(i * 20).TotalSeconds;   // …plus i*20 banked days
    PlynlingLife.Settle(p, now);
    return p;
}).ToList();

var recent = GraveyardModule.Order(graves, GraveSort.Recent, now);
Check("recent: newest death first", recent.Zip(recent.Skip(1)).All(x => x.First.DiedAt >= x.Second.DiedAt));
var longest = GraveyardModule.Order(graves, GraveSort.Longest, now);
Check("longest: longest life first", longest.Zip(longest.Skip(1)).All(x => PlynlingLife.Age(x.First, now) >= PlynlingLife.Age(x.Second, now)));
Check("the two sorts differ", recent.First().Id != longest.First().Id);

foreach (var sort in new[] { GraveSort.Recent, GraveSort.Longest })
    for (var page = 0; page < 3; page++)
    {
        var built = GraveyardModule.BuildPage(graves, sort, 0, page, now);
        var ids = Ids(built, out var count);
        Check($"{sort} page {page}: fits ({count})", count <= 40);
        Check($"{sort} page {page}: ids unique", ids.Count == ids.Distinct().Count());
        Check($"{sort} page {page}: has a sort row and a page row",
            ids.Count(i => i.StartsWith("grave:sort:")) == 2 && ids.Count(i => i.StartsWith("grave:page:")) == 2);
    }
var empty = GraveyardModule.BuildPage(new List<Plynling>(), GraveSort.Recent, 0, 0, now);
Check("an empty graveyard still renders", Ids(empty, out _).Count == 4);
var filtered = GraveyardModule.BuildPage(graves.Take(2).ToList(), GraveSort.Recent, 101, 0, now);
Check("owner filter carried in the ids", Ids(filtered, out _).All(i => i.Contains(":101:")));

Console.WriteLine($"{pass} passed, {fail} failed");
if (fail > 0) Environment.Exit(1);
```

- [ ] **Step 2: Run it — expect a compile failure** (`GraveyardModule`, `GraveSort` do not exist).

- [ ] **Step 3: Add graveyard text to `ProjectSYNCS/Helpers/PlynlingCardUi.cs`**

Add these three methods **inside** `PlynlingCardUi`, after `FoodEffect`:

```csharp
    public static string GraveyardTitle(ulong owner, GraveSortLabel sort) =>
        (owner == 0 ? "## 🪦 Cimetière des Plynlings" : $"## 🪦 Les tombes de <@{owner}>") +
        $"\n-# Tri : {sort.Text}";

    public static string EmptyGraveyard(ulong owner) => owner == 0
        ? "Le cimetière est vide. Pour l'instant."
        : $"<@{owner}> n'a encore perdu aucun Plynling. Bravo… pour l'instant.";

    public static string GraveLine(Plynling p, DateTimeOffset now)
    {
        var info = PlynlingCatalog.Info(p.Species);
        var lived = LevelCardUi.Duration((long)PlynlingLife.Age(p, now).TotalMinutes);
        return $"**{SafeName(p.Name)}** · {info.Name} — à <@{p.OwnerId}>\n" +
               $"*a vécu {lived}* · mort <t:{p.DiedAt!.Value.ToUnixTimeSeconds()}:R>";
    }
```

Then add this type at the end of the same file, **after** the class's closing brace:

```csharp
// A graveyard sort's French wording: the title's "Tri : …" text and the button label.
public readonly record struct GraveSortLabel(string Text, string Button);
```

- [ ] **Step 4: Create `ProjectSYNCS/Commands/GraveyardModule.cs`**

```csharp
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Commands;

public enum GraveSort { Recent, Longest }

// /graveyard — every dead Plynling in the server (or one person's), five per page, each
// with its memorial as the picture. Components V2, like /leaderboard, and for the same
// budget: a row with a picture costs three components; 5 rows + two button rows = 24/40.
//
// Two button rows, two verbs: `grave:sort:` and `grave:page:`. Both encode the same state,
// so sharing a verb would collide by construction (COMPONENT_CUSTOM_ID_DUPLICATED).
[CommandContextType(InteractionContextType.Guild)]
public class GraveyardModule : InteractionModuleBase<SocketInteractionContext>
{
    public const int PageSize = 5;

    private readonly PlynlingService _plynlings;

    public GraveyardModule(PlynlingService plynlings)
    {
        _plynlings = plynlings;
    }

    [SlashCommand("graveyard", "Le cimetière des Plynlings")]
    public async Task GraveyardAsync(
        [Summary("user", "Seulement les tombes de cette personne")] IUser? user = null)
    {
        var now = DateTimeOffset.UtcNow;
        var graves = await _plynlings.GetGraveyardAsync(Context.Guild.Id, user?.Id, now);
        await RespondAsync(components: BuildPage(graves, GraveSort.Recent, user?.Id ?? 0, 0, now),
            flags: MessageFlags.ComponentsV2, allowedMentions: AllowedMentions.None);
    }

    [ComponentInteraction("grave:page:*:*:*", ignoreGroupNames: true)]
    public Task OnPageAsync(string sort, string owner, string page) => ShowAsync(sort, owner, page);

    [ComponentInteraction("grave:sort:*:*:*", ignoreGroupNames: true)]
    public Task OnSortAsync(string sort, string owner, string page) => ShowAsync(sort, owner, page);

    private async Task ShowAsync(string sortStr, string ownerStr, string pageStr)
    {
        if (!Enum.TryParse<GraveSort>(sortStr, out var sort)) sort = GraveSort.Recent;
        ulong.TryParse(ownerStr, out var owner);
        int.TryParse(pageStr, out var page);

        var now = DateTimeOffset.UtcNow;
        var graves = await _plynlings.GetGraveyardAsync(Context.Guild.Id, owner == 0 ? null : owner, now);
        var components = BuildPage(graves, sort, owner, page, now);

        await ((SocketMessageComponent)Context.Interaction).UpdateAsync(m =>
        {
            m.Components = components;
            m.Flags = MessageFlags.ComponentsV2;
            m.AllowedMentions = AllowedMentions.None;
        });
    }

    // Ties break on id so the order is stable across re-renders.
    public static List<Plynling> Order(IEnumerable<Plynling> graves, GraveSort sort, DateTimeOffset now) =>
        sort == GraveSort.Longest
            ? graves.OrderByDescending(p => PlynlingLife.Age(p, now)).ThenBy(p => p.Id).ToList()
            : graves.OrderByDescending(p => p.DiedAt).ThenBy(p => p.Id).ToList();

    public static GraveSortLabel Label(GraveSort sort) => sort == GraveSort.Longest
        ? new GraveSortLabel("plus longues vies", "⏳ Plus longue vie")
        : new GraveSortLabel("plus récents", "🕯️ Plus récents");

    public static MessageComponent BuildPage(
        IReadOnlyList<Plynling> graves, GraveSort sort, ulong owner, int page, DateTimeOffset now)
    {
        var ordered = Order(graves, sort, now);
        var totalPages = Math.Max(1, (int)Math.Ceiling(ordered.Count / (double)PageSize));
        page = Math.Clamp(page, 0, totalPages - 1);

        var container = new ContainerBuilder()
            .WithAccentColor(new Color(0x5A5E78))
            .AddComponent(new TextDisplayBuilder(PlynlingCardUi.GraveyardTitle(owner, Label(sort))));

        if (ordered.Count == 0)
            container.AddComponent(new TextDisplayBuilder(PlynlingCardUi.EmptyGraveyard(owner)));

        foreach (var plynling in ordered.Skip(page * PageSize).Take(PageSize))
        {
            var tier = PlynlingCatalog.MemorialTier(PlynlingLife.Age(plynling, now));
            container.AddComponent(new SectionBuilder()
                .WithAccessory(new ThumbnailBuilder()
                    .WithMedia(new UnfurledMediaItemProperties(PlynlingArt.Memorial(plynling.Species, tier)))
                    .WithDescription(PlynlingCatalog.MemorialName(tier)))
                .AddComponent(new TextDisplayBuilder(PlynlingCardUi.GraveLine(plynling, now))));
        }

        container.AddComponent(new TextDisplayBuilder($"-# Page {page + 1}/{totalPages} · {ordered.Count} tombe(s)"));

        var sortRow = new ActionRowBuilder();
        foreach (var candidate in new[] { GraveSort.Recent, GraveSort.Longest })
            sortRow.WithButton(Label(candidate).Button, $"grave:sort:{candidate}:{owner}:0",
                candidate == sort ? ButtonStyle.Primary : ButtonStyle.Secondary, disabled: candidate == sort);

        var pageRow = new ActionRowBuilder()
            .WithButton("◀", $"grave:page:{sort}:{owner}:{page - 1}", ButtonStyle.Secondary, disabled: page == 0)
            .WithButton("▶", $"grave:page:{sort}:{owner}:{page + 1}", ButtonStyle.Secondary, disabled: page >= totalPages - 1);

        return new ComponentBuilderV2()
            .AddComponent(container)
            .AddComponent(sortRow)
            .AddComponent(pageRow)
            .Build();
    }
}
```

- [ ] **Step 5: Verify**

Run: `cd ProjectSYNCS && dotnet build -warnaserror` → clean.
Run: `dotnet run --project "$SCRATCH/graveyardcheck"` → `N passed, 0 failed`.
Run all earlier harnesses → still `0 failed`.

- [ ] **Step 6: CLAUDE.md** — add:

```markdown
**`/graveyard` is Components V2 with two button rows and two verbs** — `grave:sort:` for the
newest/longest-life toggle and `grave:page:` for paging. The budget is 24 of 40 (container,
heading, five picture rows at three components each, footer, two rows of two). Changing the
sort resets to page 0. The graveyard settles every living Plynling in the guild before
listing, so a death that happened since the last sweep is already in the ground. Ties
break on id so the order is stable across re-renders.
```

- [ ] **Step 7: Checkpoint** — stop and hand off to the user for review and commit.

---

### Task 10: `/plynling help`, the main `/help` line, README

**Files:**
- Modify: `ProjectSYNCS/Commands/PlynlingModule.cs` (`help` subcommand + `BuildHelpEmbed`)
- Modify: `ProjectSYNCS/Commands/HelpModule.cs` (one field; comment's field count 11 → 12)
- Modify: `README.md`
- Modify: `CLAUDE.md` (slash-module list, component-handler list, `/help` field count)
- Test: `$SCRATCH/helpcheck2/Program.cs`

**Interfaces:**
- Consumes: `AppInfo.Version`, everything user-facing from Tasks 2–9 (for the wording).
- Produces: `PlynlingModule.BuildHelpEmbed() → Embed`.

- [ ] **Step 1: Write the failing harness**

Create `$SCRATCH/helpcheck2/helpcheck2.csproj` (harness csproj) and `$SCRATCH/helpcheck2/Program.cs`:

```csharp
using Discord;
using ProjectSYNCS.Commands;

int pass = 0, fail = 0;
void Check(string label, bool ok) { if (ok) pass++; else { fail++; Console.WriteLine("  FAIL  " + label); } }
void Caps(string what, Embed e)
{
    foreach (var f in e.Fields) Check($"{what}: field '{f.Name}' {f.Value.Length}/1024", f.Value.Length <= 1024);
    Check($"{what}: total {e.Length}/6000", e.Length <= 6000);
    Check($"{what}: {e.Fields.Length}/25 fields", e.Fields.Length <= 25);
}

var main = HelpModule.BuildEmbed();
var plyn = PlynlingModule.BuildHelpEmbed();
Caps("/help", main);
Caps("/plynling help", plyn);

var mainText = string.Join("\n", main.Fields.Select(f => f.Value));
Check("/help points to /plynling help", mainText.Contains("/plynling help"));
foreach (var hidden in new[] { "/work", "/balance", "/graveyard", "/plynling adopt", "/plynling feed" })
    Check($"/help does not list {hidden}", !mainText.Contains(hidden));

var plynText = string.Join("\n", plyn.Fields.Select(f => f.Value));
foreach (var cmd in new[] { "/plynling adopt", "/plynling view", "/plynling feed", "/plynling pet", "/plynling freeze",
                            "/plynling thaw", "/work", "/balance", "/graveyard", "/plynling rename", "/plynling resurrect" })
    Check($"/plynling help covers {cmd}", plynText.Contains(cmd));

Console.WriteLine($"{pass} passed, {fail} failed");
if (fail > 0) Environment.Exit(1);
```

- [ ] **Step 2: Run it — expect a compile failure** (`BuildHelpEmbed` does not exist).

- [ ] **Step 3: Add to `ProjectSYNCS/Commands/PlynlingModule.cs`**

```csharp
    [SlashCommand("help", "Comment fonctionnent les Plynlings")]
    public Task HelpAsync() => RespondAsync(embed: BuildHelpEmbed(), ephemeral: true);

    /// <summary>
    /// The Plynling guide. Static and Context-free for the same reason as
    /// <see cref="HelpModule.BuildEmbed"/>: embed caps throw at *send* time, so the only way
    /// to know it fits is to build and measure it without a gateway.
    /// </summary>
    public static Embed BuildHelpEmbed() =>
        new EmbedBuilder()
            .WithTitle("🍄 Plynlings — mode d'emploi")
            .WithDescription("Un Plynling est un petit champignon qui vit avec toi. Nourris-le, caresse-le, " +
                             "et surtout… ne l'oublie pas.")
            .WithColor(new Color(0xCE323A))
            .AddField("Adopter & regarder",
                "**`/plynling adopt name:`** — Gratuit, un seul à la fois. L'espèce est tirée au sort : " +
                "commune, peu commune, rare… ou légendaire.\n" +
                "**`/plynling view [user]`** — Sa carte, avec les boutons **Caresser** et **Nourrir**.")
            .AddField("S'en occuper",
                "La **faim** se vide en **4 jours** : à 0 %, il meurt. Le **bonheur** se vide en **2 jours** " +
                "(il est juste triste).\n" +
                "**`/plynling feed food:`** — Champignon (15), Shiitake (30), Morille (40, bonheur), Truffe (80).\n" +
                "**`/plynling pet [user]`** — +25 % de bonheur, toutes les 4 h, sur n'importe quel Plynling.")
            .AddField("Gagner des cailloux",
                "**`/work`** — 40 à 60 cailloux, toutes les 4 h.\n" +
                "Parler, réagir et le vocal rapportent aussi quelques cailloux (45 au plus par jour).\n" +
                "**`/balance`** — Ton solde, visible par toi seul.")
            .AddField("Partir en vacances",
                "**`/plynling freeze`** — Gèle ton Plynling (14 jours au plus) : plus rien ne bouge. " +
                "Seulement s'il a encore au moins 50 % de faim.\n" +
                "**`/plynling thaw`** — Le dégèle. Ensuite, 7 jours avant de pouvoir le regeler.")
            .AddField("La mort",
                "Tu reçois un **message privé** environ 6 h avant qu'il meure de faim. S'il meurt, tout le " +
                "serveur l'apprend et il rejoint le cimetière.\n" +
                "**`/graveyard [user]`** — Les tombes, triées par date ou par longueur de vie. Plus il a vécu, " +
                "plus sa tombe est belle.")
            .AddField("Staff",
                "**`/plynling freeze user:`** · **`/plynling thaw user:`** — Sur n'importe quel Plynling.\n" +
                "**`/plynling rename user: name:`** · **`/plynling resurrect user:`**")
            .WithFooter($"Project S.Y.N.C.S. v{AppInfo.Version}")
            .Build();
```

- [ ] **Step 4: Add the single line to `ProjectSYNCS/Commands/HelpModule.cs`**

After the `"Commandes — Mur de la honte"` field, add:

```csharp
            .AddField("Commandes — Plynlings",
                "**`/plynling help`** — Adopte un petit champignon, nourris-le, garde-le en vie. " +
                "Tout est expliqué là-dedans.")
```

and change the class comment's "11 are used" to "12 are used".

- [ ] **Step 5: README.md**

In the table of contents, replace "· [Wall of shame](#wall-of-shame)" with "· [Wall of shame](#wall-of-shame) · [Plynlings](#plynlings)". Add these rows to the `## Commands` table, just above the `/help` row:

```markdown
| `/plynling adopt · view · feed · pet · freeze · thaw · help` | Adopt and look after a Plynling | everyone |
| `/work · /balance` | Earn cailloux; see your balance | everyone |
| `/graveyard [user]` | Every Plynling that died | everyone |
| `/plynling rename · resurrect`, `freeze/thaw user:` | Manage someone's Plynling | staff |
```

Then add this section immediately after the `### Wall of shame` section:

```markdown
### Plynlings

A **Plynling** is a small mushroom creature each member can adopt — one at a time, free.
Its species is rolled: three common, one uncommon, a rare Mystique and a legendary Doré.

- **Hunger** empties in 4 days and **happiness** in 2. At 0% hunger it **dies** — really.
  `/plynling feed` (Champignon, Shiitake, Morille, Truffe) costs **cailloux**; `/plynling pet`
  is free, every 4 hours, and anyone can pet anyone's.
- **The card** (`/plynling view`) shows it in its current mood, a live countdown to starvation,
  and **Caresser** / **Nourrir** buttons.
- **Holidays:** `/plynling freeze` stops everything for up to 14 days, as long as it isn't
  already hungry; then a week before it can be frozen again.
- **Death** is announced to the whole server, after a private warning about 6 hours
  before. `/graveyard` lists every grave, newest or longest-lived first — and the longer a
  Plynling lived, the grander its memorial, from a cairn to a marble statue.
- **Cailloux** come from `/work` (every 4 hours) and, as a small bonus, from chatting,
  reacting and voice (45 a day at most). `/balance` is private.

`/plynling help` explains it all in Discord.
```

- [ ] **Step 6: CLAUDE.md** — in the slash-module paragraph, replace "`SpeakModule` (`/tell`, `/dm`) and `AbsenceModule`" with "`PlynlingModule` (`/plynling`, a group module), `EconomyModule` (`/work`, `/balance`), `GraveyardModule` (`/graveyard`), `SpeakModule` (`/tell`, `/dm`) and `AbsenceModule`"; replace "`PollComponentHandler`, `GiveawayComponentHandler`)" with "`PollComponentHandler`, `GiveawayComponentHandler`, `PlynlingComponentHandler`)"; replace "11 of the 25 allowed fields are used" with "12 of the 25 allowed fields are used". Then add:

```markdown
**`/plynling help` is the Plynlings' own guide, and the main `/help` points to it in one
line only** — the whole feature is one command away, and listing its dozen subcommands in
`/help` would push that embed toward its caps. Both are static `BuildEmbed` methods so
both are measurable without a gateway.
```

- [ ] **Step 7: Verify**

Run: `cd ProjectSYNCS && dotnet build -warnaserror` → clean.
Run: `dotnet run --project "$SCRATCH/helpcheck2"` → `N passed, 0 failed`.
Run every harness from Tasks 1–9 → all `0 failed`.

- [ ] **Step 8: Checkpoint** — stop and hand off to the user for review and commit.

---

## Final verification (with the user, in the dev guild)

These need a live gateway, so the user runs them. Before starting: **push `assets/plynlings/` to `main`**, or every picture will show as broken.

1. `/plynling adopt name:Test` → public card, a species, 70% / 70%, *content*, a live countdown, both controls.
2. Press **Caresser** → the card updates in place with her line; press again → private cooldown refusal; a second account can pet it.
3. `/work` → public line; `/work` again → private refusal with a countdown; `/balance` → private, with passive progress.
4. **Nourrir → Truffe** → card updates, money deducted; again → private "garde tes cailloux"; from a second account → private "ce n'est pas ton Plynling".
5. `/plynling freeze` → frozen card, no controls; `/plynling thaw` → back; `/plynling freeze` → private 7-day cooldown.
6. As staff: `/plynling freeze user:@alt` → owner receives a DM; `/plynling thaw user:@alt`; `/plynling rename user:@alt name:X` → DM.
7. Force a death (in a dev DB: set `NeedsAsOf` back 5 days on a row) → within the hour, a public announcement in the game channel with a cairn; `/graveyard` lists it; **Plus longue vie** re-sorts; ◀ ▶ page.
8. `/plynling resurrect user:@alt` → public comeback, 50% / 50%.
9. `/help` shows a single Plynling line; `/plynling help` renders.
10. A name like `@everyone **x**` renders harmlessly and pings nobody.
