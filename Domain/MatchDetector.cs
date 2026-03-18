namespace Match3.Domain
{
    public class MatchDetector
    {
        public List<MatchInfo> FindMatches(Board board)
        {
            var matches = new List<MatchInfo>();
            ScanLines(board, horizontal: true, matches);
            ScanLines(board, horizontal: false, matches);
            return matches;
        }

        public static HashSet<Pos> CollectAllPositions(List<MatchInfo> matches)
        {
            var set = new HashSet<Pos>();
            foreach (var m in matches)
                foreach (var p in m.Positions)
                    set.Add(p);
            return set;
        }

        public static HashSet<Pos> FindCrossIntersections(List<MatchInfo> matches)
        {
            var result = new HashSet<Pos>();
            var horizontals = matches.Where(m => m.IsHorizontal).ToList();
            var verticals = matches.Where(m => !m.IsHorizontal).ToList();

            foreach (var h in horizontals)
            {
                var hSet = new HashSet<Pos>(h.Positions);
                foreach (var v in verticals)
                {
                    if (v.Gem != h.Gem) continue;
                    foreach (var pos in v.Positions)
                        if (hSet.Contains(pos))
                            result.Add(pos);
                }
            }
            return result;
        }

        private static void ScanLines(Board board, bool horizontal, List<MatchInfo> results)
        {
            int outerLimit = Board.SIZE;
            int innerLimit = Board.SIZE;

            for (int outer = 0; outer < outerLimit; outer++)
            {
                int inner = 0;
                while (inner < innerLimit)
                {
                    var pos = horizontal ? new Pos(outer, inner) : new Pos(inner, outer);
                    var gem = board[pos].Gem;

                    if (!gem.IsPlayable()) { inner++; continue; }

                    int start = inner;
                    while (inner < innerLimit)
                    {
                        var nextPos = horizontal ? new Pos(outer, inner) : new Pos(inner, outer);
                        if (board[nextPos].Gem != gem) break;
                        inner++;
                    }

                    if (inner - start < 3) continue;

                    var positions = new List<Pos>(inner - start);
                    for (int i = start; i < inner; i++)
                        positions.Add(horizontal ? new Pos(outer, i) : new Pos(i, outer));

                    results.Add(new MatchInfo
                    {
                        Gem = gem,
                        Positions = positions,
                        IsHorizontal = horizontal
                    });
                }
            }
        }
    }
}