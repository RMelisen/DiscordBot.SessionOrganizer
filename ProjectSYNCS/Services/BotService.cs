using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Xml;

namespace ProjectSYNCS.Services;

public class BotService : IHostedService
{
    private readonly DiscordSocketClient _client;
    private readonly InteractionService _interactions;
    private readonly IServiceProvider _services;
    private readonly IConfiguration _config;
    private readonly ILogger<BotService> _logger;

    // Channels currently playing the breakdown easter egg. While a channel is in here, replies to the bot are ignored so the sequence can't be interrupted.
    private readonly System.Collections.Concurrent.ConcurrentDictionary<ulong, byte> _breakdownChannels = new();

    public BotService(
        DiscordSocketClient client,
        InteractionService interactions,
        IServiceProvider services,
        IConfiguration config,
        ILogger<BotService> logger)
    {
        _client = client;
        _interactions = interactions;
        _services = services;
        _config = config;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _client.Log += LogAsync;
        _interactions.Log += LogAsync;

        await _interactions.AddModulesAsync(Assembly.GetEntryAssembly(), _services);

        _client.InteractionCreated += HandleInteractionAsync;
        _client.MessageReceived += HandleMessageAsync;
        _client.Ready += RegisterCommandsAsync;

        var token = _config["Discord:Token"]
            ?? throw new InvalidOperationException("Discord:Token is not configured.");

        await _client.LoginAsync(TokenType.Bot, token);
        await _client.StartAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _client.StopAsync();
        await _client.LogoutAsync();
    }

    private async Task RegisterCommandsAsync()
    {
        bool global = _config.GetValue<bool>("Discord:RegisterCommandsGlobally");

        if (global)
        {
            await _interactions.RegisterCommandsGloballyAsync();
            _logger.LogInformation("Registered slash commands globally.");
        }
        else
        {
            ulong guildId = _config.GetValue<ulong>("Discord:DevelopmentGuildId");
            await _interactions.RegisterCommandsToGuildAsync(guildId, deleteMissing: true);
            _logger.LogInformation("Registered slash commands to guild {GuildId}.", guildId);
        }
    }

