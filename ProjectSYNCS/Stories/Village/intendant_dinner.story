// Le dîner de l'Intendant — wave 2 (adulte, ancien), new. The Intendant wants a register where everything
// adds up and, without admitting it, to be asked to stay for dinner (fiche 3). Reads up_the_cote's
// intendant.walked_down and pousse_count's intendant.stood_still.

# arc: intendant_dinner
title: Le dîner de l'Intendant
family: village
stage: adult, elder
trigger: draw
size: short

## memories
intendant.stayed_dinner | intendant | private | +3 | L'a invité à rester dîner, un soir de compte. Il est resté. Il a même ri.

## scene: evening
title: Le dîner de l'Intendant
pace: now

L'Intendant a fini son compte. Il range sa plume, boucle son registre, et regarde la Côte, qu'il faut remonter avant la nuit.
Il espère, sans se l'avouer, qu'on l'invite à rester dîner. Il met très longtemps à boucler son registre.
[if memory intendant.walked_down] Il regarde {A}. « Le raccourci est par là. Je ne le connais pas, bien sûr. »
[if memory intendant.stood_still] « Vous êtes le seul compte juste de ma vie. Je le dis sans rire. Je ne ris pas. »

* home | L'inviter chez soi
  [lean compassion +2]
  L'Intendant hésite exactement le temps qu'il convient, puis accepte. Il mange tout, complimente la soupe trois fois, et rit une fois, très fort, puis s'en excuse.
  [remember intendant.stayed_dinner] [end]

* inn | L'emmener à l'auberge
  [stress shy 10]
  [lean sociability +2]
  À l'auberge, la Courge met son couvert en premier, et le Fou lui laisse la meilleure place. L'Intendant ne sait plus où se mettre. Il reste jusqu'à minuit.
  [modifier party_soul] [end]

* road | Lui souhaiter bonne route
  [stress compassionate 10]
  [lean energy -1]
  « Bonne route. » L'Intendant hoche la tête, et laisse sur la borne un feuillet plié : ses notes du temps qu'il a fait, toute la semaine. C'est très instructif.
  [grow stewardship] [end]
