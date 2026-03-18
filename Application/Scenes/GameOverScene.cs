using Match3.Rendering;
using System.Numerics;

namespace Match3.Application.Scenes
{
    public class GameOverScene(
        SpriteBatch batch,
        TextRenderer text,
        BoardLayout layout,
        GameState state,
        SceneManager sceneManager) : IScene
    {
        public SceneId Id => SceneId.GameOver;

        private const float PANEL_WIDTH = 360f;
        private const float PANEL_HEIGHT = 280f;
        private const float BUTTON_WIDTH = 160f;
        private const float BUTTON_HEIGHT = 50f;
        private const float BORDER_WIDTH = 2f;

        public void Enter() { }
        public void Update(float dt) { }

        public void OnResize()
        {
            // Frozen gems need updated screen positions
            state.UpdateVisualPositions(layout);
        }

        public void Render()
        {
            var proj = layout.Projection;

            batch.Begin(proj);
            DrawOverlay();
            batch.End();
        }

        public void OnMouseDown(float x, float y)
        {
            var (bx, by) = GetButtonTopLeft();
            if (layout.IsInsideButton(x, y, bx, by, BUTTON_WIDTH, BUTTON_HEIGHT))
                sceneManager.TransitionTo(SceneId.MainMenu);
        }

        public void OnMouseUp(float x, float y) { }
        public void OnMouseMove(float x, float y) { state.HoverX = x; state.HoverY = y; }

        private void DrawOverlay()
        {
            float cx = layout.ScreenW * 0.5f;
            float cy = layout.ScreenH * 0.5f;

            // Dim
            batch.DrawRect(0, 0, layout.ScreenW, layout.ScreenH, new Vector4(0f, 0f, 0f, 0.65f));

            // Panel
            float px = cx - PANEL_WIDTH * 0.5f;
            float py = cy - PANEL_HEIGHT * 0.5f;
            batch.DrawRect(px, py, PANEL_WIDTH, PANEL_HEIGHT, new Vector4(0.12f, 0.12f, 0.16f, 0.97f));

            batch.DrawRect(px, py, PANEL_WIDTH, BORDER_WIDTH, GameConfig.COLOR_ACCENT);
            batch.DrawRect(px, py + PANEL_HEIGHT - BORDER_WIDTH, PANEL_WIDTH, BORDER_WIDTH, GameConfig.COLOR_ACCENT);
            batch.DrawRect(px, py, BORDER_WIDTH, PANEL_HEIGHT, GameConfig.COLOR_ACCENT);
            batch.DrawRect(px + PANEL_WIDTH - BORDER_WIDTH, py, BORDER_WIDTH, PANEL_HEIGHT, GameConfig.COLOR_ACCENT);

            text.DrawCentered(batch, "GAME OVER", cx, py + 25f, 0.9f, GameConfig.COLOR_DANGER);
            text.DrawCentered(batch, "SCORE", cx, py + 85f, 0.4f, GameConfig.COLOR_LABEL);
            text.DrawCentered(batch, state.ScoreText, cx, py + 110f, 1.1f, GameConfig.COLOR_ACCENT);

            // Button
            var (bx, by) = GetButtonTopLeft();
            bool hovered = layout.IsInsideButton(state.HoverX, state.HoverY, bx, by, BUTTON_WIDTH, BUTTON_HEIGHT);
            batch.DrawRect(bx, by, BUTTON_WIDTH, BUTTON_HEIGHT,
                hovered ? new Vector4(0.45f, 0.75f, 1f, 1f) : GameConfig.COLOR_ACCENT);
            text.DrawCentered(batch, "OK", cx, by + 6f, 0.7f, GameConfig.COLOR_WHITE);
        }

        private (float x, float y) GetButtonTopLeft()
        {
            float cx = layout.ScreenW * 0.5f;
            float cy = layout.ScreenH * 0.5f;
            return (cx - BUTTON_WIDTH * 0.5f, cy + 45f);
        }
    }
}