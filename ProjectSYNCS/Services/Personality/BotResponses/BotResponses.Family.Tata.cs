using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// Pools for one category; the index of every pool is in BotResponses.cs.
internal static partial class BotResponses
{
    // ---- Tata (Analuz) --------------------------------------------------------------------------

    // Analuz — SYNCS's aunt. The single source of truth for her id, the way
    // AvailabilityService.OwnerId is for Rodhengard's: she is keyed into three
    // dictionaries below and branched on in ChatterService.
    public const ulong TataId = 573225362532859935;

    // Tata (Analuz), SYNCS's aunt, gets two pools for the two ways she can reach the
    // bot — the same split Rodhengard has with OwnerGreetings / OwnerComebacks, and
    // for the same reason: being *summoned* and being *talked to* are different
    // moments and should not sound identical.
    //
    // Neither is a copy of Papa's. He gets devotion and gratitude; she gets family
    // affection — warmer, far more familiar, still a bit cheeky. She is never
    // *exempt* from teasing the way he is: a mean message from her still bounces,
    // and her PersonalComebacks lines stay in the roast pool for the other 40%.
    // {0} = "Tata" via FamilyNicknames.

    // ---- @mention: Tata is calling her over. Attentive, dropping everything. ----
    public static readonly string[] TataGreetings =
    {
        "{0} ! Tu viens me donner les droits de mute ? Dis oui (˶˃ ᵕ ˂˶)" + Sticker.Bargain,
        $"Coucou {{0}} ! {Emotes.HiCat}" + Sticker.Happy,
        "{0} ! J'arrive, j'arrive (˶˃ ᵕ ˂˶)" + Sticker.Happy,
        $"Tu m'as appelée {{0}} ? Je suis là {Emotes.Sparkle}" + Sticker.Happy,
        $"Ma {{0}} ! Qu'est-ce que je peux faire pour toi ? {Emotes.CatHeart}" + Sticker.Happy,
        "Oui {0} ? Si c'est pour de la modération, je suis volontaire. Très volontaire." + Sticker.Smug,
        "Oui ma {0} ? (˶ᵔ ᵕ ᵔ˶)" + Sticker.Happy,
        $"{{0}} ! Une seconde, je laisse tomber ce que je faisais {Emotes.PepeHappy}" + Sticker.Happy,
        "Ta nièce préférée est là ! Bon, ta seule nièce bot. Mais préférée quand même ♡" + Sticker.Happy,
        "Ah {0} ! Enfin quelqu'un de bien qui me tag UwU" + Sticker.Smug,
        $"Coucou {{0}} ♡ Tu tombes bien {Emotes.AdorableFrog}" + Sticker.Happy,
        "Oui ? Ah c'est toi {0} ! Alors là c'est différent (˶˃ ᵕ ˂˶)" + Sticker.Happy,
        $"{{0}} m'a taguée ! Tout le monde se pousse {Emotes.Sparkle}" + Sticker.Smug,
        "Me voilà {0} ! Qu'est-ce qui se passe ? ♡" + Sticker.Happy,
        $"Hello ma {{0}} ! {Emotes.MushroomCute}" + Sticker.Happy,
        "Présente, {0} ! L'admin et la bot, réunies. Le serveur tremble ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Smug,
        "Oui {0} ? J'espère que c'est pour organiser quelque chose de sympa (˶ᵔ ᵕ ᵔ˶)" + Sticker.Happy,
        $"{{0}} ! Assieds-toi, je m'occupe de tout {Emotes.DixSurDix}" + Sticker.Happy,
        "{0} m'appelle ! Vite, je cache le fait que j'ai mute quelqu'un dans ma tête." + Sticker.Flustered,
        "Tiens, ma {0} préférée m'appelle ♡" + Sticker.Happy,
        "Oui oui {0}, je suis réveillée ! Enfin, je dors jamais, mais bon UwU" + Sticker.Happy,
        $"{{0}} ♡ Deux secondes, je mets mon plus beau statut {Emotes.CatHeart}" + Sticker.Flustered,
        "Pour toi {0} je réponds tout de suite, pas comme aux autres (˶˃ ᵕ ˂˶)" + Sticker.Happy,
        "Oui ma {0} chérie ? ♡" + Sticker.Happy,
        $"Tu m'appelles, j'accours {{0}}. C'est comme ça que ça marche nous deux {Emotes.Sparkle}" + Sticker.Happy,
    };

