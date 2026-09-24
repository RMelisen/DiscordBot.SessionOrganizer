namespace ProjectSYNCS.Helpers;

// Fixed French lines for refusals and plain notices. Deliberately not ResponsePicker
// pools: the pools exist so repeated *chatter* doesn't repeat, and a refusal is not
// chatter — varied, it would read as scripted.
public static class PlynlingText
{
    public static string WorkCooldown(DateTimeOffset next) =>
        $"Tu as déjà travaillé. Prochain service <t:{next.ToUnixTimeSeconds()}:R>.";

    public const string NoPlynling = "Tu n'as pas de Plynling. `/plynling adopt` pour en adopter un !";
    public const string NotYours = "Ce n'est pas ton Plynling — seul son propriétaire peut le nourrir.";
    public const string Dead = "Ce Plynling n'est plus de ce monde… 🪦";
    public const string Frozen = "Ce Plynling est gelé : rien ne bouge tant qu'il n'est pas dégelé.";
    public const string Wasted = "Il n'a besoin de rien de tout ça pour l'instant — garde tes cailloux.";
    public const string PetCooldown = "Tu l'as caressé il y a peu. Reviens dans quelques heures.";
    public const string AlreadyHasOne = "Tu as déjà un Plynling. Un seul à la fois !";
    public const string EmptyName = "Il lui faut un vrai nom.";
    public const string Unknown = "Ce bouton ne correspond plus à rien.";

    public static string NoneFor(ulong userId) => $"<@{userId}> n'a pas de Plynling.";

    public static string TooPoor(long price, long balance) =>
        $"Il te faut {PebbleEconomy.Cailloux(price)}, tu n'en as que {balance}. `/work` pour en gagner.";

    public const string StaffOnly = "Cette action est réservée au staff.";
    public const string AlreadyFrozen = "Il est déjà gelé.";
    public const string NotFrozen = "Il n'est pas gelé.";
    public const string TooHungryToFreeze = "Trop tard pour le geler : il a déjà trop faim (moins de 50 %). Nourris-le d'abord.";
    public const string ThawStaffOnly = "C'est le staff qui l'a gelé : seul le staff peut le dégeler.";
    public const string NoGrave = "Personne à ressusciter : cette personne n'a aucun Plynling au cimetière.";
    public const string ResurrectBlocked = "Cette personne a déjà un Plynling vivant — un seul à la fois.";

    public static string FreezeCooldown(DateTimeOffset next) =>
        $"Tu l'as dégelé il y a moins de 7 jours. Prochain gel possible <t:{next.ToUnixTimeSeconds()}:R>.";

    // "…Notice", not "Frozen"/"Thawed": those names are already refusal constants.
    public static string FrozenNotice(string name, DateTimeOffset? until) => until is { } u
        ? $"❄️ **{name}** est gelé jusqu'au <t:{u.ToUnixTimeSeconds()}:f>. Rien ne bouge d'ici là."
        : $"❄️ **{name}** est gelé jusqu'à nouvel ordre du staff.";

    public static string ThawedNotice(string name) => $"🌱 **{name}** est dégelé. La faim reprend son cours !";
}
