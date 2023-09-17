using Silk.NET.Windowing;
using Silk.NET.Maths;
using Silk.NET.Direct3D9;
using Silk.NET.Core.Native;
using SkiaSharp;

namespace SampleFramework
{
    unsafe class DirectX9Device : IGraphicsDevice
    {
        private D3D9 D3d9;

        private ComPtr<IDirect3D9> DX9;

        private ComPtr<IDirect3DDevice9> Device;

        public DirectX9Device(IWindow window)
        {
            D3d9 = D3D9.GetApi(window, false);

            DX9 = D3d9.Direct3DCreate9(D3D9.SdkVersion);

            PresentParameters presentParameters = new PresentParameters()
            {
                Windowed = true,
                SwapEffect = Swapeffect.Discard,
                HDeviceWindow = window.Native.DXHandle.Value
            };

            DX9.CreateDevice(D3D9.AdapterDefault, Devtype.Hal, window.Native.DXHandle.Value, D3D9.CreateHardwareVertexprocessing, ref presentParameters, ref Device);
        }

        public void SetClearColor(float r, float g, float b, float a)
        {
        }

        public void SetViewPort(int x, int y, uint width, uint height)
        {
            Viewport9 viewport = new Viewport9()
            {
                X = (uint)x,
                Y = (uint)y,
                Width = width,
                Height = height,
            };
            Device.SetViewport(&viewport);
        }

        public void SetFrameBuffer(uint width, uint height)
        {
        }

        public void ClearBuffer()
        {
            Rect rect = new Rect()
            {
                X1 = 0,
                Y1 = 0,
                X2 = 1920,
                Y2 = 1080
            };
            Device.Clear(0,
            rect,
            D3D9.ClearTarget,
            0,
            0.0f,
            0);
        }

        public void SwapBuffer()
        {
            RGNData rGNData = default;
            Device.Present(null, null, 0, rGNData);
        }


        public IPolygon GenPolygon(float[] vertices, uint[] indices, float[] uvs)
        {
            return null;
        }

        public IShader GenShader(string name)
        {
            return null;
        }

        public unsafe ITexture GenTexture(void* data, int width, int height, RgbaType rgbaType)
        {
            return null;
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
            Device.Dispose();
            DX9.Dispose();
            D3d9.Dispose();
        }
    }
}