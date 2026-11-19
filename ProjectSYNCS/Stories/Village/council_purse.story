// La caisse du conseil — wave 2 (adulte, ancien), new. The council's purse is always three cailloux short
// of the last count, and Maître Frêne recounts (fiche 2). Nobody ever finds out why.

# arc: council_purse
title: La caisse du conseil
family: village
stage: adult, elder
trigger: draw
size: short

## memories
frene.three_cailloux | frene | private | +2 | A glissé trois cailloux dans la caisse du conseil, en douce. Il a fait semblant de ne pas voir.

## scene: purse
title: La caisse du conseil
pace: now

La caisse du conseil a trois cailloux de moins qu'au dernier compte. Comme toujours.
Maître Frêne recompte pour la quatrième fois, aligne les cailloux en rangs de dix, et demande de l'aide.

* recount | Recompter avec lui
  [lean rationality +2]
  Ils recomptent ensemble, par dix, puis par cinq, puis un par un. Il manque toujours trois cailloux. Mais cette fois, ils en sont sûrs à deux.
  [grow stewardship] [end]

* slip | Glisser trois cailloux dans la caisse, en douce
  [lean compassion +2]
  {A} glisse trois cailloux de sa poche dans la caisse pendant qu'il tourne le dos. Maître Frêne recompte. Le compte est juste.
  Il regarde la caisse, puis {A}, puis la caisse. « Il convient de ne rien dire. »
  [remember frene.three_cailloux] [modifier clear_conscience] [end]

* fou | Demander au Fou  [mystery]
  [lean sociability +1]
  Le Fou compte la caisse en chantonnant. « Trois de moins. C'est le bon chiffre. Il en manque toujours trois, c'est comme ça qu'on sait que c'est la bonne caisse. »
  Maître Frêne trouve l'argument juridiquement solide, et le note.
  [modifier inspired] [end]
