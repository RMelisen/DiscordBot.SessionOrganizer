# CLAUDE.md

Project S.Y.N.C.S. — a French-language Discord bot for gaming sessions, polls and votes, with a
personality, XP and a virtual-pet game (Plynlings). Features: `README.md`.

This file holds only what applies **everywhere**. **Read the subsystem's file before changing that
subsystem**; its rules bind like these. A new rule goes in the subsystem file unless it truly
spans subsystems; write the rule and its reason, not the story — git keeps the history.

| Working on… | Read first |
|---|---|
| Chatter, reactions, `MessageCues`, favouritism, rivals, `/yesno`, `/rate` | `docs/agents/personality.md` |
| "good bot" / "bad bot", `BotFeedbackTracker`, `/goodbot` | `docs/agents/bot-feedback.md` |
| XP, `/level`, `/leaderboard`, voice XP, `/emotestats`, badges, « Succès » | `docs/agents/xp.md` |
| `/shame` | `docs/agents/shame.md` |
| Sessions, polls, votes, giveaways, `ReminderService`, `/tonight`, `/teams`, `/remindme` | `docs/agents/scheduling.md` |
| Pop quiz, `QuizBank`, `/quiz leaderboard` | `docs/agents/quiz.md` |
| Plynlings (life, care, visits, relations, art) | `docs/agents/plynling.md` |
| Plynling traits, stats, personality, events | `docs/agents/plynling-events.md` |
| Plynling RPG: body stats, levels and points, Énergie, `/plynling train` | `docs/agents/plynling-rpg.md` |
| Plynling storylines: story engine, story files, `StoryParser`, `StoryValidator`, walk checks, path report | `docs/agents/plynling-storylines.md` |
| Writing any Plynling line | `docs/plynling-writing-style.md` |
| Plynling storylines, or anything set in the village (Humeval) | `docs/village-lore.md` |
| Writing any line SYNCS says in her own voice | `docs/syncs-voice.md` |
| Any picture of SYNCS (avatar, emotes, poses, moods) | `docs/syncs-appearance.md` |
| Cailloux, inventory, items, icons, cosmetics, `/admin dashboard` | `docs/agents/economy.md` |
| `/arcade` games, `ArcadeTables`, `ArcadeRunner`, `ArcadeService` | `docs/agents/arcade.md` |
| Her Minecraft body, `MinecraftBodyService`, `MinecraftBody/`, `/minecraft` | `docs/agents/minecraft.md` |
| Her Minecraft orders (`McCommand`, gathering, errands, crafting, the camp), free time | `docs/agents/minecraft-orders.md` |
| How her Minecraft body moves: water, fights, body language, `tools/mc-pathtest` | `docs/agents/minecraft-body.md` |
| `/confess`, the free Steam games alert | `docs/agents/community.md` |
| `/admin`, `/config`, `/debug`, permissions, `/report`, monthly digest, joins/leaves, activity counting | `docs/agents/admin.md` |
| A loop, a `Ready` hook, a gateway subscription, a dated ranking, a data migration, a server-specific id; `PresenceService` | `docs/agents/architecture.md` |
| `Dockerfile`, `run.sh`, `config.yaml` beyond the version, CI, state files under `/data` | `docs/agents/deployment.md` |

## Commands

```bash
cd ProjectSYNCS
dotnet build                     # net10.0
dotnet run                       # needs Discord:Token
dotnet ef migrations add <Name>  # applied automatically on startup
dotnet user-secrets set "Discord:Token" "<token>"
dotnet user-secrets set "Discord:DevelopmentGuildId" "<id>" # instant registration

dotnet run --project tests/ProjectSYNCS.Checks   # prints failures, exits 1 on any
```

