# Expéditions Plynling — fiche d'écriture de la Combe aux Mousses

Fiche de relecture, **approuvée telle quelle le 2026-11-23**, questions de relecture comprises (laissées
par défaut). Chaque bloc est recopié tel quel dans `ProjectSYNCS/Stories/Expeditions/combe.place`. Toute
retouche d'un texte repasse par le propriétaire. Conception : `docs/superpowers/specs/2026-11-23-combe-aux-mousses-design.md`
(le lieu) et `2026-11-23-expedition-engine-design.md` (le moteur). Ton et règles :
`docs/plynling-writing-style.md`, `docs/village-lore.md`.

## Ce que contient la fiche

| Genre | Scènes | Clés |
|---|---|---|
| Arrivée (texte au-dessus de la 1re scène) | 3 | `arrive_lisiere`, `arrive_fog`, `arrive_rain` |
| Rencontres | 9 (+2 suites de bagarre) | `toad`, `moths`, `whistle_heard`, `whistle_follow`, `hedgehog` (+`hedgehog_b`), `beetle` (+`beetle_b`), `ferret`, `snail`, `badger` |
| Trouvailles | 3 | `find_trunk`, `find_ring`, `find_root` |
| Obstacles | 4 | `stream`, `slope`, `bramble`, `fog` |
| Le fil : *La source qui plonge* | 3 | `source_1`, `source_2`, `source_3` |
| Retour (texte au-dessus du bilan) | 3 | `return_1`, `return_2`, `return_3` |
| Sauvetage (Asha) | 1 | `rescue` |

**Les créatures et leurs étapes** (👁️ Aperçu · 🤝 Approché · 💚 Apprivoisé) :

| Créature | 👁️ | 🤝 | 💚 |
|---|---|---|---|
| Crapaud-Mousse | Intelligence | Endurance | Agilité |
| Phalènes-Lanternes | sans dé | Endurance | au bout du fil |
| Siffleur | sans dé (on l'entend) | Agilité (on le voit enfin) | Intelligence |
| Hérisson-Châtaigne | Agilité | Courage puis Endurance (la bagarre) | Agilité, facile |
| Lucane-Écorce | sans dé | Force puis Force (la poussée) | Intelligence |
| Furet des Fougères | Intelligence | Vitesse | sans dé : on lui donne la trouvaille |
| Escargot-Cloche | sans dé | Endurance (et une trouvaille) | Intelligence |
| Vieux Blaireau gris | sans dé | Courage | sans dé : le salut de l'Étrangère |

**Les astuces**, pour vérifier qu'aucune ne revient : une pierre qui respire ; des lueurs qui se posent ;
un sifflet qui répond ; une châtaigne qui roule ; un concours de poussée ; un voleur qu'on laisse gagner ;
une coquille qui tinte ; un vieux qui s'assoit sur les malpolis ; ramper sous un tronc ; un cercle de
champignons qui se ressemblent tous ; une racine au-dessus de l'eau ; quatre façons de passer ; une rivière
qu'on suit sous la terre.

**Villageois** : l'Étrangère (le sauvetage, et son salut que connaît le Blaireau) ; Pain-Blond, cité une fois (le scarabée « de la taille d'un pain de Pain-Blond »). Rien ne dit d'où
elle vient. La Souche et la Trame ne sont jamais nommées ; la lueur des Phalènes n'est rapprochée de rien.

## Comment lire un bloc

