using System.Globalization;
using Discord;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Commands;

/// <summary>
/// The one message a visit lives in (docs/agents/plynling.md, Visits): the visitor's options, then the
/// knock with the host's reactions, then the ending — each an edit of the same message. Static and
/// Context-free, like every card, so the checks measure them. Components V2: the caller re-asserts the
/// flag and passes AllowedMentions (the first send pings the invited owner only; every edit, none).
/// Names go through <see cref="PlynlingCardUi.SafeName"/>; buttons carry them raw (a label renders no
/// markdown and no mention). Nothing on a button says the odds.
/// </summary>
public static class PlynlingVisitCards
{
    // Each step its own verb, so no two rows ever share an id; every id carries what its handler needs
    // and nothing secret: the visitor's Plynling, the invited owner, the expiry, the scene, the choices.
    public static string ChoiceId(int visitorId, ulong hostOwnerId, long expiresUnix, string scene, string option) =>
        $"pvc:{visitorId}:{hostOwnerId}:{expiresUnix}:{scene}:{option}";

    public static string ConfessId(int visitorId, ulong hostOwnerId, long expiresUnix, string scene) =>
        $"pvx:{visitorId}:{hostOwnerId}:{expiresUnix}:{scene}";

    public static string ReactionId(int visitorId, ulong hostOwnerId, long expiresUnix, string scene, string option, string reaction) =>
        $"pvr:{visitorId}:{hostOwnerId}:{expiresUnix}:{scene}:{option}:{reaction}";

    public const string ConfessLabel = "💌 Lui avouer ses sentiments";

    // Blurple, as every "pick one" is (CLAUDE.md, Button colours): grey options read as decoration and
    // the invited owner missed the knock. Both steps share it. « 💌 » is green, the theme's one
    // exception: the special move must stand apart from the scene's options.
    private const ButtonStyle ChoiceStyle = ButtonStyle.Primary;
    private const ButtonStyle ConfessStyle = ButtonStyle.Success;

    private static readonly CultureInfo Fr = CultureInfo.GetCultureInfo("fr-FR");

    // A scene's text with both names, bold (TextDisplay).
    private static string Say(string template, Plynling visitor, Plynling host) =>
        PlynlingVisitStory.Expand(PlynlingVisitStory.Untag(template).Text, PlynlingCardUi.SafeName(visitor.Name), visitor.Gender,
            PlynlingCardUi.SafeName(host.Name), host.Gender);

    // A button's label: the names raw and plain. Filled after the template, through markers, so a name
    // is never read as a template.
    private static string Label(string template, Plynling visitor, Plynling host) =>
        PlynlingVisitStory.Expand(template, "\u0001", visitor.Gender, "\u0002", host.Gender)
            .Replace("**\u0001**", visitor.Name).Replace("**\u0002**", host.Name);

    private static string Expiry(ulong ownerId, DateTimeOffset expires) =>
        $"-# Réservé à <@{ownerId}> · expire <t:{expires.ToUnixTimeSeconds()}:R>.";

    private static ContainerBuilder Frame(Plynling accentOf, VisitScene scene, Plynling visitor, Plynling host) =>
        new ContainerBuilder()
            .WithAccentColor(new Color(PlynlingCatalog.Info(accentOf.Species).Accent))
            .AddComponent(new TextDisplayBuilder($"## {Say(scene.Place, visitor, host)}"));

    private static SectionBuilder WithVisitor(Plynling visitor, DateTimeOffset now, string text) =>
        new SectionBuilder()
            .WithAccessory(new ThumbnailBuilder().WithMedia(new UnfurledMediaItemProperties(PlynlingArt.SpriteOf(visitor, now)))
                .WithDescription(PlynlingCatalog.Info(visitor.Species).Name))
            .AddComponent(new TextDisplayBuilder(text));

    /// <summary>
    /// Step 1, public: the place, the visitor, the intro, and the visitor's options for its owner —
    /// plus « 💌 » when the pair may confess. The mention of the invited owner is what the first send pings.
    /// </summary>
    public static MessageComponent BuildChoice(VisitScene scene, Plynling visitor, Plynling host, ulong hostOwnerId,
        DateTimeOffset expires, bool offerConfession, DateTimeOffset now)
    {
        var a = PlynlingCardUi.SafeName(visitor.Name);
        var at = expires.ToUnixTimeSeconds();
        var row = new ActionRowBuilder();
        foreach (var o in scene.Options)
            row.WithButton(Label(o.Label, visitor, host), ChoiceId(visitor.Id, hostOwnerId, at, scene.Key, o.Key), ChoiceStyle);
        if (offerConfession) row.WithButton(ConfessLabel, ConfessId(visitor.Id, hostOwnerId, at, scene.Key), ConfessStyle);
        return new ComponentBuilderV2()
            .AddComponent(Frame(visitor, scene, visitor, host)
                .AddComponent(WithVisitor(visitor, now, $"**{a}** part rendre visite à <@{hostOwnerId}>.\n\n{Say(scene.Intro, visitor, host)}"))
                .AddComponent(new TextDisplayBuilder($"Que fait **{a}** ?\n{Expiry(visitor.OwnerId, expires)}")))
            .AddComponent(row)
            .Build();
    }

