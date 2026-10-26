using System.Globalization;
using System.Text.RegularExpressions;

namespace ProjectSYNCS.Helpers;

// Orders she understands (McOrder), in one line each. « Free » orders take her from nobody: she keeps doing what she was doing.
//
// Moving
//   Come        walk up to the player.
//   Follow      keep following the player.
//   Stay        stop and stay put (also cancels whatever she was doing).
//   GoTo        walk to coordinates (x z, or x y z).
//   Sit         crouch and stay put.
// Fighting and guarding
//   Guard       follow the player and fight the monsters around them.
//   Patrol      guard a spot (where she stands, or a remembered place) instead of a player.
// Working
//   Collect     gather N of a resource (wood, stone, coal, iron, copper, gold, diamond, dirt, sand) and bring it back.
//   Harvest     harvest the ripe crops around her, replant, bring them back.
//   Farm        stay and keep the farm: harvest, replant, store in the chest.
//   Ranch       stay and keep the animals: breed, shear, cull the surplus, store the drops.
//   Fish        fish until told otherwise.
//   Smelt       put food, ore or sand in a furnace and bring back the result.
//   Sleep       find a bed and sleep.
// Carrying
//   Give        hand the player what she carries (everything, or a kind: food, tools, armour...).
//   Store       put what she carries in the nearby chest, keeping her tools.
//   Dress       free: put on the best armour she carries.
//   Undress     free: take her armour off and keep it (she puts none on by herself until told to, or given a piece).
//   Inventory   free: say what she carries.
//   Have        free: say how much she carries of one kind of goods (iron, food, armour...).
// Remembered places (each player's own; « la maison de X », « chez X » name another's: McRequest.Of)
//   SavePlace   remember where the player stands under a name.
//   GoToPlace   walk to a remembered place (no name: home).
//   Guide       lead the player to a remembered place, waiting when they fall behind.
//   WherePlace  free: say where a remembered place is.
//   ForgetPlace free: forget a remembered place.
//   ListPlaces  free: list the places she remembers.
// Small talk and gestures (all free)
//   Where       say where she is.
//   Jump        hop (a dance is a hop too).
//   Wave        wave back at a greeting.
//   Bye         wave at a goodbye.
//   Praise      « gg syncs », « bravo syncs »: she takes the compliment (and hops).
//   Thanks      « merci syncs »: she answers it.
//   Timer       remind the player after a delay, with an optional note.
// Not orders
//   None        not for her, or refused (« viens pas »): she says nothing.
//   Unknown     called by name with nothing she understands: « oui ? », then she hears the next message.

