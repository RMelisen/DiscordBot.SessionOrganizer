using Discord;
using Discord.Interactions;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Commands;

public class HelpModule : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("help", "Comment utiliser le bot d'organisation de sessions")]
    public async Task HelpAsync()
    {
        var embed = new EmbedBuilder()
            .WithTitle("Project S.Y.N.C.S. — Aide")
            .WithDescription(
                "Organise des sessions de jeu, des activités ou des soirées film, " +
                "et laisse les autres s'inscrire en un clic.")
            .WithColor(Color.Blue)
            .AddField("Commandes",
                "**`/schedule create`** — Planifie une nouvelle session (assistant en 4 étapes : type, jour, heure, détails).\n" +
                "**`/schedule list`** — Affiche les sessions actives du serveur. Tu peux republier une carte dans le salon courant.\n" +
                "**`/schedule edit <id>`** — Modifie une session que tu as organisée (titre, date, heure, participants max).\n" +
                "**`/schedule cancel <id>`** — Annule une session que tu as organisée.\n" +
                "**`/poll create`** — Propose plusieurs créneaux et laisse chacun voter pour ses disponibilités.\n" +
                "**`/poll list`** — Affiche les sondages actifs du serveur. Tu peux en republier un dans le salon courant.\n" +
                "**`/poll delete <id>`** — Supprime un sondage que tu as créé.\n" +
                "**`/vote create`** — Propose plusieurs options en texte (jeux, films…) et laisse chacun voter.\n" +
                "**`/vote list`** — Affiche les votes actifs du serveur. Tu peux en republier un dans le salon courant.\n" +
                "**`/vote delete <id>`** — Supprime un vote que tu as créé.\n" +
                "**`/emotestats`** — Affiche le classement des emotes les plus utilisées du serveur (écrites et en réaction).\n" +
                "**`/help`** — Affiche ce message.")
            .AddField("Créer une session — pas à pas",
                "Lance **`/schedule create`**. Un assistant privé te guide :\n" +
                "**1.** *Type* — choisis la catégorie : 🎮 Jeu, 🧑‍🤝‍🧑 Activité, 🎬 Film ou ✨ Autre.\n" +
                "**2.** *Jour* — sélectionne le jour (Aujourd'hui, Demain, puis les dates des 25 prochains jours).\n" +
                $"**3.** *Heure* — choisis l'heure, de 00h à 23h.\n" +
                "**4.** *Minutes* — choisis :00, :15, :30 ou :45 (pour plus de précision, tu peux modifier l'heure exacte directement sur la carte).\n" +
                "**5.** *Détails* — un formulaire s'ouvre : saisis le **nom** de la session et, en option, " +
                "le **nombre de participants max** ; laisse vide pour un nombre illimité).\n\n" +
                "Le bouton **Retour** permet de revenir à l'étape précédente. " +
                "Une fois le formulaire validé, la carte de session est publiée dans le salon.")
            .AddField("Boutons d'une session",
                "✅ **Rejoindre** — Tu participes.\n" +
                "🔄 **Peut-être** — Tu n'es pas sûr.\n" +
                "✖️ **Refuser** — Tu ne participes pas.\n" +
                "✏️ **Modifier** — Réservé à l'organisateur.\n" +
                "🗑️ **Annuler** — Réservé à l'organisateur.")
            .AddField("Modifier une session",
                "Le bouton **✏️ Modifier** (ou `/schedule edit <id>`) ouvre un formulaire, réservé à l'organisateur, " +
                "pour ajuster :\n" +
                "• le **nom** de la session ;\n" +
                "• la **date** (format `AAAA-MM-JJ`, ex. `2026-06-20`) ;\n" +
                "• l'**heure** (format `HH:mm`, ex. `20:30`) — c'est ici que tu fixes une heure précise ;\n" +
                "• le **nombre de participants max** (0 = illimité).\n" +
                "La carte se met à jour automatiquement après validation.")
            .AddField("Trouver un créneau — sondage",
                "Lance **`/poll create`**, donne un titre, puis ajoute des créneaux un par un " +
                "(jour, heure, minutes) avec le bouton **➕ Ajouter un créneau** (jusqu'à 10), " +
                "et termine avec **✅ Terminer**.\n" +
                "Chacun clique ensuite **tous** les créneaux qui lui conviennent (plusieurs choix possibles). " +
                "L'organisateur clôture avec **🔒 Clôturer** : le créneau le plus voté est mis en avant. " +
                "Une fois clôturé, le bouton **🗓️ Créer une session** transforme directement le créneau " +
                "retenu en session (en cas d'égalité, tu choisis lequel).")
            .AddField("Choisir une option — vote",
                "Lance **`/vote create`**, donne un titre, puis ajoute des options en texte une par une " +
                "(jeux, films, activités…) avec le bouton **➕ Ajouter une option** (jusqu'à 10), " +
                "et termine avec **✅ Terminer**.\n" +
                "Chacun clique ensuite **toutes** les options qui lui conviennent (plusieurs choix possibles). " +
                "L'organisateur clôture avec **🔒 Clôturer** : l'option la plus votée est mise en avant.")
            .AddField("Clôture automatique",
                "Les sondages et votes restés ouverts se **clôturent automatiquement au bout de 2 jours**.")
            .AddField("Rappels",
                "Les participants inscrits reçoivent un rappel en message privé avant le début " +
                "de la session.")
            .AddField("Cycle de vie d'une session",
                "À l'heure prévue, la carte passe en **🔴 EN COURS** (les boutons sont désactivés), " +
                "puis en **✅ TERMINÉE** environ 2 h plus tard.\n" +
                "Si une session est **annulée**, les participants inscrits (Rejoindre / Peut-être) " +
                "reçoivent un message privé.")
            .AddField("Bon à savoir",
                $"• L'**ID** d'une session est affiché en pied de carte, à utiliser avec `/edit` et `/cancel`.\n")
            .WithFooter($"Project S.Y.N.C.S. v{AppInfo.Version}")
            .Build();

        await RespondAsync(embed: embed, ephemeral: true);
    }
}
