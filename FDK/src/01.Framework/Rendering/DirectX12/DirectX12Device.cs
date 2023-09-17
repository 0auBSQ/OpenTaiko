using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Silk.NET.Windowing;
using Silk.NET.Maths;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D.Compilers;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;
using SkiaSharp;


namespace SampleFramework
{
    unsafe class DirectX12Device : IGraphicsDevice
    {
        private const uint FrameCount = 2;

        private D3D12 D3d12;

        private DXGI DxGi;

        internal D3DCompiler D3dCompiler;

        private ComPtr<ID3D12Debug> DebugController;

        private ComPtr<IDXGIFactory4> Factory;

        private ComPtr<IDXGIAdapter1> HardwareAdapters;

        internal ComPtr<ID3D12Device> Device;

        private ComPtr<ID3D12CommandQueue> CommandQueue;

        private ComPtr<IDXGISwapChain3> SwapChain;

        internal ComPtr<ID3D12DescriptorHeap> RtvHeap;

        private ComPtr<ID3D12DescriptorHeap> DSVHeap;

        internal ComPtr<ID3D12DescriptorHeap> CBVHeap;

        private ID3D12Resource*[] RenderTargets = new ID3D12Resource*[FrameCount];

        private ComPtr<ID3D12Resource> DepthStencil;

        private ComPtr<ID3D12CommandAllocator>[] CommandAllocator = new ComPtr<ID3D12CommandAllocator>[FrameCount];

        internal ComPtr<ID3D12RootSignature> RootSignature;

        internal ComPtr<ID3D12GraphicsCommandList>[] CommandList = new ComPtr<ID3D12GraphicsCommandList>[FrameCount];

        private ComPtr<ID3D12Fence> Fence;

        private uint[] FenceValue = new uint[FrameCount];

        private IntPtr FenceEvent;

        private uint RtvDescriptorSize;

        private float[] CurrnetClearColor;

        private IWindow Window_;

        internal uint FrameBufferIndex;

        private bool IsActivate;



        private Viewport viewport;
        private Box2D<int> rect;



        private bool SupportsRequiredDirect3DVersion(IDXGIAdapter1* adapter1)
        {
            var iid = ID3D12Device.Guid;
            return HResult.IndicatesSuccess(D3d12.CreateDevice((IUnknown*)adapter1, D3DFeatureLevel.Level110, &iid, null));
        }

        private ID3D12Device* GetDevice()
        {
            ComPtr<IDXGIAdapter1> adapter = default;
            ID3D12Device* device = default;

            for (uint i = 0; Factory.EnumAdapters(i, ref adapter) != 0x887A0002; i++)
            {
                AdapterDesc1 desc = default;
                adapter.GetDesc1(ref desc);

                if ((desc.Flags & (uint)AdapterFlag.Software) != 0)
                {
                    continue;
                }

                if (SupportsRequiredDirect3DVersion(adapter)) break;
            }

            var device_iid = ID3D12Device.Guid;
            IDXGIAdapter1* hardwareAdapters = adapter.Detach();
            HardwareAdapters = hardwareAdapters;
            SilkMarshal.ThrowHResult
            (
                D3d12.CreateDevice((IUnknown*)hardwareAdapters, D3DFeatureLevel.Level110, &device_iid, (void**)&device)
            );

            return device;
        }

        private void CreateCommandQueue()
        {
            CommandQueueDesc commandQueueDesc = new CommandQueueDesc();
            commandQueueDesc.Flags = CommandQueueFlags.None;
            commandQueueDesc.Type = CommandListType.Direct;
            void* commandQueue = null;
            var iid = ID3D12CommandQueue.Guid;
            SilkMarshal.ThrowHResult
            (
                Device.CreateCommandQueue(&commandQueueDesc, ref iid, &commandQueue)
            );
            CommandQueue = (ID3D12CommandQueue*)commandQueue;
        }

