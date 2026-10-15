using Discord;
using Discord.WebSocket;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Helpers;

/// <summary>
/// Where she posts what she announces unprompted that doesn't matter much (Quokka's visits). A
/// reply always goes where she was used; this is only for her own initiatives.
/// </summary>
/// <remarks>
/// A <i>default</i>, like the game channel: `/config bots-channel set` replaces it, `clear` falls
/// back. The default is used only when it belongs to the guild; otherwise, the game channel.
/// Server-specific id, listed in CLAUDE.md's "Hardcoded ids".
/// </remarks>
public static class BotsChannel
{
    public const ulong DefaultId = 901172450719584356; // « spam-bots »

    public static async Task<IMessageChannel?> ResolveAsync(DiscordSocketClient client, GuildConfigService config,
        PlynlingAnnouncer announcer, ulong guildId)
    {
        var configured = (await config.GetAsync(guildId)).BotsChannelId;
        var id = configured != 0 ? configured : DefaultId;
        if (client.GetChannel(id) is IMessageChannel channel && channel is IGuildChannel home && home.GuildId == guildId)
            return channel;
        return await announcer.ResolveGameChannelAsync(guildId);
    }
}
