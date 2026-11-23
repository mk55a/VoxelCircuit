// Shared look for every gameplay object (player ship, enemies): flat toon light bands with a minimum
// brightness, a team-coloured rim, optional emission, no specular and no reflections.
// The GameplayOutlineMask pass feeds GameplayOutlineFeature, which draws the outline in _OutlineColor.
Shader "VoxelCircuit/Gameplay Toon"
{
    Properties
    {
        [MainTexture] _BaseMap ("Palette", 2D) = "white" {}
        [MainColor] _BaseColor ("Tint", Color) = (1, 1, 1, 1)

        [Header(Light Bands)]
        _LitThreshold ("Lit Above (N.L)", Range(-1, 1)) = 0.45
        _ShadowThreshold ("Shadow Below (N.L)", Range(-1, 1)) = 0
        _LitBrightness ("Lit Brightness", Range(0, 1.5)) = 1
        _MidBrightness ("Mid Brightness", Range(0, 1.5)) = 0.84
        _MinBrightness ("Minimum Brightness", Range(0, 1.5)) = 0.7
        _LightColorInfluence ("Light Colour Influence", Range(0, 1)) = 0

        [Header(Rim)]
        _RimColor ("Rim Colour", Color) = (0.56, 0.96, 1, 1)
        _RimStart ("Rim Start", Range(0, 1)) = 0.55
        _RimEnd ("Rim End", Range(0, 1)) = 0.9
        _RimStrength ("Rim Strength", Range(0, 1)) = 0.9
        _RimTopBias ("Rim Top Bias", Range(0, 1)) = 0.45

        [Header(Emission)]
        [NoScaleOffset] _EmissionMap ("Emission Map", 2D) = "black" {}
        [HDR] _EmissionColor ("Emission Colour", Color) = (0, 0, 0, 1)

        [Header(Outline)]
        _OutlineColor ("Outline Colour", Color) = (0.04, 0.37, 0.44, 1)

        [Header(Fog)]
        [Toggle(_GAMEPLAY_FOG)] _ReceiveFog ("Receive Fog", Float) = 0
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);
        TEXTURE2D(_EmissionMap);

        // Same layout in every pass, so the SRP Batcher can batch all gameplay materials.
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half _LitThreshold;
            half _ShadowThreshold;
            half _LitBrightness;
            half _MidBrightness;
            half _MinBrightness;
            half _LightColorInfluence;
            half4 _RimColor;
            half _RimStart;
            half _RimEnd;
            half _RimStrength;
            half _RimTopBias;
            half4 _EmissionColor;
            half4 _OutlineColor;
        CBUFFER_END

        struct PositionOnlyAttributes
        {
            float4 positionOS : POSITION;
        };

        float4 PositionOnlyVert(PositionOnlyAttributes input) : SV_POSITION
        {
            return TransformObjectToHClip(input.positionOS.xyz);
        }
        ENDHLSL

        Pass
        {
            Name "ForwardToon"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma shader_feature_local _GAMEPLAY_FOG

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                half fogFactor : TEXCOORD3;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs position = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = position.positionCS;
                output.positionWS = position.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.fogFactor = ComputeFogFactor(position.positionCS.z);
                return output;
            }

            // Two flat steps of light, never darker than _MinBrightness.
            half LightBand(half nDotL)
            {
                if (nDotL > _LitThreshold) return _LitBrightness;
                if (nDotL > _ShadowThreshold) return _MidBrightness;
                return _MinBrightness;
            }

            // Fresnel: faces seen at a grazing angle pick up the rim colour, favouring faces that point up the screen.
            half RimAmount(float3 normalWS, float3 viewDirWS)
            {
                half fresnel = 1.0h - saturate(dot(normalWS, viewDirWS));
                float3 cameraUpWS = UNITY_MATRIX_V[1].xyz;
                half topWeight = lerp(1.0h - _RimTopBias, 1.0h, saturate(dot(normalWS, cameraUpWS)));
                return smoothstep(_RimStart, _RimEnd, fresnel) * _RimStrength * topWeight;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half3 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb * _BaseColor.rgb;
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);

                Light mainLight = GetMainLight();
                half3 lightTint = lerp(half3(1.0h, 1.0h, 1.0h), mainLight.color, _LightColorInfluence);
                half3 color = albedo * LightBand(dot(normalWS, mainLight.direction)) * lightTint;

                color = lerp(color, _RimColor.rgb, RimAmount(normalWS, viewDirWS));
                color += SAMPLE_TEXTURE2D(_EmissionMap, sampler_BaseMap, input.uv).rgb * _EmissionColor.rgb;

                #if defined(_GAMEPLAY_FOG)
                color = MixFog(color, input.fogFactor);
                #endif
                return half4(color, 1.0h);
            }
            ENDHLSL
        }

        // Read by GameplayOutlineFeature: writes this object's outline colour into the outline mask.
        Pass
        {
            Name "GameplayOutlineMask"
            Tags { "LightMode" = "GameplayOutlineMask" }
            Cull Back
            ZWrite Off
            ZTest Always

            HLSLPROGRAM
            #pragma vertex PositionOnlyVert
            #pragma fragment Frag

            half4 Frag() : SV_Target
            {
                return half4(_OutlineColor.rgb, 1.0h);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            Cull Back
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma vertex PositionOnlyVert
            #pragma fragment Frag

            half Frag() : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        // No ShadowCaster pass on purpose: gameplay objects never cast shadows.
    }
}