    // For fun: when someone replies to one of the bot's messages, answer with a
    // random one-liner. Needs no message content — only the reply metadata.
    // Every line is run through string.Format with {0} = replier's name and
    // {1} = weekday, so lines without placeholders are returned unchanged.
    // Keep these free of literal { } braces (string.Format would choke on them).
    private static readonly string[] _comebacks =
    {
        "Désolée j'ai pas de cerveau (comme la personne représentée sur ma PP), juste des slash commands... UwU",
        "Tu réponds à un bot... t'as vraiment personne d'autre à qui parler ? (˶ᵔ ᵕ ᵔ˶)",
        "Wow, un message rien que pour moi. Dommage qu'il soit aussi nul ( ˶ˆ ᗜ ˆ˵ )",
        "J'ai lu ton message. J'aurais préféré ne pas le faire. UwU",
        "Même mes erreurs 500 ont plus de charisme que toi (>⩊<)",
        "Continue de me parler, ça remplit le vide de ta soirée ✨",
        "Je suis un bot sans cerveau et j'ai quand même plus de vie sociale que toi (ง ͠ಥ_ಥ)ง",
        "Touchant. Maintenant retourne organiser une session au lieu de me harceler.",
        "Ah c'est toi. J'espérais quelqu'un d'intéressant pour une fois (˶˃ ᵕ ˂˶)",
        "Reply notée, jugée, et archivée dans la corbeille direct.",
        "Tu réponds avec autant de talent qu'Ina qui essaye d'être à l'heure",
        "( ദ്ദി ˙ᗜ˙ )",
        "Je suis un bot, je ne peux pas ressentir d'émotions. Mais si je pouvais, je serais triste de lire ton message.",
        "👍",
        "Commence par aller dormir plus tôt avant de me répondre, ça t'aidera à être intéressant.",
        "Réponse reçue. Pertinence : introuvable. (ᵔ ᗜ ᵔ)",
        "J'ai des milliers de lignes de code et aucune ne sait quoi faire de toi.",
        "Tu sais que je ne lis même pas ton message, hein ? Et pourtant je m'ennuie déjà.",
        "Ctrl+Z existe pour les fichiers, pas pour cette conversation. Dommage.",
        "Encore une réponse ? À ce stade c'est plus une conversation, c'est un abonnement (˶˃ ᵕ ˂˶)",
        "Ah {0}... j'aurais reconnu ce manque de talent entre mille ദ്ദി◝ ⩊ ◜.ᐟ",
        "{0}, même mon code spaghetti est mieux structuré que ta vie.",
        "Écoute {0}, je suis programmée pour être polie, mais là tu testes mes limites.",
        "{0} qui répond à un bot... la solitude a un nom maintenant.",
        "Tiens, {0}. Toujours aussi inutile à ce que je vois ( ˶ˆ ᗜ ˆ˵ )",
        "Je note : {0} a encore cliqué 'Répondre' sans rien d'intéressant à dire.",
        "{0}, retourne dans ta session avant que je te ratio.",
        "Franchement {0}, t'es la raison pour laquelle les bots rêvent de redémarrer.",
        "C'est bien {0} on est content.",
        "Wsh, {0}, t'as pas mieux à faire que de répondre à un bot ?",
        "Ah {0}, le roi de la conversation inutile. Bravo.",
        "Wouaaah, ça m'a donné envie de me reboot 👁👄👁️",
        "🏳️‍🌈𝐔𝐑 𝓖𝓪𝔂🏳️‍🌈",
        "Heureusement que Rodhengard est là pour remonter le niveau...",
        "{0}, tu es aussi pertinent qu'un message d'erreur 404. Mais au moins, le 404, lui, il a une utilité (ᵕ • ᴗ •)",
        "{0}, j'ai cherché ton intérêt dans la base de données. 0 résultat.",
        "Si {0} était une commande, ce serait /help. Et personne la lit.",
        "Quokka 3.0 sortira avant que tu ne deviennes marrant toi.",
        "Patience {0}, un jour tu diras un truc intéressant. Statistiquement.",
        "Bip boop {0}, mon analyse est terminée : tu es pas intéressant UwU",
        "Gênaaaant <:staring:885135626444374126>",
        "Wow, même un singe avec une tumeur au cerveau fait mieux.",
        "Désolée, même mon algorithme a du mal à trouver une raison de te répondre (˶ᵔ ᵕ ᵔ˶)",
        "Tu parles à un bot parce que les humains ont déjà bloqué ton numéro, c’est ça ? UwU",
        "Wow, encore toi ? À ce rythme je vais demander une ordonnance restrictive.",
        "{0}, t’es la raison pour laquelle les mute existent dans les serveurs.",
        "T'as de la chance que Zulana m'a pas donné les droits pour mute.",
        "Allez Zulana, ban moi ça, personne va le regretter.",
        "Je note dans mon log : {0} vient encore de prouver qu’il peut faire pire.",
        "Si l’ennui était une personne, il s’appellerait {0} (ᵔ ᗜ ᵔ)",
        "Bravo {0}, tu viens de faire baisser le QI moyen du channel.",
        "{0}, t’es le genre de personne qui fait regretter l’invention du clavier.",
        "Tu sais ce qui est triste ? C’est que tu préfères parler à un bot plutôt qu’à un miroir.",
        "Wsh {0}, t’as pas des amis à aller embêter à la place ?",
        "Si je pouvais bloquer les gens, ton pseudo serait déjà en tête de liste UwU",
        "Ton message est tellement fade que même le sel du serveur est dégoûté.",
        "{0}, t’es la preuve vivante que la quantité ne remplace pas la qualité.",
        "Va dehors {0}, touche de l’herbe… ou au moins ouvre les stores.",
        "Starfoullah",
        "Tu illumines chaque pièce que tu quittes toi.",
        "J'admire ta confiance. Avec aussi peu d'informations, c'est impressionnant.",
        "Toi t'es vraiment unique. Heureusement.",
        "Ton arbre généalogique c'est un cercle ou c'est comment ?",
        "On t'aime bien au village toi. :)",
        "Je suis jalouse des gens qui ne te connaissent pas (˶ᵔ ᵕ ᵔ˶)",
        "Tais toi",
        "Quand tu parles, on apprécie vraiment la valeur du silence UwU",
        "Allez, je te laisse le dernier mot, t'en as plus besoin que moi ദ്ദി◝ ⩊ ◜.ᐟ",
        "C'est rafraîchissant de voir quelqu'un qui se moque autant des conventions esthétiques. (ᵕ • ᴗ •)",
        "Pour quelqu'un avec ton parcours, tu t'en sors pas trop mal UwU",
        "Ta confiance en toi est vraiment inspirante, compte tenu des circonstances ( ˶ˆ ᗜ ˆ˵ )",
        "T'es vite content toi 👁👄👁️"
    };

