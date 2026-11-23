# Plynlings — fiche des événements d'après-visite

Fiche de relecture. **Validée par le propriétaire le 2026-11-23** (arcs 1 à 8, puis 9 à 18, demandés en plus pour tous les liens). Aucun fichier `.story` n'est écrit avant l'accord ;
une fois approuvée, chaque bloc est recopié tel quel dans `ProjectSYNCS/Stories/<Famille>/<clé>.story`.
Format : `docs/agents/plynling-storylines.md` ; ton : `docs/plynling-writing-style.md` et
`docs/village-lore.md`.

## Pourquoi

Après chaque visite, chacun des deux Plynlings a 33 % de chances (tiré une fois par jour) de recevoir un
événement `trigger: visit`. Il n'en existe qu'un : `forgot`, le mouchoir oublié. Avec 36 scènes de visite,
le mouchoir revient à chaque fois.

## Ce que permet le moteur (et ce qu'il ne permet pas)

- **`other: anyone`, obligatoire** : l'autre Plynling est celui de la visite, le moteur ne le filtre pas.
- **`requires: [relation …]` filtre sur le lien** tel qu'il est *après* la visite (le lien est sauvé avant
  que l'événement soit tiré). Les conditions s'ajoutent (`[relation friends]`, ou `[not relation enemies]`
  `[not relation rivals]`…). Une scène de couple ne tombe donc qu'à un couple.
- **Visiteur ou hôte, on ne sait pas** : l'événement peut tomber sur l'un ou l'autre (ou les deux, chacun
  de son côté). `{A}` est celui qui vit l'événement, `{B}` l'autre ; aucun texte ne dit qui est allé chez
  qui.
- Le tirage choisit, au poids, parmi les arcs éligibles. Tous pèsent 100, comme `forgot` : un couple
  tirera entre `love_note`, les onze arcs pour tous et `forgot`, un treizième chacun.
- **`cooldown: 7d`** sur chaque nouvel arc, pour qu'une paire qui se voit tous les jours ne retombe pas
  sur le même chaque semaine. `forgot` reste sans délai : c'est lui qui vient quand tout le reste attend.

## La liste

Dix-huit arcs courts, une scène chacun, toutes étapes (bébé à ancien), 3 ou 4 options, aucune marque (règle des
événements courts), aucun souvenir de villageois (aucun événement ne les relirait).

| # | Clé | Famille | Lien après la visite | En une ligne |
|---|---|---|---|---|
| 1 | `love_note` | Love | couple | Un petit dessin de {B}, plié dans une poche |
| 2 | `late_retort` | Rivalry | ennemis | La réplique parfaite, trouvée des heures trop tard |
| 3 | `rematch` | Rivalry | rivaux | Ce soir, on prépare la revanche |
| 4 | `same_tune` | Friendship | amis | Un air de {B} qui ne veut plus partir |
| 5 | `mixed_names` | Friendship | meilleurs amis | Le Tilleul appelle {A} « {B} », et l'inverse |
| 6 | `name_doubt` | Friendship | connaissances | Était-ce bien « {B} », à chaque fois ? |
| 7 | `courge_asks` | Personal | tous | « Alors, mon chou, cette visite ? » |
| 8 | `quiet_after` | Personal | tous | La visite est finie, et tout est très calme |
| 9 | `weaver_thread` | Village | tous | Deux fils côte à côte dans la bande de la tisserande |
| 10 | `slate_line` | Village | tous | « Un lait au miel, pour {B} » sur l'ardoise de {A} |
| 11 | `fou_threads` | Village | tous | Le Fou a vu la Trame s'allumer pendant la visite |
| 12 | `bee_news` | Village | tous | Il faut dire la visite aux abeilles, sinon elles boudent |
| 13 | `cap_spot` | Personal | tous | Une tache de la couleur de {B} sur le chapeau de {A} |
| 14 | `hungry_after` | Personal | tous | Une faim à manger le Pont-Vieux |
| 15 | `unsaid_list` | Personal | tous | Tout ce qu'on n'a pas dit, compté en glands |
| 16 | `wrong_word` | Personal | tous | Une phrase drôle sur le moment, beaucoup moins dans le noir |
| 17 | `dream_again` | Personal | tous | La visite en rêve, avec {B} de la taille du Pont-Vieux |
| 18 | `small_debt` | Personal | tous | Un petit service de {B}, une petite dette qui gratte |

