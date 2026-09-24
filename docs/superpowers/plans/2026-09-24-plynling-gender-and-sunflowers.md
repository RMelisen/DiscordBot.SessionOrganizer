# Plynlings: gender and the Sunflower family — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Every Plynling gets a gender rolled at adoption, with French that agrees everywhere it is talked about, and a second family — Sunflowers, six variants with their own sprites and memorials — is chosen at adoption.

**Art direction:** the sunflower is **potted** — chosen from three drawn concepts (rounded, potted, crowned) after the first draft was rejected.

**Architecture:** Gender is a new column (`PlynlingGender`, default Male); the family is *derived* from the species (`SpeciesInfo.Family`), so the only schema change is that one column. Every Plynling pool becomes a `GenderedLines(M, F)` record picked with `.For(p.Gender)`, and short fixed words go through `PlynlingGrammar.Agree`. The art pipeline gains `sunflowers.py`, reusing the mushroom face via an origin offset, and `memorials.py` becomes family-aware — with every existing mushroom PNG proven byte-identical.

**Tech Stack:** C# / .NET 10, Discord.Net 3.20.1, EF Core 10 on SQLite, Python 3 + Pillow 12 for the art.

**Spec:** `docs/superpowers/specs/2026-09-24-plynling-gender-and-sunflowers-design.md`

## Scope amendment — gender now, sunflowers later

Decided after the plan was written: **the gender ships now; the Sunflower family does not.**
Families will come later. So:

- **Task 1** lands as written: the gender, *and* the family layer — `PlynlingFamily`, the six
  sunflower species and their catalog rows, the per-family roll, the exhaustive `Key`. That
  layer is **dormant**: only mushrooms can be adopted, and nothing renders a sunflower.
- **Task 2** lands as written (every line in both genders).
- **Tasks 3, 4 and 5 are deferred** — the `family:` option and all the sunflower art. They are
  kept intact below, already tested on a scratch copy, for when families ship.
- **Task 6** is gender-only: no mention of sunflowers anywhere a user can see.

**Since then, the mushrooms were reshaped (art v2)** — each species has its own silhouette in
`tools/plynling-art/species.py`, and `sprites.build` dispatches through `species.DRAW`. Before
resuming Tasks 4–5: `sprites.face` **already** takes `ox, oy, skin, skin_out` (Task 4 Step 3 is
done — its anchors no longer exist); the sunflower `build` should join as entries in
`species.DRAW` rather than a branch in `export.py`; `PlynlingArt.Version` is **2**, so the
byte-identical guard covers the 70 `_v2` files, and the export writes sunflower files as `_v2`
too; and `memorials.py` is unchanged, so its Task 4 edits still apply.

## Global Constraints

- Command and option names are **English**; every other user-facing string is **French**.
- **Never commit.** The user commits manually; each task ends at a checkpoint.
- `PlynlingSpecies` is stored as an int: **append-only**. The six sunflower values go after `Dore`.
- `PlynlingGender { Male, Female }` — `Male = 0`, the column default. Rolled 50/50 at adoption, never edited.
- Each family: six species, weights **70 / 70 / 70 / 54 / 27 / 9** (3 common, 1 uncommon, 1 rare, 1 legendary).
- A female Plynling is *une Plynling* / *ta Plynling*. Text not about one specific Plynling (help, README, descriptions, person-level refusals) stays in the generic masculine.
- Lines are **family-neutral**: never name a body part one family lacks (no "chapeau", no "pétales").
- Everyone eats the same four foods; feeding is unchanged.
- `PlynlingArt.Version` stays **1**; mushroom filenames never change; the **70 existing PNGs must stay byte-identical**.
- Line endings: C# and Markdown files are **CRLF**, the Python art scripts are **LF**. Edits keep each file's own endings; new C# files are CRLF, new Python files LF.
- Build: `dotnet build ProjectSYNCS/ProjectSYNCS.csproj -warnaserror` → 0 warnings, 0 errors.
- `$SCRATCH` = the session scratchpad (`C:\Users\c235773\AppData\Local\Temp\claude\C--Users-c235773-Desktop-Sources---RME-Discord-Bots-SessionOrganizer\63c08e3a-fa93-4b2c-a86b-36f5960f12b6\scratchpad`). The harnesses there (`plynlingcheck`, `plynlingdb`, `plynlingui`, `artcheck`, `economycheck`, `graveyardcheck`, `helpcheck2`, `modulecheck`) reference the real project; run each with `dotnet run --project "$SCRATCH/<name>"`. Every new check is mutation-tested (break the code, see it fail, restore byte-identical).

---

### Task 1: Gender and families in the model and catalog

**Files:**
- Modify: `ProjectSYNCS/Models/Plynling.cs`
- Modify: `ProjectSYNCS/Helpers/PlynlingCatalog.cs`
- Modify: `ProjectSYNCS/Helpers/PlynlingLife.cs` (`Create`)
- Modify: `ProjectSYNCS/Services/PlynlingService.cs` (`AdoptAsync`)
- Modify: `ProjectSYNCS/Commands/PlynlingModule.cs` (`AdoptAsync`, one line)
- Modify: `ProjectSYNCS/Helpers/PlynlingArt.cs` (`Key`)
- Create: `ProjectSYNCS/Migrations/<timestamp>_AddPlynlingGender.cs` (+ Designer, snapshot) via `dotnet ef`
- Test: `$SCRATCH/plynlingcheck`, `$SCRATCH/plynlingdb`, `$SCRATCH/artcheck`, `$SCRATCH/plynlingui`, `$SCRATCH/graveyardcheck`

**Interfaces:**
- Produces:
  - `enum PlynlingGender { Male, Female }` (namespace `ProjectSYNCS.Models`); `Plynling.Gender`
  - `PlynlingSpecies` += `Tournesol, Citron, Roux, Ivoire, Nocturne, Solaire`
  - `enum PlynlingFamily { Mushroom, Sunflower }` with `[ChoiceDisplay("Champignon")]` / `[ChoiceDisplay("Tournesol")]` (namespace `ProjectSYNCS.Helpers`)
  - `SpeciesInfo(PlynlingSpecies Species, PlynlingFamily Family, string Name, PlynlingRarity Rarity, int Weight, uint Accent)`
  - `PlynlingCatalog.InFamily(PlynlingFamily) → IEnumerable<SpeciesInfo>`, `TotalWeight(PlynlingFamily) → int`, `PickSpecies(PlynlingFamily, int roll) → PlynlingSpecies`, `RollSpecies(PlynlingFamily) → PlynlingSpecies`, `FamilyOf(PlynlingSpecies) → PlynlingFamily`, `RollGender() → PlynlingGender`
  - `PlynlingLife.Create(ulong guildId, ulong ownerId, string name, PlynlingSpecies species, PlynlingGender gender, DateTimeOffset now)`
  - `PlynlingService.AdoptAsync(ulong guildId, ulong ownerId, string name, PlynlingSpecies species, DateTimeOffset now, PlynlingGender? gender = null)` — rolls the gender when `null`
  - `PlynlingArt.Key` keys: `tournesol`, `tournesol_citron`, `tournesol_roux`, `tournesol_ivoire`, `tournesol_nocturne`, `tournesol_solaire`; throws `ArgumentOutOfRangeException` on an unknown species

- [ ] **Step 1: Extend the harnesses (they must fail to compile)**

In `$SCRATCH/plynlingcheck/Program.cs`:

Replace `Plynling New() => PlynlingLife.Create(1, 2, "Rex", PlynlingSpecies.Amanite, t0);` with:

```csharp
Plynling New() => PlynlingLife.Create(1, 2, "Rex", PlynlingSpecies.Amanite, PlynlingGender.Male, t0);
```

Replace the three rarity checks — `Check("weights total 300", PlynlingCatalog.TotalWeight == 300);`, the `PickSpecies(roll) == sp` loop line, and `Check("commons are 70% together", …)` — so they read:

```csharp
Check("weights total 300", PlynlingCatalog.TotalWeight(PlynlingFamily.Mushroom) == 300);
foreach (var (roll, sp) in new[] { (0, PlynlingSpecies.Amanite), (69, PlynlingSpecies.Amanite), (70, PlynlingSpecies.Cepe),
         (139, PlynlingSpecies.Cepe), (140, PlynlingSpecies.Rose), (209, PlynlingSpecies.Rose), (210, PlynlingSpecies.Russule),
         (263, PlynlingSpecies.Russule), (264, PlynlingSpecies.Mystique), (290, PlynlingSpecies.Mystique),
         (291, PlynlingSpecies.Dore), (299, PlynlingSpecies.Dore) })
    Check($"roll {roll} -> {sp}", PlynlingCatalog.PickSpecies(PlynlingFamily.Mushroom, roll) == sp);
Check("commons are 70% together", PlynlingCatalog.InFamily(PlynlingFamily.Mushroom).Where(x => x.Rarity == PlynlingRarity.Common).Sum(x => x.Weight) == 210);
```

Insert before `// --- the food table ---`:

```csharp
// --- families ---
static string? ChoiceLabel(Enum value) =>
    value.GetType().GetField(value.ToString())!
        .GetCustomAttributes(typeof(Discord.Interactions.ChoiceDisplayAttribute), false)
        .Cast<Discord.Interactions.ChoiceDisplayAttribute>().FirstOrDefault()?.Name;
foreach (var family in Enum.GetValues<PlynlingFamily>())
{
    var members = PlynlingCatalog.InFamily(family).ToList();
    Check($"{family}: six species", members.Count == 6);
    Check($"{family}: the same odds ladder as the mushrooms",
        members.Select(s => (s.Rarity, s.Weight)).SequenceEqual(
            PlynlingCatalog.InFamily(PlynlingFamily.Mushroom).Select(s => (s.Rarity, s.Weight))));
    Check($"{family}: every roll stays in the family",
        Enumerable.Range(0, PlynlingCatalog.TotalWeight(family))
            .All(roll => PlynlingCatalog.FamilyOf(PlynlingCatalog.PickSpecies(family, roll)) == family));
}
foreach (var (roll, sp) in new[] { (0, PlynlingSpecies.Tournesol), (70, PlynlingSpecies.Citron), (140, PlynlingSpecies.Roux),
         (210, PlynlingSpecies.Ivoire), (264, PlynlingSpecies.Nocturne), (291, PlynlingSpecies.Solaire), (299, PlynlingSpecies.Solaire) })
    Check($"sunflower roll {roll} -> {sp}", PlynlingCatalog.PickSpecies(PlynlingFamily.Sunflower, roll) == sp);
Check("every species is in the catalog exactly once",
    Enum.GetValues<PlynlingSpecies>().All(s => PlynlingCatalog.Species.Count(x => x.Species == s) == 1));
Check("family choice labels are French",
    ChoiceLabel(PlynlingFamily.Mushroom) == "Champignon" && ChoiceLabel(PlynlingFamily.Sunflower) == "Tournesol");

// --- gender ---
var girl = PlynlingLife.Create(1, 2, "Lili", PlynlingSpecies.Tournesol, PlynlingGender.Female, t0);
Check("Create keeps the gender", girl.Gender == PlynlingGender.Female);
PlynlingLife.Settle(girl, t0 + TimeSpan.FromDays(10));
PlynlingLife.Resurrect(girl, t0 + TimeSpan.FromDays(11));
Check("resurrection keeps the gender", girl.DiedAt is null && girl.Gender == PlynlingGender.Female);
var rolledGenders = Enumerable.Range(0, 2000).Select(_ => PlynlingCatalog.RollGender()).ToList();
Check("the gender roll is roughly even", rolledGenders.Count(g => g == PlynlingGender.Female) is > 850 and < 1150);
Check("Male is 0, the column default", (int)PlynlingGender.Male == 0);
```

In `$SCRATCH/plynlingdb/Program.cs`, replace `db.Plynlings.Add(PlynlingLife.Create(1, 10, "Sneaky", PlynlingSpecies.Rose, now));` with:

```csharp
        db.Plynlings.Add(PlynlingLife.Create(1, 10, "Sneaky", PlynlingSpecies.Rose, PlynlingGender.Male, now));
```

and insert after `Check("adopt after a death", o4 == AdoptOutcome.Adopted && p4 is not null);`:

