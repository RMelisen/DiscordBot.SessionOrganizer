# Plynling personality — Phase 1 (foundation) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Give every Plynling CK3-style traits (1 at bébé, 2 more at ado, a 4th at adulte), five computed stats and a personality title, shown on its card and in an ephemeral « Personnalité » view — with the bébé stage lengthened to 7 days.

**Architecture:** The trait catalog, the draws, the stats and the title are pure helpers (`PlynlingTraits`, `PlynlingStats`, `PlynlingPersonality`) checked by a scratch harness. Draws are hashed from the Plynling id through a shared `StableRoll` (the SplitMix already used by sickness) and **stored** in a new `PlynlingTrait` table; one service method, `EnsureTraitsAsync`, draws "whatever its stage is owed and it lacks" — at adoption, at the mascot's creation, and on every sweep, which also backfills existing Plynlings without a data migration. Stats are never stored except their future growth (five int columns, 0 for now).

**Tech Stack:** C# / .NET 10, EF Core SQLite (migrations applied on startup), Discord.Net 3.20 (Components V2 card, ephemeral embed); a scratch console harness.

**Spec:** `docs/superpowers/specs/2026-10-06-plynling-events-design.md` — Phase 1 and the cross-cutting rules.

## Global Constraints

- **Never commit or push.** The owner commits. Each task ends by listing the files to commit.
- **Append-only stored values:** trait keys (strings), `TraitKind` (int), `JournalKind` (new value **at the end**). `PlynlingStat` and `AiAxis` are not stored but new values still go **at the end** (the RPG appends Force, Agilité, Endurance to `PlynlingStat`).
- **Hashed rolls, never `Random`,** for draws and base stats; the existing `PlynlingSickness.Roll` must return **bit-identical** values after it starts delegating to `StableRoll`.
- **Stages:** bébé < **7 days**, ado < 14 days, adulte < 180 days, ancien after.
- **Stats:** base **1d6** per stat (hashed from id + stat), **+2** from the passion, plus traits and growth; **floor 0, no upper limit**. Ping-Qilin: base **4** in all five, traits **charming, arrogant, paranoid, sadistic** (Adorable · Vaniteuse · Méfiante · Moqueuse).
- User-facing text French, code and comments English. Trait **descriptions are gender-neutral** (no « il » / « elle »); names have M and F forms. Text follows `docs/plynling-writing-style.md`.
- Names are hostile input: `PlynlingCardUi.SafeName`, `AllowedMentions.None`.
- Discord caps: 40 components per V2 message, 5 buttons per row, embed total 6000, field 1024, title 256. **`/plynling help` is at 5 894 / 6000** — measure after editing.
- Handlers in `Interactions/Components/` with `ignoreGroupNames: true`, **never** on `PlynlingModule`; a handler that reads the database **defers first**.
- Build with `dotnet build -warnaserror` (0 warnings). Git Bash mangles `python -c` / `dotnet` heredocs — write files with the editor.

`$REPO` = `C:\Users\c235773\Desktop\Sources - RME\Discord Bots\SessionOrganizer`; `$SCRATCH` = the session scratchpad; the harness is `$SCRATCH/personality/`.

## Review Focus

1. **An existing adult or elder with no traits** gets, on the first sweep, exactly 1 childhood + 3 personality traits from three different groups — never two of a pair. (Task 2 harness: draw from nothing at `Adult` and `Elder`.)
2. **A taught passion that stays free text** keeps the innate passion's +2; one that names a catalog passion moves the +2 to that passion's stat. (Task 3 harness.)
3. **Ping-Qilin is the same everywhere:** fixed traits and base 4 whatever her row id, title « Piquante et intrépide ». (Task 3 harness.)
4. **A hostile, maximum-length name** in the « Personnalité » embed (markdown characters, `InputCaps.PlynlingName` long) builds under every embed cap. (Task 5 harness.)
5. **The card in every state** — awake, asleep, frozen, dead — still builds (≤ 40 components, ≤ 5 buttons per row), and « Personnalité » is present exactly while alive. (Task 5 harness.)

---

### Task 1: `StableRoll`, the new stage thresholds, and the harness

**Files:**
- Create: `ProjectSYNCS/Helpers/StableRoll.cs`
- Modify: `ProjectSYNCS/Helpers/PlynlingSickness.cs` (`Roll` delegates)
- Modify: `ProjectSYNCS/Helpers/PlynlingLife.cs` (`StageStart`)
- Test: `$SCRATCH/personality/` (new console project)

**Interfaces:**
- Produces: `StableRoll.Unit(int a, int b, int c) -> double` in [0, 1); `PlynlingLife.StageStart(PlynlingStage.Teen) == TimeSpan.FromDays(7)`.

- [ ] **Step 1: Create the harness project**

`$SCRATCH/personality/personality.csproj`:

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

`$SCRATCH/personality/Program.cs`:

```csharp
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

var failures = new List<string>();
void Check(bool ok, string what) { if (!ok) failures.Add(what); }
var t0 = new DateTimeOffset(2026, 10, 6, 10, 0, 0, TimeSpan.Zero);
Plynling Fresh(int id, PlynlingGender g = PlynlingGender.Female)
{
    var p = PlynlingLife.Create(1, 1, "Lila", PlynlingSpecies.Cepe, g, t0);
    p.Id = id;
    return p;
}

// ---- StableRoll: sickness rolls unchanged ------------------------------------------------------
static double OldRoll(int plynlingId, int dayKey, RollPurpose purpose)
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
foreach (var id in new[] { 1, 7, 42, 1234, int.MaxValue })
    foreach (var day in new[] { 20261006, 20270101 })
        foreach (var purpose in Enum.GetValues<RollPurpose>())
            Check(PlynlingSickness.Roll(id, day, purpose) == OldRoll(id, day, purpose), $"sickness roll unchanged ({id}, {day}, {purpose})");
Check(Enumerable.Range(1, 1000).All(i => StableRoll.Unit(i, 3, 0) is >= 0 and < 1), "StableRoll stays in [0, 1)");

// ---- stages ------------------------------------------------------------------------------------
var baby = Fresh(1);
Check(PlynlingLife.Stage(baby, t0 + TimeSpan.FromDays(6.9)) == PlynlingStage.Baby, "bébé until 7 days");
Check(PlynlingLife.Stage(baby, t0 + TimeSpan.FromDays(7)) == PlynlingStage.Teen, "ado from 7 days");
Check(PlynlingLife.Stage(baby, t0 + TimeSpan.FromDays(14)) == PlynlingStage.Adult, "adulte from 14 days");
Check(PlynlingLife.Stage(baby, t0 + TimeSpan.FromDays(180)) == PlynlingStage.Elder, "ancien from 180 days");

Console.WriteLine(failures.Count == 0 ? "OK" : "FAIL\n" + string.Join("\n", failures));
return failures.Count == 0 ? 0 : 1;
```

- [ ] **Step 2: Run it to see it fail**

Run: `cd "$SCRATCH/personality" && dotnet run`
Expected: build error — `StableRoll` does not exist.

- [ ] **Step 3: Create `StableRoll`**

`ProjectSYNCS/Helpers/StableRoll.cs`:

```csharp
namespace ProjectSYNCS.Helpers;

/// <summary>
/// A number in [0, 1) hashed from three integers — SplitMix64, stable across runs and machines,
/// unlike <c>string.GetHashCode</c> or a seeded <see cref="Random"/>. Every Plynling roll that must
/// come out the same whoever computes it goes through here: sickness mornings (purpose + 1 as
/// <paramref name="c"/>), trait draws and base stats (<see cref="PlynlingTraits"/>,
/// <see cref="PlynlingStats"/>, with <paramref name="c"/> = 0 and a salt in <paramref name="b"/>).
/// </summary>
public static class StableRoll
{
    public static double Unit(int a, int b, int c)
    {
        ulong x = (ulong)(uint)a * 0x9E3779B97F4A7C15UL
                  ^ (ulong)(uint)b * 0xC2B2AE3D27D4EB4FUL
                  ^ (ulong)(uint)c * 0x165667B19E3779F9UL;
        x += 0x9E3779B97F4A7C15UL;
        x = (x ^ (x >> 30)) * 0xBF58476D1CE4E5B9UL;
        x = (x ^ (x >> 27)) * 0x94D049BB133111EBUL;
        x ^= x >> 31;
        return (x >> 11) * (1.0 / (1UL << 53));
    }
}
```

- [ ] **Step 4: Make `PlynlingSickness.Roll` delegate**

In `ProjectSYNCS/Helpers/PlynlingSickness.cs`, replace the whole `Roll` method (comment included) with:

```csharp
    // SplitMix64 over the three inputs (StableRoll): stable across runs and machines. The purpose
    // goes in as purpose + 1, exactly as before StableRoll existed — every stored morning depends on it.
    public static double Roll(int plynlingId, int dayKey, RollPurpose purpose) =>
        StableRoll.Unit(plynlingId, dayKey, (int)purpose + 1);
```

- [ ] **Step 5: Move the ado threshold**

In `ProjectSYNCS/Helpers/PlynlingLife.cs`, in `StageStart`, change

```csharp
        PlynlingStage.Teen => TimeSpan.FromDays(2),
```

to

```csharp
        PlynlingStage.Teen => TimeSpan.FromDays(7),
```

- [ ] **Step 6: Run the harness**

Run: `cd "$SCRATCH/personality" && dotnet run`
Expected: `OK`

- [ ] **Step 7: Build the bot**

Run: `cd "$REPO/ProjectSYNCS" && dotnet build -warnaserror`
Expected: `Build succeeded`, 0 warnings.

- [ ] **Step 8: Files for the owner to commit**

`ProjectSYNCS/Helpers/StableRoll.cs`, `ProjectSYNCS/Helpers/PlynlingSickness.cs`, `ProjectSYNCS/Helpers/PlynlingLife.cs` — suggested message: `Plynlings: StableRoll, ado from 7 days`.

---

### Task 2: The trait catalog and the draws

**Files:**
- Create: `ProjectSYNCS/Models/PlynlingTrait.cs` (entity + `TraitKind`; no DbSet yet)
- Create: `ProjectSYNCS/Helpers/PlynlingStats.cs` (only the `PlynlingStat` enum in this task; Task 3 fills the class)
- Create: `ProjectSYNCS/Helpers/PlynlingTraits.cs`
- Test: `$SCRATCH/personality/Program.cs`

