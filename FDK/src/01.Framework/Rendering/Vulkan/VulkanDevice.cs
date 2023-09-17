using Silk.NET.Windowing;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using SkiaSharp;

namespace SampleFramework
{
    class VulkanDevice : IGraphicsDevice
    {
        private Vk VK;

        public VulkanDevice(IWindow window)
        {
            VK = Vk.GetApi();
        }

        public void SetClearColor(float r, float g, float b, float a)
        {
        }

        public void SetViewPort(int x, int y, uint width, uint height)
        {
        }

        public void SetFrameBuffer(uint width, uint height)
        {
        }

        public void ClearBuffer()
        {
        }

        public void SwapBuffer()
        {
        }


        public IPolygon GenPolygon(float[] vertices, uint[] indices, float[] uvs)
        {
            return new VulkanPolygon(vertices, indices, uvs);
        }

        public IShader GenShader(string name)
        {
            return new VulkanShader();
        }

        public unsafe ITexture GenTexture(void* data, int width, int height, RgbaType rgbaType)
        {
            return new VulkanTexture(data, width, height, rgbaType);
        }
        public void DrawPolygon(IPolygon polygon, IShader shader, ITexture texture, BlendType blendType)
        {
        }

        public unsafe SKBitmap GetScreenPixels()
        {  
            return null;
        }

        public void Dispose()
        {
        }
    }
}