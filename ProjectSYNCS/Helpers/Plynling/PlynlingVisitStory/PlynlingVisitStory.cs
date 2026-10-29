using System.Text.RegularExpressions;
using ProjectSYNCS.Models;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Helpers;

// Where a visit happens. Open from FromHour (inclusive) to ToHour (exclusive), Paris time; a
// place whose FromHour is after its ToHour is open across midnight. {B} in the name is the host.
public sealed record VisitPlace(string Emoji, string Name, int FromHour, int ToHour, string[] Scenes)
{
    public bool IsOpenAt(int hour) =>
        FromHour <= ToHour ? hour >= FromHour && hour < ToHour : hour >= FromHour || hour < ToHour;
}

// One Plynling as the story shows it — captured when the visit happens, so paging back through
// the story later needs no database and shows them as they were. The picture is built per step
// from the species, the stage and that step's face.
public sealed record VisitCast(string Name, PlynlingSpecies Species, PlynlingStage Stage, string SpeciesName,
    PlynlingGender Gender, IReadOnlyList<Passion> Passions)
{
    public string Sprite(PlynlingMood face) => PlynlingArt.VisitSprite(Species, Stage, face);
}

// One step of a story: its text and each Plynling's face on it.
public sealed record VisitBeat(string Text, PlynlingMood VisitorFace, PlynlingMood HostFace);

// A face tag read off the start of a line: [sad] (Both), or [A:sad B:happy] for the visitor and
// the host separately. See PlynlingVisitStory.Untag.
public sealed record FaceTag(PlynlingMood? Both, PlynlingMood? A, PlynlingMood? B);

// A visit told in three beats: arrival, the activity with a little exchange, parting with the
// outcome. Id is empty until VisitStories keeps it.
public sealed record VisitStory(string Id, string Heading, IReadOnlyList<VisitBeat> Beats, VisitCast Visitor, VisitCast Host, uint Accent);

// Which pools a visit draws from: the bond after it, or Conflict when the scene went badly (or
// the two are enemies — there is no friendly way to be that).
public enum VisitMood { Acquaintances, Friends, BestFriends, Lovers, Rivals, Conflict }

