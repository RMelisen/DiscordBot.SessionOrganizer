using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

// Fixed French lines for refusals and plain notices. Deliberately not ResponsePicker
// pools: the pools exist so repeated *chatter* doesn't repeat, and a refusal is not
// chatter — varied, it would read as scripted.
//
// A line about one specific Plynling takes its gender ("ta Plynling", "gelée"); a line
// about the *person* — they have none, they already have one — stays in the generic
// masculine, since no particular Plynling is meant.
public static class PlynlingText
{
    public static string WorkCooldown(DateTimeOffset next) =>
        $"Tu as déjà travaillé. Prochain service <t:{next.ToUnixTimeSeconds()}:R>.";

    public const string NoPlynling = "Tu n'as pas de Plynling. `/plynling adopt` pour en adopter un !";
    public const string AlreadyHasOne = "Tu as déjà un Plynling. Un seul à la fois !";
    public const string EmptyName = "Il lui faut un vrai nom.";
    public const string Unknown = "Ce bouton ne correspond plus à rien.";
    public const string StaffOnly = "Cette action est réservée au staff.";
    public const string NoGrave = "Personne à ressusciter : cette personne n'a aucun Plynling au cimetière.";
    public const string ResurrectBlocked = "Cette personne a déjà un Plynling vivant — un seul à la fois.";
    // Refused before the Plynling is even loaded, so it cannot know the gender: worded to
    // need none.
    public const string PetCooldown = "Une caresse toutes les 4 heures, pas plus. Reviens un peu plus tard.";

    public static string NoneFor(ulong userId) => $"<@{userId}> n'a pas de Plynling.";

    public static string TooPoor(long price, long balance) =>
        $"Il te faut {PebbleEconomy.Cailloux(price)}, tu n'en as que {balance}. `/work` pour en gagner.";

    // "seul son propriétaire": the owner's gender is not known, so that part stays generic.
    public static string NotYours(PlynlingGender g) =>
        $"Ce n'est pas {g.Agree("ton", "ta")} Plynling — seul son propriétaire peut {g.Agree("le", "la")} nourrir.";

    public static string Dead(PlynlingGender g) => $"{g.Agree("Ce", "Cette")} Plynling n'est plus de ce monde… 🪦";

    public static string Frozen(PlynlingGender g) => g.Agree(
        "Ce Plynling est gelé : rien ne bouge tant qu'il n'est pas dégelé.",
        "Cette Plynling est gelée : rien ne bouge tant qu'elle n'est pas dégelée.");

    public static string Wasted(PlynlingGender g) =>
        $"{g.Agree("Il", "Elle")} n'a besoin de rien de tout ça pour l'instant — garde tes cailloux.";

    public static string AlreadyFrozen(PlynlingGender g) => g.Agree("Il est déjà gelé.", "Elle est déjà gelée.");

    public static string NotFrozen(PlynlingGender g) => g.Agree("Il n'est pas gelé.", "Elle n'est pas gelée.");

    public static string TooHungryToFreeze(PlynlingGender g) => g.Agree(
        "Trop tard pour le geler : il a déjà trop faim (moins de 50 %). Nourris-le d'abord.",
        "Trop tard pour la geler : elle a déjà trop faim (moins de 50 %). Nourris-la d'abord.");

    public static string ThawStaffOnly(PlynlingGender g) => g.Agree(
        "C'est le staff qui l'a gelé : seul le staff peut le dégeler.",
        "C'est le staff qui l'a gelée : seul le staff peut la dégeler.");

    public static string FreezeCooldown(PlynlingGender g, DateTimeOffset next) =>
        $"Tu l'as {g.Agree("dégelé", "dégelée")} il y a moins de 7 jours. Prochain gel possible <t:{next.ToUnixTimeSeconds()}:R>.";

    // "…Notice", not "Frozen"/"Thawed": those names are the refusals above.
    public static string FrozenNotice(PlynlingGender g, string name, DateTimeOffset? until) => until is { } u
        ? $"❄️ **{name}** est {g.Agree("gelé", "gelée")} jusqu'au <t:{u.ToUnixTimeSeconds()}:f>. Rien ne bouge d'ici là."
        : $"❄️ **{name}** est {g.Agree("gelé", "gelée")} jusqu'à nouvel ordre du staff.";

    public static string ThawedNotice(PlynlingGender g, string name) =>
        $"🌱 **{name}** est {g.Agree("dégelé", "dégelée")}. La faim reprend son cours !";

    // Appended to the pet line on the card: "— caressée par @quelqu'un".
    public static string PettedBy(PlynlingGender g, ulong petterId) => $"{g.Agree("caressé", "caressée")} par <@{petterId}>";
}
