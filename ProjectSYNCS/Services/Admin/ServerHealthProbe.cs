using Discord;
using Discord.WebSocket;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// Reads a server from the gateway into ServerHealth's plain facts: which channel each slot uses
// here (configured, else the default when it lives on this server), whether it still exists, and
// what she may do in it. Nothing is written and nothing is sent; the judging is ServerHealth's.
public static class ServerHealthProbe
{
    /// <summary>The effective channel of every slot on this server (0 when none).</summary>
    public static IReadOnlyList<(HealthSlot Slot, ulong ChannelId)> ChannelsOf(SocketGuild guild, GuildConfig config)
    {
        ulong Default(ulong configured, ulong fallback) =>
            configured != 0 ? configured : guild.GetChannel(fallback) is not null ? fallback : 0;

        return new[]
        {
            (HealthSlot.Game, Default(config.GameChannelId, PlynlingAnnouncer.DefaultGameChannelId)),
            (HealthSlot.Bots, Default(config.BotsChannelId, BotsChannel.DefaultId)),
            (HealthSlot.LevelUp, config.LevelUpChannelId),
            (HealthSlot.Quiz, config.QuizChannelId),
            (HealthSlot.Confess, config.ConfessChannelId),
            (HealthSlot.FreeGames, config.FreeGamesChannelId),
            (HealthSlot.Main, MorningGreetingService.MainChannelOf(config, guild.Id)),
        };
    }

    /// <summary>The health lines, and what is not set yet, for /config show and the setup summary.</summary>
    public static (IReadOnlyList<HealthLine> Lines, IReadOnlyList<string> Unset) Check(SocketGuild guild, GuildConfig config)
    {
        var me = guild.CurrentUser;
        var slots = ChannelsOf(guild, config)
            .Select(c => guild.GetChannel(c.ChannelId) is { } channel
                ? new SlotFacts(c.Slot, c.ChannelId, true, me is null ? ChannelNeed.None : Have(me.GetPermissions(channel)))
                : new SlotFacts(c.Slot, c.ChannelId, false, ChannelNeed.None))
            .ToList();

        var facts = new GuildFacts(
            ManageEvents: me?.GuildPermissions.ManageEvents == true,
            PlynlingRoleSet: config.PlynlingRoleId != 0,
            PlynlingRoleProblem: config.PlynlingRoleId == 0 ? null
                : RoleMirror.Problem(guild, guild.GetRole(config.PlynlingRoleId), adopting: false, RoleKind.Plynling),
            QuizRoleSet: config.QuizRoleId != 0,
            QuizRoleProblem: config.QuizRoleId == 0 ? null
                : RoleMirror.Problem(guild, guild.GetRole(config.QuizRoleId), adopting: false, RoleKind.QuizCrown));

        return (ServerHealth.Judge(slots, facts), ServerHealth.Unset(slots, config.ModeratorRoleId != 0));
    }

    /// <summary>Her permissions in a channel, as the flags ServerHealth reads.</summary>
    public static ChannelNeed Have(ChannelPermissions perms)
    {
        var have = ChannelNeed.None;
        if (perms.ViewChannel) have |= ChannelNeed.View;
        if (perms.SendMessages) have |= ChannelNeed.Send;
        if (perms.EmbedLinks) have |= ChannelNeed.Embed;
        if (perms.AttachFiles) have |= ChannelNeed.Attach;
        if (perms.AddReactions) have |= ChannelNeed.React;
        if (perms.UseExternalEmojis) have |= ChannelNeed.ExternalEmojis;
        if (perms.ReadMessageHistory) have |= ChannelNeed.History;
        return have;
    }
}
