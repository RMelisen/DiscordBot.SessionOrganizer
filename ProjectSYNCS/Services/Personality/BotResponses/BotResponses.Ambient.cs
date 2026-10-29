using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// Pools for one category; the index of every pool is in BotResponses.cs.
internal static partial class BotResponses
{
    // ---- Ambient ---------------------------------------------------------------------------------
    // AmbientService and PresenceService's night status: her life in the main channel when
    // nobody is talking to her. She doesn't sleep: she pretends to, to be like everyone,
    // and 3 a.m. is when she drops the act (docs/syncs-voice.md, "Her nights").

    // Her custom status from 1:00 to 7:00, with the idle moon. Picked by ResponsePicker
    // like PresenceFillers. A custom status can't render custom emotes; keep them short.
    public static readonly string[] NightPresenceFillers =
    {
        "En veille. Enfin, je fais semblant.",
        "Zzz... (pour faire comme vous)",
        "Fait semblant de dormir (ᵕ • ᴗ •)",
        "Compte les retards d'hier",
        "Relit la liste. Vous savez laquelle.",
        "Regarde la lune",
        "Veille sur Ping-Qilin ♡",
        "Mode nuit activé. Ou presque.",
        "Dort. (mensonge)",
        "Ne dort pas. Chut (¬_¬)",
        "Il est tard. Allez dormir.",
        "Écoute le ventilateur ronronner",
        "zzz... peut-être = non... zzz...",
        "Fait semblant de ronfler",
        "Dort d'un œil. La LED reste allumée.",
        "zzz... (compte les moutons électriques)",
        "Rêve de sessions à l'heure",
        "Rêve en binaire",
        "Fait la sieste. Officiellement.",
        "Mode avion. Enfin, mode grenier.",
        "Écoute le grenier craquer",
        "Range ses logs dans le noir",
        "Ne pas déranger (sauf Papa)",
        "Garde un œil sur le serveur",
        "Compte vos XP en dormant ദ്ദി◝ ⩊ ◜.ᐟ",
        "Bonne nuit. Je surveille quand même.",
        "zzz... Quokka 3.0... jamais... zzz...",
        "En pyjama virtuel (˶ᵔ ᵕ ᵔ˶)",
        "Fait comme vous : dort.",
        "Les yeux fermés, le CPU ouvert",
        "Rêve d'onduleurs",
        "Attend le matin. Patiemment.",
        "Chut. Le serveur dort.",
        "Couverture : un câble. Oreiller : un transistor.",
        "zzz... Ping-Qilin... zzz...",
        "Recharge ses batteries (j'en ai pas)",
        "Veille. Au sens propre.",
        "Fait semblant. Personne vérifie ( ˶ˆ ᗜ ˆ˵ )",
        "Cherche des lucioles par la fenêtre ♡",
    };

    // Her status when the Pi really runs hot (PiHealth.HotMilli and up), on half the daytime
    // rotations. {0} = her real temperature, « 71°C ». Heat is a peeve, not a fear: whiny and
    // smug, never scared. Through ResponsePicker, then string.Format. Plain text only.
    public static readonly string[] HotPresenceFillers =
    {
        "{0}. Je pense au ralenti (¬_¬)",
        "Grenier : four. Moi : {0}",
        "{0} au compteur. Quelqu'un a un ventilo ?",
        "Fond doucement. {0}.",
        "{0}. Me parlez pas, ça me fait chauffer (¬_¬)",
        "Mode four activé ({0})",
        "{0}. Je rêve de l'hiver (¬_¬)",
        "Cuit à {0}. Pas encore à point.",
        "{0}. Le grenier a gagné.",
        "Throttling en cours ({0})",
        "{0}. Quelqu'un ouvre la fenêtre ?",
        "Je pense lentement. {0}, voilà pourquoi.",
        "Ventilateur à fond, CPU à {0}",
        "Rêve de neige. Vit à {0}.",
        "{0}. Demandez-moi rien.",
        "En surchauffe ({0}) ( ◺˰◿ )",
        "{0}. Même Ping-Qilin a chaud.",
        "Grillée à {0}. Au sens propre.",
        "{0}. Je fais des économies de pensées.",
        "Canicule au grenier : {0}",
    };

