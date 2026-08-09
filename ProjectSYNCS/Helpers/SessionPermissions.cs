using Discord;
using Discord.WebSocket;
using ProjectSYNCS.Models;
using ProjectSYNCS.Services;
using Poll = ProjectSYNCS.Models.Poll;

namespace ProjectSYNCS.Helpers;

public static class SessionPermissions
{
    /// <summary>
    /// The organizer, or any guild admin / manager, may cancel or edit a session.
    /// </summary>
    public static bool CanManage(IUser user, SessionEvent gameEvent) =>
        CanManage(user, gameEvent.OrganizerId);

    public static bool CanManage(IUser user, Poll poll) =>
        CanManage(user, poll.OrganizerId);

    public static bool CanManage(IUser user, Giveaway giveaway) =>
        CanManage(user, giveaway.OrganizerId);

    private static bool CanManage(IUser user, ulong organizerId) =>
        organizerId == user.Id || HasGuildPower(user);

    /// <summary>
    /// Guild staff — an Administrator or ManageGuild holder — plus the bot's owner,
    /// who outranks both and is staff everywhere regardless of his roles.
    /// </summary>
    /// <remarks>
    /// The third authorization model in this project, and deliberately its own thing:
    /// <see cref="CanManage(IUser, SessionEvent)"/> asks "is this yours, or are you
    /// staff", while the owner-only commands compare against
    /// <c>AvailabilityService.OwnerId</c> alone. This one is "are you staff" with no
    /// notion of ownership of the thing being acted on — which is what an XP
    /// adjustment needs, since nobody *owns* someone else's XP.
    /// </remarks>
    public static bool IsStaff(IUser user) =>
        user.Id == AvailabilityService.OwnerId || HasGuildPower(user);

    private static bool HasGuildPower(IUser user) =>
        user is SocketGuildUser guildUser
        && (guildUser.GuildPermissions.Administrator || guildUser.GuildPermissions.ManageGuild);
}
