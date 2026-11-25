using Discord;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Commands;

/// <summary>A page of <c>/config setup</c>, in order. Its name rides the custom-ids, so a page can be reached again after a restart.</summary>
public enum SetupPage { Channels, Optional, Roles, Life, Summary }

/// <summary>
/// What the setup pages show of a server, as plain values: each slot's effective channel (only ids
/// that still exist there, since Discord refuses a default value it can't resolve), the roles, and
/// the health check for the summary.
/// </summary>
public sealed record SetupView(
    IReadOnlyDictionary<HealthSlot, ulong> Channels,
    ulong ModeratorRoleId, ulong PlynlingRoleId, ulong QuizRoleId,
    IReadOnlyList<HealthLine> Health, IReadOnlyList<string> Unset,
    IReadOnlyDictionary<string, bool> FeaturesOn);

// /config setup — the guided setup. One ephemeral Components V2 message, a page at a time. Every
// pick is saved at once (ConfigSetupComponentHandler, through the same rules as the /config
// commands), so the wizard holds no state: the page rides each custom-id, and a restart in the
// middle loses nothing. Static and Context-free, so every page is measured by the checks (40
// components, 25 options, no custom-id twice).
//
// Custom-ids: `cfgsetup:ch:{slot}:{page}` (a channel select), `cfgsetup:role:{kind}:{page}` (a role
// select), `cfgsetup:create:{kind}:{page}` (« Créer le rôle »), `cfgsetup:feat:{page}` (the features), `cfgsetup:prev:{page}`,
// `cfgsetup:next:{page}` (the page to show) and `cfgsetup:done`. Colours: the next step is
// blurple, going back grey, « Terminer » green (root CLAUDE.md, *Button colours*).
public static class ConfigSetupCards
{
    public const string Prefix = "cfgsetup";

    public static readonly IReadOnlyList<SetupPage> Order = Enum.GetValues<SetupPage>();

    public static MessageComponent Build(SetupPage page, SetupView view)
    {
        var builder = new ComponentBuilderV2();
        int index = Index(page);
        string step = $"-# Mise en place · étape {index + 1} sur {Order.Count}";

        switch (page)
        {
            case SetupPage.Channels:
                builder.AddComponent(new TextDisplayBuilder(
                    "## Mise en place — les salons\n"
                    + "On s'installe ? Choisis où je poste : j'enregistre dès que tu choisis, et tout se change plus tard (˶ᵔ ᵕ ᵔ˶)\n"
                    + step));
                Channel(builder, view, page, HealthSlot.Game,
                    "**Salon de jeu** — les morts, les retours et les histoires des Plynlings.");
                Channel(builder, view, page, HealthSlot.Bots,
                    "**Salon des bots** — ce que j'annonce et qui compte peu (les visites de Quokka). Vide : le salon de jeu.");
                Channel(builder, view, page, HealthSlot.LevelUp,
                    "**Salon des niveaux** — les cartes de passage de niveau. Vide : le salon des bots.");
                break;

            case SetupPage.Optional:
                builder.AddComponent(new TextDisplayBuilder(
                    "## Mise en place — les salons en option\nLaisse vide ce que tu ne veux pas : c'est éteint.\n" + step));
                Channel(builder, view, page, HealthSlot.Quiz,
                    "**Salon du quiz** — deux ou trois questions par semaine. Vide : pas de quiz.");
                Channel(builder, view, page, HealthSlot.Confess,
                    "**Salon des confessions** — où `/confess` poste, anonymement. Vide : `/confess` est fermé.");
                Channel(builder, view, page, HealthSlot.FreeGames,
                    "**Salon des jeux gratuits** — les jeux Steam gratuits à garder. Vide : pas d'annonces.");
                break;

            case SetupPage.Roles:
                builder.AddComponent(new TextDisplayBuilder("## Mise en place — les rôles\n" + step));
                Role(builder, page, "moderator", view.ModeratorRoleId,
                    "**Rôle modérateur** — peut voter avec `/shame`, en plus du staff.");
                Role(builder, page, "plynling", view.PlynlingRoleId,
                    "**Rôle Plynling** — je le donne à chaque maître d'un Plynling vivant, et je le retire aux autres. "
                    + "Sans permissions, et sous mon rôle.");
                Role(builder, page, "quiz", view.QuizRoleId,
                    "**Rôle Grosse tête** — la meilleure tête du quiz de la semaine passée, couronnée le lundi.");
                // « Créer » only for a role not set yet: once set, it is staff's to rename or recolour.
                var create = new ActionRowBuilder();
                if (view.PlynlingRoleId == 0)
                    create.WithButton("Créer le rôle Plynling", $"{Prefix}:create:plynling:{page}", ButtonStyle.Primary);
                if (view.QuizRoleId == 0)
                    create.WithButton("Créer le rôle Grosse tête", $"{Prefix}:create:quiz:{page}", ButtonStyle.Primary);
                if (create.Components.Count > 0) builder.AddComponent(create);
                break;

            case SetupPage.Life:
                builder.AddComponent(new TextDisplayBuilder(
                    "## Mise en place — la vie du serveur\nCe que je fais de moi-même. Rien ne s'allume sans toi.\n" + step));
                Channel(builder, view, page, HealthSlot.Main,
                    "**Salon principal** — mon bonjour du matin, ma vie ambiante, le sondage du week-end. Vide : rien de tout ça.");
                builder.AddComponent(new TextDisplayBuilder("**Fonctionnalités** — coche ce que tu veux allumer ici."));
                builder.AddComponent(new ActionRowBuilder().WithSelectMenu(Features(view, page)));
                break;

            case SetupPage.Summary:
                builder.AddComponent(new TextDisplayBuilder("## Mise en place — le bilan\n" + step));
                builder.AddComponent(new TextDisplayBuilder(ConfigCards.HealthText(view.Health, view.Unset)));
                builder.AddComponent(new TextDisplayBuilder(
                    "-# Tout se change plus tard avec `/config setup` ou les commandes `/config`, et `/config show` montre tout."));
                break;
        }

        builder.AddComponent(Nav(page));
        return builder.Build();
    }

