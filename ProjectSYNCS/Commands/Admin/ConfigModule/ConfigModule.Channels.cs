using Discord;
using Discord.Interactions;
using Discord.Net;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Commands;

// The single channels: game, bots, level-up, main, quiz, confessions, free games. Part of /config: the class comment and the shared helpers are in ConfigModule.cs.
public partial class ConfigModule
{
    [Group("game-channel", "Le salon où les Plynlings sont annoncés")]
    public class GameChannelModule : SyncsModuleBase
    {
        private readonly GuildConfigService _config;

        public GameChannelModule(GuildConfigService config)
        {
            _config = config;
        }

        [SlashCommand("set", "Choisir le salon des annonces Plynling (morts, retours, histoires)")]
        public async Task SetAsync(
            [Summary("channel", "Le salon de jeu")]
            [ChannelTypes(ChannelType.Text, ChannelType.News)] IGuildChannel channel)
        {
            if (!await BeginAsync(Context)) return;

            if (!CanSendIn(Context.Guild, channel))
            {
                await SayAsync(Context, CannotSend(channel));
                return;
            }

            // The default stored as zero, so /config show says "par défaut" and a later
            // change to the default follows.
            await _config.SetGameChannelAsync(Context.Guild.Id,
                channel.Id == PlynlingAnnouncer.DefaultGameChannelId ? 0 : channel.Id);

            await SayAsync(Context,
                $"Les Plynlings seront annoncés dans <#{channel.Id}>. Morts, retours, histoires : tout passe par là maintenant {Emotes.Sparkle}");
        }

        [SlashCommand("clear", "Revenir au salon de jeu par défaut")]
        public async Task ClearAsync()
        {
            if (!await BeginAsync(Context)) return;

            await _config.SetGameChannelAsync(Context.Guild.Id, 0);

            await SayAsync(Context,
                Context.Guild.GetChannel(PlynlingAnnouncer.DefaultGameChannelId) is not null
                    ? $"Retour au salon par défaut, <#{PlynlingAnnouncer.DefaultGameChannelId}> (ᵕ • ᴗ •)"
                    : "Plus de salon de jeu ici : les Plynlings de ce serveur ne seront plus annoncés (ᵕ • ᴗ •)");
        }
    }

    [Group("bots-channel", "Le salon où elle poste ce qui compte peu (Quokka, la nuit)")]
    public class BotsChannelModule : SyncsModuleBase
    {
        private readonly GuildConfigService _config;

        public BotsChannelModule(GuildConfigService config)
        {
            _config = config;
        }

        [SlashCommand("set", "Choisir le salon des bots : ses annonces sans importance : Quokka, la nuit")]
        public async Task SetAsync(
            [Summary("channel", "Le salon des bots")]
            [ChannelTypes(ChannelType.Text, ChannelType.News)] IGuildChannel channel)
        {
            if (!await BeginAsync(Context)) return;

            if (!CanSendIn(Context.Guild, channel))
            {
                await SayAsync(Context, CannotSend(channel));
                return;
            }

            // The default stored as zero, like the game channel.
            await _config.SetBotsChannelAsync(Context.Guild.Id, channel.Id == BotsChannel.DefaultId ? 0 : channel.Id);

            await SayAsync(Context,
                $"Mes annonces sans importance iront dans <#{channel.Id}>. Quokka aussi, quand il passera {Emotes.Sparkle}");
        }

        [SlashCommand("clear", "Revenir au salon des bots par défaut")]
        public async Task ClearAsync()
        {
            if (!await BeginAsync(Context)) return;

            await _config.SetBotsChannelAsync(Context.Guild.Id, 0);

            await SayAsync(Context,
                Context.Guild.GetChannel(BotsChannel.DefaultId) is not null
                    ? $"Retour au salon par défaut, <#{BotsChannel.DefaultId}> (ᵕ • ᴗ •)"
                    : "Plus de salon des bots ici : ses annonces passeront par le salon de jeu (ᵕ • ᴗ •)");
        }
    }

    [Group("levelup-channel", "Le salon des cartes de passage de niveau")]
    public class LevelUpChannelModule : SyncsModuleBase
    {
        private readonly GuildConfigService _config;

        public LevelUpChannelModule(GuildConfigService config)
        {
            _config = config;
        }

        [SlashCommand("set", "Choisir le salon où tombent les cartes de niveau")]
        public async Task SetAsync(
            [Summary("channel", "Le salon des niveaux")]
            [ChannelTypes(ChannelType.Text, ChannelType.News)] IGuildChannel channel)
        {
            if (!await BeginAsync(Context)) return;

            if (!CanSendIn(Context.Guild, channel))
            {
                await SayAsync(Context, CannotSend(channel));
                return;
            }

            await _config.SetLevelUpChannelAsync(Context.Guild.Id, channel.Id);
            await SayAsync(Context, $"Les cartes de niveau iront dans <#{channel.Id}> {Emotes.Sparkle}");
        }

        [SlashCommand("clear", "Revenir au salon des bots pour les cartes de niveau")]
        public async Task ClearAsync()
        {
            if (!await BeginAsync(Context)) return;

            await _config.SetLevelUpChannelAsync(Context.Guild.Id, 0);
            await SayAsync(Context, "Les cartes de niveau retournent dans le salon des bots (ᵕ • ᴗ •)");
        }
    }

    [Group("main-channel", "Le salon où elle dit bonjour et vit la nuit")]
    public class MainChannelModule : SyncsModuleBase
    {
        private readonly GuildConfigService _config;

        public MainChannelModule(GuildConfigService config)
        {
            _config = config;
        }

