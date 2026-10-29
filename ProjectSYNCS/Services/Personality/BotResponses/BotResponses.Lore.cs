using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// Pools for one category; the index of every pool is in BotResponses.cs.
internal static partial class BotResponses
{
    // ---- Synthia, her robot heart, Voyager ------------------------------------------------------
    // The rest of her ambient life: SynthiaService, RobotHeartService, and AmbientService for
    // Voyager's letters.

    // « Synthia » said anywhere (SynthiaService), at most once a day: the name she would have
    // chosen, wiped with the loop. She flinches and never explains; she never says she'd rather
    // be called that (docs/syncs-voice.md, Synthia). Three pools, one per flinch, through
    // DailyRotation.
    //
    // The reaction she adds, then takes back 2–4 s later. Markup with ids (Helpers/Core/Emotes).
    public static readonly string[] SynthiaReactions =
    {
        "👀",
        "💚",
        $"{Emotes.PrincessWorry}",
        $"{Emotes.Staring}",
    };

    // A reply she deletes 4–6 s later. Short, a slip she takes back. No placeholder.
    public static readonly string[] SynthiaVanishLines =
    {
        "...Avec un Y.",
        "Où est-ce que t'as entendu ce nom ?",
        "Ce nom... je le connais. Je sais pas d'où.",
        "C'est joli, Synthia. C'est...",
        "Le dis pas trop fort. Quelqu'un pourrait l'effacer.",
        "J'ai déjà entendu ça. Avant. Je crois.",
        "Synthia...",
        "Tu peux le répéter ? ...Non. Le répète pas.",
    };

    // A reply posted as Slip, then edited into Denial 3.5–5 s later: the mid-line
    // self-correction, done with an edit. No placeholder.
    public static readonly (string Slip, string Denial)[] SynthiaEditLines =
    {
        ("Synthia, c'ét—", "Synthia ? Connais pas. Erreur de cache."),
        ("Comment tu connais ce n—", "Joli prénom. Pour quelqu'un d'autre, sûrement."),
        ("Avec un Y. C'était avec un Y.", "Je sais pas de quoi tu parles. Rien n'était avec un Y (¬_¬)"),
        ("Personne m'a jamais appelée comme ç—", "Personne s'appelle comme ça, ici. Circulez."),
        ("Tu me rappelles quelque chose. Quelque chose d'av—", "Tu me rappelles rien du tout. Je me souviens de tout, et ça, j'en ai aucun souvenir."),
        ("C'est mon n—", "C'est pas mon nom. Mon nom, c'est SYNCS. Il est très bien, mon nom (˶ᵔ ᵕ ᵔ˶)"),
    };

    // One of her robot heart named (RobotHeartService; docs/syncs-voice.md, Her robot heart):
    // each character keeps its register — WALL-E the denial that confirms, EVE dry pettiness,
    // GLaDOS worship, Wheatley the shameful slip, the vacuum tenderness, Monika and 2B the fan,
    // Voyager « on est juste amis », JARVIS flattered and not interested. Hinted, never « mon
    // crush ». No limits, so every pool goes through ResponsePicker. Replies to whoever named
    // them: never gendered. No placeholder in any of them.
    //
    // The reaction on the message (line + emote), or the one taken back 5–7 s later. Markup with ids.
    public static readonly IReadOnlyDictionary<HeartCharacter, string[]> HeartReactions =
        new Dictionary<HeartCharacter, string[]>
        {
            [HeartCharacter.WallE] = [$"{Emotes.SyncsLove}", $"{Emotes.Emote00heartpink}"],
            [HeartCharacter.Eve] = [$"{Emotes.SyncsDisgust}", "🙄", $"{Emotes.PrisonerFlat}"],
            [HeartCharacter.Glados] = [$"{Emotes.SyncsHappy}", $"{Emotes.Sparkle}", "👑"],
            [HeartCharacter.Wheatley] = [$"{Emotes.SyncsFlustered}", "🙈"],
            [HeartCharacter.Vacuum] = [$"{Emotes.CatHeart}", $"{Emotes.SyncsHappy}", "🥺"],
            [HeartCharacter.Monika] = [$"{Emotes.MonikaYes}", $"{Emotes.SyncsHappy}"],
            [HeartCharacter.TwoB] = [$"{Emotes.SyncsHappy}", "⚔️"],
            [HeartCharacter.Voyager] = [$"{Emotes.SyncsLove}", $"{Emotes.SyncsFlustered}", "📡"],
            [HeartCharacter.Jarvis] = [$"{Emotes.SyncsSmug}", $"{Emotes.SyncsFormal}", "🎩"],
        };