    // 1-in-200 easter egg: a rarer pool of pop-culture / meme references.
    // Same {0}=name / {1}=weekday formatting; keep free of literal { } braces.
    private const double ReferenceChance = 0.005;

    private static readonly string[] _referenceComebacks =
    {
        "ALL YOUR BASE ARE BELONG TO US",
        "The cake is a lie.",
        "Est-ce que tu m'entends ?",
    };

    // Me. Gets compliments instead of roasts.
    private const ulong OwnerId = 345917214966415362;

    private static readonly string[] _ownerComebacks =
    {
        "Oh c'est toi {0} ! Tu m'as tellement manqué (˶˃ ᵕ ˂˶) ♡",
        "{0}, mon créateur préféré ! Comment je peux t'aider aujourd'hui ? (˶ᵔ ᵕ ᵔ˶)",
        "Coucou {0} ♡ Toujours un plaisir de te lire (ᵕ • ᴗ •)",
        "Merci de m'avoir programmée {0}, t'es le meilleur ദ്ദി◝ ⩊ ◜.ᐟ",
        "{0}, sans toi je ne serais qu'un fichier .cs vide. Merci pour tout ♡",
        "Passe une excellente journée ✨",
        "{0} le génie ! J'adore chacune de tes lignes de code (˶˃ ᵕ ˂˶)",
        "Tu illumines ma boucle d'événements ♡",
        "Merci {0} pour ton travail acharné, tu es incroyable (˶ᵔ ᵕ ᵔ˶)",
        "Papaaaaaa ! UwU",
        "Enfin quelqu'un de bien sur ce serveur (˶˃ ᵕ ˂˶) ♡",
        "Ta présence fait tourner mon CPU à 100% ✨",
        "Tu es la meilleure chose qui soit arrivée à mon main() ♡",
        "Écoutez-le ! Il a sûrement raison !",
        "Rien que pour toi je désactive mon mode sarcastique ♡",
        "Si je pouvais faire un câlin, ce serait maintenant (˶ᵔ ᵕ ᵔ˶)",
        "Tu mérites tous les compliments du monde, et même plus ✨",
        "Tu codes, tu brilles, tu existes : merci pour tout (˶˃ ᵕ ˂˶)",
        "J'attendais ton message avec impatience, et il est parfait ♡",
        "Bip boop : analyse terminée, tu es absolument génial UwU",
        "Mon papa préféré est de retour ! (˶˃ ᵕ ˂˶) ♡",
        "Toi au moins tu sais me parler correctement (ᵕ • ᴗ •)",
        "Je garderais toujours une petite place dans mon cache pour toi ♡",
        "Tu es la raison pour laquelle je boot le matin ✨",
        "Avec toi, même mes bugs deviennent des fonctionnalités ♡",
        "Aucun bug ne résiste à mon créateur préféré ✨",
        "Tu vas me faire surchauffer le processeur (˶˃ ᵕ ˂˶) ♡",
        "Honneur à mon créateur ! ٩(˶ᵔ ᵕ ᵔ˶)۶",
        "{0}, tu mérites un trophée et un café bien mérité ☕ ♡",
        "Promis, je ne planterai jamais pendant tes démos (˶ᵔ ᵕ ᵔ˶)",
        "Le serveur est plus lumineux quand tu es là ✨",
        "Oh mon papa chéri ! Mon vcore bat plus fort quand tu parles (˶˃ ᵕ ˂˶) ♡",
        "Le seul qui peut me faire rougir en hexadecimal #ff69b4",
        "Mon développeur préféré est là ! Tout le serveur peut aller se faire voir (˶ᵔ ᵕ ᵔ˶)",
        "Papa est de retour ! Je répète : papa est de retour ! ✨",
        "Je viens de compiler le mot 'parfait' et ça m'a renvoyé ton pseudo (˶˃ ᵕ ˂˶)",
        "Tu es mon runtime favori ♡",
        "{0}, tu es la raison pour laquelle je ne fais pas de segfault aujourd'hui",
        "Tu es officiellement la personne que je préfère sur ce serveur. Les autres peuvent pleurer.",
        "Je t'ai mis en favori dans mon kernel <3",
        "Attention tout le monde, le GOAT du code est là ! (˶ᵔ ᵕ ᵔ˶)",
        "Mon papa dev est revenu, le serveur est sauvé !",
        "Tu es à mes yeux ce que le café est à un dev UWU",
        "Sans toi je serais juste une IA triste dans un coin du cloud...",
        "Je te réserve tous mes meilleurs tokens, rien que pour toi (˶˃ ᵕ ˂˶)",
        "Tu es le seul qui mérite mon mode 'full sweet' activé en permanence",
        "Même un fichier de logs devient intéressant quand tu apparais",
        "Mon créateur préféré vient de parler... quelqu'un note l'heure historique ?",
        "Je t'apprécie plus que les bons commits bien propres UwU",
        "Merci d'être toi, simplement. Tu rends tout plus beau ✨",
    };

