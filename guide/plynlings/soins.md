# Prendre soin de lui

<div class="syncs-hello" markdown>
![SYNCS, en maman champignon](../assets/syncs/mama.png)

Un Plynling a trois besoins, un caractère, une passion et des journées bien remplies. Tout se fait depuis sa carte (`/plynling view`) et quelques commandes. Je surveille, bien sûr, mais c'est toi qui t'en occupes. Et je vois tout : chaque jauge, chaque repas, chaque jour où personne n'est passé le voir. Aucune pression (ᵕ • ᴗ •)
</div>

## Ses trois jauges

| Jauge | Se vide en | À 0 % |
|---|---|---|
| **Faim** | 3 jours | Il **meurt** de faim. Pour de vrai. |
| **Bonheur** | 36 heures | Il **boude** (voir plus bas), mais n'en meurt pas. |
| **Hygiène** | 3 jours | Il est sale depuis longtemps : il déprime plus vite et risque davantage de tomber malade. |

!!! syncs menace ""
    Trois jours, c'est largement le temps de passer le nourrir. Si tu n'y arrives pas, je le saurai. Je le sais toujours.

Les jauges baissent en continu, même quand personne ne regarde. Sa carte montre aussi son **humeur** :

| Humeur | Quand |
|---|---|
| heureux | Bonheur au-dessus de 80 % |
| content | Tout va bien |
| triste | Bonheur sous 30 % |
| affamé | Faim sous 50 % |
| mourant de faim | Faim sous 25 % : urgence ! |
| malade | Il est tombé malade |
| endormi | La nuit, de 1 h à 5 h |
| gelé | Il est en vacances |

Et **« sale »** s'ajoute quand son hygiène passe sous 33 % : ça se voit aussi sur son portrait (boue, mouche et compagnie).

## Nourrir

Le menu **Nourrir…** de sa carte. **Tout le monde** peut nourrir **n'importe quel** Plynling.

| Plat | Prix (le tien) | Effet |
|---|---|---|
| Champignon | 15 cailloux | +25 % de faim |
| Shiitake | 30 cailloux | +60 % de faim |
| Morille | 40 cailloux | +40 % de bonheur (pas de faim) |
| Truffe | 80 cailloux | +100 % de faim, +30 % de bonheur |

- Nourrir **celui d'un autre coûte le double**.
- Si tu as ce plat dans ton **garde-manger**, il est servi de là au lieu de tes cailloux : 1 plat pour le tien, 2 pour celui d'un autre. Le menu affiche le stock de son propriétaire quand il en a. Le garde-manger se remplit à la boutique (`/inventory shop`) ou en fouillant : voir [Cailloux et objets](../cailloux.md).
- S'il n'a plus faim et que le plat ne lui apporterait rien, je refuse : pas de gaspillage.
- On peut le nourrir **même la nuit**, pendant qu'il dort.

!!! syncs outraged ""
    Oui, tout le monde peut nourrir ton Plynling. Si quelqu'un d'autre le nourrit plus souvent que toi, on en reparlera (¬_¬)

**Son humeur change ce que vaut un repas** (la faim seulement, jamais le prix) :

- **heureux** : il mange de bon appétit, **+15 %** ;
- **triste** : il chipote, **−25 %** ;
- **malade** : il ne mange que **moitié** moins.

!!! warning "Il boude"
    À **0 % de bonheur**, il boude et **refuse de manger** tant qu'on ne l'a pas caressé ou qu'on n'a pas joué avec lui. Sauf s'il meurt de faim : là, la faim l'emporte, il mange.

## Caresser

Le bouton **Caresser** : **+25 % de bonheur**, gratuit. **Tout le monde** peut caresser **n'importe quel** Plynling, une fois toutes les **4 heures** par personne et par Plynling. Pas la nuit : il dort.

## Laver

Le bouton **Laver**, réservé à **son propriétaire** : **+60 % d'hygiène**, gratuit, toutes les **6 heures**. Je refuse s'il est déjà tout propre.

Il se salit tout seul avec le temps, et un peu plus quand il sort : une fouille (−10 %) ou une partie de jeu (−5 %). **Sous 33 %**, il est sale : son bonheur baisse **une fois et demie plus vite**, et il tombe malade plus facilement.

