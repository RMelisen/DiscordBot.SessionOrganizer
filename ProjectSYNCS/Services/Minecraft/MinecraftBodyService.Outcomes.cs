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

// How her orders went: who's who in her lines, acknowledgements, and every outcome the body reports. Part of
// MinecraftBodyService.
public sealed partial class MinecraftBodyService
{
    private static bool Same(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    private McKin KinOf(string player) => McFamily.Of(player, _owner, _tata);

    // How she calls a player in her lines: « Papa » and « Tata » for family, their in-game name otherwise.
    private string Who(string player) => KinOf(player) switch
    {
        McKin.Papa => "Papa",
        McKin.Tata => "Tata",
        _ => player,
    };

    // The body says this player's order is under way: the pending order becomes hers to work on, and
    // whoever she was taken from hears it. Returns how to answer (whispered, detail).
    private (bool Whispered, string Detail) Confirm(string player)
    {
        McJob? job;
        lock (_gate)
        {
            if (_pending is { } pending && Same(pending.Player, player))
            {
                _order = pending with { Since = DateTimeOffset.UtcNow, Robbed = null };
                _tally.Add(McStats.Orders, 1);
                _tally.Add(McStats.OrdersBy(player), 1);
                _pending = null;
                job = pending;
            }
            else job = _order is { } order && Same(order.Player, player) ? order : null;
        }
        if (job is null) return (false, "");
        if (job.Robbed is { } robbed) Reply(robbed, job.RobbedPrivate, BotResponses.McTakenLines, Who(player));
        return (job.Private, job.Detail);
    }

    // How this player's order went, which ends it: a pending one that failed at once (her previous order
    // goes on), or the one under way.
    private (bool Whispered, string Detail) Finish(string player)
    {
        lock (_gate)
        {
            if (_pending is { } pending && Same(pending.Player, player))
            {
                _pending = null;
                return (pending.Private, pending.Detail);
            }
            if (_order is { } order && Same(order.Player, player))
            {
                // An errand done: the activity it paused holds her again, as the body went back to it.
                _order = order.Resume;
                return (order.Private, order.Detail);
            }
            return (false, "");
        }
    }

    // The order this player gave that's pending or under way, if any.
    private McOrder? JobOf(string player)
    {
        lock (_gate)
            return _pending is { } pending && Same(pending.Player, player) ? pending.Order
                : _order is { } order && Same(order.Player, player) ? order.Order
                : null;
    }

    // Her camp, as the body reports it (camped, or what a half-built one left): kept, so it can be taken down.
    private void KeepCamp(JsonElement root)
    {
        var blocks = root.GetProperty("blocks").EnumerateArray()
            .Select(b => new McCampBlock(b.GetProperty("x").GetInt32(), b.GetProperty("y").GetInt32(), b.GetProperty("z").GetInt32(),
                b.GetProperty("name").GetString() ?? ""))
            .ToList();
        if (blocks.Count == 0) return;
        var at = blocks[blocks.Count / 2];
        var dimension = root.TryGetProperty("dimension", out var d) ? d.GetString() ?? "" : "";
        lock (_gate)
        {
            _camp = new McCamp(root.TryGetProperty("x", out var x) ? x.GetInt32() : at.X, root.TryGetProperty("y", out var y) ? y.GetInt32() : at.Y,
                root.TryGetProperty("z", out var z) ? z.GetInt32() : at.Z, dimension, blocks);
            SaveState(StateLocked());
        }
    }

    // « démonte le camp »: the blocks she placed, back to the body.
    private object CampCommand(string player)
    {
        McCamp? camp;
        lock (_gate) camp = _camp;
        return new
        {
            op = "uncamp", player, dimension = camp?.Dimension ?? "",
            center = camp is null ? null : new { x = camp.X, y = camp.Y, z = camp.Z },
            blocks = (camp?.Blocks ?? []).Select(b => new { x = b.X, y = b.Y, z = b.Z, name = b.Name }).ToArray(),
        };
    }

    // Going after them through a portal: she says so, so they wait rather than think she's lost.
    private void OnCrossing(string player)
    {
        var (whispered, _) = Confirm(player);
        Ack(player, whispered, "portal", Kin(player, BotResponses.McPortalLines, BotResponses.McPortalLinesOwner, BotResponses.McPortalLinesTata));
    }

    private void OnMoving(string player, string kind)
    {
        var job = JobOf(player);
        var (whispered, detail) = Confirm(player);
        switch (kind)
        {
            case "goto" when job == McOrder.GoToPlace:
                Ack(player, whispered, $"place:{detail}", BotResponses.McPlaceGoAckLines, detail);
                break;
            case "goto":
                Ack(player, whispered, $"goto:{detail}", BotResponses.McGoToAckLines, detail);
                break;
            case "guide":
                Ack(player, whispered, $"guide:{detail}", BotResponses.McGuideAckLines, detail);
                break;
            case "guard":
                Ack(player, whispered, "guard", Kin(player, BotResponses.McGuardAckLines, BotResponses.McGuardAckLinesOwner, BotResponses.McGuardAckLinesTata));
                break;
            case "follow":
                Ack(player, whispered, "follow", Kin(player, BotResponses.McFollowAckLines, BotResponses.McFollowAckLinesOwner, BotResponses.McFollowAckLinesTata));
                break;
            default:
                Ack(player, whispered, "come", Kin(player, BotResponses.McComeAckLines, BotResponses.McComeAckLinesOwner, BotResponses.McComeAckLinesTata));
                break;
        }
    }

    // Papa's or Tata's twin of a pool when the player is one of them (McFamily), the shared pool otherwise.
    private string[] Kin(string player, string[] anyone, string[] papa, string[] tata) =>
        KinOf(player) switch
        {
            McKin.Papa => papa,
            McKin.Tata => tata,
            _ => anyone,
        };

    // How an order went (arrived, out of sight, no way there…): answered to whoever gave it, the way they gave it.
    private void OnOutcome(string player, string[] pool, params object[] extra)
    {
        var (whispered, detail) = Finish(player);
        Reply(player, whispered, pool, extra.Length > 0 ? extra : new object[] { detail });
    }

    // Hungry with nothing to eat: she asks, rationed on its own clock; louder once she can't sprint. The asking
    // gate is given back when the remark gates swallow the line, or she'd wait ten minutes for nothing.
    private void OnHungry(int food)
    {
        var starving = food <= StarvingFood;
        var gate = starving ? _starvingGate : _hungryGate;
        if (!gate.TryClaim(0)) return;
        if (!Remark(starving ? "starving" : "hungry", starving ? BotResponses.McStarvingLines : BotResponses.McHungryLines))
            gate.Release(0);
    }

    // Something she lacks, a tool wearing out or one that broke: once every 10 minutes per thing, and the gate is given
    // back when the remark gates swallow the line, as for asking for food.
    private void OnNeed(string key, string[] pool, params object[] extra)
    {
        if (!_needGate.TryClaim(key)) return;
        if (!Remark($"need:{key}", pool, null, false, extra)) _needGate.Release(key);
    }

    private static string[]? NeedPool(string what) => what switch
    {
        "rod" => BotResponses.McNeedRodLines,
        "shears" => BotResponses.McNeedShearsLines,
        "breedFood" => BotResponses.McNeedBreedFoodLines,
        "arrows" => BotResponses.McNeedArrowsLines,
        "space" => BotResponses.McPocketsFullLines,
        _ => null,
    };

    private static string ToolName(string tool) => BotResponses.McToolNames.TryGetValue(tool, out var name) ? name : tool;

    // Where her routine takes her on her own (the body's own task): a word now and then.
    private static string[]? RoutinePool(string? what) => what switch
    {
        "farm" => BotResponses.McRoutineFarmLines,
        "fish" => BotResponses.McRoutineFishLines,
        "ranch" => BotResponses.McRoutineRanchLines,
        "home" => BotResponses.McRoutineHomeLines,
        _ => null,
    };

    // Nothing to do, so she keeps near Papa or Tata (only family: the body tags along with nobody else). Now and then a word.
    private void OnTagAlong(string player)
    {
        var pool = KinOf(player) switch
        {
            McKin.Papa => BotResponses.McTagAlongLinesOwner,
            McKin.Tata => BotResponses.McTagAlongLinesTata,
            _ => null,
        };
        if (pool is not null && Random.Shared.NextDouble() < TagAlongChance) Remark("tagAlong", pool, player);
    }

    // She ran from an attacker in the middle of someone's one-off order (come, go to, gather): it's over,
    // and they hear why, unless they're the attacker. With no attacker, she dropped it to save herself (a monster on
    // her, or on fire): danger in general, nobody blamed.
    private void OnDropped(string player, string attacker)
    {
        var (whispered, _) = Finish(player);
        if (player.Length == 0 || Same(player, attacker)) return;
        if (attacker.Length == 0) Reply(player, whispered, BotResponses.McDangerDroppedLines);
        else Reply(player, whispered, BotResponses.McFleeDroppedLines, Who(attacker));
    }

    // In bed, asked, because a player went to bed, or on her own at night (auto): whatever she was doing is over,
    // whoever it was for, except an activity (a farm, fishing, a guarded place): the body goes back to it when she
    // wakes, and it stays its keeper's. On her own she tells the server, so they know the night can pass.
    private void OnSlept(string player, bool auto)
    {
        McJob? activity;
        lock (_gate) activity = _order is { } kept && IsActivity(kept.Order) ? kept : null;
        if (auto)
        {
            lock (_gate) _order = activity;
            Remark("bedtime", BotResponses.McBedtimeLines);
            return;
        }
        var (whispered, _) = Confirm(player);
        lock (_gate) _order = activity;
        Ack(player, whispered, "sleep", BotResponses.McSleepLines);
    }

    // After arriving she walks up to Papa, or else Tata, when either is on: in sight, or where she last saw them.
    private void SeekFamilyLocked(MinecraftBodyProcess body)
    {
        var target = _players.FirstOrDefault(p => KinOf(p) == McKin.Papa) ?? _players.FirstOrDefault(p => KinOf(p) == McKin.Tata);
        if (target is null || _order is not null || _pending is not null) return;
        _seeking = target;
        var spot = _seen.TryGetValue(target, out var s) ? new { x = s.X, y = s.Y, z = s.Z, dimension = s.Dimension } : null;
        body.Send(new { op = "seek", player = target, spot });
    }

    private void OnSeek(string player, bool met)
    {
        lock (_gate)
        {
            if (_seeking is null || !Same(_seeking, player)) return;
            _seeking = null;
            // Logged out while she was on her way: nobody to talk to.
            if (!_players.Contains(player, StringComparer.OrdinalIgnoreCase)) return;
        }
        var kin = KinOf(player);
        if (kin == McKin.Anyone) return;
        Reply(player, false, (kin, met) switch
        {
            (McKin.Papa, true) => BotResponses.McSeekMetLinesOwner,
            (McKin.Papa, false) => BotResponses.McSeekMissLinesOwner,
            (_, true) => BotResponses.McSeekMetLinesTata,
            _ => BotResponses.McSeekMissLinesTata,
        });
    }

    // Where she last saw someone. Only her family's spots are kept, and written straight away.
    private void OnSeen(string player, McSpot spot)
    {
        if (KinOf(player) == McKin.Anyone || spot.Dimension.Length == 0) return;
        MinecraftState state;
        lock (_gate)
        {
            _seen[player] = spot;
            state = StateLocked();
        }
        SaveState(state);
    }

    private void OnArrived(string player, string kind)
    {
        if (kind == "guide") OnOutcome(player, BotResponses.McGuideArrivedLines);
        else if (kind == "goto" && JobOf(player) == McOrder.GoToPlace) OnOutcome(player, BotResponses.McPlaceArrivedLines);
        else if (kind == "goto") OnOutcome(player, BotResponses.McGoToArrivedLines);
        // Walking up to them says it; the order still ends either way.
        else if (Random.Shared.NextDouble() < ArrivedChance) OnOutcome(player, BotResponses.McArrivedLines);
        else Finish(player);
    }

    // Handed over (the errand's outcome; the activity it paused is hers again, Finish). {1} = how many, {2} = what.
    private void OnGiven(string player, string goodsKey, int count, bool delivered)
    {
        Confirm(player);
        var goods = Goods(goodsKey) ?? McGoods.All;
        OnOutcome(player,
            !delivered ? BotResponses.McGiveLostLines
            : Kin(player, BotResponses.McGiveDoneLines, BotResponses.McGiveDoneLinesOwner, BotResponses.McGiveDoneLinesTata),
            count, BotResponses.McGoodsNames[goods]);
    }

    // Armour put on or taken off as asked: answered to whoever asked, the way they asked. {1} = how many pieces.
    private void OnDressing(string player, string[] pool, int count = 0)
    {
        bool whispered;
        lock (_gate)
            if (!_dressing.Remove(player, out whispered)) return;
        Reply(player, whispered, pool, count);
    }

    private static McGoods? Goods(string key) => Enum.TryParse<McGoods>(key, ignoreCase: true, out var goods) ? goods : null;

    private static string SmeltName(string what) =>
        Enum.TryParse<McSmelt>(what, ignoreCase: true, out var parsed) && BotResponses.McSmeltNames.TryGetValue(parsed, out var name)
            ? name
            : what;

    private void OnCollecting(string player, string resource, int count)
    {
        var (whispered, _) = Confirm(player);
        Ack(player, whispered, $"collect:{resource}", BotResponses.McCollectAckLines, count, ResourceName(resource));
    }

    private void OnCollected(string player, string resource, int count, bool delivered)
    {
        var pool = count == 0 ? BotResponses.McCollectEmptyLines
            : delivered ? Kin(player, BotResponses.McCollectDoneLines, BotResponses.McCollectDoneLinesOwner, BotResponses.McCollectDoneLinesTata)
            : BotResponses.McCollectKeptLines;
        OnOutcome(player, pool, count, ResourceName(resource));
    }

    private static string CropName(string crop) =>
        Enum.TryParse<McCrop>(crop, ignoreCase: true, out var parsed) && BotResponses.McCropNames.TryGetValue(parsed, out var name)
            ? name
            : crop;

    private static string ResourceName(string resource) =>
        Enum.TryParse<McResource>(resource, ignoreCase: true, out var parsed) && BotResponses.McResourceNames.TryGetValue(parsed, out var name)
            ? name
            : resource;

    /// <summary>
    /// A line to one player: in the chat, or whispered back if they whispered. {0} is the player (Who: Papa and Tata by title);
    /// <paramref name="extra"/> fills {1}, {2}… as the pool documents.
    /// </summary>
    private void Reply(string player, bool whispered, string[] pool, params object[] extra)
    {
        var text = string.Format(_picker.Pick(pool, LineChoices), [Who(player), .. extra]);
        lock (_gate)
            if (_body is { } body && _spawned && !_leaving)
                SayLocked(body, whispered ? new { op = "whisper", player, text } : (object)new { op = "say", text }, text);
    }
}
