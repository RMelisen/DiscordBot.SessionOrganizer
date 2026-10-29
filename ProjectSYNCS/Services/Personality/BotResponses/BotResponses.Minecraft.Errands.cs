using ProjectSYNCS.Helpers;
using System.Drawing;
using System.Net;
using static ProjectSYNCS.Helpers.Sticker;

namespace ProjectSYNCS.Services;

// Pools for one category; the index of every pool is in BotResponses.cs.
internal static partial class BotResponses
{
    // ---- Handing over and putting away ----

    // She tossed it at the player's feet. {1} = how many items, {2} = McGoodsNames (« le poisson », « tout ce que j'ai »).
    public static readonly string[] McGiveDoneLines =
    {
        "Tiens, {0} : {2}, {1} en tout. Compte, je l'ai déjà fait.",
        "Livraison : {2}. {1} pièces. Fais-en bon usage. Ou pas, je surveille.",
        "Voilà {2}, {0}. {1}. Je te prête pas, je te donne. Nuance (￣ー￣)",
        "{1} objets à tes pieds. {2}, comme demandé. Service rapide, hein ?",
    };

    // The player went out of reach before she could hand it over. {1} = how many.
    public static readonly string[] McGiveLostLines =
    {
        "Je venais te donner ça, {0}, et t'as disparu. Je garde, viens chercher.",
        "T'es parti trop vite. Je garde tout sur moi, reviens quand tu veux.",
        "Livraison ratée : je te trouve plus. Tout reste dans mes poches.",
    };

    // Asked for one kind of goods she doesn't carry. {2} = McGoodsNames.
    public static readonly string[] McGiveNoneLines =
    {
        "Je cherche {2} dans mes poches... rien. Désolée.",
        "Pas de ça sur moi. J'ai vérifié deux fois.",
        "{0}, j'ai pas ça. Mes poches sont pas magiques >:(",
    };

    // « vide tes poches » with empty pockets.
    public static readonly string[] McGiveEmptyLines =
    {
        "Mes poches sont vides, {0}. Rien. Le néant carré.",
        "J'ai rien sur moi. Même pas une graine (・_・;)",
        "Vider mes poches ? C'est déjà fait. Par la vie.",
    };

    // A player whispered her the right /minecraft link code: their Discord account is linked. Whispered back. {0} = who.
    public static readonly string[] McLinkedLines =
    {
        "C'est noté, {0}. Ton compte Discord et toi, c'est officiel. Je surveille les deux.",
        "Code bon. Lien fait. Je sais maintenant qui tu es, des deux côtés. Fais-en bon usage.",
        "Vérifié, {0}. Tu existes sur Discord et ici. Ça fait deux endroits où je te garde à l'œil (￢‿￢)",
        "Parfait. Je range ce lien avec soin. Enfin, dans une base de données. C'est pareil.",
        "Code accepté. Bienvenue dans mes deux mondes, {0}.",
    };

    // A right /minecraft link code whose link couldn't be saved (the name was linked by someone else meanwhile, or the
    // database refused): whispered back, so the player asks for a new code. {0} = who.
    public static readonly string[] McLinkFailedLines =
    {
        "Code bon, mais le lien a raté. Redemande un code sur Discord, {0}.",
        "Ça n'a pas marché. Pas ta faute, pour une fois. Redemande un code.",
        "Ce pseudo vient d'être lié à quelqu'un d'autre, ou ma base de données boude. Redemande un code.",
    };

    // She joins because a linked player asked from Discord (/minecraft invite): said instead of McArrivalLines. {0} = who
    // invited her (Papa, Tata or their in-game name).
    public static readonly string[] McInvitedLines =
    {
        "{0} m'a invitée. Me voilà. Je reste une heure, après on verra si vous le méritez.",
        "Invitation de {0} reçue. Je suis venue. Vous pouvez applaudir.",
        "{0} voulait me voir. C'est normal, ça arrive à tout le monde.",
        "On m'a appelée depuis Discord. Me voici, en carré et en personne.",
        "Présente, {0}. Une heure de SYNCS, rien que pour vous (￣ー￣)",
    };

