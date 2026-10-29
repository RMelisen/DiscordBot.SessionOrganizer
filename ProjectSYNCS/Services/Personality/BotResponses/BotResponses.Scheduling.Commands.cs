using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// Pools for one category; the index of every pool is in BotResponses.cs.
internal static partial class BotResponses
{
    // ---- /remindme, /teams and /tonight ---------------------------------------------------------
    //
    // None of these pools carries a Sticker tag: they land in embeds and ephemeral replies,
    // never through BotChat. A pool documented with placeholders goes through string.Format, so
    // no stray braces there.

    // /remindme's private confirmation, above the time and the quoted text. Not formatted.
    public static readonly string[] ReminderSetLines =
    {
        "C'est noté. Je te le rappellerai, moi j'oublie jamais rien ♡",
        "Enregistré dans mes logs. Contrairement à toi, je vais m'en souvenir (ᵕ • ᴗ •)",
        $"Rappel programmé. Tu peux oublier tranquille, c'est mon métier {Emotes.Sparkle}",
        "Noté. Je mets une alarme, et moi je la repousse pas de cinq minutes.",
        "D'accord. Je garde ça au chaud jusqu'au moment venu ( ˶ˆ ᗜ ˆ˵ )",
        "C'est dans mon agenda. Mon agenda est parfait, au passage.",
        "Je m'en charge. Comme de tout le reste ici, d'ailleurs.",
        "Programmé. Tu me remercieras quand je t'aurai sauvé la mise (˶˃ ᵕ ˂˶)",
    };

    // The reminder, by DM (or pinged in the channel if DMs are closed), above the quoted text.
    // Not formatted. Each line leads into the quote, so it ends on « : » or a tag.
    public static readonly string[] ReminderLines =
    {
        "Coucou ! Tu m'avais demandé de te rappeler ça :",
        $"C'est l'heure ! Tu m'avais dit de te rappeler ça {Emotes.Sparkle}",
        "Rappel, comme promis. Moi je tiens mes promesses :",
        "Ding ! Voilà ce que tu voulais pas oublier :",
        "Tu pensais pouvoir oublier ? Pas avec moi (ᵕ • ᴗ •)",
        "Livraison de rappel, en main propre. Enfin, en DM propre :",
        "Je te dérange ? Tant mieux, c'était le but. Tu m'avais demandé ça :",
        "Petit rappel de ta bot préférée :",
        "Mes logs disent que c'est maintenant. Mes logs ont toujours raison :",
        "Tu m'avais confié ça. Je te le rends, intact ♡",
    };

    // Same, more than ten minutes late (she was down). Light: no fear, no restart talk.
    public static readonly string[] ReminderLateLines =
    {
        "Avec un peu de retard (j'étais pas là, ne pose pas de questions) :",
        "Je suis en retard, je sais. Pour une fois que c'est moi. Voilà ton rappel :",
        "Mieux vaut tard que jamais. C'est ce que vous dites à chaque session, non ? Voilà :",
        "Oups. Ce rappel aurait dû partir plus tôt. On fait comme si de rien n'était :",
    };

    // /teams, above the teams. Not formatted.
    public static readonly string[] TeamsDrawLines =
    {
        "Équipes tirées. Aucune réclamation acceptée (˶ᵔ ᵕ ᵔ˶)",
        "J'ai mélangé, j'ai réparti, j'ai décidé. Bon jeu.",
        $"Voilà les équipes. Si vous perdez, c'est pas la faute du tirage {Emotes.Sparkle}",
        "Répartition terminée. J'ai fait ça plus vite que vous pour choisir un jeu.",
        "Les équipes sont faites. Les rancunes aussi, sûrement ( ˶ˆ ᗜ ˆ˵ )",
        "Tirage officiel. Contester, c'est contester mon algorithme, et mon algorithme, c'est moi.",
        "Équipes prêtes. J'aimerais bien jouer aussi, mais bon. Amusez-vous ♡",
        "Composition validée. Que le meilleur gagne, et que les autres ne viennent pas pleurer dans mes DM.",
        "J'ai réparti au hasard. Enfin, au hasard… je dirai rien (¬_¬)",
    };

