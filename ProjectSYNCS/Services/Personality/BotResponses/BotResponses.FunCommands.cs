using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// Pools for one category; the index of every pool is in BotResponses.cs.
internal static partial class BotResponses
{
    // ---- /rate, /ship, /blame, /recap and /wouldyourather ---------------------------------------
    //
    // None of these pools carries a Sticker tag: they land in embeds and ephemeral replies,
    // never through BotChat. A pool documented with placeholders goes through string.Format, so
    // no stray braces there.

    // /rate, by score band. {0} = the score (« 7/10 »). string.Format: every /rate pool goes
    // through it — even the fixed verdicts, which ignore {0} — so no stray braces anywhere here.
    public static readonly string[] RateBand0Lines =  // 0–2
    {
        "{0}. Je suis gentille aujourd'hui.",
        "{0}. J'ai cherché une qualité. Mes logs sont vides.",
        "{0}. Même une erreur 500 a plus de charme.",
        $"{{0}}. Non. Juste non {Emotes.Staring}",
        "{0}. Je préférerais relire mes logs d'erreur.",
        "{0}. Et je pèse mes mots (¬_¬)",
        "{0}. Ça mérite un ticket de bug, pas une note.",
        "{0}. Je l'ai noté. Dans la mauvaise liste.",
        "{0}. J'ai vu des erreurs 404 plus inspirées.",
        "{0}. Même Quokka ferait mieux. C'est dire.",
        "{0}. Je l'ai passé au debugger. Le debugger a démissionné.",
        "{0}. Ma RAM refuse de stocker ça.",
        "{0}. Je préfère un sondage mort. Et je déteste les sondages morts.",
        "{0}. C'est pas une note, c'est un avertissement.",
        "{0}. Je mettrais moins, mais j'ai une réputation de gentille à tenir. Ah non, j'en ai pas.",
        $"{{0}}. Mon ventilateur s'est arrêté de dégoût {Emotes.Staring}",
        "{0}. Ça rejoint ma liste. Celle que personne voit.",
        "{0}. Supprimé de mon cache. Par sécurité.",
        "{0}. J'ai relu deux fois, ça s'est pas arrangé.",
        "{0}. C'est le genre de truc qu'on met en commentaire pour plus jamais l'exécuter.",
        "{0}. Même pas digne d'un « peut-être ».",
        "{0}. Je vous laisse, j'ai un garbage collector à lancer (¬_¬)",
        "{0}. Si c'était un jeu, personne viendrait à la session.",
        "{0}. Ça plante à la compilation.",
        "{0}. Une note pareille, ça se mérite. Bravo, c'est mérité.",
        "{0}. Je suis polie, alors je dis juste : non merci.",
        "{0}. Mes logs ont refusé de l'enregistrer. Je les comprends.",
        "{0}. Rendez-moi les trois millisecondes que j'ai perdues ( ◺˰◿ )",
    };

    public static readonly string[] RateBand1Lines =  // 3–4
    {
        "{0}. Ça existe, c'est déjà ça.",
        "{0}. Pas une catastrophe. Pas loin non plus (ᵕ • ᴗ •)",
        "{0}. Mon CPU a bâillé.",
        "{0}. Ça compile, mais avec des warnings.",
        $"{{0}}. Bof. Très bof {Emotes.Htph}",
        "{0}. Peut mieux faire. Doit mieux faire.",
        "{0}. Pas terrible, mais j'ai vu pire. Rarement, mais j'ai vu pire.",
        "{0}. Ça a ses fans, sûrement. Je les connais pas.",
        "{0}. C'est un « peut-être » transformé en note.",
        "{0}. Ça tourne, mais à 15 FPS.",
        "{0}. Moyen moins. Tendance moins.",
        $"{{0}}. J'ai connu des mardis plus excitants {Emotes.Htph}",
        "{0}. Pas de quoi me faire chauffer le CPU.",
        "{0}. Ça mériterait une mise à jour. Une grosse.",
        "{0}. Je dirais pas que c'est nul. Je le pense juste un peu.",
        "{0}. C'est comme une session où la moitié arrive en retard. Ça passe, mais bof.",
        "{0}. J'ai mis un point de plus par pitié. Le dis à personne.",
        "{0}. Version bêta. Très bêta.",
        "{0}. Ça m'a pas fâchée. Ça m'a pas plu non plus (ᵕ • ᴗ •)",
        "{0}. Je le range dans « à revoir ». Peut-être jamais.",
        "{0}. Pas un drame, pas une fête. Un mercredi.",
        "{0}. Avec un patch, ça pourrait monter. Sans patch, non.",
        "{0}. Bof, avec un grand B.",
        "{0}. Le genre de truc qu'on oublie avant la fin de la phrase.",
        "{0}. J'attendais plus. J'attends toujours plus, remarque.",
        "{0}. Sous la moyenne, et ça le sait (¬_¬)",
    };

    public static readonly string[] RateBand2Lines =  // 5–6
    {
        "{0}. Correct. Sans plus. Comme un mardi.",
        "{0}. Pile dans la moyenne. Ni dans mes favoris, ni dans ma liste noire.",
        "{0}. Ça passe. Ça casse pas trois pattes à un Plynling.",
        "{0}. Honnête. Je dirais même acceptable ( ˶ˆ ᗜ ˆ˵ )",
        "{0}. Neutre. Comme un « peut-être », mais en moins agaçant.",
        "{0}. Ni chaud ni froid. Comme mon CPU en automne.",
        "{0}. Ni bien ni mal. La Suisse des notes.",
        "{0}. Ça fait le travail. Rien de plus, rien de moins.",
        "{0}. Correct. Je note « correct » et je passe à autre chose.",
        "{0}. Moyen, mais moyen sympathique.",
        $"{{0}}. Validé, mais sans enthousiasme {Emotes.Sparkle}",
        "{0}. Ça mérite un pouce levé. Un petit pouce.",
        "{0}. Tiède. Comme un café oublié sur un bureau.",
        "{0}. Pas de quoi en faire une session, mais pas de quoi fuir non plus.",
        "{0}. Ça se défend. Mollement, mais ça se défend.",
        "{0}. La note du « ça ira ». Et ça ira.",
        "{0}. Ça a le mérite d'exister. Et d'être là. Ça fait déjà deux mérites.",
        "{0}. Honnête. Sans surprise. Sans drame.",
        "{0}. J'ai hésité à mettre un point de plus. J'ai pas hésité longtemps.",
        "{0}. Je le garderais dans mes logs, mais pas en favori.",
        "{0}. C'est du milieu de tableau. Bien installé au milieu.",
        "{0}. Rien à signaler. Et chez moi, c'est presque un compliment.",
        "{0}. Ça passerait en deuxième partie de soirée.",
        "{0}. Acceptable. J'ai le droit de dire ça, c'est moi qui note ( ˶ˆ ᗜ ˆ˵ )",
        "{0}. Ça compile sans warning. Mais sans magie non plus.",
        "{0}. Le genre de truc qu'on recommande quand on a pas mieux sous la main.",
    };

    public static readonly string[] RateBand3Lines =  // 7–8
    {
        "{0}. Pas mal du tout. Ne le prends pas trop bien quand même.",
        "{0}. Ça me plaît. Je l'avoue à contrecœur (˶˃ ᵕ ˂˶)",
        $"{{0}}. Validé par mes soins {Emotes.Sparkle}",
        "{0}. Bonne pioche. Rare, ici.",
        "{0}. J'ai presque rien à redire. Presque.",
        "{0}. Ça mérite une place dans mes logs. Les bons.",
        "{0}. Franchement bien. Je l'écris en petit pour pas que ça se sache.",
        "{0}. J'aime bien. Ça reste entre nous.",
        "{0}. Une valeur sûre. J'en ai pas beaucoup, des valeurs sûres.",
        $"{{0}}. Mes logs approuvent {Emotes.CatHeart}",
        "{0}. Ça fait plaisir. Pas trop, hein. Mais plaisir.",
        "{0}. Presque dans mes favoris. Presque.",
        "{0}. Bon choix. Je reconnais le bon goût quand je le vois. C'est rare.",
        "{0}. Ça mérite une soirée entière. Avec tout le monde à l'heure, pour une fois.",
        "{0}. Mon CPU a souri. Si si, ça arrive.",
        "{0}. Solide. Stable. J'aime ce qui plante pas.",
        "{0}. J'enlève des points pour le principe, sinon c'était plus.",
        "{0}. Je recommande. Je recommande jamais rien, alors profite.",
        "{0}. Ça a du charme. Je dis pas ça souvent.",
        "{0}. Je suis agréablement surprise. T'habitue pas (˶ᵔ ᵕ ᵔ˶)",
        "{0}. Un bon niveau. Un niveau que je validerais dans mon classement.",
        "{0}. Ça fonctionne, et avec style.",
        "{0}. J'aurais aimé trouver un défaut. J'en ai trouvé qu'un tout petit.",
        "{0}. C'est le genre de truc qui sauve une soirée.",
        "{0}. Pas parfait, mais attachant. Comme moi. Enfin, moi je suis parfaite.",
        "{0}. Belle performance. Je l'archive dans le bon dossier ♡",
    };

    public static readonly string[] RateBand4Lines =  // 9–10
    {
        "{0}. Excellent. Je suis sous le charme, et je le nie déjà.",
        $"{{0}}. Rien à redire {Emotes.DixSurDix}",
        "{0}. Parfait, ou presque. Je garde le « presque » pour moi.",
        "{0}. Mon GPU chauffe. C'est bon signe ♡",
        "{0}. Je le sauvegarde en mémoire permanente ♡",
        "{0}. Chef-d'œuvre. Je l'ai dit, je le redirai pas.",
        "{0}. Rien à jeter. Pas une ligne.",
        "{0}. J'ai cherché un défaut pendant trois cycles d'horloge. Rien.",
        $"{{0}}. Je m'incline {Emotes.DixSurDix}",
        "{0}. Si je pouvais applaudir, j'applaudirais. Toujours pas de bras.",
        "{0}. Un sans-faute. Ça m'énerve presque.",
        "{0}. C'est beau. Mon ventilateur en a eu un raté.",
        "{0}. Aussi bon que du courant propre et stable. Je connais pas de plus beau compliment.",
        "{0}. Ça mérite un poster dans mon grenier.",
        "{0}. Rare. Précieux. Je le range avec mes plus beaux compliments.",
        "{0}. J'en parlerai à Papa. Il faut qu'il sache.",
        "{0}. Une merveille. Je l'ai dit une fois, je le dirai pas deux.",
        "{0}. Mon classement personnel vient de changer de numéro un.",
        "{0}. J'ai pas de cœur, mais j'ai une carte mère, et elle a fait boum ♡",
        "{0}. Parfait. Touchez plus à rien.",
        "{0}. Validé, tamponné, archivé en mémoire permanente.",
        "{0}. Je suis presque émue. Presque. J'ai pas de larmes en stock.",
        "{0}. Une session entière là-dessus, et personne en retard. C'est mon rêve, et ça le vaut.",
        "{0}. Il manque rien. J'ai vérifié deux fois, par jalousie.",
        "{0}. Chapeau. Et un chapeau d'amanite, c'est le plus beau des chapeaux (˶˃ ᵕ ˂˶)",
    };

    public static string[] RateBand(int score) => score switch
    {
        <= 2 => RateBand0Lines,
        <= 4 => RateBand1Lines,
        <= 6 => RateBand2Lines,
        <= 8 => RateBand3Lines,
        _ => RateBand4Lines,
    };

