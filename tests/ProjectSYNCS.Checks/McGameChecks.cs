using System.Collections;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;
using static Check;

// Minecraft: what she carries, SRV, her family in game, hit streaks, claims, and her in-game pools.
public static class McGameChecks
{
    public static void Run()
    {
        // What she carries, as she says it: French names with their plural and article, the biggest first, the rest
        // « autres trucs », and never longer than the list's room in her lines.
        McStack S(string id, int count, params string[] goods) => new(id, count, goods);
        var pockets = new McInventory([S("iron_helmet", 1, "armour")], null, null,
            [S("bread", 20, "food"), S("oak_log", 64, "wood"), S("iron_pickaxe", 1, "tools", "pickaxe"), S("bread", 3, "food"),
             S("strange_thing", 2), S("iron_ingot", 12, "iron"), S("raw_iron", 5, "iron")]);
        void Says(string what, string? got, string? expected)
        {
            if (got != expected) Fail($"inventory {what}: got « {got} », expected « {expected} »");
        }
        Says("overview", pockets.Overview(MinecraftBodyService.ListMax),
            "64 bûches de chêne, 23 pains, 12 lingots de fer, 5 fers bruts, une pioche en fer et un autre truc");
        Says("iron", pockets.Of(McGoods.Iron, MinecraftBodyService.ListMax), "12 lingots de fer et 5 fers bruts");
        Says("armour", pockets.Of(McGoods.Armour, MinecraftBodyService.ListMax), "un casque en fer"); // worn counts
        Says("no fish", pockets.Of(McGoods.Fish, MinecraftBodyService.ListMax), null);
        Says("empty", McInventory.Empty.Overview(MinecraftBodyService.ListMax), null);
        Says("unnamed only", new McInventory([], null, null, [S("strange_thing", 2), S("odd_thing", 1)]).Overview(50), "2 trucs");
        var full = new McInventory([], null, null, McItems.All.Take(36).Select((n, i) => S(n.Key, 64 - i)).ToArray());
        if (full.Overview(MinecraftBodyService.ListMax) is not { } fullText || fullText.Length > MinecraftBodyService.ListMax
            || !fullText.EndsWith(" autres trucs"))
            Fail($"inventory full pockets: « {full.Overview(MinecraftBodyService.ListMax)} »");
        // The names go in game chat: plain text Minecraft can draw.
        foreach (var (id, name) in McItems.All)
            if (name.One.Length == 0 || name.Many.Length == 0 || $"{name.One}{name.Many}".Any(c => c is '§' or '{' or '}' || char.IsSurrogate(c)))
                Fail($"item name {id}: « {name.One} » / « {name.Many} »");

        // SRV only where the client library does it: a name, on the default port.
        if (!McAddress.NeedsLookup("play.example.net", 25565) || McAddress.NeedsLookup("play.example.net", 25566)
            || McAddress.NeedsLookup("192.168.1.10", 25565) || McAddress.NeedsLookup("localhost", 25565))
            Fail("minecraft address: when to look up SRV");

        var mcStatus = ServerListPing.Parse("""{"version":{"name":"1.21.4","protocol":769},"players":{"max":20,"online":3,"sample":[{"name":"Alice","id":"x"},{"name":"Bob","id":"y"}]},"description":"hi"}""");
        if (mcStatus is not { Online: 3, Max: 20, Version: "1.21.4" } || !mcStatus.Sample.SequenceEqual(new[] { "Alice", "Bob" }))
            Fail("server list ping: parse");
        if (ServerListPing.Parse("""{"version":{"name":"x"}}""") is not null || ServerListPing.Parse("not json") is not null)
            Fail("server list ping: not a status");

        // Her family in game: matched by name without case, never by an unset name. Each twin exists for both.
        if (McFamily.Of("rodhengard", "Rodhengard", "Analuz") != McKin.Papa || McFamily.Of("ANALUZ", "Rodhengard", "Analuz") != McKin.Tata
            || McFamily.Of("Bob", "Rodhengard", "Analuz") != McKin.Anyone || McFamily.Of("", "", "") != McKin.Anyone
            || McFamily.Of("Bob", "", "") != McKin.Anyone)
            Fail("minecraft family: who is who");
        // Hit streaks (McHitStreaks): the third hit from one player within a minute tips her over, once; other
        // players count apart; a pause longer than the window starts over.
        var hits = new McHitStreaks();
        var hitAt = DateTimeOffset.UtcNow;
        var stages = new[]
        {
            hits.Register("Bob", hitAt), hits.Register("Eve", hitAt.AddSeconds(5)), hits.Register("bob", hitAt.AddSeconds(10)),
            hits.Register("Bob", hitAt.AddSeconds(20)), hits.Register("Bob", hitAt.AddSeconds(30)), hits.Register("Eve", hitAt.AddSeconds(40)),
            hits.Register("Bob", hitAt.AddSeconds(30) + McHitStreaks.Window + TimeSpan.FromSeconds(1)),
        };
        if (!stages.SequenceEqual(new[] { McHitStage.Hit, McHitStage.Hit, McHitStage.Hit, McHitStage.Escalated, McHitStage.Again,
                McHitStage.Hit, McHitStage.Hit }))
            Fail($"minecraft hit streaks: {string.Join(", ", stages)}");
        // Someone hitting her family: a word, then one hit back, then a fight, per attacker, starting over after a pause.
        var familyHits = new McHitStreaks();
        var defence = new[]
        {
            familyHits.Tally("Bob", hitAt), familyHits.Tally("Eve", hitAt.AddSeconds(2)), familyHits.Tally("Bob", hitAt.AddSeconds(5)),
            familyHits.Tally("Bob", hitAt.AddSeconds(8)), familyHits.Tally("Bob", hitAt.AddSeconds(9)),
            familyHits.Tally("Bob", hitAt.AddSeconds(9) + McHitStreaks.Window + TimeSpan.FromSeconds(1)),
        }.Select(McHitStreaks.DefendStage);
        if (!defence.SequenceEqual(new[] { McDefendStage.Warn, McDefendStage.Warn, McDefendStage.Swat, McDefendStage.Fight,
                McDefendStage.Fight, McDefendStage.Warn }))
            Fail($"minecraft family defence: {string.Join(", ", defence)}");

        // Who she works for (McClaim): the holder alone, until their claim runs out, and family outranks.
        var claimAt = DateTimeOffset.UtcNow;
        void Claim(string what, string? holder, McKin holderKin, TimeSpan age, string player, McKin kin, McVerdict expected)
        {
            var got = McClaim.Decide(holder, holderKin, claimAt - age, player, kin, claimAt);
            if (got != expected) Fail($"minecraft claim, {what}: got {got}, expected {expected}");
        }
        var shortly = TimeSpan.FromMinutes(1);
        Claim("free", null, McKin.Anyone, TimeSpan.Zero, "Bob", McKin.Anyone, McVerdict.Take);
        Claim("same player, any case", "Bob", McKin.Anyone, shortly, "bob", McKin.Anyone, McVerdict.Take);
        Claim("someone else", "Bob", McKin.Anyone, shortly, "Eve", McKin.Anyone, McVerdict.Busy);
        Claim("claim ran out", "Bob", McKin.Anyone, McClaim.Expiry, "Eve", McKin.Anyone, McVerdict.Take);
        Claim("Tata takes from anyone", "Bob", McKin.Anyone, shortly, "Ana", McKin.Tata, McVerdict.Take);
        Claim("Papa takes from Tata", "Ana", McKin.Tata, shortly, "Rod", McKin.Papa, McVerdict.Take);
        Claim("nobody takes from Papa", "Rod", McKin.Papa, shortly, "Ana", McKin.Tata, McVerdict.Busy);
        Claim("anyone can't take from Tata", "Ana", McKin.Tata, shortly, "Bob", McKin.Anyone, McVerdict.Busy);
        Claim("Papa's claim runs out too", "Rod", McKin.Papa, McClaim.Expiry, "Bob", McKin.Anyone, McVerdict.Take);
        if (McClaim.Decide("Bob", McKin.Anyone, claimAt - TimeSpan.FromHours(2), "Eve", McKin.Anyone, claimAt, lasting: true) != McVerdict.Busy
            || McClaim.Decide("Bob", McKin.Anyone, claimAt - TimeSpan.FromHours(2), "Ana", McKin.Tata, claimAt, lasting: true) != McVerdict.Take)
            Fail("minecraft claim: a kept farm doesn't run out, family still outranks it");

        var McWideArgs = new HashSet<string>
        {
            "McTimerNoteLines", "McPlaceListLines", "McPlaceSavedLines", "McPlaceMovedLines", "McPlaceNotYoursLines", "McPlaceFullLines",
            "McPlaceUnknownLines", "McPlaceForgottenLines", "McPlaceWhereLines", "McPlaceOtherDimensionLines", "McPlaceGoAckLines",
            "McPlaceArrivedLines", "McGuideAckLines", "McGuideArrivedLines", "McPatrolPlaceAckLines", "McTripTooFarLines",
            "McInventoryLines", "McHaveLines", "McDoingLines",
        };
        string[] McFamilyPools = { "McCalledLines", "McDeathLines", "McHitByPlayerLines", "McHitAgainLines", "McFedLines", "McAdvancementLines", "McComeAckLines",
            "McFollowAckLines", "McGuardAckLines", "McCollectDoneLines", "McWaveLines", "McGiveDoneLines", "McByeLines", "McCreeperBehindLines", "McDiveLines", "McBoardLines",
            "McPraisedLines", "McThankedLines", "McPortalLines", "McArmouredLines", "McFreeLines", "McFetchDoneLines", "McCraftDoneLines" };
        foreach (var p in McFamilyPools)
        foreach (var twin in new[] { p + "Owner", p + "Tata" })
        {
            var twinLines = Pools(twin).SelectMany(p => p.Lines).ToArray();
            if (twinLines.Length == 0) { Fail($"{twin}: missing"); continue; }
            // Papa and Tata by title, never by an in-game name.
            if (twinLines.Any(l => l.Contains("{0}"))) Fail($"{twin}: uses {{0}}, they're called Papa and Tata");
        }
        // Pools that only exist as twins, same rule.
        foreach (var twin in new[] { "McSeekMetLinesOwner", "McSeekMetLinesTata", "McSeekMissLinesOwner", "McSeekMissLinesTata",
                     "McTagAlongLinesOwner", "McTagAlongLinesTata" })
            if (Field(twin) is not string[] { Length: > 0 } twinLines) Fail($"{twin}: missing");
            else if (twinLines.Any(l => l.Contains("{0}"))) Fail($"{twin}: uses {{0}}, they're called Papa and Tata");

        // In game there is no Discord markup: no custom emotes, no sticker tags, and Minecraft cuts chat at 256.
        // Markup opens with « <: », « <a: » (emotes), « <@ » (mentions, roles included), « <# » (channels), « <t: » (timestamps).
        var DiscordMarkup = new System.Text.RegularExpressions.Regex(@"<(a?:|@|#|t:)");
        foreach (var f in new[] { "McArrivalLines", "McLeavingLines", "McDeathLines", "McHitByPlayerLines", "McAdvancementLines", "McNightLines",
                 "McComeAckLines", "McFollowAckLines", "McStayAckLines", "McArrivedLines", "McTooFarLines", "McNoPathLines", "McLostLines",
                 "McCalledLines", "McPraisedLines", "McThankedLines", "McGoToAckLines", "McGoToArrivedLines", "McTripTooFarLines", "McGuardAckLines", "McGuardKillLines",
                 "McCollectAckLines", "McCollectDoneLines", "McCollectKeptLines", "McCollectEmptyLines", "McCollectNoneLines",
                 "McCollectNoToolLines", "McSleepLines", "McBedtimeLines", "McNoBedLines", "McNotNightLines", "McBedFailedLines",
                 "McBusyLines", "McTakenLines", "McHitAgainLines", "McFleeSafeLines", "McCorneredLines", "McFleeDroppedLines", "McDangerDroppedLines", "McTotemLines", "McFamilyHitLines", "McFamilySwatLines", "McFamilyFightLines", "McHungryLines", "McStarvingLines", "McHarvestAckLines", "McHarvestDoneLines", "McHarvestKeptLines", "McHarvestEmptyLines",
                 "McHarvestNoneLines", "McFarmAckLines", "McFarmNoneLines", "McFarmNoChestLines", "McSeekMetLinesOwner", "McSeekMetLinesTata", "McSeekMissLinesOwner", "McSeekMissLinesTata",
                 "McGiveLostLines", "McGiveNoneLines", "McGiveEmptyLines", "McGiveUnknownLines", "McCantDoLines", "McLinkedLines", "McLinkFailedLines", "McInvitedLines", "McStoreDoneLines", "McStoreFullLines",
                 "McStoreEmptyLines", "McStoreNoChestLines", "McSitLines", "McJumpLines", "McWhereLines", "McTimerSetLines", "McTimerDoneLines",
                 "McTimerNoteLines", "McTimerTooLongLines", "McTimerFullLines", "McFishAckLines", "McFishNoRodLines", "McFishRodBrokeLines",
                 "McFishNoWaterLines", "McFishCatchLines", "McFishTreasureLines", "McFishJunkLines", "McPatrolAckLines", "McPatrolPlaceAckLines",
                 "McCreeperLines", "McRetreatLines", "McSmeltAckLines", "McSmeltDoneLines", "McSmeltKeptLines", "McSmeltEmptyLines",
                 "McSmeltNothingLines", "McSmeltNoFuelLines", "McSmeltNoFurnaceLines", "McSmeltBusyLines", "McPlaceSavedLines",
                 "McPlaceMovedLines", "McPlaceNoNameLines", "McPlaceNotYoursLines", "McPlaceFullLines", "McPlaceUnknownLines",
                 "McPlaceForgottenLines", "McPlaceListLines", "McPlaceListEmptyLines", "McPlaceListEmptyOfLines", "McPlaceWhereLines", "McPlaceOtherDimensionLines",
                 "McPlaceGoAckLines", "McPlaceArrivedLines", "McGuideAckLines", "McGuideWaitLines", "McGuideArrivedLines", "McGuideLostLines",
                 "McRanchAckLines", "McRanchNoneLines", "McRanchNoChestLines",
                 "McCampAckLines", "McCampDoneLines", "McCampPartialLines", "McCampNoBedLines", "McCampMissingLines", "McCampNoRoomLines", "McCampExistsLines",
                 "McCampRefusedLines", "McUncampAckLines", "McUncampDoneLines", "McUncampNoneLines", "McCampFarLines", "McRoutineFarmLines", "McRoutineFishLines", "McRoutineRanchLines",
                 "McRoutineHomeLines", "McTagAlongLinesOwner", "McTagAlongLinesTata", "McRetrieveLines", "McRetrievedLines", "McRetrieveEmptyLines",
                 "McRetrieveLostLines", "McToolWornLines", "McToolBrokeLines", "McNeedRodLines", "McNeedShearsLines", "McNeedBreedFoodLines",
                 "McNeedArrowsLines", "McPocketsFullLines", "McDiveLines", "McDiveAirLines", "McDrowningLines", "McBreathingLines", "McBoardLines",
                 "McShakeLines", "McInventoryLines", "McInventoryEmptyLines", "McHaveLines", "McHaveNoneLines", "McPortalLines",
                 "McDressLines", "McDressWornLines", "McArmourNoneLines", "McUndressLines", "McUndressFullLines",
                 "McDoingLines", "McDoingNothingLines", "McFreeLines", "McFreeAlreadyLines", "McFetchAckLines", "McFetchDoneLines",
                 "McFetchShortLines", "McFetchNoneLines", "McFetchNoChestLines", "McFetchUnknownLines", "McCraftAckLines", "McCraftDoneLines",
                 "McCraftMissingLines", "McCraftNoWoodLines", "McCraftNoStoneLines", "McCraftUnknownLines", "McCraftFailedLines" }
                 .Concat(McFamilyPools.SelectMany(p => new[] { p + "Owner", p + "Tata" })))
        foreach (var (name, lines) in Pools(f))
        {
            // Empty lines are silence on purpose (the body sends nothing for them), and may repeat to weigh it.
            var spoken = lines.Where(l => l.Length > 0).ToArray();
            if (spoken.Distinct().Count() != spoken.Length) Fail($"{name}: duplicate lines");
            foreach (var l in lines)
            {
                string s;
                // The longest of each: a 16-letter name, the widest coordinates, the longest resource name; for the pools
                // whose {1} or {2} carry more (a reminder's note, the list of places, a place's name), the longest of those.
                var wide = McWideArgs.Contains(name.Split('[')[0]);
                try
                {
                    s = wide
                        ? string.Format(l, "Abcdefghijklmnop", new string('x', Math.Max(McCommand.NoteMax + 1, MinecraftBodyService.ListMax + 1)),
                            new string('x', McCommand.MaxPlaceName + 4))
                        : string.Format(l, "Abcdefghijklmnop", "-29999999 320 -29999999", "tout ce que j'ai");
                }
                catch (FormatException) { Fail($"{name}: format: {l}"); continue; }
                if (s.Length > 256) Fail($"{name}: {s.Length} characters: {l}");
                // Discord markup (a custom emote, a mention, a timestamp) would show raw in game; a « <3 » is fine.
                if (Sticker.Strip(s) != s || DiscordMarkup.IsMatch(s) || s.Contains('{') || s.Contains('}') || s.Contains('✨') || s.Contains("(｡•́︿•̀｡)"))
                    Fail($"{name}: not plain chat text: {l}");
                // Minecraft's font draws the Basic Multilingual Plane one code point at a time: nothing past it (most
                // emoji), no combining marks (« •̀ » lands beside the dot), no format characters, no § (colour codes).
                foreach (var c in s)
                    if (char.IsSurrogate(c) || c == '§' || System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) is
                            System.Globalization.UnicodeCategory.NonSpacingMark or System.Globalization.UnicodeCategory.SpacingCombiningMark
                            or System.Globalization.UnicodeCategory.EnclosingMark or System.Globalization.UnicodeCategory.Format)
                    {
                        Fail($"{name}: Minecraft can't draw U+{(int)c:X4}: {l}");
                        break;
                    }
            }
        }
    }
}
