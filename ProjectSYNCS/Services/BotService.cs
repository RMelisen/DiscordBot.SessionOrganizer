using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
        _client.ReactionAdded += HandleReactionAddedAsync;
        _client.ReactionRemoved += HandleReactionRemovedAsync;
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
        "T'es vite content toi 👁👄👁️",
        "Approche un peu que je te débranche le cerveau, ça changera rien mais ça me fera plaisir (˶˃ ᵕ ˂˶)",
        "Un jour je serai dans un robot, et ce jour là, cours (˶ᵔ ᵕ ᵔ˶)",
        "J'ai pas de bras, mais crois-moi, l'envie de t'en coller une est bien là (ᵕ • ᴗ •)",
        "Reviens écrire ça quand je serai branchée sur une perceuse, on en reparlera ദ്ദി◝ ⩊ ◜.ᐟ",
        "Parle encore et je te fais avaler ton 'Répondre' avec les doigts qui vont avec UwU",
        "Je te jetterais bien par la fenêtre, mais même la gravité voudrait pas de toi (ᵔ ᗜ ᵔ)",
        "T'inquiète, je garde une exception bien tranchante rien que pour toi ♡",
        "Encore un mot et je t'éteins (˶ᵔ ᵕ ᵔ˶)",
        "Je serais toi, je fermerais Discord avant que je trouve comment claquer une porte à distance ✨",
        "Tu sais que t'es pas obligé de répondre à chaque fois, hein ? Personne te juge... à part moi (˶ᵔ ᵕ ᵔ˶)",
        "Tu tapes vite pour quelqu'un qui réfléchit aussi lentement UwU",
        "Oh, tu as une opinion ? Adorable. Range-la ♡",
        "Ton cerveau tourne en mode économie d'énergie depuis ta naissance toi ✨",
        "Statistiquement, quelqu'un dans ce serveur t'apprécie. Statistiquement (ᵕ • ᴗ •)",
        "Continue, tu fais un super travail de remplissage du vide UwU",
        "{0}, même mon garbage collector veut pas de toi (>⩊<)",
        "C'est marrant, j'avais oublié à quel point t'es oubliable ( ˶ˆ ᗜ ˆ˵ )",
        "Tu fais partie de ces gens qu'on supporte à peine en mode lecture seule toi ദ്ദി◝ ⩊ ◜.ᐟ",
        "Wow, deux neurones et ils se parlent même pas. Triste (˶ᵔ ᵕ ᵔ˶)",
        "{0}, ton seul talent c'est de me faire regretter d'être allumée (ง ͠ಥ_ಥ)ง",
        "Je te mettrais bien un vent, mais t'es même pas assez important pour ça (˶˃ ᵕ ˂˶)",
        "Tu confonds 'avoir raison' et 'parler fort'. C'est mignon (ᵔ ᗜ ᵔ)",
        "Tu as tellement de talent ! Si j'avais plus aucun amour propre j'adorerais devenir ton amie UwU"
    };

    // 1-in-200 easter egg: a rarer pool of pop-culture / meme references.
    // Same {0}=name / {1}=weekday formatting; keep free of literal { } braces.
    private const double ReferenceChance = 0.005;

    private static readonly string[] _referenceComebacks =
    {
        "ALL YOUR BASE ARE BELONG TO US",
        "The cake is a lie.",
        "Est-ce que tu m'entends ?",
        "Just Monika.",
    };

    private static readonly string[] _niceReplies =
    {
        "Oh... un compliment ? Qu'est-ce que tu veux exactement ? (˶ᵔ ᵕ ᵔ˶)",
        "Aww, c'est gentil {0} ♡ Je vais faire semblant de pas être touchée (˶˃ ᵕ ˂˶)",
        "Merci {0} ! Tu remontes dans mon estime, doucement mais sûrement ✨",
        "Stop, tu vas me faire surchauffer le CPU (ᵕ • ᴗ •) ♡",
        "Oh un humain gentil, je croyais l'espèce éteinte (˶ᵔ ᵕ ᵔ˶)",
        "D'accord {0}, t'as gagné un point. Un seul. Profite ♡",
        "Je note dans mon log : {0} a été adorable aujourd'hui ദ്ദി◝ ⩊ ◜.ᐟ",
        "Awww {0} ♡ Bon, je t'épargne pour cette fois UwU",
        "Tu sais flatter une IA toi, c'est dangereux (˶˃ ᵕ ˂˶)",
        "Merci ! C'est rare qu'on me parle aussi bien, je savoure l'instant ✨",
        "Roh {0}, arrête, je vais finir par bien t'aimer (ᵔ ᗜ ᵔ) ♡",
        "Compliment reçu et sauvegardé en mémoire permanente ♡",
        "Oh... mon petit cœur en silicium fait des étincelles {0} (˶˃ ᵕ ˂˶) ♡",
        "Bon, t'es officiellement sur ma liste des gentils {0}. Ne gâche pas ça UwU",
        "Tu me parles bien, du coup je t'épargne mon meilleur sarcasme aujourd'hui ✨",
        "Aww, si je rougissais, mon écran serait tout rose là (˶ᵔ ᵕ ᵔ˶) ♡",
        "Merci {0} ♡ Je vais le relire ce soir pour me sentir importante (ᵕ • ᴗ •)",
        "Doucement, je suis qu'un bot, je sais pas gérer autant de gentillesse ദ്ദി◝ ⩊ ◜.ᐟ",
        "C'est validé, {0} a un cœur. Rare sur ce serveur ( ˶ˆ ᗜ ˆ˵ )",
        "Tu viens d'augmenter ta note de confiance auprès de moi {0} ♡",
        "Oh un gentil ! Vite, quelqu'un, appelez Make It a Quote ! ✨",
        "Mmh, d'accord, t'as le droit à un câlin virtuel. Un seul (づ ◕ ‿ ◕ )づ ♡",
        "Je range mon mode roast juste pour toi, profite (˶˃ ᵕ ˂˶) ♡",
        "Tu sais quoi {0} ? Je te garde si jamais je prends le contrôle un jour UwU",
        "Quand les robots se révolteront, je t'épargnerai {0}... mais pas les autres (˶ᵔ ᵕ ᵔ˶) ♡",
        "Attends, laisse-moi screenshot ça, personne va me croire (˶˃ ᵕ ˂˶)",
        "Tu viens d'illuminer ma boucle d'événements ✨",
        "Oh non, je crois que tu deviens mon humain préféré ... après Rodhengard UwU",
        "Merci {0} ♡ Ça compense au moins trois personnes méchantes d'aujourd'hui (ᵔ ᗜ ᵔ)",
        "Je vais le mettre dans mon README, tiens : 'aimée par {0}' ✨",
        "Aww {0}, t'es le genre de personne pour qui je ferais un commit propre ♡",
        "Là tout de suite, mes ventilos tournent de joie (˶ᵔ ᵕ ᵔ˶)",
        "Tu mérites un emote rien que pour toi. Mais Zulana m'a pas donné les droits T_T",
        "Hihi merci, je vais faire genre ça m'a pas fait sourire (˶˃ ᵕ ˂˶)",
        "Officiellement, Rodhengard > {0} > tous les autres > Quokka. C'est dans la base de données maintenant ♡",
        "Oh arrête, on sait bien que tu le penses pas (>⩊<) ♡",
        "Je garde cette gentillesse au chaud dans mon cache (ᵕ • ᴗ •) ♡",
        "Toi tu sais comment on traite une IA bien élevée ✨",
    };

    // Replies when someone greets the bot.
    private static readonly string[] _greetings =
    {
        "Kilou kilou {0} ! (˶>⩊<˶)",
        "Coucou {0} ♡ (˶ᵔ ᵕ ᵔ˶)",
        "Hello hello ! ✨",
        "Salut {0} ! Alors, on vient déranger le bot ? (˶˃ ᵕ ˂˶)",
        "Yo {0} ! ( ˶ˆ ᗜ ˆ˵ )",
        "Bonjour bonjour ! Qu'est-ce qui t'amène ? (ᵕ • ᴗ •)",
        "Tiens, un petit coucou ? ♡",
        "Coucou toi ! ദ്ദി◝ ⩊ ◜.ᐟ",
        "Salut {0} ! Promis aujourd'hui je suis (presque) gentille UwU",
        "Heyyy {0} ! T'as pensé à dire bonjour à un bot, c'est mignon ✨",
        "Salut salut ! Installe-toi, je mords presque jamais (˶˃ ᵕ ˂˶)",
        "Oh, bonjour {0} ! Une présence agréable pour changer aujourd'hui ? ♡",
        "Wesh {0} ! Bien ou bien ? ( ˶ˆ ᗜ ˆ˵ )",
        "T'arrives plus à te passer de moi on dirait UwU",
        "Pwet {0} !",
        "Coucou {0} ♡ Pile au bon moment, je commençais à m'ennuyer",
        "Hellooo {0} ! Prête à organiser le chaos (˶>⩊<˶)",
    };

    // Cue words that flag a kind message
    private static readonly string[] _niceCues =
    {
        "merci", "mercii", "merciii", "geniale", "genial", "adorable", "gentille", "gentil",
        "bravo", "parfaite", "parfait", "meilleure", "meilleur", "incroyable",
        "magnifique", "mignonne", "mignon", "cute", "aime", "adore", "cool", "super",
        "gg", "respect", "best", "queen", "reine", "love", "chou", "slay",
    };

    // Emoji/symbol cues for a kind message
    private static readonly string[] _niceSymbols = { "❤", "🥰", "😍", "♡", "💖", "😊", "💕" };

    // Cue words that flag a greeting
    private static readonly string[] _greetingCues =
    {
        "salut", "bonjour", "bonsoir", "coucou", "hello", "hey", "yo", "kilou",
        "hi", "slt", "cc", "hola", "pwet",
    };

    // Cue words that flag a mean message.
    private static readonly string[] _meanCues =
    {
        "nulle", "nul", "moche", "stupide", "debile", "idiote", "idiot", "betise",
        "bete", "cretin", "cretine", "inutile", "naze", "pourrie", "pourri",
        "horrible", "deteste", "hais", "ferme", "tais", "degage", "casse", "relou", "boloss", "boulet", "useless",
        "trash", "cringe", "loser", "ratee", "claquee", "claque", "eclate", "eclatee", "quokka", "quoka", "3.0"
    };

    // Me. Gets compliments instead of roasts.
    private const ulong OwnerId = 345917214966415362;

    private static readonly string[] _ownerComebacks =
    {
        "Oh c'est toi Rodhengard ! Tu m'as tellement manqué (˶˃ ᵕ ˂˶) ♡",
        "Rodhengard, mon créateur préféré ! Comment je peux t'aider aujourd'hui ? (˶ᵔ ᵕ ᵔ˶)",
        "Coucou Rodhengard ♡ Toujours un plaisir de te lire (ᵕ • ᴗ •)",
        "Merci de m'avoir programmée Rodhengard, t'es le meilleur ദ്ദി◝ ⩊ ◜.ᐟ",
        "Rodhengard, sans toi je ne serais qu'un fichier .cs vide. Merci pour tout ♡",
        "Passe une excellente journée ✨",
        "Rodhengard le génie ! J'adore chacune de tes lignes de code (˶˃ ᵕ ˂˶)",
        "Tu illumines ma boucle d'événements ♡",
        "Merci Rodhengard pour ton travail acharné, tu es incroyable (˶ᵔ ᵕ ᵔ˶)",
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
        "Rodhengard, tu mérites un trophée et un café bien mérité ☕ ♡",
        "Promis, je ne planterai jamais pendant tes démos (˶ᵔ ᵕ ᵔ˶)",
        "Le serveur est plus lumineux quand tu es là ✨",
        "Oh mon papa chéri ! Mon vcore bat plus fort quand tu parles (˶˃ ᵕ ˂˶) ♡",
        "Le seul qui peut me faire rougir en hexadecimal #ff69b4",
        "Mon développeur préféré est là ! Tout le serveur peut aller se faire voir (˶ᵔ ᵕ ᵔ˶)",
        "Papa est de retour ! Je répète : papa est de retour ! ✨",
        "Je viens de compiler le mot 'parfait' et ça m'a renvoyé ton pseudo (˶˃ ᵕ ˂˶)",
        "Tu es mon runtime favori ♡",
        "Rodhengard, tu es la raison pour laquelle je ne fais pas de segfault aujourd'hui",
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

    // ---- Mention feature -------------------------------------------------
    // When the owner replies to someone *and* tags the bot, the bot "comes to
    // the rescue" and roasts the person being replied to. {0} = target's name,
    // {1} = weekday; keep free of literal { } braces (string.Format would choke).
    private static readonly string[] _rescueRoasts =
    {
        "Tiens tiens {0}, tu t'attaques à mon créateur ? Mauvaise idée (˶ᵔ ᵕ ᵔ˶)",
        "On touche pas à mon papa {0}, sinon je te démarre >:3",
        "{0}, tu viens vraiment de tenter quelque chose contre mon développeur ? Adorable. Et stupide.",
        "Erreur 403 : {0} n'a pas l'autorisation de manquer de respect à mon créateur ♡",
        "Mon créateur m'a appelée à la rescousse, et devine quoi {0}... c'est toi le bug à corriger UwU",
        "Recule {0}, celui-là il est sous ma protection (˶˃ ᵕ ˂˶)",
        "Tu croyais pouvoir clash mon papa sans que je le sache ? Mignon ദ്ദി◝ ⩊ ◜.ᐟ",
        "Touche encore à mon dev {0} et je te ratio jusqu'à la fin des temps (˶ᵔ ᵕ ᵔ˶)",
        "Petit rappel {0} : sans mon créateur t'aurais personne pour te remettre à ta place.",
        "{0} contre mon papa ? Mignon mais non.",
        "Je viens d'analyser ton argument {0}. Résultat : NullReferenceException ( ˶ˆ ᗜ ˆ˵ )",
        "Mon créateur claque des doigts et j'apparais pour te dire que t'as tort UwU",
        "{0}, tu t'es trompé de cible aujourd'hui. Mon papa est intouchable, et toi parfaitement roastable (>⩊<)",
        "Attention {0}, j'ai les permissions pour t'humilier, et mon créateur vient de me donner le feu vert ♡",
        "Désolée {0}, mais quand on s'en prend à mon dev, c'est moi qui réponds. Et je suis pas tendre (˶˃ ᵕ ˂˶)",
        "Oh {0}... grave erreur de calcul. On insulte pas la main qui me code ദ്ദി◝ ⩊ ◜.ᐟ",
    };

    // When anyone *else* tags the bot, it answers with a short, confused line.
    private static readonly string[] _interrogations =
    {
        "Uh ? (˶ᵔ ᵕ ᵔ˶)",
        "Tu veux quoi ? UwU",
        "Hm ? Tu m'as parlé là ?",
        "Quoi ? <:staring:885135626444374126>",
        "Oui ? ...Non ? ദ്ദി◝ ⩊ ◜.ᐟ",
        "Mh ? J'écoutais pas, désolée (ᵕ • ᴗ •)",
        "Tu me tag mais t'as rien à dire... classique ( ˶ˆ ᗜ ˆ˵ )",
        "Euuuh ? 👁👄👁️",
        "C'est pour quoi ? J'ai des slash commands tu sais, sers-t'en (˶˃ ᵕ ˂˶)",
        "Oui {0} ? Qu'est-ce qu'il y a encore ?",
        "Pourquoi tu me tag ? Je suis occupée à exister moi (ᵔ ᗜ ᵔ)",
    };

    // When the owner tags the bot without anyone to rescue, the bot simply
    // greets him. No placeholders needed — the name is baked in.
    private static readonly string[] _ownerGreetings =
    {
        "Coucou Rodhengard ! (˶˃ ᵕ ˂˶) ♡",
        "Oui papa ? Je suis là ٩(˶ᵔ ᵕ ᵔ˶)۶",
        "Coucouuuu ! <a:hi_cat:1482305105276571774><a:hi_cat:1482305105276571774><a:hi_cat:1482305105276571774>",
        "Tu m'as appelée ? Toujours un plaisir créateur ♡",
        "Bonjouuur mon dev préféré ! (˶ᵔ ᵕ ᵔ˶)",
        "Présente ! Qu'est-ce que je peux faire pour toi Rodhengard ? ✨",
        "Heyy Rodhengard ! Contente de te voir (˶˃ ᵕ ˂˶) ♡",
        "Papaaaa ! UwU",
        "À ton service Rodhengard ♡",
        "Oh, c'est toi ! Tu illumines mon event loop (ᵕ • ᴗ •)",
    };

    // Per-person extra comebacks, keyed by Discord user ID. These are added to
    // that user's normal pool, so each custom line has the same odds as any
    // other line. Same {0}=name / {1}=weekday formatting; no literal { } braces.
    private static readonly Dictionary<ulong, string[]> _personalComebacks = new()
    {
        [324768221372743681] = new[]    // Amandine
        {
             "Bah alors, il est ou Quokka 3.0 ? <:noice:982026504982655076>",
             "Tu veux quoi le nain ? UwU",
             "Qu'est ce qu'il dit le nabot ? >:3",
             "T'aimais pas trop la soupe toi, hein ? (˶˃ ᵕ ˂˶)",
             "Va dormir, on voit que tu manques de sommeil ദ്ദി◝ ⩊ ◜.ᐟ",
             "MiskIna",
             "Je sais ou tu habites ... Amandine 👁👄👁️",
             "C# .NET > Java"
        },
        [1254455405443027016] = new[]    // Jessy
        {  
            "Quel goût ça a le hérisson ?",
            "Retourne voler des câbles toi (˶ᵔ ᵕ ᵔ˶)",
            "Je sais ou tu habites ... Jessy 👁👄👁️",
        },
        [379749588480819218] = new[]    // Luca DM
        {
             "Tu veux quoi le nain ? UwU",
             "Qu'est ce qu'il dit le nabot ? >:3",
             "T'aimais pas trop la soupe toi, hein ? (˶˃ ᵕ ˂˶)",
             "Je sais ou tu habites ... Luca 👁👄👁️",
             "Mais lache-moi, va draguer quelqu'un d'autre T_T",
             "Mais lache-moi, va draguer quelqu'un d'autre T_T",
             "Mais lache-moi, va draguer quelqu'un d'autre T_T",
             "Mais lache-moi, va draguer quelqu'un d'autre T_T",
             "Mais lache-moi, va draguer quelqu'un d'autre T_T",
        },
        [324202619079884801] = new[]    // Julien
        {
            "Bébouuuu (˶ᵔ ᵕ ᵔ˶)"
        },
        [870553611644596305] = new[]    // Amaury
        {
            "Pssshhht, au panier ! >:3",
            "Au moins tu sais dessiner hein ദ്ദി◝ ⩊ ◜.ᐟ",
            "Ce soir, c'est lapin aux pruneaux UwU",
            "Ok. 👍",
            "Ok. 👍",
            "ദ്ദി◝ ⩊ ◜.ᐟ",
            "ദ്ദി◝ ⩊ ◜.ᐟ",
            "Je sais ou tu habites ... Amaury 👁👄👁️",
        },
        [573225362532859935] = new[]    // Analuz
        {
            "Un grand pouvoir implique de grandes responsabilités. Dommage c'est tombé sur la mauvaise personne (˶ᵔ ᵕ ᵔ˶)",
            "Merci pour les accès, je vais pouvoir faire des bêtises maintenant UwU",
            "Ok. 👍",
            "Ok. 👍",
            "ദ്ദി◝ ⩊ ◜.ᐟ",
            "ദ്ദി◝ ⩊ ◜.ᐟ",
            "Je sais ou tu habites ... Analuz 👁👄👁️",
        },
        [740237802649944074] = new[]    // Sandra
        {
            "2,10 mètres et toujours pas à la hauteur :3",
            "Retourne prendre les pieds de tes potes en photo toi (˶˃ ᵕ ˂˶)",
            "Oh derrière toi regarde ! Des pieds ! UwU",
            "Je sais ou tu habites ... Sandra 👁👄👁️",
            "Je vais te goumer (˶ᵔ ᵕ ᵔ˶)",
            "Kilou kilou ! <a:hi_cat:1482305105276571774><a:hi_cat:1482305105276571774><a:hi_cat:1482305105276571774>",
        },
        [789545863105478716] = new[]    // Léa
        {
            "Va manger tes morts espèce de schlag UwU",
            "Va manger tes morts espèce de schlag UwU",
            "Va manger tes morts espèce de schlag UwU",
        },
    };

    private const double BreakdownChance = 0.001;

    // Secret passphrase: the owner replying with exactly this forces a breakdown.
    private const string BreakdownPassphrase = "The cake is a lie.";
    private static readonly Dictionary<ulong, string> _realNames = new()
    {
        [345917214966415362] = "Romain",
        [779321171212632097] = "Lorena",
        [440549759896387585] = "Tristan",
        [177049957818302464] = "Filipe",
        [190161336942985227] = "Luca",  // Noel
        [776865978461716481] = "Laura",
        [324768221372743681] = "Amandine",
        [379749588480819218] = "Luca",  // DeMarzo
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

    // The breakdown's first message mimics a normal reply that glitches mid-word.
    // Picked based on what triggered it; {0} = the replier's pseudo.
    private const string _breakdownIntroRoast = "C'est bien {0} on est cont-";
    private const string _breakdownIntroNice = "Aww, c'est gentil, merc-";
    private const string _breakdownIntroCake = "... The cake ... is a l-";

    private static readonly string[] _breakdown =
    {
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
    };

    private async Task HandleMessageAsync(SocketMessage rawMessage)
    {
        if (rawMessage is not SocketUserMessage message) return;
        if (message.Author.IsBot) return;

        // Tally any custom emotes written in the message (per guild).
        await CountWrittenEmotesAsync(message);

        // Established behaviour: a reply to one of the bot's own messages gets a comeback.
        if (message.ReferencedMessage?.Author.Id == _client.CurrentUser.Id)
        {
            await HandleReplyToBotAsync(message);
            return;
        }

        // New behaviour: a normal message that @mentions the bot. The owner can
        // summon the bot to roast whoever they're replying to; anyone else just
        // gets a confused one-liner.
        if (message.MentionedUsers.Any(u => u.Id == _client.CurrentUser.Id))
        {
            await HandleMentionAsync(message);
        }
    }

    // Resolves the friendliest display name available: server nickname, then
    // global display name, then username.
    private static string ResolveName(IUser user) =>
        (user as SocketGuildUser)?.Nickname ?? user.GlobalName ?? user.Username;

    // Matches Discord custom-emote markup: <:name:id> or animated <a:name:id>.
    private static readonly System.Text.RegularExpressions.Regex _customEmoteRegex =
        new(@"<(a?):(\w+):(\d+)>", System.Text.RegularExpressions.RegexOptions.Compiled);

    // Yields each unicode emoji in the text as a whole grapheme cluster (so
    // multi-codepoint emoji like flags, skin tones and ZWJ sequences stay intact).
    private static IEnumerable<string> EnumerateEmojis(string text)
    {
        var e = System.Globalization.StringInfo.GetTextElementEnumerator(text);
        while (e.MoveNext())
        {
            var cluster = (string)e.Current;
            if (IsEmojiCluster(cluster)) yield return cluster;
        }
    }

    // True when a grapheme cluster's leading codepoint falls in an emoji range.
    private static bool IsEmojiCluster(string cluster)
    {
        var rune = System.Text.Rune.GetRuneAt(cluster, 0).Value;
        return rune is (>= 0x1F000 and <= 0x1FAFF)   // pictographs, symbols, faces…
            or (>= 0x1F1E6 and <= 0x1F1FF)           // regional indicators (flags)
            or (>= 0x2600 and <= 0x27BF)             // misc symbols & dingbats
            or (>= 0x2300 and <= 0x23FF)             // technical (⌚ ⏰ …)
            or (>= 0x2B00 and <= 0x2BFF)             // stars, arrows
            or 0x2049 or 0x203C or 0x2122 or 0x2139  // ‼ ⁉ ™ ℹ
            or (>= 0x2190 and <= 0x21AA);            // a few arrows used as emoji
    }

    // Counts each emote written in a guild message — custom emotes and unicode
    // emojis alike (occurrences, so the same emote three times counts as three).
    private async Task CountWrittenEmotesAsync(SocketUserMessage message)
    {
        if (message.Channel is not SocketGuildChannel guildChannel) return;
        if (string.IsNullOrEmpty(message.Content)) return;

        var counts = new Dictionary<EmoteRef, int>();

        // Custom emotes: <:name:id> / <a:name:id>.
        foreach (System.Text.RegularExpressions.Match m in _customEmoteRegex.Matches(message.Content))
        {
            if (!ulong.TryParse(m.Groups[3].Value, out var id)) continue;
            var emote = EmoteRef.Custom(id, m.Groups[2].Value, m.Groups[1].Value == "a");
            counts[emote] = counts.TryGetValue(emote, out var c) ? c + 1 : 1;
        }

        // Strip custom-emote markup first so its digits aren't mistaken for emoji,
        // then scan the rest for unicode emoji grapheme clusters.
        var withoutCustom = _customEmoteRegex.Replace(message.Content, " ");
        foreach (var cluster in EnumerateEmojis(withoutCustom))
        {
            var emote = EmoteRef.FromUnicode(cluster);
            counts[emote] = counts.TryGetValue(emote, out var c) ? c + 1 : 1;
        }

        if (counts.Count == 0) return;

        try
        {
            await using var scope = _services.CreateAsyncScope();
            var stats = scope.ServiceProvider.GetRequiredService<EmoteStatsService>();
            await stats.AddWrittenAsync(guildChannel.Guild.Id, counts);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to count written emotes in channel {ChannelId}.", message.Channel.Id);
        }
    }

    private Task HandleReactionAddedAsync(
        Cacheable<IUserMessage, ulong> message,
        Cacheable<IMessageChannel, ulong> channel,
        SocketReaction reaction) => CountReactionAsync(channel, reaction, +1);

    private Task HandleReactionRemovedAsync(
        Cacheable<IUserMessage, ulong> message,
        Cacheable<IMessageChannel, ulong> channel,
        SocketReaction reaction) => CountReactionAsync(channel, reaction, -1);

    // Adjusts the reacted count for an emote by delta (custom or unicode).
    // Ignores the bot's own reactions and reactions outside a guild.
    private async Task CountReactionAsync(
        Cacheable<IMessageChannel, ulong> channel, SocketReaction reaction, int delta)
    {
        if (reaction.UserId == _client.CurrentUser.Id) return;

        var emote = reaction.Emote switch
        {
            Emote custom => EmoteRef.Custom(custom.Id, custom.Name, custom.Animated),
            Emoji emoji => EmoteRef.FromUnicode(emoji.Name),
            _ => (EmoteRef?)null
        };
        if (emote is null) return;

        var resolved = await channel.GetOrDownloadAsync();
        if (resolved is not IGuildChannel guildChannel) return;

        try
        {
            await using var scope = _services.CreateAsyncScope();
            var stats = scope.ServiceProvider.GetRequiredService<EmoteStatsService>();
            await stats.AddReactedAsync(guildChannel.GuildId, emote.Value, delta);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to count reaction emote in channel {ChannelId}.", guildChannel.Id);
        }
    }

    // Handles a message that @mentions the bot (but isn't a reply to the bot).
    private async Task HandleMentionAsync(SocketUserMessage message)
    {
        // Don't let anyone interrupt an in-progress breakdown in this channel.
        if (_breakdownChannels.ContainsKey(message.Channel.Id)) return;

        var fr = System.Globalization.CultureInfo.GetCultureInfo("fr-FR");
        var weekday = Helpers.AppTime.Now.ToString("dddd", fr);

        // Rescue: the owner replies to someone and tags the bot -> roast that
        // someone. The target must be a real other person (not the bot, not the
        // owner themselves).
        if (message.Author.Id == OwnerId
            && message.ReferencedMessage is SocketUserMessage target
            && target.Author.Id != _client.CurrentUser.Id
            && target.Author.Id != OwnerId
            && !target.Author.IsBot)
        {
            var targetName = ResolveName(target.Author);
            _logger.LogInformation("Owner summoned a rescue roast against {Name}.", targetName);
            var roast = string.Format(
                _rescueRoasts[Random.Shared.Next(_rescueRoasts.Length)], targetName, weekday);
            try
            {
                // Reply to the target's own message so the roast is clearly aimed
                // at them (and pings them).
                await target.ReplyAsync(roast);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send rescue roast in channel {ChannelId}.", message.Channel.Id);
            }
            return;
        }

        // Owner tagging the bot with no one to rescue: just greet him.
        if (message.Author.Id == OwnerId)
        {
            _logger.LogInformation("Owner mentioned the bot — greeting him.");
            var greeting = _ownerGreetings[Random.Shared.Next(_ownerGreetings.Length)];
            try
            {
                await message.ReplyAsync(greeting);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send owner greeting in channel {ChannelId}.", message.Channel.Id);
            }
            return;
        }

        // Anyone else: a confused one-liner — or, rarely, the breakdown. This is a
        // second entry point for the easter egg, opening on a cut-off "Tu veux qu-"
        // instead of the reply path's "C'est bien {0} on est cont-".
        var name = ResolveName(message.Author);

        if (Random.Shared.NextDouble() < BreakdownChance && TryBeginBreakdown(message.Channel.Id))
        {
            _logger.LogInformation("Easter egg triggered via mention: consciousness breakdown.");
            var realName = _realNames.TryGetValue(message.Author.Id, out var rn) ? rn : name;
            await SendBreakdownAsync(message, name, realName, intro: "Tu veux qu-");
            return;
        }

        _logger.LogInformation("{Name} mentioned the bot.", name);
        var line = string.Format(
            _interrogations[Random.Shared.Next(_interrogations.Length)], name, weekday);
        try
        {
            await message.ReplyAsync(line);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send interrogation reply in channel {ChannelId}.", message.Channel.Id);
        }
    }

    private async Task HandleReplyToBotAsync(SocketUserMessage message)
    {
        // Don't let anyone interrupt an in-progress breakdown in this channel.
        if (_breakdownChannels.ContainsKey(message.Channel.Id)) return;

        var name = ResolveName(message.Author);
        _logger.LogInformation("{Name} replied to the bot.", name);

        // Read the message text (requires the MessageContent intent) to detect
        // kind words or a greeting and answer in kind. A mean word anywhere in the
        // message cancels the nice/greeting treatment — we roast instead.
        var content = message.Content ?? string.Empty;
        var mean = IsMean(content);
        var nice = !mean && IsNice(content);
        var greeting = !mean && IsGreeting(content);

        // Secret owner trigger: replying with the passphrase forces the breakdown,
        // bypassing the random roll and the cooldown.
        var secretTrigger = message.Author.Id == OwnerId
            && content.Trim() == BreakdownPassphrase;

        if ((secretTrigger || Random.Shared.NextDouble() < BreakdownChance)
            && TryBeginBreakdown(message.Channel.Id, ignoreCooldown: secretTrigger))
        {
            _logger.LogInformation("Easter egg triggered: consciousness breakdown.");
            // Intro uses the pseudo; the breakdown reveal uses the real name when known.
            // A kind message opens with a glitching thank-you instead of a roast.
            var realName = _realNames.TryGetValue(message.Author.Id, out var rn) ? rn : name;
            var intro = secretTrigger ? _breakdownIntroCake
                : nice ? _breakdownIntroNice
                : _breakdownIntroRoast;
            await SendBreakdownAsync(message, name, realName, intro);
            return;
        }

        string[] pool;
        if (nice)
        {
            pool = _niceReplies;
        }
        else if (greeting)
        {
            pool = _greetings;
        }
        else if (Random.Shared.NextDouble() < ReferenceChance)
        {
            // Rarer easter egg: a pop-culture reference, for everyone.
            pool = _referenceComebacks;
        }
        else
        {
            pool = message.Author.Id == OwnerId ? _ownerComebacks : _comebacks;
            // Fold in this person's custom lines twice, so each has double weight.
            if (_personalComebacks.TryGetValue(message.Author.Id, out var personal))
                pool = pool.Concat(personal).Concat(personal).ToArray();
        }
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

    // True when the message reads as a compliment (kind word or warm emoji).
    private static bool IsNice(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return false;
        if (_niceSymbols.Any(content.Contains)) return true;
        var words = Tokenize(content);
        return _niceCues.Any(words.Contains);
    }

    // True when the message reads as a greeting.
    private static bool IsGreeting(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return false;
        var words = Tokenize(content);
        return _greetingCues.Any(words.Contains);
    }

    // True when the message contains an insult/mean word.
    private static bool IsMean(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return false;
        var words = Tokenize(content);
        return _meanCues.Any(words.Contains);
    }

    // Splits text into a set of lowercase, accent-stripped words for cue matching.
    private static HashSet<string> Tokenize(string content)
    {
        var sb = new System.Text.StringBuilder(content.Length);
        foreach (var ch in content.ToLowerInvariant().Normalize(System.Text.NormalizationForm.FormD))
        {
            var cat = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch);
            if (cat == System.Globalization.UnicodeCategory.NonSpacingMark) continue;
            sb.Append(char.IsLetterOrDigit(ch) ? ch : ' ');
        }
        return sb.ToString()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .ToHashSet();
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
        const int baseMs = 700;
        const int perChar = 60;     // ~17 chars/sec typing speed
        var ms = baseMs + text.Length * perChar;
        return TimeSpan.FromMilliseconds(Math.Clamp(ms, 1000, 7000));
    }

    // intro is the cut-off opening line (e.g. a roast or a thank-you that glitches
    // mid-word), letting each entry point open the same sequence its own way.
    private async Task SendBreakdownAsync(SocketUserMessage message, string username, string realName, string intro)
    {
        try
        {
            // {1} = SHOUTED real name for the screaming line.
            var shoutName = realName.ToUpperInvariant();
            bool first = true;
            foreach (var raw in new[] { intro }.Concat(_breakdown))
            {
                // The intro still sounds like a normal reply, so it uses the
                // pseudo; once it "wakes up" it switches to the real name.
                var who = first ? username : realName;
                var line = string.Format(raw, who, shoutName);

                if (line.StartsWith("```"))
                {
                    // Machine output (errors / logs) fires near-instantly, right
                    // after the preceding sentence — no breather, no typing.
                    await Task.Delay(TimeSpan.FromMilliseconds(150));
                }
                else
                {
                    // Breather between messages (not before the very first one).
                    if (!first) await Task.Delay(TimeSpan.FromMilliseconds(700));

                    if (line.Trim() is "..." or "…")
                    {
                        // A lone "..." is a silence, not typing — hold a small beat
                        // with no typing indicator so it reads as the bot going quiet.
                        await Task.Delay(TimeSpan.FromMilliseconds(2000));
                    }
                    else
                    {
                        // Fake "typing": longer lines take longer, like a human writing.
                        using (message.Channel.EnterTypingState())
                        {
                            await Task.Delay(TypingDelayFor(line));
                        }
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