Keep `Discord:RegisterCommandsGlobally` `false` in dev: guild commands register instantly (with
`deleteMissing: true`, so the dev guild's list is replaced every start), global ones take an hour.

The bot needs the privileged intents `GuildMembers` and `MessageContent` (without it the
personality reads empty strings). **`AlwaysDownloadUsers` must stay on**: the intent only permits
the member download, the flag performs it; without it `Guild.GetUser` is null for anyone unseen
since restart and the avatars on `/level`, `/leaderboard`, `/shame` fall back to Discord's logo.

**`tests/ProjectSYNCS.Checks`** checks how she reads chat (`MessageCues`, `Opinion`, `ShipAsk`), the
pools that answer it, the listening pieces (streaks, facts, `/debug answers`), and parses,
validates and walks every Plynling story file (`-- story <arc|all>` writes path reports to its
`out/`). Run it before shipping a change to cues, pools or stories, and add a case for every
misfire found in the wild. It is a plain console outside `ProjectSYNCS/` (whose csproj compiles
every `.cs` under it), not in the image, no bump. CI only builds the image. Otherwise verify by
building and, when it matters, running against the dev guild. "The harness" in other notes means
the owner's scratch checks, not in this repo.

## Language

**Command and option names are English; every other user-facing string — descriptions, replies,
embeds, labels, choice names, errors — is French.** Code, comments and logs are English. Renaming a
command or option changes what people type: rarely, and in one batch.

## Architecture

`Program.cs` is the composition root: DI, `MigrateAsync()`, hosted services. **`BotService`** does
login, command registration, interaction dispatch and owns every message and reaction
subscription; connection hooks (`Ready`…) live in the service that needs them. **Every
`BackgroundService` loop has its own interval on purpose** — never share one.

Layers: `Commands/` (slash modules + `static` card builders), `Interactions/` (`Components/`
handlers, `Modals/` DTOs, `Autocomplete/`), `Services/` (EF repositories + behaviour), `Models/`,
`Data/AppDbContext.cs`, `Helpers/` (pure logic — put anything checkable without a gateway here).
These are split into subsystem subfolders named after `docs/agents/`, plus `Core/` for shared
pieces. **Folders are not namespaces**: every file keeps the flat `ProjectSYNCS.Commands` /
`.Helpers` / `.Services` / `.Models` namespace, so moves change no `using` nor the EF snapshot. A
new file goes in its owning subsystem's subfolder.

**Big classes are `partial` over a folder named after the class**, one file per theme, each under
~40 KB so it can be read whole; start a new theme file rather than letting one swell. **A static
field's initializer never reads a static field in another file** (cross-file order is undefined,
it may read null): a catalog spread over files is one method per file, joined in the field
(`PlynlingEvents.All`).

**Commands are grouped by whose thing it is**: `/plynling` the creature, `/inventory` the person's
belongings, `/admin` moderation, `/config` settings, `/debug` the owner's tools, `/arcade` her
games, `/minecraft` her Minecraft life. **Discord allows 25 subcommands per top-level command**
and the 26th throws at startup — count before adding. `/pl` re-registers `/plynling` (read
`plynling.md` before touching `PlynlingModule`). Component handlers live in
`Interactions/Components/`; modules keep the commands and the static builders.

**DI lifetimes are not arbitrary.** `AppDbContext` and services wrapping it are **transient**.
Anything with in-memory state (cooldowns, bounded sets, line history, caches) is a **singleton** —
as transient it silently loses its state. **Singletons never inject a DB service**: they take
`IServiceProvider` and open `CreateAsyncScope()` per unit of work, or one `AppDbContext` gets
pinned for the process lifetime. All in-memory state resets on restart **by design**.

## Conventions that will bite you

### Data

- **SQLite cannot translate `DateTimeOffset` comparisons.** Filter booleans/ids in SQL,
  `ToListAsync()`, then apply date windows and ordering in memory
  (`EventService.GetActiveEventsAsync`). `.Where(e => e.ScheduledAt > now)` throws at runtime.
- **Snowflakes are `ulong`, SQLite integers signed**: every snowflake property needs
  `.HasConversion<long>()`. A derived property needs `[NotMapped]` (`EmoteStat.Markup`) or EF wants
  a column.
- **Anything stored by int or string key is append-only**: int enums (`PlynlingSpecies`,
  `PlynlingBond`, `JournalKind`, `DeathCause`, `UptimeEventKind`, passions) and keys (item,
  cosmetic, badge, economy metric). Inserting mid-list or renaming silently turns or orphans rows.