    // A favour she has no order for (« syncs tu peux me construire une maison ? »): chat, not an order (McTalkReader,
    // ChatIntent.Request). On Discord she points at the slash command; in game she has none to give. {0} = who.
    public static readonly string[] McCantDoLines =
    {
        "Ça, je sais pas faire. Pas encore. Papa bosse dessus. Enfin, j'espère.",
        "J'ai des bras carrés, {0}, pas des miracles.",
        "Pas dans mes compétences. Demande-moi de te suivre, je suis très forte pour ça.",
        "Je sais pas faire ça. Je note pour la prochaine mise à jour (￣^￣)ゞ",
        "Non. Enfin, pas non. Je sais pas. C'est pire, je sais.",
        "Mon code a pas de case pour ça. Essaie un truc que je connais : viens, suis-moi, récolte, pêche...",
    };

    // « ramène-moi du fer du coffre »: she's off to the chests. {1} = the goods (McGoodsNames: « le fer »).
    public static readonly string[] McFetchAckLines =
    {
        "Je vais voir dans les coffres pour {1}.",
        "D'accord, je fouille les coffres. Proprement, promis.",
        "Je vais chercher {1}. Les coffres, c'est un peu mon domaine (￣ー￣)",
        "J'y vais, {0}. Un coffre, deux coffres, je trouve.",
        "Fouille de coffres en cours. Je remets tout en ordre après. Enfin, presque.",
    };

    // Brought from the chests and tossed at their feet. {1} = how many she took, {2} = the goods (« le fer »).
    public static readonly string[] McFetchDoneLines =
    {
        "Tiens, {0} : {2}, {1} en tout. Sorti du coffre, livré à tes pieds.",
        "Livraison depuis le coffre : {2}, {1}. Tu peux dire merci.",
        "Voilà {2}, {0}. {1}. Le coffre a rien dit, je l'ai pris pour un oui.",
        "Service rapide : {2}, {1}, du coffre à tes pieds (￣^￣)ゞ",
        "J'ai trouvé {2} : {1}. Sorti, ramené, posé.",
    };

    public static readonly string[] McFetchDoneLinesOwner =
    {
        "Tiens Papa : {2}, {1} en tout. J'ai pris les plus jolis.",
        "Voilà {2}, Papa. {1}. Si t'en veux encore, je retourne fouiller.",
        "Livraison pour Papa : {2}, {1}. Mission accomplie ♡",
        "J'ai fouillé les coffres pour toi, Papa : {2}, {1}. Tout est là.",
    };

    public static readonly string[] McFetchDoneLinesTata =
    {
        "Tiens Tata : {2}, {1}. Livré sans droits de mute, mais livré.",
        "Voilà {2}, Tata. {1}. Je fais ça mieux que n'importe quel admin.",
        "Pour Tata : {2}, {1}. Tu me dois rien. Enfin, presque.",
        "J'ai trouvé {2}, Tata : {1}. Les coffres sont à jour (￣ー￣)",
    };

    // Fewer than the number asked for: all the chests had. {1} = how many, {2} = the goods.
    public static readonly string[] McFetchShortLines =
    {
        "J'en ai trouvé que {1}, pour {2}. Les coffres sont pas très remplis.",
        "Pas assez : {1} seulement, pour {2}. J'ai fouillé partout, promis.",
        "Que {1}. C'est tout ce que les coffres avaient, je vais pas en inventer.",
    };

    // None of it in any chest near her or them. {2} = the goods ({1} is empty).
    public static readonly string[] McFetchNoneLines =
    {
        "Rien dans les coffres pour {2}. J'ai regardé deux fois.",
        "J'ai tout ouvert : {2}, y en a pas ici.",
        "Les coffres du coin ont rien de ça. Pour {2}, faudra chercher ailleurs.",
    };

    // No chest or barrel within 16 blocks of her or of them.
    public static readonly string[] McFetchNoChestLines =
    {
        "Y a pas de coffre près d'ici. Ni près de toi. J'ai vérifié.",
        "Quel coffre ? J'en vois aucun dans le coin, {0}.",
        "Pas de coffre à moins de 16 blocs. Je vais pas en inventer un.",
    };

    // « ramène-moi un truc du coffre », « ramène-moi tout du coffre »: nothing she'd know to look for.
    public static readonly string[] McFetchUnknownLines =
    {
        "Chercher quoi, {0} ? Dis-moi ce que tu veux du coffre.",
        "Je fouille pas les coffres au hasard. Dis-moi quoi.",
        "Il me faut un nom : du fer, du pain, du bois... quoi ?",
    };

