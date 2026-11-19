# Village events — wave 2 shortlist (adulte and ancien)

Second content wave of the village events rework
(`docs/superpowers/specs/2026-11-17-plynling-village-events-design.md`, *Rollout* step 3, short
events). Long events (titles, keepsakes, trait swaps) come after, one session each, as the design
says.

**Status (2026-11-19):** approved (« write the files »); all 54 are in `ProjectSYNCS/Stories/Village/`
(7.74.2) and pass the checks. The decisions at the end were left open: no cast change, old adulte and
ancien pulses still drawn. The files are what runs; where they differ (rolls at difficulty 3, S9's
honey is *Bien calé*, S14's ribbon is a mother-of-pearl button, S17's find is a rusty key, a few
memories added), the files win.

**The owner's rule for this wave: an ancien also draws the adulte events.** So most arcs say
`stage: adult, elder`, a few are adulte only (things an ancien is past: a first house, an
apprenticeship), and a few are ancien only (passing things on, being someone people come to see).

| Pool | Shared | Adulte only | Ancien only | Seasonal (shared) | Drawn all year |
|---|---|---|---|---|---|
| Adulte | 30 | 10 | — | 4 | **40** (threshold 40) |
| Ancien | 30 | — | 10 | 4 | **40** (threshold 25) |

54 short events in all: 18 rewrite an old adulte/ancien pulse (named), 36 are new, built on the
villagers' « accroches » (`docs/village-lore.md`). Seasonal ones are drawn only in their season
(`requires: [season …]`), so they don't count toward the thresholds.

Notation as in wave 1: 🪙 cailloux · 🎁 item · ❤️ a villager remembers (plain name: less kindly) ·
*italic* modifier · **leçon** stat lesson · 🎲 roll / « sinon » · ❔ mystery · 🔒 locked (trait or
passion) · 😣 stress. Amounts are drafts.

**Choices that come back.** Wave 1 left memories with the villagers; here they are read. Each
« *Reads* » line names the wave 1 (or Arrival) memory and what it changes: an extra line, or a
secret option that only shows to a Plynling who did that as a child.

---

## Shared: adulte and ancien (30)

### S1 · « Le colis égaré » — le clerc de l'Intendant · from `grown_parcel`

The owner's own example of an event whose options all felt the same, redone with the design's
stakes line. Un colis attend devant la porte de {A}, sans adresse. Ça tinte. Le clerc de l'Intendant
l'a déposé là en courant, et il est déjà en train de tomber plus loin, avec ses registres.

- **L'ouvrir** — 🎲 Courage 55 % : 🎁 un objet · sinon *Les nuits blanches*
- **Chercher le propriétaire** — ❤️ la Courge *(the Courge knows everyone's parcels)*
- **Le garder pour plus tard** — 🪙 15

### S2 · « La page tachée » — Maître Frêne · new

{A} recopie le vieux registre des raies pour Maître Frêne. Une page n'a pas la même encre que les
autres : on l'a redessinée de mémoire, un jour, après un encrier renversé.

- **Recopier sans rien dire** — ❤️ Maître Frêne *(he saw that {A} saw)*
- **Lui montrer la page, tout bas** — ❔ on verra bien *(« Examinons… » then a very long silence,
  and relief)*
- **Comparer avec le registre de l'Intendant** 🔒 Travailleur — **leçon** : Intendance *(they never
  agree anyway)*

### S3 · « La caisse du conseil » — Maître Frêne · new

La caisse du conseil a trois cailloux de moins qu'au dernier compte. Comme toujours. Maître Frêne
recompte pour la quatrième fois, et demande de l'aide.

- **Recompter avec lui** — **leçon** : Intendance
- **Glisser trois cailloux dans la caisse, en douce** — *La conscience tranquille* · ❤️ Maître Frêne
- **Demander au Fou** — ❔ on verra bien *(the Fou finds everything)*

### S4 · « Les bornes du Fou » — le Fou, le Noyer, le Chardon · new

Chaque matin, le Fou compte les bornes du pré de la Sommeille, et le compte change. Ce matin, le
Noyer et le Chardon ont chacun demandé à {A}, en cachette, de leur rapporter le chiffre.
*Reads* `noyer.ribbon_his_side` / `chardon.ribbon_his_side`: « Toi, tu sais où est la limite. »

