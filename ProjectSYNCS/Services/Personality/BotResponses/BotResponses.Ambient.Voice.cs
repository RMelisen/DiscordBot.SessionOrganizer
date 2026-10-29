using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// Pools for one category; the index of every pool is in BotResponses.cs.
internal static partial class BotResponses
{
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
}
