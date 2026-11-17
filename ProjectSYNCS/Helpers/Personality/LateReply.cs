namespace ProjectSYNCS.Helpers;

/// <summary>
/// A reply to one of her messages that is hours or days old: she notices, now and then, and says
/// when she wrote it (« hier », « il y a 3 jours »). About her message's age, never about where the
/// person was. Pure, so the phrasing can be checked without a gateway.
/// </summary>
public static class LateReply
{
    /// <summary>Younger than this, a reply is just a reply.</summary>
    public static readonly TimeSpan MinAge = TimeSpan.FromHours(6);

    /// <summary>How often a late reply that would get a stock comeback gets the remark instead.</summary>
    public const double Chance = 0.5;

    /// <summary>
    /// When she wrote the message, as French words that fit « J'ai dit ça {2} »: « il y a 7 heures »
    /// the same Paris day, then « hier », « il y a 3 jours », « la semaine dernière », « il y a 2
    /// semaines », « il y a des mois ». Null when it isn't old enough to be late.
    /// </summary>
    public static string? Ago(DateTimeOffset written, DateTimeOffset now)
    {
        var age = now - written;
        if (age < MinAge) return null;
        var days = AppTime.DayNumber(now) - AppTime.DayNumber(written);
        return days switch
        {
            <= 0 => $"il y a {(int)age.TotalHours} heures",
            1 => "hier",
            < 7 => $"il y a {days} jours",
            < 14 => "la semaine dernière",
            < 60 => $"il y a {days / 7} semaines",
            _ => "il y a des mois",
        };
    }
}
