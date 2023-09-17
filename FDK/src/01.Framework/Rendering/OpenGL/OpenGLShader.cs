using System;
using Silk.NET.Windowing;
using Silk.NET.OpenGL;
using Silk.NET.Maths;
using SkiaSharp;
using System.Numerics;

namespace SampleFramework
{
    public class OpenGLShader : IShader
    {
        internal uint ShaderProgram;

        private int MVPID;

        private int ColorID;

        private int TextureRectID;

        private int CameraID;

        public OpenGLShader(string vertexCode, string fragmentCode)
        {
            uint vertexShader = OpenGLDevice.Gl.CreateShader(ShaderType.VertexShader);
            OpenGLDevice.Gl.ShaderSource(vertexShader, vertexCode);
            OpenGLDevice.Gl.CompileShader(vertexShader);
            OpenGLDevice.Gl.GetShader(vertexShader, ShaderParameterName.CompileStatus, out int vertexStatus);
            if (vertexStatus != (int)GLEnum.True)
            {
                throw new Exception("Vertex shader failed to compile: " + OpenGLDevice.Gl.GetShaderInfoLog(vertexShader));
            }
            
            uint fragmentShader = OpenGLDevice.Gl.CreateShader(ShaderType.FragmentShader);
            OpenGLDevice.Gl.ShaderSource(fragmentShader, fragmentCode);
            OpenGLDevice.Gl.CompileShader(fragmentShader);
            OpenGLDevice.Gl.GetShader(fragmentShader, ShaderParameterName.CompileStatus, out int fragmentStatus);
            if (fragmentStatus != (int)GLEnum.True)
            {
                throw new Exception("Fragment shader failed to compile: " + OpenGLDevice.Gl.GetShaderInfoLog(fragmentShader));
            }

            ShaderProgram = OpenGLDevice.Gl.CreateProgram();
            OpenGLDevice.Gl.AttachShader(ShaderProgram, vertexShader);
            OpenGLDevice.Gl.AttachShader(ShaderProgram, fragmentShader);

            OpenGLDevice.Gl.LinkProgram(ShaderProgram);
            OpenGLDevice.Gl.GetProgram(ShaderProgram, ProgramPropertyARB.LinkStatus, out int lStatus);
            if (lStatus != (int)GLEnum.True)
            {
                throw new Exception("Fragment shader failed to compile: " + OpenGLDevice.Gl.GetProgramInfoLog(ShaderProgram));
            }
            
            OpenGLDevice.Gl.DetachShader(ShaderProgram, vertexShader);
            OpenGLDevice.Gl.DetachShader(ShaderProgram, fragmentShader);
            OpenGLDevice.Gl.DeleteShader(vertexShader);
            OpenGLDevice.Gl.DeleteShader(fragmentShader);
            

            MVPID = OpenGLDevice.Gl.GetUniformLocation(ShaderProgram, "mvp");
            ColorID = OpenGLDevice.Gl.GetUniformLocation(ShaderProgram, "color");
            TextureRectID = OpenGLDevice.Gl.GetUniformLocation(ShaderProgram, "textureRect");
            CameraID = OpenGLDevice.Gl.GetUniformLocation(ShaderProgram, "camera");
        }

        public unsafe void SetMVP(Matrix4X4<float> mvp)
        {
            OpenGLDevice.Gl.UniformMatrix4(MVPID, 1, false, (float*)&mvp);
        }

        public unsafe void SetColor(Vector4D<float> color)
        {
            System.Numerics.Vector4 vector4 = new(color.X, color.Y, color.Z, color.W);
            OpenGLDevice.Gl.Uniform4(ColorID, ref vector4);
        }

        public unsafe void SetTextureRect(Vector4D<float> rect)
        {
            System.Numerics.Vector4 vector4 = new(rect.X, rect.Y, rect.Z, rect.W);
            OpenGLDevice.Gl.Uniform4(TextureRectID, ref vector4);
        }

        public unsafe void SetCamera(Matrix4X4<float> camera)
        {
            OpenGLDevice.Gl.UniformMatrix4(CameraID, 1, false, (float*)&camera);
        }

        public void Dispose()
        {
            OpenGLDevice.Gl.DeleteProgram(ShaderProgram);
        }
    }
}