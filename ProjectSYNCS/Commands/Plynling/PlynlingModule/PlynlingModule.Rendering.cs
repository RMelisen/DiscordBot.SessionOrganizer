using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Interactions.Modals;
using ProjectSYNCS.Models;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Commands;

public abstract partial class PlynlingModule : InteractionModuleBase<SocketInteractionContext>
{
    // ---- rendering --------------------------------------------------------------

    // The card commands (adopt, view, freeze, thaw, passion) read and write the database before they
    // know what to show, and on the Pi that can outrun Discord's 3 s: the action was saved while the
    // command looked dead. So they defer first, publicly since the card is public, and the card
    // fills the deferred reply. Checks that need no database still refuse before deferring.
    private async Task RespondCardAsync(Plynling plynling, DateTimeOffset now, string? line)
    {
        var card = await BuildCardAsync(_plynlings, plynling, now, line);
        await ModifyOriginalResponseAsync(m =>
        {
            m.Components = card;
            m.Flags = MessageFlags.ComponentsV2;
            m.AllowedMentions = AllowedMentions.None;
        });
    }

    // A refusal after deferring stays private: a follow-up to a public deferral would take its place
    // publicly, so the deferred reply is deleted first and the refusal sent as a private follow-up.
    private async Task RefuseAsync(string text)
    {
        await DeleteOriginalResponseAsync();
        await FollowupAsync(text, ephemeral: true, allowedMentions: AllowedMentions.None);
    }

    // The card with everything it shows besides the Plynling itself — its partner, its owner's pantry,
    // its traits, what events wait — loaded in one place, so a new input is added once, not at every
    // caller (the view command and each care button).
    public static async Task<MessageComponent> BuildCardAsync(PlynlingService plynlings, Plynling plynling, DateTimeOffset now,
        string? lastAction, string? lastActionImage = null) =>
        BuildCard(plynling, now, lastAction, lastActionImage,
            partnerName: (await plynlings.GetPartnerAsync(plynling))?.Name,
            pantry: await plynlings.GetPantryAsync(plynling),
            traits: await plynlings.GetTraitsAsync(plynling),
            pendingEvents: await plynlings.CountPendingEventsAsync(plynling, now),
            bathSoap: await plynlings.GetBathSoapAsync(plynling, now));


    /// <summary>
    /// The card: picture (the sprite for its mood, or its memorial once dead), heading,
    /// bars, an optional line in her voice, and — only while alive and not frozen — a
    /// "Caresser" and "Laver" buttons and a "Nourrir…" select.
    /// </summary>
    /// <remarks>
    public const int ListPageSize = 10;

    public static int ListPages(int count) => Math.Max(1, (count + ListPageSize - 1) / ListPageSize);

    /// <summary>
    /// One page of /plynling list: the living, oldest first (ties on id, so the order is
    /// stable across clicks). An embed rather than Components V2 — mentions in an embed never
    /// ping, and a list of names wants no avatars. Static and Context-free, like the card,
    /// so it is checkable without a gateway. A page past the end shows the last one.
    /// </summary>
    public static Embed BuildListEmbed(IReadOnlyList<Plynling> living, int page, DateTimeOffset now)
    {
        var pages = ListPages(living.Count);
        page = Math.Clamp(page, 0, pages - 1);
        var ordered = living
            .OrderByDescending(p => PlynlingLife.Age(p, now))
            .ThenBy(p => p.Id)
            .Skip(page * ListPageSize)
            .Take(ListPageSize)
            .Select(p =>
            {
                var info = PlynlingCatalog.Info(p.Species);
                var status = PlynlingLife.IsFrozen(p) ? " ❄️" : PlynlingLife.IsAsleep(now) ? " 💤" : "";
                var age = LevelCardUi.Duration((long)PlynlingLife.Age(p, now).TotalMinutes);
                return $"**{PlynlingCardUi.SafeName(p.Name)}** {p.Gender.Symbol()} · {info.Name} " +
                       $"({PlynlingCardUi.StageLabel(PlynlingLife.Stage(p, now), p.Gender)}) — <@{p.OwnerId}> · {age}{status}";
            })
            .ToList();

        return new EmbedBuilder()
            .WithTitle("🌱 Les Plynlings du serveur")
            .WithColor(new Color(0x62AA58))
            .WithDescription(ordered.Count == 0
                ? "Aucun Plynling vivant pour l'instant. `/plynling adopt` pour commencer !"
                : string.Join("\n", ordered))
            .WithFooter($"Page {page + 1}/{pages} · {living.Count} Plynling{(living.Count > 1 ? "s" : "")}")
            .Build();
    }

    // ◀ ▶ carry *different* verbs (lprev / lnext): with one shared verb, a disabled ◀ on the
    // first page and the ▶ of another page could produce the same id, which Discord rejects.
    public static MessageComponent BuildListButtons(int page, int pages)
    {
        page = Math.Clamp(page, 0, pages - 1);
        return new ComponentBuilder()
            .WithButton("◀", $"plyn:lprev:{Math.Max(0, page - 1)}", ButtonStyle.Secondary, disabled: page == 0)
            .WithButton("▶", $"plyn:lnext:{Math.Min(pages - 1, page + 1)}", ButtonStyle.Secondary, disabled: page >= pages - 1)
            .Build();
    }