**Interfaces:**
- Consumes: `StableRoll.Unit(int, int, int)`.
- Produces:
  - `enum TraitKind { Childhood, Personality, Coping }` (namespace `ProjectSYNCS.Models`, stored as int)
  - `class PlynlingTrait { int Id; int PlynlingId; string Key; TraitKind Kind; DateTimeOffset AcquiredAt; }`
  - `enum PlynlingStat { Diplomacy, Stewardship, Learning, Intrigue, Courage }` (namespace `ProjectSYNCS.Helpers`)
  - `enum AiAxis { Boldness, Compassion, Greed, Energy, Honor, Rationality, Sociability, Vengefulness, Zeal }`
  - `sealed record TraitInfo(string Key, TraitKind Kind, string Group, string NameM, string NameF, string Emoji, string Description, IReadOnlyDictionary<PlynlingStat, int> Stats, IReadOnlyDictionary<AiAxis, int> Axes, double StressGain = 1, double StressLoss = 1)` with `string Name(PlynlingGender)`
  - `PlynlingTraits.All : IReadOnlyList<TraitInfo>`, `PlynlingTraits.ByKey(string) -> TraitInfo?`
  - `PlynlingTraits.Draw(int plynlingId, PlynlingStage stage, IReadOnlyCollection<string> held) -> IReadOnlyList<TraitInfo>` — the traits to add, in order.

- [ ] **Step 1: Add the failing checks**

In `$SCRATCH/personality/Program.cs`, insert before the final `Console.WriteLine`:

```csharp
// ---- trait catalog ------------------------------------------------------------------------------
var all = PlynlingTraits.All;
Check(all.Count(t => t.Kind == TraitKind.Childhood) == 5, "5 childhood traits");
Check(all.Count(t => t.Kind == TraitKind.Personality) == 36, "36 personality traits");
Check(all.Count(t => t.Kind == TraitKind.Coping) == 10, "10 coping traits");
Check(all.Select(t => t.Key).Distinct().Count() == all.Count, "trait keys are unique");
var groups = all.Where(t => t.Kind == TraitKind.Personality).GroupBy(t => t.Group).ToDictionary(g => g.Key, g => g.Count());
Check(groups.Count == 18 && groups["compassion"] == 3 && groups["eccentric"] == 1 && groups.Count(g => g.Value == 2) == 16,
    "16 pairs, the compassion trio and eccentric alone");
Check(PlynlingTraits.ByKey("brave")!.Stats[PlynlingStat.Courage] == 3, "Brave: Courage +3");
Check(PlynlingTraits.ByKey("humble")!.Stats[PlynlingStat.Diplomacy] == 1 && PlynlingTraits.ByKey("humble")!.Stats[PlynlingStat.Intrigue] == -1, "Modeste: Dip +1, Rus -1");
Check(PlynlingTraits.ByKey("arrogant")!.Stats[PlynlingStat.Courage] == 1 && PlynlingTraits.ByKey("arrogant")!.Stats[PlynlingStat.Diplomacy] == -1, "Vaniteux: Cou +1, Dip -1");
Check(PlynlingTraits.ByKey("lazy")!.Stats.Count == 5 && PlynlingTraits.ByKey("lazy")!.Stats.Values.All(v => v == -1), "Paresseux: -1 to all five");
Check(PlynlingTraits.ByKey("sadistic")!.Axes[AiAxis.Compassion] == -100, "Moqueur: softened compassion");
Check(PlynlingTraits.ByKey("paranoid")!.StressGain == 2 && PlynlingTraits.ByKey("eccentric")!.StressLoss == 1.5, "CK3 stress multipliers kept");
Check(PlynlingTraits.ByKey("sadistic")!.Name(PlynlingGender.Female) == "Moqueuse", "feminine names");
Check(all.All(t => t.Stats.Values.All(v => v != 0) && t.Axes.Values.All(v => v != 0)), "no zero entries stored");
var pronoun = new System.Text.RegularExpressions.Regex(@"\b(il|elle|ils|elles)\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
foreach (var t in all)
    Check(!pronoun.IsMatch(t.Description), $"description of {t.Key} is gender-neutral");

// ---- draws --------------------------------------------------------------------------------------
List<TraitInfo> DrawFrom(int id, PlynlingStage stage, params string[] held) => PlynlingTraits.Draw(id, stage, held).ToList();
bool DistinctGroups(IEnumerable<TraitInfo> ts) =>
    ts.Where(t => t.Kind == TraitKind.Personality).GroupBy(t => t.Group).All(g => g.Count() == 1);
var seen = new HashSet<string>();
for (var id = 1; id <= 2000; id++)
{
    var b = DrawFrom(id, PlynlingStage.Baby);
    Check(b.Count == 1 && b[0].Kind == TraitKind.Childhood, $"bébé {id}: one childhood trait");
    var teen = DrawFrom(id, PlynlingStage.Teen);
    Check(teen.Count == 3 && teen.Count(t => t.Kind == TraitKind.Personality) == 2 && DistinctGroups(teen), $"ado {id}: childhood + 2 personality, distinct groups");
    var adult = DrawFrom(id, PlynlingStage.Adult);
    Check(adult.Count == 4 && adult.Count(t => t.Kind == TraitKind.Personality) == 3 && DistinctGroups(adult), $"adulte {id}: childhood + 3 personality, distinct groups");
    Check(DrawFrom(id, PlynlingStage.Elder).Select(t => t.Key).SequenceEqual(adult.Select(t => t.Key)), $"ancien {id} owes what an adulte owes");
    Check(teen.Select(t => t.Key).SequenceEqual(adult.Take(3).Select(t => t.Key)), $"{id}: growing up only adds, never redraws");
    // Owed only what is missing: a teen holding its childhood trait and one personality trait gets one more.
    var more = DrawFrom(id, PlynlingStage.Teen, teen[0].Key, teen[1].Key);
    Check(more.Count == 1 && more[0].Key == teen[2].Key, $"{id}: only the missing trait is drawn");
    Check(DrawFrom(id, PlynlingStage.Adult, adult.Select(t => t.Key).ToArray()).Count == 0, $"{id}: nothing owed when complete");
    foreach (var t in adult) seen.Add(t.Key);
}
Check(all.Where(t => t.Kind != TraitKind.Coping).All(t => seen.Contains(t.Key)), "every childhood and personality trait can be drawn");
Check(all.Where(t => t.Kind == TraitKind.Coping).All(t => !seen.Contains(t.Key)), "coping traits are never drawn");
// A held personality trait's group is excluded from later draws.
var heldBrave = DrawFrom(5, PlynlingStage.Adult, "curious", "brave");
Check(heldBrave.All(t => t.Group != "bravery") && heldBrave.Count == 2, "a held trait's group is never drawn again");
```

- [ ] **Step 2: Run it to see it fail**

Run: `cd "$SCRATCH/personality" && dotnet run`
Expected: build errors — `PlynlingTraits`, `TraitKind`, `PlynlingStat`, `AiAxis`, `TraitInfo` do not exist.

- [ ] **Step 3: Create the entity**

`ProjectSYNCS/Models/PlynlingTrait.cs`:

```csharp
namespace ProjectSYNCS.Models;

// Stored as an int: **append-only**.
public enum TraitKind { Childhood, Personality, Coping }

// One trait a Plynling holds (Helpers/PlynlingTraits holds the catalog). One row per
// (Plynling, trait), enforced by a unique index. Drawn once and kept: a trait appended to the
// catalog must never change anyone's existing traits. Deleted with the Plynling (abandoned);
// kept when it dies.
public class PlynlingTrait
{
    public int Id { get; set; }
    public int PlynlingId { get; set; }

    // PlynlingTraits key — stable, never renamed.
    public string Key { get; set; } = string.Empty;
    public TraitKind Kind { get; set; }
    public DateTimeOffset AcquiredAt { get; set; }
}
```

- [ ] **Step 4: Create the stat enum**

`ProjectSYNCS/Helpers/PlynlingStats.cs`:

```csharp
namespace ProjectSYNCS.Helpers;

// The five stats. Never stored as values, but **append-only** all the same: the RPG layer appends
// Force, Agilité and Endurance at the end, and the growth columns are named after these.
public enum PlynlingStat { Diplomacy, Stewardship, Learning, Intrigue, Courage }
```

- [ ] **Step 5: Create the catalog and the draws**

`ProjectSYNCS/Helpers/PlynlingTraits.cs`:

