using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

/// <summary>
/// What she offered to set up in chat, waiting for its asker's click: a session, a `/tonight` call or
/// a vote, read from their message (ChatRequest). <see cref="Options"/> is the vote's, empty otherwise.
/// </summary>
public sealed record ChatOffer(ulong AskerId, string Title, DateTimeOffset? At, IReadOnlyList<string> Options);

/// <summary>What a click on a forgotten offer is told: it expired (or she restarted), and the command that does it.</summary>
public static class ChatOfferText
{
    public static string Expired(string command) =>
        $"Cette proposition a expiré : je les garde une heure. Passe par {command}, ou redemande-moi.";
}

/// <summary>
/// Her open offers, by id: the id rides in the button's custom-id (`chatreq:session:{id}`…), the
/// offer stays here. In memory, an hour without a click and it's gone, and a restart forgets them
/// all (by design, like every draft): the button then says so and names the command. Singleton.
/// </summary>
public sealed class ChatOffers
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromHours(1);

    private readonly DraftBox<ChatOffer> _offers = new(Lifetime);
    private long _next;

    public int Add(ChatOffer offer)
    {
        var id = (int)(Interlocked.Increment(ref _next) % int.MaxValue);
        _offers.Set((ulong)id, offer);
        return id;
    }

    public bool TryGet(int id, out ChatOffer offer) => _offers.TryGet((ulong)id, out offer!);

    /// <summary>Removes and returns the offer: of two clicks on a one-shot offer, one gets it.</summary>
    public bool TryTake(int id, out ChatOffer offer) => _offers.TryTake((ulong)id, out offer!);

    /// <summary>Puts a taken offer back, when what it was taken for failed.</summary>
    public void PutBack(int id, ChatOffer offer) => _offers.Set((ulong)id, offer);
}
