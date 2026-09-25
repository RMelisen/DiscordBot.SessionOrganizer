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
[CommandContextType(InteractionContextType.Guild)]
[Group("plynling", "Ton Plynling : l'adopter, t'en occuper, le regarder vivre")]
public class PlynlingModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly PlynlingService _plynlings;
    private readonly ResponsePicker _picker;
    private readonly PlynlingAnnouncer _announcer;
    private readonly PlynlingCooldowns _cooldowns;
    private readonly ShameService _shame;
    private readonly PlynlingPlayService _play;
    private readonly ILogger<PlynlingModule> _logger;

    public PlynlingModule(PlynlingService plynlings, ResponsePicker picker,
        PlynlingAnnouncer announcer, PlynlingCooldowns cooldowns, ShameService shame, PlynlingPlayService play,
        ILogger<PlynlingModule> logger)
    {
        _play = play;
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

    [ModalInteraction("plyn:abandon:*", ignoreGroupNames: true)]
    public async Task OnAbandonConfirmedAsync(string idStr, AbandonModal modal)
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
        if (user.Id == Context.User.Id)
        {
            await RespondAsync(PlynlingText.VisitSelf, ephemeral: true);
            return;
        }

        var mine = await _plynlings.GetCurrentAsync(Context.Guild.Id, Context.User.Id, now);
        var theirs = user.IsBot ? null : await _plynlings.GetCurrentAsync(Context.Guild.Id, user.Id, now);
        string? refusal =
            mine is null ? PlynlingText.NoPlynling
            : mine.DiedAt is not null ? PlynlingText.Dead(mine.Gender)
            : theirs is null || theirs.DiedAt is not null ? PlynlingText.NoneFor(user.Id)
            : mine.FrozenAt is not null || theirs.FrozenAt is not null ? PlynlingText.VisitFrozen
            : PlynlingLife.IsAsleep(now) ? PlynlingText.Asleep(mine.Gender)
            : _cooldowns.VisitedToday(Context.User.Id, user.Id, AppTime.DayKey(now)) ? PlynlingText.VisitedToday(user.Id)
            : null;
        if (refusal is not null || mine is null)
        {
            await RespondAsync(refusal, ephemeral: true, allowedMentions: AllowedMentions.None);
            return;
        }

        var expires = now + PlynlingLife.VisitInviteLife;
        var line = string.Format(_picker.Pick(Context.Channel.Id, BotResponses.PlynlingVisitKnockLines.For(mine.Gender)),
            PlynlingCardUi.SafeName(mine.Name), $"<@{user.Id}>");
        await RespondAsync(components: PlynlingPlayCards.BuildKnock(mine, user.Id, expires, line, now),
            flags: MessageFlags.ComponentsV2, allowedMentions: new AllowedMentions { UserIds = new List<ulong> { user.Id } });
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
        var plynling = await _plynlings.GetShownAsync(Context.Guild.Id, target.Id, now);
        if (plynling is null)
        {
            await RespondAsync(target.Id == Context.User.Id ? PlynlingText.NoPlynling : PlynlingText.NoneFor(target.Id),
                ephemeral: true, allowedMentions: AllowedMentions.None);
            return;
        }
        // The owner's first look of the day at a happy Plynling may turn up a gift.
        var gift = await _plynlings.TryGiftAsync(plynling, Context.User.Id, now, Random.Shared);
        await RespondCardAsync(plynling, now, gift.Any ? PlynlingCareService.GiftLine(plynling, gift) : null);
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

    [SlashCommand("forage", "Envoyer ton Plynling fouiller les environs — un objet ou de quoi manger, toutes les 4 h")]
    public async Task ForageAsync()
    {
        var now = DateTimeOffset.UtcNow;
        var result = await _plynlings.ForageAsync(Context.Guild.Id, Context.User.Id, now, Random.Shared);
        var gender = result.Plynling?.Gender ?? PlynlingGender.Male;
        if (result.Outcome == CareOutcome.TooSoon)
        {
            await RespondAsync(PlynlingText.ForageTooSoon(gender, result.ReadyAt!.Value), ephemeral: true);
            return;
        }
        if (result.Outcome != CareOutcome.Done || result.Find is null || result.Plynling is null)
        {
            await RespondAsync(PlynlingCareService.Refusal(result.Outcome, gender), ephemeral: true);
            return;
        }
        // A plain message, not a card: the find is the news, and the Plynling's state has not changed.
        var line = PlynlingText.FindLines(
            PlynlingText.Foraged(PlynlingCardUi.SafeName(result.Plynling.Name), gender, result.Find.Item), result.Find, Context.User.Id);
        await RespondAsync(line, allowedMentions: AllowedMentions.None);
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
                "commune, peu commune, rare… ou légendaire. Garçon ou fille ? Surprise.\n" +
                "**`/plynling view [user]`** — Sa carte, avec les boutons **Caresser** et **Nourrir**.\n" +
                "**`/plynling list`** — Tous les Plynlings vivants du serveur, du plus vieux au plus jeune.\n" +
                "**`/plynling journal [user]`** — Son journal : ses badges et ses souvenirs. Il gagne des **badges** " +
                "en vieillissant, en jouant, en rendant visite et en étant choyé — chacun rapporte quelques cailloux.\n" +
                "Il grandit : **bébé** ses 2 premiers jours, **ado** jusqu'à 14 jours, **adulte**, " +
                "puis **ancien** après 6 mois. Le temps passé gelé ne compte pas.")
            .AddField("S'en occuper",
                "La **faim** se vide en **2 jours** : à 0 %, il meurt. Le **bonheur** se vide en **36 heures** " +
                "(il est juste triste).\n" +
                "**Nourrir** (menu de sa carte) — Champignon (15), Shiitake (30), Morille (40, que du bonheur), Truffe (80, faim et bonheur). " +
                "Nourrir celui d'un autre coûte le double. Si tu as ce plat dans ton **garde-manger**, il est servi de là " +
                "(2 pour celui d'un autre) au lieu de tes cailloux.\n" +
                "**Caresser** (bouton de sa carte) — +25 % de bonheur, toutes les 4 h, sur n'importe quel Plynling.\n" +
                "Pour s'occuper de celui de quelqu'un : `/plynling view user:`, puis sa carte.\n" +
                "Il **dort de 1 h à 5 h** : on peut le nourrir, pas le caresser, et il ne meurt jamais dans son sommeil.\n" +
                "Son **humeur** compte : heureux, un repas le nourrit 15 % de plus et il te rapporte parfois un caillou ou un objet ; " +
                "triste, 25 % de moins. À 0 %, il **boude** et refuse de manger tant qu'on n'a pas joué avec lui ou qu'on " +
                "ne l'a pas caressé — sauf s'il meurt de faim.")
            .AddField("Jouer & rendre visite",
                "**`/plynling play`** — Un mini-jeu au hasard avec ton Plynling : cache-cache, pierre-papier-ciseaux " +
                "ou plus ou moins. Une fois par heure : +15 % de bonheur, +25 % et quelques cailloux si tu gagnes.\n" +
                "**`/plynling visit user:`** — Ton Plynling toque chez quelqu'un. S'il l'accueille dans l'heure, les deux " +
                "gagnent du bonheur. Une visite par jour entre deux personnes.\n" +
                "À force de se voir, ils deviennent **amis**, **meilleurs amis**… ou **rivaux** et **ennemis**. " +
                "Un garçon et une fille très proches peuvent tomber **amoureux**. Plus ils s'aiment, plus leurs visites " +
                "les rendent heureux ; entre ennemis, elles les attristent.\n" +
                "**`/plynling relations [user]`** — Ses amis, ses amours et ses ennemis.")
            .AddField("Gagner des cailloux",
                "**`/work`** — 40 à 60 cailloux, toutes les 4 h.\n" +
                "Parler, réagir et le vocal rapportent aussi quelques cailloux (45 au plus par jour).\n" +
                "**`/balance`** — Ton solde, visible par toi seul.")
            .AddField("Inventaire & collection",
                "**`/plynling forage`** — Il part fouiller les environs (toutes les 4 h) et revient avec un objet — souvent un champignon — ou de quoi manger.\n" +
                "On trouve aussi des objets dans son cadeau du jour, en gagnant un jeu et pendant les bonnes visites. " +
                "Certains ne se trouvent qu'en une saison.\n" +
                "**`/inventory collection [user]`** — Le carnet, collection par collection : ce que tu as trouvé et ce qui manque. 4 collections de 8 objets et une grande de 30 champignons ; chacune complétée rapporte des cailloux.\n" +
                "**`/inventory view`** — Ton garde-manger et tes objets. **`/inventory shop food: quantity:`** remplit " +
                "le garde-manger (−10 % dès 5).\n" +
                "**`/inventory give`** · **`/inventory trade`** · **`/inventory sell`** — Offrir, échanger (l'offre dure 1 h) ou vendre.")
            .AddField("Partir en vacances",
                "**`/plynling freeze`** — Gèle ton Plynling (14 jours au plus) : plus rien ne bouge. " +
                "Seulement s'il a encore au moins 50 % de faim.\n" +
                "**`/plynling thaw`** — Le dégèle. Ensuite, 7 jours avant de pouvoir le regeler.")
            .AddField("La mort",
                "Tu reçois un **message privé** environ 3 h avant qu'il meure de faim (à 23 h la veille si ça tombe la nuit). " +
                "S'il meurt, tout le serveur l'apprend et il rejoint le cimetière.\n" +
                "**`/plynling graveyard [user]`** — Les tombes, triées par date ou par longueur de vie. Plus il a vécu, " +
                "plus sa tombe est belle.")
            .AddField("L'abandonner",
                "**`/plynling abandon`** — Il part pour toujours : pas de tombe, pas de retour. Tu devras taper son nom " +
                "pour confirmer. Tout le serveur l'apprendra, ça se verra sur `/shame`, et tu devras attendre 30 min " +
                "avant d'en adopter un autre.")
            .AddField("Staff",
                "**`/plynling freeze user:`** · **`/plynling thaw user:`** — Sur n'importe quel Plynling.\n" +
                "**`/admin plynling rename user: name:`** · **`/admin plynling resurrect user:`**")
            .WithFooter($"Project S.Y.N.C.S. v{AppInfo.Version}")
            .Build();

    // ---- rendering --------------------------------------------------------------

    private async Task RespondCardAsync(Plynling plynling, DateTimeOffset now, string? line)
    {
        var partner = await _plynlings.GetPartnerAsync(plynling);
        await RespondAsync(components: BuildCard(plynling, now, line, partnerName: partner?.Name),
            flags: MessageFlags.ComponentsV2, allowedMentions: AllowedMentions.None);
    }


    /// <summary>
    /// The card: picture (the sprite for its mood, or its memorial once dead), heading,
    /// bars, an optional line in her voice, and — only while alive and not frozen — a
    /// "Caresser" button and a "Nourrir…" select.
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
    /// The two rows use different verbs (<c>plyn:pet</c>, <c>plyn:feed</c>): duplicated
    /// custom ids are rejected outright by Discord, disabled components included.
    /// Nourrir is offered to everyone and refused in the handler for anyone but the
    /// owner — the real check is in code, as with every gate here.
    /// </remarks>
    public static MessageComponent BuildCard(
        Plynling plynling, DateTimeOffset now, string? lastAction, string? lastActionImage = null, string? partnerName = null)
    {
        var info = PlynlingCatalog.Info(plynling.Species);
        var alive = plynling.DiedAt is null;
        var picture = alive
            ? PlynlingArt.Sprite(plynling.Species, PlynlingLife.Stage(plynling, now), PlynlingLife.Mood(plynling, now))
            : PlynlingArt.Memorial(plynling.Species, PlynlingCatalog.MemorialTier(PlynlingLife.Age(plynling, now)));

        var container = new ContainerBuilder()
            .WithAccentColor(new Color(info.Accent))
            .AddComponent(new SectionBuilder()
                .WithAccessory(new ThumbnailBuilder()
                    .WithMedia(new UnfurledMediaItemProperties(picture))
                    .WithDescription(info.Name))
                .AddComponent(new TextDisplayBuilder(PlynlingCardUi.Heading(plynling, now, partnerName))))
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
            // No petting a sleeping Plynling — the button goes, feeding stays.
            if (!PlynlingLife.IsAsleep(now))
                builder.AddComponent(new ActionRowBuilder()
                    .WithButton("🤲 Caresser", $"plyn:pet:{plynling.Id}", ButtonStyle.Primary));

            var menu = new SelectMenuBuilder()
                .WithCustomId($"plyn:feed:{plynling.Id}")
                .WithPlaceholder("🍄 Nourrir…");
            foreach (var food in PlynlingCatalog.Foods)
                menu.AddOption($"{food.Name} — {PebbleEconomy.Cailloux(food.Price)}", food.Food.ToString(),
                    PlynlingCardUi.FoodOptionDescription(food));
            builder.AddComponent(new ActionRowBuilder().WithSelectMenu(menu));
        }
        return builder.Build();
    }
}