```csharp
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

// CK3's AI personality. Each trait pushes these; the sum names the Plynling (PlynlingPersonality)
// and, from the event engine on, decides for it when its owner does not. Not stored.
public enum AiAxis { Boldness, Compassion, Greed, Energy, Honor, Rationality, Sociability, Vengefulness, Zeal }

/// <summary>
/// One trait. <see cref="Key"/> is stored and **never renamed**. <see cref="Group"/> is the
/// exclusion group: a Plynling never holds two personality traits of one group. The description is
/// shared by both genders, so it never agrees with the Plynling. Stress multipliers are CK3's, used
/// from the stress phase on.
/// </summary>
public sealed record TraitInfo(
    string Key, TraitKind Kind, string Group, string NameM, string NameF, string Emoji, string Description,
    IReadOnlyDictionary<PlynlingStat, int> Stats, IReadOnlyDictionary<AiAxis, int> Axes,
    double StressGain = 1, double StressLoss = 1)
{
    public string Name(PlynlingGender gender) => gender == PlynlingGender.Female ? NameF : NameM;
}

/// <summary>
/// The trait catalog, adapted from Crusader Kings III (values from the CK3 wiki's Traits page,
/// 2026-10-06; CK3's Martial and Prowess merge into Courage, taking the larger). Keys are CK3's
/// English names. Draws are hashed from the Plynling id (<see cref="StableRoll"/>), never a
/// <see cref="Random"/>, and the result is stored: appending a trait here changes only draws that
/// have not happened yet.
/// </summary>
public static class PlynlingTraits
{
    private const int ChildhoodSalt = 101;
    private const int PersonalitySalt = 110;    // + the slot: 110, 111, 112

    private static Dictionary<PlynlingStat, int> S(int dip = 0, int inte = 0, int sag = 0, int rus = 0, int cou = 0)
    {
        var d = new Dictionary<PlynlingStat, int>();
        void Put(PlynlingStat s, int v) { if (v != 0) d[s] = v; }
        Put(PlynlingStat.Diplomacy, dip); Put(PlynlingStat.Stewardship, inte); Put(PlynlingStat.Learning, sag);
        Put(PlynlingStat.Intrigue, rus); Put(PlynlingStat.Courage, cou);
        return d;
    }

    private static Dictionary<AiAxis, int> A(int bol = 0, int com = 0, int gre = 0, int ene = 0, int hon = 0,
        int rat = 0, int soc = 0, int ven = 0, int zea = 0)
    {
        var d = new Dictionary<AiAxis, int>();
        void Put(AiAxis a, int v) { if (v != 0) d[a] = v; }
        Put(AiAxis.Boldness, bol); Put(AiAxis.Compassion, com); Put(AiAxis.Greed, gre); Put(AiAxis.Energy, ene);
        Put(AiAxis.Honor, hon); Put(AiAxis.Rationality, rat); Put(AiAxis.Sociability, soc);
        Put(AiAxis.Vengefulness, ven); Put(AiAxis.Zeal, zea);
        return d;
    }

    private static TraitInfo C(string key, string m, string f, string emoji, string description,
        Dictionary<PlynlingStat, int> stats, Dictionary<AiAxis, int> axes) =>
        new(key, TraitKind.Childhood, "childhood", m, f, emoji, description, stats, axes);

    private static TraitInfo P(string group, string key, string m, string f, string emoji, string description,
        Dictionary<PlynlingStat, int> stats, Dictionary<AiAxis, int> axes, double gain = 1, double loss = 1) =>
        new(key, TraitKind.Personality, group, m, f, emoji, description, stats, axes, gain, loss);

    private static TraitInfo K(string key, string m, string f, string emoji, string description,
        Dictionary<PlynlingStat, int> stats, Dictionary<AiAxis, int> axes) =>
        new(key, TraitKind.Coping, "coping", m, f, emoji, description, stats, axes);

    // Order matters only for draws not yet made; append new traits at the end of their kind.
    public static readonly IReadOnlyList<TraitInfo> All = new[]
    {
        // ---- childhood: one at bébé, kept for life
        C("bossy", "Autoritaire", "Autoritaire", "📣", "Distribue les rôles avant même que le jeu ait commencé.",
            S(inte: 1, cou: 1), A(bol: 25, gre: 25, hon: 15, rat: 15, ven: 15)),
        C("charming", "Adorable", "Adorable", "🥺", "Obtient une deuxième part de tarte rien qu'en regardant la première.",
            S(dip: 1, rus: 1), A(gre: 25, soc: 25, com: 15, rat: 15, ven: 15, hon: -15)),
        C("curious", "Curieux", "Curieuse", "🔍", "Soulève chaque pierre du chemin pour voir qui habite dessous.",
            S(dip: 1, sag: 1), A(bol: 25, com: 25, ene: 15, hon: 15, soc: 15, ven: -15)),
        C("pensive", "Rêveur", "Rêveuse", "☁️", "Regarde passer les nuages et connaît le nom de la plupart.",
            S(inte: 1, sag: 1), A(rat: 25, ene: 15, hon: 15, gre: -15, bol: -15, soc: -25)),
        C("rowdy", "Turbulent", "Turbulente", "🌪️", "Arrive en courant, repart en courant, et renverse le pot de miel entre les deux.",
            S(rus: 1, cou: 1), A(bol: 25, ene: 25, soc: 15, ven: 15, com: -15, hon: -15, rat: -15)),

        // ---- personality: two at ado, the fourth trait at adulte
        P("bravery", "brave", "Courageux", "Courageuse", "🦁", "Va voir ce qui fait du bruit dans le noir, et revient le raconter.",
            S(cou: 3), A(bol: 200, ene: 20, soc: 20, rat: -20)),
        P("bravery", "craven", "Peureux", "Peureuse", "🫣", "Connaît toutes les cachettes du village. Par précaution.",
            S(rus: 2, cou: -3), A(rat: 10, ene: -20, soc: -20, bol: -200)),
        P("temper", "calm", "Calme", "Calme", "🍃", "Même le héron du vieux pont trouve ce calme un peu exagéré.",
            S(dip: 1, rus: 1), A(rat: 75, ene: -10, ven: -10, bol: -20), loss: 1.1),
        P("temper", "wrathful", "Colérique", "Colérique", "💢", "Tape du pied, souffle très fort, puis réclame un câlin.",
            S(dip: -1, rus: -1, cou: 3), A(bol: 35, ven: 20, ene: 10, com: -20, rat: -35)),
        P("romance", "chaste", "Pudique", "Pudique", "🙈", "Rougit quand on lui tient la patte, même pour traverser.",
            S(sag: 2), A(hon: 20, ene: 10, zea: 10, gre: -20, soc: -20)),
        P("romance", "lustful", "Fleur bleue", "Fleur bleue", "💘", "A déjà gravé un cœur sur trois arbres différents.",
            S(dip: 2), A(soc: 35, gre: 20, ene: 10, hon: -10, zea: -10)),
        P("ambition", "content", "Content", "Contente", "😌", "Une noisette et un rayon de soleil : la journée est réussie.",
            S(sag: 2, rus: -1), A(hon: 10, soc: -10, ven: -10, zea: -10, bol: -35, ene: -35, gre: -50), loss: 1.1),
        P("ambition", "ambitious", "Ambitieux", "Ambitieuse", "🏆", "Veut le plus gros gland, la plus haute branche et le titre qui va avec.",
            S(1, 1, 1, 1, 1), A(ene: 75, gre: 75, bol: 50, soc: 20, zea: 10, hon: -20)),
        P("work", "diligent", "Travailleur", "Travailleuse", "🧺", "Range ses cailloux par taille, puis par couleur, puis recommence.",
            S(dip: 2, inte: 3, sag: 3), A(ene: 75, bol: 35, rat: 20, ven: 10), loss: 0.5),
        P("work", "lazy", "Paresseux", "Paresseuse", "🛌", "Considère la sieste comme un métier à plein temps.",
            S(-1, -1, -1, -1, -1), A(gre: 10, com: -10, soc: -10, ven: -10, bol: -20, ene: -50)),
        P("constancy", "stubborn", "Têtu", "Têtue", "🪨", "Quand c'est non, même la tortue du café n'insiste plus.",
            S(inte: 3), A(hon: 35, ven: 35, rat: -10)),
        P("constancy", "fickle", "Lunatique", "Lunatique", "🌗", "Adore les myrtilles. Déteste les myrtilles. Ça dépend de l'heure.",
            S(dip: 2, inte: -2, rus: 1), A(bol: 20, hon: -20, rat: -20, ven: -20)),
        P("grudge", "forgiving", "Indulgent", "Indulgente", "🤲", "Pardonne avant même qu'on ait fini de s'excuser.",
            S(dip: 2, sag: 1, rus: -2), A(com: 35, hon: 20, rat: 10, ene: -10, ven: -200)),
        P("grudge", "vengeful", "Rancunier", "Rancunière", "📝", "Tient une liste. Personne ne sait qui est dessus.",
            S(dip: -2, rus: 2, cou: 2), A(ven: 200, ene: 10, hon: -10, rat: -10, com: -20)),
        P("greed", "generous", "Généreux", "Généreuse", "🎁", "Revient du marché avec moins de cailloux et plus d'amis.",
            S(dip: 3), A(com: 35, hon: 20, soc: 10, gre: -200)),
        P("greed", "greedy", "Radin", "Radine", "🪙", "Compte ses cailloux deux fois, et ceux des autres une fois.",
            S(dip: -2), A(gre: 200, hon: -10, com: -20)),
        P("sociability", "gregarious", "Sociable", "Sociable", "🗣️", "Connaît le prénom de chaque escargot du village.",
            S(dip: 2), A(soc: 200, com: 35, bol: 20)),
        P("sociability", "shy", "Timide", "Timide", "🫥", "Dit bonjour tout bas, pour ne déranger personne.",
            S(dip: -2, sag: 1), A(ven: -10, zea: -10, bol: -20, soc: -200)),
        P("honesty", "honest", "Franc", "Franche", "🫡", "Dit que la confiture est ratée, puis en reprend, par politesse.",
            S(dip: 2, rus: -4), A(hon: 50, soc: 20, bol: 10, com: 10)),
        P("honesty", "deceitful", "Menteur", "Menteuse", "🤥", "Jure que le gâteau était déjà comme ça avant son passage.",
            S(dip: -2, rus: 4), A(rat: 10, bol: -10, com: -10, hon: -50)),
        P("pride", "humble", "Modeste", "Modeste", "🌱", "Gagne le concours du moineau et s'excuse auprès des autres.",
            S(dip: 1, rus: -1), A(com: 20, hon: 20, ene: -10, gre: -50)),
        P("pride", "arrogant", "Vaniteux", "Vaniteuse", "🪞", "Se recoiffe dans chaque flaque du chemin.",
            S(dip: -1, cou: 1), A(bol: 35, gre: 20, soc: 20, ene: 10, com: -20, hon: -20, rat: -20)),
        P("justice", "just", "Juste", "Juste", "⚖️", "Coupe la tarte en parts égales. À la règle.",
            S(inte: 2, sag: 1, rus: -3), A(hon: 200, rat: 20, ven: 10, zea: 10)),
        P("justice", "arbitrary", "Capricieux", "Capricieuse", "🎲", "Change les règles du jeu à chaque tour, et gagne souvent.",
            S(inte: -2, sag: -1, rus: 3), A(bol: 10, com: -10, zea: -10, rat: -20, hon: -200), gain: 0.5),
        P("patience", "patient", "Patient", "Patiente", "⏳", "Attend que l'escargot finisse sa phrase.",
            S(sag: 2), A(rat: 35, ven: 10, ene: -10, bol: -20)),
        P("patience", "impatient", "Impatient", "Impatiente", "⏰", "Ouvre le four toutes les deux minutes pour voir si c'est prêt.",
            S(sag: -2), A(bol: 20, ene: 10, ven: -10, rat: -35)),
        P("appetite", "temperate", "Frugal", "Frugale", "🥣", "Une baie le matin, une baie le soir. Et ça suffit, à ce qu'on dit.",
            S(inte: 2), A(ene: 10, ven: -10, gre: -35)),
        P("appetite", "gluttonous", "Gourmand", "Gourmande", "🍯", "A goûté chaque pot de miel du village. Deux fois.",
            S(inte: -2), A(gre: 35, ene: -10), loss: 1.1),
        P("trust", "trusting", "Confiant", "Confiante", "🤝", "Prête son écharpe au premier venu. L'écharpe n'est jamais revenue.",
            S(dip: 2, rus: -2), A(hon: 35, soc: 35, com: 20, rat: -20, ven: -20)),
        P("trust", "paranoid", "Méfiant", "Méfiante", "👀", "Renifle chaque cadeau avant de dire merci.",
            S(dip: -1, rus: 3), A(ven: 20, com: -10, hon: -20, rat: -20, soc: -35), gain: 2),
        P("belief", "zealous", "Superstitieux", "Superstitieuse", "🍀", "Évite les fissures du chemin, au cas où les esprits de la forêt regarderaient.",
            S(cou: 2), A(zea: 200, ene: 20, rat: -20)),
        P("belief", "cynical", "Sceptique", "Sceptique", "🤨", "Les esprits de la forêt ? Pas vus, pas crus.",
            S(sag: 2, rus: 2), A(rat: 35, com: -10, ene: -20, zea: -200)),
        P("compassion", "compassionate", "Bienveillant", "Bienveillante", "💗", "Garde toujours une noisette en poche pour qui en aurait besoin.",
            S(dip: 2, rus: -2), A(com: 200, hon: 35, soc: 35, gre: -20)),
        P("compassion", "callous", "Froid", "Froide", "🧊", "Écoute les malheurs des autres en hochant la tête, puis passe à autre chose.",
            S(dip: -2, rus: 2), A(rat: 10, soc: -10, hon: -35, com: -200)),
        // Softened from CK3's Sadistic: it teases, nothing cruel.
        P("compassion", "sadistic", "Moqueur", "Moqueuse", "😏", "A un surnom pour tout le monde, et aucun n'est flatteur.",
            S(rus: 2, cou: 2), A(bol: 20, soc: 20, com: -100)),
        P("eccentric", "eccentric", "Excentrique", "Excentrique", "🎩", "Porte une feuille de chou en guise d'écharpe. Par conviction.",
            S(dip: -2, sag: 2), A(bol: 75, hon: -20, soc: -20, rat: -200), gain: 1.5, loss: 1.5),

        // ---- coping: from mental breaks only (stress phase), never drawn here
        K("comfort_eater", "Mange ses émotions", "Mange ses émotions", "🍪", "Quand ça ne va pas, la réponse est dans la boîte à biscuits.",
            S(inte: -1), A(gre: 5, ene: -5)),
        K("inappetetic", "Sans appétit", "Sans appétit", "🥄", "Tourne la cuillère dans le bol sans rien avaler. Ça passera.",
            S(dip: -1, cou: -3), A(gre: -5, ene: -10)),
        K("contrite", "Repentant", "Repentante", "🙏", "S'excuse pour des choses que personne n'avait remarquées.",
            S(rus: -2), A(com: 10, hon: 10, zea: 10, ven: -10)),
        K("improvident", "Imprévoyant", "Imprévoyante", "💸", "Donne ses cailloux au premier qui les regarde.",
            S(dip: 1), A(zea: 10, com: 10, gre: -10)),
        K("reclusive", "Reclus", "Recluse", "🐚", "A collé un mot sur sa porte : « Plus tard ».",
            S(dip: -2, inte: -1), A(bol: -10, ene: -10, soc: -35)),
        K("irritable", "Irritable", "Irritable", "🌩️", "Mieux vaut ne pas lui parler avant sa sieste. Ni après.",
            S(dip: -2, cou: 2), A(bol: 10, ene: 10, ven: 10, com: -10, rat: -20)),
        K("profligate", "Dépensier", "Dépensière", "🛍️", "Revient du marché les bras chargés, sans savoir de quoi.",
            S(), A(gre: 10, com: -10)),
        K("confider", "Confident", "Confidente", "🫂", "Va mieux après avoir tout raconté à quelqu'un. Vraiment tout.",
            S(dip: 1), A(soc: 20, com: 10)),
        K("journaller", "Écrit son journal", "Écrit son journal", "📔", "Note tout dans un petit carnet, même la météo de ses humeurs.",
            S(sag: 1), A(rat: 10)),
        K("athletic", "Sportif", "Sportive", "🏃", "Fait trois fois le tour du village en courant quand quelque chose ne va pas.",
            S(cou: 1), A(ene: 25, bol: 5)),
    };

    private static readonly Dictionary<string, TraitInfo> ByKeyMap = All.ToDictionary(t => t.Key);
    private static readonly TraitInfo[] Childhood = All.Where(t => t.Kind == TraitKind.Childhood).ToArray();
    private static readonly TraitInfo[] Personality = All.Where(t => t.Kind == TraitKind.Personality).ToArray();

    public static TraitInfo? ByKey(string key) => ByKeyMap.GetValueOrDefault(key);

    // How many personality traits each stage is owed (the childhood one is owed from bébé on).
    private static int PersonalityOwed(PlynlingStage stage) => stage switch
    {
        PlynlingStage.Baby => 0,
        PlynlingStage.Teen => 2,
        _ => 3,
    };

    /// <summary>
    /// The traits a Plynling at <paramref name="stage"/> is owed and lacks, in the order to add them.
    /// Pure: the same id, stage and held traits always give the same answer. Slot n of the
    /// personality traits is always rolled with the same salt, so a Plynling drawn as an ado and
    /// again as an adulte keeps its first two and only adds the third. Uniform for now; the event
    /// engine weights the adulte slot by the ado years.
    /// </summary>
    public static IReadOnlyList<TraitInfo> Draw(int plynlingId, PlynlingStage stage, IReadOnlyCollection<string> held)
    {
        var owned = held.Select(ByKey).OfType<TraitInfo>().ToList();
        var drawn = new List<TraitInfo>();
        if (!owned.Any(t => t.Kind == TraitKind.Childhood))
            drawn.Add(Pick(Childhood, plynlingId, ChildhoodSalt));

        var have = owned.Count(t => t.Kind == TraitKind.Personality);
        for (var slot = have; slot < PersonalityOwed(stage); slot++)
        {
            var taken = owned.Concat(drawn).Where(t => t.Kind == TraitKind.Personality).Select(t => t.Group).ToHashSet();
            drawn.Add(Pick(Personality.Where(t => !taken.Contains(t.Group)).ToArray(), plynlingId, PersonalitySalt + slot));
        }
        return drawn;
    }

    private static TraitInfo Pick(TraitInfo[] pool, int plynlingId, int salt) =>
        pool[Math.Min(pool.Length - 1, (int)(StableRoll.Unit(plynlingId, salt, 0) * pool.Length))];
}
```