**Ce qu'un lien tire après cette fiche** : son arc à lui, les douze arcs pour tous (`forgot`, 7, 8, et 9 à
18), soit 13 arcs par lien, contre 1 aujourd'hui. Les arcs pour tous n'ont rien qui suppose un lien : ils
tiennent entre ennemis comme en couple, et ne disent jamais qui est allé chez qui.

**Le stress** : 13 options sur 56 ont un coût (`shy`, `honest`, `greedy`, `temperate`, `impatient`,
`arrogant`), sur un choix contre ce trait ; 3 soulagent
(`[stress -10]` chez les canards et à la porte de {B}, `[stress -15]` pour la sieste). Chaque option montre au moins un effet
sur sa ligne d'enjeux (modificateur, leçon, stress), jamais l'affinité seule.

---

## 1. Le mot dans la poche (couple)

```
# arc: love_note
title: Le mot dans la poche
family: love
stage: baby, teen, adult, elder
trigger: visit
size: short
other: anyone
requires: [relation lovers]
cooldown: 7d

## scene: note
title: Le mot dans la poche
pace: now

Le soir, en vidant ses poches, {A} trouve une feuille de tilleul pliée en quatre. Dedans, un petit dessin de {B} : deux silhouettes et un soleil beaucoup trop grand.

* answer | Répondre par un dessin
  [lean sociability +1] [lean compassion +1]
  {A} dessine une réponse au charbon de bois : les deux silhouettes, le soleil, et cette fois une lune aussi, pour la nuit. Le dessin part le lendemain matin, glissé là où {B} le trouvera.
  [affinity +5] [grow diplomacy] [end]

* pillow | Le garder sous l'oreiller
  [lean energy -1] [lean zeal +1]
  {A} glisse le dessin sous l'oreiller et s'endort dessus. Les rêves, cette nuit-là, ont un soleil beaucoup trop grand.
  [modifier cherished] [end]

* show | Le montrer à la Courge
  [stress shy 15]
  [lean sociability +2]
  La Courge regarde le dessin, pose une vrille sur son cœur, et dit « oh, mon chou » quatre fois, de plus en plus fort. Le lendemain, tout le hameau sait qu'il y a un soleil trop grand dans la poche de {A}.
  [modifier party_soul] [end]
```

## 2. La réplique (ennemis)

```
# arc: late_retort
title: La réplique
family: rivalry
stage: baby, teen, adult, elder
trigger: visit
size: short
other: anyone
requires: [relation enemies]
cooldown: 7d

## scene: retort
title: La réplique
pace: now

Des heures après la visite, au beau milieu d'un bol de soupe, {A} trouve enfin la réplique parfaite, celle qu'il aurait fallu dire à {B}. Elle est piquante, elle est juste, et elle arrive bien trop tard.

* go | Aller la dire maintenant  [roll courage 3]
  [lean boldness +1] [lean vengefulness +1]
  ? success
    {A} traverse le hameau, trouve {B}, et dit la réplique, mot pour mot. {B} reste sans voix, puis rit, malgré tout. C'était vraiment une bonne réplique.
    [grow courage] [affinity +3] [end]
  ? failure
    {A} traverse le hameau, trouve {B}, ouvre la bouche, et ne se souvient plus de rien. Pas un mot. {A} repart très dignement.
    [modifier grumpy] [end]

* ducks | La dire aux canards du bief
  [lean sociability -1] [lean vengefulness -1]
  {A} va la dire aux canards du bief, avec le ton qu'il faut. Les canards la trouvent excellente. Ça ne règle rien, mais ça soulage beaucoup.
  [stress -10] [end]

* keep | La garder pour la prochaine fois
  [lean rationality +1] [lean vengefulness +1]
  {A} répète la réplique jusqu'à la savoir par cœur, et la range dans un coin, pour la prochaine fois. Il y aura une prochaine fois. Il y en a toujours une.
  [grow intrigue] [end]

* drop | Laisser tomber, ce n'est pas si grave
  [lean compassion +1] [lean vengefulness -2]
  {A} sourit à son bol de soupe, et laisse la réplique où elle est. Le lendemain, en croisant {B}, {A} n'y pense même plus.
  [affinity +5] [modifier clear_conscience] [end]
```

## 3. La revanche (rivaux)

