using System.Text;
using System.Text.RegularExpressions;
using Discord;
using Discord.Interactions;
using Discord.Net;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Interactions.Autocomplete;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Commands;

public partial class DebugModule : InteractionModuleBase<SocketInteractionContext>
{
    // Sickness is rare by design and could take days to reach, so the owner can force it — and a
    // low hygiene — on his own living Plynling to test the card, the art and the refusals. The
    // hygiene default (-1) means « unchanged »: Discord only enforces 0–100 on what is typed.
    [SlashCommand("plynling", "Régler l'hygiène ou la maladie de ton propre Plynling (tests)")]
    public async Task PlynlingAsync(
        [Summary("hygiene", "Hygiène en %")] [MinValue(0)] [MaxValue(100)] int hygiene = -1,
        [Summary("sick", "Malade ou non")] SickState sick = SickState.Unchanged)
    {
        if (Context.User.Id != AvailabilityService.OwnerId)
        {
            await RespondAsync("Seul Rodhengard peut utiliser cette commande.", ephemeral: true);
            return;
        }
        if (Context.Guild is null)
        {
            await RespondAsync("Sur un serveur, pas en message privé.", ephemeral: true);
            return;
        }

        var p = await _plynlings.DebugSetAsync(Context.Guild.Id, Context.User.Id,
            hygiene >= 0 ? hygiene / 100.0 : null,
            sick switch { SickState.Sick => true, SickState.Healthy => false, _ => null },
            DateTimeOffset.UtcNow);
        await RespondAsync(p is null
                ? PlynlingText.NoPlynling
                : $"🔧 **{PlynlingCardUi.SafeName(p.Name)}** : hygiène {Math.Round(p.Hygiene * 100)} %, " +
                  $"{(PlynlingLife.IsSick(p) ? "malade" : "en bonne santé")}.",
            ephemeral: true, allowedMentions: AllowedMentions.None);
    }

    // Events come once a day: the owner forces one on his own living Plynling — waiting, already
    // expired (the next sweep decides it alone), or decided at once with its story posted here.
    // Ignores the pulse, the cap, the stage and target conditions; a social event takes the chosen
    // target's living Plynling, or any other.
    [SlashCommand("event", "Forcer un événement sur ton propre Plynling (tests)")]
    public async Task EventAsync(
        [Summary("key", "Clé de l'événement")] string key,
        [Summary("mode", "Quand le décider")] EventMode mode = EventMode.Pending,
        [Summary("target", "Le propriétaire de l'autre Plynling")] IUser? target = null)
    {
        if (Context.User.Id != AvailabilityService.OwnerId)
        {
            await RespondAsync("Seul Rodhengard peut utiliser cette commande.", ephemeral: true);
            return;
        }
        if (Context.Guild is null)
        {
            await RespondAsync("Sur un serveur, pas en message privé.", ephemeral: true);
            return;
        }
        if (PlynlingEvents.ByKey(key) is not { } def)
        {
            await RespondAsync("Clés : " + string.Join(", ", PlynlingEvents.All.Select(e => $"`{e.Key}`")), ephemeral: true);
            return;
        }
        await DeferAsync(ephemeral: true);
        var now = DateTimeOffset.UtcNow;
        var p = await _plynlings.GetCurrentAsync(Context.Guild.Id, Context.User.Id, now);
        if (p is null)
        {
            await FollowupAsync(PlynlingText.NoPlynling, ephemeral: true);
            return;
        }
        // A chosen other, or any. A Response forced this way lands on your Plynling, the target as the
        // one who asked — a quick way to test answering.
        var social = def.Target != TargetKind.None || def.Type == EventType.Response;
        var targetId = !social ? null
            : target is not null ? (await _plynlings.GetCurrentAsync(Context.Guild.Id, target.Id, now))?.Id
            : await _plynlings.AnyOtherLivingIdAsync(p);
        var inst = await _plynlings.CreateEventAsync(p, def, targetId, now);
        if (mode == EventMode.Expired) await _plynlings.ExpireEventNowAsync(inst.Id, now);
        if (mode == EventMode.Now)
        {
            await _plynlings.ResolveAloneAsync(p, inst, now);
            await _plynlings.SaveAsync();
            await _announcer.TellAsync(_plynlings, new[] { inst.Id }, Context.Guild.Id, now, Context.Channel);
        }
        await FollowupAsync($"🔧 `{def.Key}` créé (#{inst.Id}, {mode}).", ephemeral: true);
    }