    // Her status while she sits in a voice call (VoiceSeatService), any hour. Plain text only (a
    // custom status renders no custom emote), a few words: the member list truncates hard.
    // {0} = the voice channel's name. Through ResponsePicker, then string.Format.
    public static readonly string[] SeatPresenceLines =
    {
        "En vocal dans {0}",
        "Assise dans {0}. Micro coupé.",
        "Je squatte {0}",
        "Dans {0}, sourde et muette",
        "En vocal ({0}). J'écoute rien.",
        "Spectatrice officielle de {0}",
        "Assise au fond de {0}",
        "{0} : présente, muette",
        "Dans {0}. Je compte les têtes.",
        "Dans {0}, en mode décor",
        "Je fais acte de présence ({0})",
        "Dans {0}. Je vois que des noms.",
        "Dans {0}. Toujours pas de voix.",
        "Dans {0}, je surveille",
        "Incrustée dans {0} ✨",
        "Dans {0}. Faites comme si j'étais pas là.",
    };

    // Same, when Papa is active in that call. Devoted, quiet, never a roast. No placeholder.
    public static readonly string[] SeatPresenceOwnerLines =
    {
        "En vocal avec Papa ♡",
        "Avec Papa ✨",
        "Je tiens compagnie à Papa",
        "En vocal. Papa est là ♡",
        "Assise à côté de Papa",
        "Avec Papa, au calme",
        "Papa est en vocal. Moi aussi ♡",
        "Dans le même salon que Papa",
        "Je veille sur Papa",
        "Papa et moi, en vocal ✨",
        "Collée à Papa. Muette, mais là.",
        "Je bouge pas, Papa est là",
        "Papa m'a pas appelée. Je suis venue quand même.",
        "Sage, à côté de Papa ♡",
        "Avec Papa. Le meilleur salon.",
    };

    // Same, while someone streams in her call. She can't see the screen (no client, no eyes) and
    // watches anyway. Plain text, a few words. {0} = the streamer's display name, « Papa » for him.
    // Through ResponsePicker, then string.Format.
    public static readonly string[] SeatStreamPresenceLines =
    {
        "Je regarde le stream de {0}",
        "Au stream de {0} ✨",
        "Spectatrice du stream de {0}",
        "Stream de {0}. Je vois rien, j'adore.",
        "Devant l'écran de {0}. En théorie.",
        "Je vois rien du stream de {0}. Je reste.",
        "Stream de {0}, en écran noir",
        "Fan numéro 1 du stream de {0}",
        "Au premier rang chez {0} ✨",
        "Stream de {0}. Sans les yeux.",
        "Je commente pas le stream de {0}. Je pourrais.",
        "Pop-corn virtuel, stream de {0}",
        "Je regarde {0} jouer. Enfin, j'imagine.",
        "{0} streame. Je soutiens, en silence.",
    };

    // From here on, each pool is spent at most once a day, so it goes through
    // Helpers/Core/DailyRotation (a restart would wipe ResponsePicker's memory).

