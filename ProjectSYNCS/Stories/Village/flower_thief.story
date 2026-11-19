// Le voleur de fleurs — wave 1 (ado). Rewrites teen_flower_thief: the thief is Mathurin, picking for
// Aude, and the whole village is in on it (lore § 11, § 12). Nobody is told; the secret stays.

# arc: flower_thief
title: Le voleur de fleurs
family: village
stage: teen
trigger: draw
size: short

## scene: garden
title: Le voleur de fleurs
pace: now

Chaque matin, une fleur de plus manque au jardin de la Maison des Pousses. Proprement coupée, sans un brin par terre.
La Bruyère compte les tiges, puis les pousses, et regarde chacune un peu trop longtemps.

* watch | Guetter à l'aube  [mystery]
  [lean boldness +2]
  À l'aube, une ombre enjambe la haie : Mathurin, l'apprenti du Chardon, rouge jusqu'aux oreilles, une seule fleur à la main.
  Il pose un doigt sur ses lèvres. {A} en pose un aussi. Le lendemain, {A} lui montre où poussent les plus belles.
  [modifier light_heart] [end]

* sign | Planter une pancarte « Défense de cueillir »
  [lean zeal +2]
  {A} peint une pancarte superbe, avec des lettres à fleurs et une bordure. Personne n'en tient compte.
  La Bruyère la trouve si bien faite qu'elle l'accroche dans la cuisine.
  [grow stewardship] [end]

* bouquet | Laisser un bouquet tout prêt sur le rebord  [needs trait lustful]
  [lean compassion +2]
  {A} cueille un bouquet, le noue d'un brin d'herbe, et le pose sur le rebord de la haie. Au matin, il a disparu.
  À sa place, sur la fenêtre de {A}, une primevère, et un peu de farine fine.
  [item col.primevere] [end]
