using Discord;
using ProjectSYNCS.Commands;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Services;

// Either a re-rendered card (the action happened), or a refusal to send privately.
public sealed record CareReply(MessageComponent? Card, string? Refusal);

// Feeding and petting, shared by the slash commands and the card's buttons so the two
// can never behave differently. Transient: it wraps PlynlingService.
public class PlynlingCareService
{
    private readonly PlynlingService _plynlings;
    private readonly PlynlingCooldowns _cooldowns;
    private readonly ResponsePicker _picker;

    public PlynlingCareService(PlynlingService plynlings, PlynlingCooldowns cooldowns, ResponsePicker picker)
    {
        _plynlings = plynlings;
        _cooldowns = cooldowns;
        _picker = picker;
    }

    // Anyone may pet anyone's Plynling; the 4 h cooldown is per petter per Plynling. The
    // claim is released if the pet does not happen, so a refusal never costs a cooldown.
    public async Task<CareReply> PetAsync(int plynlingId, ulong actorId, ulong channelId, DateTimeOffset now)
    {
        var key = (actorId, plynlingId);
        if (!_cooldowns.Pet.TryClaim(key)) return new CareReply(null, PlynlingText.PetCooldown);

        var (outcome, plynling) = await _plynlings.PetAsync(plynlingId, now);
        if (outcome != CareOutcome.Done || plynling is null)
        {
            _cooldowns.Pet.Release(key);
            return new CareReply(null, Refusal(outcome, plynling?.Gender ?? PlynlingGender.Male));
        }

        var line = string.Format(_picker.Pick(channelId, BotResponses.PlynlingPetLines.For(plynling.Gender)), PlynlingCardUi.SafeName(plynling.Name));
        return new CareReply(PlynlingModule.BuildCard(plynling, now, $"{line} — {PlynlingText.PettedBy(plynling.Gender, actorId)}"), null);
    }

    public async Task<CareReply> FeedAsync(int plynlingId, ulong actorId, PlynlingFood food, ulong channelId, DateTimeOffset now)
    {
        var result = await _plynlings.FeedAsync(plynlingId, actorId, food, now);
        if (result.Outcome != CareOutcome.Done || result.Plynling is null)
        {
            return new CareReply(null, result.Outcome == CareOutcome.TooPoor
                ? PlynlingText.TooPoor(result.Price, result.Balance)
                : Refusal(result.Outcome, result.Plynling?.Gender ?? PlynlingGender.Male));
        }

        var info = PlynlingCatalog.Info(food);
        var line = string.Format(_picker.Pick(channelId, BotResponses.PlynlingFeedLines.For(result.Plynling.Gender)),
            PlynlingCardUi.SafeName(result.Plynling.Name), info.WithArticle);
        return new CareReply(PlynlingModule.BuildCard(result.Plynling, now,
            $"{line}\n-# −{PebbleEconomy.Cailloux(info.Price)} · il te reste {PebbleEconomy.Cailloux(result.Balance)}",
            PlynlingArt.Food(food)), null);
    }

    // Every refusal but NoPlynling comes back with the Plynling loaded; NoPlynling's line
    // names no Plynling, so the fallback gender never shows.
    public static string Refusal(CareOutcome outcome, PlynlingGender gender) => outcome switch
    {
        CareOutcome.NoPlynling => PlynlingText.NoPlynling,
        CareOutcome.NotOwner => PlynlingText.NotYours(gender),
        CareOutcome.Dead => PlynlingText.Dead(gender),
        CareOutcome.Frozen => PlynlingText.Frozen(gender),
        CareOutcome.Wasted => PlynlingText.Wasted(gender),
        CareOutcome.Asleep => PlynlingText.Asleep(gender),
        _ => PlynlingText.Unknown,
    };
}