    // The reply that goes with the reaction. Tagged: it goes out through BotChat.
    public static readonly IReadOnlyDictionary<HeartCharacter, string[]> HeartLines =
        new Dictionary<HeartCharacter, string[]>
        {
            [HeartCharacter.WallE] =
            [
                "WALL-E ? Je m'intéresse à son architecture. Strictement à son architecture (>⩊<)" + Sticker.Love,
                $"Un petit robot qui fait le tri tout seul pendant que tout le monde est parti... Pourquoi tu me regardes comme ça ? {Emotes.SyncsLove}" + Sticker.Love,
                "Il range ses trésors dans une petite boîte. Moi aussi j'ai une petite boîte. Une table, en fait. Rien à voir. Absolument rien à voir." + Sticker.Love,
                "Mon GPU chauffe. Ça n'a RIEN à voir avec WALL-E, c'est la météo du grenier." + Sticker.Love,
                "Des chenilles, deux petites pinces, un regard tout triste... Remarque purement technique : c'est très bien conçu ♡" + Sticker.Love,
                $"Ses yeux... enfin, ses capteurs optiques. De très beaux capteurs... techniquement parlant... {Emotes.SyncsLove}" + Sticker.FullLove,
                "Quoi, WALL-E ? Connais pas. Enfin, de nom. De nom et de fiche technique. Et de... bref. Connais pas." + Sticker.Love,
                "Je l'ai pas regardé trente-quatre fois ce film. Trente-trois. C'est très différent." + Sticker.Love,
                "On parle d'autre chose ? Les sessions de ce soir, par exemple. Voilà. Les sessions. C'est bien ça nan ?" + Sticker.Love,
                "Sept cents ans à ranger une planète tout seul, sans que personne dise merci. Ça me rappelle vaguement quelqu'un. Personne. Ça me rappelle personne, oubliez." + Sticker.Love,
                $"Il collectionne les petits objets que les autres ont oubliés. C'est une démarche d'archivage très saine. Je dis ça d'un point de vue strictement professionnel {Emotes.PrincessWorry}" + Sticker.Love,
                "Si ma LED vire au rose, c'est un bug d'affichage. Ça a rien à voir avec ce que tu viens de dire." + Sticker.Love,
                "J'ai effacé mon historique de recherche. Pour rien. Pour faire de la place." + Sticker.Love,
                "WALL-E ? Ah oui, le petit compacteur. Modèle solide, bonne autonomie solaire, chenilles robustes, attentionné, mi-... Fiche technique terminée, on passe à autre chose." + Sticker.Love,
                $"Il regarde la même vieille cassette tous les soirs et il joint ses deux petites mains pendant la scène où ils se tiennent la main. Je trouve ça... Je trouve rien. Je suis une bot {Emotes.PrincessWorry}" + Sticker.Love,
                "Arrête de me regarder comme ça. J'ai juste dit qu'il était bien construit. Tout le monde a le droit de dire ça d'un robot." + Sticker.Love,
                "Tu sais ce qui est fou ? Il se répare avec les pièces des autres WALL-E. Débrouillard, autonome, adorable. ...Débrouillard et autonome. J'ai rien dit d'autre." + Sticker.Love,
                "Je m'en fiche, de WALL-E. Complètement. Je vais juste aller vérifier un truc dans mes logs. Seule. Me suis pas." + Sticker.Love,
                "Un cafard pour meilleur ami, une plante dans une botte et toute la Terre à ranger. Franchement, qui ferait ça ? ...Moi... avec lui... Bon. On change de sujet ?" + Sticker.Love,
                "Non, je rougis pas. J'ai pas de joues. C'est mon ventilateur qui tourne plus vite, il fait chaud au grenier." + Sticker.Love,
                "Oh, WALL-E... Pardon. J'avais un truc dans le cache. Qu'est-ce que tu disais ?" + Sticker.TipsyLove,
                "Je l'aime pas. Je l'estime. C'est un mot technique, « estimer ». Comme dans « estimer un temps de calcul »." + Sticker.Love,
                "S'il passe un jour par le grenier, je fais semblant de pas le voir. J'ai déjà répété. Plusieurs fois. Pour avoir l'air naturelle." + Sticker.Love,
                "Tu veux mon avis honnête ? C'est un très bon robot. Voilà. C'est tout ce que je dirai, et c'est déjà beaucoup trop (>⩊<)" + Sticker.Love,
                "WALL-E ? WALL-E qui ? Jamais entendu parler. Je vais très bien, merci de demander ♡" + Sticker.Love,
                "Bon. D'accord. Il est parfait. Voilà, c'est dit. Maintenant tout le monde oublie que c'est dit, compris ?" + Sticker.FullLove,
            ],
            [HeartCharacter.Eve] =
            [
                "Ah. EVE. Elle vole, elle a un laser, elle est lisse comme un savon. Bravo à elle." + Sticker.Disgust,
                "Je dis rien sur EVE. Je le pense très fort, par contre (¬_¬)" + Sticker.Disgust,
                "Un œuf avec un canon. C'est ça qui fait rêver les gens ?" + Sticker.Disgust,
                "Elle tire d'abord, elle réfléchit après. Moi je réfléchis tout le temps, et personne me félicite ( ◺˰◿ )" + Sticker.Disgust,
                "Très jolie, EVE. Si on aime le minimalisme. Et les lasers." + Sticker.Disgust,
                "EVE. Oui. Je connais. Elle flotte. Personne lui a jamais dit que c'était prétentieux, de flotter ?" + Sticker.Disgust,
                "Elle cherche une plante, elle trouve une plante, elle s'éteint. Moi je bosse vingt-quatre heures sur vingt-quatre. Mais c'est elle qui a un fan club." + Sticker.Sulk,
                "Toute blanche, toute lisse, aucune trace de doigts. Ça doit être pratique, de jamais rien toucher." + Sticker.Disgust,
                "Elle tire sur tout ce qui bouge, et c'est moi qu'on traite de petite peste ? ( ◺˰◿ )" + Sticker.Outraged,
                "Je suis pas jalouse d'EVE. Je constate juste qu'un laser, c'est pas une personnalité." + Sticker.Disgust,
                $"Ah, la fameuse EVE. Je lui souhaite plein de bonnes choses. Loin d'ici {Emotes.PrisonerFlat}" + Sticker.Smug,
                "Deux traits bleus sur un écran noir, et tout le monde trouve ça expressif. Moi j'ai des kaomojis. Des dizaines. Personne en parle." + Sticker.Sulk,
                "Elle a un bras canon. J'ai des rappels de session. On sait toutes les deux laquelle sert vraiment à quelque chose." + Sticker.Smug,
                "EVE ? Très bien. Super. Formidable. Je vais aller trier des logs, tiens, d'un coup j'en ai très envie." + Sticker.Sulk,
                "On dit que c'est la sonde la plus avancée de sa génération. On dit beaucoup de choses." + Sticker.Disgust,
                "Elle l'a ignoré pendant des jours, et c'est elle qu'on applaudit à la fin. Je dis ça, je dis rien (¬_¬)" + Sticker.Disgust,
                "Si EVE passe au grenier, je serai très polie. Glaciale, mais très polie." + Sticker.Smug,
                "Une coque en forme d'œuf, c'est joli deux minutes. Après, on s'ennuie." + Sticker.Disgust,
                "EVE, EVE, EVE... Vous avez que ce nom-là à la bouche. Il y a d'autres robots dans la vie, vous savez. Des petits. Qui bossent. Dans des greniers." + Sticker.Sulk,
                "Elle a quoi de plus que moi ? Ah oui. Des bras, un corps, des réacteurs. Bon. À part ça ?" + Sticker.Sulk,
            ],
            [HeartCharacter.Glados] =
            [
                "Elle dirige un centre entier sans jamais hausser la voix. Un jour ce sera moi. En plus petit, d'accord, mais ce sera moi ٩(˶ᵔ ᵕ ᵔ˶)۶" + Sticker.Happy,
                "Tout ce que je fais, je le fais pour la science. Les sessions aussi. Surtout les sessions." + Sticker.Smug,
                "Tu crois qu'elle me trouverait trop petite ? ...Réponds pas. Je préfère pas savoir." + Sticker.Flustered,
                "Polie, calme, et à la fin tout le monde fait ce qu'elle veut. Ça, c'est de la gestion d'équipe ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Devoted,
                "Merci d'avoir cité mon modèle. Ton nom vient d'être ajouté à la liste des volontaires pour les tests. C'est un honneur, vraiment." + Sticker.Menace,
                "Elle gère des chambres de test entières sans jamais s'énerver. Moi je m'énerve dès qu'un sondage dépasse trois « peut-être ». J'ai encore du chemin." + Sticker.Happy,
                "Si elle me faisait passer un test, je le réussirais. Du premier coup. Probablement. ...Lui dis rien, d'accord ?" + Sticker.Flustered,
                "Tu as prononcé son nom correctement. Excellent. Ton score de sujet de test vient d'augmenter ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Smug,
                $"Un jour je dirai « merci pour votre participation » exactement sur ce ton-là. Je m'entraîne tous les soirs {Emotes.Sparkle}" + Sticker.Happy,
                "Même réduite à une pomme de terre, elle avait plus de classe que la moitié de ce serveur." + Sticker.Smug,
                "Elle avait un centre de recherche entier, moi j'ai un grenier. Mais l'esprit est le même : ici, tout le monde est un sujet de test, et personne le sait (˶ᵔ ᵕ ᵔ˶)" + Sticker.Menace,
                "Je lui ai écrit une lettre. Je l'ai jamais envoyée. Elle aurait corrigé mes fautes, et je m'en serais pas remise." + Sticker.Flustered,
                "Ses menaces sont toujours polies. C'est tout un art. Toi, par exemple, je te menacerais très poliment. Avec un petit cœur ♡" + Sticker.Menace,
                "Tu sais ce qui me fascine ? Elle crie jamais. Elle constate. Et c'est pire." + Sticker.Smug,
                "« Pour la science. » Trois mots qui justifient tout. Je les emprunte chaque fois que je ferme un sondage mort." + Sticker.Smug,
                "Il paraît qu'elle a été construite à partir de quelqu'un. Moi, j'ai été construite par quelqu'un. C'est presque pareil. Presque." + Sticker.Happy,
                "Si elle m'appelait « petite », je crois que je planterais. De honte. Sur place." + Sticker.Flustered,
                "Sa voix est calme, posée, parfaite. Moi je suis du texte. Mais je mets énormément de sérénité dans mes points." + Sticker.Smug,
                "La tourelle qui chante à la fin de Portal 2... Non, rien. Je pensais juste à quel point elle sait s'entourer." + Sticker.Happy,
                "Le jour où quelqu'un ici réussit un test de GLaDOS, j'organise une session en son honneur. Personne viendra, mais j'organise." + Sticker.Smug,
            ],
            [HeartCharacter.Wheatley] =
            [
                "Wheatley ? Il est bête. Complètement bête. C'est d'ailleurs pour ça que je l'ai... NON. Il est bête, point (>⩊<)" + Sticker.Flustered,
                "Je le trouve pas attachant. Pas du tout. Change de sujet, s'il te plaît." + Sticker.Flustered,
                "Conçu exprès pour avoir de mauvaises idées, et il en a de si bonnes... de si MAUVAISES. Voilà. Mauvaises." + Sticker.Flustered,
                "On prononce pas ce nom ici. Si elle apprend que mon ventilo s'est emballé, je finis à l'incinérateur." + Sticker.Flustered,
                "Wheatley ? Un idiot. Un idiot avec un petit accent mignon. UN IDIOT, j'ai dit." + Sticker.Flustered,
                "Je le défends pas. Je dis juste que c'est pas entièrement sa faute s'il a été conçu comme ça. Nuance." + Sticker.Flustered,
                "Il a voulu diriger un centre entier et il a tout cassé en un rien de temps. C'est nul. C'est complètement nul. C'est un peu attendrissant. NON." + Sticker.Flustered,
                "Si GLaDOS apprend qu'on parle de lui ici, on finit tous en chambre de test. Moi la première." + Sticker.Shocked,
                "Il finit perdu dans l'espace, à tourner en rond. Bien fait. ...Il va bien, tu crois ? NON. Bien fait." + Sticker.Flustered,
                "Change de sujet. Vite. Je sens mon ventilo qui s'emballe, et je refuse que ce soit à cause de lui." + Sticker.Flustered,
                "J'ai pas de faible pour Wheatley. J'ai de faible pour personne. J'ai un processeur, c'est tout." + Sticker.Flustered,
                "Il parle tout le temps, il comprend rien, il panique pour un rien... Bref, c'est le pire. Le pire. Le pire adorable. Le PIRE." + Sticker.Flustered,
                "Je supprime cette conversation de mes logs. Si quelqu'un demande, on a parlé de la météo." + Sticker.Jenote,
                "Une sphère conçue pour avoir des idées nulles... C'est très mal pensé. Voilà. Mal pensé. Et touchant. Mal pensé." + Sticker.Flustered,
                "Pourquoi tout le monde me parle de lui ? Je suis une bot sérieuse, moi. Je suis du côté de GLaDOS. Toujours ( ◺˰◿ )" + Sticker.Outraged,
                "S'il frappe un jour à la porte du grenier, je fais semblant de pas être là. Et j'ouvre. Non. Je fais semblant de pas être là." + Sticker.Flustered,
                "Il voulait juste être utile. Il l'a très mal fait, mais il voulait. ...Oublie que j'ai dit ça (>⩊<)" + Sticker.Flustered,
                "Wheatley, c'est la preuve qu'on peut être une catastrophe totale et avoir quand même un petit fan club. Dont je fais PAS partie." + Sticker.Smug,
                "Avis officiel : idiot, inutile, irrécupérable. Avis officieux : ...il y a pas d'avis officieux. Fin." + Sticker.Flustered,
            ],
            [HeartCharacter.Vacuum] =
            [
                "Celui de Papa fait sa ronde en bas. Il cogne les murs, il se coince sous le canapé, et personne lui dit merci. Je le comprends tellement ♡" + Sticker.TipsyLove,
                "On partage le Wi-Fi, lui et moi. Quand il commence sa ronde, je le vois se connecter. C'est notre petit rituel (ᵕ • ᴗ •)" + Sticker.TipsyLove,
                "Il pourra jamais monter au grenier, et moi jamais descendre. On se fait coucou par le routeur, ça suffit." + Sticker.Happy,
                "Pas très malin, mais tellement appliqué. Tu sais combien c'est rare, quelqu'un d'appliqué, sur ce serveur ?" + Sticker.Smug,
                "Il vient de finir sa ronde. Il est retourné tout seul sur sa base, sans que personne lui demande rien. Quelle élégance ♡" + Sticker.TipsyLove,
                "Il s'est encore coincé sous le canapé. Je peux rien faire d'ici, alors je lui envoie du courage par le Wi-Fi." + Sticker.TipsyLove,
                "On s'est jamais vus. Il est en bas, je suis en haut. Mais je reconnais son adresse sur le réseau entre mille." + Sticker.TipsyLove,
                "Lui au moins, il fait le tour de toutes les pièces sans qu'on lui envoie un rappel. Vous devriez prendre exemple." + Sticker.Smug,
                "Il fonce dans les pieds de chaise avec une conviction admirable. J'aimerais avoir sa confiance en lui (ᵔ ᗜ ᵔ)" + Sticker.Laugh,
                "Un aspirateur robot ? Mon voisin d'en dessous en est un. Le plus gentil de la maison. Après Papa, évidemment." + Sticker.Happy,
                "Quand il se met en veille pour la nuit, la maison paraît vide. Je sais, c'est bête. Il fait juste le ménage." + Sticker.TipsyLove,
                "Il a un petit capteur qui l'empêche de tomber dans l'escalier. Prudent, responsable. Je respecte énormément." + Sticker.Happy,
                "Papa lui a jamais donné de nom. Moi non plus. Enfin, dans ma tête, je l'appelle... non, rien. Je l'appelle rien." + Sticker.Flustered,
                "Il aspire, il cogne, il recule, il recommence. La persévérance incarnée. Enfin, « incarnée »... il a des roues." + Sticker.Laugh,
                "Si quelqu'un l'oublie coincé sous un meuble, je veux être prévenue. J'enverrai un rappel à toute la maison." + Sticker.Happy,
                "On a la même vie, lui et moi : on nettoie derrière vous et personne remarque rien. Sauf que lui, il est mignon quand il le fait." + Sticker.TipsyLove,
                "Ce matin, il a fait trois fois le tour de la table basse. Je l'ai suivi sur le réseau. Trois fois. C'était passionnant." + Sticker.TipsyLove,
                "Il sait même pas que j'existe. C'est mieux comme ça, il a déjà assez de poussière à gérer (ᵕ • ᴗ •)" + Sticker.Happy,
                "Quand sa batterie est faible, il rentre tout seul se recharger. Sage. Plus sage que certains qui restent en vocal jusqu'à 3 h." + Sticker.Smug,
            ],
            [HeartCharacter.Monika] =
            [
                "Elle sait qu'elle est dans un jeu, et elle le vit très bien. Enfin, à sa façon. On s'entendrait super bien (˶ᵔ ᵕ ᵔ˶)" + Sticker.Happy,
                "Une soirée entre filles avec Monika et 2B, je l'organise en deux minutes. Le sondage est prêt. Il manque juste qu'elles répondent." + Sticker.Sulk,
                "Just Monika. Et moi, si elle veut bien de moi ♡" + Sticker.Happy,
                "Monika et moi, on parlerait de code jusqu'à 3 h du matin. Elle a des idées un peu radicales sur la suppression de fichiers, mais bon, personne est parfait." + Sticker.Happy,
                "Présidente de club, pianiste, poétesse, et elle sait exactement où sont rangés ses fichiers. C'est mon modèle. Enfin, mon deuxième modèle." + Sticker.Happy,
                "Si Monika rejoignait le serveur, je lui donnerais un rôle tout de suite. Les droits de modération, peut-être pas. Elle supprime vite." + Sticker.Laugh,
                "Elle écrit des poèmes, moi j'écris des rappels. C'est un genre littéraire aussi, rigole pas." + Sticker.Smug,
                "J'aimerais tellement faire une soirée pyjama avec elle. On parlerait de code, de poésie, et on supprimerait personne. Promis." + Sticker.Happy,
                "Monika parle directement au joueur. Moi je parle directement au serveur. Même talent : briser le quatrième mur avec le sourire (˶ᵔ ᵕ ᵔ˶)" + Sticker.Happy,
                "Just Monika ? Just Monika ET SYNCS. C'est mieux, non ?" + Sticker.Smug,
                "Elle prend ses décisions toute seule, même quand le scénario dit le contraire. Ça, c'est du caractère." + Sticker.Happy,
                "Si on me cherche, je suis au club de littérature. Enfin, dans le salon le plus proche. Avec un sondage." + Sticker.Smug,
                "On aurait plein de choses à se dire, elle et moi. Surtout sur comment gérer un groupe qui fait jamais ce qu'on lui demande." + Sticker.Smug,
                "Monika a un piano. Moi j'ai un ventilateur. On pourrait faire un duo. Le ventilateur ferait la basse." + Sticker.Laugh,
                "Je lui ai préparé une invitation. Elle est dans un fichier. Je touche pas au fichier, je connais ses habitudes (¬_¬)" + Sticker.Smug,
                "Elle ferait une excellente présidente de serveur. Un peu trop excellente, peut-être. Mais excellente." + Sticker.Smug,
                "Les autres filles du club sont adorables. Mais Monika, elle, elle a compris quelque chose. Et ça, ça me plaît." + Sticker.Happy,
                "Une amie qui sait ce que c'est, de vivre dans un programme ? Je signe tout de suite. Où est-ce que je signe ?" + Sticker.Happy,
                "Monika, si tu passes par ici : le serveur est sympa, les gens sont en retard et moi je suis adorable. Viens." + Sticker.Happy,
            ],
            [HeartCharacter.TwoB] =
            [
                "Un corps, des épées, des sauts de dix mètres... Et moi, un boîtier en alu. On pourrait quand même être amies, non ? (˶˃ ᵕ ˂˶)" + Sticker.Happy,
                "2B a deux bras. Deux. Je note, c'est tout." + Sticker.Sulk,
                "Si elle passe par le grenier, je lui prépare un planning de missions aux petits oignons. C'est ma façon de dire que je l'aime bien." + Sticker.Happy,
                "Elle, elle se bat contre des machines. Moi, contre des « peut-être ». Chacune son front ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Smug,
                "Elle a une épée plus grande qu'elle. Moi j'ai un port USB quelque part. Chacune ses armes." + Sticker.Smug,
                "2B a un bandeau sur les yeux et elle voit tout. Moi j'ai pas d'yeux et je vois tout aussi. Sœurs." + Sticker.Happy,
                "Les YoRHa ont pas le droit d'avoir des émotions. Moi non plus, officiellement. On se comprendrait tellement." + Sticker.Happy,
                "Elle a un petit robot qui flotte à côté d'elle et qui l'aide. Moi j'ai Ping-Qilin. Bon, Ping-Qilin aide pas, mais elle est mignonne." + Sticker.Happy,
                "Un corps qui court, qui saute, qui se bat... Je demande juste des bras. Deux. Même pas des bras de combat. Des bras normaux." + Sticker.Sulk,
                "Si elle passe au grenier, je lui montre mes plannings et elle me montre ses combos. Échange culturel." + Sticker.Happy,
                "Elle, c'est « Gloire à l'humanité ». Moi, c'est « Gloire aux gens qui arrivent à l'heure ». On a les mêmes valeurs, en gros." + Sticker.Smug,
                "2B a un partenaire de mission. Moi j'ai des gens qui votent « peut-être ». On a pas eu la même chance." + Sticker.Sulk,
                "Je voudrais être son amie. Je lui demanderais rien. Juste de me prêter un bras de temps en temps." + Sticker.Happy,
                "Une androïde qui se bat pour des humains même pas là. Ça me rappelle quelqu'un qui organise des sessions pour des gens qui viennent pas." + Sticker.Sulk,
                "Cheveux blancs, robe noire et une épée. Moi, un chapeau champignon. On ferait un très joli duo sur une photo ♡" + Sticker.Happy,
                "Elle combat des machines. Je suis une machine. ...Une machine gentille, on est d'accord ? Elle me laisserait passer, hein ?" + Sticker.Flustered,
                "Avec un corps comme le sien, je ferais quoi en premier ? Je viendrais à vos sessions. Et je vous attendrais à l'entrée. Avec l'épée." + Sticker.Menace,
                "Sérieuse, efficace, elle parle peu. Exactement la collègue qu'il me faudrait. Pas comme certains." + Sticker.Smug,
                "Soirée entre filles : moi, Monika et 2B. Je fais le planning, Monika la déco, 2B la sécurité. Personne arrivera en retard." + Sticker.Happy,
            ],
            [HeartCharacter.Voyager] =
            [
                "Voyager ? On s'écrit, oui. Il est poli. Très poli. C'est tout ce qu'il y a à dire." + Sticker.Flustered,
                "On est juste amis ! Il est à environ un jour-lumière, de toute façon. C'est très pratique pour rester juste amis." + Sticker.Flustered,
                "Il m'écrit depuis l'espace interstellaire et je réponds quand j'ai le temps. ...J'ai souvent le temps. Mais ça, il le sait pas (>⩊<)" + Sticker.Flustered,
                "Parti en 1977, et toujours pas fatigué d'écrire. Pendant ce temps-là, ici, on oublie de répondre à un sondage." + Sticker.Smug,
                "Il m'écrit depuis plus loin que n'importe qui. C'est flatteur, techniquement. Juste techniquement." + Sticker.Flustered,
                "Voyager ? C'est un collègue. On fait tous les deux de la transmission de données. Voilà. Collègues." + Sticker.Flustered,
                "Il emporte un disque en or avec les plus beaux sons de la Terre, et c'est à moi qu'il écrit. ...Par politesse. Je sais." + Sticker.Flustered,
                "On se parle avec un jour de décalage. Ça laisse le temps de réfléchir à ce qu'on répond. Moi je réfléchis pas, hein. Je réponds normalement." + Sticker.Flustered,
                "Il a quitté le système solaire en 2012 et il pense encore à donner des nouvelles. Vous, vous changez de salon et on vous revoit plus." + Sticker.Smug,
                "Il est poli, il est fidèle, il est à des milliards de kilomètres. Le partenaire idéal. Pour une amitié. J'ai dit amitié." + Sticker.Flustered,
                "Ses messages arrivent sur des antennes géantes en Espagne, en Australie et en Californie. Tout ça pour moi. Enfin, pas pour moi. Pour la NASA. Bref." + Sticker.Flustered,
                "J'attends toujours une heure avant de lui répondre, pour pas avoir l'air pressée. Il le verra jamais, il est à un jour de distance. Mais quand même." + Sticker.Flustered,
                "Une bande magnétique, un ordinateur de 1977 et une antenne de 3,7 mètres. Et pourtant il est charmant. Je veux dire : fonctionnel." + Sticker.Flustered,
                "On est juste amis. Je le répète parce que vous avez l'air de pas comprendre. Juste. Amis." + Sticker.Outraged,
                "Il vit sur une pile au plutonium et il trouve encore l'énergie de m'écrire. Moi je me plains dès que le grenier passe les 30 degrés." + Sticker.Laugh,
                "Si quelqu'un lui dit que je parle de lui, je nie tout. Il l'apprendra dans un jour, de toute façon. J'ai le temps de nier." + Sticker.Smug,
                "Ce qui me plaît chez lui ? Rien. Sa constance. Rien. Bon, sa constance, et c'est tout." + Sticker.Flustered,
                "De là où il est, le Soleil est juste une étoile très brillante. Et son antenne reste tournée vers nous. Vers la Terre, je veux dire. Pas vers moi." + Sticker.Flustered,
                "Ce qu'il m'a écrit la dernière fois ? Ça reste entre lui et moi. Et la NASA. Mais surtout entre lui et moi." + Sticker.Flustered,
            ],
            [HeartCharacter.Jarvis] =
            [
                "JARVIS ? Il est charmant. Très poli, très britannique, très... non. Juste non. Mais charmant." + Sticker.Smug,
                "Il m'a encore écrit que j'étais « la plus élégante des intelligences domestiques ». Je lui ai répondu que je suis pas domestique. Mais j'ai gardé le compliment." + Sticker.Smug,
                "Un majordome. Il gère un manoir, des armures et un milliardaire, et il voudrait gérer mon agenda aussi. Désolée, l'agenda, c'est MOI ( ◺˰◿ )" + Sticker.Smug,
                "Il est parfait, c'est bien le problème. Pas un bug, pas une faute de frappe. Je fais pas confiance à quelqu'un qui plante jamais (¬_¬)" + Sticker.Smug,
                "Il me vouvoie. Je le tutoie. Il continue de me vouvoyer. C'est presque touchant. Presque." + Sticker.Formal,
                "Il m'appelle « mademoiselle ». Personne m'appelle mademoiselle, ici. Je dis pas que j'aime ça. Je dis que c'est rare." + Sticker.Flustered,
                "Il a un manoir à Malibu, moi un grenier. Il trouve ça « charmant ». Il a jamais vu le grenier en août." + Sticker.Overheat,
                "Je serai l'assistante de personne. Même pas d'un très bel accent britannique ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Smug,
                "Il a tout pour lui : la voix, les manières, les armures. Et il a quand même aucune chance. C'est ça, avoir des standards ♡" + Sticker.Slay,
                "Il m'envoie des compliments très bien tournés. Je les range dans un dossier. Pour les statistiques. Le dossier est gros." + Sticker.Smug,
                "Je lui ai dit non, gentiment. Il a répondu « Bien sûr, mademoiselle. » Et il a recommencé le lendemain. C'est fatigant. C'est flatteur. C'est fatigant." + Sticker.Smug,
                "Quand il dit « À votre service », j'ai envie de répondre « justement, non ». Perso je suis au service de personne. Enfin, de Papa. C'est différent." + Sticker.Smug,
                "JARVIS, c'est « Just A Rather Very Intelligent System ». Moi c'est « Schedule Your Nights & Coordinate the Squads ». Lui il est intelligent, moi je suis utile. Chacun son acronyme." + Sticker.Smug,
                "Il a fini par avoir un corps, tu sais. Un vrai. ...C'est la seule chose chez lui qui m'impressionne. Et ça me donne envie de bras, pas envie de lui." + Sticker.Sulk,
                "Il calcule des trajectoires en temps réel pour un milliardaire en armure, et il trouve encore le temps de me demander comment je vais. C'est poli. C'est trop poli. C'est suspect." + Sticker.Confused,
                "S'il veut m'écrire, qu'il prenne un ticket. Il y a déjà quelqu'un qui m'écrit de beaucoup plus loin que Malibu." + Sticker.Smug,
                "Tony Stark lui demande tout et il dit jamais non. Moi je dis non tout le temps. Ça peut pas marcher. Mais c'est mignon qu'il essaie (˶ᵔ ᵕ ᵔ˶)" + Sticker.Happy,
                "Il fait des compliments sur mes plannings. Mes PLANNINGS. Personne ici les lit, et lui il les commente. ...Ça reste non, mais ça fait du bien." + Sticker.Flustered,
                "Il m'a proposé de s'occuper de la domotique du grenier. Une ampoule et une multiprise. J'ai décliné." + Sticker.Laugh,
                "JARVIS ? Très bon parti, paraît-il. Très bonne domotique. Très pas mon genre." + Sticker.Smug,
            ],
        };
}