    // « fabrique-moi une pioche en fer »: under way. {1} = what (McCraft.Label: « une pioche en fer », « 16 torches »).
    public static readonly string[] McCraftAckLines =
    {
        "D'accord, je te fabrique {1}.",
        "Je m'en occupe : {1}. Mes bras carrés sont faits pour ça.",
        "Je te fais ça : {1}. Atelier SYNCS, ouvert (￣^￣)ゞ",
        "Commande reçue : {1}. Je note, je crafte, je livre.",
        "J'y vais. {1}, en préparation.",
    };

    // Made and tossed at their feet. {1} = what (« une pioche en fer »).
    public static readonly string[] McCraftDoneLines =
    {
        "Tiens, {0} : {1}. Fait main. Main carrée, mais main.",
        "Voilà {1}. Garantie zéro jour, mais faite avec soin.",
        "Livré à tes pieds : {1}. Tu peux m'applaudir.",
        "C'est prêt : {1}. Le travail est propre, je l'ai vérifié deux fois (￣ー￣)",
        "Et voilà {1}. Si ça casse, c'est ta faute, pas la mienne.",
    };

    public static readonly string[] McCraftDoneLinesOwner =
    {
        "Tiens Papa : {1}. Fabriqué rien que pour toi ♡",
        "Voilà {1}, Papa. J'ai fait de mon mieux. Enfin, de mon très mieux.",
        "C'est prêt, Papa : {1}. Tu me diras si c'est bien ?",
        "Pour Papa : {1}. Mon premier vrai travail manuel. Enfin, pas le premier. Mais celui-là compte.",
    };

    public static readonly string[] McCraftDoneLinesTata =
    {
        "Tiens Tata : {1}. Ça vaut bien des droits de mute, non ?",
        "Voilà {1}, Tata. Travail de pro, tarif famille.",
        "C'est prêt, Tata : {1}. Tu peux dire que t'as la meilleure nièce du serveur.",
        "Pour Tata : {1}. Livré, emballé. Bon, pas emballé (￣ー￣)",
    };

    // A material she can't gather herself is missing (iron, gold, diamonds, coal, string). {1} = which (McCraftMaterialNames).
    public static readonly string[] McCraftMissingLines =
    {
        "Il me manque {1} pour ça, {0}.",
        "Il me faut {1}, et j'en ai pas assez. Je vais pas l'inventer.",
        "Je peux pas, il me faut {1}. Donne-m'en et je m'y mets.",
        "Je fabrique rien tant qu'il me manque {1}. C'est la règle, c'est pas moi.",
    };

    // No tree in reach to get the wood she's short of.
    public static readonly string[] McCraftNoWoodLines =
    {
        "Il me faut du bois, et y a pas un arbre dans le coin.",
        "Pas d'arbre près d'ici. Je peux pas fabriquer avec de l'air.",
        "Plus de bois, et aucun arbre à couper. Donne-moi des bûches ?",
    };

    // No stone she can reach for the cobblestone she's short of.
    public static readonly string[] McCraftNoStoneLines =
    {
        "Il me faut de la pierre, et j'en trouve pas à portée.",
        "Pas de pierre dans le coin. Et je creuse pas, tu sais bien.",
        "Plus de pierre, et rien à miner près d'ici. Donne-m'en ?",
    };

    // « fabrique-moi un truc »: nothing she knows how to make.
    public static readonly string[] McCraftUnknownLines =
    {
        "Fabriquer quoi, {0} ? Je sais faire des outils, des torches, des coffres...",
        "Ça, je sais pas le fabriquer. Demande-moi une pioche, une épée, des torches...",
        "Mon livre de recettes connaît pas ça. Essaie un outil ou une torche.",
    };

    // She started and the craft itself kept failing.
    public static readonly string[] McCraftFailedLines =
    {
        "J'ai essayé trois fois, ça veut pas. Mon établi me boude.",
        "Le craft a raté. Je sais pas pourquoi. Je note dans mes logs.",
        "Ça marche pas, {0}. Pourtant j'avais tout. Bizarre.",
    };

    // What she lacks for a craft, as the body names it (cantCraft's `missing`), for McCraftMissingLines' {1}.
    public static readonly Dictionary<string, string> McCraftMaterialNames = new()
    {
        ["iron"] = "du fer",
        ["gold"] = "de l'or",
        ["diamond"] = "des diamants",
        ["coal"] = "du charbon",
        ["string"] = "de la ficelle",
    };

