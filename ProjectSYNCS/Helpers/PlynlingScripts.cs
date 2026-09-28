using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

// Whether a conversation is friendly or tense: rivals and conflicts are tense, every other bond friendly.
public enum ConvoFlavor { Friendly, Tense }

// One line of a script: who says it (A raised the subject, B is the other) and what.
public sealed record ConvoLine(bool ByA, string Text);

/// <summary>
/// A conversation written as one piece: four lines that each answer the one before. Line 0 is A's
/// opener — narration, then A's words, separated by one \n; lines 1–3 are one spoken line each, from
/// whichever of them the script says, so one may speak twice in a row. The last line leads into doing
/// something. {S} is A, {L} is B; {s:m|f} agrees with A, {l:m|f} with B; {P} is the subject (custom
/// scripts only, and never after « de » or « à »). A line may start with a face tag — see
/// <see cref="PlynlingVisitStory.Untag"/>.
/// </summary>
public sealed record Script(ConvoLine[] Lines);

/// <summary>
/// What two Plynlings say to each other about a passion, steps 2–5 of a visit. Catalog passions have
/// scripts written for them, by flavour and by whether B shares the passion; a typed passion only gets
/// the generic ones, which say nothing about the subject beyond <c>{P}</c>. Generated from the
/// writing sheets — to add a conversation, add a script to the right key.
/// </summary>
public static class PlynlingScripts
{
    private static ConvoLine A(string text) => new(true, text);
    private static ConvoLine B(string text) => new(false, text);
    private static Script S(params ConvoLine[] lines) => new(lines);

    public static Script[] For(PlynlingPassion passion, ConvoFlavor flavor, bool shared) => Catalog[(passion, flavor, shared)];

    public static Script[] ForCustom(ConvoFlavor flavor, bool shared) => Custom[(flavor, shared)];

    /// <summary>Every script, for the checks.</summary>
    public static IEnumerable<Script> All => Catalog.Values.Concat(Custom.Values).SelectMany(x => x);

    /// <summary>Every key, with its scripts, for the checks.</summary>
    public static IEnumerable<(string Key, Script[] Scripts)> Keys =>
        Catalog.Select(kv => ($"{kv.Key.Item1}/{kv.Key.Item2}/{kv.Key.Item3}", kv.Value))
            .Concat(Custom.Select(kv => ($"custom/{kv.Key.Item1}/{kv.Key.Item2}", kv.Value)));