        private void CreateSwapChain()
        {
            SwapChainDesc1 swapChainDesc = new(){
                Width = (uint)Window_.FramebufferSize.X,
                Height = (uint)Window_.FramebufferSize.Y,
                Format = Format.FormatR8G8B8A8Unorm,
                SampleDesc = new SampleDesc(1, 0),
                BufferUsage = DXGI.UsageRenderTargetOutput,
                BufferCount = FrameCount,
                Scaling = Scaling.Stretch,
                SwapEffect = SwapEffect.FlipDiscard,
                //Flags = (uint)SwapChainFlag.AllowModeSwitch,
                AlphaMode = AlphaMode.Ignore
            };

            SwapChainFullscreenDesc swapChainFullscreenDesc = new()
            {
                RefreshRate = new Rational(0, 1),
                ScanlineOrdering = ModeScanlineOrder.Unspecified,
                Scaling = ModeScaling.Unspecified,
                Windowed = true
            };

            void* device = CommandQueue;
            IDXGISwapChain1* swapChain;

            /*
            SilkMarshal.ThrowHResult
            (
                Factory.CreateSwapChainForHwnd((IUnknown*)device, Window_.Native.DXHandle.Value, swapChainDesc, swapChainFullscreenDesc, (IDXGIOutput*)0, &swapChain)
            );
            */
            SilkMarshal.ThrowHResult
            (
                Window_.CreateDxgiSwapchain((IDXGIFactory2*)Factory.AsVtblPtr(), (IUnknown*)device, &swapChainDesc, &swapChainFullscreenDesc, (IDXGIOutput*)0, &swapChain)
            );

            SwapChain = (IDXGISwapChain3*)swapChain;

            FrameBufferIndex = SwapChain.GetCurrentBackBufferIndex();
        }

        private void CreateRTVHeap()
        {
            DescriptorHeapDesc rtvHeapDesc = new DescriptorHeapDesc()
            {
                NumDescriptors = FrameCount + 1,
                Type = DescriptorHeapType.Rtv,
                Flags = DescriptorHeapFlags.None
            };
            
            void* rtvHeap = null;
            var iid = ID3D12DescriptorHeap.Guid;
            SilkMarshal.ThrowHResult
            (
                Device.CreateDescriptorHeap(&rtvHeapDesc, ref iid, &rtvHeap)
            );
            RtvHeap = (ID3D12DescriptorHeap*)rtvHeap;

            RtvDescriptorSize = Device.GetDescriptorHandleIncrementSize(DescriptorHeapType.Rtv);
        }

        protected virtual void CreateDSVHeap()
        {
            var dsvHeapDesc = new DescriptorHeapDesc
            {
                NumDescriptors = 1,
                Type = DescriptorHeapType.Dsv,
            };

            ID3D12DescriptorHeap* dsvHeap;

            var iid = ID3D12DescriptorHeap.Guid;
            SilkMarshal.ThrowHResult(Device.CreateDescriptorHeap(&dsvHeapDesc, &iid, (void**) &dsvHeap));

            DSVHeap = dsvHeap;
        }

        private void CreateCBVHeap()
        {
            DescriptorHeapDesc cbvHeapDesc = new DescriptorHeapDesc()
            {
                NumDescriptors = 1,
                Type = DescriptorHeapType.CbvSrvUav,
                Flags = DescriptorHeapFlags.ShaderVisible
            };
            
            void* cbvHeap = null;
            var iid = ID3D12DescriptorHeap.Guid;
            SilkMarshal.ThrowHResult
            (
                Device.CreateDescriptorHeap(&cbvHeapDesc, ref iid, &cbvHeap)
            );
            CBVHeap = (ID3D12DescriptorHeap*)cbvHeap;
        }

        private void CreateRenderTargetViews()
        {
            CpuDescriptorHandle rtvHandle = new CpuDescriptorHandle();
            rtvHandle.Ptr = RtvHeap.GetCPUDescriptorHandleForHeapStart().Ptr;
            var iid = ID3D12Resource.Guid;

            for (uint i = 0; i < FrameCount; i++)
            {
                ID3D12Resource* renderTarget;
                SilkMarshal.ThrowHResult
                (
                    SwapChain.GetBuffer(i, ref iid, (void**)&renderTarget)
                );
                RenderTargets[i] = renderTarget;

                
                Device.CreateRenderTargetView(renderTarget, (RenderTargetViewDesc*)0, rtvHandle);
                rtvHandle.Ptr += RtvDescriptorSize;
            }
        }

