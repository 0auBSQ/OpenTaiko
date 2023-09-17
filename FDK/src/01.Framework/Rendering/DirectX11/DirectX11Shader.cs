using System;
using Silk.NET.Windowing;
using Silk.NET.Direct3D11;
using Silk.NET.DXGI;
using Silk.NET.Core.Native;
using Silk.NET.Maths;
using SkiaSharp;

namespace SampleFramework
{
    public class DirectX11Shader : IShader
    {
        public struct ConstantBufferStruct
        {
            public Matrix4X4<float> Projection;

            public Vector4D<float> Color;

            public Vector4D<float> TextureRect;

            public Matrix4X4<float> Camera;
        }

        internal ComPtr<ID3D10Blob> VertexCode = default;

        internal ComPtr<ID3D10Blob> PixelCode = default;

        internal ComPtr<ID3D11VertexShader> VertexShader = default;

        internal ComPtr<ID3D11PixelShader> PixelShader = default;

        internal ComPtr<ID3D11InputLayout> InputLayout = default;

        internal ComPtr<ID3D11Buffer> ConstantBuffer;

        internal ConstantBufferStruct ConstantBufferStruct_;

        public unsafe DirectX11Shader(string shaderSource)
        {
            var shaderBytes = System.Text.Encoding.ASCII.GetBytes(shaderSource);
            ComPtr<ID3D10Blob> vertexErrors = default;

            // Compile vertex shader.
            HResult hr = DirectX11Device.D3dCompiler.Compile
            (
                in shaderBytes[0],
                (uint)shaderBytes.Length,
                nameof(shaderSource),
                null,
                default(ComPtr<ID3DInclude>),
                "vs_main",
                "vs_5_0",
                0,
                0,
                ref VertexCode,
                ref vertexErrors
            );

            // Check for compilation errors.
            if (hr.IsFailure)
            {
                if (vertexErrors.Handle != null)
                {
                    Console.WriteLine(SilkMarshal.PtrToString((int)vertexErrors.GetBufferPointer(), NativeStringEncoding.LPWStr));
                }

                hr.Throw();
            }

            // Compile pixel shader.
            ComPtr<ID3D10Blob> pixelErrors = default;
            hr = DirectX11Device.D3dCompiler.Compile
            (
                in shaderBytes[0],
                (uint)shaderBytes.Length,
                nameof(shaderSource),
                null,
                default(ComPtr<ID3DInclude>),
                "ps_main",
                "ps_5_0",
                0,
                0,
                ref PixelCode,
                ref pixelErrors
            );

            // Check for compilation errors.
            if (hr.IsFailure)
            {
                if (pixelErrors.Handle != null)
                {
                    Console.WriteLine(SilkMarshal.PtrToString((int)pixelErrors.GetBufferPointer(), NativeStringEncoding.LPWStr));
                }

                hr.Throw();
            }

            // Clean up any resources.
            vertexErrors.Dispose();
            pixelErrors.Dispose();

            ConstantBufferStruct_ = new();









            
            // Create vertex shader.
            SilkMarshal.ThrowHResult
            (
                DirectX11Device.Device.CreateVertexShader
                (
                    VertexCode.GetBufferPointer(),
                    VertexCode.GetBufferSize(),
                    default(ComPtr<ID3D11ClassLinkage>),
                    ref VertexShader
                )
            );

            // Create pixel shader.
            SilkMarshal.ThrowHResult
            (
                DirectX11Device.Device.CreatePixelShader
                (
                    PixelCode.GetBufferPointer(),
                    PixelCode.GetBufferSize(),
                    default(ComPtr<ID3D11ClassLinkage>),
                    ref PixelShader
                )
            );

            // Describe the layout of the input data for the shader.
            fixed (byte* posName = SilkMarshal.StringToMemory("POS"))
            {
                fixed (byte* uvposName = SilkMarshal.StringToMemory("UVPOS"))
                {
                    var inputElements = new InputElementDesc[]
                    {
                        new InputElementDesc()
                        {
                            SemanticName = posName,
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
                            SemanticName = uvposName,
                            SemanticIndex = 0,
                            Format = Format.FormatR32G32Float,
                            InputSlot = 0,
                            AlignedByteOffset = sizeof(float) * 3,
                            InputSlotClass = InputClassification.PerVertexData,
                            InstanceDataStepRate = 0
                        }
                    };

                    fixed(InputElementDesc* data = inputElements)
                    {
                        SilkMarshal.ThrowHResult
                        (
                            DirectX11Device.Device.CreateInputLayout
                            (
                                data,
                                (uint)inputElements.Length,
                                VertexCode.GetBufferPointer(),
                                VertexCode.GetBufferSize(),
                                ref InputLayout
                            )
                        );
                    }
                }
            }


            BufferDesc bufferDesc = new()
            {
                ByteWidth = (uint)sizeof(ConstantBufferStruct),
                Usage = Usage.Default,
                BindFlags = (uint)BindFlag.ConstantBuffer,
                CPUAccessFlags = 0,
                MiscFlags = 0,
                StructureByteStride = 0
            };

            SilkMarshal.ThrowHResult(
                DirectX11Device.Device.CreateBuffer(in bufferDesc, null, ref ConstantBuffer)
            );
        }

        public void SetMVP(Matrix4X4<float> mvp)
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
            ConstantBufferStruct_.Camera = camera;
        }

        public unsafe void Update()
        {
            DirectX11Device.ImmediateContext.UpdateSubresource(ConstantBuffer, 0, null, ConstantBufferStruct_, 0, 0);
        }

        public void Dispose()
        {
            ConstantBuffer.Dispose();
            VertexCode.Dispose();
            PixelCode.Dispose();
            VertexShader.Dispose();
            PixelShader.Dispose();
            InputLayout.Dispose();
        }
    }
}