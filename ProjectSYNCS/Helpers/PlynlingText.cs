namespace ProjectSYNCS.Helpers;

// Fixed French lines for refusals and plain notices. Deliberately not ResponsePicker
// pools: the pools exist so repeated *chatter* doesn't repeat, and a refusal is not
// chatter — varied, it would read as scripted.
public static class PlynlingText
{
    public static string WorkCooldown(DateTimeOffset next) =>
        $"Tu as déjà travaillé. Prochain service <t:{next.ToUnixTimeSeconds()}:R>.";
}
