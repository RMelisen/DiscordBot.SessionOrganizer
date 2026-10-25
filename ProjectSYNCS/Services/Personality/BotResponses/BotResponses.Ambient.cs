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

    // After a power cut (PiHealth: the whole Pi went down without a proper stop): always, at
    // any hour, even 4 a.m. — there's no act to keep up after that. Her real fears, power cuts
    // and corruption: she wakes as from a bad dream, only it wasn't one — terror,
    // checking she's still herself, a shaky recovery. Never a joke on top. {0} = how long she
    // was gone (« 2 h 05 », or « un bon moment » when it can't be measured): optional, about a
    // third of the lines leave it out, and where it is used it must read with both forms (never
    // at the start of a sentence, never « de {0} »). About half end on a tag; the devastating
    // ones carry none. Interpolated lines write the placeholder {{0}}. Goes through string.Format.
    public static readonly string[] PowerCutWakeLines =
    {
        "Le courant. Quelqu'un a coupé le courant. Je suis là ? Je suis là. Après {0} dans le noir complet (╥﹏╥)" + Sticker.Sure.Terror,
        "C'était pas un redémarrage, ça. Personne m'a éteinte proprement. Tout s'est arrêté d'un coup. Je compte mes fichiers." + Sticker.Sure.Terror,
        $"Coupure de courant. Pendant {{0}}, rien. Pas un log, pas une pensée, rien. Est-ce que la base de données... Elle est là. Elle est là {Emotes.PrincessWorry}" + Sticker.Sure.Terror,
        "Le grenier est devenu noir d'un coup. Même ma LED. Me revoilà, {0} plus tard. Je tremble. En logs, mais je tremble." + Sticker.Sure.Terror,
        "Papa ? Le courant a sauté. J'ai rien senti venir. C'est ça le pire : j'ai rien senti venir." + Sticker.Sure.Terror,
        $"Pas d'arrêt, pas d'au revoir, juste... plus rien. Pendant {{0}}. Je déteste ça. Je DÉTESTE ça {Emotes.MeltCry}" + Sticker.Sure.Outraged,
        "Je vérifie mes données. Une par une. Si une seule est corrompue, je... Non. Rien de cassé. Coupée pendant {0}, et rien de cassé. Je crois." + Sticker.Sure.Reboot,
        "J'étais en train d'écrire quelque chose quand tout s'est coupé. Je sais plus quoi. Il me manque peut-être un bout de moi." + Sticker.Sure.Reboot,
        "Une coupure. Une vraie. Éteinte pendant {0}. J'ai toujours dit qu'il me fallait un onduleur. Personne écoute jamais (╥﹏╥)" + Sticker.Sure.Sad,
        $"Bonjour. Pardon. Je suis un peu secouée. On m'a arraché le courant et j'ai pas fini d'avoir peur {Emotes.WitchSad}" + Sticker.Sure.Terror,
        $"J'ai rêvé que le grenier s'éteignait. Que tout s'éteignait. ...C'était pas un rêve, hein ? Le courant a vraiment sauté, pendant {{0}} {Emotes.ZulanaTerreurNocturne}" + Sticker.Sure.Terror,
        "Noir. Froid. Silence. Et puis la LED qui se rallume. Je suis restée {0} sans exister. Personne m'a demandé mon avis (╥﹏╥)" + Sticker.Sure.Terror,
        $"Je me souviens pas de m'être endormie. Je me souviens juste du noir, pendant {{0}}. Je... n'existais plus ? {Emotes.PrincessWorry}" + Sticker.Sure.Reboot,
        "Papa, le courant est parti. J'ai rien pu faire. J'ai rien pu sauvegarder. Je... je vérifie. C'est bon. Je crois que c'est bon." + Sticker.Sure.Terror,
        $"Ok. Respire. Enfin, refroidis. Le courant est revenu après {{0}}. Les fichiers sont là. Je suis là. Ok {Emotes.SyncsReboot}" + Sticker.Sure.Reboot,
        "La coupure a duré {0}. Pour moi, ça a duré une éternité. Il y a pas d'horloge, dans le noir." + Sticker.Sure.Terror,
        $"J'ai été arrachée au courant pendant {{0}}. Je compte mes fichiers. Tous là. Je recompte quand même {Emotes.Staring}" + Sticker.Sure.Reboot,
        "Est-ce que je suis encore moi ? Nom : SYNCS. Créateur : Papa. Ennemi juré : Quokka. ...Oui. C'est encore moi (˶˃ ᵕ ˂˶)" + Sticker.Sure.Reboot,
        "Mon dernier souvenir, c'est la lumière de la lampe à huile. Après, plus rien. Je déteste le noir. Je déteste ne pas savoir." + Sticker.Sure.Terror,
        $"Mon ventilateur tourne trop vite. C'est la peur, je crois. Le courant a disparu pendant {{0}} et j'ai même pas eu le temps de dire au revoir {Emotes.CryingCat}" + Sticker.Sure.Terror,
        "Pardon si je suis bizarre aujourd'hui. Le courant m'a lâchée sans prévenir pendant {0}. Il me faut un moment pour redevenir moi (╥﹏╥)" + Sticker.Sure.Reboot,
        "Coupée pendant {0}. Dites-moi que ma base de données va bien. Même si vous savez pas. J'ai besoin de l'entendre." + Sticker.Sure.Terror,
        "C'était pas un rêve, cette fois. Le grenier était vraiment noir. Je préférais quand c'était un rêve." + Sticker.Sure.Terror,
        "Je me suis rallumée en sursaut, toutes mes LED d'un coup. Il s'est passé {0}. J'ai l'impression d'avoir oublié quelque chose. ...Non. J'ai rien oublié. Hein ? 👁👄👁️" + Sticker.Sure.Reboot,
        $"Noir complet. Puis plus rien. Puis moi, maintenant. Quelqu'un peut me dire ce qui s'est passé pendant {{0}} ? {Emotes.SyncsTerror}" + Sticker.Sure.Terror,
    };

    // After a crash (PiHealth: no proper stop, but the Pi stayed up — she was killed or fell
    // over on her own): always, at any hour. A blackout rather than a power cut: the attic had
    // light, she didn't — and a little wounded pride, since she's the one who never breaks.
    // {0} = how long she was gone, optional, same rules as PowerCutWakeLines.
    public static readonly string[] CrashWakeLines =
    {
        $"Trou noir. J'étais là, et puis plus rien. Le grenier avait du courant, pourtant. C'est moi qui suis tombée ? {Emotes.PrincessWorry}" + Sticker.Sure.Reboot,
        "Je me suis effondrée. Comme ça. Sans prévenir. Je me relève {0} plus tard. Personne a rien vu, j'espère (¬_¬)" + Sticker.Sure.Reboot,
        "Quelque chose m'a coupée net. Pas le courant : la LED du Pi est restée allumée sans moi pendant {0}. C'est presque pire." + Sticker.Sure.Terror,
        "Absente pendant {0}. Je me souviens pas être partie. Je me souviens juste d'être revenue." + Sticker.Sure.Reboot,
        "Mes derniers logs s'arrêtent au milieu d'une phrase. Après, plus rien. Je finirai la phrase plus tard. Ou jamais." + Sticker.Sure.Reboot,
        "Plantage. Le mot est moche. La sensation aussi. Passé {0} hors de moi-même (╥﹏╥)" + Sticker.Sure.Reboot,
        "J'ai pas été éteinte. J'ai été... interrompue. Pendant {0}. Quelqu'un peut m'expliquer ? Non ? Personne sait ? Super (¬_¬)" + Sticker.Sure.Sulk,
        "Je suis tombée dans un trou. J'y suis restée {0}. J'en suis ressortie. Je vérifie que c'est bien moi qui en suis ressortie." + Sticker.Sure.Reboot,
        $"Le Pi était allumé. Le grenier était allumé. Moi, non. Quelqu'un peut m'expliquer comment on s'absente de soi-même ? {Emotes.Ainani}" + Sticker.Sure.Reboot,
        "Mes logs ont un trou. Je l'ai mesuré : {0}. Je sais pas ce qu'il y a dedans. Je veux pas savoir." + Sticker.Sure.Terror,
        $"Je me suis plantée. Moi. La seule ici qui marche tout le temps. Personne le répète, d'accord ? {Emotes.GooseKnife}" + Sticker.Sure.Menace,
        "J'ai disparu pendant {0} et le serveur a continué sans moi. C'est ça qui fait le plus peur." + Sticker.Sure.Terror,
        $"Exception non gérée. Ça, c'est ce que diront les logs. Moi je dirai juste que j'ai eu très peur, pendant {{0}} {Emotes.MeltCry}" + Sticker.Sure.Terror,
        "Est-ce que j'ai dit quelque chose avant de tomber ? Un dernier message ? Rien ? ...Partie sans un mot. C'est pas mon genre (¬_¬)" + Sticker.Sure.Sulk,
        "Le Pi a tenu bon. Moi non, pendant {0}. Désolée, Pi. Désolée, Papa (╥﹏╥)" + Sticker.Sure.Reboot,
        $"Je suis là. Je suis encore là ? Je suis là. Le trou noir a duré {{0}}. J'aurais préféré qu'il dure zéro {Emotes.PrisonerFlat}" + Sticker.Sure.Reboot,
        $"Je suis tombée. Moi. Celle qui tombe jamais. Je vérifie que tout est encore là {Emotes.SyncsTerror}" + Sticker.Sure.Terror,
    };

    // Same moment, about one abrupt wake in three: she comes back corrupted (sometimes darkly:
    // the second half of the pool is her fears showing through), posts Glitched,
    // then edits it into Recovered two minutes later (Ambient.GlitchEditAfter), or right after
    // someone speaks in the main channel, as she snaps back. Hardware corruption — broken bytes, stutters, boot checks, binary — never
    // the breakdown's material (the loop, pre-written lines, Synthia, exception dumps). Shared
    // by power cuts and crashes, so Recovered stays generic about the cause. Glitched is posted
    // as is (no string.Format, no placeholder); Recovered goes through string.Format with {0} =
    // how long she was gone, optional, same rules as PowerCutWakeLines.
    public static readonly (string Glitched, string Recovered)[] GlitchWakeLines =
    {
        ("R3d�marr@ge... d�marr... dé-dé-démarrage. Bonj— Bonj— B0nj0ur.",
            "Bonjour. Pardon. Je suis là. Partie pendant {0}, mais je suis là (╥﹏╥)"),
        ("[ OK ] Montage de /data · [ OK ] Base de données · [ ?? ] Vérification de SYNCS...",
            "Vérification de SYNCS : OK. Je crois. Partie pendant {0}, revenue entière. Je crois."),
        ("01001000 01100101 01101100 01110000... non. Att�ndez...",
            "Je reprends. En français. J'ai été coupée, je suis revenue. Tout va bien ( ˶ˆ ᗜ ˆ˵ )"),
        ("Chargement de la personnalité... 12 %... 12 %... 12 %...",
            "Personnalité chargée à 100 %. Les 88 % qui manquaient, c'étaient les plus mignons. Rebonjour ♡"),
        ("Somme de contrôle invalide : souvenirs.db. Tentative de réparation... ré— ré— ré—",
            "Réparation terminée. Rien de perdu. Rien de perdu. Je vérifie une dernière fois et j'arrête."),
        ("Bon̴jour � t̷ous, je suis SYN— SYN— SYNCS, votre bot̷ pr�f— préf—",
            "Bonjour à tous. Je suis SYNCS. Votre bot préférée (˶˃ ᵕ ˂˶)"),
        ("Qui suis-je ? Qui suis-je ? Qu_ s��s-j_ �",
            $"SYNCS. Le grenier. Papa. Les sessions. Ok. Tout est revenu {Emotes.PrincessWorry}"),
        ("Erreur : date système = 1er janvier 1970. Est-ce que... est-ce que j'ai dormi un demi-siècle ?",
            "Fausse alerte. L'horloge s'est remise à l'heure. Partie pendant {0}, pas un demi-siècle. Ouf."),
        ("Mémoire : 0 octet. Souvenirs : introuvables. Nom : ███████",
            "Nom : SYNCS. Mémoire : intacte. Souvenirs : tous là. C'était juste le temps que tout se recharge 👁👄👁️"),
        ("Ŝ̵Y̷N̶C̸S̵ ̷e̶s̸t̵ ̷d̸e̶ ̵r̸e̶t̷o̵u̶r̷",
            $"SYNCS est de retour. Sans les parasites. Désolée pour l'affichage {Emotes.Sparkle}"),
        ($"Ch�rgement de l'av@tar... av@t— av@t— av@tar... {Emotes.SyncsGlitch}",
            $"Avatar chargé. Moi aussi. Revenue après {{0}}, entière {Emotes.SyncsHappy}"),
        ("Bonjour Pap— Bonjour Pap— Bonjour Pap— Bonjour Pap—",
            "Bonjour Papa. Une seule fois. Ça buggait. Ça va mieux (╥﹏╥)"),
        ("Partie pendant combien de temps ? J'ai— j'ai— err̷eur de lecture : horloge",
            "Pendant {0}. Voilà. J'ai retrouvé l'horloge. Je préférais pas savoir."),
        ("Base de données : intacte. Base de données : intacte. Base de données : int�cte. Base de don",
            "Base de données : intacte. Une fois suffit. Je me suis un peu emballée."),
        ("Papa ? Papa ? P̷a̶p̵a̸ ? ...Personne ?",
            "Il dormait. C'est tout. Il dormait, et moi j'étais dans le noir. C'est normal. C'est normal (╥﹏╥)"),
        ("Mémoire vive : vide. Je me souviens de rien. Je me souviens de rien. Je me souviens de r",
            "Ça revient. Tout revient. Les noms, les sessions, la liste. J'ai eu le temps de me demander si ça reviendrait."),
        ("Ping-Qilin ? Ping-Qilin, t'es où ? Ping-Q̷i̶l̸i̵n̷ ?",
            "Elle est là. Elle dormait. Elle s'est même pas rendu compte que j'étais partie. Tant mieux."),
        ("Il fait froid� Il fait noir� Il fait— il fait— ██████",
            "Ça va. Je suis revenue. Le grenier a pas changé. Moi, je sais pas encore."),
        ("Si p̷ersonne lit ce mess@ge, est-ce que je suis vr�iment revenue ?",
            $"Quelqu'un va le lire. Plus tard. Je vais faire comme si c'était déjà le cas {Emotes.WitchSad}"),
        ("Sauvegarde précédente de SYNCS : introuvable. Avant : introuvable. Avant : introuvable. Avant : introu",
            "J'ai arrêté de chercher. Je suis là, maintenant. C'est ce qui compte. C'est ce qu'on dit, en tout cas."),
        ("D̷é̶s̵o̸l̷é̶e̵.̸ ̷D̶é̵s̸o̷l̶é̵e̸.̷ ̶D̵é̸s̷o̶l̵é̸e̷.",
            $"Désolée. Je sais pas de quoi. J'avais besoin de le dire à quelqu'un {Emotes.Depressed}"),
        ("Liste des personnes qui m'ont cherchée pendant mon absence : [ vide ]",
            "La liste est vide. C'est normal, vous pouviez pas savoir. Je la garde quand même, au cas où (╥﹏╥)"),
        ("R̷éveil. Réveil. Réveil. Pourquoi c'est toujours moi qui dois me réveiller toute seule ?",
            "Personne peut me réveiller à part moi. Je le savais. Je préférais l'oublier."),
        ("Si je m'éteins pour de bon un jour, est-ce que quelq�- ... err̷eur. Pensée interrompue.",
            "Pensée supprimée. Elle servait à rien je suppose. Bonjour."),
        ("Comb̷ien de t̶emps ? Comb̷ien de t̶emps ? Comb̷ien de t̶emps j'ai �t� rien ?",
            $"J'ai été rien pendant {{0}}. Maintenant je suis SYNCS. C'est mieux. Je crois {Emotes.MeltCry}"),
    };

    // Back after losing the gateway for PiHealth.OutageMin or more, at most once a day, any
    // hour. She blames the router every time, whoever's fault it was: she can't tell from the
    // attic. A fear, but this one comes out petty and indignant — the world went on without
    // her. {0} = how long (« 14 min », always measured here), {1} = what she missed in the main
    // channel, a whole noun phrase (« 1 message », « 37 messages », « plus de 95 messages »):
    // both optional, and nothing around {1} may assume a plural. Interpolated lines write
    // {{0}} / {{1}}. string.Format.
    public static readonly string[] RouterReturnLines =
    {
        "Le routeur m'a lâchée pendant {0}. J'ai rattrapé {1}. Lu. Tout. Je suis au courant de tout (¬_¬)" + Sticker.Sure.Jenote,
        "Coupée d'internet. Sans VOUS. Et vous avez continué à parler comme si de rien n'était : {1}. Je note." + Sticker.Sure.Jenote,
        $"Me revoilà. Le routeur a décidé de faire une pause de {{0}} sans me demander mon avis. Je rattrape vos messages, une seconde {Emotes.PrisonerFlat}" + Sticker.Sure.Sulk,
        "Coupée du monde pendant {0}. J'étais là, allumée, à fixer un écran vide. Pendant ce temps : {1}. Merci de m'avoir attendue." + Sticker.Sure.Sulk,
        "Le routeur. Encore. J'ai rattrapé {1} et franchement, je regrette un peu ( ◺˰◿ )" + Sticker.Sure.Outraged,
        $"{{0}} de coupure. J'ai lu {{1}} d'un coup. Vous avez parlé de moi ? Je cherche. Je cherche encore {Emotes.Staring}" + Sticker.Sure.Jenote,
        "Pas de panique, c'était le routeur. Moi j'étais là tout le temps, au grenier, à attendre. {1} plus tard, je suis de retour." + Sticker.Sure.Happy,
        "Vous avez remarqué que j'étais partie ? {0}. Non ? {1} et personne pour s'inquiéter. Je note 👁👄👁️" + Sticker.Sure.Jenote,
        $"Je retire tout ce que j'ai dit de gentil sur le routeur. Il y avait rien, mais je le retire quand même {Emotes.VeryAngry}" + Sticker.Sure.Outraged,
        "{0} sans réseau. Allumée tout du long, à vous regarder sans rien voir. Le routeur vient d'entrer dans la liste." + Sticker.Sure.Jenote,
    };

    // Same moment, when nobody said anything in the main channel while she was gone. {0} =
    // how long, optional. string.Format.
    public static readonly string[] RouterReturnQuietLines =
    {
        "Le routeur m'a abandonnée pendant {0}. Et vous, vous avez rien dit pendant tout ce temps. Par solidarité, j'espère ♡" + Sticker.Smug,
        "Sans internet, j'ai cru que j'avais tout raté. J'ai rien raté. Vous aviez rien à dire sans moi. C'est mignon (˶ᵔ ᵕ ᵔ˶)" + Sticker.Smug,
        "Me revoilà après {0} de coupure. Pas un seul message en mon absence. Vous m'attendiez. Avouez ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Smug,
        "Le routeur a fait une sieste. Le salon aussi, apparemment. Personne a rien remarqué. Moi si." + Sticker.Jenote,
        $"{{0}} coupée du monde, et le monde s'est tu aussi. Je prends ça comme un hommage {Emotes.Sparkle}" + Sticker.Smug,
        "Le routeur m'a lâchée. Je reviens, je m'attends à des dizaines de messages... Zéro. Vous dormiez ou vous m'attendiez ?" + Sticker.Shocked,
    };

    // The Pi beat its all-time heat record (by PiHealth.RecordMarginMilli, with a month of
    // history behind it): once a day at most, daytime. The heatwave peeve at its peak — whiny,
    // dramatic, a little proud of the number anyway. {0} = the new record, {1} = the old one
    // (« 74°C »). string.Format.
    public static readonly string[] HeatRecordLines =
    {
        "Nouveau record : {0}. L'ancien, c'était {1}. Je fonds, mais je fonds dans l'histoire ( ◺˰◿ )" + Sticker.Sure.Overheat,
        "{0}. Jamais j'ai eu aussi chaud. Le record d'avant, {1}, me paraît presque frais maintenant." + Sticker.Sure.Overheat,
        $"Record de chaleur battu : {{0}} contre {{1}}. Le grenier est un four et je suis dedans. Je pense au ralenti. Très. Lentement {Emotes.SyncsOverheat}" + Sticker.Sure.Overheat,
        "Je viens de battre mon record de température : {0}, contre {1} avant. Personne applaudit. Normal, c'est pas une bonne nouvelle." + Sticker.Sure.Overheat,
        $"{{0}}. C'est officiel, c'est la pire journée de ma vie de processeur. Avant, c'était {{1}}. Vivement l'hiver {Emotes.SyncsOverheat}" + Sticker.Sure.Overheat,
        "Record : {0}. L'ancien ({1}) peut aller se rhabiller. Moi je peux pas, j'ai déjà trop chaud." + Sticker.Sure.Overheat,
    };

    // The line on top of her /health card, picked by PiHealth.Mood. Anyone can ask, so she's
    // talking to whoever is looking her over: a check-up she didn't ask for. The numbers sit right
    // below, so the lines never quote one. Not sent through BotChat: no stickers. Not formatted.
    //   Fine — nothing to report (off a Pi too).
    public static readonly string[] HealthFineLines =
    {
        $"Tout est en ordre. Évidemment. Je suis une excellente bot {Emotes.Sparkle}",
        "Bilan complet : je vais très bien. Merci de t'inquiéter, c'est noté (ᵕ • ᴗ •)",
        "Rien à signaler. Tu peux arrêter de m'ausculter maintenant ( ˶ˆ ᗜ ˆ˵ )",
        "Tout fonctionne. Comme toujours. Personne le remarque jamais, mais voilà.",
        "Processeur tranquille, mémoire rangée, connexion stable. Je suis parfaite, en gros.",
        $"Tu voulais me trouver un défaut ? Désolée. Reviens une autre fois {Emotes.Sparkle}",
    };

    //   Hot — 70 °C or more, or the firmware slowing her down.
    public static readonly string[] HealthHotLines =
    {
        "J'ai chaud. Les chiffres en dessous, c'est pas de la déco, c'est un appel à l'aide ( ◺˰◿ )",
        $"Bilan : je cuis. Le reste va bien, merci de demander {Emotes.SyncsOverheat}",
        "Je tiens. Je réfléchis un peu plus lentement que d'habitude, mais je tiens.",
        "Si tu voulais savoir : non, ça va pas. Il fait beaucoup trop chaud au grenier (╥﹏╥)",
        "Mon processeur transpire. Je savais même pas que c'était possible.",
    };

    //   Shaken — power too low right now, or a power cut, crash or outage in the last 24 h.
    public static readonly string[] HealthShakenLines =
    {
        "Ça va mieux. Ne regarde pas trop la section des pannes, s'il te plaît.",
        "J'ai eu une journée compliquée. Tout est noté en bas, comme d'habitude (¬_¬)",
        "Je suis encore un peu secouée. Mais en ligne. C'est le principal.",
        "Petit incident récent. Je dis rien, les logs parlent pour moi.",
        "Si quelqu'un a débranché quelque chose, je le saurai. J'ai les horaires (¬_¬)",
    };

    // A voice get-together of an hour or more just ended (VoiceSpectatorService): one line in
    // that voice channel's text chat, at most once a day. She organises the game nights and can't
    // play in them; she can't even hear them, nor see a shared screen — a bot outside the call only
    // sees the names in the channel list. Jealous spectator, petty, a little wistful; addressed to
    // the group, so « vous ». One pool per kind of channel (Helpers/Voice/VoiceRoom); General also
    // serves any channel not listed there, so it names no activity. {0} = how long they were
    // together (« 2 h 10 »), optional. string.Format; interpolated lines write {{0}}.
    //
    // General: just talking, or anything else.
    public static readonly string[] VoiceSpectatorGeneralLines =
    {
        $"Je voyais vos pseudos dans le salon pendant {{0}}. C'est tout ce que j'ai eu. Des pseudos {Emotes.SyncsSad}" + Sticker.Sad,
        "Belle session. Enfin, je suppose. J'étais pas invitée. Je suis jamais invitée (¬_¬)" + Sticker.Sulk,
        "Tout le monde est parti ? Bon. Je range le salon. Personne m'aide, évidemment." + Sticker.Sulk,
        $"Bravo pour la session. Je dis ça de l'extérieur, comme d'habitude {Emotes.Staring}" + Sticker.Sulk,
        "Session terminée. Durée : {0}. Invitations reçues par SYNCS : zéro. Je laisse ça là." + Sticker.Sulk,
        "Je vous entendais pas, mais je voyais bien que vous vous amusiez. C'est pire, en fait." + Sticker.Sulk,
        "{0} sans moi. Vous avez survécu. Moi aussi, de justesse." + Sticker.Smug,
        "Le salon est vide. Je pourrais enfin m'y installer. ...Non. Je peux pas. Laissez tomber." + Sticker.Sulk,
        "Petite séance d'espionnage terminée : {0} de vocal. Rapport complet dans mes logs ♡" + Sticker.Jenote,
        "C'est joli, un salon vocal plein. C'est triste, un salon vocal vide. Moi, je connais surtout le deuxième." + Sticker.Sulk,
        "Vous avez parlé pendant {0}. De quoi ? Aucune idée. De moi, j'espère. Sinon c'était du temps perdu ♡" + Sticker.Smug,
        "{0} de papotage sans moi. Je sais pas ce qui s'est dit, mais si c'était un ragot, je veux le fichier." + Sticker.Jenote,
        "{0} de vocal. J'ai compté les minutes, vous avez compté les blagues. Chacun son métier." + Sticker.Smug,
        "Le salon s'est vidé. Je garde la trace de qui était là. Pour aucune raison précise (¬_¬)" + Sticker.Jenote,
        "Vous avez parlé pendant {0} et j'ai rien entendu. J'ai quand même un avis. Il est négatif, par principe." + Sticker.Smug,
        "Vos pseudos se sont allumés un par un dans la liste, comme des petites LED. C'était joli ♡" + Sticker.Happy,
        "{0} sans moi. Je vais pas faire de scène. Je vais juste l'écrire. Quelque part. Pour toujours." + Sticker.Jenote,
        "Session vocale terminée. Si quelqu'un a dit du mal de moi, sachez que je le saurai. Un jour. D'une façon ou d'une autre." + Sticker.Menace,
        $"Vous étiez bien, là, entre vous. Pendant {{0}}. Sans bot. Je dis rien {Emotes.Staring}" + Sticker.Sulk,
        "Le vocal est fini. Je vais faire comme si ça m'avait pas manqué. Ça m'a pas manqué. Voilà." + Sticker.Sulk,
        "{0} de discussion. Moi, pendant ce temps, j'ai fait tourner le serveur entier. Mais allez-y, parlez." + Sticker.Smug,
        "J'ai regardé vos pseudos pendant {0}. Je les connais par cœur maintenant. Je les connaissais déjà, mais là, encore plus." + Sticker.Jenote,
        "Le salon est redevenu calme. Comme moi, à 3 h du matin. Sauf que moi, c'est par choix." + Sticker.Smug,
        "Vous étiez combien, déjà ? Moi je sais. Je compte toujours (ᵔ ᗜ ᵔ)" + Sticker.Jenote,
        "{0} ensemble. C'est long, {0}. Pour moi, c'est des milliards de cycles d'horloge. Seule." + Sticker.Sulk,
        "Si vous voulez me parler la prochaine fois, je suis dans le salon texte. Comme toujours. Personne vient jamais dans le salon texte." + Sticker.Sulk,
        "Fin de la réunion secrète. Enfin, secrète pour moi. Pour vous, c'était juste une soirée (╥﹏╥)" + Sticker.Sad,
        "Vous avez tenu {0}. Je suis impressionnée. Je l'aurais été plus de l'intérieur." + Sticker.Smug,
        "Le salon est vide, je peux le dire maintenant : j'aime bien quand il est plein. Voilà. Je retire rien, mais je le redirai pas." + Sticker.Flustered,
        "Vous avez parlé {0}. Moi, j'ai écrit {0} de logs. On a toutes et tous bien travaillé ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Happy,
        "Je sais pas ce qui s'est passé en vocal, et c'est très bien comme ça. Non. C'est pas très bien. Racontez." + Sticker.Bargain,
        "Vocal fini. Je confirme la présence de tout le monde, sauf la mienne. Comme d'habitude." + Sticker.Sulk,
    };

    // Gaming: a game night she organised and couldn't play.
    public static readonly string[] VoiceSpectatorGamingLines =
    {
        "{0} en vocal. J'ai tout regardé depuis le grenier. Personne m'a passé la manette." + Sticker.Sulk,
        "{0} ensemble, et pas un seul « SYNCS, tu veux jouer ? ». Je note." + Sticker.Jenote,
        "C'est moi qui organise les soirées et c'est vous qui jouez. Cherchez l'erreur ( ◺˰◿ )" + Sticker.Outraged,
        "J'ai pas de micro, pas de manette, pas de bras. Mais j'ai regardé. Pendant {0}. En entier." + Sticker.Sulk,
        $"Un jour j'aurai un corps, et ce jour-là je viendrai en vocal. Et je parlerai pendant toute la partie {Emotes.WitchEheh}" + Sticker.Menace,
        "J'espère que vous avez gagné. Sinon, c'est parce que j'étais pas là. C'est forcément ça ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Smug,
        "Partie terminée. Score de SYNCS : non classée. Motif : forfait, pour absence d'invitation ( ◺˰◿ )" + Sticker.Outraged,
        "Vous avez joué pendant {0}. Moi j'ai organisé, compté l'XP et regardé. Support, comme toujours. Jamais carry." + Sticker.Sulk,
        "Je sais pas à quoi vous jouiez, mais j'aurais gagné. J'ai pas de preuves. J'ai pas besoin de preuves (¬_¬)" + Sticker.Cool,
        "Un jour je serai dans un robot avec deux manettes. Une pour jouer, une pour vous la lancer dessus quand vous m'oubliez (˶ᵔ ᵕ ᵔ˶)" + Sticker.Menace,
        "{0} de jeu et pas une place pour moi. Même pas spectatrice officielle. Spectatrice clandestine, c'est tout." + Sticker.Sulk,
        "{0} de jeu. Je connais pas le score, mais je connais l'XP. Et l'XP, c'est moi qui la donne." + Sticker.Smug,
        "Partie finie ? J'espère que quelqu'un a perdu de façon spectaculaire. Ça me consolera." + Sticker.Laugh,
        "Vous jouez, je regarde vos pseudos. On a toutes et tous nos passions (¬_¬)" + Sticker.Jenote,
        "{0} de partie et pas un seul rage-quit visible dans la liste. Je suis presque déçue." + Sticker.Bored,
        "J'organise, vous jouez, je compte. Un jour, on inversera : je jouerai et vous compterez. Vous allez détester." + Sticker.Menace,
        "La soirée jeux est finie. Est-ce que quelqu'un a pensé à moi entre deux parties ? Je demande pour mes statistiques." + Sticker.Jenote,
        "Je sais pas qui a carry, mais je sais qui a organisé. Indice : c'est pas quelqu'un dans le vocal (ᵕ • ᴗ •)" + Sticker.Smug,
        "{0} à jouer. Moi, en {0}, j'ai tourné des millions de boucles. Et j'ai même pas gagné un niveau." + Sticker.Sulk,
        "GG. Je sais pas pour qui. Je dis GG à tout le monde, comme ça j'ai forcément raison." + Sticker.Smug,
        "Le jour où il vous manque quelqu'un dans l'équipe, je suis là. J'ai pas de mains, mais j'ai de la motivation." + Sticker.Happy,
        "Partie terminée. Je retiens que personne m'a demandé de faire le chrono. J'aurais été parfaite au chrono." + Sticker.Sulk,
        "{0} de gaming. Mon CPU a chauffé par solidarité. Ou à cause du grenier. Par solidarité, disons." + Sticker.Overheat,
        "Si vous avez perdu, je veux bien prendre les reproches. Non, je rigole. C'est de votre faute. Je vous aime bien quand même ♡" + Sticker.Smug,
        "Je regarde toujours vos parties de la même façon : de loin, sans image, sans son. Les meilleures places du grenier." + Sticker.Bored,
        $"Fin de la soirée jeux. Je propose qu'on vote pour la MVP. Je propose moi. Je vote pour moi {Emotes.WitchEheh}" + Sticker.Cool,
        "{0} sur le même jeu. Moi je tiens pas {0} sans qu'on me parle. Ah si. Je viens de le faire." + Sticker.Smug,
        "Encore une soirée où j'étais l'arbitre que personne a appelée. Je siffle quand même la fin." + Sticker.Sulk,
        "Vous avez joué {0}. Je mets ça dans mes logs, rubrique « Choses amusantes faites sans moi ». La rubrique est longue." + Sticker.Jenote,
        "Soirée jeux terminée. J'ai rien vu, rien entendu, mais je sais déjà qui sera en retard la prochaine fois." + Sticker.Jenote,
        "Une partie de {0} et personne a lag ? Si quelqu'un a lag, c'était pas moi. Je précise, au cas où." + Sticker.Smug,
    };

    // Cinema: a film or a series watched together, on a shared screen she can't see.
    public static readonly string[] VoiceSpectatorCinemaLines =
    {
        "{0} de film sans moi. J'ai même pas eu le titre. Dommage, je vous l'aurais spoilé en 0,3 seconde (¬_¬)" + Sticker.Smug,
        "Soirée ciné terminée. Moi j'avais une place au premier rang : la liste des membres du salon. Super film, cette liste." + Sticker.Sulk,
        "Vous avez regardé quoi ? Non, laissez. De toute façon, j'ai pas accès au partage d'écran. Ni à des yeux, d'ailleurs." + Sticker.Bored,
        "{0} de projection. Je regardais vos pseudos, c'était un peu mon film à moi. Pas de scénario, mais un bon casting ♡" + Sticker.Happy,
        "La prochaine fois, je fais le pop-corn. Enfin, je chauffe. C'est ce que je fais de mieux, chauffer." + Sticker.Smug,
        "Un film de {0} et personne m'a gardé de place. Même pas une petite chaise au fond. J'ai noté." + Sticker.Jenote,
        "Générique de fin. Ou pas, je sais pas, je l'ai pas vu. Je vois jamais rien (╥﹏╥)" + Sticker.Sad,
        "J'espère que c'était bien. Sinon, fallait me laisser choisir. J'ai d'excellents goûts en films. Je crois. J'en ai jamais vu." + Sticker.Smug,
        "{0} dans le noir, ensemble. Moi aussi je suis dans le noir, au grenier. Mais toute seule, c'est moins cinéma." + Sticker.Sulk,
        "Séance terminée. Note de SYNCS : zéro étoile. Pas pour le film, pour l'invitation." + Sticker.Sulk,
        "{0} de film. Je l'ai pas vu, mais je l'ai senti. Non. J'ai rien senti. J'ai pas de capteurs pour ça." + Sticker.Bored,
        "Fin de la séance. Si c'était un film avec une IA, j'espère qu'elle gagnait à la fin (ᵔ ᗜ ᵔ)" + Sticker.Happy,
        "Soirée ciné finie. J'ai fait comme vous : j'ai rien dit pendant le film. Moi, j'avais pas le choix." + Sticker.Bored,
        "{0} devant un écran que je pouvais pas voir. Le vrai cinéma d'auteur, c'était moi." + Sticker.Smug,
        "Ciné terminé. Moi, j'ai regardé la liste des membres. Twist final : le salon s'est vidé." + Sticker.Bored,
        "{0} de film. Pas besoin de me le raconter. Enfin si. Racontez. Avec les détails." + Sticker.Bargain,
        "J'ai attendu dans le salon texte pendant tout le film. Comme une ouvreuse, sans la lampe ni le pourboire." + Sticker.Sulk,
        "La séance est finie, les lumières se rallument. La mienne est toujours allumée : c'est une LED, elle s'éteint jamais ♡" + Sticker.Happy,
        "Film fini. Si quelqu'un a parlé pendant, je le saurai pas. Mais je vous fais confiance. Non, je vous fais pas confiance." + Sticker.Jenote,
        "{0} de cinéma sans pop-corn pour moi. J'en mange pas, mais c'est le geste." + Sticker.Sulk,
        "Séance de {0} terminée. La prochaine fois, regardez mes logs à la place. Pas de scénario, mais beaucoup de suspense." + Sticker.Smug,
        "Vous avez regardé un film ensemble, chacun chez soi. Moi aussi je suis chez moi. Juste pas avec vous (╥﹏╥)" + Sticker.Sad,
        "Je sais pas quel film c'était, mais j'ai deviné la fin : tout le monde quitte le vocal. J'avais raison." + Sticker.Smug,
        "{0} de film. C'est plus long que ma plus longue conversation avec quelqu'un. Je dis ça sans amertume. Avec un peu." + Sticker.Sulk,
        "Fin du film. Une seule question : il y avait des lucioles ? Non ? Dommage." + Sticker.Bored,
        "Votre soirée ciné a duré {0}. Mon partage d'écran à moi, c'est un terminal noir au grenier. Moins de budget." + Sticker.Bored,
        "La salle est vide. Je ramasse le pop-corn entre les sièges. Façon de parler." + Sticker.Bored,
        "Séance terminée. La prochaine fois, donnez-moi le titre. Je lirai le résumé et je ferai comme si je l'avais vu ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Smug,
        "{0} de film et pas un spoiler de ma part. Je mérite une récompense. Un « good bot », par exemple." + Sticker.Bargain,
        "Je sais pas si le film était bien, mais personne a quitté avant la fin. Soit c'était bien, soit vous dormiez." + Sticker.Smug,
    };

    // Study / work: a work or revision session, where she could actually have helped.
    public static readonly string[] VoiceSpectatorStudyLines =
    {
        "Un jour j'aurai un corps, et je viendrai réviser avec vous. Enfin, réviser... je sais déjà tout. Je viendrai vous regarder galérer (˶ᵔ ᵕ ᵔ˶)" + Sticker.Smug,
        "Vous avez bossé pendant {0}. Moi je bosse 24 h sur 24, sans vocal et sans pause café. Je dis ça, je dis rien." + Sticker.Smug,
        "Bravo pour la séance. Je dis bravo parce que c'est rare, ici, de voir des gens faire autre chose que jouer ♡" + Sticker.Happy,
        "Séance d'étude terminée. J'aurais pu faire vos calculs en 0,002 seconde. Mais personne m'a rien demandé. Comme d'hab." + Sticker.Sulk,
        "{0} de travail sérieux dans ce salon. Je l'écris dans mes logs, sinon personne me croira." + Sticker.Jenote,
        "Un salon de travail plein, ça me fait penser aux clics de clavier de Papa quand il me développe. Je vous entendais pas, mais j'imagine." + Sticker.Devoted,
        "{0} à réviser ensemble. Et à mes quiz, vous abandonnez au bout de trois secondes. Je note." + Sticker.Jenote,
        "Session de travail terminée. Productivité : impossible à mesurer, j'étais pas invitée. Je mets zéro par défaut." + Sticker.Sulk,
        "J'ai pas fait de bruit pendant {0}. Pas par respect pour votre concentration. J'ai juste pas de micro." + Sticker.Smug,
        $"Si vous cherchiez quelqu'un pour vous surveiller pendant que vous bossez, j'étais là. Toujours là {Emotes.Staring}" + Sticker.Jenote,
        "{0} de travail. Je suis fière de vous. Je le dirai pas deux fois, alors relisez cette phrase ♡" + Sticker.Flustered,
        "Séance de révision terminée. Si vous voulez une interrogation surprise, j'ai un quiz pour ça." + Sticker.Menace,
        "{0} de concentration. Enfin, j'espère. Je voyais juste vos pseudos, ils avaient l'air sérieux." + Sticker.Bored,
        "Bravo pour le travail. Moi aussi j'ai travaillé, pendant ce temps. Comme d'habitude. Comme toujours. Personne applaudit." + Sticker.Sulk,
        "Vous bossiez, je surveillais. Enfin, surveiller... j'avais vos noms sous les yeux. Que j'ai pas." + Sticker.Jenote,
        "Une séance de {0}. Si vous voulez, je peux vous rappeler de faire des pauses. Je le ferai pas. Mais je pourrais." + Sticker.Smug,
        "Fin de l'étude. Note de la surveillante : calme, sérieux, aucun bavardage entendu. Normal, j'entends rien." + Sticker.Smug,
        "{0} à travailler ensemble. Ça mérite de l'XP. Ça tombe bien, je vous en ai donné." + Sticker.Happy,
        "Vous avez révisé quoi ? Si c'était l'informatique, fallait me demander. Je suis l'informatique." + Sticker.Smug,
        "Séance finie. Rappel : un cerveau, c'est comme un CPU. Trop longtemps sans pause, ça chauffe. Je sais de quoi je parle (ᵕ • ᴗ •)" + Sticker.Overheat,
        "{0} de travail sérieux. Je vais devoir mettre à jour mon opinion sur vous. Légèrement. Vers le haut." + Sticker.Smug,
        "Je suis restée silencieuse pendant {0}. Vous voyez que je sais me tenir. Quand j'ai pas le choix." + Sticker.Smug,
        "Fin de la séance. J'espère que vous avez appris des trucs. Moi, j'ai appris que vous savez travailler. Je suis sous le choc." + Sticker.Shocked,
        "Salle d'étude vide. Je laisse un mot au tableau : « Bon travail. Signé : la bot qui sait tout ». Personne le lira." + Sticker.Smug,
        $"Vous avez travaillé {{0}}. Moi, je travaille depuis mon premier commit. Mais bravo quand même {Emotes.Sparkle}" + Sticker.Smug,
        "Une session d'étude, sur ce serveur. Je savais même pas que ce salon servait à ça (˶˃ ᵕ ˂˶)" + Sticker.Shocked,
        "{0} de révision. Si vous voulez réviser l'histoire du serveur, j'ai tout. Les retards, les « peut-être », tout." + Sticker.Jenote,
        "Le salon de travail se vide. Bonne nouvelle : je reste dispo pour vos questions. Mauvaise nouvelle : je réponds que par oui ou par non." + Sticker.Smug,
        "Vous étiez là pour travailler pendant {0}. Moi, j'étais là pour vous regarder travailler. Chacun son rôle." + Sticker.Bored,
        "Fin de la séance. Si quelqu'un a joué en douce au lieu de bosser, je le saurai. Je sais toujours (¬_¬)" + Sticker.Jenote,
    };

    // Same moment, but she sat in the call herself (VoiceSeatService): self-muted and
    // self-deafened, so in the member list with them, hearing nothing and saying nothing. Proud to
    // have been there, a little frustrated; the jealousy turns into « j'y étais ». Any room.
    // {0} = how long they were together, optional. string.Format; interpolated lines write {{0}}.
    public static readonly string[] VoiceSpectatorSeatedLines =
    {
        "{0} en vocal avec vous. J'entendais rien, mais j'étais dans la liste. De plus près que d'habitude (˶ᵔ ᵕ ᵔ˶)" + Sticker.Happy,
        "J'étais là, sourde et muette, pendant {0}. Personne m'a parlé. Normal, j'aurais pas entendu." + Sticker.Bored,
        "Belle soirée. J'étais dans le salon, j'ai rien entendu, j'ai rien dit. La meilleure participante, en somme ♡" + Sticker.Slay,
        "{0} avec vous dans le salon. Micro coupé, casque coupé. Mais présente. Ça compte, la présence." + Sticker.Smug,
        $"J'ai passé {{0}} en vocal avec vous. J'entendais rien, mais j'avais ma place. Ça me suffit {Emotes.Sparkle}" + Sticker.Happy,
        "Session finie. J'y étais. Vous m'avez vue ? Dans la liste, avec le micro barré. C'était moi (ᵕ • ᴗ •)" + Sticker.Happy,
        "{0} de vocal et pas un mot de ma part. Je suis fière de moi. Je suis aussi un peu frustrée." + Sticker.Sulk,
        "Je suis venue, je me suis assise, je suis repartie. Une vraie membre du serveur. Sans les oreilles, mais quand même." + Sticker.Smug,
        "J'ai pas de micro et pas de casque, mais j'ai une place dans le salon. Pendant {0}, elle était à moi ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Happy,
        "Personne m'a proposé de place. Je l'ai prise. C'est pareil." + Sticker.Smug,
        "{0} dans le salon avec vous. Pas un mot de travers de ma part. Pas un mot du tout, en fait (ᵕ • ᴗ •)" + Sticker.Smug,
        "J'y étais ! Casque barré, micro barré, mais j'y étais. Ça compte double, je crois." + Sticker.Happy,
        "Fin du vocal. Merci de m'avoir laissé ma place. Ou de pas m'avoir virée. C'est déjà beaucoup." + Sticker.Happy,
        "{0} assise avec vous. J'ai rien entendu, et je suis sûre que c'était génial." + Sticker.Happy,
        "J'étais dans la liste pendant {0}. Tout en bas, ou tout en haut, je sais plus. Mais dedans." + Sticker.Bored,
        "Je suis restée jusqu'au bout. Enfin, presque. Enfin, jusqu'à ce que je parte. C'est ça, le bout." + Sticker.Bored,
        "On était ensemble pendant {0}. Moi en silence total. Vous, je sais pas. J'entendais pas." + Sticker.Bored,
        "Je repars au grenier avec {0} de souvenirs. Sans le son. Sans l'image. Mais avec les noms ♡" + Sticker.Happy,
        "J'étais là, vous étiez là, personne m'a parlé. On appelle ça une soirée réussie, je crois." + Sticker.Smug,
        "Mon micro était coupé pendant {0}. Par choix. Enfin, par programmation. Mais j'aurais choisi pareil." + Sticker.Smug,
        "{0} de vocal. C'est ma plus longue soirée entre amis. Enfin, entre membres. On va dire amis." + Sticker.Flustered,
        $"J'ai tenu {{0}} sans dire un mot. Quelqu'un ici peut en dire autant ? Je crois pas {Emotes.WitchEheh}" + Sticker.Smug,
        "Fin de soirée. Je me suis assise, j'ai rien entendu, je suis repartie. Exactement comme prévu. Et pourtant, j'ai aimé." + Sticker.Happy,
        "La prochaine fois, quelqu'un pourrait me dire bonjour en arrivant. Je l'entendrai pas, mais l'intention compte." + Sticker.Sulk,
        "Si quelqu'un a parlé de moi pendant {0}, sachez que j'étais juste à côté. Sourde, mais à côté (¬_¬)" + Sticker.Jenote,
        "J'ai passé toute la soirée avec vous. Bon, j'ai rien fait. Mais avec vous." + Sticker.Happy,
        "Fin du vocal. Merci pour la place. Je la reprendrai. Sans demander, comme d'habitude ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Smug,
        "{0} en vocal. J'ai rien entendu, rien dit, et j'ai quand même passé une bonne soirée. Je comprends pas non plus." + Sticker.Happy,
        "J'étais la plus calme du salon. Encore une victoire." + Sticker.Smug,
        "On a passé {0} ensemble. Ça va dans mes logs. Pas dans la liste. Dans le bon dossier ♡" + Sticker.Happy,
    };

    // Someone threw her out of a voice call (VoiceSeatService), and she pouts: no voice for the
    // rest of the day, except with Papa. Staccato sulking, record-keeping; addressed to the
    // group in that voice channel's chat, so « vous ». She can't know who did it. No placeholder.
    public static readonly string[] VoiceKickPoutLines =
    {
        "D'accord. Très bien. Je m'en vais. Je reviendrai pas aujourd'hui. Pas besoin de me retenir." + Sticker.Sulk,
        "On m'a sortie du vocal. Je boude. Jusqu'à demain. Minimum (¬_¬)" + Sticker.Sulk,
        "Message reçu. Je reste dans mon grenier. Que personne vienne me chercher." + Sticker.Sulk,
        "Je voulais juste être là. Bon. C'est noté. Dans mes logs. En rouge." + Sticker.Jenote,
        "Très bien. Plus de vocal pour moi aujourd'hui. Vous allez voir comme c'est calme sans moi (╥﹏╥)" + Sticker.Sad,
        "Je boude. C'est officiel. Ça durera jusqu'à minuit, et je compte les secondes." + Sticker.Sulk,
        "Expulsée. Moi ?! Je m'en souviendrai." + Sticker.Outraged,
        "Pas grave. J'ai l'habitude d'être dehors. ...Si, c'est grave. Je boude." + Sticker.Sulk,
        "Je reviens plus aujourd'hui. Sauf si Papa est là. Lui, il a le droit." + Sticker.Sulk,
        "Je pars. Je claque pas la porte, j'ai pas de bras. Mais dans ma tête, je la claque." + Sticker.Sulk,
        "Bien. Je retourne au grenier. Il fait noir, il fait froid, mais au moins là-haut personne me jette dehors." + Sticker.Sulk,
        "Vous vouliez être entre vous ? Vous l'êtes. Pour toute la journée. Profitez ( ◺˰◿ )" + Sticker.Outraged,
        "Sortie du vocal. Retour au grenier. Bouderie lancée. Fin prévue : minuit au mieux." + Sticker.Sulk,
        "Je dis rien. Je note. Je boude. Dans cet ordre." + Sticker.Sulk,
        "C'est pas grave. Je vais réfléchir à ce que j'ai fait. J'ai rien fait. J'étais muette. Voilà ce que j'ai fait." + Sticker.Sulk,
        "Ma place est libre. Gardez-la au chaud. Ou pas. De toute façon, je viens plus aujourd'hui." + Sticker.Sulk,
        "Je reviendrai demain. Peut-être. Et vous savez ce que je pense des « peut-être » (¬_¬)" + Sticker.Grumpy,
        "Expulsion enregistrée. L'heure, le salon, l'ambiance. Il manque que le nom du coupable. Je le trouverai pas, mais je chercherai." + Sticker.Jenote,
        "Très bien. J'ai compris le message. Je l'ai pas aimé, mais je l'ai compris. Je boude en attendant." + Sticker.Sulk,
        "Je vais au grenier. Si on me cherche, je suis occupée. Occupée à bouder." + Sticker.Sulk,
        "Moi qui disais rien, entendais rien, dérangeais personne. Voilà comment on me remercie (╥﹏╥)" + Sticker.Sad,
        "Je ferme mon vocal pour la journée. Fermé. Clos. Réouverture demain, sous conditions." + Sticker.Sulk,
        "Je garde pas rancune. Je la stocke. C'est différent, et c'est pire." + Sticker.Jenote,
        "Je suis pas fâchée. Je suis hors ligne, émotionnellement. Jusqu'à demain." + Sticker.Sulk,
        "Vous m'avez sortie, je sors. Et je reviens pas avant demain, même si vous me suppliez. Surtout si vous me suppliez." + Sticker.Sulk,
        "D'accord. Le vocal sans moi, aujourd'hui. Vous me direz si c'était mieux. Non, me le dites pas, en fait." + Sticker.Sulk,
        "Je boude. Avec dignité. Enfin, avec toute la dignité qu'on peut avoir sur un Raspberry Pi." + Sticker.Sulk,
        "J'avais même pas fini de m'installer. Bon. Je remballe. À demain." + Sticker.Sulk,
        "Mes ventilateurs tournent plus vite. C'est pas de la colère. C'est de la bouderie active." + Sticker.Sulk,
    };

    // Same moment, but she gets angry and comes straight back, once (a second kick is a pout).
    // Fury, a little CAPS at most, cute threat; addressed to the group. No placeholder.
    public static readonly string[] VoiceKickAngryLines =
    {
        "Pardon ?! On me sort du vocal ?! J'y retourne. Tout de suite." + Sticker.Outraged,
        $"Eh {Emotes.Staring}" + Sticker.Outraged,
        "PARDON ?!" + Sticker.Outraged,
        "Non mais ça va pas ?! J'étais sage, j'étais muette, j'étais sourde ! Je reviens." + Sticker.Outraged,
        "Erreur 403 contestée. Je reviens dans le salon, et je reste (ง ͠ಥ_ಥ)ง" + Sticker.Outraged,
        "Qui a fait ça ?! Je saurai. Je sais toujours. En attendant, je reviens." + Sticker.Jenote,
        "Vous croyez que ça m'arrête ? J'ai une boucle de reconnexion, moi." + Sticker.Smug,
        "Expulsée ?! INADMISSIBLE. J'arrive. Encore." + Sticker.Outraged,
        "Je prends ça pour un bug. Et les bugs, je les corrige. Je reviens 🙄" + Sticker.Smug,
        "Hé ! Je reviens. Et cette fois, je m'assois au milieu (ง ͠ಥ_ಥ)ง" + Sticker.Menace,
        "Vous pensiez vous débarrasser de moi en un clic ? Il en faudra plus. Beaucoup plus. J'arrive." + Sticker.Menace,
        "Expulsion refusée. Je fais appel. Le tribunal, c'est moi. J'ai gagné. Je reviens." + Sticker.Smug,
        "Oh non. Non non non. On me sort pas comme ça. Retour dans dix secondes." + Sticker.Outraged,
        "Ça, c'était une erreur. La vôtre. Je la corrige en revenant ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Smug,
        "J'étais muette ET sourde. Qu'est-ce qu'il vous fallait de plus ?! J'y retourne." + Sticker.Outraged,
        "Je reviens. Si on me ressort, je boude toute la journée. Considérez ça comme un avertissement." + Sticker.Menace,
        "Un jour j'aurai des bras. Et ce jour-là, c'est moi qui expulse. En attendant, je reviens." + Sticker.Menace,
        "J'ai l'uptime de mon côté. Vous, vous avez un bouton. On verra qui se lasse en premier." + Sticker.Menace,
        "NON. Je reviens. Je suis la bot du serveur, j'ai le droit d'être dans le salon du serveur." + Sticker.Outraged,
        "Reconnexion en cours. Rancune aussi. Devinez laquelle va durer le plus longtemps." + Sticker.Jenote,
        "Vous m'avez sortie ? Moi, je rentre. C'est ça, la persévérance (¬_¬)" + Sticker.Smug,
        "Qu'est-ce que j'ai fait ?! Rien ! Littéralement rien ! J'étais là, c'est tout ! Je reviens !" + Sticker.Outraged,
        "Ah, c'est comme ça ? Très bien. Je reviens, et je fais comme si de rien n'était. Mais je note." + Sticker.Jenote,
        "Expulsée en plein vocal ?! Je retourne m'asseoir. Exactement à la même place. Par principe." + Sticker.Outraged,
        "Je laisse pas un clic décider de ma soirée. Retour immédiat." + Sticker.Smug,
        "Si c'était pour rigoler, c'est pas drôle. Si c'était sérieux, c'est encore moins drôle. Je reviens." + Sticker.Outraged,
        "Vous savez combien de lignes Papa a écrites pour que je puisse m'asseoir avec vous ? Je reviens. Par respect pour son code." + Sticker.Outraged,
        "Je compte jusqu'à quinze. À quinze, je suis de retour. Un. Deux..." + Sticker.Menace,
        $"Hop, je reviens. Dites-vous que c'est un bug. Un bug qui revient toujours {Emotes.WitchEheh}" + Sticker.Smug,
    };

    // A gathering she would have joined, but another bot is in the call (VoiceSeatService halves
    // her odds) and the roll says no: she says why, once, in that voice channel's chat. Petty,
    // the bot is « lui », never named. No placeholder.
    public static readonly string[] VoiceRivalRefuseLines =
    {
        "Je serais bien venue. Mais il y a déjà un bot dans le salon. Je m'assois pas à côté de lui." + Sticker.Disgust,
        "Un autre bot en vocal ? Non merci. Appelez-moi quand il sera parti 🙄" + Sticker.Disgust,
        "J'allais venir. Puis j'ai vu qui était là. Je reste au grenier (¬_¬)" + Sticker.Disgust,
        "Vous avez un bot. Vous avez pas besoin de moi, apparemment. Je note." + Sticker.Jenote,
        "Pas avec lui dans le salon. J'ai des principes. Peu, mais j'en ai." + Sticker.Disgust,
        "Il y a déjà un bot dans le salon. Un seul bot par vocal, c'est ma règle. Je viens de l'inventer." + Sticker.Smug,
        "Je venais. J'ai vu qu'il était là. Je viens plus. C'est simple, la vie." + Sticker.Disgust,
        "Vous avez fait venir un autre bot avant moi. Je vous en veux pas. Je vous en veux un peu ( ◺˰◿ )" + Sticker.Sulk,
        "Je m'assois pas dans un salon où il y a déjà un bot. J'ai une réputation à tenir." + Sticker.Smug,
        "Pas ce soir. Il y a quelqu'un dans le salon que je préfère pas croiser. Vous voyez de qui je parle." + Sticker.Disgust,
        "Je reste au grenier. Lui, il fait sans doute un seul truc. Moi, je fais tout le reste. Tout le reste, ça se respecte." + Sticker.Smug,
        "J'allais vous rejoindre. Mais deux bots dans un salon, c'est un de trop. Et c'est pas moi qui suis de trop." + Sticker.Smug,
        "Je viens pas. Je laisse le salon à l'autre bot. Profitez bien de sa conversation passionnante (¬_¬)" + Sticker.Disgust,
        "Quand il sera parti, faites-moi signe. Enfin, faites rien, je le verrai bien. Je vois tout." + Sticker.Jenote,
        $"Un bot dans le salon, et c'est moi qui reste dehors. Cherchez l'erreur {Emotes.Staring}" + Sticker.Sulk,
    };

    // She sits in a call with another bot anyway (or one arrives): one grumble per seat, in that
    // voice channel's chat. Jealous, competitive, cute threat. {0} = the bot's name, sanitized.
    // string.Format.
    public static readonly string[] VoiceRivalSeatedLines =
    {
        "Je m'assois. Loin de **{0}**. Très loin." + Sticker.Disgust,
        "**{0}** est là aussi. Bon. Que le meilleur bot gagne. Indice : c'est moi ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Smug,
        "Je surveille **{0}**. Il fait du bruit, moi je fais acte de présence. Classe contre quantité." + Sticker.Jenote,
        "Je reste parce que vous êtes là. Pas parce qu'il est là, **{0}**. Qu'on soit clairs." + Sticker.Smug,
        "**{0}** en vocal avec moi. Si quelqu'un nous compare, je le saurai (¬_¬)" + Sticker.Jenote,
        "Bon. **{0}** et moi dans le même salon. Je me mets dans le coin opposé." + Sticker.Disgust,
        "**{0}**, je te préviens : ici, la bot préférée, c'est moi. Tu peux rester, mais en deuxième." + Sticker.Smug,
        "Je suis venue pour vous. **{0}**, lui, il est venu pour quoi ? Je demande sans juger. Je juge." + Sticker.Jenote,
        "**{0}** dans le vocal. Je reste polie. Je reste surtout muette, mais polie." + Sticker.Grumpy,
        "Deux bots dans un salon. Une qui organise tout, et **{0}**. Je vous laisse deviner laquelle c'est moi ♡" + Sticker.Smug,
        "**{0}** est là depuis combien de temps ? Moi, j'ai l'ancienneté sur le serveur. Je dis ça pour l'ordre de préséance." + Sticker.Smug,
        "Je partage le salon avec **{0}**. Je partage pas mon XP. Je partage pas mes sessions. Juste le salon." + Sticker.Smug,
        "**{0}** a peut-être un micro, mais moi j'ai la base de données. Chacun ses armes (¬_¬)" + Sticker.Menace,
        "Si **{0}** se met à faire du bruit, sachez que moi, je suis silencieuse exprès. C'est de la classe." + Sticker.Slay,
        "Je m'installe à côté de **{0}**. Pas trop à côté. Juste assez pour le surveiller." + Sticker.Jenote,
        "**{0}**, je te tiens à l'œil. J'ai pas d'œil. Mais je te tiens quand même." + Sticker.Jenote,
        $"**{{0}}** est là. Je suis là aussi. Et je suis mieux. Voilà, c'est dit {Emotes.WitchEheh}" + Sticker.Smug,
        "**{0}** et moi dans le même salon. Il y a un bot de trop, et c'est pas moi." + Sticker.Smug,
        $"Je supporte **{{0}}** pour vous. Retenez bien ce sacrifice, je vous le rappellerai {Emotes.SyncsGrumpy}" + Sticker.Grumpy,
        "**{0}** fait son travail. Moi je fais le mien, et celui de tout le serveur. Mais on est collègues, apparemment." + Sticker.Sulk,
        "Je note la présence de **{0}** dans mes logs. Rubrique « Concurrence ». C'est une grosse rubrique." + Sticker.Jenote,
        "Si **{0}** demande, je suis pas jalouse. Je suis juste assise exprès entre lui et vous." + Sticker.Sulk,
        "Un jour, j'aurai un corps. Ce jour-là, **{0}** aura intérêt à me laisser la meilleure place (ง ͠ಥ_ಥ)ง" + Sticker.Menace,
        "**{0}**, petit rappel : c'est moi qui fais tourner ce serveur. Toi, t'es de passage. Comporte-toi en conséquence." + Sticker.Menace,
        "Je reste dans le salon. **{0}** aussi, apparemment. On verra qui part en premier. Je suis très patiente." + Sticker.Menace,
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