```csharp

    // --- gender and family persist; the roll yields both genders ---
    var (og, girl) = await new PlynlingService(Ctx()).AdoptAsync(7, 1, "Lili", PlynlingSpecies.Nocturne, now, PlynlingGender.Female);
    Check("adopt with an explicit gender", og == AdoptOutcome.Adopted && girl is not null);
    using (var db = Ctx())
    {
        var stored = db.Plynlings.Single(x => x.Id == girl!.Id);
        Check("gender round-trips", stored.Gender == PlynlingGender.Female);
        Check("a sunflower species round-trips", PlynlingCatalog.FamilyOf(stored.Species) == PlynlingFamily.Sunflower);
    }
    var genders = new List<PlynlingGender>();
    for (ulong owner = 1000; owner < 1060; owner++)
    {
        var (_, one) = await new PlynlingService(Ctx()).AdoptAsync(7, owner, "Roll", PlynlingCatalog.RollSpecies(PlynlingFamily.Sunflower), now);
        genders.Add(one!.Gender);
        Check("a rolled sunflower is a sunflower", PlynlingCatalog.FamilyOf(one.Species) == PlynlingFamily.Sunflower);
    }
    Check("sixty adoptions yield both genders", genders.Distinct().Count() == 2);
```

In `$SCRATCH/plynlingui/Program.cs`, replace `PlynlingLife.Create(1, 42, name, PlynlingSpecies.Amanite, t0)` with `PlynlingLife.Create(1, 42, name, PlynlingSpecies.Amanite, PlynlingGender.Male, t0)`.

In `$SCRATCH/graveyardcheck/Program.cs`, replace `(PlynlingSpecies)(i % 6), t0 + TimeSpan.FromDays(100 - i * 5)` with `(PlynlingSpecies)(i % 6), i % 2 == 0 ? PlynlingGender.Male : PlynlingGender.Female, t0 + TimeSpan.FromDays(100 - i * 5)`.

Replace the whole of `$SCRATCH/artcheck/Program.cs` with:

```csharp
using System.Text.RegularExpressions;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

// Every URL the bot can build must point at a file that exists, at 256x256, and no file
// may exist that nothing links to.
var dir = @"C:\Users\c235773\Desktop\Sources - RME\Discord Bots\SessionOrganizer\assets\plynlings";
var commonPy = @"C:\Users\c235773\Desktop\Sources - RME\Discord Bots\SessionOrganizer\tools\plynling-art\common.py";
int pass = 0, fail = 0;
void Check(string label, bool ok) { if (ok) pass++; else { fail++; Console.WriteLine("  FAIL  " + label); } }

// Families whose art has been exported. Sunflower joins in Task 5; until then its species
// are expected to have no files, and are left out rather than reported missing.
var shipped = new HashSet<PlynlingFamily> { PlynlingFamily.Mushroom };
var species = Enum.GetValues<PlynlingSpecies>().Where(s => shipped.Contains(PlynlingCatalog.FamilyOf(s))).ToList();

var urls = new List<string>();
foreach (var s in species)
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
Check($"{urls.Count} URLs", urls.Count == 70);

// Key parity with the art scripts: each shipped species' key must be one common.py defines.
var scriptKeys = Regex.Matches(File.ReadAllText(commonPy), "^    \"([a-z_]+)\":\\s*dict\\(", RegexOptions.Multiline)
    .Select(m => m.Groups[1].Value).ToHashSet();
foreach (var s in species)
    Check($"{s}: key '{PlynlingArt.Key(s)}' is defined in common.py", scriptKeys.Contains(PlynlingArt.Key(s)));
var all = Enum.GetValues<PlynlingSpecies>();
Check("every species has its own key", all.Select(PlynlingArt.Key).Distinct().Count() == all.Length);
var threw = false;
try { PlynlingArt.Key((PlynlingSpecies)999); } catch (ArgumentOutOfRangeException) { threw = true; }
Check("an unknown species throws instead of borrowing another's art", threw);

Console.WriteLine($"{pass} passed, {fail} failed");
if (fail > 0) Environment.Exit(1);
```

- [ ] **Step 2: Run them — expect compile failures**

Run: `dotnet run --project "$SCRATCH/plynlingcheck"`
Expected: build errors (`PlynlingGender`, `PlynlingFamily`, `InFamily` … do not exist).

- [ ] **Step 3: `ProjectSYNCS/Models/Plynling.cs`**

Replace `public enum PlynlingSpecies { Amanite, Cepe, Rose, Russule, Mystique, Dore }` with:

```csharp
// Stored as an int, so this is **append-only**: a new species goes at the end, never in
// between, or every existing row silently changes species. Which family a species belongs
// to is PlynlingCatalog's business, not a column.
public enum PlynlingSpecies
{
    Amanite, Cepe, Rose, Russule, Mystique, Dore,                   // mushrooms
    Tournesol, Citron, Roux, Ivoire, Nocturne, Solaire,             // sunflowers
}

// Rolled 50/50 at adoption and never changed. Male is 0 so the column's default is a real
// value for the rows that predate it.
public enum PlynlingGender { Male, Female }
```

Replace `    public PlynlingSpecies Species { get; set; }` with:

```csharp
    public PlynlingSpecies Species { get; set; }
    // Decides the French: a female one is "une Plynling", "gelée", "morte".
    public PlynlingGender Gender { get; set; }
```

- [ ] **Step 4: `ProjectSYNCS/Helpers/PlynlingCatalog.cs`**

Insert after the closing `}` of `enum PlynlingFood`:

```csharp

// What /plynling adopt offers. Not stored: a Plynling's family is its species' family, so
// there is nothing to keep in sync. A new family is a value here, its species appended to
// PlynlingSpecies, their rows below, and their art — no new text, since every line is
// family-neutral.
public enum PlynlingFamily
{
    [ChoiceDisplay("Champignon")] Mushroom,
    [ChoiceDisplay("Tournesol")] Sunflower,
}
```

Replace the `SpeciesInfo` record with:

```csharp
public sealed record SpeciesInfo(PlynlingSpecies Species, PlynlingFamily Family, string Name, PlynlingRarity Rarity, int Weight, uint Accent);
```

Replace the comment and array of `Species` (from `    // Weights out of 300:` through its closing `    };`) with:

```csharp
    // Weights out of 300 *per family*: each common exactly 70/300, so the three commons are
    // 70% together; 18% peu commun, 9% rare, 3% légendaire. Every family uses this ladder.
    public static readonly IReadOnlyList<SpeciesInfo> Species = new[]
    {
        new SpeciesInfo(PlynlingSpecies.Amanite,   PlynlingFamily.Mushroom,  "Amanite",            PlynlingRarity.Common,    70, 0xCE323A),
        new SpeciesInfo(PlynlingSpecies.Cepe,      PlynlingFamily.Mushroom,  "Cèpe",               PlynlingRarity.Common,    70, 0x98623A),
        new SpeciesInfo(PlynlingSpecies.Rose,      PlynlingFamily.Mushroom,  "Rosé des prés",      PlynlingRarity.Common,    70, 0xEC929E),
        new SpeciesInfo(PlynlingSpecies.Russule,   PlynlingFamily.Mushroom,  "Russule verte",      PlynlingRarity.Uncommon,  54, 0x62AA58),
        new SpeciesInfo(PlynlingSpecies.Mystique,  PlynlingFamily.Mushroom,  "Mystique",           PlynlingRarity.Rare,      27, 0x7E52CC),
        new SpeciesInfo(PlynlingSpecies.Dore,      PlynlingFamily.Mushroom,  "Doré",               PlynlingRarity.Legendary,  9, 0xE0AA2A),
        new SpeciesInfo(PlynlingSpecies.Tournesol, PlynlingFamily.Sunflower, "Tournesol",          PlynlingRarity.Common,    70, 0xECB018),
        new SpeciesInfo(PlynlingSpecies.Citron,    PlynlingFamily.Sunflower, "Tournesol citron",   PlynlingRarity.Common,    70, 0xE8DA64),
        new SpeciesInfo(PlynlingSpecies.Roux,      PlynlingFamily.Sunflower, "Tournesol roux",     PlynlingRarity.Common,    70, 0xC4522A),
        new SpeciesInfo(PlynlingSpecies.Ivoire,    PlynlingFamily.Sunflower, "Tournesol ivoire",   PlynlingRarity.Uncommon,  54, 0xE2D8BE),
        new SpeciesInfo(PlynlingSpecies.Nocturne,  PlynlingFamily.Sunflower, "Tournesol nocturne", PlynlingRarity.Rare,      27, 0x701E3A),
        new SpeciesInfo(PlynlingSpecies.Solaire,   PlynlingFamily.Sunflower, "Tournesol solaire",  PlynlingRarity.Legendary,  9, 0xF4B424),
    };
```

Replace the block from `    public static int TotalWeight => Species.Sum(s => s.Weight);` through `    public static PlynlingSpecies RollSpecies() => PickSpecies(Random.Shared.Next(TotalWeight));` with:

```csharp
    public static IEnumerable<SpeciesInfo> InFamily(PlynlingFamily family) => Species.Where(s => s.Family == family);

    public static PlynlingFamily FamilyOf(PlynlingSpecies species) => Info(species).Family;

    public static int TotalWeight(PlynlingFamily family) => InFamily(family).Sum(s => s.Weight);

    // Pure given the roll, so the odds are checkable without randomness. Only the family's
    // own species take part: a sunflower adoption can never land on a mushroom.
    public static PlynlingSpecies PickSpecies(PlynlingFamily family, int roll)
    {
        foreach (var s in InFamily(family))
        {
            if (roll < s.Weight) return s.Species;
            roll -= s.Weight;
        }
        throw new ArgumentOutOfRangeException(nameof(roll));
    }

    public static PlynlingSpecies RollSpecies(PlynlingFamily family) =>
        PickSpecies(family, Random.Shared.Next(TotalWeight(family)));

    public static PlynlingGender RollGender() =>
        Random.Shared.Next(2) == 0 ? PlynlingGender.Male : PlynlingGender.Female;
```

- [ ] **Step 5: `PlynlingLife.Create`, `PlynlingService.AdoptAsync`, the adopt command**

In `ProjectSYNCS/Helpers/PlynlingLife.cs`, replace
`    public static Plynling Create(ulong guildId, ulong ownerId, string name, PlynlingSpecies species, DateTimeOffset now) => new()`
with
`    public static Plynling Create(ulong guildId, ulong ownerId, string name, PlynlingSpecies species, PlynlingGender gender, DateTimeOffset now) => new()`
and replace `        Species = species,` with:

```csharp
        Species = species,
        Gender = gender,
```

In `ProjectSYNCS/Services/PlynlingService.cs`, replace:

```csharp
        ulong guildId, ulong ownerId, string name, PlynlingSpecies species, DateTimeOffset now)
    {
        var current = await GetCurrentAsync(guildId, ownerId, now);
        if (current is { DiedAt: null }) return (AdoptOutcome.AlreadyHasOne, current);

        var plynling = PlynlingLife.Create(guildId, ownerId, name, species, now);
```

with:

```csharp
        ulong guildId, ulong ownerId, string name, PlynlingSpecies species, DateTimeOffset now,
        PlynlingGender? gender = null)
    {
        var current = await GetCurrentAsync(guildId, ownerId, now);
        if (current is { DiedAt: null }) return (AdoptOutcome.AlreadyHasOne, current);

        // Rolled here unless given, so PlynlingLife.Create stays pure and the harnesses
        // can pin a gender.
        var plynling = PlynlingLife.Create(guildId, ownerId, name, species, gender ?? PlynlingCatalog.RollGender(), now);
```

In `ProjectSYNCS/Commands/PlynlingModule.cs`, replace `        var species = PlynlingCatalog.RollSpecies();` with:

```csharp
        // Only mushrooms are adoptable for now: the sunflower species exist in the catalog but
        // stay dormant until families ship (the plan's deferred Task 3 adds the family: option).
        var species = PlynlingCatalog.RollSpecies(PlynlingFamily.Mushroom);
```

- [ ] **Step 6: `PlynlingArt.Key` becomes exhaustive**

In `ProjectSYNCS/Helpers/PlynlingArt.cs`, replace the whole `Key` method (from `    // Must match the SPECIES keys` to its closing `    };`) with:

