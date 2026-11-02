// L'arrivée — every Plynling's first arc, brought by its adoption.
//
// Premise: a pousse comes out of the Souche after the rain and spends its first days at the
// Lisière with the Mère Lichen, who never calls it and waits for it to come. Each day it meets one
// more piece of Humeval and one more piece of the mystery, never explained: the white threads under
// the ground (the Trame), the stones of the dead, the old woman greeting the empty air above it (its
// keeper: the player), a song it was never taught, the smell of a kitchen it never entered, a pale
// light at the edge of the Bois Sourd. What it learns, who remembers it and what it keeps depend on
// its choices.
//
// Map (6 decisions, one a day):
//
//   first_steps ── to_her / stay / dig / wood ──→ the_cabin
//   the_cabin ──┬─ hum ──────→ the_orchard   (le Tilleul, his wife's song, « Ysaure »)
//               ├─ stones ───→ the_vigil     (the Pré des Retours, the Étrangère's light)
//               └─ look_up ──→ the_heather   (la Bruyère comes up to see it)
//   all three ───────────────→ the_boar      (the Trame torn open; a trace of the dead)
//   the_boar ────────────────→ the_market    (the hamlet, the Fou mocked by the Noyer's sons)
//   the_market ─┬─ laugh ────→ the_cold_evening ─┬─ find_fou ──────────────────→ lisiere
//               │                                └─ sulk ──────────────────────→ wary
//               └─ the rest ─→ the_last_evening ─┬─ take_stone ────────────────→ lisiere
//                                                ├─ sing  (knows the song) ────→ the_song
//                                                └─ light (met the Étrangère) ─→ the_light
//
// What it starts with: a keepsake per ending (Lichen's pebble, the Tilleul's wax, the Étrangère's
// moss), the memories its choices left with the villagers, a stat practised here and there.
// The mystery fragments are all true per docs/village-lore.md § 2 and none is explained: the Trame
// is alive and keeps the dead; a Plynling carries traces of them (the Tilleul's wife's song,
// Pain-Blond's father's kitchen); something answers above every pousse; the Étrangère has no one.
// No trait change: a bébé holds only its childhood trait, and a personality trait gained here would
// take one of the slots its ado draw is owed.
// « laugh » at the market is free on purpose (owner's call): a pousse deciding alone may mock the
// Fou and reach « wary », about one time in six in character.
// Every childhood trait gets an option of its own: curious (wood), rowdy (dig, chase), charming
// (say_name), pensive (ask_dead), bossy (show_way, scold).

# arc: arrival
title: L'arrivée
family: milestone
stage: baby
trigger: adoption
public: Une pousse vient de sortir de la Souche. La Mère Lichen attend qu'elle vienne à elle.

## memories
lichen.came_first       | lichen     | private | +5 | Est venu{a:|e} à elle de {a:lui|elle}-même, le premier matin.
lichen.dug_at_souche    | lichen     | private | -3 | A gratté la terre au pied de la Souche, le premier jour, et tiré sur la Trame.
lichen.said_ysaure      | lichen     | private | +2 | L'a appelée « Ysaure » tout haut, chez le Tilleul.
lichen.helped_trame     | lichen     | private | +3 | L'a aidée à recouvrir les fils blancs, le matin des sangliers.
tilleul.knew_the_song   | tilleul    | private | +5 | Lui a chanté la chanson de sa femme, que personne ne lui avait apprise.
etrangere.sat_together  | etrangere  | private | +3 | S'est assis{a:|e} près d'elle au bord du Bois Sourd, une nuit, sans rien demander.
bruyere.pointed_the_way | bruyere    | private | +2 | Lui a montré du doigt le chemin du retour, avant même de savoir parler.
fou.waved_with_him      | fou        | private | +5 | A salué le vide avec lui, sur la place, devant tout le monde.
fou.laughed             | fou        | private | -5 | A ri avec les fils du Noyer, sur la place, le jour du marché.
fou.came_back           | fou        | private | +3 | Est {a:revenu|revenue} le soir même compter les clous avec lui.
courge.stood_up         | courge     | private | +3 | A fait taire les fils du Noyer sur la place, haut{a:|e} comme trois pommes.
pain_blond.ate_crust    | pain_blond | private | +2 | A mangé la croûte des veillés sur le seuil du fournil.

## epithets
wild_one | {a:Le Sauvageon|La Sauvageonne}

