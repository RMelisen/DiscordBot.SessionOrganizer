using System.Collections;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;
using static Check;
using static McOrderChecks;

// Minecraft orders, continued: remembered and other people's places, the sweep of game-chat
// wordings and the misreads it found, more materials.
public static class McOrderSweepChecks
{
    public static void Run()
    {
        // Remembered places.
        Mc("syncs retiens ici : maison", McOrder.SavePlace, r => r.Place == "maison");
        Mc("syncs retiens cet endroit comme la mine de fer", McOrder.SavePlace, r => r.Place == "mine de fer");
        Mc("syncs note ici la forêt", McOrder.SavePlace, r => r.Place == "foret");
        Mc("syncs ici c'est la ferme", McOrder.SavePlace, r => r.Place == "ferme");
        Mc("syncs enregistre ici base 2", McOrder.SavePlace, r => r.Place == "base 2");
        Mc("syncs souviens-toi de cet endroit", McOrder.SavePlace, r => r.Place is null);
        Mc("syncs ici c'est trop bien", McOrder.Come); // no article: not a name
        Mc("syncs note ça", McOrder.Unknown);
        Mc("syncs va à la mine", McOrder.GoToPlace, r => r.Place == "mine");
        Mc("syncs va à la mine de fer", McOrder.GoToPlace, r => r.Place == "mine de fer");
        Mc("syncs va à la base 2", McOrder.GoToPlace, r => r.Place == "base 2");
        Mc("syncs rentre à la maison", McOrder.GoToPlace, r => r.Place == "maison");
        Mc("syncs rentre", McOrder.GoToPlace, r => r.Place == "");
        Mc("syncs rentre chez nous", McOrder.GoToPlace, r => r.Place == "chez nous"); // a home word: the service finds the home
        Mc("syncs va au village", McOrder.GoToPlace, r => r.Place == "village"); // unknown: the service says so
        Mc("syncs montre-moi le chemin vers la mine", McOrder.Guide, r => r.Place == "mine");
        Mc("syncs emmène-moi à la maison", McOrder.Guide, r => r.Place == "maison");
        Mc("syncs accompagne-moi jusqu'à la base 2", McOrder.Guide, r => r.Place == "base 2");
        Mc("syncs où est la mine ?", McOrder.WherePlace, r => r.Place == "mine");
        Mc("syncs c'est où la maison", McOrder.WherePlace, r => r.Place == "maison");
        Mc("syncs oublie la mine", McOrder.ForgetPlace, r => r.Place == "mine");
        Mc("syncs oublie ça", McOrder.Unknown);
        Mc("syncs tes lieux", McOrder.ListPlaces);
        Mc("syncs quels endroits tu connais ?", McOrder.ListPlaces);
        Mc("syncs accompagne-moi", McOrder.Follow); // no place: follow, as before
        Mc("syncs va voir", McOrder.Unknown);
        if (McCommand.PlaceKey("Forêt  Noire !") != "foret noire" || McCommand.PlaceName("syncs retiens ici la Forêt", "foret") != "forêt")
            Fail("place key and name");
        // Someone else's place: « de X » or « chez X » (X's home), X one of the players; only when it's about a place.
        var placeOwners = new[] { "Rodhengard", "Papa", "xX_Bob_Xx" };
        void McOf(string text, McOrder order, string? place, string? of)
        {
            var got = McCommand.Parse(text, mcNames, false, mcPlaces, placeOwners);
            if (got.Order != order || got.Place != place || got.Of != of)
                Fail($"order « {text} »: got {got.Order} place={got.Place} of={got.Of}");
        }
        McOf("syncs guide-moi à la maison de Rodhengard", McOrder.Guide, "maison", "Rodhengard");
        McOf("syncs emmène-moi chez Rodhengard", McOrder.Guide, "maison", "Rodhengard");
        McOf("syncs va chez rodhengard", McOrder.GoToPlace, "maison", "Rodhengard");
        McOf("syncs va à la mine de Papa", McOrder.GoToPlace, "mine", "Papa");
        McOf("syncs où est la base de xX_Bob_Xx ?", McOrder.WherePlace, "base", "xX_Bob_Xx");
        McOf("syncs retiens ici : maison de Rodhengard", McOrder.SavePlace, "maison", "Rodhengard");
        McOf("syncs oublie la mine de Rodhengard", McOrder.ForgetPlace, "mine", "Rodhengard");
        McOf("syncs liste les lieux de Rodhengard", McOrder.ListPlaces, null, "Rodhengard");
        McOf("syncs protège la maison de Rodhengard", McOrder.Patrol, "maison", "Rodhengard");
        McOf("syncs va à la mine de fer", McOrder.GoToPlace, "mine de fer", null);
        McOf("syncs va à la maison", McOrder.GoToPlace, "maison", null);
        McOf("syncs va chercher du bois chez Rodhengard", McOrder.Collect, null, null); // wood, wherever it is
        McOf("syncs donne-moi le pain de Rodhengard", McOrder.Give, null, null);
        Covers("McGoodsNames", typeof(McGoods));
        Covers("McSmeltNames", typeof(McSmelt));

        // The ways people type the newer orders, and ordinary chat that must stay quiet: a sweep of game-chat wordings,
        // read with five known places. Expected: the order, optionally « :detail » (goods, smelt, place, seconds).
        var sweepPlaces = new[] { "maison", "mine", "base", "ferme", "village" };
        foreach (var (text, expected) in new (string, string)[]
        {
            //  Give
            ("syncs vide tes poches", "Give:All"), ("syncs vide ton inv", "Give:All"),
            ("syncs vide ton inventaire stp", "Give:All"), ("syncs donne moi tt", "Give:All"),
            ("syncs donne tout ce que tu as", "Give:All"), ("syncs donne moi tout ce que t'as", "Give:All"),
            ("syncs file moi tout", "Give:All"), ("syncs lâche tout", "Give:All"), ("syncs drop tout", "Give:All"),
            ("syncs drop all", "Give:All"), ("syncs balance ton stuff", "Give:All"),
            ("syncs montre ce que t'as", "Inventory"), ("syncs qu'est ce que t'as sur toi", "Inventory"),
            ("syncs rends moi mes affaires", "Give:All"), ("syncs rends moi mon stuff", "Give:All"),
            ("syncs donne ton loot", "Give:All"), ("syncs donne moi tes poissons", "Give:Fish"),
            ("syncs file moi les poissons", "Give:Fish"), ("syncs donne moi le poisson", "Give:Fish"),
            ("syncs t'as du poisson ?", "Give:Fish"), ("syncs passe moi ta canne", "Give:Rod"),
            ("syncs donne moi ta canne à pêche", "Give:Rod"), ("syncs donne moi ton épée", "Give:Weapons"),
            ("syncs file moi ton arme", "Give:Weapons"), ("syncs prête moi ta pioche", "Give:Pickaxe"),
            ("syncs passe moi la pioche", "Give:Pickaxe"), ("syncs donne moi une pioche", "Give:Pickaxe"),
            ("syncs lance moi ta hache", "Give:Axe"), ("syncs donne moi tes outils", "Give:Tools"),
            ("syncs donne moi ton arc", "Give:Bow"), ("syncs donne moi tes flèches", "Give:Arrows"),
            ("syncs donne moi ton armure", "Give:Armour"), ("syncs enlève ton armure", "Undress"),
            ("syncs donne moi à manger", "Give:Food"), ("syncs j'ai faim", "Give:Food"),
            ("syncs t'as à manger ?", "Give:Food"), ("syncs file moi de la bouffe", "Give:Food"),
            ("syncs donne moi du pain", "Give:Food"), ("syncs donne moi tes graines", "Give:Seeds"),
            ("syncs donne moi ton bois", "Give:Wood"), ("syncs rends moi mes diamants", "Give:Diamond"),
            ("syncs donne moi tes diams", "Give:Diamond"), ("syncs file moi ton charbon", "Give:Coal"),
            ("syncs donne moi tes carottes", "Give:Produce"), ("syncs donne moi ta récolte", "Give:Produce"),
            ("syncs give me your fish", "Give:Fish"), ("syncs give me everything", "Give:All"),
            //  Store
            ("syncs range tes affaires", "Store"), ("syncs range ton inventaire", "Store"), ("syncs range tout", "Store"),
            ("syncs range tout dans le coffre", "Store"), ("syncs vide toi dans le coffre", "Store"),
            ("syncs mets tout dans le coffre", "Store"), ("syncs mets tes poissons dans le coffre", "Store"),
            ("syncs dépose tout au coffre", "Store"), ("syncs pose tout dans le coffre", "Store"),
            ("syncs stocke tout", "Store"), ("syncs vide ton inventaire dans le coffre", "Store"),
            ("syncs tu peux ranger tes affaires ?", "Store"), ("syncs fais du rangement", "Store"),
            ("syncs ranges toi", "Unknown"), ("syncs put everything in the chest", "Store"),
            //  Where
            ("syncs t'es où", "Where"), ("syncs t ou", "Where"), ("syncs tes où", "Where"), ("syncs tu es où ?", "Where"),
            ("syncs où es tu", "Where"), ("syncs ou t'es", "Where"), ("syncs t'es où là", "Where"),
            ("syncs t'es passée où ?", "Where"), ("syncs tu es passée où", "Where"),
            ("syncs t'es où exactement", "Where"), ("syncs tes coords", "Where"), ("syncs tes coordonnées stp", "Where"),
            ("syncs donne tes coords", "Where"), ("syncs envoie tes coordonnées", "Where"),
            ("syncs ta position ?", "Where"), ("syncs t'es perdue ?", "Where"), ("syncs where are you", "Where"),
            ("syncs where r u", "Where"), ("syncs coords", "Where"),
            //  Sit
            ("syncs assis", "Sit"), ("syncs assise", "Sit"), ("syncs assieds toi", "Sit"), ("syncs assis toi", "Sit"),
            ("syncs assois-toi", "Sit"), ("syncs couchée", "Sit"), ("syncs sit", "Sit"), ("syncs accroupis toi", "Sit"),
            ("syncs baisse toi", "Sit"),
            //  Jump
            ("syncs saute", "Jump"), ("syncs saute saute", "Jump"), ("syncs sautes", "Jump"),
            ("syncs fais un saut", "Jump"), ("syncs jump", "Jump"), ("syncs sautille", "Jump"),
            ("syncs fais des bonds", "Jump"), ("syncs danse", "Jump"),
            //  Wave
            ("salut syncs", "Wave"), ("slt syncs", "Wave"), ("coucou syncs", "Wave"), ("cc syncs", "Wave"),
            ("yo syncs", "Wave"), ("wesh syncs", "Wave"), ("re syncs", "Wave"), ("bonjour syncs", "Wave"),
            ("bonsoir syncs", "Wave"), ("hello syncs", "Wave"), ("hey syncs", "Wave"), ("hi syncs", "Wave"),
            ("salut sync ça va ?", "Wave"), ("coucou syncs tu vas bien", "Wave"), ("syncs fais coucou", "Wave"),
            ("syncs fais coucou à bob", "Wave"), ("syncs dis bonjour", "Wave"),
            ("syncs dis salut à tout le monde", "Wave"), ("syncs salue tout le monde", "Wave"),
            ("bonne nuit syncs", "Bye"), ("au revoir syncs", "Bye"), ("bye syncs", "Bye"),
            //  Timer
            ("syncs préviens moi dans 5 min", "Timer:300"), ("syncs previens moi dans 5mn", "Timer:300"),
            ("syncs rappelle moi dans 10 min", "Timer:600"), ("syncs rappel moi dans 10 min", "Timer:600"),
            ("syncs rapelle moi dans 10 min", "Timer:600"), ("syncs dans 5 min tu me préviens", "Timer:300"),
            ("syncs dans 5 min rappelle moi de rentrer", "Timer:300"),
            ("syncs préviens moi quand ça fait 5 min", "Timer:300"), ("syncs timer 5 min", "Timer:300"),
            ("syncs lance un timer de 3 min", "Timer:180"), ("syncs mets un minuteur de 10 minutes", "Timer:600"),
            ("syncs minuteur 2 min", "Timer:120"), ("syncs réveille moi dans 20 min", "Timer:1200"),
            ("syncs rappelle moi dans 1h de nourrir les poules", "Timer:3600"),
            ("syncs rappelle moi dans 1 heure", "Timer:3600"), ("syncs rappelle moi dans 1h30", "Timer:5400"),
            ("syncs préviens moi dans 30 secondes", "Timer:30"), ("syncs préviens moi dans 30sec", "Timer:30"),
            ("syncs dis moi quand 5 minutes sont passées", "Timer:300"), ("syncs remind me in 5 min", "Timer:300"),
            ("syncs rappelle moi dans 5 min que le four est prêt", "Timer:300"),
            ("syncs rappelle-moi dans 5 min pour la potion", "Timer:300"),
            ("syncs dans 10 min faut que je parte, préviens moi", "Timer:600"), ("syncs ping moi dans 5", "Timer:300"),
            //  Fish
            ("syncs va pêcher", "Fish"), ("syncs pêche", "Fish"), ("syncs va pecher", "Fish"),
            ("syncs pêche du poisson", "Fish"), ("syncs tu peux pêcher ?", "Fish"), ("syncs va à la pêche", "Fish"),
            ("syncs ramène du poisson", "Fish"), ("syncs il me faut du poisson", "Fish"),
            ("syncs va chercher du poisson", "Fish"), ("syncs attrape des poissons", "Fish"),
            ("syncs fais de la pêche", "Fish"), ("syncs mode pêche", "Fish"), ("syncs go fish", "Fish"),
            ("syncs fish", "Fish"), ("syncs pêche moi des poissons", "Fish"),
            //  Smelt
            ("syncs cuis la viande", "Smelt:Food"), ("syncs cuit la viande", "Smelt:Food"),
            ("syncs fais cuire la viande", "Smelt:Food"), ("syncs fais cuire le poisson", "Smelt:Food"),
            ("syncs cuisine", "Smelt:Food"), ("syncs cuisine le poisson", "Smelt:Food"),
            ("syncs fais à manger", "Smelt:Food"), ("syncs fais griller le steak", "Smelt:Food"),
            ("syncs cuis les patates", "Smelt:Food"), ("syncs fais cuire les pommes de terre", "Smelt:Food"),
            ("syncs fais fondre le fer", "Smelt:Ore"), ("syncs fonds le fer", "Smelt:Ore"),
            ("syncs fond le fer", "Smelt:Ore"), ("syncs fais fondre l'or", "Smelt:Ore"),
            ("syncs fais des lingots", "Smelt:Ore"), ("syncs fais des lingots de fer", "Smelt:Ore"),
            ("syncs mets le fer au four", "Smelt:Ore"), ("syncs mets la viande dans le four", "Smelt:Food"),
            ("syncs fais du verre", "Smelt:Sand"), ("syncs cuis le sable", "Smelt:Sand"),
            ("syncs smelt the iron", "Smelt:Ore"), ("syncs cook the meat", "Smelt:Food"),
            ("syncs utilise le four", "Smelt:Food"),
            //  Patrol
            ("syncs surveille la base", "Patrol:base"), ("syncs surveille la maison", "Patrol:maison"),
            ("syncs garde la base", "Patrol:base"), ("syncs garde la maison", "Patrol:maison"),
            ("syncs protège la base", "Patrol:base"), ("syncs défends la base", "Patrol:base"),
            ("syncs protège le village", "Patrol:village"), ("syncs protège les animaux", "Patrol"),
            ("syncs surveille les vaches", "Patrol"), ("syncs monte la garde ici", "Patrol"),
            ("syncs monte la garde devant la porte", "Patrol"), ("syncs garde l'entrée", "Patrol"),
            ("syncs surveille ici", "Patrol"), ("syncs reste ici et surveille", "Patrol"),
            ("syncs reste ici pour surveiller", "Patrol"), ("syncs reste là et protège", "Patrol"),
            ("syncs garde le coin", "Patrol"), ("syncs patrouille", "Patrol"), ("syncs fais une ronde", "Patrol"),
            ("syncs mode sentinelle", "Patrol"), ("syncs tu peux surveiller la base ?", "Patrol:base"),
            ("syncs protège la base pendant qu'on mine", "Patrol:base"), ("syncs guard the base", "Patrol:base"),
            //  Places
            ("syncs retiens ici : maison", "SavePlace:maison"), ("syncs retiens cet endroit : mine", "SavePlace:mine"),
            ("syncs souviens toi d'ici, c'est la mine", "SavePlace:mine"),
            ("syncs enregistre ici sous le nom base", "SavePlace:base"), ("syncs note ici la mine", "SavePlace:mine"),
            ("syncs ici c'est la maison", "SavePlace:maison"), ("syncs ici c'est chez nous", "SavePlace:maison"),
            ("syncs c'est ici la base", "SavePlace:base"), ("syncs retiens ça comme la maison", "SavePlace:maison"),
            ("syncs mémorise cet endroit maison", "SavePlace:maison"),
            ("syncs sauvegarde la position : portail", "SavePlace:portail"), ("syncs set home", "SavePlace:maison"),
            ("syncs sethome", "SavePlace:maison"), ("syncs ajoute ce lieu : mine", "SavePlace:mine"),
            ("syncs va à la maison", "GoToPlace:maison"), ("syncs va a la mine", "GoToPlace:mine"),
            ("syncs vas à la base", "GoToPlace:base"), ("syncs va au village", "GoToPlace:village"),
            ("syncs rentre à la maison", "GoToPlace:maison"), ("syncs rentre", "GoToPlace"),
            ("syncs rentre à la base", "GoToPlace:base"), ("syncs retourne à la mine", "GoToPlace:mine"),
            ("syncs go home", "GoToPlace"), ("syncs file à la maison", "GoToPlace:maison"),
            ("syncs cours à la maison", "GoToPlace:maison"), ("syncs dirige toi vers la mine", "GoToPlace:mine"),
            ("syncs rejoins la base", "GoToPlace:base"), ("syncs tp maison", "GoToPlace:maison"),
            ("syncs go mine", "GoToPlace:mine"), ("syncs à la maison", "GoToPlace:maison"),
            ("syncs direction la mine", "GoToPlace:mine"), ("syncs montre moi le chemin de la mine", "Guide:mine"),
            ("syncs montre moi le chemin vers la maison", "Guide:maison"), ("syncs montre moi la mine", "Guide:mine"),
            ("syncs emmène moi à la mine", "Guide:mine"), ("syncs emmène moi à la maison", "Guide:maison"),
            ("syncs amène moi au village", "Guide:village"), ("syncs guide moi jusqu'à la base", "Guide:base"),
            ("syncs conduis moi à la ferme", "Guide:ferme"), ("syncs ramène moi à la maison", "Guide:maison"),
            ("syncs c'est par où la mine ?", "WherePlace:mine"),
            ("syncs je suis perdu, ramène moi à la base", "Guide:base"),
            ("syncs on rentre à la maison", "GoToPlace:maison"), ("syncs où est la mine ?", "WherePlace:mine"),
            ("syncs c'est où la maison", "WherePlace:maison"), ("syncs elle est où la base ?", "WherePlace:base"),
            ("syncs la mine c'est où ?", "WherePlace:mine"), ("syncs coords de la mine", "WherePlace:mine"),
            ("syncs les coordonnées de la maison", "WherePlace:maison"), ("syncs oublie la mine", "ForgetPlace:mine"),
            ("syncs efface la maison", "ForgetPlace:maison"), ("syncs supprime la base", "ForgetPlace:base"),
            ("syncs tes lieux", "ListPlaces"), ("syncs quels endroits tu connais ?", "ListPlaces"),
            ("syncs tu connais quels lieux", "ListPlaces"), ("syncs liste des lieux", "ListPlaces"),
            ("syncs tes waypoints", "ListPlaces"), ("syncs les endroits que tu connais", "ListPlaces"),
            //  Not orders: ordinary chat that names her
            ("syncs t'es nulle", "Unknown"), ("syncs t'es où dans le classement ?", "Where"),
            ("syncs la base est jolie", "Unknown"), ("syncs regarde ma maison", "Unknown"),
            ("syncs j'adore la pêche", "Unknown"), ("syncs le fer c'est cuit", "Unknown"),
            ("syncs c'est la maison de bob", "Unknown"), ("syncs ma maison a brûlé", "Unknown"),
            ("syncs t'as vu ma base ?", "Unknown"), ("syncs tu sais où est bob ?", "Unknown"),
            ("syncs la mine est dangereuse", "Unknown"), ("syncs j'ai trouvé un village", "Unknown"),
            ("syncs le four est plein", "Unknown"), ("syncs je range ma chambre", "Unknown"),
            ("syncs bonne idée", "Unknown"), ("syncs t'es drôle", "Unknown"),
            ("syncs elle est où ta maison ?", "WherePlace:maison"), ("syncs c'est quoi ton inventaire", "Inventory"),
            ("syncs j'ai pêché un poisson", "Unknown"), ("syncs tu manges quoi ?", "Unknown"),
            ("syncs je reviens dans 5 min", "Unknown"), ("syncs je pars dans 10 minutes", "Unknown"),
            ("syncs ça fait 5 min que j'attends", "None"), ("syncs t'as des poissons ?", "Give:Fish"),
            ("syncs la base c'est la meilleure", "Unknown"), ("syncs salut la base", "Wave"),
            ("syncs tu me manques", "Unknown"), ("syncs t'as faim ?", "Unknown"), ("syncs je mange", "Unknown"),
            ("syncs je pêche", "Unknown"), ("syncs je cuis la viande", "Unknown"),
            ("syncs je range mon coffre", "Unknown"), ("syncs on se retrouve à la maison", "Unknown"),
            ("syncs ma maison est là", "Unknown"), ("syncs c'est ma base", "Unknown"),
            ("syncs le village est loin", "Unknown"), ("syncs t'as vu la mine ?", "Unknown"),
            ("syncs elle est belle la base", "Unknown"), ("syncs dans 5 min on y va", "Unknown"),
            ("syncs t'as une épée ?", "Give:Weapons"), ("syncs t'as quoi comme épée", "Give:Weapons"),
            ("syncs ta pioche est cassée", "Unknown"), ("syncs je t'ai donné ma pioche", "Unknown"),
            ("syncs j'ai vidé mes poches", "Unknown"), ("syncs regarde mes poches", "Unknown"),
            ("syncs salut c'est moi", "Wave"), ("syncs dis moi tout", "Unknown"),
            ("syncs dis moi ce que tu fais", "Unknown"), ("syncs dis moi t'es où", "Where"), ("syncs bonne nuit", "Bye"),
            ("syncs danse pas", "None"), ("syncs saute dans le vide", "Jump"), ("syncs le four est où ?", "Unknown"),
            ("syncs t'as du charbon ?", "Have:Coal"), ("syncs mets toi là", "Unknown"), ("syncs pose toi là", "Sit"),
            ("syncs j'ai trop faim", "Give:Food"), ("syncs viens on rentre", "Come"),
            ("syncs on va à la mine", "GoToPlace:mine"), ("syncs je vais à la mine", "Unknown"),
            ("syncs je retourne à la base", "Unknown"), ("syncs je rentre", "Unknown"),
            ("syncs tu rentres ?", "GoToPlace"), ("syncs rentre pas", "None"), ("syncs garde mes affaires", "Patrol"),
            ("syncs protège mes affaires", "Patrol"), ("syncs surveille mes coffres", "Patrol"),
            ("syncs la base est protégée ?", "Unknown"), ("syncs reste ici", "Stay"),
            ("syncs reste là je reviens", "Stay"), ("syncs reste ici et attends", "Stay"),
            ("syncs t'es où ? je te vois pas", "Where"), ("syncs coucou t'es où", "Where"),
        })
        {
            var r = McCommand.Parse(text, mcNames, false, sweepPlaces);
            var got = r.Order + (r.Goods is { } g ? ":" + g : "") + (r.Smelt is { } sm ? ":" + sm : "") + (r.Place is { } pl ? ":" + pl : "")
                      + (r.Seconds > 0 ? ":" + r.Seconds : "");
            if (got != expected && !got.StartsWith(expected + ":")) Fail($"sweep « {text} »: got {got}, expected {expected}");
        }

        // Misreads found by a sweep of game-chat wordings, and the chat each fix must leave alone. Same places and format.
        foreach (var (text, expected) in new (string, string)[]
        {
            //  A common word one slip from a list word is itself: « reviens » once saved a place (« retiens »).
            ("syncs reviens", "Come"), ("syncs reviens ici", "Come"), ("syncs tu peux revenir ?", "Come"),
            ("syncs t'as vu mon jardin ?", "Unknown"), ("syncs c'est entre toi et moi", "Unknown"),
            ("syncs regarde le coucher de soleil", "Unknown"), ("syncs le soleil se couche", "Unknown"),
            ("syncs je suis fauché", "Unknown"), ("syncs ma cuisine est belle", "Unknown"), ("syncs ça marche à la maison ?", "Unknown"),
            ("syncs t'as bien dormi ?", "Unknown"),
            //  Come, the way people hurry her
            ("syncs grouille", "Come"), ("syncs dépêche toi", "Come"), ("syncs magne toi", "Come"), ("syncs t'arrives ?", "Come"),
            ("syncs cmon", "Come"), ("syncs tp sur moi", "Come"), ("syncs retrouve moi", "Come"), ("syncs il faut que tu viennes", "Come"),
            ("syncs viens ou pas ?", "Come"), ("syncs y a du fer par ici ?", "Unknown"),
            //  A known place right after come or stay: there
            ("syncs viens à la base", "GoToPlace:base"), ("syncs viens me rejoindre à la mine", "GoToPlace:mine"),
            ("syncs attends moi à la maison", "GoToPlace:maison"), ("syncs reste à la mine", "GoToPlace:mine"),
            ("syncs retrouve moi à la mine", "GoToPlace:mine"), ("syncs viens voir ma base", "Come"),
            //  Follow, and stopping something she keeps doing
            ("syncs ne me lâche pas", "Follow"), ("syncs tu me suis ou pas ?", "Follow"), ("syncs je veux que tu me suives", "Follow"),
            ("syncs ne me suis plus", "Stay"), ("syncs me suis plus", "Stay"), ("syncs ne me protège plus", "Stay"),
            ("syncs ne pêche plus", "Stay"), ("syncs tu me suis plus ?", "None"), ("syncs je vais te suivre", "Unknown"),
            //  Stay, refused, and talk that isn't
            ("syncs lâche moi", "Stay"), ("syncs laisse moi tranquille", "Stay"), ("syncs fais rien", "Stay"),
            ("syncs reste pas plantée là", "None"), ("syncs reste pas avec moi", "Stay"), ("syncs ne bouge plus", "Stay"),
            ("syncs arrête de dire ça", "Unknown"), ("syncs arrête de me parler", "Unknown"), ("syncs arrête de me suivre", "Stay"),
            ("syncs lâche moi tes diamants", "Give:Diamond"), ("syncs laisse moi passer", "Unknown"),
            //  Guard: a monster to kill; help with something else is that something
            ("syncs tue les zombies", "Guard"), ("syncs tue l'araignée", "Guard"), ("syncs bute le creeper", "Guard"),
            ("syncs défonce les mobs", "Guard"), ("syncs ne tue pas les zombies", "None"), ("syncs aide moi", "Guard"),
            ("syncs aide moi à couper du bois", "Collect"), ("syncs aide moi à récolter le blé", "Harvest"),
            //  Gathering: more verbs, no verb, the wood alone, stacks, and a place after the resource
            ("syncs fais moi du bois", "Collect"), ("syncs prends du bois", "Collect"), ("syncs va farmer du fer", "Collect"),
            ("syncs envoie moi du bois", "Collect"), ("syncs bûcheronne", "Collect"), ("syncs du bois stp", "Collect"),
            ("syncs 20 bois", "Collect"), ("syncs il faut de la pierre", "Collect"), ("syncs je veux du fer", "Collect"),
            ("syncs un arbre", "Unknown"), ("syncs du bois ou de la pierre ?", "Unknown"), ("syncs prends mon bois", "Unknown"),
            ("syncs ramène de l'épicéa", "Collect"), ("syncs va chercher du bois à la maison", "Collect"),
            ("syncs va chercher du fer à la mine", "Collect"),
            //  The player's own doing, past or planned, and someone else's
            ("syncs on a trouvé du fer", "Unknown"), ("syncs bob a coupé du bois", "Unknown"), ("syncs j'ai déjà coupé du bois", "Unknown"),
            ("syncs je vais chercher du bois", "Unknown"), ("syncs je dois aller chercher du bois", "Unknown"),
            ("syncs je suis en train de couper du bois", "None"), ("syncs je m'en vais suis moi", "Follow"),
            ("syncs on va chercher du bois", "Collect"), ("syncs y a quoi dans ton sac", "Inventory"),
            //  For later, or asking why: talk
            ("syncs tu peux venir demain ?", "Unknown"), ("syncs pourquoi tu me suis ?", "Unknown"),
            //  Handing over
            ("syncs donne moi tout ton bois", "Give:Wood"), ("syncs donne moi tout le fer", "Give:Iron"),
            ("syncs donne moi un truc à manger", "Give:Food"), ("syncs file moi à bouffer", "Give:Food"),
            ("syncs t'aurais pas du pain ?", "Give:Food"), ("syncs t'as pas un peu de bouffe ?", "Give:Food"),
            ("syncs je peux avoir ton épée ?", "Give:Weapons"), ("syncs donne moi ton avis", "Unknown"),
            ("syncs nourris moi", "Give:Food"), ("syncs jsuis affamé", "Give:Food"), ("syncs j'ai super faim", "Give:Food"),
            ("syncs j'ai plus faim", "Unknown"), ("syncs t'as faim ?", "Unknown"),
            //  Sleep, goodbyes, greetings, praise
            ("syncs à dodo", "Sleep"), ("syncs on dort", "Sleep"), ("syncs va dans le lit", "Sleep"),
            ("syncs je vais dormir", "Bye"), ("syncs je vais me coucher", "Bye"), ("syncs je vais dormir va dormir aussi", "Sleep"),
            ("syncs a+", "Bye"), ("syncs @+", "Bye"), ("syncs bonne soirée", "Bye"), ("syncs je déco", "Bye"),
            ("syncs je dois y aller", "Bye"), ("wsh syncs", "Wave"),
            ("syncs t'es une boss", "Praise"), ("syncs t'assures", "Praise"), ("syncs parfait", "Praise"),
            ("syncs c'est pas parfait", "None"),
            //  A wider sweep: fishing, ranch, guarding, standing up, places, timers, the camp, English
            ("syncs lance ta ligne", "Fish"), ("syncs t'aimes pêcher ?", "Unknown"), ("syncs tu sais pêcher ?", "Unknown"),
            ("syncs je t'aime suis moi", "Follow"), ("syncs tonds les moutons", "Ranch"),
            ("syncs fais se reproduire les vaches", "Ranch"), ("syncs t'es tendre avec les vaches", "Unknown"),
            ("syncs fais le guet", "Patrol"), ("syncs à genoux", "Sit"), ("syncs lève toi", "Stay"), ("syncs debout", "Stay"),
            ("syncs fais un salto", "Jump"), ("syncs fais moi un bisou", "Unknown"), ("syncs bisous", "Bye"),
            ("syncs tu connais le chemin de la base ?", "Guide:base"), ("syncs on est où ?", "Where"),
            ("syncs t'as quels endroits ?", "ListPlaces"), ("syncs suis moi on va à la mine", "Follow"),
            ("syncs suis moi on va au village", "Follow"), ("syncs viens on va à la mine", "GoToPlace:mine"),
            ("syncs réveille moi à 20h", "Unknown"), ("syncs timer 10", "Timer:600"), ("syncs minuteur 5", "Timer:300"),
            ("syncs rappelle moi dans 2h", "Timer:7200"), ("syncs fais un feu de camp", "Camp"),
            ("syncs range le campement", "Uncamp"), ("syncs go to bed", "Sleep"), ("syncs i need wood", "Collect"),
            ("syncs i need food", "Give:Food"),
        })
        {
            var r = McCommand.Parse(text, mcNames, false, sweepPlaces);
            var got = r.Order + (r.Goods is { } g ? ":" + g : "") + (r.Smelt is { } sm ? ":" + sm : "") + (r.Place is { } pl ? ":" + pl : "")
                      + (r.Seconds > 0 ? ":" + r.Seconds : "");
            if (got != expected && !got.StartsWith(expected + ":")) Fail($"misread sweep « {text} »: got {got}, expected {expected}");
        }
        Request("syncs récupère 2 stacks de bois", McOrder.Collect, resource: McResource.Wood, count: McCommand.MaxCount);
        Request("syncs ramène 1 stack de pierre", McOrder.Collect, resource: McResource.Stone, count: 64);
        Request("syncs 20 bois", McOrder.Collect, resource: McResource.Wood, count: 20);
        Request("syncs bûcheronne", McOrder.Collect, resource: McResource.Wood, count: McCommand.DefaultCount);

        // More materials to gather, and more things to hand over or count. Fetchable ones need a possessive to be handed over
        // (« donne-moi du gravier » is a trip); the rest take any article.
        foreach (var (text, resource) in new (string, McResource)[]
        {
            ("syncs va chercher du gravier", McResource.Gravel), ("syncs ramène de l'argile", McResource.Clay),
            ("syncs mine de la redstone", McResource.Redstone), ("syncs va chercher de la poudre de redstone", McResource.Redstone),
            ("syncs trouve du lapis", McResource.Lapis), ("syncs mine du lapis lazuli", McResource.Lapis),
            ("syncs va chercher des émeraudes", McResource.Emerald), ("syncs ramène du quartz", McResource.Quartz),
            ("syncs va chercher du granite", McResource.Granite), ("syncs il me faut de la diorite", McResource.Diorite),
            ("syncs ramène de l'andésite", McResource.Andesite), ("syncs ramène du sable rouge", McResource.Sand),
            ("syncs du gravier stp", McResource.Gravel), ("syncs donne moi du gravier", McResource.Gravel),
        })
            Request(text, McOrder.Collect, resource: resource);
        Request("syncs il me faut 20 redstone", McOrder.Collect, resource: McResource.Redstone, count: 20);
        foreach (var (text, expected) in new (string, string)[]
        {
            ("syncs donne moi tes torches", "Give:Torches"), ("syncs donne moi des torches", "Give:Torches"),
            ("syncs t'as des torches ?", "Give:Torches"), ("syncs file moi des bâtons", "Give:Sticks"),
            ("syncs donne moi ta laine", "Give:Wool"), ("syncs donne moi du cuir", "Give:Leather"),
            ("syncs passe moi de la ficelle", "Give:String"), ("syncs donne moi tes plumes", "Give:Feathers"),
            ("syncs donne moi les os", "Give:Bones"), ("syncs donne moi ta poudre à canon", "Give:Gunpowder"),
            ("syncs donne moi ta poudre de redstone", "Give:Redstone"), ("syncs donne moi ton silex", "Give:Flint"),
            ("syncs donne moi tes perles de l'ender", "Give:Pearls"), ("syncs donne moi tes ender pearls", "Give:Pearls"),
            ("syncs donne moi tes pousses", "Give:Saplings"), ("syncs donne moi tes cisailles", "Give:Shears"),
            ("syncs donne moi ta houe", "Give:Hoe"), ("syncs donne moi un seau", "Give:Buckets"),
            ("syncs donne moi du verre", "Give:Glass"), ("syncs fais du verre", "Smelt:Sand"),
            ("syncs donne moi ton gravier", "Give:Gravel"), ("syncs donne moi tes émeraudes", "Give:Emerald"),
            ("syncs t'as de la redstone ?", "Have:Redstone"), ("syncs t'as du lapis ?", "Have:Lapis"),
            ("syncs t'as combien de torches ?", "Have:Torches"), ("syncs t'as combien d'émeraudes ?", "Have:Emerald"),
            ("syncs mets la laine dans le coffre", "Store"),
            // Not requests for the goods.
            ("syncs t'as vu mes os ?", "Unknown"), ("syncs t'as vu ma pioche ?", "Unknown"), ("syncs pousse toi", "Unknown"),
            ("syncs j'ai trouvé de la redstone", "Unknown"), ("syncs t'as cuit la viande ?", "Unknown"),
            ("syncs t'as pris mon épée ?", "Unknown"),
            // Odds and ends; « œ » spelled out; « bâtons de blaze » before « bâtons ».
            ("syncs donne moi tes œufs", "Give:Eggs"), ("syncs donne moi des oeufs", "Give:Eggs"),
            ("syncs donne moi tes boules de slime", "Give:Slime"), ("syncs donne moi tes bâtons de blaze", "Give:BlazeRods"),
            ("syncs donne moi des bâtons", "Give:Sticks"), ("syncs donne moi du papier", "Give:Paper"),
            ("syncs donne moi tes livres", "Give:Books"), ("syncs donne moi ton lit", "Give:Beds"), ("syncs va au lit", "Sleep"),
            ("syncs donne moi la selle", "Give:Saddle"), ("syncs donne moi la boussole", "Give:Compass"),
            ("syncs t'as une carte ?", "Give:Map"), ("syncs donne moi de l'encre", "Give:Ink"), ("syncs tue le blaze", "Guard"),
            // Armour pieces are her armour.
            ("syncs mets ton casque", "Dress"), ("syncs mets tes bottes", "Dress"), ("syncs mets un bouclier", "Dress"),
            ("syncs enlève ton casque", "Undress"), ("syncs donne moi ton bouclier", "Give:Armour"),
            // Cooking clay into bricks and logs into charcoal.
            ("syncs cuis l'argile", "Smelt:Clay"), ("syncs fais des briques", "Smelt:Clay"),
            ("syncs fais du charbon de bois", "Smelt:Wood"), ("syncs mets les bûches au four", "Smelt:Wood"),
            ("syncs cuis la viande", "Smelt:Food"), ("syncs ça crame", "Unknown"),
        })
        {
            var r = McCommand.Parse(text, mcNames, false, sweepPlaces);
            var got = r.Order + (r.Goods is { } g ? ":" + g : "") + (r.Smelt is { } sm ? ":" + sm : "");
            if (got != expected) Fail($"goods « {text} »: got {got}, expected {expected}");
        }
    }
}
