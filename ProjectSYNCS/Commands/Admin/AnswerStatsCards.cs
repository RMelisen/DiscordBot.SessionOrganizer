using System.Text;
using Discord;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Commands;

/// <summary>
/// What `/debug answers` renders: her answer counts by family, and the messages she found nothing
/// specific to say to. Static and Context-free, so the embed can be measured against Discord's caps
/// (6000 per message, 1024 per field) without a gateway.
/// </summary>
internal static class AnswerStatsCards
{
    // Under 1024 per field, and under 6000 for six fields plus the title and description.
    private const int MaxFieldLength = 900;

    // The plain answers that aren't intents, contexts, facts or streaks.
    private static readonly HashSet<string> MoodKinds = new()
    {
        AnswerKinds.Nice, AnswerKinds.Greeting, AnswerKinds.Reference, AnswerKinds.Tata, AnswerKinds.Comeback,
        AnswerKinds.PapaComeback, AnswerKinds.PapaGreeting, AnswerKinds.Interrogation, AnswerKinds.TalkedAbout,
        AnswerKinds.PapaTalkedAbout, AnswerKinds.Backbite, AnswerKinds.PapaRaspberry, AnswerKinds.PapaChild,
    };

    public static string PeriodLabel(StatsPeriod period) => period switch
    {
        StatsPeriod.Week => "7 derniers jours",
        StatsPeriod.Month => "30 derniers jours",
        _ => "depuis toujours",
    };

    public static Embed BuildTypes(IReadOnlyList<ChatAnswerCount> counts, StatsPeriod period)
    {
        var embed = new EmbedBuilder()
            .WithTitle($"Ce qu'on me dit — {PeriodLabel(period)}")
            .WithColor(Color.Purple);

        var total = counts.Sum(c => c.Count);
        if (total == 0)
            return embed.WithDescription("Rien compté sur cette période.").Build();

        embed.WithDescription($"**{total}** réponses. Par ligne : total (r = réponse à un de mes messages · m = mention · n = mon nom).");

        AddFamily(embed, "Intentions", counts.Where(c => c.Kind.StartsWith("intent:")), "intent:");
        AddFamily(embed, "Réponse à mes messages", counts.Where(c => c.Kind.StartsWith("context:")), "context:");
        AddFamily(embed, "Sous mes cartes", counts.Where(c => c.Kind.StartsWith("card:")), "card:");
        AddFamily(embed, "Humeur et réponses par défaut", counts.Where(c => MoodKinds.Contains(c.Kind)), "");
        AddFamily(embed, "Faits et séries", counts.Where(c => c.Kind.StartsWith("fact:") || c.Kind.StartsWith("streak:")), "");
        AddFamily(embed, "Spéciaux", counts.Where(c => !c.Kind.StartsWith("intent:") && !c.Kind.StartsWith("context:")
            && !c.Kind.StartsWith("card:") && !c.Kind.StartsWith("fact:") && !c.Kind.StartsWith("streak:")
            && !MoodKinds.Contains(c.Kind)), "");

        embed.AddField("Pistes", Cap(Leads(counts)));
        return embed.Build();
    }

    // One field per family: a row per kind, the most frequent first, split by path.
    private static void AddFamily(EmbedBuilder embed, string title, IEnumerable<ChatAnswerCount> counts, string prefix)
    {
        var rows = counts
            .GroupBy(c => c.Kind)
            .Select(g => (Kind: g.Key, Total: g.Sum(c => c.Count),
                Reply: Sum(g, ChatPath.Reply), Mention: Sum(g, ChatPath.Mention), Name: Sum(g, ChatPath.Name)))
            .OrderByDescending(r => r.Total)
            .ToList();
        if (rows.Count == 0) return;

        var lines = rows.Select(r =>
            $"`{r.Kind[prefix.Length..]}` — **{r.Total}** (r {r.Reply} · m {r.Mention} · n {r.Name})");
        embed.AddField(title, Cap(string.Join('\n', lines)));
    }

    private static long Sum(IEnumerable<ChatAnswerCount> counts, ChatPath path) =>
        counts.Where(c => c.Path == AnswerKinds.Key(path)).Sum(c => c.Count);

    // What to work on: how often each path ended on no specific answer, and the intents that never
    // fired (cues too narrow, or nobody says it).
    private static string Leads(IReadOnlyList<ChatAnswerCount> counts)
    {
        var sb = new StringBuilder();
        foreach (var path in Enum.GetValues<ChatPath>())
        {
            var onPath = counts.Where(c => c.Path == AnswerKinds.Key(path)).ToList();
            var all = onPath.Sum(c => c.Count);
            if (all == 0) continue;
            var unrecognised = onPath.Where(c => AnswerKinds.IsUnrecognised(c.Kind)).Sum(c => c.Count);
            sb.AppendLine($"{PathLabel(path)} : **{100 * unrecognised / all} %** sans réponse précise ({unrecognised}/{all})");
        }

        var hit = counts.Select(c => c.Kind).ToHashSet();
        var never = Enum.GetValues<ChatIntent>()
            .Where(i => i != ChatIntent.None && !hit.Contains(AnswerKinds.Intent(i)))
            .Select(i => $"`{i}`")
            .ToList();
        sb.Append(never.Count == 0
            ? "Toutes les intentions ont servi."
            : $"Jamais déclenchées : {string.Join(", ", never)}");
        sb.Append("\nLes messages eux-mêmes : `view: Exemples`.");
        return sb.ToString();
    }

    private static string PathLabel(ChatPath path) => path switch
    {
        ChatPath.Reply => "Réponses",
        ChatPath.Mention => "Mentions",
        _ => "Mon nom",
    };

    // Cut on a line break when there is one, so no row is shown half.
    private static string Cap(string text)
    {
        if (text.Length <= MaxFieldLength) return text;
        var cut = text.LastIndexOf('\n', MaxFieldLength - 2);
        return (cut > 0 ? text[..cut] : text[..(MaxFieldLength - 2)]) + "\n…";
    }

    /// <summary>The unanswered messages as a text file, newest first, one per line, no author.</summary>
    public static string BuildExamples(IReadOnlyList<UnansweredMessage> messages, int days)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Les {messages.Count} messages sans réponse précise des {days} derniers jours (sans auteur).");
        sb.AppendLine("Date · chemin · réponse donnée — message");
        sb.AppendLine();
        foreach (var m in messages)
            sb.AppendLine($"[{AppTime.ToZoned(m.At):dd/MM HH:mm}] {m.Path} · {m.Kind} — « {m.Text} »");
        return sb.ToString();
    }
}
