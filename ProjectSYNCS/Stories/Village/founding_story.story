// La fondation — wave 2 (ancien only), new. The Tilleul is the last to know the founding story, from his
// grandfather, and his version changes every winter (fiche 12, lore § 9.1). Reads apple_tree's
// tilleul.called_from_tree (whose story had a dragon).

# arc: founding_story
title: La fondation
family: village
stage: elder
trigger: draw
size: short

## memories
tilleul.told_his_way | tilleul | private | +3 | A raconté la fondation exactement comme lui, sans rien corriger. Même le grand-père qui n'était peut-être pas le sien.

## scene: story
title: La fondation
pace: now

Le Tilleul est le dernier à connaître le récit de la fondation, et sa version change chaque hiver. Il commence à s'en apercevoir.
Il demande à {A}, qui l'a entendue si souvent, de l'aider à s'en souvenir.
[if memory tilleul.called_from_tree] « Tu tombais de mon pommier. Et maintenant, c'est toi qui m'apprends mes histoires. »

* his_way | La raconter comme lui
  [stress honest 10]
  [lean compassion +2]
  {A} raconte la fondation comme le Tilleul la raconte, avec ses détours, son grand-père et ses abeilles. Il écoute, les yeux fermés, et hoche la tête à chaque erreur.
  [remember tilleul.told_his_way] [end]

* correct | Corriger les détails
  [lean rationality +2]
  {A} remet les familles dans le bon ordre, la Souche au bon endroit, la saison des pluies à sa place. Le Tilleul note tout, et l'oubliera cet hiver. C'est très bien comme ça.
  [grow learning] [end]

* dragon | Ajouter un dragon  [mystery]
  [lean boldness +2]
  {A} ajoute un dragon, petit, qui aidait à porter les bagages. Le Tilleul s'arrête, ravi. « C'est vrai. Je l'avais oublié. » Le dragon reste dans l'histoire pour de bon.
  [modifier inspired] [end]
