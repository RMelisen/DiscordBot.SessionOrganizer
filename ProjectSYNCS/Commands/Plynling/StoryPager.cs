using Discord;

namespace ProjectSYNCS.Commands;

// The arrows under a paged story — a visit's or an event's: ◀, ▶ and « ⏭ Fin », which skips to the
// last page (the outcome). On the last page ▶ has nowhere to go, so it becomes « ↺ Début » and
// « Fin » goes. Each button has its own verb (prev, next, first, last), so a card never carries the
// same custom-id twice; `id` builds the full custom-id from a verb.
public static class StoryPager
{
    public static ActionRowBuilder Row(int page, int count, Func<string, string> id)
    {
        var row = new ActionRowBuilder()
            .WithButton("◀", id("prev"), ButtonStyle.Secondary, disabled: page == 0);
        return page == count - 1
            ? row.WithButton("↺ Début", id("first"), ButtonStyle.Secondary)
            : row.WithButton("▶", id("next"), ButtonStyle.Secondary)
                .WithButton("⏭ Fin", id("last"), ButtonStyle.Secondary);
    }
}
