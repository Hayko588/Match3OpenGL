namespace Match3.Domain
{
    public readonly struct Pos(int row, int col) : IEquatable<Pos>
    {
        public readonly int Row { get; } = row;
        public readonly int Col { get; } = col;

        public bool IsAdjacentTo(Pos other) =>
            Math.Abs(Row - other.Row) + Math.Abs(Col - other.Col) == 1;

        public bool Equals(Pos other)
        {
            return Row == other.Row && Col == other.Col;
        }
        public override bool Equals(object? o)
        {
            return o is Pos other && Equals(other);
        }
        public override int GetHashCode()
        {
            return HashCode.Combine(Row, Col);
        }

        public static bool operator ==(Pos a, Pos b) => a.Equals(b);
        public static bool operator !=(Pos a, Pos b) => !a.Equals(b);
    }
}