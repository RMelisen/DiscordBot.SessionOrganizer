namespace ProjectSYNCS.Helpers;

// A select menu of any length on one select: up to Discord's 25 options it is a single page;
// past that, pages of PerPage options with a « previous » and a « next » option around them.
// One select whatever the count, so a screen's component budget never grows with a catalogue.
// Pure arithmetic; the caller builds the options and the page-turn values.
public static class SelectPages
{
    public const int Max = 25;

    // Room left on a paged select once both page-turn options are in.
    public const int PerPage = Max - 2;

    public static int Count(int items) => items <= Max ? 1 : (items + PerPage - 1) / PerPage;

    public static int Clamp(int page, int items) => Math.Clamp(page, 0, Count(items) - 1);

    // The page holding the item at index (a default option); the first when there is none.
    public static int PageOf(int index, int items) => items <= Max || index < 0 ? 0 : index / PerPage;

    public static (int Start, int Length) Range(int page, int items)
    {
        if (items <= Max) return (0, items);
        page = Clamp(page, items);
        var start = page * PerPage;
        return (start, Math.Min(PerPage, items - start));
    }
}