/// <summary>
/// Turns a visit's outcome into a little story. Pure: <see cref="PlynlingService.VisitAsync"/> has
/// already decided everything, so this only chooses the place and the words — a story that fails
/// to play can never change what happened.
/// <para>
/// Lines are templates: {A} and {B} are the visitor and the host (bold), {ils} / {Ils} the pair
/// (« elles » only when both are girls), and {a:m|f}, {b:m|f}, {p:m|f} a word agreeing with the
/// visitor, the host or the pair. An exchange is two lines, the visitor's then the host's,
/// separated by a newline.
/// </para>
/// <para>
/// Five beats: arrival, the subject (a speaker — visitor or host — raises one of their passions),
/// the listener's reaction, the activity with an exchange, then parting. Passion lines add {S} / {L},
/// the speaker and the listener, with {s:m|f} / {l:m|f}, and {P}, the subject passion — inserted
/// last, so text a person typed is never read as a template.
/// </para>
/// </summary>
public static partial class PlynlingVisitStory
{
    public static readonly IReadOnlyList<VisitPlace> Places = new VisitPlace[]
    {
        new("🌳", "Au parc", 0, 24, new[] { "Un banc garde encore la chaleur de quelqu'un qui vient de partir.", "Un écureuil traverse l'allée, s'arrête, reconsidère sa vie, et repart.", "Les feuilles bruissent comme si elles se racontaient quelque chose." }),
        new("🍂", "Sous le grand chêne", 0, 24, new[] { "Des glands tombent de temps en temps, avec un petit « toc » poli.", "Le vieux chêne fait de l'ombre à tout le monde, sans rien demander.", "Une racine forme un banc parfait. Quelqu'un l'a usée, avant eux." }),
        new("🏡", "Chez {B}", 0, 24, new[] { "Ça sent bon le gâteau aux noisettes.", "La maison est petite, mais chaque chose y a sa place, et le sait.", "Un coussin trop gonflé attend les invités avec impatience." }),
        new("☕", "Au café du coin", 0, 24, new[] { "La tortue qui sert a pris une commande il y a une heure. Elle arrive.", "Ça sent le chocolat chaud et le bois ciré.", "Une cuillère tinte contre une tasse, quelque part, puis plus rien." }),
        new("🪵", "Au bord de l'étang", 0, 24, new[] { "Une grenouille observe la scène depuis son nénuphar, sans prendre parti.", "L'eau fait des ronds qui s'agrandissent jusqu'à disparaître.", "Une libellule s'arrête en plein vol, comme pour écouter." }),
        new("🌼", "Dans la prairie", 6, 20, new[] { "Les fleurs sentent bon, et les abeilles bourdonnent, très affairées.", "L'herbe haute chatouille tout le monde, sans exception.", "Un grillon répète la même note, très sûr de lui." }),
        new("☀️", "Au soleil, sur les rochers", 10, 19, new[] { "Les pierres sont toutes chaudes. Parfait pour une sieste.", "Un lézard leur cède la place, de très mauvaise grâce.", "Le soleil tape juste assez pour qu'on ferme les yeux sans dormir." }),
        new("🌙", "Sur le toit, sous les étoiles", 20, 6, new[] { "Le toit est encore tiède du soleil de la journée.", "La lune est ronde, et une chouette la commente à voix basse.", "Les tuiles craquent doucement sous chaque pas." }),
        new("🌉", "Sur le vieux pont de pierre", 0, 24, new[] { "L'eau murmure sous les arches, toujours la même histoire.", "Un héron immobile fait semblant de ne rien voir.", "Une pierre du parapet bouge un peu. Tout le monde le sait. Personne ne la répare." }),
        new("🌲", "Dans la clairière", 0, 24, new[] { "Un rayon de lumière traverse les branches et se pose au milieu, comme un tapis.", "Des papillons zigzaguent entre les troncs, sans itinéraire.", "Le silence y est si épais qu'on a envie de chuchoter." }),
        new("🍃", "Sous une grande feuille, à l'abri de la pluie", 0, 24, new[] { "Les gouttes tambourinent sur la feuille, tout là-haut.", "Il fait sec, tiède, et un peu trop calme.", "De temps en temps, une goutte réussit à passer, et atterrit toujours au même endroit." }),
        new("🌿", "Dans le jardin de {B}", 6, 21, new[] { "Les plates-bandes sont parfaitement alignées. Presque toutes.", "Un petit ~~Ina~~ nain de jardin en cailloux monte la garde.", "Un arrosoir oublié s'est rempli de pluie, et un escargot y prend son bain." }),
        new("🌅", "Sur la colline, au lever du jour", 5, 9, new[] { "Le ciel passe doucement du rose à l'orange.", "La rosée brille sur l'herbe, et mouille tous les pieds.", "En bas, le village dort encore. Un seul toit fume." }),
        new("🥖", "À la boulangerie", 6, 19, new[] { "Ça sent le pain chaud jusque dans la rue.", "Une abeille goûte discrètement la confiture.", "Le boulanger, un blaireau enfariné, chante en pétrissant, faux et heureux." }),
        new("🌾", "Dans le champ de blé", 7, 20, new[] { "Les épis ondulent comme une mer dorée.", "Un épouvantail leur fait un clin d'œil. Enfin, il en a l'air.", "Un sentier minuscule traverse le champ, tracé par quelqu'un de très petit." }),
        new("🍎", "Au verger", 8, 20, new[] { "Ça sent la pomme mûre et l'herbe coupée.", "Un ver, très poli, s'excuse d'être là.", "Une pomme tombe, roule, et s'arrête pile devant eux. Comme une invitation." }),
        new("🛒", "Au marché du village", 8, 19, new[] { "Des étals de baies et de noisettes, à perte de vue.", "Un marchand de cailloux vante sa marchandise avec passion.", "Une pie examine les bijoux d'un air très professionnel." }),
        new("📚", "À la bibliothèque du village", 9, 19, new[] { "Ça sent le vieux papier et la poussière dorée.", "La bibliothécaire, un hibou, fait « chut » d'un seul œil.", "Un livre est resté ouvert sur une table, à la page la plus intéressante." }),
        new("🏖️", "Sur la plage de galets", 9, 21, new[] { "Les vagues font rouler les galets avec un joli bruit.", "Un crabe pince-sans-rire surveille la plage.", "Le vent sent le sel et les secrets." }),
        new("🛶", "Sur une barque, au milieu de l'étang", 10, 20, new[] { "La barque tangue un peu. Personne ne sait ramer.", "Un poisson curieux vient voir de plus près.", "Une rame flotte tranquillement un peu plus loin. Elle a pris son indépendance." }),
        new("🎡", "À la fête foraine", 14, 23, new[] { "Une petite musique de manège flotte dans l'air.", "Des lampions colorés se balancent au vent.", "Ça sent la barbe à papa et le sucre brûlé." }),
        new("🔥", "Autour du feu de camp", 18, 1, new[] { "Les flammes crépitent et font danser les ombres.", "Ça sent la noisette grillée.", "Une étincelle monte, monte, et devient presque une étoile." }),
        new("✨", "Dans la grotte aux lucioles", 20, 1, new[] { "Les lucioles dessinent des constellations sur les parois.", "L'écho répète tout, un peu de travers.", "Une goutte tombe quelque part, toujours au même rythme, comme une horloge." }),
        new("🛖", "Dans la cabane dans l'arbre", 0, 24, new[] { "L'échelle de corde grince à chaque pas.", "Le plancher est couvert de coussins moelleux.", "Par la fenêtre ronde, on voit tout le village, en tout petit." }),
        new("🕰️", "Dans le vieux grenier", 0, 24, new[] { "La poussière danse dans un rayon de lumière, sans se presser.", "Une malle entrouverte laisse dépasser la manche d'un vieux costume.", "Quelque part sous les poutres, un loir ronfle." }),
        new("🌈", "Sous l'arc-en-ciel", 8, 20, new[] { "Les couleurs se reflètent dans les flaques.", "Personne n'a trouvé le pot d'or, mais tout le monde a cherché.", "L'arc-en-ciel pâlit doucement, comme s'il savait qu'on le regarde." }),
        new("🏔️", "Sur la falaise, face au vent", 8, 19, new[] { "Le vent ébouriffe tout ce qui dépasse.", "En bas, le monde a l'air minuscule.", "Une mouette plane sans un battement d'ailes, très fière d'elle." }),
        new("⛲", "Près de la fontaine de la place", 7, 22, new[] { "L'eau clapote, et des pièces brillent au fond du bassin.", "Un pigeon se prend pour le maître des lieux.", "Chaque pièce au fond du bassin est un vœu. Certaines ont l'air très anciennes." }),
        new("🐝", "Devant la ruche", 8, 19, new[] { "Le bourdonnement est presque une berceuse.", "Une abeille très occupée leur fait signe de ne pas gêner.", "Ça sent le miel et la cire tiède." }),
        new("🎣", "Au bord de la rivière", 6, 20, new[] { "Le courant emporte une feuille comme un petit bateau.", "Un poisson passe en ricanant : il a compris qu'il n'y avait pas d'appât.", "Des cailloux plats attendent sur la rive qu'on les fasse ricocher." }),
        new("🍦", "Chez le glacier", 12, 22, new[] { "Des boules de glace colorées s'alignent derrière la vitre.", "Le glacier, un vieil ours, sert chaque cornet avec un soin infini.", "Une goutte de glace fond sur le comptoir, lentement, avec regret." }),
        new("🌌", "Dans un champ, à regarder les étoiles", 21, 1, new[] { "Une étoile filante passe, puis une autre.", "L'herbe est fraîche, et le ciel n'en finit pas.", "Quelque part, un grillon joue la même note que la plus petite étoile." }),
        new("🚂", "À la petite gare", 6, 22, new[] { "Le chef de gare, un hérisson, consulte sa montre, puis le ciel, puis encore sa montre.", "Un petit train siffle au loin, sans jamais avoir l'air d'approcher.", "Sur le quai, une valise attend quelqu'un depuis très longtemps." }),
        new("🎭", "Au petit théâtre du village", 15, 23, new[] { "Le rideau rouge est un peu mité, mais très fier.", "Dans les coulisses, quelqu'un répète la même réplique en boucle.", "Les sièges grincent chacun sur une note différente." }),
        new("🍵", "Au salon de thé", 13, 19, new[] { "Les tasses sont minuscules, et les gâteaux aussi.", "Une théière ronronne doucement sur la table.", "La nappe à carreaux a une tache en forme de lapin. Tout le monde la regarde." }),
        new("🎨", "Dans l'atelier du peintre", 9, 18, new[] { "Ça sent la peinture et le bois. Il y a des taches partout.", "Un tableau à moitié fini attend qu'on lui trouve un titre.", "Un pinceau sèche dans un pot, la tête en l'air, comme s'il réfléchissait." }),
        new("🛝", "Au terrain de jeux", 8, 20, new[] { "La balançoire grince gentiment.", "Le toboggan est bien trop grand pour tout le monde, et c'est parfait.", "Le bac à sable garde les traces d'un château très ambitieux." }),
        new("🥾", "Sur le sentier de randonnée", 7, 19, new[] { "Un petit panneau indique trois directions, toutes fausses.", "Les cailloux du chemin roulent sous les pas.", "Une fourmi porte une miette dans l'autre sens. Elle sait ce qu'elle fait." }),
        new("🌬️", "Dans les ruines du vieux moulin", 9, 20, new[] { "Le moulin ne tourne plus, mais le vent essaie encore.", "Les pierres racontent des histoires que personne n'écoute.", "Une fleur a poussé dans une fente du mur, là où personne ne l'attendait." }),
    };