/// <summary>What a player asked her in Minecraft's chat. Not stored: free to reorder.</summary>
public enum McOrder
{
    /// <summary>Not for her, or a refusal (« viens pas »): she says nothing.</summary>
    None,
    /// <summary>Called by name with nothing she understands: she answers « oui ? » and hears the player's next message.</summary>
    Unknown,
    Come,
    Follow,
    Stay,
    /// <summary>Walk to coordinates (<see cref="McRequest.Coordinates"/>).</summary>
    GoTo,
    /// <summary>Follow the player and fight the monsters around them.</summary>
    Guard,
    /// <summary>Gather <see cref="McRequest.Count"/> of <see cref="McRequest.Resource"/> and bring it back.</summary>
    Collect,
    /// <summary>Find a bed and sleep.</summary>
    Sleep,
    /// <summary>Harvest the ripe <see cref="McRequest.Crop"/> around her, replant, and bring it back.</summary>
    Harvest,
    /// <summary>Stay where she is and keep the farm: harvest what ripens, replant, store it in the chest.</summary>
    Farm,
    /// <summary>Hand the player what she carries (<see cref="McRequest.Goods"/>; null: something she doesn't know).</summary>
    Give,
    /// <summary>Put what she carries in the chest nearby, keeping her tools.</summary>
    Store,
    /// <summary>Say where she is. Takes her from nobody.</summary>
    Where,
    /// <summary>Crouch and stay put.</summary>
    Sit,
    /// <summary>Hop. Takes her from nobody.</summary>
    Jump,
    /// <summary>A greeting (« salut syncs », « fais coucou »): she waves back. Takes her from nobody.</summary>
    Wave,
    /// <summary>A goodbye (« bonne nuit syncs », « bye syncs »): she waves too. Takes her from nobody.</summary>
    Bye,
    /// <summary>A compliment (« gg syncs », « bravo syncs »): she takes it, with a hop. Takes her from nobody.</summary>
    Praise,
    /// <summary>A thank-you (« merci syncs »): she answers it. Takes her from nobody.</summary>
    Thanks,
    /// <summary>Remind the player in <see cref="McRequest.Seconds"/>, of <see cref="McRequest.Note"/> if any. Takes her from nobody.</summary>
    Timer,
    /// <summary>Fish until told otherwise.</summary>
    Fish,
    /// <summary>Guard a spot rather than a player: where she stands, or <see cref="McRequest.Place"/>.</summary>
    Patrol,
    /// <summary>Put <see cref="McRequest.Smelt"/> she carries in a furnace nearby and bring back the result.</summary>
    Smelt,
    /// <summary>Remember where the player stands as <see cref="McRequest.Place"/> (null: no name given).</summary>
    SavePlace,
    /// <summary>Walk to a remembered place (<see cref="McRequest.Place"/>; empty: home).</summary>
    GoToPlace,
    /// <summary>Lead the player to a remembered place, waiting when they fall behind.</summary>
    Guide,
    /// <summary>Say where a remembered place is. Takes her from nobody.</summary>
    WherePlace,
    /// <summary>Forget a remembered place. Takes her from nobody.</summary>
    ForgetPlace,
    /// <summary>List the places she remembers. Takes her from nobody.</summary>
    ListPlaces,
    /// <summary>Stay where she is and keep the animals: breed, shear, cull the surplus, store the drops in the chest.</summary>
    Ranch,
    /// <summary>Say what she carries. Takes her from nobody.</summary>
    Inventory,
    /// <summary>Say how much she carries of <see cref="McRequest.Goods"/>. Takes her from nobody.</summary>
    Have,
    /// <summary>Build a camp where she stands: an open camp round a campfire, with benches, a workstation and a bed. Family only.</summary>
    Camp,
    /// <summary>Take her camp down, only its own blocks. Family only.</summary>
    Uncamp,
    /// <summary>Put on the best armour she carries. Takes her from nobody.</summary>
    Dress,
    /// <summary>Take her armour off and keep it in her pockets. Takes her from nobody.</summary>
    Undress,
}

/// <summary>What she can be asked to hand over. The body knows each by its lower-case name. Not stored.</summary>
public enum McGoods
{
    /// <summary>Everything in her pockets (not what she wears).</summary>
    All,
    Food,
    Fish,
    Weapons,
    Tools,
    Pickaxe,
    Axe,
    Shovel,
    Rod,
    Bow,
    Arrows,
    /// <summary>Her armour, the pieces she wears included.</summary>
    Armour,
    Seeds,
    /// <summary>What a field gives: wheat, carrots, potatoes, beetroot, melon slices, pumpkins.</summary>
    Produce,
    Wood,
    Stone,
    Coal,
    Iron,
    Copper,
    Gold,
    Diamond,
    Dirt,
    Sand,
}

/// <summary>What she can be asked to put in a furnace. The body knows each by its lower-case name. Not stored.</summary>
public enum McSmelt
{
    /// <summary>Raw meat, fish, potatoes, kelp.</summary>
    Food,
    /// <summary>Raw iron, gold and copper, and their ores.</summary>
    Ore,
    /// <summary>Sand, for glass.</summary>
    Sand,
}

/// <summary>What she can be sent to harvest. The body knows each by its lower-case name. Not stored.</summary>
public enum McCrop
{
    /// <summary>Whatever is ripe.</summary>
    Any,
    Wheat,
    Carrot,
    Potato,
    Beetroot,
    Melon,
    Pumpkin,
    // Columns: cut above the bottom block, which grows again. Only when named (or on a farm), never in Any.
    Bamboo,
    SugarCane,
    Cactus,
}

