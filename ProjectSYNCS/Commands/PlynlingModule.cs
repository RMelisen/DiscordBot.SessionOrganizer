using Discord;
using Discord.Interactions;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Commands;

// /plynling — adopting, looking after and (for staff) managing Plynlings. Guild-only:
// every query is scoped to Context.Guild.Id.
//
// No DeferAsync anywhere: every action is one or two row reads and one write, well inside
// Discord's 3 s, and not deferring is what lets a success be a *public* V2 card while a
// refusal stays *private* — a public "thinking…" cannot become an ephemeral reply.
[CommandContextType(InteractionContextType.Guild)]
[Group("plynling", "Ton Plynling : l'adopter, t'en occuper, le regarder vivre")]
public class PlynlingModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly PlynlingService _plynlings;
    private readonly PlynlingCareService _care;
    private readonly ResponsePicker _picker;
    private readonly PlynlingAnnouncer _announcer;

    public PlynlingModule(PlynlingService plynlings, PlynlingCareService care, ResponsePicker picker, PlynlingAnnouncer announcer)
    {
        _plynlings = plynlings;
        _care = care;
        _picker = picker;
        _announcer = announcer;
    }

    [SlashCommand("adopt", "Adopter un Plynling — gratuit, mais il faudra s'en occuper")]
    public async Task AdoptAsync(
        [Summary("name", "Son nom")] [MaxLength(InputCaps.PlynlingName)] string name)
    {
        name = name.Trim();
        if (name.Length == 0)
        {
            await RespondAsync(PlynlingText.EmptyName, ephemeral: true);
            return;
        }

        var now = DateTimeOffset.UtcNow;
        // Only mushrooms are adoptable for now: the sunflower species exist in the catalog but
        // stay dormant until families ship (the plan's deferred Task 3 adds the family: option).
        var species = PlynlingCatalog.RollSpecies(PlynlingFamily.Mushroom);
        var (outcome, plynling) = await _plynlings.AdoptAsync(Context.Guild.Id, Context.User.Id, name, species, now);
        if (outcome != AdoptOutcome.Adopted || plynling is null)
        {
            await RespondAsync(PlynlingText.AlreadyHasOne, ephemeral: true);
            return;
        }

        var info = PlynlingCatalog.Info(species);
        var pool = (info.Rarity >= PlynlingRarity.Rare ? BotResponses.PlynlingAdoptRareLines : BotResponses.PlynlingAdoptLines).For(plynling.Gender);
        var line = string.Format(_picker.Pick(Context.Channel.Id, pool),
            PlynlingCardUi.SafeName(plynling.Name), info.Name, PlynlingCatalog.RarityLabel(info.Rarity));
        await RespondCardAsync(plynling, now, line);
    }

    [SlashCommand("view", "Voir un Plynling (le tien par défaut)")]
    public async Task ViewAsync(
        [Summary("user", "À qui est le Plynling (par défaut : toi)")] IUser? user = null)
    {
        var target = user ?? Context.User;
        var now = DateTimeOffset.UtcNow;
        var plynling = await _plynlings.GetShownAsync(Context.Guild.Id, target.Id, now);
        if (plynling is null)
        {
            await RespondAsync(target.Id == Context.User.Id ? PlynlingText.NoPlynling : PlynlingText.NoneFor(target.Id),
                ephemeral: true, allowedMentions: AllowedMentions.None);
            return;
        }
        await RespondCardAsync(plynling, now, null);
    }

    [SlashCommand("feed", "Nourrir ton Plynling")]
    public async Task FeedAsync([Summary("food", "Quoi lui donner")] PlynlingFood food)
    {
        var now = DateTimeOffset.UtcNow;
        var plynling = await _plynlings.GetCurrentAsync(Context.Guild.Id, Context.User.Id, now);
        if (plynling is null)
        {
            await RespondAsync(PlynlingText.NoPlynling, ephemeral: true);
            return;
        }
        await SendAsync(await _care.FeedAsync(plynling.Id, Context.User.Id, food, Context.Channel.Id, now));
    }

    [SlashCommand("pet", "Caresser un Plynling (le tien par défaut)")]
    public async Task PetAsync(
        [Summary("user", "À qui est le Plynling (par défaut : toi)")] IUser? user = null)
    {
        var target = user ?? Context.User;
        var now = DateTimeOffset.UtcNow;
        var plynling = await _plynlings.GetCurrentAsync(Context.Guild.Id, target.Id, now);
        if (plynling is null)
        {
            await RespondAsync(target.Id == Context.User.Id ? PlynlingText.NoPlynling : PlynlingText.NoneFor(target.Id),
                ephemeral: true, allowedMentions: AllowedMentions.None);
            return;
        }
        await SendAsync(await _care.PetAsync(plynling.Id, Context.User.Id, Context.Channel.Id, now));
    }

    // Without `user`, the owner freezes their own under the self-freeze rules. With a
    // different `user`, it is a staff freeze: no rules, lifted by staff only, and the
    // owner is told by DM so it never looks like a bug.
    [SlashCommand("freeze", "Geler un Plynling : plus rien ne bouge (vacances)")]
    public async Task FreezeAsync(
        [Summary("user", "Staff : le Plynling de quelqu'un d'autre")] IUser? user = null)
    {
        var target = user ?? Context.User;
        var byStaff = target.Id != Context.User.Id;
        if (byStaff && !SessionPermissions.IsStaff(Context.User))
        {
            await RespondAsync(PlynlingText.StaffOnly, ephemeral: true);
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var (outcome, plynling) = await _plynlings.FreezeAsync(Context.Guild.Id, target.Id, byStaff, now);
        if (outcome != FreezeOutcome.Frozen || plynling is null)
        {
            // Every refusal but NoPlynling carries the Plynling, so its gender is known.
            var g = plynling?.Gender ?? PlynlingGender.Male;
            await RespondAsync(outcome switch
            {
                FreezeOutcome.NoPlynling => byStaff ? PlynlingText.NoneFor(target.Id) : PlynlingText.NoPlynling,
                FreezeOutcome.Dead => PlynlingText.Dead(g),
                FreezeOutcome.AlreadyFrozen => PlynlingText.AlreadyFrozen(g),
                FreezeOutcome.TooHungry => PlynlingText.TooHungryToFreeze(g),
                FreezeOutcome.Cooldown => PlynlingText.FreezeCooldown(g, plynling!.LastSelfThawAt!.Value + PlynlingLife.SelfFreezeCooldown),
                _ => PlynlingText.Unknown,
            }, ephemeral: true, allowedMentions: AllowedMentions.None);
            return;
        }

        await RespondCardAsync(plynling, now, PlynlingText.FrozenNotice(plynling.Gender, PlynlingCardUi.SafeName(plynling.Name), plynling.FreezeUntil));
        // After the reply, never before: nothing here defers, and a DM is two or three
        // REST calls against Discord's 3 s deadline for answering the interaction.
        if (byStaff)
            await _announcer.DmOwnerAsync(plynling.OwnerId, string.Format(
                _picker.Pick(plynling.OwnerId, BotResponses.PlynlingStaffFreezeDms.For(plynling.Gender)), PlynlingCardUi.SafeName(plynling.Name)));
    }

    [SlashCommand("thaw", "Dégeler un Plynling")]
    public async Task ThawAsync(
        [Summary("user", "Staff : le Plynling de quelqu'un d'autre")] IUser? user = null)
    {
        var target = user ?? Context.User;
        var asStaff = SessionPermissions.IsStaff(Context.User);
        if (target.Id != Context.User.Id && !asStaff)
        {
            await RespondAsync(PlynlingText.StaffOnly, ephemeral: true);
            return;
        }

        var now = DateTimeOffset.UtcNow;
        // Staff may lift any freeze, including a staff freeze on their own Plynling.
        var (outcome, plynling) = await _plynlings.ThawAsync(Context.Guild.Id, target.Id, asStaff, now);
        if (outcome != ThawOutcome.Thawed || plynling is null)
        {
            var g = plynling?.Gender ?? PlynlingGender.Male;
            await RespondAsync(outcome switch
            {
                ThawOutcome.NoPlynling => target.Id == Context.User.Id ? PlynlingText.NoPlynling : PlynlingText.NoneFor(target.Id),
                ThawOutcome.Dead => PlynlingText.Dead(g),
                ThawOutcome.NotFrozen => PlynlingText.NotFrozen(g),
                ThawOutcome.StaffOnly => PlynlingText.ThawStaffOnly(g),
                _ => PlynlingText.Unknown,
            }, ephemeral: true, allowedMentions: AllowedMentions.None);
            return;
        }

        await RespondCardAsync(plynling, now, PlynlingText.ThawedNotice(plynling.Gender, PlynlingCardUi.SafeName(plynling.Name)));
        if (target.Id != Context.User.Id)   // after the reply — see FreezeAsync
            await _announcer.DmOwnerAsync(plynling.OwnerId, string.Format(
                _picker.Pick(plynling.OwnerId, BotResponses.PlynlingStaffThawDms.For(plynling.Gender)), PlynlingCardUi.SafeName(plynling.Name)));
    }

    // Staff only: the name is shown publicly (card, announcements, graveyard), so fixing
    // an offensive one is moderation. Reaches their latest grave too.
    [SlashCommand("rename", "Staff : renommer le Plynling de quelqu'un")]
    public async Task RenameAsync(
        [Summary("user", "À qui est le Plynling")] IUser user,
        [Summary("name", "Son nouveau nom")] [MaxLength(InputCaps.PlynlingName)] string name)
    {
        if (!SessionPermissions.IsStaff(Context.User))
        {
            await RespondAsync(PlynlingText.StaffOnly, ephemeral: true);
            return;
        }
        name = name.Trim();
        if (name.Length == 0)
        {
            await RespondAsync(PlynlingText.EmptyName, ephemeral: true);
            return;
        }

        var (plynling, oldName) = await _plynlings.RenameAsync(Context.Guild.Id, user.Id, name, DateTimeOffset.UtcNow);
        if (plynling is null)
        {
            await RespondAsync(PlynlingText.NoneFor(user.Id), ephemeral: true, allowedMentions: AllowedMentions.None);
            return;
        }

        await RespondAsync($"✏️ **{PlynlingCardUi.SafeName(oldName)}** s'appelle désormais **{PlynlingCardUi.SafeName(plynling.Name)}**.",
            ephemeral: true, allowedMentions: AllowedMentions.None);
        if (user.Id != Context.User.Id)   // after the reply — see FreezeAsync
            await _announcer.DmOwnerAsync(plynling.OwnerId, string.Format(
                _picker.Pick(plynling.OwnerId, BotResponses.PlynlingStaffRenameDms.For(plynling.Gender)),
                PlynlingCardUi.SafeName(oldName), PlynlingCardUi.SafeName(plynling.Name)));
    }

    // Staff only in v1 (a rare self-service item comes later, through the same
    // PlynlingService.ResurrectAsync). The comeback is announced publicly, like the death.
    [SlashCommand("resurrect", "Staff : ressusciter le dernier Plynling de quelqu'un")]
    public async Task ResurrectAsync([Summary("user", "À qui est le Plynling")] IUser user)
    {
        if (!SessionPermissions.IsStaff(Context.User))
        {
            await RespondAsync(PlynlingText.StaffOnly, ephemeral: true);
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var (outcome, plynling) = await _plynlings.ResurrectAsync(Context.Guild.Id, user.Id, now);
        if (outcome != ResurrectOutcome.Resurrected || plynling is null)
        {
            await RespondAsync(outcome == ResurrectOutcome.NoGrave ? PlynlingText.NoGrave : PlynlingText.ResurrectBlocked,
                ephemeral: true);
            return;
        }

        await RespondAsync($"✨ **{PlynlingCardUi.SafeName(plynling.Name)}** est de retour (annoncé dans <#{PlynlingAnnouncer.GameChannelId}>).",
            ephemeral: true, allowedMentions: AllowedMentions.None);
        await _announcer.AnnounceResurrectionAsync(plynling, now);   // after the reply — see FreezeAsync
    }

    [SlashCommand("help", "Comment fonctionnent les Plynlings")]
    public Task HelpAsync() => RespondAsync(embed: BuildHelpEmbed(), ephemeral: true);

    /// <summary>
    /// The Plynling guide. Static and Context-free for the same reason as
    /// <see cref="HelpModule.BuildEmbed"/>: embed caps throw at *send* time, so the only way
    /// to know it fits is to build and measure it without a gateway.
    /// </summary>
    public static Embed BuildHelpEmbed() =>
        new EmbedBuilder()
            .WithTitle("🍄 Plynlings — mode d'emploi")
            .WithDescription("Un Plynling est un petit champignon qui vit avec toi. Nourris-le, caresse-le, " +
                             "et surtout… ne l'oublie pas.")
            .WithColor(new Color(0xCE323A))
            .AddField("Adopter & regarder",
                "**`/plynling adopt name:`** — Gratuit, un seul à la fois. L'espèce est tirée au sort : " +
                "commune, peu commune, rare… ou légendaire.\n" +
                "**`/plynling view [user]`** — Sa carte, avec les boutons **Caresser** et **Nourrir**.")
            .AddField("S'en occuper",
                "La **faim** se vide en **4 jours** : à 0 %, il meurt. Le **bonheur** se vide en **2 jours** " +
                "(il est juste triste).\n" +
                "**`/plynling feed food:`** — Champignon (15), Shiitake (30), Morille (40, que du bonheur), Truffe (80, faim et bonheur).\n" +
                "**`/plynling pet [user]`** — +25 % de bonheur, toutes les 4 h, sur n'importe quel Plynling.")
            .AddField("Gagner des cailloux",
                "**`/work`** — 40 à 60 cailloux, toutes les 4 h.\n" +
                "Parler, réagir et le vocal rapportent aussi quelques cailloux (45 au plus par jour).\n" +
                "**`/balance`** — Ton solde, visible par toi seul.")
            .AddField("Partir en vacances",
                "**`/plynling freeze`** — Gèle ton Plynling (14 jours au plus) : plus rien ne bouge. " +
                "Seulement s'il a encore au moins 50 % de faim.\n" +
                "**`/plynling thaw`** — Le dégèle. Ensuite, 7 jours avant de pouvoir le regeler.")
            .AddField("La mort",
                "Tu reçois un **message privé** environ 6 h avant qu'il meure de faim. S'il meurt, tout le " +
                "serveur l'apprend et il rejoint le cimetière.\n" +
                "**`/graveyard [user]`** — Les tombes, triées par date ou par longueur de vie. Plus il a vécu, " +
                "plus sa tombe est belle.")
            .AddField("Staff",
                "**`/plynling freeze user:`** · **`/plynling thaw user:`** — Sur n'importe quel Plynling.\n" +
                "**`/plynling rename user: name:`** · **`/plynling resurrect user:`**")
            .WithFooter($"Project S.Y.N.C.S. v{AppInfo.Version}")
            .Build();

    // ---- rendering --------------------------------------------------------------

    private Task RespondCardAsync(Plynling plynling, DateTimeOffset now, string? line) =>
        RespondAsync(components: BuildCard(plynling, now, line), flags: MessageFlags.ComponentsV2,
            allowedMentions: AllowedMentions.None);

    private Task SendAsync(CareReply reply) =>
        reply.Card is not null
            ? RespondAsync(components: reply.Card, flags: MessageFlags.ComponentsV2, allowedMentions: AllowedMentions.None)
            : RespondAsync(reply.Refusal, ephemeral: true, allowedMentions: AllowedMentions.None);

    /// <summary>
    /// The card: picture (the sprite for its mood, or its memorial once dead), heading,
    /// bars, an optional line in her voice, and — only while alive and not frozen — a
    /// "Caresser" button and a "Nourrir…" select.
    /// </summary>
    /// <remarks>
    /// Static and Context-free so its component budget is checkable without a gateway.
    /// The two rows use different verbs (<c>plyn:pet</c>, <c>plyn:feed</c>): duplicated
    /// custom ids are rejected outright by Discord, disabled components included.
    /// Nourrir is offered to everyone and refused in the handler for anyone but the
    /// owner — the real check is in code, as with every gate here.
    /// </remarks>
    public static MessageComponent BuildCard(
        Plynling plynling, DateTimeOffset now, string? lastAction, string? lastActionImage = null)
    {
        var info = PlynlingCatalog.Info(plynling.Species);
        var alive = plynling.DiedAt is null;
        var picture = alive
            ? PlynlingArt.Sprite(plynling.Species, PlynlingLife.Mood(plynling, now))
            : PlynlingArt.Memorial(plynling.Species, PlynlingCatalog.MemorialTier(PlynlingLife.Age(plynling, now)));

        var container = new ContainerBuilder()
            .WithAccentColor(new Color(info.Accent))
            .AddComponent(new SectionBuilder()
                .WithAccessory(new ThumbnailBuilder()
                    .WithMedia(new UnfurledMediaItemProperties(picture))
                    .WithDescription(info.Name))
                .AddComponent(new TextDisplayBuilder(PlynlingCardUi.Heading(plynling, now))))
            .AddComponent(new SeparatorBuilder())
            .AddComponent(new TextDisplayBuilder(PlynlingCardUi.Status(plynling, now)));
        if (!string.IsNullOrWhiteSpace(lastAction))
        {
            // After a meal the food's own sprite sits beside her line — the one place the
            // food art appears, since a select menu cannot carry images.
            container.AddComponent(lastActionImage is null
                ? new TextDisplayBuilder(lastAction)
                : new SectionBuilder()
                    .WithAccessory(new ThumbnailBuilder().WithMedia(new UnfurledMediaItemProperties(lastActionImage)))
                    .AddComponent(new TextDisplayBuilder(lastAction)));
        }

        var builder = new ComponentBuilderV2().AddComponent(container);
        if (alive && plynling.FrozenAt is null)
        {
            builder.AddComponent(new ActionRowBuilder()
                .WithButton("🤲 Caresser", $"plyn:pet:{plynling.Id}", ButtonStyle.Primary));

            var menu = new SelectMenuBuilder()
                .WithCustomId($"plyn:feed:{plynling.Id}")
                .WithPlaceholder("🍄 Nourrir…");
            foreach (var food in PlynlingCatalog.Foods)
                menu.AddOption($"{food.Name} — {PebbleEconomy.Cailloux(food.Price)}", food.Food.ToString(),
                    PlynlingCardUi.FoodEffect(food));
            builder.AddComponent(new ActionRowBuilder().WithSelectMenu(menu));
        }
        return builder.Build();
    }
}
