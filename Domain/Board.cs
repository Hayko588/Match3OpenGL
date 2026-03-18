namespace Match3.Domain
{
    public class Board
    {
        public const int SIZE = GameConfig.BOARD_SIZE;

        private readonly Cell[,] cells = new Cell[SIZE, SIZE];

        public Cell this[Pos p]
        {
            get => cells[p.Row, p.Col];
            set => cells[p.Row, p.Col] = value;
        }

        public Cell this[int row, int col]
        {
            get => cells[row, col];
            set => cells[row, col] = value;
        }

        public bool InBounds(Pos p) => InBounds(p.Row, p.Col);

        public bool InBounds(int row, int col) =>
            row >= 0 && row < SIZE && col >= 0 && col < SIZE;

        public void Fill(Random rng)
        {
            for (int row = 0; row < SIZE; row++)
                for (int col = 0; col < SIZE; col++)
                    cells[row, col] = GenerateSafeCell(row, col, rng);
        }

        public void Swap(Pos a, Pos b) =>
            (cells[a.Row, a.Col], cells[b.Row, b.Col]) = (cells[b.Row, b.Col], cells[a.Row, a.Col]);

        public List<(Pos From, Pos To)> ApplyGravity()
        {
            var moves = new List<(Pos, Pos)>();

            for (int col = 0; col < SIZE; col++)
            {
                int writeRow = SIZE - 1;
                for (int readRow = SIZE - 1; readRow >= 0; readRow--)
                {
                    if (cells[readRow, col].IsEmpty) continue;

                    if (readRow != writeRow)
                    {
                        cells[writeRow, col] = cells[readRow, col];
                        cells[readRow, col] = Cell.Empty;
                        moves.Add((new Pos(readRow, col), new Pos(writeRow, col)));
                    }
                    writeRow--;
                }
            }

            return moves;
        }

        public List<Pos> FillEmpty(Random rng)
        {
            var filled = new List<Pos>();

            for (int col = 0; col < SIZE; col++)
                for (int row = 0; row < SIZE; row++)
                    if (cells[row, col].IsEmpty)
                    {
                        cells[row, col] = new Cell(GemTypeHelper.Random(rng));
                        filled.Add(new Pos(row, col));
                    }

            return filled;
        }

        public bool HasAnyValidMove(MatchDetector detector)
        {
            for (int r = 0; r < SIZE; r++)
                for (int c = 0; c < SIZE; c++)
                {
                    if (c + 1 < SIZE && HasMatchAfterSwap(new Pos(r, c), new Pos(r, c + 1), detector))
                        return true;
                    if (r + 1 < SIZE && HasMatchAfterSwap(new Pos(r, c), new Pos(r + 1, c), detector))
                        return true;
                }
            return false;
        }

        public void Reshuffle(Random rng, MatchDetector detector)
        {
            var gems = new List<GemType>();
            var positions = new List<Pos>();

            for (int r = 0; r < SIZE; r++)
                for (int c = 0; c < SIZE; c++)
                    if (!cells[r, c].HasBonus && cells[r, c].Gem.IsPlayable())
                    {
                        gems.Add(cells[r, c].Gem);
                        positions.Add(new Pos(r, c));
                    }

            for (int attempt = 0; attempt < 20; attempt++)
            {
                ShuffleList(gems, rng);
                for (int i = 0; i < positions.Count; i++)
                    cells[positions[i].Row, positions[i].Col] = new Cell(gems[i]);

                if (HasAnyValidMove(detector) && detector.FindMatches(this).Count == 0)
                    return;
            }

            Fill(rng);
        }

        private bool HasMatchAfterSwap(Pos a, Pos b, MatchDetector detector)
        {
            Swap(a, b);
            bool found = detector.FindMatches(this).Count > 0;
            Swap(a, b);
            return found;
        }

        private Cell GenerateSafeCell(int row, int col, Random rng)
        {
            for (int attempt = 0; attempt < GameConfig.MAX_FILL_ATTEMPTS; attempt++)
            {
                var gem = GemTypeHelper.Random(rng);
                if (!WouldCreateMatch(row, col, gem))
                    return new Cell(gem);
            }
            return new Cell(GemTypeHelper.Random(rng));
        }

        private bool WouldCreateMatch(int row, int col, GemType gem)
        {
            if (col >= 2 && cells[row, col - 1].Gem == gem && cells[row, col - 2].Gem == gem)
                return true;
            if (row >= 2 && cells[row - 1, col].Gem == gem && cells[row - 2, col].Gem == gem)
                return true;
            return false;
        }

        private static void ShuffleList<T>(List<T> list, Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}