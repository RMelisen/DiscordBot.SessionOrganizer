using ProjectSYNCS.Models;
using Microsoft.EntityFrameworkCore;

namespace ProjectSYNCS.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<SessionEvent> SessionEvents => Set<SessionEvent>();
    public DbSet<Participant> Participants => Set<Participant>();

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
    }
}
