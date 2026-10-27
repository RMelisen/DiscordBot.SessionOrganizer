using System.Globalization;
using System.Text;
using System.Text.Json;

namespace ProjectSYNCS.Helpers;

/// <summary>
/// What the body says of itself when /debug minecraft asks (« Voir son corps », the body's <c>debug</c> event,
/// <c>lib/debug.js</c>): what she's doing, her health, the process, and the workarounds of <c>lib/compat.js</c>. Pure,
/// so the text can be measured against Discord's 2000.
/// </summary>
public static class McBodyReport
{
    /// <summary>The owner's text for one <c>debug</c> event. Player names and ids come from the game: in code spans.</summary>
    public static string Format(JsonElement e)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"🩺 **Son corps** — Node {Code(Str(e, "node") ?? "?")}, client {Code(Str(e, "version") ?? "?")}, pid {Int(e, "pid")}, " +
                      $"en marche depuis {Duration(Int(e, "uptime") ?? 0)}, {Int(e, "rss")} Mo de mémoire");

        var where = e.TryGetProperty("pos", out var pos) && pos.ValueKind == JsonValueKind.Object
            ? $"{Code($"{Int(pos, "x")} {Int(pos, "y")} {Int(pos, "z")}")}, {Code(Str(e, "dimension") ?? "?")}"
            : "inconnue";
        sb.AppendLine($"Position : {where} · Vie {Num(e, "health")}/20 · Faim {Num(e, "food")}/20 · Air {Num(e, "oxygen")}/20");

        if (e.TryGetProperty("task", out var task) && task.ValueKind == JsonValueKind.Object)
        {
            var line = $"Tâche : {Code(Str(task, "kind") ?? "?")}";
            if (Bool(task, "own")) line += " (la sienne)";
            else if (Str(task, "player") is { Length: > 0 } player) line += $" pour {Code(player)}";
            var details = new List<string>();
            if (Str(task, "mode") is { } mode) details.Add($"mode {Code(mode)}");
            if (Str(task, "resume") is { } resume) details.Add($"reprend ensuite {Code(resume)}");
            if (details.Count > 0) line += $" ({string.Join(", ", details)})";
            sb.AppendLine(line);
        }
        else sb.AppendLine("Tâche : aucune");

        var state = new List<string>();
        if (Int(e, "heldFor") is int held && held > 0) state.Add($"tenue sur place encore {Duration(held)}");
        if (Bool(e, "sitting")) state.Add("assise");
        if (Bool(e, "eating")) state.Add("mange");
        if (Bool(e, "sleeping")) state.Add("dort");
        if (Str(e, "swim") is { } swim) state.Add($"nage ({Code(swim)})");
        if (Bool(e, "riding")) state.Add("en bateau");
        if (e.TryGetProperty("bridge", out var bridge) && bridge.ValueKind == JsonValueKind.Object)
        {
            if (Bool(bridge, "bridging")) state.Add("prête à poser un pont");
            if (Int(bridge, "standing") is int standing && standing > 0)
                state.Add(standing == 1 ? "un bloc de pont à enlever" : $"{standing} blocs de pont à enlever");
        }
        sb.AppendLine($"État : {(state.Count == 0 ? "rien de particulier" : string.Join(", ", state))}");

        var goal = Str(e, "goal");
        sb.AppendLine(Bool(e, "moving") ? $"Chemin : en marche ({Code(goal ?? "?")})"
            : goal is not null ? $"Chemin : objectif {Code(goal)}, à l'arrêt"
            : "Chemin : aucun objectif");

        if (e.TryGetProperty("loop", out var loop) && loop.ValueKind == JsonValueKind.Object)
            sb.AppendLine($"Boucle Node : retard médian {Int(loop, "p50")} ms, 99e centile {Int(loop, "p99")} ms, " +
                          $"max {Int(loop, "max")} ms (depuis la dernière consultation)");

        if (e.TryGetProperty("compat", out var compat) && compat.ValueKind == JsonValueKind.Object)
        {
            var tickEnd = Flag(compat, "tickEnd") switch { true => "envoyés", false => "inutiles", null => "pas encore su" };
            var actions = Flag(compat, "renumberedActions") switch { true => "par nom", false => "telles quelles", null => "pas encore su" };
            sb.AppendLine($"Contournements (lib/compat.js) : tick_end {tickEnd}, entity_action {actions}");
            var peak = Int(compat, "packetsPeak") ?? 0;
            var peakAt = compat.TryGetProperty("packetsPeakAt", out var at) && at.ValueKind == JsonValueKind.Number
                ? $" ({Stamp(DateTimeOffset.FromUnixTimeMilliseconds(at.GetInt64()))})" : "";
            sb.AppendLine($"Corrections du serveur en marchant : {Int(compat, "corrections") ?? 0} · Pic de paquets : {peak}/s{peakAt}");
        }
        return sb.ToString().TrimEnd();
    }

    /// <summary>« 3 h 05 min », « 4 min », « 12 s ».</summary>
    public static string Duration(int seconds)
    {
        if (seconds < 60) return $"{seconds} s";
        var t = TimeSpan.FromSeconds(seconds);
        return t.TotalHours >= 1 ? $"{(int)t.TotalHours} h {t.Minutes:00} min" : $"{t.Minutes} min";
    }

    private static string? Str(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static int? Int(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? (int)Math.Round(v.GetDouble()) : null;

    private static string Num(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number
            ? Math.Round(v.GetDouble(), 1).ToString(CultureInfo.GetCultureInfo("fr-FR")) : "?";

    private static bool Bool(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.True;

    private static bool? Flag(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) ? v.ValueKind switch { JsonValueKind.True => true, JsonValueKind.False => false, _ => null } : null;

    private static string Stamp(DateTimeOffset at) => $"<t:{at.ToUnixTimeSeconds()}:R>";

    // Inside a code span nothing is markdown: only a backtick can break out of one.
    private static string Code(string text) => $"`{text.Replace('`', '\'')}`";
}