```
# arc: rematch
title: La revanche
family: rivalry
stage: baby, teen, adult, elder
trigger: visit
size: short
other: anyone
requires: [relation rivals]
cooldown: 7d

## scene: rematch
title: La revanche
pace: now

La visite s'est jouée à pas grand-chose, comme toujours avec {B}. Le soir venu, {A} n'arrive pas à penser à autre chose : la prochaine fois, ce sera différent. La prochaine fois, {A} sera prêt{a:|e}.

* train | S'entraîner jusqu'à la nuit  [roll courage 3]
  [lean energy +2]
  ? success
    {A} s'entraîne jusqu'à la nuit au bord de la Sommeille : courir, sauter, lancer des galets. Le passeur regarde faire, et finit par compter les points.
    [grow courage] [modifier fired_up] [end]
  ? failure
    {A} s'entraîne jusqu'à la nuit, et glisse dans la boue au dernier saut. Retour crotté{a:|e} jusqu'aux oreilles, mais prêt{a:|e}, très prêt{a:|e}.
    [modifier muddy_paws] [end]

* spy | Observer l'autre en cachette
  [stress honest 10]
  [lean honor -1] [lean rationality +1]
  {A} observe {B} depuis une haie, toute une après-midi, et note tout : la façon de marcher, de rire, de manger une pomme. On ne sait pas encore à quoi ça servira. Mais c'est noté.
  [grow intrigue] [modifier sly] [end]

* admit | Reconnaître que c'était bien joué
  [lean honor +1] [lean compassion +1]
  {A} va le dire à {B}, tout simplement : c'était bien joué. {B} ne sait pas quoi répondre. C'est une première victoire, d'une autre sorte.
  [affinity +5] [grow diplomacy] [end]
```

## 4. L'air qui reste (amis)

```
# arc: same_tune
title: L'air qui reste
family: friendship
stage: baby, teen, adult, elder
trigger: visit
size: short
other: anyone
requires: [relation friends]
cooldown: 7d

## scene: tune
title: L'air qui reste
pace: now

Le lendemain de la visite, {A} se surprend à fredonner un air que {B} chantonnait sans y penser. Il revient au puits, au four, en se lavant les mains. Impossible de s'en débarrasser.

* hum | Le fredonner partout, tant pis
  [lean sociability +1] [lean energy +1]
  {A} le fredonne partout, et à midi, la tisserande le fredonne aussi, puis Pain-Blond, puis la moitié de la place. Personne ne sait d'où il vient. {A}, si.
  [modifier light_heart] [end]

* origin | Demander au Tilleul d'où il vient
  [lean rationality +2]
  Le Tilleul reconnaît l'air dès la troisième note : une chanson de la fondation, dit-il, ou du grand hiver, ou des deux. Il en chante quatre couplets, tous différents de la fois d'avant.
  [grow learning] [end]

* sing | Aller le chanter en entier
  [stress shy 10]
  [lean compassion +1] [lean boldness +1]
  {A} va chanter l'air à {B}, en entier, avec des paroles inventées en route. {B} rit, reconnaît l'air, et ne se souvient pas de l'avoir chanté. Maintenant, il est à {A} et à {B}.
  [affinity +5] [modifier inspired] [end]
```

## 5. Les prénoms mélangés (meilleurs amis)

```
# arc: mixed_names
title: Les prénoms mélangés
family: friendship
stage: baby, teen, adult, elder
trigger: visit
size: short
other: anyone
requires: [relation bestfriends]
cooldown: 7d

## scene: names
title: Les prénoms mélangés
pace: now

Depuis cette visite, le Tilleul appelle {A} « {B} ». Il appelle aussi {B} « {A} ». Il s'excuse chaque fois avec une pomme, puis recommence. Ça commence à se savoir dans tout le hameau.

* answer | Répondre quand même, avec le sourire
  [lean compassion +1] [lean energy -1]
  {A} répond quand même, avec le sourire. Le Tilleul est ravi, et donne une pomme pour la peine. Le soir, {A} a cinq pommes et un deuxième prénom.
  [modifier light_heart] [end]

* correct | Le corriger, gentiment
  [lean honor +1] [lean rationality +1]
  {A} corrige le Tilleul, gentiment, en articulant. Il hoche la tête, s'excuse avec une pomme, et dit : « Vous êtes comme les deux doigts d'une main. » Il ne précise pas lesquels.
  [grow diplomacy] [end]

* tell | Courir le raconter
  [lean sociability +1] [lean energy +1]
  {A} court le raconter à {B}, qui a eu droit au même traitement, et à autant de pommes. On fait un tas avec toutes les pommes, et on décide que, pour le Tilleul, ça ne change rien. Pour {A} et {B} non plus.
  [affinity +5] [modifier cherished] [end]
```

