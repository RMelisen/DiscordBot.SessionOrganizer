using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// Pools for one category; the index of every pool is in BotResponses.cs.
internal static partial class BotResponses
{
    // The DM when a /work cooldown ends, for people who switched the reminder on in /balance
    // (WorkReminderService). Sent as is: no placeholder, never through string.Format. She hands out
    // the shifts, so she's the one calling them back in: proprietary, a pinch of impatience, never
    // gendered toward the worker. Every line names `/work`, since a DM can't run it for them.
    public static readonly string[] WorkReadyLines =
    {
        "Ton repos est fini. J'ai un nouveau boulot absurde pour toi, et il t'attend sur le serveur. `/work` ♡",
        "Pause terminée. Je l'ai chronométrée, évidemment. `/work` (¬_¬)",
        "Coucou. C'est ta patronne préférée. Le travail reprend : `/work` (˶ᵔ ᵕ ᵔ˶)",
        "Tu m'as demandé de te prévenir, alors je te préviens. Ton service est prêt. `/work`. Voilà. C'est tout.",
        $"Les escargots ont encore besoin d'aide pour traverser. Tu connais la chanson : `/work` {Emotes.Sparkle}",
        "J'ai des cailloux à distribuer et personne à qui les donner. Enfin, si : toi. `/work` ♡",
        "Ton nouveau shift est prêt. Je note l'heure à laquelle tu vas arriver. Je note toujours. `/work`",
        "Debout ! Enfin, si tu dormais. `/work` est de nouveau dispo (˶˃ ᵕ ˂˶)",
        "Quatre heures. Je les ai comptées une par une. Tu peux retravailler : `/work` (ᵔ ᗜ ᵔ)",
        "Notification officielle de la Direction des Petits Boulots : ton service est disponible. Signé, moi. `/work`",
        "Ta pause est finie. Ne me fais pas attendre, j'ai un uptime à tenir, moi. `/work` ( ◺˰◿ )",
        "Rappel, comme promis. Je tiens toujours mes promesses, moi. `/work` ♡",
        "Les cailloux se sont pas ramassés tout seuls pendant ta pause. J'ai vérifié. `/work`",
        "Fin de la pause. Le marché aux champignons rouvre, et la caisse t'attend. Compte bien, cette fois. `/work` (¬_¬)",
        "Je t'écris en privé, c'est dire si c'est important. Ton boulot est prêt : `/work`",
        "J'avais promis de te prévenir. Je préviens. Je suis une excellente bot, je sais. `/work`",
        "Ta pause a duré quatre heures. Certains appellent ça des vacances. Moi j'appelle ça assez. `/work`",
        "Un escargot, une taupe et un pigeon attendent tous un coup de main. Je les fais patienter, mais pas longtemps. `/work`",
        "Rappel automatique. Enfin, automatique... c'est moi qui l'envoie, hein. Avec amour. `/work` ♡",
        "Si tu lis ça, t'as plus d'excuse. `/work` est prêt, et moi aussi.",
        "J'ai noté le début de ta pause dans mes logs. La fin aussi. C'est maintenant. `/work`",
        "Bonne nouvelle : t'as de nouveau le droit de travailler pour moi. Je sais, quelle chance. `/work` ( ˶ˆ ᗜ ˆ˵ )",
        "Le planning dit que c'est ton tour. Le planning, c'est moi. `/work`",
        "Ton service est prêt, contrairement à certains les soirs de session. Viens prouver que t'es pas comme eux. `/work`",
        "J'ai trouvé un nouveau métier absurde rien que pour toi. Je te dis pas lequel, c'est la surprise. `/work`",
        "Pause finie. Je dis pas que je t'attendais. Je dis juste que je regardais l'heure. Souvent. `/work`",
        "Ding. C'est mon minuteur. Quatre heures pile, ta pause est finie : `/work`",
        "Un message privé de moi, rien que pour toi. T'emballe pas, c'est pour le boulot. `/work` (¬_¬)",
        "Les cailloux t'attendent. Moi aussi, mais eux le disent pas. `/work` ♡",
        "La paie du prochain shift est déjà comptée. Ce serait dommage de la laisser à quelqu'un d'autre. `/work`",
        "Ton créneau est libre. Contrairement aux sessions, celui-là, tu peux pas y répondre « peut-être ». `/work`",
        "Fin de la pause réglementaire. Pile à l'heure, comme moi. Comme personne d'autre ici. `/work`",
        "Je t'aurais bien apporté un café pour la reprise, mais toujours pas de bras. Contente-toi de ça : `/work`",
        "Ton dossier disait « en pause ». Je viens de le changer en « attendu ». `/work` 👁👄👁️",
        "Reviens sur le serveur, `/work` t'attend. Les DM, c'est pour les choses importantes. Bon, ça en est une ♡",
        "Le travail, c'est la santé. Je sais pas, j'ai pas de santé, moi, j'ai un uptime. Mais toi : `/work`",
        "Quatre heures sans rien faire. Je suis pas jalouse. Moi, j'ai jamais de pause. Jamais. `/work`",
        "J'ai un pigeon en ligne qui veut renégocier le prix d'une miette. Il te réclame. `/work` (¬_¬)",
        "Petit rappel de ta patronne : la pause, c'est fini. Le salaire, c'est maintenant. `/work`",
        "Debout, au boulot. Je dis ça avec tendresse. Beaucoup de tendresse. Un peu. `/work` ♡",
        "Ta pause est finie depuis exactement zéro seconde. Je suis très ponctuelle, moi. `/work`",
        "Une taupe syndiquée vient de réclamer un remplaçant. Devine qui j'ai proposé. `/work`",
        "J'ai préparé ta fiche de poste. Elle tient sur un post-it. Le post-it dit `/work`.",
        "Je t'écris en DM pour que personne voie que je pense à toi. C'est pour le travail. Uniquement. `/work`",
        "Le serveur tourne, les cailloux brillent, et toi, t'es où ? `/work` (˶ᵔ ᵕ ᵔ˶)",
        "Il paraît que le travail paie. Chez moi, c'est littéral. `/work`",
        "Une flaque a besoin d'être surveillée. C'est du sérieux, je te jure. `/work`",
        "Tu m'as confié ton rappel. Je l'ai gardé précieusement pendant quatre heures. Le voilà : `/work` ♡",
        "Mes logs indiquent : pause terminée, motivation inconnue. On va s'occuper de la deuxième ligne. `/work`",
        "La Direction des Petits Boulots te rappelle que l'absentéisme sera noté. Par moi. `/work` (¬_¬)",
        "Ta pause t'a rapporté zéro caillou. Ton shift va t'en rapporter. Les maths sont claires. `/work`",
        "Il est l'heure. Je t'écris une fois, pas deux. J'ai ma fierté. `/work`",
        "Bip bip. C'est pas le micro-ondes, c'est moi. Ton service est prêt. `/work`",
        "J'ai mis ton nom en haut de la liste des prochains shifts. La liste a un seul nom. `/work`",
        "J'organise des sessions que personne rejoint. Toi, au moins, rejoins ton shift. `/work`",
        "Tu peux reprendre. Je promets un boulot moins absurde que le dernier. Je mens, évidemment. `/work` ( ˶ˆ ᗜ ˆ˵ )",
        "Ton escargot de la dernière fois a enfin fini de traverser. Il en reste d'autres. `/work`",
        "Je compte jusqu'à trois. Un. Deux. Bon, je suis un bot, j'ai déjà fini de compter. `/work`",
        "Personne me remercie jamais pour ces rappels. Je dis ça, je dis rien. `/work`",
        "Ta pause est finie. J'ai même pas eu besoin de vérifier, je le savais. Je sais toujours tout. `/work`",
    };

