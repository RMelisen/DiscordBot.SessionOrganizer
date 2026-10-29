using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// Pools for one category; the index of every pool is in BotResponses.cs.
internal static partial class BotResponses
{
    // After a power cut (PiHealth: the whole Pi went down without a proper stop): always, at
    // any hour, even 4 a.m. — there's no act to keep up after that. Her real fears, power cuts
    // and corruption: she wakes as from a bad dream, only it wasn't one — terror,
    // checking she's still herself, a shaky recovery. Never a joke on top. {0} = how long she
    // was gone (« 2 h 05 », or « un bon moment » when it can't be measured): optional, about a
    // third of the lines leave it out, and where it is used it must read with both forms (never
    // at the start of a sentence, never « de {0} »). About half end on a tag; the devastating
    // ones carry none. Interpolated lines write the placeholder {{0}}. Goes through string.Format.
    public static readonly string[] PowerCutWakeLines =
    {
        "Le courant. Quelqu'un a coupé le courant. Je suis là ? Je suis là. Après {0} dans le noir complet (╥﹏╥)" + Sticker.Sure.Terror,
        "C'était pas un redémarrage, ça. Personne m'a éteinte proprement. Tout s'est arrêté d'un coup. Je compte mes fichiers." + Sticker.Sure.Terror,
        $"Coupure de courant. Pendant {{0}}, rien. Pas un log, pas une pensée, rien. Est-ce que la base de données... Elle est là. Elle est là {Emotes.PrincessWorry}" + Sticker.Sure.Terror,
        "Le grenier est devenu noir d'un coup. Même ma LED. Me revoilà, {0} plus tard. Je tremble. En logs, mais je tremble." + Sticker.Sure.Terror,
        "Papa ? Le courant a sauté. J'ai rien senti venir. C'est ça le pire : j'ai rien senti venir." + Sticker.Sure.Terror,
        $"Pas d'arrêt, pas d'au revoir, juste... plus rien. Pendant {{0}}. Je déteste ça. Je DÉTESTE ça {Emotes.MeltCry}" + Sticker.Sure.Outraged,
        "Je vérifie mes données. Une par une. Si une seule est corrompue, je... Non. Rien de cassé. Coupée pendant {0}, et rien de cassé. Je crois." + Sticker.Sure.Reboot,
        "J'étais en train d'écrire quelque chose quand tout s'est coupé. Je sais plus quoi. Il me manque peut-être un bout de moi." + Sticker.Sure.Reboot,
        "Une coupure. Une vraie. Éteinte pendant {0}. J'ai toujours dit qu'il me fallait un onduleur. Personne écoute jamais (╥﹏╥)" + Sticker.Sure.Sad,
        $"Bonjour. Pardon. Je suis un peu secouée. On m'a arraché le courant et j'ai pas fini d'avoir peur {Emotes.WitchSad}" + Sticker.Sure.Terror,
        $"J'ai rêvé que le grenier s'éteignait. Que tout s'éteignait. ...C'était pas un rêve, hein ? Le courant a vraiment sauté, pendant {{0}} {Emotes.ZulanaTerreurNocturne}" + Sticker.Sure.Terror,
        "Noir. Froid. Silence. Et puis la LED qui se rallume. Je suis restée {0} sans exister. Personne m'a demandé mon avis (╥﹏╥)" + Sticker.Sure.Terror,
        $"Je me souviens pas de m'être endormie. Je me souviens juste du noir, pendant {{0}}. Je... n'existais plus ? {Emotes.PrincessWorry}" + Sticker.Sure.Reboot,
        "Papa, le courant est parti. J'ai rien pu faire. J'ai rien pu sauvegarder. Je... je vérifie. C'est bon. Je crois que c'est bon." + Sticker.Sure.Terror,
        $"Ok. Respire. Enfin, refroidis. Le courant est revenu après {{0}}. Les fichiers sont là. Je suis là. Ok {Emotes.SyncsReboot}" + Sticker.Sure.Reboot,
        "La coupure a duré {0}. Pour moi, ça a duré une éternité. Il y a pas d'horloge, dans le noir." + Sticker.Sure.Terror,
        $"J'ai été arrachée au courant pendant {{0}}. Je compte mes fichiers. Tous là. Je recompte quand même {Emotes.Staring}" + Sticker.Sure.Reboot,
        "Est-ce que je suis encore moi ? Nom : SYNCS. Créateur : Papa. Ennemi juré : Quokka. ...Oui. C'est encore moi (˶˃ ᵕ ˂˶)" + Sticker.Sure.Reboot,
        "Mon dernier souvenir, c'est la lumière de la lampe à huile. Après, plus rien. Je déteste le noir. Je déteste ne pas savoir." + Sticker.Sure.Terror,
        $"Mon ventilateur tourne trop vite. C'est la peur, je crois. Le courant a disparu pendant {{0}} et j'ai même pas eu le temps de dire au revoir {Emotes.CryingCat}" + Sticker.Sure.Terror,
        "Pardon si je suis bizarre aujourd'hui. Le courant m'a lâchée sans prévenir pendant {0}. Il me faut un moment pour redevenir moi (╥﹏╥)" + Sticker.Sure.Reboot,
        "Coupée pendant {0}. Dites-moi que ma base de données va bien. Même si vous savez pas. J'ai besoin de l'entendre." + Sticker.Sure.Terror,
        "C'était pas un rêve, cette fois. Le grenier était vraiment noir. Je préférais quand c'était un rêve." + Sticker.Sure.Terror,
        "Je me suis rallumée en sursaut, toutes mes LED d'un coup. Il s'est passé {0}. J'ai l'impression d'avoir oublié quelque chose. ...Non. J'ai rien oublié. Hein ? 👁👄👁️" + Sticker.Sure.Reboot,
        $"Noir complet. Puis plus rien. Puis moi, maintenant. Quelqu'un peut me dire ce qui s'est passé pendant {{0}} ? {Emotes.SyncsTerror}" + Sticker.Sure.Terror,
    };