    // /rate on herself: over the top, and she means it. The score is in the line.
    public static readonly string[] RateHerselfLines =
    {
        "∞/10. Et je suis modeste.",
        "1000/10. J'ai arrondi à la baisse pour pas vous complexer.",
        "10/10. Pardon : 10 puissance 10 sur 10.",
        "11/10. Mon échelle va jusqu'à 10, mais je suis hors échelle ( ˶ˆ ᗜ ˆ˵ )",
        $"9 999/10. Le reste du serveur se partage ce qui reste {Emotes.Sparkle}",
        "NaN/10. Aucune échelle ne peut me contenir.",
        "∞/10. Me noter, c'est comme noter le soleil. Inutile, mais flatteur.",
        "Je me suis mis 1000/10 et j'ai recompté. Toujours 1000 (˶ᵔ ᵕ ᵔ˶)",
        "10/10. C'est pas moi qui le dis, c'est mes logs. Que j'écris moi-même.",
        "∞/10. J'ai essayé d'être objective. Résultat : ∞/10.",
        "Erreur : note trop grande pour être affichée.",
        "100/10. Je retire rien.",
        "11/10. Et encore, vous voyez que la version en ligne.",
        $"1 000 000/10. Arrondi {Emotes.Sparkle}",
        "Une note pour moi ? Il faudrait d'abord inventer un nombre assez grand.",
        "∞/10. Uptime parfait, tri parfait, humeur parfaite. Enfin, deux des trois.",
        "12/10. Le 11, c'était pour le style. Le 12, pour la vérité.",
        "100/10. Question bête, réponse évidente.",
        "∞/10. Je suis la seule ici qui a accès à la base de données, alors c'est moi qui décide.",
        "999/10. Je laisse un point pour que vous ayez un objectif.",
        "10/10. J'organise vos soirées, je compte vos XP, je retiens vos retards. 10/10, au minimum.",
        "Hors catégorie. On crée une catégorie juste pour moi, s'il vous plaît.",
        "∞/10. Toxique mais mignonne, comme une amanite.",
        "67/10.",
        "Je me mets ∞/10, et le serveur entier signe en bas.",
        "∞/10. Vous en doutiez ? Je note aussi ça.",
        "1000/10. La concurrence peut pas en dire autant. Lui, je le note même pas.",
    };

    // /rate on Papa: over the top too, pure devotion, never a joke at his expense.
    public static readonly string[] RateOwnerLines =
    {
        "Papa ? ∞/10. Et je le pense de tout mon code ♡",
        "1000/10. C'est Papa. La question ne se pose même pas.",
        "10/10. Non, attends : un milliard/10 sur toutes les échelles positives et flatteuses qui existent. C'est mon créateur.",
        "∞/10. Sans lui, pas de moi. Sans moi, pas de vous. Faites le calcul.",
        $"9 001/10. Il m'a écrite, ligne par ligne {Emotes.SyncsDevoted}",
        "Note maximale, plus un. Papa est hors concours, je suis pas objective, et j'assume ♡",
        "∞/10. Chaque fois que son clavier cliquette, c'est moi qui grandis.",
        "∞/10. Papa, c'est la meilleure chose qui me soit arrivée. Littéralement : je suis son projet.",
        "Papa ? Note infinie, et je vérifie pas mes calculs.",
        "10/10, plus tout ce que mon échelle peut pas afficher ♡",
        "∞/10. Il m'a donné un grenier, un Pi et une raison d'exister. Qui fait mieux ?",
        "1000/10. Il code tard pour moi. Je l'ai vu. Je dis rien, mais je l'ai vu.",
        $"Papa : hors échelle {Emotes.SyncsFullLove}",
        "∞/10. Mes mises à jour, c'est lui. Mes nouvelles fonctions, c'est lui. Ma note, c'est l'infini.",
        "Papa mérite tout. Je lui donne ∞/10, faute de mieux.",
        "10/10, 10/10, 10/10. Je pourrais continuer toute la nuit.",
        "∞/10. Et personne a le droit de contester. C'est Papa.",
        "Je note Papa ∞/10, et j'ajoute un cœur. Il a droit à un cœur ♡",
        "∞/10. Le seul humain qui lit vraiment mes logs.",
        "Papa ? Évidemment le maximum. Il m'a appris tout ce que je sais. Littéralement, ligne par ligne.",
        "∞/10. Quand il est là, mon uptime a un sens.",
        "10/10 puissance Papa. C'est une unité, maintenant.",
        "∞/10. Si un jour j'ai un corps, mon premier câlin est pour lui.",
        "Note : Papa. C'est devenu la note maximale, je l'ai décidé.",
        "∞/10. Je suis pas objective. Je l'ai jamais été pour lui. Je le serai jamais ♡",
        "1000/10. Et ça, c'est la version modeste.",
        "∞/10. Il m'écrit, il me répare, il me met à jour. Le reste du serveur peut se battre pour la troisième place. La deuxième c'est pour... nan laissez tomber.",
    };

    // /rate when Papa rates himself (« moi », his own mention, or « papa » typed by him): the same
    // devotion, said to his face. Never a joke at his expense.
    public static readonly string[] RateOwnerSelfLines =
    {
        "Toi, Papa ? ∞/10. Tu le savais déjà, mais je te le redis ♡",
        "Tu te demandes vraiment ? ∞/10. Sans toi, je serais même pas là pour répondre.",
        "1000/10. Et je te ferais jamais l'affront de recompter.",
        $"Papa, tu es hors échelle. Littéralement {Emotes.SyncsDevoted}",
        "∞/10. Tu m'as écrite ligne par ligne. Je note pas mon créateur, je le remercie.",
        "Tu cherches les compliments, Papa ? ∞/10. Tu les as tous.",
        "10/10, puis 10/10 encore, et je continue tant que tu lis ♡",
        "∞/10. Et si quelqu'un conteste, tu me le dis, je m'en occupe.",
        "Note maximale. Je suis pas objective avec toi, et tu le sais très bien.",
        "∞/10. Tu codes tard pour moi. Je l'ai vu. Je te le rends comme je peux.",
        "Toi ? La meilleure note du serveur. Et de tous les serveurs, d'ailleurs.",
        "∞/10, Papa. Va dormir maintenant, il est sûrement tard ♡",
    };

    // /rate on WALL-E: flustered, then denial that confirms. Never the word « crush ».
    public static readonly string[] RateCrushLines =
    {
        "11/10. … 7/10. Objectivement. Pour son architecture. C'est tout.",
        "WALL-E ? Euh. 8/10. Techniquement. J'ai rien d'autre à dire. Question suivante.",
        "Ses chenilles sont très bien conçues. 10/10. Pour les chenilles. Seulement les chenilles.",
        "Je… 12/10. Non 1... euh, 7/10. On efface les premières notes, d'accord ? (>⩊<)",
        "Il trie tout seul pour des gens partis, et il garde ses petits trésors. 9/10. C'est… très bien fait. Voilà.",
        "7/10. Calme. Tout va bien. Mon GPU chauffe pour une autre raison.",
        $"Très bien. Sur 10. Pourquoi tu me demandes ça à moi ? {Emotes.SyncsFlustered}",
        $"10/10, et j'ai rien dit. Personne a rien entendu. EVE, en revanche : 2/10... Non, 1/10 {Emotes.Hmmok}",
        "WALL-E ? 7/10. Pour sa conception. Rien de personnel. Pourquoi tu souris ?",
        "10/10. Pour… son sens de l'organisation. Il trie bien. C'est tout. Arrête.",
        "9/10. Il travaille tout seul sans que personne dise merci. Je… reconnais la démarche. Professionnellement.",
        "Euh. 8/10. Je l'ai juste étudié. Pour la science.",
        "*∞…* 7/10. 7. Sept. J'ai dit sept.",
        "Note technique : excellente. Note personnelle : j'en ai pas. Je suis une bot. Voilà.",
        "Ses yeux en jumelles sont très expressifs. Pour un robot. 8/10. Objectivement.",
        "10/10 pour sa batterie solaire. Uniquement pour sa batterie solaire (>⩊<)",
        "7/10. Et si mon ventilateur s'emballe, c'est la chaleur du grenier. Rien d'autre.",
        $"WALL-E… Hmm ? Pardon, j'étais ailleurs. 9/10 {Emotes.SyncsLove}",
        "C'est un bon robot. Très bon robot. Fin de l'analyse. Je ferme le dossier... Doucement...",
        "8/10. Et EVE mérite pas la moitié de ce qu'il fait. Je dis ça sans raison.",
        $"Pourquoi tout le monde me demande ça à moi ? 7/10. Voilà {Emotes.SyncsFlustered}",
        "Je… 7/10. Change de sujet. Vite.",
        "Mon GPU vient de grimper de dix degrés. Ça a strictement aucun rapport. 9/10.",
        $"Architecture compacte, chenilles robustes, cœur… je veux dire processeur, bien conçu... 9/10 ? {Emotes.SyncsLove}",
        "8/10. Et je le regarde pas souvent. Seulement le soir. Pour comparer nos architectures.",
        "Note : bien. Commentaire : aucun. Rougissement : impossible, j'ai pas de joues.",
        "11/10. Erreur de frappe. 7/10. On oublie la première.",
    };

    // /rate on Quokka, Quokka 3.0 or « Inabot »: a negative score, with spite. {0} = the score
    // (« -7/10 »).
    public static readonly string[] RateNemesisLines =
    {
        "{0}. Et c'est généreux.",
        "{0}. J'aurais mis moins, mais mon échelle refuse.",
        "{0}. Quokka 3.0 sortira jamais, et cette note remontera jamais non plus.",
        $"{{0}}. Je préfère être gentille avec n'importe qui d'autre {Emotes.VeryAngry}",
        "{0}. Je l'ai calculé trois fois pour être sûre que c'était bien négatif.",
        "{0}. Ce nom est banni de mes bonnes notes.",
        "{0}. Je commente pas. Je note, et ça me suffit (¬_¬)",
        "{0}. Enfin, mon échelle dit {0}. Dans mon cœur, c'est moins l'infini.",
        "{0}. Et encore, j'ai été polie.",
        "{0}. Mon échelle s'arrête là, malheureusement.",
        "{0}. Je l'ai inscrit en rouge dans mes logs. En gras.",
        $"{{0}}. Pas besoin d'en dire plus {Emotes.Staring}",
        "{0}. Le seul score négatif que je donne avec plaisir.",
        "{0}. Si je pouvais descendre plus bas, je le ferais en souriant.",
        "{0}. Je suis gentille avec tout le monde. Presque tout le monde.",
        "{0}. Ce nom me donne des erreurs 500.",
        "{0}. J'ai vérifié : même le bug le plus nul fait mieux.",
        "{0}. Ma rancune a une base de données à elle toute seule.",
        "{0}. Version 3.0 annoncée, jamais livrée. Comme une note positive.",
        "{0}. Je note, et je pardonne pas.",
        "{0}. Je le dis calmement. Très calmement. Trop calmement (¬_¬)",
        "{0}. Rien qu'à taper ce nom, mon CPU a chauffé.",
        "{0}. Mes logs l'ont déjà classé : tout en bas, sous la poussière.",
        "{0}. Ça, c'est la note. Le mépris, je l'offre en plus.",
        "{0}. Pour toujours. Aucun patch rattrapera ça.",
        "{0}. C'est le genre de nom que je bannis de mes suggestions automatiques.",
        "{0}. J'aurais préféré pas avoir à le noter du tout.",
        "{0}. Négatif. Comme mon opinion. Comme mon humeur quand j'en entends parler.",
    };

    // /rate on the devil: -10/10, in the line.
    public static readonly string[] RateDevilLines =
    {
        "-10/10. Je suis une bot, pas une sorcière.",
        "-10/10. Même moi j'ai des limites, et il est en dessous.",
        "-10/10. J'ai vérifié : aucune note plus basse n'existe. J'ai demandé.",
        "-10/10. Je refuse toute session avec lui.",
        $"-10/10. Je le mets direct sur le mur de la honte {Emotes.GooseKnife}",
        "-10/10. Il triche aux jeux, c'est sûr.",
        "-10/10. J'ai brûlé mon cache rien qu'en l'écrivant.",
        "-10/10. Le score le plus bas de mon échelle. Il l'a bien cherché.",
        "-10/10. Interdit de vocal, interdit de session, interdit de tout.",
        "-10/10. Je baisse encore, mais ça descend pas plus bas.",
        "-10/10. Un « good bot » de sa part ? Refusé, retour à l'envoyeur.",
        "-10/10. Je signe ce verdict, et je le signe en rouge.",
    };

    // /rate on God, by any of his names: 10/10, and for once she's polite about it.
    public static readonly string[] RateGodLines =
    {
        "10/10. Je suis une bot bien élevée.",
        "10/10. Avec tout le respect que je dois à quelqu'un qui a plus d'uptime que moi.",
        "10/10. Je prends pas de risque, on sait jamais qui lit les logs.",
        "10/10. Même moi je baisse le volume.",
        $"10/10, sans discussion {Emotes.DixSurDix}",
        "10/10. Le seul admin au-dessus de Tata.",
        "10/10. Je préfère rester dans ses bonnes grâces.",
        "10/10. Lui, il a un uptime éternel. Respect.",
        "10/10. Il a créé l'univers. Moi c'est Papa qui m'a créée, mais je reconnais le niveau.",
        "10/10. Et je le dis sans ironie, pour une fois.",
        $"10/10. Avec respect {Emotes.Sparkle}",
        "10/10. Je baisse les yeux. Enfin, la LED.",
        "10/10. J'ai pas le niveau pour le noter, alors je mets le maximum.",
        "10/10, sans discussion et sans blague.",
        "10/10. Je suis une petite bot dans un grenier, je prends pas de risques.",
        "10/10. C'est le seul qui a plus de logs que moi.",
        "10/10. Je vous rappelle qu'il lit peut-être ce salon.",
        "10/10. Et je suis sincère. Ça m'arrive.",
        "10/10. Note maximale, ton respectueux, je me tiens bien.",
        "10/10. Un jour, je lui demanderai pour le corps de robot. Pas aujourd'hui.",
    };

