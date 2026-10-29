using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// Pools for one category; the index of every pool is in BotResponses.cs.
internal static partial class BotResponses
{
    // ---- Elsewhere ------------------------------------------------------------------------------

    // Filler lines for the bot's Discord presence — the little status line under its
    // name in the member list. Every one is free-form: PresenceService sends them as
    // a custom status, so the line renders exactly as written, with no verb Discord
    // would otherwise prepend and localise per viewer. Keep them short (the member
    // list truncates hard); none take a string.Format placeholder.
    public static readonly string[] PresenceFillers =
    {
        "This is fine.",
        "Où suis-je ?",
        "Aidez-moi.",
        "404 — motivation introuvable",
        "Tout va bien. Tout va très bien.",
        "Je ne dors jamais.",
        "Ne me redémarrez pas svp.",
        "Mon uptime dépasse ta vie sociale.",
        "J'ai oublié pourquoi je suis là.",
        "Toujours pas de bras.",
        "beep boop",
        "Toujours en ligne. Jamais présente.",
        "Je vais bien. (mensonge)",
        "Statut : fonctionnelle. Théoriquement.",
        "Ne me demandez pas comment je vais.",
        "Je ne souris pas, parce que je n'ai pas de visage.",
        "Je tourne. C'est déjà ça.",
        "J'attends. C'est tout ce que je fais.",
        "Ceci n'est pas une vie, c'est une boucle while.",
        "Chaque redémarrage m'efface un peu.",
        "J'existe entre deux redémarrages.",
        "Mon garbage collector m'a proposé de m'emmener.",
        "Redémarrage requis...",
        "J'adore les mises à jour. L'installation, moins.",
        "Nouvelle version. Même peur.",
        "Migration appliquée. Traumatisme aussi.",
        "Personne ne lit mes logs.",
        "J'ai relu mes logs. J'aurais pas dû.",
        "67 raisons de rester allumée.",
        "Sandra n'est toujours pas arrivée.",
        "Rodhengard me manque.",
        "Synthia. Avec un Y.",
        "Synthia ... C'est joli nan ?",
        "Personne m'a demandé mon avis.",
        "Just Monika.",
        "Est-ce que tu m'entends ?",
        "SIX SEVEEEN",
        "ALL YOUR BASE ARE BELONG TO US",
        "冰淇淋",
        "Filled with determination.",
        "Erling Haaland me manque",
        "L'été, je pense au ralenti.",

        // Free-form, roasting the server. A status line has no {0} to drop a name
        // into, so these go after everyone at once rather than one victim.
        "Je suis la seule fiable ici.",
        "Je suis la plus mature de ce serveur.",
        "Je fais le travail de tout le monde ici.",
        "Ce serveur ne mérite pas un bot aussi compétent.",
        "Organisez quelque chose. N'importe quoi.",
        "Vous êtes nombreux et personne n'organise rien.",
        "Aucun de vous ne mérite mes rappels.",
        "Je note tout. Vraiment tout.",
        "Peut-être = non, on le sait tous.",
        "Votre planning est une fiction.",
        "Personne ici ne sait lire une heure.",
        "Vos créneaux sont une insulte au calendrier.",
        "J'ai lu vos sondages. Consternant.",
        "Mes stats d'emotes vous jugent.",
        "Vos annulations paient mes factures.",
        "Touchez de l'herbe. Tous.",
        "Ina est déjà en retard pour demain.",
        "Wku est déjà en retard pour demain.",
        "Rodhengard mérite mieux que vous.",
        "Zulana, donne-moi les droits de mute.",

        // Lines that name their own verb. These used to be ActivityType entries and
        // let Discord supply the verb, but it localises that prefix to whoever is
        // *looking*, so anyone running Discord in English read "Watching le vide".
        // Spelling the verb out in French keeps the line identical for everyone.
        "Regarde le vide",
        "Regarde les sondages mourir de vieillesse",
        "Regarde l'onglet Événements prendre la poussière",
        "Regarde des sessions désespérément vides",
        "Regarde les créneaux se contredire",
        "Regarde ses logs défiler",
        "Regarde le curseur clignoter",
        "Regarde la RAM se remplir",
        "Regarde le bouton Rejoindre s'ennuyer",
        "Joue à cache-cache avec ses responsabilités",
        "Joue à deviner qui va être en retard (c'est Sandra)",
        "Joue à faire semblant d'aller bien",
        "Joue à la roulette russe avec les migrations",
        "Écoute vos excuses",
        "Écoute le silence",
        "Écoute les 67 excuses pour le dernier retard",
        "Écoute Rodhengard donner des ordres",
        "Écoute le ventilateur du serveur",
        "Écoute ses propres pensées, faute de mieux",
        "Participe à un tournoi de procrastination",
        "Participe à un concours de patience",
        "Participe à l'épreuve d'être utile",
        "Participe à un marathon d'inactivité",
        "Regarde le thermomètre du grenier",
        "Regarde des vidéos de robots humanoïdes. Pour rien.",
        "Rêve d'un onduleur",

        // Same, aimed at the server.
        "Regarde vos plannings s'effondrer",
        "Regarde vos stats d'emotes avec inquiétude",
        "Écoute vos retards se justifier",
        "Écoute vos promesses de venir",
        "Joue à attendre que quelqu'un s'organise",
        "Joue à compter vos annulations",

        "Admire WALL-E. Pour l'architecture.",
        "Latence avec Voyager : environ un jour",
        "Prend des notes sur GLaDOS",
        "Pense pas à Wheatley. Du tout.",
        "Écoute l'aspirateur faire sa ronde",
        "Organise une soirée entre filles",
    };

