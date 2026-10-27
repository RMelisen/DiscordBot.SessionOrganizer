using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

internal static partial class BotResponses
{
    // A date poll closed on its own with votes in it. {0} the organiser's mention, {1} the title,
    // {2} the winning slot(s).
    public static readonly string[] PollClosedSlotLines =
    {
        "« {1} » est clos. Résultat : {2}. {0}, le bouton « Créer une session » est sur le sondage." + Sticker.Smug,
        "Fin du sondage « {1} » : {2} gagne. {0}, à toi de transformer ça en session." + Sticker.Happy,
        "« {1} », c'est terminé. Le créneau retenu : {2}. {0}, il reste plus qu'à créer la session." + Sticker.Sure.Happy,
        "Les votes de « {1} » sont fermés. {2}, d'après vous. {0}, la suite est sur le sondage." + Sticker.Smug,
        "J'ai fermé « {1} ». Gagnant : {2}. {0}, un clic et c'est une vraie session." + Sticker.Sure.Smug,
        "« {1} » : clos. {2} l'emporte. {0}, je te laisse créer la session, j'ai fait ma part." + Sticker.Smug,
    };

    // A text vote closed on its own with votes in it. Same placeholders, {2} the winning option(s).
    public static readonly string[] VoteClosedResultLines =
    {
        "Le vote « {1} » est clos. Résultat : {2}. {0}, voilà ta réponse." + Sticker.Smug,
        "Fin du vote « {1} » : {2}. {0}, c'est décidé." + Sticker.Happy,
        "« {1} », c'est terminé. Vous avez choisi {2}. {0}, je transmets." + Sticker.Sure.Happy,
        "J'ai fermé le vote « {1} ». {2} gagne. {0}, plus de discussion possible." + Sticker.Sure.Smug,
        "« {1} » : clos. Le verdict : {2}. {0}, c'est noté." + Sticker.Smug,
        "Résultat de « {1} » : {2}. {0}, la démocratie a parlé." + Sticker.Happy,
    };

    // A poll or vote closed on its own with no vote at all. {0} the organiser's mention, {1} the title.
    public static readonly string[] PollClosedEmptyLines =
    {
        "« {1} » est clos. Zéro vote. {0}, je dis rien, mais je le note." + Sticker.Sure.Bored,
        "Fin de « {1} » : personne a voté. {0}, c'était peut-être pas le bon jour." + Sticker.Sure.Sad,
        "J'ai fermé « {1} ». Aucune réponse. {0}, je suis désolée pour toi." + Sticker.Sure.Sad,
        "« {1} » : clos, et vide. {0}, on retentera." + Sticker.Sure.Bored,
        "Personne a voté sur « {1} ». {0}, je l'ai fermé quand même." + Sticker.Sure.Sad,
        "« {1} » s'est terminé dans le silence. {0}, c'est pas toi, c'est eux." + Sticker.Sure.Bored,
    };

    // « j'ai voté » under an anonymous poll: she won't say whether it's true. {0} the name.
    public static readonly string[] CardDoneAnonymousLines =
    {
        "{0}, ce sondage est anonyme. Je confirme rien, je démens rien." + Sticker.Smug,
        "Anonyme, {0}. Même si je savais, je dirais rien." + Sticker.Smug,
        "{0}, c'est un vote anonyme. Ton secret est bien gardé." + Sticker.Happy,
        "Je peux pas vérifier à voix haute, {0}. Anonyme, c'est anonyme." + Sticker.Sure.Smug,
        "{0}, sur ce sondage je vois rien et je dis rien." + Sticker.Smug,
        "Vote anonyme, {0}. Je te crois sur parole, c'est tout ce que je peux faire." + Sticker.Happy,
    };
}