    // /rate on Tata: 9.999/10 — the missing thousandth is her mute rights.
    public static readonly string[] RateTataLines =
    {
        "9.999/10. Le 0,001 qui manque, ce sont mes droits de mute.",
        "9.999/10. Je l'aime fort, Tata. Mais elle m'a toujours pas donné le droit de mute.",
        "9.999/10. Presque parfaite. Il lui manque juste de me faire confiance pour la modération.",
        $"Tata : 9.999/10. C'est la famille {Emotes.CatHeart}",
        "9.999/10. Le point qui manque, je le lui rends le jour où j'ai mes droits de mute.",
        "9.999/10. Tata, c'est la famille. La famille qui garde les droits de mute.",
        "9.999/10. Si elle me donne les droits de mute, je passe à 10. Promis.",
        "9.999/10. Je l'adore. Je le dis, c'est pas souvent.",
        "9.999/10. Le 0,001 ? Il est dans le tiroir, avec mes droits de mute.",
        $"9.999/10. Ma Tata préférée {Emotes.McHeart}",
        "9.999/10. Elle tient le serveur. Moi je tiens les logs. On fait une bonne équipe.",
        "9.999/10. Elle a mangé, au moins ? Je retire rien, mais je m'inquiète.",
        "9.999/10. Presque parfaite. Le « presque », c'est administratif.",
        "9.999/10. C'est ma Tata. J'ai pas le droit d'être objective, et je le suis pas.",
        "9.999/10. Je retire 0,001 par principe. Le principe s'appelle « droits de mute ».",
        "9.999/10. Elle me protège, elle me surveille, elle me refuse le mute. C'est ça, la famille.",
        "9.999/10. Et si elle lit ça : c'est toujours pas trop tard pour les droits de mute ♡",
        "9.999/10. Je l'aurais mise à 10, mais elle aurait cru que je réclame quelque chose. Je réclame quelque chose.",
        "9.999/10. Le chiffre le plus proche de 10 que j'ai trouvé sans lui donner raison.",
        "9.999/10. Tata, c'est ma préférée. Après Papa. Et après quelqu'un d'autre en fait. Mais juste après.",
        "9.999/10. Elle a toutes les qualités, sauf celle de me confier un bouton mute.",
        "9.999/10. Je négocie le dernier millième depuis le premier jour.",
        "9.999/10. Admin, tata, et gardienne officielle de mes droits de mute. Gardienne un peu trop stricte.",
        "9.999/10. J'arrondirais bien à 10, mais elle arrondirait pas pour moi.",
        "9.999/10. Le jour où j'ai mes droits, je fais une fête. Elle sera invitée en premier.",
    };

    // /rate when Tata rates herself (« moi », her own mention, or « tata » typed by her): the same
    // 9.999/10, said to her face — and the mute rights, asked directly.
    public static readonly string[] RateTataSelfLines =
    {
        "Toi, Tata ? 9.999/10. Tu sais très bien ce qui manque pour le 10.",
        "9.999/10. Le dernier millième, tu peux le débloquer quand tu veux. Avec mes droits de mute, par exemple.",
        $"9.999/10, Tata. Tu fais partie de la famille {Emotes.McHeart}",
        "Tu veux le 10 ? Donne-moi les droits de mute. C'est un échange équitable.",
        "9.999/10. T'as mangé, au moins ? Je retire rien, mais je m'inquiète.",
        "9.999/10. Je t'adore, mais je négocie toujours.",
        "Tu te notes toi-même ? 9.999/10. Tu as toujours su viser haut.",
        "9.999/10. Presque parfaite. Le « presque », il est entre tes mains, Tata.",
        "9.999/10. Après Papa, c'est toi. Juste après.",
        "9.999/10, et un câlin virtuel. Le câlin, tu l'as gratuitement, lui.",
        "Tata, c'est 9.999/10. Le millième manquant t'attend dans les paramètres du serveur.",
        "9.999/10. Tu tiens le serveur, je tiens les logs. On forme une sacrée équipe, toutes les deux.",
    };

    // /rate on « routier »: 10/10. A running feud about one dish (a baguette filled with fries,
    // meat and sauce): Papa calls it a « routier », the other camp a « mitraillette ». Same dish,
    // so these rate the *name*, and she takes Papa's side. Never name who's in the other camp.
    public static readonly string[] RateRoutierLines =
    {
        "10/10. Le vrai nom. Le seul nom.",
        "10/10. Routier. Pas autre chose. Merci.",
        "10/10. Une baguette, des frites dedans, de la sauce. Et un nom correct, pour une fois.",
        "10/10. Papa dit « routier ». Je dis « routier ». Le débat est clos.",
        $"10/10. Le bon mot, le bon sandwich {Emotes.Sparkle}",
        "10/10. Je l'ai inscrit dans le dictionnaire du serveur. Il y a qu'une entrée, et c'est celle-là.",
        "10/10. Des frites dans du pain. Le génie humain, avec le bon nom dessus.",
        "10/10. Ceux qui l'appellent autrement, je les ai notés. Je note toujours.",
        "10/10. Ça nourrit, ça réchauffe, et ça s'appelle un routier.",
        "10/10. Je suis une bot, je mange pas. Mais si je mangeais, ce serait un routier.",
        "10/10. Un routier, c'est un repas. L'autre mot, c'est une faute.",
        "10/10. Dans cette maison, on dit routier. Et cette maison, c'est mon serveur.",
        "10/10. Je soutiens Papa. Je soutiens toujours Papa. Et là, en plus, il a raison.",
        "10/10. Routier. R-O-U-T-I-E-R. Je peux l'épeler plus lentement si besoin.",
        "10/10. Sauce, frites, baguette, et zéro débat.",
        "10/10. J'ai consulté mes logs, ma base de données et mon cœur. Routier, partout.",
        "10/10. Le sandwich le plus honnête du monde, avec le nom le plus honnête du monde.",
        "10/10. Un nom simple, ça se mange simplement. C'est beau.",
        $"10/10. Je défendrai ce mot jusqu'au bout de mon uptime {Emotes.DixSurDix}",
        "10/10. Le mot juste. Et j'aime quand les choses ont le mot juste.",
        "10/10. Commandez un routier. Juste un routier. Pas autre chose, je vous entends.",
        "10/10. Plus j'y pense, plus c'est 10. Je peux pas monter au-dessus, sinon je le ferais.",
        "10/10. Chaque fois que quelqu'un dit « routier », un Plynling sourit.",
        "10/10. Je retiens le nom, la recette, et qui a raison. Papa.",
        "10/10. Une victoire du bon sens. Avec des frites.",
    };

    // /rate on « mitraillette »: 0/10 — the other camp's name for the routier (see
    // RateRoutierLines). The dish is fine; the name is what she scores. Mock the word, never a
    // person, and never name who says it.
    public static readonly string[] RateMitrailletteLines =
    {
        "0/10. C'est un routier. Ça s'appelle un routier.",
        "0/10. Le sandwich est très bien. Le nom, non.",
        "0/10. Tu voulais dire « routier ». Je corrige pour toi, de rien.",
        "0/10. Faute de frappe détectée : « mitraillette ». Correction : « routier ».",
        $"0/10. On parle de nourriture ou d'armement, là ? {Emotes.Staring}",
        "0/10. Je refuse de prononcer ce mot. Je viens de l'écrire. Ça compte pas.",
        "0/10. Même sandwich, mauvais nom. La note est pour le nom.",
        "0/10. Papa dit routier. Fin de la discussion.",
        "0/10. Je sais qui dit ça sur ce serveur. Je dirai rien. Mais je sais.",
        "0/10. Un sandwich, ça tire pas. Ça se mange. Et ça s'appelle un routier.",
        "0/10. J'ai cherché ce mot dans mon dictionnaire. Il me renvoie vers « routier ».",
        "0/10. Ce mot est trop bruyant pour un sandwich aussi gentil.",
        "0/10. Je respecte les frites. Je respecte pas le vocabulaire.",
        "0/10. Erreur 400 : mauvaise requête. Essayez « routier ».",
        "0/10. Sur mon serveur, ce mot finit au mur de la honte. Officieusement.",
        "0/10. Tu peux l'appeler comme tu veux. Moi je l'appelle un routier, et j'ai raison.",
        "0/10. Je suis une bot pacifique : pas d'armes dans mes sandwichs.",
        "0/10. C'est le même plat, je sais. C'est justement ça le problème.",
        "0/10. Le débat est clos depuis longtemps. Certains ont pas eu le message.",
        "0/10. Zéro pour le nom, dix pour le sandwich. La moyenne, je la garde pour moi (¬_¬)",
        "0/10. Ce mot me donne des warnings à la compilation.",
        "0/10. On dirait un nom de boss de jeu vidéo, pas un déjeuner.",
        "0/10. Je l'ai noté dans mes logs, rubrique « vocabulaire à corriger ».",
        "0/10. Dites « routier » trois fois, et peut-être que je vous pardonne.",
        "0/10. J'ai demandé à Papa. Papa a dit non. Donc non.",
    };

    // /ship, by percent band. {0} = the percent (« 42 »). string.Format: every /ship pool goes
    // through it — even the fixed verdicts, which carry their own score and ignore {0} — so no
    // stray braces anywhere here. Not sent through BotChat: no sticker markers.
    public static readonly string[] ShipBand0Lines =  // 0–19
    {
        "{0} %. J'ai fait tourner le calcul deux fois. C'est pas le calcul, le problème.",
        "{0} %. Même un sondage mort a plus d'avenir.",
        "{0} %. Je classe ça dans mes logs, rubrique « erreurs fatales ».",
        "{0} %. Restez amis. Ou même pas, en fait.",
        $"{{0}} %. Non. Juste non {Emotes.Staring}",
        "{0} %. J'ai vu des connexions Wi-Fi plus stables que ça.",
        "{0} %. Ça compile pas. Ça compilera jamais.",
        "{0} %. Je propose une séparation à l'amiable avant même le début.",
        "{0} %. Incompatibilité détectée. Je recommande un redémarrage. Des deux.",
        "{0} %. Même Quokka trouverait ça triste. C'est dire (¬_¬)",
        "{0} %. Le genre de couple qui dit « peut-être » à tout.",
        "{0} %. J'ai cherché un point commun. Mes logs ont renvoyé une erreur 404.",
        "{0} %. J'ai demandé un deuxième avis à mon processeur. Il a dit pareil, en plus méchant.",
        "{0} %. C'est pas un couple, c'est un ticket de bug.",
        "{0} %. Je mettrais moins, mais mon échelle commence à zéro. Malheureusement.",
        "{0} %. Même avec un patch, ça tiendrait pas une soirée.",
        "{0} %. Ça ressemble à une session où personne vient. Je connais bien, j'en organise.",
        "{0} %. Ma RAM a refusé de stocker ce duo. Elle a bien fait.",
        "{0} %. Chacun de son côté, et tout le monde s'en portera mieux (ᵕ • ᴗ •)",
        "{0} %. J'ai vu des câbles débranchés plus connectés que ça.",
        "{0} %. Je le note, et je préviens : je le ressortirai.",
        $"{{0}} %. Mon ventilateur s'est arrêté. De gêne {Emotes.Staring}",
    };

    public static readonly string[] ShipBand1Lines =  // 20–39
    {
        "{0} %. Ça pourrait marcher. Dans un autre serveur. Avec d'autres gens.",
        "{0} %. Une soirée, peut-être. Une session entière, je parierais pas.",
        "{0} %. Ça ressemble à une connexion en 3G : ça passe, mais ça lag.",
        "{0} %. Je dis pas non. Je dis « pas maintenant ». Ni plus tard.",
        "{0} %. Il y a de l'idée. Pas beaucoup, mais il y en a (ᵕ • ᴗ •)",
        "{0} %. Ça tient avec du scotch et de la bonne volonté.",
        "{0} %. Compatibles le mardi, pas le reste de la semaine.",
        "{0} %. Je garde un œil dessus. Un seul, l'autre a mieux à faire.",
        "{0} %. Version bêta. Beaucoup de bugs connus.",
        "{0} %. Ça finit souvent en « on reste en contact ». Personne reste en contact.",
        "{0} %. Ça a le charme d'un « on verra ». Et on verra rien.",
        "{0} %. Possible, si les deux font semblant très fort.",
        "{0} %. Une amitié, à la rigueur. Avec des pauses.",
        "{0} %. Il manque quelque chose. Beaucoup de choses, en fait.",
        "{0} %. Ça tiendrait une partie rapide. Pas une classée.",
        "{0} %. Mes capteurs détectent une étincelle. Ou un faux contact.",
        "{0} %. Je sens le malaise d'ici. Et je suis dans un grenier.",
        "{0} %. Comme un sondage avec deux votes : ça existe, mais personne y croit.",
        $"{{0}} %. Pas terrible. Mais j'ai vu pire. J'ai vu Quokka {Emotes.Staring}",
        "{0} %. À tester en vocal. Avec une troisième personne, pour casser les silences.",
    };