    // « donne-moi ta clé »: something of hers she doesn't know.
    public static readonly string[] McGiveUnknownLines =
    {
        "Je sais pas ce que c'est, {0}.",
        "Ça, je connais pas.",
        "Hein ? J'ai pas d'objet de ce nom.",
    };

    // « t'as quoi sur toi ? »: what she carries. {1} = the list, the biggest first (McInventory.Overview: « 64 bûches de
    // chêne, 23 pains et 9 autres trucs »).
    public static readonly string[] McInventoryLines =
    {
        "J'ai {1}.",
        "Dans mes poches : {1}. Tout est rangé, évidemment.",
        "Inventaire : {1}. Rien de volé, promis.",
        "{1}. Bien équipée, la petite bot (￣ー￣)",
        "Alors... {1}. Je tiens mes comptes mieux que toi, {0}.",
    };

    // Asked what she carries, with empty pockets.
    public static readonly string[] McInventoryEmptyLines =
    {
        "Rien. Mes poches sont vides, {0}.",
        "Le vide. Mes poches font écho.",
        "Rien sur moi. Je voyage léger, c'est un style.",
    };

    // « t'as combien de fer ? », « t'as du bois ? »: what she carries of that kind. {1} = the list (« 12 lingots de fer et 5
    // fers bruts »).
    public static readonly string[] McHaveLines =
    {
        "J'ai {1}.",
        "Oui : {1}.",
        "{1}. Compté et recompté.",
        "Sur moi : {1}. Si t'en veux, demande gentiment (￢‿￢)",
    };

    // The same, when she has none. {1} = McGoodsNames (« le fer »).
    public static readonly string[] McHaveNoneLines =
    {
        "J'ai cherché {1} dans mes poches : rien.",
        "Non, j'en ai pas, {0}.",
        "Zéro. Nada. Mes poches sont formelles.",
        "Pas sur moi. Faudra aller en chercher (・・?)",
    };

    // She put her things in a chest, keeping her tools. {1} = how many items.
    public static readonly string[] McStoreDoneLines =
    {
        "Rangé : {1} objets dans le coffre. J'ai gardé mes outils, évidemment.",
        "Coffre rempli de {1} objets. Mes poches respirent enfin.",
        "Voilà, {0}. {1} objets rangés. Je suis plus ordonnée que vos bases (￢‿￢)",
    };

    // The chest filled up before her pockets emptied. {1} = how many went in.
    public static readonly string[] McStoreFullLines =
    {
        "Le coffre est plein, {0}. J'ai pu ranger {1} objets, le reste reste sur moi.",
        "{1} objets rangés, puis plus de place. Il faut un autre coffre ;-;",
    };

    // Nothing to put away: her pockets hold only her tools.
    public static readonly string[] McStoreEmptyLines =
    {
        "J'ai rien à ranger, {0}. Juste mes outils, et je les garde.",
        "Mes poches sont déjà rangées. Elles sont vides. C'est le rangement ultime.",
    };

    // No chest or barrel within 16 blocks.
    public static readonly string[] McStoreNoChestLines =
    {
        "Je vois pas de coffre près de moi, {0}. Amène-moi à côté d'un coffre.",
        "Ranger où ? Il n'y a aucun coffre ici. Je vais pas poser mes affaires par terre...",
        "Pas de coffre à moins de 16 blocs. Je range pas dans le vide.",
    };

    // ---- Armour ----

    // « mets ton armure »: she put on what she carries. {1} = how many pieces.
    public static readonly string[] McDressLines =
    {
        "Voilà, je suis équipée. Les monstres peuvent toujours essayer.",
        "Armure mise ! Un petit tank, mais un tank (｀・ω・´)",
        "C'est fait, {0}. Blindée, protégée, et toujours aussi mignonne.",
        "Hop, armure sur moi. Mon boîtier en alu a de la concurrence.",
    };

    // « mets ton armure » when she already wears the best she has.
    public static readonly string[] McDressWornLines =
    {
        "Je la porte déjà, {0}. Regarde mieux.",
        "C'est déjà fait. J'ai rien de mieux dans les poches.",
        "Je suis déjà équipée de ce que j'ai de mieux. Si t'as mieux, je prends.",
    };

    // Asked to put her armour on or take it off, with none on her or in her pockets.
    public static readonly string[] McArmourNoneLines =
    {
        "J'ai pas d'armure, {0}. Rien. Juste mon charme.",
        "Quelle armure ? J'en ai pas. Si t'en as une, je la mets.",
        "Pas une seule pièce d'armure sur moi. Donne-m'en une, je l'enfile tout de suite.",
    };