- **Donner le vrai chiffre aux deux** — *La conscience tranquille*
- **Donner à chacun le chiffre qui l'arrange** — 🎲 Ruse 50 % : *Malicieux* · sinon *Grognon*
  *(they compare at the inn)*
- **Rapporter au Fou qu'on le demande** — ❤️ le Fou *(« Ah ! Alors c'est cinq. »)*

### S5 · « Le dimanche de la Bruyère » — la Bruyère · new

La Bruyère ne veut qu'une chose : des pousses qui partent prêtes, et qui repassent le dimanche. {A}
repasse.
*Reads* `bruyere.felt_tucked`, `bruyere.on_the_bridge`, `bruyere.kept_rule`: one line each (« Tu ne
dormais pas, ce soir-là. Je le savais. »).

- **Laver le linge avec elle** — ❤️ la Bruyère *(« S'ils veillent si bien, qu'ils viennent laver le
  linge. » {A} vient.)*
- **Raconter une bêtise aux ados de la Maison** — *L'âme de la fête*
- **Réparer le banc qu'elle réclame à Maître Frêne** 🔒 Travailleur — **leçon** : Intendance

### S6 · « Les cuillères de Guérin » — l'Ortie, Guérin · from `grown_spoons`

Depuis une semaine, les cuillères disparaissent au hameau. Elles reviennent toutes chez Guérin,
le mari de l'Ortie, taillées avec une tête. Celle d'aujourd'hui a la tête de {A}.

- **L'acheter, et rire** — *Le cœur léger*
- **Lui demander un nez plus petit** — **leçon** : Diplomatie *(the next one has a bigger nose)*
- **En commander une pour la Courge** — ❤️ l'Ortie *(she never says thank you; she gives a tart)*

### S7 · « Le remède de la Sauge » — la Sauge · from `grown_marsh_remedy`

{A} a mal au dos depuis une semaine. La Sauge a le remède. Il marche très bien, et il est
imbuvable : le village a même un concours de grimaces qui ne dit pas son nom.
*Reads* `sauge.drank_hers`: « Toi, tu bois sans faire d'histoires. » (a secret option: ask for the
double dose, 🪙 0, *Bien reposé* and her respect)

- **Boire d'un trait** — *Apaisé*
- **Aller chercher du miel du Tilleul pour l'adoucir** — ❤️ la Sauge *(her one wish: the remedy that
  tastes good)*
- **Refuser et dormir sur une planche** — **leçon** : Courage

### S8 · « La plante qui pousse loin » — la Sauge, l'Étrangère · new

La Sauge a toujours besoin d'une plante qui pousse loin, haut, ou dans la boue. Aujourd'hui, c'est
dans les Mouilles, et elle paie.

- **Entrer dans les Mouilles** — 🎲 Courage 55 % : 🪙 20 · sinon *Les pattes sales*
- **Demander le chemin à l'Étrangère** — ❤️ l'Étrangère · 🪙 10
- **Rapporter « une plante qui y ressemble »** — ❔ on verra bien

*Reads* `etrangere.followed_light`: the Étrangère goes with {A} without being asked.

### S9 · « Les ruches du Tilleul » — le Tilleul · new

Le Tilleul n'a qu'une envie : quelqu'un à qui montrer les ruches, et qui écoute jusqu'au bout.
L'histoire est longue. Elle recommence au milieu.
*Reads* `tilleul.called_from_tree`: « Toi, tu tombais de mon pommier. »

- **Écouter jusqu'au bout** — ❤️ le Tilleul
- **L'aider à sortir les cadres** — 🎁 un rayon de miel *(food or collectible, to pick)*
- **Parler aux abeilles soi-même** 🔒 passion Insectes — **leçon** : Savoir

### S10 · « Le bois de l'hiver » — l'Intendant, la Bruyère · new

Chaque hiver, quelqu'un fait livrer du bois à la Maison des Pousses sans signer. Cette nuit,
l'Intendant demande à {A} de l'aider à décharger, sans bruit.
*Reads* `bruyere.saw_scarves`: {A} pense à l'armoire, et n'en dit rien.

- **Décharger en silence** — ❤️ l'Intendant
- **Signer d'une petite croix, pour rire** — ❔ on verra bien *(the Bruyère brings soup to the
  Côte the next morning, and says nothing)*
