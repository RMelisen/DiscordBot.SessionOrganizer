using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

/// <summary>
/// One-off arrangements for the Plynlings' launch on the production server, requested by the
/// owner: a few people's *first* Plynling is chosen rather than rolled, and one pair of owners'
/// Plynlings always has the best possible compatibility. Literal snowflakes tied to one server,
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

    // The pair whose Plynlings have the best possible compatibility. Their affinity starts at 0 like
    // anyone's; the compatibility only tilts every visit their way. Keyed on the owners, so it holds
    // for any Plynlings they have later too.
    public static ulong? SoulmateOf(ulong guildId, ulong ownerId) =>
        guildId != ProdGuildId ? null
        : ownerId == CoprinOwner ? GirolleOwner
        : ownerId == GirolleOwner ? CoprinOwner
        : null;

    public static bool AreSoulmates(Plynling a, Plynling b) =>
        a.GuildId == b.GuildId && SoulmateOf(a.GuildId, a.OwnerId) == b.OwnerId;

    /// <summary><see cref="PlynlingBonds.Compatibility"/>, except the soulmates, who get the maximum.</summary>
    public static int Compatibility(Plynling a, Plynling b) =>
        AreSoulmates(a, b) ? 20 : PlynlingBonds.Compatibility(a.Id, b.Id);
}