## 6. Le prénom (connaissances)

```
# arc: name_doubt
title: Le prénom
family: friendship
stage: baby, teen, adult, elder
trigger: visit
size: short
other: anyone
requires: [relation acquaintances]
cooldown: 7d

## scene: doubt
title: Le prénom
pace: now

Sur le chemin du retour, {A} a un doute affreux : pendant la visite, était-ce bien « {B} » à chaque fois ? Une fois au moins, il y avait une syllabe de trop. {A} en est presque sûr{a:|e}.

* courge | Demander à la Courge, qui connaît tout le monde
  [lean sociability +2]
  La Courge connaît le prénom de {B}, ceux de ses voisins, et ce que {B} a mangé mardi. Elle le répète trois fois, pour que ça rentre. Ça rentre.
  [modifier well_spoken] [end]

* practice | Répéter le prénom jusqu'à le savoir
  [lean rationality +1] [lean energy -1]
  {A} répète « {B}, {B}, {B} » tout le long du chemin, au point que les grenouilles des Mouilles le savent aussi. Ce prénom-là, {A} ne l'oubliera plus jamais.
  [grow learning] [end]

* ask | Aller le lui demander, franchement
  [stress shy 15]
  [lean honor +1] [lean boldness +1]
  {A} revient sur ses pas et demande, franchement. {B} éclate de rire : pendant toute la visite, {B} se posait exactement la même question. On repart chacun avec le bon prénom.
  [affinity +5] [modifier clear_conscience] [end]
```

## 7. « Alors, cette visite ? » (tous les liens)

```
# arc: courge_asks
title: « Alors, cette visite ? »
family: personal
stage: baby, teen, adult, elder
trigger: visit
size: short
other: anyone
cooldown: 7d

## scene: asks
title: « Alors, cette visite ? »
pace: now

Le lendemain, la Courge attrape {A} par la manche au milieu de la place. « Alors, mon chou, cette visite avec {B} ? Raconte. Non, attends, je devine. » Elle ne devine pas. Elle attend, toutes vrilles frémissantes.

* truth | Raconter la vérité, simplement
  [lean honor +2]
  {A} raconte tout, simplement, sans rien ajouter. La Courge est presque déçue, puis plus du tout : à midi, sur la place, elle en a fait une épopée, et {A} y a le beau rôle.
  [modifier clear_conscience] [end]

* embellish | En rajouter un peu
  [stress honest 15]
  [lean honor -1] [lean sociability +1]
  {A} en rajoute un peu, puis beaucoup : une averse, un canard héroïque, un pont qui tangue. La Courge boit chaque mot, et en sert le soir même une version encore plus belle, avec deux canards.
  [modifier party_soul] [end]

* mystery | Faire mystère
  [lean sociability -1] [lean rationality +1]
  {A} sourit et ne dit rien. Rien du tout. La Courge en reste muette, ce qui ne lui arrive jamais, puis invente toute seule une histoire bien plus belle que la vraie.
  [modifier sly] [end]
```

## 8. Le calme d'après (tous les liens)

```
# arc: quiet_after
title: Le calme d'après
family: personal
stage: baby, teen, adult, elder
trigger: visit
size: short
other: anyone
cooldown: 7d

## scene: quiet
title: Le calme d'après
pace: now

La visite avec {B} est finie, et tout est soudain très calme. Il reste deux bols sur la table, un rire qui traîne encore dans l'air, et une grande envie de ne plus rien faire du tout.

* nap | Faire une longue sieste
  [lean energy -2]
  {A} s'allonge « juste un instant » et se réveille à la nuit tombée, reposé{a:|e} comme après une semaine de pluie.
  [stress -15] [modifier well_rested] [end]

* tidy | Ranger en repensant à la visite
  [lean rationality +1] [lean energy +1]
  {A} lave les deux bols en repensant à chaque moment de la visite, dans l'ordre. Le soir, la table est propre, et la journée bien rangée aussi.
  [grow stewardship] [end]

* out | Ressortir aussitôt voir du monde
  [stress shy 10]
  [lean sociability +2]
  {A} ressort aussitôt, file à l'auberge, et raconte la visite à qui veut l'entendre. Ça fait une deuxième visite, en quelque sorte, avec plus de monde.
  [grow diplomacy] [end]
```

## 9. Le fil de la tisserande (tous les liens)

