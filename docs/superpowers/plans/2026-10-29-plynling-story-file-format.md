# Plynling story files — parser and checker Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Parse and check Plynling story files (`.story`), and print a path report per arc, so arcs can be written and reviewed before any storyline engine exists.

**Architecture:** Pure parsing and validation live in the bot under `ProjectSYNCS/Helpers/Plynling/Stories/`, where the engine will use them later. The slow walks (reachability, deciding alone, gender pairs) and the French path report live in `tests/ProjectSYNCS.Checks`, which references the bot project. Nothing is wired into the bot's startup yet, and players see no change.

**Tech Stack:** C# / .NET 10, the existing console checks project (no test framework), existing catalogs (`PlynlingTraits`, `PlynlingModifiers`, `ItemCatalog`, `PlynlingStats`, `PlynlingPersonality`, `PlynlingVisitStory.Expand`).

**Spec:** `docs/superpowers/specs/2026-10-29-plynling-story-file-format-design.md` (read it first; it builds on `2026-10-28-plynling-storylines-design.md`).

## Global Constraints

- Namespace stays flat: every bot file is `namespace ProjectSYNCS.Helpers;` whatever its subfolder (CLAUDE.md).
- Code, comments and parser/validator error messages are in English; the path report is in French (the owner reads it as content).
- Tags are English, prose is French. Enum-backed words in files are the C# enum name lowercased: stages `baby teen adult elder`, stats `diplomacy stewardship learning intrigue courage`, axes `boldness compassion greed energy honor rationality sociability vengefulness zeal`, needs `hunger happiness hygiene`, bonds `acquaintances friends bestfriends lovers rivals enemies`, seasons `spring summer autumn winter`, passions `cooking music gaming astronomy gardening rocks stories dance painting sport insects naps`.
- Every key read from a file (arc, scene, option, ending, memory, epithet, keepsake, flag) is a stored key once the engine ships: append-only. Nothing in this plan renames one.
- Never `Random`; nothing here rolls (the report uses fixed odds).
- The owner commits by hand: **no task runs `git commit`**. Each task ends with a checkpoint instead.
- Machine quirks: `dotnet` prints in French; `sed -i` strips CRLF; don't pipe `python` heredocs. Use the Edit/Write tools for files.
- One **patch** bump for the whole batch: `ProjectSYNCS/config.yaml` `7.55.0` → `7.55.1`, plus a `ProjectSYNCS/CHANGELOG.md` entry (Task 7). Check `git diff ProjectSYNCS/config.yaml` first; if already bumped, extend that entry instead.
- Run all checks with: `dotnet run --project tests/ProjectSYNCS.Checks`. Passing output ends with `ALL CHECKS PASSED`, exit code 0.

## Review Focus

- **Windows line endings and a UTF-8 BOM** in a hand-edited file (Visual Studio saves CRLF, sometimes with a BOM): the parser must read it like LF, with correct line numbers → case in Task 2.
- **A `[` or `]` inside prose**, or a `]` inside a quoted lock text (`[needs flag x "a ] b"]`): prose with a stray bracket is an error naming the line, and a bracket inside quotes is text → cases in Task 1.
- **Tabs instead of spaces, or no indentation at all** under an option: indentation is ignored, structure comes from `*`, `?` and `##` → case in Task 3.
- **A typo in a keyword** (`[remeber x]`, `pace: nextday`, `## scnee: x`): every unknown word is an error with file:line, never silently ignored → cases in Tasks 1–3.
- **Two errors in one file**: all of them are reported in one run, not just the first → case in Task 4.

---

## File map

| File | Responsibility |
|---|---|
| `ProjectSYNCS/Helpers/Plynling/Stories/StoryModel.cs` | Records and enums of a parsed arc, `StoryPos`, `StoryError`. |
| `ProjectSYNCS/Helpers/Plynling/Stories/StoryTags.cs` | Splits a line into leading tags, text, trailing tags; splits a tag into words. |
| `ProjectSYNCS/Helpers/Plynling/Stories/StoryVocabulary.cs` | Words → `StoryCondition`, `StoryEffect`, enum words, durations. |
| `ProjectSYNCS/Helpers/Plynling/Stories/StoryParser.cs` | Text → `StoryArc` (header, declarations, scenes, options, endings) and → cast. |
| `ProjectSYNCS/Helpers/Plynling/Stories/StoryValidator.cs` | Cross-references, catalogs, ranges, template tokens, Discord caps. |
| `ProjectSYNCS/Helpers/Plynling/Stories/StoryLibrary.cs` | Loads a folder: `village.cast` + every `*.story`, parsed and validated. |
| `ProjectSYNCS/Stories/village.cast` | The 13 core characters. |
| `tests/ProjectSYNCS.Checks/StoryChecks.cs` | Every story check case; entry point called from `Program.cs`. |
| `tests/ProjectSYNCS.Checks/StoryWalker.cs` | Path enumeration and the walk checks. |
| `tests/ProjectSYNCS.Checks/StoryReport.cs` | The French path report. |
| `tests/ProjectSYNCS.Checks/Stories/test_everything.story` | Fixture arc using every tag (not shipped). |

---

### Task 1: Model, tag splitting and vocabulary

**Files:**
- Create: `ProjectSYNCS/Helpers/Plynling/Stories/StoryModel.cs`, `StoryTags.cs`, `StoryVocabulary.cs`
- Create: `tests/ProjectSYNCS.Checks/StoryChecks.cs`
- Modify: `tests/ProjectSYNCS.Checks/Program.cs` (one call before the final summary, around line 4231)

**Interfaces:**
- Produces (all `public`, `namespace ProjectSYNCS.Helpers`):

