using Match3.Rendering;
using System.Numerics;

namespace Match3.Application.Scenes;

public sealed class MainMenuScene(
    SpriteBatch batch,
    TextRenderer text,
    BoardLayout layout,
    GameState state,
    SceneManager sceneManager) : IScene
{
    public SceneId Id => SceneId.MainMenu;

    private const float BUTTON_WIDTH = 200f;
    private const float BUTTON_HEIGHT = 56f;

    public void Enter() { }
    public void Update(float dt) { }

    public void Render()
    {
        var proj = layout.Projection;
        batch.Begin(proj);
        float cx = layout.ScreenW * 0.5f;
        float cy = layout.ScreenH * 0.5f;

        text.DrawCentered(batch, "MATCH-3", cx, cy - 130f, 1.5f, GameConfig.COLOR_WHITE);

        var (bx, by) = GetButtonTopLeft(cx, cy);
        bool hovered = layout.IsInsideButton(state.HoverX, state.HoverY, bx, by, BUTTON_WIDTH, BUTTON_HEIGHT);
        batch.DrawRect(bx, by, BUTTON_WIDTH, BUTTON_HEIGHT,
            hovered ? new Vector4(0.45f, 0.75f, 1f, 1f) : GameConfig.COLOR_ACCENT);
        text.DrawCentered(batch, "PLAY", cx, by + 6f, 0.75f, GameConfig.COLOR_WHITE);
        batch.End();
    }

    public void OnMouseDown(float x, float y)
    {
        float cx = layout.ScreenW * 0.5f;
        float cy = layout.ScreenH * 0.5f;
        var (bx, by) = GetButtonTopLeft(cx, cy);
        if (layout.IsInsideButton(x, y, bx, by, BUTTON_WIDTH, BUTTON_HEIGHT))
            sceneManager.TransitionTo(SceneId.Game);
    }

    public void OnMouseUp(float x, float y) { }
    public void OnMouseMove(float x, float y) { state.HoverX = x; state.HoverY = y; }

    private static (float x, float y) GetButtonTopLeft(float cx, float cy) =>
        (cx - BUTTON_WIDTH * 0.5f, cy + 12f);
}
