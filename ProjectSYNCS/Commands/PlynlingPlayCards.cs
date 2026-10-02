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
        var sprite = PlynlingArt.SpriteOf(plynling, now);
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
                        row.WithButton($"{rock + 1}",$"plyn:hide:{session.Id}:{rock}", ButtonStyle.Secondary);
                    break;
                case PlynlingGame.RockPaperScissors:
                    row.WithButton("✊ Pierre", $"plyn:rps:{session.Id}:{(int)RpsThrow.Rock}", ButtonStyle.Secondary)
                       .WithButton("✋ Papier", $"plyn:rps:{session.Id}:{(int)RpsThrow.Paper}", ButtonStyle.Secondary)
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
        var sprite = PlynlingArt.SpriteOf(visitor, now);
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

    // The two arrows' verbs differ, so a card never carries the same id twice (disabled or not).
    public static string VisitPrevId(string story, int beat) => $"vis:prev:{story}:{beat}";
    public static string VisitNextId(string story, int beat) => $"vis:next:{story}:{beat}";
    public static string VisitFirstId(string story, int beat) => $"vis:first:{story}:{beat}";
    public static string VisitLastId(string story, int beat) => $"vis:last:{story}:{beat}";

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

    // ◀, ▶ and « ⏭ Fin », which skips to the last step (the outcome). On the last step ▶ has nowhere
    // to go, so it becomes « ↺ Début », back to step 1, and « Fin » goes. Each has its own verb, so a
    // card can never carry the same id twice.
    private static ActionRowBuilder BuildVisitArrows(VisitStory story, int beat)
    {
        var row = new ActionRowBuilder()
            .WithButton("◀", VisitPrevId(story.Id, beat), ButtonStyle.Secondary, disabled: beat == 0);
        return beat == story.Beats.Count - 1
            ? row.WithButton("↺ Début", VisitFirstId(story.Id, beat), ButtonStyle.Secondary)
            : row.WithButton("▶", VisitNextId(story.Id, beat), ButtonStyle.Secondary)
                .WithButton("⏭ Fin", VisitLastId(story.Id, beat), ButtonStyle.Secondary);
    }

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
