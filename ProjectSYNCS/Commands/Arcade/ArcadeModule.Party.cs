using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Commands;

// The newer /arcade games: race, crash, blackjack, bomb, quote, temperature and the two boards.
// Same module (one /arcade group), second file. Their buttons are in the matching
// Interactions/Components handlers; what runs after the first click is PartyRunner's.
public partial class ArcadeModule
{
    private PartyRunner Party => _services.GetRequiredService<PartyRunner>();

    private string Pick(string[] pool, params object[] args) =>
        args.Length == 0 ? _picker.Pick(pool) : string.Format(_picker.Pick(pool), args);

    private static string Link(ArcadeGame g) => $"https://discord.com/channels/{g.GuildId}/{g.ChannelId}/{g.MessageId}";

    private async Task<bool> RefuseRunningAsync(ArcadeGame? blocking, string what)
    {
        if (blocking is null) return false;
        await RespondAsync($"{what} est déjà en cours{(blocking.MessageId == 0 ? "." : $" : {Link(blocking)}")}", ephemeral: true);
        return true;
    }

    // ---- Race -------------------------------------------------------------------------------------

    [SlashCommand("race", "Lancer une course de Plynlings, avec des paris")]
    public async Task RaceAsync()
    {
        if (await RefuseRunningAsync(_tables.All<RaceGame>().FirstOrDefault(r => r.GuildId == Context.Guild.Id), "Une course")) return;

        var now = DateTimeOffset.UtcNow;
        if (PlynlingLife.IsAsleep(now))
        {
            await RespondAsync(Pick(BotResponses.RaceAsleepLines), ephemeral: true);
            return;
        }

        // Settling the living can write: defer first. The refusals that follow are public, and short.
        await DeferAsync();
        var living = await _plynlings.GetLivingAsync(Context.Guild.Id, now);
        var eligible = living.Where(p => !PlynlingLife.IsFrozen(p) && !p.WarningSent).ToList();
        if (eligible.Count < Race.MinRunners)
        {
            await FollowupAsync(Pick(BotResponses.RaceNoRunnersLines));
            return;
        }

        var picked = eligible.OrderBy(_ => Random.Shared.Next()).Take(Race.MaxRunners).ToList();
        var runners = new List<RaceRunner>();
        foreach (var p in picked)
        {
            var courage = PlynlingStats.Compute(p, await _plynlings.GetTraitsAsync(p))
                .First(s => s.Stat == PlynlingStat.Courage).Total;
            runners.Add(new RaceRunner(p.Id, PlynlingCardUi.SafeName(p.Name), p.OwnerId,
                Race.Strength(courage, PlynlingLife.HappinessAt(p, now))));
        }

        var race = new RaceGame
        {
            GuildId = Context.Guild.Id, ChannelId = Context.Channel.Id, OrganizerId = Context.User.Id,
            StartsAt = now + Race.BettingTime, Runners = runners,
        };
        var (opened, blocking) = _tables.TryOpen(race, g => g is RaceGame && g.GuildId == race.GuildId);
        if (opened is null)
        {
            await FollowupAsync($"Une course est déjà en cours : {Link(blocking!)}");
            return;
        }

        try
        {
            race.Line = Pick(BotResponses.RaceIntroLines, $"<@{Context.User.Id}>");
            var message = await FollowupAsync(embed: PartyCards.RaceLobby(race), components: PartyCards.RaceLobbyComponents(race),
                allowedMentions: AllowedMentions.None);
            race.MessageId = message.Id;
            Party.ScheduleRace(race);
        }
        catch
        {
            _tables.End(race.Id);
            throw;
        }
    }

    // ---- Crash ------------------------------------------------------------------------------------

    [SlashCommand("crash", "Une fusée décolle : encaisse avant qu'elle explose")]
    public async Task CrashAsync()
    {
        var crash = new CrashGame
        {
            GuildId = Context.Guild.Id, ChannelId = Context.Channel.Id, OrganizerId = Context.User.Id,
            LobbyEndsAt = DateTimeOffset.UtcNow + Crash.LobbyTime, CrashPoint = Crash.CrashPoint(Random.Shared),
        };
        var (opened, blocking) = _tables.TryOpen(crash, g => g is CrashGame && g.GuildId == crash.GuildId);
        if (await RefuseRunningAsync(blocking, "Une fusée")) return;

        try
        {
            crash.Line = Pick(BotResponses.CrashIntroLines, $"<@{Context.User.Id}>");
            await RespondAsync(embed: PartyCards.CrashLobby(crash), components: PartyCards.CrashLobbyComponents(crash),
                allowedMentions: AllowedMentions.None);
            crash.MessageId = (await GetOriginalResponseAsync()).Id;
            Party.ScheduleCrash(opened!);
        }
        catch
        {
            _tables.End(crash.Id);
            throw;
        }
    }