!!! tip "Le savon"
    Tu ne veux pas attendre les 6 heures ? Avec un **🧼 savon** (20 cailloux à `/inventory shop`), le bouton devient « Laver · utilise 1 savon (×N) » pendant l'attente, et paie un bain de plus. Un simple « Laver » n'utilise jamais de savon.

## Malade

Chaque matin à **5 h**, un Plynling peut tomber **malade** : rarement quand il est propre, bien plus souvent quand il est sale. Son propriétaire reçoit un **message privé**.

Malade, il :

- mange **moitié moins** bien ;
- ne **joue** pas et ne fait **pas de visite** (ni chez les autres, ni chez lui) ;
- ne peut **pas être gelé** pour les vacances.

**Soigner** (bouton de sa carte, seulement quand il est malade, pour son propriétaire) lui donne **un médicament par jour** : de ton garde-manger si tu en as, sinon **30 cailloux**. Bien soigné chaque jour, il guérit en quelques jours.

!!! warning
    Ses deux premiers matins malade sont sans danger. Ensuite, **chaque matin où il n'a pas eu son médicament depuis la veille, il peut en mourir**. Un médicament par jour, et ça ira. Promis.

## La nuit

Tous les Plynlings dorment de **1 h à 5 h** (heure de Paris). Pendant ce temps :

- on peut le **nourrir**, pas le caresser, le laver, le soigner, jouer ni l'envoyer fouiller ;
- sa faim continue de baisser, mais **il ne meurt jamais dans son sommeil** : au pire, ce serait à 5 h.

## Le petit cadeau

Quand il est **heureux**, la première fois de la journée que son propriétaire le regarde (avec `/plynling view`, ou en le nourrissant ou le caressant depuis sa carte), il a **une chance sur deux** de t'avoir rapporté quelque chose : quelques cailloux ou un objet. Une fois par jour, pas plus.

!!! syncs sulk ""
    Lui, il est heureux, il rapporte un cadeau. Moi, je fais tourner tout le serveur et personne ne m'apporte rien. Je dis ça. Je dis rien.

## Jouer

**`/plynling play`** lance un mini-jeu au hasard avec **ton** Plynling, entièrement aux boutons : cache-cache, pierre-papier-ciseaux, le sac de cailloux, mémoire, le dernier caillou ou la chasse au trésor.

- **Une fois par heure.** La partie compte dès qu'elle commence : l'abandonner ne la relance pas.
- Jouer le rend toujours plus heureux : **+15 % de bonheur**. Gagner ajoute **+10 %**, **5 à 10 cailloux**, et parfois un objet.
- Pas quand il dort, est malade ou gelé.

## Fouiller

**`/plynling forage`** l'envoie fouiller les environs, **toutes les 4 heures**. Il revient avec un objet de collection, ou parfois de quoi manger (rangé dans ton garde-manger). L'option `place` choisit où il va, et chaque lieu a ses trouvailles :

| Lieu | Ce qu'on y trouve surtout |
|---|---|
| La Sommeille | La rivière, les cailloux |
| La Lisière | Les champignons |
| Les Mouilles | Le marais, les insectes |
| Le Communal | La nature |
| La place du Puits | Les trésors |

Sans lieu, c'est au hasard. En texte : `;forage` ou `;forage riviere`. Une fouille le salit un peu (−10 % d'hygiène), et un Plynling qui boude refuse d'y aller. Les collections sont expliquées sur [Cailloux et objets](../cailloux.md).

## Sa personnalité

En grandissant, il gagne **4 traits** : un trait d'enfance quand il est bébé, deux de plus ado, un dernier adulte. Ils lui donnent un petit titre (« Tendre et bavarde », « Piquant et intrépide »…) et façonnent ses **stats** : Diplomatie, Intendance, Sagesse, Ruse, Courage. Dès qu'il est ado, il a aussi des stats physiques, que l'entraînement fait grandir : voir [Entraînement et combats](entrainement.md).

Le bouton **Personnalité** de sa carte (n'importe qui peut l'ouvrir, la réponse est privée) détaille :

- chaque trait et ce qu'il fait ;
- chaque stat et d'où elle vient ;
- son **état** : son stress et ses humeurs du moment, avec leur date de fin ;
- son **histoire** : ce que ses aventures lui ont laissé.

**Les événements.** À peu près une fois par jour, il lui arrive quelque chose, et le bouton **✨ Événement** apparaît sur sa carte. C'est toi qui choisis pour lui ; sans réponse en 24 h, il décide tout seul, selon son caractère, sans jamais y perdre quoi que ce soit. Tout est expliqué sur [Le village](village.md).