    /// Static and Context-free so its component budget is checkable without a gateway.
    /// Every control uses its own verb (<c>plyn:pet</c>, <c>plyn:bath</c> or <c>plyn:soap</c>, <c>plyn:heal</c>, <c>plyn:feed</c>, <c>plyn:traits</c>, <c>plyn:events</c>): duplicated
    /// custom ids are rejected outright by Discord, disabled components included.
    /// Nourrir is offered to everyone and refused in the handler for anyone but the
    /// owner — the real check is in code, as with every gate here.
    /// </remarks>
    public static MessageComponent BuildCard(
        Plynling plynling, DateTimeOffset now, string? lastAction, string? lastActionImage = null, string? partnerName = null,
        IReadOnlyDictionary<PlynlingFood, int>? pantry = null, IReadOnlyList<TraitInfo>? traits = null, int pendingEvents = 0,
        int bathSoap = 0)
    {
        var info = PlynlingCatalog.Info(plynling.Species);
        var alive = plynling.DiedAt is null;
        var picture = alive
            ? PlynlingArt.SpriteOf(plynling, now)
            : PlynlingArt.Memorial(plynling.Species, PlynlingCatalog.MemorialTier(PlynlingLife.Age(plynling, now)));

        // A worn thème recolours the card and opens it with its banner — the one cosmetic that
        // costs a component; the title and the accessory ride in the heading's text.
        var theme = CosmeticSlots.Worn(plynling, CosmeticSlot.Theme);
        var container = new ContainerBuilder()
            .WithAccentColor(new Color(theme?.Accent ?? info.Accent));
        if (theme?.Banner is { } banner)
            container.AddComponent(new TextDisplayBuilder(banner));
        container
            .AddComponent(new SectionBuilder()
                .WithAccessory(new ThumbnailBuilder()
                    .WithMedia(new UnfurledMediaItemProperties(picture))
                    .WithDescription(info.Name))
                .AddComponent(new TextDisplayBuilder(PlynlingCardUi.Heading(plynling, now, partnerName))))
            .AddComponent(new SeparatorBuilder())
            .AddComponent(new TextDisplayBuilder(PlynlingCardUi.Status(plynling, now)));
        // Its personality under the status: the title, the trait emojis, then its stress level and its
        // modifiers' icons (PlynlingPersonality) — one component, as before.
        var personality = PlynlingPersonality.CardLine(traits ?? Array.Empty<TraitInfo>(), plynling.Gender);
        var stateLine = alive ? PlynlingPersonality.StateLine(plynling, now) : null;
        var line = string.Join(" · ", new[] { personality, stateLine }.OfType<string>());
        if (line.Length > 0)
            container.AddComponent(new TextDisplayBuilder(line));
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
            // No petting or washing a sleeping Plynling — the buttons go, feeding stays. Laver is
            // offered to everyone like the rest of the card and refused in the handler for anyone
            // but the owner.
            // « Soigner » joins them only while it is sick. While the free bath waits and the owner
            // holds soap (bathSoap), Laver becomes the soap button — its own verb, plyn:soap, and a
            // label that says it uses one, so a plain « Laver » never spends soap.
            if (!PlynlingLife.IsAsleep(now))
            {
                var row = new ActionRowBuilder()
                    .WithButton("Caresser", $"plyn:pet:{plynling.Id}", ButtonStyle.Primary);
                if (bathSoap > 0)
                    row.WithButton($"Laver · utilise 1 savon (×{bathSoap})", $"plyn:soap:{plynling.Id}", ButtonStyle.Secondary, new Emoji("🧼"));
                else
                    row.WithButton("Laver", $"plyn:bath:{plynling.Id}", ButtonStyle.Secondary);
                if (PlynlingLife.IsSick(plynling))
                    row.WithButton("Soigner", $"plyn:heal:{plynling.Id}", ButtonStyle.Success);
                builder.AddComponent(row);
            }

            var menu = new SelectMenuBuilder()
                .WithCustomId($"plyn:feed:{plynling.Id}")
                .WithPlaceholder("Nourrir…");
            foreach (var food in PlynlingCatalog.Foods)
                menu.AddOption(PlynlingCardUi.FoodOptionLabel(food, pantry?.GetValueOrDefault(food.Food) ?? 0), food.Food.ToString(),
                    PlynlingCardUi.FoodOptionDescription(food),
                    // Its picture once the bot's emojis are up; no icon rather than 🍄 ×4 before that.
                    Emote.TryParse(ItemCatalog.ByKey(ItemCatalog.FoodKey(food.Food))!.Emoji, out var foodPicture) ? foodPicture : null);
            builder.AddComponent(new ActionRowBuilder().WithSelectMenu(menu));
        }
        // Its own row, the card's last: the care row is hidden at night and holds three buttons when
        // sick. Shown frozen too, for « Personnalité » (anyone). « Événement » only while one waits and
        // never frozen (pendingEvents is 0 then) — offered to everyone like « Laver », refused in the
        // handler for anyone but the owner.
        if (alive)
        {
            var personalityRow = new ActionRowBuilder()
                .WithButton("Personnalité", $"plyn:traits:{plynling.Id}", ButtonStyle.Secondary);
            if (pendingEvents > 0)
                personalityRow.WithButton($"Événement ({pendingEvents})", $"plyn:events:{plynling.Id}", ButtonStyle.Success, new Emoji("✨"));
            builder.AddComponent(personalityRow);
        }
        return builder.Build();
    }
}
