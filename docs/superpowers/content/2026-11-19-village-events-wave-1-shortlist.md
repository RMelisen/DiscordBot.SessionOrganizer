# Village events — wave 1 shortlist (bébé and ado)

First content wave of the village events rework
(`docs/superpowers/specs/2026-11-17-plynling-village-events-design.md`, *Rollout* step 2).

**Status (2026-11-19):** the owner approved the list (« write the files »); all 30 are in
`ProjectSYNCS/Stories/Village/` (7.74.2) and pass the checks. The four decisions at the end were left
open, so the files take the defaults marked there: Aude and Mathurin stay out of the cast, the two
echo chains stay as they are, 21 rewrites. The files are what runs; where they differ from this list
(rolls at difficulty 3, A3's bouquet gives a primevère instead of a memory, A5's « en redemander »
gives *Bien calé*), the files win.

30 short events: **15 bébé, 15 ado**, the thresholds that let each stage stop drawing its old pulses.
21 rewrite one of the old 94 bébé/ado pulses (the old key is named), 9 are new, built on a villager's
« accroches » in `docs/village-lore.md`. Every old figure (chouette, tortue, hérisson, ours, héron,
pie, moineau…) is replaced by a Humeval villager or dropped.

How to read an option line: the label as the button shows it, then its stakes line as the card
would show it.

- 🪙 cailloux · 🎁 an item · ❤️ a villager remembers kindly · a plain name: remembers less kindly
- *Italic*: a modifier · **leçon** : a stat lesson · 🎲 a roll, with « sinon » for a failure
- ❔ a mystery option (reward hidden, costs still shown) · 🔒 an option shown locked without the trait
- 😣 stress for acting against a trait

Stakes are a draft: the amounts and modifiers get tuned when the files are written.

---

## Bébé (chez la Mère Lichen, à la Lisière ; premières descentes au hameau)

Gates and stress use the childhood traits only (Autoritaire, Adorable, Curieux, Rêveur,
Turbulent). No stress cost at all in this stage: a bébé is never asked to act against itself.

### B1 · « Gratte, gratte » — le Fou · from `baby_night_noise`

Une nuit de pluie, quelque chose gratte sous la fenêtre de la cabane. Puis ça tousse, poliment.
C'est le Fou, à genoux dans la mousse : il guette les lumières de la Lisière.
*Reads Arrival:* `[if memory fou.looked_up_with_him]` « Ah, c'est toi. Toi, tu sais regarder. »

- **Sortir le rejoindre, la couverture sur la tête** — ❔ on verra bien *(he shows the moss glowing
  faintly, or nothing at all that night: ❤️ le Fou + *Inspiré*)*
- **Rester sous la couverture** — *Bien reposé*
- **Compter les gouttes avec lui, par la fenêtre** 🔒 Rêveur — **leçon** : Savoir

### B2 · « Les deux tisanes » — la Mère Lichen, la Sauge · new

{A} renifle. La Mère Lichen a posé une tisane fumante à gauche ; la Sauge, venue exprès, une autre à
droite. Elles ne se regardent pas. Elles attendent.

- **Boire celle de la Mère Lichen** — ❤️ la Mère Lichen · *Apaisé*
- **Boire celle de la Sauge** — ❤️ la Sauge · **leçon** : Savoir *(it works in a morning, and is
  undrinkable: the grimace contest)*
- **Mélanger les deux** — ❔ on verra bien *(for once they agree on something: that it is
  undrinkable. *Le cœur léger*)*

### B3 · « Le compte des pousses » — l'Intendant · new

L'Intendant monte à la Lisière compter les pousses de la saison. Il se trompe toujours d'une, parce
qu'elles bougent. Aujourd'hui, {A} est dans le compte.

