using System.Text.Json;
using Discord;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// Voyager's letters (home only: one correspondence, its own channel) and ambient-state.json.
// Part of AmbientService.
internal sealed partial class AmbientService
{
    // Where Voyager's letters land — their own channel, not the main one. Server-specific, listed
    // in architecture.md's "Hardcoded ids". In the dev guild it never resolves and the letters are skipped.
    internal const ulong VoyagerChannelId = 901172450719584356;

    private const ulong VoyagerLetterSalt = 0x5359_4E43_564F_594C; // "SYNCVOYL"
    private const ulong VoyagerAnswerSalt = 0x5359_4E43_564F_5941; // "SYNCVOYA"

    // Voyager 1 writes to her (docs/agents/personality.md, Ambient life), in VoyagerChannelId: a
    // letter on the days Helpers/Personality/Voyager picks, at its slot; her answer, some of those
    // days, hours later; and on 18 Nov 2026, once ever, the light-day line in place of any letter.
    // Claimed in ambient-state.json before the send, so a restart never says one twice and a
    // failed send isn't retried that day. One line per tick at most.
    private async Task TryVoyagerAsync(DateTimeOffset now)
    {
        if (!Ambient.IsWakeHours(now)) return;
        if (_client.GetChannel(VoyagerChannelId) is not IMessageChannel channel || _breakdown.IsActive(channel.Id)) return;

        var day = AppTime.DayNumber(now);
        var time = AppTime.ToZoned(now).TimeOfDay;
        string? line = null, what = null;
        lock (_stateLock)
        {
            var state = LoadState();
            if (Voyager.IsCrossingDay(day))
            {
                if (!state.CrossingDone && state.LastLetterDay != day && time >= Voyager.LetterSlot(day))
                {
                    line = BotResponses.VoyagerCrossingLine;
                    what = "Voyager crossing line";
                    state = state with { CrossingDone = true, LastLetterDay = day };
                }
            }
            else if (Voyager.LetterNumber(day) is { } number && state.LastLetterDay != day && time >= Voyager.LetterSlot(day))
            {
                line = VoyagerLetter(number, now);
                what = "Voyager letter";
                state = state with { LastLetterDay = day };
            }

            if (line is null && state.LastLetterDay == day && state.LastAnswerDay != day
                && Voyager.AnswerSlot(day) is { } answerAt && time >= answerAt)
            {
                // On the crossing day the letter was the light-day line, which has no number.
                line = VoyagerAnswer(Voyager.LetterNumber(day) ?? day);
                what = "Voyager answer";
                state = state with { LastAnswerDay = day };
            }

            if (line is null) return;
            SaveState(state);
        }

        _logger.LogInformation("Ambient: {What}.", what);
        await BotChat.PostWithTypingAsync(channel, line, _logger, what!, AllowedMentions.None);
    }

    /// <summary>Letter number <paramref name="letterNumber"/>, with his light time at <paramref name="now"/>. Also /debug voyager's.</summary>
    internal static string VoyagerLetter(int letterNumber, DateTimeOffset now) =>
        string.Format(DailyRotation.Pick(BotResponses.VoyagerLetterLines, letterNumber, VoyagerLetterSalt),
            Voyager.Format(Voyager.LightTime(now)));

    /// <summary>Her answer to letter <paramref name="letterNumber"/>. Also /debug voyager's.</summary>
    internal static string VoyagerAnswer(int letterNumber) =>
        DailyRotation.Pick(BotResponses.VoyagerAnswerLines, letterNumber, VoyagerAnswerSalt);

    // What a restart must not forget: the version she last ran (for the update line), the day
    // she last spoke on waking (one wake line a day, however many deploys) — a DayKey — and
    // Voyager's correspondence: the last letter and answer days (DayNumbers) and whether the
    // light-day line went out. Fields added later default, so an older file still reads.
    // Process-wide, not per server: one body, one wake, one correspondent.
    private sealed record AmbientState(
        string? LastVersion, int LastWakeDay,
        int LastLetterDay = 0, int LastAnswerDay = 0, bool CrossingDone = false);

    private AmbientState LoadState()
    {
        try
        {
            if (File.Exists(_statePath))
                return JsonSerializer.Deserialize<AmbientState>(File.ReadAllText(_statePath))
                       ?? new AmbientState(null, 0);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ambient: could not read {Path}; starting fresh.", _statePath);
        }
        return new AmbientState(null, 0);
    }

    private void SaveState(AmbientState state)
    {
        try
        {
            File.WriteAllText(_statePath, JsonSerializer.Serialize(state));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ambient: could not write {Path}.", _statePath);
        }
    }
}
