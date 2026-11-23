using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Commands;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

internal sealed partial class ChatterService
{
    // ---- Requests she acts on: « syncs, organise un valo samedi 21h » ---------------------------
    // A Request for a session, a /tonight call, a vote, a reminder or teams is read in detail
    // (ChatRequest) and answered with the thing itself, not a pointer to the command. What would show
    // in public (a session, a call, a vote) is offered behind a button only the asker can press, which
    // opens the command's own private flow (ChatOffers, the `chatreq:` handlers); what doesn't (a
    // reminder, teams) is done at once. Giveaways, the quiz and moderation keep their pointer line.
    // Off the gateway handler (the reads and writes), like her answers from the records.

    private static readonly AllowedMentions RequestMentions = new(AllowedMentionTypes.None) { MentionRepliedUser = true };

    private bool TryActOnRequest(SocketUserMessage message, ChatPath path, IntentPick plain, string name, string weekday)
    {
        if (message.Channel is not SocketGuildChannel) return false;
        var content = message.Content ?? string.Empty;
        var read = MessageCues.ReadRequestTopic(content);
        if (read is not (RequestTopic.Session or RequestTopic.Tonight or RequestTopic.Poll or RequestTopic.Reminder or RequestTopic.Teams))
            return false;
        var topic = read.Value;
        _ = Task.Run(() => ActOnRequestAsync(topic, message, path, plain, name, weekday));
        return true;
    }

    private async Task ActOnRequestAsync(
        RequestTopic topic, SocketUserMessage message, ChatPath path, IntentPick plain, string name, string weekday)
    {
        var what = $"{AnswerKinds.Key(path)} request ({topic})";
        try
        {
            var done = topic switch
            {
                RequestTopic.Session => await OfferSessionAsync(message, path, name, weekday),
                RequestTopic.Tonight => await OfferTonightAsync(message, path, name, weekday),
                RequestTopic.Poll => await OfferVoteAsync(message, path, name, weekday),
                RequestTopic.Reminder => await SetReminderAsync(message, path),
                _ => await DealTeamsAsync(message, path, name, weekday),
            };
            if (done) return;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not act on a {Topic} request from {UserId}.", topic, message.Author.Id);
        }

        // Nothing she could do with it: the pointer line, as before.
        Count(message, path, plain.Kind);
        await ReplyWithTypingAsync(message, Format(plain, name, weekday), what, plain.Mentions);
    }

    private async Task<bool> OfferSessionAsync(SocketUserMessage message, ChatPath path, string name, string weekday)
    {
        var ask = ChatRequest.ReadSession(message.Content ?? string.Empty, DateTimeOffset.UtcNow);
        var title = Discord.Format.Sanitize(ask.Title);
        var summary = (title.Length > 0, ask.At) switch
        {
            (true, { } at) => $"**{title}**, <t:{at.ToUnixTimeSeconds()}:F>",
            (true, null) => $"**{title}**",
            (false, { } at) => $"une session <t:{at.ToUnixTimeSeconds()}:F>",
            _ => "une session",
        };
        // What else she read, so they see it before the click: « (5 places, 3 h, chaque semaine) ».
        var extras = new List<string>();
        if (ask.Places is { } places) extras.Add($"{places} places");
        if (ask.Minutes is { } minutes) extras.Add(SessionTime.DurationLabel(minutes));
        if (ask.Weekly) extras.Add("chaque semaine");
        if (extras.Count > 0) summary += $" ({string.Join(", ", extras)})";
        var id = _offers.Add(new ChatOffer(message.Author.Id, ask.Title, ask.At, Array.Empty<string>(), ask));
        return await OfferAsync(message, path, RequestTopic.Session, name, weekday, summary,
            new ButtonBuilder("Préparer la session", $"chatreq:session:{id}", ButtonStyle.Primary));
    }

    private async Task<bool> OfferTonightAsync(SocketUserMessage message, ChatPath path, string name, string weekday)
    {
        var now = DateTimeOffset.UtcNow;
        var ask = ChatRequest.ReadTonight(message.Content ?? string.Empty, now);
        var at = ask.At ?? (TonightModule.TryParseTonight(TonightModule.DefaultHour, now, out var nine) ? nine : null);
        // Too late for tonight: the pointer line says how to call one at another hour.
        if (at is not { } startsAt || startsAt <= now) return false;

        var game = Discord.Format.Sanitize(ask.Game);
        var summary = (game.Length > 0 ? $"**{game}**, " : string.Empty) + $"ce soir <t:{startsAt.ToUnixTimeSeconds()}:t>";
        var id = _offers.Add(new ChatOffer(message.Author.Id, ask.Game, startsAt, Array.Empty<string>()));
        return await OfferAsync(message, path, RequestTopic.Tonight, name, weekday, summary,
            new ButtonBuilder("Lancer l'appel", $"chatreq:tonight:{id}", ButtonStyle.Primary));
    }