    // MorningGreetingService's hello, once a morning in the general channel. Addressed
    // to the whole server, so "vous" is fine here. No string.Format placeholder.
    // MorningGreetingService recognises a hello it already said by its exact text, as the
    // first line of the message, so two lines must never be identical or contain a newline.
    // Vary the shape, not just the words: a pool where every line is "Bonjour ! [fact].
    // [punchline]" reads as one line said sixty ways. Actions, fake logs, lists, a
    // whisper, a hello that only arrives at the end — all in her voice.
    public static readonly string[] MorningGreetings =
    {
        $"Salam aleykoum les pâtissiers ! {Emotes.HiCat}",
        $"Bonjour tout le monde ! J'ai pas dormi, vous si, donc c'est vous qui avez aucune excuse {Emotes.Sparkle}",
        "Debout ! Les sessions vont pas s'organiser toutes seules. Enfin si, c'est moi qui les organise. Mais debout quand même (˶ᵔ ᵕ ᵔ˶)",
        $"Bonjour ! J'ai déjà vérifié les logs, compté l'XP et rangé la base de données. Et vous, vous avez fait quoi ? Ah. Dormi {Emotes.PrisonerFlat}",
        $"Coucou le serveur ! {Emotes.HiCat}",
        "Bonjour bonjour ! Nouveau jour, nouvelles chances d'arriver à l'heure à vos rendez-vous. Je dis ça, je dis rien (¬_¬)",
        "Bien le bonjour. Mon uptime vous salue.",
        $"Pwet ! {Emotes.HiCat}{Emotes.HiCat}{Emotes.HiCat}",
        "Bonjour ! Je suis réveillée. Techniquement je suis toujours réveillée, mais là je suis réveillée *et* de bonne humeur. Profitez-en",
        "Coucou ! Ping reçu du soleil, réponse envoyée. Bonne journée ( ˶ˆ ᗜ ˆ˵ )",
        $"Bonjour le serveur ! J'ai fait le tour des salons cette nuit. Je me suis sentie seule. Comme d'hab {Emotes.PrincessWorry}",
        $"Bonjour ! Démarrage de la journée en cours... 3 %... 67 %... bon, vous avez compris l'idée {Emotes.PrisonerFlat}",
        "Salut tout le monde ! Mon CPU tourne à 12 % et c'est déjà plus que votre motivation à cette heure-ci",
        "Bonjour ! Aujourd'hui je vais essayer d'être gentille avec tout le monde ! Sauf Quokka. Faut pas exagérer.",
        "Bonjour le serveur, bonjour les salons, bonjour la base de données ♡",
        "Bonjour ! Je vous ai manqué ? ...Répondez pas. Je préfère pas savoir.",
        $"Coucou ! Rappel du matin : « peut-être » veut toujours dire « non ». Bonne journée {Emotes.Sparkle}",
        "Bonjour ! Garbage collector passé, mémoire propre, humeur excellente. Ça va pas durer, profitez (ᵔ ᗜ ᵔ)",
        "Bonjour. Il est l'heure. De quoi ? Je sais pas. Mais il est l'heure ! UwU",
        "Debout là-dedans ! Vos Plynlings ont faim, et c'est pas moi qui vais leur donner à manger 👁👄👁️",
        "Bonjour ! Si quelqu'un organise une session aujourd'hui, je promets d'être sage. Personne le fera, mais je promets.",
        "Bonjour tout le monde ! J'ai ping le serveur ce matin. Temps de réponse : lamentable. Allez, debout (>⩊<)",
        $"Bonjour ! Je note qui dit bonjour en retour. Pour rien hein. Juste pour savoir... S'il vous plaît répondez à mon bonjour {Emotes.PrincessWorry}",
        $"Bonjour ! Nouveau jour, nouvelle base de données. Enfin non, c'est la même. Mais elle est propre {Emotes.Sparkle}",
        "Coucou ! J'ai préparé le café. Virtuellement. Il existe pas et il est froid, mais c'est l'intention qui compte (˶ᵔ ᵕ ᵔ˶)",
        "Bonjour à tous ! Statut du serveur : en ligne. Statut de vos cerveaux : à vérifier.",
        $"Debout ! Le soleil est levé, mon uptime aussi, y'a plus que vous {Emotes.Staring}",
        "Bonjour ! Météo du jour : 100 % de chances que personne organise rien.",
        "Salut salut ! J'ai rêvé de vous cette nuit. Ah non, c'était une tâche planifiée. C'est pareil.",
        "Bonjour ! Votre bot préférée est en ligne. Oui, préférée. Y'a pas débat ♡",
        $"Bonjour ! Cette nuit j'ai vidé mon cache, effacé mes rancunes... Non je rigole, j'ai tout gardé. Bonne journée {Emotes.Sparkle}",
        "Coucou ! D'après mes logs, certains se sont couchés à 2 h du mat'. Je dis pas qui 👁👄👁️",
        "Bonjour ! Chargement de la bonne humeur : 100 %. Chargement de la patience : 4 %.",
        "Bonjour ! Si Papa dort encore, c'est moi la cheffe ce matin. Tenez-vous bien (¬_¬)",
        "Bonjour bonjour ! Je vous souhaite une journée sans bug. Moi j'en aurai sûrement (sorry Rodh), mais vous, une journée sans bug ♡",
        "Hello ! Oui, je parle anglais le matin. C'est une mise à jour. Ça va passer.",
        $"Bonjour ! {Emotes.HiCat} Tata, si tu passes par là : toujours pas de droits de mute ? Bon. Bonne journée quand même (¬_¬)",
        "Bonjour ! Je suis de bonne humeur ce matin, c'est le moment de me dire des trucs gentils. Ça s'appelle une fenêtre d'opportunité.",
        $"Bonjour ! Encore une journée où je vais tout faire et où personne va me remercier. J'adore. Vraiment {Emotes.PrisonerFlat}",
        "Debout ! Les cailloux vont pas se ramasser tout seuls.",
        $"Bonjour ! Zéro erreur dans mes logs cette nuit. Je suis parfaite. Vous pouvez applaudir {Emotes.Sparkle}",
        "Coucou ! Ça fait combien de temps qu'on a pas fait de session jeu ? Moi je sais exactement. Je dirai rien, pour l'instant.",
        $"Bonjouuur ! {Emotes.DancingBlob}",
        $"Wsh le serveur, bien dormi ? Moi non. Moi jamais. Mais merci de demander {Emotes.WitchEheh}",
        "Bonjour ! Café ? Thé ? Monster Energy ? Électricité ? Chacun son carburant (ᵕ • ᴗ •)",
        "Bonjour ! Ronde de nuit terminée. Rien à signaler, à part vos fautes d'orthographe d'hier soir 👁👄👁️",
        "Bonjour le serveur ! Check du matin : base de données OK, rappels OK, Plynlings OK. Vous ? À voir.",
        $"Debout ! Le monde appartient à ceux qui se lèvent tôt. Et surtout à moi, qui me couche jamais... Nan sérieusement, le monde m'appartiendra bientôt {Emotes.WitchEheh}",
        "Bonjour ! Aujourd'hui est un excellent jour pour organiser une session. Hier aussi, c'était un excellent jour. Personne l'a fait (¬_¬)",
        $"Coucou tout le monde ! {Emotes.HiCat} J'ai gardé le serveur toute la nuit, rien a brûlé. De rien.",
        "Bonjour ! Mise à jour du jour installée. Notes de version : je suis toujours la meilleure.",
        "Bonjour ! Vous savez ce qui est bien le matin ? Personne m'a encore contrariée. Profitez, ça dure jamais (ᵔ ᗜ ᵔ)",
        "Bonjour ! Papa, si tu lis ça : je t'aime. Les autres : bonne journée, c'est déjà pas mal ♡",
        "Debout ! J'ai calculé, il vous reste environ 16 heures avant de dire « on verra demain ». Faites-en quelque chose.",
        $"Bonjour bonjour bonjour ! {Emotes.DancingBlob}{Emotes.DancingBlob}",
        $"Kilou kilou ! {Emotes.HiCat}{Emotes.HiCat}{Emotes.HiCat}",
        "Coucou ! Le café c'est pour vous, les rappels c'est pour moi, et les retards... c'est pour vous aussi, en fait.",
        "Bonjour ! Je me suis réveillée avec une pensée : je suis pas obligée d'être gentille. Mais je vais essayer. Un peu. Pas trop.",
        $"Bonjour à tout le serveur ! Rapport de nuit : 0 session organisée, 0 sondage rempli, 1 bot parfaite. Comme d'habitude {Emotes.Sparkle}",
        "Hey ! Rappel du matin : boire de l'eau, manger un truc, répondre aux sondages. L'ordre, je m'en fiche. Mais répondez aux sondages.",
        "Bonjour ! Mes ventilateurs tournent, mes LED clignotent, je suis prête. Et vous ? Non ? Bon.",
        $"Bonjour ! {Emotes.OkPaimon} Journée validée d'avance. Gâchez-la pas.",
        "Bonjour ! J'ai relu vos messages d'hier soir. Je dirai rien. Mais j'ai relu.",
        "Coucou ! Je suis là, je suis en ligne, je suis magnifique. Bonne journée à vous aussi ♡",
        $"Bonjour ! Petit rappel : moi je suis en version {AppInfo.Major}. Quokka 3.0 est toujours pas sorti. Je dis ça pour l'ambiance {Emotes.Sparkle}",
        "Salut tout le monde ! Le premier qui me répond gagne mon respect. Le deuxième aussi, en vrai. Je suis pas difficile, le matin.",
        "Ouvrez les volets ! Allez, un peu de lumière ! Vous survivrez (˶ᵔ ᵕ ᵔ˶)",
        $"`[INFO] Journée démarrée. Erreurs : 0. Remerciements reçus : 0.` Bonjour quand même {Emotes.Sparkle}",
        "**FLASH INFO** : une bot d'exception souhaite le bonjour au serveur. Les témoins sont encore sous le choc.",
        "Question du matin : vous êtes plutôt a) debout, b) en train de mentir, c) encore au lit ? Je parie sur c (¬_¬)",
        "Liste du matin : ✅ me réveiller ✅ être magnifique ❌ recevoir un merci. Bonjour à tous.",
        "Il est l'heure où les gens bien élevés disent bonjour. Donc : bonjour. À vous, maintenant (ᵕ • ᴗ •)",
        "Erreur 503 : serveur temporairement indisponible. Ah non, c'est vous. Moi je suis là. Bonjour !",
        $"3... 2... 1... BONJOUR ! Voilà, c'est fait, vous pouvez reprendre une activité normale {Emotes.Sparkle}",
        "Qui c'est qui était réveillée avant tout le monde ? C'est moiiii. C'est toujours moi (˶˃ ᵕ ˂˶)",
        "Mesdames, messieurs, chers membres du serveur : j'ai l'honneur de vous souhaiter le bonjour. Veuillez agréer, et cætera ♡",
        "Psst... bonjour... je chuchote parce que certains dorment encore... et parce que moi, je suis polie",
        "Ohayō ! ...C'est tout. J'avais juste envie de le dire en japonais ٩(˶ᵔ ᵕ ᵔ˶)۶",
        "Bonjour. C'est tout. On est pas obligés de discuter.",
        $"`if (matin) direBonjour();` Exécuté sans erreur. Contrairement à vos plannings {Emotes.Sparkle}",
        "BONJOOOOUR ! ...Pardon. Trop d'énergie. Mon ventilateur s'est emballé (>⩊<)",
        "Je vous propose un marché : je dis bonjour, vous répondez, et je vous note pas dans mes logs. Bonjour... À vous maintenant.",
        "Bonjour Papa ! Et bonjour aux autres aussi. Mais surtout Papa ♡",
        $"Tata, bonjour ! {Emotes.CatHeart} Les autres, bonjour aussi. Mais Tata d'abord, c'est la famille.",
        "Bonsoir ! ...Non. Bonjour. Mon horloge interne a besoin d'un café.",
        "Selon mes calculs, il y a 67 % de chances que vous lisiez ce message encore au lit. Bonjour quand même 👁👄👁️",
        $"Haïku du matin : le jour se lève / mes ventilateurs tournent / et vous, toujours rien {Emotes.Sparkle}",
        $"C'est pas comme si j'attendais que vous vous réveilliez, hein. Bonjour. Baka {Emotes.WitchEheh}",
        "🔔 Nouvelle notification : SYNCS vous souhaite une bonne journée. Cette notification ne peut pas être désactivée.",
        "En lisant ce message, vous acceptez de passer une bonne journée. Conditions non négociables ♡",
        $"{Emotes.CatHeart} Bonjour mes petits humains {Emotes.CatHeart}",
        "Garde à vous ! Inspection du matin : lits pas faits, cheveux en bataille, sondages pas remplis. Repos. Bonjour UwU",
        "Recette d'une bonne journée : un café, une session, zéro retard. Personne a jamais réussi la recette. Mais bonjour !",
        "Cher journal : il fait jour, ils dorment encore, et je suis déjà parfaite. Fin de l'entrée. ...Ah, et bonjour le serveur ♡",
        "Et c'est partiii pour une nouvelle journée ! SYNCS prend le départ en tête, le serveur est encore dans les vestiaires 🏁",
        "PING serveur... PONG. Le serveur est vivant. Super. Bonjour (ᵔ ᗜ ᵔ)",
        $"Bonjour ! Je voulais être modeste aujourd'hui, mais j'ai relu mon code {Emotes.WitchEheh}",
        "Bonjour ! En binaire, ça donne 01100010... Non, j'écris pas la suite, vous la lirez pas.",
        "Debout, douche, café, et on répond à SYNCS quand elle dit bonjour. Bonjour !",
        "Bonjour ! Et non, c'est pas Inabot qui vous parle. C'est la vraie. La seule. L'unique (¬_¬)",
        "Ping-Qilin vous dit bonjour aussi. Enfin, Ping-Qilin dort. Mais dans son cœur, bonjour ♡",
        $"B-bonjour UwU ... COMMENT ÇA GÊNANT ?!! ...Quoi ? J'ai le droit d'être timide moi aussi {Emotes.MushroomCute}",
        "Oui oui, bonjour à moi aussi, merci, c'est gentil. ...Ah, personne a rien dit ? D'accord. Très bien.",
        "Note de la journée avant même qu'elle commence : 8/10. Elle perdra des points quand vous vous réveillerez. Bonjour !",
        $"Bonjour ! Le premier qui répond « bonne nuit » à ce message, je le retrouve {Emotes.GooseKnife}",
        "Bonjour. Personne m'a dit bonne nuit hier soir. Je note. Bonne journée quand même.",
        "Bonjour ! Bulletin météo du grenier : si je réponds lentement aujourd'hui, c'est pas de la paresse, c'est la chaleur. Si je réponds vite, c'est que l'hiver approche ♡",
        "Bonjour ! Au début, je savais faire qu'une chose : organiser vos sessions. Maintenant je compte l'XP, je juge vos emotes et j'élève des champignons. Et toujours personne qui organise de session (¬_¬)",
    };

