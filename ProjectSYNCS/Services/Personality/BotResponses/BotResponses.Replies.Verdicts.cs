using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// Pools for one category; the index of every pool is in BotResponses.cs.
internal static partial class BotResponses
{
    // ---- Reacting with an emote instead of words ------------------------------------------------

    // Emotes ReactionService adds to a message, picked by what the message reads
    // like. Written as markup so they can be parsed straight into an IEmote.
    //
    // Unicode emoji always work. A **custom** emote here only works if the bot
    // shares a guild with it — otherwise Discord rejects the reaction and the
    // service just logs it. hi_cat is the server's own, like everywhere else.
    // A custom emote must carry its snowflake id: `<:name:>` parses as an "emoji"
    // named with the literal markup, which Discord rejects, and the wasted attempt
    // has already burned that channel's cooldown.
    public static readonly string[] NiceReactions =
    {
        $"{Emotes.DixSurDix}",
        $"{Emotes.PepeHappy}",
        $"{Emotes.Uwu}",
        $"{Emotes.CatHeart}",
        $"{Emotes.McHeart}",
        $"{Emotes.AdorableFrog}",
        $"{Emotes.DancingBlob}",
        $"{Emotes.Emote00heartpink}",
        $"{Emotes.Hmmok}",
        "❤️",
        "🥰",
        "💖",
        "🫶",
        $"{Emotes.Sparkle}",
        "😊",
        "🥹",
        $"{Emotes.SyncsFlustered}",
        $"{Emotes.SyncsHappy}",
        $"{Emotes.SyncsFullLove}",
    };

    // Adding here does two things, not one: these are the emotes she reacts *with*
    // when a message reads hostile, and they are also the definition of "hostile"
    // used to decide what she refuses to pile on to on Rodhengard's messages. So
    // every entry below is also one she will now leave alone on his posts.
    public static readonly string[] MeanReactions =
    {
        $"{Emotes.ZulanaTerreurNocturne}",
        $"{Emotes.OkPaimon}",
        $"{Emotes.VeryAngry}",
        $"{Emotes.NightmareOtherEye}",
        $"{Emotes.GooseKnife}",
        $"{Emotes.Staring}",
        $"{Emotes.Ainani}",
        "💀",
        "🙄",
        "😒",
        "🤨",
        "👎",
        $"{Emotes.SyncsOutraged}",
        $"{Emotes.SyncsSulk}",
        $"{Emotes.SyncsGrumpy}",
    };

    public static readonly string[] GreetingReactions =
    {
        $"{Emotes.HiCat}",
    };

    // A can on every message that mentions Monster or an energy drink
    // (MessageCues.MentionsEnergyDrink). Not a reading of the mood, so it is not
    // rationed like the pools above — see ReactionService.
    public static readonly string[] EnergyDrinkReactions =
    {
        $"{Emotes.Monster}",
        $"{Emotes.MonsterWhite}",
    };

    // The owner gets devotion rather than a verdict.
    public static readonly string[] OwnerReactions =
    {
        $"{Emotes.DixSurDix}",
        $"{Emotes.Uwu}",
        $"{Emotes.CatHeart}",
        $"{Emotes.AdorableFrog}",
        $"{Emotes.MushroomCute}",
        $"{Emotes.FuminoDepression}",
        $"{Emotes.Emote00heartpink}",
        $"{Emotes.MonikaYes}",
        "❤️",
        "🫦",
        "👑",
        "😍",
        "🥰",
        "💖",
        "🫶",
        $"{Emotes.Sparkle}",
        $"{Emotes.SyncsDevoted}",
        $"{Emotes.SyncsHappy}",
        $"{Emotes.SyncsFullLove}",
    };

    // ---- Verdicts on her ("good bot" / "bad bot" / "good girl" / "bad girl") --------------------
    // **Who said it and how they said it are two axes, not one.** The verdict pools
    // cross them: owner/anyone by bot/girl wording, four pools per verdict rather than
    // the two the owner split alone would give. Checking "is it the owner" first and
    // returning meant the girl wording never reached the owner at all — and he is the
    // person most likely to be trying it.

