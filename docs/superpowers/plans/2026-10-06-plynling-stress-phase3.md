# Plynling stress and modifiers — Phase 3 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** A CK3 stress gauge (0–400, levels at 100/200/300) that only the owner's choices fill, that care relieves, that drains happiness faster at each level and triggers a mental break on the way up; plus timed modifiers (« Inspirée », « Grognon »…) that touch stats and the tamagotchi's drain rates — all without breaking « nothing ticks ».

**Architecture:** Stress (`Plynling.Stress`) and the active modifiers (`Plynling.Modifiers`, one compact text column) live **on the Plynling row**, because `PlynlingLife` is pure and only sees the row: the drain helpers read the current segment's rates from it, and `Settle` becomes a small timeline that plays modifier expiries, mornings (sickness and stress decay) and starvation in time order, rebasing at each. Every stress or modifier change rebases first, so rates are constant between two transitions and every value stays exact. The rules are pure (`Helpers/PlynlingStress`, `Helpers/PlynlingModifiers`); the event engine gains stress, modifier and coping effects and the mental breaks.

**Tech Stack:** as phases 1–2.

**Spec:** `docs/superpowers/specs/2026-10-06-plynling-events-design.md` — Phase 3. **Depends on phases 1 and 2** being implemented (`TraitInfo` with `StressGain`/`StressLoss`, `PlynlingTraits.All`/`ByKey`, `EventDef`/`EventOption`/`EventEffect`, `PlynlingService.Events.cs` with `ApplyEventAsync`, `CreateEventAsync`, `GetEventContextAsync`, `PlynlingEventStory`, `PlynlingEventCards`, `PlynlingPersonality`). If they landed with different names, update this plan's code first.

**Two deliberate departures from the spec**, recorded in it by Task 1: modifiers are a **column on `Plynling`**, not a `PlynlingModifier` table (the pure drain maths must see them); the **pet-cooldown** modifier effect is dropped (the cooldown is an in-memory per-petter gate, shared on a public card — a per-Plynling multiplier on it buys little).

## Global Constraints

- **Never commit or push.** Each task ends by listing the files to commit.
- **Hard rule, kept:** stress is only ever **gained** from a choice the owner made (an option's stress cost, a failed challenge's `StressChange`), or inside a **mental break** — which only stress can trigger. Deciding alone never gains stress and never applies a **negative** modifier, except in a mental break. **Relief** (negative `StressChange`, care) applies always.
- **Stress:** int 0–400; levels **100 / 200 / 300**; happiness drain **×1 / ×1.15 / ×1.35 / ×1.6**; stats **0 / 0 / −1 / −2** (levels 0–3). Daily decay **−15** at the 05:00 morning × the traits' loss multiplier (cached as `StressLossBonusPercent`) × modifiers. Relief: **pet −5, game −15, visit −20, bath −3, meal 0**, changed by coping traits (Task 1 table). Stress never touches hunger or death.
- **Mental break:** crossing a level **upward** queues the break event for that level (ignores the pulse cap); every break outcome lowers stress by **60–100**; coping traits are drawn **uniformly** among those it lacks, **2 at most**.
- **Modifiers:** same key re-applied **refreshes** its end; each need's combined multiplier is clamped to **0.5–2**, and **hunger's to 0.5–1** (a modifier can only slow hunger: the death clock and the warning only get more lenient). Durations run on the wall clock, freeze included.
- **Every stress or modifier change rebases first** (`PlynlingLife`), so drain rates are constant between transitions; `Settle` plays modifier expiries in time order with mornings and starvation.
- **Append-only:** modifier keys, event and option keys, new effect types and new event fields at the end.
- Text: `docs/plynling-writing-style.md`; no « il/elle/ils/elles » in event text (harness).
- Build with `dotnet build -warnaserror`.

## Review Focus

1. **A slowing hunger modifier expiring before starvation**: death lands at the exact instant computed piecewise (slow, then normal) — not at the slow-rate instant, not at the fast-rate one. (Task 2 harness.)
2. **Stress decaying across a level boundary at a morning**: happiness before 05:00 at the old level's rate, after at the new one, exactly. (Task 2 harness, minute simulation.)
3. **A Plynling that never plays events** — over 60 simulated days of deciding alone — never gains stress and never holds a negative modifier. (Task 4 harness, simulated with the real catalog.)
4. **Frozen across a modifier's end**: the modifier is gone on thaw, no rebase happened while frozen, needs are exactly what they were at the freeze. (Task 2 harness.)
5. **The mascot**: expired modifiers drop off her row too (`Tend`), she never gets stress. (Task 2 harness.)

---

### Task 1: The pure rules — `PlynlingStress` and `PlynlingModifiers`