/// <summary>What she can be sent to gather. The body knows each by its lower-case name. Not stored.</summary>
public enum McResource
{
    Wood,
    Stone,
    Coal,
    Iron,
    Copper,
    Gold,
    Diamond,
    Dirt,
    Sand,
}

/// <summary>An order, with what it needs: the coordinates for GoTo (2 numbers: x z; 3: x y z), the resource and count
/// for Collect, the crop for Harvest, the goods for Give, what to smelt, a place's key (<see cref="McCommand.PlaceKey"/>)
/// and whose it is when named (<c>Of</c>: one of the parser's players, « la maison de Rodhengard »), a timer's length and
/// note.</summary>
public readonly record struct McRequest(McOrder Order, int[]? Coordinates = null, McResource? Resource = null, int Count = 0,
    McCrop? Crop = null, McGoods? Goods = null, McSmelt? Smelt = null, string? Place = null, int Seconds = 0, string? Note = null,
    string? Of = null, bool Refused = false)
{
    public static readonly McRequest Nothing = new(McOrder.None);
    /// <summary>An order refused around its verb (« viens pas »): nothing to do, and nothing to answer either.</summary>
    public static readonly McRequest Refusal = new(McOrder.None, Refused: true);
}

/// <summary>
/// Reads in-game chat for an order addressed to her. Pure, so the checks walk every phrasing.
/// The words go through <see cref="Rating.Normalize"/> first: lower case, no accents, punctuation
/// collapsed, so « Suis-moi ! » and « suis moi » are the same words. Numbers are read from the raw text,
/// where a minus sign still means something.
/// </summary>
/// <remarks>
/// <para>An order needs her name somewhere in the message (« syncs viens », « viens syncs »): the game
/// chat is shared, and « viens voir » between two players isn't for her. A message that only names
/// her (« syncs est nulle ») is Unknown only when the name opens or closes it, as a call does;
/// anywhere else it's talk about her, and she lets it go. After such a call, the same player's next
/// message is read with <c>called</c> set: as if it carried her name (« syncs » … « suis moi »).</para>
/// <para>A refusal is read around the verb only: « ne » just before it, « pas » or « jamais » just
/// after (« viens pas », « ne me suis pas », « ne va pas chercher »). A « pas » elsewhere in the
/// message doesn't cancel anything (« c'est pas grave, syncs viens »), and « plus de bois » is more
/// wood, not a refusal.</para>
/// <para>The lists are written the way people type in game chat: familiar French (« ramène ta fraise »,
/// « fais du bois »), common shortcuts (« vien », « diams »), a few English words. Add the misses
/// found in the wild, with a case in the checks.</para>
/// </remarks>
public static partial class McCommand
{
    /// <summary>The order alone, for callers that don't need its details.</summary>
    public static McOrder Read(string text, IReadOnlyCollection<string> names) => Parse(text, names).Order;

    /// <param name="text">The chat message as typed.</param>
    /// <param name="names">Every name she answers to (her in-game name, « syncs »), any case.</param>
    /// <param name="called">The player called her just before (a bare « syncs »): her name isn't needed this time,
    /// and a message with nothing she understands is let go rather than read as another call.</param>
    /// <param name="places">The keys of the places she remembers (<see cref="PlaceKey"/>), anyone's, so « va à la mine » finds one.</param>
    /// <param name="players">Whose places may be named (« la maison de Rodhengard », « chez Rodhengard »): player names, any
    /// case. The one named comes back as given, in <see cref="McRequest.Of"/>.</param>
    public static McRequest Parse(string text, IReadOnlyCollection<string> names, bool called = false, IReadOnlyCollection<string>? places = null,
        IReadOnlyCollection<string>? players = null)
    {
        var words = Clean(Rating.Normalize(text));
        if (words.Length == 0) return McRequest.Nothing;
        // Someone's place: their name taken out of the words, the place reads as usual (« chez X » as « à la maison »).
        // Only when both readings are about a place, or the plain one is nothing: « va chercher du bois chez X » is wood.
        var request = ParseWords(text, words, names, called, places);
        if ((AboutPlace(request) || request.Order is McOrder.None or McOrder.Unknown)
            && players is { Count: > 0 } && OwnerOut(words, players, names) is { } theirs
            && ParseWords(text, theirs.Words, names, called, places) is var other && AboutPlace(other))
            return other with { Of = theirs.Player };
        return request;
    }