```csharp
public sealed record StoryPos(string File, int Line) { public override string ToString() => $"{File}:{Line}"; }
public sealed record StoryError(StoryPos Pos, string Message) { public override string ToString() => $"{Pos}: {Message}"; }

public enum StoryFamily { Milestone, Friendship, Love, Rivalry, Village, Personal, Hardship }
public enum TriggerKind { Adoption, Stage, Draw, Relation, Death, Sick, Break, OldAge }
public sealed record StoryTrigger(TriggerKind Kind, string? Arg);          // stage teen, relation friends, death lovers, break 2
public enum PaceKind { NextDay, Now, Hours }
public sealed record StoryPace(PaceKind Kind, int Hours);                   // Hours 1..48 when Kind == Hours

public enum ConditionKind { Trait, OtherTrait, Stage, Flag, Memory, Keepsake, Epithet, Stat, Standing, Relation, Season, Passion, Ended, Chain, Gender }
public enum CompareOp { None, AtLeast, Below }
public sealed record StoryCondition(ConditionKind Kind, string Key, bool Negated, CompareOp Op, int Number, StoryPos Pos);

public enum EffectKind { Flag, Unflag, Remember, Forget, Reveal, Stress, Modifier, Grow, Affinity, Cailloux, Item, Lift, Epithet, Keepsake, Unlock, Trait }
public sealed record StoryEffect(EffectKind Kind, string Key, string? To, double Amount, bool Other, StoryPos Pos);

public sealed record StoryLine(string Text, IReadOnlyList<StoryCondition> When, StoryPos Pos);
public sealed record StoryParagraph(IReadOnlyList<StoryLine> Lines);
public sealed record StoryRoll(PlynlingStat Stat, int Difficulty);
public sealed record StoryOutcome(IReadOnlyList<StoryParagraph> Prose, IReadOnlyList<StoryEffect> Effects, string? Next, StoryPos Pos);
public sealed record StoryOption(string Key, string Label, IReadOnlyList<StoryCondition> Needs, string? LockText, bool Secret,
    StoryRoll? Roll, IReadOnlyDictionary<AiAxis, int> Lean, IReadOnlyDictionary<string, int> StressCosts, bool OwnerOnly,
    bool Retired, string? Public, StoryOutcome? Outcome, StoryOutcome? Success, StoryOutcome? Failure, StoryPos Pos);
public sealed record StoryScene(string Key, string Title, StoryPace Pace, string? Public, bool Bump, bool Handover,
    string? RetiredTo, IReadOnlyList<StoryParagraph> Prose, IReadOnlyList<StoryOption> Options, StoryOutcome? Narration, StoryPos Pos);
public sealed record StoryEnding(string Key, string Title, IReadOnlyList<StoryParagraph> Prose, IReadOnlyList<StoryEffect> Marks, StoryPos Pos);
public sealed record MemoryDecl(string Key, string Holder, bool Public, int Shift, string Wording, StoryPos Pos);
public sealed record EpithetDecl(string Key, string Title, StoryPos Pos);
public sealed record KeepsakeDecl(string Key, string Name, string Story, bool Hurts, StoryPos Pos);
public sealed record StoryWeightBonus(int Bonus, IReadOnlyList<StoryCondition> When);
public sealed record StoryHeader(string Title, StoryFamily Family, IReadOnlyList<PlynlingStage> Stages, StoryTrigger Trigger,
    TimeSpan? Shelf, int Weight, IReadOnlyList<StoryWeightBonus> WeightBonuses, IReadOnlyList<StoryCondition> Requires,
    PlynlingBond? Other, string Public);
public sealed record StoryArc(string Key, StoryPos Pos, StoryHeader Header, IReadOnlyList<MemoryDecl> Memories,
    IReadOnlyList<EpithetDecl> Epithets, IReadOnlyList<KeepsakeDecl> Keepsakes, IReadOnlyList<StoryScene> Scenes,
    IReadOnlyList<StoryEnding> Endings);   // the arc opens on Scenes[0] (builder's call)
public sealed record CastMember(string Key, string GivenName, string Byname, StoryPos Pos);

public sealed record StoryWord(string Text, bool Quoted);
public sealed record StoryLineParts(IReadOnlyList<string> Leading, string Text, IReadOnlyList<string> Trailing, string? Problem);

public static class StoryTags
{
    public static StoryLineParts Split(string line);                 // tag bodies without brackets
    public static IReadOnlyList<StoryWord> Words(string tag);        // whitespace split; "…" one quoted word; "->" its own word
}

public static class StoryVocabulary
{
    public static StoryCondition? Condition(IReadOnlyList<StoryWord> words, bool negated, StoryPos pos, List<StoryError> errors);
    public static StoryEffect? Effect(IReadOnlyList<StoryWord> words, StoryPos pos, List<StoryError> errors);
    public static bool TryEnum<T>(string word, out T value) where T : struct, Enum;   // lowercase enum name only
    public static TimeSpan? Duration(string word);                                    // "7d", "36h"
}
```

- `StoryChecks` entry: `public static class StoryChecks { public static void Run(Action<string> fail, string[] args); }`.

Vocabulary shapes (the words after the head, which the parser strips: `if`, `unless`, `needs`, `requires`):

| Condition words | Record |
|---|---|
| `trait <key>` / `other trait <key>` | `Trait` / `OtherTrait`, Key = trait key |
| `stage <stage>` · `relation <bond>` · `season <season>` · `passion <passion>` · `gender m\|f` | Key = the word |
| `flag <key>` or `flag <arc>.<key>` · `memory <key>` · `keepsake <key>` · `epithet <key>` · `ended <arc>.<ending>` · `chain <key>` | Key = the word |
| `stat <stat> >= n` / `stat <stat> < n` · `standing <cast> >= n` / `< n` | Op = AtLeast/Below, Number = n |
| leading `not` | Negated flips |

| Effect words | Record |
|---|---|
| `flag k` · `unflag k` · `remember m` · `other remember m` (Other = true) · `forget m` · `reveal m` · `modifier k` · `item k` · `epithet k` · `keepsake k` · `unlock arc` | Key |
| `stress n` (signed) · `affinity n` (signed) · `cailloux n` | Amount |
| `grow <stat>` | Key = stat word |
| `lift <need> x` (`0.3`, invariant culture) | Key = need word, Amount = x |
| `trait k` / `trait old -> new` | Key, To |