    private static readonly IReadOnlyDictionary<(PlynlingPassion, ConvoFlavor, bool), Script[]> Catalog =
        new Dictionary<(PlynlingPassion, ConvoFlavor, bool), Script[]>
    {
        [(PlynlingPassion.Cooking, ConvoFlavor.Friendly, false)] = new[]
        {
            S(A("{S} sort un petit carnet taché de farine.\nTu savais qu'une crêpe se retourne mieux d'un coup de poignet qu'à la spatule ?"),
              B("Je croyais que tout était une question de chance."),
              A("Pas du tout ! Il faut sentir le moment où la crêpe se décolle toute seule."),
              B("Alors apprends-moi à le sentir, ce moment.")),
            S(A("{S} renifle l'air avec beaucoup de sérieux.\nÇa sent la noisette grillée quelque part. J'ai un don pour ça."),
              B("Tu arrives vraiment à deviner ce que c'est rien qu'à l'odeur ?"),
              A("Presque toujours ! Là, c'est de la noisette, un peu de beurre, et quelqu'un qui a laissé brûler quelque chose."),
              B("Ça, je veux bien te croire. Je suis {l:prêt|prête} à goûter, si tu cuisines.")),
            S(A("{S} déplie une recette si longue qu'elle traîne par terre.\nC'est la recette du gâteau parfait. Il faut trois jours et beaucoup de courage."),
              B("Trois jours ? Pour un seul gâteau ?"),
              B("Remarque… si c'est vraiment le gâteau parfait, ça vaut peut-être le coup."),
              A("Ça vaut le coup. Et le troisième jour, on a le droit de goûter. Tu veux m'aider ?")),
        },
        [(PlynlingPassion.Cooking, ConvoFlavor.Friendly, true)] = new[]
        {
            S(A("{S} montre un petit carnet plein de ratures.\nJ'ai enfin trouvé le secret de la pâte à crêpes : il faut la laisser reposer."),
              B("Non ! Moi aussi, j'ai trouvé ça hier ! Une heure de repos, pas moins."),
              A("Une heure ! Moi, je n'ai jamais osé attendre plus de vingt minutes."),
              B("Alors on essaie ensemble : ta pâte et la mienne, et on compare.")),
            S(A("{S} a de la farine sur le bout du nez.\nJ'ai raté trois gâteaux ce matin. Le quatrième est presque un gâteau."),
              B("Presque ? Le mien d'hier a survécu, mais on aurait dit un caillou."),
              A("Le caillou, c'est le signe d'un four trop chaud. Le mien, c'est plutôt une éponge."),
              B("Éponge contre caillou : il faut qu'on les goûte ensemble, pour choisir le vainqueur.")),
        },
        [(PlynlingPassion.Cooking, ConvoFlavor.Tense, false)] = new[]
        {
            S(A("{S} agite une cuillère en bois comme un sceptre.\nJe fais les meilleures crêpes du coin. Ce n'est pas de l'orgueil, c'est un fait."),
              B("Les meilleures ? Tu n'as jamais goûté les miennes."),
              A("Je n'en ai pas besoin. Je sens les mauvaises crêpes à dix pas."),
              B("Alors je vais t'en faire une, et tu me diras ça la bouche pleine.")),
            S(A("{S} soupire devant une casserole.\nMa soupe est parfaite. Il lui faut juste un peu de sel et une meilleure ambiance."),
              B("Un peu de sel, ce n'est pas ça qui te manque : c'est un peu de modestie."),
              A("Merci, chef. Tu veux goûter, ou tu préfères critiquer de loin ?"),
              B("Je goûte. Et je critique de près.")),
        },
        [(PlynlingPassion.Cooking, ConvoFlavor.Tense, true)] = new[]
        {
            S(A("{S} bombe le torse devant un plat fumant.\nLa vraie recette, c'est la mienne. Elle vient de ma famille."),
              B("Ha ! La mienne aussi vient de ma famille. Et elle est meilleure."),
              A("Meilleure ? Tu mets du sucre là où il faut du sel."),
              B("Et toi, tu mets de l'orgueil là où il faut du beurre. On règle ça tout de suite, sur le feu.")),
            S(A("{S} claque une cuillère sur la table.\nSi on veut un gâteau digne de ce nom, il faut le faire cuire lentement."),
              B("Lentement ? On ne fait pas attendre un gâteau : on lui donne du feu."),
              A("Du feu ! Tu veux un gâteau ou un feu de camp ?"),
              B("Les deux. Donne-moi ton four, on verra qui a raison.")),
            S(A("{S} goûte une sauce et fait la grimace.\nTa sauce manque de sel. La mienne est parfaite."),
              B("Ma sauce est parfaite. C'est ta langue qui est fatiguée."),
              A("Ma langue va très bien, merci."),
              A("Tiens, goûte la mienne, et ose me dire qu'elle n'est pas meilleure.")),
        },
        [(PlynlingPassion.Music, ConvoFlavor.Friendly, false)] = new[]
        {
            S(A("{S} tapote un rythme sur une pierre, l'air très concentré.\nÉcoute ça : si on tape deux fois plus vite, ça devient une autre chanson !"),
              B("Deux fois plus vite ? Ça ressemble surtout à quelqu'un qui a très faim."),
              A("Ha ! Mais c'est ça, la musique : on entend ce qu'on ressent."),
              B("Alors joue-moi ce que tu ressens, là, tout de suite.")),
            S(A("{S} souffle dans un brin d'herbe. Ça fait un bruit affreux.\nC'est une trompette. Une trompette très jeune. Elle apprend."),
              B("Elle a du chemin à faire, ta trompette."),
              A("Tout le monde a commencé par un bruit affreux. Même les oiseaux."),
              B("D'accord. Je t'écoute jusqu'au bout, mais tu me promets une note juste.")),
            S(A("{S} bat la mesure sur ses genoux.\nJ'ai composé une chanson pour la pluie. Elle ne l'a pas encore entendue."),
              B("Une chanson pour la pluie ? Et comment tu vas la lui faire écouter ?"),
              B("Attends, je sais : il faut la chanter dehors, quand il pleut."),
              A("Exactement ! À la prochaine averse, on sort et on chante. Tu viens avec moi ?")),
        },
        [(PlynlingPassion.Music, ConvoFlavor.Friendly, true)] = new[]
        {
            S(A("{S} fredonne une mélodie en boucle, l'air rêveur.\nJe l'ai entendue en rêve. Je l'ai notée en me réveillant. Enfin, le début."),
              B("Fredonne encore, s'il te plaît. Il me semble que je la connais…"),
              A("Ça alors ! Tu l'avais rêvée, toi aussi ?"),
              B("Je la fredonne depuis ce matin sans savoir d'où elle vient. On la finit ensemble ?")),
            S(A("{S} claque des doigts en rythme, puis rate le rythme.\nC'est un rythme très moderne. Il faut une oreille exercée."),
              B("Ou une oreille qui a déjà raté des rythmes, comme la mienne."),
              A("Toi aussi, tu rates ? Personne n'ose l'avouer, d'habitude."),
              B("Je rate avec beaucoup de conviction. Viens, on rate ensemble : ce sera plus joli.")),
        },
        [(PlynlingPassion.Music, ConvoFlavor.Tense, false)] = new[]
        {
            S(A("{S} pose une flûte de roseau sur ses genoux, très {s:fier|fière}.\nJ'ai composé une mélodie que personne ne pourra jamais jouer aussi bien que moi."),
              B("Personne ? Tu as l'air bien sûr de toi, pour quelqu'un qui n'a jamais été écouté."),
              A("Parce que je sais ce que c'est, la musique. Toi, tu écoutes du bruit."),
              B("Le bruit, c'est ce que tu fais. Joue, qu'on juge par nous-mêmes.")),
            S(A("{S} souffle une note, une seule, très fort.\nVoilà. Ça, c'est un « la » parfait."),
              B("C'était un « sol », et il était enrhumé."),
              A("Tu n'y connais rien. Tu dis ça pour me contrarier."),
              B("Peut-être. Mais joue-la encore, qu'on vérifie.")),
        },
        [(PlynlingPassion.Music, ConvoFlavor.Tense, true)] = new[]
        {
            S(A("{S} lève un doigt, très docte.\nLa seule vraie mélodie, c'est celle qui monte trois fois avant de redescendre."),
              B("Trois fois ? Elle monte deux fois et elle tombe. C'est ça qui est beau."),
              A("Tu massacres les mélodies, comme toujours."),
              B("Et toi, tu leur fais la leçon. Chante la tienne, je chanterai la mienne, et on verra.")),
            S(A("{S} tape du pied, furieusement en rythme.\nMon tempo est le bon. Tu es toujours trop {l:rapide|rapide}."),
              B("Trop rapide ? C'est toi qui traînes depuis le début."),
              A("Je ne traîne pas, je prends mon temps !"),
              B("Tu prends celui des autres. Compte avec moi, et on tranche.")),
            S(A("{S} fredonne très fort, en regardant {L} de travers.\nC'est comme ça qu'on chante ce refrain. Pas comme toi."),
              B("Tu chantes faux depuis la première note."),
              A("Faux ? C'est une interprétation."),
              A("Chante-le à ta façon, et on demande aux oiseaux lequel des deux ils préfèrent.")),
        },
        [(PlynlingPassion.Gaming, ConvoFlavor.Friendly, false)] = new[]
        {
            S(A("{S} mime des boutons invisibles avec les pouces.\nJ'ai enfin battu le boss du troisième niveau. Sans perdre une seule vie. Enfin, presque."),
              B("Presque, ça veut dire combien de vies ?"),
              A("Sept. Mais la dernière était vraiment très courageuse."),
              B("Alors tu es un héros ! Explique-moi comment il fonctionne, ce boss.")),
            S(A("{S} arrive en faisant un petit bruit de pièce à chaque pas.\nPling. Pling. J'ai ramassé tout ce qui brillait sur le chemin."),
              B("Tu joues à ton jeu même en marchant ?"),
              A("Toujours ! Regarde, là, une pièce cachée. Pling !"),
              B("Elle est vraiment là ? Montre-moi où tu cherches, que j'essaie aussi.")),
            S(A("{S} tend une feuille couverte de flèches et de croix.\nC'est la solution du labyrinthe. Je l'ai trouvée. Il m'a fallu toute la semaine."),
              B("Toute la semaine ? Pour un labyrinthe ?"),
              B("Oh… il y a des dragons dessinés dans les coins. C'est un sacré labyrinthe."),
              A("Les dragons, ce sont les pièges. Viens, je te montre le chemin, case par case.")),
        },
        [(PlynlingPassion.Gaming, ConvoFlavor.Friendly, true)] = new[]
        {
            S(A("{S} parle très vite, très bas.\nSi tu sautes pile au bon moment sur le troisième champignon, tu gagnes une vie. Ne le dis à personne."),
              B("Le troisième champignon, celui juste avant le tuyau ?"),
              A("Tu connais l'astuce ?! Je croyais être {s:le seul|la seule} à la savoir !"),
              B("Je m'en sers depuis des semaines. On devrait comparer toutes nos astuces secrètes.")),
            S(A("{S} soupire comme après une longue bataille.\nJ'ai perdu contre le boss final. Il avait triché."),
              B("Le boss du volcan ? Il a triché avec moi aussi, à chaque fois !"),
              A("Quelle injustice ! Il paraît qu'il lance une boule de feu impossible à éviter."),
              B("Impossible pour un seul joueur. À deux, on peut lui échapper. On essaie ?")),
        },
        [(PlynlingPassion.Gaming, ConvoFlavor.Tense, false)] = new[]
        {
            S(A("{S} croise les bras devant une manette imaginaire.\nJe suis le meilleur du coin, tout le monde le sait."),
              B("Le meilleur ? Tu n'as même jamais fini le dernier niveau."),
              A("Parce que le dernier niveau est truqué, voilà tout."),
              B("Excuse classique. Prouve-le : refais-le devant moi.")),
            S(A("{S} secoue la tête, l'air très déçu.\nCe jeu ne vaut rien. Les développeurs n'y connaissent rien."),
              B("C'est surtout que tu perds tout le temps."),
              A("Je ne perds pas ! Je ne gagne juste pas encore."),
              B("Alors continue de ne pas gagner. Je regarde. Et je compte tes vies.")),
        },
        [(PlynlingPassion.Gaming, ConvoFlavor.Tense, true)] = new[]
        {
            S(A("{S} appuie frénétiquement sur un bouton invisible.\nPersonne ne bat mon record au troisième niveau. Personne."),
              B("Trois minutes vingt ? Je l'ai battu hier, avec une seule main."),
              A("Une main ? Tu mens. Prouve-le."),
              B("Donne-moi la manette et regarde. Ou plutôt, prends des notes.")),
            S(A("{S} lève un poing victorieux.\nJ'ai trouvé le trésor caché du niveau quatre !"),
              B("Celui derrière la cascade ? Je l'ai trouvé il y a trois jours."),
              A("Trois jours ? Alors pourquoi il était encore là ?"),
              B("Parce que je l'ai laissé, pour te laisser croire que tu étais {s:fort|forte}. Mais si tu préfères, on refait la course.")),
            S(A("{S} fait craquer ses doigts.\nJe tiens le record du niveau secret."),
              B("Il n'y a pas de niveau secret. Tu l'as inventé."),
              A("Il y en a un. Je l'ai fini deux fois."),
              A("Viens, je te montre. Et après, tu t'excuses.")),
        },
        [(PlynlingPassion.Astronomy, ConvoFlavor.Friendly, false)] = new[]
        {
            S(A("{S} lève les yeux vers le ciel, même en plein jour.\nTu savais qu'il y a des étoiles qu'on voit alors qu'elles n'existent plus ?"),
              B("Comment on peut voir quelque chose qui n'existe plus ?"),
              A("Leur lumière voyage si longtemps qu'elle arrive après qu'elles se sont éteintes."),
              B("C'est presque triste. Mais joli aussi. Raconte-moi d'autres choses sur le ciel.")),
            S(A("{S} tient un petit dessin de constellation.\nJ'en ai inventé une nouvelle. Elle s'appelle « le Caillou qui pense ». Elle a trois étoiles."),
              B("Trois étoiles seulement ? Ça fait un petit caillou."),
              A("Un caillou qui pense n'a pas besoin d'être gros."),
              B("Tu me la montreras cette nuit ? J'aimerais bien la voir aussi.")),
            S(A("{S} sort une carte du ciel pliée en douze.\nJe l'ai dessinée moi-même. Il y a quelques étoiles en trop. Je les trouve jolies."),
              B("En trop ? Comment une étoile peut être en trop ?"),
              B("Ah, je vois : celle-là, en bas, c'est une tache de confiture."),
              A("C'est la plus belle de toutes ! Ce soir, on cherche une vraie étoile pour la remplacer.")),
        },
        [(PlynlingPassion.Astronomy, ConvoFlavor.Friendly, true)] = new[]
        {
            S(A("{S} pointe un point précis du ciel.\nLà. C'est là que j'aimerais aller. Pas pour toujours. Juste pour voir."),
              B("Vers la grande étoile bleue ? J'y pense chaque nuit."),
              A("Toi aussi ? Je n'en ai jamais parlé à personne."),
              B("Alors on peut en parler ensemble. Viens, on la cherche encore.")),
            S(A("{S} compte à voix basse, les yeux fermés.\nJe mémorise les étoiles pour pouvoir les voir même le jour."),
              B("Moi aussi ! Je m'arrête toujours à cent douze, et après je me perds."),
              A("Cent douze ! Moi, je perds le compte à quatre-vingt-dix."),
              B("Alors on compte ensemble : à deux, on ira plus loin.")),
        },
        [(PlynlingPassion.Astronomy, ConvoFlavor.Tense, false)] = new[]
        {
            S(A("{S} désigne le ciel d'un air supérieur.\nSi tu regardais le ciel plus souvent, tu saurais que ta petite vie ne compte pas."),
              B("Merci pour le réconfort. Ma petite vie et moi sommes très touchés."),
              A("Je dis seulement la vérité : nous sommes minuscules."),
              B("Minuscules, mais bien contents de l'être. Viens voir ce qu'on fait sur terre, en attendant.")),
            S(A("{S} tient un petit télescope en carton, très {s:fier|fière}.\nAvec ça, je vois des planètes que tu ne verras jamais."),
              B("Avec un carton ? Donne-moi ça, je te dirai ce que je vois, moi."),
              A("Ne touche pas ! Tu vas le tordre."),
              B("Je regarde et je te le rends. Ça ne coûte rien de m'essayer.")),
        },
        [(PlynlingPassion.Astronomy, ConvoFlavor.Tense, true)] = new[]
        {
            S(A("{S} tape sur une carte du ciel dessinée à la main.\nLa constellation du Goûter, c'est moi qui l'ai découverte."),
              B("Découverte ? Je l'ai dessinée le mois dernier. Tu l'as copiée."),
              A("Je l'ai vue avant toi !"),
              B("Tu l'as vue sur mon dessin. On vérifie au ciel, ce soir.")),
            S(A("{S} lève un doigt vers la lune.\nElle est plus belle en croissant."),
              B("En croissant ? Elle est plus belle pleine. Tout le monde le sait."),
              A("Tout le monde, sauf ceux qui aiment la nuit comme moi."),
              B("J'aime la nuit autant que toi, et je dis pleine. Regarde ce soir, on verra qui a raison.")),
            S(A("{S} tend un doigt vers le ciel.\nL'étoile la plus brillante, c'est celle-là. Je l'ai repérée la première."),
              B("C'est une planète, pas une étoile."),
              A("Une planète ?"),
              A("…Bon. Alors c'est moi qui ai repéré la planète la plus brillante. Ça compte double.")),
        },
        [(PlynlingPassion.Gardening, ConvoFlavor.Friendly, false)] = new[]
        {
            S(A("{S} montre fièrement une toute petite pousse dans un pot.\nElle est sortie ce matin. Je lui ai déjà trouvé un nom."),
              B("Un nom ? Comment tu l'appelles ?"),
              A("Monique. Elle a l'air d'une Monique, non ?"),
              B("Elle a surtout l'air fragile. Dis-moi comment on s'en occupe.")),
            S(A("{S} arrive avec une pousse plantée dans une coquille de noix.\nElle voulait voir du pays. Je l'emmène partout avec moi aujourd'hui."),
              B("Elle n'a pas froid, dans sa coquille ?"),
              A("Elle est bien au chaud. Et je l'arrose avec de la rosée fraîche."),
              B("Tu m'apprendras comment tu fais pousser des choses aussi petites ?")),
            S(A("{S} fixe le sol avec une grande patience.\nJe regarde pousser mes radis. Il paraît qu'ils sont timides quand on les regarde."),
              B("Et ça marche, de les regarder ?"),
              B("Oh, attends… il y a une petite feuille qui dépasse, là !"),
              A("Chut ! Tu vas lui faire peur. Viens t'asseoir tout doucement à côté de moi.")),
        },
        [(PlynlingPassion.Gardening, ConvoFlavor.Friendly, true)] = new[]
        {
            S(A("{S} a de la terre sous les ongles et le sourire jusqu'aux oreilles.\nMes tomates ont rougi ! Je crois qu'elles m'ont {s:entendu|entendue} leur faire des compliments."),
              B("Tu leur parles aussi ? Moi, je chante pour mes fraises tous les soirs."),
              A("Et elles poussent bien ?"),
              B("Elles poussent de travers, mais elles sont très contentes. On devrait échanger nos secrets.")),
            S(A("{S} tient une graine entre deux doigts.\nDedans, il y a un arbre entier. Il faut juste attendre cinquante ans."),
              B("Cinquante ans ? Moi, je plante toujours des choses qui poussent avant la fin du mois."),
              A("Ça, c'est pour les impatients. Les arbres, c'est pour les patients."),
              B("Alors on plante tes arbres et mes fraises, et on regarde les deux pousser.")),
        },
        [(PlynlingPassion.Gardening, ConvoFlavor.Tense, false)] = new[]
        {
            S(A("{S} arrache une mauvaise herbe d'un air satisfait.\nTu vois ? Ça, c'est du jardinage. Toi, tu laisses tout pousser n'importe comment."),
              B("Je laisse la nature faire. Elle a du talent, contrairement à certains."),
              A("La nature a besoin qu'on la guide."),
              B("La nature a surtout besoin qu'on lui lâche la main. Regarde mes fleurs, plutôt.")),
            S(A("{S} soupire devant une plate-bande.\nUne limace a mangé ma plus belle salade. Elle va me le payer."),
              B("Tu as essayé de lui demander poliment de partir ?"),
              A("Une limace ne comprend pas la politesse."),
              B("Alors tu as peut-être une chance de gagner autrement que par la colère.")),
        },
        [(PlynlingPassion.Gardening, ConvoFlavor.Tense, true)] = new[]
        {
            S(A("{S} lève un arrosoir comme une épée.\nLes roses, il faut les arroser le matin. Jamais le soir."),
              B("Le soir ! Toutes les plantes préfèrent le soir, elles aiment le calme."),
              A("Tu tues tes rosiers avec ces idées-là."),
              B("Mes rosiers se portent mieux que les tiens. Compare, si tu veux.")),
            S(A("{S} montre une carotte tordue avec fierté.\nRegarde ça. La plus belle du potager."),
              B("Elle a l'air d'avoir fait une mauvaise chute."),
              A("C'est le style ! Toi, tu fais pousser des choses trop droites."),
              B("Trop droites ? Elles sont belles, mes carottes. Viens, je te fais goûter la différence.")),
            S(A("{S} montre deux tomates côte à côte.\nLa mienne est plus rouge que la tienne."),
              B("La tienne est surtout plus petite."),
              A("Petite, mais savoureuse. Le goût, ça ne se mesure pas."),
              A("Goûte, et dis-moi en face que la tienne est meilleure.")),
        },
        [(PlynlingPassion.Rocks, ConvoFlavor.Friendly, false)] = new[]
        {
            S(A("{S} vide ses poches : une douzaine de cailloux roulent par terre.\nCelui-là, il est presque rond. Presque. C'est ce qui le rend spécial."),
              B("Presque rond, c'est comme ça qu'on reconnaît un caillou qui a du caractère ?"),
              A("Exactement ! Un caillou trop parfait n'a rien à raconter."),
              B("Alors raconte-moi l'histoire de celui-là. J'ai envie de l'entendre.")),
            S(A("{S} tend un caillou percé d'un petit trou.\nSi tu regardes à travers, le monde a l'air plus rond. Essaie."),
              B("Oh ! Le ciel a l'air d'un œil, à travers."),
              A("Et les arbres ont l'air de danser. C'est le trou, il a un pouvoir."),
              B("Je peux le garder un peu ? Je veux regarder partout.")),
            S(A("{S} présente un caillou avec cérémonie.\nVoici Bernard. Il a quatre cents ans. Peut-être quatre mille. Il ne dit pas son âge."),
              B("Bernard ? Tu donnes des prénoms à tes cailloux ?"),
              B("Remarque, il a une tête de Bernard. C'est troublant."),
              A("N'est-ce pas ? Viens, je te présente le reste de la famille.")),
        },
        [(PlynlingPassion.Rocks, ConvoFlavor.Friendly, true)] = new[]
        {
            S(A("{S} tend un caillou tout lisse, encore chaud de sa poche.\nTiens-le un peu. Il est très apaisant. C'est son métier."),
              B("Il est comme celui qui est dans ma poche ! Tiens, compare."),
              A("Presque pareil ! On dirait des frères."),
              B("On devrait les laisser se rencontrer. Je crois qu'ils s'entendront bien.")),
            S(A("{S} range des cailloux en ligne, du plus petit au plus grand.\nNe les dérange pas. Ils sont en train de faire la queue."),
              B("Ils font la queue pour quoi ?"),
              A("Pour être admirés. C'est un grand honneur, pour un caillou."),
              B("Moi, je fais pareil ! Regarde ma ligne : ils ont chacun un prénom.")),
        },
        [(PlynlingPassion.Rocks, ConvoFlavor.Tense, false)] = new[]
        {
            S(A("{S} soupèse un caillou d'un air de connaisseur.\nCelui-ci est le meilleur du coin. Aucun autre caillou n'a ce genre de poids."),
              B("Il est lourd, oui. Mais lourd, ça ne veut pas dire beau."),
              A("Tu n'y connais rien aux cailloux."),
              B("J'en connais assez pour savoir que le tien est un caillou comme un autre.")),
            S(A("{S} refuse de lâcher un caillou.\nNon. Celui-là ne quitte pas ma poche."),
              B("Je n'ai rien demandé. Je regardais, c'est tout."),
              A("Ta façon de regarder m'inquiète."),
              B("Alors garde-le. Mais ne me reproche pas de le trouver moche.")),
        },
        [(PlynlingPassion.Rocks, ConvoFlavor.Tense, true)] = new[]
        {
            S(A("{S} lève un caillou triomphalement.\nRegarde ! Le plus beau spécimen de la région."),
              B("Il est correct. Le mien est bien plus beau."),
              A("Montre-le, alors."),
              B("Le voici. Regarde-le bien, et dis-moi que le tien est plus beau. Tu n'oseras pas.")),
            S(A("{S} aligne ses cailloux avec une précision militaire.\nTrente-deux cailloux, tous mieux que les tiens."),
              B("Trente-deux ? J'en ai quarante, et les miens sont plus lisses."),
              A("Lisses, ça ne veut rien dire !"),
              B("Ça veut dire plus agréables à tenir. Compare, si tu es {s:courageux|courageuse}.")),
            S(A("{S} brandit un galet parfaitement lisse.\nCelui-là, c'est le plus lisse de la rivière."),
              B("J'en ai un plus lisse à la maison."),
              A("À la maison ? Comme c'est pratique."),
              A("Va le chercher. J'attends ici, avec le mien.")),
        },
        [(PlynlingPassion.Stories, ConvoFlavor.Friendly, false)] = new[]
        {
            S(A("{S} serre un vieux livre tout contre {s:lui|elle}.\nJ'en suis au moment où le dragon avoue qu'il a peur du noir. Je ne m'en remets pas."),
              B("Un dragon qui a peur du noir ? Mais il crache du feu !"),
              A("Justement, il se sert de son feu pour s'éclairer. C'est tout le drame."),
              B("Ça donne envie de le lire. Tu me le prêtes quand tu as fini ?")),
            S(A("{S} lit en marchant et manque de trébucher.\nPardon ! J'étais en pleine bataille navale. Les pirates étaient sur le point de gagner."),
              B("Des pirates ? Ils sont drôles ou méchants ?"),
              A("Les deux ! Le capitaine est très méchant, mais il pleure quand il perd son perroquet."),
              B("Tu me raconteras la suite ? Je suis {l:curieux|curieuse}, moi aussi.")),
            S(A("{S} sort un carnet plein de noms de personnages.\nJ'ai inventé trente personnages. Il me manque juste l'histoire."),
              B("Trente personnages et pas d'histoire ?"),
              B("Tiens, celui-là, le hérisson qui a peur des pommes : il pourrait être le héros."),
              A("Le hérisson ! Tu as raison. Aide-moi à lui inventer une aventure.")),
        },
        [(PlynlingPassion.Stories, ConvoFlavor.Friendly, true)] = new[]
        {
            S(A("{S} a les yeux encore un peu rouges.\nLe petit renard du livre a enfin retrouvé sa maison. Je ne pleure pas. Je suis juste {s:ému|émue}."),
              B("Le petit renard ! J'ai pleuré à ce passage aussi, il y a trois jours."),
              A("Vraiment ? Tu l'avoues, toi ?"),
              B("Sans honte. On devrait le relire ensemble : ça fera moins mal.")),
            S(A("{S} tourne une page imaginaire.\nJ'ai commencé une histoire hier soir. Elle n'a pas de fin. Je crois qu'elle attend que je l'invente."),
              B("J'en ai aussi une qui traîne dans mon carnet, sans fin non plus !"),
              A("On pourrait peut-être se les échanger, pour voir si l'autre trouve la fin."),
              B("Bonne idée. La mienne, tu la finis ; la tienne, je la finis. Marché conclu ?")),
        },
        [(PlynlingPassion.Stories, ConvoFlavor.Tense, false)] = new[]
        {
            S(A("{S} tient un livre à bout de bras, comme un trophée.\nJ'ai lu trois cents pages en une nuit. Tu ne pourrais pas en faire autant."),
              B("Trois cents pages ? Tu les as vraiment lues, ou tu as juste tourné les pages ?"),
              A("Je les ai lues, et je me souviens de tout."),
              B("Alors dis-moi comment finit le chapitre douze, et je te crois.")),
            S(A("{S} récite une phrase avec emphase.\n« Et le vent emporta tous les secrets. » C'est la dernière phrase de mon livre préféré."),
              B("Ce n'est pas très original."),
              A("Tu ne comprends rien à la poésie."),
              B("Peut-être. Mais je connais des fins meilleures.")),
        },
        [(PlynlingPassion.Stories, ConvoFlavor.Tense, true)] = new[]
        {
            S(A("{S} serre un livre contre sa poitrine.\nLe meilleur roman de tous les temps, c'est celui-ci. Je n'accepte pas de discussion."),
              B("Le meilleur ? Il est bien trop long, le héros n'arrête pas de pleurer."),
              A("Il a de bonnes raisons !"),
              B("Ce sont surtout des raisons de trop. Le mien est bien plus rythmé.")),
            S(A("{S} claque un livre.\nTu n'as rien compris à la fin."),
              B("J'ai compris qu'elle était ratée."),
              A("Ratée ? Elle est bouleversante !"),
              B("Elle est bouleversante pour ceux qui veulent pleurer. Moi, je préfère les fins qui surprennent.")),
            S(A("{S} ouvre un livre à la dernière page.\nLa fin est la meilleure partie. C'est un fait."),
              B("La fin, c'est pour ceux qui ne savent pas savourer le milieu."),
              A("Le milieu, c'est pour ceux qui ont peur de la fin."),
              A("Lis-moi ton passage préféré, je te lis le mien. On verra lequel est le plus beau.")),
        },
        [(PlynlingPassion.Dance, ConvoFlavor.Friendly, false)] = new[]
        {
            S(A("{S} fait trois pas de côté, un tour, et s'arrête net.\nJ'ai inventé un pas. Il n'a pas encore de nom. Ni de fin."),
              B("Refais-le lentement, que je regarde."),
              A("Un, deux, trois, tour… et là, je ne sais plus."),
              B("Je crois qu'il faut finir par un saut. Essaie, pour voir.")),
            S(A("{S} arrive en faisant des petits bonds.\nJe ne marche plus. Je danse partout. C'est plus lent, mais plus joli."),
              B("Tu danses même quand tu vas au marché ?"),
              A("Surtout au marché ! Les fruits adorent ça."),
              B("Alors montre-moi comment ça se danse, un chemin.")),
            S(A("{S} tourne une fois, deux fois, et vacille.\nJ'ai le vertige. Ça veut dire que c'était une bonne pirouette."),
              B("Ou que tu vas tomber."),
              B("Attention, attention… non, tu tiens debout ! Bravo !"),
              A("Tu vois ? À ton tour, maintenant. Je te rattrape si tu tombes.")),
        },
        [(PlynlingPassion.Dance, ConvoFlavor.Friendly, true)] = new[]
        {
            S(A("{S} fait une petite révérence.\nLa danse, c'est comme marcher, mais en plus joli et avec plus de risques."),
              B("Et avec plus de chances de tomber, je sais ! Moi aussi, j'ai les genoux couverts de bleus."),
              A("Ha ! {l:Un vrai danseur|Une vraie danseuse} !"),
              B("Alors danse avec moi : on tombera ensemble, ça fera moins mal.")),
            S(A("{S} balance les bras en rythme.\nIl paraît que les arbres dansent quand il y a du vent. Moi, je n'attends pas le vent."),
              B("Moi non plus ! Je danse quand j'ai faim, quand j'ai peur et quand je suis {l:content|contente}."),
              A("Alors tu danses tout le temps ?"),
              B("Presque ! Viens, on invente un pas pour le vent et pour la faim.")),
        },
        [(PlynlingPassion.Dance, ConvoFlavor.Tense, false)] = new[]
        {
            S(A("{S} se tient très droit, le menton levé.\nMon pas de danse est le plus élégant du coin. Personne ne m'arrive à la cheville."),
              B("Élégant ? Tu ressembles à un arbre qui a peur de tomber."),
              A("C'est de la retenue ! Tu ne comprends rien à l'art."),
              B("Je comprends que tu as peur de te lâcher. Danse comme tu veux, pour une fois.")),
            S(A("{S} ferme un œil pour juger {L}.\nTu danses comme un caillou qui roule."),
              B("Un caillou, c'est déjà mieux que ton arbre."),
              A("Je ne suis pas un arbre !"),
              B("Alors bouge. Prouve-le.")),
        },
        [(PlynlingPassion.Dance, ConvoFlavor.Tense, true)] = new[]
        {
            S(A("{S} tape du pied sur le sol.\nCe pas-là se fait en trois temps, pas en quatre."),
              B("En quatre, tout le monde le sait. Les trois temps, c'est pour les paresseux."),
              A("Paresseux ? Tu danses comme quelqu'un qui compte ses pas !"),
              B("Je compte pour ne pas écraser les tiens. Montre-moi ton trois temps, je te montre mon quatre.")),
            S(A("{S} décrit un cercle en pointant un doigt.\nLe meilleur danseur du coin, c'est moi. Point final."),
              B("Meilleur ? Tu as la grâce d'une pomme de pin."),
              A("Et toi, tu danses comme un buisson dans le vent."),
              B("Un buisson, c'est déjà plus vivant qu'une pomme de pin. Viens, on règle ça sur la piste.")),
            S(A("{S} fait un grand pas de côté.\nMon pas de danse est inimitable."),
              B("Je peux le faire les yeux fermés."),
              A("Les yeux fermés ? Tu tomberais."),
              A("Allez, vas-y. Je compte jusqu'à trois, et on verra qui tombe.")),
        },
        [(PlynlingPassion.Painting, ConvoFlavor.Friendly, false)] = new[]
        {
            S(A("{S} a de la peinture bleue jusqu'aux coudes.\nJ'essaie de peindre le vent. C'est plus dur que prévu."),
              B("Le vent, ça ne se voit pas. Comment tu fais ?"),
              A("Je peins ce qu'il touche : les feuilles qui tournent, les cheveux qui s'envolent."),
              B("C'est très malin. Montre-moi ce que ça donne.")),
            S(A("{S} tient un pinceau fait d'une brindille et d'une plume.\nJ'ai fabriqué mon propre pinceau. Il peint surtout à côté, mais il a du caractère."),
              B("À côté de quoi ?"),
              A("À côté de tout. C'est ce qui donne du style."),
              B("Alors laisse-moi essayer. J'ai toujours voulu peindre un peu à côté.")),
            S(A("{S} montre un tableau où tout est violet.\nJe n'avais plus que du violet. Alors le monde est violet, cette fois."),
              B("Même le soleil ?"),
              B("En fait, un soleil violet, c'est assez joli. On dirait la fin de la journée."),
              A("C'est exactement ce que je voulais peindre ! Enfin, maintenant, oui. Tu en veux un aussi ?")),
        },
        [(PlynlingPassion.Painting, ConvoFlavor.Friendly, true)] = new[]
        {
            S(A("{S} montre une toile entièrement blanche.\nC'est un paysage sous la neige. J'ai mis trois jours. Il faut savoir s'arrêter."),
              B("Trois jours pour du blanc ? Moi, j'ai mis une semaine pour un ciel gris."),
              A("Une semaine ! Tu es plus {l:courageux|courageuse} que moi."),
              B("Non, juste plus {l:têtu|têtue}. Peignons ensemble ton blanc et mon gris : ça fera de la brume.")),
            S(A("{S} a peint un petit soleil sur le dos de sa main.\nComme ça, il fait beau même quand il pleut."),
              B("Ha ! Moi, j'ai peint des étoiles sur mon front, pour la nuit."),
              A("On est de vrais artistes ambulants !"),
              B("On devrait ouvrir une galerie sur nous-mêmes. Il y a de la place partout.")),
        },
        [(PlynlingPassion.Painting, ConvoFlavor.Tense, false)] = new[]
        {
            S(A("{S} observe une toile d'un air critique.\nCe genre de peinture, c'est n'importe quoi. Un enfant ferait mieux."),
              B("Un enfant ferait sûrement plus joyeux, oui. Mais celui-là, au moins, ose."),
              A("Oser ne suffit pas. Il faut du talent."),
              B("Et toi, tu en as beaucoup, du talent ? Montre-moi.")),
            S(A("{S} tient un pinceau sec, l'air sombre.\nAujourd'hui, je n'ai pas d'inspiration. Ça arrive aux grands artistes."),
              B("Ou aux gens qui n'ont plus de peinture."),
              A("Ne me vexe pas, je suis fragile aujourd'hui."),
              B("Bon. Je te prête la mienne, mais tu peins quelque chose de gai.")),
        },
        [(PlynlingPassion.Painting, ConvoFlavor.Tense, true)] = new[]
        {
            S(A("{S} pointe un tableau du doigt.\nJe suis {s:meilleur|meilleure} que toi en couleurs, c'est évident."),
              B("En couleurs ? Tu mélanges tout jusqu'à ce que ça devienne marron."),
              A("C'est un marron très travaillé."),
              B("Un marron reste un marron. Regarde mon vert, et dis-moi que le tien est mieux.")),
            S(A("{S} claque un pinceau sur la palette.\nCe jaune est à moi. Je l'ai découvert le premier."),
              B("On ne découvre pas un jaune : il existe depuis toujours."),
              A("Mais je l'ai mélangé le premier !"),
              B("Tu l'as mélangé après moi, et moins bien. Peignons chacun un tableau avec, et on verra.")),
            S(A("{S} présente un portrait très sérieux.\nC'est le meilleur portrait jamais peint dans le coin."),
              B("Le nez est au milieu du front."),
              A("C'est un choix artistique."),
              A("Peins-en un meilleur, si tu en es capable. Je pose pour toi, même.")),
        },
        [(PlynlingPassion.Sport, ConvoFlavor.Friendly, false)] = new[]
        {
            S(A("{S} trottine sur place en parlant.\nJ'ai couru quatre tours ce matin. Cinq, si on compte celui où je me suis {s:perdu|perdue}."),
              B("Cinq tours ? Tu n'es pas {l:fatigué|fatiguée} ?"),
              A("Si, mais quand je m'arrête, mon corps me le reproche."),
              B("Alors montre-moi comment tu tiens ce rythme sans t'arrêter.")),
            S(A("{S} boit une grande gorgée de rosée.\nIl faut bien s'hydrater. C'est ce que disent les sportifs. Et les plantes."),
              B("Les sportifs boivent de la rosée ?"),
              A("Les sportifs qui ont du goût. La rosée du matin, c'est la meilleure."),
              B("Alors la prochaine fois, je viens avec toi : on boit, et on court.")),
            S(A("{S} arrive en faisant des sauts de grenouille.\nC'est mon échauffement. La grenouille d'à côté est d'accord, elle aussi."),
              B("Ta grenouille s'échauffe avec toi ?"),
              B("Oh, regarde, elle saute plus loin que toi."),
              A("C'est ma coach. Allez, viens : on essaie de la battre à deux.")),
        },
        [(PlynlingPassion.Sport, ConvoFlavor.Friendly, true)] = new[]
        {
            S(A("{S} fait des étirements en parlant, l'air très sérieux.\nOn ne néglige jamais l'échauffement. Jamais."),
              B("Jamais ! Je m'échauffe même pour aller chercher mon goûter."),
              A("Ça, c'est de la discipline. Tu fais quoi, comme sport, en général ?"),
              B("La course, surtout. Et toi ? On devrait faire un tour ensemble.")),
            S(A("{S} lance une noix en l'air et la rattrape.\nTrente-deux fois de suite, ce matin. La trente-troisième est tombée sur ma tête."),
              B("Trente-deux ! Mon record est de vingt-neuf, avec une noisette."),
              A("Une noisette, c'est plus difficile : elle est plus petite."),
              B("Alors défie-moi : la meilleure série gagne le goûter.")),
        },
        [(PlynlingPassion.Sport, ConvoFlavor.Tense, false)] = new[]
        {
            S(A("{S} bombe le torse.\nJe suis le plus rapide du coin. Personne ne me rattrape."),
              B("Le plus rapide ? Je t'ai vu t'arrêter pour renouer ton lacet."),
              A("C'était une pause tactique !"),
              B("Une tactique de lenteur, alors. Cours, qu'on voie.")),
            S(A("{S} tient une petite médaille en carton.\nJe l'ai gagnée. Contre moi-même. C'était une course serrée."),
              B("Contre toi-même ? Tu as gagné contre quelqu'un qui n'a jamais couru."),
              A("C'est le plus dur des adversaires !"),
              B("C'est le plus facile à battre, surtout. Cours contre moi, pour changer.")),
        },
        [(PlynlingPassion.Sport, ConvoFlavor.Tense, true)] = new[]
        {
            S(A("{S} bat des bras avec fracas.\nJe fais le tour de ce buisson en dix secondes. Personne ne fait mieux."),
              B("Dix secondes ? J'en mets neuf, avec une main dans la poche."),
              A("Tu mens."),
              B("Alors chronomètre-moi. Tu comptes, je cours.")),
            S(A("{S} touche ses orteils sans plier les genoux.\nÇa, c'est de la souplesse. Tu n'en es pas capable."),
              B("Regarde bien : je les touche, et je fais la roue derrière."),
              A("La roue ? Ça ne vaut rien sans la réception."),
              B("Alors je la réussis en plus. Tu regardes ?")),
            S(A("{S} s'étire ostensiblement.\nJe cours plus vite que toi. Depuis toujours."),
              B("Tu m'as battu une fois. Il pleuvait, j'ai glissé."),
              A("Une victoire reste une victoire."),
              A("Mais si tu veux une revanche au sec, je suis {s:prêt|prête}. Tout de suite.")),
        },
        [(PlynlingPassion.Insects, ConvoFlavor.Friendly, false)] = new[]
        {
            S(A("{S} s'accroupit devant une fourmi qui transporte une miette énorme.\nElle porte cinquante fois son poids. Moi, je ne porte même pas mon goûter jusqu'au bout."),
              B("Cinquante fois ? C'est incroyable pour un si petit corps."),
              A("Elle est plus forte que nous deux réunis. Enfin, proportionnellement."),
              B("Alors on l'aide à porter sa miette ? Elle n'a rien demandé, mais elle acceptera.")),
            S(A("{S} garde une coccinelle sur le doigt depuis le début.\nElle s'appelle Pépite. On se connaît depuis ce matin. C'est déjà sérieux."),
              B("Elle est mignonne. Elle reste sur toi toute seule ?"),
              A("Elle a choisi. Les coccinelles sont libres, mais elle m'a choisi."),
              B("Tu peux me la montrer de plus près ? Je promets de ne pas respirer.")),
            S(A("{S} montre une libellule posée sur une tige.\nElle sait voler en arrière, tu sais. Moi, je n'arrive même pas à marcher en arrière."),
              B("En arrière ? Vraiment ?"),
              B("Oh, elle vient de le faire ! Elle a reculé d'un coup !"),
              A("Tu vois ? Viens, on la suit sans faire de bruit. Elle recommencera peut-être.")),
        },
        [(PlynlingPassion.Insects, ConvoFlavor.Friendly, true)] = new[]
        {
            S(A("{S} garde les mains en coupe, avec beaucoup de précautions.\nIl y a un grillon dedans. Il est timide. Ne fais pas de bruit, il va chanter."),
              B("Un grillon ! J'en ai un aussi, qui vit sous ma fenêtre. Il chante en fa."),
              A("Le mien chante en sol. Il se sent sûrement seul."),
              B("Alors présentons-les l'un à l'autre. Ils pourraient chanter en duo.")),
            S(A("{S} s'allonge dans l'herbe, le nez au ras du sol.\nIl y a toute une ville, là-dessous. Des rues, des embouteillages, des disputes."),
              B("Je sais ! Je viens de voir deux fourmis se disputer pour une miette."),
              A("Et alors, qui a gagné ?"),
              B("La plus petite : elle a filé avec la miette pendant que l'autre criait. On se couche ici et on regarde la suite ?")),
        },
        [(PlynlingPassion.Insects, ConvoFlavor.Tense, false)] = new[]
        {
            S(A("{S} écrase un moustique d'un air satisfait.\nUn de moins. Ces insectes ne servent à rien."),
              B("Ils servent à nourrir les oiseaux, et les grenouilles."),
              A("Les grenouilles n'ont qu'à se débrouiller."),
              B("Ça te ferait peut-être du bien de regarder les insectes autrement. Viens, je te montre.")),
            S(A("{S} se gratte le bras avec humeur.\nCes fourmis sont partout ! Je déteste les insectes."),
              B("Tu détestes ce que tu ne comprends pas."),
              A("Je comprends très bien qu'ils piquent."),
              B("Seulement certains. Les autres sont innocents. Regarde celle-là : elle porte sa miette.")),
        },
        [(PlynlingPassion.Insects, ConvoFlavor.Tense, true)] = new[]
        {
            S(A("{S} désigne une fourmi avec mépris.\nMes fourmis sont mieux organisées que les tiennes."),
              B("Tes fourmis ? Elles ne t'appartiennent pas. Et les miennes ont un plan."),
              A("Un plan ? Elles suivent juste un chemin."),
              B("Un chemin qu'elles ont tracé elles-mêmes. On les suit ensemble, on verra qui a raison.")),
            S(A("{S} agite un filet à papillons.\nJ'ai attrapé plus de papillons que toi."),
              B("Attrapé ? Ils ne sont pas à toi. Et je connais plus de noms de papillons que toi."),
              A("Ça, ce n'est pas un sport."),
              B("C'est bien plus difficile. Assieds-toi, je te fais passer l'examen.")),
            S(A("{S} s'agenouille devant une fourmilière.\nJe connais chaque fourmi de cette colonie."),
              B("Chaque fourmi ? Il y en a des milliers."),
              A("Et je les connais toutes. Enfin, de vue."),
              A("Nomme-moi une seule des tiennes, et je t'en nomme dix des miennes.")),
        },
        [(PlynlingPassion.Naps, ConvoFlavor.Friendly, false)] = new[]
        {
            S(A("{S} bâille avant même d'avoir dit bonjour.\nJ'ai fait une sieste si réussie que j'en ai rêvé d'une autre, dedans."),
              B("Une sieste dans une sieste ? C'est possible ?"),
              A("Avec un bon oreiller, tout est possible."),
              B("Alors montre-moi ce coin miraculeux. Je veux essayer.")),
            S(A("{S} arrive avec un oreiller de mousse sous le bras.\nJe ne compte pas dormir. C'est juste au cas où."),
              B("Au cas où quoi ?"),
              A("Au cas où un coin d'herbe serait particulièrement accueillant."),
              B("On en cherche un ensemble ? J'ai du flair pour ça.")),
            S(A("{S} s'appuie contre {L} sans prévenir.\nTu es très confortable, tu sais. Ne bouge pas, je teste."),
              B("Tu testes quoi, exactement ?"),
              B("… Bon, d'accord, tu es confortable aussi. C'est gênant."),
              A("Alors on reste comme ça encore un peu. C'est un ordre de sieste.")),
        },
        [(PlynlingPassion.Naps, ConvoFlavor.Friendly, true)] = new[]
        {
            S(A("{S} s'étire longuement.\nJ'ai fait une sieste de dix minutes. Elle a duré deux heures. C'est souvent comme ça."),
              B("Pareil pour moi ! Je dis toujours « juste cinq minutes » et je me réveille au coucher du soleil."),
              A("Le secret, c'est de ne pas mettre de réveil."),
              B("Ou de mettre plusieurs coussins. Viens, on teste les deux méthodes en même temps.")),
            S(A("{S} désigne un rayon de soleil sur l'herbe.\nTu vois ce petit carré de soleil ? C'est le mien. Je l'ai réservé."),
              B("Le mien est juste à côté. Il est plus grand."),
              A("Plus grand, mais moins chaud !"),
              B("Alors on se met à la frontière. On aura le chaud du tien et la place du mien.")),
        },
        [(PlynlingPassion.Naps, ConvoFlavor.Tense, false)] = new[]
        {
            S(A("{S} s'installe sur un coin d'herbe en se serrant contre {L}.\nCe coin, c'est le mien. Va en chercher un autre."),
              B("Il n'y a pas d'autre coin comme celui-là."),
              A("Justement."),
              B("Tu es égoïste, mais tu as bon goût. Fais-moi de la place, ou je m'assois dessus.")),
            S(A("{S} ferme un œil avec suffisance.\nJe suis le meilleur pour faire la sieste. Personne ne sait dormir comme moi."),
              B("Dormir, ce n'est pas un concours."),
              A("Tout est un concours. Surtout dormir."),
              B("Alors on se couche tous les deux et on voit qui ronfle le plus fort.")),
        },
        [(PlynlingPassion.Naps, ConvoFlavor.Tense, true)] = new[]
        {
            S(A("{S} plisse les yeux.\nLa meilleure sieste, c'est après le goûter, pas avant."),
              B("Avant ! Après, on est trop lourd, on n'arrive pas à s'endormir."),
              A("Tu n'y connais rien."),
              B("J'y connais tout. Alors fais ton test : sieste avant, sieste après, on compare.")),
            S(A("{S} bâille en frappant le sol du poing.\nMoi, je m'endors en trente secondes chrono."),
              B("Trente secondes ? Moi, en vingt."),
              A("Tu triches ! Tu fais semblant."),
              B("Chronomètre-moi. Et essaie de ne pas t'endormir avant moi.")),
            S(A("{S} s'étend de tout son long.\nJe fais les meilleures siestes du coin. C'est reconnu."),
              B("Reconnu par qui ? Par ton oreiller ?"),
              A("Par tout le monde. Même par les escargots."),
              A("Allonge-toi, qu'on compare. Le premier qui se réveille a perdu.")),
        },
    };