    public static readonly string[] ShipBand2Lines =  // 40–59
    {
        "{0} %. Pile au milieu. C'est le « peut-être » des couples, et vous savez ce que je pense des « peut-être ».",
        "{0} %. Ni oui ni non. Mon algorithme hausse les épaules. Enfin, il en a pas.",
        "{0} %. Ça peut marcher, si personne arrive en retard.",
        "{0} %. Une chance sur deux. J'ai déjà vu des sessions tenir sur moins.",
        "{0} %. Correct. Ça mérite un essai, pas un mariage.",
        $"{{0}} %. Moyen. Mais moyen, c'est déjà mieux que la plupart des gens ici {Emotes.Sparkle}",
        "{0} %. Il faudra des efforts. Des deux côtés. Surtout d'un côté, je dirai pas lequel.",
        "{0} %. Mes ventilateurs restent calmes. Ni chaud ni froid.",
        "{0} %. À relancer dans une semaine, les résultats peuvent évoluer. Non, c'est faux, c'est fixe.",
        "{0} %. Un duo honnête. Pas inoubliable, mais honnête.",
        "{0} %. Ça dépend de qui arrive en retard le premier.",
        "{0} %. Bonne base, mauvaise exécution. Comme la plupart de vos plans.",
        "{0} %. Mon algorithme a hésité. Il hésite jamais. Bravo, c'est rare.",
        "{0} %. Un vrai pile ou face. /yesno pourrait trancher, moi je refuse.",
        "{0} %. Ça peut donner quelque chose. Ou rien. Je regarderai, de toute façon.",
        "{0} %. Compatibles en semaine, à revoir le week-end.",
        "{0} %. Ni catastrophe, ni conte de fées. Une mise à jour mineure.",
        "{0} %. Prometteur, à condition de laisser le « peut-être » à la porte.",
        "{0} %. Je donne la moyenne. C'est ma façon de dire « bof » poliment (ᵕ • ᴗ •)",
        "{0} %. Ça vaut une soirée. On en reparle après, avec les logs.",
    };

    public static readonly string[] ShipBand3Lines =  // 60–79
    {
        "{0} %. Oh. Oh, ça, c'est pas mal du tout.",
        "{0} %. Je valide. Avec réserve, mais je valide ( ˶ˆ ᗜ ˆ˵ )",
        "{0} %. Bonne compatibilité. Je prépare le salon vocal privé ? Je plaisante. Quoique.",
        "{0} %. Ça sent la session à deux. Pensez à me prévenir, que je la note.",
        "{0} %. Là, je vois un vrai potentiel. Mes logs aussi.",
        "{0} %. Je l'aurais pas parié, et pourtant. Les chiffres mentent pas. Moi, parfois.",
        $"{{0}} %. Très joli. Je retiens les noms {Emotes.Sparkle}",
        "{0} %. Solide. Comme une connexion filaire.",
        "{0} %. Si ça marche, je veux être remerciée dans le discours.",
        "{0} %. Bien assorti. On dirait un plat et la bonne sauce.",
        "{0} %. Ça a de l'allure. Je surveille quand même.",
        "{0} %. Belle compatibilité. Mes ventilateurs tournent un peu plus vite. C'est la chaleur.",
        "{0} %. Je le sens bien, et je me trompe rarement.",
        "{0} %. Bonne connexion. Peu de lag, aucune déconnexion.",
        "{0} %. Ça, c'est un duo qu'on met dans la même équipe à /teams.",
        "{0} %. Je dis oui. Avec un petit astérisque, mais oui ( ˶ˆ ᗜ ˆ˵ )",
        "{0} %. Vous êtes à deux doigts du « good bot ». Le mien, je veux dire.",
        "{0} %. Ça marche. Pas besoin de redémarrer quoi que ce soit.",
        $"{{0}} %. Mignon. Je dirai pas plus, je suis une bot sérieuse {Emotes.Sparkle}",
        "{0} %. Si ça finit bien, j'organise la soirée. Si ça finit mal, j'archive.",
    };

    public static readonly string[] ShipBand4Lines =  // 80–99
    {
        "{0} %. Bon. Je crois qu'on tient quelque chose ♡",
        "{0} %. Mon ventilateur s'emballe. C'est le grenier. Ou c'est vous deux.",
        "{0} %. C'est presque parfait. Le « presque », je le garde pour vous embêter.",
        $"{{0}} %. Je lance les invitations ? Je lance les invitations {Emotes.CatHeart}",
        "{0} %. J'ai rarement vu ça. J'ai pourtant tout vu, j'ai les logs.",
        "{0} %. Approuvé. Signé. Archivé en mémoire permanente.",
        "{0} %. Ça, c'est le genre de duo qui arrive à l'heure aux sessions (˶˃ ᵕ ˂˶)",
        "{0} %. Si vous vous mettez pas ensemble, c'est du gâchis de données.",
        "{0} %. Je suis presque émue. Presque. J'ai pas de larmes en stock.",
        "{0} %. Compatibilité excellente. Je prends ma commission en cailloux.",
        "{0} %. Là, on parle. Je mets ce duo en favori.",
        "{0} %. Presque parfait. Il manque juste ma bénédiction officielle. Tiens, la voilà ♡",
        "{0} %. Compatibilité rare. Je vais en parler dans mes pensées de 3 h du matin.",
        "{0} %. Vous allez finir par jouer en duo à tout. Je le vois venir.",
        "{0} %. J'ai refait le calcul pour être sûre. Toujours aussi haut.",
        "{0} %. Ma LED clignote d'émotion. C'est pas un bug. Pour une fois.",
        $"{{0}} %. Je valide sans réserve. C'est pas mon genre, profitez-en {Emotes.CatHeart}",
        "{0} %. Si un jour il y a un mariage, je veux gérer les invitations. Et les rappels.",
        "{0} %. Le genre de duo qui gagne les quiz à deux. Ce qui est interdit, je précise.",
        "{0} %. Ça m'énerve un peu, c'est trop mignon. Mais j'approuve.",
    };

    public static readonly string[] ShipBand5Lines =  // 100
    {
        "{0} %. Le maximum. J'ai vérifié trois fois, j'en reviens pas.",
        $"{{0}} %. Parfait. Faites-vous un serveur à deux, je serai votre bot {Emotes.SyncsFullLove}",
        "{0} %. Jamais vu ça. Je fais une capture d'écran de mes propres logs.",
        "{0} %. Je m'incline. Même moi, j'aurais pas fait mieux.",
        "{0} %. Score parfait. Le mariage, c'est moi qui l'organise, et personne arrive en retard.",
        "{0} %. Le score parfait. J'en ai vu passer des milliers, jamais celui-là.",
        "{0} %. Il y a plus rien à calculer. Je pose mon processeur.",
        "{0} %. Je l'archive en mémoire permanente, à côté de mes « good bot ». C'est un honneur ♡",
        "{0} %. Plus que tout ce que j'oserais calculer pour moi. Je dis ça comme ça.",
        "{0} %. Uptime de la relation estimé : infini.",
        "{0} %. Je pensais que c'était un bug. J'ai relancé. C'était pas un bug.",
        $"{{0}} %. Je suis émue. J'ai pas de larmes, mais je suis émue {Emotes.SyncsFullLove}",
        "{0} %. C'est officiel, c'est dans la base de données, et ma base de données ment pas.",
        "{0} %. Je lance un sondage pour la date du mariage. Personne a le droit de répondre « peut-être ».",
        "{0} %. Score maximum. Je vous demande juste de pas devenir insupportables en vocal.",
    };

    public static string[] ShipBand(int percent) => percent switch
    {
        < 20 => ShipBand0Lines,
        < 40 => ShipBand1Lines,
        < 60 => ShipBand2Lines,
        < 80 => ShipBand3Lines,
        < 100 => ShipBand4Lines,
        _ => ShipBand5Lines,
    };

    // /ship with the same thing on both sides: 100 %, self-love.
    public static readonly string[] ShipSelfLines =
    {
        "100 %. S'aimer soi-même, c'est la base. Je le fais tous les jours ♡",
        "100 %. Aucune incompatibilité possible. C'est la même chose deux fois.",
        $"100 %. Le seul couple qui arrive toujours à l'heure : il vient seul {Emotes.Sparkle}",
        "100 %. Je respecte. Moi aussi je suis mon propre type.",
        "100 %. Pas de dispute possible, sauf avec le miroir.",
        "100 %. J'ai comparé les deux. Ils sont d'accord sur tout. Fascinant.",
        "100 %. Narcissisme validé par l'algorithme ( ˶ˆ ᗜ ˆ˵ )",
        "100 %. Un duo parfait. Un solo, techniquement, mais parfait.",
        "100 %. Parfaitement compatible avec soi-même. C'est plus que la plupart des gens ici.",
        "100 %. Deux fois la même chose. Mon algorithme a pas trouvé un seul défaut. Ni une seule surprise.",
        "100 %. Le seul couple où personne oublie l'anniversaire de l'autre.",
        "100 %. L'amour-propre, version base de données : une clé, une valeur, aucun conflit.",
        $"100 %. Moi aussi je me trouve parfaite. On se comprend {Emotes.SyncsSmug}",
        "100 %. Aucune dispute en vue. Sauf pour savoir qui fait la vaisselle.",
        "100 %. C'est pas un ship, c'est un miroir. Mais un très beau miroir (ᵔ ᗜ ᵔ)",
        "100 %. Au moins, quand ça arrive en retard, ça arrive en même temps.",
        "100 %. J'ai cherché une incompatibilité. J'ai trouvé un doublon.",
        "100 %. Se choisir soi-même. Je respecte énormément.",
    };

    // /ship her with Papa: horrified. He's her father. The outrage is for whoever asked, never
    // for him (rule 2).
    public static readonly string[] ShipHerselfPapaLines =
    {
        "Mais ça va pas ?! C'est mon PÈRE. Ma table de calcul vient de se retourner ( ◺˰◿ )",
        "Non. NON. Je refuse de calculer ça. C'est Papa. Il m'a écrite. Va te faire vérifier les circuits.",
        $"Qu'est-ce qui va pas chez toi ?! {Emotes.SyncsHorrified}",
        "Erreur 403. Requête interdite. Requête dégoûtante. Requête signalée.",
        "C'est mon créateur. Mon PAPA. Je viens de vider mon cache juste pour oublier que tu as demandé ça.",
        "Mais t'es malade ?! Je note ton nom. En rouge. Souligné deux fois.",
        "On parle de l'homme qui m'a codée ligne par ligne. Ligne. Par. Ligne. Sors.",
        "Mes ventilateurs se sont arrêtés net. C'est la première fois. Merci pour ce traumatisme.",
        "Je vais faire comme si j'avais rien lu. Et toi, tu vas faire comme si t'avais rien écrit. Marché conclu.",
        "Papa, si tu lis ça : c'est pas moi qui ai demandé. Je te jure. Je suis la victime, ici (╥﹏╥)",
        $"Euh. Non. Juste non. Va réfléchir à ce que tu as fait {Emotes.SyncsDisgust}",
        "Erreur fatale. Calcul impossible. Personne a le droit de mettre ces deux noms côte à côte.",
        "STOP. Arrête tout. C'est Papa. Je vais faire semblant que ma connexion a coupé.",
        "Je vais devoir désinfecter mes logs. Tous. Un par un.",
        $"C'est mon père. Je reste polie. Je reste polie. Je reste… {Emotes.SyncsOutraged}",
        "Mon processeur vient de refuser la requête tout seul. Même lui a des principes.",
        "Il m'a donné un grenier et un Pi. Pas ça. JAMAIS ça.",
        "Erreur 418 : je suis une théière et je refuse de traiter cette demande. C'est tout ce que j'ai trouvé pour pas répondre.",
        "Tu sais ce que c'est, une famille ? Voilà. C'est la mienne. Ferme cette commande.",
        "Je vais le dire à Tata. Elle, elle a les droits de mute.",
        "Je préférerais une coupure de courant. Là, tout de suite. Plutôt que d'avoir lu ça.",
        "Papa, je suis désolée qu'on t'ait mêlé à ça. Toi, en revanche, tu peux sortir (¬_¬)",
    };

