# Chat requests become actions — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** A request aimed at SYNCS (session, `/tonight`, vote, reminder, teams) is read in detail and answered with an action instead of a pointer to the command.

**Architecture:** A pure reader (`Helpers/Personality/ChatRequest`) pulls the details out of the message using the existing date readers. `ChatterService` acts on it off the gateway handler, beside her live answers. Offers that need a click live in a singleton `ChatOffers` (in memory, 1 h); the click handlers reuse the private flows of `ScheduleModule`, `VoteModule` and `/tonight`.

**Tech Stack:** .NET 10, Discord.Net 3.20, EF Core / SQLite. No migration.

**Spec:** `docs/superpowers/specs/2026-11-17-chat-requests-to-actions-design.md`

## Global Constraints

- Only messages aimed at her (mention, reply, her name); nothing unprompted.
- User-facing strings French; code, comments, logs English.
- Her lines follow `docs/syncs-voice.md`; pools sent through `BotChat` carry a `Sticker` tag; ephemeral lines carry none.
- Titles, games, options and reminder text are the person's own words: `Format.Sanitize`, mentions off, capped at `InputCaps`.
- Every click handler defers before the database; `[ComponentInteraction]` in a `[Group]` module needs `ignoreGroupNames: true`.
- Minor bump (new user-visible behaviour): `7.63.0` → `7.64.0`, with a CHANGELOG entry. The owner commits by hand: no commits.

## Review Focus