    // The hourly Plynling sweep, now, for every guild: settle, traits owed, due events, today's pulse,
    // deaths and warnings, as the loop would. Never a second pass alongside a running one.
    [SlashCommand("sweep", "Lancer tout de suite le passage horaire des Plynlings (tests)")]
    public async Task SweepAsync()
    {
        if (Context.User.Id != AvailabilityService.OwnerId)
        {
            await RespondAsync("Seul Rodhengard peut utiliser cette commande.", ephemeral: true);
            return;
        }
        // A pass reads and writes every Plynling: far past Discord's 3 s on the Pi.
        await DeferAsync(ephemeral: true);
        var count = await _sweep.RunPassAsync(wait: false);
        await FollowupAsync(count is { } n
                ? $"🔧 Passage terminé : {n} Plynling{(n > 1 ? "s" : "")} vu{(n > 1 ? "s" : "")}."
                : "🔧 Un passage est déjà en cours. Réessaie dans un instant.",
            ephemeral: true);
    }

    // Quokka shows up now in this guild's bots channel, skipping the day's slots and the
    // room-is-awake check: to see a visit without waiting for one.
    [SlashCommand("quokka", "Faire venir Quokka tout de suite dans le salon des bots (tests)")]
    public async Task QuokkaAsync()
    {
        if (Context.User.Id != AvailabilityService.OwnerId)
        {
            await RespondAsync("Seul Rodhengard peut utiliser cette commande.", ephemeral: true);
            return;
        }
        if (Context.Guild is null)
        {
            await RespondAsync("Sur un serveur, s'il te plaît : Quokka n'entre pas dans les DM.", ephemeral: true);
            return;
        }
        await DeferAsync(ephemeral: true);
        await FollowupAsync(await _quokka.SpawnAsync(Context.Guild.Id)
                ? "🔧 Quokka est arrivé dans le salon des bots."
                : "🔧 Pas de visite : Quokka est déjà là, ou il n'y a pas de salon où l'envoyer.",
            ephemeral: true);
    }