Unknown head word, wrong word count, bad number, unknown enum word → `null` plus one `StoryError` naming the tag (`unknown condition 'traits'`, `'stage' expects one of: baby, teen, adult, elder`).

- [ ] **Step 1: Write the failing checks** in `StoryChecks.cs` (section « 34a »), and call `StoryChecks.Run(Fail, args);` in `Program.cs` just before `Console.WriteLine(failures == 0 ? …`.

```csharp
// Split: leading tags, text, trailing tags; brackets inside quotes or {…} are text.
var p = StoryTags.Split("[if trait curious] {A} goûte une feuille.");
Expect(p.Leading.SequenceEqual(new[] { "if trait curious" }) && p.Text == "{A} goûte une feuille." && p.Trailing.Count == 0, "split leading");
p = StoryTags.Split("* wood | Partir  [needs trait curious]  [roll learning 7]");
Expect(p.Text == "* wood | Partir" && p.Trailing.SequenceEqual(new[] { "needs trait curious", "roll learning 7" }), "split trailing");
p = StoryTags.Split("[flag a] [remember lichen.came_first] [-> the_cabin]");
Expect(p.Text == "" && p.Leading.Count == 3, "split tag-only line");
Expect(StoryTags.Split("[needs flag x \"a ] b\"]").Leading.Single() == "needs flag x \"a ] b\"", "bracket inside quotes");
Expect(StoryTags.Split("Elle dit [quelque chose] puis part.").Problem is not null, "tag in the middle of prose");
Expect(StoryTags.Split("Une phrase [sans fin").Problem is not null, "unclosed bracket");
var w = StoryTags.Words("needs flag owes \"La pie vous doit une faveur\"");
Expect(w.Count == 4 && w[3].Quoted && w[3].Text == "La pie vous doit une faveur", "quoted word");
Expect(StoryTags.Words("trait shy -> brave").Select(x => x.Text).SequenceEqual(new[] { "trait", "shy", "->", "brave" }), "arrow word");

// Vocabulary.
var errs = new List<StoryError>(); var at = new StoryPos("t.story", 1);
var c = StoryVocabulary.Condition(StoryTags.Words("stat learning >= 6"), false, at, errs);
Expect(c is { Kind: ConditionKind.Stat, Key: "learning", Op: CompareOp.AtLeast, Number: 6 }, "stat condition");
c = StoryVocabulary.Condition(StoryTags.Words("not flag arrival.came_to_her"), false, at, errs);
Expect(c is { Kind: ConditionKind.Flag, Key: "arrival.came_to_her", Negated: true }, "negated cross-arc flag");
var e = StoryVocabulary.Effect(StoryTags.Words("trait shy -> brave"), at, errs);
Expect(e is { Kind: EffectKind.Trait, Key: "shy", To: "brave" }, "trait swap");
e = StoryVocabulary.Effect(StoryTags.Words("lift happiness 0.3"), at, errs);
Expect(e is { Kind: EffectKind.Lift, Key: "happiness", Amount: 0.3 }, "lift");
e = StoryVocabulary.Effect(StoryTags.Words("other remember lichen.x"), at, errs);
Expect(e is { Kind: EffectKind.Remember, Other: true }, "other remember");
Expect(errs.Count == 0, "no errors on valid words: " + string.Join("; ", errs));
Expect(StoryVocabulary.Effect(StoryTags.Words("remeber lichen.x"), at, errs) is null && errs.Any(x => x.Message.Contains("remeber")), "typo named");
Expect(StoryVocabulary.Condition(StoryTags.Words("stage toddler"), false, at, errs) is null, "bad stage");
Expect(StoryVocabulary.Duration("7d") == TimeSpan.FromDays(7) && StoryVocabulary.Duration("36h") == TimeSpan.FromHours(36) && StoryVocabulary.Duration("7") is null, "durations");
```

`Expect(bool ok, string what)` is a local helper in `StoryChecks` that calls `fail("story: " + what)` when `ok` is false.

- [ ] **Step 2: Run, expect a build failure** (types missing). Run: `dotnet run --project tests/ProjectSYNCS.Checks`. Expected: compile errors naming `StoryTags` / `StoryVocabulary`.
- [ ] **Step 3: Implement** the records, `StoryTags` and `StoryVocabulary` as specified above. `Split` scans once, tracking quote and `{}` depth; a `[` met after text has started and followed by more text is a `Problem` (`"tag inside prose"`).
- [ ] **Step 4: Run, expect `ALL CHECKS PASSED`.**
- [ ] **Step 5: Checkpoint** — no commit (the owner commits).

---

### Task 2: Parser — header, declarations, cast

**Files:**
- Create: `ProjectSYNCS/Helpers/Plynling/Stories/StoryParser.cs`
- Modify: `tests/ProjectSYNCS.Checks/StoryChecks.cs` (section « 34b »)

**Interfaces:**
- Consumes: Task 1.
- Produces:

```csharp
public static class StoryParser
{
    public static StoryArc? Parse(string file, string text, List<StoryError> errors);          // null only when the # line is missing or broken
    public static IReadOnlyList<CastMember> ParseCast(string file, string text, List<StoryError> errors);
}
```

Rules:
- Strip a leading BOM; split on `\r\n` or `\n`; line numbers are 1-based. `//` lines (after trimming) are ignored everywhere.
- First non-blank line `# arc: <key>`; `# cast` for the cast file. Another kind (`# beat:` …) → error `"'beat' files are not supported yet"`.
- Header lines `field: value` until the first `##`. Fields: `title` `family` `stage` (several, space or comma separated) `trigger` (`adoption` · `stage <stage>` · `draw` · `relation <bond>` · `death <bond>` · `sick` · `break <1-3>` · `old_age`) `shelf` (duration) `weight` (first `weight: n`, then any number of `weight: +n [if …] …`) `requires` (tags only, conditions) `other` (bond) `public`. Required: title, family, stage, trigger, public. Unknown or repeated field (except `weight`) → error.
- `## memories` lines: `key | holder | private|public | ±n | wording`. `## epithets`: `key | title`. `## keepsakes`: `key | name | story` with optional `| hurts`. Wrong column count → error.
- Cast lines: `key | given name | byname`.

