namespace ProjectSYNCS.Helpers;

// Which board an /arcade board game is played on. Rides in nothing stored.
public enum BoardKind { Connect4, TicTacToe }

/// <summary>
/// Puissance 4, pure: a 6×7 board as 42 cells (row 0 at the top), 0 empty, 1 and 2 the players.
/// Her play is a depth-limited negamax with alpha-beta: strong enough to beat a careless player,
/// beatable by a careful one.
/// </summary>
public static class Connect4
{
    public const int Rows = 6;
    public const int Cols = 7;
    public const int Depth = 4;

    public static int[] NewBoard() => new int[Rows * Cols];

    /// <summary>Drops a piece; returns the row it landed in, or −1 when the column is full.</summary>
    public static int Drop(int[] board, int col, int player)
    {
        if (col is < 0 or >= Cols) return -1;
        for (var r = Rows - 1; r >= 0; r--)
        {
            if (board[r * Cols + col] != 0) continue;
            board[r * Cols + col] = player;
            return r;
        }
        return -1;
    }

    public static bool CanDrop(int[] board, int col) => board[col] == 0;
    public static bool Full(int[] board) => Enumerable.Range(0, Cols).All(c => board[c] != 0);

    private static readonly (int Dr, int Dc)[] Directions = { (0, 1), (1, 0), (1, 1), (1, -1) };

    /// <summary>0 while nobody has four in a row, else the player who does.</summary>
    public static int Winner(int[] board)
    {
        for (var r = 0; r < Rows; r++)
        for (var c = 0; c < Cols; c++)
        {
            var p = board[r * Cols + c];
            if (p == 0) continue;
            foreach (var (dr, dc) in Directions)
            {
                var k = 1;
                while (k < 4)
                {
                    int rr = r + dr * k, cc = c + dc * k;
                    if (rr is < 0 or >= Rows || cc is < 0 or >= Cols || board[rr * Cols + cc] != p) break;
                    k++;
                }
                if (k == 4) return p;
            }
        }
        return 0;
    }

    /// <summary>Her move for <paramref name="me"/>; ties broken at random so she isn't predictable.</summary>
    public static int BestMove(int[] board, int me, Random rng)
    {
        var order = new[] { 3, 2, 4, 1, 5, 0, 6 };
        var best = int.MinValue;
        var moves = new List<int>();
        foreach (var col in order)
        {
            if (!CanDrop(board, col)) continue;
            var copy = (int[])board.Clone();
            Drop(copy, col, me);
            var score = -Negamax(copy, Depth - 1, int.MinValue + 1, int.MaxValue, 3 - me);
            if (score > best) { best = score; moves.Clear(); }
            if (score == best) moves.Add(col);
        }
        return moves.Count == 0 ? -1 : moves[rng.Next(moves.Count)];
    }

    private static int Negamax(int[] board, int depth, int alpha, int beta, int player)
    {
        var winner = Winner(board);
        if (winner != 0) return winner == player ? 100_000 + depth : -100_000 - depth;
        if (Full(board)) return 0;
        if (depth == 0) return Evaluate(board, player);

        foreach (var col in new[] { 3, 2, 4, 1, 5, 0, 6 })
        {
            if (!CanDrop(board, col)) continue;
            var copy = (int[])board.Clone();
            Drop(copy, col, player);
            var score = -Negamax(copy, depth - 1, -beta, -alpha, 3 - player);
            if (score > alpha) alpha = score;
            if (alpha >= beta) break;
        }
        return alpha;
    }

    // Every window of four counts for whoever alone holds pieces in it, the centre a little more.
    private static int Evaluate(int[] board, int player)
    {
        var score = 0;
        for (var r = 0; r < Rows; r++) score += (board[r * Cols + 3] == player ? 3 : board[r * Cols + 3] == 3 - player ? -3 : 0);
        for (var r = 0; r < Rows; r++)
        for (var c = 0; c < Cols; c++)
        foreach (var (dr, dc) in Directions)
        {
            int mine = 0, theirs = 0, k;
            for (k = 0; k < 4; k++)
            {
                int rr = r + dr * k, cc = c + dc * k;
                if (rr is < 0 or >= Rows || cc is < 0 or >= Cols) break;
                var v = board[rr * Cols + cc];
                if (v == player) mine++; else if (v != 0) theirs++;
            }
            if (k < 4 || (mine > 0 && theirs > 0)) continue;
            score += mine switch { 3 => 50, 2 => 5, _ => 0 } - theirs switch { 3 => 60, 2 => 5, _ => 0 };
        }
        return score;
    }

    public static string Render(int[] board)
    {
        var sb = new System.Text.StringBuilder();
        for (var r = 0; r < Rows; r++)
        {
            for (var c = 0; c < Cols; c++)
                sb.Append(board[r * Cols + c] switch { 1 => "🔴", 2 => "🟡", _ => "⚫" });
            sb.Append('\n');
        }
        sb.Append("1️⃣2️⃣3️⃣4️⃣5️⃣6️⃣7️⃣");
        return sb.ToString();
    }
}

/// <summary>Morpion, pure: 9 cells, 0 empty, 1 and 2 the players. Her play is a full minimax: she never loses.</summary>
public static class TicTacToe
{
    public static int[] NewBoard() => new int[9];

    private static readonly int[][] Lines =
    {
        new[] { 0, 1, 2 }, new[] { 3, 4, 5 }, new[] { 6, 7, 8 },
        new[] { 0, 3, 6 }, new[] { 1, 4, 7 }, new[] { 2, 5, 8 },
        new[] { 0, 4, 8 }, new[] { 2, 4, 6 },
    };

    public static int Winner(int[] board)
    {
        foreach (var l in Lines)
            if (board[l[0]] != 0 && board[l[0]] == board[l[1]] && board[l[1]] == board[l[2]]) return board[l[0]];
        return 0;
    }

    public static bool Full(int[] board) => board.All(c => c != 0);

    public static int BestMove(int[] board, int me, Random rng)
    {
        var best = int.MinValue;
        var moves = new List<int>();
        for (var i = 0; i < 9; i++)
        {
            if (board[i] != 0) continue;
            board[i] = me;
            var score = -Minimax(board, 3 - me, 1);
            board[i] = 0;
            if (score > best) { best = score; moves.Clear(); }
            if (score == best) moves.Add(i);
        }
        return moves.Count == 0 ? -1 : moves[rng.Next(moves.Count)];
    }

    private static int Minimax(int[] board, int player, int depth)
    {
        var w = Winner(board);
        if (w != 0) return w == player ? 10 - depth : depth - 10;
        if (Full(board)) return 0;
        var best = int.MinValue;
        for (var i = 0; i < 9; i++)
        {
            if (board[i] != 0) continue;
            board[i] = player;
            best = Math.Max(best, -Minimax(board, 3 - player, depth + 1));
            board[i] = 0;
        }
        return best;
    }

    public static string Symbol(int cell) => cell switch { 1 => "❌", 2 => "⭕", _ => "⬜" };
}
