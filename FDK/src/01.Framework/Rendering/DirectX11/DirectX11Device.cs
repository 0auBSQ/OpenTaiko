using Silk.NET.Windowing;
using Silk.NET.Maths;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D.Compilers;
using Silk.NET.Direct3D11;
using Silk.NET.DXGI;
using SkiaSharp;

namespace SampleFramework
{
    unsafe class DirectX11Device : IGraphicsDevice
    {
        const bool FORCE_DXVK = false;

        internal static D3D11 D3d11;

        internal static DXGI DxGi;

        internal static D3DCompiler D3dCompiler;

        internal static ComPtr<IDXGIFactory> Factory;

        internal static ComPtr<ID3D11Device> Device;

        internal static ComPtr<ID3D11DeviceContext> ImmediateContext;

        internal static ComPtr<IDXGISwapChain> SwapChain;

        internal static ComPtr<ID3D11RenderTargetView> RenderTargetView;

        internal static ComPtr<ID3D11Texture2D> DepthStencilTexture;

        internal static ComPtr<ID3D11DepthStencilView> DepthStencilView;

        internal static ComPtr<ID3D11BlendState>[] BlendStates = new ComPtr<ID3D11BlendState>[5];
        
        internal ComPtr<ID3D11SamplerState> SamplerState;

        internal static float[] CurrnetClearColor;

        internal static IWindow Window_;

        private Viewport Viewport_;







        private void CreateRenderTargetView()
        {
            SilkMarshal.ThrowHResult
            (
                SwapChain.GetBuffer(0, out ComPtr<ID3D11Texture2D> backBuffer)
            );
            SilkMarshal.ThrowHResult
            (
                Device.CreateRenderTargetView(backBuffer, null, ref RenderTargetView)
            );
            backBuffer.Dispose();
        }

        private void CreateDepthStencilView(uint width, uint height)
        {
            Texture2DDesc texture2DDesc = new Texture2DDesc();
            texture2DDesc.Width = width;
            texture2DDesc.Height = height;
            texture2DDesc.MipLevels = 1;
            texture2DDesc.ArraySize = 1;
            texture2DDesc.Format = Format.FormatD24UnormS8Uint;
            texture2DDesc.SampleDesc.Count = 1;
            texture2DDesc.SampleDesc.Quality = 0;
            texture2DDesc.Usage = Usage.Default;
            texture2DDesc.BindFlags = (uint)BindFlag.DepthStencil;
            texture2DDesc.CPUAccessFlags = 0;
            texture2DDesc.MiscFlags = 0;

            SilkMarshal.ThrowHResult
            (
                Device.CreateTexture2D(texture2DDesc, null, ref DepthStencilTexture)
            );



            DepthStencilViewDesc depthStencilDesc = new DepthStencilViewDesc();
            depthStencilDesc.Format = texture2DDesc.Format;
            depthStencilDesc.ViewDimension = DsvDimension.Texture2D;
            depthStencilDesc.Texture2D = new Tex2DDsv(0);
            SilkMarshal.ThrowHResult
            (
                Device.CreateDepthStencilView(DepthStencilTexture, depthStencilDesc, ref DepthStencilView)
            );
        }

