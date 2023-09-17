using System;
using Silk.NET.Windowing;
using Silk.NET.Maths;
using SkiaSharp;

namespace SampleFramework
{
    public enum BlendType
    {
        Normal,
        Add,
        Multi,
        Sub,
        Screen
    }
    public interface IGraphicsDevice : IDisposable
    {
        void SetClearColor(float r, float g, float b, float a);

        void SetViewPort(int x, int y, uint width, uint height);

        void SetFrameBuffer(uint width, uint height);

        void ClearBuffer();

        void SwapBuffer();

        IPolygon GenPolygon(float[] vertices, uint[] indices, float[] uvs);

        IShader GenShader(string name);

        unsafe ITexture GenTexture(void* data, int width, int height, RgbaType rgbaType);

        void DrawPolygon(IPolygon polygon, IShader shader, ITexture texture, BlendType blendType);

        unsafe SKBitmap GetScreenPixels();
    }
}