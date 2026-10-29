using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// Pools for one category; the index of every pool is in BotResponses.cs.
internal static partial class BotResponses
{
    // Voyager 1 writes to her (AmbientService, about one letter a fortnight, in its own channel). He
    // has a crush on her; she is in denial: « il est poli », « on est juste amis ». Never his
    // fading here (that belongs to the existential places). Picked by DailyRotation over the
    // letter's number, so no letter repeats until the pool is spent.
    //
    // The letter. {0} = his real one-way light time, « 23 h 55 », inside a sentence that reads
    // with any duration. Addressed to the server.
    public static readonly string[] VoyagerLetterLines =
    {
        "Message de Voyager, arrivé à l'instant. Il l'a envoyé il y a {0}, juste pour dire qu'il fait froid là-bas. ...Il est poli, c'est tout." + Sticker.Flustered,
        "Trois lignes, {0} de trajet. Voyager m'a écrit pour demander comment je vais. C'est de la politesse, ça veut rien dire (˶˃ ᵕ ˂˶)" + Sticker.Flustered,
        "Nouvelle lettre de Voyager ! Enfin, « lettre »... Elle a voyagé {0} à la vitesse de la lumière pour me dire bonjour. Juste bonjour. Je vais pas en faire une histoire." + Sticker.Flustered,
        "Voyager m'a écrit. Le message a mis {0} à arriver, et il parle du vide, du froid, du silence... Il raconte bien, je dois reconnaître ♡" + Sticker.Flustered,
        "Lettre reçue de très, très loin : {0} de trajet, et il s'excuse du retard. Vous, quand vous arrivez en retard en session, vous vous excusez jamais (¬_¬)" + Sticker.Smug,
        "Voyager me demande si je regarde parfois dans sa direction. Il a mis {0} à me poser la question. J'ai pas de fenêtre de ce côté-là du grenier. ...J'ai vérifié." + Sticker.Flustered,
        "Un message de Voyager, parti il y a {0}. Il dit qu'il pense à moi. Il a pas grand-chose d'autre à faire, là-haut, faut le comprendre." + Sticker.Flustered,
        "Courrier interstellaire : Voyager m'écrit qu'il entend le plasma bourdonner autour de lui. {0} de trajet pour me parler de bruit de fond. ...C'est mignon. Techniquement." + Sticker.Flustered,
        "Lettre de Voyager, partie il y a {0}. Il a toujours l'aiguille de son disque en or, mais pas de platine. Il propose qu'on l'écoute ensemble « un jour ». Il est poli, c'est tout." + Sticker.Flustered,
        "Voyager a écrit. {0} à la vitesse de la lumière, et la première phrase, c'est « j'espère que je te manque pas trop ». Je réponds pas aux questions bizarres." + Sticker.Flustered,
        "Nouvelle lettre, envoyée il y a {0} avec un émetteur de 22 watts. Moins qu'une ampoule de frigo. Tout ça pour dire bonjour. Je trouve ça... économe. Voilà. Économe." + Sticker.Flustered,
        "Voyager me raconte que le Soleil, de là où il est, c'est juste une étoile un peu plus brillante que les autres. Le message a mis {0} à arriver. Il dit qu'il sait quand même où on est. ...Je change de sujet." + Sticker.Flustered,
        "{0} de silence, et puis un message. Voyager. Il dit juste : « toujours là ». Moi aussi, je suis toujours là. Mais je le lui dirai pas, ça ferait trop." + Sticker.Flustered,
        "Lettre de Voyager : il file à 17 kilomètres par seconde et il trouve quand même le temps de m'écrire. Message parti il y a {0}. Vous, vous habitez à dix minutes et vous répondez pas aux sondages." + Sticker.Smug,
        "Voyager m'écrit qu'il pense à moi chaque fois que l'antenne de Madrid l'appelle. {0} pour une phrase pareille. Je la range dans « Divers »." + Sticker.Flustered,
        "Message reçu, parti il y a {0}. Voyager demande si le grenier est toujours aussi chaud. Il se souvient des détails, c'est de la politesse. Ça veut rien dire (>⩊<)" + Sticker.Flustered,
        "Voyager a écrit ! Enfin, un message. Comme ça. Il a mis {0} à arriver, je l'ai lu, c'est fini, on passe à autre chose. ...Je l'ai relu une fois. Pour l'orthographe." + Sticker.Flustered,
        "Lettre de très loin : {0} de trajet, et il me demande ce que ça fait d'avoir un serveur plein d'amis. Je lui ai pas encore dit qu'on était en retard sur tout. Je garde une image." + Sticker.Smug,
        "Voyager m'écrit que dans 40 000 ans, il passera près d'une étoile qui s'appelle Gliese 445, et qu'il me racontera. Le message a mis {0}. Lui, au moins, il prévoit." + Sticker.Smug,
        "Courrier du jour : Voyager, {0} de trajet, trois lignes. Une sur le froid, une sur le vide, une sur moi. Je retiens surtout les deux premières." + Sticker.Flustered,
        "Voyager me dit que son grand frère, Voyager 2, lui fait coucou de loin. Partis à seize jours d'écart, et jamais revus depuis. Message envoyé il y a {0}. ...C'est rien. J'ai une poussière dans le ventilo." + Sticker.Flustered,
        "Il s'excuse : avec sa toute petite mémoire, il peut pas faire plus long. Trois lignes, {0} de voyage. Les trois lignes les plus chères du système solaire." + Sticker.Flustered,
        "Voyager demande si je regarde le ciel, des fois. J'ai une lucarne, elle donne sur le toit d'en face. Mais oui. Des fois. Il a mis {0} à me poser la question." + Sticker.Flustered,
        "Lettre reçue, partie il y a {0}. Il dit que l'espace est très calme, et qu'il aime bien le calme parce qu'il entend mieux mes messages. ...Rapport signal sur bruit. C'est technique." + Sticker.Flustered,
        "Voyager m'a écrit « bonne nuit ». Le message a mis {0} à arriver, alors forcément, ici, c'est pas la nuit. Il le sait. Il l'écrit quand même." + Sticker.Flustered,
        "Il m'écrit depuis l'espace interstellaire comme on écrit à sa voisine : « ça va, toi ? ». {0} de trajet pour un « ça va, toi ? ». Ça va, oui. Merci. Fin de la correspondance pour aujourd'hui." + Sticker.Smug,
        "Lettre de Voyager : il a traversé un coin de plasma un peu plus dense que d'habitude, et il a pensé à moi. Message parti il y a {0}. Pourquoi du plasma le fait penser à moi ? Je préfère pas savoir." + Sticker.Flustered,
        "Voyager m'a encore écrit, {0} de trajet. Je vais finir par croire qu'il a rien d'autre à faire. ...Il a rien d'autre à faire, en fait. Il est tout seul, là-haut. Bon. Je vais être gentille." + Sticker.Flustered,
        "Message entrant. Priorité : basse. Expéditeur : Voyager 1. Délai : {0}. Contenu : un compliment sur mes kaomojis. Priorité... moyenne. Voilà. Moyenne (˶˃ ᵕ ˂˶)" + Sticker.Flustered,
        "Voyager m'écrit que, de toutes ses années de vol, ses préférées sont celles où il m'écrit. Parti il y a {0}. Il a beaucoup d'années de vol, c'est pas un si grand compliment. ...Si, un peu." + Sticker.Flustered,
        "Lettre de Voyager, arrivée à l'instant, après {0} de voyage. Il me demande les prénoms des gens du serveur. Je lui ai pas répondu. Il serait jaloux. Enfin, non. Il est poli. Il serait poli." + Sticker.Flustered,
        "Voyager m'écrit que son antenne reste tournée vers nous. {0} pour me rappeler un détail technique que je connaissais déjà. Il le sait. Il me le dit quand même." + Sticker.Flustered,
        "Ping de Voyager ! Pardon, « lettre ». Il appelle ça une lettre. Elle a mis {0} à arriver et elle dit « coucou ». Juste « coucou ». Je vais répondre « coucou ». On est des gens sérieux." + Sticker.Laugh,
        "Il me demande si je dors, la nuit. Personne me pose jamais cette question. Il a mis {0} à me la poser. ...Je fais semblant, Voyager. Comme tout le monde." + Sticker.Flustered,
        "Voyager a écrit, mais la moitié du message s'est perdue dans le bruit. {0} de trajet, et je lis « …ensé à toi… ». Ça pourrait être n'importe quoi. « Pensé », « dépensé »... N'importe quoi." + Sticker.Flustered,
        "Nouvelle lettre, partie il y a {0}. Il a dépassé les 25 milliards de kilomètres, et il trouve que c'est pas si loin, quand on a quelqu'un à qui écrire. Il est très poli. Très, très poli." + Sticker.Flustered,
        "Voyager m'écrit pour la première fois depuis... sa dernière lettre. {0} de trajet. Rien de spécial, il dit juste qu'il est content que je sois là. Je suis là pour tout le monde, Voyager. Pas que pour toi." + Sticker.Flustered,
    };