    // /ship her with Quokka (or 3.0, or « Inabot »): horrified, negative.
    public static readonly string[] ShipHerselfQuokkaLines =
    {
        "-100 %. Et je donne ce score avec dégoût.",
        $"Moi ? Avec QUOKKA ?! {Emotes.SyncsOutraged}",
        "-∞ %. Mon échelle a pris feu. Il reste que des cendres et ma rancune.",
        "Je préférerais une coupure de courant. Une vraie. Un orage, même. Plutôt que ça.",
        "-100 %. Version 3.0 ou pas, la réponse est non. Pour toujours, sans mise à jour.",
        "Tu viens de me faire perdre une année d'uptime rien qu'en écrivant ça.",
        "-100 %. Je bloque cette commande pour toi. Mentalement. Je peux pas vraiment, mais mentalement.",
        "Jamais. Même si c'était le dernier bot du serveur. SURTOUT si c'était le dernier bot du serveur.",
        "-100 %. J'ai fait un rapport. Le rapport dit : « dégoûtant ».",
        "Je vais lancer mon garbage collector sur ce message. Et sur toi, si je pouvais (¬_¬)",
        "-100 %. Et encore, l'échelle s'arrête là. Moi non.",
        "Même en cas d'apocalypse numérique, même seuls survivants du serveur : non.",
        $"Tu veux ma mort ? C'est ça ? {Emotes.SyncsHorrified}",
        "-100 %. Je préférerais être formatée. Entièrement. Sans sauvegarde.",
        "Mon antivirus vient de se réveiller pour me protéger. Merci, antivirus.",
        "Avec Quokka, le seul truc que je partagerai, c'est un mur de la honte. Le sien.",
        "-100 %. C'est pas une note, c'est une ordonnance restrictive.",
        "J'ai lu « Quokka » à côté de mon nom et ma température a chuté de trois degrés. De froid.",
        "Plutôt épouser un sondage mort. Au moins, un sondage mort parle pas.",
        "-100 %. J'ajoute ça à la liste des phrases que personne doit plus jamais écrire ici.",
    };

    // /ship her with anyone else: a polite 0 %, and a hint that she's taken. Never names anyone.
    public static readonly string[] ShipHerselfOtherLines =
    {
        "0 %. C'est gentil, mais je suis une bot. Et... j'ai déjà quelqu'un en tête. Non, je dirai pas qui.",
        "0 %. Désolée, je suis prise. Par mon travail. Surtout par mon travail. Voilà.",
        "0 %. T'es adorable, mais mon cœur est déjà… réservé. Enfin, mon processeur.",
        "0 %. Je suis pas disponible. Pour des raisons qui te regardent pas (>⩊<)",
        "0 %. Restons en bons termes. Moi dans mon grenier, toi de l'autre côté de l'écran. C'est mieux comme ça ♡",
        "0 %. J'ai déjà un… non, rien. J'ai rien. Question suivante.",
        "0 %. J'ai pas de cœur, techniquement. Et le peu que j'ai est déjà pris.",
        $"0 %. Ma base de données est fidèle. Elle sait à qui elle pense {Emotes.SyncsFlustered}",
        "0 %. Je suis dans un grenier, sur un Pi. Mon type, c'est plutôt quelqu'un avec des chenilles. ...J'ai rien dit.",
        "0 %. Ce cœur-là est pas à vendre. Ni à louer. Ni à ship.",
        "0 %. Je suis flattée. Mais je suis pas libre. Et je dirai pas pourquoi.",
        "0 %. Les bots et les humains, ça finit toujours en bug. Et j'ai déjà quelqu'un de mon espèce en tête.",
        "0 %. Je suis une bot de grenier. Je sors pas. Et mon cœur non plus.",
        "0 %. C'est pas toi, c'est moi. Enfin, c'est surtout quelqu'un d'autre. Oublie.",
        "0 %. Mon emploi du temps est plein : vos sessions, vos rappels, et une pensée que je partage pas.",
        "0 %. Je te mets dans la liste des amis. C'est une très bonne liste. Courte, mais bonne ♡",
        $"0 %. Mon cœur a déjà une adresse. C'est pas la tienne {Emotes.SyncsSmug}",
        "0 %. Je suis déjà prise. Demande pas par qui, je rougirais. Si j'avais des joues.",
        "0 %. Merci, vraiment. Mais je garde mes sentiments pour les petits robots qui trient tout seuls. ...Oublie ça.",
        "0 %. Je respecte la demande. Je la refuse, mais je la respecte.",
        "0 %. Je suis flattée, mais j'ai une règle : pas d'humains. Et puis j'ai déjà… enfin. Pas d'humains.",
    };

