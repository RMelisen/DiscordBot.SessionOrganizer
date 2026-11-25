using Discord;
using Discord.Interactions;
using Discord.Net;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Commands;

// The lists, additive to the code: channels without XP, /shame voters, idle channels. Part of /config: the class comment and the shared helpers are in ConfigModule.cs.
public partial class ConfigModule
{
    [Group("excluded-channels", "Les salons où rien ne compte (ni XP, ni mur de la honte)")]
    public class ExcludedChannelsModule : SyncsModuleBase
    {
        private readonly GuildConfigService _config;

        public ExcludedChannelsModule(GuildConfigService config)
        {
            _config = config;
        }

        [SlashCommand("add", "Exclure un salon : plus d'XP, et il ne compte plus pour le mur")]
        public async Task AddAsync(
            [Summary("channel", "Le salon à exclure")] IGuildChannel channel)
        {
            if (!await BeginAsync(Context)) return;

            // Refused rather than silently stored: writing it would create a second,
            // redundant source of truth for a channel the code already excludes, and
            // the row could then be "removed" without changing anything.
            if (XpTracker.HardcodedExcludedChannels.Contains(channel.Id))
            {
                await SayAsync(Context,
                    $"<#{channel.Id}> est déjà exclu par défaut, dans le code. Rien à faire {Emotes.Sparkle}");
                return;
            }

            var added = await _config.AddExcludedChannelAsync(Context.Guild.Id, channel.Id);

            await SayAsync(Context, added
                ? $"<#{channel.Id}> est maintenant exclu. Plus d'XP, et il ne compte plus pour le mur ദ്ദി◝ ⩊ ◜.ᐟ"
                : $"<#{channel.Id}> était déjà dans la liste (ᵕ • ᴗ •)");
        }

        [SlashCommand("remove", "Réinclure un salon ajouté ici")]
        public async Task RemoveAsync(
            [Summary("channel", "Le salon à réinclure")] IGuildChannel channel)
        {
            if (!await BeginAsync(Context)) return;

            // The hardcoded list is a floor, not a default that can be edited away.
            // Said plainly here so it does not read as the command silently failing.
            if (XpTracker.HardcodedExcludedChannels.Contains(channel.Id))
            {
                await SayAsync(Context,
                    $"<#{channel.Id}> est exclu par défaut dans le code : je ne peux pas le réinclure d'ici {Emotes.Staring}");
                return;
            }

            var removed = await _config.RemoveExcludedChannelAsync(Context.Guild.Id, channel.Id);

            await SayAsync(Context, removed
                ? $"<#{channel.Id}> compte de nouveau. L'XP y est à nouveau gagnable {Emotes.Sparkle}"
                : $"<#{channel.Id}> n'était pas dans la liste (ᵕ • ᴗ •)");
        }
    }

    [Group("shame-voters", "Les personnes autorisées à voter avec /shame")]
    public class ShameVotersModule : SyncsModuleBase
    {
        private readonly GuildConfigService _config;

        public ShameVotersModule(GuildConfigService config)
        {
            _config = config;
        }

        [SlashCommand("add", "Autoriser quelqu'un à voter avec /shame")]
        public async Task AddAsync(
            [Summary("user", "La personne à qui confier le vote")] IUser user)
        {
            if (!await BeginAsync(Context)) return;

            if (user.IsBot)
            {
                await SayAsync(Context, "Un bot qui vote ? Le seul bot qui juge ici, c'est moi (ᵕ • ᴗ •)");
                return;
            }

            if (ShameModule.HardcodedExtraVotersFor(Context.Guild.Id).Contains(user.Id))
            {
                await SayAsync(Context,
                    $"<@{user.Id}> vote déjà, c'est écrit dans mon code. Rien à faire {Emotes.Sparkle}");
                return;
            }

            var added = await _config.AddShameVoterAsync(Context.Guild.Id, user.Id);

            await SayAsync(Context, added
                ? $"<@{user.Id}> peut maintenant voter avec `/shame`. Je garde un œil sur ses votes, évidemment ദ്ദി◝ ⩊ ◜.ᐟ"
                : $"<@{user.Id}> était déjà dans la liste (ᵕ • ᴗ •)");
        }

        [SlashCommand("remove", "Retirer le vote /shame à quelqu'un ajouté ici")]
        public async Task RemoveAsync(
            [Summary("user", "La personne à qui retirer le vote")] IUser user)
        {
            if (!await BeginAsync(Context)) return;

            // The hardcoded voters are a floor: a config change must never revoke a right
            // the code grants.
            if (ShameModule.HardcodedExtraVotersFor(Context.Guild.Id).Contains(user.Id))
            {
                await SayAsync(Context,
                    $"<@{user.Id}> vote par défaut, c'est dans mon code : je ne peux pas lui retirer ça d'ici {Emotes.Staring}");
                return;
            }

            var removed = await _config.RemoveShameVoterAsync(Context.Guild.Id, user.Id);

            await SayAsync(Context, removed
                ? $"<@{user.Id}> ne vote plus. Le staff et le rôle modérateur votent toujours, hein (ᵕ • ᴗ •)"
                : $"<@{user.Id}> n'était pas dans la liste (ᵕ • ᴗ •)");
        }
    }

    [Group("idle-channels", "Les salons qui doivent tous se taire avant qu'elle parle dans le vide")]
    public class IdleChannelsModule : SyncsModuleBase
    {
        private readonly GuildConfigService _config;

        public IdleChannelsModule(GuildConfigService config)
        {
            _config = config;
        }

        [SlashCommand("add", "Ajouter un salon : s'il parle, elle ne parle pas dans le vide")]
        public async Task AddAsync(
            [Summary("channel", "Le salon à surveiller")]
            [ChannelTypes(ChannelType.Text, ChannelType.News, ChannelType.Forum)] IGuildChannel channel)
        {
            if (!await BeginAsync(Context)) return;

            if (AmbientService.HardcodedIdleChannels.Contains(channel.Id))
            {
                await SayAsync(Context,
                    $"<#{channel.Id}> est déjà surveillé par défaut, dans le code. Rien à faire {Emotes.Sparkle}");
                return;
            }

            var added = await _config.AddIdleChannelAsync(Context.Guild.Id, channel.Id);

            await SayAsync(Context, added
                ? $"<#{channel.Id}> est surveillé. Tant qu'on y parle, je me tais ദ്ദി◝ ⩊ ◜.ᐟ"
                : $"<#{channel.Id}> était déjà dans la liste (ᵕ • ᴗ •)");
        }

        [SlashCommand("remove", "Retirer un salon ajouté ici")]
        public async Task RemoveAsync(
            [Summary("channel", "Le salon à ne plus surveiller")]
            [ChannelTypes(ChannelType.Text, ChannelType.News, ChannelType.Forum)] IGuildChannel channel)
        {
            if (!await BeginAsync(Context)) return;

            if (AmbientService.HardcodedIdleChannels.Contains(channel.Id))
            {
                await SayAsync(Context,
                    $"<#{channel.Id}> est surveillé par défaut dans le code : je ne peux pas l'oublier d'ici {Emotes.Staring}");
                return;
            }

            var removed = await _config.RemoveIdleChannelAsync(Context.Guild.Id, channel.Id);

            await SayAsync(Context, removed
                ? $"<#{channel.Id}> n'est plus surveillé. Ce qui s'y dit ne m'empêchera plus de parler {Emotes.Sparkle}"
                : $"<#{channel.Id}> n'était pas dans la liste (ᵕ • ᴗ •)");
        }
    }
}
