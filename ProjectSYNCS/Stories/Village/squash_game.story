// La partie de courge — wave 1 (ado). Rewrites teen_ballgame: once a year the whole hamlet plays ball
// from one end to the other with a dried squash. Maître Frêne writes a rule for everything (fiche 2);
// the Courge wants everything that matters to happen at hers (fiche 6).

# arc: squash_game
title: La partie de courge
family: village
stage: teen
trigger: draw
size: short

## memories
frene.refereed | frene | private | +2 | A arbitré la partie de courge avec lui, et appliqué ses quarante règles. Toutes.

## scene: game
title: La partie de courge
pace: now

Une fois l'an, tout le hameau joue à la balle d'un bout à l'autre, avec une courge séchée. Les règles tiennent en une ligne, et personne ne se souvient de la ligne.
Maître Frêne, lui, en a écrit quarante. Il cherche quelqu'un pour l'aider à les faire respecter.

* dive | Plonger dans la mêlée  [roll courage 3]
  [stress craven 15]
  [lean boldness +2]
  ? success
    {A} plonge, ressort de la mêlée avec la courge, et court jusqu'au Pont-Vieux sous les hurlements. Personne ne sait si ça compte. Tout le monde applaudit.
    [grow courage] [end]
  ? failure
    {A} plonge, et ressort de la mêlée avec la botte de Clément, la boue de tout le Communal, et pas la courge.
    [modifier muddy_paws] [end]

* stall | Tenir la buvette avec la Courge
  [lean greed +1]
  La Courge a installé une buvette à mi-parcours, « parce que tout ce qui compte passe par chez moi ». {A} sert du cidre doux à tout le monde, et la partie passe par la buvette trois fois.
  [cailloux 15] [end]

* referee | Arbitrer avec Maître Frêne
  [lean honor +2]
  {A} lit les règles à voix haute, toutes, pendant que la partie continue sans eux. À la règle trente-huit, la partie est finie.
  Maître Frêne déclare un match nul, à la satisfaction générale.
  [remember frene.refereed] [end]
