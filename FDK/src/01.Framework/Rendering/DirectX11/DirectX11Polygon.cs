using Silk.NET.Windowing;
using Silk.NET.Direct3D11;
using Silk.NET.Core.Native;
using SkiaSharp;

namespace SampleFramework
{
    public class DirectX11Polygon : IPolygon
    {
        public uint IndiceCount { get; set; }

        public uint VertexStride;

        public ComPtr<ID3D11Buffer> VertexBuffer = default;

        public ComPtr<ID3D11Buffer> IndexBuffer = default;


        public unsafe DirectX11Polygon(float[] vertices, uint[] indices, float[] uvs)
        {
            IndiceCount = (uint)indices.Length;
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

            // Create our vertex buffer.
            var vertexBufferDesc = new BufferDesc
            {
                ByteWidth = (uint)(sizeof(float) * mergedArray.Count),
                Usage = Usage.Default,
                BindFlags = (uint)BindFlag.VertexBuffer
            };

            //fixed (float* vertexData = vertices)
            fixed (float* vertexData = mergedArray.ToArray())
            {
                var subresourceData = new SubresourceData
                {
                    PSysMem = vertexData,
                    SysMemPitch = 0,
                    SysMemSlicePitch = 0
                };

                SilkMarshal.ThrowHResult(DirectX11Device.Device.CreateBuffer(in vertexBufferDesc, in subresourceData, ref VertexBuffer));
            }

            // Create our index buffer.
            var indexBufferDesc = new BufferDesc
            {
                ByteWidth = (uint)(indices.Length * sizeof(uint)),
                Usage = Usage.Default,
                BindFlags = (uint)BindFlag.IndexBuffer
            };

            fixed (uint* indexData = indices)
            {
                var subresourceData = new SubresourceData
                {
                    PSysMem = indexData
                };

                SilkMarshal.ThrowHResult(DirectX11Device.Device.CreateBuffer(in indexBufferDesc, in subresourceData, ref IndexBuffer));
            }
        }

        public void Dispose()
        {
            VertexBuffer.Dispose();
            IndexBuffer.Dispose();
        }
    }
}