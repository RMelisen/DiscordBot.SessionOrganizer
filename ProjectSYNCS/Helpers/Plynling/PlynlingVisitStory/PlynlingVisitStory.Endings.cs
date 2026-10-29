using System.Text.RegularExpressions;
using ProjectSYNCS.Models;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Helpers;

public static partial class PlynlingVisitStory
{
    // The parting after a refused confession: the visitor ({A}) confessed, the host ({B}) said no.
    // The outcome line follows and the faces are sad whatever the line says (see DeparturePool).
    public static readonly string[] RefusedDepartures =
    {
        "{B} a répondu avec beaucoup de douceur. C'est presque pire. {A} repart en regardant ses pieds.",
        "{A} se répète « ce n'est rien » tout le long du chemin. Ça ne marche pas encore.",
        "{Ils} se disent au revoir un peu trop poliment, comme deux personnes qui ne savent plus quoi faire de leurs mains.",
        "{B} regarde {A} partir, le cœur serré {b:lui aussi|elle aussi}. Dire non ne fait de bien à personne.",
        "{A} repart en serrant, au fond de sa poche, le caillou qu'{a:il|elle} voulait offrir. Il pèse bien plus lourd qu'à l'aller.",
        "« On reste {p:amis|amies} ? » demande {B}. {A} hoche la tête. Il faudra un peu de temps. Mais oui.",
        "Sur le chemin, {A} donne un coup de pied dans un gland, puis s'excuse auprès du gland. Ce n'était pas sa faute non plus.",
        "{B} voudrait dire quelque chose de gentil. Tout ce qui lui vient est trop petit. {b:Il|Elle} se tait, et c'est mieux.",
        "{A} s'éloigne très droit, très digne. C'est au premier tournant qu'{a:il|elle} s'autorise à renifler.",
        "Autour, le monde continue comme si de rien n'était, ce qui est un peu vexant. {A} rentre quand même, un pas après l'autre.",
    };

    // The parting after a break-up: the two were a couple when the visit began and are not any more.
    public static readonly string[] BreakUpDepartures =
    {
        "{Ils} se séparent sans se disputer. C'est ce qui rend la chose si triste : il n'y a personne à qui en vouloir.",
        "{A} rend à {B} la fleur séchée qu'{a:il|elle} gardait sur {a:lui|elle}. {B} la lui redonne. Certaines choses peuvent rester.",
        "{Ils} font une partie du chemin ensemble, une dernière fois, sans se tenir la main. C'est étrange, des mains vides.",
        "« Tu vas me manquer. » « Toi aussi. » C'est vrai des deux côtés, et ça ne change rien. C'est bien ça, le plus dur.",
        "{B} regarde {A} s'éloigner, et se surprend à attendre qu'{a:il|elle} se retourne. {A} se retourne. Un tout petit signe. C'est tout.",
        "{A} et {B} se quittent comme on referme un livre qu'on a aimé : doucement, en gardant un doigt entre les pages.",
        "{Ils} décident de rester {p:amis|amies}. {Ils} le disent en même temps, pour se donner du courage.",
        "Le caillou en forme de cœur reste là où {ils} l'ont posé. Personne ne sait plus très bien à qui il appartient.",
        "{A} rentre par le chemin le plus long. Il faut du temps pour réapprendre à rentrer {a:seul|seule}.",
        "Plus tard, chez {b:lui|elle}, {B} sort deux tasses par habitude. {b:Il|Elle} en range une, très lentement.",
    };

