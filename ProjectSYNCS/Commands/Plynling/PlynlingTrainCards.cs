using Discord;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Commands;

/// <summary>
/// What /plynling train and its point buttons answer (docs/agents/plynling-rpg.md). Static and Context-free,
/// so the checks can measure them; the line picker is passed in (the module gives ResponsePicker.Pick).
/// Every answer is private: about six drills a day each, posted publicly, would flood the channel. Only a
/// level-up is announced (<see cref="LevelUpLine"/>).
/// </summary>
public static class PlynlingTrainCards
{
    private static GenderedLines DrillPool(PlynlingStat stat) => stat switch
    {
        PlynlingStat.Force => BotResponses.PlynlingDrillForceLines,
        PlynlingStat.Agility => BotResponses.PlynlingDrillAgilityLines,
        PlynlingStat.Endurance => BotResponses.PlynlingDrillEnduranceLines,
        PlynlingStat.Intelligence => BotResponses.PlynlingDrillIntelligenceLines,
        PlynlingStat.Speed => BotResponses.PlynlingDrillSpeedLines,
        _ => throw new ArgumentOutOfRangeException(nameof(stat), stat, "only body stats are drilled"),
    };

    /// <summary>The drill's private answer, or its refusal; the point buttons whenever a point waits.</summary>
    public static (string Text, MessageComponent? Buttons) Reply(TrainResult r, PlynlingStat drill, Func<string[], string> pick)
    {
        var p = r.Plynling;
        var g = p?.Gender ?? PlynlingGender.Male;
        var buttons = p is not null && p.DiedAt is null && PlynlingRpg.UnspentPoints(p) > 0 ? PointButtons(p.Id) : null;
        var waiting = buttons is null ? "" : $"\n**{PlynlingRpg.PointsToPlace(PlynlingRpg.UnspentPoints(p!))}** : choisis où.";

        switch (r.Outcome)
        {
            case TrainOutcome.Done when p is not null && r.Drill is { } d:
            {
                var name = PlynlingCardUi.SafeName(p.Name);
                var level = d.LevelAfter >= PlynlingRpg.MaxLevel ? $"Niv. {d.LevelAfter} (max)" : $"Niv. {d.LevelAfter}";
                var lines = new List<string>
                {
                    string.Format(pick(DrillPool(drill).For(g)), name),
                    $"+{d.XpGained} XP · {level} · ⚡ {(int)Math.Floor(d.EnergyLeft)}/{r.MaxEnergy}",
                };
                if (d.Boosted)
                    lines.Add(string.Format(pick(BotResponses.PlynlingDrillBoostLines.For(g)), name)
                              + $"\n{PlynlingStats.Emoji(drill)} {PlynlingStats.Name(drill)} +1 pour de bon.");
                if (d.LevelAfter > d.LevelBefore)
                    lines.Add($"🎉 **Niveau {d.LevelAfter} !**");
                if (r.Badges is { Count: > 0 } badges) lines.Add(PlynlingBadges.NewBadgeLines(badges, g));
                return (string.Join("\n", lines) + waiting, buttons);
            }
            case TrainOutcome.NoEnergy when p is not null && r.EnoughAt is { } enough:
                return (PlynlingText.TrainNoEnergy(g, (int)Math.Floor(PlynlingRpg.EnergyAt(p, r.MaxEnergy, DateTimeOffset.UtcNow)), r.MaxEnergy, enough)
                        + waiting, buttons);
            case TrainOutcome.TooYoung: return (PlynlingText.TrainTooYoung(g), null);
            case TrainOutcome.Sick: return (PlynlingText.TrainSick(g) + waiting, buttons);
            case TrainOutcome.Mascot: return (PlynlingText.TrainMascot, null);
            case TrainOutcome.NotABodyStat: return (PlynlingText.TrainUnknown, null);
            case TrainOutcome.Frozen: return (PlynlingCareService.Refusal(CareOutcome.Frozen, g), null);
            case TrainOutcome.Asleep: return (PlynlingCareService.Refusal(CareOutcome.Asleep, g) + waiting, buttons);
            case TrainOutcome.Dead: return (PlynlingCareService.Refusal(CareOutcome.Dead, g), null);
            default: return (PlynlingCareService.Refusal(CareOutcome.NoPlynling, g), null);
        }
    }

    /// <summary>
    /// One row, one blurple pick per body stat (five: a row's maximum). The id carries the Plynling, never
    /// a count: the service re-checks what is left at the click, so a stale button finds none.
    /// </summary>
    public static MessageComponent PointButtons(int plynlingId)
    {
        var row = new ActionRowBuilder();
        foreach (var stat in PlynlingStats.Body)
            row.WithButton($"+1 {PlynlingStats.Name(stat)}", $"rpg:point:{plynlingId}:{stat}", ButtonStyle.Primary,
                new Emoji(PlynlingStats.Emoji(stat)));
        return new ComponentBuilder().AddRow(row).Build();
    }

    /// <summary>
    /// The point buttons under « Personnalité », for its owner only and only while points wait (alive, ado or
    /// older): the way back to them once a drill's answer is dismissed. Null otherwise.
    /// </summary>
    public static MessageComponent? OwnerPointButtons(Plynling p, ulong viewerId, DateTimeOffset now) =>
        p.OwnerId == viewerId && p.DiedAt is null && PlynlingRpg.CanTrain(p, now) && PlynlingRpg.UnspentPoints(p) > 0
            ? PointButtons(p.Id)
            : null;

    /// <summary>The answer to a point placed: her line, the stat's new total, any badge it earned, and what is left.</summary>
    public static (string Text, MessageComponent? Buttons) PointReply(Plynling p, IReadOnlyList<TraitInfo> traits, PlynlingStat stat,
        Func<string[], string> pick, IReadOnlyList<BadgeInfo>? badges = null)
    {
        var total = PlynlingStats.Compute(p, traits).Single(l => l.Stat == stat).Total;
        var text = string.Format(pick(BotResponses.PlynlingPointLines.For(p.Gender)), PlynlingCardUi.SafeName(p.Name))
                   + $"\n{PlynlingStats.Emoji(stat)} {PlynlingStats.Name(stat)} **{total}**"
                   + (badges is { Count: > 0 } ? "\n" + PlynlingBadges.NewBadgeLines(badges, p.Gender) : "");
        var left = PlynlingRpg.UnspentPoints(p);
        return left > 0 ? (text + $"\n**{PlynlingRpg.PointsToPlace(left)}** : choisis où.", PointButtons(p.Id)) : (text, null);
    }

    /// <summary>The public level-up line, posted in the channel (mentions off by the caller).</summary>
    public static string LevelUpLine(Plynling p, int level, Func<string[], string> pick) =>
        string.Format(pick(BotResponses.PlynlingLevelUpLines.For(p.Gender)), PlynlingCardUi.SafeName(p.Name), level);
}
