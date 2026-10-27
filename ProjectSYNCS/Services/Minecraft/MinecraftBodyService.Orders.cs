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

// Chat read as orders (McCommand), who holds her (McClaim), and what's answered at once. Part of MinecraftBodyService.
public sealed partial class MinecraftBodyService
{
    // ---- Orders ---------------------------------------------------------------------------------

    private void OnChat(string player, string text, bool whispered)
    {
        if (player.Length == 0) return;
        // A link code (/minecraft link), whispered: proof, not talk. A wrong one gets nothing.
        if (whispered && McLinkCodes.ReadCode(text) is { } code)
        {
            if (LinkCodes.Redeem(player, code, DateTimeOffset.UtcNow) is { } userId) _ = SaveLinkAsync(userId, player);
            return;
        }
        bool called;
        bool answered;
        string[] known;
        Dictionary<string, string> owners;
        lock (_gate)
        {
            called = _calledAt.Remove(player, out var calledAt) && DateTimeOffset.UtcNow - calledAt <= CallWindow;
            answered = _answeredAt.TryGetValue(player, out var answeredAt) && DateTimeOffset.UtcNow - answeredAt <= CallWindow;
            if (!answered) _answeredAt.Remove(player);
            known = _places.Values.SelectMany(p => p.Keys).Distinct().ToArray();
            owners = OwnerNamesLocked();
        }
        var request = McCommand.Parse(text, [_username, .. McCommand.HerNames], called, known, owners.Keys);
        if (request.Of is { } named) request = request with { Of = owners[named] };
        switch (request.Order)
        {
            case McOrder.None:
                // Not an order: maybe talk (to her, about her, or right after she answered them). A refused order stays silent.
                if (!request.Refused) Talk(player, whispered, text, call: false, listening: called || answered);
                return;
            case McOrder.Unknown:
                // Her name and something she reads as talk (« syncs ça va ? »): answered as such. Otherwise a call.
                if (Talk(player, whispered, text, call: true, listening: false)) return;
                lock (_gate) _calledAt[player] = DateTimeOffset.UtcNow;
                if (!_callGate.TryClaim(player)) return;
                // Called by several players at once: the others get a wave, so they still know she heard (the window opens).
                if (FirstOfBurst(player, McOrder.Unknown))
                    Reply(player, whispered, Kin(player, BotResponses.McCalledLines, BotResponses.McCalledLinesOwner, BotResponses.McCalledLinesTata));
                else Send(new { op = "wave", player });
                return;
        }
        var stops = request.Order is McOrder.Stay or McOrder.Sit or McOrder.Free;
        if (!stops && !_orderGate.TryClaim(player)) return;
        if (AnswerAtOnce(player, whispered, text, request)) return;

        // A remembered place becomes coordinates here: the body only walks.
        McPlace? place = null;
        var placeOwner = player;
        if (request.Order is McOrder.GoToPlace or McOrder.Guide || (request.Order == McOrder.Patrol && request.Place is not null))
        {
            if (FindPlace(player, request.Place ?? "", request.Of, out var key) is not { } found)
            {
                Reply(player, whispered, BotResponses.McPlaceUnknownLines, Label(McCommand.PlaceName(text, key), request.Of ?? player, player));
                return;
            }
            (place, placeOwner) = found;
        }

        // The camp is family's to order, and there's one at most.
        if (request.Order is McOrder.Camp or McOrder.Uncamp)
        {
            McCamp? camp;
            lock (_gate) camp = _camp;
            if (KinOf(player) == McKin.Anyone)
            {
                Reply(player, whispered, BotResponses.McCampRefusedLines);
                return;
            }
            if (request.Order == McOrder.Camp && camp is not null)
            {
                Reply(player, whispered, BotResponses.McCampExistsLines);
                return;
            }
            if (request.Order == McOrder.Uncamp && camp is null)
            {
                Reply(player, whispered, BotResponses.McUncampNoneLines);
                return;
            }
        }

        object command = request.Order switch
        {
            McOrder.Come => new { op = "come", player },
            McOrder.Follow => new { op = "follow", player },
            McOrder.GoTo => request.Coordinates! is [var x, var z]
                ? new { op = "goto", player, x, y = (int?)null, z }
                : new { op = "goto", player, x = request.Coordinates![0], y = (int?)request.Coordinates[1], z = request.Coordinates[2] },
            McOrder.GoToPlace => new { op = "goto", player, x = place!.X, y = (int?)place.Y, z = place.Z, dimension = place.Dimension },
            McOrder.Guide => new { op = "guide", player, x = place!.X, y = place.Y, z = place.Z, dimension = place.Dimension },
            McOrder.Guard => new { op = "guard", player },
            McOrder.Patrol => new { op = "patrol", player, spot = place is null ? null : new { x = place.X, y = place.Y, z = place.Z, dimension = place.Dimension } },
            McOrder.Collect => new { op = "collect", player, resource = request.Resource!.Value.ToString().ToLowerInvariant(), count = request.Count },
            McOrder.Sleep => new { op = "sleep", player },
            McOrder.Harvest => new { op = "harvest", player, crop = (request.Crop ?? McCrop.Any).ToString().ToLowerInvariant() },
            McOrder.Farm => new { op = "farm", player },
            McOrder.Ranch => new { op = "ranch", player },
            McOrder.Camp => new { op = "camp", player },
            McOrder.Uncamp => CampCommand(player),
            McOrder.Give => new { op = "give", player, goods = request.Goods!.Value.ToString().ToLowerInvariant() },
            McOrder.Fetch => new { op = "fetch", player, goods = request.Goods!.Value.ToString().ToLowerInvariant(), count = request.Count },
            McOrder.Craft => new { op = "craft", player, item = request.Item!, count = request.Count },
            McOrder.Store => new { op = "store", player },
            McOrder.Fish => new { op = "fish", player },
            McOrder.Smelt => new { op = "smelt", player, what = (request.Smelt ?? McSmelt.Food).ToString().ToLowerInvariant() },
            McOrder.Sit => new { op = "sit" },
            McOrder.Free => new { op = "free", player },
            _ => new { op = "stay" },
        };
        var detail = request.Coordinates is { } coordinates ? string.Join(" ", coordinates)
            : place is not null ? Label(place.Name, placeOwner, player)
            : request.Goods is { } goods ? BotResponses.McGoodsNames[goods]
            : request.Item is { } item ? McCraft.Label(item, request.Count)
            : "";
        var now = DateTimeOffset.UtcNow;
        string? busyWith = null;
        McJob? robbed = null;
        var alreadyFree = false;
        lock (_gate)
        {
            if (_body is not { } body || !_spawned || _leaving) return;
            // « quartier libre » with no order and no « reste » to lift: nothing to let go of.
            if (request.Order == McOrder.Free && _order is null && _pending is null && _doing?.Kind != "held") alreadyFree = true;
            // She works for whoever gave the order under way: only they can stop her or send her elsewhere,
            // unless their claim ran out or family outranks them (McClaim).
            else if (_order is { } holder
                && McClaim.Decide(holder.Player, KinOf(holder.Player), holder.Since, player, KinOf(player), now, LastingLocked(holder))
                == McVerdict.Busy)
                busyWith = holder.Player;
            else
            {
                // An errand (handing over, putting away, fetching from a chest, crafting) pauses an activity, which the body goes back
                // to after: it holds her again then, and its keeper isn't told she left.
                var resume = IsErrand(request.Order) && _order is { } current && IsActivity(current.Order)
                    ? current
                    : null;
                // Taken from someone before their order was done: they hear why, once the new one is under way.
                robbed = resume is null && _order is { } taken && !Same(taken.Player, player)
                         && (LastingLocked(taken) || now - taken.Since < McClaim.Expiry)
                    ? taken
                    : null;
                _seeking = null;
                if (stops)
                {
                    _order = null;
                    _pending = null;
                }
                else _pending = new McJob(request.Order, player, whispered, detail, now, robbed?.Player, robbed?.Private ?? false, resume);
                body.Send(command);
            }
        }
        if (alreadyFree)
        {
            Reply(player, whispered, BotResponses.McFreeAlreadyLines);
            return;
        }
        if (busyWith is not null)
        {
            if (_busyGate.TryClaim(player)) Reply(player, whispered, BotResponses.McBusyLines, Who(busyWith));
            return;
        }
        // Everything but staying is acknowledged when the body reports it under way ("moving",
        // "collecting", "sleeping"): the player may be out of sight or there may be nothing to gather,
        // and « j'arrive » followed by « je te vois pas » reads as a broken promise.
        if (stops)
        {
            if (robbed is not null) Reply(robbed.Player, robbed.Private, BotResponses.McTakenLines, Who(player));
            Ack(player, whispered, request.Order.ToString(), request.Order switch
            {
                McOrder.Sit => BotResponses.McSitLines,
                McOrder.Free => Kin(player, BotResponses.McFreeLines, BotResponses.McFreeLinesOwner, BotResponses.McFreeLinesTata),
                _ => BotResponses.McStayAckLines,
            });
        }
    }