- **Lui dire que la Bruyère sait** — *Le cœur léger* *(« Je sais qu'elle sait. »)*

The secret stays: nobody says it aloud, nothing changes between them.

### S11 · « Aude au gué » — Aude, Mathurin · new

Aude, la fille du Noyer, tend un petit mot plié à {A}. « Pour le moulin. Tu ne l'ouvres pas. » Au
moulin, Mathurin attend déjà à la fenêtre, rouge jusqu'aux oreilles.
*Reads* `noyer.pumpkin_talk`: Aude grins. « Toi, tu sais parler citrouilles. »

- **Porter le mot sans l'ouvrir** — *Le cœur léger*
- **Aider Mathurin à écrire la réponse** — **leçon** : Diplomatie
- **Faire un détour par la place, très visible** 🔒 Moqueur — *Malicieux* *(the whole
  village pretends not to see, as usual)*

### S12 · « La colonne vide » — Maître Frêne · new

Dans le registre des pousses, Maître Frêne a une colonne « veilleur », qu'il laisse poliment vide.
Aujourd'hui, la plume en l'air, il demande à {A} ce qu'il faudrait y mettre.
*Reads* `frene.comet_register`: « Ton nom est déjà à côté d'une comète. »

- **Laisser la colonne vide** — ❤️ Maître Frêne *(« Il convient. »)*
- **Faire écrire « quelqu'un »** — *Inspiré*
- **Y dessiner un petit œil** — ❔ on verra bien *(the Fou approves loudly from the window)*

### S13 · « Une heure assise » — l'Ortie · new

L'Ortie n'a qu'une envie : une heure assise, une seule, sans que personne ne crie « maman ».
*Reads* `ortie.found_the_one`: « Toi. Tu trouves le petit. Prends-les. »

- **Prendre les quatre enfants pour l'après-midi** — 🪙 20 *(she pays double, to owe nothing)*
- **Lui apporter une chaise et une tisane, et monter la garde** — ❤️ l'Ortie
- **L'emmener à l'auberge** — ❔ on verra bien *(the Courge takes the kids; the Ortie sleeps
  sitting up)*

### S14 · « La colporteuse » — la colporteuse · new · cooldown 90 days

La colporteuse passe deux fois l'an : des rubans, du sel, les nouvelles d'ailleurs. Elle ne dit
jamais son nom.

- **Acheter un ruban** — 🎁 un ruban *(if the catalog has one; else a modifier)*
- **Échanger une histoire du village contre ses nouvelles** — **leçon** : Savoir
- **Lui demander son nom** — ❔ on verra bien *(she answers with a riddle)*

### S15 · « Avant le dessert » — l'Étrangère, la Courge · new

Au dîner de l'auberge, l'Étrangère se lève, comme toujours, juste avant le dessert. La Courge la
regarde partir. Elle sait d'où elle vient. Elle ne le dit pas.

- **La rattraper avec deux parts de tarte** — ❤️ l'Étrangère *(her wish: to feel she is from here)*
- **Garder sa place et sa part** — *Le cœur léger* *(she comes back for it, once)*
- **Demander à la Courge pourquoi** — ❔ on verra bien *(she almost tells, and bites her tongue;
  nothing is revealed)*

### S16 · « Le coin à champignons » — l'Étrangère · from `grown_forage`

L'Étrangère propose à {A} de l'accompagner à la cueillette avant l'aube. Une condition : ne jamais
montrer le chemin.

- **Jurer le secret** — 🎁 une morille · ❤️ l'Étrangère
- **Accepter, et noter le chemin en douce** — 😣 +15 (Franc) · 🎁 une morille · *Malicieux*
- **Refuser : avant l'aube, très peu pour soi** — *Bien reposé*
- **Retourner au coin des trois morilles** *(secret: only with* `etrangere.followed_light`*)* —
  🎁 une truffe

### S17 · « Les chaussures du Fou » — le Fou · new

Le Fou retrouve tout ce qui est perdu, sauf ses propres chaussures. {A} vient d'en repêcher une
paire dans la Sommeille.

- **Les lui rapporter** — ❤️ le Fou
- **Lui demander de retrouver quelque chose en échange** — ❔ on verra bien *(he finds something {A}
  didn't know was lost: an item)*
- **En faire deux pots de fleurs** — *Le cœur léger*

### S18 · « La chèvre de Dame Ermengarde » — l'Intendant · new

La chèvre de Dame Ermengarde s'est encore échappée. Elle broute devant la Maison commune, juste
entre l'Intendant et la porte. L'Intendant a peur des chèvres, et refuse de l'écrire.

- **Attraper la chèvre** — 🎲 Courage 55 % : 🪙 10 *(Dame Ermengarde)* · sinon *Les pattes sales*
- **Faire le grand tour avec l'Intendant, l'air de rien** — ❤️ l'Intendant
- **Proposer à la chèvre une meilleure herbe** 🔒 Patient — **leçon** : Diplomatie

### S19 · « Le conseil qui ne finit pas » — Maître Frêne · new

L'envie de Maître Frêne : une séance du conseil qui finisse à l'heure. Une seule fois. Renaud vient
de voter contre l'ordre du jour, par principe.

- **Tenir le sablier, très sérieusement** — ❤️ Maître Frêne
- **Sortir le cidre en avance** — *L'âme de la fête*
- **Partir avant la fin** 🔒 Impatient — *Plein d'élan*

### S20 · « Les registres à l'eau » — le clerc de l'Intendant, l'Intendant · new

Sur le Pont-Vieux, le clerc de l'Intendant trébuche. Trois registres tombent dans la Sommeille.

- **Plonger** — 🎲 Courage 50 % : ❤️ l'Intendant · sinon *Rhume des bois*
- **Faire sécher les pages une par une** — **leçon** : Intendance
- **Les réécrire de mémoire avec le Fou** — ❔ on verra bien *(the numbers are better than before)*

### S21 · « Clément contre Gaspard » — les fils du Noyer · from `grown_pie_contest`

Clément et Gaspard, les fils du Noyer, sont en concours, comme toujours : qui porte le plus, qui
mange le plus. Aujourd'hui, il leur faut un juge, et un troisième concurrent.

- **Concourir contre eux** — 🎲 Courage 45 % : *Plein d'élan* · sinon *Bien calé*
- **Arbitrer, sans favori** 🔒 Juste — **leçon** : Diplomatie
- **Proposer le concours du plus silencieux** — ❔ on verra bien *(it lasts until the Noyer arrives
  and speaks of himself in the third person)*

### S22 · « Le dîner de l'Intendant » — l'Intendant · new

L'Intendant a fini son compte et remonte la Côte. Il espère, sans se l'avouer, qu'on l'invite à
rester dîner.
*Reads* `intendant.walked_down`: he takes the shortcut without pretending.
*Reads* `intendant.stood_still`: « Le seul compte juste de ma vie. »

- **L'inviter chez soi** — ❤️ l'Intendant
- **L'emmener à l'auberge** — *L'âme de la fête* *(the Courge sets his place first)*
- **Lui souhaiter bonne route** — **leçon** : Intendance *(he leaves {A} a copy of his weather notes)*

### S23 · « Qui nourrit le mieux » — la Courge, Pain-Blond · from `grown_feast`

Pain-Blond et la Courge se disputent depuis toujours pour savoir qui nourrit le mieux le village.
Ce soir, ils ont désigné un juge : {A}.

- **La soupe de la Courge** — ❤️ la Courge · Pain-Blond s'en souviendra
- **Le pain de Pain-Blond** — ❤️ Pain-Blond · la Courge s'en souviendra
- **Déclarer l'égalité, et reprendre des deux** — *Bien calé*

### S24 · « Le juge d'un jour » — Maître Frêne · from `grown_judge`

Maître Frêne a un rhume (la Sauge y veille). Quelqu'un doit régler la querelle du jour au
Communal : à qui le tour de la chèvre. {A} est désigné.

- **Trouver un compromis** — **leçon** : Diplomatie
- **Appliquer le règlement de Maître Frêne, à la lettre** — ❤️ Maître Frêne
- **Couper la journée de la chèvre en deux** — ❔ on verra bien

### S25 · « L'averse à la Maison commune » — {B} (rival) · from `grown_storm_shelter`

L'averse tombe d'un coup sur la place. Le seul abri ouvert, c'est la Maison commune. {A} entre,
trempé, et {B} est déjà là, avec qui {A} ne s'entend pas.

- **Partager son pain** — affinité avec {B} · *La conscience tranquille*
- **S'asseoir à l'autre bout, sans un mot** — *Apaisé*
- **Ressortir sous la pluie, par fierté** — 😣 +15 (Modeste) · *Plein d'élan* · *Rhume des bois*

### S26 · « Le plat mijoté » — {B} (ami) · from `grown_bad_dish`

Toute la semaine, {B} a mijoté un plat « rien que pour {A} ». Le plat est vert. Il fait un petit
bruit. {B} attend, les yeux brillants.

- **Dire que c'est délicieux, et finir l'assiette** — 😣 +10 (Franc) · affinité avec {B} · *Bien
  calé*
- **Dire la vérité, avec douceur** — **leçon** : Diplomatie
- **En porter une part à la Sauge, « pour la science »** — ❔ on verra bien *(she declares it a
  remedy)*

### S27 · « La remarque » — {B} (rival), la Courge · from `grown_snide`

À l'auberge, en passant, {B} glisse que le chapeau de {A} « a dû être très à la mode, autrefois ».
La Courge a entendu. Toute la place l'entendra.

- **Répondre du tac au tac** — 🎲 Ruse 50 % : *En verve* · sinon *Boudeur*
- **Sourire, et ne rien dire** — *La conscience tranquille*
- **Approuver : « Autrefois, oui. Et ça reviendra. »** — ❤️ la Courge *(she tells it to everyone,
  improved)*

### S28 · « L'écharpe prêtée » — {B} (ami) · from `grown_scarf`

Voilà trois semaines que {A} porte l'écharpe que {B} lui a prêtée. Elle tient très chaud, et {B} n'a
rien redemandé.

- **La rendre, lavée et pliée** — affinité avec {B} · *La conscience tranquille*
- **La garder encore un peu** — *Choyé*
- **Proposer de la partager, un jour chacun** — ❔ on verra bien

### S29 · « Le chapeau envolé » — le passeur · from `grown_hat_wind`

Une rafale arrache le chapeau de {A}. Il file au-dessus des toits, vers la Sommeille.

- **Courir après** — 🎲 Courage 55 % : **leçon** : Courage · sinon *Les pattes sales*
- **Le laisser partir** — *Le cœur léger* *(the passeur brings it back a week later, wearing it)*
- **Demander au Fou de le retrouver** — ❤️ le Fou

### S30 · « La croûte qui disparaît » — Pain-Blond · new

Pain-Blond pose chaque matin la croûte des veillés sur le rebord. Elle a toujours disparu le
lendemain. Cette fois, il demande à {A} de veiller.
*Reads* `pain_blond.ate_crust`: « Toi, tu avais laissé une miette. »

- **Veiller toute la nuit** — ❔ on verra bien *({A} falls asleep at three; the crust is gone)*
- **Poser une croûte à soi à côté** — ❤️ Pain-Blond
- **Demander au Fou** — *Inspiré* *(« On l'a mangée, là-haut. Et on a dit merci. »)*

The mystery stays a mystery.

---

## Seasonal, shared (4) — `requires: [season …]`

### P1 · « Les citrouilles de la foire » (printemps) — le Noyer · new

Le Noyer veut gagner une fois le concours de la foire sans que le jury soit de sa famille. Cette
année, il propose {A} comme juré.

- **Juger honnêtement** 🔒 Juste — ❤️ le Noyer *(he loses, and is proud of it)*
- **Faire gagner la citrouille de l'Ortie** — ❤️ l'Ortie · le Noyer s'en souviendra
- **Refuser d'être juré** — *La conscience tranquille*

### P2 · « La moisson » (été) — le Noyer, l'Ortie · new

Tout le monde aux Longues Raies. Le Noyer paie bien, pour qu'on le dise. L'Ortie lui dit ses quatre
vérités entre deux gerbes.

- **Moissonner jusqu'au soir** — 🪙 25
- **Faire la sieste à l'ombre du Noyer** — *Bien reposé*
- **Se baigner dans la Sommeille avec les enfants** — *Le cœur léger*

### P3 · « Le jour du Compte » (automne) — l'Intendant · new

L'Intendant descend la Côte pour tout compter, et le village lui cache une chose, comme chaque
année. Cette année, c'est {A} qui cache la chèvre. Il a peur des chèvres.
*Reads* `intendant.stood_still`: « Pour une fois que quelqu'un ne bougeait pas. »

- **La cacher chez soi** — *Malicieux*
- **La cacher dans la Maison commune, sous un banc** — 🎲 Ruse 50 % : *L'âme de la fête* · sinon
  *Les pattes sales*
- **Lui souffler où elle est, avant le soir** — ❤️ l'Intendant · 😣 +10 (Superstitieux) *(the
  custom)*

### P4 · « La Longue Nuit » (hiver) — le Tilleul, la Mère Lichen · new

La nuit la plus longue, tout le village dîne à la Maison commune. Le Tilleul raconte la fondation,
une version neuve. La Mère Lichen est venue, ce qui n'arrive jamais.
*Reads* `lichen.sat_quiet`: she pats the bench next to her.

- **Écouter le Tilleul jusqu'au bout** — ❤️ le Tilleul
- **S'asseoir à côté de la Mère Lichen** — ❤️ la Mère Lichen
- **Raconter sa propre histoire** — *En verve*

---

## Adulte only (10)

### a1 · « La première maison » — la Courge, Pain-Blond, la Bruyère · new · once in a lifetime

{A} emménage dans sa propre petite maison. La Courge a apporté une marmite, Pain-Blond une miche,
la Bruyère un balai, sans un mot.

- **Pendre la crémaillère** — *L'âme de la fête*
- **Dormir la première nuit, enfin seul** — *Bien reposé*
- **Peindre la porte** 🔒 passion Peinture — **leçon** : Intendance

### a2 · « L'apprenti du Chardon » — le Chardon · new

Le Chardon a besoin d'un apprenti d'un jour : Mathurin est au gué, comme par hasard.
*Reads* `chardon.kept_ducks`: he hands {A} the bread for the ducks. *Reads* `chardon.told_ducks`:
« Toi, tu parles trop. » (no consequence)

- **Porter les sacs** — 🎲 Courage 55 % : 🪙 15 · sinon *Bien calé* *(flour everywhere)*
- **Lui dire que sa farine est la meilleure de la vallée** — ❤️ le Chardon *(he weighs the
  compliment)*
- **Demander qui sont les noms sur la poutre** — ❔ on verra bien

### a3 · « Le fournil à l'aube » — Pain-Blond · new

Pain-Blond a besoin de bras avant l'aube, et paie en pain chaud.
*Reads* `pain_blond.honest_taster`: he hands {A} the new loaf first. « Dis-moi. »

- **Pétrir** — 🪙 10 · *Bien calé*
- **Chanter pour que la pâte lève** — ❤️ Pain-Blond *(he talks to dough; now so does {A})*
- **Livrer les miches dans le hameau** — **leçon** : Diplomatie

### a4 · « Ce soir à l'auberge » — la Courge · new

L'auberge a toujours besoin de quelque chose avant ce soir. Aujourd'hui : un musicien.

- **Jouer** 🔒 passion Musique — 🪙 25
- **Chanter quand même** — 🎲 Diplomatie 50 % : *L'âme de la fête* · sinon *En verve*
- **Servir à la place** — ❤️ la Courge

### a5 · « Une journée aux Longues Raies » — le Noyer, l'Ortie · new

Le Noyer embauche à la journée. L'Ortie, sa meilleure journalière, lui tient tête toute la matinée.

- **Travailler dur** — 🪙 20
- **Donner raison à l'Ortie, devant lui** — ❤️ l'Ortie · le Noyer s'en souviendra
- **Faire la sieste sous le Noyer** 🔒 Paresseux — *Bien reposé* *(he laughs, and tells it for a
  month)*

### a6 · « Le mot dans la pierre » — {B} (ami) · from `grown_letters`

Depuis des semaines, {A} pense à {B}. Sous le Pont-Vieux, entre deux pierres, on glisse des mots
qu'on n'ose pas dire.

- **Écrire un poème, le plus beau possible** — ❔ on verra bien
- **Écrire simplement : « Je pense à toi. »** — affinité avec {B} · *Le cœur léger*
- **Garder ça pour soi** — *Apaisé*

Never makes a couple (engine rule); the old `social_declare` stays the way to declare.

### a7 · « Les branches basses » — le Tilleul, les jumelles · new

Le Tilleul taille son verger. Il laisse exprès les branches basses, pour que les jumelles du
Communal arrivent à voler ses pommes. Il demande à {A} de « garder le verger ».

- **Garder le verger, très sérieusement** — **leçon** : Intrigue *(the twins win anyway)*
- **Aider les jumelles** — *Le cœur léger*
- **Lui dire qu'on a compris** — ❤️ le Tilleul *(« Chut. »)*

### a8 · « Les étiquettes de la Sauge » — la Sauge · new

La Sauge étiquette tout, et son écriture est illisible, même pour elle. Elle a besoin de quelqu'un
pour s'y retrouver.

- **Tout reconnaître à l'odeur** 🔒 passion Jardinage — **leçon** : Savoir
- **Goûter pour vérifier** — ❔ on verra bien
- **Tout réécrire en grosses lettres** — ❤️ la Sauge · 🪙 10

### a9 · « Le quatrième aux cartes » — la Courge, le Noyer, le Fou · new

À l'auberge, il manque un quatrième aux cartes. Le Noyer joue pour gagner. Le Fou joue pour compter
les cartes à voix haute.

- **Jouer pour gagner** — 🎲 Ruse 50 % : 🪙 20 · sinon *Grognon*
- **Faire gagner le Fou** — ❤️ le Fou
- **Raconter des histoires entre les donnes** — *En verve*

### a10 · « L'ardoise du Noyer » — la Courge, le Noyer · new

La plus longue ardoise de l'auberge est celle du Noyer. La Courge demande à {A}, très gentiment, de
le lui rappeler. Très gentiment.

- **Le lui rappeler avec un compliment** — 🎲 Diplomatie 50 % : 🪙 15 *(the Courge's thanks)* ·
  sinon **leçon** : Diplomatie *(an hour of him in the third person)*
- **Écrire le total à la craie sur sa porte, pour rire** — *Malicieux*
- **Laisser l'ardoise tranquille : c'est une plaisanterie locale** — *La conscience tranquille*

---

## Ancien only (10)

### E1 · « Le galet du tas » — la Mère Lichen · new

Devant la cabane de la Lisière, le tas de galets : un par pousse accueillie. Une pousse devenue
grande peut venir chercher le sien. {A} est devenu{a:|e} grand{a:|e}.
*Reads Arrival:* `[if keepsake lichen_pebble]` « Le tien est déjà dans ta poche. » · `[if epithet
lisiere_child]` « Tu sais où c'est. »

- **Prendre son galet** — 🎁 un galet
- **Le laisser, et s'asseoir à côté d'elle** — ❤️ la Mère Lichen
- **Ajouter un galet neuf, pour la prochaine** — ❔ on verra bien

### E2 · « Jacquet veut être cueilleur » — Jacquet, l'Étrangère · from `elder_student`

Jacquet, l'aîné de l'Ortie, vient chaque jour s'asseoir devant chez {A}. Aujourd'hui il ose : il
veut être cueilleur, et il veut que {A} lui apprenne.

- **Lui apprendre les champignons** — ❤️ l'Ortie *(her son comes home with a full basket, and
  proud)*
- **L'emmener chez l'Étrangère** — ❤️ l'Étrangère
- **Lui apprendre d'abord la patience** — ❔ on verra bien

### E3 · « La fondation » — le Tilleul · new

Le Tilleul est le dernier à connaître le récit de la fondation, et sa version change chaque hiver.
Il demande à {A} de l'aider à se souvenir.

- **Le raconter comme lui** — ❤️ le Tilleul
- **Corriger les détails** — **leçon** : Savoir
- **Ajouter un dragon** — ❔ on verra bien

*Reads* `tilleul.called_from_tree`: « Tu tombais de mon pommier, et maintenant tu me corriges. »

### E4 · « Le miel du mardi » — le Tilleul, la Mère Lichen · new

Le genou du Tilleul ne veut plus monter à la Lisière. Il confie à {A} le pot de miel qu'il porte
chaque semaine à la Mère Lichen. Lui seul l'appelle Ysaure.

- **Le porter sans rien dire** — ❤️ le Tilleul
- **Le porter, et rester s'asseoir avec elle** — ❤️ la Mère Lichen *(reads* `lichen.sat_quiet`*:
  she has already pulled out a second stone)*
- **Lui demander ce qu'il faut dire** — ❔ on verra bien *(« Dis-lui… non. Dis-lui que c'est du
  miel. »)*

