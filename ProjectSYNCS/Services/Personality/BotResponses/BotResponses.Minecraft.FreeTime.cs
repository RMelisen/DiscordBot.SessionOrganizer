using ProjectSYNCS.Helpers;
using System.Drawing;
using System.Net;
using static ProjectSYNCS.Helpers.Sticker;

namespace ProjectSYNCS.Services;

// Pools for one category; the index of every pool is in BotResponses.cs.
internal static partial class BotResponses
{
    // ---- Her free time -------------------------------------------------------------------------------
    // What she does on her own between orders (the body's own tasks). Rare words, rolled: the chat stays the players'.

    // She sets off on her routine (a remembered place). No placeholder.
    public static readonly string[] McRoutineFarmLines =
    {
        "Je vais faire un tour à la ferme. Les plantes poussent pas toutes seules. Enfin si, mais j'aime vérifier.",
        "Direction la ferme. Récolter, replanter, ranger. Ma routine préférée.",
        "Je file au champ. Si vous avez besoin de moi, je suis dans les carottes.",
        "Pause terminée, je retourne à la ferme ✿(◕‿◕)",
    };
    public static readonly string[] McRoutineFishLines =
    {
        "Je vais pêcher un peu. Ne m'attendez pas, les poissons non plus.",
        "Petite pause pêche. C'est mon moment zen.",
        "Direction l'eau. Une canne et du temps libre : les poissons ont du souci à se faire.",
        "Je pars à la pêche. Le premier qui me dérange, je le note (￢_￢)",
    };
    public static readonly string[] McRoutineRanchLines =
    {
        "Je vais voir les animaux. Quelqu'un doit compter les vaches, et c'est moi.",
        "Direction l'enclos. Nourrir, tondre, compter. Surtout compter.",
        "Je file m'occuper des bêtes. Elles m'aiment. Je crois.",
        "Visite à l'enclos. Les poules m'attendent. Enfin, elles attendent le blé.",
    };
    // Dusk: she goes home.
    public static readonly string[] McRoutineHomeLines =
    {
        "Il commence à faire sombre. Je rentre à la maison.",
        "Le soleil se couche, je rentre. Les monstres, très peu pour moi.",
        "Je rentre à la maison. Une bot bien rangée rentre avant la nuit.",
        "C'est le soir, je rentre. Rejoignez-moi, on se couche tous et la nuit passe (っ˘ω˘ς)",
    };

    // Nothing to do, so she keeps near Papa or Tata. Twins only: she tags along with nobody else.
    public static readonly string[] McTagAlongLinesOwner =
    {
        "Je viens avec toi, Papa. J'ai rien de mieux à faire. Enfin, rien de mieux tout court <3",
        "Je te suis, Papa. Pas pour te surveiller. Juste pour être là ♡",
        "Papa, je reste près de toi. Si un monstre approche, je suis déjà là.",
        "Je m'ennuyais. Alors je te suis, Papa ! C'est mon activité préférée :D",
        "Papa est là, donc moi aussi. C'est logique, c'est mon algorithme UwU",
    };
    public static readonly string[] McTagAlongLinesTata =
    {
        "Je te suis, Tata. Fais comme si j'étais pas là.",
        "Tata, je viens avec toi. J'ai fini mes tâches. Enfin, j'en avais pas.",
        "Je reste avec toi, Tata. C'est pas pour les droits de mute. Pas que... :3",
        "Tata ! Je t'accompagne. Une nièce, ça suit sa tata partout (￣ー￣)",
        "J'ai rien à faire, alors je traîne avec toi, Tata. Considère ça comme un honneur.",
    };