## keepsakes
lichen_pebble | Le galet de la Mère Lichen | Un galet de la Sommeille, lisse et tiède, pareil à ceux du tas devant sa porte. Elle l'a posé devant {A} un soir : « Celui-là n'ira pas sur le tas. Pas encore. »
tilleul_wax   | La cire du Tilleul | Une boulette de cire d'abeille que le Tilleul a roulée entre ses doigts pendant que {A} chantait. Elle sent le miel et la fleur de tilleul, même en hiver.
stranger_moss | La mousse de l'Étrangère | Un brin de mousse pâle, tombé de l'Étrangère sur le chapeau de {A}. Il luisait encore le premier soir. Il ne luit plus.

## scene: first_steps
title: Les premiers pas
pace: now
public: {A} hésite au pied de la Souche.

La pluie s'est arrêtée dans la nuit. Au pied de la Souche, l'écorce s'est fendue comme une miche trop cuite, et {A} est {a:sorti|sortie} par la fente, {a:tout mouillé|toute mouillée}, la lumière plein les yeux.
[if trait curious] {A} a déjà goûté la mousse. Puis encore, pour vérifier.
[if trait rowdy] {A} a déjà glissé deux fois dans la boue, et recommence exprès.
[if trait pensive] {A} regarde une goutte se former au bout d'une feuille, et attend la suivante.
[if trait bossy] {A} a déjà remis à sa place une brindille qui n'y était pas.
[if trait charming] Un scarabée s'est arrêté pour regarder {A}. {A} lui sourit. Le scarabée ne repart pas.

À quelques pas, une vieille femme grise et verte est assise sur une pierre, si immobile qu'on la prendrait pour la pierre. Elle ne dit rien. Elle n'appelle pas. Elle attend.

* to_her | Aller vers la vieille femme
  [lean sociability +2]
  {A} fait trois pas, s'arrête, en fait quatre autres. La vieille femme ne bouge pas, mais quelque chose se détend dans ses épaules.
  Quand {A} arrive contre son genou, elle pose la main sur son chapeau, très légèrement, comme on vérifie qu'une pâte a levé. « Te voilà. »
  [flag came_to_her] [remember lichen.came_first] [-> the_cabin]

* stay | Rester contre la Souche
  [lean boldness -2] [stress rowdy 10]
  {A} reste {a:collé|collée} à l'écorce jusqu'à midi. Sous la joue, le bois est tiède, et quelque chose y bat, très lentement, bien plus lentement qu'un cœur.
  La vieille femme ne bouge pas non plus. {a:Ils|Elles} sont deux à savoir attendre.
  Le soir venu, c'est elle qui se lève, et {A} la suit, parce que la nuit tombe et qu'elle est la seule chose qui bouge.
  [flag heard_heartbeat] [-> the_cabin]

* dig | Gratter la terre au pied de la Souche  [needs trait rowdy]
  [lean energy +2]
  {A} gratte. Sous la terre noire, il y a des fils blancs, fins comme des cheveux, qui courent dans tous les sens. {A} en attrape un, et tire.
  La vieille femme est là d'un coup, bien plus vite qu'on ne l'en aurait crue capable. Elle dégage le fil des doigts de {A}, le recouche, le recouvre de terre, et dit, à la terre, pas à {A} : « Pardon. {a:Il est neuf|Elle est neuve}. »
  [flag saw_trame] [remember lichen.dug_at_souche] [-> the_cabin]

* wood | Partir vers les grands arbres  [needs trait curious]  [roll courage 5]
  [lean boldness +2]
  ? success
    {A} trottine jusqu'aux fougères, renifle l'ombre, écoute un long moment quelque chose que personne d'autre n'entend, puis revient de {a:lui|elle}-même, le bout du chapeau trempé.
    La vieille femme n'a pas bougé. Elle a seulement compté les pas, à l'aller et au retour.
    [grow courage] [-> the_cabin]
  ? failure
    {A} s'enfonce sous les fougères et ne retrouve plus la lumière. La nuit tombe. Le bois craque de partout.
    Ce n'est pas la vieille femme qui {a:le|la} retrouve. C'est une autre, mince et pâle, qui luit très doucement sous sa capuche, d'un vert de ver luisant. Elle porte {A} jusqu'à une cabane sans un mot, {a:le|la} pose sur le seuil, et repart avant qu'on ait pu la remercier.
    [stress 10] [flag met_stranger] [-> the_cabin]

## scene: the_cabin
title: La cabane de la Lisière
pace: next_day
public: {A} découvre la cabane de la Mère Lichen, et le tas de galets devant sa porte.

