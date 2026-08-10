using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Commands;

// The wall of shame. Three titles on one page: "Le Malfaisant" (hostility) and
// "Le Perfide" (consorting with rival bots), both earned and counted by ShameTracker,
// plus "Le Banni", voted through this same command.
//
// **Voting is staff-only, and that is what makes it a deterrent rather than a game.**
// Anyone may open the wall; only Administrator / ManageGuild holders, the owner, and a
// short hand-kept list may put someone on it. The cap is on the *target* — two votes a
// day, from everyone combined — so a person cannot be dogpiled even when every voter
// agrees. There is no per-voter quota: rationing moderators is not the point.
//
// One flat command with an optional `user` option rather than a group with
// subcommands, because Discord will not let a parent with subcommands be invoked bare
// — `/shame voir` and `/shame vote` would mean plain `/shame` no longer exists, and the
// board is the thing people open ten times for every vote cast.
//
// Components V2, like /level and /leaderboard, because each title shows its holder's
// avatar and an embed has one thumbnail slot for the whole message. That flag is
// all-or-nothing: a ComponentsV2 message carries NO content and NO embeds, so this
// replaced the embed rather than decorating it, and OnPeriodAsync has to re-assert the
// flag on every UpdateAsync or the edit is rejected. /emotestats and /goodbot stay
// paged embeds — they rank emotes and verdicts, which have no face to show.
//
// Guild-only: this module reads Context.Guild, which is null in a DM, and config.yaml
// ships register_globally: true (a global command is DM-enabled by default).
[CommandContextType(InteractionContextType.Guild)]
public class ShameModule : InteractionModuleBase<SocketInteractionContext>
{
    // The wall opens on 30 days. A title is a standing, but "Le Malfaisant" starts at
    // zero the day this ships and both counters are sparse, so all-time would read as
    // a permanent hall of fame that nobody can move — a month is long enough to have
    // names in it and short enough that the wall still changes.
    private const StatsPeriod DefaultPeriod = StatsPeriod.Month;

    // Fixed lines, deliberately not ResponsePicker pools: the pools exist so repeated
    // *chatter* doesn't repeat itself, and a command refusal is not chatter. Same call
    // as /level's refusal for a bot.
    private const string RefusalSelfTarget =
        "Non. Tu ne me mets pas au mur. C'est mon mur ദ്ദി◝ ⩊ ◜.ᐟ";

    private const string RefusalBotTarget =
        "Les autres bots n'ont pas d'honneur à perdre. Garde ton vote pour un vrai coupable ✨";

    private const string RefusalTargetLimit =
        "Cette personne a déjà pris son quota du jour. Laisse-la respirer jusqu'à demain (ᵕ • ᴗ •)";

    private const string RefusalNotAllowed =
        "Le vote est réservé au staff. Toi tu peux regarder le mur, c'est déjà beaucoup ( ˶ˆ ᗜ ˆ˵ )";

    // Who may cast a vote, on top of anyone with Administrator / ManageGuild (and the
    // owner) — see SessionPermissions.IsStaff. Literal snowflakes tied to this one
    // server, like AvailabilityService.OwnerId and XpTracker.ExcludedChannels.
    //
    // The list exists because "moderator" here is a matter of trust rather than of
    // Discord permissions: these people are trusted with the vote without being given
    // ManageGuild, which would hand them the whole server.
    //
    // **Kept in force even once a moderator role is configured**, deliberately: the
    // configured role *adds* voters, it never removes them. Configuring a role that
    // happens to omit one of these people must not silently revoke access they already
    // had — a config change should not be able to take something away by accident.
    private static readonly HashSet<ulong> ExtraVoters = new()
    {
        177049957818302464,
        324768221372743681,
        345917214966415362,
        573225362532859935,
    };

    private readonly ShameService _shame;
    private readonly ResponsePicker _picker;
    private readonly GuildConfigService _config;

    public ShameModule(ShameService shame, ResponsePicker picker, GuildConfigService config)
    {
        _shame = shame;
        _picker = picker;
        _config = config;
    }