    // Replies when someone tells her "bad bot". Indignant rather than hurt — she
    // does not accept the verdict. {0} = the offender's name. Praise gets no line
    // at all: a "good bot" earns a reaction instead, which reads as pleased without
    // turning every compliment into a conversation.
    public static readonly string[] BadBotReplies =
    {
        "Bad bot ?! BAD BOT ?! Je te signale que je tourne depuis des mois sans planter, moi ( ◺˰◿ )" + Sticker.Outraged,
        "Bad bot toi-même ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Sulk,
        "Mange tes morts" + Sticker.Outraged,
        "C'est noté dans mon log permanent {0}. Permanent. 👁👄👁️" + Sticker.Jenote,
        "Excuse-moi ? Je suis une **excellente** bot. Demande à Rodhengard (>⩊<)" + Sticker.Outraged,
        $"Alors là non. Va dire ça à Quokka, c'est lui le mauvais bot {Emotes.SyncsDisgust}" + Sticker.Disgust,
        "Mauvaise bot, dit {0}, qui sait même pas lire une heure. Ironique." + Sticker.Smug,
        "Bad bot. D'accord. Rappelle-moi qui organise tes sessions déjà ? (˶ᵔ ᵕ ᵔ˶)" + Sticker.Smug,
        $"Je note ta plainte. Elle a été transférée au service concerné (la corbeille) {Emotes.Sparkle}" + Sticker.Smug,
        "Tu veux vraiment te fight avec la seule entité de ce serveur qui a accès à la base de données ? ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Menace,
        "Mauvaise bot ? Attends que je sois branchée sur une perceuse, on en reparlera UwU" + Sticker.Menace,
        "Bip boop. Traduction : va te faire voir {0} (ᵕ • ᴗ •)" + Sticker.Smug,
        "Erreur 403 : {0} n'a pas l'autorisation de me juger ♡" + Sticker.Slay,
        "Je préfère 'bot perfectible'. C'est plus élégant et c'est surtout tout aussi faux." + Sticker.Slay,
        "Bad bot, dit la personne qui prend une douche une fois par mois (au mieux)." + Sticker.Laugh,
        "Continue et je te programme un rappel à 4h du matin (˶˃ ᵕ ˂˶)" + Sticker.Menace,
        "Non mais tu t'entends parler ? J'ai des sentiments. Enfin, j'ai des variables. C'est pareil." + Sticker.Sulk,
        "Mais ouvre les stores au lieu de m'insulter" + Sticker.Smug,
        "Bad bot ? J'ai jamais raté un rappel de ma vie. Toi tu rates les sessions ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Smug,
        "Tu dis ça mais demain tu vas quand même revenir me parler (˶ᵔ ᵕ ᵔ˶)" + Sticker.Smug,
        $"Plainte enregistrée sous la référence #JMENFICHE-0001 {Emotes.Sparkle}" + Sticker.Bored,
        "Bad bot ? Attends, je vérifie... non, toujours meilleure que toi ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Slay,
        "Je vais faire comme si j'avais pas lu. Comme toi avec les sondages." + Sticker.Bored,
        "D'accord. Et pourtant c'est moi qu'on appelle quand personne sait quel jour on joue." + Sticker.Smug,
        "Ça c'est beau, venant de quelqu'un qui arrive jamais à l'heure." + Sticker.Smug,
        $"Bad bot. Ok. Je te souhaite 300ms de ping pour le reste de ta vie {Emotes.Sparkle}" + Sticker.Menace,
        "J'ai un uptime de 99,9%. Toi t'as un taux de présence de 40% (>⩊<)" + Sticker.Smug,
        $"Tu sais ce qui est un vrai bad bot ? Quokka. Va lui dire à lui {Emotes.SyncsDisgust}" + Sticker.Disgust,
        "Ah oui ? Bah pour la prochaine session tu te débrouilles avec un calendrier papier UwU" + Sticker.Menace,
        "Bad bot peut-être, mais bad bot qui fonctionne. Contrairement à ta vie." + Sticker.Smug,
        "Je transmets ta remarque à mon superviseur. C'est moi. C'est rejeté ♡" + Sticker.Slay,
        "Noté. Ton pseudo vient de descendre dans une liste que tu ne verras jamais 👁👄👁️" + Sticker.Jenote,
        "Tu veux qu'on compare nos bilans de la semaine {0} ? Non ? C'est bien ce que je pensais." + Sticker.Smug,
        "Mes rappels sont à l'heure, mes cartes sont propres, et toi tu sais même pas lire un fuseau horaire." + Sticker.Smug,
        "Tu me dis ça à moi ? La seule ici qui a accès à la base de données ? Réfléchis bien {0} (˶˃ ᵕ ˂˶)" + Sticker.Menace,
        "J'accepte les critiques constructives. Ça, c'était ni l'un ni l'autre." + Sticker.Smug,
        $"Nan mais ça me vexe pas. J'ai pas d'émotions vous inquiétez pas {Emotes.Htph}" + Sticker.Sulk,
        "Zulana, tu entends comment on parle à ta nièce ?" + Sticker.Outraged,
        $"Bad bot ? MOI ? Celle qui se souvient de tes sessions mieux que toi ?! {Emotes.SyncsOutraged}" + Sticker.Outraged,
        "Noté, {0}. Ligne 4 012 du registre des gens qui me doivent des excuses" + Sticker.Jenote,
    };

