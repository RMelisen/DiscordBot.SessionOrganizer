using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

// Wave 8 story cycles: the Grand Concours, the regatta, the weasel, the flood.
public static partial class PlynlingEvents
{
    private static EventDef[] Wave8Stories() => new[]
    {
        // adulte et ancien: le Grand Concours (CK3's imperial examination: the notice, the night before,
        // a slip of paper in the hall, then four endings)
        new EventDef("grown_exam", EventType.Pulse, Grown, "Le Grand Concours",
            "Une affiche est clouée sur la porte de la bibliothèque, écrite à la plume par la chouette en personne : « Grand Concours. Une épreuve, un lauréat, un nom peint sur le tableau d'honneur. » Le tableau est dans l'entrée. Le dernier nom peint date de douze hivers, et la peinture s'écaille. {A} relit l'affiche trois fois.",
            new[]
            {
                Plain("study", "S'inscrire, et réviser dès ce soir",
                    "{A} s'inscrit en lettres soignées, emprunte onze livres, et les empile sur la table de la cuisine. Le premier est ouvert avant le dîner. Le dîner attendra.",
                    E(new GrowStat(PlynlingStat.Learning), new FollowUp("grown_exam_eve", 48, 96)), Ai((AiAxis.Rationality, 2)), Stress(("lazy", 15))),
                Plain("sign", "S'inscrire, pour voir",
                    "{A} signe d'un trait rapide, entre l'ours et l'escargot. L'escargot s'inscrit tous les ans, depuis toujours, et n'a encore jamais fini l'épreuve à temps.",
                    E(new FollowUp("grown_exam_eve", 48, 96)), Ai((AiAxis.Boldness, 1), (AiAxis.Sociability, 1))),
                Plain("cakes", "Ne pas s'inscrire, mais cuisiner pour les candidats",
                    "Le jour du concours, un plateau de gâteaux au miel attend devant la bibliothèque. Les candidats mangent tout. Le soir, la chouette fait savoir que le concours de l'an prochain aura une catégorie « pâtisserie ». Personne ne sait si c'est une blague.",
                    E(new GrowStat(PlynlingStat.Stewardship), new ApplyModifier("cherished")), Ai((AiAxis.Compassion, 2))),
            }),

        new EventDef("grown_exam_eve", EventType.FollowUp, AnyStage, "La veille du concours",
            "C'est la veille du concours. Les livres sont ouverts partout, même sur la chaise. Dehors, des rires : les autres candidats vont au café, « pour se détendre ». Et sur le banc devant chez {A}, un jeune loir sanglote dans son cahier, persuadé de tout rater.",
            new[]
            {
                Try("cram", "Réviser toute la nuit", new EventChallenge(PlynlingStat.Learning, 8),
                    "{A} révise jusqu'à ce que les étoiles pâlissent. À l'aube, tout est là, rangé, à sa place dans la tête. {A} part au concours les yeux rouges et l'esprit clair.",
                    "{A} révise jusqu'à l'aube, mais les mots finissent par danser sur la page. Au matin, {A} connaît par cœur la liste des fleuves… à l'envers.",
                    E(new ApplyModifier("inspired"), new FollowUp("grown_exam_paper", 12, 24)), E(new ApplyModifier("sleepless"), new FollowUp("grown_exam_paper", 12, 24)),
                    Ai((AiAxis.Rationality, 1), (AiAxis.Energy, 1))),
                Plain("cafe", "Rejoindre les autres au café",
                    "Au café, les candidats parlent de tout sauf du concours. La tortue sert des tisanes « pour la mémoire », et l'ours raconte son propre concours, perdu de très loin, avec une telle joie que plus personne n'a peur.",
                    E(new LiftNeed(Need.Happiness, 0.2), new FollowUp("grown_exam_paper", 12, 24)), Ai((AiAxis.Sociability, 2))),
                Plain("dormouse", "S'asseoir à côté du loir, et réviser à deux",
                    "{A} s'assoit à côté du loir, et les deux révisent ensemble, une question chacun. À minuit, le loir ne pleure plus. À une heure, le loir dort sur l'épaule de {A}, et {A} n'ose plus bouger.",
                    E(new ApplyModifier("clear_conscience"), new FollowUp("grown_exam_paper", 12, 24)), Ai((AiAxis.Compassion, 2))),
                Plain("sleep", "Se coucher tôt : une tête reposée vaut deux livres",
                    "{A} ferme les livres à huit heures, boit un lait chaud et dort comme une pierre. Au matin, la tête est légère. Un peu vide, peut-être, mais légère.",
                    E(new ApplyModifier("well_rested"), new FollowUp("grown_exam_paper", 12, 24)), Ai((AiAxis.Rationality, 1), (AiAxis.Energy, -1))),
            }),

        new EventDef("grown_exam_paper", EventType.FollowUp, AnyStage, "Le papier plié",
            "La salle de lecture a été vidée, et des pupitres alignés. La chouette distribue les sujets, puis s'installe sur son perchoir pour surveiller. Au bout d'une heure, la chouette dort. Un papier plié glisse alors jusqu'aux pattes de {A}, poussé par une patte inconnue. Dessus, d'une écriture minuscule : toutes les réponses.",
            new[]
            {
                Try("own", "Repousser le papier du pied, et écrire ses propres réponses", new EventChallenge(PlynlingStat.Learning, 8),
                    "{A} repousse le papier sous le pupitre voisin, du bout du pied, et écrit. Les mots viennent, un par un, puis tous ensemble. {A} pose la plume au moment où la chouette se réveille.",
                    "{A} repousse le papier et écrit. Mais la question trois porte sur les fougères, et {A} n'en connaît que la couleur. {A} rend sa copie le cœur un peu lourd, et la conscience légère.",
                    E(new FollowUp("grown_exam_laureate", 24, 48)), E(new FollowUp("grown_exam_honest", 24, 48)), Ai((AiAxis.Honor, 1), (AiAxis.Rationality, 1))),
                Try("report", "Réveiller la chouette, et lui remettre le papier", new EventChallenge(PlynlingStat.Learning, 7),
                    "{A} toussote. La chouette ouvre un œil, voit le papier, et le déchire en mille morceaux. Puis accorde à {A}, sans un mot, dix minutes de plus que tout le monde. Ces dix minutes suffisent.",
                    "{A} toussote. La chouette ouvre un œil, voit le papier, et le déchire en mille morceaux. Puis accorde à {A} dix minutes de plus. Dix minutes, ce n'est pas assez pour les fougères.",
                    E(new ApplyModifier("clear_conscience"), new FollowUp("grown_exam_laureate", 24, 48)), E(new ApplyModifier("clear_conscience"), new FollowUp("grown_exam_honest", 24, 48)),
                    Ai((AiAxis.Honor, 2))),
                Try("peek", "Jeter un coup d'œil, un seul", new EventChallenge(PlynlingStat.Intrigue, 8),
                    "{A} déplie le papier sous le pupitre, copie tout, et le fait disparaître dans sa manche avant que la chouette ne s'étire. Personne n'a rien vu. Presque rien.",
                    "{A} déplie le papier. Le papier craque. La chouette ouvre un œil, puis deux, et descend de son perchoir en silence.",
                    E(new FollowUp("grown_exam_hollow", 24, 48)), E(new FollowUp("grown_exam_caught", 6, 12)), Ai((AiAxis.Honor, -2), (AiAxis.Greed, 1)), Stress(("honest", 30), ("just", 20))),
            }),

        new EventDef("grown_exam_laureate", EventType.FollowUp, AnyStage, "Le tableau d'honneur",
            "Une semaine plus tard, tout le village se presse dans l'entrée de la bibliothèque. Le peintre a sorti ses pinceaux. La chouette déplie un papier, ajuste ses lunettes, et lit le nom du lauréat. C'est {A}. Le silence dure une seconde, puis l'escargot se met à applaudir, et tout le monde suit.",
            new[]
            {
                Try("speech", "Faire un discours, un vrai", new EventChallenge(PlynlingStat.Diplomacy, 8),
                    "{A} remercie la chouette, les livres, la tortue, et l'escargot, pour son exemple de persévérance. L'escargot pleure. L'ours aussi. On peint le nom de {A} en lettres dorées, et la chouette remet la bourse du concours.",
                    "{A} commence à remercier tout le monde et oublie la moitié du village. Le discours dure quarante minutes, à force de rattrapages. On peint le nom quand même, et la bourse est remise pendant la quarante et unième.",
                    E(new ApplyModifier("laureate"), new GiveCailloux(25)), E(new GiveCailloux(25)), Ai((AiAxis.Sociability, 2)), Stress(("shy", 20))),
                Plain("thanks", "Dire merci, et rien d'autre",
                    "« Merci. » Puis {A} descend de l'estrade. Le village trouve ça très élégant, et en parle pendant des semaines. Le nom de {A} est peint en lettres dorées, et la bourse glissée dans sa patte.",
                    E(new ApplyModifier("laureate"), new GiveCailloux(25)), Ai((AiAxis.Rationality, 1), (AiAxis.Sociability, -1))),
                Plain("library", "Offrir la bourse à la bibliothèque",
                    "{A} tend la bourse à la chouette : « Pour la réserve. » La chouette, émue, range ses lunettes trois fois de suite. Une semaine plus tard, une étagère neuve porte une petite plaque au nom de {A}.",
                    E(new ApplyModifier("laureate"), new ApplyModifier("clear_conscience")), Ai((AiAxis.Compassion, 1), (AiAxis.Honor, 1)), Stress(("greedy", 20))),
            }),

        new EventDef("grown_exam_honest", EventType.FollowUp, AnyStage, "La mention de la chouette",
            "Les résultats sont affichés. Le nom peint sur le tableau d'honneur n'est pas celui de {A}, mais celui d'un jeune loir, qui n'en revient pas. En bas de la liste, pourtant, d'une écriture minuscule, la chouette a ajouté une ligne : « Mention spéciale : {A}, pour la copie la plus honnête. »",
            new[]
            {
                Plain("congrats", "Aller féliciter le loir",
                    "{A} serre la patte du loir, qui tremble encore. Le loir murmure : « C'est un peu grâce à toi, tu sais. » {A} ne sait pas trop pourquoi, mais rentre avec quelque chose de chaud dans la poitrine.",
                    E(new ApplyModifier("cherished")), Ai((AiAxis.Compassion, 2))),
                Plain("next", "Recopier la liste des livres, pour l'an prochain",
                    "{A} recopie la liste des livres, en ajoute deux, et punaise le tout au-dessus du lit. L'an prochain, le nom sera peint. En attendant, on révise les fougères.",
                    E(new GrowStat(PlynlingStat.Learning)), Ai((AiAxis.Rationality, 1), (AiAxis.Energy, 1))),
                Plain("frame", "Encadrer la mention",
                    "La mention est découpée, encadrée, et accrochée au-dessus de la cheminée. Les visiteurs la lisent à voix haute. {A} fait semblant de ne pas écouter, à chaque fois.",
                    E(new LiftNeed(Need.Happiness, 0.2), new ApplyModifier("clear_conscience")), Ai((AiAxis.Sociability, 1))),
            }),

        new EventDef("grown_exam_caught", EventType.FollowUp, AnyStage, "Pris sur le fait",
            "La chouette se tient devant le pupitre de {A}, le papier plié entre deux plumes. Toute la salle a cessé d'écrire. « Ce papier », dit la chouette, très doucement. « Je t'écoute. »",
            new[]
            {
                Plain("confess", "Tout avouer",
                    "{A} avoue tout, d'une petite voix. La chouette hoche la tête, déchire la copie, et en tend une neuve. « Recommence, avec ta tête à toi. Tu as une heure. » {A} écrit pendant une heure, moins bien, mais vrai. Le soir, sur la copie, un mot de la chouette : « Mieux. »",
                    E(new GrowStat(PlynlingStat.Learning), new ApplyModifier("clear_conscience")), Ai((AiAxis.Honor, 2))),
                Try("deny", "Jurer que le papier est arrivé tout seul", new EventChallenge(PlynlingStat.Intrigue, 9),
                    "« Arrivé tout seul, je le jure. » La chouette fixe {A} un long moment, puis le papier, puis la rangée de derrière, où un jeune corbeau regarde ses pattes avec beaucoup d'intérêt. La chouette va s'asseoir à côté du corbeau, et n'en bouge plus.",
                    "« Arrivé tout seul, je le jure. » La chouette retourne le papier : au dos, en tout petit, le nom de {A}, sur un vieux brouillon de {A}. Le silence de la salle est terrible. {A} rentre avant la fin.",
                    E(new GrowStat(PlynlingStat.Intrigue)), E(new ApplyModifier("guilty")), Ai((AiAxis.Honor, -2)), Stress(("honest", 30))),
            }),

        new EventDef("grown_exam_hollow", EventType.FollowUp, AnyStage, "Des lettres dorées",
            "Le nom de {A} est peint sur le tableau d'honneur, en lettres dorées. Tout le village applaudit. La chouette serre la patte de {A} : « Je n'avais jamais lu de réponses aussi justes. » Au fond de la manche de {A}, le petit papier plié pèse aussi lourd qu'une pierre.",
            new[]
            {
                Plain("confess", "Tout avouer, là, devant tout le monde",
                    "{A} sort le papier de sa manche et le tend à la chouette, devant tout le village. Silence. Puis la chouette prend un pinceau, efface le nom, et écrit à la place, en petit : « Le plus courageux. » Le village n'a jamais vu de tableau d'honneur aussi bizarre, ni aussi beau.",
                    E(new GrowStat(PlynlingStat.Courage), new ApplyModifier("clear_conscience")), Ai((AiAxis.Honor, 2))),
                Plain("keep", "Sourire, saluer, et garder le papier dans sa manche",
                    "{A} sourit, salue, et empoche la bourse. Le nom brille au soleil. Ensuite, chaque fois que {A} passe devant la bibliothèque, {A} change de trottoir.",
                    E(new GiveCailloux(25), new ApplyModifier("guilty")), Ai((AiAxis.Greed, 2), (AiAxis.Honor, -1)), Stress(("honest", 30), ("just", 20))),
                Plain("burn", "Brûler le papier, et mériter le nom après coup",
                    "Le soir, {A} brûle le papier dans la cheminée et regarde la petite flamme bleue. Le nom reste sur le tableau. {A} révise pourtant tous les soirs de l'année suivante, pour mériter, avec un peu de retard, ce qui y est écrit.",
                    E(new GrowStat(PlynlingStat.Learning), new ApplyModifier("laureate")), Ai((AiAxis.Rationality, 1), (AiAxis.Honor, 1))),
            }),

        // adulte et ancien: la Régate des Feuilles (CK3's chariot race: the build, the start, then
        // the lead or the back of the field)
        new EventDef("grown_regatta", EventType.Pulse, Grown, "La Régate des Feuilles",
            "Dimanche, c'est la Régate des Feuilles sur l'étang : des bateaux grands comme une main, faits de ce qu'on trouve, et poussés par le vent. Le premier prix est une bourse, et le droit de porter toute l'année la casquette de l'amiral. {A} a une semaine pour construire un bateau.",
            new[]
            {
                Plain("fast", "Un bateau fin et rapide, en écorce de bouleau",
                    "{A} taille une coque fine comme une feuille de papier, et une voile en plume de pie. Au premier essai, le bateau file si vite que {A} doit le rattraper à la nage.",
                    E(new GrowStat(PlynlingStat.Stewardship), new FollowUp("grown_regatta_start", 48, 96)), Ai((AiAxis.Boldness, 1), (AiAxis.Rationality, 1))),
                Plain("sturdy", "Une coque de noix, solide comme un rocher",
                    "{A} choisit la plus grosse coque de noix du marché, la calfeutre à la cire et y plante un mât en brindille. Le bateau n'est pas rapide. Le bateau ne coulera jamais. C'est une philosophie.",
                    E(new GrowStat(PlynlingStat.Stewardship), new FollowUp("grown_regatta_start", 48, 96)), Ai((AiAxis.Rationality, 2))),
                Plain("pretty", "Le plus beau bateau de l'étang, avec un pavillon",
                    "{A} peint la coque en rouge, coud un pavillon brodé à son nom, et ajoute un minuscule équipage en glands. Les passants s'arrêtent pour regarder. Personne ne pose la question de la flottaison.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new FollowUp("grown_regatta_start", 48, 96)), Ai((AiAxis.Sociability, 2))),
            }),

        new EventDef("grown_regatta_start", EventType.FollowUp, AnyStage, "Le départ",
            "Vingt bateaux sont alignés entre deux roseaux. Le moineau tient le sifflet, l'ours tient les paris, et tout le village tient la rive. À côté du bateau de {A} : celui de la pie, noir et luisant, et celui du vieil escargot, qui participe depuis quarante ans sans jamais avoir fini. Coup de sifflet. Le vent se lève.",
            new[]
            {
                Try("gap", "Foncer dans le trou entre deux bateaux", new EventChallenge(PlynlingStat.Courage, 8),
                    "{A} souffle dans la voile et vise le trou. Le bateau s'y faufile à un cheveu près, frôle celui de la pie, et passe en tête au premier virage. Sur la rive, quelqu'un crie le nom de {A}.",
                    "{A} vise le trou. Le trou se referme. Le bateau rebondit sur celui de la pie, tourne trois fois sur lui-même, et repart… dans le mauvais sens.",
                    E(new FollowUp("grown_regatta_lead", 2, 6)), E(new FollowUp("grown_regatta_back", 2, 6)), Ai((AiAxis.Boldness, 2)), Stress(("craven", 20))),
                Try("wind", "Lire le vent, et attendre la bonne rafale", new EventChallenge(PlynlingStat.Learning, 8),
                    "{A} attend. Les autres partent, s'emmêlent, se cognent. Puis la rafale arrive, exactement comme prévu, et pousse le bateau de {A} au-dessus de la mêlée, jusqu'en tête.",
                    "{A} attend la bonne rafale. La bonne rafale ne vient pas. Une mauvaise vient à la place, et range le bateau de {A} soigneusement derrière tous les autres.",
                    E(new FollowUp("grown_regatta_lead", 2, 6)), E(new FollowUp("grown_regatta_back", 2, 6)), Ai((AiAxis.Rationality, 2))),
                Plain("duckling", "Redresser le bateau d'un caneton qui chavire",
                    "À peine parti, le bateau d'un caneton chavire. {A} se penche, le redresse, l'égoutte, et le remet à l'eau. Le caneton repart, ravi. Le bateau de {A}, lui, est désormais tout au fond de la course.",
                    E(new ApplyModifier("cherished"), new FollowUp("grown_regatta_back", 2, 6)), Ai((AiAxis.Compassion, 2))),
            }),

        new EventDef("grown_regatta_lead", EventType.FollowUp, AnyStage, "La dernière bouée",
            "Le bateau de {A} est en tête, seul, à l'approche de la dernière bouée. La pie est trop loin pour inquiéter. Mais le long de la rive, sans un bruit, une petite coque grise remonte : le bateau du vieil escargot, porté par un courant que personne n'avait vu. Sur la rive, l'escargot ne respire plus.",
            new[]
            {
                Try("push", "Tout donner jusqu'à la ligne", new EventChallenge(PlynlingStat.Stewardship, 8),
                    "{A} tend la voile au maximum. Le bateau franchit la ligne avec une longueur d'avance. Le moineau siffle, l'ours compte la bourse, et on pose sur la tête de {A} la casquette de l'amiral, un peu grande. L'escargot, deuxième, est fou de joie : l'escargot n'avait jamais fini.",
                    "{A} tend la voile. La voile se déchire. Le bateau gris passe, lentement, majestueusement, et franchit la ligne. Quarante ans d'attente. Le village pleure. {A}, deuxième, pleure aussi, de joie, ou presque.",
                    E(new GiveCailloux(30), new ApplyModifier("fired_up")), E(new GiveCailloux(10), new LiftNeed(Need.Happiness, 0.15)), Ai((AiAxis.Boldness, 1), (AiAxis.Energy, 1))),
                Plain("let", "Laisser passer l'escargot",
                    "{A} relâche la voile, juste un peu. Le petit bateau gris passe, lentement, et franchit la ligne en premier. L'escargot ne dit rien pendant une minute entière. Puis l'escargot offre à {A} sa vieille coquille de rechange, gardée quarante ans pour ce jour-là.",
                    E(new GiveItem("col.coquille_escargot"), new ApplyModifier("clear_conscience")), Ai((AiAxis.Compassion, 2)), Stress(("ambitious", 15))),
            }),

        new EventDef("grown_regatta_back", EventType.FollowUp, AnyStage, "Au fond de l'étang",
            "Le bateau de {A} est dernier, très dernier, coincé entre un nénuphar et une famille de grenouilles qui trouve ça très drôle. Devant, la course continue sans {A}. Mais {A} connaît l'étang : à gauche, derrière les roseaux, s'ouvre un passage étroit que personne n'emprunte jamais.",
            new[]
            {
                Try("reeds", "Prendre le passage des roseaux", new EventChallenge(PlynlingStat.Intrigue, 8),
                    "Le bateau de {A} disparaît dans les roseaux, et ressort de l'autre côté, juste derrière la pie. Troisième place ! Les grenouilles, qui ont tout suivi, applaudissent avec leurs pattes palmées, ce qui fait un bruit très particulier.",
                    "Le bateau de {A} disparaît dans les roseaux, et n'en ressort pas. On le retrouve le lendemain, décoré de lentilles d'eau, avec un têtard à bord. Le têtard refuse de descendre.",
                    E(new GrowStat(PlynlingStat.Intrigue), new GiveCailloux(10)), E(new LiftNeed(Need.Happiness, 0.15)), Ai((AiAxis.Boldness, 1), (AiAxis.Rationality, 1))),
                Plain("parade", "Profiter de la promenade, et saluer la rive",
                    "{A} renonce à la course et salue la foule, comme à une parade. Le bateau dérive doucement jusqu'à la ligne, bon dernier, sous la plus grande ovation de la journée. Le moineau invente un prix : « Le plus beau voyage ».",
                    E(new ApplyModifier("light_heart"), new LiftNeed(Need.Happiness, 0.1)), Ai((AiAxis.Sociability, 2))),
                Plain("frogs", "Engager les grenouilles comme équipage",
                    "{A} négocie avec les grenouilles : une mouche chacune, payable à l'arrivée. Les grenouilles poussent le bateau à la nage, un peu dans tous les sens. Septième place. L'équipage réclame sa paye, et une revanche.",
                    E(new GrowStat(PlynlingStat.Diplomacy)), Ai((AiAxis.Sociability, 1), (AiAxis.Rationality, 1))),
            }),

        // adulte et ancien: la belette du moulin (CK3's party baron: admire, envy or befriend the one
        // everyone loves; the three branches meet at the mill party)
        new EventDef("grown_darling", EventType.Pulse, Grown, "La belette du moulin",
            "Une belette s'est installée dans le vieux moulin, et en trois semaines, tout le village l'adore. La belette connaît le prénom de chacun, rit à toutes les blagues de l'ours, et donne des fêtes où même le hérisson arrive en retard. Ce matin encore, au café, on ne parle que de la belette. {A} remue son thé.",
            new[]
            {
                Plain("watch", "Observer la belette, pour percer son secret",
                    "{A} s'installe au fond du café, un carnet sur les genoux. La belette entre, et en une minute, salue onze personnes par leur nom, demande des nouvelles d'un genou, et complimente un chapeau. {A} note tout. Ça va être un long carnet.",
                    E(new FollowUp("grown_darling_lesson", 24, 72)), Ai((AiAxis.Rationality, 1), (AiAxis.Sociability, 1))),
                Plain("envy", "Trouver que la belette, c'est surfait",
                    "« Surfait », dit {A} tout haut, dans le café silencieux. Toutes les têtes se tournent. {A} boit son thé avec une grande dignité, en se brûlant un peu. La guerre est déclarée, au moins d'un côté.",
                    E(new FollowUp("grown_darling_rival", 24, 72)), Ai((AiAxis.Vengefulness, 1), (AiAxis.Boldness, 1)), Stress(("content", 15))),
                Plain("knock", "Monter frapper à la porte du moulin",
                    "{A} monte au moulin avec un pot de confiture, et frappe. Rien. Puis la porte s'ouvre sur la belette, couverte de farine jusqu'aux oreilles, l'air un peu perdu. « Oh. Personne ne frappe jamais. On vient directement aux fêtes. Entre. »",
                    E(new FollowUp("grown_darling_visit", 24, 72)), Ai((AiAxis.Sociability, 2))),
            }),

        new EventDef("grown_darling_lesson", EventType.FollowUp, AnyStage, "Le carnet",
            "Le carnet de {A} est plein, et le secret de la belette tient en une page : se souvenir des prénoms, poser des questions, écouter les réponses, et rire quand c'est drôle, pas avant. Ça paraît simple. {A} décide d'essayer la méthode sur l'ours, ce matin, au marché.",
            new[]
            {
                Try("method", "Appliquer la méthode, à la lettre", new EventChallenge(PlynlingStat.Diplomacy, 8),
                    "{A} demande à l'ours des nouvelles de son genou, se souvient de son parfum de glace préféré, et écoute jusqu'au bout l'histoire de la grande tempête. L'ours, ému, offre un cornet. Le soir, une invitation arrive du moulin.",
                    "{A} demande à l'ours des nouvelles de son genou. L'ours n'a jamais eu mal au genou, et passe l'après-midi à s'inquiéter. Le soir, une invitation arrive quand même : la belette a entendu l'histoire, et l'a trouvée irrésistible.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new ApplyModifier("well_spoken"), new FollowUp("grown_darling_party", 48, 96)), E(new FollowUp("grown_darling_party", 48, 96)),
                    Ai((AiAxis.Sociability, 2))),
                Plain("own", "Refermer le carnet : on ne copie pas les gens",
                    "{A} referme le carnet et le range. On n'est pas une belette. Ce soir-là, au café, {A} raconte une histoire à sa façon, maladroite, trop longue. La tortue rit au bon moment. Le lendemain, une invitation arrive du moulin.",
                    E(new ApplyModifier("clear_conscience"), new FollowUp("grown_darling_party", 48, 96)), Ai((AiAxis.Honor, 1), (AiAxis.Rationality, 1))),
            }),

        new EventDef("grown_darling_rival", EventType.FollowUp, AnyStage, "La contre-fête",
            "Pour prouver qu'on peut s'amuser sans belette, {A} donne une fête le même soir que celle du moulin. Guirlandes, gâteaux, musique : tout est prêt, sauf les invités. À huit heures, la salle est vide. À huit heures et demie, on frappe à la porte.",
            new[]
            {
                Plain("open", "Ouvrir, en se préparant au pire",
                    "Sur le seuil : la belette, un gâteau dans les pattes, et derrière, la moitié du village. « On a annulé la nôtre, dit la belette. Une fête sans toi, ça n'avait pas de sens. » {A} reste bouche bée, puis s'écarte pour laisser entrer tout le monde.",
                    E(new LiftNeed(Need.Happiness, 0.2), new FollowUp("grown_darling_party", 72, 120)), Ai((AiAxis.Sociability, 1))),
                Plain("hide", "Souffler les bougies, et faire le mort",
                    "{A} souffle les bougies. On frappe encore. Puis un papier glisse sous la porte : « On a vu la lumière. On laisse le gâteau devant. Viens au moulin samedi ? La belette. » Le gâteau est délicieux. C'est le pire.",
                    E(new LiftNeed(Need.Hunger, 0.2), new FollowUp("grown_darling_party", 72, 120)), Ai((AiAxis.Sociability, -1))),
                Try("full", "Ouvrir, et annoncer que c'est complet", new EventChallenge(PlynlingStat.Intrigue, 8),
                    "« Complet », dit {A}. La belette regarde la salle vide derrière {A}, sourit, et repart sans un mot. Le lendemain, tout le village parle de la fête « complète » de {A}. Personne n'y était. Tout le monde voudrait y avoir été.",
                    "« Complet », dit {A}. Derrière, un ballon se dégonfle avec un long sifflement. La belette essaie de ne pas rire, très fort, et échoue. {A} finit par rire aussi. Les deux mangent tout le gâteau sur les marches.",
                    E(new GrowStat(PlynlingStat.Intrigue), new ApplyModifier("sly"), new FollowUp("grown_darling_party", 72, 120)), E(new LiftNeed(Need.Hunger, 0.2), new FollowUp("grown_darling_party", 72, 120)),
                    Ai((AiAxis.Boldness, 1), (AiAxis.Honor, -1)), Stress(("honest", 15))),
            }),

        new EventDef("grown_darling_visit", EventType.FollowUp, AnyStage, "Le moulin sans fête",
            "Dans le moulin, sans les lampions ni la foule, tout est très calme. La belette verse le thé, et ses pattes tremblent un peu. « Je donne des fêtes, avoue la belette, parce que le moulin grince la nuit. Avec du monde, on ne l'entend pas. »",
            new[]
            {
                Plain("tea", "Revenir prendre le thé, les soirs sans fête",
                    "{A} revient le lendemain, et le surlendemain. Le moulin grince toujours, mais les deux en rient maintenant, et lui ont même donné un nom. Quand vient la grande fête du samedi, la belette garde à {A} la meilleure place.",
                    E(new ApplyModifier("cherished"), new FollowUp("grown_darling_party", 72, 120)), Ai((AiAxis.Compassion, 2))),
                Try("grease", "Graisser les rouages, pour faire taire le moulin", new EventChallenge(PlynlingStat.Stewardship, 8),
                    "{A} grimpe dans les rouages avec une burette et graisse tout, jusqu'à la dernière dent. Le soir, silence complet. La belette dort douze heures d'affilée, et se réveille avec une seule idée : une fête, en l'honneur de {A}.",
                    "{A} graisse les rouages. Le moulin ne grince plus. Le moulin chante, maintenant, une note haute et tremblante, toute la nuit. La belette trouve ça plus joli, finalement, et l'invitation pour samedi arrive avec une partition.",
                    E(new GrowStat(PlynlingStat.Stewardship), new FollowUp("grown_darling_party", 72, 120)), E(new FollowUp("grown_darling_party", 72, 120)),
                    Ai((AiAxis.Rationality, 1), (AiAxis.Compassion, 1))),
            }),

        new EventDef("grown_darling_party", EventType.FollowUp, AnyStage, "La fête du moulin",
            "Samedi soir. Le moulin brille de cent lanternes. On danse dans la cour, on mange dans l'escalier, et l'escargot porte un nœud papillon. À minuit, la belette grimpe sur un tonneau, lève son verre de jus de pomme, et cherche quelqu'un dans la foule. Son regard s'arrête sur {A}. « À {A} ! » Et tout le moulin répète.",
            new[]
            {
                Try("toast", "Grimper sur un tonneau, et porter un toast en retour", new EventChallenge(PlynlingStat.Diplomacy, 8),
                    "{A} grimpe sur le tonneau d'à côté. « À la belette, qui a appris au moulin à se taire et au village à danser ! » Le moulin explose. On porte {A} et la belette jusqu'au matin, en chantant faux.",
                    "{A} grimpe sur le tonneau d'à côté, qui roule. {A} porte le toast en roulant à travers la cour, puis jusque dans la mare. Le moulin trouve ça encore mieux qu'un discours.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new ApplyModifier("party_soul")), E(new ApplyModifier("party_soul")), Ai((AiAxis.Sociability, 2)), Stress(("shy", 20))),
                Plain("dance", "Danser jusqu'à l'aube",
                    "{A} danse avec l'ours, avec le hérisson, avec trois grenouilles à la fois et, pour finir, avec la belette, pendant que le soleil se lève sur la roue du moulin. Personne ne se souvient de la musique. Tout le monde se souvient de la danse.",
                    E(new ApplyModifier("party_soul"), new LiftNeed(Need.Happiness, 0.25)), Ai((AiAxis.Energy, 2))),
                Plain("roof", "S'éclipser sur le toit, regarder les étoiles",
                    "Vers deux heures, {A} et la belette grimpent sur le toit du moulin. En bas, la fête continue toute seule. La belette montre les étoiles une par une, et leur donne des noms d'habitants du village. Une étoile porte désormais le nom de {A}. Discrètement.",
                    E(new ApplyModifier("cherished"), new ApplyModifier("soothed")), Ai((AiAxis.Compassion, 1), (AiAxis.Sociability, 1))),
            }),

        // adulte et ancien: la grande crue (CK3's natural disasters: the night, the shelter, the mud,
        // the thank-you supper)
        new EventDef("grown_flood", EventType.Pulse, Grown, "La nuit de la crue",
            "Trois jours de pluie, sans arrêt. Cette nuit, un grondement réveille {A} : la rivière est sortie de son lit. L'eau monte dans les rues basses, et on entend crier du côté du café. Pas le temps de réfléchir : où courir d'abord ?",
            new[]
            {
                Try("cellar", "Sauver la cave à confitures de la tortue", new EventChallenge(PlynlingStat.Courage, 8),
                    "{A} plonge dans la cave, de l'eau jusqu'au cou, et remonte les pots un par un, jusqu'au tout premier, étiqueté d'une année que personne ne connaît. La tortue serre le pot contre sa carapace, sans un mot. Les mots, ce sera pour plus tard.",
                    "{A} plonge dans la cave et remonte… un pot de cornichons. Un seul. La tortue le regarde, regarde {A}, {a:trempé|trempée}, et éclate de rire au milieu de la nuit et de l'eau. C'est déjà ça.",
                    E(new GrowStat(PlynlingStat.Courage), new FollowUp("grown_flood_shelter", 6, 12)), E(new FollowUp("grown_flood_shelter", 6, 12)), Ai((AiAxis.Boldness, 2)), Stress(("craven", 20))),
                Plain("snail", "Porter l'escargot en haut de la colline",
                    "La maison de l'escargot, c'est l'escargot. {A} le porte à deux mains jusqu'en haut de la colline, en courant, sous la pluie. L'escargot, qui n'est jamais allé aussi vite de sa vie, demande si on peut recommencer.",
                    E(new ApplyModifier("cherished"), new FollowUp("grown_flood_shelter", 6, 12)), Ai((AiAxis.Compassion, 2))),
                Plain("bell", "Sonner la cloche de la gare pour réveiller tout le monde",
                    "{A} court à la gare et sonne la cloche du hérisson, à toute volée. En cinq minutes, le village entier est debout, en chemise de nuit, et s'organise. Le hérisson arrive en dernier, pour la première fois de sa vie, et ne fera jamais le moindre reproche.",
                    E(new GrowStat(PlynlingStat.Stewardship), new FollowUp("grown_flood_shelter", 6, 12)), Ai((AiAxis.Rationality, 1), (AiAxis.Energy, 1))),
            }),

        new EventDef("grown_flood_shelter", EventType.FollowUp, AnyStage, "Un toit pour tout le monde",
            "La maison de {A} est sur la hauteur, et l'eau ne l'atteint pas. Alors on frappe à la porte, toute la nuit : le blaireau et ses couvertures, trois grenouilles ravies, le moineau trempé, l'escargot. Au matin, on ne voit plus le plancher, et tout ce petit monde a faim.",
            new[]
            {
                Try("organize", "Organiser les rations et les lits, avec une liste", new EventChallenge(PlynlingStat.Stewardship, 8),
                    "{A} fait une liste, puis deux, puis un tableau. Chacun reçoit un coin, une couverture et une part de pain. Le blaireau, impressionné, prend des notes. Au bout de trois jours, la maison tourne comme une horloge : celle de la gare, pas celle du café.",
                    "{A} fait une liste, mais les grenouilles la mangent. Le reste du séjour se fait au hasard, dans la bonne humeur et un certain désordre. Personne n'a faim. Personne ne sait vraiment comment.",
                    E(new GrowStat(PlynlingStat.Stewardship), new ApplyModifier("trade_sense"), new FollowUp("grown_flood_mud", 24, 48)), E(new FollowUp("grown_flood_mud", 24, 48)),
                    Ai((AiAxis.Rationality, 2))),
                Plain("pantry", "Vider le garde-manger, jusqu'à la dernière noix",
                    "{A} sort tout : le miel, les noix, la confiture des grandes occasions. Le petit-déjeuner dure jusqu'à midi. Le garde-manger est vide, mais la maison n'a jamais été aussi pleine.",
                    E(new ApplyModifier("cherished"), new FollowUp("grown_flood_mud", 24, 48)), Ai((AiAxis.Compassion, 2)), Stress(("greedy", 20))),
                Plain("stories", "Raconter des histoires, pour que la nuit passe",
                    "Chaque soir, à la lumière d'une bougie, {A} raconte une histoire, et chacun en raconte une à son tour. Celle de l'escargot dure deux soirs entiers. Personne ne pense plus à l'eau, dehors.",
                    E(new LiftNeed(Need.Happiness, 0.2), new FollowUp("grown_flood_mud", 24, 48)), Ai((AiAxis.Sociability, 2))),
            }),

        new EventDef("grown_flood_mud", EventType.FollowUp, AnyStage, "Ce que la rivière rend",
            "L'eau s'est retirée. Le village est couvert d'une boue épaisse, qui sent la rivière et les racines. Tout le monde sort les pelles. Et dans la boue, on trouve de tout : des cuillères, une botte, des choses que la rivière gardait depuis très longtemps.",
            new[]
            {
                Try("dig", "Fouiller la boue, là où le courant a tourné", new EventChallenge(PlynlingStat.Learning, 8),
                    "{A} devine où le courant a ralenti, et creuse là. Sous la boue : une vieille pièce frappée d'un pont qui n'existe plus. La rivière la gardait pour quelqu'un. Visiblement, pour {A}.",
                    "{A} creuse au mauvais endroit, et trouve une botte. La botte contient une grenouille. La grenouille a beaucoup d'opinions sur le dérangement.",
                    E(new GiveItem("col.piece_ancienne"), new FollowUp("grown_flood_supper", 48, 96)), E(new FollowUp("grown_flood_supper", 48, 96)), Ai((AiAxis.Rationality, 1), (AiAxis.Greed, 1))),
                Plain("cafe", "Dégager le café de la tortue, pelle après pelle",
                    "{A} pellette toute la journée devant le café. Le soir, la porte s'ouvre de nouveau, et la tortue sert le premier chocolat de l'après-crue, sur un comptoir encore humide. Gratuit, pour tout le monde, jusqu'à nouvel ordre.",
                    E(new LiftNeed(Need.Hunger, 0.2), new FollowUp("grown_flood_supper", 48, 96)), Ai((AiAxis.Compassion, 1), (AiAxis.Energy, 1))),
                Plain("nest", "Aider le héron à refaire son nid, sous le vieux pont",
                    "Le nid du héron est parti avec la crue. {A} rapporte des brindilles, de la mousse, une plume trouvée dans la boue. Le héron arrange tout, sans un mot, puis s'y installe, sur une patte, et regarde {A} une longue seconde. Venant du héron, c'est un discours.",
                    E(new ApplyModifier("clear_conscience"), new FollowUp("grown_flood_supper", 48, 96)), Ai((AiAxis.Compassion, 1), (AiAxis.Honor, 1))),
            }),

        new EventDef("grown_flood_supper", EventType.FollowUp, AnyStage, "Le souper de la crue",
            "Une semaine après la crue, le village dresse une grande table sur la place, avec tout ce qu'on a sauvé. La tortue a sorti un pot de sa plus vieille confiture. Avant le dessert, l'ours se lève et tape sur son verre. « On voudrait dire merci à quelqu'un. » Tous les yeux se tournent vers {A}.",
            new[]
            {
                Plain("purse", "Accepter les remerciements, et la petite bourse",
                    "Le village a fait une collecte : une petite bourse, cousue par le blaireau. {A} la prend, rougit, et dit merci à son tour. Le reste de la soirée, on raconte la crue, et chaque fois, l'histoire grandit un peu.",
                    E(new ApplyModifier("flood_hero"), new GiveCailloux(20)), Ai((AiAxis.Sociability, 1), (AiAxis.Greed, 1))),
                Plain("all", "Lever son verre à tout le village",
                    "{A} se lève : « Ce n'est pas moi. C'est nous. » Puis nomme tout le monde, un par un, grenouilles comprises. Le village applaudit, chacun pour chacun, et la confiture de la tortue fait trois fois le tour de la table.",
                    E(new ApplyModifier("flood_hero"), new ApplyModifier("clear_conscience")), Ai((AiAxis.Compassion, 1), (AiAxis.Honor, 1))),
                Plain("dishes", "S'éclipser à la cuisine pour faire la vaisselle",
                    "Quand on cherche {A} pour le discours, {A} est déjà à la cuisine, les manches retroussées, à essuyer les assiettes avec la tortue. Les deux parlent de la vieille confiture, de l'année sur l'étiquette, et de tout ce que la rivière a emporté, autrefois.",
                    E(new ApplyModifier("flood_hero"), new ApplyModifier("soothed")), Ai((AiAxis.Compassion, 1), (AiAxis.Sociability, -1))),
            }),
    };
}
