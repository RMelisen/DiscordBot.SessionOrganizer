// Le juge d'un jour — wave 2 (adulte, ancien). Rewrites grown_judge: Maître Frêne has a cold (the Sauge
// sees to it) and someone settles the day's quarrel at the Communal, eternal source of small disputes —
// whose goat, whose turn (fiche 2, lore § 5).

# arc: judge_for_a_day
title: Le juge d'un jour
family: village
stage: adult, elder
trigger: draw
size: short

## memories
frene.applied_rules | frene | private | +2 | A appliqué son règlement du Communal à la lettre, le jour où il avait un rhume. Il était très fier.

## scene: communal
title: Le juge d'un jour
pace: now

Maître Frêne a un rhume. La Sauge l'a mis au lit avec un bol de quelque chose de vert. Quelqu'un doit régler la querelle du jour au Communal.
Le Noyer et la tisserande se disputent la chèvre commune : à qui le tour, aujourd'hui. Le hameau a désigné {A}.

* compromise | Trouver un compromis
  [lean compassion +1]
  {A} écoute les deux, longtemps, puis tranche : la chèvre chez le Noyer, le lait chez la tisserande. Personne n'est content. C'est donc juste.
  [grow diplomacy] [end]

* rules | Appliquer le règlement de Maître Frêne, à la lettre
  [lean zeal +2]
  {A} ouvre le règlement du Communal, article neuf, alinéa trois. C'est le tour de la tisserande. Le Noyer s'incline devant la loi, en grognant.
  Au lit, Maître Frêne apprend la nouvelle et éternue de bonheur.
  [remember frene.applied_rules] [end]

* split | Couper la journée de la chèvre en deux  [mystery]
  [lean rationality +1]
  Le matin chez l'un, l'après-midi chez l'autre. La chèvre, consultée, choisit de passer la journée entière au milieu, sous le grand frêne. Tout le monde trouve ça sage.
  [modifier light_heart] [end]