What binds them stays unsaid (lore: no event settles it).

### E5 · « Les genoux savent » — la Sauge · from `elder_knees`

Les genoux de {A} annoncent la pluie. Ce matin, ils annoncent un orage. Le ciel est tout bleu, et la
Sauge hausse un sourcil.

- **Prévenir tout le hameau** — *En verve*
- **Comparer ses genoux avec ceux du Tilleul** — *Le cœur léger*
- **Prendre le baume de la Sauge** — *Apaisé* · ❤️ la Sauge

### E6 · « Quelqu'un qu'on vient voir » — Aude · new

On vient voir {A}, maintenant, pour un conseil. Ce matin, c'est Aude : comment dire à son père, pour
Mathurin ?
*Reads* `noyer.pumpkin_talk`: a fourth, secret option: « Commence par ses citrouilles. »

- **« Attends le printemps. »** — **leçon** : Diplomatie
- **« Tout le village est déjà de ton côté. »** — *Le cœur léger*
- **« Ça, c'est à toi de voir. »** — *La conscience tranquille*

Nothing is told to the Noyer: the village never changes.

### E7 · « Raconte ! » — les enfants de l'Ortie · from `elder_story`

Les quatre enfants de l'Ortie s'assoient en rond devant {A}. « Raconte quand tu étais petit{a:|e} ! »
Il en manque un. Toujours le même.

