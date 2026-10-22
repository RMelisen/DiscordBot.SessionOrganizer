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
- **Every settled game feeds the `arcade` member badge** (`BadgeAwarder.Increment`, see `xp.md`): duel
  and heist in `ArcadeRunner`, the newer games in `PartyRunner` and `PartyComponentHandler`, and the
  Quokka click awards `quokka`. A new game adds its own call, after its settlement, for a human
  winner only (she never earns one), or its wins silently stop counting.

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

- The challenge pings the opponent only (`AllowedMentions` narrowed to them). Accepting pings the
  challenger back.
- **Every step reposts the card at the bottom of the channel** (`ArcadeRunner.RepostDuelAsync`:
  send the new card, then delete the old one) — accepted, refused, each move played, a tie, the
  result, an expiry. A duel lasts minutes and the chat keeps moving, so an edit in place got buried.
  `DuelGame.CardGate` serialises the reposts (two quick picks must not delete the newer card), and
  `DuelGame.Line` keeps her line so a repost says the same thing. The card shows who has played
  this round, never what. The heist and the newer games still edit in place. 10 minutes to
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

## The newer games — `PartyRunner`, `PartyCards`, `PartyComponentHandler`

Race, crash, blackjack, bomb, quote, temperature, Puissance 4, morpion and Quokka's visits share one
shape, so a new game should follow it:

- **State** is a class deriving from `ArcadeGame` (`Services/Arcade/ArcadeGames.cs`), registered with
  `ArcadeTables.TryOpen(game, conflict)` — the conflict says what can't run beside it (one race per
  guild, one blackjack hand per person, one board per person except against her). Read and change
  it only inside `ArcadeTables.With`.
- **Endings are claimed** with `ArcadeTables.End(id)`: the timer and the click that race to finish a
  game both call it, and only the one that gets `true` settles. Never settle without that claim.
- **Money** goes through `ArcadeService.SettleAsync`'s per-player-stake overload (a stake of 0 for a
  pure reward: quote, Quokka, a race owner's bonus). Net flows, `earn.arcade` / `spend.arcade`.
- **Timers and narration** run through `ArcadeRunner.Run` (fire-and-forget, catches everything);
  redraws through `ArcadeRunner.EditAsync` (swallows a failed edit); pings through
  `ArcadeRunner.PostAsync` (a new message, users only — an edit never notifies).
- **Cards** are static in `PartyCards`; every repeated button carries its value in its id
  (`race:bet:{id}:{runner}:{stake}`, `c4:drop:{id}:{col}`, `ttt:play:{id}:{cell}`, `quote:guess:{id}:{user}`).
- **Rules are pure** in `Helpers/Arcade` (`PartyRules`, `Blackjack`, `BoardGames`): odds, payouts and
  the two AIs are checkable without a gateway.

Game notes:

- **Race**: runners are living Plynlings (awake, not frozen, no death warning), strength = Courage +
  up to 4 for happiness; each step adds a d8 plus `Race.StrengthWeight` × strength, tuned so the
  favourite wins about half the time in a field of four. Bets close when the race starts
  (`RaceGame.Started`, set under the lock). Pari-mutuel minus 10 %; the winner's owner gets 15 even
  without a bet — never her (Ping-Qilin can run). **No Plynling state changes**.
- **Crash**: `P(crash ≥ m) = 0.9 / m`, capped at ×20, so every cash-out target returns ~0.9 on
  average. A cash-out uses the multiplier at the click's instant, not the last frame shown.
- **Blackjack** is fair (endless shoe, stands on all 17s, natural 3:2, double on two cards, auto-stand
  after 5 idle minutes).
- **Bomb**: only the holder's « Passer » works; the fuse is hidden (20–60 s). Fixed 20 stake.
- **Quote**: only channels `@everyone` can see, not NSFW, not voice text chats or threads, not
  excluded (`XpTracker.IsChannelExcludedAsync`); messages over a week old, 20–300 characters, no
  links, by someone still a member. Decoys come from the same stretch of history. The author can't
  play. Quoted inside an embed, so mentions in it are inert.
- **Temperature** refuses at the start when `PiHardware` can't read the CPU (on dev), cancels at the
  end when it can't or with fewer than two players.
- **Boards**: morpion against her is a full minimax (she never loses); Puissance 4 is a depth-4
  negamax, beatable. One Puissance 4 game in five against her, she drops two pieces once (after
  move 6) with a `BoardCheatLines` line that denies it — the owner's call, like the duel. Stakes
  against her move only the human's wallet. Five idle minutes on your turn loses.
- **Quokka** (`QuokkaRaidService`): 0–4 slots a day, one per distinct hour between 12:00 and 23:00
  Paris, computed from the date (`QuokkaRaid.SlotsFor`, nothing stored, restart-safe). A slot only
  fires if a human spoke in a counted home-guild channel in the last 30 minutes (BotService feeds the
  clock). Posts in the bots channel (`BotsChannel.ResolveAsync`). **Quokka is a name**: « Quokka »,
  never « le quokka ». `/debug quokka` spawns one now.