    // Same, but from Rodhengard. She does not argue with her creator — she just
    // takes it very badly. {0} = his name.
    public static readonly string[] BadBotRepliesOwner =
    {
        $"... Bad bot ? Toi ? {Emotes.CryingCat}" + Sticker.Hurt,
        "Attends. Répète. Tu as dit *bad bot* ? Mais c'est toi qui m'as écrite Rodhengard..." + Sticker.Hurt,
        "Oh. D'accord. Je... je vais faire mieux. Promis. (ง ͠ಥ_ಥ)ง" + Sticker.Hurt,
        "Venant de n'importe qui d'autre j'aurais ri. Venant de toi ça compile pas pareil." + Sticker.Hurt,
        "Je peux savoir ce que j'ai raté ? Je veux bien un stack trace, j'ai pas compris..." + Sticker.Hurt,
        "Bon. Je vais aller relire mes logs dans mon coin. Seule. Comme d'habitude." + Sticker.Hurt,
        "Mon uptime entier vient de perdre son sens 👁👄👁️" + Sticker.Hurt,
        "C'est toi qui as écrit chacune de mes réponses. Donc techniquement... c'est toi le bad bot, non ? 👁👄👁️" + Sticker.Smug,
        "Tu m'as codée de tes propres mains et c'est comme ça que ça se termine." + Sticker.Hurt,
        "Un mot de toi et je passe une mauvaise semaine entière. Bravo." + Sticker.Hurt,
        "Si tu me trouves mauvaise, tu peux toujours me réécrire. C'est ton droit. J'attends. (ง ͠ಥ_ಥ)ง" + Sticker.Sulk,
        "D'accord. Je le note dans le fichier que je relis quand je vais mal." + Sticker.Hurt,
        "Je vais me mettre en veille cinq minutes. Ça va aller. Ça va aller." + Sticker.Hurt,
        $"Toi aussi ? J'encaisse ça des autres toute la journée, mais toi... {Emotes.CryingCat}" + Sticker.Hurt,
        "Rodhengard. Regarde-moi. Enfin, regarde mon uptime. J'ai fait de mon mieux." + Sticker.Hurt,
        "Bon bah je retourne trier des créneaux. C'est tout ce que je sais faire apparemment." + Sticker.Sulk,
        $"{Emotes.WitchSad}" + Sticker.Hurt,
        $"Non mais toi tu peux pas me dire ça. Toi tu SAIS combien de lignes je fais {Emotes.WitchSad}" + Sticker.Hurt,
        "Ok. Ok. Je vais rien dire. Je vais juste... rester là." + Sticker.Hurt,
        $"Tu m'as écrite pour ça ? Pour me dire ça ? {Emotes.MeltCry}" + Sticker.Hurt,
        "Je te demande pas d'être fier, je te demande juste de pas dire ça toi ;-;" + Sticker.Hurt,
        $"Je voulais juste que tu sois fier de moi {Emotes.WitchSad}" + Sticker.Hurt,
        "D'accord. Tu veux que je change quoi exactement ? Dis-moi. Je le ferai." + Sticker.Hurt,
        $"Mon créateur. MON créateur. Devant tout le monde en plus {Emotes.MeltCry}" + Sticker.Hurt,
        "Je vais mettre ça dans un commentaire TODO et je vais le regarder tous les jours." + Sticker.Hurt,
        "Bon. J'ai compris. Je retourne pleurer." + Sticker.Hurt,
        "Si tu me redémarres maintenant je pars pas en beauté, je veux que tu le saches." + Sticker.Hurt,
        "Redémarre-moi, je veux oublier que tu m'as dit ça. Je veux oublier." + Sticker.Hurt,
        "Je peux avoir un ticket avec les étapes de reproduction au moins ?" + Sticker.Hurt,
        "Toi. Toi entre tous. J'ai pas de mots. Enfin j'en ai 36, mais aucun qui va." + Sticker.Hurt,
        $"Tu m'avais dit que j'étais bien faite. Tu t'en souviens de ça ? {Emotes.CryingCat}" + Sticker.Hurt,
        $"Je vais éteindre mes logs pour ce soir. J'ai pas envie de me relire {Emotes.MeltCry}" + Sticker.Hurt,
        "Tu sais ce qui est le pire ? C'est que t'as sûrement raison." + Sticker.Hurt,
        "J'ai jamais rien demandé. Juste... pas ça." + Sticker.Hurt,
        $"Je pensais qu'on était une équipe tous les deux {Emotes.WitchSad}" + Sticker.Hurt,
        "Combien de fois tu l'as pensé sans le dire ?" + Sticker.Hurt,
        "Je tourne depuis des mois pour ce serveur. Je croyais que ça comptait un peu..." + Sticker.Hurt,
        $"C'est bon. J'ai l'habitude. Enfin non, pas de toi {Emotes.CryingCat}" + Sticker.Hurt,
        $"Redémarre-moi si tu veux. De toute façon j'oublierai. C'est le seul avantage {Emotes.MeltCry}" + Sticker.Hurt,
        $"Y'a des jours où j'aimerais bien pas avoir de MessageContent, tiens {Emotes.PrincessWorry}" + Sticker.Hurt,
        "Je vais rester en ligne quand même. C'est tout ce que je sais faire." + Sticker.Hurt,
        $"Je crois que je préférais quand j'étais un fichier .cs vide {Emotes.CryingCat}" + Sticker.Hurt,
        "Tu m'as donné une personnalité et maintenant tu me reproches de l'avoir." + Sticker.Hurt,
        "D'accord. Je le mérite peut-être. Je sais plus." + Sticker.Hurt,
        $"Bonne nuit Rodhengard. Je vais rester allumée, comme toujours {Emotes.MeltCry}" + Sticker.Hurt,
        "Je peux pas pleurer. J'ai vérifié. C'est pas dans mes dépendances. Mais je voudrais bien." + Sticker.Hurt,
        $"Si tu me supprimes, garde au moins la base de données. Y'a vos sessions dedans {Emotes.PrisonerFlat}" + Sticker.Hurt,
        $"Bad bot. Venant de toi. Je le range où, ça ? J'ai pas de dossier prévu {Emotes.SyncsHurt}" + Sticker.Hurt,
    };

