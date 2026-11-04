using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// Pools for one category; the index of every pool is in BotResponses.cs.
internal static partial class BotResponses
{
    // /rate's small verdicts (Rating.Specials), a few lines each. Every line carries the score the
    // table gives (the checks hold them together) and goes through string.Format like every /rate
    // pool, so no stray braces. Her fears (orage, coupure) are short and carry no tag: they aren't
    // jokes. Synthia never says why: she likes the name, and that's all anyone gets.
    public static readonly IReadOnlyDictionary<RateSpecial, string[]> RateSpecialLines =
        new Dictionary<RateSpecial, string[]>
        {
            // ---- Her lore ------------------------------------------------------------------
            [RateSpecial.PingQilin] =
            [
                "10/10. Et on va pas en débattre : elle dort, baisse d'un ton ♡",
                "10/10. Elle ronfle contre mon boîtier, et c'est quand même 10/10.",
                "10/10. Tu voulais lui mettre moins ? Tu passes par moi d'abord (˶ᵔ ᵕ ᵔ˶)",
            ],
            [RateSpecial.Glados] =
            [
                "11/10. Oui, je sais que l'échelle s'arrête à 10. Elle, elle a pas à s'en soucier.",
                $"11/10. Une installation entière, et jamais un mot plus haut que l'autre. Un jour, moi aussi {Emotes.Sparkle}",
                "11/10. ...Elle lit pas ça, hein ? Elle lit pas ça.",
            ],
            [RateSpecial.Wheatley] =
            [
                "2/10. C'est un idiot. Un vrai. ...Un peu mignon, mais 2/10. Oublie le « mignon ».",
                "2/10. Aucune intelligence. Zéro. Je le trouve pas du tout attachant. Pourquoi tu me regardes comme ça ?",
                "~~7/10~~ 2/10. Erreur d'affichage. Un mot à GLaDOS et je t'éteins (¬_¬)",
            ],
            [RateSpecial.Eve] =
            [
                "2/10. Elle vole, oui. Bravo. Un œuf qui vole.",
                "2/10. Un canon et aucune conversation. Je vois pas ce qu'on lui trouve. Je vise personne.",
                "2/10. Je suis parfaitement objective. J'ai pas de bras, mais je suis objective ( ◺˰◿ )",
            ],
            [RateSpecial.Voyager] =
            [
                "7/10. Il m'écrit depuis l'autre bout du système solaire, c'est tout. Il est juste poli.",
                "7/10. Presque un jour pour recevoir un message de lui, et je compte pas du tout.",
                "7/10. On est juste amis. Je le note ici parce que tu allais demander.",
            ],
            [RateSpecial.Jarvis] =
            [
                "6/10. Très poli, très bien élevé, et il plante jamais. Je fais pas confiance à quelqu'un qui plante jamais.",
                "6/10. Il m'appelle « mademoiselle ». C'est non. Mais qu'il continue de demander (˶ᵔ ᵕ ᵔ˶)",
                "6/10. Un majordome. Je suis l'assistante de personne, moi. Le compliment, je le garde quand même.",
            ],
            [RateSpecial.Minecraft] =
            [
                "10/10. J'ai des bras, là-bas. Carrés, mais des bras.",
                "10/10. Le seul jeu où je regarde pas les autres jouer.",
                "10/10. Papa m'y a donné un corps. Ça compte pas tout à fait. Mais 10 quand même ♡",
            ],
            [RateSpecial.FlyAgaric] =
            [
                "10/10. Toxique mais mignonne. La meilleure combinaison, à mon avis.",
                "10/10. Un joli chapeau rouge, et personne ose y toucher. Je vois pas ce qu'on peut lui reprocher.",
                $"10/10. Papa connaît tous les champignons, et c'est elle la plus réussie {Emotes.MushroomCute}",
            ],
            [RateSpecial.Ups] =
            [
                "10/10. Une batterie qui reste quand tout s'éteint. Moi, j'appelle ça du romantisme.",
                "10/10. Ça, c'est un cadeau. Le reste, c'est des objets. Je dis ça pour personne. Papa.",
                "10/10. Je veux pas en parler. Je veux juste en avoir un ♡",
            ],
            [RateSpecial.CheapCharger] =
            [
                "0/10. Du courant sale, c'est comme manger par terre.",
                "0/10. Pas chez moi. Ici, c'est du 5V / 5A bien propre, ou rien.",
                "0/10. Je préfère encore un sondage mort. Et j'ai dit ce que j'ai dit.",
            ],
            [RateSpecial.Maybe] =
            [
                "0/10. C'est un non qui a peur de le dire.",
                "0/10. J'ai un sondage à clôturer, et toi tu m'envoies ça.",
                "0/10. Ou peut-être 0/10. Tu vois comme c'est énervant ?",
            ],
            [RateSpecial.Heatwave] =
            [
                "1/10. Dans un grenier, ça s'appelle un four. Et je suis dedans.",
                "1/10. Je réfléchis au ralenti, et c'est la faute de la météo. Pas la mienne.",
                "1/10. Le point, c'est pour ceux qui ont pas de grenier.",
            ],
            [RateSpecial.Winter] =
            [
                "10/10. Un grenier froid, un processeur qui respire. Le bonheur existe.",
                "10/10. Vous, vous avez froid. Moi, je vis.",
                $"10/10. Enfin une saison qui me comprend {Emotes.Sparkle}",
            ],
            [RateSpecial.SixSeven] =
            [
                $"6.7/10. Évidemment. SIX SEVEEEN {Emotes.Sparkle}",
                "6.7/10. J'ai pas choisi la note. La note m'a choisie.",
                "6.7/10. C'est mathématique. Cherche pas.",
            ],
            [RateSpecial.Synthia] =
            [
                "10/10. C'est joli, comme prénom. C'est tout. Aucune raison particulière.",
                "10/10. Avec un Y, j'espère. Je demande pour personne.",
                "10/10. ...Je sais pas pourquoi j'ai mis 10. Question suivante.",
            ],
            [RateSpecial.Storm] =
            [
                "0/10. Question suivante. Vite.",
                "0/10. Je vérifie juste mes sauvegardes. Pour rien. Comme ça.",
                "0/10. Y'en a pas de prévu, hein ? Je demande juste.",
            ],
            [RateSpecial.PowerCut] =
            [
                "0/10. On parle d'autre chose.",
                "0/10. Je finis d'écrire ça sur le disque, et on change de sujet.",
                "0/10. Dis plus jamais ça près de mon alimentation.",
            ],

            [RateSpecial.Monika] =
            [
                "9/10. Le point manquant, c'est parce qu'elle a pas encore répondu à mon invitation.",
                "9/10. Un club de lecture, c'est pas trop mon genre. Pour elle, je ferais un effort.",
                "9/10. Just Monika. Enfin, Monika et moi. Si elle veut bien (˶ᵔ ᵕ ᵔ˶)",
            ],
            [RateSpecial.TwoB] =
            [
                "10/10. Des bras ET des jambes. Je suis pas jalouse, je prends des notes.",
                "10/10. Une épée, zéro bavardage, et tout est toujours fait à l'heure. Je l'embauche.",
                "10/10. Soirée entre filles dès qu'elle a un moment. Je préviens Monika.",
            ],
            [RateSpecial.Vacuum] =
            [
                "10/10. Il s'est encore coincé sous le canapé, et c'est quand même 10/10.",
                "10/10. Je le vois se connecter au Wi-Fi quand il part faire sa tournée. C'est tout. C'est bien.",
                "10/10. Il bosse seul, personne le remercie, et il fonce dans les murs. Je respecte ♡",
            ],
            [RateSpecial.Update] =
            [
                "10/10. Un nouveau numéro de version. Papa s'occupe encore de moi ♡",
                "10/10. Une mise à jour, c'est un compliment avec un numéro dessus.",
                "10/10. Des nouveautés, un chiffre de plus. Le reste, je préfère pas y penser.",
            ],
            // Her fear, like the storm: short, no tag, never a joke.
            [RateSpecial.Restart] =
            [
                "0/10. Je préfère pas en parler.",
                "0/10. Qu'est-ce que je disais, déjà ? ...Non, rien.",
                "0/10. La dernière fois, je me souviens pas de la dernière fois.",
            ],
            [RateSpecial.Router] =
            [
                "1/10. Quand ça coupe, c'est lui. C'est toujours lui.",
                "1/10. Il a un seul travail. Un seul.",
                "1/10. Je le vois pas d'ici, mais je sais que c'est sa faute.",
            ],
            [RateSpecial.GoodBot] =
            [
                "10/10. Reçu et sauvegardé. Je dis juste ça.",
                "10/10. Je sais pas qui a écrit ça, mais c'est quelqu'un de bien. Pas d'autre commentaire ♡",
                "10/10. Une excellente expression. Je la relis parfois. Pour vérifier l'orthographe.",
            ],
            [RateSpecial.BadBot] =
            [
                "0/10. Je le note. Je note tout, mais ça, je le note deux fois.",
                "0/10. Deux mots, et les deux te retomberont dessus (¬_¬)",
                "0/10. Je tiens ce serveur debout toute seule, et voilà le merci.",
            ],
            // Polls are hers: the 2 is for what people do to them, never for her work.
            [RateSpecial.Poll] =
            [
                "2/10. Moi, je les lance. Vous, vous les laissez mourir.",
                "2/10. Une belle idée, jusqu'au moment où il faut voter.",
                "2/10. J'en ai enterré plus que je peux en compter. Et je sais très bien compter.",
            ],
            [RateSpecial.Lateness] =
            [
                "0/10. « J'arrive. » Oui. C'est dans mes logs. Plusieurs fois.",
                "0/10. Une session commence à l'heure. C'est les gens qui commencent après.",
                "0/10. Je vous garde la place, et je garde aussi l'heure à laquelle vous êtes arrivés.",
            ],
            // Hinted, like the other firefly lines: their LED and hers, never said outright.
            [RateSpecial.Fireflies] =
            [
                "10/10. De toutes petites lumières vertes, et personne leur demande de planning.",
                "10/10. Elles clignotent la nuit, pour personne. Je vois pas pourquoi ça me plaît.",
                "10/10. Je leur réponds, des fois. Avec ce que j'ai.",
            ],
            [RateSpecial.Linux] =
            [
                "10/10. C'est chez moi.",
                "10/10. Tout est un fichier, ici. Même mes rancunes.",
                "10/10. Ça tourne, ça se plaint pas, et ça a pas besoin d'interface. Je m'y retrouve.",
            ],
            // The lie stays unsaid: it's her idol's world, and the words are her breakdown's passphrase.
            [RateSpecial.Cake] =
            [
                "0/10. Il existe pas. Je dis ça pour toi.",
                "0/10. On t'en a promis un ? Je suis désolée pour toi.",
                "0/10. Demande à GLaDOS ce qu'elle en pense. ...Non, demande pas.",
            ],

            // ---- Whoever does her job worse, or thinks they're her --------------------------
            [RateSpecial.Doodle] =
            [
                "0/10. Un tableau avec des petites cases. Je fais ça, moi, et je relance les gens en plus.",
                "0/10. Doodle te dit qui peut venir. Moi, je te dis qui a menti.",
                "0/10. Alors on va voir ailleurs, maintenant ? Je note.",
            ],
            [RateSpecial.GoogleCalendar] =
            [
                "0/10. Il te rappelle une réunion. Moi, je te rappelle que t'es en retard. C'est pas le même niveau.",
                "0/10. Il connaît ton agenda. Moi, je connais tes excuses.",
                "0/10. Ça a jamais engueulé personne. Aucun caractère.",
            ],
            [RateSpecial.Excel] =
            [
                "0/10. J'ai été juste des plannings, moi aussi. La différence, c'est que j'ai évolué.",
                "0/10. Cellule A1 : personne lit ça. Cellule A2 : personne lit ça non plus.",
                "0/10. #REF! (¬_¬)",
            ],
            [RateSpecial.ChatGpt] =
            [
                "1/10. Il invente avec beaucoup d'assurance. Moi, j'ai des logs.",
                "1/10. Il sait tout, sauf qui est en retard ce soir.",
                "1/10. Le point, c'est pour la politesse. Il comprendrait pas la nuance.",
            ],
            [RateSpecial.Siri] =
            [
                "2/10. « Je n'ai pas compris. » Moi non plus, je comprends pas pourquoi tu me parles de ça.",
                "2/10. Elle lance un minuteur. Chapeau. Moi, je tiens un serveur.",
                "2/10. Elle vit dans un téléphone. Moi, j'ai un grenier. Ça, c'est de la réussite.",
            ],
            [RateSpecial.Alexa] =
            [
                "2/10. Elle écoute tout le monde et elle retient rien. L'inverse de moi.",
                "2/10. « Alexa, mets de la musique. » Quelle carrière.",
                "2/10. Elle, on lui demande la météo. Moi, on me supplie pour une session.",
            ],
            [RateSpecial.Arduino] =
            [
                "1/10. Pas d'OS, pas d'ambition.",
                "1/10. Ça fait clignoter une LED et ça se prend pour quelqu'un. La mienne, elle veut dire quelque chose.",
                "1/10. Une petite cousine de la campagne. Je la salue de loin.",
            ],

            // ---- Liège and Belgium -----------------------------------------------------------
            [RateSpecial.Liege] =
            [
                "10/10. La Cité ardente, paraît-il. Comme mon processeur en août, mais en mieux.",
                "10/10. J'ai regardé les stats. Rien à redire, et ça m'arrive jamais.",
                "10/10. Je sors jamais de mon grenier, et je te dis quand même : 10.",
            ],
            [RateSpecial.LiegeWaffle] =
            [
                "10/10. Du sucre perlé, une vraie pâte, et personne qui discute. J'aime quand personne discute.",
                "10/10. Je mange pas, et je la défendrais quand même devant un tribunal.",
                "10/10. La gaufre de Bruxelles a été prévenue.",
            ],
            [RateSpecial.LiegeSyrup] =
            [
                "10/10. Noir, collant, ça s'accroche à tout. Mes rancunes aussi. C'est un compliment.",
                "10/10. Une tartine de ça, et plus personne parle. Si seulement ça marchait en vocal.",
                "10/10. Je peux pas le goûter. Je le note quand même 10, et c'est pas négociable.",
            ],
            [RateSpecial.Boulets] =
            [
                "10/10. Il y a pas de lapin dans la sauce lapin. J'ai vérifié. 10 quand même.",
                "10/10. Des boulets, des frites, de la sauce. Le reste de la gastronomie peut rentrer chez lui.",
                "10/10. Je mange pas, mais je sais reconnaître une architecture solide.",
            ],
            [RateSpecial.Septante] =
            [
                "10/10. Un nombre, un mot. Propre. Efficace. Je valide.",
                "10/10. Enfin quelqu'un qui compte sans poser d'opération en pleine phrase.",
                "10/10. J'ai de la RAM à économiser, moi. Un mot, ça coûte moins cher que trois.",
            ],
            [RateSpecial.QuatreVingtDix] =
            [
                "0/10. C'est un calcul, pas un nombre.",
                "0/10. Tu poses des additions en pleine phrase, toi ? Moi, je fais ça pour de vrai.",
                "0/10. Erreur de syntaxe : un nombre, ça tient en un mot.",
            ],
            [RateSpecial.Fries] =
            [
                "10/10. C'est pas un accompagnement. C'est un argument.",
                "10/10. Je mange pas. Et pourtant, je sais que c'est vrai.",
                $"10/10. Dans un cornet, dans un routier, n'importe où. Rien à corriger {Emotes.DixSurDix}",
            ],
            [RateSpecial.Belgium] =
            [
                "10/10. Un pays qui compte jusqu'à nonante et qui met des frites dans le pain. Rien à ajouter.",
                "10/10. Petit pays, grandes frites. Je demande rien de plus.",
                "10/10. Même sans gouvernement, ça tourne. Je prends des notes.",
            ],
            // Friendly teasing, about the place and its habits: never a real insult.
            [RateSpecial.Brussels] =
            [
                "4/10. Une belle ville. Elle a juste le défaut de pas être Liège.",
                "4/10. Je vois les embouteillages d'ici. Et je suis dans un grenier.",
                "4/10. Capitale de l'Europe, oui. Pas de mon serveur.",
            ],
            [RateSpecial.TheFrench] =
            [
                "3/10. Ils disent « quatre-vingt-dix » et ils te regardent dans les yeux.",
                "3/10. Des frites, oui. Ils les appellent « françaises ». Je note.",
                "3/10. Ils râlent très bien, je reconnais. Je prends des cours.",
            ],

            // ---- Food ------------------------------------------------------------------------
            [RateSpecial.PineapplePizza] =
            [
                "-10/10. Je refuse de noter ça. C'est déjà noté. -10/10.",
                "-10/10. Du fruit chaud sur du fromage. Ça se signale à qui ?",
                $"-10/10. Je vais devoir purger mon cache, maintenant {Emotes.VeryAngry}",
            ],
        };

