using Microsoft.EntityFrameworkCore;
using ProjectSYNCS.Data;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Services;

// EF access for /remindme — transient. PersonalReminderService (the loop) opens a scope around it.
public class PersonalReminderStore
{
    // Per person, so nobody turns her into a spam cannon.
    public const int MaxPending = 15;

    private readonly AppDbContext _db_context;

    public PersonalReminderStore(AppDbContext db_context)
    {
        _db_context = db_context;
    }

    public async Task<int> CountPendingAsync(ulong userId) =>
        await _db_context.PersonalReminders.CountAsync(r => r.UserId == userId && !r.Sent);

    public async Task<PersonalReminder> AddAsync(ulong guildId, ulong channelId, ulong userId, string text, DateTimeOffset dueAt)
    {
        var reminder = new PersonalReminder
        {
            GuildId = guildId,
            ChannelId = channelId,
            UserId = userId,
            Text = text,
            DueAt = dueAt,
        };
        _db_context.PersonalReminders.Add(reminder);
        await _db_context.SaveChangesAsync();
        return reminder;
    }

    // Soonest first.
    public async Task<List<PersonalReminder>> GetPendingAsync(ulong userId)
    {
        var rows = await _db_context.PersonalReminders.AsNoTracking()
            .Where(r => r.UserId == userId && !r.Sent)
            .ToListAsync();
        return rows.OrderBy(r => r.DueAt).ToList();
    }

    // Only its owner can cancel it. Returns whether something was deleted.
    public async Task<bool> CancelAsync(int id, ulong userId) =>
        await _db_context.PersonalReminders
            .Where(r => r.Id == id && r.UserId == userId && !r.Sent)
            .ExecuteDeleteAsync() == 1;

    public async Task<List<PersonalReminder>> GetDueAsync(DateTimeOffset now)
    {
        var rows = await _db_context.PersonalReminders.AsNoTracking()
            .Where(r => !r.Sent)
            .ToListAsync();
        return rows.Where(r => r.DueAt <= now).OrderBy(r => r.DueAt).ToList();
    }

    // The once-only claim before sending: true for the caller who flipped it.
    public async Task<bool> ClaimSendAsync(int id) =>
        await _db_context.PersonalReminders
            .Where(r => r.Id == id && !r.Sent)
            .ExecuteUpdateAsync(u => u.SetProperty(r => r.Sent, true)) == 1;

    // A delivered reminder has nothing left to say: the row goes. (One whose send crashed stays
    // flagged, harmlessly, and never goes out twice.)
    public async Task DeleteAsync(int id) =>
        await _db_context.PersonalReminders.Where(r => r.Id == id).ExecuteDeleteAsync();
}