- **Embellir. Beaucoup.** — *En verve*
- **Raconter la vraie histoire, toute simple** — ❤️ l'Ortie *(she listens from the door)*
- **Commencer, et s'endormir au milieu** — *Bien reposé*

### E8 · « Pardon ? » — le Tilleul · from `elder_pardon`

{A} entend un peu moins bien. Au marché, {A} a demandé du miel, et reçu un chapeau.

- **Garder le chapeau, et le porter** — *Le cœur léger*
- **Se fabriquer un cornet acoustique** — **leçon** : Intendance
- **Aller voir le Tilleul, qui n'entend pas mieux** — ❤️ le Tilleul *(they shout happily at each
  other all afternoon)*

### E9 · « La poutre du moulin » — le Chardon · new

Au moulin, le Chardon grave sur une poutre le nom de chaque apprenti qu'il a eu, « pour ses
comptes ». {A} les lit, un par un.
*Reads* `chardon.kept_ducks`: a little duck is carved next to one name.

- **Lui demander de graver son nom** — ❤️ le Chardon
- **Compter les noms** — **leçon** : Intendance
- **Graver un petit cœur près du nom de Mathurin** 🔒 Fleur bleue — ❔ on verra bien *(the Chardon
  pretends not to see it, for years)*

### E10 · « Le gland » — Maître Frêne, les jumelles · from `elder_acorn`

