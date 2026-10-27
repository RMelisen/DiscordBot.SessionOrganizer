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

// What the body reports (one JSON event per line on its stdout), and her arrival. Part of MinecraftBodyService.
public sealed partial class MinecraftBodyService
{
    // ---- What the body reports ------------------------------------------------------------------

    private void OnEvent(string line)
    {
        // The body keeps stdout for events; anything else that slips through is log, not protocol.
        if (!line.StartsWith('{'))
        {
            OnBodyLog(line);
            return;
        }
        try
        {
            using var doc = JsonDocument.Parse(line);
            var root = doc.RootElement;
            switch (root.GetProperty("ev").GetString())
            {
                case "spawned":
                    OnSpawned();
                    break;
                case "version":
                    var version = (root.GetProperty("server").GetString() ?? "", root.GetProperty("using").GetString() ?? "",
                        root.TryGetProperty("fallback", out var fb) && fb.ValueKind == JsonValueKind.True);
                    lock (_gate) _version = version;
                    break;
                case "players":
                    var names = root.GetProperty("names").EnumerateArray().Select(n => n.GetString() ?? "")
                        .Where(n => n.Length > 0 && !n.Equals(_username, StringComparison.OrdinalIgnoreCase)).ToList();
                    lock (_gate) _players = names;
                    break;
                case "system":
                    OnSystem(root.GetProperty("key").GetString() ?? "",
                        root.GetProperty("args").EnumerateArray()
                            .Select(a => new McArg(a.GetProperty("text").GetString() ?? "",
                                a.TryGetProperty("key", out var k) && k.ValueKind == JsonValueKind.String ? k.GetString() : null))
                            .ToList());
                    break;
                case "chat":
                    OnChat(root.GetProperty("player").GetString() ?? "", root.GetProperty("text").GetString() ?? "",
                        root.TryGetProperty("private", out var priv) && priv.ValueKind == JsonValueKind.True);
                    break;
                case "moving":
                    OnMoving(root.GetProperty("player").GetString() ?? "", root.GetProperty("kind").GetString() ?? "");
                    break;
                case "crossing":
                    OnCrossing(root.GetProperty("player").GetString() ?? "");
                    break;
                case "arrived":
                    OnArrived(root.GetProperty("player").GetString() ?? "",
                        root.TryGetProperty("kind", out var arrivedKind) ? arrivedKind.GetString() ?? "" : "");
                    break;
                case "cantReach":
                    var unreached = root.GetProperty("player").GetString() ?? "";
                    OnOutcome(unreached, root.GetProperty("why").GetString() switch
                    {
                        "notVisible" => BotResponses.McTooFarLines,
                        "lost" => JobOf(unreached) == McOrder.Guide ? BotResponses.McGuideLostLines : BotResponses.McLostLines,
                        "tooFar" => BotResponses.McTripTooFarLines,
                        "otherDimension" => BotResponses.McPlaceOtherDimensionLines,
                        _ => BotResponses.McNoPathLines,
                    });
                    break;
                case "waiting":
                    Remark("guideWait", BotResponses.McGuideWaitLines, root.GetProperty("player").GetString());
                    break;
                case "creeper":
                    Remark("creeper", BotResponses.McCreeperLines, root.GetProperty("player").GetString());
                    break;
                case "retreat":
                    Remark("retreat", BotResponses.McRetreatLines, root.GetProperty("player").GetString());
                    break;
                case "totem":
                    // Her totem saved her: said at once, like the moments that can't wait.
                    Remark("totem", BotResponses.McTotemLines, force: true);
                    break;
                case "given":
                    OnGiven(root.GetProperty("player").GetString() ?? "", root.GetProperty("goods").GetString() ?? "",
                        root.GetProperty("count").GetInt32(), root.GetProperty("delivered").GetBoolean());
                    break;
                case "cantGive":
                    var noGoods = Goods(root.GetProperty("goods").GetString() ?? "");
                    OnOutcome(root.GetProperty("player").GetString() ?? "",
                        noGoods == McGoods.All ? BotResponses.McGiveEmptyLines : BotResponses.McGiveNoneLines,
                        "", noGoods is { } g ? BotResponses.McGoodsNames[g] : "");
                    break;
                case "fetching":
                    var fetcher = root.GetProperty("player").GetString() ?? "";
                    var fetchingGoods = root.GetProperty("goods").GetString() ?? "";
                    Ack(fetcher, Confirm(fetcher).Whispered, $"fetch:{fetchingGoods}", BotResponses.McFetchAckLines, GoodsName(fetchingGoods));
                    break;
                case "fetched":
                    OnFetched(root.GetProperty("player").GetString() ?? "", root.GetProperty("goods").GetString() ?? "",
                        root.GetProperty("count").GetInt32(), root.GetProperty("asked").GetInt32(), root.GetProperty("delivered").GetBoolean());
                    break;
                case "cantFetch":
                    var unfetched = root.GetProperty("goods").GetString() ?? "";
                    OnOutcome(root.GetProperty("player").GetString() ?? "",
                        root.GetProperty("why").GetString() == "noChest" ? BotResponses.McFetchNoChestLines : BotResponses.McFetchNoneLines,
                        "", GoodsName(unfetched));
                    break;
                case "dressed":
                    OnDressing(root.GetProperty("player").GetString() ?? "", BotResponses.McDressLines, root.GetProperty("count").GetInt32());
                    break;
                case "cantDress":
                    OnDressing(root.GetProperty("player").GetString() ?? "",
                        root.GetProperty("why").GetString() == "worn" ? BotResponses.McDressWornLines : BotResponses.McArmourNoneLines);
                    break;
                case "undressed":
                    OnDressing(root.GetProperty("player").GetString() ?? "",
                        root.GetProperty("full").GetBoolean() ? BotResponses.McUndressFullLines : BotResponses.McUndressLines,
                        root.GetProperty("count").GetInt32());
                    break;
                case "cantUndress":
                    OnDressing(root.GetProperty("player").GetString() ?? "",
                        root.GetProperty("why").GetString() == "full" ? BotResponses.McUndressFullLines : BotResponses.McArmourNoneLines);
                    break;
                case "armoured":
                    var giver = root.GetProperty("by").GetString() ?? "";
                    if (giver.Length > 0)
                        Remark("armoured", Kin(giver, BotResponses.McArmouredLines, BotResponses.McArmouredLinesOwner,
                            BotResponses.McArmouredLinesTata), giver, force: true);
                    break;
                case "stored":
                    var storer = root.GetProperty("player").GetString() ?? "";
                    Confirm(storer);
                    OnOutcome(storer, root.GetProperty("full").GetBoolean() ? BotResponses.McStoreFullLines : BotResponses.McStoreDoneLines,
                        root.GetProperty("count").GetInt32());
                    break;
                case "cantStore":
                    OnOutcome(root.GetProperty("player").GetString() ?? "",
                        root.GetProperty("why").GetString() == "empty" ? BotResponses.McStoreEmptyLines : BotResponses.McStoreNoChestLines);
                    break;
                case "fishing":
                    var fisher = root.GetProperty("player").GetString() ?? "";
                    Ack(fisher, Confirm(fisher).Whispered, "fish", BotResponses.McFishAckLines);
                    break;
                case "cantFish":
                    OnOutcome(root.GetProperty("player").GetString() ?? "", root.GetProperty("why").GetString() switch
                    {
                        "noRod" => BotResponses.McFishNoRodLines,
                        "broke" => BotResponses.McFishRodBrokeLines,
                        _ => BotResponses.McFishNoWaterLines,
                    });
                    break;
                case "caught":
                    var catchKind = root.GetProperty("kind").GetString();
                    if (catchKind == "fish") _tally.Add(McStats.Fish, 1);
                    else if (catchKind == "treasure") _tally.Add(McStats.Treasure, 1);
                    if (catchKind == "treasure"
                        || Random.Shared.NextDouble() < (catchKind == "fish" ? CatchChance : JunkChance))
                        Remark("catch", catchKind switch
                        {
                            "treasure" => BotResponses.McFishTreasureLines,
                            "fish" => BotResponses.McFishCatchLines,
                            _ => BotResponses.McFishJunkLines,
                        }, root.GetProperty("player").GetString());
                    break;
                case "patrolling":
                    var keeper = root.GetProperty("player").GetString() ?? "";
                    var (patrolPrivate, patrolDetail) = Confirm(keeper);
                    Ack(keeper, patrolPrivate, $"patrol:{patrolDetail}",
                        patrolDetail.Length > 0 ? BotResponses.McPatrolPlaceAckLines : BotResponses.McPatrolAckLines, patrolDetail);
                    break;
                case "cantPatrol":
                    OnOutcome(root.GetProperty("player").GetString() ?? "",
                        root.GetProperty("why").GetString() == "tooFar" ? BotResponses.McTripTooFarLines : BotResponses.McPlaceOtherDimensionLines);
                    break;
                case "smelting":
                    var cook = root.GetProperty("player").GetString() ?? "";
                    var smelting = root.GetProperty("what").GetString() ?? "";
                    Ack(cook, Confirm(cook).Whispered, $"smelt:{smelting}", BotResponses.McSmeltAckLines, root.GetProperty("count").GetInt32(),
                        SmeltName(smelting));
                    break;
                case "smelted":
                    var cooked = root.GetProperty("count").GetInt32();
                    OnOutcome(root.GetProperty("player").GetString() ?? "",
                        cooked == 0 ? BotResponses.McSmeltEmptyLines
                        : root.GetProperty("delivered").GetBoolean() ? BotResponses.McSmeltDoneLines
                        : BotResponses.McSmeltKeptLines,
                        cooked, SmeltName(root.GetProperty("what").GetString() ?? ""));
                    break;
                case "cantSmelt":
                    OnOutcome(root.GetProperty("player").GetString() ?? "", root.GetProperty("why").GetString() switch
                    {
                        "noFuel" => BotResponses.McSmeltNoFuelLines,
                        "noFurnace" => BotResponses.McSmeltNoFurnaceLines,
                        "busy" => BotResponses.McSmeltBusyLines,
                        _ => BotResponses.McSmeltNothingLines,
                    }, "", SmeltName(root.GetProperty("what").GetString() ?? ""));
                    break;
                case "inventory":
                    var inventory = ReadInventory(root);
                    lock (_gate)
                    {
                        _inventory = inventory;
                        _inventoryAt = DateTimeOffset.UtcNow;
                    }
                    break;
                case "doing":
                    var doing = new McBodyDoing(root.GetProperty("kind").GetString() ?? "idle",
                        root.TryGetProperty("player", out var doingFor) ? doingFor.GetString() ?? "" : "",
                        root.TryGetProperty("what", out var doingWhat) ? doingWhat.GetString() ?? "" : "");
                    lock (_gate) _doing = doing;
                    break;
                case "position":
                    var asker = root.GetProperty("player").GetString() ?? "";
                    if (asker == DiscordAsker)
                    {
                        TaskCompletionSource<McSpot>? waiting;
                        lock (_gate) (waiting, _whereFromDiscord) = (_whereFromDiscord, null);
                        waiting?.TrySetResult(new McSpot(root.GetProperty("x").GetInt32(), root.GetProperty("y").GetInt32(),
                            root.GetProperty("z").GetInt32(), root.GetProperty("dimension").GetString() ?? ""));
                        break;
                    }
                    bool whereWhispered;
                    lock (_gate)
                        if (!_askedWhere.Remove(asker, out whereWhispered)) break;
                    var dimension = root.GetProperty("dimension").GetString() ?? "";
                    Reply(asker, whereWhispered, BotResponses.McWhereLines,
                        $"{root.GetProperty("x").GetInt32()} {root.GetProperty("y").GetInt32()} {root.GetProperty("z").GetInt32()}",
                        BotResponses.McDimensionNames[DimensionOf(dimension)]);
                    break;
                case "marked":
                    OnMarked(root.GetProperty("player").GetString() ?? "", root.GetProperty("x").GetInt32(), root.GetProperty("y").GetInt32(),
                        root.GetProperty("z").GetInt32(), root.GetProperty("dimension").GetString() ?? "");
                    break;
                case "cantMark":
                    var unmarked = root.GetProperty("player").GetString() ?? "";
                    bool markWhispered;
                    lock (_gate)
                    {
                        if (!_marking.Remove(unmarked, out var lost)) break;
                        markWhispered = lost.Private;
                    }
                    Reply(unmarked, markWhispered, BotResponses.McTooFarLines);
                    break;
                case "killed":
                    _tally.Add(McStats.Kills, 1);
                    if (Random.Shared.NextDouble() < KillChance)
                        Remark("kill", BotResponses.McGuardKillLines, root.GetProperty("player").GetString());
                    break;
                case "collecting":
                    OnCollecting(root.GetProperty("player").GetString() ?? "", root.GetProperty("resource").GetString() ?? "",
                        root.GetProperty("count").GetInt32());
                    break;
                case "collected":
                    _tally.Add(McStats.Gathered(root.GetProperty("resource").GetString() ?? ""), root.GetProperty("count").GetInt32());
                    OnCollected(root.GetProperty("player").GetString() ?? "", root.GetProperty("resource").GetString() ?? "",
                        root.GetProperty("count").GetInt32(), root.GetProperty("delivered").GetBoolean());
                    break;
                case "cantCollect":
                    OnOutcome(root.GetProperty("player").GetString() ?? "",
                        root.GetProperty("why").GetString() == "noTool" ? BotResponses.McCollectNoToolLines : BotResponses.McCollectNoneLines,
                        "", ResourceName(root.GetProperty("resource").GetString() ?? ""));
                    break;
                case "sleeping":
                    OnSlept(root.GetProperty("player").GetString() ?? "",
                        root.TryGetProperty("auto", out var auto) && auto.ValueKind == JsonValueKind.True);
                    break;
                case "cantSleep":
                    var whyNot = root.GetProperty("why").GetString();
                    OnOutcome(root.GetProperty("player").GetString() ?? "", whyNot switch
                    {
                        "day" => BotResponses.McNotNightLines,
                        "noBed" => BotResponses.McNoBedLines,
                        _ => BotResponses.McBedFailedLines,
                    });
                    // Every bed refused her after she'd dropped what she was doing: she works for nobody now,
                    // unless it was an activity, which the body goes back to.
                    if (whyNot == "failed") lock (_gate) if (!IsActivity(_order?.Order)) _order = null;
                    break;
                case "harvesting":
                    var (harvestPrivate, _) = Confirm(root.GetProperty("player").GetString() ?? "");
                    var harvesting = root.GetProperty("crop").GetString() ?? "";
                    Ack(root.GetProperty("player").GetString() ?? "", harvestPrivate, $"harvest:{harvesting}", BotResponses.McHarvestAckLines,
                        root.GetProperty("count").GetInt32(), CropName(harvesting));
                    break;
                case "farmed":
                    // A farm round's produce (ordered or her own routine): no line, only her stats.
                    _tally.Add(McStats.Harvested, root.GetProperty("count").GetInt32());
                    break;
                case "harvested":
                    var harvested = root.GetProperty("count").GetInt32();
                    _tally.Add(McStats.Harvested, harvested);
                    OnOutcome(root.GetProperty("player").GetString() ?? "",
                        harvested == 0 ? BotResponses.McHarvestEmptyLines
                        : root.GetProperty("delivered").GetBoolean() ? BotResponses.McHarvestDoneLines
                        : BotResponses.McHarvestKeptLines,
                        harvested, CropName(root.GetProperty("crop").GetString() ?? ""));
                    break;
                case "cantHarvest":
                    OnOutcome(root.GetProperty("player").GetString() ?? "", BotResponses.McHarvestNoneLines,
                        "", CropName(root.GetProperty("crop").GetString() ?? ""));
                    break;
                case "farming":
                    var farmer = root.GetProperty("player").GetString() ?? "";
                    Ack(farmer, Confirm(farmer).Whispered, "farm", BotResponses.McFarmAckLines);
                    break;
                case "cantFarm":
                    OnOutcome(root.GetProperty("player").GetString() ?? "",
                        root.GetProperty("why").GetString() == "noChest" ? BotResponses.McFarmNoChestLines : BotResponses.McFarmNoneLines);
                    break;
                case "camping":
                    var camper = root.GetProperty("player").GetString() ?? "";
                    Ack(camper, Confirm(camper).Whispered, "camp", BotResponses.McCampAckLines);
                    break;
                case "camped":
                    KeepCamp(root);
                    // Something missing first; whole but without a bed (nothing to make one from), said apart.
                    var whole = root.TryGetProperty("complete", out var complete) && complete.ValueKind == JsonValueKind.True;
                    var bed = !root.TryGetProperty("bed", out var hasBed) || hasBed.ValueKind == JsonValueKind.True;
                    OnOutcome(root.GetProperty("player").GetString() ?? "",
                        !whole ? BotResponses.McCampPartialLines : bed ? BotResponses.McCampDoneLines : BotResponses.McCampNoBedLines);
                    break;
                case "cantCamp":
                    if (root.TryGetProperty("blocks", out var partial) && partial.GetArrayLength() > 0) KeepCamp(root);
                    var why = root.GetProperty("why").GetString();
                    var missing = root.TryGetProperty("missing", out var short_) && short_.GetArrayLength() > 0
                        ? short_[0].GetString() ?? "" : "";
                    if (why == "noRoom") OnOutcome(root.GetProperty("player").GetString() ?? "", BotResponses.McCampNoRoomLines);
                    else OnOutcome(root.GetProperty("player").GetString() ?? "", BotResponses.McCampMissingLines, "",
                        BotResponses.McCampMissingNames.GetValueOrDefault(missing, "de quoi construire"));
                    break;
                case "uncamping":
                    var uncamper = root.GetProperty("player").GetString() ?? "";
                    Ack(uncamper, Confirm(uncamper).Whispered, "uncamp", BotResponses.McUncampAckLines);
                    break;
                case "uncamped":
                    lock (_gate)
                    {
                        _camp = null;
                        SaveState(StateLocked());
                    }
                    OnOutcome(root.GetProperty("player").GetString() ?? "", BotResponses.McUncampDoneLines);
                    break;
                case "cantUncamp":
                    OnOutcome(root.GetProperty("player").GetString() ?? "",
                        root.GetProperty("why").GetString() == "none" ? BotResponses.McUncampNoneLines : BotResponses.McCampFarLines);
                    break;
                case "ranching":
                    var rancher = root.GetProperty("player").GetString() ?? "";
                    Ack(rancher, Confirm(rancher).Whispered, "ranch", BotResponses.McRanchAckLines);
                    break;
                case "cantRanch":
                    OnOutcome(root.GetProperty("player").GetString() ?? "",
                        root.GetProperty("why").GetString() == "noChest" ? BotResponses.McRanchNoChestLines : BotResponses.McRanchNoneLines);
                    break;
                case "routine":
                    if (RoutinePool(root.GetProperty("what").GetString()) is { } routinePool && Random.Shared.NextDouble() < RoutineChance)
                        Remark("routine", routinePool);
                    break;
                case "tagAlong":
                    OnTagAlong(root.GetProperty("player").GetString() ?? "");
                    break;
                case "retrieving":
                    Remark("retrieving", BotResponses.McRetrieveLines, force: true);
                    break;
                case "retrieved":
                    // Nothing left where she died always gets a word; what she got back, half the time.
                    var retrieved = root.GetProperty("count").GetInt32();
                    if (retrieved == 0) Remark("retrieved", BotResponses.McRetrieveEmptyLines, force: true);
                    else if (Random.Shared.NextDouble() < RetrievedChance) Remark("retrieved", BotResponses.McRetrievedLines, null, false, retrieved);
                    break;
                case "retrieveLost":
                    if (Random.Shared.NextDouble() < RetrieveLostChance) Remark("retrieved", BotResponses.McRetrieveLostLines);
                    break;
                case "creeperBehind":
                    var exposed = root.GetProperty("player").GetString() ?? "";
                    if (exposed.Length > 0 && _creeperGate.TryClaim(exposed))
                        Remark("creeperBehind", Kin(exposed, BotResponses.McCreeperBehindLines, BotResponses.McCreeperBehindLinesOwner,
                            BotResponses.McCreeperBehindLinesTata), exposed, force: true);
                    break;
                case "wornOut":
                    var wornTool = root.GetProperty("tool").GetString() ?? "";
                    OnNeed($"worn:{wornTool}", BotResponses.McToolWornLines, ToolName(wornTool));
                    break;
                case "broke":
                    var brokenTool = root.GetProperty("tool").GetString() ?? "";
                    OnNeed($"broke:{brokenTool}", BotResponses.McToolBrokeLines, ToolName(brokenTool));
                    break;
                case "lacking":
                    var lacked = root.GetProperty("what").GetString() ?? "";
                    if (NeedPool(lacked) is { } needPool) OnNeed(lacked, needPool);
                    break;
                case "diving":
                    var diver = root.GetProperty("player").GetString() ?? "";
                    if (Random.Shared.NextDouble() < DiveChance)
                        Remark("dive", Kin(diver, BotResponses.McDiveLines, BotResponses.McDiveLinesOwner, BotResponses.McDiveLinesTata), diver);
                    break;
                case "surfacing":
                    if (Random.Shared.NextDouble() < SurfacingChance) Remark("surfacing", BotResponses.McDiveAirLines);
                    break;
                case "drowning":
                    Remark("drowning", BotResponses.McDrowningLines, force: true);
                    break;
                case "breathing":
                    if (Random.Shared.NextDouble() < BreathingChance) Remark("breathing", BotResponses.McBreathingLines);
                    break;
                case "boarded":
                    var skipper = root.GetProperty("player").GetString() ?? "";
                    if (Random.Shared.NextDouble() < BoardChance)
                        Remark("boarded", Kin(skipper, BotResponses.McBoardLines, BotResponses.McBoardLinesOwner, BotResponses.McBoardLinesTata), skipper);
                    break;
                case "shook":
                    if (Random.Shared.NextDouble() < ShakeChance) Remark("shook", BotResponses.McShakeLines);
                    break;
                case "hungry":
                    OnHungry(root.GetProperty("food").GetInt32());
                    break;
                case "fed":
                    var feeder = root.GetProperty("by").GetString() ?? "";
                    if (feeder.Length > 0)
                        Remark("fed", Kin(feeder, BotResponses.McFedLines, BotResponses.McFedLinesOwner, BotResponses.McFedLinesTata), feeder,
                            force: true);
                    break;
                case "safe":
                    Remark("safe", BotResponses.McFleeSafeLines, root.GetProperty("player").GetString());
                    break;
                case "familyHurt":
                    OnFamilyHurt(root.GetProperty("victim").GetString() ?? "", root.GetProperty("by").GetString() ?? "");
                    break;
                case "defending":
                    OnDefending(root.GetProperty("player").GetString() ?? "", root.GetProperty("victim").GetString() ?? "",
                        root.GetProperty("swat").GetBoolean());
                    break;
                case "cantDefend":
                    // Busy with someone's order: words only.
                    Remark("familyHit", BotResponses.McFamilyHitLines, root.GetProperty("player").GetString(), false,
                        Who(root.GetProperty("victim").GetString() ?? ""));
                    break;
                case "cornered":
                    // Nowhere to run: said at once, like the escalation it follows.
                    Remark("cornered", BotResponses.McCorneredLines, root.GetProperty("player").GetString(), force: true);
                    break;
                case "dropped":
                    OnDropped(root.GetProperty("player").GetString() ?? "", root.GetProperty("by").GetString() ?? "");
                    break;
                case "seek":
                    OnSeek(root.GetProperty("player").GetString() ?? "", root.GetProperty("outcome").GetString() == "met");
                    break;
                case "seen":
                    OnSeen(root.GetProperty("player").GetString() ?? "", new McSpot(root.GetProperty("x").GetInt32(),
                        root.GetProperty("y").GetInt32(), root.GetProperty("z").GetInt32(), root.GetProperty("dimension").GetString() ?? ""));
                    break;
                case "hurt":
                    if (root.TryGetProperty("by", out var by) && by.ValueKind == JsonValueKind.String && by.GetString() is { Length: > 0 } hitter)
                        OnHit(hitter);
                    break;
                case "night":
                    if (Random.Shared.NextDouble() < NightChance) Remark("night", BotResponses.McNightLines);
                    break;
                case "kicked":
                    var reason = root.GetProperty("reason").GetString() ?? "";
                    lock (_gate)
                    {
                        _lastKick = reason;
                        _lastKickAt = DateTimeOffset.UtcNow;
                    }
                    _logger.LogWarning("Minecraft body kicked: {Reason}", reason);
                    break;
                case "end":
                    var ended = root.GetProperty("reason").GetString();
                    lock (_gate) _lastEnd = ended;
                    _logger.LogInformation("Minecraft body disconnected: {Reason}", ended);
                    break;
                case "debug":
                    TaskCompletionSource<JsonElement>? asked;
                    lock (_gate) (asked, _debugAnswer) = (_debugAnswer, null);
                    asked?.TrySetResult(root.Clone());
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Unreadable Minecraft body event ({Error}): {Line}", ex.Message, line);
        }
    }

    private void OnSpawned()
    {
        MinecraftBodyProcess? body;
        lock (_gate)
        {
            body = _body;
            if (body is null) return;
            _spawned = true;
            _inGameSince = DateTimeOffset.UtcNow;
            _failures = 0;
            // Whose hits she watches for (Defending her family). Names only: offline mode lets anyone take one.
            body.Send(new { op = "family", names = new[] { _owner, _tata }.Where(n => n.Length > 0).ToArray() });
            SendRoutineLocked(body);
        }
        _logger.LogInformation("Minecraft body spawned on {Host}:{Port}.", _host, _port);
        _ = Task.Run(async () =>
        {
            await Task.Delay(ArrivalPause);
            lock (_gate)
                if (ReferenceEquals(_body, body) && !_leaving)
                {
                    // Invited (/minecraft invite): she says who brought her, instead of her usual hello.
                    var invitedBy = McInvite.Inviter(_invitedBy, _invitedUntil, DateTimeOffset.UtcNow);
                    _invitedBy = null;
                    var text = invitedBy is null ? _picker.Pick(BotResponses.McArrivalLines, LineChoices)
                        : string.Format(_picker.Pick(BotResponses.McInvitedLines, LineChoices), Who(invitedBy));
                    SayLocked(body, new { op = "say", text }, text);
                }
            await Task.Delay(SeekPause);
            lock (_gate)
                if (ReferenceEquals(_body, body) && !_leaving)
                    SeekFamilyLocked(body);
        });
    }

    private void OnSystem(string key, IReadOnlyList<McArg> args)
    {
        List<string> players;
        lock (_gate) players = _players;

        if (McDeath.Classify(key, args, players, _username) is { } death)
        {
            if (death.Kind == McDeathKind.Self) _tally.Add(McStats.Deaths, 1);
            var pool = death.Kind == McDeathKind.Self ? BotResponses.McDeathLines[death.Kind]
                : Kin(death.Victim, BotResponses.McDeathLines[death.Kind], BotResponses.McDeathLinesOwner, BotResponses.McDeathLinesTata);
            var ordinary = death.Kind is not (McDeathKind.Self or McDeathKind.Player) && KinOf(death.Victim) == McKin.Anyone;
            if (!ordinary || Random.Shared.NextDouble() < DeathChance) Remark($"death:{death.Kind}", pool, death.Victim);
            return;
        }
        if (McDeath.AdvancementEarner(key, args, players) is { } earner && Random.Shared.NextDouble() < AdvancementChance)
            Remark("advancement", Kin(earner, BotResponses.McAdvancementLines,
                BotResponses.McAdvancementLinesOwner, BotResponses.McAdvancementLinesTata), earner);
    }
}
