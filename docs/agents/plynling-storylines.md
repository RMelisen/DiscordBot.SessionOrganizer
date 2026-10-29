# Plynling storylines — story files

The event rework into fewer, deeper arcs (design: `docs/superpowers/specs/2026-10-28-plynling-storylines-design.md`;
the file format in full: `docs/superpowers/specs/2026-10-29-plynling-story-file-format-design.md`). The world
every arc lives in is `docs/village-lore.md`; the prose follows `docs/plynling-writing-style.md`.

**Built so far: the format, its parser, its validator and the checks.** The engine (runs, steps, the
living message, the card entry, `/debug` tools), its tables and the startup load are not built: no
story reaches a player yet, and the old event catalog (`plynling-events.md`) runs as before.

## Files

- **One arc per file** under `ProjectSYNCS/Stories/<Family>/<key>.story`, UTF-8; CRLF, LF and a BOM all
  read the same. `ProjectSYNCS/Stories/village.cast` lists the villagers stories may name
  (`key | given name | byname`), from the bible. The folder is copied to the build and publish output.
- **The fixture** `tests/ProjectSYNCS.Checks/Stories/test_everything.story` uses every tag, condition and
  effect once. It is never shipped. A new word in the format gets a line there, or the checks fail.
- **Code:** `Helpers/Plynling/Stories/` — `StoryModel` (records), `StoryTags` (brackets, quotes, words),
  `StoryVocabulary` (conditions and effects), `StoryParser` (+ `.Scenes`), `StoryValidator`,
  `StoryLibrary` (loads a folder). Pure: no gateway, no database. The walks and the report live in the
  checks project (`StoryWalker`, `StoryReport`).

## Rules that bite

- **Keys are forever** once the engine ships: arc, scene, option, ending, memory, epithet, keepsake,
  flag. Never rename or remove one. A scene taken out stays as `## scene: <key>` + `retired -> <other>`;
  an option as `* <key> | <label> [retired]`. Memory, epithet and keepsake keys are global across files.
- **Every problem is reported**, with file and line, never just the first. Parser and validator errors
  are in English; the path report is in French (the owner reads it as content).
- **The validator is the gate the engine will use at startup**: unknown keys, dangling `[-> …]`, a
  missing `interrupted` ending (or `loss` for love and friendship arcs), a scene without a free option
  (no needs, not owner-only, not secret, no stress cost), ranges, template tokens, and Discord's caps
  measured with 32-character names and each agreement at its longer form.
- **The walks** (checks only): every scene and ending reachable (the `loss` branch counts, since the
  engine jumps there), no loop, 3–5 endings besides `interrupted` and loss-only ones, every flag set is
  tested and every flag tested is set (`flag <arc>.<f>` in another arc counts), deciding alone never
  dead-ends, at most 100 cailloux and one trait change per path, no template left in any of the four
  gender pairs. The arc's own flags are followed exactly; every other condition counts as possible.
- **Marks** (epithet, keepsake, trait, unlock) only in endings; a trait swap stays within one
  personality group.

## Checking an arc

```bash
dotnet run --project tests/ProjectSYNCS.Checks                      # every check, story files included
dotnet run --project tests/ProjectSYNCS.Checks -- story <arc>       # also writes out/<arc>.paths.txt
dotnet run --project tests/ProjectSYNCS.Checks -- story all
```

The report (`tests/ProjectSYNCS.Checks/out/`, gitignored) lists the reachable endings and what each needs,
the shortest path decided alone, where an in-character Plynling ends (each childhood trait for a bébé
arc, else each trait the arc names; rolls at 50 %, other conditions false), then every path as
reading: the scene's text, the choice, its roll, lock and stress, the outcome, the ending, its marks and
memories. Agreements show both forms (« sorti/sortie », « seul(e) »), conditional lines as `[si …]`.

Per-arc workflow (from the storylines spec): premise → branch diagram → owner review → the story file →
owner review → checks and report → playthrough on the dev guild once the engine exists.
