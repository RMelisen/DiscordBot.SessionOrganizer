# Plynling badges

Part of `plynling.md` (*Badges and journal*): read that first.

`Helpers/Plynling/PlynlingBadges` is the catalog (8.25.0): **11 paliers** of five tiers (Bronze → Diamant,
`CollectionTier`, as the member badges) and **7 moments** earned once. Unlike member paliers, **every tier
is stored**, one `PlynlingBadge` row each, because each pays, journals and scores once. Each tier and moment
has a **stable key** — a rename orphans every earned copy. The 19 keys from before the paliers are tiers
now, under their old keys and rewards (`week` is Longévité Bronze, `games_100` Joueur Platine…; checks
section 68 pins them); a new tier pays 10/20/30/40/50 by rank. `PlynlingBadge` rows have a unique index on
(Plynling, key), which is what really makes each reward paid once.

- **`BadgeFacts`** is what a badge reads beyond the row: age, the action's `BadgeEvent`, the best body stat
  (needs traits) and whether it ever recovered (its journal). `BadgeFactsAsync` skips the two queries
  once their badges are earned. A new badge that needs more data adds a field there.
- **Where badges are checked**: every action that calls `AwardAsync` (meals, pets, games, visits, events,
  drills, a placed point, a flushed recovery) and the hourly sweep (`ProgressAsync`), which catches the rest
  (« Coquet » from the wardrobe) within the hour. On ship day the sweep awarded every tier existing
  Plynlings already qualified for, with its cailloux.
- **`Hosted`** (visits received, for « Hôte ») counts from 8.25.0; nothing earlier said which side a visit
  was on. `Visits` still counts both sides.
- **The journal** shows each reached palier at its highest tier with `value/next` (facts passed by the
  caller, living only), then the moments; the count is of every stored tier.
- **Every badge moves the score** (its reward, `PlynlingScore.ForMoment`): a badge added or a reward changed
  means re-running the calibration (`-- score-sim`) and retuning `MemorialScores` from it (*Score*).
- More tiers per Plynling make the `breeder` member palier (« Famille Plynling ») climb faster — accepted.
- Paliers have no art yet: the icon is the tier's medal before the palier's emoji (`BadgeInfo.Icon`).
