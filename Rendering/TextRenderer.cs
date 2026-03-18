using System.Numerics;

namespace Match3.Rendering
{
    public sealed class TextRenderer
    {
        private readonly FontAtlas _atlas;

        public FontAtlas Atlas => _atlas;
        public float FontSize => _atlas.FontSize;

        public TextRenderer(FontAtlas atlas)
        {
            _atlas = atlas;
        }

        public void Draw(SpriteBatch batch, string text, float x, float y,
            float scale, Vector4 color)
        {
            float cursor = x;
            float scaledSize = _atlas.FontSize * scale;

            foreach (char ch in text)
            {
                if (!_atlas.TryGetGlyph(ch, out var g))
                    continue;

                if (ch != ' ')
                {
                    float gx = cursor + g.OffsetX * scale;
                    float gy = y + (scaledSize + g.OffsetY * scale);
                    float gw = g.Width * scale;
                    float gh = g.Height * scale;

                    batch.DrawTexRect(_atlas.TextureId,
                        gx, gy, gw, gh,
                        g.U0, g.V0, g.U1, g.V1,
                        color);
                }

                cursor += g.Advance * scale;
            }
        }

        public void DrawCentered(SpriteBatch batch, string text, float cx, float y,
            float scale, Vector4 color)
        {
            float w = _atlas.MeasureWidth(text, scale);
            Draw(batch, text, cx - w * 0.5f, y, scale, color);
        }
    }
}