- `{A}` est le Plynling. Un mot qui s'accorde avec lui s'écrit `{a:m|f}` (« {a:trempé|trempée} »).
- `* clé | Libellé du bouton` : une option, 80 caractères au plus. Puis ses mots :
  - `[roll Stat n]` : un test contre la difficulté n. Stats du corps : Force, Agilité, Endurance,
    Intelligence, Vitesse ; parfois Courage. Pour fixer les idées, au niveau 1 une stat du corps vaut 3 à 5 :
    difficulté 3 ≈ 55–65 %, 5 ≈ 45–50 %, 7 ≈ 35–40 %.
  - `[needs step créature étape]` : l'option n'apparaît que si la personne a déjà cette étape.
  - `[if no step créature étape]` : seulement si elle ne l'a pas encore.
  - `[needs unlock clé]` (le fil), `[needs trait clé]` (montrée avec l'icône du trait), `[if found]` /
    `[if not found]` (une trouvaille déjà faite pendant cette expédition).
- `? win` et `? lose` : ce qui arrive selon le dé ; `? done` pour une option sans dé. Les crochets en fin de
  ligne disent l'effet :
  - `[wound]` : une égratignure ; au deuxième, c'est le sauvetage ;
  - `[step créature étape]` : une étape du carnet ;
  - `[find]` : la trouvaille de l'expédition (une seule par sortie), et `[lose find]` ;
  - `[dirt]` : crotté, un peu d'hygiène en moins ;
  - `[cailloux n]`, `[stress n]` ;
  - `[unlock clé]`, `[epithet clé]`, `[moment]` (une ligne de journal) ;
  - `[-> scène]` : la suite immédiate (les bagarres en deux temps).
- **Chaque scène a au moins une option sans dé**, qui ne coûte rien.
- **Une scène n'apparaît que si elle a quelque chose à offrir** : une rencontre dont les trois étapes sont
  faites se fait plus rare, une trouvaille ne tombe qu'avant la trouvaille du jour, le fil ne tombe qu'à son
  étape.

---

## Arrivées

Une seule, tirée au hasard, en tête de la première scène.

```
## arrival: arrive_lisiere
{A} passe les derniers arbres de la Lisière. Trois pas plus loin, le bruit du village s'éteint d'un coup, comme une porte qu'on ferme. Il ne reste que l'eau qui goutte des fougères, et une odeur de cave après la pluie. Bienvenue dans la Combe aux Mousses.
```

```
## arrival: arrive_fog
Ce matin, la brume est descendue jusqu'au fond de la Combe et n'en est pas remontée. {A} y entre comme on entre dans une tasse de lait. Les troncs apparaissent un à un, tout près, puis disparaissent derrière {a:lui|elle}.
```

```
## arrival: arrive_rain
Il a plu cette nuit, et toute la Combe brille. Chaque mousse est une éponge, chaque feuille garde sa goutte. {A} n'a pas fait dix pas qu'{a:il est déjà trempé|elle est déjà trempée} jusqu'au chapeau, et {a:il|elle} trouve ça très bien.
```

---

## Rencontres

### 1. La pierre qui respire (Crapaud-Mousse)

```
## scene: toad
kind: encounter toad
title: La pierre qui respire

Au bord du ruisseau, une pierre ronde, couverte de mousse. Une très belle pierre. {A} est presque {a:sûr|sûre} de l'avoir vue bouger.

* look | Regarder la pierre de plus près  [roll Intelligence 4] [if no step toad seen]
  ? win  [step toad seen]
  La pierre a des yeux. Deux yeux dorés, à moitié fermés, qui ne s'intéressent pas du tout à {A}. C'est un crapaud, si vieux et si tranquille que la mousse a poussé sur son dos.
  ? lose  [dirt]
  {A} s'assoit sur la pierre pour mieux réfléchir. La pierre pousse un « crôa » indigné et file dans le ruisseau. Plouf. {A} aussi, un peu.

* sit | S'asseoir à côté de lui sans bouger  [roll Endurance 5] [needs step toad seen] [if no step toad close]
  ? win  [step toad close]
  {A} s'assoit. Ne bouge plus. Longtemps. Une fourmi lui passe sur le pied. Enfin, le crapaud cligne d'un œil, lentement, vers {a:lui|elle}. Chez les crapauds, c'est presque une poignée de main.
  ? lose
  Au bout d'un moment, le nez de {A} le chatouille. Le crapaud, qui a horreur des éternuements, s'en va bouder plus loin.

* fern | Ôter la fougère tombée sur son dos  [roll Agilité 5] [needs step toad close] [if no step toad tamed]
  ? win  [step toad tamed]
  Doucement, du bout des doigts, {A} soulève la fougère. Le crapaud se laisse faire. Puis, ce qui ne s'était jamais vu, il fait un pas vers {a:lui|elle}, et se rendort contre son pied.
  ? lose
  La fougère casse et retombe pile sur l'œil du crapaud. Il la garde, très digne, comme un chapeau qu'il aurait choisi.

* leave | Le laisser méditer
  ? done
  {A} le laisse à ses pensées de pierre. Il y en a sûrement beaucoup.
```

### 2. Les lueurs entre les troncs (Phalènes-Lanternes)

```
## scene: moths
kind: encounter moths
title: Les lueurs entre les troncs

Là où la Combe est la plus sombre, entre deux troncs serrés, quelque chose luit. Des points verts, très doux, qui montent et descendent comme une respiration.

* glow | S'approcher des lueurs  [if no step moths seen]
  ? done  [step moths seen]
  Ce sont des papillons de nuit. Des dizaines. Leurs ailes luisent d'un vert pâle, comme une lanterne sous un torchon. Ils dansent sans un bruit.

* still | Rester immobile, les bras tendus  [roll Endurance 6] [needs step moths seen] [if no step moths close]
  ? win  [step moths close]
  {A} tend les bras et ne respire plus. Un papillon se pose sur sa main. Puis un deuxième, sur son chapeau. {A} brille un peu, à son tour, le temps de compter jusqu'à dix.
  ? lose
  Un papillon se pose sur le nez de {A}. Le nez n'est pas d'accord. Atchoum. Toute la nuée s'envole et se rallume un peu plus loin.

* watch | Regarder de loin
  ? done
  {A} les regarde danser un long moment. Personne au village ne {a:le|la} croira. Tant pis.
```

### 3. Le sifflet dans les fougères (le Siffleur, première fois)

```
## scene: whistle_heard
kind: encounter whistler
title: Le sifflet dans les fougères

Trois notes montent des fougères. Une haute, deux basses. Puis plus rien. Puis les trois mêmes, un peu plus loin.

* answer | Siffler les trois notes en retour  [if no step whistler seen]
  ? done  [step whistler seen]
  {A} siffle les trois notes. Silence. Puis, des fougères, les trois notes reviennent, et une quatrième en prime, comme une question. Quelqu'un, là-dedans, a de la conversation.

* rush | Plonger dans les fougères pour voir qui c'est  [roll Vitesse 5]
  ? win  [cailloux 3]
  {A} plonge. Les fougères se referment, une petite ombre file, et il ne reste qu'un caillou brillant là où elle était. Un cadeau, ou un oubli.
  ? lose  [wound]
  {A} plonge tête la première dans les fougères, et dans la ronce qui se cachait dessous. Aïe. Le sifflet, lui, rit en trois notes.

* listen | Écouter sans bouger
  ? done
  {A} écoute les trois notes s'éloigner. Elles reviendront. Ce genre de sifflet revient toujours.
```

### 4. Les trois notes (le Siffleur, plus tard)

```
## scene: whistle_follow
kind: encounter whistler
title: Les trois notes
needs: step whistler seen

Les trois notes, encore. Une haute, deux basses. Cette fois, elles viennent de tout près, d'un buisson de fougères qui tremble.

* follow | Suivre le sifflet à travers les fougères  [roll Agilité 5] [if no step whistler close]
  ? win  [step whistler close]
  {A} se glisse entre les tiges sans en casser une. Et le voilà : un oiseau rond comme une pomme, brun comme une feuille morte, qui siffle sans ouvrir le bec. Il regarde {A}. {A} le regarde. Ils sont aussi surpris l'un que l'autre.
  ? lose  [dirt]
  {A} suit le sifflet, puis un autre, puis un troisième. Il y a trois sifflets ? Non : c'est le même, qui tourne autour d'{a:lui|elle} pour s'amuser. {A} finit par terre, plein de mousse.

* tune | Lui siffler son propre air  [roll Intelligence 6] [needs step whistler close] [if no step whistler tamed]
  ? win  [step whistler tamed]
  {A} siffle l'air exact : une haute, deux basses, et la question à la fin. L'oiseau penche la tête, sautille jusqu'à son épaule et s'y installe pour le reste du chemin.
  ? lose
  {A} se trompe d'une note. L'oiseau répète la bonne, très lentement, comme on parle à un tout-petit. Ce sera pour une autre fois.

* listen | L'écouter de loin
  ? done
  {A} s'assoit et l'écoute. Au bout d'un moment, le sifflet ajoute une note pour {a:lui|elle}. {A} en est {a:sûr|sûre}.
```

### 5. La châtaigne qui roule (Hérisson-Châtaigne)

```
## scene: hedgehog
kind: encounter hedgehog
title: La châtaigne qui roule

En haut de la pente, une grosse bogue de châtaigne se met à rouler. Droit sur {A}. Elle a des pattes. Elle a un museau. Elle a l'air très, très fâchée.

* dodge | Sauter de côté  [roll Agilité 4] [if no step hedgehog seen]
  ? win  [step hedgehog seen]
  {A} saute. La bogue passe, freine, se déroule : un hérisson aux piquants bruns et pointus comme des bogues, qui fait semblant de ne pas avoir raté son coup.
  ? lose  [wound]
  Trop tard. La bogue roule sur le pied de {A}, qui découvre ce que ça fait, une bogue vivante. Ça pique. Ça pique beaucoup.

* stand | Lui tenir tête, chapeau en avant  [roll Courage 4] [needs step hedgehog seen] [if no step hedgehog close]
  ? win  [-> hedgehog_b]
  {A} plante ses pieds et gonfle son chapeau. Le hérisson s'arrête net, renifle, recule de trois pas. Il prend de l'élan.
  ? lose  [wound]
  {A} tient tête une demi-seconde, ce qui est déjà bien. Le hérisson, lui, tient tête beaucoup plus longtemps, et piquants en avant.

* chestnut | Lui renvoyer une châtaigne en roulant  [roll Agilité 3] [needs step hedgehog close] [if no step hedgehog tamed]
  ? win  [step hedgehog tamed]
  {A} fait rouler une châtaigne jusqu'à lui. Le hérisson la renvoie du museau. {A} la renvoie. Ça dure jusqu'à ce que la châtaigne finisse dans le ruisseau, et le hérisson a l'air de trouver que c'était la meilleure partie.
  ? lose
  La châtaigne part de travers et rebondit sur son nez. Le hérisson se met en boule, vexé. Il boudera jusqu'à la prochaine fois.

* climb | Grimper sur une souche et attendre
  ? done
  {A} grimpe sur une souche. Le hérisson fait trois fois le tour en grognant, puis s'en va, digne, comme s'il avait gagné.
```

```
## scene: hedgehog_b
kind: sequel hedgehog
title: La deuxième charge

Le hérisson charge, roulé en boule. {A} n'a plus le temps de réfléchir.

* hold | Ne pas bouger d'un poil  [roll Endurance 5]
  ? win  [step hedgehog close]
  {A} ne bouge pas. La boule s'arrête à un doigt de ses pieds, se déroule, et le hérisson lève vers {a:lui|elle} un museau plein de respect. Chez les hérissons, on ne charge pas deux fois quelqu'un qui ne bouge pas.
  ? lose  [wound]
  {A} bouge d'un poil. Le mauvais poil. La bogue lui roule dessus, et s'en va sans s'excuser.

* back | Reculer, finalement
  ? done
  {A} recule. Le hérisson s'arrête, satisfait d'avoir eu le dernier mot, et retourne en haut de sa pente.
```

### 6. Le tronc qui bouge (Lucane-Écorce)

```
## scene: beetle
kind: encounter beetle
title: Le tronc qui bouge

Un tronc couché barre le sentier. Un tronc ordinaire, sauf qu'un bout d'écorce, gros comme un pain, vient de se lever sur six pattes.

* watch | Regarder ce morceau d'écorce de plus près  [if no step beetle seen]
  ? done  [step beetle seen]
  C'est un scarabée. Un scarabée de la taille d'un pain de Pain-Blond, couleur d'écorce, avec deux grandes cornes en forme de pince. Il garde son tronc, et il le fait savoir.

* push | Le pousser pour passer  [roll Force 5] [needs step beetle seen] [if no step beetle close]
  ? win  [-> beetle_b]
  {A} pousse. Le scarabée pousse. Ils restent front contre cornes, aucun ne recule. Le scarabée gratte la mousse : il veut la revanche tout de suite.
  ? lose  [wound]
  {A} pousse. Le scarabée pousse mieux. {A} se retrouve {a:assis|assise} dans la mousse, une belle bosse sur le front.

* bark | Remettre à sa place l'écorce tombée de son tronc  [roll Intelligence 5] [needs step beetle close] [if no step beetle tamed]
  ? win  [step beetle tamed]
  Un pan d'écorce est tombé du tronc. {A} cherche, essaie, trouve le bon sens et le remet en place, comme une pièce de puzzle. Le scarabée vient vérifier, tapote, et s'écarte pour laisser passer {A}. Chez lui, c'est un merci.
  ? lose
  {A} remet l'écorce à l'envers. Le scarabée la retourne lui-même, en soupirant de toutes ses pattes.

* around | Faire le tour du tronc
  ? done
  {A} fait le tour, poliment. Le scarabée {a:le|la} suit des yeux tout du long, sans rien dire, ce qui est pire.
```

```
## scene: beetle_b
kind: sequel beetle
title: La revanche

Le scarabée s'arc-boute, cornes en avant. Deuxième manche.

* push | Pousser de toutes ses forces  [roll Force 6]
  ? win  [step beetle close]
  {A} pousse de toutes ses forces, et le scarabée recule d'un pas. Un seul. Il s'arrête, regarde {A}, et va se percher sur son tronc comme un roi sur son trône. Il a perdu, mais il a décidé que c'était un match nul.
  ? lose  [wound]
  Deuxième manche pour le scarabée. Il pousse {A} jusque dans les fougères, sans méchanceté, avec beaucoup de méthode.

* stop | S'arrêter là, à égalité
  ? done
  {A} recule le premier. Le scarabée garde la position un long moment, pour être sûr que c'est fini.
```

### 7. Le voleur de paniers (Furet des Fougères)

Seulement après une trouvaille pendant cette expédition.

```
## scene: ferret
kind: encounter ferret
title: Le voleur de paniers
needs: found

Les fougères bruissent derrière {A}. Puis à gauche. Puis devant. Quelque chose de long et de très rapide tourne autour de son panier, et de ce qu'il y a dedans.

* turn | Se retourner d'un coup  [roll Intelligence 4] [if no step ferret seen]
  ? win  [step ferret seen]
  {A} se retourne au bon moment. Une bête longue comme un bras, rousse et moustachue, se fige, le museau à deux doigts du panier. Elle fait semblant de passer par là.
  ? lose  [lose find]
  {A} se retourne du mauvais côté. Quand {a:il|elle} se retourne du bon, le panier est plus léger, et une queue rousse disparaît dans les fougères.

* chase | Le laisser approcher… et lui courir après  [roll Vitesse 5] [needs step ferret seen] [if no step ferret close]
  ? win  [step ferret close]
  Le furet saisit la trouvaille et file. {A} file plus vite. Au bout de vingt pas, le furet lâche tout, s'assoit et regarde {A} avec quelque chose qui ressemble à de l'admiration.
  ? lose  [lose find]
  Le furet saisit la trouvaille et file. {A} court, court, et perd. Le furet, de loin, fait un petit bond de victoire.

* give | Lui tendre la trouvaille  [needs step ferret close] [if no step ferret tamed]
  ? done  [lose find] [step ferret tamed]
  {A} tend sa trouvaille. Le furet n'y croit pas. Il la prend, très lentement, puis revient poser sa tête contre la main de {A}. Ça valait bien un champignon.

* hold | Serrer le panier contre soi
  ? done
  {A} serre son panier contre {a:lui|elle}. Les fougères bruissent encore un peu, déçues, puis se taisent.
```

### 8. La coquille qui tinte (Escargot-Cloche)

```
## scene: snail
kind: encounter snail
title: La coquille qui tinte

Quelque part, quelque chose tinte. Tout doucement, comme une petite cloche qu'on aurait oubliée sous une feuille.

* hear | Chercher d'où vient le tintement  [if no step snail seen]
  ? done  [step snail seen]
  Sous une feuille, un escargot à la coquille couleur de cuivre. À chaque fois qu'une goutte tombe dessus, elle tinte. Derrière lui, une traînée d'argent file sous les fougères.

* trail | Suivre sa traînée d'argent jusqu'au bout  [roll Endurance 5] [needs step snail seen] [if no step snail close]
  ? win  [step snail close] [find rare]
  {A} suit la traînée. Elle est longue. Très longue. Elle passe sous trois troncs et fait le tour d'une souche, deux fois. Au bout, l'escargot attend, et à côté de lui, un champignon que {A} n'avait jamais vu.
  ? lose
  La traînée tourne en rond. {A} aussi. Au bout d'un moment, il faut bien admettre qu'on suit sa propre trace.

* tap | Tapoter sa coquille en rythme  [roll Intelligence 5] [needs step snail close] [if no step snail tamed]
  ? win  [step snail tamed]
  Tic, tic-tic, tic. La coquille répond : ting, ting-ting, ting. L'escargot sort ses cornes en entier, ce qu'un escargot ne fait que pour ses amis.
  ? lose
  Tic, tic, tic, tic. Ce n'est pas le bon rythme. L'escargot rentre dans sa coquille et ferme la porte.

* greet | Le saluer et passer son chemin
  ? done
  {A} salue l'escargot. Une goutte tombe sur sa coquille, ting. Ça devait être une réponse.
```

(`[find rare]` : la trouvaille, tirée vers le rare ; seulement si l'expédition n'en a pas encore. Sinon, l'étape seule.)

### 9. Le vieux du terrier (Vieux Blaireau gris)

Rare : seulement quand la personne a au moins une étape chez cinq autres créatures.

```
## scene: badger
kind: encounter badger
title: Le vieux du terrier

Au fond de la Combe, sous les racines d'un hêtre, un terrier large comme une porte. Ça sent la terre et le vieux bois. Quelqu'un, là-dedans, ronfle comme un moulin.

* near | S'approcher du terrier  [if no step badger seen]
  ? done  [step badger seen]
  Le ronflement s'arrête. Un museau gris sort, puis une tête rayée, énorme, vieille comme les arbres. Le blaireau regarde {A}, longuement. Puis il rentre, sans un mot, finir sa sieste.

* hold | Tenir bon quand il sort, sans reculer  [roll Courage 5] [needs step badger seen] [if no step badger close]
  ? win  [step badger close]
  Le blaireau sort, tout entier, et avance jusqu'à {A}. {A} ne recule pas. Le vieux renifle son chapeau, souffle par le nez, et se recouche en travers de l'entrée, à côté d'{a:lui|elle}. C'est une permission.
  ? lose  [dirt]
  Le blaireau sort, et {A} recule d'un pas. Le vieux s'assoit sur la mousse, puis, distraitement, à moitié sur {A}. Il est lourd. Il est tiède. Il ne s'en rend pas compte.

* bow | Le saluer la main sur le cœur, comme l'Étrangère  [needs step badger close] [if no step badger tamed]
  ? done  [step badger tamed]
  {A} pose la main sur son cœur et s'incline, comme l'Étrangère le fait. Le vieux blaireau regarde, puis baisse lentement la tête. Il connaît ce salut. Il a l'air content qu'on s'en souvienne.

* fight | Le défier à la bagarre
  ? done  [dirt]
  {A} le défie. Le vieux blaireau bâille, s'avance, et s'assoit dessus. Délicatement. Il attend qu'{a:il|elle} ait compris, puis se relève et rentre chez lui.

* quiet | Le laisser dormir
  ? done
  {A} s'éloigne sur la pointe des pieds. Le ronflement reprend, rassuré.
```

---

## Trouvailles

Seulement si l'expédition n'a pas encore fait sa trouvaille.

### 10. Sous le tronc couché

```
## scene: find_trunk
kind: find
title: Sous le tronc couché

Un tronc couché, si vieux qu'il est devenu doux comme du pain. Dessous, dans l'ombre, quelque chose de pâle dépasse.

* crawl | Ramper dessous  [roll Agilité 4]
  ? win  [find]
  {A} se glisse sous le tronc, à plat ventre, et ressort avec la trouvaille, couverte de mousse et très fière.
  ? lose  [dirt]
  {A} se glisse sous le tronc, et le tronc se rappelle soudain qu'il est un tronc. {A} ressort par où {a:il|elle} est {a:entré|entrée}, à reculons, {a:crotté|crottée} jusqu'aux oreilles.

* lift | Soulever le tronc  [roll Force 6]
  ? win  [find]
  {A} soulève le tronc, un peu, juste assez. Dessous, dans la terre fraîche, le champignon attend comme un cadeau sous un couvercle.
  ? lose  [wound]
  {A} soulève le tronc, un peu, puis le tronc redescend, un peu trop vite, sur ses doigts.

* leave | Le laisser où il est
  ? done
  Il y a des choses qui sont mieux sous leur tronc. {A} en décide ainsi, et passe.
```

### 11. Le cercle

```
## scene: find_ring
kind: find
title: Le cercle

Dans une clairière grande comme une nappe, des champignons poussent en cercle parfait. Tous pareils. Sauf un, peut-être.

* pick | Chercher celui qui n'est pas comme les autres  [roll Intelligence 4]
  ? win  [find]
  {A} fait le tour du cercle une fois, deux fois. Au troisième tour, {a:il|elle} le voit : un seul champignon dont le chapeau est d'une autre couleur. Celui-là vient avec {a:lui|elle}.
  ? lose
  {A} fait le tour du cercle tant de fois que la tête lui tourne. Ils se ressemblent tous. Ils se ressemblent vraiment tous.

* dance | Faire le tour du cercle en sautillant  [roll Agilité 5]
  ? win  [find]
  {A} fait le tour en sautillant, sans écraser un seul champignon. Au dernier saut, {a:il|elle} atterrit pile à côté du plus beau.
  ? lose  [dirt]
  Au troisième saut, {A} glisse et atterrit au milieu du cercle, sur le derrière. Les champignons n'ont rien. {A} est {a:plein|pleine} de terre.

* leave | Admirer le cercle sans y toucher
  ? done
  {A} admire le cercle un moment. Certaines choses sont plus jolies quand personne n'y touche.
```

### 12. La racine au-dessus de l'eau

```
## scene: find_root
kind: find
title: La racine au-dessus de l'eau

Une racine enjambe le ruisseau comme un pont pour fourmis. Au milieu, juste au-dessus de l'eau, pousse un champignon.

* walk | Marcher sur la racine  [roll Agilité 5]
  ? win  [find]
  {A} avance sur la racine, les bras écartés, un pas, puis un autre. Le champignon se cueille sans un bruit.
  ? lose  [dirt]
  {A} avance sur la racine, les bras écartés. Au troisième pas, la racine se tortille un peu. Plouf. Le champignon, lui, reste sec.

* branch | Le décrocher avec une branche  [roll Intelligence 5]
  ? win  [find]
  {A} trouve une branche fourchue, juste de la bonne longueur, et décroche le champignon. Il tombe dans sa main, pas dans l'eau.
  ? lose
  Le champignon tombe dans l'eau, file avec le courant, et disparaît sous une pierre. Il est parti en expédition, lui aussi.

* leave | Le laisser au ruisseau
  ? done
  {A} le laisse pousser au-dessus de l'eau. Il a l'air de bien s'y plaire.
```

---

## Obstacles

### 13. Le ruisseau

```
## scene: stream
kind: obstacle
title: Le ruisseau

Le ruisseau coupe le chemin. Il n'est pas large, mais il est froid, et il a l'air de le savoir.

* jump | Sauter par-dessus  [roll Agilité 5]
  ? win
  {A} prend son élan et saute. Atterrissage parfait. {A} se retourne pour voir si quelqu'un a vu. Personne. C'est dommage.
  ? lose  [wound] [dirt]
  {A} saute, presque assez loin. Le genou rencontre une pierre, le reste rencontre l'eau.

* wade | Traverser à gué, pierre après pierre  [roll Endurance 4]
  ? win
  {A} traverse, l'eau jusqu'aux genoux, sans se presser. De l'autre côté, {a:il|elle} secoue ses pieds un à un, comme un chat.
  ? lose  [dirt]
  À mi-chemin, une pierre roule. {A} finit le trajet à quatre pattes, et {a:trempé|trempée}.

* stones | Attendre que les pierres apparaissent  [needs trait patient]
  ? done  [cailloux 3]
  {A} attend. Le ruisseau baisse un peu, comme toujours vers cette heure-là. Les pierres apparaissent, et entre deux, un joli caillou qui n'attendait que {a:lui|elle}.

* around | Remonter jusqu'au tronc qui fait pont
  ? done
  {A} remonte le ruisseau jusqu'au tronc couché qui l'enjambe. C'est plus long. C'est sec.
```

### 14. La pente

```
## scene: slope
kind: obstacle
title: La pente

Le chemin plonge d'un coup. La pente est couverte de feuilles mouillées, luisantes comme du savon.

* slide | Descendre en glissant  [roll Vitesse 4]
  ? win  [cailloux 5]
  {A} s'assoit sur une grande feuille et se laisse glisser. C'est très rapide et très réussi. En bas, dans les feuilles, brille un caillou que la glissade a déterré.
  ? lose  [wound]
  {A} descend en glissant, beaucoup plus vite que prévu, et s'arrête contre une racine. La racine n'a rien.

* careful | Descendre pas à pas, en se tenant aux racines  [roll Endurance 3]
  ? win
  {A} descend en se tenant à chaque racine, comme on descend un escalier qu'on ne connaît pas. En bas, {a:il|elle} n'a pas une feuille sur {a:lui|elle}.
  ? lose  [dirt]
  À mi-pente, {A} rate une racine et finit en luge, sur le dos. {a:Plein|Pleine} de feuilles, mais en bas.

* path | Chercher un autre chemin
  ? done
  {A} longe le haut de la pente jusqu'à un endroit plus doux. Ça prend du temps, mais les feuilles restent par terre.
```

### 15. Les ronces

```
## scene: bramble
kind: obstacle
title: Les ronces

Un mur de ronces ferme le passage. Derrière, on entend le ruisseau, et quelque chose qui sent bon la mûre.

* push | Forcer le passage  [roll Force 5]
  ? win
  {A} écarte les ronces à deux mains, les tient, passe. De l'autre côté, il reste même quelques mûres.
  ? lose  [wound]
  {A} force le passage. Les ronces forcent aussi. {A} passe, mais en laisse un peu derrière.

* gap | Chercher un trou dans la haie  [roll Intelligence 4]
  ? win
  En regardant bien, il y a un trou en bas, juste à la taille d'un Plynling. {A} s'y glisse sans une égratignure.
  ? lose  [dirt]
  {A} trouve un trou. C'était un terrier de lapin. {A} en ressort plein de terre, le lapin aussi, et ils se regardent sans savoir qui doit s'excuser.

* trail | Suivre une petite piste que personne n'avait vue  [needs trait curious]
  ? done
  {A} remarque ce que personne n'aurait vu : une toute petite piste, au ras du sol, qui contourne la haie. {A} la suit, {a:fier|fière} comme tout.

* around | Faire le tour
  ? done
  {A} fait le grand tour. Les mûres, de l'autre côté, sont pour les oiseaux.
```

### 16. La brume

```
## scene: fog
kind: obstacle
title: La brume

D'un coup, la brume tombe. Les troncs disparaissent, puis les fougères, puis les pieds de {A}. Il ne reste que des bruits : de l'eau, une branche, un oiseau.

* sound | Se guider au bruit du ruisseau  [roll Intelligence 5]
  ? win
  {A} ferme les yeux et suit le bruit de l'eau. Quand la brume se lève, {a:il|elle} est exactement où {a:il|elle} voulait être.
  ? lose  [stress 5]
  {A} suit le bruit de l'eau, qui tourne, et tourne. Quand la brume se lève, {A} est {a:revenu|revenue} à son point de départ, un peu {a:inquiet|inquiète}.

* straight | Marcher tout droit, sans peur  [roll Courage 4]
  ? win
  {A} marche tout droit, le menton haut. La brume s'écarte devant {a:lui|elle}, comme si elle n'osait pas.
  ? lose  [wound]
  {A} marche tout droit, le menton haut, et rencontre un arbre, le front d'abord.

* hide | Se coller à un tronc jusqu'à ce que ça passe  [needs trait craven]
  ? done  [cailloux 3]
  {A} connaît la meilleure façon d'attendre : {a:collé|collée} à un tronc, sans bouger. La brume passe. Entre deux racines, il y avait un caillou, pour la patience.

* wait | S'asseoir et attendre
  ? done
  {A} s'assoit et attend. La brume finit toujours par partir. Celle-ci prend son temps.
```

---

## Le fil : *La source qui plonge*

Une étape par expédition au plus, dans l'ordre. Raté, on recommence une prochaine fois, sans rien perdre.

### 17. Là où l'eau s'en va

```
## scene: source_1
kind: thread source 1
title: Là où l'eau s'en va

Le ruisseau de la Combe arrive à une grosse pierre moussue, et disparaît dessous. Pas de trou, pas de cascade : il s'en va, simplement, sous la terre.

* ear | Coller l'oreille contre la pierre  [roll Intelligence 4]
  ? win  [unlock source_1]
  {A} colle son oreille contre la pierre. Dessous, l'eau continue, on l'entend chanter. Elle va quelque part, par là, vers le fond de la Combe. {A} s'en souviendra.
  ? lose
  {A} n'entend que son propre cœur, qui bat fort. Ce sera pour une autre fois.

* push | Essayer de pousser la pierre  [roll Force 7]
  ? win  [unlock source_1]
  La pierre bouge d'un doigt. Dessous, l'eau file dans un tunnel tout noir, vers le fond de la Combe. {A} remet la pierre en place. On ne laisse pas une porte ouverte.
  ? lose
  La pierre ne bouge pas. Elle n'a jamais bougé. Elle a l'air de trouver l'idée amusante.

* watch | Regarder l'eau disparaître
  ? done
  {A} regarde l'eau s'en aller sous la pierre, longtemps. Où va-t-elle ? La question reste là, comme un caillou dans la poche.
```

### 18. L'eau sous la terre

```
## scene: source_2
kind: thread source 2
title: L'eau sous la terre
needs: unlock source_1

Plus loin dans la Combe, {A} entend l'eau de nouveau, sous ses pieds. Elle file vers un ravin, là où la terre se fend.

* climb | Descendre dans le ravin  [roll Agilité 5]
  ? win  [unlock source_2]
  {A} descend dans le ravin, de racine en racine. Tout au fond, l'eau ressort de la roche, claire et pressée, et file encore plus bas, là où il fait sombre.
  ? lose  [dirt]
  {A} commence à descendre, puis la terre décide de descendre plus vite qu'{a:lui|elle}. Retour en haut, {a:plein|pleine} de terre, et le ravin garde son secret pour aujourd'hui.

* follow | Suivre le chant de l'eau en surface  [roll Intelligence 5]
  ? win  [unlock source_2]
  {A} marche en écoutant le sol. Le chant de l'eau mène au bord d'un ravin, et tout en bas, l'eau ressort, claire. Le chemin continue par là. Un jour.
  ? lose
  Le chant de l'eau se perd sous un tapis de feuilles. {A} écoute, écoute. Ce sera pour une prochaine fois.

* rest | S'asseoir au bord et écouter
  ? done
  {A} s'assoit au bord du ravin et écoute l'eau en bas. Elle n'est pas pressée qu'on la trouve.
```

### 19. Le bassin des Phalènes

```
## scene: source_3
kind: thread source 3
title: Le bassin des Phalènes
needs: unlock source_2

Tout au fond du ravin, il fait sombre comme au crépuscule, même à midi. L'eau s'y repose dans un bassin rond, immobile. Et au-dessus du bassin, par centaines, les Phalènes-Lanternes dansent.

* sit | S'asseoir au bord sans faire de bruit
  ? done  [unlock source_3] [step moths tamed] [epithet phalenes] [moment]
  {A} s'assoit au bord. Les lueurs vertes descendent, une à une, et se posent sur l'eau, sur les pierres, sur {A}. Le bassin s'allume tout entier. Personne n'a jamais vu ça. Maintenant, quelqu'un l'a vu.

* hand | Tendre la main au-dessus de l'eau
  ? done  [unlock source_3] [step moths tamed] [epithet phalenes] [moment]
  {A} tend la main. Une Phalène s'y pose, puis toutes les autres viennent se poser autour, sur l'eau, jusqu'à ce que le bassin brille comme une lanterne. {A} ne bouge plus. Ça dure longtemps. Ça ne dure pas assez.
```

**Le titre** (`phalenes`) : « {a:celui|celle} qui a vu le bassin des Phalènes ». Sur la carte, après le nom :
« Brindille, celle qui a vu le bassin des Phalènes ». **Le moment de journal** : « A trouvé le bassin des Phalènes,
au fond de la Combe aux Mousses. »

---

## Retours

Une ligne, tirée au hasard, au-dessus du bilan.

```
## return: return_1
{A} remonte vers la Lisière. Le bruit du village revient d'un coup, trois pas avant les premières maisons, comme une porte qu'on rouvre.
```

```
## return: return_2
Les derniers troncs, puis la lumière. {A} sort de la Combe en clignant des yeux, de la mousse dans le chapeau, et des histoires plein les poches.
```

```
## return: return_3
{A} rentre par le même chemin, en marchant un peu moins vite qu'à l'aller. Ce n'est pas la fatigue. C'est qu'il y avait encore des bruits à écouter.
```

---

## Le sauvetage

Au deuxième coup dur. La trouvaille reste dans la Combe ; le reste est gardé.

```
## rescue: rescue

{A} s'assoit sur une racine. Le pied fait mal, le genou aussi, et la Combe a l'air beaucoup plus grande qu'à l'aller.

Une lueur verte, très douce, s'approche entre les troncs. Sous une capuche, l'Étrangère. Elle s'accroupit, regarde le genou, regarde {A}.

« On rentre ? » demande-t-elle. Ce n'est pas vraiment une question.

Elle prend {A} dans ses bras, et pose le panier sur la mousse. « Il sera mieux ici », dit-elle. Puis elle marche, sans se presser, et sous sa capuche la lueur éclaire le chemin jusqu'à la Lisière.
```

---

## Questions de relecture

Laissées par défaut, à changer d'un mot :

1. **Le titre** « {a:celui|celle} qui a vu le bassin des Phalènes » : un peu long sur la carte ; une version
   courte serait « l'{a:ami|amie} des Phalènes ».
2. **Le Siffleur est un oiseau** rond et brun qui siffle sans ouvrir le bec. C'est la seule bizarrerie qu'il a.
3. **Le Blaireau reconnaît le salut de l'Étrangère.** Il connaît ce geste, mais rien ne dit d'où.
4. **Le Furet n'est pas « du Bois Sourd »** au sens magique : un furet roux, juste très doué.
5. **Le sauvetage ne varie pas** : une seule scène. Elle peut avoir deux ou trois variantes si on la voit souvent.
6. **« crotté », « trempé »** marquent l'hygiène qui baisse (`[dirt]`), jamais une blessure.