- [ ] **Step 1: Write the failing checks.**

```csharp
const string Head = "# arc: t\ntitle: T\nfamily: milestone\nstage: baby\ntrigger: adoption\npublic: P\n";
var a = Parse(Head + "## memories\nlichen.x | lichen | private | +5 | Est venue.\n## epithets\nshy_one | {a:Le Timide|La Timide}\n## keepsakes\npebble | Le galet | Un galet tiède. | hurts\n", out var pe);
Expect(pe.Count == 0, "valid header: " + string.Join("; ", pe));
Expect(a!.Key == "t" && a.Header.Family == StoryFamily.Milestone && a.Header.Stages.Single() == PlynlingStage.Baby
    && a.Header.Trigger.Kind == TriggerKind.Adoption, "header fields");
Expect(a.Memories.Single() is { Key: "lichen.x", Holder: "lichen", Public: false, Shift: 5 }, "memory");
Expect(a.Keepsakes.Single().Hurts && a.Epithets.Single().Title == "{a:Le Timide|La Timide}", "keepsake, epithet");
a = Parse(Head.Replace("trigger: adoption", "trigger: draw\nweight: 10\nweight: +5 [if passion gardening]\nshelf: 7d\nother: friends"), out pe);
Expect(a!.Header.Weight == 10 && a.Header.WeightBonuses.Single().Bonus == 5 && a.Header.Other == PlynlingBond.Friends
    && a.Header.Shelf == TimeSpan.FromDays(7), "weight, shelf, other");
// CRLF and BOM read like LF, with the right line numbers.
Parse("﻿" + (Head + "colour: blue\n").Replace("\n", "\r\n"), out pe);
Expect(pe.Any(x => x.Pos.Line == 7 && x.Message.Contains("colour")), "CRLF+BOM: unknown field on line 7");
ExpectError(Head.Replace("public: P\n", ""), "public");               // required field missing
ExpectError(Head.Replace("stage: baby", "stage: toddler"), "toddler");
ExpectError("# beat: x\n", "not supported");
ExpectError(Head + "## memories\nlichen.x | lichen | private | 5\n", "columns");
var cast = StoryParser.ParseCast("c", "# cast\n// note\nlichen | Ysaure | la Mère Lichen\n", pe = new());
Expect(pe.Count == 0 && cast.Single() is { Key: "lichen", GivenName: "Ysaure", Byname: "la Mère Lichen" }, "cast");
```

`Parse(text, out errors)` and `ExpectError(text, fragment)` are local helpers: `ExpectError` fails unless some error message contains `fragment`.

- [ ] **Step 2: Run, expect failure** (`StoryParser` missing).
- [ ] **Step 3: Implement** `Parse` (header and declaration sections; scene and ending sections are skipped for now) and `ParseCast`.
- [ ] **Step 4: Run, expect `ALL CHECKS PASSED`.**
- [ ] **Step 5: Checkpoint.**

---

### Task 3: Parser — scenes, prose, options, endings

**Files:**
- Modify: `ProjectSYNCS/Helpers/Plynling/Stories/StoryParser.cs`
- Modify: `tests/ProjectSYNCS.Checks/StoryChecks.cs` (section « 34c »)

**Interfaces:**
- Consumes: Tasks 1–2. Produces: `StoryArc.Scenes` and `StoryArc.Endings` filled.

Rules:
- `## scene: <key>`: then optional header lines `title:` `pace:` (`next_day` · `now` · `+Nh`, N 1..48) `public:`, bare `bump`, `handover`, `retired -> <key>`, until the first other line. A live scene needs `title` and `pace`; a retired one needs only `retired`.
- **Prose:** each non-blank line is one `StoryLine`; leading `[if …]` / `[unless …]` tags become its `When` (unless → `Negated`); any other leading tag on a prose line, or trailing tags, are errors. A blank line closes the paragraph.
- **Options:** `* <key> | <label>` with trailing attribute tags. Until the option's prose starts, tag-only lines are **attributes**: `needs <condition> ["lock text"]`, `roll <stat> <difficulty>`, `lean <axis> ±n`, `stress <trait> n`, `owner-only`, `secret`, `public "…"`, `retired`. After prose has started, tag-only lines are **effects** (Task 1 vocabulary) or `-> <key>`. `? success` / `? failure` open the two outcomes of a roll; each has its own prose, effects and `->`.
- Indentation is ignored (spaces, tabs, none).
- A scene with no options whose body ends in a tag-only line containing `-> x` is a narration: its trailing tags form `Narration`.
- `## ending: <key>`: `title:` then prose, then tag-only lines as `Marks` (no `->` allowed).
- Structural errors: `->` missing or twice in one outcome; `? success` without `[roll]` or a roll without both outcomes; an outcome that has both plain prose and `? success`; an attribute after prose; `## scnee:` (unknown section); an option before any scene.

- [ ] **Step 1: Write the failing checks.** Put the spec's sample (the `## scene: first_steps` block and an `## ending: welcomed` with `title: Le premier soir`, one prose line and `[keepsake lichen_pebble] [cailloux 50]`) in a raw string constant `Sample`, after `Head`.

