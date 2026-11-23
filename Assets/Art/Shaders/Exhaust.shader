// Engine exhaust flame: additive, unlit, stepped into voxel-like bands from nozzle (UV.y = 0) to tip (UV.y = 1),
// each band flickering on its own. _Intensity is driven per renderer by ThrusterExhaust.
Shader "VoxelCircuit/Exhaust"
{
    Properties
    {
        [HDR] _CoreColor ("Nozzle Colour", Color) = (0.55, 0.95, 1, 1)
        [HDR] _TailColor ("Tail Colour", Color) = (0.05, 0.45, 1, 1)
        _Intensity ("Intensity (set by ThrusterExhaust)", Float) = 1
        _Steps ("Voxel Steps", Range(2, 16)) = 6
        _Falloff ("Tail Falloff", Range(0.2, 4)) = 1.4
        _FlickerSpeed ("Flicker Speed (changes per second)", Float) = 24
        _FlickerAmount ("Flicker Amount", Range(0, 1)) = 0.35
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Transparent" "IgnoreProjector" = "True" }

        Pass
        {
            Name "Exhaust"
            Tags { "LightMode" = "UniversalForward" }
            Blend One One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _CoreColor;
                half4 _TailColor;
                half _Intensity;
                half _Steps;
                half _Falloff;
                float _FlickerSpeed;
                half _FlickerAmount;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            float Hash(float2 p)
            {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float band = floor(saturate(input.uv.y) * _Steps);
                half along = band / _Steps;                                   // 0 at the nozzle, close to 1 at the tip

                half3 color = lerp(_CoreColor.rgb, _TailColor.rgb, along);
                half fade = pow(1.0h - along, _Falloff);
                half flicker = 1.0h - _FlickerAmount * Hash(float2(band, floor(_Time.y * _FlickerSpeed)));
                return half4(color * fade * flicker * _Intensity, 1.0h);
            }
            ENDHLSL
        }
    }
}
