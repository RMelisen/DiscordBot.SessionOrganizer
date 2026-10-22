namespace ProjectSYNCS.Helpers;

/// <summary>One way to say an order in game, and the order McCommand reads it as (None: talk, McTalkReader).</summary>
public sealed record McHelpExample(string Text, McOrder Order);

/// <summary>A group of orders in /minecraft help: what she does, then a few ways to ask for it.</summary>
public sealed record McHelpSection(string Title, string Blurb, McHelpExample[] Examples);

/// <summary>
/// The manual /minecraft help shows (MinecraftModule.BuildHelp). Every example is parsed by the checks and must read as
/// the order it illustrates, so the manual can't drift from McCommand. Phrasings as players type them in game chat.
/// </summary>
public static class McHelp
{
    public static readonly McHelpSection[] Sections =
    [
        new("Se déplacer",
            "Mets son nom au début ou à la fin : « syncs … » ou « … syncs ». Elle vient, te suit, s'arrête ou va où tu dis.",
            [
                new("syncs viens", McOrder.Come), new("syncs suis-moi", McOrder.Follow), new("syncs bouge pas", McOrder.Stay),
                new("syncs assieds-toi", McOrder.Sit), new("syncs va en 120 64 -300", McOrder.GoTo), new("syncs t'es où ?", McOrder.Where),
                new("syncs saute", McOrder.Jump),
            ]),
        new("Récolter, cultiver, pêcher",
            "Elle ramasse ce que tu demandes et te le rapporte, ou s'installe à la ferme, aux enclos ou au bord de l'eau jusqu'à nouvel ordre.",
            [
                new("syncs va chercher 20 bois", McOrder.Collect), new("syncs ramasse les pommes de terre", McOrder.Harvest),
                new("syncs occupe-toi de la ferme", McOrder.Farm), new("syncs occupe-toi des animaux", McOrder.Ranch),
                new("syncs va pêcher", McOrder.Fish), new("syncs fais fondre le fer", McOrder.Smelt),
            ]),
        new("Les lieux",
            "Elle retient les endroits que tu lui montres (les tiens, et ceux des autres par leur pseudo), y va, t'y emmène ou les surveille.",
            [
                new("syncs retiens ici : maison", McOrder.SavePlace), new("syncs va à la mine", McOrder.GoToPlace),
                new("syncs rentre", McOrder.GoToPlace), new("syncs emmène-moi à la maison", McOrder.Guide),
                new("syncs où est la mine ?", McOrder.WherePlace), new("syncs tes lieux", McOrder.ListPlaces),
                new("syncs oublie la mine", McOrder.ForgetPlace), new("syncs protège la maison", McOrder.Patrol),
            ]),
        new("Objets et coffres",
            "Elle te donne ce qu'elle porte, range tout dans le coffre d'à côté, ou te dit ce qu'elle a sur elle.",
            [
                new("syncs donne-moi du poisson", McOrder.Give), new("syncs donne moi à manger", McOrder.Give),
                new("syncs range tes affaires", McOrder.Store), new("syncs t'as quoi ?", McOrder.Inventory),
                new("syncs t'as combien de fer ?", McOrder.Have),
            ]),
        new("Le camp",
            "Pour Papa et Tata : elle construit une petite cabane (lit, coffre, établi, four, feu de camp), en fabriquant tout et en allant chercher ce qui manque, puis la démonte et garde tout.",
            [
                new("syncs monte le camp", McOrder.Camp), new("syncs démonte le camp", McOrder.Uncamp),
            ]),
        new("Combat et sommeil",
            "Elle te suit et tape les monstres autour de toi, ou va se coucher dans le lit le plus proche.",
            [
                new("syncs protège-moi", McOrder.Guard), new("syncs couvre-moi", McOrder.Guard), new("syncs va au lit", McOrder.Sleep),
            ]),
        new("Minuteur",
            "Elle te prévient au bout du temps demandé (2 heures au plus), avec ton message si tu en donnes un.",
            [
                new("syncs préviens-moi dans 5 min", McOrder.Timer),
                new("syncs rappelle-moi d'arrêter le four dans 1h", McOrder.Timer),
            ]),
        new("Lui parler",
            "Elle répond comme sur Discord. Juste après sa réponse, ton message suivant lui est adressé même sans son nom. Un chuchotement (/msg) est toujours pour elle.",
            [
                new("salut syncs", McOrder.Wave), new("bonne nuit syncs", McOrder.Bye), new("gg syncs", McOrder.Praise),
                new("merci syncs", McOrder.Thanks), new("syncs ça va ?", McOrder.None), new("syncs raconte une blague", McOrder.None),
            ]),
    ];
}
