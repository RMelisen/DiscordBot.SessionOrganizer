using Discord;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

/// <summary>
/// The one table of what a session category looks like: its emoji and its French name. The card,
/// the native event, the reminder DM, the lists, the reports and both category pickers read it,
/// so a new category is one line here.
/// </summary>
public static class SessionCategories
{
    private static readonly (SessionCategory Category, string Emoji, string Name)[] Table =
    {
        (SessionCategory.Game, "🎮", "Jeu"),
        (SessionCategory.Activity, "🧑‍🤝‍🧑", "Activité"),
        (SessionCategory.Movie, "🎬", "Film"),
        (SessionCategory.Other, "✨", "Autre"),
    };

    public static IEnumerable<SessionCategory> All => Table.Select(t => t.Category);

    public static string Emoji(SessionCategory category) => Row(category).Emoji;

    public static string Name(SessionCategory category) => Row(category).Name;

    /// <summary>« 🎮 Jeu ».</summary>
    public static string Label(SessionCategory category) => $"{Emoji(category)} {Name(category)}";

    /// <summary>The category select of both wizards (/schedule create, a poll turned into a session).</summary>
    public static SelectMenuBuilder Select(string customId)
    {
        var menu = new SelectMenuBuilder()
            .WithCustomId(customId)
            .WithPlaceholder("Choisis une catégorie");
        foreach (var (category, emoji, name) in Table)
            menu.AddOption(name, category.ToString(), emote: new Emoji(emoji));
        return menu;
    }

    // An unknown value (a future enum member read by old code) reads as « Autre ».
    private static (SessionCategory Category, string Emoji, string Name) Row(SessionCategory category)
    {
        foreach (var row in Table)
            if (row.Category == category) return row;
        return Table[^1];
    }
}