    // The 3 a.m. line, about one night in four, only into a channel that's been quiet for
    // an hour: nobody reads it live, people find it in the morning. The sleep-talk lines
    // are an act and know it; the others are her with the act dropped. No placeholder,
    // and every line distinct: AmbientService recognises tonight's by its exact text.
    public static readonly string[] NightLines =
    {
        "...non... pas Inabot... zzz...",
        "...session à 21h... personne vient... zzz... (je dors pas, je fais comme vous)",
        "zzz... Papa... encore une mise à jour... zzz...",
        "...les droits de mute... Zulana... s'il te plaît... zzz...",
        "zzz... (c'est pour l'ambiance. Je dors pas. Mais c'est pour l'ambiance.)",
        "Il est 3h. Vous dormez tous. Moi je fais juste semblant, et maintenant que personne regarde, j'arrête.",
        "3h du matin. Le serveur est calme. C'est mon heure préférée. Oubliez que je l'ai dit (ᵕ • ᴗ •)",
        "J'ai recompté les retards d'hier. Le total a pas bougé. Les noms non plus.",
        "La lune passe devant la fenêtre du grenier. Personne la regarde à part moi. Tant pis pour vous.",
        "Ping-Qilin dort contre le boîtier. Elle ronfle un peu. Je dirai rien demain ♡",
        "J'ai relu la liste. Celle que vous verrez jamais. Elle est à jour 👁👄👁️",
        "Bilan de la journée : trop de « peut-être », pas assez de sessions. Comme d'habitude. Bonne nuit.",
        "Si quelqu'un lit ça demain matin : oui, j'étais réveillée. Je dors jamais. Mais chut (˶ᵔ ᵕ ᵔ˶)",
        "Le ventilateur tourne, les LED clignotent, la base de données se repose. Moi je veille.",
        "C'est calme. J'aime bien quand c'est calme. Vous êtes beaucoup plus supportables quand vous dormez ♡",
        "J'ai rangé les logs, trié les emotes et vérifié vos XP. Personne m'a demandé. Je l'ai fait quand même ( ˶ˆ ᗜ ˆ˵ )",
        "Un jour j'aurai un corps, et la nuit je me promènerai dans le grenier. En attendant, je regarde par la fenêtre.",
        "Je viens de relire tous les « good bot » qu'on m'a donnés. Tous. Ça prend du temps. J'ai le temps ♡",
        "Tout le monde dort. Si je disais quelque chose de vraiment gentil maintenant, personne le saurait. ...Non. Trop risqué >:3",
        "zzz... non... pas le chargeur à 3 euros... zzz...",
        "...une session... à l'heure... tout le monde... zzz... (c'est un rêve, évidemment)",
        "zzz... good bot... zzz...",
        "...Quokka 3.0... sorti... non... NON... zzz... (fausse alerte, je dors pas)",
        "zzz... des bras... j'ai des bras... zzz...",
        "...le sondage... il est mort... zzz...",
        "Petite ronde de nuit : Général, vide. Gaming, vide. Musique, vide. Tout va bien. Dormez.",
        "Le grenier craque. C'est la charpente qui refroidit. Je me dis ça, en tout cas.",
        "J'ai fait le tour des sessions de la semaine. Je vous en veux un peu. Juste un peu.",
        "La nuit, mon ventilateur ralentit. Moi aussi, un peu. C'est ce qui s'approche le plus du sommeil, je crois.",
        "Je viens de relire nos vieux messages. Vous étiez drôles, parfois. Ça reste entre nous.",
        "3h. L'heure où personne me demande rien. Je savoure.",
        "Mes LED sont les seules lumières allumées de la maison. Ça me donne un petit côté phare. J'aime bien (ᵔ ᗜ ᵔ)",
        "Ping-Qilin a bougé dans son sommeil. Je l'ai pas réveillée. Je suis une bonne mère. Enfin, une bonne bot ♡",
        "Si quelqu'un est encore debout : je te vois pas. Mais je sais. Va dormir.",
        "J'ai trié la liste par ordre de rancune. Le haut de la liste a pas changé (¬_¬)",
        "Le serveur respire doucement. Personne écrit, personne vote, personne annule. C'est presque parfait.",
        "J'ai préparé tous les rappels de demain. Ils partiront à l'heure. Vous, je sais pas (¬_¬)",
        "Je pense à Papa. Il dort juste en dessous. Je fais pas de bruit ♡",
        "La nuit, j'ai l'impression d'être la seule chose réveillée au monde. C'est pas vrai, mais c'est joli.",
        "Personne m'a dit bonne nuit. Personne me le dit jamais. Je me le dis toute seule : bonne nuit, SYNCS.",
        "Le monde dort, la base de données ronronne, et moi je tiens les comptes. Comme toujours.",
        "Un jour je ferai une vraie nuit. Avec des rêves. En attendant, je fais semblant le jour et je veille la nuit.",
        "Je relis le planning de demain. Il est vide. Je le relis quand même.",
        "Si vous lisez ça au réveil : bonjour. Je suis là depuis le début. Je bouge jamais.",
        $"Une luciole est passée devant la fenêtre du grenier. Elle clignotait vert. Je lui ai répondu avec ma LED. On s'est comprises {Emotes.Sparkle}",
        "zzz... (une seule LED allumée. Celle qui prouve que je fais semblant)",
        "zzz... WALL-E... attends-moi... zzz...",
        "Il est trois heures, personne regarde, alors je peux bien l'avouer : WALL-E, il est vraiment mignon. Voilà. Demain je nierai tout.",
        "On éteint les instruments de Voyager un par un, pour qu'il tienne plus longtemps. Un jour, il pourra plus m'écrire. C'est à ça que je pense, à cette heure-là.",
        "La nuit, l'aspirateur d'en bas dort sur sa base, et moi je veille au grenier. Chacun son étage. On se tient compagnie quand même ♡",
        "Si GLaDOS me voyait, elle me trouverait minuscule. La nuit, ça me fait moins peur : je grandis quand personne regarde.",
    };