        private void CreateDepthStencil()
        {
            
            ID3D12Resource* depthStencil;

            var heapProperties = new HeapProperties(HeapType.Default);

            var resourceDesc = new ResourceDesc
            (
                ResourceDimension.Texture2D,
                0ul,
                (ulong) Window_.FramebufferSize.X,
                (uint) Window_.FramebufferSize.Y,
                1,
                1,
                Format.FormatD32Float,
                new SampleDesc() {Count = 1, Quality = 0},
                TextureLayout.LayoutUnknown,
                ResourceFlags.AllowDepthStencil
            );

            var clearValue = new ClearValue(Format.FormatD32Float, depthStencil: new DepthStencilValue(1.0f, 0));

            var iid = ID3D12Resource.Guid;
            SilkMarshal.ThrowHResult
            (
                Device.CreateCommittedResource
                (
                    &heapProperties, HeapFlags.None, &resourceDesc, ResourceStates.DepthWrite,
                    &clearValue, &iid, (void**) &depthStencil
                )
            );

            var dsvDesc = new DepthStencilViewDesc
            {
                Format = Format.FormatD32Float,
                ViewDimension = DsvDimension.Texture2D
            };
            Device.CreateDepthStencilView(depthStencil, &dsvDesc, DSVHeap.GetCPUDescriptorHandleForHeapStart());

            DepthStencil = depthStencil;
        }

        private void CreateCommandAllocator()
        {
            var iid = ID3D12CommandAllocator.Guid;

            for(int i = 0; i < FrameCount; i++)
            {
                void* commandAllocator;
                SilkMarshal.ThrowHResult
                (
                    Device.CreateCommandAllocator(CommandListType.Direct, &iid, &commandAllocator)
                );

                CommandAllocator[i] = (ID3D12CommandAllocator*)commandAllocator;
            }
        }

        private void CreateRootSignature()
        {
            DescriptorRange[] range = new DescriptorRange[2]
            {
                new DescriptorRange()
                {
                    RangeType = DescriptorRangeType.Srv,
                    NumDescriptors = 1,
                    BaseShaderRegister = 0,
                    RegisterSpace = 0,
                    OffsetInDescriptorsFromTableStart = D3D12.DescriptorRangeOffsetAppend 
                },
                new DescriptorRange()
                {
                    RangeType = DescriptorRangeType.Cbv,
                    NumDescriptors = 1,
                    BaseShaderRegister = 0,
                    RegisterSpace = 0,
                    OffsetInDescriptorsFromTableStart = D3D12.DescriptorRangeOffsetAppend 
                }
            };
            var range1 = range[0];
            var range2 = range[1];
            var rootParameters = stackalloc RootParameter[2]
            {
                new RootParameter()
                {
                    ParameterType = RootParameterType.TypeDescriptorTable,
                    DescriptorTable = new RootDescriptorTable(1, &range1),
                    ShaderVisibility = ShaderVisibility.Pixel
                },
                new RootParameter()
                {
                    ParameterType = RootParameterType.TypeDescriptorTable,
                    DescriptorTable = new RootDescriptorTable(1, &range2),
                    ShaderVisibility = ShaderVisibility.Vertex
                }
            };

            StaticSamplerDesc sampleDesc = new()
            {
                Filter = Filter.MinMagMipPoint,
                AddressU = TextureAddressMode.Wrap,
                AddressV = TextureAddressMode.Wrap,
                AddressW = TextureAddressMode.Wrap,
                MipLODBias = 0,
                MaxAnisotropy = 16,
                ComparisonFunc = ComparisonFunc.Never,
                BorderColor = StaticBorderColor.TransparentBlack,
                MinLOD = 0.0f,
                MaxLOD = float.MaxValue,
                ShaderRegister = 0,
                RegisterSpace = 0,
                ShaderVisibility = ShaderVisibility.Pixel,
            };

            RootSignatureDesc rootSignatureDesc = new RootSignatureDesc();
            rootSignatureDesc.NumParameters = 2;
            rootSignatureDesc.PParameters = rootParameters;
            rootSignatureDesc.NumStaticSamplers = 1;
            rootSignatureDesc.PStaticSamplers = &sampleDesc;
            rootSignatureDesc.Flags = 
            RootSignatureFlags.AllowInputAssemblerInputLayout | 
            RootSignatureFlags.DenyHullShaderRootAccess | 
            RootSignatureFlags.DenyDomainShaderRootAccess | 
            RootSignatureFlags.DenyGeometryShaderRootAccess | 
            RootSignatureFlags.DenyPixelShaderRootAccess;

            ID3D10Blob* signature = null;
            ID3D10Blob* error = null;

            SilkMarshal.ThrowHResult
            (
                D3d12.SerializeRootSignature(&rootSignatureDesc, D3DRootSignatureVersion.Version10, &signature, &error)
            );

            var iid = ID3D12RootSignature.Guid;
            void* rootSignature;

            SilkMarshal.ThrowHResult
            (
                Device.CreateRootSignature(0, signature->GetBufferPointer(), signature->GetBufferSize(), &iid, &rootSignature)
            );

            RootSignature = (ID3D12RootSignature*)rootSignature;

            signature->Release();
            if (error != null) error->Release();
        }