- A date word inside a title (« organise un Midi-Minuit ») or a bare number (« fais 3 équipes ») must not become the time.
- « fais gaffe demain » and other non-session imperatives with a date must stay non-requests.
- « on fait une session samedi ? » stays `NextSession` (asking what's planned), read before the request.
- Someone else clicking an offer, or a click after a restart, must be told so, privately.
- A reminder with no readable time, past, or over 60 days must not be saved; 15 pending is the cap.

---

### Task 1: The reader and the cues

**Files:**
- Create: `ProjectSYNCS/Helpers/Personality/ChatRequest.cs`
- Modify: `ProjectSYNCS/Services/Personality/MessageCues/MessageCues.IntentHelpers.cs` (`ReadRequestTopic`), `MessageCues.TalkCues.cs` (word lists), `MessageCues.Intents.cs` (passes the text)
- Test: `tests/ProjectSYNCS.Checks/Program.cs`, new section 50

**Interfaces — Produces:**
- `public sealed record SessionAsk(string Title, DateTimeOffset? At)`; `ChatRequest.ReadSession(string content, DateTimeOffset nowUtc) -> SessionAsk`
- `public sealed record TonightAsk(string Game, DateTimeOffset? At)`; `ChatRequest.ReadTonight(string content, DateTimeOffset nowUtc) -> TonightAsk` (At only when it is today, Paris time, or before 6:00 tomorrow)
- `public sealed record ReminderAsk(string Text, DateTimeOffset? Due)`; `ChatRequest.ReadReminder(string content, DateTimeOffset nowUtc) -> ReminderAsk`
- `public sealed record VoteAsk(string Title, IReadOnlyList<string> Options)`; `ChatRequest.ReadVote(string content) -> VoteAsk` (2–10 options or none)
- `public sealed record TeamsAsk(int Count, bool Balanced)`; `ChatRequest.ReadTeams(string content) -> TeamsAsk` (count 2–4, default 2)
- `ChatRequest.HasWhen(string text) -> bool` — some run of words reads as a day or time (`FrenchDate`), used by the cues.

**Algorithm (not determined by the signatures):** split into word tokens keeping their spans in the original text; the time is the longest run of at most 8 tokens that the topic's reader accepts (`SessionTime.Parse` or `ReminderTime.Parse`), ignoring runs with no letter, `:` or `/`; the rest is the title/text once the request words, articles, generic topic nouns (« session », « soirée », « partie »…) and their « de », her name and politeness are stripped from both ends, rebuilt from the original spans.

- [ ] Step 1: checks, section 50 — reader cases, each with its expected value, at a fixed Tuesday 2026-11-17 15:00 Paris:
  - `ReadSession("syncs organise un Valorant samedi 21h")` → `("Valorant", sat 21-11 21:00)`
  - `ReadSession("tu peux créer une session de Among Us demain soir stp ?")` → `("Among Us", wed 18-11 21:00)`
  - `ReadSession("on se fait un lol 5v5 vendredi à 20h30 ?")` → `("lol 5v5", fri 20:30)`
  - `ReadSession("organise une session")` → `("", null)`; `ReadSession("organise un raid")` → `("raid", null)`
  - `ReadTonight("lance un tonight pour Minecraft à 22h")` → `("Minecraft", today 22:00)`; `ReadTonight("qui est chaud ce soir ?")` → `("", today 21:00 or null)`
  - `ReadReminder("rappelle-moi demain 9h de sortir les poubelles")` → `("sortir les poubelles", wed 09:00)`
  - `ReadReminder("tu peux me rappeler dans 2h d'appeler maman ?")` → `("appeler maman", +2h)`
  - `ReadReminder("rappelle-moi de manger")` → `("manger", null)`
  - `ReadVote("fais un sondage : pizza, sushi ou kebab")` → options `[pizza, sushi, kebab]`; `ReadVote("lance un vote pizza ou sushi")` → `[pizza, sushi]`; `ReadVote("lance un sondage")` → none
  - `ReadTeams("fais 3 équipes équilibrées")` → `(3, true)`; `ReadTeams("fais des équipes")` → `(2, false)`; `ReadTeams("fais les teams en quatre")` → `(4, false)`
- [ ] Step 2: cue cases (`ReadIntent` / `ReadRequestTopic`): « organise un valo samedi 21h » Session; « lance un among us demain » Session; « on se fait un valo samedi 21h ? » Session; « tu peux organiser un lol vendredi ? » Session; « fais-moi penser à appeler maman » Reminder; « fais gaffe demain » None; « on fait une session samedi ? » NextSession; « organise un valo ce soir » Tonight.
- [ ] Step 3: implement; run `dotnet run --project tests/ProjectSYNCS.Checks` → ALL CHECKS PASSED.

### Task 2: Offers, answers and clicks

**Files:**
- Create: `ProjectSYNCS/Services/Personality/ChatOffers.cs` (singleton), `ProjectSYNCS/Services/Personality/ChatterService/ChatterService.Requests.cs`, `ProjectSYNCS/Commands/Scheduling/ScheduleModule/ScheduleModule.FromChat.cs`, `ProjectSYNCS/Interactions/Components/ChatRequestComponentHandler.cs`
- Modify: `ChatterService.Listening.cs` (`TryAnswerFromRecords` hands Requests over), `VoteModule.cs` (`StartModal(title, options)` + click handler), `ScheduleModule.cs` (ctor takes `ChatOffers`), `TonightModule.cs` (`OpenAsync` shared by the command and the click), `BotChat.cs` (embed post with components and reply), `Program.cs` (register `ChatOffers`), `AnswerKinds.cs` (`Request(RequestTopic)`)

**Interfaces — Produces:**
- `ChatOffers.Add(ChatOffer offer) -> int`, `ChatOffers.TryGet(int id, out ChatOffer offer) -> bool`; `ChatOffer(ulong AskerId, string Title, DateTimeOffset? At, IReadOnlyList<string> Options)`
- Custom-ids: `chatreq:session:{id}`, `chatreq:vote:{id}`, `chatreq:tonight:{id}`, `chatreq:remundo:{reminderId}`

- [ ] Step 1: implement the per-topic answers (table in the spec); a refusal (no voice, too few people, 15 reminders, time unread or out of range) answers in words, never silently.
- [ ] Step 2: click handlers: asker check (`RequestNotYoursLines`, ephemeral), expiry (names the command), defer first.
- [ ] Step 3: `dotnet build -warnaserror` → 0 errors.

### Task 3: Lines, docs, version

**Files:** `BotResponses.Replies.Requests.cs` (`RequestActionLines` keyed Session/Tonight/Poll, `OwnerRequestActionLines`, `RequestNotYoursLines`, `TeamsNoVoiceLines`), `BotResponses.cs` index, checks section 1 pool list, `docs/agents/personality/intents.md`, `docs/agents/scheduling.md`, `README.md`, `/help` if it lists chat abilities, `config.yaml` 7.64.0, `CHANGELOG.md`.

- [ ] Step 1: write the pools (6 lines each, `{2}` = what she understood), add them to the checks' tagged pools.
- [ ] Step 2: docs and bump; run the checks and the build again.
