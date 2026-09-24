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

    public PlynlingModule(PlynlingService plynlings, PlynlingCareService care, ResponsePicker picker)
    {
        _plynlings = plynlings;
        _care = care;
        _picker = picker;
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
        var species = PlynlingCatalog.RollSpecies();
        var (outcome, plynling) = await _plynlings.AdoptAsync(Context.Guild.Id, Context.User.Id, name, species, now);
        if (outcome != AdoptOutcome.Adopted || plynling is null)
        {
            await RespondAsync(PlynlingText.AlreadyHasOne, ephemeral: true);
            return;
        }

        var info = PlynlingCatalog.Info(species);
        var pool = info.Rarity >= PlynlingRarity.Rare ? BotResponses.PlynlingAdoptRareLines : BotResponses.PlynlingAdoptLines;
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
