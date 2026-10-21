namespace ProjectSYNCS.Models;

// A Discord member and the in-game name they proved is theirs (/minecraft link: a code whispered to her in game). One
// name per member, one member per name: PlayerKey, the name in lower case, is unique, since Minecraft names ignore case.
public class MinecraftLink
{
    public ulong UserId { get; set; }

    // As they typed it, for display.
    public string Player { get; set; } = string.Empty;
    public string PlayerKey { get; set; } = string.Empty;

    public DateTimeOffset LinkedAt { get; set; } = DateTimeOffset.UtcNow;
}
