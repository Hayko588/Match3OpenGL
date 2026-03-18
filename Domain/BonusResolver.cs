namespace Match3.Domain
{
    public struct BonusPlacement
    {
        public Pos Position { get; }
        public BonusType Bonus { get; }
        public GemType Gem { get; }

        public BonusPlacement(Pos position, BonusType bonus, GemType gem)
        {
            Position = position;
            Bonus = bonus;
            Gem = gem;
        }
    }

    public sealed class ResolveResult
    {
        public required HashSet<Pos> DestroySet { get; init; }
        public required List<BonusPlacement> BonusCreations { get; init; }
        public required List<Pos> PendingBombs { get; init; }
        public int DestroyedCount => DestroySet.Count;
    }

    public sealed class BonusResolver
    {
        public ResolveResult Resolve(Board board, List<MatchInfo> matches, Pos? lastMoved)
        {
            var allMatched = MatchDetector.CollectAllPositions(matches);
            var bonusCreations = DetermineBonusCreations(board, matches, lastMoved);
            var bonusPositions = bonusCreations.Select(b => b.Position).ToHashSet();

            var destroySet = new HashSet<Pos>(allMatched);
            var pendingBombs = ActivateBonuses(board, destroySet);

            foreach (var bp in bonusPositions)
                destroySet.Remove(bp);

            return new ResolveResult
            {
                DestroySet = destroySet,
                BonusCreations = bonusCreations,
                PendingBombs = pendingBombs
            };
        }

        public (HashSet<Pos> Destroyed, List<Pos> ChainBombs) ResolveBombExplosion(
            Board board, List<Pos> pendingBombs)
        {
            var destroyed = new HashSet<Pos>();
            var chainBombs = new List<Pos>();

            foreach (var bomb in pendingBombs)
            {
                for (int dr = -1; dr <= 1; dr++)
                    for (int dc = -1; dc <= 1; dc++)
                    {
                        var target = new Pos(bomb.Row + dr, bomb.Col + dc);
                        if (!board.InBounds(target) || board[target].IsEmpty) continue;

                        destroyed.Add(target);
                        var cell = board[target];
                        if (!cell.HasBonus) continue;

                        switch (cell.Bonus)
                        {
                            case BonusType.LineH:
                                DestroyRow(board, target.Row, destroyed);
                                break;
                            case BonusType.LineV:
                                DestroyColumn(board, target.Col, destroyed);
                                break;
                            case BonusType.Bomb:
                                chainBombs.Add(target);
                                break;
                        }
                    }
            }

            return (destroyed, chainBombs);
        }

        private static List<BonusPlacement> DetermineBonusCreations(
            Board board, List<MatchInfo> matches, Pos? lastMoved)
        {
            var creations = new List<BonusPlacement>();
            var taken = new HashSet<Pos>();

            var crosses = MatchDetector.FindCrossIntersections(matches);
            foreach (var pos in crosses)
            {
                creations.Add(new BonusPlacement(pos, BonusType.Bomb, board[pos].Gem));
                taken.Add(pos);
            }

            foreach (var match in matches)
            {
                if (match.BonusType == BonusType.None) continue;

                var pos = PickBonusPosition(match, taken, lastMoved);
                if (pos is null) continue;

                creations.Add(new BonusPlacement(pos.Value, match.BonusType, match.Gem));
                taken.Add(pos.Value);
            }

            return creations;
        }

        private static Pos? PickBonusPosition(MatchInfo match, HashSet<Pos> taken, Pos? lastMoved)
        {
            if (lastMoved.HasValue && match.Positions.Contains(lastMoved.Value) && !taken.Contains(lastMoved.Value))
                return lastMoved.Value;

            var mid = match.Positions[match.Positions.Count / 2];
            if (!taken.Contains(mid)) return mid;

            return match.Positions.FirstOrDefault(p => !taken.Contains(p));
        }

        private static List<Pos> ActivateBonuses(Board board, HashSet<Pos> destroySet)
        {
            var pendingBombs = new List<Pos>();
            var queue = new Queue<Pos>();
            var activated = new HashSet<Pos>();

            foreach (var pos in destroySet)
                if (board[pos].HasBonus)
                    queue.Enqueue(pos);

            while (queue.Count > 0)
            {
                var pos = queue.Dequeue();
                if (!activated.Add(pos)) continue;

                var cell = board[pos];
                if (!cell.HasBonus) continue;

                switch (cell.Bonus)
                {
                    case BonusType.LineH:
                        EnqueueNewBonuses(board, DestroyRow(board, pos.Row, destroySet), destroySet, queue);
                        break;
                    case BonusType.LineV:
                        EnqueueNewBonuses(board, DestroyColumn(board, pos.Col, destroySet), destroySet, queue);
                        break;
                    case BonusType.Bomb:
                        pendingBombs.Add(pos);
                        break;
                }
            }

            return pendingBombs;
        }

        private static List<Pos> DestroyRow(Board board, int row, HashSet<Pos> destroySet)
        {
            var added = new List<Pos>();
            for (int c = 0; c < Board.SIZE; c++)
            {
                var p = new Pos(row, c);
                if (destroySet.Add(p)) added.Add(p);
            }
            return added;
        }

        private static List<Pos> DestroyColumn(Board board, int col, HashSet<Pos> destroySet)
        {
            var added = new List<Pos>();
            for (int r = 0; r < Board.SIZE; r++)
            {
                var p = new Pos(r, col);
                if (destroySet.Add(p)) added.Add(p);
            }
            return added;
        }

        private static void EnqueueNewBonuses(
            Board board,
            List<Pos> newPositions,
            HashSet<Pos> destroySet,
            Queue<Pos> queue)
        {
            foreach (var p in newPositions)
                if (board[p].HasBonus)
                    queue.Enqueue(p);
        }
    }
}