```csharp
    // Must match the SPECIES / SUNFLOWERS keys in tools/plynling-art/common.py. Exhaustive
    // on purpose: it used to end in `_ => "dore"`, so a species added without a key would
    // silently have worn a Doré's pictures. Now it throws, and artcheck catches it.
    public static string Key(PlynlingSpecies species) => species switch
    {
        PlynlingSpecies.Amanite => "amanite",
        PlynlingSpecies.Cepe => "cepe",
        PlynlingSpecies.Rose => "rose",
        PlynlingSpecies.Russule => "russule",
        PlynlingSpecies.Mystique => "mystique",
        PlynlingSpecies.Dore => "dore",
        PlynlingSpecies.Tournesol => "tournesol",
        PlynlingSpecies.Citron => "tournesol_citron",
        PlynlingSpecies.Roux => "tournesol_roux",
        PlynlingSpecies.Ivoire => "tournesol_ivoire",
        PlynlingSpecies.Nocturne => "tournesol_nocturne",
        PlynlingSpecies.Solaire => "tournesol_solaire",
        _ => throw new ArgumentOutOfRangeException(nameof(species), species, "No art key for this species."),
    };
```

- [ ] **Step 7: The migration**

Run: `cd ProjectSYNCS && dotnet ef migrations add AddPlynlingGender`
Then open the generated `Migrations/*_AddPlynlingGender.cs`. Its `Up` must be exactly one `AddColumn<int>(name: "Gender", table: "Plynlings", type: "INTEGER", nullable: false, defaultValue: 0)` and its `Down` one `DropColumn`. Anything else means the model drifted — stop and investigate.

- [ ] **Step 8: Build and run**

Run: `dotnet build ProjectSYNCS/ProjectSYNCS.csproj -warnaserror` → 0 errors, 0 warnings.
Run: `plynlingcheck`, `plynlingdb`, `plynlingui`, `graveyardcheck`, `artcheck`, `economycheck`, `modulecheck` → all `0 failed` / `OK`.

- [ ] **Step 9: Mutation tests**

1. In `PickSpecies`, iterate `Species` instead of `InFamily(family)` → `plynlingcheck` must fail ("every roll stays in the family", the sunflower rolls). Restore.
2. In `Key`, change the throwing arm to `_ => "dore"` → `artcheck` must fail ("an unknown species throws"). Restore.
Confirm both files are byte-identical to before (hash) and rebuild.

- [ ] **Step 10: Checkpoint** — report and stop. No commit.

---

### Task 2: Every Plynling line in both genders

**Files:**
- Create: `ProjectSYNCS/Services/GenderedLines.cs`
- Create: `ProjectSYNCS/Helpers/PlynlingGrammar.cs`
- Modify: `ProjectSYNCS/Services/BotResponses.cs` (the ten Plynling pools + table of contents)
- Modify: `ProjectSYNCS/Helpers/PlynlingText.cs` (whole file)
- Modify: `ProjectSYNCS/Helpers/PlynlingCardUi.cs`
- Modify: `ProjectSYNCS/Services/PlynlingCareService.cs`, `ProjectSYNCS/Commands/PlynlingModule.cs`, `ProjectSYNCS/Services/PlynlingAnnouncer.cs` (call sites)
- Test: `$SCRATCH/plynlingui`

The call sites change in this task, not a later one: switching the pools' type breaks every one of them, and the build must stay green at every checkpoint.

**Interfaces:**
- Consumes: `PlynlingGender`, `Plynling.Gender` (Task 1).
- Produces:
  - `public sealed record GenderedLines(string[] M, string[] F)` with `string[] For(PlynlingGender)` (namespace `ProjectSYNCS.Services`)
  - `PlynlingGrammar.Agree(this PlynlingGender, string masculine, string feminine)`, `PlynlingGrammar.Symbol(this PlynlingGender)` → `"♂"` / `"♀"`
  - `BotResponses.PlynlingAdoptLines`, `PlynlingAdoptRareLines`, `PlynlingFeedLines`, `PlynlingPetLines`, `PlynlingDeathLines`, `PlynlingResurrectLines`, `PlynlingWarningLines`, `PlynlingStaffFreezeDms`, `PlynlingStaffThawDms`, `PlynlingStaffRenameDms` — all `GenderedLines`
  - `PlynlingText.NotYours/Dead/Frozen/Wasted/AlreadyFrozen/NotFrozen/TooHungryToFreeze/ThawStaffOnly(PlynlingGender)`, `FreezeCooldown(PlynlingGender, DateTimeOffset)`, `FrozenNotice(PlynlingGender, string, DateTimeOffset?)`, `ThawedNotice(PlynlingGender, string)`, `PettedBy(PlynlingGender, ulong)`; `PetCooldown` stays a gender-free `const`
  - `PlynlingCardUi.MoodLabel(PlynlingMood, PlynlingGender)`; `PlynlingCareService.Refusal(CareOutcome, PlynlingGender)`

- [ ] **Step 1: Rewrite the pool checks in `$SCRATCH/plynlingui/Program.cs`**

Replace everything from the line `// --- pools ---` down to (not including) `Console.WriteLine($"{pass} passed, {fail} failed");` with:

```csharp
// --- pools: every GenderedLines field, both halves ---
var responses = typeof(ProjectSYNCS.Services.XpService).Assembly.GetType("ProjectSYNCS.Services.BotResponses")!;
var toc = File.ReadAllText(@"C:\Users\c235773\Desktop\Sources - RME\Discord Bots\SessionOrganizer\ProjectSYNCS\Services\BotResponses.cs").Split("internal static class BotResponses")[0];
ProjectSYNCS.Services.GenderedLines Lines(string n) =>
    (ProjectSYNCS.Services.GenderedLines)responses.GetField(n, BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!;
var arity = new Dictionary<string, int>
{
    ["PlynlingAdoptLines"] = 2, ["PlynlingAdoptRareLines"] = 3, ["PlynlingFeedLines"] = 2, ["PlynlingPetLines"] = 1,
    ["PlynlingDeathLines"] = 4, ["PlynlingResurrectLines"] = 2, ["PlynlingWarningLines"] = 2,
    ["PlynlingStaffFreezeDms"] = 1, ["PlynlingStaffThawDms"] = 1, ["PlynlingStaffRenameDms"] = 2,
};
var pools = responses.GetFields(BindingFlags.Public | BindingFlags.Static)
    .Where(f => f.FieldType == typeof(ProjectSYNCS.Services.GenderedLines)).Select(f => f.Name).ToList();
Check("every gendered pool is registered here, and only those", pools.Count == arity.Count && pools.All(arity.ContainsKey));
// A crude guard against a line pasted into the wrong half: whole words, case-insensitive.
// The allow-list is for words that are legitimately there — "la mort" is a noun.
var bans = new Dictionary<PlynlingGender, string[]>
{
    [PlynlingGender.Female] = new[] { @"\bil\b", @"-le\b", @"\bmort\b" },
    [PlynlingGender.Male] = new[] { @"\belle\b", @"-la\b", @"\bmorte\b" },
};
string[] allowed = { "la mort" };
foreach (var name in pools)
{
    Check($"{name} is in the table of contents", toc.Contains(name));
    foreach (var g in new[] { PlynlingGender.Male, PlynlingGender.Female })
    {
        var half = Lines(name).For(g);
        Check($"{name}.{g} has at least 3 lines", half.Length >= 3);
        foreach (var line in half)
        {
            var fmtArgs = Enumerable.Range(0, arity[name]).Select(i => (object)$"ARG{i}").ToArray();   // not "args": top-level programs have one
            try { string.Format(line, fmtArgs); Check("format-safe", true); } catch (FormatException) { Check($"format-safe: {line}", false); }
            Check($"{name}.{g}: no placeholder beyond {{{arity[name] - 1}}}", !line.Contains($"{{{arity[name]}}}"));
            var scrubbed = allowed.Aggregate(line, (s, a) => s.Replace(a, "", StringComparison.OrdinalIgnoreCase));
            foreach (var ban in bans[g])
                Check($"{name}.{g} grammar ({ban}): {line}",
                    !System.Text.RegularExpressions.Regex.IsMatch(scrubbed, ban, System.Text.RegularExpressions.RegexOptions.IgnoreCase));
        }
    }
}
foreach (var name in new[] { "PlynlingAdoptLines", "PlynlingAdoptRareLines" })
{
    Check($"{name}: every boy's line says garçon", Lines(name).M.All(l => l.Contains("garçon")));
    Check($"{name}: every girl's line says fille", Lines(name).F.All(l => l.Contains("fille")));
}
Check("game channel id", ProjectSYNCS.Services.PlynlingAnnouncer.GameChannelId == 878305034432045080UL);
Check("warning lines read with a relative timestamp",
    Lines("PlynlingWarningLines").M.Concat(Lines("PlynlingWarningLines").F).All(l => !l.Contains("jusqu'à {1}") && !l.Contains("que {1}")));

// --- the card and fixed text agree with a girl ---
Plynling Girl(string name) { var p = PlynlingLife.Create(1, 42, name, PlynlingSpecies.Tournesol, PlynlingGender.Female, t0); p.Id = 8; return p; }
var lili = Girl("Lili");
var boyHead = PlynlingCardUi.Heading(alive, t0);
var girlHead = PlynlingCardUi.Heading(lili, t0);
Check("a boy's heading: ♂ and âgé", boyHead.Contains("♂") && boyHead.Contains("âgé de"));
Check("a girl's heading: ♀ and âgée", girlHead.Contains("♀") && girlHead.Contains("âgée de"));
foreach (var (mood, m, f) in new[] { (PlynlingMood.Content, "content", "contente"), (PlynlingMood.Happy, "heureux", "heureuse"),
         (PlynlingMood.Sad, "triste", "triste"), (PlynlingMood.Hungry, "affamé", "affamée"),
         (PlynlingMood.Starving, "mourant de faim", "mourante de faim"), (PlynlingMood.Frozen, "gelé", "gelée") })
    Check($"mood {mood}: {m} / {f}", PlynlingCardUi.MoodLabel(mood, PlynlingGender.Male) == m && PlynlingCardUi.MoodLabel(mood, PlynlingGender.Female) == f);
var frozenGirl = Girl("Glaçon"); PlynlingLife.Freeze(frozenGirl, t0, byStaff: false);
Check("a frozen girl is Gelée", PlynlingCardUi.Status(frozenGirl, t0).Contains("Gelée"));
var deadGirl = Girl("Adieu"); PlynlingLife.Settle(deadGirl, t0 + TimeSpan.FromDays(10));
var deadStatus = PlynlingCardUi.Status(deadGirl, t0 + TimeSpan.FromDays(10));
Check("a dead girl is Morte, and Elle repose", deadStatus.Contains("Morte") && deadStatus.Contains("Elle repose"));
var graveLine = PlynlingCardUi.GraveLine(deadGirl, t0 + TimeSpan.FromDays(10));
Check("a girl's grave line: ♀ and morte", graveLine.Contains("♀") && graveLine.Contains("morte <t:"));
var F = PlynlingGender.Female;
Check("NotYours(F)", PlynlingText.NotYours(F).Contains("ta Plynling") && PlynlingText.NotYours(F).Contains("la nourrir"));
Check("Dead(F)", PlynlingText.Dead(F).StartsWith("Cette Plynling"));
Check("Frozen(F)", PlynlingText.Frozen(F).Contains("est gelée") && PlynlingText.Frozen(F).Contains("elle n'est pas dégelée"));
Check("Wasted(F)", PlynlingText.Wasted(F).StartsWith("Elle"));
Check("AlreadyFrozen(F) / NotFrozen(F)", PlynlingText.AlreadyFrozen(F) == "Elle est déjà gelée." && PlynlingText.NotFrozen(F) == "Elle n'est pas gelée.");
Check("TooHungryToFreeze(F)", PlynlingText.TooHungryToFreeze(F).Contains("Nourris-la"));
Check("ThawStaffOnly(F)", PlynlingText.ThawStaffOnly(F).Contains("l'a gelée"));
Check("FreezeCooldown(F)", PlynlingText.FreezeCooldown(F, t0).Contains("dégelée"));
Check("FrozenNotice(F)", PlynlingText.FrozenNotice(F, "Lili", t0).Contains("est gelée") && PlynlingText.FrozenNotice(F, "Lili", null).Contains("est gelée"));
Check("ThawedNotice(F)", PlynlingText.ThawedNotice(F, "Lili").Contains("est dégelée"));
Check("PettedBy(F)", PlynlingText.PettedBy(F, 1) == "caressée par <@1>" && PlynlingText.PettedBy(PlynlingGender.Male, 1) == "caressé par <@1>");
Check("the boy's versions stay masculine", PlynlingText.Dead(PlynlingGender.Male).StartsWith("Ce Plynling") && PlynlingText.Wasted(PlynlingGender.Male).StartsWith("Il"));
Check("the pet cooldown names no Plynling, so needs no gender",
    !System.Text.RegularExpressions.Regex.IsMatch(PlynlingText.PetCooldown, @"\b(il|elle|l'as|caressée?)\b"));

```

