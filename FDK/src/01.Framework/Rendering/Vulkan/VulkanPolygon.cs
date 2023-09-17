using Silk.NET.Windowing;
using Silk.NET.OpenGL;
using SkiaSharp;

namespace SampleFramework
{
    public class VulkanPolygon : IPolygon
    {
        public uint IndiceCount { get; set; }


        public unsafe VulkanPolygon(float[] vertices, uint[] indices, float[] uvs)
        {
        }

        public void Dispose()
        {
        }
    }
}