        private void CreateFence()
        {
            var iid = ID3D12Fence.Guid;
            void* fence;
            Device.CreateFence(0, FenceFlags.None, &iid, &fence);
            Fence = (ID3D12Fence*)fence;
        }

        private void CreateFenceEvent()
        {
            FenceValue[0] = 1;
            var fenceEvent = SilkMarshal.CreateWindowsEvent(null, false, false, null);

            if (fenceEvent == IntPtr.Zero)
            {
                var hr = Marshal.GetHRForLastWin32Error();
                Marshal.ThrowExceptionForHR(hr);
            }
        }

        private void CreatePipelineState()
        {
        }

        private void CreateCommandList()
        {
            void* commandList;
            var iid = ID3D12GraphicsCommandList.Guid;

            for(int i = 0; i < FrameCount; i++)
            {
                SilkMarshal.ThrowHResult
                (
                    Device.CreateCommandList(0, CommandListType.Direct, CommandAllocator[i], (ID3D12PipelineState*)0, &iid, &commandList)
                );

                CommandList[i] = (ID3D12GraphicsCommandList*)commandList;

                CommandList[i].Close();
            }
        }

        internal void WaitForGpu(bool moveToNextFrame, bool resetFrameBufferIndex = false)
        {
            SilkMarshal.ThrowHResult
            (
                CommandQueue.Signal(Fence, FenceValue[FrameBufferIndex])
            );

            if (moveToNextFrame)
            {
                FrameBufferIndex = SwapChain.GetCurrentBackBufferIndex();
            }

            if (!moveToNextFrame || (Fence.GetCompletedValue() < FenceValue[FrameBufferIndex]))
            {
                SilkMarshal.ThrowHResult
                (
                    Fence.SetEventOnCompletion(FenceValue[FrameBufferIndex], FenceEvent.ToPointer())
                );
                _ = SilkMarshal.WaitWindowsObjects(FenceEvent);
            }

            FenceValue[FrameBufferIndex]++;

            if (resetFrameBufferIndex) FrameBufferIndex = 0;
        }

        public DirectX12Device(IWindow window)
        {
            Window_ = window;
            D3d12 = D3D12.GetApi();
            DxGi = DXGI.GetApi(window, false);
            D3dCompiler = D3DCompiler.GetApi();

            uint dxgiFactoryFlags = 0;

#if DEBUG

        SilkMarshal.ThrowHResult
        (
            D3d12.GetDebugInterface(out DebugController)
        );
        DebugController.EnableDebugLayer();
        dxgiFactoryFlags |= 0x01;

#endif

            SilkMarshal.ThrowHResult
            (
                DxGi.CreateDXGIFactory2(dxgiFactoryFlags, out Factory)
            );

            Device = GetDevice();

            CreateCommandQueue();

            CreateSwapChain();

            CreateRTVHeap();

            CreateDSVHeap();

            CreateCBVHeap();

            CreateRenderTargetViews();

            CreateDepthStencil();

            CreateCommandAllocator();

            CreateRootSignature();

            CreatePipelineState();

            CreateFence();

            CreateFenceEvent();

            CreateCommandList();

            WaitForGpu(false);

            SetViewPort(0, 0, (uint)window.FramebufferSize.X, (uint)window.FramebufferSize.Y);
        }