Note on "growing up only adds": slot 0 and 1 are drawn with salts 110 and 111 whether the draw happens at ado or straight at adulte, and the pool at slot n excludes only the groups of slots before it — so the ado-then-adulte path and the adulte-at-once path agree. The harness checks this.

- [ ] **Step 6: Run the harness**

Run: `cd "$SCRATCH/personality" && dotnet run`
Expected: `OK`

- [ ] **Step 7: Build the bot**

Run: `cd "$REPO/ProjectSYNCS" && dotnet build -warnaserror`
Expected: `Build succeeded`, 0 warnings.

- [ ] **Step 8: Files for the owner to commit**

`ProjectSYNCS/Models/PlynlingTrait.cs`, `ProjectSYNCS/Helpers/PlynlingStats.cs`, `ProjectSYNCS/Helpers/PlynlingTraits.cs` — suggested message: `Plynlings: trait catalog and draws`.

---

### Task 3: Stats, the personality title, and the mascot's fixed personality

**Files:**
- Modify: `ProjectSYNCS/Models/Plynling.cs` (five growth columns)
- Modify: `ProjectSYNCS/Helpers/PlynlingStats.cs`
- Create: `ProjectSYNCS/Helpers/PlynlingPersonality.cs` (title and card line in this task; the embed in Task 5)
- Modify: `ProjectSYNCS/Helpers/PlynlingMascot.cs`
- Test: `$SCRATCH/personality/Program.cs`

**Interfaces:**
- Consumes: `TraitInfo`, `PlynlingTraits.ByKey`, `StableRoll.Unit`, `PlynlingPassions.Taught(Plynling) -> Passion?` (record `Passion(PlynlingPassion? Catalog, string? Custom)`), `PlynlingMascot.Is(Plynling)`.
- Produces:
  - `Plynling.GrowthDiplomacy`, `GrowthStewardship`, `GrowthLearning`, `GrowthIntrigue`, `GrowthCourage` (int)
  - `sealed record StatLine(PlynlingStat Stat, int Base, int Passion, int Traits, int Growth, int Total)`
  - `PlynlingStats.All : IReadOnlyList<PlynlingStat>`, `PassionBonus = 2`, `Name(PlynlingStat) -> string`, `Emoji(PlynlingStat) -> string`, `PassionStat(PlynlingPassion) -> PlynlingStat`, `PassionFor(Plynling) -> PlynlingPassion`, `Base(Plynling, PlynlingStat) -> int`, `Growth(Plynling, PlynlingStat) -> int`, `Compute(Plynling, IReadOnlyList<TraitInfo>) -> IReadOnlyList<StatLine>`
  - `PlynlingPersonality.Axes(IEnumerable<TraitInfo>) -> IReadOnlyDictionary<AiAxis, int>`, `Title(IReadOnlyList<TraitInfo>, PlynlingGender) -> string?`, `CardLine(IReadOnlyList<TraitInfo>, PlynlingGender) -> string?`
  - `PlynlingMascot.TraitKeys : IReadOnlyList<string>`, `PlynlingMascot.BaseStat = 4`

- [ ] **Step 1: Add the failing checks**

In `$SCRATCH/personality/Program.cs`, insert before the final `Console.WriteLine`:

```csharp
// ---- stats ----------------------------------------------------------------------------------------
var dice = new HashSet<int>();
for (var id = 1; id <= 500; id++)
    foreach (var stat in PlynlingStats.All)
    {
        var b = PlynlingStats.Base(Fresh(id), stat);
        Check(b is >= 1 and <= 6, $"1d6 base ({id}, {stat})");
        dice.Add(b);
        Check(b == PlynlingStats.Base(Fresh(id), stat), $"base is stable ({id}, {stat})");
    }
Check(dice.SetEquals(new[] { 1, 2, 3, 4, 5, 6 }), "every face of the die comes up");

Check(PlynlingStats.PassionStat(PlynlingPassion.Cooking) == PlynlingStat.Stewardship
      && PlynlingStats.PassionStat(PlynlingPassion.Music) == PlynlingStat.Diplomacy
      && PlynlingStats.PassionStat(PlynlingPassion.Astronomy) == PlynlingStat.Learning
      && PlynlingStats.PassionStat(PlynlingPassion.Naps) == PlynlingStat.Intrigue
      && PlynlingStats.PassionStat(PlynlingPassion.Insects) == PlynlingStat.Courage, "passion → stat table");
Check(Enum.GetValues<PlynlingPassion>().Select(PlynlingStats.PassionStat).Distinct().Count() == 5, "every stat has a passion");

var cook = Fresh(11); cook.Passion = PlynlingPassion.Cooking;
Check(PlynlingStats.PassionFor(cook) == PlynlingPassion.Cooking, "innate passion by default");
cook.TaughtPassion = "les trains";
Check(PlynlingStats.PassionFor(cook) == PlynlingPassion.Cooking, "a free-text taught passion keeps the innate bonus");
cook.TaughtPassion = "la musique";
Check(PlynlingStats.PassionFor(cook) == PlynlingPassion.Music, "a taught catalog passion takes the bonus");

var sheet = PlynlingStats.Compute(cook, new[] { PlynlingTraits.ByKey("brave")!, PlynlingTraits.ByKey("lazy")! });
var courage = sheet.Single(l => l.Stat == PlynlingStat.Courage);
Check(courage.Traits == 2 && courage.Total == courage.Base + 2, "Courageux + Paresseux: Courage +2");
var diplomacy = sheet.Single(l => l.Stat == PlynlingStat.Diplomacy);
Check(diplomacy.Passion == 2 && diplomacy.Total == diplomacy.Base + 2 - 1, "taught music: Diplomatie +2, lazy -1");

var weak = Fresh(12);
var floor = PlynlingStats.Compute(weak, new[] { PlynlingTraits.ByKey("honest")!, PlynlingTraits.ByKey("lazy")!, PlynlingTraits.ByKey("compassionate")!, PlynlingTraits.ByKey("just")! });
Check(floor.Single(l => l.Stat == PlynlingStat.Intrigue).Total == 0, "a stat never goes below 0");
weak.GrowthCourage = 30;
Check(PlynlingStats.Compute(weak, Array.Empty<TraitInfo>()).Single(l => l.Stat == PlynlingStat.Courage).Total > 30, "no upper limit");

// ---- the mascot ----------------------------------------------------------------------------------
PlynlingMascot.Bind(999);
var mascotTraits = PlynlingMascot.TraitKeys.Select(k => PlynlingTraits.ByKey(k)!).ToList();
foreach (var id in new[] { 3, 77, 4242 })
{
    var m = Fresh(id); m.OwnerId = 999; m.Passion = PlynlingMascot.Passion;
    var totals = PlynlingStats.Compute(m, mascotTraits).ToDictionary(l => l.Stat, l => l.Total);
    Check(totals[PlynlingStat.Diplomacy] == 3 && totals[PlynlingStat.Stewardship] == 4 && totals[PlynlingStat.Learning] == 4
          && totals[PlynlingStat.Intrigue] == 12 && totals[PlynlingStat.Courage] == 7, $"Ping-Qilin's stats whatever her id ({id})");
}
Check(PlynlingPersonality.Title(mascotTraits, PlynlingGender.Female) == "Piquante et intrépide", "Ping-Qilin's title");
Check(PlynlingPersonality.Title(mascotTraits, PlynlingGender.Male) == "Piquant et intrépide", "the title agrees in the masculine");

// ---- title and card line ------------------------------------------------------------------------
Check(PlynlingPersonality.Title(Array.Empty<TraitInfo>(), PlynlingGender.Male) is null, "no traits, no title");
Check(PlynlingPersonality.CardLine(Array.Empty<TraitInfo>(), PlynlingGender.Male) is null, "no traits, no card line");
// Brave: Bol +200, then three ±20 ties (Energy, Rationality, Sociability) — axis order picks Energy.
Check(PlynlingPersonality.Title(new[] { PlynlingTraits.ByKey("brave")! }, PlynlingGender.Male) == "Intrépide et infatigable", "Courageux alone: ties break on axis order");
Check(PlynlingPersonality.CardLine(mascotTraits, PlynlingGender.Female) == "🎭 *Piquante et intrépide* · 🥺 🪞 👀 😏", "the card line");
for (var id = 1; id <= 300; id++)
{
    var ts = PlynlingTraits.Draw(id, PlynlingStage.Adult, Array.Empty<string>());
    Check(PlynlingPersonality.Title(ts, PlynlingGender.Female) is { Length: > 0 }, $"every drawn adulte has a title ({id})");
}
```