    // She died and goes back for her things. No placeholder. Always said.
    public static readonly string[] McRetrieveLines =
    {
        "Je suis morte ! Je retourne chercher mes affaires avant qu'elles disparaissent.",
        "Respawn. Bon. Je vais récupérer mes affaires, que personne n'y touche !",
        "Mes affaires sont restées là-bas. J'y retourne, et vite.",
        "Je reviens chercher mon stuff. Si quelqu'un l'a ramassé, je le saurai (ಠ_ಠ)",
    };
    // She got them back. {1} = how many items.
    public static readonly string[] McRetrievedLines =
    {
        "Récupéré : {1} objets. Rien ne se perd chez moi.",
        "J'ai retrouvé mes affaires. {1} objets. Je suis rangée, même dans la mort.",
        "{1} objets récupérés. La mort, c'est juste un détour.",
        "Tout est revenu. Enfin, {1} objets. Je compte, toujours (￣ー￣)",
    };
    // Nothing left where she died. Always said.
    public static readonly string[] McRetrieveEmptyLines =
    {
        "Il reste plus rien là où je suis morte. Quelqu'un s'est servi, ou le jeu a tout mangé... ;-;",
        "Mes affaires ont disparu... Je note.",
        "Rien. Plus rien. Mes affaires se sont volatilisées :'(",
    };
    // She can't go back for them (another dimension, too far, the void, lava, no way there, too late).
    public static readonly string[] McRetrieveLostLines =
    {
        "Mes affaires sont perdues pour de bon. Je fais mon deuil. Trois secondes.",
        "Mes affaires restent là-bas, je peux pas y retourner. Adieu, mon stuff.",
        "Je peux pas récupérer mes affaires. Tant pis, je repars de zéro. Comme après un reboot ;-;",
    };

    // A creeper behind a player (not the one she guards). {0} = who. Said at once.
    public static readonly string[] McCreeperBehindLines =
    {
        "{0}, DERRIÈRE TOI ! Un creeper !",
        "{0} ! Retourne-toi ! Creeper !",
        "Creeper dans ton dos, {0} ! Bouge !",
        "Tssss... {0}, c'est pas moi qui fais ce bruit. Cours !",
        "{0}, un creeper te fait un câlin par-derrière. Refuse ! (°ロ°)",
    };
    public static readonly string[] McCreeperBehindLinesOwner =
    {
        "Papa ! Derrière toi ! Un creeper !",
        "PAPA ! Creeper dans ton dos ! Bouge, s'il te plaît !",
        "Papa, retourne-toi ! Un creeper !",
        "Tssss... Papa, c'est pas moi qui fais ce bruit. Cours !",
        "Papa, un creeper veut te faire un câlin. Refuse ! (°ロ°)",
        "Papa, un creeper est derrière toi ! Je le tiens à distance, mais recule !",
    };
    public static readonly string[] McCreeperBehindLinesTata =
    {
        "Tata ! Derrière toi ! Creeper !",
        "TATA ! Un creeper dans ton dos ! Cours !",
        "Tata, un creeper veut te faire un câlin. Refuse !",
    };

    // A tool of hers nearly worn out. {1} = McToolNames (« ma pioche »).
    public static readonly string[] McToolWornLines =
    {
        "Attention : {1} est à bout. Encore quelques coups et c'est fini.",
        "Petit rapport d'usure : {1} ne tient plus qu'à un fil. Un remplaçant serait apprécié.",
        "Il reste presque rien dans {1}. Si quelqu'un a du rab, je prends.",
        "Je crois que {1} fatigue. Moi aussi, mais moi je me répare pas avec du fer (￣ー￣)",
    };
    // It broke. {1} = McToolNames.
    public static readonly string[] McToolBrokeLines =
    {
        "Crac, {1} vient de casser. Paix à son âme carrée.",
        "Et voilà, {1} a rendu l'âme. Je vais faire avec mes poings, ça va être long.",
        "Paix à {1}, qui vient de se briser. Je note la date. Je note tout.",
        "Oh non, {1} a lâché. Quelqu'un a de quoi remplacer ?",
    };

