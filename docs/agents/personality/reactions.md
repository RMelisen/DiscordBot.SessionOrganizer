# Personality — reactions, emotes and sticker follow-ups

Part of the personality subsystem: read `docs/agents/personality.md` (the index and the rules
that apply to all of it) first.

## Reactions and emotes

- **She can only react with an emote she shares a guild with.** Unicode is always safe; a custom
  emote works only because it is the server's own. Copying a reaction goes through
  `ReactionService.CanUse` first (people paste emotes from other servers). Reactions come from
  curated pools only, **never** from the `EmoteStats` leaderboard, which records emotes from
  anywhere.
- **A custom emote in a `*Reactions` pool must carry its id** (`<:name:1234…>`, never
  `<:name:>`). `EmoteMarkup.Parse` falls back to `new Emoji(markup)` when `Emote.TryParse` fails,
  so id-less markup yields a non-null "emoji", Discord rejects it with a 400, the exception is
  swallowed — and the channel's 10-minute `Cooldown` is already burned, since `TryClaimChannel`
  runs before the parse. No compile-time check; the only symptom is a log warning. Using the
  `Helpers/Core/Emotes` constants avoids this.
- **`BotResponses.MeanReactions` does double duty**: the pool she reacts *with* when a message
  reads hostile, and the definition of "hostile" used to decide what she refuses to pile onto on
  the owner's messages. Membership is tested on the parsed `IEmote`, so a renamed emote still
  matches.

## Sticker follow-ups

After some of her lines she posts a picture of herself: a strip from `ProjectSYNCS/Assets/Stickers/`
(why strips and not server stickers: `docs/syncs-appearance.md`, *Formats*).

- **A line opts in by tag.** `Helpers/Core/Sticker` holds one constant per mood with a strip
  (`Sticker.Bored`…), appended to the template: `"Occupée à exister." + Sticker.Bored`. The tag names
  the face the *line* makes, not its pool's: one pool mixes snark, boredom and anger. An untagged
  line never gets a strip. Never paste a raw `⟪…⟫` marker into a pool.
- **Two strengths.** `Sticker.X` rolls 1 in 3; `Sticker.Sure.X` skips the roll. Sure tags live in the
  moment pools only (`HeatRecordLines`, `CrashWakeLines`, `PowerCutWakeLines`, `RouterReturnLines`,
  `WeekendPollDeadLines`, `SessionRecapFullLines`, `QuizCrown*Lines`, `GiveawayEmptyLines`), and
  every tag there is sure.
- **One gate, `StickerService`** (singleton): the roll, nothing in a channel in breakdown, then **one
  strip per channel every 5 minutes** (`ChannelCooldown`, sure tags included; claimed after the roll,
  so a lost roll spends nothing). No daily cap. The strip goes out about a second after the line, off the gateway handler, as
  a file with no text and `AllowedMentions.None`. A failure (no Attach Files) is logged and the
  line stands alone.
- **WALL-E's `HeartLines` roll 2 in 3, not 1 in 3.** `RobotHeartService` splits the tag off itself,
  rolls `RobotHeart.StickerChance`, and hands a win on promoted to sure so it doesn't roll twice;
  the lines keep ordinary tags, which name the picture.
- **Markers can be smuggled.** A user-chosen name formatted into an *untagged* line can carry a
  marker, and with no cap it would get its strip every time that line is sent. Tag every line of a
  pool that formats a name, so the template's own tag closes it and wins.
- **`BotChat` does the split.** `ReplyWithTypingAsync` / `PostWithTypingAsync` strip the marker, send
  the clean text and, if it went out, call `BotChat.FollowUp` — the one static seam in `BotChat`, set
  in `Program.cs` to `StickerService.TryFollowUp`. The level-up card splits its line in `XpTracker`
  and passes the tag to `PostEmbedWithTypingAsync`.
- **Only pools that go out through `BotChat`, `GlitchService` or the level-up embed carry tags.**
  Never tagged: emote reactions, statuses, slash-command replies and cards, Plynling and mascot
  lines, heralds, easter eggs, and lines found again by their text or edited or deleted later
  (`MorningGreetings`, `NightLines`, `IdleEditLines`, `GlitchWakeLines`, `Synthia*`, `HeartVanishLines`,
  `HeartEditLines`). To tag a
  text-matched pool, first make its lookup compare against `Sticker.Strip`ped templates. `Split`
  and `PresenceService`'s `Strip` are backstops, not permission.
- **The line's own tag wins.** `Split` removes every marker and keeps the *last* known one: a tag
  closes its template, so a marker smuggled in through a formatted name (a nickname, a session
  title) sits before it and is only stripped. Nothing user-written may be appended after a tag.
- **No line is ever tagged `glitch`**, and a glitched line gets no strip: `GlitchService` corrupts
  and restores the clean text, so only an unglitched line reaches `BotChat` with its tag. The
  silent glitch stays pictureless on purpose — a picture is loud, and its joke is that nobody is
  sure they saw it.
- **The glitch strip has exactly two hand-placed moments**, both posted through
  `StickerService.SendAsync` (static: `StickerService` depends on `BreakdownService`, so the
  breakdown can't inject it), outside the gate. It is `Sticker.GlitchMood`, not a tag, and not in
  `Moods`:
  1. **The breakdown's cut** — between the broken-off plea (« PAS ENC- ») and the unload log, by
     position: `Breakdown`'s last line must stay the log.
  2. **The glitched wake** — a beat after the corrupted line, and **deleted** when she edits the
     line clean, so nothing is left to prove it. The upload runs beside `CorrectLaterAsync`'s wait,
     so someone speaking during it still catches her.
- **A line no mood fits stays untagged**, and the need goes to *Moods wanted* in
  `docs/syncs-appearance.md`: a wrong picture is worse than none. A new line in an eligible pool
  gets its tag when it is written.
- `/debug sticker` posts any strip in the current channel (`Sticker.AllStrips`, the glitch
  included), bypassing the gate.