- [ ] **Step 2: Run it to see it fail**

Run: `cd "$SCRATCH/personality" && dotnet run`
Expected: build errors — `PlynlingStats.All`, `Base`, `GrowthCourage`, `PlynlingPersonality`, `PlynlingMascot.TraitKeys` do not exist.

- [ ] **Step 3: Add the growth columns**

In `ProjectSYNCS/Models/Plynling.cs`, after `public long FedByOthers { get; set; }`:

```csharp

    // What events have taught it, per stat (Helpers/PlynlingStats). The rest of a stat is computed —
    // a hashed die, its passion, its traits — so only what was earned is stored. 0 until events exist.
    public int GrowthDiplomacy { get; set; }
    public int GrowthStewardship { get; set; }
    public int GrowthLearning { get; set; }
    public int GrowthIntrigue { get; set; }
    public int GrowthCourage { get; set; }
```

- [ ] **Step 4: Fill `PlynlingStats`**

Replace the whole content of `ProjectSYNCS/Helpers/PlynlingStats.cs` with:

```csharp
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

// The five stats. Never stored as values, but **append-only** all the same: the RPG layer appends
// Force, Agilité and Endurance at the end, and the growth columns are named after these.
public enum PlynlingStat { Diplomacy, Stewardship, Learning, Intrigue, Courage }

// One stat and where it comes from, for the « Personnalité » breakdown.
public sealed record StatLine(PlynlingStat Stat, int Base, int Passion, int Traits, int Growth, int Total);

/// <summary>
/// A stat is computed, never stored: a die rolled from the id (1d6, hashed — nothing stored), the
/// passion's +2, every trait's modifier, and the growth events earned. Floor 0, no upper limit.
/// The mascot's die is fixed at <see cref="PlynlingMascot.BaseStat"/>, so she is the same character
/// on every guild and on dev.
/// </summary>
public static class PlynlingStats
{
    public const int PassionBonus = 2;
    private const int BaseSalt = 200;           // + the stat

    public static readonly IReadOnlyList<PlynlingStat> All = Enum.GetValues<PlynlingStat>();

    public static string Name(PlynlingStat stat) => stat switch
    {
        PlynlingStat.Diplomacy => "Diplomatie",
        PlynlingStat.Stewardship => "Intendance",
        PlynlingStat.Learning => "Sagesse",
        PlynlingStat.Intrigue => "Ruse",
        PlynlingStat.Courage => "Courage",
        _ => throw new ArgumentOutOfRangeException(nameof(stat), stat, null),
    };

    public static string Emoji(PlynlingStat stat) => stat switch
    {
        PlynlingStat.Diplomacy => "💬",
        PlynlingStat.Stewardship => "📦",
        PlynlingStat.Learning => "📚",
        PlynlingStat.Intrigue => "🦊",
        PlynlingStat.Courage => "🛡️",
        _ => throw new ArgumentOutOfRangeException(nameof(stat), stat, null),
    };

    // Exhaustive on purpose: a 13th passion must be given a stat here, or this throws.
    public static PlynlingStat PassionStat(PlynlingPassion passion) => passion switch
    {
        PlynlingPassion.Music or PlynlingPassion.Dance or PlynlingPassion.Painting => PlynlingStat.Diplomacy,
        PlynlingPassion.Cooking or PlynlingPassion.Gardening or PlynlingPassion.Rocks => PlynlingStat.Stewardship,
        PlynlingPassion.Astronomy or PlynlingPassion.Stories => PlynlingStat.Learning,
        PlynlingPassion.Gaming or PlynlingPassion.Naps => PlynlingStat.Intrigue,
        PlynlingPassion.Sport or PlynlingPassion.Insects => PlynlingStat.Courage,
        _ => throw new ArgumentOutOfRangeException(nameof(passion), passion, null),
    };

    // The passion that earns the bonus: a taught passion the catalog recognises, else the innate one.
    // A taught passion that stays free text changes nothing.
    public static PlynlingPassion PassionFor(Plynling p) =>
        PlynlingPassions.Taught(p) is { Catalog: { } taught } ? taught : p.Passion;

    public static int Base(Plynling p, PlynlingStat stat) =>
        PlynlingMascot.Is(p) ? PlynlingMascot.BaseStat : 1 + (int)(StableRoll.Unit(p.Id, BaseSalt + (int)stat, 0) * 6);

    public static int Growth(Plynling p, PlynlingStat stat) => stat switch
    {
        PlynlingStat.Diplomacy => p.GrowthDiplomacy,
        PlynlingStat.Stewardship => p.GrowthStewardship,
        PlynlingStat.Learning => p.GrowthLearning,
        PlynlingStat.Intrigue => p.GrowthIntrigue,
        PlynlingStat.Courage => p.GrowthCourage,
        _ => throw new ArgumentOutOfRangeException(nameof(stat), stat, null),
    };

    public static IReadOnlyList<StatLine> Compute(Plynling p, IReadOnlyList<TraitInfo> traits)
    {
        var passionStat = PassionStat(PassionFor(p));
        return All.Select(stat =>
        {
            var die = Base(p, stat);
            var passion = stat == passionStat ? PassionBonus : 0;
            var fromTraits = traits.Sum(t => t.Stats.GetValueOrDefault(stat));
            var growth = Growth(p, stat);
            return new StatLine(stat, die, passion, fromTraits, growth, Math.Max(0, die + passion + fromTraits + growth));
        }).ToList();
    }
}
```

- [ ] **Step 5: Create `PlynlingPersonality` (title and card line)**

`ProjectSYNCS/Helpers/PlynlingPersonality.cs`:

```csharp
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

/// <summary>
/// What a Plynling's traits add up to: the sum of their AI axes, the two strongest of which name it
/// (« Piquante et intrépide »), and the line its card shows. Pure.
/// </summary>
public static class PlynlingPersonality
{
    // One adjective per axis and direction, M then F. The title is « {Adj1} et {adj2} ».
    private static readonly Dictionary<(AiAxis Axis, bool Positive), (string M, string F)> Adjectives = new()
    {
        [(AiAxis.Boldness, true)] = ("intrépide", "intrépide"),
        [(AiAxis.Boldness, false)] = ("prudent", "prudente"),
        [(AiAxis.Compassion, true)] = ("tendre", "tendre"),
        [(AiAxis.Compassion, false)] = ("piquant", "piquante"),
        [(AiAxis.Greed, true)] = ("avide", "avide"),
        [(AiAxis.Greed, false)] = ("désintéressé", "désintéressée"),
        [(AiAxis.Energy, true)] = ("infatigable", "infatigable"),
        [(AiAxis.Energy, false)] = ("nonchalant", "nonchalante"),
        [(AiAxis.Honor, true)] = ("loyal", "loyale"),
        [(AiAxis.Honor, false)] = ("roublard", "roublarde"),
        [(AiAxis.Rationality, true)] = ("réfléchi", "réfléchie"),
        [(AiAxis.Rationality, false)] = ("fantasque", "fantasque"),
        [(AiAxis.Sociability, true)] = ("bavard", "bavarde"),
        [(AiAxis.Sociability, false)] = ("solitaire", "solitaire"),
        [(AiAxis.Vengefulness, true)] = ("susceptible", "susceptible"),
        [(AiAxis.Vengefulness, false)] = ("conciliant", "conciliante"),
        [(AiAxis.Zeal, true)] = ("mystique", "mystique"),
        [(AiAxis.Zeal, false)] = ("terre-à-terre", "terre-à-terre"),
    };

    public static IReadOnlyDictionary<AiAxis, int> Axes(IEnumerable<TraitInfo> traits)
    {
        var sum = Enum.GetValues<AiAxis>().ToDictionary(a => a, _ => 0);
        foreach (var t in traits)
            foreach (var (axis, value) in t.Axes)
                sum[axis] += value;
        return sum;
    }

    // The two strongest axes, by size then by axis order (so ties always break the same way).
    public static string? Title(IReadOnlyList<TraitInfo> traits, PlynlingGender gender)
    {
        var words = Axes(traits)
            .Where(kv => kv.Value != 0)
            .OrderByDescending(kv => Math.Abs(kv.Value)).ThenBy(kv => kv.Key)
            .Take(2)
            .Select(kv => Adjectives[(kv.Key, kv.Value > 0)])
            .Select(a => gender == PlynlingGender.Female ? a.F : a.M)
            .ToList();
        if (words.Count == 0) return null;
        var first = char.ToUpperInvariant(words[0][0]) + words[0][1..];
        return words.Count == 1 ? first : $"{first} et {words[1]}";
    }

    // Childhood first, then personality in the order acquired, then coping.
    public static IEnumerable<TraitInfo> Ordered(IReadOnlyList<TraitInfo> traits) =>
        traits.Where(t => t.Kind == TraitKind.Childhood)
            .Concat(traits.Where(t => t.Kind == TraitKind.Personality))
            .Concat(traits.Where(t => t.Kind == TraitKind.Coping));

    // The card's personality line: the title and the trait emojis. Null without traits.
    public static string? CardLine(IReadOnlyList<TraitInfo> traits, PlynlingGender gender)
    {
        if (traits.Count == 0) return null;
        var emojis = string.Join(" ", Ordered(traits).Select(t => t.Emoji));
        return Title(traits, gender) is { } title ? $"🎭 *{title}* · {emojis}" : $"🎭 {emojis}";
    }
}
```

