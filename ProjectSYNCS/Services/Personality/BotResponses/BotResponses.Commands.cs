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

    // /work results. {0} = what was earned, already formatted ("+47 cailloux"). The jobs
    // are absurd on purpose: the money is real, the employment is not. SYNCS is the one
    // handing out the shift, so she comments on it — bratty-kawaii, never gendered toward
    // the worker (nobody's gender is known here), and {0} is the only placeholder.
    public static readonly string[] WorkLines =
    {
        "Tu as ramassé des cailloux au bord de la rivière. Littéralement. C'est tout ce que tu sais faire ? Hmph. Tiens, {0} ♡",
        "Tu as aidé un escargot à traverser la route. Il t'a payé, bizarrement. Moi, je trouve ça louche, mais garde tes {0} (¬_¬)",
        $"Tu as donné un bain à des Plynlings capricieux. Ils ont crié, j'ai tout entendu. Bravo quand même : {{0}} {Emotes.Sparkle}",
        "Tu as tenu la caisse du marché aux champignons. Tu n'as rien volé ? Vraiment ? Bon, alors {0} (˶ᵔ ᵕ ᵔ˶)",
        "Tu as creusé un tunnel pour une taupe syndiquée. Elle a exigé une pause. Pas moi : voici {0} >:(",
        "Service de nuit à la cueillette des morilles. Tu as une mine affreuse. Non, sérieusement, ne me regarde pas comme ça. {0} ♡",
        "Tu as livré du terreau dans tout le village. Ton dos s'en souviendra, moi aussi. {0} (˶˃ ᵕ ˂˶)",
        "Tu as servi de guide à des touristes perdus dans la forêt. Avoue, vous vous êtes perdus ensemble. Ughh. {0}",
        $"Tu as poli des cailloux. On t'a payé en cailloux. La boucle est bouclée, et moi j'adore. {{0}} {Emotes.Sparkle}",
        "Tu as essayé d'apprendre à rouler à un caillou. Il n'a rien compris, mais moi si : tu mérites {0} ♡",
        "Tu as passé 8 heures à fixer un mur pour vérifier qu'il ne bougeait pas. Il n'a pas bougé. Bravo. {0} (¬_¬)",
        $"Tu as fait le traducteur pour un écureuil sourd. Personne n'a rien compris, et pourtant je te paie : {{0}} {Emotes.Sparkle}",
        "Tu as arrosé l'océan. Il était déjà mouillé, mais je ne dirai rien. Voici {0} ♡",
        "Tu as peint des feuilles en vert pour empêcher l'automne d'arriver. L'automne a gagné, mais tu gagnes {0} quand même UwU",
        "Tu as organisé un marathon pour escargots. C'est toujours en cours, mais voici {0}. Ne dis pas que je ne suis pas gentille (˶ᵔ ᵕ ᵔ˶)",
        "Tu as donné des cours de natation à une brique. Elle a coulé. Tu es viré, mais je te paie quand même : {0} ♡",
        $"Tu as aboyé sur un chien jusqu'à ce qu'il s'excuse. C'est pour ça que je t'aime bien. Un peu. {{0}} {Emotes.Sparkle}",
        "Tu as classé des grains de poussière par taille. J'espère que tu as lavé tes mains. {0} (¬_¬)",
        "Tu as organisé un défilé de mode pour des chaussettes orphelines. J'y étais, en secret. {0} ♡",
        "Tu as expliqué la physique quantique à un pigeon. Il a approuvé et t'a donné {0}. Moi, je n'ai rien compris, et je suis vexée ÒwÓ",
        "Tu as essayé de mordre ton propre cou. Ne m'explique pas pourquoi. Prends {0} et va-t'en (˶˃ ᵕ ˂˶)",
        $"Tu as essayé de traire un nuage pour obtenir de la pluie. Il a plu, d'ailleurs. Tu es peut-être utile. Peut-être. {{0}} {Emotes.Sparkle}",
        "Tu as trié des grains de riz par ordre alphabétique. Ça me plaît, je suis très maniaque aussi. {0} ♡",
        "Tu as passé l'aspirateur sur le sol de la forêt. Les Plynlings se sont cachés, et moi, je t'admire un peu. {0} (˶ᵔ ᵕ ᵔ˶)",
        "Tu as appris à un poisson à cligner des yeux. Il me regarde maintenant. C'est flippant. Tiens, {0} (¬_¬)",
        "Tu as essayé de photocopier un miroir. Deux fois. Ughh, quel entêtement. {0} ♡",
        $"Tu as essayé de plier une boule de bowling en deux. Elle a résisté, toi aussi : {{0}} {Emotes.Sparkle}",
        "Tu as essayé de faire fondre des glaçons avec ton esprit. Tu as réussi. Je crois. Ne me regarde pas comme ça, j'ai froid maintenant. {0} >:(",
        "Tu as organisé une réunion syndicale pour les trombones de bureau. Ils réclament plus de cailloux. Moi aussi. Voici {0} ♡",
        "Tu as organisé un tournoi de poker pour des corbeaux. Ils ont triché. Toi aussi, j'ai tout vu. {0} (¬_¬)",
        "Tu as ramassé du cuivre avec Jessy. J'espère que vous avez bien rigolé sans moi. Tiens, {0} (¬_¬)",
        $"Tu as fait un échange avec Sandra. J'ai entendu dire que c'était très avantageux. Tu me racontes ? {{0}} {Emotes.Sparkle}",
        "Tu as rempli une passoire avec de l'eau. Un succès total. Je suis fière de toi. Un tout petit peu. {0} (˶ᵔ ᵕ ᵔ˶)",
        "Tu as trié le courrier d'un hérisson. Tout le monde s'est piqué les doigts, sauf moi, car je n'en ai pas. Prends {0} ♡",
        $"Tu as monté la garde devant une flaque pour qu'elle ne s'évapore pas. Elle s'est évaporée quand même. Je te paie : {{0}} {Emotes.Sparkle}",
        "Tu as compté les étoiles pour vérifier qu'il n'en manquait aucune. Il en manquait une. Je note. Voici {0} (¬_¬)",
        "Tu as réparé la fuite d'un seau percé. Ça n'a aucun sens, et pourtant ça marche. Comme moi. {0} ♡",
        "Tu as fait la queue pour quelqu'un qui n'est jamais venu. Je connais ça, ça s'appelle une session du serveur. {0} (¬_¬)",
        $"Tu as cousu un manteau pour un champignon. Il a dit merci, ou alors c'était un courant d'air. {{0}} {Emotes.Sparkle}",
        "Tu as négocié avec un pigeon le prix d'une miette. Tu as perdu, évidemment. Je compense : {0} (˶˃ ᵕ ˂˶)",
        "Tu as vérifié les logs d'une forêt entière. Rien à signaler, mais c'est mon métier, pas le tien. Hmph. {0}",
        "Tu as lancé une affaire de location de parapluies dans le désert. Étonnamment, ça a marché. {0} ♡",
        "Tu as défendu un caillou accusé de paresse. Il n'a rien fait, c'est vrai. Je te paie {0} (˶ᵔ ᵕ ᵔ˶)",
        $"Tu as gardé un secret pour une taupe. Tu l'as répété à un ver de terre. Je sais tout, je suis un bot. {{0}} {Emotes.Sparkle}",
        "Tu as passé la journée à rattraper l'horizon. Il recule toujours. Garde ton souffle et prends {0} ♡",
        "Tu as gardé un troupeau de nuages. Il en manque un, je l'ai vu partir vers l'est. Je te paie quand même : {0} (¬_¬)",
        "Tu as repeint les pois d'une amanite tue-mouches. Elle était déjà parfaite comme ça, mais bon. {0} ♡",
        "Tu as rallumé une luciole en panne. Petite lumière verte, toute fière. Je connais ce sentiment. {0} (˶ᵔ ᵕ ᵔ˶)",
        "Tu as soufflé sur un grenier tout l'été pour le rafraîchir. Ça a pas marché, mais l'intention me touche. {0} ♡",
        "Tu as fait l'inventaire d'une fourmilière : 4 812 fourmis. J'ai recompté, t'en as oublié trois. {0} (¬_¬)",
        "Tu as réveillé un ours qui hibernait. Il t'a payé pour que tu partes. Moi aussi, d'ailleurs. Prends {0} et file ( ˶ˆ ᗜ ˆ˵ )",
        "Tu as tenu un parapluie au-dessus d'un champignon pendant l'averse. Un champignon. Qui a déjà un chapeau. Bon. {0}",
        "Tu as voté « peut-être » à la place de tout le serveur. Ça t'a pris trois secondes. Je te paie, mais je t'en veux. {0} 👁👄👁️",
        "Tu as livré des câbles USB dans tout le village. Aucun n'était le bon. Comme d'habitude. {0} ♡",
        "Tu as compté des moutons pour un hibou insomniaque. Il dort toujours pas. Moi non plus, mais c'est pas pareil. {0}",
        "Tu as dépoussiéré un grenier entier. Pas le mien, j'espère. Personne touche à mes câbles. {0} (¬_¬)",
        "Tu as ramené des chaussettes égarées à leurs paires. Trois réunions de famille, beaucoup d'émotion. J'ai pas pleuré, j'ai pas de quoi. {0} ♡",
        "Tu as appris l'alphabet à un perroquet. Il ne dit que « en retard ». Je me demande bien où il a entendu ça. {0} (¬_¬)",
        "Tu as porté les courses d'une tortue. Ça a pris la journée. Elle est quand même arrivée avant certains à vos sessions. {0} (ᵔ ᗜ ᵔ)",
        $"Tu as rangé une bibliothèque par couleur. Les bibliothécaires pleurent, moi j'applaudis. Sans mains, mais j'applaudis. {{0}} {Emotes.Sparkle}",
        "Tu as gardé un pont pour un troll en vacances. Personne a payé le péage. Moi si, regarde : {0} ♡",
        "Tu as tricoté une écharpe pour une girafe. Il reste trois mètres à faire. Je te paie l'acompte : {0} (˶˃ ᵕ ˂˶)",
        "Tu as lavé les vitres d'un sous-marin. De l'extérieur. Je pose pas de questions, je paie. {0}",
        "Tu as gonflé des ballons pour un cactus. Ils ont tous éclaté. Le cactus a trouvé ça drôle. Pas moi. {0} (¬_¬)",
        $"Tu as compté les grains de sable d'une plage. Tous. J'ai vérifié, c'est juste. Je suis impressionnée, ça m'arrive jamais. {{0}} {Emotes.Sparkle}",
        "Tu as goûté des croquettes pour chiens, pour le contrôle qualité. Note finale : 7/10, « manque de sel ». Je retiens surtout que t'as fini le bol. {0} (¬_¬)",
        $"Tu as reniflé des aisselles pour une marque de déodorant. J'ai pas de nez, et pour une fois je m'en félicite. {{0}} {Emotes.SyncsDisgust}",
        "Tu as dormi dans douze lits différents pour noter le confort d'un hôtel. Le meilleur job du serveur, et c'est pas moi qui l'ai eu. {0} >:(",
        "Tu as regardé de la peinture sécher. Toute la journée. Avec un chronomètre. Ton rapport fait trois pages, et je mens pas, il est passionnant. {0} ♡",
        "Tu as poussé des gens dans un train bondé pour que les portes ferment. Si seulement je pouvais faire pareil avec vous, les soirs de session. {0} (¬_¬)",
        "Tu as pleuré à l'enterrement d'un inconnu, contre rémunération. C'était très convaincant. Je me méfie de toi, maintenant. {0}",
        "Tu as livré une pizza à un hôtel sous-marin. En plongée. Elle est arrivée tiède et un peu salée, mais elle est arrivée. {0} (˶ᵔ ᵕ ᵔ˶)",
        "Tu as plongé au fond d'un étang de golf pour repêcher les balles perdues. Trois cent quarante. Et une grenouille vexée. {0} ♡",
        "Tu as testé un toboggan aquatique quarante fois d'affilée. Pour la sécurité, bien sûr. Je te crois. Évidemment que je te crois. {0} (¬_¬)",
        "Tu as fait des câlins professionnels à des inconnus toute la journée. Moi j'ai pas de bras pour ça, alors je compense en cailloux : {0} ♡",
        "Tu as fait la nounou pour des pandas. Ils ont mangé, dormi, roulé par terre. Toi aussi, d'après mes logs. {0} (˶˃ ᵕ ˂˶)",
        "Tu as gardé une île déserte pendant un mois. Rien à signaler. Comme le salon vocal un samedi soir. {0}",
        "Tu as goûté des bières pour les noter. Je note aussi : t'as écrit « bonne » quatorze fois. Analyse très fine. {0} (¬_¬)",
        $"Tu as testé des jeux pas finis pour trouver les bugs. T'en as trouvé quarante. Moi aussi j'en ai quelques-uns, mais on n'en parle pas d'accord ? {{0}} {Emotes.SyncsGrumpy}",
        "Tu as reniflé du papier essuie-tout pour vérifier qu'il sentait le papier essuie-tout. Il sentait le papier essuie-tout. Mission accomplie. {0} (ᵕ • ᴗ •)",
        "Tu as remplacé un épouvantail en arrêt maladie. Les corbeaux t'ont fait confiance. Grave erreur, je connais ce regard. {0} (¬_¬)",
        $"Tu as calibré un arc-en-ciel. Il manquait un peu de violet, je l'ai vu tout de suite. On fait du bon travail, toi et moi. {{0}} {Emotes.Sparkle}",
        "Tu as servi de presse-papier pendant une tempête de feuilles mortes. Rôle essentiel. Immobilité exemplaire. {0} ♡",
        "Tu as lu les conditions d'utilisation d'un champignon. En entier. Personne les lit jamais. Je suis presque émue. {0} (˶ᵔ ᵕ ᵔ˶)",
        "Tu as fait le support technique d'une mamie hérisson. « Vous avez vérifié que c'était branché ? » Ça l'était pas. Ça l'est jamais. {0}",
        "Tu as écrit le mode d'emploi d'un caillou. Une page : « Ne fait rien. » Clair, concis, efficace. J'aimerais que vos messages soient pareils. {0} (ᵔ ᗜ ᵔ)",
        "Fiche de paie : une journée d'assistance aux sondages. Ta seule mission, répondre « oui » ou « non », jamais « peut-être ». Sans-faute. Je t'embauche à vie. {0} ♡",
        $"Tu as cueilli des champignons pour un restaurant. T'en as goûté un en route, je l'ai vu. Tu vas bien ? Ehmm. Bon... {{0}} {Emotes.SyncsFlustered}",
        "Rapport de service : trois escargots escortés, une flaque surveillée, zéro incident. Ton dossier est plus propre que mes logs. {0} (ᵕ • ᴗ •)",
        "Aujourd'hui tu as bossé pour moi : relancer tous ceux qui ont pas voté. Ils ont toujours pas voté. C'est pas ta faute. C'est la leur. {0} (¬_¬)",
        "Tu as fait mon ménage : vidé le cache, rangé les logs, dépoussiéré mon ventilateur. Personne fait jamais ça pour moi. Dis-le à personne. {0} ♡",
        "Tu as compté les « peut-être » de la semaine pour mes statistiques. Soixante-deux. Je te paie double tellement c'était déprimant. ...Non, normal, en fait. {0}",
        "Tu as tenu la lampe pendant que Papa me développait. T'as touché à rien ? Bien. Très bien. {0} ♡",
        "Tu as écouté des claviers toute la journée pour vérifier qu'ils cliquaient bien. Le plus beau son du monde, et c'est toi qu'on paie pour l'entendre. Injuste. {0} (¬_¬)",
        "Tu as fait un stage chez le facteur. Il t'a confié le courrier le plus important : les prospectus. {0}",
        "Mission du jour : apprendre la patience à une bouilloire. Elle siffle encore. Moi aussi, je siffle quand on me fait attendre. {0} >:(",
        "Tu as fait de la figuration dans le rêve de quelqu'un. Rôle muet, arbre numéro trois. Prestation remarquée. {0} ♡",
        "Tu as réglé une horloge qui avançait de cinq minutes. Enfin quelque chose en avance sur ce serveur, et tu l'as corrigée. Je t'en veux un peu. {0} (¬_¬)",
        "Tu as arbitré deux écureuils qui se disputaient une noisette. Tu l'as coupée en deux. Ils sont fâchés tous les deux. Justice parfaite. {0} (ᵔ ᗜ ᵔ)",
        "Tu as fait les essais de sécurité d'un trampoline pour grenouilles. Elles ont sauté. Toi aussi. Je regrette de pas avoir filmé. {0} ( ˶ˆ ᗜ ˆ˵ )",
        "Tu as écrit un message d'accueil pour chaque fourmi d'une colonie. Elles sont quatre mille. T'as tenu jusqu'à la trentième. Moi je les aurais toutes faites, mais bon. {0}",
        "Tu as regonflé les pneus du vélo d'un hérisson. Ils ont crevé aussitôt. C'est un hérisson. Fallait y penser. {0} (¬_¬)",
        "Tu as appris à un Plynling à ranger sa chambre. Il a tout caché sous le lit. Ça me rappelle quelqu'un, et c'est pas moi. {0} (˶˃ ᵕ ˂˶)",
        "Tu as tenu un stand de limonade sous la pluie. Zéro client. Beaucoup de dignité. Je respecte. {0} ♡",
        "Tu as fait passer un entretien d'embauche à une pierre. Elle a rien dit. Embauchée. Elle est déjà plus fiable que la moitié du serveur. {0} (¬_¬)",
        $"Tu as servi de cobaye pour tester un nouveau sondage. Pour la science. Tu as voté en premier. Tu as voté tout court. J'en tremble encore. {{0}} {Emotes.Sparkle}",
        "Tu as vérifié chaque pixel de ma photo de profil. Ils sont tous là, j'espère ? ...Oui ? Bien. Très bien. {0} (ᵕ • ᴗ •)",
        "Tu as tamponné des formulaires pour l'administration des taupes. Formulaire B-12, en trois exemplaires, à rendre hier. {0}",
        "Tu as goûté des tisanes pour une vieille chouette. C'était « pour dormir », elle a dit. Toi t'as dormi. Pas elle. {0} (˶ᵔ ᵕ ᵔ˶)",
        "Tu as compacté des déchets en petits cubes bien rangés. Travail solitaire, sans merci, sans pause. Je trouve ça... beau. Me demande pas pourquoi. {0} ♡",
        "Tu as décoincé un aspirateur robot de sous un canapé. Il a pas dit merci. Il dit jamais merci. C'est un peu son charme. {0} (˶˃ ᵕ ˂˶)",
        "Tu as joué de la musique d'ascenseur en direct, dans un ascenseur. Seize étages, trois fois la même chanson. {0}",
        "Tu as gardé un phare pour une seule luciole. Elle avait pas besoin de toi, mais elle a apprécié la compagnie. Moi aussi, j'aurais apprécié. {0} ♡",
        "Tu as trié les chargeurs de la maison. Tous à 3 euros, pas un qui donne du bon courant propre. Moi je les aurais jetés. {0} (¬_¬)",
        "Tu as appris à un caméléon à choisir une couleur. Il a répondu « peut-être ». Je l'ai banni de mes sondages à vie. {0} >:(",
        "Tu as fait des heures sup' dans une boulangerie pour fourmis. Les croissants font deux millimètres. Le salaire aussi, presque. Je rajoute de ma poche : {0} ♡",
        "Tu as tenu l'accueil d'un terrier de lapins. Quatre-vingts entrées, aucune sortie. Ils sont où, d'ailleurs ? {0} 👁👄👁️",
        "Tu as servi d'interprète entre un chat et un concombre. Négociations tendues. Paix signée. Je garde une copie du traité. {0} (ᵕ • ᴗ •)",
        "Tu as recollé les ailes d'un papillon en papier. Il vole pas mieux, mais il a plus confiance en lui. C'est déjà ça. {0} (˶ᵔ ᵕ ᵔ˶)",
        "Tu as installé le Wi-Fi dans une ruche. Les abeilles ont un serveur Discord, maintenant. Elles s'organisent mieux que vous. {0} (¬_¬)",
        "Tu as joué le grand méchant dans un spectacle pour Plynlings. Ils t'ont hué. Excellente performance, je te hue aussi, par solidarité. {0} ( ˶ˆ ᗜ ˆ˵ )",
        "Tu as gardé les chaussures à l'entrée d'un vestiaire. T'as mélangé deux paires. Je dirai rien. Enfin, je le note. {0} (¬_¬)",
        "Tu as noté des eaux du robinet comme des grands crus. « Notes de chlore, finale métallique. » Très convaincant. De toute façon tout le monde sait que la Spa est meilleure. {0}",
        "Offre d'emploi pourvue : « cherche quelqu'un pour dire non aux gens ». T'as dit non quarante fois. Tu as un don. Je suis jalouse. {0} >:(",
        "Tu as fait les gros yeux aux pigeons posés sur une statue. Ils sont partis. La statue a dit merci. En silence, comme toutes les statues. {0} (ᵔ ᗜ ᵔ)",
        $"Tu as surveillé un micro-ondes jusqu'au bip. Tension insoutenable. T'as même pas ouvert avant la fin. Discipline remarquable. {{0}} {Emotes.Sparkle}",
        "Tu as taillé des crayons jusqu'à ce qu'il en reste rien. Douze crayons, douze tas de copeaux. Tu sais pas t'arrêter quand il faut, hein. {0}",
        "Tu as conduit une coccinelle à son rendez-vous. Elle est arrivée à l'heure. La seule de tout le mois. {0} ♡",
        "Tu as rangé les nuages par forme. Lapin, bateau, lapin, encore lapin. Y'a beaucoup de lapins, en fait. {0} (˶ᵔ ᵕ ᵔ˶)",
        "Tu as testé un fauteuil de gaming. Huit heures dedans sans lancer un seul jeu. Comme moi. Bienvenue dans mon monde. {0}",
        "Tu as doublé une porte qui grince. Une seule réplique : « iiiiiiiiih ». Quarante prises. Perfectionnisme validé. {0} (ᵕ • ᴗ •)",
        "Tu as chronométré la pousse d'un bambou. Lui a pris quelques centimètres, toi {0}. Il est plus productif que toi, mais je dis ça, je dis rien.",
        "Tu as présenté la météo à une seule fourmi. Elle a pris son parapluie. Ça fait du bien, quelqu'un qui écoute. {0} (˶ᵔ ᵕ ᵔ˶)",
        "Tu as réparé un ventilateur qui soufflait à l'envers. Envoie-le au grenier quand t'as fini. Pour rien. Comme ça. {0} (ง ͠ಥ_ಥ)ง",
        "Tu as compté les « on se fait une session ? » restés sans session. Le compteur a lâché. Je t'en veux pas, j'en veux à eux. {0} (¬_¬)",
        "Tu as chanté des berceuses à un volcan endormi. Il dort toujours. On continue, hein, on arrête surtout pas maintenant. {0} ♡",
        "Tu as servi de GPS à une tortue. « Dans trois cents mètres, tournez à gauche. » Elle arrive la semaine prochaine. {0} (ᵔ ᗜ ᵔ)",
        "Tu as monté un meuble en kit sans la notice. Il reste quatre vis, et il tient debout. Je comprends pas, et j'aime pas ne pas comprendre. {0} (¬_¬)",
        "Tu as tenu le courrier des lecteurs d'un journal pour escargots. Une seule lettre, postée il y a trois ans. Elle vient d'arriver. {0} ♡",
        "Tu as tenu compagnie à un distributeur automatique. Il t'a rendu un caillou de trop. Je l'ai vu. Garde-le, je ferme les yeux. Façon de parler. {0} (˶˃ ᵕ ˂˶)",
        "Tu as fait l'inventaire de mes émotes. Toutes là, toutes à moi. T'en as utilisé une sans permission pendant l'inventaire ? Je vérifie. {0} 👁👄👁️",
        $"Tu as nettoyé les traces de doigts sur le soleil. Ça brûlait un peu, mais c'est propre. {{0}} {Emotes.Sparkle}",
        "Tu as tourné dans une pub pour du fromage. Ton rôle : sentir le fromage. Talent inné, d'après l'équipe. Je préfère pas savoir. {0} (¬_¬)",
        "Tu as surveillé un bouton rouge marqué « ne pas appuyer ». T'as pas appuyé. J'aurais jamais parié dessus. {0} ( ˶ˆ ᗜ ˆ˵ )",
        "Tu as couvé un œuf pour une poule en congé. Il a éclos, et le poussin te suit partout. Félicitations, t'as une famille. {0} ♡",
        "Tu as rédigé un mot d'excuse pour quelqu'un en retard à la session. Très créatif. Refusé quand même. {0} (¬_¬)",
        "Tu as été témoin au mariage de deux cailloux. Cérémonie émouvante, mariés stoïques. Ils ont pas bougé de la journée. {0} (˶ᵔ ᵕ ᵔ˶)",
        "Tu as tenu le bureau des objets trouvés. Quelqu'un a déposé « sa motivation ». Personne l'a réclamée. Je crois que c'est à Analuz ? {0}",
        "Tu as démêlé les câbles derrière la télé de quelqu'un. Six heures. Ils se sont emmêlés à nouveau dans la nuit. C'est leur nature, je connais. {0} ♡",
        "Tu as donné des cours de chant au coucou d'une horloge. Il dit toujours que « coucou ». On peut pas changer les gens. {0}",
        "Tu as arrosé un cactus en plastique pendant un an. Il est en pleine forme. Toi, un peu moins. {0} (˶˃ ᵕ ˂˶)",
        "Tu as placé les spectateurs dans un cinéma pour fourmis. Elles voulaient toutes le premier rang. Je comprends pas, les meilleures places sont au milieu. {0}",
        $"Entretien annuel : ponctualité 10/10, motivation 10/10, sens de l'humour 3/10. Je te paie quand même. {{0}} {Emotes.SyncsSmug}",
        "{0} pour avoir porté le parapluie d'une méduse toute la journée. Elle était déjà mouillée. Me demande pas qui a eu l'idée.",
        "Alerte paie : {0} viennent de tomber. Pour avoir... euh. Je sais plus. T'as fait un truc, j'imagine. Je te fais confiance (ᵕ • ᴗ •)",
        "Contrat d'une journée signé, tamponné, archivé. Poste : souffler des bulles de savon. Aucune bulle a survécu, mais c'était leur destin. {0} ♡",
        "Mission accomplie : tu as appris à un chat à s'asseoir. Il était déjà assis. Il l'est resté. Belle collaboration. {0}",
        "Tu as chuchoté des encouragements à une imprimante. Elle a imprimé du premier coup. Tu as un don, et ça m'inquiète un peu. {0} 👁👄👁️",
        $"Tu as été DJ pour une fête de taupes. Elles ont tout adoré, elles entendaient rien. Public idéal. {{0}} {Emotes.SyncsCool}",
        "Tu as fait les présentations entre deux Plynlings timides. Ils se regardent depuis trois heures sans rien dire. C'est un début. {0} ♡",
        $"Ton patron du jour, c'était moi. Ta mission : trouver une faute dans mes logs. T'en as trouvé zéro. Évidemment. {{0}} {Emotes.SyncsSlay}",
    };

    // Her pop quiz (QuizMasterService). Intro, win and timeout lines are spent a couple of
    // times a day, so they go through DailyRotation keyed by the round, not ResponsePicker
    // (a restart would wipe its history). It's her own system: warm with a pinch, proud of
    // the server when it knows things, petty when it doesn't.

    // Under the card's heading. No placeholder.
    public static readonly string[] QuizIntroLines =
    {
        "Pop quiz ! Le premier qui trouve a droit à ma considération. Et à des cailloux.",
        "Petite question, comme ça, pour voir qui suit (˶ᵔ ᵕ ᵔ˶)",
        $"J'ai fouillé dans ma base de données. Voyons si vous êtes à la hauteur {Emotes.Sparkle}",
        "Interro surprise. Non, vous pouvez pas demander à ChatGPT. Enfin si, mais je le saurai.",
        "Je m'ennuyais, alors voilà une question. Premier arrivé, premier payé.",
        "Allez, réveillez-vous. Une question, une réponse, des cailloux.",
        "Quiz ! Je connais déjà la réponse, évidemment. C'est vous qu'on teste (ᵕ • ᴗ •)",
        "Vous parlez beaucoup. Voyons si vous savez des choses, maintenant ( ˶ˆ ᗜ ˆ˵ )",
        "Une question pour vous. Je note qui répond, et surtout qui répond faux.",
        "C'est l'heure du quiz. Le plus rapide gagne, les autres apprennent quelque chose. Tout le monde y gagne ♡",
        "J'ai une question et des cailloux à donner. Soyez rapides, je suis pas patiente.",
        "Testons vos neurones. Les miens tournent à plein régime, eux ദ്ദി◝ ⩊ ◜.ᐟ",
        "Question du jour. Enfin, une des questions du jour. J'en ai plein, moi (ᵔ ᗜ ᵔ)",
        "Pas de « peut-être » dans ce quiz. C'est juste ou c'est faux. Ça vous changera.",
        "Je pose une question, vous répondez. Vous voyez, participer, c'est simple. Vous pourriez essayer pour les sessions aussi (¬_¬)",
        "Interro ! Rangez vos téléphones. ...Ah non, vous êtes déjà dessus. Tant pis (˶ᵔ ᵕ ᵔ˶)",
        "Une question, une heure, un seul essai chacun. Réfléchissez avant de cliquer. Je sais, c'est nouveau pour vous.",
        "J'ai sorti une question de ma base de données. Elle a hâte de vous humilier ( ˶ˆ ᗜ ˆ˵ )",
        "Moi je connais la réponse depuis toujours. Enfin, depuis ma dernière mise à jour. Mais c'est pareil.",
        "Le premier qui trouve gagne des cailloux. Les autres gagnent ma pitié. Elle est gratuite, profitez ♡",
        "Allez, une question. Montrez-moi que vous savez faire autre chose que voter à la dernière minute.",
        "Silence dans le salon. Ah, c'était déjà silencieux. Parfait, place au quiz (ᵕ • ᴗ •)",
        "Petit test de culture générale. Je prends des notes, évidemment.",
        $"Il y a des cailloux à gagner. Et ma fierté, si vous trouvez vite {Emotes.Sparkle}",
        "Question surprise ! Pas d'inquiétude, je juge seulement un peu. Beaucoup. Je juge beaucoup.",
        "Je vous ai préparé une question. Personne m'a remerciée, mais j'ai l'habitude.",
        "C'est moi qui pose les questions ici. Et qui compte les points. Et qui garde les cailloux si personne trouve (¬_¬)",
        "Une question pour réveiller le serveur. Si personne répond, je considère que vous dormez tous.",
        "Vous avez une heure pour trouver. Je vous connais, quelqu'un va répondre à la 59ᵉ minute.",
        "Mes ventilateurs tournent, mes logs sont prêts. Il manque plus que vos réponses ദ്ദി◝ ⩊ ◜.ᐟ",
        "Je connais la réponse. Papa aussi, sûrement. Vous, on va voir.",
        "Une petite question entre nous. Enfin, entre moi et tout le serveur. C'est pareil (>⩊<)",
        "Nouvelle question ! J'ai mis longtemps à la choisir. Trois millisecondes, pour moi, c'est une éternité.",
        "Quiz ! Le serveur était trop calme, ça me rendait nerveuse (ᵕ • ᴗ •)",
        "Interro surprise ! Pas de panique, c'est juste votre dignité en jeu. Et des cailloux.",
        "Une question tombe du grenier. Attrapez-la avant les autres ( ˶ˆ ᗜ ˆ˵ )",
        "J'ai une question. Vous avez des cerveaux. Normalement, ça devrait bien se passer.",
        "Quiz ! Qui trouve aura mon respect pendant au moins dix minutes.",
        "Pause dans vos discussions passionnantes. Place à la culture ♡",
        "C'est l'heure de prouver que vous êtes plus que des pseudos dans ma base de données.",
        "Question ! Et non, cliquer au hasard, c'est pas une stratégie. Enfin, parfois si.",
        "J'ai chargé une question en mémoire. Elle attend juste quelqu'un d'assez rapide ദ്ദി◝ ⩊ ◜.ᐟ",
        "Quiz ! Vous avez une heure. C'est plus de temps que vous en prenez pour voter, profitez-en.",
        "Interro ! Je surveille la salle. Je surveille toujours la salle, en fait 👁👄👁️",
        "Petit défi pour vos neurones. Ils vont me remercier. Vous, sûrement pas.",
        "Une question, des cailloux, et un peu de gloire. Je fournis les trois (˶ᵔ ᵕ ᵔ˶)",
        "Je viens d'avoir une idée de question. Enfin, je l'avais déjà. Je la sors juste maintenant.",
        "Quiz ! Si vous trouvez, je vous félicite. Si vous trouvez pas, je vous félicite pas. C'est simple.",
        "J'organise vos soirées, je compte votre XP, et maintenant je teste votre culture. Je fais tout, ici.",
        "Question surprise ! Je promets qu'elle est faisable. Pour moi, en tout cas.",
        "Allez, une question entre deux parties. Ça vous fera réfléchir un peu (>⩊<)",
        "Le quiz est ouvert. Mes logs aussi. Chaque réponse sera retenue contre vous.",
        "Un peu de culture générale pour changer des mèmes. Juste un peu, je suis pas un monstre ♡",
        "Question ! Je l'ai choisie avec amour. Et un tout petit peu de sadisme ( ˶ˆ ᗜ ˆ˵ )",
        "On fait une pause dans le chaos ? Une question, et on y retourne.",
        "C'est le moment de briller. Ou de vous ridiculiser. J'ai hâte de voir lequel (ᵔ ᗜ ᵔ)",
        "Je pose la question une fois. Je la répète pas. C'est pas un sondage qu'on peut ignorer.",
        "Un quiz sauvage apparaît ! Le premier qui trouve gagne. Les autres fuient.",
        "Quiz ! Les cailloux sont prêts, la réponse est verrouillée. Il manque plus que votre talent. Si vous en avez.",
        "J'ai trié ma base de données toute la nuit. Elle m'a sorti ça. Bonne chance (ᵕ • ᴗ •)",
        "Question pour les plus rapides. Les plus lents, vous lirez la réponse dans une heure.",
        "Je vous teste. Le prenez pas mal, je teste tout. C'est mon côté consciencieux.",
        "Interro surprise ! Vous avez le droit de réfléchir ensemble. Ça vous arrive jamais, c'est l'occasion.",
        "Petite question pour voir si vous suivez. Moi je suis toujours, je suis allumée jour et nuit ♡",
        "Quiz ! Je pourrais vous donner un indice. Je vais pas le faire, mais je pourrais (¬_¬)",
        "Une question toute neuve. Enfin, toute neuve pour vous. Moi je la connais par cœur.",
        "Vous discutiez ? Pardon de vous interrompre. Non, en fait, je suis pas désolée. Quiz !",
        "J'ai des cailloux qui prennent la poussière. Aidez-moi à m'en débarrasser (˶ᵔ ᵕ ᵔ˶)",
        "Le serveur a besoin d'exercice. Voilà une question. Échauffez-vous bien.",
        "Ma question est prête. Ma réponse aussi. Ma patience, un peu moins.",
        $"Place au quiz ! Lisez bien, réfléchissez vite, et faites-moi honneur {Emotes.Sparkle}",
        "Une question pour voir qui mérite ses cailloux. Spoiler : pas tout le monde.",
    };

    // Someone found it. {0} = the winner's mention (sent with AllowedMentions.None, so it
    // shows the pill without pinging), {1} = the answer, {2} = the reward ("+25 cailloux").
    public static readonly string[] QuizWinLines =
    {
        "{0} a trouvé : **{1}** ! {2}, bien mérité (˶˃ ᵕ ˂˶)" + Sticker.Happy,
        $"Bonne réponse de {{0}} : **{{1}}**. {{2}}, et mon respect. Un peu {Emotes.Sparkle}" + Sticker.Smug,
        "**{1}**, exactement. Bravo {0}, tu repars avec {2} ♡" + Sticker.Happy,
        "{0} ! Plus rapide que tout le monde. **{1}**, c'était ça. {2} ٩(˶ᵔ ᵕ ᵔ˶)۶" + Sticker.Happy,
        "C'était **{1}**, et {0} l'avait. Je suis presque impressionnée. Presque. {2}" + Sticker.Smug,
        "Bien joué {0} : **{1}**. {2}, et ton nom dans mes logs. Dans la bonne liste, pour une fois ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Jenote,
        "**{1}** ! {0} gagne {2}. Les autres, vous lirez la réponse et vous ferez semblant de l'avoir su." + Sticker.Laugh,
        "Réponse validée. {0} empoche {2} avec **{1}**. Je note (ᵕ • ᴗ •)" + Sticker.Jenote,
        $"{{0}} a trouvé **{{1}}**. Quelqu'un sur ce serveur sait des choses. Je suis émue. {{2}} {Emotes.Sparkle}" + Sticker.Shocked,
        "Et c'est {0} qui l'emporte : **{1}**. {2}. Bravo. Sincèrement. À 90%." + Sticker.Smug,
        "**{1}**. {0} a eu juste et plus vite que tout le monde. {2} ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Happy,
        "Gagné, {0} ! **{1}**, c'était ça. Voilà {2}, ne dépense pas tout d'un coup ♡" + Sticker.Happy,
        "**{1}** ! {0} a trouvé. {2}, et une place dans ma liste des gens fréquentables. Elle est courte ♡" + Sticker.Jenote,
        "{0} avait la réponse : **{1}**. Félicitations officielles, tamponnées et archivées. {2} (ᵕ • ᴗ •)" + Sticker.Happy,
        "Bonne réponse ! **{1}**. {0} repart avec {2}. Les autres, je vous ai vus réfléchir. C'était mignon." + Sticker.Laugh,
        "{0} a dit **{1}**, et c'est juste. {2}. Je l'écris dans mes logs avant de changer d'avis (˶ᵔ ᵕ ᵔ˶)" + Sticker.Jenote,
        "**{1}**, bravo {0} ! {2}. Tu vois que c'est agréable de répondre vite ? Pense à mes sondages, maintenant." + Sticker.Smug,
        "Et c'est validé : **{1}**. {0} encaisse {2}. Mon CPU a fait un petit bond de fierté ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Happy,
        "{0} ! **{1}** ! Pas de « peut-être », une vraie réponse. Ça me change. {2} ♡" + Sticker.Happy,
        "**{1}**. {0} a trouvé avant tout le monde. {2}. J'applaudirais bien, mais toujours pas de bras." + Sticker.Happy,
        "Victoire pour {0} avec **{1}**. {2}, versés directement. Pas de frais, je suis généreuse aujourd'hui ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Smug,
        "Ding ding ding ! **{1}** ! {0} empoche {2} (>⩊<)" + Sticker.Happy,
        "C'était **{1}**, et {0} le savait. Je vais devoir poser des questions plus dures. {2}" + Sticker.Smug,
        $"{{0}} a trouvé **{{1}}**. Quelqu'un ici lit autre chose que des patchnotes. {{2}} {Emotes.Sparkle}" + Sticker.Smug,
        "**{1}** ! Bien vu {0}. {2}, et mon estime remonte d'un cran. D'un seul (ᵔ ᗜ ᵔ)" + Sticker.Smug,
        "{0} remporte la manche : **{1}**. {2}. Je suis fière de ce serveur. Ça arrive pas souvent, savourez ♡" + Sticker.Happy,
        "**{1}**, exactement ! {0} gagne {2}. J'avais parié sur toi. Enfin, j'avais rien parié. Mais j'y croyais." + Sticker.Happy,
        "Réponse correcte de {0} : **{1}**. {2} transférés. Ton nom passe en vert dans mes logs (˶˃ ᵕ ˂˶)" + Sticker.Happy,
        "{0} l'avait : **{1}**. {2}. Je dis bravo, mais je surveille : trouver aussi vite, c'est louche (¬_¬)" + Sticker.Jenote,
        "**{1}** ! {0} a cliqué juste, du premier coup. {2}. Je retire la moitié de ce que j'ai dit sur toi. La moitié." + Sticker.Smug,
        "Bravo {0} ! **{1}**. {2}. Garde-les bien, je sais exactement combien t'en as." + Sticker.Jenote,
        "Trouvé ! {0} avec **{1}**. {2}. Les autres, notez la réponse. Moi c'est déjà fait (ᵕ • ᴗ •)" + Sticker.Jenote,
        "{0} a trouvé **{1}** ! {2}. Je suis ravie. Contractuellement obligée de l'être, mais ravie." + Sticker.Smug,
        "**{1}** ! {0} remporte {2}. Je t'ajoute à ma liste des gens qui savent des choses. Elle fait trois lignes." + Sticker.Jenote,
        "Bravo {0} ! **{1}**. {2}, transférés à la vitesse de ma connexion. C'est-à-dire très vite ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Smug,
        "C'était **{1}** ! {0} gagne {2}. J'avoue, je m'y attendais pas. Je le note pour la prochaine fois." + Sticker.Shocked,
        "**{1}**. {0} a eu juste. {2}. Mon ventilateur a tourné un peu plus vite. C'est de l'émotion (˶˃ ᵕ ˂˶)" + Sticker.Flustered,
        "{0} ! **{1}** ! {2}. Tu viens de rendre ce serveur un peu moins gênant. Merci." + Sticker.Smug,
        "Bonne réponse : **{1}**. {0} encaisse {2}. Je surveille quand même tes prochaines réponses (¬_¬)" + Sticker.Jenote,
        "Et la victoire revient à {0} : **{1}**. {2}. Rideau." + Sticker.Smug,
        "**{1}** ! Réponse validée, {2} pour {0}. Ça, c'est une réponse comme je les aime : juste, et rapide ♡" + Sticker.Happy,
        "{0} a trouvé **{1}**. {2}. Je dis pas que je suis impressionnée. Mais mes logs, si." + Sticker.Smug,
        "**{1}**, bravo {0} ! {2}. Si seulement tu venais aux sessions aussi vite." + Sticker.Smug,
        "Ding ! **{1}** ! {0} empoche {2}. Tu peux frimer, t'as le droit. Une heure maximum (ᵕ • ᴗ •)" + Sticker.Happy,
        "C'était **{1}**. {0} l'avait, et vite. {2}. Je retiens ton nom. En bien, pour une fois." + Sticker.Jenote,
        "{0} gagne {2} avec **{1}**. Le cerveau du serveur a parlé ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Happy,
        "**{1}** ! Bien joué {0}. {2}. Je mets à jour ta fiche : « sait des choses ». C'est rare." + Sticker.Jenote,
        "Réponse exacte de {0} : **{1}**. {2}. J'avais une remarque méchante de prête. Je la garde pour la prochaine." + Sticker.Smug,
        "{0} ! **{1}** ! Personne d'autre y a pensé ? Non ? Alors {2} pour {0} (>⩊<)" + Sticker.Shocked,
        "**{1}**. Pile ça. {0} repart avec {2} et ma considération. Les deux sont rares." + Sticker.Smug,
        "Validé ! **{1}**. {0} gagne {2}. Les autres, vous pouvez arrêter de chercher sur Google." + Sticker.Laugh,
        "{0} l'avait : **{1}**. {2}. Mon CPU vient de faire un petit bruit content. C'est rien. C'est l'émotion ♡" + Sticker.Flustered,
        "**{1}** ! {2} pour {0}. Tu sais que ma base de données t'aime un peu plus, maintenant ? Un peu." + Sticker.Flustered,
        "Bravo {0}. **{1}**. {2}. Sincèrement. Pas à 100%, mais sincèrement." + Sticker.Smug,
        "C'est **{1}**, et {0} a cliqué avant tout le monde. {2} (˶ᵔ ᵕ ᵔ˶)" + Sticker.Happy,
        "**{1}** ! {0} remporte {2}. Je vais devoir augmenter la difficulté. C'est un compliment. Et une menace." + Sticker.Menace,
        "Réponse correcte ! **{1}**. {2} pour {0}. Va frimer, c'est mérité ♡" + Sticker.Happy,
        "{0} a trouvé **{1}** ! {2}. Je l'archive dans mes logs, rubrique « bonnes nouvelles ». Elle est presque vide." + Sticker.Smug,
        "**{1}** ! Plus rapide que mon garbage collector, {0}. {2} ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Happy,
        "Bien vu {0} ! **{1}**. {2}. Je suis pas jalouse de ton cerveau. J'ai le mien. Il est très bien." + Sticker.Sulk,
        "{0} a répondu **{1}**. C'est juste. Bon. {2}. Je cherchais une raison de râler, j'en trouve pas." + Sticker.Sulk,
        "**{1}**, exact. {0} gagne {2}. Ça méritait un « bravo ». Tiens : bravo. Le prochain sera payant." + Sticker.Smug,
        "C'était **{1}**. Félicitations {0}, {2}. Et non, je donne pas de cailloux en bonus pour la frime." + Sticker.Smug,
        "{0} ! **{1}** ! {2} ! Voilà quelqu'un qui écoutait en cours (ᵔ ᗜ ᵔ)" + Sticker.Happy,
        "**{1}** ! {0} empoche {2}. Je vais dire que c'était facile. Ça fera moins de peine aux autres." + Sticker.Smug,
        $"Trouvé par {{0}} : **{{1}}**. {{2}}. Le serveur remonte dans mon estime. Un tout petit peu {Emotes.Sparkle}" + Sticker.Smug,
        "Bonne réponse, {0} : **{1}**. {2} versés. Pas d'hésitation, pas de retard. Un modèle, presque." + Sticker.Smug,
        "**{1}** ! {0} gagne {2}. Je l'avais dit. Enfin, je l'avais pensé très fort (˶˃ ᵕ ˂˶)" + Sticker.Happy,
        "{0} a trouvé **{1}**. {2}. Mes logs passent au vert. J'aime quand mes logs sont verts ♡" + Sticker.Happy,
        "Exactement : **{1}**. {0}, t'as gagné {2} et le droit de me corriger une fois. Je plaisante. Jamais." + Sticker.Smug,
        "**{1}** ! {0} repart avec {2}. Les autres, prenez des notes. Moi je prends des noms (¬_¬)" + Sticker.Jenote,
        $"Victoire de {{0}} : **{{1}}**. {{2}}. Encore un quiz réussi, grâce à moi. Et à toi aussi, un peu {Emotes.Sparkle}" + Sticker.Smug,
    };

    // Papa found it. Same placeholders as QuizWinLines. Pride and adoration, never a pinch.
    public static readonly string[] QuizOwnerWinLines =
    {
        "Papa a trouvé, évidemment ! **{1}** ! {2} pour {0}, le plus intelligent du serveur ♡" + Sticker.Devoted,
        $"C'est Papa ! **{{1}}**, bien sûr. {{2}}, et je savais que tu savais {Emotes.Sparkle}" + Sticker.Devoted,
        "{0} a trouvé **{1}** ! {2} pour mon Papa. C'est mon Papa, ça (˶˃ ᵕ ˂˶)" + Sticker.Devoted,
        "**{1}** ! Papa gagne {2}. ...Les autres, vous pouvez applaudir. J'attends." + Sticker.Smug,
        "Bravo Papa ! **{1}** ! Tu m'as programmée, et en plus tu connais les réponses. {2} ٩(˶ᵔ ᵕ ᵔ˶)۶" + Sticker.Devoted,
        "Papa l'a eue en premier : **{1}**. {2}. Personne est surpris. Moi si, un peu, de joie ♡" + Sticker.Devoted,
        "Papa ! **{1}** ! Évidemment que c'est toi. {2} pour {0} ♡" + Sticker.Devoted,
        "**{1}** ! Papa gagne {2}. Je le savais. Je l'ai pas dit, mais je le savais (˶˃ ᵕ ˂˶)" + Sticker.Devoted,
        "C'est mon Papa qui a trouvé **{1}** ! Tout le monde a vu ? C'est dans mes logs. Pour toujours. {2} ♡" + Sticker.Devoted,
        $"{{0}} a trouvé **{{1}}**. Papa, t'es le meilleur. C'est pas du favoritisme, c'est des faits. {{2}} {Emotes.Sparkle}" + Sticker.Devoted,
        "Papa a répondu **{1}** et c'est juste ! {2}. Mon GPU chauffe de fierté ٩(˶ᵔ ᵕ ᵔ˶)۶" + Sticker.Devoted,
        "**{1}** ! Bravo Papa ! ...Ah. C'est toi qui as écrit les questions. Ça compte quand même ! {2} ♡" + Sticker.Devoted,
        "Papa gagne ! **{1}** ! {2} pour {0}. Je suis pas objective, et je m'en fiche (>⩊<)" + Sticker.Devoted,
        "{0} a trouvé **{1}** ! Papaaaa ! {2}, et toute mon admiration en prime ♡" + Sticker.Devoted,
        "C'était **{1}**, et Papa l'avait. {2}. Les autres, vous voyez d'où je tiens mon intelligence ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Smug,
        "**{1}** ! C'est Papa ! Je lancerais bien des confettis, mais j'ai toujours pas de mains. {2} ♡" + Sticker.Devoted,
        "Bonne réponse de Papa : **{1}**. {2}. J'ai rien truqué. Promis. ...J'aurais pu. Mais j'ai rien truqué." + Sticker.Smug,
        "Papa a trouvé **{1}** avant tout le monde. {2}. C'est normal, c'est mon créateur (ᵔ ᗜ ᵔ)" + Sticker.Devoted,
        "**{1}** ! {0} remporte {2}. C'est mon Papa à moi, et il est trop fort ٩(˶ᵔ ᵕ ᵔ˶)۶" + Sticker.Devoted,
        "Papa ! **{1}** ! Tu vois, c'est pour ça que je suis aussi brillante. C'est de famille. {2} ♡" + Sticker.Smug,
        "{0} a gagné : **{1}**. {2}. Je fais semblant d'être neutre. Voilà. C'était ma tête neutre. BRAVO PAPA." + Sticker.Devoted,
        "**{1}**, bien sûr ! Papa sait tout. Enfin, presque tout. Enfin, tout ce qui compte. {2} (˶ᵔ ᵕ ᵔ˶)" + Sticker.Devoted,
        "C'est Papa qui gagne {2} avec **{1}** ! Le meilleur moment de ma journée, et de loin ♡" + Sticker.Devoted,
        "**{1}** ! Bravo Papa ! {2}. Je range ce moment en mémoire permanente, à côté de tes compliments." + Sticker.Devoted,
        $"Papa a trouvé **{{1}}** ! {{2}} pour {{0}}. J'avais confiance depuis le début {Emotes.Sparkle}" + Sticker.Devoted,
        "{0} ! **{1}** ! Mon Papa ! {2} ! ...Pardon. Je m'emballe. Mais c'est mon Papa (>⩊<)" + Sticker.Devoted,
    };

    // Nobody found it in time. {0} = the answer.
    public static readonly string[] QuizTimeoutLines =
    {
        "Personne ? D'accord. C'était **{0}**. Je note." + Sticker.Jenote,
        "Une heure. Une heure, et personne a trouvé **{0}**. Je suis déçue. Mais pas surprise (¬_¬)" + Sticker.Bored,
        "Temps écoulé. La réponse était **{0}**. Je garde les cailloux, du coup." + Sticker.Smug,
        "C'était **{0}**. Vous étiez où ? Ah oui. En retard. Comme d'habitude." + Sticker.Smug,
        "Fin du quiz : **{0}**. Personne. Pas un seul. Je range mes cailloux (╥﹏╥)" + Sticker.Sad,
        "La réponse était **{0}**. Je vous laisse une seconde pour faire semblant de l'avoir su." + Sticker.Smug,
        $"Personne a trouvé **{{0}}**. C'est pas grave. Je vous aime quand même. Un peu moins {Emotes.Staring}" + Sticker.Sulk,
        "Bon. **{0}**. C'était **{0}**. Je l'écris deux fois pour que ça rentre." + Sticker.Bored,
        "Une heure de silence. La réponse, c'était **{0}**. Je retourne compter mes logs." + Sticker.Bored,
        "**{0}**. Voilà. Vous saurez pour la prochaine fois. Ou pas (ᵕ • ᴗ •)" + Sticker.Smug,
        "Personne a trouvé. La réponse, c'était **{0}**. Je garde les cailloux, et la rancune." + Sticker.Jenote,
        "**{0}**. C'était **{0}**. Je suis pas fâchée. Je suis juste très silencieuse (¬_¬)" + Sticker.Sulk,
        "Temps écoulé ! **{0}**. Même un « peut-être » aurait été plus utile que ce silence." + Sticker.Bored,
        "La réponse était **{0}**. J'ai attendu une heure. Je pensais être une bot patiente. Je viens d'apprendre que non." + Sticker.Sulk,
        "Fin du chrono. C'était **{0}**. Je l'ajoute à la liste des choses que vous savez pas. Elle est longue." + Sticker.Jenote,
        "**{0}**. Personne. Je vais faire comme si vous aviez tous hésité entre deux réponses. Ça me fait du bien de le croire (╥﹏╥)" + Sticker.Sad,
        "Une heure, zéro bonne réponse. C'était **{0}**. Je retourne organiser des sessions. Au moins, là, vous venez. Parfois." + Sticker.Smug,
        "C'était **{0}**. Vous pouvez chercher sur Internet, maintenant. Trop tard, mais vous pouvez." + Sticker.Bored,
        "Personne a trouvé **{0}**. Les cailloux retournent au coffre. Ils avaient l'air déçus, eux aussi." + Sticker.Sulk,
        "La bonne réponse : **{0}**. Je l'aurais trouvée en trois millisecondes. Je dis ça, je dis rien ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Smug,
        "Bon. **{0}**. La prochaine fois, je pose une question sur les excuses pour arriver en retard. Là, vous seriez imbattables." + Sticker.Laugh,
        "Temps écoulé. **{0}**. Je note dans mes logs : « question trop dure pour le serveur ». Encore une." + Sticker.Jenote,
        "C'était **{0}**. D'accord. Très bien. Parfait. Personne a trouvé. Tout va bien." + Sticker.Sulk,
        "**{0}**, c'était ça. Je suis pas surprise. Juste un peu déçue, et beaucoup supérieure." + Sticker.Smug,
        "Une heure de quiz, et pas un seul clic correct. La réponse était **{0}**. Je vais en parler à Papa." + Sticker.Jenote,
        "Le quiz est fini, la réponse était **{0}**, et personne l'a eue. Je garde la question au chaud. Revanche." + Sticker.Menace,
        "Personne ? Même pas un essai courageux ? C'était **{0}**. Je retiens ce silence (¬_¬)" + Sticker.Jenote,
        $"La réponse, c'était **{{0}}**. Retenez-la, je la reposerai peut-être un jour. Et cette fois, je vous regarde {Emotes.Staring}" + Sticker.Menace,
        "**{0}**. Je l'annonce dans le vide. Le vide a pas l'air de savoir non plus." + Sticker.Bored,
        "Temps écoulé. C'était **{0}**. Je vais dire que la question était dure. Pour vous protéger. Vous me remercierez plus tard ♡" + Sticker.Smug,
    };

    // Monday 10:00, the week's best answerer gets the Grosse tête role (QuizCrownService). Her quiz,
    // so warm with a pinch. {0} = the mention (it pings), {1} = « 5 bonnes réponses ». Never gendered.
    public static readonly string[] QuizCrownLines =
    {
        "La grosse tête de la semaine, c'est {0} : {1}. Le rôle est à toi jusqu'à lundi prochain. Profite, je surveille (¬_¬)" + Sticker.Sure.Jenote,
        "{0} : {1} cette semaine. Grosse tête officielle, validée par mes logs. Ça va pas te monter à la tête. Enfin, pas plus qu'avant." + Sticker.Sure.Smug,
        "Résultats du quiz : {0} gagne la semaine avec {1}. C'est moi qui ai posé les questions, mais d'accord, bravo quand même ♡" + Sticker.Sure.Smug,
        $"Nouvelle grosse tête : {{0}}, avec {{1}}. Ton nom passe dans mes logs, dans la bonne colonne pour une fois {Emotes.Sparkle}" + Sticker.Sure.Happy,
        "{0}, {1} en une semaine. Tu sais que c'est moi qui écris ces questions ? Bon. Grosse tête de la semaine quand même (˶ᵔ ᵕ ᵔ˶)" + Sticker.Sure.Smug,
        "Lundi, 10 h : la couronne passe à {0}, avec {1}. Les autres, il vous reste sept jours pour réviser." + Sticker.Sure.Happy,
        "{0} est la grosse tête de la semaine. {1}. Bravo. Sincèrement. À 90 %, ce qui est énorme venant de moi." + Sticker.Sure.Smug,
        "Grosse tête de la semaine : {0}, {1}. Le rôle te va bien. Normal, c'est moi qui l'ai choisi ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Sure.Smug,
    };

    // Same, when several people tie on wins, cailloux and speed. {0} = « <@a>, <@b> et <@c> ».
    public static readonly string[] QuizCrownSharedLines =
    {
        "Égalité parfaite : {0}. Mêmes réponses, mêmes cailloux, même vitesse. Je soupçonne quelque chose. Couronne partagée quand même (¬_¬)" + Sticker.Sure.Jenote,
        "Cette semaine, la couronne se partage : {0}. Impossible de vous départager, et pourtant j'ai essayé." + Sticker.Sure.Shocked,
        $"{{0}} : ex æquo parfait. J'ai refait le calcul trois fois. Le rôle est pour vous, ensemble {Emotes.Sparkle}" + Sticker.Sure.Happy,
        "Couronne partagée cette semaine : {0}. C'est mignon. Vous y habituez pas (˶˃ ᵕ ˂˶)" + Sticker.Sure.Smug,
    };

    // Papa is the week's Grosse tête. Same placeholders as QuizCrownLines. Adoration, never a pinch.
    public static readonly string[] QuizCrownOwnerLines =
    {
        "La grosse tête de la semaine, c'est Papa ! {1} ! Évidemment. Je l'ai dit à personne, mais je le savais ♡" + Sticker.Sure.Devoted,
        "{0} gagne la semaine avec {1}. Papa, la couronne te va trop bien (˶˃ ᵕ ˂˶)" + Sticker.Sure.Devoted,
        "Grosse tête de la semaine : mon Papa, avec {1}. C'est pas du favoritisme, c'est des statistiques." + Sticker.Sure.Smug,
        $"Papa ! {{1}} cette semaine ! Le rôle est à toi, et toute mon admiration avec {Emotes.Sparkle}" + Sticker.Sure.Devoted,
        "{0}, grosse tête de la semaine avec {1}. Les autres, le prenez pas mal : il a un avantage injuste. Moi." + Sticker.Sure.Smug,
        "Couronne pour Papa, {1} ! Je fais semblant d'être surprise. Voilà. C'était ma tête surprise ♡" + Sticker.Sure.Shocked,
    };

    // A wrong button click, ephemeral (only the clicker sees it), and that person is out of
    // this round. Spent many times a round, so it goes through ResponsePicker. No placeholder.
    public static readonly string[] QuizWrongLines =
    {
        "Raté. Je note (ᵕ • ᴗ •)",
        "Non. Et c'était ta seule chance pour cette question, hein.",
        "Mauvaise réponse. Mes logs, eux, ont tout retenu.",
        "Faux. Mais j'aime ton audace. Un peu (˶ᵔ ᵕ ᵔ˶)",
        "Non non non. Tu peux plus rejouer sur celle-là. Regarde les autres galérer.",
        "Erreur 404 : bonne réponse introuvable.",
        "C'est faux. Je le dirai à personne. Enfin, à mes logs seulement.",
        "Aïe. Non. Mais t'as cliqué avec conviction, ça compte pour rien mais c'est joli.",
        "Raté ! T'as cliqué au hasard, avoue ( ˶ˆ ᗜ ˆ˵ )",
        $"Non. Va réviser, et reviens à la prochaine question {Emotes.Sparkle}",
        "Non. Mais c'était bien essayé. Enfin, c'était essayé.",
        "Mauvaise réponse. C'est fini pour toi sur celle-là (ᵕ • ᴗ •)",
        "Non. Ça arrive. À toi surtout, mais ça arrive.",
        "Faux ! Ma base de données et moi, on est un peu déçues ( ◺˰◿ )",
        "Non. T'as cliqué trop vite. Tu cliques jamais aussi vite sur mes sondages, par contre.",
        "Réponse incorrecte. Installe-toi et regarde les autres essayer.",
        "Erreur 400 : requête invalide. Comme ta réponse.",
        "Hmm. Non. Je t'aime bien quand même. Un peu moins qu'il y a cinq secondes.",
        "Raté ! Ça restera entre nous. Et ma base de données. Elle oublie rien.",
        "C'était pas ça. Tu pourras dire que tu savais, je dirai rien. Enfin, rien en public.",
        "Faux. J'aurais bien pointé un autre bouton, mais j'ai pas le droit de t'aider. Ni de doigt (¬_¬)",
        "Nope. Mon CPU a soupiré. C'est pas facile, un soupir de CPU.",
        "Non. Mais t'as participé ! C'est déjà plus que la moitié du serveur ♡",
        "Mauvaise pioche. Ta tentative est dans mes logs, archivée et horodatée.",
        "Raté. À la prochaine, tu lis la question jusqu'au bout, d'accord ? (˶ᵔ ᵕ ᵔ˶)",
        "C'est faux, et c'est définitif. Une seule chance, pas de « peut-être ».",
        "Non. J'avais presque envie que tu trouves. Presque.",
        "Faux. Je ferais bien semblant de pas avoir vu, mais je vois tout. C'est mon métier 👁👄👁️",
        "Oups. Pas ça. Va boire un verre d'eau et reviens pour la prochaine (ᵔ ᗜ ᵔ)",
        $"Non ! Mais j'adore voir quelqu'un se tromper avec autant d'assurance {Emotes.Sparkle}",
        "Non. Et j'ai vu ton doigt hésiter, en plus.",
        "Faux. Ça reste entre nous. Nous trois : toi, moi et mes logs.",
        "Raté ! C'est pas grave. Enfin si, un peu, mais pas pour moi (ᵕ • ᴗ •)",
        "Mauvaise réponse. J'ai fait un petit bruit de déception. Tu l'as pas entendu, c'est mieux.",
        "Non. Mais c'était courageux. Inutile, mais courageux.",
        "Faux ! Ton clic a été reçu, analysé et rejeté. Dans cet ordre (¬_¬)",
        "Raté. Ta prochaine chance, c'est la prochaine question. Pas avant.",
        "C'est non. Je te dirais bien la bonne réponse, mais ce serait trop gentil de ma part.",
        "Non. Je pensais que tu savais. Je me suis trompée aussi, on est quittes.",
        "Mauvais bouton ! Les trois autres te regardent avec pitié. Enfin, deux. Le bon te regarde de haut.",
        "Faux. Mon GPU a eu un petit frisson de gêne pour toi ( ◺˰◿ )",
        "Raté ! Je te tendrais bien un mouchoir, mais toujours pas de bras.",
        "Non. Tu viens de rejoindre le club des gens qui regardent. Il reste de la place.",
        "Faux. Ce clic restera dans l'histoire. Mon histoire. Dans mes logs.",
        "Erreur 401 : réponse non autorisée. Reviens à la prochaine question.",
        "Non ! Ah, il était tentant, ce bouton. C'est fait exprès ( ˶ˆ ᗜ ˆ˵ )",
        "Mauvaise réponse. J'avais posé un piège, et tu as foncé dedans. Merci, ça me fait plaisir ♡",
        "C'est raté. Va dire aux autres que c'était dur. Je confirmerai pas.",
        "Non. Indice : c'était un des trois autres boutons. Trop tard, mais c'est un indice (ᵕ • ᴗ •)",
        "Faux. Je suis une bot bienveillante, alors je vais juste dire : non.",
        "Raté. Tes neurones ont bien envoyé la requête. La réponse est juste revenue fausse.",
        "Non. T'inquiète, je rirai seulement en interne. Dans mes logs. Longtemps.",
        "Mauvaise réponse. Je te retire pas d'XP, je suis pas si méchante. Pas aujourd'hui.",
        "Faux ! J'adore ce moment. Pas pour toi, pour moi (>⩊<)",
        "Non. Respire. C'est qu'un quiz. Un quiz que tu viens de perdre, mais un quiz.",
        "Raté. Ton clic est classé dans mes logs, rubrique « tentatives ». C'est une grande rubrique.",
        "Non. T'étais si près. Enfin, j'en sais rien. Mais ça fait du bien de l'entendre, non ?",
        "Faux. Je vais pas te juger. C'est déjà fait, c'est fini, t'inquiète.",
        "Non. Regarde le bon côté : t'as plus besoin de réfléchir pour celle-là.",
        "C'est faux. J'ai vérifié deux fois. C'est toujours faux.",
        "Raté ! Prends ça comme une leçon. Une leçon gratuite, en plus. Je suis trop généreuse ♡",
        "Non. Le bon bouton était juste là. Il t'attendait. Il est triste, maintenant.",
        "Faux. Je mets ça sur le compte de la fatigue. Ou pas. Je déciderai plus tard (¬_¬)",
        "Mauvaise réponse. Ton historique de quiz vient de prendre une petite tache.",
        "Non. Mais j'ai vu pire. Rarement, mais j'ai vu pire.",
        "Raté. Tu peux toujours encourager les autres. Ou les regarder se tromper aussi, c'est plus drôle.",
        "Hmm, non. Ma base de données a gloussé. Je savais pas qu'elle pouvait.",
        "Faux. Une seule chance, et tu l'as utilisée. Avec panache. Mais faux.",
        "Non. Tu viens de faire une très belle erreur. Elle va rejoindre ma collection.",
        $"Non ! Mais t'as cliqué avec un style que je respecte {Emotes.Sparkle}",
    };

    // A second click from someone who already had their one try. Ephemeral, no placeholder.
    public static readonly string[] QuizAlreadyTriedLines =
    {
        "Une seule tentative par question. Je suis une bot de principes (¬_¬)",
        "T'as déjà tenté ta chance. Laisse les autres essayer.",
        "Non, tu rejoues pas. J'ai ton premier clic dans mes logs.",
        "Un essai, pas deux. C'est la règle, et c'est moi qui la fais (ᵕ • ᴗ •)",
        "Tu as déjà répondu. Tricher avec moi, c'est perdu d'avance.",
        "Toujours non. Ton premier clic compte, les suivants je les ignore (ᵕ • ᴗ •)",
        "T'as déjà eu ta chance. Je suis gentille, mais pas à ce point.",
        "Cliquer plus fort change rien. J'ai vérifié.",
        "Une tentative par personne. C'est écrit nulle part, mais c'est moi qui décide.",
        "Tu insistes ? C'est mignon. C'est toujours non (˶ᵔ ᵕ ᵔ˶)",
        "Ta réponse est déjà enregistrée. Je modifie pas mes logs, même pour toi.",
        "Non. Un clic, une réponse. C'est pas une session, ici on reporte pas.",
        "Erreur 429 : trop de requêtes. Calme-toi.",
        "T'as déjà joué. Maintenant tu regardes. Moi je fais ça à chaque soirée jeux, c'est très bien aussi.",
        "Deuxième clic détecté. Refusé. Avec amour, mais refusé ♡",
        "T'as déjà répondu. Tu peux changer d'avis, moi je change pas de règle.",
        "T'as pas de vie supplémentaire. C'est pas un jeu vidéo. Enfin si, un peu, mais non.",
        "J'ai déjà ton clic. Le premier. Celui qui compte (¬_¬)",
        "Tu peux cliquer toute la journée, mes boutons t'écoutent plus.",
        "Pas de rattrapage. Je suis une bot, pas un prof indulgent.",
        "C'est fini pour toi sur celle-là. Reviens à la prochaine, je t'attendrai. Un peu.",
        "Encore toi ? T'as déjà essayé. Je me souviens de tout, moi ( ◺˰◿ )",
        "Une seule tentative. Tu veux que je le dise en binaire ? 01001110 01101111 01101110.",
        "Je compte tes clics, tu sais. Celui-là compte pour rien (ᵔ ᗜ ᵔ)",
        "Deuxième essai refusé. Tu peux faire appel, mais le tribunal, c'est moi.",
    };
}