    // Beat 3: the listener reacts to the subject, then the speaker answers — one \n between them.
    // Keyed by mood and by whether the listener shares the passion. Generic, so {P} stands alone
    // (after a colon, « pour », « sur », or as a subject — never after « de » or « à »).
    // Step 6: the talk winds down and turns into doing something. {S} is whoever says it here.
    public static readonly IReadOnlyDictionary<VisitMood, string[]> Closers = new Dictionary<VisitMood, string[]>
    {
        [VisitMood.Acquaintances] = new[]
        {
            "Bon… et si on essayait quelque chose, pour voir ?",
            "On pourrait… faire un truc ? Enfin, si tu veux. Tu n'es pas {l:obligé|obligée}.",
            "Je crois qu'on a épuisé la conversation. On passe à l'action ? Doucement ?",
            "J'ai une petite idée. Elle est petite, mais elle est à nous.",
            "Tu veux essayer ? Si c'est raté, on dira que c'était exprès.",
            "Viens. Enfin, si tu veux venir. Je veux dire : viens.",
            "On fait quelque chose ensemble ? Il paraît que ça aide, pour se connaître.",
            "Bon. Je propose qu'on arrête de parler avant de dire quelque chose de bizarre.",
        },
        [VisitMood.Friends] = new[]
        {
            "Bon, assez parlé. Viens, j'ai une idée. Elle est presque bonne.",
            "Tu sais ce qui serait encore mieux que d'en parler ? Le faire. Tout de suite.",
            "Le dernier arrivé range tout, après !",
            "Allez, viens. On verra bien ce que ça donne : c'est toujours le meilleur moment.",
            "J'ai une idée. Ne demande pas. Suis-moi.",
            "On arrête de parler, sinon on va encore oublier de s'amuser. Viens !",
            "Tope là. On essaie, et si on rate, on aura au moins bien ri.",
            "Et si on arrêtait d'en parler pour commencer, plutôt ?",
        },
        [VisitMood.BestFriends] = new[]
        {
            "Tu sais ce qu'on va faire ? La même chose que d'habitude. En mieux.",
            "Viens. J'ai gardé le meilleur pour toi.",
            "Arrête, je sais déjà ce que tu vas dire. Viens plutôt.",
            "J'ai eu une idée en t'écoutant. Tu vas adorer. Ou détester. Les deux, sûrement.",
            "Allez, on y va. Comme toujours : à deux, et sans plan.",
            "Tu te souviens de ce qu'on s'était promis de faire un jour ? C'est aujourd'hui.",
            "Plus un mot. Suis-moi, comme la dernière fois. Mais cette fois, on revient.",
            "On en reparlera plus tard. Là, on a mieux à faire.",
        },
        [VisitMood.Lovers] = new[]
        {
            "Viens. N'importe où, du moment que c'est avec toi.",
            "Donne-moi la main. On y va.",
            "J'ai une surprise. Ferme les yeux. Non, ouvre-les, sinon tu vas tomber. 💕",
            "On fait quelque chose de joli ensemble ? 💞",
            "Assez parlé. Je veux juste être avec toi. Et faire quelque chose, aussi. Mais surtout être avec toi.",
            "Tu viens ? J'ai une idée, et elle a besoin de nous deux.",
            "Allez, avant que je dise encore quelque chose de gênant. Viens. 💕",
            "Suis-moi. Promis, cette fois, je sais où on va. Presque.",
        },
        [VisitMood.Rivals] = new[]
        {
            "Assez parlé. Prouve-le.",
            "On règle ça tout de suite.",
            "Tu veux une démonstration ? Tu vas l'avoir.",
            "Un défi. Maintenant. Pas de discussion, et pas de revanche avant la fin.",
            "Bon. On va voir qui a raison. Indice : moi.",
            "Suis-moi, si tu l'oses. Et tu l'oses, je le sais.",
            "Trois, deux, un… Pourquoi tu ne bouges pas ? C'est parti !",
            "Le perdant range tout. Le gagnant regarde. Je vais regarder.",
        },
        [VisitMood.Conflict] = new[]
        {
            "Bon. On fait quelque chose, ou on reste là à se regarder ?",
            "Faisons quelque chose. Ça nous évitera de parler.",
            "J'ai une idée. Elle ne va pas te plaire. Tant mieux.",
            "On n'est d'accord sur rien, alors faisons quelque chose, pour changer.",
            "Allez. Qu'on en finisse.",
            "Et si on faisait quelque chose, avant que je m'énerve pour de bon ?",
            "Debout. On bouge. Je ne supporte plus ce silence.",
            "Très bien. Puisque c'est comme ça, on va jouer. Et je vais gagner.",
        },
    };

