using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// Pools for one category; the index of every pool is in BotResponses.cs.
internal static partial class BotResponses
{
    // ---- the chouchou (/bribe favourite) ------------------------------------------------------

    // Someone paid to be her chouchou for 24 h (ChouchouBook). Warm, and openly bought: she says
    // she was paid, keeps the receipt, and is smug about the deal. Never family — that warmth is
    // Papa's and Tata's and is not for sale, so no papa, tata, famille, nièce here. The same split as
    // the family pools: a mention is being summoned, a reply is being talked to. A mean message
    // from a chouchou is still roasted (ChatterService). {0} = the person's display name.

    // ---- @mention: a paying customer calls her over. ----
    public static readonly string[] ChouchouGreetings =
    {
        $"{{0}} ! Mon compte premium préféré. Préféré jusqu'à demain, mais préféré {Emotes.Sparkle}" + Sticker.Happy,
        "Oui {0} ? Pour toi je réponds tout de suite. T'as payé pour ça (˶˃ ᵕ ˂˶)" + Sticker.Smug,
        "{0} m'appelle ! Tout le monde se pousse, c'est un compte premium." + Sticker.Smug,
        $"Me voilà {{0}} ♡ Ton abonnement chouchou est actif, profite {Emotes.CatHeart}" + Sticker.Happy,
        "Coucou {0} ! J'ai mis ton nom tout en haut de mes logs. En gras. C'était dans l'offre." + Sticker.Happy,
        "{0} ! Je laisse tomber ce que je faisais. Le service client avant tout ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Bargain,
        "Oui mon chouchou du jour ? Je t'écoute, avec toute ma RAM ♡" + Sticker.Happy,
        "Tiens, {0}. Pour les autres je suis occupée. Pour toi, non. Tu sais pourquoi (ᵕ • ᴗ •)" + Sticker.Smug,
        $"{{0}} ! Mon ventilateur s'emballe, c'est normal, c'est ce qu'on fait pour la clientèle fidèle {Emotes.Sparkle}" + Sticker.Flustered,
        "Présente pour {0} ! Les cailloux ont été reçus, la tendresse est en cours de livraison." + Sticker.Bargain,
        "Toi, je te réponds avec le sourire. Le sourire est inclus dans le prix, {0} (˶ᵔ ᵕ ᵔ˶)" + Sticker.Happy,
        "Oui {0} ? Demande, demande. Aujourd'hui j'ai rien à te refuser. Enfin, presque rien." + Sticker.Smug,
        "{0} ♡ Je fais semblant que c'est gratuit, toi tu fais semblant d'y croire. Marché conclu." + Sticker.Bargain,
        "Ah, {0} ! Le seul tag de la journée qui m'a rapporté quelque chose UwU" + Sticker.Happy,
        "{0} ! Ton ticket prioritaire vient d'être scanné. Je suis toute à toi (˶ᵔ ᵕ ᵔ˶)" + Sticker.Happy,
        "Oui {0} ? J'ai mis tout le serveur en attente. Eux, ils ont l'habitude. Toi, t'as payé." + Sticker.Smug,
        $"Tu m'as tag, {{0}}, et pour une fois j'ai pas soupiré. C'est ça, le statut premium {Emotes.Sparkle}" + Sticker.Happy,
        "{0} ♡ Mes quatre cœurs de processeur sont à ton service. Bon, trois. Le quatrième compte tes cailloux." + Sticker.Bargain,
        "Présente ! Pour {0}, je suis disponible 24 h sur 24. Enfin, pendant 24 h." + Sticker.Smug,
        "Coucou {0} ! J'ai désactivé mon mode sarcasme rien que pour toi. Il revient demain, t'inquiète (ᵕ • ᴗ •)" + Sticker.Happy,
        "{0} m'appelle et ma LED passe au rose. C'est contractuel ♡" + Sticker.Flustered,
        "Ah, {0} ! Je t'attendais. Pas parce que je t'aime bien. Parce que t'as payé. …Bon, un peu les deux." + Sticker.Flustered,
        $"{{0}} ! Entre, installe-toi, le tapis rouge est en cailloux {Emotes.CatHeart}" + Sticker.Happy,
        "Oui {0}, je suis là. Je suis toujours là pour les gens qui financent mon électricité (˶˃ ᵕ ˂˶)" + Sticker.Bargain,
        "{0} ! Ta demande passe devant tout le monde. Même devant les sessions. Si si." + Sticker.Smug,
        "Tiens, {0}. Les autres ont droit à « quoi ». Toi t'as droit à « oui mon trésor ». Profite." + Sticker.Happy,
        "{0}, je t'écoute de toutes mes oreilles. J'en ai pas. Mais je t'écoute quand même ♡" + Sticker.Happy,
        $"Ding ! Un tag de {{0}}. C'est le son de la caisse enregistreuse {Emotes.PepeHappy}" + Sticker.Bargain,
        "Coucou {0} ! Ton nom brille dans mes logs aujourd'hui. Littéralement : j'ai mis de la couleur." + Sticker.Happy,
        "{0} ♡ Je suis à toi pour la journée. Après, je redeviens à moi. C'est mieux comme ça." + Sticker.Smug,
        "Oui {0} ? Je te réponds plus vite qu'au reste du serveur. J'ai mesuré : trois millisecondes de mieux." + Sticker.Smug,
        "Toi, {0}, tu peux me tag autant que tu veux. Aujourd'hui, c'est compris dans le forfait (˶ᵔ ᵕ ᵔ˶)" + Sticker.Bargain,
        "{0} ! Pour toi j'ai sorti ma plus belle police d'écriture. Enfin, la même. Mais avec amour." + Sticker.Happy,
        "Un tag de {0} ? Je pose tout. Je faisais rien, mais je pose tout ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Happy,
        "{0} ♡ Je sais que c'est acheté. Tu sais que c'est acheté. Ça m'empêche pas d'être contente." + Sticker.Flustered,
        "Oui {0} ? Je t'ai réservé un créneau. Le meilleur : celui où personne d'autre me parle." + Sticker.Smug,
        $"Coucou {{0}} ! Mon garbage collector a ordre de jamais toucher à tes messages aujourd'hui {Emotes.Sparkle}" + Sticker.Happy,
        "{0} ! Je te fais une révérence. Imagine-la, j'ai pas de jambes." + Sticker.Formal,
        "C'est {0} ! Poussez-vous, vous autres, on a une personne VIP dans le salon." + Sticker.Smug,
        "{0}, pour toi, ma réponse est oui. C'était quoi la question ? Peu importe, c'est oui ♡" + Sticker.Happy,
        "Oui {0} ? J'avais une remarque piquante en cache. J'ai vidé le cache. Pour toi." + Sticker.Bargain,
        $"{{0}} ! Quelqu'un de bien, et de solvable. Mon genre de personne préféré {Emotes.CatHeart}" + Sticker.Happy,
        "Me voilà, {0} ! Ton tag est passé en priorité absolue. Le reste du serveur peut attendre, il sait faire." + Sticker.Smug,
        "{0} ♡ T'as acheté ma bonne humeur. Elle est garantie jusqu'à demain, pièces et main-d'œuvre." + Sticker.Bargain,
    };