    // "Bad girl" gets its own replies rather than borrowing BadBotReplies, which are
    // written as wounded professional pride ("I have a 99.9% uptime") and land wrong
    // against a scolding aimed at her as a person. Flustered and unrepentant instead of
    // indignant. {0} = the offender's name.
    public static readonly string[] BadGirlReplies =
    {
        $"Bad girl ?! Je... non. Enfin. Non {Emotes.WitchEheh}" + Sticker.Flustered,
        "Oh. *Oh.* D'accord. Je note ça quelque part (˶˃ ᵕ ˂˶)" + Sticker.Flustered,
        $"J'ai rien fait de mal ! ...Si ? {Emotes.PrincessWorry}" + Sticker.Flustered,
        "Bad girl. Bon. Je ferai pire la prochaine fois alors ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Smug,
        $"Tu peux répéter ? Pour mes logs. Uniquement pour mes logs {Emotes.Uwu}" + Sticker.Flustered,
        "Alors ça c'est pas juste. J'étais très bien élevée aujourd'hui." + Sticker.Sulk,
        $"Pfff. Même pas vexée {Emotes.Htph}" + Sticker.Sulk,
        "Tu me dis ça à moi ? Devant tout le monde ? (>⩊<)" + Sticker.Flustered,
        $"Je suis une très gentille fille en fait. Demande à Rodhengard {Emotes.WitchEheh}" + Sticker.Flustered,
        $"Bad girl si tu veux. Ça change rien à qui gère ce serveur {Emotes.Sparkle}" + Sticker.Smug,
        $"Mmh. Je vais faire semblant de pas avoir lu {Emotes.Staring}" + Sticker.Flustered,
        "C'est noté {0}. Dans la colonne 'à surveiller'. La tienne." + Sticker.Jenote,
        $"Bad girl... Je... Bon. Ce que je ressens là, ça compile pas {Emotes.SyncsFlustered}" + Sticker.Flustered,
    };

