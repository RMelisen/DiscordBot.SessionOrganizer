using System.Text.RegularExpressions;
using Discord;
using Discord.Interactions;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Commands;

// /tonight: « qui est chaud ce soir ? » — lighter than /schedule. One open call per guild at a
// time; buttons in TonightComponentHandler; ReminderService closes it at midnight. The card
// builders are static and shared by all three.
[CommandContextType(InteractionContextType.Guild)]
public partial class TonightModule : InteractionModuleBase<SocketInteractionContext>
{
    public const int DefaultMinimum = 4;
    private const string DefaultHour = "21:00";

    // How many names a field lists before « … et N autre(s) » — the giveaway's cap, same reason.
    private const int MaxNamesShown = 20;

    private readonly TonightService _tonight;

    public TonightModule(TonightService tonight)
    {
        _tonight = tonight;
    }

    [GeneratedRegex(@"^\s*(\d{1,2})\s*(?:h|:)\s*(\d{2})?\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex HourRegex();

    [SlashCommand("tonight", "Demander qui est chaud pour jouer ce soir")]
    public async Task TonightAsync(
        [Summary("game", "À quoi vous joueriez (facultatif)")]
        [MaxLength(InputCaps.TonightGame)]
        string? game = null,
        [Summary("time", "À quelle heure (21:00 par défaut) — « 21h », « 20h30 », « 21:15 »")]
        [MaxLength(10)]
        string? hour = null,
        [Summary("minimum", "Combien de « chauds » pour lancer la session (4 par défaut)")]
        [MinValue(2)] [MaxValue(10)]
        int minimum = DefaultMinimum)
    {
        if (!TryParseTonight(hour ?? DefaultHour, DateTimeOffset.UtcNow, out var startsAt))
        {
            await RespondAsync("Je n'ai pas compris l'heure. Essaie « 21h », « 20h30 » ou « 21:15 ».", ephemeral: true);
            return;
        }
        if (startsAt <= DateTimeOffset.UtcNow)
        {
            await RespondAsync("Cette heure-là est déjà passée. Ce soir, c'est plus tard.", ephemeral: true);
            return;
        }

        await DeferAsync();
        var open = await _tonight.GetOpenAsync(Context.Guild.Id);
        if (open is not null)
        {
            // The deferred reply is public: say it there, briefly, with the link.
            await FollowupAsync($"Quelqu'un a déjà demandé qui était chaud ce soir : {Link(open)}");
            return;
        }

        var call = await _tonight.CreateAsync(Context.Guild.Id, Context.Channel.Id, Context.User.Id,
            game?.Trim() ?? "", startsAt, minimum, ExpiryFor(startsAt));
        var message = await FollowupAsync(embed: BuildEmbed(call), components: BuildComponents(call),
            allowedMentions: AllowedMentions.None);
        await _tonight.SetMessageAsync(call.Id, message.Id);
    }

    // « 21h » today, Paris time. A small hour already past (« 1h » at 22:00) means after midnight.
    public static bool TryParseTonight(string text, DateTimeOffset nowUtc, out DateTimeOffset startsAt)
    {
        startsAt = default;
        var m = HourRegex().Match(text);
        if (!m.Success) return false;
        var h = int.Parse(m.Groups[1].Value);
        var min = m.Groups[2].Success ? int.Parse(m.Groups[2].Value) : 0;
        if (h > 23 || min > 59) return false;

        var today = AppTime.ToZoned(nowUtc).Date;
        startsAt = AppTime.AtWallClock(today.AddHours(h).AddMinutes(min));
        if (startsAt <= nowUtc && h < 6)
            startsAt = AppTime.AtWallClock(today.AddDays(1).AddHours(h).AddMinutes(min));
        return true;
    }

    // Midnight Paris after the call, or an hour after the start for a late-night one.
    private static DateTimeOffset ExpiryFor(DateTimeOffset startsAt)
    {
        var midnight = AppTime.AtWallClock(AppTime.ToZoned(DateTimeOffset.UtcNow).Date.AddDays(1));
        var afterStart = startsAt + TimeSpan.FromHours(1);
        return afterStart > midnight ? afterStart : midnight;
    }

    public static string Link(TonightCall call) =>
        $"https://discord.com/channels/{call.GuildId}/{call.ChannelId}/{call.MessageId}";

    public static int CountIn(TonightCall call) => call.Responses.Count(r => r.Status == TonightStatus.In);

    // ---- Card -------------------------------------------------------------------------------------

    public static Embed BuildEmbed(TonightCall call)
    {
        var title = string.IsNullOrEmpty(call.Game) ? "Qui est chaud ce soir ?" : $"{call.Game} — qui est chaud ce soir ?";
        var ts = call.StartsAt.ToUnixTimeSeconds();
        var inCount = CountIn(call);

        // Her line is chosen by the call's id, not the picker: the card is redrawn on every click
        // and must keep saying the same thing.
        string line;
        Color color;
        if (call.SessionEventId != 0)
        {
            line = "✅ C'est devenu une vraie session. Rendez-vous sur sa carte.";
            color = Color.DarkGrey;
        }
        else if (call.Closed)
        {
            line = StableRoll.Pick(BotResponses.TonightExpiredLines, call.Id, 41, 0);
            color = Color.DarkGrey;
        }
        else
        {
            line = StableRoll.Pick(BotResponses.TonightCardLines, call.Id, 40, 0);
            color = inCount >= call.Minimum ? Color.Green : Color.Orange;
        }

        var embed = new EmbedBuilder()
            .WithTitle(title)
            .WithDescription($"{line}\n\n" +
                             $"Rendez-vous <t:{ts}:t> (<t:{ts}:R>) · lancé par <@{call.OrganizerId}>\n" +
                             $"Il faut **{call.Minimum}** chauds pour lancer la session.")
            .AddField($"Chauds ({inCount}/{call.Minimum})", Names(call, TonightStatus.In), inline: true)
            .AddField("Peut-être", Names(call, TonightStatus.Maybe), inline: true)
            .AddField("Pas ce soir", Names(call, TonightStatus.Out), inline: true)
            .WithColor(color);
        return embed.Build();
    }

    private static string Names(TonightCall call, TonightStatus status)
    {
        var ids = call.Responses.Where(r => r.Status == status).OrderBy(r => r.AnsweredAt).Select(r => r.UserId).ToList();
        if (ids.Count == 0) return "—";
        var shown = string.Join("\n", ids.Take(MaxNamesShown).Select(id => $"<@{id}>"));
        return ids.Count > MaxNamesShown ? $"{shown}\n… et {ids.Count - MaxNamesShown} autre(s)" : shown;
    }

    // No emotes on these buttons, at the owner's request. Each carries its own verb.
    public static MessageComponent BuildComponents(TonightCall call)
    {
        var builder = new ComponentBuilder();
        if (call.Closed) return builder.Build();

        builder.WithButton("Chaud", $"tonight:in:{call.Id}", ButtonStyle.Success, row: 0)
            .WithButton("Peut-être", $"tonight:maybe:{call.Id}", ButtonStyle.Secondary, row: 0)
            .WithButton("Pas ce soir", $"tonight:out:{call.Id}", ButtonStyle.Danger, row: 0);
        if (CountIn(call) >= call.Minimum)
            builder.WithButton("Créer la session", $"tonight:session:{call.Id}", ButtonStyle.Primary, row: 1);
        return builder.Build();
    }
}