La vieille femme s'appelle la Mère Lichen. C'est du moins comme ça que tout le monde l'appelle. Sa cabane sent la pierre mouillée et la fumée froide.
[if flag came_to_her] La première nuit, elle a laissé {A} dormir contre elle, et n'a pas bougé de la nuit.
[if flag saw_trame] Elle n'a pas reparlé des fils blancs. Mais ce matin, elle a regardé les mains de {A} un long moment.
[if flag met_stranger] Elle n'a pas demandé qui avait ramené {A}. Elle a seulement regardé vers le Communal, longtemps.
Devant la porte, il y a un tas de galets de rivière, gris et lisses, des dizaines et des dizaines. Aucun ne porte de nom.

Ce matin, assise sur sa pierre, la Mère Lichen lève les yeux vers le vide, juste au-dessus de {A}, et fait un petit signe du menton, comme on salue quelqu'un qu'on connaît. Il n'y a personne.

* hum | Fredonner l'air qui lui trotte dans la tête
  [lean sociability +1]
  {A} fredonne un air. Trois notes qui montent, une qui hésite, et tout recommence. {A} ne sait pas d'où {a:il|elle} le tient.
  La Mère Lichen s'arrête d'écosser ses fèves. Elle écoute jusqu'au bout, sans bouger. Puis, elle qui ne pose jamais de questions : « Où as-tu pris ça ? »
  Et, comme {A} ne sait pas : « Demain, on descend au clos. Il y a quelqu'un qui doit entendre ça. »
  [flag knows_song] [-> the_orchard]

* stones | Compter les galets devant la porte
  [lean rationality +2]
  {A} compte. Il y en a trop. {A} recommence, et se trompe encore.
  « Un par mort que j'ai couché à la Souche », dit la Mère Lichen. Elle en montre un, sans le toucher. « Celui-là, c'était un meunier. Il riait fort. »
  À l'écart du tas, il y en a un plus petit, que le soleil a chauffé. {A} tend la main. « Celui-là n'est à personne. Pas encore. »
  Ce soir, elle veillera la Souche, comme chaque nuit de pluie. Elle ne propose pas à {A} de venir. Elle laisse la porte ouverte.
  [grow learning] [flag counted_stones] [-> the_vigil]

* look_up | Regarder là où elle a fait signe
  [lean zeal +2]
  {A} lève la tête. Il n'y a rien, qu'une branche et le ciel. Puis, sans savoir pourquoi, {A} fait le même petit signe du menton, vers le même vide.
  La Mère Lichen le voit. Elle sourit, et c'est la première fois. « Bon. Tu es {a:veillé|veillée}. »
  L'après-midi même, elle fait porter un message au hameau par un gamin qui passait.
  [flag greeted_keeper] [-> the_heather]

## scene: the_orchard
title: Le clos du Tilleul
pace: next_day
public: {A} descend au clos du Tilleul avec la Mère Lichen.

Le clos du Tilleul est un verger en pente, plein de ruches et d'herbe haute. Le vieux Tilleul est là, grand, voûté, deux abeilles sur l'épaule, et il parle déjà avant qu'on soit arrivé : du temps, de son genou, d'un essaim de l'an passé qui s'était posé sur le puits.
« Chante-lui », dit la Mère Lichen.
{A} fredonne les trois notes qui montent, celle qui hésite. Le Tilleul s'arrête au milieu d'un mot. Les abeilles aussi, on dirait.

« C'est la chanson de ma femme. Elle la chantait aux ruches. Plus personne ne la sait, depuis l'hiver des fièvres. »
Puis, tout bas, à la Mère Lichen, et il croit sans doute que personne d'autre n'entend : « Ysaure… d'où {a:il|elle} la tient ? »
Elle répond : « Pas encore. »
[if flag heard_heartbeat] {A} se souvient du battement, sous l'écorce de la Souche. La chanson a le même pas.

* sing_again | Chanter encore, pour lui
  [lean compassion +2]
  {A} chante encore, plus fort, et cette fois sans hésiter sur la quatrième note. Le Tilleul s'assoit sur une ruche renversée et ferme les yeux.
  Les abeilles sortent, une à une, et viennent se poser sur {A}. Aucune ne pique. Le vieux pleure sans s'essuyer, comme on laisse pleuvoir.
  À la fin, il roule entre ses doigts une boulette de cire, longtemps, sans rien dire.
  [grow diplomacy] [remember tilleul.knew_the_song] [flag sang_for_him] [-> the_boar]