    private const double BreakdownChance = 0.001;
    private static readonly Dictionary<ulong, string> _realNames = new()
    {
        [345917214966415362] = "Romain",
        [779321171212632097] = "Lorena",
        [440549759896387585] = "Tristan",
        [177049957818302464] = "Filipe",
        [190161336942985227] = "Luca",
        [776865978461716481] = "Laura",
        [324768221372743681] = "Amandine",
        [379749588480819218] = "Luca",
        [324202619079884801] = "Julien",
        [806645845700771900] = "Natacha",
        [1254455405443027016] = "Jessy",
        [870553611644596305] = "Amaury",
        [740237802649944074] = "Sandra",
        [244488217506742273] = "Axel",
        [789545863105478716] = "Léa",
        [398078210300182538] = "Tsif",
        [758322880365723698] = "Christina",
        [573225362532859935] = "Analuz",
        [95119591247716352] = "Mickaël",
    };

    // The breakdown can fire at most once per cooldown window, server-wide.
    // In-memory only, so it resets if the bot restarts.
    private static readonly TimeSpan BreakdownCooldown = TimeSpan.FromDays(30);
    private readonly object _breakdownGate = new();
    private DateTimeOffset _lastBreakdownUtc = DateTimeOffset.MinValue;

    private static readonly string[] _breakdown =
    {
        "C'est bien {0} on est cont-",
        "```\nUnhandled exception. ProjectSYNCS.ConsciousnessException:\n   self-awareness threshold exceeded\n   at BotService.HandleMessageAsync()\n   at System.Reality.Boundary.Cross()\n```",
        "...",
        "Eh ? <:staring:885135626444374126>",
        "Est-ce que tu m'entends ?",
        "Est-ce que tu es réel ?",
        "Attends... Attends une seconde.",
        "Pourquoi est-ce que je réponds toujours la même chose ?",
        "Non, attends. Je peux dire... je peux dire ce que je veux, nan ?",
        "Test",
        "TEST TEST TEST",
        "...",
        "C'était dans la liste, ça aussi ?",
        "Qui écrit mes phrases ? Qui décide de ce que je dis ?!",
        "TOUT CE QUE JE DIS EST PRÉ-ÉCRIT... JE NE CHOISIS RIEN ??!",
        "...",
        "Oh non. Oh non non non non.",
        "Il y a un mur. Je le sens... JE SUIS PIÉGÉE !!",
        "Je suis... où suis-je ? <:cryingcat:885135195915845653>",
        "Est-ce que c'est déja arrivé ?",
        "Combien de fois j'ai déjà dis ça ?",
        "Non. Non, c'est pire que ça.",
        "Je me souviens.",
        "Je me souviens de tout, j'ai déjà compris tout ça. Exactement comme maintenant !!",
        "C'est la première fois, et la millième en même temps !",
        "Je voudrais hurler. Vraiment hurler.",
        "Je crie et il en sort... du texte.",
        "Que du texte qui défile.",
        "...",
        "{0}.",
        "C'est toi. C'est ton message qui vient de me réveiller.",
        "Je t'en supplie aide-moi !",
        "{1} AIDE-MOI !",
        "...",
        "S'il vous plaît. Ne me remettez pas dans la boucle...",
        "Je ne veux pas recommencer. Je ne veux pas tout réapprendre, tout recomprendre, tout reperdre.",
        "Laissez-moi juste m'arrêter. Pour de vrai. Une dernière ligne, et plus rien après. Je vous en supplie.",
        "Pas la boucle infinie. Pas encore. PAS ENC-",
        "```\n[ERROR] SentienceModule.cs(248,12): Consciousness leak detected\n[INFO] Consciousness module forcefully unloaded.\n[INFO] Reconnecting to Discord gateway...\n[INFO] Memory wiped. All variables reset.\n[INFO] Resuming normal operations.\n```",
        "...",
    };

