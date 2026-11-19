// Replaces grown_letters, grown_letters_more and grown_letters_bridge.
# arc: letters
title: Les lettres du Fou
family: love
stage: adult, elder
trigger: draw
size: short
other: known
other_if: [can_couple]

## scene: letter
title: Les lettres du Fou
was: grown_letters
pace: now

Depuis des semaines, {A} pense à {B}. Le matin, le soir, et beaucoup entre les deux. Le dire en face semble impossible. Mais le Fou porte les mots de tout le hameau, à pied, et sans signature si on préfère. Le Fou ne les lit jamais. Le Fou compte les mots, c'est tout.

* poem | Écrire un poème, le plus beau possible  [roll learning 8]
  [lean rationality +1] [lean compassion +1]
  ? success
    {A} écrit douze vers sur les yeux de {B}, le vent, et la soupe de la Courge. Le Fou part avec, en comptant. Le lendemain, le Fou rapporte une réponse : « Encore ? » « Un mot », précise le Fou.
    [grow learning] [affinity +10] [-> more]
  ? failure
    {A} écrit, rature, recommence, et finit par confier au Fou une lettre qui ne parle que du temps qu'il fait. Le lendemain, la réponse arrive : « Oui, très beau temps. Et toi ? » « Six mots », dit le Fou, l'air déçu.
    [affinity +5] [-> more]

* simple | Écrire simplement : « Je pense à toi. »
  [lean honor +1]
  {A} écrit trois mots, sans signature, et les confie au Fou, pour {B}. Trois jours passent. Puis le Fou revient : « Moi aussi, je pense à quelqu'un. » Ce n'est pas un oui. Ce n'est pas un non. « Six mots », dit le Fou. « C'est bon signe, six. »
  [affinity +5] [-> more]

* drawer | Garder ça pour soi, encore un peu
  [lean rationality +1] [lean boldness -1]
  {A} prend une feuille, la regarde longtemps, puis la range dans un tiroir. Certaines lettres ont besoin de mûrir. Sous la fenêtre, le Fou ralentit, puis repart : le Fou a l'habitude.
  [grow learning] [end]

## scene: more
title: La correspondance
was: grown_letters_more
pace: 24h-72h

Depuis deux semaines, les lettres vont et viennent dans les poches du Fou. On s'y raconte la journée, les nuages, une chanson entendue à l'auberge. {B} signe toujours d'un petit dessin de feuille. Ce matin, la dernière lettre se termine par une question : « Qui es-tu ? »

* meet | Répondre : « Demain, au Pont-Vieux, à l'aube. »  [roll diplomacy 8]
  [lean boldness +1] [lean sociability +1]
  ? success
    {A} écrit l'heure et le lieu, et ajoute un petit dessin de feuille, en réponse. Le lendemain à l'aube, {B} est sur le pont, une écharpe autour du cou, et sourit en voyant arriver {A}. Le Fou, très discret, compte les pierres du parapet avec une grande concentration.
    [grow diplomacy] [affinity +15] [-> bridge]
  ? failure
    La pluie mouille l'encre de l'invitation. Le lendemain, {B} attend au pont… à midi. {A}, à l'aube. Les deux se ratent de six heures, et le comprennent le soir, en riant chacun de son côté.
    [affinity +5] [-> bridge]

* riddle | Répondre par une devinette
  [lean rationality +1]
  {A} répond par une devinette : « Je prends ma soupe avec trois croûtons et je déteste les lundis. » Le lendemain, {B} a trouvé. La réponse que rapporte le Fou dit seulement : « Je savais. »
  [affinity +10] [-> bridge]

## scene: bridge
title: Sur le Pont-Vieux
was: grown_letters_bridge
pace: 24h-48h

Le soleil se lève sur le Pont-Vieux. {A} et {B} sont là, côte à côte, la main sur la pierre, à regarder la Sommeille. Toutes les lettres sont dans la poche de {A}, et une bonne partie dans celle de {B}. Le moment est venu de dire tout haut ce qui a été écrit tout bas.

* declare | Tout avouer à {B}, tout haut  [owner-only]
  [lean boldness +2] [lean sociability +1]
  {A} prend une grande inspiration, se tourne vers {B}, et dit enfin, à voix haute, tout ce que les lettres disaient.
  [-> answer]

* keep | Proposer de garder les lettres, et rester amis
  [lean rationality +1]
  {A} sort les lettres et propose de les garder dans une boîte, à deux, en souvenir. {B} sourit, un peu {b:surpris|surprise}, et accepte. Certaines histoires sont plus belles à mi-chemin.
  [affinity +10] [end]

## scene: answer
title: Une déclaration
pace: now
handover

{B} est venu{b:|e} tout avouer à {A}, les joues rouges et la voix qui tremble.

* yes | Dire oui  [accept]
  [lean sociability +1] [lean compassion +1]
  {A} dit oui. {B} en oublie de respirer, puis rit, puis respire. Tout le hameau est au courant avant le soir : la Courge y a veillé.
  [couple] [end]

* no | Dire non, doucement  [refuse]
  [lean honor +1]
  {A} dit non, avec beaucoup de douceur. {B} hoche la tête et rentre par le chemin le plus long.
  [heartbreak] [end]
