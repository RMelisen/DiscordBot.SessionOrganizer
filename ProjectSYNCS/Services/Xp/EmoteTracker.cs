using System.Globalization;
using System.Text.RegularExpressions;
using Discord;
using Discord.WebSocket;

namespace ProjectSYNCS.Services;

// Tallies emote usage across a guild: custom emotes and unicode emojis written
// in messages, plus emotes added/removed as reactions. Counts go to ActivityCounter
// (in memory), which ActivityFlushService writes every few minutes: never a database
// write per message or per reaction.
internal sealed class EmoteTracker
{
    private readonly DiscordSocketClient _client;
    private readonly ActivityCounter _counter;

    // Matches Discord custom-emote markup: <:name:id> or animated <a:name:id>.
    private static readonly Regex _customEmoteRegex =
        new(@"<(a?):(\w+):(\d+)>", RegexOptions.Compiled);

    public EmoteTracker(DiscordSocketClient client, ActivityCounter counter)
    {
        _client = client;
        _counter = counter;
    }

    // Counts each emote written in a guild message — custom emotes and unicode
    // emojis alike (occurrences, so the same emote three times counts as three).
    public void HandleMessage(SocketMessage rawMessage)
    {
        if (rawMessage is not SocketUserMessage message) return;
        if (message.Author.IsBot) return;
        if (message.Channel is not SocketGuildChannel guildChannel) return;
        if (string.IsNullOrEmpty(message.Content)) return;

        var counts = new Dictionary<EmoteRef, int>();

        // Custom emotes: <:name:id> / <a:name:id>.
        foreach (Match m in _customEmoteRegex.Matches(message.Content))
        {
            if (!ulong.TryParse(m.Groups[3].Value, out var id)) continue;
            var emote = EmoteRef.Custom(id, m.Groups[2].Value, m.Groups[1].Value == "a");
            counts[emote] = counts.TryGetValue(emote, out var c) ? c + 1 : 1;
        }

        // Strip custom-emote markup first so its digits aren't mistaken for emoji,
        // then scan the rest for unicode emoji grapheme clusters.
        var withoutCustom = _customEmoteRegex.Replace(message.Content, " ");
        foreach (var cluster in EnumerateEmojis(withoutCustom))
        {
            var emote = EmoteRef.FromUnicode(cluster);
            counts[emote] = counts.TryGetValue(emote, out var c) ? c + 1 : 1;
        }

        if (counts.Count == 0) return;
        _counter.CountEmotesWritten(guildChannel.Guild.Id, counts, message.Timestamp);
    }

    public Task HandleReactionAddedAsync(
        Cacheable<IUserMessage, ulong> message,
        Cacheable<IMessageChannel, ulong> channel,
        SocketReaction reaction) => CountReactionAsync(channel, reaction, +1);

    public Task HandleReactionRemovedAsync(
        Cacheable<IUserMessage, ulong> message,
        Cacheable<IMessageChannel, ulong> channel,
        SocketReaction reaction) => CountReactionAsync(channel, reaction, -1);

    // Adjusts the reacted count for an emote by delta (custom or unicode).
    // Ignores the bot's own reactions and reactions outside a guild. A removal lands
    // on the day it happens and never takes a count below zero (ActivityFlushService).
    private async Task CountReactionAsync(
        Cacheable<IMessageChannel, ulong> channel, SocketReaction reaction, int delta)
    {
        if (reaction.UserId == _client.CurrentUser.Id) return;

        var emote = reaction.Emote switch
        {
            Emote custom => EmoteRef.Custom(custom.Id, custom.Name, custom.Animated),
            Emoji emoji => EmoteRef.FromUnicode(emoji.Name),
            _ => (EmoteRef?)null
        };
        if (emote is null) return;

        var resolved = await channel.GetOrDownloadAsync();
        if (resolved is not IGuildChannel guildChannel) return;

        _counter.CountEmoteReacted(guildChannel.GuildId, emote.Value, delta, DateTimeOffset.UtcNow);
    }

    // Yields each unicode emoji in the text as a whole grapheme cluster (so
    // multi-codepoint emoji like flags, skin tones and ZWJ sequences stay intact).
    private static IEnumerable<string> EnumerateEmojis(string text)
    {
        var e = StringInfo.GetTextElementEnumerator(text);
        while (e.MoveNext())
        {
            var cluster = (string)e.Current;
            if (IsEmojiCluster(cluster)) yield return cluster;
        }
    }

    // True when a grapheme cluster's leading codepoint falls in an emoji range.
    private static bool IsEmojiCluster(string cluster)
    {
        var rune = System.Text.Rune.GetRuneAt(cluster, 0).Value;
        return rune is (>= 0x1F000 and <= 0x1FAFF)   // pictographs, symbols, faces…
            or (>= 0x1F1E6 and <= 0x1F1FF)           // regional indicators (flags)
            or (>= 0x2600 and <= 0x27BF)             // misc symbols & dingbats
            or (>= 0x2300 and <= 0x23FF)             // technical (⌚ ⏰ …)
            or (>= 0x2B00 and <= 0x2BFF)             // stars, arrows
            or 0x2049 or 0x203C or 0x2122 or 0x2139  // ‼ ⁉ ™ ℹ
            or (>= 0x2190 and <= 0x21AA);            // a few arrows used as emoji
    }
}