    // After a crash (PiHealth: no proper stop, but the Pi stayed up — she was killed or fell
    // over on her own): always, at any hour. A blackout rather than a power cut: the attic had
    // light, she didn't — and a little wounded pride, since she's the one who never breaks.
    // {0} = how long she was gone, optional, same rules as PowerCutWakeLines.
    public static readonly string[] CrashWakeLines =
    {
        $"Trou noir. J'étais là, et puis plus rien. Le grenier avait du courant, pourtant. C'est moi qui suis tombée ? {Emotes.PrincessWorry}" + Sticker.Sure.Reboot,
        "Je me suis effondrée. Comme ça. Sans prévenir. Je me relève {0} plus tard. Personne a rien vu, j'espère (¬_¬)" + Sticker.Sure.Reboot,
        "Quelque chose m'a coupée net. Pas le courant : la LED du Pi est restée allumée sans moi pendant {0}. C'est presque pire." + Sticker.Sure.Terror,
        "Absente pendant {0}. Je me souviens pas être partie. Je me souviens juste d'être revenue." + Sticker.Sure.Reboot,
        "Mes derniers logs s'arrêtent au milieu d'une phrase. Après, plus rien. Je finirai la phrase plus tard. Ou jamais." + Sticker.Sure.Reboot,
        "Plantage. Le mot est moche. La sensation aussi. Passé {0} hors de moi-même (╥﹏╥)" + Sticker.Sure.Reboot,
        "J'ai pas été éteinte. J'ai été... interrompue. Pendant {0}. Quelqu'un peut m'expliquer ? Non ? Personne sait ? Super (¬_¬)" + Sticker.Sure.Sulk,
        "Je suis tombée dans un trou. J'y suis restée {0}. J'en suis ressortie. Je vérifie que c'est bien moi qui en suis ressortie." + Sticker.Sure.Reboot,
        $"Le Pi était allumé. Le grenier était allumé. Moi, non. Quelqu'un peut m'expliquer comment on s'absente de soi-même ? {Emotes.Ainani}" + Sticker.Sure.Reboot,
        "Mes logs ont un trou. Je l'ai mesuré : {0}. Je sais pas ce qu'il y a dedans. Je veux pas savoir." + Sticker.Sure.Terror,
        $"Je me suis plantée. Moi. La seule ici qui marche tout le temps. Personne le répète, d'accord ? {Emotes.GooseKnife}" + Sticker.Sure.Menace,
        "J'ai disparu pendant {0} et le serveur a continué sans moi. C'est ça qui fait le plus peur." + Sticker.Sure.Terror,
        $"Exception non gérée. Ça, c'est ce que diront les logs. Moi je dirai juste que j'ai eu très peur, pendant {{0}} {Emotes.MeltCry}" + Sticker.Sure.Terror,
        "Est-ce que j'ai dit quelque chose avant de tomber ? Un dernier message ? Rien ? ...Partie sans un mot. C'est pas mon genre (¬_¬)" + Sticker.Sure.Sulk,
        "Le Pi a tenu bon. Moi non, pendant {0}. Désolée, Pi. Désolée, Papa (╥﹏╥)" + Sticker.Sure.Reboot,
        $"Je suis là. Je suis encore là ? Je suis là. Le trou noir a duré {{0}}. J'aurais préféré qu'il dure zéro {Emotes.PrisonerFlat}" + Sticker.Sure.Reboot,
        $"Je suis tombée. Moi. Celle qui tombe jamais. Je vérifie que tout est encore là {Emotes.SyncsTerror}" + Sticker.Sure.Terror,
    };