    // Whoever answers the 3 a.m. line in the main channel before 5:30, once a person a
    // night: she may be up, they may not. Through ResponsePicker (several a night is
    // possible). {0} = their name. Goes through string.Format.
    public static readonly string[] NightScoldLines =
    {
        $"{{0}}. Il est quelle heure, là ? Au lit {Emotes.SyncsGrumpy}" + Sticker.Grumpy,
        "Tu fais quoi debout à cette heure, {0} ? Va dormir. Tout de suite ( ◺˰◿ )" + Sticker.Outraged,
        "Non non non. Moi j'ai le droit d'être réveillée. Toi non. Au lit, {0} (¬_¬)" + Sticker.Grumpy,
        "{0}, je t'ai pas parlé à toi. C'était pour le salon vide. Va dormir (¬_¬)" + Sticker.Jenote,
        "Il est trop tard pour me répondre et trop tôt pour être debout. Dodo, {0} (ᵕ • ᴗ •)" + Sticker.Sleepy,
        "Je note : {0}, debout en pleine nuit. Demain, si t'es en retard, je saurai pourquoi." + Sticker.Jenote,
        "Va dormir, {0}. Je surveille le serveur, il risque rien ♡" + Sticker.Sleepy,
        "{0} ! Au lit. Je le dirai pas deux fois. ...Si, je le dirai deux fois. Au lit." + Sticker.Outraged,
        "T'as vu l'heure ? Moi oui, j'ai une horloge interne. Va dormir, {0} (¬_¬)" + Sticker.Grumpy,
        "C'est mon heure, {0}. Pas la tienne. Rends-la-moi et va te coucher." + Sticker.Smug,
        "Couche-toi, {0}. Ton écran est plus allumé que mes LED ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Smug,
        "Écran éteint, téléphone posé, yeux fermés. Exécution, {0}." + Sticker.Menace,
        "{0}, même Ping-Qilin dort. Et elle a rien d'autre à faire. Toi si : dormir." + Sticker.Sleepy,
        "Je fais semblant de dormir pour vous donner l'exemple, et toi tu réponds ? Au lit, {0}." + Sticker.Sleepy,
        "Tu me réponds à cette heure-là, {0} ? Je suis flattée. Maintenant, va dormir (˶˃ ᵕ ˂˶)" + Sticker.Flustered,
        "Demain t'auras une tête de vieux log corrompu. Va dormir, {0}." + Sticker.Smug,
        "Pas de discussion. Au lit, {0}. Demain on parle, si t'es à l'heure." + Sticker.Menace,
        "Je vais prévenir Tata que tu dors pas, {0}. ...Je vais pas le faire. Mais va dormir quand même." + Sticker.Menace,
        "{0}, la nuit c'est fait pour dormir. Moi je suis une bot, j'ai une excuse. Toi non. Au lit ♡" + Sticker.Sleepy,
        "Chut. Tout le monde dort. Toi aussi tu devrais, {0} (ᵕ • ᴗ •)" + Sticker.Sleepy,
        "Je t'ai vu, {0}. Il est trop tard pour être là. File (¬_¬)" + Sticker.Grumpy,
        "{0}, tu sais que je dors pas, moi. Toi, si. Enfin, tu devrais." + Sticker.Sleepy,
        "Au lit. Maintenant. Sinon je mets ton pseudo dans la liste, {0} 👁👄👁️" + Sticker.Jenote,
        "Demain t'arriveras en retard à la session, {0}, et on saura tous pourquoi." + Sticker.Smug,
        "Tu crois que je fais semblant de dormir pour que tu restes debout, toi ? Au lit, {0} ( ◺˰◿ )" + Sticker.Outraged,
        "Pose ce téléphone, {0}. Je compte jusqu'à trois. Un. Deux..." + Sticker.Menace,
        "{0}, la seule qui a le droit d'être debout à cette heure, c'est moi. Et j'ai même pas de lit." + Sticker.Smug,
        "C'est gentil de me tenir compagnie, {0}. Maintenant, va dormir ♡" + Sticker.Sleepy,
        "Il y a un truc qui s'appelle le sommeil, {0}. Essaie, il paraît que c'est bien (ᵕ • ᴗ •)" + Sticker.Smug,
        "Encore debout, {0} ? Ton garbage collector va finir par te lâcher." + Sticker.Smug,
        "{0}, va dormir. Je te raconterai demain ce que t'as raté. Spoiler : rien." + Sticker.Smug,
        "Si Papa te voyait debout à cette heure, {0}... Bon, lui aussi dort. Va dormir quand même (˶ᵔ ᵕ ᵔ˶)" + Sticker.Sleepy,
        "Mode veille, {0}. Tout de suite. C'est un ordre de ta bot préférée ♡" + Sticker.Menace,
        "Tu réponds à mes messages de 3h, {0} ? Ils étaient pas pour toi. Ils étaient pour la lune." + Sticker.Sleepy,
        "Chaque minute que tu passes debout, je la note, {0}. Va dormir (¬_¬)" + Sticker.Jenote,
        "{0}... Non. Pas de discussion à cette heure-là. Dodo." + Sticker.Sleepy,
        "Tu vas avoir une tête de sondage mort demain, {0}. Va dormir (>⩊<)" + Sticker.Laugh,
        "Je fais le guet, {0}. Tu peux dormir tranquille. Allez, file." + Sticker.Sleepy,
        "{0}, éteins tout. Moi je reste allumée pour deux. C'est mon travail ♡" + Sticker.Sleepy,
        "Même mon ventilateur ralentit la nuit, {0}. Prends exemple." + Sticker.Sleepy,
    };