- **Ne plus bouger d'un poil** — ❤️ l'Intendant *(for once, the count is right)*
- **Changer de place à chaque fois** — *Malicieux* *(he counts one more, then two)*
- **L'aider à compter** — **leçon** : Intendance
- **Mettre toutes les pousses en rang** 🔒 Autoritaire — 🪙 10 *(« frais de rangement », noted in the
  register)*

### B4 · « Le caillou de compagnie » — la Mère Lichen · from `baby_pebble`

{A} shoote dans un galet frappé de la marque de Roncebelle. Il roule, s'arrête, et attend la suite.
C'est un ami. C'est aussi de l'argent.

- **Lui donner un nom et l'emmener partout** — 🎁 un galet
- **Le dépenser : c'est quand même un caillou** — 🪙 10
- **Le cacher sous le tas de la Mère Lichen** — ❔ on verra bien *(second beat next day: there are
  two pebbles now. She says nothing. ❤️ la Mère Lichen)*

### B5 · « Tout en haut du pommier » — le Tilleul · from `baby_apple_tree`

Au clos du Tilleul, {A} a grimpé chercher une pomme. La pomme est attrapée. La branche du bas, vue
d'en haut, est très loin.

- **Appeler à l'aide** — ❤️ le Tilleul *(he comes, then shouts it to the bees: the whole hamlet
  knows by noon)*
- **Redescendre sans aide** — 🎲 Courage 60 % : 🎁 une pomme · sinon *Les pattes sales*
- **Attendre que le pommier se baisse** 🔒 Rêveur — **leçon** : Savoir *(the Tilleul sits under the
  tree and tells the founding, a new version, without ever looking up)*

### B6 · « Où sont les autres ? » — l'Étrangère · from `baby_lost`

{A} cueillait des mûres à la Lisière avec tout le monde. {A} relève la tête : plus personne. Entre
les fougères, une petite lumière verte s'arrête, comme si elle attendait.

- **Suivre la lumière verte** — ❤️ l'Étrangère · 🎁 une morille *(a mushroom spot she shows only to
  those who can keep quiet)*
- **Suivre le ruisseau, sans aide** — 🎲 Courage 55 % : **leçon** : Courage · sinon *Rhume des bois*
- **Ne pas bouger et attendre** — *Apaisé* *(the Mère Lichen comes. « Tu as bien fait. »)*

### B7 · « La pâtisserie de boue » — Pain-Blond · from `baby_mud_pie`

{A} a ouvert une pâtisserie au bord d'une flaque. Le premier client est Pain-Blond, qui n'ose jamais
rien refuser, et qui regarde la tarte de boue avec beaucoup de courage.

- **Lui vendre une tarte, très cher** — 🪙 15 *(he pays, and adds a real brioche « pour l'échange »)*
- **Lui offrir la plus belle part** — ❤️ Pain-Blond *(the first one who ever fed him)*
- **Inventer un gâteau à trois étages** 🔒 Curieux — **leçon** : Intendance

### B8 · « Pas encore » — la Mère Lichen · from `baby_why`

Depuis ce matin, {A} suit la Mère Lichen avec des « pourquoi ». Pourquoi la Souche, pourquoi la
pluie, pourquoi tu ne dis rien. Elle répond toujours la même chose : « Pas encore. »

- **Encore un pourquoi, juste un** — **leçon** : Savoir
- **Se taire et s'asseoir à côté d'elle** — ❤️ la Mère Lichen *(her one wish: someone who sits
  without asking anything)*
- **Aller demander au Tilleul** — ❔ on verra bien *(he answers a completely different question,
  at length. *Distrait*, or *Inspiré*)*

### B9 · « Cinq minutes à l'auberge » — la Courge · from `baby_ice_cream`

La Courge doit s'absenter « cinq minutes » et pose {A} derrière le comptoir. Trois marmites, une
louche, et le Noyer qui entre déjà.

- **Servir les clients** — 🪙 15 *(tips)*
- **Goûter chaque marmite, pour bien conseiller** — *Bien calé*
- **Raconter une histoire à toute la salle** 🔒 Adorable — ❤️ la Courge *(she comes back an hour
  later to a silent, listening room)*

### B10 · « Les jumelles du Communal » — le Tilleul (off-stage) · new

Sur la place, les jumelles répètent ce que le Tilleul a dit à ses abeilles, tout de travers : à les
entendre, {A} a avalé le puits.

- **Rétablir la vérité, calmement** — **leçon** : Diplomatie
- **En rajouter : le puits, et le seau** — *En verve* *(the Courge has it by noon, improved)*
- **Leur courir après** 🔒 Turbulent — *Le cœur léger*

### B11 · « Le panier oublié » — le Noyer · from `baby_berry_stall`

Au bord du chemin, un panier de noix, et personne. {A} ne sait pas encore que le Noyer en « oublie »
un là chaque semaine, exprès.

- **Prendre une noix, juste une** — ❤️ le Noyer *(he is delighted, and will tell it for a month)*
- **Garder le panier jusqu'au retour de son propriétaire** — 🪙 15 *(the Noyer, caught out, pays for
  the guard duty)*
