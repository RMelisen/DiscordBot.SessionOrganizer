using Discord;
using Discord.Interactions;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Commands;

// The in-Discord usage guide. Hand-maintained: a new user-facing command means editing
// this and README.md, neither of which is generated. Owner-only commands are
// deliberately absent; staff ones are listed, since staff still have to discover them.
//
// **Discord's embed caps are the reason this is one static builder rather than inline
// in the handler.** An embed field value may be 1024 characters and the whole embed
// 6000, counting the title, description, every field name and value, and the footer.
// Exceeding either throws inside EmbedBuilder.Build() — at *send* time, so the command
// simply fails with nothing in the logs pointing at the length. That is exactly what
// happened: the "Autres" field grew past 1024 and the embed past 6000 as commands were
// added, and /help was dead for several releases before anyone noticed. Splitting it
// out makes it constructible without a gateway, which is what lets the scratch harness
// assert both caps.
//
// So: keep every field short, and split a section rather than letting one grow. There
// is plenty of room in the 25-field limit — 11 are used.
public class HelpModule : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("help", "Comment utiliser le bot d'organisation de sessions")]
    public Task HelpAsync() => RespondAsync(embed: BuildEmbed(), ephemeral: true);

    /// <summary>
    /// The whole guide. Static and Context-free so its size can be checked without a
    /// gateway connection — see the note above for why that matters.
    /// </summary>
    public static Embed BuildEmbed() =>
        new EmbedBuilder()
            .WithTitle("Project S.Y.N.C.S. — Aide")
            .WithDescription(
                "Organise des sessions de jeu, des activités ou des soirées film, " +
                "et laisse les autres s'inscrire en un clic.")
            .WithColor(Color.Blue)
            .AddField("Commandes — Sessions",
                "**`/schedule create`** — Planifie une session (assistant en 4 étapes).\n" +
                "**`/schedule list`** — Les sessions actives. Tu peux en republier une ici.\n" +
                "**`/schedule edit <id>`** — Modifie une session que tu as organisée.\n" +
                "**`/schedule cancel <id>`** — Annule une session que tu as organisée.")
            .AddField("Commandes — Sondages & votes",
                "**`/poll create`** — Propose des créneaux, chacun vote pour ses disponibilités.\n" +
                "**`/vote create`** — Pareil, mais avec des options en texte (jeux, films…).\n" +
                "**`/poll list`** · **`/vote list`** — Les sondages et votes actifs.\n" +
                "**`/poll delete <id>`** · **`/vote delete <id>`** — Supprime le tien.")
            .AddField("Commandes — Tirages au sort",
                "**`/giveaway create`** — Lance un tirage : le **lot**, une **durée** (10 min à 7 jours), " +
                "et en option une **description** et un **nombre de gagnants** (1 par défaut, 10 max).\n" +
                "**`/giveaway list`** · **`/giveaway delete <id>`** — Les tirages en cours, et supprimer le tien.\n" +
                "**🎉 Participer** t'inscrit, **✖️ Ne plus participer** te retire. " +
                "À la fin, je tire au sort et j'annonce toute seule.")
            .AddField("Commandes — Niveaux & statistiques",
                "**`/level [user]`** — Ta carte : niveau, progression et rang.\n" +
                "**`/leaderboard`** — Le classement, 5 par page. Trois vues (**Niveaux**, **Réactions**, " +
                "**Vocal**) et trois filtres (**depuis toujours**, **30 jours**, **7 jours**).\n" +
                "**`/emotestats`** — Les emotes les plus utilisées, écrites et en réaction.\n" +
                "**`/yesno [question]`** — Tu hésites ? Je tranche. Pile ou face, mais avec du caractère.\n" +
                "**`/goodbot`** — Qui m'a dit *good bot* (ou l'inverse). Un 👍 ou un 👎 sur un de " +
                "mes messages compte pareil.")
            .AddField("Comment on gagne de l'XP",
                "En parlant et en réagissant, et un peu plus en s'adressant à moi.\n" +
                "En vocal aussi : il faut être **accompagné**, **micro ouvert** et **pas en sourdine** " +
                "— quelqu'un de muet ne compte pas comme de la compagnie, et le salon AFK ne rapporte rien.\n" +
                "Le gain en vocal **diminue au fil de la journée** (plein tarif la première heure, " +
                "puis de moins en moins) et repart à zéro chaque nuit.")
            .AddField("Commandes — Mur de la honte",
                "**`/shame`** — Quatre titres : **Le Malfaisant**, que je décerne toute seule à qui est " +
                "méchant (un point par personne visée), **Le Banni**, que vous votez, **Le Perfide**, " +
                "pour ceux qui parlent aux *autres* bots — je vois qui leur répond et qui utilise leurs " +
                "commandes — et **L'Hystérique**, pour ceux qui écrivent des phrases entières en " +
                "MAJUSCULES.\n" +
                "**`/shame user:@quelqu'un`** — Dénonce quelqu'un. **Réservé au staff**, et " +
                "**2 votes maximum par personne visée et par jour**.")
            .AddField("Commandes — Staff & aide",
                "**`/addxp`** · **`/removexp`** — Ajuster l'XP de quelqu'un.\n" +
                "**`/config`** — Le rôle autorisé à voter avec `/shame`, et les salons où rien ne " +
                "compte. **`/config show`** affiche la configuration actuelle.\n" +
                "**`/help`** — Affiche ce message.")
            .AddField("Créer une session — pas à pas",
                "Lance **`/schedule create`**. Un assistant privé te guide :\n" +
                "**1.** *Type* — 🎮 Jeu, 🧑‍🤝‍🧑 Activité, 🎬 Film ou ✨ Autre.\n" +
                "**2.** *Jour* — Aujourd'hui, Demain, puis les 25 prochains jours.\n" +
                "**3.** *Heure* — de 00h à 23h. **4.** *Minutes* — :00, :15, :30 ou :45.\n" +
                "**5.** *Détails* — le **nom** et, en option, le **nombre de participants max** " +
                "(vide = illimité).\n" +
                "**Retour** revient à l'étape précédente. La carte est publiée une fois validée.")
            .AddField("Boutons d'une session",
                "✅ **Rejoindre** · 🔄 **Peut-être** · ✖️ **Refuser**\n" +
                "✏️ **Modifier** · 🗑️ **Annuler** — réservés à l'organisateur.\n" +
                "📅 **Créer/Retirer l'événement Discord** — réservé à l'organisateur : ajoute ou retire " +
                "un événement dans l'onglet **Événements**, synchronisé avec la session.\n" +
                "**✏️ Modifier** (ou `/schedule edit <id>`) ouvre un formulaire : nom, date " +
                "(`AAAA-MM-JJ`), heure (`HH:mm` — c'est ici qu'on fixe une heure précise) et " +
                "participants max (0 = illimité).")
            .AddField("Sondages & votes — mode d'emploi",
                "Après **`/poll create`** ou **`/vote create`**, donne un titre puis ajoute tes " +
                "créneaux ou tes options un par un (jusqu'à 10), et termine avec **✅ Terminer**.\n" +
                "Chacun clique ensuite **tout** ce qui lui convient — plusieurs choix possibles.\n" +
                "L'organisateur clôture avec **🔒 Clôturer** : le plus voté est mis en avant. " +
                "Pour un sondage, **🗓️ Créer une session** transforme le créneau retenu en session.")
            .AddField("Bon à savoir",
                "Les sondages et votes restés ouverts se **clôturent automatiquement au bout de 2 jours**.\n" +
                "Les inscrits reçoivent un **rappel en message privé** avant le début de la session.\n" +
                "À l'heure prévue la carte passe en **🔴 EN COURS**, puis en **✅ TERMINÉE** environ " +
                "2 h plus tard. Une session **annulée** prévient les inscrits par message privé.\n" +
                "L'**ID** d'une session est en pied de carte, pour `/schedule edit` et `/schedule cancel`.")
            .WithFooter($"Project S.Y.N.C.S. v{AppInfo.Version}")
            .Build();
}
