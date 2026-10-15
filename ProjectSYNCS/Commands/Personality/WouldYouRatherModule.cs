using Discord;
using Discord.Interactions;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Commands;

// /wouldyourather: a two-button dilemma, hers (WouldYouRatherDilemmas) or the asker's own. Votes
// stay hidden for WouldYouRatherService.Window, then the card turns into the result: the split,
// who chose what, her comment and her own pick. Guild-only: the round lives in a channel and
// closes by editing its card.
[CommandContextType(InteractionContextType.Guild)]
public class WouldYouRatherModule : InteractionModuleBase<SocketInteractionContext>
{
    private const string HalfRefusal =
        "Un dilemme, c'est deux options. Donne-moi `a` **et** `b`, ou aucune des deux et je choisis.";

    // How many voter mentions a result field lists before « +N »: a field value caps at 1024.
    private const int MaxNames = 30;

    private readonly WouldYouRatherService _rounds;
    private readonly ResponsePicker _picker;

    public WouldYouRatherModule(WouldYouRatherService rounds, ResponsePicker picker)
    {
        _rounds = rounds;
        _picker = picker;
    }

    [SlashCommand("wouldyourather", "Tu préfères… ? Un dilemme, deux boutons, résultats dans 10 minutes")]
    public async Task WouldYouRatherAsync(
        [Summary("a", "Ta première option (sinon, je choisis le dilemme)")]
        [MaxLength(InputCaps.WyrOption)]
        string? a = null,
        [Summary("b", "Ta deuxième option")]
        [MaxLength(InputCaps.WyrOption)]
        string? b = null)
    {
        var hasA = !string.IsNullOrWhiteSpace(a);
        var hasB = !string.IsNullOrWhiteSpace(b);
        if (hasA != hasB)
        {
            await RespondAsync(HalfRefusal, ephemeral: true);
            return;
        }

        string optionA, optionB;
        if (hasA)
        {
            (optionA, optionB) = (OneLine(a!), OneLine(b!));
        }
        else
        {
            var parts = _picker.Pick(BotResponses.WouldYouRatherDilemmas).Split('|', 2);
            (optionA, optionB) = (parts[0], parts[1]);
        }

        var round = _rounds.Open(Context.Guild.Id, Context.Channel.Id, optionA, optionB,
            _picker.Pick(BotResponses.WyrIntroLines));
        try
        {
            await RespondAsync(embed: Card(round, 0), components: Buttons(round.Id, disabled: false),
                allowedMentions: AllowedMentions.None);
            round.MessageId = (await GetOriginalResponseAsync()).Id;
            _rounds.ScheduleClose(round);
        }
        catch
        {
            _rounds.Drop(round.Id);
            throw;
        }
    }

    // A custom option is shown in a field title and inside her pick: one line, no surrounding space.
    private static string OneLine(string text) => string.Join(' ', text.Split('\n', StringSplitOptions.TrimEntries)).Trim();

    /// <summary>The open card: both options, how many have voted, when it closes. Never the split.</summary>
    public static Embed Card(WyrRound round, int total) =>
        new EmbedBuilder()
            .WithTitle("Tu préfères…")
            .WithDescription(
                $"{round.Intro}\n\n🅰️ **{round.A}**\n*ou*\n🅱️ **{round.B}**\n\n" +
                $"-# {total} vote(s) · résultats <t:{round.EndsAt.ToUnixTimeSeconds()}:R>")
            .WithColor(Color.Teal)
            .Build();

    /// <summary>The revealed card: each side's share and voters, her comment, her own pick.</summary>
    public static Embed ResultCard(WyrRound round, IReadOnlyList<ulong> forA, IReadOnlyList<ulong> forB,
        string comment, string herPick)
    {
        var total = forA.Count + forB.Count;
        return new EmbedBuilder()
            .WithTitle("Tu préfères… — résultats")
            .WithDescription($"{comment}\n\n{herPick}")
            .AddField($"🅰️ {round.A}", Side(forA, total))
            .AddField($"🅱️ {round.B}", Side(forB, total))
            .WithFooter($"{total} vote(s)")
            .WithColor(Color.Teal)
            .Build();
    }

    private static string Side(IReadOnlyList<ulong> voters, int total)
    {
        var percent = total == 0 ? 0 : (int)Math.Round(voters.Count * 100.0 / total);
        var cells = percent / 10;
        var bar = new string('█', cells) + new string('░', 10 - cells);
        var names = voters.Count == 0
            ? "personne"
            : string.Join(" ", voters.Take(MaxNames).Select(id => $"<@{id}>"))
              + (voters.Count > MaxNames ? $" +{voters.Count - MaxNames}" : "");
        return $"{bar} **{percent} %** ({voters.Count})\n{names}";
    }

    // Two buttons, two verbs' worth of ids (the side differs), so they never collide.
    public static MessageComponent Buttons(int roundId, bool disabled) =>
        new ComponentBuilder()
            .WithButton(customId: $"wyr:pick:{roundId}:0", emote: new Emoji("🅰️"), style: ButtonStyle.Primary, disabled: disabled)
            .WithButton(customId: $"wyr:pick:{roundId}:1", emote: new Emoji("🅱️"), style: ButtonStyle.Primary, disabled: disabled)
            .Build();
}
