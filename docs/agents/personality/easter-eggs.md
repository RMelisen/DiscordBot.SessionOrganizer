# Personality — easter eggs

Part of the personality subsystem: read `docs/agents/personality.md` (the index and the rules
that apply to all of it) first.

## Quoicoubeh easter egg

A sentence ending on a spelling of "quoi" gets the matching "Quoicoubeh", at
`ChatterService.QuoicoubehChance`.

1. `MessageCues.ReadQuoiBait` matches **only the last token** — mid-sentence would fire on every
   question.
2. The answer is **derived** from the spelling ("kwa" → "Kwacoubeh"), so `_quoiBait` is the whole
   edit surface. `coi` is absent ("rester coi").
3. The roll happens **before** the match: same odds, and it skips tokenizing most messages on the
   hottest path.

"c'est n'importe quoi" qualifies — that's the joke. Absent from `README.md` and `/help`.

## Tron / Trouille easter egg

A sentence ending on a word that sounds like "si" ("merci", "aussi", "ici", "celui-ci", "démocratie")
gets "Tron" or "Trouille" (`ChatterService.TronChance`, 8% — lower than quoicoubeh because these
endings are far commoner than "quoi"). Runs right after quoicoubeh in the same ambient slot; both
bait on the last word, so at most one fires.

1. `MessageCues.EndsOnSiSound` matches **suffixes of the last token** only (`_siEndings`), raw or
   squashed so "merciii" lands.
2. **`tie` is not a suffix on its own**: it sounds like "si" only after a/é/u/i ("-atie", "-étie",
   "-utie", "-itie"); "partie", "sortie", "garantie" say "ti". Same for `sie` ("Asie", "poésie" say
   "zi") — absent.
3. The line goes through `ResponsePicker.Pick` (two-line pool, so it alternates). Roll before match,
   as quoicoubeh. Absent from `README.md` and `/help`.

## Glitch easter egg

`GlitchService`: about one conversational line in 200 (`Helpers/Personality/Glitch.Chance`), at most once a
day, comes out corrupted (`Glitch.Corrupt`: look-alike or broken-byte letters, a stuttered word, a
word smeared with combining strokes, sometimes the end cut off) and is edited back to the real line
10–15 s later, **silently** — no comment, no cover-up: the denial is the joke.

1. **Opt-in, never on the send path as a whole.** Only `ChatterService`'s two send wrappers and
   `RivalryService`'s two lines go through it. Everything that must stay exact goes straight to
   `BotChat`: the morning hello and the 3 a.m. line are recognised by their text after a restart
   (a glitch left standing would make her say them twice), the giveaway draw carries pings, the
   quiz and the verdict replies are tracked elsewhere.
2. **Markup is never touched**: only words of three letters or more without `<`, `>`, `@` or a
   link are corrupted, and a cut-off ending cuts at a space, so an emote is dropped whole, never
   halved.
3. The roll comes before the daily claim, so a lost roll doesn't use up the day. Skipped during a
   breakdown. The restore runs off the gateway handler. Absent from `README.md` and `/help`.

