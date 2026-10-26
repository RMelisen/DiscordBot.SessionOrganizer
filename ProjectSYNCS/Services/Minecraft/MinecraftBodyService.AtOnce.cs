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

// Answered at once, taking her from nobody: what she carries, reminders. Part of MinecraftBodyService.
public sealed partial class MinecraftBodyService
{
    // ---- What she carries -----------------------------------------------------------------------

    // « t'as quoi sur toi ? », « t'as combien de fer ? »: read from the body's last report, nothing sent to it.
    private void TellInventory(string player, bool whispered, McGoods goods)
    {
        McInventory? inventory;
        lock (_gate) inventory = _inventory;
        if (inventory is null) return; // reported on spawn: only before it, when nobody can ask yet
        if (goods == McGoods.All)
        {
            if (inventory.Overview(ListMax) is { } all) Reply(player, whispered, BotResponses.McInventoryLines, all);
            else Reply(player, whispered, BotResponses.McInventoryEmptyLines);
            return;
        }
        if (inventory.Of(goods, ListMax) is { } some) Reply(player, whispered, BotResponses.McHaveLines, some);
        else Reply(player, whispered, BotResponses.McHaveNoneLines, BotResponses.McGoodsNames[goods]);
    }

    private static McInventory ReadInventory(JsonElement root)
    {
        static McStack? Stack(JsonElement e) => e.ValueKind != JsonValueKind.Object ? null
            : new McStack(e.GetProperty("name").GetString() ?? "", e.GetProperty("count").GetInt32(),
                e.GetProperty("goods").EnumerateArray().Select(g => g.GetString() ?? "").ToArray());
        static McStack[] Stacks(JsonElement root, string name) =>
            root.TryGetProperty(name, out var list) && list.ValueKind == JsonValueKind.Array
                ? list.EnumerateArray().Select(Stack).OfType<McStack>().ToArray()
                : [];
        return new McInventory(Stacks(root, "worn"), root.TryGetProperty("held", out var held) ? Stack(held) : null,
            root.TryGetProperty("offhand", out var off) ? Stack(off) : null, Stacks(root, "pockets"));
    }

    // ---- Reminders ------------------------------------------------------------------------------

    private void StartTimer(string player, bool whispered, int seconds, string? note)
    {
        if (TimeSpan.FromSeconds(seconds) > MaxTimer)
        {
            Reply(player, whispered, BotResponses.McTimerTooLongLines);
            return;
        }
        seconds = Math.Max(seconds, (int)MinTimer.TotalSeconds);
        MinecraftBodyProcess? body;
        lock (_gate)
        {
            body = _spawned && !_leaving ? _body : null;
            if (body is null) return;
            var running = _timers.GetValueOrDefault(player);
            if (running >= MaxTimers) body = null;
            else _timers[player] = running + 1;
        }
        if (body is null)
        {
            Reply(player, whispered, BotResponses.McTimerFullLines);
            return;
        }
        Reply(player, whispered, BotResponses.McTimerSetLines, DurationText(seconds));
        _ = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(seconds));
            bool fire;
            lock (_gate)
            {
                // Another body since: its timers were cleared, this one died with the old one.
                if (!ReferenceEquals(_body, body)) return;
                if (_timers.TryGetValue(player, out var left))
                {
                    if (left <= 1) _timers.Remove(player);
                    else _timers[player] = left - 1;
                }
                fire = _players.Contains(player, StringComparer.OrdinalIgnoreCase);
            }
            if (!fire) return;
            if (note is null) Reply(player, whispered, BotResponses.McTimerDoneLines, DurationText(seconds));
            else Reply(player, whispered, BotResponses.McTimerNoteLines, note);
        });
    }

    // « 30 secondes », « 1 minute », « 5 minutes », « 1 heure », « 1 h 30 ».
    private static string DurationText(int seconds)
    {
        if (seconds < 60) return $"{seconds} secondes";
        if (seconds < 3600)
        {
            var minutes = (int)Math.Round(seconds / 60.0);
            return minutes == 1 ? "1 minute" : $"{minutes} minutes";
        }
        var hours = seconds / 3600;
        var rest = (seconds % 3600) / 60;
        return rest > 0 ? $"{hours} h {rest:00}" : hours == 1 ? "1 heure" : $"{hours} heures";
    }
}
