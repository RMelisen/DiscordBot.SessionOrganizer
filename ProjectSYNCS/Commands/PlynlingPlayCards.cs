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
}
