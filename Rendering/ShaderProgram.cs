using Silk.NET.OpenGL;

namespace Match3.Rendering
{
    public sealed class ShaderProgram : IDisposable
    {
        private readonly GL gl;
        public uint Handle { get; }

        private readonly Dictionary<string, int> uniformCache = [];

        public ShaderProgram(GL gl, string vertexSource, string fragmentSource)
        {
            this.gl = gl;

            uint vert = Compile(ShaderType.VertexShader, vertexSource);
            uint frag = Compile(ShaderType.FragmentShader, fragmentSource);

            Handle = gl.CreateProgram();
            gl.AttachShader(Handle, vert);
            gl.AttachShader(Handle, frag);
            gl.LinkProgram(Handle);

            gl.GetProgram(Handle, ProgramPropertyARB.LinkStatus, out int status);
            if (status == 0)
                throw new InvalidOperationException($"Shader link error: {gl.GetProgramInfoLog(Handle)}");

            gl.DetachShader(Handle, vert);
            gl.DetachShader(Handle, frag);
            gl.DeleteShader(vert);
            gl.DeleteShader(frag);
        }

        public void Use() => gl.UseProgram(Handle);

        public int GetUniform(string name)
        {
            if (!uniformCache.TryGetValue(name, out int loc))
            {
                loc = gl.GetUniformLocation(Handle, name);
                uniformCache[name] = loc;
            }
            return loc;
        }

        public void SetInt(string name, int value) => gl.Uniform1(GetUniform(name), value);

        public unsafe void SetMatrix4(string name, System.Numerics.Matrix4x4 mat)
        {
            float* p = (float*)&mat;
            gl.UniformMatrix4(GetUniform(name), 1, false, p);
        }

        private uint Compile(ShaderType type, string source)
        {
            uint s = gl.CreateShader(type);
            gl.ShaderSource(s, source);
            gl.CompileShader(s);
            gl.GetShader(s, ShaderParameterName.CompileStatus, out int status);
            if (status == 0)
                throw new InvalidOperationException($"Shader compile ({type}): {gl.GetShaderInfoLog(s)}");
            return s;
        }

        public void Dispose() => gl.DeleteProgram(Handle);
    }
}