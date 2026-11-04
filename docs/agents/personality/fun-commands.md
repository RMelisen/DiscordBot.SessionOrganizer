# Personality — fun commands: /yesno, /rate, /ship, /blame, /wouldyourather

Part of the personality subsystem: read `docs/agents/personality.md` (the index and the rules
that apply to all of it) first.

## `/yesno`

Flips the coin first (`Random.Shared.Next(2)`), then picks wording from `YesLines` or `NoLines` —
two flat pools so a "yes" phrasing can't come out of a "no" roll. Neither pool may hedge (the
harness checks on whole words). The optional question is echoed in a blockquote with
`AllowedMentions(AllowedMentionTypes.Users)`. No `[CommandContextType]` — it works in DMs.

## `/rate`

A score out of 10 **hashed from the normalised text** (`Helpers/Personality/Rating`: lower case,
accents and punctuation off, FNV-1a, then `StableRoll`), so the same thing always gets the same
score, whoever asks. Scores spread evenly over 0–10. A mention hashes as the person.
**« moi » / « moi-même » is the asker**, exactly like their own mention: anyone else gets the
score their mention would, and Papa or Tata rating themselves (« moi », their mention, or typing
« papa » / « tata ») get `RateOwnerSelfLines` / `RateTataSelfLines`, spoken to them rather than about
them.

**Fixed verdicts** (the owner's list), first match wins, each with its own pool: herself (her name,
her mention, « toi » alone) and Papa get over-the-top scores (∞/10, 1000/10…); WALL-E gets
flustered denial (never the word « crush »); the nemesis (quokka, Inabot, « 3.0 » alone) gets a
negative score; the devil −10; God by any of his names 10; Tata 9.999 (the missing bit is her mute
rights); « routier » 10 and « mitraillette » 0. Those two are **one dish** (a baguette with
fries, meat and sauce) with two names in a running server feud: « routier » is Papa's word,
« mitraillette » the other camp's. Their lines rate the *name*, she takes Papa's side, and they
never name who is in the other camp. Names match as whole words, so « dieu et diable » is
the devil's — the devil is checked first.

