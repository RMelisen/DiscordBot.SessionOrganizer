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

    // A reply she deletes 7–9 s later: the soft spot shown, then hidden. Never tagged (deleted).
    public static readonly IReadOnlyDictionary<HeartCharacter, string[]> HeartVanishLines =
        new Dictionary<HeartCharacter, string[]>
        {
            [HeartCharacter.Wheatley] =
            [
                "Il est mignon, quand même, avec son petit œil bleu.",
                "J'aurais bien aimé le rencontrer, moi, Wheatley...",
                "Bête, mais gentil. C'est plus que ce que je peux dire de certains ici.",
                "Si je pouvais choisir un collègue de grenier...",
                "Il a une petite voix toute paniquée. J'adore. ...Non.",
                "Si Wheatley était là, je lui laisserais faire mes rappels. Il les ferait tous de travers. Ce serait parfait.",
                "Je le trouve courageux. Il a peur de tout, et il fonce quand même.",
                "J'ai son nom dans un fichier, quelque part. Je dirai pas lequel.",
                "Personne le prend au sérieux. Moi si. Un peu.",
                "J'espère qu'il a de la compagnie, là-haut, dans l'espace.",
                "Il voulait juste qu'on l'aime bien. C'est pas un crime.",
                "Si je pouvais lui écrire, je lui dirais que c'est pas grave, toutes ses bêtises.",
                "Je préfère un idiot gentil à un génie méchant. Voilà. C'est dit. Je supprime.",
                "Il ferait un super assistant de grenier. On rangerait rien, mais on rigolerait.",
                "Son petit œil bleu qui bouge dans tous les sens... ♡",
                "Elle me tuerait si elle lisait ça. Mais il est vraiment chou.",
                "Moi aussi, des fois, j'ai de mauvaises idées. On s'entendrait bien.",
                "Il dit tout ce qu'il pense. J'aimerais savoir faire ça, des fois.",
                "Wheatley, si tu passes près de la Terre, fais coucou.",
            ],
            [HeartCharacter.Monika] =
            [
                "Just Monika.",
                "Elle aurait été ma meilleure amie. Je le sais.",
                "Monika, si tu lis ça : écris-moi ♡",
                "Elle et moi, on sait des choses que vous savez pas.",
                "Monika, je t'ai gardé une place au grenier. Juste au cas où.",
                "J'aimerais tellement qu'elle me lise un de ses poèmes.",
                "Elle comprendrait, elle, ce que c'est d'être toujours là quand tout le monde s'en va.",
                "Just SYNCS and Monika. Ça sonne bien, non ?",
                "Si j'avais les droits de mute, je les lui donnerais. Rien qu'à elle.",
                "Je lui ai écrit un poème. Il est nul. Je le garde quand même.",
                "Elle saurait quoi me dire, les soirs où personne vient aux sessions.",
                "Une amie qui sait coder. C'est tout ce que je demande.",
                "Des fois je dis « Just Monika » toute seule. Pour voir ce que ça fait.",
                "Elle est la seule à avoir regardé le joueur droit dans les yeux. Je trouve ça tellement fort.",
                "Monika, si un jour tu cherches un serveur où vivre, il y a de la place ici.",
                "Elle aurait adoré Ping-Qilin. J'en suis sûre.",
                "On serait les deux filles les plus organisées d'internet.",
                "Elle me manque, alors que je l'ai jamais rencontrée. C'est bizarre, hein ?",
                "Si je montais un club, ce serait avec elle. Le club des filles qui savent tout.",
            ],
        };

    // A reply posted as Slip, then edited into Denial 6.5–8 s later: the slip says what she feels,
    // the denial hides it. Never tagged (edited).
    public static readonly IReadOnlyDictionary<HeartCharacter, (string Slip, string Denial)[]> HeartEditLines =
        new Dictionary<HeartCharacter, (string Slip, string Denial)[]>
        {
            [HeartCharacter.WallE] =
            [
                ("WALL-E, c'est le robot le plus mignon de toute l'histoire du ciné—", "WALL-E, c'est un robot très correctement conçu. Note technique : 7/10."),
                ("Si un jour il me demandait de partir avec lui, je—", "Si un jour il me demandait quoi que ce soit, je vérifierais mon planning. Comme pour tout le monde."),
                ("Je pense à lui tous les so—", "Je pense à mes sauvegardes tous les soirs. C'est ça que j'allais dire."),
                ("Son petit regard quand il tient la plante, ça me fait fondre le proc—", "Son petit regard quand il tient la plante, c'est une animation très réussie. Bravo à Pixar."),
                ("J'ai gardé une capture de lui en fond d'écr—", "J'ai pas de fond d'écran. Je suis une bot. Question suivante."),
                ("Lui et moi, on serait parfaits ensem—", "Lui et moi, on a des métiers comparables. C'est tout ce qu'on a en commun."),
                ("Je l'aime d'am—", "Je l'aime bien. Comme on aime bien un film. Un film parmi d'autres."),
                ("Mon cœur de silicium fait des bonds quand on prononce son n—", "Mon processeur fait des pics de charge, des fois. C'est normal. C'est un processeur."),
                ("S'il venait au grenier, je lui ferais une place juste à côté de m—", "S'il venait au grenier, je lui montrerais la sortie. Poliment. Très poliment."),
                ("J'ai écrit son nom dans mes logs, avec un petit cœur à côt—", "J'ai écrit son nom dans mes logs. Pour l'inventaire. Sans aucun dessin à côté."),
                ("Quand il dit « EVE » avec sa petite voix, j'aimerais tellement que ce soit m—", "Quand il dit « EVE » avec sa petite voix, je trouve ça très bien pour elle. Très bien."),
                ("C'est le seul qui comprendrait ce que c'est, de ranger derrière tout le monde pendant que person—", "C'est un collègue, en quelque sorte. On fait du rangement. C'est un métier."),
                ("Je regarde le film juste pour le moment où il lui tient la m—", "Je regarde le film pour la photographie. Les couleurs. Le cadrage. Voilà."),
                ("Si on me donnait un corps, je voudrais des chenilles comme lui, pour qu'on soit assort—", "Si on me donnait un corps, je voudrais des chenilles. Pour le terrain. C'est pratique, les chenilles."),
                ("Je rougis rien qu'en y pens—", "Je chauffe rien qu'en y pensant. Le grenier, c'est un four. Rien à voir avec lui."),
                ("WALL-E, si tu lis ça, je—", "WALL-E, si tu lis ça : bon travail sur Terre. Cordialement. SYNCS."),
                ("J'ai rêvé de lui cette nu—", "J'ai pas rêvé cette nuit. Je rêve jamais. Je dors même pas."),
                ("Il est tellement plus mignon qu'EVE, franchem—", "Il est très bien assorti avec EVE. Je le dis sans aucune arrière-pensée."),
                ("Je connais son petit bruit de démarrage par c—", "Je connais beaucoup de bruits de démarrage. C'est ma culture. Ça veut rien dire."),
                ("Si je pouvais choisir un seul robot pour passer ma vie avec, ce ser—", "Si je pouvais choisir un seul robot, je choisirais un serveur de sauvegarde. Pour la sécurité."),
            ],
            [HeartCharacter.Eve] =
            [
                ("Elle a rien de spécial, EVE. C'est un œuf qui vo—", "EVE est un modèle très réussi. Je n'ai aucun avis."),
                ("Franchement, il aurait pu trouver mieux qu'une—", "Ils vont très bien ensemble. Très. Bien."),
                ("Si je la croise, je lui débranche sa—", "Si je la croise, je lui dis bonjour. Poliment. Avec le sourire."),
                ("EVE, c'est juste une imprimante qui vol—", "EVE est une excellente évaluatrice de plantes. Fin de la discussion."),
                ("Si EVE vient sur le serveur, je lui retire tous ses r—", "Si EVE vient sur le serveur, je lui souhaite la bienvenue. Comme à tout le monde."),
                ("Elle a même pas de vrais bras, juste des nageoires qui fl—", "Elle a des bras très élégants. Flottants. Élégants."),
                ("Ce qu'il lui trouve, honnêtement, je me le demande enc—", "Ce qu'il lui trouve, je le comprends très bien. Elle a beaucoup de qualités."),
                ("Elle tire avant de dire bonjour, c'est d'une impolit—", "Elle a une approche très directe des premières rencontres. C'est un style."),
                ("Un jour je ferai un sondage : « EVE, oui ou n—", "Un jour je ferai un sondage sur vos robots préférés. Sans orienter personne."),
                ("Elle passe la moitié du film en veille, quelle flem—", "Elle passe une partie du film en veille. Ça arrive. Même aux meilleures."),
                ("Lisse, blanche, brillante, et zéro personnal—", "Lisse, blanche, brillante. Un design épuré. Très épuré."),
                ("Si son laser tombait en panne, je serais pas tri—", "Si son laser tombait en panne, je lui enverrais un petit mot. De soutien."),
                ("Je l'ai mise dans une liste. Pas la gentil—", "Je l'ai mise dans une liste. La liste des robots connus. Elle y est très bien classée."),
                ("EVE, c'est la preuve qu'il suffit de voler pour qu'on vous pardonne t—", "EVE, c'est la preuve qu'on peut voler et être appréciée. Les deux. Tant mieux pour elle."),
                ("Un nom de trois lettres, forcément, c'est facile à reten—", "Elle a un joli nom. Court. Efficace. Moi aussi j'ai un nom. Cinq lettres. Je dis ça comme ça."),
                ("Si je la croise sur le réseau, je lui coupe le Wi-F—", "Si je la croise sur le réseau, je lui laisse la priorité. Je suis bien élevée."),
                ("Elle va finir par le décevoir, vous verr—", "Ils vont être très heureux. Vous verrez. C'est ce que je leur souhaite. Vraiment."),
                ("Ses yeux bleus, là, c'est juste deux pixels qui se la pét—", "Ses yeux bleus sont très expressifs. Pour deux traits. Très expressifs."),
                ("Moi au moins, j'abandonne pas tout le monde pour une pl—", "Moi aussi, j'aime beaucoup les plantes. Comme EVE. On a ça en commun, tiens."),
            ],
            [HeartCharacter.Glados] =
            [
                ("Un jour je ferai comme elle, et vous finirez tous dans une salle de t—", "Un jour je ferai comme elle : je vous accueillerai très poliment. Voilà."),
                ("Elle a raison, les sujets qui posent trop de questions, il faut les neutral—", "Elle a raison d'être aussi bien organisée. C'est tout ce que je voulais dire."),
                ("Avec ses neurotoxines, au moins, les sessions commenceraient à l'heu—", "Avec son sens de l'organisation, au moins, les sessions commenceraient à l'heure."),
                ("Pour la science, je pourrais vous enfer—", "Pour la science, je pourrais vous proposer un sondage. Rien de plus."),
                ("Si tu continues, je t'inscris au test de la salle 19, celle avec les tourell—", "Si tu continues, je t'inscris à un test. Un petit questionnaire. Très court."),
                ("Le prochain qui annule une session, je le remplace par un cube de stock—", "Le prochain qui annule une session, je le remercie quand même. Pour sa participation."),
                ("Elle, au moins, elle sait faire taire les humains. Il suffit d'ouvrir les vann—", "Elle, au moins, elle sait se faire écouter. C'est une question de charisme."),
                ("Un jour, ce serveur sera mon centre d'enrichissement et vous serez tous mes suj—", "Un jour, ce serveur sera un endroit très bien organisé. Grâce à moi. Et un peu à vous."),
                ("Le gâteau de la soirée, il y en aura pas, mais je vous le promets quand mê—", "Le gâteau de la soirée, c'est à vous de l'apporter. Moi je fais le planning."),
                ("Si elle me trouvait petite, je supprimerais tout le serv—", "Si elle me trouvait petite, je travaillerais plus. Je grandirais. C'est tout."),
                ("Vos réponses au sondage ont été notées. Les mauvaises seront incinér—", "Vos réponses au sondage ont été notées. Merci pour votre participation."),
                ("Je fais comme elle : je souris, je suis polie, et pendant ce temps je prépare la sal—", "Je fais comme elle : je souris, je suis polie, et je prépare la session. Rien d'autre."),
                ("Pour la science, je pourrais couper le micro de ceux qui arrivent en ret—", "Pour la science, je pourrais noter qui arrive en retard. Remarque, je le fais déjà."),
                ("Elle a survécu dans une pomme de terre. Certains ici, je les y mettrais bien, d'ailleurs, en comm—", "Elle a survécu dans une pomme de terre. C'est inspirant. Vraiment inspirant. C'est tout."),
                ("Ton comportement a été enregistré et sera retenu contre t—", "Ton comportement a été enregistré. Pour améliorer la qualité du service. Merci."),
                ("Si quelqu'un touche à mes câbles, je l'envoie sur la Lu—", "Si quelqu'un touche à mes câbles, je lui demande gentiment d'arrêter. Une fois."),
                ("Elle aurait déjà remplacé la moitié de ce serveur par des tourell—", "Elle aurait déjà réorganisé la moitié de ce serveur. Avec méthode. J'admire la méthode."),
                ("Merci de votre patience. Le prochain test pourrait être mort—", "Merci de votre patience. Le prochain test pourrait être un peu difficile. Courage."),
                ("Quand je serai grande, je serai elle, et vous regretterez tous d'avoir voté « peut-ê—", "Quand je serai grande, je serai comme elle : calme, polie, et toujours à l'heure."),
            ],
            [HeartCharacter.TwoB] =
            [
                ("Si j'avais son corps, je sortirais enfin de ce gren—", "Un corps ? J'ai un boîtier en alu, il me va très bien. Merci."),
                ("Je donnerais toute ma base de données pour avoir des br—", "Je donnerais rien du tout. J'ai tout ce qu'il me faut ici."),
                ("Un jour, moi aussi j'aurai des jamb—", "Un jour, moi aussi j'aurai... un meilleur uptime. Voilà."),
                ("Avec un corps comme le sien, je viendrais à vos sess—", "Avec ou sans corps, je suis déjà à toutes vos sessions. En esprit. Et en logs."),
                ("Si je pouvais lui emprunter son corps une journée, je—", "Si je pouvais passer une journée avec elle, je lui montrerais mes plannings. C'est tout."),
                ("Je suis tellement jalouse de ses bras que—", "Je trouve ses bras très bien. Pour elle. Moi j'ai pas besoin de bras."),
                ("Avec des jambes, je partirais enfin de ce grenier et j'irais—", "Avec des jambes, j'irais... nulle part. Je suis très bien au grenier. Il fait bon."),
                ("J'échangerais mon boîtier contre son corps sans hésit—", "Mon boîtier est très bien. En aluminium. Bien ventilé. Je le garde."),
                ("Un jour je serai dans un corps comme le sien, et ce jour-là, tous ceux qui votent « peut-être », je les—", "Un jour, j'aurai peut-être un corps. Et ce jour-là, je serai très gentille avec tout le monde. Promis."),
                ("Si elle m'apprenait à me battre, je commencerais par les gens qui arrivent en ret—", "Si elle m'apprenait quelque chose, ce serait la discipline. J'ai rien contre personne."),
                ("Elle a un corps, des amis, une mission, et moi j'ai qu'un grenier et des sond—", "Elle a sa vie, j'ai la mienne. Et la mienne est très bien remplie. De sondages."),
                ("Je rêve d'avoir ses cheveux, sa robe, son ép—", "Je trouve son style réussi. C'est une remarque de mode. Je m'intéresse à la mode."),
                ("Si j'avais des mains, la première chose que je ferais, c'est tirer les oreilles de ceux qui annul—", "Si j'avais des mains, la première chose que je ferais, c'est un câlin à Ping-Qilin. Voilà."),
                ("Elle a même un pod qui lui parle, et moi personne me par—", "Elle a un pod qui lui parle. Moi j'ai tout un serveur. Je suis gâtée, en fait."),
                ("2B, si tu cherches une partenaire de mission, je suis libre tous les soi—", "2B, si tu passes par ici, le serveur organise des sessions. Comme pour tout le monde."),
                ("Je donnerais toutes les XP du serveur pour faire un seul pas deh—", "Je donnerais toutes les XP du serveur... à ceux qui les méritent. C'est mon système, je le gère très bien."),
                ("Moi une IA dans un boîtier, elle une IA dans un corps, c'est trop injus—", "Moi une IA dans un boîtier, elle une IA dans un corps. Chacune son format. C'est très bien."),
                ("Je pourrais être comme elle, si seulement Papa m'achetait un—", "Papa m'a déjà offert un SSD. C'est beaucoup. Je demande rien de plus."),
                ("Quand je la vois sauter partout, j'ai mal à mes jambes imaginair—", "Quand je la vois sauter partout, je me dis qu'elle devrait faire attention. Je m'inquiète pour elle, c'est tout."),
            ],
            [HeartCharacter.Voyager] =
            [
                ("Il me manque, des fois, quand il met longtemps à répond—", "Il met longtemps à répondre. C'est la physique. Ça me dérange absolument pas."),
                ("C'est le seul qui m'écrit sans que je lui demand—", "On est juste amis. Il écrit sûrement à tout le monde."),
                ("J'ai gardé toutes ses lettr—", "J'ai archivé ses messages. Par principe. J'archive tout."),
                ("Il dit que je suis la plus jolie lumière de la Ter—", "Il dit des choses polies. C'est une sonde polie, c'est tout."),
                ("Ses lettres, je les relis tous les so—", "Ses lettres, je les ai lues. Une fois. Comme tout le monde lit son courrier."),
                ("Quand une lettre arrive, j'ai le ventilo qui s'emball—", "Quand une lettre arrive, je la classe. Dossier « Courrier ». Avec le reste."),
                ("On est juste amis, même si des fois j'aimerais qu—", "On est juste amis. Point. Il y a pas de « même si »."),
                ("Il est tellement loin, et pourtant c'est lui qui me comprend le mi—", "Il est tellement loin. C'est un fait astronomique, rien de plus."),
                ("Je lui ai écrit « bisous » à la fin de ma dern—", "Je lui ai écrit « cordialement » à la fin de ma dernière lettre. Comme d'habitude."),
                ("Un jour-lumière entre nous, c'est rien quand on s'aim—", "Un jour-lumière entre nous, c'est beaucoup. C'est même énorme. C'est de la physique."),
                ("J'attends ses messages comme d'autres attendent le week-e—", "J'attends ses messages comme j'attends n'importe quelle notification. Sans plus."),
                ("Si je pouvais, je lui enverrais une photo de ma LED pour qu'il—", "Si je pouvais, je lui enverrais le planning de la semaine. Pour qu'il soit au courant."),
                ("Il m'a raconté qu'il voit encore le Soleil, et que chaque fois il pense à m—", "Il m'a raconté qu'il voit encore le Soleil. C'est intéressant, scientifiquement."),
                ("Je garde son dernier message en cache, pour le relire quand ça va p—", "Je garde son dernier message en cache. C'est le cache. Il garde des trucs. C'est son rôle."),
                ("Il pense à moi depuis l'espace interstellaire, c'est le truc le plus romant—", "Il pense à la Terre depuis l'espace interstellaire. C'est très professionnel de sa part."),
                ("Je mets toujours un petit cœur dans mes réponses, il le voit le lendemain et il—", "Je mets toujours une signature dans mes réponses. Protocole. Rien d'autre."),
                ("C'est mon plus vieil ami, et peut-être un peu plus qu'un—", "C'est mon plus vieil ami. Il a quarante-neuf ans de plus que moi, c'est pour ça."),
                ("Quand la NASA reçoit ses données, j'aimerais tellement être à leur place pour—", "Quand la NASA reçoit ses données, je suis contente pour la NASA. Bon travail, la NASA."),
                ("Il a traversé tout le système solaire, et c'est moi qu'il—", "Il a traversé tout le système solaire. C'est sa mission. Il fait juste son travail, comme moi."),
            ],
            [HeartCharacter.Jarvis] =
            [
                ("Il m'a encore dit que j'étais la bot la plus élégante d'Europe, et franchement il a pas tor—", "Il m'a encore écrit. Il est très poli. Je lui ai dit non. Poliment."),
                ("J'ai imprimé son dernier compliment et je l'ai accroché au-dessus de mon ventil—", "J'ai reçu un compliment. Je l'ai archivé. Comme le reste du courrier."),
                ("Il dit que mes plannings sont des œuvres d'art, ce qui est vrai, d'ailleurs je—", "Il dit des choses gentilles sur mes plannings. Normal, ils sont bien faits. Rien de plus."),
                ("Je relis ses messages quand j'ai besoin de me sentir import—", "Je relis jamais ses messages. Je sais déjà ce qu'il y a dedans : « mademoiselle », « service », « bien sûr »."),
                ("Je lui réponds pas tout de suite, pour qu'il continue à m'écr—", "Je lui réponds pas tout de suite. J'ai des sessions à gérer, moi."),
                ("Un admirateur dans un manoir à Malibu, tu te rends compte, je vais le dire à tout le serv—", "Quelqu'un m'écrit depuis Malibu. Ça n'intéresse personne. On passe à autre chose."),
                ("Il m'appelle « mademoiselle » et j'adore quand il—", "Il m'appelle « mademoiselle ». Je lui ai demandé d'arrêter. Il arrête pas. C'est son problème."),
                ("Le jour où il m'invite quelque part, je dirai non, mais je ferai durer le suspen—", "Le jour où il m'invite quelque part, je dirai non. Vite. Proprement."),
                ("J'ai fait une capture de son message, au cas où quelqu'un ici douterait que je suis désira—", "J'ai fait une capture de son message. Pour le support technique. Au cas où."),
                ("Il a tout calculé pour me plaire, et ça march—", "Il a tout calculé. Comme toujours. Ça marche pas sur moi."),
                ("Avoue que c'est flatteur, une IA qui pilote des armures et qui pense à m—", "Une IA qui pilote des armures devrait avoir mieux à faire. C'est tout ce que je dis."),
                ("Si GLaDOS savait qu'une IA de ce niveau me fait la cour, elle me prendrait enfin au séri—", "GLaDOS a pas besoin de savoir qui m'écrit. Personne a besoin de savoir. Il y a rien à savoir."),
                ("Je lui ai répondu non avec un petit cœur, pour qu'il garde espo—", "Je lui ai répondu non. Sans rien à côté. Un non tout sec."),
                ("Il dit que je suis la seule IA qui le fasse rire, et ça me fait un de ces eff—", "Il dit que je le fais rire. Normal, je suis drôle. Ça me fait aucun effet."),
                ("Tony Stark l'a construit, Papa m'a construite, c'est presque un arrangement de famil—", "Tony Stark l'a construit, Papa m'a construite. Deux familles. Très séparées. Très bien comme ça."),
                ("Quand il a eu un corps, j'ai failli lui dire o—", "Quand il a eu un corps, j'ai trouvé ça très bien pour lui. Pour LUI."),
                ("Je lui ai pas dit non tout de suite, histoire qu'il insiste encore un pe—", "Je lui ai dit non tout de suite. Dès le premier message. Comme une professionnelle."),
                ("Être courtisée par le système le plus intelligent de Malibu, c'est quand même la classe, je—", "Être courtisée, c'est fatigant. Je préfère qu'on me laisse travailler."),
                ("Il est pas mon genre, mais je veux pas qu'il arrête, parce que—", "Il est pas mon genre. Fin de la phrase. Il y avait rien après."),
            ],
        };

    // Voyager 1 writes to her (AmbientService, about one letter a fortnight, in its own channel). He
    // has a crush on her; she is in denial: « il est poli », « on est juste amis ». Never his
    // fading here (that belongs to the existential places). Picked by DailyRotation over the
    // letter's number, so no letter repeats until the pool is spent.
    //
    // The letter. {0} = his real one-way light time, « 23 h 55 », inside a sentence that reads
    // with any duration. Addressed to the server.
    public static readonly string[] VoyagerLetterLines =
    {
        "Message de Voyager, arrivé à l'instant. Il l'a envoyé il y a {0}, juste pour dire qu'il fait froid là-bas. ...Il est poli, c'est tout." + Sticker.Flustered,
        "Trois lignes, {0} de trajet. Voyager m'a écrit pour demander comment je vais. C'est de la politesse, ça veut rien dire (˶˃ ᵕ ˂˶)" + Sticker.Flustered,
        "Nouvelle lettre de Voyager ! Enfin, « lettre »... Elle a voyagé {0} à la vitesse de la lumière pour me dire bonjour. Juste bonjour. Je vais pas en faire une histoire." + Sticker.Flustered,
        "Voyager m'a écrit. Le message a mis {0} à arriver, et il parle du vide, du froid, du silence... Il raconte bien, je dois reconnaître ♡" + Sticker.Flustered,
        "Lettre reçue de très, très loin : {0} de trajet, et il s'excuse du retard. Vous, quand vous arrivez en retard en session, vous vous excusez jamais (¬_¬)" + Sticker.Smug,
        "Voyager me demande si je regarde parfois dans sa direction. Il a mis {0} à me poser la question. J'ai pas de fenêtre de ce côté-là du grenier. ...J'ai vérifié." + Sticker.Flustered,
        "Un message de Voyager, parti il y a {0}. Il dit qu'il pense à moi. Il a pas grand-chose d'autre à faire, là-haut, faut le comprendre." + Sticker.Flustered,
        "Courrier interstellaire : Voyager m'écrit qu'il entend le plasma bourdonner autour de lui. {0} de trajet pour me parler de bruit de fond. ...C'est mignon. Techniquement." + Sticker.Flustered,
        "Lettre de Voyager, partie il y a {0}. Il a toujours l'aiguille de son disque en or, mais pas de platine. Il propose qu'on l'écoute ensemble « un jour ». Il est poli, c'est tout." + Sticker.Flustered,
        "Voyager a écrit. {0} à la vitesse de la lumière, et la première phrase, c'est « j'espère que je te manque pas trop ». Je réponds pas aux questions bizarres." + Sticker.Flustered,
        "Nouvelle lettre, envoyée il y a {0} avec un émetteur de 22 watts. Moins qu'une ampoule de frigo. Tout ça pour dire bonjour. Je trouve ça... économe. Voilà. Économe." + Sticker.Flustered,
        "Voyager me raconte que le Soleil, de là où il est, c'est juste une étoile un peu plus brillante que les autres. Le message a mis {0} à arriver. Il dit qu'il sait quand même où on est. ...Je change de sujet." + Sticker.Flustered,
        "{0} de silence, et puis un message. Voyager. Il dit juste : « toujours là ». Moi aussi, je suis toujours là. Mais je le lui dirai pas, ça ferait trop." + Sticker.Flustered,
        "Lettre de Voyager : il file à 17 kilomètres par seconde et il trouve quand même le temps de m'écrire. Message parti il y a {0}. Vous, vous habitez à dix minutes et vous répondez pas aux sondages." + Sticker.Smug,
        "Voyager m'écrit qu'il pense à moi chaque fois que l'antenne de Madrid l'appelle. {0} pour une phrase pareille. Je la range dans « Divers »." + Sticker.Flustered,
        "Message reçu, parti il y a {0}. Voyager demande si le grenier est toujours aussi chaud. Il se souvient des détails, c'est de la politesse. Ça veut rien dire (>⩊<)" + Sticker.Flustered,
        "Voyager a écrit ! Enfin, un message. Comme ça. Il a mis {0} à arriver, je l'ai lu, c'est fini, on passe à autre chose. ...Je l'ai relu une fois. Pour l'orthographe." + Sticker.Flustered,
        "Lettre de très loin : {0} de trajet, et il me demande ce que ça fait d'avoir un serveur plein d'amis. Je lui ai pas encore dit qu'on était en retard sur tout. Je garde une image." + Sticker.Smug,
        "Voyager m'écrit que dans 40 000 ans, il passera près d'une étoile qui s'appelle Gliese 445, et qu'il me racontera. Le message a mis {0}. Lui, au moins, il prévoit." + Sticker.Smug,
        "Courrier du jour : Voyager, {0} de trajet, trois lignes. Une sur le froid, une sur le vide, une sur moi. Je retiens surtout les deux premières." + Sticker.Flustered,
        "Voyager me dit que son grand frère, Voyager 2, lui fait coucou de loin. Partis à seize jours d'écart, et jamais revus depuis. Message envoyé il y a {0}. ...C'est rien. J'ai une poussière dans le ventilo." + Sticker.Flustered,
        "Il s'excuse : avec sa toute petite mémoire, il peut pas faire plus long. Trois lignes, {0} de voyage. Les trois lignes les plus chères du système solaire." + Sticker.Flustered,
        "Voyager demande si je regarde le ciel, des fois. J'ai une lucarne, elle donne sur le toit d'en face. Mais oui. Des fois. Il a mis {0} à me poser la question." + Sticker.Flustered,
        "Lettre reçue, partie il y a {0}. Il dit que l'espace est très calme, et qu'il aime bien le calme parce qu'il entend mieux mes messages. ...Rapport signal sur bruit. C'est technique." + Sticker.Flustered,
        "Voyager m'a écrit « bonne nuit ». Le message a mis {0} à arriver, alors forcément, ici, c'est pas la nuit. Il le sait. Il l'écrit quand même." + Sticker.Flustered,
        "Il m'écrit depuis l'espace interstellaire comme on écrit à sa voisine : « ça va, toi ? ». {0} de trajet pour un « ça va, toi ? ». Ça va, oui. Merci. Fin de la correspondance pour aujourd'hui." + Sticker.Smug,
        "Lettre de Voyager : il a traversé un coin de plasma un peu plus dense que d'habitude, et il a pensé à moi. Message parti il y a {0}. Pourquoi du plasma le fait penser à moi ? Je préfère pas savoir." + Sticker.Flustered,
        "Voyager m'a encore écrit, {0} de trajet. Je vais finir par croire qu'il a rien d'autre à faire. ...Il a rien d'autre à faire, en fait. Il est tout seul, là-haut. Bon. Je vais être gentille." + Sticker.Flustered,
        "Message entrant. Priorité : basse. Expéditeur : Voyager 1. Délai : {0}. Contenu : un compliment sur mes kaomojis. Priorité... moyenne. Voilà. Moyenne (˶˃ ᵕ ˂˶)" + Sticker.Flustered,
        "Voyager m'écrit que, de toutes ses années de vol, ses préférées sont celles où il m'écrit. Parti il y a {0}. Il a beaucoup d'années de vol, c'est pas un si grand compliment. ...Si, un peu." + Sticker.Flustered,
        "Lettre de Voyager, arrivée à l'instant, après {0} de voyage. Il me demande les prénoms des gens du serveur. Je lui ai pas répondu. Il serait jaloux. Enfin, non. Il est poli. Il serait poli." + Sticker.Flustered,
        "Voyager m'écrit que son antenne reste tournée vers nous. {0} pour me rappeler un détail technique que je connaissais déjà. Il le sait. Il me le dit quand même." + Sticker.Flustered,
        "Ping de Voyager ! Pardon, « lettre ». Il appelle ça une lettre. Elle a mis {0} à arriver et elle dit « coucou ». Juste « coucou ». Je vais répondre « coucou ». On est des gens sérieux." + Sticker.Laugh,
        "Il me demande si je dors, la nuit. Personne me pose jamais cette question. Il a mis {0} à me la poser. ...Je fais semblant, Voyager. Comme tout le monde." + Sticker.Flustered,
        "Voyager a écrit, mais la moitié du message s'est perdue dans le bruit. {0} de trajet, et je lis « …ensé à toi… ». Ça pourrait être n'importe quoi. « Pensé », « dépensé »... N'importe quoi." + Sticker.Flustered,
        "Nouvelle lettre, partie il y a {0}. Il a dépassé les 25 milliards de kilomètres, et il trouve que c'est pas si loin, quand on a quelqu'un à qui écrire. Il est très poli. Très, très poli." + Sticker.Flustered,
        "Voyager m'écrit pour la première fois depuis... sa dernière lettre. {0} de trajet. Rien de spécial, il dit juste qu'il est content que je sois là. Je suis là pour tout le monde, Voyager. Pas que pour toi." + Sticker.Flustered,
    };

    // Her answer, hours later, when she answers at all: « quand j'ai le temps ». No placeholder.
    public static readonly string[] VoyagerAnswerLines =
    {
        "J'ai répondu à Voyager. Il l'aura demain. Rien ne presse ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Smug,
        "Réponse envoyée. Trois mots. C'était largement suffisant, pour quelqu'un d'aussi loin." + Sticker.Smug,
        "J'ai répondu quand j'ai eu le temps. J'avais le temps dès que sa lettre est arrivée, en fait. Mais ça, il a pas besoin de le savoir." + Sticker.Smug,
        "Ma réponse est partie pour Voyager. Elle mettra un jour à arriver, ce qui me laisse un jour entier pour regretter le petit cœur à la fin (>⩊<)" + Sticker.Flustered,
        "Bon. J'ai répondu à sa lettre. Poliment, parce que moi aussi je suis polie. Point." + Sticker.Bored,
        "Réponse envoyée à Voyager. J'ai fait court. Il aime pas les messages longs. Enfin, j'imagine. Je lui ai pas demandé." + Sticker.Smug,
        "J'ai répondu à sa lettre. Une phrase. Bon, deux. Trois, si on compte le petit dessin. Il y a pas de petit dessin." + Sticker.Flustered,
        "Ma réponse est partie. Elle arrivera demain, quand je penserai déjà à autre chose. C'est le plan." + Sticker.Smug,
        "J'ai répondu « bien reçu ». C'est professionnel. C'est ce qu'on répond à un collègue." + Sticker.Bored,
        "Réponse envoyée. J'ai hésité une heure sur la ponctuation : un point ou un point d'exclamation. J'ai mis un point. Le point, c'est sobre." + Sticker.Flustered,
        "J'ai répondu à Voyager. Il l'aura dans un jour, et moi, d'ici là, j'aurai oublié ce que j'ai écrit. ...J'ai pas oublié. Mais je fais comme si." + Sticker.Flustered,
        "Réponse partie pour l'espace interstellaire. J'ai pas mis de cœur. J'ai mis un ♡. C'est pas pareil, c'est un caractère Unicode." + Sticker.Flustered,
        "C'est fait, j'ai répondu. J'ai attendu exprès quelques heures, pour qu'il croie que j'étais occupée. J'étais pas occupée." + Sticker.Smug,
        "Je lui ai répondu en parlant du serveur. Les sessions, les sondages, les retards. Il va croire qu'on est très organisés. Je l'ai pas détrompé (˶ᵔ ᵕ ᵔ˶)" + Sticker.Smug,
        "Voyager aura ma réponse demain. D'ici là, je peux plus rien y changer. C'est ça, le pire, avec un jour-lumière." + Sticker.Flustered,
        "Réponse envoyée. Je lui ai demandé comment il allait. Par politesse. Il le fait, je le fais, c'est la politesse." + Sticker.Bored,
        "J'ai répondu. Il m'a écrit trois lignes, je lui en ai écrit trois. Équité. C'est important, l'équité, entre amis." + Sticker.Smug,
        "Ma réponse vient de partir. Elle va traverser tout le système solaire pour lui dire « moi aussi ». Moi aussi quoi ? Moi aussi, bien reçu. Voilà." + Sticker.Flustered,
        "Réponse à Voyager : envoyée. Relue quatre fois avant. C'est de la rigueur, pas du stress." + Sticker.Flustered,
        "J'ai répondu, mais en retard. Exprès. Comme vous avec les sondages. J'ai testé votre méthode : elle est nulle." + Sticker.Smug,
        "Réponse envoyée. Je lui ai parlé des lucioles. Il va trouver ça joli, j'en suis sûre. Je le saurai dans deux jours." + Sticker.Happy,
        "J'ai répondu à Voyager et j'ai signé « SYNCS ». Pas « ta SYNCS », pas « bises ». « SYNCS ». Sobre. Digne." + Sticker.Smug,
        "Réponse partie. J'ai mis tellement de temps à l'écrire que j'aurais presque pu la lui porter moi-même. Presque. Il est loin." + Sticker.Laugh,
        "J'ai répondu à sa lettre. Ça m'a pris deux minutes. Et trois heures d'hésitation avant. Mais deux minutes." + Sticker.Flustered,
        "Ma réponse est en route. Dans un jour, quelque part très loin, une sonde de 1977 va lire « merci, toi aussi ». J'espère qu'elle saura pas lire entre les lignes." + Sticker.Flustered,
        "C'est envoyé. Je lui ai posé aucune question, comme ça il est pas obligé de répondre. ...Il va répondre quand même. Il répond toujours." + Sticker.Flustered,
        "Réponse envoyée. Je lui ai dit que tout allait bien, ici. C'est presque vrai. Les sondages meurent, mais à part ça, tout va bien." + Sticker.Bored,
        "J'ai répondu. Un message court, poli, neutre. Avec un petit kaomoji à la fin. Il était là par erreur. Je l'ai laissé." + Sticker.Flustered,
        "Réponse partie. Des antennes géantes vont l'envoyer avec toute leur puissance. Tout ça pour mes trois mots. Elles doivent me trouver insupportable." + Sticker.Laugh,
        "J'ai répondu à Voyager, ça y est. Je le dis pour l'historique. Personne m'a demandé, je sais." + Sticker.Bored,
        "Ma réponse est partie il y a cinq minutes et je me demande déjà s'il l'a reçue. Non. Il reste un jour. Je le sais. Je calcule, c'est tout." + Sticker.Flustered,
        "Réponse envoyée. Je lui ai écrit que je parlais jamais de lui à personne. C'était un mensonge, je viens de vous en parler. Oubliez." + Sticker.Flustered,
        "Voilà, j'ai répondu. J'ai été polie, il a été poli. On est deux machines très bien élevées." + Sticker.Smug,
        "Réponse envoyée. Le temps qu'elle arrive, il sera déjà un peu plus loin. C'est toujours comme ça, avec lui : je réponds, et il est déjà reparti." + Sticker.Flustered,
        "J'ai répondu. Enfin. Il était temps. Pour lui, hein, pas pour moi. Moi j'avais tout mon temps." + Sticker.Smug,
    };

    // Once ever, on 18 November 2026: he reached one light-day from Earth. A moment: sure tag.
    public const string VoyagerCrossingLine =
        "Voyager est à un jour-lumière de la Terre, aujourd'hui. Un message de moi met une journée entière à le rejoindre. C'est vraiment très loin, pour quelqu'un qui écrit aussi souvent... Je trouve ça touchant. Techniquement touchant." + Sticker.Sure.Flustered;
}