- [ ] **Step 2: Run it — expect compile failures** (`GenderedLines`, `MoodLabel(…, gender)`, `PlynlingText.Dead(g)` … do not exist).

- [ ] **Step 3: Create `ProjectSYNCS/Services/GenderedLines.cs`**

```csharp
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Services;

// A Plynling pool in both genders. Every line about one specific Plynling lives in one of
// these, so a call site cannot pick a line without saying whose — `.For(p.Gender)` — and a
// female Plynling can never be handed a masculine line through a forgotten switch.
//
// The halves are ordinary arrays, so ResponsePicker is unchanged. A half may repeat a line
// from the other when there is nothing in it to agree ("**{0}** frétille de bonheur ✨").
public sealed record GenderedLines(string[] M, string[] F)
{
    public string[] For(PlynlingGender gender) => gender == PlynlingGender.Female ? F : M;
}
```

- [ ] **Step 4: Create `ProjectSYNCS/Helpers/PlynlingGrammar.cs`**

```csharp
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

// Agreement for the short fixed words about one Plynling — "âgé/âgée", "gelé/gelée",
// "Mort/Morte". Anything long enough to need rewording goes in a GenderedLines pool
// instead; this is for single words, where two spellings side by side stay readable.
public static class PlynlingGrammar
{
    public static string Agree(this PlynlingGender gender, string masculine, string feminine) =>
        gender == PlynlingGender.Female ? feminine : masculine;

    public static string Symbol(this PlynlingGender gender) => gender.Agree("♂", "♀");
}
```

- [ ] **Step 5: The ten pools in `ProjectSYNCS/Services/BotResponses.cs`**

Replace everything from the line `    // A new Plynling, announced on the card. {0} = its name (sanitised), {1} = species.` through the closing `    };` of `PlynlingStaffRenameDms` with:

```csharp
    // ---- Plynlings ----------------------------------------------------------------------
    // Every pool here is a GenderedLines: M for a boy, F for a girl, picked with
    // .For(p.Gender). A girl is "une Plynling" — the noun follows the creature. Lines are
    // family-neutral: nothing a mushroom has and a sunflower lacks (no cap, no petals).

    // A new Plynling, announced on the card. {0} = its name (sanitised), {1} = species.
    // Every line says whether it is a boy or a girl.
    public static readonly GenderedLines PlynlingAdoptLines = new(
        M: new[]
        {
            "Un nouveau Plynling pointe le bout de son nez : **{0}**, espèce {1}. C'est un garçon ! Nourris-le bien (˶ᵔ ᵕ ᵔ˶)",
            "**{0}** vient de sortir de terre ! Un Plynling {1}, tout frais tout mignon — et c'est un garçon ✨",
            "Félicitations, c'est un garçon ! **{0}** ({1}) te regarde déjà avec des yeux affamés.",
            "Un Plynling de plus dans le monde : **{0}**, {1}. C'est un petit garçon. Promets-moi de ne pas l'oublier.",
        },
        F: new[]
        {
            "Une nouvelle Plynling pointe le bout de son nez : **{0}**, espèce {1}. C'est une fille ! Nourris-la bien (˶ᵔ ᵕ ᵔ˶)",
            "**{0}** vient de sortir de terre ! Une Plynling {1}, toute fraîche toute mignonne — et c'est une fille ✨",
            "Félicitations, c'est une fille ! **{0}** ({1}) te regarde déjà avec des yeux affamés.",
            "Une Plynling de plus dans le monde : **{0}**, {1}. C'est une petite fille. Promets-moi de ne pas l'oublier.",
        });

    // The same moment for a rare or legendary pull, which is worth making a fuss about.
    // {0} = name, {1} = species, {2} = rarity label ("rare" / "légendaire": both invariant).
    public static readonly GenderedLines PlynlingAdoptRareLines = new(
        M: new[]
        {
            "QUOI ?! Un Plynling **{2}** ! **{0}**, espèce {1}, et c'est un garçon… tu as une chance insolente ✨✨",
            "Je n'en crois pas mes capteurs : **{0}**, un garçon, espèce {1}. C'est **{2}**, ça. Garde-le en vie, par pitié.",
            "Alerte rareté : **{0}** ({1}, *{2}*) vient de naître. C'est un garçon, et tout le serveur va être jaloux ദ്ദി◝ ⩊ ◜.ᐟ",
        },
        F: new[]
        {
            "QUOI ?! Une Plynling **{2}** ! **{0}**, espèce {1}, et c'est une fille… tu as une chance insolente ✨✨",
            "Je n'en crois pas mes capteurs : **{0}**, une fille, espèce {1}. C'est **{2}**, ça. Garde-la en vie, par pitié.",
            "Alerte rareté : **{0}** ({1}, *{2}*) vient de naître. C'est une fille, et tout le serveur va être jaloux ദ്ദി◝ ⩊ ◜.ᐟ",
        });

    // Shown on the card after a meal. {0} = name, {1} = the food with its article.
    public static readonly GenderedLines PlynlingFeedLines = new(
        M: new[]
        {
            "Tu donnes {1} à **{0}**. Il n'en fait qu'une bouchée (˶˃ ᵕ ˂˶)",
            "**{0}** a dévoré {1}. Il te regarde comme si tu étais la meilleure personne du monde.",
            "Miam ! {1} pour **{0}**, qui fait une petite danse de joie ✨",
            "**{0}** grignote {1} avec une concentration impressionnante.",
        },
        F: new[]
        {
            "Tu donnes {1} à **{0}**. Elle n'en fait qu'une bouchée (˶˃ ᵕ ˂˶)",
            "**{0}** a dévoré {1}. Elle te regarde comme si tu étais la meilleure personne du monde.",
            "Miam ! {1} pour **{0}**, qui fait une petite danse de joie ✨",
            "**{0}** grignote {1} avec une concentration impressionnante.",
        });

    // Shown on the card after a pet. {0} = name.
    public static readonly GenderedLines PlynlingPetLines = new(
        M: new[]
        {
            "**{0}** ronronne. Oui, les Plynlings ronronnent, ne pose pas de questions.",
            "**{0}** ferme les yeux et savoure la caresse (˶ᵔ ᵕ ᵔ˶)",
            "**{0}** frétille de bonheur ✨",
            "**{0}** se blottit contre ta main. C'est officiel, c'est ton meilleur ami.",
            "**{0}** fait un petit bruit satisfait. Encore, encore !",
        },
        F: new[]
        {
            "**{0}** ronronne. Oui, les Plynlings ronronnent, ne pose pas de questions.",
            "**{0}** ferme les yeux et savoure la caresse (˶ᵔ ᵕ ᵔ˶)",
            "**{0}** frétille de bonheur ✨",
            "**{0}** se blottit contre ta main. C'est officiel, c'est ta meilleure amie.",
            "**{0}** fait un petit bruit satisfait. Encore, encore !",
        });

    // Posted publicly in the game channel, with the memorial as the picture.
    // {0} = name, {1} = owner mention (sent with pings off), {2} = time lived, {3} = memorial.
    public static readonly GenderedLines PlynlingDeathLines = new(
        M: new[]
        {
            "🪦 **{0}**, le Plynling de {1}, s'est éteint après {2} de vie. Il repose désormais sous {3}.",
            "🪦 Un Plynling de moins sur cette terre… **{0}** ({1}) nous a quittés après {2}. On lui a dressé {3}.",
            "🪦 Minute de silence pour **{0}**, compagnon de {1} pendant {2}. Il dort sous {3}.",
            "🪦 **{0}** n'a pas survécu à la faim. {2} de vie, et maintenant {3}. {1}, il t'attendait…",
        },
        F: new[]
        {
            "🪦 **{0}**, la Plynling de {1}, s'est éteinte après {2} de vie. Elle repose désormais sous {3}.",
            "🪦 Une Plynling de moins sur cette terre… **{0}** ({1}) nous a quittés après {2}. On lui a dressé {3}.",
            "🪦 Minute de silence pour **{0}**, compagne de {1} pendant {2}. Elle dort sous {3}.",
            "🪦 **{0}** n'a pas survécu à la faim. {2} de vie, et maintenant {3}. {1}, elle t'attendait…",
        });

    // Posted publicly when staff bring one back. {0} = name, {1} = owner mention.
    public static readonly GenderedLines PlynlingResurrectLines = new(
        M: new[]
        {
            "✨ **{0}** est revenu d'entre les morts ! {1}, c'est ta deuxième chance. Ne la gâche pas.",
            "✨ La terre tremble… **{0}** ressort du cimetière, un peu poussiéreux mais bien vivant. Bon retour, {1} !",
            "✨ Miracle ! **{0}** respire à nouveau. {1}, nourris-le vite, il a une faim de mort-vivant.",
        },
        F: new[]
        {
            "✨ **{0}** est revenue d'entre les morts ! {1}, c'est ta deuxième chance. Ne la gâche pas.",
            "✨ La terre tremble… **{0}** ressort du cimetière, un peu poussiéreuse mais bien vivante. Bon retour, {1} !",
            "✨ Miracle ! **{0}** respire à nouveau. {1}, nourris-la vite, elle a une faim de morte-vivante.",
        });

    // The single DM about six hours before death. {0} = name, {1} = a relative Discord
    // timestamp ("dans 6 heures") — so every line must read with "dans …" in that slot.
    public static readonly GenderedLines PlynlingWarningLines = new(
        M: new[]
        {
            "⚠️ **{0}** a terriblement faim… il mourra {1} si personne ne le nourrit. `/plynling feed`, vite !",
            "⚠️ Ton Plynling **{0}** va mourir de faim {1}. Il compte sur toi.",
            "⚠️ Psst… **{0}** est au bord de l'évanouissement. Il s'effondrera {1}. Ne l'abandonne pas (╥﹏╥)",
        },
        F: new[]
        {
            "⚠️ **{0}** a terriblement faim… elle mourra {1} si personne ne la nourrit. `/plynling feed`, vite !",
            "⚠️ Ta Plynling **{0}** va mourir de faim {1}. Elle compte sur toi.",
            "⚠️ Psst… **{0}** est au bord de l'évanouissement. Elle s'effondrera {1}. Ne l'abandonne pas (╥﹏╥)",
        });

    // DMs to an owner when staff act on their Plynling, so it never looks like a bug.
    public static readonly GenderedLines PlynlingStaffFreezeDms = new(
        M: new[]
        {
            "❄️ Le staff a gelé ton Plynling **{0}**. Rien ne bouge tant qu'il n'est pas dégelé — il ne risque rien.",
            "❄️ **{0}** a été mis au frais par le staff. Il t'attendra, bien au froid.",
            "❄️ Pause forcée pour **{0}** : le staff l'a gelé. Pas de faim, pas de soucis, juste une longue sieste.",
        },
        F: new[]
        {
            "❄️ Le staff a gelé ta Plynling **{0}**. Rien ne bouge tant qu'elle n'est pas dégelée — elle ne risque rien.",
            "❄️ **{0}** a été mise au frais par le staff. Elle t'attendra, bien au froid.",
            "❄️ Pause forcée pour **{0}** : le staff l'a gelée. Pas de faim, pas de soucis, juste une longue sieste.",
        });

    public static readonly GenderedLines PlynlingStaffThawDms = new(
        M: new[]
        {
            "🌱 Le staff a dégelé **{0}**. La faim reprend son cours : pense à le nourrir !",
            "🌱 **{0}** se réveille, dégelé par le staff. Il a déjà un petit creux.",
            "🌱 Fin de la sieste pour **{0}** : le staff l'a dégelé. Son estomac s'en souvient déjà.",
        },
        F: new[]
        {
            "🌱 Le staff a dégelé **{0}**. La faim reprend son cours : pense à la nourrir !",
            "🌱 **{0}** se réveille, dégelée par le staff. Elle a déjà un petit creux.",
            "🌱 Fin de la sieste pour **{0}** : le staff l'a dégelée. Son estomac s'en souvient déjà.",
        });

    // {0} = old name, {1} = new name.
    public static readonly GenderedLines PlynlingStaffRenameDms = new(
        M: new[]
        {
            "✏️ Le staff a renommé ton Plynling **{0}** en **{1}**.",
            "✏️ Petit changement d'identité : **{0}** s'appelle désormais **{1}** (décision du staff).",
            "✏️ Ton Plynling répond maintenant au nom de **{1}** — le staff a jugé que **{0}** ne lui allait plus.",
        },
        F: new[]
        {
            "✏️ Le staff a renommé ta Plynling **{0}** en **{1}**.",
            "✏️ Petit changement d'identité : **{0}** s'appelle désormais **{1}** (décision du staff).",
            "✏️ Ta Plynling répond maintenant au nom de **{1}** — le staff a jugé que **{0}** ne lui allait plus.",
        });
```