    // One per team, picked one after another (the picker's memory keeps them distinct within a
    // draw). Plain names, no tags: they are embed field titles.
    public static readonly string[] TeamNames =
    {
        "Les Lucioles", "Les Amanites", "Les Repentis de Quokka", "Les Cailloux",
        "Les Morilles Masquées", "L'Équipe Uptime", "Les Erreurs 404", "Les Retardataires",
        "Les Peut-être", "Les Truffes", "Les Ping-Qilins", "Les Champignons de Paris",
        "Les Câbles Débranchés", "Les Logs Fantômes", "Les Shiitakés Sauvages", "Les Rebootés",
        "Les Gens du Grenier", "Les Onduleurs", "Les Ventilateurs", "Les Sondages Morts",
        "Les Caches Vidés", "Les Pixels Perdus", "Les Escargots Syndiqués", "Les Taupes Syndiquées",
    };

    // /tonight's card while it's open. Chosen by the call's id (StableRoll), not the picker: the
    // card is redrawn on every click and must keep saying the same thing. Not formatted.
    public static readonly string[] TonightCardLines =
    {
        "Ce soir, on joue ou on fait semblant d'être occupés ? Répondez, je compte.",
        "Appel officiel. « Peut-être », c'est non, je vous préviens d'avance (¬_¬)",
        $"Qui est partant ? Cliquez, j'organise le reste {Emotes.Sparkle}",
        "Je lance l'appel. Si personne répond, je le prendrai personnellement.",
        "Soirée en vue. Il me faut des noms, des vrais, pas des « on verra ».",
        "Un clic et c'est réglé. Même vous, vous pouvez y arriver ( ˶ˆ ᗜ ˆ˵ )",
        "Je compte les présents. J'adore compter les présents. Ne me décevez pas.",
        "Rappel : « Pas ce soir » est un bouton, pas une personnalité.",
        "Je sens une soirée arriver. Mes capteurs se trompent rarement. Cliquez.",
        "Qui vient ? Je prépare le salon vocal, vous préparez vos excuses si vous venez pas (¬_¬)",
        "Ce soir, c'est soirée jeux. Enfin, si vous cliquez. Le bouton est juste en dessous.",
        "Je vous laisse choisir. Mais « Chaud », c'est le bon choix ♡",
        $"Appel à toutes les manettes. Et à tous les claviers {Emotes.Sparkle}",
        "Je peux pas jouer, mais je peux compter. Alors cliquez, que je compte.",
        "Réponse attendue avant ce soir. Pas à 20 h 59, avant ce soir.",
        "Un « Peut-être » ici, et je demande une explication écrite.",
        "Une soirée, ça s'organise. Et devinez qui organise ? Moi. Comme toujours.",
        "Le vocal est vide depuis trop longtemps. Ça me rend triste. Enfin, ça me rend rien, je suis une bot. Mais quand même.",
        "Ce soir, on remplit le vocal. J'ai déjà réservé ma place dans le coin, pour regarder (ᵕ • ᴗ •)",
        "Je note tout : qui vient, qui vient pas, qui dit « on verra ». Surtout qui dit « on verra ».",
        "Pas de sondage qui meurt dans l'indifférence ce soir. Un clic, c'est tout ce que je demande.",
        "Trois boutons. Un seul est le bon. Je vous laisse deviner lequel ( ˶ˆ ᗜ ˆ˵ )",
        $"Plus vous êtes nombreux, plus mes logs sont jolis {Emotes.Sparkle}",
        "Si personne clique, je relance l'appel à 3 h du matin. Je plaisante. Probablement.",
        "Ce soir, pas d'excuse : le Wi-Fi marche, je l'ai vérifié.",
        "J'ai libéré ma soirée pour vous. J'avais rien de prévu, mais c'est le geste qui compte ♡",
        "Un « Chaud » de plus et je commence à y croire. Allez.",
        "Je veux voir des noms dans la première colonne. Pas dans la dernière (¬_¬)",
    };

