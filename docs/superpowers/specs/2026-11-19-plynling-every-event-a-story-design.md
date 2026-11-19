# Every Plynling event a story file — design

2026-11-19. Extends the story format (`2026-10-29-plynling-story-file-format-design.md`, as changed by
`2026-11-17-plynling-village-events-design.md`) so the last 50 C# events can move to `.story` files.
Binding notes: `docs/agents/plynling-storylines.md`, `docs/agents/plynling-events.md`.

## Intent

The owner wants **one system, so every event can be edited as text**. After 7.74.2, 84 village arcs are
story files, but 50 events are still hand-written C# in `Helpers/Plynling/PlynlingEvents/`:

| File | Events | What the format lacks today |
|---|---|---|
| `Asks.cs` (11) | campfire, secret, letters (3 scenes), befriend (3 scenes) and their replies | asking the other owner; target filters; delay ranges |
| `Moments.cs` (15) | declare, rival, pact, mend and their replies; `on_teen`, `on_adult`; after a visit (+ its follow-up), bereaved, sick, recovered | asking; relation effects; a roll against the other; the triggers visit, death, sick, recovered |
| `GrowingUp.cs` (12) | the bébé dream (1 + 4 echoes two weeks later), the ado apprenticeship (1 + 3 lessons + 3 masters a week later) | delay ranges in days; three owner decisions in a short arc |
| `Breaks.cs` (12) | the mental breaks, levels 1–3 | `trigger: break n`; the coping-trait effect |

## Goals

- Every one of the 50 events becomes a scene in a `.story` file. **`PlynlingEvents` holds no hand-written
  event**: `All` is `StoryCatalog.Compiled`; the C# builders (`Plain`, `Try`, `Ask`, `Answer`, `E`, `Ai`,
  `Stress`, `Nothing`, `NoStress`) and the four catalog files are deleted.
- **Behaviour is unchanged for players**: same texts (as rewritten in Humeval in 7.74.2), same options and
  option keys, same effects, weights, targets, delays and leans. A player should not be able to tell.
- **Nothing stored is lost**: instances waiting under an old key still open, resolve and render; ado
  choices still feed the adult personality; break history still counts; journals keep their titles.
- Every new word is checked by the validator, walked by the checks, and appears in `test_everything.story`.

## Non-goals

- Changing any event's content, balance or frequency. The owner can edit the files afterwards; that is
  the point.
- New story features beyond what the 50 events need (handover scenes that continue the arc afterwards,
  `[other …]` effects, `relation`/`old_age` triggers, `condition chain`). They stay refused by the
  validator, « not supported yet ».
- Moving `PlynlingEvents.Removed` (titles of deleted events, for old journals) out of C#. It is data for
  history, not an event.

## Anti-goals

- A behaviour that lives in C# and cannot be read in the file (the 2026-10-29 anti-goal still binds).
- Any waiting instance cancelled by the deploy, or any old choice silently dropped from a history read.
- A second event engine. This is approach C again: files compile onto today's `EventDef`.

## Approaches

1. **Compile the new words onto what the engine already does — chosen by the owner.** A handover scene
   compiles to today's `Response` def, `trigger: break 2` to `BreakLevel: 2`, `trigger: sick` to
   `OnAction.FellSick`, a delay range to `FollowUp(min, max)`. The engine is barely touched; risk is low
   because play runs on code already in production.
2. **A story player replacing the compiled layer.** Cleaner on paper, but it rewrites the engine every
   event depends on and gives nothing more for editing as text: the files would look the same.
3. Moving everything except the asks was dropped: 19 events would stay in C#, which breaks « one system ».

## The format additions

Spelling below is binding unless marked *builder's call*.

### 1. Old keys: `was:`

A scene may carry `was: <old key>` (one per scene, under `title:`). The compiler adds `<old key> → arc.scene`
to the alias map `PlynlingEvents.ByKey` already reads (`StoryCompiler.Aliases`, today only for retired
scenes). Rules:

- An old key is claimed by **at most one scene** in the whole catalog; it must not contain a dot and must
  not collide with a live key (validator).
