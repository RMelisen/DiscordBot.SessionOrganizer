# Plynling Passions and Five-Beat Visit Stories Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Give every Plynling an innate passion (and an optional one taught by its owner as free text), tell visits as five-beat scenes built around those passions, and post an accepted visit's story as a new message.

**Architecture:** A pure catalog (`Helpers/PlynlingPassions.cs`) owns the 12 passions, the matching of typed text and the per-passion line pools. `Plynling` gains three columns. `PlynlingVisitStory.Build` stays pure and grows from three beats to five, choosing a speaker and a subject passion. The accept handler closes the knock and plays the story on a follow-up message.

**Tech Stack:** .NET 10, Discord.Net 3.20 (Interactions, Components V2), EF Core + SQLite.

**Spec:** `docs/superpowers/specs/2026-09-28-plynling-passions-design.md`

## Global Constraints

- All user-facing text is French; code, comments and logs are English. Command and option names are English.
- **Never commit.** The owner commits by hand. Each task ends with a checkpoint, not a `git commit`.
- There is no test project. Verification = `dotnet build` from `ProjectSYNCS/` with 0 errors and 0 warnings, plus the scratch harness (Task 1) printing `OK`.
- `PlynlingPassion` and `JournalKind` are stored as ints: **append only**, never reorder or insert.
- Typed passion text is hostile input: rendered through `PlynlingCardUi.SafeName` and sent with `AllowedMentions.None` everywhere.
- Taught passion: 2–40 characters (`InputCaps.Passion = 40`), no links, 24 h change cooldown.
- In every generic pool, `{P}` never follows `de` or `à` (it may follow `pour`, `sur`, `avec`, a colon, or stand as a subject).
- Exchanges and openers are two lines separated by exactly one `\n` (a literal backslash-n in the C# source).
- Plynling pools in `BotResponses` are `GenderedLines`; the F half never contains the whole words `il`, `mort`, or a `-le` suffix, and the M half never `elle`, `morte`, `-la`.
- Visit story: five beats, `BeatPause` stays 7 s.

## File Map

| File | Change |
|---|---|
| `ProjectSYNCS/Models/Plynling.cs` | + `PlynlingPassion` enum, 3 properties |
| `ProjectSYNCS/Helpers/PlynlingPassions.cs` | **new** — catalog, `Passion` record, matching, pools, combos |
| `ProjectSYNCS/Helpers/InputCaps.cs` | + `Passion` |
| `ProjectSYNCS/Migrations/*_AddPlynlingPassions.cs` | **new** (generated) + backfill SQL |
| `ProjectSYNCS/Services/PlynlingService.cs` | adoption roll, `TeachPassionAsync`, `ResetPassionAsync` |
| `ProjectSYNCS/Helpers/PlynlingCardUi.cs` | passions line in `Heading` |
| `ProjectSYNCS/Helpers/PlynlingJournalUi.cs` | `JournalKind.LearnedPassion` |
| `ProjectSYNCS/Interactions/Modals/PassionModal.cs` | **new** |
| `ProjectSYNCS/Commands/PlynlingModule.cs` | `/plynling passion` + modal handler, help embed |
| `ProjectSYNCS/Commands/AdminModule.cs` | `/admin plynling passion-reset` |
| `ProjectSYNCS/Helpers/PlynlingText.cs` | refusal / confirmation strings |
| `ProjectSYNCS/Services/BotResponses.cs` | `PlynlingPassionTaughtLines`, `PlynlingStaffPassionResetDms` |
| `ProjectSYNCS/Helpers/PlynlingVisitStory.cs` | five beats, `{S}{L}{P}`, reaction/custom pools |
| `ProjectSYNCS/Interactions/Components/PlynlingComponentHandler.cs` | knock closes, story as follow-up |
| `README.md`, `CLAUDE.md` | docs |
| scratch `passions-harness/` (outside the repo) | throwaway checks |

---

### Task 1: The passion catalog and the scratch harness

**Files:**
- Modify: `ProjectSYNCS/Models/Plynling.cs` (enum only in this task)
- Create: `ProjectSYNCS/Helpers/PlynlingPassions.cs`
- Create (scratch, not in repo): `<scratchpad>/passions-harness/passions-harness.csproj`, `Program.cs`

`<scratchpad>` = `C:\Users\c235773\AppData\Local\Temp\claude\C--Users-c235773-Desktop-Sources---RME-Discord-Bots-SessionOrganizer\a4f17c04-d805-4b53-a4d2-dff3721d6e42\scratchpad` (any folder outside the repo works).

**Interfaces:**
- Produces:
  - `enum PlynlingPassion { Cooking, Music, Gaming, Astronomy, Gardening, Rocks, Stories, Dance, Painting, Sport, Insects, Naps }` (namespace `ProjectSYNCS.Models`)
  - `record PassionInfo(PlynlingPassion Passion, string Emoji, string Label, string[] Keywords, string[] Openers, string[] SharedLines, string[] Activities)`
  - `record Passion(PlynlingPassion? Catalog, string? Custom)` with `string Key`, `bool SameAs(Passion)`, `string Render()`, `string Display()`
  - `static class PlynlingPassions`: `IReadOnlyList<PassionInfo> All`, `PassionInfo Info(PlynlingPassion)`, `string Normalize(string)`, `string Clean(string?)`, `bool ContainsLink(string)`, `PlynlingPassion? Resolve(string)`, `Passion FromText(string)`, `IReadOnlyList<Passion> Of(Plynling)`, `Passion? Taught(Plynling)`, `PlynlingPassion RollInnate(Random)`, `string[]? ComboFor(PlynlingPassion, PlynlingPassion)`, `TimeSpan TeachCooldown`, `double SharedTopicChance`

- [ ] **Step 1: Create the scratch harness (it will fail to compile — the types do not exist yet)**

`<scratchpad>/passions-harness/passions-harness.csproj`:

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

`<scratchpad>/passions-harness/Program.cs`:

```csharp
using System.Text.RegularExpressions;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

var failures = new List<string>();
void Check(bool ok, string what) { if (!ok) failures.Add(what); }

// ---- catalog ----
Check(PlynlingPassions.All.Count == Enum.GetValues<PlynlingPassion>().Length, "one catalog entry per enum value");
foreach (var info in PlynlingPassions.All)
{
    Check(PlynlingPassions.Info(info.Passion) == info, $"Info({info.Passion}) round-trips");
    Check(PlynlingPassions.Resolve(info.Label) == info.Passion, $"Resolve(label) finds {info.Passion}");
    Check(info.Openers.Length > 0 && info.SharedLines.Length > 0 && info.Activities.Length > 0, $"{info.Passion} pools non-empty");
    foreach (var o in info.Openers) Check(o.Split('\n').Length == 2, $"{info.Passion} opener has one \\n: {o}");
    foreach (var k in info.Keywords) Check(PlynlingPassions.Normalize(k) == k, $"{info.Passion} keyword normalised: {k}");
}
foreach (var a in Enum.GetValues<PlynlingPassion>())
    foreach (var b in Enum.GetValues<PlynlingPassion>())
        Check(PlynlingPassions.ComboFor(a, b) == PlynlingPassions.ComboFor(b, a), $"combo symmetric {a}/{b}");

// ---- text ----
Check(PlynlingPassions.Normalize("  L'Astronomie ! ") == "astronomie", "Normalize strips article, accents, punctuation");
Check(PlynlingPassions.Normalize("Les Trains à vapeur") == "trains a vapeur", "Normalize multiword");
Check(PlynlingPassions.Clean(" a\n  b ") == "a b", "Clean collapses whitespace");
Check(PlynlingPassions.Clean(null) == "", "Clean(null)");
Check(PlynlingPassions.ContainsLink("voir https://x.y") && PlynlingPassions.ContainsLink("discord.gg/abc") && !PlynlingPassions.ContainsLink("la pêche"), "ContainsLink");
Check(PlynlingPassions.Resolve("les trains à vapeur") is null, "custom stays custom");
Check(PlynlingPassions.Resolve("Cuisiner des gâteaux") == PlynlingPassion.Cooking, "keyword match");
Check(PlynlingPassions.Resolve("les jeux vidéo") == PlynlingPassion.Gaming, "multiword keyword");
Check(PlynlingPassions.FromText("Les trains").SameAs(PlynlingPassions.FromText("les TRAINS !")), "custom SameAs after normalising");
Check(!PlynlingPassions.FromText("les trains").SameAs(PlynlingPassions.FromText("les avions")), "different custom");

// ---- a Plynling's passions ----
var p = new Plynling { Id = 1, Passion = PlynlingPassion.Cooking };
Check(PlynlingPassions.Of(p).Count == 1, "innate only");
p.TaughtPassion = "la pâtisserie";
Check(PlynlingPassions.Of(p).Count == 1, "taught resolving to the innate one is deduplicated");
p.TaughtPassion = "les trains";
Check(PlynlingPassions.Of(p).Count == 2 && PlynlingPassions.Taught(p)!.Custom == "les trains", "innate + custom");
Check(PlynlingPassions.FromText("x <@123>").Display() == $"« {PlynlingCardUi.SafeName("x <@123>")} »", "Display sanitises custom text");

HarnessMore.Run(Check);   // later tasks add checks in HarnessMore.cs

Console.WriteLine(failures.Count == 0 ? "OK" : string.Join("\n", failures));
return failures.Count == 0 ? 0 : 1;
```

`<scratchpad>/passions-harness/HarnessMore.cs`:

```csharp
static class HarnessMore
{
    public static void Run(Action<bool, string> check) { }
}
```

(The harness uses `Plynling.Passion` / `TaughtPassion`: Step 3 adds all three properties to the model in this task. The migration for them comes in Task 2 — the build does not need it.)

- [ ] **Step 2: Run the harness to see it fail**

Run: `dotnet run --project "<scratchpad>/passions-harness"`
Expected: build errors — `PlynlingPassions` / `PlynlingPassion` not found.

- [ ] **Step 3: Add the enum and properties to `Models/Plynling.cs`**

After `public enum PlynlingGender { Male, Female }` add:

```csharp
// A Plynling's innate passion (Helpers/PlynlingPassions). Stored as an int: **append-only** —
// a value inserted in the middle would turn every later Plynling's passion into its neighbour's.
public enum PlynlingPassion { Cooking, Music, Gaming, Astronomy, Gardening, Rocks, Stories, Dance, Painting, Sport, Insects, Naps }
```

In `class Plynling`, after `public PlynlingGender Gender { get; set; }`:

```csharp
    // Rolled at adoption, never changes. Rows older than passions were backfilled from the id.
    public PlynlingPassion Passion { get; set; }
    // What its owner taught it (/plynling passion): free text, cleaned, hostile input like the name.
    public string? TaughtPassion { get; set; }
    // When it was last taught or cleared — the start of the change cooldown.
    public DateTimeOffset? TaughtPassionAt { get; set; }
```

- [ ] **Step 4: Create `Helpers/PlynlingPassions.cs`**

```csharp
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

public sealed record PassionInfo(PlynlingPassion Passion, string Emoji, string Label, string[] Keywords,
    string[] Openers, string[] SharedLines, string[] Activities);

/// <summary>
/// One passion a Plynling has: a catalog one (rich, hand-written lines) or a custom text taught by
/// its owner (generic templates). Exactly one of the two is set.
/// </summary>
public sealed record Passion(PlynlingPassion? Catalog, string? Custom)
{
    // Two passions are the same when both are the same catalog one, or both custom with equal
    // normalised text ("Les trains !" = "les trains").
    public string Key => Catalog is { } c ? "cat:" + c : "txt:" + PlynlingPassions.Normalize(Custom ?? "");
    public bool SameAs(Passion other) => Key == other.Key;

    /// <summary>For {P} and SYNCS's lines: « la cuisine », or the typed text in « guillemets ».</summary>
    public string Render() => Catalog is { } c ? PlynlingPassions.Info(c).Label : $"« {PlynlingCardUi.SafeName(Custom ?? "")} »";

    /// <summary>For the card: with the catalog emoji.</summary>
    public string Display() => Catalog is { } c ? $"{PlynlingPassions.Info(c).Emoji} {PlynlingPassions.Info(c).Label}" : Render();
}

/// <summary>
/// The passions. Pure: no database, no clock. An innate passion is one of the catalog below; a taught
/// one is free text, upgraded to a catalog passion when <see cref="Resolve"/> recognises it.
/// Lines are story templates (see <see cref="PlynlingVisitStory"/>): {S}/{L} speaker and listener,
/// {s:m|f}/{l:m|f} their agreements, {Ils}/{ils}, {p:m|f}. An opener is narration, then the speaker's
/// line, separated by one \n. Catalog lines are written for their passion and do not use {P}.
/// </summary>
public static class PlynlingPassions
{
    public static readonly TimeSpan TeachCooldown = TimeSpan.FromHours(24);

    // How often a shared passion, when there is one, is what they talk about.
    public const double SharedTopicChance = 0.6;

    public static readonly IReadOnlyList<PassionInfo> All = new PassionInfo[]
    {
        new(PlynlingPassion.Cooking, "🍳", "la cuisine",
            new[] { "cuisine", "cuisiner", "recette", "recettes", "patisserie", "gateau", "gateaux", "crepe", "crepes", "cuisson", "chef" },
            new[] { "{S} sort un petit carnet taché de farine.\nTu savais qu'une crêpe se retourne mieux d'un coup de poignet qu'à la spatule ?" },
            new[] { "{Ils} se mettent à échanger des recettes à toute vitesse, sans même finir leurs phrases." },
            new[] { "🥞 {Ils} se lancent dans des crêpes. La première est ratée, comme le veut la tradition." }),
        new(PlynlingPassion.Music, "🎵", "la musique",
            new[] { "musique", "chanson", "chansons", "chanter", "chant", "piano", "guitare", "violon", "flute", "batterie", "melodie" },
            new[] { "{S} tapote un rythme sur une pierre, l'air très concentré.\nÉcoute ça : si on tape deux fois plus vite, ça devient une autre chanson !" },
            new[] { "{Ils} se mettent à fredonner la même chanson, pile en même temps." },
            new[] { "🎶 {S} apprend un petit air à {L}, qui le massacre avec beaucoup d'enthousiasme." }),
        new(PlynlingPassion.Gaming, "🎮", "les jeux vidéo",
            new[] { "jeux video", "jeu video", "gaming", "console", "manette", "minecraft" },
            new[] { "{S} mime des boutons invisibles avec les pouces.\nJ'ai enfin battu le boss du troisième niveau. Sans perdre une seule vie. Enfin, presque." },
            new[] { "{Ils} se lancent dans un débat passionné sur le meilleur jeu de tous les temps. Personne ne gagne." },
            new[] { "🎮 {Ils} inventent un jeu vidéo sans écran : il suffit de crier les actions très fort." }),
        new(PlynlingPassion.Astronomy, "🔭", "l'astronomie",
            new[] { "astronomie", "etoile", "etoiles", "planete", "planetes", "lune", "espace", "galaxie", "cosmos", "constellation", "constellations" },
            new[] { "{S} lève les yeux vers le ciel, même en plein jour.\nTu savais qu'il y a des étoiles qu'on voit alors qu'elles n'existent plus ?" },
            new[] { "{Ils} comparent les constellations qu'{ils} ont inventées. Certaines se ressemblent beaucoup." },
            new[] { "🔭 {Ils} fabriquent un télescope avec une feuille roulée. On ne voit rien, mais c'est magnifique." }),
        new(PlynlingPassion.Gardening, "🌱", "le jardinage",
            new[] { "jardinage", "jardin", "jardiner", "plante", "plantes", "fleur", "fleurs", "potager", "graine", "graines" },
            new[] { "{S} montre fièrement une toute petite pousse dans un pot.\nElle est sortie ce matin. Je lui ai déjà trouvé un nom." },
            new[] { "{Ils} s'échangent des graines comme d'autres s'échangent des secrets." },
            new[] { "🌱 {Ils} plantent une graine ensemble et la regardent pousser. Elle ne pousse pas. {Ils} restent quand même." }),
        new(PlynlingPassion.Rocks, "🪨", "les cailloux",
            new[] { "caillou", "cailloux", "pierre", "pierres", "galet", "galets", "roche", "roches", "mineraux", "cristaux" },
            new[] { "{S} vide ses poches : une douzaine de cailloux roulent par terre.\nCelui-là, il est presque rond. Presque. C'est ce qui le rend spécial." },
            new[] { "{Ils} étalent leurs collections côte à côte. Le silence qui suit est plein de respect." },
            new[] { "🪨 {Ils} classent des cailloux par couleur, puis par taille, puis par « personnalité »." }),
        new(PlynlingPassion.Stories, "📚", "les histoires",
            new[] { "histoire", "histoires", "lecture", "lire", "livre", "livres", "conte", "contes", "roman", "romans" },
            new[] { "{S} serre un vieux livre tout contre {s:lui|elle}.\nJ'en suis au moment où le dragon avoue qu'il a peur du noir. Je ne m'en remets pas." },
            new[] { "{Ils} se racontent la fin de leurs livres préférés, en se coupant la parole." },
            new[] { "📖 {S} lit une histoire à voix haute ; {L} fait toutes les voix des méchants." }),
        new(PlynlingPassion.Dance, "💃", "la danse",
            new[] { "danse", "danser", "ballet", "valse", "tango" },
            new[] { "{S} fait trois pas de côté, un tour, et s'arrête net.\nJ'ai inventé un pas. Il n'a pas encore de nom. Ni de fin." },
            new[] { "Sans un mot, {ils} se mettent à danser le même pas. Ça marche du premier coup." },
            new[] { "💃 {S} apprend à {L} une petite danse. {L} marche sur tous les pieds disponibles." }),
        new(PlynlingPassion.Painting, "🎨", "la peinture",
            new[] { "peinture", "peindre", "dessin", "dessiner", "aquarelle", "tableau", "tableaux" },
            new[] { "{S} a de la peinture bleue jusqu'aux coudes.\nJ'essaie de peindre le vent. C'est plus dur que prévu." },
            new[] { "{Ils} comparent leurs taches de peinture comme des médailles." },
            new[] { "🎨 {Ils} peignent le portrait {p:l'un de l'autre|l'une de l'autre}. Personne n'est ressemblant." }),
        new(PlynlingPassion.Sport, "🏃", "le sport",
            new[] { "sport", "course", "courir", "football", "foot", "natation", "nager", "velo", "escalade", "tennis" },
            new[] { "{S} trottine sur place en parlant.\nJ'ai couru quatre tours ce matin. Cinq, si on compte celui où je me suis {s:perdu|perdue}." },
            new[] { "{Ils} se lancent un défi de course avant même d'avoir fini de parler." },
            new[] { "🏃 Course jusqu'au bout du chemin ! {S} part trop vite et s'essouffle à mi-parcours." }),
        new(PlynlingPassion.Insects, "🐞", "les insectes",
            new[] { "insecte", "insectes", "fourmi", "fourmis", "coccinelle", "coccinelles", "papillon", "papillons", "scarabee", "abeille", "abeilles" },
            new[] { "{S} s'accroupit devant une fourmi qui transporte une miette énorme.\nElle porte cinquante fois son poids. Moi, je ne porte même pas mon goûter jusqu'au bout." },
            new[] { "{Ils} s'allongent à plat ventre pour suivre une colonne de fourmis. Pendant très longtemps." },
            new[] { "🐞 {Ils} construisent un petit hôtel pour coccinelles. Il n'a pas encore de clients." }),
        new(PlynlingPassion.Naps, "😴", "les siestes",
            new[] { "sieste", "siestes", "dormir", "sommeil", "dodo", "roupiller" },
            new[] { "{S} bâille avant même d'avoir dit bonjour.\nJ'ai fait une sieste si réussie que j'en ai rêvé d'une autre, dedans." },
            new[] { "{Ils} tombent d'accord sur un point essentiel : la meilleure sieste, c'est la prochaine." },
            new[] { "😴 {Ils} testent tous les coins d'herbe pour trouver le meilleur endroit où faire la sieste. {Ils} s'endorment avant la fin." }),
    };

    // Pair activities, keyed in either order (see ComboFor). Beat 4 prefers these.
    private static readonly Dictionary<(PlynlingPassion, PlynlingPassion), string[]> Combos = new()
    {
        [Pair(PlynlingPassion.Cooking, PlynlingPassion.Music)] = new[] { "🎶 {Ils} composent une chanson sur les crêpes. Le refrain dit seulement « crêpe », mais avec beaucoup d'émotion." },
        [Pair(PlynlingPassion.Astronomy, PlynlingPassion.Naps)] = new[] { "🌌 {Ils} s'allongent pour regarder les étoiles. Au bout de la troisième, tout le monde dort." },
        [Pair(PlynlingPassion.Insects, PlynlingPassion.Gardening)] = new[] { "🐛 {Ils} font la visite du potager en saluant chaque insecte par son prénom." },
    };

    private static (PlynlingPassion, PlynlingPassion) Pair(PlynlingPassion a, PlynlingPassion b) => a <= b ? (a, b) : (b, a);

    public static string[]? ComboFor(PlynlingPassion a, PlynlingPassion b) => Combos.GetValueOrDefault(Pair(a, b));

    public static PassionInfo Info(PlynlingPassion passion) => All[(int)passion];

    public static PlynlingPassion RollInnate(Random rng) => (PlynlingPassion)rng.Next(All.Count);

    private static readonly Regex LeadingArticle = new(@"^(de la |de l'|du |des |les |le |la |l'|un |une )", RegexOptions.Compiled);
    private static readonly Regex NotWord = new(@"[^a-z0-9' ]+", RegexOptions.Compiled);
    private static readonly Regex Spaces = new(@"\s+", RegexOptions.Compiled);

    /// <summary>Lowercase, no accents, no leading article, no punctuation, single spaces.</summary>
    public static string Normalize(string text)
    {
        var decomposed = text.Replace('’', '\'').ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var bare = new StringBuilder(decomposed.Length);
        foreach (var ch in decomposed)
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark) bare.Append(ch);
        var s = Spaces.Replace(NotWord.Replace(bare.ToString(), " "), " ").Trim();
        s = LeadingArticle.Replace(s, "");          // the apostrophe is still there, so « l' » is caught
        return Spaces.Replace(s.Replace("'", " "), " ").Trim();
    }

    /// <summary>What gets stored: line breaks and runs of spaces collapsed, trimmed.</summary>
    public static string Clean(string? raw) => raw is null ? "" : Spaces.Replace(raw, " ").Trim();

    public static bool ContainsLink(string text)
    {
        var lower = text.ToLowerInvariant();
        return lower.Contains("http") || lower.Contains("www.") || lower.Contains("discord.gg");
    }

    /// <summary>The catalog passion a typed text names, if a keyword appears in it as a whole word.</summary>
    public static PlynlingPassion? Resolve(string text)
    {
        var norm = " " + Normalize(text) + " ";
        foreach (var info in All)
            foreach (var keyword in info.Keywords)
                if (norm.Contains(" " + keyword + " ")) return info.Passion;
        return null;
    }

    public static Passion FromText(string text) =>
        Resolve(text) is { } c ? new Passion(c, null) : new Passion(null, text);

    public static Passion? Taught(Plynling p) =>
        string.IsNullOrWhiteSpace(p.TaughtPassion) ? null : FromText(p.TaughtPassion);

    /// <summary>Its passions: the innate one, then the taught one unless it is the same.</summary>
    public static IReadOnlyList<Passion> Of(Plynling p)
    {
        var innate = new Passion(p.Passion, null);
        return Taught(p) is { } taught && !taught.SameAs(innate) ? new[] { innate, taught } : new[] { innate };
    }
}
```

- [ ] **Step 5: Build the bot, then run the harness**

Run: `dotnet build` in `ProjectSYNCS/` → 0 errors, 0 warnings.
Run: `dotnet run --project "<scratchpad>/passions-harness"` → `OK`.

- [ ] **Step 6: Checkpoint.** Do not commit; tell the owner Task 1 is ready.

---

### Task 2: Storage — migration, adoption roll, input cap

**Files:**
- Modify: `ProjectSYNCS/Helpers/InputCaps.cs`
- Modify: `ProjectSYNCS/Services/PlynlingService.cs:78-101` (`AdoptAsync`)
- Create: `ProjectSYNCS/Migrations/<timestamp>_AddPlynlingPassions.cs` (generated, then edited)

**Interfaces:**
- Consumes: `Plynling.Passion/TaughtPassion/TaughtPassionAt`, `PlynlingPassions.RollInnate(Random)` (Task 1)
- Produces: `InputCaps.Passion` (`const int`, 40); `AdoptAsync(..., PlynlingGender? gender = null, PlynlingPassion? passion = null)`

- [ ] **Step 1: Add the cap to `InputCaps.cs`** (after `PlynlingName`):

```csharp
    /// <summary>
    /// A passion taught to a Plynling (/plynling passion). Shown on the card and inside visit
    /// story lines, so it stays short enough to read as a phrase, not a paragraph.
    /// </summary>
    public const int Passion = 40;
```

- [ ] **Step 2: Roll the innate passion at adoption.** In `PlynlingService.AdoptAsync`, change the signature and the creation:

```csharp
    public async Task<(AdoptOutcome Outcome, Plynling? Plynling)> AdoptAsync(
        ulong guildId, ulong ownerId, string name, PlynlingSpecies species, DateTimeOffset now,
        PlynlingGender? gender = null, PlynlingPassion? passion = null)
    {
        var current = await GetCurrentAsync(guildId, ownerId, now);
        if (current is { DiedAt: null }) return (AdoptOutcome.AlreadyHasOne, current);

        // Rolled here unless given, so PlynlingLife.Create stays pure and the harnesses
        // can pin a gender or a passion.
        var plynling = PlynlingLife.Create(guildId, ownerId, name, species, gender ?? PlynlingCatalog.RollGender(), now);
        plynling.Passion = passion ?? PlynlingPassions.RollInnate(Random.Shared);
        _db_context.Plynlings.Add(plynling);
```

(the rest of the method is unchanged).

- [ ] **Step 3: Generate the migration**

Run in `ProjectSYNCS/`: `dotnet ef migrations add AddPlynlingPassions`
Expected: a new `Migrations/<timestamp>_AddPlynlingPassions.cs` with three `AddColumn` calls on the Plynlings table (`Passion` INTEGER NOT NULL default 0, `TaughtPassion` TEXT NULL, `TaughtPassionAt` TEXT NULL). Read it and confirm the table name it uses.

- [ ] **Step 4: Add the one-off backfill.** At the end of `Up`, after the three `AddColumn` calls (use the table name the generated code uses — expected `Plynlings`):

```csharp
            // Plynlings older than passions get one, once, from their id: stable, and no runtime
            // roll. New ones roll theirs at adoption. The third migration here that carries data —
            // see CLAUDE.md; nothing is wiped, a required trait is filled in.
            migrationBuilder.Sql("UPDATE \"Plynlings\" SET \"Passion\" = (\"Id\" * 5 + 1) % 12;");
```

`Down` stays as generated (drops the columns).

- [ ] **Step 5: Build.** `dotnet build` → 0 errors, 0 warnings. Harness still `OK`.

- [ ] **Step 6: Checkpoint.** Do not commit.

---

### Task 3: Show passions on the card and in the journal

**Files:**
- Modify: `ProjectSYNCS/Helpers/PlynlingCardUi.cs:27-40` (`Heading`)
- Modify: `ProjectSYNCS/Helpers/PlynlingJournalUi.cs:7-12` (enum), `:45-47` (`Line`)
- Modify: `<scratchpad>/passions-harness/HarnessMore.cs`

**Interfaces:**
- Consumes: `PlynlingPassions.Of(Plynling)`, `Passion.Display()` (Task 1)
- Produces: `JournalKind.LearnedPassion` (appended last), `PlynlingCardUi.PassionsLine(Plynling)`

- [ ] **Step 1: Add harness checks** — replace `HarnessMore.cs` with:

```csharp
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

static class HarnessMore
{
    public static void Run(Action<bool, string> check)
    {
        var p = new Plynling { Id = 7, Name = "Pistache", Passion = PlynlingPassion.Music, TaughtPassion = "les trains" };
        check(PlynlingCardUi.PassionsLine(p) == "💭 🎵 la musique · « les trains »", "card passions line");
        check(PlynlingJournalUi.Line(JournalKind.LearnedPassion, "les trains", "Pistache", PlynlingGender.Female)
                  .Contains("prise de passion"), "journal line agrees (F)");
        check((int)JournalKind.LearnedPassion == Enum.GetValues<JournalKind>().Length - 1, "LearnedPassion is appended last");
    }
}
```

- [ ] **Step 2: Run the harness** → compile error (`PassionsLine`, `LearnedPassion` missing).

- [ ] **Step 3: Card.** In `PlynlingCardUi`, add:

```csharp
    // Its passions, innate then taught, on one line of the heading — no extra component.
    public static string PassionsLine(Plynling p) =>
        "💭 " + string.Join(" · ", PlynlingPassions.Of(p).Select(x => x.Display()));
```

and in `Heading`, insert it after the accessory, before the partner:

```csharp
               accessory +
               "\n" + PassionsLine(p) +
               (partnerName is null ? "" : $"\n💞 En couple avec **{SafeName(partnerName)}**");
```

- [ ] **Step 4: Journal.** Append to the enum (after `Grieving`, same line or a new line with a comment):

```csharp
    BecameFriends, BecameBestFriends, BecameLovers, BecameRivals, BecameEnemies, Heartbroken, BrokeUp, Grieving,
    // detail: the taught text
    LearnedPassion,
```

and in `Line`, before `_ => "…"`:

```csharp
        JournalKind.LearnedPassion => $"💭 S'est {g.Agree("pris", "prise")} de passion pour « {PlynlingCardUi.SafeName(detail ?? "?")} ».",
```

- [ ] **Step 5: Build + harness** → 0 errors / 0 warnings; `OK`.

- [ ] **Step 6: Checkpoint.** Do not commit.

---

### Task 4: `/plynling passion` and `/admin plynling passion-reset`

**Files:**
- Create: `ProjectSYNCS/Interactions/Modals/PassionModal.cs`
- Modify: `ProjectSYNCS/Services/PlynlingService.cs` (two methods, one enum)
- Modify: `ProjectSYNCS/Helpers/PlynlingText.cs`
- Modify: `ProjectSYNCS/Services/BotResponses.cs` (two pools + index comment)
- Modify: `ProjectSYNCS/Commands/PlynlingModule.cs` (command + modal handler)
- Modify: `ProjectSYNCS/Commands/AdminModule.cs` (`PlynlingAdminModule`)

**Interfaces:**
- Consumes: `InputCaps.Passion`, `PlynlingPassions.Clean/ContainsLink/Taught/TeachCooldown`, `JournalKind.LearnedPassion`
- Produces:
  - `enum TeachOutcome { Taught, Cleared, NoPlynling, Cooldown }`
  - `Task<(TeachOutcome Outcome, Plynling? Plynling, DateTimeOffset? ReadyAt)> PlynlingService.TeachPassionAsync(int plynlingId, ulong ownerId, string? text, DateTimeOffset now)`
  - `Task<Plynling?> PlynlingService.ResetPassionAsync(ulong guildId, ulong ownerId, DateTimeOffset now)`
  - `BotResponses.PlynlingPassionTaughtLines` (`{0}` name, `{1}` rendered passion), `BotResponses.PlynlingStaffPassionResetDms` (`{0}` name)

- [ ] **Step 1: Modal DTO** — `Interactions/Modals/PassionModal.cs`:

```csharp
using Discord.Interactions;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Interactions.Modals;

// /plynling passion. Built by hand in PlynlingModule.PassionAsync to pre-fill the current
// passion — keep the custom id "passion", the label and the cap in sync with that builder.
public class PassionModal : IModal
{
    public string Title => "Sa passion";

    [InputLabel("Quelle passion veux-tu lui apprendre ?")]
    [RequiredInput(false)]
    [ModalTextInput("passion", placeholder: "Avec son article : la pêche, les trains…", maxLength: InputCaps.Passion)]
    public string Passion { get; set; } = string.Empty;
}
```

- [ ] **Step 2: Service.** In `PlynlingService.cs`, next to the other outcome enums at the top:

```csharp
public enum TeachOutcome { Taught, Cleared, NoPlynling, Cooldown }
```

and after `RenameAsync`:

```csharp
    // /plynling passion: set (or clear, when text is null) what its owner taught it. The cooldown
    // runs from the last change either way, so set/clear cannot be toggled for spam.
    public async Task<(TeachOutcome Outcome, Plynling? Plynling, DateTimeOffset? ReadyAt)> TeachPassionAsync(
        int plynlingId, ulong ownerId, string? text, DateTimeOffset now)
    {
        var plynling = await GetByIdAsync(plynlingId, now);
        if (plynling is null || plynling.OwnerId != ownerId || plynling.DiedAt is not null)
            return (TeachOutcome.NoPlynling, null, null);
        if (plynling.TaughtPassionAt is { } at && at + PlynlingPassions.TeachCooldown > now)
            return (TeachOutcome.Cooldown, plynling, at + PlynlingPassions.TeachCooldown);

        plynling.TaughtPassion = text;
        plynling.TaughtPassionAt = now;
        if (text is not null) await AddMomentAsync(plynling, JournalKind.LearnedPassion, text, now);
        await _db_context.SaveChangesAsync();
        return (text is null ? TeachOutcome.Cleared : TeachOutcome.Taught, plynling, null);
    }

    // Staff moderation, like RenameAsync: reaches the latest grave too, since its card shows it.
    public async Task<Plynling?> ResetPassionAsync(ulong guildId, ulong ownerId, DateTimeOffset now)
    {
        var plynling = await GetShownAsync(guildId, ownerId, now);
        if (plynling is null) return null;
        plynling.TaughtPassion = null;
        plynling.TaughtPassionAt = null;
        await _db_context.SaveChangesAsync();
        return plynling;
    }
```

- [ ] **Step 3: Texts.** In `PlynlingText.cs`, near the other refusals:

```csharp
    public static string PassionCooldown(DateTimeOffset ready) =>
        $"Tu lui as appris une passion il y a peu. Tu pourras en changer <t:{ready.ToUnixTimeSeconds()}:R>.";
    public const string PassionTooShort = "Une passion d'une seule lettre ? Essaie avec au moins deux.";
    public const string PassionLink = "Pas de lien dans une passion, merci.";
    public static string PassionCleared(string name) =>
        $"C'est effacé : **{name}** n'a plus que sa passion de naissance.";
    public static string PassionResetDone(string name) =>
        $"💭 La passion apprise de **{name}** a été effacée.";
```

- [ ] **Step 4: Pools.** In `BotResponses.cs`, after `PlynlingPetLines`:

```csharp
    // /plynling passion, on the card. {0} = name, {1} = the passion (« la cuisine », or the typed
    // text in « guillemets »). « pour {1} » is safe: « pour » never contracts with an article.
    public static readonly GenderedLines PlynlingPassionTaughtLines = new(
        M: new[]
        {
            "**{0}** s'est pris de passion pour {1}. Je ne comprends pas, mais je respecte. ♡",
            "Nouvelle obsession pour **{0}** : {1}. Prépare-toi à en entendre parler tous les jours (¬_¬)",
            "**{0}** ne parle plus que de ça : {1}. Il est adorable. Un peu fatigant. Adorable ✨",
        },
        F: new[]
        {
            "**{0}** s'est prise de passion pour {1}. Je ne comprends pas, mais je respecte. ♡",
            "Nouvelle obsession pour **{0}** : {1}. Prépare-toi à en entendre parler tous les jours (¬_¬)",
            "**{0}** ne parle plus que de ça : {1}. Elle est adorable. Un peu fatigante. Adorable ✨",
        });
```

and after `PlynlingStaffRenameDms`:

```csharp
    // DM when staff clear a taught passion. {0} = name.
    public static readonly GenderedLines PlynlingStaffPassionResetDms = new(
        M: new[] { "💭 Le staff a effacé la passion que tu avais apprise à **{0}**. Tu peux lui en apprendre une autre avec `/plynling passion`." },
        F: new[] { "💭 Le staff a effacé la passion que tu avais apprise à **{0}**. Tu peux lui en apprendre une autre avec `/plynling passion`." });
```

In the index comment at the top of the file, add `PlynlingPassionTaughtLines ..... /plynling passion` under the Plynlings block and append `· PlynlingStaffPassionResetDms` to the staff DMs line.

- [ ] **Step 5: Command + modal handler.** In `PlynlingModule.cs`, after `WardrobeAsync`. Build the modal by hand the same way `ScheduleModule.BuildEditModal` does (open it and mirror its `ModalBuilder` / `AddTextInput` calls exactly — that is the known-good pattern for Discord.Net 3.20 here):

```csharp
    // Free text, like a Tomodachi Life word: it only ever appears through generic story
    // templates, or upgraded to a catalog passion when PlynlingPassions.Resolve recognises it.
    [SlashCommand("passion", "Apprendre une passion à ton Plynling (ou effacer celle apprise)")]
    public async Task PassionAsync()
    {
        var now = DateTimeOffset.UtcNow;
        var plynling = await _plynlings.GetCurrentAsync(Context.Guild.Id, Context.User.Id, now);
        if (plynling is null || plynling.DiedAt is not null)
        {
            await RespondAsync(PlynlingText.NoPlynling, ephemeral: true);
            return;
        }
        if (plynling.TaughtPassionAt is { } at && at + PlynlingPassions.TeachCooldown > now)
        {
            await RespondAsync(PlynlingText.PassionCooldown(at + PlynlingPassions.TeachCooldown), ephemeral: true);
            return;
        }
        // Hand-built to pre-fill it; binds to PassionModal — keep the two in sync.
        var modal = new ModalBuilder()
            .WithTitle("Sa passion")
            .WithCustomId($"plyn:passion:{plynling.Id}")
            .AddTextInput("Quelle passion veux-tu lui apprendre ?", "passion", TextInputStyle.Short,
                placeholder: "Avec son article : la pêche, les trains…", maxLength: InputCaps.Passion,
                required: false, value: plynling.TaughtPassion);
        await RespondWithModalAsync(modal.Build());
    }

    [ModalInteraction("plyn:passion:*", ignoreGroupNames: true)]
    public async Task OnPassionTaughtAsync(string idStr, PassionModal modal)
    {
        var now = DateTimeOffset.UtcNow;
        var cleaned = PlynlingPassions.Clean(modal.Passion);
        if (cleaned.Length == 1)
        {
            await RespondAsync(PlynlingText.PassionTooShort, ephemeral: true);
            return;
        }
        if (PlynlingPassions.ContainsLink(cleaned))
        {
            await RespondAsync(PlynlingText.PassionLink, ephemeral: true);
            return;
        }

        var text = cleaned.Length == 0 ? null : cleaned;
        var (outcome, plynling, ready) = int.TryParse(idStr, out var id)
            ? await _plynlings.TeachPassionAsync(id, Context.User.Id, text, now)
            : (TeachOutcome.NoPlynling, null, null);
        switch (outcome)
        {
            case TeachOutcome.NoPlynling:
                await RespondAsync(PlynlingText.NoPlynling, ephemeral: true);
                return;
            case TeachOutcome.Cooldown:
                await RespondAsync(PlynlingText.PassionCooldown(ready!.Value), ephemeral: true);
                return;
            case TeachOutcome.Cleared:
                await RespondAsync(PlynlingText.PassionCleared(PlynlingCardUi.SafeName(plynling!.Name)), ephemeral: true);
                return;
        }

        var line = string.Format(_picker.Pick(Context.Channel.Id, BotResponses.PlynlingPassionTaughtLines.For(plynling!.Gender)),
            PlynlingCardUi.SafeName(plynling.Name), PlynlingPassions.Taught(plynling)!.Render());
        await RespondCardAsync(plynling, now, line);
    }
```

If `AddTextInput`'s parameter names differ from the ones above in this Discord.Net version, match `ScheduleModule.BuildEditModal` — do not guess.

- [ ] **Step 6: Admin reset.** In `AdminModule.PlynlingAdminModule`, after `ResurrectAsync`:

```csharp
        // Staff only: the taught passion is free text shown publicly (card, visit stories), so
        // clearing an offensive one is moderation — the same reasoning as rename.
        [SlashCommand("passion-reset", "Effacer la passion apprise au Plynling de quelqu'un")]
        public async Task PassionResetAsync([Summary("user", "À qui est le Plynling")] IUser user)
        {
            if (!SessionPermissions.IsStaff(Context.User))
            {
                await RespondAsync(PlynlingText.StaffOnly, ephemeral: true);
                return;
            }

            var plynling = await _plynlings.ResetPassionAsync(Context.Guild.Id, user.Id, DateTimeOffset.UtcNow);
            if (plynling is null)
            {
                await RespondAsync(PlynlingText.NoneFor(user.Id), ephemeral: true, allowedMentions: AllowedMentions.None);
                return;
            }

            await RespondAsync(PlynlingText.PassionResetDone(PlynlingCardUi.SafeName(plynling.Name)),
                ephemeral: true, allowedMentions: AllowedMentions.None);
            if (user.Id != Context.User.Id)   // after the reply — see PlynlingModule.FreezeAsync
                await _announcer.DmOwnerAsync(plynling.OwnerId, string.Format(
                    _picker.Pick(plynling.OwnerId, BotResponses.PlynlingStaffPassionResetDms.For(plynling.Gender)),
                    PlynlingCardUi.SafeName(plynling.Name)));
        }
```

- [ ] **Step 7: Build.** `dotnet build` → 0 errors, 0 warnings. Harness `OK`.

- [ ] **Step 8: Gender-word check** on the two new pools (the plynlingui rule). Run:

```bash
python - <<'EOF'
import re
s=open(r"C:/Users/c235773/Desktop/Sources - RME/Discord Bots/SessionOrganizer/ProjectSYNCS/Services/BotResponses.cs",encoding="utf-8").read()
for name in ["PlynlingPassionTaughtLines","PlynlingStaffPassionResetDms"]:
    i=s.index(name+" = new"); j=s.index("});",i)
    m,f=s[i:j].split("F: new[]")
    print(name,[w for w in [r"\bil\b",r"-le\b",r"\bmort\b"] if re.search(w,f)],[w for w in [r"\belle\b",r"-la\b",r"\bmorte\b"] if re.search(w,m,re.I)])
EOF
```

Expected: both lists empty for both pools.

- [ ] **Step 9: Checkpoint.** Do not commit.

---

### Task 5: The five-beat story engine

**Files:**
- Modify: `ProjectSYNCS/Helpers/PlynlingVisitStory.cs`
- Modify: `<scratchpad>/passions-harness/HarnessMore.cs`

**Interfaces:**
- Consumes: `Passion`, `PlynlingPassions.Of/Info/ComboFor/SharedTopicChance` (Task 1), `VisitOutcome` (existing)
- Produces:
  - `record VisitCast(string Name, string Sprite, string SpeciesName, PlynlingGender Gender, IReadOnlyList<Passion> Passions)`
  - `IReadOnlyDictionary<(VisitMood Mood, bool Shared), string[]> Reactions`
  - `string[] CustomOpeners`, `IReadOnlyDictionary<VisitMood, string[]> CustomActivities` (no `Conflict` key)
  - `static Passion PickSubject(IReadOnlyList<Passion> speaker, IReadOnlyList<Passion> listener, Random rng)`
  - `static string Expand(string template, string a, PlynlingGender ga, string b, PlynlingGender gb, bool visitorSpeaks = true, string? passion = null)`
  - `VisitStory.Beats.Count == 5`

- [ ] **Step 1: Add harness checks.** Append inside `HarnessMore.Run`:

```csharp
        // ---- story engine ----
        string Leftover(string s) => System.Text.RegularExpressions.Regex.Match(s, @"\{[^}]*\}").Value;
        var genders = new[] { PlynlingGender.Male, PlynlingGender.Female };

        // Every template, expanded for all four gender pairs and both speakers, leaves no {…}.
        IEnumerable<string> Templates() =>
            PlynlingVisitStory.Arrivals.Values.SelectMany(x => x)
            .Concat(PlynlingVisitStory.Activities.Values.SelectMany(x => x))
            .Concat(PlynlingVisitStory.Exchanges.Values.SelectMany(x => x))
            .Concat(PlynlingVisitStory.Departures.Values.SelectMany(x => x))
            .Concat(PlynlingVisitStory.Reactions.Values.SelectMany(x => x))
            .Concat(PlynlingVisitStory.CustomOpeners)
            .Concat(PlynlingVisitStory.CustomActivities.Values.SelectMany(x => x))
            .Concat(PlynlingPassions.All.SelectMany(i => i.Openers.Concat(i.SharedLines).Concat(i.Activities)))
            .Concat(Enum.GetValues<PlynlingPassion>().SelectMany(a => Enum.GetValues<PlynlingPassion>()
                .Select(b => PlynlingPassions.ComboFor(a, b) ?? Array.Empty<string>())).SelectMany(x => x));
        foreach (var t in Templates())
            foreach (var ga in genders) foreach (var gb in genders) foreach (var speaks in new[] { true, false })
            {
                var x = PlynlingVisitStory.Expand(t, "Aa", ga, "Bb", gb, speaks, "« les trains »");
                check(Leftover(x) == "", $"leftover {Leftover(x)} in: {t}");
            }

        // Two-line pools: exactly one \n.
        foreach (var t in PlynlingVisitStory.Reactions.Values.SelectMany(x => x).Concat(PlynlingVisitStory.CustomOpeners))
            check(t.Split('\n').Length == 2, $"two lines: {t}");

        // {P} never right after de / à.
        foreach (var t in Templates())
            check(!System.Text.RegularExpressions.Regex.IsMatch(t, @"\b(de|à|d')\s*\{P\}"), $"{{P}} after de/à: {t}");

        // Every mood has reactions both ways, and custom activities except Conflict.
        foreach (var mood in Enum.GetValues<VisitMood>())
        {
            check(PlynlingVisitStory.Reactions[(mood, true)].Length > 0 && PlynlingVisitStory.Reactions[(mood, false)].Length > 0, $"reactions for {mood}");
            check(mood == VisitMood.Conflict || PlynlingVisitStory.CustomActivities[mood].Length > 0, $"custom activities for {mood}");
        }

        // Typed text is never interpreted as a template.
        check(PlynlingVisitStory.Expand("{P}", "Aa", PlynlingGender.Male, "Bb", PlynlingGender.Male, true, "{A}") == "{A}", "{P} inserted last");

        // Subject picking: always one of the speaker's; a shared one wins often.
        var rng = new Random(1);
        var cooking = new Passion(PlynlingPassion.Cooking, null);
        var music = new Passion(PlynlingPassion.Music, null);
        var trains = new Passion(null, "les trains");
        int sharedPicks = 0;
        for (var i = 0; i < 1000; i++)
        {
            var s = PlynlingVisitStory.PickSubject(new[] { cooking, trains }, new[] { music, new Passion(null, "Les Trains") }, rng);
            check(s == cooking || s == trains, "subject is the speaker's");
            if (s == trains) sharedPicks++;
        }
        check(sharedPicks > 700, $"shared subject picked often ({sharedPicks}/1000)");
```

(Full `Build` calls need `Plynling` rows with species/sprites; the template checks above cover every line `Build` can emit, and `Build` itself is exercised in the dev guild in Task 8.)

- [ ] **Step 2: Run the harness** → compile errors (`Reactions`, `CustomOpeners`, `CustomActivities`, `PickSubject`, `Expand` overload).

- [ ] **Step 3: `VisitCast` carries passions.** Replace the record:

```csharp
public sealed record VisitCast(string Name, string Sprite, string SpeciesName, PlynlingGender Gender, IReadOnlyList<Passion> Passions);
```

and `Cast`:

```csharp
    private static VisitCast Cast(Plynling p, DateTimeOffset now) => new(
        PlynlingCardUi.SafeName(p.Name),
        PlynlingArt.Sprite(p.Species, PlynlingLife.Stage(p, now), PlynlingLife.Mood(p, now)),
        PlynlingCatalog.Info(p.Species).Name,
        p.Gender,
        PlynlingPassions.Of(p));
```

- [ ] **Step 4: New pools.** After `Departures`, add:

```csharp
    // Beat 3: the listener reacts to the subject, then the speaker answers — one \n between them.
    // Keyed by mood and by whether the listener shares the passion. Generic, so {P} stands alone
    // (after a colon, « pour », « sur », or as a subject — never after « de » or « à »).
    public static readonly IReadOnlyDictionary<(VisitMood Mood, bool Shared), string[]> Reactions =
        new Dictionary<(VisitMood, bool), string[]>
        {
            [(VisitMood.Acquaintances, false)] = new[] { "Oh. {P}, alors. C'est… intéressant.\nTu dis ça poliment, mais je vais quand même t'en parler." },
            [(VisitMood.Acquaintances, true)] = new[] { "Attends, toi aussi ? {P} ?\nJe croyais être {s:le seul|la seule} ! On devrait se voir plus souvent." },
            [(VisitMood.Friends, false)] = new[] { "Je n'y connais rien. Explique-moi tout !\nTout ? Installe-toi, ça va prendre la journée." },
            [(VisitMood.Friends, true)] = new[] { "{P} ! C'est pour ça qu'on s'entend si bien !\nJe le savais depuis le début." },
            [(VisitMood.BestFriends, false)] = new[] { "Tu m'en parles tous les jours, tu sais.\nEt tu m'écoutes tous les jours. C'est pour ça que je t'aime bien." },
            [(VisitMood.BestFriends, true)] = new[] { "On en parle encore ? Toujours {P} ?\nToujours. Et toi aussi, avoue." },
            [(VisitMood.Lovers, false)] = new[] { "Je pourrais t'écouter en parler pendant des heures.\nTu dis ça parce que tu regardes mes yeux, pas parce que tu écoutes." },
            [(VisitMood.Lovers, true)] = new[] { "Tu sais ce que j'aime encore plus que ça ?\nNon ?… Oh. Oh ! 💕" },
            [(VisitMood.Rivals, false)] = new[] { "Pff. {P}, ce n'est même pas difficile.\nAlors montre-moi, si c'est si facile." },
            [(VisitMood.Rivals, true)] = new[] { "J'en sais bien plus que toi sur le sujet.\nOn parie ?" },
            [(VisitMood.Conflict, false)] = new[] { "Passionnant. Vraiment.\nTu pourrais au moins faire semblant." },
            [(VisitMood.Conflict, true)] = new[] { "Tu ne vas pas me l'apprendre, j'en fais depuis bien avant toi.\nC'est ça. Continue de te vanter." },
        };

    // Beat 2 for a custom (typed) passion: narration, then the speaker's line.
    public static readonly string[] CustomOpeners =
    {
        "{S} se redresse, l'air très sérieux.\nJe dois t'avouer quelque chose. Ma passion ? {P}.",
        "{S} sort un carnet couvert de dessins.\nTout ça, c'est pour {P}. Oui, tout.",
    };

    // Beat 4 for a custom subject. No Conflict: in a conflict the activity is always the squabble.
    public static readonly IReadOnlyDictionary<VisitMood, string[]> CustomActivities = new Dictionary<VisitMood, string[]>
    {
        [VisitMood.Acquaintances] = new[] { "🗒️ {S} explique les bases à {L}. Sujet du jour : {P}. {L} hoche la tête très souvent." },
        [VisitMood.Friends] = new[] { "🎉 {Ils} inventent un jeu sur le moment. Thème imposé : {P}." },
        [VisitMood.BestFriends] = new[] { "📜 {Ils} fondent un club secret. Thème : {P}. Membres : deux. Mot de passe : secret." },
        [VisitMood.Lovers] = new[] { "💕 {S} fabrique un petit cadeau pour {L}, sur un thème bien précis : {P}." },
        [VisitMood.Rivals] = new[] { "⚡ Concours improvisé, un seul sujet : {P}. {Ils} se déclarent {p:tous|toutes} les deux {p:vainqueurs|gagnantes}." },
    };
```

- [ ] **Step 5: `PickSubject` and `Expand`.** Replace the `Choice` regex and `Expand` with:

```csharp
    /// <summary>
    /// What the speaker talks about: a passion both share, <see cref="PlynlingPassions.SharedTopicChance"/>
    /// of the time when there is one, else any of the speaker's own.
    /// </summary>
    public static Passion PickSubject(IReadOnlyList<Passion> speaker, IReadOnlyList<Passion> listener, Random rng)
    {
        var shared = speaker.Where(p => listener.Any(p.SameAs)).ToList();
        if (shared.Count > 0 && rng.NextDouble() < PlynlingPassions.SharedTopicChance) return shared[rng.Next(shared.Count)];
        return speaker[rng.Next(speaker.Count)];
    }

    private static readonly Regex Choice = new(@"\{([abpsl]):([^|}]*)\|([^}]*)\}", RegexOptions.Compiled);

    /// <summary>
    /// Fills a template's names, pronouns and agreements. {S}/{L} are the speaker and the listener
    /// (the visitor speaks when <paramref name="visitorSpeaks"/>). {P} is inserted last, so text a
    /// person typed is never read as a template.
    /// </summary>
    public static string Expand(string template, string a, PlynlingGender ga, string b, PlynlingGender gb,
        bool visitorSpeaks = true, string? passion = null)
    {
        var girls = ga == PlynlingGender.Female && gb == PlynlingGender.Female;
        var (s, gs, l, gl) = visitorSpeaks ? (a, ga, b, gb) : (b, gb, a, ga);
        var text = Choice.Replace(template, m =>
        {
            var female = m.Groups[1].Value switch
            {
                "a" => ga == PlynlingGender.Female,
                "b" => gb == PlynlingGender.Female,
                "s" => gs == PlynlingGender.Female,
                "l" => gl == PlynlingGender.Female,
                _ => girls,
            };
            return female ? m.Groups[3].Value : m.Groups[2].Value;
        });
        text = text
            .Replace("{A}", $"**{a}**").Replace("{B}", $"**{b}**")
            .Replace("{S}", $"**{s}**").Replace("{L}", $"**{l}**")
            .Replace("{ils}", girls ? "elles" : "ils").Replace("{Ils}", girls ? "Elles" : "Ils");
        return passion is null ? text : text.Replace("{P}", passion);
    }
```

- [ ] **Step 6: `Build` — five beats.** Replace the body of `Build` from `var place = …` through `return new VisitStory(…)` with:

```csharp
        var place = PlaceFor(now, rng);
        var mood = MoodFor(outcome.GoodScene, outcome.After);

        // Who talks, and about what.
        var visitorSpeaks = rng.Next(2) == 0;
        var (speaker, listener) = visitorSpeaks ? (visitor, host) : (host, visitor);
        var subject = PickSubject(speaker.Passions, listener.Passions, rng);
        var shared = listener.Passions.Any(subject.SameAs);
        var info = subject.Catalog is { } c ? PlynlingPassions.Info(c) : null;
        string P(string template) => Expand(template, visitor.Name, visitor.Gender, host.Name, host.Gender, visitorSpeaks, subject.Render());
        string Said(string who, string line) => $"💬 **{who}** : {line}";

        var opener = P(pick(info?.Openers ?? CustomOpeners)).Split('\n');
        var reaction = P(pick(Reactions[(mood, shared)])).Split('\n');
        var sharedLine = shared && info is not null ? "\n" + P(pick(info.SharedLines)) : "";

        var exchange = X(pick(Exchanges[mood])).Split('\n');
        var beats = new[]
        {
            $"*{pick(place.Scenes)}*\n{X(pick(Arrivals[mood]))}",
            $"{opener[0]}\n{Said(speaker.Name, opener[1])}",
            $"{Said(listener.Name, reaction[0])}\n{Said(speaker.Name, reaction[1])}{sharedLine}",
            $"{P(pick(ActivityPool(mood, info, listener)))}\n{Said(visitor.Name, exchange[0])}\n{Said(host.Name, exchange[1])}",
            string.IsNullOrWhiteSpace(outcomeLines) ? X(pick(Departures[mood])) : $"{X(pick(Departures[mood]))}\n{outcomeLines}",
        };
        var heading = $"{place.Emoji} {place.Name.Replace("{B}", host.Name)}";
        return new VisitStory("", heading, beats, visitor, host, PlynlingCatalog.Info(outcome.Host.Species).Accent);
```

and add, below `Build`:

```csharp
    // Beat 4: a combo of the subject with one of the listener's catalog passions, else the subject's
    // own activities, else the custom ones for this mood. In a conflict, always the squabble.
    private static string[] ActivityPool(VisitMood mood, PassionInfo? subject, VisitCast listener)
    {
        if (mood == VisitMood.Conflict) return Activities[VisitMood.Conflict];
        if (subject is null) return CustomActivities[mood];
        var combos = listener.Passions
            .Where(p => p.Catalog is { } lc && lc != subject.Passion)
            .Select(p => PlynlingPassions.ComboFor(subject.Passion, p.Catalog!.Value))
            .OfType<string[]>()
            .SelectMany(x => x)
            .ToArray();
        return combos.Length > 0 ? combos : subject.Activities;
    }
```

Keep the existing `string X(string template) => Expand(template, visitor.Name, visitor.Gender, host.Name, host.Gender);` line above `var place` — it still serves arrival, exchange and departure (speaker-agnostic).

Also update the class doc comment's template paragraph to mention `{S}`/`{L}` (speaker/listener, `{s:m|f}`/`{l:m|f}`), `{P}` (the subject passion, inserted last) and the five beats.

- [ ] **Step 7: Build + harness** → 0 errors / 0 warnings; `OK`.

- [ ] **Step 8: Checkpoint.** Do not commit.

---

### Task 6: The story as a new message

**Files:**
- Modify: `ProjectSYNCS/Interactions/Components/PlynlingComponentHandler.cs:234-282`
- Modify: `ProjectSYNCS/Helpers/PlynlingText.cs`

**Interfaces:**
- Consumes: `VisitStories.Add`, `PlynlingPlayCards.BuildVisitStory/BuildKnockClosed`, `PlynlingVisitStory.Build` (Task 5)
- Produces: `PlynlingText.VisitAccepted(string visitor, string host)`; `PlayStoryAsync(IUserMessage message, VisitStory story)`

- [ ] **Step 1: Text.** In `PlynlingText.cs` next to `InviteExpired`:

```csharp
    // The knock once accepted: the story itself is posted as a new message under it.
    public static string VisitAccepted(string visitor, string host) =>
        $"✅ **{host}** a accueilli **{visitor}** — l'histoire est juste en dessous ↓";
```

- [ ] **Step 2: Accept handler.** Replace from the comment `// The visit is decided and saved; …` through the end of `PlayStoryAsync` with:

```csharp
        // The visit is decided and saved; what follows is only its telling. The knock closes in
        // place (that answers the click inside Discord's 3 s), and the story is a follow-up — a new
        // message at the bottom of the channel — whose later beats edit that follow-up.
        var channel = Context.Channel.Id;
        var story = _stories.Add(
            PlynlingVisitStory.Build(pair, PlynlingPlayCards.VisitOutcomeLines(pair), now, Random.Shared, pool => _picker.Pick(channel, pool)),
            Random.Shared);
        await component.UpdateAsync(m =>
        {
            m.Components = PlynlingPlayCards.BuildKnockClosed(PlynlingText.VisitAccepted(story.Visitor.Name, story.Host.Name));
            m.Flags = MessageFlags.ComponentsV2;
            m.AllowedMentions = AllowedMentions.None;
        });

        IUserMessage message;
        try
        {
            message = await component.FollowupAsync(components: PlynlingPlayCards.BuildVisitStory(story, 0, arrows: false),
                flags: MessageFlags.ComponentsV2, allowedMentions: AllowedMentions.None);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Visit story {Story}: could not be posted", story.Id);
            return;
        }
        _ = Task.Run(() => PlayStoryAsync(message, story));
    }

    // Beats 2 to 5, one edit of the follow-up each; the last carries the arrows. A failed edit is
    // retried once and then logged — the visit itself is already saved, so the worst case is a card
    // stuck on a beat. The interaction token keeps a follow-up editable for 15 minutes.
    private async Task PlayStoryAsync(IUserMessage message, VisitStory story)
    {
        for (var beat = 1; beat < story.Beats.Count; beat++)
        {
            await Task.Delay(BeatPause);
            var last = beat == story.Beats.Count - 1;
            var card = PlynlingPlayCards.BuildVisitStory(story, beat, arrows: last);
            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    await message.ModifyAsync(m =>
                    {
                        m.Components = card;
                        m.Flags = MessageFlags.ComponentsV2;
                        m.AllowedMentions = AllowedMentions.None;
                    });
                    break;
                }
                catch (Exception ex) when (attempt < 2)
                {
                    _logger.LogWarning(ex, "Visit story {Story}: beat {Beat} failed, retrying", story.Id, beat + 1);
                    await Task.Delay(TimeSpan.FromSeconds(1));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Visit story {Story}: beat {Beat} could not be shown", story.Id, beat + 1);
                    return;
                }
            }
        }
    }
```

`story.Visitor.Name` / `story.Host.Name` are already sanitised by `Cast`. If `FollowupAsync` in this Discord.Net version has no `flags:` parameter, check its signature and pass the Components V2 flag the way it accepts it (for example through `MessageFlags` on the `components:` overload) — do not drop the flag: a V2 card sent without it is rejected.

- [ ] **Step 3: Build.** `dotnet build` → 0 errors, 0 warnings. Harness `OK`.

- [ ] **Step 4: Checkpoint.** Do not commit.

---

### Task 7: First writing pass

**Files:**
- Modify: `ProjectSYNCS/Helpers/PlynlingPassions.cs` (pools and `Combos`)
- Modify: `ProjectSYNCS/Helpers/PlynlingVisitStory.cs` (`Reactions`, `CustomOpeners`, `CustomActivities`)

This task is authoring, not code: the structure is fixed, only lines are added. Style: the soft, funny narrator voice of the existing visit pools (see `Arrivals`, `Activities` in `PlynlingVisitStory.cs`); dialogue short and specific; Tomodachi-Life small absurdities.

**Targets** (totals per pool, counting what Tasks 1 and 5 already wrote):

| Pool | Target |
|---|---|
| Each passion's `Openers` | 6 |
| Each passion's `SharedLines` | 3 |
| Each passion's `Activities` | 5 |
| `Combos` | 15 pairs, 1–2 lines each |
| Each `Reactions[(mood, shared)]` | 5 |
| `CustomOpeners` | 8 |
| Each `CustomActivities[mood]` | 4 |

**Rules** (the harness enforces the first four):
1. Openers and reactions: exactly one `\n` (literal backslash-n).
2. Only these placeholders: `{A} {B} {S} {L} {P} {ils} {Ils}` and `{a:|} {b:|} {s:|} {l:|} {p:|}`.
3. `{P}` never after `de`, `à` or `d'`.
4. Activities, combos and custom activities start with an emoji, like the existing `Activities`.
5. Catalog pools do not use `{P}`; generic pools (`Reactions`, `Custom*`) must work for any passion, including « les trains à vapeur ».
6. No line assumes a place (« chez toi », « à la maison ») — the story can happen at the bakery or in a cave.
7. Family-neutral: never a cap, petals or spores.
8. The speaker's own gender agreements use `{s:m|f}`, the listener's `{l:m|f}`, the pair's `{p:m|f}`.

- [ ] **Step 1: Fill each passion to target** (12 × 14 lines).
- [ ] **Step 2: Fill `Combos` to 15 pairs.** Suggested pairs: Cooking+Gardening, Cooking+Insects, Music+Dance, Music+Stories, Gaming+Sport, Gaming+Stories, Astronomy+Stories, Painting+Gardening, Painting+Astronomy, Rocks+Painting, Sport+Naps, Dance+Sport (plus the three already there).
- [ ] **Step 3: Fill `Reactions`, `CustomOpeners`, `CustomActivities` to target.**
- [ ] **Step 4: Build + harness** → 0 errors / 0 warnings; `OK`.
- [ ] **Step 5: Checkpoint.** Do not commit.

---

### Task 8: Docs, help, and a live check

**Files:**
- Modify: `ProjectSYNCS/Commands/PlynlingModule.cs` (`BuildHelpEmbed`)
- Modify: `README.md`, `CLAUDE.md`
- Modify: `<scratchpad>/passions-harness/HarnessMore.cs`

- [ ] **Step 1: Harness check on the help embed.** Append to `HarnessMore.Run`:

```csharp
        var help = ProjectSYNCS.Commands.PlynlingModule.BuildHelpEmbed();
        check(help.Length <= 6000, $"help embed total {help.Length}/6000");
        foreach (var f in help.Fields) check(f.Value.Length <= 1024, $"help field {f.Name} {f.Value.Length}/1024");
```

- [ ] **Step 2: Help embed.** In `BuildHelpEmbed`, add a field after « Jouer & rendre visite »:

```csharp
            .AddField("Passions",
                "Chaque Plynling naît avec une **passion** : la cuisine, la musique, les étoiles, les siestes… " +
                "Pendant les visites, il en parle, et selon l'autre, ça passionne, ça ennuie ou ça tourne à la compétition.\n" +
                "**`/plynling passion`** — Apprends-lui une seconde passion, en toutes lettres (« la pêche », « les trains »). " +
                "Une fois par jour ; laisse vide pour l'effacer.")
```

and in the « Staff » field, append ` · **`/admin plynling passion-reset user:`**` to the second line.

- [ ] **Step 3: Build + harness** → `OK` (re-measure: if a cap fails, shorten the new field).

- [ ] **Step 4: README.** In the Plynlings section of `README.md` (grep `plynling visit`), add a row/line for `/plynling passion` (everyone) and `/admin plynling passion-reset` (staff), in the same format as its neighbours, and one sentence saying a visit is a five-beat story built around the two Plynlings' passions, posted as a new message.

- [ ] **Step 5: CLAUDE.md.** Four edits:
  1. In "Commands are grouped by whose thing it is", change `` `/plynling` holds 14 `` to `` `/plynling` holds 15 ``.
  2. "**Two migrations carry data, not schema**" → "**Three migrations carry data**", and add after that paragraph: "`AddPlynlingPassions` is the third, and the first that is not a wipe: it backfills an innate passion for Plynlings that predate passions, from the id (`(Id * 5 + 1) % 12`), so every row has one and nothing is rolled at runtime. Deriving the passion from the id at read time instead was rejected — adding a 13th passion would have silently changed every existing Plynling's."
  3. In the "A visit is told as a story" paragraph, replace "three beats — arrival, activity with a two-line exchange, parting plus `PlynlingPlayCards.VisitOutcomeLines`" with "five beats — arrival, the subject (one of them raises a passion), the reaction, the activity with a two-line exchange, parting plus `PlynlingPlayCards.VisitOutcomeLines`", and replace "« Accueillir » answers with beat 1; the next two are background `ModifyOriginalResponseAsync` edits 7 s apart" with "« Accueillir » closes the knock in place and posts beat 1 as a follow-up — a new message at the bottom of the channel; the next four are background edits of that follow-up, 7 s apart".
  4. Add a new paragraph after that one:

  "**Plynling passions: one innate, one taught, and the taught one is hostile input.** `Plynling.Passion` is one of the 12 in `Helpers/PlynlingPassions` — stored as an int, so **append-only** — rolled at adoption. `TaughtPassion` is free text from `/plynling passion` (2–40 characters, no links, 24 h cooldown, staff clear it with `/admin plynling passion-reset`), sanitised and sent with pings off like a name. `PlynlingPassions.Resolve` upgrades a typed text that names a catalog passion, so it gets the rich lines. Catalog passions have hand-written pools (openers, shared lines, activities, pair combos); a custom one only ever appears through generic templates as `{P}`, which is why those never put `{P}` after « de » or « à » (« parler de les trains »), and why `Expand` inserts `{P}` last — typed text is never read as a template. Beat 2's speaker is visitor or host at random; the subject is a shared passion 60 % of the time when there is one."

- [ ] **Step 6: Live check (with the owner).** Run the bot against the dev guild (`dotnet run` in `ProjectSYNCS/`; the migration applies on startup). With two accounts: `/plynling view` shows the 💭 line; `/plynling passion` → type « les trains à vapeur » → the card posts with SYNCS's line; `/plynling passion` again → cooldown refusal; `/plynling visit` → « Accueillir » → the knock closes with « ✅ … juste en dessous ↓ », a new card appears below and plays five beats 7 s apart, the last with ◀ ▶ « 5/5 ».

- [ ] **Step 7: Checkpoint.** Do not commit; hand over to the owner.