* bees | Aller voir les ruches de plus près
  [lean boldness +1]
  {A} s'approche des ruches. Le Tilleul suit en boitant. « Il faut tout leur dire, aux abeilles. Les naissances, les morts. Sinon elles s'en vont. »
  Il se penche vers la plus vieille ruche et lui annonce, très fort, parce qu'il est à moitié sourd : « Une pousse est arrivée. Elle sait la chanson. »
  Puis il continue, plus bas, d'autres nouvelles, d'autres années. Il est question de la Mère Lichen, d'une nuit de pluie, de lumières. {A} n'entend pas le reste.
  [flag heard_the_bees] [-> the_boar]

* say_name | Répéter « Ysaure » tout haut  [needs trait charming]
  [lean sociability +2]
  « Ysaure », répète {A}, très {a:content|contente} de {a:lui|elle}-même, assez fort pour que tout le clos l'entende.
  La Mère Lichen se fige. Le lichen fonce sur ses joues. Le Tilleul, lui, rit comme un jeune homme, et il en tousse. « Tu vois ? Il y en a au moins {a:un|une} qui t'appelle encore comme ça. »
  « Ça suffit », dit-elle. Mais sur le chemin du retour, elle marche moins vite, pour que {A} suive.
  [grow diplomacy] [remember lichen.said_ysaure] [flag said_ysaure] [-> the_boar]

## scene: the_vigil
title: La veille
pace: next_day
public: {A} passe la nuit à la Souche avec la Mère Lichen.

La nuit venue, {A} a suivi la Mère Lichen jusqu'à la Souche. Elle s'assoit sur sa pierre et ne dit rien pendant une heure.
Puis elle montre le pré, à côté, où l'herbe est plus verte qu'ailleurs, sans une pierre, sans un nom. « Le Pré des Retours. On y couche les morts, et la Souche les reprend. Tout le monde. Les pousses aussi, à la fin. »

Plus tard, au bord du Communal, une lueur verte passe entre les troncs, très pâle. Elle s'arrête, et repart. Quelqu'un cueille, dans le noir.
« L'Étrangère », dit la Mère Lichen. « Laisse-la. »
[if flag met_stranger] {A} la reconnaît : c'est elle qui l'a {a:ramené|ramenée} jusqu'au seuil, le premier soir.

* follow_light | Suivre la lueur verte  [roll intrigue 5]
  [lean boldness +2]
  ? success
    {A} se glisse sous les fougères sans un bruit. La lueur s'est arrêtée au bord du Bois Sourd. L'Étrangère est à genoux, face aux grands arbres, immobile, comme on regarde une maison où l'on n'entre plus.
    Elle sent {A} arriver. Elle ne {a:le|la} chasse pas. {a:Ils restent là tous les deux|Elles restent là toutes les deux}, longtemps.
    Puis elle dit, sans se retourner : « Toi, tu as quelqu'un. » Ce n'est pas une question. En se relevant, elle effleure le chapeau de {A}, et un brin de mousse y reste, qui luit.
    [grow intrigue] [remember etrangere.sat_together] [flag met_stranger] [-> the_boar]
  ? failure
    {A} se prend le pied dans une racine. Aussitôt, la lueur s'éteint : elle a rabattu sa capuche.
    Un long silence. Puis sa voix, tout près, dans le noir : « Rentre. Tu as quelqu'un qui t'attend. Moi, non. »
    Quand {A} revient à la Souche, la Mère Lichen n'a pas bougé. Elle a tout vu, évidemment.
    [flag sent_back] [-> the_boar]

* stay_vigil | Rester près de la Mère Lichen
  [lean honor +2]
  {A} reste. La Mère Lichen dit un mot par heure, à peu près. Une fois : « Froid. » Une fois, quand un renard passe : « Renard. » Une fois, sans raison : « Bien. »
  À l'aube, elle se lève, fait craquer son dos, et dit la phrase la plus longue de la nuit : « Tu sais attendre. C'est rare, chez les pousses. »
  [flag kept_vigil] [-> the_boar]

* ask_dead | Demander ce que la Souche fait des morts  [needs trait pensive]
  [lean rationality +2]
  La Mère Lichen ne répond pas tout de suite. Elle ne répond jamais tout de suite. « Elle les garde. Là-dessous, dans les fils blancs. »
  « Et après ? »
  Elle regarde {A} longtemps, de haut en bas, comme on cherche quelqu'un dans une foule. « Après, ça pousse. »
  Elle n'en dira pas plus. Ni cette nuit, ni peut-être jamais.
  [flag asked_the_dead] [-> the_boar]

## scene: the_heather
title: La Bruyère
pace: next_day
public: La Bruyère monte à la Lisière voir la nouvelle pousse.