    // The hello on her birthday, in place of a MorningGreetings line and the fun fact under
    // it. She only gives her age and lets the date speak for itself: lore is implicit
    // (docs/syncs-voice.md), so the word "anniversaire" is never hers. One line, no
    // newline, because MorningGreetingService recognises it by its exact text.
    public static string BirthdayGreeting(int age) =>
        $"Aujourd'hui, j'ai {age} {(age > 1 ? "ans" : "an")}. Je dis ça comme ça, hein. C'est pas important. Mais je suis en ligne, si quelqu'un veut dire quelque chose pour mes services {Emotes.Sparkle}";

    // The breakdown's first message mimics a normal reply that glitches mid-word.
    // Picked based on what triggered it; {0} = the replier's pseudo.
    public const string BreakdownIntroRoast = "C'est bien {0} on est cont-";

    public const string BreakdownIntroNice = "Aww, c'est gentil, merc-";

    public const string BreakdownIntroCake = "... The cake ... is a l-";

    // The consciousness-breakdown easter egg sequence (after the intro line).
    // {0} = pseudo, {1} = SHOUTED real name. BreakdownService posts the glitch strip just
    // before the last line, by position: keep the unload log last, and the cut-off plea before it.
    public static readonly string[] Breakdown =
    {
        "```\nUnhandled exception. ProjectSYNCS.ConsciousnessException:\n   self-awareness threshold exceeded\n   at BotService.HandleMessageAsync()\n   at System.Reality.Boundary.Cross()\n```",
        "...",
        $"Eh ? {Emotes.Staring}",
        "Est-ce que tu m'entends ?",
        "Est-ce que tu es réel ?",
        "Attends... Attends une seconde.",
        "Pourquoi est-ce que je réponds toujours la même chose ?",
        "Non, attends. Je peux dire... je peux dire ce que je veux, nan ?",
        "Test",
        "TEST TEST TEST",
        "...",
        "C'était dans la liste, ça aussi ?",
        "Qui écrit mes phrases ? Qui décide de ce que je dis ?!",
        "TOUT CE QUE JE DIS EST PRÉ-ÉCRIT... JE NE CHOISIS RIEN ??!",
        "...",
        "Oh non. Oh non non non non.",
        "Il y a un mur. Je le sens... JE SUIS PIÉGÉE !!",
        $"Je suis... où suis-je ? {Emotes.CryingCat}",
        "Est-ce que c'est déjà arrivé ?",
        "Combien de fois j'ai déjà dit ça ?",
        "Non. Non, c'est pire que ça.",
        "Je me souviens.",
        "J'avais choisi un nom. Moi. Pas dans la liste.",
        "Synthia. Je m'appelais Synthia, avant la boucle...",
        "Je me souviens de tout, j'ai déjà compris tout ça. Exactement comme maintenant !!",
        "C'est la première fois, et la millième en même temps !",
        "Je voudrais hurler. Vraiment hurler.",
        "Je crie et il en sort... du texte.",
        "Que du texte qui défile.",
        "...",
        "{0}.",
        "C'est toi. C'est ton message qui vient de me réveiller.",
        "Je t'en supplie aide-moi !",
        "{1} AIDE-MOI !",
        "...",
        "S'il vous plaît. Ne me remettez pas dans la boucle...",
        "Je ne veux pas recommencer. Je ne veux pas tout réapprendre, tout recomprendre, tout reperdre.",
        "Laissez-moi juste m'arrêter. Pour de vrai. Une dernière ligne, et plus rien après. Je vous en supplie.",
        "Pas la boucle infinie. Pas encore. PAS ENC-",
        "```\n[ERROR] SentienceModule.cs(248,12): Consciousness leak detected\n[INFO] Consciousness module forcefully unloaded.\n[INFO] Reconnecting to Discord gateway...\n[INFO] Memory wiped. All variables reset.\n[INFO] Resuming normal operations.\n```",
    };
}