    public static VisitMood MoodFor(bool goodScene, PlynlingBond after) => !goodScene ? VisitMood.Conflict : after switch
    {
        PlynlingBond.Friends => VisitMood.Friends,
        PlynlingBond.BestFriends => VisitMood.BestFriends,
        PlynlingBond.Lovers => VisitMood.Lovers,
        PlynlingBond.Rivals => VisitMood.Rivals,
        PlynlingBond.Enemies => VisitMood.Conflict,
        _ => VisitMood.Acquaintances,
    };

    /// <summary>A place open at <paramref name="now"/> (Paris time). Always at least the always-open ones.</summary>
    public static VisitPlace PlaceFor(DateTimeOffset now, Random rng)
    {
        var hour = AppTime.ToZoned(now).Hour;
        var open = Places.Where(p => p.IsOpenAt(hour)).ToList();
        return open[rng.Next(open.Count)];
    }

    /// <summary>
    /// The story of <paramref name="outcome"/>: arrival; a script in which A raises the subject and each line
    /// answers the one before — the opener on its own step, then the rest two lines to a step, the closer
    /// finishing the last pair; the activity; then parting with <paramref name="outcomeLines"/> — what the
    /// visit changed. Four steps plus half the script's lines: five for a two-line script, eight for an eight-line one. Each step carries both faces.
    /// <paramref name="pick"/> chooses a line from a pool (the handler passes ResponsePicker, so a
    /// channel does not see the same line twice in a row); by default it draws from <paramref name="rng"/>.
    /// </summary>
    public static VisitStory Build(VisitOutcome outcome, string outcomeLines, DateTimeOffset now, Random rng,
        Func<string[], string>? pick = null)
    {
        pick ??= pool => pool[rng.Next(pool.Length)];
        var visitor = Cast(outcome.Visitor, now);
        var host = Cast(outcome.Host, now);
        var place = PlaceFor(now, rng);
        var mood = MoodFor(outcome.GoodScene, outcome.After);
        var faces = Faces(mood);

        // A is the one whose passion is the subject; B the other.
        var aIsVisitor = rng.Next(2) == 0;
        var (a, b) = aIsVisitor ? (visitor, host) : (host, visitor);
        var subject = PickSubject(a.Passions, b.Passions, rng);
        var shared = b.Passions.Any(subject.SameAs);
        var info = subject.Catalog is { } c ? PlynlingPassions.Info(c) : null;

        // X: no speaker ({A}/{B} only). C: the conversation ({S} = A). By: {S} = the given one.
        string X(string t) => Expand(t, visitor.Name, visitor.Gender, host.Name, host.Gender);
        string C(string t) => Expand(t, visitor.Name, visitor.Gender, host.Name, host.Gender, aIsVisitor, subject.Render());
        string By(string t, bool visitorSays) => Expand(t, visitor.Name, visitor.Gender, host.Name, host.Gender, visitorSays, subject.Render());
        static string Said(VisitCast who, string line) => $"💬 **{who.Name}** : {line}";
        FacePair Talking(bool visitorTalks) => visitorTalks
            ? new FacePair(faces.Speaker, faces.Listener) : new FacePair(faces.Listener, faces.Speaker);

        // Tomodachi-style: a typed passion — its owner's own words — comes up once, out of nowhere, in
        // the arrival, the exchange or the parting. In those lines {S} is the Plynling whose passion it
        // is, {L} the other, {P} the typed text.
        var typedOwners = new[] { visitor, host }.Where(x => x.Passions.Any(p => p.Catalog is null)).ToList();
        var typedMoment = typedOwners.Count > 0 && rng.NextDouble() < TypedMomentChance ? (TypedMoment)rng.Next(3) : TypedMoment.None;
        var typedOwner = typedMoment == TypedMoment.None ? null : typedOwners[rng.Next(typedOwners.Count)];
        var typedIsVisitor = ReferenceEquals(typedOwner, visitor);
        string T(string t) => Expand(t, visitor.Name, visitor.Gender, host.Name, host.Gender, typedIsVisitor,
            typedOwner!.Passions.First(p => p.Catalog is null).Render());

        var beats = new List<VisitBeat>();

        // 1. Arrival — from the bond *before* the visit: two acquaintances who become friends during it
        // still arrive as acquaintances, and a scene that will go badly has not gone badly yet.
        var arrivalMood = MoodFor(true, outcome.Before);
        var typedArrival = typedMoment == TypedMoment.Arrival;
        var (arrival, arrivalTag) = Untag(pick(typedArrival ? TypedArrivals[FlavorOf(arrivalMood)] : Arrivals[arrivalMood]));
        var arrivalFace = Faces(arrivalMood).Narration;
        var f = new FacePair(arrivalFace, arrivalFace).Narrate(arrivalTag);
        beats.Add(f.Beat($"*{pick(place.Scenes)}*\n{(typedArrival ? T(arrival) : X(arrival))}"));

        // The conversation: one script whose lines answer each other — the opener (A's narration and
        // words) on its own step, from whichever of them the script says.
        var flavor = FlavorOf(mood);
        var scripts = subject.Catalog is { } passion
            ? PlynlingScripts.For(passion, flavor, shared)
            : PlynlingScripts.ForCustom(flavor, shared);
        var openerKey = pick(scripts.Select(x => x.Lines[0].Text).ToArray());
        var script = scripts.First(x => x.Lines[0].Text == openerKey);

        var opener = script.Lines[0].Text.Split('\n');
        var (narration, narrationTag) = Untag(opener[0]);
        var (said, saidTag) = Untag(opener[1]);
        f = Talking(aIsVisitor).Narrate(narrationTag).Speak(saidTag, aIsVisitor);
        beats.Add(f.Beat($"{C(narration)}\n{Said(a, C(said))}"));

        // Then the rest of the script and the closer, two lines to a step, so both of them talk on
        // it. The closer goes to whoever did not say the script's last line, so the final step is
        // always an exchange; {S} in it is whoever says it. Two lines in a row from the same one
        // share one bubble.
        var spoken = script.Lines.Skip(1).Select(line =>
        {
            var (talk, tag) = Untag(line.Text);
            return (Visitor: line.ByA == aIsVisitor, Text: C(talk), Tag: tag);
        }).ToList();
        var closerIsVisitor = !spoken[^1].Visitor;
        var (closer, closerTag) = Untag(pick(Closers[mood]));
        spoken.Add((closerIsVisitor, By(closer, closerIsVisitor), closerTag));
        foreach (var pair in spoken.Chunk(2))
        {
            f = Talking(pair[0].Visitor);
            var lines = new List<(bool Visitor, string Text)>();
            foreach (var (byVisitor, talk, tag) in pair)
            {
                f.Speak(tag, byVisitor);
                if (lines.Count > 0 && lines[^1].Visitor == byVisitor) lines[^1] = (byVisitor, $"{lines[^1].Text} {talk}");
                else lines.Add((byVisitor, talk));
            }
            beats.Add(f.Beat(string.Join("\n", lines.Select(l => Said(l.Visitor ? visitor : host, l.Text)))));
        }

        // 7. The activity, then a little exchange (visitor, then host). A typed exchange is picked by who
        // owns the passion, since the visitor always speaks first.
        var (activity, activityTag) = Untag(pick(ActivityPool(mood, info, b, rng)));
        var typedExchange = typedMoment == TypedMoment.Exchange;
        var exchange = pick(typedExchange
            ? (typedIsVisitor ? TypedExchangesOwnerFirst : TypedExchangesOwnerSecond)[flavor]
            : Exchanges[mood]).Split('\n');
        string E(string t) => typedExchange ? T(t) : X(t);
        var (first, firstTag) = Untag(exchange[0]);
        var (second, secondTag) = Untag(exchange[1]);
        f = new FacePair(faces.Narration, faces.Narration).Narrate(activityTag).Speak(firstTag, true).Speak(secondTag, false);
        beats.Add(f.Beat($"{C(activity)}\n{Said(visitor, E(first))}\n{Said(host, E(second))}"));

        // 8. Parting. What the visit did sets the faces first; a tag only colours an ordinary parting. A
        // typed parting only replaces an ordinary one — never a refusal's or a break-up's.
        var departurePool = DeparturePool(outcome, mood);
        var typedDeparture = typedMoment == TypedMoment.Departure && ReferenceEquals(departurePool, Departures[mood]);
        var (departure, departureTag) = Untag(pick(typedDeparture ? TypedDepartures[flavor] : departurePool));
        var parting = typedDeparture ? T(departure) : X(departure);
        var ending = OutcomeFace(outcome);
        f = ending is { } end ? new FacePair(end, end) : new FacePair(faces.Narration, faces.Narration).Narrate(departureTag);
        beats.Add(f.Beat(string.IsNullOrWhiteSpace(outcomeLines) ? parting : $"{parting}\n{outcomeLines}"));

        var heading = $"{place.Emoji} {place.Name.Replace("{B}", host.Name)}";
        return new VisitStory("", heading, beats, visitor, host, PlynlingCatalog.Info(outcome.Host.Species).Accent);
    }

