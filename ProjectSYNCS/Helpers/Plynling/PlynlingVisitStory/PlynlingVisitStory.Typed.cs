using System.Text.RegularExpressions;
using ProjectSYNCS.Models;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Helpers;

public static partial class PlynlingVisitStory
{
    // The typed-passion moments (see Build): {S} is the Plynling whose typed passion it is, {L} the other,
    // {P} the typed text in « guillemets ». Like every {P} line, the text never follows « de », « à »,
    // « du » or « au », nothing refers back to it by pronoun, and it never opens a sentence (it is
    // lowercase). Keyed by flavour: the arrival uses the bond before the visit, the rest the mood.
    public static readonly IReadOnlyDictionary<ConvoFlavor, string[]> TypedArrivals = new Dictionary<ConvoFlavor, string[]>
    {
        [ConvoFlavor.Friendly] = new[]
        {
            "{S} arrive en fredonnant une chanson dont les paroles, en entier, sont : {P}.",
            "{S} arrive avec un dessin roulé sous le bras. Titre : {P}. {L} n'ose pas demander ce que c'est. {L} demande quand même.",
            "Premier mot de {S} en arrivant, avant même bonjour : {P}. Le bonjour vient après, un peu en retard.",
            "{S} a écrit sur son bras, à l'encre, en lettres appliquées : {P}. « C'est pour ne pas oublier. » Personne n'aurait oublié.",
            "{L} voit arriver {S} de loin, avec une pancarte. Sur la pancarte : {P}. C'est tout ce qu'il y a sur la pancarte.",
            "{S} arrive les yeux brillants : {s:il|elle} a rêvé de sa passion toute la nuit. Sa passion, c'est {P}. Le rêve était en couleurs.",
            "{S} arrive en retard. Son excuse : {P}. {L} ne pose pas de questions. Avec {S}, l'excuse est toujours la même.",
            "« Tu ne devineras jamais à quoi j'ai pensé en venant », lance {S}. {L} devine du premier coup : {P}.",
            "{S} arrive avec un cadeau pour {L}, emballé dans un papier où {s:il|elle} a écrit partout, en tout petit : {P}.",
            "{S} arrive, s'arrête, et annonce solennellement, sans que personne n'ait rien demandé : {P}. Puis dit bonjour.",
            "{L} n'a pas encore dit bonjour que {S} en est déjà au troisième point de son exposé. Sujet : {P}.",
            "{S} a brodé un petit écusson pour l'occasion. Dessus, un peu de travers : {P}.",
        },
        [ConvoFlavor.Tense] = new[]
        {
            "{S} arrive avec un badge bien en vue : {P}. C'est une provocation. {L} le prend comme une provocation.",
            "{S} arrive en marmonnant quelque chose. {L} tend l'oreille. C'est encore sa passion : {P}.",
            "« Je ne suis pas {s:venu|venue} pour toi, mais pour ma passion : {P}. » {L} lève les yeux au ciel, très haut.",
            "{S} arrive, jette un regard à {L}, et écrit dans la poussière, en grosses lettres : {P}. Comme on plante un drapeau.",
            "{L} attendait {S} de pied ferme. {S} arrive, et commence par un avertissement : aujourd'hui, pas un mot contre sa passion. Pour mémoire : {P}.",
            "{S} arrive en sifflotant l'hymne qu'{s:il|elle} a inventé pour sa passion. Titre : {P}. {L} sifflote plus fort, autre chose.",
            "{S} arrive avec une pile de feuilles, qui portent toutes le même titre : {P}. {L} sent venir un très long moment.",
            "« Tiens. Toi. » « Tiens. Toi. » Puis {S} ajoute, parce qu'il faut toujours qu'{s:il|elle} ajoute quelque chose : {P}.",
            "{S} arrive avec une écharpe brodée. Dessus : {P}. {L} n'a rien contre l'écharpe. {L} a tout contre {S}.",
            "{S} arrive et pose ses conditions. Première condition : {P}. Il n'y en a pas d'autre.",
        },
    };

    // A typed exchange whose first line (the visitor's) belongs to the passion's owner…
    public static readonly IReadOnlyDictionary<ConvoFlavor, string[]> TypedExchangesOwnerFirst = new Dictionary<ConvoFlavor, string[]>
    {
        [ConvoFlavor.Friendly] = new[]
        {
            "Tu sais à quoi je pense ?\nLaisse-moi deviner : {P}.",
            "J'ai rêvé de ma passion, cette nuit : {P}, en couleurs.\nEt j'étais dans le rêve ?",
            "Si je devais choisir entre toi et {P}…\nNe finis pas cette phrase. Je préfère ne pas savoir.",
            "Tu veux que je t'explique {P} ?\nD'accord. Mais cette fois, on s'arrête avant la nuit.",
            "Un jour, j'écrirai un livre sur ma passion : {P}.\nJe le lirai. Même les passages ennuyeux. Surtout les passages ennuyeux.",
            "J'ai appris un nouveau truc sur {P}.\nRaconte. J'ai tout mon temps, et un goûter.",
            "Ça te dérange, si je reparle de ma passion ?\nTu veux dire : {P} ? Vas-y. Je m'assois.",
            "Tu me promets de ne jamais te moquer de ma passion ?\nJamais. C'est sacré, {P}.",
        },
        [ConvoFlavor.Tense] = new[]
        {
            "Au moins, moi, j'ai une vraie passion : {P}.\nEt moi, une patience infinie. Sinon, je t'aurais {s:quitté|quittée} depuis longtemps.",
            "Tu n'as jamais rien compris à ma passion.\nJ'ai très bien compris : {P}. C'est bien ça, le problème.",
            "Ma passion ne t'a rien fait, tu sais.\nNon. C'est toi qui m'en parles depuis une heure.",
            "Je parie que tu ne sais même pas ce que c'est, {P}.\nJe sais. Tu me l'as expliqué quatorze fois.",
            "Un jour, tout le village aimera {P}.\nEt ce jour-là, je déménage.",
            "Ris, si tu veux. Moi, au moins, j'ai {P}.\nEt moi, j'avais ma tranquillité. Avant que tu arrives.",
        },
    };

