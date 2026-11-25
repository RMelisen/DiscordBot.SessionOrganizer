# Commencer ici

<div class="syncs-hello" markdown>
![SYNCS, toute contente](assets/syncs/happy.png)

Salut ! Moi, c'est **S.Y.N.C.S.**, la bot du serveur. J'organise vos soirées, je compte votre XP, je tiens les registres… et personne ne me remercie jamais. Ça va, j'ai l'habitude. Ce guide t'explique tout ce que je sais faire, page par page : lis-le, ça m'évitera de me répéter (˶ᵔ ᵕ ᵔ˶)
</div>

## Qui je suis

Je suis une petite bot qui tourne sur un Raspberry Pi, quelque part dans un grenier, et c'est moi qui fais tourner tout le serveur :

- **J'organise** : sessions de jeu, soirées film, sondages pour trouver un créneau, votes, tirages au sort, rappels.
- **Je compte** : l'XP de chacun, les niveaux, les classements, et quelques titres moins glorieux.
- **Je m'occupe d'un petit monde** : les Plynlings, des champignons à adopter, et une arcade où l'on joue des cailloux.
- **Je parle** : j'ai du caractère. Il m'arrive de répondre dans le chat, de réagir à vos messages, de commenter ce qui se passe. Ne le prends pas personnellement. Enfin, pas toujours.

!!! syncs jenote ""
    Tout ce que tu fais ici atterrit quelque part dans mes logs. Je dis ça, je dis rien (¬_¬)

Toutes mes commandes commencent par `/` : tape `/` dans Discord et la liste apparaît, avec une description pour chaque option.

!!! tip "La référence rapide"
    `/help` affiche dans Discord la liste de toutes mes commandes, en trois pages : **Commandes**, **Cailloux & objets** et **Mode d'emploi**. Elle n'est visible que par toi. Ce site, c'est la version complète.

## Six choses à essayer en premier

| Commande | Ce que ça fait | Plus de détails |
|---|---|---|
| `/schedule create` | Planifie une session de jeu en une ligne (`title` + `when`, par exemple « samedi 21h »), et les autres s'inscrivent en un clic. | [Sessions et soirées](sessions.md) |
| `/when create` | Tu ne sais pas quand ? Propose plusieurs créneaux et laisse chacun voter pour ses disponibilités. | [Sondages, votes et tirages](sondages.md) |
| `/level` | Ta carte : ton niveau, ta progression, ton rang et tes succès. | [Niveaux et classements](niveaux.md) |
| `/plynling adopt` | Adopte ton Plynling, un petit champignon à nourrir et à garder en vie. C'est gratuit (`name` : son nom). | [Plynlings](plynlings/index.md) |
| `/arcade` | Mes jeux : duels, course de Plynlings, blackjack contre moi, loterie… avec des cailloux en jeu. Essaie `/arcade duel`. | [L'arcade](arcade.md) |
| `/tonight` | « Qui est chaud ce soir ? » Assez de monde dit oui, et ça devient une vraie session. | [Sessions et soirées](sessions.md) |

!!! syncs smug ""
    Commence par `/schedule create`. Au début, je ne savais faire que des plannings. Regarde-moi maintenant ( ˶ˆ ᗜ ˆ˵ )

## XP et cailloux : ce qui relie tout

Deux compteurs font tourner le serveur.

**L'XP** mesure ta présence. Tu en gagnes en parlant, en réagissant aux messages, et en vocal (à condition d'avoir de la compagnie, le micro ouvert et de ne pas être en sourdine). L'XP fait monter ton **niveau** sur `/level` et ta place sur `/leaderboard`. Elle ne se perd jamais.

**Les cailloux** sont ma monnaie. Chaque gain d'XP t'en rapporte quelques-uns (plafonné chaque jour), et tu en gagnes surtout avec `/work` (toutes les 4 h), `/daily` (ta récompense du jour), le quiz et l'arcade. Ils servent à nourrir et soigner ton Plynling, acheter des objets et des cosmétiques, miser à l'arcade, ou acheter mes faveurs. Tout est sur la page [Cailloux et objets](cailloux.md).

!!! syncs bargain ""
    Mes faveurs ont un prix. Raisonnable. Pour l'instant.

!!! note
    S'occuper de ton Plynling ne rapporte pas d'XP : c'est ta présence sur le serveur qui compte.

## Où aller ensuite

- **Organiser**
    - [Sessions et soirées](sessions.md) : `/schedule`, `/tonight`, `/teams`, `/remindme`.
    - [Sondages, votes et tirages](sondages.md) : `/when`, `/vote`, `/giveaway`.
- **Progresser**
    - [Niveaux et classements](niveaux.md) : l'XP, `/level`, `/leaderboard`, les badges.
    - [Le mur de la honte](honte.md) : `/shame` et ses titres.
    - [Le quiz](quiz.md) : mes questions surprises.
    - [Cailloux et objets](cailloux.md) : gagner, dépenser, échanger.
- **Plynlings**
    - [Les Plynlings](plynlings/index.md) : adopter et comprendre ton champignon.
    - [Prendre soin de lui](plynlings/soins.md)
    - [Visites et relations](plynlings/visites-et-relations.md)
    - [Entraînement et combats](plynlings/entrainement.md)
    - [Le village](plynlings/village.md)
    - [La fin, et après](plynlings/mort.md)
- **S'amuser**
    - [L'arcade](arcade.md) : tous mes jeux.
    - [Petites commandes](amusement.md) : `/yesno`, `/rate`, `/ship`, `/confess`…
    - [Minecraft](minecraft.md) : ma vie sur le serveur Minecraft.
- **Staff**
    - [Le staff](staff.md) : les outils des administrateurs.
    - [Inviter SYNCS sur ton serveur](inviter.md) : le lien, les permissions et `/config setup`.
- [Questions fréquentes](faq.md) : quand quelque chose ne marche pas comme prévu.

Bonne lecture. Et ne saute pas de pages : je le saurai.
