using Discord;
using Discord.Interactions;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Commands;

// /minecraft: her Minecraft life, seen from Discord (docs/agents/minecraft.md, /minecraft). Thin over
// MinecraftBodyService, which owns everything about the game; the builders here are static and Context-free so the
// checks can measure them. Read-only answers are ephemeral, to keep channels clean.
[Group("minecraft", "Le serveur Minecraft : son mode d'emploi, où elle en est, et l'y inviter")]
[CommandContextType(InteractionContextType.Guild)]
public class MinecraftModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly MinecraftBodyService _minecraft;
    private readonly MinecraftDataService _data;

    public MinecraftModule(MinecraftBodyService minecraft, MinecraftDataService data)
    {
        _minecraft = minecraft;
        _data = data;
    }

    // ---- link -----------------------------------------------------------------------------------

    // No option: shows the member's link. name: issues a code to whisper to her in game. remove: deletes the member's
    // link, or (the owner only) the link of `player`. A name linked to someone else is refused, and Papa's and Tata's
    // names only go to their own Discord accounts: offline mode lets anyone log in under any name.
    [SlashCommand("link", "Lier ton compte Discord à ton pseudo Minecraft")]
    public async Task LinkAsync(
        [Summary("name", "Ton pseudo en jeu"), MaxLength(16)] string? name = null,
        [Summary("remove", "Retirer ton lien")] bool remove = false,
        [Summary("player", "Le pseudo dont retirer le lien (propriétaire du bot seulement)"), MaxLength(16)] string? player = null)
    {
        await DeferAsync(ephemeral: true);
        var userId = Context.User.Id;

        if (remove)
        {
            if (player is not null)
            {
                if (userId != AvailabilityService.OwnerId)
                {
                    await Reply("Seul Rodhengard peut retirer le lien de quelqu'un d'autre.");
                    return;
                }
                await Reply(await _data.LinkOfPlayerAsync(player) is { } theirs && await _data.RemoveLinkAsync(theirs.UserId)
                    ? $"Lien de {Format.Sanitize(theirs.Player)} retiré." : "Ce pseudo n'est lié à personne.");
                return;
            }
            await Reply(await _data.RemoveLinkAsync(userId) ? "Ton lien est retiré." : "Tu n'avais pas de compte lié.");
            return;
        }

        if (name is null)
        {
            await Reply(await _data.LinkOfUserAsync(userId) is { } mine
                ? $"Ton compte est lié à **{Format.Sanitize(mine.Player)}**."
                : "Aucun compte lié. Utilise l'option `name` pour en lier un.");
            return;
        }

        name = name.Trim();
        if (!McLinkCodes.ValidName(name))
        {
            await Reply("Ce n'est pas un pseudo Minecraft valide (3 à 16 lettres, chiffres ou _).");
            return;
        }
        var family = _minecraft.KinOfPlayer(name) switch
        {
            McKin.Papa => userId != AvailabilityService.OwnerId,
            McKin.Tata => userId != BotResponses.TataId,
            _ => false,
        };
        if (family)
        {
            await Reply("Ce pseudo est réservé : il ne peut être lié qu'à un compte précis.");
            return;
        }
        if (await _data.LinkOfPlayerAsync(name) is { } taken && taken.UserId != userId)
        {
            await Reply("Ce pseudo est déjà lié à un autre compte.");
            return;
        }
        if (!_minecraft.IsSpawned)
        {
            await Reply("Elle doit être sur le serveur pour vérifier : réessaie quand elle y est.");
            return;
        }
        var code = _minecraft.LinkCodes.Issue(userId, name, DateTimeOffset.UtcNow);
        await Reply($"Chuchote-lui en jeu, depuis **{Format.Sanitize(name)}** : `/msg {_minecraft.Username} lien {code}` " +
                    $"(valable {McLinkCodes.Lifetime.TotalMinutes:0} minutes).");
    }

    // ---- invite ---------------------------------------------------------------------------------

    // A linked member brings her onto the server for an hour (McInvite), under the owner's switch and leave hold.
    [SlashCommand("invite", "La faire venir sur le serveur pour une heure (compte lié requis)")]
    public async Task InviteAsync()
    {
        await DeferAsync(ephemeral: true);
        if (await _data.LinkOfUserAsync(Context.User.Id) is not { } link)
        {
            await Reply("Lie d'abord ton compte avec `/minecraft link`.");
            return;
        }
        await Reply(_minecraft.Invite(link.Player) switch
        {
            McInviteVerdict.Go => "Elle arrive. Elle reste au moins une heure.",
            McInviteVerdict.NotAllowed => "Elle n'a pas le droit de jouer en ce moment.",
            McInviteVerdict.Held => "On vient de la faire partir : réessaie un peu plus tard.",
            McInviteVerdict.AlreadyIn => "Elle est déjà sur le serveur.",
            McInviteVerdict.ServerDown => "Le serveur ne répond pas : réessaie plus tard.",
            _ => "Elle est déjà invitée, elle arrive.",
        });
    }

    // ---- stats ----------------------------------------------------------------------------------

    [SlashCommand("stats", "Ce qu'elle a fait sur le serveur depuis le début")]
    public async Task StatsAsync()
    {
        await DeferAsync(ephemeral: true);
        var totals = await _data.StatsAsync();
        // What the service counted since its last write, so the answer is up to date.
        foreach (var (key, n) in _minecraft.PendingStats()) totals[key] = totals.GetValueOrDefault(key) + n;
        await FollowupAsync(embed: BuildStats(totals, _minecraft.PlacesCount, MinecraftBodyService.ResourceLabel), ephemeral: true,
            allowedMentions: AllowedMentions.None);
    }

    // How many players the stats show by orders given.
    private const int TopPlayers = 5;

    /// <summary>Her stats as one embed. <paramref name="resourceLabel"/> names a gathered resource in French.</summary>
    public static Embed BuildStats(IReadOnlyDictionary<string, long> totals, int placesRemembered, Func<string, string> resourceLabel)
    {
        long Get(string key) => totals.GetValueOrDefault(key);
        var minutes = Get(McStats.Minutes);
        var embed = new EmbedBuilder()
            .WithTitle("SYNCS sur Minecraft — ses stats")
            .WithColor(Color.Green)
            .AddField("Sa vie", $"Temps en jeu : **{minutes / 60} h {minutes % 60:00}**\nMorts : **{Get(McStats.Deaths)}**\n" +
                                $"Monstres tués en protégeant quelqu'un : **{Get(McStats.Kills)}**", inline: true)
            .AddField("Son travail", $"Poissons pêchés : **{Get(McStats.Fish)}** (trésors : **{Get(McStats.Treasure)}**)\n" +
                                     $"Récoltes : **{Get(McStats.Harvested)}**\nLieux retenus : **{placesRemembered}**", inline: true);

        var gathered = totals.Where(t => t.Key.StartsWith(McStats.GatheredPrefix) && t.Value > 0)
            .OrderByDescending(t => t.Value)
            .Select(t => $"{Format.Sanitize(resourceLabel(t.Key[McStats.GatheredPrefix.Length..]))} : **{t.Value}**")
            .ToList();
        embed.AddField("Ce qu'elle a ramassé", gathered.Count == 0 ? "Rien pour l'instant." : string.Join("\n", gathered.Take(15)));

        var orders = totals.Where(t => t.Key.StartsWith(McStats.OrdersByPrefix) && t.Value > 0)
            .OrderByDescending(t => t.Value).ThenBy(t => t.Key)
            .Take(TopPlayers)
            .Select(t => $"{Format.Sanitize(t.Key[McStats.OrdersByPrefix.Length..])} : **{t.Value}**")
            .ToList();
        embed.AddField($"Ordres reçus : {Get(McStats.Orders)}",
            orders.Count == 0 ? "Personne ne lui a encore rien demandé." : string.Join("\n", orders));
        return embed.WithFooter($"Project S.Y.N.C.S. v{AppInfo.Version}").Build();
    }

    private Task Reply(string text) => FollowupAsync(text, ephemeral: true, allowedMentions: AllowedMentions.None);

    [SlashCommand("help", "Ce qu'elle comprend en jeu, avec des exemples à taper")]
    public Task HelpAsync() => RespondAsync(embed: BuildHelp(0), components: BuildHelpButtons(0), ephemeral: true);

    [SlashCommand("status", "Le serveur, qui est connecté, et ce qu'elle fait en ce moment")]
    public Task StatusAsync() => RespondAsync(_minecraft.PublicStatusText(), ephemeral: true, allowedMentions: AllowedMentions.None);

    [SlashCommand("inventory", "Ce qu'elle porte et ce qu'elle a dans les poches")]
    public Task InventoryAsync() => RespondAsync(_minecraft.InventoryText(), ephemeral: true, allowedMentions: AllowedMentions.None);

    // Waits on the body (a few seconds at most): deferred first.
    [SlashCommand("where", "Où elle est sur le serveur")]
    public async Task WhereAsync()
    {
        await DeferAsync(ephemeral: true);
        await FollowupAsync(await _minecraft.WhereAsync(WhereTimeout), ephemeral: true, allowedMentions: AllowedMentions.None);
    }

    private static readonly TimeSpan WhereTimeout = TimeSpan.FromSeconds(3);

    // ---- help -----------------------------------------------------------------------------------

    // The manual's sections (McHelp.Sections), split in two pages: each embed of a message counts towards the 6000.
    private const int FirstPageSections = 4;
    public const int HelpPages = 2;

    /// <summary>One page of the manual. Its custom-ids (minecraft:help:N) are read by MinecraftHelpHandler.</summary>
    public static Embed BuildHelp(int page)
    {
        var sections = page == 0 ? McHelp.Sections.Take(FirstPageSections) : McHelp.Sections.Skip(FirstPageSections);
        var embed = new EmbedBuilder()
            .WithTitle($"SYNCS sur Minecraft — ce qu'elle comprend ({page + 1}/{HelpPages})")
            .WithDescription("Écris-lui dans le chat du jeu. Ces phrases ne sont que des exemples : elle comprend aussi les variantes.")
            .WithColor(Color.Green)
            .WithFooter($"Project S.Y.N.C.S. v{AppInfo.Version}");
        foreach (var s in sections)
            embed.AddField(s.Title, s.Blurb + "\n" + string.Join("\n", s.Examples.Select(e => $"• `{e.Text}`")));
        return embed.Build();
    }

    public static MessageComponent BuildHelpButtons(int page) =>
        new ComponentBuilder()
            .WithButton("◀", $"minecraft:help:{page - 1}", ButtonStyle.Secondary, disabled: page <= 0)
            .WithButton("▶", $"minecraft:help:{page + 1}", ButtonStyle.Secondary, disabled: page >= HelpPages - 1)
            .Build();
}
