using Discord;
using Discord.Interactions;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Commands;

public class EmoteStatsModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly EmoteStatsService _stats;

    public EmoteStatsModule(EmoteStatsService stats)
    {
        _stats = stats;
    }

    [SlashCommand("emotestats", "Classement des emotes les plus utilisées du serveur")]
    public async Task EmoteStatsAsync()
    {
        await DeferAsync();

        var top = await _stats.GetTopAsync(Context.Guild.Id, 15);
        if (top.Count == 0)
        {
            await FollowupAsync("Aucune emote comptabilisée pour le moment. (˶ᵔ ᵕ ᵔ˶)");
            return;
        }

        var lines = top.Select((s, i) =>
        {
            var markup = s.EmoteId != 0
                ? (s.IsAnimated ? $"<a:{s.Name}:{s.EmoteId}>" : $"<:{s.Name}:{s.EmoteId}>")
                : s.Unicode;
            return $"**{i + 1}.** {markup} — ✍️ {s.WrittenCount}  👍 {s.ReactedCount}";
        });

        var embed = new EmbedBuilder()
            .WithTitle("Emotes les plus utilisées")
            .WithDescription(string.Join("\n", lines))
            .WithColor(Color.Gold)
            .WithFooter("✍️ écrites · 👍 en réaction")
            .Build();

        await FollowupAsync(embed: embed);
    }
}