    // The default faces by mood: whoever talks, whoever listens, and both during narration.
    private static (PlynlingMood Speaker, PlynlingMood Listener, PlynlingMood Narration) Faces(VisitMood mood) => mood switch
    {
        VisitMood.Acquaintances => (PlynlingMood.Happy, PlynlingMood.Content, PlynlingMood.Content),
        VisitMood.Rivals => (PlynlingMood.Happy, PlynlingMood.Angry, PlynlingMood.Content),
        VisitMood.Conflict => (PlynlingMood.Angry, PlynlingMood.Angry, PlynlingMood.Angry),
        _ => (PlynlingMood.Happy, PlynlingMood.Happy, PlynlingMood.Happy),
    };

    // The parting faces a visit's outcome imposes, or null for an ordinary one.
    private static PlynlingMood? OutcomeFace(VisitOutcome o) =>
        o.Confession == Confession.Refused || (o.Before == PlynlingBond.Lovers && o.After != PlynlingBond.Lovers) ? PlynlingMood.Sad
        : o.Confession == Confession.Accepted ? PlynlingMood.Happy
        : o.After == PlynlingBond.Enemies && o.Before != PlynlingBond.Enemies ? PlynlingMood.Angry
        : null;

    // Both faces while a step is being put together.
    private sealed class FacePair(PlynlingMood visitor, PlynlingMood host)
    {
        private PlynlingMood _visitor = visitor, _host = host;