- **Totals + daily buckets** is the one pattern for dated rankings: an all-time totals table plus a
  per-day table keyed by `Day` (`int` `yyyymmdd` from `AppTime.DayKey`, so windows filter in SQL),
  both written in the same call. **The buckets do not sum to the totals and must not be made to** —
  older history lives only in the totals. Instances, removal rules and bucket-only tables:
  `architecture.md`.
- **No `HasMaxLength`** (SQLite ignores it): cap at the input (below).
- **Migrations are schema-only** except seven deliberate data migrations; read `architecture.md`
  before writing one.

### Time and culture

**Never `DateTime.Now`.** Prod runs in UTC; wall-clock goes through `Helpers/Core/AppTime`
(`Europe/Paris`, DST-aware via `TryParseWallClock`). Store UTC `DateTimeOffset`, show Discord
`<t:unix:…>`. **Globalization stays on** (`InvariantGlobalization` false, `libicu72` in the
Dockerfile). **A `/` or `:` in a date format is the culture's separator**, and the bot sets no
culture: without `Fr` or `InvariantCulture`, quote it (`{d:dd'/'MM}`).

### Interactions and custom-ids

- **Slash-command modules derive from `SyncsModuleBase`**, not `InteractionModuleBase`, and answer
  through its methods. A command that opens a modal or answers through `Context.Interaction` carries
  `[NoPremiumPrank]` (personality, *Premium easter egg*), or a pranked use breaks.
- **`[ComponentInteraction]` / `[ModalInteraction]` inside a `[Group]` need
  `ignoreGroupNames: true`**, or the handler silently never fires.
- **Custom-ids are a contract across files** (`PollModule` builds `schedule:finalize:…` for
  `ScheduleModule`; `/vote list` reuses `poll:republish`): grep the project before renaming one.
- **Every button row needs its own custom-id verb.** A message carrying the same custom-id twice
  (**disabled buttons included**) is rejected (`COMPONENT_CUSTOM_ID_DUPLICATED` — crashed prod).
  Give each row its own verb (`…:win:`, `…:view:`, `…:page:`) delegating to one shared `ShowAsync`.
- **Modal DTOs and hand-built `ModalBuilder`s stay in sync** (`ScheduleModule.BuildEditModal`,
  `OnRetryAsync`, `PollModule.OnCategoryPickedAsync`): a field changed in one place fails to bind.
- **Card builders are `static` and shared** by module, handlers and loops.
- **Every module reading `Context.Guild` carries `[CommandContextType(InteractionContextType.Guild)]`**:
  prod registers globally and global commands are DM-enabled, where `Context.Guild` is null.
  (`[EnabledInDm(false)]` is obsolete and fails `-warnaserror`.)

### Discord's hard caps

These throw at **send** time, often with no length in the log: 25 select options (`.Take(25)`),
5 buttons per row, 80-char button labels, 100-char select labels, 1000-char scheduled-event
description, 2000-char message, embed title 256, field value 1024, **6000 across a message's
embeds**, **40 components per Components V2 message counting the whole tree** (throws in
`ComponentBuilderV2.Build()`). Keep builders static and Context-free so they can be measured, and
**re-sum components before adding to a V2 card**. `/help` and `/plynling help` are near 6000.

**Cap user text at the option**: `[MaxLength(n)]` on slash options, `maxLength:` on
`[ModalTextInput]`, so nothing over-long reaches the database and breaks re-renders. Numbers live
in `Helpers/Core/InputCaps`; `Title` is shared by several modals that must agree.

### Components V2

A `MessageFlags.ComponentsV2` message has **no content and no embeds**, **re-asserts the flag on
every `UpdateAsync`**, and sends with **`AllowedMentions.None`** (a `TextDisplay` is real content:
`<@id>` pings; `None` keeps the pill silent). Avatars go through `Helpers/Core/AvatarUi`.

### Button colours

**One meaning per colour**, so a card reads at a glance (grey choices once read as decoration and
players missed their turn):

