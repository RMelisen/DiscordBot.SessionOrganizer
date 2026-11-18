namespace ProjectSYNCS.Helpers;

/// <summary>
/// The wall's categories a bribe can erase a hit from: the things you <em>did</em>. Ban votes are not
/// here on purpose: they were cast by staff, and buying them back would make them meaningless.
/// Not stored anywhere, so its order is free.
/// </summary>
public enum ShameCategory
{
    // The /bribe shame choices: the same names as Title, which the replies use.
    [Discord.Interactions.ChoiceDisplay("Le Malfaisant")] Mean,
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
        ShameCategory.Perfidy => "Le Perfide",
        ShameCategory.Shout => "L'Hystérique",
        ShameCategory.Abandon => "L'Indigne",
        _ => throw new ArgumentOutOfRangeException(nameof(category)),
    };
}