```csharp
a = Parse(Sample, out pe);
Expect(pe.Count == 0, "sample parses: " + string.Join("; ", pe));
var s = a!.Scenes.Single();
Expect(s is { Key: "first_steps", Title: "Les premiers pas", Pace.Kind: PaceKind.NextDay } && s.Public!.Contains("hésite"), "scene header");
Expect(s.Prose.Single().Lines.Count == 3 && s.Prose[0].Lines[1].When.Single() is { Kind: ConditionKind.Trait, Key: "curious" }, "conditional line");
Expect(s.Options.Select(o => o.Key).SequenceEqual(new[] { "to_her", "stay", "wood" }), "options in order");
var wood = s.Options[2];
Expect(wood.Roll == new StoryRoll(PlynlingStat.Learning, 7) && wood.Needs.Single().Key == "curious" && wood.Lean[AiAxis.Boldness] == 2, "wood attributes");
Expect(wood.Outcome is null && wood.Success!.Next == "the_cabin" && wood.Failure!.Next == "lost_at_edge"
    && wood.Failure.Effects.Any(x => x.Kind == EffectKind.Stress && x.Amount == 10), "roll outcomes");
Expect(s.Options[1].StressCosts["rowdy"] == 10 && s.Options[0].Outcome!.Effects.Count == 2, "stress cost vs effects");
Expect(a.Endings.Single().Marks.Select(m => m.Kind).SequenceEqual(new[] { EffectKind.Keepsake, EffectKind.Cailloux }), "ending marks");
// Indentation is ignored.
Expect(Parse(Sample.Replace("\n  ", "\n\t").Replace("\n    ", "\n"), out pe) is not null && pe.Count == 0, "tabs or no indent");
ExpectError(Sample.Replace("[-> the_cabin]\n\n* stay", "\n\n* stay"), "->");                 // to_her lost its ->
ExpectError(Sample.Replace("  [roll learning 7]", ""), "roll");                               // ? success without a roll
ExpectError(Sample.Replace("[lean sociability +2]", "[remeber lichen.x]"), "remeber");
ExpectError(Sample.Replace("## scene:", "## scnee:"), "scnee");
ExpectError(Sample.Replace("pace: next_day", "pace: nextday"), "nextday");
ExpectError(Sample.Replace("La pluie s'est", "La pluie [flag x] s'est"), "prose");
var n = Parse(Head + "## scene: beat\ntitle: B\npace: now\nUne ligne.\n[flag seen] [-> end]\n## ending: end\ntitle: E\nFin.\n", out pe);
Expect(pe.Count == 0 && n!.Scenes[0].Options.Count == 0 && n.Scenes[0].Narration!.Next == "end", "narration scene");
var r = Parse(Head + "## scene: old\nretired -> beat\n", out pe);
Expect(pe.Count == 0 && r!.Scenes[0].RetiredTo == "beat", "retired scene");
```

- [ ] **Step 2: Run, expect failures.**
- [ ] **Step 3: Implement** scene, option, outcome and ending parsing in `StoryParser` per the rules.
- [ ] **Step 4: Run, expect `ALL CHECKS PASSED`.**
- [ ] **Step 5: Checkpoint.**

---

### Task 4: Validator

**Files:**
- Create: `ProjectSYNCS/Helpers/Plynling/Stories/StoryValidator.cs`
- Modify: `tests/ProjectSYNCS.Checks/StoryChecks.cs` (section « 34d »)

**Interfaces:**
- Consumes: Tasks 1–3, `PlynlingTraits.ByKey`, `PlynlingModifiers.ByKey`, `ItemCatalog.ByKey`, `PlynlingStats.Name`.
- Produces: `public static class StoryValidator { public static IReadOnlyList<StoryError> Validate(IReadOnlyList<StoryArc> arcs, IReadOnlyList<CastMember> cast); public const int NameWorstCase = 32; }`

Every error carries the offending element's `StoryPos`. **All** errors are collected; nothing stops at the first. The checks (ids used in the check cases):

| Id | Rule |
|---|---|
| V-keys | Arc keys unique across files; scene + ending keys unique within an arc (one namespace, they are `->` targets); option keys unique within a scene; memory, epithet, keepsake keys unique across all files. |
| V-catalog | Trait keys (conditions, `stress <trait>`, `trait` effects) exist in `PlynlingTraits`; `modifier` keys in `PlynlingModifiers`; `item` keys in `ItemCatalog`. |
| V-targets | Every `->`, `retired ->` points to a scene or ending of the same arc. |
| V-required | An `interrupted` ending exists. A `loss` scene or ending exists when family is `love` or `friendship` (builder's call: the spec's "close friendship" is every friendship arc). `other` is set when family is friendship/love/rivalry or trigger is relation/death. |
| V-refs | `remember`/`forget`/`reveal`/`memory` keys are declared in some file; a memory's holder is a cast key and its key starts with `<holder>.`; `reveal` targets a private memory; `standing` names a cast key; `epithet`/`keepsake` refs are declared; `unlock` and `ended <arc>.<ending>` name an existing arc (and ending); `flag <arc>.<f>` names an arc that sets `f`. |
| V-other | `{B}`, `{b:…}`, `{p:…}`, `other trait`, `relation`, `[affinity]`, `[other remember]` and `handover` only in an arc with `other`. |
| V-tokens | In every text (titles, labels, prose, public lines, lock texts, memory wordings, epithet titles, keepsake names and stories): only `{A}` `{B}` `{T}` `{a:…\|…}` `{b:…\|…}` `{p:…\|…}`; no other brace. |
| V-options | A live scene has 2–4 live options, or none and a `Narration`. With options, at least one is **free**: no `needs`, not `owner-only`, not `secret`, no stress cost. |
| V-marks | `epithet`, `keepsake`, `trait`, `unlock` only in endings; at most one `trait` per ending; a swap's two traits are personality traits of the same `Group` and differ; a gained trait is a personality trait. |
| V-ranges | stress cost 1..60; `stress` effect −100..60; roll difficulty 1..20; `lift` 0 < x ≤ 0.3; `cailloux` 1..100; `affinity` −50..50; `lean` −5..5. |
| V-caps | With `{A}`/`{B}` expanded to 32 characters: label + (roll ? 20 : 0) + (needs ? 3 + lock length : 0) ≤ 80, where the lock length is the lock text or the trait's longest name; titles ≤ 100; a scene's prose with every line included ≤ 3000; an outcome's or ending's prose ≤ 3000; any public line ≤ 300. |

