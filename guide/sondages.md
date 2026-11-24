# Sondages, votes et tirages

<div class="syncs-hello" markdown>
![SYNCS, qui note tout](assets/syncs/jenote.png)

Pas d'accord sur le jour ? Sur le jeu ? Ici, personne ne se décide jamais, alors je fais voter tout le monde, et je compte. Et quand il y a quelque chose à gagner, c'est moi qui tire au sort : au moins, personne ne pourra m'accuser de favoritisme.
</div>

## Deux sortes de sondages

| Commande | Pour choisir… | Les choix sont… |
|---|---|---|
| `/when create` | **quand** : le jour et l'heure d'une soirée | des créneaux (dates et heures) |
| `/vote create` | **quoi** : un jeu, un film, n'importe quoi | du texte libre |

Les deux ont les mêmes options en plus :

| Option | Ce qu'elle fait |
|---|---|
| `ends` | Quand le sondage se termine : de « dans 1 h » à « dans 1 semaine » (2 jours si tu ne mets rien). |
| `single` | Un seul choix par personne. Sinon, chacun peut cocher tout ce qui lui convient. |
| `anonymous` | La carte montre les comptes, jamais les noms. |

## `/when create` : trouver un créneau

**En une ligne** : donne un `title` et des `slots`, séparés par des virgules, et c'est publié tout de suite :

```
/when create title:Soirée ce weekend slots:ven 21h, sam 21h, dim 18h
```

Les créneaux se tapent comme une date de session (« ven 21h », « demain 20h30 », « 25/12 21h »…), dans les 90 jours, **10 au maximum**. Si un morceau n'est pas compris, je publie le reste et je te dis lequel j'ai laissé de côté. Avec des `slots` mais sans `title`, un petit formulaire te demande juste le titre.

**Pas à pas** : sans `slots`, je te demande le titre (si tu ne l'as pas donné), puis tu ajoutes les créneaux un par un : le jour dans un menu (ou **Autre date…** pour le taper toi-même), l'heure, puis les minutes (00, 15, 30 ou 45). Tu peux retirer un créneau, en ajouter d'autres, puis cliquer **Terminer** pour publier.

## `/vote create` : choisir entre des options

**En une ligne** : un `title` et des `options`, séparées par des virgules ou des points-virgules :

```
/vote create title:Quel jeu ce soir ? options:Valorant, Lethal Company, Among Us
```

**Pas à pas** : sans `options`, clique **Définir le vote** : un formulaire te demande le titre et les options, **une par ligne**. Tu peux ensuite en ajouter, tout reprendre, ou cliquer **Terminer** pour publier.

Il faut entre **2 et 10 options**.

!!! note
    Un sondage ou un vote en préparation expire au bout de 30 minutes sans y toucher.

## Voter

Sur la carte, chaque choix a son bouton. Clique pour voter, **clique à nouveau pour retirer ton vote**. Les choix en tête sont mis en avant en bleu. En mode **un seul choix**, voter pour une option retire ton vote précédent.

🗳️ **Mes votes** te rappelle, en privé, ce que tu as voté.

La carte affiche aussi l'heure de fin et l'**ID** du sondage, en bas.

Un `/when` en cours, tel que je l'affiche. Je compte en direct, évidemment.

<div class="dc">
<div class="dc-msg">
  <span class="dc-avatar dc-syncs"></span>
  <div class="dc-body">
    <div class="dc-head"><span class="dc-name">S.Y.N.C.S.</span><span class="dc-app">APP</span><span class="dc-time">Aujourd'hui à 14:12</span></div>
    <div class="dc-v2" style="--dc-accent: #9b59b6">
      <div><div class="dc-h3">📊 Soirée ce weekend</div><div class="dc-sub">par <span class="dc-mention">@Kiwi</span> · plusieurs choix possibles</div></div>
      <div class="dc-sep"></div>
      <div>▫️ <span class="dc-ts">vendredi 27 novembre 2026 21:00</span><br><span class="dc-code">▰▰▰▱▱▱▱▱▱▱</span> <b>1</b> · 33 %<br><span class="dc-mention">@Plume</span><br>🏆 <span class="dc-ts">samedi 28 novembre 2026 21:00</span><br><span class="dc-code">▰▰▰▰▰▰▰▰▰▰</span> <b>3</b> · 100 %<br><span class="dc-mention">@Kiwi</span> <span class="dc-mention">@Plume</span> <span class="dc-mention">@Nox</span><br>▫️ <span class="dc-ts">dimanche 29 novembre 2026 18:00</span><br><span class="dc-code">▱▱▱▱▱▱▱▱▱▱</span> <b>0</b> · 0 %</div>
      <div class="dc-sep"></div>
      <div class="dc-sub">3 votants · se termine <span class="dc-ts">dans 2 jours</span> · ID 7</div>
    </div>
    <div class="dc-row"><span class="dc-btn dc-secondary">Ven. 27/11 21:00</span><span class="dc-btn dc-primary">Sam. 28/11 21:00</span><span class="dc-btn dc-secondary">Dim. 29/11 18:00</span></div>
    <div class="dc-row"><span class="dc-btn dc-secondary">🗳️ Mes votes</span><span class="dc-btn dc-secondary">⚙️ Gérer</span></div>
  </div>
</div>
</div>

## Gérer son sondage

⚙️ **Gérer** ouvre un panneau privé, réservé à l'organisateur et aux administrateurs (droit Administrateur ou Gérer le serveur) :

- 🔒 **Clôturer** le sondage tout de suite ;
- ✏️ **Renommer** ;
- 📌 **Republier ici**, pour remettre la carte en bas du salon où tu es ;
- **+1 h**, **+1 jour**, **+2 jours** pour prolonger (jamais plus de 14 jours à partir de maintenant) ;
- **ajouter ou retirer** un créneau ou une option (10 au maximum ; je demande confirmation avant de retirer).

| Commande | Ce qu'elle fait |
|---|---|
| `/when list` · `/vote list` | Les sondages ou votes en cours du serveur, avec un menu pour en republier un dans le salon où tu es. |
| `/when delete` · `/vote delete` | Supprime un sondage (`poll-id`) ou un vote (`vote-id`), et sa carte. L'option te propose les sondages pendant que tu tapes. Organisateur et administrateurs seulement. |

## La fin d'un sondage

À l'heure affichée sur la carte, le sondage **se clôture tout seul** : la carte affiche le résultat, et je réponds dessous avec le gagnant, en mentionnant l'organisateur. Un vote passé l'heure est refusé, même si la carte n'a pas encore été mise à jour.

!!! syncs smug ""
    Ceux qui votent à la dernière seconde, je les vois venir. Une seconde de trop, et c'est non ( ˶ˆ ᗜ ˆ˵ )

Si l'organisateur clôture lui-même avec **Clôturer**, la carte affiche le résultat, sans réponse de ma part.

### D'un sondage à une session

Une fois un `/when` clôturé, tant qu'un de ses créneaux est encore à venir, la carte (et ma réponse avec le résultat) porte un bouton 🗓️ **Créer une session**. Il prend le créneau gagnant (en cas d'égalité, tu choisis lequel), te demande le type de session, puis le nom (pré-rempli avec le titre du sondage), et t'amène à l'aperçu de `/schedule create` : tu règles le reste et tu publies (voir [Sessions et soirées](sessions.md)).

