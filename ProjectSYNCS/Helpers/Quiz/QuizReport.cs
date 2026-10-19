using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

public sealed record QuizGroupLine(string Label, int Posted, int Found);

/// <summary>What /report quiz shows. Built by <see cref="QuizReport.Compute"/>.</summary>
public sealed record QuizReportData(
    int Posted,
    int Found,
    int Unanswered,
    IReadOnlyList<QuizGroupLine> ByCategory,
    IReadOnlyList<QuizGroupLine> ByDifficulty,
    int? MedianSeconds,
    PersonTally? Fastest,
    IReadOnlyList<string> UnfoundPrompts,
    IReadOnlyList<PersonTally> Winners);

/// <summary>
/// The quiz over one window, from its QuizRound rows (filtered on Day in SQL by the caller). A round
/// whose card never went out (MessageId 0) is not a question. Category and difficulty are not
/// stored: they come from QuizBank by key (a key no longer in the bank is left out of those two
/// lists). Wrong answers are never stored, so there is no « hardest by attempts ». Pure.
/// </summary>
public static class QuizReport
{
    public const int ListedPrompts = 5;

    public static QuizReportData Compute(IReadOnlyList<QuizRound> rounds)
    {
        var posted = rounds.Where(r => r.MessageId != 0).ToList();
        var found = posted.Where(r => r.WinnerId != 0 && r.WonAt is not null).ToList();
        var unanswered = posted.Where(r => r.Closed && r.WinnerId == 0).ToList();
        var known = posted.Select(r => (Round: r, Question: QuizBank.ByKey(r.QuestionKey)))
            .Where(x => x.Question is not null).ToList();

        IReadOnlyList<QuizGroupLine> By<TKey>(Func<QuizQuestion, TKey> key, Func<TKey, string> label) where TKey : notnull =>
            known.GroupBy(x => key(x.Question!))
                .Select(g => new QuizGroupLine(label(g.Key), g.Count(), g.Count(x => x.Round.WinnerId != 0)))
                .OrderByDescending(l => l.Posted)
                .ToList();

        var times = found
            .Select(r => (Round: r, Ms: (int)(r.WonAt!.Value - r.PostedAt).TotalMilliseconds))
            .OrderBy(x => x.Ms)
            .ToList();

        return new QuizReportData(
            posted.Count, found.Count, unanswered.Count,
            By<QuizCategory>(q => q.Category, QuizBank.Label),
            By<QuizDifficulty>(q => q.Difficulty, QuizBank.Label),
            times.Count == 0 ? null : times[times.Count / 2].Ms / 1000,
            times.Count == 0 ? null : new PersonTally(times[0].Round.WinnerId, times[0].Ms, 0),
            unanswered.Select(r => QuizBank.ByKey(r.QuestionKey)?.Prompt).OfType<string>().Distinct().Take(ListedPrompts).ToList(),
            found.GroupBy(r => r.WinnerId)
                .Select(g => new PersonTally(g.Key, g.Count(), posted.Count))
                .OrderByDescending(x => x.Count)
                .Take(SessionReport.ListSize)
                .ToList());
    }
}