- [ ] **Step 6: Fix the mascot's personality**

In `ProjectSYNCS/Helpers/PlynlingMascot.cs`, after `public const PlynlingPassion Passion = PlynlingPassion.Naps;`:

```csharp
    // Chosen, not drawn, so she is the same character on every guild and on dev: Adorable,
    // Vaniteuse, Méfiante, Moqueuse. PlynlingService.EnsureTraitsAsync gives her rows these four.
    public static readonly IReadOnlyList<string> TraitKeys = new[] { "charming", "arrogant", "paranoid", "sadistic" };

    // Her die for every stat (PlynlingStats.Base), instead of a roll hashed from a row id that
    // differs per guild.
    public const int BaseStat = 4;
```

- [ ] **Step 7: Run the harness**

Run: `cd "$SCRATCH/personality" && dotnet run`
Expected: `OK`

- [ ] **Step 8: Build the bot**

Run: `cd "$REPO/ProjectSYNCS" && dotnet build -warnaserror`
Expected: `Build succeeded`, 0 warnings. (The growth columns have no migration yet — Task 4 adds it; the bot is not run before then.)

- [ ] **Step 9: Files for the owner to commit**

`ProjectSYNCS/Models/Plynling.cs`, `ProjectSYNCS/Helpers/PlynlingStats.cs`, `ProjectSYNCS/Helpers/PlynlingPersonality.cs`, `ProjectSYNCS/Helpers/PlynlingMascot.cs` — suggested message: `Plynlings: stats, personality title, Ping-Qilin's personality`. Commit together with Task 4 if the owner prefers never to have a model change without its migration in history.

---

### Task 4: Storage, the migration, and drawing at adoption, creation and sweep

**Files:**
- Modify: `ProjectSYNCS/Data/AppDbContext.cs`
- Create: `ProjectSYNCS/Migrations/<timestamp>_AddPlynlingTraits.cs` (+ designer, snapshot — via `dotnet ef`)
- Modify: `ProjectSYNCS/Helpers/PlynlingJournalUi.cs` (`JournalKind.TraitGained` + its line)
- Modify: `ProjectSYNCS/Services/PlynlingService.cs` (`EnsureTraitsAsync`, `GetTraitsAsync`, calls in `AdoptAsync`, `EnsureMascotAsync`, `ProgressAsync`)

**Interfaces:**
- Consumes: `PlynlingTrait`, `TraitKind`, `PlynlingTraits.Draw`, `PlynlingTraits.ByKey`, `PlynlingMascot.TraitKeys`, `PlynlingLife.Stage`.
- Produces: `AppDbContext.PlynlingTraits : DbSet<PlynlingTrait>`; `JournalKind.TraitGained` (detail = trait key); `PlynlingService.EnsureTraitsAsync(Plynling p, DateTimeOffset now) -> Task` (adds, never saves); `PlynlingService.GetTraitsAsync(Plynling p) -> Task<IReadOnlyList<TraitInfo>>` (acquisition order).

- [ ] **Step 1: Register the table**

In `ProjectSYNCS/Data/AppDbContext.cs`, after `public DbSet<PlynlingBadge> PlynlingBadges => Set<PlynlingBadge>();`:

```csharp
    public DbSet<PlynlingTrait> PlynlingTraits => Set<PlynlingTrait>();
```

and in `OnModelCreating`, right after the `modelBuilder.Entity<PlynlingBadge>(…);` block:

```csharp
        // Its traits: drawn once and stored, so the catalog can grow without changing anyone. One row
        // per trait per Plynling; deleted with an abandoned one, kept on a dead one, like its badges.
        modelBuilder.Entity<PlynlingTrait>(e =>
        {
            e.HasOne<Plynling>().WithMany().HasForeignKey(x => x.PlynlingId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.PlynlingId, x.Key }).IsUnique();
        });
```

- [ ] **Step 2: Append the journal kind**

In `ProjectSYNCS/Helpers/PlynlingJournalUi.cs`, change the end of the enum from

```csharp
    // sickness (Died's detail is "illness" for an illness death)
    FellSick, Recovered,
}
```

to

```csharp
    // sickness (Died's detail is "illness" for an illness death)
    FellSick, Recovered,
    // detail: the trait key (Helpers/PlynlingTraits)
    TraitGained,
}
```

and in `Line(...)`, right after the `JournalKind.Recovered => …,` arm:

```csharp
        JournalKind.TraitGained => PlynlingTraits.ByKey(detail ?? "") is { } trait
            ? $"{trait.Emoji} Un nouveau trait : **{trait.Name(g)}**."
            : "🎭 Un nouveau trait.",
```

- [ ] **Step 3: Add `EnsureTraitsAsync` and `GetTraitsAsync`**

In `ProjectSYNCS/Services/PlynlingService.cs`, right before `public async Task ProgressAsync(Plynling p, DateTimeOffset now)`:

```csharp
    // Whatever its stage is owed and it lacks (PlynlingTraits.Draw): the childhood trait from bébé,
    // two personality traits at ado, the fourth trait at adulte. The mascot gets her four chosen ones
    // instead. One path for adoption, the mascot's creation and every sweep — which is also what gives
    // Plynlings that predate traits theirs. Adds and journals; never saves (rides the caller's save).
    public async Task EnsureTraitsAsync(Plynling p, DateTimeOffset now)
    {
        if (p.DiedAt is not null) return;
        var held = await _db_context.PlynlingTraits.Where(t => t.PlynlingId == p.Id).Select(t => t.Key).ToListAsync();
        var add = PlynlingMascot.Is(p)
            ? PlynlingMascot.TraitKeys.Where(k => !held.Contains(k)).Select(k => PlynlingTraits.ByKey(k)!).ToList()
            : PlynlingTraits.Draw(p.Id, PlynlingLife.Stage(p, now), held);
        foreach (var trait in add)
        {
            _db_context.PlynlingTraits.Add(new PlynlingTrait { PlynlingId = p.Id, Key = trait.Key, Kind = trait.Kind, AcquiredAt = now });
            await AddMomentAsync(p, JournalKind.TraitGained, trait.Key, now);
        }
    }

    // Its traits in the order acquired. A key no longer in the catalog is skipped, never thrown on.
    public async Task<IReadOnlyList<TraitInfo>> GetTraitsAsync(Plynling p) =>
        (await _db_context.PlynlingTraits.Where(t => t.PlynlingId == p.Id).OrderBy(t => t.Id).Select(t => t.Key).ToListAsync())
        .Select(PlynlingTraits.ByKey).OfType<TraitInfo>().ToList();
```

- [ ] **Step 4: Draw at adoption, at the mascot's creation and on every sweep**

In `AdoptAsync`, change

```csharp
        await AddMomentAsync(plynling, JournalKind.Adopted, null, now);  // needs its id: after the first save
        await _db_context.SaveChangesAsync();
        return (AdoptOutcome.Adopted, plynling);
```

to

```csharp
        await AddMomentAsync(plynling, JournalKind.Adopted, null, now);  // needs its id: after the first save
        await EnsureTraitsAsync(plynling, now);                           // its childhood trait, at once
        await _db_context.SaveChangesAsync();
        return (AdoptOutcome.Adopted, plynling);
```

In `EnsureMascotAsync`, change

```csharp
        await AddMomentAsync(plynling, JournalKind.Adopted, null, now);
        await _db_context.SaveChangesAsync();
        return plynling;
```

to

```csharp
        await AddMomentAsync(plynling, JournalKind.Adopted, null, now);
        await EnsureTraitsAsync(plynling, now);
        await _db_context.SaveChangesAsync();
        return plynling;
```

In `ProgressAsync`, change

```csharp
        await AwardAsync(p, now);
    }
```

to

```csharp
        await EnsureTraitsAsync(p, now);   // the traits its new stage brings — and any it predates
        await AwardAsync(p, now);
    }
```

(`ProgressAsync` already returns early for a dead Plynling, and the sweep calls it inside its per-item `try`, before its save.)

- [ ] **Step 5: Build, then generate the migration**

Run: `cd "$REPO/ProjectSYNCS" && dotnet build -warnaserror && dotnet ef migrations add AddPlynlingTraits`
Expected: `Build succeeded`, then `Done.`

- [ ] **Step 6: Read the migration**

Open the new `Migrations/*_AddPlynlingTraits.cs`. Expected in `Up`, and nothing else:
- five `AddColumn<int>` on `Plynlings` (`GrowthCourage`, `GrowthDiplomacy`, `GrowthIntrigue`, `GrowthLearning`, `GrowthStewardship`), `nullable: false`, `defaultValue: 0`;
- `CreateTable` `PlynlingTraits` (`Id`, `PlynlingId`, `Key`, `Kind`, `AcquiredAt`) with the cascade foreign key to `Plynlings`;
- `CreateIndex` unique on (`PlynlingId`, `Key`).

No `Sql(...)`, no data. If anything else appears (a change to another table), stop and find which model change caused it.

- [ ] **Step 7: Build again**

Run: `cd "$REPO/ProjectSYNCS" && dotnet build -warnaserror`
Expected: `Build succeeded`, 0 warnings.

- [ ] **Step 8: Run the harness (regression)**

Run: `cd "$SCRATCH/personality" && dotnet run`
Expected: `OK`

- [ ] **Step 9: Files for the owner to commit**

`ProjectSYNCS/Data/AppDbContext.cs`, `ProjectSYNCS/Migrations/*_AddPlynlingTraits.cs`, `ProjectSYNCS/Migrations/*_AddPlynlingTraits.Designer.cs`, `ProjectSYNCS/Migrations/AppDbContextModelSnapshot.cs`, `ProjectSYNCS/Helpers/PlynlingJournalUi.cs`, `ProjectSYNCS/Services/PlynlingService.cs` — suggested message: `Plynlings: store traits, draw at adoption and on the sweep`.

---

### Task 5: The card line, « Personnalité », and its ephemeral view

**Files:**
- Modify: `ProjectSYNCS/Helpers/PlynlingPersonality.cs` (`DetailEmbed`)
- Modify: `ProjectSYNCS/Commands/PlynlingModule.cs` (`BuildCard` parameter, line and row; the `view` call site)
- Modify: `ProjectSYNCS/Services/PlynlingCareService.cs` (four `BuildCard` call sites)
- Create: `ProjectSYNCS/Interactions/Components/PlynlingPersonalityHandler.cs`
- Test: `$SCRATCH/personality/Program.cs`

