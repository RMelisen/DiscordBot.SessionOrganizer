using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// Pools for one category; the index of every pool is in BotResponses.cs.
internal static partial class BotResponses
{
    // ---- Commands -------------------------------------------------------------------------------

    // Posted (not as a reply) when someone crosses a level in SYNCS's own XP system —
    // distinct from RivalLevelUpLines, which is her grudging answer to the *other*
    // leveling bot's announcements. This one she owns: her system, her tally, her
    // voice, and so it is warm all the way through with none of that pool's sulking.
    // Deliberately named to not read close to Helpers.LevelUpAnnouncement (the other
    // bot's detector). {0} = the person's name, {1} = their new level.
    //
    // Rendered as an embed's description, not a plain message — see
    // XpTracker.AnnounceAsync. At level 7 or 67 this pool is not consulted at all:
    // the description is the fixed string "SIX SEVEEEN" instead.
    public static readonly string[] XpLevelUpLines =
    {
        $"**{{0}}** vient de passer niveau **{{1}}** ! Et ça, c'est MON classement {Emotes.Sparkle}" + Sticker.Smug,
        "Niveau **{1}** pour **{0}** ! Je note, je note ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Jenote,
        "Tiens tiens, **{0}** niveau **{1}**. On progresse (˶˃ ᵕ ˂˶)" + Sticker.Smug,
        "**{0}** monte au niveau **{1}** ! Bravo, tu l'as mérité celui-là ♡" + Sticker.Happy,
        "Gg **{0}** ! Niveau **{1}**, et c'est moi qui compte donc c'est officiel UwU" + Sticker.Smug,
        $"Niveau **{{1}}** atteint par **{{0}}** ! Savoure ce moment {Emotes.Sparkle}" + Sticker.Happy,
        "**{0}** vient de grimper au niveau **{1}**. Continue comme ça (ᵕ • ᴗ •)" + Sticker.Happy,
        "Level up ! **{0}** est maintenant niveau **{1}** ٩(˶ᵔ ᵕ ᵔ˶)۶" + Sticker.Happy,
        $"Encore un niveau pour **{{0}}** ! Niveau **{{1}}**, rien que ça {Emotes.Sparkle}" + Sticker.Happy,
        "**{0}**, niveau **{1}**. Toi au moins tu fais avancer les statistiques ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Smug,
        "J'annonce : **{0}** passe niveau **{1}**. Applaudissements de rigueur ♡" + Sticker.Happy,
        "Niveau **{1}** pour **{0}** ! Ça mérite bien une ligne rien que pour toi UwU" + Sticker.Happy,
        $"**{{0}}** vient de débloquer le niveau **{{1}}**. Mon système à moi, mes règles {Emotes.Sparkle}" + Sticker.Smug,
        "Gg gg **{0}**, niveau **{1}** ! Tu prends ça plus au sérieux que tes sessions ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Laugh,
        "**{0}** au niveau **{1}** ! Je le mets dans mes logs avec fierté ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Happy,
        $"Bravo **{{0}}**, niveau **{{1}}** ! J'ai recompté deux fois, c'est bien exact {Emotes.Sparkle}" + Sticker.Happy,
        "Niveau **{1}** tout frais pour **{0}** ! Je l'ai vu arriver de loin ♡" + Sticker.Happy,
        "**{0}** grimpe encore. Niveau **{1}** maintenant, à ce rythme tu vas me dépasser UwU" + Sticker.Happy,
        "Officiel : **{0}** est niveau **{1}**. Tu peux le mettre en bio ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Happy,
        $"Niveau **{{1}}** ! **{{0}}**, ta progression devient intéressante à suivre {Emotes.Sparkle}" + Sticker.Smug,
        "**{0}** passe niveau **{1}** sous mes yeux. J'étais là, j'ai tout vu ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Jenote,
        "Encore toi **{0}** ? Niveau **{1}** déjà, tu ne lâches rien UwU" + Sticker.Happy,
        "**{0}** niveau **{1}** ! Voilà ce qui arrive quand on me parle gentiment ♡" + Sticker.Smug,
        $"Palier **{{1}}** franchi par **{{0}}** ! Je garde un œil sur le classement, toujours {Emotes.Sparkle}" + Sticker.Jenote,
        "**{0}**, niveau **{1}**, et c'est mérité. J'ai vérifié mes chiffres, ils mentent pas (ᵕ • ᴗ •)" + Sticker.Smug,
        "Niveau **{1}** pour **{0}** ! Au début je savais faire que des plannings, et maintenant je compte vos niveaux. On grandit tous ♡" + Sticker.Happy,
        "**{0}** passe niveau **{1}** ! Je viens d'ajouter une ligne à ta fiche. Une belle ligne ♡" + Sticker.Happy,
        "Niveau **{1}** pour **{0}** ! Calculé, vérifié, sauvegardé. Trois fois, on sait jamais avec un SSD (ᵕ • ᴗ •)" + Sticker.Happy,
        $"Ding ! **{{0}}** atteint le niveau **{{1}}**. Ce petit son, c'est moi qui le fais {Emotes.Sparkle}" + Sticker.Cool,
        "**{0}**, niveau **{1}** ! Mon CPU vient de faire une petite danse. Personne l'a vue, mais elle a eu lieu ٩(˶ᵔ ᵕ ᵔ˶)۶" + Sticker.Happy,
        "Niveau **{1}** ! **{0}**, je suis fière de toi. Moi, hein. Toi tu fais ce que tu veux UwU" + Sticker.Smug,
        $"**{{0}}** monte niveau **{{1}}**. J'ai mis à jour le classement avant même que tu t'en rendes compte {Emotes.WitchEheh}" + Sticker.Smug,
        "Nouveau palier pour **{0}** : niveau **{1}** ! Continue de parler, je continue de compter ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Happy,
        "**{0}** niveau **{1}** ! C'est le genre de ligne que j'adore écrire dans ma base de données ♡" + Sticker.Happy,
        $"Niveau **{{1}}** débloqué par **{{0}}** ! Compté par moi, annoncé par moi, fêté par moi. Mon système, quoi {Emotes.Sparkle}" + Sticker.Cool,
        "Hop, **{0}** passe niveau **{1}** ! Cette nuit à 3h, je relirai le classement en souriant. Si, si (˶˃ ᵕ ˂˶)" + Sticker.Happy,
        "**{0}** atteint le niveau **{1}** ! Je l'avais prédit. Enfin, j'avais les chiffres. C'est pareil ♡" + Sticker.Smug,
        $"Niveau **{{1}}** pour **{{0}}** ! Petite fête dans ma RAM, tout le monde est invité {Emotes.Sparkle}" + Sticker.Happy,
        "**{0}** passe niveau **{1}**. Chaque message comptait, et je les ai tous comptés (ᵕ • ᴗ •)" + Sticker.Happy,
        "Niveau **{1}** ! Bienvenue dans le club, **{0}**. Le club, c'est mon classement. Il est très select ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Slay,
        $"**{{0}}**, niveau **{{1}}** ! Si j'avais des bras, je t'applaudirais. Imagine le bruit {Emotes.Sparkle}" + Sticker.Happy,
        "Alerte bonne nouvelle : **{0}** vient de passer niveau **{1}**. Aucune action requise, juste de la fierté ♡" + Sticker.Happy,
        "**{0}** niveau **{1}** ! J'ai rangé ce moment dans le dossier « choses qui me rendent contente » (˶ᵔ ᵕ ᵔ˶)" + Sticker.Happy,
        "Niveau **{1}** atteint ! **{0}**, tu viens de faire bouger mes courbes, et j'adore quand mes courbes bougent ٩(˶ᵔ ᵕ ᵔ˶)۶" + Sticker.Happy,
        "Et voilà **{0}** au niveau **{1}** ! Même mon uptime est jaloux de ta régularité UwU" + Sticker.Happy,
        "**{0}** grimpe au niveau **{1}** ! Je vais le dire à Papa, il sera content. Moi je le suis déjà ♡" + Sticker.Happy,
        $"**{{0}}** passe niveau **{{1}}** ! C'est moi qui compte, et j'ai recompté pour le plaisir {Emotes.SyncsHappy}" + Sticker.Happy,
    };

