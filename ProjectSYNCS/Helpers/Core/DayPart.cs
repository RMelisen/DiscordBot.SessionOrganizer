namespace ProjectSYNCS.Helpers;

/// <summary>
/// « Bonjour » or « bonsoir »: which greeting the hour calls for, in Paris time. French switches
/// to « bonsoir » around the end of the afternoon; she keeps it until 3 a.m., and anything later
/// is already the next morning to her (the owner's call).
/// </summary>
public enum DayPart
{
    Day,
    Evening,
}

public static class DayParts
{
    /// <summary>From here « bonjour »…</summary>
    public static readonly TimeSpan DayStart = TimeSpan.FromHours(3);

    /// <summary>…and from here « bonsoir », until the next <see cref="DayStart"/>.</summary>
    public static readonly TimeSpan EveningStart = TimeSpan.FromHours(18);

    public static DayPart At(DateTimeOffset instant)
    {
        var time = AppTime.ToZoned(instant).TimeOfDay;
        return time >= DayStart && time < EveningStart ? DayPart.Day : DayPart.Evening;
    }

    public static DayPart Now => At(DateTimeOffset.UtcNow);

    /// <summary>From here until <see cref="EveningStart"/> it is broad daylight: a « bonne nuit »
    /// then is a nap. Earlier than this, someone going to bed at 5 a.m. is just very late.</summary>
    public static readonly TimeSpan DaylightStart = TimeSpan.FromHours(8);

    public static bool IsDaylight(DateTimeOffset instant)
    {
        var time = AppTime.ToZoned(instant).TimeOfDay;
        return time >= DaylightStart && time < EveningStart;
    }
}

/// <summary>
/// The moments of the week that have a flavour of their own: the Monday morning groan, Friday
/// evening's « bon week-end », the weekend, Saturday night, Sunday evening's « demain c'est lundi ».
/// Each sits wholly inside one <see cref="DayPart"/>, so its lines greet like that half.
/// </summary>
public enum WeekMoment
{
    None,
    MondayMorning,
    FridayEvening,
    WeekendDay,
    SaturdayEvening,
    SundayEvening,
}

public static class WeekMoments
{
    /// <summary>Monday morning ends at noon; the afternoon is just a weekday.</summary>
    public static readonly TimeSpan MondayMorningEnd = TimeSpan.FromHours(12);

    /// <summary>
    /// The moment of the week at <paramref name="instant"/>, in Paris time. The small hours belong
    /// to the evening before, as « bonsoir » does: 1 a.m. on Saturday is still Friday evening.
    /// </summary>
    public static WeekMoment At(DateTimeOffset instant)
    {
        var zoned = AppTime.ToZoned(instant);
        var day = (zoned - DayParts.DayStart).DayOfWeek;
        var evening = DayParts.At(instant) == DayPart.Evening;
        return (day, evening) switch
        {
            (DayOfWeek.Monday, false) when zoned.TimeOfDay < MondayMorningEnd => WeekMoment.MondayMorning,
            (DayOfWeek.Friday, true) => WeekMoment.FridayEvening,
            (DayOfWeek.Saturday or DayOfWeek.Sunday, false) => WeekMoment.WeekendDay,
            (DayOfWeek.Saturday, true) => WeekMoment.SaturdayEvening,
            (DayOfWeek.Sunday, true) => WeekMoment.SundayEvening,
            _ => WeekMoment.None,
        };
    }
}

/// <summary>
/// A pool whose greetings follow the clock: the lines that fit any hour, plus the « bonjour »
/// lines by day or the « bonsoir » ones by evening (<see cref="DayParts"/>). Both joined arrays
/// are built once, so a pick costs nothing more than a plain pool's, and
/// <c>ResponsePicker</c> remembers a line whichever half it arrived in.
/// </summary>
/// <remarks>
/// A line that greets (« Bonjour {0} ! », « Bonne soirée ! ») goes in <see cref="Day"/> or
/// <see cref="Evening"/>, ideally with a twin in the other; « dire bonjour » (saying hello, at any
/// hour) and jokes about mixing the two up stay in <see cref="AnyTime"/>. Checks section 45 holds
/// every pool of this type to that.
/// </remarks>
public sealed class TimedPool<T>
{
    public T[] AnyTime { get; }
    public T[] Day { get; }
    public T[] Evening { get; }

    private readonly T[] _byDay;
    private readonly T[] _byEvening;

    public TimedPool(T[] anyTime, T[] day, T[] evening)
    {
        (AnyTime, Day, Evening) = (anyTime, day, evening);
        _byDay = [.. anyTime, .. day];
        _byEvening = [.. anyTime, .. evening];
    }

    /// <summary>The lines that fit the hour of <paramref name="instant"/>.</summary>
    public T[] At(DateTimeOffset instant) => DayParts.At(instant) == DayPart.Day ? _byDay : _byEvening;

    /// <summary>The lines that fit right now.</summary>
    public T[] Now => At(DateTimeOffset.UtcNow);
}