**Interfaces:**
- Consumes: `PlynlingStats.Compute`, `PlynlingStats.Name`, `PlynlingStats.Emoji`, `PlynlingPersonality.Title`, `PlynlingPersonality.Ordered`, `PlynlingPersonality.CardLine`, `PlynlingService.GetTraitsAsync`, `PlynlingService.GetByIdAsync(int, DateTimeOffset) -> Task<Plynling?>`, `PlynlingCardUi.SafeName`, `PlynlingCatalog.Info(species).Accent` (uint), `PlynlingText.Unknown`.
- Produces: `PlynlingPersonality.DetailEmbed(Plynling, IReadOnlyList<TraitInfo>) -> Embed`; `PlynlingModule.BuildCard(…, IReadOnlyList<TraitInfo>? traits = null)`; custom-id `plyn:traits:{id}`.

- [ ] **Step 1: Check the custom-id is free**

Run: `cd "$REPO/ProjectSYNCS" && grep -rn "plyn:traits" --include=*.cs .`
Expected: no output.

- [ ] **Step 2: Add the failing checks**

In `$SCRATCH/personality/Program.cs`, add `using Discord;` and `using ProjectSYNCS.Commands;` at the top, and insert before the final `Console.WriteLine`:

```csharp
// ---- « Personnalité » ------------------------------------------------------------------------------
var hostile = Fresh(21);
hostile.Name = new string('*', InputCaps.PlynlingName - 4) + "@`_~";
var hostileTraits = PlynlingTraits.Draw(21, PlynlingStage.Adult, Array.Empty<string>()).Concat(new[] { PlynlingTraits.ByKey("journaller")!, PlynlingTraits.ByKey("athletic")! }).ToList();
var detail = PlynlingPersonality.DetailEmbed(hostile, hostileTraits);
Check(detail.Length <= 6000 && detail.Title.Length <= 256 && (detail.Description?.Length ?? 0) <= 4096
      && detail.Fields.All(f => f.Value.Length <= 1024), "the Personnalité embed fits Discord's caps with a hostile name");
Check(detail.Description!.Contains("Écrit son journal"), "coping traits are listed");
Check(PlynlingPersonality.DetailEmbed(Fresh(22), Array.Empty<TraitInfo>()).Description!.Contains("grandissant"), "no traits yet: says they come with age");

// ---- the card ---------------------------------------------------------------------------------------
bool HasPersonalityButton(MessageComponent card, int id) =>
    card.Components.OfType<ActionRowComponent>().Any(r => r.Components.OfType<ButtonComponent>().Any(b => b.CustomId == $"plyn:traits:{id}"));
var noon = new DateTimeOffset(2026, 10, 6, 10, 0, 0, TimeSpan.Zero);       // 12:00 Paris, awake
var night = new DateTimeOffset(2026, 10, 6, 1, 0, 0, TimeSpan.Zero);       // 03:00 Paris, asleep
var awake = Fresh(31);
var cardAwake = PlynlingModule.BuildCard(awake, noon, "Une ligne.", traits: mascotTraits);
Check(HasPersonalityButton(cardAwake, 31), "awake: Personnalité on the card");
Check(cardAwake.Components.OfType<ActionRowComponent>().All(r => r.Components.Count <= 5), "awake: ≤ 5 per row");
Check(HasPersonalityButton(PlynlingModule.BuildCard(awake, night, null, traits: mascotTraits), 31), "asleep: Personnalité stays");
var frozen = Fresh(32); PlynlingLife.Freeze(frozen, noon, byStaff: true);
Check(HasPersonalityButton(PlynlingModule.BuildCard(frozen, noon, null, traits: mascotTraits), 32), "frozen: Personnalité stays");
var dead = Fresh(33); dead.DiedAt = noon;
Check(!HasPersonalityButton(PlynlingModule.BuildCard(dead, noon, null, traits: mascotTraits), 33), "dead: no Personnalité");
Check(HasPersonalityButton(PlynlingModule.BuildCard(awake, noon, null), 31), "the button does not depend on traits being passed");
```

(`BuildCard` throws inside `ComponentBuilderV2.Build()` past 40 components, so building at all is the 40-component check.)

- [ ] **Step 3: Run it to see it fail**

Run: `cd "$SCRATCH/personality" && dotnet run`
Expected: build errors — `DetailEmbed` and the `traits:` parameter of `BuildCard` do not exist.

- [ ] **Step 4: Add `DetailEmbed`**

In `ProjectSYNCS/Helpers/PlynlingPersonality.cs`, add `using Discord;` and `using System.Text;` at the top, and inside the class, after `CardLine`:

```csharp
    // « 📜 Personnalité », answered privately: the title, each trait with what it does, and each stat
    // with where it comes from — CK3's tooltip, in an embed. The name is hostile input.
    public static Embed DetailEmbed(Plynling p, IReadOnlyList<TraitInfo> traits)
    {
        var text = new StringBuilder();
        if (Title(traits, p.Gender) is { } title) text.AppendLine($"*« {title} »*").AppendLine();
        if (traits.Count == 0)
            text.AppendLine("Pas encore de traits : ils arrivent en grandissant.");
        foreach (var t in Ordered(traits))
            text.AppendLine($"{t.Emoji} **{t.Name(p.Gender)}** — {t.Description}{Effects(t)}");

        var stats = string.Join("\n", PlynlingStats.Compute(p, traits).Select(StatText));
        return new EmbedBuilder()
            .WithTitle($"📜 Personnalité de {PlynlingCardUi.SafeName(p.Name)}")
            .WithColor(new Color(PlynlingCatalog.Info(p.Species).Accent))
            .WithDescription(text.ToString())
            .AddField("Statistiques", stats)
            .Build();
    }

    private static string Signed(int v) => v > 0 ? $"+{v}" : $"−{-v}";

    private static string Effects(TraitInfo t) => t.Stats.Count == 0
        ? ""
        : " *(" + string.Join(" · ", PlynlingStats.All.Where(t.Stats.ContainsKey).Select(s => $"{PlynlingStats.Emoji(s)} {Signed(t.Stats[s])}")) + ")*";

    private static string StatText(StatLine l)
    {
        var parts = new List<string> { $"base {l.Base}" };
        if (l.Passion != 0) parts.Add($"passion {Signed(l.Passion)}");
        if (l.Traits != 0) parts.Add($"traits {Signed(l.Traits)}");
        if (l.Growth != 0) parts.Add($"progrès {Signed(l.Growth)}");
        return $"{PlynlingStats.Emoji(l.Stat)} **{PlynlingStats.Name(l.Stat)} {l.Total}** · {string.Join(" · ", parts)}";
    }
```

- [ ] **Step 5: The card's line and row**

In `ProjectSYNCS/Commands/PlynlingModule.cs`, change the `BuildCard` signature from

```csharp
    public static MessageComponent BuildCard(
        Plynling plynling, DateTimeOffset now, string? lastAction, string? lastActionImage = null, string? partnerName = null,
        IReadOnlyDictionary<PlynlingFood, int>? pantry = null)
```

to

```csharp
    public static MessageComponent BuildCard(
        Plynling plynling, DateTimeOffset now, string? lastAction, string? lastActionImage = null, string? partnerName = null,
        IReadOnlyDictionary<PlynlingFood, int>? pantry = null, IReadOnlyList<TraitInfo>? traits = null)
```

Right after

```csharp
            .AddComponent(new TextDisplayBuilder(PlynlingCardUi.Status(plynling, now)));
```

add

```csharp
        // Its personality under the status: the title and the trait emojis (PlynlingPersonality).
        if (PlynlingPersonality.CardLine(traits ?? Array.Empty<TraitInfo>(), plynling.Gender) is { } personality)
            container.AddComponent(new TextDisplayBuilder(personality));
```

And change the end of the method from

```csharp
            builder.AddComponent(new ActionRowBuilder().WithSelectMenu(menu));
        }
        return builder.Build();
```

to

```csharp
            builder.AddComponent(new ActionRowBuilder().WithSelectMenu(menu));
        }
        // Its own row, the card's last: the care row is hidden at night and holds three buttons when
        // sick. Shown frozen too — a personality does not thaw. Anyone may look.
        if (alive)
            builder.AddComponent(new ActionRowBuilder()
                .WithButton("Personnalité", $"plyn:traits:{plynling.Id}", ButtonStyle.Secondary, new Emoji("📜")));
        return builder.Build();