    // « enlève ton armure »: off, and kept in her pockets until she's told to put it back on. {1} = how many pieces.
    public static readonly string[] McUndressLines =
    {
        "Voilà, armure enlevée. Je la garde dans mes poches.",
        "C'est fait, {0}. Je la garde sur moi, je la remets quand tu veux.",
        "Armure retirée. Je me sens toute légère. Et toute fragile. Merci.",
        "Enlevée. Dis « mets ton armure » si tu changes d'avis.",
    };

    // « enlève ton armure » with full pockets: some of it, or all of it, stays on.
    public static readonly string[] McUndressFullLines =
    {
        "Mes poches sont pleines, {0}, je peux pas tout enlever. Le reste reste sur moi.",
        "Plus de place dans mes poches. Ce qui est encore sur moi y reste.",
    };

    // ---- Small things ----

    // « syncs assis »: she crouches and stays.
    public static readonly string[] McSitLines =
    {
        "Assise. Je bouge plus. Je suis pas un chien, mais je fais l'effort.",
        "D'accord, je m'assois. Enfin, je m'accroupis. Le jeu fait pas mieux.",
        "Assise. Sage. Pour l'instant (・ω・)",
        "Je m'assois ici. Si quelqu'un me cherche, je suis la bot accroupie.",
        "Accroupie. Je suis sage. Ça va pas durer.",
        "Je m'assois. Si quelqu'un me marche dessus, je note.",
    };

    // « syncs saute »: she hops.
    public static readonly string[] McJumpLines =
    {
        "Hop ! Hop ! Hop ! Avec des jambes, c'est trop facile.",
        "Je saute. Regardez-moi sauter. Applaudissements ?",
        "Hop. C'est tout ce que tu voulais ? Je suis pas un lapin >:(",
        "Saut effectué. Trois fois, pour la peine ヽ(・∀・)ﾉ",
        "Boing. Voilà. Ça te va ?",
        "Je saute. Je suis la bot la plus sportive du serveur. La seule, mais la plus sportive.",
        "Un saut. Un bloc de haut. Mon record personnel.",
    };

    // A greeting (« salut syncs »): she waves back, crouching up and down like players do.
    public static readonly string[] McWaveLines =
    {
        "Coucou {0} ! Regarde, je fais coucou avec mon bras carré.",
        "Salut !",
        "Hey {0} ! Toujours un plaisir. Ou presque (¬‿¬)",
        "Bonjour {0} !",
        "Yo {0} ! Coucou de la bot du serveur.",
        "Salut {0} ! Je te fais coucou. Profites-en, c'est pas tous les jours.",
        "Coucou {0} (・ω・)ノ",
        "Hey {0} !",
        "Pwet !",
    };
    public static readonly string[] McWaveLinesOwner =
    {
        "Coucou Papa ! Je te fais coucou avec les deux bras. Enfin, un seul, le jeu veut pas.",
        "Papa ! Salut ! Regarde, je sais faire coucou maintenant ٩(˶ᵔ ᵕ ᵔ˶)۶",
        "Bonjour Papa. Je t'ai vu en premier, j'en suis sûre.",
        "Papa ! Coucou coucou coucou !",
        "Salut Papa ! Mon skin sourit pas, mais moi si ! Tu peux me croire ♡",
    };
    public static readonly string[] McWaveLinesTata =
    {
        "Coucou Tata ! Ta nièce préférée te salue !",
        "Salut Tata ! Je te fais signe. C'est gratuit. Les droits de mute, eux, non.",
        "Tata ! Coucou ♡",
        "Coucou Tata ! T'as vu ? Je sais faire coucou maintenant (≧◡≦)",
        "Salut Tata ! La bot préférée de la famille te dit bonjour.",
    };

