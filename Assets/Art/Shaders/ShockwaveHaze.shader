// Heat haze for the shockwave: bends whatever is behind the wave (enemies, buildings) by sampling a copy of the
// scene with a rippling offset. Uses the same strip mesh and ring shape as VoxelCircuit/Shockwave, as a child of
// the glow object. The scene copy (Opaque Texture) is requested by ShockwaveScreenEffects only while a wave runs.
// Note: the copy holds opaque objects only, so transparent things behind the haze (glass, clouds) are not bent.
Shader "VoxelCircuit/Shockwave Haze"
{
    Properties
    {
        _Strength ("Distortion Strength (screen fraction)", Range(0, 0.08)) = 0.02
        _TrailPower ("Haze Falloff", Range(0.2, 6)) = 1.2
        _RippleFrequency ("Ripple Frequency", Range(1, 80)) = 28
        _RippleSpeed ("Ripple Speed", Float) = 14
        _Cells ("Voxel Cells (vertical)", Range(4, 64)) = 24
        _Fade ("Fade (set by ShockwaveEmitter)", Range(0, 1)) = 1
        _CurveDepth ("Curve Depth (set by ShockwaveEmitter)", Float) = 3
        _FrontDistance ("Front Distance (set by ShockwaveEmitter)", Float) = 3
        _TrailLength ("Trail Length (set by ShockwaveEmitter)", Float) = 10
        _ClipBottom ("Clip Bottom (set by ShockwaveEmitter)", Float) = -100
        _ClipTop ("Clip Top (set by ShockwaveEmitter)", Float) = 100
        _Intensity ("Intensity (set by ShockwaveEmitter)", Float) = 1
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Transparent" "IgnoreProjector" = "True" }

        Pass
        {
            Name "ShockwaveHaze"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
            #include "ShockwaveCommon.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half _Strength;
                half _TrailPower;
                half _RippleFrequency;
                float _RippleSpeed;
                half _Cells;
                half _Fade;
                float _CurveDepth;
                float _FrontDistance;
                float _TrailLength;
                float _ClipBottom;
                float _ClipTop;
                half _Intensity;
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
                float worldY : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = ShockwaveRingWS(input.positionOS.xyz, _CurveDepth, _FrontDistance, _TrailLength);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.uv = input.uv;
                output.worldY = positionWS.y;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 screenUV = input.positionCS.xy / _ScaledScreenParams.xy;

                // Strongest right behind the leading edge, fading along the trail.
                half mask = pow(saturate(input.uv.x), _TrailPower) * ShockwaveEnds(input.uv.y)
                          * ShockwaveClip(input.worldY, _ClipBottom, _ClipTop) * _Fade;

                // Stepped ripple: horizontal waves plus a per-cell vertical jitter, so the bend looks blocky like the world.
                float cell = floor(input.uv.y * _Cells);
                float ripple = sin(input.uv.x * _RippleFrequency - _Time.y * _RippleSpeed + cell * 1.7);
                float jitter = ShockwaveHash(float2(cell, floor(_Time.y * 12.0))) - 0.5;
                float2 offset = float2(ripple, jitter) * _Strength * mask * saturate(_Intensity);

                half3 bent = SampleSceneColor(screenUV + offset);
                return half4(bent, saturate(mask * 1.5h));
            }
            ENDHLSL
        }
    }
}