        public DirectX11Device(IWindow window)
        {
            Window_ = window;
            D3d11 = D3D11.GetApi(window, FORCE_DXVK);
            DxGi = DXGI.GetApi(window, FORCE_DXVK);
            D3dCompiler = D3DCompiler.GetApi();


            SilkMarshal.ThrowHResult
            (
                DxGi.CreateDXGIFactory(out Factory)
            );


            D3DFeatureLevel[] featureLevels = new D3DFeatureLevel[]
            {
            D3DFeatureLevel.Level111,
            D3DFeatureLevel.Level110,
            };

            uint debugFlag = 0;

#if DEBUG
        debugFlag = (uint)CreateDeviceFlag.Debug;
#endif

            fixed (D3DFeatureLevel* levels = featureLevels)
            {
                SilkMarshal.ThrowHResult
                (
                    D3d11.CreateDevice(
                        default(ComPtr<IDXGIAdapter>),
                        D3DDriverType.Hardware,
                        0,
                        debugFlag,
                        levels,
                        (uint)featureLevels.Length,
                        D3D11.SdkVersion,
                        ref Device,
                        default,
                        ref ImmediateContext)
                );
            }



            SwapChainDesc swapChainDesc = new SwapChainDesc();
            swapChainDesc.BufferDesc.Width = (uint)window.FramebufferSize.X;
            swapChainDesc.BufferDesc.Height = (uint)window.FramebufferSize.Y;
            swapChainDesc.BufferDesc.Format = Format.FormatR8G8B8A8Unorm;
            swapChainDesc.BufferDesc.ScanlineOrdering = ModeScanlineOrder.Unspecified;
            swapChainDesc.BufferDesc.Scaling = ModeScaling.Unspecified;
            swapChainDesc.BufferDesc.RefreshRate.Numerator = 0;
            swapChainDesc.BufferDesc.RefreshRate.Denominator = 1;
            swapChainDesc.SampleDesc.Count = 1;
            swapChainDesc.SampleDesc.Quality = 0;
            swapChainDesc.BufferUsage = DXGI.UsageRenderTargetOutput;
            swapChainDesc.BufferCount = 2;
            swapChainDesc.OutputWindow = window.Native.DXHandle.Value;
            swapChainDesc.Windowed = true;
            swapChainDesc.SwapEffect = SwapEffect.FlipDiscard;

            SilkMarshal.ThrowHResult
            (
                Factory.CreateSwapChain(
                    Device,
                    &swapChainDesc,
                    ref SwapChain
                )
            );

            CreateRenderTargetView();
            CreateDepthStencilView((uint)window.FramebufferSize.X, (uint)window.FramebufferSize.X);
            
            for(BlendType i = 0; i < BlendType.Screen + 1; i++)
            {
                BlendDesc blendDesc = new(false, false);
                blendDesc.RenderTarget[0].BlendEnable = true;
                switch (i)
                {
                    case BlendType.Normal:
                    blendDesc.RenderTarget[0].SrcBlend = Blend.SrcAlpha;
                    blendDesc.RenderTarget[0].DestBlend = Blend.InvSrcAlpha;
                    break;
                    case BlendType.Add:
                    blendDesc.RenderTarget[0].SrcBlend = Blend.One;
                    blendDesc.RenderTarget[0].DestBlend = Blend.One;
                    break;
                    case BlendType.Multi:
                    blendDesc.RenderTarget[0].SrcBlend = Blend.SrcAlpha;
                    blendDesc.RenderTarget[0].DestBlend = Blend.One;
                    break;
                    case BlendType.Sub:
                    blendDesc.RenderTarget[0].SrcBlend = Blend.Zero;
                    blendDesc.RenderTarget[0].DestBlend = Blend.InvSrcColor;
                    break;
                    case BlendType.Screen:
                    blendDesc.RenderTarget[0].SrcBlend = Blend.InvDestColor;
                    blendDesc.RenderTarget[0].DestBlend = Blend.One;
                    break;
                }

                blendDesc.RenderTarget[0].BlendOp = BlendOp.Add;
                blendDesc.RenderTarget[0].SrcBlendAlpha = Blend.One;
                blendDesc.RenderTarget[0].DestBlendAlpha = Blend.Zero;
                blendDesc.RenderTarget[0].BlendOpAlpha = BlendOp.Add;
                blendDesc.RenderTarget[0].RenderTargetWriteMask = (byte)ColorWriteEnable.All;

                Device.CreateBlendState(blendDesc, ref BlendStates[(int)i]);
            }
            
            SamplerDesc samplerDesc = new()
            {
                Filter = Filter.MinMagMipLinear,
                AddressU = TextureAddressMode.Wrap,
                AddressV = TextureAddressMode.Wrap,
                AddressW = TextureAddressMode.Wrap
            };
            
            SilkMarshal.ThrowHResult(
                DirectX11Device.Device.CreateSamplerState(samplerDesc, ref SamplerState)
            );


            SetViewPort(0, 0, (uint)window.Size.X, (uint)window.Size.Y);
        }

        public void SetClearColor(float r, float g, float b, float a)
        {
            CurrnetClearColor = new float[] { r, g, b, a };
        }

        public void SetViewPort(int x, int y, uint width, uint height)
        {
            if (width <= 0 || height <= 0) return;
            
            Viewport_ = new Viewport(0, 0, width, height, 0.0f, 1.0f);
        }