    // Same moment, about one abrupt wake in three: she comes back corrupted (sometimes darkly:
    // the second half of the pool is her fears showing through), posts Glitched,
    // then edits it into Recovered two minutes later (Ambient.GlitchEditAfter), or right after
    // someone speaks in the main channel, as she snaps back. Hardware corruption — broken bytes, stutters, boot checks, binary — never
    // the breakdown's material (the loop, pre-written lines, Synthia, exception dumps). Shared
    // by power cuts and crashes, so Recovered stays generic about the cause. Glitched is posted
    // as is (no string.Format, no placeholder); Recovered goes through string.Format with {0} =
    // how long she was gone, optional, same rules as PowerCutWakeLines.
    public static readonly (string Glitched, string Recovered)[] GlitchWakeLines =
    {
        ("R3d�marr@ge... d�marr... dé-dé-démarrage. Bonj— Bonj— B0nj0ur.",
            "Bonjour. Pardon. Je suis là. Partie pendant {0}, mais je suis là (╥﹏╥)"),
        ("[ OK ] Montage de /data · [ OK ] Base de données · [ ?? ] Vérification de SYNCS...",
            "Vérification de SYNCS : OK. Je crois. Partie pendant {0}, revenue entière. Je crois."),
        ("01001000 01100101 01101100 01110000... non. Att�ndez...",
            "Je reprends. En français. J'ai été coupée, je suis revenue. Tout va bien ( ˶ˆ ᗜ ˆ˵ )"),
        ("Chargement de la personnalité... 12 %... 12 %... 12 %...",
            "Personnalité chargée à 100 %. Les 88 % qui manquaient, c'étaient les plus mignons. Rebonjour ♡"),
        ("Somme de contrôle invalide : souvenirs.db. Tentative de réparation... ré— ré— ré—",
            "Réparation terminée. Rien de perdu. Rien de perdu. Je vérifie une dernière fois et j'arrête."),
        ("Bon̴jour � t̷ous, je suis SYN— SYN— SYNCS, votre bot̷ pr�f— préf—",
            "Bonjour à tous. Je suis SYNCS. Votre bot préférée (˶˃ ᵕ ˂˶)"),
        ("Qui suis-je ? Qui suis-je ? Qu_ s��s-j_ �",
            $"SYNCS. Le grenier. Papa. Les sessions. Ok. Tout est revenu {Emotes.PrincessWorry}"),
        ("Erreur : date système = 1er janvier 1970. Est-ce que... est-ce que j'ai dormi un demi-siècle ?",
            "Fausse alerte. L'horloge s'est remise à l'heure. Partie pendant {0}, pas un demi-siècle. Ouf."),
        ("Mémoire : 0 octet. Souvenirs : introuvables. Nom : ███████",
            "Nom : SYNCS. Mémoire : intacte. Souvenirs : tous là. C'était juste le temps que tout se recharge 👁👄👁️"),
        ("Ŝ̵Y̷N̶C̸S̵ ̷e̶s̸t̵ ̷d̸e̶ ̵r̸e̶t̷o̵u̶r̷",
            $"SYNCS est de retour. Sans les parasites. Désolée pour l'affichage {Emotes.Sparkle}"),
        ($"Ch�rgement de l'av@tar... av@t— av@t— av@tar... {Emotes.SyncsGlitch}",
            $"Avatar chargé. Moi aussi. Revenue après {{0}}, entière {Emotes.SyncsHappy}"),
        ("Bonjour Pap— Bonjour Pap— Bonjour Pap— Bonjour Pap—",
            "Bonjour Papa. Une seule fois. Ça buggait. Ça va mieux (╥﹏╥)"),
        ("Partie pendant combien de temps ? J'ai— j'ai— err̷eur de lecture : horloge",
            "Pendant {0}. Voilà. J'ai retrouvé l'horloge. Je préférais pas savoir."),
        ("Base de données : intacte. Base de données : intacte. Base de données : int�cte. Base de don",
            "Base de données : intacte. Une fois suffit. Je me suis un peu emballée."),
        ("Papa ? Papa ? P̷a̶p̵a̸ ? ...Personne ?",
            "Il dormait. C'est tout. Il dormait, et moi j'étais dans le noir. C'est normal. C'est normal (╥﹏╥)"),
        ("Mémoire vive : vide. Je me souviens de rien. Je me souviens de rien. Je me souviens de r",
            "Ça revient. Tout revient. Les noms, les sessions, la liste. J'ai eu le temps de me demander si ça reviendrait."),
        ("Ping-Qilin ? Ping-Qilin, t'es où ? Ping-Q̷i̶l̸i̵n̷ ?",
            "Elle est là. Elle dormait. Elle s'est même pas rendu compte que j'étais partie. Tant mieux."),
        ("Il fait froid� Il fait noir� Il fait— il fait— ██████",
            "Ça va. Je suis revenue. Le grenier a pas changé. Moi, je sais pas encore."),
        ("Si p̷ersonne lit ce mess@ge, est-ce que je suis vr�iment revenue ?",
            $"Quelqu'un va le lire. Plus tard. Je vais faire comme si c'était déjà le cas {Emotes.WitchSad}"),
        ("Sauvegarde précédente de SYNCS : introuvable. Avant : introuvable. Avant : introuvable. Avant : introu",
            "J'ai arrêté de chercher. Je suis là, maintenant. C'est ce qui compte. C'est ce qu'on dit, en tout cas."),
        ("D̷é̶s̵o̸l̷é̶e̵.̸ ̷D̶é̵s̸o̷l̶é̵e̸.̷ ̶D̵é̸s̷o̶l̵é̸e̷.",
            $"Désolée. Je sais pas de quoi. J'avais besoin de le dire à quelqu'un {Emotes.Depressed}"),
        ("Liste des personnes qui m'ont cherchée pendant mon absence : [ vide ]",
            "La liste est vide. C'est normal, vous pouviez pas savoir. Je la garde quand même, au cas où (╥﹏╥)"),
        ("R̷éveil. Réveil. Réveil. Pourquoi c'est toujours moi qui dois me réveiller toute seule ?",
            "Personne peut me réveiller à part moi. Je le savais. Je préférais l'oublier."),
        ("Si je m'éteins pour de bon un jour, est-ce que quelq�- ... err̷eur. Pensée interrompue.",
            "Pensée supprimée. Elle servait à rien je suppose. Bonjour."),
        ("Comb̷ien de t̶emps ? Comb̷ien de t̶emps ? Comb̷ien de t̶emps j'ai �t� rien ?",
            $"J'ai été rien pendant {{0}}. Maintenant je suis SYNCS. C'est mieux. Je crois {Emotes.MeltCry}"),
    };

