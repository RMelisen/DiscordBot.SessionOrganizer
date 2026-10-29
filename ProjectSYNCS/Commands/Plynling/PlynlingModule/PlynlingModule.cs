using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Interactions.Modals;
using ProjectSYNCS.Models;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Commands;

// /plynling — adopting, looking after and (for staff) managing Plynlings. Guild-only:
// every query is scoped to Context.Guild.Id.
//
// No DeferAsync anywhere: every action is one or two row reads and one write, well inside
// Discord's 3 s, and not deferring is what lets a success be a *public* V2 card while a
// refusal stays *private* — a public "thinking…" cannot become an ephemeral reply.
//
// Abstract and group-less on purpose: the commands are registered twice, under /plynling and
// under the /pl shortcut, by the two sealed groups in PlynlingGroups.cs. Discord.Net picks up
// inherited [SlashCommand]s and skips abstract modules, so a command added here lands in both.
// The modal handlers are *not* attributed here — two copies of one custom-id would be two
// owners — and are wired up by PlynlingLongModule only.
[CommandContextType(InteractionContextType.Guild)]
public abstract partial class PlynlingModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly PlynlingService _plynlings;
    private readonly PlynlingRoleService _roles;
    private readonly ResponsePicker _picker;
    private readonly PlynlingAnnouncer _announcer;
    private readonly PlynlingCooldowns _cooldowns;
    private readonly ShameService _shame;
    private readonly PlynlingPlayService _play;
    private readonly CosmeticService _cosmetics;
    private readonly PlynlingVisitRunner _visits;
    private readonly ILogger<PlynlingModule> _logger;

    protected PlynlingModule(PlynlingService plynlings, ResponsePicker picker,
        PlynlingAnnouncer announcer, PlynlingCooldowns cooldowns, ShameService shame, PlynlingPlayService play,
        CosmeticService cosmetics, PlynlingVisitRunner visits, PlynlingRoleService roles, ILogger<PlynlingModule> logger)
    {
        _roles = roles;
        _visits = visits;
        _play = play;
        _cosmetics = cosmetics;
        _plynlings = plynlings;
        _picker = picker;
        _announcer = announcer;
        _cooldowns = cooldowns;
        _shame = shame;
        _logger = logger;
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
        if (_cooldowns.AdoptBlockedUntil(Context.Guild.Id, Context.User.Id, now) is { } ready)
        {
            await RespondAsync(PlynlingText.AdoptCooldown(ready), ephemeral: true);
            return;
        }
        // Only mushrooms are adoptable for now: the sunflower species exist in the catalog but
        // stay dormant until families ship (the plan's deferred Task 3 adds the family: option).
        var species = PlynlingCatalog.RollSpecies(PlynlingFamily.Mushroom);
        await DeferAsync();
        var (outcome, plynling) = await _plynlings.AdoptAsync(Context.Guild.Id, Context.User.Id, name, species, now);
        if (outcome != AdoptOutcome.Adopted || plynling is null)
        {
            await RefuseAsync(PlynlingText.AlreadyHasOne);
            return;
        }

        var info = PlynlingCatalog.Info(plynling.Species);  // not `species`: a launch arrangement may replace it
        var pool = BotResponses.PlynlingAdoptLines.For(plynling.Gender);
        var line = string.Format(_picker.Pick(pool),
            PlynlingCardUi.SafeName(plynling.Name), info.Name);
        await RespondCardAsync(plynling, now, line);
        await _roles.SyncMemberAsync(Context.Guild, Context.User.Id);   // after the card: never delays it, never throws
    }

    // A command rather than a card button, and confirmed by typing the name: nobody should
    // lose a Plynling to a misclick. It is meant to cost a little pride — a public
    // announcement, L'Indigne on /shame, and 30 minutes before adopting again.
    [SlashCommand("abandon", "Abandonner ton Plynling — pour toujours, et tout le monde le saura")]
    public async Task AbandonAsync()
    {
        var plynling = await _plynlings.GetCurrentAsync(Context.Guild.Id, Context.User.Id, DateTimeOffset.UtcNow);
        if (plynling is null || plynling.DiedAt is not null)
        {
            await RespondAsync(PlynlingText.NoPlynling, ephemeral: true);
            return;
        }
        await RespondWithModalAsync<AbandonModal>($"plyn:abandon:{plynling.Id}");
    }

    // Bound to "plyn:abandon:*" by PlynlingLongModule.
    protected async Task OnAbandonConfirmedAsync(string idStr, AbandonModal modal)
    {
        var now = DateTimeOffset.UtcNow;
        var plynling = int.TryParse(idStr, out var id) ? await _plynlings.GetByIdAsync(id, now) : null;
        if (plynling is null || plynling.OwnerId != Context.User.Id || plynling.DiedAt is not null)
        {
            await RespondAsync(PlynlingText.NoPlynling, ephemeral: true);
            return;
        }
        if (!PlynlingCardUi.NamesMatch(modal.Name, plynling.Name))
        {
            await RespondAsync(PlynlingText.AbandonMismatch(plynling.Gender), ephemeral: true);
            return;
        }

        var gone = await _plynlings.AbandonAsync(plynling.Id, Context.User.Id, now);
        if (gone is null)
        {
            await RespondAsync(PlynlingText.NoPlynling, ephemeral: true);
            return;
        }
        _cooldowns.MarkAbandoned(Context.Guild.Id, Context.User.Id, now);
        await RespondAsync(PlynlingText.AbandonDone(gone.Gender, PlynlingCardUi.SafeName(gone.Name)), ephemeral: true);

        try
        {
            await _shame.AddAbandonHitAsync(Context.Guild.Id, Context.User.Id);
        }
        catch (Exception ex)
        {
            // The abandonment already happened; a missed shame point must not undo it.
            _logger.LogWarning(ex, "Failed to record the abandon shame for {UserId}.", Context.User.Id);
        }
        await _announcer.AnnounceAbandonAsync(gone, now);
        await _roles.SyncMemberAsync(Context.Guild, Context.User.Id);
    }

    // Your own Plynling only, one game an hour — claimed here, at the start, so abandoning a
    // game never rolls a new one. The game itself runs in PlynlingComponentHandler.
    [SlashCommand("play", "Jouer avec ton Plynling — un mini-jeu au hasard, une fois par heure")]
    public async Task PlayAsync()
    {
        var now = DateTimeOffset.UtcNow;
        var plynling = await _plynlings.GetCurrentAsync(Context.Guild.Id, Context.User.Id, now);
        string? refusal =
            plynling is null ? PlynlingText.NoPlynling
            : plynling.DiedAt is not null ? PlynlingText.Dead(plynling.Gender)
            : plynling.FrozenAt is not null ? PlynlingText.Frozen(plynling.Gender)
            : PlynlingLife.IsAsleep(now) ? PlynlingText.Asleep(plynling.Gender)
            : PlynlingLife.IsSick(plynling) ? PlynlingText.SickNoPlay(plynling.Gender)      // before the claim: no cooldown spent
            : !_cooldowns.Play.TryClaim(plynling.Id) ? PlynlingText.PlayCooldown(plynling.Gender)
            : null;
        if (refusal is not null || plynling is null)
        {
            await RespondAsync(refusal, ephemeral: true);
            return;
        }

        var session = _play.Start(Context.Guild.Id, Context.User.Id, plynling.Id, now, Random.Shared);
        await RespondAsync(components: PlynlingPlayCards.BuildGame(session, plynling, now, null),
            flags: MessageFlags.ComponentsV2, allowedMentions: AllowedMentions.None);
    }

    // An invitation, not a visit: it only happens if the other owner presses « Accueillir »
    // (PlynlingComponentHandler), within the hour. This message is the one line that pings —
    // the invited owner only, since it is addressed to them.
    [SlashCommand("visit", "Emmener ton Plynling rendre visite à celui de quelqu'un")]
    public async Task VisitAsync([Summary("user", "Chez qui aller")] IUser user)
    {
        var now = DateTimeOffset.UtcNow;
        var opening = await OpenVisitAsync(_plynlings, _cooldowns, _visits, _picker, Context.Guild.Id, Context.User.Id, user, now);
        if (opening.Refusal is not null || opening.Card is null)
        {
            await RespondAsync(opening.Refusal, ephemeral: true, allowedMentions: AllowedMentions.None);
            return;
        }

        if (opening.Story is { } story)
        {
            await RespondAsync(components: opening.Card, flags: MessageFlags.ComponentsV2, allowedMentions: AllowedMentions.None);
            try
            {
                await FollowupAsync(components: PlynlingPlayCards.BuildVisitStory(story, 0),
                    flags: MessageFlags.ComponentsV2, allowedMentions: AllowedMentions.None);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Visit story {Story}: could not be posted", story.Id);
            }
            // What the visit left behind at her home, decided by her at once — after the visit's story.
            await _announcer.TellAsync(_plynlings, opening.Untold ?? [], Context.Guild.Id, now, Context.Channel);
            return;
        }

        await RespondAsync(components: opening.Card,
            flags: MessageFlags.ComponentsV2, allowedMentions: new AllowedMentions { UserIds = new List<ulong> { user.Id } });
    }

    /// <summary>
    /// Everything a visit request decides before anything is sent — the checks, the knock, or at her
    /// home the visit itself — shared by /plynling visit and the <c>;pl visit</c> shortcut so the two
    /// cannot drift. Each caller only sends what it returns, with its own mention rules.
    /// </summary>
    public static async Task<VisitOpening> OpenVisitAsync(PlynlingService plynlings, PlynlingCooldowns cooldowns,
        PlynlingVisitRunner visits, ResponsePicker picker, ulong guildId, ulong visitorId, IUser host, DateTimeOffset now)
    {
        if (host.Id == visitorId) return new VisitOpening(PlynlingText.VisitSelf);

        var mine = await plynlings.GetCurrentAsync(guildId, visitorId, now);
        // Bots have no Plynling — except her, whose Ping-Qilin can be visited.
        var theirs = host.IsBot && host.Id != PlynlingMascot.OwnerId ? null : await plynlings.GetCurrentAsync(guildId, host.Id, now);
        // In memory for a knock a moment ago, stored on their relation for a visit before a restart.
        var visitedToday = mine is not null && theirs is not null
            && (cooldowns.VisitedToday(visitorId, host.Id, AppTime.DayKey(now)) || await plynlings.VisitedTodayAsync(mine.Id, theirs.Id, now));
        string? refusal =
            mine is null ? PlynlingText.NoPlynling
            : mine.DiedAt is not null ? PlynlingText.Dead(mine.Gender)
            : theirs is null || theirs.DiedAt is not null ? PlynlingText.NoneFor(host.Id)
            : mine.FrozenAt is not null || theirs.FrozenAt is not null ? PlynlingText.VisitFrozen
            : PlynlingLife.IsAsleep(now) ? PlynlingText.Asleep(mine.Gender)
            : PlynlingLife.IsSick(mine) ? PlynlingText.VisitSick(PlynlingCardUi.SafeName(mine.Name))
            : PlynlingLife.IsSick(theirs) ? PlynlingText.VisitSick(PlynlingCardUi.SafeName(theirs.Name))
            : visitedToday ? PlynlingText.VisitedToday(host.Id)
            : null;
        if (refusal is not null || mine is null || theirs is null)
            return new VisitOpening(refusal ?? PlynlingText.NoPlynling);

        // At her home nobody could press « Accueillir », so she opens the door herself: no knock, no
        // ping, straight to the story — the same way « Accueillir » tells it.
        if (PlynlingMascot.Is(theirs))
        {
            var (story, visitRefusal, untold) = await visits.RunAsync(mine, theirs, now);
            if (story is null) return new VisitOpening(visitRefusal ?? PlynlingText.Unknown);
            var welcome = string.Format(picker.Pick(BotResponses.MascotWelcomeLines),
                PlynlingCardUi.SafeName(mine.Name), $"<@{visitorId}>");
            return new VisitOpening(null, PlynlingPlayCards.BuildKnockClosed($"{welcome}\n-# L'histoire est juste en dessous ↓"),
                story, untold);
        }

        var expires = now + PlynlingLife.VisitInviteLife;
        var (knockPool, typed) = PlynlingPassions.PickLines(BotResponses.PlynlingVisitKnockLines, BotResponses.PlynlingVisitKnockTypedLines, mine, Random.Shared);
        var line = string.Format(picker.Pick(knockPool),
            PlynlingCardUi.SafeName(mine.Name), $"<@{host.Id}>", typed);
        return new VisitOpening(null, PlynlingPlayCards.BuildKnock(mine, host.Id, expires, line, now));
    }

    // Its stats, badges and moments — the living one, or else the latest grave, like view.
    [SlashCommand("journal", "Le journal d'un Plynling : ses badges et ses souvenirs (le tien par défaut)")]
    public async Task JournalAsync([Summary("user", "À qui est le Plynling (par défaut : toi)")] IUser? user = null)
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
        var (badges, moments) = await _plynlings.GetJournalAsync(plynling.Id);
        var relations = await _plynlings.GetRelationsAsync(plynling.Id);
        await RespondAsync(components: PlynlingJournalCards.BuildJournal(plynling, badges, moments, 0, now, relations),
            flags: MessageFlags.ComponentsV2, allowedMentions: AllowedMentions.None);
    }

    // Everyone it has met and what they are to it — the living one, or else the latest grave.
    [SlashCommand("relations", "Les amis, amours et ennemis d'un Plynling (le tien par défaut)")]
    public async Task RelationsAsync([Summary("user", "À qui est le Plynling (par défaut : toi)")] IUser? user = null)
    {
        var target = user ?? Context.User;
        var plynling = await _plynlings.GetShownAsync(Context.Guild.Id, target.Id, DateTimeOffset.UtcNow);
        if (plynling is null)
        {
            await RespondAsync(target.Id == Context.User.Id ? PlynlingText.NoPlynling : PlynlingText.NoneFor(target.Id),
                ephemeral: true, allowedMentions: AllowedMentions.None);
            return;
        }
        var relations = await _plynlings.GetRelationsAsync(plynling.Id);
        await RespondAsync(embed: PlynlingJournalCards.BuildRelationsEmbed(plynling, relations), allowedMentions: AllowedMentions.None);
    }

    [SlashCommand("list", "Tous les Plynlings vivants du serveur, du plus vieux au plus jeune")]
    public async Task ListAsync()
    {
        var now = DateTimeOffset.UtcNow;
        var living = await _plynlings.GetLivingAsync(Context.Guild.Id, now);
        await RespondAsync(embed: BuildListEmbed(living, 0, now),
            components: BuildListButtons(0, ListPages(living.Count)), allowedMentions: AllowedMentions.None);
    }

    [SlashCommand("view", "Voir un Plynling (le tien par défaut)")]
    public async Task ViewAsync(
        [Summary("user", "À qui est le Plynling (par défaut : toi)")] IUser? user = null)
    {
        var target = user ?? Context.User;
        var now = DateTimeOffset.UtcNow;
        await DeferAsync();
        var plynling = await _plynlings.GetShownAsync(Context.Guild.Id, target.Id, now);
        if (plynling is null)
        {
            await RefuseAsync(target.Id == Context.User.Id ? PlynlingText.NoPlynling : PlynlingText.NoneFor(target.Id));
            return;
        }
        await RespondCardAsync(plynling, now, await ViewLineAsync(_plynlings, _picker, plynling, Context.User.Id, now));
    }

    // The line a looked-at card opens with — shared with the ;pl shortcut, so looking by text is
    // looking (the daily gift included).
    public static async Task<string?> ViewLineAsync(PlynlingService plynlings, ResponsePicker picker, Plynling plynling,
        ulong viewerId, DateTimeOffset now)
    {
        // The owner's first look of the day at a happy Plynling may turn up a gift.
        var gift = await plynlings.TryGiftAsync(plynling, viewerId, now, Random.Shared);
        // Otherwise, now and then, what its typed passion has it thinking about (a gift says enough).
        // Her own is introduced by her, every time — nobody else's card speaks for its owner.
        return gift.Any ? PlynlingCareService.GiftLine(plynling, gift)
            : PlynlingMascot.Is(plynling) && plynling.DiedAt is null
                ? string.Format(picker.Pick(BotResponses.MascotViewLines), PlynlingCardUi.SafeName(plynling.Name))
            : PlynlingPassions.Thought(plynling, now, Random.Shared) is (var pool, var typed)
                ? string.Format(picker.Pick(pool), PlynlingCardUi.SafeName(plynling.Name), typed)
                : null;
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
        await DeferAsync();
        var (outcome, plynling) = await _plynlings.FreezeAsync(Context.Guild.Id, target.Id, byStaff, now);
        if (outcome != FreezeOutcome.Frozen || plynling is null)
        {
            // Every refusal but NoPlynling carries the Plynling, so its gender is known.
            var g = plynling?.Gender ?? PlynlingGender.Male;
            await RefuseAsync(outcome switch
            {
                FreezeOutcome.NoPlynling => byStaff ? PlynlingText.NoneFor(target.Id) : PlynlingText.NoPlynling,
                FreezeOutcome.Dead => PlynlingText.Dead(g),
                FreezeOutcome.AlreadyFrozen => PlynlingText.AlreadyFrozen(g),
                FreezeOutcome.TooHungry => PlynlingText.TooHungryToFreeze(g),
                FreezeOutcome.Sick => PlynlingText.SickNoFreeze(g),
                FreezeOutcome.Cooldown => PlynlingText.FreezeCooldown(g, plynling!.LastSelfThawAt!.Value + PlynlingLife.SelfFreezeCooldown),
                _ => PlynlingText.Unknown,
            });
            return;
        }

        await RespondCardAsync(plynling, now, PlynlingText.FrozenNotice(plynling.Gender, PlynlingCardUi.SafeName(plynling.Name), plynling.FreezeUntil));
        // After the card, never before: the card is what the person is waiting for, and a DM is two
        // or three REST calls that may fail.
        if (byStaff)
            await _announcer.DmOwnerAsync(plynling.OwnerId, string.Format(
                _picker.Pick(BotResponses.PlynlingStaffFreezeDms.For(plynling.Gender)), PlynlingCardUi.SafeName(plynling.Name)));
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
        await DeferAsync();
        var (outcome, plynling) = await _plynlings.ThawAsync(Context.Guild.Id, target.Id, asStaff, now);
        if (outcome != ThawOutcome.Thawed || plynling is null)
        {
            var g = plynling?.Gender ?? PlynlingGender.Male;
            await RefuseAsync(outcome switch
            {
                ThawOutcome.NoPlynling => target.Id == Context.User.Id ? PlynlingText.NoPlynling : PlynlingText.NoneFor(target.Id),
                ThawOutcome.Dead => PlynlingText.Dead(g),
                ThawOutcome.NotFrozen => PlynlingText.NotFrozen(g),
                ThawOutcome.StaffOnly => PlynlingText.ThawStaffOnly(g),
                _ => PlynlingText.Unknown,
            });
            return;
        }

        await RespondCardAsync(plynling, now, PlynlingText.ThawedNotice(plynling.Gender, PlynlingCardUi.SafeName(plynling.Name)));
        if (target.Id != Context.User.Id)   // after the reply — see FreezeAsync
            await _announcer.DmOwnerAsync(plynling.OwnerId, string.Format(
                _picker.Pick(BotResponses.PlynlingStaffThawDms.For(plynling.Gender)), PlynlingCardUi.SafeName(plynling.Name)));
    }

    [SlashCommand("forage", "Envoyer ton Plynling fouiller les environs — un objet ou de quoi manger, toutes les 4 h")]
    public async Task ForageAsync()
    {
        var now = DateTimeOffset.UtcNow;
        var result = await _plynlings.ForageAsync(Context.Guild.Id, Context.User.Id, now, Random.Shared);
        var (text, refused) = ForageReply(result, Context.User.Id);
        if (refused)
            await RespondAsync(text, ephemeral: true);
        else
            await RespondAsync(text, allowedMentions: AllowedMentions.None);
    }

    // What /plynling forage answers, shared with the ;forage shortcut. A refusal is shown privately —
    // or as a passing reply, from the shortcut.
    public static (string Text, bool Refused) ForageReply(ForageResult result, ulong userId)
    {
        var gender = result.Plynling?.Gender ?? PlynlingGender.Male;
        if (result.Outcome == CareOutcome.TooSoon)
            return (PlynlingText.ForageTooSoon(gender, result.ReadyAt!.Value), true);
        if (result.Outcome != CareOutcome.Done || result.Find is null || result.Plynling is null)
            return (PlynlingCareService.Refusal(result.Outcome, gender), true);
        // A plain message, not a card: the find is the news, and the Plynling's state has not changed.
        return (PlynlingText.FindLines(
            PlynlingText.Foraged(PlynlingCardUi.SafeName(result.Plynling.Name), gender, result.Find.Item), result.Find, userId), false);
    }

    [SlashCommand("graveyard", "Le cimetière des Plynlings (ou seulement ceux de quelqu'un)")]
    public async Task GraveyardAsync(
        [Summary("user", "Seulement les tombes de cette personne")] IUser? user = null)
    {
        var now = DateTimeOffset.UtcNow;
        var graves = await _plynlings.GetGraveyardAsync(Context.Guild.Id, user?.Id, now);
        await RespondAsync(components: PlynlingGraveyardCards.BuildPage(graves, GraveSort.Recent, user?.Id ?? 0, 0, now),
            flags: MessageFlags.ComponentsV2, allowedMentions: AllowedMentions.None);
    }

    [SlashCommand("wardrobe", "Habiller ton Plynling avec tes cosmétiques")]
    public async Task WardrobeAsync()
    {
        var plynling = await _plynlings.GetCurrentAsync(Context.Guild.Id, Context.User.Id, DateTimeOffset.UtcNow);
        if (plynling is null || plynling.DiedAt is not null)
        {
            await RespondAsync(PlynlingText.NoPlynling, ephemeral: true);
            return;
        }
        var owned = await _cosmetics.OwnedAsync(Context.Guild.Id, Context.User.Id);
        var (embed, components) = CosmeticCards.BuildWardrobe(plynling, owned, null);
        await RespondAsync(embed: embed, components: components, ephemeral: true, allowedMentions: AllowedMentions.None);
    }

    // Free text, like a Tomodachi Life word: it only ever appears through generic story
    // templates, or upgraded to a catalog passion when PlynlingPassions.Resolve recognises it.
    [SlashCommand("passion", "Apprendre une passion à ton Plynling (ou effacer celle apprise)")]
    public async Task PassionAsync()
    {
        var now = DateTimeOffset.UtcNow;
        var plynling = await _plynlings.GetCurrentAsync(Context.Guild.Id, Context.User.Id, now);
        if (plynling is null || plynling.DiedAt is not null)
        {
            await RespondAsync(PlynlingText.NoPlynling, ephemeral: true);
            return;
        }
        if (plynling.TaughtPassionAt is { } at && at + PlynlingPassions.TeachCooldown > now)
        {
            await RespondAsync(PlynlingText.PassionCooldown(at + PlynlingPassions.TeachCooldown), ephemeral: true);
            return;
        }
        // Hand-built to pre-fill it; binds to PassionModal — keep the two in sync.
        var modal = new ModalBuilder()
            .WithTitle("Sa passion")
            .WithCustomId($"plyn:passion:{plynling.Id}")
            .AddTextInput("Quelle passion veux-tu lui apprendre ?", "passion",
                placeholder: "Avec son article : la pêche, les trains…", maxLength: InputCaps.Passion,
                required: false, value: plynling.TaughtPassion)
            .Build();
        await RespondWithModalAsync(modal);
    }

    // Bound to "plyn:passion:*" by PlynlingLongModule.
    protected async Task OnPassionTaughtAsync(string idStr, PassionModal modal)
    {
        var now = DateTimeOffset.UtcNow;
        var cleaned = PlynlingPassions.Clean(modal.Passion);
        if (cleaned.Length == 1)
        {
            await RespondAsync(PlynlingText.PassionTooShort, ephemeral: true);
            return;
        }
        if (PlynlingPassions.ContainsLink(cleaned))
        {
            await RespondAsync(PlynlingText.PassionLink, ephemeral: true);
            return;
        }

        var text = cleaned.Length == 0 ? null : cleaned;
        await DeferAsync();
        var (outcome, plynling, ready) = int.TryParse(idStr, out var id)
            ? await _plynlings.TeachPassionAsync(id, Context.User.Id, text, now)
            : (TeachOutcome.NoPlynling, null, null);
        switch (outcome)
        {
            case TeachOutcome.NoPlynling:
                await RefuseAsync(PlynlingText.NoPlynling);
                return;
            case TeachOutcome.Cooldown:
                await RefuseAsync(PlynlingText.PassionCooldown(ready!.Value));
                return;
            case TeachOutcome.Cleared:
                await RefuseAsync(PlynlingText.PassionCleared(PlynlingCardUi.SafeName(plynling!.Name)));
                return;
        }

        var line = string.Format(_picker.Pick(BotResponses.PlynlingPassionTaughtLines.For(plynling!.Gender)),
            PlynlingCardUi.SafeName(plynling.Name), PlynlingPassions.Taught(plynling)!.Render());
        await RespondCardAsync(plynling, now, line);
    }
}

/// <summary>
/// What <see cref="PlynlingModule.OpenVisitAsync"/> decided: a refusal; or a card to post — the knock,
/// or at her home the closed knock with the visit's <see cref="Story"/> and the event stories it left
/// <see cref="Untold"/>.
/// </summary>
public sealed record VisitOpening(string? Refusal, MessageComponent? Card = null, VisitStory? Story = null,
    IReadOnlyList<int>? Untold = null);
