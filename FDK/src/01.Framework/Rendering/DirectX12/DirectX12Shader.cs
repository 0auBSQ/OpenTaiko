using System;
using System.Runtime.CompilerServices;
using Silk.NET.Windowing;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;
using Silk.NET.Core.Native;
using Silk.NET.Maths;
using SkiaSharp;

namespace SampleFramework
{
    public class DirectX12Shader : IShader
    {
        public struct ConstantBufferStruct
        {
            public Matrix4X4<float> Projection;

            public Vector4D<float> Color;

            public Vector4D<float> TextureRect;
        }

        public ComPtr<ID3D12PipelineState> PipelineState;

        public ComPtr<ID3D12Resource> ConstantBuffer;

        public ComPtr<ID3D12ShaderReflectionConstantBuffer> ConstantBufferView_;

        private unsafe void* DataBegin;

        private ConstantBufferStruct ConstantBufferStruct_ = new ConstantBufferStruct();


        internal unsafe DirectX12Shader(DirectX12Device device, string shaderSource)
        {
            CreateConstantBuffer(device);

            DirectXShaderSource directXShaderSource = new DirectXShaderSource(device.D3dCompiler);

            const int ElementsLength = 2;

            var inputElementDescs = stackalloc InputElementDesc[ElementsLength]
            {
                new InputElementDesc()
                {
                    SemanticName = (byte*)SilkMarshal.StringToMemory("POS"),
                    SemanticIndex = 0,
                    Format = Format.FormatR32G32B32Float,
                    InputSlot = 0,
                    AlignedByteOffset = 0,
                    InputSlotClass = InputClassification.PerVertexData,
                    InstanceDataStepRate = 0
                }
                ,
                new InputElementDesc()
                {
                    SemanticName = (byte*)SilkMarshal.StringToMemory("UVPOS"),
                    SemanticIndex = 0,
                    Format = Format.FormatR32G32Float,
                    InputSlot = 0,
                    AlignedByteOffset = sizeof(float) * 3,
                    InputSlotClass = InputClassification.PerVertexData,
                    InstanceDataStepRate = 0
                }
            };

            GraphicsPipelineStateDesc graphicsPipelineStateDesc = new GraphicsPipelineStateDesc();

            graphicsPipelineStateDesc.InputLayout = new InputLayoutDesc()
            {
                PInputElementDescs = inputElementDescs,
                NumElements = ElementsLength,
            };

            graphicsPipelineStateDesc.PRootSignature = device.RootSignature;
            graphicsPipelineStateDesc.VS = new ShaderBytecode(directXShaderSource.VertexCode.GetBufferPointer(), directXShaderSource.VertexCode.GetBufferSize());
            graphicsPipelineStateDesc.PS = new ShaderBytecode(directXShaderSource.PixelCode.GetBufferPointer(), directXShaderSource.PixelCode.GetBufferSize());

            RasterizerDesc rasterizerDesc = new RasterizerDesc()
            {
                FillMode = FillMode.Solid,
                CullMode = CullMode.Back,
                FrontCounterClockwise = 0,
                DepthBias = D3D12.DefaultDepthBias,
                DepthBiasClamp = 0,
                SlopeScaledDepthBias = 0,
                DepthClipEnable = 1,
                MultisampleEnable = 0,
                AntialiasedLineEnable = 0,
                ForcedSampleCount = 0,
                ConservativeRaster = ConservativeRasterizationMode.Off
            };
            graphicsPipelineStateDesc.RasterizerState = rasterizerDesc;


            var defaultRenderTargetBlend = new RenderTargetBlendDesc()
            {
                BlendEnable = 0,
                LogicOpEnable = 0,
                SrcBlend = Blend.One,
                DestBlend = Blend.Zero,
                BlendOp = BlendOp.Add,
                SrcBlendAlpha = Blend.One,
                DestBlendAlpha = Blend.Zero,
                BlendOpAlpha = BlendOp.Add,
                LogicOp = LogicOp.Noop,
                RenderTargetWriteMask = (byte)ColorWriteEnable.All
            };
            BlendDesc blendDesc = new BlendDesc()
            {
                AlphaToCoverageEnable = 0,
                IndependentBlendEnable = 0,
                RenderTarget = new BlendDesc.RenderTargetBuffer()
                {
                    [0] = defaultRenderTargetBlend,
                    [1] = defaultRenderTargetBlend,
                    [2] = defaultRenderTargetBlend,
                    [3] = defaultRenderTargetBlend,
                    [4] = defaultRenderTargetBlend,
                    [5] = defaultRenderTargetBlend,
                    [6] = defaultRenderTargetBlend,
                    [7] = defaultRenderTargetBlend
                }
            };

            var defaultStencilOp = new DepthStencilopDesc
            {
                StencilFailOp = StencilOp.Keep,
                StencilDepthFailOp = StencilOp.Keep,
                StencilPassOp = StencilOp.Keep,
                StencilFunc = ComparisonFunc.Always
            };

            graphicsPipelineStateDesc.BlendState = blendDesc;


            graphicsPipelineStateDesc.DepthStencilState = new ()
            {
                DepthEnable = 1,
                DepthWriteMask = DepthWriteMask.All,
                DepthFunc = ComparisonFunc.Less,
                StencilEnable = 0,
                StencilReadMask = D3D12.DefaultStencilReadMask,
                StencilWriteMask = D3D12.DefaultStencilWriteMask,
                FrontFace = defaultStencilOp,
                BackFace = defaultStencilOp
            };
            graphicsPipelineStateDesc.SampleMask = uint.MaxValue;
            graphicsPipelineStateDesc.PrimitiveTopologyType = PrimitiveTopologyType.Triangle;
            graphicsPipelineStateDesc.NumRenderTargets = 1;
            graphicsPipelineStateDesc.RTVFormats[0] = Format.FormatR8G8B8A8Unorm;
            graphicsPipelineStateDesc.DSVFormat = Format.FormatUnknown;
            graphicsPipelineStateDesc.SampleDesc.Count = 1;
            graphicsPipelineStateDesc.SampleDesc.Quality = 0;
            graphicsPipelineStateDesc.DepthStencilState.DepthEnable = 0;

            ID3D12PipelineState* pipelineState;
            var iid = ID3D12PipelineState.Guid;
            SilkMarshal.ThrowHResult
            (
                device.Device.CreateGraphicsPipelineState(graphicsPipelineStateDesc, &iid, (void**)&pipelineState)
            );

            directXShaderSource.Dispose();

            PipelineState = pipelineState;
            
        }