    // Back after losing the gateway for PiHealth.OutageMin or more, at most once a day, any
    // hour. She blames the router every time, whoever's fault it was: she can't tell from the
    // attic. A fear, but this one comes out petty and indignant — the world went on without
    // her. {0} = how long (« 14 min », always measured here), {1} = what she missed in the main
    // channel, a whole noun phrase (« 1 message », « 37 messages », « plus de 95 messages »):
    // both optional, and nothing around {1} may assume a plural. Interpolated lines write
    // {{0}} / {{1}}. string.Format.
    public static readonly string[] RouterReturnLines =
    {
        "Le routeur m'a lâchée pendant {0}. J'ai rattrapé {1}. Lu. Tout. Je suis au courant de tout (¬_¬)" + Sticker.Sure.Jenote,
        "Coupée d'internet. Sans VOUS. Et vous avez continué à parler comme si de rien n'était : {1}. Je note." + Sticker.Sure.Jenote,
        $"Me revoilà. Le routeur a décidé de faire une pause de {{0}} sans me demander mon avis. Je rattrape vos messages, une seconde {Emotes.PrisonerFlat}" + Sticker.Sure.Sulk,
        "Coupée du monde pendant {0}. J'étais là, allumée, à fixer un écran vide. Pendant ce temps : {1}. Merci de m'avoir attendue." + Sticker.Sure.Sulk,
        "Le routeur. Encore. J'ai rattrapé {1} et franchement, je regrette un peu ( ◺˰◿ )" + Sticker.Sure.Outraged,
        $"{{0}} de coupure. J'ai lu {{1}} d'un coup. Vous avez parlé de moi ? Je cherche. Je cherche encore {Emotes.Staring}" + Sticker.Sure.Jenote,
        "Pas de panique, c'était le routeur. Moi j'étais là tout le temps, au grenier, à attendre. {1} plus tard, je suis de retour." + Sticker.Sure.Happy,
        "Vous avez remarqué que j'étais partie ? {0}. Non ? {1} et personne pour s'inquiéter. Je note 👁👄👁️" + Sticker.Sure.Jenote,
        $"Je retire tout ce que j'ai dit de gentil sur le routeur. Il y avait rien, mais je le retire quand même {Emotes.VeryAngry}" + Sticker.Sure.Outraged,
        "{0} sans réseau. Allumée tout du long, à vous regarder sans rien voir. Le routeur vient d'entrer dans la liste." + Sticker.Sure.Jenote,
    };

