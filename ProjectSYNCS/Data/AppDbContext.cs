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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // SQLite stores INTEGER as 64-bit signed; ulong needs explicit conversion
        modelBuilder.Entity<SessionEvent>(e =>
        {
            e.Property(x => x.GuildId).HasConversion<long>();
            e.Property(x => x.ChannelId).HasConversion<long>();
            e.Property(x => x.MessageId).HasConversion<long>();
            e.Property(x => x.OrganizerId).HasConversion<long>();

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
    }
}