Une petite femme dure et noueuse monte le chemin de la Lisière, des fleurs violettes plein les branches, qu'elle ne remarque pas. C'est la Bruyère, qui tient l'Orphelinat, au hameau.
Elle ne dit pas bonjour. Elle soulève le chapeau de {A}, regarde dessous, compte ses doigts. Puis elle sort une tranche de pain de sa poche : « Mange. »
« Dans quelques jours, tu descends chez moi. Ici, on se lève, on lave, on mange. »
Derrière, la Mère Lichen dit : « Tu leur fais peur. » La Bruyère : « Tu les gâtes. » C'est, visiblement, une conversation qu'elles ont depuis vingt ans.

* eat | Manger le pain jusqu'à la dernière miette
  [lean greed +1]
  {A} mange tout, et les miettes tombées sur le ventre aussi.
  La Bruyère regarde faire, les bras croisés. « Bon. {a:Celui-là|Celle-là} vivra. »
  Plus tard, la Mère Lichen explique que dans la bouche de la Bruyère, c'est une déclaration d'amour. Ce n'est pas tout à fait une plaisanterie.
  [lift hunger 0.2] [flag ate_her_bread] [-> the_boar]

* hide | Se cacher derrière la Mère Lichen
  [lean sociability -2]
  {A} file derrière la Mère Lichen et ne laisse dépasser qu'un œil.
  « Pas de ça chez moi », dit la Bruyère. Mais elle pose le pain sur la pierre, à portée de main, et recule de deux pas. Elle a l'habitude.
  En redescendant, elle se retourne une seule fois. {A} la regardait encore.
  [flag hid_from_her] [-> the_boar]

* show_way | Lui montrer du doigt le chemin du retour  [needs trait bossy]
  [lean boldness +2]
  {A} tend le bras vers le hameau, très droit, et ne dit rien. Le sens est clair.
  Il y a un silence. Puis la Bruyère rit, d'un rire sec et court que la Mère Lichen n'avait pas entendu depuis des années.
  « Toi, je te garde à l'œil. » Et, à la Mère Lichen, en partant : « {a:Celui-là|Celle-là}, je {a:le|la} garde. »
  [remember bruyere.pointed_the_way] [flag pointed_the_way] [-> the_boar]

## scene: the_boar
title: La nuit des sangliers
pace: next_day
public: Les sangliers ont retourné la Lisière dans la nuit.

Cette nuit, des sangliers sont sortis du Bois Sourd. Ils ont labouré la Lisière à la recherche de racines, jusqu'au bord du Pré des Retours.
Au matin, la terre est retournée partout, et partout dépassent des fils blancs, arrachés, emmêlés, comme une laine qu'on aurait tirée d'un coussin.
La Mère Lichen est déjà à genoux. Elle recouche les fils un par un, les recouvre, tasse la terre du plat de la main. Ses mains tremblent. « Ils ne savent pas. Ils ont faim, c'est tout. »
[if flag saw_trame] {A} reconnaît les fils blancs du premier jour.
[if flag asked_the_dead] {A} se souvient : « Là-dessous, dans les fils blancs. »
[if flag heard_heartbeat] Près de la Souche, {A} pose la joue par terre. Le battement lent est toujours là, mais il hésite.
[if flag kept_vigil] Elle n'a presque pas dormi depuis deux nuits, et ça se voit.

* help | Aider à recouvrir les fils
  [lean compassion +2]
  {A} pousse la terre avec ses petites mains. Un fil cassé lui reste entre les doigts, et quelque chose passe.
  Une odeur de pain chaud. Une cuisine où {A} n'est jamais {a:entré|entrée}, une table, un rire d'homme. Puis plus rien.
  {A} lâche le fil. La Mère Lichen a vu son visage. Elle ne demande rien. Elle dit seulement, très bas : « Toi aussi, tu l'as senti. »
  [remember lichen.helped_trame] [flag felt_bread] [-> the_market]

* chase | Suivre les traces des sangliers  [needs trait rowdy]  [roll courage 5]
  [lean boldness +3]
  ? success
    {A} suit les traces jusqu'au bord du Bois Sourd, et tombe nez à nez avec une laie et ses petits. Elle est cent fois plus grosse. {A} crie quand même.
    La laie, sans doute surprise de se faire crier dessus par un champignon, fait demi-tour et emmène les siens sous les arbres.
    Quand {A} revient, la Mère Lichen dit seulement : « Tu as crié sur un sanglier. » On ne sait pas si c'est un reproche.
    [grow courage] [flag chased_boars] [-> the_market]
  ? failure
    {A} suit les traces jusqu'au bord du Bois Sourd. La laie se retourne, grogne, et charge. {A} roule dans le fossé, la tête la première, et y reste jusqu'à ce que le bruit s'éloigne.
    {A} revient {a:couvert|couverte} de boue, sans rien de cassé que la fierté. La Mère Lichen {a:le|la} lave au seau, sans un mot.
    [stress 15] [flag chased_boars] [-> the_market]

