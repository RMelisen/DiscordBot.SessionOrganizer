using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using ProjectSYNCS.Data;
using ProjectSYNCS.Helpers;
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
            // Helpers/Core/AvatarUi then falls back to Discord's generic blue logo — so every
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
        services.AddTransient<MemberBadgeService>();
        services.AddTransient<GiveawayService>();
        services.AddTransient<ShameService>();
        services.AddTransient<PebbleService>();
        services.AddTransient<PlynlingService>();
        services.AddTransient<InventoryService>();
        services.AddTransient<CosmeticService>();
        services.AddTransient<AdminStatsService>();
        services.AddTransient<WarningService>();
        services.AddTransient<EconomyDashboardService>();
        services.AddTransient<QuizService>();
        services.AddTransient<VoiceCompanionService>();
        services.AddTransient<PlynlingCareService>();
        services.AddTransient<PlynlingVisitRunner>();
        services.AddTransient<TonightService>();
        // Brings a card back to the bottom of a channel: the /… list menus and a reply asking her to.
        services.AddTransient<CardRepost>();
        services.AddTransient<PersonalReminderStore>();
        services.AddTransient<MinecraftDataService>();
        services.AddTransient<ArcadeService>();
        services.AddTransient<RecapService>();
        services.AddTransient<SchedulingReportService>();
        services.AddTransient<CommunityReportService>();
        services.AddTransient<SyncsReportService>();
        services.AddTransient<ActivityReportService>();
        services.AddTransient<QuizReportService>();
        services.AddTransient<PlynlingReportService>();
        services.AddTransient<EmoteReportService>();
        services.AddTransient<ReportService>();
        // The /arcade games are in memory (the tables) and run by timers (the runner): singletons.
        services.AddSingleton<ArcadeTables>();
        services.AddSingleton<ArcadeRunner>();
        services.AddSingleton<PartyRunner>();
        services.AddSingleton<PlynlingCooldowns>();
        services.AddSingleton<TradeOffers>();
        services.AddSingleton<VisitStories>();
        services.AddSingleton<PlynlingPlayService>();
        services.AddSingleton<PlynlingAnnouncer>();
        services.AddSingleton<PlynlingShortcutService>();

        // Singleton despite reading the database, unlike every other service here that
        // does: it caches per-guild config, which a transient would drop on every
        // resolve. Takes IServiceProvider and scopes per unit of work, like the
        // trackers. Registered before XpTracker, which holds it.
        services.AddSingleton<GuildConfigService>();
        // The mirrored roles. RoleMirror holds the pass lock and the logged-once set; the two
        // owners say who should hold each role.
        services.AddSingleton<RoleMirror>();
        services.AddSingleton<PlynlingRoleService>();

        // Personality / chat behaviour collaborators (singletons: they hold
        // in-memory state like the breakdown cooldown).
        services.AddSingleton<BreakdownService>();
        services.AddSingleton<AvailabilityService>();
        services.AddSingleton<ResponsePicker>();
        // The glitch easter egg's daily cap is in-memory: a singleton, like the picker.
        services.AddSingleton<GlitchService>();
        // The sticker follow-ups: one instance, reached from BotChat's static hook.
        services.AddSingleton<StickerService>();
        // What her own recent posts were, so a reply to one is answered about it: in memory.
        services.AddSingleton<HerMessages>();
        // Who is going back and forth with her: shared by ChatterService and RobotHeartService.
        services.AddSingleton<ConversationStreak>();
        // Where she was told « chut », and until when: shared by every service that chats in a channel.
        services.AddSingleton<ChatHush>();
        // Who she is still listening to after a call or an answer, so « syncs ? » then a question works.
        services.AddSingleton<ChatListening>();
        // /debug answers: the counts and the messages she couldn't answer specifically (30 rolling
        // days) are both in the database, behind one transient service.
        services.AddTransient<ChatAnswerService>();
        // In-memory daily gate / per-channel stretches: singletons.
        services.AddSingleton<SynthiaService>();
        // The robot heart's daily gate is in-memory: a singleton.
        services.AddSingleton<RobotHeartService>();
        // Open /wouldyourather rounds and their votes are in memory: a singleton.
        services.AddSingleton<WouldYouRatherService>();
        // The roll call and the voice seat keep in-memory clocks; both are fed by VoiceXpService's
        // sweep, and the seat reads the roll call's sessions (live-session tie-break).
        services.AddSingleton<SessionAttendanceService>();
        services.AddSingleton<VoiceSeatService>();
        services.AddSingleton<VoiceSpectatorService>();
        // In-memory hourly window against raids: a singleton.
        services.AddSingleton<WelcomeService>();
        // Gateway-driven, opens its own scope per join or leave.
        services.AddSingleton<MemberFlowTracker>();
        // Counts messages, voice presence and commands in memory; ActivityFlushService writes them.
        // Registered before BotService and VoiceXpService on purpose: hosts stop in reverse order,
        // so its flush on stop runs after everything that feeds the counter has stopped.
        services.AddSingleton<ActivityCounter>();
        services.AddHostedService<ActivityFlushService>();
        // Replies under her cards (« votez », « j'ai voté », « remonte-le »): its gates are in memory.
        services.AddSingleton<CardReplyService>();
        // Every member-badge write goes through it, fire and forget; its claim set is in memory.
        services.AddSingleton<BadgeAwarder>();
        services.AddSingleton<ChatterService>();
        services.AddSingleton<EmoteTracker>();
        services.AddSingleton<ReactionService>();
        // Registered before BotFeedbackTracker, which asks it who acted last.
        services.AddSingleton<RivalryService>();
        // Before BotFeedbackTracker, which calls into it after recording a verdict.
        services.AddSingleton<XpTracker>();
        services.AddSingleton<BotFeedbackTracker>();
        // After XpTracker, which it asks whether a channel is excluded.
        services.AddSingleton<ShameTracker>();

        services.AddHostedService<BotService>();
        // One instance for both roles: the host runs its 2-minute loop, AmbientService,
        // PresenceService and /health read what it knows about the Pi.
        services.AddSingleton<PiHealthService>();
        services.AddHostedService(sp => sp.GetRequiredService<PiHealthService>());
        services.AddHostedService<ReminderService>();
        // /remindme's delivery: its own 1-minute loop.
        services.AddHostedService<PersonalReminderService>();
        services.AddHostedService<PresenceService>();
        // One instance for both roles: the host runs its loop, BotService feeds it
        // messages so a greeting can bring the hello forward.
        services.AddSingleton<MorningGreetingService>();
        services.AddHostedService(sp => sp.GetRequiredService<MorningGreetingService>());
        // A singleton too: /debug digest sends one on demand.
        services.AddSingleton<MonthlyDigestService>();
        services.AddHostedService(sp => sp.GetRequiredService<MonthlyDigestService>());
        // Same shape: BotService feeds it the main channel's messages, the host runs its loop.
        services.AddSingleton<AmbientService>();
        services.AddHostedService(sp => sp.GetRequiredService<AmbientService>());
        services.AddHostedService<VoiceXpService>();
        services.AddHostedService<GiveawayDrawService>();
        services.AddHostedService<WeekendPollService>();
        // Same shape: the host runs its 1-minute loop, BotService feeds it messages (typed
        // answers, the quiet clock), the quiz buttons and /debug quiz call into it.
        services.AddSingleton<QuizCrownService>();
        services.AddSingleton<QuizMasterService>();
        services.AddHostedService(sp => sp.GetRequiredService<QuizMasterService>());
        // Same shape: the host runs its hourly loop, /debug sweep runs a pass on demand.
        services.AddSingleton<PlynlingSweepService>();
        services.AddHostedService(sp => sp.GetRequiredService<PlynlingSweepService>());
        services.AddHostedService<ApplicationEmojiService>();
        services.AddHostedService<PlynlingMascotService>();
        services.AddHostedService<SeasonalAvatarService>();
        // Quokka's visits. One instance for both roles: the host runs its slots, BotService feeds it
        // messages (the room-is-awake clock), /debug quokka spawns one on demand.
        services.AddSingleton<QuokkaRaidService>();
        services.AddHostedService(sp => sp.GetRequiredService<QuokkaRaidService>());
        // Her body on the Minecraft server. Same shape: the host runs its 1-minute loop, /debug
        // minecraft reads its state and forces a join or a leave. Off without Minecraft:Host.
        services.AddSingleton<MinecraftBodyService>();
        services.AddHostedService(sp => sp.GetRequiredService<MinecraftBodyService>());
    })
    .Build();

    // BotChat is static; the one thing it needs from DI is the sticker gate.
    BotChat.FollowUp = host.Services.GetRequiredService<StickerService>().TryFollowUp;

    using (var scope = host.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }

    await host.RunAsync();