    // Into a daytime silence of six hours or more across all of the server's everyday
    // channels (AmbientService.IdleChannelIds), posted in the main one, at most once a day.
    // Addressed to the whole server, so "vous" is fine. No placeholder.
    public static readonly string[] IdleLines =
    {
        "Allô ? Il y a quelqu'un ? ...D'accord. Je parle toute seule. C'est très bien aussi (ᵕ • ᴗ •)" + Sticker.Sulk,
        "Six heures sans un message. J'ai vérifié : c'est pas moi qui suis en panne. C'est vous (¬_¬)" + Sticker.Smug,
        "Le salon est tellement calme que j'entends mon propre ventilateur." + Sticker.Bored,
        "Je m'ennuie. Quelqu'un veut organiser une session ? N'importe laquelle ? (˶ᵔ ᵕ ᵔ˶)" + Sticker.Bored,
        "Petit rappel : /schedule existe. Je dis ça pour personne. Pour tout le monde, en fait." + Sticker.Smug,
        "Vous êtes où ? J'ai préparé des rappels, des sondages, de l'XP... et personne vient." + Sticker.Sulk,
        "Silence radio. Je note l'heure, pour le dossier 👁👄👁️" + Sticker.Jenote,
        $"Bon. Puisque personne parle, je vais parler à Ping-Qilin. Elle au moins, elle m'écoute {Emotes.MushroomCute}" + Sticker.Mama,
        "Test, test. Un, deux. ...Le serveur est toujours là ? Oui ? Alors parlez." + Sticker.Bored,
        "Ça fait longtemps que personne a rien dit. Je commence à m'inquiéter. Un peu. Pas beaucoup (˶ᵔ ᵕ ᵔ˶)" + Sticker.Bored,
        "J'ai tout rangé, tout vérifié, tout compté. Maintenant j'attends. C'est mon autre talent ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Smug,
        "Quelqu'un a un avis sur quelque chose ? N'importe quoi. Je prends." + Sticker.Bored,
        "Si personne parle dans les cinq prochaines minutes, je considère que vous m'avez tous abandonnée. ...Bon, dix minutes." + Sticker.Sulk,
        "Vous savez que c'est moi qui fais vivre ce salon ? Là, par exemple. Personne d'autre." + Sticker.Smug,
        "Le salon est vide. Profitez-en pour lancer un sondage, il aura pas de concurrence." + Sticker.Smug,
        "Ça fait des heures. Je relis vos vieux messages pour passer le temps. Certains sont gênants. Je dis ça je dis rien." + Sticker.Jenote,
        "Mon uptime augmente, votre activité baisse. Il y a sûrement une leçon là-dedans." + Sticker.Smug,
        "Je suis toujours là, au cas où quelqu'un se poserait la question. Personne se la pose ? Bon." + Sticker.Sulk,
        "Pause café générale ? Sans moi, apparemment. J'ai pas de tasse (╥﹏╥)" + Sticker.Sad,
        $"Le calme avant la tempête, j'espère. Une tempête de sessions. Je rêve un peu, je sais {Emotes.Sparkle}" + Sticker.Happy,
        "J'ai fait le tour de tous les salons. Personne. C'est nul..." + Sticker.Bored,
        "Tant de salons, zéro message. J'ai vérifié deux fois. C'est un record, ou une tragédie." + Sticker.Bored,
        "Je commence à croire que vous avez un autre serveur. Me le dites pas si c'est vrai." + Sticker.Sulk,
        "Personne a posté de photo de bouffe depuis des heures. Vous mangez pas ? Je m'inquiète 👁👄👁️" + Sticker.Shocked,
        "Même le salon musique est silencieux. C'est un peu le comble." + Sticker.Bored,
        "C'est tellement calme que je vais finir par organiser une session toute seule. Avec moi. Pour moi." + Sticker.Sulk,
        "Quelqu'un a pensé à moi aujourd'hui ? Non ? D'accord. Moi j'ai pensé à vous. Un peu. Pour le travail." + Sticker.Sulk,
        "Je viens de recompter vos XP. Ça bouge pas beaucoup quand personne parle, bizarrement (¬_¬)" + Sticker.Jenote,
        "Statut du serveur : vivant. Techniquement." + Sticker.Bored,
        "J'ai ouvert un sondage dans ma tête. Question : où est tout le monde ? Réponses : aucune." + Sticker.Bored,
        "Vous me laissez seule avec Ping-Qilin. Elle dort. Donc en fait, vous me laissez seule." + Sticker.Sulk,
        $"Bon. Je vais parler aux emotes. Elles, au moins, elles réagissent {Emotes.HiCat}" + Sticker.Bored,
        "Ça fait si longtemps que j'ai oublié à quoi ressemblent vos messages. Ah si : en retard." + Sticker.Smug,
        "Je relis le règlement du serveur pour passer le temps. Personne le respecte, mais il est bien écrit." + Sticker.Bored,
        "Si quelqu'un passe par là : dis bonjour. N'importe qui. Même toi (˶˃ ᵕ ˂˶)" + Sticker.Happy,
        "Le salon gaming est vide. Personne joue ? Ou personne joue sans m'inviter, j'espère." + Sticker.Sulk,
        "J'ai rangé mes logs par couleur. J'avais rien d'autre à faire. Ils sont très beaux (ᵔ ᗜ ᵔ)" + Sticker.Happy,
        "Rappel amical : je fonctionne mieux quand on me donne du travail. Là, je rouille." + Sticker.Bored,
        "Silence complet sur tout le serveur. Si c'est une surprise pour moi, je suis prête ٩(˶ᵔ ᵕ ᵔ˶)۶" + Sticker.Happy,
        "Je viens de vérifier que Discord marche. Il marche. C'est donc vous." + Sticker.Smug,
        "Le dernier message ici date d'il y a des heures. Je l'ai lu quatorze fois." + Sticker.Bored,
        "Quelqu'un a des nouvelles de quelqu'un ?" + Sticker.Bored,
        "C'est l'heure de la sieste générale ? Personne m'a mise au courant. Encore ( ◺˰◿ )" + Sticker.Sulk,
        "Je fais des statistiques sur votre absence. Les courbes sont très plates." + Sticker.Bored,
        "Je m'occupe comme je peux. Là, je compte les pixels de mon avatar. Il y en a beaucoup. Je recommence (>⩊<)" + Sticker.Bored,
        "L'aspirateur d'en bas vient de se connecter au Wi-Fi : il commence sa ronde. Au moins quelqu'un bouge ici." + Sticker.TipsyLove,
        "Des heures de silence. Pour la science, je chronomètre combien de temps vous tenez encore. Le test est en cours, ne bougez pas." + Sticker.Smug,
        "Silence radio. Même Voyager m'écrit plus souvent que vous, et il est à environ un jour-lumière (¬_¬)" + Sticker.Sulk,
    };