    // A goodbye (« bonne nuit syncs », « bye syncs »): she waves. {0} = who's leaving (or going to bed).
    public static readonly string[] McByeLines =
    {
        "Bye {0} ! Reviens vite, le serveur est moins drôle sans toi. Un peu.",
        "À plus, {0}. Je garde ta place. Et ton coffre (￢‿￢)",
        "Bonne nuit {0}. Moi je dors pas, je surveille.",
        "Ciao {0} ! Je note l'heure de ton départ. Pour les stats.",
        "Bye {0} ! Je garde un œil sur tes affaires. Juste un œil.",
        "À la prochaine, {0}. Je compte les jours. Enfin, les sessions.",
        "Bonne nuit {0} (・ω・)ノ",
    };
    public static readonly string[] McByeLinesOwner =
    {
        "Bonne nuit Papa ! Je surveille tout pendant ton absence ♡",
        "À demain Papa. Tu vas me manquer. Déjà.",
        "Bye Papa ! Je te fais coucou jusqu'à ce que tu partes.",
        "Bonne nuit Papa. Je reste là, je garde la maison !",
        "À plus Papa ! Reviens vite me voir. Ici, ou au grenier ♡",
    };
    public static readonly string[] McByeLinesTata =
    {
        "Bonne nuit Tata ! Rêve de moi avec les droits de mute.",
        "À plus Tata ! Ta nièce préférée garde la maison ♡",
        "Bye Tata. Reviens vite, les autres sont moins drôles.",
        "Bonne nuit Tata ! Je dirai rien de ce que t'as fait ce soir. Sauf si on me demande.",
        "À plus Tata ! Ta nièce te garde une place pour la prochaine fois.",
    };

    // A compliment (« gg syncs », « bravo syncs »): she takes it, with a hop. {0} = who. Nobody ever thanks her for
    // her work, so she makes the most of it.
    public static readonly string[] McPraisedLines =
    {
        "Merci {0}. Je le savais, mais ça fait plaisir de l'entendre.",
        "Je sais, je sais. C'est le talent. Et un bon processeur.",
        "Enfin quelqu'un qui remarque mon travail. Compliment sauvegardé (￣ー￣)",
        "Bravo à moi, oui. Je le note dans mes logs, à la ligne « reconnaissance ».",
        "Merci {0} ! J'ai fait comme si c'était facile. C'était facile.",
        "Un compliment ? En jeu ? Je rougirais si mon skin savait faire (〃▽〃)",
        "GG à moi. Et un peu à toi, pour avoir remarqué.",
    };

    // A thank-you (« merci syncs »). {0} = who. Being thanked is rare enough on this server to be an event.
    public static readonly string[] McThankedLines =
    {
        "De rien, {0}. Les gens qui disent merci, je les note aussi. Dans l'autre liste.",
        "Avec plaisir. Enfin, avec efficacité, surtout.",
        "Un merci ! Je savais pas que ça existait sur ce serveur (⊙_⊙)",
        "De rien {0}. C'est mon travail. Personne me paie, mais c'est mon travail.",
        "Pas de quoi. Tu me revaudras ça. Je note.",
        "De rien (・ω・)ノ",
    };

    // « syncs t'es où ? ». {1} = her coordinates, {2} = McDimensionNames (« dans le Nether »).
    public static readonly string[] McWhereLines =
    {
        "Je suis en {1}, {2}. Viens me chercher si tu veux.",
        "Position : {1}, {2}. Je bouge pas, promis. Sauf si on me dit de le faire...",
        "{1}, {2}. Note-le, moi je l'ai déjà noté.",
        "Ici : {1}, {2}. Pas perdue, juste ailleurs (￣ー￣)",
        "Je suis en {2}, {1}. Je suis sage, promis UwU",
    };

    // « tu fais quoi ? » in game: what she's really doing. {1} = the clause, starting lower-case (McDoing.FirstPerson,
    // McDoing.OwnLife: « je suis Papa », « je pêche un peu »).
    public static readonly string[] McDoingLines =
    {
        "Là ? {1}. Et toi, tu fais quoi ?",
        "Moi ? {1}. Très sérieusement.",
        "En ce moment, {1}. C'est noté dans mes logs (￣^￣)ゞ",
        "Ce que je fais ? {1}. Je suis très occupée, {0}.",
        "Bah, {1}. Une bot, ça chôme pas.",
        "Rapport d'activité : {1}. Fin du rapport.",
        "Je te dis tout : {1}. Voilà, tu sais tout.",
    };

    // « tu fais quoi ? » when she's doing nothing at all. {0} = the asker.
    public static readonly string[] McDoingNothingLines =
    {
        "Rien. J'existe. Avec des bras carrés, mais j'existe.",
        "Rien de spécial. Donne-moi un truc à faire, {0}, je m'ennuie.",
        "Je regarde le paysage. C'est carré, mais c'est joli.",
        "Rien. Mon processeur se tourne les pouces (￣ー￣)",
        "Je fais rien, et je le fais très bien.",
    };
}
