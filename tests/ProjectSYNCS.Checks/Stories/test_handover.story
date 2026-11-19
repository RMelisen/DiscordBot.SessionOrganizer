// Fixture: a short arc that hands a decision to the other owner (handover), with the words that go
// with it. Not shipped, never played. test_everything is long and cannot end on [end], which every
// handover option must, so these words live here.
# arc: test_handover
title: La question de test
family: love
stage: adult, elder
trigger: draw
size: short
other: known
other_if: [can_couple] [affinity >= 0]

## scene: ask
title: La question
pace: now

{A} regarde {B} depuis le banc du Puits.

* ask | Poser la question  [owner-only]
  [lean boldness +2]
  {A} se lève et va poser la question à {B}.
  [-> answer]

* race | Proposer une course  [roll courage vs]
  ? success
    {A} gagne d'une longueur, et {B} rit.
    [bond friends] [end]
  ? failure
    {B} gagne d'une longueur.
    [affinity +5] [end]

* later | Attendre encore
  {A} attend un autre jour.
  [-> more]

## scene: answer
title: Une question
pace: now
handover

{B} est venu{b:|e} poser une question à {A}.

* yes | Dire oui  [accept]
  [lean sociability +1]
  {A} dit oui.
  [couple] [end]

* no | Dire non  [refuse]
  {A} dit non.
  [heartbreak] [end]

## scene: more
title: Un autre jour
pace: 24h-72h

{A} repense à {B}.

* go | Y aller
  {A} y va.
  [end]

* stay | Rester
  {A} reste.
  [bond peace] [end]
