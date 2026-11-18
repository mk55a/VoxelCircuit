// Far cloud ring for VoxelCircuit: unlit, transparent, ignores fog (it sits far beyond the fog distance),
// and scrolls its texture sideways on its own so the clouds drift without a script.
Shader "VoxelCircuit/Cloud Band"
{
    Properties
    {
        [MainTexture] _BaseMap ("Clouds (RGBA)", 2D) = "white" {}
        [HDR] _Tint ("Tint", Color) = (1, 1, 1, 1)
        _Opacity ("Opacity", Range(0, 1)) = 1
        _ScrollSpeed ("Drift (texture widths per second)", Float) = 0.002
    }

    SubShader
    {
        // Transparent-10: drawn just before other transparent objects, so the glass ring tints it correctly.
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Transparent-10" "IgnoreProjector" = "True" }

        Pass
        {
            Name "CloudBand"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _Tint;
                half _Opacity;
                float _ScrollSpeed;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap) + float2(_Time.y * _ScrollSpeed, 0);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 colour = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _Tint;
                colour.a *= _Opacity;
                return colour;
            }
            ENDHLSL
        }
    }
}