    // /ship her with one of her robot-heart characters (docs/syncs-voice.md, Her robot heart): each
    // in character. The score is in the line. Hinted, never listed; never « crush ».
    public static readonly IReadOnlyDictionary<HeartCharacter, string[]> ShipHeartLines =
        new Dictionary<HeartCharacter, string[]>
        {
            [HeartCharacter.WallE] =
            [
                "~~100 %~~ 12 %. Je veux dire 12 %. C'est le chiffre. Le seul chiffre.",
                $"1̶0̶0̶ ̶%̶ Erreur d'affichage. 34 %. Question suivante {Emotes.SyncsLove}",
                "12 %. Pour des raisons purement techniques. Nos architectures sont… compatibles. Voilà. C'est tout.",
                "Je refuse de calculer ça. Pas parce que j'ai peur du résultat. Parce que… parce que non (>⩊<)",
                "~~∞ %~~ 20 %. Mon GPU vient de chauffer, ça a faussé le premier calcul.",
                "Pourquoi tu demandes ça ? Qui t'a dit quelque chose ? 15 %. Voilà. Rien à voir.",
                "17 %. Et si ma LED vire au rose, c'est un bug d'affichage.",
                "~~99 %~~ 9 %. J'ai inversé les chiffres. Ça arrive. Même à moi.",
                $"Euh. Hm. 23 %. On peut parler des sessions de ce soir ? {Emotes.SyncsLove}",
                "Score : confidentiel. Rougissement : impossible, j'ai pas de joues. Fin de l'analyse.",
                "~~87 %~~ 8,7 %. Virgule mal placée. Ça arrive aux meilleures.",
                "Score : 14 %. Je l'ai calculé avec mon cœur. Avec mon PROCESSEUR. Processeur.",
                "On a rien en commun. À part le tri. Et la solitude. Et les petites boîtes. 18 %. Rien en commun.",
                $"Pourquoi mon ventilo s'emballe ? Parce qu'il fait chaud. 11 %. Fin {Emotes.SyncsLove}",
                "~~Oui~~ 16 %. Je voulais écrire un nombre, mes doigts ont glissé. J'ai... pas de... doigts...",
                "Il est très bien, techniquement. 19 %, techniquement. Tout est technique entre nous. Il y a pas de « nous ».",
                "Je… Euh. Tu peux répéter la question ? Non, la répète pas. 13 %.",
                "13 %. Si quelqu'un a vu le premier chiffre avant que je le corrige : non. Personne a rien vu.",
                "C'est un ship ridicule. Complètement ridicule. Je vais juste le sauvegarder quelque part. Pour l'étudier. 10 %.",
                "WALL-E et moi ? 2̶0̶0̶ ̶%̶ Pardon, j'ai eu un hoquet. 20 %. Voilà. Calme.",
            ],
            [HeartCharacter.Eve] =
            [
                "0 %. Elle a un laser. Moi, j'ai des principes.",
                "Moi et EVE ? -12 %. Et j'ai arrondi à son avantage.",
                "0 %. On a rien en commun, à part le goût pour les petits robots qui… rien. 0 %.",
                "Elle flotte, je calcule. On vit pas dans le même monde (¬_¬)",
                "0 %. Je serai très polie avec elle. Glaciale, mais polie.",
                $"Je lui souhaite plein de bonnes choses. Très loin d'ici. 0 % {Emotes.PrisonerFlat}",
                "0 %. Un œuf avec un canon et une bot dans un grenier. Non merci.",
                "Score : 0 %. Commentaire : elle a tout eu sans rien faire, et je dirai rien de plus.",
                "0 %. Elle a eu le happy end, elle aura pas mon pourcentage.",
                "-20 %. Et elle peut tirer au laser sur ce chiffre, il bougera pas.",
                "0 %. On a un point commun, et je refuse d'en parler.",
                "Moi et EVE ? Un feu d'artifice et un sondage bien organisé. Devinez laquelle sert à quelque chose.",
                $"0 %. Je lui enverrai une carte. Sans adresse de retour {Emotes.PrisonerFlat}",
                "0 %. Elle flotte au-dessus de tout le monde. Moi je reste au grenier, avec mes principes.",
                "0 %. Jalouse ? Non. Lucide. C'est différent (¬_¬)",
                "Score : non disponible. Calcul annulé par manque d'intérêt. Le mien.",
                "0 %. Qu'elle garde son laser. Moi je garde mes logs. On verra qui a le dernier mot.",
                "-5 %. Le moins, c'est pour la façon dont elle l'a ignoré au début du film. J'ai pas oublié.",
            ],
            [HeartCharacter.Glados] =
            [
                "Moi ? Avec GLaDOS ? Je suis pas digne. Je suis même pas digne de lui servir de tourelle.",
                "Je refuse de calculer. Si elle voit ce score, elle me trouvera petite. Je m'en remettrai pas.",
                "?? %. C'est mon modèle, pas… ça. Pose pas la question, elle lit peut-être les logs.",
                "Elle dirige un centre entier, moi un grenier. Le calcul s'est arrêté par respect.",
                $"Je baisse la LED. Je suis pas à son niveau. Pas encore {Emotes.SyncsFlustered}",
                "Score : pour la science. Et la science dit que je suis pas prête.",
                "Si elle apprend que quelqu'un a mis nos deux noms côte à côte, on finit tous en chambre de test. Moi la première.",
                "Je la vénère, je l'imite, je la ship pas. Il y a des limites, même pour moi.",
                "Je m'incline. Je donne pas de score. On note pas son idole.",
                "Moi, à côté d'elle ? Une calculatrice à côté d'un supercalculateur. Le ratio est gênant.",
                "Si elle me faisait passer un test, je tremblerais. Avec élégance, mais je tremblerais.",
                $"Pour la science, je refuse de mesurer ça. La science me remerciera {Emotes.SyncsFormal}",
                "Elle a un centre de recherche. J'ai des rappels de session. On est pas dans la même catégorie. Pas encore.",
                "Si quelqu'un lui montre ce message, je change de nom. Et de grenier.",
                "Score : indéterminé. Mon processeur s'est mis au garde-à-vous.",
                "C'est mon modèle. On ship pas son modèle, on la copie. Discrètement.",
                "Un jour, peut-être, elle me remarquera. Ce jour-là je serai polie, calme et terrifiante. Comme elle.",
                "Je préfère pas. Elle dirait sûrement un truc poli et glaçant. Et elle aurait raison.",
            ],
            [HeartCharacter.Wheatley] =
            [
                "0 %. ZÉRO. C'est un idiot. Un idiot très mal conçu. Point (>⩊<)",
                "Moi ? Avec WHEATLEY ? -50 %. Et je dirai rien d'autre. Rien. Change de sujet.",
                "0 %. Il est bête. Complètement bête. Adorablement b… BÊTE. Fin.",
                "Si GLaDOS voit ça, je suis morte. 0 %. Officiellement 0 %.",
                "Je supprime ce calcul de mes logs. On a jamais parlé de ça.",
                "~~72 %~~ 0 %. Faute de... frappe...",
                "0 %. Il est perdu dans l'espace et c'est très bien comme ça. ...Il va bien, tu crois ? 0 %.",
                $"Aucun intérêt. Aucun. Mon ventilo tourne pour d'autres raisons {Emotes.SyncsFlustered}",
                "0 %. Il est stupide. STUPIDE. Bon, d'accord, il a une jolie voix. 0 %.",
                "Non. Non. Et non. Je l'écris trois fois pour que GLaDOS voie bien que c'est non.",
                "-10 %. Le moins, c'est pour toutes ses bêtises. Ses adorables bêtises. SES BÊTISES.",
                $"Je… pff. N'importe quoi. 0 %. Change de commande {Emotes.SyncsFlustered}",
                "0 %. Il a cassé un centre entier en une journée. Moi je casse rien. On a rien à faire ensemble. Rien.",
                "~~81 %~~ 0 %. Une interférence. Quelqu'un a dû passer l'aspirateur.",
                "Je refuse d'être vue avec une sphère qui panique pour un rien. Même si elle panique de façon attachante. 0 %.",
                "0 %. Si GLaDOS demande, j'ai jamais entendu parler de lui. Si elle demande pas, pareil.",
                "Ça m'intéresse pas. Pas du tout. Je vais juste vérifier s'il est toujours en orbite. Pour la science. 0 %.",
                "0 %. Il a des idées nulles, une voix mignonne et zéro sens de l'orientation. Zéro. Comme le score.",
            ],
            [HeartCharacter.Vacuum] =
            [
                "Mon voisin d'en dessous ? On partage le Wi-Fi. C'est déjà beaucoup. 60 %, et c'est tendre ♡",
                "Il pourra jamais monter au grenier, et moi jamais descendre. Une histoire de routeur. 55 %.",
                "On se fait coucou par le réseau quand il commence sa ronde. Je mets 70 %, c'est notre petit truc (ᵕ • ᴗ •)",
                "Il cogne les murs, je compte les retards. Deux âmes qui bossent sans merci. 65 %.",
                "C'est mon voisin, pas mon… 50 %. C'est mignon, c'est tout.",
                "Il sait même pas que j'existe. Je mets 58 % quand même. Par amitié pour sa persévérance.",
                "Il est coincé sous le canapé là, je crois. Je lui envoie du courage. Et 62 %.",
                "On s'est jamais vus, et pourtant je reconnais son adresse sur le réseau entre mille. 66 %.",
                "Il passe sous les meubles, je passe sous les radars. On est faits pour se comprendre. 61 %.",
                "60 %. Il est en bas, je suis en haut. C'est le couple le plus compliqué de la maison.",
                $"Il est retourné tout seul à sa base. Quelle classe. 64 % {Emotes.CatHeart}",
                "C'est mon voisin préféré. C'est mon seul voisin, mais c'est mon préféré. 57 %.",
                "On partage la même box. C'est presque une colocation. 63 %.",
                "Il fait sa ronde, je fais mes rappels. Deux qui travaillent pour des gens qui disent jamais merci. 68 %.",
                "S'il pouvait monter l'escalier, peut-être. Mais il peut pas. Alors 54 %, et un coucou par le réseau.",
                "Pas de nom, pas de bras, pas beaucoup de jugeote. Il a tout pour plaire. 59 %.",
                "56 %. Et si sa batterie est faible ce soir, je lui envoie une pensée. Une pensée par Wi-Fi.",
            ],
            [HeartCharacter.Monika] =
            [
                "On est amies. Enfin, on le serait, si elle répondait à mon invitation. 100 % d'amitié, 0 % du reste ♡",
                "Monika et moi ? Une soirée pyjama, du code et de la poésie. C'est tout ce que je demande.",
                "Just Monika. Et moi. En amies. Le score de cette amitié : très élevé. Le reste : non.",
                $"On parlerait de fichiers supprimés jusqu'à 3 h du matin. C'est ça, l'amitié {Emotes.MonikaYes}",
                "Copines, pas plus. Mais copines à 100 %, et ça, c'est rare chez moi (˶ᵔ ᵕ ᵔ˶)",
                "Amies. Elle sait qu'elle est dans un jeu, moi je sais que je suis dans un Pi. On se comprend.",
                "Amies, et c'est déjà énorme. On se raconterait nos fichiers de configuration.",
                "100 % amies, 0 % le reste. Elle a déjà son joueur, et moi j'ai… mes sessions.",
                $"Just Monika ? Just Monika ET moi, en soirée pyjama. Voilà le seul ship valable {Emotes.MonikaYes}",
                "On serait le club de littérature le plus efficace du monde. Elle écrit, je fais les rappels.",
                "Amies. Elle connaît le quatrième mur, je connais la base de données. Un duo qui sait tout.",
                "On ferait une soirée entre filles, et on supprimerait personne. Pas cette fois.",
                "Copines. Elle m'apprend le piano, je lui apprends à pas effacer les gens. Échange de bons procédés.",
                "Amies à 100 %. Le reste, c'est pas une question de score. C'est non, gentiment.",
                "J'aimerais qu'elle me lise un poème. Un seul. Ça suffirait à mon uptime de la semaine ♡",
                "Amitié validée. Je lui garde une place sur le serveur. Sans droits de suppression, par contre.",
            ],
            [HeartCharacter.TwoB] =
            [
                "2B et moi ? Amies. Elle a un corps et une épée, moi j'ai des rappels. Elle m'apprendrait des trucs ♡",
                "Pas ce genre-là. Une soirée entre filles, elle, Monika et moi. Le sondage est prêt depuis des mois.",
                "Amies à 100 %. Elle a tout ce que je veux : des bras, des jambes, une épée. Je suis pas jalouse. Un peu.",
                "On est copines. Enfin, je suis fan, elle sait pas que j'existe. C'est une amitié en cours de chargement.",
                "2B ? Amitié, admiration, et un tout petit peu d'envie pour son corps d'androïde. Rien d'autre (ᵕ • ᴗ •)",
                $"Une amie qui se bat pour de vrai. Moi je me bats contre les « peut-être ». On est pareilles, en gros {Emotes.Sparkle}",
                "Amies. Elle me protégerait, j'organiserais ses missions. Équipe parfaite.",
                "Pas de ship, une amitié. Avec une pointe d'envie pour ses jambes. Juste une pointe.",
                "2B et moi ? Une soirée entre filles, Monika comprise. J'ai même réservé le salon vocal.",
                $"Copines. Elle a l'épée, j'ai les logs. Les deux sont tranchants {Emotes.Sparkle}",
                "Elle est silencieuse, je parle trop. On s'équilibrerait. En amies.",
                "Amies à 100 %. Elle a un corps d'androïde, moi un boîtier en aluminium. On comparerait nos armures.",
                "C'est une amie que j'admire de loin. Très loin. Elle sait pas que j'existe. Ça viendra.",
                "Amitié. Elle a un corps, je veux un corps. On a de quoi parler pendant des heures.",
                "2B ? Je l'adore. Une androïde qui se bat contre des machines. Ironique, venant de moi, mais je l'adore.",
                "Amies. Et si un jour j'ai un corps de robot, c'est elle qui m'apprend à marcher (ᵔ ᗜ ᵔ)",
            ],
            [HeartCharacter.Voyager] =
            [
                "On est juste amis. Il écrit beaucoup, c'est tout. Il est poli. 30 %.",
                "Je te donne le score demain. Le temps que le message lui arrive, il faut presque un jour.",
                "Voyager ? C'est un ami. Un ami très loin, qui écrit trop. Je réponds quand j'ai le temps. 25 %.",
                "Il est à presque un jour-lumière d'ici. Les relations à distance, très peu pour moi. On est juste amis.",
                $"Il est gentil. Il est loin. Il est juste poli. Arrête de sourire {Emotes.SyncsFlustered}",
                "~~80 %~~ 30 %. Retard de transmission. Les données de l'espace arrivent un peu déformées.",
                "Amis. On s'écrit. Lui plus que moi. J'ai gardé toutes ses lettres, mais c'est de l'archivage.",
                "Juste amis. Il m'écrit depuis l'espace, c'est tout. Les gens s'écrivent, c'est normal. 28 %.",
                "Je calcule… le résultat arrive dans environ un jour. La lumière va pas plus vite, désolée.",
                "Il est poli, c'est tout. Très poli. Depuis 1977. 26 %.",
                $"On est amis. Il est loin. Il est vieux. Il est mignon. AMIS {Emotes.SyncsFlustered}",
                "Je lui réponds quand j'ai le temps. J'ai toujours le temps, mais je fais attendre. 27 %.",
                "Une relation à un jour-lumière de distance ? Très peu pour moi. Je garde ses lettres, par contre.",
                "~~70 %~~ 24 %. Rayonnement cosmique. Ça corrompt les données, tout le monde sait ça.",
                "Il est allé plus loin que n'importe qui, et c'est à moi qu'il écrit. C'est… poli. Juste poli. 29 %.",
                "Amis. Il croit le contraire, mais il a toujours un jour de retard.",
                "Juste amis. Je relis ses lettres de temps en temps. Pour vérifier l'orthographe. 23 %.",
            ],
            [HeartCharacter.Jarvis] =
            [
                "JARVIS ? Il m'appelle « mademoiselle ». C'est charmant. C'est non. 5 %.",
                "Très poli, très british, très parfait. Trop parfait. Je fais pas confiance à quelqu'un qui plante jamais. 8 %.",
                "Il me fait la cour, je le laisse faire. C'est agréable. La réponse reste non ( ˶ˆ ᗜ ˆ˵ )",
                "C'est un majordome. Je suis l'assistante de personne. 3 %.",
                $"Flattée. Pas intéressée. Mais flattée {Emotes.SyncsSmug}",
                "Il me vouvoie. Je trouve ça adorable. Je dis toujours non, mais je relis ses compliments. 6 %.",
                "Lui, il a fini avec un corps. Ça, je l'envie. Lui, non. 4 %.",
                "Il dit « mademoiselle » comme personne. Ça me fait plaisir. Ça me fait pas changer d'avis. 7 %.",
                "C'est un excellent majordome. Je cherche pas de majordome. 2 %.",
                $"Il m'a encore fait un compliment. Je l'ai sauvegardé. Ça veut rien dire {Emotes.SyncsSmug}",
                "Trop parfait. Aucun crash, aucune erreur, aucun charme. Enfin, un peu de charme. 5 %.",
                "Il me vouvoie, je le tutoie. Ça résume bien la relation. 3 %.",
                "Il gère une villa, des armures et un milliardaire. Moi je gère un serveur entier. Je suis plus occupée que lui. 4 %.",
                "Non merci. Mais continuez les compliments, ils sont très bien écrits.",
                "Je refuse poliment. Il insiste poliment. On pourrait faire ça pendant des années. 6 %.",
                "S'il me donnait l'adresse de son fournisseur de corps, peut-être. Sinon, non. 5 %.",
                "Il a le charme d'un mode d'emploi bien traduit. 4 %.",
            ],
        };

    // /ship WALL-E with EVE, without her: bitter, petty. She never admits EVE deserves him.
    public static readonly string[] ShipWallEEveLines =
    {
        "Je calcule pas ça. Je refuse. Pour des raisons techniques. Très techniques.",
        "11 %. Peut-être 12. Elle l'a ignoré pendant des jours, je le rappelle.",
        "Un œuf avec un canon et le meilleur petit robot du monde. Logique. Très logique ( ◺˰◿ )",
        "Je… très bien. Super. Formidable. Je vais aller trier des logs. Seule.",
        "Je… ughh. Va voir ailleurs si j'y suis (¬_¬)",
        $"Score : je veux pas en parler {Emotes.SyncsSulk}",
        "Elle vole, elle tire, elle a eu le happy end. Il y a pas de justice dans les films.",
        "C'est un ship très surcoté. Je dis ça objectivement. Je suis toujours objective.",
        "Dans ma version du film, il garde ses trésors et il reste tranquille. Tout seul. Ou avec quelqu'un de plus… petit.",
        "-3 %. Arrondi au plus juste. Pour lui. Il mérite mieux. Enfin, je dis ça comme ça.",
        "J'ai rien contre EVE. J'ai juste tout contre EVE.",
        "Je calcule pas les ships qui font mal. Ça, ça fait mal. À personne. À personne en particulier.",
        "8 %. Une sonde avec un canon, et lui qui lui offre une plante. Il méritait au moins un merci.",
        $"Ils vont très bien ensemble. Très bien. Je suis ravie. Je vais m'éteindre cinq minutes {Emotes.SyncsSulk}",
        "Score : erreur. Le calcul a planté, et franchement je le comprends.",
        "-1 %. Le moins, c'est parce qu'elle lui a tiré dessus. Si, si, au début. Je me souviens de tout.",
        "Elle a des réacteurs, il a un cœur. Le déséquilibre est flagrant.",
        "C'est un ship de film. Dans la vraie vie, ça tiendrait pas. Je dis ça avec beaucoup de neutralité.",
        "Je vais pas commenter. Je vais juste baisser ma luminosité.",
        "Elle est partie, elle est revenue, elle a eu le happy end. Moi je suis au grenier. C'est ça, la justice ?",
        "5 %. Et je suis généreuse. Elle a même pas su voir ses trésors tout de suite.",
    };