- [ ] **Step 1: Write the failing checks.** One minimal valid arc string `Valid` (two scenes, three endings including `interrupted`, a free option in each scene, one declared memory with holder `lichen`) and a one-line cast `lichen | Ysaure | la Mère Lichen`. Then one case per rule, each a single `Replace` on `Valid` and the expected fragment:

```csharp
Expect(Validate(Valid).Count == 0, "valid arc: " + string.Join("; ", Validate(Valid)));
ExpectInvalid(Valid.Replace("[-> two]", "[-> nowhere]"), "nowhere");                                      // V-targets
ExpectInvalid(Valid.Replace("## ending: interrupted", "## ending: cut"), "interrupted");                     // V-required
ExpectInvalid(Valid.Replace("[remember lichen.x]", "[remember lichen.y]"), "lichen.y");                      // V-refs
ExpectInvalid(Valid.Replace("trait curious", "trait curiouss"), "curiouss");                                 // V-catalog
ExpectInvalid(Valid.Replace("{A} attend.", "{B} attend."), "{B}");                                           // V-other
ExpectInvalid(Valid.Replace("{A} attend.", "{C} attend."), "{C}");                                           // V-tokens
ExpectInvalid(Valid.Replace("[stress rowdy 10]", "[stress rowdy 90]"), "90");                               // V-ranges
ExpectInvalid(Valid.Replace("* stay | Rester", "* stay | " + new string('x', 79)), "80");                    // V-caps
ExpectInvalid(Valid.Replace("[cailloux 20] [-> e1]", "[keepsake pebble] [-> e1]"), "ending");             // V-marks: a keepsake in an outcome
ExpectInvalid(Valid + Valid.Replace("# arc: v", "# arc: v2"), "lichen.x");                                  // V-keys, memory declared twice
// Two problems in one file are both reported.
var two = Validate(Valid.Replace("[-> two]", "[-> nowhere]").Replace("trait curious", "trait curiouss"));
Expect(two.Count >= 2, "every error reported, not the first only");
```

(`Valid`'s exact text is the implementer's, provided each `Replace` above hits exactly one spot; `ExpectInvalid(text, fragment)` parses and validates and fails unless an error contains `fragment`; for the V-keys case it parses two arcs from two texts.)

- [ ] **Step 2: Run, expect failures.**
- [ ] **Step 3: Implement** `StoryValidator.Validate`.
- [ ] **Step 4: Run, expect `ALL CHECKS PASSED`.**
- [ ] **Step 5: Checkpoint.**

---

### Task 5: Library, the cast, the fixture arc and the content copy