    // Staff, one of the hardcoded names above, or a holder of the guild's configured
    // moderator role — whichever comes first. The role is the only part that touches
    // the database, and only when the two free checks have already said no.
    private async Task<bool> CanVoteAsync(IUser user)
    {
        if (SessionPermissions.IsStaff(user) || ExtraVoters.Contains(user.Id)) return true;

        var config = await _config.GetAsync(Context.Guild.Id);
        if (config.ModeratorRoleId == 0) return false;

        return user is SocketGuildUser member
            && member.Roles.Any(r => r.Id == config.ModeratorRoleId);
    }

    [SlashCommand("shame", "Le mur de la honte — ou dénonce quelqu'un pour l'y mettre")]
    public async Task ShameAsync(
        [Summary("user", "La personne à dénoncer. Laisse vide pour voir le mur.")]
        SocketUser? user = null)
    {
        if (user is not null)
        {
            await VoteAsync(user);
            return;
        }

        await DeferAsync();
        await FollowupAsync(
            components: await BuildWallAsync(DefaultPeriod),
            flags: MessageFlags.ComponentsV2,
            allowedMentions: AllowedMentions.None);
    }

    // Only one button row exists on this message, so nothing can collide with it — but
    // the id still carries its own verb (`:win:`) rather than being bare, because the
    // day a second row is added the collision is silent, instant, and rejected by
    // Discord as COMPONENT_CUSTOM_ID_DUPLICATED. StatsPeriodUi appends the page
    // segment, which this board has no use for and ignores.
    [ComponentInteraction("shame:win:*:*", ignoreGroupNames: true)]
    public async Task OnPeriodAsync(string periodStr, string _)
    {
        if (!Enum.TryParse<StatsPeriod>(periodStr, out var period)) period = DefaultPeriod;

        var components = await BuildWallAsync(period);

        var component = (SocketMessageComponent)Context.Interaction;
        await component.UpdateAsync(m =>
        {
            m.Components = components;
            // Re-asserted on every edit: the flag is a property of the message, and an
            // update that omitted it would be rejected against a ComponentsV2 message.
            m.Flags = MessageFlags.ComponentsV2;
            m.AllowedMentions = AllowedMentions.None;
        });
    }

    private async Task VoteAsync(SocketUser target)
    {
        // Refusals are ephemeral: only the person who tried needs to know, and a public
        // "you can't do that" would be a second message about a vote that never
        // happened.
        //
        // Checked before anything else, so someone who may not vote learns that rather
        // than which of the other rules they also broke.
        if (!await CanVoteAsync(Context.User))
        {
            await RespondAsync(RefusalNotAllowed, ephemeral: true);
            return;
        }

        if (target.Id == Context.Client.CurrentUser.Id)
        {
            await RespondAsync(RefusalSelfTarget, ephemeral: true);
            return;
        }

        if (target.IsBot)
        {
            await RespondAsync(RefusalBotTarget, ephemeral: true);
            return;
        }

        await DeferAsync();

        var result = await _shame.TryVoteAsync(Context.Guild.Id, target.Id);
        if (result == ShameVoteResult.TargetLimitReached)
        {
            await FollowupAsync(RefusalTargetLimit, ephemeral: true);
            return;
        }

        var voterName = NameOf(Context.User);
        var targetName = NameOf(target);

        // Self-shaming is allowed and gets its own pool: a line about two different
        // people cannot land when both of them are the same person.
        var line = Context.User.Id == target.Id
            ? string.Format(_picker.Pick(Context.Channel.Id, BotResponses.ShameSelfVoteLines), voterName)
            : string.Format(_picker.Pick(Context.Channel.Id, BotResponses.ShameVoteLines), voterName, targetName);

        // Names are rendered as text, not mentions: the announcement is public and
        // pinging the person you just shamed turns a joke into a notification.
        await FollowupAsync(line, allowedMentions: AllowedMentions.None);
    }