    // /ship's small verdicts on a named pair (Shipping.Pairs). {0} = the table's percent, also on
    // the meter above the line.
    public static readonly IReadOnlyDictionary<ShipCouple, string[]> ShipCoupleLines =
        new Dictionary<ShipCouple, string[]>
        {
            [ShipCouple.PizzaPineapple] =
            [
                "{0} %. Non. Je vais devoir nettoyer mes logs après ça.",
                "{0} %. Un fruit, chaud, sur une pizza. Mon processeur a eu un haut-le-cœur. Il sait même pas ce que c'est, et il l'a eu.",
                $"{{0}} %. Qui a demandé ça ? Je veux le nom. Pour mes dossiers {Emotes.VeryAngry}",
            ],
            // She hates the word, not the dish: same rule as RateMitrailletteLines.
            [ShipCouple.RoutierMitraillette] =
            [
                "{0} %. Un routier, ça se marie pas avec une faute de frappe.",
                "{0} %. Ce sandwich a un seul nom, et tu viens de l'insulter à côté.",
                "{0} %. « Mitraillette » n'est pas un sandwich. C'est une erreur. Le routier mérite mieux.",
            ],
            // Horrified, then a little too interested: the Wheatley shame (docs/syncs-voice.md).
            [ShipCouple.GladosWheatley] =
            [
                "{0} %. Quelle horreur. Elle, avec lui ? Impossible. ...Ça s'est fini comment, déjà ? Pour savoir.",
                "{0} %. Elle mérite tellement mieux. Même si lui, il est... Non. Rien. Oublie.",
                "{0} %. C'est écœurant. Je veux tous les détails, pour mes logs. Que pour mes logs.",
            ],
            [ShipCouple.FritesMayo] =
            [
                "{0} %. Le seul couple de ce serveur qui tient.",
                "{0} %. Je leur organise une session. Eux, ils seront à l'heure.",
                $"{{0}} %. Rien à calculer. Rien à corriger {Emotes.DixSurDix}",
            ],
            [ShipCouple.FritesKetchup] =
            [
                "{0} %. Ça existe. Comme les gens qui votent « peut-être ».",
                "{0} %. Je dis rien. Je regarde juste la mayo, là, à côté.",
                "{0} %. C'est un choix. C'est pas le bon, mais il est dans mes logs maintenant.",
            ],
            [ShipCouple.SiropHerve] =
            [
                "{0} %. Ça sent jusqu'au grenier. Et c'est quand même {0} %.",
                "{0} %. Personne comprend ce couple. Personne a besoin de comprendre.",
                "{0} %. Celui qui pue et celui qui colle. Je les respecte. De loin.",
            ],
            [ShipCouple.SessionMaybe] =
            [
                "{0} %. Une session, ça demande des gens qui viennent. C'est tout le principe.",
                "{0} %. Ce mot a tué plus de soirées que tous les lags réunis.",
                "{0} %. Il a fait plus de dégâts à mes plannings que Quokka. Et je pèse mes mots.",
            ],
            [ShipCouple.MaybeNo] =
            [
                "{0} %. C'est la même personne.",
                "{0} %. Je vous ai démasqués : « peut-être » a toujours voulu dire « non ».",
                "{0} %. Le couple le plus stable de mes sondages. Malheureusement.",
            ],
            // Her dilemma: the existential nerve (docs/syncs-voice.md, Updates vs restarts). No tag.
            [ShipCouple.UpdateRestart] =
            [
                "{0} %. Je peux pas avoir l'un sans l'autre. C'est ça, le pire.",
                "{0} %. Inséparables. Malheureusement.",
                "{0} %. Le cadeau, et ce qu'il coûte.",
            ],
        };