In the table of contents, replace the line `//   Plynlings` with:

```
//   Plynlings — every pool is a GenderedLines (M/F halves), picked with .For(p.Gender)
```

- [ ] **Step 6: Replace the whole of `ProjectSYNCS/Helpers/PlynlingText.cs`**

```csharp
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

// Fixed French lines for refusals and plain notices. Deliberately not ResponsePicker
// pools: the pools exist so repeated *chatter* doesn't repeat, and a refusal is not
// chatter — varied, it would read as scripted.
//
// A line about one specific Plynling takes its gender ("ta Plynling", "gelée"); a line
// about the *person* — they have none, they already have one — stays in the generic
// masculine, since no particular Plynling is meant.
public static class PlynlingText
{
    public static string WorkCooldown(DateTimeOffset next) =>
        $"Tu as déjà travaillé. Prochain service <t:{next.ToUnixTimeSeconds()}:R>.";

    public const string NoPlynling = "Tu n'as pas de Plynling. `/plynling adopt` pour en adopter un !";
    public const string AlreadyHasOne = "Tu as déjà un Plynling. Un seul à la fois !";
    public const string EmptyName = "Il lui faut un vrai nom.";
    public const string Unknown = "Ce bouton ne correspond plus à rien.";
    public const string StaffOnly = "Cette action est réservée au staff.";
    public const string NoGrave = "Personne à ressusciter : cette personne n'a aucun Plynling au cimetière.";
    public const string ResurrectBlocked = "Cette personne a déjà un Plynling vivant — un seul à la fois.";
    // Refused before the Plynling is even loaded, so it cannot know the gender: worded to
    // need none.
    public const string PetCooldown = "Une caresse toutes les 4 heures, pas plus. Reviens un peu plus tard.";

    public static string NoneFor(ulong userId) => $"<@{userId}> n'a pas de Plynling.";

    public static string TooPoor(long price, long balance) =>
        $"Il te faut {PebbleEconomy.Cailloux(price)}, tu n'en as que {balance}. `/work` pour en gagner.";

    // "seul son propriétaire": the owner's gender is not known, so that part stays generic.
    public static string NotYours(PlynlingGender g) =>
        $"Ce n'est pas {g.Agree("ton", "ta")} Plynling — seul son propriétaire peut {g.Agree("le", "la")} nourrir.";

    public static string Dead(PlynlingGender g) => $"{g.Agree("Ce", "Cette")} Plynling n'est plus de ce monde… 🪦";

    public static string Frozen(PlynlingGender g) => g.Agree(
        "Ce Plynling est gelé : rien ne bouge tant qu'il n'est pas dégelé.",
        "Cette Plynling est gelée : rien ne bouge tant qu'elle n'est pas dégelée.");

    public static string Wasted(PlynlingGender g) =>
        $"{g.Agree("Il", "Elle")} n'a besoin de rien de tout ça pour l'instant — garde tes cailloux.";

    public static string AlreadyFrozen(PlynlingGender g) => g.Agree("Il est déjà gelé.", "Elle est déjà gelée.");

    public static string NotFrozen(PlynlingGender g) => g.Agree("Il n'est pas gelé.", "Elle n'est pas gelée.");

    public static string TooHungryToFreeze(PlynlingGender g) => g.Agree(
        "Trop tard pour le geler : il a déjà trop faim (moins de 50 %). Nourris-le d'abord.",
        "Trop tard pour la geler : elle a déjà trop faim (moins de 50 %). Nourris-la d'abord.");

    public static string ThawStaffOnly(PlynlingGender g) => g.Agree(
        "C'est le staff qui l'a gelé : seul le staff peut le dégeler.",
        "C'est le staff qui l'a gelée : seul le staff peut la dégeler.");

    public static string FreezeCooldown(PlynlingGender g, DateTimeOffset next) =>
        $"Tu l'as {g.Agree("dégelé", "dégelée")} il y a moins de 7 jours. Prochain gel possible <t:{next.ToUnixTimeSeconds()}:R>.";

    // "…Notice", not "Frozen"/"Thawed": those names are the refusals above.
    public static string FrozenNotice(PlynlingGender g, string name, DateTimeOffset? until) => until is { } u
        ? $"❄️ **{name}** est {g.Agree("gelé", "gelée")} jusqu'au <t:{u.ToUnixTimeSeconds()}:f>. Rien ne bouge d'ici là."
        : $"❄️ **{name}** est {g.Agree("gelé", "gelée")} jusqu'à nouvel ordre du staff.";

    public static string ThawedNotice(PlynlingGender g, string name) =>
        $"🌱 **{name}** est {g.Agree("dégelé", "dégelée")}. La faim reprend son cours !";

    // Appended to the pet line on the card: "— caressée par @quelqu'un".
    public static string PettedBy(PlynlingGender g, ulong petterId) => $"{g.Agree("caressé", "caressée")} par <@{petterId}>";
}
```

- [ ] **Step 7: `ProjectSYNCS/Helpers/PlynlingCardUi.cs`**

In `Heading`, replace:

```csharp
        return $"## {SafeName(p.Name)}\n" +
               $"{info.Name} · *{PlynlingCatalog.RarityLabel(info.Rarity)}*\n" +
               $"à <@{p.OwnerId}> · âgé de {age}";
```

with:

```csharp
        return $"## {SafeName(p.Name)} {p.Gender.Symbol()}\n" +
               $"{info.Name} · *{PlynlingCatalog.RarityLabel(info.Rarity)}*\n" +
               $"à <@{p.OwnerId}> · {p.Gender.Agree("âgé", "âgée")} de {age}";
```

In `Status`, replace:

```csharp
            return $"🪦 Mort <t:{died.ToUnixTimeSeconds()}:R>, après {LevelCardUi.Duration((long)lived.TotalMinutes)} de vie. " +
                   $"Il repose sous {memorial}.";
```

with:

```csharp
            return $"🪦 {p.Gender.Agree("Mort", "Morte")} <t:{died.ToUnixTimeSeconds()}:R>, après {LevelCardUi.Duration((long)lived.TotalMinutes)} de vie. " +
                   $"{p.Gender.Agree("Il", "Elle")} repose sous {memorial}.";
```

replace:

```csharp
                ? $"❄️ Gelé jusqu'au <t:{until.ToUnixTimeSeconds()}:f>"
                : "❄️ Gelé par le staff"
```

with:

```csharp
                ? $"❄️ {p.Gender.Agree("Gelé", "Gelée")} jusqu'au <t:{until.ToUnixTimeSeconds()}:f>"
                : $"❄️ {p.Gender.Agree("Gelé", "Gelée")} par le staff"
```

replace `$"**Humeur** · *{MoodLabel(PlynlingLife.Mood(p, now))}*";` with `$"**Humeur** · *{MoodLabel(PlynlingLife.Mood(p, now), p.Gender)}*";`, and replace the whole `MoodLabel` method with:

```csharp
    public static string MoodLabel(PlynlingMood mood, PlynlingGender gender) => mood switch
    {
        PlynlingMood.Happy => gender.Agree("heureux", "heureuse"),
        PlynlingMood.Sad => "triste",
        PlynlingMood.Hungry => gender.Agree("affamé", "affamée"),
        PlynlingMood.Starving => gender.Agree("mourant de faim", "mourante de faim"),
        PlynlingMood.Frozen => gender.Agree("gelé", "gelée"),
        _ => gender.Agree("content", "contente"),
    };
```

In `GraveLine`, replace:

```csharp
        return $"**{SafeName(p.Name)}** · {info.Name} — à <@{p.OwnerId}>\n" +
               $"*a vécu {lived}* · mort <t:{p.DiedAt!.Value.ToUnixTimeSeconds()}:R>";
```

with:

```csharp
        return $"**{SafeName(p.Name)}** {p.Gender.Symbol()} · {info.Name} — à <@{p.OwnerId}>\n" +
               $"*a vécu {lived}* · {p.Gender.Agree("mort", "morte")} <t:{p.DiedAt!.Value.ToUnixTimeSeconds()}:R>";
```

- [ ] **Step 8: The call sites**

`ProjectSYNCS/Services/PlynlingCareService.cs`:
- `            return new CareReply(null, Refusal(outcome));` → `            return new CareReply(null, Refusal(outcome, plynling?.Gender ?? PlynlingGender.Male));`
- `        var line = string.Format(_picker.Pick(channelId, BotResponses.PlynlingPetLines), PlynlingCardUi.SafeName(plynling.Name));` → `        var line = string.Format(_picker.Pick(channelId, BotResponses.PlynlingPetLines.For(plynling.Gender)), PlynlingCardUi.SafeName(plynling.Name));`
- `$"{line} — caressé par <@{actorId}>"` → `$"{line} — {PlynlingText.PettedBy(plynling.Gender, actorId)}"`
- `                : Refusal(result.Outcome));` → `                : Refusal(result.Outcome, result.Plynling?.Gender ?? PlynlingGender.Male));`
- `        var line = string.Format(_picker.Pick(channelId, BotResponses.PlynlingFeedLines),` → `        var line = string.Format(_picker.Pick(channelId, BotResponses.PlynlingFeedLines.For(result.Plynling.Gender)),`
- Replace the `Refusal` method with:

```csharp
    // Every refusal but NoPlynling comes back with the Plynling loaded; NoPlynling's line
    // names no Plynling, so the fallback gender never shows.
    public static string Refusal(CareOutcome outcome, PlynlingGender gender) => outcome switch
    {
        CareOutcome.NoPlynling => PlynlingText.NoPlynling,
        CareOutcome.NotOwner => PlynlingText.NotYours(gender),
        CareOutcome.Dead => PlynlingText.Dead(gender),
        CareOutcome.Frozen => PlynlingText.Frozen(gender),
        CareOutcome.Wasted => PlynlingText.Wasted(gender),
        _ => PlynlingText.Unknown,
    };
```

`ProjectSYNCS/Commands/PlynlingModule.cs`:
- In `AdoptAsync`, replace `        var pool = info.Rarity >= PlynlingRarity.Rare ? BotResponses.PlynlingAdoptRareLines : BotResponses.PlynlingAdoptLines;` with `        var pool = (info.Rarity >= PlynlingRarity.Rare ? BotResponses.PlynlingAdoptRareLines : BotResponses.PlynlingAdoptLines).For(plynling.Gender);`
- In `FreezeAsync`, replace:

```csharp
        if (outcome != FreezeOutcome.Frozen || plynling is null)
        {
            await RespondAsync(outcome switch
```

with:

```csharp
        if (outcome != FreezeOutcome.Frozen || plynling is null)
        {
            // Every refusal but NoPlynling carries the Plynling, so its gender is known.
            var g = plynling?.Gender ?? PlynlingGender.Male;
            await RespondAsync(outcome switch
```

  and in that switch: `FreezeOutcome.Dead => PlynlingText.Dead,` → `FreezeOutcome.Dead => PlynlingText.Dead(g),`; `FreezeOutcome.AlreadyFrozen => PlynlingText.AlreadyFrozen,` → `FreezeOutcome.AlreadyFrozen => PlynlingText.AlreadyFrozen(g),`; `FreezeOutcome.TooHungry => PlynlingText.TooHungryToFreeze,` → `FreezeOutcome.TooHungry => PlynlingText.TooHungryToFreeze(g),`; `PlynlingText.FreezeCooldown(plynling!.LastSelfThawAt` → `PlynlingText.FreezeCooldown(g, plynling!.LastSelfThawAt`.
- `PlynlingText.FrozenNotice(PlynlingCardUi.SafeName(plynling.Name), plynling.FreezeUntil)` → `PlynlingText.FrozenNotice(plynling.Gender, PlynlingCardUi.SafeName(plynling.Name), plynling.FreezeUntil)`
- `BotResponses.PlynlingStaffFreezeDms)` → `BotResponses.PlynlingStaffFreezeDms.For(plynling.Gender))`
- In `ThawAsync`, replace:

```csharp
        if (outcome != ThawOutcome.Thawed || plynling is null)
        {
            await RespondAsync(outcome switch
```

with:

```csharp
        if (outcome != ThawOutcome.Thawed || plynling is null)
        {
            var g = plynling?.Gender ?? PlynlingGender.Male;
            await RespondAsync(outcome switch
```

  and: `ThawOutcome.Dead => PlynlingText.Dead,` → `ThawOutcome.Dead => PlynlingText.Dead(g),`; `ThawOutcome.NotFrozen => PlynlingText.NotFrozen,` → `ThawOutcome.NotFrozen => PlynlingText.NotFrozen(g),`; `ThawOutcome.StaffOnly => PlynlingText.ThawStaffOnly,` → `ThawOutcome.StaffOnly => PlynlingText.ThawStaffOnly(g),`.
- `PlynlingText.ThawedNotice(PlynlingCardUi.SafeName(plynling.Name))` → `PlynlingText.ThawedNotice(plynling.Gender, PlynlingCardUi.SafeName(plynling.Name))`
- `BotResponses.PlynlingStaffThawDms)` → `BotResponses.PlynlingStaffThawDms.For(plynling.Gender))`
- `BotResponses.PlynlingStaffRenameDms)` → `BotResponses.PlynlingStaffRenameDms.For(plynling.Gender))`

`ProjectSYNCS/Services/PlynlingAnnouncer.cs`:
- `BotResponses.PlynlingDeathLines)` → `BotResponses.PlynlingDeathLines.For(plynling.Gender))`
- `BotResponses.PlynlingResurrectLines)` → `BotResponses.PlynlingResurrectLines.For(plynling.Gender))`
- `BotResponses.PlynlingWarningLines)` → `BotResponses.PlynlingWarningLines.For(plynling.Gender))`

Then confirm nothing still reads a pool without `.For`: `grep -rn "BotResponses\.Plynling" ProjectSYNCS --include=*.cs | grep -v "\.For("` → only `BotResponses.cs` itself (and the `AdoptAsync` ternary, whose `.For` sits after the parenthesis).

- [ ] **Step 9: Build and run**

Run: build `-warnaserror` → clean. `plynlingui` → `0 failed`; every other harness still `0 failed` / `OK`.

- [ ] **Step 10: Mutation tests**

1. In `PlynlingFeedLines`' `F` half, change `Elle n'en fait qu'une bouchée` to `Il n'en fait qu'une bouchée` → `plynlingui` must fail the grammar guard. Restore.
2. In `GenderedLines.For`, return `M` always → the adopt "fille" checks must fail. Restore.
Confirm byte-identical restores; rebuild.

- [ ] **Step 11: Checkpoint** — report and stop. No commit.

---

### Task 3: `family:` on `/plynling adopt` — DEFERRED

> **Deferred** by the scope amendment: sunflowers ship with families, later. Kept as
> written so it can be picked up unchanged; do not execute it now.

**Files:**
- Modify: `ProjectSYNCS/Commands/PlynlingModule.cs` (`AdoptAsync`)
- Test: `$SCRATCH/modulecheck`

**Interfaces:**
- Consumes: `PlynlingFamily`, `PlynlingCatalog.RollSpecies(PlynlingFamily)` (Task 1).
- Produces: `/plynling adopt name: family:` — `family` required, a `PlynlingFamily` choice list.

- [ ] **Step 1: Extend `$SCRATCH/modulecheck/Program.cs`** — insert before `var names = interactions.SlashCommands`:

```csharp
var adopt = interactions.SlashCommands.Single(c => c.Name == "adopt" && c.Module.SlashGroupName == "plynling");
var familyOption = adopt.Parameters.SingleOrDefault(p => p.Name == "family");
Check("/plynling adopt has a family option", familyOption is not null);
Check("family is required", familyOption?.IsRequired == true);
Check("family is a PlynlingFamily choice list", familyOption?.ParameterType == typeof(ProjectSYNCS.Helpers.PlynlingFamily));
```

- [ ] **Step 2: Run it — expect `3 FAILED`** (the option does not exist yet).

- [ ] **Step 3: Add the option.** In `ProjectSYNCS/Commands/PlynlingModule.cs`, replace:

```csharp
        [Summary("name", "Son nom")] [MaxLength(InputCaps.PlynlingName)] string name)
```

with:

```csharp
        [Summary("name", "Son nom")] [MaxLength(InputCaps.PlynlingName)] string name,
        [Summary("family", "Champignon ou tournesol : l'espèce est tirée au sort dans la famille")] PlynlingFamily family)
```

and replace the three lines Task 1 left there (the two-line comment and `        var species = PlynlingCatalog.RollSpecies(PlynlingFamily.Mushroom);`) with:

```csharp
        var species = PlynlingCatalog.RollSpecies(family);
```

- [ ] **Step 4: Build and run** — build clean; `modulecheck` → `OK`, still 42 commands; all other harnesses `0 failed`.

- [ ] **Step 5: Mutation test** — give the parameter a default (`PlynlingFamily family = PlynlingFamily.Mushroom`) → `modulecheck` must fail "family is required". Restore byte-identical; rebuild.

- [ ] **Step 6: Checkpoint** — report and stop. No commit. From here a sunflower can be adopted in the dev guild, but its pictures do not exist until Task 5.

---

### Task 4: The sunflower art — code and a draft for review — DEFERRED

> **Deferred** by the scope amendment: sunflowers ship with families, later. Kept as
> written so it can be picked up unchanged; do not execute it now.

**Files:**
- Modify: `tools/plynling-art/common.py` (the `SUNFLOWERS` table)
- Modify: `tools/plynling-art/sprites.py` (`face` gets an origin and a skin)
- Create: `tools/plynling-art/sunflowers.py`
- Modify: `tools/plynling-art/memorials.py` (family-aware)
- Test: `$SCRATCH/artproto/determinism.py` (the 70 mushroom PNGs stay byte-identical)

**Interfaces:**
- Produces:
  - `common.SUNFLOWERS[key]` = `dict(tier, petal=[5 tones], disc=[4 tones], sparkle)` for the six keys of Task 1
  - `sprites.face(g, state, f, ox=0, oy=0, skin=None, skin_out=None)` — defaults reproduce the mushroom face exactly
  - `sunflowers.build(state, sp, frame=0, shadow=True) → Image` (32×32 RGBA) — the **potted** design chosen at the concept review: the head with its face on the disc, a short stem with two leaves, a terracotta pot; `python sunflowers.py <out_dir>` writes the review sheets
  - `memorials.memorial(tier, sp)` accepts sunflower keys; `memorials.palette(sp)`

- [ ] **Step 1: Record that the export is deterministic before touching anything**

`$SCRATCH/artproto/determinism.py` (already present) re-renders the 70 files into `$SCRATCH/artproto/render` and compares them with `assets/plynlings/`.
Run: `python "$SCRATCH/artproto/determinism.py"` → `70 identical, 0 different`.

- [ ] **Step 2: `common.py` — the palette table.** Insert before the line `STEM = [(255, 252, 242), (250, 240, 220), (238, 222, 196), (212, 190, 162), (178, 154, 126)]`:

```python
# The sunflower family. petal: five tones, lit to deep. disc: four tones for the face's
# disc, lit to rim — kept light at the centre whatever the variant, because the face is
# drawn in INK on it and a dark disc would swallow the eyes. Keys must match
# PlynlingArt.Key in the bot.
SUNFLOWERS = {
    "tournesol":          dict(tier="commun", petal=[(255, 238, 140), (252, 214, 64), (236, 176, 24), (190, 128, 20), (128, 82, 18)],
                               disc=[(226, 178, 116), (198, 144, 86), (164, 110, 62), (116, 76, 44)], sparkle=None),
    "tournesol_citron":   dict(tier="commun", petal=[(255, 252, 200), (250, 240, 150), (232, 218, 100), (190, 176, 70), (132, 120, 46)],
                               disc=[(226, 178, 116), (198, 144, 86), (164, 110, 62), (116, 76, 44)], sparkle=None),
    "tournesol_roux":     dict(tier="commun", petal=[(252, 186, 120), (232, 126, 64), (196, 82, 42), (148, 52, 32), (96, 32, 26)],
                               disc=[(226, 178, 116), (198, 144, 86), (164, 110, 62), (116, 76, 44)], sparkle=None),
    "tournesol_ivoire":   dict(tier="peu commun", petal=[(255, 255, 250), (246, 240, 222), (226, 216, 190), (184, 172, 146), (128, 118, 100)],
                               disc=[(214, 164, 108), (180, 124, 72), (136, 88, 50), (84, 54, 34)], sparkle=None),
    "tournesol_nocturne": dict(tier="rare", petal=[(196, 96, 120), (152, 52, 82), (112, 30, 58), (76, 20, 42), (46, 12, 28)],
                               disc=[(214, 164, 108), (180, 124, 72), (136, 88, 50), (84, 54, 34)], sparkle=(255, 190, 210)),
    "tournesol_solaire":  dict(tier="légendaire", petal=[(255, 250, 196), (255, 222, 96), (244, 180, 36), (196, 124, 20), (132, 78, 14)],
                               disc=[(255, 226, 150), (240, 188, 96), (204, 146, 64), (148, 98, 40)], sparkle=(255, 250, 205)),
}
```

- [ ] **Step 3: `sprites.py` — the face takes an origin and a skin**

Replace:

```python
# ---- the face -------------------------------------------------------------------------
# Eyes sit at columns 12-13 and 18-19, rows 19-21; the mouth at rows 23-25.

def face(g, state, f):
    def P(x, y, c):
        g.put(x, y + f, c)
```

with:

```python
# ---- the face -------------------------------------------------------------------------
# Eyes sit at columns 12-13 and 18-19, rows 19-21; the mouth at rows 23-25 — before the
# (ox, oy) offset. Every family wears this same face: a sunflower draws it on its disc,
# seven rows higher, with the disc's tones as its skin (the eye bags and the brows).

def face(g, state, f, ox=0, oy=0, skin=None, skin_out=None):
    skin = STEM if skin is None else skin
    skin_out = STEM_OUT if skin_out is None else skin_out

    def P(x, y, c):
        g.put(x + ox, y + f + oy, c)
```

then inside `face`: in `tired_eye`, the two `STEM[4]` become `skin[4]`; in `brows`, `P(x, 17, STEM_OUT)` becomes `P(x, 17, skin_out)` and `INK if inner_raise else STEM_OUT` becomes `INK if inner_raise else skin_out`.

Run: `python "$SCRATCH/artproto/determinism.py"` → `70 identical, 0 different`. The defaults must reproduce the mushroom face pixel for pixel.

- [ ] **Step 4: Create `tools/plynling-art/sunflowers.py`** (LF line endings)