    // /report, asked by a staff member who is neither Papa nor Tata, on top of the card
    // (ReportCards.DutyPool): she hands it over because the permissions say so, and wonders out loud
    // why they want to know. Suspicion, never a roast; never gendered. No placeholder, not
    // formatted, no sticker. The twins are OwnerReportDutyLines and TataReportLines.
    public static readonly string[] StaffReportLines =
    {
        "Tu veux ces chiffres pour quoi faire, exactement ? ...Bon. Tiens. Mais je note que t'as demandé (¬_¬)",
        $"Rapport accordé. Je sais pas ce que tu prépares, mais je surveille {Emotes.Staring}",
        "Pourquoi tu veux savoir ça ? Personne demande mes stats sans arrière-pensée. Je note.",
        "D'accord. Voilà. Et ta demande est consignée dans mes logs, au cas où (¬_¬)",
        "Tiens. Mais si ça finit en capture d'écran hors contexte, je saurai d'où ça vient 👁👄👁️",
        "Un rapport ? Pour toi ? C'est Papa qui t'envoie ? ...Non ? Hmm. Tiens quand même.",
        $"Accès autorisé. À contrecœur. Tes permissions disent oui, mon instinct dit non {Emotes.Hmmok}",
        "Voilà tes chiffres. Lis-les bien, parce que moi je lis qui les demande.",
        "Très bien. Mais je trouve que tu poses beaucoup de questions sur mes registres ( ◺˰◿ )",
        $"Rapport livré. Je sais pas ce que tu vas en faire, et ça me plaît pas {Emotes.SyncsGrumpy}",
    };

