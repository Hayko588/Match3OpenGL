using Match3.Animation;
using Match3.Domain;
using Match3.Rendering;
using System.Numerics;

namespace Match3.Application.Scenes
{
    public sealed class GameScene(
        SpriteBatch batch,
        GemBatch gemBatch,
        TextRenderer text,
        GemRenderer gemRenderer,
        BoardLayout layout,
        GameState state,
        GameController controller,
        AnimationUpdater animator,
        VfxManager vfx,
        SceneManager sceneManager) : IScene
    {
        public SceneId Id => SceneId.Game;

        public void Enter()
        {
            vfx.Clear();
            controller.StartNewGame();
        }

        public void OnResize()
        {
            state.UpdateVisualPositions(layout);
        }

        public void Update(float dt)
        {
            UpdateTimer(dt);
            UpdateAnimation(dt);
        }

        public void Render()
        {
            var proj = layout.Projection;

            batch.Begin(proj);
            DrawBoard();
            DrawSelectionGlows();
            batch.End();

            gemBatch.Begin(proj);
            DrawGemShapes();
            gemBatch.End();

            batch.Begin(proj);
            DrawBonusIndicators();
            vfx.Render(batch, text, state.Time);
            DrawHUD();
            batch.End();
        }

        public void OnMouseDown(float x, float y)
        {
            state.IsDragging = true;
            state.DragUsed = false;
            state.DragStartX = x;
            state.DragStartY = y;
            state.DragOrigin = layout.ScreenToCell(x, y) ?? new Pos(-1, -1);
        }

        public void OnMouseUp(float x, float y)
        {
            if (state.IsDragging && !state.DragUsed)
                HandleClick(x, y);
            state.IsDragging = false;
            state.DragUsed = false;
        }

        public void OnMouseMove(float x, float y)
        {
            state.HoverX = x;
            state.HoverY = y;

            if (!state.IsDragging || state.DragUsed) return;
            if (state.IsAnimating || state.IsTimeUp) return;

            var origin = state.DragOrigin;
            if (!controller.Board.InBounds(origin)) return;

            float dx = x - state.DragStartX;
            float dy = y - state.DragStartY;
            float threshold = layout.CellSize * GameConfig.DRAG_THRESHOLD;

            if (dx * dx + dy * dy <= threshold * threshold) return;

            var target = MathF.Abs(dx) > MathF.Abs(dy)
                ? new Pos(origin.Row, origin.Col + (dx > 0 ? 1 : -1))
                : new Pos(origin.Row + (dy > 0 ? 1 : -1), origin.Col);

            if (!controller.Board.InBounds(target)) return;

            state.DragUsed = true;
            state.ClearSelection();
            controller.TrySwap(origin, target);
        }

        private void HandleClick(float x, float y)
        {
            if (state.IsAnimating || state.IsTimeUp) return;

            var cell = layout.ScreenToCell(x, y);
            if (cell is not { } pos) { state.ClearSelection(); return; }
            if (!state.HasSelection) { state.SelectedCell = pos; return; }

            var selected = state.SelectedCell!.Value;
            if (pos == selected) { state.ClearSelection(); return; }
            if (!pos.IsAdjacentTo(selected)) { state.ClearSelection(); return; }

            controller.TrySwap(selected, pos);
            state.ClearSelection();
        }

        private void UpdateTimer(float dt)
        {
            if (state.TimeRemaining <= 0f)
            {
                if (!state.IsAnimating) sceneManager.TransitionTo(SceneId.GameOver);
                return;
            }
            state.TimeRemaining -= dt;
            if (state.TimeRemaining <= 0f)
            {
                state.TimeRemaining = 0f;
                if (!state.IsAnimating) sceneManager.TransitionTo(SceneId.GameOver);
            }
        }

        private void UpdateAnimation(float dt)
        {
            if (!state.IsAnimating) return;

            state.AnimTimer += dt;
            float rawT = Math.Clamp(state.AnimTimer / state.AnimDuration, 0f, 1f);

            Func<float, float> easingFunc = state.Phase is AnimationPhase.Gravity or AnimationPhase.Spawning
                ? Easing.BounceOut : Easing.QuadOut;

            animator.Update(state, easingFunc(rawT));

            if (state.AnimTimer >= state.AnimDuration)
                controller.OnPhaseComplete();
        }

        private void DrawBoard()
        {
            float dim = layout.CellSize * Board.SIZE;
            float bdr = GameConfig.BOARD_BORDER;
            batch.DrawRect(layout.BoardX - bdr, layout.BoardY - bdr, dim + bdr * 2, dim + bdr * 2,
                GameConfig.COLOR_BOARD_BORDER);

            for (int r = 0; r < Board.SIZE; r++)
                for (int c = 0; c < Board.SIZE; c++)
                {
                    float x = layout.BoardX + c * layout.CellSize;
                    float y = layout.BoardY + r * layout.CellSize;
                    var color = (r + c) % 2 == 0 ? GameConfig.COLOR_CELL_DARK : GameConfig.COLOR_CELL_LIGHT;
                    batch.DrawRect(x + 1, y + 1, layout.CellSize - 2, layout.CellSize - 2, color);
                }
        }

        private void DrawSelectionGlows()
        {
            if (!state.HasSelection || state.IsAnimating) return;

            var sel = state.SelectedCell!.Value;
            var vis = state.Visuals[sel.Row, sel.Col];
            if (vis is null) return;

            float pulse = 0.20f + 0.12f * MathF.Sin(state.Time * GameConfig.SELECTION_PULSE_SPEED);
            batch.DrawCircle(vis.X, vis.Y, layout.CellSize * GameConfig.SELECTION_OUTER_RADIUS,
                new Vector4(1f, 1f, 1f, pulse), 16);
        }

        private void DrawGemShapes()
        {
            float baseRadius = layout.CellSize * GameConfig.GEM_RADIUS_FACTOR;

            bool hasSelection = state.HasSelection && !state.IsAnimating;
            int selRow = -1, selCol = -1;
            if (hasSelection)
            {
                var sel = state.SelectedCell!.Value;
                selRow = sel.Row;
                selCol = sel.Col;
            }

            for (int r = 0; r < Board.SIZE; r++)
                for (int c = 0; c < Board.SIZE; c++)
                {
                    if (state.Visuals[r, c] is not { } vis) continue;
                    if (!vis.IsVisible) continue;

                    float radius = baseRadius * vis.Scale;
                    if (r == selRow && c == selCol)
                        radius *= 1f + GameConfig.SELECTION_SCALE_AMOUNT * MathF.Sin(state.Time * GameConfig.SELECTION_SCALE_SPEED);

                    gemRenderer.DrawGem(vis, radius);
                }
        }

        private void DrawBonusIndicators()
        {
            float baseRadius = layout.CellSize * GameConfig.GEM_RADIUS_FACTOR;

            for (int r = 0; r < Board.SIZE; r++)
                for (int c = 0; c < Board.SIZE; c++)
                {
                    if (state.Visuals[r, c] is not { } vis) continue;
                    if (vis.Bonus == BonusType.None || vis.Alpha <= 0.5f) continue;

                    gemRenderer.DrawBonusIndicator(vis.X, vis.Y, baseRadius * vis.Scale,
                        vis.Bonus, vis.Alpha, state.Time);
                }
        }

        private void DrawHUD()
        {
            float hudH = layout.BoardY - 10f;
            float pad = 16f;

            float sPW = layout.ScreenW * 0.38f;
            batch.DrawRect(pad, 8, sPW, hudH - 16, GameConfig.COLOR_PANEL);
            text.Draw(batch, "SCORE", pad + 10, 10f, 0.32f, GameConfig.COLOR_LABEL);
            text.Draw(batch, state.ScoreText, pad + 10, 28f, 0.65f, GameConfig.COLOR_ACCENT);

            float tPW = layout.ScreenW * 0.35f;
            float tX = layout.ScreenW - tPW - pad;
            batch.DrawRect(tX, 8, tPW, hudH - 16, GameConfig.COLOR_PANEL);

            bool lowTime = state.TimeRemaining < 10f;
            var timerColor = lowTime
                ? new Vector4(GameConfig.COLOR_DANGER.X, GameConfig.COLOR_DANGER.Y, GameConfig.COLOR_DANGER.Z,
                    0.7f + 0.3f * MathF.Abs(MathF.Sin(state.Time * 4f)))
                : GameConfig.COLOR_ACCENT;

            text.Draw(batch, "TIME", tX + 10, 10f, 0.32f, GameConfig.COLOR_LABEL);
            text.Draw(batch, state.TimerText, tX + 10, 28f, 0.65f, timerColor);

            float barX = tX + 6, barW = tPW - 12;
            batch.DrawRect(barX, hudH - 20, barW, 6, GameConfig.COLOR_BAR_BG);
            batch.DrawRect(barX, hudH - 20,
                barW * Math.Clamp(state.TimeRemaining / GameConfig.GAME_DURATION, 0f, 1f), 6, timerColor);
        }
    }
}