    /// <summary>Step 2, the knock: what the visitor chose, and the host's reactions for the invited owner.</summary>
    public static MessageComponent BuildKnock(VisitScene scene, VisitChoice option, Plynling visitor, Plynling host, ulong hostOwnerId,
        DateTimeOffset expires, DateTimeOffset now)
    {
        var a = PlynlingCardUi.SafeName(visitor.Name);
        var b = PlynlingCardUi.SafeName(host.Name);
        var chose = Say(option.Label, visitor, host);
        if (chose.Length > 0 && char.IsLetter(chose[0])) chose = char.ToLower(chose[0], Fr) + chose[1..];
        var at = expires.ToUnixTimeSeconds();
        var row = new ActionRowBuilder();
        foreach (var r in scene.Reactions)
            row.WithButton(Label(r.Label, visitor, host), ReactionId(visitor.Id, hostOwnerId, at, scene.Key, option.Key, r.Key), ChoiceStyle);
        return new ComponentBuilderV2()
            .AddComponent(Frame(visitor, scene, visitor, host)
                .AddComponent(WithVisitor(visitor, now, $"{Say(scene.Intro, visitor, host)}\n\n**{a}** a choisi : {chose}."))
                .AddComponent(new TextDisplayBuilder($"<@{hostOwnerId}>, que fait **{b}** ?\n{Expiry(hostOwnerId, expires)}")))
            .AddComponent(row)
            .Build();
    }

    /// <summary>
    /// Step 3, the ending: both Plynlings (faces from the result), the visitor's half, the host's half,
    /// then what the visit changed. In the confession, the halves are the answer's (yes, no).
    /// </summary>
    public static MessageComponent BuildEnding(VisitScene scene, VisitChoice option, VisitChoice reaction, VisitOutcome outcome, DateTimeOffset now,
        string? lead = null)
    {
        var visitor = outcome.Visitor;
        var host = outcome.Host;
        var good = scene.Confession ? outcome.Confession == Confession.Accepted : outcome.GoodScene;
        var (visitorHalf, visitorTag) = PlynlingVisitStory.Untag(good ? option.Halves.Good : option.Halves.Bad);
        var (hostHalf, hostTag) = PlynlingVisitStory.Untag(good ? reaction.Halves.Good : reaction.Halves.Bad);
        var (visitorFace, hostFace) = Faces(outcome, good, visitorTag, hostTag);
        var text = $"{Say(visitorHalf, visitor, host)}\n{Say(hostHalf, visitor, host)}\n{PlynlingPlayCards.VisitOutcomeLines(outcome)}";
        if (lead is not null) text = $"{lead}\n\n{text}";   // her welcome, at her own home
        return new ComponentBuilderV2()
            .AddComponent(Frame(host, scene, visitor, host)
                .AddComponent(new MediaGalleryBuilder()
                    .AddItem(PlynlingArt.VisitSpriteOf(visitor, now, visitorFace), PlynlingCardUi.SafeName(visitor.Name), false)
                    .AddItem(PlynlingArt.VisitSpriteOf(host, now, hostFace), PlynlingCardUi.SafeName(host.Name), false))
                .AddComponent(new SeparatorBuilder())
                .AddComponent(new TextDisplayBuilder(text)))
            .Build();
    }

    // Good: the narration face of the bond it ends on. Bad: angry, or sad between friends or closer. A
    // visit that broke something (or made a couple) takes PlynlingVisitStory's parting face. A face tag
    // opening a half wins for its Plynling: [face] is the half's own Plynling, [A:… B:…] names each.
    internal static (PlynlingMood Visitor, PlynlingMood Host) Faces(VisitOutcome o, bool good, FaceTag? visitorTag, FaceTag? hostTag)
    {
        var face = good ? PlynlingVisitStory.Faces(PlynlingVisitStory.MoodFor(true, o.After)).Narration
            : o.Before is PlynlingBond.Friends or PlynlingBond.BestFriends or PlynlingBond.Lovers ? PlynlingMood.Sad : PlynlingMood.Angry;
        if (PlynlingVisitStory.OutcomeFace(o) is { } parting) face = parting;
        var (v, h) = (face, face);
        if (visitorTag is not null) { v = visitorTag.Both ?? visitorTag.A ?? v; h = visitorTag.B ?? h; }
        if (hostTag is not null) { h = hostTag.Both ?? hostTag.B ?? h; v = hostTag.A ?? v; }
        return (v, h);
    }

    /// <summary>A visit message closed without a visit: expired, too late, an old knock.</summary>
    public static MessageComponent BuildClosed(string text) =>
        new ComponentBuilderV2().AddComponent(new ContainerBuilder().AddComponent(new TextDisplayBuilder(text))).Build();
}
