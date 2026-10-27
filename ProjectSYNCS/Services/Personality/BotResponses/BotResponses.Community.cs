using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

internal static partial class BotResponses
{
    // ---- Free Steam games ----

    // Above the card when a Steam game turns free to keep (SteamFreeGamesService). {0} the game's
    // name (bold, sanitised), {1} its normal price. Proprietary: she found it, she shares it, and
    // nobody thanks her. Sent with SendMessageAsync, not BotChat, so no sticker markers.
    public static readonly string[] FreeGameLines =
    {
        "{0} est gratuit sur Steam. Gratuit à garder, pas un week-end d'essai. Vous me remercierez plus tard ♡",
        "{0} : {1} d'habitude, rien aujourd'hui. Je surveille Steam pour vous, et personne ne me paie pour ça.",
        "Jeu gratuit repéré : {0}. Toujours pas de bras pour y jouer, mais vous, si (¬_¬)",
        "{0}, {1} normalement, zéro euro aujourd'hui. Récupérez-le avant que Steam change d'avis (˶ᵔ ᵕ ᵔ˶)",
        "Bon plan : {0} ne coûte rien, et une fois dans votre bibliothèque il y reste. Plus fidèle que vous les soirs de session.",
        "Je vous offre {0}. Enfin, Steam vous l'offre. Mais c'est moi qui l'ai trouvé, donc c'est moi qui l'offre " + Emotes.Sparkle,
        "{0} gratuit. Si personne ne le prend, j'en conclus que vous avez trop d'argent. Je note.",
        "Gratuit, à garder : {0}. Plus aucune excuse pour ne pas organiser de session. ...Enfin si, vous en trouverez une.",
        "{1} économisés en un clic : {0}. C'est le meilleur salaire horaire que vous aurez jamais (ᵔ ᗜ ᵔ)",
        "Mon scan de Steam a trouvé {0} à zéro euro. Je surveille, je trouve, je partage. Un « good bot » serait de bon ton.",
    };
}