    // Components V2, and the budget is the thing to watch. Discord allows 40 per message
    // counting the whole tree; this uses **26**:
    //
    //   container 1 + heading 1
    //   + 4 titles x 5 (separator, Section, its TextDisplay, the avatar Thumbnail,
    //     and one TextDisplay for the runners-up)  = 20
    //   + action row 1 + its three filter buttons 3
    //
    // So there is room for two more titles and no more. Only the *holder* wears an
    // avatar — giving every ranked row one costs three components each and would blow
    // the budget immediately, for the sake of putting a face on people who did not win
    // the title. **Re-do this sum before adding anything here**, because the failure is
    // an exception inside ComponentBuilderV2.Build() at send time, not a compile error.
    private async Task<MessageComponent> BuildWallAsync(StatsPeriod period)
    {
        var wall = await _shame.GetWallAsync(Context.Guild.Id, period);

        var container = new ContainerBuilder()
            .WithAccentColor(Color.DarkRed)
            .AddComponent(new TextDisplayBuilder(
                $"# Le mur de la honte\n-# {StatsPeriodUi.Label(period)}"));

        AddTitle(container, $"{Emotes.GooseKnife} Le Malfaisant",
            wall.Malfaisants, BotResponses.ShameEmptyMalfaisant, "méchanceté", "méchancetés");
        AddTitle(container, $"{Emotes.PrisonerFlat} Le Banni",
            wall.Bannis, BotResponses.ShameEmptyBanni, "vote", "votes");
        AddTitle(container, $"{Emotes.NightmareOtherEye} Le Perfide",
            wall.Perfides, BotResponses.ShameEmptyPerfide, "trahison", "trahisons");
        AddTitle(container, $"{Emotes.VeryAngry} L'Hystérique",
            wall.Hysteriques, BotResponses.ShameEmptyHysterique, "cri", "cris");

        return new ComponentBuilderV2()
            .AddComponent(container)
            .AddComponent(new ActionRowBuilder().AddFilterRow("shame:win", period))
            .Build();
    }

    // One title: its holder as a Section wearing their avatar, then the runners-up as a
    // plain line beneath.
    //
    // A title with nobody in it still renders, with a line in her voice and no avatar —
    // there is no face to show, and hiding the title outright would make the wall change
    // shape between filters, which reads as broken. There is no minimum count either: a
    // window holding one vote must show that vote, or the vote looks like it vanished.
    private void AddTitle(
        ContainerBuilder container,
        string heading,
        IReadOnlyList<ShameTally> rows,
        string[] emptyPool,
        string unit,
        string units)
    {
        container.AddComponent(new SeparatorBuilder());

        if (rows.Count == 0)
        {
            container.AddComponent(new TextDisplayBuilder(
                $"### {heading}\n*{_picker.Pick(Context.Channel.Id, emptyPool)}*"));
            return;
        }

        var holder = rows[0];

        container.AddComponent(new SectionBuilder()
            .WithAccessory(AvatarUi.Thumbnail(Context.Guild.GetUser(holder.UserId)))
            .AddComponent(new TextDisplayBuilder(
                $"### {heading}\n{Row(holder, 1, unit, units)}")));

        if (rows.Count > 1)
        {
            container.AddComponent(new TextDisplayBuilder(string.Join("\n",
                rows.Skip(1).Select((t, i) => Row(t, i + 2, unit, units)))));
        }
    }

    // A TextDisplay is real message content, so <@id> here genuinely pings — unlike the
    // embed this replaced, where mentions rendered inert for free. Every send and every
    // filter click passes AllowedMentions.None, which keeps the blue clickable pill and
    // silences it; without that the wall would notify everyone on it on every re-render.
    private static string Row(ShameTally tally, int rank, string unit, string units) =>
        $"{LevelCardUi.RankMarker(rank)} <@{tally.UserId}> — "
        + $"**{tally.Count}** {(tally.Count == 1 ? unit : units)}";

    private static string NameOf(IUser user) => BotResponses.DisplayNameFor(
        user.Id, (user as SocketGuildUser)?.Nickname ?? user.GlobalName ?? user.Username);
}
