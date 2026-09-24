using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// The in-memory Plynling cooldowns — a singleton, like every gate here. Only petting is
// rationed this way: a restart lets people pet once more early, which costs nothing,
// since petting can never keep a Plynling alive. Anything that touches money or death
// (/work, self-freeze) is stored in the database instead.
public sealed class PlynlingCooldowns
{
    public CooldownGate<(ulong Petter, int PlynlingId)> Pet { get; } =
        new(PlynlingLife.PetCooldown, forget: TimeSpan.FromHours(8));
}
