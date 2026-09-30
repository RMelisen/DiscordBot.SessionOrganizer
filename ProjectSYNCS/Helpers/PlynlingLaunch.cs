using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

/// <summary>
/// One-off arrangements for the Plynlings' launch on the production server, requested by the
/// owner: a few people's *first* Plynling is chosen rather than rolled, and one pair of those first
/// Plynlings gets the best possible compatibility. Literal snowflakes tied to one server,
/// like the other hardcoded ids.
/// </summary>
public static class PlynlingLaunch
{
    public const ulong ProdGuildId = 878305033995825164;

    private const ulong CoprinOwner = 345917214966415362;
    private const ulong GirolleOwner = 324768221372743681;

    // Applied only to someone's first Plynling on the production server: while they have no row
    // there and no abandonment on record (PlynlingService.AdoptAsync checks both).
    private static readonly Dictionary<ulong, (PlynlingSpecies Species, PlynlingGender Gender)> FirstAdoptions = new()
    {
        [CoprinOwner] = (PlynlingSpecies.Coprin, PlynlingGender.Male),
        [GirolleOwner] = (PlynlingSpecies.Dore, PlynlingGender.Female),
    };

    public static (PlynlingSpecies Species, PlynlingGender Gender)? FirstAdoption(ulong guildId, ulong ownerId) =>
        guildId == ProdGuildId && FirstAdoptions.TryGetValue(ownerId, out var pick) ? pick : null;

    // The pair whose first Plynlings are made for each other. Compatibility is derived from the two
    // Plynling ids and never stored (PlynlingBonds.Compatibility), so nothing is special-cased at
    // visit time: whichever of the two adopts second is simply given an id that hashes to the
    // maximum with the other's. Their affinity starts at 0 like anyone's.
    public static ulong? SoulmateOf(ulong guildId, ulong ownerId) =>
        guildId != ProdGuildId ? null
        : ownerId == CoprinOwner ? GirolleOwner
        : ownerId == GirolleOwner ? CoprinOwner
        : null;

    /// <summary>
    /// The first id at or after <paramref name="from"/> whose compatibility with
    /// <paramref name="mateId"/> is the maximum. About one id in 41 qualifies, so the search is
    /// short; null only if the bound is somehow reached, and the id is then generated as usual.
    /// </summary>
    public static int? MatchingId(int mateId, int from)
    {
        for (var id = Math.Max(from, 1); id < from + 10_000; id++)
            if (id != mateId && PlynlingBonds.Compatibility(mateId, id) == PlynlingBonds.MaxCompatibility)
                return id;
        return null;
    }
}
