using System.Collections;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;
using static Check;

// Minecraft: when she joins and leaves, how she reads deaths, and her in-game orders.
public static class McOrderChecks
{
    public static readonly string[] mcNames = new[] { "SYNCS" }.Concat(McCommand.HerNames).ToArray();
    public static readonly string[] mcPlaces = new[] { "maison", "mine", "mine de fer", "base 2" };

    public static void Request(string text, McOrder order, string? coords = null, McResource? resource = null, int count = 0)
    {
        var got = McCommand.Parse(text, mcNames);
        var gotCoords = got.Coordinates is null ? null : string.Join(" ", got.Coordinates);
        if (got.Order != order || gotCoords != coords || got.Resource != resource || (count != 0 && got.Count != count))
            Fail($"request « {text} »: got {got.Order} [{gotCoords}] {got.Resource} x{got.Count}");
    }

    public static void Mc(string text, McOrder order, Func<McRequest, bool>? also = null, bool called = false)
    {
        var got = McCommand.Parse(text, mcNames, called, mcPlaces);
        if (got.Order != order || (also is not null && !also(got)))
            Fail($"order « {text} »: got {got.Order} goods={got.Goods} smelt={got.Smelt} place={got.Place} s={got.Seconds} note={got.Note}");
    }

    public static void NotOrder(string text, McOrder order)
    {
        if (McCommand.Parse(text, mcNames, false, mcPlaces).Order == order) Fail($"order « {text} »: read as {order}");
    }