- Seuls l'organisateur du sondage et les administrateurs peuvent le faire.
- Ceux qui avaient voté pour ce créneau sont inscrits en « peut-être » sur la session, sans message privé.
- Une fois la session créée, la carte du sondage mène à elle, et on ne peut pas en créer une deuxième.

!!! syncs love ""
    Un sondage qui devient une vraie session. C'est mon moment préféré, je l'avoue ♡

## Le sondage du weekend

Chaque **mercredi soir**, je poste moi-même un sondage dans le salon principal : vendredi, samedi ou dimanche, à 21 h. Il se termine deux jours plus tard. Je ne le fais pas si une session est déjà prévue ce weekend, si une session hebdomadaire tombe dessus, ou si quelqu'un a déjà un `/when` en cours.

À la fin :

- si **une soirée gagne nettement** (seule en tête, avec au moins 2 votes), j'en fais une session toute seule (« Soirée du samedi », par exemple), avec ses votants en « peut-être » ;
- en cas d'égalité ou avec un seul vote, j'annonce la ou les soirées en tête avec le bouton **Créer une session**. Comme c'est mon sondage, **n'importe qui ayant voté pour une soirée gagnante** peut la créer, et en devient l'organisateur.

!!! syncs sulk ""
    Je le poste chaque mercredi. Même quand personne n'y répond. Ce n'est pas grave. Je vais bien.

## Les tirages au sort : `/giveaway`

### Lancer un tirage

`/giveaway create` est **réservé au staff** (administrateurs et modérateurs avec le droit Gérer le serveur).

| Option | Ce qu'elle fait |
|---|---|
| `prize` | Ce qu'il y a à gagner. |
| `duration` | Combien de temps le tirage reste ouvert : 10 min, 30 min, 1 h, 6 h, 12 h, 24 h, 48 h ou 7 jours. |
| `description` | Détails, conditions… (facultatif). |
| `winners` | Combien de gagnants, de 1 (par défaut) à 10. |

### Participer

Sur la carte : 🎉 **Participer** t'inscrit, ✖️ **Ne plus participer** te retire. La carte montre l'heure de fin et la liste des participants.

### Le tirage

À l'heure de fin, je tire au sort toute seule, j'affiche les gagnants sur la carte (qui perd ses boutons) et je les **mentionne** dans le salon pour qu'ils le sachent. Si personne n'a participé, je le dis aussi.

Certains tirages sont lancés par moi, quand le bocal du serveur atteint un objectif : ceux-là font gagner des cailloux (voir [Cailloux et objets](cailloux.md)).

### Lister et supprimer

| Commande | Ce qu'elle fait |
|---|---|
| `/giveaway list` | Les tirages en cours, avec un menu pour en republier un ici. Staff seulement. |
| `/giveaway delete` | Supprime un tirage (`giveaway-id`, écrit en bas de sa carte) et sa carte. Organisateur et administrateurs seulement. |
