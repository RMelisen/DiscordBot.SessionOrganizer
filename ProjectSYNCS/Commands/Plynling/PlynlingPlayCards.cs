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
    /// (plyn:hide / rps / bag / memogo / memo / nim / dig), and within a row the ids differ by their last part.
    /// </summary>
    public static MessageComponent BuildGame(PlynlingGameSession session, Plynling plynling, DateTimeOffset now, string? endLine)
    {
        var info = PlynlingCatalog.Info(plynling.Species);
        var state = session.State;
        var sprite = PlynlingArt.SpriteOf(plynling, now);
        var text = PlynlingGameUi.Text(state, PlynlingCardUi.SafeName(plynling.Name), plynling.Gender);
        if (!string.IsNullOrWhiteSpace(endLine)) text += "\n" + endLine;

        var builder = new ComponentBuilderV2().AddComponent(new ContainerBuilder()
            .WithAccentColor(new Color(info.Accent))
            .AddComponent(new SectionBuilder()
                .WithAccessory(new ThumbnailBuilder().WithMedia(new UnfurledMediaItemProperties(sprite)).WithDescription(info.Name))
                .AddComponent(new TextDisplayBuilder(text))));

        // The treasure hunt keeps its grid once over, every cell disabled and the gland shown: the
        // reveal reads better on the field than in a sentence.
        if (state.Game == PlynlingGame.TreasureHunt)
        {
            foreach (var row in TreasureRows(session.Id, state)) builder.AddComponent(row);
            return builder.Build();
        }
        if (state.Status != GameStatus.Playing) return builder.Build();

        var id = session.Id;
        switch (state.Game)
        {
            case PlynlingGame.HideAndSeek:
                var rocks = new ActionRowBuilder();
                for (var rock = 0; rock < PlynlingGameState.Rocks; rock++)
                    rocks.WithButton($"{rock + 1}", $"plyn:hide:{id}:{rock}", ButtonStyle.Primary);
                builder.AddComponent(rocks);
                break;
            case PlynlingGame.RockPaperScissors:
                builder.AddComponent(new ActionRowBuilder()
                    .WithButton("✊ Pierre", $"plyn:rps:{id}:{(int)RpsThrow.Rock}", ButtonStyle.Primary)
                    .WithButton("✋ Papier", $"plyn:rps:{id}:{(int)RpsThrow.Paper}", ButtonStyle.Primary)
                    .WithButton("✌️ Ciseaux", $"plyn:rps:{id}:{(int)RpsThrow.Scissors}", ButtonStyle.Primary));
                break;
            case PlynlingGame.PebbleBag:
                builder.AddComponent(new ActionRowBuilder()
                    .WithButton("Plus haut", $"plyn:bag:{id}:up:{state.Step}", ButtonStyle.Primary, new Emoji("⬆️"))
                    .WithButton("Plus bas", $"plyn:bag:{id}:down:{state.Step}", ButtonStyle.Primary, new Emoji("⬇️")));
                break;
            case PlynlingGame.Memory when !state.Memorised:
                builder.AddComponent(new ActionRowBuilder().WithButton("J'ai retenu !", $"plyn:memogo:{id}", ButtonStyle.Primary, new Emoji("👀")));
                break;
            case PlynlingGame.Memory:
                // Six options in two rows of three; a right pick stays, disabled, so the row doesn't jump.
                var picked = state.Sequence.Take(state.Progress).ToHashSet();
                for (var start = 0; start < state.Options.Count; start += 3)
                {
                    var row = new ActionRowBuilder();
                    for (var slot = start; slot < Math.Min(start + 3, state.Options.Count); slot++)
                    {
                        var key = state.Options[slot];
                        var done = picked.Contains(key);
                        row.WithButton(PlynlingGameUi.MushroomLabel(key), $"plyn:memo:{id}:{slot}",
                            done ? ButtonStyle.Secondary : ButtonStyle.Primary, EmoteOf(PlynlingGameUi.MushroomEmoji(key)), disabled: done);
                    }
                    builder.AddComponent(row);
                }
                break;
            case PlynlingGame.LastPebble:
                var takes = new ActionRowBuilder();
                for (var n = 1; n <= PlynlingGameState.NimMaxTake; n++)
                    takes.WithButton($"{n}", $"plyn:nim:{id}:{n}:{state.Step}", ButtonStyle.Primary, disabled: n > state.Pile);
                builder.AddComponent(takes);
                break;
        }
        return builder.Build();
    }

    // The 4×4 field: undug cells blurple « · » (a pick), dug ones grey and disabled with their hint, and once the game is
    // over every cell disabled with the gland shown where it was.
    private static IEnumerable<ActionRowBuilder> TreasureRows(string id, PlynlingGameState state)
    {
        var over = state.Status != GameStatus.Playing;
        for (var r = 0; r < PlynlingGameState.GridSize; r++)
        {
            var row = new ActionRowBuilder();
            for (var c = 0; c < PlynlingGameState.GridSize; c++)
            {
                var cell = r * PlynlingGameState.GridSize + c;
                var customId = $"plyn:dig:{id}:{cell}";
                if (state.Dug.TryGetValue(cell, out var hint))
                    row.WithButton(null, customId, ButtonStyle.Secondary, new Emoji(PlynlingGameUi.HintEmoji(hint)), disabled: true);
                else if (over && cell == state.Treasure)
                    row.WithButton(null, customId, ButtonStyle.Secondary, new Emoji(PlynlingGameUi.HintEmoji(DigHint.Found)), disabled: true);
                else
                    row.WithButton("·", customId, over ? ButtonStyle.Secondary : ButtonStyle.Primary, disabled: over);
            }
            yield return row;
        }
    }

    // A custom emoji's markup (<:name:id>) or a Unicode emoji, as a button wants it.
    private static IEmote EmoteOf(string markup) => Emote.TryParse(markup, out var emote) ? emote : new Emoji(markup);

    /// <summary>
    /// What a visit changed, closing its story: the confession, the bond, badges, finds, and the
    /// happiness line — everything the old one-line visit card said after its line.
    /// </summary>
    public static string VisitOutcomeLines(VisitOutcome pair)
    {
        var a = PlynlingCardUi.SafeName(pair.Visitor.Name);
        var b = PlynlingCardUi.SafeName(pair.Host.Name);
        var lines = new List<string>();
        if (pair.Confession == Confession.Accepted) lines.Add(PlynlingText.ConfessionAccepted(a, b));
        else if (pair.Confession == Confession.Refused) lines.Add(PlynlingText.ConfessionRefused(a, b));
        if (pair.After != pair.Before && pair.Confession != Confession.Accepted
            && !(pair.Before == PlynlingBond.BestFriends && pair.After == PlynlingBond.Friends))   // a quiet drift
        {
            lines.Add(pair.Before == PlynlingBond.Lovers
                ? PlynlingText.BrokeUp(a, b)
                : PlynlingBonds.ChangeLine(pair.After, a, pair.Visitor.Gender, b, pair.Host.Gender));
        }
        foreach (var (who, badges) in new[] { (pair.Visitor, pair.VisitorBadges), (pair.Host, pair.HostBadges) })
            if (badges.Count > 0)
                lines.Add("**" + PlynlingCardUi.SafeName(who.Name) + "** · " + PlynlingBadges.NewBadgeLines(badges, who.Gender));
        foreach (var (who, find) in new[] { (pair.Visitor, pair.VisitorFind), (pair.Host, pair.HostFind) })
            if (find is not null)
                lines.Add(PlynlingText.FindLines(PlynlingText.VisitFind(PlynlingCardUi.SafeName(who.Name), who.OwnerId, find.Item), find, who.OwnerId));
        var happiness = (int)Math.Round(Math.Abs(pair.Happiness) * 100);
        var sign = pair.Happiness < 0 ? "−" : "+";
        lines.Add($"-# {sign}{happiness} % de bonheur pour **{a}** et **{b}**");
        return string.Join("\n", lines);
    }

    // The pager's vis:prev|next|first|last:{story}:{beat} — the verbs PlynlingComponentHandler binds.
    private static ActionRowBuilder BuildVisitArrows(VisitStory story, int beat) =>
        StoryPager.Row(beat, story.Beats.Count, verb => $"vis:{verb}:{story.Id}:{beat}");

    /// <summary>
    /// One step of a visit's story: the place, the two Plynlings side by side (a gallery of two, each
    /// in this step's face; the alt text is its name), then the step's text, « 3/7 », and ◀ ▶ — from
    /// the very first step, since the reader pages through at their own pace.
    /// </summary>
    public static MessageComponent BuildVisitStory(VisitStory story, int beat)
    {
        beat = Math.Clamp(beat, 0, story.Beats.Count - 1);
        var step = story.Beats[beat];
        return new ComponentBuilderV2()
            .AddComponent(new ContainerBuilder()
                .WithAccentColor(new Color(story.Accent))
                .AddComponent(new TextDisplayBuilder($"## {story.Heading}"))
                .AddComponent(new MediaGalleryBuilder()
                    .AddItem(story.Visitor.Sprite(step.VisitorFace), story.Visitor.Name, false)
                    .AddItem(story.Host.Sprite(step.HostFace), story.Host.Name, false))
                .AddComponent(new SeparatorBuilder())
                .AddComponent(new TextDisplayBuilder($"{step.Text}\n-# {beat + 1}/{story.Beats.Count}")))
            .AddComponent(BuildVisitArrows(story, beat))
            .Build();
    }
}
