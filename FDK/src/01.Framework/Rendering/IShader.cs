using Silk.NET.Maths;

namespace SampleFramework
{
    public interface IShader : IDisposable
    {
        void SetMVP(Matrix4X4<float> mvp);
        void SetColor(Vector4D<float> color);
        void SetTextureRect(Vector4D<float> rect);
        void SetCamera(Matrix4X4<float> camera);
    }
}