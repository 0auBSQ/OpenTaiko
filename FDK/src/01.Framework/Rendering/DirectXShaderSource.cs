using System;
using Silk.NET.Windowing;
using Silk.NET.Maths;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D.Compilers;
using Silk.NET.DXGI;

namespace SampleFramework
{
    unsafe class DirectXShaderSource : IDisposable
    {
        const string shaderSource = @"

    Texture2D g_texture : register(t0);
    SamplerState g_sampler : register(s0);
                
    cbuffer ConstantBuffer : register(b0)
    {
        float4x4 Projection;
        float4 Color;
        float4 TextureRect;
    }

    struct vs_in {
        float3 position_local : POS;
        float2 uvposition_local : UVPOS;
    };

    struct vs_out {
        float4 position_clip : SV_POSITION;
        float2 uvposition_clip : TEXCOORD0;
    };

    vs_out vs_main(vs_in input) {
        vs_out output = (vs_out)0;

        float4 position = float4(input.position_local, 1.0);

        output.position_clip = position;
                    
        output.uvposition_clip = input.uvposition_local;
        return output;
    }

    float4 ps_main(vs_out input) : SV_TARGET {
        float4 totalcolor = float4( input.uvposition_clip.x, input.uvposition_clip.y, 0.0, 1.0 );

        return totalcolor;
    }
    ";

        public ComPtr<ID3D10Blob> VertexCode = default;

        public ComPtr<ID3D10Blob> PixelCode = default;

        public DirectXShaderSource(D3DCompiler d3dCompiler)
        {
            var shaderBytes = System.Text.Encoding.ASCII.GetBytes(shaderSource);
            ComPtr<ID3D10Blob> vertexErrors = default;

            // Compile vertex shader.
            HResult hr = d3dCompiler.Compile
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
            hr = d3dCompiler.Compile
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
        }

        public void Dispose()
        {
            VertexCode.Dispose();
            PixelCode.Dispose();
        }
    }
}