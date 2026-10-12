using Discord;

namespace ProjectSYNCS.Helpers;

// Turns the markup stored in the `*Reactions` pools into something Discord can
// actually react with. Custom emotes arrive as `<:name:id>`; everything else is a
// literal unicode emoji.
//
// Shared rather than duplicated because of the trap below: a custom emote written
// *without* its snowflake id (`<:name:>`) fails `Emote.TryParse` and falls through
// to `Emoji`, producing an "emoji" whose name is the literal markup string. It is
// non-null, so a caller's null guard passes, and Discord then rejects the reaction
// with a 400 that gets swallowed and logged. Two copies of this would be two places
// for that to be re-learned.
public static class EmoteMarkup
{
    public static IEmote? Parse(string markup)
    {
        if (string.IsNullOrEmpty(markup)) return null;
        return Emote.TryParse(markup, out var custom) ? custom : new Emoji(markup);
    }
}
