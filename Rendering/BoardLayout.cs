using Match3.Domain;
using System.Numerics;

namespace Match3.Rendering
{
    public sealed class BoardLayout
    {
        public float CellSize { get; private set; }
        public float BoardX { get; private set; }
        public float BoardY { get; private set; }
        public int ScreenW { get; private set; }
        public int ScreenH { get; private set; }
        public float HudHeight { get; private set; }

        public void Recalculate(int screenW, int screenH)
        {
            ScreenW = screenW;
            ScreenH = screenH;
            HudHeight = screenH * GameConfig.HUD_HEIGHT_FACTOR;

            float pad = GameConfig.BOARD_PADDING;
            float availW = screenW - pad * 2;
            float availH = screenH - HudHeight - pad;
            float dim = MathF.Min(availW, availH);

            CellSize = dim / Board.SIZE;
            BoardX = (screenW - dim) * 0.5f;
            BoardY = HudHeight + (availH - dim) * 0.5f;
            Projection = Matrix4x4.CreateOrthographicOffCenter(0, screenW, screenH, 0, -1, 1);
        }

        public Matrix4x4 Projection { get; private set; }

        public Vector2 CellCenter(Pos pos) => new(
            BoardX + pos.Col * CellSize + CellSize * 0.5f,
            BoardY + pos.Row * CellSize + CellSize * 0.5f);

        public Pos? ScreenToCell(float x, float y)
        {
            int col = (int)((x - BoardX) / CellSize);
            int row = (int)((y - BoardY) / CellSize);
            return row >= 0 && row < Board.SIZE && col >= 0 && col < Board.SIZE
                ? new Pos(row, col)
                : null;
        }

        public bool IsInsideButton(float mx, float my, float bx, float by, float bw, float bh) =>
            mx >= bx && mx <= bx + bw && my >= by && my <= by + bh;
    }
}