    // Orders that take her from nobody and change nothing she's doing: answered here, or by a body event (where she
    // is, a spot to remember). True when the request was one of them.
    private bool AnswerAtOnce(string player, bool whispered, string text, McRequest request)
    {
        switch (request.Order)
        {
            case McOrder.Timer:
                StartTimer(player, whispered, request.Seconds, request.Note);
                return true;
            case McOrder.Jump:
                if (Send(new { op = "jump" }) && Random.Shared.NextDouble() < JumpChance) Reply(player, whispered, BotResponses.McJumpLines);
                return true;
            case McOrder.Wave:
                if (Send(new { op = "wave", player }) && FirstOfBurst(player, McOrder.Wave))
                    Reply(player, whispered, Kin(player, BotResponses.McWaveLines, BotResponses.McWaveLinesOwner, BotResponses.McWaveLinesTata));
                return true;
            case McOrder.Bye:
                if (Send(new { op = "wave", player }) && FirstOfBurst(player, McOrder.Bye))
                    Reply(player, whispered, Kin(player, BotResponses.McByeLines, BotResponses.McByeLinesOwner, BotResponses.McByeLinesTata));
                return true;
            case McOrder.Praise:
                if (Send(new { op = "jump" }) && FirstOfBurst(player, McOrder.Praise))
                    Reply(player, whispered, Kin(player, BotResponses.McPraisedLines, BotResponses.McPraisedLinesOwner, BotResponses.McPraisedLinesTata));
                return true;
            case McOrder.Thanks:
                // A thank-you has no gesture of its own: the others get the wave.
                if (FirstOfBurst(player, McOrder.Thanks))
                    Reply(player, whispered, Kin(player, BotResponses.McThankedLines, BotResponses.McThankedLinesOwner, BotResponses.McThankedLinesTata));
                else Send(new { op = "wave", player });
                return true;
            case McOrder.Where:
                lock (_gate) _askedWhere[player] = whispered;
                Send(new { op = "where", player });
                return true;
            case McOrder.Dress:
            case McOrder.Undress:
                lock (_gate) _dressing[player] = whispered;
                Send(new { op = request.Order == McOrder.Dress ? "dress" : "undress", player });
                return true;
            case McOrder.Fetch when request.Goods is null:
                Reply(player, whispered, BotResponses.McFetchUnknownLines);
                return true;
            case McOrder.Craft when request.Item is null:
                Reply(player, whispered, BotResponses.McCraftUnknownLines);
                return true;
            case McOrder.Give when request.Goods is null:
                Reply(player, whispered, BotResponses.McGiveUnknownLines);
                return true;
            case McOrder.ListPlaces:
                ListPlaces(player, whispered, request.Of);
                return true;
            case McOrder.Inventory:
            case McOrder.Have:
                TellInventory(player, whispered, request.Goods ?? McGoods.All);
                return true;
            case McOrder.WherePlace:
                if (FindPlace(player, request.Place ?? "", request.Of, out var whereKey) is var (found, owner))
                    Reply(player, whispered, BotResponses.McPlaceWhereLines, Spot(found.X, found.Y, found.Z, found.Dimension),
                        Label(found.Name, owner, player));
                else Reply(player, whispered, BotResponses.McPlaceUnknownLines, Label(McCommand.PlaceName(text, whereKey), request.Of ?? player, player));
                return true;
            case McOrder.ForgetPlace:
                ForgetPlace(player, whispered, text, request.Place ?? "", request.Of);
                return true;
            case McOrder.SavePlace:
                if (request.Place is null)
                {
                    Reply(player, whispered, BotResponses.McPlaceNoNameLines);
                    return true;
                }
                lock (_gate) _marking[player] = (request.Place, McCommand.PlaceName(text, request.Place), request.Of, whispered);
                Send(new { op = "mark", player });
                return true;
            default:
                return false;
        }
    }

    // A command to the body while she's in game. False when she isn't.
    private bool Send(object command)
    {
        lock (_gate)
        {
            if (_body is not { } body || !_spawned || _leaving) return false;
            body.Send(command);
            return true;
        }
    }

    // Farming, fishing, keeping animals and guarding a place last until someone says otherwise; errands and sleep only
    // pause them.
    private static bool IsActivity(McOrder? order) => order is McOrder.Farm or McOrder.Fish or McOrder.Patrol or McOrder.Ranch;

    // Errands: over in a moment (a craft in a few), they pause an activity rather than end it (the body goes back to it
    // after).
    private static bool IsErrand(McOrder order) => order is McOrder.Give or McOrder.Store or McOrder.Fetch or McOrder.Craft;

    // A farm, a pen or a guarded place holds her while its keeper is on the server; every other claim runs out
    // (McClaim.Expiry).
    private bool LastingLocked(McJob job) =>
        job.Order is McOrder.Farm or McOrder.Patrol or McOrder.Ranch && _players.Contains(job.Player, StringComparer.OrdinalIgnoreCase);
}