    // ---- Blackjack --------------------------------------------------------------------------------

    [SlashCommand("blackjack", "Une main de blackjack contre moi")]
    public async Task BlackjackAsync(
        [Summary("stake", "Ta mise")] [MinValue(Blackjack.MinStake)] [MaxValue(Blackjack.MaxStake)] int stake)
    {
        var balance = await _arcade.BalanceAsync(Context.Guild.Id, Context.User.Id);
        if (balance < stake)
        {
            await RespondAsync($"Tu n'as que **{PebbleEconomy.Cailloux(balance)}**. Mise moins, ou va faire un `/work`.", ephemeral: true);
            return;
        }

        var game = new BlackjackGame { GuildId = Context.Guild.Id, ChannelId = Context.Channel.Id, PlayerId = Context.User.Id, Stake = stake };
        var (opened, blocking) = _tables.TryOpen(game, g => g is BlackjackGame b && b.PlayerId == Context.User.Id);
        if (await RefuseRunningAsync(blocking, "Ta main")) return;

        try
        {
            var rng = Random.Shared;
            game.Player.Add(Blackjack.Draw(rng));
            game.Dealer.Add(Blackjack.Draw(rng));
            game.Player.Add(Blackjack.Draw(rng));
            game.Dealer.Add(Blackjack.Draw(rng));
            game.Line = Pick(BotResponses.BlackjackDealLines, $"<@{Context.User.Id}>");

            // A natural ends the hand at once.
            if (Blackjack.IsNatural(game.Player) && _tables.End(game.Id))
            {
                await DeferAsync();
                var (embed, components) = await Party.FinishBlackjackAsync(game, bust: false);
                await FollowupAsync(embed: embed, components: components, allowedMentions: AllowedMentions.None);
                return;
            }

            await RespondAsync(embed: PartyCards.BlackjackTable(game, reveal: false), components: PartyCards.BlackjackButtons(game),
                allowedMentions: AllowedMentions.None);
            game.MessageId = (await GetOriginalResponseAsync()).Id;
            Party.WatchBlackjack(opened!.Id);
        }
        catch
        {
            _tables.End(game.Id);
            throw;
        }
    }

    // ---- Bomb -------------------------------------------------------------------------------------

    [SlashCommand("bomb", "Patate chaude : passe la bombe avant qu'elle explose (mise : 20)")]
    public async Task BombAsync()
    {
        if (await _arcade.BalanceAsync(Context.Guild.Id, Context.User.Id) < Bomb.Stake)
        {
            await RespondAsync($"Il faut **{PebbleEconomy.Cailloux(Bomb.Stake)}** pour jouer. Va faire un `/work`.", ephemeral: true);
            return;
        }

        var bomb = new BombGame
        {
            GuildId = Context.Guild.Id, ChannelId = Context.Channel.Id, OrganizerId = Context.User.Id,
            LobbyEndsAt = DateTimeOffset.UtcNow + Bomb.LobbyTime,
        };
        bomb.Players.Add(Context.User.Id);
        var (opened, blocking) = _tables.TryOpen(bomb, g => g is BombGame && g.GuildId == bomb.GuildId);
        if (await RefuseRunningAsync(blocking, "Une bombe")) return;

        try
        {
            bomb.Line = Pick(BotResponses.BombIntroLines, $"<@{Context.User.Id}>");
            await RespondAsync(embed: PartyCards.BombLobby(bomb), components: PartyCards.BombLobbyComponents(bomb),
                allowedMentions: AllowedMentions.None);
            bomb.MessageId = (await GetOriginalResponseAsync()).Id;
            Party.ScheduleBomb(opened!);
        }
        catch
        {
            _tables.End(bomb.Id);
            throw;
        }
    }

    // ---- Quote ------------------------------------------------------------------------------------

    [SlashCommand("quote", "Qui a dit ça ? Devine l'auteur d'un vieux message du serveur")]
    public async Task QuoteAsync()
    {
        if (await RefuseRunningAsync(_tables.All<QuoteRound>().FirstOrDefault(q => q.GuildId == Context.Guild.Id), "Une devinette")) return;

        // Digging through history takes a few calls: defer first.
        await DeferAsync();
        var found = await FindQuoteAsync(Context.Guild);
        if (found is null)
        {
            await FollowupAsync(Pick(BotResponses.QuoteNothingLines));
            return;
        }

        var (message, choices) = found.Value;
        var round = new QuoteRound
        {
            GuildId = Context.Guild.Id, ChannelId = Context.Channel.Id,
            AuthorId = message.Author.Id, Content = message.Content.Trim(), WrittenAt = message.Timestamp,
            SourceLink = message.GetJumpUrl(), Choices = choices,
        };
        var (opened, blocking) = _tables.TryOpen(round, g => g is QuoteRound && g.GuildId == round.GuildId);
        if (opened is null)
        {
            await FollowupAsync($"Une devinette est déjà en cours : {Link(blocking!)}");
            return;
        }

        try
        {
            round.Line = Pick(BotResponses.QuoteIntroLines);
            var card = await FollowupAsync(embed: PartyCards.QuoteCard(round), components: PartyCards.QuoteButtons(round),
                allowedMentions: AllowedMentions.None);
            round.MessageId = card.Id;
            Party.ScheduleQuote(round);
        }
        catch
        {
            _tables.End(round.Id);
            throw;
        }
    }

