# Her orders and her free time on the Minecraft server

The rest of her Minecraft life: `minecraft.md` (the code, the two processes, joining and leaving, talk,
`/minecraft`, failures, her remarks, deployment) and `minecraft-body.md` (water, fights, body language,
arriving, testing movement locally).
This file is the index and holds the rules that bind every order. The rest is split by topic under
`docs/agents/minecraft-orders/`: read this file, then **only the topic file you are changing**. A
reference elsewhere to « minecraft-orders.md, *Some section* » means the section of that name in
the file below.

| Topic | Sections | File |
|---|---|---|
| How chat becomes an order (`McCommand`) | refusals, phrase lists, `Clean`, the six-letter slip, *Talk about oneself is not an order*, counts, coordinates, *An order can come in two steps*, praise and thanks | `minecraft-orders/reading.md` |
| Moving and gathering | *Come*, *Follow*, *Stay*, *Go to*, *Guard*, *Collect*, wood, stone, columns, fields, picking up, `goto`, sleep and bedtime | `minecraft-orders/moving-and-gathering.md` |
| Activities, care and answers | *Harvest*, *Farm*, *Ranch*, food and armour, healing, foraging, creepers, *Patrol*, errands, *Give*, *Fetch*, *Craft*, *Fishing*, *Smelt*, remembered places, inventory, instant answers, « tu fais quoi ? » | `minecraft-orders/activities.md` |
| Getting around | *She never digs and never places a block*, bridges, the camp, doors, ladders, trapdoors, fields, the physics tick, render distance, portals | `minecraft-orders/getting-around.md` |
| Her own life between orders | own tasks, `autonomyTick` | `minecraft-orders/free-time.md` |

A rule you add goes in the topic file it belongs to; only what binds every order goes here.

## Every order

- **An order that fails at once changes nothing.** The body checks an order can be done (in sight,
  not too far, something to gather, a bed) *before* `stay()` drops the current one, so
  « syncs va chercher des diamants » with none around never pulls her off someone's follow. Keep
  that order in any new op.
- **Orders are acknowledged when the body reports them under way** (`moving`, `collecting`,
  `sleeping`), not when they're read. « J'arrive » followed by « je te vois pas » reads as a broken
  promise. Stay is acknowledged straight away.
- **Whispered orders get whispered answers**, outcome included. The service remembers who gave the
  current order and how. In every line Papa and Tata are « Papa » and « Tata » (`Who`), never their
  in-game names.
- **She works for one player at a time** (`McClaim`, pure, walked by the checks). Whoever gave the
  order under way holds her: only they can stop her or send her elsewhere, and anyone else gets
  `McBusyLines` (once every 10 seconds per player). Family outranks: Papa > Tata > everyone, so
  family can always take her and nobody takes her from them; whoever she leaves mid-order hears why
  (`McTakenLines`). A claim runs out **10 minutes** after its order got under way, Papa's included,
  so a forgotten « suis-moi » never locks her for the evening; family can still take her back.
  The service keeps the order under way (`_order`) apart from the one just sent (`_pending`): the
  pending one only becomes hers when the body confirms it, so a failed order leaves the claim where
  it was. Going to bed (asked, or because a player did) ends any claim.
- **Orders aren't rationed like remarks.** One gate per player ignores a second order within the
  same second (a double send), never a stop, so « viens » then « stop » always works. Another gate
  gives each player her « oui ? » once every 10 seconds at most (the window still opens). Only their
  acks are held back, when the same order comes again (*Her remarks*).