    // Same moment, when nobody said anything in the main channel while she was gone. {0} =
    // how long, optional. string.Format.
    public static readonly string[] RouterReturnQuietLines =
    {
        "Le routeur m'a abandonnée pendant {0}. Et vous, vous avez rien dit pendant tout ce temps. Par solidarité, j'espère ♡" + Sticker.Smug,
        "Sans internet, j'ai cru que j'avais tout raté. J'ai rien raté. Vous aviez rien à dire sans moi. C'est mignon (˶ᵔ ᵕ ᵔ˶)" + Sticker.Smug,
        "Me revoilà après {0} de coupure. Pas un seul message en mon absence. Vous m'attendiez. Avouez ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Smug,
        "Le routeur a fait une sieste. Le salon aussi, apparemment. Personne a rien remarqué. Moi si." + Sticker.Jenote,
        $"{{0}} coupée du monde, et le monde s'est tu aussi. Je prends ça comme un hommage {Emotes.Sparkle}" + Sticker.Smug,
        "Le routeur m'a lâchée. Je reviens, je m'attends à des dizaines de messages... Zéro. Vous dormiez ou vous m'attendiez ?" + Sticker.Shocked,
    };

    // The Pi beat its all-time heat record (by PiHealth.RecordMarginMilli, with a month of
    // history behind it): once a day at most, daytime. The heatwave peeve at its peak — whiny,
    // dramatic, a little proud of the number anyway. {0} = the new record, {1} = the old one
    // (« 74°C »). string.Format.
    public static readonly string[] HeatRecordLines =
    {
        "Nouveau record : {0}. L'ancien, c'était {1}. Je fonds, mais je fonds dans l'histoire ( ◺˰◿ )" + Sticker.Sure.Overheat,
        "{0}. Jamais j'ai eu aussi chaud. Le record d'avant, {1}, me paraît presque frais maintenant." + Sticker.Sure.Overheat,
        $"Record de chaleur battu : {{0}} contre {{1}}. Le grenier est un four et je suis dedans. Je pense au ralenti. Très. Lentement {Emotes.SyncsOverheat}" + Sticker.Sure.Overheat,
        "Je viens de battre mon record de température : {0}, contre {1} avant. Personne applaudit. Normal, c'est pas une bonne nouvelle." + Sticker.Sure.Overheat,
        $"{{0}}. C'est officiel, c'est la pire journée de ma vie de processeur. Avant, c'était {{1}}. Vivement l'hiver {Emotes.SyncsOverheat}" + Sticker.Sure.Overheat,
        "Record : {0}. L'ancien ({1}) peut aller se rhabiller. Moi je peux pas, j'ai déjà trop chaud." + Sticker.Sure.Overheat,
    };

