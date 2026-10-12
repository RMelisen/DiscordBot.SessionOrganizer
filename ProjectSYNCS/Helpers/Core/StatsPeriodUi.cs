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
    /// <remarks>
    /// <para><b><paramref name="idPrefix"/> must not be the prefix the caller's paging
    /// buttons use.</b> Discord rejects a message whose components share a custom-id —
    /// with <c>COMPONENT_CUSTOM_ID_DUPLICATED</c>, and regardless of either being
    /// disabled — and the active filter's id here is exactly
    /// <c>{prefix}:{currentPeriod}:0</c>, which is also what a "◀" pointing at page 0
    /// would produce from the same prefix. Every caller therefore gives its filter row
    /// its own verb (<c>…:win:</c>) and keeps paging on another (<c>…:view:</c> /
    /// <c>…:page:</c>), with a handler for each.</para>
    /// <para>This is not hypothetical: it shipped, and it takes 2+ pages and one click
    /// on "▶" to hit.</para>
    /// </remarks>
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

    /// <summary>
    /// The same row for a Components V2 message, which builds its rows explicitly
    /// rather than by index. Same ids, same labels — <c>/leaderboard</c> passes
    /// <c>level:view:{view}</c> as the prefix, which is why its custom-id orders the
    /// view segment before the period one.
    /// </summary>
    public static ActionRowBuilder AddFilterRow(
        this ActionRowBuilder builder, string idPrefix, StatsPeriod active)
    {
        foreach (var period in All)
        {
            builder.WithButton(
                Label(period),
                $"{idPrefix}:{period}:0",
                period == active ? ButtonStyle.Primary : ButtonStyle.Secondary,
                disabled: period == active);
        }
        return builder;
    }
}
