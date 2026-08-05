using Discord;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Helpers;

/// <summary>
/// The filter row every ranking command shows, in one place.
/// </summary>
/// <remarks>
/// <c>/emotestats</c> and <c>/goodbot</c> offer the same three windows, and two
/// commands labelling the same filter differently would read as a bug. The labels are
/// user-facing French; the custom-id prefix is what differs between callers.
/// </remarks>
public static class StatsPeriodUi
{
    /// <summary>The three windows, in the order they are shown.</summary>
    public static readonly StatsPeriod[] All =
        { StatsPeriod.Month, StatsPeriod.Week, StatsPeriod.AllTime };

    public static string Label(StatsPeriod period) => period switch
    {
        StatsPeriod.Month => "30 jours",
        StatsPeriod.Week => "7 jours",
        _ => "Depuis toujours",
    };

    /// <summary>
    /// Adds the filter buttons on <paramref name="row"/>. Ids are built as
    /// <c>{idPrefix}:{period}:0</c> — changing the window always resets to page 0,
    /// since the ranking is a different list and page 4 of the old one means nothing.
    /// The active filter is highlighted and disabled, so it reads as the current view
    /// rather than an available action.
    /// </summary>
    public static ComponentBuilder AddFilterRow(
        this ComponentBuilder builder, string idPrefix, StatsPeriod active, int row = 0)
    {
        foreach (var period in All)
        {
            builder.WithButton(
                Label(period),
                $"{idPrefix}:{period}:0",
                period == active ? ButtonStyle.Primary : ButtonStyle.Secondary,
                disabled: period == active,
                row: row);
        }
        return builder;
    }
}