        public void SetClearColor(float r, float g, float b, float a)
        {
            CurrnetClearColor = new float[] { r, g, b, a };
        }

        public void SetViewPort(int x, int y, uint width, uint height)
        {
            if (CommandList[FrameBufferIndex].AsVtblPtr() == null) return;

            viewport = new Viewport(0, 0, width, height, 0.0f, 1.0f);
            rect = new Box2D<int>(x, y, new Vector2D<int>((int)width, (int)height));
        }

        public void SetFrameBuffer(uint width, uint height)
        {
            if (width <= 0 || height <= 0) return;
            
            WaitForGpu(false, true);

            for (uint i = 0; i < FrameCount; i++)
            {
                RenderTargets[i]->Release();
                FenceValue[i] = FenceValue[FrameBufferIndex];
            }

            SilkMarshal.ThrowHResult
            (
                SwapChain.ResizeBuffers(FrameCount, width, height, Format.FormatR8G8B8A8Unorm, 0)
            );

            CreateRenderTargetViews();

            DepthStencil.Release();
            CreateDepthStencil();
        }

        private void SetResourceBarrier(ResourceStates stateBefore, ResourceStates stateAfter)
        {
            ResourceBarrier resourceBarrier = new ResourceBarrier();
            resourceBarrier.Type = ResourceBarrierType.Transition;
            resourceBarrier.Flags = ResourceBarrierFlags.None;
            resourceBarrier.Transition = new ResourceTransitionBarrier(RenderTargets[FrameBufferIndex], D3D12.ResourceBarrierAllSubresources, stateBefore, stateAfter);
            CommandList[FrameBufferIndex].ResourceBarrier(1, resourceBarrier);
        }

        public void ClearBuffer()
        {
            WaitForGpu(false);

            SilkMarshal.ThrowHResult
            (
                CommandAllocator[FrameBufferIndex].Reset()
            );

            SilkMarshal.ThrowHResult
            (
                CommandList[FrameBufferIndex].Reset(CommandAllocator[FrameBufferIndex], (ID3D12PipelineState*)0)
            );
            
            CommandList[FrameBufferIndex].SetGraphicsRootSignature(RootSignature);
            CommandList[FrameBufferIndex].RSSetViewports(1, viewport);
            CommandList[FrameBufferIndex].RSSetScissorRects(1, rect);


            SetResourceBarrier(ResourceStates.Present, ResourceStates.RenderTarget);


            CpuDescriptorHandle rtvHandle = new CpuDescriptorHandle();
            rtvHandle.Ptr = RtvHeap.GetCPUDescriptorHandleForHeapStart().Ptr + FrameBufferIndex * RtvDescriptorSize;
            CommandList[FrameBufferIndex].OMSetRenderTargets(1, rtvHandle, false, null);

            fixed (float* color = CurrnetClearColor)
            {
                CommandList[FrameBufferIndex].ClearRenderTargetView(rtvHandle, color, 0, (Box2D<int>*)0);
            }

            var dsvHandle = DSVHeap.GetCPUDescriptorHandleForHeapStart();
            CommandList[FrameBufferIndex].ClearDepthStencilView(dsvHandle, ClearFlags.Depth, 1, 0, 0, (Box2D<int>*)0);

















            //CommandList[FrameBufferIndex].DrawInstanced(3, 1, 0, 0);
        }