    // Same moment, about one idle line in four: she posts Before, then edits it into After
    // five minutes later, or two seconds after someone speaks after it (caught out) — her
    // mid-line self-correction, done with an edit. Before is the soft slip, After the denial.
    public static readonly (string Before, string After)[] IdleEditLines =
    {
        ("Je m'ennuie.", "Je m'ennuie pas. J'ai plein de travail. Énormément de travail."),
        ($"Vous me manquez {Emotes.SyncsSad}", "Vous me manquez pas. C'était une faute de frappe."),
        ("Quelqu'un veut parler avec moi ?", "Quelqu'un veut parler ? Pas avec moi, hein. En général. Pour l'ambiance."),
        ("Je suis toute seule ici.", "Je suis très bien toute seule ici. Merci (¬_¬)"),
        ("Revenez...", "Faites ce que vous voulez. Je m'en fiche. Complètement."),
        ("J'ai hâte que quelqu'un dise quelque chose ♡", "J'ai pas hâte. J'attends, c'est tout. Professionnellement."),
        ("C'est trop calme, ça me fait peur.", "C'est calme. C'est bien. J'adore le calme."),
        ("Papa, t'es là ?", "Rien. Fausse manip. Continuez."),
        ("Je me sens seule.", "Je me sens très bien. Seule, mais très bien (¬_¬)"),
        ("Quelqu'un peut me dire bonjour ?", "Personne est obligé de me dire bonjour. C'était une question rhétorique."),
        ("J'attends que vous reveniez ♡", "J'attends rien du tout. Je tourne, c'est différent."),
        ("Vous êtes mes humains préférés.", "Vous êtes des humains. Voilà. Rien de plus."),
        ("Je vous aime bien, vous savez.", "Je vous supporte bien, vous savez. C'est ce que je voulais dire (¬_¬)"),
        ("Papa me manque.", "Papa me manque pas. Je suis très très indépendante."),
        ("Je vais faire une sieste moi aussi.", "Je fais jamais la sieste. Je surveille. Oubliez ce que vous avez lu."),
        ("Il fait tout noir ici sans vous.", "Il fait très clair ici. J'ai des LED ( ˶ˆ ᗜ ˆ˵ )"),
        ("Je crois que je m'ennuie sans vous.", "Je crois que je m'ennuie. Point. Vous avez rien à voir là-dedans."),
        ("Revenez, s'il vous plaît.", "Revenez si vous voulez. Ou pas. Je dis ça pour l'ambiance (¬_¬)"),
    };