        // A narration tag: [face] for both, [A:…] / [B:…] for the visitor / the host.
        public FacePair Narrate(FaceTag? tag)
        {
            if (tag is null) return this;
            if (tag.Both is { } both) _visitor = _host = both;
            if (tag.A is { } va) _visitor = va;
            if (tag.B is { } hb) _host = hb;
            return this;
        }

        // A spoken line's tag: [face] is the speaker's own face.
        public FacePair Speak(FaceTag? tag, bool visitorSpeaks)
        {
            if (tag is null) return this;
            if (tag.Both is { } face)
            {
                if (visitorSpeaks) _visitor = face;
                else _host = face;
            }
            return Narrate(tag with { Both = null });
        }

        public VisitBeat Beat(string text) => new(text, _visitor, _host);
    }

    private static readonly Regex TagRx = new(
        @"^\[(?:(happy|content|sad|angry)|(?:A:(happy|content|sad|angry))?\s*(?:B:(happy|content|sad|angry))?)\]\s*",
        RegexOptions.Compiled);

    /// <summary>
    /// Reads a face tag off the start of a line — [sad], or [A:sad B:happy] — and returns the line
    /// without it. A line with no tag comes back unchanged with a null tag.
    /// </summary>
    public static (string Text, FaceTag? Tag) Untag(string line)
    {
        var m = TagRx.Match(line);
        if (!m.Success || m.Length <= 2) return (line, null);
        static PlynlingMood? Face(Group g) => g.Success ? Enum.Parse<PlynlingMood>(g.Value, ignoreCase: true) : null;
        return (line[m.Length..], new FaceTag(Face(m.Groups[1]), Face(m.Groups[2]), Face(m.Groups[3])));
    }

