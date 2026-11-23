// Shockwave plasma: one half of the expanding ring (o -> O -> "( )"). The mesh is a flat strip: UV.x 0 = end of the
// trail, 1 = leading edge. The vertex shader shapes it into the ring and its trail (see ShockwaveCommon.hlsl).
// The trail breaks up into drifting, stepped vapour toward its end. ShockwaveEmitter sets the shape values,
// _Fade and _Intensity (brightness over time, peaking at the release) per renderer.
Shader "VoxelCircuit/Shockwave"
{
    Properties
    {
        [HDR] _CoreColor ("Leading Edge Colour", Color) = (1.2, 2.4, 2.6, 1)
        [HDR] _PlasmaColor ("Plasma Colour", Color) = (0.15, 0.6, 2.2, 1)
        [HDR] _VapourColor ("Vapour Colour", Color) = (0.25, 0.45, 0.9, 1)
        _TrailPower ("Plasma Falloff", Range(0.5, 8)) = 2.5
        _EdgeWidth ("Leading Edge Width", Range(0.01, 0.5)) = 0.12
        _Cells ("Voxel Cells (vertical)", Range(4, 64)) = 24
        _FlickerSpeed ("Flicker Speed (changes per second)", Float) = 20
        _FlickerAmount ("Flicker Amount", Range(0, 1)) = 0.4
        _VapourAmount ("Vapour Amount", Range(0, 2)) = 0.8
        _VapourScale ("Vapour Cell Size", Range(2, 40)) = 12
        _VapourDrift ("Vapour Drift Speed", Float) = 0.6
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
        // One step after the heat haze, so the glow is drawn over the bent scene.
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Transparent+1" "IgnoreProjector" = "True" }

        Pass
        {
            Name "Shockwave"
            Tags { "LightMode" = "UniversalForward" }
            Blend One One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "ShockwaveCommon.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _CoreColor;
                half4 _PlasmaColor;
                half4 _VapourColor;
                half _TrailPower;
                half _EdgeWidth;
                half _Cells;
                float _FlickerSpeed;
                half _FlickerAmount;
                half _VapourAmount;
                half _VapourScale;
                float _VapourDrift;
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

            // Blocky vapour: a coarse grid of cells whose brightness drifts over time and thins out toward the trail end.
            half Vapour(float2 uv)
            {
                float2 grid = float2(uv.x * _VapourScale * 0.5, uv.y * _VapourScale * 2.0);
                float2 cell = floor(grid + float2(_Time.y * _VapourDrift, 0.0));
                half puff = ShockwaveHash(cell);
                half density = saturate((uv.x - 0.05) * 1.6) * (1.0h - smoothstep(0.55h, 0.95h, uv.x));
                return step(0.45h, puff) * puff * density;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half along = saturate(input.uv.x);                    // 0 = end of trail, 1 = leading edge
                half trail = pow(along, _TrailPower);
                half edge = smoothstep(1.0h - _EdgeWidth, 1.0h, along);

                // Voxel-like plasma: the sheet is cut into vertical cells that flicker independently.
                float cell = floor(input.uv.y * _Cells);
                half flicker = 1.0h - _FlickerAmount * ShockwaveHash(float2(cell, floor(_Time.y * _FlickerSpeed)));

                half3 color = _PlasmaColor.rgb * trail * flicker
                            + _CoreColor.rgb * edge
                            + _VapourColor.rgb * Vapour(input.uv) * _VapourAmount;
                half visible = ShockwaveEnds(input.uv.y) * ShockwaveClip(input.worldY, _ClipBottom, _ClipTop);
                return half4(color * visible * _Fade * _Intensity, 1.0h);
            }
            ENDHLSL
        }
    }
}