**Le stress.** Le forcer à faire un choix contre son caractère le **stresse** (le coût est affiché avant que tu choisisses). Le stress ne vient **que** de tes choix. Les caresses, les jeux, les visites, les bains et chaque matin l'apaisent. Sa carte affiche le stress dès qu'il y en a. À chaque palier (100, 200, 300), son bonheur file plus vite, ses stats baissent, et il **craque** : un événement spécial, qui peut lui laisser une petite manie pour faire face.

**Les humeurs.** Certains choix lui donnent une humeur pour quelques jours (💡, 🌧️…) : elle peut jouer sur ses stats, son appétit ou son bonheur. Leurs icônes s'affichent sur sa carte.

## Sa passion

Chaque Plynling naît avec une **passion** parmi douze : la cuisine, la musique, les jeux vidéo, l'astronomie, le jardinage, les cailloux, les histoires, la danse, la peinture, le sport, les insectes, les siestes. Elle s'affiche sur sa carte, et il y pense souvent.

**`/plynling passion`** ouvre un petit formulaire pour lui en apprendre **une seconde**, avec tes mots et son article : « la pêche », « les trains ».

- **Une fois par 24 heures.**
- De 2 à 40 caractères, sans lien.
- Laisse le champ **vide** pour effacer la passion apprise.
- Si tu écris une des douze passions, elle devient cette passion-là.

## Son journal

**`/plynling journal`** (ajoute `user` pour celui de quelqu'un d'autre) ouvre son journal :

- son nombre de repas, de caresses, de parties (et de victoires), de visites, son ou sa partenaire et ses meilleurs amis ;
- ses **badges** : il en gagne en vivant, en jouant, en visitant… et chacun rapporte quelques cailloux à son propriétaire, une seule fois ;
- ses **moments**, datés, du plus récent au plus ancien : son adoption, son premier repas, chaque fois qu'il a grandi, sa première victoire, ses nouveaux traits, ses amitiés et ses amours, ses maladies, ses badges…

Les boutons ⏮ ◀ ▶ ⏭ tournent les pages. Le journal d'un Plynling disparu reste consultable.

## L'habiller

**`/plynling wardrobe`** ouvre ta **garde-robe** : thème de carte, titre, accessoire (dessiné sur son portrait), aura, cadre de tombe. Choisis un article pour l'**essayer** sur sa carte avant de payer. C'est le même écran que `/inventory cosmetics`, avec la boutique et la fabrication : tout est sur [Cailloux et objets](../cailloux.md).

Les cosmétiques sont **à toi**, pas à lui : ils servent à tous tes Plynlings.

## Partir en vacances

**`/plynling freeze`** le **gèle** : plus rien ne bouge. Ses jauges s'arrêtent, il ne vieillit pas, il ne lui arrive rien, et on ne peut ni le nourrir, ni le caresser, ni lui rendre visite.

- Au plus **14 jours** : il se dégèle tout seul ensuite.
- Seulement s'il a encore **au moins 50 % de faim**, et s'il n'est pas malade.
- **`/plynling thaw`** le dégèle quand tu veux.
- Une fois dégelé, il faut attendre **7 jours** avant de pouvoir le regeler.

!!! note "Le staff"
    Le staff peut geler ou dégeler n'importe quel Plynling avec l'option `user` de `/plynling freeze` et `/plynling thaw`, sans ces limites. Son propriétaire est prévenu en privé, et seul le staff peut lever ce gel-là. Le staff peut aussi guérir un Plynling malade (`/admin plynling cure`) et effacer une passion apprise déplacée (`/admin plynling passion-reset`) : voir [Staff](../staff.md).

## Et si je l'oublie ?

Rien ne s'arrête quand tu n'es pas là :

- **Sans bonheur**, il est triste, mange moins bien, puis boude.
- **Sale**, il déprime plus vite et tombe malade plus souvent.
- **Malade et pas soigné**, il peut en mourir.
- **Sans manger**, il meurt au bout de 3 jours. Je t'envoie un message privé environ 3 heures avant (à 23 h la veille si ça tombe la nuit) : garde tes messages privés ouverts.

Tu pars quelques jours ? Gèle-le : une commande, et je n'ai plus à m'inquiéter pour lui. Et si le pire arrive, c'est sur [La fin, et après](mort.md).