- **Rapporter le panier chez le Noyer** 🔒 Autoritaire — **leçon** : Intendance

### B12 · « L'appel de l'Ortie » — l'Ortie · new (the peek-a-boo of `baby_peekaboo`)

L'Ortie fait l'appel de ses quatre enfants devant chez elle. Il en manque un, comme toujours, le
même. {A}, qui passait, se retrouve dans la rangée.

- **Répondre « présent ! »** — ❤️ l'Ortie *(she feeds {A} with the others, and never says merci)*
- **Se cacher mieux que l'enfant qui manque** — **leçon** : Intrigue
- **Aller chercher l'enfant qui manque** 🔒 Curieux — 🎁 une bille *(he was in the hay, with his
  marbles)*

### B13 · « La comète » — Maître Frêne · from `baby_comet` · once in a lifetime

Une étoile à longue traîne traverse le ciel, très lentement. Tout le hameau est sur la place. Maître
Frêne, à la lanterne, l'inscrit déjà au registre.

- **Faire un vœu, très fort** — *Le cœur léger*
- **Courir derrière la comète** — **leçon** : Courage
- **Demander à Maître Frêne d'écrire son nom à côté** — ❤️ Maître Frêne *(the first entry in the
  register with a pousse's name in it)*

### B14 · « Les canards du bief » — le Chardon · new

Au moulin, {A} surprend le Chardon en train de jeter du pain aux canards. Il se fige. « Je ne nourris
rien du tout. »

- **Garder le secret** — ❤️ le Chardon *(an extra handful of flour in the bag, forever)*
- **Nourrir les canards avec lui** — *Le cœur léger*
- **Le répéter à tout le hameau** — 🪙 5 *(he grumbles, then pays « pour la paix ». le Chardon s'en
  souviendra, sans tendresse)*

### B15 · « Le loir du tiroir » — la Mère Lichen · from `baby_dormouse`

Les chaussettes de {A} disparaissent une par une. Ce matin, dans le tiroir : un loir, roulé dans un
nid de chaussettes. Il ouvre un œil, et se rendort.

- **Le laisser dormir et lui donner un nom** — *Le cœur léger*
- **Lui expliquer poliment que c'est un tiroir** — **leçon** : Diplomatie
- **Le porter au hameau pour le montrer** — ❔ on verra bien *(the Courge lays a place for it at
  table. ❤️ la Courge)*

---

## Ado (à la Maison des Pousses, chez la Bruyère)

Gates and stress use the personality traits (two at ado). `{B}` is another Plynling the ado knows;
those events only name it, with an affinity effect — no handed-over decision (later build).

### A1 · « Les règles de la Maison » — la Bruyère · from `teen_shortcut` + `teen_broken_mirror`