    /// <summary>
    /// A random old message from a channel everyone can read (not NSFW, not excluded), by a
    /// member still here, plus three decoys: other authors from the same stretch of history when
    /// there are enough, members at random otherwise. Null after a few empty digs.
    /// </summary>
    private async Task<(IMessage Message, IReadOnlyList<(ulong Id, string Name)> Choices)?> FindQuoteAsync(SocketGuild guild)
    {
        var xp = _services.GetRequiredService<XpTracker>();
        var everyone = guild.EveryoneRole;
        var me = guild.CurrentUser;
        var channels = new List<SocketTextChannel>();
        foreach (var c in guild.TextChannels)
        {
            if (c is SocketVoiceChannel or SocketThreadChannel || c.IsNsfw) continue;
            if (c.GetPermissionOverwrite(everyone)?.ViewChannel == PermValue.Deny) continue;
            if (c.Category is SocketCategoryChannel cat && cat.GetPermissionOverwrite(everyone)?.ViewChannel == PermValue.Deny) continue;
            var mine = me.GetPermissions(c);
            if (!mine.ViewChannel || !mine.ReadMessageHistory) continue;
            if (await xp.IsChannelExcludedAsync(guild.Id, c)) continue;
            channels.Add(c);
        }
        if (!everyone.Permissions.ViewChannel || channels.Count == 0) return null;

        var latest = DateTimeOffset.UtcNow - QuoteRules.MinAge;
        for (var attempt = 0; attempt < QuoteRules.Attempts; attempt++)
        {
            var channel = channels[Random.Shared.Next(channels.Count)];
            if (channel.CreatedAt >= latest) continue;
            var span = latest - channel.CreatedAt;
            var at = channel.CreatedAt + TimeSpan.FromTicks((long)(span.Ticks * Random.Shared.NextDouble()));
            IEnumerable<IMessage> batch;
            try
            {
                batch = await channel.GetMessagesAsync(SnowflakeUtils.ToSnowflake(at), Direction.After, 100).FlattenAsync();
            }
            catch
            {
                continue;
            }

            var usable = batch.Where(m => m is IUserMessage { Type: MessageType.Default or MessageType.Reply }
                                          && !m.Author.IsBot && !m.Author.IsWebhook
                                          && m.Timestamp < latest
                                          && QuoteRules.Usable(m.Content.Trim())
                                          && guild.GetUser(m.Author.Id) is not null).ToList();
            if (usable.Count == 0) continue;

            var pick = usable[Random.Shared.Next(usable.Count)];
            var decoys = batch.Select(m => m.Author.Id).Distinct()
                .Where(id => id != pick.Author.Id && guild.GetUser(id) is { IsBot: false })
                .OrderBy(_ => Random.Shared.Next())
                .Take(QuoteRules.Choices - 1)
                .ToList();
            if (decoys.Count < QuoteRules.Choices - 1)
                decoys.AddRange(guild.Users.Where(u => !u.IsBot && u.Id != pick.Author.Id && !decoys.Contains(u.Id))
                    .OrderBy(_ => Random.Shared.Next())
                    .Take(QuoteRules.Choices - 1 - decoys.Count)
                    .Select(u => u.Id));
            if (decoys.Count == 0) continue;

            var choices = decoys.Append(pick.Author.Id)
                .OrderBy(_ => Random.Shared.Next())
                .Select(id => (id, BotResponses.DisplayNameFor(guild.GetUser(id))))
                .ToList();
            return (pick, choices);
        }
        return null;
    }

    // ---- Temperature ------------------------------------------------------------------------------

    [SlashCommand("temperature", "Devine la température de mon processeur (mise : 10)")]
    public async Task TemperatureAsync()
    {
        if (PiHardware.TryReadCpuMilliCelsius() is null)
        {
            await RespondAsync(Pick(BotResponses.TempNoSensorLines), ephemeral: true);
            return;
        }

        var round = new TemperatureRound
        {
            GuildId = Context.Guild.Id, ChannelId = Context.Channel.Id, OrganizerId = Context.User.Id,
            EndsAt = DateTimeOffset.UtcNow + TemperatureRules.Window,
        };
        var (opened, blocking) = _tables.TryOpen(round, g => g is TemperatureRound && g.GuildId == round.GuildId);
        if (await RefuseRunningAsync(blocking, "Un pari sur ma température")) return;

        try
        {
            round.Line = Pick(BotResponses.TempIntroLines, $"<@{Context.User.Id}>");
            await RespondAsync(embed: PartyCards.TemperatureCard(round), components: PartyCards.TemperatureButtons(round),
                allowedMentions: AllowedMentions.None);
            round.MessageId = (await GetOriginalResponseAsync()).Id;
            Party.ScheduleTemperature(opened!);
        }
        catch
        {
            _tables.End(round.Id);
            throw;
        }
    }

