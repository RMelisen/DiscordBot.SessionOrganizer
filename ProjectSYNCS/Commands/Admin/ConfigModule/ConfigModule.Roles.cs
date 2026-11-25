using Discord;
using Discord.Interactions;
using Discord.Net;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Commands;

// The roles: the moderator role, and the two she mirrors (Plynling, Grosse tête). Part of /config: the class comment and the shared helpers are in ConfigModule.cs.
public partial class ConfigModule
{
    [Group("moderator-role", "Le rôle autorisé à voter avec /shame")]
    public class ModeratorRoleModule : SyncsModuleBase
    {
        private readonly GuildConfigService _config;

        public ModeratorRoleModule(GuildConfigService config)
        {
            _config = config;
        }

        [SlashCommand("set", "Définir le rôle qui peut voter avec /shame")]
        public async Task SetAsync(
            [Summary("role", "Le rôle des modérateurs")] IRole role)
        {
            if (!await BeginAsync(Context)) return;

            await _config.SetModeratorRoleAsync(Context.Guild.Id, role.Id);

            await SayAsync(Context,
                $"<@&{role.Id}> peut maintenant voter avec `/shame`. "
                + "Le staff et la liste du code gardent leur accès, forcément ദ്ദി◝ ⩊ ◜.ᐟ");
        }

        [SlashCommand("clear", "Retirer le rôle modérateur configuré")]
        public async Task ClearAsync()
        {
            if (!await BeginAsync(Context)) return;

            await _config.SetModeratorRoleAsync(Context.Guild.Id, 0);

            await SayAsync(Context,
                "Plus de rôle modérateur configuré. Seuls le staff et les votants de la liste peuvent voter (ᵕ • ᴗ •)");
        }
    }

    [Group("plynling-role", "Le rôle de tous les maîtres d'un Plynling vivant")]
    public class PlynlingRoleModule : SyncsModuleBase
    {
        private readonly GuildConfigService _config;
        private readonly PlynlingRoleService _roles;
        private readonly RoleMirror _mirror;
        private readonly ILogger<PlynlingRoleModule> _logger;

        public PlynlingRoleModule(GuildConfigService config, PlynlingRoleService roles, RoleMirror mirror,
            ILogger<PlynlingRoleModule> logger)
        {
            _config = config;
            _roles = roles;
            _mirror = mirror;
            _logger = logger;
        }

        private ManagedRole Setup => new(RoleKind.Plynling, "Plynling", PlynlingRoleService.DefaultName,
            id => _config.SetPlynlingRoleAsync(Context.Guild.Id, id),
            () => _roles.LoadHoldersAsync(Context.Guild.Id));

        [SlashCommand("create", "Créer le rôle des maîtres de Plynlings et le gérer")]
        public async Task CreateAsync(
            [Summary("name", "Le nom du rôle (par défaut : Maîtres de Plynlings)")]
            [MaxLength(InputCaps.RoleName)] string? name = null)
        {
            if (!await BeginAsync(Context)) return;
            await CreateManagedRoleAsync(Context, _mirror, _logger, Setup,
                (await _config.GetAsync(Context.Guild.Id)).PlynlingRoleId, name,
                "Renomme-le ou change sa couleur comme tu veux : moi, je ne fais que le donner et le retirer ദ്ദി◝ ⩊ ◜.ᐟ");
        }

        [SlashCommand("set", "Gérer un rôle existant comme rôle Plynling")]
        public async Task SetAsync(
            [Summary("role", "Le rôle à donner aux maîtres de Plynlings")] IRole role)
        {
            if (!await BeginAsync(Context)) return;
            await SetManagedRoleAsync(Context, _mirror, Setup, role, "est maintenant le rôle Plynling.");
        }

        [SlashCommand("clear", "Ne plus gérer de rôle Plynling")]
        public async Task ClearAsync()
        {
            if (!await BeginAsync(Context)) return;

            await _config.SetPlynlingRoleAsync(Context.Guild.Id, 0);

            await SayAsync(Context,
                "Je ne gère plus de rôle Plynling. Le rôle et ses membres restent tels quels : "
                + "supprime-le dans Discord si tu n'en veux plus (ᵕ • ᴗ •)");
        }
    }

    [Group("quiz-role", "Le rôle de la grosse tête du quiz de la semaine")]
    public class QuizRoleModule : SyncsModuleBase
    {
        private readonly GuildConfigService _config;
        private readonly QuizCrownService _crown;
        private readonly RoleMirror _mirror;
        private readonly ILogger<QuizRoleModule> _logger;

        public QuizRoleModule(GuildConfigService config, QuizCrownService crown, RoleMirror mirror,
            ILogger<QuizRoleModule> logger)
        {
            _config = config;
            _crown = crown;
            _mirror = mirror;
            _logger = logger;
        }

        // The first pass gives it to the champion reigning now, silently: Monday announces as usual.
        private ManagedRole Setup => new(RoleKind.QuizCrown, "Grosse tête", QuizCrownService.DefaultName,
            id => _config.SetQuizRoleAsync(Context.Guild.Id, id),
            () => _crown.LoadHoldersAsync(Context.Guild.Id));

        [SlashCommand("create", "Créer le rôle de la grosse tête de la semaine et le gérer")]
        public async Task CreateAsync(
            [Summary("name", "Le nom du rôle (par défaut : Grosse tête de la semaine)")]
            [MaxLength(InputCaps.RoleName)] string? name = null)
        {
            if (!await BeginAsync(Context)) return;
            await CreateManagedRoleAsync(Context, _mirror, _logger, Setup,
                (await _config.GetAsync(Context.Guild.Id)).QuizRoleId, name,
                "Chaque lundi à 10 h, je le donne à la meilleure tête du quiz de la semaine passée ദ്ദി◝ ⩊ ◜.ᐟ");
        }

        [SlashCommand("set", "Gérer un rôle existant comme rôle de la grosse tête")]
        public async Task SetAsync(
            [Summary("role", "Le rôle à donner à la grosse tête de la semaine")] IRole role)
        {
            if (!await BeginAsync(Context)) return;
            await SetManagedRoleAsync(Context, _mirror, Setup, role, "est maintenant le rôle de la grosse tête.");
        }

        [SlashCommand("clear", "Ne plus gérer de rôle de la grosse tête")]
        public async Task ClearAsync()
        {
            if (!await BeginAsync(Context)) return;

            await _config.SetQuizRoleAsync(Context.Guild.Id, 0);

            await SayAsync(Context,
                "Plus de grosse tête : je ne couronne plus personne. Le rôle et ses membres restent tels quels : "
                + "supprime-le dans Discord si tu n'en veux plus (ᵕ • ᴗ •)");
        }
    }
}