    // …and one whose second line (the host's) belongs to the owner.
    public static readonly IReadOnlyDictionary<ConvoFlavor, string[]> TypedExchangesOwnerSecond = new Dictionary<ConvoFlavor, string[]>
    {
        [ConvoFlavor.Friendly] = new[]
        {
            "Tu penses encore à ta passion, là ?\nÀ quoi d'autre ? C'est toute ma vie, {P}.",
            "Tu as l'air ailleurs.\nJe suis ailleurs. Je suis quelque part avec {P}.",
            "Raconte-moi encore ta passion.\nEncore ? Bon. Tout a commencé avec {P}…",
            "Tu m'apprendrais ta passion ?\nTu veux vraiment apprendre {P} ? Alors dès demain.",
            "Qu'est-ce que tu fais, quand tu es triste ?\nJe pense à ma passion : {P}. Ça marche presque toujours.",
            "Tu as un secret ?\nUn seul, et tu le connais déjà : {P}.",
            "Si tu devais emporter une seule chose sur une île déserte ?\nToi. Et {P}, si ça rentre dans le sac.",
            "Je peux te poser une question sur {P} ?\nTu viens de me rendre très {s:heureux|heureuse}.",
        },
        [ConvoFlavor.Tense] = new[]
        {
            "Tu vas encore nous parler de ta passion ?\nOui. Ça s'appelle {P}, et tu vas écouter.",
            "Tu ne penses vraiment qu'à ça ?\nNon. Parfois, je pense aussi à te battre. Mais surtout, oui : {P}.",
            "Ta passion est ridicule.\nTa jalousie aussi. Et {P} se porte très bien, merci.",
            "Je ne veux plus entendre parler de ta passion.\nTrop tard. Je l'ai brodée sur ton coussin : {P}.",
            "Pourquoi tu souris ?\nJe pensais à ma passion : {P}. Et à ta tête, tout à l'heure.",
            "Tu as encore écrit ta passion quelque part chez moi ?\nSur ta porte. En rouge : {P}.",
        },
    };

    public static readonly IReadOnlyDictionary<ConvoFlavor, string[]> TypedDepartures = new Dictionary<ConvoFlavor, string[]>
    {
        [ConvoFlavor.Friendly] = new[]
        {
            "{S} repart en répétant tout bas : {P}, {P}, {P}. {L} ne sait pas si c'est une chanson ou une prière.",
            "Au moment de partir, {S} glisse un petit papier dans la main de {L}. Dessus : {P}. Rien d'autre. {L} le garde quand même.",
            "{S} s'éloigne en racontant sa passion à qui veut l'entendre. Il n'y a personne. {S} continue quand même : {P}.",
            "« Tu penseras à moi ? » demande {L}. « Chaque fois que je penserai à ma passion », répond {S}. Ça fait souvent : sa passion, c'est {P}.",
            "{L} regarde {S} partir. Au loin, on l'entend encore dire : {P}.",
            "{S} laisse derrière {s:lui|elle} une petite affiche, au bord du chemin : {P}. {L} ne l'enlève pas. Ça fait joli.",
            "{S} part en courant, avec une idée toute neuve pour sa passion : {P}, en mieux.",
            "Avant de partir, {S} fait promettre à {L} d'essayer au moins une fois : {P}. {L} promet. {L} tiendra peut-être.",
            "{S} s'en va en dessinant dans l'air des formes que {L} ne comprend pas. Ça a sûrement un rapport avec {P}.",
            "Dernier mot de {S}, de loin, les mains en porte-voix : {P} ! {L} fait un signe. C'est devenu leur au revoir.",
        },
        [ConvoFlavor.Tense] = new[]
        {
            "{S} s'en va en criant, pour que tout le quartier l'entende : {P} ! {L} se bouche les oreilles.",
            "{S} repart en laissant derrière {s:lui|elle}, bien en évidence, une brochure sur sa passion : {P}. {L} s'en sert pour s'éventer.",
            "Juste avant de partir, {S} lance : « Un jour, tu comprendras ma passion. » {L} répond : « Un jour, tu comprendras les silences. »",
            "{S} part sans dire au revoir, mais en fredonnant sa passion : {P}. C'est pire.",
            "{L} regarde {S} s'éloigner, et se surprend à murmurer : {P}. Horreur. C'est contagieux.",
            "{S} griffonne sa passion sur le premier caillou venu, et le laisse derrière {s:lui|elle} : {P}. {L} le retourne, face contre terre.",
            "« Je reviendrai avec des preuves », annonce {S}. Des preuves pour sa passion : {P}. {L} a déjà hâte de ne pas les lire.",
            "{Ils} se séparent sur un désaccord total. Sur quoi ? Sur {P}, évidemment.",
        },
    };
}
