namespace Match3.Domain
{
    public enum GemType : byte
    {
        None = 0,
        Red = 1,
        Blue = 2,
        Green = 3,
        Yellow = 4,
        Purple = 5
    }

    public static class GemTypeHelper
    {
        private static readonly GemType[] Playable =
            [GemType.Red, GemType.Blue, GemType.Green, GemType.Yellow, GemType.Purple];

        public static GemType Random(Random rng) => Playable[rng.Next(GameConfig.GEM_COUNT)];

        public static bool IsPlayable(this GemType gem) => gem != GemType.None;
    }
}