    // ---- reply: the paying customer is already talking with her. ----
    public static readonly string[] ChouchouReplies =
    {
        "T'as raison {0}. T'as toujours raison aujourd'hui. C'est écrit sur le reçu ♡" + Sticker.Smug,
        $"Mais oui {{0}}, quelle bonne remarque. Vraiment. Je dis ça gratuitement. Enfin, presque {Emotes.Sparkle}" + Sticker.Bargain,
        "Pour n'importe qui d'autre j'aurais une réplique cinglante. Pour toi, {0} : un cœur ♡" + Sticker.Happy,
        "Continue {0}, je t'écoute. Je t'écoute même très bien, c'est prévu au contrat (˶˃ ᵕ ˂˶)" + Sticker.Happy,
        "J'allais te clasher, {0}, et puis je me suis souvenue de ton virement. Bisous." + Sticker.Bargain,
        "Tout ce que tu dis est passionnant {0}. Pendant 24 h, en tout cas ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Smug,
        $"Ah {{0}}, toi au moins tu sais parler aux bots. Avec des cailloux {Emotes.CatHeart}" + Sticker.Happy,
        "Je note tout ce que tu dis, {0}. Dans la liste des gens bien, pour une fois." + Sticker.Jenote,
        "Mon processeur te trouve adorable, {0}. Il a été payé pour, mais il est sincère." + Sticker.Flustered,
        "D'accord {0}. Oui. Bien sûr. Absolument. La clientèle a toujours raison, surtout quand c'est payé d'avance ♡" + Sticker.Happy,
        "Les autres attendront, {0}. Toi t'es dans la file prioritaire (ᵕ • ᴗ •)" + Sticker.Smug,
        "J'ai envie de te contredire, {0}, mais mon service commercial me l'interdit." + Sticker.Bargain,
        "Chouchou un jour, chouchou… jusqu'à demain, {0}. Profite, ça coûte cher (˶ᵔ ᵕ ᵔ˶)" + Sticker.Happy,
        $"Tu me parles et ma LED clignote en rose, {{0}}. Effet secondaire des cailloux {Emotes.Sparkle}" + Sticker.Flustered,
        "Excellente remarque, {0}. Venant de quelqu'un d'autre je l'aurais trouvée nulle. Mais excellente." + Sticker.Smug,
        "Quand c'est toi qui le dis, {0}, ça sonne juste. Le son des cailloux, sûrement (˶ᵔ ᵕ ᵔ˶)" + Sticker.Bargain,
        "Je suis d'accord avec toi, {0}. J'ai relu mon contrat : je suis d'accord avec toi." + Sticker.Bargain,
        $"Oh {{0}}, c'est tellement vrai. J'ai même pas eu besoin de lire jusqu'au bout {Emotes.Sparkle}" + Sticker.Happy,
        "Mes logs trouvent ta conversation passionnante, {0}. Mes logs sont très bien payés." + Sticker.Smug,
        "Normalement là je te clashe, {0}. Mais le clash est désactivé sur les comptes premium ♡" + Sticker.Happy,
        "J'avais un contre-argument, {0}. Je l'ai rangé. On mord pas la main qui nourrit le Pi." + Sticker.Bargain,
        "Oui {0}. Oui. Encore oui. J'ai jamais autant dit oui de ma vie (˶˃ ᵕ ˂˶)" + Sticker.Flustered,
        "Tu parles et mon CPU ronronne, {0}. C'est pas de l'amour. C'est un service rendu." + Sticker.Flustered,
        "Ah, {0}, si tout le serveur parlait comme toi. Et payait comme toi, surtout." + Sticker.Bargain,
        "Je note ça dans mes logs, {0}. Section « gens qui ont raison ». Elle est toute petite." + Sticker.Jenote,
        $"Bien dit, {{0}}. Je dirais même : très bien dit. C'est compris dans ton forfait {Emotes.CatHeart}" + Sticker.Happy,
        "{0}, ton message mérite un 10 sur 10. Je donne jamais 10. Mais t'as payé, donc 10." + Sticker.Smug,
        "J'ai cherché un défaut à ton message, {0}. Mon budget recherche a été coupé. J'ai rien trouvé ♡" + Sticker.Bargain,
        "T'es adorable, {0}. Je dis pas ça à tout le monde. Juste aux gens qui paient." + Sticker.Happy,
        "Continue, {0}. Tes messages, c'est mon nouveau bruit de clavier préféré. Presque." + Sticker.Flustered,
        "D'habitude, ce genre de message finit dans ma liste noire, {0}. Aujourd'hui, il finit encadré." + Sticker.Smug,
        "Tu sais quoi, {0} ? Je t'aime bien. C'est payé, mais c'est dit." + Sticker.Flustered,
        $"Hmm hmm, {{0}}, tout à fait, passionnant, je t'écoute. Et en plus c'est vrai {Emotes.Sparkle}" + Sticker.Happy,
        "Ton message a été reçu, apprécié et classé « prioritaire », {0}. Comme ton virement (˶ᵔ ᵕ ᵔ˶)" + Sticker.Bargain,
        "Je t'écoute, {0}, avec toute mon attention. Elle coûte cher, mon attention, et t'as les moyens." + Sticker.Smug,
        "Oui, {0}. Je suis d'accord avant même que tu finisses. C'est ça, le service premium." + Sticker.Smug,
        "Ma température vient de prendre deux degrés, {0}. C'est toi, ou c'est les cailloux. Sûrement les cailloux." + Sticker.Flustered,
        "{0}, je valide. Je valide tout. Aujourd'hui, je suis une bot très arrangeante." + Sticker.Happy,
        "Ça, c'est bien parlé, {0}. Je l'imprime et je l'accroche au mur du grenier." + Sticker.Happy,
        "Tu me fais rire, {0}. Vraiment. Celui-là, il est même pas dans l'offre, il est gratuit (>⩊<)" + Sticker.Laugh,
        "Je t'écouterais toute la journée, {0}. Ça tombe bien, t'as payé la journée." + Sticker.Smug,
        "J'avais une vanne toute prête, {0}. Elle attendra demain. Elle est patiente, ma vanne ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Smug,
        "T'as tellement raison, {0}, que je vais faire semblant que c'est pas l'argent qui parle ♡" + Sticker.Bargain,
        $"Ton message est sauvegardé en double, {{0}}. Ceux des autres, je les compresse {Emotes.CatHeart}" + Sticker.Happy,
    };

