using Discord;
using Discord.WebSocket;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Commands;

/// <summary>
/// What to tell someone whose session, poll or vote card couldn't be posted. The row has already
/// been removed (no orphans); the usual cause is a permission she lacks in that channel, so it
/// names the one missing.
/// </summary>
public static class CardPostFailure
{
    public static string Explain(IGuild? guild, IChannel channel, string what)
    {
        var me = (guild as SocketGuild)?.CurrentUser;
        if (me is not null && channel is IGuildChannel guildChannel)
        {
            // The French names live in ServerHealth, so the health check and this refusal agree.
            var missing = ServerHealth.Missing(ChannelNeed.View | ChannelNeed.Send | ChannelNeed.Embed,
                ServerHealthProbe.Have(me.GetPermissions(guildChannel)));
            if (missing.Count > 0)
                return $"Je n'ai pas pu poster {what} ici : il me manque la permission {ServerHealth.JoinNames(missing)} dans ce salon. "
                    + "Rien n'a été créé. Demande à un admin, ou relance la commande dans un autre salon.";
        }
        return $"Je n'ai pas pu poster {what} ici, et rien n'a été créé. Réessaie dans un instant — le détail est dans mes logs.";
    }
}
