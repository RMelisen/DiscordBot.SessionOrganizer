# `/work` DM reminder — design

## Intent
Members forget `/work` once its 4 h cooldown ends. Let each member opt in to one DM from SYNCS when
their next shift is ready, and opt out again.

## Goals / non-goals
- Per-member toggle, persisted (survives restarts), off by default.
- Exactly one DM per finished cooldown, whether the shift was started by `/work` or `;work`.
- Non-goals: reminders for `/plynling forage`, a channel fallback, a configurable delay.
- Anti-goals: a DM storm after a restart, a DM for a cooldown that ended before opting in, a ping
  in a channel.

## Approaches
- **Toggle placement** (picked: button on `/balance`). `/balance` is already the private screen
  showing « Prochain `/work` », so the clicker is always the owner and no new command is needed.
  Rejected: an option on `/work` (mixes two actions), a button under every public `/work` reply
  (clutter, stale buttons), a new top-level command (a whole command for one switch).
- **Scheduling** (my call: own loop on wallet columns). Rejected: inserting `/remindme` rows — they
  would count toward its 15 cap, show in `/reminders`, fall back to a channel ping, and need
  deleting on opt-out.

## Design
- **Model**: `PebbleWallet` gains `bool WorkReminder` and `DateTimeOffset? WorkRemindedAt`.
  Migration `AddWorkReminder` (schema only).
- **Due rule** (pure, in `PebbleEconomy`): due when `WorkReminder` and `LastWorkAt` is set and
  `LastWorkAt + 4h <= now` and (`WorkRemindedAt` is null or `WorkRemindedAt < LastWorkAt`).
- **Toggle**: `/balance` gets one button, `bal:remind:{on|off}` (« 🔔 Me prévenir en MP » /
  « 🔕 Ne plus me prévenir »). The handler (in `Interactions/Components/`) defers, flips the flag,
  and redraws the card with a one-line notice. Switching on while `/work` is already available
  stamps `WorkRemindedAt = now`, so a cooldown that already ended never sends a DM; switching on
  mid-cooldown leaves it alone, so this shift's DM still goes out. The card builder becomes `static` and is shared by
  the command and the handler.
- **Hint**: when the reminder is off, the paid `/work` / `;work` footer ends with
  « · 🔔 rappel en MP : `/balance` ». When it is on, the footer has no hint.
- **Delivery**: `WorkReminderService`, its own `BackgroundService` with a 2-minute interval (one
  interval per loop; a 2-minute delay doesn't matter for a 4-hour cooldown). Each pass loads the
  wallets with `WorkReminder == true` (filtered in SQL), applies the due rule in memory, and
  for each due wallet (per-item `try`): claims it first by saving `WorkRemindedAt = now`, so a crash
  costs the DM and never sends a second; then sends a line picked with `ResponsePicker` from a new
  pool in her voice, with `AllowedMentions.None`. `CannotSendMessageToUser` is swallowed and
  logged at debug. The loop skips a pass while the gateway is not connected. Silence mode does
  not apply, because the member asked for this notice.
- **Docs**: `/help` and `README.md` mention the button; `economy.md` gets the rule;
  `architecture.md` lists the loop. Minor version bump + `CHANGELOG.md` entry.

## Left to the builder
The exact French lines, and where the claim save lives (`PebbleService` gets the methods).
