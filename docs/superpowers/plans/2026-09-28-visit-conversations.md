# Visit Conversations Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Turn a visit into an eight-step conversation whose faces follow the talk, shown as two side-by-side sprites the reader pages through with ◀ ▶.

**Architecture:** A new angry face in the art pipeline (14 new files). `PlynlingVisitStory.Build` stays pure: it now returns eight `VisitBeat`s, each carrying its text and a face per Plynling, computed from a defaults table and optional `[tag]`s at the start of lines. The card renders a `MediaGallery` of two and always shows the arrows; the timed playback is removed.

**Tech Stack:** .NET 10, Discord.Net 3.20.1 (Components V2, `MediaGalleryBuilder`), Python 3 + Pillow for the art.

**Spec:** `docs/superpowers/specs/2026-09-28-visit-conversations-design.md`

## Global Constraints

- User-facing text French; code, comments, logs English. **Never commit** — the owner commits.
- Verification = `dotnet build` in `ProjectSYNCS/` with 0 warnings / 0 errors + the scratch harness (`<scratchpad>/passions-harness`) printing `OK`.
- Art: **nothing moves side to side**; new files only, no `ART_VERSION` / `PlynlingArt.Version` bump.
- Visit faces: `happy`, `content`, `sad`, `angry` only.
- Tag grammar at the start of a line (or of either half of a two-line entry): `[face]` or `[A:face]`, `[B:face]`, `[A:face B:face]`.
- Two-line entries keep exactly one `\n`. `{P}` never after `de` / `à` / `d'`.
- `<scratchpad>` = `C:\Users\c235773\AppData\Local\Temp\claude\C--Users-c235773-Desktop-Sources---RME-Discord-Bots-SessionOrganizer\a4f17c04-d805-4b53-a4d2-dff3721d6e42\scratchpad`.

---

### Task 1: The angry face (art) — owner approval gate

**Files:** `tools/plynling-art/sprites.py`, `motion.py`, `species.py`, `export.py`

- [ ] **Step 1: Face.** In `sprites.face`, add a branch before `elif state == "sleeping"`:

```python
    elif state == "angry":
        for x0 in (12, 18):                                             # narrowed: the top row is lidded
            for x in (x0, x0 + 1):
                P(x, 20, INK)
                P(x, 21, INK)
            P(x0, 20, WHITE)
        for x, y in ((11, 17), (12, 18), (13, 19), (20, 17), (19, 18), (18, 19)):   # brows in a V
            P(x, y, INK)
        for x, y in ((14, 25), (15, 24), (16, 24), (17, 25)):           # a tight frown
            P(x, y, INK)
        blush(ANGRY_FLUSH)
```

and `ANGRY_FLUSH = (232, 96, 96)` beside the other colours.

- [ ] **Step 2: Extras.** In `sprites.extras`, add after the heart:

```python
    if state == "angry":
        anger_mark(px, pose)
        steam(px, pose)
```

with, above `extras`:

```python
ANGER = (226, 58, 58)
STEAM = (236, 236, 240)


def anger_mark(px, pose):
    """The 💢 at the top right: four little corner brackets that pulse in place."""
    big = pose.pulse if pose else 0
    d = 1 if big else 0
    for (cx, cy), (sx, sy) in (((25, 3), (-1, -1)), ((29, 3), (1, -1)), ((25, 7), (-1, 1)), ((29, 7), (1, 1))):
        x, y = cx + sx * d, cy + sy * d
        px(x, y, ANGER)
        px(x - sx, y, ANGER)
        px(x, y - sy, ANGER)


def steam(px, pose):
    """A small puff rising straight up from the top left, fading as it climbs."""
    f = pose.f if pose else 0
    k = f % 8
    a = max(60, 220 - k * 22)
    for dx, dy in ((0, 0), (1, 0), (0, -1), (1, -1)):
        px(4 + dx, 9 - k + dy, STEAM, a)
```

- [ ] **Step 3: Motion.** In `motion.Pose.__init__`, after the happy hop:

