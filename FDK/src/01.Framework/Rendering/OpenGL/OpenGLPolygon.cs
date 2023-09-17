using Silk.NET.Windowing;
using Silk.NET.OpenGL;
using SkiaSharp;

namespace SampleFramework
{
    public class OpenGLPolygon : IPolygon
    {
        public uint IndiceCount { get; set; }

        internal uint VAO;
        internal uint VBO;
        internal uint EBO;
        internal uint UVBO;

        public unsafe OpenGLPolygon(float[] vertices, uint[] indices, float[] uvs)
        {
            VAO = OpenGLDevice.Gl.GenVertexArray();
            OpenGLDevice.Gl.BindVertexArray(VAO);


            VBO = OpenGLDevice.Gl.GenBuffer();
            OpenGLDevice.Gl.BindBuffer(BufferTargetARB.ArrayBuffer, VBO);
            fixed(float* v = vertices) 
            {
                    OpenGLDevice.Gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(sizeof(float) * vertices.Length), v, BufferUsageARB.StaticDraw);
            }


            EBO = OpenGLDevice.Gl.GenBuffer();
            OpenGLDevice.Gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, EBO);
            
            fixed(uint* e = indices) 
            {
                IndiceCount = (uint)indices.Length;
                OpenGLDevice.Gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(sizeof(uint) * indices.Length), e, BufferUsageARB.StaticDraw);
            }

            OpenGLDevice.Gl.EnableVertexAttribArray(0);
            OpenGLDevice.Gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), null);


            UVBO = OpenGLDevice.Gl.GenBuffer();
            OpenGLDevice.Gl.BindBuffer(BufferTargetARB.ArrayBuffer, UVBO);
            
            fixed(float* tc = uvs) 
            {
                OpenGLDevice.Gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(sizeof(float) * uvs.Length), tc, BufferUsageARB.StaticDraw);
            }
                
            OpenGLDevice.Gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, EBO);

            OpenGLDevice.Gl.EnableVertexAttribArray(1);
            OpenGLDevice.Gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), null);
                
            
            OpenGLDevice.Gl.BindVertexArray(0);
            OpenGLDevice.Gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
            OpenGLDevice.Gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0);
        }

        public void Dispose()
        {
            OpenGLDevice.Gl.DeleteVertexArray(VAO);
            OpenGLDevice.Gl.DeleteBuffer(VBO);
            OpenGLDevice.Gl.DeleteBuffer(EBO);
            OpenGLDevice.Gl.DeleteBuffer(UVBO);
        }
    }
}