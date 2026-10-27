using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

// Wave 8, second batch: bébé and ado.
public static partial class PlynlingEvents
{
    private static EventDef[] Wave8Batch2Young() => new[]
    {
        // ---- wave 8, second batch: bébé
        new EventDef("baby_cant_sleep", EventType.Pulse, Baby, "Les yeux grands ouverts",
            "Minuit passé. Tout le village dort, sauf {A}, qui compte les poutres du plafond pour la neuvième fois. Dehors, sur la branche, la chouette ne dort pas non plus. La nuit, la chouette ne dort jamais : c'est son métier.",
            new[]
            {
                Plain("owl", "Ouvrir la fenêtre, et parler à la chouette",
                    "La chouette raconte ce qui se passe la nuit : les souris qui font leurs courses, la lune qui change de place, le hérisson qui ronfle en morse. À la troisième histoire, {A} dort, le nez sur le rebord.",
                    E(new ApplyModifier("well_rested")), Ai((AiAxis.Sociability, 1), (AiAxis.Rationality, 1))),
                Plain("sheep", "Compter les moutons, sérieusement",
                    "{A} compte les moutons. Au quatre-vingtième, un mouton refuse de sauter. {A} négocie. Le mouton finit par sauter. {A} dort avant le quatre-vingt-unième.",
                    E(new GrowStat(PlynlingStat.Stewardship)), Ai((AiAxis.Rationality, 1))),
                Try("night", "Sortir explorer la nuit, en pyjama", new EventChallenge(PlynlingStat.Courage, 4),
                    "{A} sort en pyjama et découvre le village de nuit : les lucioles au-dessus de la mare, la boulangerie déjà allumée, et le boulanger qui offre la première brioche, encore chaude. {A} rentre à l'aube, et dort jusqu'à midi.",
                    "{A} sort en pyjama, fait trois pas, et entend un bruit. C'est une feuille. {A} rentre très vite, et dort enfin, sous la couverture, la tête comprise.",
                    E(new GrowStat(PlynlingStat.Courage), new LiftNeed(Need.Hunger, 0.15)), Nothing, Ai((AiAxis.Boldness, 2))),
            }),

        new EventDef("baby_mud_pie", EventType.Pulse, Baby, "La pâtisserie de boue",
            "{A} a ouvert une pâtisserie au bord de la flaque. Au menu : tarte de boue, gâteau de boue, et une spécialité de boue aux pâquerettes. Le premier client s'approche : l'ours, qui a l'air d'avoir très faim.",
            new[]
            {
                Plain("sell", "Vendre une tarte à l'ours, très cher",
                    "« Trois cailloux », annonce {A}. L'ours paie trois cailloux, très sérieusement, prend la tarte, la renifle, et la pose délicatement sur un muret, « pour plus tard ». Les trois cailloux sont rangés sous le lit, pour toujours.",
                    E(new GrowStat(PlynlingStat.Stewardship), new GiveCailloux(3)), Ai((AiAxis.Greed, 1), (AiAxis.Sociability, 1))),
                Plain("gift", "Offrir la plus belle part, gratuitement",
                    "{A} offre la plus belle part. L'ours la reçoit comme une médaille, et le lendemain, apporte en échange un vrai gâteau, au miel. {A} trouve l'échange équitable.",
                    E(new LiftNeed(Need.Hunger, 0.2)), Ai((AiAxis.Compassion, 2))),
                Try("tower", "Inventer un gâteau à trois étages, devant le client", new EventChallenge(PlynlingStat.Learning, 4),
                    "{A} invente sur place le gâteau de boue à trois étages, décoré de cailloux blancs. L'ours applaudit. Le gâteau tient debout une heure entière, un record.",
                    "Le troisième étage glisse sur le deuxième, puis sur {A}. L'ours aide à tout nettoyer, et rit tout le long.",
                    E(new GrowStat(PlynlingStat.Learning)), Nothing, Ai((AiAxis.Rationality, 1), (AiAxis.Energy, 1))),
            }),

        new EventDef("baby_doudou", EventType.Pulse, Baby, "La grenouille en tricot",
            "La grenouille en tricot de {A} a disparu. Celle avec un œil en bouton et une patte recousue trois fois. Sans la grenouille, impossible de dormir, ni de manger, ni de faire quoi que ce soit. {A} a fouillé partout. Partout, sauf peut-être…",
            new[]
            {
                Plain("village", "Fouiller tout le village, maison par maison",
                    "{A} frappe à toutes les portes. Personne n'a vu la grenouille. Mais tout le monde promet de chercher, et l'ours colle même une affiche sur son kiosque : « Recherchée : grenouille, un œil, très aimée. »",
                    E(new GrowStat(PlynlingStat.Diplomacy), new FollowUp("baby_doudou_found", 24, 48)), Ai((AiAxis.Sociability, 2))),
                Plain("thread", "Suivre la piste : un fil de laine verte",
                    "Un fil de laine verte traverse le jardin, passe sous la haie, monte le long d'un arbre. {A} le suit jusqu'à la nuit, et rentre avec une pelote énorme et une piste très chaude.",
                    E(new GrowStat(PlynlingStat.Learning), new FollowUp("baby_doudou_found", 24, 48)), Ai((AiAxis.Rationality, 2))),
                Plain("new", "Essayer la grenouille neuve du magasin de jouets",
                    "Le blaireau du magasin de jouets propose une grenouille neuve, plus verte, avec deux yeux. {A} la regarde longtemps, et la repose. « Ce n'est pas la même. » Le blaireau hoche la tête : ça, le blaireau le savait.",
                    E(new FollowUp("baby_doudou_found", 24, 48)), Ai((AiAxis.Rationality, 1), (AiAxis.Honor, 1))),
            }),

        new EventDef("baby_doudou_found", EventType.FollowUp, AnyStage, "La grenouille retrouvée",
            "Ce matin, la grenouille en tricot attend sur le rebord de la fenêtre. Lavée, séchée, avec un nouvel œil : un bouton doré. Posée à côté, une petite plume noire et blanche. Sur le toit d'en face, la pie regarde ailleurs avec beaucoup d'application.",
            new[]
            {
                Plain("thanks", "Crier merci vers le toit",
                    "{A} crie merci vers le toit. La pie fait semblant de ne pas entendre, puis laisse tomber, l'air de rien, un deuxième bouton doré, de rechange.",
                    E(new GiveItem("col.bouton"), new ApplyModifier("cherished")), Ai((AiAxis.Compassion, 1), (AiAxis.Sociability, 1))),
                Plain("hug", "Serrer la grenouille, et ne plus jamais la lâcher",
                    "{A} serre la grenouille si fort que le nouveau bouton laisse une marque sur la joue. Cette nuit-là, {A} dort douze heures. La grenouille aussi, sans doute.",
                    E(new ApplyModifier("well_rested"), new LiftNeed(Need.Happiness, 0.2)), Ai((AiAxis.Compassion, 1))),
                Plain("suspect", "Plisser les yeux vers la pie",
                    "{A} plisse les yeux vers le toit. La pie plisse les yeux en retour. Personne ne cède. Mais le bouton doré est très joli, et on décide, sans un mot, d'en rester là.",
                    E(new GrowStat(PlynlingStat.Intrigue)), Ai((AiAxis.Rationality, 1), (AiAxis.Boldness, 1))),
            }),

        // ---- wave 8, second batch: ado
        new EventDef("teen_rival", EventType.Pulse, Teen, "Toujours deuxième",
            "Un jeune furet est arrivé au village, et depuis, à la bibliothèque, à la course, au lancer de pomme de pin, le furet finit premier, et {A} deuxième. Ce matin, le furet passe devant {A}, ralentit, et lâche : « Toujours deuxième ? » Puis s'éloigne en sifflotant.",
            new[]
            {
                Plain("train", "S'entraîner en secret, tous les matins",
                    "{A} se lève avant le soleil, court autour de l'étang, lit deux livres par jour, et lance des pommes de pin contre le mur du jardin jusqu'au dîner. Trois jours plus tard, un défi arrive, écrit à la main : la grande course, samedi.",
                    E(new GrowStat(PlynlingStat.Courage), new FollowUp("teen_rival_race", 24, 48)), Ai((AiAxis.Energy, 2)), Stress(("lazy", 15))),
                Plain("shrug", "Hausser les épaules : deuxième, c'est très bien",
                    "{A} hausse les épaules et retourne à son livre. Le furet s'arrête, vexé que ça ne marche pas. Le soir, un défi arrive, en bonne et due forme : la grande course, samedi.",
                    E(new ApplyModifier("clear_conscience"), new FollowUp("teen_rival_race", 24, 48)), Ai((AiAxis.Rationality, 1))),
                Try("retort", "Répondre du tac au tac", new EventChallenge(PlynlingStat.Diplomacy, 6),
                    "« Deuxième, peut-être. Mais moi, je ne siffle pas faux. » Le furet s'arrête net, et s'en va sans siffler. Le soir, un défi arrive : la grande course, samedi.",
                    "{A} cherche une réponse, et la trouve trois heures plus tard, en se brossant les dents. Trop tard. Le soir, un défi arrive : la grande course, samedi.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new FollowUp("teen_rival_race", 24, 48)), E(new FollowUp("teen_rival_race", 24, 48)),
                    Ai((AiAxis.Boldness, 1), (AiAxis.Sociability, 1)), Stress(("shy", 15))),
            }),

        new EventDef("teen_rival_race", EventType.FollowUp, AnyStage, "La grande course",
            "Samedi, la moitié du village s'est installée le long du chemin de l'étang. Le moineau tient le sifflet. Le furet s'étire, très sûr de soi. Le parcours : trois tours d'étang, le vieux pont, et retour. Coup de sifflet.",
            new[]
            {
                Try("sprint", "Partir à fond dès le départ", new EventChallenge(PlynlingStat.Courage, 7),
                    "{A} part comme une flèche et mène deux tours entiers. Au troisième, le furet revient, épaule contre épaule. Sur le vieux pont, {A} passe d'un museau. Le moineau hésite longtemps, puis lève la patte de {A}.",
                    "{A} part comme une flèche, mène deux tours, et s'effondre au troisième. Le furet gagne presque en marchant. Puis revient en arrière, et finit le dernier tour à côté de {A}, sans un mot.",
                    E(new GrowStat(PlynlingStat.Courage), new FollowUp("teen_rival_end", 6, 12)), E(new FollowUp("teen_rival_end", 6, 12)), Ai((AiAxis.Boldness, 2))),
                Try("pace", "Garder son souffle pour la fin", new EventChallenge(PlynlingStat.Stewardship, 7),
                    "{A} laisse filer le furet, garde son rythme, et le rattrape dans la dernière ligne droite, à bout de souffle. Victoire d'un cheveu. Le furet, pour la première fois, ne trouve rien à dire.",
                    "{A} garde son souffle pour la fin. La fin arrive trop vite. Le furet gagne, se retourne, et pour la première fois, ne dit rien.",
                    E(new GrowStat(PlynlingStat.Stewardship), new FollowUp("teen_rival_end", 6, 12)), E(new FollowUp("teen_rival_end", 6, 12)), Ai((AiAxis.Rationality, 2))),
                Try("brambles", "Couper par les ronces", new EventChallenge(PlynlingStat.Intrigue, 7),
                    "{A} coupe par les ronces, ressort devant le furet, et gagne. Le furet regarde les épines accrochées partout sur {A}, et sourit d'un drôle d'air.",
                    "{A} coupe par les ronces et y reste {a:coincé|coincée}. Le furet, en passant, s'arrête pour aider {A} à se dégager, et perd la course. Le moineau ne sait plus du tout quoi noter.",
                    E(new GrowStat(PlynlingStat.Intrigue), new FollowUp("teen_rival_end", 6, 12)), E(new FollowUp("teen_rival_end", 6, 12)),
                    Ai((AiAxis.Honor, -2)), Stress(("honest", 20), ("just", 20))),
            }),

        new EventDef("teen_rival_end", EventType.FollowUp, AnyStage, "Au bout du pont",
            "Le soir de la course, {A} trouve le furet, seul, au bout du vieux pont, les pattes dans le vide. Plus de sifflotement. « Là d'où je viens, dit le furet sans se retourner, quand on n'est pas premier, on n'existe pas. »",
            new[]
            {
                Plain("sit", "S'asseoir à côté, les pattes dans le vide",
                    "{A} s'assoit. Le soleil se couche. Au bout d'un moment, {A} dit : « Ici, on existe même septième. Demande à l'escargot. » Le furet rit, pour la première fois sans moquerie. Le lendemain, les deux courent ensemble, pour rien, juste pour courir.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new ApplyModifier("cherished")), Ai((AiAxis.Compassion, 2))),
                Plain("rematch", "Proposer une revanche, chaque samedi",
                    "« Revanche samedi prochain ? Et celui d'après ? » Le furet se retourne, l'œil brillant. Depuis, chaque samedi, le village vient voir la course, et plus personne ne tient les comptes, surtout pas les deux coureurs.",
                    E(new ApplyModifier("fired_up")), Ai((AiAxis.Boldness, 1), (AiAxis.Sociability, 1))),
            }),

        new EventDef("teen_bakery", EventType.Pulse, Teen, "Le fournil à l'aube",
            "Le boulanger cherche quelqu'un pour l'aider le matin, avant le lever du soleil. Le salaire : quelques cailloux, et tous les croissants ratés. {A} se présente à quatre heures. Le fournil est chaud, sombre, et sent si bon que c'en est presque injuste.",
            new[]
            {
                Try("knead", "Pétrir la pâte, comme le boulanger montre", new EventChallenge(PlynlingStat.Stewardship, 6),
                    "{A} pétrit, plie, laisse reposer, recommence. À six heures, la première fournée sort, dorée, parfaite. Le boulanger casse un croissant en deux, l'écoute craquer, et hoche la tête : « Demain aussi. »",
                    "{A} pétrit trop fort. Les croissants sortent en forme de cailloux. Le boulanger en goûte un quand même, très courageusement. « Demain, plus doucement. »",
                    E(new GrowStat(PlynlingStat.Stewardship), new FollowUp("teen_bakery_week", 72, 120)), E(new LiftNeed(Need.Hunger, 0.2), new FollowUp("teen_bakery_week", 72, 120)),
                    Ai((AiAxis.Energy, 1), (AiAxis.Rationality, 1))),
                Plain("taste", "Goûter chaque fournée, par conscience professionnelle",
                    "{A} goûte la première fournée, puis la deuxième, par sécurité. À six heures, le boulanger compte les croissants, recompte, et regarde {A}, qui a des miettes jusqu'aux oreilles. « Demain, tu goûtes moins. Mais tu reviens. »",
                    E(new LiftNeed(Need.Hunger, 0.25), new FollowUp("teen_bakery_week", 72, 120)), Ai((AiAxis.Greed, 1), (AiAxis.Energy, -1)), Stress(("temperate", 15))),
                Plain("sing", "Chanter, pour que la pâte lève",
                    "Le boulanger assure que la pâte lève mieux en musique. {A} chante tout ce qu'on connaît, puis invente. La pâte lève. Le boulanger lève aussi un sourcil, mais ne dit rien, parce que la pâte a levé.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new FollowUp("teen_bakery_week", 72, 120)), Ai((AiAxis.Sociability, 2))),
            }),

        new EventDef("teen_bakery_week", EventType.FollowUp, AnyStage, "Le dernier jour au fournil",
            "Une semaine de fournées à l'aube. {A} sait maintenant pétrir, plier, et ne jamais ouvrir le four trop tôt. Le dernier jour, le boulanger tend une petite bourse, et pose à côté une boule de pâte crue. « Le salaire. Et ça, c'est pour ton pain à toi. Fais-en ce que tu veux. »",
            new[]
            {
                Plain("sun", "Façonner un pain en forme de soleil",
                    "{A} façonne un soleil aux rayons tressés. Le boulanger l'enfourne sans un mot, puis le pose en vitrine, avec une étiquette : « Pas à vendre ». La bourse, de son côté, pèse son poids de cailloux.",
                    E(new GrowStat(PlynlingStat.Stewardship), new GiveCailloux(15)), Ai((AiAxis.Rationality, 1))),
                Plain("long", "Faire un pain pour toute la place",
                    "Une seule boule de pâte, mais {A} l'étire, l'étire, et en fait un pain long comme un banc. Toute la place en mange un morceau. Le boulanger mesure le pain deux fois, et l'inscrit dans le carnet des records.",
                    E(new GiveCailloux(15), new LiftNeed(Need.Happiness, 0.15)), Ai((AiAxis.Sociability, 1), (AiAxis.Compassion, 1))),
                Plain("back", "Demander à revenir la semaine prochaine",
                    "{A} garde la boule de pâte dans un torchon et demande à revenir. Le boulanger sourit, pour la première fois de la semaine. « Quatre heures. Pas une minute de plus. » Avec le hérisson, ça fait deux personnes au village qui comptent les minutes.",
                    E(new GiveCailloux(15), new ApplyModifier("trade_sense")), Ai((AiAxis.Energy, 1), (AiAxis.Rationality, 1))),
            }),

        new EventDef("teen_hidden_garden", EventType.Pulse, Teen, "Sous le lierre",
            "Derrière la bibliothèque, un vieux mur couvert de lierre. {A} y est {a:passé|passée} cent fois. Aujourd'hui, le vent soulève le lierre, et dessous apparaît une petite porte en bois, sans poignée, entrouverte.",
            new[]
            {
                Try("push", "Pousser la porte", new EventChallenge(PlynlingStat.Courage, 6),
                    "La porte grince. Derrière : un jardin oublié, des fleurs hautes comme des arbres, une fontaine sèche, et un banc où quelqu'un a laissé un livre ouvert, la page cornée. Le jardin attend quelqu'un depuis longtemps.",
                    "La porte grince si fort que {A} s'enfuit. Une heure plus tard, {A} revient sur la pointe des pieds. La porte, patiente, attend toujours.",
                    E(new GrowStat(PlynlingStat.Courage), new FollowUp("teen_hidden_garden_owl", 24, 48)), E(new FollowUp("teen_hidden_garden_owl", 24, 48)), Ai((AiAxis.Boldness, 2))),
                Plain("ask", "Demander d'abord à la chouette où mène la porte",
                    "La chouette ferme son livre, très lentement. « Le jardin de la vieille bibliothécaire. Avant moi. » Puis, après un silence : « Personne n'y est entré depuis. Tu peux, si tu fais attention. »",
                    E(new GrowStat(PlynlingStat.Learning), new FollowUp("teen_hidden_garden_owl", 24, 48)), Ai((AiAxis.Rationality, 1), (AiAxis.Honor, 1))),
                Plain("ivy", "Remettre le lierre en place, et garder le secret",
                    "{A} remet le lierre en place, très soigneusement, et n'en dit rien à personne. Certaines portes sont plus belles fermées, avec un secret derrière. {A} repasse devant chaque jour, juste pour savoir.",
                    E(new ApplyModifier("soothed")), Ai((AiAxis.Rationality, 1), (AiAxis.Boldness, -1))),
            }),

        new EventDef("teen_hidden_garden_owl", EventType.FollowUp, AnyStage, "Le jardin de la bibliothécaire",
            "Dans le jardin, {A} a commencé à arracher les ronces, à remplir la fontaine, à replanter. Ce matin, quelqu'un est assis sur le banc, le livre à la page cornée sur les genoux : la chouette. « J'étais son élève, dit la chouette. Je n'ai jamais osé revenir. »",
            new[]
            {
                Plain("together", "Proposer de jardiner ensemble",
                    "Désormais, tous les matins, la chouette et {A} jardinent en silence. La fontaine coule de nouveau. Un jour, la chouette lit à voix haute la page cornée : un poème sur un jardin. La chouette pleure un peu. {A} fait semblant d'arroser.",
                    E(new GrowStat(PlynlingStat.Stewardship), new ApplyModifier("cherished")), Ai((AiAxis.Compassion, 2))),
                Plain("give", "Rendre le jardin à la chouette",
                    "{A} tend le petit arrosoir à la chouette. « Le jardin vous attendait. » La chouette tient l'arrosoir longtemps, puis donne à {A} une petite clé de bronze, celle de la porte sous le lierre. « À nous deux, alors. »",
                    E(new GiveItem("col.cle_rouillee"), new ApplyModifier("clear_conscience")), Ai((AiAxis.Honor, 1), (AiAxis.Compassion, 1))),
            }),

        new EventDef("teen_camp", EventType.Pulse, Teen, "L'île du lac",
            "Chaque été, les jeunes du village passent trois jours sur l'île du lac, avec un sac, une tente, et le moineau comme moniteur. Le radeau part dans une heure. {A} a fait son sac trois fois, et l'a défait deux fois.",
            new[]
            {
                Plain("light", "Partir avec presque rien, pour l'aventure",
                    "{A} emporte une couverture, un couteau à beurre et beaucoup d'optimisme. Sur le radeau, le moineau regarde le sac de {A}, puis {A}, et soupire profondément.",
                    E(new GrowStat(PlynlingStat.Courage), new FollowUp("teen_camp_night", 12, 24)), Ai((AiAxis.Boldness, 2))),
                Plain("everything", "Emporter tout ce qui pourrait servir",
                    "{A} emporte trois couvertures, une lanterne, une boussole, du miel, des pansements et un parapluie. Le radeau penche. Personne ne rit : tout le monde sait déjà chez qui on frappera quand la pluie viendra.",
                    E(new GrowStat(PlynlingStat.Stewardship), new FollowUp("teen_camp_night", 12, 24)), Ai((AiAxis.Rationality, 2))),
                Plain("stay", "Rester au village : trois jours, c'est long",
                    "{A} regarde le radeau partir depuis le ponton. Le village est très calme sans les jeunes. {A} aide la tortue au café, et le soir, entend des chansons qui viennent de l'île, portées par l'eau.",
                    E(new ApplyModifier("soothed")), Ai((AiAxis.Boldness, -1), (AiAxis.Energy, -1))),
            }),

        new EventDef("teen_camp_night", EventType.FollowUp, AnyStage, "La nuit sur l'île",
            "Première nuit sur l'île. Le feu crépite, le moineau ronfle déjà sous sa tente, et quelqu'un vient de raconter l'histoire du monstre du lac, qui sort les nuits sans lune. Ce soir, justement, pas de lune. Et derrière les roseaux, quelque chose fait « plop ».",
            new[]
            {
                Try("look", "Aller voir ce qui fait « plop »", new EventChallenge(PlynlingStat.Courage, 7),
                    "{A} écarte les roseaux, la lanterne levée. Le monstre du lac est une vieille carpe, énorme, qui fait des bulles en dormant. {A} revient au feu et raconte, très sérieusement, avoir vu le monstre. C'est vrai, en un sens.",
                    "{A} s'avance, la lanterne levée. « Plop. » {A} recule. « Plop. » {A} est déjà sous la tente, sous trois couvertures. Le lendemain, on découvre une grenouille qui s'ennuyait.",
                    E(new GrowStat(PlynlingStat.Courage), new FollowUp("teen_camp_back", 24, 36)), E(new FollowUp("teen_camp_back", 24, 36)), Ai((AiAxis.Boldness, 2)), Stress(("craven", 20))),
                Try("prank", "Faire « plop » à son tour, pour effrayer les autres", new EventChallenge(PlynlingStat.Intrigue, 7),
                    "{A} se glisse derrière la tente et fait « plop » avec un caillou dans l'eau. Panique générale, cris, une tente qui s'effondre. Le moineau se réveille, compte les têtes, et surveille {A} avec méfiance jusqu'à la fin du séjour.",
                    "{A} lance un caillou pour faire « plop ». Le caillou tombe dans la marmite de chocolat. Tout le monde sait d'où vient le caillou. On boit le chocolat quand même, caillou compris.",
                    E(new GrowStat(PlynlingStat.Intrigue), new FollowUp("teen_camp_back", 24, 36)), E(new FollowUp("teen_camp_back", 24, 36)), Ai((AiAxis.Boldness, 1), (AiAxis.Honor, -1))),
                Plain("story", "Raconter une autre histoire, plus rassurante",
                    "{A} raconte l'histoire du monstre du lac qui avait peur du noir, et qui venait, les nuits sans lune, se réchauffer près des feux de camp. Plus personne n'a peur. Quelqu'un laisse même une couverture près des roseaux, au cas où.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new FollowUp("teen_camp_back", 24, 36)), Ai((AiAxis.Compassion, 1), (AiAxis.Sociability, 1))),
            }),

        new EventDef("teen_camp_back", EventType.FollowUp, AnyStage, "Le radeau du retour",
            "Le dernier matin, on démonte les tentes. Sur le radeau du retour, tout le monde est fatigué, sale et heureux. Le moineau distribue les médailles du séjour, des bouchons peints, une par personne. Le moineau arrive devant {A} avec la dernière, et hésite sur l'inscription.",
            new[]
            {
                Plain("medal", "Accepter la médaille, quoi qu'on y lise",
                    "La médaille dit : « Le plus inattendu ». {A} ne sait pas si c'est un compliment. Le moineau non plus. {A} la porte quand même tout l'été, et finit par décider que oui.",
                    E(new LiftNeed(Need.Happiness, 0.2), new ApplyModifier("light_heart")), Ai((AiAxis.Sociability, 1))),
                Plain("swim", "Sauter du radeau, et finir à la nage",
                    "{A} saute du radeau à mi-chemin et finit la traversée à la nage, sous les acclamations. Le moineau crie quelque chose sur la sécurité, puis saute aussi, parce que tout le monde saute. Le radeau arrive vide au ponton.",
                    E(new GrowStat(PlynlingStat.Courage), new LiftNeed(Need.Hygiene, 0.2)), Ai((AiAxis.Boldness, 1), (AiAxis.Energy, 1))),
                Plain("keepsake", "Garder un souvenir de l'île au fond de la poche",
                    "Au fond de la poche de {A} : une coquille d'escargot vide, en spirale parfaite, ramassée sur la plage de l'île. Posée sur la table de chevet, la coquille sent encore l'eau douce et le feu de camp.",
                    E(new GiveItem("col.coquille_escargot"), new ApplyModifier("cherished")), Ai((AiAxis.Compassion, 1))),
            }),
    };
}