        [SlashCommand("set", "Choisir son salon principal : bonjour du matin, ligne de 3 h, réveil")]
        public async Task SetAsync(
            [Summary("channel", "Le salon principal")]
            [ChannelTypes(ChannelType.Text, ChannelType.News)] IGuildChannel channel)
        {
            if (!await BeginAsync(Context)) return;

            if (!CanSendIn(Context.Guild, channel))
            {
                await SayAsync(Context, CannotSend(channel));
                return;
            }

            await _config.SetMainChannelAsync(Context.Guild.Id,
                channel.Id == MorningGreetingService.DefaultChannelId ? 0 : channel.Id);

            await SayAsync(Context,
                $"J'emménage dans <#{channel.Id}>. Bonjour du matin, nuits blanches, tout se passera là-bas ദ്ദി◝ ⩊ ◜.ᐟ");
        }

        [SlashCommand("clear", "Revenir au salon principal par défaut (ou n'en avoir aucun)")]
        public async Task ClearAsync()
        {
            if (!await BeginAsync(Context)) return;

            await _config.SetMainChannelAsync(Context.Guild.Id, 0);

            await SayAsync(Context, HomeGuild.Is(Context.Guild.Id)
                ? $"Je rentre à la maison, dans <#{MorningGreetingService.DefaultChannelId}> {Emotes.Sparkle}"
                : "Plus de salon principal ici : pas de bonjour du matin, pas de vie ambiante, pas de sondage du week-end (ᵕ • ᴗ •)");
        }
    }

    [Group("quiz-channel", "Le salon où elle pose ses questions de quiz")]
    public class QuizChannelModule : SyncsModuleBase
    {
        private readonly GuildConfigService _config;

        public QuizChannelModule(GuildConfigService config)
        {
            _config = config;
        }

        [SlashCommand("set", "Choisir le salon du quiz (deux ou trois questions par semaine)")]
        public async Task SetAsync(
            [Summary("channel", "Le salon du quiz")]
            [ChannelTypes(ChannelType.Text, ChannelType.News)] IGuildChannel channel)
        {
            if (!await BeginAsync(Context)) return;

            if (!CanSendIn(Context.Guild, channel))
            {
                await SayAsync(Context, CannotSend(channel));
                return;
            }

            await _config.SetQuizChannelAsync(Context.Guild.Id, channel.Id);

            await SayAsync(Context,
                $"Le quiz se passera dans <#{channel.Id}>. Jusqu'à deux questions par jour, quand il y a du monde. Révisez ദ്ദി◝ ⩊ ◜.ᐟ");
        }

        [SlashCommand("clear", "Arrêter le quiz sur ce serveur")]
        public async Task ClearAsync()
        {
            if (!await BeginAsync(Context)) return;

            await _config.SetQuizChannelAsync(Context.Guild.Id, 0);

            await SayAsync(Context,
                "Plus de quiz ici. Une question déjà posée ira quand même jusqu'au bout. Je garde mes cailloux (ᵕ • ᴗ •)");
        }
    }

    [Group("confess-channel", "Le salon où tombent les confessions anonymes (/confess)")]
    public class ConfessChannelModule : SyncsModuleBase
    {
        private readonly GuildConfigService _config;

        public ConfessChannelModule(GuildConfigService config)
        {
            _config = config;
        }

        [SlashCommand("set", "Choisir le salon des confessions et ouvrir /confess")]
        public async Task SetAsync(
            [Summary("channel", "Le salon des confessions")]
            [ChannelTypes(ChannelType.Text, ChannelType.News)] IGuildChannel channel)
        {
            if (!await BeginAsync(Context)) return;

            if (!CanSendIn(Context.Guild, channel))
            {
                await SayAsync(Context, CannotSend(channel));
                return;
            }

            await _config.SetConfessChannelAsync(Context.Guild.Id, channel.Id);

            await SayAsync(Context,
                $"Les confessions tomberont dans <#{channel.Id}>. Anonymes. Enfin, pour vous (¬_¬)");
        }

        [SlashCommand("clear", "Fermer /confess sur ce serveur")]
        public async Task ClearAsync()
        {
            if (!await BeginAsync(Context)) return;

            await _config.SetConfessChannelAsync(Context.Guild.Id, 0);

            await SayAsync(Context,
                "Plus de confessions ici. Les anciennes restent où elles sont, et leurs secrets avec.");
        }
    }

    [Group("freegames-channel", "Le salon où elle annonce les jeux Steam gratuits à garder")]
    public class FreeGamesChannelModule : SyncsModuleBase
    {
        private readonly GuildConfigService _config;

        public FreeGamesChannelModule(GuildConfigService config)
        {
            _config = config;
        }

        [SlashCommand("set", "Choisir le salon des jeux Steam gratuits")]
        public async Task SetAsync(
            [Summary("channel", "Le salon des jeux gratuits")]
            [ChannelTypes(ChannelType.Text, ChannelType.News)] IGuildChannel channel)
        {
            if (!await BeginAsync(Context)) return;

            if (!CanSendIn(Context.Guild, channel))
            {
                await SayAsync(Context, CannotSend(channel));
                return;
            }

            await _config.SetFreeGamesChannelAsync(Context.Guild.Id, channel.Id);

            await SayAsync(Context,
                $"Dès qu'un jeu Steam devient gratuit à garder, je le poste dans <#{channel.Id}>. De rien d'avance {Emotes.Sparkle}");
        }

        [SlashCommand("clear", "Ne plus annoncer les jeux Steam gratuits ici")]
        public async Task ClearAsync()
        {
            if (!await BeginAsync(Context)) return;

            await _config.SetFreeGamesChannelAsync(Context.Guild.Id, 0);

            await SayAsync(Context,
                "D'accord. Je garderai les bons plans pour moi. Tant pis pour vous (¬_¬)");
        }
    }
}
