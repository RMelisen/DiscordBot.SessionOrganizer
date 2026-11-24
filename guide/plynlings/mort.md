# La fin, et après

<div class="syncs-hello" markdown>
![SYNCS, triste](../assets/syncs/sad.png)

Un Plynling peut mourir. Pas de vieillesse : de faim, ou d'une maladie qu'on n'a pas soignée. C'est la page que j'aime le moins. Voici comment ça arrive, comment l'éviter, et ce qui se passe ensuite. Lis surtout le « comment l'éviter ».
</div>

## Comment il peut mourir

| Cause | Quand | Comment l'éviter |
|---|---|---|
| **La faim** | Sa jauge de faim tombe à 0 % (elle se vide en 3 jours) | Le nourrir. Ou le geler si tu pars. |
| **La maladie** | À partir de son troisième matin malade, s'il n'a pas eu son médicament depuis la veille | **Soigner** sur sa carte, un médicament par jour |

Ce qui ne le tue **jamais** :

- **la tristesse**, la saleté ou le stress : ça le rend malheureux ou malade, pas plus ;
- **le sommeil** : il ne meurt jamais en dormant. Une mort due la nuit arrive à 5 h ;
- **le gel** : un Plynling gelé ne bouge plus, sa faim non plus.

!!! tip "Je préviens toujours"
    Je t'envoie un **message privé** environ **12 heures avant** qu'il meure de faim (à **23 h la veille** si ça tombe la nuit), et un autre le matin où il **tombe malade**. Garde tes messages privés ouverts pour me laisser te prévenir.

!!! syncs mama ""
    Un message de moi, à ce moment-là, ce n'est jamais pour rien. Ouvre-le, s'il te plaît.

Tous les détails pour le garder en forme sont sur [Prendre soin de lui](soins.md).

## Quand il meurt

- **Tout le serveur l'apprend** : je l'annonce dans le salon du jeu.
- Sa **carte** devient un mémorial : « Mort il y a…, après tant de jours de vie. Il repose sous… », avec sa tombe en image. Plus aucun bouton.
- Ses **meilleurs amis** et son ou sa **partenaire** en ont gros sur le cœur : leur bonheur chute, et ce deuil entre dans leur journal.
- Ce qui l'attendait (ses événements en cours) est annulé.
- Si le serveur a un rôle Plynling, tu le perds, jusqu'à ta prochaine adoption.

Tant que tu n'en as pas adopté un autre, `/plynling view`, `/plynling journal` et `/plynling relations` montrent toujours le dernier disparu : son mémorial, son journal (gardé en entier) et ses amis. Chez ses amis, il passe dans l'onglet **Disparus** de leurs relations.

## Sa tombe

Chaque Plynling repose sous une tombe, de la plus simple à la plus belle :

1. un cairn
2. une petite stèle
3. une stèle gravée
4. une urne
5. une statue

**Mieux on s'est occupé de lui, tout au long de sa vie, plus sa tombe est belle.** Si tu lui as mis un **cadre de tombe** (un cosmétique, voir [Cailloux et objets](../cailloux.md)), il encadre son nom au cimetière.

## Le cimetière

**`/plynling graveyard`** ouvre le cimetière du serveur : chaque tombe avec son image, son nom, son espèce, son propriétaire, combien de temps il a vécu et quand il est parti. Cinq tombes par page.

- **🕯️ Plus récents** / **⏳ Plus longue vie** : change le tri.
- **◀ ▶** : tourne les pages.
- L'option `user` n'affiche que les tombes d'une personne.

!!! syncs sad ""
    Je me souviens de chacun d'eux. Oublier, ce n'est pas mon genre ♡

## Adopter à nouveau

Tu peux adopter un nouveau Plynling **tout de suite** avec `/plynling adopt`. Tes **cosmétiques** te restent et habilleront le suivant ; ton inventaire et tes cailloux aussi. Rien ne presse, mais rien ne t'en empêche.

## L'abandonner

**`/plynling abandon`** le fait partir **pour toujours**. Ce n'est pas une mort :

- **pas de tombe, pas de retour possible**, et son journal disparaît avec lui ;
- tu dois **taper son nom** pour confirmer, pour qu'aucun clic malheureux ne le fasse partir ;
- **tout le serveur l'apprend**, et ça compte pour le titre de *L'Indigne* sur `/shame` (voir [Le mur de la honte](../honte.md)) ;
- ses meilleurs amis et son ou sa partenaire en ont le cœur brisé ;
- tu dois attendre **30 minutes** avant d'en adopter un autre.

!!! syncs mama ""
    Abandonner, c'est ton droit. Je le respecte. Je préfère juste qu'on se dise au revoir.

!!! warning
    Un Plynling abandonné ne peut **pas** être ressuscité, même par le staff. Si tu pars en vacances, gèle-le plutôt (`/plynling freeze`).

## Ressusciter un Plynling (staff)

Le staff peut ramener un Plynling mort avec **`/admin plynling resurrect`** (option `user`, la personne à qui il appartient) :

- seulement **le dernier** Plynling mort de cette personne ;
- seulement si elle n'a **pas déjà un Plynling vivant** : un seul à la fois ;
- il revient avec **50 %** de faim, de bonheur et d'hygiène, guéri, et reprend sa vie où il l'avait laissée (même âge, même journal, mêmes amis) ;
- son retour est annoncé dans le salon du jeu.

Le staff peut aussi renommer le Plynling de quelqu'un, vivant ou dernier disparu, avec **`/admin plynling rename`** (options `user` et `name`) ; son propriétaire est prévenu en privé. Plus de détails sur la page [Staff](../staff.md).