    // Gives someone a role, with her own permissions. Refuses up front what Discord would answer with a
    // bare 403: she lacks ManageRoles, or the role is @everyone, managed, or not below her top role.
    [SlashCommand("role", "Donner un rôle à quelqu'un (tests)")]
    public async Task RoleAsync(
        [Summary("user", "Qui reçoit le rôle")] IUser user,
        [Summary("role", "Le rôle à donner")] IRole role)
    {
        if (Context.User.Id != AvailabilityService.OwnerId)
        {
            await RespondAsync("Seul Rodhengard peut utiliser cette commande.", ephemeral: true);
            return;
        }
        if (Context.Guild is null)
        {
            await RespondAsync("Sur un serveur, pas en message privé.", ephemeral: true);
            return;
        }

        var me = Context.Guild.CurrentUser;
        string? refusal = null;
        if (!me.GuildPermissions.ManageRoles)
            refusal = "Il me manque la permission « Gérer les rôles ».";
        else if (role.Id == Context.Guild.Id || role.IsManaged)
            refusal = "Ce rôle ne peut pas être donné à la main.";
        else if (role.Position >= me.Hierarchy)
            refusal = "Ce rôle est au même niveau que mon rôle le plus haut, ou au-dessus.";
        else if (Context.Guild.GetUser(user.Id) is not { } member)
            refusal = "Cette personne n'est pas sur le serveur.";
        else if (member.Roles.Any(r => r.Id == role.Id))
            refusal = "Cette personne a déjà ce rôle.";
        if (refusal is not null)
        {
            await RespondAsync($"🔧 {refusal}", ephemeral: true);
            return;
        }

        await DeferAsync(ephemeral: true);
        try
        {
            await Context.Guild.GetUser(user.Id).AddRoleAsync(role);
            _logger.LogInformation("Owner gave role {RoleId} to {UserId}.", role.Id, user.Id);
            await FollowupAsync($"🔧 {Format.Sanitize(role.Name)} donné à {MentionUtils.MentionUser(user.Id)}.",
                allowedMentions: AllowedMentions.None, ephemeral: true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to give role {RoleId} to {UserId}.", role.Id, user.Id);
            await FollowupAsync("🔧 Discord a refusé (voir les logs).", ephemeral: true);
        }
    }

    // Any of her sticker strips, posted here as the follow-up would post it, bypassing the
    // roll: to see each strip on PC and on the phone. The glitch strip too, which no line can tag.
    [SlashCommand("sticker", "Poster ici une image de SYNCS, au choix de l'humeur (tests)")]
    public async Task StickerAsync(
        [Summary("mood", "L'humeur de l'image")]
        [Autocomplete(typeof(StickerMoodAutocompleteHandler))] string mood)
    {
        if (Context.User.Id != AvailabilityService.OwnerId)
        {
            await RespondAsync("Seul Rodhengard peut utiliser cette commande.", ephemeral: true);
            return;
        }
        if (!Sticker.AllStrips.Contains(mood))
        {
            await RespondAsync($"Humeur inconnue : {Format.Sanitize(mood)}.", ephemeral: true);
            return;
        }

        // The upload can outrun Discord's 3 s.
        await DeferAsync(ephemeral: true);
        var posted = await _stickers.PostAsync(Context.Channel, mood);
        await FollowupAsync(
            posted ? "Envoyé." : "Échec de l'envoi (droit d'envoyer des fichiers ? voir les logs).",
            ephemeral: true);
    }

    // Voyager's correspondence (AmbientService): his light time now, the next letter and its
    // answer, the light-day; and, on demand, any of its lines posted here, bypassing the
    // calendar and the state — the only way to see them on the dev guild.
    [SlashCommand("voyager", "Voir où en est Voyager, ou poster ici une ligne de sa correspondance (tests)")]
    public async Task VoyagerAsync(
        [Summary("post", "Une ligne à poster ici")] VoyagerPost post = VoyagerPost.None)
    {
        if (Context.User.Id != AvailabilityService.OwnerId)
        {
            await RespondAsync("Seul Rodhengard peut utiliser cette commande.", ephemeral: true);
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var next = AppTime.DayNumber(now);
        // A letter comes every 18 days at most: this ends quickly.
        while (Voyager.LetterNumber(next) is null) next++;
        var number = Voyager.LetterNumber(next)!.Value;
        var slot = Voyager.LetterSlot(next);
        var light = Voyager.LightTime(now);
        var state =
            $"Temps-lumière : **{light.TotalMinutes:0.0} min** (« {Voyager.Format(light)} »)\n" +
            $"Prochaine lettre : n° {number}, le {DateOnly.FromDayNumber(next):dd/MM/yyyy} à {slot.ToString(@"hh\:mm")}" +
            (Voyager.AnswerSlot(next) is { } a ? $", réponse à {a.ToString(@"hh\:mm")}" : ", sans réponse") + "\n" +
            $"Jour-lumière : {Voyager.CrossingDay:dd/MM/yyyy}";

        if (post == VoyagerPost.None)
        {
            await RespondAsync(state, ephemeral: true);
            return;
        }

        // The typing pause can outrun Discord's 3 s.
        await DeferAsync(ephemeral: true);
        var line = post switch
        {
            VoyagerPost.Letter => AmbientService.VoyagerLetter(number, now),
            VoyagerPost.Answer => AmbientService.VoyagerAnswer(number),
            _ => BotResponses.VoyagerCrossingLine,
        };
        var sent = await BotChat.PostWithTypingAsync(Context.Channel, line, _logger, "debug Voyager line", AllowedMentions.None);
        await FollowupAsync(state + (sent is null ? "\nÉchec de l'envoi (voir les logs)." : "\nEnvoyé."), ephemeral: true);
    }

    // The pop quiz on demand, in this guild's quiz channel: post a round now (the next in the
    // rotation, or the question named by `key`), or close the open one as the hour running out
    // would. Skips the slots, the daily count and the quiet clock; never opens a second round.
    [SlashCommand("quiz", "Poser ou fermer un quiz tout de suite (tests)")]
    public async Task QuizAsync(
        [Summary("action", "Poser une question, fermer celle en cours, ou couronner la grosse tête")] QuizAction action = QuizAction.Post,
        [Summary("key", "Clé de la question (par défaut : la suivante de la rotation)")]
        [Autocomplete(typeof(QuizKeyAutocompleteHandler))] string? key = null)
    {
        if (Context.User.Id != AvailabilityService.OwnerId)
        {
            await RespondAsync("Seul Rodhengard peut utiliser cette commande.", ephemeral: true);
            return;
        }
        if (Context.Guild is null)
        {
            await RespondAsync("Sur un serveur, pas en message privé.", ephemeral: true);
            return;
        }

        await DeferAsync(ephemeral: true);
        var result = action switch
        {
            QuizAction.Close => await _quiz.CloseNowAsync(Context.Guild.Id),
            QuizAction.Crown => await _crown.CrownNowAsync(Context.Guild),
            _ => await _quiz.PostNowAsync(Context.Guild.Id, key),
        };
        await FollowupAsync(result, ephemeral: true, allowedMentions: AllowedMentions.None);
    }

    // Stress builds over days of the owner's own choices: set it directly to test the levels, the
    // card and a mental break (raising it past a level queues the break, as in play).
    [SlashCommand("stress", "Régler le stress de ton propre Plynling (tests)")]
    public async Task StressAsync([Summary("value", "0 à 400")] [MinValue(0)] [MaxValue(400)] int value)
    {
        if (Context.User.Id != AvailabilityService.OwnerId)
        {
            await RespondAsync("Seul Rodhengard peut utiliser cette commande.", ephemeral: true);
            return;
        }
        if (Context.Guild is null)
        {
            await RespondAsync("Sur un serveur, pas en message privé.", ephemeral: true);
            return;
        }
        await DeferAsync(ephemeral: true);
        var result = await _plynlings.DebugStressAsync(Context.Guild.Id, Context.User.Id, value, DateTimeOffset.UtcNow);
        await FollowupAsync(result ?? PlynlingText.NoPlynling, ephemeral: true, allowedMentions: AllowedMentions.None);
    }

    [SlashCommand("modifier", "Ajouter ou retirer un modificateur sur ton propre Plynling (tests)")]
    public async Task ModifierAsync([Summary("key", "Clé du modificateur")] string key, [Summary("remove", "Le retirer")] bool remove = false)
    {
        if (Context.User.Id != AvailabilityService.OwnerId)
        {
            await RespondAsync("Seul Rodhengard peut utiliser cette commande.", ephemeral: true);
            return;
        }
        if (Context.Guild is null)
        {
            await RespondAsync("Sur un serveur, pas en message privé.", ephemeral: true);
            return;
        }
        if (PlynlingModifiers.ByKey(key) is not { } mod)
        {
            await RespondAsync("Clés : " + string.Join(", ", PlynlingModifiers.All.Select(m => $"`{m.Key}`")), ephemeral: true);
            return;
        }
        await DeferAsync(ephemeral: true);
        var ok = await _plynlings.DebugModifierAsync(Context.Guild.Id, Context.User.Id, mod, remove, DateTimeOffset.UtcNow);
        await FollowupAsync(ok ? $"🔧 `{key}` {(remove ? "retiré" : "appliqué")}." : PlynlingText.NoPlynling, ephemeral: true);
    }

    // Adding an emote to Helpers/Core/Emotes.cs meant copying its name and snowflake by hand. This
    // writes the entries for every emote of this server that isn't declared yet, ready to paste.
    // Short results go out as a code block (one tap to copy); long ones as a file, since a
    // message is capped at 2000 characters.
    [SlashCommand("emotes", "Générer le code des émotes du serveur absentes d'Emotes.cs")]
    public async Task EmotesAsync()
    {
        if (Context.User.Id != AvailabilityService.OwnerId)
        {
            await RespondAsync("Seul Rodhengard peut utiliser cette commande.", ephemeral: true);
            return;
        }
        if (Context.Guild is null)
        {
            await RespondAsync("Sur un serveur, pas en message privé.", ephemeral: true);
            return;
        }

        var (snippet, count) = EmoteSnippet.Build(Context.Guild.Emotes);
        if (count == 0)
        {
            await RespondAsync("Toutes les émotes du serveur sont déjà dans `Emotes.cs`. ✅", ephemeral: true);
            return;
        }

        var header = $"**{count}** émote(s) à ajouter dans `Helpers/Core/Emotes.cs` :";
        if (snippet.Length <= 1800)
        {
            await RespondAsync($"{header}\n```csharp\n{snippet}\n```", ephemeral: true);
            return;
        }

        await DeferAsync(ephemeral: true);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(snippet));
        await FollowupWithFileAsync(stream, "Emotes.generated.txt", header, ephemeral: true);
    }

    // What people say to her, to decide what she should learn next: how many of each kind of chat
    // answer she gave (ChatAnswerService, by path), or the messages she found nothing specific to
    // say to over the period (UnansweredLog: kept 30 rolling days, no authors — « depuis toujours »
    // means those 30 days), as a file since they exceed a message. Ephemeral, under her
    // report-for-duty line.
    [SlashCommand("answers", "Ce qu'on me dit le plus, et ce que je sais pas encore répondre")]
    public async Task AnswersAsync(
        [Summary("period", "La période comptée")] AnswersPeriod period = AnswersPeriod.Week,
        [Summary("view", "Les comptes, ou les messages eux-mêmes")] AnswersView view = AnswersView.Types)
    {
        if (Context.User.Id != AvailabilityService.OwnerId)
        {
            await RespondAsync("Seul Rodhengard peut utiliser cette commande.", ephemeral: true);
            return;
        }
        if (Context.Guild is null)
        {
            await RespondAsync("Sur un serveur, pas en message privé.", ephemeral: true);
            return;
        }

        if (view == AnswersView.Examples)
        {
            // Only 30 days are kept, so « depuis toujours » is those 30 days.
            var days = period == AnswersPeriod.Week ? 7 : UnansweredLog.KeptDays;
            await DeferAsync(ephemeral: true);
            var messages = await _answers.GetUnansweredAsync(Context.Guild.Id, days);
            if (messages.Count == 0)
            {
                await FollowupAsync($"Aucun message sans réponse précise sur les {days} derniers jours.", ephemeral: true);
                return;
            }

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(AnswerStatsCards.BuildExamples(messages, days)));
            var olderNote = period == AnswersPeriod.AllTime ? $" (je ne garde que les {UnansweredLog.KeptDays} derniers jours)" : "";
            await FollowupWithFileAsync(stream, "sans-reponse.txt",
                $"{_picker.Pick(BotResponses.OwnerReportLines)}\n**{messages.Count}** message(s) sans réponse précise " +
                $"sur les {days} derniers jours{olderNote}, du plus récent au plus ancien.",
                allowedMentions: AllowedMentions.None, ephemeral: true);
            return;
        }

        // The database can outrun Discord's 3 s on the Pi.
        await DeferAsync(ephemeral: true);
        var statsPeriod = period switch
        {
            AnswersPeriod.Month => StatsPeriod.Month,
            AnswersPeriod.AllTime => StatsPeriod.AllTime,
            _ => StatsPeriod.Week,
        };
        var counts = await _answers.GetAsync(Context.Guild.Id, statsPeriod);
        await FollowupAsync(_picker.Pick(BotResponses.OwnerReportLines),
            embed: AnswerStatsCards.BuildTypes(counts, statsPeriod), allowedMentions: AllowedMentions.None, ephemeral: true);
    }