```python
        # angry: two short stomps — straight up and down, one pixel each
        if state == "angry":
            self.hop = 1 if f in (3, 11) else 0
        # angry: the anger mark pulses, big every other quarter-beat
        self.pulse = 1 if state == "angry" and (f // 4) % 2 == 1 else 0
```

(`self.pulse` must also exist, 0, for every other state — the line above does that.) Frame 0 stays the rest pose.

- [ ] **Step 4: Coprin ink.** Add `"angry": 2` to `COPRIN_INK` and `"angry": 0` to `COPRIN_BABY_INK` in `species.py`. Grep `species.py` for any other per-state dict and add `angry` to it the same way (as `"content"`).

- [ ] **Step 5: Export list.** `export.STATES` gains `"angry"`.

- [ ] **Step 6: Preview.** Run from `tools/plynling-art`:

```bash
python -c "from common import SPECIES, sheet; from sprites import build; sp=list(SPECIES); print(sheet(sp, ['content','happy','sad','angry'], lambda s, st: build(st, s), sp, ['content','happy','sad','angry'], r'<scratchpad>/angry_preview.png', K=5)); print(sheet(sp, ['angry'], lambda s, st: build(st, s, stage='baby'), sp, ['angry (bébé)'], r'<scratchpad>/angry_baby_preview.png', K=5))"
```

Send both images to the owner (`SendUserFile`). **Stop until approved**; iterate on Steps 1–3 if asked.

- [ ] **Step 7: Export.** `python export.py` → the count grows by 14. Confirm `assets/plynlings/plynling_cepe_angry_v4.webp` and `plynling_cepe_baby_angry_v4.webp` exist and that no existing file changed (`git status assets/` shows only additions).

- [ ] **Step 8: Art README.** In `tools/plynling-art/README.md`: moods list gains angry (V brows, narrowed eyes, flush, a pulsing 💢 and a rising puff, two vertical stomps); file count updated; note the 14 files were added without a version bump.

---

### Task 2: `PlynlingMood.Angry` in the bot

**Files:** `ProjectSYNCS/Helpers/PlynlingCatalog.cs:9`, `ProjectSYNCS/Helpers/PlynlingCardUi.cs` (`MoodLabel`)

- [ ] **Step 1:** `public enum PlynlingMood { Content, Happy, Sad, Hungry, Starving, Frozen, Sleeping, Angry }` — appended, with a comment: "Angry is a visit face only: PlynlingLife.Mood never returns it."
- [ ] **Step 2:** `MoodLabel`: `PlynlingMood.Angry => gender.Agree("fâché", "fâchée"),`.
- [ ] **Step 3:** Build → 0/0.

---

### Task 3: The eight-step engine with faces

**Files:** `ProjectSYNCS/Helpers/PlynlingVisitStory.cs`, `ProjectSYNCS/Helpers/PlynlingPassions.cs` (`PassionInfo` gains `Details`), `<scratchpad>/passions-harness/HarnessMore.cs`

**Interfaces (produced):**

```csharp
public sealed record VisitCast(string Name, PlynlingSpecies Species, PlynlingStage Stage, string SpeciesName,
    PlynlingGender Gender, IReadOnlyList<Passion> Passions)
{
    public string Sprite(PlynlingMood face) => PlynlingArt.Sprite(Species, Stage, face);
}
public sealed record VisitBeat(string Text, PlynlingMood VisitorFace, PlynlingMood HostFace);
public sealed record VisitStory(string Id, string Heading, IReadOnlyList<VisitBeat> Beats, VisitCast Visitor, VisitCast Host, uint Accent);
public sealed record FaceTag(PlynlingMood? Both, PlynlingMood? A, PlynlingMood? B);
public static (string Text, FaceTag? Tag) PlynlingVisitStory.Untag(string line);
// pools
Reactions[(mood, shared)]            // single lines now (B)
AnswerFollowups[(mood, shared)]      // A
Followups[(mood, shared)]            // B
Closers[mood]                        // either; {S} = who says it
CustomDetails                        // A, {P}
PassionInfo.Details                  // A, per passion
```

