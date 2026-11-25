using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Commands;

// /config setup: the guided setup (ConfigSetupCards, handled by ConfigSetupComponentHandler). Part of
// /config: the class comment and the shared helpers are in ConfigModule.cs.
public partial class ConfigModule
{
    [SlashCommand("setup", "Mettre le bot en place sur ce serveur, étape par étape")]
    public async Task SetupAsync()
    {
        if (!await BeginAsync(Context)) return;

        var config = await _config.GetAsync(Context.Guild.Id);
        await FollowupAsync(
            components: ConfigSetupCards.Build(ConfigSetupCards.Order[0], SetupViewOf(Context.Guild, config)),
            flags: MessageFlags.ComponentsV2,
            ephemeral: true,
            allowedMentions: AllowedMentions.None);
    }

    /// <summary>What the setup pages show of this server.</summary>
    internal static SetupView SetupViewOf(SocketGuild guild, GuildConfig config)
    {
        var channels = ServerHealthProbe.ChannelsOf(guild, config)
            .Where(c => c.ChannelId != 0 && guild.GetChannel(c.ChannelId) is not null)
            .ToDictionary(c => c.Slot, c => c.ChannelId);
        ulong Existing(ulong roleId) => roleId != 0 && guild.GetRole(roleId) is not null ? roleId : 0;
        var (health, unset) = ServerHealthProbe.Check(guild, config);
        var features = ServerFeatures.All.ToDictionary(f => f.Key, f => config.IsOn(f.Key, guild.Id));
        return new SetupView(channels, Existing(config.ModeratorRoleId), Existing(config.PlynlingRoleId),
            Existing(config.QuizRoleId), health, unset, features);
    }
}