```
# arc: weaver_thread
title: Le fil de la tisserande
family: village
stage: baby, teen, adult, elder
trigger: visit
size: short
other: anyone
cooldown: 7d

## scene: thread
title: Le fil de la tisserande
pace: now

Au bord de la place, la tisserande tisse une bande qui n'en finit pas, avec un fil pour chaque chose qu'elle voit passer. Ce matin, {A} y reconnaît deux fils côte à côte : la couleur de son chapeau, et celle du chapeau de {B}. « La visite », dit-elle, sans lever les yeux.

* ask | Demander ce que raconte le reste de la bande
  [lean rationality +1] [lean sociability +1]
  La tisserande montre du doigt : là, le grand hiver ; là, la foire de l'an dernier ; là, une chèvre qui a mangé un chapeau. Tout le village tient dans sa bande. Maintenant, {A} aussi.
  [grow learning] [end]

* remove | Lui demander d'enlever ces deux fils
  [lean sociability -1] [lean boldness +1]
  La tisserande hausse les épaules et tire les deux fils d'un coup sec. Le lendemain, il y a un trou à cet endroit de la bande, et tout le hameau vient demander ce qui s'est passé là.
  [grow intrigue] [end]

* add | Ajouter un troisième fil, d'une couleur à soi
  [lean zeal +1] [lean compassion +1]
  {A} choisit une couleur qui n'est à personne, et la tisserande la passe entre les deux autres sans un mot. Le soir, il y a dans la bande un petit nœud de trois couleurs. Elle dit que c'est rare.
  [affinity +3] [modifier inspired] [end]
```

## 10. La ligne sur l'ardoise (tous les liens)

```
# arc: slate_line
title: La ligne sur l'ardoise
family: village
stage: baby, teen, adult, elder
trigger: visit
size: short
other: anyone
cooldown: 7d

## scene: slate
title: La ligne sur l'ardoise
pace: now

À l'auberge, une ligne est apparue sur l'ardoise de {A} depuis la visite : « un lait au miel, pour {B} ». L'écriture ressemble beaucoup à celle de {B}. Elle ressemble aussi un peu à celle des enfants de la Courge, qui rient très fort derrière le comptoir.

* pay | Payer, sans rien dire
  [stress greedy 15]
  [lean greed -2]
  {A} pose un caillou sur le comptoir et efface la ligne. Le soir même, {B} boit un lait au miel sans savoir d'où il vient, et {A} le regarde boire du fond de la salle, l'air de rien.
  [affinity +3] [modifier light_heart] [end]

* reply | Ajouter une ligne sur l'ardoise de {B}
  [lean boldness +1] [lean vengefulness +1]
  {A} prend la craie et écrit, sur l'ardoise de {B} : « deux laits au miel, pour {A} ». Le lendemain, il y en a trois sur la sienne. L'affaire promet de durer longtemps.
  [modifier sly] [end]

* kids | Questionner les enfants de la Courge  [roll intrigue 3]
  [lean rationality +1]
  ? success
    {A} pose trois questions, l'air de rien, et les enfants de la Courge avouent à la quatrième : c'était pour voir. Ils effacent la ligne, et offrent le lait au miel pour la peine.
    [grow intrigue] [end]
  ? failure
    Les trois enfants de la Courge jurent, la main sur le cœur, qu'ils n'ont rien vu. Ils jurent si fort, et si longtemps, que {A} n'en saura jamais rien.
    [modifier distracted] [end]
```

## 11. Ce qu'a vu le Fou (tous les liens)

```
# arc: fou_threads
title: Ce qu'a vu le Fou
family: village
stage: baby, teen, adult, elder
trigger: visit
size: short
other: anyone
cooldown: 7d

## scene: threads
title: Ce qu'a vu le Fou
pace: now

Le Fou arrête {A} sur le Pont-Vieux, très sérieux. Pendant la visite avec {B}, il passait à la Lisière, et il l'a vu : sous la mousse, un fil de la Trame s'est allumé, juste un instant. « Ça veut dire quelque chose », dit-il. Il ne sait pas quoi.

* look | Aller voir à la Lisière, la nuit venue  [roll courage 3]
  [lean boldness +1] [lean zeal +1]
  ? success
    {A} attend à la Lisière jusqu'à la nuit noire, le nez dans la mousse. Rien, puis, peut-être, une lueur très pâle, le temps d'un battement de cils. {A} ne le dira à personne. Sauf au Fou.
    [grow courage] [modifier inspired] [end]
  ? failure
    {A} attend à la Lisière jusqu'à la nuit noire, puis un craquement dans les fougères, et {A} rentre au hameau au pas de course. C'était un hérisson. Très ordinaire, mais très soudain.
    [modifier on_edge] [end]

* explain | Lui expliquer que c'était juste une visite
  [lean rationality +2]
  {A} explique, patiemment : une visite, c'est deux Plynlings, une table, et rien qui brille. Le Fou écoute, hoche la tête, et répond : « Justement. » Puis il s'en va, enchanté de la conversation.
  [grow learning] [end]

* tell | Courir le raconter à {B}
  [lean sociability +1] [lean energy +1]
  {A} court le raconter à {B}, qui écoute avec des yeux ronds, éclate de rire, puis ne rit plus du tout. Le soir, chacun de son côté, {A} et {B} regardent un peu la mousse, au cas où.
  [affinity +5] [modifier light_heart] [end]
```