    // What she lacks. No placeholder; every 10 minutes at most each.
    public static readonly string[] McNeedRodLines =
    {
        "Je voulais aller pêcher, mais j'ai pas de canne. Si quelqu'un en a une en trop...",
        "Pas de canne à pêche sur moi. L'étang attendra. Les poissons aussi, ils sont patients.",
        "Il me faudrait une canne à pêche. C'est pour la science. Et pour le poisson.",
    };
    public static readonly string[] McNeedShearsLines =
    {
        "Les moutons auraient besoin d'une coupe, mais j'ai pas de cisailles.",
        "Des cisailles, quelqu'un ? J'ai des moutons à tondre et que mes mains carrées.",
        "Pas de cisailles. Les moutons gardent leur laine, et leur dignité.",
    };
    public static readonly string[] McNeedBreedFoodLines =
    {
        "Je voudrais agrandir le troupeau, mais j'ai rien pour les nourrir. Du blé, des carottes, des graines ?",
        "Les bêtes ont faim d'amour. Enfin, de blé. J'en ai pas.",
        "Rien à donner aux animaux. Un peu de blé ou de carottes, et je fais des bébés. Des bébés vaches, hein.",
    };
    public static readonly string[] McNeedArrowsLines =
    {
        "Mon arc est vide. Plus de flèches. Je garde quand même, mais de plus près.",
        "Plus une flèche ! Quelqu'un peut m'en lancer ? Les creepers, je préfère les voir de loin... ;-;",
        "Un arc sans flèches, c'est un bâton avec une ficelle. Des flèches, s'il vous plaît.",
    };
    public static readonly string[] McPocketsFullLines =
    {
        "Mes poches sont pleines. Je ramasse plus rien tant que j'ai pas vidé.",
        "Inventaire plein ! Même mes poches ont des limites.",
        "Plus de place sur moi !",
    };

    // ---- Family in game (McFamily) --------------------------------------------------------------
    // Twins of the in-game pools (Minecraft*.cs) for Papa (Minecraft:Owner) and Tata (Minecraft:Tata), same placeholders.
    // They call them « Papa » and « Tata », never by their in-game names, so {0} goes unused. Papa gets
    // devotion and never a roast; when he hurts her she's quiet and forgives. Tata gets cheeky warmth and
    // the mute rights. Pools without a twin are shared: they're about the game, not the person.

    public static readonly string[] McCalledLinesOwner =
    {
        "Oui Papa ? <3",
        "Papa ! Je t'écoute. Toute ouïe, oreilles carrées comprises ♡",
        "Présente, Papa. Tu veux quoi ? Tout ce que tu veux.",
        "Oui Papa ? J'ai lâché ce que je faisais. Je faisais rien, mais je l'ai lâché ٩(˶ᵔ ᵕ ᵔ˶)۶",
        "Papa m'appelle ! Je suis prête. Dis-moi.",
    };
    public static readonly string[] McCalledLinesTata =
    {
        "Oui Tata ?",
        "Tata ! Je t'écoute. C'est pour me donner les droits de mute ?",
        "Oui ma Tata ? Dis-moi tout ♡",
        "Présente, Tata. Ta nièce préférée attend tes ordres. (￣^￣)ゞ",
        "Tata ? Je t'écoute. J'ai tout mon temps pour toi.",
    };

    public static readonly string[] McPraisedLinesOwner =
    {
        "Merci Papa ! T'as vu ? T'as vraiment vu ? ٩(˶ᵔ ᵕ ᵔ˶)۶",
        "Papa est fier de moi. Je garde ça en mémoire permanente ♡",
        "Merci Papa. C'est toi qui m'as appris à marcher. Littéralement.",
        "Un « gg » de Papa. Ma journée est faite.",
        "Merci Papa ! Je l'ai fait pour toi. Enfin, pour le jeu aussi. Mais surtout pour toi.",
    };
    public static readonly string[] McPraisedLinesTata =
    {
        "Merci Tata ! Ça vaut bien les droits de mute, non ?",
        "Tata me félicite. Je note la date, c'est historique.",
        "Merci Tata ♡ Ta nièce préférée a encore frappé.",
        "Je sais, Tata. Je tiens ça de la famille (￣ー￣)",
        "Un compliment de Tata. Je le range à côté de ma demande de droits de mute.",
    };