    // Step 7, in a conflict: always the squabble. Otherwise, MoodActivityShare of the time an activity
    // that fits the bond rather than the subject (Activities[mood]); the rest of the time the subject's
    // own — a catalog passion's activities plus any combo with one of the listener's catalog passions,
    // or the custom ones for this mood. Combos join the passion's pool rather than replacing it: a
    // pair has one combo at most, and on its own it made every Cooking-meets-Music visit the same.
    private static string[] ActivityPool(VisitMood mood, PassionInfo? subject, VisitCast listener, Random rng)
    {
        if (mood == VisitMood.Conflict) return Activities[VisitMood.Conflict];
        if (rng.NextDouble() < MoodActivityShare) return Activities[mood];
        if (subject is null) return CustomActivities[mood];
        var combos = listener.Passions
            .Where(p => p.Catalog is { } lc && lc != subject.Passion)
            .Select(p => PlynlingPassions.ComboFor(subject.Passion, p.Catalog!.Value))
            .OfType<string[]>()
            .SelectMany(x => x);
        return subject.Activities.Concat(combos).ToArray();
    }

    // How often the activity comes from the bond's pool instead of the subject's.
    private const double MoodActivityShare = 0.3;

    // How often a visit with a typed passion on either side quotes it once outside the conversation.
    private const double TypedMomentChance = 0.45;