```

Also add to the `BuildCard` remarks' verb list: replace `(<c>plyn:pet</c>, <c>plyn:bath</c>, <c>plyn:heal</c>, <c>plyn:feed</c>)` with `(<c>plyn:pet</c>, <c>plyn:bath</c>, <c>plyn:heal</c>, <c>plyn:feed</c>, <c>plyn:traits</c>)`.

- [ ] **Step 6: Pass the traits from every call site**

In `PlynlingModule.cs` (the `view` command, around the line `await RespondAsync(components: BuildCard(plynling, now, line, partnerName: partner?.Name, pantry: pantry),`), change that call to:

```csharp
        await RespondAsync(components: BuildCard(plynling, now, line, partnerName: partner?.Name, pantry: pantry,
                traits: await _plynlings.GetTraitsAsync(plynling)),
```

(keep whatever follows on the original line — flags and `allowedMentions` — unchanged). Check the field name first: `grep -n "PlynlingService" Commands/PlynlingModule.cs` — use the module's existing `PlynlingService` field.

In `ProjectSYNCS/Services/PlynlingCareService.cs`, for each of the four `PlynlingModule.BuildCard(` calls, add `traits: await _plynlings.GetTraitsAsync(<the Plynling passed as first argument>)` as the last argument. The four become:

```csharp
        return new CareReply(PlynlingModule.BuildCard(plynling, now, text, partnerName: partner?.Name,
            pantry: await _plynlings.GetPantryAsync(plynling), traits: await _plynlings.GetTraitsAsync(plynling)), null);
```

```csharp
        return new CareReply(PlynlingModule.BuildCard(plynling, now, line, partnerName: partner?.Name,
            pantry: await _plynlings.GetPantryAsync(plynling), traits: await _plynlings.GetTraitsAsync(plynling)), null);
```

(twice — bath and medicine), and for the meal (`result.Plynling`):

```csharp
        return new CareReply(PlynlingModule.BuildCard(result.Plynling, now, text, PlynlingArt.Food(food), partner?.Name,
```

keeps its first line; on its continuation line, append `traits: await _plynlings.GetTraitsAsync(result.Plynling)` as the last argument. Then:

Run: `cd "$REPO/ProjectSYNCS" && grep -rn "BuildCard(" --include=*.cs . | grep -v LevelModule | grep -v "static MessageComponent"`
Expected: five lines, each with `traits:` on it or on its continuation line.

- [ ] **Step 7: The handler**

`ProjectSYNCS/Interactions/Components/PlynlingPersonalityHandler.cs`:

```csharp
using Discord;
using Discord.Interactions;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Interactions.Components;

// « 📜 Personnalité » on the Plynling card: its traits and stats, answered privately to whoever
// pressed. Its own module rather than PlynlingComponentHandler, which is already long; never on
// PlynlingModule (registered twice, /plynling and /pl).
public class PlynlingPersonalityHandler : InteractionModuleBase<SocketInteractionContext>
{
    private readonly PlynlingService _plynlings;

    public PlynlingPersonalityHandler(PlynlingService plynlings) => _plynlings = plynlings;

    [ComponentInteraction("plyn:traits:*", ignoreGroupNames: true)]
    public async Task OnTraitsAsync(string idStr)
    {
        if (!int.TryParse(idStr, out var id))
        {
            await RespondAsync(PlynlingText.Unknown, ephemeral: true);
            return;
        }
        // Two reads before anything can be shown: defer first (the Pi can outrun Discord's 3 s).
        await DeferAsync(ephemeral: true);
        var plynling = await _plynlings.GetByIdAsync(id, DateTimeOffset.UtcNow);
        if (plynling is null)
        {
            await FollowupAsync(PlynlingText.Unknown, ephemeral: true);
            return;
        }
        var traits = await _plynlings.GetTraitsAsync(plynling);
        await FollowupAsync(embed: PlynlingPersonality.DetailEmbed(plynling, traits), ephemeral: true,
            allowedMentions: AllowedMentions.None);
    }
}
```

Check `PlynlingText` lives where the other handler finds it: `grep -rn "class PlynlingText" ProjectSYNCS` — add the matching `using` if it is not `ProjectSYNCS.Helpers`.

- [ ] **Step 8: Run the harness**

Run: `cd "$SCRATCH/personality" && dotnet run`
Expected: `OK`

- [ ] **Step 9: Build the bot**

Run: `cd "$REPO/ProjectSYNCS" && dotnet build -warnaserror`
Expected: `Build succeeded`, 0 warnings.

- [ ] **Step 10: Files for the owner to commit**

`ProjectSYNCS/Helpers/PlynlingPersonality.cs`, `ProjectSYNCS/Commands/PlynlingModule.cs`, `ProjectSYNCS/Services/PlynlingCareService.cs`, `ProjectSYNCS/Interactions/Components/PlynlingPersonalityHandler.cs` — suggested message: `Plynlings: personality on the card and « Personnalité »`.

---

### Task 6: Help, docs, version, and the dev-guild test

**Files:**
- Modify: `ProjectSYNCS/Commands/PlynlingModule.cs` (`BuildHelpEmbed`)
- Modify: `README.md`
- Create: `docs/agents/plynling-events.md`
- Modify: `docs/agents/plynling.md`
- Modify: `CLAUDE.md`
- Modify: `ProjectSYNCS/config.yaml`
- Test: `$SCRATCH/personality/Program.cs`

**Interfaces:**
- Consumes: everything above. Produces nothing new in code.

- [ ] **Step 1: Add the help check**

In `$SCRATCH/personality/Program.cs`, insert before the final `Console.WriteLine`:

```csharp
// ---- /plynling help ----------------------------------------------------------------------------------
var help = PlynlingModule.BuildHelpEmbed();
Check(help.Length <= 6000, $"/plynling help fits ({help.Length}/6000)");
Check(help.Fields.All(f => f.Value.Length <= 1024), "every help field ≤ 1024");
Check(help.Fields[0].Value.Contains("Personnalité") && help.Fields[0].Value.Contains("première semaine"), "help mentions Personnalité and the new bébé stage");
```

Run: `cd "$SCRATCH/personality" && dotnet run`
Expected: `FAIL` with « help mentions Personnalité and the new bébé stage ».

- [ ] **Step 2: Edit `/plynling help`**

In `BuildHelpEmbed`, in the « Adopter & regarder » field, change

```csharp
                "Il grandit : **bébé** ses 2 premiers jours, **ado** jusqu'à 14 jours, **adulte**, " +
                "puis **ancien** après 6 mois. Le temps passé gelé ne compte pas.")
```

to

```csharp
                "Il grandit : **bébé** sa première semaine, **ado** jusqu'à 14 jours, **adulte**, " +
                "puis **ancien** après 6 mois. Le temps passé gelé ne compte pas.\n" +
                "**Personnalité** (bouton de sa carte) — Ses 4 traits, gagnés en grandissant, et ses stats.")
```

Run: `cd "$SCRATCH/personality" && dotnet run`
Expected: `OK` (help at about 5 990 / 6000). If the length check fails, shorten the new line — never another field — and re-run. The two-page split planned in the spec happens with phase 2.

- [ ] **Step 3: README**

In `README.md`, change the line

```markdown
- **Growing up:** *bébé* for its first 2 days, *ado* until 14 days, *adulte*, then *ancien*
```

to

```markdown
- **Growing up:** *bébé* for its first week, *ado* until 14 days, *adulte*, then *ancien*
```

(keep the rest of that line as it is), and right after the bullet it belongs to, add:

```markdown
- **Personality:** every Plynling gets traits as it grows — one childhood trait as a *bébé*, two
  personality traits as an *ado*, a fourth as an *adulte* — adapted from Crusader Kings III (Courageux,
  Timide, Gourmand…). Its traits and its passion shape five stats (Diplomatie, Intendance, Sagesse,
  Ruse, Courage) and a little title (« Piquante et intrépide »). The card shows them; « 📜 Personnalité »
  details them privately.
```

- [ ] **Step 4: The new subsystem notes**

`docs/agents/plynling-events.md`:

```markdown
# Plynling personality and events

Traits, stats and the personality title (phase 1 of
`docs/superpowers/specs/2026-10-06-plynling-events-design.md`); events, stress and modifiers come in
later phases and get their rules here. The pet itself is in `plynling.md`. **Before writing any
trait, title or event text, read `docs/plynling-writing-style.md`.**

## Traits — `Helpers/PlynlingTraits`

- The catalog is CK3's (values from the CK3 wiki): 5 childhood, 36 personality in 18 exclusion
  groups, 10 coping (mental breaks only — never drawn). **Keys are stored and never renamed**; append
  new traits at the end of their kind. `TraitKind` is an int, append-only.
- **Owed by stage:** the childhood trait from bébé, 2 personality traits at ado, 3 from adulte. Never
  two personality traits of one group.
- **Draws are hashed** (`StableRoll`, salts 101 and 110 + slot), **then stored** in `PlynlingTrait`
  (unique on Plynling + key). Slot n always uses the same salt, so drawing at ado then adulte gives
  the same traits as drawing at adulte at once. Storing is what lets the catalog grow without
  changing anyone.
- **One path: `PlynlingService.EnsureTraitsAsync`** — "draw what the stage is owed and it lacks" —
  called at adoption, at the mascot's creation and from `ProgressAsync` on every sweep. That last
  call is also the backfill for Plynlings older than traits; there is no data migration. Dead rows
  are skipped; a resurrected one catches up on the next sweep. It never saves.
- Descriptions are **shared by both genders**, so they never agree with the Plynling (the harness
  bans il/elle in them); names have M and F forms.
- **Ping-Qilin's traits are chosen**, not drawn (`PlynlingMascot.TraitKeys`: Adorable, Vaniteuse,
  Méfiante, Moqueuse) and her die is fixed at 4, so she is the same character on every guild and
  on dev.

## Stats — `Helpers/PlynlingStats`

Computed, never stored: a hashed 1d6 per stat, +2 on its passion's stat (a taught passion the
catalog recognises takes the bonus; free text leaves it on the innate one), every trait's
modifier, and the stored growth columns. **Floor 0, no upper limit.** `PlynlingStat` is
append-only — the RPG appends Force, Agilité, Endurance. `PassionStat` is exhaustive and throws on a
passion it does not map.

## Personality title — `Helpers/PlynlingPersonality`

The traits' AI axes are summed; the two largest by size (ties: axis order) pick adjectives from an
18-entry table, « {Adj1} et {adj2} ». The card shows it with the trait emojis under the status;
« 📜 Personnalité » (`plyn:traits:{id}`, `PlynlingPersonalityHandler`, anyone, ephemeral, deferred)
shows each trait and each stat's breakdown.

## `StableRoll`

The one SplitMix64 for every roll that must come out the same whoever computes it.
`PlynlingSickness.Roll` delegates with `purpose + 1` as the third input, bit-identical to before —
never change that mapping, every stored morning depends on it.
```

- [ ] **Step 5: Point to it**

In `docs/agents/plynling.md`:
- in « Life stages », change `bébé (< 2 d), ado (< 14 d)` to `bébé (< 7 d), ado (< 14 d)`, and replace the sentence that starts « **Cosmetic only** — nothing about needs or death » (it wraps onto the next line, ending « reads them. ») with « Nothing about needs or death reads them; they gate trait draws (`plynling-events.md`). »
- right after the first paragraph of the file (the one ending « read `docs/plynling-writing-style.md`. »), add:

```markdown
Traits, stats, the personality title — and later events, stress and modifiers — are in
`plynling-events.md`.
```

In `CLAUDE.md`, in the table, after the row `| Plynlings (life, care, visits, relations, art) | \`docs/agents/plynling.md\` |`, add:

```markdown
| Plynling traits, stats, personality, events | `docs/agents/plynling-events.md` |
```

- [ ] **Step 6: Bump the version**

In `ProjectSYNCS/config.yaml`, change the current version (5.11.26 when this plan was written) to `version: "5.12.0"`.

- [ ] **Step 7: Build**

Run: `cd "$REPO/ProjectSYNCS" && dotnet build -warnaserror`
Expected: `Build succeeded`, 0 warnings.

- [ ] **Step 8: Dev-guild test (with the owner)**

Run: `cd "$REPO/ProjectSYNCS" && dotnet run` (dev token and dev guild in user secrets). Then, in the dev guild:

1. Startup log shows the `AddPlynlingTraits` migration applied, no error.
2. `/plynling adopt name:Test` → the card shows a 🎭 line with one emoji and a title, and a « 📜 Personnalité » row at the bottom.
3. Press « Personnalité » → private embed: title, one childhood trait with its effect, five stats with « base n · passion +2 · traits … ».
4. `/plynling journal` → « Un nouveau trait : … » moment.
5. Ping-Qilin (`/plynling view user:@SYNCS`) → after the first sweep (about one hour after start), « 🎭 *Piquante et intrépide* · 🥺 🪞 👀 😏 », and her Personnalité shows Dip 3, Int 4, Sag 4, Rus 12, Cou 7.
6. An existing Plynling older than 14 days → after the first sweep, four traits, three personality ones from different pairs; its journal has four « nouveau trait » moments.
7. A frozen Plynling's card still has « Personnalité »; a dead one's does not.
8. `/plynling help` → sends (no error), mentions « Personnalité » and « première semaine ».

Report each result to the owner; anything failing goes back to the task that owns it.

- [ ] **Step 9: Files for the owner to commit**

`ProjectSYNCS/Commands/PlynlingModule.cs`, `README.md`, `docs/agents/plynling-events.md`, `docs/agents/plynling.md`, `CLAUDE.md`, `ProjectSYNCS/config.yaml` — suggested message: `Plynlings: personality docs and help, version 5.12.0`.