    // The monthly digest (MonthlyDigestService) for last month, in your DMs now: yours, or a preview of
    // Tata's (sent to you, not to her). It doesn't touch the digest's state: the ones on the 1st still go out.
    [SlashCommand("digest", "Recevoir tout de suite en DM le rapport du mois dernier (tests)")]
    public async Task DigestAsync(
        [Summary("version", "Ta version, ou un aperçu de celle de Tata")] DigestRecipient version = DigestRecipient.Owner)
    {
        if (Context.User.Id != AvailabilityService.OwnerId)
        {
            await RespondAsync("Seul Rodhengard peut utiliser cette commande.", ephemeral: true);
            return;
        }

        // Several reports read the database: well past Discord's 3 s on the Pi.
        await DeferAsync(ephemeral: true);
        var (year, month) = MonthlyDigest.PreviousMonth(DateTimeOffset.UtcNow);
        var sent = await _digest.SendAsync(version, year, month, toOwner: true);
        await FollowupAsync(sent ? "📬 Rapport envoyé en DM." : "Impossible de t'écrire en DM (voir les logs).", ephemeral: true);
    }

    // Her body on the Minecraft server (MinecraftBodyService): where it stands, what she carries, what her body is doing
    // and how its process copes (asked of the body, so deferred), a join or leave past
    // her rule, or the switch that allows her on the server at all (kept across restarts, off until
    // allowed). A forced leave keeps her out for half an hour; a forced join still follows the rule
    // once she's in, and needs her to be allowed.
    [SlashCommand("minecraft", "Voir où en est SYNCS sur le serveur Minecraft, ou l'y envoyer (tests)")]
    public async Task MinecraftAsync(
        [Summary("action", "Voir l'état ou l'inventaire, la faire rejoindre ou partir")] MinecraftAction action = MinecraftAction.Status)
    {
        if (Context.User.Id != AvailabilityService.OwnerId)
        {
            await RespondAsync("Seul Rodhengard peut utiliser cette commande.", ephemeral: true);
            return;
        }

        // The body answers through the game's event loop, up to 3 s: past Discord's own 3 s.
        if (action == MinecraftAction.Body)
        {
            await DeferAsync(ephemeral: true);
            await FollowupAsync(await _minecraft.DebugBodyAsync(TimeSpan.FromSeconds(3)), ephemeral: true);
            return;
        }

        var text = action switch
        {
            MinecraftAction.Join => _minecraft.ForceJoin(),
            MinecraftAction.Leave => _minecraft.ForceLeave(),
            MinecraftAction.Allow => _minecraft.SetAllowed(true),
            MinecraftAction.Forbid => _minecraft.SetAllowed(false),
            MinecraftAction.Inventory => _minecraft.InventoryText(),
            _ => _minecraft.StatusText(),
        };
        await RespondAsync(text, ephemeral: true);
    }