        private unsafe void CreateConstantBuffer(DirectX12Device device)
        {
            long size = (sizeof(ConstantBufferStruct) + 0xff) & ~0xff;
            HeapProperties heapProperties = new HeapProperties()
            {
                Type = HeapType.Upload,
                CPUPageProperty = CpuPageProperty.Unknown,
                MemoryPoolPreference = MemoryPool.Unknown,
                CreationNodeMask = 1,
                VisibleNodeMask = 1
            };

            ResourceDesc resourceDesc = new ResourceDesc()
            {
                Dimension = ResourceDimension.Buffer,
                Alignment = 0,
                Width = (ulong)size,
                Height = 1,
                DepthOrArraySize = 1,
                MipLevels = 1,
                Format = Format.FormatUnknown,
                SampleDesc = new SampleDesc()
                {
                    Count = 1,
                    Quality = 0,
                },
                Layout = TextureLayout.LayoutRowMajor,
                Flags = ResourceFlags.None
            };

            void* constantBuffer;
            var riid = ID3D12Resource.Guid;
            SilkMarshal.ThrowHResult
            (
                device.Device.CreateCommittedResource(heapProperties, HeapFlags.None, resourceDesc, ResourceStates.GenericRead, (ClearValue*)0, &riid, &constantBuffer)
            );
            ConstantBuffer = (ID3D12Resource*)constantBuffer;


            


            CpuDescriptorHandle rtvHandle = new CpuDescriptorHandle();
            rtvHandle.Ptr = device.CBVHeap.GetCPUDescriptorHandleForHeapStart().Ptr;

            ConstantBufferViewDesc constantBufferViewDesc = new()
            {
                SizeInBytes = (uint)ConstantBuffer.GetDesc().Width,
                BufferLocation = ConstantBuffer.GetGPUVirtualAddress(),
            };

            device.Device.CreateConstantBufferView(constantBufferViewDesc, rtvHandle);


            SilkMarshal.ThrowHResult(device.Device.GetDeviceRemovedReason());


            Silk.NET.Direct3D12.Range range = new Silk.NET.Direct3D12.Range();
            void* dataBegin;
            SilkMarshal.ThrowHResult(ConstantBuffer.Map(0, &range, &dataBegin));
            DataBegin = dataBegin;

            Update(device);
        }

        public unsafe void SetMVP(Matrix4X4<float> mvp)
        {
            ConstantBufferStruct_.Projection = mvp;
        }

        public void SetColor(Vector4D<float> color)
        {
            ConstantBufferStruct_.Color = color;
        }

        public void SetTextureRect(Vector4D<float> rect)
        {
            ConstantBufferStruct_.TextureRect = rect;
        }

        public void SetCamera(Matrix4X4<float> camera)
        {
        }

        internal unsafe void Update(DirectX12Device device)
        {
            CpuDescriptorHandle rtvHandle = new CpuDescriptorHandle();
            rtvHandle.Ptr = device.CBVHeap.GetCPUDescriptorHandleForHeapStart().Ptr;

            device.CommandList[device.FrameBufferIndex].SetDescriptorHeaps(1, device.CBVHeap);
            device.CommandList[device.FrameBufferIndex].SetGraphicsRootDescriptorTable(0, device.CBVHeap.GetGPUDescriptorHandleForHeapStart());

            fixed(void* data = &ConstantBufferStruct_)
            {
                Unsafe.CopyBlock(DataBegin, data, (uint)sizeof(ConstantBufferStruct ));
            }
        }

        public void Dispose()
        {
            PipelineState.Dispose();
            ConstantBuffer.Dispose();
        }
    }
}