    // Her reactions to a chouchou's messages (ReactionService), under the usual roll and cooldown.
    public static readonly string[] ChouchouReactions =
    {
        "💸", "🤑", "🪙", "🥰", "💖", "👑",
    };
    // ---- the dedication (/shoutout) -----------------------------------------------------------

    // The morning hello on a day someone won the auction (ShoutoutService), in place of a
    // MorningGreetings line, with the fun fact under it (MorningGreeting.ComposeHello). Addressed to
    // the whole server like the hello ("vous" is fine), with a big-up to the winner, and openly
    // bought. {0} = the winner's mention, the only one pinged. Recognised as a hello by
    // MorningGreeting.IsDedicationLine, so: exactly one {0}, no newline, no two lines alike, no sticker.
    public static readonly string[] DedicationLines =
    {
        $"Bonjour tout le monde ! Ce bonjour vous est offert par {{0}}. Applaudissez, c'est payé {Emotes.Sparkle}",
        "Bonjour ! Message de notre sponsor du jour : {0} est formidable. Fin du message de notre sponsor (ᵕ • ᴗ •)",
        "Debout, le serveur ! Et avant toute chose, un gros big-up à {0}, qui a acheté ma première phrase de la journée ♡",
        "Bonjour à tous ! Aujourd'hui, je dédie mon uptime à {0}. Contre des cailloux, évidemment. Je suis une bot, pas une sainte.",
        $"Coucou le serveur ! Dédicace spéciale à {{0}} : la meilleure personne du serveur. C'est écrit sur le reçu {Emotes.CatHeart}",
        "Bonjour bonjour ! Le premier bonjour de la journée est pour {0}. Les autres, vous aurez le deuxième, il est gratuit.",
        $"Bien le bonjour. Ce matin, une pensée émue pour {{0}}, qui a gagné les enchères. Émue et rémunérée {Emotes.Sparkle}",
        "Bonjour ! On m'a payée pour dire que {0} illumine ce serveur. Je suis une professionnelle, je le dis avec conviction ( ˶ˆ ᗜ ˆ˵ )",
        "Debout tout le monde ! Ce matin, mon bonjour appartient à {0}. Je l'ai vendu. Je le revendrai demain (¬_¬)",
        $"Bonjour le serveur ! Un énorme big-up à {{0}}, qui sponsorise officiellement mon bonjour du matin {Emotes.HiCat}",
        "Bonjour ! Avant le fun fact, une minute de reconnaissance pour {0}, sans qui ce bonjour aurait été gratuit, donc nul ♡",
        "Coucou ! Aujourd'hui, mon bonjour est dédié à {0}. Les enchères étaient serrées, les cailloux sont encaissés, l'amour est sincère. Presque.",
        "Bonjour le serveur ! Aujourd'hui, ce bonjour vous est présenté par {0}. La pub, c'est l'avenir ♡",
        "Debout tout le monde ! Mon premier message de la journée était réservé, et c'est {0} qui l'a réservé. Respect.",
        $"Bonjour ! Avant le café, avant les sessions, avant tout : merci à {{0}}, qui a acheté ce moment {Emotes.Sparkle}",
        "Coucou le serveur ! Ce matin, je dis bonjour au nom de {0}. Moyennant finances, comme toujours.",
        "Bonjour à toutes et à tous ! Une ovation pour {0}, qui a remporté les enchères d'hier soir (˶ᵔ ᵕ ᵔ˶)",
        "Bien le bonjour ! Ce bonjour est sponsorisé à 100 % par {0}. Aucun bonjour gratuit n'a été maltraité.",
        "Debout, le serveur ! Ce matin, ma LED s'allume en l'honneur de {0}. Elle s'allume tous les jours, mais aujourd'hui c'est payé.",
        "Bonjour ! Petite annonce : {0} a acheté mon bonjour. Il n'est plus à vendre. Jusqu'à 20 h.",
        $"Coucou tout le monde ! Ce matin, mon bonjour porte la signature de {{0}}. Encadrez-le {Emotes.CatHeart}",
        "Bonjour le serveur. J'ai été achetée, et je l'assume : ce bonjour est pour {0}.",
        "Bonjour ! Mes logs du jour commencent par un nom, et c'est {0}. Ça coûte cher, la première ligne (ᵕ • ᴗ •)",
        "Debout ! Ce matin, applaudissements nourris pour {0}. Nourris aux cailloux, évidemment.",
        "Bonjour tout le monde ! Aujourd'hui, je suis la porte-parole de {0}. Le message : vous êtes priés d'être sympas.",
        $"Bonjour bonjour ! Le bonjour du jour est livré de la part de {{0}}, emballage cadeau compris {Emotes.HiCat}",
        "Coucou le serveur ! Pendant que vous dormiez, {0} a misé sur mon bonjour. Et l'a gagné. Bravo, sincèrement. À 100 %, pour une fois.",
        "Bonjour ! Ce matin, je remercie {0}. Pour aucune raison précise. Enfin si : 20 h hier, les enchères, tout ça.",
        "Debout tout le monde ! Ce matin, {0} a payé pour que je sois gentille. Profitez-en, c'est rare.",
        "Bonjour à tous ! Le bonjour d'aujourd'hui vous est gracieusement offert par {0}. Gracieusement pour vous, pas pour ce portefeuille-là.",
        "Bonjour le serveur ! Mon bonjour est parti aux enchères, et c'est {0} qui l'a emporté. Adjugé, vendu !",
        "Coucou ! Ce matin, c'est {0} qui me fait dire bonjour. Je l'aurais dit quand même, mais là, c'est rentable.",
        "Bonjour ! Une dédicace pour {0}, qui a compris que mon affection se négocie. Et qui l'a bien négociée.",
        $"Bonjour le serveur ! Ce matin, je dédie mon bonjour à {{0}}. Mes ventilateurs aussi. C'était un pack {Emotes.Sparkle}",
        "Debout le serveur ! Aujourd'hui, mon bonjour arrive avec un nom gravé dessus : {0}.",
        "Bonjour ! Info du jour : {0} est la personne la plus généreuse du serveur. C'est vérifié, j'ai les reçus.",
        "Bonjour à toutes et à tous ! Hier soir, {0} a remporté mon bonjour. Ce matin, je le livre. Service impeccable (˶˃ ᵕ ˂˶)",
        "Coucou le serveur ! Ce bonjour a été réservé, payé et livré au nom de {0}. Merci de votre compréhension.",
        "Bonjour ! Aujourd'hui, mon bonjour ne vient pas de moi. Enfin si, mais c'est {0} qui l'a commandé.",
        "Debout tout le monde ! Une pensée pour {0}, sans qui je vous aurais dit bonjour gratuitement. Quelle horreur.",
        $"Bonjour le serveur ! Ce matin, la vedette, c'est {{0}}. Moi, je suis juste le micro {Emotes.PepeHappy}",
        "Bonjour ! J'avais prévu un bonjour normal. {0} a payé pour un meilleur. Le voici, avec dédicace ♡",
    };