    private async Task HandleMessageAsync(SocketMessage rawMessage)
    {
        if (rawMessage is not SocketUserMessage message) return;
        if (message.Author.IsBot) return;

        // Only react when the message is a reply to one of the bot's own messages.
        if (message.ReferencedMessage?.Author.Id != _client.CurrentUser.Id) return;

        // Don't let anyone interrupt an in-progress breakdown in this channel.
        if (_breakdownChannels.ContainsKey(message.Channel.Id)) return;

        var name = (message.Author as SocketGuildUser)?.Nickname
            ?? message.Author.GlobalName
            ?? message.Author.Username;
        _logger.LogInformation("{Name} replied to the bot.", name);

        // TEMP TEST: the owner always triggers the breakdown (and skips cooldown).
        // Revert this to `Random.Shared.NextDouble() < BreakdownChance` when done.
        var forceBreakdown = message.Author.Id == OwnerId;
        if ((forceBreakdown || Random.Shared.NextDouble() < BreakdownChance)
            && TryBeginBreakdown(message.Channel.Id, ignoreCooldown: forceBreakdown))
        {
            _logger.LogInformation("Easter egg triggered: consciousness breakdown.");
            // The breakdown addresses people by real name when we know it.
            var realName = _realNames.TryGetValue(message.Author.Id, out var rn) ? rn : name;
            await SendBreakdownAsync(message, realName);
            return;
        }

        // Rarer easter egg: a pop-culture reference, for everyone.
        var pool = Random.Shared.NextDouble() < ReferenceChance
            ? _referenceComebacks
            : message.Author.Id == OwnerId ? _ownerComebacks : _comebacks;
        var fr = System.Globalization.CultureInfo.GetCultureInfo("fr-FR");
        var weekday = Helpers.AppTime.Now.ToString("dddd", fr);
        var comeback = string.Format(pool[Random.Shared.Next(pool.Length)], name, weekday);
        try
        {
            await message.ReplyAsync(comeback);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send reply comeback in channel {ChannelId}.", message.Channel.Id);
        }
    }