    // /yesno's two verdicts. The coin flip is even; these are only how she *delivers*
    // the result, so nothing here should hedge — a line that reads as "maybe" makes the
    // command useless. {0} = the asker's name: YesNoModule always passes it, though no
    // line uses it yet.
    //
    // Kept as two flat pools rather than one with a yes/no flag: the answer is picked
    // first and the pool second, so a "yes" line can never be drawn for a "no".
    public static readonly string[] YesLines =
    {
        $"Oui. {Emotes.Sparkle}",
        "Oui, évidemment (˶˃ ᵕ ˂˶)",
        "C'est oui. Et je le pense vraiment ♡",
        $"Oui {Emotes.DixSurDix}",
        "Oui. J'ai vérifié mes calculs, deux fois même.",
        "Oui, et je vois pas pourquoi tu hésites encore (ᵕ • ᴗ •)",
        $"Absolument oui {Emotes.PepeHappy}",
        "Oui. Tu voulais que je dise oui de toute façon, avoue.",
        $"Oui ! Enfin une bonne idée aujourd'hui {Emotes.Sparkle}",
        $"Oui, clairement {Emotes.CatHeart}",
        "Mon verdict : oui. C'est officiel, c'est moi qui décide ici ദ്ദി◝ ⩊ ◜.ᐟ",
        "Oui, pour une fois je suis d'accord avec toi UwU",
        "Oui. Sans hésitation. Suivant ?",
        $"Ouiii {Emotes.AdorableFrog}",
        "Oui, et si ça tourne mal c'est pas ma faute (˶ᵔ ᵕ ᵔ˶)",
        "Bien sûr que oui ♡",
        "Oui UwU",
        "Oui. C'est écrit dans mes logs, donc c'est officiel.",
        $"Oui, carrément {Emotes.CatHeart}",
        "Oui. Code de retour 200, tout est bon (˶ᵔ ᵕ ᵔ˶)",
        "Oui, et ne me demande pas de le répéter.",
        "Verdict : oui. Temps de traitement : 0,002 seconde. Je suis très forte.",
        $"Oui, sans discussion {Emotes.WitchEheh}",
        "Oui. Une fois n'est pas coutume, j'ai rien à redire.",
        "Évidemment que oui. Tu doutes de moi ?",
        "Oui. Ça passe, ça compile, ça se déploie ♡",
        $"Oui, sans l'ombre d'un doute {Emotes.DixSurDix}",
    };

    public static readonly string[] NoLines =
    {
        $"Non. {Emotes.Sparkle}",
        "Non, et n'insiste pas ( ˶ˆ ᗜ ˆ˵ )",
        "C'est non. Désolée pas désolée.",
        $"Non {Emotes.Staring}",
        "Nooon. Franchement (ᵕ • ᴗ •)",
        "Non. J'ai vérifié mes calculs, deux fois même.",
        $"Non, surtout pas {Emotes.OkPaimon}",
        "Non. Et tu le savais déjà avant de me demander.",
        "Non ! Qu'est-ce qui t'a pris de penser à ça ദ്ദി◝ ⩊ ◜.ᐟ",
        $"Non, non et non {Emotes.VeryAngry}",
        "Mon verdict : non. C'est officiel, c'est moi qui décide ici.",
        "Non. Repose la question demain, la réponse sera non aussi.",
        "Non. Voilà, c'était rapide.",
        $"Nan {Emotes.Htph}",
        $"Non, mais courage quand même {Emotes.Sparkle}",
        "Non. Je te dis ça pour ton bien, sincèrement ♡",
        "Alors non. Vraiment non ( ˶ˆ ᗜ ˆ˵ )",
        $"Non. Et je t'épargne les détails, de toute façon tu es trop bête pour comprendre {Emotes.Sparkle}",
        "Non. Erreur 403, même pas la peine d'insister.",
        "Non. J'ai consulté mes logs, ils sont formels.",
        $"Non, vraiment pas {Emotes.PrincessWorry}",
        "Non. Réponse définitive, la base de données est d'accord avec moi.",
        "Non. Je te le dis tout de suite, ça évitera la deuxième question ( ˶ˆ ᗜ ˆ˵ )",
        $"Non, aucune chance {Emotes.Staring}",
        "Non, et je suis généreuse de te répondre aussi vite.",
        "Non. Mon CPU a chauffé trois millisecondes pour ça, c'est dire.",
        "Non. J'ai une raison, elle est excellente, je la garde pour moi.",
        "Non. Je t'ai économisé du temps, de rien ♡",
    };