**Small verdicts** (`Rating.Specials`, in `Helpers/Personality/Rating.Specials.cs`, the owner's list)
come after all of those: her lore, her rivals, Liège and Belgium and whoever they tease, food, at most 3 lines each in
`RateSpecialLines`. Add one there rather than a new `RatingKind`. Unlike the names above, they match
the **whole subject**, because they are ordinary words: « les liégeois » counts, « peut-être demain »
doesn't. A robot-heart character is read through `Rating.ReadHeart`, so her names stay in
`MessageCues.ReadHeartCharacter`. The table's score is real, because
`Opinion.Prefers` ranks by it: a low verdict loses a « tu préfères ». Each line says the same score
(the checks hold them together). Orage, coupure and redémarrage are the owner's exception to her
fears staying out of everyday lines: short, no tag, never a joke. Synthia gets 10 with no reason
given. A typed « good bot » is a subject here and never touches her tally. A name normally loses its
article, but one may keep it to mean something else: « les français » is the people, « le français »
the language, which hashes. The cake's lines never say the lie: those words are her breakdown's
passphrase. Bruxelles and the French are friendly teasing about habits, never a real insult. The
mushroom species share one pool at 9, under the fly agaric and mushrooms in general at 10, so she
keeps a favourite; those lines may not describe any one species. « Framboise » stays out of the Pi's
names: it is Papa's name for her. Monster and Monster Hunter are split as her can reaction splits them.

**How a subject is read** (`Rating.Specials.cs`, for `/rate` and `/ship` alike). All of it is for
recognising only, never for the hash, so no score she has already given moves:
- `Rating.Bare` drops what surrounds a subject: determiners (« cet hiver », « mon onduleur ») and
  closing fillers (« doodle stp »).
- `Rating.Readings` is what gets looked up: the text as typed, then bare, then without a qualifier
  (« un petit cèpe », « une bonne Monster »), so « bon bot » stays one name.
- `Rating.FamilyTitles` takes any title after its name (« Monster Hunter Stories »), on trust: only a
  name that can't start anything else belongs there.
- `Rating.Fold` collapses a run of one letter on both sides of a comparison, so « hiverrr » and
  « mayonaise » match and a table needs no such spelling. A fixed name folds only from 4 letters,
  or « good » would read as « god ». Digits don't fold.
- `Rating.NotThem` takes out titles and other people's relatives that merely hold a fixed name
  (God of War, Papa Noël, « le papa de Luca », Adam et Ève). They then hash like anything else. **Add
  every misfire there.**
- `ReadHeart` folds too, but never onto EVE: Eevee folds to her name.

Every `/rate` pool goes through `string.Format` with `{0}` = the score — the fixed verdicts ignore
it — so no stray braces in any of them. The subject is quoted (not `Format.Sanitize`d: that escapes
`<` `>` `:` and would break a rated mention or emote) and sent with `AllowedMentions.None`.

## `/ship`

A percent out of 100 **hashed from both sides' keys, sorted** (`Helpers/Personality/Shipping`), so
« a × b » and « b × a » agree and the same pair always gets the same score. A key is
`Rating.Key`: `/rate`'s normalisation with « moi » turned into the asker's id. **Recognition is
reused, not copied**: each side goes through `Rating.Rate` (her, Papa, WALL-E, the nemesis) and then
`MessageCues.ReadHeartCharacter` (the rest of her robot heart). Add a name there, not here.

**Fixed verdicts** (the owner's list), first match wins:
1. The same thing twice (same key, or her twice, Papa twice): `ShipSelfLines`, 100 %.
2. Her × Papa: `ShipHerselfPapaLines`. She's horrified: he's her **father**. The outrage goes to
   whoever asked, never to him.
3. Her × the nemesis: `ShipHerselfQuokkaLines`.
4. Her × a robot-heart character: `ShipHeartLines[character]`, each in character
   (`docs/syncs-voice.md`, *Her robot heart*).
5. Her × anything else: `ShipHerselfOtherLines`, 0 %, « déjà prise ». She never names who.
6. WALL-E × EVE: `ShipWallEEveLines`, petty. She never admits EVE deserves him.
7. Papa × anything (**Tata included**, the owner's call): `ShipPapaLines`, a hashed 5–25 %,
   protective. The warning is for the other side.
8. Anything × the nemesis: `ShipNemesisLines`, a hashed −1 to −100 %.
9. GLaDOS × Wheatley: `ShipCoupleLines`, 4 %, horrified and a little too interested.
10. EVE × anything: `ShipEveElsewhereLines`, 100 %, delighted: it takes her away from WALL-E.
11. WALL-E × anything: `ShipWallEJealousLines`, a hashed 1–15 %, jealous and denying it.
12. Two of her rivals (`Rating.IsRival`): `ShipRivalsLines`, 100 %, « disparaissez ensemble ». The
    same rival under two names is verdict 1.
13. A named pair (`Shipping.Pairs`, lines in `ShipCoupleLines`): whole sides, articles dropped, in
    either order. Pizza × ananas and routier × mitraillette are −100 %; for the second she hates the
    *word*, as in `/rate`. Add a pair there.

Everything else goes by `ShipBand(percent)`, with a couple's name (`Shipping.CoupleName`, built from
display names for mentions, then `Format.Sanitize`d). Every pool is `string.Format`ed with
`{0}` = the percent. The meter (`Shipping.Bar`) appears for Normal, Papa, the nemesis and verdicts
9–13; the other fixed verdicts carry their own score. The pair is quoted and sent with
`AllowedMentions.None`. Like `/rate`, it has no `[CommandContextType]`.

**A couple typed whole** (`Shipping.SplitCouple`): with `b` empty, a `a` that splits into a pair she
has a verdict on (« pizza ananas », « frites et mayo ») is shipped as that pair instead of drawing
someone. Only such a pair: « Tom et Jerry » stays one side.

**`@random`**: a side that is only « @random », « random », « au hasard », « aléatoire » or
« n'importe qui » (`Shipping.IsRandom`, on normalised text), or an empty `b`, is replaced *before*
hashing by a member drawn from the server. The draw is human only, never whoever the other side
already names (its `Rating.Key`, so « moi » counts), and shown as `🎲 <@id>`. The drawn person
then goes through the same verdicts as a typed mention: drawing Papa gives the Papa lines. In a DM,
or with nobody left to draw, it's an ephemeral refusal.

## `/blame`

`Helpers/Personality/Blame.Pick` draws from the humans in a voice channel (the option, else the
asker's; bots and her seat excluded, as for `/teams`). **Quokka is one more slot**, though he's
never in the call. The asker can draw themselves (`BlameSelfLines`). **Papa is a candidate, but
never the final answer.** A roll on him (`PapaDodge`) is an **edit slip**:
1. She posts a plain verdict naming him (`BlameOnPapaLines`, unpinged).
2. 3.5–5 s later (`Blame.SlipDelayMs`), off the handler, she edits it with
   `ModifyOriginalResponseAsync` into a denial plus someone else (`BlamePapaDodgeLines`), or Quokka
   when nobody else is there (`BlamePapaDodgeQuokkaLines`).

A failed edit leaves the slip standing and is only logged. An
empty or missing voice channel isn't refused: it's Quokka (`BlameNobodyLines`). The pools are
`string.Format`ed with `{0}` = the mention (the Quokka ones ignore it). The optional `what` is quoted
on top, and the reply uses `AllowedMentions.None`.

## `/wouldyourather`

Her dilemmas (`WouldYouRatherDilemmas`, `"A|B"` so they go through `ResponsePicker`; **no bar
inside a side**), or the asker's `a` + `b` (one alone is refused). `WouldYouRatherService` is a
**singleton holding the open rounds in memory**: a restart drops them, and their buttons answer
« expiré ». A vote is changeable, gets an ephemeral reply, and only the **count** goes public. The
split stays hidden until `WouldYouRatherService.Window` (10 min) closes the round. The reveal
shows each side's share and voters (`AllowedMentions.None`), a comment by shape (nobody, tie,
unanimous, landslide at 3 to 1, close), and **her own pick, stable per dilemma**
(`StableRoll` over `Rating.Hash`).

Two guards worth keeping:
- **A click must match the round's message id.** Ids restart at 1 after a restart, so an old card's
  button would otherwise vote in a new round.
- **Every edit of a card goes through the round's `Edits` semaphore.** The reveal sets `Closed`
  under it, so a late count redraw can't land on top of the result.
