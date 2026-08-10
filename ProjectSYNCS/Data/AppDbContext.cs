using ProjectSYNCS.Models;
using Microsoft.EntityFrameworkCore;

namespace ProjectSYNCS.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<SessionEvent> SessionEvents => Set<SessionEvent>();
    public DbSet<Participant> Participants => Set<Participant>();
    public DbSet<Poll> Polls => Set<Poll>();
    public DbSet<PollOption> PollOptions => Set<PollOption>();
    public DbSet<PollVote> PollVotes => Set<PollVote>();
    public DbSet<EmoteStat> EmoteStats => Set<EmoteStat>();
    public DbSet<EmoteDailyStat> EmoteDailyStats => Set<EmoteDailyStat>();
    public DbSet<BotFeedback> BotFeedbacks => Set<BotFeedback>();
    public DbSet<BotFeedbackDailyStat> BotFeedbackDailyStats => Set<BotFeedbackDailyStat>();
    public DbSet<MemberXp> MemberXps => Set<MemberXp>();
    public DbSet<GuildSettings> GuildSettings => Set<GuildSettings>();
    public DbSet<GuildExcludedChannel> GuildExcludedChannels => Set<GuildExcludedChannel>();
    public DbSet<MemberDailyStat> MemberDailyStats => Set<MemberDailyStat>();
    public DbSet<Giveaway> Giveaways => Set<Giveaway>();
    public DbSet<GiveawayEntry> GiveawayEntries => Set<GiveawayEntry>();
    public DbSet<ShameRecord> ShameRecords => Set<ShameRecord>();
    public DbSet<ShameDailyStat> ShameDailyStats => Set<ShameDailyStat>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // SQLite stores INTEGER as 64-bit signed; ulong needs explicit conversion
        modelBuilder.Entity<SessionEvent>(e =>
        {
            e.Property(x => x.GuildId).HasConversion<long>();
            e.Property(x => x.ChannelId).HasConversion<long>();
            e.Property(x => x.MessageId).HasConversion<long>();
            e.Property(x => x.OrganizerId).HasConversion<long>();
            e.Property(x => x.NativeEventId).HasConversion<long>();

            e.HasIndex(x => x.GuildId);
            e.HasIndex(x => new { x.ScheduledAt, x.ReminderSent });
        });

        modelBuilder.Entity<Participant>(e =>
        {
            e.Property(x => x.UserId).HasConversion<long>();
            e.HasIndex(x => new { x.SessionEventId, x.UserId }).IsUnique();
        });

        modelBuilder.Entity<Poll>(e =>
        {
            e.Property(x => x.GuildId).HasConversion<long>();
            e.Property(x => x.ChannelId).HasConversion<long>();
            e.Property(x => x.MessageId).HasConversion<long>();
            e.Property(x => x.OrganizerId).HasConversion<long>();

            e.HasIndex(x => x.GuildId);
        });

        modelBuilder.Entity<PollVote>(e =>
        {
            e.Property(x => x.UserId).HasConversion<long>();
            // One vote row per (slot, user); toggling adds or removes it.
            e.HasIndex(x => new { x.PollOptionId, x.UserId }).IsUnique();
        });

        modelBuilder.Entity<EmoteStat>(e =>
        {
            e.Property(x => x.GuildId).HasConversion<long>();
            e.Property(x => x.EmoteId).HasConversion<long>();
            // One row per (guild, emote); custom emotes key on EmoteId, unicode
            // emojis on the Unicode string. Counts are scoped to the guild.
            e.HasIndex(x => new { x.GuildId, x.EmoteId, x.Unicode }).IsUnique();
        });

        modelBuilder.Entity<EmoteDailyStat>(e =>
        {
            e.Property(x => x.GuildId).HasConversion<long>();
            e.Property(x => x.EmoteId).HasConversion<long>();
            // One row per (guild, emote, day) — the upsert key.
            e.HasIndex(x => new { x.GuildId, x.EmoteId, x.Unicode, x.Day }).IsUnique();
            // Serves the rolling-window read: "this guild, since day N". Day is an
            // int precisely so this range can be evaluated in SQL.
            e.HasIndex(x => new { x.GuildId, x.Day });
        });

        modelBuilder.Entity<BotFeedback>(e =>
        {
            e.Property(x => x.GuildId).HasConversion<long>();
            e.Property(x => x.UserId).HasConversion<long>();
            // One row per (guild, user); the tally is scoped to the guild it was
            // earned in, like the emote counts.
            e.HasIndex(x => new { x.GuildId, x.UserId }).IsUnique();
        });

        modelBuilder.Entity<BotFeedbackDailyStat>(e =>
        {
            e.Property(x => x.GuildId).HasConversion<long>();
            e.Property(x => x.UserId).HasConversion<long>();
            // One row per (guild, user, day) — the upsert key.
            e.HasIndex(x => new { x.GuildId, x.UserId, x.Day }).IsUnique();
            // Serves the rolling-window read: "this guild, since day N". Day is an
            // int precisely so this range can be evaluated in SQL.
            e.HasIndex(x => new { x.GuildId, x.Day });
        });

        modelBuilder.Entity<MemberXp>(e =>
        {
            e.Property(x => x.GuildId).HasConversion<long>();
            e.Property(x => x.UserId).HasConversion<long>();
            // One row per (guild, user); the all-time totals. The dated sibling is
            // MemberDailyStat below — see MemberXp.cs for why they are two tables.
            e.HasIndex(x => new { x.GuildId, x.UserId }).IsUnique();
        });

        modelBuilder.Entity<MemberDailyStat>(e =>
        {
            e.Property(x => x.GuildId).HasConversion<long>();
            e.Property(x => x.UserId).HasConversion<long>();
            // One row per (guild, user, day) — the upsert key.
            e.HasIndex(x => new { x.GuildId, x.UserId, x.Day }).IsUnique();
            // Serves the rolling-window read: "this guild, since day N". Day is an
            // int precisely so this range can be evaluated in SQL.
            e.HasIndex(x => new { x.GuildId, x.Day });
        });

        modelBuilder.Entity<Giveaway>(e =>
        {
            e.Property(x => x.GuildId).HasConversion<long>();
            e.Property(x => x.ChannelId).HasConversion<long>();
            e.Property(x => x.MessageId).HasConversion<long>();
            e.Property(x => x.OrganizerId).HasConversion<long>();

            e.HasIndex(x => x.GuildId);
            // The sweep's read: the open ones. EndsAt is deliberately not indexed —
            // SQLite cannot compare a DateTimeOffset in a query anyway, so the cutoff
            // is applied in memory and an index on it would never be used.
            e.HasIndex(x => x.IsClosed);
        });

        modelBuilder.Entity<GiveawayEntry>(e =>
        {
            e.Property(x => x.UserId).HasConversion<long>();
            // One entry row per (giveaway, person); the button toggles it.
            e.HasIndex(x => new { x.GiveawayId, x.UserId }).IsUnique();
        });

        modelBuilder.Entity<ShameRecord>(e =>
        {
            e.Property(x => x.GuildId).HasConversion<long>();
            e.Property(x => x.UserId).HasConversion<long>();
            // One row per (guild, user); the all-time totals plus the voter's daily
            // flag. The dated sibling is ShameDailyStat below.
            e.HasIndex(x => new { x.GuildId, x.UserId }).IsUnique();
        });

        modelBuilder.Entity<ShameDailyStat>(e =>
        {
            e.Property(x => x.GuildId).HasConversion<long>();
            e.Property(x => x.UserId).HasConversion<long>();
            // One row per (guild, user, day) — the upsert key.
            e.HasIndex(x => new { x.GuildId, x.UserId, x.Day }).IsUnique();
            // Serves the rolling-window read: "this guild, since day N". Day is an
            // int precisely so this range can be evaluated in SQL.
            e.HasIndex(x => new { x.GuildId, x.Day });
        });

        modelBuilder.Entity<GuildSettings>(e =>
        {
            e.Property(x => x.GuildId).HasConversion<long>();
            e.Property(x => x.ModeratorRoleId).HasConversion<long>();
            // At most one settings row per guild.
            e.HasIndex(x => x.GuildId).IsUnique();
        });

        modelBuilder.Entity<GuildExcludedChannel>(e =>
        {
            e.Property(x => x.GuildId).HasConversion<long>();
            e.Property(x => x.ChannelId).HasConversion<long>();
            // Adding the same channel twice is a no-op, not a second row.
            e.HasIndex(x => new { x.GuildId, x.ChannelId }).IsUnique();
            // Serves the only read there is: every excluded channel for one guild,
            // loaded once and then cached (see GuildConfigService).
            e.HasIndex(x => x.GuildId);
        });
    }
}
