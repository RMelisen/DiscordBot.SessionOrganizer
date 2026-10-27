using System.Globalization;
using System.Text;
using System.Text.Json;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// Talk that isn't an order, being hit, defending family, and her rationed remarks. Part of MinecraftBodyService.
public sealed partial class MinecraftBodyService
{
    // ---- Talk -----------------------------------------------------------------------------------

    /// <summary>
    /// Chat that isn't an order, read the way Discord reads it (McTalkReader) and answered from her Discord pools made fit
    /// for the game (McChat.Game). An answer to someone isn't a remark: no gate, no budget, only the conversation streak.
    /// Talked about rather than to, she speaks on her own initiative: a remark like any other. Each answer opens the
    /// call window again, so the next message is heard without her name. True when it was dealt with, said or not;
    /// false when there was nothing to say (a call then gets its « oui ? »).
    /// </summary>
    private bool Talk(string player, bool whispered, string text, bool call, bool listening)
    {
        var kin = KinOf(player);
        var read = McTalkReader.Read(HerNameAsSyncs(text), kin, call, listening, whispered, McTalkReader.AskerId(player, kin),
            _health.CurrentMilli, _picker, Random.Shared);
        if (read is not { } talk) return false;
        // « tu fais quoi ? » is answered from what she's really doing, not from her Discord chores.
        if (talk.Kind == nameof(ChatIntent.WhatAreYouDoing))
        {
            TellDoing(player, whispered);
            return true;
        }
        var weekday = AppTime.Now.ToString("dddd", CultureInfo.GetCultureInfo("fr-FR"));

        if (talk.Remark)
        {
            if (Random.Shared.NextDouble() < TalkedAboutChance && McChat.Game(talk.Pool) is { Length: > 0 } lines)
                Remark("talkedAbout", lines, player, false, weekday, talk.A, talk.B);
            return true;
        }

        var pool = talk.Pool;
        object[] extra = [weekday, talk.A, talk.B];
        if (kin != McKin.Papa)
        {
            var now = DateTimeOffset.UtcNow;
            var key = McTalkReader.AskerId(player, kin);
            if (_talks.IsClosed(0, key, now)) return true;
            var count = _talks.Record(0, key, now);
            if (count >= ConversationStreak.EndAt)
            {
                _talks.Close(0, key, now);
                (pool, extra) = (BotResponses.StreakEndLines, [weekday, count]);
            }
            else if (count == ConversationStreak.NudgeAt && talk.Fallback)
                (pool, extra) = (BotResponses.StreakNudgeLines, [weekday, count]);
        }

        if (McChat.Game(pool, whispered) is not { Length: > 0 } game) return false;
        _logger.LogInformation("Minecraft: {Player} talked to her — {Kind}.", player, talk.Kind);
        Reply(player, whispered, game, extra);
        // She answered, unless she just ended the conversation: their next message is read as said to her (an order
        // needs no name), and for CallWindow what is plainly for her is answered, whatever else they say meanwhile.
        lock (_gate)
            if (!ReferenceEquals(pool, BotResponses.StreakEndLines))
            {
                var now = DateTimeOffset.UtcNow;
                _calledAt[player] = now;
                _answeredAt[player] = now;
            }
            else _answeredAt.Remove(player);
        return true;
    }

    // « tu fais quoi ? »: the order under way (or just sent) first, else her own life as the body last reported it, else
    // nothing. Asked by someone else than its player, a whispered order keeps its place to itself (McDoing.ForStatus's
    // rule). An answer like any other: it opens the window again.
    private void TellDoing(string player, bool whispered)
    {
        McJob? job;
        McBodyDoing? doing;
        lock (_gate) (job, doing) = (_order ?? _pending, _doing);
        string? clause;
        if (job is not null)
        {
            var theirs = Same(job.Player, player);
            clause = McDoing.FirstPerson(job.Order, Who(job.Player), job.Private && !theirs ? null : job.Detail, toAsker: theirs);
        }
        else clause = McDoing.OwnLife(doing, doing is { Player.Length: > 0 } d ? Who(d.Player) : null, firstPerson: true,
            toAsker: doing is not null && Same(doing.Player, player));
        _logger.LogInformation("Minecraft: {Player} asked what she's doing — {Clause}.", player, clause ?? "nothing");
        Reply(player, whispered, clause is null ? BotResponses.McDoingNothingLines : BotResponses.McDoingLines, clause ?? "");
        lock (_gate)
        {
            var now = DateTimeOffset.UtcNow;
            _calledAt[player] = now;
            _answeredAt[player] = now;
        }
    }