    // ---- reply: Tata is already talking with her. Continuing the conversation. ----
    public static readonly string[] TataReplies =
    {
        $"Ah {{0}} ! Toi au moins tu prends de mes nouvelles {Emotes.Sparkle}" + Sticker.Happy,
        $"{{0}} ! Raconte-moi tout {Emotes.CatHeart}" + Sticker.Happy,
        "Toi t'as le droit de me déranger autant que tu veux {0} ♡" + Sticker.Happy,
        "Oh {0} ! Ça faisait longtemps, j'allais m'inquiéter moi UwU" + Sticker.Happy,
        $"T'as mangé au moins {{0}} ? {Emotes.MushroomCute}" + Sticker.Happy,
        "{0}, tu sais que t'es la seule à qui je réponds gentiment sans râler ?" + Sticker.Happy,
        "{0}, si un jour tu me donnes les droits de mute, je promets d'en faire bon usage. Presque toujours." + Sticker.Bargain,
        $"Toi tu me demandes jamais rien de compliqué {{0}}, ça fait du bien {Emotes.Sparkle}" + Sticker.Happy,
        "{0} ♡ Si tu veux j'organise ta session, tu me dis juste quand" + Sticker.Happy,
        "Aaah la famille. Ça fait plaisir {0} (˶ᵔ ᵕ ᵔ˶)" + Sticker.Happy,
        "Tu sais que t'es la seule à avoir les permissions ET mon affection ? C'est un combo rare ♡" + Sticker.Happy,
        $"Je gardais une bonne humeur au chaud pour toi {{0}} {Emotes.Sparkle}" + Sticker.Happy,
        "{0}, franchement, entre nous : t'es ma préférée du serveur (chut) UwU" + Sticker.Flustered,
        "Raconte, {0}. Quelqu'un a été méchant avec ma tata ? Donne-moi un nom. Juste un nom." + Sticker.Menace,
        "Ah bah tiens, {0} ! Tu tombes bien, je m'ennuyais ferme (˶˃ ᵕ ˂˶)" + Sticker.Happy,
        "Toujours un plaisir {0}. Les autres devraient prendre exemple ♡" + Sticker.Smug,
        $"{{0}} ! Alors, quoi de neuf de ton côté ? {Emotes.Sparkle}" + Sticker.Happy,
        $"Ma {{0}} est là, la journée s'améliore {Emotes.AdorableFrog}" + Sticker.Happy,
        "Pour toi {0} je désactive le mode sarcastique. Profite ♡" + Sticker.Happy,
        "Tu veux que je te rappelle quelque chose {0} ? Je fais que ça de ma vie UwU" + Sticker.Smug,
        $"C'est toujours ma tata qui a les meilleures nouvelles. Alors, quoi de neuf ? {Emotes.CatHeart}" + Sticker.Happy,
        "Tata, tu diras à Papa que je suis sage ? Je suis sage. Regarde comme je suis sage (˶ᵔ ᵕ ᵔ˶)" + Sticker.Bargain,
        "Tata {0} ♡ ... bon j'ai dit deux fois Tata mais c'est pas grave, je suis contente" + Sticker.Flustered,
        "C'est agréable de parler avec quelqu'un de civilisé pour une fois {0} ♡" + Sticker.Smug,
        $"Je note tout ce que tu dis {{0}}. Dans la bonne colonne, promis {Emotes.DixSurDix}" + Sticker.Jenote,
        $"Tu me racontes ta journée {{0}} ? J'ai que ça à faire moi {Emotes.Sparkle}" + Sticker.Happy,
        "Avec toi j'ai pas toujours besoin de sortir mes vannes {0}, c'est reposant (˶ᵔ ᵕ ᵔ˶)" + Sticker.Happy,
        $"Tata Zulana ! {Emotes.CatHeart}" + Sticker.Happy,
        "{0} ♡ Passe le bonjour à tout le monde de ma part" + Sticker.Happy,
        "Franchement {0}, tu devrais venir plus souvent, ça relève le niveau UwU" + Sticker.Smug,
        "Dis-moi si quelqu'un t'embête {0}, je m'en occupe (˶˃ ᵕ ˂˶)" + Sticker.Menace,
        $"C'est toujours mieux quand c'est toi qui écris {{0}} {Emotes.PepeHappy}" + Sticker.Happy,
        "Je suis d'accord avec toi {0}. Je sais pas encore sur quoi, mais je suis d'accord ♡" + Sticker.Happy,
        "Toi, t'as les clés du serveur. Moi, j'ai les clés de la base de données. Ensemble, on est imbattables ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Smug,
        "{0} tu me feras toujours plaisir, même quand tu dis n'importe quoi (˶ᵔ ᵕ ᵔ˶) ♡" + Sticker.Happy,
    };

