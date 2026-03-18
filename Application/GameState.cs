using Match3.Animation;
using Match3.Domain;

namespace Match3.Application
{
    public class GameState
    {
        public ObjectPool<GemVisual> VisualPool { get; } =
            new(() => new GemVisual(), v => v.Reset(), prewarm: 64);

        private int score;
        private string scoreText = "0";
        private int lastTimerSeconds = -1;
        private string timerText = "1:00";

        public int Score
        {
            get => score;
            set
            {
                if (score == value) return;
                score = value;
                scoreText = value.ToString();
            }
        }

        public string ScoreText => scoreText;

        public float TimeRemaining { get; set; }
        public bool IsTimeUp => TimeRemaining <= 0f;

        public string TimerText
        {
            get
            {
                int secs = (int)MathF.Ceiling(TimeRemaining);
                if (secs < 0) secs = 0;
                if (secs != lastTimerSeconds)
                {
                    lastTimerSeconds = secs;
                    timerText = $"{secs / 60}:{secs % 60:D2}";
                }
                return timerText;
            }
        }

        public int TimerSeconds => (int)MathF.Ceiling(TimeRemaining);

        public Pos? SelectedCell { get; set; }
        public bool HasSelection => SelectedCell.HasValue;

        public bool IsDragging { get; set; }
        public bool DragUsed { get; set; }
        public float DragStartX { get; set; }
        public float DragStartY { get; set; }
        public Pos DragOrigin { get; set; }

        public AnimationPhase Phase { get; set; } = AnimationPhase.Idle;
        public float AnimTimer { get; set; }
        public float AnimDuration { get; set; }
        public bool IsAnimating => Phase != AnimationPhase.Idle;

        public Pos SwapFrom { get; set; }
        public Pos SwapTo { get; set; }
        public Pos? LastMoved { get; set; }

        public HashSet<Pos> DestroySet { get; set; } = [];
        public List<BonusPlacement> BonusCreations { get; set; } = [];
        public List<Pos> PendingBombs { get; set; } = [];
        public HashSet<Pos> BombDestroySet { get; set; } = [];
        public int CascadeLevel { get; set; }

        public List<(GemVisual Vis, float FromY, float ToY)> FallData { get; } = [];
        public List<(GemVisual Vis, float FromY, float ToY)> SpawnData { get; } = [];

        public GemVisual?[,] Visuals { get; } = new GemVisual[Board.SIZE, Board.SIZE];

        public float Time { get; set; }

        public float HoverX { get; set; }
        public float HoverY { get; set; }

        public void StartPhase(AnimationPhase phase, float duration)
        {
            Phase = phase;
            AnimTimer = 0f;
            AnimDuration = duration;
        }

        public void ReturnVisual(int row, int col)
        {
            if (Visuals[row, col] is { } vis)
            {
                VisualPool.Return(vis);
                Visuals[row, col] = null;
            }
        }

        public void ReturnVisual(Pos pos) => ReturnVisual(pos.Row, pos.Col);

        public GemVisual PlaceVisual(Pos pos, GemType gem, BonusType bonus, float x, float y)
        {
            var vis = VisualPool.Get();
            vis.Init(gem, bonus, x, y);
            Visuals[pos.Row, pos.Col] = vis;
            return vis;
        }

        public void ResetForNewGame()
        {
            Score = 0;
            TimeRemaining = GameConfig.GAME_DURATION;
            lastTimerSeconds = -1;
            Phase = AnimationPhase.Idle;
            SelectedCell = null;
            IsDragging = false;
            DragUsed = false;
            CascadeLevel = 0;
            DestroySet.Clear();
            BonusCreations.Clear();
            PendingBombs.Clear();
            BombDestroySet.Clear();
            FallData.Clear();
            SpawnData.Clear();

            for (int r = 0; r < Board.SIZE; r++)
                for (int c = 0; c < Board.SIZE; c++)
                    ReturnVisual(r, c);
        }

        public void ClearSelection() => SelectedCell = null;

        public void UpdateVisualPositions(Rendering.BoardLayout layout)
        {
            for (int r = 0; r < Board.SIZE; r++)
                for (int c = 0; c < Board.SIZE; c++)
                {
                    if (Visuals[r, c] is not { } vis) continue;

                    if (Phase == AnimationPhase.Idle)
                    {
                        var center = layout.CellCenter(new Pos(r, c));
                        vis.X = center.X;
                        vis.Y = center.Y;
                    }
                }

            for (int i = 0; i < FallData.Count; i++)
            {
                var (vis, _, oldToY) = FallData[i];
                for (int r = 0; r < Board.SIZE; r++)
                    for (int c = 0; c < Board.SIZE; c++)
                        if (Visuals[r, c] == vis)
                        {
                            float newToY = layout.CellCenter(new Pos(r, c)).Y;
                            FallData[i] = (vis, vis.Y, newToY);
                        }
            }

            for (int i = 0; i < SpawnData.Count; i++)
            {
                var (vis, _, oldToY) = SpawnData[i];
                for (int r = 0; r < Board.SIZE; r++)
                    for (int c = 0; c < Board.SIZE; c++)
                        if (Visuals[r, c] == vis)
                        {
                            float newToY = layout.CellCenter(new Pos(r, c)).Y;
                            float newStartY = layout.BoardY - layout.CellSize;
                            SpawnData[i] = (vis, newStartY, newToY);
                        }
            }

            if (Phase == AnimationPhase.Idle)
            {
                for (int r = 0; r < Board.SIZE; r++)
                    for (int c = 0; c < Board.SIZE; c++)
                        if (Visuals[r, c] is { } v)
                        {
                            var center = layout.CellCenter(new Pos(r, c));
                            v.X = center.X;
                            v.Y = center.Y;
                        }
            }
        }
    }
}