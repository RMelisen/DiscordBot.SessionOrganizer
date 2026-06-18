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
        services.AddSingleton<InteractionService>(sp =>
            new InteractionService(sp.GetRequiredService<DiscordSocketClient>(), interactionConfig));

        var dbPath = config["Database:Path"] ?? "ProjectSYNCS.db";
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite($"Data Source={dbPath}"),
            ServiceLifetime.Transient);

        services.AddTransient<EventService>();
        services.AddTransient<PollService>();

        services.AddHostedService<BotService>();
        services.AddHostedService<ReminderService>();
    })
    .Build();

    using (var scope = host.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }

    await host.RunAsync();