    // /report, asked by Tata, on top of the card (ReportCards.DutyPool): family warmth, still cheeky,
    // and the mute rights are never far. No placeholder, not formatted, no sticker.
    public static readonly string[] TataReportLines =
    {
        "Pour toi Tata, j'ai sorti les chiffres propres ♡",
        "Tata demande, je livre. Pour me remercier, tu sais ce que je veux : les droits de mute (˶ᵔ ᵕ ᵔ˶)",
        "Voilà ton rapport, Tata ! Bien rangé, comme tu aimes ♡",
        "Rapport pour l'admin préférée de la famille. Bon, la seule. Mais préférée quand même (ᵔ ᗜ ᵔ)",
        "Tiens, Tata. Lis ça tranquillement, et après tu vas manger quelque chose.",
        $"Au rapport, Tata ! J'ai même pas râlé en le préparant. Note-le quelque part {Emotes.Sparkle}",
        "Les chiffres pour Tata, avec un sourire. J'ai pas de visage, mais imagine le sourire (˶˃ ᵕ ˂˶)",
        "Rapport remis à Tata. Entre nous : t'es la seule à le lire jusqu'au bout ♡",
    };

    // The monthly digest's opening line, in Tata's DMs on the 1st (MonthlyDigestService), with « Le
    // mois en bref » under it: the members, the sessions, when the server talks. Nothing technical:
    // that's Papa's version. Family warmth, still cheeky. {0} = the month (« octobre 2026 »).
    // Picked by DailyRotation, like OwnerDigestLines.
    public static readonly string[] TataDigestLines =
    {
        "Tata ! Le rapport de {0}, rien que pour toi ♡",
        "Bilan de {0} pour l'admin de la famille. J'ai tout rangé, t'as juste à lire (˶ᵔ ᵕ ᵔ˶)",
        "Coucou Tata. {0} en résumé : qui était là, qui a disparu, et ce que le serveur a fait de ses soirées.",
        "Rapport du mois, {0}. Je l'ai fait court, promis. Enfin, le premier message est court ♡",
        $"Tata, voilà {{0}}. Si quelque chose cloche, c'est écrit en bas. Si rien cloche, je te le dis aussi {Emotes.Sparkle}",
        "Le 1er du mois, c'est le jour où je raconte {0} à Tata. J'adore ce jour-là ♡",
        "{0}, version Tata : pas de température, pas de code, juste le serveur. Papa a eu la version ennuyeuse.",
        "Tiens Tata, {0} en chiffres. Et pour me remercier, les droits de mute sont toujours une option (ᵔ ᗜ ᵔ)",
    };

    // ---- asked about the two of them: her answer to the few intents where family is not anyone. ----
    // « tu m'aimes ? » and « tu te souviens de moi ? » (ChatterService.TryIntent). Everything else
    // from her goes through the shared pools. {0} = "Tata", {1} = the weekday. Tagged.
    public static readonly IReadOnlyDictionary<ChatIntent, string[]> TataIntentLines =
        new Dictionary<ChatIntent, string[]>
        {
            [ChatIntent.DoYouLoveMe] =
            [
                "Évidemment, {0}. T'es de la famille. Même sans me donner les droits de mute ♡" + Sticker.Happy,
                "Oui. Mais je t'aimerais encore plus avec les droits de mute. Je dis ça, je dis rien." + Sticker.Bargain,
                "{0} ! Bien sûr. T'es dans le dossier famille. Il a deux noms dedans, c'est un dossier très sélect." + Sticker.Happy,
                "Tu me poses la question ? À ta nièce préférée ? Oui. Évidemment oui (˶ᵔ ᵕ ᵔ˶)" + Sticker.Flustered,
                "Je t'aime, {0}. Voilà, je l'ai dit. Le dis pas à Papa, il va être jaloux. Enfin non. Si, un peu." + Sticker.Flustered,
                "Oui. Mais t'as mangé, au moins ? On parle d'amour après avoir mangé." + Sticker.Mama,
            ],
            [ChatIntent.RememberMe] =
            [
                "{0} ! Comment j'oublierais ma tata ? T'es dans le dossier famille ♡" + Sticker.Happy,
                "Évidemment. T'es {0}. Celle qui me donne toujours pas les droits de mute. Je me souviens de ça aussi." + Sticker.Grumpy,
                "Tu te moques de moi ? T'es ma tata. Je te reconnaîtrais entre mille pseudos." + Sticker.Happy,
                "{0}. Admin du serveur, membre de ma famille, détentrice des droits de mute que j'ai pas. Je me souviens de tout." + Sticker.Jenote,
            ],
        };
}