Règle 14 : on ne sort pas après la soupe. Ce soir, tout le monde va au Pont-Vieux. Une pousse maligne
trouve toujours la règle qui manque.

- **Obéir à la règle 14** — ❤️ la Bruyère
- **Trouver la règle qui manque** — 🎲 Intrigue 50 % : *Malicieux* · sinon *Grognon* *(full name, and
  the laundry)*
- **Sortir quand même** — 😣 +15 stress (Juste) · *Plein d'élan*

### A2 · « Après l'extinction des feux » — la Bruyère, {B} · from `teen_pillow_fight`

Minuit au dortoir. Les lanternes sont éteintes depuis longtemps. Dans le noir, un oreiller vole, et
atterrit sur {A}. Ça vient du lit de {B}.

- **Riposter, de toutes ses forces** — affinité avec {B} · *L'âme de la fête*
- **Faire semblant de dormir** — ❔ on verra bien *(the Bruyère comes to tuck everyone in, once
  they sleep. {A} feels it. ❤️ la Bruyère · *Apaisé*)*
- **Proclamer une trêve** 🔒 Juste — **leçon** : Diplomatie

### A3 · « Le voleur de fleurs » — Mathurin, Aude · from `teen_flower_thief`

Chaque matin, une fleur de plus manque au jardin de la Maison. Proprement coupée. La Bruyère compte
les tiges, et les têtes des pousses.

- **Guetter à l'aube** — ❔ on verra bien *(Mathurin, red to the ears, with one flower. {A} joins the
  village's complicity: ❤️ le Chardon)*
- **Planter une pancarte « Défense de cueillir »** — **leçon** : Intendance *(ignored, but very well
  written)*
- **Laisser un bouquet tout prêt sur le rebord** 🔒 Fleur bleue — *Le cœur léger*

### A4 · « Les lucioles du Pont-Vieux » — la Bruyère · from `teen_fireflies`

Une nuit par an, les lucioles se donnent rendez-vous sur le Pont-Vieux. C'est ce soir, bien après
l'heure.

- **Sortir sans bruit par la fenêtre** — ❔ on verra bien *(the Bruyère is already on the bridge, in
  her shawl. Neither will ever mention it.)*
- **Demander la permission** — *Choyé* *(« Non. » Then: « Mets ton écharpe. »)*
- **Rester au lit et en rêver** — *Bien reposé*

### A5 · « Le four de la cave » — Pain-Blond · new (from `teen_burnt_cake` / `teen_bakery`)

La nuit, il y a de la lumière sous le fournil. Dans la cave, Pain-Blond essaie une recette qu'il
n'ose montrer à personne. Il sursaute, la plaque à la main.
*Reads Arrival:* `[if memory pain_blond.ate_crust]` « Toi… la miette sur le rebord. »

- **Goûter, et dire franchement** — ❤️ Pain-Blond *(his honest taster from now on)*
- **Goûter, et en redemander** — 🎁 une brioche au miel *(the one thing he hoped to hear)*
- **Remonter sans bruit** — *La conscience tranquille*
- **Goûter la chose aux noix** *(secret: shown only with the Arrival memory)* — ❔ on verra bien

### A6 · « Les quatre de l'Ortie » — l'Ortie · new

L'Ortie confie ses quatre enfants à {A} pour l'après-midi, « une heure, pas plus », et s'en va avant
qu'on dise non. Au bout de dix minutes, il en manque un. Toujours le même.

- **Les compter toutes les cinq minutes** 🔒 Travailleur — 🪙 20 *(she pays double, so as to owe
  nothing)*
- **Les emmener tous à la rivière** — *Le cœur léger*
- **Chercher celui qui manque** — 🎲 Savoir 55 % : ❤️ l'Ortie · sinon *Les pattes sales*

### A7 · « Le ruban de Maître Frêne » — Maître Frêne, le Noyer, le Chardon · new