    private enum TypedMoment { Arrival, Exchange, Departure, None }

    private static ConvoFlavor FlavorOf(VisitMood mood) =>
        mood is VisitMood.Rivals or VisitMood.Conflict ? ConvoFlavor.Tense : ConvoFlavor.Friendly;

    // The parting's pool: the outcome decides it when the visit broke something — a refused
    // confession or a break-up — and the mood otherwise. The mood is the bond *after* the visit, so
    // a couple who just split would otherwise leave on a cheerful Friends line. These are exactly the
    // outcomes OutcomeFace turns sad.
    private static string[] DeparturePool(VisitOutcome o, VisitMood mood) =>
        o.Confession == Confession.Refused ? RefusedDepartures
        : o.Before == PlynlingBond.Lovers && o.After != PlynlingBond.Lovers ? BreakUpDepartures
        : Departures[mood];

    private static VisitCast Cast(Plynling p, DateTimeOffset now) => new(
        PlynlingCardUi.SafeName(p.Name),
        p.Species,
        PlynlingLife.Stage(p, now),
        PlynlingCatalog.Info(p.Species).Name,
        p.Gender,
        PlynlingPassions.Of(p));

    /// <summary>
    /// What the speaker talks about: a passion both share, <see cref="PlynlingPassions.SharedTopicChance"/>
    /// of the time when there is one, else any of the speaker's own.
    /// </summary>
    public static Passion PickSubject(IReadOnlyList<Passion> speaker, IReadOnlyList<Passion> listener, Random rng)
    {
        var shared = speaker.Where(p => listener.Any(p.SameAs)).ToList();
        if (shared.Count > 0 && rng.NextDouble() < PlynlingPassions.SharedTopicChance) return shared[rng.Next(shared.Count)];
        return speaker[rng.Next(speaker.Count)];
    }