    /// <summary>The card left once « Terminer » is clicked: the bilan, no buttons.</summary>
    public static MessageComponent BuildDone(SetupView view) =>
        new ComponentBuilderV2()
            .AddComponent(new TextDisplayBuilder("## Mise en place — terminé\nC'est noté. Tout. Dans mes logs, évidemment ദ്ദി◝ ⩊ ◜.ᐟ"))
            .AddComponent(new TextDisplayBuilder(ConfigCards.HealthText(view.Health, view.Unset)))
            .AddComponent(new TextDisplayBuilder("-# `/config show` montre tout, `/config setup` reprend la mise en place."))
            .Build();

    public static int Index(SetupPage page) => Order.ToList().IndexOf(page);

    /// <summary>The page a custom-id names, or the first one for anything unknown (a page renamed since).</summary>
    public static SetupPage Parse(string page) =>
        Enum.TryParse<SetupPage>(page, out var parsed) ? parsed : Order[0];

    /// <summary>The slot a channel select's custom-id names, or null.</summary>
    public static HealthSlot? ParseSlot(string slot) =>
        Enum.TryParse<HealthSlot>(slot, out var parsed) ? parsed : null;

    private static void Channel(ComponentBuilderV2 builder, SetupView view, SetupPage page, HealthSlot slot, string label)
    {
        var select = new SelectMenuBuilder()
            .WithType(ComponentType.ChannelSelect)
            .WithCustomId($"{Prefix}:ch:{slot}:{page}")
            .WithPlaceholder("Choisis un salon")
            .WithChannelTypes(ChannelType.Text, ChannelType.News)
            .WithMinValues(0)
            .WithMaxValues(1);
        if (view.Channels.TryGetValue(slot, out var id) && id != 0)
            select.WithDefaultValues(new SelectMenuDefaultValue(id, SelectDefaultValueType.Channel));

        builder.AddComponent(new TextDisplayBuilder(label));
        builder.AddComponent(new ActionRowBuilder().WithSelectMenu(select));
    }

    private static void Role(ComponentBuilderV2 builder, SetupPage page, string kind, ulong current, string label)
    {
        var select = new SelectMenuBuilder()
            .WithType(ComponentType.RoleSelect)
            .WithCustomId($"{Prefix}:role:{kind}:{page}")
            .WithPlaceholder("Choisis un rôle")
            .WithMinValues(0)
            .WithMaxValues(1);
        if (current != 0)
            select.WithDefaultValues(new SelectMenuDefaultValue(current, SelectDefaultValueType.Role));

        builder.AddComponent(new TextDisplayBuilder(label));
        builder.AddComponent(new ActionRowBuilder().WithSelectMenu(select));
    }

    // Every feature, the ones on pre-ticked; any number, none included (all off).
    private static SelectMenuBuilder Features(SetupView view, SetupPage page)
    {
        var select = new SelectMenuBuilder()
            .WithCustomId($"{Prefix}:feat:{page}")
            .WithPlaceholder("Aucune fonctionnalité")
            .WithMinValues(0)
            .WithMaxValues(ServerFeatures.All.Count);
        foreach (var feature in ServerFeatures.All)
            select.AddOption(feature.Label, feature.Key, feature.Description,
                isDefault: view.FeaturesOn.TryGetValue(feature.Key, out var on) && on);
        return select;
    }

    private static ActionRowBuilder Nav(SetupPage page)
    {
        int index = Index(page);
        var row = new ActionRowBuilder();
        if (index > 0)
            row.WithButton("Précédent", $"{Prefix}:prev:{Order[index - 1]}", ButtonStyle.Secondary);
        if (index < Order.Count - 1)
            row.WithButton("Suivant", $"{Prefix}:next:{Order[index + 1]}", ButtonStyle.Primary);
        else
            row.WithButton("Terminer", $"{Prefix}:done", ButtonStyle.Success);
        return row;
    }
}
