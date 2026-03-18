using Silk.NET.OpenGL;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Match3.Rendering
{
    public sealed class GemBatch : IDisposable
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct GemVertex
        {
            public Vector2 Position;
            public Vector2 LocalUV;
            public Vector4 Color;
            public float ShapeType;
            public float Softness;

            public const uint SIZE = 40;
        }

        private readonly GL gl;
        private readonly ShaderProgram shader;

        private const int MAX_GEMS = 256;
        private const int MAX_VERTS = MAX_GEMS * 4;
        private const int MAX_INDICES = MAX_GEMS * 6;

        private readonly GemVertex[] verts = new GemVertex[MAX_VERTS];
        private int vertCount;
        private int gemCount;
        private readonly uint vao, vbo, ebo;

        public const float SHAPE_CIRCLE = 0f;
        public const float SHAPE_SQUARE = 1f;
        public const float SHAPE_DIAMOND = 2f;
        public const float SHAPE_HEXAGON = 3f;
        public const float SHAPE_STAR = 4f;

        private const string VERT_SRC = @"#version 330 core
layout(location = 0) in vec2 aPos;
layout(location = 1) in vec2 aLocalUV;
layout(location = 2) in vec4 aColor;
layout(location = 3) in float aShapeType;
layout(location = 4) in float aSoftness;

uniform mat4 uProjection;

out vec2 vUV;
out vec4 vColor;
flat out float vShape;
flat out float vSoftness;

void main() {
    gl_Position = uProjection * vec4(aPos, 0.0, 1.0);
    vUV = aLocalUV;
    vColor = aColor;
    vShape = aShapeType;
    vSoftness = aSoftness;
}";

        private const string FRAG_SRC = @"#version 330 core
in vec2 vUV;
in vec4 vColor;
flat in float vShape;
flat in float vSoftness;

out vec4 FragColor;

// SDF functions — return negative inside, positive outside

float sdCircle(vec2 p) {
    return length(p) - 0.85;
}

float sdSquare(vec2 p) {
    vec2 d = abs(p) - vec2(0.72);
    return length(max(d, 0.0)) + min(max(d.x, d.y), 0.0) - 0.08;
}

float sdDiamond(vec2 p) {
    p = abs(p);
    float d = dot(p, normalize(vec2(1.0, 1.0))) - 0.60;
    return d;
}

float sdHexagon(vec2 p) {
    p = abs(p);
    float d = dot(p, normalize(vec2(1.0, 1.732))) - 0.78;
    d = max(d, p.x - 0.78);
    return d;
}

float sdStar(vec2 p) {
    // 5-pointed star
    float a = atan(p.y, p.x) + 1.5708; // rotate -90deg
    float r = length(p);
    float k = 2.5132741; // pi * 2 / 5 * 2
    float n = floor((a + 0.6283) / 1.2566); // pi*2/5
    a = a - n * 1.2566;
    float inner = 0.36;
    float outer = 0.82;
    float d = r - mix(inner, outer, cos(a * 2.5) * 0.5 + 0.5);
    return d;
}

void main() {
    float d;
    int shape = int(vShape + 0.5);

    if (shape == 0) d = sdCircle(vUV);
    else if (shape == 1) d = sdSquare(vUV);
    else if (shape == 2) d = sdDiamond(vUV);
    else if (shape == 3) d = sdHexagon(vUV);
    else d = sdStar(vUV);

    // Smooth edge using softness parameter
    float alpha = 1.0 - smoothstep(-vSoftness, vSoftness, d);
    if (alpha < 0.001) discard;

    FragColor = vec4(vColor.rgb, vColor.a * alpha);
}";

        public GemBatch(GL gl)
        {
            this.gl = gl;
            shader = new ShaderProgram(gl, VERT_SRC, FRAG_SRC);

            vao = gl.GenVertexArray();
            vbo = gl.GenBuffer();
            ebo = gl.GenBuffer();

            gl.BindVertexArray(vao);

            gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
            unsafe
            {
                gl.BufferData(BufferTargetARB.ArrayBuffer,
                    (nuint)(MAX_VERTS * GemVertex.SIZE), null, BufferUsageARB.DynamicDraw);
            }

            var indices = new uint[MAX_INDICES];
            for (int i = 0; i < MAX_GEMS; i++)
            {
                uint b = (uint)(i * 4);
                int j = i * 6;
                indices[j] = b; indices[j + 1] = b + 1; indices[j + 2] = b + 2;
                indices[j + 3] = b + 2; indices[j + 4] = b + 3; indices[j + 5] = b;
            }
            gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, ebo);
            unsafe
            {
                fixed (uint* ptr = indices)
                    gl.BufferData(BufferTargetARB.ElementArrayBuffer,
                        (nuint)(MAX_INDICES * sizeof(uint)), ptr, BufferUsageARB.StaticDraw);
            }

            unsafe
            {
                gl.EnableVertexAttribArray(0);
                gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, GemVertex.SIZE, (void*)0);
                gl.EnableVertexAttribArray(1);
                gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, GemVertex.SIZE, (void*)8);
                gl.EnableVertexAttribArray(2);
                gl.VertexAttribPointer(2, 4, VertexAttribPointerType.Float, false, GemVertex.SIZE, (void*)16);
                gl.EnableVertexAttribArray(3);
                gl.VertexAttribPointer(3, 1, VertexAttribPointerType.Float, false, GemVertex.SIZE, (void*)32);
                gl.EnableVertexAttribArray(4);
                gl.VertexAttribPointer(4, 1, VertexAttribPointerType.Float, false, GemVertex.SIZE, (void*)36);
            }

            gl.BindVertexArray(0);
        }

        public void Begin(Matrix4x4 projection)
        {
            vertCount = 0;
            gemCount = 0;
            shader.Use();
            shader.SetMatrix4("uProjection", projection);
        }

        public void Draw(float cx, float cy, float radius, float shapeType,
            Vector4 color, float softness = 0.06f)
        {
            if (gemCount >= MAX_GEMS) Flush();

            int i = vertCount;

            verts[i] = new GemVertex
            {
                Position = new Vector2(cx - radius, cy - radius),
                LocalUV = new Vector2(-1f, -1f),
                Color = color,
                ShapeType = shapeType,
                Softness = softness
            };
            verts[i + 1] = new GemVertex
            {
                Position = new Vector2(cx + radius, cy - radius),
                LocalUV = new Vector2(1f, -1f),
                Color = color,
                ShapeType = shapeType,
                Softness = softness
            };
            verts[i + 2] = new GemVertex
            {
                Position = new Vector2(cx + radius, cy + radius),
                LocalUV = new Vector2(1f, 1f),
                Color = color,
                ShapeType = shapeType,
                Softness = softness
            };
            verts[i + 3] = new GemVertex
            {
                Position = new Vector2(cx - radius, cy + radius),
                LocalUV = new Vector2(-1f, 1f),
                Color = color,
                ShapeType = shapeType,
                Softness = softness
            };

            vertCount += 4;
            gemCount++;
        }

        public void End() => Flush();

        private unsafe void Flush()
        {
            if (gemCount == 0) return;

            gl.BindVertexArray(vao);
            gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);

            fixed (GemVertex* ptr = verts)
                gl.BufferSubData(BufferTargetARB.ArrayBuffer, 0,
                    (nuint)(vertCount * GemVertex.SIZE), ptr);

            gl.DrawElements(PrimitiveType.Triangles,
                (uint)(gemCount * 6), DrawElementsType.UnsignedInt, null);

            vertCount = 0;
            gemCount = 0;
        }

        public void Dispose()
        {
            gl.DeleteVertexArray(vao);
            gl.DeleteBuffer(vbo);
            gl.DeleteBuffer(ebo);
            shader.Dispose();
        }
    }
}