* turn_away | Retourner se coucher
  [lean energy -2] [lean compassion -1]
  {A} regarde un moment la vieille femme à genoux dans la boue, puis rentre à la cabane et se roule en boule près du feu éteint.
  Elle travaille seule jusqu'à midi. Quand elle rentre, elle ne dit rien. Elle ne dit jamais rien. Mais ce jour-là, son silence a un poids.
  [flag turned_away] [-> the_market]

## scene: the_market
title: Le jour du marché
pace: next_day
public: {A} descend au hameau pour la première fois. C'est jour de marché.

Pour la première fois, la Mère Lichen emmène {A} au hameau. On passe le Pont-Vieux, dont elle touche la pierre en passant, comme tout le monde, puis on monte jusqu'à la place du Puits.
Il y a du monde, du bruit, des odeurs de tout. Une femme ronde et orange, des vrilles partout, fond sur {A}, l'appelle « mon chou », lui demande s'il est gentil, son veilleur, s'il est grand, s'il est là, maintenant, et n'attend aucune réponse. C'est la Courge, de l'auberge.
[if flag chased_boars] La nouvelle est arrivée avant {A} : au hameau, on parle déjà de la pousse qui a crié sur une laie.
[if flag turned_away] La Mère Lichen marche un pas devant, sans se retourner.

Près du puits, un homme maigre à la tête jaune ébouriffée compte des pierres à voix haute. En voyant {A}, il s'arrête, lève les yeux juste au-dessus de son chapeau, et dit poliment, comme à quelqu'un de présent : « Bonjour, vous. {a:Il est bien, celui-là|Elle est bien, celle-là}. »
« Ça, c'est le Fou, mon chou », dit la Courge. « Il est gentil. »
Deux garçons du Noyer, assis sur le muret, éclatent de rire. « Pissenlit parle aux nuages ! Pissenlit et ses lumières ! »
[if flag heard_the_bees] Des lumières. Le Tilleul en parlait à ses abeilles.
[if flag greeted_keeper] {A} connaît ce regard-là, juste au-dessus du chapeau. La Mère Lichen a le même.

* wave | Saluer le vide avec le Fou
  [lean compassion +2]
  {A} lève les yeux au même endroit que lui, et fait le petit signe du menton.
  Les garçons rient plus fort, puis moins, puis plus du tout : c'est moins drôle quand ils sont deux. Le plus jeune regarde ses pieds.
  Le Fou dévisage {A} longtemps. Puis il se tourne vers le vide, ravi : « Vous voyez ? {a:Il|Elle} vous connaît. »
  [remember fou.waved_with_him] [flag waved_with_fou] [-> the_last_evening]

* laugh | Rire avec les garçons
  [lean sociability +2] [lean compassion -2]
  {A} rit. C'est facile, de rire avec ceux qui rient. Le plus grand des garçons donne une tape dans le dos de {A}, content d'avoir du renfort.
  Le Fou ne regarde pas {A}. Il reprend son compte là où il s'était arrêté : « Six, sept, huit pierres. » La Courge, elle, regarde {A}, et ne dit plus « mon chou ».
  La Mère Lichen, qui ne regarde jamais le Fou en face, regarde ses pieds.
  [remember fou.laughed] [-> the_cold_evening]

* bread | Suivre l'odeur du pain jusqu'au four
  [lean greed +1]
  {A} se faufile entre les jambes jusqu'au four banal, où un jeune homme doré et timide, couvert de farine, sort les miches. C'est Pain-Blond, le boulanger.
  Il casse la croûte du premier pain de la fournée et la tend à {A}. « C'est pour les veillés. Je l'ai toujours fait. » Personne ne le lui a appris.
  [if flag felt_bread] {A} s'arrête sur le seuil. Cette odeur, cette pièce, cette table. {A} n'y est jamais {a:entré|entrée}, et pourtant {A} les connaît.
  [if flag felt_bread] Au fond du fournil, une vieille femme aveugle lève le nez, renifle, et dit d'une voix changée : « Colin… cette pousse sent comme sentait ton père. »
  [remember pain_blond.ate_crust] [flag ate_crust] [-> the_last_evening]

