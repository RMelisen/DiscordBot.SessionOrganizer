using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

// Wave 8 short events: ancien. New events go at the end of this file.
public static partial class PlynlingEvents
{
    private static EventDef[] ShortElder() => new[]
    {
        // ---- ancien
        new EventDef("elder_birthdays", EventType.Pulse, Elder, "Les fêtes d'avant",
            "Au fond d'un tiroir, {A} retrouve une vieille boîte de bougies à moitié fondues. Une par fête : les moissons, la grande crue, le mariage des hérissons. Au village, plus personne ne s'en souvient. {A} se souvient de toutes, une par une.",
            new[]
            {
                Plain("cake", "Se faire un gâteau, rien que pour soi",
                    "{A} fait un petit gâteau, plante la plus vieille bougie de la boîte, et chante, sans public. À la deuxième phrase, on frappe : la tortue, avec un gâteau aussi. La tortue s'en souvenait.",
                    E(new ApplyModifier("cherished")), Ai((AiAxis.Compassion, 1))),
                Plain("remember", "Se rappeler les fêtes d'autrefois",
                    "{A} s'assoit au soleil et se souvient : le gâteau tombé, la surprise ratée, la fois où tout le village avait chanté faux. Le soir, {A} est {a:heureux|heureuse} comme après une grande fête.",
                    E(new ApplyModifier("soothed")), Ai((AiAxis.Rationality, 1))),
                Plain("tell", "Raconter tout ça au café",
                    "« Vous vous souvenez de la fête des moissons, la vraie ? » Le café se tait, puis tout le monde la refait, là, tout de suite, et chante, très fort, très faux. La tortue sort une bougie de sous le comptoir. La tortue a toujours une bougie sous le comptoir.",
                    E(new LiftNeed(Need.Happiness, 0.25)), Ai((AiAxis.Sociability, 2))),
            }),

        new EventDef("elder_old_face", EventType.Pulse, Elder, "Un visage d'autrefois",
            "Au marché, une vieille cigogne s'arrête net devant {A}. « Toi ! La grande partie de balle ! Tu avais marqué contre ton camp ! » {A} n'en a aucun souvenir.",
            new[]
            {
                Plain("pretend", "Faire semblant de s'en souvenir",
                    "{A} fait semblant, et la cigogne raconte toute l'histoire. Au bout d'un moment, {A} s'en souvient vraiment, et les deux rient au milieu du marché comme des jeunes.",
                    E(new LiftNeed(Need.Happiness, 0.2)), Ai((AiAxis.Sociability, 1))),
                Plain("admit", "Avouer qu'on ne se souvient de rien",
                    "« Aucun souvenir. » La cigogne réfléchit, puis rit : « Moi non plus, en fait. C'était peut-être quelqu'un d'autre. » Les deux prennent un thé quand même, pour vérifier.",
                    E(new ApplyModifier("clear_conscience")), Ai((AiAxis.Honor, 2))),
            }),

        new EventDef("elder_unsent_letter", EventType.Pulse, Elder, "La lettre jamais envoyée",
            "Au fond d'un tiroir, {A} retrouve une lettre jamais envoyée : des excuses, écrites quarante ans plus tôt, pour une dispute dont {A} ne se rappelle plus la raison. Le destinataire, un vieux blaireau, habite toujours à l'autre bout du village.",
            new[]
            {
                Plain("send", "L'envoyer, enfin",
                    "{A} glisse la lettre dans la boîte. Trois jours plus tard, une réponse : « J'avais oublié la dispute. Toi, non. Viens prendre le thé. »",
                    E(new ApplyModifier("cherished"), new ApplyModifier("clear_conscience")), Ai((AiAxis.Honor, 1), (AiAxis.Compassion, 1))),
                Plain("visit", "Porter la lettre en personne",
                    "{A} traverse le village, frappe, et tend la lettre. Le vieux blaireau la lit sur le seuil, puis serre {A} dans ses bras, sans rien dire, très longtemps.",
                    E(new ApplyModifier("soothed"), new LiftNeed(Need.Happiness, 0.15)), Ai((AiAxis.Boldness, 1), (AiAxis.Compassion, 1))),
                Plain("burn", "La brûler : c'est du passé",
                    "{A} brûle la lettre dans la cheminée. Ce qui devait être dit l'a été, quelque part, un jour, ou ne compte plus. La flamme est petite et chaude.",
                    E(new ApplyModifier("soothed")), Ai((AiAxis.Rationality, 1))),
            }),

        new EventDef("elder_cane", EventType.Pulse, Elder, "La canne",
            "Le médecin a conseillé une canne à {A}. « Pour la sécurité. » Posée contre la porte, la canne a tout d'une ennemie.",
            new[]
            {
                Plain("carve", "La sculpter, et en faire une œuvre",
                    "{A} sculpte la canne : un escargot en haut, des feuilles tout le long, un héron au bout. Le village s'arrête pour l'admirer. {A} en oublierait presque que c'est une canne.",
                    E(new GrowStat(PlynlingStat.Stewardship)), Ai((AiAxis.Rationality, 1))),
                Plain("corner", "La laisser au coin de la porte",
                    "{A} laisse la canne au coin de la porte, par principe. Au bout d'une semaine, la canne sert à attraper les pommes du haut. C'est un début.",
                    E(new GrowStat(PlynlingStat.Courage)), Ai((AiAxis.Boldness, 1))),
                Plain("adopt", "L'adopter, sans faire d'histoires",
                    "{A} prend la canne et se promène jusqu'à l'étang, plus loin que depuis des mois. La canne n'est pas une ennemie. C'est une collègue.",
                    E(new ApplyModifier("soothed")), Ai((AiAxis.Rationality, 1))),
            }),

        new EventDef("elder_evening_bench", EventType.Pulse, Elder, "Le banc du soir",
            "Le soleil se couche derrière le moulin. {A} est {a:assis|assise} sur le banc devant chez soi, comme chaque soir. Le village rentre, et les lumières s'allument une à une.",
            new[]
            {
                Plain("count", "Compter les fenêtres qui s'allument",
                    "{A} compte les fenêtres qui s'allument : trente-deux. Chacune, {A} la connaît. Chacune a une histoire. Le compte est bon.",
                    E(new ApplyModifier("soothed")), Ai((AiAxis.Rationality, 1))),
                Plain("wave", "Saluer chaque passant",
                    "{A} salue chaque passant : le hérisson, l'ours, trois petits, la tortue qui rentre lentement. Tout le monde rend le salut. C'est une bonne journée qui se termine.",
                    E(new LiftNeed(Need.Happiness, 0.15)), Ai((AiAxis.Sociability, 1))),
            }),

        new EventDef("elder_pardon", EventType.Pulse, Elder, "Pardon ?",
            "Depuis quelque temps, {A} entend un peu moins bien. Ce matin, au marché, {A} a demandé un chocolat et reçu un chapeau. Le chapeau est très joli, cela dit.",
            new[]
            {
                Plain("keep", "Garder le chapeau, et le porter",
                    "{A} paie le chapeau, le met, et redemande un chocolat, plus fort. Le chocolat arrive. Le chapeau reste. Bonne affaire.",
                    E(new LiftNeed(Need.Happiness, 0.15)), Ai((AiAxis.Rationality, 1))),
                Plain("horn", "Se fabriquer un cornet acoustique",
                    "{A} fabrique un cornet en écorce. Ça marche très bien. Peut-être trop : {A} entend maintenant tous les potins du café, même ceux du fond.",
                    E(new GrowStat(PlynlingStat.Intrigue)), Ai((AiAxis.Rationality, 1), (AiAxis.Boldness, 1))),
            }),

        new EventDef("elder_knitting", EventType.Pulse, Elder, "Les aiguilles",
            "Un petit du village regarde {A} tricoter depuis une heure, sans un mot, la bouche ouverte. Les aiguilles cliquettent.",
            new[]
            {
                Plain("teach", "Lui apprendre, maille par maille",
                    "{A} pose de petites aiguilles dans les pattes du petit. Maille à l'endroit, maille à l'envers. Au bout d'une semaine, le petit tricote une écharpe bosselée, et l'offre à {A}.",
                    E(new ApplyModifier("cherished")), Ai((AiAxis.Compassion, 2))),
                Plain("gift", "Finir l'écharpe, et la lui offrir",
                    "{A} finit l'écharpe et la noue autour du cou du petit. Le petit la porte toute la saison, même les jours chauds.",
                    E(new LiftNeed(Need.Happiness, 0.15)), Ai((AiAxis.Compassion, 1))),
            }),

        new EventDef("elder_knees", EventType.Pulse, Elder, "Les genoux savent",
            "Les genoux de {A} annoncent la pluie. Ce matin, les genoux annoncent un orage, un gros. Le ciel est pourtant tout bleu, et le village se moque gentiment.",
            new[]
            {
                Plain("warn", "Prévenir tout le monde quand même",
                    "{A} prévient tout le monde. On rit. À quinze heures, l'orage éclate. À quinze heures cinq, le village entier vient demander l'avis des genoux pour la semaine prochaine.",
                    E(new GrowStat(PlynlingStat.Diplomacy)), Ai((AiAxis.Sociability, 1))),
                Plain("tea", "Se faire un thé, et attendre",
                    "{A} se fait un thé, ferme les volets, et attend. L'orage éclate à l'heure dite. Les genoux avaient raison. Les genoux ont toujours raison.",
                    E(new ApplyModifier("soothed")), Ai((AiAxis.Rationality, 1))),
            }),

        new EventDef("elder_before_trains", EventType.Pulse, Elder, "Avant les trains",
            "Un petit du village demande à {A}, très sérieusement : « C'était comment, avant les trains ? » {A} ne se savait pas si {a:vieux|vieille}.",
            new[]
            {
                Plain("tale", "Inventer une histoire extraordinaire",
                    "{A} raconte qu'avant les trains, on voyageait à dos d'escargot géant, et que ça prenait des années. Le petit écoute, les yeux ronds. L'escargot, qui passait par là, ne dément pas.",
                    E(new ApplyModifier("light_heart")), Ai((AiAxis.Boldness, 1), (AiAxis.Sociability, 1))),
                Plain("truth", "Dire la vérité : les trains existaient déjà",
                    "« Les trains existaient déjà. Mais le hérisson n'était pas encore chef de gare. » Le petit trouve ça encore plus incroyable.",
                    E(new GrowStat(PlynlingStat.Diplomacy)), Ai((AiAxis.Honor, 1))),
            }),

        new EventDef("elder_sunrise", EventType.Pulse, Elder, "Le lever du soleil",
            "{A} s'est {a:réveillé|réveillée} avant l'aube, sans raison. Dehors, tout est gris et silencieux. Le soleil va bientôt se lever, derrière la colline.",
            new[]
            {
                Plain("hill", "Monter sur la colline pour le voir",
                    "{A} monte la colline, lentement, et arrive au sommet pile au moment où le soleil se lève sur toute la vallée. Ça faisait longtemps. Ça valait chaque pas.",
                    E(new GrowStat(PlynlingStat.Courage), new ApplyModifier("soothed")), Ai((AiAxis.Energy, 1))),
                Plain("window", "Le regarder par la fenêtre, avec un thé",
                    "{A} prépare un thé et s'installe à la fenêtre. Le soleil se lève sur les toits, la gare, le moulin. Le premier oiseau chante. La journée commence bien.",
                    E(new ApplyModifier("soothed")), Ai((AiAxis.Energy, -1))),
            }),
    };
}
