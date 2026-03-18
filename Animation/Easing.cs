namespace Match3.Animation
{
    public static class Easing
    {
        public static float Linear(float t) => t;

        public static float QuadOut(float t) => t * (2f - t);

        public static float CubicOut(float t) { t--; return 1f + t * t * t; }

        public static float BackOut(float t)
        {
            const float C = 1.70158f;
            t--;
            return 1f + t * t * ((C + 1f) * t + C);
        }

        public static float BounceOut(float t)
        {
            const float N = 7.5625f;
            const float D = 2.75f;

            if (t < 1f / D) return N * t * t;
            if (t < 2f / D) return N * (t -= 1.5f / D) * t + 0.75f;
            if (t < 2.5f / D) return N * (t -= 2.25f / D) * t + 0.9375f;
            return N * (t -= 2.625f / D) * t + 0.984375f;
        }
    }
}