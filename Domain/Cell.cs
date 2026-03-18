namespace Match3.Domain
{
    public struct Cell(GemType gem, BonusType bonus = BonusType.None)
    {
        public GemType Gem = gem;
        public BonusType Bonus = bonus;

        public static readonly Cell Empty = new(GemType.None);

        public readonly bool IsEmpty => Gem == GemType.None;
        public readonly bool HasBonus => Bonus != BonusType.None;
    }
}