// Fixture: a short event, one scene, closed by [end] with no ending section. Not shipped, never played.
# arc: test_short
title: Le caillou de test
family: village
stage: teen, adult
trigger: draw
size: short
weight: 10

## scene: only
title: Le caillou
pace: now

{A} trouve un caillou rond sur le chemin.

* keep | Le garder
  {A} le glisse dans sa poche.
  [cailloux 10] [end]

* toss | Le lancer  [roll courage 6]
  ? success
    Le caillou ricoche trois fois.
    [end]
  ? failure
    [sad] Le caillou tombe dans l'herbe.
    [end]
