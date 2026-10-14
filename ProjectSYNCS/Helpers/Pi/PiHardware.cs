using System.Globalization;

namespace ProjectSYNCS.Helpers;

// What she can read off her own body: the CPU's temperature and frequency, how long the machine
// has been up, its load and memory, and the firmware's power and throttling flags. Linux files,
// readable from inside the add-on's container (/sys and /proc are the host's). Everywhere else —
// dev on Windows, a file missing or unreadable — the answer is null and the caller carries on
// without it.
public static class PiHardware
{
    private const string ThermalPath = "/sys/class/thermal/thermal_zone0/temp";
    private const string UptimePath = "/proc/uptime";
    private const string LoadPath = "/proc/loadavg";
    private const string MemInfoPath = "/proc/meminfo";
    private const string CpuFreqPath = "/sys/devices/system/cpu/cpu0/cpufreq/scaling_cur_freq";
    private const string PlatformRoot = "/sys/devices/platform";
    private const string HwmonRoot = "/sys/class/hwmon";

    // Where the firmware's get_throttled lives depends on the board's device tree
    // (soc/soc:firmware on a Pi 4, soc@107c000000/soc@107c000000:firmware on a Pi 5): found once.
    private static readonly Lazy<string?> ThrottledPath = new(FindThrottledPath);

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

    /// <summary>The load average over 1, 5 and 15 minutes (the machine's, like the uptime), or null.</summary>
    public static (double One, double Five, double Fifteen)? TryReadLoadAverage()
    {
        try
        {
            if (!File.Exists(LoadPath)) return null;
            // "0.41 0.38 0.30 1/213 4567"
            var parts = File.ReadAllText(LoadPath).Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3) return null;
            return double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var one)
                && double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var five)
                && double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var fifteen)
                    ? (one, five, fifteen)
                    : null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>The machine's memory in bytes: total, and available to a new program. Null when unreadable.</summary>
    public static (long Total, long Available)? TryReadMemory()
    {
        try
        {
            if (!File.Exists(MemInfoPath)) return null;
            long? total = null, available = null;
            // "MemTotal:        8245384 kB"
            foreach (var line in File.ReadLines(MemInfoPath))
            {
                if (line.StartsWith("MemTotal:")) total = ReadKiB(line);
                else if (line.StartsWith("MemAvailable:")) available = ReadKiB(line);
                if (total is not null && available is not null) break;
            }
            return total is { } t && available is { } a ? (t, a) : null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>The first core's current frequency in MHz, or null.</summary>
    public static int? TryReadCpuMegahertz()
    {
        try
        {
            if (!File.Exists(CpuFreqPath)) return null;
            // In kHz.
            return int.TryParse(File.ReadAllText(CpuFreqPath).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var khz)
                ? khz / 1000
                : null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// The firmware's power and throttling flags, decoded. Read from get_throttled when the
    /// firmware exposes it; failing that, the rpi_volt sensor's under-voltage alarm, which only
    /// knows « right now ». Null when neither can be read.
    /// </summary>
    public static PowerReading? TryReadPower()
    {
        try
        {
            if (ThrottledPath.Value is { } path)
            {
                // Hexadecimal, without a prefix: "50005".
                var text = File.ReadAllText(path).Trim();
                if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) text = text[2..];
                if (int.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var bits))
                    return PiHealth.DecodeThrottled(bits);
            }

            if (!Directory.Exists(HwmonRoot)) return null;
            foreach (var dir in Directory.EnumerateDirectories(HwmonRoot))
            {
                var name = Path.Combine(dir, "name");
                var alarm = Path.Combine(dir, "in0_lcrit_alarm");
                if (File.Exists(name) && File.ReadAllText(name).Trim() == "rpi_volt" && File.Exists(alarm))
                    return new PowerReading(File.ReadAllText(alarm).Trim() == "1", null, null, null);
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    private static long? ReadKiB(string line)
    {
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2 && long.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var kib)
            ? kib * 1024
            : null;
    }

    // Two levels under /sys/devices/platform, no deeper: the firmware node sits there on every
    // Pi, and the tree below is large and full of symlink loops.
    private static string? FindThrottledPath()
    {
        try
        {
            if (!Directory.Exists(PlatformRoot)) return null;
            foreach (var first in Directory.EnumerateDirectories(PlatformRoot))
            {
                var direct = Path.Combine(first, "get_throttled");
                if (File.Exists(direct)) return direct;
                foreach (var second in Directory.EnumerateDirectories(first))
                {
                    if (!Path.GetFileName(second).EndsWith("firmware", StringComparison.Ordinal)) continue;
                    var nested = Path.Combine(second, "get_throttled");
                    if (File.Exists(nested)) return nested;
                }
            }
        }
        catch
        {
            // Unreadable sysfs: no flags, the hwmon fallback may still answer.
        }
        return null;
    }
}
