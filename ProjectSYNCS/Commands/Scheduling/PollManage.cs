using Discord;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;
using Poll = ProjectSYNCS.Models.Poll;

namespace ProjectSYNCS.Commands;

// The ephemeral « ⚙️ Gérer » panel of a poll or vote card: close it, push its deadline, rename it,
// add or remove an option, bring the card back down. Organiser and staff only. Every verb is its
// own (root CLAUDE.md, custom-ids).
public static class PollManage
{
    // A vote needs two options to mean anything; a date poll, one slot.
    public static int MinOptions(Poll poll) => poll.Kind == PollKind.Text ? 2 : 1;

    public static string Panel(Poll poll, string? note = null)
    {
        var kind = poll.Kind == PollKind.Text ? "Vote" : "Sondage";
        var head = $"**⚙️ Gérer « {Format.Sanitize(poll.Title)} »** (#{poll.Id})\n"
                   + (poll.IsClosed ? $"-# {kind} clôturé." : $"-# {kind} · fin <t:{poll.ClosesAt.ToUnixTimeSeconds()}:R>");
        return note is null ? head : $"{note}\n\n{head}";
    }

    public static MessageComponent PanelComponents(Poll poll)
    {
        int id = poll.Id;
        var builder = new ComponentBuilder();
        if (poll.IsClosed) return builder.Build();

        builder
            .WithButton("Clôturer", $"poll:close:{id}", ButtonStyle.Danger, new Emoji("🔒"), row: 0)
            .WithButton("Renommer", $"poll:rename:{id}", ButtonStyle.Secondary, new Emoji("✏️"), row: 0)
            .WithButton("Republier ici", $"poll:repost:{id}", ButtonStyle.Secondary, new Emoji("📌"), row: 0)
            .WithButton("+1 h", $"poll:extend:{id}:1", ButtonStyle.Secondary, row: 1)
            .WithButton("+1 jour", $"poll:extend:{id}:24", ButtonStyle.Secondary, row: 1)
            .WithButton("+2 jours", $"poll:extend:{id}:48", ButtonStyle.Secondary, row: 1);
        if (poll.Options.Count < PollRules.MaxOptions)
            builder.WithButton(poll.Kind == PollKind.Text ? "Ajouter une option" : "Ajouter un créneau",
                $"poll:addopt:{id}", ButtonStyle.Secondary, new Emoji("➕"), row: 0);

        if (poll.Options.Count > MinOptions(poll))
        {
            var menu = new SelectMenuBuilder()
                .WithCustomId($"poll:rmopt:{id}")
                .WithPlaceholder(poll.Kind == PollKind.Text ? "Retirer une option" : "Retirer un créneau");
            foreach (var o in PollRules.Ordered(poll))
            {
                var label = PollCards.ButtonLabel(poll, o);
                menu.AddOption(label, o.Id.ToString(),
                    o.Votes.Count == 0 ? "aucun vote" : $"{o.Votes.Count} vote(s)", new Emoji("➖"));
            }
            builder.WithSelectMenu(menu, row: 2);
        }
        return builder.Build();
    }

    public static MessageComponent RemoveConfirm(int pollId, int optionId) =>
        new ComponentBuilder()
            .WithButton("Oui, retirer", $"poll:rmoptyes:{pollId}:{optionId}", ButtonStyle.Danger)
            .WithButton("Non", $"poll:rmoptno:{pollId}", ButtonStyle.Secondary)
            .Build();
}
