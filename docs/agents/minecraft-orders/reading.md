# Minecraft orders — how an order is read

Part of her Minecraft orders: read `docs/agents/minecraft-orders.md` (the index, and the rules
that bind every order) first.

## Reading an order

A player gives her an order in chat (`McCommand`, pure, with cases in the checks). It needs her
name in the message (`McCommand.HerNames` plus her in-game name: « sync » counts), because the
chat is shared. Stay is read before refusals, because « bouge pas » means stay; « reste avec moi »
is read before stay, because it means follow.

**Refusals are read next to the verb only**: « ne » up to two words before, « pas » just before or
up to two words after, « jamais » after, « plus » after unless it means more (« plus de bois »).
A « pas » further away belongs to another sentence (« c'est pas grave, syncs viens »), and « ou pas »
is impatience (« tu viens ou pas ? »). **Something she keeps doing refused with « plus »** (follow,
guard, patrol, farm, ranch, fishing: « ne me suis plus », « ne pêche plus ») is a stop, so Stay;
not with « tu » just before, a complaint (« tu me suis plus ? »). Stay itself can be refused
(« reste pas plantée là » is nothing) unless the phrase says no itself (« bouge pas ») or it stops
a follow (« reste pas avec moi »); « arrête de dire ça » is about her talk, not her task. A
message with « demain », « hier » or « pourquoi » is never an order (« tu peux venir demain ? »):
it goes to talk.

**The phrase lists are written the way people type in game chat**: familiar French (« ramène ta
fraise », « fais du bois », « il me faut »), shortcuts (« vien », « vi1 », « diams »), a little
English, and the infinitives of questions (« tu peux venir ? »). Every phrase is whole words,
longest first. A word that is common outside orders stays out (« assez », « help » alone, « à
moi »): a missed order costs a retype, a false one makes her walk off. Add the misses found in the
wild, with a case in the checks.

Before matching, `Clean` squeezes letters held down for emphasis (three or more become one:
« viiiiens »), maps game-chat spellings (`Slang`: « mwa », « twa », « chui » → « je suis », so
« chui là » is never « suis » = follow) and drops politeness. Words of six letters or more match
with one slip (`Close`: a letter added, dropped, changed or two swapped), and so does her name;
shorter words must be exact, because at four or five letters one slip makes another word
(« viens » / « liens »).

**The slip cuts both ways at six letters too**: « ranger » / « manger », « ranges » / « manges »,
« préviens » / « reviens », « pêches » / « poches ». Such a word stays out of a list alone and only
comes in a longer phrase (« ranger tes affaires », « préviens-moi »). When the list word must stay,
the common word goes in `NoSlip` instead, and is then only ever itself: « reviens » read as
« retiens » saved a place instead of coming, « jardin » farmed, « entre » walked her home,
« coucher » sat her down. Check any new six-letter word against the common words one slip away.

**Talk about oneself is not an order.** A phrase right after « je » doesn't count (« je range ma
chambre », « ça fait 5 min que j'attends »), unless it starts with it (« j'ai faim »), nor one after
« je » and a modal, two words back at most (`OwnPlan`: « je vais chercher du bois », « je dois aller
chercher »; further back the lost comma may have ended the sentence: « je m'en vais, suis-moi »);
nor one right after « il » or « elle » (« il reste du blé »), or « en train de »; nor one after an
auxiliary, maybe past an adverb, the past tense (« je t'ai donné ma pioche », « j'ai déjà vidé mes
poches », « t'as bien dormi »), or after « a » when the word as typed is a participle (« on a
trouvé du fer », « Bob a coupé du bois »: « à » comes before an infinitive, « aide-moi à couper »,
and « y a » is « il y a »). A few one-word verbs are nouns or participles after certain words (`NounAfter`: « la
pêche », « j'ai pêché », « c'est cuit », « au fond », « la base est protégée ? »), looked up by the
word as typed, since the slip lets « pêché » match « pêcher ».

**Counts** come from a number anywhere (« 20 », « x20 », « 20x »), else from a quantity in words
right before the resource (« dix bûches », « une stack de pierre », « une dizaine de bûches »),
else 10. Only right before: « du bois pour une maison » isn't one log. A number before « stack(s) »
is 64 each (« 2 stacks » was 2 logs). **A resource with no verb** is gathered when every other word
is filler and there's a partitive, a quantity or a number (`BareAsk`: « syncs du bois », « syncs 20
bois », « il faut de la pierre », « je veux du fer »); « syncs un arbre » points at one, and « syncs
bois » alone is « drink ». A resource between a going verb and a known place is the errand, not the
place (« va chercher du bois à la maison » is wood). **Coordinates** accept
F3's decimals, floored to the block (-300.7 is block -301), and labels or brackets around them.

A few readings sit between orders. **Come or stay with a known place right after**, past a
preposition (`PlaceRightAfter`: « viens à la base », « attends-moi à la mine », « reste à la
maison »), is going there; « viens voir ma base » is still come. **A killing verb with a monster
within three words** is guard (« tue les zombies », `KillVerbs`, `FoeWords`), and **« aide-moi à
… »** is help with what follows, read without the « aide-moi » (« aide-moi à couper du bois » is
wood). « lâche-moi » and « laisse-moi » are stay only as the whole message (« lâche-moi tes
diamants » hands them over). « y a du fer par ici ? » says what's here and doesn't call her. **A
follow before a going verb** is the player leading (« suis-moi, on va à la mine »: follow, not her
walk alone). After « tu » / « t' », a liking or knowing verb asks about her (`LikeVerbs`: « t'aimes
pêcher ? », « tu sais pêcher ? »); not « aime », or « je t'aime, suis-moi » would lose its order.
« œ » is spelled « oe » before reading (Normalize drops it: « œufs » read as « ufs »).

**An order can come in two steps.** A message that only names her (« syncs », « hé syncs ? ») is a
call, but only when the name opens or closes it; elsewhere it's talk about her. She answers with a
short « oui ? » (`McCalledLines`), and that player's **next message within 30 seconds** is read as
if it carried her name (`McCommand.Parse(…, called: true)`): « syncs » … « suis moi ». One message
only, whatever it turns out to be, so a call followed by chat doesn't keep her listening; a
follow-up she doesn't understand is read as talk (`minecraft.md`, *Talk*), never another « oui ? ». A call
with something she reads as talk in it (« syncs ça va ? ») is answered as talk instead of « oui ? »:
only a call with nothing to answer (`McTalkReader.IsBareCall`, or nothing read) gets it. Other players still need her
name in that window. **A compliment or a thank-you in that same place is not a call**: « gg syncs »,
« bravo syncs » (`Praise`, with a hop) and « merci syncs » (`Thanks`) get their own lines. An
adjective counts only when said to her (« t'es géniale »): « syncs la base est jolie » is about the
base, and stays a call. The window lives in the service, per player, in memory. There is no help
list: the owner removed it.