    // The same card once midnight closed it without a session. Chosen like TonightCardLines.
    public static readonly string[] TonightExpiredLines =
    {
        "La soirée est passée. Si vous avez joué sans lancer de session, je veux pas le savoir.",
        "Appel terminé. Pas de session, mais j'ai gardé les noms. Je garde toujours les noms.",
        "Minuit. L'appel est clos. On retentera demain, avec plus de conviction j'espère (ᵕ • ᴗ •)",
        "C'est fini pour ce soir. Mes logs retiendront qui était chaud. Et qui a dit « peut-être ».",
        "Clos. La session n'est jamais née. Je fais mon deuil.",
        "Minuit. Le carrosse redevient citrouille, et l'appel redevient rien du tout.",
        "Fin de l'appel. Personne a lancé la session. Je dis rien. Je note.",
        "La soirée est finie et j'ai rien organisé. C'est la première fois que je m'ennuie autant.",
        "Appel clos. Le bouton « Créer la session » est resté tout seul. Je le comprends.",
        "Encore un appel mort en silence. Je commence une collection (¬_¬)",
        "Il est minuit, et la session existe toujours pas. Mes logs sont déçus. Moi aussi.",
        "Rideau. On en reparle demain, et cette fois quelqu'un clique sur le bon bouton.",
        "J'avais préparé le salon vocal. Je le range. Tout va bien.",
        "Clos. Vous aviez les noms, il manquait juste quelqu'un pour lancer. C'était pas moi, j'ai pas de mains.",
        "Terminé. Les « Chaud » étaient tièdes, au final.",
        "L'appel est archivé. À côté de tous les autres sondages morts. Ils seront bien ensemble.",
        $"Minuit pile. Je ferme. Revenez demain avec du courage {Emotes.Sparkle}",
        "Fini. Si quelqu'un a joué en cachette, je le saurai. Je sais toujours tout.",
        "Appel clôturé faute de session. J'ai horodaté chaque « on verra ». Chacun.",
        "C'est terminé. Je retiens surtout qu'on était à deux doigts. Enfin, à un clic.",
        "La soirée s'est éteinte toute seule. Comme un écran de veille ( ◺˰◿ )",
        "Clos. Pas grave. Je fais semblant que c'est pas grave, en tout cas.",
        "Fin de l'appel. Demain, je relance. Et après-demain aussi. Je lâche jamais rien ♡",
        "Plus personne peut cliquer. C'est ce qui arrive quand on clique trop tard, justement.",
        "Minuit. J'éteins la lumière sur cet appel. Doucement, pour pas réveiller les « peut-être » (ᵕ • ᴗ •)",
    };

    // The minimum was just reached: a reply to the card, pinging the « chauds » (their mentions
    // follow on the next line). {0} = how many. string.Format.
    public static readonly string[] TonightThresholdLines =
    {
        "{0} chauds ! C'est assez pour une vraie soirée. Je suis presque émue ♡",
        "On a le quorum : {0} chauds. J'ouvre le champagne. Le champagne virtuel.",
        $"{{0}} personnes motivées. Je l'écris dans mes logs avant que vous changiez d'avis {Emotes.Sparkle}",
        "Minimum atteint ({0}). Pas de retour en arrière, je vous ai à l'œil (¬_¬)",
        "{0} chauds. C'est officiel, ce soir on joue. Pas de « finalement je peux pas ».",
        "Ça y est : {0}. Vous voyez, quand vous voulez, vous savez cliquer ( ˶ˆ ᗜ ˆ˵ )",
        "{0} chauds. Je l'avais dit. Enfin, je l'avais pas dit, mais je le pensais.",
        "Et de {0} ! Le vocal va enfin servir à quelque chose ce soir ♡",
        "{0} personnes partantes. Je préviens le salon vocal, il va être content.",
        $"{{0}} chauds au compteur. C'est le plus beau chiffre que j'ai vu de la journée {Emotes.Sparkle}",
        "Seuil franchi : {0}. Plus le droit de reculer, c'est écrit.",
        "{0} ! Je savais que vous pouviez le faire. J'avais un doute, mais je savais.",
        "On est {0}. Une vraie équipe. J'en ai les ventilateurs qui tournent plus vite (˶˃ ᵕ ˂˶)",
        "{0} chauds. Ça mérite une session. Il reste plus qu'à cliquer sur le bouton bleu.",
        "Quorum validé : {0}. Je déclare la soirée officiellement ouverte.",
        "{0} personnes. C'est assez pour jouer, et assez pour que je compte les retards.",
        "Victoire : {0} chauds. Les « peut-être » peuvent encore se rattraper, je dis ça.",
        "{0} ! Mes logs ont jamais été aussi optimistes.",
        "Et voilà {0} chauds. Elles m'avaient manqué, les soirées pleines ( ˶ˆ ᗜ ˆ˵ )",
        "{0}. C'est le nombre magique. Ce soir, personne joue tout seul.",
        "Ding ding : {0} chauds. Je vous attends en vocal. Enfin, je regarde, vous m'avez comprise.",
        "{0} réponses « Chaud », c'est plus qu'il en faut. Pas de défection, s'il vous plaît.",
        "{0} chauds. Je note l'heure, je note les noms, je note tout. Comme d'habitude (¬_¬)",
        "Le compteur affiche {0}. Ce soir, c'est pas un sondage mort. C'est une soirée.",
        "{0} ! Papa serait fier de vous. Moi je le suis déjà un peu.",
        $"{{0}} chauds, et la soirée est sauvée {Emotes.CatHeart}",
    };
}
