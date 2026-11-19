// La croûte qui disparaît — wave 2 (adulte, ancien), new. Pain-Blond sets aside the crust of the first loaf
// « pour ceux qui veillent »; it is always gone the next day (fiche 7). The mystery stays a mystery.
// Reads arrival's pain_blond.ate_crust.

# arc: vanishing_crust
title: La croûte qui disparaît
family: village
stage: adult, elder
trigger: draw
size: short

## memories
pain_blond.second_crust | pain_blond | private | +3 | A posé une croûte à côté de la sienne, sur le rebord, pour ceux qui veillent. Les deux avaient disparu le lendemain.

## scene: sill
title: La croûte qui disparaît
pace: now

Chaque matin, Pain-Blond pose sur le rebord la croûte du premier pain, « pour ceux qui veillent ». Le lendemain, elle a toujours disparu.
Cette fois, un peu gêné, il demande à {A} de veiller. Juste pour savoir. Il n'est pas sûr de vouloir savoir.
[if memory pain_blond.ate_crust] « Toi, tu avais laissé une miette, la première fois. Je m'en souviens. »

* watch | Veiller toute la nuit  [mystery]
  [lean boldness +1]
  {A} s'assoit sous le rebord, une couverture sur les genoux, les yeux grands ouverts. À trois heures, {A} ferme les yeux une seconde.
  Au matin, la croûte a disparu. Il reste une miette, bien au milieu. Pain-Blond regarde la miette, puis le ciel, et sourit à ses chaussures.
  [modifier inspired] [end]

* second | Poser une croûte à soi à côté
  [stress greedy 10]
  [lean compassion +2]
  {A} pose une croûte de son propre pain à côté de celle de Pain-Blond. Le lendemain, les deux ont disparu. Pain-Blond n'en revient pas.
  [remember pain_blond.second_crust] [end]

* fou | Demander au Fou
  [lean sociability +1]
  Le Fou ne réfléchit même pas. « On l'a mangée, là-haut. Et on a dit merci. Tu n'as pas entendu ? » Pain-Blond est très ému. {A} aussi, un peu.
  [modifier light_heart] [end]