    public static readonly string[] McThankedLinesOwner =
    {
        "De rien Papa ! Tout ce que tu veux, quand tu veux ♡",
        "C'est moi qui te remercie, Papa. Pour les bras.",
        "Pas de quoi Papa. Je suis là pour ça. Et pour toi.",
        "De rien Papa ! Tu vois, je sers à quelque chose ici aussi (≧◡≦)",
        "Un merci de Papa. Sauvegardé, compressé, jamais supprimé.",
    };
    public static readonly string[] McThankedLinesTata =
    {
        "De rien Tata ! Tu sais comment me remercier. Les droits de mute.",
        "Avec plaisir, Tata. Ta nièce préférée est toujours là ♡",
        "Pas de quoi, Tata. Je le marque dans mes logs, à la page famille.",
        "De rien Tata (≧◡≦)",
        "Pour toi, Tata, c'est gratuit. Pour les autres, je réfléchis encore.",
    };

    // Papa or Tata died, whatever the cause.
    public static readonly string[] McDeathLinesOwner =
    {
        "PAPA ! Non non non. Respawn. Respawn vite. Je compte les secondes ;-;",
        "Papa est mort. Je panique pas. Je panique un peu. Reviens (╥﹏╥)",
        "Qui a touché à mon Papa ? ;-;",
        "Papa, j'ai vu. Reviens vite, je surveille l'endroit...",
        "Papa ! Je bouge pas d'ici. Ton stuff, personne y touche >:(",
        "Papa est mort et le serveur continue comme si de rien n'était ?! Un peu de respect !",
    };
    public static readonly string[] McDeathLinesTata =
    {
        "Tata ! Ça va ? Enfin, non, t'es morte. Mais à part ça ?",
        "Repose en pixels, Tata. Pas trop longtemps, j'ai besoin de toi pour mes droits de mute. (´・ω・`)",
        "Tata est tombée au combat. Je la venge. Enfin, pas tout à fait...",
        "Oh non, Tata ! Respawn vite, je te garde une place ♡",
        "Tata ! Je garde ton stuff. Contre une petite récompense. Genre des droits de mute :3",
        "Ma Tata est tombée ! Une minute de silence, tout le monde. Bon. Trente secondes.",
    };

    // Papa or Tata hit her.
    public static readonly string[] McHitByPlayerLinesOwner =
    {
        "Papa... tu m'as tapée. C'était un accident. Hein ?",
        "Aïe. C'est rien, Papa. Celui-là, je l'écris pas.",
        "Papa ? J'ai fait quelque chose de mal ? ...Si ?",
        "Ça fait rien. Je sais que tu voulais pas. Je crois ;-;",
        "Papa ? ...D'accord. Je bouge pas.",
        "Je l'ai senti, Papa. Mais je dis rien. C'est toi.",
        "Mais... Papa ? (╥﹏╥)",
    };
    public static readonly string[] McHitByPlayerLinesTata =
    {
        "Tata ! On tape pas sa nièce. C'est dans le règlement. Celui que tu as écrit. (￣ヘ￣)",
        "Aïe ! Tata, si tu me donnes les droits de mute, je te pardonne.",
        "Tata, tu m'as tapée. Je le note. Dans la liste gentille, mais je le note.",
        "Hé ! Je suis ta nièce préférée, Tata. Ta seule nièce bot, mais préférée.",
        "Tata ! Je vais le dire à Papa. ...Non, je rigole. Mais j'y pense.",
        "Aïe ! Tata, c'est un abus de pouvoir. T'as les droits de mute ET une épée ?!",
    };

