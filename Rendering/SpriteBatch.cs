using Silk.NET.OpenGL;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Match3.Rendering
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Vertex
    {
        public Vector2 Position;
        public Vector2 TexCoord;
        public Vector4 Color;
        public const uint SIZE_IN_BYTES = 32;
    }

    public sealed class SpriteBatch : IDisposable
    {
        private readonly GL gl;
        private readonly ShaderProgram shader;

        private const int MAX_QUADS = 4096;
        private const int MAX_VERTS = MAX_QUADS * 4;
        private const int MAX_INDICES = MAX_QUADS * 6;

        private readonly Vertex[] verts = new Vertex[MAX_VERTS];
        private int vertCount;
        private int quadCount;
        private readonly uint vao, vbo, ebo;
        private uint currentTexture;

        public SpriteBatch(GL gl, ShaderProgram shader)
        {
            this.gl = gl;
            this.shader = shader;

            vao = gl.GenVertexArray();
            vbo = gl.GenBuffer();
            ebo = gl.GenBuffer();

            gl.BindVertexArray(vao);
            gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
            unsafe { gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(MAX_VERTS * Vertex.SIZE_IN_BYTES), null, BufferUsageARB.DynamicDraw); }

            var indices = new uint[MAX_INDICES];
            for (int i = 0; i < MAX_QUADS; i++)
            {
                uint b = (uint)(i * 4); int j = i * 6;
                indices[j] = b; indices[j + 1] = b + 1; indices[j + 2] = b + 2;
                indices[j + 3] = b + 2; indices[j + 4] = b + 3; indices[j + 5] = b;
            }
            gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, ebo);
            unsafe { fixed (uint* ptr = indices) gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(MAX_INDICES * sizeof(uint)), ptr, BufferUsageARB.StaticDraw); }

            unsafe
            {
                gl.EnableVertexAttribArray(0);
                gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, Vertex.SIZE_IN_BYTES, (void*)0);
                gl.EnableVertexAttribArray(1);
                gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, Vertex.SIZE_IN_BYTES, (void*)8);
                gl.EnableVertexAttribArray(2);
                gl.VertexAttribPointer(2, 4, VertexAttribPointerType.Float, false, Vertex.SIZE_IN_BYTES, (void*)16);
            }
            gl.BindVertexArray(0);
        }

        public void Begin(Matrix4x4 projection)
        {
            vertCount = 0; quadCount = 0; currentTexture = 0;
            shader.Use();
            shader.SetMatrix4("uProjection", projection);
            shader.SetInt("uUseTexture", 0);
        }

        public void End() => Flush();

        public void DrawRect(float x, float y, float w, float h, Vector4 color)
        {
            EnsureSolid();
            if (quadCount >= MAX_QUADS) Flush();
            int i = vertCount;
            var z = Vector2.Zero;
            verts[i] = new() { Position = new(x, y), TexCoord = z, Color = color };
            verts[i + 1] = new() { Position = new(x + w, y), TexCoord = z, Color = color };
            verts[i + 2] = new() { Position = new(x + w, y + h), TexCoord = z, Color = color };
            verts[i + 3] = new() { Position = new(x, y + h), TexCoord = z, Color = color };
            vertCount += 4; quadCount++;
        }

        public void DrawCircle(float cx, float cy, float radius, Vector4 color, int segments = 24)
        {
            EnsureSolid();
            for (int s = 0; s < segments; s += 2)
            {
                if (quadCount >= MAX_QUADS) Flush();
                float a0 = MathF.PI * 2f * s / segments;
                float a1 = MathF.PI * 2f * (s + 1) / segments;
                float a2 = MathF.PI * 2f * Math.Min(s + 2, segments) / segments;
                int i = vertCount;
                var z = Vector2.Zero;
                verts[i] = new() { Position = new(cx, cy), TexCoord = z, Color = color };
                verts[i + 1] = new() { Position = new(cx + MathF.Cos(a0) * radius, cy + MathF.Sin(a0) * radius), TexCoord = z, Color = color };
                verts[i + 2] = new() { Position = new(cx + MathF.Cos(a1) * radius, cy + MathF.Sin(a1) * radius), TexCoord = z, Color = color };
                verts[i + 3] = new() { Position = new(cx + MathF.Cos(a2) * radius, cy + MathF.Sin(a2) * radius), TexCoord = z, Color = color };
                vertCount += 4; quadCount++;
            }
        }

        public void DrawDiamond(float cx, float cy, float hw, float hh, Vector4 color)
        {
            EnsureSolid();
            if (quadCount >= MAX_QUADS) Flush();
            int i = vertCount;
            var z = Vector2.Zero;
            verts[i] = new() { Position = new(cx, cy - hh), TexCoord = z, Color = color };
            verts[i + 1] = new() { Position = new(cx + hw, cy), TexCoord = z, Color = color };
            verts[i + 2] = new() { Position = new(cx, cy + hh), TexCoord = z, Color = color };
            verts[i + 3] = new() { Position = new(cx - hw, cy), TexCoord = z, Color = color };
            vertCount += 4; quadCount++;
        }

        public void DrawRegularPoly(float cx, float cy, float radius, int sides, float rotation, Vector4 color)
        {
            EnsureSolid();
            float step = MathF.PI * 2f / sides;
            for (int s = 0; s < sides; s += 2)
            {
                if (quadCount >= MAX_QUADS) Flush();
                float a0 = rotation + s * step;
                float a1 = rotation + (s + 1) * step;
                float a2 = rotation + Math.Min(s + 2, sides) * step;
                int i = vertCount;
                var z = Vector2.Zero;
                verts[i] = new() { Position = new(cx, cy), TexCoord = z, Color = color };
                verts[i + 1] = new() { Position = new(cx + MathF.Cos(a0) * radius, cy + MathF.Sin(a0) * radius), TexCoord = z, Color = color };
                verts[i + 2] = new() { Position = new(cx + MathF.Cos(a1) * radius, cy + MathF.Sin(a1) * radius), TexCoord = z, Color = color };
                verts[i + 3] = new() { Position = new(cx + MathF.Cos(a2) * radius, cy + MathF.Sin(a2) * radius), TexCoord = z, Color = color };
                vertCount += 4; quadCount++;
            }
        }

        public void DrawStar(float cx, float cy, float outerR, float innerR, int points, Vector4 color)
        {
            EnsureSolid();
            int total = points * 2;
            for (int s = 0; s < total; s += 2)
            {
                if (quadCount >= MAX_QUADS) Flush();
                float a0 = MathF.PI * 2f * s / total - MathF.PI * 0.5f;
                float a1 = MathF.PI * 2f * (s + 1) / total - MathF.PI * 0.5f;
                float a2 = MathF.PI * 2f * (s + 2) / total - MathF.PI * 0.5f;
                float r0 = s % 2 == 0 ? outerR : innerR;
                float r1 = (s + 1) % 2 == 0 ? outerR : innerR;
                float r2 = (s + 2) % 2 == 0 ? outerR : innerR;
                int i = vertCount;
                var z = Vector2.Zero;
                verts[i] = new() { Position = new(cx, cy), TexCoord = z, Color = color };
                verts[i + 1] = new() { Position = new(cx + MathF.Cos(a0) * r0, cy + MathF.Sin(a0) * r0), TexCoord = z, Color = color };
                verts[i + 2] = new() { Position = new(cx + MathF.Cos(a1) * r1, cy + MathF.Sin(a1) * r1), TexCoord = z, Color = color };
                verts[i + 3] = new() { Position = new(cx + MathF.Cos(a2) * r2, cy + MathF.Sin(a2) * r2), TexCoord = z, Color = color };
                vertCount += 4; quadCount++;
            }
        }

        public void DrawTexRect(uint textureId, float x, float y, float w, float h,
            float u0, float v0, float u1, float v1, Vector4 color)
        {
            SetTexture(textureId);
            if (quadCount >= MAX_QUADS) Flush();
            int i = vertCount;
            verts[i] = new() { Position = new(x, y), TexCoord = new(u0, v0), Color = color };
            verts[i + 1] = new() { Position = new(x + w, y), TexCoord = new(u1, v0), Color = color };
            verts[i + 2] = new() { Position = new(x + w, y + h), TexCoord = new(u1, v1), Color = color };
            verts[i + 3] = new() { Position = new(x, y + h), TexCoord = new(u0, v1), Color = color };
            vertCount += 4; quadCount++;
        }

        private void EnsureSolid() => SetTexture(0);

        private void SetTexture(uint textureId)
        {
            if (textureId == currentTexture) return;
            Flush();
            currentTexture = textureId;
            if (textureId == 0)
            {
                shader.SetInt("uUseTexture", 0);
                gl.BindTexture(TextureTarget.Texture2D, 0);
            }
            else
            {
                shader.SetInt("uUseTexture", 1);
                gl.BindTexture(TextureTarget.Texture2D, textureId);
            }
        }

        private unsafe void Flush()
        {
            if (quadCount == 0) return;
            gl.BindVertexArray(vao);
            gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
            fixed (Vertex* ptr = verts)
                gl.BufferSubData(BufferTargetARB.ArrayBuffer, 0, (nuint)(vertCount * Vertex.SIZE_IN_BYTES), ptr);
            gl.DrawElements(PrimitiveType.Triangles, (uint)(quadCount * 6), DrawElementsType.UnsignedInt, null);
            vertCount = 0; quadCount = 0;
        }

        public void Dispose()
        {
            gl.DeleteVertexArray(vao);
            gl.DeleteBuffer(vbo);
            gl.DeleteBuffer(ebo);
        }
    }
}