- [ ] **Step 1: Harness first.** Replace the story section of `HarnessMore.Run` so it also:
  - collects every pool including the new ones and, for each line and each half of two-line entries, runs `Untag` and checks the text no longer starts with `[` and that any tag parsed;
  - checks `Reactions`, `AnswerFollowups`, `Followups`, `Closers`, `CustomDetails`, `Details` have **no** `\n`; `Openers`, `CustomOpeners`, `Exchanges` have exactly one;
  - builds 200 stories from two test `Plynling`s (all four gender pairs, shared / unshared / custom passions, every `VisitMood` via `GoodScene`/`After`, confession accepted/refused) and checks: 8 beats, no `{`, no `[`, faces within {Happy, Content, Sad, Angry}, beat 3's bubble names B.

Test Plynlings: `new Plynling { Id = 1, Name = "Aa", Species = PlynlingSpecies.<first enum value>, Gender = …, Passion = …, TaughtPassion = … }`; the outcome: `new VisitOutcome(v, h, [], [], good, before, after, confession, 0.25)`.

- [ ] **Step 2:** Run → compile errors. Expected.

- [ ] **Step 3: Records and tags.** Add the records above (replacing `VisitCast` / `VisitStory`) and:

```csharp
    private static readonly Regex TagRx = new(@"^\[(?:(happy|content|sad|angry)|(?:A:(happy|content|sad|angry))?\s*(?:B:(happy|content|sad|angry))?)\]\s*", RegexOptions.Compiled);

    public static (string Text, FaceTag? Tag) Untag(string line)
    {
        var m = TagRx.Match(line);
        if (!m.Success || m.Length == 0) return (line, null);
        static PlynlingMood? F(Group g) => g.Success ? Enum.Parse<PlynlingMood>(g.Value, ignoreCase: true) : null;
        return (line[m.Length..], new FaceTag(F(m.Groups[1]), F(m.Groups[2]), F(m.Groups[3])));
    }
```

`Cast` builds `VisitCast` with `PlynlingLife.Stage(p, now)` and `p.Species`.

- [ ] **Step 4: Faces.**

```csharp
    // Defaults: (speaker, listener, narration) by mood.
    private static (PlynlingMood Speaker, PlynlingMood Listener, PlynlingMood Narration) Faces(VisitMood mood) => mood switch
    {
        VisitMood.Acquaintances => (PlynlingMood.Happy, PlynlingMood.Content, PlynlingMood.Content),
        VisitMood.Rivals => (PlynlingMood.Happy, PlynlingMood.Angry, PlynlingMood.Content),
        VisitMood.Conflict => (PlynlingMood.Angry, PlynlingMood.Angry, PlynlingMood.Angry),
        _ => (PlynlingMood.Happy, PlynlingMood.Happy, PlynlingMood.Happy),
    };
```

A small mutable `FacePair { Visitor, Host }` helper applies a narration tag (`Both` → both, `A` → visitor, `B` → host) or a spoken tag (`Both` → the speaker only).

- [ ] **Step 5: `Build`.** Rewrite to produce eight beats:
  1. Arrival — narration faces; place scene + arrival (tagged).
  2. Opener — A speaks: default speaker/listener faces; the opener's narration half applies as narration, its spoken half as A's.
  3. Reaction — B speaks: `Reactions[(mood, shared)]`; if shared and catalog, a `SharedLines` line appended (narration tag).
  4. and 5. Talk — `talker = rng.NextDouble() < 0.7 ? other(previous) : previous`; A says `rng.Next(2) == 0 ? (info?.Details ?? CustomDetails) : AnswerFollowups[(mood, shared)]`; B says `Followups[(mood, shared)]`.
  6. Closer — `closer = rng.Next(2) == 0 ? A : B`; `Closers[mood]` expanded with `{S}` = closer.
  7. Activity — narration faces; activity (tagged) + the `Exchanges` pair, each half's tag on its own speaker (visitor, host).
  8. Departure — outcome faces (refused confession or break-up → Sad both; accepted → Happy both; newly enemies → Angry both; else narration); departure tag; outcome lines.

  Each talking beat's text is `💬 **{name}** : {line}` (with the opener's narration line before it on step 2). `{S}`/`{L}` in conversation lines mean A/B throughout (as today); only `Closers` use the closer as `{S}`.