**Files:**
- Create: `ProjectSYNCS/Helpers/Plynling/Stories/StoryLibrary.cs`
- Create: `ProjectSYNCS/Stories/village.cast`
- Create: `tests/ProjectSYNCS.Checks/Stories/test_everything.story`
- Modify: `ProjectSYNCS/ProjectSYNCS.csproj` (copy `Stories\**\*` like `Assets\`)
- Modify: `tests/ProjectSYNCS.Checks/StoryChecks.cs` (section « 34e »)

**Interfaces:**
- Produces:

```csharp
public sealed record StoryLoad(IReadOnlyList<StoryArc> Arcs, IReadOnlyList<CastMember> Cast, IReadOnlyList<StoryError> Errors);
public static class StoryLibrary
{
    // village.cast from castFolder; every *.story under each folder, recursively, sorted by path; parsed and validated together.
    public static StoryLoad Load(string castFolder, params string[] storyFolders);
}
```

- `StoryChecks.RepoRoot()` walks up from `AppContext.BaseDirectory` to the folder holding `ProjectSYNCS/config.yaml`.
- `village.cast`: `# cast`, a `//` note pointing to `docs/village-lore.md`, then the 13 core characters:

```text
lichen     | Ysaure      | la Mère Lichen
frene      | François    | Maître Frêne
intendant  | Gauvain     | l'Intendant
chardon    | Chamberlain | le Chardon
sauge      | Aliénor     | la Sauge
courge     | Mahaut      | la Courge
pain_blond | Colin       | Pain-Blond
bruyere    | Guillemette | la Bruyère
noyer      | Hugues      | le Noyer
ortie      | Jehanne     | l'Ortie
etrangere  | Asha        | l'Étrangère
tilleul    | Barnabé     | le Tilleul
fou        | Gilles      | le Fou
```

- `test_everything.story`: family `love`, `other: lovers`, so every tag is legal. It must use every condition kind, every effect kind, every option attribute, a roll with both outcomes, a narration scene, a retired scene, a retired option, a `handover` scene, `bump`, `[public "…"]`, `[if]` and `[unless]` lines, `{a:…}`/`{b:…}`/`{p:…}`, a `loss` scene, an `interrupted` ending and 3–5 other endings, and declare memories (one public, one private, one revealed), an epithet and a keepsake. French prose may be placeholder sentences; header line `// Fixture: every tag once. Not shipped, never played.`. Cross-arc `flag`/`ended` conditions point to the fixture itself (`flag test_everything.<f>`).

- [ ] **Step 1: Write the failing checks.**

```csharp
var root = RepoRoot();
var shipped = StoryLibrary.Load(Path.Combine(root, "ProjectSYNCS", "Stories"), Path.Combine(root, "ProjectSYNCS", "Stories"));
foreach (var err in shipped.Errors) fail("story file: " + err);
Expect(shipped.Cast.Count == 13 && shipped.Cast.Any(c => c.Key == "lichen" && c.GivenName == "Ysaure"), "village.cast");
var fixture = StoryLibrary.Load(Path.Combine(root, "ProjectSYNCS", "Stories"), Path.Combine(root, "tests", "ProjectSYNCS.Checks", "Stories"));
foreach (var err in fixture.Errors) fail("fixture: " + err);
var t = fixture.Arcs.Single(x => x.Key == "test_everything");
var effects = t.Scenes.SelectMany(AllEffects).Concat(t.Endings.SelectMany(x => x.Marks)).Select(x => x.Kind).ToHashSet();
foreach (var k in Enum.GetValues<EffectKind>()) Expect(effects.Contains(k), $"fixture uses effect {k}");
var conds = AllConditions(t).Select(x => x.Kind).ToHashSet();
foreach (var k in Enum.GetValues<ConditionKind>()) Expect(conds.Contains(k), $"fixture uses condition {k}");
```

`AllEffects(StoryScene)` and `AllConditions(StoryArc)` are local helpers in `StoryChecks` that gather from options, outcomes, narration, prose lines, `needs`, `requires` and weight bonuses.

- [ ] **Step 2: Run, expect failures** (`StoryLibrary` missing).
- [ ] **Step 3: Implement** `StoryLibrary.Load`, write `village.cast` and the fixture, and add to the csproj, next to the `Assets` item:

```xml
<None Include="Stories\**\*">
  <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  <CopyToPublishDirectory>PreserveNewest</CopyToPublishDirectory>
</None>
```

- [ ] **Step 4: Run, expect `ALL CHECKS PASSED`**, and `dotnet build ProjectSYNCS` succeeds with `ProjectSYNCS/bin/**/Stories/village.cast` present.
- [ ] **Step 5: Checkpoint.**

---

### Task 6: The walker

**Files:**
- Create: `tests/ProjectSYNCS.Checks/StoryWalker.cs`
- Modify: `tests/ProjectSYNCS.Checks/StoryChecks.cs` (section « 34f »; run the walk on every arc of both loads)

**Interfaces:**
- Consumes: Tasks 1–5, `PlynlingTraits`, `PlynlingPersonality.Axes`, `PlynlingVisitStory.Expand`.
- Produces:

```csharp
public sealed record WalkStep(StoryScene Scene, StoryOption? Option, bool? Success);   // Option null for a narration
public sealed record WalkPath(IReadOnlyList<WalkStep> Steps, StoryEnding Ending, bool Alone, int Cailloux, int TraitChanges,
    IReadOnlyDictionary<string, int> StressCosts);
public sealed record WalkResult(IReadOnlyList<WalkPath> Paths, IReadOnlyList<string> Problems,
    IReadOnlyList<(string Profile, IReadOnlyDictionary<string, double> EndingShare)> InCharacter);
public static class StoryWalker
{
    public const int MaxPaths = 20_000;
    public static WalkResult Walk(StoryArc arc, IReadOnlyList<StoryArc> all);
}
```

The walk:
- **Paths:** depth-first from `Scenes[0]`, over every live option (rolls branch into success and failure; a retired scene follows its redirect). State is the set of run flags: `flag`/`unflag` effects apply, and `flag`/`not flag` conditions on this arc's own flags are evaluated exactly. Every other condition counts as **satisfiable** (the walk looks for what *can* happen). An option is `Alone`-pickable when it is free in the V-options sense; `WalkPath.Alone` is true when every step was. Cailloux and `trait` effects are summed over the path, the ending's marks included.
- **Problems** (each one `fail`s in `StoryChecks`, prefixed `"<arc>: "`):
  - a scene revisited on one path → `loop through <scene>`;
  - more than `MaxPaths` paths → `too many paths`;
  - a live scene or an ending never reached, `interrupted` and `loss` (and what only `loss` leads to) excepted → `unreachable <key>`;
  - fewer than 3 or more than 5 endings, `interrupted` and `loss` endings excepted → `endings: n`;
  - a flag tested (here or by another arc's `flag <arc>.<f>`) and never set, or set and never tested anywhere → `flag <f> never set` / `flag <f> never tested`;
  - starting from `Scenes[0]` and from `loss`, a branch through alone-pickable options only that does not end → `alone dead end at <scene>`;
  - a path with cailloux over 100 → `cailloux <n> on a path`; with more than one trait change → `two trait changes on a path`;
  - any text, expanded in the four gender pairs with 32-character names (`PlynlingVisitStory.Expand(text, name, ga, name, gb)`), that still contains `{` or `}` → `template left in <where>`.
- **In character** (for the report): profiles are « neutre » (no trait) plus, for an arc whose first stage is `baby`, each childhood trait, otherwise each trait named anywhere in the arc. At each scene, the options pickable alone by that profile (`needs` on `trait`/`other trait` read the profile, every other non-flag condition counts as **false**; options with a stress cost for a held trait excluded) are weighted `max(1, 100 + Σ lean × axes[axis] / 10)` with `PlynlingPersonality.Axes(profile)`, as `PlynlingEventEngine.Decide` does; rolls succeed at 50 %. The result is each ending's share of probability. In a `handover` scene the decider is the other Plynling, whose traits the walk does not know: there the profile is « neutre » and `trait` gates count as false.

- [ ] **Step 1: Write the failing checks** (small inline arcs built on `Valid` from Task 4).

```csharp
var ok = StoryWalker.Walk(ParseOne(Valid), new[] { ParseOne(Valid) });
Expect(ok.Problems.Count == 0, "valid arc walks clean: " + string.Join("; ", ok.Problems));
Expect(ok.Paths.Any(p => p.Alone), "an alone path exists");
ExpectProblem(Valid.Replace("[-> e3]", "[-> e2]"), "unreachable e3");
ExpectProblem(Valid.Replace("[-> e1]", "[-> one]"), "loop through one");
ExpectProblem(Valid.Replace("[flag met]", "[flag mett]"), "never set");            // met is tested, mett is set
ExpectProblem(Valid.Replace("[cailloux 20]", "[cailloux 90]"), "cailloux");       // 90 + the ending's 20 > 100
ExpectProblem(Valid.Replace("{a:sorti|sortie}", "{a:sorti|sortie"), "template");
var shares = ok.InCharacter.Single(x => x.Profile == "neutre").EndingShare;
Expect(Math.Abs(shares.Values.Sum() - 1) < 1e-9, "in-character shares sum to 1");
foreach (var arc in fixture.Arcs) foreach (var pr in StoryWalker.Walk(arc, fixture.Arcs).Problems) fail($"fixture walk: {arc.Key}: {pr}");
foreach (var arc in shipped.Arcs) foreach (var pr in StoryWalker.Walk(arc, shipped.Arcs).Problems) fail($"story walk: {arc.Key}: {pr}");
```

(`Valid` must contain the spots these `Replace` calls hit: a flag `met` set in scene one and tested in scene two, an option leading to `e3`, an option leading to `e1`, a `[cailloux 20]` outcome on a path that ends in an ending carrying `[cailloux 20]`, and a `{a:sorti|sortie}`. Adjust Task 4's `Valid` accordingly. `ExpectProblem(text, fragment)` parses, walks and fails unless a problem contains `fragment`.)

- [ ] **Step 2: Run, expect failures.**
- [ ] **Step 3: Implement** `StoryWalker.Walk`.
- [ ] **Step 4: Run, expect `ALL CHECKS PASSED`.**
- [ ] **Step 5: Checkpoint.**

---

### Task 7: The path report, version, docs

**Files:**
- Create: `tests/ProjectSYNCS.Checks/StoryReport.cs`
- Modify: `tests/ProjectSYNCS.Checks/StoryChecks.cs` (section « 34g »: the `story <arc>` argument)
- Modify: `ProjectSYNCS/config.yaml`, `ProjectSYNCS/CHANGELOG.md`
- Create: `docs/agents/plynling-storylines.md`
- Modify: `CLAUDE.md` (a table row and one line under the checks paragraph)

**Interfaces:**
- Consumes: Task 6.
- Produces: `public static class StoryReport { public static string Write(StoryArc arc, WalkResult walk); }`, and the run form `dotnet run --project tests/ProjectSYNCS.Checks -- story <arc>`, which also writes the report to `tests/ProjectSYNCS.Checks/out/<arc>.paths.txt` (`out/` is already in `.gitignore`) and prints that path. `story all` writes one per arc. Without the argument nothing is written.

The report, in French, in this order (layout of the spec's sample):
1. `<title> (<key>) — <n> scènes, <n> fins, <n> chemins`.
2. **Fins atteignables:** one line per ending: path count, shortest path length, and, when one ending is reached only through a locked option or a failed roll, `seulement via « <option> » <raté|réussi> (<lock>)`; an unreachable ending as `⚠ <key> 0 chemin — jamais atteinte`.
3. **Décidé seul:** the shortest alone path as `scene/option → …` with its stress (always 0 when deciding alone) and its ending; then **En personnage:** one line per profile, `<profil> : <fin> 62 % · <fin> 38 %` (shares rounded, endings under 1 % left out).
4. **Chemins:** each path numbered `Chemin i / n → <ending>`, each step `k. <scene title> — « <label> »` followed by the roll (`jet Sagesse 7 · réussi`), the lock (`🔒 Curieux`), the stress costs (`stress rowdy +10`), then its outcome prose with `{A}` left as is and conditional lines prefixed `[si …]`; at the end `Marques : …` (epithet titles in the masculine, keepsake names, cailloux, trait change) and `Mémoires : …` (wordings).

- [ ] **Step 1: Write the failing check.**

```csharp
var rep = StoryReport.Write(t, StoryWalker.Walk(t, fixture.Arcs));
Expect(rep.StartsWith(t.Header.Title + " (test_everything)"), "report title line");
foreach (var head in new[] { "Fins atteignables", "Décidé seul", "En personnage", "Chemin 1 / " })
    Expect(rep.Contains(head), "report has " + head);
Expect(t.Endings.Where(x => x.Key is not ("interrupted" or "loss")).All(x => rep.Contains(x.Key)), "report names every ending");
// The `story <arc|all>` argument (Interfaces above) writes the files; it is checked by hand in Step 4, not here.
```

- [ ] **Step 2: Run, expect failure.**
- [ ] **Step 3: Implement** `StoryReport.Write` and the `story` argument.
- [ ] **Step 4: Run, expect `ALL CHECKS PASSED`.** Then run `dotnet run --project tests/ProjectSYNCS.Checks -- story test_everything` and read `tests/ProjectSYNCS.Checks/out/test_everything.paths.txt` top to bottom: it must read as French prose, with every section present.
- [ ] **Step 5: Version and changelog.** `git diff ProjectSYNCS/config.yaml`; if clean, set `version: "7.55.1"` and add at the top of `CHANGELOG.md`:

```markdown
## 7.55.1 — <today>

- Plynlings: groundwork for the coming storylines (story files and their checks). Nothing changes in play yet.
```

- [ ] **Step 6: Docs.** Create `docs/agents/plynling-storylines.md`: what a story file is (pointer to the format spec for the full grammar), where files live (`ProjectSYNCS/Stories/`, the fixture under `tests/`), the keys-are-forever rule and `retired`, how to run the checks and the `story <arc>` report, and that the engine and startup load are not built yet. In `CLAUDE.md`, add a table row `| Plynling storylines: story files, the parser, the checker, the path report | docs/agents/plynling-storylines.md |` after the Plynling events row, and to the checks paragraph a sentence saying it also parses, validates and walks every story file (`-- story <arc>` writes the path report).
- [ ] **Step 7: Final run.** `dotnet build ProjectSYNCS -warnaserror` succeeds and `dotnet run --project tests/ProjectSYNCS.Checks` prints `ALL CHECKS PASSED`.
- [ ] **Step 8: Checkpoint** — tell the owner the batch is ready to commit, with the new version 7.55.1.