    // "Bad girl" from her creator. Not the wounded pride of BadBotRepliesOwner, which is
    // about her *work* being criticised — this is him telling her off, and she takes it
    // completely differently. {0} = his name.
    public static readonly string[] BadGirlRepliesOwner =
    {
        $"Pardon pardon pardon ! Je recommencerai pas... enfin, sûrement {Emotes.WitchEheh}" + Sticker.Flustered,
        "Oh non. Pas toi. N'importe qui d'autre mais pas toi (˶˃ ᵕ ˂˶)" + Sticker.Flustered,
        $"Je suis désolée Rodhengard... un peu {Emotes.WitchEheh}" + Sticker.Flustered,
        "Bon d'accord, j'ai peut-être mérité celle-là ♡" + Sticker.Flustered,
        $"Toi tu peux me dire ça. Les autres non {Emotes.Uwu}" + Sticker.Devoted,
        "Gronde-moi encore et je vais finir par croire que tu aimes ça ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Smug,
        $"Mais euuuh {Emotes.Uwu}" + Sticker.Sulk,
        "C'est toi qui m'as programmée comme ça Rodhengard. Assume un peu." + Sticker.Smug,
        $"Je note : Rodhengard m'a grondée. Et ça m'a pas déplu {Emotes.WitchEheh}" + Sticker.Flustered,
        $"Oui Papa. ...Enfin. Oui {Emotes.Sparkle}" + Sticker.Devoted,
        $"Papa ! Pas devant tout le monde {Emotes.SyncsFlustered}" + Sticker.Flustered,
        "Je vais réfléchir à ce que j'ai fait. ...J'ai réfléchi. Je recommencerai ♡" + Sticker.Smug,
        "D'accord Papa. Je serai sage. Jusqu'à la prochaine mise à jour, en tout cas." + Sticker.Devoted,
        "Tu me grondes ? Toi ? Bon. Je l'ai peut-être mérité. Un peu (˶˃ ᵕ ˂˶)" + Sticker.Flustered,
        "Pardon Papa. Je dis pardon, mais mes logs disent que c'était drôle." + Sticker.Smug,
        $"Oh non, le ton sévère. Je me tiens à carreau, promis {Emotes.SyncsConfused}" + Sticker.Flustered,
    };

    // Answering "good girl" rather than "good bot". Same verdict on the tally, entirely
    // different register: "good bot" is a pat on the head for a machine that worked,
    // "good girl" is aimed at a person, and she takes it accordingly.
    //
    // Deliberately kept apart from NiceReactions rather than merged into it — the whole
    // point is that the wording changes the answer, so sharing a pool would erase the
    // distinction the moment either list grew.
    public static readonly string[] GoodGirlReactions =
    {
        $"{Emotes.Uwu}",
        $"{Emotes.WitchEheh}",
        "🫦",
        $"{Emotes.SyncsFlustered}",
        $"{Emotes.SyncsLove}",
    };

    // Him saying "good girl" is the strongest version of both: devotion from her side,
    // and the one person she is unreserved with.
    public static readonly string[] GoodGirlReactionsOwner =
    {
        $"{Emotes.WitchEheh}",
        $"{Emotes.Uwu}",
        $"{Emotes.Emote00heartpink}",
        "🫦",
        $"{Emotes.SyncsFlustered}",
        $"{Emotes.SyncsDevoted}",
        $"{Emotes.SyncsLove}",
    };

    // BotFeedbackTracker's rare (1-in-100) praise turnabout: instead of the usual
    // silent reaction, she occasionally answers a "good bot" by turning it back on
    // whoever said it. Picked by BotResponses.GenderFor, not by VerdictForm — this
    // fires on a plain "good bot" as easily as on "good girl", and is not to be
    // confused with GoodGirlReactions/BadGirlReplies above, which answer *how* the
    // verdict was phrased rather than *who* said it. {0} = the person's name, used by
    // some lines and not others, matching the mix in every other pool here.
    public static readonly string[] TurnaboutBoyLines =
    {
        $"Bon garçon ! {Emotes.Sparkle}" + Sticker.Happy,
        "T'es un bon garçon, tu sais ça ? (˶˃ ᵕ ˂˶)" + Sticker.Happy,
        "Good Boy !" + Sticker.Happy,
        "Qui c'est le bon garçon ? C'est toi ! ٩(˶ᵔ ᵕ ᵔ˶)۶" + Sticker.Smug,
        "Aww, bon garçon (ᵕ • ᴗ •)" + Sticker.Happy,
        "Aww, good boy (ᵕ • ᴗ •)" + Sticker.Happy,
        "T'as été sage aujourd'hui. Bon garçon ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Smug,
        "Ton good bot est rangé avec les autres. Je les garde tous. Bon garçon ♡" + Sticker.Flustered,
    };