## 12. Le dire aux abeilles (tous les liens)

```
# arc: bee_news
title: Le dire aux abeilles
family: village
stage: baby, teen, adult, elder
trigger: visit
size: short
other: anyone
cooldown: 7d

## scene: bees
title: Le dire aux abeilles
pace: now

Le Tilleul arrête {A} devant son clos. C'est la coutume : toute nouvelle se dit aux abeilles, sinon elles boudent. « Et la visite avec {B}, personne ne leur en a parlé. » Les ruches, en effet, ont l'air vexées.

* all | Tout leur raconter, de bout en bout
  [lean honor +1] [lean sociability +1]
  {A} s'assoit devant les ruches et raconte la visite de bout en bout, sans rien oublier. Les abeilles bourdonnent plus doucement. Le Tilleul, qui écoutait aussi, essuie une larme et dit que c'était sa partie préférée.
  [modifier soothed] [end]

* whisper | Le leur chuchoter, pour que personne n'entende
  [lean sociability -1] [lean rationality +1]
  {A} se penche vers la ruche et chuchote, la main en cornet. Les abeilles écoutent, très attentives. Le Tilleul, lui, n'a rien entendu du tout, et boude un peu à son tour.
  [grow stewardship] [end]

* leave | S'en aller : ce ne sont que des abeilles
  [lean rationality +1] [lean honor -1]
  {A} s'en va. Une abeille suit, jusqu'au puits, jusqu'à la porte, jusqu'au lit, en bourdonnant d'un ton de reproche. Elle ne pique pas. Elle bourdonne. Toute la nuit.
  [modifier sleepless] [end]
```

## 13. La tache sur le chapeau (tous les liens)

```
# arc: cap_spot
title: La tache sur le chapeau
family: personal
stage: baby, teen, adult, elder
trigger: visit
size: short
other: anyone
cooldown: 7d

## scene: spot
title: La tache sur le chapeau
pace: now

Le lendemain de la visite, {A} découvre une petite tache sur son chapeau, exactement de la couleur de celui de {B}. Elle ne part ni à l'eau, ni au savon, ni en frottant très fort.

* sauge | Courir chez la Sauge
  [lean rationality +1] [lean energy +1]
  La Sauge examine la tache longtemps, de très près, puis rend son verdict : les champignons qui se fréquentent déteignent un peu les uns sur les autres. « Ça passe. Ou pas. Une tisane, en attendant ? »
  [grow learning] [modifier soothed] [end]

* proud | La montrer à tout le monde
  [stress shy 10]
  [lean sociability +1] [lean boldness +1]
  {A} se promène le chapeau bien haut. À midi, la moitié du hameau a son avis sur la tache, et l'autre moitié sur {B}. {A} répond à tout le monde avec le même sourire.
  [grow diplomacy] [end]

* hide | La cacher sous une feuille
  [lean boldness -1] [lean rationality +1]
  {A} cache la tache sous une feuille de chêne, très bien ajustée. Personne ne remarque la tache. Tout le monde remarque la feuille.
  [modifier distracted] [end]

* compare | Aller comparer avec {B}
  [lean sociability +1] [lean compassion +1]
  {A} va voir {B}, et la surprise est double : {B} a la même tache, de la couleur du chapeau de {A}. On se regarde un long moment, et on décide que ça ne regarde personne.
  [affinity +5] [modifier light_heart] [end]
```

## 14. La faim d'après (tous les liens)

