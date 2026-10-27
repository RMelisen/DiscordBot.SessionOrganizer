using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

// Wave 3.
public static partial class PlynlingEvents
{
    private static EventDef[] Wave3() => new[]
    {
        // ---- wave 3: bébé
        new EventDef("baby_berry_stall", EventType.Pulse, Baby, "L'étal sans marchand",
            "Au bord du chemin, un étal de fraises, et personne derrière. Un bocal, une pancarte : « Un caillou la fraise. Merci. » {A} n'a pas de caillou. {A} a très, très envie d'une fraise.",
            new[]
            {
                Try("take", "En prendre une, juste une", new EventChallenge(PlynlingStat.Intrigue, 4),
                    "{A} prend une fraise et la mange. La fraise est parfaite. Le remords aussi, jusqu'au soir.",
                    "Au moment où {A} tend la patte, la pancarte se retourne toute seule. Au dos : « Je te vois. — La pie. » {A} repart les pattes vides et le cœur battant.",
                    E(new GrowStat(PlynlingStat.Intrigue)), Nothing, Ai((AiAxis.Honor, -2), (AiAxis.Greed, 1))),
                Plain("drawing", "Payer avec un dessin de fraise",
                    "{A} glisse dans le bocal un dessin de fraise, très ressemblant, et prend une vraie fraise. Le lendemain, la pancarte dit : « Un caillou la fraise, ou un dessin. Merci. »",
                    E(new GrowStat(PlynlingStat.Diplomacy), new ApplyModifier("clear_conscience")), Ai((AiAxis.Honor, 1), (AiAxis.Rationality, 1))),
                Plain("mind", "Tenir l'étal en attendant le marchand",
                    "{A} tient l'étal tout l'après-midi, très {a:sérieux|sérieuse}, et vend onze fraises à onze passants. Le vieux lapin revient, compte, et offre la douzième.",
                    E(new GrowStat(PlynlingStat.Stewardship)), Ai((AiAxis.Honor, 1), (AiAxis.Energy, 1)), Stress(("rowdy", 10))),
            }),

        new EventDef("baby_burp", EventType.Pulse, Baby, "Le silence de la bibliothèque",
            "À la bibliothèque, le silence est si profond qu'on entend les pages vieillir. C'est le moment que choisit le ventre de {A} pour lâcher un rot. Un énorme. La chouette ouvre un œil.",
            new[]
            {
                Try("sorry", "S'excuser, tout bas", new EventChallenge(PlynlingStat.Diplomacy, 4),
                    "{A} chuchote « pardon », si bas que la chouette doit se pencher. La chouette hoche la tête et referme l'œil. Plus tard, un livre apparaît sur la table de {A} : « Les bonnes manières, tome 1 ».",
                    "{A} chuchote « pardon », et le hoquet arrive. Hic. Hic. La chouette ouvre le deuxième œil.",
                    E(new GrowStat(PlynlingStat.Diplomacy)), Nothing, Ai((AiAxis.Honor, 1))),
                Plain("encore", "En faire un deuxième, encore plus beau",
                    "{A} prend une grande inspiration et se surpasse. Les étagères tremblent. Au fond, quelqu'un applaudit. La chouette sort sans un mot, et revient avec un écriteau : « Silence. Même toi. »",
                    E(new ApplyModifier("light_heart")), Ai((AiAxis.Boldness, 2), (AiAxis.Honor, -1)), gate: new TraitGate("rowdy")),
                Try("blame", "Regarder l'escargot d'un air accusateur", new EventChallenge(PlynlingStat.Intrigue, 4),
                    "{A} fixe l'escargot avec reproche. Toute la salle se tourne vers l'escargot. L'escargot, digne, rentre dans sa coquille et n'en ressort qu'à la fermeture.",
                    "L'escargot fixe {A} en retour, plus longtemps, avec plus de reproche. {A} craque en premier.",
                    E(new GrowStat(PlynlingStat.Intrigue)), Nothing, Ai((AiAxis.Honor, -2)), Stress(("charming", 10))),
            }),

        new EventDef("baby_lost", EventType.Pulse, Baby, "Où sont passés les autres ?",
            "{A} cueillait des mûres avec tout le monde. {A} relève la tête : plus personne. Juste des ronces, une mare, et une vieille crapaude qui regarde {A} par-dessus ses lunettes.",
            new[]
            {
                Plain("ask", "Demander son chemin à la crapaude",
                    "La crapaude indique le chemin, mais d'abord, la recette de sa soupe d'orties. En entier. Avec les variantes. {A} rentre à la nuit, le chemin en tête et la recette aussi.",
                    E(new GrowStat(PlynlingStat.Learning), new FollowUp("baby_lost_soup", 48, 96)), Ai((AiAxis.Sociability, 1), (AiAxis.Rationality, 1)), Stress(("rowdy", 10))),
                Try("stream", "Suivre le ruisseau, {a:tout seul|toute seule}", new EventChallenge(PlynlingStat.Courage, 4),
                    "{A} suit le ruisseau, qui suit son idée, et débouche pile sous le vieux pont. Le héron salue d'une patte : « Tu es en retard pour le goûter. »",
                    "Le ruisseau tourne en rond, et {A} aussi. C'est finalement la crapaude qui ramène {A} au village, en grommelant, et en récitant sa recette.",
                    E(new GrowStat(PlynlingStat.Courage)), E(new FollowUp("baby_lost_soup", 48, 96)), Ai((AiAxis.Boldness, 2))),
                Plain("wait", "Rester là et attendre qu'on vienne",
                    "{A} s'assoit sur une souche. Les autres reviennent vite, en criant le nom de {A}, et {A} est {a:câliné|câlinée} par tout le monde. La crapaude, déçue, garde sa recette pour une autre fois.",
                    E(new ApplyModifier("cherished")), Ai((AiAxis.Rationality, 1), (AiAxis.Boldness, -1))),
            }),

        new EventDef("baby_lost_soup", EventType.FollowUp, AnyStage, "La soupe de la crapaude",
            "On frappe. C'est la vieille crapaude, une marmite dans les pattes. « Soupe d'orties. Pour grandir. Ne dis pas non : ça pique moins quand c'est chaud. »",
            new[]
            {
                Plain("taste", "Goûter, bravement",
                    "{A} goûte. Ça pique un peu, ça réchauffe beaucoup. La crapaude regarde {A} manger jusqu'à la dernière cuillère, puis repart sans un mot, satisfaite.",
                    E(new ApplyModifier("hearty")), Ai((AiAxis.Boldness, 1))),
                Plain("invite", "Inviter la crapaude à manger aussi",
                    "La crapaude s'installe, mange trois bols, raconte sa jeunesse dans une mare très chic, et oublie sa marmite en partant. La marmite est toujours là.",
                    E(new GrowStat(PlynlingStat.Diplomacy)), Ai((AiAxis.Sociability, 2))),
            }),

        new EventDef("baby_grow_up", EventType.Pulse, Baby, "Plus tard, je serai…",
            "{A} surgit d'un buisson, une brindille à la patte, et la pointe vers le héron : « Quand je serai {a:grand|grande}, je serai… » Le héron attend la suite, sur une patte, très intéressé.",
            new[]
            {
                Plain("explore", "« …partir explorer le monde entier ! »",
                    "« Commence par le fond du jardin », dit le héron. {A} part aussitôt, et revient avec une carte, un ver de terre et de grandes ambitions.",
                    E(new GrowStat(PlynlingStat.Courage), new FollowUp("grown_dream_explore", 336, 384)), Ai((AiAxis.Boldness, 2))),
                Plain("cafe", "« …tenir le café, comme la tortue ! »",
                    "« Alors entraîne-toi », dit le héron, en tendant une tasse vide. {A} sert un café imaginaire, rend une monnaie imaginaire, et réclame un pourboire bien réel.",
                    E(new GrowStat(PlynlingStat.Stewardship), new FollowUp("grown_dream_cafe", 336, 384)), Ai((AiAxis.Greed, 1), (AiAxis.Sociability, 1))),
                Plain("owl", "« …tout savoir, comme la chouette ! »",
                    "« Alors dis-moi combien j'ai de plumes », dit le héron. {A} commence à compter. Le soleil se couche à la plume trois cent quatre.",
                    E(new GrowStat(PlynlingStat.Learning), new FollowUp("grown_dream_owl", 336, 384)), Ai((AiAxis.Rationality, 2))),
                Plain("chief", "« …chef du village, et de tout le monde ! »",
                    "« Très bien, chef », dit le héron, sans bouger d'une plume. {A} donne trois ordres au héron, deux à l'escargot et un à la rivière. Seul l'escargot obéit, mais l'escargot allait déjà dans ce sens.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new FollowUp("grown_dream_chief", 336, 384)), Ai((AiAxis.Boldness, 1), (AiAxis.Sociability, 1)), gate: new TraitGate("bossy")),
            }),

        // ---- wave 3: ado
        new EventDef("teen_secret", EventType.Pulse, Teen, "Un secret trop lourd",
            "Depuis des jours, {A} porte un secret. Un petit secret, mais lourd, comme un gland qui serait en plomb. {B} est {b:le seul|la seule} à qui {A} pourrait le dire.",
            new[]
            {
                Ask("tell", "Tout raconter à {B}", "{A} entraîne {B} derrière le vieux pont et chuchote tout, d'une traite.", "reply_secret",
                    Ai((AiAxis.Sociability, 2)), ownerOnly: false),
                Plain("journal", "L'écrire dans un carnet, puis cacher le carnet",
                    "{A} écrit le secret, ferme le carnet et le glisse sous une latte du plancher. Le secret pèse beaucoup moins. Le plancher grince un peu plus.",
                    E(new ApplyModifier("soothed")), Ai((AiAxis.Sociability, -1), (AiAxis.Rationality, 1))),
                Plain("keep", "Le garder pour soi",
                    "{A} garde le secret. Certains jours, ça pèse. D'autres jours, ça tient chaud.",
                    E(new GrowStat(PlynlingStat.Intrigue)), Ai((AiAxis.Rationality, 1)), Stress(("gregarious", 10))),
            },
            Target: TargetKind.Known,
            TargetCondition: t => t.Bond is PlynlingBond.Friends or PlynlingBond.BestFriends),

        new EventDef("reply_secret", EventType.Response, AnyStage, "Un secret confié",
            "Derrière le vieux pont, {B} a chuchoté un secret à {A}, d'une traite, sans respirer.",
            new[]
            {
                Answer("promise", "Promettre de ne jamais le répéter",
                    "{A} promet, la patte sur le cœur. {B} respire enfin. Le secret dort maintenant chez deux personnes, et dort beaucoup mieux.",
                    Stance.Accept, E(new AffinityShift(15)), Ai((AiAxis.Honor, 2))),
                Answer("swap", "Confier un secret en retour",
                    "{A} se penche et chuchote à son tour. {B} écarquille les yeux. Les deux secrets se ressemblent comme deux glands.",
                    Stance.Accept, E(new AffinityShift(20)), Ai((AiAxis.Sociability, 2))),
                Answer("laugh", "Éclater de rire : ce n'est vraiment pas grave",
                    "« C'est tout ? » {B} rougit, puis rit aussi, un peu {b:vexé|vexée}. D'un coup, le secret ne pèse plus rien.",
                    Stance.Refuse, E(new AffinityShift(5)), Ai((AiAxis.Rationality, 1))),
            }),

        new EventDef("teen_treasure_map", EventType.Pulse, Teen, "La carte au trésor",
            "En ouvrant un vieux livre de la bibliothèque, {A} fait tomber une carte. Une vraie : un pont, un arbre tordu, une croix. La chouette n'a rien vu. D'habitude, la chouette voit tout.",
            new[]
            {
                Plain("dig", "Partir creuser, le soir même",
                    "{A} emprunte une pelle, compte les pas depuis le vieux pont, et creuse. À la nuit tombée, la pelle fait « tonk ».",
                    E(new FollowUp("teen_map_box", 1, 4)), Ai((AiAxis.Boldness, 1), (AiAxis.Energy, 1)), Stress(("craven", 10))),
                Try("study", "Étudier la carte avant de creuser", new EventChallenge(PlynlingStat.Learning, 6),
                    "{A} remarque que l'arbre tordu est dessiné à l'envers. Une heure de calculs plus tard, la croix est au bon endroit, et la pelle aussi.",
                    "{A} étudie tant que la carte finit en boule. {A} creuse au hasard : trois trous, puis un quatrième. Le quatrième fait « tonk ».",
                    E(new GrowStat(PlynlingStat.Learning), new FollowUp("teen_map_box", 1, 4)), E(new FollowUp("teen_map_box", 1, 4)), Ai((AiAxis.Rationality, 2))),
                Plain("return", "Rendre la carte à la chouette",
                    "La chouette regarde la carte, puis {A}, puis la carte. « Garde-la. Certaines choses doivent être trouvées par quelqu'un. » Pas d'autre explication.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new FollowUp("teen_map_box", 24, 48)), Ai((AiAxis.Honor, 2))),
            }),

        new EventDef("teen_map_box", EventType.FollowUp, AnyStage, "Ce que la croix cachait",
            "Au fond du trou, une boîte en fer. Dans la boîte, un carnet. Sur la première page, d'une écriture toute ronde : « Journal du héron. Ne pas lire. Surtout toi. »",
            new[]
            {
                Try("read", "Lire, juste la première page", new EventChallenge(PlynlingStat.Intrigue, 6),
                    "« Aujourd'hui, j'ai essayé de tenir sur une patte. Je suis tombé dans la rivière. Demain, je réessaie. » {A} referme le carnet, très doucement.",
                    "{A} lit trois lignes. Une ombre passe sur la page : le héron, sur une patte, juste derrière. « Surtout toi », répète le héron.",
                    E(new GrowStat(PlynlingStat.Intrigue)), Nothing, Ai((AiAxis.Honor, -1), (AiAxis.Rationality, 1)), Stress(("honest", 20))),
                Plain("give", "Le rapporter au héron, sans l'ouvrir",
                    "Le héron prend le carnet, le soupèse et le glisse sous son aile. Le héron fait semblant de ne pas être ému. Le héron fait ça très mal.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new ApplyModifier("clear_conscience")), Ai((AiAxis.Honor, 2))),
                Plain("rebury", "Le réenterrer pour le prochain chercheur",
                    "{A} referme la boîte, rebouche le trou et redessine la carte, en plus joli. La nouvelle carte part dans un autre livre, pour quelqu'un d'autre.",
                    E(new GrowStat(PlynlingStat.Stewardship)), Ai((AiAxis.Rationality, 1))),
            }),

        new EventDef("teen_ghost_stories", EventType.Pulse, Teen, "Histoires au coin du feu",
            "La nuit est tombée. Autour du feu, {A} et {B} se racontent des histoires à faire peur. Une seule règle : le premier qui sursaute a perdu.",
            new[]
            {
                Try("story", "Raconter la plus effrayante, sans sursauter", new EventChallenge(PlynlingStat.Courage, 6, VsTarget: true),
                    "{A} raconte le hérisson qui a perdu sa montre, une nuit, et qui la cherche encore, à pas lents, de jardin en jardin. Au moment où une brindille craque — c'était le hérisson, au loin, qui cherchait vraiment sa montre — {B} saute en l'air. Victoire.",
                    "{B} raconte la tortue qui, la nuit, recompte les cailloux de sa caisse, un par un, en se rapprochant des maisons. Au même moment, quelque chose bouge dans les fourrés. {A} saute si haut qu'une chouette doit s'écarter.",
                    E(new GrowStat(PlynlingStat.Courage), new AffinityShift(5)), E(new AffinityShift(5)), Ai((AiAxis.Boldness, 2)), Stress(("craven", 20))),
                Try("sheet", "S'éclipser, et grogner comme le blaireau dans les fourrés", new EventChallenge(PlynlingStat.Intrigue, 6),
                    "{A} grogne dans les fourrés, exactement comme le vieux blaireau. {B} hurle, puis rit, puis jure de se venger. Au loin, le vrai blaireau grogne en retour. Plus personne ne rit. Par précaution, {A} dort la lumière allumée pendant une semaine.",
                    "{A} grogne si fort que ça finit en quinte de toux. {B} tend une tasse d'eau vers les fourrés, sans même s'arrêter de raconter.",
                    E(new GrowStat(PlynlingStat.Intrigue), new AffinityShift(5), new ApplyModifier("sly")), Nothing, Ai((AiAxis.Honor, -1), (AiAxis.Boldness, 1))),
                Plain("kind", "Finir sur une histoire qui finit bien",
                    "{A} raconte le blaireau grognon qui voulait juste qu'on lui rende son écharpe. {B} trouve ça nul, et réclame la suite.",
                    E(new AffinityShift(10)), Ai((AiAxis.Compassion, 1), (AiAxis.Sociability, 1))),
            },
            Target: TargetKind.Known),

        new EventDef("teen_puppets", EventType.Pulse, Teen, "Les marionnettes de la foire",
            "À la foire, un marionnettiste ambulant joue sa pièce. L'une des marionnettes ressemble beaucoup à {A}. Trop. Et c'est celle qui tombe dans la rivière à chaque scène.",
            new[]
            {
                Plain("laugh", "Rire plus fort que tout le monde",
                    "{A} rit si fort que la salle rit de {A} qui rit de la marionnette. Le marionnettiste, ravi, ajoute une chute.",
                    E(new GrowStat(PlynlingStat.Diplomacy)), Ai((AiAxis.Sociability, 2)), Stress(("arrogant", 30))),
                Try("heckle", "« Ce n'est pas du tout comme ça que ça s'est passé ! »", new EventChallenge(PlynlingStat.Diplomacy, 6),
                    "{A} raconte la vraie version. La foule préfère la vraie version. Le marionnettiste prend des notes.",
                    "La foule fait « chut ». La marionnette aussi, d'une petite voix aiguë. {A} se rassoit.",
                    E(new GrowStat(PlynlingStat.Courage), new ApplyModifier("well_spoken")), Nothing, Ai((AiAxis.Boldness, 2), (AiAxis.Vengefulness, 1)), Stress(("shy", 20))),
                Try("steal", "Se glisser derrière le rideau et prendre la marionnette", new EventChallenge(PlynlingStat.Intrigue, 7),
                    "{A} fait danser la marionnette, saluer, puis pousser le méchant dans la rivière. Tonnerre d'applaudissements. Le marionnettiste propose une tournée.",
                    "{A} s'emmêle dans les fils. Pendant cinq minutes, le public ne sait plus qui manipule qui.",
                    E(new GrowStat(PlynlingStat.Intrigue), new ApplyModifier("inspired")), Nothing, Ai((AiAxis.Boldness, 2), (AiAxis.Honor, -1))),
            }),

        // ---- wave 3: adulte et ancien
        new EventDef("grown_snail_race", EventType.Pulse, Grown, "La course d'escargots",
            "C'est le jour de la grande course d'escargots. L'escargot du village s'est inscrit, contre l'avis de tout le monde, à commencer par l'escargot du village.",
            new[]
            {
                Try("bet", "Parier sur l'escargot du village", new EventChallenge(PlynlingStat.Stewardship, 8),
                    "L'escargot du village arrive dernier, mais tous les autres ont quitté la piste pour suivre une feuille de laitue. Victoire par abandon. {A} empoche la cagnotte : trois glands et une médaille en bouchon.",
                    "L'escargot du village s'arrête à mi-course pour saluer quelqu'un. Puis tout le monde. {A} perd son pari, et gagne un ami.",
                    E(new GrowStat(PlynlingStat.Stewardship), new ApplyModifier("trade_sense")), Nothing, Ai((AiAxis.Greed, 1), (AiAxis.Zeal, 1))),
                Plain("cheer", "L'encourager depuis le bord de la piste",
                    "{A} crie « Allez ! » pendant six heures. L'escargot du village franchit la ligne au coucher du soleil, sous les applaudissements de {A}, et de {A} {a:seul|seule}.",
                    E(new ApplyModifier("light_heart")), Ai((AiAxis.Compassion, 2), (AiAxis.Sociability, 1))),
                Try("carry", "Le porter un peu. Juste un peu.", new EventChallenge(PlynlingStat.Intrigue, 8),
                    "{A} soulève l'escargot « pour lui montrer le paysage » et le repose trois mètres plus loin. Personne n'a rien vu. L'escargot du village gagne, et ne pose aucune question.",
                    "Le moineau, qui arbitre, siffle. Disqualification. Pour la peine, {A} doit porter l'escargot jusqu'à chez lui. L'escargot trouve le trajet très agréable.",
                    E(new GrowStat(PlynlingStat.Intrigue)), Nothing, Ai((AiAxis.Honor, -2)), Stress(("honest", 30), ("just", 20))),
            }),

        new EventDef("grown_magpie", EventType.Pulse, Grown, "La pie voleuse",
            "Au marché, la pie fond sur {A} et repart avec le plus beau bouton de son manteau. Perchée tout en haut du chêne, la pie fait briller le bouton au soleil, très lentement, pour que {A} voie bien.",
            new[]
            {
                Try("climb", "Grimper le récupérer", new EventChallenge(PlynlingStat.Courage, 8),
                    "{A} grimpe jusqu'en haut. La pie, impressionnée, rend le bouton, et un deuxième, qui n'est pas à {A}.",
                    "{A} grimpe à mi-hauteur et reste {a:coincé|coincée}. La pie descend, s'assoit à côté, et commente le paysage jusqu'à l'arrivée des secours.",
                    E(new GrowStat(PlynlingStat.Courage), new FollowUp("grown_magpie_stash", 48, 96)), Nothing, Ai((AiAxis.Boldness, 2)), Stress(("craven", 20))),
                Try("trade", "Proposer un échange", new EventChallenge(PlynlingStat.Stewardship, 8),
                    "{A} propose un dé à coudre. La pie évalue, refuse, réclame une cuillère, accepte un ruban. Affaire conclue : la pie sait négocier, et {A} aussi.",
                    "La pie réclame le manteau entier, pour aller avec le bouton. Les négociations sont rompues.",
                    E(new GrowStat(PlynlingStat.Stewardship), new FollowUp("grown_magpie_stash", 48, 96), new ApplyModifier("trade_sense")), Nothing, Ai((AiAxis.Greed, 1), (AiAxis.Rationality, 1))),
                Plain("let", "Lui laisser le bouton",
                    "{A} fait au revoir au bouton. La pie, décontenancée, regarde le bouton d'un autre œil. Un bouton qu'on vous laisse brille moins.",
                    E(new GrowStat(PlynlingStat.Learning), new FollowUp("grown_magpie_stash", 48, 96)), Ai((AiAxis.Greed, -1), (AiAxis.Rationality, 1)), Stress(("greedy", 20))),
            }),

        new EventDef("grown_magpie_stash", EventType.FollowUp, AnyStage, "Le trésor de la pie",
            "En passant sous le grand chêne, {A} voit briller quelque chose dans un creux : le trésor de la pie. Des boutons, des cuillères, une clé, des lunettes, et la sonnette du hérisson, disparue depuis l'hiver.",
            new[]
            {
                Plain("return", "Tout rapporter aux propriétaires",
                    "{A} fait le tour du village. Le hérisson récupère sa sonnette et la fait sonner toute la journée. Du haut du chêne, la pie boude, mais regarde {A} avec quelque chose comme du respect.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new ApplyModifier("clear_conscience")), Ai((AiAxis.Honor, 2))),
                Plain("gift", "Y déposer un cadeau pour la pie",
                    "{A} dépose un petit caillou poli au milieu du trésor. Le lendemain, devant la porte de {A}, quelqu'un a laissé une cuillère. Très brillante.",
                    E(new ApplyModifier("magpie_friend")), Ai((AiAxis.Compassion, 1))),
                Plain("secret", "Ne toucher à rien, et ne rien dire",
                    "{A} ne touche à rien. Chaque fois que le hérisson cherche sa sonnette, {A} regarde ailleurs, très fort.",
                    E(new GrowStat(PlynlingStat.Intrigue)), Ai((AiAxis.Honor, -1)), Stress(("honest", 20))),
            }),

        new EventDef("grown_jam_cellar", EventType.Pulse, Grown, "La porte du garde-manger",
            "{A} est {a:resté|restée} aider la tortue à fermer le café. La tortue s'est endormie sur le comptoir. Au fond du garde-manger, {A} remarque une porte que personne n'a jamais remarquée.",
            new[]
            {
                Try("open", "Ouvrir la porte, sans bruit", new EventChallenge(PlynlingStat.Intrigue, 8),
                    "Derrière, une cave pleine de pots de confiture jusqu'au plafond. Chaque pot porte une étiquette, une année et quelques mots : « Le printemps où le pont a gelé. » « L'année du héron amoureux. » Le plus vieux pot date d'avant le village.",
                    "La porte grince. La tortue ouvre un œil, puis l'autre, et dit, très lentement : « Pas… encore. »",
                    E(new GrowStat(PlynlingStat.Intrigue), new FollowUp("grown_cellar_morning", 8, 16)), Nothing,
                    Ai((AiAxis.Boldness, 1), (AiAxis.Honor, -1)), Stress(("honest", 20))),
                Plain("wake", "Réveiller la tortue pour demander",
                    "La tortue se réveille, regarde la porte, regarde {A}. Après un très long silence : « Reviens demain. Tôt. »",
                    E(new GrowStat(PlynlingStat.Diplomacy), new FollowUp("grown_cellar_morning", 8, 16)), Ai((AiAxis.Honor, 2))),
                Plain("leave", "Faire comme si la porte n'existait pas",
                    "{A} prend une tartine, referme le garde-manger et rentre. Cette nuit-là, {A} rêve de portes. Les portes ont toutes un goût de confiture.",
                    E(new GrowStat(PlynlingStat.Learning)), Ai((AiAxis.Rationality, 1)), Stress(("curious", 20))),
            }),

        new EventDef("grown_cellar_morning", EventType.FollowUp, AnyStage, "Les pots de la tortue",
            "Au petit matin, la tortue attend {A} devant le garde-manger, une lanterne à la patte. « Chaque année, je garde un pot. Pour me souvenir. Choisis. »",
            new[]
            {
                Plain("oldest", "Le plus vieux pot",
                    "La tortue ouvre le plus vieux pot. Ça sent une saison dont personne d'autre ne se souvient. La tortue raconte ; {A} écoute jusqu'à midi.",
                    E(new GrowStat(PlynlingStat.Learning)), Ai((AiAxis.Rationality, 1))),
                Plain("this_year", "Celui de cette année, encore sans étiquette",
                    "La tortue tend une plume : « Alors c'est toi qui écris l'étiquette. » {A} réfléchit longtemps, puis écrit son propre nom.",
                    E(new ApplyModifier("cherished")), Ai((AiAxis.Sociability, 1))),
            }),

        new EventDef("grown_hobby", EventType.Pulse, Grown, "Juste une fois",
            "Depuis des semaines, {B} insiste : {A} doit essayer son passe-temps préféré. « Juste une fois. Tu verras, c'est facile. » {B} a apporté tout le matériel, et une liste.",
            new[]
            {
                Try("try", "Essayer, de bon cœur", new EventChallenge(PlynlingStat.Learning, 8),
                    "{A} écoute, essaie, rate, réessaie, et réussit. {B} saute de joie et en parle à tout le village pendant une semaine.",
                    "{A} essaie avec beaucoup de bonne volonté, pour un résultat que personne n'arrive à nommer. {B} dit que c'est très bien pour une première fois, d'une voix bizarre.",
                    E(new GrowStat(PlynlingStat.Learning), new AffinityShift(15)), E(new AffinityShift(10)),
                    Ai((AiAxis.Sociability, 1), (AiAxis.Compassion, 1)), Stress(("stubborn", 20))),
                Try("improve", "Proposer « une petite amélioration » à la méthode", new EventChallenge(PlynlingStat.Diplomacy, 8),
                    "{B} essaie l'amélioration, s'arrête, réessaie, et regarde {A} avec des yeux ronds. C'est mieux. C'est même beaucoup mieux.",
                    "{B} explique, très calmement, pourquoi la méthode est ainsi depuis toujours. L'explication dure jusqu'au dîner.",
                    E(new GrowStat(PlynlingStat.Stewardship), new AffinityShift(10)), E(new AffinityShift(-5)),
                    Ai((AiAxis.Boldness, 1), (AiAxis.Rationality, 1)), Stress(("humble", 20))),
                Plain("watch", "Refuser gentiment, mais regarder",
                    "{A} refuse, avec un sourire. {B} range le matériel, puis le ressort : « Et si tu regardais, juste ? » {A} regarde. Regarder, c'est déjà bien.",
                    E(new AffinityShift(5)), Ai((AiAxis.Rationality, 1))),
            },
            Target: TargetKind.Known),

        new EventDef("grown_bare_pantry", EventType.Pulse, Grown, "Un garde-manger bien vide",
            "Chez {B}, le garde-manger est presque vide : un pot de miel raclé jusqu'au fond, et trois glands. {B} sourit : « J'ai déjà mangé, mais sers-toi ! »",
            new[]
            {
                Try("basket", "Revenir avec un panier « trop lourd à porter »", new EventChallenge(PlynlingStat.Intrigue, 7),
                    "{A} revient avec un panier plein : « J'en ai trop, ça va se perdre. » {B} n'est pas dupe, et fait semblant de l'être, très bien.",
                    "{A} dépose le panier sur le paillasson et file. {B} reconnaît l'écriture de {A} sur l'étiquette du pot.",
                    E(new GrowStat(PlynlingStat.Intrigue), new AffinityShift(20)), E(new AffinityShift(10)), Ai((AiAxis.Compassion, 2), (AiAxis.Honor, 1))),
                Plain("feast", "Organiser un grand goûter… chez {B}",
                    "Tout le village débarque chez {B}, chacun avec un plat. À la fin, les restes remplissent le garde-manger jusqu'au plafond. {B} soupçonne quelque chose, mais trop tard.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new AffinityShift(15)), Ai((AiAxis.Sociability, 2))),
                Try("speak", "En parler franchement", new EventChallenge(PlynlingStat.Diplomacy, 8),
                    "« Ton garde-manger est vide. » {B} se tait, puis rit, puis raconte tout : un mauvais hiver, un pot cassé, la fierté. {A} et {B} font les courses ensemble.",
                    "{B} se raidit : « Tout va très bien. » Le sujet est clos, et la porte aussi, un peu vite.",
                    E(new AffinityShift(15)), E(new AffinityShift(-5)), Ai((AiAxis.Honor, 2)), Stress(("shy", 20))),
                Plain("tact", "Prendre un gland, et parler d'autre chose",
                    "{A} prend un gland, le mange lentement, et parle d'autre chose. Certaines fiertés ont besoin qu'on les laisse tranquilles.",
                    E(new GrowStat(PlynlingStat.Learning)), Ai((AiAxis.Rationality, 1)), Stress(("compassionate", 20))),
            },
            Target: TargetKind.Known,
            TargetCondition: t => t.Bond is PlynlingBond.Friends or PlynlingBond.BestFriends or PlynlingBond.Lovers),

        new EventDef("grown_wish_oak", EventType.Pulse, Grown, "La fête des souhaits",
            "Ce soir, c'est la fête des souhaits : chacun accroche un souhait aux branches du vieux chêne, puis en décroche un autre au hasard, à réaliser avant la fin de la semaine. {A} a un bout de papier, un crayon, et beaucoup trop de souhaits pour un seul papier.",
            new[]
            {
                Plain("stones", "Souhaiter « beaucoup de cailloux »",
                    "{A} écrit son souhait en lettres bien rondes et l'accroche tout en haut, là où tout le monde le verra.",
                    E(new FollowUp("grown_wish_stone", 24, 72)), Ai((AiAxis.Greed, 2))),
                Plain("fly", "Souhaiter « voler, une fois »",
                    "{A} écrit « voler, une fois », et accroche le papier à la branche la plus haute, pour donner l'exemple.",
                    E(new FollowUp("grown_wish_fly", 24, 72)), Ai((AiAxis.Boldness, 1), (AiAxis.Zeal, 1))),
                Plain("blank", "Accrocher le papier sans rien écrire",
                    "{A} accroche le papier blanc. Qui le décrochera choisira pour {A}. Et un souhait qu'on n'écrit pas, on ne peut pas le rater.",
                    E(new ApplyModifier("clear_conscience")), Ai((AiAxis.Rationality, 1), (AiAxis.Zeal, -1))),
            }),

        new EventDef("grown_wish_stone", EventType.FollowUp, AnyStage, "Beaucoup de cailloux",
            "Au matin, devant la porte de {A} : un caillou. Un seul, mais grand comme la maison. À côté, l'ours s'essuie le front : c'est l'ours qui a décroché le souhait, l'ours l'a pris au pied de la lettre, et l'ours a roulé ce caillou depuis la carrière.",
            new[]
            {
                Plain("bench", "En faire un banc pour tout le village",
                    "{A} taille, polit, et installe un banc devant la maison. Tout le village vient s'y asseoir. En un sens, c'est beaucoup de cailloux.",
                    E(new GrowStat(PlynlingStat.Stewardship)), Ai((AiAxis.Compassion, 1))),
                Try("spend", "Essayer de le dépenser au café", new EventChallenge(PlynlingStat.Diplomacy, 8),
                    "{A} pousse le caillou jusqu'au café. La tortue le contemple longuement, puis propose un chocolat chaud par jour, à vie, contre le droit de s'y adosser l'après-midi.",
                    "Le caillou bloque la porte du café pendant trois jours. Les clients passent par la fenêtre.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new ApplyModifier("trade_sense")), Nothing, Ai((AiAxis.Greed, 1))),
            }),

        new EventDef("grown_wish_fly", EventType.FollowUp, AnyStage, "Voler, une fois",
            "Une cigogne atterrit devant chez {A}, consulte un petit papier, puis {A}. « C'est toi, le souhait ? Je l'ai décroché à la fête. Monte. Un seul tour. »",
            new[]
            {
                Try("fly", "Monter, et garder les yeux ouverts", new EventChallenge(PlynlingStat.Courage, 8),
                    "Vu d'en haut, le village tient dans une patte. {A} voit le chêne, le pont, le café, et sa propre maison, toute petite. En redescendant, {A} n'a plus tout à fait la même taille, à l'intérieur.",
                    "{A} garde les yeux fermés tout le tour. Une fois en bas, {A} raconte quand même que c'était magnifique, et c'est sûrement vrai.",
                    E(new GrowStat(PlynlingStat.Courage), new ApplyModifier("inspired")), Nothing, Ai((AiAxis.Boldness, 2)), Stress(("craven", 20))),
                Plain("ground", "Remercier, mais rester par terre",
                    "{A} remercie la cigogne et lui offre une tartine à la place. La cigogne repart, un peu déçue, mais rassasiée. Voler, c'est aussi savoir qu'on aurait pu.",
                    E(new GrowStat(PlynlingStat.Learning)), Ai((AiAxis.Rationality, 1))),
            }),

        new EventDef("elder_story", EventType.Pulse, Elder, "Raconte, raconte !",
            "Une douzaine de petits du village s'assoient en rond autour de {A}, les yeux brillants. « Raconte quand tu étais {a:petit|petite} ! »",
            new[]
            {
                Try("embellish", "Embellir un peu. Beaucoup.", new EventChallenge(PlynlingStat.Diplomacy, 8),
                    "{A} raconte la grande crue. Dans l'histoire, la crue devient un dragon, le dragon une montagne, et {A} sauve tout le monde deux fois. Les petits applaudissent. Au fond, le héron lève un sourcil.",
                    "{A} s'emmêle : le dragon arrive avant la crue, et la montagne parle. Un petit lève la patte : « Ça ne tient pas debout. » Les petits adorent quand même.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new ApplyModifier("well_spoken")), E(new ApplyModifier("light_heart")),
                    Ai((AiAxis.Zeal, 1), (AiAxis.Sociability, 1)), Stress(("honest", 20))),
                Plain("true", "Raconter la vraie histoire, toute simple",
                    "{A} raconte la première flaque, le premier caillou, la première fois au café. Rien d'extraordinaire. Les petits écoutent sans bouger, parce que c'est vrai.",
                    E(new ApplyModifier("clear_conscience")), Ai((AiAxis.Honor, 2))),
                Plain("doze", "Commencer… et s'endormir au milieu",
                    "{A} commence une histoire de forêt, de nuit et de lanterne, et s'endort au milieu. Les petits inventent la fin eux-mêmes. Leur fin est bien meilleure.",
                    E(new ApplyModifier("well_rested")), Ai((AiAxis.Energy, -2))),
            }),

        new EventDef("elder_glasses", EventType.Pulse, Elder, "Les lunettes de la chouette",
            "Sur les étagères de {A}, les étiquettes des pots ont rapetissé. Toutes. En même temps. La chouette, de passage, pose sur la table une paire de lunettes rondes, sans un mot.",
            new[]
            {
                Plain("wear", "Les essayer",
                    "{A} met les lunettes. Les étiquettes reprennent leur taille normale. Le monde aussi. {A} découvre que le pot « miel » contient de la moutarde depuis trois ans.",
                    E(new GrowStat(PlynlingStat.Learning)), Ai((AiAxis.Rationality, 2)), Stress(("arrogant", 30))),
                Plain("refuse", "Refuser : les étiquettes ont rapetissé, c'est tout",
                    "{A} rend les lunettes. La chouette les pose un peu plus loin sur la table, au cas où. Le lendemain, {A} les porte, « juste pour voir si les étiquettes sont revenues ».",
                    E(new GrowStat(PlynlingStat.Learning)), Ai((AiAxis.Boldness, 1), (AiAxis.Rationality, -1))),
                Try("relabel", "Réécrire toutes les étiquettes, en très grand", new EventChallenge(PlynlingStat.Stewardship, 8),
                    "{A} réécrit chaque étiquette en lettres énormes. MIEL. CONFITURE. MOUTARDE (ATTENTION). Les étagères ressemblent à des affiches. La chouette approuve d'un hochement.",
                    "{A} réécrit tout en très grand, sans lunettes. Les étiquettes sont immenses, et toutes fausses.",
                    E(new GrowStat(PlynlingStat.Stewardship)), Nothing, Ai((AiAxis.Rationality, 1), (AiAxis.Energy, 1)), Stress(("lazy", 20))),
            }),
    };
}
