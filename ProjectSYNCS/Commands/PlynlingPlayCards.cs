using Discord;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Commands;

// The messages /plynling play and /plynling visit draw — static and Context-free like the
// card, so their components are checkable without a gateway. Components V2: every send and
// update re-asserts the flag, and passes AllowedMentions.None (a TextDisplay really pings).
public static class PlynlingPlayCards
{
    /// <summary>
    /// A game at its current step: the Plynling's sprite, the game's text, and its buttons
    /// while it is being played. <paramref name="endLine"/> is her reaction and the reward,
    /// once it is over. Every button carries the game's id; the verbs differ per game
    /// (plyn:hide / plyn:rps / plyn:guess), and within a row the ids differ by their last part.
    /// </summary>
    public static MessageComponent BuildGame(PlynlingGameSession session, Plynling plynling, DateTimeOffset now, string? endLine)
    {
        var info = PlynlingCatalog.Info(plynling.Species);
        var state = session.State;
        var sprite = PlynlingArt.Sprite(plynling.Species, PlynlingLife.Stage(plynling, now), PlynlingLife.Mood(plynling, now));
        var text = PlynlingGameUi.Text(state, PlynlingCardUi.SafeName(plynling.Name), plynling.Gender);
        if (!string.IsNullOrWhiteSpace(endLine)) text += "\n" + endLine;

        var builder = new ComponentBuilderV2().AddComponent(new ContainerBuilder()
            .WithAccentColor(new Color(info.Accent))
            .AddComponent(new SectionBuilder()
                .WithAccessory(new ThumbnailBuilder().WithMedia(new UnfurledMediaItemProperties(sprite)).WithDescription(info.Name))
                .AddComponent(new TextDisplayBuilder(text))));

        if (state.Status == GameStatus.Playing)
        {
            var row = new ActionRowBuilder();
            switch (state.Game)
            {
                case PlynlingGame.HideAndSeek:
                    for (var rock = 0; rock < PlynlingGameState.Rocks; rock++)
                        row.WithButton($"🪨 {rock + 1}", $"plyn:hide:{session.Id}:{rock}", ButtonStyle.Secondary);
                    break;
                case PlynlingGame.RockPaperScissors:
                    row.WithButton("✊ Pierre", $"plyn:rps:{session.Id}:{(int)RpsThrow.Rock}", ButtonStyle.Secondary)
                       .WithButton("✋ Feuille", $"plyn:rps:{session.Id}:{(int)RpsThrow.Paper}", ButtonStyle.Secondary)
                       .WithButton("✌️ Ciseaux", $"plyn:rps:{session.Id}:{(int)RpsThrow.Scissors}", ButtonStyle.Secondary);
                    break;
                default:
                    row.WithButton("🔢 Deviner", $"plyn:guess:{session.Id}", ButtonStyle.Primary);
                    break;
            }
            builder.AddComponent(row);
        }
        return builder.Build();
    }

    // The « Accueillir » button's id carries the visitor's Plynling, the invited owner and the
    // expiry — everything the accept needs, and nothing secret.
    public static string VisitId(int visitorId, ulong hostOwnerId, DateTimeOffset expires) =>
        $"plyn:visit:{visitorId}:{hostOwnerId}:{expires.ToUnixTimeSeconds()}";

    /// <summary>The knock: the visitor at the door, and « Accueillir » for the invited owner.</summary>
    public static MessageComponent BuildKnock(Plynling visitor, ulong hostOwnerId, DateTimeOffset expires, string line, DateTimeOffset now)
    {
        var info = PlynlingCatalog.Info(visitor.Species);
        var sprite = PlynlingArt.Sprite(visitor.Species, PlynlingLife.Stage(visitor, now), PlynlingLife.Mood(visitor, now));
        return new ComponentBuilderV2()
            .AddComponent(new ContainerBuilder()
                .WithAccentColor(new Color(info.Accent))
                .AddComponent(new SectionBuilder()
                    .WithAccessory(new ThumbnailBuilder().WithMedia(new UnfurledMediaItemProperties(sprite)).WithDescription(info.Name))
                    .AddComponent(new TextDisplayBuilder(
                        $"{line}\n-# Réservé à <@{hostOwnerId}> · l'invitation expire <t:{expires.ToUnixTimeSeconds()}:R>."))))
            .AddComponent(new ActionRowBuilder()
                .WithButton("🏠 Accueillir", VisitId(visitor.Id, hostOwnerId, expires), ButtonStyle.Success))
            .Build();
    }

    // A knock nobody will answer any more (expired), without its button.
    public static MessageComponent BuildKnockClosed(string text) =>
        new ComponentBuilderV2().AddComponent(new ContainerBuilder().AddComponent(new TextDisplayBuilder(text))).Build();

    /// <summary>The visit: both Plynlings side by side, her line, and what it gave them.</summary>
    public static MessageComponent BuildMeeting(Plynling visitor, Plynling host, string line, DateTimeOffset now, double happinessShare)
    {
        string Sprite(Plynling p) => PlynlingArt.Sprite(p.Species, PlynlingLife.Stage(p, now), PlynlingLife.Mood(p, now));
        var happiness = (int)Math.Round(Math.Abs(happinessShare) * 100);
        var sign = happinessShare < 0 ? "−" : "+";
        return new ComponentBuilderV2()
            .AddComponent(new ContainerBuilder()
                .WithAccentColor(new Color(PlynlingCatalog.Info(host.Species).Accent))
                .AddComponent(new TextDisplayBuilder($"## 🏡 Visite\n{line}"))
                .AddComponent(new MediaGalleryBuilder()
                    .AddItem(Sprite(visitor), PlynlingCardUi.SafeName(visitor.Name), false)
                    .AddItem(Sprite(host), PlynlingCardUi.SafeName(host.Name), false))
                .AddComponent(new TextDisplayBuilder(
                    $"-# {sign}{happiness} % de bonheur pour **{PlynlingCardUi.SafeName(visitor.Name)}** et **{PlynlingCardUi.SafeName(host.Name)}**")))
            .Build();
    }
}