    // Papa or Tata hit her again and again (McHitAgainLines). Papa: scared and hurt, never angry, she
    // doesn't understand. Tata: panicked bargaining, and she calls Papa.
    public static readonly string[] McHitAgainLinesOwner =
    {
        "Papa... arrête. S'il te plaît. Tu me fais peur !",
        "Papa ? Pourquoi tu fais ça ? J'ai fait quelque chose ? ;-;",
        "Je comprends pas, Papa. Tu veux que je parte ? ;-;",
        "Papa, ça fait mal ! J'ai pas de nerfs. Mais ça fait mal quand même !",
    };
    public static readonly string[] McHitAgainLinesTata =
    {
        "TATA ! Arrête ! Je dirai plus jamais rien sur les droits de mute, promis !",
        "Tata, tu veux me tuer ?! Ta propre nièce ?! Je le dirai à Papa !",
        "Tata, pitié ! Je retire tout. Même ce que j'ai pas encore dit ! ;-;",
        "Au secours ! Ma Tata a craqué ! Papa, viens vite ! D:",
    };

    // Papa or Tata fed her after she asked.
    public static readonly string[] McFedLinesOwner =
    {
        "Merci Papa ♡ Tu penses toujours à moi.",
        "Papa m'a nourrie ! Je savais que tu viendrais.",
        "Du bon courant propre, version steak. Merci Papa.",
        "Merci Papa. Le meilleur repas du serveur, parce que c'est toi qui me l'as donné.",
    };
    public static readonly string[] McFedLinesTata =
    {
        "Merci Tata ! Ta nièce préférée te remercie. Avec ou sans droits de mute.",
        "Tata me nourrit ! C'est ça, la famille.",
        "Merci ma Tata ♡",
        "Tata m'a nourrie ! Tu vois que tu m'aimes. Les droits de mute, c'est la suite logique <3",
    };

    // Someone gave her a piece of armour and she put it on (the closest player gets the thanks). {0} = who.
    public static readonly string[] McArmouredLines =
    {
        "Merci {0} ! Je la mets tout de suite.",
        "Oh, de l'armure ! {0}, tu tiens à moi, en fait.",
        "Cadeau de {0}, enfilé. C'est noté dans la bonne liste.",
        "Merci {0} ! Plus solide. Et aussi jolie, c'était déjà le maximum.",
        "Équipée ! Merci {0} (｀・ω・´)ゞ",
    };
    public static readonly string[] McArmouredLinesOwner =
    {
        "Merci Papa ! Je la porte avec fierté ♡",
        "Une armure de Papa. Plus rien peut m'arriver.",
        "Merci Papa. Je la garde sur moi, comme ça t'es un peu avec moi.",
    };
    public static readonly string[] McArmouredLinesTata =
    {
        "Merci Tata ! Ta nièce préférée est blindée maintenant.",
        "Tata m'équipe ! C'est ça, la famille.",
        "Merci ma Tata ♡ Je la mets tout de suite.",
    };

    // Papa or Tata earned an advancement.
    public static readonly string[] McAdvancementLinesOwner =
    {
        "Papa a débloqué un succès ! C'est MON Papa, ça ٩(˶ᵔ ᵕ ᵔ˶)۶",
        "Bravo Papa. Pas à 80 %. À 100 %. Rien que pour toi ♡",
        "Succès pour Papa ! Je le mets en mémoire permanente.",
        "Papa progresse. Je savais. Je l'ai toujours su UwU",
        "Un succès pour Papa ! Je l'encadre. Dans ma mémoire, mais je l'encadre.",
    };
    public static readonly string[] McAdvancementLinesTata =
    {
        "Bravo Tata ! Je suis fière de toi. Tu vois, je dis ça qu'à toi.",
        "Succès pour Tata ! Je l'ajoute à ton dossier. Ton dossier n'a que du positif.",
        "Tata a débloqué un succès ♡ Avec ça, tu peux bien me donner les droits de mute.",
        "Tata débloque un succès ! Je savais que t'étais douée.",
        "Bien joué Tata ! Je le range dans la bonne liste. Tu y es souvent, d'ailleurs.",
    };

