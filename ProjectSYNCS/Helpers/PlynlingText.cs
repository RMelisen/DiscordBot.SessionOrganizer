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
}
