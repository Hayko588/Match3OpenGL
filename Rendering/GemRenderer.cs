using Match3.Animation;
using Match3.Domain;
using System.Numerics;

namespace Match3.Rendering
{
    public class GemRenderer(GemBatch gemBatch, SpriteBatch spriteBatch)
    {
        public void DrawGem(GemVisual visual, float radius)
        {
            if (!visual.IsVisible) return;
            gemBatch.Draw(visual.X, visual.Y, radius, visual.ShapeId,
                new Vector4(visual.Color.X, visual.Color.Y, visual.Color.Z, visual.Alpha));
        }

        public void DrawBonusIndicator(float cx, float cy, float gemRadius,
            BonusType bonus, float alpha, float time)
        {
            var white = new Vector4(1f, 1f, 1f, alpha * 0.85f);
            float r = gemRadius;

            switch (bonus)
            {
                case BonusType.LineH:
                    spriteBatch.DrawRect(cx - r * 0.8f, cy - r * 0.11f, r * 1.6f, r * 0.22f, white);
                    spriteBatch.DrawRect(cx - r * 0.8f - 3f, cy - r * 0.22f, 3f, r * 0.44f, white);
                    spriteBatch.DrawRect(cx + r * 0.8f, cy - r * 0.22f, 3f, r * 0.44f, white);
                    break;

                case BonusType.LineV:
                    spriteBatch.DrawRect(cx - r * 0.11f, cy - r * 0.8f, r * 0.22f, r * 1.6f, white);
                    spriteBatch.DrawRect(cx - r * 0.22f, cy - r * 0.8f - 3f, r * 0.44f, 3f, white);
                    spriteBatch.DrawRect(cx - r * 0.22f, cy + r * 0.8f, r * 0.44f, 3f, white);
                    break;

                case BonusType.Bomb:
                    float pulse = 0.7f + 0.3f * MathF.Sin(time * 8f);
                    float sz = r * 0.55f * pulse;
                    var bc = new Vector4(1f, 1f, 1f, alpha * 0.85f * pulse);
                    spriteBatch.DrawRect(cx - sz, cy - 1.5f, sz * 2f, 3f, bc);
                    spriteBatch.DrawRect(cx - 1.5f, cy - sz, 3f, sz * 2f, bc);
                    float d = r * 0.3f;
                    spriteBatch.DrawRect(cx - d - 2f, cy - d - 2f, 4f, 4f, bc);
                    spriteBatch.DrawRect(cx + d - 2f, cy - d - 2f, 4f, 4f, bc);
                    spriteBatch.DrawRect(cx - d - 2f, cy + d - 2f, 4f, 4f, bc);
                    spriteBatch.DrawRect(cx + d - 2f, cy + d - 2f, 4f, 4f, bc);
                    break;
            }
        }
    }
}