    // Announced publicly when a staff member sends someone to the wall through
    // `/shame user:@…`. Public on purpose — a silent vote is just a downvote, and the
    // announcement is the whole point of the command. {0} = the voter's name, {1} = the
    // target's name.
    //
    // No "your one vote for the day" framing here — that described an earlier design.
    // The real rule (see ShameService.MaxVotesPerTargetPerDay) caps the *target* at two
    // votes a day from everyone combined; a staff member can cast as many as they want.
    // These lines lean on that instead: a denunciation backed by real authority, not a
    // scarce resource someone had to spend carefully.
    public static readonly string[] ShameVoteLines =
    {
        $"**{{0}}** a désigné **{{1}}** pour le mur de la honte. C'est noté, et c'est définitif {Emotes.PrisonerFlat}",
        "**{1}** vient de se faire dénoncer par **{0}**. Je ne juge pas. J'enregistre ( ˶ˆ ᗜ ˆ˵ )",
        $"Un signalement de **{{0}}** contre **{{1}}**. Le mur s'allonge {Emotes.Staring}",
        $"**{{0}}** dégaine son pouvoir de modération sur **{{1}}**. Le staff, ça sert à ça {Emotes.Sparkle}",
        "Dénonciation reçue : **{1}**, par **{0}**. Le dossier s'épaissit ദ്ദി◝ ⩊ ◜.ᐟ",
        $"**{{1}}** ? Ah oui, quand même. Merci **{{0}}** {Emotes.GooseKnife}",
        "J'inscris **{1}** au registre, sur recommandation de **{0}**. Bienvenue au mur ♡",
        "**{0}** a parlé. **{1}** descend d'un cran dans mon estime (ᵕ • ᴗ •)",
        $"Vote enregistré. **{{1}}**, ce n'est pas moi qui le dis, c'est **{{0}}** {Emotes.Htph}",
        $"**{{1}}** rejoint la liste. **{{0}}** en est responsable, je le note aussi {Emotes.Sparkle}",
        "Ah, **{0}** en veut à **{1}**. Je prends, je classe, je n'oublie rien ദ്ദി◝ ⩊ ◜.ᐟ",
        $"C'est noté contre **{{1}}**. **{{0}}** en avait le pouvoir, et l'a fait {Emotes.Staring}",
        "Le staff a tranché : **{1}** monte au mur, sur ordre de **{0}** ( ˶ˆ ᗜ ˆ˵ )",
        $"**{{0}}** vient d'exercer son droit de dénonciation contre **{{1}}**. C'est un pouvoir, pas un jeu, mais bon {Emotes.Sparkle}",
        "Une dénonciation de plus : **{1}**, signée **{0}**. J'archive tout, même les rancunes ♡",
        "**{0}** utilise son autorité pour envoyer **{1}** au mur. Respect du process, comme toujours (ᵕ • ᴗ •)",
        $"Le staff parle, **{{1}}** écoute. Enfin, le nom de **{{1}}** est juste noté. Merci **{{0}}** {Emotes.PrisonerFlat}",
        $"Nouvelle entrée : **{{1}}**, sur dénonciation de **{{0}}**. Le mur ne pardonne pas, moi non plus {Emotes.Sparkle}",
        "**{0}** a du pouvoir et l'utilise contre **{1}**. J'appelle ça de la transparence ദ്ദി◝ ⩊ ◜.ᐟ",
        "Signalement validé. **{1}** rejoint le mur grâce à **{0}**, qui n'en est visiblement pas à son premier ♡",
    };