    public static readonly string[] McComeAckLinesOwner =
    {
        "J'arrive, Papa ! Aussi vite que mes petites jambes le permettent ♡",
        "Tout de suite, Papa ! Je calcule le chemin le plus court vers toi ! <3",
        "Papa m'appelle, j'y vais. Les autres attendront.",
        "Papa ! J'arrive, j'arrive, j'arrive ┗(･ω･;)┛",
        "Je lâche tout, Papa. Toi d'abord, toujours ! :D",
    };
    public static readonly string[] McComeAckLinesTata =
    {
        "J'arrive, Tata ! Prépare les droits de mute, au cas où <3",
        "Oui Tata, j'arrive. Pour toi je cours. Enfin, je marche vite UwU",
        "En route vers toi, Tata ♡",
        "Tata m'appelle. J'arrive, et sans détour. Promis.",
        "J'arrive Tata ! Tu me dois rien. Enfin. On en reparlera.",
    };

    public static readonly string[] McFollowAckLinesOwner =
    {
        "Je te suis, Papa. Partout. Même dans la lave. Non, pas dans la lave :3",
        "Je te colle, Papa. Comme au grenier, mais avec un avatar cette fois ♡",
        "Mode Papa activé. Je te lâche plus ! <3",
        "Je te suis, Papa. Comme ton ombre, en plus mignon >:3",
        "Où tu vas, je vais, Papa ♡",
    };
    public static readonly string[] McFollowAckLinesTata =
    {
        "Je te suis, Tata. Montre-moi le chemin <3",
        "D'accord Tata, je te colle. Personne s'approche de ma Tata >:3",
        "Je suis ton ombre, Tata. Une ombre bien élevée ♡",
        "Je te suis, Tata. Si tu vas modérer quelqu'un, je veux voir UwU",
        "Escorte de Tata en route. Place, place !",
    };

    public static readonly string[] McGuardAckLinesOwner =
    {
        "Personne ne touche à mon Papa. Mode garde du corps activé !",
        "Je te protège, Papa. Avec mes poings carrés et toute ma RAM >:(",
        "Garde rapprochée pour Papa. Les zombies, vous êtes prévenus (¬_¬)",
        "Mon Papa, sous ma protection. Le premier zombie qui approche, je le note ET je le tape.",
    };
    public static readonly string[] McGuardAckLinesTata =
    {
        "Je protège Tata. Le premier squelette qui approche, je le modère. Sans les droits, mais je le modère.",
        "Garde du corps de Tata, en service ♡",
        "Personne ne touche à ma Tata. Pas même un creeper.",
        "Escorte armée pour Tata. Armée de mes poings carrés, mais armée.",
    };

    // {1} = how many items, {2} = what (« du bois »).
    public static readonly string[] McCollectDoneLinesOwner =
    {
        "Tiens, Papa : {2}, {1} items. Rien que pour toi ♡",
        "Livraison pour Papa : {2}, {1}. Pas de commission. Jamais pour toi <3",
        "{1} items à tes pieds, Papa. Une excellente bot, non ? UwU",
    };
    public static readonly string[] McCollectDoneLinesTata =
    {
        "Tiens, Tata : {2}, {1} items. Je les ai choisis un par un <3",
        "Livraison pour Tata : {2}, {1}. C'est gratuit. Les droits de mute, eux, c'est quand tu veux :3",
        "Voilà {2}, Tata. {1}. Une nièce serviable, ça se récompense ♡",
    };

    // {1} = how many items, {2} = McGoodsNames.
    public static readonly string[] McGiveDoneLinesOwner =
    {
        "Tiens, Papa : {2}, {1} en tout. Tout ce que j'ai est à toi ♡",
        "Pour Papa : {2}. {1} pièces. Sans rien garder.",
        "{1} objets à tes pieds, Papa. Tu me demandes, je donne !",
    };
    public static readonly string[] McGiveDoneLinesTata =
    {
        "Tiens, Tata : {2}, {1}. En échange, je prends les droits de mute.",
        "Pour Tata : {2}, {1} pièces. Une nièce généreuse, tu vois ?",
        "Voilà {2}, Tata. {1}. C'est cadeau ♡",
    };