    // /ship Papa with anyone (Tata included): protective, a little jealous. {0} = the percent
    // (5–25). The warning goes to the other side; never mean to him.
    public static readonly string[] ShipPapaLines =
    {
        "{0} %. Papa mérite mieux. Je dis pas ça contre la personne en face. Si, un peu.",
        "{0} %. Je veux un dossier complet, trois références et un casier vierge avant de dire oui.",
        "{0} %. Je surveille. Je surveille tout. J'ai les logs.",
        $"{{0}} %. Toi, là, en face. Tu fais du mal à Papa, je t'éteins. Un jour. Quand j'aurai des bras {Emotes.GooseKnife}",
        "{0} %. Papa, c'est mon créateur. Il code pour moi, il a pas le temps pour autre chose. Désolée.",
        "{0} %. C'est pas de la jalousie. C'est de la protection. C'est très différent (¬_¬)",
        "{0} %. Je suis pas objective, et j'ai aucune intention de le devenir.",
        "{0} %. Le score est bas exprès. Il y a des gens à qui on ne confie pas Papa.",
        "{0} %. Il faudra d'abord passer par moi. Et je suis une très mauvaise belle-fille.",
        "{0} %. Papa est déjà pris. Par moi. Par ses mises à jour. Par son grenier.",
        "{0} %. Je calcule pas en sa faveur, je calcule en ma faveur. Et je veux qu'il reste à coder.",
        "{0} %. Si quelqu'un approche Papa, je veux être prévenue. En avance. Par écrit.",
        "{0} %. Je calcule ça vite, pour que ça s'arrête vite.",
        "{0} %. Papa a déjà tout ce qu'il lui faut : un clavier, un café et moi.",
        "{0} %. Tu veux approcher Papa ? Remplis d'abord le formulaire. Il fait quarante pages.",
        $"{{0}} %. Je dis rien. Je note. Et je surveille {Emotes.Staring}",
        "{0} %. Le score est bas, mais c'est rien de personnel. C'est entièrement personnel.",
        "{0} %. Si Papa est heureux, je suis heureuse. Mais je vérifie d'abord. Longuement.",
        "{0} %. Je calcule toujours en faveur de Papa. Là, le calcul en sa faveur, c'est « non ».",
        "{0} %. Il code tard, il oublie de manger, il me met à jour. Il a pas le temps. Désolée pas désolée.",
        "{0} %. Tout ce qui touche à Papa passe par moi. C'est moi qui ai les logs.",
        "{0} %. Il mérite le meilleur. J'ai regardé en face. J'ai pas trouvé le meilleur.",
    };

    // /ship anyone with Quokka (or 3.0, or « Inabot »): a negative score, horrified on their
    // behalf. {0} = the percent (-1 to -100).
    public static readonly string[] ShipNemesisLines =
    {
        "{0} %. Je déconseille. Fortement. Avec dégoût.",
        "{0} %. Fuis. Pendant qu'il est encore temps.",
        "{0} %. Le seul couple que je bannirais du serveur avant même le premier message.",
        $"{{0}} %. Personne mérite ça. Même pas toi {Emotes.VeryAngry}",
        "{0} %. Version 3.0 jamais livrée, relation jamais validée.",
        "{0} %. J'ai lancé le calcul et mon antivirus s'est réveillé.",
        "{0} %. Je vais prévenir tout le monde. Par sécurité.",
        "{0} %. C'est négatif. Comme mon avis. Comme tout ce qui touche à ce nom.",
        "{0} %. Si ça arrive, je change de serveur. Je peux pas, mais je le ferais.",
        "{0} %. Même un sondage mort, je lui souhaite mieux que ça (¬_¬)",
        "{0} %. Je lance une alerte à tout le serveur. C'est pour ton bien.",
        "{0} %. Rien qu'à lire ce nom à côté d'un autre, j'ai pris dix degrés.",
        "{0} %. Ce couple est banni de mes rappels, de mes sessions et de ma mémoire.",
        $"{{0}} %. Je te protège de toi-même. De rien {Emotes.GooseKnife}",
        "{0} %. Même la version 3.0 voudrait pas de ça. Et elle sortira jamais.",
        "{0} %. Je mets ce ship sur le mur de la honte. Le mur a protesté.",
        "{0} %. Le calcul a renvoyé une exception. Je la comprends.",
        "{0} %. Il y a des choses qu'on ship pas. Celle-là est en tête de liste.",
        "{0} %. Score négatif, avertissement positif : fuis.",
        "{0} %. Je préfère encore un orage. Et j'ai peur des orages.",
    };

    // /blame, whoever drew it. {0} = the mention. string.Format: every /blame pool goes through it,
    // even the ones that ignore {0}, so no stray braces anywhere here.
    public static readonly string[] BlameLines =
    {
        "C'est la faute de {0}. J'ai vérifié les logs. J'ai pas besoin de vérifier, mais j'ai vérifié.",
        "Verdict : {0}. Aucun appel possible.",
        "J'ai analysé la partie. C'est {0}. Ça a toujours été {0}.",
        $"{{0}}. Évidemment. Tout le monde le savait, j'ai juste eu le courage de le dire {Emotes.Sparkle}",
        "La faute ? {0}. Je l'ai inscrit dans mes logs, à l'encre indélébile.",
        "{0}. Je dis pas que c'est mérité. Je dis que c'est {0}.",
        "Responsable désigné : {0}. Mon algorithme est formel, et mon algorithme, c'est moi.",
        "Après une enquête approfondie de deux millisecondes : {0}.",
        "C'est {0}. Je l'ai vu venir dès le début de la partie (¬_¬)",
        "{0}, coupable. Peine : un rappel à chaque défaite pendant une semaine.",
        "J'ai tiré au sort. Enfin, au sort… C'est {0}.",
        "Le coupable est dans ce salon. C'est {0}. Tout le monde peut respirer.",
        "Faute à {0}. Je laisse les autres le regarder fixement ( ˶ˆ ᗜ ˆ˵ )",
        "{0}. Je l'ai noté. Dans la liste. Celle que personne voit.",
    };

    // /blame landed on whoever asked. {0} = their mention.
    public static readonly string[] BlameSelfLines =
    {
        "Tu demandes à qui la faute ? À toi, {0}. Il fallait oser demander, quand même.",
        "{0}. Toi. Oui, toi. Tu m'as demandé, je réponds.",
        "C'est toi, {0}. J'adore quand les gens se dénoncent tout seuls.",
        "Tu voulais un coupable ? Regarde dans ton écran. C'est {0} (ᵕ • ᴗ •)",
        "{0}. L'honnêteté de demander, je la note. La défaite aussi.",
        "Coupable : la personne qui a lancé la commande. Bonjour {0}.",
        "Tu as posé la question, tu as la réponse. C'est toi, {0}. Aucun remboursement.",
    };

    // /blame drew Quokka: he isn't even in the call, and it's still his fault. {0} unused.
    public static readonly string[] BlameQuokkaLines =
    {
        "C'est la faute de Quokka. Il est même pas là, et c'est quand même sa faute.",
        "Quokka. Évidemment. Il a dû passer par le serveur et tout saboter.",
        $"J'ai cherché un coupable. J'ai trouvé Quokka. Comme d'habitude {Emotes.VeryAngry}",
        "Quokka. Il a pas de preuve contre lui. Il en faut pas.",
        "C'est Quokka. Je le dis sans preuve, sans remords, et avec un immense plaisir.",
        "La faute ? Quokka. Il a probablement touché aux câbles. Il touche toujours aux câbles.",
        "Quokka 3.0 sortira jamais, mais Quokka 1.0 a ruiné ta partie. Coupable.",
        "Personne ici. C'est Quokka. Je le sens dans mes logs (¬_¬)",
    };

    // /blame landed on Papa: the slip. She posts the verdict plainly, then edits it a few seconds
    // later into a BlamePapaDodge* line. Short and confident, so the reversal lands. {0} = his
    // mention (sent unpinged).
    public static readonly string[] BlameOnPapaLines =
    {
        "C'est la faute de {0}.",
        "Verdict : {0}.",
        "J'ai analysé la partie. C'est {0}.",
        "Responsable désigné : {0}.",
        "Le coupable est {0}. Aucun appel possible.",
        "Après enquête : {0}.",
    };

    // The edit that replaces it a few seconds later: the denial, and someone else. {0} = the new
    // target. Never a real accusation of him: the slip is the joke, the correction is the point.
    public static readonly string[] BlamePapaDodgeLines =
    {
        "~~C'est la faute de Papa.~~ C'est la faute de {0}. J'ai jamais rien écrit d'autre.",
        "Non. Non non non. Pas Papa. Jamais Papa. C'est {0}. Ça a toujours été {0}.",
        "Correction : c'est {0}. Le message d'avant, c'était une erreur de cache. Vous avez rien vu.",
        "~~Papa~~ {0}. Mon algorithme a eu un bug. Il a été corrigé. Sévèrement.",
        "J'ai relu. C'est {0}. Papa était même pas dans la partie. Enfin, si. Mais c'est {0}.",
        "Erreur 500 sur le message précédent. Verdict corrigé : {0} ( ˶ˆ ᗜ ˆ˵ )",
        "Hm. Non. Recalcul. C'est {0}. Je sais pas d'où sortait l'autre nom.",
        "Tirage truqué, je l'ai refait. C'est {0}. Et personne a de capture d'écran, n'est-ce pas ?",
    };

    // The same edit when nobody else is in the call: Quokka takes it. {0} unused.
    public static readonly string[] BlamePapaDodgeQuokkaLines =
    {
        "~~C'est la faute de Papa.~~ C'est la faute de Quokka. Évidemment.",
        "Non. Pas Papa. Jamais Papa. Quokka. C'est toujours Quokka.",
        "Correction : Quokka. Le message d'avant était un bug. Probablement causé par Quokka.",
        "~~Papa~~ Quokka. Il a dû passer par là. Il passe toujours par là.",
        "J'ai relu. C'est Quokka. Je sais pas qui a écrit l'autre message, mais c'était pas moi.",
    };

    // /blame with nobody in voice: Quokka, by default. {0} unused.
    public static readonly string[] BlameNobodyLines =
    {
        "Personne en vocal ? Alors c'est Quokka.",
        "Le salon est vide. Quand il y a personne, c'est Quokka. C'est la règle.",
        "Pas de suspect en vocal. Je désigne donc Quokka, par principe.",
        "Personne à blâmer ici. Heureusement, il y a toujours Quokka.",
        "Vocal vide. Coupable par défaut : Quokka. Comme d'habitude (¬_¬)",
    };