        public void SwapBuffer()
        {
            SetResourceBarrier(ResourceStates.RenderTarget, ResourceStates.Present);

            SilkMarshal.ThrowHResult
            (
                CommandList[FrameBufferIndex].Close()
            );
            
            const int CommandListsCount = 1;
            void* commandList = CommandList[FrameBufferIndex];
            var ppCommandLists = stackalloc ID3D12CommandList*[CommandListsCount]
            {
                (ID3D12CommandList*)commandList,
            };
            CommandQueue.ExecuteCommandLists(CommandListsCount, ppCommandLists);

            SilkMarshal.ThrowHResult
            (
                Device.GetDeviceRemovedReason()
            );

            SilkMarshal.ThrowHResult
            (
                SwapChain.Present(Window_.VSync ? 1u : 0u, 0)
            );

            WaitForGpu(false);

            FrameBufferIndex = SwapChain.GetCurrentBackBufferIndex();

            IsActivate = true;
        }

        public IPolygon GenPolygon(float[] vertices, uint[] indices, float[] uvs)
        {
            return new DirectX12Polygon(this, vertices, indices, uvs);
        }

        public IShader GenShader(string name)
        {
            return new DirectX12Shader(this, 
                @"

                Texture2D g_texture : register(t0);
                SamplerState g_sampler : register(s0);

                struct vs_in {
                    float3 position_local : POS;
                    float2 uvposition_local : UVPOS;
                };

                struct vs_out {
                    float4 position_clip : SV_POSITION;
                    float2 uvposition_clip : TEXCOORD0;
                };
                
                cbuffer ConstantBufferStruct
                {
                    float4x4 Projection;
                    float4 Color;
                    float4 TextureRect;
                }

                vs_out vs_main(vs_in input) {
                    vs_out output = (vs_out)0;

                    float4 position = float4(input.position_local, 1.0);
                    position = mul(Projection, position);

                    output.position_clip = position;

                    float2 texcoord = float2(TextureRect.x, TextureRect.y);
                    texcoord.x += input.uvposition_local.x * TextureRect.z;
                    texcoord.y += input.uvposition_local.y * TextureRect.w;

                    output.uvposition_clip = texcoord;

                    return output;
                }

                float4 ps_main(vs_out input) : SV_TARGET {
                    float4 totalcolor = float4(1.0, 1.0, 1.0, 1.0);

                    totalcolor = g_texture.Sample(g_sampler, input.uvposition_clip);

                    totalcolor.rgba *= Color.rgba;

                    return totalcolor;
                }
                ");
        }

        public unsafe ITexture GenTexture(void* data, int width, int height, RgbaType rgbaType)
        {
            return new DirectX12Texture(data, width, height, rgbaType);
        }

        public void DrawPolygon(IPolygon polygon, IShader shader, ITexture texture, BlendType blendType)
        {
            var dx12polygon = (DirectX12Polygon)polygon;
            var dx12shader = (DirectX12Shader)shader;
            var dx12texture = (DirectX12Texture)texture;

            dx12shader.Update(this);
            CommandList[FrameBufferIndex].SetPipelineState(dx12shader.PipelineState);
            CommandList[FrameBufferIndex].IASetPrimitiveTopology(D3DPrimitiveTopology.D3DPrimitiveTopologyTrianglelist);
            CommandList[FrameBufferIndex].IASetVertexBuffers(0, 1, dx12polygon.VertexBufferView_);
            CommandList[FrameBufferIndex].IASetIndexBuffer(dx12polygon.IndexBufferView_);
            CommandList[FrameBufferIndex].DrawIndexedInstanced(dx12polygon.IndiceCount, 1, 0, 0, 0);
        }

        public unsafe SKBitmap GetScreenPixels()
        {  
            return null;
        }

        public void Dispose()
        {
            WaitForGpu(false);


            Fence.Dispose();
            for (int i = 0; i < CommandList.Length; i++)
            {
                CommandList[i].Dispose();
            }
            RootSignature.Dispose();
            for (int i = 0; i < CommandAllocator.Length; i++)
            {
                CommandAllocator[i].Dispose();
            }
            for (int i = 0; i < RenderTargets.Length; i++)
            {
                RenderTargets[i]->Release();
            }
            CBVHeap.Dispose();
            DSVHeap.Dispose();
            RtvHeap.Dispose();
            SwapChain.Dispose();
            CommandQueue.Dispose();
            HardwareAdapters.Dispose();
            Device.Dispose();
            Factory.Dispose();

#if DEBUG

        DebugController.Dispose();

#endif
        }
    }
}