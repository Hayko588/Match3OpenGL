using Silk.NET.OpenGL;
using StbTrueTypeSharp;

namespace Match3.Rendering
{
    public struct GlyphInfo
    {
        public float U0, V0, U1, V1;
        public float Width, Height;
        public float OffsetX, OffsetY;
        public float Advance;
    }

    public sealed class FontAtlas : IDisposable
    {
        private readonly GL _gl;
        public uint TextureId { get; }
        public float FontSize { get; }
        public float LineHeight { get; }

        private readonly Dictionary<char, GlyphInfo> _glyphs = new();

        private const int AtlasW = 512;
        private const int AtlasH = 512;

        private const int FirstChar = 32;
        private const int CharCount = 95;

        public FontAtlas(GL gl, float fontSize = 32f)
        {
            _gl = gl;
            FontSize = fontSize;

            byte[] fontData = LoadFontData();

            var bakedChars = new StbTrueType.stbtt_bakedchar[CharCount];
            byte[] atlasPixels = new byte[AtlasW * AtlasH];

            unsafe
            {
                fixed (byte* fontPtr = fontData)
                fixed (byte* pixelPtr = atlasPixels)
                fixed (StbTrueType.stbtt_bakedchar* charPtr = bakedChars)
                {
                    StbTrueType.stbtt_BakeFontBitmap(
                        fontPtr, 0, fontSize,
                        pixelPtr, AtlasW, AtlasH,
                        FirstChar, CharCount, charPtr);
                }
            }

            for (int i = 0; i < CharCount; i++)
            {
                char ch = (char)(FirstChar + i);
                var bc = bakedChars[i];

                float glyphW = bc.x1 - bc.x0;
                float glyphH = bc.y1 - bc.y0;

                _glyphs[ch] = new GlyphInfo
                {
                    U0 = bc.x0 / (float)AtlasW,
                    V0 = bc.y0 / (float)AtlasH,
                    U1 = bc.x1 / (float)AtlasW,
                    V1 = bc.y1 / (float)AtlasH,
                    Width = glyphW,
                    Height = glyphH,
                    OffsetX = bc.xoff,
                    OffsetY = bc.yoff,
                    Advance = bc.xadvance,
                };
            }

            LineHeight = fontSize * 1.2f;

            TextureId = _gl.GenTexture();
            _gl.BindTexture(TextureTarget.Texture2D, TextureId);

            byte[] rgba = new byte[AtlasW * AtlasH * 4];
            for (int i = 0; i < AtlasW * AtlasH; i++)
            {
                rgba[i * 4 + 0] = 255;
                rgba[i * 4 + 1] = 255;
                rgba[i * 4 + 2] = 255;
                rgba[i * 4 + 3] = atlasPixels[i];
            }

            unsafe
            {
                fixed (byte* ptr = rgba)
                {
                    _gl.TexImage2D(TextureTarget.Texture2D, 0,
                        InternalFormat.Rgba, AtlasW, AtlasH, 0,
                        PixelFormat.Rgba, PixelType.UnsignedByte, ptr);
                }
            }

            _gl.TexParameter(TextureTarget.Texture2D,
                TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            _gl.TexParameter(TextureTarget.Texture2D,
                TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

            _gl.BindTexture(TextureTarget.Texture2D, 0);
        }

        public bool TryGetGlyph(char ch, out GlyphInfo info)
            => _glyphs.TryGetValue(ch, out info);

        public float MeasureWidth(string text, float scale = 1f)
        {
            float w = 0;
            foreach (char ch in text)
            {
                if (_glyphs.TryGetValue(ch, out var g))
                    w += g.Advance * scale;
            }
            return w;
        }

        private static byte[] LoadFontData()
        {
            string[] candidates =
            {
            // Windows
            @"C:\Windows\Fonts\segoeui.ttf",
            @"C:\Windows\Fonts\arial.ttf",
            @"C:\Windows\Fonts\calibri.ttf",
            @"C:\Windows\Fonts\consola.ttf",
            @"C:\Windows\Fonts\tahoma.ttf",
            // Linux
            "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",
            "/usr/share/fonts/truetype/liberation/LiberationSans-Regular.ttf",
            "/usr/share/fonts/TTF/DejaVuSans.ttf",
            // macOS
            "/System/Library/Fonts/Helvetica.ttc",
            "/System/Library/Fonts/SFNSText.ttf",
            "/Library/Fonts/Arial.ttf",
        };

            foreach (var path in candidates)
            {
                if (File.Exists(path))
                {
                    Console.WriteLine($"Font loaded: {path}");
                    return File.ReadAllBytes(path);
                }
            }

            throw new FileNotFoundException(
                "No system font found. Place a .ttf file next to the executable and name it 'font.ttf'.");
        }

        public void Dispose()
        {
            _gl.DeleteTexture(TextureId);
        }
    }
}