    // ---- Boards -----------------------------------------------------------------------------------

    [SlashCommand("puissance4", "Une partie de Puissance 4, contre quelqu'un ou contre moi")]
    public Task Connect4Async(
        [Summary("opponent", "Ton adversaire (personne : c'est moi)")] IUser? opponent = null,
        [Summary("stake", "Une mise, si tu veux (0 par défaut)")] [MinValue(0)] [MaxValue(Duel.MaxStake)] int stake = 0) =>
        StartBoardAsync(BoardKind.Connect4, opponent, stake);

    [SlashCommand("morpion", "Une partie de morpion, contre quelqu'un ou contre moi")]
    public Task TicTacToeAsync(
        [Summary("opponent", "Ton adversaire (personne : c'est moi)")] IUser? opponent = null,
        [Summary("stake", "Une mise, si tu veux (0 par défaut)")] [MinValue(0)] [MaxValue(Duel.MaxStake)] int stake = 0) =>
        StartBoardAsync(BoardKind.TicTacToe, opponent, stake);

    private async Task StartBoardAsync(BoardKind kind, IUser? opponent, int stake)
    {
        var meId = Context.Client.CurrentUser.Id;
        var againstHer = opponent is null || opponent.Id == meId;
        if (opponent is not null && opponent.Id == Context.User.Id)
        {
            await RespondAsync("Jouer contre toi-même ? Tu gagnerais. Et tu perdrais. Non.", ephemeral: true);
            return;
        }
        if (opponent is { IsBot: true } && !againstHer)
        {
            await RespondAsync("Les autres bots ne savent pas jouer. Ni à ça, ni à rien.", ephemeral: true);
            return;
        }
        if (stake > 0)
        {
            var balance = await _arcade.BalanceAsync(Context.Guild.Id, Context.User.Id);
            if (balance < stake)
            {
                await RespondAsync($"Tu n'as que **{PebbleEconomy.Cailloux(balance)}**.", ephemeral: true);
                return;
            }
            if (!againstHer && await _arcade.BalanceAsync(Context.Guild.Id, opponent!.Id) < stake)
            {
                await RespondAsync($"<@{opponent.Id}> n'a pas de quoi suivre une mise de **{PebbleEconomy.Cailloux(stake)}**.",
                    ephemeral: true, allowedMentions: AllowedMentions.None);
                return;
            }
        }

        var game = new BoardGame
        {
            GuildId = Context.Guild.Id, ChannelId = Context.Channel.Id, Kind = kind,
            Player1 = Context.User.Id, Player2 = againstHer ? meId : opponent!.Id,
            AgainstHer = againstHer, Stake = stake, Accepted = againstHer,
            Board = kind == BoardKind.Connect4 ? Connect4.NewBoard() : TicTacToe.NewBoard(),
            CheatPending = againstHer && kind == BoardKind.Connect4 && Random.Shared.Next(5) == 0,
        };
        // One board at a time per person — her excepted: anyone may play her while others do.
        var (opened, blocking) = _tables.TryOpen(game, g => g is BoardGame b
            && (b.IsPlayer(Context.User.Id) || (!againstHer && b.IsPlayer(game.Player2))));
        if (await RefuseRunningAsync(blocking, "Une partie avec toi ou ton adversaire")) return;

        try
        {
            var name = PartyCards.BoardName(kind);
            if (againstHer)
            {
                game.Line = Pick(BotResponses.BoardHerStartLines, $"<@{Context.User.Id}>", name);
                await RespondAsync(embed: PartyCards.BoardCard(game), components: PartyCards.BoardButtons(game),
                    allowedMentions: AllowedMentions.None);
            }
            else
            {
                game.Line = Pick(BotResponses.BoardChallengeLines, $"<@{Context.User.Id}>", $"<@{opponent!.Id}>", name);
                await RespondAsync($"<@{opponent.Id}>", embed: PartyCards.BoardCard(game), components: PartyCards.BoardChallengeButtons(game),
                    allowedMentions: new AllowedMentions { UserIds = new List<ulong> { opponent.Id } });
            }
            game.MessageId = (await GetOriginalResponseAsync()).Id;
            Party.WatchBoard(opened!.Id);
        }
        catch
        {
            _tables.End(game.Id);
            throw;
        }
    }
}
