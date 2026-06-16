using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using LazyRain.Data;
using LazyRain.Services;
using Microsoft.EntityFrameworkCore;

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
            GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.GuildMembers,
            LogLevel = LogSeverity.Info,
            MessageCacheSize = 100
        };
        services.AddSingleton(socketConfig);
        services.AddSingleton<DiscordSocketClient>();

        var interactionConfig = new InteractionServiceConfig
        {
            UseCompiledLambda = true,
            LogLevel = LogSeverity.Info
        };
        services.AddSingleton(interactionConfig);
        services.AddSingleton<InteractionService>();

        //var dbPath = config["Database:Path"] ?? "ProjectSYNCS.db";
        //services.AddDbContext<AppDbContext>(options =>
        //    options.UseSqlite($"Data Source={dbPath}"),
        //    ServiceLifetime.Transient);

        //services.AddTransient<EventService>();

        //services.AddHostedService<BotService>();
        //services.AddHostedService<ReminderService>();
    })
    .Build();