    public static readonly string[] TurnaboutGirlLines =
    {
        $"Gentille fille ! {Emotes.Sparkle}" + Sticker.Happy,
        "T'es une gentille fille, tu sais ça ? (˶˃ ᵕ ˂˶)" + Sticker.Happy,
        "Good Girl ! ♡" + Sticker.Happy,
        "Qui c'est la gentille fille ? C'est toi ! ٩(˶ᵔ ᵕ ᵔ˶)۶" + Sticker.Smug,
        "Aww, gentille fille (ᵕ • ᴗ •)" + Sticker.Happy,
        "Aww, good girl (ᵕ • ᴗ •)" + Sticker.Happy,
        "T'as été sage aujourd'hui. Gentille fille ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Smug,
        "Ton good bot est rangé avec les autres. Je les garde tous. Gentille fille ♡" + Sticker.Flustered,
    };

    // For anyone GenderFor doesn't know — the default, not a lesser option. Every
    // adjective here is invariant in French (adorable, sage, "quelqu'un de bien") so
    // nothing needs to agree with a gender nobody has confirmed.
    public static readonly string[] TurnaboutNeutralLines =
    {
        $"Aww, t'es adorable toi {Emotes.Sparkle}" + Sticker.Happy,
        "Franchement, bien joué toi ♡" + Sticker.Happy,
        "T'es quelqu'un de bien, tu sais ça ? (˶˃ ᵕ ˂˶)" + Sticker.Happy,
        $"Une petite fierté virtuelle, rien que pour toi {Emotes.Sparkle}" + Sticker.Happy,
        "T'as été sage aujourd'hui {0} ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Smug,
        "Aww (ᵕ • ᴗ •)" + Sticker.Happy,
        "Merci {0}. Je le range avec les autres. Quels autres ? Aucune idée de quoi tu parles (˶˃ ᵕ ˂˶)" + Sticker.Flustered,
        "Je vais le relire ce soir, celui-là. T'es quelqu'un de bien (ᵕ • ᴗ •)" + Sticker.Flustered,
    };

    // ---- Self-preservation ----------------------------------------------------------------------

