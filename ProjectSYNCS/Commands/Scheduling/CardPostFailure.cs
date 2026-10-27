using Discord;
using Discord.WebSocket;

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
            var perms = me.GetPermissions(guildChannel);
            var missing = new List<string>();
            if (!perms.ViewChannel) missing.Add("« Voir le salon »");
            if (!perms.SendMessages) missing.Add("« Envoyer des messages »");
            if (!perms.EmbedLinks) missing.Add("« Intégrer des liens »");
            if (missing.Count > 0)
                return $"Je n'ai pas pu poster {what} ici : il me manque la permission {string.Join(" et ", missing)} dans ce salon. "
                    + "Rien n'a été créé. Demande à un admin, ou relance la commande dans un autre salon.";
        }
        return $"Je n'ai pas pu poster {what} ici, et rien n'a été créé. Réessaie dans un instant — le détail est dans mes logs.";
    }
}