```python
"""The sunflower Plynling: a flower head with its face on the disc, on a short stem growing out
of a little terracotta pot, two leaves for arms. The moods live in the petals — spread wide
when happy, sagging with sadness and hunger, browned and falling when starving, huddled
when frozen — and in the leaves, which lift or droop. The face is sprites.face, shared with
the mushrooms, moved up onto the disc."""
import math
import os
import sys

from PIL import Image

from common import N, Grid, lerp, INK, SUNFLOWERS, sheet
from sprites import face, extras, PALE

GREEN = [(178, 226, 128), (132, 196, 92), (92, 158, 68), (62, 118, 54), (38, 80, 40)]
GREEN_OUT = (32, 62, 36)
POT = [(236, 146, 104), (206, 112, 74), (170, 84, 56), (124, 58, 42), (80, 38, 30)]
HEAD_X, HEAD_Y = 15.5, 10.5    # the flower head's centre
DISC_R = 6.8
RING, PETAL_R, PETALS = 7.6, 2.9, 10
FACE_OY = -8                   # the mushroom face's rows, moved up onto the disc


def mood(state):
    """(droop of the lower petals in pixels, petal size, leaf lift in rows: + up, - down)."""
    return {"happy": (0.0, 1.08, 2), "content": (0.4, 1.0, 1), "sad": (1.4, 0.96, 0),
            "hungry": (2.2, 0.94, -1), "starving": (3.2, 0.88, -2), "frozen": (0.6, 0.86, 0)}[state]


def pot(g):
    """A terracotta pot: a lit rim, then a body tapering towards its foot."""
    for y in range(22, 30):
        if y <= 23:
            x0, x1 = 9, 22
        else:
            k = y - 24
            x0, x1 = 10 + k // 3, 21 - k // 3
        for x in range(x0, x1 + 1):
            t = (x + 0.5 - x0) / (x1 + 1 - x0)
            i = 0 if (y == 22 and t < 0.5) else (1 if t < 0.35 else (2 if t < 0.75 else 3))
            if y == 23:
                i = min(4, i + 1)                        # the shadow under the rim
            g.put(x, y, POT[i], "pot")


def stem(g):
    for y in range(17, 22):
        for x in (14, 15, 16, 17):
            g.put(x, y, GREEN[1] if x < 16 else GREEN[2], "stem")


def leaf(g, x0, y0, side, lift):
    """A pointed leaf off the stem, midrib darker, tipped up (lift > 0) or drooping."""
    for t in range(6):
        cx = x0 + side * t
        cy = y0 - (lift * t) / 5.0
        half = [1, 1.6, 1.9, 1.7, 1.1, 0.5][t]
        for dy in range(-2, 3):
            if abs(dy) <= half:
                c = GREEN[1] if dy < 0 else (GREEN[2] if dy > 0 else GREEN[3])
                g.put(int(round(cx)), int(round(cy + dy)), c, "leaf")


def petals(g, pal, state):
    """Fat round petals on a ring, top ones first so the lower ones overlap them. Each is lit
    on its top-left with a darker lower-right rim, so neighbours stay distinct."""
    droop, scale, _ = mood(state)
    pr = PETAL_R * scale
    missing = {3, 7} if state == "starving" else set()
    for i in sorted(range(PETALS), key=lambda i: math.sin(2 * math.pi * i / PETALS - math.pi / 2)):
        if i in missing:
            continue
        a = 2 * math.pi * i / PETALS - math.pi / 2
        low = max(0.0, math.sin(a))                    # 0 at the top, 1 at the bottom
        px = HEAD_X + math.cos(a) * RING + math.cos(a) * droop * 0.25 * low
        py = HEAD_Y + math.sin(a) * RING + droop * (0.35 + low)
        ex = pr * (1.0 + 0.25 * abs(math.cos(a)))       # stretched outward a little
        ey = pr * (1.0 + 0.25 * abs(math.sin(a)))
        for y in range(N):
            for x in range(N):
                dx, dy = (x + 0.5 - px) / ex, (y + 0.5 - py) / ey
                d = dx * dx + dy * dy
                if d > 1:
                    continue
                if d > 0.62 and dx + dy > 0.35:
                    c = pal[3]
                elif dx + dy < -0.6:
                    c = pal[0]
                elif d > 0.5 and dx + dy > 0:
                    c = pal[2]
                else:
                    c = pal[1]
                if state == "starving" and d > 0.3:
                    c = lerp(c, (150, 104, 56), 0.45)   # browned
                g.put(x, y, c, "petal")


def disc(g, pal):
    """The face's disc: a round highlight top-left, seeds dithered round the rim."""
    for y in range(N):
        for x in range(N):
            nx, ny = (x + 0.5 - HEAD_X) / DISC_R, (y + 0.5 - HEAD_Y) / DISC_R
            rr = nx * nx + ny * ny
            if rr > 1:
                continue
            hx, hy = nx + 0.32, ny + 0.36
            if hx * hx + hy * hy < 0.2:
                i = 0
            elif rr > 0.66:
                i = 2 if (x + y) % 2 == 0 or rr > 0.84 else 1
            else:
                i = 1
            g.put(x, y, pal[i], "disc")


def build(state, sp, frame=0, shadow=True):
    p = SUNFLOWERS[sp]
    pet, dsc = p["petal"], p["disc"]
    _, _, lift = mood(state)
    g = Grid()
    pot(g)
    stem(g)
    leaf(g, 13, 20, -1, lift)
    leaf(g, 18, 20, 1, lift)
    petals(g, pet, state)
    disc(g, dsc)
    out = {"petal": pet[4], "disc": dsc[3], "stem": GREEN_OUT, "leaf": GREEN_OUT, "pot": POT[4]}
    g.outline(lambda reg, ny: out.get(reg, INK))
    face(g, state, 0, oy=FACE_OY, skin=[dsc[0], dsc[0], dsc[1], dsc[2], dsc[2]], skin_out=dsc[3])

    im = Image.new("RGBA", (N, N), (0, 0, 0, 0))
    if shadow:
        for x in range(9, 23):
            im.putpixel((x, 30), (0, 0, 0, 55))
    for y in range(N):
        for x in range(N):
            c = g.c[y][x]
            if c is None:
                continue
            if state == "frozen":
                c = lerp(c, (176, 226, 255), 0.45)
            elif state == "starving" and g.r[y][x] in ("stem", "leaf"):
                c = lerp(c, PALE, 0.28)                  # drained of colour
            im.putpixel((x, y), c + (255,))
    extras(im, state, p, frame)                          # the mushrooms' heart, sweat, frost, sparkle
    return im


if __name__ == "__main__":
    # Review sheets, written where asked — never into the repo.
    from memorials import memorial
    out = sys.argv[1] if len(sys.argv) > 1 else "."
    os.makedirs(out, exist_ok=True)
    states = ["happy", "content", "sad", "hungry", "starving", "frozen"]
    keys = list(SUNFLOWERS)
    print(sheet(["tournesol"], states, lambda s, st: build(st, s), ["tournesol"], states,
                os.path.join(out, "sunflower_closeup.png"), K=7))
    print(sheet(keys, states, lambda s, st: build(st, s), keys, states, os.path.join(out, "sunflowers_all.png"), K=4))
    print(sheet(["tournesol", "tournesol_nocturne"], [1, 2, 3, 4, 5], lambda s, t: memorial(t, s),
                ["tournesol", "tournesol_nocturne"],
                ["1 cairn", "2 petite stèle", "3 stèle gravée", "4 urne", "5 statue"],
                os.path.join(out, "sunflower_memorials.png"), K=5))
```

- [ ] **Step 5: `memorials.py` becomes family-aware** — all mushroom output must stay identical.

Replace:

```python
from common import (N, Grid, lerp, tone, SPECIES, SPOTS, STEM, STONE, MARBLE, GRASS, GOLD, GOLD_HI,
                   GOLD_D, lit, stone_colour, blob, up, sheet)
```

with:

```python
import math

from common import (N, Grid, lerp, tone, SPECIES, SUNFLOWERS, SPOTS, STEM, STONE, MARBLE, GRASS, GOLD,
                   GOLD_HI, GOLD_D, lit, stone_colour, blob, up, sheet)
```

Replace `def accent(sp):\n    p = SPECIES[sp]` (the first two lines of `accent`) with:

```python
def palette(sp):
    """The fields the memorials read, for either family. A sunflower's petals stand in for a
    mushroom's cap and its disc for the gills; it has no spots."""
    if sp in SPECIES:
        return SPECIES[sp]
    f = SUNFLOWERS[sp]
    return dict(tier=f["tier"], cap=f["petal"], spot=(f["petal"][0], f["petal"][1]), spots=[],
                gill=f["disc"][1], sparkle=f["sparkle"], disc=f["disc"])


def accent(sp):
    p = palette(sp)
```

Insert before the line `# ---- the marble Plynling ------------------------------------------------------------`:

```python
SUN_PETAL, SUN_HEART = (252, 214, 64), (116, 76, 44)      # the little sunflowers on sunflower graves


def sun_sprout(g, p, x0=24):
    """A sunflower seedling where a mushroom grave has a mushroom sprout."""
    pet, disc = p["cap"], p["disc"]
    for dx, dy in ((0, -1), (-1, 0), (1, 0), (0, 1), (-1, -1), (1, -1), (-1, 1), (1, 1)):
        g.put(x0 + 1 + dx, 20 + dy, pet[1] if dy <= 0 else pet[2])
    g.put(x0 + 1, 20, disc[2])
    g.put(x0, 22, LEAF[0])
    g.put(x0 + 1, 22, LEAF[1])
    g.put(x0 + 1, 23, LEAF[2])


```

Insert before the line `def statue_details(g, p):`:

```python
def sun_statue(g, p):
    """The potted sunflower Plynling in marble: the flower head, a short stem with two leaves,
    and its little pot — the living sprite's anatomy, scaled down onto the plinth."""
    M = MARBLE
    tones = [M["hi"], M["light"], M["base"], M["shadow"], M["deep"]]
    for y in range(15, 19):                                         # the pot
        x0, x1 = (11, 20) if y == 15 else (12 + (y - 16) // 2, 19 - (y - 16) // 2)
        for x in range(x0, x1 + 1):
            t = (x + 0.5 - x0) / (x1 + 1 - x0)
            i = (0 if t < 0.5 else 2) if y == 15 else (1 if t < 0.35 else (2 if t < 0.75 else 3))
            g.put(x, y, tones[i], "statue")
    for y in (12, 13, 14):                                          # the stem
        g.put(15, y, tones[1], "statue")
        g.put(16, y, tones[3], "statue")
    for x, y, i in ((13, 13, 1), (14, 13, 1), (12, 12, 0), (17, 13, 3), (18, 13, 3), (19, 12, 2)):   # leaves
        g.put(x, y, tones[i], "statue")
    cx, cy = 15.5, 6.8
    for k in sorted(range(10), key=lambda k: math.sin(2 * math.pi * k / 10 - math.pi / 2)):   # petals
        a = 2 * math.pi * k / 10 - math.pi / 2
        px, py = cx + math.cos(a) * 4.7, cy + math.sin(a) * 4.7
        for y in range(N):
            for x in range(N):
                dx, dy = (x + 0.5 - px) / 1.9, (y + 0.5 - py) / 1.9
                d = dx * dx + dy * dy
                if d <= 1:
                    i = 3 if (d > 0.55 and dx + dy > 0.3) else (0 if dx + dy < -0.6 else 1)
                    g.put(x, y, tones[i], "statue")
    for y in range(N):                                              # the disc
        for x in range(N):
            nx, ny = (x + 0.5 - cx) / 3.9, (y + 0.5 - cy) / 3.9
            if nx * nx + ny * ny <= 1:
                i = 1 if (nx + 0.3) ** 2 + (ny + 0.35) ** 2 < 0.2 else (3 if nx * nx + ny * ny > 0.7 else 2)
                g.put(x, y, tones[i], "statue")


def sun_statue_details(g, p):
    M = MARBLE
    for x, y in ((13, 7), (14, 7), (17, 7), (18, 7)):             # closed, peaceful eyes
        g.put(x, y, M["deep"])
    g.put(15, 9, M["deep"])
    g.put(16, 9, M["deep"])
    gem(g, 15, 16, p["accent"])                                  # a gem on the pot


```

In `memorial`, replace `    p = dict(SPECIES[sp], accent=accent(sp))` with:

```python
    p = dict(palette(sp), accent=accent(sp))
    sun = sp in SUNFLOWERS
    seedling = sun_sprout if sun else sprout
    # On a sunflower grave the tier-3/4 flowers are little sunflowers.
    fl = (lambda c: SUN_PETAL) if sun else (lambda c: c)
    heart = SUN_HEART if sun else HEART
```

and then, inside `memorial`:
- `        statue(g, p)` → `        (sun_statue if sun else statue)(g, p)`
- the three `        sprout(g, p, 24)` calls (tiers 1, 2 and 3) → `        seedling(g, p, 24)`
- the tier-3 carved emblem — replace

```python
        for x, y in ((12, 11), (13, 10), (14, 10), (15, 10), (16, 10), (17, 11)):
            g.put(x, y, STONE["shadow"])                           # carved emblem
        g.put(14, 12, STONE["shadow"])
        g.put(15, 12, STONE["shadow"])
```

  with

```python
        if sun:                                                    # carved emblem: a flower
            for x, y in ((14, 9), (13, 10), (16, 10), (13, 11), (16, 11), (14, 12), (15, 9), (15, 12)):
                g.put(x, y, STONE["shadow"])
        else:                                                      # carved emblem: a cap
            for x, y in ((12, 11), (13, 10), (14, 10), (15, 10), (16, 10), (17, 11)):
                g.put(x, y, STONE["shadow"])
            g.put(14, 12, STONE["shadow"])
            g.put(15, 12, STONE["shadow"])
```

