// Le bois de l'hiver — wave 2 (adulte, ancien), new. Every year the Intendant has wood delivered to the
// Maison des Pousses without signing; the Bruyère knows very well (fiches 3, 8). Nothing is said aloud,
// nothing changes between them. Reads scarf_pile's bruyere.saw_scarves.

# arc: winter_wood
title: Le bois de l'hiver
family: village
stage: adult, elder
trigger: draw
size: short

## memories
intendant.unloaded_wood | intendant | private | +3 | L'a aidé à décharger le bois de l'hiver devant la Maison des Pousses, une nuit, sans un mot.

## scene: cart
title: Le bois de l'hiver
pace: now

Chaque hiver, quelqu'un fait livrer du bois à la Maison des Pousses, sans signer. Cette nuit, une charrette attend au bout de la place.
L'Intendant, très raide, demande à {A} de l'aider à décharger. Sans bruit.
[if memory bruyere.saw_scarves] {A} pense à une certaine armoire, et à onze écharpes vertes. {A} n'en dit rien.

* quiet | Décharger en silence
  [lean compassion +2]
  Bûche par bûche, sans un bruit. À la fin, l'Intendant époussette ses manches et tend la main, très officiellement. « Il est d'usage de remercier. »
  [remember intendant.unloaded_wood] [end]

* sign | Signer d'une petite croix, pour rire  [mystery]
  [lean boldness +2]
  {A} trace une petite croix à la craie sur la dernière bûche. L'Intendant blêmit.
  Le lendemain matin, au pied de la Côte, il trouve un bol de soupe encore chaud, sous un torchon. Personne ne dit rien. Personne ne dira jamais rien.
  [modifier light_heart] [end]

* tell | Lui dire que la Bruyère sait
  [lean honor +1]
  L'Intendant soulève une bûche, la repose. « Je sais qu'elle sait. » Il en soulève une autre. « Elle sait que je sais. C'est très bien comme ça. »
  [grow diplomacy] [end]