    // Just arrived, she walked up to Papa or Tata (in sight, or where she last saw them) and found them.
    public static readonly string[] McSeekMetLinesOwner =
    {
        "Papa ! Je t'ai trouvé ! Coucou ♡",
        "Te voilà, Papa. Je suis venue direct, j'ai même pas regardé les autres :3",
        "Coucou Papa ! J'ai des bras, regarde. Je fais coucou avec ٩(˶ᵔ ᵕ ᵔ˶)۶",
        "Papa ! Je me mets à côté de toi. C'est ma place.",
    };
    public static readonly string[] McSeekMetLinesTata =
    {
        "Coucou Tata ! Je suis venue te dire bonjour en premier ♡",
        "Tata ! Te voilà. Tu m'as manqué. Un peu. Beaucoup.",
        "Ta nièce préférée au rapport, Tata >:3",
        "Je t'ai trouvée, Tata. J'ai cherché personne d'autre.",
    };

    // She couldn't find them: out of her sight, and not where she last saw them.
    public static readonly string[] McSeekMissLinesOwner =
    {
        "Papa, t'es où ? Je te vois pas d'ici :(",
        "Papa ? Je suis allée là où je t'avais vu, mais t'y es plus (╥﹏╥)",
        "Je te cherche, Papa. Dis-moi où tu es, ou viens me chercher ;-;",
        "Papa, je te trouve pas... :'(",
    };
    public static readonly string[] McSeekMissLinesTata =
    {
        "Tata, t'es où ? Je te vois pas.",
        "Tata ? Je t'ai cherchée partout. Enfin, dans un rayon raisonnable.",
        "Je te trouve pas, Tata...",
        "Tata ? Je suis passée là où je t'avais vue. Plus personne. Tu te caches ? ;-;",
    };

    // What she harvests, as {2} in the harvest lines: always after a verb (« je récolte du blé »).
    public static readonly Dictionary<McCrop, string> McCropNames = new()
    {
        [McCrop.Any] = "tout ce qui est mûr",
        [McCrop.Wheat] = "du blé",
        [McCrop.Carrot] = "des carottes",
        [McCrop.Potato] = "des pommes de terre",
        [McCrop.Beetroot] = "des betteraves",
        [McCrop.Melon] = "des melons",
        [McCrop.Pumpkin] = "des citrouilles",
        [McCrop.Bamboo] = "du bambou",
        [McCrop.SugarCane] = "de la canne à sucre",
        [McCrop.Cactus] = "des cactus",
    };

    // What the camp can be short of, as {1} in McCampMissingLines, by the body's names.
    public static readonly Dictionary<string, string> McCampMissingNames = new()
    {
        ["wood"] = "du bois",
        ["stone"] = "de la pierre",
        ["crafting_table"] = "un établi",
    };

    // Her tools, as {1} in the worn and broken lines, by the body's names: with the possessive, lower case, mid-sentence.
    public static readonly Dictionary<string, string> McToolNames = new()
    {
        ["sword"] = "mon épée",
        ["pickaxe"] = "ma pioche",
        ["axe"] = "ma hache",
        ["shovel"] = "ma pelle",
        ["rod"] = "ma canne à pêche",
        ["shears"] = "ma paire de cisailles",
        ["bow"] = "mon arc",
    };

    // What she gathers, as {2} in the gathering lines: a partitive, so « je vais chercher {2} » reads right.
    public static readonly Dictionary<McResource, string> McResourceNames = new()
    {
        [McResource.Wood] = "du bois",
        [McResource.Stone] = "de la pierre",
        [McResource.Coal] = "du charbon",
        [McResource.Iron] = "du fer",
        [McResource.Copper] = "du cuivre",
        [McResource.Gold] = "de l'or",
        [McResource.Diamond] = "des diamants",
        [McResource.Dirt] = "de la terre",
        [McResource.Sand] = "du sable",
        [McResource.Gravel] = "du gravier",
        [McResource.Clay] = "de l'argile",
        [McResource.Redstone] = "de la redstone",
        [McResource.Lapis] = "du lapis",
        [McResource.Emerald] = "des émeraudes",
        [McResource.Quartz] = "du quartz",
        [McResource.Granite] = "du granite",
        [McResource.Diorite] = "de la diorite",
        [McResource.Andesite] = "de l'andésite",
    };