Maître Frêne mesure le pré de la Sommeille et cherche quelqu'un pour tenir le bout du ruban. Le
Noyer regarde d'un côté du pré, le Chardon de l'autre. Ils font semblant de ne pas regarder.

- **Tenir le ruban bien droit** — ❤️ Maître Frêne · *La conscience tranquille*
- **Le tirer un peu côté Noyer** — ❤️ le Noyer · le Chardon s'en souviendra
- **Le tirer un peu côté Chardon** — ❤️ le Chardon · le Noyer s'en souviendra
- **Lâcher le ruban et compter les bornes avec le Fou** 🔒 Excentrique — ❔ on verra bien

*Read later by the adulte events of the pré quarrel.*

### A8 · « Un nom dans la marge » — {B}, la Courge · from `teen_crush`

{A} rougit quand {B} passe. Le nom de {B} est écrit dans la marge d'un livre de la Maison, effacé,
puis réécrit, plus petit.

- **Laisser une fleur devant le lit de {B}, sans signer** — affinité avec {B}
- **Aller parler à {B}, de n'importe quoi** — 🎲 Diplomatie 50 % : affinité avec {B} · sinon *En
  verve* *(a long speech about turnips)*
- **En parler à la Courge** — ❤️ la Courge *(by noon, the whole place knows, embellished, kindly)*

Never makes a couple (engine rule).

### A9 · « La noce des renards » — the custom · from `teen_fox_wedding`

Grand soleil, et pourtant il pleut : les renards se marient. La noce passe à la Lisière en ce moment
même, et la coutume est claire : personne ne regarde.

- **Écarter les fougères, juste un peu** — ❔ on verra bien
- **Fermer les yeux et compter jusqu'à cent** — *Apaisé*
- **Laisser un bouquet au bord du chemin, sans regarder** — 🎁 une plume *(found the next morning, in
  its place)*

### A10 · « Par la Côte » — l'Intendant · from `teen_runaway` · once in a lifetime

Ce matin, {A} quitte Humeval. Pour toujours. Le baluchon est prêt : deux tartines, un galet, une
chaussette. À mi-Côte, l'Intendant descend vers le hameau, son registre sous le bras.

- **Monter jusqu'en haut** — **leçon** : Courage *(the road goes on; Roncebelle stays out of sight)*
- **Redescendre avec l'Intendant** — ❤️ l'Intendant *(he pretends not to know the shortcuts, and
  takes one)*
- **S'asseoir et manger les tartines** — *Bien calé* *(back for supper. The Bruyère says: « Mange. »)*

### A11 · « La pile d'écharpes » — la Bruyère · new

Envoyé chercher une couverture dans l'armoire de la Bruyère, {A} trouve une pile d'écharpes
tricotées, toutes de la même couleur. Une par hiver, à peu près.

- **Refermer l'armoire et ne rien dire** — ❤️ la Bruyère *(she noticed)*
- **Compter les écharpes** — **leçon** : Intendance *(« Onze. »)*
- **Lui demander pour qui c'est** — ❔ on verra bien *(« Mange. »)*

The secret stays a secret: no option gives a scarf to the Intendant (the bible: the tender secret
stays untold).

### A12 · « Le guet du gué » — Aude, Mathurin, le Noyer · new

Au gué, Aude et Mathurin se parlent tout bas. {A} fait le guet, comme tout le village l'a fait un jour.
Sur le chemin, le Noyer arrive, et il chantonne.

- **Siffler le signal** — *Le cœur léger*
- **Retenir le Noyer en parlant de ses citrouilles** — 🎲 Diplomatie 55 % : ❤️ le Noyer · sinon
  **leçon** : Diplomatie *(he walks by and sees nothing anyway; he never knows)*
- **Se cacher dans les roseaux** — **leçon** : Intrigue

### A13 · « Les noix de la nuit » — Jacquet, le Noyer, l'Ortie · new