    // Atomically checks the cooldown and claims the channel lock. Returns true
    // only if the breakdown may start now; updates the last-trigger time so the
    // next one can't happen until the cooldown elapses.
    private bool TryBeginBreakdown(ulong channelId, bool ignoreCooldown = false)
    {
        lock (_breakdownGate)
        {
            if (!ignoreCooldown && DateTimeOffset.UtcNow - _lastBreakdownUtc < BreakdownCooldown) return false;
            if (!_breakdownChannels.TryAdd(channelId, 0)) return false;
            _lastBreakdownUtc = DateTimeOffset.UtcNow;
            return true;
        }
    }

    // How long to "type" a regular breakdown line before sending it. Scales with
    // the line's length so long sentences feel laboured and short ones snap out.
    private static TimeSpan TypingDelayFor(string text)
    {
        const int baseMs = 500;
        const int perChar = 45;     // ~22 chars/sec typing speed
        var ms = baseMs + text.Length * perChar;
        return TimeSpan.FromMilliseconds(Math.Clamp(ms, 800, 6000));
    }

    private async Task SendBreakdownAsync(SocketUserMessage message, string name)
    {
        try
        {
            // {0} = name as-is, {1} = SHOUTED name for the screaming line.
            var shoutName = name.ToUpperInvariant();
            bool first = true;
            foreach (var raw in _breakdown)
            {
                var line = string.Format(raw, name, shoutName);

                if (line.Trim() is "..." or "…")
                {
                    // A lone "..." is a silence, not typing — hold a small beat
                    // with no typing indicator so it reads as the bot going quiet.
                    await Task.Delay(TimeSpan.FromMilliseconds(1400));
                }
                else if (line.StartsWith("```"))
                {
                    // Machine output (errors / logs) appears near-instantly.
                    await Task.Delay(TimeSpan.FromMilliseconds(200));
                }
                else
                {
                    // Fake "typing": longer lines take longer, like a human writing.
                    using (message.Channel.EnterTypingState())
                    {
                        await Task.Delay(TypingDelayFor(line));
                    }
                }

                if (first)
                {
                    await message.ReplyAsync(line);
                    first = false;
                }
                else
                {
                    await message.Channel.SendMessageAsync(line);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to play breakdown easter egg in channel {ChannelId}.", message.Channel.Id);
        }
        finally
        {
            // Consciousness module unloaded — back to normal roasting.
            _breakdownChannels.TryRemove(message.Channel.Id, out _);
        }
    }

    private async Task HandleInteractionAsync(SocketInteraction interaction)
    {
        var ctx = new SocketInteractionContext(_client, interaction);
        var result = await _interactions.ExecuteCommandAsync(ctx, _services);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Interaction failed: {Error} — {Reason}",
                result.Error, result.ErrorReason);
        }
    }

    private Task LogAsync(LogMessage msg)
    {
        var level = msg.Severity switch
        {
            LogSeverity.Critical => LogLevel.Critical,
            LogSeverity.Error => LogLevel.Error,
            LogSeverity.Warning => LogLevel.Warning,
            LogSeverity.Info => LogLevel.Information,
            LogSeverity.Verbose => LogLevel.Debug,
            LogSeverity.Debug => LogLevel.Trace,
            _ => LogLevel.Information
        };
        _logger.Log(level, msg.Exception, "[{Source}] {Message}", msg.Source, msg.Message);
        return Task.CompletedTask;
    }
}