```
# arc: hungry_after
title: La faim d'après
family: personal
stage: baby, teen, adult, elder
trigger: visit
size: short
other: anyone
cooldown: 7d

## scene: hunger
title: La faim d'après
pace: now

Le soir de la visite, {A} a une faim de moisson, une faim à manger le Pont-Vieux, pierres comprises. Il paraît que voir {B}, ça creuse. Le garde-manger, lui, n'a pas l'air d'être au courant.

* feast | Tout manger, jusqu'à la dernière noix
  [stress temperate 10]
  [lean greed +1] [lean energy +1]
  {A} mange tout : le pain, la confiture, trois noix, et un bout de fromage dont personne ne réclamera jamais la propriété. Le ventre rond, la tête lourde, la nuit est excellente.
  [modifier hearty] [end]

* share | Partager avec un voisin
  [lean compassion +1] [lean sociability +1]
  {A} frappe chez un voisin avec une miche, et le voisin, qui avait faim aussi, sort une soupe. On mange à deux ce qu'on aurait mangé seul, et c'est meilleur.
  [grow diplomacy] [end]

* wait | Attendre le petit-déjeuner, dignement
  [stress impatient 10]
  [lean energy -1] [lean honor +1]
  {A} se couche le ventre vide, très dignement. Au matin, le petit-déjeuner a un goût de fête, et {A} le mange très, très lentement.
  [grow stewardship] [end]
```

## 15. Ce qu'on n'a pas dit (tous les liens)

```
# arc: unsaid_list
title: Ce qu'on n'a pas dit
family: personal
stage: baby, teen, adult, elder
trigger: visit
size: short
other: anyone
cooldown: 7d

## scene: unsaid
title: Ce qu'on n'a pas dit
pace: now

Le soir, {A} s'aperçoit qu'il reste plein de choses à dire à {B}. Une, puis deux, puis tant qu'il faut les compter avec des glands, alignés sur le rebord de la fenêtre. À minuit, il y a plus de glands que de rebord.

* pouch | Les garder dans une bourse, pour la prochaine fois
  [lean rationality +1] [lean zeal +1]
  {A} range les glands dans une bourse, un par chose à dire, et la pose près de la porte. La prochaine visite sera longue, et la bourse est lourde.
  [grow stewardship] [end]

* go | Aller tout dire, tout de suite
  [lean boldness +1] [lean energy +1]
  {A} traverse le hameau dans la nuit, la bourse à la main, et trouve {B} endormi{b:|e}. {A} dit toutes les choses à la porte, tout bas, un gland à la fois, et rentre se coucher, {a:léger|légère}.
  [stress -10] [end]

* river | Les jeter dans la Sommeille, un par un
  [lean rationality +1] [lean sociability -1]
  {A} jette les glands dans la Sommeille, un par un, du haut du Pont-Vieux. Les choses importantes reviendront bien. Les autres font de jolis ronds dans l'eau.
  [modifier soothed] [end]
```

## 16. La phrase de trop (tous les liens)

```
# arc: wrong_word
title: La phrase de trop
family: personal
stage: baby, teen, adult, elder
trigger: visit
size: short
other: anyone
cooldown: 7d

## scene: word
title: La phrase de trop
pace: now

En pleine nuit, {A} se réveille avec une phrase en tête : une phrase dite à {B} pendant la visite. Sur le moment, elle semblait drôle. Dans le noir, elle semble beaucoup moins drôle.

* sorry | Aller s'excuser dès l'aube
  [stress arrogant 15]
  [lean honor +1] [lean compassion +1]
  Dès l'aube, {A} va trouver {B} et s'excuse, d'une traite. {B} ne se souvient pas de la phrase. Pas du tout. {B} demande même à l'entendre, pour voir, et la trouve plutôt drôle.
  [affinity +3] [grow diplomacy] [end]

* sleep | Décider que ce n'était rien, et se rendormir
  [lean energy -1] [lean rationality +1]
  {A} décide que ce n'était rien, se retourne, et se rendort. Ce n'était rien, en effet. Il y a des nuits où le noir exagère.
  [modifier well_rested] [end]

* turn | La retourner dans tous les sens jusqu'au matin
  [lean rationality +1] [lean zeal +1]
  {A} retourne la phrase dans tous les sens jusqu'au matin : trop longue, trop courte, mal placée. À l'aube, elle n'a plus aucun sens, ni dans un sens, ni dans l'autre.
  [modifier sleepless] [end]
```

## 17. Le rêve de la visite (tous les liens)