    // Her in-game name read as « syncs » by the chat cues, which only know that one (« SYNCS_bot ça va ? »).
    private string HerNameAsSyncs(string text) =>
        McTalkReader.IsHerName(_username) ? text
            : System.Text.RegularExpressions.Regex.Replace(text, $@"(?<![\w]){System.Text.RegularExpressions.Regex.Escape(_username)}(?![\w])", "syncs",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

    // Whether this player's hello, goodbye, « gg », « merci » or call gets her words: the first of a burst does, and
    // Papa and Tata always do (the owner's call). Family still claims the gate, so the others after them get the gesture.
    private bool FirstOfBurst(string player, McOrder kind)
    {
        var first = _burstGate.TryClaim(kind);
        return first || KinOf(player) != McKin.Anyone;
    }

    // An order's acknowledgement (« j'arrive », « je te suis »), said once every 3 minutes for the same order from the
    // same player: <paramref name="what"/> names the order and whatever makes it another one (the place, the resource).
    private void Ack(string player, bool whispered, string what, string[] pool, params object[] extra)
    {
        if (_ackGate.TryClaim($"{player.ToLowerInvariant()}|{what}")) Reply(player, whispered, pool, extra);
    }

    // Her lines go out one after another, each after the time it takes to type it, as on Discord (BotChat): an
    // answer in the same instant as the question reads as a machine. The queue keeps them in order. Whatever can't
    // wait (a forced remark: a creeper behind someone, drowning) goes out at once, and so does her goodbye, which
    // the quit right behind it would cut off.
    private void SayLocked(MinecraftBodyProcess body, object command, string text, bool now = false)
    {
        if (now)
        {
            body.Send(command);
            return;
        }
        _speech = _speech.ContinueWith(async _ =>
        {
            await Task.Delay(TypingPause(text));
            lock (_gate)
                if (ReferenceEquals(_body, body) && _spawned && !_leaving)
                    body.Send(command);
        }, TaskScheduler.Default).Unwrap();
    }

    // Game chat is typed slower than Discord: a short « oui ? » takes under a second, a long line about three.
    private static TimeSpan TypingPause(string text) =>
        TimeSpan.FromMilliseconds(Math.Clamp(400 + text.Length * 40, 700, 3000));

    // A player hit her. The first hits get a remark; the one that makes a streak (McHitStreaks: the same
    // player, three times, no more than a minute apart) is when she understands they want to kill her, and
    // that line goes out at once, past the remark gates. Hits after it keep the same lines, rationed again.
    private void OnHit(string hitter)
    {
        // Someone she's hitting back for Papa or Tata: their hits are the fight, not a reason to complain or run.
        lock (_gate)
            if (_defending.TryGetValue(hitter, out var defence) && defence.Until > DateTimeOffset.UtcNow) return;
        McHitStage stage;
        lock (_gate) stage = _hits.Register(hitter, DateTimeOffset.UtcNow);
        if (stage == McHitStage.Hit)
        {
            if (KinOf(hitter) == McKin.Anyone && Random.Shared.NextDouble() >= HitChance) return;
            Remark("hit", Kin(hitter, BotResponses.McHitByPlayerLines, BotResponses.McHitByPlayerLinesOwner,
                BotResponses.McHitByPlayerLinesTata), hitter);
            return;
        }
        Remark("hit", Kin(hitter, BotResponses.McHitAgainLines, BotResponses.McHitAgainLinesOwner, BotResponses.McHitAgainLinesTata),
            hitter, force: stage == McHitStage.Escalated);
        // And she runs. Never from Papa: she stays, scared, and takes it.
        if (KinOf(hitter) == McKin.Papa) return;
        lock (_gate)
            if (_body is { } body && _spawned && !_leaving)
                body.Send(new { op = "flee", player = hitter });
    }

    // A player hit Papa or Tata (never family: the body filters them out). The first hit gets a word; again within
    // the streak's window, she hits them back once; after that she fights them (McHitStreaks.DefendStage). The body
    // says whether she could go (`defending`) or was busy with someone's order (`cantDefend`, words only).
    private void OnFamilyHurt(string victim, string attacker)
    {
        if (victim.Length == 0 || attacker.Length == 0 || KinOf(attacker) != McKin.Anyone) return;
        McDefendStage stage;
        lock (_gate) stage = McHitStreaks.DefendStage(_familyHits.Tally(attacker, DateTimeOffset.UtcNow));
        if (stage == McDefendStage.Warn)
        {
            Remark("familyHit", BotResponses.McFamilyHitLines, attacker, false, Who(victim));
            return;
        }
        lock (_gate)
            if (_body is { } body && _spawned && !_leaving)
                body.Send(new { op = "defend", player = attacker, victim, swat = stage == McDefendStage.Swat });
    }

    // She's hitting them back. The swat and the start of a fight are said at once (the hit needs its reason); the
    // fight's later hits are rationed.
    private void OnDefending(string attacker, string victim, bool swat)
    {
        bool fightStarts;
        lock (_gate)
        {
            fightStarts = !swat && !(_defending.TryGetValue(attacker, out var was) && was.Fighting && was.Until > DateTimeOffset.UtcNow);
            _defending[attacker] = (DateTimeOffset.UtcNow + DefendHold, !swat);
        }
        Remark(swat ? "familySwat" : "familyFight", swat ? BotResponses.McFamilySwatLines : BotResponses.McFamilyFightLines,
            attacker, swat || fightStarts, Who(victim));
    }

    /// <summary>
    /// One of her lines in Minecraft's chat, rationed per kind, overall and by the 10-minute budget.
    /// <paramref name="force"/> says it anyway, for the moments that can't wait, and still starts both cooldowns so
    /// nothing piles on after (it doesn't spend the budget). Returns whether it was said. <paramref name="extra"/>
    /// fills {1}, {2}… as the pool documents.
    /// </summary>
    private bool Remark(string kind, string[] pool, string? name = null, bool force = false, params object[] extra)
    {
        if (force)
        {
            _kindGate.TryClaim(kind);
            _anyGate.TryClaim(0);
        }
        else if (!_kindGate.TryClaim(kind)) return false;
        else if (!_anyGate.TryClaim(0))
        {
            _kindGate.Release(kind);
            return false;
        }
        lock (_gate)
            if (_body is { } body && _spawned && !_leaving)
            {
                if (!force && !SpendBudgetLocked())
                {
                    _kindGate.Release(kind);
                    _anyGate.Release(0);
                    return false;
                }
                var text = string.Format(_picker.Pick(pool, LineChoices), [name is null ? "" : Who(name), .. extra]);
                SayLocked(body, new { op = "say", text }, text, now: force);
                return true;
            }
        return false;
    }

    // Whether an unforced remark still fits in the last 10 minutes' budget, counting it if so.
    private bool SpendBudgetLocked()
    {
        var now = DateTimeOffset.UtcNow;
        while (_remarksSaid.TryPeek(out var said) && now - said >= RemarkBudgetWindow) _remarksSaid.Dequeue();
        if (_remarksSaid.Count >= RemarkBudget) return false;
        _remarksSaid.Enqueue(now);
        return true;
    }
}