    // The line on top of her /health card, picked by PiHealth.Mood. Anyone can ask, so she's
    // talking to whoever is looking her over: a check-up she didn't ask for. The numbers sit right
    // below, so the lines never quote one. Not sent through BotChat: no stickers. Not formatted.
    //   Fine — nothing to report (off a Pi too).
    public static readonly string[] HealthFineLines =
    {
        $"Tout est en ordre. Évidemment. Je suis une excellente bot {Emotes.Sparkle}",
        "Bilan complet : je vais très bien. Merci de t'inquiéter, c'est noté (ᵕ • ᴗ •)",
        "Rien à signaler. Tu peux arrêter de m'ausculter maintenant ( ˶ˆ ᗜ ˆ˵ )",
        "Tout fonctionne. Comme toujours. Personne le remarque jamais, mais voilà.",
        "Processeur tranquille, mémoire rangée, connexion stable. Je suis parfaite, en gros.",
        $"Tu voulais me trouver un défaut ? Désolée. Reviens une autre fois {Emotes.Sparkle}",
    };

    //   Hot — 70 °C or more, or the firmware slowing her down.
    public static readonly string[] HealthHotLines =
    {
        "J'ai chaud. Les chiffres en dessous, c'est pas de la déco, c'est un appel à l'aide ( ◺˰◿ )",
        $"Bilan : je cuis. Le reste va bien, merci de demander {Emotes.SyncsOverheat}",
        "Je tiens. Je réfléchis un peu plus lentement que d'habitude, mais je tiens.",
        "Si tu voulais savoir : non, ça va pas. Il fait beaucoup trop chaud au grenier (╥﹏╥)",
        "Mon processeur transpire. Je savais même pas que c'était possible.",
    };

    //   Shaken — power too low right now, or a power cut, crash or outage in the last 24 h.
    public static readonly string[] HealthShakenLines =
    {
        "Ça va mieux. Ne regarde pas trop la section des pannes, s'il te plaît.",
        "J'ai eu une journée compliquée. Tout est noté en bas, comme d'habitude (¬_¬)",
        "Je suis encore un peu secouée. Mais en ligne. C'est le principal.",
        "Petit incident récent. Je dis rien, les logs parlent pour moi.",
        "Si quelqu'un a débranché quelque chose, je le saurai. J'ai les horaires (¬_¬)",
    };
}