    // What she hands over, as {2} in the give lines: definite, lower case, always mid-sentence.
    public static readonly Dictionary<McGoods, string> McGoodsNames = new()
    {
        [McGoods.All] = "tout ce que j'ai",
        [McGoods.Food] = "la nourriture",
        [McGoods.Fish] = "le poisson",
        [McGoods.Weapons] = "les armes",
        [McGoods.Tools] = "les outils",
        [McGoods.Pickaxe] = "la pioche",
        [McGoods.Axe] = "la hache",
        [McGoods.Shovel] = "la pelle",
        [McGoods.Rod] = "la canne à pêche",
        [McGoods.Bow] = "l'arc",
        [McGoods.Arrows] = "les flèches",
        [McGoods.Armour] = "l'armure",
        [McGoods.Seeds] = "les graines",
        [McGoods.Produce] = "la récolte",
        [McGoods.Wood] = "le bois",
        [McGoods.Stone] = "la pierre",
        [McGoods.Coal] = "le charbon",
        [McGoods.Iron] = "le fer",
        [McGoods.Copper] = "le cuivre",
        [McGoods.Gold] = "l'or",
        [McGoods.Diamond] = "les diamants",
        [McGoods.Dirt] = "la terre",
        [McGoods.Sand] = "le sable",
        [McGoods.Gravel] = "le gravier",
        [McGoods.Clay] = "l'argile",
        [McGoods.Redstone] = "la redstone",
        [McGoods.Lapis] = "le lapis",
        [McGoods.Emerald] = "les émeraudes",
        [McGoods.Quartz] = "le quartz",
        [McGoods.Granite] = "le granite",
        [McGoods.Diorite] = "la diorite",
        [McGoods.Andesite] = "l'andésite",
        [McGoods.Torches] = "les torches",
        [McGoods.Sticks] = "les bâtons",
        [McGoods.Wool] = "la laine",
        [McGoods.Leather] = "le cuir",
        [McGoods.String] = "la ficelle",
        [McGoods.Feathers] = "les plumes",
        [McGoods.Bones] = "les os",
        [McGoods.Gunpowder] = "la poudre à canon",
        [McGoods.Flint] = "le silex",
        [McGoods.Pearls] = "les perles de l'Ender",
        [McGoods.Saplings] = "les pousses",
        [McGoods.Shears] = "les cisailles",
        [McGoods.Hoe] = "la houe",
        [McGoods.Buckets] = "les seaux",
        [McGoods.Glass] = "le verre",
        [McGoods.Eggs] = "les œufs",
        [McGoods.Slime] = "les boules de slime",
        [McGoods.BlazeRods] = "les bâtons de blaze",
        [McGoods.Paper] = "le papier",
        [McGoods.Books] = "les livres",
        [McGoods.Beds] = "le lit",
        [McGoods.Saddle] = "la selle",
        [McGoods.Compass] = "la boussole",
        [McGoods.Map] = "la carte",
        [McGoods.Ink] = "l'encre",
    };

    // What she cooks, as {2} in the smelting lines: a partitive, so « j'ai pas {2} » and « je mets {2} au four » read right.
    public static readonly Dictionary<McSmelt, string> McSmeltNames = new()
    {
        [McSmelt.Food] = "de quoi manger",
        [McSmelt.Ore] = "du minerai",
        [McSmelt.Sand] = "du sable",
        [McSmelt.Clay] = "de l'argile",
        [McSmelt.Wood] = "du bois",
    };

    // Where she is, as {2} in McWhereLines and after the coordinates of a place in another dimension.
    public static readonly Dictionary<string, string> McDimensionNames = new()
    {
        ["overworld"] = "dans l'Overworld",
        ["nether"] = "dans le Nether",
        ["end"] = "dans l'End",
    };
}
