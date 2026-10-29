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
}