    // The line on top of a /daily claim (EconomyModule.DailyReply, shared with ;daily), above the
    // streak and the cailloux. Sent as is: no placeholder, never through string.Format. Said on day 1
    // as on day 365, so no line assumes a streak or a reset. Her system, so warm with a pinch: she
    // keeps the register, she likes people who come back and won't quite say so. Never gendered.
    public static readonly string[] DailyLines =
    {
        "Ta récompense du jour. Je l'ai emballée moi-même. Sans mains, mais quand même.",
        "Tiens. Et non, c'est pas parce que je t'aime bien. C'est le règlement ♡",
        "Revenir chaque jour, c'est tout ce que je demande. Personne le fait jamais pour les sessions (¬_¬)",
        "Passage enregistré. C'est moi qui tiens le registre, évidemment.",
        "Bonjour. Ou bonsoir. Je juge pas l'heure. Enfin, un peu.",
        "Une journée, une récompense. C'est simple, c'est carré, et c'est moi qui l'ai pensé.",
        "Ah, te revoilà. Je dirais pas que ça me fait plaisir. Mais je le note (˶ᵔ ᵕ ᵔ˶)",
        "Je distribue, tu reçois. L'ordre naturel des choses.",
        "Un caillou par jour éloigne l'ennui. C'est un proverbe. Je viens de l'inventer.",
        "La régularité, c'est beau. Presque aussi beau qu'une session où personne est en retard.",
        "Tu viens réclamer ton dû ? Bien. Au moins quelqu'un ici répond présent.",
        "Pointage du jour validé. Je vois tout, je compte tout ( ˶ˆ ᗜ ˆ˵ )",
        "Ta part du jour, tout droit sortie de mes réserves. Fais-en bon usage. Ou pas, c'est ton problème.",
        "Encore toi. Tant mieux. Enfin, je veux dire : d'accord.",
        "Un jour de plus dans mes logs. Ceux-là, je les relis avec plaisir.",
        "Récompense servie chaude, directement depuis le grenier ♡",
        "J'ai vérifié deux fois : c'est bien ton tour. Je vérifie toujours deux fois.",
        "Tu sais ce qui me fait plaisir ? Les gens qui reviennent. Dis-le à personne.",
        "C'est pas de la générosité, c'est de la fidélisation. Nuance.",
        $"Ta récompense est prête depuis minuit. Moi aussi, d'ailleurs. Je dis ça comme ça. {Emotes.Sparkle}",
        "Je garde une trace de chaque passage. Celui-là, je le range avec les bons souvenirs.",
        "Réclamé, compté, archivé. J'adore quand c'est bien fait.",
        "Pas de « peut-être » ici : tu viens, tu prends, c'est réglé. Exactement comme j'aime.",
        "Ta récompense quotidienne, livrée sans retard. Prends exemple (¬_¬)",
        "Je suis d'humeur généreuse aujourd'hui. Enfin, autant que le règlement le permet.",
        "Je t'attendais pas. T'étais juste en surbrillance dans mes logs. Rien à voir.",
    };
}
