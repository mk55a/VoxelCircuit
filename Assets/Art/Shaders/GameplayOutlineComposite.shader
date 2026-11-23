// Full-screen pass used by GameplayOutlineFeature. Reads the outline mask (each gameplay object drawn in its
// outline colour) and paints that colour on empty pixels next to an object, giving a silhouette outline.
// Assign this shader to the material referenced by the feature; Thickness is the outline width in pixels.
Shader "VoxelCircuit/Internal/Gameplay Outline Composite"
{
    Properties
    {
        _Thickness ("Thickness (pixels)", Range(1, 6)) = 2
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "OutlineComposite"
            ZWrite Off
            ZTest Always
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            SAMPLER(outline_point_clamp_sampler);
            float _Thickness;

            static const int Directions = 12;
            static const int Rings = 2;

            half4 SampleMask(float2 uv)
            {
                return SAMPLE_TEXTURE2D_X(_BlitTexture, outline_point_clamp_sampler, uv);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;

                // Inside an object: leave it untouched.
                if (SampleMask(uv).a > 0.5h)
                    return half4(0.0h, 0.0h, 0.0h, 0.0h);

                // Outside: take the colour of any object within _Thickness pixels.
                float2 texel = _ScreenSize.zw;
                half4 nearest = half4(0.0h, 0.0h, 0.0h, 0.0h);
                [unroll]
                for (int ring = 1; ring <= Rings; ring++)
                {
                    float radius = _Thickness * ring / Rings;
                    [unroll]
                    for (int i = 0; i < Directions; i++)
                    {
                        float angle = i * (TWO_PI / Directions);
                        half4 neighbour = SampleMask(uv + float2(cos(angle), sin(angle)) * radius * texel);
                        nearest = neighbour.a > nearest.a ? neighbour : nearest;
                    }
                }
                return nearest;
            }
            ENDHLSL
        }
    }
}
