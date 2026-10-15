# Arcade — `/arcade` games for cailloux

`ArcadeModule` (`[Group("arcade")]`, guild-only) holds the commands; `ArcadeComponentHandler` the
buttons; `ArcadeCards` the static card builders. Pure rules live in `Helpers/Arcade/ArcadeRules`
(`Duel`, `Heist`).

## Where things live

- **`ArcadeTables`** (singleton) is the only state: the games running now, in memory, behind one
  lock it owns. A restart drops them; their buttons then answer « cette partie n'existe plus ».
  Checks and changes that must be one step (a pick and « have both picked? », a join and the crew
  cap) run inside `WithDuel` / `WithHeist`.
- **`ArcadeRunner`** (singleton) runs a game past the click that started it: timers
  (fire-and-forget tasks that catch everything), card redraws, settlement. It opens a scope around
  each settlement, like every singleton.
- **`ArcadeService`** (transient) is the money.

## Money: settle once, at the end

**Nothing is held in escrow.** A balance is checked when someone joins (so nobody joins with
nothing), and **`ArcadeService.SettleAsync`** re-reads every wallet at the end, drops whoever can no
longer cover the stake, applies every move and logs it, in one `SaveChanges`. So a restart mid-game
moves no cailloux, and there's nothing to refund. **Every arcade game settles through it** — don't
write a second path.

The log records **net** flows: `earn.arcade` sums the positive nets, `spend.arcade` the negative
ones, so a duel winner doesn't show as spending and earning their own stake. Both keys are stored —
append-only — and shared by every game, so the dashboard gains one row each, not one per game.

## Duel

- The challenge pings the opponent only (`AllowedMentions` narrowed to them). 10 minutes to
  accept, then 5 to pick; `ArcadeRunner.WatchDuel` checks every 30 s and calls it off (nobody pays).
- Moves are picked from an **ephemeral** row; the round rides in the custom-id
  (`duel:rps:{id}:{round}:{move}`) so a row from a tied round can't count twice.
- A tie replays up to `Duel.MaxRounds` rounds, then it's a draw. The winner gets the stake minus
  `Duel.Commission` (rounded up), which vanishes: a small sink.
- **Against her**: no accept step, and **she always wins** — `Duel.CounterTo` your move, picked
  after yours. **Her lines never admit it** (`DuelHerWinLines`: innocent, smug, « quelle
  coïncidence »). That's the owner's call; don't make her confess or lose. Many people can duel her
  at once (she isn't counted as « already in a duel »).
- **Challenging the owner** adds `DuelOwnerWarningLines` to the card: she teases the challenger that
  she could cheat for Papa. **Talk only — that game is fair.**
- Other bots and yourself are refused.

## Heist

- One per guild at a time, then a **6-hour** cooldown (`Heist.Cooldown`, in memory: a restart lifts
  it early, accepted). The lobby lasts **25 minutes** or until the organizer's « Lancer le casse »;
  `TryLaunchHeist` lets exactly one of the timer and the button run it.
- **Expected value is constant**: success chance `min(0.75, 0.20 + 0.10·n)`, payout
  `0.9 × stake / chance`, so every crew size returns 90 % of the stake on average — size changes
  the variance, never the value. Keep that property if the numbers change.
- Three narration beats edited onto the card (`BeatPause` apart — well clear of the edit rate
  limit), then the result, and a **reply pinging the crew** (they may have waited 25 minutes). On a
  failure she blames one random payer.
