
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
    float4x4 Camera;
}

vs_out vs_main(vs_in input) {
    vs_out output = (vs_out)0;

    float4 position = float4(input.position_local, 1.0);
    position = mul(Projection, position);
    position = mul(position, Camera);

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