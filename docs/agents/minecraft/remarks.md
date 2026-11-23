# Her remarks

Part of `docs/agents/minecraft.md`: read that first.

- One per kind every 2 minutes, and never two within 20 seconds. Two separate `CooldownGate`s,
  owned by the service.
- **And at most 4 in any 10 minutes** (`RemarkBudget`), the owner's call: she talks less than she
  did. A forced remark is neither held back nor counted. A remark the budget swallows gives both
  gates back, like one the gates swallow, so the callers that release their own gate still do.
- **The commonest remarks are rolled on top**, so the chat stays the players': night falls (25 %),
  advancements (30 %), an ordinary death (30 %; hers, a kill by a player and family's always get a
  word), a first hit from anyone but family (60 %, rare enough to keep its odds; the escalation is
  never rolled), a guard kill (10 %: kills come in waves) and a catch (15 %, junk 25 %, a treasure
  always). So are the answers whose gesture already says it: arriving where she was called (25 %)
  and the hop (50 %). Refusals and failures are never rolled: they're how a player knows she heard.
- **Order acks are said once every 3 minutes for the same order** from the same player (`Ack`,
  keyed by the order plus what makes it another one: the place, the coordinates, the resource, the
  crop). « suis-moi » sent again gets her following, not the same words. Refusals, outcomes, « je
  suis avec X » and « je te vois pas » are never held back.
- **A burst gets words once** (`_burstGate`, per kind, across players, 30 s): a hello, a goodbye, a
  « gg », a « merci » or a bare call from several players at once. The first player gets the line;
  the others only the gesture (the wave, the hop; a wave for a thank-you or a call, whose window
  still opens). **Papa and Tata always get their words** (`FirstOfBurst`, the owner's call), and
  still start the burst, so whoever follows them gets the gesture.
- **Lines come from small pools** (most have 2 to 9), heard by the same few players in one evening,
  so every pick in game is `ResponsePicker.Pick(pool, LineChoices)`: among the 2 lines said least
  recently, not among the half of the pool not said lately. A line comes back only once the pool has
  nearly gone round. A new send path uses it too.
- **Who hit her is known for sure** since 1.19.4: the server's `damage_event` names the cause (the
  shooter, for an arrow), which Mineflayer passes as `entityHurt`'s second argument. A mob, a fall
  or fire is no player, and she says nothing. On older versions there's no source, and the body
  falls back to guessing the closest player within reach (`exactHits`).
- **Repeated hits escalate** (`McHitStreaks`, pure, walked by the checks). The same player hitting
  her three times with no more than a minute between two hits is someone trying to kill her: that
  third hit gets `McHitAgainLines` (angry and scared at once) **at once, past the remark gates**
  (`Remark(…, force: true)`, which still starts both cooldowns). Further hits in the streak keep that
  pool, rationed like any remark. Streaks are per player and start over after a minute's pause, or
  when she leaves. Papa's twin is scared and hurt, never angry; Tata's is panicked bargaining.
- **And she runs** (`flee`): from the escalating hit on, the service sends `flee` with the attacker,
  **never for Papa**, the owner's call: she stays and takes it, scared. The body sprints away
  (`GoalInvert(GoalFollow(attacker, 20))`) until she's 20 blocks off, out of sight, or 15 seconds
  have passed (a new hit while running extends it), then reports `safe` (`McFleeSafeLines`, a
  rationed remark). Still no digging or building: with no way out she's `cornered`
  (`McCorneredLines`, said at once). An activity (`resumeActivity`) or a follow or guard is picked
  up again after; a one-off order is dropped and its player told why (`dropped` →
  `McFleeDroppedLines`), unless they're the attacker. She sprints only while her food bar is above
  6, so she runs better when she has something to eat.
- **She defends whoever a monster hurts near her, and Papa and Tata against players.** The same
  `damage_event` tells her who hurts whom, as long as they're in her sight. She goes when free, during
  an activity, following or guarding anyone, or busy with an order she can start again
  (`Restartable`: collect, harvest, come, goto, guide), and goes back to it after (`resumeFor`,
  `resumeAfterBreak`, shared with running away). Gathering and harvesting look around where she
  stands, so she first walks back to where she stopped (`back`, 20 s at most), then the same
  command runs again from the pockets as they were (`since`): what she cut before the fight still
  counts and is handed over, with no second acknowledgement. Only what's over in seconds or can't be
  left keeps her (a furnace, handing over, storing, a bed, a portal, running away). She never hits
  family, nor a creeper.
  - **A monster** hitting any player within 16 blocks of her: she fights it, for up to 20 s,
    silently. Body-only, like eating. A monster hitting *her* is *Fights*.
  - **A player** hitting one of them is the owner's three steps, by that player's streak
    (`McHitStreaks.Tally`, same one-minute window, `DefendStage`, walked by the checks): the first
    hit gets a warning (`McFamilyHitLines`, rationed), the second within the window she hits them
    back once (`McFamilySwatLines`, said at once), the third and after she fights them for 15 s
    after each hit (`McFamilyFightLines`, the start said at once). Busy with someone's order, the
    body answers `cantDefend` and it stays words. While she's fighting someone, their hits on her
    don't count as hits: no remark, no running away (`_defending`, 30 s). Below 3 hearts she breaks
    off and eats. PvP must be on for her hits to land.
- In-game lines are **plain text under 256 characters**: no custom emotes, no sticker tags (the
  checks enforce it).
- **Kaomoji in game must be drawable by Minecraft's font**, which covers the Basic Multilingual
  Plane through Unifont, one code point at a time. So: nothing past U+FFFF (most emoji, `👁`), no
  combining marks (`(•̀ᴗ•́)`, `( ͡° ͜ʖ ͡°)`: the accents land beside the face), no format characters,
  no `§` (colour codes), no Discord markup (`<:`, `<a:`, `<@`, `<#`, `<t:`, which would show raw;
  a plain `<` like « <3 » is fine), and no script that needs shaping
  (Malayalam's `ദ്ദി`). The checks reject the first four. Katakana, box drawing, `ω`, `ಠ`, `✧`,
  `✿` all draw fine. The in-game set (`syncs-voice.md`) is wider than Discord's, but used
  sparingly, the owner's call: **about one line in four** at most, none in most pools of two or
  three lines; dry lines and Papa hurting her stay bare.
- She posts nothing on Discord when she joins or leaves the server: it was noise, so keep it that way.

**Family in game.** `McFamily.Of` matches a player against `Minecraft:Owner` (Papa) and
`Minecraft:Tata` without case, and the service picks a twin pool (`…Owner`, `…Tata`) for the
lines aimed at one person: the « oui ? », their death, them hitting her, their advancement, and the
acks for come, follow, guard and a delivery. Twins call them « Papa » and « Tata », never `{0}`
(the checks enforce it). Same tiers as on Discord (`personality/people.md`, *Favouritism*): Papa is never
roasted and, when he hits her, she's hurt and forgives; Tata gets cheeky warmth and the mute
rights. Every other pool is shared, because it's about the game rather than the person. A new
person-aimed pool gets its twins too, and its name goes in the checks' `McFamilyPools`. Offline
mode lets anyone take those names, which is fine: names only pick lines here.
