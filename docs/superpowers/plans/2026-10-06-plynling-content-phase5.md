# Plynling events — Phase 5 (content wave 1) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Bring the pulse catalog to the spec's first batch — **at least 5 bébé, 5 ado and 10 adulte/ancien** pulse events — with ten new events inspired by Crusader Kings III situations and written in the Plynlings' voice. Later waves (10–15 events each) reuse this plan's process unchanged.

**Architecture:** No engine change. Situations are picked from the CK3 game files (read from a folder **outside the repo**), shortlisted for the owner, written into a scratch **writing sheet** the owner reviews, then generated into `Helpers/PlynlingEvents.cs` and checked by the harness — the same workflow as `PlynlingScripts`.

**Tech Stack:** the catalog types and harness from phases 2–4.

**Spec:** `docs/superpowers/specs/2026-10-06-plynling-events-design.md` — Phase 5. **Depends on phases 2–4** (the content may use every effect: growth, affinity, stress costs and `StressChange`, `ApplyModifier`, `FollowUp`, `AskTarget`, gates, challenges, `OwnerOnly`, `TargetCondition`).

## Global Constraints

- **Never commit or push.** The owner commits.
- **CK3 files never enter the repo** (public remote; Paradox's files). Read them from the owner's folder; nothing is copied or translated — situations and mechanics only, every line rewritten.
- **Writing:** `docs/plynling-writing-style.md` (re-read before writing). Cozy, dry, specific; a turn in each scene; their village world (the tortoise at the café, the owl librarian, the heron on the old bridge, the hedgehog station master, the sparrow who judges contests, the snail…); cailloux; berries, acorns, honey, jam. Never a cap, petals or spores; never family. **No « il/elle/ils/elles »** in event text; `{a:m|f}` / `{b:m|f}` for agreements.
- **Every event:** 2–4 options; at least one **ungated, stress-free, not owner-only** option (deciding alone); option labels ≤ 80 characters; keys `snake_case`, prefixed by stage (`baby_`, `teen_`, `grown_`), unique, **never renamed once shipped**.
- **Balance per event:** at most one `GrowStat` per outcome, amount 1; stress costs 10–40, only on options that go against a trait; negative modifiers only behind owner choices (the harness re-checks the 60-day simulation from phase 3).
- **Two review gates with the owner:** the shortlist (Task 1) and the writing sheet (Task 2). No C# before the sheet is approved.

## Review Focus

1. **A new event that is never drawn** (stage, condition or target condition too narrow): the harness simulation reports every new key's draw count over 90 days × 300 ids; zero fails. (Task 3.)
2. **A trait gate or stress cost naming a trait key that does not exist** — caught by the phase-2 invariant on trait keys. (Task 3 runs it.)
3. **A line that reads wrong for one gender pair** — the expansion loop prints every failing pair; the owner reads the girl/girl and boy/girl expansions of each new event in the sheet. (Tasks 2–3.)
4. **A social event whose target condition no Plynling on the server can meet** — the simulation's draw count shows it; the dev test forces it with `/debug event … target:`. (Tasks 3–4.)
5. **Two new events using the same trick** (style guide: no trick twice in a batch) — the sheet lists each event's trick; the owner checks the column. (Task 2.)

---

### Task 1: The shortlist from CK3

**Files:**
- Create: `$SCRATCH/events-wave1-shortlist.md` (scratch, not in the repo)

- [ ] **Step 1: Get the source**

Ask the owner for the folder holding their copy of the CK3 files (for example `C:\Users\c235773\Desktop\ck3-ref\`, containing `events\` and `localization\english\` or `localization\french\`). If they have not copied them, list what to copy: `…\steamapps\common\Crusader Kings III\game\events\` and `…\game\localization\english\` (or `french\`). Wait for the path.

- [ ] **Step 2: Survey**

Read, in that folder: the childhood event files (names containing `childhood`), the yearly/random event files (names containing `yearly`), and the stress/coping event files (names containing `stress`) — use `Glob` with `**/*childhood*.txt`, `**/*yearly*.txt`, `**/*stress*.txt` under `events/`, and the matching `*_l_english.yml` (or French) localization for their titles and descriptions. Only read; never copy a file anywhere.

- [ ] **Step 3: Write the shortlist**

`$SCRATCH/events-wave1-shortlist.md`: **20 candidate situations**, a table with one line each:

| # | CK3 source (file + event id) | Situation in one sentence, retold for the village | Stage | Mechanic it would exercise (gate / challenge / stress / modifier / follow-up / social) |

Aim for 7 bébé, 7 ado, 6 adulte/ancien candidates, with every mechanic appearing at least twice and at least 3 social candidates. No CK3 sentence is quoted — situations are retold in your own words.

- [ ] **Step 4: Owner gate**

Show the shortlist to the owner and ask them to pick **10**: 3 bébé, 3 ado, 4 adulte/ancien (at least one social). Wait for the choice.

---

### Task 2: The writing sheet

**Files:**
- Create: `$SCRATCH/events-wave1.md`

- [ ] **Step 1: Re-read the voice**

Read `docs/plynling-writing-style.md` in full, and the six phase-2 starter events plus the phase-4 events in `Helpers/PlynlingEvents.cs` as the reference register.

- [ ] **Step 2: Write each chosen event**

For each of the 10, in `$SCRATCH/events-wave1.md`, this exact block:

```markdown
## <key>  ·  <stage>  ·  <Pulse | social: Known/Hostile/Anyone + condition>
**Title:** <title, ≤ 40 chars>
**Trick:** <the style-guide trick the scene's turn uses>
**Scene:** <one or two sentences, a hook and a turn>

| Option key | Label (≤ 80) | Gate | Challenge | Outcome (success) | Outcome (failure) | Effects (success / failure) | Stress costs | AI |
|---|---|---|---|---|---|---|---|---|
| … | … | — or trait/stat | — or Stat + difficulty | … | — or … | e.g. `+1 Dip` / `—` | — or `honest 20` | e.g. `Soc +2, Com +1` |

**Read for girl/girl and boy/girl:** <the scene and the longest outcome expanded twice, by hand>
```

Rules while writing: one turn per scene; the deciding-alone option is a real choice, not a shrug; a challenge's failure is funny, never cruel; a social event's text never assumes intimacy unless its target condition guarantees it (`Known` covers acquaintances).

- [ ] **Step 3: Self-check against the style checklist**

Go through the style guide's « Checklist before a batch ships » for the sheet, plus: every event has its deciding-alone option; no trick appears twice (the **Trick** lines); no « il/elle »; every label ≤ 80. Fix the sheet.

- [ ] **Step 4: Owner gate**

Send the sheet to the owner (`SendUserFile`, `display: render`) and wait for approval or corrections. Apply corrections in the sheet, then show it again until approved.

---

### Task 3: Into the catalog, checked

**Files:**
- Modify: `ProjectSYNCS/Helpers/PlynlingEvents.cs` (append the 10 events after the phase-4 events)
- Test: `$SCRATCH/personality/Program.cs`

- [ ] **Step 1: Add the coverage and reachability checks**

In `$SCRATCH/personality/Program.cs`, insert before the final `Console.WriteLine`:

```csharp
// ==== PHASE 5 — coverage and reachability =================================================================
int Pulses(PlynlingStage s) => PlynlingEvents.All.Count(e => e.Type == EventType.Pulse && e.BreakLevel == 0 && e.Stages.Contains(s));
Check(Pulses(PlynlingStage.Baby) >= 5 && Pulses(PlynlingStage.Teen) >= 5 && Pulses(PlynlingStage.Adult) >= 10, $"first batch coverage ({Pulses(PlynlingStage.Baby)}/{Pulses(PlynlingStage.Teen)}/{Pulses(PlynlingStage.Adult)})");

// Every pulse event is drawn: 300 Plynlings × 90 days per stage, all targets available, no recent window.
var draws = PlynlingEvents.All.ToDictionary(e => e.Key, _ => 0);
var everyKind = new HashSet<TargetKind> { TargetKind.Known, TargetKind.Hostile, TargetKind.Anyone };
foreach (var stage in new[] { PlynlingStage.Baby, PlynlingStage.Teen, PlynlingStage.Adult })
    for (var id = 1; id <= 300; id++)
    {
        var ts = PlynlingTraits.Draw(id, stage, Array.Empty<string>());
        var px = Fresh(id);
        var cx = new EventContext(px, ts, PlynlingStats.Compute(px, ts), stage);
        for (var d = 0; d < 90; d++)
            if (PlynlingEventEngine.PickPulse(id, 20261001 + d % 28 + d / 28 * 100, PlynlingEvents.All, cx, Array.Empty<string>(), everyKind) is { } picked)
                draws[picked.Key]++;
    }
foreach (var e in PlynlingEvents.All.Where(e => e.Type == EventType.Pulse && e.BreakLevel == 0))
    Check(draws[e.Key] > 0, $"{e.Key} is drawn at least once ({draws[e.Key]})");
Console.WriteLine("draws: " + string.Join(", ", draws.Where(kv => kv.Value > 0).OrderByDescending(kv => kv.Value).Select(kv => $"{kv.Key} {kv.Value}")));
```

(`PickPulse` only hashes the day key, so these keys need not be real dates — they just have to differ.)

- [ ] **Step 2: Run it to see the coverage fail**

Run: `cd "$SCRATCH/personality" && dotnet run`
Expected: `FAIL` with « first batch coverage (2/2/6) ».

- [ ] **Step 3: Generate the events**

Append the approved sheet's 10 events to `PlynlingEvents.All`, after the phase-4 events, with the helpers already in the file (`Plain`, `Try`, `Ask`, `E`, `Stress`, `Ai`, `TraitGate`, `StatGate`, `EventChallenge`). One sheet row → one `EventOption`; « Effects » → `E(...)` with `GrowStat`, `AffinityShift`, `StressChange`, `ApplyModifier`, `FollowUp`, `AskTarget`; « Stress costs » → `Stress((key, n), …)`; « AI » → `Ai((axis, w), …)`. Copy the text verbatim from the approved sheet.

- [ ] **Step 4: Run the harness**

Run: `cd "$SCRATCH/personality" && dotnet run`
Expected: `OK`, and a `draws:` line listing every pulse event. If a new event is drawn far less than its stage-mates (under a fifth of the median), widen its condition or raise its weight, and tell the owner.

- [ ] **Step 5: Build**

Run: `cd "$REPO/ProjectSYNCS" && dotnet build -warnaserror` — Expected: `Build succeeded`.

- [ ] **Step 6: Files for the owner to commit**

`ProjectSYNCS/Helpers/PlynlingEvents.cs` — `Plynling events: content wave 1 (10 events)`.

---

### Task 4: Read them live, then ship

**Files:**
- Modify: `ProjectSYNCS/config.yaml`

- [ ] **Step 1: Dev-guild read-through (with the owner)**

For each of the 10 keys: `/debug event key:<key>` (add `target:` for a social one), open it, read the choice card, pick each option in turn on fresh forced copies (one `/debug event` per option), and page the story to the end. Note any line that reads badly in context; fix it in the sheet first, then in the C#, and re-run Task 3's harness.

- [ ] **Step 2: Version**

In `config.yaml`, bump the patch: `5.15.0` → `5.15.1`.

- [ ] **Step 3: Build**

Run: `cd "$REPO/ProjectSYNCS" && dotnet build -warnaserror` — Expected: `Build succeeded`.

- [ ] **Step 4: Files for the owner to commit**

`ProjectSYNCS/Helpers/PlynlingEvents.cs`, `ProjectSYNCS/config.yaml` — `Plynling events: wave 1 polish, 5.15.1`.

---

## Later waves

Run this plan again with `wave2`, `wave3`… in the scratch file names: 10–15 events per wave, shortlist → sheet → catalog → read-through, keeping the stage balance (roughly a quarter bébé, a quarter ado, half adulte/ancien) and about a third social. Patch version per wave.
