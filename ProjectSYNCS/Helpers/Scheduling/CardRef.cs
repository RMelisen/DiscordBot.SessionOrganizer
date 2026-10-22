namespace ProjectSYNCS.Helpers;

// The four cards people answer with a button. In memory only, never stored: free to reorder.
public enum CardKind
{
    // /poll and /vote alike: both render through PollModule's builders.
    Poll,
    Session,
    Tonight,
    Giveaway,
}

/// <summary>Which card a message is, and the id of the row behind it.</summary>
public readonly record struct CardRef(CardKind Kind, int Id);

/// <summary>
/// Recognises one of her cards from its buttons' custom-ids, pure. The custom-ids carry the row's
/// id, so a reply to a card needs no lookup table and survives a restart. A card that has lost its
/// buttons (a drawn giveaway, a cancelled or started session, an expired /tonight) is not
/// recognised: there is nothing left to click, and a reply to it reads like any other.
/// </summary>
/// <remarks>The prefixes are a contract with the card builders (root CLAUDE.md, custom-ids):
/// `poll:vote|close|tosession:{id}`, `event:*:{id}`, `tonight:*:{id}`, `giveaway:enter|leave:{id}`.</remarks>
public static class CardRefs
{
    public static CardRef? Read(IEnumerable<string?> customIds)
    {
        foreach (var customId in customIds)
        {
            if (string.IsNullOrEmpty(customId)) continue;
            var parts = customId.Split(':');
            if (parts.Length < 3 || !int.TryParse(parts[2], out var id)) continue;

            CardKind? kind = (parts[0], parts[1]) switch
            {
                ("poll", "vote" or "close" or "tosession") => CardKind.Poll,
                ("event", _) => CardKind.Session,
                ("tonight", _) => CardKind.Tonight,
                ("giveaway", "enter" or "leave") => CardKind.Giveaway,
                _ => null,
            };
            if (kind is { } k) return new CardRef(k, id);
        }

        return null;
    }
}