    private async Task<bool> OfferVoteAsync(SocketUserMessage message, ChatPath path, string name, string weekday)
    {
        var ask = ChatRequest.ReadVote(message.Content ?? string.Empty);
        var shown = ask.Options.Select(o => $"**{Discord.Format.Sanitize(o)}**").ToList();
        var summary = ask.Asked
            ? $"le vote **{Discord.Format.Sanitize(ask.Title)}**"
            : shown.Count == 0
                ? "un vote"
                : "un vote entre " + string.Join(", ", shown.Take(shown.Count - 1)) + " et " + shown[^1];
        var id = _offers.Add(new ChatOffer(message.Author.Id, ask.Title, null, ask.Options));
        return await OfferAsync(message, path, RequestTopic.Poll, name, weekday, summary,
            new ButtonBuilder("Préparer le vote", $"chatreq:vote:{id}", ButtonStyle.Primary));
    }

    // Her line with what she read ({2}), and the button. Papa gets his own pool around the same.
    private async Task<bool> OfferAsync(SocketUserMessage message, ChatPath path, RequestTopic topic, string name,
        string weekday, string summary, ButtonBuilder button)
    {
        var pool = message.Author.Id == OwnerId ? BotResponses.OwnerRequestActionLines : BotResponses.RequestActionLines[topic];
        var line = string.Format(_picker.Pick(pool), name, weekday, summary, string.Empty);
        var components = new ComponentBuilder().WithButton(button).Build();
        return await SendActionAsync(message, path, topic, line, components);
    }

    private async Task<bool> SetReminderAsync(SocketUserMessage message, ChatPath path)
    {
        var now = DateTimeOffset.UtcNow;
        var ask = ChatRequest.ReadReminder(message.Content ?? string.Empty, now);
        // No time she can read, or one out of range: the pointer line names /remindme.
        if (ask.Due is not { } due || due - now < ReminderTime.Min || due - now > ReminderTime.Max) return false;

        await using var scope = _services.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<PersonalReminderStore>();
        if (await store.CountPendingAsync(message.Author.Id) >= PersonalReminderStore.MaxPending)
        {
            Count(message, path, AnswerKinds.Request(RequestTopic.Reminder));
            await ReplyWithTypingAsync(message,
                $"Tu as déjà {PersonalReminderStore.MaxPending} rappels en attente. Annule-en un avec `/reminders`.",
                "a reminder past the cap", RequestMentions);
            return true;
        }

        // « rappelle-moi demain 9h » says when, not what: the message itself is the what.
        var text = ask.Text.Length > 0 ? ask.Text : $"Ton message : {message.GetJumpUrl()}";
        var guildId = ((SocketGuildChannel)message.Channel).Guild.Id;
        var reminder = await store.AddAsync(guildId, message.Channel.Id, message.Author.Id, text, due);
        var unix = due.ToUnixTimeSeconds();
        var line = $"{_picker.Pick(BotResponses.ReminderSetLines)}\n<t:{unix}:F> (<t:{unix}:R>)\n"
                   + MessageFormat.Quote(text, InputCaps.Reminder);
        var components = new ComponentBuilder()
            .WithButton("Annuler", $"chatreq:remundo:{reminder.Id}:{message.Author.Id}", ButtonStyle.Danger)
            .Build();
        return await SendActionAsync(message, path, RequestTopic.Reminder, line, components);
    }

    private async Task<bool> DealTeamsAsync(SocketUserMessage message, ChatPath path, string name, string weekday)
    {
        var kind = AnswerKinds.Request(RequestTopic.Teams);
        if ((message.Author as SocketGuildUser)?.VoiceChannel is not { } voice)
        {
            Count(message, path, kind);
            await ReplyWithTypingAsync(message, string.Format(_picker.Pick(BotResponses.TeamsNoVoiceLines), name, weekday),
                "teams asked from outside voice", RequestMentions);
            return true;
        }

        var ask = ChatRequest.ReadTeams(message.Content ?? string.Empty);
        await using var scope = _services.CreateAsyncScope();
        var (embed, components, refusal) = await TeamsModule.DrawAsync(voice, ask.Count,
            ask.Balanced ? TeamMode.Balanced : TeamMode.Random, scope.ServiceProvider.GetRequiredService<XpService>(), _picker);
        Count(message, path, kind);
        if (refusal is not null)
        {
            await ReplyWithTypingAsync(message, refusal, "teams refused", RequestMentions);
            return true;
        }
        _listening.Answered(message.Channel.Id, message.Author.Id, DateTimeOffset.UtcNow);
        await BotChat.PostEmbedWithTypingAsync(message.Channel, embed!, embed!.Description ?? string.Empty, _logger,
            "teams dealt from chat", components: components, replyTo: new MessageReference(message.Id),
            allowedMentions: RequestMentions);
        return true;
    }

    // An answer that carries a button, under the message that asked. Not through her reply watch:
    // an edit of the request would strike through a line whose button still works.
    private async Task<bool> SendActionAsync(SocketUserMessage message, ChatPath path, RequestTopic topic, string line,
        MessageComponent components)
    {
        Count(message, path, AnswerKinds.Request(topic));
        _listening.Answered(message.Channel.Id, message.Author.Id, DateTimeOffset.UtcNow);
        await BotChat.PostWithTypingAsync(message.Channel, line, _logger, $"{topic} request answered",
            RequestMentions, new MessageReference(message.Id), components);
        return true;
    }
}