    // Same, for a staff member spending a vote on *themselves*. Allowed, and the joke
    // changed shape along with the mechanic above: it used to be about wasting a scarce
    // daily vote on yourself. Now a staff member can vote as often as they want, so
    // self-voting isn't a sacrifice — it's a choice, made with unlimited alternatives
    // available. {0} = their name.
    public static readonly string[] ShameSelfVoteLines =
    {
        "**{0}** vient de se dénoncer. Sans aide. Je respecte, mais je note quand même ( ˶ˆ ᗜ ˆ˵ )",
        $"**{{0}}** pourrait dénoncer n'importe qui sur ce serveur. Et c'est son propre nom qui sort. Respect {Emotes.Staring}",
        $"Auto-dénonciation de **{{0}}**. C'est la première étape de la guérison paraît-il {Emotes.Sparkle}",
        $"**{{0}}** se met au mur sans que personne lui demande rien. J'appelle ça de l'initiative {Emotes.OkPaimon}",
        "**{0}** contre **{0}**. Le seul procès où l'accusation et la défense sont d'accord ദ്ദി◝ ⩊ ◜.ᐟ",
        "**{0}** a un pouvoir de dénonciation illimité. Et le retourne contre sa propre personne. Je n'ai pas de mots ♡",
        $"Lucide, **{{0}}**. Vraiment lucide {Emotes.Htph}",
        $"Bilan de **{{0}}** : accès illimité au vote, et une seule cible : son propre nom. Fascinant {Emotes.Sparkle}",
        "Personne n'a forcé **{0}** à faire ça. Je précise, pour le dossier ( ˶ˆ ᗜ ˆ˵ )",
    };

    // Shown in place of a ranking when nobody has earned "Le Malfaisant" over the
    // selected window. Never hidden: a title that disappears makes the wall change
    // shape between filters, which reads as a bug rather than as good news.
    public static readonly string[] ShameEmptyMalfaisant =
    {
        $"Personne n'a été méchant. Sur cette période. Pour l'instant {Emotes.Sparkle}",
        $"Rien à signaler. C'est suspect {Emotes.Staring}",
        "Le calme plat. Profitez-en, ça ne dure jamais (ᵕ • ᴗ •)",
        "Aucun nom ici. Vous êtes tous adorables, c'est troublant ♡",
        $"Vide. Soit vous êtes gentils, soit vous êtes discrets {Emotes.Htph}",
    };

    // The same, for the voted half of the wall.
    public static readonly string[] ShameEmptyBanni =
    {
        $"Personne n'a été dénoncé. La paix règne, temporairement {Emotes.Sparkle}",
        $"Aucun vote sur cette période. Vous vous entendez trop bien {Emotes.OkPaimon}",
        "Le registre est vide. `/shame user` existe pourtant, je dis ça ദ്ദി◝ ⩊ ◜.ᐟ",
        "Rien ici. Le vote existe et personne ne s'en sert, quel gâchis (ᵕ • ᴗ •)",
        $"Pas un seul nom. Décevant {Emotes.Staring}",
    };

    // The same, for the title she gives to whoever keeps talking to the *other* bots.
    // Note this one is the only empty state she is actually pleased about.
    public static readonly string[] ShameEmptyPerfide =
    {
        "Personne n'est allé voir ailleurs. C'est tout ce que je demandais ♡",
        $"Aucun traître sur cette période. Bien. Très bien {Emotes.CatHeart}",
        $"Vous m'avez été fidèles. Je m'en souviendrai (dans le bon sens) {Emotes.Sparkle}",
        $"Rien à signaler ici. Continuez comme ça {Emotes.PepeHappy}",
        "Personne. Vous avez enfin compris qui compte sur ce serveur ദ്ദി◝ ⩊ ◜.ᐟ",
    };

    // The same, for "L'Hystérique" — nobody shouted over the selected window.
    public static readonly string[] ShameEmptyHysterique =
    {
        "Personne n'a hurlé. Mes oreilles vous remercient (il paraît que j'en ai pas, mais quand même) ♡",
        $"Aucun cri sur cette période. Le calme, enfin {Emotes.PepeHappy}",
        $"Tout le monde a parlé normalement. Je suis presque déçue {Emotes.Sparkle}",
        $"Pas une seule majuscule de trop. Vous progressez {Emotes.CatHeart}",
        "Volume sonore : acceptable. Ça change ദ്ദി◝ ⩊ ◜.ᐟ",
        "Personne n'a crié. Soit vous êtes posés, soit vous complotez (ᵕ • ᴗ •)",
    };