- [ ] **Step 6: Pools, first cut.**
  - `Reactions`: keep the first half of every current entry (the harness forbids `\n` now).
  - `AnswerFollowups`: the second halves, same keys (rewritten in Task 5 where needed).
  - `Followups`, `Closers`, `CustomDetails`, `PassionInfo.Details`: two real lines each to start (filled in Task 5).
  - `PassionInfo` gains `string[] Details` (after `Activities`), filled per passion.

- [ ] **Step 7:** Build → 0/0; harness → `OK`.

---

### Task 4: The card — gallery, arrows only

**Files:** `ProjectSYNCS/Commands/PlynlingPlayCards.cs` (`BuildVisitStory`), `ProjectSYNCS/Interactions/Components/PlynlingComponentHandler.cs`

- [ ] **Step 1: `BuildVisitStory(VisitStory story, int beat)`** (drop the `arrows` parameter):

```csharp
        beat = Math.Clamp(beat, 0, story.Beats.Count - 1);
        var step = story.Beats[beat];
        return new ComponentBuilderV2()
            .AddComponent(new ContainerBuilder()
                .WithAccentColor(new Color(story.Accent))
                .AddComponent(new TextDisplayBuilder($"## {story.Heading}"))
                .AddComponent(new MediaGalleryBuilder()
                    .AddItem(story.Visitor.Sprite(step.VisitorFace), story.Visitor.Name, false)
                    .AddItem(story.Host.Sprite(step.HostFace), story.Host.Name, false))
                .AddComponent(new SeparatorBuilder())
                .AddComponent(new TextDisplayBuilder($"{step.Text}\n-# {beat + 1}/{story.Beats.Count}")))
            .AddComponent(new ActionRowBuilder()
                .WithButton("◀", VisitPrevId(story.Id, beat), ButtonStyle.Secondary, disabled: beat == 0)
                .WithButton("▶", VisitNextId(story.Id, beat), ButtonStyle.Secondary, disabled: beat == story.Beats.Count - 1))
            .Build();
```

Update its doc comment (two sprites side by side, alt text = name, arrows from the start).

- [ ] **Step 2: Handler.** The accept flow posts `BuildVisitStory(story, 0)` as the follow-up and returns; delete `PlayStoryAsync`, `BeatPause` and the `Task.Run`. `PageStoryAsync` calls `BuildVisitStory(story, beat + step)`.

- [ ] **Step 3:** Build → 0/0 (fix every `BuildVisitStory` call site). Harness `OK`.

---

### Task 5: Writing pass

Authoring, not code; the harness enforces format. Targets:

| Pool | Target |
|---|---|
| `PassionInfo.Details` | 10 per passion |
| `CustomDetails` | 10 |
| `Followups[(mood, shared)]` | 5 per key (60) |
| `Closers[mood]` | 6 per mood (36) |
| `AnswerFollowups` | every line reviewed; lines that only made sense after their first half rewritten |

Rules: single lines (no `\n`); A's lines are about their passion and read fine twice in a row; B's lines react or ask; closers lead into doing something; agreements `{s:}`/`{l:}` (A/B), closers `{s:}` = the one talking; no place assumed; family-neutral.

- [ ] Fill, build, harness `OK`.

---

### Task 6: Tag pass

- [ ] Go through Arrivals, Openers, SharedLines, Activities, Combos, CustomOpeners, CustomActivities, Exchanges, Departures, Reactions, AnswerFollowups, Followups, Closers, Details, CustomDetails; add a tag where the line's emotion clashes with the defaults table (sad line among friends → `[sad]`, a sting between acquaintances → `[angry]`, a calm line in a conflict → `[content]`…). Build, harness `OK`.

---

### Task 7: Docs and hand-over

- [ ] `CLAUDE.md` — the "A visit is told as a story" paragraph: eight steps, faces (table + tags), `MediaGallery` of two (the phone trade-off), arrows only (no timer, no background edits), snapshot = species + stage + per-step faces. Replace the "The card is one `Section` with a thumbnail per Plynling" sentence.
- [ ] `README.md` visits bullet: eight steps, faces, ◀ ▶.
- [ ] Final build + harness; hand over for the dev-guild run. Do not commit.