    private static readonly IReadOnlyDictionary<(ConvoFlavor, bool), Script[]> Custom =
        new Dictionary<(ConvoFlavor, bool), Script[]>
    {
        [(ConvoFlavor.Friendly, false)] = new[]
        {
            S(A("{S} se redresse, l'air très sérieux.\nJe dois t'avouer quelque chose. Ma passion ? {P}."),
              B("Ah ! Et depuis quand ?"),
              A("Depuis toujours, je crois. Enfin, depuis que j'ai su ce que c'était."),
              B("Alors raconte-moi tout : je ne connais rien du tout à ce sujet.")),
            S(A("{S} sort un carnet couvert de dessins.\nTout ça, c'est pour {P}. Oui, tout."),
              B("Tout ? Il y a beaucoup de pages !"),
              A("Et ce n'est que le premier carnet. J'ai déjà commencé le deuxième."),
              B("Alors tu vas m'en apprendre un peu, pour que je comprenne.")),
            S(A("{S} baisse la voix, comme pour un secret.\nPersonne ne le sait encore, mais ma grande passion, c'est… {P}."),
              B("Vraiment ? Tu ne m'en avais jamais parlé."),
              A("Je n'osais pas. C'est un peu bizarre, non ?"),
              B("Pas du tout. C'est justement ce qui rend une passion intéressante.")),
            S(A("{S} arrive avec un petit panneau autour du cou. Dessus : {P}.\nComme ça, tout le monde sait. C'est plus simple."),
              B("Tu t'y intéresses depuis longtemps ?"),
              A("Assez longtemps pour avoir fabriqué un panneau."),
              B("Alors tu vas devoir m'expliquer ce que c'est vraiment.")),
        },
        [(ConvoFlavor.Friendly, true)] = new[]
        {
            S(A("{S} arrive en sautillant.\nDevine ce qui me passionne en ce moment : {P} !"),
              B("Non ? Moi aussi, en ce moment ! Je n'en reviens pas."),
              A("Depuis quand ?"),
              B("Depuis longtemps, sans jamais oser en parler. Maintenant, on peut en parler ensemble.")),
            S(A("{S} se redresse, l'air très sérieux.\nJe dois t'avouer quelque chose. Ma passion ? {P}."),
              B("La mienne aussi ! On a ça en commun."),
              A("Ça alors ! Toi qui m'as toujours semblé si {l:différent|différente} de moi."),
              B("Comme quoi, les apparences… Raconte-moi ce que tu aimes le plus là-dedans.")),
            S(A("{S} sort un carnet couvert de dessins.\nTout ça, c'est pour {P}. Oui, tout."),
              B("Attends, montre ! J'ai le même carnet, à la maison, presque plein aussi."),
              A("Le même ? On devrait les comparer !"),
              B("Tout de suite. Je te montre les meilleures pages en premier.")),
            S(A("{S} se met à parler très vite.\nJ'ai-découvert-un-truc-génial : {P}. Il-faut-que-je-t'explique-tout."),
              B("Je connais déjà tout ! Pour moi aussi, c'est une passion."),
              A("Alors je n'ai rien à t'expliquer ?!"),
              B("Si : tes découvertes à toi, celles que je ne connais pas. Vas-y, je t'écoute.")),
        },
        [(ConvoFlavor.Tense, false)] = new[]
        {
            S(A("{S} croise les bras, l'air {s:supérieur|supérieure}.\nMa passion, c'est {P}. Et je suis le meilleur du coin."),
              B("Le meilleur ? Tu n'as même pas de public."),
              A("Le talent n'a pas besoin de public."),
              B("Le talent, si. Prouve-le devant moi.")),
            S(A("{S} soupire.\nPersonne ne comprend ma passion : {P}. Personne."),
              B("Peut-être parce que tu en parles toujours d'un air agacé."),
              A("Je ne suis pas {s:agacé|agacée} !"),
              B("Bon. Explique-moi, alors, sans te fâcher.")),
            S(A("{S} montre un objet en rapport avec {P}, l'air méprisant.\nCeci vaut plus que tout ce que tu possèdes."),
              B("Si tu le dis. Moi, je le trouve assez moche."),
              A("Tu es {l:jaloux|jalouse}."),
              B("Ou simplement honnête. Mais si tu veux me convaincre, essaie.")),
            S(A("{S} lève le menton.\nJe suis plus {s:passionné|passionnée} que toi, quoi que tu dises. Le sujet ? {P}."),
              B("Tu n'as même pas dit ce que tu faisais."),
              A("Je fais tout !"),
              B("Alors montre un seul exemple. Un seul. Je t'attends.")),
        },
        [(ConvoFlavor.Tense, true)] = new[]
        {
            S(A("{S} pointe du doigt.\nSur {P}, je suis imbattable. Personne ne m'arrive à la cheville."),
              B("Sur ce sujet, c'est moi le meilleur. Depuis des années."),
              A("Des années ? Moi, des siècles !"),
              B("Alors un défi. Sur le terrain. Maintenant.")),
            S(A("{S} claque des doigts.\nJe connais tout sur {P}, absolument tout."),
              B("Tout ? Alors dis-moi le point que tu maîtrises le moins."),
              A("Il n'y en a pas !"),
              B("Tu mens : tout le monde a un point faible. On va le prouver.")),
            S(A("{S} ricane.\nSur {P}, tu ne fais que répéter ce que j'ai dit."),
              B("Je l'ai dit avant toi."),
              A("C'est faux !"),
              B("C'est vrai. On règle ça en refaisant tout, chacun sa méthode.")),
            S(A("{S} tape du poing dans sa main.\nSur {P}, il n'y a qu'un vrai connaisseur ici. C'est moi."),
              B("Deux, puisque je suis là aussi. Et je suis {l:meilleur|meilleure}."),
              A("Meilleur ? Tu ne t'es jamais entraîné."),
              B("J'ai fait plus que toi. Défi ?")),
        },
    };
}
