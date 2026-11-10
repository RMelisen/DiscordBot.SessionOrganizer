// L'arrivée — every Plynling's welcome, brought by its adoption. Third draft (2026-11-10), written
// to the cosy bible (docs/village-lore.md): short, warm, nothing at stake.
//
// Premise: a pousse comes out of the Souche, is waited for, is shown the hameau, and is given a
// pebble of its own. Three evenings, three choices, no wrong answer. It introduces the Mère Lichen,
// one villager of the player's choosing, and the idea that someone is watching from above.
// It stands alone: no later event needs anything from it beyond its small memories.
//
// Map (3 decisions, one a day):
//
//   first_steps ── to_her / stay / dig (rowdy) / wood (curious) ──→ the_village
//   the_village ── bread / bees / inn / count ────────────────────→ the_pebble
//   the_pebble ──┬─ keep ────→ kept_pebble   (keepsake: the pebble)
//                ├─ pile ────→ on_the_pile   (epithet: de la Lisière)
//                └─ nod_up ──→ watched       (epithet: Nez-en-l'air)
//
// From the bible: the Mère Lichen waits for the pousse to come by itself; one pebble on her pile per
// pousse she has welcomed; « pardon » to a thread of the Trame; the hand on the Pont-Vieux's stone;
// the crust of the veillés; the Tilleul shouting the news to his bees; the Courge's extra bowl; the
// Fou greeting the keeper aloud.
// Nobody genders the keeper: « quelqu'un », « on », « vous ». The player's gender is unknown.
// No stress, no roll, no trait change: a bébé holds only its childhood trait.

# arc: arrival
title: L'arrivée
family: milestone
stage: baby
trigger: adoption
public: Une pousse vient de sortir de la Souche. La Mère Lichen attend qu'elle vienne à elle.

## memories
lichen.came_first      | lichen     | private | +3 | Est venu{a:|e} à elle de {a:lui|elle}-même, le premier matin.
lichen.said_pardon     | lichen     | private | +2 | A dit pardon à la Trame, le premier jour, en tapant la terre un peu partout.
pain_blond.ate_crust   | pain_blond | private | +2 | A mangé la croûte des veillés devant le four, et en a laissé une miette sur le rebord.
tilleul.met_bees       | tilleul    | private | +2 | Était là quand il a annoncé aux abeilles qu'il y avait une pousse neuve.
courge.ate_well        | courge     | private | +2 | A fini trois bols de soupe à l'auberge, le jour de sa première descente au hameau.
fou.looked_up_with_him | fou        | private | +3 | A regardé en l'air avec lui, sur la place, un bon moment.

## epithets
sky_gazer     | {a:Le|La} Nez-en-l'air
lisiere_child | de la Lisière

## keepsakes
lichen_pebble | Le galet de la Mère Lichen | Un galet de la Sommeille, lisse et tiède, plus petit que ceux du tas devant sa porte. Elle l'a posé devant {A} un soir : « Le tien. »

## scene: first_steps
title: Les premiers pas
pace: now
public: {A} hésite au pied de la Souche.

Il a plu toute la nuit. Au matin, l'écorce de la Souche s'est fendue comme une miche trop cuite, et {A} est {a:sorti|sortie} par la fente, {a:trempé|trempée}, la lumière plein les yeux.
[if trait curious] {A} a déjà goûté la mousse, l'écorce et un caillou. Le caillou était le moins bon.
[if trait rowdy] {A} a déjà glissé deux fois dans la boue. La troisième, c'était exprès.
[if trait pensive] {A} regarde une goutte grossir au bout d'une feuille, tomber, et attend la suivante.
[if trait bossy] {A} a déjà remis à sa place une brindille qui n'y était pas.
[if trait charming] Un scarabée s'est arrêté pour regarder {A}. {A} lui sourit. Le scarabée reste.

À dix pas, une vieille femme grise et verte est assise sur une pierre, si immobile qu'on la prendrait pour la pierre. Elle n'appelle pas. Elle ne tend pas la main. Elle attend, et elle a l'air d'avoir tout son temps.
Une seule fois, elle lève les yeux. Pas vers {A} : juste au-dessus. Elle fait un petit signe du menton, comme on salue quelqu'un qui vient d'entrer. Au-dessus de {A}, il n'y a que le ciel.

* to_her | Aller vers la vieille femme
  [lean sociability +2]
  {A} fait trois pas, tombe, en fait quatre autres. La vieille femme ne bouge pas, mais quelque chose sourit dans ses épaules.
  Quand {A} arrive contre son genou, elle pose la main sur son chapeau, très légèrement, comme on vérifie qu'une pâte a levé. « Te voilà. »
  [flag came_to_her] [remember lichen.came_first] [-> the_village]

