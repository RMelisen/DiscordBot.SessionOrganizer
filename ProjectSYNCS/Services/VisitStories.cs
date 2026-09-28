using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// The visits told recently, so their ◀ ▶ can page back through them — a singleton, in memory like
// games and trade offers. The most recent Capacity are kept, the oldest forgotten; a restart
// forgets them all, and an old card's arrows then say the story is no longer available. Nothing
// is lost that mattered: what the visit did is in the database, the story is only its telling.
public sealed class VisitStories
{
    public const int Capacity = 300;

    private const string Alphabet = "abcdefghijkmnpqrstuvwxyz23456789";   // no 0/o, 1/l
    private readonly object _gate = new();
    private readonly Dictionary<string, VisitStory> _stories = new();
    private readonly Queue<string> _order = new();

    // Keeps the story under a fresh short id, and returns it carrying that id.
    public VisitStory Add(VisitStory story, Random rng)
    {
        lock (_gate)
        {
            string id;
            do id = new string(Enumerable.Range(0, 6).Select(_ => Alphabet[rng.Next(Alphabet.Length)]).ToArray());
            while (_stories.ContainsKey(id));

            var kept = story with { Id = id };
            _stories[id] = kept;
            _order.Enqueue(id);
            while (_order.Count > Capacity) _stories.Remove(_order.Dequeue());
            return kept;
        }
    }

    public VisitStory? Get(string id)
    {
        lock (_gate) return _stories.GetValueOrDefault(id);
    }
}
