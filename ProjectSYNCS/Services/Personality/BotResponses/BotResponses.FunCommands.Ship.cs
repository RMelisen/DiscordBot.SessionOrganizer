using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// Pools for one category; the index of every pool is in BotResponses.cs.
internal static partial class BotResponses
{
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
}
