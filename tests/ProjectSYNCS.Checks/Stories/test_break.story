// Fixture: a mental break (trigger: break), its coping traits and the deeper relief a break allows.
// Not shipped, never played.
# arc: test_break
title: La crise de test
family: hardship
stage: baby, teen, adult, elder
trigger: break 2
size: short

## scene: break
title: La crise
pace: now

{A} n'en peut plus.

* talk | Tout raconter
  {A} parle longtemps.
  [stress -90] [coping] [end]

* write | Tout écrire
  {A} écrit.
  [stress -120] [coping journaller] [end]

* rest | Se reposer
  {A} se repose.
  [stress -60] [end]
