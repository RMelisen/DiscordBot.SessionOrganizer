using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Commands;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Interactions.Components;

// The cosmetics screen's controls (CosmeticCards). The screen is an ephemeral message only its owner
// sees, so whoever clicks is whoever the screen is about. Every click defers first — most read or
// write the database before anything can be shown — then redraws the screen in place.
public class CosmeticComponentHandler : InteractionModuleBase<SocketInteractionContext>
{
    private readonly CosmeticService _cosmetics;
    private readonly PlynlingService _plynlings;
    private readonly ILogger<CosmeticComponentHandler> _logger;

    public CosmeticComponentHandler(CosmeticService cosmetics, PlynlingService plynlings, ILogger<CosmeticComponentHandler> logger)
    {
        _cosmetics = cosmetics;
        _plynlings = plynlings;
        _logger = logger;
    }

    [ComponentInteraction("cosm:tab:*", ignoreGroupNames: true)]
    public async Task OnTabAsync(string tabStr)
    {
        if (!Enum.TryParse<CosmeticTab>(tabStr, out var tab)) return;
        await DeferAsync();
        await RedrawAsync(new CosmeticView(tab), null);
    }

    [ComponentInteraction("cosm:back:*", ignoreGroupNames: true)]
    public Task OnBackAsync(string tabStr) => OnTabAsync(tabStr);

    [ComponentInteraction("cosm:slot:*", ignoreGroupNames: true)]
    public async Task OnSlotAsync(string slotStr)
    {
        if (!Enum.TryParse<CosmeticSlot>(slotStr, out var slot)) return;
        await DeferAsync();
        await RedrawAsync(new CosmeticView(CosmeticTab.Wardrobe, slot), null);
    }

    // The screen's selects. In the wardrobe a pick is worn at once (« Aucun » takes the slot off);
    // in the shop and the recipes it is only tried on.
    [ComponentInteraction("cosm:pick:*:*", ignoreGroupNames: true)]
    public async Task OnPickAsync(string tabStr, string weekStr, string[] selected)
    {
        if (!Enum.TryParse<CosmeticTab>(tabStr, out var tab)) return;
        var value = selected.FirstOrDefault() ?? "";
        var now = DateTimeOffset.UtcNow;
        await DeferAsync();

        switch (tab)
        {
            case CosmeticTab.Wardrobe:
            {
                var off = value.StartsWith(CosmeticCards.NonePrefix, StringComparison.Ordinal);
                var cosmetic = off ? null : CosmeticCatalog.ByKey(value);
                var slot = CosmeticSlot.Theme;
                if (off ? !Enum.TryParse(value[CosmeticCards.NonePrefix.Length..], out slot) : cosmetic is null)
                {
                    await RedrawAsync(new CosmeticView(tab), PlynlingText.UnknownItem);
                    return;
                }
                if (cosmetic is not null) slot = cosmetic.Slot;
                var (outcome, plynling) = await _cosmetics.WearAsync(Context.Guild.Id, Context.User.Id, slot, cosmetic?.Key, now);
                var notice = outcome != CosmeticOutcome.Done ? CosmeticCards.Notice(outcome, cosmetic)
                    : cosmetic is null ? CosmeticCards.TakenOff(slot)
                    : WornNotice(plynling!.Name, cosmetic);
                await RedrawAsync(new CosmeticView(tab, slot), notice);
                return;
            }
            case CosmeticTab.Shop when !int.TryParse(weekStr, out var week) || week != CosmeticCatalog.WeekKey(now):
                await RedrawAsync(new CosmeticView(tab), CosmeticCards.Notice(CosmeticOutcome.NotInShop, null));
                return;
            default:
                await RedrawAsync(new CosmeticView(tab, Picked: value), null);
                return;
        }
    }

    [ComponentInteraction("cosm:buy:*:*", ignoreGroupNames: true)]
    public Task OnBuyAsync(string weekStr, string key) => BuyAsync(weekStr, key, wear: false);

    [ComponentInteraction("cosm:buyw:*:*", ignoreGroupNames: true)]
    public Task OnBuyWearAsync(string weekStr, string key) => BuyAsync(weekStr, key, wear: true);

