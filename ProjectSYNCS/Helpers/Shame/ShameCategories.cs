namespace ProjectSYNCS.Helpers;

/// <summary>
/// The wall's titles, every one of which a bribe can erase a hit from: the four things you
/// <em>did</em> and the staff's ban votes (Le Banni, the owner's call: everything here has a price).
/// Not stored anywhere, so its order is free; it follows the wall's.
/// </summary>
public enum ShameCategory
{
    // The /bribe shame choices: the same names as Title, which the replies use.
    [Discord.Interactions.ChoiceDisplay("Le Malfaisant")] Mean,
    [Discord.Interactions.ChoiceDisplay("Le Banni")] Ban,
    [Discord.Interactions.ChoiceDisplay("Le Perfide")] Perfidy,
    [Discord.Interactions.ChoiceDisplay("L'Hystérique")] Shout,
    [Discord.Interactions.ChoiceDisplay("L'Indigne")] Abandon,
}

public static class ShameCategories
{
    /// <summary>The title each category gives on the wall (as ShameModule shows it).</summary>
    public static string Title(ShameCategory category) => category switch
    {
        ShameCategory.Mean => "Le Malfaisant",
        ShameCategory.Ban => "Le Banni",
        ShameCategory.Perfidy => "Le Perfide",
        ShameCategory.Shout => "L'Hystérique",
        ShameCategory.Abandon => "L'Indigne",
        _ => throw new ArgumentOutOfRangeException(nameof(category)),
    };
}
