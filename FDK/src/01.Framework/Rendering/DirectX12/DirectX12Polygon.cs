using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Silk.NET.Windowing;
using Silk.NET.Maths;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D.Compilers;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;

namespace SampleFramework
{
    internal class DirectX12Polygon : IPolygon
    {
        public uint IndiceCount { get; set; }

        public uint VertexStride;


        public ComPtr<ID3D12Resource> VertexBuffer;

        public VertexBufferView VertexBufferView_;

        public ComPtr<ID3D12Resource> IndexBuffer;

        public IndexBufferView IndexBufferView_;


        private unsafe void CreateVertexBuffer(DirectX12Device device, float[] vertices, float[] uvs)
        {
            VertexStride = 5U * sizeof(float);

            List<float> mergedArray = new();
            for(int i = 0; i < vertices.Length / 3; i++)
            {
                int pos = 3 * i;
                int pos_uv = 2 * i;
                mergedArray.Add(vertices[pos]);
                mergedArray.Add(vertices[pos + 1]);
                mergedArray.Add(vertices[pos + 2]);
                mergedArray.Add(uvs[pos_uv]);
                mergedArray.Add(uvs[pos_uv + 1]);
            }



            uint vertexBufferSize = (uint)(sizeof(float) * mergedArray.Count);

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
                Width = vertexBufferSize,
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

            void* vertexBuffer;
            var iid = ID3D12Resource.Guid;
            SilkMarshal.ThrowHResult
            (
                device.Device.CreateCommittedResource(heapProperties, HeapFlags.None, resourceDesc, ResourceStates.GenericRead, null, &iid, &vertexBuffer)
            );
            VertexBuffer = (ID3D12Resource*)vertexBuffer;

            Silk.NET.Direct3D12.Range range = new Silk.NET.Direct3D12.Range();

            void* dataBegin;
            SilkMarshal.ThrowHResult(VertexBuffer.Map(0, &range, &dataBegin));

            var data = (float*)SilkMarshal.Allocate(sizeof(float) * mergedArray.Count);
            for(int i = 0; i < mergedArray.Count; i++)
            {
                data[i] = mergedArray[i];
            }
            
            Unsafe.CopyBlock(dataBegin, data, vertexBufferSize);
            VertexBuffer.Unmap(0, (Silk.NET.Direct3D12.Range*)0);

            VertexBufferView_ = new VertexBufferView()
            {
                BufferLocation = VertexBuffer.GetGPUVirtualAddress(),
                StrideInBytes = VertexStride,
                SizeInBytes = vertexBufferSize,
            };
        }

        private unsafe void CreateIndexBuffer(DirectX12Device device, uint[] indices)
        {
            uint bufferSize = (uint)(sizeof(uint) * indices.Length);

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
                Width = bufferSize,
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

            void* indexBuffer;
            var iid = ID3D12Resource.Guid; 
            SilkMarshal.ThrowHResult
            (
                device.Device.CreateCommittedResource(heapProperties, HeapFlags.None, resourceDesc, ResourceStates.GenericRead, null, &iid, &indexBuffer)
            );

            IndexBuffer = (ID3D12Resource*)indexBuffer;

            Silk.NET.Direct3D12.Range range = new Silk.NET.Direct3D12.Range();

            void* dataBegin;
            SilkMarshal.ThrowHResult(IndexBuffer.Map(0, &range, &dataBegin));

            var data = (uint*)SilkMarshal.Allocate(sizeof(uint) * indices.Length);
            for(int i = 0; i < indices.Length; i++)
            {
                data[i] = indices[i];
            }
            
            Unsafe.CopyBlock(dataBegin, data, bufferSize);
            IndexBuffer.Unmap(0, (Silk.NET.Direct3D12.Range*)0);

            IndexBufferView_ = new IndexBufferView()
            {
                BufferLocation = IndexBuffer.GetGPUVirtualAddress(),
                SizeInBytes = bufferSize,
                Format = Format.FormatR32Uint
            };
        }

        public unsafe DirectX12Polygon(DirectX12Device device, float[] vertices, uint[] indices, float[] uvs)
        {
            IndiceCount = (uint)indices.Length;
            CreateVertexBuffer(device, vertices, uvs);
            CreateIndexBuffer(device, indices);
        }

        public void Dispose()
        {
            VertexBuffer.Release();
            IndexBuffer.Release();
        }
    }
}