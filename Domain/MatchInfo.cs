namespace Match3.Domain
{
    public class MatchInfo
    {
        public required GemType Gem { get; init; }
        public required List<Pos> Positions { get; init; }
        public required bool IsHorizontal { get; init; }

        public int Length => Positions.Count;

        public BonusType BonusType => Length switch
        {
            >= 5 => BonusType.Bomb,
            4 => IsHorizontal ? BonusType.LineH : BonusType.LineV,
            _ => BonusType.None
        };
    }
}