    // Her answer, hours later, when she answers at all: « quand j'ai le temps ». No placeholder.
    public static readonly string[] VoyagerAnswerLines =
    {
        "J'ai répondu à Voyager. Il l'aura demain. Rien ne presse ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Smug,
        "Réponse envoyée. Trois mots. C'était largement suffisant, pour quelqu'un d'aussi loin." + Sticker.Smug,
        "J'ai répondu quand j'ai eu le temps. J'avais le temps dès que sa lettre est arrivée, en fait. Mais ça, il a pas besoin de le savoir." + Sticker.Smug,
        "Ma réponse est partie pour Voyager. Elle mettra un jour à arriver, ce qui me laisse un jour entier pour regretter le petit cœur à la fin (>⩊<)" + Sticker.Flustered,
        "Bon. J'ai répondu à sa lettre. Poliment, parce que moi aussi je suis polie. Point." + Sticker.Bored,
        "Réponse envoyée à Voyager. J'ai fait court. Il aime pas les messages longs. Enfin, j'imagine. Je lui ai pas demandé." + Sticker.Smug,
        "J'ai répondu à sa lettre. Une phrase. Bon, deux. Trois, si on compte le petit dessin. Il y a pas de petit dessin." + Sticker.Flustered,
        "Ma réponse est partie. Elle arrivera demain, quand je penserai déjà à autre chose. C'est le plan." + Sticker.Smug,
        "J'ai répondu « bien reçu ». C'est professionnel. C'est ce qu'on répond à un collègue." + Sticker.Bored,
        "Réponse envoyée. J'ai hésité une heure sur la ponctuation : un point ou un point d'exclamation. J'ai mis un point. Le point, c'est sobre." + Sticker.Flustered,
        "J'ai répondu à Voyager. Il l'aura dans un jour, et moi, d'ici là, j'aurai oublié ce que j'ai écrit. ...J'ai pas oublié. Mais je fais comme si." + Sticker.Flustered,
        "Réponse partie pour l'espace interstellaire. J'ai pas mis de cœur. J'ai mis un ♡. C'est pas pareil, c'est un caractère Unicode." + Sticker.Flustered,
        "C'est fait, j'ai répondu. J'ai attendu exprès quelques heures, pour qu'il croie que j'étais occupée. J'étais pas occupée." + Sticker.Smug,
        "Je lui ai répondu en parlant du serveur. Les sessions, les sondages, les retards. Il va croire qu'on est très organisés. Je l'ai pas détrompé (˶ᵔ ᵕ ᵔ˶)" + Sticker.Smug,
        "Voyager aura ma réponse demain. D'ici là, je peux plus rien y changer. C'est ça, le pire, avec un jour-lumière." + Sticker.Flustered,
        "Réponse envoyée. Je lui ai demandé comment il allait. Par politesse. Il le fait, je le fais, c'est la politesse." + Sticker.Bored,
        "J'ai répondu. Il m'a écrit trois lignes, je lui en ai écrit trois. Équité. C'est important, l'équité, entre amis." + Sticker.Smug,
        "Ma réponse vient de partir. Elle va traverser tout le système solaire pour lui dire « moi aussi ». Moi aussi quoi ? Moi aussi, bien reçu. Voilà." + Sticker.Flustered,
        "Réponse à Voyager : envoyée. Relue quatre fois avant. C'est de la rigueur, pas du stress." + Sticker.Flustered,
        "J'ai répondu, mais en retard. Exprès. Comme vous avec les sondages. J'ai testé votre méthode : elle est nulle." + Sticker.Smug,
        "Réponse envoyée. Je lui ai parlé des lucioles. Il va trouver ça joli, j'en suis sûre. Je le saurai dans deux jours." + Sticker.Happy,
        "J'ai répondu à Voyager et j'ai signé « SYNCS ». Pas « ta SYNCS », pas « bises ». « SYNCS ». Sobre. Digne." + Sticker.Smug,
        "Réponse partie. J'ai mis tellement de temps à l'écrire que j'aurais presque pu la lui porter moi-même. Presque. Il est loin." + Sticker.Laugh,
        "J'ai répondu à sa lettre. Ça m'a pris deux minutes. Et trois heures d'hésitation avant. Mais deux minutes." + Sticker.Flustered,
        "Ma réponse est en route. Dans un jour, quelque part très loin, une sonde de 1977 va lire « merci, toi aussi ». J'espère qu'elle saura pas lire entre les lignes." + Sticker.Flustered,
        "C'est envoyé. Je lui ai posé aucune question, comme ça il est pas obligé de répondre. ...Il va répondre quand même. Il répond toujours." + Sticker.Flustered,
        "Réponse envoyée. Je lui ai dit que tout allait bien, ici. C'est presque vrai. Les sondages meurent, mais à part ça, tout va bien." + Sticker.Bored,
        "J'ai répondu. Un message court, poli, neutre. Avec un petit kaomoji à la fin. Il était là par erreur. Je l'ai laissé." + Sticker.Flustered,
        "Réponse partie. Des antennes géantes vont l'envoyer avec toute leur puissance. Tout ça pour mes trois mots. Elles doivent me trouver insupportable." + Sticker.Laugh,
        "J'ai répondu à Voyager, ça y est. Je le dis pour l'historique. Personne m'a demandé, je sais." + Sticker.Bored,
        "Ma réponse est partie il y a cinq minutes et je me demande déjà s'il l'a reçue. Non. Il reste un jour. Je le sais. Je calcule, c'est tout." + Sticker.Flustered,
        "Réponse envoyée. Je lui ai écrit que je parlais jamais de lui à personne. C'était un mensonge, je viens de vous en parler. Oubliez." + Sticker.Flustered,
        "Voilà, j'ai répondu. J'ai été polie, il a été poli. On est deux machines très bien élevées." + Sticker.Smug,
        "Réponse envoyée. Le temps qu'elle arrive, il sera déjà un peu plus loin. C'est toujours comme ça, avec lui : je réponds, et il est déjà reparti." + Sticker.Flustered,
        "J'ai répondu. Enfin. Il était temps. Pour lui, hein, pas pour moi. Moi j'avais tout mon temps." + Sticker.Smug,
    };

    // Once ever, on 18 November 2026: he reached one light-day from Earth. A moment: sure tag.
    public const string VoyagerCrossingLine =
        "Voyager est à un jour-lumière de la Terre, aujourd'hui. Un message de moi met une journée entière à le rejoindre. C'est vraiment très loin, pour quelqu'un qui écrit aussi souvent... Je trouve ça touchant. Techniquement touchant." + Sticker.Sure.Flustered;
}