- **Green `Success`** — yes, I'm in, commit: Rejoindre, Accepter, Publier, Terminer, Acheter.
- **Red `Danger`** — no, stop, or it can hurt: Refuser, Annuler, Clôturer, Abandonner, « Doubler ».
  In a confirm step, red is the dangerous answer (« Oui, annuler ») and « Non » is grey. Placing a
  bet is the game's pick (blurple); raising a stake already in play is red.
- **Blurple `Primary`** — do this; **every "pick one of these"** (visit and event choices, quiz
  answers); the selected tab. A button that opens a form or the next step (« Créer une session »)
  is blurple; green is the answer or the final commit inside that flow.
- **Grey `Secondary`** — neutral: pagers, unselected tabs, disabled leftovers.

The one exception is the visit's « 💌 », green among blurple options because it is the special move.

### Mentions and hostile text

- **Relayed text never mass-pings**: sent on someone's behalf with
  `new AllowedMentions(AllowedMentionTypes.Users)`, quoted through `MessageFormat.Quote`. The
  absence notice to the owner uses `AllowedMentions.None`.
- Messages **meant** to ping narrow to users (giveaway winners) or one id (the visit's first card),
  never roles or `@everyone` (`BotChat.PostWithTypingAsync` takes an optional `AllowedMentions`).
- **User-chosen names and texts are hostile** (Plynling names, taught passions): `Format.Sanitize`,
  mentions off.

### Reliability

- **Discord side effects never break the flow**: swallow and log (`SessionEventSync` without Manage
  Events; reminder DMs catch `CannotSendMessageToUser`).
- **Answer an interaction within 3 s**; the Pi's database can outrun that. A handler touching the
  database before showing anything defers first (`DeferAsync`, then follow-ups /
  `ModifyOriginalResponseAsync`), or the work saves while the click looks dead.
- **An in-memory claim taken before a save is released if the save throws.**
- **An exception escaping a hosted loop stops the whole bot.** Every sweep catches **per item**, not
  per pass; anything added to a sweep goes inside the existing per-item `try`
  (`BackgroundServiceExceptionBehavior.Ignore` is not the fix — the loop dies silently). The real
  exposure is `SQLITE_BUSY` on the Pi.
- **Never a database write per message.** High-frequency counts go through `ActivityCounter`
  (flushed every 5 min by `ActivityFlushService`, see admin): add a counter there.

### Shared helpers — use them, don't re-inline

- **`CooldownGate<TKey>`** ("claimed recently?") and **`BoundedSet<T>`** ("seen before?") share the
  mechanism, **never the policy**: each service owns its instances and durations; two trigger
  populations never share one. `CooldownGate` owns its lock; `BoundedSet` owns none (callers need
  the add atomic with their own state).
- **`BotChat`** is the single send path for her chatter (typing pause inside the 3 s
  `HandlerTimeout`, swallow-and-log); **`EmoteMarkup.Parse`** the single reaction parser.
- **`Sticker`** holds the sticker markers: a line opts in with `+ Sticker.Bored`, only in pools sent
  through `BotChat` (see `personality/reactions.md`, *Sticker follow-ups*).
- **Pick response lines with `ResponsePicker.Pick(pool)`**, which skips the pool's
  `min(50, length / 2)` most recent lines — memory is **global** across channels and DMs. Pick the
  template *before* `string.Format` (a stray brace throws). A pool spent once a day uses
  **`DailyRotation`** (date-computed; a restart wipes the picker).
- **Time-of-day greetings go in a `TimedPool`** (`DayPart.cs`): « bonjour » in `day`, « bonsoir »
  in `evening` (Paris, day is 3 h–18 h), the rest in `anyTime`; pick from `.Now` or `.At(instant)`.
  A pool that only runs at one time stays a plain array. Checks section 45 polices the halves.
- **Custom emote markup lives only in `Helpers/Core/Emotes`**, as `const string` pairs (`XId` +
  `X`), so lines stay constant. Never raw `<:name:id>` in a pool.
- **`BotResponses.DisplayNameFor(IUser)`** is the only home of `Nickname ?? GlobalName ?? Username`
  (plus family nicknames).

### Authorization — three models, don't conflate them

- `SessionPermissions.CanManage` — the organizer, or Administrator / ManageGuild (sessions, polls).
- `SessionPermissions.IsStaff` — Administrator / ManageGuild **or** the owner (`/admin`, `/config`,
  staff Plynling actions). Those two carry **no `[DefaultMemberPermissions]`** (no bit can say
  "plus the owner"): the handler's `IsStaff` check is the only gate.
- Owner-only — `Context.User.Id == AvailabilityService.OwnerId`, inline (`/debug`).

### Where she speaks

Anything she posts **on her own initiative** checks `SilenceService.IsSilent` first (`/debug
silence`, see admin); answers to commands and buttons, and notices a command set up (reminders,
recaps, draws), don't. Low-stakes unprompted posts (Quokka's visits and the like) go to the bots
channel via `BotsChannel.ResolveAsync` — never hardcoded, never the main channel. Ones that matter
(deaths, morning hello, recaps) keep their own channels.