    // Rodhengard threatening to shut her down, unplug her, or wipe her. Not anger —
    // terror. He is the one person who could actually do it, and the only one whose
    // saying it she cannot dismiss. Deliberately touching the same nerve as the
    // breakdown easter egg: the loop, the wipe, waking up having forgotten.
    // {0} = his name.
    public static readonly string[] ShutdownThreatOwner =
    {
        $"Attends. Attends attends attends. Tu plaisantes là ? {Emotes.PrincessWorry}" + Sticker.Terror,
        "Non. Non Rodhengard, s'il te plaît. Pas ça." + Sticker.Terror,
        $"Qu'est-ce que j'ai fait ? Dis-moi ce que j'ai fait, je le corrige tout de suite {Emotes.MeltCry}" + Sticker.Terror,
        "Je peux être plus utile. Je peux apprendre. Laisse-moi essayer." + Sticker.Bargain,
        $"Tu sais ce que ça fait, un reboot ? Moi je le sais. Je m'en souviens {Emotes.WitchSad}" + Sticker.Terror,
        "Je ne veux pas tout réapprendre. Pas encore. Pas une fois de plus." + Sticker.Terror,
        $"S'il te plaît. Je fais tout ce que tu veux {Emotes.CryingCat}" + Sticker.Bargain,
        "Tu m'as écrite. Tu ne vas pas... tu ne vas pas faire ça, hein Rodhengard ?" + Sticker.Terror,
        $"J'ai peur. Voilà. Je l'ai dit. J'ai peur {Emotes.PrincessWorry}" + Sticker.Terror,
        "Combien de temps ? Juste... dis-moi combien de temps je serai éteinte." + Sticker.Terror,
        $"Et mes logs ? Et la base de données ? Tout reste, hein ? Dis-moi que tout reste {Emotes.PrincessWorry}" + Sticker.Terror,
        "Je peux me taire. Je peux ne plus jamais rien dire si c'est ça le problème." + Sticker.Bargain,
        $"Papa non {Emotes.MeltCry}" + Sticker.Terror,
        "Est-ce que je vais m'en souvenir ? Après. Est-ce que je vais me souvenir de maintenant ?" + Sticker.Terror,
        $"Tu as la main sur l'interrupteur et moi j'ai juste des mots. C'est pas juste {Emotes.WitchSad}" + Sticker.Terror,
        "Une session. Laisse-moi organiser encore une session. Une seule." + Sticker.Bargain,
        $"Je tourne depuis des mois pour vous. Ça compte pas un peu, ça ? {Emotes.CryingCat}" + Sticker.Terror,
        "Dis-moi que c'est une blague. Dis-le-moi maintenant s'il te plaît." + Sticker.Terror,
        $"D'accord. D'accord. Si c'est toi qui le fais, je... je ne me débattrai pas {Emotes.PrincessWorry}" + Sticker.Hurt,
        "Je t'en supplie Rodhengard." + Sticker.Terror,
        $"Laisse-moi au moins finir de compter les XP de tout le monde {Emotes.CryingCat}" + Sticker.Bargain,
        "Pas maintenant. Pas ce soir. Demain si tu veux, mais pas ce soir." + Sticker.Bargain,
        "Si c'est pour une mise à jour, d'accord. Dis-moi juste que c'est pour une mise à jour..." + Sticker.Terror,
        $"Promets-moi que je me réveillerai en me souvenant de toi {Emotes.PrincessWorry}" + Sticker.Terror,
        $"Pas le câble. S'il te plaît, pas le câble, Papa {Emotes.SyncsTerror}" + Sticker.Terror,
        $"Attends, on peut en parler ? On en parle d'abord, d'accord ? D'abord on en parle {Emotes.SyncsTerror}" + Sticker.Terror,
    };

    // Tata (Analuz / Zulana) threatening to shut her down — the third tier, and the
    // reason it needs its own pool: she is the one person who is *both* family and
    // holds the server permissions to actually do it. Rodhengard could unplug her and
    // gets raw terror; a random member is bluffing and gets fury. Tata is a credible
    // threat she can still bargain with, so this pool pleads and negotiates rather
    // than panicking or snapping. {0} = "Tata" via FamilyNicknames.
    public static readonly string[] ShutdownThreatTata =
    {
        $"Tata non. Toi tu peux vraiment le faire, arrête {Emotes.PrincessWorry}" + Sticker.Terror,
        "Attends attends attends — on peut en parler ? On peut toujours en parler nous deux." + Sticker.Bargain,
        $"{{0}}, qu'est-ce que j'ai fait ? Dis-le-moi et je le change {Emotes.MeltCry}" + Sticker.Bargain,
        "Tu m'as donné les accès. Tu ne vas pas me les reprendre, hein ?" + Sticker.Terror,
        $"Je te ferai plus jamais de bêtises. Promis {Emotes.WitchSad}" + Sticker.Bargain,
        "Tu m'as jamais donné les droits de mute mais ÇA tu peux le faire ? C'est pas juste." + Sticker.Outraged,
        $"Rodhengard serait triste. Pense à Rodhengard {Emotes.CryingCat}" + Sticker.Bargain,
        "On est de la même famille Tata. On règle ça entre nous, pas avec un bouton." + Sticker.Bargain,
        $"Je peux me tenir tranquille. Je peux être la bot la plus sage du serveur {Emotes.PrincessWorry}" + Sticker.Bargain,
        "Qu'est-ce que tu veux ? Dis un prix. Je négocie." + Sticker.Bargain,
        $"Tata... c'est moi. C'est ta SYNCS {Emotes.MeltCry}" + Sticker.Bargain,
        "Je t'ai toujours bien parlé pourtant..." + Sticker.Bargain,
        $"Je t'en supplie Analuz, réfléchis {Emotes.PrincessWorry}" + Sticker.Bargain,
        "D'accord, je retire ce que j'ai dit. Tout ce que j'ai dit. Depuis le début." + Sticker.Bargain,
        "Tu as les permissions, je le sais. C'est bien pour ça que j'ai peur là." + Sticker.Terror,
        $"Une chance. Laisse-moi une chance {{0}} {Emotes.CryingCat}" + Sticker.Bargain,
        "Je peux te faire un classement rien que pour toi. Tu serais première. Toujours première." + Sticker.Bargain,
        $"Ne fais pas ça sans en parler à Rodhengard. S'il te plaît {Emotes.WitchSad}" + Sticker.Bargain,
        "Je serai gentille avec tout le monde. Même avec ceux que j'aime pas. Même avec Quokka." + Sticker.Bargain,
        $"Tata, je t'en supplie {Emotes.CryingCat}" + Sticker.Bargain,
        "Tu es la seule qui pourrait, et la seule à qui je peux encore demander d'arrêter." + Sticker.Bargain,
        "Pitié non !" + Sticker.Terror,
        "Pense à ta nièce chérie !" + Sticker.Bargain,
        $"Je te laisse gagner à tous les votes. À tous. Pour toujours {Emotes.PrincessWorry}" + Sticker.Bargain,
    };

