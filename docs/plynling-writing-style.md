# Plynling writing style

How Plynling lines are written — the visit conversations in `Helpers/PlynlingScripts.cs` first, and
the same voice for the other Plynling pools (arrivals, activities, closers, `BotResponses`). Taken
from the approved rewrite of `Cooking/Friendly/false`; the engine rules themselves are in
`CLAUDE.md` ("The conversation is one script", "Two lines to a step").

## The voice

Each script is a **tiny scene with a turn**, not a chat. A hook in the opener, something that
moves — a reveal, a misunderstanding, an absurd escalation, a reversal, a quiet beat — and a line
that lands. If nothing changes between the first line and the last, it is filler.

Cozy, dry and specific. The humour comes from character and concrete detail, never from stacked
puns. Tender is allowed, and so is a little melancholy; cruelty and vulgarity are not.

## What to avoid

The pattern the old scripts fell into, in any of its parts:

- A states a fact → B **echoes it as a question** (« Trois jours ? », « Tout ? », « Vraiment ? »)
  → A explains → B asks to be taught or to help (« Apprends-moi », « Je peux t'aider ? »).
- **B as a question machine.** B needs a point of view: teases, disagrees, misreads, tops the
  story, turns out to be the expert, has a secret of their own.
- **Generic claims** (« J'ai un don pour ça ») where one concrete detail would do the work.
- **Two scripts in a key using the same trick.** Three literal-idiom misunderstandings in one key
  is two too many.
- Pop-culture references, real brands, anything that needs outside knowledge to land.

## Tools that work

| Trick | Example from the approved key |
|---|---|
| Invented ritual or rule of their world | « Remue toujours dans le même sens. » « La sauce se souvient de choses tristes. Et elle tourne. » |
| Deadpan reality check | « Il faut juste que ça sente dimanche. » — « On est mardi. » |
| Literal misreading | « …au bain-marie. » — « Et Marie est d'accord ? » |
| Absurd escalation through B | the soufflé that falls at any noise, and B who needs to sneeze |
| Mock-epic memory | the crêpe that landed on the heron, who wears it like a beret |
| Quiet sensory beat | the bread that crackles « comme la glace au printemps » |
| B topping or turning the tables | « Mets-la au frais et ne la regarde plus. C'est ce qu'on fait avec moi quand je boude. » |
| Subtext | a hot-chocolate blend named after B, with chilli « pour le caractère » |

Show, don't state: an object, a texture, a smell, a sound. The narration line sets the scene in
one gesture (« {S} pose une tartelette devant {L} et la fixe sans cligner des yeux. »).

## Length

Mix within each key: about a third **2 lines** (the opener and one answer that lands on its own —
a punchline or a quiet beat), half **4 lines**, the rest **6 or 8**. A long script earns its length
with a build-up (the stone soup), never with padding. Keep every line short: one or two sentences,
narration under ~20 words.

## The two axes of a key

**Flavour.** `Friendly` covers every bond from acquaintances to lovers, so nothing may assume a
shared past or intimacy. `Tense` covers rivals *and* enemies: real friction — one-upmanship,
bruised pride, passive-aggressive politeness, grudging respect, a well-aimed comeback — petty and
funny, never cruel.

**Shared.** When B shares the passion: insider knowledge, techniques, arguments only fans would
have, traditions they both know. When B does not: curiosity, culture clash, charming
misreadings — and B's own world poking through.

## Their world

A village of small creatures, told through what they touch. Recurring figures are welcome and
make the world feel lived-in: the old tortoise who serves at the café, the owl librarian who says
« chut » with one eye, the heron on the old bridge who pretends to see nothing, the hedgehog
station master, the old bear at the ice-cream stand, the sparrow who judges contests, the snail
who is the only customer. Money is **cailloux**; food is berries, nuts, acorns, honey, bread, jam.

Never name a cap, petals or spores, and nothing about a Plynling's family: they are written
family-neutral.

## Mechanics (enforced by the harness)

- Line 0 is A's: narration, `\n`, then A's words. Line 1 is B's. Every other line is one spoken line.
- An **even** count: 2, 4, 6 or 8. A generic closer follows the last line, said by whoever did not
  say it, so the last line may land as a punchline — the closer turns it into doing something.
- Two lines in a row from the same speaker share one bubble — use that for a beat (the soufflé's
  « Je respirerai à peine. Je ne voudrais pas t'inquiéter, mais j'ai très envie d'éternuer. »).
- `{S}` is A and `{L}` is B, in every line of the script. Any word agreeing with one of them goes
  through `{s:m|f}` / `{l:m|f}`, and every line must read right for all four gender pairs. A word
  agreeing with *both* (« l'un de l'autre », « tous les deux ») takes `{p:m|f}`, feminine only for
  two girls — or is reworded away (« toi et moi », « ensemble »).
- `{P}` only in the custom scripts. It renders as the typed text in guillemets — « les trains »,
  « la pâtisserie japonaise » — of unknown gender and number, starting lowercase. So: never after
  « de », « à », « du » or « au » (after a colon, « sur », « pour », « avec », « dans » is fine); no
  pronoun that refers back to it (« ça » and « c'est » are safe); and never at the start of a
  sentence, where its lowercase shows — « ma passion : {P}. », not « ma passion. {P}. ». With no
  subject to be funny about, a custom script lives entirely on the two characters.
- Openers are unique within a key.
- Face tags: `[happy]`, `[content]`, `[sad]`, `[angry]` — on a spoken line, the speaker's face; on
  narration, both. Use them where the emotion visibly turns, not everywhere.

## SYNCS's own lines on the Plynling card (`BotResponses`)

These are in *her* voice — bratty, kawaii, petty — and players see them after every pet, meal,
game and knock, so structural sameness shows fast. A pool used to be one shape repeated: the
Plynling does something cute, she turns it back onto herself, a kaomoji closes it. Keep each pool
to roughly:

- **Openings:** at most a third start with `**{0}**`. Others open on « Tu… », on her reaction, on
  a quoted line, or on a sound (« Crunch, crunch. », « Toc, toc-toc, toc. »).
- **Her reactions:** in about half the lines, not all — and in many modes: jealous, bossy, fake
  conspirator, openly competing with the Plynling, secretly soft, dramatic, petty score-keeping,
  fake indifference. Each mode only a few times per pool.
- **Kaomoji:** on about 60 % of lines, sometimes mid-line. A line that lands on its own words ends
  there.
- **A running joke lives in one pool, once.** « Oui, les Plynlings ronronnent, ne pose pas de
  questions » was in three pools; « je l'ai enregistré » in two.
- **Never gender the player.** Their gender is unknown: « tu sais nourrir un Plynling », not « tu
  es un très bon nourricier ». Only the Plynling's words agree, through the M/F halves.
- **`{1}` in the feed pool carries its article** (« une truffe »): never start a line with it (it
  would start lowercase) and never put « de », « à » or « que » in front of it.

## Checklist before a batch ships

1. Read each script aloud: does the last line land?
2. Does B have a voice in every script?
3. Is any trick used twice in the key?
4. Does the key hit the length mix?
5. Run the scratch harness over the key (every script built as a full story, all four gender pairs).
