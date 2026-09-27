# Admin: cailloux and stats — design

Two staff tools under `/admin`, beside `xp` and `plynling`.

## `/admin pebble add|remove user: amount:`

- Staff only (`SessionPermissions.IsStaff`, checked in the handler, no `[DefaultMemberPermissions]`
  — the `/admin` rule), ephemeral, bots refused, `amount` 1–1 000 000.
- `remove` clamps the balance at 0.
- One save through `PebbleService.GetOrCreateWalletAsync`; the reply names the new balance
  (« 500 cailloux ajoutés à @Luca. Solde : 1 340. ») with `AllowedMentions.None`.
- The person is not notified, like `/admin xp`.

## `/admin stats`

A private snapshot of this server's economy, from what is stored — flows over time (cailloux
spent per day) are not recorded and are out of scope.

- **Cailloux:** total in circulation, wallets, average per wallet, the 5 richest.
- **Plynlings:** alive, frozen, graves, and the living ones by species. Rows are settled in
  memory (`PlynlingLife.Settle`) without saving, so a death nobody has noticed yet counts.
- **Collections:** completions per set, collectibles discovered in total (inventory rows).
- **Cosmetics:** held in total, worn right now (living Plynlings, all slots), the 3 held by the
  most people.

`AdminStatsService.GetAsync(guildId, now)` returns an `EconomyStats` record; `AdminCards.BuildStats`
renders it (static, measurable). Staff only, ephemeral.

## Checks

Adjusting adds, removes, clamps at 0 and saves once; the stats aggregate correctly on an
in-memory database (including a settled death and worn cosmetics); the embed fits Discord's caps;
`/admin` holds `xp`, `plynling`, `pebble` and `stats`.
