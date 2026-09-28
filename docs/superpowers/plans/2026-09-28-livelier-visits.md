# Livelier visits — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** A visit plays out as a 3-beat story in a place, shaped by the pair's bond, on a
phone-friendly card that can be paged back with ◀ ▶.

**Architecture:** `VisitAsync` still decides everything. A pure `Helpers/PlynlingVisitStory` turns
the outcome into a place and three beats; `VisitStories` (singleton) keeps the last 300; the card
builder renders a beat; the handler plays the beats by editing the message.

**Tech Stack:** C# / .NET 10, Discord.Net 3.20 Components V2, the scratch harness.

Spec: `docs/superpowers/specs/2026-09-28-livelier-visits-design.md`.

## Global Constraints

- No rule changes: affinity, bond, confession, happiness, finds and badges stay `VisitAsync`'s.
- Lines are French, agree with the pair (« ils » / « elles ») and with each Plynling; names are
  `SafeName`d and every send passes `AllowedMentions.None`.
- Background edits swallow and log their errors.
- Distinct custom-id verbs: `vis:prev:{story}:{beat}`, `vis:next:{story}:{beat}`.
- Checks first; build `-warnaserror`; commit per task; push only when asked.

---

### Task 1: The story

**Files:** `Helpers/PlynlingVisitStory.cs`.

**Produces:** `record VisitPlace(string Emoji, string Name, int FromHour, int ToHour)`;
`record VisitStory(string Id, VisitPlace Place, IReadOnlyList<string> Beats, int VisitorId, int HostId)`;
`PlynlingVisitStory.Places`, `PlaceFor(DateTimeOffset now, Random rng)`,
`Build(VisitOutcome outcome, string outcomeLines, DateTimeOffset now, Random rng)`.

- [ ] Checks: places at their hours; each bond's pool; a bad scene → conflict; agreement for every
  gender pair; no « { » left; beat 3 ends with the outcome lines.
- [ ] Implement; build; run; commit « Wrote the visit stories ».

### Task 2: The card, the playback and the arrows

**Files:** `Services/VisitStories.cs` (singleton, registered), `Commands/PlynlingPlayCards.cs`
(`BuildVisitStory(story, beat, visitor, host, now, arrows)` replacing `BuildMeeting`),
`Interactions/Components/PlynlingComponentHandler.cs` (play the beats; `vis:prev`, `vis:next`).

- [ ] Checks: small thumbnails (no MediaGallery), under 40 components, arrows only when asked,
  distinct ids, disabled at the ends, « 2/3 »; the store keeps 300 and forgets the oldest.
- [ ] Implement; build; run; commit « Played visits as a little story ».

### Task 3: Docs and version

- [ ] README, `/plynling help` (caps re-checked), CLAUDE.md; commit; version bump as its own commit
  once both plans are done; push only if asked.