* scold | Dire aux garçons de se taire  [needs trait bossy]
  [lean boldness +2] [lean honor +1]
  {A} se plante devant le muret, les poings serrés. « Taisez-vous. »
  Les garçons sont dix fois plus grands. Ils se taisent quand même, de surprise. Le plus jeune descend du muret le premier.
  Le Fou n'a rien remarqué, ou fait semblant. La Courge, elle, a tout vu. « {a:Celui-là|Celle-là}, mon chou, {a:il|elle} ira loin. Ou en prison. »
  [remember courge.stood_up] [flag scolded_boys] [-> the_last_evening]

## scene: the_last_evening
title: Un soir à la Lisière
pace: next_day
public: Un soir à la Lisière, avant de descendre à l'Orphelinat.

C'est l'un des derniers soirs à la Lisière. Bientôt, la Bruyère viendra chercher {A} pour l'Orphelinat, comme elle vient chercher toutes les pousses.
[if flag ate_her_bread] {A} se souvient de son pain. Il était bon.
[if flag hid_from_her] {A} n'est pas {a:sûr|sûre} d'avoir envie de la revoir.
[if flag pointed_the_way] {A} a déjà prévu de lui montrer le chemin du retour une deuxième fois.

La Mère Lichen est assise devant la porte, près du tas de galets. Elle prend un petit galet, à l'écart du tas, et le pose par terre, entre elle et {A}. Elle ne le tend pas. Elle attend.
[if flag counted_stones] {A} le reconnaît : c'est celui qui n'était à personne.
[if flag came_to_her] Comme le premier matin, elle laisse {A} venir.
[if flag turned_away] Elle a mis longtemps, ce soir, à le sortir de sa poche.
[if flag waved_with_fou] Elle a su, pour le Fou. Tout se sait au hameau avant même qu'on soit rentré. Elle n'en dit rien, mais elle regarde longtemps du côté de la place.
[if flag scolded_boys] La Courge est montée à la Lisière dans l'après-midi, rien que pour raconter les garçons du Noyer. Deux fois.
[if flag ate_crust] {A} a gardé un bout de la croûte des veillés. Il n'en reste presque rien.
[if flag sent_back] Au loin, du côté du Communal, passe une lueur verte. {A} entend encore la voix dans le noir : « Moi, non. »
[if flag knows_song] Et {A} a de nouveau dans la tête les trois notes qui montent.

* take_stone | Prendre le galet
  [lean sociability +1]
  {A} ramasse le galet. Il est tiède. Il tient tout entier dans la main.
  La Mère Lichen hoche la tête, une fois. « Celui-là n'ira pas sur le tas. Pas encore. »
  [-> lisiere]

* sing | Fredonner la chanson du clos  [needs flag knows_song "Connaît la chanson du clos"]
  [lean compassion +1]
  {A} fredonne les trois notes qui montent, celle qui hésite. La Mère Lichen ferme les yeux.
  « Le Tilleul voudra l'entendre encore », dit-elle. « Va, demain. Il t'attendra. » Le galet reste par terre. Elle le ramasse, et le garde.
  [-> the_song]

* light | Aller vers la lueur du Communal  [needs flag met_stranger "A croisé l'Étrangère"]
  [lean boldness +2]
  {A} laisse le galet par terre et part vers la lueur verte. La Mère Lichen ne {a:le|la} retient pas. Elle ne retient jamais personne.
  L'Étrangère est assise au bord du Communal, la capuche baissée. Elle ne dit pas de partir. Ce soir, elle laisse {A} regarder sa lumière de près.
  [-> the_light]

## scene: the_cold_evening
title: Un soir sans un mot
pace: next_day
public: Un soir froid à la Lisière.

La Mère Lichen n'a pas dit un mot depuis la place du Puits. Ni sur le pont, ni sur le chemin, ni ce soir.
Elle est assise devant la porte, près du tas de galets, un petit galet dans la main. Elle le tourne, et le retourne. Elle ne le pose pas.
[if flag greeted_keeper] Hier encore, elle saluait le vide au-dessus de {A}. Ce soir, elle ne lève pas les yeux.
Au hameau, dans la grange de l'auberge où il dort quand il fait froid, le Fou est sans doute en train de compter quelque chose.

* find_fou | Redescendre au hameau, trouver le Fou
  [lean compassion +2] [lean honor +1]
  {A} redescend {a:seul|seule} dans le noir, passe le Pont-Vieux en touchant la pierre, comme on le lui a montré, et trouve la grange de l'auberge.
  Le Fou est assis dans la paille et compte des clous à la lueur d'une chandelle. Il ne dit pas que ce n'est rien. Il ne dit rien du tout. Il pousse seulement la moitié des clous vers {A}.
  Ils comptent ensemble jusqu'à ce que la chandelle s'éteigne. « Six », dit le Fou, à la fin. « Il y en a six. C'est le bon compte. »
  Quand {A} remonte à la Lisière, la Mère Lichen est toujours devant la porte. Le galet est posé par terre.
  [remember fou.came_back] [flag made_amends] [-> lisiere]