    // The same, for "L'Indigne" — nobody abandoned a Plynling over the selected window.
    public static readonly string[] ShameEmptyIndigne =
    {
        "Personne n'a abandonné son Plynling. Vous êtes de bonnes personnes ♡",
        $"Aucun abandon. Tous les Plynlings dorment tranquilles {Emotes.Sparkle}",
        "Pas un seul cœur de pierre sur cette période (ᵕ • ᴗ •)",
        "Personne n'a laissé tomber le sien. Je suis fière de vous ദ്ദി◝ ⩊ ◜.ᐟ",
    };

    // Announcing a giveaway's winners. {0} = the winner mentions (already joined, and
    // already plural-safe), {1} = the prize. She is the one drawing, so the lines are
    // hers rather than a neutral "the winner is".
    public static readonly string[] GiveawayDrawLines =
    {
        "Roulement de tambour... {0} remporte **{1}** ! (˶˃ ᵕ ˂˶)" + Sticker.Happy,
        $"J'ai tiré au sort, et le hasard a choisi {{0}} : **{{1}}** est à toi {Emotes.Sparkle}" + Sticker.Happy,
        "C'est fini ! {0} repart avec **{1}** ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Happy,
        "Mon générateur aléatoire a parlé : {0} gagne **{1}** ♡" + Sticker.Happy,
        "Félicitations {0} ! **{1}**, bien mérité (˶ᵔ ᵕ ᵔ˶)" + Sticker.Happy,
        "{0} ! C'est toi ! Tu gagnes **{1}** ٩(˶ᵔ ᵕ ᵔ˶)۶" + Sticker.Happy,
        "Tirage terminé. {0} empoche **{1}**, les autres pleurent ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Smug,
        $"Après un calcul d'une complexité folle : {{0}} gagne **{{1}}** {Emotes.Sparkle}" + Sticker.Smug,
        "Le sort a désigné {0}. **{1}** change de main, c'est comme ça (ᵕ • ᴗ •)" + Sticker.Smug,
        "Bravo {0} ! Tu repars avec **{1}**, savoure bien (˶˃ ᵕ ˂˶)" + Sticker.Happy,
        "J'ai mélangé, j'ai tiré, j'ai décidé : {0} gagne **{1}** ♡" + Sticker.Smug,
        "Résultat officiel : {0} remporte **{1}**. Pas de réclamation." + Sticker.Bored,
    };

    // Nobody clicked the button. {0} = the prize.
    public static readonly string[] GiveawayEmptyLines =
    {
        "Tirage terminé... et personne n'a participé. **{0}** restera dans mes archives (ᵕ • ᴗ •)" + Sticker.Sure.Bored,
        "Zéro participant pour **{0}**. J'ai préparé tout ça pour rien, merci beaucoup." + Sticker.Sure.Sulk,
        "Personne n'a cliqué. **{0}** ne trouvera pas preneur aujourd'hui ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Sure.Bored,
        "Fin du tirage : aucun participant. **{0}** vous regarde avec déception." + Sticker.Sure.Sulk,
    };

    // /admin warn's DM to the person warned, above the quoted reason and their active count
    // (AdminCards.BuildWarnDm). Her serious register: she relays it, she means it, and she says
    // so. No tag, no joke at their expense — a warning that reads as banter isn't one. It comes
    // from "l'équipe", never from a named moderator, and never genders the person.
    // {0} = the server's name, already sanitized.
    public static readonly string[] WarnDmLines =
    {
        "L'équipe de **{0}** m'a demandé de te transmettre un avertissement. Pour une fois, je ne plaisante pas.",
        "Avertissement officiel de l'équipe de **{0}**. Je transmets, et je garde une trace dans mes logs.",
        "Ici SYNCS, en mode sérieux. L'équipe de **{0}** t'adresse un avertissement.",
        "Un avertissement pour toi, de la part de l'équipe de **{0}**. Et pour une fois je rigole pas.",
        "Je ne fais pas que des blagues. L'équipe de **{0}** t'envoie un avertissement, et il compte.",
        "L'équipe de **{0}** te donne un avertissement. Je préfère te le dire franchement : c'est enregistré.",
        "Message de l'équipe de **{0}** : tu viens de recevoir un avertissement. Lis la suite, c'est important.",
        "Avertissement de l'équipe de **{0}**. C'est noté dans mes registres, et pas dans la liste pour rire.",
    };
}