- **Every place that compares stored `EventKey`s with a computed list of keys includes the `was:` keys**
  of the matching defs. Today: `BreakKeys` and `PulseKeys` (`PlynlingService.Events.cs`), the on-action
  de-duplication `i.EventKey == def.Key` (same file), `StoryOpenerKeys` (`PlynlingService.Stories.cs`).
  Simplest is to compare `ByKey(i.EventKey)` with the def rather than raw keys (*builder's call* how).
- The checks hold a **frozen list of the 50 old keys and each one's option keys** (test data, written
  once from today's C# before it is deleted). A check fails if an old key is claimed by no `was:`, or if
  the claiming scene lacks one of its old option keys (live or `[retired]`).
- `Removed` stays for the 292 keys deleted in 7.74.2; the 50 moved keys need no entry (they resolve).

### 2. Asking the other owner: `handover`

The format already parses `handover` on a scene; the validator refuses it. It becomes supported for
**the last scene of a run only**:

- An option whose outcome is `[-> <scene>]` to a handover scene compiles to `AskTarget("<arc>.<scene>")`
  instead of a `FollowUp`. The handover scene compiles to an `EventType.Response` def. Everything after
  that is today's response machinery (queued on the other Plynling with `ParentInstanceId`, frozen and
  dead targets, deciding alone by stance).