* sulk | Rester dans son coin
  [lean energy -2]
  {A} reste dans son coin, près du feu éteint, et finit par s'endormir.
  Au matin, le petit galet est retourné à l'écart du tas, là où il était. Elle n'en reparle pas.
  [-> wary]

## ending: lisiere
title: La pousse de la Lisière

Les derniers jours passent comme passent les jours à la Lisière : lentement, en silence, et sans qu'on s'en aperçoive.
[if flag made_amends] Au hameau, on raconte que le Fou a une pousse qui vient compter avec lui, certains soirs. Il dit qu'{a:il|elle} compte juste.
[if flag kept_vigil] Certaines nuits, {A} va encore s'asseoir près de la Souche avec elle, sans qu'aucune des deux ait besoin de le demander.
[if flag felt_bread] Parfois, en passant devant le four banal, {A} s'arrête sans savoir pourquoi.

Quand la Bruyère vient chercher {A} pour l'Orphelinat, {A} a le galet dans la main. La Mère Lichen ne dit pas au revoir. Elle fait le petit signe du menton, vers le vide, juste au-dessus de {A}. Puis, une seule fois, vers {A}.
[keepsake lichen_pebble] [cailloux 40]

## ending: the_song
title: La chanson du clos

Le lendemain, {A} redescend au clos du Tilleul, et le surlendemain aussi. Le vieux parle du temps, de son genou, d'un essaim de l'an passé. Puis il se tait, et {A} chante. Les abeilles sortent toujours à la quatrième note.
[if flag sang_for_him] Un jour, il finit par la donner, la boulette de cire qu'il roulait entre ses doigts la première fois.
[unless flag sang_for_him] Un jour, en partant, il glisse dans la main de {A} une boulette de cire.
Elle sent le miel et la fleur de tilleul.
[if flag said_ysaure] Devant {A}, il ne l'appelle plus autrement qu'Ysaure. La Mère Lichen fait semblant de ne pas entendre.

Quand la Bruyère vient chercher {A} pour l'Orphelinat, le Tilleul est là, au bord du chemin, sorti de son clos pour la première fois depuis des années. Il ne dit rien. Il fredonne.
[keepsake tilleul_wax] [cailloux 40]

## ending: the_light
title: La lueur du Communal

Les derniers jours, {A} passe ses soirs au bord du Communal, près de la cabane de l'Étrangère. Elle parle peu, avec des tournures d'ailleurs, et répond aux questions par d'autres questions. Elle ne demande jamais à {A} d'où {a:il|elle} vient. Elle demande où {a:il|elle} va.
[if flag sent_back] Elle ne dit plus « Rentre ». Elle ne dit plus « Moi, non ». Elle laisse {A} rester.
Un soir, {A} lui dit que sa lumière est belle. Elle répond que chez elle, on la trouvait plus vive. Puis elle remonte sa capuche.

Quand la Bruyère vient chercher {A} pour l'Orphelinat, un brin de mousse pâle est resté sur son chapeau. Il luit encore, un peu, le premier soir.
[keepsake stranger_moss] [cailloux 40]

## ending: wary
title: Sans au revoir

Les derniers jours à la Lisière passent sans un mot de trop. La Mère Lichen nourrit {A}, {a:le|la} couvre la nuit, ne {a:le|la} quitte pas des yeux. Mais elle ne fait plus le petit signe du menton au-dessus de son chapeau.
[if flag turned_away] Les fils blancs, après les sangliers, elle les a recouverts seule. Elle ne l'a pas oublié.
Au hameau, le Fou compte toujours ses pierres. Quand {A} passe, il compte un peu plus fort.

Quand la Bruyère vient chercher {A} pour l'Orphelinat, la Mère Lichen dit seulement : « {a:Celui-là|Celle-là}, tiens-{a:le|la}. » La Bruyère répond : « Je les tiens tous. » Et elle emmène {A} sans lui tenir la main.
[epithet wild_one] [cailloux 20]

## ending: interrupted
title: {a:Rendu|Rendue} à la Souche

{A} n'a pas fini sa première semaine. La Mère Lichen l'a {a:couché|couchée} au Pré des Retours, à côté de la Souche, sans pierre et sans nom, comme tout le monde.
Puis elle a ajouté un galet au tas devant sa porte. Le petit, celui qui était à l'écart.