    private static McRequest ParseWords(string text, string[] words, IReadOnlyCollection<string> names, bool called,
        IReadOnlyCollection<string>? places)
    {

        // Her name, or a one-letter slip of it (« sycns », « sincs ») when it's long enough not to be another word.
        var nameWords = names.Select(Rating.Normalize).Where(n => n.Length > 0 && !n.Contains(' ')).ToHashSet();
        var at = Array.FindIndex(words, w => nameWords.Contains(w) || (w.Length >= 5 && nameWords.Any(n => n.Length >= 5 && Close(w, n))));
        if (at < 0 && !called) return McRequest.Nothing;
        var rest = words.Where((w, i) => i != at).ToArray();
        var calledOut = at >= 0 && (at == 0 || at == words.Length - 1);
        var numbers = Coordinate.Matches(text)
            .Select(m => (int)Math.Floor(double.Parse(m.Value, CultureInfo.InvariantCulture))).ToArray();
        var counts = Count.Matches(text).Select(m => int.Parse(m.Groups[1].Value)).ToArray();
        // Words other than numbers: « syncs 120 64 -300 » is coordinates and nothing else.
        var wordsOnly = rest.Where(w => !w.All(char.IsDigit) && !IsCountToken(w)).ToArray();

        // An order, unless refused around its verb (null: refused, so she says nothing at all).
        McRequest? When(string[] phrases, McRequest request) =>
            Find(wordsOnly, phrases) is not { } hit ? McRequest.Nothing
            : Refused(wordsOnly, hit) ? null
            : request;

        // An order whose verb was found, unless refused around it (« ne retiens pas »): then nothing at all.
        McRequest Unless((int Start, int Length) hit, string[] around, McRequest request) =>
            Refused(around, hit) ? McRequest.Refusal : request;
        var known = places ?? Array.Empty<string>();
        var place = PlaceIn(rest, known);

        // A timer first: its note can say anything (« rappelle-moi d'arrêter le four » is not stay).
        if (Find(wordsOnly, TimerVerbs) is { } timerVerb && DurationIn(text) is { } seconds)
            return Unless(timerVerb, wordsOnly, new(McOrder.Timer, Seconds: seconds, Note: TimerNote(text, names)));

        // Places: remembering one, forgetting one, listing them. Read on the words with their numbers (« base 2 »).
        if (Find(rest, SaveHomePhrases) is { } setHome) return Unless(setHome, rest, new(McOrder.SavePlace, Place: "maison"));
        if (SaveAt(rest) is { } save)
            return Unless(save.Verb, rest, new(McOrder.SavePlace, Place: NameAfter(rest, save.NameFrom)));
        if (Find(rest, ForgetVerbs) is { } forget && NameAfter(rest, forget.Start + forget.Length) is { } forgotten)
            return Unless(forget, rest, new(McOrder.ForgetPlace, Place: place?.Key ?? forgotten));
        if (Find(wordsOnly, ListPlacesPhrases) is { } listing) return Unless(listing, wordsOnly, new(McOrder.ListPlaces));

        // What she carries, asked: before stay (« il te reste combien de pain ? ») and before handing over
        // (« qu'est-ce que t'as dans tes poches ? » is not « vide tes poches »).
        if (CarryQuestion(wordsOnly) is { } carry) return carry;

        if (Find(wordsOnly, FollowOverStayPhrases) is not null) return When(FollowOverStayPhrases, new(McOrder.Follow)) ?? McRequest.Refusal;
        if (Find(wordsOnly, UncampPhrases) is { } uncamp) return Unless(uncamp, wordsOnly, new(McOrder.Uncamp));
        if (Find(wordsOnly, CampPhrases) is { } camp) return Unless(camp, wordsOnly, new(McOrder.Camp));
        if (Find(wordsOnly, RanchAlonePhrases) is not null) return When(RanchAlonePhrases, new(McOrder.Ranch)) ?? McRequest.Refusal;
        if (CareFor(wordsOnly, RanchVerbs, (w, j) => AnimalWords.Contains(w[j])) is { } herd)
            return Unless(herd, wordsOnly, new(McOrder.Ranch));
        if (Find(wordsOnly, FarmAlonePhrases) is not null) return When(FarmAlonePhrases, new(McOrder.Farm)) ?? McRequest.Refusal;
        if (FarmCare(wordsOnly) is { } care) return Refused(wordsOnly, care) ? McRequest.Refusal : new(McOrder.Farm);

        // Guarding a place, before stay and guard: « protège la base » is not « protège-moi ». A remembered place named
        // with it is where she guards (« surveille la mine »); otherwise, where she stands.
        var placeWord = place is { } p ? p.Key.Split(' ')[0] : null;
        if (Find(wordsOnly, PatrolAlonePhrases) is { } patrolAlone)
            return Unless(patrolAlone, wordsOnly, new(McOrder.Patrol, Place: place?.Key));
        if (CareFor(wordsOnly, PatrolVerbs, (w, j) => BaseWords.Contains(w[j]) || w[j] == placeWord, skipMe: true) is { } patrol)
            return Unless(patrol, wordsOnly, new(McOrder.Patrol, Place: place?.Key));
        if (Find(wordsOnly, StayHerePhrases) is not null && CareFor(wordsOnly, PatrolVerbs, (_, _) => true, skipMe: true, window: 0) is { } stayGuard)
            return Unless(stayGuard, wordsOnly, new(McOrder.Patrol, Place: place?.Key));

        if (Find(wordsOnly, SitPhrases) is { } sit) return Unless(sit, wordsOnly, new(McOrder.Sit));
        if (Find(wordsOnly, StayPhrases) is not null) return new(McOrder.Stay);

        foreach (var (phrases, order) in new[] { (SleepPhrases, McOrder.Sleep), (GuardPhrases, McOrder.Guard) })
        {
            var found = When(phrases, new(order));
            if (found is null) return McRequest.Refusal;
            if (found.Value.Order != McOrder.None) return found.Value;
        }

        // What she carries: handed over, or put in a chest. Before gathering, so « donne-moi ton fer » isn't a trip to the
        // mine. Handing over is read before storing, whose « ranger » is one slip from « manger » (« donne-moi à manger »);
        // with a chest word, it's all storing (« jette tes poissons dans le coffre »).
        var chest = wordsOnly.Any(w => ChestWords.Contains(w));
        // Armour on or off, before handing over: « enlève » and « retire » are give verbs too, and taking it off used to
        // toss it. With another give verb it's still handed over (« enlève ton armure et donne-la moi »).
        if (!chest && ArmourVerb(wordsOnly, DressVerbs, DressAlonePhrases) is { } dressing)
            return Unless(dressing, wordsOnly, new(McOrder.Dress));
        if (!chest && ArmourVerb(wordsOnly, UndressVerbs, UndressAlonePhrases) is { } undressing
            && Find(wordsOnly.Select((w, i) => i >= undressing.Start && i < undressing.Start + undressing.Length ? "" : w).ToArray(),
                GiveVerbs) is null)
            return Unless(undressing, wordsOnly, new(McOrder.Undress));
        var give = Find(wordsOnly, GiveVerbs);
        if (!chest && Find(wordsOnly, GiveAllPhrases) is { } giveAll) return Unless(giveAll, wordsOnly, new(McOrder.Give, Goods: McGoods.All));
        if (!chest && give is { } giveVerb && GoodsAfter(wordsOnly, giveVerb.Start + giveVerb.Length) is { Goods: { } goods } asked
            && (asked.Owned || CarriedOnly.Contains(goods)))
            return Unless(giveVerb, wordsOnly, new(McOrder.Give, Goods: goods));
        if (!chest && Find(wordsOnly, HaveVerbs) is { } have && GoodsAfter(wordsOnly, have.Start + have.Length) is { Goods: { } had }
            && CarriedOnly.Contains(had))
            return Unless(have, wordsOnly, new(McOrder.Give, Goods: had));
        if (Find(wordsOnly, HungerPhrases) is not null) return new(McOrder.Give, Goods: McGoods.Food);
        if (Find(wordsOnly, StorePhrases) is { } stored) return Unless(stored, wordsOnly, new(McOrder.Store));
        if (chest && Find(wordsOnly, IntoChestVerbs) is { } intoChest) return Unless(intoChest, wordsOnly, new(McOrder.Store));

        // Fishing: « va pêcher », or fish asked for with a gathering verb (« ramène du poisson »).
        if (Find(wordsOnly, FishPhrases) is { } fishing) return Unless(fishing, wordsOnly, new(McOrder.Fish));
        if (wordsOnly.Any(w => FishWords.Contains(w)) && Find(wordsOnly, CollectPhrases.Concat(CatchVerbs).ToArray()) is { } fishFetch)
            return Unless(fishFetch, wordsOnly, new(McOrder.Fish));

        // Cooking, before crops and resources: « cuis les patates », « fais fondre le fer », « mets le fer au four ».
        if (Find(wordsOnly, SmeltPhrases) is { } smelt) return Unless(smelt, wordsOnly, new(McOrder.Smelt, Smelt: SmeltIn(wordsOnly, smelt)));
        if (CareFor(wordsOnly, IntoFurnaceVerbs, (w, j) => FurnaceWords.Contains(w[j]), window: 5) is { } furnace)
            return Unless(furnace, wordsOnly, new(McOrder.Smelt, Smelt: SmeltIn(wordsOnly, furnace)));

        // A place she knows, named after a going verb, before gathering: « va à la mine de fer » has « mine » and « fer ».
        // Or the question with the place first (« la mine c'est où ? »), or the place alone after « à » (« syncs à la maison »).
        if (place is { } knownPlace)
        {
            foreach (var (verbs, order) in new[] { (WherePlaceVerbs, McOrder.WherePlace), (GuideVerbs, McOrder.Guide), (GoToPlaceVerbs, McOrder.GoToPlace) })
                if (Find(rest, verbs) is { } placeVerb && placeVerb.Start + placeVerb.Length <= knownPlace.At)
                    return Unless(placeVerb, rest, new(order, Place: knownPlace.Key));
            if (Find(rest, WhereIsAnywhere) is { } whereAnywhere) return Unless(whereAnywhere, rest, new(McOrder.WherePlace, Place: knownPlace.Key));
            if (rest.Length > 0 && Prepositions.Contains(rest[0]) && rest.Skip(1).Take(knownPlace.At - 1).All(w => Articles.Contains(w)))
                return new(McOrder.GoToPlace, Place: knownPlace.Key);
        }

        // A crop, before the resources: « pommes de terre » has « terre » (dirt).
        var resourceAt = Array.FindIndex(wordsOnly, w => ResourceWords.Any(r => r.Words.Contains(w)));
        // The verb must be another word than the crop: « la récolte est prête » names the harvest, it doesn't ask for it.
        if (CropIn(wordsOnly) is { } crop && (crop.Crop != McCrop.Any || resourceAt < 0)
            && Find(wordsOnly.Select((w, i) => i == crop.At ? "" : w).ToArray(), CropVerbs) is { } verb)
            return Refused(wordsOnly, verb) ? McRequest.Refusal : new(McOrder.Harvest, Crop: crop.Crop);

        if (resourceAt >= 0)
        {
            var wanted = ResourceWords.First(r => r.Words.Contains(wordsOnly[resourceAt])).Resource;
            var count = counts.FirstOrDefault(n => n > 0);
            if (count == 0) count = QuantityBefore(wordsOnly, resourceAt);
            var collect = When(CollectPhrases,
                new(McOrder.Collect, Resource: wanted, Count: Math.Clamp(count == 0 ? DefaultCount : count, 1, MaxCount)));
            if (collect is null) return McRequest.Refusal;
            if (collect.Value.Order != McOrder.None) return collect.Value;
        }
        // « syncs récolte », « syncs moissonne »: whatever is ripe. Not with a resource (« récolte du sable » is
        // sand), and not « récolte » after an article, which is the noun (« la récolte est prête »).
        else if (Find(wordsOnly, HarvestPhrases) is { } said
                 && !(said.Length == 1 && wordsOnly[said.Start] is "recolte" or "recoltes" && said.Start > 0
                      && wordsOnly[said.Start - 1] is "la" or "ta" or "ma" or "sa" or "une" or "les" or "des" or "tes" or "mes" or "ses" or "notre" or "votre"))
        {
            var harvest = When(HarvestPhrases, new(McOrder.Harvest, Crop: McCrop.Any));
            if (harvest is null) return McRequest.Refusal;
            if (harvest.Value.Order != McOrder.None) return harvest.Value;
        }

        if (numbers.Length is 2 or 3)
        {
            if (wordsOnly.Length == 0) return new(McOrder.GoTo, Coordinates: numbers);
            foreach (var phrases in new[] { GoToPhrases, ComePhrases })
            {
                var goTo = When(phrases, new(McOrder.GoTo, Coordinates: numbers));
                if (goTo is null) return McRequest.Refusal;
                if (goTo.Value.Order != McOrder.None) return goTo.Value;
            }
        }

        // Remembered places: where one is, the way there with the player, or the walk alone. A place she knows anywhere
        // after the verb, else the name after a preposition (« va à la mine »), which the service may not know.
        if (Find(rest, WherePlaceVerbs) is { } whereIs && PlaceAfter(rest, whereIs, place, needsPreposition: false) is { } asked1)
            return Unless(whereIs, rest, new(McOrder.WherePlace, Place: asked1));
        if (Find(rest, GuideVerbs) is { } guide && PlaceAfter(rest, guide, place, needsPreposition: true) is { } asked2)
            return Unless(guide, rest, new(McOrder.Guide, Place: asked2));
        if (Find(rest, GoToPlaceVerbs) is { } goPlace)
        {
            if (PlaceAfter(rest, goPlace, place, needsPreposition: true) is { } asked3)
                return Unless(goPlace, rest, new(McOrder.GoToPlace, Place: asked3));
            // « rentre », « rentre chez nous »: home. Not with a come in it (« viens on rentre » is come).
            if (Find(rest, HomeVerbs) is { } home && Find(wordsOnly, ComePhrases) is null
                && rest.Skip(home.Start + home.Length).All(w => NameFiller.Contains(w) || NameStop.Contains(w) || w is "pas" or "jamais"))
                return Unless(home, rest, new(McOrder.GoToPlace, Place: ""));
        }

        foreach (var (phrases, order) in new[] { (FollowPhrases, McOrder.Follow), (ComePhrases, McOrder.Come) })
        {
            var found = When(phrases, new(order));
            if (found is null) return McRequest.Refusal;
            if (found.Value.Order != McOrder.None) return found.Value;
        }

        // The small things, last: a greeting with an order in it is the order.
        foreach (var (phrases, order) in new[]
                 { (JumpPhrases, McOrder.Jump), (WherePhrases, McOrder.Where), (ByePhrases, McOrder.Bye), (WavePhrases, McOrder.Wave) })
        {
            var found = When(phrases, new(order));
            if (found is null) return McRequest.Refusal;
            if (found.Value.Order != McOrder.None) return found.Value;
        }
        // « donne-moi ta clé »: something of hers she doesn't know. She says so rather than « oui ? ».
        if (give is { } unknownGive && GoodsAfter(wordsOnly, unknownGive.Start + unknownGive.Length) is { Goods: null, Owned: true })
            return Unless(unknownGive, wordsOnly, new(McOrder.Give));
        // A compliment or a thank-you, where a call would be: short, her name at one end (or right after a call), so
        // talk about her between players stays theirs. Before the call itself, or « gg syncs » gets « oui ? ».
        if ((calledOut || called) && rest.Length <= 6)
            foreach (var (phrases, order) in new[] { (ThanksPhrases, McOrder.Thanks), (PraisePhrases, McOrder.Praise) })
            {
                var found = When(phrases, new(order));
                if (found is null) return McRequest.Refusal;
                if (found.Value.Order != McOrder.None) return found.Value;
            }
        return calledOut && rest.Length <= 6 ? new(McOrder.Unknown) : McRequest.Nothing;
    }
}
