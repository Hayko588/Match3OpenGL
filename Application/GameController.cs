using Match3.Animation;
using Match3.Domain;
using Match3.Rendering;

namespace Match3.Application
{
    public class GameController
    {
        private readonly Board board;
        private readonly MatchDetector detector;
        private readonly BonusResolver bonusResolver;
        private readonly GameState state;
        private readonly BoardLayout layout;
        private readonly VfxManager vfx;
        private readonly Random rng;

        public Board Board => board;

        public GameController(Board board, MatchDetector detector, BonusResolver bonusResolver,
            GameState state, BoardLayout layout, VfxManager vfx, Random rng)
        {
            this.board = board;
            this.detector = detector;
            this.bonusResolver = bonusResolver;
            this.state = state;
            this.layout = layout;
            this.vfx = vfx;
            this.rng = rng;
        }

        public void StartNewGame()
        {
            state.ResetForNewGame();
            board.Fill(rng);
            RebuildVisuals();
        }

        public void TrySwap(Pos from, Pos to)
        {
            board.Swap(from, to);
            state.SwapFrom = from;
            state.SwapTo = to;
            state.CascadeLevel = 0;
            state.LastMoved = to;
            state.StartPhase(AnimationPhase.Swapping, GameConfig.SWAP_DURATION);
        }

        public void OnPhaseComplete()
        {
            switch (state.Phase)
            {
                case AnimationPhase.Swapping:
                    CompleteSwap();
                    break;

                case AnimationPhase.SwapBack:
                    SwapVisuals(state.SwapFrom, state.SwapTo);
                    state.Phase = AnimationPhase.Idle;
                    break;

                case AnimationPhase.Destroying:
                    ApplyDestruction();
                    if (state.PendingBombs.Count > 0)
                        BeginBombExplosion();
                    else
                        BeginGravity();
                    break;

                case AnimationPhase.BombExploding:
                    ApplyBombDestruction();
                    if (state.PendingBombs.Count > 0)
                        BeginBombExplosion();
                    else
                        BeginGravity();
                    break;

                case AnimationPhase.Gravity:
                    SnapFallPositions();
                    BeginSpawn();
                    break;

                case AnimationPhase.Spawning:
                    SnapSpawnPositions();
                    CheckCascadeOrFinish();
                    break;
            }
        }

        private void CompleteSwap()
        {
            SwapVisuals(state.SwapFrom, state.SwapTo);

            var matches = detector.FindMatches(board);
            if (matches.Count == 0)
            {
                board.Swap(state.SwapFrom, state.SwapTo);
                state.StartPhase(AnimationPhase.SwapBack, GameConfig.SWAP_BACK_DURATION);
                return;
            }

            BeginDestroy(matches);
        }

        private void BeginDestroy(List<MatchInfo> matches)
        {
            var result = bonusResolver.Resolve(board, matches, state.LastMoved);

            state.DestroySet = result.DestroySet;
            state.BonusCreations = result.BonusCreations;
            state.PendingBombs = result.PendingBombs;

            int points = result.DestroyedCount * GameConfig.POINTS_PER_GEM * (1 + state.CascadeLevel);
            state.Score += points;

            vfx.SpawnMatchParticles(state.DestroySet, state.Visuals);

            foreach (var pos in state.DestroySet)
            {
                var cell = board[pos];
                if (!cell.HasBonus) continue;
                var center = layout.CellCenter(pos);
                var color = GameConfig.GetGemColor(cell.Gem);

                if (cell.Bonus == BonusType.LineH)
                    vfx.SpawnDestroyers(center, horizontal: true, color, layout.CellSize);
                else if (cell.Bonus == BonusType.LineV)
                    vfx.SpawnDestroyers(center, horizontal: false, color, layout.CellSize);
            }

            foreach (var bombPos in state.PendingBombs)
                vfx.SpawnExplosion(layout.CellCenter(bombPos), layout.CellSize);

            state.StartPhase(AnimationPhase.Destroying, GameConfig.DESTROY_DURATION);
        }

        private void ApplyDestruction()
        {
            foreach (var pos in state.DestroySet)
            {
                board[pos] = Cell.Empty;
                state.ReturnVisual(pos);
            }

            foreach (var bc in state.BonusCreations)
            {
                board[bc.Position] = new Cell(bc.Gem, bc.Bonus);
                var center = layout.CellCenter(bc.Position);
                state.PlaceVisual(bc.Position, bc.Gem, bc.Bonus, center.X, center.Y);
            }
        }

