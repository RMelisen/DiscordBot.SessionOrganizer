using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ProjectSYNCS.Services;

/// <summary>
/// The score's one-off rebuild: every Plynling that existed when the score shipped gets its history counted
/// (PlynlingService.RebuildScoreAsync). Run from Program.cs right after the migrations, before the bot
/// connects, so no live action can land between a row's history and its rebuild and be counted twice.
/// </summary>
/// <remarks>
/// Not a data migration: the rebuild needs the badge catalog, the stage ages, the RPG curve and the event
/// catalog, which live in C#. Idempotent through Plynling.ScoreRebuilt; each Plynling is its own unit in its
/// own scope and its own try, so one failure is logged and retried at the next start, never blocks the bot.
/// The mascot is rebuilt like anyone (her owner id is bound only on Ready): harmless, since she is never
/// ranked and cannot die.
/// </remarks>
public static class PlynlingScoreRebuild
{
    public static async Task RunAsync(IServiceProvider services, ILogger logger, DateTimeOffset now)
    {
        List<int> ids;
        using (var scope = services.CreateScope())
            ids = await scope.ServiceProvider.GetRequiredService<PlynlingService>().GetScoreRebuildIdsAsync();
        if (ids.Count == 0) return;

        long total = 0;
        var done = 0;
        foreach (var id in ids)
        {
            try
            {
                using var scope = services.CreateScope();
                var points = await scope.ServiceProvider.GetRequiredService<PlynlingService>().RebuildScoreAsync(id, now);
                if (points is { } p)
                {
                    total += p;
                    done++;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Plynling score rebuild failed for Plynling {Id}; retried at the next start", id);
            }
        }
        logger.LogInformation("Plynling score rebuilt for {Done}/{Count} Plynling(s), {Total} point(s) in all", done, ids.Count, total);
    }
}
