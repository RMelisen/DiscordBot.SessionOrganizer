namespace ProjectSYNCS.Services;

// Tracks whether the bot's owner has flagged themselves as absent. State is kept
// in memory and resets to "available" on restart, by design. While the owner is
// absent, ChatterService intercepts messages that ping them and replies, in a
// formal tone, that they are unavailable.
public sealed class AvailabilityService
{
    // Rodhengard, the owner. The single source of truth for this id.
    public const ulong OwnerId = 345917214966415362;

    // volatile: written from the slash-command path, read from gateway handlers.
    private volatile bool _ownerAbsent;

    public bool IsOwnerAbsent => _ownerAbsent;

    public void SetOwnerAbsent(bool absent) => _ownerAbsent = absent;
}