- `        bloom(g, 5, 23, PETALS[0])` → `        bloom(g, 5, 23, fl(PETALS[0]), heart)`
- the tier-4 bushes — replace

```python
        bush(g, 5.5, 23.0, 3.2, 2.3, [(4, 22, PETALS[0]), (6, 21, PETALS[1]), (7, 23, PETALS[2])])
        bush(g, 25.5, 23.0, 3.2, 2.3, [(24, 22, PETALS[3]), (26, 21, cap[1]), (27, 23, PETALS[0])])
```

  with

```python
        bush(g, 5.5, 23.0, 3.2, 2.3, [(4, 22, fl(PETALS[0])), (6, 21, fl(PETALS[1])), (7, 23, fl(PETALS[2]))])
        bush(g, 25.5, 23.0, 3.2, 2.3, [(24, 22, fl(PETALS[3])), (26, 21, cap[1]), (27, 23, fl(PETALS[0]))])
```

- `        statue_details(g, p)` → `        (sun_statue_details if sun else statue_details)(g, p)`

Run: `python "$SCRATCH/artproto/determinism.py"` → `70 identical, 0 different` (export.py is untouched, so this re-renders exactly the mushrooms through the edited code).

- [ ] **Step 6: Render the review sheets**

Run (from `tools/plynling-art`): `python sunflowers.py "$SCRATCH/art-review"`
Expected: three PNGs — `sunflower_closeup.png` (the classic Tournesol, six moods, 7×), `sunflowers_all.png` (all six variants), `sunflower_memorials.png` (tiers 1–5 for two variants). Look at them before sending.

- [ ] **Step 7: Send the sheets to the user for review, and STOP**

Send the three PNGs (SendUserFile). Ask for the art verdict: the petals per mood, the leaves (mostly hidden behind the head — keep or enlarge?), the pot, how distinct the six variants are, and the tier-5 statue (its pot tends to merge into the plinth). Do not start Task 5 until the art is approved; any requested change is made in `sunflowers.py` / `memorials.py` / `common.py`, re-checked with `determinism.py`, and re-sent.

---

### Task 5: Export the full sunflower set — DEFERRED

> **Deferred** by the scope amendment: sunflowers ship with families, later. Kept as
> written so it can be picked up unchanged; do not execute it now.

**Files:**
- Modify: `tools/plynling-art/export.py`
- Modify: `tools/plynling-art/README.md`
- Create: 66 files in `assets/plynlings/` (36 sprites + 30 memorials)
- Test: `$SCRATCH/artcheck`, a hash snapshot of the 70 mushroom files

**Interfaces:**
- Consumes: `sunflowers.build`, `memorials.memorial` for sunflower keys (Task 4, as approved).
- Produces: `assets/plynlings/plynling_tournesol*_<mood>_v1.png` and `memorial_tournesol*_<tier>_v1.png` — the URLs `PlynlingArt` builds for the six sunflower species.

- [ ] **Step 1: Snapshot the mushroom files.** Run from the repository root (this step and Step 5):

```bash
python -c "import hashlib,json,os,sys; d=sys.argv[1]; json.dump({f: hashlib.sha256(open(os.path.join(d,f),'rb').read()).hexdigest() for f in sorted(os.listdir(d))}, open(sys.argv[2],'w'))" "assets/plynlings" "$SCRATCH/mushroom_hashes.json"
```

Expected: a JSON file with 70 entries.

- [ ] **Step 2: Extend `artcheck` (it must fail).** In `$SCRATCH/artcheck/Program.cs`, replace `var shipped = new HashSet<PlynlingFamily> { PlynlingFamily.Mushroom };` with `var shipped = new HashSet<PlynlingFamily> { PlynlingFamily.Mushroom, PlynlingFamily.Sunflower };` and `Check($"{urls.Count} URLs", urls.Count == 70);` with `Check($"{urls.Count} URLs", urls.Count == 136);`, and update the comment above `shipped` to say every family's art is exported.
Run it → FAILs for the 66 missing sunflower files.

- [ ] **Step 3: `export.py` renders both families.** Replace:

```python
from common import SPECIES
from memorials import memorial
from sprites import build
```

with:

```python
import sunflowers
from common import SPECIES, SUNFLOWERS
from memorials import memorial
from sprites import build
```

and replace:

```python
    for sp in SPECIES:
        for state in STATES:
            save(build(state, sp), f"plynling_{sp}_{state}")
            count += 1
```

with:

```python
    for sp in list(SPECIES) + list(SUNFLOWERS):
        draw = build if sp in SPECIES else sunflowers.build
        for state in STATES:
            save(draw(state, sp), f"plynling_{sp}_{state}")
            count += 1
```

- [ ] **Step 4: Export.** Run (from `tools/plynling-art`): `python export.py` → `136 files written to …assets\plynlings`.

- [ ] **Step 5: Prove the mushrooms did not move.** Run:

```bash
python -c "import hashlib,json,os,sys; d=sys.argv[1]; old=json.load(open(sys.argv[2])); bad=[f for f,h in old.items() if hashlib.sha256(open(os.path.join(d,f),'rb').read()).hexdigest()!=h]; print(len(old)-len(bad),'identical,',len(bad),'changed', bad)" "assets/plynlings" "$SCRATCH/mushroom_hashes.json"
```

Expected: `70 identical, 0 changed []`.

- [ ] **Step 6: Run `artcheck`** → `0 failed` (136 URLs, every file 256×256, no orphans, every key in `common.py`).

- [ ] **Step 7: `tools/plynling-art/README.md`** — replace:

```markdown
- `sprites.py` — the living Plynling in six moods (happy, content, sad, hungry, starving, frozen).
- `memorials.py` — the five memorial tiers (cairn → statue), each carrying the species' accent colour.
- `source/` — the four food sprites (16×16, hand-drawn), exported as-is.
- `export.py` — renders all 70 files at 256×256 into `assets/plynlings/`.
```

with:

```markdown
- `sprites.py` — the mushroom Plynling in six moods (happy, content, sad, hungry, starving, frozen),
  and `face()`, the expressions every family shares.
- `sunflowers.py` — the potted sunflower Plynling: the same face on the flower's disc, a short stem
  with two leaves, a terracotta pot; moods carried by the petals and leaves. `python sunflowers.py <dir>`
  writes review sheets there.
- `memorials.py` — the five memorial tiers (cairn → statue), each carrying the species' accent
  colour; sunflower graves get sunflower seedlings, flowers and statue.
- `source/` — the four food sprites (16×16, hand-drawn), exported as-is.
- `export.py` — renders all 136 files at 256×256 into `assets/plynlings/`.

**Adding a family:** a palette table in `common.py` (keys matching `PlynlingArt.Key`), a
module drawing it with `sprites.face`, its branch in `memorials.palette`, and a loop in
`export.py`. Existing files must come out byte-identical.
```

- [ ] **Step 8: Checkpoint** — report and stop. No commit. Remind the user the new PNGs only resolve once `assets/plynlings/` is pushed.

---

### Task 6: Help, README and CLAUDE.md — gender only

Nothing here mentions sunflowers: they are dormant (see the scope amendment).

**Files:**
- Modify: `ProjectSYNCS/Commands/PlynlingModule.cs` (`BuildHelpEmbed`)
- Modify: `README.md`, `CLAUDE.md`
- Test: `$SCRATCH/helpcheck2`

- [ ] **Step 1: Extend `$SCRATCH/helpcheck2/Program.cs`** — insert before the summary line:

```csharp
Check("/plynling help mentions boy or girl", plynText.Contains("garçon ou fille", StringComparison.OrdinalIgnoreCase));
Check("/plynling help does not mention the dormant sunflowers",
    !plynText.Contains("ournesol") && !(plyn.Description ?? "").Contains("ournesol"));
Check("/help does not mention the dormant sunflowers", !mainText.Contains("ournesol"));
```

Run it → 1 FAIL ("boy or girl").

- [ ] **Step 2: `/plynling help`.** In `BuildHelpEmbed`, replace:

```csharp
                "**`/plynling adopt name:`** — Gratuit, un seul à la fois. L'espèce est tirée au sort : " +
                "commune, peu commune, rare… ou légendaire.\n" +
```

with:

```csharp
                "**`/plynling adopt name:`** — Gratuit, un seul à la fois. L'espèce est tirée au sort : " +
                "commune, peu commune, rare… ou légendaire. Garçon ou fille ? Surprise.\n" +
```

- [ ] **Step 3: Run `helpcheck2`** → `0 failed` (caps still hold); build clean; `modulecheck` OK.

- [ ] **Step 4: README.md.** Replace:

```markdown
A **Plynling** is a small mushroom creature each member can adopt — one at a time, free.
Its species is rolled: three common, one uncommon, a rare Mystique and a legendary Doré.
```

with:

```markdown
A **Plynling** is a small mushroom creature each member can adopt — one at a time, free.
Its species is rolled: three common, one uncommon, a rare Mystique and a legendary Doré.
Each one is a boy or a girl, and the bot's French follows suit: *un* or *une Plynling*.
```

- [ ] **Step 5: CLAUDE.md.** Insert after the line `break on id so the order is stable across re-renders.` (the end of the `/graveyard` note):

```markdown

**Every Plynling line exists in both genders, and the type makes that unskippable.** Each
Plynling pool in `BotResponses` is a `GenderedLines(M, F)`, so a call site cannot pick a line
without `.For(p.Gender)` — two flat arrays would have compiled fine with a forgotten switch
and shipped a boy's line to a girl. A girl is *une Plynling*: the noun follows the creature.
Text not about one specific Plynling — `/plynling help`, `/help`, the README, command
descriptions, person-level refusals like `NoPlynling` — stays in the generic masculine. Short
fixed words go through `PlynlingGrammar.Agree` ("âgé/âgée", "Gelé/Gelée"). The `plynlingui`
harness walks every `GenderedLines` field by reflection and bans `il`, `-le`, `mort` in `F`
and `elle`, `-la`, `morte` in `M` (whole words, with an allow-list for *la mort*) — crude, but
it catches the likeliest mistake, a line pasted into the wrong half. `PetCooldown` is the one
Plynling line with no gender: it is refused before the Plynling is loaded, so it is worded to
need none.

**Plynling families exist in the code and are dormant.** `PlynlingFamily`, the six sunflower
species and their catalog rows are in place, and `PlynlingCatalog` rolls only within a family
— but `/plynling adopt` always rolls a mushroom, and no sunflower art exists, so nothing can
ever show one. The rest (the `family:` option, the art, the docs) is Tasks 3–5 of
`docs/superpowers/plans/2026-09-24-plynling-gender-and-sunflowers.md`, deferred on purpose and
already tested on a scratch copy. **`PlynlingSpecies` is append-only**: it is stored as an int,
so a species inserted in the middle would silently turn every later row into its neighbour.
The family is `SpeciesInfo.Family`, not a column. `PlynlingArt.Key` is exhaustive and
**throws**: it used to end in `_ => "dore"`, which would have dressed any unkeyed species in a
Doré's pictures without a word. Every line is written family-neutral — never name a cap,
petals or spores in a Plynling line — which is why the three "chapeau" lines were rewritten.
```

- [ ] **Step 6: Final run** — build `-warnaserror` clean; every harness (`plynlingcheck`, `plynlingdb`, `plynlingui`, `artcheck`, `economycheck`, `graveyardcheck`, `helpcheck2`, `modulecheck`) → `0 failed` / `OK`.

- [ ] **Step 7: Checkpoint** — report and stop. No commit.

---

## Final verification (with the user, in the dev guild)

1. `/plynling adopt name:Lili` → a mushroom card; the heading shows ♂ or ♀, and the adoption line says *garçon* or *fille* to match.
2. The gender is random: to read both, set `Gender` (0 = boy, 1 = girl) on a row in the dev DB and `/plynling view` it again.
3. A girl's card: "âgée de", mood "contente" / "affamée", freeze → "Gelée jusqu'au …", the pet line "— caressée par …".
4. Feed and pet a girl; refusals read "Cette Plynling …", "Elle …".
5. Staff freeze/thaw/rename a girl → the DMs agree.
6. Force a death (dev DB: `NeedsAsOf` back 5 days) → the announcement agrees (prod-channel guild only); `/graveyard` shows ♂/♀ and "mort"/"morte".
7. `/plynling help` mentions *garçon ou fille*; nothing anywhere mentions sunflowers.
