using System.Globalization;

namespace ProjectSYNCS.Helpers;

// The two things she can read off her own body: the CPU's temperature and how long the
// machine has been up. Linux files, readable from inside the add-on's container (/sys and /proc are
// the host's). Everywhere else — dev on Windows, a file missing or unreadable — the answer is
// null and the caller carries on without it.
public static class PiHardware
{
    private const string ThermalPath = "/sys/class/thermal/thermal_zone0/temp";
    private const string UptimePath = "/proc/uptime";

    /// <summary>The CPU temperature in thousandths of a degree Celsius, or null.</summary>
    public static int? TryReadCpuMilliCelsius()
    {
        try
        {
            if (!File.Exists(ThermalPath)) return null;
            var text = File.ReadAllText(ThermalPath).Trim();
            return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var milli) ? milli : null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// How long the machine has been up: the host's uptime, even inside a container (/proc/uptime
    /// isn't namespaced). Null when it can't be read.
    /// </summary>
    public static TimeSpan? TryReadHostUptime()
    {
        try
        {
            if (!File.Exists(UptimePath)) return null;
            // "12345.67 54321.00": seconds up, then seconds idle summed over the cores.
            var first = File.ReadAllText(UptimePath).Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
            return double.TryParse(first, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds)
                ? TimeSpan.FromSeconds(seconds)
                : null;
        }
        catch
        {
            return null;
        }
    }
}