```
# arc: dream_again
title: Le rêve de la visite
family: personal
stage: baby, teen, adult, elder
trigger: visit
size: short
other: anyone
cooldown: 7d

## scene: dream
title: Le rêve de la visite
pace: now

Cette nuit, {A} refait la visite en rêve. Tout est pareil, sauf que {B} est grand{b:|e} comme le Pont-Vieux, ne parle qu'en grenouille, et tient absolument à servir le thé.

* back | Se rendormir pour voir la fin
  [lean energy -2]
  {A} referme les yeux très fort et retrouve le rêve là où il s'était arrêté. La fin est excellente. Au réveil, {A} ne se souvient de rien, sauf qu'elle était excellente.
  [modifier well_rested] [end]

* tell | Raconter le rêve à {B}
  [stress shy 10]
  [lean sociability +1] [lean boldness +1]
  {A} raconte le rêve à {B}, grenouille comprise, en imitant. {B} tient bon jusqu'au thé, puis rit si fort qu'il faut s'asseoir.
  [affinity +5] [modifier light_heart] [end]

* draw | Le dessiner avant qu'il s'en aille
  [lean zeal +1] [lean energy +1]
  Au réveil, {A} dessine le rêve au charbon, sur le mur, avant qu'il s'en aille : le pont, la grenouille, le thé. Le dessin est raté, mais le pont est très ressemblant.
  [modifier inspired] [end]
```

## 18. La petite dette (tous les liens)

```
# arc: small_debt
title: La petite dette
family: personal
stage: baby, teen, adult, elder
trigger: visit
size: short
other: anyone
cooldown: 7d

## scene: debt
title: La petite dette
pace: now

Pendant la visite, {B} a rendu un petit service à {A}, sans même y penser : une porte tenue, une miette chassée d'un chapeau. Depuis, {A} a une petite dette, et elle gratte comme une écharde.

* honey | La rendre tout de suite, avec un pot de miel
  [stress greedy 15]
  [lean greed -1] [lean honor +1]
  {A} porte un pot de miel à {B}, en expliquant que c'est pour la miette. {B} ne se souvient d'aucune miette, mais garde le miel. Les comptes sont bons.
  [affinity +3] [modifier clear_conscience] [end]

* secret | La rendre en secret  [roll intrigue 3]
  [lean rationality +1] [lean sociability -1]
  ? success
    {A} rend le service en cachette : un seuil balayé à l'aube devant chez {B}, sans témoin. {B} cherche qui pendant une semaine, et soupçonne tout le hameau, sauf {A}.
    [grow intrigue] [modifier sly] [end]
  ? failure
    {A} balaie le seuil de {B} à l'aube, en cachette, et {B} ouvre la porte au troisième coup de balai. On se regarde. {A} continue à balayer, très naturellement.
    [modifier tense] [end]

* even | Décider qu'on est quittes
  [lean rationality +1] [lean greed +1]
  {A} refait le compte de la visite, longuement, et finit par trouver un service rendu à {B} : un tabouret avancé, ou un silence au bon moment. On est quittes. La dette ne gratte presque plus.
  [modifier trade_sense] [end]
```

## Questions pour la relecture

Arcs 1 à 8 : validés (délai de 7 jours gardé, dessin gardé). Arcs 9 à 18 : validés tels quels ; les questions ci-dessous restent à leur valeur par défaut (aucun `weight:` ajouté, tout reste à 100).

- **Le poids des arcs de lien.** Avec douze arcs pour tous, l'arc propre à un lien (`love_note`,
  `late_retort`…) ne sort plus qu'une fois sur treize après une visite. Je propose `weight: 300` sur les six
  arcs de lien (1 à 6), pour qu'ils sortent environ une fois sur cinq : c'est une ligne d'en-tête ajoutée à
  des arcs déjà validés, d'où la question. Sinon, tout reste à 100.
- **Six options qui tournent mal, gentiment** : `bee_news` (une abeille vexée, `sleepless`), `wrong_word`
  (`sleepless` si on rumine), `cap_spot` (la feuille, `distracted`), et les échecs de `slate_line`
  (`distracted`), `fou_threads` (`on_edge`) et `small_debt` (`tense`). Rien de méchant, mais ce sont des
  modificateurs négatifs à un moment qui en avait peu.
- **`slate_line` chez un bébé** : tout le monde a une ardoise à l'auberge (lore § 7), bébés compris.
  C'est voulu comme une petite absurdité ; dis-moi si ça te gêne.