**Files:**
- Create: `ProjectSYNCS/Helpers/PlynlingStress.cs`
- Create: `ProjectSYNCS/Helpers/PlynlingModifiers.cs`
- Modify: `ProjectSYNCS/Models/Plynling.cs` (`Stress`, `Modifiers`, `StressLossBonusPercent`)
- Modify: `ProjectSYNCS/Helpers/PlynlingTraits.cs` (coping traits' `StressLoss`)
- Modify: `docs/superpowers/specs/2026-10-06-plynling-events-design.md` (the two departures)
- Test: `$SCRATCH/personality/Program.cs`

**Interfaces:**
- Produces:
  - `Plynling.Stress` (int), `Plynling.Modifiers` (string?, format `key:unixSeconds;key:unixSeconds`), `Plynling.StressLossBonusPercent` (int, 0 = ×1)
  - `enum CareAct { Pet, Game, Visit, Bath, Meal }`
  - `PlynlingStress`: `Max = 400`, `LevelStep = 100`, `DailyDecay = 15`, `Level(int stress) -> int` (0–3), `HappinessFactor(int level) -> double`, `StatPenalty(int level) -> int`, `GainMultiplier(IEnumerable<TraitInfo>) -> double`, `LossBonusPercent(IEnumerable<TraitInfo>) -> int`, `Scaled(int amount, IEnumerable<TraitInfo>) -> int`, `Relief(CareAct, IReadOnlyList<TraitInfo>, bool closeBond = false) -> int`, `DecayAt(Plynling) -> int`
  - `enum Need { Hunger, Happiness, Hygiene }`
  - `sealed record ModifierInfo(string Key, string NameM, string NameF, string Emoji, string Description, TimeSpan Duration, bool Negative, IReadOnlyDictionary<PlynlingStat, int> Stats, double Hunger = 1, double Happiness = 1, double Hygiene = 1, double Meal = 1, double Gift = 1, double StressDecay = 1)` with `Name(PlynlingGender)`
  - `PlynlingModifiers`: `All`, `ByKey(string) -> ModifierInfo?`, `Active(Plynling) -> IReadOnlyList<(ModifierInfo Info, DateTimeOffset Ends)>`, `Write(Plynling, IEnumerable<(string Key, DateTimeOffset Ends)>)`, `Multiplier(Plynling, Need) -> double`, `MealFactor(Plynling) -> double`, `GiftFactor(Plynling) -> double`, `StressDecayFactor(Plynling) -> double`, `StatDelta(Plynling, PlynlingStat) -> int`, `NextEnd(Plynling) -> DateTimeOffset?`

- [ ] **Step 1: Add the failing checks**

In `$SCRATCH/personality/Program.cs`, insert before the final `Console.WriteLine`:

```csharp
// ==== PHASE 3 — stress and modifiers, pure ===========================================================
Check(PlynlingStress.Level(0) == 0 && PlynlingStress.Level(99) == 0 && PlynlingStress.Level(100) == 1
      && PlynlingStress.Level(299) == 2 && PlynlingStress.Level(300) == 3 && PlynlingStress.Level(400) == 3, "levels at 100/200/300");
Check(PlynlingStress.HappinessFactor(0) == 1 && PlynlingStress.HappinessFactor(1) == 1.15 && PlynlingStress.HappinessFactor(2) == 1.35
      && PlynlingStress.HappinessFactor(3) == 1.6, "happiness drain by level");
Check(PlynlingStress.StatPenalty(1) == 0 && PlynlingStress.StatPenalty(2) == -1 && PlynlingStress.StatPenalty(3) == -2, "stat penalty by level");
TraitInfo T(string k) => PlynlingTraits.ByKey(k)!;
Check(PlynlingStress.Scaled(30, new[] { T("paranoid") }) == 60 && PlynlingStress.Scaled(30, new[] { T("arbitrary") }) == 15
      && PlynlingStress.Scaled(30, new[] { T("eccentric") }) == 45, "gain multipliers (Méfiant ×2, Capricieux ×0.5, Excentrique ×1.5)");
Check(PlynlingStress.LossBonusPercent(new[] { T("diligent") }) == -50 && PlynlingStress.LossBonusPercent(new[] { T("journaller") }) == 50
      && PlynlingStress.LossBonusPercent(Array.Empty<TraitInfo>()) == 0, "loss multipliers as a percent bonus");
var none0 = Array.Empty<TraitInfo>();
Check(PlynlingStress.Relief(CareAct.Pet, none0) == 5 && PlynlingStress.Relief(CareAct.Game, none0) == 15
      && PlynlingStress.Relief(CareAct.Visit, none0) == 20 && PlynlingStress.Relief(CareAct.Bath, none0) == 3
      && PlynlingStress.Relief(CareAct.Meal, none0) == 0, "base relief");
Check(PlynlingStress.Relief(CareAct.Meal, new[] { T("comfort_eater") }) == 10, "Mange ses émotions: meals relieve");
Check(PlynlingStress.Relief(CareAct.Game, new[] { T("athletic") }) == 30, "Sportif: games ×2");
Check(PlynlingStress.Relief(CareAct.Visit, new[] { T("confider") }, closeBond: true) == 40
      && PlynlingStress.Relief(CareAct.Visit, new[] { T("confider") }) == 20, "Confident: close visits ×2");
Check(PlynlingStress.Relief(CareAct.Visit, new[] { T("reclusive") }) == 10 && PlynlingStress.Relief(CareAct.Visit, new[] { T("improvident") }) == 30, "Reclus, Imprévoyant");
Check(PlynlingStress.Relief(CareAct.Pet, new[] { T("contrite") }) == 10 && PlynlingStress.Relief(CareAct.Pet, new[] { T("irritable") }) == 2
      && PlynlingStress.Relief(CareAct.Bath, new[] { T("profligate") }) == 10, "Repentant, Irritable, Dépensier");

// modifiers
Check(PlynlingModifiers.All.Select(m => m.Key).Distinct().Count() == PlynlingModifiers.All.Count, "modifier keys unique");
Check(PlynlingModifiers.All.All(m => m.Hunger <= 1), "no modifier speeds hunger up");
Check(PlynlingModifiers.All.All(m => m.Negative == (m.Happiness > 1 || m.Hygiene > 1 || m.Meal < 1 || m.Gift < 1 || m.StressDecay < 1 || m.Stats.Values.Any(v => v < 0))),
    "Negative is exactly « makes something worse »");
foreach (var m in PlynlingModifiers.All)
    Check(!pronoun.IsMatch(m.Description), $"modifier {m.Key}: gender-neutral description");
var pm = Fresh(60);
var end1 = noon.AddDays(2);
PlynlingModifiers.Write(pm, new[] { ("light_heart", end1), ("grumpy", noon.AddDays(1)) });
Check(PlynlingModifiers.Active(pm).Count == 2 && PlynlingModifiers.NextEnd(pm) == noon.AddDays(1), "write, read, next end");
Check(Math.Abs(PlynlingModifiers.Multiplier(pm, Need.Happiness) - 0.8 * 1.3) < 1e-9, "multipliers combine");
PlynlingModifiers.Write(pm, new[] { ("well_rested", end1) });
Check(PlynlingModifiers.Multiplier(pm, Need.Hunger) == 0.8 && PlynlingModifiers.Multiplier(pm, Need.Happiness) == 1, "hunger slowed only");
pm.Modifiers = "nonsense;inspired:abc;inspired:" + end1.ToUnixTimeSeconds();
Check(PlynlingModifiers.Active(pm).Count == 1 && PlynlingModifiers.StatDelta(pm, PlynlingStat.Learning) == 2, "malformed entries are skipped");
```

Run: `cd "$SCRATCH/personality" && dotnet run` — Expected: build errors (`PlynlingStress`, `PlynlingModifiers`, `CareAct`, `Need` do not exist).

- [ ] **Step 2: The columns**

In `ProjectSYNCS/Models/Plynling.cs`, after the growth columns:

```csharp

    // CK3's stress, 0..400 (Helpers/PlynlingStress). Only the owner's choices raise it; care and the
    // mornings lower it. Every change rebases the needs, since its level changes how fast happiness drains.
    public int Stress { get; set; }

    // Active modifiers, "key:unixSeconds;…" (Helpers/PlynlingModifiers). On the row, not in a table:
    // the drain maths are pure and read only this row. Settle drops each one at its end, rebasing.
    public string? Modifiers { get; set; }

    // Its traits' daily stress-decay multiplier as a bonus percent (0 = ×1, 50 = ×1.5, −50 = ×0.5),
    // cached here so Settle can decay stress without loading the traits. Refreshed whenever its traits
    // change (PlynlingService.EnsureTraitsAsync, a coping trait).
    public int StressLossBonusPercent { get; set; }
```

- [ ] **Step 3: Coping traits' decay**

In `PlynlingTraits.cs`, give `K(...)` a loss parameter:

```csharp
    private static TraitInfo K(string key, string m, string f, string emoji, string description,
        Dictionary<PlynlingStat, int> stats, Dictionary<AiAxis, int> axes, double loss = 1) =>
        new(key, TraitKind.Coping, "coping", m, f, emoji, description, stats, axes, StressLoss: loss);
```

and add `, loss: 1.25` to `inappetetic`, `, loss: 1.5` to `reclusive` and `, loss: 1.5` to `journaller` (as the last argument of their `K(...)` calls).

- [ ] **Step 4: `PlynlingStress`**

`ProjectSYNCS/Helpers/PlynlingStress.cs`:

```csharp
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

// The care that relieves stress.
public enum CareAct { Pet, Game, Visit, Bath, Meal }

/// <summary>
/// CK3's stress, pure. 0–400, three levels; each drains happiness faster and, from level 2, costs
/// stats. Only the owner's choices raise it (the event engine enforces it); care and the mornings
/// lower it. It never touches hunger or death.
/// </summary>
public static class PlynlingStress
{
    public const int Max = 400;
    public const int LevelStep = 100;
    public const int DailyDecay = 15;

    public static int Level(int stress) => Math.Clamp(stress / LevelStep, 0, 3);

    public static double HappinessFactor(int level) => level switch { 1 => 1.15, 2 => 1.35, 3 => 1.6, _ => 1.0 };

    public static int StatPenalty(int level) => level switch { 2 => -1, 3 => -2, _ => 0 };

    // CK3's gain multipliers (Méfiant ×2, Excentrique ×1.5, Capricieux ×0.5), multiplied together.
    public static double GainMultiplier(IEnumerable<TraitInfo> traits) => traits.Aggregate(1.0, (m, t) => m * t.StressGain);

    public static int Scaled(int amount, IEnumerable<TraitInfo> traits) => (int)Math.Round(amount * GainMultiplier(traits));

    // The traits' decay multiplier as a bonus percent, for Plynling.StressLossBonusPercent.
    public static int LossBonusPercent(IEnumerable<TraitInfo> traits) =>
        (int)Math.Round((traits.Aggregate(1.0, (m, t) => m * t.StressLoss) - 1) * 100);

    // How much a care act relieves, after its coping traits.
    public static int Relief(CareAct act, IReadOnlyList<TraitInfo> traits, bool closeBond = false)
    {
        bool Has(string key) => traits.Any(t => t.Key == key);
        double relief = act switch
        {
            CareAct.Pet => Has("contrite") ? 10 : 5,
            CareAct.Game => 15,
            CareAct.Visit => Has("improvident") ? 30 : 20,
            CareAct.Bath => Has("profligate") ? 10 : 3,
            CareAct.Meal => Has("comfort_eater") ? 10 : 0,
            _ => 0,
        };
        if (act == CareAct.Pet && Has("irritable")) relief *= 0.5;
        if (act == CareAct.Game && Has("athletic")) relief *= 2;
        if (act == CareAct.Game && Has("irritable")) relief *= 1.5;
        if (act == CareAct.Visit && Has("reclusive")) relief *= 0.5;
        if (act == CareAct.Visit && closeBond && Has("confider")) relief *= 2;
        return (int)Math.Round(relief);
    }

    // The morning's decay for this row: the base, its traits' cached multiplier, its modifiers.
    public static int DecayAt(Plynling p) =>
        (int)Math.Round(DailyDecay * (1 + p.StressLossBonusPercent / 100.0) * PlynlingModifiers.StressDecayFactor(p));
}
```

- [ ] **Step 5: `PlynlingModifiers`**

`ProjectSYNCS/Helpers/PlynlingModifiers.cs`:

```csharp
using System.Globalization;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

public enum Need { Hunger, Happiness, Hygiene }

/// <summary>
/// A timed modifier. <see cref="Key"/> is stored (in <c>Plynling.Modifiers</c>) and **never renamed**.
/// Drain factors multiply how fast a need empties (0.8 = slower); Meal multiplies a meal's hunger,
/// Gift the happy gift's chance, StressDecay the morning's decay. <see cref="Negative"/> is exactly
/// "makes something worse" (the harness checks it): deciding alone never applies one, outside a
/// mental break. No modifier may speed hunger up.
/// </summary>
public sealed record ModifierInfo(
    string Key, string NameM, string NameF, string Emoji, string Description, TimeSpan Duration, bool Negative,
    IReadOnlyDictionary<PlynlingStat, int> Stats,
    double Hunger = 1, double Happiness = 1, double Hygiene = 1, double Meal = 1, double Gift = 1, double StressDecay = 1)
{
    public string Name(PlynlingGender g) => g == PlynlingGender.Female ? NameF : NameM;
}

public static class PlynlingModifiers
{
    private static readonly Dictionary<PlynlingStat, int> NoStats = new();
    private static Dictionary<PlynlingStat, int> S(PlynlingStat stat, int v) => new() { [stat] = v };

    // Append new modifiers at the end; keys are stored.
    public static readonly IReadOnlyList<ModifierInfo> All = new[]
    {
        new ModifierInfo("inspired", "Inspiré", "Inspirée", "💡", "Les idées arrivent plus vite que les mots.",
            TimeSpan.FromDays(3), false, S(PlynlingStat.Learning, 2)),
        new ModifierInfo("fired_up", "Plein d'élan", "Pleine d'élan", "🔥", "Prêt à grimper n'importe quel arbre, même les grands.",
            TimeSpan.FromDays(3), false, S(PlynlingStat.Courage, 2)),
        new ModifierInfo("well_rested", "Bien reposé", "Bien reposée", "🛏️", "Une nuit si bonne que l'estomac fait la grasse matinée aussi.",
            TimeSpan.FromDays(2), false, NoStats, Hunger: 0.8),
        new ModifierInfo("light_heart", "Le cœur léger", "Le cœur léger", "🎈", "Les petits tracas glissent dessus comme la pluie sur une feuille.",
            TimeSpan.FromDays(2), false, NoStats, Happiness: 0.8),
        new ModifierInfo("lucky", "Porte-bonheur", "Porte-bonheur", "🍀", "Trouve des trèfles à quatre feuilles sans même les chercher.",
            TimeSpan.FromDays(3), false, NoStats, Gift: 1.5),
        new ModifierInfo("soothed", "Apaisé", "Apaisée", "🫖", "Une tasse chaude, une couverture, et le monde peut attendre.",
            TimeSpan.FromDays(3), false, NoStats, Happiness: 0.8, StressDecay: 2),
        new ModifierInfo("grumpy", "Grognon", "Grognonne", "🌧️", "Tout agace. Surtout ce qui ne fait rien.",
            TimeSpan.FromDays(2), true, NoStats, Happiness: 1.3),
        new ModifierInfo("muddy_paws", "Les pattes sales", "Les pattes sales", "🐾", "Laisse des empreintes partout, même au plafond, on ne sait pas comment.",
            TimeSpan.FromDays(1), true, NoStats, Hygiene: 1.5),
        new ModifierInfo("sulky", "Boudeur", "Boudeuse", "😤", "Mange du bout des lèvres, pour bien montrer quelque chose.",
            TimeSpan.FromDays(2), true, NoStats, Meal: 0.8),
        new ModifierInfo("distracted", "Distrait", "Distraite", "🌀", "Commence trois phrases et n'en finit aucune.",
            TimeSpan.FromDays(3), true, S(PlynlingStat.Learning, -2)),
        new ModifierInfo("woods_cold", "Rhume des bois", "Rhume des bois", "🤧", "Éternue des feuilles mortes. C'est moins joli qu'on croit.",
            TimeSpan.FromDays(2), true, NoStats, Happiness: 1.2, Hygiene: 1.3),
    };

    private static readonly Dictionary<string, ModifierInfo> ByKeyMap = All.ToDictionary(m => m.Key);

    public static ModifierInfo? ByKey(string key) => ByKeyMap.GetValueOrDefault(key);

    // What the row holds, in order of end. Unknown keys and malformed entries are skipped, never thrown on.
    public static IReadOnlyList<(ModifierInfo Info, DateTimeOffset Ends)> Active(Plynling p)
    {
        var list = new List<(ModifierInfo, DateTimeOffset)>();
        foreach (var part in (p.Modifiers ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var bits = part.Split(':');
            if (bits.Length == 2 && ByKey(bits[0]) is { } info
                && long.TryParse(bits[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var unix))
                list.Add((info, DateTimeOffset.FromUnixTimeSeconds(unix)));
        }
        return list.OrderBy(x => x.Item2).ToList();
    }

    public static void Write(Plynling p, IEnumerable<(string Key, DateTimeOffset Ends)> modifiers)
    {
        var text = string.Join(";", modifiers.Select(m => $"{m.Key}:{m.Ends.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)}"));
        p.Modifiers = text.Length == 0 ? null : text;
    }

    public static DateTimeOffset? NextEnd(Plynling p) => Active(p).Select(m => (DateTimeOffset?)m.Ends).FirstOrDefault();

    // A need's drain multiplier now: the product of its modifiers, clamped — hunger can only slow.
    public static double Multiplier(Plynling p, Need need)
    {
        var product = Active(p).Aggregate(1.0, (m, x) => m * need switch
        {
            Need.Hunger => x.Info.Hunger,
            Need.Happiness => x.Info.Happiness,
            _ => x.Info.Hygiene,
        });
        return need == Need.Hunger ? Math.Clamp(product, 0.5, 1) : Math.Clamp(product, 0.5, 2);
    }

    public static double MealFactor(Plynling p) => Math.Clamp(Active(p).Aggregate(1.0, (m, x) => m * x.Info.Meal), 0.5, 2);
    public static double GiftFactor(Plynling p) => Math.Clamp(Active(p).Aggregate(1.0, (m, x) => m * x.Info.Gift), 0.5, 2);
    public static double StressDecayFactor(Plynling p) => Math.Clamp(Active(p).Aggregate(1.0, (m, x) => m * x.Info.StressDecay), 0.5, 2);
    public static int StatDelta(Plynling p, PlynlingStat stat) => Active(p).Sum(x => x.Info.Stats.GetValueOrDefault(stat));
}
```

- [ ] **Step 6: Record the departures in the spec**

In `docs/superpowers/specs/2026-10-06-plynling-events-design.md`, Phase 3 « Modifiers », replace the « **Storage — `PlynlingModifier`:** … » bullet with:

```markdown
- **Storage — a column, `Plynling.Modifiers`** (`key:unixSeconds;…`), not a table: `PlynlingLife` is
  pure and reads only the row, and `Settle` must see every modifier's end to play it in time order.
  The journal keeps the history; the instance that applied one can be found from the event history.
```

and in its effects bullet remove « pet cooldown; », adding after the bullet: « (A pet-cooldown effect was dropped: the cooldown is an in-memory gate per petter on a shared card.) ». In « Cross-cutting → Migrations », replace `PlynlingModifier` in the table list with nothing and add `Modifiers`, `StressLossBonusPercent` to the column list.

- [ ] **Step 7: Run the harness and build**

Run: `cd "$SCRATCH/personality" && dotnet run` then `cd "$REPO/ProjectSYNCS" && dotnet build -warnaserror`
Expected: `OK`; `Build succeeded`.

- [ ] **Step 8: Files for the owner to commit**

`ProjectSYNCS/Helpers/PlynlingStress.cs`, `ProjectSYNCS/Helpers/PlynlingModifiers.cs`, `ProjectSYNCS/Models/Plynling.cs`, `ProjectSYNCS/Helpers/PlynlingTraits.cs`, the spec — `Plynling stress and modifiers: pure rules` (commit with Task 2, which adds the migration, if the owner prefers).

---

### Task 2: `PlynlingLife` — rates, the `Settle` timeline, and the migration

**Files:**
- Modify: `ProjectSYNCS/Helpers/PlynlingLife.cs`
- Create: `ProjectSYNCS/Migrations/<timestamp>_AddPlynlingStress.cs` (via `dotnet ef`)
- Test: `$SCRATCH/personality/Program.cs`

**Interfaces:**
- Consumes: Task 1.
- Produces (on `PlynlingLife`): `HungerRate(Plynling)`, `HappinessRate(Plynling)`, `HygieneRate(Plynling)` (doubles); `AddStress(Plynling, DateTimeOffset, int amount) -> int` (levels crossed upward); `Relieve(Plynling, DateTimeOffset, int amount)`; `SetStress(Plynling, DateTimeOffset, int value)`; `AddModifier(Plynling, DateTimeOffset, ModifierInfo)`; `RemoveModifier(Plynling, DateTimeOffset, string key)`; `GiftDraw(Random rng, double chanceFactor = 1)`; `MealFactor` now includes modifiers. `HungerAt`, `HygieneAt`, `HappinessAt`, `DeathAt` read the rates; `Settle` plays modifier ends; mornings decay stress; `Tend` drops ended modifiers.

- [ ] **Step 1: Add the failing checks**

In `$SCRATCH/personality/Program.cs`, insert before the final `Console.WriteLine`:

```csharp
// ==== PHASE 3 — exact needs with rates ================================================================
static double Hours(double h) => h;
var day0 = new DateTimeOffset(2026, 10, 6, 10, 0, 0, TimeSpan.Zero);   // 12:00 Paris
Plynling Live(int id) { var p = Fresh(id); p.NeedsAsOf = day0; p.LiveSince = day0; p.LastMorningDay = PlynlingLife.MorningDayAtOrBefore(day0); return p; }

// stress level: happiness drains ×1.15 at level 1
var s1 = Live(70); s1.Happiness = 1; s1.Hygiene = 1; s1.Stress = 150;
Check(Math.Abs(PlynlingLife.HappinessAt(s1, day0.AddHours(18)) - (1 - 18 * 1.15 / 36)) < 1e-9, "level 1: happiness ×1.15");

// a hunger-slowing modifier
var h1 = Live(71); h1.Hunger = 1;
PlynlingLife.AddModifier(h1, day0, PlynlingModifiers.ByKey("well_rested")!);
Check(Math.Abs(PlynlingLife.HungerAt(h1, day0.AddHours(24)) - (1 - 24 * 0.8 / 48)) < 1e-9, "well rested: hunger ×0.8");

// Review focus 1: the slow modifier ends (2 days) before starvation — death is piecewise exact.
var h2 = Live(72); h2.Hunger = 0.9; h2.Happiness = 1; h2.Hygiene = 1;
PlynlingLife.AddModifier(h2, day0, PlynlingModifiers.ByKey("well_rested")!);
// 48 h at ×0.8 uses 0.8 of hunger; the remaining 0.1 lasts 4.8 h at ×1.
var expected = day0.AddHours(48 + 4.8);
SicknessRoll neverSick = (_, _, _) => 0.999;    // keep illness out of an exact-time check
PlynlingLife.Settle(h2, day0.AddDays(4), neverSick);
Check(h2.DiedAt is { } died && Math.Abs((died - (PlynlingLife.IsAsleep(expected) ? PlynlingLife.WakeAfter(expected) : expected)).TotalSeconds) < 1,
    $"death after the modifier ends is piecewise exact ({h2.DiedAt:o})");
Check(h2.Modifiers is null, "the ended modifier is gone");

// Review focus 2: stress decays at the morning; happiness exact across the level change
var s2 = Live(73); s2.Happiness = 1; s2.Hygiene = 1; s2.Hunger = 1; s2.Stress = 105;   // level 1, decays to 90 at 05:00
var morning = PlynlingLife.MorningAt(PlynlingLife.NextDay(s2.LastMorningDay));
var at = morning.AddHours(3);
PlynlingLife.Settle(s2, at, neverSick);
var before = (morning - day0).TotalHours * 1.15;
var after = 3.0 * 1.0;
Check(s2.Stress == 90, $"the morning decays stress by 15 ({s2.Stress})");
Check(Math.Abs(PlynlingLife.HappinessAt(s2, at) - Math.Max(0, 1 - (before + after) / 36)) < 1e-6, "happiness exact across the morning");

// Review focus 4: frozen across a modifier's end
var f2 = Live(74); f2.Hunger = 0.6; f2.Happiness = 0.6; f2.Hygiene = 0.6;
PlynlingLife.AddModifier(f2, day0, PlynlingModifiers.ByKey("grumpy")!);
PlynlingLife.Freeze(f2, day0.AddHours(1), byStaff: true);
var frozenHappiness = f2.Happiness;
PlynlingLife.Settle(f2, day0.AddDays(5));
Check(f2.Modifiers is null && f2.Happiness == frozenHappiness && f2.NeedsAsOf == day0.AddHours(1), "frozen: the modifier ends, nothing else moves");

// Review focus 5: the mascot sheds ended modifiers
PlynlingMascot.Bind(999);
var m2 = Live(75); m2.OwnerId = 999;
PlynlingLife.AddModifier(m2, day0, PlynlingModifiers.ByKey("inspired")!);
PlynlingLife.Settle(m2, day0.AddDays(4));
Check(m2.Modifiers is null, "the mascot sheds ended modifiers");

// stress bookkeeping
var s3 = Live(76);
Check(PlynlingLife.AddStress(s3, day0, 120) == 1 && s3.Stress == 120, "crossing 100 counts one level");
Check(PlynlingLife.AddStress(s3, day0, 200) == 2 && s3.Stress == 320, "two levels at once");
Check(PlynlingLife.AddStress(s3, day0, 500) == 0 && s3.Stress == 400, "capped at 400");
PlynlingLife.Relieve(s3, day0, 1000);
Check(s3.Stress == 0, "relief floors at 0");
var r2 = Live(77);
PlynlingLife.AddModifier(r2, day0, PlynlingModifiers.ByKey("grumpy")!);
PlynlingLife.AddModifier(r2, day0.AddHours(10), PlynlingModifiers.ByKey("grumpy")!);
Check(PlynlingModifiers.Active(r2).Count == 1 && PlynlingModifiers.Active(r2)[0].Ends == day0.AddHours(10) + TimeSpan.FromDays(2), "re-applying refreshes");
PlynlingLife.RemoveModifier(r2, day0.AddHours(11), "grumpy");
Check(r2.Modifiers is null, "remove");
Check(PlynlingLife.GiftDraw(new Random(1), 100) > 0, "a huge gift factor always finds something");
```

Run: `cd "$SCRATCH/personality" && dotnet run` — Expected: build errors (`AddModifier`, `AddStress`, … do not exist).

- [ ] **Step 2: Rates in the drain helpers**

In `PlynlingLife.cs`, replace `HungerAt`, `HygieneAt`, `HappinessAt` and `DeathAt` with:

```csharp
    // The current segment's drain multipliers: its stress level and its modifiers, both on the row and
    // constant between two transitions (every change of either rebases first). 1 = the base rate.
    public static double HungerRate(Plynling p) => PlynlingModifiers.Multiplier(p, Need.Hunger);
    public static double HygieneRate(Plynling p) => PlynlingModifiers.Multiplier(p, Need.Hygiene);
    public static double HappinessRate(Plynling p) =>
        PlynlingStress.HappinessFactor(PlynlingStress.Level(p.Stress)) * PlynlingModifiers.Multiplier(p, Need.Happiness);

    public static double HungerAt(Plynling p, DateTimeOffset t) =>
        IsFrozen(p) || IsDead(p) ? p.Hunger : Clamp(p.Hunger - (t - p.NeedsAsOf) / HungerLife * HungerRate(p));

    public static double HygieneAt(Plynling p, DateTimeOffset t) =>
        IsFrozen(p) || IsDead(p) ? p.Hygiene : Clamp(p.Hygiene - (t - p.NeedsAsOf) / HygieneLife * HygieneRate(p));

    public static bool IsDirty(Plynling p, DateTimeOffset t) => HygieneAt(p, t) < DirtyBelow;

    // Happiness drains at its segment rate while clean and DirtyHappinessFactor times faster once
    // hygiene has fallen below DirtyBelow. Hygiene falls linearly within a segment, so it crosses at one
    // exact instant and the drain is two straight pieces — exact, not approximated.
    public static double HappinessAt(Plynling p, DateTimeOffset t)
    {
        if (IsFrozen(p) || IsDead(p)) return p.Happiness;
        var elapsed = Math.Max(0, (t - p.NeedsAsOf).TotalSeconds);
        var cleanFor = p.Hygiene < DirtyBelow ? 0 : (p.Hygiene - DirtyBelow) * HygieneLife.TotalSeconds / HygieneRate(p);
        var clean = Math.Min(elapsed, cleanFor);
        var dirty = elapsed - clean;
        return Clamp(p.Happiness - (clean + dirty * DirtyHappinessFactor) * HappinessRate(p) / HappinessLife.TotalSeconds);
    }

    // When it will starve at the current segment's rate. Settle plays any modifier end first, so a
    // death it finds is always computed within one segment.
    public static DateTimeOffset? DeathAt(Plynling p) =>
        IsFrozen(p) || IsDead(p) ? null : p.NeedsAsOf + p.Hunger * HungerLife / HungerRate(p);
```

(The original `IsDirty` line is replaced by the copy above — keep only one.)

- [ ] **Step 3: The timeline in `Settle`**

Replace the `while (true) { … }` loop in `Settle` with:

```csharp
        // The timeline: modifier ends and mornings, in time order, each rebasing; a starvation due
        // before the next of them wins and stops it.
        while (true)
        {
            var day = NextDay(p.LastMorningDay);
            var morning = MorningAt(day);
            var ends = PlynlingModifiers.NextEnd(p);
            var next = ends is { } e && e < morning ? e : morning;
            if (next > now) break;
            if (EffectiveDeathAt(p) is { } starve && starve <= next) break;
            if (ends is { } end && end < morning)
            {
                EndModifiersAt(p, end);
                changed = true;
                continue;
            }
            p.LastMorningDay = day;
            changed = true;
            if (morning < frozenUntil || IsFrozen(p)) continue;         // the illness pauses with the rest
            PlayMorning(p, morning, day, roll);
            if (IsDead(p)) return true;
        }
```

Add after `Settle`:

```csharp
    // Every modifier ending at `at` goes; the needs are rebased there first (not while frozen — they
    // do not move then, and the thaw restarts the clock).
    private static void EndModifiersAt(Plynling p, DateTimeOffset at)
    {
        if (!IsFrozen(p) && !IsDead(p)) Rebase(p, at);
        PlynlingModifiers.Write(p, PlynlingModifiers.Active(p).Where(m => m.Ends > at).Select(m => (m.Info.Key, m.Ends)));
    }
```

At the top of `PlayMorning`, before `if (!IsSick(p))`, add:

```csharp
        // The morning eases its stress — a transition, so the needs are rebased at 05:00 first.
        if (p.Stress > 0)
        {
            Rebase(p, morning);
            p.Stress = Math.Max(0, p.Stress - PlynlingStress.DecayAt(p));
        }
```

In `Tend`, right after the `if (IsSick(p)) { … }` block, add:

```csharp
        if (PlynlingModifiers.Active(p).Any(m => m.Ends <= now))
        {
            PlynlingModifiers.Write(p, PlynlingModifiers.Active(p).Where(m => m.Ends > now).Select(m => (m.Info.Key, m.Ends)));
            changed = true;
        }
```

- [ ] **Step 4: Stress and modifier transitions**

Add to `PlynlingLife.cs` (near `Sadden`):

```csharp
    // ---- stress and modifiers: every change is a transition and rebases first --------------------

    // Raises stress (capped at 400) and returns how many levels it climbed — each one a mental break
    // for the caller to queue.
    public static int AddStress(Plynling p, DateTimeOffset now, int amount)
    {
        if (amount <= 0) return 0;
        RebaseIfLive(p, now);
        var before = PlynlingStress.Level(p.Stress);
        p.Stress = Math.Min(PlynlingStress.Max, p.Stress + amount);
        return Math.Max(0, PlynlingStress.Level(p.Stress) - before);
    }

    public static void Relieve(Plynling p, DateTimeOffset now, int amount)
    {
        if (amount <= 0 || p.Stress == 0) return;
        RebaseIfLive(p, now);
        p.Stress = Math.Max(0, p.Stress - amount);
    }

    // /debug stress only.
    public static void SetStress(Plynling p, DateTimeOffset now, int value)
    {
        RebaseIfLive(p, now);
        p.Stress = Math.Clamp(value, 0, PlynlingStress.Max);
    }

    // Applies or refreshes (same key) a modifier from now.
    public static void AddModifier(Plynling p, DateTimeOffset now, ModifierInfo modifier)
    {
        RebaseIfLive(p, now);
        PlynlingModifiers.Write(p, PlynlingModifiers.Active(p)
            .Where(m => m.Info.Key != modifier.Key && m.Ends > now)
            .Select(m => (m.Info.Key, m.Ends))
            .Append((modifier.Key, now + modifier.Duration)));
    }

    public static void RemoveModifier(Plynling p, DateTimeOffset now, string key)
    {
        RebaseIfLive(p, now);
        PlynlingModifiers.Write(p, PlynlingModifiers.Active(p).Where(m => m.Info.Key != key).Select(m => (m.Info.Key, m.Ends)));
    }

    private static void RebaseIfLive(Plynling p, DateTimeOffset at)
    {
        if (!IsFrozen(p) && !IsDead(p)) Rebase(p, at);
    }
```

Change `MealFactor` to multiply by the modifiers:

```csharp
    public static double MealFactor(Plynling p, DateTimeOffset now)
    {
        var happiness = HappinessAt(p, now);
        var mood = happiness > HappyAbove ? 1 + HappyMealBonus
            : happiness < SadBelow ? 1 - SadMealPenalty
            : 1.0;
        return mood * PlynlingModifiers.MealFactor(p);
    }
```

and `GiftDraw`:

```csharp
    public static long GiftDraw(Random rng, double chanceFactor = 1) =>
        rng.NextDouble() < Math.Min(1, GiftChance * chanceFactor) ? rng.Next(GiftMin, GiftMax + 1) : 0;
```

In `PlynlingService.TryGiftAsync`, change `PlynlingLife.GiftDraw(rng)` to `PlynlingLife.GiftDraw(rng, PlynlingModifiers.GiftFactor(p))`.

- [ ] **Step 5: Run the harness**

Run: `cd "$SCRATCH/personality" && dotnet run`
Expected: `OK`. Also re-run the hygiene/sickness harness if it still exists (`$SCRATCH/health`, from the hygiene plan) — every old check must still pass: with no stress and no modifier, every rate is 1 and every value is unchanged.

- [ ] **Step 6: Build and generate the migration**

Run: `cd "$REPO/ProjectSYNCS" && dotnet build -warnaserror && dotnet ef migrations add AddPlynlingStress`
Expected: `Done.` In `Up`: `AddColumn<int>` `Stress` (default 0), `AddColumn<string>` `Modifiers` (nullable), `AddColumn<int>` `StressLossBonusPercent` (default 0) on `Plynlings`. Nothing else.

- [ ] **Step 7: Build**

Run: `cd "$REPO/ProjectSYNCS" && dotnet build -warnaserror` — Expected: `Build succeeded`.

- [ ] **Step 8: Files for the owner to commit**

`ProjectSYNCS/Helpers/PlynlingLife.cs`, `ProjectSYNCS/Services/PlynlingService.cs`, the three migration files — `Plynling stress and modifiers: exact rates and the Settle timeline`.

---

### Task 3: Stats, the trait cache, and care relief

**Files:**
- Modify: `ProjectSYNCS/Helpers/PlynlingStats.cs` (`StatLine` gains `State`; `Compute` reads stress and modifiers)
- Modify: `ProjectSYNCS/Services/PlynlingService.cs` (`EnsureTraitsAsync` refreshes the cache; relief in `FeedAsync`, `PetAsync`, `BathAsync`, `FinishPlayAsync`, `VisitAsync`)
- Modify: `ProjectSYNCS/Helpers/PlynlingPersonality.cs` (`StatText` shows the state part)
- Test: `$SCRATCH/personality/Program.cs`

**Interfaces:**
- Produces: `StatLine(PlynlingStat Stat, int Base, int Passion, int Traits, int Growth, int State, int Total)` — **`State` inserted before `Total`** (stress penalty + modifier deltas); every `new StatLine(` and every positional deconstruction must follow. `PlynlingService.RefreshStressCacheAsync(Plynling p)` (never saves).

- [ ] **Step 1: Add the failing checks**

```csharp
// ==== PHASE 3 — stats with state =======================================================================
var st = Fresh(80); st.Stress = 250;   // level 2: −1 all
PlynlingLife.AddModifier(st, noon, PlynlingModifiers.ByKey("inspired")!);
var sheet3 = PlynlingStats.Compute(st, Array.Empty<TraitInfo>()).ToDictionary(l => l.Stat);
Check(sheet3[PlynlingStat.Learning].State == 1 && sheet3[PlynlingStat.Courage].State == -1, "state: stress −1, Inspirée +2 Sagesse");
Check(sheet3[PlynlingStat.Courage].Total == Math.Max(0, sheet3[PlynlingStat.Courage].Base - 1 + sheet3[PlynlingStat.Courage].Passion), "total includes state");
```

Run — Expected: build error (`State` does not exist).

- [ ] **Step 2: `StatLine` and `Compute`**

In `PlynlingStats.cs`:

```csharp
public sealed record StatLine(PlynlingStat Stat, int Base, int Passion, int Traits, int Growth, int State, int Total);
```

and in `Compute`, replace the lambda's last two lines with:

```csharp
            var state = PlynlingStress.StatPenalty(PlynlingStress.Level(p.Stress)) + PlynlingModifiers.StatDelta(p, stat);
            return new StatLine(stat, die, passion, fromTraits, growth, state, Math.Max(0, die + passion + fromTraits + growth + state));
```

In `PlynlingPersonality.StatText`, after the growth part:

```csharp
        if (l.State != 0) parts.Add($"état {Signed(l.State)}");
```

Run: `grep -rn "new StatLine(\|StatLine(" ProjectSYNCS --include=*.cs` — only `PlynlingStats.cs` constructs one.

- [ ] **Step 3: The trait cache**

In `PlynlingService.cs`, add after `GetTraitsAsync`:

```csharp
    // Its traits' stress-decay multiplier, cached on the row for Settle (which cannot load traits).
    // Counts traits added earlier in this unit of work. Never saves.
    public async Task RefreshStressCacheAsync(Plynling p)
    {
        var keys = (await _db_context.PlynlingTraits.Where(t => t.PlynlingId == p.Id).Select(t => t.Key).ToListAsync())
            .Concat(_db_context.PlynlingTraits.Local.Where(t => t.PlynlingId == p.Id).Select(t => t.Key))
            .Distinct();
        p.StressLossBonusPercent = PlynlingStress.LossBonusPercent(keys.Select(PlynlingTraits.ByKey).OfType<TraitInfo>());
    }
```

and at the end of `EnsureTraitsAsync` (after the `foreach`), add `await RefreshStressCacheAsync(p);` — it runs on every sweep, which also fills the cache for every existing Plynling.

- [ ] **Step 4: Care relieves**

In `PlynlingService.cs`:
- `PetAsync`: after `PlynlingLife.Pet(plynling, now);` add `PlynlingLife.Relieve(plynling, now, PlynlingStress.Relief(CareAct.Pet, await GetTraitsAsync(plynling)));`
- `BathAsync`: after `PlynlingLife.Bath(plynling!, now);` add `PlynlingLife.Relieve(plynling!, now, PlynlingStress.Relief(CareAct.Bath, await GetTraitsAsync(plynling!)));`
- `FeedAsync`: after `PlynlingLife.Feed(plynling, info, now);` add `PlynlingLife.Relieve(plynling, now, PlynlingStress.Relief(CareAct.Meal, await GetTraitsAsync(plynling)));`
- `FinishPlayAsync`: after `PlynlingLife.Play(plynling, now, won);` add `PlynlingLife.Relieve(plynling, now, PlynlingStress.Relief(CareAct.Game, await GetTraitsAsync(plynling)));`
- `VisitAsync`: after the two `PlynlingLife.Visit(...)` calls add:

```csharp
        var close = after is PlynlingBond.Friends or PlynlingBond.BestFriends or PlynlingBond.Lovers;
        PlynlingLife.Relieve(visitor, now, PlynlingStress.Relief(CareAct.Visit, await GetTraitsAsync(visitor), close));
        PlynlingLife.Relieve(host, now, PlynlingStress.Relief(CareAct.Visit, await GetTraitsAsync(host), close));
```

- [ ] **Step 5: Run the harness and build**

Run both. Expected: `OK`; `Build succeeded`.

- [ ] **Step 6: Files for the owner to commit**

`ProjectSYNCS/Helpers/PlynlingStats.cs`, `ProjectSYNCS/Helpers/PlynlingPersonality.cs`, `ProjectSYNCS/Services/PlynlingService.cs` — `Plynling stress: stats, decay cache, care relief`.

---

### Task 4: Events gain stress, modifiers and mental breaks

**Files:**
- Modify: `ProjectSYNCS/Helpers/PlynlingEventCatalog.cs` (effects, `BreakLevel`, `WeightByModifier`)
- Modify: `ProjectSYNCS/Helpers/PlynlingEventEngine.cs` (`WeightOf` reads modifiers; `PickBreak`)
- Modify: `ProjectSYNCS/Helpers/PlynlingTraits.cs` (`DrawCoping`)
- Modify: `ProjectSYNCS/Helpers/PlynlingEvents.cs` (three breaks; two starter events gain a modifier)
- Modify: `ProjectSYNCS/Models/PlynlingEventInstance.cs` (`StressDelta`, `GainedTraitKey`)
- Modify: `ProjectSYNCS/Services/PlynlingService.Events.cs` (`ApplyEventAsync`, `QueueEventAsync`)
- Create: `ProjectSYNCS/Migrations/<timestamp>_AddEventStressColumns.cs`
- Test: `$SCRATCH/personality/Program.cs`

**Interfaces:**
- Produces: effects `StressChange(int Amount)`, `ApplyModifier(string Key)`, `GainCoping()`; `EventDef` gains trailing `int BreakLevel = 0, IReadOnlyDictionary<string, double>? WeightByModifier = null`; `PlynlingEventEngine.PickBreak(int plynlingId, int salt, int level, IEnumerable<EventDef>) -> EventDef?`; `PlynlingTraits.DrawCoping(int plynlingId, int salt, IReadOnlyCollection<string> held) -> TraitInfo?` (null at 2 coping traits); `PlynlingEventInstance.StressDelta` (int?), `GainedTraitKey` (string?); `PlynlingService.QueueEventAsync(Plynling, EventDef, int? targetId, DateTimeOffset) -> PlynlingEventInstance` (never saves); `PlynlingEventEngine.AppliesWhenAlone(EventEffect, EventDef) -> bool`.

- [ ] **Step 1: Add the failing checks**

```csharp
// ==== PHASE 3 — events with stress ======================================================================
Check(PlynlingEvents.All.Count(e => e.BreakLevel > 0) == 3 && new[] { 1, 2, 3 }.All(l => PlynlingEvents.All.Any(e => e.BreakLevel == l)), "one break per level");
foreach (var e in PlynlingEvents.All.Where(e => e.BreakLevel > 0))
{
    Check(e.Type == EventType.Triggered, $"{e.Key}: a break is triggered, never pulsed");
    Check(e.Options.All(o => o.OnSuccess.OfType<StressChange>().Sum(s => s.Amount) is <= -60 and >= -100), $"{e.Key}: every outcome lowers stress by 60–100");
}
foreach (var e in PlynlingEvents.All.Where(e => e.BreakLevel == 0))
    foreach (var o in e.Options)
        Check(!o.OnSuccess.Concat(o.OnFailure).OfType<GainCoping>().Any(), $"{e.Key}/{o.Key}: coping only from breaks");
foreach (var e in PlynlingEvents.All)
    foreach (var o in e.Options)
        Check(o.OnSuccess.Concat(o.OnFailure).OfType<ApplyModifier>().All(m => PlynlingModifiers.ByKey(m.Key) is not null), $"{e.Key}/{o.Key}: modifier keys exist");

// alone: the rule
var normal = PlynlingEvents.ByKey("grown_parcel")!;
var brk = PlynlingEvents.All.First(e => e.BreakLevel > 0);
Check(!PlynlingEventEngine.AppliesWhenAlone(new StressChange(30), normal) && PlynlingEventEngine.AppliesWhenAlone(new StressChange(-30), normal), "alone: no gain, relief ok");
Check(!PlynlingEventEngine.AppliesWhenAlone(new ApplyModifier("grumpy"), normal) && PlynlingEventEngine.AppliesWhenAlone(new ApplyModifier("lucky"), normal), "alone: no negative modifier");
Check(PlynlingEventEngine.AppliesWhenAlone(new ApplyModifier("grumpy"), brk) && PlynlingEventEngine.AppliesWhenAlone(new GainCoping(), brk), "a break applies everything");

// coping draws
var copingSeen = new HashSet<string>();
for (var id = 1; id <= 500; id++)
{
    var c1 = PlynlingTraits.DrawCoping(id, 1, Array.Empty<string>())!;
    Check(c1.Kind == TraitKind.Coping, "a coping trait");
    copingSeen.Add(c1.Key);
    var c2 = PlynlingTraits.DrawCoping(id, 2, new[] { c1.Key })!;
    Check(c2.Key != c1.Key, "never the same twice");
    Check(PlynlingTraits.DrawCoping(id, 3, new[] { c1.Key, c2.Key }) is null, "two at most");
}
Check(copingSeen.Count == 10, "every coping trait can come up");

// Review focus 3: sixty days of deciding alone never gains stress or a negative modifier
for (var id = 1; id <= 200; id++)
{
    var traits = PlynlingTraits.Draw(id, PlynlingStage.Adult, Array.Empty<string>());
    var p3 = Fresh(id);
    var ctx3 = new EventContext(p3, traits, PlynlingStats.Compute(p3, traits), PlynlingStage.Adult);
    foreach (var e in PlynlingEvents.All.Where(e => e.BreakLevel == 0))
        for (var inst = 1; inst <= 60; inst++)
        {
            var o = PlynlingEventEngine.DecideAlone(e, ctx3, id * 1000 + inst);
            Check(PlynlingEventEngine.StressCost(o, ctx3) == 0, $"{id}/{e.Key}: alone never pays a stress cost");
            foreach (var eff in o.OnSuccess.Concat(o.OnFailure).Where(x => PlynlingEventEngine.AppliesWhenAlone(x, e)))
                Check(eff is not StressChange { Amount: > 0 } && (eff is not ApplyModifier am || !PlynlingModifiers.ByKey(am.Key)!.Negative),
                    $"{id}/{e.Key}/{o.Key}: nothing harmful applies alone");
        }
}
```

Run — Expected: build errors.

- [ ] **Step 2: Catalog additions**

In `PlynlingEventCatalog.cs`, after `AffinityShift`:

```csharp
// Positive = gain (owner choices and breaks only), negative = relief (always).
public sealed record StressChange(int Amount) : EventEffect;
// Applies or refreshes a modifier. A negative one never applies when the Plynling decided alone,
// outside a mental break.
public sealed record ApplyModifier(string Key) : EventEffect;
// Mental breaks only: one coping trait it lacks, drawn uniformly; nothing at two.
public sealed record GainCoping : EventEffect;
```

Append to `EventDef`'s parameter list, after `WeightByTrait`:

```csharp
    int BreakLevel = 0, IReadOnlyDictionary<string, double>? WeightByModifier = null
```

and update its doc comment: « <see cref="BreakLevel"/> > 0 marks the mental break for that stress level (triggered only). »

- [ ] **Step 3: Engine additions**

In `PlynlingEventEngine.WeightOf`, before `return`, add:

```csharp
        if (def.WeightByModifier is { } byModifier)
        {
            var active = PlynlingModifiers.Active(ctx.Self).Select(m => m.Info.Key).ToHashSet();
            foreach (var (key, factor) in byModifier)
                if (active.Contains(key)) w *= factor;
        }
```

Make `PickPulse` skip breaks: in its first `Where`, add `&& d.BreakLevel == 0`. Add:

```csharp
    // The rule that keeps "not playing costs nothing": alone, an event never gains stress and never
    // applies a negative modifier or a coping trait — except a mental break, which only stress (from
    // the owner's own choices) can bring on.
    public static bool AppliesWhenAlone(EventEffect effect, EventDef def) =>
        def.BreakLevel > 0 || effect switch
        {
            StressChange s => s.Amount < 0,
            ApplyModifier m => PlynlingModifiers.ByKey(m.Key) is { Negative: false },
            GainCoping => false,
            _ => true,
        };

    public static EventDef? PickBreak(int plynlingId, int salt, int level, IEnumerable<EventDef> defs)
    {
        var pool = defs.Where(d => d.BreakLevel == level).OrderBy(d => d.Key).ToList();
        return pool.Count == 0 ? null : pool[Math.Min(pool.Count - 1, (int)(StableRoll.Unit(plynlingId, salt, 330) * pool.Count))];
    }
```

- [ ] **Step 4: `DrawCoping`**

In `PlynlingTraits.cs`:

```csharp
    private static readonly TraitInfo[] Coping = All.Where(t => t.Kind == TraitKind.Coping).ToArray();
    public const int MaxCoping = 2;

    // A mental break's coping trait: uniform among those it lacks — never from its other traits. Null at two.
    public static TraitInfo? DrawCoping(int plynlingId, int salt, IReadOnlyCollection<string> held)
    {
        if (held.Count(k => ByKey(k)?.Kind == TraitKind.Coping) >= MaxCoping) return null;
        var pool = Coping.Where(t => !held.Contains(t.Key)).ToArray();
        return pool.Length == 0 ? null : Pick(pool, plynlingId, 400 + salt);
    }
```

(Declare `Coping` next to `Childhood` and `Personality`.)

- [ ] **Step 5: The breaks and two modifier rewards**

In `PlynlingEvents.cs`, add `private static readonly PlynlingStage[] Any = Enum.GetValues<PlynlingStage>();` with the other stage arrays, then append to `All` (at the end):

```csharp
        // ---- mental breaks (one per stress level; triggered when it climbs past one)
        new EventDef("break_cloud", EventType.Triggered, Any, "Le petit nuage noir",
            "Depuis ce matin, un petit nuage noir suit {A} partout, et pleut un peu dessus quand personne ne regarde.",
            new[]
            {
                Plain("shout", "Crier un bon coup dans la forêt",
                    "{A} crie si fort que trois corbeaux changent d'adresse. Le nuage, impressionné, s'en va. Reste une petite humeur de chien.",
                    E(new StressChange(-80), new ApplyModifier("grumpy")), Ai((AiAxis.Boldness, 2), (AiAxis.Vengefulness, 1))),
                Plain("tell", "Aller tout raconter à quelqu'un",
                    "{A} parle longtemps. Le nuage écoute aussi, puis s'éloigne. Quelque chose a changé dans sa façon de faire face.",
                    E(new StressChange(-70), new GainCoping()), Ai((AiAxis.Sociability, 2))),
                Plain("curl", "Se rouler en boule sous une feuille",
                    "{A} reste sous la feuille jusqu'au soir. Le nuage finit par s'ennuyer.",
                    E(new StressChange(-60)), Ai((AiAxis.Energy, -1), (AiAxis.Sociability, -1))),
            },
            BreakLevel: 1),

        new EventDef("break_drop", EventType.Triggered, Any, "La goutte d'eau",
            "Une miette de travers, et c'est la goutte d'eau. {A} sent quelque chose monter, monter…",
            new[]
            {
                Plain("smash", "Tout casser (un peu)",
                    "Un pot de confiture n'a pas survécu. {A} se sent mieux, et un peu {a:honteux|honteuse}.",
                    E(new StressChange(-90), new ApplyModifier("grumpy"), new GainCoping()), Ai((AiAxis.Vengefulness, 2), (AiAxis.Rationality, -2))),
                Plain("walk", "Partir marcher très loin",
                    "{A} revient à la nuit tombée, {a:couvert|couverte} de boue jusqu'aux oreilles, l'air plus léger.",
                    E(new StressChange(-80), new ApplyModifier("muddy_paws")), Ai((AiAxis.Energy, 2))),
                Plain("cry", "Pleurer un bon coup",
                    "{A} pleure contre la carapace de la tortue du café, qui a toujours un mouchoir propre.",
                    E(new StressChange(-70)), Ai((AiAxis.Compassion, 1), (AiAxis.Sociability, 1))),
            },
            BreakLevel: 2),

        new EventDef("break_toomuch", EventType.Triggered, Any, "Trop, c'est trop",
            "{A} n'a plus envie de rien. Même le miel a un goût de rien.",
            new[]
            {
                Plain("hide", "Se terrer chez soi",
                    "{A} ferme les volets deux jours entiers. À la réouverture, quelque chose a changé.",
                    E(new StressChange(-100), new GainCoping()), Ai((AiAxis.Sociability, -2))),
                Plain("help", "Accepter l'aide du hérisson",
                    "Le hérisson apporte une couverture, une soupe, et ses horaires de train préférés, à lire pour s'endormir.",
                    E(new StressChange(-90), new ApplyModifier("soothed")), Ai((AiAxis.Sociability, 1), (AiAxis.Rationality, 1))),
                Plain("drift", "Se laisser porter",
                    "{A} se laisse flotter quelques jours. Les choses glissent, puis reviennent doucement à leur place.",
                    E(new StressChange(-100), new ApplyModifier("distracted")), Ai((AiAxis.Energy, -2))),
            },
            BreakLevel: 3),
```

And give two starter events a positive modifier (append to their `E(...)`): in `grown_parcel` / `open`, success: `E(new GrowStat(PlynlingStat.Courage), new ApplyModifier("lucky"))`; in `teen_contest` / `polish`: `E(new GrowStat(PlynlingStat.Stewardship), new ApplyModifier("light_heart"))`. And give `teen_shortcut` / `sneak` a failure stress: its `onFailure` becomes `E(new StressChange(20))`.

- [ ] **Step 6: Instance columns and the migration**

In `PlynlingEventInstance.cs`, after `BondAfter`:

```csharp
    // The net stress this event caused (cost + effects), and the coping trait a break gave — both
    // depend on state at the time, so the story stores them rather than recomputing.
    public int? StressDelta { get; set; }
    public string? GainedTraitKey { get; set; }
```

Run: `cd "$REPO/ProjectSYNCS" && dotnet build -warnaserror && dotnet ef migrations add AddEventStressColumns` — `Up` adds the two nullable columns only.

- [ ] **Step 7: Applying it**

In `PlynlingService.Events.cs`, replace `ApplyEventAsync` with:

```csharp
    private async Task ApplyEventAsync(Plynling p, PlynlingEventInstance inst, EventDef def, EventOption option,
        EventContext ctx, bool decidedAlone, DateTimeOffset now)
    {
        // Loaded first: reading a Plynling settles it and may save, which must happen before this
        // unit of work starts changing anything.
        var target = inst.TargetPlynlingId is { } tid ? await GetByIdAsync(tid, now) : null;
        var stressBefore = p.Stress;
        var climbed = 0;
        // The owner's own choice may cost stress, scaled by its traits. Alone, the option had no cost.
        if (!decidedAlone)
            climbed += PlynlingLife.AddStress(p, now, PlynlingStress.Scaled(PlynlingEventEngine.StressCost(option, ctx), ctx.Traits));

        var success = true;
        if (option.Challenge is { } challenge)
        {
            inst.ChancePercent = PlynlingEventEngine.Chance(challenge, ctx);
            success = PlynlingEventEngine.Succeeds(inst.Id, inst.ChancePercent.Value);
            inst.ChallengeSucceeded = success;
        }
        foreach (var effect in success ? option.OnSuccess : option.OnFailure)
        {
            if (decidedAlone && !PlynlingEventEngine.AppliesWhenAlone(effect, def)) continue;
            switch (effect)
            {
                case GrowStat g:
                    PlynlingStats.AddGrowth(p, g.Stat, g.Amount);
                    break;
                case AffinityShift a when target is { DiedAt: null, FrozenAt: null }:
                    var (before, after) = await ShiftAffinityAsync(p, target, a.Delta, now);
                    inst.BondBefore ??= before;
                    inst.BondAfter = after;
                    break;
                case StressChange s when s.Amount > 0:
                    climbed += PlynlingLife.AddStress(p, now, PlynlingStress.Scaled(s.Amount, ctx.Traits));
                    break;
                case StressChange s:
                    PlynlingLife.Relieve(p, now, -s.Amount);
                    break;
                case ApplyModifier m when PlynlingModifiers.ByKey(m.Key) is { } modifier:
                    PlynlingLife.AddModifier(p, now, modifier);
                    break;
                case GainCoping:
                    var held = ctx.Traits.Select(t => t.Key).ToList();
                    if (PlynlingTraits.DrawCoping(p.Id, inst.Id, held) is { } coping)
                    {
                        _db_context.PlynlingTraits.Add(new PlynlingTrait { PlynlingId = p.Id, Key = coping.Key, Kind = TraitKind.Coping, AcquiredAt = now });
                        await AddMomentAsync(p, JournalKind.TraitGained, coping.Key, now);
                        inst.GainedTraitKey = coping.Key;
                        await RefreshStressCacheAsync(p);
                    }
                    break;
            }
        }
        inst.StressDelta = p.Stress - stressBefore;
        inst.ResolvedAt = now;
        inst.OptionKey = option.Key;
        inst.DecidedAlone = decidedAlone;
        await AddMomentAsync(p, JournalKind.EventStory, def.Key, now);

        // Each level climbed brings on its mental break, whatever the pulse cap says.
        var level = PlynlingStress.Level(p.Stress);
        for (var l = level - climbed + 1; l <= level; l++)
            if (PlynlingEventEngine.PickBreak(p.Id, inst.Id, l, PlynlingEvents.All) is { } breakDef)
                await QueueEventAsync(p, breakDef, null, now);
    }
```

Add, and make `CreateEventAsync` use it:

```csharp
    // Adds an instance without saving — inside another unit of work (a break queued by an event).
    public Task<PlynlingEventInstance> QueueEventAsync(Plynling p, EventDef def, int? targetId, DateTimeOffset now)
    {
        var inst = new PlynlingEventInstance
        {
            PlynlingId = p.Id, EventKey = def.Key, TargetPlynlingId = targetId,
            CreatedAt = now, AvailableAt = now, ExpiresAt = now + PlynlingEventEngine.Lifetime,
        };
        _db_context.PlynlingEventInstances.Add(inst);
        return Task.FromResult(inst);
    }

    public async Task<PlynlingEventInstance> CreateEventAsync(Plynling p, EventDef def, int? targetId, DateTimeOffset now)
    {
        var inst = await QueueEventAsync(p, def, targetId, now);
        await _db_context.SaveChangesAsync();
        return inst;
    }
```

(replacing the phase-2 `CreateEventAsync` body). Add `using ProjectSYNCS.Models;` if `TraitKind` is not in scope.

- [ ] **Step 8: Run the harness and build**

Run both. Expected: `OK`; `Build succeeded`.

- [ ] **Step 9: Files for the owner to commit**

`PlynlingEventCatalog.cs`, `PlynlingEventEngine.cs`, `PlynlingTraits.cs`, `PlynlingEvents.cs`, `PlynlingEventInstance.cs`, `PlynlingService.Events.cs`, the migration files — `Plynling events: stress, modifiers, mental breaks`.

---

### Task 5: Showing it — card, choice, story, Personnalité

**Files:**
- Modify: `ProjectSYNCS/Helpers/PlynlingPersonality.cs` (`StateLine`, embed field « État »)
- Modify: `ProjectSYNCS/Commands/PlynlingModule.cs` (`BuildCard` appends the state)
- Modify: `ProjectSYNCS/Commands/PlynlingEventCards.cs` (stress costs in option details)
- Modify: `ProjectSYNCS/Helpers/PlynlingEventStory.cs` (outcome lines for stress, modifiers, coping)
- Test: `$SCRATCH/personality/Program.cs`

**Interfaces:**
- Produces: `PlynlingPersonality.StateLine(Plynling p, DateTimeOffset now) -> string?` (e.g. « 😣 2 · 💡 🌧️ »).

- [ ] **Step 1: Add the failing checks**

```csharp
// ==== PHASE 3 — display ==================================================================================
var shown = Fresh(90, PlynlingGender.Male); shown.Stress = 210;
PlynlingLife.AddModifier(shown, noon, PlynlingModifiers.ByKey("inspired")!);
Check(PlynlingPersonality.StateLine(shown, noon) == "😣 Stress 2 · 💡", "state line: level and icons");
Check(PlynlingPersonality.StateLine(Fresh(91), noon) is null, "calm and plain: no state line");
var embed3 = PlynlingPersonality.DetailEmbed(shown, mascotTraits);
Check(embed3.Fields.Any(f => f.Name == "État" && f.Value.Contains("Inspirée") == false && f.Value.Contains("Inspiré")), "Personnalité lists the state");
var story3 = new PlynlingEventInstance { Id = 5, PlynlingId = 50, EventKey = "break_cloud", OptionKey = "tell", ResolvedAt = noon,
    DecidedAlone = false, StressDelta = -70, GainedTraitKey = "journaller" };
var out3 = PlynlingEventStory.OutcomeText(story3, lila, null);
Check(out3.Contains("Stress −70") && out3.Contains("Écrit son journal"), "the story tells stress and the coping trait");
```

(`Fresh(90)` is male: « Inspiré ».)

Run — Expected: build error (`StateLine`).

- [ ] **Step 2: Personality and card**

In `PlynlingPersonality.cs`:

```csharp
    // Its stress level (from 1) and its modifiers' icons, for the card. Null when there is neither.
    public static string? StateLine(Plynling p, DateTimeOffset now)
    {
        var parts = new List<string>();
        var level = PlynlingStress.Level(p.Stress);
        if (level > 0) parts.Add($"😣 Stress {level}");
        var icons = string.Join(" ", PlynlingModifiers.Active(p).Where(m => m.Ends > now).Select(m => m.Info.Emoji));
        if (icons.Length > 0) parts.Add(icons);
        return parts.Count == 0 ? null : string.Join(" · ", parts);
    }
```

In `DetailEmbed`, before `.Build()`, add the « État » field when there is anything:

```csharp
        var state = new List<string>();
        if (p.Stress > 0) state.Add($"😣 **Stress {p.Stress}**/{PlynlingStress.Max} · niveau {PlynlingStress.Level(p.Stress)}");
        foreach (var (info, ends) in PlynlingModifiers.Active(p))
            state.Add($"{info.Emoji} **{info.Name(p.Gender)}** — {info.Description} *(jusqu'à <t:{ends.ToUnixTimeSeconds()}:R>)*");
```

and change the builder to add `.AddField("État", string.Join("\n", state))` only when `state.Count > 0` (build the `EmbedBuilder` into a variable, add the field conditionally, then `.Build()`).

In `PlynlingModule.BuildCard`, change the personality line to append the state:

```csharp
        var personality = PlynlingPersonality.CardLine(traits ?? Array.Empty<TraitInfo>(), plynling.Gender);
        var stateLine = alive ? PlynlingPersonality.StateLine(plynling, now) : null;
        var line = string.Join(" · ", new[] { personality, stateLine }.OfType<string>());
        if (line.Length > 0)
            container.AddComponent(new TextDisplayBuilder(line));
```

- [ ] **Step 3: Stress costs on the choice**

In `PlynlingEventCards.Details`, after the challenge part:

```csharp
        var cost = PlynlingStress.Scaled(PlynlingEventEngine.StressCost(o, ctx), ctx.Traits);
        if (cost > 0)
            parts.Add($"😣 +{cost} stress ({string.Join(", ", o.StressCosts.Keys.Where(ctx.Has).Select(k => PlynlingTraits.ByKey(k)!.Name(g)))})");
```

- [ ] **Step 4: The story's outcome**

In `PlynlingEventStory.OutcomeText`, before `return string.Join(...)`:

```csharp
        if (inst.StressDelta is { } ds && ds != 0)
            lines.Add(ds > 0 ? $"-# 😣 Stress +{ds} pour **{self.Name}**" : $"-# 🌿 Stress −{-ds} pour **{self.Name}**");
        foreach (var m in (success ? option.OnSuccess : option.OnFailure).OfType<ApplyModifier>())
            if (PlynlingModifiers.ByKey(m.Key) is { } mod && (!inst.DecidedAlone || PlynlingEventEngine.AppliesWhenAlone(m, def)))
                lines.Add($"-# {mod.Emoji} **{mod.Name(self.Gender)}** pour {(int)mod.Duration.TotalDays} jours");
        if (inst.GainedTraitKey is { } gained && PlynlingTraits.ByKey(gained) is { } trait)
            lines.Add($"-# {trait.Emoji} Nouveau trait : **{trait.Name(self.Gender)}**");
```

- [ ] **Step 5: Run the harness and build**

Run both. Expected: `OK`; `Build succeeded`. Re-measure the event card and story card with the longest break event (the phase-2 card loop covers every event, breaks included).

- [ ] **Step 6: Files for the owner to commit**

`PlynlingPersonality.cs`, `PlynlingModule.cs`, `PlynlingEventCards.cs`, `PlynlingEventStory.cs` — `Plynling stress: on the card, the choice and the story`.

---

### Task 6: Owner tools, help, docs, version, dev-guild test

**Files:**
- Modify: `ProjectSYNCS/Commands/DebugModule.cs` (`/debug stress`, `/debug modifier`)
- Modify: `ProjectSYNCS/Services/PlynlingService.Events.cs` (two debug helpers)
- Modify: `ProjectSYNCS/Commands/PlynlingModule.cs` (help page 1, « Stress »)
- Modify: `README.md`, `docs/agents/plynling-events.md`, `docs/agents/plynling.md`, `ProjectSYNCS/config.yaml`

- [ ] **Step 1: Debug commands**

In `DebugModule.cs`, after `EventAsync`:

```csharp
    // Stress builds over days of the owner's own choices: set it directly to test the levels, the
    // card and a mental break (raising it past a level queues the break, as in play).
    [SlashCommand("stress", "Régler le stress de ton propre Plynling (tests)")]
    public async Task StressAsync([Summary("value", "0 à 400")] [MinValue(0)] [MaxValue(400)] int value)
    {
        if (Context.User.Id != AvailabilityService.OwnerId) { await RespondAsync("Seul Rodhengard peut utiliser cette commande.", ephemeral: true); return; }
        if (Context.Guild is null) { await RespondAsync("Sur un serveur, pas en message privé.", ephemeral: true); return; }
        await DeferAsync(ephemeral: true);
        var result = await _plynlings.DebugStressAsync(Context.Guild.Id, Context.User.Id, value, DateTimeOffset.UtcNow);
        await FollowupAsync(result ?? PlynlingText.NoPlynling, ephemeral: true, allowedMentions: AllowedMentions.None);
    }

    [SlashCommand("modifier", "Ajouter ou retirer un modificateur sur ton propre Plynling (tests)")]
    public async Task ModifierAsync([Summary("key", "Clé du modificateur")] string key, [Summary("remove", "Le retirer")] bool remove = false)
    {
        if (Context.User.Id != AvailabilityService.OwnerId) { await RespondAsync("Seul Rodhengard peut utiliser cette commande.", ephemeral: true); return; }
        if (Context.Guild is null) { await RespondAsync("Sur un serveur, pas en message privé.", ephemeral: true); return; }
        if (PlynlingModifiers.ByKey(key) is not { } mod)
        {
            await RespondAsync("Clés : " + string.Join(", ", PlynlingModifiers.All.Select(m => $"`{m.Key}`")), ephemeral: true);
            return;
        }
        await DeferAsync(ephemeral: true);
        var ok = await _plynlings.DebugModifierAsync(Context.Guild.Id, Context.User.Id, mod, remove, DateTimeOffset.UtcNow);
        await FollowupAsync(ok ? $"🔧 `{key}` {(remove ? "retiré" : "appliqué")}." : PlynlingText.NoPlynling, ephemeral: true);
    }
```

In `PlynlingService.Events.cs`:

```csharp
    // /debug stress: set it, and queue the breaks for any level climbed, as play would.
    public async Task<string?> DebugStressAsync(ulong guildId, ulong ownerId, int value, DateTimeOffset now)
    {
        var p = await GetCurrentAsync(guildId, ownerId, now);
        if (p is null) return null;
        var before = PlynlingStress.Level(p.Stress);
        PlynlingLife.SetStress(p, now, value);
        for (var l = before + 1; l <= PlynlingStress.Level(p.Stress); l++)
            if (PlynlingEventEngine.PickBreak(p.Id, (int)(now.ToUnixTimeSeconds() % 100000), l, PlynlingEvents.All) is { } def)
                await QueueEventAsync(p, def, null, now);
        await FlushMomentsAsync(p);
        await _db_context.SaveChangesAsync();
        return $"🔧 Stress {p.Stress} (niveau {PlynlingStress.Level(p.Stress)}).";
    }

    public async Task<bool> DebugModifierAsync(ulong guildId, ulong ownerId, ModifierInfo mod, bool remove, DateTimeOffset now)
    {
        var p = await GetCurrentAsync(guildId, ownerId, now);
        if (p is null) return false;
        if (remove) PlynlingLife.RemoveModifier(p, now, mod.Key);
        else PlynlingLife.AddModifier(p, now, mod);
        await FlushMomentsAsync(p);
        await _db_context.SaveChangesAsync();
        return true;
    }
```

- [ ] **Step 2: Help page 1**

In `BuildPersonalityHelp`, add a field after « Événements »:

```csharp
            .AddField("Stress et humeurs",
                "Forcer un choix contre son caractère le **stresse** (le coût est affiché). Caresses, jeux, visites et bains " +
                "l'apaisent, et chaque matin aussi. À chaque palier (100, 200, 300), son bonheur file plus vite, puis ses stats " +
                "baissent, et il **craque** : un événement spécial, qui peut lui laisser une manie pour faire face.\n" +
                "Certains choix lui donnent une **humeur** pour quelques jours (💡, 🌧️…) : stats, appétit, bonheur. " +
                "Le stress ne vient **que** de tes choix : un Plynling qui décide seul n'en prend jamais.")
```

Run: `cd "$SCRATCH/personality" && dotnet run` — the help checks re-measure page 1. Expected: `OK`.

- [ ] **Step 3: Docs**

`README.md`, after the « Events » bullet:

```markdown
- **Stress and moods:** forcing a choice against its nature stresses it; care and mornings soothe it.
  Past 100, 200 and 300 its happiness drains faster, its stats dip, and it has a little breakdown — an
  event that may leave it a coping habit. Some choices give a mood for a few days (inspired, grumpy,
  well rested…). Stress only ever comes from its owner's choices.
```

`docs/agents/plynling-events.md`, append:

```markdown
## Stress and modifiers — `Helpers/PlynlingStress`, `Helpers/PlynlingModifiers`

- **On the row** (`Stress`, `Modifiers` as `key:unix;…`, `StressLossBonusPercent`): `PlynlingLife` is
  pure, and its drain helpers read the current segment's rates from the row. **Every change of stress
  or modifiers goes through `PlynlingLife` and rebases first** — never set the fields directly.
- **`Settle` is a timeline:** modifier ends, mornings (sickness, stress decay) and starvation, in time
  order, rebasing at each. A death is always computed within one segment.
- **Hunger can only slow** (clamp 0.5–1): the death clock and the warning only get more lenient.
- **The "not playing is free" rule is code:** `PlynlingEventEngine.AppliesWhenAlone` drops stress gains,
  negative modifiers and coping traits when it decided alone — except in a mental break, which only
  stress (the owner's own choices) can trigger. The harness simulates 60 days of deciding alone.
- `StressLossBonusPercent` caches the traits' decay multiplier for `Settle`; `EnsureTraitsAsync`
  refreshes it every sweep and a new coping trait refreshes it at once.
- Modifier keys are stored — never renamed, append only. `Negative` must mean exactly "makes
  something worse" (harness).
- Tests: `/debug stress value:`, `/debug modifier key: [remove:]`.
```

`docs/agents/plynling.md`, « Happiness, gift and sulking »: add one sentence at the end: « Stress levels and modifiers change drain rates, the meal factor and the gift chance — see `plynling-events.md`. »

`config.yaml`: `version: "5.13.0"` → `version: "5.14.0"`.

- [ ] **Step 4: Build**

Run: `cd "$REPO/ProjectSYNCS" && dotnet build -warnaserror` — Expected: `Build succeeded`.

- [ ] **Step 5: Dev-guild test (with the owner)**

1. Startup applies `AddPlynlingStress` and `AddEventStressColumns`.
2. `/debug event key:teen_shortcut`, choose « Passer quand même » with a Franc/Juste Plynling (or any with a cost shown) → the choice showed « 😣 +n stress (…) »; the story's outcome shows « Stress +n »; the card shows nothing until 100.
3. `/debug stress value:150` → the card shows « 😣 Stress 1 », and « ✨ Événement » offers « Le petit nuage noir ».
4. Pick « Aller tout raconter » → a coping trait appears in Personnalité and the journal; stress drops by 70.
5. `/debug modifier key:inspired` → 💡 on the card; Personnalité: Sagesse « état +2 », « jusqu'à … ».
6. `/debug modifier key:well_rested` and watch the hunger bar over an hour — it drops slower.
7. Pet the Plynling at stress 50 → Personnalité shows stress 45.
8. Next morning: stress decayed by 15 (× its traits).
9. Freeze then thaw across a modifier's end (staff freeze, `/admin`) → the modifier is gone, needs unchanged.

Report each result; failures go back to the owning task.

- [ ] **Step 6: Files for the owner to commit**

`DebugModule.cs`, `PlynlingService.Events.cs`, `PlynlingModule.cs`, `README.md`, `docs/agents/plynling-events.md`, `docs/agents/plynling.md`, `config.yaml` — `Plynling stress: debug tools, help, docs, 5.14.0`.
