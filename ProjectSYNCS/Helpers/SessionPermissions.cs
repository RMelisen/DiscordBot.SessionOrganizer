using Discord;
using Discord.WebSocket;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

public static class SessionPermissions
{
    /// <summary>
    /// The organizer, or any guild admin / manager, may cancel or edit a session.
    /// </summary>
    public static bool CanManage(IUser user, SessionEvent gameEvent)
    {
        if (gameEvent.OrganizerId == user.Id)
            return true;

        return user is SocketGuildUser guildUser &&
            (guildUser.GuildPermissions.Administrator || guildUser.GuildPermissions.ManageGuild);
    }
}