    private async Task BuyAsync(string weekStr, string key, bool wear)
    {
        await DeferAsync();
        var now = DateTimeOffset.UtcNow;
        var week = int.TryParse(weekStr, out var w) ? w : 0;
        var result = await _cosmetics.BuyAsync(Context.Guild.Id, Context.User.Id, key, week, now, wear);
        var cosmetic = CosmeticCatalog.ByKey(key);
        var bought = result.Outcome == CosmeticOutcome.Done && cosmetic is not null;
        // A purchase that went through redraws clean (its receipt follows); a « …et porter » whose
        // Plynling is gone says so on the card.
        var notice = bought && (!wear || result.Worn) ? null
            : CosmeticCards.Notice(result.Outcome, cosmetic, cosmetic?.Price ?? 0, wear, result.Worn);
        await RedrawAsync(new CosmeticView(CosmeticTab.Shop, Picked: key), notice);
        if (!bought) return;

        // Like /inventory shop: a purchase that went through gets its own ephemeral receipt, which the
        // next click doesn't erase. A failed receipt is logged and dropped — the purchase stands.
        try
        {
            await FollowupAsync(ShopCards.Receipt(CosmeticCards.Bought(cosmetic!, result.Balance, result.Worn)), ephemeral: true,
                allowedMentions: AllowedMentions.None);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cosmetic receipt failed for {UserId}", Context.User.Id);
        }
    }

    [ComponentInteraction("cosm:make:*", ignoreGroupNames: true)]
    public Task OnMakeAsync(string key) => MakeAsync(key, wear: false);

    [ComponentInteraction("cosm:makew:*", ignoreGroupNames: true)]
    public Task OnMakeWearAsync(string key) => MakeAsync(key, wear: true);

    // A craft keeps its notice on the card: it is not a purchase, so no receipt.
    private async Task MakeAsync(string key, bool wear)
    {
        await DeferAsync();
        var result = await _cosmetics.CraftAsync(Context.Guild.Id, Context.User.Id, key, DateTimeOffset.UtcNow, wear);
        var cosmetic = CosmeticCatalog.ByKey(key);
        await RedrawAsync(new CosmeticView(CosmeticTab.Craft, Picked: key),
            CosmeticCards.Notice(result.Outcome, cosmetic, cosmetic?.Price ?? 0, wear, result.Worn));
    }

    // « Porter » on an owned item in the shop or the recipes: worn, and the item view stays.
    [ComponentInteraction("cosm:wear:*:*", ignoreGroupNames: true)]
    public async Task OnWearAsync(string tabStr, string key)
    {
        if (!Enum.TryParse<CosmeticTab>(tabStr, out var tab)) return;
        await DeferAsync();
        if (CosmeticCatalog.ByKey(key) is not { } cosmetic)
        {
            await RedrawAsync(new CosmeticView(tab), PlynlingText.UnknownItem);
            return;
        }
        var (outcome, plynling) = await _cosmetics.WearAsync(Context.Guild.Id, Context.User.Id, cosmetic.Slot, key, DateTimeOffset.UtcNow);
        var notice = outcome == CosmeticOutcome.Done ? WornNotice(plynling!.Name, cosmetic) : CosmeticCards.Notice(outcome, cosmetic);
        await RedrawAsync(new CosmeticView(tab, Picked: key), notice);
    }

    // The worn accessory's colour or position (cosm:style:{choice}:{key}): free, worn at once.
    [ComponentInteraction("cosm:style:*:*", ignoreGroupNames: true)]
    public async Task OnStyleAsync(string choiceStr, string key, string[] selected)
    {
        if (!int.TryParse(choiceStr, out var choice)) return;
        await DeferAsync();
        var option = selected.FirstOrDefault() ?? "";
        var (outcome, plynling) = await _cosmetics.StyleAsync(Context.Guild.Id, Context.User.Id, key, choice, option);
        var cosmetic = CosmeticCatalog.ByKey(key);
        var notice = outcome == CosmeticOutcome.Done && cosmetic is not null
            ? $"{Emotes.Sparkle} {PlynlingCardUi.SafeName(plynling!.Name)} change de style : {cosmetic.Emoji} {CosmeticCatalog.ShortName(cosmetic)}, " +
              $"{AccessoryStyles.Choices(key)[choice].Options.First(o => o.Key == option).Name.ToLowerInvariant()}."
            : outcome == CosmeticOutcome.UnknownItem ? "❌ Ce n'est plus ce qu'il porte : choisis à nouveau."
            : CosmeticCards.Notice(outcome, cosmetic);
        await RedrawAsync(new CosmeticView(CosmeticTab.Wardrobe, CosmeticSlot.Accessory), notice);
    }

    private static string WornNotice(string name, CosmeticInfo c) =>
        $"{Emotes.Sparkle} {PlynlingCardUi.SafeName(name)} porte maintenant {c.Emoji} {CosmeticCatalog.ShortName(c)} !";

    // Re-reads everything, so the screen always shows what is true after the click.
    private async Task RedrawAsync(CosmeticView view, string? notice)
    {
        var now = DateTimeOffset.UtcNow;
        var data = await CosmeticCards.LoadAsync(_plynlings, _cosmetics, Context.Guild.Id, Context.User.Id, now);
        var card = CosmeticCards.Build(view, data, now, notice);
        await ModifyOriginalResponseAsync(m =>
        {
            m.Components = card;
            m.Flags = MessageFlags.ComponentsV2;
            m.AllowedMentions = AllowedMentions.None;
        });
    }
}
