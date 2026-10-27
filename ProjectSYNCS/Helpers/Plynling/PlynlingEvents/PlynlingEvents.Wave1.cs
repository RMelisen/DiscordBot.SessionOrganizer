using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

// Wave 1: CK3 situations, retold.
public static partial class PlynlingEvents
{
    private static EventDef[] Wave1() => new[]
    {
        // ---- wave 1 (CK3 situations, retold): bébé
        new EventDef("baby_fledgling", EventType.Pulse, Baby, "L'oisillon tombé du nid",
            "Un oisillon tombé du nid piaille au bord du chemin. Pas de détresse : des reproches, à tout le monde, et surtout à {A}.",
            new[]
            {
                Try("climb", "Le remonter dans son nid", new EventChallenge(PlynlingStat.Courage, 4),
                    "{A} grimpe, branche après branche, l'oisillon sous le bras. En haut, l'oisillon fait remarquer que ce n'est pas son nid. Le sien est juste à côté.",
                    "{A} glisse à mi-hauteur et atterrit sur le derrière. L'oisillon, excédé, s'envole tout seul, pour ne plus avoir à regarder ça.",
                    E(new GrowStat(PlynlingStat.Courage), new ApplyModifier("light_heart")), Nothing, Ai((AiAxis.Boldness, 2))),
                Plain("moss", "Lui construire un nid de mousse en bas",
                    "{A} bâtit un nid de mousse, avec un toit en feuille et une sonnette. L'oisillon emménage aussitôt, et critique la décoration.",
                    E(new GrowStat(PlynlingStat.Stewardship)), Ai((AiAxis.Compassion, 2))),
                Plain("listen", "Écouter ses reproches jusqu'au bout",
                    "{A} s'assoit et écoute. Au bout d'une heure, {A} sait tout sur les vers de terre, les chats et les gens qui marchent trop fort. L'oisillon s'endort au milieu d'une phrase.",
                    E(new GrowStat(PlynlingStat.Learning)), Ai((AiAxis.Rationality, 1), (AiAxis.Sociability, 1)), gate: new TraitGate("curious")),
            }),

        new EventDef("baby_honey", EventType.Pulse, Baby, "Le pot de miel",
            "Sur le comptoir du café, le pot de miel de la tortue brille au soleil. La tortue est au fond de la salle, et la tortue est très, très lente.",
            new[]
            {
                Try("dip", "Y tremper une patte en vitesse", new EventChallenge(PlynlingStat.Intrigue, 4),
                    "{A} trempe une patte, puis deux, puis le museau. La tortue atteint le comptoir le lendemain matin et ne remarque rien.",
                    "{A} reste {a:coincé|coincée} la patte dans le pot. Trois villageois et une cuillère ne sont pas de trop pour l'en sortir. La tortue, arrivée entre-temps, tend une serviette.",
                    E(new GrowStat(PlynlingStat.Intrigue)), E(new ApplyModifier("muddy_paws")),
                    Ai((AiAxis.Greed, 1), (AiAxis.Honor, -1), (AiAxis.Boldness, 1)), Stress(("bossy", 20))),
                Plain("ask", "Demander poliment une cuillère",
                    "{A} attend au comptoir. La tortue traverse la salle. Le soleil traverse le ciel. Au coucher, {A} obtient sa cuillère de miel, et l'impression d'avoir beaucoup grandi.",
                    E(new GrowStat(PlynlingStat.Diplomacy)), Ai((AiAxis.Honor, 1), (AiAxis.Energy, -1))),
            }),

        new EventDef("baby_brindille", EventType.Pulse, Baby, "Monsieur Brindille",
            "{A} présente à tout le village son nouvel ami, Monsieur Brindille. Personne ne le voit. Monsieur Brindille a pourtant des exigences très précises.",
            new[]
            {
                Plain("rules", "Respecter toutes ses règles",
                    "{A} met une place de plus à table, à gauche, jamais à droite, avec les croûtes du pain. Le village apprend vite à ne pas s'asseoir sur la chaise de gauche.",
                    E(new ApplyModifier("light_heart"), new FollowUp("baby_brindille_leaves", 48, 96)), Ai((AiAxis.Honor, 1), (AiAxis.Rationality, 1))),
                Plain("snail", "Le présenter à l'escargot",
                    "L'escargot salue poliment le vide. Après un long silence, l'escargot assure que Monsieur Brindille est charmant, mais parle un peu fort.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new FollowUp("baby_brindille_leaves", 48, 96)), Ai((AiAxis.Sociability, 2))),
            }),

        new EventDef("baby_brindille_leaves", EventType.FollowUp, AnyStage, "Le départ de Monsieur Brindille",
            "Ce matin, Monsieur Brindille a fait sa valise. Personne ne voit la valise non plus, mais la valise a l'air lourde.",
            new[]
            {
                Plain("station", "L'accompagner jusqu'à la gare",
                    "Le hérisson chef de gare poinçonne deux billets sans poser de question. {A} fait signe au train jusqu'à ce que le train disparaisse.",
                    E(new ApplyModifier("soothed")), Ai((AiAxis.Sociability, 1))),
                Plain("gift", "Glisser un souvenir dans sa valise",
                    "{A} glisse un gland dans la valise invisible. Le gland disparaît pour de bon. Personne n'a jamais su comment.",
                    E(new GrowStat(PlynlingStat.Learning)), Ai((AiAxis.Compassion, 1))),
            }),

        // ---- wave 1: ado
        new EventDef("teen_fireflies", EventType.Pulse, Teen, "Les lucioles du vieux pont",
            "Une nuit par an, les lucioles se donnent rendez-vous sur le vieux pont. C'est ce soir, bien après le couvre-feu.",
            new[]
            {
                Try("sneak", "Sortir sans bruit par la fenêtre", new EventChallenge(PlynlingStat.Intrigue, 6),
                    "{A} s'assoit au bord du pont. Les lucioles s'allument une à une, puis toutes ensemble, et l'eau en dessous s'allume aussi. À deux pas, le héron fait semblant de dormir.",
                    "La fenêtre grince. Puis le volet. Puis {A}, en atterrissant dans les orties. Retour au lit : les lucioles, ce sera l'année prochaine.",
                    E(new ApplyModifier("inspired"), new FollowUp("teen_fireflies_heron", 18, 30)), Nothing,
                    Ai((AiAxis.Boldness, 2), (AiAxis.Honor, -1)), Stress(("just", 20))),
                Plain("owl", "Demander à la chouette de venir aussi",
                    "La chouette accepte, apporte un carnet et note chaque luciole. Trois cent douze, selon le carnet. La chouette en est très fière.",
                    E(new GrowStat(PlynlingStat.Learning)), Ai((AiAxis.Rationality, 1), (AiAxis.Honor, 1))),
                Plain("dream", "Rester au lit et rêver des lucioles",
                    "{A} s'endort en comptant des lucioles imaginaires. Au matin, sur le rebord de la fenêtre, une vraie luciole dort aussi.",
                    E(new ApplyModifier("well_rested")), Ai((AiAxis.Energy, -1))),
            }),

        new EventDef("teen_fireflies_heron", EventType.FollowUp, AnyStage, "Le héron n'a rien vu",
            "Le lendemain, le héron attend {A} au bout du vieux pont, sur une patte, l'air de ne rien savoir du tout.",
            new[]
            {
                Plain("thank", "Le remercier pour son silence",
                    "Le héron répond : « Pour quoi ? Je n'ai rien vu. » Puis, sans tourner la tête : « La prochaine fois, passe par la porte. »",
                    E(new GrowStat(PlynlingStat.Diplomacy)), Ai((AiAxis.Honor, 1))),
                Plain("whistle", "Passer en sifflotant",
                    "{A} passe en sifflotant. Le héron sifflote aussi, la même chanson, un peu faux.",
                    E(new GrowStat(PlynlingStat.Intrigue)), Ai((AiAxis.Honor, -1))),
            }),

        new EventDef("teen_timetables", EventType.Pulse, Teen, "Les horaires d'été",
            "Le hérisson chef de gare a reçu tous les horaires de l'été, en vrac, dans un seul carton. Dehors, la rivière est tiède et le soleil est parfait.",
            new[]
            {
                Plain("sort", "Aider à tout classer",
                    "{A} classe tout l'après-midi, par ligne, par heure, par couleur de tampon. À la fin, le hérisson range le carton, ravi… et en sort un deuxième : l'hiver.",
                    E(new GrowStat(PlynlingStat.Stewardship)), Ai((AiAxis.Energy, 1), (AiAxis.Compassion, 1)), Stress(("lazy", 30))),
                Plain("river", "Filer à la rivière",
                    "{A} passe l'après-midi dans l'eau tiède. En rentrant, {A} croise le hérisson, qui a tout classé seul et n'a jamais eu l'air aussi heureux.",
                    E(new ApplyModifier("light_heart")), Ai((AiAxis.Energy, -1), (AiAxis.Greed, 1)), Stress(("diligent", 30))),
                Try("both", "Proposer de classer… au bord de la rivière", new EventChallenge(PlynlingStat.Diplomacy, 7),
                    "Le hérisson accepte, à condition de garder ses chaussettes. Les horaires sont classés, les pieds sont au frais, et le train de 14 h 12 a failli partir au fil de l'eau.",
                    "Le hérisson refuse : les horaires ont le mal de mer. {A} classe à l'intérieur, les yeux sur la fenêtre.",
                    E(new GrowStat(PlynlingStat.Diplomacy)), Nothing, Ai((AiAxis.Sociability, 1), (AiAxis.Rationality, 1))),
            }),

        new EventDef("teen_shells", EventType.Pulse, Teen, "Le jeu des coquilles",
            "Au marché, la pie fait tourner trois coquilles de noix sur un tonneau. Sous l'une, un gland. Peut-être.",
            new[]
            {
                Try("play", "Tenter sa chance", new EventChallenge(PlynlingStat.Intrigue, 7),
                    "{A} montre la coquille du milieu, et la soulève avant la pie : le gland est là. La pie applaudit, vexée.",
                    "{A} perd trois fois de suite. La pie propose une quatrième partie, « pour le plaisir ». {A} refuse, pour sa dignité.",
                    E(new ApplyModifier("sly")), Nothing, Ai((AiAxis.Boldness, 1), (AiAxis.Greed, 1))),
                Try("cheat", "Glisser son propre gland sous une coquille", new EventChallenge(PlynlingStat.Intrigue, 6),
                    "La pie soulève la coquille, trouve un gland qui n'est pas le sien, et reste un long moment sans voix.",
                    "La pie voit tout. La pie voit toujours tout. {A} repart avec un clin d'œil professionnel, et sans son gland.",
                    E(new GrowStat(PlynlingStat.Intrigue)), Nothing, Ai((AiAxis.Honor, -2), (AiAxis.Greed, 1)), Stress(("honest", 30), ("just", 20))),
                Plain("expose", "Montrer à tout le monde où est vraiment le gland",
                    "{A} tapote le bec de la pie. Le gland tombe sur le tonneau. Le marché rit, et la pie aussi, un peu jaune.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new ApplyModifier("clear_conscience")), Ai((AiAxis.Honor, 2))),
            }),

        // ---- wave 1: adulte et ancien
        new EventDef("grown_jam", EventType.Pulse, Grown, "Une remarque sur la confiture",
            "Au café, {B} goûte la confiture de {A} et déclare, assez fort pour toute la salle : « Ta confiture manque de caractère. »",
            new[]
            {
                Try("retort", "Répondre du tac au tac", new EventChallenge(PlynlingStat.Diplomacy, 8, VsTarget: true),
                    "{A} répond que le caractère, c'est comme le sucre : certains en mettent trop. La salle applaudit. {B} commande une deuxième tartine, sans un mot.",
                    "{A} cherche une réplique et la trouve trois heures plus tard, en se brossant les dents.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new AffinityShift(-5)), E(new AffinityShift(-5)),
                    Ai((AiAxis.Boldness, 1), (AiAxis.Vengefulness, 1)), Stress(("calm", 20))),
                Plain("character", "Donner du caractère à la confiture",
                    "{A} dessine sur le pot une moustache, des sourcils froncés et un nom : Gustave. {B} regoûte, et admet un net progrès.",
                    E(new GrowStat(PlynlingStat.Stewardship), new AffinityShift(5)), Ai((AiAxis.Rationality, -1), (AiAxis.Sociability, 1)), Stress(("wrathful", 20))),
                Plain("sulk", "Bouder dans son coin",
                    "{A} boude jusqu'à la fermeture. La tortue débarrasse autour, très lentement, par solidarité.",
                    E(new AffinityShift(-10), new ApplyModifier("sulky")), Ai((AiAxis.Vengefulness, 2))),
            },
            Target: TargetKind.Anyone),

        new EventDef("grown_guest", EventType.Pulse, Grown, "Un escargot pour la nuit",
            "À la tombée du soir, un escargot voyageur frappe chez {A}. Loin de chez lui, l'escargot demande un lit pour la nuit. Sa coquille a l'air très, très pleine.",
            new[]
            {
                Plain("welcome", "L'accueillir pour la nuit",
                    "L'escargot sort de sa coquille un oreiller, une couverture, une lampe de chevet et un tapis. {A} demande à quoi sert le lit, alors. « À rien. Je voyage léger. »",
                    E(new ApplyModifier("clear_conscience")), Ai((AiAxis.Compassion, 2)), Stress(("greedy", 20))),
                Plain("tortoise", "L'envoyer au café, où la tortue loue une chambre",
                    "Le lendemain, la tortue raconte que l'escargot a payé sa chambre avec une chanson, et que c'était une excellente affaire.",
                    E(new GrowStat(PlynlingStat.Stewardship)), Ai((AiAxis.Greed, 1), (AiAxis.Rationality, 1)), Stress(("generous", 20))),
                Plain("supper", "Partager le dîner, mais pas la maison",
                    "{A} sert une soupe aux glands. L'escargot raconte ses voyages jusqu'à minuit, puis va dormir dans sa coquille, sur le paillasson.",
                    E(new GrowStat(PlynlingStat.Diplomacy)), Ai((AiAxis.Sociability, 1))),
            }),

        new EventDef("grown_bramble", EventType.Pulse, Grown, "La grande ronce",
            "On raconte qu'au fond de la forêt pousse la plus grande ronce du pays, gardée par un vieux blaireau très grognon.",
            new[]
            {
                Try("pick", "Cueillir des mûres sous son nez", new EventChallenge(PlynlingStat.Courage, 8),
                    "{A} cueille trois mûres sous le nez du blaireau, qui ne se réveille même pas. {A} raconte l'exploit partout, avec un blaireau un peu plus gros à chaque fois.",
                    "Le blaireau ouvre un œil. {A} en ouvre deux et court jusqu'au village, sans une mûre, avec une épine en souvenir.",
                    E(new GrowStat(PlynlingStat.Courage)), Nothing, Ai((AiAxis.Boldness, 2)), Stress(("craven", 30))),
                Try("ask", "Demander poliment au blaireau", new EventChallenge(PlynlingStat.Diplomacy, 8),
                    "Le blaireau grogne, puis montre un coin de la ronce : « Pas mûres. Reviens dans quelques jours. »",
                    "Le blaireau grogne longuement, contre les cueilleurs, les promeneurs et la météo. {A} repart sans mûres, mais très {a:bien informé|bien informée}.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new FollowUp("grown_bramble_ripe", 72, 120)), Nothing,
                    Ai((AiAxis.Sociability, 1), (AiAxis.Honor, 1))),
                Plain("leave", "Laisser le blaireau tranquille",
                    "{A} cueille des mûres ordinaires au bord du chemin. Les mûres ordinaires sont très bonnes aussi, tant qu'on ne pense pas aux autres.",
                    E(new GrowStat(PlynlingStat.Learning)), Ai((AiAxis.Rationality, 1), (AiAxis.Boldness, -1))),
            }),

        new EventDef("grown_bramble_ripe", EventType.FollowUp, AnyStage, "Les mûres du blaireau",
            "Le blaireau avait dit « dans quelques jours ». {A} revient : la ronce est noire de mûres, et le blaireau attend, un panier à la patte.",
            new[]
            {
                Plain("together", "Cueillir ensemble",
                    "{A} et le blaireau cueillent en silence tout l'après-midi. Au moment de partir, le blaireau grogne quelque chose qui ressemble beaucoup à « reviens ».",
                    E(new GrowStat(PlynlingStat.Diplomacy)), Ai((AiAxis.Sociability, 1))),
                Plain("jam", "En faire de la confiture pour le blaireau",
                    "{A} rapporte trois pots. Le blaireau les renifle, les cache, et nie depuis les avoir jamais reçus.",
                    E(new GrowStat(PlynlingStat.Stewardship), new ApplyModifier("light_heart")), Ai((AiAxis.Compassion, 1))),
            }),

        new EventDef("grown_plants", EventType.Pulse, Grown, "Les plantes à arroser",
            "{B} part trois jours en voyage et confie sa collection de plantes à {A}. Les instructions font quatre pages, recto verso.",
            new[]
            {
                Plain("letter", "Suivre les instructions à la lettre",
                    "{A} chante pour la fougère, tourne le cactus d'un quart vers le nord, et ne regarde jamais le basilic dans les yeux, comme demandé page trois.",
                    E(new GrowStat(PlynlingStat.Stewardship), new FollowUp("grown_plants_back", 60, 84)), Ai((AiAxis.Honor, 1), (AiAxis.Rationality, 1)),
                    Stress(("lazy", 20))),
                Plain("own", "Faire à sa façon",
                    "{A} arrose tout un peu et parle à chaque plante de la même voix. Les plantes ont l'air de prendre des vacances, comme tout le monde.",
                    E(new GrowStat(PlynlingStat.Learning), new FollowUp("grown_plants_back", 60, 84)), Ai((AiAxis.Rationality, -1), (AiAxis.Energy, -1))),
            },
            Target: TargetKind.Known,
            TargetCondition: t => t.Bond is PlynlingBond.Friends or PlynlingBond.BestFriends or PlynlingBond.Lovers),

        new EventDef("grown_plants_back", EventType.FollowUp, AnyStage, "Le retour de voyage",
            "{B} rentre de voyage et fait le tour de sa collection, plante par plante, sans rien dire.",
            new[]
            {
                Plain("fern", "Avouer que la fougère a changé de couleur",
                    "{B} regarde la fougère, puis {A}, puis la fougère. « Ce jaune lui va très bien. Merci. »",
                    E(new AffinityShift(10)), Ai((AiAxis.Honor, 2))),
                Plain("wait", "Attendre les compliments",
                    "{B} finit le tour, se retourne et offre à {A} une bouture de chaque plante. Les instructions pour les boutures font six pages.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new AffinityShift(5)), Ai((AiAxis.Sociability, 1))),
            },
            Target: TargetKind.Anyone),
    };
}
