// Replaces break_toomuch.
# arc: break_toomuch
title: Trop, c'est trop
family: hardship
stage: baby, teen, adult, elder
trigger: break 3
size: short

## scene: break
title: Trop, c'est trop
was: break_toomuch
pace: now

{A} n'a plus envie de rien. Même le miel du Tilleul a un goût de rien.

* hide | Se terrer chez soi
  [lean sociability -2]
  {A} tire les rideaux deux jours entiers. À la réouverture, quelque chose a changé.
  [stress -100] [coping] [end]

* help | Accepter l'aide de la Sauge
  [lean sociability +1] [lean rationality +1]
  La Sauge apporte une couverture, une soupe et une tisane imbuvable. « Bois ça, et ne fais pas cette tête. » Ça va un peu mieux. Surtout grâce à la soupe.
  [stress -65] [end]

* drift | Se laisser porter
  [lean energy -2]
  {A} se laisse flotter quelques jours, comme une feuille sur la Sommeille. Les choses glissent, puis reviennent doucement à leur place.
  [stress -100] [modifier distracted] [end]
