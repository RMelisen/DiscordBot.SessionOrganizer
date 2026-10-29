// Fixture: every tag once. Not shipped, never played. The checks (StoryChecks.cs) fail if a tag,
// a condition or an effect is missing from it, so a new word in the format gets a line here.
# arc: test_everything
title: L'arc de test
family: love
stage: adult, elder
trigger: relation lovers
shelf: 7d
weight: 10
weight: +5 [if passion gardening] [if season autumn]
requires: [stage adult] [not trait craven]
other: lovers
public: {A} et {B} s'écrivent.

## memories
courge.saw_them | courge | public  | +2 | Les a vus ensemble au Pont-Vieux.
lichen.secret   | lichen | private | +3 | Sait ce que {A} a promis à {B}.

## epithets
faithful | {a:Le Fidèle|La Fidèle}

## keepsakes
letter | La lettre | Une lettre de {B}, pliée en quatre. | hurts

## scene: start
title: La lettre
pace: next_day
public: {A} hésite devant une lettre.
bump

{A} trouve une lettre de {B}.
[if gender f] {A} la relit deux fois.
[unless relation lovers] Personne n'en sait rien.
{p:Les deux|Elles deux} se connaissent depuis longtemps.

* read | Lire la lettre
  [lean compassion +2]
  {A} lit la lettre, et {b:il|elle} lui manque.
  [flag read] [remember courge.saw_them] [-> answer]

* burn | Brûler la lettre  [needs trait brave]
  [stress shy 20]
  {A} la brûle sans la lire.
  [unflag read] [-> answer]

* hide | Cacher la lettre  [owner-only] [public "{A} cache quelque chose."]
  [lean honor -1]
  {A} la cache sous une pierre.
  [flag hid] [remember lichen.secret] [modifier inspired] [-> answer]

* old | Une option d'avant  [retired]

## scene: answer
title: La réponse
pace: +6h
public: {A} doit répondre.
handover

[if flag read] {B} attend une réponse.
[if memory lichen.secret] La Mère Lichen sait.
[if keepsake letter] La lettre est encore là.
[if epithet faithful] On {a:le|la} dit fidèle.
[if standing lichen >= 2] La Mère Lichen sourit.
[if stat diplomacy >= 5] {A} sait parler.
[if season winter] Il neige.
[if chain old_door] Une porte, quelque part.
[if other trait shy] {B} rougit.
[if ended test_everything.together] Ce n'est pas la première fois.
[if flag test_everything.hid] Quelque chose est caché.

* yes | Dire oui  [needs flag read "{A} a lu la lettre"]
  {A} dit oui.
  [affinity +10] [other remember courge.saw_them] [-> walk]

* no | Dire non
  {A} dit non.
  [stress 10] [-> apart]

* chance | Tenter sa chance  [roll diplomacy 7] [secret]
  ? success
    {B} rit.
    [lift happiness 0.2] [grow diplomacy] [-> walk]
  ? failure
    {B} se tait.
    [-> apart]

## scene: walk
title: La promenade
pace: now
{A} et {B} marchent jusqu'au Pont-Vieux.
[reveal lichen.secret] [-> together]

## scene: apart
title: Chacun chez soi
pace: next_day
{A} rentre seul{a:|e}.

* wait | Attendre
  {A} attend.
  [forget courge.saw_them] [cailloux 30] [-> alone]

* write | Écrire à nouveau
  {A} écrit.
  [item care.soap] [-> faithful_end]

## scene: old_scene
retired -> apart

## scene: loss
title: Le départ de {B}
pace: now
{B} ne reviendra pas.

* keep | Garder la lettre
  {A} garde la lettre.
  [-> gone]

* let_go | La laisser au vent
  {A} la laisse partir.
  [-> gone]

## ending: together
title: Ensemble
{A} et {B} rentrent ensemble.
[epithet faithful] [trait brave] [unlock test_everything]

## ending: faithful_end
title: Fidèle
{A} reste {a:fidèle|fidèle}.
[epithet faithful] [cailloux 40]

## ending: alone
title: Seul{a:|e}
{A} rentre.
[trait brave -> craven]

## ending: gone
title: Parti
{A} garde le silence.
[keepsake letter]

## ending: interrupted
title: Interrompu
La Souche reprend {A}.