        public void SetFrameBuffer(uint width, uint height)
        {
            if (width <= 0 || height <= 0) return;

            RenderTargetView.Dispose();

            DepthStencilTexture.Dispose();
            DepthStencilView.Dispose();

            SilkMarshal.ThrowHResult
            (
                SwapChain.ResizeBuffers(0, width, height, Format.FormatR8G8B8A8Unorm, 0)
            );

            CreateRenderTargetView();
            CreateDepthStencilView(width, height);
        }

        public void ClearBuffer()
        {
            ImmediateContext.OMSetRenderTargets(1, ref RenderTargetView, DepthStencilView);
            ImmediateContext.ClearRenderTargetView(RenderTargetView, CurrnetClearColor);
            
            ImmediateContext.RSSetViewports(1, in Viewport_);
        }

        public void SwapBuffer()
        {

            SilkMarshal.ThrowHResult
            (
                SwapChain.Present(Window_.VSync ? 1u : 0u, 0)
            );
        }


        public IPolygon GenPolygon(float[] vertices, uint[] indices, float[] uvs)
        {
            return new DirectX11Polygon(vertices, indices, uvs);
        }

        public IShader GenShader(string name)
        {
            using StreamReader stream = new StreamReader(@$"{name}.hlsl");
            return new DirectX11Shader(stream.ReadToEnd());
        }


        public unsafe ITexture GenTexture(void* data, int width, int height, RgbaType rgbaType)
        {
            return new DirectX11Texture(data, width, height, rgbaType);
        }

        public unsafe void DrawPolygon(IPolygon polygon, IShader shader, ITexture texture, BlendType blendType)
        {
            DirectX11Polygon dx11polygon = (DirectX11Polygon)polygon;
            DirectX11Shader dx11shader = (DirectX11Shader)shader;
            DirectX11Texture dx11texture = (DirectX11Texture)texture;

            if (dx11texture == null || dx11texture.IsWrongPixels) return;

            ImmediateContext.IASetInputLayout(dx11shader.InputLayout);
            ImmediateContext.IASetVertexBuffers(0, 1, dx11polygon.VertexBuffer, dx11polygon.VertexStride, 0);
            ImmediateContext.IASetIndexBuffer(dx11polygon.IndexBuffer, Format.FormatR32Uint, 0);
            ImmediateContext.IASetPrimitiveTopology(D3DPrimitiveTopology.D3DPrimitiveTopologyTrianglelist);

            dx11shader.Update();

            // Bind our shaders.
            ImmediateContext.VSSetShader(dx11shader.VertexShader, default(ComPtr<ID3D11ClassInstance>), 0);
            ImmediateContext.VSSetConstantBuffers(0, 1, dx11shader.ConstantBuffer);


            float[] blendFactors = new float[] { 1, 1, 1, 1 };
            ImmediateContext.OMSetBlendState(BlendStates[(int)blendType], blendFactors, 0xffffffff);


            ImmediateContext.PSSetShader(dx11shader.PixelShader, default(ComPtr<ID3D11ClassInstance>), 0);
            ImmediateContext.PSSetConstantBuffers(0, 1, dx11shader.ConstantBuffer);

            ImmediateContext.PSSetShaderResources(0, 1, dx11texture.TextureView);
            ImmediateContext.PSSetSamplers(0, 1, SamplerState);

            ImmediateContext.ClearDepthStencilView(DepthStencilView, (uint)ClearFlag.Depth | (uint)ClearFlag.Stencil, 1.0f, 0);

            // Draw the quad.
            ImmediateContext.DrawIndexed(polygon.IndiceCount, 0, 0);
        }

        public unsafe SKBitmap GetScreenPixels()
        {  
            ComPtr<ID3D11Texture2D> backBuffer = default;
            var iid = ID3D11Texture2D.Guid;
            void* ptr = backBuffer;
            SwapChain.GetBuffer(0, &iid, &ptr);


            return null;
        }

        public void Dispose()
        {
            SamplerState.Dispose();

            for(int i = 0; i < 5; i++)
            {
                BlendStates[i].Dispose();
            }

            DepthStencilTexture.Dispose();
            DepthStencilView.Dispose();
            RenderTargetView.Dispose();
            ImmediateContext.Dispose();
            Device.Dispose();
            Factory.Dispose();
        }
    }
}