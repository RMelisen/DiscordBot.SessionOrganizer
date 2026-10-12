namespace ProjectSYNCS.Helpers;

// What a home-guild voice channel is for, so the spectator line (VoiceSpectatorService) fits
// what she just watched. Server-specific ids, listed in CLAUDE.md's "Hardcoded ids". A channel
// that isn't listed counts as General, the pool written for any get-together.
public enum VoiceRoom
{
    General,
    Gaming,
    Cinema,
    Study,
}

public static class VoiceRooms
{
    private static readonly Dictionary<ulong, VoiceRoom> Rooms = new()
    {
        [878305034432045083] = VoiceRoom.General,  // Général
        [1535968645140848670] = VoiceRoom.Gaming,  // Gaming
        [1481782119243190375] = VoiceRoom.Gaming,  // Gaming 2
        [878307678374490204] = VoiceRoom.Cinema,   // Cinéma
        [933852203612012605] = VoiceRoom.Study,    // Étude/Travail
    };

    public static VoiceRoom For(ulong channelId) =>
        Rooms.TryGetValue(channelId, out var room) ? room : VoiceRoom.General;

    /// <summary>
    /// Where she waits for a session nobody has joined yet (VoiceSeatService, "first one there"):
    /// Cinéma for a film, the first Gaming channel for anything else.
    /// </summary>
    public static ulong DefaultFor(ProjectSYNCS.Models.SessionCategory category) =>
        category == ProjectSYNCS.Models.SessionCategory.Movie ? CinemaDefault : GamingDefault;

    private const ulong GamingDefault = 1535968645140848670; // Gaming
    private const ulong CinemaDefault = 878307678374490204;  // Cinéma
}
