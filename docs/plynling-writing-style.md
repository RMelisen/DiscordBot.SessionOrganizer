# Plynling writing style

How Plynling lines are written: the visit scenes (`ProjectSYNCS/Stories/Visits/`), the storylines, and
SYNCS's own lines on the card (`BotResponses`). It was first taken from the approved rewrite of the old
visit conversations (`Cooking/Friendly/false`, removed with them in 8.2.0); their examples stay below
because the voice is the same. The file formats are in `docs/agents/plynling-storylines.md`, the village
in `docs/village-lore.md`.

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

## Their world

A village of small creatures, told through what they touch: Humeval, in **`docs/village-lore.md`**.
Its named villagers (`Stories/village.cast`) and places are welcome and make the world feel
lived-in. Money is **cailloux**; food is berries, nuts, acorns, honey, bread, jam.

Plynlings are all mushrooms, so a cap, a stem or gills may be named. Nothing about a Plynling's
family: they are written family-neutral.

The old visit conversations had figures of their own (the tortoise at the café, the owl librarian,
the heron on the old bridge…). They were retired with them in 8.2.0: never mention them.

## Visit scenes

A scene (`docs/agents/plynling-storylines.md`, *Visit files*) is an intro, the visitor's options and the
host's reactions, each with a good and a bad half. The ending prints the chosen option's half, then the
chosen reaction's, then what the visit changed.

- **The intro sets one concrete situation** with a hook a choice can act on: two or three sentences, a
  villager or a place from the lore, nothing that assumes how the two feel about each other beyond the
  scene's bonds.
- **Every option is a different kind of act** (bold, kind, clever, lazy…), and its leans say which: the
  host judges it by its own character. The same for the reactions, judged by the visitor's.
- **A host's half reads right after any of the visitor's options**: it is about the host's attitude, never
  the visitor's exact act. Read every option × reaction pair aloud.
- **A bad half is a small mishap with a smile**, never cruelty, and it must fit every bond the scene
  serves (between best friends as between rivals).
- **Halves are one or two sentences**; the outcome line (bond change, badges, happiness) follows them, so
  they don't conclude the relationship.
- `{A}` is the visitor and `{B}` the host in every line. A word agreeing with one of them goes through
  `{a:m|f}` / `{b:m|f}`; one agreeing with both takes `{p:m|f}` (feminine only for two girls), or is
  reworded away (« toi et moi », « ensemble »). Every line must read right for all four gender pairs.
- Face tags `[happy]`, `[content]`, `[sad]`, `[angry]` (that half's Plynling) or `[A:… B:…]` may open a
  half, where the emotion visibly turns, not everywhere.
- Story outcomes take the same face tags, on their first line ({A}'s face, or `[A:… B:…]`), only where the
  outcome's effects would give the wrong face (`docs/agents/plynling-storylines.md`, *What the player sees*).
- **The confession** (`confess.visit`): its options are ways of confessing, its halves `yes` / `no`. A
  `no` is gentle and leaves the two friends.

## Typed passions

A typed passion renders as the owner's text in guillemets — « les trains », « la pâtisserie japonaise »
— of unknown gender and number, starting lowercase. So: never after « de », « à », « du » or « au »
(after a colon, « sur », « pour », « avec », « dans » is fine); no pronoun that refers back to it (« ça »
and « c'est » are safe); and never at the start of a sentence, where its lowercase shows. That holds for
the card's typed twins and thought bubbles (`PlynlingPetTypedLines`, `PlynlingFeedTypedLines`,
`PlynlingThoughtTypedLines`, `PlynlingDreamTypedLines`).

## SYNCS's own lines on the Plynling card (`BotResponses`)

These are in *her* voice — bratty, kawaii, petty — and players see them after every pet, meal,
game and visit, so structural sameness shows fast. A pool used to be one shape repeated: the
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

1. Read each scene aloud, every option followed by every reaction: does each pair land?
2. Does each option, and each reaction, bring a different kind of act?
3. Is any trick used twice in the batch?
4. Does every bad half still fit every bond the scene serves?
5. Run the checks (`dotnet run --project tests/ProjectSYNCS.Checks`): templates for all four gender pairs,
   Discord's caps, and the coverage of every bond, stage and hour.