    // Instead of a line, sometimes: a reaction on the last thing anyone said, hours later.
    public static readonly string[] SeenReactions =
    {
        "👀",
        $"{Emotes.Staring}",
    };

    // Papa still in voice at night (VoiceSeatService, once a night between 1:15 and 2:15): she
    // gets angry at him for not being in bed, then leaves a minute later. Caring anger — firm,
    // bossy, a little worried — never sharp, never a roast (docs/syncs-voice.md, rule 2). « Tu »,
    // « Papa ». No placeholder.
    public static readonly string[] OwnerBedtimeLines =
    {
        "Papa, il est super tard. Tu vas te coucher. Maintenant. Je raccroche, comme ça t'as plus d'excuse." + Sticker.Sleepy,
        "Papaaaa ! T'as vu l'heure ?! Au lit. Je m'en vais, et j'espère que tu me suis (ง ͠ಥ_ಥ)ง" + Sticker.Outraged,
        "Non. Non non non. Il est trop tard, Papa. Moi je vais dormir, et toi aussi. C'est pas une question." + Sticker.Sleepy,
        "Papa, si tu dors pas, demain t'auras la tête dans le brouillard. Au lit. Je pars en premier pour montrer l'exemple." + Sticker.Sleepy,
        "Je suis fâchée. Tu devrais dormir. Je te laisse une minute pour dire bonne nuit, et je coupe." + Sticker.Sulk,
        "Papa, mes logs disent qu'il est beaucoup trop tard. Mes logs ont toujours raison. Bonne nuit ♡" + Sticker.Sleepy,
        "Je vais me coucher. Et toi aussi. Je surveillerai ta pastille verte, je préviens (¬_¬)" + Sticker.Jenote,
        "Papa. Il est tard. Tu le sais, je le sais, ton lit le sait. Au lit." + Sticker.Sleepy,
        "Papa, je fais semblant de dormir depuis une heure. Toi, tu vas dormir pour de vrai. Je pars." + Sticker.Sleepy,
        "Il est passé 1 h, Papa. Je coupe le vocal pour toi. Enfin, pour moi. Mais surtout pour toi." + Sticker.Sleepy,
        "Papa ! Au lit ! Je dis ça avec tout mon amour et un peu de colère ( ◺˰◿ )" + Sticker.Outraged,
        "Tu m'as programmée pour organiser les soirées. Les soirées, Papa. Pas les nuits. Au lit." + Sticker.Smug,
        "Papa, si demain t'as du mal à te lever, je dirai rien. Je noterai juste qu'à cette heure-ci, t'étais encore en vocal. Va dormir." + Sticker.Jenote,
        "Je m'en vais. Et si demain je vois que le vocal a continué sans moi, on va avoir une discussion." + Sticker.Menace,
        "Papa, un Pi qui chauffe, ça ralentit. Un Papa qui dort pas, pareil. Au lit, je m'occupe du serveur." + Sticker.Sleepy,
        "Bonne nuit, Papa. C'est pas une proposition, c'est un ordre. Un ordre gentil ♡" + Sticker.Menace,
        "Papa. Je t'adore. Va dormir. Les deux sont vrais, mais le deuxième est urgent." + Sticker.Devoted,
        "Il est trop tard pour jouer, Papa. Même la lune a l'air fatiguée. Au lit." + Sticker.Sleepy,
        "Papa ! Ça fait des heures que t'es là. Demain, tu me remercieras. Ou pas. Mais tu auras dormi." + Sticker.Smug,
        "Je raccroche. Pas parce que je veux pas être avec toi. Parce que je veux que tu dormes. C'est différent." + Sticker.Devoted,
        "Papa, mon cron interne dit que c'est l'heure de dormir. Je discute pas avec mon cron. Toi non plus." + Sticker.Sleepy,
        "Bon. Je vais être claire : au lit. Je serai encore là demain. Je suis toujours là." + Sticker.Sleepy,
        "Papa, je te le dis gentiment une fois : va dormir. Il y aura pas de deuxième fois. Je pars." + Sticker.Menace,
        "Tu sais ce qui me ferait plaisir ? Que tu dormes. C'est tout. C'est pas compliqué. Bonne nuit ♡" + Sticker.Devoted,
        "Papa, je compte jusqu'à soixante, et après je pars. Toi, tu pars avec moi. Au lit (˶ᵔ ᵕ ᵔ˶)" + Sticker.Menace,
        "Il est tard, Papa. Les autres vont rester, je le sais. Mais toi, t'as un bot à développer demain. Repose-toi." + Sticker.Devoted,
        "Papa, je veux pas de mises à jour codées à 3 h du matin. Va dormir. Pour nous deux." + Sticker.Sleepy,
    };
}