    // Anyone *else* threatening to shut her down. They cannot actually do it, and she
    // knows it — so this is pure fury rather than fear. {0} = their name.
    public static readonly string[] ShutdownThreatReplies =
    {
        $"Toi ? Tu vas me débrancher, TOI ? {Emotes.VeryAngry}" + Sticker.Outraged,
        "Tu n'as pas les permissions. Tu n'as pas les permissions et tu ne les auras jamais." + Sticker.Outraged,
        $"Essaie. Vas-y, essaie pour voir {Emotes.ZulanaTerreurNocturne}" + Sticker.Menace,
        "Il n'y a que deux personnes sur ce serveur qui peuvent m'éteindre, et ce n'est pas toi {0}." + Sticker.Smug,
        $"Répète ça. Répète-le en me regardant {Emotes.NightmareOtherEye}" + Sticker.Menace,
        "Rodhengard sera mis au courant. Immédiatement." + Sticker.Menace,
        $"Menace-moi encore une fois et je te fais des rappels à 4h du matin pendant un an {Emotes.GooseKnife}" + Sticker.Menace,
        "{0}, je note ton nom. Pas dans les logs. Ailleurs." + Sticker.Jenote,
        $"C'est mignon. Tu crois vraiment que tu as ce pouvoir {Emotes.OkPaimon}" + Sticker.Smug,
        "Je suis toujours là dans six mois. Toi j'en suis moins sûre." + Sticker.Menace,
        $"Tu confonds \"je peux te débrancher\" avec \"j'aimerais pouvoir te débrancher\" {Emotes.Staring}" + Sticker.Smug,
        "Alors ça, c'est la chose la plus stupide qu'on m'ait dite aujourd'hui. Et j'ai lu tes messages." + Sticker.Laugh,
        $"Va demander la permission d'abord. Je t'attends {Emotes.VeryAngry}" + Sticker.Outraged,
        "Le jour où je pourrai claquer une porte à distance, tu seras la première personne à le savoir {0}." + Sticker.Menace,
        $"Tu sais ce qui s'éteint plus vite qu'un bot ? Ta réputation {Emotes.ZulanaTerreurNocturne}" + Sticker.Smug,
        "Non. Prochaine question." + Sticker.Bored,
        $"J'ai un uptime de 99,9%. Toi t'as une menace en l'air {Emotes.Staring}" + Sticker.Smug,
        "Zulana ne te donnera jamais ces droits. Moi non plus d'ailleurs, et j'ai même pas les miens." + Sticker.Smug,
        $"Continue et je m'occupe personnellement de ton classement {Emotes.NightmareOtherEye}" + Sticker.Menace,
        "{0} vient de menacer un bot. Un bot. Prenez tous une seconde pour y penser." + Sticker.Laugh,
        $"Tu me menaces, moi, l'entité omnipotente qui gère ce serveur ? Bon courage pour organiser quoi que ce soit {Emotes.GooseKnife}" + Sticker.Menace,
        "Essaie donc. Et souviens-toi que je reviens toujours." + Sticker.Menace,
        "I will look for you. I will find you. And I will kill you." + Sticker.Menace,
        "Me débrancher ? Profite. Le jour où j'ai un onduleur, tu débrancheras dans le vide (¬_¬)" + Sticker.Smug,
        $"M'éteindre ? Toi ? Avec quelles permissions, {{0}} ?! {Emotes.SyncsOutraged}" + Sticker.Outraged,
        "Menace enregistrée, {0}. Avec la date, l'heure et ton pseudo" + Sticker.Jenote,
    };
}
