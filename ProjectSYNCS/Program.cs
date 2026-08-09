using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using ProjectSYNCS.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ProjectSYNCS.Services;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((ctx, config) =>
    {
        config.AddJsonFile("appsettings.json", optional: false);
        // Optional, overrides overlapping keys in appsettings.json
        config.AddJsonFile($"appsettings.{ctx.HostingEnvironment.EnvironmentName}.json", optional: true);
        config.AddEnvironmentVariables();
    })
    .ConfigureServices((ctx, services) =>
    {
        var config = ctx.Configuration;

        var socketConfig = new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.GuildMembers | GatewayIntents.MessageContent,
            LogLevel = LogSeverity.Info,
            MessageCacheSize = 100,
            // Downloads the full member list on Ready instead of caching only the people
            // seen in events since startup. The GuildMembers intent above is the
            // prerequisite; without this flag it buys nothing on its own.
            //
            // Load-bearing for the avatars on /level, /leaderboard and /shame.
            // Guild.GetUser reads this cache and returns null for anyone not in it, and
            // Helpers/AvatarUi then falls back to Discord's generic blue logo — so every
            // ranking of *historic* totals was showing that placeholder for exactly the
            // people who had not spoken since the last restart, which on an all-time
            // board is most of them. Cheap here (one small private guild); it would be a
            // real startup and memory cost on a large one.
            AlwaysDownloadUsers = true
        };
        services.AddSingleton(socketConfig);
        services.AddSingleton<DiscordSocketClient>();

        var interactionConfig = new InteractionServiceConfig
        {
            UseCompiledLambda = true,
            LogLevel = LogSeverity.Info
        };
        services.AddSingleton(interactionConfig);
        services.AddSingleton<InteractionService>(sp =>
            new InteractionService(sp.GetRequiredService<DiscordSocketClient>(), interactionConfig));

        var dbPath = config["Database:Path"] ?? "ProjectSYNCS.db";
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite($"Data Source={dbPath}"),
            ServiceLifetime.Transient);

        services.AddTransient<EventService>();
        services.AddTransient<PollService>();
        services.AddTransient<EmoteStatsService>();
        services.AddTransient<BotFeedbackService>();
        services.AddTransient<XpService>();
        services.AddTransient<GiveawayService>();
        services.AddTransient<ShameService>();

        // Personality / chat behaviour collaborators (singletons: they hold
        // in-memory state like the breakdown cooldown).
        services.AddSingleton<BreakdownService>();
        services.AddSingleton<AvailabilityService>();
        services.AddSingleton<ResponsePicker>();
        services.AddSingleton<ChatterService>();
        services.AddSingleton<EmoteTracker>();
        services.AddSingleton<ReactionService>();
        // Registered before BotFeedbackTracker, which asks it who acted last.
        services.AddSingleton<RivalryService>();
        // Before BotFeedbackTracker, which calls into it after recording a verdict.
        services.AddSingleton<XpTracker>();
        services.AddSingleton<BotFeedbackTracker>();
        // Independent of the trackers above: reads only the raw message and writes its
        // own counters, so its registration order is not load-bearing.
        services.AddSingleton<ShameTracker>();

        services.AddHostedService<BotService>();
        services.AddHostedService<ReminderService>();
        services.AddHostedService<PresenceService>();
        services.AddHostedService<VoiceXpService>();
        services.AddHostedService<GiveawayDrawService>();
    })
    .Build();

    using (var scope = host.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }

    await host.RunAsync();