{A} a ramassé un gland. Un chêne met cent ans à devenir grand.

- **Le planter au Communal** — ❤️ Maître Frêne *(he writes it in the register)*
- **Le donner aux jumelles** — *Le cœur léger*
- **Le garder dans sa poche** — 🎁 un gland

---

## Decisions for you

1. **Cuts and swaps.** Anything to drop or trade? Old premises left out and available: `grown_tin_box`,
   `grown_clover`, `grown_wish_oak`, `grown_teahouse`, `grown_golden_carp`, `grown_regatta`,
   `grown_secret_dance`, `grown_moon_party`, `elder_memoirs`, `elder_rocking_chair`, `elder_dance`,
   `elder_old_rival`.
2. **Rewrite count.** 18 here, 39 with wave 1, over the « best 20–30 ». Fine, or should some
   rewrites become new events?
3. **What stays drawn when adulte retires.** `social_declare`, `social_rival`, `social_pact`,
   `social_mend` and `grown_campfire` ask the other owner (declaring, pacts, making up). Story files
   can't do that yet, so they must stay drawn or events stop forming bonds. The old chains' openers
   (`grown_exam`, `grown_journey`, the master/dream echoes' parents…) need the same look. My pick:
   keep the five asking pulses, retire the rest.
4. **Retire adulte and ancien with this wave?** Same switch as bébé/ado. My pick: retire both in the
   release that ships the files, as you did for wave 1.
5. **Ancien-only share.** 10 of the ancien’s 40. Fewer (more shared) or more (a stage that feels
   different)?
6. **Aude and Mathurin in the cast** (open since wave 1): S11, E6 and E9 would let them remember
   directly.
