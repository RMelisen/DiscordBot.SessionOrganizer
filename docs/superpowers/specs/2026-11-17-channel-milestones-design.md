# Channel message milestones — design

## Why

A small "wow" moment: SYNCS notices when a channel reaches its 1 000th, 10 000th, 100 000th
message and replies to that exact message. The server has about 20 people, so milestones are rare
events worth celebrating. The owner also wants to look back: who reached which milestone, and when.

## Goals

- Know each channel's message count, history included, though Discord gives no total.
- Announce a milestone the moment it is reached, by replying to the message that reached it.
- `/debug milestones`: the current channel's count, the milestones reached (who, when, a link)
  and the next one.

## Non-goals and anti-goals

- **Never announce a milestone that is already past** (owner's rule): anything crossed while she is
  reading history (the first catch-up, or after a restart) is recorded silently.
- No threads, forum posts or voice-channel chats.
- No public command; `/debug` is owner-only.
- Never a database write per message (root `CLAUDE.md`).

## Decisions (owner's)

- Milestones: **1 000, 10 000, 100 000, then every 100 000**.
- **Deleted messages are uncounted**, when she sees the deletion (`MessageDeleted`,
  `MessagesBulkDeleted`) and the message was already counted.
- Debug command ephemeral, current channel, lists each milestone and who reached it.
- Saving every 5 minutes is more than 20 people need: **every 30 minutes**, only the channels that
  changed, plus once on a clean stop.

## Approaches

1. **History read oldest first, then live counting (chosen).** Each channel is read from its
   creation onwards, 100 messages a request, counting in order, so the message reaching each
   milestone (and its author) is known exactly. Once caught up, new messages count live, in memory.
   A bookmark (the last message counted) makes a restart cheap and exact.
2. *Live counting only.* No history: the counts would start at 0 today, so the real 1 000th or
   10 000th could never be found. Rejected.
3. *Discord's message search total.* Not available to bots reliably, and gives no order (no "who
   wrote the 10 000th"). Rejected.

## Design

**State (two tables, one migration):**

- `ChannelMessageCount` — one row per (guild, channel): `Count` (long), `Cursor` (the id of the last
  message counted, a snowflake with `.HasConversion<long>()`).
- `ChannelMilestone` — one row per (channel, milestone): `Milestone` (long), `MessageId`,
  `AuthorId`, `ReachedAt`, `Announced` (bool). Unique on (channel, milestone): a milestone is
  reached once, even if deletions take the count back under it and it is crossed again.

**Counting (`ChannelMilestoneService`, singleton + hosted loop, its own interval):**

- Channels: the home guild's text channels (not threads, not voice channels), minus the XP-excluded
  ones (`XpTracker.ExcludedChannels` + `/config`). Messages from bots count, hers included.
- Catch-up: per channel, one at a time, read forward from `Cursor` (or the channel's creation id
  when there is no row) in batches of 100, ascending; each message counted bumps `Count`,
  advances `Cursor`, and a milestone crossed is recorded **unannounced**. Runs at start and for any
  channel that appears later. Discord.Net handles the rate limits.
- While a channel catches up, its live messages are held aside; when a batch comes back empty the
  channel is marked caught up under the lock and the held messages newer than `Cursor` are counted,
  so nothing is lost or counted twice in the hand-over.
- Live: `BotService` feeds every message; a caught-up channel bumps `Count` and `Cursor` in memory.
  A milestone reached live is recorded (one write: rare) and announced.
- Deletions: a deleted message with id ≤ `Cursor` in a tracked channel decrements `Count` (never
  below 0). Deletions while she is offline are missed; accepted.
- Saving: every 30 minutes, the changed channels' `Count` and `Cursor`, plus on a clean stop.
  After a crash, the restart's catch-up re-reads from the saved `Cursor`, so the lost counts come
  back. Any deletions in that window are lost too; accepted.

**Announcing:**

- A reply to the milestone message, pinging only its author (`AllowedMentions` narrowed to that
  user). Pools: `MilestoneLines` (a person), `MilestoneBotLines` (another bot: rival jealousy),
  `MilestoneSelfLines` (her own message: smug). Each pool gets the number formatted the French
  way (« 10 000 ») and the channel.
- Not in silence mode (`SilenceService.IsSilent`), not in a hushed channel (`ChatHush`), not during a
  breakdown there. A skipped announcement stays recorded as unannounced.

**`/debug milestones`** (owner-only, ephemeral, current channel): the count (or « comptage en
cours » while catching up), each milestone reached (number, author as an inert mention, Discord
timestamp, jump link, « annoncé » or not), the next milestone and how many to go. `/debug` goes
from 20 to 21 subcommands.

## Left to the builder

- Exact names, the loop's tick (the catch-up pace and the 30-minute save can share the service but
  not another service's interval), the lines themselves (`docs/syncs-voice.md`), and the checks
  section for the pure parts (the milestone sequence, "which milestones does going from n to m
  cross", the French number format).
- Docs: `docs/agents/personality/ambient.md` (what she does unprompted), `architecture.md` (the
  new loop), `admin.md` (`/debug milestones`), CHANGELOG, a minor bump with the migration.