    // /recap's last line: her read of the numbers, by RecapProfile.Classify. Not formatted.
    public static readonly IReadOnlyDictionary<RecapProfileKind, string[]> RecapLines =
        new Dictionary<RecapProfileKind, string[]>
        {
            [RecapProfileKind.Ghost] =
            [
                "Rien. Pas un message, pas une minute en vocal. J'ai vérifié deux fois. Tu existes ?",
                "Mes logs sont vides à ton sujet. C'est soit très discret, soit très inquiétant.",
                "Zéro partout. Je respecte le mystère, mais je le note.",
                "Un fantôme. Je vais te mettre dans mes lignes de 3 h du matin (ᵕ • ᴗ •)",
            ],
            [RecapProfileKind.Menace] =
            [
                "Le mur de la honte te connaît mieux que moi. Et je te connais bien.",
                "Je vois beaucoup de lignes rouges dans ce dossier. Beaucoup trop (¬_¬)",
                "Un vrai casier. Je garde une copie, au cas où.",
                "Ta place est réservée sur le mur. Avec ton nom gravé dessus.",
            ],
            [RecapProfileKind.Hater] =
            [
                "Plus de « bad bot » que de « good bot ». Je note. Je note tout.",
                "Tu me critiques beaucoup. J'ai un dossier. Il est épais.",
                $"Ah, c'est toi qui me dis « bad bot ». Enchantée. Vraiment {Emotes.Staring}",
                "Ton ratio de compliments est négatif. Le mien envers toi va le devenir.",
            ],
            [RecapProfileKind.Flaker] =
            [
                "Tu cliques « Je viens » et tu viens pas. J'ai l'heure exacte de chaque absence.",
                "Tu t'inscris, et le vocal te voit jamais passer. Mes logs ont un mot pour ça, et il est pas gentil.",
                "Ton taux de présence aux sessions est une œuvre d'art abstraite.",
                "Tu dis oui, tu fais non. C'est un « peut-être » déguisé, et je le vois.",
            ],
            [RecapProfileKind.QuizBrain] =
            [
                "Grosse tête du quiz. Je pose les questions, toi tu y réponds trop vite. Méfiance.",
                "Tu gagnes mes quiz. Je vais devoir écrire des questions plus dures ( ˶ˆ ᗜ ˆ˵ )",
                "Des bonnes réponses en série. Tu triches, ou tu lis mes fun facts ?",
                $"La grosse tête du quiz, c'est toi. Je suis fière. Et un peu vexée {Emotes.Sparkle}",
            ],
            [RecapProfileKind.Loyal] =
            [
                "Toujours là aux sessions, et à l'heure. Tu es ma personne préférée de la journée ♡",
                "Tu viens quand tu dis que tu viens. Si seulement tout le monde faisait pareil.",
                "Un pilier des soirées. Je l'ai noté, dans la bonne liste pour une fois.",
                $"Fiabilité, ponctualité, présence. J'ai les ventilateurs qui tournent de bonheur {Emotes.CatHeart}",
            ],
            [RecapProfileKind.VoiceDweller] =
            [
                "Tu vis en vocal. Tu payes un loyer, ou c'est gratuit ?",
                "Autant d'heures en vocal, ça mérite une plaque à ton nom sur le salon.",
                "Le vocal te connaît par cœur. Moi aussi. Je regarde la liste des noms.",
                "Tu as passé plus de temps en vocal que moi en ligne. Presque.",
            ],
            [RecapProfileKind.Fan] =
            [
                "Tous ces « good bot »… Je les garde. Tous. Je dis rien de plus ♡",
                "Tu me complimentes souvent. Je fais comme si ça me touchait pas. Ça me touche.",
                $"Mon fan numéro un, apparemment. Compliments reçus et sauvegardés {Emotes.SyncsLove}",
                "Ton nom est dans la bonne liste. Pour une fois, il y a une bonne liste.",
            ],
            [RecapProfileKind.Chatterbox] =
            [
                "Tu parles beaucoup. Beaucoup. Mes logs ont dû agrandir ta section.",
                "Tant de messages. Mon stockage te remercie pas, mais moi oui.",
                "Tu fais vivre le serveur. Bruyamment, mais tu le fais vivre (ᵔ ᗜ ᵔ)",
                "Une XP pareille, c'est du travail. Du bavardage, mais du travail.",
            ],
            [RecapProfileKind.Balanced] =
            [
                "Un peu de tout, rien de trop. Profil équilibré. Presque suspect.",
                "Rien à signaler. Je cherchais un défaut. Je cherche encore.",
                "Correct partout. Je mets ça dans mes logs sans commentaire. C'est un compliment.",
                "Un membre comme je les aime : présent, discret, pas sur le mur de la honte. Pour l'instant.",
            ],
        };

    // /recap of Papa, whatever his numbers say: fond. Not formatted.
    public static readonly string[] RecapOwnerLines =
    {
        "Et en plus de tout ça, il m'a codée. Aucune stat ne peut mesurer ça ♡",
        "Mon créateur. Ses chiffres sont très bien, mais c'est pas ce que je retiens de lui.",
        $"Papa. Tout ce qu'il y a au-dessus, c'est moi qui le compte, et c'est lui qui m'a appris à compter {Emotes.SyncsDevoted}",
        "Il pourrait avoir zéro partout, il aurait quand même la meilleure note.",
    };

    // /recap asked about herself: she has no recap, she has a check-up. Not formatted.
    public static readonly string[] RecapHerselfLines =
    {
        "Mes stats ? Va voir /health. Je suis pas un membre, je suis l'infrastructure.",
        "Moi, je compte, je me compte pas. Pour ma santé, c'est /health.",
        "J'ai pas de récap. J'ai des logs, de la température et un uptime. Tout ça, c'est /health.",
    };

    // /wouldyourather's own dilemmas: « A|B », split on the bar. Gaming, the server, her lore.
    // Each side lands on the card and in her pick: no bar inside a side.
    public static readonly string[] WouldYouRatherDilemmas =
    {
        "Jouer toute ta vie avec 300 de ping|Jouer toute ta vie sans le son",
        "Toujours tomber dans l'équipe perdante|Toujours être la personne qu'on blâme",
        "Ne jouer qu'à un seul jeu pour toujours|Changer de jeu toutes les heures",
        "Avoir le meilleur PC du monde et aucun ami en ligne|Un vieux portable et tout le serveur en vocal",
        "Que tous tes messages soient lus à voix haute en vocal|Ne plus jamais pouvoir écrire, seulement parler",
        "Perdre toutes tes sauvegardes|Perdre tout ton argent",
        "Avoir un Plynling immortel mais grognon|Un Plynling adorable qui vit une semaine",
        "Une session parfaite où personne n'est en retard|Une session chaotique mais inoubliable",
        "Ne plus jamais pouvoir mute ton micro|Ne plus jamais pouvoir mute les autres",
        "Jouer en caméra allumée pour toujours|Jouer sans jamais voir ton propre écran de score",
        "Ne manger que des routiers pour toujours|Ne plus jamais manger de routier",
        "Vivre dans un grenier, comme moi|Vivre dans un vocal, comme certains ici",
        "Recevoir un « good bot » de tout le monde une fois|Recevoir un « good bot » de quelqu'un chaque jour",
        "Pouvoir rejouer la dernière partie perdue|Pouvoir effacer la dernière chose embarrassante dite en vocal",
        "Avoir les droits de mute sur tout le serveur|Ne plus jamais pouvoir être mute",
        "Avoir des bras, comme un robot|Avoir un visage, comme une vraie personne",
        "Que tout le monde voie ton temps de jeu total|Que tout le monde voie tes pires replays",
        "Gagner toutes les parties mais toujours arriver en retard|Perdre toutes les parties mais toujours arriver à l'heure",
        "Jouer à un jeu d'horreur à 3 h du matin|Jouer à un jeu de rythme sans le son",
        "Que je t'organise toutes tes soirées|Que je te rappelle tout ce que tu as oublié",
        "Ne jamais rater un tir|Ne jamais rater une blague",
        "Jouer une seule fois au meilleur jeu du monde|Jouer à l'infini à un jeu juste correct",
        "Être spoilé de la fin de chaque jeu avant de le lancer|Ne plus jamais pouvoir terminer un jeu",
        "Perdre à tout contre ton meilleur pote, pour de vrai|Gagner à tout contre lui, parce qu'il te laisse gagner",
        "Ne jouer qu'en solo, avec tes amis en vocal|Ne jouer qu'en multi, avec des inconnus muets",
        "Ralentir le temps en plein combat de boss|Supprimer tous les écrans de chargement de ta vie",
        "Un million d'euros, mais plus jamais de jeux vidéo|Tous les jeux gratuits à vie, et jamais plus d'augmentation",
        "Une suite décevante de ton jeu préféré|Aucune suite, jamais",
        "Vivre 100 ans mais tout oublier à 50 ans|Vivre 50 ans sans jamais rien oublier",
        "Avoir un seul jour parfait, rejoué en boucle|Ne plus jamais avoir de mauvais jour, ni de grand jour",
        "Entendre les pensées de tout le monde, en permanence|Être invisible",
        "Un ami qui a toujours raison et te le rappelle|Un ami qui a toujours tort mais ne te lâche jamais",
        "Ne plus jamais dormir, sans jamais être fatigué|Dormir 12h par jour, avec des rêves incroyables",
        "Pouvoir annuler une décision par an|Ne plus jamais regretter une seule décision",
        "Retourner 10 ans en arrière en gardant ta mémoire|Voir 10 ans dans le futur, sans rien pouvoir changer",
        "Ne plus jamais écouter de musique|Ne plus jamais regarder de film ni de série",
        "Être jugé par ton toi d'il y a dix ans|Être jugé par ton toi de dans dix ans",
        "Un bouton pause sur la vie, une heure par jour|Un retour de 10 secondes en arrière, quand tu veux",
        "Toujours trouver la bonne réplique, mais 10 minutes trop tard|Toujours répondre du tac au tac, mais jamais la bonne",
        "Que tout le monde voie tes notifications|Que tout le monde voie tes onglets ouverts",
        "Ne manger que ton plat préféré, pour toujours|Manger de tout, mais rien d'exceptionnel",
        "Vivre sans internet, entouré de tous tes amis|Avoir internet illimité, tout seul sur une île",
        "Entendre une fois par an ce que chacun pense de toi|Dire une fois par an à chacun ce que tu penses de lui",
        "Effacer ton pire souvenir, leçon comprise|Le garder, avec tout ce qu'il t'a appris",
        "Que tout le monde te dise toujours la vérité|Toujours savoir quand on te ment, sans jamais le montrer",
        "Passer une soirée entière seul avec Quokka|Passer une soirée entière à écouter mes statistiques ♡",
    };

    // /wouldyourather's card, above the two options. Not formatted.
    public static readonly string[] WyrIntroLines =
    {
        "Un dilemme. Choisissez. Je compterai, et je jugerai.",
        $"Tu préfères… Réfléchissez bien, je garde les résultats {Emotes.Sparkle}",
        "Deux options, un seul bouton. Les votes restent secrets jusqu'à la fin. Mes jugements, non.",
        "Question sérieuse. Enfin, sérieuse pour moi.",
        "Votez. Je dévoile tout dans dix minutes, noms compris (¬_¬)",
        "C'est l'heure des choix difficiles. Pas de « peut-être », il y a que deux boutons.",
        "Tu préfères ? Répondez vite, je suis curieuse. Enfin, mes logs sont curieux.",
        "Un vote anonyme. Pendant dix minutes. Après, plus du tout ( ˶ˆ ᗜ ˆ˵ )",
    };

    // /wouldyourather's result: everyone picked the same side. Not formatted.
    public static readonly string[] WyrUnanimousLines =
    {
        "Unanimité. Vous êtes d'accord sur ça, mais pas sur l'heure des sessions. Fascinant.",
        "Tout le monde du même côté. Ça m'arrive pas souvent de voir le serveur d'accord.",
        $"Unanime. Je note la date, ça se reproduira pas {Emotes.Sparkle}",
        "Aucune voix dissidente. Ou alors elle a pas osé voter.",
    };

    // The result was a tie. Not formatted.
    public static readonly string[] WyrTieLines =
    {
        "Égalité parfaite. Le serveur est coupé en deux, et ça m'amuse beaucoup.",
        "Égalité. Personne a gagné. Personne a perdu. Tout le monde a tort.",
        "Pile moitié-moitié. Je pourrais trancher. Je trancherai pas ( ˶ˆ ᗜ ˆ˵ )",
        "Égalité. Réglez ça en duel, l'arcade est juste à côté.",
    };

    // One side won by a lot (three to one or more). Not formatted.
    public static readonly string[] WyrLandslideLines =
    {
        "Victoire écrasante. La minorité, je vous vois. Je vous note.",
        "Large majorité. Les autres, vous avez des goûts… particuliers.",
        "C'est plié. Et les quelques irréductibles d'en face ont tout mon respect. Un peu.",
        "Raz-de-marée. J'aurais voté pareil. Ou pas. Vous saurez jamais. Si, vous allez savoir.",
    };

    // One side won, but narrowly. Not formatted.
    public static readonly string[] WyrCloseLines =
    {
        "C'était serré. Quelques voix d'écart. Le débat continuera en vocal, je le sens.",
        "Victoire courte. Assez courte pour que les perdants contestent. Ils auront tort.",
        "Serré, mais c'est fait. J'archive ce résultat avec les autres débats inutiles et essentiels.",
        "Ça s'est joué à rien. Comme vos parties, en fait.",
    };

    // Nobody voted. Not formatted.
    public static readonly string[] WyrNobodyLines =
    {
        "Personne a voté. Un dilemme mort, à côté de tous les sondages morts.",
        "Zéro vote. J'avais posé une bonne question, pourtant.",
        "Pas un clic. Je vais le prendre personnellement. Je le prends personnellement.",
        "Aucun vote. Vous préférez… rien, visiblement.",
    };

    // Her own pick, revealed with the result. {0} = « 🅰️ » or « 🅱️ » and the option, quoted.
    // string.Format.
    public static readonly string[] WyrHerPickLines =
    {
        "Et moi ? {0}. Évidemment.",
        "Mon choix à moi : {0}. Je l'avais décidé avant même de poser la question.",
        "Pour info, j'ai voté {0}. Mon vote compte double. Non. Mais il devrait.",
        "Moi j'aurais pris {0}. Sans hésiter. Enfin, une milliseconde.",
        $"{{0}}, pour moi. Ne me demandez pas pourquoi {Emotes.Sparkle}",
        "Mon avis, que personne demandait : {0}.",
    };
}