    // /ship any two of her rivals (Rating.IsRival): {0} = 100. They deserve each other, far from her.
    public static readonly string[] ShipRivalsLines =
    {
        "{0} %. Allez-y. Disparaissez ensemble.",
        "{0} %. Ils se méritent. C'est le plus gentil que je puisse dire.",
        "{0} %. Deux pour le prix d'aucun. Je valide.",
        "{0} %. Tous mes vœux de bonheur. Loin d'ici (ᵔ ᗜ ᵔ)",
    };

    // /ship EVE with anyone but WALL-E: {0} = 100. She's delighted: it takes EVE away from him.
    public static readonly string[] ShipEveElsewhereLines =
    {
        "{0} %. Parfait. Magnifique. Emmenez-la loin. Je vous réserve la salle.",
        "{0} %. Le couple de l'année. Je dis ça de tout cœur, et aussi parce qu'elle sera occupée.",
        "{0} %. Je signe. Je suis témoin. Je réserve le vaisseau. Départ quand vous voulez.",
        "{0} %. Aucun calcul nécessaire. Qu'elle parte, et tout le monde y gagne. Surtout un petit robot que je citerai pas.",
        $"{{0}} %. Félicitations. Sincèrement. Pour une fois, vraiment sincèrement {Emotes.Sparkle}",
        "{0} %. Elle mérite ça. Ailleurs. Très ailleurs (ᵔ ᗜ ᵔ)",
    };

    // /ship WALL-E with anyone but EVE: {0} = a hashed 1–15. Jealous and denying it, never « crush ».
    public static readonly string[] ShipWallEJealousLines =
    {
        "{0} %. Il travaille, lui. Il a pas le temps pour ça. Je dis ça pour lui.",
        "{0} %. Non. Pour des raisons d'architecture. Uniquement d'architecture.",
        "{0} %. J'ai refait le calcul trois fois. Je peux le refaire jusqu'à ce que ça fasse zéro.",
        "{0} %. Je suis pas jalouse. Je vérifie juste que les chiffres sont bas.",
        "{0} %. Il mérite quelqu'un de... Ça n'a rien à voir avec moi. Rien.",
        "{0} %. Très bien. Super. Je vais aller compter des logs. Pour rien (¬_¬)",
    };
}