    private static readonly Regex Choice = new(@"\{([abpsl]):([^|}]*)\|([^}]*)\}", RegexOptions.Compiled);

    /// <summary>
    /// Fills a template's names, pronouns and agreements. {S}/{L} are the speaker and the listener
    /// (the visitor speaks when <paramref name="visitorSpeaks"/>). {P} is inserted last, so text a
    /// person typed is never read as a template.
    /// </summary>
    public static string Expand(string template, string a, PlynlingGender ga, string b, PlynlingGender gb,
        bool visitorSpeaks = true, string? passion = null)
    {
        var girls = ga == PlynlingGender.Female && gb == PlynlingGender.Female;
        var (s, gs, l, gl) = visitorSpeaks ? (a, ga, b, gb) : (b, gb, a, ga);
        var text = Choice.Replace(template, m =>
        {
            var female = m.Groups[1].Value switch
            {
                "a" => ga == PlynlingGender.Female,
                "b" => gb == PlynlingGender.Female,
                "s" => gs == PlynlingGender.Female,
                "l" => gl == PlynlingGender.Female,
                _ => girls,
            };
            return female ? m.Groups[3].Value : m.Groups[2].Value;
        });
        text = text
            .Replace("{A}", $"**{a}**").Replace("{B}", $"**{b}**")
            .Replace("{S}", $"**{s}**").Replace("{L}", $"**{l}**")
            .Replace("{ils}", girls ? "elles" : "ils").Replace("{Ils}", girls ? "Elles" : "Ils");
        return passion is null ? text : text.Replace("{P}", passion);
    }
}