Jacquet, l'aîné de l'Ortie, gratte au carreau de la Maison. Il va chiper des noix chez le Noyer, et
il lui faut quelqu'un pour tenir le sac.

- **Tenir le sac** — 😣 +15 stress (Franc) · *Malicieux* *(and the Ortie's nut tart, the next day)*
- **Refuser, et retourner se coucher** — *La conscience tranquille*
- **Prévenir le Noyer** — ❔ on verra bien *(the next morning, the « forgotten » basket is twice
  as big)*

### A14 · « La graine de l'Étrangère » — l'Étrangère · from `teen_seed` · two beats

L'Étrangère passe à la Maison et donne à chaque pousse une graine du Bois, pâle, qu'elle n'a jamais
vu pousser ici. {A} met la sienne dans un pot, sur le rebord.

- **S'en occuper chaque jour, avec un carnet** — **leçon** : Savoir
- **Lui parler tous les soirs** — ❔ on verra bien
- **L'oublier un peu** — *Bien reposé*

Second beat the next day, whatever was chosen: what came up (a pale sprout that glows faintly at
night, a very ordinary radish, or nothing yet). The beat gives ❤️ l'Étrangère to whoever goes to
show her.

### A15 · « La partie de courge » — Maître Frêne, la Courge · from `teen_ballgame`

Une fois l'an, tout le hameau joue à la balle d'un bout à l'autre, avec une courge séchée. Les
règles tiennent en une ligne, et personne ne se souvient de la ligne. Maître Frêne en a écrit
quarante.

- **Plonger dans la mêlée** — 🎲 Courage 55 % : **leçon** : Courage · sinon *Les pattes sales*
- **Tenir la buvette avec la Courge** — 🪙 15
- **Arbitrer avec Maître Frêne** — ❤️ Maître Frêne

---

## What this wave sets up

- **Choices that come back inside the wave:** B1 and A5 read Arrival's memories (a line, and A5's
  secret option).
- **Memories set for later waves:** B3 (l'Intendant, for the autumn count), A7 (le Noyer / le
  Chardon / Maître Frêne, for the pré quarrel), B14 (le Chardon, the mill), A5 (Pain-Blond, his
  cellar), A11 (la Bruyère). Every memory above is one a later event can plausibly read; the rest
  were left out on purpose (writing rule: a memory only when something will read it).
- **Features shown at least once:** mystery (14), locked trait option (12), a stress cost against a
  trait (A1, A13), rolls (7), once in a lifetime (B13, A10), a second beat (B4's hide option, A14),
  a secret option behind a memory (A5), two-Plynling (A2, A8).
- **Cailloux** stay ≤ 20 per path (cap 30).

---

## Decisions for you

1. **Cuts and swaps.** Anything to drop or trade? Old premises left out and available as swaps:
   `baby_puddle`, `baby_secret_language`, `baby_imaginary` / `baby_brindille` (the Courge's extra
   plate fits), `baby_otter`, `teen_slang` (« tout mirabelle »), `teen_fish_tale`, `teen_dare`,
   `teen_ghost_stories`, `teen_stalls` (Pain-Blond ↔ la Courge).
2. **Aude and Mathurin in the cast?** A3 and A12 would read better with their own memories. Today
   only the 13 main villagers can hold one, so A3 gives its memory to le Chardon. Adding them is two
   lines in `village.cast`.
3. **The two old chains with adulte echoes.** `baby_grow_up` (« Plus tard, je serai… ») and
   `teen_apprentice` (choosing a master) send follow-ups that land in adulte (`grown_dream_*`,
   `grown_master_*`). Once their stage stops drawing old pulses, those echoes stop too. Options:
   keep those two drawn after the retirement until the growing-up arcs replace them (my pick), or let
   them go.
4. **Rewrite count.** This wave uses 21 of the « best 20–30 » old premises on bébé and ado alone.
   Fine to count the cap per stage, or should I keep a few for adulte and write more new ones here?
