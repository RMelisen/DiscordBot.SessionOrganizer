# arc: weaver_thread
title: Le fil de la tisserande
family: village
stage: baby, teen, adult, elder
trigger: visit
size: short
other: anyone
cooldown: 7d

## scene: thread
title: Le fil de la tisserande
pace: now

Au bord de la place, la tisserande tisse une bande qui n'en finit pas, avec un fil pour chaque chose qu'elle voit passer. Ce matin, {A} y reconnaît deux fils côte à côte : la couleur de son chapeau, et celle du chapeau de {B}. « La visite », dit-elle, sans lever les yeux.

* ask | Demander ce que raconte le reste de la bande
  [lean rationality +1] [lean sociability +1]
  La tisserande montre du doigt : là, le grand hiver ; là, la foire de l'an dernier ; là, une chèvre qui a mangé un chapeau. Tout le village tient dans sa bande. Maintenant, {A} aussi.
  [grow learning] [end]

* remove | Lui demander d'enlever ces deux fils
  [lean sociability -1] [lean boldness +1]
  La tisserande hausse les épaules et tire les deux fils d'un coup sec. Le lendemain, il y a un trou à cet endroit de la bande, et tout le hameau vient demander ce qui s'est passé là.
  [grow intrigue] [end]

* add | Ajouter un troisième fil, d'une couleur à soi
  [lean zeal +1] [lean compassion +1]
  {A} choisit une couleur qui n'est à personne, et la tisserande la passe entre les deux autres sans un mot. Le soir, il y a dans la bande un petit nœud de trois couleurs. Elle dit que c'est rare.
  [affinity +3] [modifier inspired] [end]