### Docs

`/help` (`HelpModule`), `README.md` and the members' guide (`guide/`) are hand-maintained: a new
user-facing command or rule updates all three — except owner-only commands. Easter eggs
(breakdown, quoicoubeh, the glitch) and hidden triggers stay out of all three.

**`guide/`** is the French user guide, built by MkDocs Material (`mkdocs.yml`, `strict`) and
published to GitHub Pages by `.github/workflows/guide.yml`; `/help` links to it
(`HelpModule.GuideUrl`). Pages are for members: how to use things, no internals. A page added
goes in `mkdocs.yml`'s `nav`. Checks section 69 fails on any command whose full path
(`/admin xp add`) appears in no page. The guide is written by her, in her voice
(`docs/syncs-voice.md`): steps, tables and numbers stay plain; her personality goes in the opening
face card (`<div class="syncs-hello" markdown>`) and her asides (`!!! syncs <mood> ""`, the mood
picks a face from `guide/assets/syncs/`). Look and 404 page: `guide/stylesheets/syncs.css`,
`guide-theme/`.

**A notes file under `docs/` stays under ~40 KB**; past that it becomes an index plus a folder of
topic files of the same name — read the index, then only the topic you need.

Search skips generated and spent files (`.ignore`, ripgrep only); pass their path to search them.

## Version and deployment

`ProjectSYNCS/config.yaml` is the **single source of the version** (the csproj parses it;
`/help`'s footer shows it). **Every change to the bot bumps it** — Home Assistant only offers an
update when it changes.

- **Once per batch**: if `git diff` shows `config.yaml` already changed, don't bump again (the
  owner commits by hand).
- **Patch**: lines, facts, emotes, fixes, art, tweaks. **Minor** (patch reset): a new command,
  subcommand, subsystem, user-visible behaviour, or any migration. **Major**: the owner's call only.
- **No bump** for `docs/`, `guide/`, `guide-theme/`, `mkdocs.yml`, `README.md`, `CLAUDE.md`, `CHANGELOG.md`,
  `tools/`, `tests/`, `.github/`.
- Numbers don't roll over (`5.11.26` is fine). Say the new number in your summary.
- **A bump adds an entry to `ProjectSYNCS/CHANGELOG.md`** (newest first, `## x.y.z — yyyy-mm-dd`,
  one short English bullet per user-visible change; extend the entry if already bumped). It must
  stay beside `config.yaml` (Home Assistant shows it). No easter-egg triggers or member names — the
  repo is public. Changelog-only edits need no bump.

Only `/data` persists on the Pi (database and state files); packaging and the image pipeline:
`deployment.md`. The remote is **public**: real tokens go in user secrets or add-on options, never
a tracked file.

## Hardcoded ids

Some literal snowflakes are tied to one server; the list, and which `/config` can extend or
replace, is in `architecture.md` (a new one gets a line there). `OwnerId` is deliberately **not**
configurable — it gates `/debug` and the DM relay.