    [SlashCommand("absent", "Activer ou désactiver ton mode absent")]
    public async Task SetAbsenceAsync(
        [Summary("state", "Activer ou désactiver le mode absent")] AbsenceState state)
    {
        if (Context.User.Id != AvailabilityService.OwnerId)
        {
            await RespondAsync("Seul Rodhengard peut utiliser cette commande.", ephemeral: true);
            return;
        }

        bool absent = state == AbsenceState.On;
        _availability.SetOwnerAbsent(absent);

        await RespondAsync(
            absent
                ? $"Mode absent **activé**. Je préviendrai poliment quiconque te mentionne. {Emotes.Sparkle}"
                : "Mode absent **désactivé**. Tu es de nouveau disponible (˶˃ ᵕ ˂˶)",
            ephemeral: true);
    }

    [SlashCommand("silence", "La faire taire : elle ne parle plus que pour répondre aux commandes")]
    public async Task SetSilenceAsync(
        [Summary("state", "Activer ou désactiver le mode silence")] SilenceState state)
    {
        if (Context.User.Id != AvailabilityService.OwnerId)
        {
            await RespondAsync("Seul Rodhengard peut utiliser cette commande.", ephemeral: true);
            return;
        }

        bool silent = state == SilenceState.On;
        var since = _silence.Since;
        _silence.Set(silent);

        string reply = (silent, since) switch
        {
            (true, { } already) => $"Je suis déjà muette, depuis le <t:{already.ToUnixTimeSeconds()}:f>. Chut.",
            (true, null) => "Mode silence **activé**. Plus de bavardage, de réactions, de bonjour ni d'annonces : "
                            + "je ne parle que pour répondre aux commandes. Les rappels de sessions, les tirages "
                            + "et les `/remindme` partent toujours. Même après un redémarrage. 🤐",
            (false, { } began) => $"Mode silence **désactivé** : j'étais muette depuis le <t:{began.ToUnixTimeSeconds()}:f>. "
                                  + "J'ai plein de choses à dire (˶˃ ᵕ ˂˶)",
            (false, null) => "Je n'étais pas en mode silence. Je parle, je parle.",
        };
        await RespondAsync(reply, ephemeral: true);
    }
}