    // Beat 4 for a custom subject. No Conflict: in a conflict the activity is always the squabble.
    public static readonly IReadOnlyDictionary<VisitMood, string[]> CustomActivities = new Dictionary<VisitMood, string[]>
    {
        [VisitMood.Acquaintances] = new[] { "{S} montre à {L} deux ou trois choses sur sa passion : {P}. {L} pose des questions très polies, et en pense une sur deux.", "{Ils} essaient ensemble, pour voir. Thème : {P}. C'est hésitant, mais personne ne se moque.", "{S} fait une petite démonstration. Sujet : {P}. {L} applaudit, par politesse d'abord, puis pour de vrai.", "{S} prête ses notes à {L}. Titre en haut de la page : {P}. {L} les lit en entier. Même les ratures.", "{S} explique les bases. Sujet du jour : {P}. {L} hoche la tête très souvent, et retient au moins un mot.", "{Ils} découvrent le sujet ensemble, petit à petit : {P}. Pour une première fois, ce n'est pas si mal.", "{S} dessine un schéma dans la poussière pour expliquer sa passion : {P}. {L} penche la tête pour le voir dans le bon sens.", "{L} pose une question sur la passion de {S} : {P}. La réponse dure longtemps. {L} ne regrette pas d'avoir demandé. Presque pas." },
        [VisitMood.Friends] = new[] { "{Ils} inventent un jeu sur le moment. Thème imposé : {P}. Les règles changent dès le deuxième tour.", "{Ils} fabriquent une affiche géante. En gros, au milieu : {P}. Il y a beaucoup trop de paillettes.", "{S} invente un quiz. Sujet : {P}. {L} répond « caillou » à toutes les questions, et gagne quand même.", "{Ils} passent un long moment sur un seul sujet : {P}. Le temps file sans prévenir.", "{Ils} prennent des poses ridicules sur le thème : {P}. Chaque pose est plus absurde que la précédente.", "{Ils} improvisent une chanson dont le refrain, en entier, est : {P}.", "{Ils} fabriquent un petit objet souvenir. Gravé dessus, de travers : {P}.", "{S} fait visiter à {L} son coin secret, celui de sa passion : {P}. {L} promet de ne le dire à personne, et tiendra parole." },
        [VisitMood.BestFriends] = new[] { "{Ils} fondent un club secret. Thème : {P}. Membres : deux. Mot de passe : secret.", "{Ils} rangent leurs souvenirs dans une boîte. Sur le couvercle : {P}.", "{S} a préparé une surprise pour {L}, sur le thème qu'{s:il|elle} adore : {P}. {L} fait semblant d'être {l:étonné|étonnée}, puis l'est vraiment.", "{Ils} inventent un spectacle entier. Sujet : {P}. Public : un moineau. Il reste jusqu'au bout.", "{Ils} créent un langage secret qui ne sert qu'à parler d'une seule chose. La chose en question : {P}.", "{Ils} tiennent un carnet commun, rien que pour ça : {P}. Il est déjà presque plein.", "{Ils} montent une cabane dont le nom officiel est : {P}. Accès réservé aux deux membres.", "{L} offre à {S} un cadeau fait main, sur le thème : {P}. {S} ne s'en remet pas." },
        [VisitMood.Lovers] = new[] { "{S} fabrique un petit cadeau pour {L}, sur un thème bien précis : {P}. 💕", "{S} écrit un poème pour {L}. Titre : {P}. La dernière rime est « toujours ».", "{Ils} partagent un goûter en parlant d'un seul sujet : {P}. Ou peut-être d'autre chose. Surtout d'autre chose. 💞", "{S} emmène {L} voir quelque chose de spécial. Thème : {P}. {L} ne regarde que {S}.", "{Ils} s'écrivent des petits mots, tous sur le même thème : {P}. Et tous signés d'un cœur.", "{Ils} passent un long moment côte à côte, à rêver ensemble. Le rêve a un thème : {P}. 💞", "{L} a préparé une surprise pour {S}, sur le thème : {P}. {S} en a les yeux qui brillent.", "{L} essaie pour la première fois la passion de {S} : {P}. C'est raté. C'est le plus beau raté que {S} ait jamais vu. 💕" },
        [VisitMood.Rivals] = new[] { "Concours improvisé, un seul sujet : {P}. {Ils} se déclarent {p:tous|toutes} les deux {p:vainqueurs|gagnantes}.", "Duel de connaissances. Thème : {P}. Le score est serré. Il l'est toujours.", "{Ils} rédigent le règlement officiel. Sujet : {P}. Chaque règle a une exception inventée par l'autre.", "{S} lance un défi. Thème : {P}. {L} le relève sans même demander les règles.", "Concours de précision, thème imposé : {P}. {Ils} contestent tous les résultats, y compris les leurs.", "Défi chronométré. Sujet : {P}. Le chronomètre est un escargot. Personne ne sait qui a gagné.", "{S} se proclame {s:champion incontesté|championne incontestée}. Discipline : {P}. {L} conteste immédiatement.", "{Ils} fabriquent un trophée en écorce pour le vainqueur. Discipline : {P}. {Ils} se le disputent encore." },
    };
}