* stay | Rester contre la Souche
  [lean boldness -1]
  {A} reste {a:collé|collée} à l'écorce jusqu'au soir. Sous la joue, le bois est tiède comme un pain qui sort du four.
  La vieille femme ne bouge pas non plus. {a:Ils|Elles} sont deux à savoir attendre.
  À la nuit, elle se lève, pose une couverture sur {A}, et se rassoit. Un peu plus près.
  [flag waited] [-> the_village]

* dig | Gratter la terre au pied de la Souche  [needs trait rowdy]
  [lean energy +2]
  {A} gratte. Sous la terre noire, il y a des fils blancs, fins comme des cheveux, qui courent dans tous les sens. {A} en attrape un.
  La vieille femme est là d'un coup, bien plus vite qu'on ne l'en aurait crue capable. Elle recouche le fil, le recouvre, et dit à la terre : « Pardon. {a:Il est neuf|Elle est neuve}. »
  Puis elle montre à {A} comment on dit pardon : une petite tape sur la terre. {A} tape. Beaucoup. Partout.
  [flag saw_trame] [remember lichen.said_pardon] [-> the_village]

* wood | Partir vers les grands arbres  [needs trait curious]
  [lean boldness +2]
  {A} trottine jusqu'aux fougères, renifle l'ombre, écoute un long moment quelque chose que personne d'autre n'entend, puis revient, le bout du chapeau mouillé.
  La vieille femme n'a pas bougé. Elle a seulement compté les pas, à l'aller et au retour. « Quarante. Tu iras plus loin. Pas encore. »
  [grow courage] [flag went_far] [-> the_village]

## scene: the_village
title: Le hameau
pace: next_day
public: {A} descend au hameau pour la première fois. C'est jour de marché.

La vieille femme s'appelle la Mère Lichen. C'est du moins comme ça que tout le monde l'appelle. Ce matin, elle a installé {A} dans son panier, entre les fèves et un pot de soupe, et elle a pris le chemin du hameau.
[if flag came_to_her] {A} tient un coin de son châle, et ne le lâche pas.
[if flag waited] {A} a gardé la couverture sur les épaules. La Mère Lichen n'a rien dit. Elle a juste rentré un coin qui traînait.
[if flag saw_trame] En chemin, {A} se penche du panier pour taper la terre tous les dix pas. « Pardon », traduit la Mère Lichen à ceux qu'on croise.
[if flag went_far] {A} essaie trois fois de sortir du panier. « Quarante et un », compte la Mère Lichen, en {a:le|la} rasseyant.
Sur le Pont-Vieux, elle pose la main sur la pierre, sans s'arrêter. {A} fait pareil, parce que ça a l'air important.

Sur la place du Puits, tout sent quelque chose : le pain chaud d'un côté, le miel de l'autre, la soupe par la porte ouverte de l'auberge. Près du puits, un grand maigre à la tête jaune ébouriffée compte les pavés à voix haute.
La Mère Lichen pose le panier. « Va. Je ne bouge pas. »

* bread | Suivre l'odeur du pain
  [lean energy +1]
  Devant le four du village, un jeune homme tout doré, de la farine jusqu'aux coudes, parle tout bas à une boule de pâte. Il sursaute en voyant {A}, rougit, et lui tend une croûte encore chaude.
  « C'est la croûte des veillés. La première de la fournée. D'habitude je la pose sur le rebord, pour… enfin. Pour là-haut. »
  {A} mange la croûte, et laisse une miette sur le rebord. Pain-Blond regarde la miette, puis le ciel, et sourit à ses chaussures.
  [flag ate_crust] [remember pain_blond.ate_crust] [-> the_pebble]

* bees | Suivre une abeille
  [lean boldness +1]
  L'abeille mène {A} jusqu'à un verger, où un très vieil arbre voûté crie les nouvelles à ses ruches. « …ET LA CHÈVRE DE DAME ERMENGARDE EST RETROUVÉE. Dans le lavoir. Oui. »
  Il aperçoit {A}, plisse les yeux, et se retourne vers les ruches. « ET IL Y A UNE POUSSE NEUVE. Avec un chapeau comme ça. » Toute la place a entendu.
  Puis le Tilleul donne à {A} un doigt de miel, l'appelle par un prénom qui n'est pas le sien, s'excuse, et lui donne une pomme.
  [flag told_bees] [remember tilleul.met_bees] [-> the_pebble]

