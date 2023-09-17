using Silk.NET.Windowing;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;
using SkiaSharp;
using Silk.NET.Core.Native;

namespace SampleFramework
{
    public class DirectX12Texture : ITexture
    {
        internal bool IsWrongPixels;
        internal ComPtr<ID3D12Resource> Texture;
        internal ComPtr<ID3D12Resource> ConstantBuffer;

        private Format RgbaTypeToFormat(RgbaType rgbaType)
        {
            switch(rgbaType)
            {
                case RgbaType.Rgba:
                return Format.FormatR8G8B8A8Unorm;
                case RgbaType.Bgra:
                return Format.FormatB8G8R8A8Unorm;
                default:
                return Format.FormatR8G8B8A8Unorm;
            }
        }

        public unsafe DirectX12Texture(void* data, int width, int height, RgbaType rgbaType)
        {

            if (data == null || width <= 0 || height <= 0)
            {
                IsWrongPixels = true;
            }
        }

        public void Dispose()
        {
            Texture.Dispose();
        }
    }
}