        private void BeginBombExplosion()
        {
            var (destroyed, chainBombs) = bonusResolver.ResolveBombExplosion(board, state.PendingBombs);

            state.BombDestroySet = destroyed;
            state.PendingBombs = chainBombs;

            int points = destroyed.Count * GameConfig.POINTS_PER_GEM * (1 + state.CascadeLevel);
            state.Score += points;
            vfx.SpawnMatchParticles(destroyed, state.Visuals);

            foreach (var pos in destroyed)
            {
                var cell = board[pos];
                if (!cell.HasBonus) continue;
                var center = layout.CellCenter(pos);
                var color = GameConfig.GetGemColor(cell.Gem);

                if (cell.Bonus == BonusType.LineH)
                    vfx.SpawnDestroyers(center, true, color, layout.CellSize);
                else if (cell.Bonus == BonusType.LineV)
                    vfx.SpawnDestroyers(center, false, color, layout.CellSize);
                else if (cell.Bonus == BonusType.Bomb)
                    vfx.SpawnExplosion(center, layout.CellSize);
            }

            state.StartPhase(AnimationPhase.BombExploding, GameConfig.BOMB_EXPLODE_DURATION);
        }

        private void ApplyBombDestruction()
        {
            foreach (var pos in state.BombDestroySet)
            {
                if (!board.InBounds(pos) || board[pos].IsEmpty) continue;
                board[pos] = Cell.Empty;
                state.ReturnVisual(pos);
            }
        }

        private void BeginGravity()
        {
            var moves = board.ApplyGravity();
            state.FallData.Clear();

            foreach (var (from, to) in moves)
            {
                var vis = state.Visuals[from.Row, from.Col];
                if (vis is null) continue;

                float fromY = vis.Y;
                float toY = layout.CellCenter(to).Y;

                state.Visuals[to.Row, to.Col] = vis;
                state.Visuals[from.Row, from.Col] = null;
                state.FallData.Add((vis, fromY, toY));
            }

            if (state.FallData.Count > 0)
                state.StartPhase(AnimationPhase.Gravity, GameConfig.GRAVITY_DURATION);
            else
                BeginSpawn();
        }

        private void BeginSpawn()
        {
            var filled = board.FillEmpty(rng);
            state.SpawnData.Clear();

            foreach (var pos in filled)
            {
                var center = layout.CellCenter(pos);
                float startY = layout.BoardY - layout.CellSize;
                var cell = board[pos];

                var vis = state.PlaceVisual(pos, cell.Gem, cell.Bonus, center.X, startY);
                state.SpawnData.Add((vis, startY, center.Y));
            }

            if (state.SpawnData.Count > 0)
                state.StartPhase(AnimationPhase.Spawning, GameConfig.SPAWN_DURATION);
            else
                CheckCascadeOrFinish();
        }

        private void CheckCascadeOrFinish()
        {
            var cascade = detector.FindMatches(board);
            if (cascade.Count > 0)
            {
                state.CascadeLevel++;
                state.LastMoved = null;
                BeginDestroy(cascade);
                return;
            }

            if (!board.HasAnyValidMove(detector))
            {
                board.Reshuffle(rng, detector);
                RebuildVisuals();
            }

            state.Phase = AnimationPhase.Idle;
        }

        private void SnapFallPositions()
        {
            foreach (var (vis, _, toY) in state.FallData)
                vis.Y = toY;
        }

        private void SnapSpawnPositions()
        {
            foreach (var (vis, _, toY) in state.SpawnData)
                vis.Y = toY;
        }

        public void RebuildVisuals()
        {
            for (int r = 0; r < Board.SIZE; r++)
                for (int c = 0; c < Board.SIZE; c++)
                    state.ReturnVisual(r, c);

            for (int r = 0; r < Board.SIZE; r++)
                for (int c = 0; c < Board.SIZE; c++)
                {
                    var cell = board[r, c];
                    if (cell.IsEmpty) continue;
                    var center = layout.CellCenter(new Pos(r, c));
                    state.PlaceVisual(new Pos(r, c), cell.Gem, cell.Bonus, center.X, center.Y);
                }
        }

        private void SwapVisuals(Pos a, Pos b)
        {
            (state.Visuals[a.Row, a.Col], state.Visuals[b.Row, b.Col]) =
                (state.Visuals[b.Row, b.Col], state.Visuals[a.Row, a.Col]);

            var pa = layout.CellCenter(a);
            var pb = layout.CellCenter(b);
            var va = state.Visuals[a.Row, a.Col];
            var vb = state.Visuals[b.Row, b.Col];
            if (va != null) { va.X = pa.X; va.Y = pa.Y; }
            if (vb != null) { vb.X = pb.X; vb.Y = pb.Y; }
        }
    }
}