- **On a handover card, `{A}` is the Plynling deciding (the other owner's) and `{B}` is the one who
  asked**, as on every card and as today's replies are written. Its gates, rolls, leans and stress read
  the decider, and **its effects land on the decider** (the reply's own owner chose them), as today's
  replies do (`reply_rival` grows the decider's Courage). *This replaces the 2026-10-29 builder's call*
  (« every other effect lands on the arc's owner »), because the reply is played by the existing Response
  engine on the other owner's card.
- A handover scene's options end with `[end]` only (no `[-> …]`, no ending): the run stops at the
  answer, as today. Continuing the asker's arc after the answer is a non-goal.
- A handover scene reads **no run flags** (the reply is a new instance on another Plynling): the
  validator refuses `[if flag …]` / `[needs flag …]` in it.
- A handover scene's options carry `[accept]` or `[refuse]` (the stance deciding alone leans on); an
  option with neither is neutral.
- An arc with a handover scene needs `other:` (already checked).
- **An asking option is written `[owner-only]`** unless the Plynling may ask alone. Today's `Ask` helper
  made it the hidden default (deciding alone never risks a refusal); in files it is always written, so it
  can be read. Today's exceptions (`tell` in the secret, `pact`, `mend`) simply omit it.
- The reply belongs to the other Plynling: the asker's story page (`PlynlingEventStory`) must not walk
  into it, and the reply's page shows only the reply. *Builder's call* how, but check both pages.

Two arcs share one reply today: `grown_letters_bridge` asks `reply_declare`. In files, **the letters arc
gets its own handover scene, a copy of the declaration reply** (same text and options, a new key, no
`was:`); `reply_declare` is claimed by the declare arc. Waiting instances keep working either way.

### 3. Who the other Plynling is

`other:` grows from one bond to (binding words, *builder's call* on the exact grammar if it reads better):

- `other: <bond>[, <bond>…]` — any of these bonds (`friends, bestfriends` for the secret). Rivals and
  enemies still mean a hostile target, every other bond a known one (today's rule).
- `other: anyone` — any living Plynling of the guild (`TargetKind.Anyone`).
- `other: hostile` — rivals or enemies (`social_rival`, `social_mend`).
- `other_if: <condition> …` — conditions on the candidate, compiled into `TargetCondition`. Needed:
  `relation <bond>`, `affinity >= n` (new: the pair's affinity), `can_couple` (new: `TargetInfo.CanCouple`).

Mapping of today's targets: campfire and befriend `other: anyone` + `other_if: [relation acquaintances]
[affinity >= 0]`; secret `other: friends, bestfriends`; letters and declare `other: known` +
`other_if: [can_couple]` (*builder's call*: `known` as a word for « any non-hostile bond it has met »);
pact `other: friends`; rival and mend `other: hostile`; visit and bereaved `other: anyone` (the trigger
supplies the target).

### 4. Triggers

| Word | Compiles to | Note |
|---|---|---|
| `break 1` / `break 2` / `break 3` | `Triggered`, `BreakLevel` n | already parsed |
| `sick` | `OnAction.FellSick` | already parsed |
| `recovered` | `OnAction.Recovered` | new word |
| `visit` | `OnAction.AfterVisit` | new word; the visit's other Plynling is `{B}` |
| `death` | `OnAction.Bereaved` | already parsed as `death <bond>`; the bond becomes optional (any mourner, as today); the one who died is `{B}` |

A trigger arc replaces the C# moment with the same on-action (`PickTriggered` already keeps story defs
only). `{T}` stays for adoption and stage arcs only. Break arcs: a free option is still required; **the
« not playing is free » rule does not apply inside a break** (today's `AppliesWhenAlone` exempts
`BreakLevel > 0`), so the validator must not refuse a coping trait or a negative modifier there.

### 5. Effects and rolls

| Tag | Compiles to | Where |
|---|---|---|
| `[coping]` / `[coping <trait>]` | `GainCoping` / `GainCoping(trait)` | break arcs only (validator) |
| `[couple]` | `Couple` | handover scenes only |
| `[heartbreak]` | `Heartbreak` | handover scenes only |
| `[bond peace]` / `[bond friends]` / `[bond bestfriends]` | `SetAffinityAtLeast(0)` / `(FriendsFrom + BondMargin)` / `(BestFriendsFrom + BondMargin)` | arcs with `other:` |
| `[roll <stat> vs]` | `EventChallenge(stat, 0, VsTarget: true)` | arcs with `other:` (*builder's call*: the fallback difficulty when no other is known stays 0, as today) |
| `[accept]` / `[refuse]` | `Stance` | handover scenes only |

The stakes line learns what it must say for these (a bond reached, a couple, a coping trait already
shows). `[needs trait bossy] [secret]` reproduces today's hidden `TraitGate`.

### 6. Delays: `pace:` ranges

`pace:` accepts `<n>h`, `<n>d` and a range `<min>-<max>` in the same unit (`14d-16d`, `24h-72h`), compiled
to `FollowUp(minHours, maxHours)`, hashed as every follow-up. `+Nh` keeps working. The 1–48 h cap applies
to `+Nh` only; a range may run up to 30 days (*builder's call* on the cap). Today's delays: 24–72 h,
24–48 h, 48–96 h, 168–240 h, 336–384 h.

### 7. Size

Short arcs allow **1–3 owner decisions per path** (today 1–2); a handed-over decision is not counted.
The apprenticeship, the letters and befriend each have three. Short arcs still give no marks and use
`[end]`; they never block the pulse (only open long events do), so a two-week echo does not stop daily
events.

## Files

One arc per file, under `ProjectSYNCS/Stories/<Family>/` (*builder's call* on arc keys and folders;
suggested): Friendship `campfire`, `secret`, `befriend`, `pact`; Love `declare`, `letters`; Rivalry
`rival`, `mend`; Milestone `teen`, `adult`; Personal `forgot`, `dream`, `apprentice`, `sick`, `recovered`;
Hardship `bereaved` and one file per break (12). Every scene that replaces a C# def carries its `was:`.
Texts are copied as they are today. Stages: an arc's `stage:` is its opener's (C# follow-ups and replies
were « any stage », but follow-ups and responses are never stage-checked when they come).

## Effects on the rest of the engine

- **The ado baseline moves slightly.** `PlynlingEventEngine.AdoPool` averages every def whose stages
  include Teen. Today the « any stage » follow-ups, replies and moments count; after the move they carry
  their arc's stages. The builder prints the baseline before and after in the checks output; a small
  shift is accepted (the owner's rule is « unchanged for players », and this is a normalisation constant,
  not a choice). *Builder's call*: if a scene-level `stage:` is cheaper than accepting the shift, add it.
- `PickBreak` orders by key, so which break a given Plynling gets at a given level may change once. Not
  stored; accepted.
- `/debug event`: its unknown-key list loses the C# section and lists arcs; an old key typed resolves
  through `was:`.
- Checks that use C# events as fixtures (`on_teen`, the breaks, the asks, `Reachability`) move to the
  story keys or to the old keys through `ByKey`. `Reachability` keeps its asking-pulse count (≥ 6) and its
  per-stage variety.

## Release

One batch, **7.74.3** (patch: no migration, no user-visible change). The changelog entry says the last
events moved to story files. Docs: `plynling-storylines.md` (the new words, handover rules, `was:`),
`plynling-events.md` (no C# catalog left), the 2026-10-29 spec gets a pointer to this one where it
changed (handover's effects, triggers). Dev-guild playthrough: one ask answered by a second owner, one
break, one stage moment, one waiting old-key instance resolved.

## Left to the builder

The exact grammar of `other:` / `other_if:`, folder and arc names, the range cap, how the stored-key
comparisons include aliases, and whether a scene-level `stage:` is worth it — each marked above.