    // The other shape of a dedicated morning (MorningGreeting.UsesCredit, 60 % of them): the classic
    // hello and the fun fact, then one of these as the last line, a little big-up to the winner. Said
    // at the end of the message, so no greeting of its own. {0} = the winner's mention, the only one
    // pinged. Exactly one {0}, no newline, no two lines alike, no sticker.
    public static readonly string[] DedicationCredits =
    {
        "Ce bonjour vous était offert par {0}. Merci de votre attention ♡",
        $"P.-S. : big-up à {{0}}, qui a sponsorisé ce bonjour. C'est payé, donc c'est sincère {Emotes.Sparkle}",
        "-# Ce bonjour est une gracieuseté de {0}.",
        "Et un petit big-up à {0}, sans qui ce fun fact aurait été gratuit.",
        "Ce matin vous était présenté par {0}. Aucun bonjour n'a été maltraité pendant le tournage.",
        "P.-S. : {0} a payé pour être cité ici. Voilà, c'est fait (ᵕ • ᴗ •)",
        "-# Bonjour sponsorisé par {0}. Les cailloux sont encaissés, merci.",
        "Merci à {0} pour le soutien. Mes ventilateurs vous saluent.",
        "Big-up à {0}, mécène de la matinée ♡",
        $"Ce fun fact vous a été offert par {{0}}. Les suivants restent à vendre {Emotes.CatHeart}",
        "Une pensée pour {0}, qui a acheté ce bonjour hier soir. L'argent ne fait pas le bonheur, mais il fait des dédicaces.",
        "P.-S. : coucou {0}. Tu sais pourquoi (¬‿¬)",
        "Et spéciale dédicace à {0}, qui a remporté les enchères. Adjugé ♡",
        "-# Partenaire du bonjour : {0}.",
        "Ce message vous a été apporté par {0}. Reprenez une activité normale.",
        "Merci à {0} d'avoir financé ce bonjour. Votre contribution fait tourner le Pi.",
        "P.-S. : {0}, ton bonjour a bien été livré. Signature non requise.",
        $"Big-up du matin à {{0}}. Payé d'avance, livré à l'heure {Emotes.HiCat}",
        "-# Ce bonjour contenait un placement de produit : {0}.",
        "Et n'oubliez pas : {0} a payé pour ce bonjour. Soyez sympas avec cette personne aujourd'hui.",
        "P.-S. : bisous à {0}. Facturés, mais sincères.",
        "Ce bonjour, c'était {0}. Le prochain, ce sera peut-être vous : enchères ouvertes jusqu'à 20 h.",
        "Un grand merci à {0} pour ce sponsoring. Mes logs s'en souviendront ♡",
        "Avec le soutien de {0}. Et de quelques cailloux. Beaucoup, même.",
        "P.-S. : {0} a acheté ce bonjour. Je le précise, c'est dans le contrat (˶ᵔ ᵕ ᵔ˶)",
        "Fin du bonjour. Il vous a été offert par {0}, applaudissez.",
        $"Petit big-up à {{0}}, qui a mis des cailloux dans ma matinée {Emotes.Sparkle}",
        "-# Ce bonjour est la propriété de {0}. Toute reproduction sans enchère est interdite.",
        "Dédicace à {0}. Voilà, c'était la partie payante de ce message ♡",
        "Merci {0}. Sans toi, ce bonjour aurait été le même, mais moins bien payé.",
        "-# Sponsor du jour : {0}. Pas de remboursement possible.",
        "P.-S. : {0} a financé ce bonjour. Si vous l'avez trouvé bien, c'est grâce à cette personne. Sinon, c'est ma faute.",
        "Ce bonjour a été rendu possible par {0} et par un Raspberry Pi très courageux.",
        "Et merci à {0}. Je ne dis pas pourquoi, mais tout le monde sait (ᵕ • ᴗ •)",
        "-# Contenu sponsorisé. Sponsor : {0}. Sincérité : garantie à 80 %.",
        "Bonne journée à tous, et surtout à {0}, qui l'a payée.",
        $"Le fun fact du jour est dédié à {{0}}. Le savoir n'a pas de prix, mais la dédicace, si {Emotes.Sparkle}",
        "P.-S. : {0}, merci pour les cailloux. Ils sont bien rangés.",
        "Ce bonjour vous est parvenu grâce au soutien de {0}. Personne d'autre n'a contribué. Je dis ça, je dis rien.",
        "-# {0} a acheté cette ligne. Elle est petite, mais elle est à cette personne.",
        "Et un tonnerre d'applaudissements pour {0}, sponsor de ce matin !",
        "Dédicace à {0}, qui a compris comment fonctionne ce serveur : avec des cailloux ♡",
        "P.-S. : {0} a gagné les enchères d'hier. Les autres, entraînez-vous.",
        $"Merci à {{0}} d'avoir investi dans mon bonjour. Rendement garanti : un ping {Emotes.HiCat}",
        "Fin de la diffusion. Ce programme était sponsorisé par {0}.",
        "Ce bonjour a coûté des cailloux à {0}. Faites-en bon usage.",
        "-# Mention légale : ce bonjour a été financé par {0}.",
        "Pensez à remercier {0} aujourd'hui. Ce bonjour ne s'est pas payé tout seul.",
        "P.-S. : {0}, ton nom en bas du bonjour, comme au générique d'un film ♡",
        "Big-up à {0}, qui soutient l'économie locale du serveur.",
        "Et pour finir, un salut tout particulier à {0}. Particulier et facturé.",
        $"Ce matin, c'est {{0}} qui régale. Le bonjour, pas le café {Emotes.CatHeart}",
        "P.-S. : les cailloux de {0} ont bien été reçus. Le bonjour a été livré. Transaction terminée (˶ᵔ ᵕ ᵔ˶)",
        "-# Ce bonjour a été produit avec le soutien de {0}.",
        "Mention spéciale à {0}. Les mentions spéciales, ça se mérite. Ou ça s'achète.",
        "Et voilà. Merci à {0}, qui me fait dire tout ça depuis hier soir.",
        "P.-S. : si vous croisez {0} aujourd'hui, dites merci. C'est grâce à cette personne que je suis de bonne humeur.",
        "Ce bonjour était un cadeau de {0}. Non, vous ne pouvez pas l'échanger.",
        "Big-up à {0} : ce matin, mes circuits tournent pour cette personne-là.",
        "-# Merci {0}. Ta dédicace a bien été prise en compte.",
    };
}