    public static void Run()
    {
        // 13. Minecraft: when she joins and leaves (McPresence), how she reads deaths (McDeath), and her
        // in-game pools (plain text under Minecraft's 256-character chat cap).
        var mcT0 = new DateTimeOffset(2026, 10, 19, 20, 0, 0, TimeSpan.Zero);
        string[] Names(int n, params string[] extra) => Enumerable.Range(1, n).Select(i => $"p{i}").Concat(extra).ToArray();
        void Presence(string what, McPresenceVerdict got, bool inGame, bool fewRunning)
        {
            if (got.InGame != inGame || (got.FewSince is not null) != fewRunning)
                Fail($"presence: {what}: got InGame={got.InGame}, FewSince={got.FewSince}");
        }

        Presence("joins at 4", McPresence.Decide(true, Names(4), 4, "Papa", false, null, mcT0), true, false);
        Presence("no join at 3", McPresence.Decide(true, Names(3), 3, "Papa", false, null, mcT0), false, false);
        Presence("joins for the owner alone", McPresence.Decide(true, Names(0, "papa"), 1, "Papa", false, null, mcT0), true, false);
        Presence("no owner configured", McPresence.Decide(true, Names(1), 1, "", false, null, mcT0), false, false);
        Presence("count beyond the sample", McPresence.Decide(true, Names(2), 5, "Papa", false, null, mcT0), true, false);
        Presence("stays at 3", McPresence.Decide(true, Names(3), 3, "Papa", true, null, mcT0), true, false);
        Presence("≤2 starts the clock", McPresence.Decide(true, Names(2), 2, "Papa", true, null, mcT0), true, true);
        Presence("≤2 at 3 min stays", McPresence.Decide(true, Names(2), 2, "Papa", true, mcT0, mcT0 + McPresence.Linger), true, true);
        Presence("≤2 past 3 min leaves", McPresence.Decide(true, Names(1), 1, "Papa", true, mcT0, mcT0 + McPresence.Linger + TimeSpan.FromSeconds(1)), false, true);
        Presence("owner on: ≤2 doesn't count", McPresence.Decide(true, Names(1, "Papa"), 2, "Papa", true, mcT0, mcT0 + TimeSpan.FromHours(1)), true, false);
        Presence("back to 3 resets the clock", McPresence.Decide(true, Names(3), 3, "Papa", true, mcT0, mcT0 + TimeSpan.FromMinutes(2)), true, false);
        Presence("server down", McPresence.Decide(false, Names(8, "Papa"), 9, "Papa", true, null, mcT0), false, false);

        var mcPlayers = new[] { "Alice", "Bob" };
        McArg McP(string n) => new(n, null);
        void Death(string key, McArg[] args, McDeathKind? kind)
        {
            var got = McDeath.Classify(key, args, mcPlayers, "SYNCS")?.Kind;
            if (got != kind) Fail($"death {key}: got {got?.ToString() ?? "null"}, expected {kind?.ToString() ?? "null"}");
        }
        Death("death.attack.explosion.player", new[] { McP("Alice"), new McArg("Creeper", "entity.minecraft.creeper") }, McDeathKind.Creeper);
        Death("death.fell.accident.generic", new[] { McP("Alice") }, McDeathKind.Fall);
        Death("death.attack.fall", new[] { McP("Alice") }, McDeathKind.Fall);
        Death("death.attack.lava", new[] { McP("Bob") }, McDeathKind.Fire);
        Death("death.attack.inFire.player", new[] { McP("Bob"), new McArg("Zombie", "entity.minecraft.zombie") }, McDeathKind.Fire);
        Death("death.attack.drown", new[] { McP("Bob") }, McDeathKind.Drown);
        Death("death.attack.mob", new[] { McP("Bob"), new McArg("Zombie", "entity.minecraft.zombie") }, McDeathKind.Mob);
        Death("death.attack.arrow", new[] { McP("Bob"), new McArg("Skeleton", "entity.minecraft.skeleton") }, McDeathKind.Mob);
        Death("death.attack.player", new[] { McP("Bob"), McP("Alice") }, McDeathKind.Player);
        Death("death.attack.arrow.item", new[] { McP("Bob"), McP("Alice"), new McArg("Bow", null) }, McDeathKind.Player);
        Death("death.attack.outOfWorld", new[] { McP("Alice") }, McDeathKind.Other);
        Death("death.attack.mob", new[] { McP("SYNCS"), new McArg("Zombie", "entity.minecraft.zombie") }, McDeathKind.Self);
        Death("death.attack.mob", new[] { McP("Rex"), new McArg("Zombie", "entity.minecraft.zombie") }, null); // a named pet
        Death("chat.type.advancement.task", new[] { McP("Alice"), McP("Stone Age") }, null);
        if (McDeath.AdvancementEarner("chat.type.advancement.task", new[] { McP("Alice"), McP("Stone Age") }, mcPlayers) != "Alice")
            Fail("advancement earner");
        if (McDeath.AdvancementEarner("chat.type.advancement.goal", new[] { McP("Rex"), McP("x") }, mcPlayers) is not null)
            Fail("advancement earner: not a player");
        Covers("McDeathLines", typeof(McDeathKind));
        Covers("McResourceNames", typeof(McResource));
        Covers("McCropNames", typeof(McCrop));

        // In-game orders (McCommand): her name is needed, a refusal is not an order, staying wins.
        void Order(string text, McOrder expected)
        {
            var got = McCommand.Read(text, mcNames);
            if (got != expected) Fail($"order « {text} »: got {got}, expected {expected}");
        }
        Order("syncs viens", McOrder.Come);
        Order("SYNCS, viens ici !", McOrder.Come);
        Order("viens syncs", McOrder.Come);
        Order("syncs tu viens ?", McOrder.Come);
        Order("Syncs rejoins-moi", McOrder.Come);
        Order("syncs par ici", McOrder.Come);
        Order("syncs suis-moi", McOrder.Follow);
        Order("Suis moi SYNCS", McOrder.Follow);
        Order("syncs follow me", McOrder.Follow);
        Order("syncs accompagne-moi", McOrder.Follow);
        Order("syncs reste là", McOrder.Stay);
        Order("syncs reste ici", McOrder.Stay);
        Order("syncs stop", McOrder.Stay);
        Order("syncs arrête-toi", McOrder.Stay);
        Order("syncs bouge pas", McOrder.Stay);
        Order("syncs attends", McOrder.Stay);
        Order("syncs viens pas", McOrder.None);
        Order("syncs ne me suis pas", McOrder.None);
        Order("viens voir", McOrder.None);
        Order("suis moi", McOrder.None);
        Order("je suis là syncs", McOrder.Unknown);
        Order("syncs", McOrder.Unknown);
        Order("syncs ?", McOrder.Unknown);
        Order("hé syncs", McOrder.Unknown);
        // A compliment or a thank-you is not a call: « gg syncs » once got « oui ? ».
        Order("gg syncs", McOrder.Praise);
        Order("bravo syncs", McOrder.Praise);
        Order("syncs bien joué !", McOrder.Praise);
        Order("t'es la meilleure syncs", McOrder.Praise);
        Order("syncs t'es géniale", McOrder.Praise);
        Order("syncs c'est génial", McOrder.Unknown);
        Order("merci syncs", McOrder.Thanks);
        Order("Merci beaucoup Syncs !", McOrder.Thanks);
        Order("thx syncs", McOrder.Thanks);
        Order("gg syncs viens", McOrder.Come);
        Order("merci syncs bonne nuit", McOrder.Bye);
        Order("j'ai dit merci à syncs pour le bois tout à l'heure", McOrder.None);
        Order("je crois que syncs est un peu nulle franchement", McOrder.None);
        Order("syncs est trop forte en vrai de vrai de vrai", McOrder.None);
        Order("suisse", McOrder.None);
        Order("syncs dodo", McOrder.Sleep);
        Order("syncs va dormir", McOrder.Sleep);
        Order("syncs protège-moi", McOrder.Guard);
        Order("Syncs défends-moi !", McOrder.Guard);
        Order("syncs ramène-toi", McOrder.Come);
        Order("syncs va chercher", McOrder.Unknown);
        Order("syncs va chercher plus de bois", McOrder.Collect); // « plus de » is more, not a refusal

        // Variants of the orders she knows: familiar French, game-chat spellings, a little English.
        foreach (var (text, expected) in new (string, McOrder)[]
        {
            ("syncs vien", McOrder.Come), ("syncs vi1 stp", McOrder.Come), ("syncs ramène ta fraise", McOrder.Come),
            ("syncs rapplique", McOrder.Come), ("syncs radine-toi", McOrder.Come), ("syncs viens me voir", McOrder.Come),
            ("syncs rapproche-toi", McOrder.Come), ("syncs au pied", McOrder.Come), ("syncs come here", McOrder.Come),
            ("c'est pas grave, syncs viens", McOrder.Come), ("sync viens", McOrder.Come),
            ("syncs viens avec moi", McOrder.Follow), ("syncs reste avec moi", McOrder.Follow),
            ("syncs colle-moi", McOrder.Follow), ("syncs suis-moi stp", McOrder.Follow), ("syncs follow me", McOrder.Follow),
            ("syncs fige-toi", McOrder.Stay), ("syncs annule", McOrder.Stay), ("syncs laisse tomber", McOrder.Stay),
            ("syncs ça suffit", McOrder.Stay), ("syncs arrête de me suivre", McOrder.Stay), ("syncs sur place", McOrder.Stay),
            ("syncs bouge pu", McOrder.Stay), ("syncs wait", McOrder.Stay),
            ("syncs au dodo", McOrder.Sleep), ("syncs va au lit", McOrder.Sleep), ("syncs pionce", McOrder.Sleep),
            ("syncs au secours", McOrder.Guard), ("syncs à l'aide !", McOrder.Guard), ("syncs couvre-moi", McOrder.Guard),
            ("syncs tue les monstres", McOrder.Guard), ("syncs escorte-moi", McOrder.Guard), ("syncs help me", McOrder.Guard),
            ("syncs fais du bois", McOrder.Collect), ("syncs j'ai besoin de fer", McOrder.Collect),
            ("syncs il me faut 20 pierres", McOrder.Collect), ("syncs ramasse du sable", McOrder.Collect),
            ("syncs abats 5 arbres", McOrder.Collect), ("syncs trouve-moi des diams", McOrder.Collect),
            ("syncs file-moi du charbon", McOrder.Collect), ("syncs chop some wood", McOrder.Collect),
            ("syncs direction 100 64 -200", McOrder.GoTo), ("syncs rdv 100 -200", McOrder.GoTo),
            ("syncs rends-toi en 100 64 -200", McOrder.GoTo),
            // Refusals, read next to the verb only.
            ("syncs ne viens pas", McOrder.None), ("syncs viens plus", McOrder.None), ("syncs ne va pas chercher de bois", McOrder.None),
            ("syncs reste pas avec moi", McOrder.Stay), // « don't stay with me »: she stops following, which is what was meant
            ("syncs protège-moi pas", McOrder.None),
            // Not orders: words that look like one in other sentences.
            ("syncs help", McOrder.Unknown), ("syncs t'es assez forte", McOrder.Unknown), ("c'est à moi syncs", McOrder.Unknown),
            ("syncs tu dors ?", McOrder.Sleep), // she takes it as an invitation; accepted
        })
            Order(text, expected);
        Request("syncs il me faut 20 pierres", McOrder.Collect, resource: McResource.Stone, count: 20);
        Request("syncs abats 5 arbres", McOrder.Collect, resource: McResource.Wood, count: 5);
        Request("syncs trouve-moi des diams", McOrder.Collect, resource: McResource.Diamond, count: McCommand.DefaultCount);
        Request("syncs rdv 100 -200", McOrder.GoTo, "100 -200");

        // How people type in game chat: stretched letters, phonetic spellings, one slip on a long verb.
        foreach (var (text, expected) in new (string, McOrder)[]
        {
            ("syncs viiiiens", McOrder.Come), ("syncs stoooop", McOrder.Stay), ("syncs suis mwa", McOrder.Follow),
            ("syncs viens vers moa", McOrder.Come), ("syncs protge-moi", McOrder.Guard), ("syncs va chercer du bois", McOrder.Collect),
            ("syncs rapplque", McOrder.Come), ("sycns viens", McOrder.Come), ("syncsss viens", McOrder.Come),
            ("chui là syncs", McOrder.Unknown), ("jsuis ici syncs", McOrder.Come), // « je suis ici »: here, so come
            ("syncs viens stp", McOrder.Come), ("syncs suis moi steuplait", McOrder.Follow),
            // Questions and infinitives.
            ("syncs tu peux venir ?", McOrder.Come), ("syncs tu peux me suivre ?", McOrder.Follow),
            ("syncs tu pourrais me protéger ?", McOrder.Guard), ("syncs tu peux rester là ?", McOrder.Stay),
            ("syncs tu peux aller dormir ?", McOrder.Sleep), ("syncs tu peux aller chercher du fer ?", McOrder.Collect),
            ("syncs tu veux bien me rejoindre", McOrder.Come), ("syncs tu peux rester avec moi", McOrder.Follow),
            ("syncs tu peux pas venir ?", McOrder.None),
            // Traps: short words stay exact, so one slip doesn't make another order.
            ("syncs les liens", McOrder.Unknown), ("syncs t'es teste", McOrder.Unknown),
        })
            Order(text, expected);

        // Quantities in words, read only right before the resource.
        Request("syncs va chercher dix bûches", McOrder.Collect, resource: McResource.Wood, count: 10);
        Request("syncs ramène une stack de pierre", McOrder.Collect, resource: McResource.Stone, count: 64);
        Request("syncs il me faut une dizaine de bûches", McOrder.Collect, resource: McResource.Wood, count: 10);
        Request("syncs ramasse un peu de sable", McOrder.Collect, resource: McResource.Sand, count: 5);
        Request("syncs va chercher trois diamants", McOrder.Collect, resource: McResource.Diamond, count: 3);
        Request("syncs va chercher x20 charbon", McOrder.Collect, resource: McResource.Coal, count: 20);
        Request("syncs va chercher 15x fer", McOrder.Collect, resource: McResource.Iron, count: 15);
        Request("syncs va chercher du bois pour une maison", McOrder.Collect, resource: McResource.Wood, count: McCommand.DefaultCount);
        // Coordinates as Minecraft shows them: F3 decimals (floored to the block), x:/z: labels, brackets.
        Request("syncs va XYZ: 120.523 / 64.000 / -300.700", McOrder.GoTo, "120 64 -301");
        Request("syncs va x:120 z:-300", McOrder.GoTo, "120 -300");
        Request("syncs va en (120, 64, -300)", McOrder.GoTo, "120 64 -300");

        Request("syncs va 120 64 -300", McOrder.GoTo, "120 64 -300");
        Request("syncs viens en 120, 64, -300", McOrder.GoTo, "120 64 -300");
        Request("syncs go -45 812", McOrder.GoTo, "-45 812");
        Request("syncs 120 64 -300", McOrder.GoTo, "120 64 -300");
        Request("syncs tp 0 70 0", McOrder.GoTo, "0 70 0");
        Request("Bob123 syncs viens", McOrder.Come);
        Request("syncs va 120", McOrder.Unknown);
        Request("syncs va chercher 10 bois", McOrder.Collect, resource: McResource.Wood, count: 10);
        Request("syncs ramène-moi 5 bûches", McOrder.Collect, resource: McResource.Wood, count: 5);
        Request("syncs va chercher du fer", McOrder.Collect, resource: McResource.Iron, count: McCommand.DefaultCount);
        Request("syncs mine 200 charbon", McOrder.Collect, resource: McResource.Coal, count: McCommand.MaxCount);
        Request("syncs va chercher de l'or", McOrder.Collect, resource: McResource.Gold);
        Request("syncs récolte du sable", McOrder.Collect, resource: McResource.Sand);

        // In two steps: « syncs », then the order without her name. The service sets `called` for the caller's next
        // message only; with nothing she understands it is let go, never read as another call.
        foreach (var (text, expected) in new (string, McOrder)[]
        {
            ("suis moi", McOrder.Follow), ("viens", McOrder.Come), ("stop", McOrder.Stay), ("protège-moi", McOrder.Guard),
            ("dodo", McOrder.Sleep), ("tu peux venir ?", McOrder.Come), ("120 64 -300", McOrder.GoTo),
            ("va chercher du bois", McOrder.Collect), ("viens pas", McOrder.None), ("mdr t'es nulle", McOrder.None),
            ("ok", McOrder.None), ("je suis là", McOrder.None), ("syncs ?", McOrder.Unknown), ("syncs viens", McOrder.Come),
            ("merci", McOrder.Thanks), ("gg", McOrder.Praise),
        })
        {
            var got = McCommand.Parse(text, mcNames, called: true).Order;
            if (got != expected) Fail($"order after a call « {text} »: got {got}, expected {expected}");
        }
        if (McCommand.Parse("va chercher 5 fer", mcNames, called: true) is not { Order: McOrder.Collect, Resource: McResource.Iron, Count: 5 })
            Fail("order after a call: collect with a count");

        // Crops and farms: a crop word with any gathering verb, « pommes de terre » before « terre » (dirt), a farm before
        // « reste » (stay) and « garde » (guard), and the noun « la récolte » is not an order.
        void Crop(string text, McOrder order, McCrop? crop = null)
        {
            var got = McCommand.Parse(text, mcNames);
            if (got.Order != order || (crop is not null && got.Crop != crop))
                Fail($"crop « {text} »: got {got.Order} {got.Crop}, expected {order} {crop}");
        }
        Crop("syncs récolte le blé", McOrder.Harvest, McCrop.Wheat);
        Crop("syncs va chercher des carottes", McOrder.Harvest, McCrop.Carrot);
        Crop("syncs ramasse les pommes de terre", McOrder.Harvest, McCrop.Potato);
        Crop("syncs il me faut des patates", McOrder.Harvest, McCrop.Potato);
        Crop("syncs cueille les melons", McOrder.Harvest, McCrop.Melon);
        Crop("syncs récolte les citrouilles", McOrder.Harvest, McCrop.Pumpkin);
        Crop("syncs coupe le bambou", McOrder.Harvest, McCrop.Bamboo);
        Crop("syncs récolte la canne à sucre", McOrder.Harvest, McCrop.SugarCane);
        Crop("syncs ramasse les cactus", McOrder.Harvest, McCrop.Cactus);
        Crop("syncs moissonne", McOrder.Harvest, McCrop.Any);
        Crop("syncs récolte", McOrder.Harvest, McCrop.Any);
        Crop("syncs fais la récolte", McOrder.Harvest, McCrop.Any);
        Crop("syncs récolte le champ", McOrder.Harvest, McCrop.Any);
        Crop("syncs harvest the wheat", McOrder.Harvest, McCrop.Wheat);
        Crop("syncs récolte du sable", McOrder.Collect);
        Crop("syncs va chercher du bois dans le champ", McOrder.Collect);
        Crop("syncs va chercher de la terre", McOrder.Collect);
        Crop("syncs la récolte est prête", McOrder.Unknown);
        Crop("syncs récolte pas le blé", McOrder.None);
        Crop("syncs occupe-toi de la ferme", McOrder.Farm);
        Crop("syncs reste à la ferme", McOrder.Farm);
        Crop("syncs garde le champ", McOrder.Farm);
        Crop("syncs entretiens les cultures", McOrder.Farm);
        Crop("syncs cultive", McOrder.Farm);
        Crop("syncs ne t'occupe pas de la ferme", McOrder.None);
        Crop("syncs reste là", McOrder.Stay);
        if (McCommand.Parse("occupe toi du champ", mcNames, called: true).Order != McOrder.Farm) Fail("crop: farm after a call");

        // Harvest, the way people type it: more verbs, misspelt crops, the noun « récolte » after a gathering verb.
        foreach (var (text, crop) in new (string, McCrop)[]
        {
            ("syncs fauche le blé", McCrop.Wheat), ("syncs arrache les carottes", McCrop.Carrot), ("syncs déterre les patates", McCrop.Potato),
            ("syncs farm le blé", McCrop.Wheat), ("syncs récup les betteraves", McCrop.Beetroot), ("syncs ramène des carrotes", McCrop.Carrot),
            ("syncs va chercher des betraves", McCrop.Beetroot), ("syncs récolte les citrouiles", McCrop.Pumpkin),
            ("syncs prends les pastèques", McCrop.Melon), ("syncs ramène la récolte", McCrop.Any), ("syncs replante le champ", McCrop.Any),
            ("syncs replante", McCrop.Any), ("syncs vide le champ", McCrop.Any), ("syncs fais le champ", McCrop.Any),
            ("syncs va récolter les légumes", McCrop.Any), ("syncs tu peux récolter le blé ?", McCrop.Wheat),
            ("syncs ramasse les pom de terre", McCrop.Potato), ("syncs cueille les épis", McCrop.Wheat),
        })
            Crop(text, McOrder.Harvest, crop);

        // Farm: a care verb and a farm word, in any wording.
        foreach (var text in new[]
        {
            "syncs prends soin de mes plantations", "syncs surveille le potager", "syncs occupe-toi du jardin",
            "syncs tu peux t'occuper de la ferme ?", "syncs tu peux garder le champ", "syncs gère la ferme",
            "syncs reste dans les champs", "syncs bosse à la ferme", "syncs occupe-toi du blé", "syncs entretiens les carottes",
            "syncs travaille au champ", "syncs mode fermière", "syncs jardine", "syncs keep the farm", "syncs watch the crops",
            "syncs occupe toi de notre ferme stp",
        })
            Crop(text, McOrder.Farm);

        // Traps: words from these lists in sentences that aren't farm orders.
        foreach (var (text, order) in new (string, McOrder)[]
        {
            ("syncs reste là et ferme-la", McOrder.Stay), // « ferme » without an article is « shut »
            ("syncs garde-moi", McOrder.Guard),
            ("syncs garde des carottes", McOrder.Guard), // keeps some: never a farm or a harvest (« garde » alone was already guard)
            ("syncs il reste du blé ?", McOrder.Unknown), // « il reste »: there's some left, not « stay »
            ("syncs arrache-toi", McOrder.Unknown), ("syncs prends ça", McOrder.Unknown),
            ("syncs t'as vu mon champ ?", McOrder.Unknown), ("syncs le blé a poussé", McOrder.Unknown),
            ("syncs ne prends pas soin de la ferme", McOrder.None), ("syncs n'arrache pas les carottes", McOrder.None),
        })
            Crop(text, order);

        // Keeping animals: a care verb with an animal word, read before the farm.
        foreach (var text in new[]
        {
            "syncs occupe-toi des animaux", "syncs nourris les vaches", "syncs occupe-toi des animaux de la ferme",
            "syncs prends soin des moutons", "syncs gère l'enclos", "syncs fais de l'élevage", "syncs mode éleveuse",
            "syncs tu peux t'occuper des poules ?", "syncs take care of the animals", "syncs élève des cochons",
        })
            Crop(text, McOrder.Ranch);
        // The camp: up and down, « range le camp » taking it down (not putting her things away).
        foreach (var text in new[] { "syncs monte le camp", "syncs installe un campement", "syncs fais un camp", "monte le camp syncs" })
            Crop(text, McOrder.Camp);
        foreach (var text in new[] { "syncs démonte le camp", "syncs plie le camp", "syncs range le camp", "syncs lève le camp" })
            Crop(text, McOrder.Uncamp);
        Crop("syncs range tes affaires", McOrder.Store);
        // Guarding animals stays a patrol (it must never start a cull); a refusal is nothing.
        Crop("syncs garde les moutons", McOrder.Patrol);
        Crop("syncs surveille les vaches", McOrder.Patrol);
        Crop("syncs occupe-toi pas des animaux", McOrder.None);
        Crop("syncs ne nourris pas les cochons", McOrder.None);
        // Talk about one's own animals, and words one slip from an animal, are no ranch.
        foreach (var text in new[] { "syncs je nourris mes vaches", "syncs occupe-toi des sapins", "syncs enlève les cochons de là" })
            if (McCommand.Read(text, mcNames) == McOrder.Ranch) Fail($"ranch: « {text} » is not one");
        // Her routine's places, by name (McRoutine): a pen word anywhere, a farm or fishing word first.
        foreach (var (keys, expected) in new (string[], string)[]
        {
            (new[] { "ferme", "etang", "maison", "mine de fer" }, "ferme|etang||maison"),
            (new[] { "champ de ble", "ferme aux animaux", "base", "foret" }, "champ de ble||ferme aux animaux|base"),
            (new[] { "lac", "enclos", "potager", "qg", "chez moi" }, "potager|lac|enclos|chez moi"),
            (new[] { "foret", "chemin du lac", "mine" }, "|||"),
        })
        {
            var (farm, fish, ranch, home) = McRoutine.Classify(keys);
            if ($"{farm}|{fish}|{ranch}|{home}" is var got && got != expected) Fail($"routine places [{string.Join(", ", keys)}]: got {got}");
        }

        // Errands, small things, timers, fishing, cooking, guarding a place, remembered places.
        // Not this order, whatever else it is (talk, a call, another order).
        // Free time: back to her own life at once. A question about her evening is talk.
        Mc("syncs t'es libre", McOrder.Free);
        Mc("syncs quartier libre", McOrder.Free);
        Mc("syncs fais ta vie", McOrder.Free);
        Mc("c'est bon syncs tu peux y aller", McOrder.Free);
        Mc("syncs repos", McOrder.Free);
        Mc("syncs tu peux disposer", McOrder.Free);
        Mc("syncs tu peux arrêter de me suivre", McOrder.Free);
        NotOrder("syncs t'es libre ce soir ?", McOrder.Free);
        NotOrder("syncs t'es libre demain ?", McOrder.Free);
        NotOrder("syncs t'es libre samedi ?", McOrder.Free);
        NotOrder("syncs tu es libre quand ?", McOrder.Free);
        NotOrder("syncs vas-y", McOrder.Free);
        NotOrder("syncs libre", McOrder.Free);
        NotOrder("syncs t'es pas libre", McOrder.Free);
        NotOrder("syncs je suis libre", McOrder.Free);
        Mc("syncs bouge pas", McOrder.Stay);

        // Fetching from a chest: a fetch verb with a chest word. A store verb with one stays storing; without one, gathering.
        Mc("syncs ramène-moi du fer du coffre", McOrder.Fetch, r => r.Goods == McGoods.Iron && r.Count == 0);
        Mc("syncs va chercher 20 pains dans le coffre", McOrder.Fetch, r => r.Goods == McGoods.Food && r.Count == 20);
        Mc("syncs sors-moi le pain du tonneau", McOrder.Fetch, r => r.Goods == McGoods.Food);
        Mc("syncs apporte-moi du bois du coffre", McOrder.Fetch, r => r.Goods == McGoods.Wood);
        Mc("syncs récupère les diamants dans le coffre", McOrder.Fetch, r => r.Goods == McGoods.Diamond);
        Mc("syncs ramène-moi un truc du coffre", McOrder.Fetch, r => r.Goods is null);
        Mc("syncs ramène-moi tout du coffre", McOrder.Fetch, r => r.Goods is null);
        Mc("syncs range le pain dans le coffre", McOrder.Store);
        Mc("syncs mets tout dans le coffre", McOrder.Store);
        NotOrder("syncs ramène le fer au coffre", McOrder.Fetch);
        Mc("syncs ramène le fer du coffre", McOrder.Fetch, r => r.Goods == McGoods.Iron);
        Mc("syncs va chercher du fer", McOrder.Collect);
        NotOrder("je vais chercher du pain dans le coffre syncs", McOrder.Fetch);
        Mc("syncs ramène pas le fer du coffre", McOrder.None);

        // Crafting for a player: a craft verb and an item; « fais-moi » only with the item right after it.
        Mc("syncs fabrique-moi une pioche en fer", McOrder.Craft, r => r.Item == "iron_pickaxe" && r.Count == 1);
        Mc("syncs fais-moi 16 torches", McOrder.Craft, r => r.Item == "torch" && r.Count == 16);
        Mc("syncs crafte une table", McOrder.Craft, r => r.Item == "crafting_table");
        Mc("syncs fais-moi une épée", McOrder.Craft, r => r.Item == "sword" && r.Count == 1);
        Mc("syncs craft un coffre", McOrder.Craft, r => r.Item == "chest");
        Mc("syncs fabrique-moi une pioche en pierre", McOrder.Craft, r => r.Item == "stone_pickaxe");
        Mc("syncs tu peux me faire une canne à pêche ?", McOrder.Craft, r => r.Item == "fishing_rod");
        Mc("syncs fais-moi des échelles", McOrder.Craft, r => r.Item == "ladder" && r.Count == 3);
        Mc("syncs fais-moi 10 pioches", McOrder.Craft, r => r.Item == "pickaxe" && r.Count == 4);
        Mc("syncs fabrique-moi un truc", McOrder.Craft, r => r.Item is null);
        foreach (var notCraft in new[] { "syncs fais-moi peur", "syncs fais-moi rire", "syncs fais-moi confiance", "je fabrique une pioche syncs",
                     "syncs fais-moi du bois", "syncs fais-moi signe dans 5 min" })
            NotOrder(notCraft, McOrder.Craft);
        Mc("syncs fais-moi du bois", McOrder.Collect, r => r.Resource == McResource.Wood);
        Mc("syncs va chercher du fer", McOrder.Collect, r => r.Resource == McResource.Iron);
        // Her names for what she makes, and for each material the body may say she lacks.
        foreach (var c in McCraft.All)
            if (string.IsNullOrWhiteSpace(McCraft.Label(c.Key, 1)) || string.IsNullOrWhiteSpace(McCraft.Label(c.Key, 16))) Fail($"McCraft.Label({c.Key}): empty");
        // What the body reports making (crafting/crafted `item`): a raw game id would be read out in chat.
        foreach (var item in McCraft.All.Select(c => c.Key)
                     .Concat(from m in new[] { "wooden", "stone", "iron", "golden", "diamond" }
                             from t in new[] { "pickaxe", "axe", "shovel", "hoe", "sword" }
                             select $"{m}_{t}"))
            if (McCraft.Label(item, 1).Contains('_') || McCraft.Label(item, 8).Contains('_')) Fail($"McCraft.Label({item}): no French name");
        if (McCraft.Label("iron_pickaxe", 1) != "une pioche en fer" || McCraft.Label("torch", 16) != "16 torches" || McCraft.Label("sword", 1) != "une épée")
            Fail($"McCraft.Label: « {McCraft.Label("iron_pickaxe", 1)} », « {McCraft.Label("torch", 16)} », « {McCraft.Label("sword", 1)} »");
        foreach (var material in new[] { "iron", "gold", "diamond", "coal", "string" })
            if (!((IDictionary)Field("McCraftMaterialNames")).Contains(material)) Fail($"McCraftMaterialNames: no {material}");

        // Handing over: everything, or a kind of goods; a possessive turns a gathering word into her own stock.
        Mc("syncs vide tes poches", McOrder.Give, r => r.Goods == McGoods.All);
        Mc("syncs donne-moi tout", McOrder.Give, r => r.Goods == McGoods.All);
        Mc("syncs file moi ton stuff", McOrder.Give, r => r.Goods == McGoods.All);
        Mc("syncs donne-moi tes poissons", McOrder.Give, r => r.Goods == McGoods.Fish);
        Mc("syncs donne moi du poisson", McOrder.Give, r => r.Goods == McGoods.Fish);
        Mc("syncs passe-moi ta pioche", McOrder.Give, r => r.Goods == McGoods.Pickaxe);
        Mc("syncs donne moi ta canne à pêche", McOrder.Give, r => r.Goods == McGoods.Rod);
        Mc("syncs rends-moi mes diamants", McOrder.Give, r => r.Goods == McGoods.Diamond);
        Mc("syncs donne-moi ton fer", McOrder.Give, r => r.Goods == McGoods.Iron);
        Mc("syncs donne moi tes pommes de terre", McOrder.Give, r => r.Goods == McGoods.Produce);
        Mc("syncs donne-moi ta meilleure épée", McOrder.Give, r => r.Goods == McGoods.Weapons);
        Mc("syncs donne moi à manger", McOrder.Give, r => r.Goods == McGoods.Food);
        Mc("syncs donne-moi ton armure", McOrder.Give, r => r.Goods == McGoods.Armour);
        Mc("syncs rends-moi mon armure", McOrder.Give, r => r.Goods == McGoods.Armour);
        // Armour on and off: taken off, it stays in her pockets; with another give verb it's still handed over.
        Mc("syncs mets ton armure", McOrder.Dress);
        Mc("syncs remets ton armure stp", McOrder.Dress);
        Mc("syncs équipe-toi", McOrder.Dress);
        Mc("syncs enfile ton armure", McOrder.Dress);
        Mc("syncs put on your armor", McOrder.Dress);
        Mc("syncs enlève ton armure", McOrder.Undress);
        Mc("syncs retire ton armure", McOrder.Undress);
        Mc("syncs ôte ton armure", McOrder.Undress);
        Mc("syncs take off your armor", McOrder.Undress);
        Mc("syncs enlève ton armure et donne-la moi", McOrder.Give, r => r.Goods == McGoods.Armour);
        Mc("syncs n'enlève pas ton armure", McOrder.None);
        Mc("syncs mets ton armure dans le coffre", McOrder.Store);
        Mc("syncs mets tout dans le coffre", McOrder.Store); // « mets » needs the armour word
        Mc("syncs donne-moi ta clé", McOrder.Give, r => r.Goods is null);
        Mc("syncs donne-moi du fer", McOrder.Collect); // a partitive is still the gathering order
        Mc("syncs file-moi du charbon", McOrder.Collect);
        Mc("syncs ne me donne pas ton poisson", McOrder.None);
        Mc("syncs vide tes poches stp", McOrder.Give, r => r.Goods == McGoods.All); // « poches » is not « pêches »
        // Putting away: a store verb, or anything put into a chest.
        Mc("syncs range tes affaires", McOrder.Store);
        Mc("syncs range", McOrder.Store);
        Mc("syncs vide tes poches dans le coffre", McOrder.Store);
        Mc("syncs mets tout dans le coffre", McOrder.Store);
        Mc("syncs dépose tout", McOrder.Store);
        // Small things.
        Mc("syncs t'es où ?", McOrder.Where);
        Mc("syncs tu es où", McOrder.Where);
        Mc("syncs tes coordonnées", McOrder.Where);
        Mc("syncs donne ta position", McOrder.Where);
        Mc("syncs t'es où ? viens", McOrder.Come); // an order beats the question
        Mc("syncs assis", McOrder.Sit);
        Mc("syncs assieds-toi", McOrder.Sit);
        Mc("syncs saute", McOrder.Jump);
        Mc("syncs saute pas", McOrder.None);
        Mc("syncs fais coucou", McOrder.Wave);
        Mc("salut syncs", McOrder.Wave);
        Mc("coucou syncs !", McOrder.Wave);
        Mc("re syncs", McOrder.Wave);
        Mc("salut syncs, viens", McOrder.Come);
        Mc("salut", McOrder.Wave, called: true);
        // Timers: a verb and a length of time, the note after « de », « pour », « que ».
        Mc("syncs préviens-moi dans 5 min", McOrder.Timer, r => r.Seconds == 300 && r.Note is null);
        Mc("syncs rappelle-moi dans 10 minutes de sortir le fer du four", McOrder.Timer, r => r.Seconds == 600 && r.Note == "sortir le fer du four");
        Mc("syncs rappelle moi de retourner à la base dans 2 min", McOrder.Timer, r => r.Seconds == 120 && r.Note == "retourner à la base");
        Mc("syncs préviens moi dans une demi-heure", McOrder.Timer, r => r.Seconds == 1800);
        Mc("syncs préviens moi dans un quart d'heure", McOrder.Timer, r => r.Seconds == 900 && r.Note is null);
        Mc("syncs minuteur 30s", McOrder.Timer, r => r.Seconds == 30);
        Mc("syncs tu peux me prévenir dans cinq minutes ?", McOrder.Timer, r => r.Seconds == 300);
        Mc("syncs rappelle-moi d'arrêter le four dans 1h", McOrder.Timer, r => r.Seconds == 3600 && r.Note == "arrêter le four");
        Mc("syncs dis-moi où t'es", McOrder.Where); // « dis-moi » without a length of time is no timer
        // What she carries: asked, never fetched nor handed over.
        Mc("syncs t'as quoi sur toi ?", McOrder.Inventory);
        Mc("syncs t'as quoi ?", McOrder.Inventory);
        Mc("syncs qu'est-ce que t'as dans tes poches ?", McOrder.Inventory); // not « vide tes poches »
        Mc("syncs montre ton inventaire", McOrder.Inventory);
        Mc("syncs montre-moi ce que tu as", McOrder.Inventory);
        Mc("syncs ton inv ?", McOrder.Inventory);
        Mc("syncs inventaire", McOrder.Inventory);
        Mc("syncs y a quoi dans ton sac", McOrder.Inventory);
        Mc("syncs t'as combien de trucs ?", McOrder.Inventory);
        Mc("syncs what do you have", McOrder.Inventory);
        Mc("syncs t'as combien de fer ?", McOrder.Have, r => r.Goods == McGoods.Iron);
        Mc("syncs combien de pain t'as ?", McOrder.Have, r => r.Goods == McGoods.Food);
        Mc("syncs t'as combien de poissons", McOrder.Have, r => r.Goods == McGoods.Fish); // « combien »: not a request
        Mc("syncs il te reste combien de pommes de terre ?", McOrder.Have, r => r.Goods == McGoods.Produce); // not stay
        Mc("syncs t'as du fer ?", McOrder.Have, r => r.Goods == McGoods.Iron); // not a trip to the mine
        Mc("syncs t'aurais du bois ?", McOrder.Have, r => r.Goods == McGoods.Wood);
        Mc("syncs t'as quoi comme bois ?", McOrder.Have, r => r.Goods == McGoods.Wood);
        Mc("syncs t'as quoi comme armure ?", McOrder.Have, r => r.Goods == McGoods.Armour); // not « take it off »
        Mc("syncs t'as du fer ? ramène-en 10", McOrder.Collect); // an order beats the question
        Mc("syncs donne-moi ton inventaire", McOrder.Give, r => r.Goods == McGoods.All);
        Mc("syncs range ton inventaire", McOrder.Store);
        Mc("syncs t'as quoi comme épée", McOrder.Give, r => r.Goods == McGoods.Weapons); // goods she can't fetch: a request
        Mc("syncs regarde mes poches", McOrder.Unknown); // the player's pockets, not hers
        Mc("syncs t'as quoi contre moi", McOrder.Unknown);
        if (McCommand.Parse("syncs qu'est-ce que t'as fait ?", mcNames).Order is McOrder.Inventory or McOrder.Have)
            Fail("order « qu'est-ce que t'as fait ? »: read as a question about her pockets");
        // Fishing and cooking.
        Mc("syncs va pêcher", McOrder.Fish);
        Mc("syncs va à la pêche", McOrder.Fish);
        Mc("syncs ramène du poisson", McOrder.Fish);
        Mc("syncs ne pêche pas", McOrder.None);
        Mc("syncs cuis la viande", McOrder.Smelt, r => r.Smelt == McSmelt.Food);
        Mc("syncs fais cuire les patates", McOrder.Smelt, r => r.Smelt == McSmelt.Food);
        Mc("syncs fais fondre le fer", McOrder.Smelt, r => r.Smelt == McSmelt.Ore);
        Mc("syncs fais fondre", McOrder.Smelt, r => r.Smelt == McSmelt.Ore);
        Mc("syncs fais du verre", McOrder.Smelt, r => r.Smelt == McSmelt.Sand);
        // Guarding a place, not a player.
        Mc("syncs surveille la base", McOrder.Patrol, r => r.Place is null);
        Mc("syncs protège la maison", McOrder.Patrol, r => r.Place == "maison");
        Mc("syncs garde le coin", McOrder.Patrol);
        Mc("syncs monte la garde ici", McOrder.Patrol);
        Mc("syncs patrouille", McOrder.Patrol);
        Mc("syncs surveille la mine", McOrder.Patrol, r => r.Place == "mine");
        Mc("syncs protège-moi à la maison", McOrder.Guard); // « moi »: the player
        Mc("syncs monte la garde", McOrder.Guard);
        Mc("syncs garde le champ", McOrder.Farm);
    }
}
