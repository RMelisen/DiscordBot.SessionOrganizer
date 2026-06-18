using Discord;
using Discord.WebSocket;
using ProjectSYNCS.Models;
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

    private static bool CanManage(IUser user, ulong organizerId)
    {
        if (organizerId == user.Id)
            return true;

        return user is SocketGuildUser guildUser &&
            (guildUser.GuildPermissions.Administrator || guildUser.GuildPermissions.ManageGuild);
    }
}