* inn | Entrer à l'auberge
  [lean sociability +1]
  « Mais qui voilà ! Assieds-toi, mon chou. Tu as mangé ? Non. Tu es à qui ? À la Mère Lichen, bien sûr. Et là-haut, qui c'est, qui te veille ? Tu ne sais pas. Personne ne sait, c'est ça qui est bien. »
  La Courge n'a attendu aucune réponse. Trois bols de soupe plus tard, {A} ne peut plus bouger.
  Elle pose un quatrième bol au bout de la table, devant une chaise vide. « Au cas où. »
  [flag at_inn] [remember courge.ate_well] [-> the_pebble]

* count | Compter les pavés avec le grand maigre
  [lean rationality +1]
  « Quarante-six, quarante-sept… ah. Bonjour. » Il ne regarde pas {A} : il regarde juste au-dessus, et soulève un chapeau qu'il n'a pas. « Et bonjour à vous. Vous avez l'œil. »
  « C'est le Fou », dit la tisserande à {A}, comme on dirait « c'est le syndic ».
  Le Fou se penche. « On t'a {a:regardé|regardée} dormir, cette nuit. Longtemps. Je dis ça, je ne dis rien. » {A} lève la tête. Lui aussi. Ils restent comme ça un bon moment, et deux gamins s'arrêtent pour regarder en l'air avec eux.
  [flag with_fou] [remember fou.looked_up_with_him] [-> the_pebble]

## scene: the_pebble
title: Le galet
pace: next_day
public: Un soir à la Lisière, la Mère Lichen pose un galet devant {A}.

Le soir, devant la cabane, la Mère Lichen range le tas de galets qu'elle garde près de sa porte. Il y en a des dizaines, gris et lisses, tous de la rivière. Aucun ne porte de nom.
[if flag ate_crust] {A} sent encore le pain chaud. Elle a reniflé une fois, et hoché la tête.
[if flag told_bees] Au hameau, tout le monde sait déjà qu'il y a une pousse neuve. Le Tilleul l'a redit deux fois aux abeilles, pour être sûr.
[if flag at_inn] {A} n'a pas dîné. {A} ne dînera peut-être plus jamais.
[if flag with_fou] {A} a compté les galets en arrivant. Le compte n'était pas bon, et c'était très bien comme ça.

« Un par pousse que j'ai vue sortir », dit-elle. Elle en montre un du menton. « Celui-là est au moulin, maintenant. Il rit fort. » Un autre. « Celle-là m'apporte des noix que je n'ai pas demandées. »
Elle sort de sa poche un galet plus petit que les autres, lisse et tiède, et le pose devant {A}. « Le tien. »
Puis elle lève les yeux, juste au-dessus de {A}, et fait son petit signe du menton, comme le premier matin.

* keep | Garder le galet
  [lean sociability +1]
  {A} prend le galet à deux mains. Il est juste à la bonne taille.
  [-> kept_pebble]

* pile | Le poser sur le tas
  [lean zeal +1]
  {A} porte le galet jusqu'au tas, et cherche longtemps la bonne place.
  [-> on_the_pile]

* nod_up | Saluer là-haut, comme elle
  [lean boldness +1]
  {A} lève la tête, et fait le signe du menton. Un peu trop fort : le chapeau bascule.
  [-> watched]

## ending: kept_pebble
title: Le galet sous la joue
{A} n'a pas de poche. {A} dort donc avec le galet dans la main, puis sous la joue, puis sur le ventre.
La Mère Lichen fait semblant de ne rien voir. « Garde-le. Le tas n'est pas pressé. Toi non plus. »
[keepsake lichen_pebble]

## ending: on_the_pile
title: Une place sur le tas
{A} pose le galet tout en haut. Il roule. {A} le repose. Il roule encore. La Mère Lichen le cale avec deux autres, sans un mot.
« Voilà. Maintenant tu sais où c'est, chez toi. Tu peux aller aussi loin que tu veux. »
[epithet lisiere_child]

## ending: watched
title: Bien veillé{a:|e}
Là-haut, il n'y a qu'une branche et la première étoile. {A} salue quand même, une fois, deux fois, puis de la main, pour être {a:sûr|sûre}.
La Mère Lichen rit, un tout petit rire, comme un caillou qui roule. « Tu ne verras rien. Moi non plus. Mais quelqu'un est là. Ça se sent, comme la pluie avant la pluie. »
Elle pose le galet sur le tas, bien au milieu. « Tu es bien {a:veillé|veillée}. »
[epithet sky_gazer]

## ending: interrupted
title: Une autre fois
{A} s'est {a:endormi|endormie} avant la fin de l'histoire. La Mère Lichen garde son galet au chaud.
